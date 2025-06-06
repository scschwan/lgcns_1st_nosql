using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        // RawDataRepository.cs에 추가할 메서드들

        /// <summary>
        /// raw_data 컬렉션의 문서 개수 조회
        /// </summary>
        public async Task<long> GetCountAsync()
        {
            try
            {
                return await _collection.CountDocumentsAsync(FilterDefinition<RawDataDocument>.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"문서 개수 조회 오류: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 모든 raw_data 문서 조회
        /// </summary>
        public async Task<List<RawDataDocument>> GetAllAsync()
        {
            try
            {
                var cursor = await _collection.FindAsync(FilterDefinition<RawDataDocument>.Empty);
                return await cursor.ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"전체 문서 조회 오류: {ex.Message}");
                return new List<RawDataDocument>();
            }
        }

        /// <summary>
        /// 여러 문서 일괄 생성
        /// </summary>
        public async Task CreateManyAsync(List<RawDataDocument> documents)
        {
            try
            {
                if (documents != null && documents.Count > 0)
                {
                    await _collection.InsertManyAsync(documents);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"일괄 문서 생성 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 모든 문서 삭제
        /// </summary>
        public async Task DeleteManyAsync(FilterDefinition<RawDataDocument> filter)
        {
            try
            {
                await _collection.DeleteManyAsync(filter);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"문서 삭제 오류: {ex.Message}");
                throw;
            }
        }

    }
}