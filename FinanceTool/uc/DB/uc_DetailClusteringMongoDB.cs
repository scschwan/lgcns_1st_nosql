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
    public partial class uc_DetailClustering
    {

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
                            ClusterId = _parentClusterId,
                            ClusterSubId = Convert.ToInt32(row["ClusterSubID"]),
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
                    //var allKeywords = new HashSet<string>(existingCluster.Keywords ?? new List<string>());

                    // 기존 세부 클러스터에 추가
                    var allKeywords = new HashSet<string>(existingCluster.Keywords ?? new List<string>());
                    var allDataIndices = new HashSet<string>(existingCluster.DataIndices ?? new List<string>());


                    foreach (var target in targetRowsData)
                    {
                        foreach (var keyword in target.Keywords)
                            allKeywords.Add(keyword);
                        foreach (var index in target.DataIndices)
                            allDataIndices.Add(index);
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

                    int mergedClusterNumber = await clusteringRepo.MergeDetailClustersAsync(
                        targetIds,
                        mergedClusterName,
                        _parentClusterId // 새 클러스터 생성
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
                //_detailClusteringData = dataTable;
                //_detailClusteringData.AcceptChanges();
                //mergeClusterDataTable = await EnrichWithRawTableDataAsync(_detailClusteringData);

                dataTable.AcceptChanges();
                mergeClusterDataTable = await EnrichWithRawTableDataAsync(dataTable);

                // *** 8단계: ClusteringManager 데이터 새로고침 ***
                if (_clusteringManager != null)
                {
                    await _clusteringManager.RefreshDataAsync(mergeClusterDataTable);
                }

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 병합 완료: {newClusterNumber}");

                // 병합 클러스터 리스트 생성
                create_check_keyword_list();

                // 병합 작업 후 UI 업데이트
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
                            //targetRow["ClusterID"] = newClusterNumber;
                            targetRow["ClusterSubID"] = newClusterNumber;
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
                                var updatedElement = clusteringRepo.UpdateSubClusterIdAsync(targetId, newClusterNumber);

                                if (updatedElement != null)
                                {
                                    //Debug.WriteLine($"MongoDB에서 클러스터 {targetId}의 cluster_id를 {newClusterNumber}로 변경");

                                    var targetRow = dataTable.AsEnumerable()
                                        .FirstOrDefault(row => Convert.ToInt32(row["ID"]) == targetId);

                                    if (targetRow != null)
                                    {
                                        //targetRow["ClusterID"] = newClusterNumber;
                                        targetRow["ClusterSubID"] = newClusterNumber;

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
                            newRow["ClusterSubID"] = newCluster.ClusterSubId;
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
                                    targetRow["ClusterSubID"] = newClusterNumber;
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
                    .Where(row => row["ClusterSubId"] != DBNull.Value &&
                           targetIds.Contains(Convert.ToInt32(row["ClusterSubId"])))
                    .ToList();

                Debug.WriteLine($"병합 해제할 하위 클러스터 수: {childRows.Count}");

                // 하위 클러스터들의 ClusterID 초기화
                foreach (var row in childRows)
                {
                    row["ClusterSubId"] = -1; // 미병합 상태로 변경
                }

                // 변경사항 적용
                dataTable.AcceptChanges();

                // MongoDB에서도 삭제 및 상태 재설정
                var clusteringRepo = new ClusteringRepository();


                foreach (int targetId in targetIds)
                {
                    // 1. 삭제할 클러스터 정보 조회
                    var cluster = await clusteringRepo.GetByClusterNumberAsync(targetId);
                    if (cluster != null)
                    {
                        // 2. 이 클러스터에 병합된 다른 클러스터들의 상태 재설정
                        var childClusters = await clusteringRepo.GetDetailClustersByParentIdAsync(_parentClusterId);
                        var affectedChildren = childClusters.Where(c => c.ClusterSubId == targetId).ToList();
                        foreach (var child in childClusters)
                        {
                            await clusteringRepo.UpdateSubClusterIdAsync(child.ClusterNumber, -1);
                            //Debug.WriteLine($"클러스터 {child.ClusterNumber}의 병합 상태 해제");
                        }

                        // 3. 클러스터 자체 삭제
                        await clusteringRepo.DeleteDetailClusterAndRestoreChildrenAsync(targetId);
                        //Debug.WriteLine($"클러스터 {targetId} 삭제 완료");
                    }
                }

                mergeClusterDataTable = await EnrichWithRawTableDataAsync(dataTable);
                await _clusteringManager.RefreshDataAsync(mergeClusterDataTable);

                var searchCriteria = CreateSearchCriteriaFromCurrentUI();
                await _clusteringManager.SearchAsync(searchCriteria, true);

                // 병합 작업 후 UI 업데이트
                UpdateModifiedDataGridView();
                UpdateSupplySummaryDataGridView();

                Debug.WriteLine("병합 해제 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터 삭제 오류: {ex.Message}");
                MessageBox.Show($"클러스터 삭제 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 세부 클러스터링 완료 시 전역 데이터와 동기화
        /// </summary>
        private async Task SyncDetailClusteringToGlobalData()
        {
            try
            {
                Debug.WriteLine($"세부 클러스터링 데이터를 전역 데이터와 동기화 시작 - 부모 클러스터: {_parentClusterId}");

                if (DataHandler.finalClusteringData == null || _detailClusteringData == null)
                {
                    Debug.WriteLine("동기화할 데이터가 없습니다.");
                    return;
                }

                // *** 1. 전역 데이터에서 해당 부모 클러스터 관련 데이터만 추출 ***
                var globalParentRows = DataHandler.finalClusteringData.AsEnumerable()
                    .Where(row => Convert.ToInt32(row["ClusterID"]) == _parentClusterId)
                    .ToList();

                Debug.WriteLine($"전역 데이터에서 부모 클러스터 {_parentClusterId} 관련 행: {globalParentRows.Count}개");

                // *** 전역 데이터에 ClusterSubID 컬럼이 없으면 추가 ***
                if (!DataHandler.finalClusteringData.Columns.Contains("ClusterSubID"))
                {
                    DataHandler.finalClusteringData.Columns.Add("ClusterSubID", typeof(int));

                    // 기존 모든 행에 기본값 -1 설정 (한 번만 실행)
                    foreach (DataRow existingRow in DataHandler.finalClusteringData.Rows)
                    {
                        existingRow["ClusterSubID"] = -1;
                    }

                    DataHandler.finalClusteringData.AcceptChanges();
                    Debug.WriteLine("전역 데이터에 ClusterSubID 컬럼 추가 및 기본값 설정 완료");
                }

                // *** 2. 로컬 데이터의 클러스터 번호 목록 추출 (효율성을 위해 미리 수집) ***
                var localClusterNumbers = new HashSet<int>(
                    _detailClusteringData.AsEnumerable()
                        .Select(row => Convert.ToInt32(row["ID"]))
                );

                Debug.WriteLine($"로컬 데이터 클러스터 번호: {localClusterNumbers.Count}개");

                // *** 3. 부모 클러스터 관련 행만을 대상으로 수정 작업 수행 ***
                foreach (var globalRow in globalParentRows)
                {
                    int globalClusterNumber = Convert.ToInt32(globalRow["ID"]);

                    // 로컬 데이터에서 해당 클러스터 찾기
                    var localRow = _detailClusteringData.AsEnumerable()
                        .FirstOrDefault(row => Convert.ToInt32(row["ID"]) == globalClusterNumber);

                    if (localRow != null)
                    {
                        // *** 기존 데이터 업데이트 (ClusterSubID 동기화) ***
                        int clusterSubId = Convert.ToInt32(localRow["ClusterSubID"]);

                        globalRow["ClusterSubID"] = clusterSubId;
                        globalRow["클러스터명"] = localRow["클러스터명"];
                        globalRow["키워드목록"] = localRow["키워드목록"];
                        globalRow["Count"] = localRow["Count"];
                        globalRow["합산금액"] = localRow["합산금액"];
                        globalRow["dataIndex"] = localRow["dataIndex"];

                        Debug.WriteLine($"기존 클러스터 {globalClusterNumber} 업데이트 - ClusterSubID: {clusterSubId}");
                    }
                }

                // *** 4. 새로 생성된 세부 상위 클러스터 추가 ***
                // (ClusterSubID == ClusterNumber이면서 전역 데이터에 없는 클러스터)
                var newDetailClusters = _detailClusteringData.AsEnumerable()
                    .Where(localRow =>
                    {
                        int clusterId = Convert.ToInt32(localRow["ID"]);
                        int clusterSubId = Convert.ToInt32(localRow["ClusterSubID"]);

                        // 세부 상위 클러스터이면서 전역 데이터에 없는 경우
                        return clusterSubId == clusterId &&
                               clusterSubId > 0 &&
                               !globalParentRows.Any(globalRow => Convert.ToInt32(globalRow["ID"]) == clusterId);
                    })
                    .ToList();

                foreach (var newLocalRow in newDetailClusters)
                {
                    DataRow newGlobalRow = DataHandler.finalClusteringData.NewRow();
                    newGlobalRow["ID"] = newLocalRow["ID"];
                    newGlobalRow["ClusterID"] = newLocalRow["ClusterID"];
                    newGlobalRow["ClusterSubID"] = newLocalRow["ClusterSubID"];
                    newGlobalRow["클러스터명"] = newLocalRow["클러스터명"];
                    newGlobalRow["키워드목록"] = newLocalRow["키워드목록"];
                    newGlobalRow["Count"] = newLocalRow["Count"];
                    newGlobalRow["합산금액"] = newLocalRow["합산금액"];
                    newGlobalRow["dataIndex"] = newLocalRow["dataIndex"];

                    DataHandler.finalClusteringData.Rows.Add(newGlobalRow);

                    int newClusterNumber = Convert.ToInt32(newLocalRow["ID"]);
                    Debug.WriteLine($"새로운 세부 클러스터 {newClusterNumber} 전역 데이터에 추가");
                }

                // *** 5. 삭제된 세부 클러스터 전역에서도 제거 ***
                // (전역에는 있지만 로컬에는 없는 세부 상위 클러스터)
                var globalRowsToDelete = globalParentRows
                    .Where(globalRow =>
                    {
                        int globalClusterNumber = Convert.ToInt32(globalRow["ID"]);
                        int globalClusterSubId = globalRow["ClusterSubID"] != DBNull.Value ?
                                                Convert.ToInt32(globalRow["ClusterSubID"]) : -1;

                        // 세부 상위 클러스터이면서 로컬 데이터에 없는 경우
                        return globalClusterSubId == globalClusterNumber &&
                               globalClusterSubId > 0 &&
                               !localClusterNumbers.Contains(globalClusterNumber);
                    })
                    .ToList();

                foreach (var rowToDelete in globalRowsToDelete)
                {
                    int deletedClusterNumber = Convert.ToInt32(rowToDelete["ID"]);
                    DataHandler.finalClusteringData.Rows.Remove(rowToDelete);
                    Debug.WriteLine($"삭제된 세부 클러스터 {deletedClusterNumber} 전역 데이터에서 제거");
                }

                // *** 6. 변경사항 저장 ***
                DataHandler.finalClusteringData.AcceptChanges();

                Debug.WriteLine($"세부 클러스터링 데이터 동기화 완료 - 처리된 부모 클러스터 관련 행: {globalParentRows.Count}개");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"데이터 동기화 오류: {ex.Message}");
                throw;
            }
        }

    }
}
