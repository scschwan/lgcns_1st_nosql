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

using FinanceTool.MongoModels;
using FinanceTool.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using ClosedXML.Excel;

namespace FinanceTool
{
    public partial class uc_FileLoad : UserControl
    {
        // 기존 필드
        private List<string> process_col_list = new List<string>();
        private string selectedStandColumn = "";
        //private HashSet<int> hiddenRows = new HashSet<int>();

        // SQLite 관련 추가 필드
        private DataConverter dataConverter;
        private int currentPage = 1;
        private int pageSize = 1000;
        private int totalPages = 1;
        private int totalRows = 0;

       

        // uc_FileLoad 클래스에서 멤버 변수 추가
        private bool _fileLoaded = false;

        // MongoDB Repository 객체
        private RawDataRepository rawDataRepo = new RawDataRepository();

        public uc_FileLoad()
        {
            InitializeComponent();
            InitializePagingControls(false);

            dataConverter = new DataConverter();
        }

        private void InitializePagingControls(bool attachEvents)
        {
            // 콤보박스 초기화
            cmb_pageSize.Items.Clear();
            cmb_pageSize.Items.AddRange(new object[] { 500, 1000, 2000, 5000 });
            cmb_pageSize.SelectedIndex = 1; // 기본값 1000
            cmb_pageSize.DropDownStyle = ComboBoxStyle.DropDownList;

            // NumericUpDown 설정
            num_pageNumber.Minimum = 1;
            num_pageNumber.Maximum = 1;
            num_pageNumber.Value = 1;


            // 컨트롤 비활성화 (파일 로드 전)
            EnablePagingControls(false);

            // 이벤트 등록은 옵션에 따라 결정
            if (attachEvents)
            {
                AttachPagingEvents();
            }

            // 초기 페이징 상태
            UpdatePaginationInfo();

            DataHandler.RegisterDataGridView(dataGridView_delete_col);
            DataHandler.RegisterDataGridView(dataGridView_delete_data);
        }

        // 페이징 이벤트 등록 메서드 (별도로 분리)
        private void AttachPagingEvents()
        {
            // 이벤트 등록
            cmb_pageSize.SelectedIndexChanged += cmb_pageSize_SelectedIndexChanged;
            num_pageNumber.ValueChanged += num_pageNumber_ValueChanged;
            //btn_prevPage.Click += btn_prevPage_Click;
            //btn_nextPage.Click += btn_nextPage_Click;
        }

