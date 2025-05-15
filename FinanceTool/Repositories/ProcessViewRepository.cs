using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinanceTool.MongoModels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinanceTool.Repositories
{
    /// <summary>
    /// process_view_data 컬렉션을 위한 저장소 클래스
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
        /// 특정 기간에 처리된 ProcessView 문서 찾기
        /// </summary>
        public async Task<List<ProcessViewDocument>> GetByProcessedDateRangeAsync(DateTime start, DateTime end)
        {
            var filter = Builders<ProcessViewDocument>.Filter.And(
                Builders<ProcessViewDocument>.Filter.Gte(d => d.ProcessedDate, start),
                Builders<ProcessViewDocument>.Filter.Lte(d => d.ProcessedDate, end)
            );
            return await _collection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// 처리 유형별 ProcessView 문서 찾기
        /// </summary>
        public async Task<List<ProcessViewDocument>> GetByProcessingTypeAsync(string processingType)
        {
            var filter = Builders<ProcessViewDocument>.Filter.Eq("ProcessingInfo.ProcessingType", processingType);
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
                .Set(d => d.UserModified, true)
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
            var sort = Builders<ProcessViewDocument>.Sort.Descending(d => d.ProcessedDate);

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
            var filter = Builders<ProcessViewDocument>.Filter.Or(
                Builders<ProcessViewDocument>.Filter.AnyEq("Keywords.ExtractedKeywords", keyword),
                Builders<ProcessViewDocument>.Filter.AnyEq("Keywords.FinalKeywords", keyword)
            );
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

        // ProcessViewRepository.cs 파일에 다음 메서드 추가
        public async Task<long> CountDocumentsAsync(FilterDefinition<ProcessViewDocument> filter = null)
        {
            filter = filter ?? Builders<ProcessViewDocument>.Filter.Empty;
            return await _collection.CountDocumentsAsync(filter);
        }

        /// <summary>
        /// ProcessData 컬렉션에서 데이터를 가져와 ProcessView 생성
        /// </summary>
        public async Task<List<ProcessViewDocument>> CreateFromProcessDataAsync(
            List<string> processDataIds,
            string processingType,
            string separator = null,
            string modelName = null)
        {
            // ProcessData 저장소 접근
            var processDataRepo = new ProcessDataRepository();
            var createdDocuments = new List<ProcessViewDocument>();

            foreach (var processDataId in processDataIds)
            {
                // ProcessData 문서 가져오기
                var processData = await processDataRepo.GetByIdAsync(processDataId);
                if (processData == null) continue;

                // 관련 컬럼 데이터 추출
                string originalText = string.Empty;
                if (processData.Data != null && processData.Data.ContainsKey("Column2"))
                {
                    originalText = processData.Data["Column2"]?.ToString() ?? string.Empty;
                }

                // ProcessView 문서 생성
                var processViewDoc = new ProcessViewDocument
                {
                    ProcessDataId = processDataId,
                    Keywords = new KeywordInfo
                    {
                        OriginalText = originalText,
                        ExtractedKeywords = new List<string>(),
                        RemovedKeywords = new List<string>(),
                        FinalKeywords = new List<string>()
                    },
                    ProcessingInfo = new ProcessingInfo
                    {
                        ProcessingType = processingType,
                        Separator = separator,
                        ModelName = modelName
                    },
                    ProcessedDate = DateTime.Now,
                    UserModified = false,
                    LastModifiedDate = DateTime.Now
                };

                // 필요한 경우 추가 필드 채우기
                // ...

                createdDocuments.Add(processViewDoc);
            }

            // 생성된 문서들 일괄 저장
            if (createdDocuments.Count > 0)
            {
                await InsertManyAsync(createdDocuments);
            }

            return createdDocuments;
        }
    }
}