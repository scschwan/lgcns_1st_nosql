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
    /// 클러스터링 결과를 관리하는 전용 저장소
    /// </summary>
    /// <remarks>
    /// clustering_results 컬렉션에 대한 전문화된 데이터 접근 계층을 제공합니다.
    /// 기본 CRUD 기능에 추가하여 클러스터링 전용 연산들을 제공합니다:
    /// - 클러스터 번호별 검색 및 정렬
    /// - 대용량 클러스터링 데이터 처리
    /// - 성능 최적화를 위한 인덱스 관리
    /// 생성자에서 자동으로 필요한 데이터베이스 인덱스를 설정합니다.
    /// </remarks>
    public class ClusteringRepository : BaseRepository<ClusteringResultDocument>
    {
        /// <summary>
        /// ClusteringRepository 생성자
        /// </summary>
        /// <remarks>
        /// "clustering_results" 컬렉션을 대상으로 하는 기본 저장소를 초기화하고,
        /// 클러스터링 연산 성능 최적화를 위해 필요한 데이터베이스 인덱스를 생성합니다.
        /// cluster_number와 cluster_id 필드에 대한 인덱스를 자동 생성하여 검색 성능을 향상시킵니다.
        /// </remarks>
        public ClusteringRepository() : base("clustering_results")
        {
            // 비동기 메서드를 동기적으로 실행하여 인덱스 생성
            Task.Run(async () => await EnsureIndexesCreatedAsync()).Wait();
        }

        /// <summary>
        /// 클러스터링 연산 성능 최적화를 위한 인덱스 생성
        /// </summary>
        /// <returns>인덱스 생성 작업을 나타내는 Task</returns>
        /// <remarks>
        /// 클러스터링 결과 조회 및 정렬 성능을 최적화하기 위한 인덱스들을 생성합니다:
        /// - cluster_number: 클러스터 번호별 오름차순 정렬용
        /// - cluster_id: 클러스터 ID별 빠른 검색용
        /// 컴렉션 초기화 상태를 확인하고, 필요 시 재초기화를 수행합니다.
        /// 인덱스 생성 실패 시에도 예외를 발생시키지 않고 로그만 기록합니다.
        /// </remarks>
        /// <exception cref="MongoException">인덱스 생성 중 예외 발생 시 (예외 전파 안 함)</exception>
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

        /// <summary>
        /// 지정된 클러스터 번호의 클러스터명을 업데이트
        /// </summary>
        /// <param name="clusterNumber">업데이트할 클러스터 번호</param>
        /// <param name="newClusterName">새로운 클러스터명</param>
        /// <returns>업데이트 성공 여부</returns>
        /// <remarks>
        /// 특정 클러스터의 이름만 변경하고 다른 속성은 유지합니다.
        /// 클러스터명은 사용자가 클러스터링 결과를 식별하고 관리하는 데 사용됩니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 업데이트 작업 실패 시</exception>
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

        /// <summary>
        /// 클러스터의 모든 정보를 한 번에 업데이트
        /// </summary>
        /// <param name="clusterNumber">업데이트할 클러스터 번호</param>
        /// <param name="clusterName">새로운 클러스터명</param>
        /// <param name="keywords">새로운 키워드 목록</param>
        /// <param name="count">새로운 문서 개수</param>
        /// <param name="totalAmount">새로운 총 금액</param>
        /// <param name="dataIndices">새로운 데이터 인덱스 목록</param>
        /// <returns>업데이트 성공 여부 (매치된 문서가 있으면 true)</returns>
        /// <remarks>
        /// 클러스터의 모든 주요 속성을 한 번의 작업으로 업데이트합니다.
        /// null 값이 전달된 경우 빈 리스트로 처리하여 데이터 무결성을 보장합니다.
        /// 클러스터 재계산이나 병합 후 정보 갱신에 사용됩니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 업데이트 작업 실패 시</exception>
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
        /// <returns>새로 생성할 수 있는 클러스터 번호</returns>
        /// <remarks>
        /// 현재 컬렉션에서 가장 큰 클러스터 번호를 찾아 1을 더한 값을 반환합니다.
        /// 컬렉션이 비어있거나 문서가 없는 경우 1부터 시작합니다.
        /// 새로운 클러스터 생성 시 고유한 번호를 보장하는 데 사용됩니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 조회 작업 실패 시 기본값 1 반환</exception>
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
        /// 클러스터 번호로 특정 클러스터 문서를 조회
        /// </summary>
        /// <param name="clusterNumber">조회할 클러스터 번호</param>
        /// <returns>클러스터 번호와 일치하는 클러스터 문서 또는 null</returns>
        /// <remarks>
        /// 클러스터 번호는 각 클러스터의 고유 식별자입니다.
        /// 클러스터 상세 정보 조회나 특정 클러스터 작업에 사용됩니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 조회 작업 실패 시</exception>
        public async Task<ClusteringResultDocument> GetByClusterNumberAsync(int clusterNumber)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterNumber, clusterNumber);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        /// <summary>
        /// 클러스터의 ClusterId 속성을 업데이트
        /// </summary>
        /// <param name="clusterNumber">업데이트할 클러스터 번호</param>
        /// <param name="newClusterId">새로운 ClusterId 값</param>
        /// <returns>업데이트 성공 여부</returns>
        /// <remarks>
        /// ClusterId는 클러스터의 계층 구조를 나타내는 데 사용됩니다.
        /// 클러스터 병합이나 계층 재구성 시 상위 클러스터 참조를 변경하는 데 사용됩니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 업데이트 작업 실패 시</exception>
        public async Task<bool> UpdateClusterIdAsync(int clusterNumber, int newClusterId)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterNumber, clusterNumber);
            var update = Builders<ClusteringResultDocument>.Update
                .Set(c => c.ClusterId, newClusterId);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// 클러스터의 ClusterSubId 속성을 업데이트
        /// </summary>
        /// <param name="clusterNumber">업데이트할 클러스터 번호</param>
        /// <param name="newSubClusterId">새로운 ClusterSubId 값</param>
        /// <returns>업데이트 성공 여부</returns>
        /// <remarks>
        /// ClusterSubId는 세부 클러스터링에서 하위 클러스터 그룹을 나타냅니다.
        /// 세부 클러스터링 작업이나 클러스터 세분화 시 사용됩니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 업데이트 작업 실패 시</exception>
        public async Task<bool> UpdateSubClusterIdAsync(int clusterNumber, int newSubClusterId)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterNumber, clusterNumber);
            var update = Builders<ClusteringResultDocument>.Update
                .Set(c => c.ClusterSubId, newSubClusterId);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }



        /// <summary>
        /// 클러스터 번호로 특정 클러스터를 삭제
        /// </summary>
        /// <param name="clusterNumber">삭제할 클러스터 번호</param>
        /// <returns>삭제 성공 여부</returns>
        /// <remarks>
        /// 지정된 클러스터 번호와 일치하는 클러스터를 완전히 삭제합니다.
        /// 삭제된 클러스터는 복구할 수 없으므로 신중하게 사용해야 합니다.
        /// 클러스터 정리나 잘못 생성된 클러스터 제거 시 사용됩니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 삭제 작업 실패 시</exception>
        public async Task<bool> DeleteByClusterNumberAsync(int clusterNumber)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterNumber, clusterNumber);
            var result = await _collection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

       

        /// <summary>
        /// 특정 상위 클러스터에 속한 하위 클러스터들을 조회
        /// </summary>
        /// <param name="parentClusterNumber">상위 클러스터 번호</param>
        /// <returns>하위 클러스터 문서 목록</returns>
        /// <remarks>
        /// ClusterId가 parentClusterNumber와 일치하는 모든 하위 클러스터를 찾습니다.
        /// 상위 클러스터 자신은 결과에서 제외됩니다.
        /// 클러스터 계층 구조 탐색이나 하위 클러스터 관리에 사용됩니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 조회 작업 실패 시</exception>
        public async Task<List<ClusteringResultDocument>> GetChildClustersAsync(int parentClusterNumber)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.And(
                Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterId, parentClusterNumber),
                Builders<ClusteringResultDocument>.Filter.Ne(c => c.ClusterNumber, parentClusterNumber),
                Builders<ClusteringResultDocument>.Filter.Where(c => c.ClusterNumber != c.ClusterSubId)
            );

            return await _collection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// 특정 상위 클러스터에 속한 모든 하위 클러스터들을 조회 (세부 클러스터 포함)
        /// </summary>
        /// <param name="parentClusterNumber">상위 클러스터 번호</param>
        /// <returns>모든 하위 클러스터 문서 목록 (세부 클러스터 포함)</returns>
        /// <remarks>
        /// GetChildClustersAsync와 유사하지만 세부 클러스터링 정보도 포함합니다.
        /// ClusterId가 parentClusterNumber와 일치하는 모든 클러스터를 반환하되,
        /// 상위 클러스터 자신은 제외됩니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 조회 작업 실패 시</exception>
        public async Task<List<ClusteringResultDocument>> GetChildClustersWithSubClusterAsync(int parentClusterNumber)
        {
            var filter = Builders<ClusteringResultDocument>.Filter.And(
                Builders<ClusteringResultDocument>.Filter.Eq(c => c.ClusterId, parentClusterNumber),
                Builders<ClusteringResultDocument>.Filter.Ne(c => c.ClusterNumber, parentClusterNumber)
            );

            return await _collection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// 세부 클러스터의 하위 클러스터들 조회
        /// </summary>
        /// <param name="subClusterId">세부 클러스터 ID</param>
        /// <returns>세부 클러스터에 속한 하위 클러스터 문서 목록</returns>
        /// <remarks>
        /// ClusterSubId가 subClusterId와 일치하는 모든 하위 클러스터를 찾습니다.
        /// 세부 클러스터 자신은 결과에서 제외됩니다.
        /// 세부 클러스터링의 계층 구조를 탐색하는 데 사용됩니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 조회 작업 실패 시 빈 리스트 반환</exception>
        public async Task<List<ClusteringResultDocument>> GetSubChildClustersAsync(int subClusterId)
        {           
            try
            {
                var filter = Builders<ClusteringResultDocument>.Filter.And(
                    Builders<ClusteringResultDocument>.Filter.Eq(d => d.ClusterSubId, subClusterId),
                    Builders<ClusteringResultDocument>.Filter.Ne(d => d.ClusterNumber, subClusterId) // 자기 자신 제외
                );

                var clusters = await _collection.Find(filter).ToListAsync();

                Debug.WriteLine($"세부 클러스터 {subClusterId}의 하위 클러스터 {clusters?.Count ?? 0}개 조회");
                return clusters ?? new List<ClusteringResultDocument>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세부 하위 클러스터 조회 오류 (세부클러스터: {subClusterId}): {ex.Message}");
                return new List<ClusteringResultDocument>();
            }
        }


        /// <summary>
        /// 여러 클러스터를 병합하거나 기존 클러스터를 업데이트
        /// </summary>
        /// <param name="targetClusterNumbers">병합할 클러스터 번호 목록</param>
        /// <param name="newClusterName">새로운 클러스터명 (선택사항)</param>
        /// <param name="existingClusterNumber">기존 클러스터 번호 (0이면 새 클러스터 생성)</param>
        /// <returns>병합된 클러스터의 번호</returns>
        /// <remarks>
        /// 여러 클러스터를 하나로 병합하는 복합 작업을 수행합니다.
        /// 새 클러스터 생성 또는 기존 클러스터 확장 두 가지 모드를 지원합니다.
        /// 병합 과정에서 모든 키워드, 데이터 인덱스, 금액이 통합됩니다.
        /// 병합된 클러스터들은 새 클러스터를 상위로 참조하도록 업데이트됩니다.
        /// </remarks>
        /// <exception cref="ArgumentException">병합할 클러스터 번호가 없는 경우</exception>
        /// <exception cref="InvalidOperationException">병합할 클러스터를 찾을 수 없거나 기존 클러스터가 존재하지 않는 경우</exception>
        /// <exception cref="MongoException">MongoDB 작업 실패 시</exception>
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
        /// <returns>모든 클러스터 정보를 포함하는 DataTable</returns>
        /// <remarks>
        /// 모든 클러스터 결과를 DataGridView 등 UI 컨트롤에 표시하기 위해 DataTable로 변환합니다.
        /// 클러스터 번호 오름차순으로 정렬되며, MongoDB ObjectId도 보존됩니다.
        /// 키워드와 데이터 인덱스는 쉼표로 구분된 문자열로 변환됩니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 조회 작업 실패 시</exception>
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
        /// 특정 상위 클러스터의 모든 하위 데이터 조회 (세부 클러스터링용)
        /// </summary>
        /// <param name="parentClusterId">상위 클러스터 ID</param>
        /// <returns>상위 클러스터에 속한 하위 클러스터 문서 목록</returns>
        /// <remarks>
        /// 세부 클러스터링 작업에서 특정 상위 클러스터에 속한 하위 클러스터들을 찾습니다.
        /// 부모 클러스터 자체는 결과에서 제외되어 순수한 하위 클러스터만 반환합니다.
        /// ClusterId가 parentClusterId와 일치하지만 ClusterNumber는 다른 문서들을 조회합니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 조회 작업 실패 시 빈 리스트 반환</exception>
        public async Task<List<ClusteringResultDocument>> GetDetailClustersByParentIdAsync(int parentClusterId)
        {
            try
            {
                // *** 수정: 부모 클러스터 자체는 제외 ***
                var filter = Builders<ClusteringResultDocument>.Filter.And(
                    Builders<ClusteringResultDocument>.Filter.Eq(d => d.ClusterId, parentClusterId),
                    Builders<ClusteringResultDocument>.Filter.Ne(d => d.ClusterNumber, parentClusterId)
                );
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
        /// <param name="parentClusterId">상위 클러스터 ID</param>
        /// <returns>세부 클러스터 정보를 포함하는 DataTable</returns>
        /// <remarks>
        /// GetDetailClustersByParentIdAsync 메서드와 ConvertToDataTable 메서드를 조합하여
        /// 세부 클러스터 데이터를 UI에 표시하기 위한 DataTable로 변환합니다.
        /// 에러 발생 시 빈 DataTable을 반환하여 안정성을 보장합니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 조회 작업 실패 시 빈 DataTable 반환</exception>
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
        /// <returns>새로 생성할 수 있는 세부 클러스터 번호</returns>
        /// <remarks>
        /// 전체 컷렉션에서 가장 큰 클러스터 번호를 찾아 1을 더한 값을 반환합니다.
        /// GetNextClusterNumberAsync와 동일한 로직으로 작동하지만 세부 클러스터링 전용으로 명명되었습니다.
        /// 세부 클러스터링 작업에서 새로운 클러스터 번호가 필요할 때 사용됩니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 조회 작업 실패 시 기본값 1 반환</exception>
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
        /// 세부 클러스터 ID 업데이트 (중복 메서드 - 통합 필요)
        /// </summary>
        /// <param name="clusterNumber">업데이트할 클러스터 번호</param>
        /// <param name="newSubClusterId">새로운 ClusterSubId 값</param>
        /// <returns>업데이트 성공 여부</returns>
        /// <remarks>
        /// 이 메서드는 윈의 UpdateSubClusterIdAsync와 동일한 기능을 수행합니다.
        /// 세부 클러스터링 작업에서 ClusterSubId 업데이트에 사용됩니다.
        /// 코드 리팩토링 시 두 메서드를 통합하는 것을 고려해야 합니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 업데이트 작업 실패 시</exception>
        public async Task<bool> UpdateClusterSubIdAsync(int clusterNumber, int newSubClusterId)
        {
            try
            {
                var filter = Builders<ClusteringResultDocument>.Filter.Eq(d => d.ClusterNumber, clusterNumber);
                var update = Builders<ClusteringResultDocument>.Update.Set(d => d.ClusterSubId, newSubClusterId);

                var result = await _collection.UpdateOneAsync(filter, update);

                //Debug.WriteLine($"클러스터 {clusterNumber}의 ClusterSubID를 {newSubClusterId}로 업데이트: {result.ModifiedCount > 0}");
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ClusterSubID 업데이트 오류 (클러스터: {clusterNumber}): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 세부 클러스터 병합 (여러 클러스터를 하나로 병합)
        /// </summary>
        /// <param name="targetClusterNumbers">병합할 클러스터 번호 목록</param>
        /// <param name="mergedClusterName">병합된 클러스터의 새 이름</param>
        /// <param name="parentClusterId">상위 클러스터 ID</param>
        /// <returns>새로 생성된 세부 클러스터 번호 (실패 시 -1)</returns>
        /// <remarks>
        /// 여러개의 세부 클러스터를 하나의 상위 세부 클러스터로 병합합니다.
        /// 새로운 세부 클러스터를 생성하고, 기존 클러스터들의 ClusterSubId를 업데이트합니다.
        /// 병합 과정에서 키워드, 데이터 인덱스, 금액 등이 모두 통합됩니다.
        /// 새 세부 클러스터는 자기 자신을 ClusterSubId로 참조합니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 작업 실패 시 -1 반환</exception>
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
        /// 세부 클러스터 삭제 및 하위 클러스터들 복원
        /// </summary>
        /// <param name="detailClusterNumber">삭제할 세부 클러스터 번호</param>
        /// <returns>삭제 및 복원 성공 여부</returns>
        /// <remarks>
        /// 세부 클러스터를 삭제하고 속해 있던 하위 클러스터들을 원래 상태로 복원합니다.
        /// 하위 클러스터들의 ClusterSubId를 -1로 설정하여 독립적인 클러스터로 복원합니다.
        /// 세부 클러스터 병합을 취소하거나 잘못 생성된 세부 클러스터를 제거할 때 사용됩니다.
        /// 삭제될 세부 클러스터는 자기 자신을 ClusterSubId로 참조하는 상위 세부 클러스터여야 합니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 작업 실패 시 false 반환</exception>
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
        /// ClusteringResultDocument 리스트를 DataTable로 변환 (내부 도우미 메서드)
        /// </summary>
        /// <param name="documents">변환할 클러스터 문서 목록</param>
        /// <returns>변환된 DataTable</returns>
        /// <remarks>
        /// MongoDB 문서 리스트를 UI 표시용 DataTable 형태로 변환하는 내부 유틸리티 메서드입니다.
        /// DataTable 열 정의와 데이터 매핑을 처리하며, null 값 처리를 포함합니다.
        /// 키워드와 데이터 인덱스는 쉼표로 구분된 문자열로 직렬화됩니다.
        /// 에러 발생 시 빈 DataTable을 반환하여 안정성을 보장합니다.
        /// </remarks>
        /// <exception cref="Exception">DataTable 변환 실패 시 빈 DataTable 반환</exception>
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
    /// 세부 클러스터 요약 정보를 담는 데이터 클래스
    /// </summary>
    /// <remarks>
    /// 세부 클러스터링 작업에서 클러스터 그룹의 요약 정보를 저장하고 전달하는 데 사용되는 DTO(Data Transfer Object) 클래스입니다.
    /// 세부 클러스터 ID별로 그룹화된 클러스터들의 통계 정보를 제공하여 성능 최적화를 지원합니다.
    /// UI에서 세부 클러스터 요약을 표시하거나 비즈니스 리포트 생성에 활용될 수 있습니다.
    /// </remarks>
    public class DetailClusterSummary
    {
        /// <summary>
        /// 세부 클러스터 ID
        /// </summary>
        /// <value>해당 세부 클러스터를 식별하는 고유 번호</value>
        public int ClusterSubId { get; set; }
        
        /// <summary>
        /// 세부 클러스터 명
        /// </summary>
        /// <value>사용자가 지정한 세부 클러스터의 이름</value>
        public string ClusterName { get; set; }
        
        /// <summary>
        /// 세부 클러스터에 속한 총 데이터 개수
        /// </summary>
        /// <value>세부 클러스터에 포함된 모든 데이터 항목의 합계</value>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// 세부 클러스터에 속한 총 금액
        /// </summary>
        /// <value>세부 클러스터에 포함된 모든 데이터의 금액 합계</value>
        public decimal TotalAmount { get; set; }
        
        /// <summary>
        /// 세부 클러스터에 속한 클러스터 번호 목록
        /// </summary>
        /// <value>이 세부 클러스터에 속한 모든 하위 클러스터의 번호 목록</value>
        public List<int> ClusterNumbers { get; set; } = new List<int>();
    }
}