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
using ClosedXML.Excel;
using FinanceTool.Data;
using System.Collections.Concurrent;
using System.Runtime;

using ExcelDataReader;
using DocumentFormat.OpenXml.Presentation;


namespace FinanceTool
{
    public partial class uc_FileLoad : UserControl
    {
        // 기존 필드
        private List<string> process_col_list = new List<string>();
        private string selectedStandColumn = "";
        //private HashSet<int> hiddenRows = new HashSet<int>();

        // SQLite 관련 추가 필드
        
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

        public uc_FileLoad()
        {
            InitializeComponent();
            InitializePagingControls(false);

        }
        // uc_FileLoad.cs에 추가
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (this.Visible)
            {
                // 화면이 보여질 때만 레이아웃 재계산
                RefreshLayouts();
            }
        }

        private void RefreshLayouts()
        {
            this.SuspendLayout();

            // TableLayoutPanel 재계산
            if (this.tableLayoutMain != null)
            {
                this.tableLayoutMain.SuspendLayout();
                this.tableLayoutMain.ResumeLayout(true);
                this.tableLayoutMain.PerformLayout();
            }

            this.ResumeLayout(true);
            this.PerformLayout();
        }

        public void InitializePagingControls(bool attachEvents)
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
            EnablePagingControls(true);

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
                try
                {
                    // MongoDB 초기화 상태 확인
                    var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                    bool isInitialized = await mongoManager.EnsureInitializedAsync();

                    if (!isInitialized)
                    {
                        MessageBox.Show("MongoDB 초기화에 실패했습니다.", "오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // MongoDB 데이터 존재 여부 확인
                    var rawDataCollection = await mongoManager.GetCollectionAsync<RawDataDocument>("raw_data");
                    var filter = Builders<RawDataDocument>.Filter.Empty;
                    long documentCount = await rawDataCollection.CountDocumentsAsync(filter);

                    bool dataExists = documentCount > 0;
                    bool resetRequired = MongoDBManager.ResetDatabaseOnStartup;

                    // 파일 최초 load의 경우 바로 초기화
                    if (excelLoadinitFlag && resetRequired)
                    {
                        using (var progressForm = new ProcessProgressForm())
                        {
                            progressForm.Show();
                            progressForm.UpdateProgressHandler(0, "MongoDB 데이터베이스 초기화 준비 중...");

                            // MongoDB 데이터베이스 초기화 - 진행 상황을 표시하면서 초기화
                            await mongoManager.ResetDatabaseAsync(progressForm.UpdateProgressHandler);

                            // 완료 메시지
                            await progressForm.UpdateProgressHandler(100, "초기화 완료");
                            await Task.Delay(500); // 사용자가 완료 메시지를 볼 수 있도록 짧은 지연
                            progressForm.Close();
                        }
                        excelLoadinitFlag = false;
                    }
                    // 기존 데이터가 있거나 MongoDB 리셋이 필요한 경우
                    else if (dataExists || resetRequired)
                    {
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
                        }

                        // 컬렉션의 문서 수에 따라 프로그레스바 표시
                        using (var progressForm = new ProcessProgressForm())
                        {
                            progressForm.Show();
                            progressForm.UpdateProgressHandler(0, "MongoDB 데이터베이스 초기화 중...");

                            // MongoDB 데이터베이스 초기화 - 진행 상황을 표시하면서 초기화
                            await mongoManager.ResetDatabaseAsync(progressForm.UpdateProgressHandler);

                            // 완료 메시지
                            await progressForm.UpdateProgressHandler(100, "초기화 완료");
                            await Task.Delay(500); // 사용자가 완료 메시지를 볼 수 있도록 짧은 지연
                            progressForm.Close();
                        }
                        Debug.WriteLine("MongoDB 데이터베이스 초기화 완료");
                    }

                    // 파일 로드 진행
                    string filePath = openFileDialog.FileName;
                    await LoadExcelDataAsync(filePath);
                    lbl_filename.Text = filePath;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"파일 로드 준비 중 오류가 발생했습니다: {ex.Message}", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Debug.WriteLine($"파일 로드 준비 오류: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }



        private async Task LoadExcelDataAsync(string filePath)
        {
            try
            {
                using (var progress = new ProcessProgressForm())
                {
                    progress.Show();
                    await progress.UpdateProgressHandler(5, "파일 업로드 준비 중...");
                    Application.DoEvents(); //

                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                    await progress.UpdateProgressHandler(10, "Excel 파일 스트리밍 로딩 중...");
                    Application.DoEvents(); //

                    Stopwatch sw = Stopwatch.StartNew();

                    var excelData = new DataTable();
                    int cpuCount = Environment.ProcessorCount;

                    using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet(new ExcelDataSetConfiguration
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration
                            {
                                UseHeaderRow = true
                            }
                        });

                        excelData = result.Tables[0];
                    }

                    sw.Stop();
                    Debug.WriteLine($"[ExcelDataReader] 엑셀 파싱 완료: {sw.ElapsedMilliseconds}ms, 행 수: {excelData.Rows.Count}, 열 수: {excelData.Columns.Count}");

                    await progress.UpdateProgressHandler(40, "MongoDB 저장 준비 중...");
                    Application.DoEvents(); 

                    var mongoConverter = new MongoDataConverter();

                    // 병렬 처리 옵션: CPU 코어 수 × 2
                    var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = cpuCount * 2 };

                    // MongoDB 저장
                    List<RawDataDocument> documents = await mongoConverter.ConvertExcelToMongoDBAsync(
                        excelData, Path.GetFileName(filePath), progress.UpdateProgressHandler, parallelOptions);

                    await progress.UpdateProgressHandler(90, "UI 초기화 중...");

                    if (!_fileLoaded)
                    {
                        AttachPagingEvents();
                        _fileLoaded = true;
                    }

                    EnablePagingControls(true);
                    DataHandler.excelData = excelData;
                    currentPage = 1;
                    pageSize = 1000;
                    await LoadMongoPagedDataAsync(true);
                    await AddMongoColumnsToGrid(dataGridView_delete_col, excelData.Columns);
                    await progress.UpdateProgressHandler(95, "컬럼 정보 설정 중...");
                    GetMongoColumnList(excelData.Columns);
                    SetupColumnLists();

                    await progress.UpdateProgressHandler(100, "데이터 로드 완료!");
                    progress.Close();

                    Debug.WriteLine("✅ Excel → MongoDB 업로드 완료");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"엑셀 파일 로드 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine($"엑셀 로딩 오류: {ex.Message}");
            }
        }


        // MongoDB 기반으로 페이징 데이터 로드
        public async Task LoadMongoPagedDataAsync(bool progressYN = false)
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
                    var filter = Builders<RawDataDocument>.Filter.Empty;

                    // hiddenData가 false인 경우, 숨겨진 문서 제외
                    if (!DataHandler.hiddenData)
                    {
                        filter = Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false);
                    }

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
                            // MongoDB에서 페이징된 데이터 가져오기
                            var filter = Builders<RawDataDocument>.Filter.Empty;

