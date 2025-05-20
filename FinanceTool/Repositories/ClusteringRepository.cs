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
                var filter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterNumber, clusterNumber);
                var update = Builders<ClusteringResultDocument>.Update
                    .Set(c => c.ClusterName, clusterName)
                    .Set(c => c.Keywords, keywords)
                    .Set(c => c.Count, count)
                    .Set(c => c.TotalAmount, totalAmount)
                    .Set(c => c.DataIndices, dataIndices);

                var result = await _collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0 || result.MatchedCount > 0;
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

        /// <summary>
        /// 새 클러스터링 결과 생성
        /// </summary>
        public async Task<string> CreateClusterAsync(
            string clusterName,
            List<string> keywords,
            List<string> documentIds,
            decimal totalAmount = 0)
        {
            // 다음 클러스터 번호 가져오기
            int nextClusterNumber = await GetNextClusterNumberAsync();

            var cluster = new ClusteringResultDocument
            {
                ClusterNumber = nextClusterNumber,
                ClusterId = -1, // 초기값은 미병합 상태
                ClusterName = clusterName,
                Keywords = keywords,
                DataIndices = documentIds,
                Count = documentIds.Count,
                TotalAmount = totalAmount,
                CreatedAt = DateTime.Now
            };

            return await CreateAsync(cluster);
        }

        /// <summary>
        /// 클러스터 구성원 업데이트
        /// </summary>
        public async Task<bool> UpdateClusterMembersAsync(
            int clusterNumber,
            List<string> documentIds,
            decimal totalAmount = 0)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterNumber, clusterNumber);
            var update = Builders<ClusteringResultDocument>.Update
                .Set(c => c.DataIndices, documentIds)
                .Set(c => c.Count, documentIds.Count)
                .Set(c => c.TotalAmount, totalAmount);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        // 클러스터 번호로 클러스터 검색
        public async Task<ClusteringResultDocument> GetByClusterNumberAsync(int clusterNumber)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterNumber, clusterNumber);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        // 상위 클러스터 번호로 하위 클러스터 찾기
        public async Task<List<ClusteringResultDocument>> GetByParentClusterNumberAsync(int parentClusterNumber)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterId, parentClusterNumber);
            return await _collection.Find(filter).ToListAsync();
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

        // 클러스터 정보 전체 업데이트
        public async Task<bool> UpdateClusterByNumberAsync(
            int clusterNumber,
            string clusterName,
            List<string> keywords,
            int count,
            decimal totalAmount,
            List<string> dataIndices)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterNumber, clusterNumber);
            var update = Builders<ClusteringResultDocument>.Update
                .Set(c => c.ClusterName, clusterName)
                .Set(c => c.Keywords, keywords)
                .Set(c => c.Count, count)
                .Set(c => c.TotalAmount, totalAmount)
                .Set(c => c.DataIndices, dataIndices);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        // 병합된 하위 클러스터만 가져오기 (ClusterId > 0 && ClusterId != ClusterNumber)
        // 병합된 하위 클러스터만 가져오기 (ClusterId > 0 && ClusterId != ClusterNumber)
        public async Task<List<ClusteringResultDocument>> GetMergedChildClustersAsync()
        {
            var filter = Builders<ClusteringResultDocument>.Filter.Where(c =>
                c.ClusterId > 0 && c.ClusterId != c.ClusterNumber);

            return await _collection.Find(filter).ToListAsync();
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
        /// 병합된 클러스터 해제 (ClusterId 초기화)
        /// </summary>
        public async Task<bool> UnmergeClusterAsync(int clusterNumber)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterNumber, clusterNumber);
            var update = Builders<ClusteringResultDocument>.Update
                .Set(c => c.ClusterId, -1); // 미병합 상태로 변경

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// 클러스터 삭제 및 관련 클러스터 상태 재설정
        /// </summary>
        public async Task DeleteClusterAndResetMembersAsync(int clusterNumber)
        {
            // 1. 삭제할 클러스터가 병합 클러스터인지 확인
            var clusterToDelete = await _collection.Find(
                Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterNumber, clusterNumber)
            ).FirstOrDefaultAsync();

            if (clusterToDelete != null)
            {
                // 2. 이 클러스터에 병합된 다른 클러스터들의 상태 재설정
                if (clusterToDelete.IsMergedCluster)
                {
                    var membersFilter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterId, clusterNumber);
                    var resetUpdate = Builders<ClusteringResultDocument>.Update
                        .Set(c => c.ClusterId, -1); // 미병합 상태로 변경

                    await _collection.UpdateManyAsync(membersFilter, resetUpdate);
                }

                // 3. 클러스터 삭제
                await _collection.DeleteOneAsync(
                    Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterNumber, clusterNumber)
                );
            }
        }

        /// <summary>
        /// 모든 미병합 클러스터 가져오기
        /// </summary>
        public async Task<List<ClusteringResultDocument>> GetUnmergedClustersAsync()
        {
            var filter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterId, -1);
            return await _collection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// 특정 클러스터에 병합된 모든 클러스터 가져오기
        /// </summary>
        public async Task<List<ClusteringResultDocument>> GetMergedClustersAsync(int parentClusterNumber)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterId, parentClusterNumber);
            return await _collection.Find(filter).ToListAsync();
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
        /// DataTable에서 클러스터링 결과 가져오기 (메모리 객체 변환용)
        /// </summary>
        public async Task<List<ClusteringResultDocument>> FromDataTableAsync(DataTable dataTable)
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
                return new List<ClusteringResultDocument>();

            var clusters = new List<ClusteringResultDocument>();

            foreach (DataRow row in dataTable.Rows)
            {
                var cluster = new ClusteringResultDocument();

                // MongoDB ID 처리 (있으면 사용, 없으면 새로 생성)
                if (row.Table.Columns.Contains("_MongoId") && row["_MongoId"] != DBNull.Value && !string.IsNullOrEmpty(row["_MongoId"].ToString()))
                {
                    cluster.Id = row["_MongoId"].ToString();
                }

                // ClusterNumber 처리
                if (row["ID"] != DBNull.Value)
                    cluster.ClusterNumber = Convert.ToInt32(row["ID"]);

                // ClusterId 처리
                if (row["ClusterID"] != DBNull.Value)
                    cluster.ClusterId = Convert.ToInt32(row["ClusterID"]);

                if (row["클러스터명"] != DBNull.Value)
                    cluster.ClusterName = row["클러스터명"].ToString();

                if (row["키워드목록"] != DBNull.Value)
                    cluster.Keywords = row["키워드목록"].ToString().Split(',').Select(k => k.Trim()).ToList();

                if (row["Count"] != DBNull.Value)
                    cluster.Count = Convert.ToInt32(row["Count"]);

                if (row["합산금액"] != DBNull.Value)
                    cluster.TotalAmount = Convert.ToDecimal(row["합산금액"]);

                if (row["dataIndex"] != DBNull.Value)
                    cluster.DataIndices = row["dataIndex"].ToString().Split(',').Select(id => id.Trim()).ToList();

                clusters.Add(cluster);
            }

            return clusters;
        }
    }
}