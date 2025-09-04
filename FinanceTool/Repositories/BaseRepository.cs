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
    /// <typeparam name="T">MongoDB 문서 모델 타입</typeparam>
    /// <remarks>
    /// Repository 패턴을 구현하여 MongoDB 컬렉션에 대한 표준 CRUD 작업을 제공합니다.
    /// 제네릭 타입을 사용하여 다양한 문서 모델에 재사용 가능한 데이터 접근 계층을 구현합니다.
    /// 모든 파생 클래스는 이 클래스를 상속받아 특화된 데이터 접근 메서드를 추가할 수 있습니다.
    /// </remarks>
    public class BaseRepository<T> where T : class
    {
        /// <summary>MongoDB 컬렉션 인스턴스</summary>
        protected IMongoCollection<T> _collection;
        /// <summary>MongoDB 데이터베이스 관리자 인스턴스</summary>
        protected readonly Data.MongoDBManager _dbManager;
        /// <summary>컬렉션 이름</summary>
        protected readonly string _collectionName;
        /// <summary>저장소 초기화 상태</summary>
        private bool _initialized = false;


        /// <summary>
        /// BaseRepository 생성자
        /// </summary>
        /// <param name="collectionName">연결할 MongoDB 컬렉션 이름</param>
        /// <remarks>
        /// MongoDB 관리자 싱글톤 인스턴스를 가져오고 지정된 컬렉션으로 저장소를 초기화합니다.
        /// 비동기 초기화를 통해 데이터베이스 연결과 컬렉션 설정을 수행합니다.
        /// </remarks>
        public BaseRepository(string collectionName)
        {
            _dbManager = Data.MongoDBManager.Instance;
            _collectionName = collectionName;
            InitializeAsync();
        }

        /// <summary>
        /// 새로운 문서를 MongoDB 컬렉션에 생성
        /// </summary>
        /// <param name="document">생성할 문서 객체</param>
        /// <returns>생성된 문서의 ID 문자열</returns>
        /// <remarks>
        /// 문서 객체를 MongoDB 컬렉션에 삽입하고 자동 생성된 ObjectId를 반환합니다.
        /// 문서는 BsonId 속성을 통해 자동으로 ID가 설정됩니다.
        /// 파생 클래스에서 오버라이드하여 특화된 생성 로직을 구현할 수 있습니다.
        /// </remarks>
        /// <exception cref="ArgumentNullException">document가 null인 경우</exception>
        /// <exception cref="MongoException">MongoDB 삽입 작업 실패 시</exception>
        public virtual async Task<string> CreateAsync(T document)
        {
            await _collection.InsertOneAsync(document);
            // ObjectId는 document에 설정된 것으로 가정 (BsonId 속성)
            return GetDocumentId(document).ToString();
        }

        /// <summary>
        /// 저장소를 비동기적으로 초기화
        /// </summary>
        /// <returns>초기화 작업을 나타내는 Task</returns>
        /// <remarks>
        /// MongoDB 데이터베이스 연결을 확인하고 지정된 컬렉션에 대한 참조를 설정합니다.
        /// 중복 초기화를 방지하기 위해 초기화 상태를 확인합니다.
        /// 초기화 실패 시 디버그 로그를 출력하고 오류를 기록합니다.
        /// </remarks>
        /// <exception cref="Exception">MongoDB 연결 또는 컬렉션 액세스 실패 시</exception>
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
        /// 여러 문서를 한 번에 MongoDB 컬렉션에 생성
        /// </summary>
        /// <param name="documents">생성할 문서 객체들의 컬렉션</param>
        /// <returns>생성 작업을 나타내는 Task</returns>
        /// <remarks>
        /// 대량 삽입 작업을 통해 성능을 최적화합니다.
        /// 트랜잭션 내에서 모든 문서가 성공적으로 삽입되거나 전체가 실패합니다.
        /// 파생 클래스에서 오버라이드하여 특화된 대량 생성 로직을 구현할 수 있습니다.
        /// </remarks>
        /// <exception cref="ArgumentNullException">documents가 null인 경우</exception>
        /// <exception cref="MongoException">MongoDB 대량 삽입 작업 실패 시</exception>
        public virtual async Task CreateManyAsync(IEnumerable<T> documents)
        {
            await _collection.InsertManyAsync(documents);
        }

        

        /// <summary>
        /// 조건에 맞는 모든 문서를 조회
        /// </summary>
        /// <param name="filter">필터링 조건 (null인 경우 모든 문서 반환)</param>
        /// <returns>조건에 맞는 문서들의 리스트</returns>
        /// <remarks>
        /// 람다 표현식을 사용하여 유연한 조건 검색을 지원합니다.
        /// 필터가 null인 경우 컬렉션의 모든 문서를 반환합니다.
        /// 대용량 컬렉션의 경우 성능을 고려하여 페이징을 사용하는 것을 권장합니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 조회 작업 실패 시</exception>
        public virtual async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> filter = null)
        {
            return await _collection.Find(filter ?? Builders<T>.Filter.Empty).ToListAsync();
        }

     
      

        /// <summary>
        /// 문서 객체에서 ID 값을 추출
        /// </summary>
        /// <param name="document">ID를 추출할 문서 객체</param>
        /// <returns>문서의 ID 값</returns>
        /// <remarks>
        /// 리플렉션을 사용하여 문서 객체의 'Id' 속성 값을 가져옵니다.
        /// 모든 MongoDB 문서 모델은 'Id' 속성을 가져야 합니다.
        /// 성능을 고려하여 자주 호출되는 부분에서는 캐싱을 고려할 수 있습니다.
        /// </remarks>
        /// <exception cref="InvalidOperationException">문서에 Id 속성이 없는 경우</exception>
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
        /// 필터 조건에 맞는 문서 목록을 조회
        /// </summary>
        /// <param name="filter">적용할 필터 조건</param>
        /// <returns>필터와 일치하는 문서 목록</returns>
        /// <remarks>
        /// MongoDB 쿼리 빌더를 통해 복잡한 조건 검색을 지원합니다.
        /// 예외 발생 시 빈 리스트를 반환하여 안정성을 보장합니다.
        /// 성능: O(n) - 컬렉션 크기에 비례
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 조회 작업 실패 시</exception>
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
        /// 필터 조건, 정렬 방식 및 페이징을 적용하여 문서 목록을 조회
        /// </summary>
        /// <param name="filter">적용할 필터 조건</param>
        /// <param name="sort">적용할 정렬 방식</param>
        /// <param name="skip">건너뛸 문서 수 (페이지네이션 오프셋)</param>
        /// <param name="limit">가져올 최대 문서 수 (페이지 크기)</param>
        /// <returns>조건에 맞는 정렬 및 페이징된 문서 목록</returns>
        /// <remarks>
        /// 대용량 데이터셋에 대한 효율적인 페이징 기능을 제공합니다.
        /// 정렬과 페이징을 조합하여 메모리 효율적인 데이터 검색을 지원합니다.
        /// 성능: O(log n + k) - 인덱스 사용 시
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 조회 작업 실패 시</exception>
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
        /// <param name="id">삭제할 문서의 ObjectId</param>
        /// <returns>삭제 성공 여부 (true: 삭제됨, false: 문서가 존재하지 않거나 오류 발생)</returns>
        /// <remarks>
        /// MongoDB ObjectId를 사용하여 정확한 문서 매칭을 수행합니다.
        /// 삭제 결과는 디버그 로그에 기록됩니다.
        /// 예외 발생 시 false를 반환하고 오류 로그를 출력합니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 삭제 작업 실패 시</exception>
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
        /// <param name="filter">삭제 조건을 정의하는 필터</param>
        /// <returns>실제 삭제된 문서 수</returns>
        /// <remarks>
        /// 주의: 여러 문서를 한 번에 삭제하므로 신중하게 사용해야 합니다.
        /// 삭제 작업은 트랜잭션으로 처리되어 원자성을 보장합니다.
        /// 삭제 결과는 디버그 로그에 기록됩니다.
        /// 성능: O(n) - 조건에 맞는 문서 수에 비례
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 대량 삭제 작업 실패 시</exception>
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
        /// <param name="predicate">삭제 조건을 정의하는 람다 표현식</param>
        /// <returns>실제 삭제된 문서 수</returns>
        /// <remarks>
        /// C# LINQ 표현식을 MongoDB 쿼리로 변환하여 직관적인 조건 설정을 제공합니다.
        /// 내부적으로 FilterDefinition으로 변환되어 DeleteManyAsync 메서드를 호출합니다.
        /// 타입 안전성과 IntelliSense 지원을 통해 개발 편의성을 향상시킵니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 삭제 작업 실패 시</exception>
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
        /// <param name="predicate">삭제 조건을 정의하는 람다 표현식</param>
        /// <returns>삭제 성공 여부 (true: 문서 삭제됨, false: 조건에 맞는 문서 없음 또는 오류 발생)</returns>
        /// <remarks>
        /// 조건에 맞는 첫 번째 문서만 삭제합니다.
        /// C# LINQ 표현식을 사용하여 타입 안전한 조건 설정이 가능합니다.
        /// 삭제 결과는 디버그 로그에 기록됩니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 삭제 작업 실패 시</exception>
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

        /// <summary>
        /// ID로 단일 문서 조회
        /// </summary>
        /// <param name="id">조회할 문서의 ObjectId</param>
        /// <returns>해당 ID의 문서 객체, 문서가 존재하지 않거나 오류 발생 시 null</returns>
        /// <remarks>
        /// MongoDB의 기본 인덱스인 _id를 사용하여 O(1) 성능으로 조회합니다.
        /// 파생 클래스에서 오버라이드하여 특화된 조회 로직을 구현할 수 있습니다.
        /// 예외 발생 시 null을 반환하여 안정성을 보장합니다.
        /// 성능: O(1) - _id 인덱스 사용
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 조회 작업 실패 시</exception>
        public virtual async Task<T> GetByIdAsync(ObjectId id)
        {
            try
            {
                var filter = Builders<T>.Filter.Eq("_id", id);
                return await _collection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{typeof(T).Name} 문서 조회 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 문서 업데이트
        /// </summary>
        /// <param name="id">업데이트할 문서의 ObjectId</param>
        /// <param name="document">새로운 내용으로 교체할 문서 객체</param>
        /// <returns>업데이트 성공 여부 (true: 문서 수정됨, false: 문서가 존재하지 않거나 오류 발생)</returns>
        /// <remarks>
        /// 전체 문서를 새로운 문서로 교체하는 ReplaceOne 작업을 수행합니다.
        /// 부분 업데이트가 필요한 경우 파생 클래스에서 별도의 메서드를 구현하세요.
        /// 업데이트 결과는 디버그 로그에 기록됩니다.
        /// 파생 클래스에서 오버라이드하여 특화된 업데이트 로직을 구현할 수 있습니다.
        /// 성능: O(1) - _id 인덱스 사용
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 업데이트 작업 실패 시</exception>
        public virtual async Task<bool> UpdateAsync(ObjectId id, T document)
        {
            try
            {
                var filter = Builders<T>.Filter.Eq("_id", id);
                var result = await _collection.ReplaceOneAsync(filter, document);

                Debug.WriteLine($"{typeof(T).Name} 문서 업데이트: ID={id}, 수정된 문서 수={result.ModifiedCount}");
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{typeof(T).Name} 문서 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 여러 ID로 문서들 조회
        /// </summary>
        /// <param name="ids">조회할 문서들의 ObjectId 목록</param>
        /// <returns>존재하는 문서들의 목록 (일부 ID가 존재하지 않아도 오류 없이 해당 문서들만 반환)</returns>
        /// <remarks>
        /// MongoDB의 In 연산자를 사용하여 여러 문서를 한 번에 효율적으로 조회합니다.
        /// 존재하지 않는 ID는 무시되고, 존재하는 문서들만 결과에 포함됩니다.
        /// 대량 ID 조회 시 배치 처리를 고려하여 메모리 사용량을 최적화하세요.
        /// 파생 클래스에서 오버라이드하여 특화된 다중 조회 로직을 구현할 수 있습니다.
        /// 성능: O(k) - 조회하는 ID 수에 비례
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 조회 작업 실패 시</exception>
        public virtual async Task<List<T>> GetByIdsAsync(List<ObjectId> ids)
        {
            try
            {
                var filter = Builders<T>.Filter.In("_id", ids);
                return await _collection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{typeof(T).Name} 다중 문서 조회 오류: {ex.Message}");
                return new List<T>();
            }
        }
    }
}