# MongoDB 마이그레이션 아키텍처 및 진행 상황

## 프롬프트 지침

이 문서는 코드 마이그레이션 작업 시 AI 어시스턴트가 따라야 할 규칙을 포함합니다:

* **소스 수정 규칙**: 
     - 소스를 수정하기 전 프로젝트 내 모든 코드를 확인하여 각 소스 간 참조 영역을 인지할 것
     - MongoDB 관련 데이터 CRUD 로직만 수정할 것
     - 화면 출력 방식이나 UI 상에서 수정하는 데이터 수정 내역은 변경하지 말 것
* **폐기 예정 코드 처리**:
     - DBManager.cs, DataConverter.cs는 폐기할 소스이므로 해당 인스턴스에서 호출되는 함수는 MongoDBManager나 MongoDataConverter를 활용하거나 신규로 추가하여 활용할 것
* **문서 관리 규칙**:
     - MD 파일 수정 요청 시에는 기존 내용을 그대로 유지하면서 업데이트 내역만 신규로 추가할 것

## 프로젝트 개요

SQLite 기반의 데이터 처리 애플리케이션을 MongoDB로 마이그레이션하는 프로젝트입니다. 주요 목적은 대용량 데이터 처리 성능 향상과 동적 컬럼 관리의 유연성 확보입니다.

## 기본 원칙

* **SQLite 대체**: SQLite를 활용하는 모든 로직은 MongoDB 로직으로 완전히 대체합니다.
* **코드 관리**: Git repository 내용은 직접 수정할 수 없으며, 접근 권한만 가지고 있습니다. 모든 변경 사항은 별도로 관리합니다.
* **버전 이력**: 소스 버전 이력을 계속 남겨 수정 사항을 다른 세션에서도 이해할 수 있도록 합니다.
* **최소 수정 원칙**: UI 로직은 그대로 유지하고 데이터 액세스 로직만 MongoDB로 교체합니다.
* **역할 분리**: DBManager.cs는 MongoDBManager.cs로, DataConverter.cs는 MongoDataConverter.cs로 대체합니다.

## Data Clustering(Grouping) 원칙(Clustering.cs
)
### 클러스터 구조 및 ID 관리

* 각 클러스터는 고유 ID를 가지며, 병합 시 신규 ID 생성
* 클러스터 상태는 ID와 ClusterID 관계로 구분:

* ClusterID = ID: 병합된 클러스터(상위) 객체
* ClusterID = -1: 아직 어떤 클러스터에도 병합되지 않은 독립 객체
* ClusterID > 0 & ClusterID ≠ ID: 이미 다른 클러스터에 속한 하위 객체



### 데이터 병합 메커니즘

* 클러스터 병합 시 하위 클러스터들의 모든 데이터가 합쳐짐:

* data_indices: 원본 데이터 ID 참조 목록
* count: 포함된 항목 수
* total_amount: 금액 합계
* keywords: 키워드 목록(중복 제거)



### 병합 시나리오

* 병합된 클러스터 간 추가 병합 가능
* 병합된 클러스터에 미병합 클러스터 추가 가능
* 클러스터 병합 취소 기능으로 기존 구조 복원 가능

### 추가 관리 기능

* 클러스터명 자동 생성 및 수동 변경 가능(20자 제한)
* 키워드 기반 클러스터 검색
* 공급업체 기반 클러스터 필터링
* 미병합 클러스터 "Undefined" 일괄 통합


## 대용량 데이터 처리 전략

### 하드웨어 환경
- **CPU**: INTEL U9 285K (고성능 멀티코어 CPU)
- **저장장치**: 2TB SSD (고속 저장 장치)
- **메모리**: 192GB DDR5 RAM (대용량 메모리)
- **용도**: 100만~1000만 건의 데이터 처리 환경

### 최적화 권장 사항

#### 데이터 로딩 전략
- **지연 로딩(Lazy Loading) 구현**: 필요한 데이터만 메모리에 로드
- **페이징 처리 최적화**: 현재 구현된 페이징 로직을 개선하여 대용량 데이터에서도 효율적으로 동작하도록 함
- **인메모리 캐싱 활용**: 192GB 메모리를 활용하여 자주 접근하는 데이터 캐싱

#### 데이터 처리 전략
1. **MongoDB 서버 측 처리 확대**:
   - MongoDB 집계 파이프라인(`$match`, `$group`, `$project` 등) 활용
   - 데이터 필터링 및 그룹화 작업을 클라이언트가 아닌 서버에서 수행
   - 복잡한 조인 및 집계 작업도 MongoDB 서버 측에서 처리

2. **DataTable 사용 최적화**:
   - 전체 데이터를 DataTable로 변환하는 대신 필요한 부분만 변환
   - 대규모 DataTable 사용 시 메모리 관리와 GC 부하에 주의
   - `DataHandler.processTable`과 같은 전역 변수 사용 최소화

