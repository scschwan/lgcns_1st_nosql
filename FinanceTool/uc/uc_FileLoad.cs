using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClosedXML.Excel;

using FinanceTool.MongoModels;
using FinanceTool.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using FinanceTool.Data;
using System.Collections.Concurrent;
using ExcelDataReader;


namespace FinanceTool
{
    public partial class uc_FileLoad : UserControl
    {
        // 기존 필드
        private List<string> process_col_list = new List<string>();
        private string selectedStandColumn = "";
        //private HashSet<int> hiddenRows = new HashSet<int>();

        
        private int currentPage = 1;
        private int pageSize = 1000;
        private int totalPages = 1;
        private int totalRows = 0;

        private bool excelLoadinitFlag = true;

        // uc_FileLoad 클래스에서 멤버 변수 추가
        //private bool _fileLoaded = false;
        private bool _fileLoaded = true;

        // MongoDB Repository 객체
        private RawDataRepository rawDataRepo = new RawDataRepository();

        //공급업체명 표준화 관련 멤버 변수
        private List<string> _allColumns = new List<string>();
        private DataTable _standardMappingData = null;

        // 클래스 멤버 변수 추가
        private System.Windows.Forms.Timer _columnOrderUpdateTimer;
        private bool _columnOrderChanged = false;
        private MongoDataConverter _mongoConverter = new MongoDataConverter();

        public uc_FileLoad()
        {
            InitializeComponent();
            InitializePagingControls(false);

        }
      

        // NumericUpDown 값 변경 이벤트 핸들러
        private async void num_pageNumber_ValueChanged(object sender, EventArgs e)
        {
            // 값이 범위를 벗어나면 조정
            if (num_pageNumber.Value < 1)
            {
                num_pageNumber.Value = 1;
                return;
            }

            if (num_pageNumber.Value > totalPages)
            {
                num_pageNumber.Value = totalPages;
                return;
            }

            // 이벤트 재귀 방지
            if (currentPage == (int)num_pageNumber.Value)
                return;

            // 페이지 이동
            currentPage = (int)num_pageNumber.Value;
            await LoadMongoPagedDataAsync();
        }

        private void uc_FileLoad_Load(object sender, EventArgs e)
        {
            // 초기화 시 아무 작업도 하지 않음
        }


        // 이전 페이지 이동
        private async void btn_prevPage_Click(object sender, EventArgs e)
        {
            if (currentPage > 1)
            {
                num_pageNumber.Value--;
                //await LoadPagedDataAsync();
            }
        }

        // 다음 페이지 이동
        private async void btn_nextPage_Click(object sender, EventArgs e)
        {
            if (currentPage < totalPages)
            {
                num_pageNumber.Value++;
                //await LoadPagedDataAsync();
            }
        }

