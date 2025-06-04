using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceTool.MongoModels
{
    public class FileSessionDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("session_name")]
        public string SessionName { get; set; }

        [BsonElement("account_column_name")]
        public string AccountColumnName { get; set; }

        [BsonElement("amount_column_name")]
        public string AmountColumnName { get; set; }

        [BsonElement("total_amount")]
        public decimal TotalAmount { get; set; }

        [BsonElement("total_rows")]
        public decimal TotalRows { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } // processing, completed, failed

        [BsonElement("created_date")]
        public DateTime CreatedDate { get; set; }

        [BsonElement("completed_date")]
        public DateTime? CompletedDate { get; set; }

        [BsonElement("result_file_path")]
        public string ResultFilePath { get; set; }

        [BsonElement("file_ids")]
        public List<ObjectId> FileIds { get; set; } = new List<ObjectId>();
    }
}