# FinanceTool MongoDB 컬렉션 및 변수 활용 분석

## 문서 개요

본 문서는 FinanceTool 프로젝트의 MongoDB 데이터베이스 구조와 각 컬렉션의 필드(변수) 활용 현황을 분석한 자료입니다.

**프로젝트 정보**
- **저장소**: https://github.com/scschwan/lgcns_1st_nosql
- **데이터베이스**: MongoDB (localhost:27017)
- **데이터베이스명**: FinanceTool
- **연결 설정**: MaxConnectionPoolSize=3000, MaxConnecting=1000

---

## MongoDB 컬렉션 목록

FinanceTool 프로젝트는 **8개의 주요 컬렉션**으로 구성되어 있습니다:

| 컬렉션명 | 문서 모델 | Repository | 용도 |
|---------|---------|-----------|------|
| raw_data | RawDataDocument | RawDataRepository | Excel 원시 데이터 저장 |
| process_data | ProcessDataDocument | ProcessDataRepository | 가공된 데이터 저장 |
| process_view_data | ProcessViewDocument | ProcessViewRepository | UI 최적화 뷰 데이터 |
| clustering_results | ClusteringResultDocument | ClusteringRepository | 클러스터링 결과 |
| column_mapping | ColumnMappingDocument | ColumnMappingRepository | 컬럼 매핑 정보 |
| keywords | KeywordDocument | - | 키워드 분석 정보 |
| file_sessions | FileSessionDocument | FileSessionRepository | 파일 세션 관리 |
| uploaded_files | UploadedFileDocument | UploadedFileRepository | 업로드 파일 메타데이터 |

---

## 1. raw_data 컬렉션

### 용도
Excel 파일에서 가져온 원시 데이터를 저장하는 기본 컬렉션입니다.

### 필드 구조

```csharp
{
    "_id": ObjectId,                    // MongoDB 고유 식별자
    "data": Dictionary<string, object>, // 동적 컬럼 데이터
    "import_date": DateTime,            // 데이터 가져오기 날짜
    "file_name": string,                // 원본 Excel 파일명
    "is_hidden": bool,                  // 데이터 가시성 플래그 (기본: false)
    "hidden_reason": string             // 숨김 이유 (선택적)
}
```

### 필드별 활용

| 필드명 | 타입 | 필수 | 활용 목적 |
|--------|------|------|----------|
| _id | ObjectId | ✓ | 문서 고유 식별, 다른 컬렉션에서 참조 |
| data | Dictionary | ✓ | Excel의 모든 컬럼 데이터를 키-값 쌍으로 저장 |
| import_date | DateTime | ✓ | 데이터 이력 추적, 시간순 정렬 |
| file_name | string | | 데이터 출처 추적, 파일별 필터링 |
| is_hidden | bool | ✓ | UI에서 데이터 숨김 처리 (논리적 삭제) |
| hidden_reason | string | | 숨김 원인 기록 (복원 시 참고) |

### 주요 활용 사례

```csharp
// 1. 페이징 처리된 데이터 조회 (uc_FileLoad.cs)
var filter = Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false);
var (items, total) = await MongoDBManager.Instance.FindWithPaginationAsync(
    "raw_data", filter, pageNumber, pageSize
);

// 2. 데이터 숨김 처리 (is_hidden 플래그 업데이트)
var update = Builders<RawDataDocument>.Update.Set(d => d.IsHidden, true);
await MongoDBManager.Instance.UpdateDocumentsAsync("raw_data", filter, update);

// 3. 배치 삽입 (대용량 데이터 처리)
await RawDataRepository.InsertRawDataBatchAsync(documents, batchSize: 10000);
```

---

## 2. process_data 컬렉션

### 용도
raw_data에서 선택된 필드만 추출하여 정제한 데이터를 저장합니다.

### 필드 구조

