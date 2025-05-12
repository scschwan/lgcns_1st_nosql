using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        }

        /// <summary>
        /// 새 클러스터링 결과 생성
        /// </summary>
        public async Task<string> CreateClusterAsync(
            int clusterId,
            string clusterName,
            List<string> keywords,
            List<string> documentIds,
            decimal totalAmount = 0)
        {
            var cluster = new ClusteringResultDocument
            {
                ClusterId = clusterId,
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
            string clusterId,
            List<string> documentIds,
            decimal totalAmount = 0)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.Id, clusterId);
            var update = Builders<ClusteringResultDocument>.Update
                .Set(c => c.DataIndices, documentIds)
                .Set(c => c.Count, documentIds.Count)
                .Set(c => c.TotalAmount, totalAmount);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// 클러스터 이름 변경
        /// </summary>
        public async Task<bool> RenameClusterAsync(string clusterId, string newName)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.Id, clusterId);
            var update = Builders<ClusteringResultDocument>.Update
                .Set(c => c.ClusterName, newName);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// 클러스터 키워드 업데이트
        /// </summary>
        public async Task<bool> UpdateClusterKeywordsAsync(string clusterId, List<string> keywords)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.Id, clusterId);
            var update = Builders<ClusteringResultDocument>.Update
                .Set(c => c.Keywords, keywords);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// 클러스터 병합
        /// </summary>
        public async Task<string> MergeClustersAsync(
            List<string> clusterIds,
            string newClusterName = null)
        {
            // 병합할 클러스터 로드
            var filter = Builders<ClusteringResultDocument>.Filter.In(c => c.Id, clusterIds);
            var clusters = await _collection.Find(filter).ToListAsync();

            if (clusters.Count < 2)
                throw new InvalidOperationException("병합하려면 최소 2개 이상의 클러스터가 필요합니다.");

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

            // 새 병합 클러스터 생성
            var mergedCluster = new ClusteringResultDocument
            {
                ClusterId = clusters.Max(c => c.ClusterId) + 1,
                ClusterName = mergedName,
                Keywords = allKeywords.ToList(),
                DataIndices = allDocIds.ToList(),
                Count = allDocIds.Count,
                TotalAmount = totalAmount,
                CreatedAt = DateTime.Now
            };

            // 새 클러스터 저장
            await _collection.InsertOneAsync(mergedCluster);

            // 기존 클러스터 삭제 (옵션)
            await _collection.DeleteManyAsync(filter);

            return mergedCluster.Id;
        }

        /// <summary>
        /// 문서 ID로 속한 클러스터 찾기
        /// </summary>
        public async Task<List<ClusteringResultDocument>> FindClustersByDocumentIdAsync(string documentId)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.AnyEq(c => c.DataIndices, documentId);
            return await _collection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// 키워드로 클러스터 검색
        /// </summary>
        public async Task<List<ClusteringResultDocument>> SearchClustersByKeywordAsync(string keyword)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.AnyEq(c => c.Keywords, keyword);
            return await _collection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// 멤버 수 기준으로 상위 클러스터 찾기
        /// </summary>
        public async Task<List<ClusteringResultDocument>> GetTopClustersByMemberCountAsync(int limit = 10)
        {
            return await _collection.Find(Builders<ClusteringResultDocument>.Filter.Empty)
                .Sort(Builders<ClusteringResultDocument>.Sort.Descending(c => c.Count))
                .Limit(limit)
                .ToListAsync();
        }

        /// <summary>
        /// 금액 기준으로 상위 클러스터 찾기
        /// </summary>
        public async Task<List<ClusteringResultDocument>> GetTopClustersByAmountAsync(int limit = 10)
        {
            return await _collection.Find(Builders<ClusteringResultDocument>.Filter.Empty)
                .Sort(Builders<ClusteringResultDocument>.Sort.Descending(c => c.TotalAmount))
                .Limit(limit)
                .ToListAsync();
        }

        /// <summary>
        /// 키워드 출현 빈도 분석
        /// </summary>
        public async Task<Dictionary<string, int>> AnalyzeKeywordFrequencyAsync()
        {
            var results = new Dictionary<string, int>();
            var clusters = await _collection.Find(Builders<ClusteringResultDocument>.Filter.Empty).ToListAsync();

            foreach (var cluster in clusters)
            {
                foreach (var keyword in cluster.Keywords)
                {
                    if (results.ContainsKey(keyword))
                    {
                        results[keyword]++;
                    }
                    else
                    {
                        results[keyword] = 1;
                    }
                }
            }

            // 빈도 내림차순으로 정렬
            return results.OrderByDescending(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// 클러스터링 결과를 DataTable 형태로 변환 (UI 표시용)
        /// </summary>
        public async Task<System.Data.DataTable> ToDataTableAsync()
        {
            var clusters = await _collection.Find(Builders<ClusteringResultDocument>.Filter.Empty)
                .Sort(Builders<ClusteringResultDocument>.Sort.Ascending(c => c.ClusterId))
                .ToListAsync();

            var dataTable = new System.Data.DataTable();
            dataTable.Columns.Add("ID", typeof(string));
            dataTable.Columns.Add("ClusterID", typeof(int));
            dataTable.Columns.Add("클러스터명", typeof(string));
            dataTable.Columns.Add("키워드목록", typeof(string));
            dataTable.Columns.Add("Count", typeof(int));
            dataTable.Columns.Add("합산금액", typeof(decimal));
            dataTable.Columns.Add("dataIndex", typeof(string));

            foreach (var cluster in clusters)
            {
                var row = dataTable.NewRow();
                row["ID"] = cluster.Id;
                row["ClusterID"] = cluster.ClusterId;
                row["클러스터명"] = cluster.ClusterName;
                row["키워드목록"] = string.Join(",", cluster.Keywords);
                row["Count"] = cluster.Count;
                row["합산금액"] = cluster.TotalAmount;
                row["dataIndex"] = string.Join(",", cluster.DataIndices);

                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        /// <summary>
        /// DataTable에서 클러스터링 결과 가져오기 (메모리 객체 변환용)
        /// </summary>
        public async Task<List<ClusteringResultDocument>> FromDataTableAsync(System.Data.DataTable dataTable)
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
                return new List<ClusteringResultDocument>();

            var clusters = new List<ClusteringResultDocument>();

            foreach (System.Data.DataRow row in dataTable.Rows)
            {
                var cluster = new ClusteringResultDocument();

                if (row["ID"] != DBNull.Value && row["ID"].ToString() != "")
                    cluster.Id = row["ID"].ToString();

                if (row["ClusterID"] != DBNull.Value)
                    cluster.ClusterId = Convert.ToInt32(row["ClusterID"]);

                if (row["클러스터명"] != DBNull.Value)
                    cluster.ClusterName = row["클러스터명"].ToString();

                if (row["키워드목록"] != DBNull.Value)
                    cluster.Keywords = row["키워드목록"].ToString().Split(',').ToList();

                if (row["Count"] != DBNull.Value)
                    cluster.Count = Convert.ToInt32(row["Count"]);

                if (row["합산금액"] != DBNull.Value)
                    cluster.TotalAmount = Convert.ToDecimal(row["합산금액"]);

                if (row["dataIndex"] != DBNull.Value)
                    cluster.DataIndices = row["dataIndex"].ToString().Split(',').ToList();

                clusters.Add(cluster);
            }

            return clusters;
        }
    }
}