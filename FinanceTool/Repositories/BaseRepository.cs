using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace FinanceTool.Repositories
{
    /// <summary>
    /// MongoDB 컬렉션에 대한 기본 저장소 패턴 구현
    /// </summary>
    public class BaseRepository<T> where T : class
    {
        protected IMongoCollection<T> _collection;
        protected readonly Data.MongoDBManager _dbManager;


        private readonly string _collectionName;


        public BaseRepository(string collectionName)
        {
            _dbManager = Data.MongoDBManager.Instance;
            _collectionName = collectionName;
            InitializeAsync();
        }

        /// <summary>
        /// 문서 생성
        /// </summary>
        public virtual async Task<string> CreateAsync(T document)
        {
            await _collection.InsertOneAsync(document);
            // ObjectId는 document에 설정된 것으로 가정 (BsonId 속성)
            return GetDocumentId(document).ToString();
        }

        public async Task InitializeAsync()
        {
            _collection = await _dbManager.GetCollectionAsync<T>(_collectionName);
        }

        /// <summary>
        /// 여러 문서 생성
        /// </summary>
        public virtual async Task CreateManyAsync(IEnumerable<T> documents)
        {
            await _collection.InsertManyAsync(documents);
        }

        /// <summary>
        /// ID로 문서 조회
        /// </summary>
        public virtual async Task<T> GetByIdAsync(string id)
        {
            var filter = Builders<T>.Filter.Eq("_id", id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        /// <summary>
        /// 조건에 맞는 모든 문서 조회
        /// </summary>
        public virtual async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> filter = null)
        {
            return await _collection.Find(filter ?? Builders<T>.Filter.Empty).ToListAsync();
        }

        /// <summary>
        /// 페이징 처리된 문서 조회
        /// </summary>
        public virtual async Task<(List<T> Items, long TotalCount)> GetPagedAsync(
            Expression<Func<T, bool>> filter = null,
            int pageNumber = 1,
            int pageSize = 10,
            Expression<Func<T, object>> sortExpression = null,
            bool ascending = true)
        {
            var findOptions = new FindOptions<T>
            {
                Skip = (pageNumber - 1) * pageSize,
                Limit = pageSize
            };

            if (sortExpression != null)
            {
                findOptions.Sort = ascending
                    ? Builders<T>.Sort.Ascending(sortExpression)
                    : Builders<T>.Sort.Descending(sortExpression);
            }

            var actualFilter = filter ?? Builders<T>.Filter.Empty;
            var query = _collection.Find(actualFilter);

            long totalCount = await query.CountDocumentsAsync();
            List<T> items = await query.Skip((pageNumber - 1) * pageSize).Limit(pageSize).ToListAsync();

            return (items, totalCount);
        }

        /// <summary>
        /// 문서 업데이트
        /// </summary>
        public virtual async Task<bool> UpdateAsync(string id, T document)
        {
            var filter = Builders<T>.Filter.Eq("_id", id);
            var result = await _collection.ReplaceOneAsync(filter, document);
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// 부분 업데이트
        /// </summary>
        public virtual async Task<bool> UpdateFieldsAsync(string id, UpdateDefinition<T> update)
        {
            var filter = Builders<T>.Filter.Eq("_id", id);
            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// 문서 삭제
        /// </summary>
        public virtual async Task<bool> DeleteAsync(string id)
        {
            var filter = Builders<T>.Filter.Eq("_id", id);
            var result = await _collection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

        /// <summary>
        /// 문서에서 ID 가져오기 (리플렉션 사용)
        /// </summary>
        protected object GetDocumentId(T document)
        {
            var property = typeof(T).GetProperty("Id");
            if (property != null)
            {
                return property.GetValue(document);
            }

            throw new InvalidOperationException("문서에 Id 속성이 없습니다.");
        }
    }
}