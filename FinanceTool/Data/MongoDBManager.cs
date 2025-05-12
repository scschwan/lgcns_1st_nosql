using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FinanceTool.MongoModels;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core;

namespace FinanceTool.Data
{
    /// <summary>
    /// MongoDB 데이터베이스 관리 클래스
    /// </summary>
    public class MongoDBManager : IDisposable
    {
        private static readonly object _lockObj = new object();
        private static MongoDBManager _instance;
        private IMongoClient _client;
        private IMongoDatabase _database;
        private string _connectionString;
        private string _databaseName;
        private bool _disposed = false;
        private bool _isInitialized = false;

        // 데이터베이스 리셋 모드 제어 플래그
        private static bool _resetDatabaseOnStartup = true;  // 기본값: 리셋 활성화

        // 리셋 모드 설정을 위한 프로퍼티
        public static bool ResetDatabaseOnStartup
        {
            get { return _resetDatabaseOnStartup; }
            set { _resetDatabaseOnStartup = value; }
        }

        // 싱글톤 인스턴스 접근자
        public static MongoDBManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObj)
                    {
                        if (_instance == null)
                        {
                            _instance = new MongoDBManager();
                        }
                    }
                }
                return _instance;
            }
        }

        // 생성자 - 초기화는 별도 수행
        private MongoDBManager()
        {
            // 생성자에서는 초기화하지 않음
            _connectionString = "mongodb://localhost:27017";
            _databaseName = "FinanceTool";
        }

        // 데이터베이스 초기화 상태 확인 및 필요시 초기화
        public bool EnsureInitialized()
        {
            if (_disposed)
                return false;

            if (!_isInitialized)
            {
                lock (_lockObj)
                {
                    if (!_isInitialized)
                    {
                        try
                        {
                            InitializeDatabase();
                            _isInitialized = true;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"MongoDB 초기화 오류: {ex.Message}");
                            return false;
                        }
                    }
                }
            }
            return _isInitialized;
        }

        // 데이터베이스 초기화
        private async void InitializeDatabase()
        {
            try
            {
                // MongoDB 클라이언트 및 데이터베이스 연결
                _client = new MongoClient(_connectionString);
                _database = _client.GetDatabase(_databaseName);

                // 기본 컬렉션 생성 (필요한 경우)
                // 리셋 플래그가 활성화된 경우 데이터베이스 리셋
                if (_resetDatabaseOnStartup)
                {
                    await ResetDatabaseAsync();
                    Debug.WriteLine("데이터베이스 리셋 모드 활성화: 데이터베이스가 초기화되었습니다.");
                }
                else
                {
                    // 컬렉션만 확인 (리셋하지 않음)
                    await EnsureCollectionsExistAsync();
                    Debug.WriteLine("데이터베이스 영속성 모드 활성화: 기존 데이터가 유지됩니다.");
                }



                Debug.WriteLine("MongoDB 데이터베이스 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 초기화 오류: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        // 필요한 컬렉션 존재 확인 및 생성
        // 데이터베이스 리셋 메서드 추가
        public async Task ResetDatabaseAsync()
        {
            if (!EnsureInitialized())
                throw new InvalidOperationException("MongoDB가 초기화되지 않았습니다.");

            try
            {
                // 기존 컬렉션 목록 가져오기
                var collections = await _database.ListCollectionNames().ToListAsync();

                // 각 컬렉션의 모든 데이터 삭제
                foreach (var collName in collections)
                {
                    // 시스템 컬렉션은 제외
                    if (!collName.StartsWith("system."))
                    {
                        var collection = _database.GetCollection<BsonDocument>(collName);
                        await collection.DeleteManyAsync(new BsonDocument());
                        Debug.WriteLine($"컬렉션 '{collName}'의 데이터가 삭제되었습니다.");
                    }
                }

                // 필요한 컬렉션과 인덱스 생성
                await EnsureCollectionsExistAsync();

                Debug.WriteLine("데이터베이스 리셋 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"데이터베이스 리셋 오류: {ex.Message}");
                throw;
            }
        }

        // 비동기 컬렉션 확인 메서드 (기존 메서드 대체)
        private async Task EnsureCollectionsExistAsync()
        {
            var collectionNames = await _database.ListCollectionNames().ToListAsync();

            // 컬럼 매핑 컬렉션
            if (!collectionNames.Contains("column_mapping"))
            {
                await _database.CreateCollectionAsync("column_mapping");

                // 인덱스 생성
                var collection = _database.GetCollection<BsonDocument>("column_mapping");
                var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("original_name");
                var indexOptions = new CreateIndexOptions { Unique = true };
                await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(indexKeys, indexOptions));
            }

            // 기타 필요한 컬렉션 확인 및 생성
            string[] requiredCollections = { "raw_data", "process_data" };
            foreach (string collName in requiredCollections)
            {
                if (!collectionNames.Contains(collName))
                {
                    await _database.CreateCollectionAsync(collName);
                    Debug.WriteLine($"컬렉션 생성됨: {collName}");
                }
            }

            // 필요한 인덱스 생성
            await CreateDefaultIndexesAsync();
        }

        private async Task CreateDefaultIndexesAsync()
        {
            // raw_data 컬렉션 인덱스
            var rawDataCollection = _database.GetCollection<BsonDocument>("raw_data");
            var rawDataIndex = Builders<BsonDocument>.IndexKeys.Ascending("import_date");
            await rawDataCollection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(rawDataIndex));

            // process_data 컬렉션 인덱스
            var processDataCollection = _database.GetCollection<BsonDocument>("process_data");
            var processDataIndex = Builders<BsonDocument>.IndexKeys.Ascending("raw_data_id");
            await processDataCollection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(processDataIndex));
        }

        public static void EnableDataPersistence()
        {
            _resetDatabaseOnStartup = false;
            Debug.WriteLine("데이터 영속성 모드가 활성화되었습니다. 다음 실행 시 데이터가 유지됩니다.");
        }

        public static void DisableDataPersistence()
        {
            _resetDatabaseOnStartup = true;
            Debug.WriteLine("데이터 영속성 모드가 비활성화되었습니다. 다음 실행 시 데이터가 초기화됩니다.");
        }

        // 컬렉션 존재 여부 확인
        public bool CollectionExists(string collectionName)
        {
            if (!EnsureInitialized())
                return false;

            try
            {
                var filter = new BsonDocument("name", collectionName);
                var collections = _database.ListCollections(new ListCollectionsOptions { Filter = filter });
                return collections.Any();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"컬렉션 존재 확인 오류: {ex.Message}");
                return false;
            }
        }

        // 문서 삽입 - 단일 문서
        public async Task<string> InsertDocumentAsync<T>(string collectionName, T document)
        {
            if (!EnsureInitialized())
                throw new InvalidOperationException("MongoDB가 초기화되지 않았습니다.");

            try
            {
                var collection = _database.GetCollection<T>(collectionName);
                await collection.InsertOneAsync(document);

                // MongoDB의 _id 값 반환 (BsonDocument인 경우)
                if (document is BsonDocument doc && doc.Contains("_id"))
                {
                    return doc["_id"].ToString();
                }

                return "Document inserted successfully";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"문서 삽입 오류: {ex.Message}");
                throw;
            }
        }

        // 문서 삽입 - 다중 문서
        public async Task InsertManyDocumentsAsync<T>(string collectionName, IEnumerable<T> documents)
        {
            if (!EnsureInitialized())
                throw new InvalidOperationException("MongoDB가 초기화되지 않았습니다.");

            try
            {
                var collection = _database.GetCollection<T>(collectionName);
                await collection.InsertManyAsync(documents);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"다중 문서 삽입 오류: {ex.Message}");
                throw;
            }
        }

        // 문서 조회 - 단일 문서
        public async Task<T> FindDocumentAsync<T>(string collectionName, FilterDefinition<T> filter)
        {
            if (!EnsureInitialized())
                throw new InvalidOperationException("MongoDB가 초기화되지 않았습니다.");

            try
            {
                var collection = _database.GetCollection<T>(collectionName);
                return await collection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"문서 조회 오류: {ex.Message}");
                throw;
            }
        }

        // 문서 조회 - 다중 문서
        public async Task<List<T>> FindDocumentsAsync<T>(string collectionName, FilterDefinition<T> filter, int? limit = null)
        {
            if (!EnsureInitialized())
                throw new InvalidOperationException("MongoDB가 초기화되지 않았습니다.");

            try
            {
                var collection = _database.GetCollection<T>(collectionName);
                var findFluent = collection.Find(filter);

                if (limit.HasValue)
                {
                    findFluent = findFluent.Limit(limit.Value);
                }

                return await findFluent.ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"다중 문서 조회 오류: {ex.Message}");
                throw;
            }
        }

        // 문서 업데이트
        public async Task<long> UpdateDocumentsAsync<T>(string collectionName, FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            if (!EnsureInitialized())
                throw new InvalidOperationException("MongoDB가 초기화되지 않았습니다.");

            try
            {
                var collection = _database.GetCollection<T>(collectionName);
                var result = await collection.UpdateManyAsync(filter, update);
                return result.ModifiedCount;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"문서 업데이트 오류: {ex.Message}");
                throw;
            }
        }

        // 문서 삭제
        public async Task<long> DeleteDocumentsAsync<T>(string collectionName, FilterDefinition<T> filter)
        {
            if (!EnsureInitialized())
                throw new InvalidOperationException("MongoDB가 초기화되지 않았습니다.");

            try
            {
                var collection = _database.GetCollection<T>(collectionName);
                var result = await collection.DeleteManyAsync(filter);
                return result.DeletedCount;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"문서 삭제 오류: {ex.Message}");
                throw;
            }
        }

        // 컬렉션 삭제
        public async Task DropCollectionAsync(string collectionName)
        {
            if (!EnsureInitialized())
                throw new InvalidOperationException("MongoDB가 초기화되지 않았습니다.");

            try
            {
                await _database.DropCollectionAsync(collectionName);
                Debug.WriteLine($"컬렉션이 삭제됨: {collectionName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"컬렉션 삭제 오류: {ex.Message}");
                throw;
            }
        }

        // 페이징 처리된 결과 조회
        public async Task<(List<T> Items, long TotalCount)> FindWithPaginationAsync<T>(
            string collectionName,
            FilterDefinition<T> filter,
            int pageNumber,
            int pageSize,
            SortDefinition<T> sort = null)
        {
            if (!EnsureInitialized())
                throw new InvalidOperationException("MongoDB가 초기화되지 않았습니다.");

            try
            {
                var collection = _database.GetCollection<T>(collectionName);

                // 전체 개수 조회
                long totalCount = await collection.CountDocumentsAsync(filter);

                // 페이징 처리된 결과 조회
                var findFluent = collection.Find(filter)
                    .Skip((pageNumber - 1) * pageSize)
                    .Limit(pageSize);

                if (sort != null)
                {
                    findFluent = findFluent.Sort(sort);
                }

                var items = await findFluent.ToListAsync();

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"페이징 처리 조회 오류: {ex.Message}");
                throw;
            }
        }

        // IDisposable 구현
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // MongoDB는 .NET 드라이버에서 자체적으로 리소스 관리를 수행하므로
                    // 명시적인 리소스 해제는 필요하지 않습니다.
                    // 단지 연결 상태를 정리하고 플래그를 설정합니다.
                    _client = null;
                    _database = null;
                }

                _disposed = true;
                _isInitialized = false;
            }
        }

        ~MongoDBManager()
        {
            Dispose(false);
        }

        // MongoDBManager.cs에 연결 재시도 로직 추가
        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            int retryCount = 0;
            const int maxRetries = 3;

            while (retryCount < maxRetries)
            {
                try
                {
                    return _database.GetCollection<T>(collectionName);
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                        throw new Exception($"MongoDB 연결 실패 (최대 재시도 횟수 초과): {ex.Message}", ex);

                    Thread.Sleep(1000); // 1초 대기 후 재시도
                }
            }

            throw new Exception("알 수 없는 오류로 MongoDB 컬렉션을 가져오지 못했습니다.");
        }

        // MongoDBManager.cs에 인덱스 생성 메서드 추가
        public async Task CreateIndexesAsync()
        {
            try
            {
                // raw_data 컬렉션에 인덱스 생성
                var rawDataCollection = _database.GetCollection<RawDataDocument>("raw_data");
                await rawDataCollection.Indexes.CreateOneAsync(
                    Builders<RawDataDocument>.IndexKeys.Ascending("import_date"));

                // process_data 컬렉션에 인덱스 생성
                var processDataCollection = _database.GetCollection<ProcessDataDocument>("process_data");
                await processDataCollection.Indexes.CreateOneAsync(
                    Builders<ProcessDataDocument>.IndexKeys.Ascending("raw_data_id"));

                // 필요한 경우 다른 컬렉션에도 인덱스 추가
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"인덱스 생성 중 오류: {ex.Message}");
                throw;
            }
        }

        // FinanceTool/Data/MongoDBManager.cs 파일에 추가
        public void Cleanup()
        {
            try
            {
                // MongoDB 드라이버는 자체적으로 연결 풀을 관리하므로
                // 명시적인 연결 종료는 필요하지 않습니다.
                // 추가적인 리소스 정리가 필요한 경우 여기에 구현
                Debug.WriteLine("MongoDB 리소스 정리 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 리소스 정리 중 오류: {ex.Message}");
            }
        }
    }
}