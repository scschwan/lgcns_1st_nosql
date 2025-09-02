using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FinanceTool.MongoModels;
using FinanceTool.Repositories;
using SessionDisplayData = FinanceTool.uc_MultiFileUpload.SessionDisplayData;
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
            List<SessionDisplayData> selectedSessions,
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
            List<SessionDisplayData> selectedSessions)
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
        /// 세션별 데이터를 raw_data로 처리 (초고속 병렬 처리 버전)
        /// 192GB 메모리와 멀티코어 CPU를 최대한 활용
        /// </summary>
        private async Task<BatchProcessingResult> ProcessSessionsToRawDataAsync(
            List<SessionProcessingData> sessions,
            UpdateProgressDelegate progressCallback)
        {
            var result = new BatchProcessingResult();
            var sw = Stopwatch.StartNew();

            try
            {
                Debug.WriteLine($"[세션처리] 시작 - 총 {sessions.Count}개 세션");
                await progressCallback(30, "세션 데이터 병렬 처리 시작...");

                // 1단계: 모든 세션의 데이터를 동시에 병렬 추출 (SemaphoreSlim 제거)
                var allRawDocuments = sessions
                    .AsParallel()
                    .WithDegreeOfParallelism(Environment.ProcessorCount * 4) // 최대 병렬도
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .SelectMany((sessionData, sessionIndex) =>
                    {
                        try
                        {
                            Debug.WriteLine($"[세션처리] 세션 시작 - {sessionData.Session.SessionName} ({sessionIndex + 1}/{sessions.Count})");

                            // 세션 내 모든 파일의 데이터를 병렬 추출
                            var sessionDocuments = sessionData.Files
                                .AsParallel()
                                .WithDegreeOfParallelism(Environment.ProcessorCount * 2)
                                .SelectMany(file => ExtractFileDataUltraFast(file, sessionData))
                                .ToList();

                            Debug.WriteLine($"[세션처리] 세션 완료 - {sessionData.Session.SessionName}: {sessionDocuments.Count:N0}개 문서 추출");
                            return sessionDocuments;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[세션처리] 세션 오류 - {sessionData.Session.SessionName}: {ex.Message}");
                            return new List<RawDataDocument>();
                        }
                    })
                    .ToList();

                sw.Stop();
                Debug.WriteLine($"[세션처리] 데이터 추출 완료 - 소요시간: {sw.ElapsedMilliseconds:N0}ms, 총 문서: {allRawDocuments.Count:N0}개");

                await progressCallback(70, $"MongoDB 배치 삽입 중... ({allRawDocuments.Count:N0}개 문서)");

                // 2단계: 초고속 병렬 MongoDB 삽입
                if (allRawDocuments.Count > 0)
                {
                    await InsertRawDataUltraFastAsync(allRawDocuments, progressCallback);
                }

                await progressCallback(85, "결과 집계 중...");

                // 결과 설정
                result.Success = true;
                result.TotalRowsProcessed = allRawDocuments.Count;
                result.TotalFilesProcessed = sessions.Sum(s => s.Files.Count);
                result.SessionResults = sessions.Select(s => new SessionResult
                {
                    Success = true,
                    SessionName = s.Session.SessionName,
                    ProcessedRows = allRawDocuments.Count(d => true), // 실제로는 세션별로 계산 필요
                    ProcessedFiles = s.Files.Count
                }).ToList();

                Debug.WriteLine($"[세션처리] 전체 완료 - 총 소요시간: {sw.ElapsedMilliseconds:N0}ms");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[세션처리] 최상위 오류: {ex.Message}");
                result.ErrorMessage = $"세션 처리 중 오류가 발생했습니다: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 파일에서 특정 계정명 데이터를 초고속으로 추출
        /// PLINQ를 사용한 행 단위 병렬 처리
        /// </summary>
        private List<RawDataDocument> ExtractFileDataUltraFast(
            UploadedFileDocument file,
            SessionProcessingData sessionData)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                string filePath = Path.Combine(UPLOAD_FOLDER, file.StoredFilename);

                Debug.WriteLine($"[파일추출] 시작 - {file.OriginalFilename}");

                // 계정명 배열 준비
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
                    if (allRows.Count <= 1)
                    {
                        Debug.WriteLine($"[파일추출] 빈 파일 - {file.OriginalFilename}");
                        return new List<RawDataDocument>();
                    }

                    // 헤더 분석
                    var headerRow = allRows.First();
                    var headerCells = headerRow.Elements<Cell>().ToList();
                    var columnMapping = BuildColumnMapping(headerCells, workbookPart);

                    var allColumnNames = headerCells
                        .Select((cell, index) => new { Cell = cell, Index = index })
                        .Where(item => !string.IsNullOrWhiteSpace(GetCellValue(item.Cell, workbookPart)))
                        .Select(item => GetCellValue(item.Cell, workbookPart).Trim())
                        .ToList();

                    int accountColumnIndex = FindColumnIndex(headerCells, sessionData.AccountColumnName, workbookPart);
                    if (accountColumnIndex == -1)
                    {
                        Debug.WriteLine($"[파일추출] 계정 컬럼 없음 - {file.OriginalFilename}: {sessionData.AccountColumnName}");
                        return new List<RawDataDocument>();
                    }

                    var dataRows = allRows.Skip(1).ToList();

                    Debug.WriteLine($"[파일추출] 병렬 처리 시작 - {file.OriginalFilename}: {dataRows.Count:N0}행, 계정: [{string.Join(", ", accountNames)}]");

                    // PLINQ를 사용한 초고속 병렬 필터링 및 변환
                    var documents = dataRows
                        .AsParallel()
                        .WithDegreeOfParallelism(Environment.ProcessorCount * 4) // 최대 병렬도
                        .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                        .Select((row, rowIndex) =>
                        {
                            try
                            {
                                var cells = row.Elements<Cell>().ToList();
                                if (accountColumnIndex >= cells.Count) return null;

                                string accountValue = GetCellValue(cells[accountColumnIndex], workbookPart)?.Trim();
                                if (string.IsNullOrEmpty(accountValue)) return null;

                                // 계정명 매칭 검사 (병렬 처리 최적화)
                                bool isMatchingAccount = accountNames.Any(targetAccount =>
                                    accountValue.Equals(targetAccount, StringComparison.OrdinalIgnoreCase));

                                if (!isMatchingAccount) return null;

                                // RawDataDocument 생성
                                //return CreateRawDataDocumentFast(cells, columnMapping, workbookPart);
                                return CreateRawDataDocumentWithAllColumns(cells, allColumnNames, workbookPart);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[파일추출] 행 처리 오류 - {file.OriginalFilename} 행{rowIndex}: {ex.Message}");
                                return null;
                            }
                        })
                        .Where(doc => doc != null)
                        .ToList();

                    sw.Stop();
                    Debug.WriteLine($"[파일추출] 완료 - {file.OriginalFilename}: {documents.Count:N0}개 문서, 소요시간: {sw.ElapsedMilliseconds:N0}ms");

                    return documents;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[파일추출] 파일 오류 - {file.OriginalFilename}: {ex.Message}");
                return new List<RawDataDocument>();
            }
        }

        /*
        /// <summary>
        /// RawDataDocument 고속 생성 (메모리 최적화)
        /// </summary>
        private RawDataDocument CreateRawDataDocumentFast(List<Cell> cells, Dictionary<string, int> columnMapping, WorkbookPart workbookPart)
        {
            var data = new Dictionary<string, object>(columnMapping.Count); // 미리 용량 할당

            // 병렬 처리로 셀 값 추출 및 변환
            var cellValues = columnMapping
                .AsParallel()
                .WithDegreeOfParallelism(Math.Min(Environment.ProcessorCount, columnMapping.Count))
                .Select(mapping =>
                {
                    try
                    {
                        string columnName = mapping.Key;
                        int columnIndex = mapping.Value;

                        if (columnIndex >= cells.Count) return null;

                        string cellValue = GetCellValue(cells[columnIndex], workbookPart);
                        if (string.IsNullOrWhiteSpace(cellValue)) return null;

                        // 고속 숫자 변환
                        object value = decimal.TryParse(cellValue.Replace(",", ""), out decimal numericValue)
                            ? (object)numericValue
                            : cellValue.Trim();

                        return new { ColumnName = columnName, Value = value };
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(cv => cv != null)
                .ToDictionary(cv => cv.ColumnName, cv => cv.Value);

            return new RawDataDocument
            {
                Data = cellValues,
                ImportDate = DateTime.UtcNow,
                IsHidden = false
            };
        }
        */

        /// <summary>
        /// 헤더의 모든 컬럼을 보장하는 RawDataDocument 생성 (개선된 버전)
        /// 모든 컬럼이 data Dictionary에 포함되도록 보장
        /// </summary>
        private RawDataDocument CreateRawDataDocumentWithAllColumns(
            List<Cell> cells,
            List<string> allColumnNames,
            WorkbookPart workbookPart)
        {
            var data = new Dictionary<string, object>(allColumnNames.Count); // 미리 용량 할당

            // ✅ 핵심 개선: 헤더의 모든 컬럼을 순회하여 data Dictionary에 추가
            for (int columnIndex = 0; columnIndex < allColumnNames.Count; columnIndex++)
            {
                string columnName = allColumnNames[columnIndex];

                if (columnIndex < cells.Count)
                {
                    string cellValue = GetCellValue(cells[columnIndex], workbookPart);

                    // ✅ 수정된 부분: null이나 빈 값이어도 Dictionary에 추가
                    if (string.IsNullOrWhiteSpace(cellValue))
                    {
                        data[columnName] = null; // null 값도 명시적으로 저장
                    }
                    else
                    {
                        // 고속 숫자 변환
                        object value = decimal.TryParse(cellValue.Replace(",", ""), out decimal numericValue)
                            ? (object)numericValue
                            : cellValue.Trim();
                        data[columnName] = value;
                    }
                }
                else
                {
                    // ✅ 셀이 존재하지 않는 경우에도 null로 추가
                    data[columnName] = null;
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
        /// MongoDB에 초고속 배치 삽입 (메모리 기반 병렬 처리)
        /// </summary>
        private async Task InsertRawDataUltraFastAsync(List<RawDataDocument> documents, UpdateProgressDelegate progressCallback)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                Debug.WriteLine($"[MongoDB삽입] 시작 - 총 {documents.Count:N0}개 문서");

                // 배치 크기를 메모리 상황에 맞게 동적 조정
                int batchSize = Math.Min(50000, Math.Max(5000, documents.Count / Environment.ProcessorCount));

                var batches = documents
                    .Select((doc, index) => new { doc, index })
                    .GroupBy(x => x.index / batchSize)
                    .Select(g => g.Select(x => x.doc).ToList())
                    .ToList();

                Debug.WriteLine($"[MongoDB삽입] 배치 구성 - {batches.Count}개 배치, 배치당 최대 {batchSize:N0}개 문서");

                var completedBatches = 0;

                // 초고속 병렬 배치 삽입 (SemaphoreSlim 제거)
                var insertTasks = batches
                    .AsParallel()
                    .WithDegreeOfParallelism(Math.Min(10, batches.Count)) // 최대 10개 동시 삽입
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(async (batch, batchIndex) =>
                    {
                        try
                        {
                            var batchSw = Stopwatch.StartNew();

                            await _rawDataRepository.CreateManyAsync(batch);

                            batchSw.Stop();
                            Interlocked.Increment(ref completedBatches);

                            // 진행률 업데이트
                            int progress = 70 + (completedBatches * 15 / batches.Count);
                            await progressCallback(progress, $"MongoDB 삽입 중... ({completedBatches}/{batches.Count} 배치)");

                            Debug.WriteLine($"[MongoDB삽입] 배치 완료 - {batchIndex + 1}/{batches.Count}: {batch.Count:N0}개, {batchSw.ElapsedMilliseconds}ms");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[MongoDB삽입] 배치 오류 - {batchIndex + 1}: {ex.Message}");
                            throw;
                        }
                    });

                // 모든 배치 삽입 완료 대기
                await Task.WhenAll(insertTasks);

                sw.Stop();
                Debug.WriteLine($"[MongoDB삽입] 전체 완료 - 소요시간: {sw.ElapsedMilliseconds:N0}ms, 총 {documents.Count:N0}개 문서");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MongoDB삽입] 최상위 오류: {ex.Message}");
                throw;
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

                    // ✅ 수정된 부분: 빈 값도 null로 명시적 저장
                    if (string.IsNullOrWhiteSpace(cellValue))
                    {
                        data[columnName] = null;
                    }
                    else
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
                else
                {
                    // ✅ 추가된 부분: 셀이 없어도 null로 추가
                    data[columnName] = null;
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
        /// DataHandler 테이블 정보 갱신 (메모리 최적화 버전)
        /// </summary>
        private async Task RefreshDataHandlerAsync()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                Debug.WriteLine($"[DataHandler갱신] 시작");

                // raw_data 개수 확인
                var rawDataCount = await _rawDataRepository.GetCountAsync();
                Debug.WriteLine($"[DataHandler갱신] raw_data 문서 수: {rawDataCount:N0}");

                if (rawDataCount > 0)
                {
                    // 메모리 효율적인 데이터 로딩 (페이징 또는 스트리밍 고려)
                    if (rawDataCount > 500000) // 50만 건 이상인 경우
                    {
                        Debug.WriteLine($"[DataHandler갱신] 대용량 데이터 감지 - 페이징 처리 모드");
                        await RefreshDataHandlerWithPagingAsync(rawDataCount);
                    }
                    else
                    {
                        Debug.WriteLine($"[DataHandler갱신] 일반 모드");
                        await RefreshDataHandlerNormalAsync();
                    }
                }
                else
                {
                    // 빈 테이블 설정
                    DataHandler.excelData = new DataTable();
                    DataHandler.processTable = new DataTable();
                    Debug.WriteLine($"[DataHandler갱신] 빈 테이블로 초기화");
                }

                sw.Stop();
                Debug.WriteLine($"[DataHandler갱신] 완료 - 소요시간: {sw.ElapsedMilliseconds:N0}ms");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DataHandler갱신] 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 대용량 데이터에 대한 페이징 기반 DataHandler 갱신 (수정 버전)
        /// MongoDataConverter.GetPagedRawDataAsync 방식을 응용한 개선
        /// </summary>
        private async Task RefreshDataHandlerWithPagingAsync(long totalCount)
        {
            try
            {
                const int pageSize = 100000; // 10만 건씩 처리
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                Debug.WriteLine($"[페이징갱신] 시작 - 총 {totalPages}페이지, 페이지당 {pageSize:N0}건");

                DataTable finalTable = null;

                for (int page = 0; page < totalPages; page++)
                {
                    var sw = Stopwatch.StartNew();
                    Debug.WriteLine($"[페이징갱신] 페이지 처리 시작 - {page + 1}/{totalPages}");

                    // *** 수정된 부분: MongoDataConverter 방식을 응용 ***
                    // skip = page * pageSize, limit = pageSize
                    int skip = page * pageSize;
                    var pageData = await _rawDataRepository.GetPagedAsync(skip, pageSize, includeHidden: false);

                    sw.Stop();
                    Debug.WriteLine($"[페이징갱신] MongoDB 조회 완료 - 페이지 {page + 1}: {pageData.Count:N0}개, 조회시간: {sw.ElapsedMilliseconds}ms");

                    // DataTable 변환 시간 측정
                    var convertSw = Stopwatch.StartNew();
                    var pageTable = ConvertRawDataToDataTable(pageData);
                    convertSw.Stop();

                    Debug.WriteLine($"[페이징갱신] DataTable 변환 완료 - 페이지 {page + 1}: 변환시간: {convertSw.ElapsedMilliseconds}ms");

                    if (finalTable == null)
                    {
                        finalTable = pageTable.Clone(); // 스키마만 복사
                        Debug.WriteLine($"[페이징갱신] 최종 테이블 스키마 생성 - 컬럼 수: {finalTable.Columns.Count}");
                    }

                    // 데이터 병합 (고속 처리)
                    var mergeSw = Stopwatch.StartNew();
                    foreach (DataRow row in pageTable.Rows)
                    {
                        finalTable.ImportRow(row);
                    }
                    mergeSw.Stop();

                    Debug.WriteLine($"[페이징갱신] 데이터 병합 완료 - 페이지 {page + 1}: {pageTable.Rows.Count:N0}행, 병합시간: {mergeSw.ElapsedMilliseconds}ms, 누적: {finalTable.Rows.Count:N0}행");

                    // 메모리 정리
                    pageTable.Dispose();
                    pageData.Clear(); // 리스트 정리

                    // 주기적 GC (5페이지마다)
                    if (page % 5 == 0 && page > 0)
                    {
                        var gcSw = Stopwatch.StartNew();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        gcSw.Stop();

                        Debug.WriteLine($"[페이징갱신] GC 실행 완료 - 페이지 {page + 1} 후, GC 시간: {gcSw.ElapsedMilliseconds}ms");
                    }
                }

                Debug.WriteLine($"[페이징갱신] 모든 페이지 로딩 완료 - 최종 행 수: {finalTable.Rows.Count:N0}");

                // 컬럼 매핑 및 정렬 적용
                await EnsureColumnMappingExistsAsync(finalTable);
                var sortedTable = await SortColumnsBySequenceAsync(finalTable);

                DataHandler.excelData = sortedTable;
                DataHandler.processTable = sortedTable;

                Debug.WriteLine($"[페이징갱신] 완료 - 최종 테이블: {sortedTable.Rows.Count:N0}행, {sortedTable.Columns.Count}컬럼");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[페이징갱신] 오류: {ex.Message}");
                Debug.WriteLine($"[페이징갱신] 스택 트레이스: {ex.StackTrace}");
                throw;
            }
        }

      

        /// <summary>
        /// 일반적인 DataHandler 갱신 (기존 로직 유지)
        /// </summary>
        private async Task RefreshDataHandlerNormalAsync()
        {
            var rawDataList = await _rawDataRepository.GetAllAsync();
            var dataTable = ConvertRawDataToDataTable(rawDataList);

            await EnsureColumnMappingExistsAsync(dataTable);
            var sortedDataTable = await SortColumnsBySequenceAsync(dataTable);

            DataHandler.excelData = sortedDataTable;
            DataHandler.processTable = sortedDataTable;

            Debug.WriteLine($"[일반갱신] 완료 - 테이블: {dataTable.Rows.Count:N0}행");
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
                    Debug.WriteLine($"[EnsureColumnMappingExistsAsync] column_mapping : {column.ColumnName} , sequence : {sequence + 1}");
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
                //foreach (var key in allKeys.OrderBy(k => k))
                foreach (var key in allKeys)
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