using FinanceTool.MongoModels;
using FinanceTool.Utilities;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

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
                    ServerSelectionTimeout = TimeSpan.FromSeconds(5),
                    MaxConnectionPoolSize = 3000,
                    MaxConnecting = 1000,
                    SocketTimeout = TimeSpan.FromMinutes(10)

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
                var targetCollections = new[]
                {
            "clustering_results",
            "column_mapping",
            "process_data",
            "process_view_data",
            "raw_data"
        };

                await progressCallback?.Invoke(10, "컬렉션 초기화 시작...");

                int total = targetCollections.Length;
                int processed = 0;

                foreach (var collName in targetCollections)
                {
                    var collectionList = await _database.ListCollectionNames().ToListAsync();
                    bool exists = collectionList.Contains(collName);

                    if (exists)
                    {
                        var collection = _database.GetCollection<BsonDocument>(collName);
                        long count = await collection.CountDocumentsAsync(new BsonDocument());

                        if (count > 10000)
                        {
                            await _database.DropCollectionAsync(collName);
                            await _database.CreateCollectionAsync(collName);
                            Debug.WriteLine($"컬렉션 '{collName}' 드롭 후 재생성 완료 ({count:N0}건 삭제)");
                        }
                        else
                        {
                            await collection.DeleteManyAsync(new BsonDocument());
                            Debug.WriteLine($"컬렉션 '{collName}' 데이터 삭제 완료 ({count:N0}건 삭제)");
                        }
                    }
                    else
                    {
                        await _database.CreateCollectionAsync(collName);
                        Debug.WriteLine($"컬렉션 '{collName}' 새로 생성됨 (기존 없음)");
                    }

                    processed++;
                    int progress = 10 + (int)((double)processed / total * 80);
                    await progressCallback?.Invoke(progress, $"'{collName}' 초기화 완료");
                }

                await progressCallback?.Invoke(100, "모든 컬렉션 초기화 완료");
                Debug.WriteLine("✅ 중간 전략 기반 데이터베이스 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"데이터베이스 리셋 오류: {ex.Message}");
                throw;
            }
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