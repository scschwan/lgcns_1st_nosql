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
        /// <summary>
        /// 컨트롤의 표시 상태 변경 시 호출되는 이벤트 핸들러
        /// </summary>
        /// <param name="e">이벤트 인수</param>
        /// <remarks>
        /// 컨트롤이 사용자에게 보여질 때마다 호출되어 필요한 초기화 작업 수행
        /// UI 레이아웃 갱신 및 사용자 환경 설정 적용
        /// </remarks>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (this.Visible)
            {
                // 화면이 보여질 때만 레이아웃 재계산
                RefreshLayouts();
            }
        }

        /// <summary>
        /// 모든 레이아웃을 새로고침
        /// </summary>
        /// <remarks>
        /// UI 컨트롤들의 위치와 크기를 재조정하여 사용자 화면에 적절히 표시
        /// 다양한 화면 해상도와 창 크기에 따른 레이아웃 자동 조정 기능
        /// </remarks>
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

        /// <summary>
        /// 페이징 컨트롤들을 초기화
        /// </summary>
        /// <param name="attachEvents">이벤트 핸들러 연결 여부</param>
        /// <remarks>
        /// 대용량 데이터의 효율적 표시를 위한 페이징 기능 설정
        /// 페이지 크기, 네비게이션 버튼, 상태 표시 등을 초기화
        /// </remarks>
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
        /// <summary>
        /// 페이징 관련 이벤트 핸들러들을 연결
        /// </summary>
        /// <remarks>
        /// 다음/이전 페이지 버튼, 페이지 사이즈 변경 등의 이벤트를 연결
        /// 대용량 데이터의 효율적 표시를 위한 페이징 기능 제공
        /// </remarks>
        private void AttachPagingEvents()
        {
            // 이벤트 등록
            cmb_pageSize.SelectedIndexChanged += cmb_pageSize_SelectedIndexChanged;
            num_pageNumber.ValueChanged += num_pageNumber_ValueChanged;
            //btn_prevPage.Click += btn_prevPage_Click;
            //btn_nextPage.Click += btn_nextPage_Click;
        }

        // 페이징 컨트롤 활성화/비활성화 메서드
        /// <summary>
        /// 페이징 컨트롤들의 활성/비활성 설정
        /// </summary>
        /// <param name="enabled">활성 여부 (true: 활성, false: 비활성)</param>
        /// <remarks>
        /// 데이터 로딩 중이나 오류 상황에서 사용자 인터랙션 제어
        /// 페이징 버튼, 페이지 사이즈 선택 등의 UI 요소들을 일괄 제어
        /// </remarks>
        private void EnablePagingControls(bool enabled)
        {
            btn_prevPage.Enabled = enabled;
            btn_nextPage.Enabled = enabled;
            num_pageNumber.Enabled = enabled;
            cmb_pageSize.Enabled = enabled;
        }

        /// <summary>
        /// MongoDB 컬럼 목록을 DataGridView에 추가
        /// </summary>
        /// <param name="targetDgv">컬럼을 추가할 대상 DataGridView</param>
        /// <param name="columns">추가할 컬럼 정보가 포함된 DataColumnCollection</param>
        /// <remarks>
        /// MongoDB의 컬럼 정보를 UI DataGridView에 동적으로 추가하여 사용자가 확인할 수 있도록 함
        /// 각 컬럼의 타입과 속성을 분석하여 적절한 표시 형식으로 변환
        /// </remarks>
        /// <exception cref="ArgumentNullException">targetDgv 또는 columns가 null인 경우</exception>
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
        /// <summary>
        /// MongoDB에서 컬럼 목록을 가져와서 UI에 설정
        /// </summary>
        /// <param name="columns">MongoDB 컬럼 정보가 포함된 DataColumnCollection</param>
        /// <remarks>
        /// MongoDB의 데이터 스키마를 기반으로 UI 컬럼 목록을 동적 생성
        /// 데이터 타입 및 제약 조건을 고려한 컬럼 설정
        /// </remarks>
        /// <exception cref="ArgumentNullException">columns가 null인 경우</exception>
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
        /// <summary>
        /// 컬럼 목록 설정 및 초기화
        /// </summary>
        /// <remarks>
        /// 데이터 원본, 목표, 처리 컬럼들의 목록을 설정하고 동기화
        /// ComboBox 컨트롤들을 업데이트하여 사용자에게 선택 옵션 제공
        /// 데이터 처리 파이프라인의 기초가 되는 컬럼 정보 준비
        /// </remarks>
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
        /// <summary>
        /// ComboBox 컨트롤의 기본 설정을 구성
        /// </summary>
        /// <param name="comboBox">설정할 ComboBox 컨트롤</param>
        /// <param name="defaultText">기본적으로 표시할 텍스트</param>
        /// <remarks>
        /// 드롭다운 컨트롤의 초기값 설정 및 스타일 지정
        /// 사용자 인터페이스의 일관성을 위한 표준화된 ComboBox 설정
        /// </remarks>
        /// <exception cref="ArgumentNullException">comboBox가 null인 경우</exception>
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


        /// <summary>
        /// DataGridView의 설정을 구성
        /// </summary>
        /// <param name="dataTable">바인딩할 데이터 테이블</param>
        /// <param name="dataGridView">설정할 DataGridView 컨트롤</param>
        /// <remarks>
        /// DataTable을 DataGridView에 바인딩하고 적절한 시각적 서식 적용
        /// 컬럼 너비, 행 스타일, 선택 모드 등을 비즈니스 요구에 맞게 설정
        /// 대용량 데이터 표시를 위한 성능 최적화 적용
        /// </remarks>
        /// <exception cref="ArgumentNullException">dataTable 또는 dataGridView가 null인 경우</exception>
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
        /// <summary>
        /// 페이지네이션 정보를 업데이트
        /// </summary>
        /// <remarks>
        /// 현재 페이지 번호, 전체 페이지 수, 데이터 개수 등을 계산하여 UI에 표시
        /// 대용량 데이터의 현재 상태와 네비게이션 정보를 사용자에게 제공
        /// </remarks>
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
        /// <summary>
        /// DataGridView에 서식 및 스타일을 적용
        /// </summary>
        /// <remarks>
        /// 컬럼, 행 스타일, 셀 포매트, 컬럼 너비 등을 설정하여 데이터의 가독성 향상
        /// 비즈니스 데이터의 표준화된 시각적 표현을 위한 UI 서식 적용
        /// </remarks>
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
        /// <summary>
        /// 체크된 행들의 데이터를 가져오는 메서드
        /// </summary>
        /// <param name="dgv">대상 DataGridView</param>
        /// <returns>체크된 행들의 데이터 목록</returns>
        /// <remarks>
        /// 사용자가 체크박스로 선택한 행들의 데이터를 추출
        /// 선택된 데이터를 배치 처리하거나 다른 작업에 활용 가능
        /// </remarks>
        /// <exception cref="ArgumentNullException">dgv가 null인 경우</exception>
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

        /// <summary>
        /// 지정된 컬럼의 숫자 데이터 여부를 비동기적으로 검사
        /// </summary>
        /// <param name="columnName">검사할 컬럼 이름</param>
        /// <returns>숫자 여부와 비숫자 데이터 목록을 포함한 튜플</returns>
        /// <remarks>
        /// 대용량 데이터에서 수치 연산이 가능한 컬럼인지 비동기적으로 검증
        /// 비숫자 데이터가 발견되면 상세 내역을 NonNumericData 객체로 반환
        /// 성능: Task 기반 비동기 처리로 대용량 데이터 효율적 처리
        /// </remarks>
        /// <exception cref="ArgumentException">columnName이 null이거나 비어있는 경우</exception>
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

        /// <summary>
        /// 모든 행의 표시 상태를 복원
        /// </summary>
        /// <remarks>
        /// 필터링이나 검색으로 숨겨진 행들을 다시 보이도록 설정
        /// DataGridView의 모든 행을 다시 visible 상태로 돌리는 기능
        /// </remarks>
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

        /// <summary>
        /// 커서를 초기 상태로 설정
        /// </summary>
        /// <remarks>
        /// 데이터 로딩 중이나 처리 작업 후 커서를 기본 상태로 복원
        /// 사용자 경험 향상을 위한 UI 상태 관리
        /// </remarks>
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


        /// <summary>
        /// 처리된 행들을 제거
        /// </summary>
        /// <param name="values">제거할 값들의 목록</param>
        /// <remarks>
        /// 지정된 값에 해당하는 데이터 행들을 DataGridView에서 제거
        /// 데이터 정리 및 버전 관리를 위한 사횩 행 제거 기능
        /// </remarks>
        /// <exception cref="ArgumentNullException">values가 null인 경우</exception>
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


        /// <summary>
        /// 필터링된 결과로 삭제 대상 DataGridView를 채움
        /// </summary>
        /// <param name="filteredValues">필터링된 값들의 목록</param>
        /// <remarks>
        /// 사용자가 선택한 조건에 따라 필터링된 데이터를 삭제 대상 그리드에 표시
        /// 삭제 작업 전 미리보기를 위한 데이터 시각화 기능
        /// </remarks>
        /// <exception cref="ArgumentNullException">filteredValues가 null인 경우</exception>
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


        /// <summary>
        /// 공급업체명 표준화 관련 컨트롤 초기화 및 이벤트 연결
        /// </summary>
        /// <remarks>
        /// 데이터 표준화를 위한 UI 컨트롤들을 설정하고 초기값을 지정
        /// 표준화 매핑 규칙과 사용자 인터페이스를 연결하여 일관된 데이터 처리 환경 제공
        /// </remarks>
        /// <exception cref="InvalidOperationException">컨트롤 초기화 중 오류 발생 시</exception>
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
        /// <summary>
        /// 컬럼 목록을 비동기적으로 로드
        /// </summary>
        /// <remarks>
        /// MongoDB에서 컬럼 정보를 비동기적으로 가져와서 UI 컬럼 목록을 업데이트
        /// 대용량 컬럼 데이터를 효율적으로 처리하기 위한 비동기 로딩
        /// UI 블록킹 없이 백그라운드에서 데이터 로드 수행
        /// </remarks>
        /// <exception cref="InvalidOperationException">데이터 로드 중 오류 발생 시</exception>
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
        /// <param name="excludeDgv">제외할 DataGridView (갱신 대상에서 제외)</param>
        /// <remarks>
        /// 지정된 DataGridView를 제외하고 다른 모든 DataGridView의 컬럼 순서를 업데이트
        /// 컬럼 순서 변경 시 다른 그리드에도 일관성 있게 적용하는 기능
        /// </remarks>
        /// <exception cref="ArgumentNullException">excludeDgv가 null인 경우</exception>
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
