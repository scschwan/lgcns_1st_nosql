using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceTool.MongoModels
{
    /// <summary>
    /// process_data 컬렉션의 문서 구조 정의
    /// </summary>
    public class ProcessDataDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // raw_data 문서의 ID 참조
        [BsonElement("raw_data_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string RawDataId { get; set; }

        // 선택된 데이터 필드
        [BsonElement("data")]
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        // 처리 메타데이터
        [BsonElement("import_date")]
        public DateTime ImportDate { get; set; }

        [BsonElement("processed_date")]
        public DateTime ProcessedDate { get; set; } = DateTime.Now;

        // 클러스터링 관련 필드 (옵션)
        [BsonElement("cluster_id")]
        public int? ClusterId { get; set; }

        [BsonElement("cluster_name")]
        public string ClusterName { get; set; }
    }
}