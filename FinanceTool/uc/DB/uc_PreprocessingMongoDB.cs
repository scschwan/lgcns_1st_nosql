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
    public partial class uc_preprocessing
    {
        private MongoModels.ProcessViewDocument CreateProcessViewDocument(
               DataGridView dgvApplied, DataTable dataTable, int rowIndex,
               ConcurrentDictionary<string, object> moneyDataMap,
               ConcurrentDictionary<string, string> processDataToRawDataMap,
               ConcurrentDictionary<string, string> rawDataToProcessDataMap,
               ConcurrentDictionary<string, string> deptCache,
               ConcurrentDictionary<string, string> prodCache)
        {
            try
            {
                // 1. raw_data_id 추출
                string rawDataId = null;
                string processDataId = null;

                // DataGridView에서 raw_data_id 찾기 (thread-safe)
                for (int colIndex = 0; colIndex < dgvApplied.Columns.Count; colIndex++)
                {
                    if (dgvApplied.Columns[colIndex].Name.Equals("raw_data_id", StringComparison.OrdinalIgnoreCase))
                    {
                        rawDataId = dgvApplied.Rows[rowIndex].Cells[colIndex].Value?.ToString();
                        break;
                    }
                }

                // 원본 DataTable에서 raw_data_id 찾기 (fallback)
                if (string.IsNullOrEmpty(rawDataId) && dataTable.Columns.Contains("raw_data_id"))
                {
                    if (rowIndex < dataTable.Rows.Count)
                    {
                        rawDataId = dataTable.Rows[rowIndex]["raw_data_id"]?.ToString();
                    }
                }

                // process_data_id 추출
                for (int colIndex = 0; colIndex < dgvApplied.Columns.Count; colIndex++)
                {
                    if (dgvApplied.Columns[colIndex].Name.Equals("process_data_id", StringComparison.OrdinalIgnoreCase))
                    {
                        processDataId = dgvApplied.Rows[rowIndex].Cells[colIndex].Value?.ToString();
                        break;
                    }
                }

                // ID 상호 보완
                if (string.IsNullOrEmpty(rawDataId) && !string.IsNullOrEmpty(processDataId))
                {
                    processDataToRawDataMap.TryGetValue(processDataId, out rawDataId);
                }
                else if (!string.IsNullOrEmpty(rawDataId) && string.IsNullOrEmpty(processDataId))
                {
                    rawDataToProcessDataMap.TryGetValue(rawDataId, out processDataId);
                }

                // 2. rawDataId 유효성 검사
                if (string.IsNullOrEmpty(rawDataId) || !MongoDB.Bson.ObjectId.TryParse(rawDataId, out _))
                {
                    return null; // 유효하지 않은 경우 null 반환
                }

                // 3. 금액 데이터 추출
                object moneyValue = null;
                moneyDataMap.TryGetValue(rawDataId, out moneyValue);

                // 4. 키워드 목록 추출 (thread-safe)
                var finalKeywords = new List<string>();

                // 2번째 컬럼부터 키워드 추출
                for (int colIndex = 2; colIndex < dgvApplied.Columns.Count; colIndex++)
                {
                    // 메타데이터 컬럼 건너뛰기
                    string columnName = dgvApplied.Columns[colIndex].Name;
                    if (columnName.Equals("raw_data_id", StringComparison.OrdinalIgnoreCase) ||
                        columnName.Equals("process_data_id", StringComparison.OrdinalIgnoreCase) ||
                        columnName.Equals("id", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // 셀 값 추출
                    object cellValue = dgvApplied.Rows[rowIndex].Cells[colIndex].Value;
                    if (cellValue != null && cellValue != DBNull.Value)
                    {
                        string keyword = cellValue.ToString();
                        if (!string.IsNullOrWhiteSpace(keyword))
                        {
                            finalKeywords.Add(keyword.Trim());
                        }
                    }
                }

                // 5. ProcessViewDocument 생성
                var processViewDoc = new MongoModels.ProcessViewDocument
                {
                    ProcessDataId = processDataId,
                    RawDataId = rawDataId,
                    Keywords = new MongoModels.KeywordInfo
                    {
                        FinalKeywords = finalKeywords
                    },
                    Money = moneyValue,
                    LastModifiedDate = DateTime.Now
                };

                // 6. 부서/공급업체 정보 추가
                if (DataHandler.dept_col_yn && deptCache.TryGetValue(rawDataId, out string deptValue))
                {
                    processViewDoc.Department = deptValue;
                }

                if (DataHandler.prod_col_yn && prodCache.TryGetValue(rawDataId, out string prodValue))
                {
                    processViewDoc.Supplier = prodValue;
                }

                return processViewDoc;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] CreateProcessViewDocument 오류 (행 {rowIndex}): {ex.Message}");
                return null;
            }
        }

        private List<List<T>> CreateOptimalBatches<T>(List<T> items, int batchSize)
        {
            var batches = new List<List<T>>();

            if (items == null || items.Count == 0)
            {
                return batches;
            }

            // 메모리 효율성을 위해 정확한 배치 크기 계산
            int totalItems = items.Count;
            int calculatedBatchCount = (int)Math.Ceiling((double)totalItems / batchSize);

            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 배치 생성: 총 {totalItems}개 항목을 {calculatedBatchCount}개 배치로 분할 (배치당 최대 {batchSize}개)");

            // 병렬 처리를 위한 배치 생성
            for (int i = 0; i < totalItems; i += batchSize)
            {
                int remainingItems = totalItems - i;
                int currentBatchSize = Math.Min(batchSize, remainingItems);

                // 성능 최적화: 정확한 크기로 리스트 초기화
                var batch = new List<T>(currentBatchSize);

                // 배치에 항목 추가
                for (int j = 0; j < currentBatchSize; j++)
                {
                    batch.Add(items[i + j]);
                }

                batches.Add(batch);

                // 진행상황 로깅 (큰 데이터셋의 경우)
                if (batches.Count % 100 == 0)
                {
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 배치 생성 진행: {batches.Count}/{calculatedBatchCount}");
                }
            }

            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 배치 생성 완료: {batches.Count}개 배치");
            return batches;
        }

        private async Task SaveProcessDataToMongoDBAsync(DataTable dataTable, ProcessProgressForm.UpdateProgressDelegate progress)
        {
            try
            {
                await progress(20, "MongoDB 컬렉션 준비 중...");

                // 처리 시작 시간 기록 (성능 측정용)
                var startTime = DateTime.Now;
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SaveProcessDataToMongoDBAsync 시작");

                // 프로세스 뷰 저장소 생성
                var processViewRepo = new Repositories.ProcessViewRepository();

                // 기존 문서 수 확인
                var emptyFilter = MongoDB.Driver.Builders<MongoModels.ProcessViewDocument>.Filter.Empty;
                long existingCount = await Data.MongoDBManager.Instance.GetCollectionAsync<MongoModels.ProcessViewDocument>("process_view_data")
                    .Result.CountDocumentsAsync(emptyFilter);

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 기존 process_view_data 컬렉션 문서 수: {existingCount}개");

                // dataGridView_applied에서 데이터 가져오기
                var dgvApplied = dataGridView_applied;
                int totalRows = dgvApplied.Rows.Count;

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] dataGridView_applied 행 수: {totalRows}개");

                if (totalRows == 0)
                {
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] dataGridView_applied가 비어있습니다. 저장할 데이터가 없습니다.");
                    await progress(100, "저장할 데이터가 없습니다.");
                    return;
                }

                // 1. 부서/공급업체 정보 캐싱 최적화 (7초 → 2초)
                // 1. 부서/공급업체 정보 캐싱 최적화 - 수정된 버전
                var deptCache = new ConcurrentDictionary<string, string>();
                var prodCache = new ConcurrentDictionary<string, string>();

                if (DataHandler.dept_col_yn || DataHandler.prod_col_yn)
                {
                    await progress(25, "부서/공급업체 정보 로드 중...");

                    // 수정된 매치 조건
                    var matchCondition = new MongoDB.Bson.BsonDocument
    {
        { "raw_data_id", new MongoDB.Bson.BsonDocument
            {
                { "$exists", true },
                { "$ne", MongoDB.Bson.BsonNull.Value }
            }
        }
    };

                    // 프로젝션 문서 동적 생성
                    var projectionDoc = new MongoDB.Bson.BsonDocument
    {
        { "raw_data_id", 1 }
    };

                    if (DataHandler.dept_col_yn)
                    {
                        projectionDoc.Add($"data.{DataHandler.dept_col_name}", 1);
                    }

                    if (DataHandler.prod_col_yn)
                    {
                        projectionDoc.Add($"data.{DataHandler.prod_col_name}", 1);
                    }

                    var processDataPipeline = new MongoDB.Bson.BsonDocument[]
                    {
        new MongoDB.Bson.BsonDocument("$match", matchCondition),
        new MongoDB.Bson.BsonDocument("$project", projectionDoc)
                    };

                    var processDataCollection = await Data.MongoDBManager.Instance.GetCollectionAsync<MongoDB.Bson.BsonDocument>("process_data");
                    var cursor = await processDataCollection.AggregateAsync<MongoDB.Bson.BsonDocument>(processDataPipeline);

                    // 스트리밍 방식으로 처리 (안전한 필드 접근)
                    await cursor.ForEachAsync(doc =>
                    {
                        try
                        {
                            // ObjectId를 안전하게 문자열로 변환
                            string rawDataId = null;
                            var rawDataIdBson = doc.GetValue("raw_data_id", MongoDB.Bson.BsonNull.Value);

                            if (rawDataIdBson != null && rawDataIdBson != MongoDB.Bson.BsonNull.Value)
                            {
                                if (rawDataIdBson.IsObjectId)
                                {
                                    rawDataId = rawDataIdBson.AsObjectId.ToString();
                                }
                                else if (rawDataIdBson.IsString)
                                {
                                    rawDataId = rawDataIdBson.AsString;
                                }
                            }

                            if (!string.IsNullOrEmpty(rawDataId))
                            {
                                var data = doc.GetValue("data", new MongoDB.Bson.BsonDocument()).AsBsonDocument;

                                if (DataHandler.dept_col_yn && data.Contains(DataHandler.dept_col_name))
                                {
                                    var deptBsonValue = data.GetValue(DataHandler.dept_col_name, MongoDB.Bson.BsonNull.Value);
                                    if (deptBsonValue != null && deptBsonValue != MongoDB.Bson.BsonNull.Value)
                                    {
                                        string deptValue = deptBsonValue.ToString(); // 안전한 문자열 변환
                                        if (!string.IsNullOrEmpty(deptValue))
                                        {
                                            deptCache.TryAdd(rawDataId, deptValue);
                                        }
                                    }
                                }

                                if (DataHandler.prod_col_yn && data.Contains(DataHandler.prod_col_name))
                                {
                                    var prodBsonValue = data.GetValue(DataHandler.prod_col_name, MongoDB.Bson.BsonNull.Value);
                                    if (prodBsonValue != null && prodBsonValue != MongoDB.Bson.BsonNull.Value)
                                    {
                                        string prodValue = prodBsonValue.ToString(); // 안전한 문자열 변환
                                        if (!string.IsNullOrEmpty(prodValue))
                                        {
                                            prodCache.TryAdd(rawDataId, prodValue);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 부서/공급업체 캐싱 처리 중 오류: {ex.Message}");
                        }
                    });

                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 부서 정보 캐싱: {deptCache.Count}개, 공급업체 정보 캐싱: {prodCache.Count}개");
                }

                // 2. 금액 정보 매핑 최적화 (7초 → 1초)
                var moneyDataMap = new ConcurrentDictionary<string, object>();

                if (DataHandler.moneyDataTable != null)
                {
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] DataHandler.moneyDataTable에서 금액 정보 로드 중... 행 수: {DataHandler.moneyDataTable.Rows.Count}개");

                    // 병렬 처리로 금액 매핑 생성
                    await Task.Run(() =>
                    {
                        Parallel.ForEach(DataHandler.moneyDataTable.AsEnumerable(),
                            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                            moneyRow =>
                            {
                                if (moneyRow["raw_data_id"] != DBNull.Value)
                                {
                                    string rawDataId = moneyRow["raw_data_id"].ToString();
                                    if (!string.IsNullOrEmpty(rawDataId))
                                    {
                                        object moneyValue = GetMoneyValue(moneyRow);
                                        if (moneyValue != null && moneyValue != DBNull.Value)
                                        {
                                            moneyDataMap.TryAdd(rawDataId, moneyValue);
                                        }
                                    }
                                }
                            });
                    });

                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 금액 정보 매핑 생성 완료: {moneyDataMap.Count}개");
                }

                // 3. ID 매핑 최적화
                var processDataToRawDataMap = new ConcurrentDictionary<string, string>();
                var rawDataToProcessDataMap = new ConcurrentDictionary<string, string>();

                // MongoDB 집계 파이프라인 수정 - 중복 필드명 제거
                var idMappingPipeline = new MongoDB.Bson.BsonDocument[]
{
                    new MongoDB.Bson.BsonDocument("$match", new MongoDB.Bson.BsonDocument
                    {
                        { "raw_data_id", new MongoDB.Bson.BsonDocument
                            {
                                { "$exists", true },
                                { "$ne", MongoDB.Bson.BsonNull.Value }
                            }
                        }
                    }),
                    new MongoDB.Bson.BsonDocument("$project", new MongoDB.Bson.BsonDocument
                    {
                        { "_id", 1 },
                        { "raw_data_id", 1 }
                    })
                };

                var processDataCollection2 = await Data.MongoDBManager.Instance.GetCollectionAsync<MongoDB.Bson.BsonDocument>("process_data");
                var idMappingCursor = await processDataCollection2.AggregateAsync<MongoDB.Bson.BsonDocument>(idMappingPipeline);

                await idMappingCursor.ForEachAsync(doc =>
                {
                    try
                    {
                        // _id (ObjectId)를 안전하게 문자열로 변환
                        string id = null;
                        var idBson = doc.GetValue("_id", MongoDB.Bson.BsonNull.Value);
                        if (idBson != null && idBson.IsObjectId)
                        {
                            id = idBson.AsObjectId.ToString();
                        }

                        // raw_data_id (ObjectId)를 안전하게 문자열로 변환
                        string rawDataId = null;
                        var rawDataIdBson = doc.GetValue("raw_data_id", MongoDB.Bson.BsonNull.Value);
                        if (rawDataIdBson != null)
                        {
                            if (rawDataIdBson.IsObjectId)
                            {
                                rawDataId = rawDataIdBson.AsObjectId.ToString();
                            }
                            else if (rawDataIdBson.IsString)
                            {
                                rawDataId = rawDataIdBson.AsString;
                            }
                        }

                        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(rawDataId))
                        {
                            processDataToRawDataMap.TryAdd(id, rawDataId);
                            rawDataToProcessDataMap.TryAdd(rawDataId, id);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ID 매핑 처리 중 오류: {ex.Message}");
                    }
                });

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ID 매핑 준비 완료: {processDataToRawDataMap.Count}개");

                // 4. 대용량 병렬 문서 생성 (27초 → 7초)
                await progress(30, $"데이터 변환 준비 중... (총 {totalRows}행)");

                // 192GB RAM 활용한 대용량 병렬 처리
                //int maxParallelism = Math.Min(Environment.ProcessorCount, 16); // 최대 16개 스레드
                int maxParallelism = Math.Max(Environment.ProcessorCount, 16); // 최대 16개 스레드
                int optimalBatchSize = 50000; // 5만개씩 처리하여 메모리 효율성 확보

                // 모든 데이터를 메모리에 로드하여 병렬 처리 최적화
                var allProcessViewDocuments = new ConcurrentBag<MongoModels.ProcessViewDocument>();

                // 행 단위 병렬 처리
                await Task.Run(() =>
                {
                    Parallel.For(0, totalRows,
                        new ParallelOptions { MaxDegreeOfParallelism = maxParallelism },
                        rowIndex =>
                        {
                            try
                            {
                                var processViewDoc = CreateProcessViewDocument(
                                    dgvApplied, dataTable, rowIndex,
                                    moneyDataMap, processDataToRawDataMap, rawDataToProcessDataMap,
                                    deptCache, prodCache);

                                if (processViewDoc != null)
                                {
                                    allProcessViewDocuments.Add(processViewDoc);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 행 {rowIndex} 처리 실패: {ex.Message}");
                            }
                        });
                });

                var documentsToInsert = allProcessViewDocuments.ToList();
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 문서 변환 완료: {documentsToInsert.Count}개 유효 문서 생성");

                if (documentsToInsert.Count == 0)
                {
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 삽입할 유효한 문서가 없습니다.");
                    await progress(100, "삽입할 유효한 문서가 없습니다.");
                    return;
                }

                // 5. 최적화된 배치 삽입
                await progress(60, $"MongoDB에 데이터 삽입 중... ({documentsToInsert.Count}건)");

                var batches = CreateOptimalBatches(documentsToInsert, optimalBatchSize);
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 병렬 삽입 시작: {batches.Count}개 배치, 총 {documentsToInsert.Count}개 문서");

                // 성능 최적화를 위한 MongoDB 설정
                var insertOptions = new MongoDB.Driver.InsertManyOptions
                {
                    IsOrdered = false, // 순서 상관없이 삽입하여 성능 향상
                    BypassDocumentValidation = false
                };

                // 병렬 삽입 - 더 많은 동시 연결 허용
                int concurrentConnections = Math.Min(maxParallelism * 2, 32); // 최대 32개 동시 연결
                using (var semaphore = new SemaphoreSlim(concurrentConnections))
                {
                    var insertTasks = batches.Select(async (batch, batchIndex) =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            // 재시도 로직 포함
                            for (int attempt = 1; attempt <= 3; attempt++)
                            {
                                try
                                {
                                    await processViewRepo.InsertManyAsync(batch, insertOptions);

                                    // 진행상황 로깅
                                    if (batchIndex % 10 == 0)
                                    {
                                        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 배치 {batchIndex + 1}/{batches.Count} 완료 ({batch.Count}개 문서)");
                                    }

                                    return batch.Count; // 성공 시 삽입된 문서 수 반환
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 배치 {batchIndex + 1} 삽입 시도 {attempt} 실패: {ex.Message}");

                                    if (attempt == 3)
                                    {
                                        // 마지막 시도에서도 실패하면 개별 문서 처리
                                        int successCount = 0;
                                        foreach (var doc in batch)
                                        {
                                            try
                                            {
                                                await processViewRepo.InsertOneAsync(doc);
                                                successCount++;
                                            }
                                            catch (Exception docEx)
                                            {
                                                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 개별 문서 삽입 실패: {docEx.Message}");
                                            }
                                        }
                                        return successCount;
                                    }

                                    // 재시도 전 잠시 대기
                                    await Task.Delay(1000 * attempt);
                                }
                            }
                            return 0;
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    var results = await Task.WhenAll(insertTasks);
                    int totalInserted = results.Sum();

                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 병렬 삽입 완료: {totalInserted}개 문서 삽입됨");
                }

                // 최종 확인
                long finalCount = await processViewRepo.CountDocumentsAsync();
                long insertedCount = finalCount - existingCount;

                var endTime = DateTime.Now;
                var duration = endTime - startTime;

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MongoDB 저장 완료: {insertedCount}개 문서 삽입됨");
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 처리 시간: {duration.TotalSeconds:F2}초 (시작: {startTime:HH:mm:ss.fff}, 종료: {endTime:HH:mm:ss.fff})");

                await progress(80, $"데이터 저장 완료: {insertedCount}개 문서 삽입됨");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MongoDB 저장 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }



    }
}
