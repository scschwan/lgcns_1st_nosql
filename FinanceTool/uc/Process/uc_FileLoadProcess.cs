using ExcelDataReader;
using FinanceTool.Data;
using FinanceTool.MongoModels;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTool
{
    public partial class uc_FileLoad
    {
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

        private async Task LoadExcelDataAsync(string filePath)
        {
            try
            {
                using (var progress = new ProcessProgressForm())
                {
                    progress.Show();
                    await progress.UpdateProgressHandler(5, "파일 업로드 준비 중...");
                    Application.DoEvents(); //

                    // ✅ 컬럼 순서 초기화 추가
                    await DataHandler_fileLoad.LoadColumnOrderFromMongoDB();

                    // ✅ 컬럼 이벤트 핸들러 초기화
                    InitializeColumnOrderHandling();

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
        /// BsonValue에서 실제 값 추출 (기존 함수 최적화)
        /// </summary>
        private string ExtractValue(BsonValue bsonValue)
        {
            try
            {
                if (bsonValue.IsString)
                    return bsonValue.AsString;
                if (bsonValue.IsNumeric)
                    return bsonValue.ToString();
                if (bsonValue.IsBsonDocument)
                {
                    var doc = bsonValue.AsBsonDocument;
                    if (doc.Contains("_v"))
                    {
                        var valueDoc = doc["_v"];
                        if (valueDoc.IsBsonDocument && valueDoc.AsBsonDocument.Contains("$numberDecimal"))
                            return valueDoc.AsBsonDocument["$numberDecimal"].AsString;
                        else if (valueDoc.IsNumeric)
                            return valueDoc.ToString();
                        else if (valueDoc.IsString)
                            return valueDoc.AsString;
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
        /// 다른 DataGridView들의 컬럼 순서 갱신
        /// </summary>
        private void RefreshOtherDataGridViewOrders(DataGridView excludeDgv)
        {
            try
            {
                var dataGridViews = new[] { dataGridView_target, dataGridView_process };

                foreach (var dgv in dataGridViews)
                {
                    if (dgv == excludeDgv || dgv.DataSource == null) continue;

                    // 현재 DataSource를 순서 적용하여 재설정
                    if (dgv.DataSource is DataTable currentTable)
                    {
                        var orderedTable = DataHandler_fileLoad.ApplyColumnOrder(currentTable);
                        dgv.DataSource = orderedTable;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"다른 DataGridView 갱신 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 시스템 컬럼 여부 확인
        /// </summary>
        private bool IsSystemColumn(string columnName)
        {
            return columnName == "id" ||
                   columnName == "_id" ||
                   columnName == "import_date" ||
                   columnName == "is_hidden" ||
                   columnName == "hiddenYN";
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














        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    }
}
