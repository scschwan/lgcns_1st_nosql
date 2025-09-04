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
    /// <remarks>
    /// Excel에서 가져온 원시 데이터를 MongoDB에 저장하고 관리하는 저장소입니다.
    /// BaseRepository를 상속받아 기본 CRUD 기능과 함께 원시 데이터 특화 기능을 제공합니다.
    /// 대용량 데이터 처리, 페이징, 필터링 기능을 최적화하여 제공합니다.
    /// </remarks>
    public class RawDataRepository : BaseRepository<RawDataDocument>
    {
        /// <summary>
        /// RawDataRepository 생성자
        /// </summary>
        /// <remarks>
        /// "raw_data" 컬렉션을 대상으로 하는 기본 저장소를 초기화합니다.
        /// BaseRepository의 기본 기능을 상속받아 원시 데이터 전용 기능을 추가합니다.
        /// </remarks>
        public RawDataRepository() : base("raw_data")
        {
        }


        /// <summary>
        /// 필터 조건에 맞는 원시 데이터 문서들을 조회
        /// </summary>
        /// <param name="filter">적용할 MongoDB 필터 정의</param>
        /// <param name="limit">가져올 최대 문서 수 (선택사항)</param>
        /// <returns>필터링된 원시 데이터 문서 목록</returns>
        /// <remarks>
        /// 유연한 필터링 조건을 사용하여 원시 데이터를 검색합니다.
        /// limit 매개변수를 통해 결과 수를 제한하여 대용량 데이터 처리 시 성능을 최적화합니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 조회 작업 실패 시</exception>
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
        /// raw_data 컬렉션의 전체 문서 개수를 조회
        /// </summary>
        /// <returns>컬렉션에 저장된 문서의 총 개수</returns>
        /// <remarks>
        /// 대용량 데이터 컬렉션에 대해 효율적인 개수 조회를 수행합니다.
        /// 전체 문서를 로드하지 않고 개수만 계산하여 성능을 최적화합니다.
        /// 페이징 처리나 데이터 통계에 활용됩니다.
        /// </remarks>
        /// <exception cref="MongoException">개수 조회 작업 실패 시</exception>
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
        /// raw_data 컬렉션의 모든 문서를 조회
        /// </summary>
        /// <returns>컬렉션의 모든 원시 데이터 문서 목록</returns>
        /// <remarks>
        /// 컬렉션의 모든 문서를 메모리로 로드합니다.
        /// 대용량 데이터의 경우 메모리 사용량이 많을 수 있으므로 주의해야 합니다.
        /// 소규모 데이터에 대해서만 사용하고, 대용량의 경우 GetPagedAsync() 사용을 권장합니다.
        /// </remarks>
        /// <exception cref="MongoException">데이터 조회 작업 실패 시</exception>
        /// <exception cref="OutOfMemoryException">대용량 데이터로 인한 메모리 부족 시</exception>
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
        /// 여러 원시 데이터 문서를 일괄 생성
        /// </summary>
        /// <param name="documents">생성할 원시 데이터 문서 목록</param>
        /// <returns>일괄 생성 작업을 나타내는 Task</returns>
        /// <remarks>
        /// Excel 파일에서 가져온 대용량 데이터를 효율적으로 저장합니다.
        /// MongoDB의 insertMany 연산을 사용하여 대량 데이터 삽입 성능을 최적화합니다.
        /// 널 체크를 통해 빈 목록에 대한 불필요한 작업을 방지합니다.
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 대량 삽입 작업 실패 시</exception>
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
        /// 조건에 맞는 원시 데이터 문서들을 일괄 삭제
        /// </summary>
        /// <param name="filter">삭제 조건을 정의하는 MongoDB 필터</param>
        /// <returns>삭제 작업을 나타내는 Task</returns>
        /// <remarks>
        /// 주의: 이 메서드는 지정된 조건에 맞는 모든 문서를 영구적으로 삭제합니다.
        /// 삭제 후 데이터 복구가 불가능하므로 신중하게 사용해야 합니다.
        /// 예외 발생 시 디버그 로그에 오류를 기록하고 예외를 다시 발생시킵니다.
        /// 성능: O(n) - 조건에 맞는 문서 수에 비례
        /// </remarks>
        /// <exception cref="MongoException">MongoDB 삭제 작업 실패 시</exception>
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


        /// <summary>
        /// 페이징 처리된 raw_data 문서 조회 (SessionDataProcessor 전용)
        /// MongoDataConverter.GetPagedRawDataAsync와 동일한 방식으로 동작
        /// </summary>
        /// <param name="skip">건너뛸 문서 수 (page * pageSize 형태로 전달)</param>
        /// <param name="limit">가져올 문서 수 (pageSize)</param>
        /// <param name="includeHidden">숨겨진 문서 포함 여부 (기본값: false, 숨겨진 문서 제외)</param>
        /// <returns>페이징된 문서 목록 (날짜 기준 내림차순 정렬)</returns>
        /// <remarks>
        /// 대용량 데이터셋에 대한 메모리 효율적인 페이징 기능을 제공합니다.
        /// ImportDate 기준 내림차순 정렬로 최신 데이터를 우선 반환합니다.
        /// 수행 시간과 결과 통계를 디버그 로그에 기록합니다.
        /// MongoDB 인덱스를 활용하여 빠른 정렬과 페이징을 지원합니다.
        /// 성능: O(log n + k) - 인덱스 사용 시, k는 limit 수
        /// </remarks>
        /// <exception cref="Exception">페이징 조회 작업 실패 시</exception>
        public async Task<List<RawDataDocument>> GetPagedAsync(int skip, int limit, bool includeHidden = false)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                // 필터 구성 (MongoDataConverter.GetPagedRawDataAsync와 동일한 로직)
                var filterBuilder = Builders<RawDataDocument>.Filter;
                var filter = filterBuilder.Empty;

                // includeHidden=false일 때는 숨겨진 문서를 제외
                if (!includeHidden)
                {
                    filter = filterBuilder.Eq(d => d.IsHidden, false);
                }

                // 날짜 기준 내림차순 정렬 (최신 데이터 우선)
                var sort = Builders<RawDataDocument>.Sort.Descending(d => d.ImportDate);

                // MongoDB 페이징 쿼리 실행
                var collection = await _dbManager.GetCollectionAsync<RawDataDocument>("raw_data");

                var documents = await collection
                    .Find(filter)
                    .Sort(sort)
                    .Skip(skip)
                    .Limit(limit)
                    .ToListAsync();

                sw.Stop();
                Debug.WriteLine($"[RawDataRepository] 페이징 조회 완료 - Skip: {skip:N0}, Limit: {limit:N0}, 결과: {documents.Count:N0}개, 소요시간: {sw.ElapsedMilliseconds}ms");

                return documents;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RawDataRepository] 페이징 조회 오류: {ex.Message}");
                throw new Exception($"페이징된 raw_data 조회 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        /// <summary>
        /// 페이징 처리된 raw_data 문서 조회 (고급 버전 - 필터와 정렬 옵션 포함)
        /// 대용량 데이터 처리를 위한 최적화된 버전
        /// </summary>
        /// <param name="skip">건너뛸 문서 수 (페이지네이션 오프셋)</param>
        /// <param name="limit">가져올 문서 수 (페이지 크기)</param>
        /// <param name="customFilter">사용자 정의 필터 (기본 필터와 AND 조건으로 결합, null 가능)</param>
        /// <param name="customSort">사용자 정의 정렬 (기본: ImportDate 내림차순, null 가능)</param>
        /// <returns>필터링과 정렬이 적용된 페이징된 문서 목록</returns>
        /// <remarks>
        /// 복잡한 검상 조건과 사용자 정의 정렬을 지원하는 고급 페이징 기능입니다.
        /// 사용자 필터는 기본 숨김 필터(IsHidden=false)와 AND 연결됩니다.
        /// 네트워크 전송과 데이터베이스 자원 사용을 최적화하여 대용량 데이터에 적합합니다.
        /// 수행 시간과 결과 통계를 디버그 로그에 상세히 기록합니다.
        /// 성능: O(log n + k) - 인덱스 및 정렬 최적화 시
        /// </remarks>
        /// <exception cref="Exception">고급 페이징 조회 작업 실패 시</exception>
        public async Task<List<RawDataDocument>> GetPagedAdvancedAsync(
            int skip,
            int limit,
            FilterDefinition<RawDataDocument> customFilter = null,
            SortDefinition<RawDataDocument> customSort = null)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                // 기본 필터 설정 (숨겨지지 않은 문서만)
                var filterBuilder = Builders<RawDataDocument>.Filter;
                var baseFilter = filterBuilder.Eq(d => d.IsHidden, false);

                // 사용자 정의 필터가 있으면 결합
                var finalFilter = customFilter != null
                    ? filterBuilder.And(baseFilter, customFilter)
                    : baseFilter;

                // 기본 정렬 설정 (날짜 기준 내림차순)
                var finalSort = customSort ?? Builders<RawDataDocument>.Sort.Descending(d => d.ImportDate);

                // MongoDB 컬렉션 가져오기
                var collection = await _dbManager.GetCollectionAsync<RawDataDocument>("raw_data");

                // 최적화된 쿼리 실행
                var documents = await collection
                    .Find(finalFilter)
                    .Sort(finalSort)
                    .Skip(skip)
                    .Limit(limit)
                    .ToListAsync();

                sw.Stop();
                Debug.WriteLine($"[RawDataRepository] 고급 페이징 조회 완료 - Skip: {skip:N0}, Limit: {limit:N0}, 결과: {documents.Count:N0}개, 소요시간: {sw.ElapsedMilliseconds}ms");

                return documents;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RawDataRepository] 고급 페이징 조회 오류: {ex.Message}");
                throw new Exception($"고급 페이징된 raw_data 조회 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        /// <summary>
        /// 초고속 페이징 조회 (프로젝션 적용으로 네트워크 트래픽 최소화)
        /// 특정 필드만 필요한 경우 사용
        /// </summary>
        /// <param name="skip">건너뛸 문서 수 (페이지네이션 오프셋)</param>
        /// <param name="limit">가져올 문서 수 (페이지 크기)</param>
        /// <param name="projectionFields">조회할 Data 필드의 키 목록 (null이면 전체 문서, 비어있으면 메타데이터만)</param>
        /// <returns>요청된 필드만 포함하는 프로젝션된 문서 목록</returns>
        /// <remarks>
        /// MongoDB Projection 기능을 활용하여 네트워크 대역폭과 메모리 사용량을 최소화합니다.
        /// 필수 메타데이터(_id, ImportDate, IsHidden)는 항상 포함되며, 지정된 Data 필드만 추가로 조회합니다.
        /// 대용량 문서에서 일부 필드만 필요한 경우 상당한 성능 향상을 제공합니다.
        /// 예를 들어, 계정과 금액 필드만 필요한 경우 [{"Account", "Amount"}] 사용
        /// 프로젝션 사용 시 디버그 로그에 선택된 필드 정보를 출력합니다.
        /// 성능: O(log n + k) + 네트워크 I/O 감소로 추가 성능 향상
        /// </remarks>
        /// <exception cref="Exception">프로젝션 페이징 조회 작업 실패 시</exception>
        public async Task<List<RawDataDocument>> GetPagedWithProjectionAsync(
            int skip,
            int limit,
            string[] projectionFields = null)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                // 필터 설정
                var filter = Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false);
                var sort = Builders<RawDataDocument>.Sort.Descending(d => d.ImportDate);

                var collection = await _dbManager.GetCollectionAsync<RawDataDocument>("raw_data");
                var query = collection.Find(filter).Sort(sort).Skip(skip).Limit(limit);

                List<RawDataDocument> documents;

                if (projectionFields != null && projectionFields.Length > 0)
                {
                    // 프로젝션 적용 (지정된 필드만 조회)
                    var projectionBuilder = Builders<RawDataDocument>.Projection;
                    var projection = projectionBuilder.Include("_id").Include("ImportDate").Include("IsHidden");

                    // 요청된 필드들을 프로젝션에 추가
                    foreach (var field in projectionFields)
                    {
                        projection = projection.Include($"Data.{field}");
                    }

                    documents = await query.Project<RawDataDocument>(projection).ToListAsync();

                    Debug.WriteLine($"[RawDataRepository] 프로젝션 필드: [{string.Join(", ", projectionFields)}]");
                }
                else
                {
                    // 프로젝션 없이 전체 문서 조회
                    documents = await query.ToListAsync();
                }

                sw.Stop();
                Debug.WriteLine($"[RawDataRepository] 프로젝션 페이징 조회 완료 - Skip: {skip:N0}, Limit: {limit:N0}, 결과: {documents.Count:N0}개, 소요시간: {sw.ElapsedMilliseconds}ms");

                return documents;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RawDataRepository] 프로젝션 페이징 조회 오류: {ex.Message}");
                throw new Exception($"프로젝션 페이징된 raw_data 조회 중 오류가 발생했습니다: {ex.Message}");
            }
        }
    }
}