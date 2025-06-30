using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FinanceTool.MongoModels;
using FinanceTool.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FinanceTool
{
    /// <summary>
    /// 세션 데이터를 처리하여 raw_data 컬렉션으로 이동시키는 프로세서
    /// </summary>
    public class SessionDataProcessor
    {
        private readonly RawDataRepository _rawDataRepository;
        private readonly ProcessDataRepository _processDataRepository;
        private readonly UploadedFileRepository _uploadedFileRepository;
        private readonly FileSessionRepository _fileSessionRepository;
        private const string UPLOAD_FOLDER = @"C:\Dmillions\excel_upload";

        public delegate Task UpdateProgressDelegate(int percentage, string message);

        public SessionDataProcessor()
        {
            _rawDataRepository = new RawDataRepository();
            _processDataRepository = new ProcessDataRepository();
            _uploadedFileRepository = new UploadedFileRepository();
            _fileSessionRepository = new FileSessionRepository();
        }


        /// <summary>
        /// 전체 워크플로우 실행 (기존 btn_selectFile_Click 대체)
        /// </summary>
        public async Task<ProcessingResult> ProcessFullWorkflowAsync(
            List<uc_MultiFileUpload.SessionDisplayData> selectedSessions,
            UpdateProgressDelegate progressCallback)
        {
            var result = new ProcessingResult();

            try
            {
                await progressCallback(5, "워크플로우 시작...");

                // 1. 선택된 세션 검증
                if (selectedSessions == null || selectedSessions.Count == 0)
                {
                    result.ErrorMessage = "처리할 세션이 선택되지 않았습니다.";
                    return result;
                }

                await progressCallback(10, "컬렉션 초기화 중...");

                // 2. 컬렉션 초기화 (기존 btn_selectFile_Click 로직)
                await InitializeCollectionsAsync();

                await progressCallback(20, "세션 데이터 분석 중...");

                // 3. 세션별 데이터 추출 및 분석
                Debug.WriteLine($"AnalyzeSelectedSessionsAsync =>  selectedSessions : {selectedSessions}");
                var sessionAnalysis = await AnalyzeSelectedSessionsAsync(selectedSessions);
                if (!sessionAnalysis.IsValid)
                {
                    result.ErrorMessage = sessionAnalysis.ErrorMessage;
                    return result;
                }

                await progressCallback(30, "파일 데이터 처리 시작...");

                Debug.WriteLine($"ProcessSessionsToRawDataAsync =>  SessionData : {sessionAnalysis.SessionData}");

                // 4. 세션별 데이터를 raw_data로 처리
                var processingResult = await ProcessSessionsToRawDataAsync(
                    sessionAnalysis.SessionData,
                    progressCallback);

                if (!processingResult.Success)
                {
                    result.ErrorMessage = processingResult.ErrorMessage;
                    return result;
                }

                await progressCallback(90, "후처리 작업 중...");
                Debug.WriteLine($"ExecutePostProcessingInitializationAsync =>  start ");
                // 5. 후처리 초기화 작업
                await ExecutePostProcessingInitializationAsync();

                //Debug.WriteLine($"ExecutePostProcessingInitializationAsync => DataColumnCollection: [{string.Join(", ", DataHandler.excelData.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}]");

                await progressCallback(100, "처리 완료");

                result.Success = true;
                result.ProcessedRowCount = processingResult.TotalRowsProcessed;
                result.ProcessedFileCount = processingResult.TotalFilesProcessed;
                result.SessionResults = processingResult.SessionResults;

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"전체 워크플로우 오류: {ex.Message}");
                result.ErrorMessage = $"처리 중 오류가 발생했습니다: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 컬렉션 초기화 (기존 btn_selectFile_Click에서 분리)
        /// </summary>
        private async Task InitializeCollectionsAsync()
        {
            try
            {
                // 필요한 Repository 인스턴스들 생성
                var clusteringRepository = new ClusteringRepository();
                var columnMappingRepository = new ColumnMappingRepository();
                var processViewRepository = new ProcessViewRepository();

                Debug.WriteLine("5개 컬렉션 초기화 시작...");

                // 1. clustering_results 컬렉션 초기화
                await clusteringRepository.DeleteManyAsync(FilterDefinition<ClusteringResultDocument>.Empty);
                Debug.WriteLine("clustering_results 컬렉션 초기화 완료");

                // 2. column_mapping 컬렉션 초기화
                await columnMappingRepository.DeleteManyAsync(FilterDefinition<ColumnMappingDocument>.Empty);
                Debug.WriteLine("column_mapping 컬렉션 초기화 완료");

                // 3. process_data 컬렉션 초기화
                await _processDataRepository.DeleteManyAsync(FilterDefinition<ProcessDataDocument>.Empty);
                Debug.WriteLine("process_data 컬렉션 초기화 완료");

                // 4. process_view_data 컬렉션 초기화
                await processViewRepository.DeleteManyAsync(FilterDefinition<ProcessViewDocument>.Empty);
                Debug.WriteLine("process_view_data 컬렉션 초기화 완료");

                // 5. raw_data 컬렉션 초기화
                await _rawDataRepository.DeleteManyAsync(FilterDefinition<RawDataDocument>.Empty);
                Debug.WriteLine("raw_data 컬렉션 초기화 완료");

                Debug.WriteLine("모든 컬렉션 초기화 완료 (5개)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"컬렉션 초기화 오류: {ex.Message}");
                throw new Exception($"데이터베이스 초기화 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        /// <summary>
        /// 선택된 세션들 분석
        /// </summary>
        private async Task<SessionAnalysisResult> AnalyzeSelectedSessionsAsync(
            List<uc_MultiFileUpload.SessionDisplayData> selectedSessions)
        {
            var result = new SessionAnalysisResult();
            var sessionDataList = new List<SessionProcessingData>();

            try
            {
                foreach (var displaySession in selectedSessions)
                {
                    // MongoDB에서 실제 세션 정보 조회
                    Debug.WriteLine($"AnalyzeSelectedSessionsAsync -> displaySession.Id.ToString() :{displaySession.Id.ToString()}");
                    var sessionDoc = await _fileSessionRepository.GetByIdAsync(displaySession.Id);
                    if (sessionDoc == null)
                    {
                        result.ErrorMessage = $"세션 '{displaySession.SessionName}'을 찾을 수 없습니다.";
                        return result;
                    }

                    // 세션에 포함된 파일들 조회
                    var sessionFiles = new List<UploadedFileDocument>();
                    if (sessionDoc.FileIds != null)
                    {
                        foreach (var fileId in sessionDoc.FileIds)
                        {
                            //var file = await _uploadedFileRepository.GetByIdAsync(fileId.ToString());
                            var file = await _uploadedFileRepository.GetByIdAsync(fileId);
                            if (file != null)
                            {
                                sessionFiles.Add(file);
                            }
                        }
                    }

                    var sessionData = new SessionProcessingData
                    {
                        Session = sessionDoc,
                        Files = sessionFiles,
                        AccountName = sessionDoc.AccountName ?? "",
                        AccountColumnName = sessionDoc.AccountColumnName,
                        AmountColumnName = sessionDoc.AmountColumnName
                    };

                    sessionDataList.Add(sessionData);
                }

                result.IsValid = true;
                result.SessionData = sessionDataList;
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 분석 오류: {ex.Message}");
                result.ErrorMessage = $"세션 분석 중 오류가 발생했습니다: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 세션별 데이터를 raw_data로 처리 (병렬 처리)
        /// </summary>
        private async Task<BatchProcessingResult> ProcessSessionsToRawDataAsync(
            List<SessionProcessingData> sessions,
            UpdateProgressDelegate progressCallback)
        {
            var result = new BatchProcessingResult();
            var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
            var totalSessions = sessions.Count;
            var completedSessions = 0;

            try
            {
                // 세션별 병렬 처리
                var sessionTasks = sessions.Select(async (sessionData, index) =>
                {
                    await semaphore.WaitAsync();

                    try
                    {
                        var sessionResult = await ProcessSingleSessionAsync(sessionData);

                        // 진행률 업데이트
                        Interlocked.Increment(ref completedSessions);
                        int progress = 30 + (completedSessions * 50 / totalSessions);
                        await progressCallback(progress,
                            $"세션 처리 중... ({completedSessions}/{totalSessions}) - {sessionData.Session.SessionName}");

                        return sessionResult;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                var sessionResults = await Task.WhenAll(sessionTasks);

                // 결과 집계
                result.Success = sessionResults.All(r => r.Success);
                result.TotalRowsProcessed = sessionResults.Sum(r => r.ProcessedRows);
                result.TotalFilesProcessed = sessionResults.Sum(r => r.ProcessedFiles);
                result.SessionResults = sessionResults.ToList();

                if (!result.Success)
                {
                    var failedSessions = sessionResults.Where(r => !r.Success).ToList();
                    result.ErrorMessage = $"{failedSessions.Count}개 세션 처리 실패: " +
                                        string.Join(", ", failedSessions.Select(f => f.ErrorMessage));
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 배치 처리 오류: {ex.Message}");
                result.ErrorMessage = $"세션 처리 중 오류가 발생했습니다: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 단일 세션 처리
        /// </summary>
        private async Task<SessionResult> ProcessSingleSessionAsync(SessionProcessingData sessionData)
        {
            var result = new SessionResult
            {
                SessionName = sessionData.Session.SessionName
            };

            try
            {
                var allRawDocuments = new List<RawDataDocument>();

                // 세션 내 각 파일 처리
                foreach (var file in sessionData.Files)
                {
                    var fileDocuments = await ExtractFileDataByAccountAsync(file, sessionData);
                    allRawDocuments.AddRange(fileDocuments);
                }

                // MongoDB에 배치 삽입
                if (allRawDocuments.Count > 0)
                {
                    await InsertRawDataBatchAsync(allRawDocuments);
                }

                result.Success = true;
                result.ProcessedRows = allRawDocuments.Count;
                result.ProcessedFiles = sessionData.Files.Count;

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 처리 오류 [{sessionData.Session.SessionName}]: {ex.Message}");
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// 파일에서 특정 계정명 데이터만 추출
        /// </summary>
        /// <summary>
        /// SessionDataProcessor에서 병합된 세션 처리 (AccountName 분할 처리)
        /// </summary>
        private async Task<List<RawDataDocument>> ExtractFileDataByAccountAsync(
            UploadedFileDocument file,
            SessionProcessingData sessionData)
        {
            return await Task.Run(() =>
            {
                var documents = new List<RawDataDocument>();

                try
                {
                    string filePath = Path.Combine(UPLOAD_FOLDER, file.StoredFilename);

                    // 병합된 세션의 경우 AccountName이 쉼표로 구분되어 있음
                    string[] accountNames = sessionData.AccountName.Split(',')
                        .Select(name => name.Trim())
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToArray();

                    using (var document = SpreadsheetDocument.Open(filePath, false))
                    {
                        var workbookPart = document.WorkbookPart;
                        var worksheet = workbookPart.WorksheetParts.First().Worksheet;
                        var sheetData = worksheet.GetFirstChild<SheetData>();

                        var allRows = sheetData.Elements<Row>().ToList();
                        if (allRows.Count <= 1) return documents;

                        // 헤더 분석
                        var headerRow = allRows.First();
                        var headerCells = headerRow.Elements<Cell>().ToList();
                        var columnMapping = BuildColumnMapping(headerCells, workbookPart);

                        int accountColumnIndex = FindColumnIndex(headerCells, sessionData.AccountColumnName, workbookPart);
                        if (accountColumnIndex == -1) return documents;

                        // 데이터 행 처리 (모든 계정명에 대해 필터링)
                        for (int rowIndex = 1; rowIndex < allRows.Count; rowIndex++)
                        {
                            var row = allRows[rowIndex];
                            var cells = row.Elements<Cell>().ToList();

                            if (accountColumnIndex < cells.Count)
                            {
                                string accountValue = GetCellValue(cells[accountColumnIndex], workbookPart);

                                // 병합된 세션의 모든 계정명과 비교
                                foreach (string targetAccountName in accountNames)
                                {
                                    if (accountValue.Trim().Equals(targetAccountName.Trim(), StringComparison.OrdinalIgnoreCase))
                                    {
                                        var rawDoc = CreateRawDataDocument(cells, columnMapping, workbookPart);
                                        documents.Add(rawDoc);
                                        break; // 하나라도 매치되면 추가하고 다음 행으로
                                    }
                                }
                            }
                        }
                    }

                    return documents;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"파일 데이터 추출 오류 [{file.OriginalFilename}]: {ex.Message}");
                    return documents;
                }
            });
        }

        // 헬퍼 메서드들 (세션 제한으로 일부만 구현)
        private Dictionary<string, int> BuildColumnMapping(List<Cell> headerCells, WorkbookPart workbookPart)
        {
            var mapping = new Dictionary<string, int>();
            for (int i = 0; i < headerCells.Count; i++)
            {
                string columnName = GetCellValue(headerCells[i], workbookPart);
                if (!string.IsNullOrWhiteSpace(columnName))
                {
                    mapping[columnName.Trim()] = i;
                }
            }
            return mapping;
        }

        private int FindColumnIndex(List<Cell> headerCells, string columnName, WorkbookPart workbookPart)
        {
            for (int i = 0; i < headerCells.Count; i++)
            {
                string cellValue = GetCellValue(headerCells[i], workbookPart);
                if (cellValue.Trim().Equals(columnName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        private string GetCellValue(Cell cell, WorkbookPart workbookPart)
        {
            if (cell?.CellValue == null) return string.Empty;

            string value = cell.CellValue.InnerXml;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                var stringTablePart = workbookPart.SharedStringTablePart;
                if (stringTablePart != null)
                {
                    value = stringTablePart.SharedStringTable.ElementAt(int.Parse(value)).InnerText;
                }
            }

            return value ?? string.Empty;
        }

        /// <summary>
        /// 엑셀 행을 RawDataDocument로 변환
        /// </summary>
        private RawDataDocument CreateRawDataDocument(List<Cell> cells, Dictionary<string, int> columnMapping, WorkbookPart workbookPart)
        {
            var data = new Dictionary<string, object>();

            foreach (var mapping in columnMapping)
            {
                string columnName = mapping.Key;
                int columnIndex = mapping.Value;

                if (columnIndex < cells.Count)
                {
                    string cellValue = GetCellValue(cells[columnIndex], workbookPart);

                    // 빈 값이 아닌 경우만 추가
                    if (!string.IsNullOrWhiteSpace(cellValue))
                    {
                        // 숫자 변환 시도
                        if (decimal.TryParse(cellValue.Replace(",", ""), out decimal numericValue))
                        {
                            data[columnName] = numericValue;
                        }
                        else
                        {
                            data[columnName] = cellValue.Trim();
                        }
                    }
                }
            }

            return new RawDataDocument
            {
                Data = data,
                ImportDate = DateTime.UtcNow,
                IsHidden = false
            };
        }

        /// <summary>
        /// MongoDB에 raw_data 배치 삽입 (병렬 처리)
        /// </summary>
        private async Task InsertRawDataBatchAsync(List<RawDataDocument> documents)
        {
            const int batchSize = 10000; // 배치 크기
            var batches = documents
                .Select((doc, index) => new { doc, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.doc).ToList())
                .ToList();

            var semaphore = new SemaphoreSlim(Math.Min(5, batches.Count)); // 최대 5개 동시 처리

            var insertTasks = batches.Select(async batch =>
            {
                await semaphore.WaitAsync();

                try
                {
                    await _rawDataRepository.CreateManyAsync(batch);
                    Debug.WriteLine($"배치 삽입 완료: {batch.Count}개 문서");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"배치 삽입 오류: {ex.Message}");
                    throw;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(insertTasks);
        }

        /// <summary>
        /// 후처리 초기화 작업 (기존 btn_selectFile_Click 로직)
        /// </summary>
        private async Task ExecutePostProcessingInitializationAsync()
        {
            try
            {
                // DataHandler의 테이블 정보 갱신
                await RefreshDataHandlerAsync();

                // 기타 필요한 초기화 작업
                Debug.WriteLine("후처리 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"후처리 초기화 오류: {ex.Message}");
                throw new Exception($"후처리 작업 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        /// <summary>
        /// DataHandler 테이블 정보 갱신
        /// </summary>
        private async Task RefreshDataHandlerAsync()
        {
            try
            {
                // raw_data에서 데이터 조회하여 DataHandler 업데이트
                var rawDataCount = await _rawDataRepository.GetCountAsync();
                Debug.WriteLine($"raw_data 문서 수: {rawDataCount}");

                // raw_data의 모든 데이터를 DataTable로 변환하여 DataHandler에 설정
                var rawDataList = await _rawDataRepository.GetAllAsync();

                if (rawDataList.Count > 0)
                {
                    // MongoDB 문서들을 DataTable로 변환
                    var dataTable = ConvertRawDataToDataTable(rawDataList);

                    // 컬럼 매핑 정보 확인 및 자동 생성
                    await EnsureColumnMappingExistsAsync(dataTable);

                    // *** 컬럼 순서 정렬 적용 ***
                    var sortedDataTable = await SortColumnsBySequenceAsync(dataTable);


                    // DataHandler의 processTable 업데이트
                    // *** 핵심 수정: 두 곳 모두 설정 ***
                    //DataHandler.excelData = dataTable;      // ← 추가!
                    //DataHandler.processTable = dataTable;
                    DataHandler.excelData = sortedDataTable;      // ← 추가!
                    DataHandler.processTable = sortedDataTable;

                    Debug.WriteLine($"DataHandler.processTable 업데이트 완료: {dataTable.Rows.Count}행");
                }
                else
                {
                    // 데이터가 없는 경우 빈 테이블 설정
                    // 데이터가 없는 경우 빈 테이블 설정
                    DataHandler.excelData = new DataTable();     // ← 추가!
                    DataHandler.processTable = new DataTable();
                    Debug.WriteLine("DataHandler.processTable을 빈 테이블로 초기화");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DataHandler 갱신 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 컬럼 매핑 정보가 없는 경우 자동 생성
        /// </summary>
        private async Task EnsureColumnMappingExistsAsync(DataTable dataTable)
        {
            try
            {
                var columnMappingRepository = new ColumnMappingRepository();
                var existingMappings = await columnMappingRepository.GetAllAsync();
                var existingColumnNames = existingMappings.Select(cm => cm.OriginalName).ToHashSet();

                var newMappings = new List<ColumnMappingDocument>();
                int sequence = existingMappings.Count > 0 ? existingMappings.Max(cm => cm.Sequence) + 1 : 1;

                foreach (DataColumn column in dataTable.Columns)
                {
                    if (!existingColumnNames.Contains(column.ColumnName))
                    {
                        var mapping = new ColumnMappingDocument
                        {
                            OriginalName = column.ColumnName,
                            DisplayName = column.ColumnName,
                            DataType = GetDataTypeString(column.DataType),
                            IsVisible = true,
                            Sequence = sequence++
                        };
                        newMappings.Add(mapping);
                    }
                }

                if (newMappings.Count > 0)
                {
                    await columnMappingRepository.CreateManyAsync(newMappings);
                    Debug.WriteLine($"{newMappings.Count}개 새 컬럼 매핑 생성");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"컬럼 매핑 자동 생성 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// .NET 데이터 타입을 문자열로 변환
        /// </summary>
        private string GetDataTypeString(Type dataType)
        {
            if (dataType == typeof(string)) return "text";
            if (dataType == typeof(int) || dataType == typeof(decimal) || dataType == typeof(double)) return "number";
            if (dataType == typeof(DateTime)) return "date";
            if (dataType == typeof(bool)) return "boolean";
            return "text"; // 기본값
        }

        /// <summary>
        /// column_mapping 컬렉션의 sequence 순서에 따라 DataTable 컬럼 정렬
        /// </summary>
        private async Task<DataTable> SortColumnsBySequenceAsync(DataTable originalTable)
        {
            try
            {
                // 1. column_mapping에서 컬럼 순서 정보 조회
                var columnMappingRepository = new ColumnMappingRepository();
                var columnMappings = await columnMappingRepository.GetAllAsync();

                if (columnMappings.Count == 0)
                {
                    Debug.WriteLine("column_mapping이 없습니다. 원본 순서 유지");
                    return originalTable;
                }

                // 2. sequence 순서로 정렬된 컬럼 목록 생성
                var sortedColumns = columnMappings
                    .Where(cm => cm.IsVisible) // 보이는 컬럼만
                    .OrderBy(cm => cm.Sequence)
                    .Select(cm => cm.OriginalName)
                    .ToList();

                // 3. 원본 테이블에 없는 컬럼은 제외하고, 있는 컬럼 중 매핑되지 않은 것은 뒤에 추가
                var existingColumns = originalTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                var finalColumnOrder = new List<string>();

                // 정렬된 순서의 컬럼들 중 실제 존재하는 것들 추가
                foreach (var columnName in sortedColumns)
                {
                    if (existingColumns.Contains(columnName))
                    {
                        finalColumnOrder.Add(columnName);
                    }
                }

                // 매핑에 없지만 원본 테이블에 존재하는 컬럼들을 뒤에 추가
                var unmappedColumns = existingColumns
                    .Where(col => !finalColumnOrder.Contains(col))
                    .OrderBy(col => col);
                finalColumnOrder.AddRange(unmappedColumns);

                // 4. 새로운 DataTable 생성 (정렬된 컬럼 순서대로)
                var sortedTable = new DataTable();

                // 컬럼 추가 (정렬된 순서대로)
                foreach (var columnName in finalColumnOrder)
                {
                    var originalColumn = originalTable.Columns[columnName];
                    var newColumn = new DataColumn(columnName, originalColumn.DataType);
                    sortedTable.Columns.Add(newColumn);
                }

                // 5. 데이터 복사
                foreach (DataRow originalRow in originalTable.Rows)
                {
                    var newRow = sortedTable.NewRow();
                    foreach (var columnName in finalColumnOrder)
                    {
                        newRow[columnName] = originalRow[columnName];
                    }
                    sortedTable.Rows.Add(newRow);
                }

                Debug.WriteLine($"컬럼 정렬 완료: 총 {finalColumnOrder.Count}개 컬럼");
                //Debug.WriteLine($"정렬 순서: {string.Join(", ", finalColumnOrder.Take(20))}...");

                return sortedTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"컬럼 정렬 오류: {ex.Message}");
                Debug.WriteLine("원본 테이블 순서 유지");
                return originalTable;
            }
        }

        /// <summary>
        /// RawDataDocument 리스트를 DataTable로 변환
        /// </summary>
        private DataTable ConvertRawDataToDataTable(List<RawDataDocument> rawDataList)
        {
            var dataTable = new DataTable();

            if (rawDataList.Count == 0)
                return dataTable;

            try
            {
                // 모든 문서의 키를 수집하여 컬럼 생성
                var allKeys = new HashSet<string>();
                foreach (var doc in rawDataList)
                {
                    if (doc.Data != null)
                    {
                        foreach (var key in doc.Data.Keys)
                        {
                            allKeys.Add(key);
                        }
                    }
                }

                // ID 컬럼 추가
                dataTable.Columns.Add("_id", typeof(string));

                // 기본 컬럼들 추가
                dataTable.Columns.Add("import_date", typeof(DateTime));
                dataTable.Columns.Add("is_hidden", typeof(bool));

                // 데이터 컬럼들 추가 (동적)
                foreach (var key in allKeys.OrderBy(k => k))
                {
                    dataTable.Columns.Add(key, typeof(object));
                }

                // 데이터 행 추가
                foreach (var doc in rawDataList)
                {
                    var row = dataTable.NewRow();

                    // 기본 필드 설정
                    row["_id"] = doc.Id.ToString();
                    row["import_date"] = doc.ImportDate;
                    row["is_hidden"] = doc.IsHidden;

                    // 동적 데이터 설정
                    if (doc.Data != null)
                    {
                        foreach (var kvp in doc.Data)
                        {
                            if (dataTable.Columns.Contains(kvp.Key))
                            {
                                row[kvp.Key] = kvp.Value ?? DBNull.Value;
                            }
                        }
                    }

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DataTable 변환 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 메모리 정리
        /// </summary>
        public void Dispose()
        {
            // 리소스 정리
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    // 결과 클래스들
    public class ProcessingResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int ProcessedRowCount { get; set; }
        public int ProcessedFileCount { get; set; }
        public List<SessionResult> SessionResults { get; set; } = new List<SessionResult>();
    }

    public class SessionAnalysisResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<SessionProcessingData> SessionData { get; set; } = new List<SessionProcessingData>();
    }

    public class SessionProcessingData
    {
        public FileSessionDocument Session { get; set; }
        public List<UploadedFileDocument> Files { get; set; } = new List<UploadedFileDocument>();
        public string AccountName { get; set; }
        public string AccountColumnName { get; set; }
        public string AmountColumnName { get; set; }
    }

    public class BatchProcessingResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int TotalRowsProcessed { get; set; }
        public int TotalFilesProcessed { get; set; }
        public List<SessionResult> SessionResults { get; set; } = new List<SessionResult>();
    }

    public class SessionResult
    {
        public bool Success { get; set; } = false;
        public string SessionName { get; set; }
        public string ErrorMessage { get; set; }
        public int ProcessedRows { get; set; }
        public int ProcessedFiles { get; set; }
    }
}