```csharp
{
    "_id": ObjectId,                    // MongoDB 고유 식별자
    "raw_data_id": ObjectId,            // 원시 데이터 문서 참조
    "data": Dictionary<string, object>, // 선택된 컬럼 데이터
    "import_date": DateTime,            // 원시 데이터 가져오기 날짜
    "processed_date": DateTime,         // 가공 완료 날짜
    "cluster_id": int?,                 // 할당된 클러스터 ID (선택적)
    "cluster_name": string              // 할당된 클러스터명 (선택적)
}
```

### 필드별 활용

| 필드명 | 타입 | 필수 | 활용 목적 |
|--------|------|------|----------|
| _id | ObjectId | ✓ | 가공 데이터 고유 식별 |
| raw_data_id | ObjectId | ✓ | raw_data와 1:1 참조 관계 유지 |
| data | Dictionary | ✓ | 분석에 필요한 컬럼만 저장 |
| import_date | DateTime | ✓ | raw_data에서 복사 (데이터 이력) |
| processed_date | DateTime | ✓ | 가공 시점 기록 |
| cluster_id | int? | | 클러스터링 후 할당된 ID |
| cluster_name | string | | 클러스터링 후 할당된 이름 |

### 주요 활용 사례

```csharp
// 1. raw_data에서 process_data로 변환 (uc_preprocessing.cs)
var processDoc = new ProcessDataDocument
{
    RawDataId = rawDoc.Id,
    Data = selectedColumns,
    ImportDate = rawDoc.ImportDate,
    ProcessedDate = DateTime.Now
};

// 2. 병렬 배치 삽입
await ProcessDataRepository.InsertProcessDataBatchesAsync(documents, batchSize: 5000);

// 3. 클러스터 할당 업데이트
var update = Builders<ProcessDataDocument>.Update
    .Set(d => d.ClusterId, clusterId)
    .Set(d => d.ClusterName, clusterName);
```

---

## 3. process_view_data 컬렉션

### 용도
UI 렌더링에 최적화된 뷰 데이터를 저장합니다. process_data와 raw_data를 조인한 결과를 캐싱합니다.

### 필드 구조

```csharp
{
    "_id": ObjectId,                        // MongoDB 고유 식별자
    "process_data_id": ObjectId,            // process_data 문서 참조
    "raw_data_id": ObjectId,                // raw_data 문서 참조 (신규)
    "keywords": {                           // 키워드 정보
        "final_keywords": List<string>      // 최종 확정된 키워드 목록
    },
    "money": object,                        // 거래 금액
    "department": string,                   // 부서 정보 (신규)
    "supplier": string,                     // 공급업체 정보 (신규)
    "last_modified_date": DateTime          // 최종 수정 날짜
}
```

### 필드별 활용

| 필드명 | 타입 | 필수 | 활용 목적 |
|--------|------|------|----------|
| _id | ObjectId | ✓ | 뷰 데이터 고유 식별 |
| process_data_id | ObjectId | ✓ | process_data 참조 |
| raw_data_id | ObjectId | ✓ | raw_data 직접 참조 (빠른 조회) |
| keywords.final_keywords | List | ✓ | 키워드 기반 검색/필터링 |
| money | object | | 금액 표시, 정렬, 집계 |
| department | string | | 부서별 비용 분석 |
| supplier | string | | 공급업체별 거래 내역 분석 |
| last_modified_date | DateTime | ✓ | 캐시 무효화, 증분 업데이트 |

### 주요 활용 사례

```csharp
// 1. preprocessing 완료 후 process_view_data 생성 (btn_complete_Click)
var viewDoc = new ProcessViewDocument
{
    ProcessDataId = processDoc.Id,
    RawDataId = processDoc.RawDataId,
    Keywords = new KeywordInfo { FinalKeywords = extractedKeywords },
    Money = amount,
    Department = dept,
    Supplier = supplier
};

// 2. 병렬 배치 삽입
await ProcessViewRepository.InsertProcessViewDataBatchAsync(documents, batchSize: 5000);

// 3. datatransform에서 조회 및 활용
var viewData = await ProcessViewRepository.GetAllAsync();
```

