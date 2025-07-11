using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Data;
using System.Diagnostics;
using FinanceTool.Models.MongoModels;
using FinanceTool.MongoModels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinanceTool.Repositories
{
    /// <summary>
    /// 클러스터링 결과를 관리하는 저장소
    /// </summary>
    public class ClusteringRepository : BaseRepository<ClusteringResultDocument>
    {
        public ClusteringRepository() : base("clustering_results")
        {
            // 비동기 메서드를 동기적으로 실행하기 위한 작업
            Task.Run(async () => await EnsureIndexesCreatedAsync()).Wait();
        }

                /// <summary>
        /// 필요한 인덱스 생성
        /// </summary>
        private async Task EnsureIndexesCreatedAsync()
        {
            try
            {
                if (_collection == null)
                {
                    Debug.WriteLine("컬렉션이 초기화되지 않았습니다. 초기화를 시도합니다.");
                    await InitializeAsync(); // BaseRepository에 있는 초기화 메서드 호출
                }

                if (_collection != null) // null 체크 추가
                {
                    // cluster_number 필드에 오름차순 인덱스 생성
                    var clusterNumberIndex = Builders<ClusteringResultDocument>.IndexKeys.Ascending(c => c.ClusterNumber);
                    await _collection.Indexes.CreateOneAsync(new CreateIndexModel<ClusteringResultDocument>(clusterNumberIndex));

                    // cluster_id 필드에 인덱스 생성
                    var clusterIdIndex = Builders<ClusteringResultDocument>.IndexKeys.Ascending(c => c.ClusterId);
                    await _collection.Indexes.CreateOneAsync(new CreateIndexModel<ClusteringResultDocument>(clusterIdIndex));

                    Debug.WriteLine("인덱스 생성 완료");
                }
                else
                {
                    Debug.WriteLine("컬렉션이 여전히 null입니다. 초기화에 실패했습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"인덱스 생성 중 오류 발생: {ex.Message}");
            }
        }

        // ClusteringRepository.cs에 추가
        public async Task<bool> UpdateClusterNameAsync(int clusterNumber, string newClusterName)
        {
            try
            {
                var filter = Builders<ClusteringResultDocument>.Filter.Eq(d => d.ClusterNumber, clusterNumber);
                var update = Builders<ClusteringResultDocument>.Update.Set(d => d.ClusterName, newClusterName);

                var result = await _collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터명 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        // 클러스터 전체 정보 업데이트
        public async Task<bool> UpdateClusterFullInfoAsync(
            int clusterNumber,
            string clusterName,
            List<string> keywords,
            int count,
            decimal totalAmount,
            List<string> dataIndices)
        {
            try
            {
                var filter = Builders<ClusteringResultDocument>.Filter.Eq(d => d.ClusterNumber, clusterNumber);

                var update = Builders<ClusteringResultDocument>.Update
                    .Set(d => d.ClusterName, clusterName)
                    .Set(d => d.Keywords, keywords ?? new List<string>())
                    .Set(d => d.Count, count)
                    .Set(d => d.TotalAmount, totalAmount)
                    .Set(d => d.DataIndices, dataIndices ?? new List<string>());

                var result = await _collection.UpdateOneAsync(filter, update);

                Debug.WriteLine($"클러스터 {clusterNumber} 전체 정보 업데이트: {result.MatchedCount}개 매치, {result.ModifiedCount}개 수정");

                return result.MatchedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터 정보 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 다음 사용 가능한 클러스터 번호 생성
        /// </summary>
        public async Task<int> GetNextClusterNumberAsync()
        {
            try
            {
                // 현재 최대 클러스터 번호 찾기
                var maxResult = await _collection.Find(Builders<ClusteringResultDocument>.Filter.Empty)
                    .Sort(Builders<ClusteringResultDocument>.Sort.Descending(c => c.ClusterNumber))
                    .Limit(1)
                    .FirstOrDefaultAsync();

                // 문서가 없거나 최대값이 없으면 1부터 시작
                if (maxResult == null)
                    return 1;

                // 최대값 + 1 반환
                return maxResult.ClusterNumber + 1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"다음 클러스터 번호 생성 중 오류: {ex.Message}");
                return 1; // 오류 발생 시 기본값 1 반환
            }
        }

       

        // 클러스터 번호로 클러스터 검색
        public async Task<ClusteringResultDocument> GetByClusterNumberAsync(int clusterNumber)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterNumber, clusterNumber);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        // 클러스터 ID 업데이트
        public async Task<bool> UpdateClusterIdAsync(int clusterNumber, int newClusterId)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterNumber, clusterNumber);
            var update = Builders<ClusteringResultDocument>.Update
                .Set(c => c.ClusterId, newClusterId);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        // 클러스터 번호로 클러스터 삭제
        public async Task<bool> DeleteByClusterNumberAsync(int clusterNumber)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterNumber, clusterNumber);
            var result = await _collection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

       

        // 특정 상위 클러스터에 속한 하위 클러스터 찾기
        public async Task<List<ClusteringResultDocument>> GetChildClustersAsync(int parentClusterNumber)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.And(
                Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterId, parentClusterNumber),
                Builders<ClusteringResultDocument>.Filter.Ne(c => c.ClusterNumber, parentClusterNumber)
            );

            return await _collection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// 클러스터 병합
        /// </summary>
        public async Task<int> MergeOrUpdateClusterAsync(
            List<int> targetClusterNumbers,
            string newClusterName = null,
            int existingClusterNumber = 0)
        {
            if (targetClusterNumbers == null || targetClusterNumbers.Count < 1)
                throw new ArgumentException("병합할 클러스터 번호가 필요합니다.");

            bool isNewCluster = existingClusterNumber <= 0;
            int mergedClusterNumber;

            // 병합할 클러스터 로드
            var filter = Builders<ClusteringResultDocument>.Filter.In(c => c.ClusterNumber, targetClusterNumbers);
            var clusters = await _collection.Find(filter).ToListAsync();

            if (clusters.Count < 1)
                throw new InvalidOperationException("병합할 클러스터가 없습니다.");

            // 새 클러스터 번호 결정
            if (isNewCluster)
            {
                mergedClusterNumber = await GetNextClusterNumberAsync();
            }
            else
            {
                mergedClusterNumber = existingClusterNumber;

                // 기존 클러스터 존재 여부 확인
                var existingCluster = await _collection.Find(
                    Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterNumber, mergedClusterNumber)
                ).FirstOrDefaultAsync();

                if (existingCluster == null)
                {
                    throw new InvalidOperationException($"클러스터 번호 {mergedClusterNumber}를 가진 클러스터가 존재하지 않습니다.");
                }
            }

            // 새 클러스터 이름 (지정되지 않은 경우 첫 번째 클러스터 이름 사용)
            string mergedName = newClusterName ??
                                $"{clusters[0].ClusterName}_merged_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}";

            // 모든 문서 ID와 키워드 수집
            var allDocIds = new HashSet<string>();
            var allKeywords = new HashSet<string>();
            decimal totalAmount = 0;

            foreach (var cluster in clusters)
            {
                foreach (var docId in cluster.DataIndices)
                {
                    allDocIds.Add(docId);
                }

                foreach (var keyword in cluster.Keywords)
                {
                    allKeywords.Add(keyword);
                }

                totalAmount += cluster.TotalAmount;
            }

            if (isNewCluster)
            {
                // 새 병합 클러스터 생성
                var mergedCluster = new ClusteringResultDocument
                {
                    ClusterNumber = mergedClusterNumber,
                    ClusterId = mergedClusterNumber, // 병합된 클러스터는 자신의 번호를 ClusterId로 가짐
                    ClusterName = mergedName,
                    Keywords = allKeywords.ToList(),
                    DataIndices = allDocIds.ToList(),
                    Count = allDocIds.Count,
                    TotalAmount = totalAmount,
                    CreatedAt = DateTime.Now
                };

                // 새 클러스터 저장
                await _collection.InsertOneAsync(mergedCluster);
            }
            else
            {
                // 기존 클러스터 업데이트
                var existingFilter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterNumber, mergedClusterNumber);
                var update = Builders<ClusteringResultDocument>.Update
                    .Set(c => c.ClusterName, mergedName)
                    .Set(c => c.Keywords, allKeywords.ToList())
                    .Set(c => c.DataIndices, allDocIds.ToList())
                    .Set(c => c.Count, allDocIds.Count)
                    .Set(c => c.TotalAmount, totalAmount);

                await _collection.UpdateOneAsync(existingFilter, update);
            }

            // 기존 클러스터의 ClusterId 업데이트 (병합 대상 표시)
            foreach (int targetNumber in targetClusterNumbers)
            {
                if (targetNumber != mergedClusterNumber) // 자기 자신은 제외
                {
                    var targetFilter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterNumber, targetNumber);
                    var update = Builders<ClusteringResultDocument>.Update
                        .Set(c => c.ClusterId, mergedClusterNumber);

                    await _collection.UpdateOneAsync(targetFilter, update);
                }
            }

            return mergedClusterNumber;
        }

        /// <summary>
        /// 클러스터링 결과를 DataTable 형태로 변환 (UI 표시용)
        /// </summary>
        public async Task<DataTable> ToDataTableAsync()
        {
            var clusters = await _collection.Find(Builders<ClusteringResultDocument>.Filter.Empty)
                .Sort(Builders<ClusteringResultDocument>.Sort.Ascending(c => c.ClusterNumber))
                .ToListAsync();

            var dataTable = new DataTable();
            dataTable.Columns.Add("ID", typeof(int));         // ClusterNumber로 매핑
            dataTable.Columns.Add("ClusterID", typeof(int));  // ClusterId로 매핑
            dataTable.Columns.Add("ClusterSubID", typeof(int));  // ClusterId로 매핑
            dataTable.Columns.Add("클러스터명", typeof(string));
            dataTable.Columns.Add("키워드목록", typeof(string));
            dataTable.Columns.Add("Count", typeof(int));
            dataTable.Columns.Add("합산금액", typeof(decimal));
            dataTable.Columns.Add("dataIndex", typeof(string));
            dataTable.Columns.Add("_MongoId", typeof(string)); // MongoDB ObjectId 보존 (숨김 처리)

            foreach (var cluster in clusters)
            {
                var row = dataTable.NewRow();
                row["ID"] = cluster.ClusterNumber;
                row["ClusterID"] = cluster.ClusterId;
                row["ClusterSubID"] = -1;
                row["클러스터명"] = cluster.ClusterName;
                row["키워드목록"] = string.Join(",", cluster.Keywords);
                row["Count"] = cluster.Count;
                row["합산금액"] = cluster.TotalAmount;
                row["dataIndex"] = string.Join(",", cluster.DataIndices);
                row["_MongoId"] = cluster.Id;

                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        /// <summary>
        /// cluster_number == cluster_id인 최종 클러스터만 조회
        /// </summary>
        /// <returns>최종 클러스터 문서 목록</returns>
        public async Task<List<ClusteringResultDocument>> GetFinalClustersAsync()
        {
            try
            {
                // cluster_number == cluster_id 조건으로 필터링
                var filter = Builders<ClusteringResultDocument>.Filter.Where(c => c.ClusterNumber == c.ClusterId);

                // 정렬 조건 추가 (클러스터 번호 기준 오름차순)
                var sort = Builders<ClusteringResultDocument>.Sort.Ascending(c => c.ClusterNumber);

                var cursor = await _collection.FindAsync(filter, new FindOptions<ClusteringResultDocument>
                {
                    Sort = sort
                });

                var results = await cursor.ToListAsync();

                Debug.WriteLine($"GetFinalClustersAsync: {results.Count}개의 최종 클러스터 조회 완료");
                return results;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetFinalClustersAsync 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 특정 raw_data ID가 속한 클러스터 정보 조회
        /// </summary>
        /// <param name="rawDataId">raw_data 문서 ID</param>
        /// <returns>해당 클러스터 정보 (없으면 null)</returns>
        public async Task<ClusteringResultDocument> GetClusterByRawDataIdAsync(string rawDataId)
        {
            try
            {
                if (string.IsNullOrEmpty(rawDataId))
                    return null;

                // data_indices 배열에 해당 rawDataId가 포함된 클러스터 검색
                var filter = Builders<ClusteringResultDocument>.Filter.AnyEq(c => c.DataIndices, rawDataId);

                // cluster_number == cluster_id인 최종 클러스터만 조회
                var finalClusterFilter = Builders<ClusteringResultDocument>.Filter.Where(c => c.ClusterNumber == c.ClusterId);

                // 두 조건을 AND로 결합
                var combinedFilter = Builders<ClusteringResultDocument>.Filter.And(filter, finalClusterFilter);

                var cursor = await _collection.FindAsync(combinedFilter);
                var result = await cursor.FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetClusterByRawDataIdAsync 오류 (rawDataId: {rawDataId}): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 여러 raw_data ID에 대한 클러스터명 매핑 일괄 조회
        /// </summary>
        /// <param name="rawDataIds">raw_data ID 목록</param>
        /// <returns>raw_data ID → cluster_name 매핑 딕셔너리</returns>
        public async Task<Dictionary<string, string>> GetClusterNameMappingAsync(List<string> rawDataIds)
        {
            var mappingDict = new Dictionary<string, string>();

            if (rawDataIds == null || rawDataIds.Count == 0)
                return mappingDict;

            try
            {
                // 최종 클러스터만 조회 (cluster_number == cluster_id)
                var finalClusterFilter = Builders<ClusteringResultDocument>.Filter.Where(c => c.ClusterNumber == c.ClusterId);

                var cursor = await _collection.FindAsync(finalClusterFilter);
                var finalClusters = await cursor.ToListAsync();

                // 각 클러스터의 data_indices를 확인하여 매핑 생성
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

                Debug.WriteLine($"GetClusterNameMappingAsync: {rawDataIds.Count}개 ID 중 {mappingDict.Count}개 매핑 완료");
                return mappingDict;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetClusterNameMappingAsync 오류: {ex.Message}");
                return mappingDict;
            }
        }

        /// <summary>
        /// 전체 raw_data → cluster_name 매핑 조회 (캐싱용)
        /// </summary>
        /// <returns>전체 매핑 딕셔너리</returns>
        public async Task<Dictionary<string, string>> GetAllClusterNameMappingAsync()
        {
            var mappingDict = new Dictionary<string, string>();

            try
            {
                // 최종 클러스터만 조회 (cluster_number == cluster_id)
                var filter = Builders<ClusteringResultDocument>.Filter.Where(c => c.ClusterNumber == c.ClusterId);

                var cursor = await _collection.FindAsync(filter);
                var finalClusters = await cursor.ToListAsync();

                foreach (var cluster in finalClusters)
                {
                    if (cluster.DataIndices != null && !string.IsNullOrEmpty(cluster.ClusterName))
                    {
                        foreach (var dataIndex in cluster.DataIndices)
                        {
                            if (!string.IsNullOrEmpty(dataIndex) && !mappingDict.ContainsKey(dataIndex))
                            {
                                mappingDict[dataIndex] = cluster.ClusterName;
                            }
                        }
                    }
                }

                Debug.WriteLine($"GetAllClusterNameMappingAsync: 전체 {mappingDict.Count}개 매핑 조회 완료");
                return mappingDict;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetAllClusterNameMappingAsync 오류: {ex.Message}");
                return mappingDict;
            }
        }




        /// <summary>
        /// 특정 상위 클러스터의 모든 하위 데이터 조회 (세부 클러스터링용)
        /// </summary>
        public async Task<List<ClusteringResultDocument>> GetDetailClustersByParentIdAsync(int parentClusterId)
        {
            try
            {
                var filter = Builders<ClusteringResultDocument>.Filter.Eq(d => d.ClusterId, parentClusterId);
                var documents = await _collection.Find(filter).ToListAsync();

                Debug.WriteLine($"상위 클러스터 {parentClusterId}의 하위 데이터 {documents.Count}개 조회");
                return documents;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세부 클러스터 데이터 조회 오류: {ex.Message}");
                return new List<ClusteringResultDocument>();
            }
        }

        /// <summary>
        /// 세부 클러스터링 데이터를 DataTable로 변환
        /// </summary>
        public async Task<DataTable> GetDetailClustersAsDataTableAsync(int parentClusterId)
        {
            try
            {
                var documents = await GetDetailClustersByParentIdAsync(parentClusterId);
                return ConvertToDataTable(documents);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세부 클러스터 DataTable 변환 오류: {ex.Message}");
                return new DataTable();
            }
        }

        /// <summary>
        /// 새로운 세부 클러스터 번호 생성
        /// </summary>
        public async Task<int> GetNextDetailClusterNumberAsync()
        {
            try
            {
                var maxClusterNumber = await _collection
                    .Find(Builders<ClusteringResultDocument>.Filter.Empty)
                    .SortByDescending(d => d.ClusterNumber)
                    .Limit(1)
                    .Project(d => d.ClusterNumber)
                    .FirstOrDefaultAsync();

                int nextNumber = maxClusterNumber + 1;
                Debug.WriteLine($"새로운 세부 클러스터 번호: {nextNumber}");
                return nextNumber;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세부 클러스터 번호 생성 오류: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// 세부 클러스터 ID 업데이트
        /// </summary>
        public async Task<bool> UpdateClusterSubIdAsync(int clusterNumber, int newSubId)
        {
            try
            {
                var filter = Builders<ClusteringResultDocument>.Filter.Eq(d => d.ClusterNumber, clusterNumber);
                var update = Builders<ClusteringResultDocument>.Update.Set(d => d.ClusterSubId, newSubId);

                var result = await _collection.UpdateOneAsync(filter, update);
                bool success = result.ModifiedCount > 0;

                Debug.WriteLine($"클러스터 {clusterNumber}의 SubId를 {newSubId}로 업데이트: {(success ? "성공" : "실패")}");
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세부 클러스터 ID 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 세부 클러스터 병합 (여러 클러스터를 하나로 병합)
        /// </summary>
        public async Task<int> MergeDetailClustersAsync(List<int> targetClusterNumbers, string mergedClusterName, int parentClusterId)
        {
            try
            {
                // 1. 새로운 세부 클러스터 번호 생성
                int newDetailClusterNumber = await GetNextDetailClusterNumberAsync();

                // 2. 병합될 클러스터들의 데이터 수집
                var filter = Builders<ClusteringResultDocument>.Filter.In(d => d.ClusterNumber, targetClusterNumbers);
                var targetClusters = await _collection.Find(filter).ToListAsync();

                if (!targetClusters.Any())
                {
                    Debug.WriteLine("병합할 클러스터를 찾을 수 없습니다.");
                    return -1;
                }

                // 3. 병합된 데이터 계산
                var allKeywords = new HashSet<string>();
                var allDataIndices = new HashSet<string>();
                int totalCount = 0;
                decimal totalAmount = 0;

                foreach (var cluster in targetClusters)
                {
                    foreach (var keyword in cluster.Keywords)
                        allKeywords.Add(keyword);
                    foreach (var index in cluster.DataIndices)
                        allDataIndices.Add(index);

                    totalCount += cluster.Count;
                    totalAmount += cluster.TotalAmount;
                }

                // 4. 새로운 세부 상위 클러스터 생성
                var newDetailCluster = new ClusteringResultDocument
                {
                    ClusterNumber = newDetailClusterNumber,
                    ClusterId = parentClusterId,
                    ClusterSubId = newDetailClusterNumber, // 자기 자신을 참조 (상위 세부 클러스터)
                    ClusterName = mergedClusterName,
                    Keywords = allKeywords.ToList(),
                    Count = totalCount,
                    TotalAmount = totalAmount,
                    DataIndices = allDataIndices.ToList(),
                    CreatedAt = DateTime.UtcNow
                };

                await _collection.InsertOneAsync(newDetailCluster);

                // 5. 기존 클러스터들의 cluster_sub_id를 새로운 세부 클러스터로 변경
                foreach (int targetNumber in targetClusterNumbers)
                {
                    await UpdateClusterSubIdAsync(targetNumber, newDetailClusterNumber);
                }

                Debug.WriteLine($"세부 클러스터 병합 완료: {targetClusterNumbers.Count}개 → 클러스터 {newDetailClusterNumber}");
                return newDetailClusterNumber;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세부 클러스터 병합 오류: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// 세부 클러스터 정보 전체 업데이트
        /// </summary>
        public async Task<bool> UpdateDetailClusterFullInfoAsync(int clusterNumber, string clusterName,
            List<string> keywords, int count, decimal totalAmount, List<string> dataIndices)
        {
            try
            {
                var filter = Builders<ClusteringResultDocument>.Filter.Eq(d => d.ClusterNumber, clusterNumber);
                var update = Builders<ClusteringResultDocument>.Update
                    .Set(d => d.ClusterName, clusterName)
                    .Set(d => d.Keywords, keywords)
                    .Set(d => d.Count, count)
                    .Set(d => d.TotalAmount, totalAmount)
                    .Set(d => d.DataIndices, dataIndices);

                var result = await _collection.UpdateOneAsync(filter, update);
                bool success = result.ModifiedCount > 0;

                Debug.WriteLine($"세부 클러스터 {clusterNumber} 전체 정보 업데이트: {(success ? "성공" : "실패")}");
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세부 클러스터 정보 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 세부 클러스터 삭제 및 하위 클러스터들 복원
        /// </summary>
        public async Task<bool> DeleteDetailClusterAndRestoreChildrenAsync(int detailClusterNumber)
        {
            try
            {
                // 1. 해당 세부 클러스터에 속한 하위 클러스터들 찾기
                var childFilter = Builders<ClusteringResultDocument>.Filter.Eq(d => d.ClusterSubId, detailClusterNumber);
                var childClusters = await _collection.Find(childFilter).ToListAsync();

                // 2. 하위 클러스터들의 cluster_sub_id를 -1로 복원
                foreach (var child in childClusters)
                {
                    await UpdateClusterSubIdAsync(child.ClusterNumber, -1);
                }

                // 3. 세부 상위 클러스터 삭제
                var deleteFilter = Builders<ClusteringResultDocument>.Filter.And(
                    Builders<ClusteringResultDocument>.Filter.Eq(d => d.ClusterNumber, detailClusterNumber),
                    Builders<ClusteringResultDocument>.Filter.Eq(d => d.ClusterSubId, detailClusterNumber)
                );

                var deleteResult = await _collection.DeleteOneAsync(deleteFilter);
                bool success = deleteResult.DeletedCount > 0;

                Debug.WriteLine($"세부 클러스터 {detailClusterNumber} 삭제 및 {childClusters.Count}개 하위 클러스터 복원: {(success ? "성공" : "실패")}");
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세부 클러스터 삭제 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 세부 클러스터별 요약 데이터 조회 (Classification 화면용)
        /// </summary>
        public async Task<List<DetailClusterSummary>> GetDetailClusterSummaryAsync(int parentClusterId)
        {
            try
            {
                var pipeline = new[]
                {
                    // 특정 상위 클러스터의 데이터만 필터링
                    new BsonDocument("$match", new BsonDocument("cluster_id", parentClusterId)),
                    
                    // cluster_sub_id 기준으로 그룹화
                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", "$cluster_sub_id" },
                        { "cluster_name", new BsonDocument("$first", "$cluster_name") },
                        { "total_count", new BsonDocument("$sum", "$count") },
                        { "total_amount", new BsonDocument("$sum", "$total_amount") },
                        { "cluster_numbers", new BsonDocument("$push", "$cluster_number") }
                    }),
                    
                    // 정렬
                    new BsonDocument("$sort", new BsonDocument("_id", 1))
                };

                var aggregateResult = await _collection.AggregateAsync<BsonDocument>(pipeline);
                var bsonDocuments = await aggregateResult.ToListAsync();

                var summaries = new List<DetailClusterSummary>();
                foreach (var doc in bsonDocuments)
                {
                    var summary = new DetailClusterSummary
                    {
                        ClusterSubId = doc["_id"].AsInt32,
                        ClusterName = doc["cluster_name"].AsString,
                        TotalCount = doc["total_count"].AsInt32,
                        TotalAmount = doc["total_amount"].AsDecimal,
                        ClusterNumbers = doc["cluster_numbers"].AsBsonArray.Select(x => x.AsInt32).ToList()
                    };
                    summaries.Add(summary);
                }

                Debug.WriteLine($"상위 클러스터 {parentClusterId}의 세부 클러스터 요약 {summaries.Count}개 조회");
                return summaries;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세부 클러스터 요약 조회 오류: {ex.Message}");
                return new List<DetailClusterSummary>();
            }
        }


        /// <summary>
        /// 일반 클러스터링 데이터만 조회 (cluster_sub_id = -1)
        /// </summary>
        public async Task<DataTable> GetNormalClustersAsDataTableAsync()
        {
            try
            {
                var filter = Builders<ClusteringResultDocument>.Filter.Eq(d => d.ClusterSubId, -1);
                var documents = await _collection.Find(filter).ToListAsync();

                Debug.WriteLine($"일반 클러스터 데이터 {documents.Count}개 조회");
                return ConvertToDataTable(documents);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"일반 클러스터 DataTable 변환 오류: {ex.Message}");
                return new DataTable();
            }
        }

        /// <summary>
        /// ClusteringResultDocument 리스트를 DataTable로 변환
        /// </summary>
        private DataTable ConvertToDataTable(List<ClusteringResultDocument> documents)
        {
            try
            {
                DataTable dataTable = new DataTable();

                // 컬럼 정의
                dataTable.Columns.Add("ID", typeof(int));
                dataTable.Columns.Add("ClusterID", typeof(int));
                dataTable.Columns.Add("ClusterSubID", typeof(int)); // 신규 추가
                dataTable.Columns.Add("클러스터명", typeof(string));
                dataTable.Columns.Add("키워드목록", typeof(string));
                dataTable.Columns.Add("Count", typeof(int));
                dataTable.Columns.Add("합산금액", typeof(decimal));
                dataTable.Columns.Add("dataIndex", typeof(string));
                dataTable.Columns.Add("_id", typeof(string));

                // 데이터 추가
                foreach (var doc in documents)
                {
                    DataRow row = dataTable.NewRow();
                    row["ID"] = doc.ClusterNumber;
                    row["ClusterID"] = doc.ClusterId;
                    row["ClusterSubID"] = doc.ClusterSubId; // 신규 추가
                    row["클러스터명"] = doc.ClusterName ?? "";
                    row["키워드목록"] = string.Join(",", doc.Keywords ?? new List<string>());
                    row["Count"] = doc.Count;
                    row["합산금액"] = doc.TotalAmount;
                    row["dataIndex"] = string.Join(",", doc.DataIndices ?? new List<string>());
                    row["_id"] = doc.Id ?? "";

                    dataTable.Rows.Add(row);
                }

                Debug.WriteLine($"DataTable 변환 완료: {documents.Count}개 문서");
                return dataTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DataTable 변환 오류: {ex.Message}");
                return new DataTable();
            }
        }


    }

    /// <summary>
    /// 세부 클러스터 요약 정보 클래스
    /// </summary>
    public class DetailClusterSummary
    {
        public int ClusterSubId { get; set; }
        public string ClusterName { get; set; }
        public int TotalCount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<int> ClusterNumbers { get; set; } = new List<int>();
    }
}