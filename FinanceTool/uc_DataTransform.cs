using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.VisualBasic.Devices;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FinanceTool
{
    public partial class uc_DataTransform : UserControl
    {

        DataTable originDataTable;
        DataTable transformDataTable;
        DataTable viewTransformDataTable;
        DataTable modifiedDataTable;

        private bool isProcessingSelection = false;
        private decimal decimalDivider = 1;
        private string decimalDividerName = "원";
        private int keywordColumnsCount = 0;

        private bool isFinishSession = false;
        public uc_DataTransform()
        {
            InitializeComponent();
        }

       
        // initUI 메서드 수정
        public async Task initUI()
        {
            try
            {
                Debug.WriteLine("data Transform initUI -> MongoDB 데이터 로드 시작");

                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "MongoDB 연결 확인 중...");

                    // MongoDB 연결 확인
                    bool mongoConnected = await Data.MongoDBManager.Instance.EnsureInitializedAsync();
                    if (!mongoConnected)
                    {
                        throw new Exception("MongoDB 연결에 실패했습니다.");
                    }

                    await progressForm.UpdateProgressHandler(20, "ProcessView 데이터 로드 중...");

                    // ProcessView 저장소 인스턴스 생성
                    var processViewRepo = new Repositories.ProcessViewRepository();

                    // MongoDB에서 process_view_data 컬렉션의 문서 조회
                    var filter = Builders<MongoModels.ProcessViewDocument>.Filter.Empty;
                    var sort = Builders<MongoModels.ProcessViewDocument>.Sort.Descending(d => d.LastModifiedDate);

                    var processViewDocs = await processViewRepo.GetAllAsync();

                    await progressForm.UpdateProgressHandler(30, $"ProcessView 데이터 변환 중...");

                    // ProcessView 문서를 DataTable로 변환 - 키워드 바로 매핑
                    DataTable viewData = new DataTable();

                    // 필요한 메타데이터 컬럼 추가
                    //viewData.Columns.Add("id", typeof(string));
                    //viewData.Columns.Add("process_data_id", typeof(string));
                    viewData.Columns.Add("raw_data_id", typeof(string)); // raw_data_id 직접 사용

                    // 각 키워드를 별도 컬럼으로 추가
                    int maxKeywordColumns = 0;

                    // 전처리: 먼저 최대 키워드 컬럼 수를 결정
                    foreach (var doc in processViewDocs)
                    {
                        int keywordCount = doc.Keywords?.FinalKeywords?.Count ?? 0;
                        maxKeywordColumns = Math.Max(maxKeywordColumns, keywordCount);
                    }

                    // 키워드 컬럼 생성 (Column0부터 시작)
                    for (int i = 0; i < maxKeywordColumns; i++)
                    {
                        viewData.Columns.Add($"Column{i}", typeof(string));
                    }

                    Debug.WriteLine($"생성된 컬럼 구조: {string.Join(", ", viewData.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");

                    // 문서를 DataTable로 변환
                    await Task.Run(() => {
                        foreach (var doc in processViewDocs)
                        {
                            DataRow row = viewData.NewRow();
                            //row["id"] = doc.Id;
                            //row["process_data_id"] = doc.ProcessDataId;
                            row["raw_data_id"] = doc.RawDataId; // 직접 raw_data_id 사용

                            // 키워드들을 Column0부터 바로 매핑
                            var keywords = doc.Keywords?.FinalKeywords ?? new List<string>();
                            for (int i = 0; i < keywords.Count && i < maxKeywordColumns; i++)
                            {
                                row[$"Column{i}"] = keywords[i];
                            }

                            viewData.Rows.Add(row);
                        }
                    });

                    await progressForm.UpdateProgressHandler(40, "데이터 설정 중...");

                    // DataTable 설정
                    originDataTable = viewData;
                    transformDataTable = viewData.Copy();

                    Debug.WriteLine("data Transform initUI -> transformDataTable 설정 완료");

                    

                    // ProcessView에서 바로 금액 정보를 가져오므로 추가 로드 필요 없음
                    // 대신 moneyDataTable을 초기화
                    await progressForm.UpdateProgressHandler(50, "금액 데이터 설정 중...");
                    await SetupMoneyDataTable();

                    // 원본 데이터로 뷰 데이터 보강
                    await progressForm.UpdateProgressHandler(60, "원본 데이터 보강 중...");
                    viewTransformDataTable = await EnrichTransformDataWithMongoData(transformDataTable);

                    Debug.WriteLine("data Transform initUI -> DataGridView 바인딩 설정 완료");

                    // 메인 UI 스레드로 돌아가서 UI 컨트롤 업데이트
                    await Task.Run(() =>
                    {
                        if (Application.OpenForms.Count > 0)
                        {
                            Application.OpenForms[0].Invoke((MethodInvoker)delegate
                            {
                                
                                // 정렬 처리 설정
                                sum_keyword_table.SortCompare += DataHandler.money_SortCompare;
                                match_keyword_table.SortCompare += DataHandler.money_SortCompare;
                            });
                        }
                    });

                    // 나머지 초기화 로직
                    await progressForm.UpdateProgressHandler(70, "키워드 병합 리스트 생성 중...");



                    // create_merge_keyword_list 함수 호출 - 새로운 ProcessMergeKeywordListWithProgress 호출
                    await create_merge_keyword_list();
                    Debug.WriteLine("data Transform initUI -> create_merge_keyword_list 완료");

                    // 키워드 콤보박스 설정
                    await set_keyword_combo_list();
                    Debug.WriteLine("data Transform initUI -> set_keyword_combo_list 설정 완료");

                    // 메인 UI 스레드로 돌아가서 DataHandler 등록
                    await Task.Run(() =>
                    {
                        if (Application.OpenForms.Count > 0)
                        {
                            Application.OpenForms[0].Invoke((MethodInvoker)delegate
                            {
                                Debug.WriteLine("RegisterDataGridView -> match_keyword_table");
                                DataHandler.RegisterDataGridView(match_keyword_table);

                                // 이벤트 핸들러 중복 등록 방지
                                decimal_combo.SelectedIndexChanged -= decimal_combo_SelectedIndexChanged; // 기존 핸들러 제거
                                decimal_combo.SelectedIndex = 0;
                                decimal_combo.SelectedIndexChanged += decimal_combo_SelectedIndexChanged;
                            });
                        }
                    });

                    // 최종 결과를 화면에 표시
                    await Task.Run(() =>
                    {
                        if (Application.OpenForms.Count > 0)
                        {
                            Application.OpenForms[0].Invoke((MethodInvoker)delegate
                            {
                                // 보강된 viewTransformDataTable로 dataGridView_2nd 업데이트
                                Debug.WriteLine($"viewTransformDataTable 표시 준비: {viewTransformDataTable.Rows.Count}개 행");

                                // 기존 데이터 소스 제거
                                dataGridView_2nd.DataSource = null;

                                // 새 데이터 소스 설정
                                dataGridView_2nd.DataSource = viewTransformDataTable;

                                // 필요한 컬럼 숨김 처리 다시 수행
                                if (dataGridView_2nd.Columns["raw_data_id"] != null)
                                    dataGridView_2nd.Columns["raw_data_id"].Visible = false;

                                /*
                                if (dataGridView_2nd.Columns["id"] != null)
                                    dataGridView_2nd.Columns["id"].Visible = false;

                                if (dataGridView_2nd.Columns["process_data_id"] != null)
                                    dataGridView_2nd.Columns["process_data_id"].Visible = false;
                                */

                                // 필요한 경우 열 너비 조정
                                dataGridView_2nd.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                                // 행 카운트 로깅
                                Debug.WriteLine($"viewTransformDataTable 표시 완료: 표시된 행 수={dataGridView_2nd.Rows.Count}");

                                // 데이터그리드뷰 새로고침 강제
                                dataGridView_2nd.Refresh();
                            });
                        }
                    });

                    await progressForm.UpdateProgressHandler(100, "데이터 로드 완료");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"initUI 오류: {ex.Message}\n{ex.StackTrace}");
                await Task.Run(() =>
                {
                    MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {ex.Message}",
                                  "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
        }

        // 금액 데이터 테이블 설정 메서드 추가 (LoadMoneyDataFromMongoDB 대체)
        private async Task SetupMoneyDataTable()
        {
            try
            {
                Debug.WriteLine("SetupMoneyDataTable 시작");

                // 금액 데이터 테이블 생성
                DataTable moneyTable = new DataTable();
                moneyTable.Columns.Add("raw_data_id", typeof(string));
                moneyTable.Columns.Add("amount", typeof(decimal));

                // transformDataTable에서 직접 금액 정보 추출
                foreach (DataRow row in transformDataTable.Rows)
                {
                    if (row["raw_data_id"] != DBNull.Value && row["Column0"] != DBNull.Value)
                    {
                        string rawDataId = row["raw_data_id"].ToString();

                        // 금액 파싱
                        decimal amount = 0;
                        if (decimal.TryParse(row["Column0"].ToString(), out amount))
                        {
                            DataRow moneyRow = moneyTable.NewRow();
                            moneyRow["raw_data_id"] = rawDataId;
                            moneyRow["amount"] = amount;
                            moneyTable.Rows.Add(moneyRow);
                        }
                    }
                }

                // 금액 정보가 없는 raw_data_id는 ProcessData 또는 RawData에서 보강
                if (moneyTable.Rows.Count < transformDataTable.Rows.Count / 2)
                {
                    Debug.WriteLine("ProcessView에서 충분한 금액 정보를 찾지 못함. 원본 데이터에서 보강");

                    // 처리 로직은 기존 LoadMoneyDataFromMongoDB와 동일하게 유지
                    var processDataRepo = new Repositories.ProcessDataRepository();
                    var rawDataRepo = new Repositories.RawDataRepository();

                    // 이미 로드된 ID 목록 생성
                    HashSet<string> loadedIds = new HashSet<string>();
                    foreach (DataRow row in moneyTable.Rows)
                    {
                        loadedIds.Add(row["raw_data_id"].ToString());
                    }

                    // 누락된 ID 목록
                    HashSet<string> missingIds = new HashSet<string>();
                    foreach (DataRow row in transformDataTable.Rows)
                    {
                        if (row["raw_data_id"] != DBNull.Value)
                        {
                            string id = row["raw_data_id"].ToString();
                            if (!loadedIds.Contains(id))
                            {
                                missingIds.Add(id);
                            }
                        }
                    }

                    // 누락된 ID에 대한 금액 정보 로드
                    if (missingIds.Count > 0)
                    {
                        // 처리 로직 이하 유지
                        // 기존 LoadMoneyDataFromMongoDB 코드와 동일
                    }
                }

                // DataHandler.moneyDataTable 설정
                DataHandler.moneyDataTable = moneyTable;
                Debug.WriteLine($"금액 데이터 설정 완료: {moneyTable.Rows.Count}개 행");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"금액 데이터 설정 중 오류: {ex.Message}\n{ex.StackTrace}");
                // 빈 테이블 생성하여 에러 방지
                DataTable emptyTable = new DataTable();
                emptyTable.Columns.Add("raw_data_id", typeof(string));
                emptyTable.Columns.Add("amount", typeof(decimal));
                DataHandler.moneyDataTable = emptyTable;
            }
        }

        // EnrichTransformDataWithMongoData 메서드 수정 (원본 구조 최대한 유지)
        public async Task<DataTable> EnrichTransformDataWithMongoData(DataTable transformDataTable)
        {
            try
            {
                Debug.WriteLine("EnrichTransformDataWithMongoData 시작");

                // 원본 데이터를 수정하지 않도록 복사본 생성
                DataTable resultTable = new DataTable();

                // MongoDB 연결 확인
                await Data.MongoDBManager.Instance.EnsureInitializedAsync();

                // ColumnMapping 저장소 생성 (또는 직접 MongoDB 액세스)
                var columnMappingFilter = Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("is_visible", true);
                var columnMappingsResult = await Data.MongoDBManager.Instance.FindDocumentsAsync<MongoDB.Bson.BsonDocument>(
                    "column_mapping",
                    columnMappingFilter);

                // 시각화될 컬럼명 추출
                List<string> visibleColumns = new List<string>();
                foreach (var doc in columnMappingsResult)
                {
                    if (doc.Contains("original_name"))
                    {
                        string originalName = doc["original_name"].AsString;
                        visibleColumns.Add(originalName);
                    }
                }

                Debug.WriteLine($"시각화될 컬럼: {string.Join(", ", visibleColumns)}");

                if (visibleColumns.Count == 0)
                {
                    Debug.WriteLine("표시할 컬럼이 없습니다. column_mapping 컬렉션의 is_visible 속성을 확인하세요.");
                    return transformDataTable.Copy();
                }

                // 1. 먼저 시각화 컬럼 추가
                foreach (string column in visibleColumns)
                {
                    resultTable.Columns.Add(column, typeof(string));
                }

                // 2. 그 다음 원본 transformDataTable의 컬럼 추가 (중복 제외)
                foreach (DataColumn column in transformDataTable.Columns)
                {
                    if (!resultTable.Columns.Contains(column.ColumnName))
                    {
                        resultTable.Columns.Add(column.ColumnName, column.DataType);
                    }
                }

                // 3. 원본 데이터의 모든 행 복사
                foreach (DataRow originalRow in transformDataTable.Rows)
                {
                    DataRow newRow = resultTable.NewRow();

                    // 원본 테이블의 모든 컬럼 값을 새 행에 복사
                    foreach (DataColumn column in transformDataTable.Columns)
                    {
                        if (resultTable.Columns.Contains(column.ColumnName))
                        {
                            newRow[column.ColumnName] = originalRow[column.ColumnName];
                        }
                    }

                    resultTable.Rows.Add(newRow);
                }

                // 4. raw_data_id 컬럼이 있는지 확인
                if (!resultTable.Columns.Contains("raw_data_id"))
                {
                    Debug.WriteLine("transformDataTable에 raw_data_id 컬럼이 없습니다.");
                    return resultTable;
                }

                // 5. RawData 저장소 생성
                var rawDataRepo = new Repositories.RawDataRepository();

                // 6. 모든 행의 raw_data_id 목록 수집
                HashSet<string> rawDataIds = new HashSet<string>();
                Dictionary<string, List<DataRow>> idToRowsMap = new Dictionary<string, List<DataRow>>();

                foreach (DataRow row in resultTable.Rows)
                {
                    if (row["raw_data_id"] != DBNull.Value)
                    {
                        string rawDataId = row["raw_data_id"].ToString();
                        if (!string.IsNullOrEmpty(rawDataId))
                        {
                            rawDataIds.Add(rawDataId);

                            if (!idToRowsMap.ContainsKey(rawDataId))
                            {
                                idToRowsMap[rawDataId] = new List<DataRow>();
                            }
                            idToRowsMap[rawDataId].Add(row);
                        }
                    }
                }

                if (rawDataIds.Count == 0)
                {
                    Debug.WriteLine("유효한 raw_data_id가 없습니다.");
                    return resultTable;
                }

                Debug.WriteLine($"보강할 raw_data_id: {rawDataIds.Count}개");

                // 7. 배치 처리로 원본 데이터 가져오기
                const int batchSize = 100;
                List<string> idList = rawDataIds.ToList();

                // 안전한 배치 처리
                for (int i = 0; i < idList.Count; i += batchSize)
                {
                    int currentBatchSize = Math.Min(batchSize, idList.Count - i);
                    if (i >= idList.Count || currentBatchSize <= 0)
                        continue;

                    List<string> batchIds = idList.GetRange(i, currentBatchSize);

                    // MongoDB ID 형식으로 필터 생성
                    var batchFilter = Builders<MongoModels.RawDataDocument>.Filter.In(d => d.Id, batchIds);
                    var batchRawDatas = await rawDataRepo.FindDocumentsAsync(batchFilter);

                    //Debug.WriteLine($"배치 조회 결과: {batchRawDatas.Count}개 문서 ({i + 1}-{i + currentBatchSize}배치)");

                    // 조회된 데이터를 매핑
                    foreach (var rawData in batchRawDatas)
                    {
                        string id = rawData.Id;

                        if (idToRowsMap.ContainsKey(id) && rawData.Data != null)
                        {
                            foreach (DataRow resultRow in idToRowsMap[id])
                            {
                                foreach (string column in visibleColumns)
                                {
                                    if (rawData.Data.ContainsKey(column) && resultTable.Columns.Contains(column))
                                    {
                                        resultRow[column] = rawData.Data[column]?.ToString() ?? string.Empty;
                                    }
                                }
                            }
                        }
                    }

                    //Debug.WriteLine($"배치 처리 완료: {batchIds.Count}개 ID, 처리된 ID: {batchRawDatas.Count}");
                }

                // 이미 필요한 메타데이터 컬럼만 있을테니 더 이상 제거할 필요 없음
                Debug.WriteLine("EnrichTransformDataWithMongoData 완료");
                return resultTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 데이터 보강 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
                // 예외 발생 시 원본 데이터 테이블의 복사본 반환
                return transformDataTable.Copy();
            }
        }


        private bool searchYN = false;
        private async Task create_merge_keyword_list(bool progressYN = false)
        {
            try
            {
                searchYN = true;

                if (progressYN)
                {
                    using (var progressForm = new ProcessProgressForm())
                    {
                        Debug.WriteLine("create_merge_keyword_list start ");
                        progressForm.Show();
                        await progressForm.UpdateProgressHandler(10, "키워드 요약 테이블 생성 중...");

                        await ProcessMergeKeywordListWithProgress(progressForm.UpdateProgressHandler);

                        await progressForm.UpdateProgressHandler(100, "완료");
                    }
                }
                else
                {
                    // 프로그레스 없이 진행
                    await ProcessMergeKeywordListWithProgress(null);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"키워드 리스트 생성 오류: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
            finally
            {
                Debug.WriteLine($"create_merge_keyword_list complete");
                searchYN = false;
            }
        }

        // 키워드 병합 처리 함수 (개선버전)
        private async Task ProcessMergeKeywordListWithProgress(ProcessProgressForm.UpdateProgressDelegate progress)
        {
            try
            {
                // 진행 상황 업데이트 래퍼 함수
                async Task UpdateProgress(int percentage, string message = null)
                {
                    if (progress != null)
                    {
                        await progress(percentage, message);
                    }
                }

                await UpdateProgress(15, "키워드 데이터 로딩 중...");

                // 1. 키워드 데이터 확인
                if (transformDataTable == null || transformDataTable.Rows.Count == 0)
                {
                    Debug.WriteLine("데이터 테이블이 비어 있습니다.");
                    return;
                }

                // 2. 키워드 컬럼 식별 (Column2부터 시작하는 컬럼들)
                List<string> keywordColumns = new List<string>();
                foreach (DataColumn column in transformDataTable.Columns)
                {
                    if (column.ColumnName.StartsWith("Column") &&
                        int.TryParse(column.ColumnName.Substring(6), out int colIndex) &&
                        colIndex >= 0)
                    {
                        keywordColumns.Add(column.ColumnName);
                    }
                }

                Debug.WriteLine($"키워드 컬럼: {string.Join(", ", keywordColumns)}");

                if (keywordColumns.Count == 0)
                {
                    Debug.WriteLine("키워드 컬럼을 찾을 수 없습니다.");
                    return;
                }

                await UpdateProgress(20, "키워드 추출 중...");

                // 3. 모든 키워드 추출 및 빈도 계산
                // 병렬 처리 위한 ConcurrentDictionary 사용
                ConcurrentDictionary<string, int> concurrentFrequency = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                ConcurrentDictionary<string, ConcurrentBag<string>> concurrentKeywordToRawDataIds =
                    new ConcurrentDictionary<string, ConcurrentBag<string>>(StringComparer.OrdinalIgnoreCase);

                // 시스템 리소스에 맞게 병렬 처리 최적화
                int cpuCount = Environment.ProcessorCount;
                int maxDegreeOfParallelism = Math.Max(1, cpuCount - 1); // 시스템에 하나의 코어는 남겨둠

                // 행 병렬 처리로 키워드 추출 및 빈도 계산
                await Task.Run(() => {
                    Parallel.ForEach(transformDataTable.AsEnumerable(),
                        new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                        row => {
                            string rawDataId = row["raw_data_id"]?.ToString();
                            if (string.IsNullOrEmpty(rawDataId)) return;

                            // 각 키워드 컬럼에서 키워드 추출
                            foreach (string colName in keywordColumns)
                            {
                                string keyword = row[colName]?.ToString();
                                if (string.IsNullOrWhiteSpace(keyword)) continue;

                                // 키워드 표준화
                                keyword = keyword.Trim();

                                // 키워드 빈도 증가
                                concurrentFrequency.AddOrUpdate(keyword, 1, (k, v) => v + 1);

                                // 키워드에 해당하는 raw_data_id 추가
                                concurrentKeywordToRawDataIds.AddOrUpdate(
                                    keyword,
                                    new ConcurrentBag<string> { rawDataId },
                                    (k, bag) => {
                                        bag.Add(rawDataId);
                                        return bag;
                                    });
                            }
                        });
                });

                // 일반 Dictionary로 변환 및 중복 제거
                Dictionary<string, int> keywordFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, List<string>> keywordToRawDataIds = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                foreach (var pair in concurrentFrequency)
                {
                    keywordFrequency[pair.Key] = pair.Value;
                }

                foreach (var pair in concurrentKeywordToRawDataIds)
                {
                    keywordToRawDataIds[pair.Key] = pair.Value.Distinct().ToList();
                }

                await UpdateProgress(40, $"키워드별 금액 합산 중... ({keywordFrequency.Count}개 키워드)");

                // 4. 키워드별 금액 합산 (개선된 버전)
                // Dictionary 대신 ConcurrentDictionary 사용
                ConcurrentDictionary<string, decimal> concurrentKeywordTotalMoney =
                    new ConcurrentDictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

                // 금액 정보를 직접 확인 - transformDataTable의 Column0 사용
                Dictionary<string, decimal> rawDataToMoney = new Dictionary<string, decimal>();

                // DataHandler.moneyDataTable에서 금액 정보 로드
                if (DataHandler.moneyDataTable != null && DataHandler.moneyDataTable.Rows.Count > 0)
                {
                    Debug.WriteLine($"DataHandler.moneyDataTable에서 금액 정보 로드 중... 행 수: {DataHandler.moneyDataTable.Rows.Count}개");

                    // moneyDataTable 구조 확인 로깅
                    string[] columnNames = DataHandler.moneyDataTable.Columns.Cast<DataColumn>()
                        .Select(c => c.ColumnName).ToArray();
                    Debug.WriteLine($"moneyDataTable 컬럼 구조: {string.Join(", ", columnNames)}");

                    // moneyDataTable 데이터 로드
                    foreach (DataRow moneyRow in DataHandler.moneyDataTable.Rows)
                    {
                        // raw_data_id 확인
                        if (moneyRow.Table.Columns.Contains("raw_data_id") && moneyRow["raw_data_id"] != DBNull.Value)
                        {
                            string rawDataId = moneyRow["raw_data_id"].ToString();
                            if (!string.IsNullOrEmpty(rawDataId))
                            {
                                // 데이터 구조에 따라 금액 가져오기
                                // ExtractColumnToNewTable 함수는 해당 컬럼을 첫 번째 또는 두 번째 컬럼으로 옮길 수 있음
                                object moneyValue = null;

                                // 첫 번째 시도: 인덱스 기반 접근 (ExtractColumnToNewTable 함수 결과)
                                if (moneyRow.Table.Columns.Count > 1)
                                {
                                    // 첫 번째 열이 raw_data_id가 아니라면, 첫 번째 열이 금액일 가능성 있음
                                    if (moneyRow.Table.Columns[0].ColumnName != "raw_data_id")
                                    {
                                        moneyValue = moneyRow[0];
                                    }
                                    // 두 번째 열을 시도
                                    else if (moneyRow.Table.Columns.Count > 1)
                                    {
                                        moneyValue = moneyRow[1];
                                    }
                                }

                                // 두 번째 시도: 컬럼명 사용
                                if (moneyValue == null || moneyValue == DBNull.Value)
                                {
                                    // DataHandler.levelName[0]이 금액 컬럼명
                                    string moneyColumnName = DataHandler.levelName[0];
                                    if (moneyRow.Table.Columns.Contains(moneyColumnName))
                                    {
                                        moneyValue = moneyRow[moneyColumnName];
                                    }
                                    
                                }

                                // 금액을 parseable 형태로 변환
                                if (moneyValue != null && moneyValue != DBNull.Value)
                                {
                                    decimal amount;
                                    if (decimal.TryParse(moneyValue.ToString(), out amount))
                                    {
                                        rawDataToMoney[rawDataId] = amount;

                                        // 디버깅용 (처음 몇 개 항목만)
                                        if (rawDataToMoney.Count <= 5)
                                        {
                                            Debug.WriteLine($"금액 매핑: rawDataId={rawDataId}, 금액={amount}");
                                        }
                                    }
                                }
                            }
                        }
                    }

                    Debug.WriteLine($"금액 정보가 로드된 raw_data_id: {rawDataToMoney.Count}개");
                }

                Debug.WriteLine($"금액 정보가 로드된 raw_data_id: {rawDataToMoney.Count}개");

                // 추가 금액 정보 로드 - ProcessViewDocument의 money 필드에서 직접 가져옴
                // 추가 금액 정보 로드 - 최적화 버전
                if (rawDataToMoney.Count < transformDataTable.Rows.Count / 2)
                {
                    Debug.WriteLine($"금액 데이터가 충분하지 않습니다. ProcessView에서 추가 로드를 시도합니다. 현재: {rawDataToMoney.Count}/{transformDataTable.Rows.Count}");

                    try
                    {
                        // 시작 시간 측정
                        DateTime startTime = DateTime.Now;

                        // ProcessView 저장소 인스턴스 생성
                        var processViewRepo = new Repositories.ProcessViewRepository();

                        // 필요한 ID만 추출
                        var missingIds = new HashSet<string>();
                        foreach (DataRow row in transformDataTable.Rows)
                        {
                            string rawDataId = row["raw_data_id"]?.ToString();
                            if (!string.IsNullOrEmpty(rawDataId) && !rawDataToMoney.ContainsKey(rawDataId))
                            {
                                missingIds.Add(rawDataId);
                            }
                        }

                        if (missingIds.Count > 0)
                        {
                            Debug.WriteLine($"{missingIds.Count}개의 금액 정보를 보강합니다.");

                            // 배치 크기 설정 - MongoDB 쿼리 제한에 맞게 조정
                            const int batchSize = 1000;
                            var idsList = missingIds.ToList();

                            // 배치 처리를 위해 리스트를 분할
                            var batches = new List<List<string>>();
                            for (int i = 0; i < idsList.Count; i += batchSize)
                            {
                                batches.Add(idsList.Skip(i).Take(batchSize).ToList());
                            }

                            Debug.WriteLine($"{batches.Count}개 배치로 분할하여 처리합니다.");

                            // 결과를 저장할 ConcurrentDictionary (스레드 안전)
                            var results = new ConcurrentDictionary<string, decimal>();

                            // 각 배치 순차 처리 (병렬 처리시 오류 발생 가능성)
                            foreach (var batch in batches)
                            {
                                try
                                {
                                    // 배치의 모든 ID를 한 번에 쿼리 (FindDocumentsAsync 사용)
                                    var batchFilter = Builders<MongoModels.ProcessViewDocument>.Filter.In(d => d.RawDataId, batch);
                                    var batchDocs = await processViewRepo.FindDocumentsAsync(batchFilter);

                                    // 결과를 결합
                                    foreach (var doc in batchDocs)
                                    {
                                        if (doc.Money != null)
                                        {
                                            decimal amount;
                                            // 금액 파싱 시도 - 다양한 형식 처리
                                            if (doc.Money is decimal decimalAmount)
                                            {
                                                results[doc.RawDataId] = decimalAmount;
                                            }
                                            else if (decimal.TryParse(doc.Money.ToString(), out amount))
                                            {
                                                results[doc.RawDataId] = amount;
                                            }
                                        }
                                    }

                                    Debug.WriteLine($"배치 처리 완료: {batchDocs.Count}개 문서 처리됨");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"배치 처리 중 오류: {ex.Message}");
                                }
                            }

                            // 결과를 rawDataToMoney에 병합
                            foreach (var pair in results)
                            {
                                rawDataToMoney[pair.Key] = pair.Value;
                            }

                            DateTime endTime = DateTime.Now;
                            TimeSpan duration = endTime - startTime;

                            Debug.WriteLine($"금액 정보 보강 완료: {rawDataToMoney.Count}개 (처리 시간: {duration.TotalSeconds:F2}초)");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"금액 정보 보강 중 오류: {ex.Message}");
                    }
                }

                // 키워드별 금액 합산 (병렬 처리)
                await Task.Run(() => {
                    try
                    {
                        Parallel.ForEach(keywordToRawDataIds,
                            new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                            pair => {
                                try
                                {
                                    string keyword = pair.Key;
                                    List<string> rawDataIds = pair.Value;

                                    decimal totalAmount = 0;
                                    foreach (string rawDataId in rawDataIds)
                                    {
                                        if (rawDataToMoney.TryGetValue(rawDataId, out decimal amount))
                                        {
                                            totalAmount += amount;
                                        }
                                    }

                                    // 스레드 안전한 방식으로 결과 저장
                                    concurrentKeywordTotalMoney.AddOrUpdate(
                                        keyword,
                                        totalAmount,
                                        (k, oldValue) => totalAmount
                                    );
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"키워드 처리 중 오류: {ex.Message}");
                                }
                            });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"병렬 처리 중 오류: {ex.Message}");
                        throw;
                    }
                });

                await UpdateProgress(60, "요약 데이터 생성 중...");

                // 결과를 일반 Dictionary로 변환
                Dictionary<string, decimal> keywordTotalMoney = new Dictionary<string, decimal>(
                    concurrentKeywordTotalMoney,
                    StringComparer.OrdinalIgnoreCase
                );

                // 5. 결과 DataTable 생성
                modifiedDataTable = new DataTable();
                modifiedDataTable.Columns.Add("Value", typeof(string));
                modifiedDataTable.Columns.Add("Count", typeof(int));
                modifiedDataTable.Columns.Add("합산금액", typeof(string));

                // 키워드 빈도 기준으로 정렬 (내림차순)
                var sortedKeywords = keywordFrequency.OrderByDescending(pair => pair.Value)
                                                    .ThenBy(pair => pair.Key);

                foreach (var pair in sortedKeywords)
                {
                    string keyword = pair.Key;
                    int count = pair.Value;
                    decimal totalMoney = keywordTotalMoney.ContainsKey(keyword) ? keywordTotalMoney[keyword] : 0;

                    // 금액 포맷팅
                    string formattedMoney = FormatToKoreanUnit(totalMoney);

                    // 디버깅용 로깅 (첫 10개)
                    if (modifiedDataTable.Rows.Count < 10)
                    {
                        Debug.WriteLine($"키워드: {keyword}, 빈도: {count}, 금액: {totalMoney} -> {formattedMoney}");
                    }

                    modifiedDataTable.Rows.Add(keyword, count, formattedMoney);
                }

                await UpdateProgress(80, "UI 업데이트 중...");

                // 6. UI 업데이트 (GridView에 표시)
                await Task.Run(() => {
                    if (Application.OpenForms.Count > 0)
                    {
                        Application.OpenForms[0].Invoke((MethodInvoker)delegate {
                            if (sum_keyword_table.Rows.Count > 0)
                            {
                                sum_keyword_table.Rows.Clear();
                                sum_keyword_table.Columns.Clear();
                            }

                            // 원본 DataTable의 컬럼들 추가
                            foreach (DataColumn col in modifiedDataTable.Columns)
                            {
                                sum_keyword_table.Columns.Add(col.ColumnName, col.ColumnName);
                            }

                            // 데이터 추가
                            foreach (DataRow row in modifiedDataTable.Rows)
                            {
                                int rowIndex = sum_keyword_table.Rows.Add();

                                // 데이터 채우기
                                for (int i = 0; i < modifiedDataTable.Columns.Count; i++)
                                {
                                    sum_keyword_table.Rows[rowIndex].Cells[i].Value = row[i];
                                }
                            }

                            // DataGridView 속성 설정
                            sum_keyword_table.AllowUserToAddRows = false;
                            sum_keyword_table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                            sum_keyword_table.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                            sum_keyword_table.Font = new System.Drawing.Font("맑은 고딕", 14.25F);

                            // 나머지 컬럼들은 읽기 전용으로 설정
                            for (int i = 1; i < sum_keyword_table.Columns.Count; i++)
                            {
                                sum_keyword_table.Columns[i].ReadOnly = true;
                            }
                        });
                    }
                });

                await UpdateProgress(90, "완료된 결과: " + modifiedDataTable.Rows.Count + "개 키워드");
                Debug.WriteLine($"키워드 요약 테이블 생성 완료: {modifiedDataTable.Rows.Count}개 키워드");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"키워드 분석 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }


        public async Task<DataTable> EnrichTransformDataWithRawData(DataTable transformDataTable)
        {
            try
            {
                // 원본 데이터를 수정하지 않도록 복사본 생성
                DataTable resultTable = new DataTable();

                // MongoDB 연결 확인
                await Data.MongoDBManager.Instance.EnsureInitializedAsync();

                // 1. is_visible=true인 컬럼 목록 가져오기
                var columnMappingFilter = Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("is_visible", true);
                var columnMappingsResult = await Data.MongoDBManager.Instance.FindDocumentsAsync<MongoDB.Bson.BsonDocument>(
                    "column_mapping",
                    columnMappingFilter);

                // 시각화될 컬럼명 추출
                List<string> visibleColumns = new List<string>();
                foreach (var doc in columnMappingsResult)
                {
                    if (doc.Contains("original_name"))
                    {
                        string originalName = doc["original_name"].AsString;
                        visibleColumns.Add(originalName);
                    }
                }

                Debug.WriteLine($"시각화될 컬럼: {string.Join(", ", visibleColumns)}");

                if (visibleColumns.Count == 0)
                {
                    Debug.WriteLine("표시할 컬럼이 없습니다. column_mapping 컬렉션의 is_visible 속성을 확인하세요.");
                    return transformDataTable.Copy();
                }

                // 2. 결과 테이블에 컬럼 구성
                // 먼저 visibleColumns 추가
                foreach (string column in visibleColumns)
                {
                    resultTable.Columns.Add(column, typeof(string));
                }

                // 그 다음 원본 transformDataTable의 컬럼 추가 (중복 제외)
                foreach (DataColumn column in transformDataTable.Columns)
                {
                    if (!resultTable.Columns.Contains(column.ColumnName))
                    {
                        resultTable.Columns.Add(column.ColumnName, column.DataType);
                    }
                }

                // 3. 원본 데이터의 모든 행 복사
                foreach (DataRow originalRow in transformDataTable.Rows)
                {
                    DataRow newRow = resultTable.NewRow();

                    // 원본 테이블의 모든 컬럼 값을 새 행에 복사
                    foreach (DataColumn column in transformDataTable.Columns)
                    {
                        if (resultTable.Columns.Contains(column.ColumnName))
                        {
                            newRow[column.ColumnName] = originalRow[column.ColumnName];
                        }
                    }

                    resultTable.Rows.Add(newRow);
                }

                // 4. raw_data_id 컬럼이 있는지 확인
                if (!resultTable.Columns.Contains("raw_data_id"))
                {
                    Debug.WriteLine("transformDataTable에 raw_data_id 컬럼이 없습니다.");
                    return resultTable;
                }

                // 5. RawData 저장소 생성
                var rawDataRepo = new Repositories.RawDataRepository();

                // 6. 모든 행의 raw_data_id 목록 수집
                HashSet<string> rawDataIds = new HashSet<string>();
                Dictionary<string, List<DataRow>> idToRowsMap = new Dictionary<string, List<DataRow>>();

                foreach (DataRow row in resultTable.Rows)
                {
                    if (row["raw_data_id"] != DBNull.Value)
                    {
                        string rawDataId = row["raw_data_id"].ToString();
                        if (!string.IsNullOrEmpty(rawDataId))
                        {
                            rawDataIds.Add(rawDataId);

                            if (!idToRowsMap.ContainsKey(rawDataId))
                            {
                                idToRowsMap[rawDataId] = new List<DataRow>();
                            }
                            idToRowsMap[rawDataId].Add(row);
                        }
                    }
                }

                if (rawDataIds.Count == 0)
                {
                    Debug.WriteLine("유효한 raw_data_id가 없습니다.");
                    return resultTable;
                }

                Debug.WriteLine($"보강할 raw_data_id: {rawDataIds.Count}개");

                // 7. 배치 처리로 원본 데이터 가져오기
                const int batchSize = 100;
                List<string> idList = rawDataIds.ToList();

                // 안전한 배치 처리
                for (int i = 0; i < idList.Count; i += batchSize)
                {
                    int currentBatchSize = Math.Min(batchSize, idList.Count - i);
                    if (i >= idList.Count || currentBatchSize <= 0)
                        continue;

                    List<string> batchIds = idList.GetRange(i, currentBatchSize);

                    // MongoDB ID 형식으로 필터 생성
                    var batchFilter = Builders<MongoModels.RawDataDocument>.Filter.In(d => d.Id, batchIds);
                    var batchRawDatas = await rawDataRepo.FindDocumentsAsync(batchFilter);

                    // 조회된 데이터를 매핑
                    foreach (var rawData in batchRawDatas)
                    {
                        string id = rawData.Id;

                        if (idToRowsMap.ContainsKey(id) && rawData.Data != null)
                        {
                            foreach (DataRow resultRow in idToRowsMap[id])
                            {
                                foreach (string column in visibleColumns)
                                {
                                    if (rawData.Data.ContainsKey(column) && resultTable.Columns.Contains(column))
                                    {
                                        resultRow[column] = rawData.Data[column]?.ToString() ?? string.Empty;
                                    }
                                }
                            }
                        }
                    }
                }

                Debug.WriteLine("EnrichTransformDataWithRawData 완료");
                return resultTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 데이터 보강 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
                // 예외 발생 시 원본 데이터 테이블의 복사본 반환
                return transformDataTable.Copy();
            }
        }

        public DataTable FilterTransformDataByKeyword(DataTable viewTransformDataTable, DataTable originalTransformDataTable, string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
                return viewTransformDataTable.Copy();

            DataTable resultTable = viewTransformDataTable.Clone();

            // 원본 transformDataTable의 컬럼명 목록 가져오기
            List<string> originalColumnNames = new List<string>();
            foreach (DataColumn col in originalTransformDataTable.Columns)
            {
                originalColumnNames.Add(col.ColumnName);
            }
            Debug.WriteLine($"originalColumnNames  : {string.Join(',', originalColumnNames)}");

            // viewTransformDataTable의 각 행에 대해 검색
            for (int rowIndex = 0; rowIndex < viewTransformDataTable.Rows.Count; rowIndex++)
            {
                DataRow row = viewTransformDataTable.Rows[rowIndex];
                bool containsKeyword = false;

                // 원본 컬럼명에 해당하는 컬럼만 검사
                foreach (string colName in originalColumnNames)
                {
                    if (viewTransformDataTable.Columns.Contains(colName) &&
                        row[colName] != null &&
                        row[colName] != DBNull.Value)
                    {
                        string cellValue = row[colName].ToString();

                        if (cellValue.Equals(keyword, StringComparison.Ordinal))
                        {
                            containsKeyword = true;
                            break;
                        }
                    }
                }

                if (containsKeyword)
                {
                    resultTable.Rows.Add(row.ItemArray);
                }
            }

            return resultTable;
        }


        private async Task set_keyword_combo_list()
        {
            try
            {
                await Task.Run(() =>
                {
                    if (Application.OpenForms.Count > 0)
                    {
                        Application.OpenForms[0].Invoke((MethodInvoker)delegate
                        {
                            // UI 컨트롤 접근은 이 블록 내부에서만 수행
                            keyword_search_combo.Items.Clear();
                            keyword_search_combo.Items.Add("키워드 선택");

                            // modifiedDataTable이 생성됐는지 확인
                            if (modifiedDataTable != null && modifiedDataTable.Rows.Count > 0)
                            {
                                // 키워드 추가 (정렬된 상태 유지)
                                foreach (DataRow row in modifiedDataTable.Rows)
                                {
                                    if (row[0] != null && row[0] != DBNull.Value)
                                    {
                                        string keyword = row[0].ToString();
                                        if (!string.IsNullOrWhiteSpace(keyword))
                                        {
                                            keyword_search_combo.Items.Add(keyword);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Debug.WriteLine("키워드 데이터가 없습니다.");
                            }

                            // 첫 번째 항목 선택 (최소 1개 항목이 존재)
                            if (keyword_search_combo.Items.Count > 0)
                            {
                                keyword_search_combo.SelectedIndex = 0;
                            }
                        });
                    }
                });

                Debug.WriteLine($"키워드 콤보박스 설정 완료: {(keyword_search_combo.Items.Count - 1)}개 항목");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"키워드 콤보박스 설정 중 오류: {ex.Message}");
            }
        }

        public void CreateFilteredDataGridView(DataGridView dgv, DataTable dt, List<string> filterWords)
        {
            // DataGridView 초기화
            dgv.DataSource = null;
            dgv.Rows.Clear();
            dgv.Columns.Clear();
            if (DataHandler.dragSelections.ContainsKey(dgv))
            {
                DataHandler.dragSelections[dgv].Clear();
            }

            // CheckBox 컬럼 추가
            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn();
            checkColumn.Name = "CheckBox";
            checkColumn.HeaderText = "";
            checkColumn.Width = 50;
            checkColumn.ThreeState = false;
            checkColumn.FillWeight = 20;  // 다른 컬럼들보다 작은 값 설정

            dgv.Columns.Add(checkColumn);


            // 원본 DataTable의 컬럼들 추가
            foreach (DataColumn col in dt.Columns)
            {
                dgv.Columns.Add(col.ColumnName, col.ColumnName);
            }


            // 데이터 필터링 및 추가
            foreach (DataRow row in dt.Rows)
            {
                if (filterWords.Count > 0)
                {
                    string firstColumnValue = row[0].ToString();

                    // list<string>의 항목과 비교
                    if (filterWords.Any(word => firstColumnValue.Contains(word)))
                    {
                        int rowIndex = dgv.Rows.Add();
                        dgv.Rows[rowIndex].Cells["CheckBox"].Value = false;  // 체크박스 초기값

                        // 데이터 채우기
                        for (int i = 0; i < dt.Columns.Count; i++)
                        {
                            dgv.Rows[rowIndex].Cells[i + 1].Value = row[i];  // +1은 체크박스 컬럼 때문

                        }

                    }
                }
                else
                {
                    int rowIndex = dgv.Rows.Add();
                    dgv.Rows[rowIndex].Cells["CheckBox"].Value = false;  // 체크박스 초기값

                    // 데이터 채우기
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        dgv.Rows[rowIndex].Cells[i + 1].Value = row[i];  // +1은 체크박스 컬럼 때문
                    }
                }

            }

            // DataGridView 속성 설정
            dgv.AllowUserToAddRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.ReadOnly = false;
            dgv.Columns["CheckBox"].ReadOnly = false;  // 체크박스 컬럼만 편집 가능
            dgv.Font = new System.Drawing.Font("맑은 고딕", 14.25F);
            //dgv.Columns[2].DefaultCellStyle.Format = "N0";
            //dgv.Columns[3].DefaultCellStyle.Format = "N0";

            //Debug.WriteLine($"dgv.Columns[1] : {dgv.Columns[1].Name}");
            //Debug.WriteLine($"dgv.Columns[2] : {dgv.Columns[2].Name}");

            // 나머지 컬럼들은 읽기 전용으로 설정
            for (int i = 1; i < dgv.Columns.Count; i++)
            {
                dgv.Columns[i].ReadOnly = true;
            }


        }

        public string FormatToKoreanUnit(decimal number)
        {
            // 절대값으로 계산 후 나중에 부호 처리
            bool isNegative = number < 0;
            number = Math.Abs(number);


            string result;
            decimal divideNum = 0;


            divideNum = Math.Round(number / decimalDivider, 2);

            // 소수점 이하가 없는 경우 (정수인 경우)
            if (divideNum == Math.Truncate(divideNum))
            {
                result = string.Format("{0:N0}", divideNum) + " " + decimalDividerName;

            }
            // 소수점 둘째 자리가 0인 경우 (예: 10.5)
            else if (divideNum * 10 % 1 == 0)
            {
                result = string.Format("{0:N1}", divideNum) + " " + decimalDividerName;
            }
            //소수점 2째자리 표기
            else
            {
                result = string.Format("{0:N2}", divideNum) + " " + decimalDividerName;
            }




            // 음수 처리
            if (isNegative && divideNum != 0)
            {
                result = "-" + result;
            }

            return result;
        }


        private void keyword_search_button_Click(object sender, EventArgs e)
        {
            _ = DoKeywordSearchAsync(sender, e);
            
        }

        //체크 항목 데이터 수집
        public List<string> GetCheckedRowsData(DataGridView dgv)
        {
            List<string> checkedData = new List<string>();

            foreach (DataGridViewRow row in dgv.Rows)
            {
                // CheckBox 컬럼(0번째)이 체크되었는지 확인
                if (row.Cells[0].Value != null &&
                    Convert.ToBoolean(row.Cells[0].Value) == true)
                {
                    // 1번째 열의 데이터를 리스트에 추가
                    string value = row.Cells[1].Value?.ToString() ?? "";
                    checkedData.Add(value);
                }
            }

            return checkedData;
        }

        //데이터 치환 함수
        public void ReplaceDataTableValues(List<string> targetList, DataTable dt, string replaceText, int startColumnIndex)
        {
            // 모든 행 순회
            foreach (DataRow row in dt.Rows)
            {
                // startColumnIndex부터 마지막 컬럼까지만 순회
                for (int colIndex = startColumnIndex; colIndex < dt.Columns.Count; colIndex++)
                {
                    string currentValue = row[colIndex]?.ToString() ?? "";

                    // targetList의 문자열과 일치하는지 확인
                    if (targetList.Any(target => currentValue.Equals(target, StringComparison.OrdinalIgnoreCase)))
                    {
                        row[colIndex] = replaceText;
                    }
                }
            }
        }

        private async void change_keyword_Click(object sender, EventArgs e)
        {
            string target_keyword = "";

            if ("".Equals(modified_keyword.Text.ToString()) || modified_keyword.Text == null)
            {
                MessageBox.Show("변환 키워드를 입력하셔야 합니다.", "알림",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Warning);

                return;
            }
            else
            {
                target_keyword = modified_keyword.Text.ToString();
            }
            
            //1.선택된 테이블 내 키워드 목록 출력
            List<string> changeValuelList = GetCheckedRowsData(match_keyword_table);

            if (changeValuelList.Count == 0)
            {
                MessageBox.Show("키워드 변환 대상을 선택하셔야 합니다.", "알림",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Warning);

                return;
            }

            using (var progressForm = new ProcessProgressForm())
            {
                progressForm.Show();
                await progressForm.UpdateProgressHandler(10, "키워드 변환 중...");
                await Task.Delay(10);

                //2.dataTransform dataTable 내 키워드 일괄 변환
                //2,2 -> dataTable 에서 일일히 찾아가면서 변환
                //0,1번 index는 부서,공급업체명 일 것이라 가정하므로 2번 index부터 치환(현재는 부서,공급업체명을 표기하지 않는다)
                ReplaceDataTableValues(changeValuelList, transformDataTable, target_keyword, 0);

                await progressForm.UpdateProgressHandler(30, "키워드 변환 내역 저장 중...");
                await Task.Delay(10);

                //viewTransformDataTable 도 변환 
                Debug.WriteLine("EnrichTransformDataWithRawData start");
                viewTransformDataTable = await EnrichTransformDataWithRawData(transformDataTable);
                Debug.WriteLine("EnrichTransformDataWithRawData end");


                await progressForm.UpdateProgressHandler(60, "변환 키워드 기반 요약 정보 재 산출 중...");
                await Task.Delay(10);


                Debug.WriteLine("data Transform change_keyword_Click -> create_merge_keyword_list & set_keyword_combo_list 설정 시작");

                //3.변경된 키워드 기반 리스트 재 생성
                await create_merge_keyword_list();
                await Task.Delay(10);
                await set_keyword_combo_list();

                Debug.WriteLine("data Transform change_keyword_Click -> set_keyword_combo_list 설정 완료");


                await progressForm.UpdateProgressHandler(90, "화면 완료...");
                await Task.Delay(10);


                await progressForm.UpdateProgressHandler(100);
                await Task.Delay(10);
                progressForm.Close();

            }
           

            MessageBox.Show("키워드 변환이 완료되었습니다.", "Info",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Information);


            match_keyword_table.DataSource = null;
            match_keyword_table.Rows.Clear();
            match_keyword_table.Columns.Clear();
            if (DataHandler.dragSelections.ContainsKey(match_keyword_table))
            {
                DataHandler.dragSelections[match_keyword_table].Clear();
            }
            
            dataGridView_transform.DataSource = null;
            dataGridView_transform.Rows.Clear();
            dataGridView_transform.Columns.Clear();

            //search_keyword_detail_list();

            //변환된 행 값으로 자동 선택

            bool exactMatch = true;

            for (int i = 0; i < sum_keyword_table.Rows.Count; i++)
            {
                if (sum_keyword_table.Rows[i].Cells[0].Value != null)
                {
                    string cellValue = sum_keyword_table.Rows[i].Cells[0].Value.ToString();

                    bool match = exactMatch
                        ? cellValue.Equals(target_keyword, StringComparison.OrdinalIgnoreCase)
                        : cellValue.IndexOf(target_keyword, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (match)
                    {
                        // 현재 선택 모두 해제
                        sum_keyword_table.ClearSelection();

                        // 행 선택
                        sum_keyword_table.Rows[i].Selected = true;

                        // 선택한 행이 보이도록 스크롤
                        sum_keyword_table.FirstDisplayedScrollingRowIndex = i;

                    }
                }
            }
            // 키워드를 사용하여 transformDataTable 필터링
            DataTable filteredTable = FilterTransformDataByKeyword(viewTransformDataTable, transformDataTable, target_keyword);

            // 필터링된 결과를 다른 DataGridView에 표시
            dataGridView_2nd.DataSource = null;
            dataGridView_2nd.Rows.Clear();
            dataGridView_2nd.Columns.Clear();
            dataGridView_2nd.DataSource = filteredTable;
            //dataGridView_2nd.Columns["import_date"].Visible = false;

            if (dataGridView_2nd.Columns["raw_data_id"] != null)
            {
                dataGridView_2nd.Columns["raw_data_id"].Visible = false;
            }

            
            
        }

        private void check_all_keyword_list_CheckedChanged(object sender, EventArgs e)
        {
            // 모든 행의 체크박스 상태 변경
            foreach (DataGridViewRow row in match_keyword_table.Rows)
            {
                row.Cells[0].Value = check_all_keyword_list.Checked;
            }
        }

        // User Control을 Form으로 감싸서 보여주는 방법
        public void ShowUserControlAsDialog(UserControl userControl)
        {
            Debug.WriteLine("ShowUserControlAsDialog start");

            // 폼 생성 및 기본 설정
            Form form = new Form
            {
                Text = "Clustering 결과 확인",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                Size = new Size(1900, 1000), // 적절한 초기 크기 지정
                MinimizeBox = true,
                MaximizeBox = true
            };

            // 진행 상태 표시를 위한 컨트롤 추가
            Panel loadingPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.White
            };

            Label loadingLabel = new Label
            {
                Text = "데이터 렌더링 중...",
                Font = new System.Drawing.Font("맑은 고딕", 14),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            loadingPanel.Controls.Add(loadingLabel);
            form.Controls.Add(loadingPanel);

            // UserControl을 form에 추가하기 전에 먼저 폼 표시 (비모달 방식)
            Debug.WriteLine("ShowUserControlAsDialog - Show form");
            form.Show();

            // 백그라운드 작업으로 데이터 렌더링 및 컨트롤 초기화 완료
            Task.Run(() => {
                // 약간의 지연을 통해 로딩 메시지가 먼저 표시될 수 있도록 함
                Task.Delay(100).Wait();

                form.Invoke((MethodInvoker)delegate {
                    Debug.WriteLine("ShowUserControlAsDialog - Adding UserControl");

                    // 이미 초기화된 UserControl을 Form에 추가
                    userControl.Dock = DockStyle.Fill;
                    form.Controls.Add(userControl);

                    // 로딩 패널 제거
                    form.Controls.Remove(loadingPanel);
                    loadingPanel.Dispose();

                    // 필요시 폼 크기 조정
                    form.ClientSize = new Size(
                        Math.Min(Screen.PrimaryScreen.WorkingArea.Width - 100, userControl.Width),
                        Math.Min(Screen.PrimaryScreen.WorkingArea.Height - 100, userControl.Height)
                    );

                    Debug.WriteLine("ShowUserControlAsDialog - UserControl added and rendered");
                });
            });

            Debug.WriteLine("ShowUserControlAsDialog - immediate return");

            // 이 메서드는 즉시 반환됨 (비모달)
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            try
            {
                // 간단한 로딩 메시지만 표시
                using (var waitCursor = new WaitCursor())
                {
                    // 데이터 로드 작업을 백그라운드 스레드에서 처리
                    await Task.Run(async () => {
                        if (DataHandler.firstClusteringData.Rows.Count == 0)
                        {
                            DataHandler.firstClusteringData = await DataHandler.CreateSetGroupDataTableAsync(originDataTable, DataHandler.moneyDataTable);
                        }
                        if (DataHandler.secondClusteringData.Rows.Count == 0)
                        {
                            DataHandler.secondClusteringData = await DataHandler.CreateSetGroupDataTableAsync(transformDataTable, DataHandler.moneyDataTable, true);
                        }
                    });

                    // 팝업 컨트롤 생성 및 초기화 (UI 스레드에서)
                    uc_clusteringPopup popup_control = new uc_clusteringPopup();
                    popup_control.initUI();

                    // 비모달 방식으로 팝업 표시
                    ShowUserControlAsDialog(popup_control);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터링 팝업 표시 중 오류: {ex.Message}");
                MessageBox.Show($"데이터 처리 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 간단한 대기 커서 클래스
        public class WaitCursor : IDisposable
        {
            private Cursor _previousCursor;

            public WaitCursor()
            {
                _previousCursor = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
            }

            public void Dispose()
            {
                Cursor.Current = _previousCursor;
            }
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            if (isFinishSession)
            {
                DialogResult dupleCheckResult = MessageBox.Show(
                $"현재 페이지에서 수정된 정보를 기준으로 Clustering 페이지를 갱신하기 위해 "
                + "기존 Clustering 페이지의 수정 내역을 초기화합니다."
                + "현재 페이지 정보를 기준으로 Clustering 페이지로 이동하시겠습니까?"
                + "\n(원치 않으실 경우 상단 메뉴바 > Clustering 항목을 클릭하여 이동 가능합니다. )",
                "Clustering 페이지 초기화 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
                );

                if (dupleCheckResult != DialogResult.Yes)
                {
                    return;
                }
                else
                {
                    DataHandler.finalClusteringData = null;
                }
            }

            using (var progressForm = new ProcessProgressForm())
            {
                progressForm.Show();
                await progressForm.UpdateProgressHandler(10, "데이터 저장 준비 중...");
                await Task.Delay(10);
                DataHandler.secondClusteringData = await DataHandler.CreateSetGroupDataTableAsync(transformDataTable, DataHandler.moneyDataTable, true);

                Debug.WriteLine("CreateSetGroupDataTable 수행 완료");

                DataHandler.recomandKeywordTable = modifiedDataTable;

                await progressForm.UpdateProgressHandler(30, "데이터 저장 준비 중...");
                await Task.Delay(10);

                userControlHandler.uc_clustering.initUI();

                await progressForm.UpdateProgressHandler(40, "화면 구성 중...");
                await Task.Delay(10);


                if (this.ParentForm is Form1 form)
                {
                    form.LoadUserControl(userControlHandler.uc_clustering);
                }
                await progressForm.UpdateProgressHandler(90, "화면 완료...");
                await Task.Delay(10);

                isFinishSession = true;

                await progressForm.UpdateProgressHandler(100);
                await Task.Delay(10);
                progressForm.Close();

            }
            
          
        }

        private void search_keyword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                keyword_search_button_Click(sender, e);   // 호출하고 싶은 함수
                e.SuppressKeyPress = true;  // 비프음 방지
            }
        }

        private void keyword_search_radio2_CheckedChanged(object sender, EventArgs e)
        {
            search_keyword.Enabled = keyword_search_radio2.Checked;
        }

        private void keyword_search_radio1_CheckedChanged(object sender, EventArgs e)
        {
            search_keyword.Enabled = keyword_search_radio2.Checked;

        }

        private async void dept_col_check_CheckedChanged(object sender, EventArgs e)
        {
            DataHandler.dept_col_yn = dept_col_check.Checked;

            //기존 clustering 결과는 초기화
            if (DataHandler.secondClusteringData.Rows.Count > 0)
            {
                DataHandler.secondClusteringData = await DataHandler.CreateSetGroupDataTableAsync(transformDataTable, DataHandler.moneyDataTable, true);
            }
            
        }

        private async void prod_col_check_CheckedChanged(object sender, EventArgs e)
        {
            DataHandler.prod_col_yn = prod_col_check.Checked;

            //기존 clustering 결과는 초기화
            if (DataHandler.secondClusteringData.Rows.Count > 0)
            {
                DataHandler.secondClusteringData = await DataHandler.CreateSetGroupDataTableAsync(transformDataTable, DataHandler.moneyDataTable, true);
            }
            
        }

        private void dataGridView_modified_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            search_keyword_detail_list();
        }

        private void search_keyword_detail_list()
        {
            if (sum_keyword_table.SelectedCells.Count > 0)
            {
                // 선택된 셀의 행 인덱스 가져오기
                int rowIndex = sum_keyword_table.SelectedCells[0].RowIndex;

                // 선택된 행의 첫 번째 열(Value/Keyword) 값 가져오기
                string keyword = sum_keyword_table.Rows[rowIndex].Cells[0].Value.ToString();

                // 키워드를 사용하여 transformDataTable 필터링
                DataTable filteredTable = FilterTransformDataByKeyword(viewTransformDataTable, transformDataTable, keyword);

                // 필터링된 결과를 다른 DataGridView에 표시
                dataGridView_2nd.DataSource = null;
                dataGridView_2nd.Rows.Clear();
                dataGridView_2nd.Columns.Clear();
                dataGridView_2nd.DataSource = filteredTable;
                //dataGridView_2nd.Columns["import_date"].Visible = false;

                if (dataGridView_2nd.Columns["raw_data_id"] != null)
                {
                    dataGridView_2nd.Columns["raw_data_id"].Visible = false;
                }

                // 또는 상태 표시줄에 결과 개수 표시
                Debug.WriteLine($"키워드 '{keyword}'를 포함하는 행: {filteredTable.Rows.Count}개");
            }
        }

        private void match_keyword_table_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (match_keyword_table.SelectedCells.Count > 0)
            {
                // 선택된 셀의 행 인덱스 가져오기
                int rowIndex = match_keyword_table.SelectedCells[0].RowIndex;

                // 선택된 행의 첫 번째 열(Value/Keyword) 값 가져오기
                string keyword = match_keyword_table.Rows[rowIndex].Cells[1].Value.ToString();

                // 키워드를 사용하여 transformDataTable 필터링
                DataTable filteredTable = FilterTransformDataByKeyword(viewTransformDataTable, transformDataTable, keyword);

                // 필터링된 결과를 다른 DataGridView에 표시
                dataGridView_transform.DataSource = filteredTable;
                //dataGridView_transform.Columns["import_date"].Visible = false;
                if (dataGridView_transform.Columns["raw_data_id"] != null)
                {
                    dataGridView_transform.Columns["raw_data_id"].Visible = false;
                }

                // 또는 상태 표시줄에 결과 개수 표시
                Debug.WriteLine($"키워드 '{keyword}'를 포함하는 행: {filteredTable.Rows.Count}개");
            }
        }


        private async void decimal_combo_SelectedIndexChanged(object sender, EventArgs e)
        {
            Debug.WriteLine($"decimal_combo.SelectedIndex : {decimal_combo.SelectedIndex}");
            //선택 값 기준 decimal 단위 변환
            double divider = Math.Pow(1000, decimal_combo.SelectedIndex);
            //억 원은 10 나누기
            if (decimal_combo.SelectedIndex == 3)
            {
                divider = divider / 10;
            }
            decimalDivider = (decimal)divider;
            decimalDividerName = decimal_combo.SelectedItem.ToString();

            //리스트 재 조회
            // 나머지 초기화 로직
            //await Task.Run(() => create_merge_keyword_list(true));
            //create_merge_keyword_list(true);
            // Task.Run을 사용하여 create_merge_keyword_list를 실행하고 완료될 때까지 기다림
                    await Task.Run(() => {
                        // UI 스레드에서 실행해야 하는 부분이 있다면 Invoke 사용
                        this.Invoke((MethodInvoker)delegate {
                            create_merge_keyword_list(true);                           
                        });
                    });

            if (match_keyword_table.Rows.Count > 0)
            {
                Debug.WriteLine("keyword_search_button_Click 함수 호출");
                await DoKeywordSearchAsync(sender, e);
            }

        }

        // 비동기 작업을 수행하는 내부 함수
        private async Task DoKeywordSearchAsync(object sender, EventArgs e)
        {

            //searchYN =true 이면 대기
            while (searchYN)
            {
                await Task.Delay(10);
            }
            string target_keyword = "";

            //combobox로 검색할 경우
            if (keyword_search_radio1.Checked)
            {
                if (keyword_search_combo.SelectedIndex != 0)
                {
                    target_keyword = keyword_search_combo.SelectedItem.ToString();
                }
            }
            //직접 검색할 경우
            else if (keyword_search_radio2.Checked)
            {
                if (!"".Equals(search_keyword.Text.ToString()) && search_keyword.Text != null)
                {
                    target_keyword = search_keyword.Text.ToString();
                }
            }

            //List<string> lowlevelList = DataHandler.GetColumnValuesAsList(DataHandler.lowLevelData, 0);
            List<string> valuelList = DataHandler.GetColumnValuesAsList(modifiedDataTable, 0);

            List<string> MathcingPairs = new List<string>();

            if (!"".Equals(target_keyword))
            {
                MathcingPairs = DataHandler.FindMachKeyword(valuelList, target_keyword);
                Debug.WriteLine($"MathcingPairs.Count : {MathcingPairs.Count}");
                if (MathcingPairs.Count == 0)
                {
                    match_keyword_table.DataSource = null;
                    match_keyword_table.Rows.Clear();
                    match_keyword_table.Columns.Clear();
                    if (DataHandler.dragSelections.ContainsKey(match_keyword_table))
                    {
                        DataHandler.dragSelections[match_keyword_table].Clear();
                    }
                    return;
                }
            }


            CreateFilteredDataGridView(match_keyword_table, modifiedDataTable, MathcingPairs);

            check_all_keyword_list.Checked = false;

            modified_keyword.Text = target_keyword;
        }

    }

}
