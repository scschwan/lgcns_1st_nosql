using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceTool.MongoModels
{
    /// <summary>
    /// raw_data 컬렉션의 문서 구조 정의
    /// </summary>
    public class RawDataDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // 원본 엑셀 데이터를 동적으로 저장하는 Dictionary
        [BsonElement("data")]
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        // 메타데이터
        [BsonElement("import_date")]
        public DateTime ImportDate { get; set; } = DateTime.Now;

        // 추가 메타데이터 (필요시)
        [BsonElement("file_name")]
        public string FileName { get; set; }

        [BsonElement("is_hidden")]
        public bool IsHidden { get; set; } = false;

        [BsonElement("hidden_reason")]
        public string HiddenReason { get; set; }

        // MongoDB는 동적 필드를 지원하므로, 필요할 때 속성을 추가하거나
        // Data 딕셔너리를 사용하여 임의의 필드를 저장할 수 있습니다.
    }
}