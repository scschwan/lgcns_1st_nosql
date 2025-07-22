using FinanceTool.MongoModels;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FinanceTool.Repositories
{
    /// <summary>
    /// MongoDB 컬렉션에 대한 기본 저장소 패턴 구현
    /// </summary>
    public class BaseRepository<T> where T : class
    {
        protected IMongoCollection<T> _collection;
        protected readonly Data.MongoDBManager _dbManager;
        protected readonly string _collectionName;
        private bool _initialized = false;


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
            if (_initialized) return;

            try
            {
                // MongoDBManager가 초기화되었는지 확인
                await _dbManager.EnsureInitializedAsync();

                // 컬렉션 가져오기
                _collection = await _dbManager.GetCollectionAsync<T>(_collectionName);
                _initialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BaseRepository 초기화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 여러 문서 생성
        /// </summary>
        public virtual async Task CreateManyAsync(IEnumerable<T> documents)
        {
            await _collection.InsertManyAsync(documents);
        }

        

        /// <summary>
        /// 조건에 맞는 모든 문서 조회
        /// </summary>
        public virtual async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> filter = null)
        {
            return await _collection.Find(filter ?? Builders<T>.Filter.Empty).ToListAsync();
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

        /// <summary>
        /// 필터 조건에 맞는 문서 목록을 조회합니다.
        /// </summary>
        /// <param name="filter">적용할 필터 조건</param>
        /// <returns>필터와 일치하는 문서 목록</returns>
        public async Task<List<T>> FindDocumentsAsync(FilterDefinition<T> filter)
        {
            try
            {
                return await _collection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FindDocumentsAsync 오류: {ex.Message}");
                return new List<T>();
            }
        }


        /// <summary>
        /// 필터 조건, 정렬 방식 및 페이징을 적용하여 문서 목록을 조회합니다.
        /// </summary>
        /// <param name="filter">적용할 필터 조건</param>
        /// <param name="sort">적용할 정렬 방식</param>
        /// <param name="skip">건너뛸 문서 수</param>
        /// <param name="limit">가져올 최대 문서 수</param>
        /// <returns>조건에 맞는 정렬 및 페이징된 문서 목록</returns>
        public async Task<List<T>> FindDocumentsAsync(
            FilterDefinition<T> filter,
            SortDefinition<T> sort,
            int skip,
            int limit)
        {
            try
            {
                return await _collection.Find(filter)
                    .Sort(sort)
                    .Skip(skip)
                    .Limit(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FindDocumentsAsync 오류: {ex.Message}");
                return new List<T>();
            }
        }

        /// <summary>
        /// ID로 문서 삭제
        /// </summary>
        /// <param name="id">삭제할 문서의 ID</param>
        /// <returns>삭제 성공 여부</returns>
        public async Task<bool> DeleteAsync(ObjectId id)
        {
            try
            {
                var filter = Builders<T>.Filter.Eq("_id", id);
                var result = await _collection.DeleteOneAsync(filter);

                Debug.WriteLine($"{typeof(T).Name} 문서 삭제: ID={id}, 삭제된 문서 수={result.DeletedCount}");
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{typeof(T).Name} 문서 삭제 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 조건에 맞는 문서들 삭제
        /// </summary>
        /// <param name="filter">삭제 조건</param>
        /// <returns>삭제된 문서 수</returns>
        public async Task<long> DeleteManyAsync(FilterDefinition<T> filter)
        {
            try
            {
                var result = await _collection.DeleteManyAsync(filter);

                Debug.WriteLine($"{typeof(T).Name} 다중 문서 삭제: 삭제된 문서 수={result.DeletedCount}");
                return result.DeletedCount;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{typeof(T).Name} 다중 문서 삭제 오류: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 조건에 맞는 문서들 삭제 (람다 표현식 사용)
        /// </summary>
        /// <param name="predicate">삭제 조건</param>
        /// <returns>삭제된 문서 수</returns>
        public async Task<long> DeleteManyAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                var filter = Builders<T>.Filter.Where(predicate);
                return await DeleteManyAsync(filter);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{typeof(T).Name} 다중 문서 삭제 (람다) 오류: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 단일 문서 삭제 (람다 표현식 사용)
        /// </summary>
        /// <param name="predicate">삭제 조건</param>
        /// <returns>삭제 성공 여부</returns>
        public async Task<bool> DeleteOneAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                var filter = Builders<T>.Filter.Where(predicate);
                var result = await _collection.DeleteOneAsync(filter);

                Debug.WriteLine($"{typeof(T).Name} 단일 문서 삭제 (람다): 삭제된 문서 수={result.DeletedCount}");
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{typeof(T).Name} 단일 문서 삭제 (람다) 오류: {ex.Message}");
                return false;
            }
        }
    }
}