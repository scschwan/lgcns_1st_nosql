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
    public class RawDataRepository : BaseRepository<RawDataDocument>
    {
        public RawDataRepository() : base("raw_data")
        {
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

        // RawDataRepository.cs에 추가할 메서드들

        /// <summary>
        /// raw_data 컬렉션의 문서 개수 조회
        /// </summary>
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
        /// 모든 raw_data 문서 조회
        /// </summary>
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
        /// 여러 문서 일괄 생성
        /// </summary>
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
        /// 모든 문서 삭제
        /// </summary>
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
        /// <param name="includeHidden">숨겨진 문서 포함 여부 (기본값: false)</param>
        /// <returns>페이징된 문서 목록</returns>
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
        /// <param name="skip">건너뛸 문서 수</param>
        /// <param name="limit">가져올 문서 수</param>
        /// <param name="customFilter">사용자 정의 필터 (선택사항)</param>
        /// <param name="customSort">사용자 정의 정렬 (선택사항)</param>
        /// <returns>페이징된 문서 목록</returns>
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
        /// <param name="skip">건너뛸 문서 수</param>
        /// <param name="limit">가져올 문서 수</param>
        /// <param name="projectionFields">조회할 필드 목록 (null이면 전체 필드)</param>
        /// <returns>프로젝션된 문서 목록</returns>
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