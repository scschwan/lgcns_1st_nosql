using FinanceTool.MongoModels;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FinanceTool.Repositories
{
    /// <summary>
    /// process_view_data 컬렉션을 위한 저장소 클래스 (개선버전)
    /// </summary>
    public class ProcessViewRepository : BaseRepository<ProcessViewDocument>
    {
        public ProcessViewRepository() : base("process_view_data")
        {
        }

        /// <summary>
        /// ProcessData ID로 관련 ProcessView 문서 찾기
        /// </summary>
        public async Task<List<ProcessViewDocument>> GetByProcessDataIdAsync(string processDataId)
        {
            var filter = Builders<ProcessViewDocument>.Filter.Eq(d => d.ProcessDataId, processDataId);
            return await _collection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// RawData ID로 관련 ProcessView 문서 찾기 (추가됨)
        /// </summary>
        public async Task<List<ProcessViewDocument>> GetByRawDataIdAsync(string rawDataId)
        {
            var filter = Builders<ProcessViewDocument>.Filter.Eq(d => d.RawDataId, rawDataId);
            return await _collection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// 특정 기간에 처리된 ProcessView 문서 찾기
        /// </summary>
        public async Task<List<ProcessViewDocument>> GetByModifiedDateRangeAsync(DateTime start, DateTime end)
        {
            var filter = Builders<ProcessViewDocument>.Filter.And(
                Builders<ProcessViewDocument>.Filter.Gte(d => d.LastModifiedDate, start),
                Builders<ProcessViewDocument>.Filter.Lte(d => d.LastModifiedDate, end)
            );
            return await _collection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// 특정 ProcessView 문서 업데이트
        /// </summary>
        public async Task<bool> UpdateKeywordsAsync(string id, KeywordInfo newKeywords)
        {
            var filter = Builders<ProcessViewDocument>.Filter.Eq(d => d.Id, id);
            var update = Builders<ProcessViewDocument>.Update
                .Set(d => d.Keywords, newKeywords)
                .Set(d => d.LastModifiedDate, DateTime.Now);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// 특정 ProcessView 문서의 금액 정보 업데이트 (추가됨)
        /// </summary>
        public async Task<bool> UpdateMoneyAsync(string id, object moneyValue)
        {
            var filter = Builders<ProcessViewDocument>.Filter.Eq(d => d.Id, id);
            var update = Builders<ProcessViewDocument>.Update
                .Set(d => d.Money, moneyValue)
                .Set(d => d.LastModifiedDate, DateTime.Now);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// 페이징 처리된 ProcessView 문서 가져오기
        /// </summary>
        public async Task<(List<ProcessViewDocument> Items, long TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            FilterDefinition<ProcessViewDocument> filter = null)
        {
            filter = filter ?? Builders<ProcessViewDocument>.Filter.Empty;
            var sort = Builders<ProcessViewDocument>.Sort.Descending(d => d.LastModifiedDate);

            long totalCount = await _collection.CountDocumentsAsync(filter);
            var items = await _collection.Find(filter)
                .Sort(sort)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <summary>
        /// 키워드로 ProcessView 문서 검색
        /// </summary>
        public async Task<List<ProcessViewDocument>> SearchByKeywordAsync(string keyword)
        {
            var filter = Builders<ProcessViewDocument>.Filter.AnyEq("Keywords.FinalKeywords", keyword);
            return await _collection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// 여러 ProcessView 문서를 배치로 삽입
        /// </summary>
        public async Task InsertManyAsync(List<ProcessViewDocument> documents)
        {
            if (documents == null || documents.Count == 0)
                return;

            await _collection.InsertManyAsync(documents, new InsertManyOptions { IsOrdered = false });
        }

        public async Task<long> CountDocumentsAsync(FilterDefinition<ProcessViewDocument> filter = null)
        {
            filter = filter ?? Builders<ProcessViewDocument>.Filter.Empty;
            return await _collection.CountDocumentsAsync(filter);
        }

        /// <summary>
        /// ProcessData 컬렉션에서 데이터를 가져와 ProcessView 생성 (개선버전)
        /// </summary>
        public async Task<List<ProcessViewDocument>> CreateFromProcessDataAsync(List<string> processDataIds)
        {
            // ProcessData 저장소 접근
            var processDataRepo = new ProcessDataRepository();
            var createdDocuments = new List<ProcessViewDocument>();

            foreach (var processDataId in processDataIds)
            {
                // ProcessData 문서 가져오기
                var processData = await processDataRepo.GetByIdAsync(processDataId);
                if (processData == null) continue;

                // raw_data_id 가져오기
                string rawDataId = processData.RawDataId;

                // 금액 데이터 가져오기
                object moneyValue = null;
                if (processData.Data != null)
                {
                    // moneyColumnName 변수의 값을 컬럼명으로 가진 데이터 찾기
                    string moneyColumnName = DataHandler.levelName[0]; // 금액 컬럼명
                    if (processData.Data.ContainsKey(moneyColumnName))
                    {
                        moneyValue = processData.Data[moneyColumnName];
                    }
                }

                // 키워드 확인
                List<string> keywords = new List<string>();
                // 타겟 컬럼명 (두 번째 컬럼)에서 추출된 키워드 처리 로직
                string targetColumnName = DataHandler.levelName[1]; // 타겟 컬럼명
                if (processData.Data != null && processData.Data.ContainsKey(targetColumnName))
                {
                    string originalText = processData.Data[targetColumnName]?.ToString() ?? string.Empty;
                    // 여기서 키워드 처리 로직 (구분자로 분할 등)을 적용하여 keywords 리스트 채우기
                    // 이 로직은 uc_preprocessing.cs의 SaveProcessDataToMongoDBAsync 메서드 참조
                }

                // ProcessView 문서 생성
                var processViewDoc = new ProcessViewDocument
                {
                    ProcessDataId = processDataId,
                    RawDataId = rawDataId, // 추가된 필드
                    Keywords = new KeywordInfo
                    {
                        FinalKeywords = keywords
                    },
                    Money = moneyValue, // 추가된 필드
                    LastModifiedDate = DateTime.Now
                };

                createdDocuments.Add(processViewDoc);
            }

            // 생성된 문서들 일괄 저장
            if (createdDocuments.Count > 0)
            {
                await InsertManyAsync(createdDocuments);
            }

            return createdDocuments;
        }

        /// <summary>
        /// 단일 ProcessView 문서를 삽입합니다.
        /// </summary>
        public async Task<bool> InsertOneAsync(ProcessViewDocument document)
        {
            try
            {
                await _collection.InsertOneAsync(document);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InsertOneAsync 오류: {ex.Message}");
                return false;
            }
        }
    }
}