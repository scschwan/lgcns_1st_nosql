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

    }
}