                            // hiddenData가 false인 경우, 숨겨진 문서 제외
                            if (!DataHandler.hiddenData)
                            {
                                filter = Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false);
                            }

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
            dataTable.Columns.Add("is_hidden", typeof(bool));  // hiddenYN 대신 is_hidden 사용

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
                row["is_hidden"] = doc.IsHidden;  // 직접 is_hidden 값 사용

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
        public async Task AddMongoColumnsToGrid(DataGridView targetDgv, DataColumnCollection columns)
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
            targetDgv.Font = new System.Drawing.Font("Pretendard", 14.25F);

            // 컬럼 추가
            foreach (DataColumn column in columns)
            {
                if (!column.ColumnName.Equals("id") &&
                    !column.ColumnName.Equals("_id") &&
                    !column.ColumnName.Equals("is_hidden") &&
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
        public void GetMongoColumnList(DataColumnCollection columns)
        {
            process_col_list = new List<string>();


            //Debug.WriteLine($"MongoDB DataColumnCollection : {columns.to}");
            //Debug.WriteLine($"DataColumnCollection: [{string.Join(", ", excelData.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}]");

            foreach (DataColumn column in columns)
            {
                //Debug.WriteLine($"column.ColumnName : {column.ColumnName}");
                if (column.ColumnName != "id" &&
                    column.ColumnName != "_id" &&
                    column.ColumnName != "import_date" &&
                    column.ColumnName != "is_hidden" &&
                    column.ColumnName != "hiddenYN")
                {
                    // 선택된 열(visibleColumns)이 있는 경우 그 열만 포함
                    if (DataHandler.visibleColumns == null ||
                        DataHandler.visibleColumns.Count == 0 ||
                        DataHandler.visibleColumns.Contains(column.ColumnName))
                    {
                        process_col_list.Add(column.ColumnName);
                    }
                }
            }

            Debug.WriteLine($"MongoDB process_col_list count: {process_col_list.Count}");
            //Debug.WriteLine($"MongoDB process_col_list: [{string.Join(", ", process_col_list)}]");
        }

        // 컬럼 목록 설정
        public void SetupColumnLists()
        {
            try
            {
                // ComboBox에 열 이름 추가 (공통 로직)
                SetupComboBox(stand_col_combo, "데이터 삭제 기준 열 선택");
                SetupComboBox(sub_acc_col_combo, "세목 열 선택");
                SetupComboBox(dept_col_combo, "코스트센터 열 선택");
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

            // id와 import_date 컬럼을 항상 숨김 처리
            if (dataGridView.Columns.Contains("id"))
            {
                dataGridView.Columns["id"].Visible = false;
            }

            if (dataGridView.Columns.Contains("import_date"))
            {
                dataGridView.Columns["import_date"].Visible = false;
            }

            // is_hidden 컬럼이 있다면 그것도 숨김
            if (dataGridView.Columns.Contains("is_hidden"))
            {
                dataGridView.Columns["is_hidden"].Visible = false;
            }

            // 각 행을 순회하며 is_hidden 필드에 따라 스타일 적용
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                bool isHidden = false;

                // is_hidden 컬럼 확인
                if (dataGridView.Columns.Contains("is_hidden") &&
                    row.Cells["is_hidden"].Value != null)
                {
                    isHidden = Convert.ToBoolean(row.Cells["is_hidden"].Value);
                }
                // 기존 hiddenYN 컬럼 확인 (하위 호환성 유지)
                else if (dataGridView.Columns.Contains("hiddenYN") &&
                         row.Cells["hiddenYN"].Value != null &&
                         row.Cells["hiddenYN"].Value.ToString() == "0")
                {
                    isHidden = true;
                }

                // 숨겨진 행이면 회색 스타일 적용
                if (isHidden)
                {
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.LightGray;
                    row.DefaultCellStyle.ForeColor = System.Drawing.Color.DarkGray;
                }
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
                dgv.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Pretendard", 9.0f, FontStyle.Bold);

                // 셀 폰트 설정
                dgv.DefaultCellStyle.Font = new System.Drawing.Font("Pretendard", 9.0f);
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

        // MongoDB에서 컬럼 가시성 업데이트하는 새 메서드
        private async void UpdateColumnVisibilityInMongo(string columnName, bool isVisible)
        {
            try
            {
                var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                var columnCollection = await mongoManager.GetCollectionAsync<BsonDocument>("column_mapping");

                var filter = Builders<BsonDocument>.Filter.Eq("original_name", columnName);
                var update = Builders<BsonDocument>.Update.Set("is_visible", isVisible);

                await columnCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 컬럼 가시성 업데이트 오류: {ex.Message}");
                // 오류 무시하고 계속 진행
            }
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
            dataGridView_delete_data.Font = new System.Drawing.Font("Pretendard", 14.25F);
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

        //2025.07.16
        //공급업체 표준화 함수

        /// <summary>
        /// 공급업체명 표준화 관련 컨트롤 초기화
        /// </summary>
        public async Task InitializeStandardizationControls()
        {
            try
            {
                Debug.WriteLine("공급업체명 표준화 컨트롤 초기화 시작");

                // 1. 숫자형 컬럼과 전체 컬럼 목록 로드
                await LoadColumnListsAsync();

                // 2. Key 콤보박스 설정 (숫자형 컬럼만)
                comboBox_standard_key.Items.Clear();
                comboBox_standard_key.Items.Add("-- Key 컬럼 선택 --");
                foreach (string column in _allColumns)
                {
                    comboBox_standard_key.Items.Add(column);
                }
                comboBox_standard_key.SelectedIndex = 0;

                // 3. Target 콤보박스 설정 (전체 컬럼)
                comboBox_standard_target.Items.Clear();
                comboBox_standard_target.Items.Add("-- 대상 컬럼 선택 --");
                foreach (string column in _allColumns)
                {
                    comboBox_standard_target.Items.Add(column);
                }
                comboBox_standard_target.SelectedIndex = 0;

                // 4. DataGridView 초기화
                InitializeStandardDataGridView();

                // 5. 이벤트 핸들러 등록
                comboBox_standard_key.SelectedIndexChanged += ComboBox_standard_key_SelectedIndexChanged;
                comboBox_standard_target.SelectedIndexChanged += ComboBox_standard_target_SelectedIndexChanged;
                standard_btn.Click += Standard_btn_Click;

                Debug.WriteLine($"컬럼 로드 완료 -  전체: {_allColumns.Count}개");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"표준화 컨트롤 초기화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// MongoDB에서 컬럼 목록 로드 (숫자형 컬럼 자동 감지)
        /// </summary>
        private async Task LoadColumnListsAsync()
        {
            try
            {
                _allColumns.Clear();

                // 샘플 데이터로 컬럼 분석
                var pipeline = new[]
                {
            new BsonDocument("$sample", new BsonDocument("size", 1000)),
            new BsonDocument("$project", new BsonDocument("data", 1))
        };

                var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                var collection = await mongoManager.GetCollectionAsync<RawDataDocument>("raw_data");
                var sampleDocs = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

                var allColumnSet = new HashSet<string>();

                // 샘플 데이터에서 모든 컬럼 추출
                foreach (var doc in sampleDocs)
                {
                    if (doc.Contains("data") && doc["data"].IsBsonDocument)
                    {
                        var dataDoc = doc["data"].AsBsonDocument;
                        foreach (var element in dataDoc.Elements)
                        {
                            allColumnSet.Add(element.Name);
                        }
                    }
                }

                _allColumns = allColumnSet.OrderBy(x => x).ToList();

                Debug.WriteLine($"컬럼 분석 완료 - 전체: {_allColumns.Count}개");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"컬럼 목록 로드 오류: {ex.Message}");
            }
        }

       
        /// <summary>
        /// 표준화 DataGridView 초기화
        /// </summary>
        private void InitializeStandardDataGridView()
        {
            try
            {
                dataGridView_standard.Columns.Clear();
                dataGridView_standard.Rows.Clear();

                // 컬럼 설정
                dataGridView_standard.Columns.Add("KeyValue", "Key 값");
                dataGridView_standard.Columns.Add("TargetValue", "대상값");
                dataGridView_standard.Columns.Add("Count", "Count");
                
                // 대상값 컬럼만 편집 가능하도록 설정 (uc_clustering 패턴)
                dataGridView_standard.Columns["KeyValue"].ReadOnly = true;
                dataGridView_standard.Columns["TargetValue"].ReadOnly = false;
                dataGridView_standard.Columns["Count"].ReadOnly = true;

                // DataGridView 속성 설정
                dataGridView_standard.AllowUserToAddRows = false;
                dataGridView_standard.AllowUserToDeleteRows = false;
                dataGridView_standard.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dataGridView_standard.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGridView_standard.Font = new System.Drawing.Font("Pretendard", 12F);

                // Count 컬럼 숫자 포맷 설정
                dataGridView_standard.Columns["Count"].DefaultCellStyle.Format = "N0";
                dataGridView_standard.Columns["Count"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                // 편집 완료 이벤트 등록
                dataGridView_standard.CellEndEdit += DataGridView_standard_CellEndEdit;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"표준화 DataGridView 초기화 오류: {ex.Message}");
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

        // 핵심 분석 및 처리 함수

        /// <summary>
        /// Key-Target 매핑 분석 및 표시
        /// </summary>
        private async Task AnalyzeKeyTargetMapping()
        {
            try
            {
                if (!ValidateStandardizationSelection())
                    return;

                string keyColumn = comboBox_standard_key.SelectedItem.ToString();
                string targetColumn = comboBox_standard_target.SelectedItem.ToString();

                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "매핑 분석 중...");

                    // MongoDB 집계 파이프라인으로 매핑 분석
                    _standardMappingData = await GetKeyTargetMappingDataAsync_Simple(keyColumn, targetColumn);

                    await progressForm.UpdateProgressHandler(80, "결과 표시 중...");

                    // DataGridView에 결과 표시
                    DisplayMappingResults();

                    await progressForm.UpdateProgressHandler(100, "완료");
                }

                Debug.WriteLine($"매핑 분석 완료: {_standardMappingData.Rows.Count}개 결과");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"매핑 분석 오류: {ex.Message}");
                MessageBox.Show($"매핑 분석 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// MongoDB에서 Key-Target 매핑 데이터 조회
        /// </summary>
        /// <summary>
        /// 단순화된 Key-Target 매핑 데이터 조회 (안전 버전)
        /// </summary>
        private async Task<DataTable> GetKeyTargetMappingDataAsync_Simple(string keyColumn, string targetColumn)
        {
            try
            {
                var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                var collection = await mongoManager.GetCollectionAsync<RawDataDocument>("raw_data");

                // 단순한 집계 파이프라인
                var pipeline = new[]
                {
            // 1단계: 필요한 필드만 추출
            new BsonDocument("$project", new BsonDocument
            {
                ["keyValue"] = $"$data.{keyColumn}",
                ["targetValue"] = $"$data.{targetColumn}"
            }),

            // 2단계: null 값 제외
            new BsonDocument("$match", new BsonDocument
            {
                ["keyValue"] = new BsonDocument("$ne", BsonNull.Value),
                ["targetValue"] = new BsonDocument("$ne", BsonNull.Value)
            })
        };

                var results = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

                // C# 코드에서 복합 타입 처리 및 그룹화
                var mappingDict = new Dictionary<(string key, string target), int>();

                foreach (var doc in results)
                {
                    try
                    {
                        // Key 값 추출
                        string keyValue = ExtractValue(doc["keyValue"]);
                        string targetValue = ExtractValue(doc["targetValue"]);

                        if (!string.IsNullOrEmpty(keyValue) && !string.IsNullOrEmpty(targetValue))
                        {
                            var key = (keyValue, targetValue);
                            if (mappingDict.ContainsKey(key))
                                mappingDict[key]++;
                            else
                                mappingDict[key] = 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"문서 처리 중 오류: {ex.Message}");
                        continue;
                    }
                }

                // DataTable 생성
                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("KeyValue", typeof(string));
                dataTable.Columns.Add("TargetValue", typeof(string));
                dataTable.Columns.Add("Count", typeof(int));
                

                // Key별 그룹화 및 순위 계산
                var groupedByKey = mappingDict.GroupBy(kvp => kvp.Key.key);

                foreach (var keyGroup in groupedByKey)
                {                   
                    var sortedTargets = keyGroup.OrderByDescending(kvp => kvp.Value);

                    foreach (var item in sortedTargets)
                    {
                        DataRow row = dataTable.NewRow();
                        row["KeyValue"] = keyGroup.Key;
                        row["TargetValue"] = item.Key.target;
                        row["Count"] = item.Value;                       
                        dataTable.Rows.Add(row);
                    }
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"단순 매핑 데이터 조회 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// BsonValue에서 실제 값 추출 (복합 타입 처리)
        /// </summary>
        private string ExtractValue(BsonValue bsonValue)
        {
            try
            {
                if (bsonValue.IsString)
                    return bsonValue.AsString;

                if (bsonValue.IsNumeric)
                    return bsonValue.ToString();

                // 복합 타입 (_t, _v 구조) 처리
                if (bsonValue.IsBsonDocument)
                {
                    var doc = bsonValue.AsBsonDocument;
                    if (doc.Contains("_v"))
                    {
                        var valueDoc = doc["_v"];
                        if (valueDoc.IsBsonDocument && valueDoc.AsBsonDocument.Contains("$numberDecimal"))
                        {
                            return valueDoc.AsBsonDocument["$numberDecimal"].AsString;
                        }
                        else if (valueDoc.IsNumeric)
                        {
                            return valueDoc.ToString();
                        }
                        else if (valueDoc.IsString)
                        {
                            return valueDoc.AsString;
                        }
                    }
                }

                return bsonValue.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 매핑 결과를 DataGridView에 표시
        /// </summary>
        private void DisplayMappingResults()
        {
            try
            {
                dataGridView_standard.Rows.Clear();

                if (_standardMappingData != null && _standardMappingData.Rows.Count > 0)
                {
                    foreach (DataRow row in _standardMappingData.Rows)
                    {
                        int rowIndex = dataGridView_standard.Rows.Add();
                        dataGridView_standard.Rows[rowIndex].Cells["KeyValue"].Value = row["KeyValue"];
                        dataGridView_standard.Rows[rowIndex].Cells["TargetValue"].Value = row["TargetValue"];
                        dataGridView_standard.Rows[rowIndex].Cells["Count"].Value = row["Count"];
                       
                    }

                    // 컬럼 폭 자동 조정
                    dataGridView_standard.AutoResizeColumns();
                }

                Debug.WriteLine($"매핑 결과 표시 완료: {_standardMappingData?.Rows.Count ?? 0}개 항목");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"결과 표시 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 표준화 수행 (최다 빈도 값으로 통일)
        /// </summary>
        private async Task PerformStandardization()
        {
            try
            {
                if (_standardMappingData == null || _standardMappingData.Rows.Count == 0)
                {
                    MessageBox.Show("먼저 매핑 분석을 수행해주세요.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult result = MessageBox.Show(
                    "표준화를 수행하면 각 Key 값별로 최다 빈도의 대상값으로 통일됩니다.\n" +
                    "이 작업은 되돌릴 수 없습니다. 계속하시겠습니까?",
                    "표준화 수행 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;

                string keyColumn = comboBox_standard_key.SelectedItem.ToString();
                string targetColumn = comboBox_standard_target.SelectedItem.ToString();

                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "표준화 대상 분석 중...");

                    // Key별 최다 빈도 값 추출
                    var standardValues = GetStandardValuesFromMapping();

                    await progressForm.UpdateProgressHandler(30, "MongoDB 일괄 업데이트 중...");

                    // MongoDB 일괄 업데이트 수행
                    int updatedCount = await PerformBulkStandardization(keyColumn, targetColumn, standardValues);

                    await progressForm.UpdateProgressHandler(70, "매핑 데이터 재분석 중...");

                    // 매핑 데이터 재분석
                    await AnalyzeKeyTargetMapping();

                    await progressForm.UpdateProgressHandler(90, "페이징 데이터 새로고침 중...");

                    // 페이징 데이터 새로고침
                    await LoadMongoPagedDataAsync();

                    await progressForm.UpdateProgressHandler(100, "완료");

                    MessageBox.Show($"표준화 완료: {updatedCount}개 문서 업데이트", "완료",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"표준화 수행 오류: {ex.Message}");
                MessageBox.Show($"표준화 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 매핑 데이터에서 Key별 표준값(최다 빈도) 추출
        /// </summary>
        private Dictionary<string, string> GetStandardValuesFromMapping()
        {
            var standardValues = new Dictionary<string, string>();

            var keyGroups = _standardMappingData.AsEnumerable()
                .GroupBy(row => row["KeyValue"].ToString());

            foreach (var keyGroup in keyGroups)
            {
                // 각 Key별로 Count가 가장 높은 항목을 표준값으로 설정
                var standardRow = keyGroup.OrderByDescending(row => Convert.ToInt32(row["Count"])).FirstOrDefault();
                if (standardRow != null)
                {
                    standardValues[keyGroup.Key] = standardRow["TargetValue"].ToString();
                }
            }

            return standardValues;
        }

        /// <summary>
        /// MongoDB 일괄 표준화 수행
        /// </summary>
        private async Task<int> PerformBulkStandardization(string keyColumn, string targetColumn, Dictionary<string, string> standardValues)
        {
            try
            {
                var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                var collection = await mongoManager.GetCollectionAsync<BsonDocument>("raw_data");
                int totalUpdated = 0;

                // Key별로 배치 업데이트 수행
                foreach (var kvp in standardValues)
                {
                    string keyValue = kvp.Key;
                    string standardTarget = kvp.Value;

                    Debug.WriteLine($"=== Key '{keyValue}' 처리 시작 ===");

                    // 단순하게 문자열 매칭으로 변경
                    // 복합 타입을 고려한 필터 (매핑 분석과 동일한 방식)
                    var filter = CreateUniversalFilter(keyColumn, keyValue);

                    // 업데이트 전 매칭되는 문서 수 확인
                    long matchCount = await collection.CountDocumentsAsync(filter);
                    Debug.WriteLine($"Key '{keyValue}' 매칭 문서 수: {matchCount}개");

                    if (matchCount > 0)
                    {
                        var update = Builders<BsonDocument>.Update.Set($"data.{targetColumn}", standardTarget);
                        var updateResult = await collection.UpdateManyAsync(filter, update);
                        totalUpdated += (int)updateResult.ModifiedCount;

                        Debug.WriteLine($"Key '{keyValue}' 표준화 완료: {updateResult.ModifiedCount}개 문서 업데이트");
                    }
                    else
                    {
                        Debug.WriteLine($"Key '{keyValue}' 매칭되는 문서가 없습니다.");

                        // 해당 키값이 실제로 존재하는지 확인
                        await DebugSpecificKey(keyColumn, keyValue);
                    }
                }

                return totalUpdated;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"일괄 표준화 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 복합 타입과 문자열 타입을 모두 처리하는 통합 필터 생성
        /// </summary>
        private FilterDefinition<BsonDocument> CreateUniversalFilter(string fieldName, string value)
        {
            var filters = new List<FilterDefinition<BsonDocument>>();

            // 1. 문자열 직접 매칭
            filters.Add(Builders<BsonDocument>.Filter.Eq($"data.{fieldName}", value));

            // 2. 복합 타입 (_v 필드) 매칭 - 문자열로
            filters.Add(Builders<BsonDocument>.Filter.Eq($"data.{fieldName}._v", value));

            // 3. 숫자로 변환 가능한 경우 숫자 매칭
            if (decimal.TryParse(value, out decimal numericValue))
            {
                filters.Add(Builders<BsonDocument>.Filter.Eq($"data.{fieldName}._v", numericValue));
            }

            // OR 조건으로 결합
            return Builders<BsonDocument>.Filter.Or(filters);
        }

        /// <summary>
        /// 특정 키값의 존재 여부를 정확히 확인하는 디버깅 함수
        /// </summary>
        private async Task DebugSpecificKey(string keyColumn, string keyValue)
        {
            try
            {
                // 함수 시작 부분에 추가
                string targetColumn = comboBox_standard_target.SelectedItem.ToString();

                var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                var collection = await mongoManager.GetCollectionAsync<BsonDocument>("raw_data");

                Debug.WriteLine($"=== '{keyValue}' 키값 존재 여부 확인 ===");

                // 해당 키값을 가진 문서 직접 검색
                //var specificFilter = Builders<BsonDocument>.Filter.Eq($"data.{keyColumn}._v.$numberDecimal", keyValue);
                //var specificFilter = Builders<BsonDocument>.Filter.Eq($"data.{keyColumn}._v.$", keyValue);
                var filter = CreateUniversalFilter(keyColumn, keyValue);
                var specificCount = await collection.CountDocumentsAsync(filter);

                Debug.WriteLine($"정확한 필터로 찾은 '{keyValue}' 문서 수: {specificCount}개");

                if (specificCount > 0)
                {
                    // 실제 문서 몇 개 조회해서 구조 확인
                    var specificDocs = await collection.Find(filter).Limit(3).ToListAsync();
                    Debug.WriteLine($"'{keyValue}' 키값을 가진 실제 문서들:");
                    foreach (var doc in specificDocs)
                    {
                        Debug.WriteLine($"  - {keyColumn}: {doc["data"][keyColumn]}");
                        if (doc["data"].AsBsonDocument.Contains(targetColumn))
                        {
                            Debug.WriteLine($"  - {targetColumn}: {doc["data"][targetColumn]}");
                        }
                        Debug.WriteLine("  ---");
                    }
                }
                else
                {
                    // 전체 고유 키값들 조회 (처음 20개만)
                    var uniqueKeysPipeline = new[]
                        {
                        new BsonDocument("$project", new BsonDocument
                        {
                            ["keyValue"] = new BsonDocument("$cond", new BsonArray
                            {
                                // 복합 타입인지 확인
                                new BsonDocument("$eq", new BsonArray { new BsonDocument("$type", $"$data.{keyColumn}"), "object" }),
                                // 복합 타입이면 _v 값 사용
                                $"$data.{keyColumn}._v",
                                // 아니면 직접 값 사용
                                $"$data.{keyColumn}"
                            })
                        }),
                        new BsonDocument("$group", new BsonDocument
                        {
                            ["_id"] = "$keyValue",
                            ["count"] = new BsonDocument("$sum", 1)
                        }),
                        new BsonDocument("$sort", new BsonDocument("count", -1)),
                        new BsonDocument("$limit", 20)
                    };

                    var uniqueKeys = await collection.Aggregate<BsonDocument>(uniqueKeysPipeline).ToListAsync();
                    Debug.WriteLine($"실제 데이터베이스의 상위 20개 {keyColumn} 값들:");
                    foreach (var keyDoc in uniqueKeys)
                    {
                        Debug.WriteLine($"  - {keyDoc["_id"]} (문서 수: {keyDoc["count"]})");
                    }
                }

                Debug.WriteLine("=== 키값 확인 완료 ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"특정 키값 디버깅 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// MongoDB에서 특정 Target 값 일괄 변경
        /// </summary>
        private async Task UpdateTargetValueInMongoDB(string keyValue, string oldTargetValue, string newTargetValue)
        {
            try
            {
                var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                var collection = await mongoManager.GetCollectionAsync<BsonDocument>("raw_data");
                string keyColumn = comboBox_standard_key.SelectedItem.ToString();
                string targetColumn = comboBox_standard_target.SelectedItem.ToString();

                var keyFilter = CreateUniversalFilter(keyColumn, keyValue);
                var filter = Builders<BsonDocument>.Filter.And(
                    keyFilter,
                    Builders<BsonDocument>.Filter.Eq($"data.{targetColumn}", oldTargetValue)
                );

                var update = Builders<BsonDocument>.Update.Set($"data.{targetColumn}", newTargetValue);

                var result = await collection.UpdateManyAsync(filter, update);

                Debug.WriteLine($"Target 값 변경 완료: {result.ModifiedCount}개 문서 업데이트 ({oldTargetValue} → {newTargetValue})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Target 값 변경 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 표준화 선택 유효성 검사
        /// </summary>
        private bool ValidateStandardizationSelection()
        {
            if (comboBox_standard_key.SelectedIndex <= 0)
            {
                MessageBox.Show("Key 컬럼을 선택해주세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (comboBox_standard_target.SelectedIndex <= 0)
            {
                MessageBox.Show("대상 컬럼을 선택해주세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (comboBox_standard_key.SelectedItem.ToString() == comboBox_standard_target.SelectedItem.ToString())
            {
                MessageBox.Show("Key 컬럼과 대상 컬럼은 다르게 선택해주세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
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