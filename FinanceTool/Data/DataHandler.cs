using ClosedXML.Excel;
// 파일 상단에 추가할 네임스페이스
using FinanceTool.Models.MongoModels;
using FinanceTool.MongoModels;
using FinanceTool.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using SharpCompress.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace FinanceTool
{
    /// <summary>
    /// 전역 데이터 처리 및 관리를 담당하는 정적 클래스
    /// </summary>
    /// <remarks>
    /// 책임: 애플리케이션 전반의 데이터 상태 관리 및 데이터 리포지토리 인스턴스 관리
    /// 계층: Data Layer - 비즈니스 로직과 UI 간의 데이터 중간 계층
    /// 패턴: Singleton 패턴 및 Static Factory 패턴
    /// 의존성: MongoDB Repository 인스턴스들, DataTable 기반 데이터 처리
    /// </remarks>
    public class DataHandler
    {
        /// <summary>
        /// 파일로드 페이지에서 정제된 주 데이터 테이블
        /// </summary>
        /// <value>사용자가 업로드한 원시 데이터를 가공한 결과</value>
        /// <remarks>전체 애플리케이션에서 공용하는 메인 데이터소스</remarks>
        public static DataTable processTable = new DataTable();

        /// <summary>
        /// Excel 파일에서 직접 로드한 원시 데이터
        /// </summary>
        /// <value>Excel 파일의 원시 내용</value>
        public static DataTable excelData = new DataTable();
        
        /// <summary>
        /// 전처리 작업이 완료된 데이터
        /// </summary>
        /// <value>데이터 정제 및 표준화가 적용된 데이터</value>
        public static DataTable preprocessedData = new DataTable();
        
        /// <summary>
        /// 계정 계층 및 레벨 정보가 포함된 데이터
        /// </summary>
        /// <value>치수 데이터 및 계정 찬성 정보</value>
        public static DataTable lowLevelData = new DataTable();
        
        /// <summary>
        /// 금액 데이터 전용 테이블
        /// </summary>
        /// <value>수치 데이터만 추출한 금액 전용 데이터</value>
        public static DataTable moneyDataTable = new DataTable();

        /// <summary>
        /// 추천 키워드 데이터 테이블
        /// </summary>
        /// <value>AI 기반으로 추출된 추천 키워드 목록</value>
        public static DataTable recomandKeywordTable = new DataTable();

        /// <summary>
        /// 1차 클러스터링 결과 데이터
        /// </summary>
        /// <value>초기 클러스터링 작업 결과</value>
        public static DataTable firstClusteringData = new DataTable();
        
        /// <summary>
        /// 2차 클러스터링 결과 데이터
        /// </summary>
        /// <value>세부 클러스터링 작업 결과</value>
        public static DataTable secondClusteringData = new DataTable();
        
        /// <summary>
        /// 최종 클러스터링 결과 데이터
        /// </summary>
        /// <value>모든 클러스터링 작업이 완료된 최종 데이터</value>
        public static DataTable finalClusteringData = new DataTable();

        /// <summary>
        /// 서브 클러스터링 전용 저장 테이블
        /// </summary>
        /// <value>세부 클러스터 분류 작업을 위한 전용 데이터</value>
        public static DataTable subClusteringData = new DataTable();

        /// <summary>
        /// 현재 세션 ID (MongoDB ObjectId)
        /// </summary>
        /// <value>현재 작업 중인 세션의 고유 식별자</value>
        /// <remarks>빈 값인 경우 ObjectId.Empty 상태</remarks>
        public static ObjectId _currentSessionId = ObjectId.Empty;
        

        /// <summary>
        /// 금액 컸의 인덱스 위치
        /// </summary>
        /// <value>데이터 테이블에서 금액 컸의 위치 인덱스</value>
        public static int moneyIndex = 0;
        
        /// <summary>
        /// 계정 레벨 목록
        /// </summary>
        /// <value>계정의 계층 구조 인덱스 목록</value>
        public static List<int> levelList = new List<int>();

        /// <summary>
        /// 계정 레벨명 목록
        /// </summary>
        /// <value>계정의 계층 구조 이름 목록</value>
        ///  // 수정 전에 선택된 컬럼명 확인
        // DataHandler.levelName[0]; // 금액 컬럼명
        // DataHandler.levelName[1]; // 타겟 컬럼명

        public static List<string> levelName = new List<string>();
       
        /// <summary>
        /// 분리자 관리 매니저 인스턴스
        /// </summary>
        /// <value>텍스트 분리 및 처리를 담당하는 매니저</value>
        /// <remarks>지연 초기화 패턴 사용</remarks>
        public static SeparatorManager spManager;

        /// <summary>
        /// 부서 컸 명
        /// </summary>
        /// <value>부서 정보를 나타내는 컸의 이름</value>
        public static string dept_col_name;
        
        /// <summary>
        /// 상품 컸 명
        /// </summary>
        /// <value>상품 정보를 나타내는 컸의 이름</value>
        public static string prod_col_name;
        
        /// <summary>
        /// 하위계정 컸 명
        /// </summary>
        /// <value>하위계정 정보를 나타내는 컸의 이름</value>
        public static string sub_acc_col_name;

        /// <summary>
        /// 부서 컸 사용 여부
        /// </summary>
        /// <value>부서 컸을 분석에 포함할지 여부</value>
        public static bool dept_col_yn = true;
        
        /// <summary>
        /// 상품 컸 사용 여부
        /// </summary>
        /// <value>상품 컸을 분석에 포함할지 여부</value>
        public static bool prod_col_yn = true;

        /// <summary>
        /// 숨겨진 데이터 표시 여부
        /// </summary>
        /// <value>숨겨진 데이터를 UI에 표시할지 여부</value>
        public static bool hiddenData = false;

        /// <summary>
        /// 임시 파일 저장 경로
        /// </summary>
        /// <value>데이터 처리 중 사용할 임시 JSON 파일 경로</value>
        /// <remarks>시스템 임시 디렉토리를 기반으로 생성</remarks>
        public static string tempFilePath = Path.Combine(Path.GetTempPath(), "finance_data_temp.json");
        
        /// <summary>
        /// MongoDB 연결 및 관리 매니저 인스턴스
        /// </summary>
        /// <value>MongoDB 데이터베이스 연결 및 처리를 담당하는 싱글턴 인스턴스</value>
        public static Data.MongoDBManager mongoDBManager = Data.MongoDBManager.Instance;

        /// <summary>
        /// 원시 데이터 리포지토리 인스턴스
        /// </summary>
        /// <value>Excel 파일에서 로드된 원시 데이터 처리 리포지토리</value>
        public static RawDataRepository rawDataRepo = new RawDataRepository();
        
        /// <summary>
        /// 가공된 데이터 리포지토리 인스턴스
        /// </summary>
        /// <value>전처리 및 정제가 완료된 데이터 처리 리포지토리</value>
        public static ProcessDataRepository processDataRepo = new ProcessDataRepository();
        
        /// <summary>
        /// 클러스터링 데이터 리포지토리 인스턴스
        /// </summary>
        /// <value>클러스터링 작업 결과를 관리하는 리포지토리</value>
        public static ClusteringRepository clusteringRepo = new ClusteringRepository();

        /// <summary>
        /// 표시 가능한 컸 목록
        /// </summary>
        /// <value>UI에 표시되는 컸들의 이름 목록</value>
        public static List<string> visibleColumns  = new List<string>();

        /// <summary>
        /// 컸 표시 순서 관리 딕셔너리
        /// </summary>
        /// <value>컸명과 해당 컸의 표시 순서 인덱스를 매핑</value>
        /// <remarks>DataGridView의 컸 순서를 사용자 선호에 따라 동적으로 관리</remarks>
        public static Dictionary<string, int> columnDisplayOrder = new Dictionary<string, int>();

        /// <summary>
        /// 현재 작업중인 세션명 이름 저장
        /// </summary>
        /// <value>현재 작업중인 세션명 이름 저장</value>
        public static string currentSessionName = "";






        /// <summary>
        /// 진행 상태 표시 대화상자 클래스
        /// </summary>
        /// <remarks>
        /// 장시간 실행되는 작업의 진행 상황을 사용자에게 시각적으로 표시
        /// 비동기 작업의 진행률과 상태 메시지를 실시간으로 업데이트
        /// </remarks>
        public class ProgressDialog : Form
        {
            public ProgressBar progressBar;
            private Label statusLabel;

            /// <summary>
            /// ProgressDialog 생성자
            /// </summary>
            public ProgressDialog()
            {
                InitializeComponents();
            }

            /// <summary>
            /// UI 컨포넌트들을 초기화
            /// </summary>
            /// <remarks>
            /// ProgressBar와 Label 컨트롤을 생성하고 레이아웃 설정
            /// 대화상자의 크기, 위치, 스타일 등을 사용자 친화적으로 구성
            /// </remarks>
            private void InitializeComponents()
            {
                this.Width = 400;
                this.Height = 120;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.StartPosition = FormStartPosition.CenterScreen;
                this.ControlBox = false;
                this.Text = "처리 중...";

               
                progressBar = new ProgressBar
                {
                    Style = ProgressBarStyle.Blocks,
                    Location = new Point(20, 20),
                    Width = 360,
                    Size = new System.Drawing.Size(340, 30),
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0
                };

                statusLabel = new Label
                {
                    Location = new Point(20, 60),
                    Size = new System.Drawing.Size(340, 20),                    
                    TextAlign = ContentAlignment.MiddleCenter,
                    Text = "데이터 처리 중... (0%)"
                };

                this.Controls.Add(progressBar);
                this.Controls.Add(statusLabel);
            }

            /// <summary>
            /// 진행률과 상태 메시지를 비동기적으로 업데이트
            /// </summary>
            /// <param name="percentage">진행률 (0-100)</param>
            /// <param name="status">선택적 상태 메시지 (null인 경우 기본 메시지 사용)</param>
            /// <remarks>
            /// UI 스레드에서 안전하게 호출하기 위한 Invoke 기반 비동기 업데이트
            /// 백그라운드 작업에서 UI 컨트롤을 안전하게 조작하는 기능
            /// </remarks>
            /// <exception cref="ArgumentOutOfRangeException">percentage가 0-100 범위를 벗어난 경우</exception>
            public async Task UpdateProgress(int percentage, string status = null)
            {
                if (InvokeRequired)
                {
                    await Invoke(async () => await UpdateProgress(percentage, status));
                    return;
                }

                progressBar.Value = percentage;
                statusLabel.Text = status ?? $"데이터 처리 중입니다... ({percentage}%)";

                if (percentage >= 100)
                {
                    statusLabel.Text = "처리가 완료되었습니다. (100%)";
                    await Task.Delay(500); // 0.5초 대기
                }
            }
        }

       

        /// <summary>
        /// DataTable의 특정 컬럼 값들을 문자열 리스트로 변환
        /// </summary>
        /// <param name="table">데이터를 추출할 DataTable</param>
        /// <param name="columnIndex">추출할 컬럼의 인덱스</param>
        /// <returns>지정된 컬럼의 모든 값들을 담은 문자열 리스트</returns>
        /// <remarks>
        /// DataTable의 특정 컬럼 데이터를 순회하며 모든 행의 값을 리스트로 추출
        /// null 값은 빈 문자열로 변환되어 처리
        /// 데이터 분석 및 키워드 추출에 사용
        /// </remarks>
        /// <exception cref="ArgumentNullException">table이 null인 경우</exception>
        /// <exception cref="IndexOutOfRangeException">columnIndex가 유효하지 않은 경우</exception>
        public static List<string> GetColumnValuesAsList(DataTable table, int columnIndex)
        {
            // 반환할 리스트 초기화
            List<string> result = new List<string>();

            // DataTable의 행을 순회하며 특정 열의 값을 리스트에 추가
            foreach (DataRow row in table.Rows)
            {
                // 열의 값을 문자열로 변환하여 추가 (null 값은 빈 문자열로 처리)
                result.Add(row[columnIndex]?.ToString() ?? string.Empty);
            }

            return result;
        }

     

        /// <summary>
        /// 키워드 기준으로 매칭 데이터를 찾는 메서드
        /// </summary>
        /// <param name="listA">검색 대상 문자열 리스트</param>
        /// <param name="search_keyword">검색할 키워드</param>
        /// <returns>매칭된 문자열 리스트</returns>
        /// <remarks>
        /// 입력된 키워드와 리스트 아이템들 간의 포함 관계를 찾아
        /// 매칭되는 데이터를 반환하는 키워드 기반 데이터 필터링 기능
        /// </remarks>
        public static List<string> FindMachKeyword(List<string> listA, string search_keyword)
        {
            // 결과를 저장할 리스트
            List<string> output = new List<string>();

            // B의 각 값을 A와 비교
            foreach (string valueA in listA)
            {
                
                
                // A와 B의 값이 서로 포함 관계인지 확인
                if (CompareByTwoChars(search_keyword, valueA))
                {
                    // 포함 관계가 있으면 출력 리스트에 추가
                    //Debug.WriteLine($"포함 키워드 대상 감지 : search_keyword : {search_keyword} valueA : {valueA}");
                    output.Add(valueA);
                }
            }
            
            //Debug.WriteLine($"output List : {string.Join(",", output)}");

            return output;
        }


       

        /// <summary>
        /// 두 단어를 2글자씩 분할하여 유사도 비교
        /// </summary>
        /// <param name="baseWord">비교 기준이 되는 단어</param>
        /// <param name="targetWord">비교 대상 단어</param>
        /// <returns>두 단어가 유사한지 여부 (true: 유사, false: 비유사)</returns>
        /// <remarks>
        /// 단어를 2글자씩 분할하여 공통 부분이 있는지 확인하는 문자열 유사도 비교 알고리즘
        /// 한글 키워드 매칭에 특화된 방식으로 부분 일치도를 검사
        /// </remarks>
        public static bool CompareByTwoChars(string baseWord, string targetWord)
        {

            if (targetWord.Length < 2)
            {
                return false;
            }

            // 2글자 미만인 경우 처리
            if (baseWord.Length < 2 )
            {
                //return false;
                return targetWord.Contains(baseWord);

            }

            // 기준 단어를 2글자씩 자르기
            List<string> baseParts = new List<string>();
            for (int i = 0; i < baseWord.Length - 1; i++)
            {
                baseParts.Add(baseWord.Substring(i, 2));
            }

            // 대상 단어를 2글자씩 자르기
            List<string> targetParts = new List<string>();
            for (int i = 0; i < targetWord.Length - 1; i++)
            {
                targetParts.Add(targetWord.Substring(i, 2));
            }

            // 두 리스트 간에 공통된 2글자 조합이 있는지 확인
            return baseParts.Any(b => targetParts.Contains(b));
        }

        /// <summary>
        /// 그룹 데이터 테이블을 비동기적으로 생성
        /// </summary>
        /// <param name="sourceTable">원본 데이터 테이블</param>
        /// <param name="moneyDataTable">금액 데이터 테이블</param>
        /// <param name="secondyn">2차 처리 여부 (기본값: false)</param>
        /// <returns>클러스터링 결과가 포함된 데이터 테이블</returns>
        /// <remarks>
        /// 클러스터링 작업을 위한 데이터 그룹화 및 집계 처리
        /// 성능: 대용량 데이터 처리를 위한 비동기 연산 및 메모리 최적화
        /// 의존성: DataTable 기반 데이터 조작, 금액 계산 로직
        /// 버전 관리: MongoDB 전환에 따라 삭제 예정 (레거시 기능)
        /// </remarks>
        /// <exception cref="ArgumentNullException">sourceTable 또는 moneyDataTable이 null인 경우</exception>
        /// <exception cref="InvalidOperationException">데이터 처리 중 오류 발생 시</exception>
        public static async Task<DataTable> CreateSetGroupDataTableAsync(DataTable sourceTable, DataTable moneyDataTable, bool secondyn = false)
        {
            // 시작 시간 측정 (성능 모니터링용)
            var stopwatch = Stopwatch.StartNew();
            Debug.WriteLine("CreateSetGroupDataTableAsync 수행 시작");
            Debug.WriteLine($"sourceTable 행 수: {sourceTable.Rows.Count}");

            // 결과 DataTable 생성 - 컬럼 구조 명확히 정의
            DataTable resultTable = new DataTable();
            resultTable.Columns.Add("ID", typeof(int));
            resultTable.Columns.Add("ClusterID", typeof(int));
            resultTable.Columns.Add("ClusterSubID", typeof(int));
            resultTable.Columns.Add("클러스터명", typeof(string));
            resultTable.Columns.Add("키워드목록", typeof(string));
            resultTable.Columns.Add("Count", typeof(int));
            resultTable.Columns.Add("합산금액", typeof(decimal));
            resultTable.Columns.Add("dataIndex", typeof(string));

            try
            {
                // 성능 최적화: 미리 충분한 용량 할당
                resultTable.MinimumCapacity = Math.Max(100, sourceTable.Rows.Count / 10);

                // 금액 정보를 저장할 딕셔너리 (raw_data_id -> 금액)
                Dictionary<string, decimal> moneyLookup = new Dictionary<string, decimal>(sourceTable.Rows.Count);

                // 1. 먼저 moneyDataTable에서 금액 정보 로드 (기존 로직)
                if (moneyDataTable != null && moneyDataTable.Columns.Count > 0)
                {
                    // 금액 컬럼명 가져오기
                    string moneyColumnName = moneyDataTable.Columns[0].ColumnName;

                    foreach (DataRow row in moneyDataTable.Rows)
                    {
                        if (row["raw_data_id"] != DBNull.Value && row[moneyColumnName] != DBNull.Value)
                        {
                            string rawDataId = row["raw_data_id"].ToString();
                            if (!string.IsNullOrEmpty(rawDataId) &&
                                decimal.TryParse(row[moneyColumnName].ToString(), out decimal money))
                            {
                                moneyLookup[rawDataId] = money;
                            }
                        }
                    }

                    Debug.WriteLine($"moneyDataTable에서 로드한 금액 정보: {moneyLookup.Count}개");
                }

                // 2. MongoDB process_view_data 컬렉션에서 money 정보 로드 (신규 추가)
                var processViewRepo = new Repositories.ProcessViewRepository();

                // 필요한 raw_data_id 목록 추출
                HashSet<string> neededIds = new HashSet<string>();
                foreach (DataRow row in sourceTable.Rows)
                {
                    if (row["raw_data_id"] != DBNull.Value)
                    {
                        string rawDataId = row["raw_data_id"].ToString();
                        if (!string.IsNullOrEmpty(rawDataId))
                        {
                            neededIds.Add(rawDataId);
                        }
                    }
                }

                // 아직 금액 정보가 없는 ID만 필터링
                var missingMoneyIds = neededIds
                    .Where(id => !moneyLookup.ContainsKey(id))
                    .ToList();

                Debug.WriteLine($"process_view_data에서 로드할 금액 정보: {missingMoneyIds.Count}개");

                // 배치 처리로 MongoDB에서 금액 정보 로드
                if (missingMoneyIds.Count > 0)
                {
                    const int MongoDBbatchSize = 10000;

                    // MongoDB 연결 확인
                    await Data.MongoDBManager.Instance.EnsureInitializedAsync();

                    for (int i = 0; i < missingMoneyIds.Count; i += MongoDBbatchSize)
                    {
                        int currentBatchSize = Math.Min(MongoDBbatchSize, missingMoneyIds.Count - i);
                        var batchIds = missingMoneyIds.GetRange(i, currentBatchSize);

                        // ID 목록으로 process_view_data 조회
                        var filter = Builders<MongoModels.ProcessViewDocument>.Filter.In(d => d.RawDataId, batchIds);
                        var processViewDocs = await processViewRepo.FindDocumentsAsync(filter);

                        // 조회된 데이터에서 money 정보 추출
                        foreach (var doc in processViewDocs)
                        {
                            if (!string.IsNullOrEmpty(doc.RawDataId) && doc.Money != null)
                            {
                                decimal amount = 0;

                                // Money 필드 타입에 따른 처리 (다양한 타입 지원)
                                if (doc.Money is decimal decimalAmount)
                                {
                                    amount = decimalAmount;
                                }
                                else if (doc.Money is double doubleAmount)
                                {
                                    amount = (decimal)doubleAmount;
                                }
                                else if (doc.Money is int intAmount)
                                {
                                    amount = intAmount;
                                }
                                else if (doc.Money is long longAmount)
                                {
                                    amount = longAmount;
                                }
                                else if (doc.Money is string strAmount && decimal.TryParse(strAmount, out decimal parsedAmount))
                                {
                                    amount = parsedAmount;
                                }
                                else
                                {
                                    // 다른 타입인 경우 ToString 후 파싱 시도
                                    string moneyStr = doc.Money.ToString();
                                    if (!string.IsNullOrEmpty(moneyStr) && decimal.TryParse(moneyStr, out decimal parsedValue))
                                    {
                                        amount = parsedValue;
                                    }
                                }

                                // 파싱된 금액이 0이 아니면 저장
                                if (amount != 0)
                                {
                                    moneyLookup[doc.RawDataId] = amount;
                                }
                            }
                        }
                    }

                    Debug.WriteLine($"process_view_data에서 로드한 금액 정보: {moneyLookup.Count}개");
                }

                Debug.WriteLine($"금액 정보 로드 완료: {moneyLookup.Count}개, 소요 시간: {stopwatch.ElapsedMilliseconds}ms");

                // 부서/공급업체 정보를 위한 딕셔너리 (명확한 크기 지정)
                Dictionary<string, string> deptLookup = new Dictionary<string, string>(sourceTable.Rows.Count);
                Dictionary<string, string> prodLookup = new Dictionary<string, string>(sourceTable.Rows.Count);

                // 부서/공급업체 정보가 필요한 경우에만 MongoDB에서 로드
                if (secondyn && (dept_col_yn || prod_col_yn))
                {
                    // MongoDB 연결 확인
                    bool mongoConnected = await Data.MongoDBManager.Instance.EnsureInitializedAsync();
                    if (!mongoConnected)
                    {
                        throw new Exception("MongoDB 연결에 실패했습니다.");
                    }

                    // ProcessView 저장소에서 부서/공급업체 정보 로드
                    
                    var processDataRepo = new Repositories.ProcessDataRepository();

                   

                    // 성능 최적화: 필요한 ID만 쿼리하는 필터 사용
                    if (neededIds.Count > 0)
                    {
                        // 배치 처리 도입: 대량 데이터 처리 최적화
                        const int mongoBatchSize = 10000; // MongoDB 권장 최대 배치 크기

                        foreach (var idBatch in BatchIdsForQuery(neededIds, mongoBatchSize))
                        {
                            var filter = Builders<MongoModels.ProcessViewDocument>.Filter.In(d => d.RawDataId, idBatch);
                            var batchDocs = await processViewRepo.FindDocumentsAsync(filter);

                            foreach (var doc in batchDocs)
                            {
                                if (!string.IsNullOrEmpty(doc.RawDataId))
                                {
                                    if (dept_col_yn && !string.IsNullOrEmpty(doc.Department))
                                    {
                                        deptLookup[doc.RawDataId] = doc.Department;
                                    }
                                    if (prod_col_yn && !string.IsNullOrEmpty(doc.Supplier))
                                    {
                                        prodLookup[doc.RawDataId] = doc.Supplier;
                                    }
                                }
                            }
                        }
                    }

                    Debug.WriteLine($"부서 정보 캐싱: {deptLookup.Count}개, 공급업체 정보 캐싱: {prodLookup.Count}개");
                }

                // 집합 셋을 관리할 딕셔너리
                Dictionary<string, (int ID, int Count, decimal SumValue, HashSet<string> SourceIndices)> setGroups =
                    new Dictionary<string, (int, int, decimal, HashSet<string>)>();

                // 그룹 ID 카운터
                //int nextGroupId = 0;
                int nextGroupId = 1;

                // 시스템 리소스에 맞게 병렬 처리 최적화
                int batchSize = CalculateOptimalBatchSize(sourceTable.Rows.Count);
                int processorCount = Environment.ProcessorCount;
                int maxDegreeOfParallelism = Math.Max(1, processorCount - 1);

                Debug.WriteLine($"병렬 처리 설정: 최대 {maxDegreeOfParallelism}개 스레드, 배치 크기 {batchSize}");

                // 데이터 그룹화 - 병렬 처리 최적화, 더 작은 배치 크기로 처리
                var lockObj = new object();
                var rowBatches = SplitIntoOptimalBatches(sourceTable.Rows.Count, batchSize);

                await Task.Run(() => {
                    Parallel.ForEach(rowBatches,
                        new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                        batchRange => {
                            // 배치별 로컬 Dictionary (병합 전 임시 저장용)
                            var batchGroups = new Dictionary<string, (int Count, decimal SumValue, HashSet<string> Ids)>();

                            for (int rowIndex = batchRange.Start; rowIndex < batchRange.End; rowIndex++)
                            {
                                if (rowIndex >= sourceTable.Rows.Count) continue;

                                DataRow row = sourceTable.Rows[rowIndex];

                                // raw_data_id 가져오기 및 유효성 검사
                                if (row["raw_data_id"] == DBNull.Value)
                                    continue;

                                string rawDataId = row["raw_data_id"].ToString();
                                if (string.IsNullOrEmpty(rawDataId))
                                    continue;

                                // 키워드 집합 생성
                                List<string> setElements = new List<string>();

                                // 키워드 컬럼 처리
                                foreach (DataColumn col in sourceTable.Columns)
                                {
                                    // 메타데이터 컬럼 제외
                                    if (IsMetaDataColumn(col.ColumnName))
                                        continue;

                                    if (row[col] != DBNull.Value && !string.IsNullOrWhiteSpace(row[col].ToString()))
                                    {
                                        setElements.Add(row[col].ToString().Trim());
                                    }
                                }

                                // 부서/공급업체 정보 추가 (필요시)
                                if (secondyn && dept_col_yn && deptLookup.TryGetValue(rawDataId, out string deptValue))
                                {
                                    setElements.Add(deptValue);
                                }

                                if (secondyn && prod_col_yn && prodLookup.TryGetValue(rawDataId, out string prodValue))
                                {
                                    setElements.Add(prodValue);
                                }

                                // 키워드가 없는 경우 건너뛰기
                                if (setElements.Count == 0)
                                    continue;

                                // 집합을 정렬하여 일관성 유지
                                setElements.Sort();

                                // 집합 셋 문자열 생성
                                string setKey = string.Join(",", setElements);

                                // 금액 정보 조회
                                decimal refValue = 0;
                                moneyLookup.TryGetValue(rawDataId, out refValue);

                                // 배치별 로컬 Dictionary에 추가 또는 업데이트
                                if (!batchGroups.ContainsKey(setKey))
                                {
                                    batchGroups[setKey] = (1, refValue, new HashSet<string> { rawDataId });
                                }
                                else
                                {
                                    var existing = batchGroups[setKey];
                                    existing.Ids.Add(rawDataId);
                                    batchGroups[setKey] = (existing.Count + 1, existing.SumValue + refValue, existing.Ids);
                                }
                            }

                            // 전역 setGroups로 병합 (lock 사용)
                            lock (lockObj)
                            {
                                foreach (var entry in batchGroups)
                                {
                                    string setKey = entry.Key;
                                    var batchGroup = entry.Value;

                                    if (!setGroups.ContainsKey(setKey))
                                    {
                                        int id = nextGroupId++;
                                        setGroups[setKey] = (id, batchGroup.Count, batchGroup.SumValue, batchGroup.Ids);
                                    }
                                    else
                                    {
                                        var existing = setGroups[setKey];
                                        // 기존 HashSet에 새 ID 추가
                                        foreach (var id in batchGroup.Ids)
                                        {
                                            existing.SourceIndices.Add(id);
                                        }
                                        setGroups[setKey] = (
                                            existing.ID,
                                            existing.Count + batchGroup.Count,
                                            existing.SumValue + batchGroup.SumValue,
                                            existing.SourceIndices
                                        );
                                    }
                                }
                            }
                        });
                });

                Debug.WriteLine($"데이터 그룹화 완료: {setGroups.Count}개 그룹, 소요 시간: {stopwatch.ElapsedMilliseconds}ms");

                // 결과 DataTable에 행 추가 - 단일 스레드로 안전하게 처리
                // ID로 정렬
                var sortedGroups = setGroups.OrderBy(g => g.Value.ID).ToList();

                foreach (var group in sortedGroups)
                {
                    // 그룹 요소 배열
                    string setKey = group.Key;
                    string[] elements = setKey.Split(',');
                    var groupValue = group.Value;

                    try
                    {
                        // 핵심 버그 수정 부분: resultTable.NewRow() 호출 전에 테이블 상태 확인
                        if (resultTable.Columns.Count < 7)
                        {
                            Debug.WriteLine($"오류: 결과 테이블 컬럼 부족 - 현재 {resultTable.Columns.Count}개 컬럼 존재");
                            continue;
                        }

                        // 새 행 생성 (스레드 안전하게 처리)
                        DataRow newRow = resultTable.NewRow();

                        // 모든 컬럼에 값 명시적 할당
                        newRow["ID"] = groupValue.ID;
                        newRow["ClusterID"] = -1;
                        newRow["ClusterSubID"] = -1;
                        newRow["클러스터명"] = string.Join("_", elements);
                        newRow["키워드목록"] = setKey;
                        newRow["Count"] = groupValue.Count;
                        newRow["합산금액"] = groupValue.SumValue;
                        newRow["dataIndex"] = string.Join(",", groupValue.SourceIndices);

                        // 행 추가 (안전하게)
                        resultTable.Rows.Add(newRow);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"행 생성 중 오류: {ex.Message} - 그룹 ID: {groupValue.ID}");
                        // 오류 발생해도 계속 진행
                    }
                }

                Debug.WriteLine($"결과 테이블 생성 완료: {resultTable.Rows.Count}개 행, 총 소요 시간: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CreateSetGroupDataTableAsync 오류: {ex.Message}\n{ex.StackTrace}");

                // 오류 발생 시 빈 테이블 반환하지 않고 기본 컬럼만 있는 테이블 반환
                if (resultTable.Rows.Count == 0)
                {
                    Debug.WriteLine("오류 발생으로 인해 빈 결과 테이블 반환");
                }
            }

            return resultTable;
        }

        // 헬퍼 메서드들
        /// <summary>
        /// 지정된 컬럼명이 메타데이터 컬럼인지 확인
        /// </summary>
        /// <param name="columnName">확인할 컬럼명</param>
        /// <returns>메타데이터 컬럼인 경우 true, 그렇지 않으면 false</returns>
        /// <remarks>
        /// 시스템에서 사용하는 메타데이터 컬럼들(raw_data_id, id, process_data_id, import_date)을
        /// 일반 데이터 컬럼과 구분하기 위한 유틸리티 메서드
        /// </remarks>
        private static bool IsMetaDataColumn(string columnName)
        {
            // 메타데이터 컬럼 목록
            string[] metaColumns = { "raw_data_id", "id", "process_data_id", "import_date" };
            return metaColumns.Contains(columnName);
        }

        /// <summary>
        /// 데이터 처리를 위한 최적의 배치 크기를 계산
        /// </summary>
        /// <param name="totalItems">전체 처리할 항목 수</param>
        /// <returns>최적화된 배치 크기</returns>
        /// <remarks>
        /// 데이터 크기에 따라 최적의 배치 처리 크기를 결정
        /// - 10,000개 미만: 1,000개 배치
        /// - 100,000개 미만: 10,000개 배치
        /// - 100,000개 이상: 20,000개 배치
        /// 메모리 사용량과 처리 성능의 균형을 고려한 설정
        /// </remarks>
        private static int CalculateOptimalBatchSize(int totalItems)
        {
            // 최적의 배치 크기 계산 (항목 수 기준)
            if (totalItems < 10000) return 1000;
            if (totalItems < 100000) return 10000;
            return 20000;
        }

        /// <summary>
        /// 전체 항목을 최적의 배치 단위로 분할
        /// </summary>
        /// <param name="totalItems">전체 항목 수</param>
        /// <param name="batchSize">각 배치의 크기</param>
        /// <returns>시작과 종료 인덱스를 포함하는 배치 범위 리스트</returns>
        /// <remarks>
        /// 대용량 데이터를 효율적으로 처리하기 위해 지정된 크기로 분할
        /// 각 배치는 (Start, End) 튜플 형태로 반환되며 End는 포함되지 않는 인덱스
        /// 병렬 처리 및 메모리 관리 최적화에 사용
        /// </remarks>
        private static List<(int Start, int End)> SplitIntoOptimalBatches(int totalItems, int batchSize)
        {
            var batches = new List<(int Start, int End)>();
            for (int i = 0; i < totalItems; i += batchSize)
            {
                int end = Math.Min(i + batchSize, totalItems);
                batches.Add((i, end));
            }
            return batches;
        }

        /// <summary>
        /// ID 집합을 지정된 배치 크기로 분할
        /// </summary>
        /// <param name="ids">분할할 ID 집합</param>
        /// <param name="batchSize">배치 크기</param>
        /// <returns>분할된 ID 리스트들</returns>
        /// <remarks>
        /// 대량 데이터 소오스 쿠리 최적화를 위한 배치 처리 유틸리티
        /// </remarks>
        private static IEnumerable<List<string>> BatchIdsForQuery(HashSet<string> ids, int batchSize)
        {
            var idList = ids.ToList();
            for (int i = 0; i < idList.Count; i += batchSize)
            {
                yield return idList.Skip(i).Take(batchSize).ToList();
            }
        }

        /// <summary>
        /// 클러스터링 결과를 DataGridView에 표시하기 위한 설정
        /// </summary>
        /// <param name="dgv">설정할 DataGridView 객체</param>
        /// <param name="dt">클러스터링 결과 데이터 테이블</param>
        /// <remarks>
        /// 클러스터링 결과 데이터를 필터링하고 DataGridView에 적절한 형식으로 표시
        /// 컬럼 숨김, 데이터 포맷, 스타일 등을 클러스터링 결과 표시에 맞게 구성
        /// </remarks>
        public static void SetupDataGridView(DataGridView dgv, DataTable dt)
        {
            // 조건에 맞는 데이터만 필터링
            var filteredData = dt.AsEnumerable()
                .Where(row =>
                    Convert.ToInt32(row["ClusterID"]) <= 0 ||
                    Convert.ToInt32(row["ClusterID"]) == Convert.ToInt32(row["ID"]))
                .CopyToDataTable();

            dgv.DataSource = filteredData;

            // ID 컬럼 숨기기
            if (dgv.Columns["ID"] != null)
            {
                dgv.Columns["ID"].Visible = false;
            }

            // ClusterID 컬럼 숨기기
            dgv.Columns["ClusterID"].Visible = false;

            // dataIndex 컬럼 숨기기
            dgv.Columns["dataIndex"].Visible = false;

            // Count 컬럼 형식 지정
            if (dgv.Columns["Count"] != null)
            {
                dgv.Columns["Count"].DefaultCellStyle.Format = "N0"; // 천 단위 구분자
                dgv.Columns["Count"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            // 합산금액 컬럼 형식 지정
            if (dgv.Columns["합산금액"] != null)
            {
                dgv.Columns["합산금액"].DefaultCellStyle.Format = "N0"; // 천 단위 구분자
                dgv.Columns["합산금액"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            if (dgv.Columns["클러스터명"] != null)
            {
                dgv.Columns["클러스터명"].ReadOnly = true;
            }

            // 나머지 컬럼들은 읽기 전용
            if (dgv.Columns["키워드목록"] != null)
            {
                dgv.Columns["키워드목록"].ReadOnly = true;
            }

            // 기본 설정
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;

            // MongoDB 컬렉션에 클러스터링 결과 저장
            //SaveClusteringResultsToMongoDB(filteredData);
        }

       

        // DataGridView별로 선택된 셀들을 추적하기 위한 딕셔너리
        // 마우스 다운/업 이벤트를 사용하여 선택 영역 추적
        
        /// <summary>
        /// DataGridView별 선택된 셀들을 추적하기 위한 딕셔너리
        /// </summary>
        /// <value>각 DataGridView와 해당 그리드에서 선택된 셀들의 목록</value>
        /// <remarks>
        /// 마우스 드래그 선택 및 다중 셀 선택 기능을 지원하기 위한 전역 상태 관리
        /// 사용자의 다양한 선택 패턴을 추적하여 UI/UX 향상
        /// </remarks>
        public static Dictionary<DataGridView, List<DataGridViewCell>> dragSelections = new Dictionary<DataGridView, List<DataGridViewCell>>();

        /// <summary>
        /// DataGridView를 드래그 선택 시스템에 등록
        /// </summary>
        /// <param name="dgv">등록할 DataGridView 컨트롤</param>
        /// <remarks>
        /// 새로운 DataGridView에 마우스 이벤트 핸들러들을 연결하고 선택 관리 초기화
        /// 다중 셀 선택 기능과 드래그 선택 기능을 활성화
        /// </remarks>
        /// <exception cref="ArgumentNullException">dgv가 null인 경우</exception>
        public static void RegisterDataGridView(DataGridView dgv)
        {
            // 초기화
            dragSelections[dgv] = new List<DataGridViewCell>();

            // 이벤트 핸들러 등록
            dgv.MouseUp += DataGridView_MouseUp;
            dgv.CellContentClick += DataGridView_CellContentClick;
        }

       

        /// <summary>
        /// DataGridView에서 마우스 버튼을 떼 때 발생하는 이벤트 핸들러
        /// </summary>
        /// <param name="sender">이벤트를 발생시킨 DataGridView 객체</param>
        /// <param name="e">마우스 이벤트 인수</param>
        /// <remarks>
        /// 마우스 드래그 선택 작업을 종료하고 선택된 영역을 확정
        /// 다중 셀 선택 기능의 핀기 역할을 담당
        /// </remarks>
        public static void DataGridView_MouseUp(object sender, MouseEventArgs e)
        {
            
            DataGridView dgv = sender as DataGridView;
            if (dgv == null) return;

            // 마우스 업 시 현재 선택된 셀 저장
            dragSelections[dgv].Clear();
            foreach (DataGridViewCell cell in dgv.SelectedCells)
            {
                dragSelections[dgv].Add(cell);
            }

            // 디버그용
            Debug.WriteLine($"선택된 셀 수: {dragSelections[dgv].Count}");
        }

        /// <summary>
        /// DataGridView에서 셀 콘텐츠 클릭 시 발생하는 이벤트 핸들러
        /// </summary>
        /// <param name="sender">이벤트를 발생시킨 DataGridView 객체</param>
        /// <param name="e">셀 이벤트 인수 (행/컸 인덱스 포함)</param>
        /// <remarks>
        /// 체크박스 컸 클릭 시 다중 체크/다중 체크 해제 기능 처리
        /// 사용자의 선택 상태를 추적하고 집단 작업을 위한 선택 관리
        /// UI 선택 영역 갱신 및 사용자 피드백 제공
        /// </remarks>
        public static void DataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

            
            DataGridView dgv = sender as DataGridView;
            if (dgv == null) return;

            //Debug.WriteLine($"DataGridView_CellContentClick start => dragSelections[dgv].Count : {dragSelections[dgv].Count}");
            // 체크박스 컬럼 클릭 시 (체크박스 컬럼이 0번 컬럼이라고 가정)
            //if (e.ColumnIndex == 0 && e.RowIndex >= 0)
            if (e.ColumnIndex == 0 && e.RowIndex >= 0)
            {
                // 클릭된 셀의 체크박스 상태 확인
                DataGridViewCheckBoxCell clickedCell = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewCheckBoxCell;

                
                if (clickedCell == null) return;

                bool newValue = !(Convert.ToBoolean(clickedCell.Value)); // 현재 값의 반대로 설정

                // 마우스 업에서 저장한 선택 영역 사용
                if (dragSelections.ContainsKey(dgv) && dragSelections[dgv].Count > 0)
                {
                    // 저장된 선택 영역의 모든 체크박스 상태 변경
                    foreach (DataGridViewCell cell in dragSelections[dgv])
                    {
                        if (cell.ColumnIndex == 0) // 체크박스 컬럼인 경우
                        {
                            //Debug.WriteLine($"cell.RowIndex : {cell.RowIndex}");
                            if (cell.RowIndex >= 0 && cell.RowIndex < dgv.Rows.Count)
                            {
                                DataGridViewCheckBoxCell checkCell = dgv.Rows[cell.RowIndex].Cells[0] as DataGridViewCheckBoxCell;
                                if (checkCell != null)
                                    checkCell.Value = newValue;
                            }
                            else
                            {
                                Debug.WriteLine($"잘못된 RowIndex: {cell.RowIndex}");
                                return; // 또는 적절한 처리
                            }

                        }
                    }
                }
                else
                {
                    // 저장된 선택 영역이 없으면 클릭된 셀만 변경
                    clickedCell.Value = newValue;
                }
                // 마우스 다운 시 현재 선택된 셀 저장
                dragSelections[dgv].Clear();
                // 데이터그리드뷰 새로고침
                dgv.Refresh();
            }
            //Debug.WriteLine("DataGridView_CellContentClick end");
        }

        /// <summary>
        /// 금액 컸에 대한 커스텀 정렬 비교 이벤트 핸들러
        /// </summary>
        /// <param name="sender">이벤트를 발생시킨 DataGridView 객체</param>
        /// <param name="e">정렬 비교 이벤트 인수</param>
        /// <remarks>
        /// 금액 데이터를 숫자 값으로 올바르게 정렬하기 위한 커스텀 비교 로직
        /// 문자열 형태의 금액을 숫자로 변환하여 정렬 수행
        /// 통화 기호 및 천 단위 구분자 처리 포함
        /// </remarks>
        public static void money_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            // 디버깅을 위한 로깅 추가
            //Debug.WriteLine($"SortCompare 호출됨: DataGridView={sender.GetType().Name}, Column={e.Column.Name}, HeaderText={e.Column.HeaderText}");
            try
            {
                // 정렬이 어떤 DataGridView에서 발생했는지 확인
                // 디버그 로그 추가
                //Debug.WriteLine($"정렬 시도: Column={e.Column.Name}, HeaderText={e.Column.HeaderText}, ValueType={e.Column.ValueType}");

                // 두 값이 모두 null이면 동등하게 처리
                if ((e.CellValue1 == null || e.CellValue1 == DBNull.Value) &&
                    (e.CellValue2 == null || e.CellValue2 == DBNull.Value))
                {
                    e.SortResult = 0;
                    e.Handled = true;
                    return;
                }

                // 값1이 null이면 값2보다 작게 처리
                if (e.CellValue1 == null || e.CellValue1 == DBNull.Value)
                {
                    e.SortResult = -1;
                    e.Handled = true;
                    return;
                }

                // 값2가 null이면 값1보다 크게 처리
                if (e.CellValue2 == null || e.CellValue2 == DBNull.Value)
                {
                    e.SortResult = 1;
                    e.Handled = true;
                    return;
                }

                // "금액" 컬럼에 대해서만 커스텀 정렬 적용
                if (e.Column.Name == "합산금액" || e.Column.HeaderText == "합산금액" || e.Column.Name == "total_money" || e.Column.HeaderText == "total_money")
                {
                    //Debug.WriteLine($"커스텀 정렬 적용: Column={e.Column.Name}, HeaderText={e.Column.HeaderText}");
                    // 셀 값에서 숫자만 추출
                    Decimal val1 = ExtractNumber(e.CellValue1?.ToString() ?? "");
                    Decimal val2 = ExtractNumber(e.CellValue2?.ToString() ?? "");

                    //Debug.WriteLine($"비교 값: {e.CellValue1} ({val1}) vs {e.CellValue2} ({val2})");

                    // 숫자 기준으로 비교
                    e.SortResult = val1.CompareTo(val2);
                    // 이벤트 처리 완료 표시
                    e.Handled = true;

                    //Debug.WriteLine("커스텀 정렬 완료");
                }
                else if (e.Column.ValueType == typeof(string))
                {
                    //Debug.WriteLine($"[string]기본 정렬 사용: Column={e.Column.Name}, HeaderText={e.Column.HeaderText} , ValueType={e.Column.ValueType}");
                    // 문자열 타입에 대한 안전한 처리 추가
                    string value1 = e.CellValue1?.ToString() ?? string.Empty;
                    string value2 = e.CellValue2?.ToString() ?? string.Empty;

                    e.SortResult = string.Compare(value1, value2);
                    e.Handled = true;
                }
                else
                {
                    //Debug.WriteLine($"[default]기본 정렬 사용: Column={e.Column.Name}, HeaderText={e.Column.HeaderText} , ValueType={e.Column.ValueType}");
                }
            }
            catch (Exception ex)
            {
                // 예외 발생 시 로그 기록
                Debug.WriteLine($"정렬 중 예외 발생: {ex.Message}");

                // 기본 정렬 사용
                Debug.WriteLine($"기본 정렬 사용: Column={e.Column.Name}, HeaderText={e.Column.HeaderText}");
                e.Handled = false;
            }
           
        }

        // 문자열에서 숫자만 추출하는 함수
        private static Decimal ExtractNumber(string text)
        {
            /*
            if (string.IsNullOrEmpty(text))
                return 0;

            // 마이너스 부호 여부 확인
            bool isNegative = text.Trim().StartsWith("-");

            // 숫자만 추출 (마이너스 부호 제외)
            string numericPart = new string(text.Where(c => char.IsDigit(c)).ToArray());

            // 숫자 부분이 비어 있으면 0 반환
            if (string.IsNullOrEmpty(numericPart))
                return 0;

            // 숫자로 변환
            if (Decimal.TryParse(numericPart, out Decimal result))
            {
                // 마이너스 부호가 있었다면 결과값을 음수로 변환
                return isNegative ? -result : result;
            }

            return 0;
            */
            if (string.IsNullOrEmpty(text))
            {
                Debug.WriteLine($"text is null?? {text}");
                return 0;
            }

            //Debug.WriteLine($"tExtractNumber text :  {text}");
            //, 값 변환

            // 콤마 제거
            string cleanText = text.Replace(",", "");

            // 정규식으로 숫자 패턴 추출 (부호, 숫자, 소수점 포함)
            Match match = Regex.Match(cleanText, @"(-?\d+(\.\d+)?)");

            if (match.Success && Decimal.TryParse(match.Value, out Decimal result))
            {
                return result;
            }
            else
            {
               
                Debug.WriteLine($"text is match.Success  : {match.Success}, match.Value :  {match.Value} " );
                if (Decimal.TryParse(match.Value, out Decimal result33))
                {
                    Debug.WriteLine($",Convert.ToDecimal(match.Value) :  {Convert.ToDecimal(match.Value)}");
                }
                
            }

            return 0;
        }


        
        /// <summary>
        /// 현재 세션 ID 설정
        /// </summary>
        public static void SetCurrentSessionId(ObjectId sessionId)
        {
            _currentSessionId = sessionId;
            Debug.WriteLine($"현재 세션 ID 설정: {sessionId}");
        }

 

    }

}
