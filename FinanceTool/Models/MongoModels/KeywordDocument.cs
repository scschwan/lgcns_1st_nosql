using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceTool.Models.MongoModels
{
    /// <summary>
    /// 키워드 정보를 저장하는 keywords 컬렉션의 MongoDB 문서 모델
    /// </summary>
    /// <remarks>
    /// 데이터 분석 및 클러스터링 과정에서 추출된 키워드 정보를 관리하는 문서 모델입니다.
    /// 키워드의 빈도, 출처 컬럼, 관련 키워드, 가중치 등의 정보를 저장하여
    /// 텍스트 분석, 검색 최적화, 클러스터링 성능 향상에 활용됩니다.
    /// 
    /// 주요 기능:
    /// - 키워드별 출현 빈도 추적
    /// - 키워드 출처 컬럼 정보 관리
    /// - 관련 키워드 네트워크 구축
    /// - 키워드 가중치 기반 우선순위 설정
    /// - 추천 키워드 플래그 관리
    /// </remarks>
    public class KeywordDocument
    {
        /// <summary>
        /// MongoDB 문서의 고유 식별자
        /// </summary>
        /// <remarks>
        /// MongoDB에서 자동 생성되는 ObjectId를 문자열로 저장합니다.
        /// 키워드 문서의 내부 식별자로, 데이터베이스 인덱스와 조인 작업에 사용됩니다.
        /// BsonId 어트리뷰트로 인해 MongoDB의 _id 필드와 매핑됩니다.
        /// </remarks>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        /// <summary>
        /// 추출된 키워드 텍스트
        /// </summary>
        /// <remarks>
        /// 원시 데이터에서 추출된 실제 키워드 문자열입니다.
        /// 클러스터링, 검색, 분석 작업의 기본 단위가 되는 필드입니다.
        /// 대소문자, 공백, 특수문자 등이 정규화되어 저장될 수 있습니다.
        /// 예: "결제", "이체", "ATM", "카드" 등
        /// MongoDB에서는 "keyword" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("keyword")]
        public string Keyword { get; set; }

        /// <summary>
        /// 키워드의 출현 빈도수
        /// </summary>
        /// <remarks>
        /// 전체 데이터셋에서 해당 키워드가 나타나는 총 횟수를 나타냅니다.
        /// 키워드의 중요도와 가중치 계산에 핵심적인 지표로 사용됩니다.
        /// 클러스터링 알고리즘에서 키워드의 우선순위를 결정하는 데 활용됩니다.
        /// 값이 클수록 더 중요한 키워드로 간주됩니다.
        /// MongoDB에서는 "count" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("count")]
        public int Count { get; set; }

        /// <summary>
        /// 키워드가 추출된 원본 데이터 컬럼명 목록
        /// </summary>
        /// <remarks>
        /// 해당 키워드가 발견된 Excel 컬럼들의 이름을 저장하는 리스트입니다.
        /// 키워드의 컨텍스트와 출처를 추적하여 더 정확한 분석을 가능하게 합니다.
        /// 예: ["Description", "Memo", "거래내용"] 등
        /// 키워드 기반 검색이나 필터링 시 특정 컬럼에서만 검색하는 데 활용됩니다.
        /// MongoDB에서는 "source_columns" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("source_columns")]
        public List<string> SourceColumns { get; set; } = new List<string>();

        /// <summary>
        /// 현재 키워드와 관련된 키워드들의 목록
        /// </summary>
        /// <remarks>
        /// 동일한 문서나 클러스터에서 함께 나타나는 연관 키워드들을 저장합니다.
        /// 키워드 네트워크 분석, 추천 시스템, 의미론적 검색에 활용됩니다.
        /// 예: "ATM" 키워드의 관련 키워드로 ["출금", "현금", "은행"] 등
        /// 머신러닝 기반 키워드 클러스터링이나 자연어 처리에서 활용될 수 있습니다.
        /// MongoDB에서는 "related_keywords" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("related_keywords")]
        public List<string> RelatedKeywords { get; set; } = new List<string>();

        /// <summary>
        /// 키워드가 포함된 raw_data 문서들의 ID 목록
        /// </summary>
        /// <remarks>
        /// 해당 키워드가 발견된 모든 원시 데이터 문서들의 MongoDB ObjectId 목록입니다.
        /// 키워드에서 실제 데이터로의 역추적이 가능하여 상세 분석에 활용됩니다.
        /// 키워드 기반 문서 검색, 클러스터 구성원 조회 등에 사용됩니다.
        /// Count 속성값과 이 리스트의 요소 개수는 일치해야 합니다.
        /// MongoDB에서는 "document_ids" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("document_ids")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> DocumentIds { get; set; } = new List<string>();

        /// <summary>
        /// 키워드 정보가 마지막으로 업데이트된 날짜 및 시간
        /// </summary>
        /// <remarks>
        /// 키워드 데이터의 최종 수정 시점을 기록합니다.
        /// 기본값은 현재 시스템 시간(DateTime.Now)으로 설정됩니다.
        /// 데이터 동기화, 캐시 무효화, 증분 업데이트 등에 활용됩니다.
        /// 키워드 분석 결과의 신뢰성과 최신성을 판단하는 지표로 사용됩니다.
        /// MongoDB에서는 "last_updated" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("last_updated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        /// <summary>
        /// 키워드의 중요도 가중치
        /// </summary>
        /// <remarks>
        /// 키워드의 상대적 중요도를 나타내는 부동소수점 값입니다.
        /// 기본값은 1.0이며, 더 중요한 키워드일수록 높은 값을 가집니다.
        /// TF-IDF, 빈도수, 사용자 피드백 등을 종합하여 계산됩니다.
        /// 클러스터링 알고리즘, 검색 랭킹, 추천 시스템에서 활용됩니다.
        /// 0.0~10.0 범위의 값을 권장하며, 음수는 허용되지 않습니다.
        /// MongoDB에서는 "weight" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("weight")]
        public double Weight { get; set; } = 1.0;

        /// <summary>
        /// 추천 키워드 여부를 나타내는 플래그
        /// </summary>
        /// <remarks>
        /// true일 경우 시스템에서 사용자에게 추천할 만한 중요 키워드로 표시됩니다.
        /// 기본값은 false(일반 키워드)로 설정됩니다.
        /// 높은 빈도수, 높은 가중치, 사용자 행동 패턴 등을 기반으로 결정됩니다.
        /// UI에서 키워드 제안, 자동 완성, 인기 검색어 등에 활용됩니다.
        /// 사용자 경험 개선과 검색 효율성 향상에 기여합니다.
        /// MongoDB에서는 "is_recommendation" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("is_recommendation")]
        public bool IsRecommendation { get; set; } = false;
    }
}