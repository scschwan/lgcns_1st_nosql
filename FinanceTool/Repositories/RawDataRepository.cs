﻿using System;
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

        /// <summary>
        /// 날짜 범위로 문서 조회
        /// </summary>
        public async Task<List<RawDataDocument>> GetByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            bool excludeHidden = true)
        {
            var filterBuilder = Builders<RawDataDocument>.Filter;
            var filter = filterBuilder.Gte(d => d.ImportDate, startDate) &
                         filterBuilder.Lte(d => d.ImportDate, endDate);

            if (excludeHidden)
            {
                filter &= filterBuilder.Eq(d => d.IsHidden, false);
            }

            return await _collection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// 특정 필드에 특정 값을 포함하는 문서 검색 
        /// </summary>
        public async Task<List<RawDataDocument>> SearchByFieldValueAsync(
            string fieldName,
            object fieldValue,
            bool exactMatch = false)
        {
            FilterDefinition<RawDataDocument> filter;

            if (exactMatch)
            {
                // 정확한 일치 검색
                filter = Builders<RawDataDocument>.Filter.Eq($"Data.{fieldName}", fieldValue);
            }
            else
            {
                // 부분 일치 검색 (문자열만 가능)
                if (fieldValue is string strValue)
                {
                    filter = Builders<RawDataDocument>.Filter.Regex($"Data.{fieldName}",
                        new MongoDB.Bson.BsonRegularExpression(strValue, "i"));
                }
                else
                {
                    filter = Builders<RawDataDocument>.Filter.Eq($"Data.{fieldName}", fieldValue);
                }
            }

            return await _collection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// 여러 필드 검색 (AND 조건)
        /// </summary>
        public async Task<List<RawDataDocument>> SearchByMultipleFieldsAsync(
            Dictionary<string, object> fieldCriteria,
            bool excludeHidden = true)
        {
            var filterBuilder = Builders<RawDataDocument>.Filter;
            var filters = new List<FilterDefinition<RawDataDocument>>();

            foreach (var criteria in fieldCriteria)
            {
                filters.Add(filterBuilder.Eq($"Data.{criteria.Key}", criteria.Value));
            }

            var combinedFilter = filterBuilder.And(filters);

            if (excludeHidden)
            {
                combinedFilter &= filterBuilder.Eq(d => d.IsHidden, false);
            }

            return await _collection.Find(combinedFilter).ToListAsync();
        }

        /// <summary>
        /// 문서 숨기기/표시 상태 변경
        /// </summary>
        public async Task<bool> SetHiddenStatusAsync(string id, bool hidden, string reason = null)
        {
            var update = Builders<RawDataDocument>.Update
                .Set(d => d.IsHidden, hidden);

            if (hidden && !string.IsNullOrEmpty(reason))
            {
                update = update.Set(d => d.HiddenReason, reason);
            }
            else if (!hidden)
            {
                update = update.Unset(d => d.HiddenReason);
            }

            var result = await _collection.UpdateOneAsync(
                Builders<RawDataDocument>.Filter.Eq(d => d.Id, id),
                update);

            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// 모든 숨긴 문서 표시
        /// </summary>
        public async Task<long> UnhideAllAsync()
        {
            var filter = Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, true);
            var update = Builders<RawDataDocument>.Update
                .Set(d => d.IsHidden, false)
                .Unset(d => d.HiddenReason);

            var result = await _collection.UpdateManyAsync(filter, update);
            return result.ModifiedCount;
        }

        /// <summary>
        /// 특정 필드 값으로 문서 숨기기
        /// </summary>
        public async Task<long> HideDocumentsByFieldValueAsync(
            string fieldName,
            object fieldValue,
            string reason = null)
        {
            var filter = Builders<RawDataDocument>.Filter.Eq($"Data.{fieldName}", fieldValue);

            var update = Builders<RawDataDocument>.Update
                .Set(d => d.IsHidden, true);

            if (!string.IsNullOrEmpty(reason))
            {
                update = update.Set(d => d.HiddenReason, reason);
            }

            var result = await _collection.UpdateManyAsync(filter, update);
            return result.ModifiedCount;
        }

        public async Task<List<RawDataDocument>> GetByKeywordAsync(string keyword)
        {
            // MongoDB Text Search 인덱스 활용
            var filter = Builders<RawDataDocument>.Filter.Text(keyword);
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<List<RawDataDocument>> GetWithPaginationAsync(int page, int pageSize, Expression<Func<RawDataDocument, object>> sortField = null)
        {
            // 페이징 처리와 정렬 기능 추가
            var query = _collection.Find(Builders<RawDataDocument>.Filter.Empty);

            if (sortField != null)
                query = query.Sort(Builders<RawDataDocument>.Sort.Ascending(sortField));

            return await query.Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync();
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

        /// <summary>
        /// ID 목록으로 여러 문서 한 번에 조회
        /// </summary>
        public async Task<List<RawDataDocument>> GetDocumentsByIdsAsync(List<string> ids)
        {
            var filter = Builders<RawDataDocument>.Filter.In(d => d.Id, ids);
            return await _collection.Find(filter).ToListAsync();
        }

    }
}