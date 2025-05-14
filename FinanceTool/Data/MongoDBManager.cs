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
        public async Task<bool> EnsureInitializedAsync()
        {
            if (_disposed)
                return false;

            if (!_isInitialized)
            {
                lock (_lockObj)
                {
                    if (_isInitialized) return true;
                }

                try
                {
                    await InitializeDatabaseAsync();

                    lock (_lockObj)
                    {
                        _isInitialized = true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"MongoDB 초기화 오류: {ex.Message}");
                    return false;
                }
            }

            return true;
        }


        // 데이터베이스 초기화
        private async Task InitializeDatabaseAsync()
        {
            try
            {
                var settings = new MongoClientSettings
                {
                    Server = new MongoServerAddress("localhost", 27017),
                    ConnectTimeout = TimeSpan.FromSeconds(5),
                    ServerSelectionTimeout = TimeSpan.FromSeconds(5)
                };

                _client = new MongoClient(settings);
                await _client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
                _database = _client.GetDatabase(_databaseName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 초기화 오류: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"MongoDB 초기화 중 오류가 발생했습니다.\n\n오류: {ex.Message}", "MongoDB 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        // 필요한 컬렉션 존재 확인 및 생성
        // 데이터베이스 리셋 메서드 추가
        // 기존 메서드 대체 - 진행 상황 업데이트 기능 추가
        public async Task ResetDatabaseAsync(ProcessProgressForm.UpdateProgressDelegate progressCallback = null)
        {
            if (!await EnsureInitializedAsync())
                throw new InvalidOperationException("MongoDB가 초기화되지 않았습니다.");

            try
            {
                // 컬렉션 목록 가져오기
                var collections = await _database.ListCollectionNames().ToListAsync();
                int totalCollections = collections.Count(c => !c.StartsWith("system."));
                int processedCollections = 0;

                await progressCallback?.Invoke(10, "컬렉션 정보 확인 중...");

                // 각 컬렉션의 문서 수를 먼저 계산 (진행 상황 계산용)
                var collectionStats = new Dictionary<string, long>();
                long totalDocuments = 0;

                foreach (var collName in collections)
                {
                    if (!collName.StartsWith("system."))
                    {
                        var collection = _database.GetCollection<BsonDocument>(collName);
                        long count = await collection.CountDocumentsAsync(new BsonDocument());
                        collectionStats[collName] = count;
                        totalDocuments += count;

                        await progressCallback?.Invoke(
                            10 + (int)((double)processedCollections / totalCollections * 20),
                            $"컬렉션 '{collName}' 크기 확인 중: {count:N0}개 문서");

                        processedCollections++;
                    }
                }

                // 문서 삭제 진행
                processedCollections = 0;
                long deletedDocuments = 0;

                foreach (var collName in collections)
                {
                    if (!collName.StartsWith("system."))
                    {
                        var collection = _database.GetCollection<BsonDocument>(collName);

                        // 컬렉션 크기가 큰 경우(10,000건 이상) 배치 삭제 수행
                        if (collectionStats[collName] > 10000)
                        {
                            const int batchSize = 10000;
                            long remaining = collectionStats[collName];

                            while (remaining > 0)
                            {
                                // 배치 단위로 삭제
                                var filter = new BsonDocument();
                                var sort = Builders<BsonDocument>.Sort.Ascending("_id");
                                var options = new FindOptions<BsonDocument> { Limit = batchSize };

                                var batch = await collection.Find(filter).Sort(sort).Limit(batchSize).ToListAsync();
                                if (batch.Count == 0) break;

                                var ids = batch.Select(doc => doc["_id"]).ToList();
                                var deleteFilter = Builders<BsonDocument>.Filter.In("_id", ids);
                                var result = await collection.DeleteManyAsync(deleteFilter);

                                deletedDocuments += result.DeletedCount;
                                remaining -= result.DeletedCount;

                                double overallProgress = 30 +
                                    ((double)deletedDocuments / totalDocuments * 60);

                                await progressCallback?.Invoke(
                                    (int)overallProgress,
                                    $"컬렉션 '{collName}' 초기화 중: {(collectionStats[collName] - remaining):N0}/{collectionStats[collName]:N0}");
                            }
                        }
                        else
                        {
                            // 작은 컬렉션은 한 번에 삭제
                            var result = await collection.DeleteManyAsync(new BsonDocument());
                            deletedDocuments += result.DeletedCount;

                            double overallProgress = 30 +
                                ((double)deletedDocuments / totalDocuments * 60);

                            await progressCallback?.Invoke(
                                (int)overallProgress,
                                $"컬렉션 '{collName}' 초기화 완료: {result.DeletedCount:N0}개 문서 삭제");
                        }

                        processedCollections++;
                    }
                }

                await progressCallback?.Invoke(90, "필수 컬렉션 생성 중...");
                await EnsureCollectionsExistAsync();
                await progressCallback?.Invoke(95, "인덱스 생성 중...");
                await CreateDefaultIndexesAsync();
                await progressCallback?.Invoke(100, "데이터베이스 초기화 완료");

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

            if (!collectionNames.Contains("column_mapping"))
            {
                await _database.CreateCollectionAsync("column_mapping");
                var collection = _database.GetCollection<BsonDocument>("column_mapping");
                var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("original_name");
                var indexOptions = new CreateIndexOptions { Unique = true };
                await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(indexKeys, indexOptions));
            }

            string[] requiredCollections = { "raw_data", "process_data" };
            foreach (string collName in requiredCollections)
            {
                if (!collectionNames.Contains(collName))
                {
                    await _database.CreateCollectionAsync(collName);
                    Debug.WriteLine($"컬렉션 생성됨: {collName}");
                }
            }

            await CreateDefaultIndexesAsync();
        }


        private async Task CreateDefaultIndexesAsync()
        {
            var rawDataCollection = _database.GetCollection<BsonDocument>("raw_data");
            await rawDataCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("import_date")));

            var processDataCollection = _database.GetCollection<BsonDocument>("process_data");
            await processDataCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("raw_data_id")));
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
        public async Task<bool> CollectionExists(string collectionName)
        {
            bool ensureResult = await EnsureInitializedAsync();

            if (!ensureResult)
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
            bool ensureResult = await EnsureInitializedAsync();

            if (!ensureResult)
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
            bool ensureResult = await EnsureInitializedAsync();

            if (!ensureResult)
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
            bool ensureResult = await EnsureInitializedAsync();

            if (!ensureResult)
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
            bool ensureResult = await EnsureInitializedAsync();

            if (!ensureResult)
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
            bool ensureResult = await EnsureInitializedAsync();

            if (!ensureResult)
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
            bool ensureResult = await EnsureInitializedAsync();

            if (!ensureResult)
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
            bool ensureResult = await EnsureInitializedAsync();

            if (!ensureResult)
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
            bool ensureResult = await EnsureInitializedAsync();

            if (!ensureResult)
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
        public async Task<IMongoCollection<T>> GetCollectionAsync<T>(string collectionName)
        {
            int retryCount = 0;
            const int maxRetries = 3;

            while (retryCount < maxRetries)
            {
                try
                {
                    Debug.WriteLine($"[MongoDBManager] Try GetCollection try count : {retryCount} collectionName : {collectionName}");
                    if (!await EnsureInitializedAsync())
                        throw new InvalidOperationException("MongoDB 초기화 실패");

                    return _database.GetCollection<T>(collectionName);
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                        throw new Exception($"MongoDB 연결 실패 (최대 재시도 횟수 초과): {ex.Message}", ex);

                    await Task.Delay(1000);
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
                Debug.WriteLine("MongoDB 리소스 정리 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 리소스 정리 중 오류: {ex.Message}");
            }
        }

    }
}