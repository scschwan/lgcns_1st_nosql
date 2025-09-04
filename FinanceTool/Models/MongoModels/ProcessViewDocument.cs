using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceTool.MongoModels
{
    /// <summary>
    /// 가공된 데이터의 보기 층 정보를 저장하는 process_view_data 컬렉션의 MongoDB 문서 모델
    /// </summary>
    /// <remarks>
    /// process_data에서 추출한 핵심 정보를 UI 설정에 최적화된 형태로 저장하는 문서 모델입니다.
    /// 클러스터링 결과에서 나온 키워드 정보, 금액 데이터, 부서 정보 등을
    /// 통합적으로 관리하여 효율적인 데이터 조회와 분석을 지원합니다.
    /// 
    /// 주요 기능:
    /// - process_data와 raw_data 양방향 참조
    /// - 최종 키워드 목록 및 분류 정보 관리
    /// - 금액 데이터 표준화 및 저장
    /// - 부서/공급업체 정보 관리
    /// - UI 렌더링 성능 최적화
    /// </remarks>
    public class ProcessViewDocument
    {
        /// <summary>
        /// MongoDB 문서의 고유 식별자
        /// </summary>
        /// <remarks>
        /// MongoDB에서 자동 생성되는 ObjectId를 문자열로 저장합니다.
        /// process_view_data 문서의 내부 식별자로, 데이터베이스 인덱스와 조인 작업에 사용됩니다.
        /// BsonId 어트리뷰트로 인해 MongoDB의 _id 필드와 매핑됩니다.
        /// </remarks>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        /// <summary>
        /// 참조할 process_data 문서의 ID
        /// </summary>
        /// <remarks>
        /// 이 보기 데이터가 기반하는 process_data 문서의 ObjectId를 저장합니다.
        /// process_data와의 1:1 참조 관계를 만든시너 상세 가공 정보로의 역추적을 가능하게 합니다.
        /// 내부 연결성과 데이터 무결성을 담보하는 필수 요소입니다.
        /// MongoDB에서는 "process_data_id" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("process_data_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ProcessDataId { get; set; }

        /// <summary>
        /// 원시 데이터 문서의 ID 참조 (신규 추가 필드)
        /// </summary>
        /// <remarks>
        /// 이 보기 데이터의 원본 소스인 raw_data 문서의 ObjectId를 저장합니다.
        /// process_data를 거치지 않고 직접 원시 데이터로의 추적이 가능합니다.
        /// 빠른 데이터 조회, 원드립 수정, 이력 추적 등에 활용되는 개선된 기능입니다.
        /// 데이터 체인의 완전성과 추적성을 보장합니다.
        /// MongoDB에서는 "raw_data_id" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("raw_data_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string RawDataId { get; set; }

        /// <summary>
        /// 클러스터링 및 분석에서 추출한 키워드 정보
        /// </summary>
        /// <remarks>
        /// 가공된 데이터에서 최종적으로 확정된 키워드 정보를 저장하는 복합 객체입니다.
        /// KeywordInfo 클래스를 통해 최종 키워드 목록과 관련 메타데이터를 관리합니다.
        /// UI에서 키워드 기반 검색, 필터링, 분류에 직접 사용됩니다.
        /// 기본값으로 빈 KeywordInfo 인스턴스를 생성합니다.
        /// MongoDB에서는 "keywords" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("keywords")]
        public KeywordInfo Keywords { get; set; } = new KeywordInfo();

        /// <summary>
        /// 거래 금액 정보
        /// </summary>
        /// <remarks>
        /// 거래에 관련된 금액 데이터를 유연한 형태로 저장합니다.
        /// object 타입을 사용하여 decimal, double, string 등 다양한 형식의 금액을 지원합니다.
        /// 화폐 단위, 부호, 소수점 처리 등이 포함된 원본 데이터를 보존할 수 있습니다.
        /// UI에서 금액 표시, 정렬, 집계 등에 사용됩니다.
        /// MongoDB에서는 "money" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("money")]
        public object Money { get; set; }

        /// <summary>
        /// 거래와 관련된 부서 정보 (신규 추가 필드)
        /// </summary>
        /// <remarks>
        /// 거래를 수행한 부서나 비용 발생 부서의 이름을 저장합니다.
        /// 거래 데이터에서 추출되거나 사용자가 직접 설정할 수 있습니다.
        /// 부서별 비용 분석, 예산 관리, 조직 단위 리포트 생성에 활용됩니다.
        /// null 값이 허용되며, 부서 정보가 없는 거래도 지원합니다.
        /// MongoDB에서는 "department" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("department")]
        public string Department { get; set; }

        /// <summary>
        /// 거래 상대방 공급업체 정보 (신규 추가 필드)
        /// </summary>
        /// <remarks>
        /// 거래에 관련된 공급업체, 판매업체, 수수료 지급 대상 등의 이름을 저장합니다.
        /// 거래 데이터에서 자동 추출되거나 사용자가 매뉴얼로 입력할 수 있습니다.
        /// 공급업체별 거래 내역 분석, 지출 패턴 파악, 공급망 관리에 활용됩니다.
        /// null 값이 허용되며, 공급업체 정보가 없는 내부 거래도 지원합니다.
        /// MongoDB에서는 "supplier" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("supplier")]
        public string Supplier { get; set; }

        /// <summary>
        /// 보기 데이터가 마지막으로 수정된 날짜 및 시간
        /// </summary>
        /// <remarks>
        /// 보기 데이터의 마지막 수정 시점을 기록합니다.
        /// 기본값은 현재 시스템 시간(DateTime.Now)으로 설정됩니다.
        /// 데이터 동기화, 증분 업데이트, 캐시 무효화에 활용됩니다.
        /// 사용자가 데이터를 수정할 때마다 자동으로 업데이트됩니다.
        /// 데이터 변경 이력과 신뢰성 추적을 위한 중요한 메타데이터입니다.
        /// MongoDB에서는 "last_modified_date" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("last_modified_date")]
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 키워드 정보를 관리하는 내장 복합 객체
    /// </summary>
    /// <remarks>
    /// ProcessViewDocument에 내장되어 키워드 관련 정보를 체계적으로 관리하는 클래스입니다.
    /// 현재는 최종 키워드 목록만 저장하지만, 향후 신뢰도, 점수,
    /// 공동 발생 빈도 등의 추가 메타데이터 확장이 가능합니다.
    /// MongoDB에서 BSON 내장 문서로 저장되어 빠른 조회와 인덱싱이 가능합니다.
    /// </remarks>
    public class KeywordInfo
    {
        /// <summary>
        /// 클러스터링 및 분석 과정에서 확정된 최종 키워드 목록
        /// </summary>
        /// <remarks>
        /// 데이터 분석, 텍스트 마이닝, 클러스터링 등의 과정을 거쳐 최종적으로 확정된 키워드들을 저장합니다.
        /// 기본값은 빈 리스트로 설정되며, 필수값은 아닙니다.
        /// UI에서 키워드 표시, 검색, 필터링에 직접 사용되는 핵심 데이터입니다.
        /// 예: ["ATM", "출금", "현금", "은행"] 등
        /// 중복을 허용하지 않으며, 대소문자를 구분합니다.
        /// MongoDB에서는 "final_keywords" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("final_keywords")]
        public List<string> FinalKeywords { get; set; } = new List<string>();
    }
}