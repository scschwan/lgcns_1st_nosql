using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FinanceTool.Models.MongoModels;
using FinanceTool.MongoModels;
using MongoDB.Driver;

namespace FinanceTool.Repositories
{
    /// <summary>
    /// raw_data 컬렉션에 대한 특화된 저장소
    /// </summary>
    public class RawDataRepository : BaseRepository<RawDataDocument>
    {
        public RawDataRepository() : base("raw_data")
        {
        }


        public async Task<List<RawDataDocument>> FindDocumentsAsync(FilterDefinition<RawDataDocument> filter, int? limit = null)
        {
            var query = _collection.Find(filter);

            if (limit.HasValue)
            {
                query = query.Limit(limit.Value);
            }

            return await query.ToListAsync();
        }

    }
}