using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceTool.MongoModels
{
    public class UploadedFileDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("original_filename")]
        public string OriginalFilename { get; set; }

        [BsonElement("stored_filename")]
        public string StoredFilename { get; set; }

        [BsonElement("file_path")]
        public string FilePath { get; set; }

        [BsonElement("file_size")]
        public long FileSize { get; set; }

        [BsonElement("upload_date")]
        public DateTime UploadDate { get; set; }

        [BsonElement("account_column_name")]
        public string AccountColumnName { get; set; }

        [BsonElement("amount_column_name")]
        public string AmountColumnName { get; set; }

        [BsonElement("detected_columns")]
        public List<string> DetectedColumns { get; set; } = new List<string>();

        [BsonElement("total_rows")]
        public decimal TotalRows { get; set; }

        [BsonElement("total_amount")]
        public decimal TotalAmount { get; set; }

        [BsonElement("session_id")]
        public ObjectId? SessionId { get; set; }

        [BsonElement("processing_status")]
        public string ProcessingStatus { get; set; } // uploaded, processed, error

        // *** 새로 추가된 필드들 ***
        [BsonElement("account_contents")]
        public List<string> AccountContents { get; set; } = new List<string>(); // 계정명 컬럼의 고유값들


    }
}