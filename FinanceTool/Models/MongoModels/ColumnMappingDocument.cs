using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceTool.MongoModels
{
    /// <summary>
    /// column_mapping 컬렉션의 문서 구조 정의
    /// </summary>
    public class ColumnMappingDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("original_name")]
        public string OriginalName { get; set; }

        [BsonElement("display_name")]
        public string DisplayName { get; set; }

        [BsonElement("data_type")]
        public string DataType { get; set; }

        [BsonElement("is_visible")]
        public bool IsVisible { get; set; } = true;

        [BsonElement("sequence")]
        public int Sequence { get; set; }
    }
}