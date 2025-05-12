using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinanceTool.Models.MongoModels;
using FinanceTool.MongoModels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinanceTool.Repositories
{
    /// <summary>
    /// process_data 컬렉션에 대한 특화된 저장소
    /// </summary>
    public class ProcessDataRepository : BaseRepository<ProcessDataDocument>
    {
        public ProcessDataRepository() : base("process_data")
        {
        }

        /// <summary>
        /// raw_data ID로 관련 process_data 문서 찾기
        /// </summary>
        public async Task<List<ProcessDataDocument>> GetByRawDataIdAsync(string rawDataId)
        {
            var filter = Builders<ProcessDataDocument>.Filter.Eq(d => d.RawDataId, rawDataId);
            return await _collection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// 클러스터 ID로 문서 찾기
        /// </summary>
        public async Task<List<ProcessDataDocument>> GetByClusterIdAsync(int clusterId)
        {
            var filter = Builders<ProcessDataDocument>.Filter.Eq(d => d.ClusterId, clusterId);
            return await _collection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// 문서의 클러스터 정보 업데이트
        /// </summary>
        public async Task<bool> UpdateClusterInfoAsync(string id, int clusterId, string clusterName)
        {
            var filter = Builders<ProcessDataDocument>.Filter.Eq(d => d.Id, id);
            var update = Builders<ProcessDataDocument>.Update
                .Set(d => d.ClusterId, clusterId)
                .Set(d => d.ClusterName, clusterName);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// 여러 문서의 클러스터 정보 일괄 업데이트
        /// </summary>
        public async Task<long> UpdateManyClusterInfoAsync(
            List<string> documentIds,
            int clusterId,
            string clusterName)
        {
            var filter = Builders<ProcessDataDocument>.Filter.In(d => d.Id, documentIds);
            var update = Builders<ProcessDataDocument>.Update
                .Set(d => d.ClusterId, clusterId)
                .Set(d => d.ClusterName, clusterName);

            var result = await _collection.UpdateManyAsync(filter, update);
            return result.ModifiedCount;
        }

        /// <summary>
        /// 특정 필드 기준으로 그룹화하여 집계
        /// </summary>
        public async Task<List<BsonDocument>> GroupByFieldAsync(string fieldName)
        {
            var pipeline = new[]
            {
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", $"$Data.{fieldName}" },
                    { "count", new BsonDocument("$sum", 1) }
                }),
                new BsonDocument("$sort", new BsonDocument("count", -1))
            };

            var results = await _collection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results;
        }

        /// <summary>
        /// 지정된 컬럼만 포함하는 새 process_data 문서 생성
        /// </summary>
        public async Task<List<string>> CreateFromRawDataAsync(
            List<string> rawDataIds,
            List<string> selectedColumns)
        {
            // RawDataRepository를 사용하여 원본 문서 가져오기
            var rawDataRepo = new RawDataRepository();
            var createdIds = new List<string>();

            foreach (var rawDataId in rawDataIds)
            {
                var rawDocument = await rawDataRepo.GetByIdAsync(rawDataId);
                if (rawDocument == null) continue;

                // 새 ProcessDataDocument 생성
                var processDoc = new ProcessDataDocument
                {
                    RawDataId = rawDataId,
                    ImportDate = rawDocument.ImportDate,
                    ProcessedDate = DateTime.Now,
                    Data = new Dictionary<string, object>()
                };

                // 선택된 컬럼만 복사
                foreach (var column in selectedColumns)
                {
                    if (rawDocument.Data.ContainsKey(column))
                    {
                        processDoc.Data[column] = rawDocument.Data[column];
                    }
                }

                // 저장 및 ID 추가
                var newId = await CreateAsync(processDoc);
                createdIds.Add(newId);
            }

            return createdIds;
        }

        /// <summary>
        /// 데이터 필드에 대한 검색
        /// </summary>
        public async Task<List<ProcessDataDocument>> SearchDataFieldsAsync(
            Dictionary<string, object> searchCriteria)
        {
            var filterBuilder = Builders<ProcessDataDocument>.Filter;
            var filters = new List<FilterDefinition<ProcessDataDocument>>();

            foreach (var criteria in searchCriteria)
            {
                // 값 타입에 따라 다른 필터 적용
                if (criteria.Value is string strValue)
                {
                    // 문자열은 부분 일치 검색
                    filters.Add(filterBuilder.Regex($"Data.{criteria.Key}",
                        new MongoDB.Bson.BsonRegularExpression(strValue, "i")));
                }
                else
                {
                    // 다른 타입은 정확한 일치 검색
                    filters.Add(filterBuilder.Eq($"Data.{criteria.Key}", criteria.Value));
                }
            }

            // OR 조건으로 필터 결합
            var combinedFilter = filterBuilder.Or(filters);
            return await _collection.Find(combinedFilter).ToListAsync();
        }
    }
}