---

## 4. clustering_results 컬렉션

### 용도
클러스터링 분석 결과를 저장하며, 계층적 클러스터 구조를 지원합니다.

### 필드 구조

```csharp
{
    "_id": ObjectId,                    // MongoDB 고유 식별자
    "cluster_number": int,              // 클러스터 고유 번호
    "cluster_id": int,                  // 상위 병합 클러스터 ID
    "cluster_sub_id": int,              // 세부 클러스터 ID
    "cluster_name": string,             // 클러스터명 (20자 제한)
    "keywords": List<string>,           // 대표 키워드 목록
    "count": int,                       // 포함 항목 수
    "total_amount": decimal,            // 합산 금액
    "data_indices": List<ObjectId>,     // raw_data 문서 ID 목록
    "created_at": DateTime              // 생성 날짜
}
```

### 클러스터 상태 관리 체계

| 상태 | cluster_id | cluster_sub_id | 의미 |
|------|-----------|---------------|------|
| 일반 클러스터 | -1 | -1 | 독립적인 개별 클러스터 (미병합) |
| 상위 병합 클러스터 | cluster_number | -1 | 다른 클러스터들을 포함 |
| 하위 병합 클러스터 | 상위번호 | -1 | 특정 상위 클러스터에 병합됨 |
| 세부 상위 클러스터 | 기존상위번호 | cluster_number | 세부 클러스터링 상위 |
| 세부 하위 클러스터 | 기존상위번호 | 세부상위번호 | 세부 클러스터에 병합됨 |

### 필드별 활용

| 필드명 | 타입 | 필수 | 활용 목적 |
|--------|------|------|----------|
| _id | ObjectId | ✓ | 클러스터 문서 고유 식별 |
| cluster_number | int | ✓ | 클러스터 번호 (UI 표시) |
| cluster_id | int | ✓ | 병합 상태 추적 (-1: 미병합) |
| cluster_sub_id | int | ✓ | 세부 클러스터링 상태 (-1: 미진행) |
| cluster_name | string | ✓ | 사용자 정의 이름 |
| keywords | List | ✓ | 클러스터 특성 파악 |
| count | int | ✓ | 클러스터 크기 |
| total_amount | decimal | ✓ | 금액 기반 중요도 |
| data_indices | List | ✓ | 원본 데이터 참조 |
| created_at | DateTime | ✓ | 클러스터링 이력 |

### 주요 활용 사례

```csharp
// 1. 일반 클러스터링 화면 조회 (uc_Clustering.cs)
var filter = Builders<ClusteringResultDocument>.Filter.Eq(d => d.ClusterSubId, -1);
var clusters = await ClusteringRepository.GetAllClustersAsync(filter);

// 2. 세부 클러스터링 화면 조회 (uc_DetailClustering.cs)
var filter = Builders<ClusteringResultDocument>.Filter.Eq(d => d.ClusterId, selectedClusterId);
var detailClusters = await ClusteringRepository.GetAllClustersAsync(filter);

// 3. 클러스터 병합
var mergedCluster = new ClusteringResultDocument
{
    ClusterNumber = newNumber,
    ClusterId = newNumber,  // 상위 클러스터 표시
    ClusterSubId = -1,
    Count = childClusters.Sum(c => c.Count),
    TotalAmount = childClusters.Sum(c => c.TotalAmount),
    Keywords = childClusters.SelectMany(c => c.Keywords).Distinct().ToList(),
    DataIndices = childClusters.SelectMany(c => c.DataIndices).ToList()
};

// 4. 하위 클러스터 ID 업데이트
var update = Builders<ClusteringResultDocument>.Update
    .Set(d => d.ClusterId, parentClusterId);
await ClusteringRepository.UpdateManyAsync(filter, update);
```

---

## 5. column_mapping 컬렉션

### 용도
Excel 원본 컬럼명과 사용자 친화적 표시명 간의 매핑 정보를 관리합니다.

### 필드 구조

