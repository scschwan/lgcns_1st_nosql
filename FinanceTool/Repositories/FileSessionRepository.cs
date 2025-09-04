using FinanceTool.Data;
using FinanceTool.MongoModels;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;

namespace FinanceTool.Repositories
{
    /// <summary>
    /// 파일 처리 세션 전용 MongoDB 저장소 클래스
    /// </summary>
    /// <remarks>
    /// 다중 파일 업로드 및 처리 세션을 관리하는 전용 저장소입니다.
    /// 각 세션은 다수의 파일을 그룹화하여 처리하고, 진행 상태와 결과를 추적합니다.
    /// BaseRepository 패턴을 상속하여 기본 CRUD 작업을 제공하며, 세션 관리 특화 작업을 지원합니다.
    /// 
    /// 주요 기능:
    /// - 세션 완료 상태 및 결과 업데이트
    /// - 세션명 수정 및 관리
    /// - 세션에 파일 추가 및 연결
    /// - 세션별 총합 금액 및 행수 관리
    /// - 병합 세션 정보 업데이트
    /// - ObjectId 기반 직접 조회
    /// 
    /// 성능: 세션 ID와 상태 필드에 대한 인덱스 최적화 적용
    /// 의존성: BaseRepository, FileSessionDocument, MongoDB.Driver
    /// </remarks>
    public class FileSessionRepository : BaseRepository<FileSessionDocument>
    {
        /// <summary>
        /// FileSessionRepository 인스턴스를 초기화합니다
        /// </summary>
        /// <remarks>
        /// 'file_sessions' 컬렉션을 대상으로 하는 저장소를 생성하고 기본 설정을 적용합니다.
        /// 상위 BaseRepository 생성자를 호출하여 MongoDB 연결 및 컬렉션 설정을 완료합니다.
        /// 
        /// 성능: 세션 관리에 필요한 인덱스 자동 생성
        /// 의존성: BaseRepository 초기화 로직에 의존
        /// </remarks>
        public FileSessionRepository() : base("file_sessions")
        {
        }

