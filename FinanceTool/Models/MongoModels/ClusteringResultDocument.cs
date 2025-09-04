using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceTool.MongoModels
{
    /// <summary>
    /// 클러스터링 결과를 저장하는 clustering_results 컬렉션의 MongoDB 문서 모델
    /// </summary>
    /// <remarks>
    /// 원시 데이터에 대한 클러스터링 분석 결과를 저장하는 모델입니다.
    /// 계층적 클러스터 구조 지원을 위해 다양한 ID 체계를 제공합니다:
    /// - 일반 클러스터: 기본 클러스터링 결과
    /// - 병합 클러스터: 여러 클러스터를 통합한 결과
    /// - 세부 클러스터: 상위 클러스터를 세분화한 결과
    /// 각 클러스터는 키워드, 문서 수, 총 금액 등의 요약 정보를 포함합니다.
    /// </remarks>
    public class ClusteringResultDocument
    {
        /// <summary>
        /// MongoDB 문서의 고유 식별자
        /// </summary>
        /// <remarks>
        /// MongoDB에서 자동 생성되는 ObjectId를 문자열로 저장합니다.
        /// 클러스터링 결과 문서의 내부 식별자로, ClusterNumber와 별개로 관리됩니다.
        /// 데이터베이스 색인, 조인, 연결 작업에 사용됩니다.
        /// </remarks>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        /// <summary>
        /// 정수 기반 클러스터 번호 (기존 ID 체계와 호환)
        /// </summary>
        [BsonElement("cluster_number")]
        public int ClusterNumber { get; set; }

        /// <summary>
        /// 소속된 클러스터 ID (-1: 미병합, 양수: 병합된 클러스터의 클러스터 번호)
        /// </summary>
        [BsonElement("cluster_id")]
        public int ClusterId { get; set; }

        /// <summary>
        /// 세부 클러스터 ID (신규 추가)
        /// -1: 세부 클러스터링 미진행
        /// cluster_number와 같음: 세부 상위 클러스터
        /// 다른 값: 해당 세부 클러스터에 병합됨
        /// </summary>
        [BsonElement("cluster_sub_id")]
        public int ClusterSubId { get; set; } = -1;

        /// <summary>
        /// 클러스터의 사용자 정의 이름
        /// </summary>
        /// <remarks>
        /// 사용자가 클러스터를 식별하기 위해 지정한 이름입니다.
        /// 자동 생성되거나 사용자가 수동으로 편집할 수 있습니다.
        /// UI에서 클러스터를 구별하고 관리하는 데 사용됩니다.
        /// MongoDB에서는 "cluster_name" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("cluster_name")]
        public string ClusterName { get; set; }

        /// <summary>
        /// 클러스터의 대표 키워드 목록
        /// </summary>
        /// <remarks>
        /// 클러스터에 속한 데이터들의 공통 키워드나 특징을 나타내는 문자열 목록입니다.
        /// 클러스터링 알고리즘에 의해 자동 추출되거나 사용자가 수동으로 설정할 수 있습니다.
        /// 클러스터의 특성을 이해하고 데이터를 분류하는 데 활용됩니다.
        /// MongoDB에서는 "keywords" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("keywords")]
        public List<string> Keywords { get; set; } = new List<string>();

        /// <summary>
        /// 클러스터에 속한 데이터 문서의 총 개수
        /// </summary>
        /// <remarks>
        /// 해당 클러스터에 분류된 raw_data 문서들의 총 개수를 나타냅니다.
        /// 클러스터의 크기를 파악하고 비중을 계산하는 데 사용됩니다.
        /// DataIndices 리스트의 요소 개수와 일치해야 합니다.
        /// MongoDB에서는 "count" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("count")]
        public int Count { get; set; }

        /// <summary>
        /// 클러스터에 속한 데이터들의 총 금액
        /// </summary>
        /// <remarks>
        /// 해당 클러스터에 분류된 모든 거래 데이터의 금액을 합산한 값입니다.
        /// 재무 데이터 분석에서 클러스터별 영향도를 파악하는 데 중요한 지표입니다.
        /// decimal 타입을 사용하여 금액 계산의 정확성을 보장합니다.
        /// MongoDB에서는 "total_amount" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("total_amount")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 클러스터에 속한 raw_data 문서들의 ID 목록
        /// </summary>
        /// <remarks>
        /// 해당 클러스터에 속한 모든 원시 데이터 문서들의 MongoDB ObjectId 목록입니다.
        /// 이 목록을 통해 클러스터에 속한 구체적인 데이터들을 조회할 수 있습니다.
        /// 클러스터링 결과와 원시 데이터 사이의 참조 무결성을 유지하는 데 중요합니다.
        /// MongoDB에서는 "data_indices" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("data_indices")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> DataIndices { get; set; } = new List<string>();

        /// <summary>
        /// 클러스터 생성 날짜 및 시간
        /// </summary>
        /// <remarks>
        /// 해당 클러스터가 생성된 정확한 시점을 기록합니다.
        /// 기본값은 현재 시스템 시간(DateTime.Now)으로 설정됩니다.
        /// 클러스터링 이력 추적, 시간 순 정렬, 감사 로그 등에 활용됩니다.
        /// MongoDB에서는 "created_at" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 일반 클러스터 여부 (cluster_sub_id = -1)
        /// </summary>
        [BsonIgnore]
        public bool IsNormalCluster => ClusterSubId == -1;

        /// <summary>
        /// 세부 상위 클러스터 여부 (cluster_sub_id = cluster_number)
        /// </summary>
        [BsonIgnore]
        public bool IsDetailParentCluster => ClusterSubId > 0 && ClusterSubId == ClusterNumber;

        /// <summary>
        /// 세부 하위 클러스터 여부 (cluster_sub_id > 0 && cluster_sub_id != cluster_number)
        /// </summary>
        [BsonIgnore]
        public bool IsDetailChildCluster => ClusterSubId > 0 && ClusterSubId != ClusterNumber;

        /// <summary>
        /// 병합된 상위 클러스터 여부 (cluster_id = cluster_number)
        /// </summary>
        [BsonIgnore]
        public bool IsMergedParentCluster => ClusterId > 0 && ClusterId == ClusterNumber;

        /// <summary>
        /// 병합된 하위 클러스터 여부 (cluster_id > 0 && cluster_id != cluster_number)
        /// </summary>
        [BsonIgnore]
        public bool IsMergedChildCluster => ClusterId > 0 && ClusterId != ClusterNumber;

        /// <summary>
        /// 독립 클러스터 여부 (cluster_id = -1)
        /// </summary>
        [BsonIgnore]
        public bool IsIndependentCluster => ClusterId == -1;

        /// <summary>
        /// 이 문서가 병합된 클러스터인지 여부
        /// </summary>
        [BsonIgnore]
        public bool IsMergedCluster
        {
            get { return ClusterId > 0 && ClusterId == ClusterNumber; }
        }
    }
}