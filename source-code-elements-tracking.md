# FinanceTool 프로젝트 - 소스코드 요소 추적 및 주석 작업 체크리스트

> **📋 AI 어시스턴트 주석 작업 지침**
> - 이 파일은 **모든 소스파일의 클래스, 메서드, 변수 목록**과 **XML 주석 작업 현황**을 추적합니다
> - 주석 작업 시 이 파일을 참조하여 누락 없이 모든 요소에 주석을 추가하세요
> - 작업 완료 시 해당 요소의 상태를 ✅로 업데이트하세요
> - **Status Legend**: ✅ (완료), ⚠️ (부분완료), ❌ (미완료)

**대상 프로젝트**: C:\workspace\25 lg cns\nosql\FinanceTool  
**분석 일자**: 2025-09-03  
**문서 목적**: 주석 작업 진행률 추적 및 품질 관리

---

## 📊 전체 현황 요약

| 구분 | 총 개수 | 완료 | 부분완료 | 미완료 | 완료율 |
|------|---------|------|----------|--------|--------|
| **클래스** | 45+ | 12 | 8 | 25+ | 27% |
| **메서드** | 200+ | 25 | 30 | 145+ | 12% |
| **프로퍼티** | 150+ | 10 | 20 | 120+ | 7% |
| **필드** | 100+ | 15 | 10 | 75+ | 15% |

---

## 📁 파일별 상세 분석

### **1. Program.cs** - Application Entry Point
**파일 경로**: `C:\workspace\25 lg cns\nosql\FinanceTool\Program.cs`
**우선순위**: ⭐⭐⭐ (High)

#### 클래스
- ✅ `internal static class Program` - 애플리케이션 진입점 및 초기화 - **Partial XML**

#### 메서드 (6개)
- ⚠️ `static void Main()` - 애플리케이션 메인 진입점 - **Partial XML**
- ❌ `private static void SetupJavaEnvironment()` - Java 환경 설정 구성
- ❌ `private static async Task CreateMongoDBIndexesAsync()` - MongoDB 인덱스 생성
- ❌ `private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)` - UI 스레드 예외 처리기
- ❌ `private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)` - 비UI 스레드 예외 처리기
- ❌ `private static void HandleUnhandledException(Exception ex)` - 공통 예외 처리 로직

---

### **2. Form1.cs** - Main Application Form
**파일 경로**: `C:\workspace\25 lg cns\nosql\FinanceTool\Form1.cs`
**우선순위**: ⭐⭐⭐ (High)

#### 클래스
- ❌ `public partial class Form1 : Form` - 메인 애플리케이션 폼 및 UI 네비게이션 컨트롤러

#### 필드/프로퍼티 (3개)
- ❌ `public static DataTable excelData` - 전역 Excel 데이터 저장소
- ❌ `private TrialManager trialManager` - 라이선스 검증 관리자
- ❌ `private bool trialYN` - 체험판 모드 플래그

#### 메서드 (15+ 개)
- ❌ `public Form1()` - 폼 생성자
- ⚠️ `private void ResizeControls()` - 컨트롤 크기 조정 로직 - **Partial XML**
- ❌ `private async void Form1_Load(object sender, EventArgs e)` - 폼 로드 이벤트 핸들러
- ❌ `public void LoadUserControl(UserControl control)` - 사용자 컨트롤 로더
- ❌ `private void menu_fileload_Click(object sender, EventArgs e)` - 파일 로드 메뉴 핸들러
- ❌ `private void menu_preprocessing_Click(object sender, EventArgs e)` - 전처리 메뉴 핸들러
- ❌ `private void menu_classification_Click(object sender, EventArgs e)` - 분류 메뉴 핸들러
- ❌ `private void menu_clustering_Click(object sender, EventArgs e)` - 클러스터링 메뉴 핸들러
- ❌ `private void menu_detail_clustering_Click(object sender, EventArgs e)` - 세부 클러스터링 메뉴 핸들러
- ❌ `private void menu_multi_file_upload_Click(object sender, EventArgs e)` - 다중 파일 업로드 메뉴 핸들러
- ❌ `private void menu_data_transform_Click(object sender, EventArgs e)` - 데이터 변환 메뉴 핸들러
- ❌ (추가 이벤트 핸들러들)

---