        /// <summary>
        /// 세션의 완료 상태와 결과 정보를 업데이트합니다
        /// </summary>
        /// <param name="sessionId">업데이트할 세션의 ObjectId</param>
        /// <param name="status">새로운 세션 상태 ("Completed", "Failed" 등)</param>
        /// <param name="completedDate">세션 완료 시각</param>
        /// <param name="resultFilePath">처리 결과 파일 경로</param>
        /// <returns>업데이트 성공 시 true, 실패 시 false</returns>
        /// <remarks>
        /// 파일 처리 세션이 완료될 때 호출되어 최종 상태와 결과를 기록합니다.
        /// Status, CompletedDate, ResultFilePath 필드를 동시에 업데이트하여 데이터 일관성을 보장합니다.
        /// 
        /// 예외 처리: 내부적으로 예외를 처리하여 false 반환
        /// 로깅: 업데이트 결과 및 오류 Debug.WriteLine에 출력
        /// 성능: 세션 ID 기반 인덱스 활용
        /// </remarks>
        public async Task<bool> UpdateSessionCompletionAsync(ObjectId sessionId, string status, DateTime completedDate, string resultFilePath)
        {
            try
            {
                var filter = Builders<FileSessionDocument>.Filter.Eq(s => s.Id, sessionId);
                var update = Builders<FileSessionDocument>.Update
                    .Set(s => s.Status, status)
                    .Set(s => s.CompletedDate, completedDate)
                    .Set(s => s.ResultFilePath, resultFilePath);

                var result = await _collection.UpdateOneAsync(filter, update);

                Debug.WriteLine($"세션 업데이트 결과 - ModifiedCount: {result.ModifiedCount}, MatchedCount: {result.MatchedCount}");

                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 완료 정보 업데이트 중 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 세션의 디스플레이 이름을 업데이트합니다
        /// </summary>
        /// <param name="sessionId">업데이트할 세션의 ObjectId</param>
        /// <param name="newSessionName">새로운 세션명</param>
        /// <returns>업데이트 성공 시 true, 실패 시 false</returns>
        /// <remarks>
        /// 사용자가 세션명을 수정할 때 사용되는 메서드입니다.
        /// 데이터베이스 필드명을 직접 사용하여 MongoDB 문서를 업데이트합니다.
        /// 
        /// 예외 처리: 내부적으로 예외를 처리하여 false 반환
        /// 로깅: 오류 발생 시 Debug.WriteLine을 통해 로그 출력
        /// 성능: 세션 ID 기반 인덱스 활용
        /// 주의: 필드명을 문자열로 직접 지정 (강한 타입 안전성 경고)
        /// </remarks>
        public async Task<bool> UpdateSessionNameAsync(ObjectId sessionId, string newSessionName)
        {
            try
            {
                var filter = Builders<FileSessionDocument>.Filter.Eq("_id", sessionId);
                var update = Builders<FileSessionDocument>.Update.Set("session_name", newSessionName);

                var result = await _collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션명 업데이트 오류: {ex.Message}");
                return false;
            }
        }

      

        /// <summary>
        /// 지정된 세션에 파일 ID를 추가합니다
        /// </summary>
        /// <param name="sessionId">파일을 추가할 세션의 ObjectId</param>
        /// <param name="fileId">추가할 파일의 ObjectId</param>
        /// <returns>추가 성공 시 true, 실패 시 false</returns>
        /// <remarks>
        /// 세션에 새로운 파일을 연결할 때 사용됩니다.
        /// AddToSet 연산자를 사용하여 중복 추가를 방지합니다.
        /// 파일 ID는 FileIds 배열 필드에 추가되어 세션과 파일 간 연결 관계를 유지합니다.
        /// 
        /// 예외 처리: 내부적으로 예외를 처리하여 false 반환
        /// 로깅: 오류 발생 시 Debug.WriteLine을 통해 로그 출력
        /// 성능: 세션 ID 기반 인덱스 활용, AddToSet 연산 최적화
        /// </remarks>
        public async Task<bool> AddFileToSessionAsync(ObjectId sessionId, ObjectId fileId)
        {
            try
            {
                var filter = Builders<FileSessionDocument>.Filter.Eq(d => d.Id, sessionId);
                var update = Builders<FileSessionDocument>.Update.AddToSet(d => d.FileIds, fileId);

                var result = await _collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션에 파일 추가 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 세션의 집계 데이터(총 금액, 총 행수)를 업데이트합니다
        /// </summary>
        /// <param name="sessionId">업데이트할 세션의 ObjectId</param>
        /// <param name="totalAmount">세션 내 모든 파일의 총 금액</param>
        /// <param name="totalRows">세션 내 모든 파일의 총 행수</param>
        /// <returns>업데이트 성공 시 true, 실패 시 false</returns>
        /// <remarks>
        /// 세션에 포함된 모든 파일들의 금액과 행수를 집계하여 저장합니다.
        /// 파일 추가/제거 또는 데이터 변경 시 통계 정보를 동기화하기 위해 사용됩니다.
        /// TotalAmount와 TotalRows 필드를 동시에 업데이트하여 데이터 일관성을 보장합니다.
        /// 
        /// 예외 처리: 내부적으로 예외를 처리하여 false 반환
        /// 로깅: 오류 발생 시 Debug.WriteLine을 통해 로그 출력
        /// 성능: 세션 ID 기반 인덱스 활용
        /// </remarks>
        public async Task<bool> UpdateSessionTotalsAsync(ObjectId sessionId, decimal totalAmount, decimal totalRows)
        {
            try
            {
                var filter = Builders<FileSessionDocument>.Filter.Eq(d => d.Id, sessionId);
                var update = Builders<FileSessionDocument>.Update
                    .Set(d => d.TotalAmount, totalAmount)
                    .Set(d => d.TotalRows, totalRows);

                var result = await _collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 총합 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ObjectId를 사용하여 세션 문서를 직접 조회합니다
        /// </summary>
        /// <param name="id">조회할 세션의 ObjectId</param>
        /// <returns>조회된 FileSessionDocument 객체, 조회 실패 시 null</returns>
        /// <remarks>
        /// BaseRepository의 GetByIdAsync 메서드를 오버라이드하여 ObjectId 직접 지원을 추가합니다.
        /// 기본 BaseRepository 메서드는 string ID를 사용하지만, 이 메서드는 ObjectId를 직접 처리합니다.
        /// MongoDB 필드명 "_id"를 직접 사용하여 검색 성능을 최적화합니다.
        /// 
        /// 예외 처리: 내부적으로 예외를 처리하여 null 반환
        /// 로깅: 오류 발생 시 Debug.WriteLine을 통해 로그 출력
        /// 성능: Primary key 기반 최고 성능 조회
        /// 주의: BaseRepository의 GetByIdAsync(string)와 다른 시그니처 (new 키워드 기대)
        /// </remarks>
        public async Task<FileSessionDocument> GetByIdAsync(ObjectId id)
        {
            try
            {
                var filter = Builders<FileSessionDocument>.Filter.Eq("_id", id);
                var cursor = await _collection.FindAsync(filter);
                return await cursor.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 조회 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 여러 세션을 병합할 때 병합된 세션의 모든 정보를 일괄 업데이트합니다
        /// </summary>
        /// <param name="sessionId">병합 대상 세션의 ObjectId</param>
        /// <param name="mergedSessionName">병합 후 새로운 세션명</param>
        /// <param name="mergedAccountName">병합 후 계정명</param>
        /// <param name="accountColumnName">계정 컬럼명</param>
        /// <param name="allFileIds">병합된 모든 파일 ID 목록</param>
        /// <param name="totalAmount">병합된 총 금액</param>
        /// <param name="totalRows">병d합된 총 행수</param>
        /// <returns>업데이트 성공 시 true, 실패 시 false</returns>
        /// <remarks>
        /// 다중 세션 병합 작업에서 사용되는 종합 업데이트 메서드입니다.
        /// 세션명, 계정 정보, 파일 목록, 총합 데이터를 한 번에 업데이트하여 데이터 일관성을 보장합니다.
        /// 추가로 UpdatedDate를 현재 시간(UTC)으로 설정하여 병합 시점을 기록합니다.
        /// 
        /// 예외 처리: 내부적으로 예외를 처리하여 false 반환
        /// 로깅: 오류 발생 시 Debug.WriteLine을 통해 로그 출력
        /// 성능: 세션 ID 기반 인덱스 활용, 단일 업데이트 연산 최적화
        /// 주의: 필드명을 문자열로 직접 지정 (강한 타입 안전성 경고)
        /// </remarks>
        public async Task<bool> UpdateMergedSessionAsync(
            ObjectId sessionId,
            string mergedSessionName,
            string mergedAccountName,
            string accountColumnName,
            List<ObjectId> allFileIds,
            decimal totalAmount,
            decimal totalRows)
        {
            try
            {
                var filter = Builders<FileSessionDocument>.Filter.Eq("_id", sessionId);
                var update = Builders<FileSessionDocument>.Update
                    .Set("session_name", mergedSessionName)
                    .Set("account_name", mergedAccountName)
                    .Set("account_column_name", accountColumnName)
                    .Set("file_ids", allFileIds)
                    .Set("total_amount", totalAmount)
                    .Set("total_rows", totalRows)
                    .Set("file_count", allFileIds.Count)
                    .Set("updated_date", DateTime.UtcNow);

                var result = await _collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"병합된 세션 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        // TODO: 다음 특화 메서드들이 구현되어야 합니다:
        // - GetActiveSessionsAsync(): 활성 상태인 세션들 조회
        // - GetBySessionNameAsync(string sessionName): 세션명으로 세션 조회
        // - UpdateSessionStatusAsync(ObjectId sessionId, string status): 세션 상태만 단독 업데이트
        // - DeleteSessionAsync(ObjectId sessionId): 세션 완전 삭제
        // - GetSessionStatisticsAsync(): 세션 통계 정보 조회
    }
}