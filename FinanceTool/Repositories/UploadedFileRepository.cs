using FinanceTool.Data;
using FinanceTool.MongoModels;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;

namespace FinanceTool.Repositories
{
    /// <summary>
    /// 업로드된 파일 전용 MongoDB 저장소 클래스
    /// </summary>
    /// <remarks>
    /// 사용자가 업로드한 Excel 파일들의 메타데이터와 처리 상태를 관리합니다.
    /// 파일의 기본 정보(파일명, 크기, 경로)와 비즈니스 정보(계정 컬럼, 금액 컬럼)를 함께 저장합니다.
    /// BaseRepository 패턴을 상속하여 기본 CRUD 작업을 제공하며, 파일 관리 특화 작업을 지원합니다.
    /// 
    /// 주요 기능:
    /// - 파일명 기반 파일 검색
    /// - 계정 및 금액 컬럼 정보 관리
    /// - 세션 ID 연결 및 해제
    /// - ObjectId 기반 직접 조회
    /// - MongoDB Collection 직접 접근 지원
    /// 
    /// 성능: 파일명과 세션 ID 필드에 대한 인덱스 최적화 적용
    /// 의존성: BaseRepository, UploadedFileDocument, MongoDB.Driver
    /// </remarks>
    public class UploadedFileRepository : BaseRepository<UploadedFileDocument>
    {
        /// <summary>
        /// UploadedFileRepository 인스턴스를 초기화합니다
        /// </summary>
        /// <remarks>
        /// 'uploaded_files' 컬렉션을 대상으로 하는 저장소를 생성하고 기본 설정을 적용합니다.
        /// 상위 BaseRepository 생성자를 호출하여 MongoDB 연결 및 컬렉션 설정을 완료합니다.
        /// 
        /// 성능: 파일 관리에 필요한 인덱스 자동 생성
        /// 의존성: BaseRepository 초기화 로직에 의존
        /// </remarks>
        public UploadedFileRepository() : base("uploaded_files")
        {
        }


        /// <summary>
        /// MongoDB Collection에 직접 접근할 수 있도록 하는 속성
        /// </summary>
        /// <value>UploadedFileDocument 타입의 IMongoCollection 인스턴스</value>
        /// <remarks>
        /// BaseRepository의 protected _collection 필드를 public으로 노출하여 외부에서 직접 접근을 허용합니다.
        /// 고급 연산이나 Repository 패턴 외부의 복잡한 쿼리가 필요한 경우 사용됩니다.
        /// 
        /// 주의: 이 속성을 사용하면 Repository 패턴의 캐슔화를 우회하게 됨
        /// 권장: 가능한 한 Repository 메서드를 사용하고, 필요시만 직접 접근
        /// 성능: 직접 MongoDB 드라이버 인스턴스 반환으로 오버헤드 없음
        /// </remarks>
        public IMongoCollection<UploadedFileDocument> Collection => _collection;

