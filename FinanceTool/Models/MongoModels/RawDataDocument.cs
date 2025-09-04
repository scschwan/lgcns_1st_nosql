using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceTool.MongoModels
{
    /// <summary>
    /// raw_data 컬렉션의 문서 구조를 정의하는 MongoDB 모델 클래스
    /// </summary>
    /// <remarks>
    /// Excel 파일에서 가져온 원시 데이터를 MongoDB에 저장하기 위한 문서 모델입니다.
    /// 동적 필드 지원을 위해 Dictionary<string, object> 타입을 사용하며,
    /// 다양한 구조의 Excel 데이터를 유연하게 처리할 수 있습니다.
    /// 또한 데이터 가시성 설정, 날짜 추적, 파일 정보 관리 등의 기능을 제공합니다.
    /// </remarks>
    public class RawDataDocument
    {
        /// <summary>
        /// MongoDB 문서의 고유 식별자
        /// </summary>
        /// <remarks>
        /// MongoDB에서 자동 생성되는 ObjectId를 문자열로 저장합니다.
        /// BsonId 어트리뷐트로 인해 MongoDB의 _id 필드와 매핑됩니다.
        /// 문서의 각종 색인, 연결, 참조 작업에 사용됩니다.
        /// </remarks>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        /// <summary>
        /// Excel 파일에서 가져온 원시 데이터를 동적으로 저장하는 Dictionary
        /// </summary>
        /// <remarks>
        /// Excel의 컴럼 이름을 키로, 셀 값을 값으로 저장합니다.
        /// object 타입을 사용하여 문자열, 숫자, 날짜 등 다양한 데이터 타입을 지원합니다.
        /// 예: {"Transaction_Date": "2023-01-01", "Amount": 1000, "Description": "Sample"}
        /// MongoDB에서는 "data" 필드로 저장되며, 동적 스키마를 지원합니다.
        /// </remarks>
        [BsonElement("data")]
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 데이터 가져오기(가져오기) 날짜 및 시간
        /// </summary>
        /// <remarks>
        /// Excel 파일에서 데이터를 MongoDB로 가져온 정확한 시점을 기록합니다.
        /// 기본값은 당시 시스템 시간(DateTime.Now)으로 설정됩니다.
        /// 데이터 이력, 로그 추적, 시간 순 정렬 등에 활용됩니다.
        /// MongoDB에서는 "import_date" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("import_date")]
        public DateTime ImportDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 원본 Excel 파일의 이름
        /// </summary>
        /// <remarks>
        /// 데이터의 출처를 추적하기 위해 원본 Excel 파일의 이름을 저장합니다.
        /// 여러 파일에서 가져온 데이터를 구별하거나 데이터 검증 시 유용합니다.
        /// null 값이 허용되며, 선택적으로 설정할 수 있습니다.
        /// 예: "재무데이터_2023.xlsx", "거래내역_월1분기.xlsx" 등
        /// MongoDB에서는 "file_name" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("file_name")]
        public string FileName { get; set; }

        /// <summary>
        /// 데이터 가시성 설정 플래그
        /// </summary>
        /// <remarks>
        /// true일 때 해당 문서는 일반적인 조회에서 숨겨집니다.
        /// 잠시 숨기기, 오류 데이터 처리, 테스트 데이터 숨김 등에 사용됩니다.
        /// 기본값은 false(보이기)로 설정되어 있습니다.
        /// 묰영구 삭제 대신 대안으로 활용될 수 있습니다.
        /// MongoDB에서는 "is_hidden" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("is_hidden")]
        public bool IsHidden { get; set; } = false;

        /// <summary>
        /// 데이터가 숨겨진 이유를 설명하는 텍스트
        /// </summary>
        /// <remarks>
        /// IsHidden이 true일 때 숨겨진 이유를 기록하는 선택적 필드입니다.
        /// 데이터 관리자가 숨김 원인을 추적하고 차후 복원 결정에 활용할 수 있습니다.
        /// 예: "중복 데이터", "데이터 오류", "테스트용", "사용자 요청" 등
        /// null 값이 허용되며, IsHidden이 false일 때는 일반적으로 null입니다.
        /// MongoDB에서는 "hidden_reason" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("hidden_reason")]
        public string HiddenReason { get; set; }

        /// <remarks>
        /// MongoDB는 동적 필드를 지원하므로, 필요할 때 속성을 추가하거나
        /// Data 딕셔너리를 사용하여 임의의 필드를 저장할 수 있습니다.
        /// 이 유연성 덕분에 다양한 구조의 Excel 파일을 표준화된 모델로 처리할 수 있습니다.
        /// </remarks>
    }
}