        // 페이징 컨트롤 활성화/비활성화 메서드
        private void EnablePagingControls(bool enabled)
        {
            btn_prevPage.Enabled = enabled;
            btn_nextPage.Enabled = enabled;
            num_pageNumber.Enabled = enabled;
            cmb_pageSize.Enabled = enabled;
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

        private async void btn_selectFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title = "엑셀 파일 선택"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // 기존 데이터 존재 여부 확인
                bool dataExists = DBManager.Instance.IsInitialized && DBManager.Instance.TableExists("raw_data");

                // 기존 데이터가 있으면 확인 메시지 표시
                if (dataExists)
                {
                    DialogResult result = MessageBox.Show(
                        "파일을 새로 업로드 할 경우 \n기존 업로드 내역 및 작업 내용이 모두 초기화 됩니다.\n" +
                        "파일을 계속 업로드 하시겠습니까?",
                        "경고",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Warning
                    );

                    if (result == DialogResult.Cancel)
                    {
                        return; // 사용자가 취소함
                    }

                    // 사용자가 확인했으므로 데이터베이스 초기화 진행
                    if (!DBManager.Instance.ResetDatabase())
                    {
                        MessageBox.Show("데이터베이스 초기화에 실패했습니다.", "오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    // 초기 데이터베이스 설정 (첫 실행 시)
                    if (!DBManager.Instance.EnsureInitialized())
                    {
                        MessageBox.Show("데이터베이스 초기화에 실패했습니다.", "오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                string filePath = openFileDialog.FileName;
                await LoadExcelDataAsync(filePath);
                lbl_filename.Text = filePath;
            }
        }

        private async Task LoadExcelDataAsync(string filePath)
        {
            try
            {
                using (var progress = new ProcessProgressForm())
                {
                    progress.Show();
                    await Task.Delay(10);

                    await progress.UpdateProgressHandler(5, "파일 업로드 준비 중...");

                    // MongoDB 데이터 컨버터 생성
                    MongoDataConverter mongoConverter = new MongoDataConverter();

                    // 1. Excel 파일 직접 열기
                    await progress.UpdateProgressHandler(10, "Excel 파일 로드 중...");
                    DataTable excelData;

                    using (var workbook = new XLWorkbook(filePath))
                    {
                        var worksheet = workbook.Worksheets.First();
                        var range = worksheet.RangeUsed();

                        // Excel 데이터를 DataTable로 변환
                        excelData = range.AsTable().AsNativeDataTable();

                    }

                    await progress.UpdateProgressHandler(40, "데이터 전처리 완료...");

                    // 2. DataTable을 MongoDB에 바로 저장
                    await progress.UpdateProgressHandler(50, "MongoDB에 데이터 저장 중...");
                    List<RawDataDocument> documents = await mongoConverter.ConvertExcelToMongoDBAsync(
                        excelData, Path.GetFileName(filePath), progress.UpdateProgressHandler);

                    await progress.UpdateProgressHandler(80, "데이터 처리 중...");

                    // 3. UI 초기화 및 데이터 표시 설정
                    // File load completed - register events if first load
                    if (!_fileLoaded)
                    {
                        AttachPagingEvents();
                        _fileLoaded = true;
                    }

                    // Enable paging controls
                    EnablePagingControls(true);

                    // 로드된 데이터 설정 - 전역 변수에 저장 (기존 코드 호환성 유지)
                    DataHandler.excelData = excelData;

                    // 4. 페이징 처리 초기화
                    currentPage = 1;
                    pageSize = 1000;

                    // 5. MongoDB에서 데이터 로드하여 DataGridView에 표시
                    await LoadMongoPagedDataAsync(true);
                    await Task.Delay(10);

                    // 6. 선택 가능한 컬럼 목록 그리드에 추가
                    await AddMongoColumnsToGrid(dataGridView_delete_col, excelData.Columns);

                    await progress.UpdateProgressHandler(90, "컬럼 정보 설정 중...");
                    await Task.Delay(10);

                    // 7. 컬럼 목록 가져오기
                    GetMongoColumnList(excelData.Columns);

                    // 8. 컬럼 콤보박스 설정
                    SetupColumnLists();

                    await progress.UpdateProgressHandler(100, "데이터 로드 완료!");
                    await Task.Delay(10);
                    progress.Close();

                    Debug.WriteLine("File loading completed successfully with MongoDB");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"엑셀 파일 로드 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // MongoDB 기반으로 페이징 데이터 로드
        private async Task LoadMongoPagedDataAsync(bool progressYN = false)
        {
            // 파일이 로드되지 않았으면 아무 작업도 수행하지 않음
            if (!_fileLoaded)
            {
                Debug.WriteLine("파일이 로드되지 않아 페이징 작업을 건너뜁니다.");
                return;
            }

            try
            {
                // MongoDB 데이터 컨버터
                MongoDataConverter mongoConverter = new MongoDataConverter();

                if (progressYN)
                {
                    // MongoDB에서 페이징된 데이터 가져오기
                    var (documents, totalCount) = await mongoConverter.GetPagedRawDataAsync(
                        currentPage, pageSize, DataHandler.hiddenData);

                    // 페이징 메타데이터 계산
                    totalRows = (int)totalCount;
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                    // MongoDB 문서를 DataTable로 변환
                    DataTable pageData = ConvertMongoDocumentsToDataTable(documents);

                    // UI 업데이트는 메인 스레드에서 수행
                    this.BeginInvoke(new Action(() =>
                    {
                        ConfigureDataGridView(pageData, dataGridView_target);
                        ConfigureDataGridView(pageData, dataGridView_process);
                        UpdatePaginationInfo();
                        ApplyGridFormatting();
                    }));
                }
                else
                {
                    using (var loadingForm = new ProcessProgressForm())
                    {
                        loadingForm.Show();
                        loadingForm.UpdateProgressHandler(10);

                        // MongoDB에서 페이징된 데이터 가져오기
                        var result = await Task.Run(async () =>
                        {
                            var (documents, totalCount) = await mongoConverter.GetPagedRawDataAsync(
                                currentPage, pageSize, DataHandler.hiddenData);

                            // 페이징 메타데이터 계산
                            totalRows = (int)totalCount;
                            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                            loadingForm.UpdateProgressHandler(70);

                            // MongoDB 문서를 DataTable로 변환
                            return ConvertMongoDocumentsToDataTable(documents);
                        });

                        loadingForm.UpdateProgressHandler(80);

                        // UI 업데이트는 메인 스레드에서 수행
                        this.BeginInvoke(new Action(() =>
                        {
                            ConfigureDataGridView(result, dataGridView_target);
                            ConfigureDataGridView(result, dataGridView_process);
                            UpdatePaginationInfo();
                            ApplyGridFormatting();
                        }));

                        loadingForm.UpdateProgressHandler(100);
                        await Task.Delay(300);
                        loadingForm.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 페이지 데이터 로드 중 오류: {ex.Message}");
                MessageBox.Show($"데이터 로드 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // MongoDB 문서를 DataTable로 변환하는 헬퍼 메서드
        private DataTable ConvertMongoDocumentsToDataTable(List<RawDataDocument> documents)
        {
            DataTable dataTable = new DataTable();

            // 기본 컬럼 추가
            dataTable.Columns.Add("id", typeof(string));
            dataTable.Columns.Add("import_date", typeof(DateTime));
            dataTable.Columns.Add("hiddenYN", typeof(int));

            // 첫 번째 문서의 데이터를 기반으로 동적 컬럼 추가
            if (documents.Count > 0 && documents[0].Data != null)
            {
                foreach (var key in documents[0].Data.Keys)
                {
                    if (!dataTable.Columns.Contains(key))
                    {
                        dataTable.Columns.Add(key);
                    }
                }
            }

            // 문서 데이터를 DataTable에 추가
            foreach (var doc in documents)
            {
                DataRow row = dataTable.NewRow();
                row["id"] = doc.Id;
                row["import_date"] = doc.ImportDate;
                row["hiddenYN"] = doc.IsHidden ? 0 : 1;

                // 동적 데이터 필드 추가
                if (doc.Data != null)
                {
                    foreach (var kvp in doc.Data)
                    {
                        if (dataTable.Columns.Contains(kvp.Key))
                        {
                            row[kvp.Key] = kvp.Value ?? DBNull.Value;
                        }
                    }
                }

                dataTable.Rows.Add(row);
            }

            return dataTable;
        }


        // MongoDB 컬럼 목록을 그리드에 추가
        private async Task AddMongoColumnsToGrid(DataGridView targetDgv, DataColumnCollection columns)
        {
            // 대상 DataGridView 초기화
            targetDgv.DataSource = null;
            targetDgv.Rows.Clear();
            targetDgv.Columns.Clear();

            if (DataHandler.dragSelections.ContainsKey(targetDgv))
            {
                DataHandler.dragSelections[targetDgv].Clear();
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
            targetDgv.Columns.Add(checkColumn);

            // 데이터 컬럼 추가
            DataGridViewTextBoxColumn textColumn = new DataGridViewTextBoxColumn
            {
                Name = "Data",
                HeaderText = "컬럼명"
            };
            targetDgv.Columns.Add(textColumn);

            // GridView 설정
            targetDgv.AllowUserToAddRows = false;
            targetDgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            targetDgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            targetDgv.Columns["Data"].ReadOnly = true;
            targetDgv.Columns["CheckBox"].ReadOnly = false;
            targetDgv.Font = new System.Drawing.Font("맑은 고딕", 14.25F);

            // 컬럼 추가
            foreach (DataColumn column in columns)
            {
                if (!column.ColumnName.Equals("id") &&
                    !column.ColumnName.Equals("import_date") &&
                    !column.ColumnName.Equals("hiddenYN"))
                {
                    int rowIndex = targetDgv.Rows.Add();
                    targetDgv.Rows[rowIndex].Cells["CheckBox"].Value = true;
                    targetDgv.Rows[rowIndex].Cells["Data"].Value = column.ColumnName;
                }
            }
        }

        // MongoDB 컬럼 목록 가져오기
        private void GetMongoColumnList(DataColumnCollection columns)
        {
            process_col_list = new List<string>();

            foreach (DataColumn column in columns)
            {
                if (column.ColumnName != "id" &&
                    column.ColumnName != "import_date" &&
                    column.ColumnName != "hiddenYN")
                {
                    process_col_list.Add(column.ColumnName);
                }
            }

            Debug.WriteLine($"MongoDB process_col_list count: {process_col_list.Count}");
        }

        // 컬럼 목록 설정
        private void SetupColumnLists()
        {
            try
            {
                // ComboBox에 열 이름 추가 (공통 로직)
                SetupComboBox(stand_col_combo, "데이터 삭제 기준 열 선택");
                SetupComboBox(sub_acc_col_combo, "세목 열 선택");
                SetupComboBox(dept_col_combo, "부서 열 선택");
                SetupComboBox(prod_col_combo, "공급업체 열 선택");
                SetupComboBox(cmb_target, "키워드 대상 열 선택");
                SetupComboBox(cmb_money, "금액 열 선택");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"컬럼 목록 설정 중 오류 발생: {ex.Message}");
            }
        }

        // ComboBox 설정 공통 로직
        private void SetupComboBox(ComboBox comboBox, string defaultText)
        {
            comboBox.Items.Clear();
            comboBox.Items.Add(defaultText);

            foreach (string column in process_col_list)
            {
                comboBox.Items.Add(column);
            }

            if (comboBox.Items.Count > 0)
                comboBox.SelectedIndex = 0;
        }


        public void ConfigureDataGridView(DataTable dataTable, DataGridView dataGridView)
        {
            // DataGridView의 DataSource를 DataTable로 설정
            dataGridView.DataSource = dataTable;

            // hiddenYN 컬럼이 있는지 확인
            if (dataTable.Columns.Contains("hiddenYN"))
            {
                // hiddenYN 컬럼을 숨김
                dataGridView.Columns["hiddenYN"].Visible = false;

                // 각 행을 순회하며 hiddenYN 값이 0인 경우 스타일 적용
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    // hiddenYN 컬럼의 값이 0인지 확인
                    if (row.Cells["hiddenYN"].Value != null && row.Cells["hiddenYN"].Value.ToString() == "0")
                    {
                        //Debug.WriteLine($"hidden row Process row id : {row.Cells["id"].Value.ToString()} , row hiddenyn  : {row.Cells["hiddenYN"].Value.ToString()}");
                        // 배경색과 글자색 변경
                        row.DefaultCellStyle.BackColor = System.Drawing.Color.LightGray;
                        row.DefaultCellStyle.ForeColor = System.Drawing.Color.DarkGray;
                    }
                    else
                    {
                        //Debug.WriteLine($"hidden row Process row id : {row.Cells["id"].Value.ToString()} , row hiddenyn  : {row.Cells["hiddenYN"].Value.ToString()}");
                    }
                }
            }
            else
            {
                // hiddenYN 컬럼이 없는 경우 경고 메시지 출력 (옵션)
                MessageBox.Show("'hiddenYN' 컬럼이 존재하지 않습니다.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // 페이징 정보 업데이트
        private void UpdatePaginationInfo()
        {
            // NumericUpDown 범위 설정
            num_pageNumber.Maximum = Math.Max(1, totalPages);

            // 현재 페이지 값 설정 (이벤트 발생 방지를 위해 조건 체크)
            if (num_pageNumber.Value != currentPage)
                num_pageNumber.Value = currentPage;

            // 라벨 텍스트 업데이트
            lbl_pagination2.Text = $"/ {totalPages} (총 {totalRows:N0}행)";

            // 버튼 활성화/비활성화
            btn_prevPage.Enabled = currentPage > 1;
            btn_nextPage.Enabled = currentPage < totalPages;
        }

        // 그리드 형식 적용
        private void ApplyGridFormatting()
        {
            foreach (DataGridView dgv in new[] { dataGridView_target, dataGridView_process })
            {
                // AutoSizeColumnsMode 설정 제거
                dgv.AllowUserToAddRows = false;
                dgv.ReadOnly = true;
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

                // 헤더 스타일 설정
                dgv.EnableHeadersVisualStyles = false;
                dgv.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.LightSteelBlue;
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.Black;
                dgv.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("맑은 고딕", 9.0f, FontStyle.Bold);

                // 셀 폰트 설정
                dgv.DefaultCellStyle.Font = new System.Drawing.Font("맑은 고딕", 9.0f);
            }
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

            Debug.WriteLine(String.Join(", ", checkedData));

            return checkedData;
        }

        public class NonNumericData
        {
            public int RowIndex { get; set; }
            public string Value { get; set; }
        }

        //금액 컬럼 검증 로직
        //숫자 데이터가 아닌 데이터가 있다면 false return
        private (bool isAllNumeric, List<NonNumericData> nonNumericList) CheckNumericColumn(DataGridView dgv, string columnName)
        {
            var nonNumericList = new List<NonNumericData>();

            try
            {
                if (!dgv.Columns.Contains(columnName))
                {
                    throw new ArgumentException($"컬럼 '{columnName}'이 존재하지 않습니다.");
                }

                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.IsNewRow) continue;

                    var cellValue = row.Cells[columnName].Value;

                    if (cellValue == null || cellValue == DBNull.Value || string.IsNullOrWhiteSpace(cellValue.ToString()))
                        continue;

                    string strValue = cellValue.ToString().Trim();

                    if (!decimal.TryParse(strValue, out _))
                    {
                        nonNumericList.Add(new NonNumericData
                        {
                            RowIndex = row.Index,
                            Value = strValue
                        });
                    }
                }

                return (nonNumericList.Count == 0, nonNumericList);
            }
            catch (Exception ex)
            {
                throw new Exception($"컬럼 검사 중 오류 발생: {ex.Message}");
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

            try
            {
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    progressForm.UpdateProgressHandler(10);

                    // 선택된 컬럼 목록 생성
                    List<string> selectedColumns = new List<string>();
                    selectedColumns.Add(sub_acc_col_combo.SelectedItem.ToString());
                    selectedColumns.Add(dept_col_combo.SelectedItem.ToString());
                    selectedColumns.Add(prod_col_combo.SelectedItem.ToString());
                    selectedColumns.Add(cmb_money.SelectedItem.ToString());
                    selectedColumns.Add(cmb_target.SelectedItem.ToString());

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

                    // MongoDB 방식으로 변경
                    MongoDataConverter mongoConverter = new MongoDataConverter();
                    await mongoConverter.PrepareProcessDataAsync(selectedColumns);

                    progressForm.UpdateProgressHandler(70);

                    // 레거시 코드와의 호환을 위해 DataHandler.processTable 설정
                    // 변경 전: DataHandler.processTable = DBManager.Instance.ExecuteQuery("SELECT * FROM process_data");

                    // 변경 후: MongoDB에서 데이터 가져와 DataTable로 변환
                    DataHandler.processTable = await DataHandler.GetDataTableFromProcessDataAsync();

                    progressForm.UpdateProgressHandler(90);
                    progressForm.UpdateProgressHandler(100);
                }

                // 다음 단계로 이동
                await userControlHandler.uc_Preprocessing.initUI();

                if (this.ParentForm is Form1 form)
                {
                    form.LoadUserControl(userControlHandler.uc_Preprocessing);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"처리 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine($"btn_complete_Click 오류: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // 숫자 컬럼 체크 함수 - MongoDB 버전
        private async Task<(bool isAllNumeric, List<NonNumericData> nonNumericList)> CheckNumericColumnAsync(string columnName)
        {
            var nonNumericList = new List<NonNumericData>();

            try
            {
                // MongoDB에서 해당 필드를 가진 모든 문서 조회
                var filter = Builders<RawDataDocument>.Filter.Ne($"Data.{columnName}", BsonNull.Value);

                // 숨겨진 문서는 제외
                if (DataHandler.hiddenData)
                {
                    filter = Builders<RawDataDocument>.Filter.And(
                        filter,
                        Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false)
                    );
                }

                var documents = await rawDataRepo.FindDocumentsAsync(filter);
                int rowIndex = 0;

                foreach (var doc in documents)
                {
                    if (doc.Data != null && doc.Data.ContainsKey(columnName) && doc.Data[columnName] != null)
                    {
                        var value = doc.Data[columnName];
                        string strValue = value.ToString().Trim();

                        if (!string.IsNullOrEmpty(strValue))
                        {
                            // 숫자로 변환 가능한지 확인
                            if (!decimal.TryParse(strValue, out _))
                            {
                                nonNumericList.Add(new NonNumericData
                                {
                                    RowIndex = rowIndex,
                                    Value = strValue
                                });
                            }
                        }
                    }
                    rowIndex++;
                }

                return (nonNumericList.Count == 0, nonNumericList);
            }
            catch (Exception ex)
            {
                throw new Exception($"컬럼 검사 중 오류 발생: {ex.Message}");
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

            // 선택된 열이 없는 경우
            //if (restore_list.Count == 0) return;

            for (int i = 0; i < dataGridView_process.Columns.Count; i++)
            {
                if (restore_list.Contains(dataGridView_process.Columns[i].Name))
                {
                    dataGridView_process.Columns[i].Visible = true;

                    // SQLite에서 컬럼 가시성 업데이트
                    dataConverter.UpdateColumnVisibility(dataGridView_process.Columns[i].Name, true);
                }
                else
                {
                    dataGridView_process.Columns[i].Visible = false;

                    // SQLite에서 컬럼 가시성 업데이트
                    dataConverter.UpdateColumnVisibility(dataGridView_process.Columns[i].Name, false);
                }
            }

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
                dataGridView_delete_data.Font = new System.Drawing.Font("맑은 고딕", 14.25F);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"데이터 로드 중 오류: {ex.Message}");
                MessageBox.Show($"데이터 로드 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // MongoDB에서 필드의 고유값 가져오기
        private async Task<List<object>> GetDistinctValuesFromMongoAsync(string fieldName)
        {
            // 필드가 존재하는 모든 문서에서 고유 값을 가져오기
            //var filter = Builders<RawDataDocument>.Filter.Ne($"Data.{fieldName}", BsonNull.Value);
            var filterBuilder = Builders<RawDataDocument>.Filter;
            var filter = filterBuilder.And(
                filterBuilder.Exists($"Data.{fieldName}"),
                filterBuilder.Ne($"Data.{fieldName}", BsonNull.Value)
            );

            // 숨겨진 문서는 제외
            if (DataHandler.hiddenData)
            {
                filter = Builders<RawDataDocument>.Filter.And(
                    filter,
                    Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false)
                );
            }

            var distinctValues = new List<object>();
            var documents = await rawDataRepo.FindDocumentsAsync(filter);

            // 문서에서 해당 필드의 고유 값을 추출
            var valueSet = new HashSet<string>();
            foreach (var doc in documents)
            {
                if (doc.Data != null && doc.Data.ContainsKey(fieldName) && doc.Data[fieldName] != null)
                {
                    string value = doc.Data[fieldName].ToString();
                    if (!string.IsNullOrEmpty(value) && !valueSet.Contains(value))
                    {
                        valueSet.Add(value);
                        distinctValues.Add(value);
                    }
                }
            }

            // 값을 정렬
            distinctValues.Sort((a, b) => string.Compare(a.ToString(), b.ToString()));

            return distinctValues;
        }

        private async void restore_del_data_btn_Click(object sender, EventArgs e)
        {
            try
            {
                // 모든 그리드 선택 초기화 - 이 부분이 중요!
                InitializeCursors();


                DataHandler.hiddenData = false;

                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    progressForm.UpdateProgressHandler(20);

                    // 변경해야 할 코드
                    MongoDataConverter mongoConverter = new MongoDataConverter();
                    await mongoConverter.UnhideAllDocumentsAsync();

                    progressForm.UpdateProgressHandler(50);

                    // UI 업데이트
                    this.BeginInvoke(new Action(() =>
                    {
                        // 숨겨진 행 컬렉션 초기화
                        //hiddenRows.Clear();

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

        // 모든 행 표시 상태 및 스타일 복원
        private void RestoreAllRowsVisibility()
        {
            // Process 그리드
            foreach (DataGridViewRow row in dataGridView_process.Rows)
            {
                row.Visible = true;
            }

            // Target 그리드 - 스타일 초기화
            foreach (DataGridViewRow row in dataGridView_target.Rows)
            {
                row.DefaultCellStyle.BackColor = dataGridView_target.DefaultCellStyle.BackColor;
                row.DefaultCellStyle.ForeColor = dataGridView_target.DefaultCellStyle.ForeColor;
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

                    // MongoDB 데이터 컨버터 생성
                    var mongoConverter = new MongoDataConverter();

                    // MongoDB에서 숨기기 처리
                    int hiddenCount = 0;
                    await Task.Run(async () =>
                    {
                        // 각 값에 대해 해당하는 문서 숨기기
                        foreach (string value in delList)
                        {
                            // MongoDataConverter 클래스에 실제로 있는 메서드 호출
                            // 필드 값 기준으로 문서 숨기기
                            await mongoConverter.HideDocumentsByFieldAsync(selectedStandColumn, value, "사용자에 의해 숨겨짐");
                            hiddenCount++; // 여기서는 각 값마다 카운트 증가 (실제로는 숨겨진 문서 수를 반환받는 것이 좋음)
                        }
                    });

                    progressForm.UpdateProgressHandler(60);

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

                    // 페이지 데이터 리로드 (MongoDB 버전 사용)
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


        // 그리드 커서 초기화 메서드 분리
        private void InitializeCursors()
        {
            // 모든 그리드 선택 해제
            foreach (DataGridView dgv in new[] { dataGridView_target, dataGridView_process })
            {
                try
                {
                    // 현재 선택을 모두 제거
                    dgv.ClearSelection();

                    // 현재 셀을 null로 설정
                    dgv.CurrentCell = null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"그리드 커서 초기화 실패: {ex.Message}");
                }
            }

            // 이전에 처리했던 방식대로 추가 조치
            Application.DoEvents(); // UI 업데이트 허용
        }

        // 처리된 행을 삭제 데이터 그리드에서 제거
        private void RemoveProcessedRows(List<string> values)
        {
            for (int i = dataGridView_delete_data.Rows.Count - 1; i >= 0; i--)
            {
                DataGridViewRow row = dataGridView_delete_data.Rows[i];
                string value = row.Cells["Data"].Value?.ToString();
                if (values.Contains(value))
                {
                    dataGridView_delete_data.Rows.RemoveAt(i);
                }
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

        // MongoDB에서 검색을 위한 새 메서드
        private async Task<List<string>> SearchMongoFieldByKeywordsAsync(string fieldName, string[] keywords)
        {
            var resultValues = new List<string>();
            var valueSet = new HashSet<string>(); // 중복 방지를 위한 Set

            foreach (string keyword in keywords)
            {
                // 정규식 패턴 생성 (대소문자 구분 없이 검색)
                var regexPattern = new BsonRegularExpression(keyword, "i");

                // 필드 값이 검색 키워드를 포함하는 문서 필터
                var filter = Builders<RawDataDocument>.Filter.Regex($"Data.{fieldName}", regexPattern);

                // 숨겨진 문서는 제외
                if (DataHandler.hiddenData)
                {
                    filter = Builders<RawDataDocument>.Filter.And(
                        filter,
                        Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false)
                    );
                }

                // 문서 조회
                var documents = await rawDataRepo.FindDocumentsAsync(filter);

                // 결과에서 필드 값 추출
                foreach (var doc in documents)
                {
                    if (doc.Data != null && doc.Data.ContainsKey(fieldName) && doc.Data[fieldName] != null)
                    {
                        string value = doc.Data[fieldName].ToString();
                        if (!string.IsNullOrEmpty(value) && !valueSet.Contains(value))
                        {
                            valueSet.Add(value);
                            resultValues.Add(value);
                        }
                    }
                }
            }

            // 결과 정렬
            resultValues.Sort();

            return resultValues;
        }

        // 필터링된 결과로 DataGridView 채우기 - 기존 함수 유지
        private void PopulateDeleteDataGridWithResults(List<string> filteredValues)
        {
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

            // 필터링된 데이터 추가
            foreach (string value in filteredValues)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    int rowIndex = dataGridView_delete_data.Rows.Add();
                    dataGridView_delete_data.Rows[rowIndex].Cells["CheckBox"].Value = false;
                    dataGridView_delete_data.Rows[rowIndex].Cells["Data"].Value = value;
                }
            }

            // DataGridView 설정
            dataGridView_delete_data.AllowUserToAddRows = false;
            dataGridView_delete_data.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView_delete_data.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView_delete_data.ReadOnly = false;
            dataGridView_delete_data.Columns["CheckBox"].ReadOnly = false;
            dataGridView_delete_data.Font = new System.Drawing.Font("맑은 고딕", 14.25F);
        }

        // 필터링된 결과로 DataGridView 채우기
       

        private void delete_search_keyword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                delete_search_button_Click(sender, e);
                e.SuppressKeyPress = true;  // 비프음 방지
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
            messageLabel.Font = new System.Drawing.Font("맑은 고딕", 12F, FontStyle.Regular);

            this.Controls.Add(messageLabel);
        }
    }
}