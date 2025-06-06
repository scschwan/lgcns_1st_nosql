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
        [BsonElement("account_name")]
        public string AccountName { get; set; }

        [BsonElement("file_ids")]
        public List<ObjectId> FileIds { get; set; } = new List<ObjectId>();

        /// <summary>
        /// 파일 개수 (계산된 값)
        /// </summary>
        [BsonElement("file_count")]
        [BsonIgnoreIfNull]
        public int? FileCount { get; set; }

        /// <summary>
        /// 업데이트 날짜 (병합 시 등)
        /// </summary>
        [BsonElement("updated_date")]
        [BsonIgnoreIfNull]
        public DateTime? UpdatedDate { get; set; }
    }
}