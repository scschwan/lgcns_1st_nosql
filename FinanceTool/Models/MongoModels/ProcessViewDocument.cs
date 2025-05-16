using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceTool.MongoModels
{
    /// <summary>
    /// process_view_data 컬렉션의 문서 구조 정의 (개선버전)
    /// </summary>
    public class ProcessViewDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // process_data 문서의 ID 참조
        [BsonElement("process_data_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ProcessDataId { get; set; }

        // raw_data 문서의 ID 참조 (신규)
        [BsonElement("raw_data_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string RawDataId { get; set; }

        // 키워드 관련 정보 (수정됨)
        [BsonElement("keywords")]
        public KeywordInfo Keywords { get; set; } = new KeywordInfo();

        // 금액 정보 (신규)
        [BsonElement("money")]
        public object Money { get; set; }

        // 마지막 수정 일자
        [BsonElement("last_modified_date")]
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 키워드 정보 하위 문서 (수정됨)
    /// </summary>
    public class KeywordInfo
    {
        [BsonElement("final_keywords")]
        public List<string> FinalKeywords { get; set; } = new List<string>();
    }
}