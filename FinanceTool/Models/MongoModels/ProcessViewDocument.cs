using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceTool.MongoModels
{
    /// <summary>
    /// process_view_data 컬렉션의 문서 구조 정의
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

        // 키워드 관련 정보
        [BsonElement("keywords")]
        public KeywordInfo Keywords { get; set; } = new KeywordInfo();

        // 처리 정보
        [BsonElement("processing_info")]
        public ProcessingInfo ProcessingInfo { get; set; } = new ProcessingInfo();

        // 처리 메타데이터
        [BsonElement("processed_date")]
        public DateTime ProcessedDate { get; set; } = DateTime.Now;

        // 사용자 수정 여부
        [BsonElement("user_modified")]
        public bool UserModified { get; set; } = false;

        // 마지막 수정 일자
        [BsonElement("last_modified_date")]
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 키워드 정보 하위 문서
    /// </summary>
    public class KeywordInfo
    {
        [BsonElement("original_text")]
        public string OriginalText { get; set; }

        [BsonElement("extracted_keywords")]
        public List<string> ExtractedKeywords { get; set; } = new List<string>();

        [BsonElement("removed_keywords")]
        public List<string> RemovedKeywords { get; set; } = new List<string>();

        [BsonElement("final_keywords")]
        public List<string> FinalKeywords { get; set; } = new List<string>();
    }

    /// <summary>
    /// 처리 정보 하위 문서
    /// </summary>
    public class ProcessingInfo
    {
        // separator, model 등 처리 방식
        [BsonElement("processing_type")]
        public string ProcessingType { get; set; }

        // 구분자 처리 시 사용된 구분자
        [BsonElement("separator")]
        public string Separator { get; set; }

        // 모델 처리 시 사용된 모델
        [BsonElement("model_name")]
        public string ModelName { get; set; }
    }
}