```csharp
{
    "_id": ObjectId,                // MongoDB 고유 식별자
    "original_name": string,        // Excel 원본 컬럼명
    "display_name": string,         // UI 표시명
    "data_type": string,            // 데이터 타입 (text/number/currency/date/boolean)
    "is_visible": bool,             // UI 표시 여부 (기본: true)
    "sequence": int                 // 표시 순서
}
```

### 필드별 활용

| 필드명 | 타입 | 필수 | 활용 목적 |
|--------|------|------|----------|
| _id | ObjectId | ✓ | 매핑 정보 고유 식별 |
| original_name | string | ✓ | Excel 컬럼명 (변경 불가) |
| display_name | string | ✓ | 사용자 친화적 이름 |
| data_type | string | ✓ | 정렬, 필터링, 유효성 검사 |
| is_visible | bool | ✓ | 컬럼 표시/숨김 제어 |
| sequence | int | ✓ | DataGridView 컬럼 순서 |

### 주요 활용 사례

```csharp
// 1. 컬럼 매핑 조회 (uc_FileLoad.cs, uc_preprocessing.cs)
var mappings = await ColumnMappingRepository.GetAllAsync();
var visibleColumns = mappings.Where(m => m.IsVisible).OrderBy(m => m.Sequence);

// 2. UI 콤보박스에 표시
foreach (var mapping in visibleColumns)
{
    comboBox.Items.Add(mapping.DisplayName);
}

// 3. 컬럼 가시성 토글
var update = Builders<ColumnMappingDocument>.Update
    .Set(m => m.IsVisible, !currentValue);
```

---

## 6. keywords 컬렉션

### 용도
텍스트 분석 및 클러스터링 과정에서 추출된 키워드 정보를 저장합니다.

### 필드 구조

```csharp
{
    "_id": ObjectId,                    // MongoDB 고유 식별자
    "keyword": string,                  // 키워드 텍스트
    "count": int,                       // 출현 빈도수
    "source_columns": List<string>,     // 키워드 출처 컬럼 목록
    "related_keywords": List<string>,   // 관련 키워드 목록
    "document_ids": List<ObjectId>,     // 키워드 포함 문서 ID 목록
    "last_updated": DateTime,           // 최종 업데이트 날짜
    "weight": double,                   // 키워드 가중치 (기본: 1.0)
    "is_recommendation": bool           // 추천 키워드 여부 (기본: false)
}
```

### 필드별 활용

| 필드명 | 타입 | 필수 | 활용 목적 |
|--------|------|------|----------|
| _id | ObjectId | ✓ | 키워드 문서 고유 식별 |
| keyword | string | ✓ | 실제 키워드 텍스트 |
| count | int | ✓ | 중요도 계산, 우선순위 |
| source_columns | List | ✓ | 키워드 출처 추적 |
| related_keywords | List | ✓ | 키워드 네트워크 분석 |
| document_ids | List | ✓ | 역추적 (키워드→문서) |
| last_updated | DateTime | ✓ | 캐시 무효화 |
| weight | double | ✓ | TF-IDF, 검색 랭킹 |
| is_recommendation | bool | ✓ | 자동 완성, 인기 검색어 |

### 주요 활용 사례

```csharp
// 1. 키워드 추출 및 저장 (uc_preprocessing.cs)
// - keyword_seper_split_Click: 구분자 기반 분리
// - keyword_model_split_Click: NLP 모델 기반 추출

// 2. 키워드 빈도 업데이트
var filter = Builders<KeywordDocument>.Filter.Eq(k => k.Keyword, keyword);
var update = Builders<KeywordDocument>.Update
    .Inc(k => k.Count, 1)
    .AddToSet(k => k.DocumentIds, docId);

// 3. 추천 키워드 조회
var recommendedKeywords = await KeywordRepository
    .Find(k => k.IsRecommendation == true)
    .SortByDescending(k => k.Weight)
    .Limit(10)
    .ToListAsync();
```

---

## 7. file_sessions 컬렉션

