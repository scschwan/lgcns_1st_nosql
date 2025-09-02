using DocumentFormat.OpenXml.Wordprocessing;
using FinanceTool.Data;
using FinanceTool.MongoModels;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTool
{
    public partial class uc_FileLoad
    {

        // MongoDB 기반으로 페이징 데이터 로드
        public async Task LoadMongoPagedDataAsync(bool progressYN = false)
        {
            // 파일이 로드되지 않았으면 아무 작업도 수행하지 않음
            if (!_fileLoaded)
            {
                Debug.WriteLine("파일이 로드되지 않아 페이징 작업을 건너뜁니다.");
                return;
            }

            try
            {
                // MongoDB 데이터 컨버터
                MongoDataConverter mongoConverter = new MongoDataConverter();

                if (progressYN)
                {
                    // MongoDB에서 페이징된 데이터 가져오기
                    var filter = Builders<RawDataDocument>.Filter.Empty;

                    // hiddenData가 false인 경우, 숨겨진 문서 제외
                    if (!DataHandler.hiddenData)
                    {
                        filter = Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false);
                    }

                    var (documents, totalCount) = await mongoConverter.GetPagedRawDataAsync(
                        currentPage, pageSize, DataHandler.hiddenData);



                    // 페이징 메타데이터 계산
                    totalRows = (int)totalCount;
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                    // MongoDB 문서를 DataTable로 변환
                    DataTable pageData = ConvertMongoDocumentsToDataTable(documents);

                    // UI 업데이트는 메인 스레드에서 수행
                    this.BeginInvoke(new Action(() =>
                    {
                        ConfigureDataGridView(pageData, dataGridView_target);
                        ConfigureDataGridView(pageData, dataGridView_process);
                        UpdatePaginationInfo();
                        ApplyGridFormatting();
                    }));
                }
                else
                {
                    using (var loadingForm = new ProcessProgressForm())
                    {
                        loadingForm.Show();
                        loadingForm.UpdateProgressHandler(10);

                        // MongoDB에서 페이징된 데이터 가져오기
                        var result = await Task.Run(async () =>
                        {
                            // MongoDB에서 페이징된 데이터 가져오기
                            var filter = Builders<RawDataDocument>.Filter.Empty;

                            // hiddenData가 false인 경우, 숨겨진 문서 제외
                            if (!DataHandler.hiddenData)
                            {
                                filter = Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false);
                            }

                            var (documents, totalCount) = await mongoConverter.GetPagedRawDataAsync(
                            currentPage, pageSize, DataHandler.hiddenData);

                            // 페이징 메타데이터 계산
                            totalRows = (int)totalCount;
                            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                            loadingForm.UpdateProgressHandler(70);

                            // MongoDB 문서를 DataTable로 변환
                            return ConvertMongoDocumentsToDataTable(documents);
                        });

                        loadingForm.UpdateProgressHandler(80);

                        // UI 업데이트는 메인 스레드에서 수행
                        this.BeginInvoke(new Action(() =>
                        {
                            ConfigureDataGridView(result, dataGridView_target);
                            ConfigureDataGridView(result, dataGridView_process);
                            UpdatePaginationInfo();
                            ApplyGridFormatting();
                        }));

                        loadingForm.UpdateProgressHandler(100);
                        await Task.Delay(300);
                        loadingForm.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 페이지 데이터 로드 중 오류: {ex.Message}");
                MessageBox.Show($"데이터 로드 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // MongoDB 문서를 DataTable로 변환하는 헬퍼 메서드
        private DataTable ConvertMongoDocumentsToDataTable(List<RawDataDocument> documents)
        {
            DataTable dataTable = new DataTable();

            // 기본 컬럼 추가
            dataTable.Columns.Add("id", typeof(string));
            dataTable.Columns.Add("import_date", typeof(DateTime));
            dataTable.Columns.Add("is_hidden", typeof(bool));  // hiddenYN 대신 is_hidden 사용

            // 첫 번째 문서의 데이터를 기반으로 동적 컬럼 추가
            if (documents.Count > 0 && documents[0].Data != null)
            {
                foreach (var key in documents[0].Data.Keys)
                {
                    if (!dataTable.Columns.Contains(key))
                    {
                        dataTable.Columns.Add(key);
                    }
                }
            }

            // 문서 데이터를 DataTable에 추가
            foreach (var doc in documents)
            {
                DataRow row = dataTable.NewRow();
                row["id"] = doc.Id;
                row["import_date"] = doc.ImportDate;
                row["is_hidden"] = doc.IsHidden;  // 직접 is_hidden 값 사용

                // 동적 데이터 필드 추가
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

            //return dataTable;
            // ✅ 컬럼 순서 적용하여 반환
            return DataHandler_fileLoad.ApplyColumnOrder(dataTable);
        }


        // MongoDB에서 컬럼 가시성 업데이트하는 새 메서드
        private async void UpdateColumnVisibilityInMongo(string columnName, bool isVisible)
        {
            try
            {
                var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                var columnCollection = await mongoManager.GetCollectionAsync<BsonDocument>("column_mapping");

                var filter = Builders<BsonDocument>.Filter.Eq("original_name", columnName);
                var update = Builders<BsonDocument>.Update.Set("is_visible", isVisible);

                await columnCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 컬럼 가시성 업데이트 오류: {ex.Message}");
                // 오류 무시하고 계속 진행
            }
        }

        // MongoDB에서 필드의 고유값 가져오기
        private async Task<List<object>> GetDistinctValuesFromMongoAsync(string fieldName)
        {
            // 필드가 존재하는 모든 문서에서 고유 값을 가져오기
            //var filter = Builders<RawDataDocument>.Filter.Ne($"Data.{fieldName}", BsonNull.Value);
            var filterBuilder = Builders<RawDataDocument>.Filter;
            var filter = filterBuilder.And(
                filterBuilder.Exists($"Data.{fieldName}"),
                filterBuilder.Ne($"Data.{fieldName}", BsonNull.Value)
            );

            // 숨겨진 문서는 제외
            if (DataHandler.hiddenData)
            {
                filter = Builders<RawDataDocument>.Filter.And(
                    filter,
                    Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false)
                );
            }

            var distinctValues = new List<object>();
            var documents = await rawDataRepo.FindDocumentsAsync(filter);

            // 문서에서 해당 필드의 고유 값을 추출
            var valueSet = new HashSet<string>();
            foreach (var doc in documents)
            {
                if (doc.Data != null && doc.Data.ContainsKey(fieldName) && doc.Data[fieldName] != null)
                {
                    string value = doc.Data[fieldName].ToString();
                    if (!string.IsNullOrEmpty(value) && !valueSet.Contains(value))
                    {
                        valueSet.Add(value);
                        distinctValues.Add(value);
                    }
                }
            }

            // 값을 정렬
            distinctValues.Sort((a, b) => string.Compare(a.ToString(), b.ToString()));

            return distinctValues;
        }

        // MongoDB에서 검색을 위한 새 메서드
        private async Task<List<string>> SearchMongoFieldByKeywordsAsync(string fieldName, string[] keywords)
        {
            var resultValues = new List<string>();
            var valueSet = new HashSet<string>(); // 중복 방지를 위한 Set

            foreach (string keyword in keywords)
            {
                // 정규식 패턴 생성 (대소문자 구분 없이 검색)
                var regexPattern = new BsonRegularExpression(keyword, "i");

                // 필드 값이 검색 키워드를 포함하는 문서 필터
                var filter = Builders<RawDataDocument>.Filter.Regex($"Data.{fieldName}", regexPattern);

                // 숨겨진 문서는 제외
                if (DataHandler.hiddenData)
                {
                    filter = Builders<RawDataDocument>.Filter.And(
                        filter,
                        Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false)
                    );
                }

                // 문서 조회
                var documents = await rawDataRepo.FindDocumentsAsync(filter);

                // 결과에서 필드 값 추출
                foreach (var doc in documents)
                {
                    if (doc.Data != null && doc.Data.ContainsKey(fieldName) && doc.Data[fieldName] != null)
                    {
                        string value = doc.Data[fieldName].ToString();
                        if (!string.IsNullOrEmpty(value) && !valueSet.Contains(value))
                        {
                            valueSet.Add(value);
                            resultValues.Add(value);
                        }
                    }
                }
            }

            // 결과 정렬
            resultValues.Sort();

            return resultValues;
        }

        /// <summary>
        /// 표준화 수행 (최다 빈도 값으로 통일) - 초고속 병렬 처리 버전
        /// 기존 PerformStandardization 함수를 완전히 교체
        /// </summary>
        private async Task PerformStandardization()
        {
            try
            {
                if (_standardMappingData == null || _standardMappingData.Rows.Count == 0)
                {
                    MessageBox.Show("먼저 매핑 분석을 수행해주세요.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult result = MessageBox.Show(
                    "표준화를 수행하면 각 Key 값별로 최다 빈도의 대상값으로 통일됩니다.\n" +
                    "이 작업은 되돌릴 수 없습니다. 계속하시겠습니까?",
                    "표준화 수행 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;

                string keyColumn = comboBox_standard_key.SelectedItem.ToString();
                string targetColumn = comboBox_standard_target.SelectedItem.ToString();

                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(5, "표준화 준비 중...");

                    // 🔥 1단계: Key별 최다 빈도 값 추출 (메모리 기반)
                    await progressForm.UpdateProgressHandler(10, "표준값 분석 중...");
                    var standardValues = GetStandardValuesFromMapping_Optimized();

                    Debug.WriteLine($"[표준화] 분석된 표준값: {standardValues.Count}개");

                    // 🔥 2단계: 초고속 병렬 MongoDB 업데이트 수행
                    await progressForm.UpdateProgressHandler(20, "고속 병렬 처리 시작...");
                    int updatedCount = await PerformBulkStandardization_UltraFast(keyColumn, targetColumn, standardValues, progressForm.UpdateProgressHandler);

                    await progressForm.UpdateProgressHandler(85, "매핑 데이터 재분석 중...");

                    // 🔥 3단계: 매핑 데이터 재분석 (최적화된 버전 사용)
                    await AnalyzeKeyTargetMapping_UltraFast();

                    await progressForm.UpdateProgressHandler(95, "페이징 데이터 새로고침 중...");

                    // 🔥 4단계: 페이징 데이터 새로고침
                    await LoadMongoPagedDataAsync();

                    await progressForm.UpdateProgressHandler(100, "표준화 완료");

                    MessageBox.Show($"표준화 완료: {updatedCount:N0}개 문서 업데이트", "완료",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"표준화 수행 오류: {ex.Message}");
                MessageBox.Show($"표준화 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 매핑 데이터에서 Key별 표준값 추출 (최적화 버전)
        /// </summary>
        private Dictionary<string, string> GetStandardValuesFromMapping_Optimized()
        {
            var sw = Stopwatch.StartNew();

            // 🔥 PLINQ로 병렬 그룹화 및 최다 빈도값 추출
            var standardValues = _standardMappingData.AsEnumerable()
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount * 2)
                .GroupBy(row => row["KeyValue"].ToString())
                .ToDictionary(
                    keyGroup => keyGroup.Key,
                    keyGroup => keyGroup
                        .OrderByDescending(row => Convert.ToInt32(row["Count"]))
                        .First()["TargetValue"].ToString()
                );

            sw.Stop();
            Debug.WriteLine($"[표준값추출] 완료 - {standardValues.Count}개 표준값, 소요시간: {sw.ElapsedMilliseconds}ms");

            return standardValues;
        }

        /// <summary>
        /// Key-Target 매핑 분석 및 표시 (초고속 버전)
        /// 기존 AnalyzeKeyTargetMapping 함수를 완전히 교체
        /// </summary>
        private async Task AnalyzeKeyTargetMapping_UltraFast()
        {
            try
            {
                if (!ValidateStandardizationSelection())
                    return;

                string keyColumn = comboBox_standard_key.SelectedItem.ToString();
                string targetColumn = comboBox_standard_target.SelectedItem.ToString();

                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "고속 매핑 분석 시작...");

                    // 🔥 초고속 매핑 분석 (메모리 기반)
                    _standardMappingData = await GetKeyTargetMappingDataAsync_UltraFast(keyColumn, targetColumn);

                    await progressForm.UpdateProgressHandler(80, "결과 표시 중...");

                    // DataGridView에 결과 표시
                    DisplayMappingResults();

                    await progressForm.UpdateProgressHandler(100, "매핑 분석 완료");
                }

                Debug.WriteLine($"초고속 매핑 분석 완료: {_standardMappingData.Rows.Count}개 결과");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"매핑 분석 오류: {ex.Message}");
                MessageBox.Show($"매핑 분석 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Key-Target 매핑 데이터 조회 (초고속 메모리 기반 버전)
        /// 192GB 메모리와 PLINQ를 활용한 최적화
        /// </summary>
        private async Task<DataTable> GetKeyTargetMappingDataAsync_UltraFast(string keyColumn, string targetColumn)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                Debug.WriteLine($"[매핑분석] 초고속 분석 시작 - Key: {keyColumn}, Target: {targetColumn}");

                var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                var collection = await mongoManager.GetCollectionAsync<RawDataDocument>("raw_data");

                // 🔥 1단계: 메모리 기반 전체 데이터 캐싱
                Debug.WriteLine($"[매핑분석] 1단계 - 전체 데이터 메모리 캐싱");
                var cachedData = await PreloadMappingDataToMemoryAsync(collection, keyColumn, targetColumn);
                Debug.WriteLine($"[매핑분석] 1단계 완료 - 캐시된 레코드: {cachedData.Count:N0}개");

                // 🔥 2단계: PLINQ 기반 초고속 그룹화
                Debug.WriteLine($"[매핑분석] 2단계 - PLINQ 병렬 그룹화");
                var mappingResults = cachedData
                    .AsParallel()
                    .WithDegreeOfParallelism(Environment.ProcessorCount * 4) // CPU 코어 × 4배
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Where(item => !string.IsNullOrEmpty(item.KeyValue) && !string.IsNullOrEmpty(item.TargetValue))
                    .GroupBy(item => new { KeyValue = item.KeyValue, TargetValue = item.TargetValue })
                    .Select(group => new MappingResult
                    {
                        KeyValue = group.Key.KeyValue,
                        TargetValue = group.Key.TargetValue,
                        Count = group.Count()
                    })
                    .ToList();

                Debug.WriteLine($"[매핑분석] 2단계 완료 - 매핑 결과: {mappingResults.Count:N0}개");

                // 🔥 3단계: 메모리 기반 Key별 순위 계산
                Debug.WriteLine($"[매핑분석] 3단계 - Key별 순위 계산");
                var finalResults = mappingResults
                    .AsParallel()
                    .WithDegreeOfParallelism(Environment.ProcessorCount * 2)
                    .GroupBy(mr => mr.KeyValue)
                    .SelectMany(keyGroup =>
                    {
                        return keyGroup.OrderByDescending(mr => mr.Count); // Count 기준 내림차순
                    })
                    .ToList();

                // 🔥 4단계: DataTable 생성 (UI 바인딩용)
                DataTable dataTable = CreateMappingDataTable(finalResults);

                sw.Stop();
                Debug.WriteLine($"[매핑분석] 전체 완료 - 소요시간: {sw.ElapsedMilliseconds:N0}ms, 결과: {dataTable.Rows.Count}개");

                return dataTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[매핑분석] 오류 발생: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// 매핑 데이터를 메모리에 캐싱 (고성능 projection 활용)
        /// </summary>
        private async Task<List<MappingDataItem>> PreloadMappingDataToMemoryAsync(
            IMongoCollection<RawDataDocument> collection,
            string keyColumn,
            string targetColumn)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                // 🔥 필요한 필드만 projection하여 네트워크 대역폭 최적화
                var projection = Builders<RawDataDocument>.Projection
                    .Include($"data.{keyColumn}")
                    .Include($"data.{targetColumn}");

                // 🔥 MongoDB 집계 파이프라인으로 전처리
                var pipeline = new[]
                {
            new BsonDocument("$project", new BsonDocument
            {
                ["keyValue"] = $"$data.{keyColumn}",
                ["targetValue"] = $"$data.{targetColumn}"
            }),
            new BsonDocument("$match", new BsonDocument
            {
                ["keyValue"] = new BsonDocument("$ne", BsonNull.Value),
                ["targetValue"] = new BsonDocument("$ne", BsonNull.Value)
            })
        };

                var results = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

                sw.Stop();
                Debug.WriteLine($"[매핑캐싱] MongoDB 조회 완료 - {results.Count:N0}개, 소요시간: {sw.ElapsedMilliseconds:N0}ms");

                // 🔥 PLINQ로 캐시 구조체 변환
                var cachedData = results
                    .AsParallel()
                    .WithDegreeOfParallelism(Environment.ProcessorCount * 4)
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(doc => new MappingDataItem
                    {
                        KeyValue = ExtractValue(doc["keyValue"]),
                        TargetValue = ExtractValue(doc["targetValue"])
                    })
                    .Where(item => !string.IsNullOrEmpty(item.KeyValue) && !string.IsNullOrEmpty(item.TargetValue))
                    .ToList();

                Debug.WriteLine($"[매핑캐싱] PLINQ 변환 완료 - 유효한 항목: {cachedData.Count:N0}개");
                return cachedData;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[매핑캐싱] 오류 발생: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// DataTable 생성 최적화 함수
        /// </summary>
        private DataTable CreateMappingDataTable(List<MappingResult> mappingResults)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("KeyValue", typeof(string));
            dataTable.Columns.Add("TargetValue", typeof(string));
            dataTable.Columns.Add("Count", typeof(int));

            // 🔥 병렬로 DataRow 생성 후 순차 추가 (DataTable 스레드 안전성 고려)
            var dataRows = mappingResults
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(result => new object[] { result.KeyValue, result.TargetValue, result.Count })
                .ToList();

            // DataTable에 순차 추가 (스레드 안전)
            foreach (var rowData in dataRows)
            {
                DataRow row = dataTable.NewRow();
                row.ItemArray = rowData;
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }


        /// <summary>
        /// MongoDB 일괄 표준화 수행 (최종 최적화 버전)
        /// 기존 PerformBulkStandardization 함수를 완전히 교체
        /// </summary>
        private async Task<int> PerformBulkStandardization_UltraFast(
            string keyColumn,
            string targetColumn,
            Dictionary<string, string> standardValues,
            ProcessProgressForm.UpdateProgressDelegate progressCallback = null)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                Debug.WriteLine($"[초고속표준화] 시작 - Key별 표준값: {standardValues.Count}개");

                var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                var collection = await mongoManager.GetCollectionAsync<BsonDocument>("raw_data");

                // 🔥 1단계: 메모리 기반 데이터 캐싱 (디스크 I/O 제거)
                await progressCallback?.Invoke(25, "전체 데이터 메모리 캐싱 중...");
                var allDocumentsCache = await PreloadAllDocumentsToMemory_Optimized(collection, keyColumn, targetColumn);
                Debug.WriteLine($"[초고속표준화] 1단계 완료 - 캐시된 문서: {allDocumentsCache.Count:N0}개");

                // 🔥 2단계: PLINQ 기반 초고속 병렬 처리
                await progressCallback?.Invoke(40, "PLINQ 병렬 변환 처리 중...");
                var updateBatches = await CreateUpdateBatches_MemoryOptimized(allDocumentsCache, standardValues, targetColumn);
                Debug.WriteLine($"[초고속표준화] 2단계 완료 - 생성된 배치: {updateBatches.Count}개");

                // 🔥 3단계: 배치별 병렬 MongoDB 업데이트
                await progressCallback?.Invoke(50, "병렬 MongoDB 업데이트 실행 중...");
                int totalUpdated = await ExecuteParallelBatchUpdates_Optimized(collection, updateBatches, targetColumn, progressCallback);
                Debug.WriteLine($"[초고속표준화] 3단계 완료 - 업데이트된 문서: {totalUpdated:N0}개");

                sw.Stop();
                Debug.WriteLine($"[초고속표준화] 전체 완료 - 총 소요시간: {sw.ElapsedMilliseconds:N0}ms, 업데이트: {totalUpdated:N0}개");

                return totalUpdated;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[초고속표준화] 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 전체 문서를 메모리에 캐싱 (성능 최적화 버전)
        /// </summary>
        private async Task<List<StandardizationDocument>> PreloadAllDocumentsToMemory_Optimized(
            IMongoCollection<BsonDocument> collection,
            string keyColumn,
            string targetColumn)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                // 🔥 MongoDB 집계 파이프라인으로 필요한 데이터만 전처리
                var pipeline = new[]
                {
            new BsonDocument("$project", new BsonDocument
            {
                ["_id"] = 1,
                ["keyValue"] = $"$data.{keyColumn}",
                ["targetValue"] = $"$data.{targetColumn}"
            }),
            new BsonDocument("$match", new BsonDocument
            {
                ["keyValue"] = new BsonDocument("$ne", BsonNull.Value),
                ["targetValue"] = new BsonDocument("$ne", BsonNull.Value)
            })
        };

                var results = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

                Debug.WriteLine($"[메모리캐싱] MongoDB 집계 완료 - {results.Count:N0}개 문서, 소요시간: {sw.ElapsedMilliseconds:N0}ms");

                // 🔥 PLINQ로 캐시 구조체 변환 (초고속 병렬 처리)
                var cachedDocuments = results
                    .AsParallel()
                    .WithDegreeOfParallelism(Environment.ProcessorCount * 4) // CPU 코어 × 4배
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(doc => new StandardizationDocument
                    {
                        Id = doc["_id"].AsObjectId,
                        KeyValue = ExtractValue(doc["keyValue"]),
                        TargetValue = ExtractValue(doc["targetValue"])
                    })
                    .Where(doc => !string.IsNullOrEmpty(doc.KeyValue) && !string.IsNullOrEmpty(doc.TargetValue))
                    .ToList();

                sw.Stop();
                Debug.WriteLine($"[메모리캐싱] PLINQ 변환 완료 - 유효한 문서: {cachedDocuments.Count:N0}개, 총 소요시간: {sw.ElapsedMilliseconds:N0}ms");

                return cachedDocuments;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[메모리캐싱] 오류 발생: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// PLINQ 기반 초고속 업데이트 배치 생성 (메모리 최적화)
        /// </summary>
        private async Task<List<StandardizationBatch>> CreateUpdateBatches_MemoryOptimized(
            List<StandardizationDocument> cachedDocuments,
            Dictionary<string, string> standardValues,
            string targetColumn)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var sw = Stopwatch.StartNew();

                    // 🔥 PLINQ로 초고속 필터링 및 그룹화
                    var updateGroups = cachedDocuments
                        .AsParallel()
                        .WithDegreeOfParallelism(Environment.ProcessorCount * 4)
                        .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                        .Where(doc => standardValues.ContainsKey(doc.KeyValue)) // 표준화 대상만
                        .Where(doc => doc.TargetValue != standardValues[doc.KeyValue]) // 변경 필요한 문서만
                        .GroupBy(doc => standardValues[doc.KeyValue]) // 표준값별 그룹화
                        .ToList();

                    Debug.WriteLine($"[배치생성] 그룹화 완료 - {updateGroups.Count}개 표준값 그룹");

                    // 🔥 동적 배치 크기 결정 (192GB 메모리 활용)
                    int totalDocuments = updateGroups.Sum(g => g.Count());
                    int optimalBatchSize = CalculateOptimalBatchSize_Memory(totalDocuments);

                    Debug.WriteLine($"[배치생성] 총 업데이트 대상: {totalDocuments:N0}개, 최적 배치 크기: {optimalBatchSize:N0}");

                    // 🔥 표준값별 배치 생성
                    var batches = new List<StandardizationBatch>();
                    int batchIndex = 0;

                    foreach (var group in updateGroups)
                    {
                        string standardValue = group.Key;
                        var documents = group.ToList();

                        // 큰 그룹은 여러 배치로 분할
                        for (int i = 0; i < documents.Count; i += optimalBatchSize)
                        {
                            var batchDocs = documents.Skip(i).Take(optimalBatchSize).ToList();

                            batches.Add(new StandardizationBatch
                            {
                                StandardValue = standardValue,
                                Documents = batchDocs,
                                BatchIndex = ++batchIndex
                            });
                        }
                    }

                    sw.Stop();
                    Debug.WriteLine($"[배치생성] 완료 - {batches.Count}개 배치 생성, 소요시간: {sw.ElapsedMilliseconds:N0}ms");

                    return batches;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[배치생성] 오류 발생: {ex.Message}");
                    throw;
                }
            });
        }

        /// <summary>
        /// 병렬 배치 업데이트 실행 (최종 최적화)
        /// </summary>
        private async Task<int> ExecuteParallelBatchUpdates_Optimized(
            IMongoCollection<BsonDocument> collection,
            List<StandardizationBatch> batches,
            string targetColumn,
            ProcessProgressForm.UpdateProgressDelegate progressCallback = null)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                int totalUpdated = 0;
                int completedBatches = 0;

                // 🔥 최대 동시성 설정 (CPU 집약적 + MongoDB 연결 풀 고려)
                int maxConcurrency = Math.Min(Environment.ProcessorCount * 2, Math.Min(batches.Count, 10));
                using var semaphore = new SemaphoreSlim(maxConcurrency);

                Debug.WriteLine($"[병렬업데이트] 시작 - 최대 동시성: {maxConcurrency}, 총 배치: {batches.Count}개");

                // 🔥 병렬 배치 처리 with 진행률 업데이트
                var updateTasks = batches.Select(async batch =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        int updated = await ExecuteSingleBatch_Optimized(collection, batch, targetColumn);

                        // 스레드 안전한 진행률 업데이트
                        int completed = Interlocked.Increment(ref completedBatches);
                        Interlocked.Add(ref totalUpdated, updated);

                        // 진행률 콜백 (10배치마다)
                        if (completed % Math.Max(1, batches.Count / 10) == 0)
                        {
                            int progress = 50 + (completed * 30 / batches.Count);
                            await progressCallback?.Invoke(progress, $"배치 처리 중... ({completed}/{batches.Count})");
                        }

                        return updated;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                var results = await Task.WhenAll(updateTasks);
                totalUpdated = results.Sum();

                sw.Stop();
                Debug.WriteLine($"[병렬업데이트] 완료 - 업데이트: {totalUpdated:N0}개, 소요시간: {sw.ElapsedMilliseconds:N0}ms");

                return totalUpdated;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[병렬업데이트] 오류 발생: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 개별 배치 실행 (최적화된 BulkWrite)
        /// </summary>
        private async Task<int> ExecuteSingleBatch_Optimized(
            IMongoCollection<BsonDocument> collection,
            StandardizationBatch batch,
            string targetColumn)
        {
            const int maxRetries = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // 🔥 BulkWrite로 단일 요청 처리
                    var docIds = batch.Documents.Select(d => d.Id).ToList();
                    var filter = Builders<BsonDocument>.Filter.In("_id", docIds);
                    var update = Builders<BsonDocument>.Update.Set($"data.{targetColumn}", batch.StandardValue);

                    var result = await collection.UpdateManyAsync(filter, update);

                    return (int)result.ModifiedCount;
                }
                catch (Exception ex)
                {
                    if (attempt >= maxRetries)
                    {
                        Debug.WriteLine($"[배치{batch.BatchIndex}] 최종 실패 - {ex.Message}");
                        throw;
                    }

                    // 지수 백오프
                    int delayMs = (int)Math.Pow(2, attempt) * 100;
                    await Task.Delay(delayMs);
                }
            }

            return 0;
        }

        /// <summary>
        /// 메모리 기반 최적 배치 크기 계산 (192GB 메모리 활용)
        /// </summary>
        private int CalculateOptimalBatchSize_Memory(int totalDocuments)
        {
            // 192GB 메모리 환경에서 더 큰 배치 크기 사용 가능
            if (totalDocuments < 50000)
                return 5000;   // 소량 데이터
            else if (totalDocuments < 500000)
                return 20000;  // 중량 데이터  
            else if (totalDocuments < 5000000)
                return 50000;  // 대량 데이터
            else
                return 100000; // 초대량 데이터 (192GB 메모리 최대 활용)
        }

        // 🔥 최적화된 구조체들
        public struct StandardizationDocument
        {
            public ObjectId Id { get; set; }
            public string KeyValue { get; set; }
            public string TargetValue { get; set; }
        }

        public class StandardizationBatch
        {
            public string StandardValue { get; set; }
            public List<StandardizationDocument> Documents { get; set; }
            public int BatchIndex { get; set; }
        }



        /// <summary>
        /// 복합 타입과 문자열 타입을 모두 처리하는 통합 필터 생성
        /// </summary>
        private FilterDefinition<BsonDocument> CreateUniversalFilter(string fieldName, string value)
        {
            var filters = new List<FilterDefinition<BsonDocument>>();

            // 1. 문자열 직접 매칭
            filters.Add(Builders<BsonDocument>.Filter.Eq($"data.{fieldName}", value));

            // 2. 복합 타입 (_v 필드) 매칭 - 문자열로
            filters.Add(Builders<BsonDocument>.Filter.Eq($"data.{fieldName}._v", value));

            // 3. 숫자로 변환 가능한 경우 숫자 매칭
            if (decimal.TryParse(value, out decimal numericValue))
            {
                filters.Add(Builders<BsonDocument>.Filter.Eq($"data.{fieldName}._v", numericValue));
            }

            // OR 조건으로 결합
            return Builders<BsonDocument>.Filter.Or(filters);
        }

        // 🔥 고성능을 위한 구조체들
        public struct MappingDataItem
        {
            public string KeyValue { get; set; }
            public string TargetValue { get; set; }
        }

        public struct MappingResult
        {
            public string KeyValue { get; set; }
            public string TargetValue { get; set; }
            public int Count { get; set; }
        }
    }
}
