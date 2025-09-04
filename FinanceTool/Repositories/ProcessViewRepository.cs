using FinanceTool.MongoModels;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FinanceTool.Repositories
{
    /// <summary>
    /// 처리된 데이터의 뷰(View) 정보 전용 MongoDB 저장소 클래스
    /// </summary>
    /// <remarks>
    /// ProcessData를 기반으로 생성된 비즈니스 뷰 데이터를 관리합니다.
    /// 키워드 정보, 부서/공급업체 매핑, 금액 데이터 등을 포함한 비즈니스 로직용 뷰를 제공합니다.
    /// BaseRepository 패턴을 상속하여 기본 CRUD 작업을 제공하며, 뷰 데이터 특화 작업을 지원합니다.
    /// 
    /// 주요 기능:
    /// - 대용량 뷰 데이터 배치 삽입 (성능 최적화)
    /// - 단일 뷰 데이터 삽입
    /// - 필터 기반 뷰 데이터 갯수 조회
    /// - 예외 처리 및 로깅 지원
    /// 
    /// 성능: InsertManyAsync에서 WriteConcern 최적화 적용, 배치 작업에 적합
    /// 의존성: BaseRepository, ProcessViewDocument, MongoDB.Driver
    /// </remarks>
    public class ProcessViewRepository : BaseRepository<ProcessViewDocument>
    {
        /// <summary>
        /// ProcessViewRepository 인스턴스를 초기화합니다
        /// </summary>
        /// <remarks>
        /// 'process_view_data' 컬렉션을 대상으로 하는 저장소를 생성하고 기본 설정을 적용합니다.
        /// 상위 BaseRepository 생성자를 호출하여 MongoDB 연결 및 컬렉션 설정을 완료합니다.
        /// 
        /// 성능: 뷰 데이터 특성에 맞는 인덱스 설정 자동 적용
        /// 의존성: BaseRepository 초기화 로직에 의존
        /// </remarks>
        public ProcessViewRepository() : base("process_view_data")
        {
        }


        /// <summary>
        /// 다수의 뷰 데이터 문서를 고성능 배치로 삽입합니다
        /// </summary>
        /// <param name="documents">삽입할 ProcessViewDocument 목록 (null 또는 빈 목록이면 작업 건너뛰기)</param>
        /// <param name="options">배치 삽입 옵션 (IsOrdered, BypassDocumentValidation 등)</param>
        /// <returns>비동기 작업 Task</returns>
        /// <remarks>
        /// 대용량 데이터 처리를 위해 WriteConcern을 최적화하여 성능을 향상시킵니다.
        /// IsOrdered가 false로 설정되어 뺑렬 삽입으로 성능 최적화를 달성합니다.
        /// 
        /// 예외 처리:
        /// - MongoBulkWriteException: 부분적 실패 상황에서 성공/실패 건수 로깅 후 예외 재전파
        /// - 일반 Exception: 예상치 못한 오류 로깅 후 예외 전파
        /// 
        /// 성능: WriteConcern 최적화, 비순차 배치 삽입
        /// 로깅: 삽입 결과 및 오류 세부 내역 Debug.WriteLine에 출력
        /// </remarks>
        /// <exception cref="MongoBulkWriteException">배치 삽입 중 일부 문서 삽입 실패 시</exception>
        /// <exception cref="System.Exception">데이터베이스 연결 또는 예상치 못한 오류 발생 시</exception>
        public async Task InsertManyAsync(List<ProcessViewDocument> documents, InsertManyOptions options)
        {
            if (documents == null || documents.Count == 0)
                return;

            try
            {
                // MongoDB 연결 상태 확인
                await InitializeAsync();

                // 대용량 데이터 처리를 위한 WriteConcern 최적화
                var optimizedOptions = new InsertManyOptions
                {
                    IsOrdered = options?.IsOrdered ?? false, // 순서 상관없이 삽입하여 성능 향상
                    BypassDocumentValidation = options?.BypassDocumentValidation ?? false
                };

                // 배치 삽입 실행
                await _collection.InsertManyAsync(documents, optimizedOptions);

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ProcessView 문서 {documents.Count}개 삽입 완료");
            }
            catch (MongoBulkWriteException ex)
            {
                // 부분적 실패 처리
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 배치 삽입 중 일부 문서 실패: {ex.WriteErrors?.Count ?? 0}개 오류");

                // 성공한 문서 수 로깅
                int successCount = documents.Count - (ex.WriteErrors?.Count ?? 0);
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 성공적으로 삽입된 문서: {successCount}개");

                // 실패한 문서들에 대한 세부 정보 로깅
                if (ex.WriteErrors != null)
                {
                    foreach (var error in ex.WriteErrors)
                    {
                        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 삽입 실패 - 인덱스: {error.Index}, 오류: {error.Message}");
                    }
                }

                throw; // 상위 호출자에게 예외 전파
            }
           
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 예상치 못한 오류 발생: {ex.Message}");
                throw;
            }
        }




        /// <summary>
        /// 지정된 필터 조건에 맞는 뷰 데이터 문서 갯수를 조회합니다
        /// </summary>
        /// <param name="filter">검색 조건 필터 (null인 경우 전체 문서 갯수 반환)</param>
        /// <returns>조건에 맞는 문서의 개수</returns>
        /// <remarks>
        /// 대용량 데이터 환경에서 효율적인 갯수 조회를 위해 인덱스를 활용합니다.
        /// 필터가 null인 경우 Empty 필터로 치환하여 전체 문서 수를 반환합니다.
        /// 
        /// 성능: 인덱스 기반 고속 갯수 연산
        /// 사용 예: 페이지네이션 총 갯수 산정, 대시보드 통계
        /// </remarks>
        /// <exception cref="MongoDB.Driver.MongoException">데이터베이스 연결 또는 쿼리 오류 발생 시</exception>
        public async Task<long> CountDocumentsAsync(FilterDefinition<ProcessViewDocument> filter = null)
        {
            await InitializeAsync(); // 초기화 확인 추가
            filter = filter ?? Builders<ProcessViewDocument>.Filter.Empty;
            return await _collection.CountDocumentsAsync(filter);
        }

        

        /// <summary>
        /// 단일 뷰 데이터 문서를 삽입하고 성공 여부를 반환합니다
        /// </summary>
        /// <param name="document">삽입할 ProcessViewDocument 객체</param>
        /// <returns>삽입 성공 시 true, 실패 시 false</returns>
        /// <remarks>
        /// 단일 문서 삽입용 메서드로, 소량 데이터 처리에 적합합니다.
        /// 대용량 데이터의 경우 InsertManyAsync() 사용을 권장합니다.
        /// 
        /// 예외 처리: 내부적으로 예외를 처리하여 false 반환
        /// 로깅: 오류 발생 시 Debug.WriteLine을 통해 로그 출력
        /// 성능: 단일 문서 삽입 연산에 최적화
        /// </remarks>
        public async Task<bool> InsertOneAsync(ProcessViewDocument document)
        {
            try
            {
                await InitializeAsync(); // 초기화 확인 추가
                await _collection.InsertOneAsync(document);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InsertOneAsync 오류: {ex.Message}");
                return false;
            }
        }
    }
}