3. **메모리 관리 전략**:
   - 대규모 객체는 필요 시 생성하고 빠르게 해제
   - 192GB 메모리 활용을 위한 캐싱 전략 수립
   - 불필요한 참조 제거 및 메모리 누수 방지

4. **병렬 처리 구현**:
   - 고성능 CPU를 활용한 병렬 데이터 처리 구현
   - file_load.cs 의 LoadExcelDataAsync 함수와 InsertRawDataBatchAsync,InsertProcessDataBatchesAsync 함수처럼 반드시 구현할 것
   - 데이터 처리 파이프라인 구축

#### 인덱스 전략
- **MongoDB 인덱스 최적화**: 검색, 정렬, 집계에 사용되는 필드에 적절한 인덱스 구성
- **복합 인덱스 활용**: 자주 함께 검색되는 필드를 위한 복합 인덱스 생성
- **인덱스 사용 모니터링**: 쿼리 실행 계획 분석을 통한 인덱스 효율성 검증

#### UI 응답성 유지
- **백그라운드 작업 처리**: 무거운 데이터 처리는 백그라운드 스레드에서 수행
- **비동기 프로그래밍 확대**: `async/await` 패턴을 일관되게 적용하여 UI 스레드 블로킹 방지
- **진행 상황 표시 개선**: 사용자에게 진행 상황을 보다 정확하게 전달



## 폴더 구조

```
FinanceTool/
│
├── Data/  # 데이터 액세스 계층 폴더
│   ├── MongoDBManager.cs     # MongoDB 연결 및 기본 작업 관리 (완료됨)
│   └── MongoDataConverter.cs # Excel → MongoDB 변환 (완료됨)
│
├── Models/  # 모델 클래스 폴더
│   └── MongoModels/  # MongoDB 관련 모델 폴더
│       ├── RawDataDocument.cs           # raw_data 컬렉션 모델 (완료됨)
│       ├── ProcessDataDocument.cs       # process_data 컬렉션 모델 (완료됨)
│       ├── ColumnMappingDocument.cs     # column_mapping 컬렉션 모델 (완료됨)
│       ├── ClusteringResultDocument.cs  # clustering_results 컬렉션 모델 (완료됨)
│       └── KeywordDocument.cs           # keywords 컬렉션 모델 (완료됨)
│
└── Repositories/  # MongoDB 컬렉션별 특화 저장소 폴더
    ├── BaseRepository.cs            # 기본 저장소 패턴 (완료됨)
    ├── RawDataRepository.cs         # raw_data 컬렉션 특화 작업 (완료됨)
    ├── ProcessDataRepository.cs     # process_data 컬렉션 특화 작업 (완료됨)
    └── ClusteringRepository.cs      # 클러스터링 관련 특화 작업 (완료됨)
```

## 진행 상황

### 완료된 작업

1. **코어 인프라 구축**:
   - MongoDB 연결 관리자 구현 (`MongoDBManager.cs`)
   - 문서 모델 클래스 정의 (`RawDataDocument.cs` 등)
   - 저장소 패턴 구현 (`BaseRepository.cs` 등)

2. **uc_FileLoad.cs 수정**:
   - Excel 파일 -> MongoDB 저장 구현
   - MongoDB 기반의 데이터 페이징 처리
   - `is_hidden` 필드를 사용한 데이터 숨김 처리
   - UI 그리드뷰에 데이터 표시 및 스타일링
   
3. **uc_processing.cs 수정**:
  - sqlite -> mongodb 대체 및 관련 함수 수정
  - datatransform.cs 데이터 전송 로직 개선


4. **uc_datatransform.cs 수정**:
  - sqlite -> mongodb 대체 및 관련 함수 수정
  - clustering.cs 데이터 전송 로직 개선

### 진행 중인 작업

1. **UI 컴포넌트 MongoDB 연동**:
   - `uc_Preprocessing.cs` MongoDB 연동
   - `uc_Classification.cs` MongoDB 연동
   - `uc_Clustering.cs` MongoDB 연동

### 남은 작업

1. **DataHandler.cs 완전 대체**:
   - 모든 SQLite 참조 제거
   - MongoDB 기반 데이터 처리 로직으로 교체

2. **전체 애플리케이션 테스트**:
   - 모든 기능 테스트
   - 성능 검증

3. **SQLite 코드 제거**:
   - 불필요한 참조 제거

## 데이터 구조

### 1. raw_data 컬렉션
```json
{
  "_id": ObjectId("..."),
  "data": {
    "column1": "value1",
    "column2": "value2",
    "dynamicColumn1": "dynamicValue1"
  },
  "import_date": ISODate("2025-05-05T10:30:00Z"),
  "is_hidden": false
}
```