### 용도
파일 업로드 세션 정보를 관리하며, 여러 파일을 하나의 세션으로 그룹화합니다.

### 필드 구조

```csharp
{
    "_id": ObjectId,                // MongoDB 고유 식별자
    "session_name": string,         // 세션명
    "worker_name": string,          // 작업자명
    "account_column_name": string,  // 계정 컬럼명
    "amount_column_name": string,   // 금액 컬럼명
    "total_amount": decimal,        // 총 금액
    "total_rows": decimal,          // 총 행 수
    "status": string,               // 처리 상태 (processing/completed/failed)
    "created_date": DateTime,       // 생성 날짜
    "completed_date": DateTime?,    // 완료 날짜 (선택적)
    "result_file_path": string,     // 결과 파일 경로
    "account_name": string,         // 계정명
    "file_ids": List<ObjectId>,     // 업로드된 파일 ID 목록
    "file_count": int?,             // 파일 개수 (계산된 값)
    "updated_date": DateTime?       // 업데이트 날짜 (병합 시)
}
```

### 필드별 활용

| 필드명 | 타입 | 필수 | 활용 목적 |
|--------|------|------|----------|
| _id | ObjectId | ✓ | 세션 고유 식별 |
| session_name | string | ✓ | 세션 구별 |
| worker_name | string | ✓ | 작업자 추적 |
| account_column_name | string | ✓ | 데이터 매핑 |
| amount_column_name | string | ✓ | 금액 컬럼 지정 |
| total_amount | decimal | ✓ | 세션별 집계 |
| total_rows | decimal | ✓ | 데이터 규모 파악 |
| status | string | ✓ | 처리 진행 상황 |
| file_ids | List | ✓ | 세션 내 파일 그룹화 |

---

## 8. uploaded_files 컬렉션

### 용도
업로드된 Excel 파일의 메타데이터를 저장합니다.

### 필드 구조

```csharp
{
    "_id": ObjectId,                    // MongoDB 고유 식별자
    "original_filename": string,        // 원본 파일명
    "stored_filename": string,          // 저장된 파일명
    "file_path": string,                // 파일 경로
    "file_size": long,                  // 파일 크기 (bytes)
    "upload_date": DateTime,            // 업로드 날짜
    "account_column_name": string,      // 계정 컬럼명
    "amount_column_name": string,       // 금액 컬럼명
    "detected_columns": List<string>,   // 감지된 컬럼 목록
    "total_rows": decimal,              // 총 행 수
    "total_amount": decimal,            // 총 금액
    "session_id": ObjectId?,            // 세션 ID (선택적)
    "processing_status": string,        // 처리 상태 (uploaded/processed/error)
    "account_contents": List<string>    // 계정명 컬럼의 고유값 목록
}
```

### 필드별 활용

| 필드명 | 타입 | 필수 | 활용 목적 |
|--------|------|------|----------|
| _id | ObjectId | ✓ | 파일 고유 식별 |
| original_filename | string | ✓ | 사용자 파일명 추적 |
| stored_filename | string | ✓ | 시스템 파일명 |
| file_size | long | ✓ | 용량 관리 |
| detected_columns | List | ✓ | 컬럼 자동 감지 결과 |
| session_id | ObjectId? | | 세션 그룹화 |
| processing_status | string | ✓ | 처리 상태 추적 |
| account_contents | List | ✓ | 계정별 필터링 |

---

## 컬렉션 간 관계도

```
┌─────────────────┐
│  uploaded_files │
│  (파일 메타)     │
└────────┬────────┘
         │
         ↓
┌─────────────────┐
│  file_sessions  │
│  (세션 관리)     │
└─────────────────┘

┌─────────────────┐
│    raw_data     │──→ Excel 원시 데이터
└────────┬────────┘
         │ 1:1
         ↓
┌─────────────────┐
│  process_data   │──→ 가공된 데이터
└────────┬────────┘
         │ 1:1
         ↓
┌──────────────────┐
│ process_view_data│──→ UI 최적화 뷰
└────────┬─────────┘
         │
         ↓
┌──────────────────┐
│clustering_results│──→ 클러스터링 결과
│                  │    (data_indices로 raw_data 참조)
└──────────────────┘

┌─────────────────┐
│ column_mapping  │──→ 컬럼 메타데이터
└─────────────────┘

┌─────────────────┐
│    keywords     │──→ 키워드 분석
│                 │    (document_ids로 raw_data 참조)
└─────────────────┘
```