        // 페이지 크기 변경
        private async void cmb_pageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmb_pageSize.SelectedItem != null)
            {
                pageSize = Convert.ToInt32(cmb_pageSize.SelectedItem);
                currentPage = 1; // 페이지 크기 변경 시 첫 페이지로
                await LoadMongoPagedDataAsync();
            }
        }

       


        private async void btn_complete_Click(object sender, EventArgs e)
        {
            //data Validation 
            if (sub_acc_col_combo.SelectedIndex < 1)
            {
                MessageBox.Show("세목 열을 선택하셔야 합니다.", "알림",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Warning);
                return;
            }

            if (dept_col_combo.SelectedIndex < 1)
            {
                MessageBox.Show("부서 열을 선택하셔야 합니다.", "알림",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Warning);
                return;
            }

            if (prod_col_combo.SelectedIndex < 1)
            {
                MessageBox.Show("공급업체 열을 선택하셔야 합니다.", "알림",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Warning);
                return;
            }

            if (cmb_money.SelectedIndex < 1)
            {
                MessageBox.Show("금액 열을 선택하셔야 합니다.", "알림",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning);
                return;
            }
            else
            {
                // 금액 컬럼 데이터 유효성 검증
                var (isAllNumeric, nonNumericData) = await CheckNumericColumnAsync(cmb_money.SelectedItem.ToString());
                if (!isAllNumeric)
                {
                    var firstError = nonNumericData[0];
                    MessageBox.Show(
                        $"금액 열은 숫자값만 있어야 합니다.\n행 번호 : {firstError.RowIndex + 1}, 행 값 : {firstError.Value}",
                        "알림",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }
            }

            if (cmb_target.SelectedIndex < 1)
            {
                MessageBox.Show("타겟 열을 선택하셔야 합니다.", "알림",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning);
                return;
            }

            // 선택된 컬럼 목록 생성
            List<string> selectedColumns = new List<string>();
            selectedColumns.Add(sub_acc_col_combo.SelectedItem.ToString());
            selectedColumns.Add(dept_col_combo.SelectedItem.ToString());
            selectedColumns.Add(prod_col_combo.SelectedItem.ToString());
            selectedColumns.Add(cmb_money.SelectedItem.ToString());
            selectedColumns.Add(cmb_target.SelectedItem.ToString());

            // 중복 검사
            var duplicates = selectedColumns.GroupBy(x => x)
                                          .Where(g => g.Count() > 1)
                                          .Select(g => g.Key)
                                          .ToList();

            if (duplicates.Count > 0)
            {
                string duplicateColumns = string.Join(", ", duplicates);
                MessageBox.Show($"동일한 컬럼을 중복 선택할 수 없습니다.\n중복된 컬럼: {duplicateColumns}",
                               "중복 선택 오류",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    progressForm.UpdateProgressHandler(10);

                   

                    // 필요한 전역 변수 설정
                    DataHandler.sub_acc_col_name = sub_acc_col_combo.SelectedItem.ToString();
                    DataHandler.dept_col_name = dept_col_combo.SelectedItem.ToString();
                    DataHandler.prod_col_name = prod_col_combo.SelectedItem.ToString();
                    DataHandler.levelList.Clear();
                    DataHandler.levelName.Clear();

                    // 금액 컬럼 인덱스 설정 (프로세스 테이블에서는 0)
                    DataHandler.levelList.Add(5);
                    DataHandler.moneyIndex = 5;
                    DataHandler.levelName.Add(cmb_money.SelectedItem.ToString());

                    // 타겟 컬럼 인덱스 설정 (프로세스 테이블에서는 1)
                    DataHandler.levelList.Add(6);
                    DataHandler.levelName.Add(cmb_target.SelectedItem.ToString());

                    progressForm.UpdateProgressHandler(30);
                    Debug.WriteLine($"[file load] btn_complete_Click processing...");
                    Debug.WriteLine($"[file load] PrepareProcessDataAsync start");

                    // MongoDB 방식으로 변경
                    MongoDataConverter mongoConverter = new MongoDataConverter();
                    await mongoConverter.PrepareProcessDataAsync(selectedColumns);

                    progressForm.UpdateProgressHandler(70);

                    // 레거시 코드와의 호환을 위해 DataHandler.processTable 설정
                    // 변경 전: DataHandler.processTable = DBManager.Instance.ExecuteQuery("SELECT * FROM process_data");

                    // 변경 후: MongoDB에서 데이터 가져와 DataTable로 변환
                    DataHandler.processTable = await DataHandler_fileLoad.GetDataTableFromProcessDataAsync();

                    progressForm.UpdateProgressHandler(90);
                    progressForm.UpdateProgressHandler(100);
                }

                // 다음 단계로 이동
                await userControlHandler.uc_Preprocessing.initUI();

                if (this.ParentForm is Form1 form)
                {
                    form.LoadUserControl(userControlHandler.uc_Preprocessing , form.dataPreprocessingToolStripMenuItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"처리 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine($"btn_complete_Click 오류: {ex.Message}\n{ex.StackTrace}");
            }
        }

      
        private void del_col_list_allcheck_CheckedChanged(object sender, EventArgs e)
        {
            // 모든 행의 체크박스 상태 변경
            foreach (DataGridViewRow row in dataGridView_delete_col.Rows)
            {
                row.Cells[0].Value = del_col_list_allcheck.Checked;
            }
        }

        private void del_data_list_allcheck_CheckedChanged(object sender, EventArgs e)
        {
            // 모든 행의 체크박스 상태 변경
            foreach (DataGridViewRow row in dataGridView_delete_data.Rows)
            {
                row.Cells[0].Value = del_data_list_allcheck.Checked;
            }
        }

        private void restore_col_btn_Click(object sender, EventArgs e)
        {
            List<string> restore_list = GetCheckedRowsData(dataGridView_delete_col);

            // 선택된 열 목록 저장
            DataHandler.visibleColumns = new List<string>(restore_list);

            for (int i = 0; i < dataGridView_process.Columns.Count; i++)
            {
                if (restore_list.Contains(dataGridView_process.Columns[i].Name))
                {
                    dataGridView_process.Columns[i].Visible = true;
                }
                else
                {
                    dataGridView_process.Columns[i].Visible = false;
                }
            }

            // MongoDB 컬렉션에서 컬럼 가시성 업데이트
            foreach (DataColumn column in DataHandler.excelData.Columns)
            {
                if (column.ColumnName != "id" &&
                    column.ColumnName != "import_date" &&
                    column.ColumnName != "hiddenYN")
                {
                    bool isVisible = restore_list.Contains(column.ColumnName);
                    try
                    {
                        // MongoDB에서 컬럼 매핑 정보 업데이트
                        UpdateColumnVisibilityInMongo(column.ColumnName, isVisible);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"컬럼 가시성 업데이트 오류: {ex.Message}");
                    }
                }
            }

            // 선택된 열만 사용하도록 컬럼 목록 업데이트
            GetMongoColumnList(DataHandler.excelData.Columns);
            SetupColumnLists();
        }

        

        private void stand_col_combo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (stand_col_combo.SelectedIndex == 0)
            {
                return;
            }

            selectedStandColumn = stand_col_combo.SelectedItem.ToString();
            PopulateDeleteDataGrid(selectedStandColumn);
        }

        // 삭제 데이터 그리드 채우기
        // 삭제 데이터 그리드 채우기 - MongoDB 버전으로 변환
        private async void PopulateDeleteDataGrid(string columnName)
        {
            try
            {
                // MongoDB에서 고유 값 가져오기
                // 이전 코드: string query = $"SELECT DISTINCT {columnName} FROM raw_data WHERE {columnName} IS NOT NULL ORDER BY {columnName}";
                // 이전 코드: DataTable distinctValues = DBManager.Instance.ExecuteQuery(query);

                // MongoDB에서 필드의 고유 값 가져오기
                var distinctValues = await GetDistinctValuesFromMongoAsync(columnName);

                // DataGridView 초기화
                dataGridView_delete_data.DataSource = null;
                dataGridView_delete_data.Rows.Clear();
                dataGridView_delete_data.Columns.Clear();
                if (DataHandler.dragSelections.ContainsKey(dataGridView_delete_data))
                {
                    DataHandler.dragSelections[dataGridView_delete_data].Clear();
                }

                // 체크박스 컬럼 추가
                DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn
                {
                    Name = "CheckBox",
                    HeaderText = "",
                    Width = 50,
                    ThreeState = false,
                    FillWeight = 20
                };
                dataGridView_delete_data.Columns.Add(checkColumn);

                // 데이터 컬럼 추가
                DataGridViewTextBoxColumn dataColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Data",
                    HeaderText = "데이터"
                };
                dataGridView_delete_data.Columns.Add(dataColumn);

                // 데이터 리스트의 각 항목을 행으로 추가
                foreach (var value in distinctValues)
                {
                    if (value != null && !string.IsNullOrEmpty(value.ToString()))
                    {
                        int rowIndex = dataGridView_delete_data.Rows.Add();
                        dataGridView_delete_data.Rows[rowIndex].Cells["CheckBox"].Value = false;
                        dataGridView_delete_data.Rows[rowIndex].Cells["Data"].Value = value.ToString();
                    }
                }

                dataGridView_delete_data.AllowUserToAddRows = false;
                dataGridView_delete_data.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGridView_delete_data.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dataGridView_delete_data.ReadOnly = false;
                dataGridView_delete_data.Columns["CheckBox"].ReadOnly = false;  // 체크박스 컬럼만 편집 가능
                dataGridView_delete_data.Font = new System.Drawing.Font("Pretendard", 14.25F);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"데이터 로드 중 오류: {ex.Message}");
                MessageBox.Show($"데이터 로드 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        
        private async void restore_del_data_btn_Click(object sender, EventArgs e)
        {
            try
            {
                // 모든 그리드 선택 초기화
                InitializeCursors();

                DataHandler.hiddenData = false;

                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    progressForm.UpdateProgressHandler(20);

                    // MongoDB에서 모든 데이터의 is_hidden을 false로 설정
                    await Task.Run(async () =>
                    {
                        var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                        var rawDataCollection = await mongoManager.GetCollectionAsync<RawDataDocument>("raw_data");

                        var filter = Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, true);
                        var update = Builders<RawDataDocument>.Update.Set(d => d.IsHidden, false);

                        await rawDataCollection.UpdateManyAsync(filter, update);
                    });

                    progressForm.UpdateProgressHandler(50);

                    // UI 업데이트
                    this.BeginInvoke(new Action(() =>
                    {
                        // 모든 행 표시 설정 & 스타일 초기화
                        RestoreAllRowsVisibility();
                    }));

                    progressForm.UpdateProgressHandler(70);

                    // 데이터 다시 로드
                    if (!string.IsNullOrEmpty(selectedStandColumn))
                    {
                        PopulateDeleteDataGrid(selectedStandColumn);
                    }

                    progressForm.UpdateProgressHandler(90);

                    // 페이지 데이터 리로드
                    await LoadMongoPagedDataAsync();
                    progressForm.UpdateProgressHandler(100);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 복원 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

      

        private async void delete_data_btn_Click(object sender, EventArgs e)
        {
            List<string> delList = GetCheckedRowsData(dataGridView_delete_data);

            if (delList.Count == 0)
            {
                MessageBox.Show("데이터 삭제 대상을 선택하셔야 합니다.", "알림",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Warning);
                return;
            }

            DataHandler.hiddenData = true;

            try
            {
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    progressForm.UpdateProgressHandler(10);

                    // MongoDB에서 숨기기 처리
                    int hiddenCount = 0;
                    await Task.Run(async () =>
                    {
                        // MongoDB 저장소 접근
                        var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                        var rawDataCollection = await mongoManager.GetCollectionAsync<RawDataDocument>("raw_data");

                        // 각 값에 대해 해당하는 문서 숨기기
                        foreach (string value in delList)
                        {
                            // 필드 값이 일치하는 문서 찾기
                            var filter = Builders<RawDataDocument>.Filter.Eq($"Data.{selectedStandColumn}", value);
                            var update = Builders<RawDataDocument>.Update.Set("is_hidden", true);

                            // 업데이트 실행
                            var result = await rawDataCollection.UpdateManyAsync(filter, update);
                            hiddenCount += (int)result.ModifiedCount;
                        }
                    });

                    progressForm.UpdateProgressHandler(30);

                    // UI 업데이트는 메인 스레드에서 수행
                    this.BeginInvoke(new Action(() =>
                    {
                        // 모든 그리드 선택 초기화
                        InitializeCursors();
                    }));

                    progressForm.UpdateProgressHandler(80);

                    // 삭제 데이터 목록에서 처리된 항목 제거
                    RemoveProcessedRows(delList);

                    progressForm.UpdateProgressHandler(90);

                    // 페이지 데이터 리로드 (MongoDB 버전 메서드 호출)
                    await LoadMongoPagedDataAsync();

                    progressForm.UpdateProgressHandler(100);
                }

                MessageBox.Show($"{delList.Count}개 항목이 숨겨졌습니다.", "정보",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 숨기기 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // 그리드뷰 셀 선택 이벤트 핸들러
        private void dataGridView_target_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView_target.SelectedRows.Count > 0)
            {
            }
          
        }

        private void dataGridView_target_CellClick(object sender, DataGridViewCellEventArgs e)
        {
          
        }

        private async void delete_search_button_Click(object sender, EventArgs e)
        {
            // 현재 선택된 컬럼이 없으면 메시지 표시 후 종료
            if (string.IsNullOrEmpty(selectedStandColumn) || stand_col_combo.SelectedIndex == 0)
            {
                MessageBox.Show("검색하기 전에 기준 열을 선택해주세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 검색 키워드 가져오기
            string searchText = delete_search_keyword.Text.Trim();

            // 키워드가 비어있으면 모든 항목 표시
            if (string.IsNullOrEmpty(searchText))
            {
                PopulateDeleteDataGrid(selectedStandColumn);
                return;
            }

            try
            {
                // 키워드를 쉼표(,)로 분리
                string[] keywords = searchText.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                              .Select(k => k.Trim())
                                              .Where(k => !string.IsNullOrEmpty(k))
                                              .ToArray();

                if (keywords.Length == 0)
                {
                    PopulateDeleteDataGrid(selectedStandColumn);
                    return;
                }

                // MongoDB에서 검색 - 정규식 검색 사용
                var filteredValues = await SearchMongoFieldByKeywordsAsync(selectedStandColumn, keywords);

                // DataGridView 초기화 및 데이터 표시
                PopulateDeleteDataGridWithResults(filteredValues);

                // 결과 메시지 표시 (선택사항)
                if (filteredValues.Count == 0)
                {
                    MessageBox.Show($"검색 결과가 없습니다.", "검색 결과",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"검색 중 오류 발생: {ex.Message}");
                MessageBox.Show($"데이터 검색 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void delete_search_keyword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                delete_search_button_Click(sender, e);
                e.SuppressKeyPress = true;  // 비프음 방지
            }
        }


        // 이벤트 핸들러

        /// <summary>
        /// Key 콤보박스 선택 변경
        /// </summary>
        private async void ComboBox_standard_key_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_standard_key.SelectedIndex > 0 && comboBox_standard_target.SelectedIndex > 0)
            {
                await AnalyzeKeyTargetMapping();
            }
        }

        /// <summary>
        /// Target 콤보박스 선택 변경
        /// </summary>
        private async void ComboBox_standard_target_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_standard_key.SelectedIndex > 0 && comboBox_standard_target.SelectedIndex > 0)
            {
                await AnalyzeKeyTargetMapping();
            }
        }

        /// <summary>
        /// 표준화 수행 버튼 클릭
        /// </summary>
        private async void Standard_btn_Click(object sender, EventArgs e)
        {
            await PerformStandardization();
        }

        /// <summary>
        /// DataGridView 편집 완료 (uc_clustering 패턴)
        /// </summary>
        private async void DataGridView_standard_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == 1) // TargetValue 컬럼
                {
                    var row = dataGridView_standard.Rows[e.RowIndex];
                    string keyValue = row.Cells["KeyValue"].Value?.ToString();
                    string oldTargetValue = _standardMappingData.Rows[e.RowIndex]["TargetValue"].ToString();
                    string newTargetValue = row.Cells["TargetValue"].Value?.ToString();

                    if (!string.IsNullOrEmpty(newTargetValue) && newTargetValue != oldTargetValue)
                    {
                        using (var progressForm = new ProcessProgressForm())
                        {
                            progressForm.Show();
                            await progressForm.UpdateProgressHandler(10, "대상값 변경 중...");

                            // MongoDB에서 해당 값 일괄 변경
                            await UpdateTargetValueInMongoDB(keyValue, oldTargetValue, newTargetValue);

                            await progressForm.UpdateProgressHandler(50, "매핑 데이터 재분석 중...");

                            // 매핑 데이터 재분석
                            await AnalyzeKeyTargetMapping();

                            await progressForm.UpdateProgressHandler(80, "페이징 데이터 새로고침 중...");

                            // 페이징 데이터 새로고침
                            await LoadMongoPagedDataAsync();

                            await progressForm.UpdateProgressHandler(100, "완료");
                        }

                        MessageBox.Show($"'{oldTargetValue}'를 '{newTargetValue}'로 변경 완료", "변경 완료",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"편집 완료 처리 오류: {ex.Message}");
                MessageBox.Show($"변경 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

       

        
       
        // InitializeComponent() 후에 호출할 초기화 메서드
        private void InitializeColumnOrderHandling()
        {
            // 컬럼 순서 변경 이벤트 등록
            dataGridView_target.ColumnDisplayIndexChanged += DataGridView_ColumnDisplayIndexChanged;
            dataGridView_process.ColumnDisplayIndexChanged += DataGridView_ColumnDisplayIndexChanged;

            // 타이머 초기화
            _columnOrderUpdateTimer = new System.Windows.Forms.Timer();
            _columnOrderUpdateTimer.Interval = 500; // 500ms 디바운싱
            _columnOrderUpdateTimer.Tick += ColumnOrderUpdateTimer_Tick;
        }

        /// <summary>
        /// 컬럼 순서 변경 이벤트 핸들러
        /// </summary>
        private void DataGridView_ColumnDisplayIndexChanged(object sender, DataGridViewColumnEventArgs e)
        {
            try
            {
                var dgv = sender as DataGridView;
                if (dgv == null) return;

                // 시스템 컬럼은 순서 변경 대상에서 제외
                if (IsSystemColumn(e.Column.Name)) return;

                _columnOrderChanged = true;

                // 1. 즉시 메모리 업데이트
                DataHandler_fileLoad.UpdateColumnDisplayOrder(dgv);

                // 2. 디바운싱된 DB 업데이트
                _columnOrderUpdateTimer.Stop();
                _columnOrderUpdateTimer.Start();

                // 3. 다른 DataGridView들 즉시 갱신
                RefreshOtherDataGridViewOrders(dgv);

                Debug.WriteLine($"컬럼 순서 변경됨: {e.Column.Name} → DisplayIndex: {e.Column.DisplayIndex}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"컬럼 순서 변경 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 디바운싱된 DB 업데이트
        /// </summary>
        private async void ColumnOrderUpdateTimer_Tick(object sender, EventArgs e)
        {
            _columnOrderUpdateTimer.Stop();

            if (_columnOrderChanged)
            {
                try
                {
                    await DataHandler_fileLoad.SaveColumnOrderToMongoDB();
                    _columnOrderChanged = false;
                    Debug.WriteLine("컬럼 순서 MongoDB 저장 완료");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"컬럼 순서 저장 오류: {ex.Message}");
                }
            }
        }


    }



    // 간단한 로딩 폼
    public class LoadingForm : Form
    {
        private Label messageLabel;

        public LoadingForm(string message)
        {
            InitializeComponent(message);
        }

        private void InitializeComponent(string message)
        {
            this.Width = 300;
            this.Height = 100;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = System.Drawing.Color.White;

            messageLabel = new Label();
            messageLabel.Text = message;
            messageLabel.AutoSize = false;
            messageLabel.Size = new Size(280, 60);
            messageLabel.Location = new Point(10, 20);
            messageLabel.TextAlign = ContentAlignment.MiddleCenter;
            messageLabel.Font = new System.Drawing.Font("Pretendard", 12F, FontStyle.Regular);

            this.Controls.Add(messageLabel);
        }

       

        
    }
}