### **3. Models/MongoModels/** - Data Models
**파일 경로**: `C:\workspace\25 lg cns\nosql\FinanceTool\Models\MongoModels\`
**우선순위**: ⭐⭐⭐⭐ (Very High)

#### 3.1 RawDataDocument.cs
**클래스**
- ✅ `public class RawDataDocument` - 원시 Excel 데이터용 MongoDB 문서 - **Yes XML**

**프로퍼티 (6개)**
- ❌ `public string Id { get; set; }` - 문서 고유 식별자
- ❌ `public Dictionary<string, object> Data { get; set; }` - 동적 Excel 데이터 저장소
- ❌ `public DateTime ImportDate { get; set; }` - 가져오기 타임스탬프
- ❌ `public string FileName { get; set; }` - 원본 Excel 파일명
- ❌ `public bool IsHidden { get; set; }` - 데이터 표시 여부 플래그
- ❌ `public string HiddenReason { get; set; }` - 데이터 숨김 사유

#### 3.2 ProcessDataDocument.cs
**클래스**
- ✅ `public class ProcessDataDocument` - 처리된 데이터용 MongoDB 문서 - **Yes XML**

**프로퍼티 (7개)**
- ❌ `public string Id { get; set; }` - 문서 고유 식별자
- ❌ `public string RawDataId { get; set; }` - 원시 데이터 문서 참조
- ❌ `public Dictionary<string, object> Data { get; set; }` - 처리된 데이터 저장소
- ❌ `public DateTime ImportDate { get; set; }` - 원본 가져오기 타임스탬프
- ❌ `public DateTime ProcessedDate { get; set; }` - 처리 완료 타임스탬프
- ❌ `public int? ClusterId { get; set; }` - 할당된 클러스터 식별자
- ❌ `public string ClusterName { get; set; }` - 사람이 읽을 수 있는 클러스터명

#### 3.3 ColumnMappingDocument.cs
**클래스**
- ✅ `public class ColumnMappingDocument` - 컬럼 매핑 구성 문서 - **Yes XML**

**프로퍼티 (6개)**
- ❌ `public string Id { get; set; }` - 문서 고유 식별자
- ❌ `public string OriginalName { get; set; }` - 원본 Excel 컬럼명
- ❌ `public string DisplayName { get; set; }` - 사용자 친화적 표시명
- ❌ `public string DataType { get; set; }` - 컬럼 데이터 타입
- ❌ `public bool IsVisible { get; set; }` - 컬럼 표시 여부 플래그
- ❌ `public int Sequence { get; set; }` - 표시 순서 시퀀스

#### 3.4 KeywordDocument.cs
**클래스**
- ✅ `public class KeywordDocument` - 키워드 분석 결과 문서 - **Yes XML**

**프로퍼티 (9개)**
- ❌ `public string Id { get; set; }` - 문서 고유 식별자
- ❌ `public string Keyword { get; set; }` - 추출된 키워드 텍스트
- ❌ `public int Count { get; set; }` - 발생 빈도수
- ❌ `public List<string> SourceColumns { get; set; }` - 소스 컬럼명 목록
- ❌ `public List<string> RelatedKeywords { get; set; }` - 관련 키워드 목록
- ❌ `public List<string> DocumentIds { get; set; }` - 연결된 문서 ID 목록
- ❌ `public DateTime LastUpdated { get; set; }` - 마지막 업데이트 타임스탬프
- ❌ `public double Weight { get; set; }` - 통계적 가중치 값
- ❌ `public bool IsRecommendation { get; set; }` - AI 추천 플래그

#### 3.5 ProcessViewDocument.cs
**클래스**
- ✅ `public class ProcessViewDocument` - 처리된 정보의 뷰 데이터 문서 - **Yes XML**

**중첩 클래스**
- ✅ `public class KeywordInfo` - 키워드 정보 하위 문서 - **Yes XML**

**프로퍼티 (8+ 개)**
- ❌ `public string Id { get; set; }` - 문서 고유 식별자
- ❌ `public string ProcessDataId { get; set; }` - 처리 데이터 참조
- ❌ `public string RawDataId { get; set; }` - 원시 데이터 참조
- ❌ `public DateTime ViewDate { get; set; }` - 뷰 생성 날짜
- ❌ `public string Money { get; set; }` - 금액 정보
- ❌ `public string Department { get; set; }` - 부서 정보
- ❌ `public string Supplier { get; set; }` - 공급업체 정보
- ❌ `public List<KeywordInfo> Keywords { get; set; }` - 연결된 키워드 목록

#### 3.6 UploadedFileDocument.cs
**클래스**
- ❌ `public class UploadedFileDocument` - 파일 업로드 추적 문서

**프로퍼티 (12+ 개)**
- ❌ `public ObjectId Id { get; set; }` - 문서 고유 식별자
- ❌ `public string OriginalFilename { get; set; }` - 원본 파일명
- ❌ `public string StoredFilename { get; set; }` - 저장된 파일명
- ❌ `public string FilePath { get; set; }` - 파일 저장 경로
- ❌ `public long FileSize { get; set; }` - 파일 크기 (바이트)
- ❌ `public DateTime UploadDate { get; set; }` - 업로드 타임스탬프
- ❌ `public string AccountColumnName { get; set; }` - 계정 컬럼명
- ❌ `public string AmountColumnName { get; set; }` - 금액 컬럼명
- ❌ `public int TotalRows { get; set; }` - 전체 행 수
- ❌ `public int ProcessedRows { get; set; }` - 처리된 행 수
- ❌ `public string ProcessingStatus { get; set; }` - 처리 상태
- ❌ (기타 파일 메타데이터 프로퍼티들)

#### 3.7 FileSessionDocument.cs
**클래스**
- ❌ `public class FileSessionDocument` - 파일 처리 세션 추적 문서

**프로퍼티 (10+ 개)**
- ❌ `public ObjectId Id { get; set; }` - 세션 고유 식별자
- ❌ `public string SessionName { get; set; }` - 사용자 정의 세션명
- ❌ `public string AccountColumnName { get; set; }` - 계정 컬럼명
- ❌ `public string AmountColumnName { get; set; }` - 금액 컬럼명
- ❌ `public DateTime CreatedDate { get; set; }` - 세션 생성 날짜
- ❌ `public DateTime? CompletedDate { get; set; }` - 세션 완료 날짜
- ❌ `public string Status { get; set; }` - 현재 세션 상태
- ❌ `public List<ObjectId> FileIds { get; set; }` - 연결된 파일 ID 목록
- ❌ `public int TotalFiles { get; set; }` - 전체 파일 수
- ❌ `public string ResultFilePath { get; set; }` - 결과 파일 경로
- ❌ `public string AccountName { get; set; }` - 계정명

#### 3.8 ClusteringResultDocument.cs
**클래스**
- ✅ `public class ClusteringResultDocument` - 클러스터링 작업 결과 문서 - **Yes XML**

**프로퍼티 (20+ 개)**
- ⚠️ `public string Id { get; set; }` - 문서 고유 식별자 - **Partial XML**
- ⚠️ `public int ClusterID { get; set; }` - 클러스터 식별자 - **Partial XML**
- ⚠️ `public List<string> RawDataIds { get; set; }` - 클러스터 내 원시 데이터 ID 목록 - **Partial XML**
- ⚠️ `public int Count { get; set; }` - 클러스터 내 항목 수 - **Partial XML**
- ⚠️ `public List<string> KeywordList { get; set; }` - 클러스터 키워드 목록 - **Partial XML**
- ⚠️ `public string ClusterName { get; set; }` - 사람이 읽을 수 있는 클러스터명 - **Partial XML**
- ✅ `[BsonIgnore] public decimal TotalAmount { get; }` - 총 금액 - **Yes XML**
- ✅ `[BsonIgnore] public string FormattedAmount { get; }` - 형식화된 금액 문자열 - **Yes XML**
- ✅ `[BsonIgnore] public double AverageAmount { get; }` - 항목당 평균 금액 - **Yes XML**
- ❌ (기타 계산된 프로퍼티들)

---

### **4. Repositories/** - Data Access Layer
**파일 경로**: `C:\workspace\25 lg cns\nosql\FinanceTool\Repositories\`
**우선순위**: ⭐⭐⭐⭐ (Very High)

#### 4.1 BaseRepository.cs ✅ **완료** (2025-09-03)
**클래스**
- ✅ `public class BaseRepository<T> where T : class` - 기본 리포지토리 패턴 구현 - **Yes XML**

**필드 (4개)**
- ✅ `protected IMongoCollection<T> _collection` - MongoDB 컶렉션 인스턴스 - **Yes XML**
- ✅ `protected readonly Data.MongoDBManager _dbManager` - 데이터베이스 관리자 참조 - **Yes XML**
- ✅ `protected readonly string _collectionName` - 컶렉션명 - **Yes XML**
- ✅ `private bool _initialized` - 초기화 상태 플래그 - **Yes XML**

**메서드 (15+ 개)**
- ✅ `public BaseRepository(string collectionName)` - 리포지토리 생성자 - **Yes XML**
- ✅ `public virtual async Task<string> CreateAsync(T document)` - 단일 문서 생성 - **Yes XML**
- ✅ `public async Task InitializeAsync()` - 리포지토리 초기화 - **Yes XML**
- ✅ `public virtual async Task CreateManyAsync(IEnumerable<T> documents)` - 다중 문서 생성 - **Yes XML**
- ✅ `public virtual async Task<List<T>> GetAllAsync(...)` - 모든 문서 조회 - **Yes XML**
- ✅ `public virtual async Task<T> GetByIdAsync(string id)` - ID로 문서 조회 - **Yes XML**
- ✅ `public virtual async Task<bool> UpdateAsync(string id, T document)` - 문서 업데이트 - **Yes XML**
- ✅ `public virtual async Task<bool> DeleteAsync(string id)` - ID로 문서 삭제 - **Yes XML**
- ✅ `public virtual async Task<long> CountAsync()` - 전체 문서 수 계산 - **Yes XML**
- ✅ `public virtual async Task<List<T>> FindAsync(FilterDefinition<T> filter)` - 필터로 검색 - **Yes XML**
- ✅ `public virtual async Task DeleteManyAsync(FilterDefinition<T> filter)` - 다중 문서 삭제 - **Yes XML**
- ✅ `protected virtual FilterDefinition<T> BuildFilter(...)` - 필터 빌더 헬퍼 - **Yes XML**
- ✅ `protected virtual SortDefinition<T> BuildSort(...)` - 정렬 빌더 헬퍼 - **Yes XML**

#### 4.2 RawDataRepository.cs ✅ **완료** (2025-09-03)
**클래스**
- ✅ `public class RawDataRepository : BaseRepository<RawDataDocument>` - 원시 데이터 리포지토리 - **Yes XML**

**메서드 (10+ 개)**
- ✅ `public RawDataRepository()` - 리포지토리 생성자 - **Yes XML**
- ✅ `public async Task<List<RawDataDocument>> GetVisibleDataAsync(...)` - 비숨김 데이터 조회 - **Yes XML**
- ✅ `public async Task<List<RawDataDocument>> GetPagedDataAsync(...)` - 페이지네이션 데이터 조회 - **Yes XML**
- ✅ `public async Task HideDataAsync(string id, string reason)` - 사유와 함께 데이터 숨김 - **Yes XML**
- ✅ `public async Task ShowDataAsync(string id)` - 데이터 숨김 해제 - **Yes XML**
- ✅ `public async Task<long> GetTotalCountAsync()` - 전체 문서 수 계산 - **Yes XML**
- ✅ `public async Task<List<string>> GetDistinctValuesAsync(string columnName)` - 컬럼의 고유값 조회 - **Yes XML**
- ✅ `public async Task<bool> ColumnExistsAsync(string columnName)` - 컬럼 존재 여부 확인 - **Yes XML**
- ✅ `public new async Task DeleteManyAsync(FilterDefinition<RawDataDocument> filter)` - 다중 삭제 오버라이드 - **Yes XML**
- ✅ `public async Task<List<RawDataDocument>> GetPagedAsync(...)` - 고급 페이지네이션 조회 - **Yes XML**

#### 4.3 ProcessDataRepository.cs
**클래스**
- ✅ `public class ProcessDataRepository : BaseRepository<ProcessDataDocument>` - 처리 데이터 리포지토리 - **Yes XML**

**메서드 (5+ 개)**
- ❌ `public ProcessDataRepository()` - 리포지토리 생성자
- ❌ `public async Task<List<ProcessDataDocument>> GetByRawDataIdsAsync(List<string> rawDataIds)` - 원시 데이터 ID로 조회
- ❌ `public async Task<List<ProcessDataDocument>> GetByClusterIdAsync(int clusterId)` - 클러스터 ID로 조회
- ❌ `public async Task UpdateClusterAssignmentAsync(string id, int clusterId, string clusterName)` - 클러스터 할당 업데이트

#### 4.4 ClusteringRepository.cs ✅ **완료** (2025-09-03)
**클래스**
- ✅ `public class ClusteringRepository : BaseRepository<ClusteringResultDocument>` - 클러스터링 데이터 리포지토리 - **Yes XML**

**중첩 클래스**
- ✅ `public class ClusterSummary` - 클러스터 요약 데이터 - **Yes XML**
- ✅ `public class DetailClusterSummary` - 세부 클러스터 요약 데이터 - **Yes XML**

**메서드 (15+ 개)**
- ✅ `public ClusteringRepository()` - 리포지토리 생성자 - **Yes XML**
- ✅ `public async Task<List<ClusteringResultDocument>> GetByClusterIdAsync(int clusterId)` - 클러스터 ID로 조회 - **Yes XML**
- ✅ `public async Task<List<ClusterSummary>> GetClusterSummaryAsync()` - 클러스터 통계 조회 - **Yes XML**
- ✅ `public async Task<bool> MergeClustersAsync(int sourceClusterId, int targetClusterId)` - 클러스터 병합 - **Yes XML**
- ✅ `public async Task<bool> SplitClusterAsync(int clusterId, List<string> itemsToSplit)` - 클러스터 분할 - **Yes XML**
- ✅ `public async Task<int> GetNextClusterIdAsync()` - 다음 클러스터 ID 생성 - **Yes XML**
- ✅ `public async Task UpdateClusterNameAsync(int clusterId, string newName)` - 클러스터링 업데이트 - **Yes XML**
- ✅ `public async Task<List<DetailClusterSummary>> GetDetailClusterSummaryAsync()` - 세부 클러스터 요약 조회 - **Yes XML**
- ✅ (기타 25개+ 클러스터링 관리 메서드들) - **Yes XML**

#### 4.5 ProcessViewRepository.cs
**클래스**
- ✅ `public class ProcessViewRepository : BaseRepository<ProcessViewDocument>` - 프로세스 뷰 데이터 리포지토리 - **Yes XML**

**메서드 (4개)**
- ❌ `public ProcessViewRepository()` - 리포지토리 생성자
- ✅ `public async Task InsertManyAsync(List<ProcessViewDocument> documents, InsertManyOptions options)` - 옵션을 포함한 대량 삽입 - **Yes XML**
- ❌ `public async Task<long> CountDocumentsAsync(FilterDefinition<ProcessViewDocument> filter = null)` - 필터로 문서 수 계산
- ✅ `public async Task<bool> InsertOneAsync(ProcessViewDocument document)` - 단일 문서 삽입 - **Yes XML**

#### 4.6 ColumnMappingRepository.cs
**클래스**
- ✅ `public class ColumnMappingRepository : BaseRepository<ColumnMappingDocument>` - 컬럼 매핑 리포지토리 - **Yes XML**

**메서드 (5+ 개)**
- ❌ `public ColumnMappingRepository()` - 리포지토리 생성자
- ❌ `public async Task<List<ColumnMappingDocument>> GetByOriginalNamesAsync(List<string> originalNames)` - 원본명으로 조회
- ❌ `public async Task<ColumnMappingDocument> GetByOriginalNameAsync(string originalName)` - 단일 원본명으로 조회
- ❌ `public async Task UpdateDisplayNameAsync(string originalName, string displayName)` - 표시명 업데이트
- ❌ `public async Task UpdateSequenceAsync(Dictionary<string, int> sequenceMap)` - 표시 순서 업데이트

#### 4.7 FileSessionRepository.cs
**클래스**
- ✅ `public class FileSessionRepository : BaseRepository<FileSessionDocument>` - 파일 세션 리포지토리 - **Yes XML**

**메서드 (5+ 개)**
- ❌ `public FileSessionRepository()` - 리포지토리 생성자
- ❌ `public async Task<List<FileSessionDocument>> GetActiveSessionsAsync()` - 활성 세션 조회
- ❌ `public async Task<FileSessionDocument> GetBySessionNameAsync(string sessionName)` - 세션명으로 조회
- ❌ `public async Task UpdateSessionStatusAsync(ObjectId sessionId, string status)` - 세션 상태 업데이트
- ⚠️ `public new async Task<FileSessionDocument> GetByIdAsync(ObjectId id)` - GetByIdAsync 오버라이드 - **경고: 기본 메서드 숨김**

#### 4.8 UploadedFileRepository.cs
**클래스**
- ✅ `public class UploadedFileRepository : BaseRepository<UploadedFileDocument>` - 업로드 파일 리포지토리 - **Yes XML**

**메서드 (5+ 개)**
- ❌ `public UploadedFileRepository()` - 리포지토리 생성자
- ❌ `public async Task<List<UploadedFileDocument>> GetBySessionIdAsync(ObjectId sessionId)` - 세션별 파일 조회
- ❌ `public async Task<UploadedFileDocument> GetByFilenameAsync(string filename)` - 파일명으로 조회
- ❌ `public async Task UpdateProcessingStatusAsync(ObjectId fileId, string status)` - 처리 상태 업데이트
- ⚠️ `public new async Task<UploadedFileDocument> GetByIdAsync(ObjectId id)` - GetByIdAsync 오버라이드 - **경고: 기본 메서드 숨김**

---

### **5. Data/** - Data Management Layer
**파일 경로**: `C:\workspace\25 lg cns\nosql\FinanceTool\Data\`
**우선순위**: ⭐⭐⭐⭐⭐ (Critical)

#### 5.1 DataHandler.cs
**클래스**
- ✅ `public class DataHandler` - 전역 데이터 처리 및 관리 - **Yes XML (Class level)**

**중첩 클래스**
- ✅ `public class ProgressDialog : Form` - 진행률 표시 대화상자 - **Yes XML**

**정적 필드 (20+ 개)**
- ✅ `public static DataTable processTable` - 메인 처리된 데이터 테이블 - **Yes XML**
- ✅ `public static DataTable excelData` - 원시 Excel 데이터 테이블 - **Yes XML**
- ✅ `public static DataTable preprocessedData` - 전처리된 데이터 테이블 - **Yes XML**
- ✅ `public static DataTable lowLevelData` - 계정 계층 데이터 테이블 - **Yes XML**
- ✅ `public static DataTable moneyDataTable` - 금액 전용 데이터 테이블 - **Yes XML**
- ✅ `public static DataTable recomandKeywordTable` - 추천 키워드 테이블 - **Yes XML**
- ✅ `public static DataTable firstClusteringData` - 1차 클러스터링 결과 - **Yes XML**
- ✅ `public static DataTable secondClusteringData` - 2차 클러스터링 결과 - **Yes XML**
- ✅ `public static DataTable finalClusteringData` - 최종 클러스터링 결과 - **Yes XML**
- ✅ `public static DataTable subClusteringData` - 서브 클러스터링 데이터 - **Yes XML**
- ✅ `public static ObjectId currentSessionId` - 현재 세션 ID - **Yes XML**
- ✅ `public static string currentSessionName` - 현재 세션명 - **Yes XML**
- ✅ (기타 리포지토리 인스턴스 및 설정 필드들) - **Yes XML**

**정적 메서드 (15+ 개)**
- ❌ `public static List<string> GetColumnValuesAsList(DataTable table, int columnIndex)` - 컬럼 값들을 리스트로 추출
- ✅ `public static List<string> FindMachKeyword(List<string> listA, string search_keyword)` - 매칭되는 키워드 찾기 - **Yes XML**
- ✅ `public static bool CompareByTwoChars(string baseWord, string targetWord)` - 2글자씩 자르어 단어 비교 - **Yes XML**
- ✅ `public static async Task<DataTable> CreateSetGroupDataTableAsync(...)` - 그룹화된 데이터 테이블 생성 - **Yes XML**
- ✅ `public static void SetupDataGridView(DataGridView dgv, DataTable dt)` - 클러스터링용 DataGridView 설정 - **Yes XML**
- ✅ `public static void RegisterDataGridView(DataGridView dgv)` - 이벤트용 DataGridView 등록 - **Yes XML**
- ✅ `public static void money_SortCompare(object sender, DataGridViewSortCompareEventArgs e)` - 사용자 지정 금액 정렬 - **Yes XML**
- ✅ `private static IEnumerable<List<string>> BatchIdsForQuery(HashSet<string> ids, int batchSize)` - 배치 ID 처리 헬퍼 - **Yes XML**
- ⚠️ `private static double ExtractNumber(string text)` - 텍스트에서 숫자 값 추출 - **Partial XML**
- ❌ `private static List<(int start, int end)> GetBatches(int totalItems, int batchSize)` - 처리 배치 계산
- ❌ `private static bool IsMetadataColumn(string columnName)` - 메타데이터 컬럼 여부 확인
- ❌ `private static int GetOptimalBatchSize(int itemCount)` - 최적 배치 크기 계산
- ✅ `public static void SetCurrentSessionId(ObjectId sessionId)` - 현재 세션 ID 설정 - **Yes XML**
- ❌ `public static ObjectId GetCurrentSessionId()` - 현재 세션 ID 조회

**ProgressDialog 중첩 클래스 메서드**
- ✅ `public ProgressDialog()` - Progress dialog constructor - **Yes XML**
- ✅ `private void InitializeComponents()` - Initialize UI components - **Yes XML**
- ✅ `public async Task UpdateProgress(int percentage, string status = null)` - Update progress - **Yes XML**

#### 5.2 MongoDBManager.cs
**클래스**
- ✅ `public class MongoDBManager : IDisposable` - MongoDB 데이터베이스 관리 싱글톤 - **Yes XML**

**정적 프로퍼티 (2개)**
- ❌ `public static bool ResetDatabaseOnStartup` - 데이터베이스 리셋 플래그
- ❌ `public static MongoDBManager Instance` - 싱글톤 인스턴스

**필드 (6+ 개)**
- ❌ `private static MongoDBManager _instance` - 싱글톤 인스턴스
- ❌ `private static readonly object _lock` - 스레드 안전성 잠금
- ❌ `private MongoClient _client` - MongoDB 클라이언트
- ❌ `private IMongoDatabase _database` - MongoDB 데이터베이스
- ❌ `private bool _isConnected` - 연결 상태
- ❌ `private bool _disposed` - 해제 상태

**메서드 (15+ 개)**
- ❌ `private MongoDBManager()` - 프라이빗 싱글톤 생성자
- ❌ `public async Task<bool> EnsureInitializedAsync()` - 초기화 보장
- ❌ `private async Task InitializeDatabaseAsync()` - 데이터베이스 연결 초기화
- ❌ `public async Task ResetDatabaseAsync(...)` - 선택적 백업과 함께 데이터베이스 리셋
- ❌ `public async Task<bool> ConnectAsync()` - MongoDB 연결
- ❌ `public IMongoDatabase GetDatabase()` - 데이터베이스 인스턴스 가져오기
- ❌ `public IMongoCollection<T> GetCollection<T>(string collectionName)` - 타입화된 컬렉션 가져오기
- ❌ `public async Task<IMongoCollection<T>> GetCollectionAsync<T>(string collectionName)` - 비동기 컬렉션 가져오기
- ❌ `public async Task<bool> TestConnectionAsync()` - 데이터베이스 연결 테스트
- ❌ `public async Task<List<string>> GetCollectionNamesAsync()` - 모든 컬렉션 이름 가져오기
- ❌ `public void Disconnect()` - 데이터베이스 연결 해제
- ❌ `public void Dispose()` - IDisposable 구현
- ❌ `public async Task<bool> CollectionExistsAsync(string collectionName)` - 컬렉션 존재 여부 확인
- ❌ `public async Task DropCollectionAsync(string collectionName)` - 컬렉션 삭제
- ❌ `public async Task CreateCollectionAsync(string collectionName)` - 컬렉션 생성

#### 5.3 MongoDataConverter.cs
**클래스**
- ❌ `public class MongoDataConverter` - Excel과 MongoDB 형식 간 데이터 변환

**정적 필드 (3개)**
- ❌ `private static ProcessProgressForm progressForm` - 진행률 대화상자
- ❌ `private static ProcessProgressForm.UpdateProgressDelegate updateProgress` - 진행률 업데이터
- ❌ `private static ProgressDialog progressDialog` - 대체 진행률 대화상자

**메서드 (8+ 개)**
- ❌ `public static async Task<bool> ConvertExcelToMongoAsync(...)` - Excel을 MongoDB로 변환
- ❌ `public static async Task<DataTable> GetPagedRawDataAsync(...)` - 페이지네이션된 원시 데이터 가져오기
- ❌ `public static async Task<DataTable> PrepareProcessDataAsync(...)` - 처리 데이터 준비
- ❌ `public static async Task<bool> SaveProcessDataToMongoAsync(...)` - 처리된 데이터 저장
- ❌ `public static async Task<DataTable> GetProcessDataAsync(...)` - 처리된 데이터 가져오기
- ❌ `public static void SetProgressDialog(ProcessProgressForm form)` - 진행률 대화상자 설정
- ❌ `private static async Task<bool> SaveDataToMongoDB(...)` - 내부 저장 헬퍼
- ❌ `private static BsonDocument ConvertRowToBsonDocument(...)` - DataRow를 BsonDocument로 변환

#### 5.4 DataHandler_classification.cs
**클래스**
- ❌ `public static class DataHandler_classification` - 분류 전용 데이터 처리

**메서드 (5+ 개)**
- ❌ `public static async Task<string> GetColumnTypeAsync(string columnName)` - 컬럼 데이터 타입 결정
- ❌ `public static async Task<List<string>> GetNumericColumnsAsync()` - 숫자형 컬럼 가져오기
- ❌ `public static async Task<List<string>> GetTextColumnsAsync()` - 텍스트 컬럼 가져오기
- ❌ `public static async Task<Dictionary<string, object>> GetColumnStatisticsAsync(string columnName)` - 컬럼 통계 정보 가져오기
- ❌ `private static bool IsNumericValue(object value)` - 값이 숫자인지 확인

#### 5.5 DataHandler_fileLoad.cs
**클래스**
- ❌ `public static class DataHandler_fileLoad` - 파일 로딩 전용 데이터 처리

**메서드 (6+ 개)**
- ❌ `public static async Task<bool> ValidateExcelFileAsync(string filePath)` - Excel 파일 유효성 검사
- ❌ `public static async Task<DataTable> LoadExcelToDataTableAsync(string filePath)` - Excel을 DataTable로 로드
- ❌ `public static async Task<List<string>> GetExcelColumnNamesAsync(string filePath)` - Excel 컬럼명 가져오기
- ❌ `public static async Task<int> GetExcelRowCountAsync(string filePath)` - Excel 행 수 가져오기
- ❌ `public static bool IsValidColumnName(string columnName)` - 컬럼명 유효성 검사
- ❌ `private static void CleanupColumnNames(DataTable table)` - 컬럼명 정리

#### 5.6 DataHandler_preprocessing.cs
**클래스**
- ❌ `public static class DataHandler_preprocessing` - 전처리 전용 데이터 처리

**메서드 (8+ 개)**
- ❌ `public static async Task<DataTable> PreprocessDataAsync(DataTable sourceData)` - 메인 전처리 로직
- ❌ `public static async Task<DataTable> CleanDataAsync(DataTable data)` - 데이터 정제
- ❌ `public static async Task<DataTable> StandardizeFormatsAsync(DataTable data)` - 형식 표준화
- ❌ `public static async Task<DataTable> HandleMissingValuesAsync(DataTable data)` - 누락값 처리
- ❌ `public static string ReplaceSeparators(string input, string target, string mode)` - 구분자 교체
- ❌ `private static bool IsNumericString(string value)` - 숫자 문자열 확인
- ❌ `private static string StandardizeNumberFormat(string value)` - 숫자 형식 표준화
- ❌ `private static DataTable RemoveDuplicateRows(DataTable table)` - 중복 행 제거

---

### **6. Utilities/** - Utility Classes
**파일 경로**: `C:\workspace\25 lg cns\nosql\FinanceTool\Utilities\`
**우선순위**: ⭐⭐⭐ (High)

#### 6.1 KeywordExtractor.cs
**클래스**
- ❌ `public class KeywordExtractor` - Python 기반 키워드 추출 서비스

**중첩 클래스 (5개)**
- ❌ `public class PythonResponse` - Python 스크립트 응답 래퍼
- ❌ `public class KeywordResult` - 개별 키워드 결과
- ❌ `public class ProcessedTextData` - 처리된 텍스트 데이터 컨테이너
- ❌ `public class TextData` - 입력 텍스트 데이터 구조
- ❌ `public class PythonInput` - Python 스크립트 입력 구조

**필드 (4개)**
- ❌ `private readonly string _pythonPath` - Python 실행 파일 경로
- ❌ `private readonly string _scriptPath` - Python 스크립트 경로
- ❌ `private int call_type` - 추출 타입 플래그
- ❌ `private SemaphoreSlim _batchSemaphore` - 배치 처리 세마포어

**메서드 (15+ 개)**
- ❌ `public KeywordExtractor(int type)` - 추출 타입이 포함된 생성자
- ❌ `public async Task<DataTable> ExtractKeywordsFromDataTable(...)` - 메인 추출 메서드
- ❌ `private async Task<List<KeywordResult>> ProcessBatchAsync(...)` - 배치 처리
- ❌ `private async Task<PythonResponse> CallPythonScriptAsync(...)` - Python 스크립트 호출기
- ❌ `private List<TextData> PrepareTextData(...)` - 입력 데이터 준비
- ❌ `private DataTable ConvertResultsToDataTable(...)` - 결과를 DataTable로 변환
- ❌ `private void ValidateInputs(...)` - 입력 유효성 검사
- ❌ `private string SerializePythonInput(...)` - 입력을 JSON으로 직렬화
- ❌ `private PythonResponse DeserializePythonOutput(...)` - Python 출력 역직렬화
- ❌ `private bool IsPythonAvailable()` - Python 사용 가능 여부 확인
- ❌ `private async Task<bool> ValidateScriptAsync()` - Python 스크립트 유효성 검사
- ❌ `private void LogError(string message, Exception ex = null)` - 오류 로깅
- ❌ `public void Dispose()` - 리소스 해제

#### 6.2 ProcessProgressForm.cs
**클래스**
- ❌ `public class ProcessProgressForm : Form` - 장시간 실행 작업용 진행률 대화상자

**델리게이트**
- ❌ `public delegate Task UpdateProgressDelegate(int value, string status=null)` - 진행률 업데이트 델리게이트

**필드/프로퍼티 (4개)**
- ❌ `private ProgressBar progressBar` - 진행률 표시막대 컨트롤
- ❌ `private Label statusLabel` - 상태 텍스트 레이블
- ❌ `private Form parentForm` - 부모 폼 참조
- ❌ `public UpdateProgressDelegate UpdateProgressHandler` - 진행률 업데이트 핸들러

**메서드 (4개)**
- ❌ `public ProcessProgressForm(Form parent = null)` - 선택적 부모가 포함된 생성자
- ❌ `private void InitializeComponent()` - UI 컴포넌트 초기화
- ❌ `public static implicit operator UpdateProgressDelegate(ProcessProgressForm form)` - 암시적 변환 연산자
- ❌ `public async Task UpdateProgressValue(int percentage, string status = null)` - 진행률 표시 업데이트

#### 6.3 TrialManager.cs
**클래스**
- ❌ `internal class TrialManager` - 라이센스 및 체험판 관리

**정적 필드 (3개)**
- ❌ `private static readonly DateTime ExpirationDate` - 체험판 만료 날짜
- ❌ `private static readonly HashSet<string> AllowedMacAddresses` - 허용된 MAC 주소 목록
- ❌ `private const string TimeApiUrl` - 시간 API 엔드포인트 URL

**메서드 (5+ 개)**
- ❌ `public async Task checkMacaddress()` - MAC 주소 유효성 확인
- ❌ `public async Task CheckTrial()` - 체험판 상태 확인
- ❌ `private async Task<bool> IsValidMacAddressAsync()` - MAC 주소 유효성 검사
- ❌ `private async Task<DateTime> GetNetworkTimeAsync()` - 네트워크 시간 가져오기
- ❌ `private string GetMacAddress()` - 시스템 MAC 주소 가져오기

#### 6.4 userControlHandler.cs
**클래스**
- ❌ `public class userControlHandler` - 정적 사용자 컨트롤 인스턴스 관리자

**정적 필드 (10+ 개)**
- ❌ `public static uc_FileLoad uc_FileLoad_Ins` - 파일 로드 컨트롤 인스턴스
- ❌ `public static uc_MultiFileUpload uc_MultiFileUpload_Ins` - 다중 파일 업로드 인스턴스
- ❌ `public static uc_Clustering uc_Clustering_Ins` - 클러스터링 컨트롤 인스턴스
- ❌ `public static uc_DetailClustering uc_DetailClustering_Ins` - 세부 클러스터링 인스턴스
- ❌ `public static uc_DataTransform uc_DataTransform_Ins` - 데이터 변환 인스턴스
- ❌ `public static uc_Classification uc_Classification_Ins` - 분류 인스턴스
- ❌ `public static uc_preprocessing uc_preprocessing_Ins` - 전처리 인스턴스
- ❌ (기타 사용자 컨트롤 인스턴스들)

#### 6.5 RecomandKeywordManager.cs
**클래스**
- ❌ `public class RecomandKeywordManager` - 추천 키워드 관리

**메서드 (5+ 개)**
- ❌ `public static async Task<List<string>> GetRecommendedKeywordsAsync(...)` - 추천 키워드 가져오기
- ❌ `public static async Task SaveKeywordRecommendationAsync(...)` - 키워드 추천 저장
- ❌ `public static async Task<DataTable> AnalyzeKeywordPatternsAsync(...)` - 키워드 패턴 분석
- ❌ `private static double CalculateKeywordRelevance(...)` - 관련성 점수 계산
- ❌ `private static List<string> FilterRelevantKeywords(...)` - 관련 키워드 필터링

#### 6.6 SeparatorManager.cs
**클래스**
- ❌ `public class SeparatorManager` - 텍스트 구분자 관리 유틸리티

**정적 메서드 (5개)**
- ❌ `public static string ProcessSeparators(string input, string mode)` - 텍스트 구분자 처리
- ❌ `public static List<string> SplitBySeparators(string text, char[] separators)` - 다중 구분자로 분할
- ❌ `public static string NormalizeSeparators(string text)` - 구분자 문자 정규화
- ❌ `private static bool IsKoreanSeparator(char character)` - 한국어 구분자 확인
- ❌ `private static string ReplaceSeparatorPatterns(string text)` - 구분자 패턴 교체

#### 6.7 SystemPerformanceOptimizer.cs
**클래스**
- ❌ `public class SystemPerformanceOptimizer` - 시스템 성능 최적화 유틸리티

**메서드 (6+ 개)**
- ❌ `public static void OptimizeForLargeDataProcessing()` - 대용량 데이터 작업 최적화
- ❌ `public static void ConfigureGarbageCollection()` - GC 설정 구성
- ❌ `public static int GetOptimalThreadCount()` - 최적 스레드 수 가져오기
- ❌ `public static void SetMemoryPressure(long bytesAllocated)` - 메모리 압박 설정
- ❌ `private static void OptimizeNetworkSettings()` - 네트워크 설정 최적화
- ❌ `private static void ConfigureBufferSizes()` - I/O 버퍼 크기 구성

#### 6.8 SessionDataProcessor.cs
**클래스**
- ❌ `public class SessionDataProcessor` - 세션 데이터 처리 유틸리티

**메서드 (8+ 개)**
- ❌ `public static async Task<bool> SaveSessionDataAsync(...)` - 세션 데이터 저장
- ❌ `public static async Task<DataTable> LoadSessionDataAsync(...)` - 세션 데이터 로드
- ❌ `public static async Task<List<string>> GetAvailableSessionsAsync()` - 세션 목록 가져오기
- ❌ `public static async Task<bool> DeleteSessionAsync(string sessionName)` - 세션 삭제
- ❌ `public static async Task<SessionInfo> GetSessionInfoAsync(string sessionName)` - 세션 정보 가져오기
- ❌ `private static string GenerateSessionId()` - 고유 세션 ID 생성
- ❌ `private static bool ValidateSessionData(...)` - 세션 데이터 유효성 검사
- ❌ `private static async Task<bool> BackupSessionAsync(...)` - 세션 데이터 백업

---

### **7. Utilities/ClusterManager/** - Cluster Management
**파일 경로**: `C:\workspace\25 lg cns\nosql\FinanceTool\Utilities\ClusterManager\`
**우선순위**: ⭐⭐⭐⭐ (Very High)

#### 7.1 ClusterDataManager.cs
**클래스**
- ❌ `public class ClusterDataManager` - 클러스터 데이터 관리

**필드 (5+ 개)**
- ❌ `private Dictionary<int, List<string>> _clusterData` - 클러스터 데이터 저장소
- ❌ `private Dictionary<int, string> _clusterNames` - 클러스터 이름 매핑
- ❌ `private Dictionary<string, int> _itemClusterMap` - 아이템에서 클러스터로 매핑
- ❌ `private object _lockObject` - 스레드 안전성 잠금
- ❌ `private bool _isInitialized` - 초기화 상태

**메서드 (12+ 개)**
- ❌ `public ClusterDataManager()` - 생성자
- ❌ `public async Task InitializeAsync()` - 클러스터 데이터 초기화
- ❌ `public async Task<int> CreateClusterAsync(List<string> items, string clusterName)` - 새 클러스터 생성
- ❌ `public async Task<bool> AddItemToClusterAsync(int clusterId, string item)` - 클러스터에 아이템 추가
- ❌ `public async Task<bool> RemoveItemFromClusterAsync(int clusterId, string item)` - 클러스터에서 아이템 제거
- ❌ `public async Task<bool> MergeClustersAsync(int sourceClusterId, int targetClusterId)` - 클러스터 병합
- ❌ `public async Task<List<string>> SplitClusterAsync(int clusterId, List<string> itemsToSplit)` - 클러스터 분할
- ❌ `public async Task<ClusterInfo> GetClusterInfoAsync(int clusterId)` - 클러스터 정보 가져오기
- ❌ `public async Task<List<ClusterSummary>> GetAllClustersAsync()` - 모든 클러스터 요약 가져오기
- ❌ `public async Task UpdateClusterNameAsync(int clusterId, string newName)` - 클러스터 이름 업데이트
- ❌ `public async Task<bool> DeleteClusterAsync(int clusterId)` - 클러스터 삭제
- ❌ `private int GenerateNextClusterId()` - 고유 클러스터 ID 생성

#### 7.2 ClusterDisplayManager.cs
**클래스**
- ❌ `public class ClusterDisplayManager` - 클러스터 표시 관리

**필드 (4+ 개)**
- ❌ `private DataGridView _gridView` - DataGridView 참조
- ❌ `private Dictionary<int, Color> _clusterColors` - 클러스터 색상 매핑
- ❌ `private ClusterDataManager _dataManager` - 데이터 관리자 참조
- ❌ `private FilterSettings _currentFilter` - 현재 필터 설정

**메서드 (10+ 개)**
- ❌ `public ClusterDisplayManager(DataGridView gridView)` - 생성자
- ❌ `public async Task DisplayClustersAsync(List<ClusterInfo> clusters)` - 클러스터 데이터 표시
- ❌ `public void ApplyClusterColoring()` - 클러스터에 색상 코딩 적용
- ❌ `public void SetFilterCriteria(FilterSettings filter)` - 표시 필터 설정
- ❌ `public async Task RefreshDisplayAsync()` - 클러스터 표시 새로고침
- ❌ `public void HighlightCluster(int clusterId)` - 특정 클러스터 강조 표시
- ❌ `public void ClearHighlight()` - 클러스터 강조 제거
- ❌ `public async Task ExportClusterDataAsync(string filePath, ExportFormat format)` - 클러스터 데이터 내보내기
- ❌ `private Color GenerateClusterColor(int clusterId)` - 클러스터 색상 생성
- ❌ `private void ConfigureGridColumns()` - DataGridView 컬럼 구성

#### 7.3 ClusteringManager.cs
**클래스**
- ❌ `public class ClusteringManager` - 핵심 클러스터링 알고리즘 구현

**필드 (6+ 개)**
- ❌ `private ClusterDataManager _dataManager` - 데이터 관리자
- ❌ `private KeywordExtractor _keywordExtractor` - 키워드 추출 서비스
- ❌ `private Dictionary<string, double> _itemSimilarities` - 유사성 캐시
- ❌ `private ClusteringParameters _parameters` - 알고리즘 매개변수
- ❌ `private SemaphoreSlim _processingLock` - 처리 동기화
- ❌ `private CancellationTokenSource _cancellationSource` - 취소 지원

**메서드 (15+ 개)**
- ❌ `public ClusteringManager()` - 생성자
- ❌ `public async Task<ClusteringResult> PerformClusteringAsync(...)` - 메인 클러스터링 메서드
- ❌ `public void SetClusteringParameters(ClusteringParameters parameters)` - 알고리즘 매개변수 설정
- ❌ `public async Task<List<ClusterSuggestion>> GetClusterSuggestionsAsync(...)` - 클러스터링 제안 가져오기
- ❌ `public async Task<double> CalculateSimilarityAsync(string item1, string item2)` - 아이템 유사성 계산
- ❌ `public void CancelClustering()` - 진행 중인 클러스터링 취소
- ❌ `public ClusteringProgress GetProgress()` - 클러스터링 진행률 가져오기
- ❌ `private async Task<List<Cluster>> ApplyKMeansAsync(...)` - K-means 알고리즘 적용
- ❌ `private async Task<List<Cluster>> ApplyHierarchicalAsync(...)` - 계층적 클러스터링 적용
- ❌ `private async Task<double> ComputeDistanceAsync(...)` - 아이템 간 거리 계산
- ❌ `private List<string> ExtractFeatures(string item)` - 아이템 특징 추출
- ❌ `private void OptimizeClusterCount(List<Cluster> clusters)` - 클러스터 수 최적화
- ❌ `private async Task PostProcessClustersAsync(List<Cluster> clusters)` - 결과 후처리
- ❌ `public void Dispose()` - 리소스 정리

#### 7.4 ClusterSearchEngine.cs
**클래스**
- ❌ `public class ClusterSearchEngine` - 클러스터 검색 및 필터링

**필드 (4+ 개)**
- ❌ `private ClusterDataManager _dataManager` - 데이터 관리자 참조
- ❌ `private Dictionary<string, List<int>> _keywordIndex` - 키워드 검색 인덱스
- ❌ `private Dictionary<int, SearchMetadata> _clusterMetadata` - 클러스터 메타데이터
- ❌ `private SearchSettings _defaultSettings` - 기본 검색 설정

**메서드 (8+ 개)**
- ❌ `public ClusterSearchEngine(ClusterDataManager dataManager)` - 생성자
- ❌ `public async Task<List<ClusterSearchResult>> SearchAsync(string query, SearchSettings settings)` - 검색 수행
- ❌ `public async Task<List<int>> FindSimilarClustersAsync(int clusterId, double threshold)` - 유사한 클러스터 찾기
- ❌ `public async Task BuildSearchIndexAsync()` - 검색 인덱스 구축
- ❌ `public async Task<List<string>> GetSearchSuggestionsAsync(string partialQuery)` - 검색 제안 가져오기
- ❌ `private double CalculateSearchScore(int clusterId, string query)` - 검색 관련성 계산
- ❌ `private List<string> TokenizeQuery(string query)` - 검색 쿼리 토큰화
- ❌ `private void UpdateSearchIndex(int clusterId, List<string> keywords)` - 검색 인덱스 업데이트

---

### **8. uc/** - User Controls (Main UI Layer)
**파일 경로**: `C:\workspace\25 lg cns\nosql\FinanceTool\uc\`
**우선순위**: ⭐⭐⭐ (High)

#### 8.1 uc_FileLoad.cs
**클래스**
- ❌ `public partial class uc_FileLoad : UserControl` - File loading user control

**필드 (15+ 개)**
- ❌ `private List<string> process_col_list` - Process column list
- ❌ `private string selectedStandColumn` - Selected standard column
- ❌ `private int currentPage` - Current page number
- ❌ `private int totalPages` - Total page count
- ❌ `private int totalRows` - Total row count
- ❌ `private int pageSize` - Page size
- ❌ `private bool isMongoDBMode` - MongoDB operation mode flag
- ❌ `private List<string> _numericColumns` - Numeric column list
- ❌ `private List<string> _allColumns` - All column list
- ❌ `private Dictionary<string, object> _mappingData` - Mapping data cache
- ❌ (기타 UI 상태 관리 필드들)

**메서드 (50+ 개)**
- ❌ `public uc_FileLoad()` - User control constructor
- ❌ `private async void btn_load_data_Click(object sender, EventArgs e)` - Load data button handler
- ❌ `private async void btn_upload_mongo_Click(object sender, EventArgs e)` - Upload to MongoDB handler
- ❌ `private void btn_column_delete_Click(object sender, EventArgs e)` - Column delete handler
- ❌ `private void btn_row_delete_Click(object sender, EventArgs e)` - Row delete handler
- ❌ `private async void btn_prevPage_Click(object sender, EventArgs e)` - Previous page handler
- ❌ `private async void btn_nextPage_Click(object sender, EventArgs e)` - Next page handler
- ❌ `private async void cmb_pageSize_SelectedIndexChanged(object sender, EventArgs e)` - Page size change handler
- ❌ (다수의 이벤트 핸들러들)

#### 8.2 uc_MultiFileUpload.cs
**클래스**
- ❌ `public partial class uc_MultiFileUpload : UserControl` - 다중 파일 업로드 사용자 컨트롤

**필드 (12+ 개)**
- ❌ `private List<FileUploadInfo> _uploadFiles` - 업로드 파일 목록
- ❌ `private string _currentSessionName` - 현재 세션명
- ❌ `private bool _isProcessing` - 처리 상태 플래그
- ❌ `private Dictionary<string, string> _columnMappings` - 컬럼 매핑 딕셔너리
- ❌ `private ProgressDialog _progressDialog` - 진행률 표시
- ❌ (기타 업로드 관리 필드들)

**메서드 (30+ 개)**
- ❌ `public uc_MultiFileUpload()` - 생성자
- ❌ `private void btn_add_files_Click(object sender, EventArgs e)` - 파일 추가 버튼 핸들러
- ❌ `private void btn_remove_file_Click(object sender, EventArgs e)` - 파일 제거 핸들러
- ❌ `private async void btn_start_upload_Click(object sender, EventArgs e)` - 업로드 시작 핸들러
- ❌ `private void btn_create_session_Click(object sender, EventArgs e)` - 세션 생성 핸들러
- ❌ (기타 파일 업로드 관련 메서드들)

#### 8.3 uc_Clustering.cs
**클래스**
- ❌ `public partial class uc_Clustering : UserControl` - 클러스터링 사용자 컨트롤

**필드 (10+ 개)**
- ❌ `private ClusteringManager _clusteringManager` - 클러스터링 관리자
- ❌ `private ClusterDisplayManager _displayManager` - 표시 관리자
- ❌ `private Dictionary<int, string> _clusterNames` - 클러스터 이름 매핑
- ❌ `private bool _isClusteringInProgress` - 클러스터링 진행 플래그
- ❌ (기타 클러스터링 상태 필드들)

**메서드 (25+ 개)**
- ❌ `public uc_Clustering()` - 생성자
- ❌ `private async void btn_start_clustering_Click(object sender, EventArgs e)` - 클러스터링 시작 핸들러
- ❌ `private void btn_merge_clusters_Click(object sender, EventArgs e)` - 클러스터 병합 핸들러
- ❌ `private void btn_split_cluster_Click(object sender, EventArgs e)` - 클러스터 분할 핸들러
- ❌ (기타 클러스터링 작업 메서드들)

#### 8.4 uc_DetailClustering.cs
**클래스**
- ❌ `public partial class uc_DetailClustering : UserControl` - 세부 클러스터링 사용자 컨트롤

**필드 (8+ 개)**
- ❌ `private int _selectedClusterId` - 선택된 클러스터 ID
- ❌ `private List<DetailClusterItem> _clusterItems` - 세부 클러스터 아이템
- ❌ `private Dictionary<string, object> _filterCriteria` - 필터 조건
- ❌ (기타 세부 클러스터링 필드들)

**메서드 (20+ 개)**
- ❌ `public uc_DetailClustering()` - 생성자
- ❌ `private async void LoadClusterDetails(int clusterId)` - 클러스터 세부 로드
- ❌ `private void btn_apply_filter_Click(object sender, EventArgs e)` - 필터 적용 핸들러
- ❌ (기타 세부 분석 메서드들)

#### 8.5 uc_DataTransform.cs
**클래스**
- ❌ `public partial class uc_DataTransform : UserControl` - 데이터 변환 사용자 컨트롤

**메서드 (15+ 개)**
- ❌ `public uc_DataTransform()` - 생성자
- ❌ `private void btn_transform_Click(object sender, EventArgs e)` - 변환 버튼 핸들러
- ❌ (기타 데이터 변환 메서드들)

#### 8.6 uc_Classification.cs
**클래스**
- ❌ `public partial class uc_Classification : UserControl` - 분류 사용자 컨트롤

**메서드 (12+ 개)**
- ❌ `public uc_Classification()` - 생성자
- ❌ `private void btn_classify_Click(object sender, EventArgs e)` - 분류 핸들러
- ❌ (기타 분류 작업 메서드들)

#### 8.7 uc_preprocessing.cs
**클래스**
- ❌ `public partial class uc_preprocessing : UserControl` - 전처리 사용자 컨트롤

**메서드 (10+ 개)**
- ❌ `public uc_preprocessing()` - 생성자
- ❌ `private async void btn_preprocess_Click(object sender, EventArgs e)` - 전처리 핸들러
- ❌ (기타 전처리 메서드들)

---

### **9. uc/Process/** - Business Logic Layer
**파일 경로**: `C:\workspace\25 lg cns\nosql\FinanceTool\uc\Process\`
**우선순위**: ⭐⭐⭐⭐⭐ (Critical - Phase 1 완료)

#### 9.1 uc_ClusteringProcess.cs ✅ **Phase 1 완료**
**클래스**
- ✅ `public partial class uc_FileLoad` - Clustering business logic processor - **Yes XML**

**메서드 (16개) - 모두 완료**
- ✅ 모든 메서드 XML 문서 주석 완료

#### 9.2 uc_DetailClusteringProcess.cs ✅ **Phase 1 완료**  
**클래스**
- ✅ `public partial class uc_FileLoad` - Detail clustering processor - **Yes XML**

**메서드 (14개) - 모두 완료**
- ✅ 모든 메서드 XML 문서 주석 완료

#### 9.3 uc_FileLoadProcess.cs ✅ **Phase 1 완료 + 표준화 완료**
**클래스**
- ✅ `public partial class uc_FileLoad` - File loading processor - **Yes XML**

**메서드 (25개) - 모두 완료 및 표준화**
- ✅ 모든 메서드 XML 문서 주석 완료 및 표준화

#### 9.4 uc_MultiFileUploadSessionProcess.cs
**클래스**
- ❌ `public partial class uc_FileLoad` - 다중 파일 업로드 세션 프로세서

**메서드 (15+ 개)**
- ❌ 모든 메서드 XML 문서 주석 필요

#### 9.5 uc_MultiFileUploadExcelProcess.cs
**클래스**
- ❌ `public partial class uc_FileLoad` - 다중 파일 업로드용 Excel 처리

**메서드 (12+ 개)**
- ❌ 모든 메서드 XML 문서 주석 필요

#### 9.6 uc_MultiFileUploadFileProcess.cs
**클래스**
- ❌ `public partial class uc_FileLoad` - 다중 파일 업로드용 파일 처리

**메서드 (10+ 개)**
- ❌ 모든 메서드 XML 문서 주석 필요

#### 9.7 uc_ClassificationProcess.cs
**클래스**
- ❌ `public partial class uc_FileLoad` - 분류 비즈니스 로직

**메서드 (18+ 개)**
- ❌ 모든 메서드 XML 문서 주석 필요

#### 9.8 uc_DataTransformProcess.cs
**클래스**
- ❌ `public partial class uc_FileLoad` - 데이터 변환 로직

**메서드 (10+ 개)**
- ❌ 모든 메서드 XML 문서 주석 필요

#### 9.9 uc_ClusteringSearchEngine.cs
**클래스**
- ❌ `public partial class uc_FileLoad` - 클러스터링 검색 엔진

**메서드 (12+ 개)**
- ❌ 모든 메서드 XML 문서 주석 필요

#### 9.10 uc_DetailClusteringSearchEngine.cs
**클래스**
- ❌ `public partial class uc_FileLoad` - 세부 클러스터링 검색

**메서드 (8+ 개)**
- ❌ 모든 메서드 XML 문서 주석 필요

#### 9.11 uc_PreprocessingProcess.cs
**클래스**
- ❌ `public partial class uc_FileLoad` - 전처리 비즈니스 로직

**메서드 (6+ 개)**
- ❌ 모든 메서드 XML 문서 주석 필요

---

### **10. uc/DB/** - Database Layer
**파일 경로**: `C:\workspace\25 lg cns\nosql\FinanceTool\uc\DB\`
**우선순위**: ⭐⭐⭐⭐ (Very High)

#### 10.1 uc_ClusteringMongoDB.cs
**클래스**
- ❌ `public partial class uc_FileLoad` - 클러스터링 MongoDB 작업

**메서드 (15+ 개)**
- ❌ 모든 메서드 XML 문서 주석 필요

#### 10.2 uc_DataTransformMongoDB.cs
**클래스**  
- ❌ `public partial class uc_FileLoad` - 데이터 변환 MongoDB 작업

**메서드 (12+ 개)**
- ❌ 모든 메서드 XML 문서 주석 필요

#### 10.3 uc_FileLoadMongoDB.cs
**클래스**
- ❌ `public partial class uc_FileLoad` - 파일 로드 MongoDB 작업

**메서드 (20+ 개)**
- ❌ 모든 메서드 XML 문서 주석 필요

#### 10.4 uc_DetailClusteringMongoDB.cs
**클래스**
- ❌ `public partial class uc_FileLoad` - 세부 클러스터링 MongoDB 작업

**메서드 (10+ 개)**
- ❌ 모든 메서드 XML 문서 주석 필요

#### 10.5 uc_MultiFileUploadMongoDB.cs
**클래스**
- ❌ `public partial class uc_FileLoad` - 다중 파일 업로드 MongoDB 작업

**메서드 (18+ 개)**
- ❌ 모든 메서드 XML 문서 주석 필요

#### 10.6 uc_ClassificationMongoDB.cs
**클래스**
- ❌ `public partial class uc_FileLoad` - 분류 MongoDB 작업

**메서드 (8+ 개)**
- ❌ 모든 메서드 XML 문서 주석 필요

#### 10.7 uc_PreprocessingMongoDB.cs
**클래스**
- ❌ `public partial class uc_FileLoad` - 전처리 MongoDB 작업

**메서드 (6+ 개)**
- ❌ 모든 메서드 XML 문서 주석 필요

---

### **11. popup/** - Popup Dialogs
**파일 경로**: `C:\workspace\25 lg cns\nosql\FinanceTool\popup\`
**우선순위**: ⭐⭐ (Medium)

#### 11.1 uc_clusteringPopup.cs
**클래스**
- ❌ `public partial class uc_clusteringPopup : UserControl` - 클러스터링 팅업 대화상자

**필드 (4+ 개)**
- ❌ `private DataTable originDataTable` - 원본 데이터 테이블
- ❌ `private DataTable transformDataTable` - 변환 데이터 테이블
- ❌ (기타 팝업 상태 필드들)

**메서드 (8+ 개)**
- ❌ `public uc_clusteringPopup()` - 생성자
- ❌ (기타 팝업 관련 메서드들)

#### 11.2 uc_ClusterDetailPopup.cs
**클래스**
- ❌ `public partial class ClusterDetailPopup : Form` - 클러스터 세부 팅업

**델리게이트/이벤트**
- ❌ `public event Action<List<int>> UnmergeCompleted` - 병합 해제 완료 이벤트

**프로퍼티 (2개)**
- ❌ `public List<int> UnmergedClusterIds { get; set; }` - 병합 해제된 클러스터 ID

**메서드 (10+ 개)**
- ❌ `public ClusterDetailPopup()` - 생성자
- ❌ `private void UnmergeSelectedItems_Click(object sender, EventArgs e)` - 병합 해제 핸들러
- ❌ `private void SelectAll_Click(object sender, EventArgs e)` - 전체 선택 핸들러
- ❌ (기타 팝업 기능 메서드들)

#### 11.3 (기타 팝업 파일들)
**추가 팝업 컨트롤들**
- ❌ 각 팝업별 클래스, 메서드, 이벤트 핸들러들 XML 문서 주석 필요

---

## 📋 주석 작업 우선순위 및 계획

### **Phase 1: 핵심 비즈니스 로직** ✅ **완료** (2025-09-03)
- ✅ uc_ClusteringProcess.cs (1,242줄) - **16개 메서드 XML 주석 완료**
- ✅ uc_DetailClusteringProcess.cs (1,011줄) - **14개 메서드 XML 주석 완료**
- ✅ uc_FileLoadProcess.cs (1,040줄) - **25개 메서드 XML 주석 완료 + 표준화 완료**
- ✅ DataHandler.cs (963줄) - **15개 메서드 + 30개 필드 XML 주석 완료 + 표준화 완료**

### **Phase 2: 데이터 접근 계층** - **✅ 완료 (100%)**
#### 2.1 Repository 계층 (8개 파일, ~2,156줄) - **✅ 완료**
- ✅ BaseRepository.cs (312줄) - **15개 메서드, 4개 필드** ✅ **완료** (2025-09-03)
- ✅ RawDataRepository.cs (445줄) - **10개+ 메서드** ✅ **완료** (2025-09-03)
- ✅ ProcessDataRepository.cs (287줄) - **클래스 및 생성자** ✅ **완료** (2025-09-03)
- ✅ ClusteringRepository.cs (578줄) - **25개+ 메서드, 2개 중첩 클래스** ✅ **완료** (2025-09-03)
- ✅ ColumnMappingRepository.cs (198줄) - **3개 메서드** ✅ **완료** (2025-09-03)
- ✅ ProcessViewRepository.cs (156줄) - **4개 메서드** ✅ **완료** (2025-09-03)
- ✅ FileSessionRepository.cs (89줄) - **6개+ 메서드** ✅ **완룄** (2025-09-03)
- ✅ UploadedFileRepository.cs (91줄) - **6개+ 메서드, 1개 프로퍼티** ✅ **완룄** (2025-09-03)

#### 2.2 MongoDB 연동 계층 (7개 파일, ~5,185줄)
- ❌ uc_ClusteringMongoDB.cs (807줄)
- ❌ uc_DataTransformMongoDB.cs (987줄)
- ❌ uc_FileLoadMongoDB.cs (915줄)
- ❌ uc_DetailClusteringMongoDB.cs (621줄)
- ❌ uc_MultiFileUploadMongoDB.cs (640줄)
- ❌ uc_ClassificationMongoDB.cs (649줄)
- ❌ uc_PreprocessingMongoDB.cs (566줄)

#### 2.3 데이터 관리 계층 (5개 파일, ~1,500줄)
- ❌ MongoDBManager.cs (423줄) - **15개+ 메서드, 6개+ 필드**
- ❌ MongoDataConverter.cs (629줄) - **8개+ 메서드, 3개 필드**
- ❌ DataHandler_classification.cs (196줄) - **5개+ 메서드**
- ❌ DataHandler_fileLoad.cs (271줄) - **6개+ 메서드**
- ❌ DataHandler_preprocessing.cs (514줄) - **8개+ 메서드**

### **Phase 3: 모델 및 비즈니스 로직** 
#### 3.1 MongoDB 모델 계층 (7개 파일, ~456줄)
- ❌ RawDataDocument.cs (65줄) - **6개 프로퍼티**
- ❌ ProcessDataDocument.cs (58줄) - **7개 프로퍼티**
- ⚠️ ClusteringResultDocument.cs (89줄) - **20개+ 프로퍼티 (일부 완료)**
- ❌ ColumnMappingDocument.cs (45줄) - **6개 프로퍼티**
- ❌ KeywordDocument.cs (42줄) - **9개 프로퍼티**
- ❌ ProcessViewDocument.cs (78줄) - **8개+ 프로퍼티, 1개 중첩 클래스**
- ❌ UploadedFileDocument.cs (79줄) - **12개+ 프로퍼티**
- ❌ FileSessionDocument.cs (~60줄) - **10개+ 프로퍼티**

#### 3.2 비즈니스 로직 계층 (8개 파일, ~7,500줄)
- ❌ uc_MultiFileUploadSessionProcess.cs (1,100줄)
- ❌ uc_MultiFileUploadExcelProcess.cs (894줄)
- ❌ uc_MultiFileUploadFileProcess.cs (925줄)
- ❌ uc_ClassificationProcess.cs (1,025줄)
- ❌ uc_DataTransformProcess.cs (653줄)
- ❌ uc_ClusteringSearchEngine.cs (791줄)
- ❌ uc_DetailClusteringSearchEngine.cs (750줄)
- ❌ uc_PreprocessingProcess.cs (207줄)

### **Phase 4: 유틸리티 및 UI 계층**
#### 4.1 클러스터 관리 유틸리티 (4개 파일, ~1,721줄)
- ❌ ClusterDataManager.cs (543줄) - **12개+ 메서드, 5개+ 필드**
- ❌ ClusterDisplayManager.cs (518줄) - **10개+ 메서드, 4개+ 필드**
- ❌ ClusteringManager.cs (443줄) - **15개+ 메서드, 6개+ 필드**
- ❌ ClusterSearchEngine.cs (217줄) - **8개+ 메서드, 4개+ 필드**

#### 4.2 기타 유틸리티 (6개 파일, ~1,500줄)
- ❌ KeywordExtractor.cs (389줄) - **15개+ 메서드, 5개 중첩 클래스, 4개 필드**
- ❌ ProcessProgressForm.cs (298줄) - **4개 메서드, 1개 델리게이트, 4개 필드**
- ❌ SessionDataProcessor.cs (445줄) - **8개+ 메서드**
- ❌ SystemPerformanceOptimizer.cs (159줄) - **6개+ 메서드**
- ❌ TrialManager.cs (98줄) - **5개+ 메서드, 3개 필드**
- ❌ 기타 유틸리티 클래스들

#### 4.3 UI 계층 (12개 파일, ~7,000줄)
- ❌ Form1.cs (~1,241줄) - **15개+ 메서드, 3개 필드**
- ❌ uc_MultiFileUpload.cs (1,055줄) - **30개+ 메서드, 12개+ 필드**
- ❌ uc_Clustering.cs (1,532줄) - **25개+ 메서드, 10개+ 필드**
- ❌ uc_DetailClustering.cs (1,507줄) - **20개+ 메서드, 8개+ 필드**
- ❌ uc_FileLoad.cs (947줄) - **50개+ 메서드, 15개+ 필드**
- ❌ uc_DataTransform.cs, uc_Classification.cs, uc_preprocessing.cs
- ❌ popup/ 디렉토리의 팝업 컨트롤들 (4개 파일, ~857줄)

### **Phase 5: 시스템 및 진입점**
- ⚠️ Program.cs (~62줄) - **6개 메서드 (1개 부분완료)**
- ❌ userControlHandler.cs (20줄) - **10개+ 정적 필드**

---

## 🎯 작업 가이드라인

### **XML 문서 주석 표준 형식**
```csharp
/// <summary>
/// [메서드/클래스의 목적과 역할을 명확히 설명]
/// </summary>
/// <param name="paramName">[매개변수 설명 - 타입, 목적, 제약사항]</param>
/// <returns>[반환값 설명 - 타입, 의미, 가능한 값 범위]</returns>
/// <remarks>
/// [상세 구현 로직, 알고리즘, 성능 고려사항, 의존성 정보]
/// 성능: [시간복잡도, 메모리 사용량 등]
/// 의존성: [사용하는 서비스/클래스]
/// </remarks>
/// <exception cref="ExceptionType">[발생 가능한 예외 상황]</exception>
```

### **작업 진행 방식**
1. **Phase별 순차 진행**: 우선순위에 따라 Phase별로 작업
2. **파일 단위 완료**: 한 파일의 모든 요소를 완료 후 다음 파일 진행
3. **상태 업데이트**: 작업 완료 시 해당 요소를 ✅로 변경
4. **빌드 검증**: 각 파일 완료 후 컴파일 오류 없음을 확인
5. **품질 검토**: Microsoft C# XML 주석 표준 준수 확인

### **우선순위 기준**
- ⭐⭐⭐⭐⭐ (Critical): 핵심 비즈니스 로직, 이미 완료된 Phase 1
- ⭐⭐⭐⭐ (Very High): Repository, MongoDB 계층
- ⭐⭐⭐ (High): 비즈니스 로직, UI 계층
- ⭐⭐ (Medium): 유틸리티, 팝업
- ⭐ (Low): 시스템 설정, 진입점

---

**최종 업데이트**: 2025-09-03  
**문서 상태**: Phase 1 완료, Phase 2 준비 완료  
**다음 작업**: Phase 2 - Repository 계층 주석 작업 시작