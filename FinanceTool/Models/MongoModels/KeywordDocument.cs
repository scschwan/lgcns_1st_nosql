using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceTool.Models.MongoModels
{
    /// <summary>
    /// 키워드 컬렉션의 문서 구조 정의
    /// </summary>
    public class KeywordDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("keyword")]
        public string Keyword { get; set; }

        [BsonElement("count")]
        public int Count { get; set; }

        [BsonElement("source_columns")]
        public List<string> SourceColumns { get; set; } = new List<string>();

        [BsonElement("related_keywords")]
        public List<string> RelatedKeywords { get; set; } = new List<string>();

        [BsonElement("document_ids")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> DocumentIds { get; set; } = new List<string>();

        [BsonElement("last_updated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [BsonElement("weight")]
        public double Weight { get; set; } = 1.0;

        [BsonElement("is_recommendation")]
        public bool IsRecommendation { get; set; } = false;
    }
}