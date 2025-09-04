using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceTool.MongoModels
{
    /// <summary>
    /// 컬럼 매핑 정보를 저장하는 column_mapping 컬렉션의 MongoDB 문서 모델
    /// </summary>
    /// <remarks>
    /// Excel 파일의 원본 컬럼명과 사용자 친화적 표시명 사이의 매핑 정보를 관리합니다.
    /// 데이터 타입, 표시 순서, 가시성 설정 등의 메타데이터를 제공하여
    /// UI에서 일관된 데이터 표시와 관리를 지원합니다.
    /// 
    /// 주요 기능:
    /// - 원본 컬럼명에서 표시명으로 변환
    /// - 데이터 타입 지정 및 유효성 검사 지원
    /// - 컬럼 표시 순서 및 가시성 제어
    /// - 키-값 기반 고속 매핑 검색
    /// </remarks>
    public class ColumnMappingDocument
    {
        /// <summary>
        /// MongoDB 문서의 고유 식별자
        /// </summary>
        /// <remarks>
        /// MongoDB에서 자동 생성되는 ObjectId를 문자열로 저장합니다.
        /// 컬럼 매핑 문서의 내부 식별자로, 데이터베이스 인덱스와 조인 작업에 사용됩니다.
        /// BsonId 어트리뷐트로 인해 MongoDB의 _id 필드와 매핑됩니다.
        /// </remarks>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        /// <summary>
        /// Excel 파일에서 가져온 원본 컬럼명
        /// </summary>
        /// <remarks>
        /// Excel 파일의 헤더 셀에 있는 실제 컬럼 이름입니다.
        /// 이 값은 원시 데이터의 키로 사용되며, 변경되지 않아야 합니다.
        /// 데이터 매핑과 조회 작업의 기준이 되는 필드입니다.
        /// 예: "Transaction_Date", "Amount", "Description", "하이와", "군냉" 등
        /// MongoDB에서는 "original_name" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("original_name")]
        public string OriginalName { get; set; }

        /// <summary>
        /// UI에서 표시될 사용자 친화적인 컶럼 명
        /// </summary>
        /// <remarks>
        /// 원본 컬럼명을 사용자가 이해하기 쉬드록 변환한 표시명입니다.
        /// DataGridView, 리포트, 수출 파일 등에서 사용자에게 보여지는 실제 컶럼 명입니다.
        /// 사용자가 직접 수정할 수 있으며, 다국어 지원이 가능합니다.
        /// 예: "Transaction_Date" → "거래일자", "Amount" → "금액" 등
        /// MongoDB에서는 "display_name" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("display_name")]
        public string DisplayName { get; set; }

        /// <summary>
        /// 컶럼 데이터의 타입 지정
        /// </summary>
        /// <remarks>
        /// 해당 컬럼에 저장된 데이터의 타입을 나타내는 문자열입니다.
        /// 데이터 유효성 검사, 정렬, 필터링, 계산 등의 작업에 활용됩니다.
        /// 
        /// 사용 가능한 데이터 타입:
        /// - "text": 일반 텍스트 데이터
        /// - "number": 숫자 데이터 (정수, 실수)
        /// - "currency": 통화/금액 데이터
        /// - "date": 날짜 데이터
        /// - "boolean": 참/거짓 데이터
        /// 
        /// MongoDB에서는 "data_type" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("data_type")]
        public string DataType { get; set; }

        /// <summary>
        /// 컶럼의 UI 표시 여부를 제어하는 플래그
        /// </summary>
        /// <remarks>
        /// true이면 해당 컬럼이 DataGridView, 리포트, 수출 등에서 표시됩니다.
        /// false이면 데이터는 유지되지만 UI에서는 숨겨집니다.
        /// 기본값은 true(표시)로 설정되어 있습니다.
        /// 사용자가 분석에 필요없는 컬럼을 숨기거나, 시스템용 메타데이터를 감추는 데 사용합니다.
        /// MongoDB에서는 "is_visible" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("is_visible")]
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// UI에서 컬럼을 표시할 순서를 지정하는 수치
        /// </summary>
        /// <remarks>
        /// 작은 수가 먼저 표시되며, 같은 수치의 경우 알파벳 순서로 정렬됩니다.
        /// DataGridView의 컬럼 순서, Excel 내보내기 순서, 리포트 컬럼 순서 등에 사용됩니다.
        /// 사용자가 드래그 앨 드롭 등으로 컬럼 순서를 조정하면 이 값이 업데이트됩니다.
        /// 
        /// 예시:
        /// - Sequence = 1: 첫 번째 컬럼
        /// - Sequence = 10: 두 번째 컬럼 (1~9 사이 없음)
        /// - Sequence = 100: 세 번째 컬럼
        /// 
        /// MongoDB에서는 "sequence" 필드로 저장됩니다.
        /// </remarks>
        [BsonElement("sequence")]
        public int Sequence { get; set; }
    }
}