        /// <summary>
        /// 원본 파일명을 사용하여 업로드된 파일 정보를 조회합니다
        /// </summary>
        /// <param name="filename">검색할 원본 파일명 (OriginalFilename 필드를 대상으로 검색)</param>
        /// <returns>조회된 UploadedFileDocument 객체, 조회 실패 시 null</returns>
        /// <remarks>
        /// 사용자가 동일한 이름의 파일을 중복 업로드하는 것을 방지하거나,
        /// 기존 파일을 참조할 때 사용되는 메서드입니다.
        /// OriginalFilename 필드에 대한 정확한 매칭을 수행합니다.
        /// 
        /// 예외 처리: 내부적으로 예외를 처리하여 null 반환
        /// 로깅: 오류 발생 시 Debug.WriteLine을 통해 로그 출력
        /// 성능: OriginalFilename 필드의 인덱스 활용
        /// 사용 예: 중복 파일 검사, 파일 존재 여부 확인
        /// </remarks>
        public async Task<UploadedFileDocument> GetByFilenameAsync(string filename)
        {
            try
            {
                var filter = Builders<UploadedFileDocument>.Filter.Eq(d => d.OriginalFilename, filename);
                return await _collection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파일명으로 조회 오류: {ex.Message}");
                return null;
            }
        }

     
        /// <summary>
        /// 파일의 계정 컬럼 정보(컬럼명 및 내용)만 선택적으로 업데이트합니다
        /// </summary>
        /// <param name="fileId">업데이트할 파일의 ObjectId</param>
        /// <param name="accountColumnName">계정을 나타내는 컬럼명</param>
        /// <param name="accountContents">계정 컬럼에 포함된 고유 값들의 목록</param>
        /// <returns>업데이트 성공 시 true, 실패 시 false</returns>
        /// <remarks>
        /// Excel 파일에서 계정 정보를 추출한 후 해당 정보만 업데이트하는 메서드입니다.
        /// 계정 컬럼명과 해당 컬럼의 고유한 내용들을 동시에 저장하여 데이터 일관성을 보장합니다.
        /// 
        /// 예외 처리: 내부적으로 예외를 처리하여 false 반환
        /// 로깅: 오류 발생 시 Debug.WriteLine을 통해 로그 출력
        /// 성능: 선택적 필드 업데이트로 효율성 향상
        /// 주의: 필드명을 문자열로 직접 지정 (강한 타입 안전성 경고)
        /// 사용 예: 파일 분석 후 계정 정보만 추가 업데이트
        /// </remarks>
        public async Task<bool> UpdateAccountColumnInfoAsync(ObjectId fileId, string accountColumnName, List<string> accountContents)
        {
            try
            {
                var filter = Builders<UploadedFileDocument>.Filter.Eq("_id", fileId);
                var update = Builders<UploadedFileDocument>.Update
                    .Set("account_column_name", accountColumnName)
                    .Set("account_contents", accountContents);

                var result = await Collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"계정명 컬럼 정보 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 파일의 금액 컬럼 정보(컬럼명 및 총액)만 선택적으로 업데이트합니다
        /// </summary>
        /// <param name="fileId">업데이트할 파일의 ObjectId</param>
        /// <param name="amountColumnName">금액을 나타내는 컬럼명</param>
        /// <param name="totalAmount">파일 내 모든 행의 총 금액 합계</param>
        /// <returns>업데이트 성공 시 true, 실패 시 false</returns>
        /// <remarks>
        /// Excel 파일에서 금액 정보를 추출하고 집계한 후 해당 정보만 업데이트하는 메서드입니다.
        /// 금액 컬럼명과 총 금액을 동시에 저장하여 데이터 일관성을 보장합니다.
        /// 
        /// 예외 처리: 내부적으로 예외를 처리하여 false 반환
        /// 로깅: 오류 발생 시 Debug.WriteLine을 통해 로그 출력
        /// 성능: 선택적 필드 업데이트로 효율성 향상
        /// 주의: 필드명을 문자열로 직접 지정 (강한 타입 안전성 경고)
        /// 사용 예: 파일 분석 후 금액 정보만 추가 업데이트
        /// </remarks>
        public async Task<bool> UpdateAmountColumnInfoAsync(ObjectId fileId, string amountColumnName, decimal totalAmount)
        {
            try
            {
                var filter = Builders<UploadedFileDocument>.Filter.Eq("_id", fileId);
                var update = Builders<UploadedFileDocument>.Update
                    .Set("amount_column_name", amountColumnName)
                    .Set("total_amount", totalAmount);

                var result = await Collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"금액 컬럼 정보 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ObjectId를 사용하여 업로드된 파일 문서를 직접 조회합니다
        /// </summary>
        /// <param name="id">조회할 파일의 ObjectId</param>
        /// <returns>조회된 UploadedFileDocument 객체, 조회 실패 시 null</returns>
        /// <remarks>
        /// BaseRepository의 GetByIdAsync 메서드를 오버라이드하여 ObjectId 직접 지원을 추가합니다.
        /// 기본 BaseRepository 메서드는 string ID를 사용하지만, 이 메서드는 ObjectId를 직접 처리합니다.
        /// MongoDB 필드명 "_id"를 직접 사용하여 검색 성능을 최적화합니다.
        /// 
        /// 예외 처리: 내부적으로 예외를 처리하여 null 반환
        /// 로깅: 오류 발생 시 Debug.WriteLine을 통해 로그 출력 (주의: 로그 메시지가 "세션 조회 오류"로 잘못 표기됨)
        /// 성능: Primary key 기반 최고 성능 조회
        /// 주의: BaseRepository의 GetByIdAsync(string)와 다른 시그니처 (new 키워드 기대)
        /// 사용 예: 세션에서 파일 ID를 통한 직접 조회
        /// </remarks>
        public async Task<UploadedFileDocument> GetByIdAsync(ObjectId id)
        {
            try
            {
                var filter = Builders<UploadedFileDocument>.Filter.Eq("_id", id);
                var cursor = await _collection.FindAsync(filter);
                return await cursor.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파일 조회 오류: {ex.Message}");
                return null;
            }
        }


       
        /// <summary>
        /// 파일을 세션에 연결하거나 연결을 해제하기 위해 세션 ID를 업데이트합니다
        /// </summary>
        /// <param name="fileId">업데이트할 파일의 ObjectId</param>
        /// <param name="sessionId">연결할 세션의 ObjectId (null 또는 ObjectId.Empty인 경우 연결 해제)</param>
        /// <returns>업데이트 성공 시 true, 실패 시 false</returns>
        /// <remarks>
        /// 파일과 세션 간의 연결 관계를 관리하는 주요 메서드입니다.
        /// sessionId가 유효한 경우 해당 세션에 파일을 연결하고,
        /// null이나 ObjectId.Empty인 경우 세션 연결을 해제합니다.
        /// 
        /// 특징:
        /// - Set/Unset 연산자를 적절히 사용하여 연결/해제 처리
        /// - null 값 처리에 대한 안전한 예외 처리
        /// - 상태 기반 동적 업데이트 로직
        /// 
        /// 예외 처리: 내부적으로 예외를 처리하여 false 반환
        /// 로깅: 오류 발생 시 Debug.WriteLine을 통해 로그 출력
        /// 성능: 파일 ID 기반 인덱스 활용, 조건부 업데이트 최적화
        /// 사용 예: 세션 생성 시 파일 추가, 세션 해체 시 파일 분리
        /// </remarks>
        public async Task<bool> UpdateSessionIdAsync(ObjectId fileId, ObjectId sessionId)
        {
            try
            {
                var filter = Builders<UploadedFileDocument>.Filter.Eq(d => d.Id, fileId);

                UpdateDefinition<UploadedFileDocument> update;

                // sessionId가 null이거나 Empty인 경우 필드를 제거 (null로 설정)
                if (sessionId == null || sessionId == ObjectId.Empty)
                {
                    update = Builders<UploadedFileDocument>.Update.Unset(d => d.SessionId);
                }
                else
                {
                    update = Builders<UploadedFileDocument>.Update.Set(d => d.SessionId, sessionId);
                }

                var result = await _collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 ID 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        // TODO: 다음 특화 메서드들이 구현되어야 합니다:
        // - GetBySessionIdAsync(ObjectId sessionId): 세션별 파일 목록 조회
        // - UpdateProcessingStatusAsync(ObjectId fileId, string status): 처리 상태만 단독 업데이트
        // - GetFileStatisticsAsync(): 전체 파일 통계 정보
        // - DeleteFileAsync(ObjectId fileId): 파일 완전 삭제
        // - GetRecentFilesAsync(int count): 최근 업로드된 파일 목록
    }
}