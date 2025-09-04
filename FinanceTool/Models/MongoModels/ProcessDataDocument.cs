using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceTool.MongoModels
{
    /// <summary>
    /// process_data 컬렉션의 가공된 데이터 문서 모델
    /// </summary>
    /// <remarks>
    /// raw_data에서 선택되고 처리된 데이터를 저장하는 MongoDB 문서 모델입니다.
    /// 원시 데이터에서 필요한 필드만 추출하고 정제하여 저장합니다.
    /// 클러스터링 처리 결과와 연결되어 분석된 데이터의 추가 정보를 제공합니다.
    /// raw_data와 1:1 참조 관계를 유지하여 데이터 일관성을 보장합니다.
    /// </remarks>
    public class ProcessDataDocument
    {
        /// <summary>MongoDB 문서의 고유 식별자</summary>
        /// <remarks>MongoDB에서 자동 생성되는 ObjectId로 가공 데이터 문서를 식별합니다.</remarks>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        /// <summary>원시 데이터 문서의 ID 참조</summary>
        /// <remarks>이 가공 데이터가 파생된 원시 raw_data 문서의 ObjectId를 저장하여 추적 가능하도록 합니다.</remarks>
        [BsonElement("raw_data_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string RawDataId { get; set; }

        /// <summary>선택되고 정제된 데이터 필드들</summary>
        /// <remarks>raw_data에서 필요한 필드만 추출하고 정제하여 저장한 Dictionary입니다.</remarks>
        [BsonElement("data")]
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        /// <summary>원시 데이터 가져오기 날짜</summary>
        /// <remarks>raw_data에서 복사된 원시 ImportDate 값으로 데이터 이력을 추적합니다.</remarks>
        [BsonElement("import_date")]
        public DateTime ImportDate { get; set; }

        /// <summary>데이터 가공 완료 날짜</summary>
        /// <remarks>raw_data에서 process_data로 바로 언제 가공되었는지를 기록합니다.</remarks>
        [BsonElement("processed_date")]
        public DateTime ProcessedDate { get; set; } = DateTime.Now;

        /// <summary>할당된 클러스터 ID (선택적)</summary>
        /// <remarks>클러스터링 처리 후 이 데이터가 속한 클러스터의 ID를 저장합니다. null일 수 있습니다.</remarks>
        [BsonElement("cluster_id")]
        public int? ClusterId { get; set; }

        /// <summary>할당된 클러스터명 (선택적)</summary>
        /// <remarks>클러스터링 처리 후 이 데이터가 속한 클러스터의 이름을 저장합니다. null일 수 있습니다.</remarks>
        [BsonElement("cluster_name")]
        public string ClusterName { get; set; }
    }
}