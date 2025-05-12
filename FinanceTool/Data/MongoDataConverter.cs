using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FinanceTool.Data;
using FinanceTool.MongoModels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinanceTool
{
    /// <summary>
    /// 데이터 변환 및 MongoDB 저장을 담당하는 클래스
    /// </summary>
    public class MongoDataConverter
    {
        private readonly MongoDBManager _dbManager;

        public MongoDataConverter()
        {
            _dbManager = MongoDBManager.Instance;
        }

        /// <summary>
        /// Excel 데이터를 MongoDB에 저장
        /// </summary>
        public async Task<List<RawDataDocument>> ConvertExcelToMongoDBAsync(
            DataTable excelData,
            string fileName = null,
            ProcessProgressForm.UpdateProgressDelegate progressCallback = null)
        {
            bool ensureResult = await _dbManager.EnsureInitializedAsync();


            if (!ensureResult)
            {
                throw new InvalidOperationException("MongoDB가 초기화되지 않았습니다.");
            }

            if (excelData == null || excelData.Rows.Count == 0)
            {
                throw new ArgumentException("유효한 엑셀 데이터가 없습니다.");
            }

            Stopwatch sw = Stopwatch.StartNew();
            Debug.WriteLine($"Excel → MongoDB 변환 시작: {excelData.Rows.Count}행");

            // 진행 상황 업데이트
            await progressCallback?.Invoke(10, "데이터 준비 중...");

            // 컬럼 매핑 정보 저장
            await SaveColumnMappingAsync(excelData.Columns);
            await progressCallback?.Invoke(20, "컬럼 정보 저장 완료...");

            // 엑셀 데이터를 MongoDB 문서로 변환
            var rawDataDocuments = new List<RawDataDocument>();
            int rowCount = excelData.Rows.Count;

            for (int i = 0; i < rowCount; i++)
            {
                DataRow row = excelData.Rows[i];
                var document = new RawDataDocument
                {
                    ImportDate = DateTime.Now,
                    FileName = fileName,
                    Data = new Dictionary<string, object>()
                };

                foreach (DataColumn column in excelData.Columns)
                {
                    // null 값 처리
                    if (row[column] != DBNull.Value)
                    {
                        document.Data[column.ColumnName] = ConvertValueToAppropriateType(row[column]);
                    }
                }

                rawDataDocuments.Add(document);

                // 정기적으로 진행 상황 업데이트 (10행마다)
                if (i % 10 == 0 || i == rowCount - 1)
                {
                    int progressPercentage = 20 + (int)((i + 1) / (double)rowCount * 60);
                    await progressCallback?.Invoke(progressPercentage, $"데이터 변환 중... ({i + 1}/{rowCount})");
                }
            }

            // 배치 삽입으로 MongoDB에 저장
            await progressCallback?.Invoke(80, "MongoDB에 데이터 저장 중...");
            await InsertRawDataBatchAsync(rawDataDocuments);

            sw.Stop();
            Debug.WriteLine($"Excel → MongoDB 변환 완료. 소요 시간: {sw.ElapsedMilliseconds}ms, 저장된 문서 수: {rawDataDocuments.Count}");

            await progressCallback?.Invoke(100, "변환 완료");

            return rawDataDocuments;
        }

        /// <summary>
        /// 컬럼 매핑 정보를 MongoDB에 저장
        /// </summary>
        private async Task SaveColumnMappingAsync(DataColumnCollection columns)
        {
            var columnMappings = new List<ColumnMappingDocument>();
            int sequence = 0;

            foreach (DataColumn column in columns)
            {
                // 기존 매핑 확인
                var filter = Builders<ColumnMappingDocument>.Filter.Eq(c => c.OriginalName, column.ColumnName);
                var existingMapping = await _dbManager.FindDocumentAsync("column_mapping", filter);

                if (existingMapping != null)
                {
                    // 기존 매핑 업데이트
                    var update = Builders<ColumnMappingDocument>.Update
                        .Set(c => c.DataType, MapDotNetTypeToString(column.DataType))
                        .Set(c => c.Sequence, sequence);

                    await _dbManager.UpdateDocumentsAsync("column_mapping", filter, update);
                }
                else
                {
                    // 새 매핑 생성
                    var mapping = new ColumnMappingDocument
                    {
                        OriginalName = column.ColumnName,
                        DisplayName = column.ColumnName, // 초기값은 원본 이름과 동일
                        DataType = MapDotNetTypeToString(column.DataType),
                        IsVisible = true,
                        Sequence = sequence
                    };

                    columnMappings.Add(mapping);
                }

                sequence++;
            }

            // 새 매핑이 있으면 일괄 삽입
            if (columnMappings.Count > 0)
            {
                await _dbManager.InsertManyDocumentsAsync("column_mapping", columnMappings);
            }

            Debug.WriteLine($"{columns.Count}개 컬럼 매핑 정보 저장 완료");
        }

        /// <summary>
        /// .NET 데이터 타입을 문자열로 변환
        /// </summary>
        private string MapDotNetTypeToString(Type dotNetType)
        {
            if (dotNetType == typeof(int) || dotNetType == typeof(long) ||
                dotNetType == typeof(short) || dotNetType == typeof(byte))
                return "integer";
            else if (dotNetType == typeof(bool))
                return "boolean";
            else if (dotNetType == typeof(float) || dotNetType == typeof(double) ||
                     dotNetType == typeof(decimal))
                return "decimal";
            else if (dotNetType == typeof(DateTime))
                return "datetime";
            else if (dotNetType == typeof(byte[]))
                return "binary";
            else
                return "text";
        }

        /// <summary>
        /// 값을 적절한 타입으로 변환
        /// </summary>
        private object ConvertValueToAppropriateType(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            // MongoDB는 기본적으로 대부분의 .NET 타입을 지원하지만,
            // 일부 타입(예: decimal)은 명시적 변환이 필요합니다.
            if (value is decimal decimalValue)
                return Convert.ToDouble(decimalValue);

            // 날짜 처리
            if (value is DateTime dateValue)
                return dateValue;

            return value;
        }

        /// <summary>
        /// raw_data 문서를 MongoDB에 배치로 삽입
        /// </summary>
        private async Task InsertRawDataBatchAsync(List<RawDataDocument> documents)
        {
            const int batchSize = 1000; // 최적 배치 크기

            for (int i = 0; i < documents.Count; i += batchSize)
            {
                var batch = documents.Skip(i).Take(Math.Min(batchSize, documents.Count - i)).ToList();
                await _dbManager.InsertManyDocumentsAsync("raw_data", batch);
                Debug.WriteLine($"배치 {i / batchSize + 1} 삽입 완료: {batch.Count}개 문서");
            }
        }

        /// <summary>
        /// 저장된 raw_data 문서를 가져옴
        /// </summary>
        public async Task<List<RawDataDocument>> GetRawDataAsync(int limit = 1000)
        {
            var filter = Builders<RawDataDocument>.Filter.Empty;
            return await _dbManager.FindDocumentsAsync("raw_data", filter, limit);
        }

        /// <summary>
        /// 페이징 처리된 raw_data 문서 조회
        /// </summary>
        // MongoDataConveter.cs의 GetPagedRawDataAsync 메서드 수정
        public async Task<(List<RawDataDocument> Items, long TotalCount)> GetPagedRawDataAsync(
            int pageNumber, int pageSize, bool includeHidden = false)
        {
            var filterBuilder = Builders<RawDataDocument>.Filter;
            var filter = filterBuilder.Empty;

            // 파라미터 이름을 hideHidden에서 includeHidden으로 변경
            // includeHidden=false일 때는 숨겨진 문서를 제외
            // includeHidden=true일 때는 모든 문서를 포함
            if (!includeHidden)
            {
                filter = filterBuilder.Eq(d => d.IsHidden, false);
            }

            // 날짜 기준 내림차순 정렬
            var sort = Builders<RawDataDocument>.Sort.Descending(d => d.ImportDate);

            return await _dbManager.FindWithPaginationAsync("raw_data", filter, pageNumber, pageSize, sort);
        }

        /// <summary>
        /// ProcessData 준비 (선택된 필드를 MongoDB에 저장)
        /// </summary>
        // MongoDataConverter.cs에 있는 PrepareProcessDataAsync 메서드 수정
        public async Task PrepareProcessDataAsync(IEnumerable<string> selectedColumns)
        {
            bool ensureResult = await _dbManager.EnsureInitializedAsync();

            if (!ensureResult)
            {
                throw new InvalidOperationException("MongoDB가 초기화되지 않았습니다.");
            }

            // raw_data에서 숨겨지지 않은 모든 문서 조회
            var filter = Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false);
            var rawDataDocuments = await _dbManager.FindDocumentsAsync("raw_data", filter);

            // process_data 컬렉션을 비우고 다시 생성
            await _dbManager.DropCollectionAsync("process_data");

            // process_data 문서 생성 및 저장
            var processDataDocuments = new List<ProcessDataDocument>();

            foreach (var rawDoc in rawDataDocuments)
            {
                // ObjectId 형식으로 직접 설정
                // 이 부분이 중요! 문자열이 아닌 ObjectId로 설정합니다.
                var processDoc = new ProcessDataDocument
                {
                    RawDataId = rawDoc.Id, // MongoDB 문서 ID 할당
                    ImportDate = rawDoc.ImportDate,
                    ProcessedDate = DateTime.Now,
                    Data = new Dictionary<string, object>()
                };

                // 선택된 컬럼만 복사
                foreach (string column in selectedColumns)
                {
                    if (rawDoc.Data.ContainsKey(column))
                    {
                        processDoc.Data[column] = rawDoc.Data[column];
                    }
                }

                processDataDocuments.Add(processDoc);
            }

            // 배치 삽입
            if (processDataDocuments.Count > 0)
            {
                const int batchSize = 1000;
                for (int i = 0; i < processDataDocuments.Count; i += batchSize)
                {
                    var batch = processDataDocuments.Skip(i).Take(Math.Min(batchSize, processDataDocuments.Count - i)).ToList();
                    await _dbManager.InsertManyDocumentsAsync("process_data", batch);
                }
            }

            Debug.WriteLine($"process_data 준비 완료: {processDataDocuments.Count}개 문서 생성");
        }

        /// <summary>
        /// 행 숨기기
        /// </summary>
        public async Task HideDocumentAsync(string docId, string reason = null)
        {
            var filter = Builders<RawDataDocument>.Filter.Eq(d => d.Id, docId);
            var update = Builders<RawDataDocument>.Update
                .Set(d => d.IsHidden, true)
                .Set(d => d.HiddenReason, reason);

            await _dbManager.UpdateDocumentsAsync("raw_data", filter, update);
            Debug.WriteLine($"문서 숨김 처리: {docId}, 이유: {reason}");
        }

        /// <summary>
        /// 모든 행 표시 (숨김 해제)
        /// </summary>
        public async Task UnhideAllDocumentsAsync()
        {
            var filter = Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, true);
            var update = Builders<RawDataDocument>.Update
                .Set(d => d.IsHidden, false)
                .Unset(d => d.HiddenReason);

            var count = await _dbManager.UpdateDocumentsAsync("raw_data", filter, update);
            Debug.WriteLine($"모든 숨겨진 문서 표시 처리: {count}개 문서");
        }

        // MongoDataConverter.cs 클래스에 추가할 메서드
        public async Task HideDocumentsByFieldAsync(string fieldName, object fieldValue, string reason = null)
        {
            if (string.IsNullOrEmpty(fieldName) || fieldValue == null)
                return;

            // 필드 값 기준으로 문서 필터 생성
            var filter = Builders<RawDataDocument>.Filter.Eq($"Data.{fieldName}", fieldValue);

            // 숨김 상태로 업데이트
            var update = Builders<RawDataDocument>.Update
                .Set(d => d.IsHidden, true)
                .Set(d => d.HiddenReason, reason);

            // 일치하는 모든 문서 업데이트
            var result = await _dbManager.UpdateDocumentsAsync("raw_data", filter, update);
            Debug.WriteLine($"필드 {fieldName}={fieldValue} 기준으로 {result}개 문서가 숨겨짐");
        }
    }
}