### 2. process_data 컬렉션
```json
{
  "_id": ObjectId("..."),
  "raw_data_id": ObjectId("..."),
  "data": {
    "selectedCol1": "value1",
    "selectedCol2": "value2"
  },
  "import_date": ISODate("2025-05-05T10:30:00Z"),
  "processed_date": ISODate("2025-05-05T11:15:00Z"),
  "cluster_id": 5,
  "cluster_name": "클러스터_5"
}
```

### 3. column_mapping  컬렉션
```json
{
  "_id": ObjectId("..."),
  "original_name": "column1",
  "display_name": "사용자 정의 이름",
  "data_type": "text",
  "is_visible": true,
  "sequence": 1
}
```

### 4. clustering_results  컬렉션
```json
{
  "_id": ObjectId("..."),
  "process_data_id": ObjectId("..."),
  "cluster_id": 3,
  "cluster_name": "클러스터_3",
  "cluster_center": {
    "feature1": 0.75,
    "feature2": 0.25
  },
  "distance_to_center": 0.15,
  "silhouette_score": 0.82,
  "created_date": ISODate("2025-05-14T14:30:00Z"),
  "model_parameters": {
    "algorithm": "kmeans",
    "n_clusters": 5,
    "random_state": 42
  }
}
```

### 5. process_view_data  컬렉션
```json
{
  "_id": ObjectId("..."),
  "process_data_id": ObjectId("..."),
  "raw_data_id": ObjectId("..."),
  "keywords": {
    "final_keywords": ["키워드1", "키워드3"]
  },
  "money": <금액 데이터>,
  "last_modified_date": ISODate("2025-05-14T12:45:00Z")
}
```
## 주요 변경 사항

### 데이터 숨김 처리
- SQLite의 `hidden_rows` 테이블 대신 `RawDataDocument` 클래스의 `is_hidden` 필드 사용
- 행 숨김/복원 시 해당 문서의 `is_hidden` 속성만 업데이트
- UI에서도 `is_hidden` 값에 따라 회색 처리

### 컬럼 가시성
- `column_mapping` 컬렉션에 `is_visible` 필드 사용
- 컬럼 표시/숨김 시 해당 필드만 업데이트
- UI 콤보박스에는 가시적인 컬럼만 표시

### 비동기 패턴
- 모든 데이터 접근 코드를 비동기(async/await)로 전환
- UI 응답성 향상 및 대용량 데이터 처리 최적화

## 업데이트 내역

### 2025-05-12 (1차 업데이트)
- `uc_FileLoad.cs` 파일에서 데이터 숨김 처리 로직을 MongoDB 기반으로 수정
- `is_hidden` 필드를 활용한 데이터 필터링 구현
- UI에서 숨겨진 데이터 회색 처리 적용
- `delete_data_btn_Click` 및 `restore_del_data_btn_Click` 메서드 MongoDB 버전으로 리팩터링
- `MongoDataConverter.GetPagedRawDataAsync` 메서드의 파라미터 의미 명확화

### 2025-05-12 (2차 업데이트)

#### 문제 해결
- `uc_FileLoad.cs` 파일의 데이터 처리에서 MongoDB ID 타입 불일치 문제 해결
- `raw_data_id` 필드 타입 불일치로 인한 오류 수정 (string 타입으로 일관되게 처리)
- `DataHandler.ExtractColumnToNewTable` 메서드에서 `raw_data_id` 컬럼 타입을 decimal에서 string으로 변경
- `CreateDataTableFromColumnNamesAsync` 메서드 추가로 컬럼 인덱스 기반에서 컬럼명 기반 접근 방식으로 전환
- MongoDB ObjectId 처리 방식 개선 및 일관성 유지

#### 코드 개선
- MongoDB 문서와 .NET DataTable 간 데이터 변환 로직 최적화
- `ConvertProcessDocumentsToDataTable` 메서드의 불필요한 컬럼 제거 및 간소화
- `uc_preprocessing.cs`의 `initUI` 메서드에서 컬럼 매핑 문제 해결
- 컬럼 인덱스 대신 컬럼명을 기준으로 하는 더 안정적인 접근 방식으로 전환
- MongoDB ID 필드 처리 방식을 문자열 기반으로 통일하여 타입 불일치 문제 해결

#### 수정된 메서드 목록
1. `DataHandler.ExtractColumnToNewTable` - 타입 불일치 문제 해결
2. `DataHandler.CreateDataTableFromColumnNamesAsync` - 컬럼명 기반 새 메서드 추가
3. `DataHandler.ConvertProcessDocumentsToDataTable` - 간소화 및 최적화
4. `MongoDataConverter.PrepareProcessDataAsync` - ID 처리 방식 개선



