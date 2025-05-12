# MongoDB 마이그레이션 아키텍처 및 진행 상황

## 프로젝트 개요

SQLite 기반의 데이터 처리 애플리케이션을 MongoDB로 마이그레이션하는 프로젝트입니다. 주요 목적은 대용량 데이터 처리 성능 향상과 동적 컬럼 관리의 유연성 확보입니다.

## 폴더 구조

```
FinanceTool/
│
├── [기존 파일들 그대로 유지]  # 기존 SQLite 기반 코드
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

## 기존 코드 대체 계획

1. **완전히 삭제할 파일**:
   - `DBManager.cs` - MongoDB로 완전히 대체됨

2. **대폭 수정할 파일**:
   - `DataConverter.cs` - MongoDataConverter.cs로 대체
   - `DataHandler.cs` - MongoDB 저장소 패턴 사용하도록 수정 (진행 중)

3. **부분 수정할 파일**:
   - UI 컴포넌트 파일들 (uc_*.cs) - DataHandler 변경에 따른 연동 수정 필요

## 구현된 주요 컴포넌트

### 1. MongoDBManager.cs
- MongoDB 서버 연결 관리
- 컬렉션 생성 및 관리
- 기본 CRUD 작업 지원
- 데이터 영속성 플래그 지원 (SQLite 방식과 유사하게 필요시 초기화 가능)

### 2. MongoDB 문서 모델
- RawDataDocument: 원본 데이터 저장
- ProcessDataDocument: 가공 데이터 저장
- ClusteringResultDocument: 클러스터링 결과 저장
- 등 MongoDB 컬렉션과 매핑되는 클래스들

### 3. 리포지토리 패턴
- BaseRepository: 공통 CRUD 작업 추상화
- 특화 리포지토리: 각 컬렉션별 특수 기능 구현
  - RawDataRepository: 원본 데이터 관리
  - ProcessDataRepository: 가공 데이터 관리
  - ClusteringRepository: 클러스터링 결과 관리

## MongoDB 데이터 구조

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

### 3. clustering_results 컬렉션
```json
{
  "_id": ObjectId("..."),
  "cluster_id": 5,
  "cluster_name": "클러스터_5",
  "keywords": ["키워드1", "키워드2", "키워드3"],
  "count": 120,
  "total_amount": 15000000,
  "data_indices": [ObjectId("..."), ObjectId("..."), ...],
  "created_at": ISODate("2025-05-05T12:30:00Z")
}
```

## 주요 변경 사항

1. **데이터 접근 방식**:
   - SQLite 쿼리 → MongoDB 문서 쿼리로 변경
   - SQL 문 → MongoDB 필터 표현식으로 변경
   - DataTable 직접 사용 → MongoDB 문서 객체 사용 후 필요시 변환

2. **비동기 패턴 적용**:
   - 기존 동기 메서드 → 비동기 메서드(Task 기반)로 변경
   - UI 블로킹 방지

3. **데이터 흐름 변화**:
   - 엑셀 → MongoDB 직접 저장
   - 클러스터링 결과 → MongoDB 컬렉션에 저장

## 진행 상황

1. **완료된 작업**:
   - 코어 MongoDB 인프라 구축
   - 문서 모델 클래스 정의
   - 저장소 패턴 구현
   - 기본 CRUD 기능 구현

2. **진행 중인 작업**:
   - DataHandler.cs 수정 - MongoDB 접근 방식으로 변경

3. **남은 작업**:
   - UI 컴포넌트 수정 및 연동
   - 전체 애플리케이션 테스트

## 추가 참고 사항

1. **MongoDB 설정**:
   - `mongod.conf` 파일에서 성능 최적화 설정 적용
   - 초기 실행 시 데이터 리셋 여부 설정 가능 (`MongoDBManager.ResetDatabaseOnStartup`)

2. **DataTable 호환성**:
   - UI 연동을 위해 MongoDB 문서 ↔ DataTable 변환 기능 구현 필요
   - ClusteringRepository에 `ToDataTable()` 메서드 구현됨
