using FinanceTool.Data;
using FinanceTool.MongoModels;
using FinanceTool.Repositories;
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
    public partial class uc_Classification
    {
        // 클러스터링 데이터 강화 메서드 (MongoDB 사용)
        // 클러스터링 데이터 로드 및 raw_data 정보로 강화
        private async Task<DataTable> CreateEnhancedClusteringDataAsync()
        {
            // 1. 클러스터링 데이터 로드 (메모리 또는 MongoDB에서)
            DataTable clusteringData;
            var clusteringRepo = new ClusteringRepository();

            // 메모리에 있는 경우 활용
            if (DataHandler.finalClusteringData != null && DataHandler.finalClusteringData.Rows.Count > 0)
            {
                Debug.WriteLine("메모리에 캐싱된 클러스터링 데이터 사용");
                clusteringData = DataHandler.finalClusteringData.Copy();
            }
            else
            {
                // MongoDB에서 로드
                Debug.WriteLine("MongoDB에서 클러스터링 데이터 로드");
                clusteringData = await clusteringRepo.ToDataTableAsync();
                DataHandler.finalClusteringData = clusteringData.Copy();
            }

            // 2. 강화된 데이터 테이블 생성
            DataTable enhancedTable = clusteringData.Copy();

            // 공급업체명과 부서명 컬럼 추가 (없는 경우)
            if (!enhancedTable.Columns.Contains(DataHandler.prod_col_name))
                enhancedTable.Columns.Add(DataHandler.prod_col_name, typeof(string));

            if (!enhancedTable.Columns.Contains(DataHandler.dept_col_name))
                enhancedTable.Columns.Add(DataHandler.dept_col_name, typeof(string));

            if (!enhancedTable.Columns.Contains("ClusterSubID"))
                enhancedTable.Columns.Add("ClusterSubID");

            enhancedTable.Columns.Add("세부클러스터명");

            // *** 수정: 컬럼 순서 조정 ***
            // 세부클러스터명 컬럼을 클러스터명 다음에 배치
            int clusterNameIndex = enhancedTable.Columns["클러스터명"].Ordinal;
            enhancedTable.Columns["세부클러스터명"].SetOrdinal(clusterNameIndex + 1);

            // 3. 클러스터별 dataIndex 수집
            Dictionary<int, List<string>> clusterToDataIndices = new Dictionary<int, List<string>>();

            foreach (DataRow row in enhancedTable.Rows)
            {
                if (row.IsNull("ClusterID")) continue;

                int clusterId = Convert.ToInt32(row["ClusterID"]);
                string dataIndexStr = row["dataIndex"]?.ToString();

                if (string.IsNullOrEmpty(dataIndexStr)) continue;

                if (!clusterToDataIndices.ContainsKey(clusterId))
                    clusterToDataIndices[clusterId] = new List<string>();

                foreach (string indexStr in dataIndexStr.Split(','))
                {
                    string trimmedIndex = indexStr.Trim();
                    if (!string.IsNullOrEmpty(trimmedIndex))
                        clusterToDataIndices[clusterId].Add(trimmedIndex);
                }
            }

            // 4. MongoDB에서 raw_data 정보로 강화
            // 각 클러스터에 대해 raw_data 정보 조회 및 추가
            var rawDataRepo = new RawDataRepository();

            foreach (var entry in clusterToDataIndices)
            {
                int clusterId = entry.Key;
                List<string> dataIndices = entry.Value;

                if (dataIndices.Count == 0) continue;

                var filter = Builders<RawDataDocument>.Filter.In(d => d.Id, dataIndices);
                var rawDataDocs = await rawDataRepo.FindDocumentsAsync(filter);

                // 공급업체 및 부서명 추출
                HashSet<string> uniqueProds = new HashSet<string>();
                HashSet<string> uniqueDepts = new HashSet<string>();

                foreach (var doc in rawDataDocs)
                {
                    // 공급업체명
                    if (doc.Data.TryGetValue(DataHandler.prod_col_name, out var prod) && prod != null)
                        uniqueProds.Add(prod.ToString());

                    // 부서명
                    if (doc.Data.TryGetValue(DataHandler.dept_col_name, out var dept) && dept != null)
                        uniqueDepts.Add(dept.ToString());
                }

                // 쉼표로 구분된 문자열로 변환
                string combinedProds = string.Join(",", uniqueProds);
                string combinedDepts = string.Join(",", uniqueDepts);

                // 문자열 길이 제한
                if (combinedProds.Length > 32767)
                    combinedProds = combinedProds.Substring(0, 32767);

                if (combinedDepts.Length > 32767)
                    combinedDepts = combinedDepts.Substring(0, 32767);

                // enhancedTable에 값 설정
                foreach (DataRow row in enhancedTable.Rows)
                {
                    if (!row.IsNull("ClusterID") && Convert.ToInt32(row["ClusterID"]) == clusterId)
                    {
                        row[DataHandler.prod_col_name] = combinedProds;
                        row[DataHandler.dept_col_name] = combinedDepts;
                    }
                    if (row.IsNull("ClusterSubID"))
                    {
                        row["ClusterSubID"] = -1;
                    }

                }
            }

            // *** 여기에 클러스터명과 세부클러스터명 설정 로직을 한 번만 실행 ***
            foreach (DataRow row in enhancedTable.Rows)
            {
                int clusterId = !row.IsNull("ClusterID") ? Convert.ToInt32(row["ClusterID"]) : -1;
                int clusterSubId = !row.IsNull("ClusterSubID") ? Convert.ToInt32(row["ClusterSubID"]) : -1;
                int id = Convert.ToInt32(row["ID"]);
                string originalClusterName = row["클러스터명"]?.ToString() ?? "";

                // 클러스터명과 세부클러스터명 설정
                if (clusterSubId == id && clusterSubId > 0)
                {
                    // 세부 상위 클러스터인 경우
                    // 부모 클러스터명 찾기
                    var parentCluster = enhancedTable.AsEnumerable()
                        .FirstOrDefault(r => Convert.ToInt32(r["ID"]) == clusterId);

                    row["클러스터명"] = parentCluster?["클러스터명"]?.ToString() ?? originalClusterName;
                    row["세부클러스터명"] = originalClusterName;
                }
                else
                {
                    // 일반 병합 클러스터인 경우
                    row["클러스터명"] = originalClusterName;
                    row["세부클러스터명"] = "";
                }
            }


            // CreateEnhancedClusteringDataAsync 함수 마지막에
            // 커스텀 정렬: 병합 클러스터 다음에 세부 클러스터들이 오도록
            var sortedRows = enhancedTable.AsEnumerable()
                .OrderBy(row =>
                {
                    int clusterId = Convert.ToInt32(row["ClusterID"]);
                    int clusterSubId = row["ClusterSubID"] != DBNull.Value ? Convert.ToInt32(row["ClusterSubID"]) : -1;
                    int id = Convert.ToInt32(row["ID"]);

                    // 정렬 키: "부모클러스터ID_세부여부_ID"
                    if (clusterSubId == id && clusterSubId > 0)
                    {
                        // 세부 클러스터: 부모 ID를 기준으로 하되 세부 표시
                        return $"{clusterId:D10}_1_{id:D10}";
                    }
                    else
                    {
                        // 일반 클러스터: ID를 기준으로 정렬
                        return $"{id:D10}_0_{id:D10}";
                    }
                })
                .ToList();

            // 정렬된 결과로 새 테이블 생성
            DataTable sortedTable = enhancedTable.Clone();
            foreach (var row in sortedRows)
            {
                sortedTable.ImportRow(row);
            }

            //return enhancedTable;
            return sortedTable;
        }


        /// <summary>
        /// Excel로 데이터를 내보내는 함수 - MongoDB 버전으로 개선
        /// </summary>
        public async Task<string> ExportToExcelAsync(List<string> columnList, bool hiddenTableYN = false)
        {
            string savedFilePath = null;
            try
            {
                using (var progress = new ProcessProgressForm())
                {
                    progress.Show();
                    await progress.UpdateProgressHandler(5, "데이터 내보내기 준비 중...");
                    await Task.Delay(10);

                    // 1단계: export_result 데이터 테이블 생성 (raw_data 컬렉션에서 데이터 로드)
                    DataTable export_result = null;

                    await Task.Run(async () =>
                    {
                        try
                        {
                            // MongoDB에서 raw_data 문서 조회
                            var rawDataRepo = new RawDataRepository();

                            // 필터 설정 - 숨겨진 문서 처리
                            var filter = hiddenTableYN ?
                                Builders<RawDataDocument>.Filter.Empty :
                                Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false);

                            await progress.UpdateProgressHandler(10, "MongoDB 데이터 조회 중...");

                            // 모든 문서 가져오기 - 페이징 사용 (대용량 데이터 처리)
                            List<RawDataDocument> allDocuments = new List<RawDataDocument>();
                            int batchSize = 10000;
                            int currentBatch = 0;
                            bool hasMoreData = true;

                            while (hasMoreData)
                            {
                                var skip = currentBatch * batchSize;
                                var sort = Builders<RawDataDocument>.Sort.Ascending(d => d.Id);

                                var batch = await rawDataRepo.FindDocumentsAsync(filter, sort, skip, batchSize);

                                if (batch.Count == 0)
                                {
                                    hasMoreData = false;
                                }
                                else
                                {
                                    allDocuments.AddRange(batch);
                                    currentBatch++;

                                    // 진행 상황 업데이트 (5% ~ 50% 사이로 배분)
                                    int progressValue = 10 + (int)(40.0 * allDocuments.Count / (currentBatch * batchSize + 1));
                                    await progress.UpdateProgressHandler(progressValue, $"데이터 로드 중... ({allDocuments.Count:N0}건)");
                                }
                            }

                            Debug.WriteLine($"총 {allDocuments.Count:N0}개 문서 로드 완료");
                            await progress.UpdateProgressHandler(50, "데이터 변환 중...");

                            // MongoDB 문서를 DataTable로 변환
                            export_result = ConvertRawDocumentsToEnhancedDataTable(allDocuments, columnList);

                            // 클러스터링 정보 추가
                            await progress.UpdateProgressHandler(60, "클러스터 정보 추가 중...");
                            await AddClusterInfoToExportDataAsync(export_result);



                            await progress.UpdateProgressHandler(70, "데이터 내보내기 준비 완료");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"데이터 로드 중 오류: {ex.Message}\n{ex.StackTrace}");
                            throw; // 예외를 상위로 전파
                        }
                    });

                    // 2단계: cluster_result 데이터 테이블 생성
                    await progress.UpdateProgressHandler(75, "클러스터 정보 변환 중...");
                    DataTable cluster_result = ConvertDataGridViewToCustomDataTable(dataGridView_classify);

                    // 3단계: Excel 저장
                    await progress.UpdateProgressHandler(90, "Excel 파일 저장 중...");
                    // DataHandler.SaveDataTableToExcel 메서드를 수정하여 저장 경로 반환
                    savedFilePath = DataHandler_classification.SaveDataTableToExcel(cluster_result, export_result);

                    await progress.UpdateProgressHandler(100, "Excel 파일 저장 완료");
                    await Task.Delay(500); // 완료 메시지 표시
                }

                // 저장 완료 메시지
                /*
                MessageBox.Show("Excel 파일로 내보내기가 완료되었습니다.", "내보내기 완료",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                */
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excel 파일 저장 중 오류 발생: {ex.Message}");
                MessageBox.Show($"Excel 파일 저장 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return savedFilePath;
            }
            return savedFilePath;
        }

        // MongoDB에서 visible 컬럼 목록 가져오기
        private async Task GetColumnListAsync()
        {
            try
            {
                process_col_list = new List<string>();

                // MongoDB의 column_mapping 컬렉션에서 visible 컬럼 가져오기
                var columnMappingRepo = new ColumnMappingRepository();
                var visibleColumns = await columnMappingRepo.GetVisibleColumnsAsync();

                foreach (var column in visibleColumns)
                {
                    process_col_list.Add(column.OriginalName);
                }

                // import_date 제외 (필요한 경우)
                process_col_list.Remove("import_date");
                Debug.WriteLine($"process_col_list count: {process_col_list.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"컬럼 목록 조회 중 오류: {ex.Message}");
                throw; // 상위 메서드에서 처리하도록 예외 전파
            }
        }

        // *** 3. 현재 페이지 데이터에 대해서만 클러스터명 매핑 조회 (캐시 제거) ***
        private async Task<Dictionary<string, string>> GetClusterNameMappingForPageAsync(List<string> rawDataIds)
        {
            var mappingDict = new Dictionary<string, string>();

            if (rawDataIds == null || rawDataIds.Count == 0)
                return mappingDict;

            try
            {
                var clusteringRepo = new ClusteringRepository();

                // clustering_results에서 cluster_number == cluster_id인 최종 클러스터만 조회
                var filter = Builders<ClusteringResultDocument>.Filter.Where(c => c.ClusterNumber == c.ClusterId);
                var finalClusters = await clusteringRepo.FindDocumentsAsync(filter);

                // 현재 페이지의 raw_data ID에 대해서만 매핑 생성
                foreach (var cluster in finalClusters)
                {
                    if (cluster.DataIndices != null && !string.IsNullOrEmpty(cluster.ClusterName))
                    {
                        foreach (var dataIndex in cluster.DataIndices)
                        {
                            if (rawDataIds.Contains(dataIndex) && !mappingDict.ContainsKey(dataIndex))
                            {
                                mappingDict[dataIndex] = cluster.ClusterName;
                            }
                        }
                    }
                }

                Debug.WriteLine($"현재 페이지 클러스터 매핑 생성: {rawDataIds.Count}개 ID 중 {mappingDict.Count}개 매핑");
                return mappingDict;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터명 매핑 조회 중 오류: {ex.Message}");
                return mappingDict;
            }
        }

        // MongoDB에서 컬럼 가시성 업데이트하는 비동기 메서드
        private async Task UpdateColumnVisibilityInMongoAsync(string columnName, bool isVisible)
        {
            try
            {
                var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                var columnCollection = await mongoManager.GetCollectionAsync<BsonDocument>("column_mapping");

                var filter = Builders<BsonDocument>.Filter.Eq("original_name", columnName);
                var update = Builders<BsonDocument>.Update.Set("is_visible", isVisible);

                var result = await columnCollection.UpdateOneAsync(filter, update);
                Debug.WriteLine($"컬럼 '{columnName}' 가시성 업데이트: Visible={isVisible}, 결과={result.ModifiedCount}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB {columnName} 컬럼 가시성 업데이트 오류: {ex.Message}");
                // 오류 발생 시에도 계속 진행 (개별 컬럼 업데이트 실패가 전체에 영향을 주지 않도록)
            }
        }

        // 페이징된 데이터 로드 메서드 (MongoDB 사용) - raw_data 활용 수정
        // LoadPagedDataAsync 함수를 수정하여 isAlreadyProgress 매개변수 추가
        private async Task LoadPagedDataAsync(bool isAlreadyProgress = false)
        {
            if (isProcessingSearch) return;

            try
            {
                isProcessingSearch = true;

                // isAlreadyProgress가 true면 별도의 프로그레스바를 표시하지 않음
                if (!isAlreadyProgress)
                {
                    using (var loadingForm = new ProcessProgressForm())
                    {
                        loadingForm.Show();
                        await loadingForm.UpdateProgressHandler(10, "데이터 로드 준비 중...");
                        await Task.Delay(10);

                        await PerformLoadPagedData(loadingForm.UpdateProgressHandler);

                        await loadingForm.UpdateProgressHandler(100, "데이터 로드 완료");
                        await Task.Delay(100);
                        loadingForm.Close();
                    }
                }
                else
                {
                    // 외부에서 프로그레스바가 이미 표시되고 있는 경우
                    await PerformLoadPagedData(null);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"페이지 데이터 로드 중 오류: {ex.Message}\n{ex.StackTrace}");
                if (!isAlreadyProgress) // 이미 외부 프로그레스바가 있으면 메시지 박스를 표시하지 않음
                {
                    MessageBox.Show($"데이터 로드 중 오류 발생: {ex.Message}", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                isProcessingSearch = false;
            }
        }

        // 실제 데이터 로드 로직을 분리하는 헬퍼 메서드
        // 실제 데이터 로드 로직을 분리하는 헬퍼 메서드
        // *** 2. 개선된 PerformLoadPagedData 함수 (캐시 제거 + 간단한 실시간 조회) ***
        private async Task PerformLoadPagedData(ProcessProgressForm.UpdateProgressDelegate progressHandler = null)
        {
            List<ColumnMappingDocument> visibleColumns = null;
            DataTable pageData = null;

            await Task.Run(async () =>
            {
                try
                {
                    // 1. MongoDB에서 visible 컬럼 목록 조회
                    var columnMappingRepo = new ColumnMappingRepository();
                    visibleColumns = await columnMappingRepo.GetVisibleColumnsAsync();
                    Debug.WriteLine($"조회된 가시적 컬럼 수: {visibleColumns.Count}");

                    if (progressHandler != null)
                    {
                        await progressHandler(20, "컬럼 정보 로드 완료");
                    }

                    // 2. MongoDB에서 raw_data 로드
                    var mongoConverter = new MongoDataConverter();
                    var (documents, totalCount) = await mongoConverter.GetPagedRawDataAsync(
                        currentPage, pageSize, DataHandler.hiddenData);

                    // 메타데이터 업데이트
                    totalRows = (int)totalCount;
                    totalPages = (int)Math.Ceiling((double)totalRows / pageSize);

                    if (progressHandler != null)
                    {
                        await progressHandler(50, "Raw 데이터 로드 완료");
                    }

                    // 3. 현재 페이지 데이터의 raw_data ID 목록 추출
                    var currentPageRawDataIds = documents.Select(d => d.Id).ToList();

                    // 4. 현재 페이지 데이터에 대해서만 클러스터명 매핑 조회
                    var clusterNameMapping = await GetClusterNameMappingForPageAsync(currentPageRawDataIds);
                    Debug.WriteLine($"현재 페이지 클러스터 매핑: {clusterNameMapping.Count}개 항목");

                    // *** 추가: 세부클러스터명 매핑도 함께 조회 ***
                    var detailClusterNameMapping = await GetDetailClusterNameMappingForPageAsync(currentPageRawDataIds);
                    Debug.WriteLine($"현재 페이지 세부클러스터 매핑: {detailClusterNameMapping.Count}개 항목");


                    if (progressHandler != null)
                    {
                        await progressHandler(65, "클러스터 매핑 조회 완료");
                    }

                    // 5. MongoDB 문서를 DataTable로 변환 (클러스터명 포함)
                    //pageData = ConvertRawDocumentsToDataTableWithClusterName(documents, clusterNameMapping);
                    pageData = ConvertRawDocumentsToDataTableWithClusterName(documents, clusterNameMapping, detailClusterNameMapping);
                    Debug.WriteLine($"변환된 pageData: {pageData.Rows.Count}행, 클러스터명 매핑: {GetClusterNameMappingStats(pageData)}");

                    if (progressHandler != null)
                    {
                        await progressHandler(70, "데이터 변환 완료");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"데이터 로드 작업 중 오류: {ex.Message}");
                    throw;
                }
            });

            if (progressHandler != null)
            {
                await progressHandler(80, "UI 업데이트 중...");
            }

            // UI 업데이트
            try
            {
                if (pageData != null)
                {
                    // 원본 그리드와 키워드 그리드 모두 동일한 데이터로 설정
                    ConfigureDataGridView(pageData, dataGridView_origin);
                    ConfigureDataGridView(pageData, dataGridView_keyword);

                    Debug.WriteLine($"dataGridView_keyword 설정 완료 (컬럼 수: {dataGridView_keyword.Columns.Count})");

                    // 컬럼 가시성 적용
                    if (visibleColumns != null && visibleColumns.Count > 0)
                    {
                        ApplyColumnVisibilityExplicit(dataGridView_keyword, visibleColumns);
                        Debug.WriteLine("컬럼 가시성 적용 완료");
                    }

                    // 클러스터명 컬럼 스타일 적용
                    ApplyClusterNameColumnStyle(dataGridView_keyword);
                }

                await AddSelectedColumnToGridAsync(dataGridView_delete_col2, dataGridView_keyword);
                Debug.WriteLine($"dataGridView_delete_col2 설정 완료 (행 수: {dataGridView_delete_col2.Rows.Count})");

                UpdatePaginationInfo();
                ApplyGridFormatting();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UI 업데이트 중 오류: {ex.Message}\\n{ex.StackTrace}");
            }

            if (progressHandler != null)
            {
                await progressHandler(90, "데이터 로드 마무리 중...");
            }
        }

        // GetClusterNameMappingForPageAsync 함수 다음에 추가
        private async Task<Dictionary<string, string>> GetDetailClusterNameMappingForPageAsync(List<string> rawDataIds)
        {
            var mappingDict = new Dictionary<string, string>();

            if (rawDataIds == null || rawDataIds.Count == 0)
                return mappingDict;

            try
            {
                var clusteringRepo = new ClusteringRepository();

                // cluster_sub_id == cluster_number인 세부 상위 클러스터만 조회
                var filter = Builders<ClusteringResultDocument>.Filter.Where(c =>
                    c.ClusterSubId == c.ClusterNumber && c.ClusterSubId > 0);
                var detailClusters = await clusteringRepo.FindDocumentsAsync(filter);

                // 현재 페이지의 raw_data ID에 대해서만 매핑 생성
                foreach (var cluster in detailClusters)
                {
                    if (cluster.DataIndices != null && !string.IsNullOrEmpty(cluster.ClusterName))
                    {
                        foreach (var dataIndex in cluster.DataIndices)
                        {
                            if (rawDataIds.Contains(dataIndex) && !mappingDict.ContainsKey(dataIndex))
                            {
                                mappingDict[dataIndex] = cluster.ClusterName;
                            }
                        }
                    }
                }

                Debug.WriteLine($"현재 페이지 세부클러스터 매핑 생성: {rawDataIds.Count}개 ID 중 {mappingDict.Count}개 매핑");
                return mappingDict;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세부클러스터명 매핑 조회 중 오류: {ex.Message}");
                return mappingDict;
            }
        }

        /// <summary>
        /// 세션 완료 정보 업데이트
        /// </summary>
        private async Task<bool> UpdateSessionCompletionAsync(string resultFilePath)
        {
            try
            {
                ObjectId currentSessionId = DataHandler_classification.GetCurrentSessionId();

                if (currentSessionId == ObjectId.Empty)
                {
                    Debug.WriteLine("현재 세션 ID가 설정되지 않아 세션 업데이트를 건너뜁니다.");
                    return false;
                }

                var fileSessionRepo = new FileSessionRepository();

                // 세션 정보 업데이트
                bool updateResult = await fileSessionRepo.UpdateSessionCompletionAsync(
                    currentSessionId,
                    "completed",
                    DateTime.UtcNow,
                    resultFilePath
                );

                if (updateResult)
                {
                    Debug.WriteLine($"세션 완료 정보 업데이트 성공: {currentSessionId}");
                    Debug.WriteLine($"결과 파일 경로: {resultFilePath}");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"세션 완료 정보 업데이트 실패: {currentSessionId}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 완료 정보 업데이트 중 오류: {ex.Message}");
                return false;
            }
        }


    }
}
