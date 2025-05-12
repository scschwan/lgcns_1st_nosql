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

        [BsonElement("cluster_id")]
        public int ClusterId { get; set; }

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
    }
}