## 2025-05-14 (3차 업데이트) - Preprocessing 모듈 병렬 처리 최적화 계획

uc_preprocessing.cs 파일의 데이터 처리 함수들에 대한 병렬 처리 및 MongoDB 마이그레이션 계획을 수립했습니다. 주요 목표는 대용량 데이터(80MB+) 처리 시 발생하는 성능 문제와 데이터 누락 현상을 해결하는 것입니다.

#### 데이터 누락 문제 해결
- MongoDB 삽입 작업 중 발생하는 데이터 누락 문제를 재시도 로직 구현으로 해결
- 병렬 처리에서의 자원 경합 문제를 적절한 동시성 제한으로 해결
- 실패 로깅 메커니즘 추가로 문제 추적 개선

## 2025-05-15 업데이트
1.keyword_seper_split_Click 함수의 병렬 처리 적용 및 progressbar 추가 
2.remove_1key_Click 함수의 병렬 처리 적용 및 progressbar 추가 
3.keyword_model_split_Click함수의 병렬 처리 적용 및 progressbar 추가
 -  특히 여기는 python 프로그램을 호출하는 구간이라서 병렬 처리가 적용이 되어 있지만 현재 리소스(cpu,ram)을 최대한 활용하는 방법으로 더 개선
4.btn_complete_Click 함수의 병렬처리 
  -  기존 sqlite에서 process_view_data 데이터에 저장하던걸 collection 을 신규로 생성하여 nosql에 저장 
  - 관련된 클래스 및 래포지토리 소스 개발이 필요함 > 신규 생성된 collection 에 데이터 insert 시 raw_data,process_data insert 방식과 동일한 구성으로 병렬 처리 
5.userControlHandler.uc_dataTransform.initUI(); 에서 기존sqlite 조회 로직을 collection 조회 기능으로 변경해야 함 
  - 또한 이후 데이터 처리도 변경된 항목에 맞게 마이그레이션 필요.

#### 병렬 처리 개선 대상 함수

1. **btn_apply_Click**
   - 사용자 정의 필터 적용 로직을 병렬 처리로 전환
   - MongoDB 업데이트 연산을 배치 처리로 구현
   - 진행 상황 표시 기능 추가

2. **remove_apply_btn_Click**
   - 필터 제거 작업을 병렬 처리로 최적화
   - 진행 상황 표시 기능 추가

3. **keyword_model_split_Click**
   - Python 프로세스 호출 부분의 병렬 처리 최적화
   - 현재 시스템 리소스(CPU, RAM)를 최대한 활용하도록 작업 분할
   - 멀티프로세싱을 통한 Python 스크립트 성능 향상
   - 데이터 전송 오버헤드 최소화

4. **btn_complete_Click**
   - `process_view_data` 데이터를 저장할 새 MongoDB 컬렉션 설계
   - 새 컬렉션을 위한 모델 클래스 및 저장소 구현
   - 병렬 삽입 처리 구현 (raw_data, process_data와 동일한 방식)

5. **userControlHandler.uc_dataTransform.initUI**
   - SQLite 조회 로직을 MongoDB 쿼리로 대체
   - 이후 데이터 처리 로직을 MongoDB 구조에 맞게 조정

## 병렬 처리 구현 전략

대용량 데이터 처리를 위한 병렬 처리 구현 전략을 다음과 같이 수립했습니다. 이 전략은 특히 80MB 이상의 대용량 데이터 처리 시 성능과 안정성을 확보하기 위한 방안입니다.

### 1. 적응형 작업 분할

데이터 크기와 시스템 리소스에 따라 최적의 작업 분할 전략을 적용합니다.

csharp
// 데이터 크기에 따른 적응형 배치 크기 설정
private int DetermineBatchSize(int totalItems)
{
    // 작은 데이터셋 (10만 건 이하)
    if (totalItems < 100000)
        return 10000;
    // 중간 데이터셋 (10만~50만 건)
    else if (totalItems < 500000)
        return 5000;
    // 대용량 데이터셋 (50만 건 이상)
    else
        return 2000;
}

// 배치로 데이터 분할
var batches = documents
    .Select((doc, index) => new { doc, index })
    .GroupBy(x => x.index / batchSize)
    .Select(g => g.Select(x => x.doc).ToList())
    .ToList();

    
## 2025-05-16 작업내용
1.preprocessing -> datatransfom 데이터 이동시 로직 개선 
2.datatransform 병합 로직 개선
3.키워드 변환 테스트 중
4.clustering 병합 결과 확인 테스트 필요
5.clustering 페이지 데이터 전송 구간 개선 필요

## 2025-05-19 작업 내용
1.datatransform 키워드 변환 로직 점검
2.clustering 병합 결과 페이지 개성
3.clustering 페이지 데이터 전송 구간 개선 -> 신규 로직 적용

  