---

## 데이터 흐름 (Workflow)

### 1단계: 파일 업로드 (uc_FileLoad)
```
Excel 파일 → uploaded_files → file_sessions → raw_data
```
- Excel 파일 읽기
- 메타데이터 저장 (uploaded_files)
- 세션 생성 (file_sessions)
- 원시 데이터 배치 삽입 (raw_data)

### 2단계: 전처리 (uc_preprocessing)
```
raw_data → process_data → process_view_data
```
- 필요한 컬럼 선택
- 데이터 정제
- 키워드 추출 (keywords 컬렉션)
- 병렬 처리로 process_data, process_view_data 생성

### 3단계: 데이터 변환 (uc_datatransform)
```
process_view_data → 키워드 변환 → clustering 준비
```
- 키워드 기반 데이터 변환
- 클러스터링을 위한 특성 추출

### 4단계: 클러스터링 (uc_Clustering)
```
process_data → clustering_results
```
- 클러스터 생성
- 클러스터 병합 (계층 구조)
- 세부 클러스터링 (uc_DetailClustering)

### 5단계: 분류 및 리포트 (uc_Classification)
```
clustering_results → 집계 → 리포트 생성
```
- 클러스터별 통계
- Excel 리포트 생성

---

## Repository 패턴 활용

각 컬렉션은 전용 Repository를 통해 접근합니다:

### BaseRepository<T>
모든 Repository의 기본 클래스로 공통 CRUD 작업을 제공합니다.

```csharp
public abstract class BaseRepository<T>
{
    protected IMongoCollection<T> Collection;
    
    // 공통 메서드
    Task<List<T>> GetAllAsync();
    Task<T> GetByIdAsync(string id);
    Task InsertAsync(T document);
    Task InsertManyAsync(IEnumerable<T> documents);
    Task<long> UpdateAsync(FilterDefinition<T> filter, UpdateDefinition<T> update);
    Task<long> DeleteAsync(FilterDefinition<T> filter);
}
```

### 특화 Repository

| Repository | 특화 기능 |
|-----------|---------|
| RawDataRepository | 페이징, 숨김 처리, 배치 삽입 |
| ProcessDataRepository | 배치 삽입, raw_data 참조 |
| ProcessViewRepository | 배치 삽입, 복합 조회 |
| ClusteringRepository | 병합/분리, 계층 조회, 통계 |
| ColumnMappingRepository | 순서 관리, 가시성 제어 |
| FileSessionRepository | 세션 관리, 파일 그룹화 |
| UploadedFileRepository | 파일 메타데이터 관리 |

---

## 병렬 처리 전략

대용량 데이터 처리를 위해 적응형 배치 크기를 사용합니다:

```csharp
// 데이터 크기별 배치 크기 결정
private int DetermineBatchSize(int totalItems)
{
    if (totalItems < 100000)
        return 10000;  // 10만 건 이하
    else if (totalItems < 500000)
        return 5000;   // 10만~50만 건
    else
        return 2000;   // 50만 건 이상
}

// 병렬 배치 삽입 예시
var batches = documents
    .Select((doc, index) => new { doc, index })
    .GroupBy(x => x.index / batchSize)
    .Select(g => g.Select(x => x.doc).ToList())
    .ToList();

await Parallel.ForEachAsync(batches, 
    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
    async (batch, ct) => {
        await repository.InsertManyAsync(batch);
    }
);
```

---

## 인덱스 전략

성능 최적화를 위한 권장 인덱스:

