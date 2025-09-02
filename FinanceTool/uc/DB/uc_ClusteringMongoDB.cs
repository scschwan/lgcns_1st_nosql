using FinanceTool.MongoModels;
using FinanceTool.Repositories;
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
    public partial class uc_Clustering
    {
        // MongoDB에 클러스터링 데이터 저장하는 새 헬퍼 메서드
        private async Task SaveClusteringDataToMongoDBAsync(DataTable clusteringData)
        {
            try
            {
                var clusteringRepo = new ClusteringRepository();
                List<ClusteringResultDocument> documents = new List<ClusteringResultDocument>();

                foreach (DataRow row in clusteringData.Rows)
                {
                    int clusterId = -1;
                    int clusterNumber = Convert.ToInt32(row["ID"]);

                    // ClusterID 처리 (병합 상태 확인)
                    if (row["ClusterID"] != DBNull.Value)
                    {
                        clusterId = Convert.ToInt32(row["ClusterID"]);
                    }

                    var clusterDoc = new ClusteringResultDocument
                    {
                        ClusterNumber = clusterNumber,
                        ClusterId = clusterId,
                        ClusterName = row["클러스터명"].ToString(),
                        Keywords = row["키워드목록"].ToString().Split(',').Select(k => k.Trim()).ToList(),
                        Count = Convert.ToInt32(row["Count"]),
                        TotalAmount = Convert.ToDecimal(row["합산금액"])
                    };

                    // dataIndex 처리
                    if (!row.IsNull("dataIndex") && !string.IsNullOrEmpty(row["dataIndex"].ToString()))
                    {
                        /*
                        clusterDoc.DataIndices = row["dataIndex"].ToString()
                                               .Split(',')
                                               .Select(id => id.Trim())
                                               .Where(id => !string.IsNullOrEmpty(id))
                                               .ToList();
                        */
                        //2025.05.29
                        //대용량 처리 개선
                        var allIndices = row["dataIndex"].ToString()
                        .Split(',')
                        .Select(id => id.Trim())
                        .Where(id => !string.IsNullOrEmpty(id))
                        .ToList();

                        // 16MB 제한 고려하여 최대 50만개로 제한
                        clusterDoc.DataIndices = allIndices.Count > 50000 ?
                            allIndices.Take(50000).ToList() :
                            allIndices;

                        if (allIndices.Count > 50000)
                        {
                            Debug.WriteLine($"경고: 클러스터 {clusterDoc.ClusterNumber}의 DataIndices가 제한을 초과했습니다. ({allIndices.Count}개)");
                        }
                    }

                    documents.Add(clusterDoc);
                }

                // 데이터 일괄 저장
                if (documents.Count > 0)
                {
                    //await clusteringRepo.CreateManyAsync(documents);
                    // 수정된 코드: 배치별로 분할 저장
                    const int batchSize = 10000; // 문서별 배치 크기
                    for (int i = 0; i < documents.Count; i += batchSize)
                    {
                        var batch = documents.Skip(i).Take(batchSize).ToList();
                        await clusteringRepo.CreateManyAsync(batch);
                    }
                    Debug.WriteLine($"{documents.Count}개의 클러스터 데이터를 MongoDB에 저장했습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터 데이터 MongoDB 저장 오류: {ex.Message}");
            }
        }


        public async Task<DataTable> EnrichWithRawTableDataAsync(DataTable inputTable)
        {
            DataTable resultTable = inputTable.Copy();
            Debug.WriteLine("EnrichWithRawTableDataAsync start!!");
            try
            {
                // 1. MongoDB에서 is_visible=true인 컬럼 목록 가져오기
                var columnMappingRepo = new ColumnMappingRepository();
                var visibleColumns = await columnMappingRepo.GetVisibleColumnsAsync();

                if (visibleColumns.Count == 0)
                {
                    Debug.WriteLine("표시할 컬럼이 없습니다. column_mapping 컬렉션을 확인하세요.");
                    return resultTable;
                }

                // 2. 결과 테이블에 컬럼 추가
                foreach (var column in visibleColumns)
                {
                    if (!resultTable.Columns.Contains(column.OriginalName))
                    {
                        resultTable.Columns.Add(column.OriginalName, typeof(string));
                    }
                }

                // 3. 모든 행에서 조회할 ID 목록 수집
                HashSet<string> rawDataIds = new HashSet<string>();
                Dictionary<string, List<DataRow>> idToRowsMap = new Dictionary<string, List<DataRow>>();

                foreach (DataRow row in resultTable.Rows)
                {
                    string dataIndices = row["dataIndex"]?.ToString();
                    if (string.IsNullOrEmpty(dataIndices))
                        continue;

                    // 쉼표로 구분된 경우 모든 ID를 처리
                    string[] indices = dataIndices.Split(',');
                    foreach (string indexStr in indices)
                    {
                        string trimmedIndex = indexStr.Trim();
                        if (string.IsNullOrEmpty(trimmedIndex))
                            continue;

                        rawDataIds.Add(trimmedIndex);

                        // ID를 키로, 해당 ID를 참조하는 행들을 값으로 저장
                        if (!idToRowsMap.ContainsKey(trimmedIndex))
                        {
                            idToRowsMap[trimmedIndex] = new List<DataRow>();
                        }
                        idToRowsMap[trimmedIndex].Add(row);
                    }
                }

                if (rawDataIds.Count == 0)
                    return resultTable;

                // 4. MongoDB에서 raw_data 문서 조회
                var rawDataRepo = new RawDataRepository();
                //var filter = Builders<RawDataDocument>.Filter.In(d => d.Id, rawDataIds.ToList());
                //var rawDataDocuments = await rawDataRepo.FindDocumentsAsync(filter);

                //2025.05.29
                //대용량 batch 처리
                // 수정된 코드
                const int batchSize = 10000;
                var allRawDataDocuments = new List<RawDataDocument>();
                var rawDataIdsList = rawDataIds.ToList();

                // 배치별로 분할하여 조회
                for (int i = 0; i < rawDataIdsList.Count; i += batchSize)
                {
                    var batchIds = rawDataIdsList.Skip(i).Take(batchSize).ToList();
                    var filter = Builders<RawDataDocument>.Filter.In(d => d.Id, batchIds);
                    var batchDocuments = await rawDataRepo.FindDocumentsAsync(filter);
                    allRawDataDocuments.AddRange(batchDocuments);
                }

                var rawDataDocuments = allRawDataDocuments;

                // 5. 조회된 데이터를 결과 테이블에 매핑
                foreach (var doc in rawDataDocuments)
                {
                    if (idToRowsMap.ContainsKey(doc.Id))
                    {
                        foreach (DataRow resultRow in idToRowsMap[doc.Id])
                        {
                            foreach (var column in visibleColumns)
                            {
                                string columnName = column.OriginalName;
                                if (doc.Data.ContainsKey(columnName))
                                {
                                    resultRow[columnName] = doc.Data[columnName]?.ToString() ?? "";
                                }
                            }
                        }
                    }
                }
                Debug.WriteLine("EnrichWithRawTableDataAsync end!!");
                Debug.WriteLine($"raw_data 문서 {rawDataDocuments.Count}개로 클러스터링 데이터를 보강했습니다.");
                return resultTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RAW_TABLE 데이터 추가 중 오류 발생: {ex.Message}");
                return resultTable;
            }
        }


        public async Task MergeAndCreateNewCluster(DataTable dataTable, List<int> targetIds,
     string clusterName = null, string clusterID = null)
        {
            if (targetIds == null || targetIds.Count == 0) return;

            try
            {
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 병합 시작: {targetIds.Count}개");

                var clusteringRepo = new ClusteringRepository();

                // 새 클러스터 번호 결정
                int newClusterNumber;
                bool isNewCluster = true;
                ClusteringResultDocument existingCluster = null;

                if (clusterID != null && int.TryParse(clusterID, out newClusterNumber))
                {
                    isNewCluster = false;
                    existingCluster = await clusteringRepo.GetByClusterNumberAsync(newClusterNumber);
                    if (existingCluster == null)
                        throw new Exception($"클러스터 번호 {newClusterNumber} 존재하지 않습니다.");
                }
                else
                {
                    newClusterNumber = await clusteringRepo.GetNextClusterNumberAsync();
                }

                // 2단계: 메모리에서 대상 행들의 데이터 수집
                var targetRowsData = new List<ClusteringResultDocument>();

                foreach (int targetId in targetIds)
                {
                    var row = dataTable.AsEnumerable()
                        .FirstOrDefault(r => Convert.ToInt32(r["ID"]) == targetId);

                    if (row != null)
                    {
                        var rowData = new ClusteringResultDocument
                        {
                            ClusterNumber = targetId,
                            ClusterName = row["클러스터명"]?.ToString() ?? "",
                            Keywords = row["키워드목록"]?.ToString()?.Split(',')
                                .Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)).ToList() ?? new List<string>(),
                            Count = Convert.ToInt32(row["Count"]),
                            TotalAmount = Convert.ToDecimal(row["합산금액"]),
                            DataIndices = row["dataIndex"]?.ToString()?.Split(',')
                                .Select(i => i.Trim()).Where(i => !string.IsNullOrEmpty(i)).ToList() ?? new List<string>()
                        };
                        targetRowsData.Add(rowData);
                    }
                }

                // 3단계: 클러스터명 생성
                string mergedClusterName = clusterName ??
                    string.Join("_", targetRowsData.Select(r => r.ClusterName).Take(3)) +
                    (targetRowsData.Count > 3 ? "..." : "");

                if (mergedClusterName.Length > 20)
                    mergedClusterName = mergedClusterName.Substring(0, 17) + "...";

                // *** 4단계: 기존 클러스터에 추가하는 경우 데이터 누적 ***
                if (!isNewCluster && existingCluster != null)
                {
                    Debug.WriteLine($"기존 클러스터 {newClusterNumber}에 데이터 누적");

                    // 키워드 병합 (중복 제거)
                    var allKeywords = new HashSet<string>(existingCluster.Keywords ?? new List<string>());
                    foreach (var target in targetRowsData)
                    {
                        foreach (var keyword in target.Keywords)
                        {
                            allKeywords.Add(keyword);
                        }
                    }

                    // DataIndices 병합 (중복 제거)
                    var allDataIndices = new HashSet<string>(existingCluster.DataIndices ?? new List<string>());
                    foreach (var target in targetRowsData)
                    {
                        foreach (var index in target.DataIndices)
                        {
                            allDataIndices.Add(index);
                        }
                    }

                    // 새로운 누적 데이터 계산
                    int newCount = existingCluster.Count + targetRowsData.Sum(t => t.Count);
                    decimal newTotalAmount = existingCluster.TotalAmount + targetRowsData.Sum(t => t.TotalAmount);

                    // *** ClusteringRepository의 UpdateClusterFullInfoAsync 메서드 사용 ***
                    bool updateSuccess = await clusteringRepo.UpdateClusterFullInfoAsync(
                        newClusterNumber,
                        string.IsNullOrEmpty(clusterName) ? existingCluster.ClusterName : clusterName,
                        allKeywords.ToList(),
                        newCount,
                        newTotalAmount,
                        allDataIndices.ToList()
                    );

                    if (!updateSuccess)
                    {
                        Debug.WriteLine($"기존 클러스터 {newClusterNumber} 업데이트 실패");
                        return;
                    }

                    Debug.WriteLine($"기존 클러스터 {newClusterNumber} 누적 완료: " +
                                   $"Count {existingCluster.Count} -> {newCount}, " +
                                   $"Amount {existingCluster.TotalAmount} -> {newTotalAmount}");
                }
                else
                {
                    // *** 5단계: 새 클러스터 생성 (ClusteringRepository의 MergeOrUpdateClusterAsync 사용) ***
                    Debug.WriteLine($"새 클러스터 {newClusterNumber} 생성");

                    int mergedClusterNumber = await clusteringRepo.MergeOrUpdateClusterAsync(
                        targetIds,
                        mergedClusterName,
                        0 // 새 클러스터 생성
                    );

                    if (mergedClusterNumber != newClusterNumber)
                    {
                        Debug.WriteLine($"예상한 클러스터 번호 {newClusterNumber}와 실제 생성된 번호 {mergedClusterNumber}가 다름");
                        newClusterNumber = mergedClusterNumber;
                    }
                }

                // *** 6단계: DataTable 업데이트 (기존 행 업데이트 + 병합된 클러스터들의 ClusterID 변경) ***
                await UpdateDataTableAfterMerge(dataTable, targetIds, newClusterNumber, isNewCluster);

                // *** 7단계: 데이터 보강 (동기적 처리로 일관성 보장) ***
                mergeClusterDataTable = await EnrichWithRawTableDataAsync(dataTable);

                // *** 8단계: ClusteringManager 데이터 새로고침 ***
                if (_clusteringManager != null)
                {
                    await _clusteringManager.RefreshDataAsync(mergeClusterDataTable);
                }

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 병합 완료: {newClusterNumber}");

                // 병합 클러스터 리스트 생성
                create_check_keyword_list();

                // 병합 작업 후 업데이트
                UpdateModifiedDataGridView();
                UpdateSupplySummaryDataGridView();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 병합 오류: {ex.Message}");
                throw;
            }
        }
        // *** 새로 추가: DataTable 업데이트 헬퍼 메서드 ***
        private async Task UpdateDataTableAfterMerge(DataTable dataTable, List<int> targetIds,
            int newClusterNumber, bool isNewCluster)
        {
            await Task.Run(() =>
            {
                try
                {
                    // 병합된 클러스터들의 ClusterID 업데이트
                    foreach (int targetId in targetIds)
                    {
                        var targetRow = dataTable.AsEnumerable()
                            .FirstOrDefault(row => Convert.ToInt32(row["ID"]) == targetId);

                        if (targetRow != null)
                        {
                            targetRow["ClusterID"] = newClusterNumber;
                        }
                    }

                    // 기존 클러스터에 update
                    if (!isNewCluster)
                    {
                        var existingRow = dataTable.AsEnumerable()
                            .FirstOrDefault(row => Convert.ToInt32(row["ID"]) == newClusterNumber);

                        if (existingRow != null)
                        {
                            // MongoDB에서 최신 데이터를 가져와서 DataTable 업데이트
                            var clusteringRepo = new ClusteringRepository();
                            var updatedCluster = clusteringRepo.GetByClusterNumberAsync(newClusterNumber).Result;

                            if (updatedCluster != null)
                            {
                                existingRow["클러스터명"] = updatedCluster.ClusterName;
                                existingRow["Count"] = updatedCluster.Count;
                                existingRow["합산금액"] = updatedCluster.TotalAmount;
                                existingRow["키워드목록"] = string.Join(",", updatedCluster.Keywords);
                                existingRow["dataIndex"] = string.Join(",", updatedCluster.DataIndices);
                            }

                            // 병합되는 클러스터들의 ClusterID를 기존 클러스터 번호로 변경
                            foreach (int targetId in targetIds)
                            {
                                var updatedElement = clusteringRepo.UpdateClusterIdAsync(targetId, newClusterNumber);

                                if (updatedElement != null)
                                {
                                    //Debug.WriteLine($"MongoDB에서 클러스터 {targetId}의 cluster_id를 {newClusterNumber}로 변경");

                                    var targetRow = dataTable.AsEnumerable()
                                        .FirstOrDefault(row => Convert.ToInt32(row["ID"]) == targetId);

                                    if (targetRow != null)
                                    {
                                        targetRow["ClusterID"] = newClusterNumber;
                                        //Debug.WriteLine($"클러스터 {targetId}의 ClusterID를 {newClusterNumber}로 변경");
                                    }
                                }

                            }
                        }

                    }
                    // 새 클러스터가 생성된 경우, 기존 DataTable에서 상위 클러스터 행 찾아서 업데이트
                    else
                    {
                        // *** 핵심 수정: 새 클러스터 행을 DataTable에 추가 ***
                        var clusteringRepo = new ClusteringRepository();
                        var newCluster = clusteringRepo.GetByClusterNumberAsync(newClusterNumber).Result;

                        if (newCluster != null)
                        {
                            DataRow newRow = dataTable.NewRow();
                            newRow["ID"] = newCluster.ClusterNumber;
                            newRow["ClusterID"] = newCluster.ClusterId;
                            // *** ClusterSubID 컬럼이 있으면 설정, 없으면 무시 ***
                            if (dataTable.Columns.Contains("ClusterSubID"))
                            {
                                newRow["ClusterSubID"] = newCluster.ClusterSubId;
                            }
                            newRow["클러스터명"] = newCluster.ClusterName;
                            newRow["키워드목록"] = string.Join(",", newCluster.Keywords);
                            newRow["Count"] = newCluster.Count;
                            newRow["합산금액"] = newCluster.TotalAmount;
                            newRow["dataIndex"] = string.Join(",", newCluster.DataIndices);

                            dataTable.Rows.Add(newRow); // ← 새 행 추가

                            Debug.WriteLine($"새 클러스터 행 추가: ID={newCluster.ClusterNumber}");

                            // 병합되는 클러스터들의 ClusterID를 기존 클러스터 번호로 변경
                            foreach (int targetId in targetIds)
                            {
                                var targetRow = dataTable.AsEnumerable()
                                         .FirstOrDefault(row => Convert.ToInt32(row["ID"]) == targetId);

                                if (targetRow != null)
                                {
                                    targetRow["ClusterID"] = newClusterNumber;
                                    //Debug.WriteLine($"클러스터 {targetId}의 ClusterID를 {newClusterNumber}로 변경");
                                }
                            }
                        }
                    }

                    // *** 이제 AcceptChanges() 호출 ***
                    dataTable.AcceptChanges();

                    Debug.WriteLine($"DataTable 업데이트 완료: {targetIds.Count}개 행 처리");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"DataTable 업데이트 오류: {ex.Message}");
                }
            });
        }


        public async Task deleteClusterId(DataTable dataTable, List<int> targetIds)
        {
            try
            {
                Debug.WriteLine($"병합 해제 대상 ID: {string.Join(", ", targetIds)}");
                var clusteringRepo = new ClusteringRepository();

                // *** 1단계: 영향받는 세부 클러스터 파악 및 재계산 ***
                await UpdateAffectedSubClusters(targetIds, clusteringRepo, dataTable);

                // *** 추가: 세부클러스터링 객체(id=sub_cluster_id) 삭제 처리 ***
                await DeleteSubClusteringObjects(targetIds, clusteringRepo, dataTable);

                // *** 2단계: 기존 로직 (ClusterSubID 초기화 추가) ***
                // 삭제할 행들을 찾아서 리스트에 담기
                var rowsToDelete = dataTable.AsEnumerable()
                    .Where(row => targetIds.Contains(Convert.ToInt32(row["ID"])))
                    .ToList();

                // 찾은 행들을 삭제
                foreach (var row in rowsToDelete)
                {
                    dataTable.Rows.Remove(row);
                }

                // 병합된 하위 클러스터들 찾기
                var childRows = dataTable.AsEnumerable()
                    .Where(row => row["ClusterID"] != DBNull.Value &&
                           targetIds.Contains(Convert.ToInt32(row["ClusterID"])))
                    .ToList();

                Debug.WriteLine($"병합 해제할 하위 클러스터 수: {childRows.Count}");

                // 하위 클러스터들의 ClusterID와 ClusterSubID 모두 초기화
                foreach (var row in childRows)
                {
                    row["ClusterID"] = -1; // 미병합 상태로 변경
                    row["ClusterSubID"] = -1; // *** 신규 추가: 세부 클러스터도 초기화 ***
                }

                // 변경사항 적용
                dataTable.AcceptChanges();

                // *** 3단계: MongoDB에서도 삭제 및 상태 재설정 ***
                foreach (int targetId in targetIds)
                {
                    // 1. 삭제할 클러스터 정보 조회
                    var cluster = await clusteringRepo.GetByClusterNumberAsync(targetId);
                    if (cluster != null)
                    {
                        // 2. 이 클러스터에 병합된 다른 클러스터들의 상태 재설정
                        var childClusters = await clusteringRepo.GetChildClustersAsync(targetId);
                        foreach (var child in childClusters)
                        {
                            await clusteringRepo.UpdateClusterIdAsync(child.ClusterNumber, -1);
                            await clusteringRepo.UpdateClusterSubIdAsync(child.ClusterNumber, -1); // *** 신규 추가 ***
                        }

                        // 3. 클러스터 자체 삭제
                        await clusteringRepo.DeleteByClusterNumberAsync(targetId);
                    }
                }

                // *** 4단계: UI 새로고침 ***
                mergeClusterDataTable = await EnrichWithRawTableDataAsync(dataTable);
                await _clusteringManager.RefreshDataAsync(mergeClusterDataTable);
                var searchCriteria = CreateSearchCriteriaFromCurrentUI();
                await _clusteringManager.SearchAsync(searchCriteria);

                Debug.WriteLine("완전한 병합 해제 완료 - 세부 클러스터 데이터 정합성 보장");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터 삭제 오류: {ex.Message}");
                MessageBox.Show($"클러스터 삭제 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 세부클러스터링 객체(id=sub_cluster_id) 삭제 처리
        /// </summary>
        private async Task DeleteSubClusteringObjects(List<int> targetIds, ClusteringRepository repo, DataTable dataTable)
        {
            try
            {
                Debug.WriteLine("세부클러스터링 객체 삭제 처리 시작");

                // 1. 삭제할 클러스터들이 가지고 있는 모든 sub_cluster_id 수집
                var subClusterIdsToDelete = new HashSet<int>();

                foreach (int targetId in targetIds)
                {
                    // 해당 클러스터에 속한 모든 하위 클러스터들의 sub_cluster_id 수집
                    var childClusters = await repo.GetChildClustersAsync(targetId);
                    foreach (var child in childClusters)
                    {
                        if (child.ClusterSubId > 0)
                        {
                            subClusterIdsToDelete.Add(child.ClusterSubId);
                        }
                    }

                    // 삭제할 클러스터 자체의 sub_cluster_id도 수집
                    var cluster = await repo.GetByClusterNumberAsync(targetId);
                    if (cluster != null && cluster.ClusterSubId > 0)
                    {
                        subClusterIdsToDelete.Add(cluster.ClusterSubId);
                    }
                }

                Debug.WriteLine($"삭제할 세부클러스터링 객체 ID: {string.Join(", ", subClusterIdsToDelete)}");

                // 2. 각 세부클러스터링 객체별로 삭제 처리
                foreach (int subClusterId in subClusterIdsToDelete)
                {
                    // MongoDB에서 삭제
                    await repo.DeleteByClusterNumberAsync(subClusterId);

                    // DataTable에서도 삭제
                    var rowsToDelete = dataTable.AsEnumerable()
                        .Where(row => Convert.ToInt32(row["ID"]) == subClusterId)
                        .ToList();

                    foreach (var row in rowsToDelete)
                    {
                        dataTable.Rows.Remove(row);
                        Debug.WriteLine($"세부클러스터링 객체 {subClusterId} 삭제 완료");
                    }
                }

                Debug.WriteLine("모든 세부클러스터링 객체 삭제 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세부클러스터링 객체 삭제 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 영향받는 세부 클러스터들의 데이터를 재계산하고 업데이트하는 함수
        /// </summary>
        /// <param name="targetIds">병합 해제할 클러스터 ID 목록</param>
        /// <param name="repo">클러스터링 레포지토리</param>
        /// <param name="dataTable">메모리상의 데이터테이블</param>
        private async Task UpdateAffectedSubClusters(List<int> targetIds, ClusteringRepository repo, DataTable dataTable)
        {
            try
            {
                Debug.WriteLine($"영향받는 세부 클러스터 업데이트 시작: 대상 {targetIds.Count}개");

                // *** 1단계: 해제할 클러스터들이 속한 세부 클러스터 ID 수집 ***
                var affectedSubClusterIds = new HashSet<int>();
                var clusterToSubClusterMap = new Dictionary<int, int>(); // 클러스터ID -> 세부클러스터ID 매핑

                foreach (int targetId in targetIds)
                {
                    var cluster = await repo.GetByClusterNumberAsync(targetId);
                    if (cluster != null && cluster.ClusterSubId > 0)
                    {
                        affectedSubClusterIds.Add(cluster.ClusterSubId);
                        clusterToSubClusterMap[targetId] = cluster.ClusterSubId;
                        Debug.WriteLine($"클러스터 {targetId}는 세부클러스터 {cluster.ClusterSubId}에 속함");
                    }
                }

                if (affectedSubClusterIds.Count == 0)
                {
                    Debug.WriteLine("영향받는 세부 클러스터가 없음 - 세부 클러스터 업데이트 건너뜀");
                    return;
                }

                Debug.WriteLine($"영향받는 세부 클러스터: {string.Join(", ", affectedSubClusterIds)}");

                // *** 2단계: 각 세부 클러스터별로 재계산 수행 ***
                foreach (int subClusterId in affectedSubClusterIds)
                {
                    await UpdateSingleSubCluster(subClusterId, targetIds, repo, dataTable);
                }

                Debug.WriteLine("모든 영향받는 세부 클러스터 업데이트 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세부 클러스터 업데이트 오류: {ex.Message}");
                throw; // 상위로 예외 전파
            }
        }

        /// <summary>
        /// 단일 세부 클러스터의 데이터를 재계산하고 업데이트하는 함수
        /// </summary>
        /// <param name="subClusterId">업데이트할 세부 클러스터 ID</param>
        /// <param name="removingClusterIds">제거될 클러스터 ID 목록</param>
        /// <param name="repo">클러스터링 레포지토리</param>
        /// <param name="dataTable">메모리상의 데이터테이블</param>
        private async Task UpdateSingleSubCluster(int subClusterId, List<int> removingClusterIds,
            ClusteringRepository repo, DataTable dataTable)
        {
            try
            {
                Debug.WriteLine($"세부 클러스터 {subClusterId} 업데이트 시작");

                // *** 1단계: 현재 세부 클러스터 정보 조회 ***
                var currentSubCluster = await repo.GetByClusterNumberAsync(subClusterId);
                if (currentSubCluster == null)
                {
                    Debug.WriteLine($"세부 클러스터 {subClusterId}를 찾을 수 없음");
                    return;
                }

                // *** 2단계: 해당 세부 클러스터에 속한 모든 하위 클러스터 조회 ***
                var allSubChildren = await repo.GetSubChildClustersAsync(subClusterId);
                Debug.WriteLine($"세부 클러스터 {subClusterId}의 전체 하위 클러스터: {allSubChildren.Count}개");

                // *** 3단계: 제거될 클러스터들을 제외한 나머지 클러스터들만 필터링 ***
                var remainingChildren = allSubChildren
                    .Where(child => !removingClusterIds.Contains(child.ClusterNumber))
                    .ToList();

                Debug.WriteLine($"제거 후 남은 하위 클러스터: {remainingChildren.Count}개");

                // *** 4단계: 남은 하위 클러스터가 없으면 세부 클러스터 삭제 ***
                if (remainingChildren.Count == 0)
                {
                    Debug.WriteLine($"세부 클러스터 {subClusterId}에 남은 하위 클러스터가 없어 삭제");

                    // MongoDB에서 삭제
                    await repo.DeleteByClusterNumberAsync(subClusterId);

                    // DataTable에서도 해당 행 삭제
                    var rowsToDelete = dataTable.AsEnumerable()
                        .Where(row => Convert.ToInt32(row["ID"]) == subClusterId)
                        .ToList();

                    foreach (var row in rowsToDelete)
                    {
                        dataTable.Rows.Remove(row);
                        Debug.WriteLine($"DataTable에서 세부 클러스터 {subClusterId} 행 삭제");
                    }

                    // 삭제 로직 후 즉시 적용
                    dataTable.AcceptChanges();

                    return;
                }

                // *** 5단계: 남은 하위 클러스터들로 새로운 집계 데이터 계산 ***
                int newCount = remainingChildren.Sum(child => child.Count);
                decimal newTotalAmount = remainingChildren.Sum(child => child.TotalAmount);

                // 키워드 중복 제거하여 병합
                var newKeywords = new HashSet<string>();
                foreach (var child in remainingChildren)
                {
                    foreach (var keyword in child.Keywords ?? new List<string>())
                    {
                        if (!string.IsNullOrWhiteSpace(keyword))
                        {
                            newKeywords.Add(keyword.Trim());
                        }
                    }
                }

                // DataIndices 중복 제거하여 병합
                var newDataIndices = new HashSet<string>();
                foreach (var child in remainingChildren)
                {
                    foreach (var index in child.DataIndices ?? new List<string>())
                    {
                        if (!string.IsNullOrWhiteSpace(index))
                        {
                            newDataIndices.Add(index.Trim());
                        }
                    }
                }

                Debug.WriteLine($"세부 클러스터 {subClusterId} 재계산 결과:");
                Debug.WriteLine($"  Count: {currentSubCluster.Count} → {newCount}");
                Debug.WriteLine($"  Amount: {currentSubCluster.TotalAmount} → {newTotalAmount}");
                Debug.WriteLine($"  Keywords: {currentSubCluster.Keywords?.Count ?? 0} → {newKeywords.Count}");
                Debug.WriteLine($"  DataIndices: {currentSubCluster.DataIndices?.Count ?? 0} → {newDataIndices.Count}");

                // *** 6단계: MongoDB에서 세부 클러스터 정보 업데이트 ***
                bool updateSuccess = await repo.UpdateClusterFullInfoAsync(
                    subClusterId,
                    currentSubCluster.ClusterName, // 클러스터명은 유지
                    newKeywords.ToList(),
                    newCount,
                    newTotalAmount,
                    newDataIndices.ToList()
                );

                if (!updateSuccess)
                {
                    throw new Exception($"세부 클러스터 {subClusterId} MongoDB 업데이트 실패");
                }

                // *** 7단계: DataTable에서도 해당 행 업데이트 ***
                var memoryRows = dataTable.AsEnumerable()
                    .Where(row => Convert.ToInt32(row["ID"]) == subClusterId)
                    .ToList();

                foreach (var row in memoryRows)
                {
                    row["Count"] = newCount;
                    row["합산금액"] = newTotalAmount;
                    row["키워드목록"] = string.Join(",", newKeywords);
                    row["dataIndex"] = string.Join(",", newDataIndices);

                    Debug.WriteLine($"DataTable에서 세부 클러스터 {subClusterId} 행 업데이트 완료");
                }

                // 업데이트 로직 후 즉시 적용
                dataTable.AcceptChanges();

                Debug.WriteLine($"세부 클러스터 {subClusterId} 업데이트 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세부 클러스터 {subClusterId} 업데이트 오류: {ex.Message}");
                throw; // 상위로 예외 전파
            }
        }
    }
}
