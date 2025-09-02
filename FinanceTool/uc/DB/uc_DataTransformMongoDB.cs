using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTool
{
    public partial class uc_DataTransform
    {

        /// <summary>
        /// MongoDB 데이터 안전 로드 (uc_Clustering 배치 패턴)
        /// </summary>
        private async Task<Dictionary<string, Dictionary<string, object>>> LoadMongoDataSafely(HashSet<string> rawDataIds)
        {
            var mongoDataLookup = new Dictionary<string, Dictionary<string, object>>();

            try
            {
                var rawDataRepo = new Repositories.RawDataRepository();
                const int batchSize = 10000; // 안전한 배치 크기
                var idList = rawDataIds.ToList();

                // 배치별 순차 처리 (병렬 처리 제거로 안정성 확보)
                for (int i = 0; i < idList.Count; i += batchSize)
                {
                    int currentBatchSize = Math.Min(batchSize, idList.Count - i);
                    var batchIds = idList.GetRange(i, currentBatchSize);

                    try
                    {
                        var batchFilter = Builders<MongoModels.RawDataDocument>.Filter.In(d => d.Id, batchIds);
                        var batchRawDatas = await rawDataRepo.FindDocumentsAsync(batchFilter);

                        foreach (var rawData in batchRawDatas)
                        {
                            if (rawData.Data != null)
                            {
                                mongoDataLookup[rawData.Id] = rawData.Data;
                            }
                        }

                        if (i % (batchSize * 5) == 0) // 매 5번째 배치마다 로깅
                        {
                            Debug.WriteLine($"MongoDB 배치 로드 진행: {i + currentBatchSize}/{idList.Count}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"배치 {i / batchSize + 1} 로드 오류: {ex.Message}");
                        // 배치 오류 시 다음 배치 계속 진행
                    }
                }

                return mongoDataLookup;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 데이터 로드 전체 오류: {ex.Message}");
                return new Dictionary<string, Dictionary<string, object>>();
            }
        }

        /// <summary>
        /// 기존 EnrichTransformDataWithMongoData 메서드를 대체하는 호출부
        /// </summary>
        public async Task<DataTable> EnrichTransformDataWithMongoData(DataTable transformDataTable)
        {
            try
            {
                Debug.WriteLine("EnrichTransformDataWithMongoData 시작");

                // MongoDB 연결 확인
                await Data.MongoDBManager.Instance.EnsureInitializedAsync();

                // 1. 가시적 컬럼 목록 조회
                var columnMappingFilter = Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("is_visible", true);
                var columnMappingsResult = await Data.MongoDBManager.Instance.FindDocumentsAsync<MongoDB.Bson.BsonDocument>(
                    "column_mapping",
                    columnMappingFilter);

                // 시각화될 컬럼명 추출
                List<string> visibleColumns = new List<string>();
                foreach (var doc in columnMappingsResult)
                {
                    if (doc.Contains("original_name"))
                    {
                        string originalName = doc["original_name"].AsString;
                        visibleColumns.Add(originalName);
                    }
                }

                Debug.WriteLine($"시각화될 컬럼: {string.Join(", ", visibleColumns)}");

                if (visibleColumns.Count == 0)
                {
                    Debug.WriteLine("표시할 컬럼이 없습니다. 원본 테이블 복사본 반환");
                    return transformDataTable.Copy();
                }

                // 2. 안전한 결과 테이블 생성 (uc_Clustering 패턴)
                DataTable resultTable = CreateSafeResultTable(transformDataTable, visibleColumns);

                // 3. raw_data_id 수집 및 유효성 검증
                var rawDataIds = new HashSet<string>();
                var rowToIdMap = new Dictionary<int, string>();

                for (int i = 0; i < transformDataTable.Rows.Count; i++)
                {
                    DataRow row = transformDataTable.Rows[i];
                    if (row["raw_data_id"] != DBNull.Value && row["raw_data_id"] != null)
                    {
                        string rawDataId = row["raw_data_id"].ToString();
                        if (!string.IsNullOrEmpty(rawDataId))
                        {
                            rawDataIds.Add(rawDataId);
                            rowToIdMap[i] = rawDataId;
                        }
                    }
                }

                if (rawDataIds.Count == 0)
                {
                    Debug.WriteLine("유효한 raw_data_id가 없습니다.");
                    return CopyDataSafely(transformDataTable, resultTable);
                }

                Debug.WriteLine($"보강할 raw_data_id: {rawDataIds.Count}개");

                // 4. MongoDB에서 안전한 배치 조회
                var mongoDataLookup = await LoadMongoDataSafely(rawDataIds);
                Debug.WriteLine($"MongoDB 데이터 로드 완료: {mongoDataLookup.Count}개");

                // 5. 안전한 데이터 보강 (행별 순차 처리)
                await EnrichDataSafely(transformDataTable, resultTable, mongoDataLookup, visibleColumns, rowToIdMap);

                Debug.WriteLine($"EnrichTransformDataWithMongoData 완료: {resultTable.Rows.Count}행");
                return resultTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 데이터 보강 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
                // 예외 발생 시 원본 데이터 테이블의 복사본 반환
                return transformDataTable.Copy();
            }
        }

        /// <summary>
        /// 안전한 결과 테이블 생성 (uc_Clustering 패턴)
        /// </summary>
        private DataTable CreateSafeResultTable(DataTable sourceTable, List<string> visibleColumns)
        {
            DataTable resultTable = new DataTable();

            try
            {

                //1. 가시적 컬럼들 추가 (중복 제외)
                foreach (string columnName in visibleColumns)
                {
                    if (!resultTable.Columns.Contains(columnName))
                    {
                        resultTable.Columns.Add(columnName, typeof(string));
                    }
                }
                // 2. 먼저 원본 테이블의 컬럼들 추가
                foreach (DataColumn sourceColumn in sourceTable.Columns)
                {
                    Type columnType = sourceColumn.DataType;
                    // 안전성을 위해 모든 컬럼을 string 타입으로 통일
                    resultTable.Columns.Add(sourceColumn.ColumnName, typeof(string));
                }


                Debug.WriteLine($"결과 테이블 컬럼 생성 완료: {resultTable.Columns.Count}개");
                return resultTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"결과 테이블 생성 오류: {ex.Message}");
                // 오류 시 최소한의 테이블 반환
                DataTable fallbackTable = new DataTable();
                fallbackTable.Columns.Add("raw_data_id", typeof(string));
                return fallbackTable;
            }
        }

        /// <summary>
        /// 안전한 데이터 복사 (NewRow() 대신 직접 구성)
        /// </summary>
        private DataTable CopyDataSafely(DataTable sourceTable, DataTable targetTable)
        {
            try
            {
                for (int i = 0; i < sourceTable.Rows.Count; i++)
                {
                    DataRow sourceRow = sourceTable.Rows[i];

                    // 값 배열 직접 구성 (NewRow() 사용 안함)
                    object[] rowValues = new object[targetTable.Columns.Count];

                    // 각 컬럼별로 안전하게 값 설정
                    for (int j = 0; j < targetTable.Columns.Count; j++)
                    {
                        string columnName = targetTable.Columns[j].ColumnName;

                        if (sourceTable.Columns.Contains(columnName))
                        {
                            object sourceValue = sourceRow[columnName];
                            rowValues[j] = sourceValue == null || sourceValue == DBNull.Value ?
                                           string.Empty : sourceValue.ToString();
                        }
                        else
                        {
                            rowValues[j] = string.Empty;
                        }
                    }

                    // 직접 행 추가 (NewRow() 대신)
                    targetTable.Rows.Add(rowValues);
                }

                Debug.WriteLine($"안전한 데이터 복사 완료: {targetTable.Rows.Count}행");
                return targetTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"안전한 데이터 복사 오류: {ex.Message}");
                return targetTable;
            }
        }

        /// <summary>
        /// 안전한 데이터 보강 (순차 처리)
        /// </summary>
        private async Task EnrichDataSafely(
            DataTable sourceTable,
            DataTable targetTable,
            Dictionary<string, Dictionary<string, object>> mongoLookup,
            List<string> visibleColumns,
            Dictionary<int, string> rowToIdMap)
        {
            try
            {
                Debug.WriteLine($"안전한 데이터 보강 시작: {sourceTable.Rows.Count}행");

                // 순차 처리로 안정성 확보 (병렬 처리 제거)
                for (int i = 0; i < sourceTable.Rows.Count; i++)
                {
                    try
                    {
                        DataRow sourceRow = sourceTable.Rows[i];

                        // 값 배열 직접 구성
                        object[] enrichedValues = new object[targetTable.Columns.Count];

                        // 1. 원본 데이터 복사
                        for (int j = 0; j < targetTable.Columns.Count; j++)
                        {
                            string columnName = targetTable.Columns[j].ColumnName;

                            if (sourceTable.Columns.Contains(columnName))
                            {
                                object sourceValue = sourceRow[columnName];
                                enrichedValues[j] = sourceValue == null || sourceValue == DBNull.Value ?
                                                   string.Empty : sourceValue.ToString();
                            }
                            else
                            {
                                enrichedValues[j] = string.Empty;
                            }
                        }

                        // 2. MongoDB 데이터로 보강
                        if (rowToIdMap.TryGetValue(i, out string rawDataId) &&
                            mongoLookup.TryGetValue(rawDataId, out var mongoData))
                        {
                            foreach (string visibleColumn in visibleColumns)
                            {
                                if (targetTable.Columns.Contains(visibleColumn) &&
                                    mongoData.TryGetValue(visibleColumn, out object mongoValue))
                                {
                                    int columnIndex = targetTable.Columns.IndexOf(visibleColumn);
                                    if (columnIndex >= 0 && columnIndex < enrichedValues.Length)
                                    {
                                        enrichedValues[columnIndex] = mongoValue == null ?
                                                                     string.Empty : mongoValue.ToString();
                                    }
                                }
                            }
                        }

                        // 직접 행 추가
                        targetTable.Rows.Add(enrichedValues);

                        // 진행 상황 로깅
                        if (i > 0 && i % 50000 == 0)
                        {
                            Debug.WriteLine($"데이터 보강 진행: {i}/{sourceTable.Rows.Count}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"행 {i} 보강 오류: {ex.Message}");

                        // 오류 시 기본 행 추가
                        try
                        {
                            object[] fallbackValues = new object[targetTable.Columns.Count];
                            for (int k = 0; k < fallbackValues.Length; k++)
                            {
                                fallbackValues[k] = string.Empty;
                            }
                            targetTable.Rows.Add(fallbackValues);
                        }
                        catch (Exception fallbackEx)
                        {
                            Debug.WriteLine($"대체 행 추가도 실패: {fallbackEx.Message}");
                        }
                    }
                }

                Debug.WriteLine($"안전한 데이터 보강 완료: {targetTable.Rows.Count}행");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"데이터 보강 전체 오류: {ex.Message}");
            }
        }

        private bool searchYN = false;
        private async Task create_merge_keyword_list(bool progressYN = false)
        {
            try
            {
                searchYN = true;

                if (progressYN)
                {
                    using (var progressForm = new ProcessProgressForm())
                    {
                        Debug.WriteLine("create_merge_keyword_list start ");
                        progressForm.Show();
                        await progressForm.UpdateProgressHandler(10, "키워드 요약 테이블 생성 중...");

                        await ProcessMergeKeywordListWithProgress(progressForm.UpdateProgressHandler);

                        await progressForm.UpdateProgressHandler(100, "완료");
                    }
                }
                else
                {
                    // 프로그레스 없이 진행
                    await ProcessMergeKeywordListWithProgress(null);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"키워드 리스트 생성 오류: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
            finally
            {
                Debug.WriteLine($"create_merge_keyword_list complete");
                searchYN = false;
            }
        }

        // 키워드 병합 처리 함수 (개선버전)
        // 키워드 병합 처리 함수 (개선버전 - 병렬 처리 적용)
        private async Task<(ConcurrentDictionary<string, int> keywordFrequency,
                   ConcurrentDictionary<string, ConcurrentBag<string>> keywordToRawDataIds)>
    ProcessKeywordsUltraSpeed(DataTable transformDataTable, List<string> keywordColumns)
        {
            try
            {
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 극한 속도 키워드 처리 시작: {transformDataTable.Rows.Count}행");

                // 극한 병렬 설정 - CPU 코어 수의 16배 (192GB RAM 활용)
                int extremeParallelism = Environment.ProcessorCount * 16; // 16코어 * 16 = 256 스레드
                const int ultraBatchSize = 50000; // 대용량 배치

                // 결과 저장용 스레드 안전 컬렉션
                var keywordFrequency = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var keywordToRawDataIds = new ConcurrentDictionary<string, ConcurrentBag<string>>(StringComparer.OrdinalIgnoreCase);

                // 1단계: 데이터를 메모리에 최적화하여 로드 (극한 메모리 사용)
                var rowDataCache = new UltraSpeedRowData[transformDataTable.Rows.Count];

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 메모리 캐싱 시작...");

                // 메모리 캐싱을 병렬로 수행 (극한 속도)
                await Task.Run(() =>
                {
                    Parallel.For(0, transformDataTable.Rows.Count,
                        new ParallelOptions { MaxDegreeOfParallelism = extremeParallelism },
                        i =>
                        {
                            try
                            {
                                var row = transformDataTable.Rows[i];
                                string rawDataId = row["raw_data_id"]?.ToString();

                                if (!string.IsNullOrEmpty(rawDataId))
                                {
                                    var keywords = new string[keywordColumns.Count];
                                    for (int j = 0; j < keywordColumns.Count; j++)
                                    {
                                        keywords[j] = row[keywordColumns[j]]?.ToString()?.Trim();
                                    }

                                    rowDataCache[i] = new UltraSpeedRowData
                                    {
                                        RawDataId = rawDataId,
                                        Keywords = keywords
                                    };
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 행 {i} 캐싱 오류: {ex.Message}");
                            }
                        });
                });

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 메모리 캐싱 완료");

                // 2단계: 배치별 극한 병렬 처리
                var batches = new List<UltraSpeedRowData[]>();

                for (int i = 0; i < rowDataCache.Length; i += ultraBatchSize)
                {
                    int batchSize = Math.Min(ultraBatchSize, rowDataCache.Length - i);
                    var batch = new UltraSpeedRowData[batchSize];
                    Array.Copy(rowDataCache, i, batch, 0, batchSize);
                    batches.Add(batch);
                }

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 극한 병렬 키워드 처리 시작: {batches.Count}개 배치");

                // 극한 병렬 배치 처리
                await Task.Run(() =>
                {
                    Parallel.ForEach(batches,
                        new ParallelOptions { MaxDegreeOfParallelism = extremeParallelism },
                        batch =>
                        {
                            try
                            {
                                // 배치별 로컬 결과 (메모리 효율성)
                                var localKeywordFreq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                                var localKeywordToIds = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

                                // 배치 내 데이터 처리 (극한 속도)
                                foreach (var rowData in batch)
                                {
                                    if (rowData.RawDataId == null) continue;

                                    foreach (var keyword in rowData.Keywords)
                                    {
                                        if (string.IsNullOrWhiteSpace(keyword)) continue;

                                        // 로컬 집계
                                        if (!localKeywordFreq.ContainsKey(keyword))
                                        {
                                            localKeywordFreq[keyword] = 0;
                                            localKeywordToIds[keyword] = new HashSet<string>();
                                        }

                                        localKeywordFreq[keyword]++;
                                        localKeywordToIds[keyword].Add(rowData.RawDataId);
                                    }
                                }

                                // 글로벌 결과에 병합 (스레드 안전)
                                foreach (var kvp in localKeywordFreq)
                                {
                                    keywordFrequency.AddOrUpdate(kvp.Key, kvp.Value, (k, v) => v + kvp.Value);
                                }

                                foreach (var kvp in localKeywordToIds)
                                {
                                    keywordToRawDataIds.AddOrUpdate(
                                        kvp.Key,
                                        new ConcurrentBag<string>(kvp.Value),
                                        (k, existingBag) =>
                                        {
                                            foreach (var id in kvp.Value)
                                            {
                                                existingBag.Add(id);
                                            }
                                            return existingBag;
                                        }
                                    );
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 배치 처리 오류: {ex.Message}");
                            }
                        });
                });

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 극한 속도 키워드 처리 완료: {keywordFrequency.Count}개 고유 키워드");

                return (keywordFrequency, keywordToRawDataIds);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 극한 속도 키워드 처리 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 극한 속도용 행 데이터 구조체 (메모리 효율성)
        /// </summary>
        private struct UltraSpeedRowData
        {
            public string RawDataId { get; set; }
            public string[] Keywords { get; set; }
        }

        /// <summary>
        /// ProcessMergeKeywordListWithProgress에서 기존 키워드 추출 부분을 이것으로 교체
        /// </summary>
        private async Task ProcessMergeKeywordListWithProgress(ProcessProgressForm.UpdateProgressDelegate progress)
        {
            try
            {
                // 진행 상황 업데이트 래퍼 함수
                async Task UpdateProgress(int percentage, string message = null)
                {
                    if (progress != null)
                    {
                        await progress(percentage, message);
                    }
                }

                await UpdateProgress(15, "키워드 데이터 로딩 중...");

                // 1. 키워드 데이터 확인
                if (transformDataTable == null || transformDataTable.Rows.Count == 0)
                {
                    Debug.WriteLine("데이터 테이블이 비어 있습니다.");
                    return;
                }

                // 2. 키워드 컬럼 식별 (Column0부터 시작하는 컬럼들)
                List<string> keywordColumns = new List<string>();
                foreach (DataColumn column in transformDataTable.Columns)
                {
                    if (column.ColumnName.StartsWith("Column") &&
                        int.TryParse(column.ColumnName.Substring(6), out int colIndex) &&
                        colIndex >= 0)
                    {
                        keywordColumns.Add(column.ColumnName);
                    }
                }

                Debug.WriteLine($"키워드 컬럼: {string.Join(", ", keywordColumns)}");

                if (keywordColumns.Count == 0)
                {
                    Debug.WriteLine("키워드 컬럼을 찾을 수 없습니다.");
                    return;
                }

                await UpdateProgress(20, "키워드 추출 중...");

                // 3. 극한 속도 키워드 처리 (기존 Parallel.ForEach 대체)
                var (keywordFrequency, keywordToRawDataIds) = await ProcessKeywordsUltraSpeed(transformDataTable, keywordColumns);

                await UpdateProgress(40, $"키워드별 금액 합산 중... ({keywordFrequency.Count}개 키워드)");

                // 4. 금액 정보를 극한 속도로 처리
                var rawDataToMoney = new ConcurrentDictionary<string, decimal>();

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 극한 속도 금액 정보 로드 시작: {DataHandler.moneyDataTable.Rows.Count}개 행");

                if (DataHandler.moneyDataTable != null && DataHandler.moneyDataTable.Rows.Count > 0)
                {


                    // 금액 데이터를 극한 병렬 처리로 로드
                    await Task.Run(() =>
                    {
                        int extremeParallelism = Environment.ProcessorCount * 16;

                        Parallel.ForEach(DataHandler.moneyDataTable.AsEnumerable(),
                            new ParallelOptions { MaxDegreeOfParallelism = extremeParallelism },
                            moneyRow =>
                            {
                                try
                                {
                                    // raw_data_id 확인
                                    if (moneyRow.Table.Columns.Contains("raw_data_id") && moneyRow["raw_data_id"] != DBNull.Value)
                                    {
                                        string rawDataId = moneyRow["raw_data_id"].ToString();
                                        if (!string.IsNullOrEmpty(rawDataId))
                                        {
                                            // 금액 값 추출 (기존 로직 유지)
                                            object moneyValue = null;

                                            if (moneyRow.Table.Columns.Count > 1)
                                            {
                                                if (moneyRow.Table.Columns[0].ColumnName != "raw_data_id")
                                                {
                                                    moneyValue = moneyRow[0];
                                                }
                                                else if (moneyRow.Table.Columns.Count > 1)
                                                {
                                                    moneyValue = moneyRow[1];
                                                }
                                            }

                                            if (moneyValue == null || moneyValue == DBNull.Value)
                                            {
                                                string moneyColumnName = DataHandler.levelName[0];
                                                if (moneyRow.Table.Columns.Contains(moneyColumnName))
                                                {
                                                    moneyValue = moneyRow[moneyColumnName];
                                                }
                                            }

                                            if (moneyValue != null && moneyValue != DBNull.Value)
                                            {
                                                if (decimal.TryParse(moneyValue.ToString(), out decimal amount))
                                                {
                                                    rawDataToMoney.TryAdd(rawDataId, amount);
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 금액 처리 중 오류: {ex.Message}");
                                }
                            }
                        );
                    });


                }

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 금액 정보 로드 완료: {rawDataToMoney.Count}개");

                await UpdateProgress(60, "키워드별 금액 합산 중...");

                // 5. 키워드별 금액 합산 (극한 병렬 처리)
                var keywordTotalMoney = new ConcurrentDictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

                await Task.Run(() =>
                {
                    int extremeParallelism = Environment.ProcessorCount * 16;

                    Parallel.ForEach(keywordToRawDataIds,
                        new ParallelOptions { MaxDegreeOfParallelism = extremeParallelism },
                        pair =>
                        {
                            try
                            {
                                string keyword = pair.Key;
                                var rawDataIds = pair.Value.Distinct().ToList(); // 중복 제거

                                decimal totalAmount = 0;
                                foreach (string rawDataId in rawDataIds)
                                {
                                    if (rawDataToMoney.TryGetValue(rawDataId, out decimal amount))
                                    {
                                        totalAmount += amount;
                                    }
                                }

                                keywordTotalMoney.TryAdd(keyword, totalAmount);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 키워드별 금액 합산 중 오류: {ex.Message}");
                            }
                        }
                    );
                });

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 키워드별 금액 합산 완료: {keywordTotalMoney.Count}개");

                await UpdateProgress(80, "요약 데이터 생성 중...");

                // 6. 나머지 로직은 기존과 동일 (결과 DataTable 생성 등)
                modifiedDataTable = new DataTable();
                modifiedDataTable.Columns.Add("Value", typeof(string));
                modifiedDataTable.Columns.Add("Count", typeof(int));
                modifiedDataTable.Columns.Add("합산금액", typeof(string));

                // 키워드 빈도 기준으로 정렬 (내림차순)
                var sortedKeywords = keywordFrequency.OrderByDescending(pair => pair.Value)
                                                    .ThenBy(pair => pair.Key);

                foreach (var pair in sortedKeywords)
                {
                    string keyword = pair.Key;
                    int count = pair.Value;
                    decimal totalMoney = keywordTotalMoney.TryGetValue(keyword, out decimal money) ? money : 0;

                    // 금액 포맷팅
                    string formattedMoney = FormatToKoreanUnit(totalMoney);

                    modifiedDataTable.Rows.Add(keyword, count, formattedMoney);
                }

                await UpdateProgress(90, "UI 업데이트 중...");

                // 7. UI 업데이트 (기존 로직과 동일)
                await Task.Run(() =>
                {
                    if (Application.OpenForms.Count > 0)
                    {
                        Application.OpenForms[0].Invoke((MethodInvoker)delegate
                        {
                            if (sum_keyword_table.Rows.Count > 0)
                            {
                                sum_keyword_table.Rows.Clear();
                                sum_keyword_table.Columns.Clear();
                            }

                            // 원본 DataTable의 컬럼들 추가
                            foreach (DataColumn col in modifiedDataTable.Columns)
                            {
                                sum_keyword_table.Columns.Add(col.ColumnName, col.ColumnName);
                            }

                            // 데이터 추가
                            foreach (DataRow row in modifiedDataTable.Rows)
                            {
                                int rowIndex = sum_keyword_table.Rows.Add();

                                // 데이터 채우기
                                for (int i = 0; i < modifiedDataTable.Columns.Count; i++)
                                {
                                    sum_keyword_table.Rows[rowIndex].Cells[i].Value = row[i];
                                }
                            }

                            // DataGridView 속성 설정
                            sum_keyword_table.AllowUserToAddRows = false;
                            sum_keyword_table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                            sum_keyword_table.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                            sum_keyword_table.Font = new System.Drawing.Font("Pretendard", 14.25F);

                            // Count 컬럼(1번 인덱스)에 천 단위 콤마 포맷팅 적용
                            if (sum_keyword_table.Columns.Count > 1)
                            {
                                sum_keyword_table.Columns[1].DefaultCellStyle.Format = "N0";
                            }

                            // 나머지 컬럼들은 읽기 전용으로 설정
                            for (int i = 1; i < sum_keyword_table.Columns.Count; i++)
                            {
                                sum_keyword_table.Columns[i].ReadOnly = true;
                            }
                        });
                    }
                });

                await UpdateProgress(100, "완료된 결과: " + modifiedDataTable.Rows.Count + "개 키워드");
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 극한 속도 키워드 요약 테이블 생성 완료: {modifiedDataTable.Rows.Count}개 키워드");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"키워드 분석 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }


        public async Task<DataTable> EnrichTransformDataWithRawData(DataTable transformDataTable)
        {
            try
            {
                // 원본 데이터를 수정하지 않도록 복사본 생성
                DataTable resultTable = new DataTable();

                // MongoDB 연결 확인
                await Data.MongoDBManager.Instance.EnsureInitializedAsync();

                // 1. is_visible=true인 컬럼 목록 가져오기
                var columnMappingFilter = Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("is_visible", true);
                var columnMappingsResult = await Data.MongoDBManager.Instance.FindDocumentsAsync<MongoDB.Bson.BsonDocument>(
                    "column_mapping",
                    columnMappingFilter);

                // 시각화될 컬럼명 추출
                List<string> visibleColumns = new List<string>();
                foreach (var doc in columnMappingsResult)
                {
                    if (doc.Contains("original_name"))
                    {
                        string originalName = doc["original_name"].AsString;
                        visibleColumns.Add(originalName);
                    }
                }

                Debug.WriteLine($"시각화될 컬럼: {string.Join(", ", visibleColumns)}");

                if (visibleColumns.Count == 0)
                {
                    Debug.WriteLine("표시할 컬럼이 없습니다. column_mapping 컬렉션의 is_visible 속성을 확인하세요.");
                    return transformDataTable.Copy();
                }

                // 2. 결과 테이블에 컬럼 구성
                // 먼저 visibleColumns 추가
                foreach (string column in visibleColumns)
                {
                    resultTable.Columns.Add(column, typeof(string));
                }

                // 그 다음 원본 transformDataTable의 컬럼 추가 (중복 제외)
                foreach (DataColumn column in transformDataTable.Columns)
                {
                    if (!resultTable.Columns.Contains(column.ColumnName))
                    {
                        resultTable.Columns.Add(column.ColumnName, column.DataType);
                    }
                }

                // 3. 원본 데이터의 모든 행 복사
                foreach (DataRow originalRow in transformDataTable.Rows)
                {
                    DataRow newRow = resultTable.NewRow();

                    // 원본 테이블의 모든 컬럼 값을 새 행에 복사
                    foreach (DataColumn column in transformDataTable.Columns)
                    {
                        if (resultTable.Columns.Contains(column.ColumnName))
                        {
                            newRow[column.ColumnName] = originalRow[column.ColumnName];
                        }
                    }

                    resultTable.Rows.Add(newRow);
                }

                // 4. raw_data_id 컬럼이 있는지 확인
                if (!resultTable.Columns.Contains("raw_data_id"))
                {
                    Debug.WriteLine("transformDataTable에 raw_data_id 컬럼이 없습니다.");
                    return resultTable;
                }

                // 5. RawData 저장소 생성
                var rawDataRepo = new Repositories.RawDataRepository();

                // 6. 모든 행의 raw_data_id 목록 수집
                HashSet<string> rawDataIds = new HashSet<string>();
                Dictionary<string, List<DataRow>> idToRowsMap = new Dictionary<string, List<DataRow>>();

                foreach (DataRow row in resultTable.Rows)
                {
                    if (row["raw_data_id"] != DBNull.Value)
                    {
                        string rawDataId = row["raw_data_id"].ToString();
                        if (!string.IsNullOrEmpty(rawDataId))
                        {
                            rawDataIds.Add(rawDataId);

                            if (!idToRowsMap.ContainsKey(rawDataId))
                            {
                                idToRowsMap[rawDataId] = new List<DataRow>();
                            }
                            idToRowsMap[rawDataId].Add(row);
                        }
                    }
                }

                if (rawDataIds.Count == 0)
                {
                    Debug.WriteLine("유효한 raw_data_id가 없습니다.");
                    return resultTable;
                }

                Debug.WriteLine($"보강할 raw_data_id: {rawDataIds.Count}개");

                // 7. 배치 처리로 원본 데이터 가져오기
                const int batchSize = 10000;
                List<string> idList = rawDataIds.ToList();

                // 안전한 배치 처리
                for (int i = 0; i < idList.Count; i += batchSize)
                {
                    int currentBatchSize = Math.Min(batchSize, idList.Count - i);
                    if (i >= idList.Count || currentBatchSize <= 0)
                        continue;

                    List<string> batchIds = idList.GetRange(i, currentBatchSize);

                    // MongoDB ID 형식으로 필터 생성
                    var batchFilter = Builders<MongoModels.RawDataDocument>.Filter.In(d => d.Id, batchIds);
                    var batchRawDatas = await rawDataRepo.FindDocumentsAsync(batchFilter);

                    // 조회된 데이터를 매핑
                    foreach (var rawData in batchRawDatas)
                    {
                        string id = rawData.Id;

                        if (idToRowsMap.ContainsKey(id) && rawData.Data != null)
                        {
                            foreach (DataRow resultRow in idToRowsMap[id])
                            {
                                foreach (string column in visibleColumns)
                                {
                                    if (rawData.Data.ContainsKey(column) && resultTable.Columns.Contains(column))
                                    {
                                        resultRow[column] = rawData.Data[column]?.ToString() ?? string.Empty;
                                    }
                                }
                            }
                        }
                    }
                }

                Debug.WriteLine("EnrichTransformDataWithRawData 완료");
                return resultTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 데이터 보강 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
                // 예외 발생 시 원본 데이터 테이블의 복사본 반환
                return transformDataTable.Copy();
            }
        }

        public DataTable FilterTransformDataByKeyword(DataTable viewTransformDataTable, DataTable originalTransformDataTable, string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
                return viewTransformDataTable.Copy();

            DataTable resultTable = viewTransformDataTable.Clone();

            // 원본 transformDataTable의 컬럼명 목록 가져오기
            List<string> originalColumnNames = new List<string>();
            foreach (DataColumn col in originalTransformDataTable.Columns)
            {
                originalColumnNames.Add(col.ColumnName);
            }
            Debug.WriteLine($"originalColumnNames  : {string.Join(',', originalColumnNames)}");

            // viewTransformDataTable의 각 행에 대해 검색
            for (int rowIndex = 0; rowIndex < viewTransformDataTable.Rows.Count; rowIndex++)
            {
                DataRow row = viewTransformDataTable.Rows[rowIndex];
                bool containsKeyword = false;

                // 원본 컬럼명에 해당하는 컬럼만 검사
                foreach (string colName in originalColumnNames)
                {
                    if (viewTransformDataTable.Columns.Contains(colName) &&
                        row[colName] != null &&
                        row[colName] != DBNull.Value)
                    {
                        string cellValue = row[colName].ToString();

                        if (cellValue.Equals(keyword, StringComparison.Ordinal))
                        {
                            containsKeyword = true;
                            break;
                        }
                    }
                }

                if (containsKeyword)
                {
                    resultTable.Rows.Add(row.ItemArray);
                }
            }

            return resultTable;
        }

    }
}
