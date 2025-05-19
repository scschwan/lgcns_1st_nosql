using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinanceTool.MongoModels;
using MongoDB.Driver;

namespace FinanceTool.Repositories
{
    /// <summary>
    /// column_mapping 컬렉션에 대한 특화된 저장소
    /// </summary>
    public class ColumnMappingRepository : BaseRepository<ColumnMappingDocument>
    {
        public ColumnMappingRepository() : base("column_mapping")
        {
        }

        /// <summary>
        /// 표시 가능한(is_visible = true) 컬럼 목록 가져오기
        /// </summary>
        public async Task<List<ColumnMappingDocument>> GetVisibleColumnsAsync()
        {
            var filter = Builders<ColumnMappingDocument>.Filter.Eq(c => c.IsVisible, true);
            var sort = Builders<ColumnMappingDocument>.Sort.Ascending(c => c.Sequence);

            return await _collection.Find(filter).Sort(sort).ToListAsync();
        }

        /// <summary>
        /// 원본 컬럼명으로 매핑 정보 가져오기
        /// </summary>
        public async Task<ColumnMappingDocument> GetByOriginalNameAsync(string originalName)
        {
            var filter = Builders<ColumnMappingDocument>.Filter.Eq(c => c.OriginalName, originalName);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        /// <summary>
        /// 모든 컬럼 매핑 정보 가져오기
        /// </summary>
        public async Task<List<ColumnMappingDocument>> GetAllMappingsAsync()
        {
            var sort = Builders<ColumnMappingDocument>.Sort.Ascending(c => c.Sequence);
            return await _collection.Find(Builders<ColumnMappingDocument>.Filter.Empty).Sort(sort).ToListAsync();
        }

        /// <summary>
        /// 컬럼 표시 여부 업데이트
        /// </summary>
        public async Task<bool> UpdateVisibilityAsync(string originalName, bool isVisible)
        {
            var filter = Builders<ColumnMappingDocument>.Filter.Eq(c => c.OriginalName, originalName);
            var update = Builders<ColumnMappingDocument>.Update.Set(c => c.IsVisible, isVisible);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// 컬럼 표시 순서 업데이트
        /// </summary>
        public async Task<bool> UpdateSequenceAsync(string originalName, int sequence)
        {
            var filter = Builders<ColumnMappingDocument>.Filter.Eq(c => c.OriginalName, originalName);
            var update = Builders<ColumnMappingDocument>.Update.Set(c => c.Sequence, sequence);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// 컬럼 표시명 업데이트
        /// </summary>
        public async Task<bool> UpdateDisplayNameAsync(string originalName, string displayName)
        {
            var filter = Builders<ColumnMappingDocument>.Filter.Eq(c => c.OriginalName, originalName);
            var update = Builders<ColumnMappingDocument>.Update.Set(c => c.DisplayName, displayName);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// 새 컬럼 매핑 추가
        /// </summary>
        public async Task<string> AddColumnMappingAsync(
            string originalName,
            string displayName,
            string dataType = "text",
            bool isVisible = true)
        {
            // 마지막 순서 값 가져오기
            var lastSequenceDoc = await _collection.Find(Builders<ColumnMappingDocument>.Filter.Empty)
                .Sort(Builders<ColumnMappingDocument>.Sort.Descending(c => c.Sequence))
                .Limit(1)
                .FirstOrDefaultAsync();

            int sequence = (lastSequenceDoc != null) ? lastSequenceDoc.Sequence + 1 : 1;

            var mapping = new ColumnMappingDocument
            {
                OriginalName = originalName,
                DisplayName = displayName,
                DataType = dataType,
                IsVisible = isVisible,
                Sequence = sequence
            };

            return await CreateAsync(mapping);
        }
    }
}