using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FinanceTool.MongoModels;
using MongoDB.Driver;

namespace FinanceTool.Repositories
{
    /// <summary>
    /// 컬럼 매핑 정보 전용 MongoDB 저장소 클래스
    /// </summary>
    /// <remarks>
    /// Excel 데이터의 원본 컬럼명과 사용자 친화적 표시명 간의 매핑 관계를 관리합니다.
    /// BaseRepository 패턴을 상속하여 기본 CRUD 작업을 제공하며, 컬럼 매핑 특화 작업을 지원합니다.
    /// 
    /// 주요 기능:
    /// - 원본 컬럼명에서 표시명으로 변환
    /// - 표시 가능한 컬럼 조회 및 순서 정렬
    /// - 컬럼 표시 설정 및 순서 관리
    /// - 데이터 타입 및 가시성 설정
    /// 
    /// 성능: OriginalName 및 Sequence 필드에 대한 인덱스 최적화 적용
    /// 의존성: BaseRepository, ColumnMappingDocument, MongoDB.Driver
    /// </remarks>
    public class ColumnMappingRepository : BaseRepository<ColumnMappingDocument>
    {
        /// <summary>
        /// ColumnMappingRepository 인스턴스를 초기화합니다
        /// </summary>
        /// <remarks>
        /// 'column_mapping' 컬렉션을 대상으로 하는 저장소를 생성하고 기본 설정을 적용합니다.
        /// 상위 BaseRepository 생성자를 호출하여 MongoDB 연결 및 컬렉션 설정을 완료합니다.
        /// 
        /// 성능: 컬랙션 초기화 시 OriginalName 및 Sequence 인덱스가 자동 생성됩니다
        /// 의존성: BaseRepository 초기화 로직에 의존
        /// </remarks>
        public ColumnMappingRepository() : base("column_mapping")
        {
        }

        /// <summary>
        /// 표시 가능한 컬럼 매핑들을 Sequence 순서로 조회합니다
        /// </summary>
        /// <returns>
        /// IsVisible이 true로 설정된 컬럼 매핑 목록 (Sequence 오름차순 정렬)
        /// </returns>
        /// <remarks>
        /// UI에서 사용자에게 표시할 컬럼들만 필터링하여 반환합니다.
        /// Sequence 필드에 따라 오름차순으로 정렬되어 표시 순서를 보장합니다.
        /// 
        /// 성능: IsVisible 및 Sequence 필드의 인덱스를 활용한 빠른 조회
        /// 사용 예: DataGridView 컬럼 설정, UI 표시 순서 관리
        /// </remarks>
        /// <exception cref="MongoDB.Driver.MongoException">데이터베이스 연결 또는 쿼리 오류 발생 시</exception>
        public async Task<List<ColumnMappingDocument>> GetVisibleColumnsAsync()
        {
            var filter = Builders<ColumnMappingDocument>.Filter.Eq(c => c.IsVisible, true);
            var sort = Builders<ColumnMappingDocument>.Sort.Ascending(c => c.Sequence);

            return await _collection.Find(filter).Sort(sort).ToListAsync();
        }

        /// <summary>
        /// 전체 컬럼 매핑 정보를 조회합니다 (가시성 상관없이)
        /// </summary>
        /// <returns>
        /// 데이터베이스에 저장된 모든 컬럼 매핑 문서 목록. 오류 발생 시 빈 목록 반환
        /// </returns>
        /// <remarks>
        /// 관리자 기능이나 전체 컬럼 매핑 현황을 파악할 때 사용합니다.
        /// GetVisibleColumnsAsync()와 달리 IsVisible 필터링을 적용하지 않습니다.
        /// 
        /// 예외 처리: MongoDB 연결 오류 등의 예외를 내부적으로 처리하여 빈 목록 반환
        /// 로깅: 오류 발생 시 Debug.WriteLine을 통해 로그 출력
        /// 성능: 필터 조건 없이 전체 컴렉션 스캔
        /// </remarks>
        public async Task<List<ColumnMappingDocument>> GetAllAsync()
        {
            try
            {
                var cursor = await _collection.FindAsync(FilterDefinition<ColumnMappingDocument>.Empty);
                return await cursor.ToListAsync();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"컬럼 매핑 조회 오류: {ex.Message}");
                return new List<ColumnMappingDocument>();
            }
        }

        // TODO: 다음 특화 메서드들이 구현되어야 합니다:
        // - GetByOriginalNamesAsync(List<string> originalNames): 원본 컬럼명 목록으로 매핑 조회
        // - GetByOriginalNameAsync(string originalName): 단일 원본 컬럼명으로 매핑 조회
        // - UpdateDisplayNameAsync(string originalName, string displayName): 표시명 업데이트
        // - UpdateSequenceAsync(Dictionary<string, int> sequenceMap): 표시 순서 업데이트
        // - UpdateVisibilityAsync(string originalName, bool isVisible): 가시성 업데이트
    }
}