### raw_data 컬렉션
```javascript
db.raw_data.createIndex({ "import_date": -1 })
db.raw_data.createIndex({ "is_hidden": 1 })
db.raw_data.createIndex({ "file_name": 1 })
```

### process_data 컬렉션
```javascript
db.process_data.createIndex({ "raw_data_id": 1 })
db.process_data.createIndex({ "cluster_id": 1 })
db.process_data.createIndex({ "processed_date": -1 })
```

### clustering_results 컬렉션
```javascript
db.clustering_results.createIndex({ "cluster_id": 1 })
db.clustering_results.createIndex({ "cluster_sub_id": 1 })
db.clustering_results.createIndex({ "cluster_number": 1 }, { unique: true })
db.clustering_results.createIndex({ "keywords": 1 })
```

### process_view_data 컬렉션
```javascript
db.process_view_data.createIndex({ "process_data_id": 1 })
db.process_view_data.createIndex({ "raw_data_id": 1 })
db.process_view_data.createIndex({ "keywords.final_keywords": 1 })
```

---

## 성능 최적화 고려사항

### 1. 연결 풀 설정
```csharp
MaxConnectionPoolSize = 3000
MaxConnecting = 1000
SocketTimeout = 10분
```

### 2. 배치 처리
- raw_data 삽입: 10,000건/배치
- process_data 삽입: 5,000건/배치
- 업데이트 작업: 2,000건/배치

### 3. 메모리 관리
- 192GB RAM 활용
- 인메모리 캐싱 (DataHandler.processTable)
- 병렬 처리 (CPU 코어 수 활용)

### 4. MongoDB 집계 파이프라인 활용
```csharp
// 서버 측 집계 예시
var pipeline = new[]
{
    new BsonDocument("$match", new BsonDocument("is_hidden", false)),
    new BsonDocument("$group", new BsonDocument
    {
        { "_id", "$cluster_id" },
        { "count", new BsonDocument("$sum", 1) },
        { "total", new BsonDocument("$sum", "$total_amount") }
    })
};
```

---

## 데이터 일관성 보장

### 1. 참조 무결성
- raw_data ← process_data (raw_data_id)
- process_data ← process_view_data (process_data_id)
- raw_data ← clustering_results (data_indices)

### 2. 트랜잭션 사용 사례
```csharp
// 클러스터 병합 시 트랜잭션 (원자성 보장)
using (var session = await client.StartSessionAsync())
{
    session.StartTransaction();
    try
    {
        // 1. 새 병합 클러스터 생성
        await clusteringRepo.InsertAsync(mergedCluster);
        
        // 2. 하위 클러스터 ID 업데이트
        await clusteringRepo.UpdateManyAsync(filter, update);
        
        await session.CommitTransactionAsync();
    }
    catch
    {
        await session.AbortTransactionAsync();
        throw;
    }
}
```

---

## 모니터링 및 로깅

### Debug 로깅
```csharp
Debug.WriteLine($"[MongoDBManager] 컬렉션 '{collName}' 초기화 완료");
Debug.WriteLine($"MongoDB 연결 실패 (재시도 {retryCount}/{maxRetries})");
```

### 진행 상황 추적
```csharp
ProcessProgressForm.UpdateProgressDelegate progressCallback
progressCallback?.Invoke(progress, message);
```

---

## 향후 확장 가능성

### 1. AI 통합 준비
- keywords 컬렉션: NLP 키워드 추출
- clustering_results: 머신러닝 기반 클러스터링
- weight, is_recommendation 필드 활용

### 2. 실시간 분석
- Change Streams 활용 가능
- 실시간 대시보드 구축

### 3. 분산 처리
- Sharding 전략 수립 가능
- 대규모 데이터 (1000만+ 건) 대응

---

## 문서 버전

- **작성일**: 2025-12-26
- **프로젝트 버전**: MongoDB 마이그레이션 완료 버전
- **분석 기준**: GitHub main 브랜치 최신 커밋

