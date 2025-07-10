using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceTool.MongoModels
{
    /// <summary>
    /// 클러스터링 결과를 저장하는 컬렉션의 문서 구조 정의
    /// </summary>
    public class ClusteringResultDocument
    {
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

        [BsonElement("cluster_name")]
        public string ClusterName { get; set; }

        [BsonElement("keywords")]
        public List<string> Keywords { get; set; } = new List<string>();

        [BsonElement("count")]
        public int Count { get; set; }

        [BsonElement("total_amount")]
        public decimal TotalAmount { get; set; }

        // raw_data 문서의 ID 목록 (클러스터 멤버)
        [BsonElement("data_indices")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> DataIndices { get; set; } = new List<string>();

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