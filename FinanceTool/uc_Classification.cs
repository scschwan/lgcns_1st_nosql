using DocumentFormat.OpenXml.Wordprocessing;
using FinanceTool.MongoModels;
using FinanceTool.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
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
    public partial class uc_Classification : UserControl
    {
        DataTable export_result = new DataTable();
        DataTable cluster_result = new DataTable();
        List<string> process_col_list = new List<string>();

        private DataConverter dataConverter;
        private int currentPage = 1;
        private int pageSize = 1000;
        private int totalPages = 1;
        private int totalRows = 0;

        private bool isProcessingSearch = false;

        public uc_Classification()
        {
            InitializeComponent();
            //lb_priority.Items.Add("총 금액");
        }

        // uc_Classification.cs의 initUI 메서드 - MongoDB 활용
        // initUI 함수를 수정하여 전체 진행 과정에 프로그레스바 적용
        public async void initUI()
        {
            try
            {
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "초기화 준비 중...");
                    await Task.Delay(10);

                    // 1. MongoDB에서 visible 컬럼 목록 가져오기
                    await progressForm.UpdateProgressHandler(20, "컬럼 정보 로드 중...");
                    await GetColumnListAsync();

                    // 2. 클러스터링 데이터 로드 및 강화
                    await progressForm.UpdateProgressHandler(30, "클러스터링 데이터 로드 중...");
                    DataTable enhancedClusteringData = await CreateEnhancedClusteringDataAsync();

                    // 3. 페이징된 데이터 로드 (isAlreadyProgress = true로 설정)
                    await progressForm.UpdateProgressHandler(50, "페이지 데이터 로드 중...");
                    await LoadPagedDataAsync(true);

                    // 4. 클러스터링 데이터를 DataGridView에 표시
                    await progressForm.UpdateProgressHandler(80, "UI 컴포넌트 초기화 중...");
                    await Task.Run(() =>
                    {
                        if (Application.OpenForms.Count > 0)
                        {
                            Application.OpenForms[0].Invoke((MethodInvoker)delegate
                            {
                                CreateCheckDataGridView(dataGridView_classify, enhancedClusteringData);
                            });
                        }
                    });

                    await progressForm.UpdateProgressHandler(100, "초기화 완료");
                    await Task.Delay(100);
                    progressForm.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"initUI 메서드 오류: {ex.Message}");
                MessageBox.Show($"초기화 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // 클러스터링 데이터 강화 메서드 (MongoDB 사용)
        // 클러스터링 데이터 로드 및 raw_data 정보로 강화
        private async Task<DataTable> CreateEnhancedClusteringDataAsync()
        {
            // 1. 클러스터링 데이터 로드 (메모리 또는 MongoDB에서)
            DataTable clusteringData;
            var clusteringRepo = new ClusteringRepository();

            // 메모리에 있는 경우 활용
            if (DataHandler.finalClusteringData != null && DataHandler.finalClusteringData.Rows.Count > 0)
            {
                Debug.WriteLine("메모리에 캐싱된 클러스터링 데이터 사용");
                clusteringData = DataHandler.finalClusteringData.Copy();
            }
            else
            {
                // MongoDB에서 로드
                Debug.WriteLine("MongoDB에서 클러스터링 데이터 로드");
                clusteringData = await clusteringRepo.ToDataTableAsync();
                DataHandler.finalClusteringData = clusteringData.Copy();
            }

            // 2. 강화된 데이터 테이블 생성
            DataTable enhancedTable = clusteringData.Copy();

            // 공급업체명과 부서명 컬럼 추가 (없는 경우)
            if (!enhancedTable.Columns.Contains(DataHandler.prod_col_name))
                enhancedTable.Columns.Add(DataHandler.prod_col_name, typeof(string));

            if (!enhancedTable.Columns.Contains(DataHandler.dept_col_name))
                enhancedTable.Columns.Add(DataHandler.dept_col_name, typeof(string));

            // 3. 클러스터별 dataIndex 수집
            Dictionary<int, List<string>> clusterToDataIndices = new Dictionary<int, List<string>>();

            foreach (DataRow row in enhancedTable.Rows)
            {
                if (row.IsNull("ClusterID")) continue;

                int clusterId = Convert.ToInt32(row["ClusterID"]);
                string dataIndexStr = row["dataIndex"]?.ToString();

                if (string.IsNullOrEmpty(dataIndexStr)) continue;

                if (!clusterToDataIndices.ContainsKey(clusterId))
                    clusterToDataIndices[clusterId] = new List<string>();

                foreach (string indexStr in dataIndexStr.Split(','))
                {
                    string trimmedIndex = indexStr.Trim();
                    if (!string.IsNullOrEmpty(trimmedIndex))
                        clusterToDataIndices[clusterId].Add(trimmedIndex);
                }
            }

            // 4. MongoDB에서 raw_data 정보로 강화
            // 각 클러스터에 대해 raw_data 정보 조회 및 추가
            var rawDataRepo = new RawDataRepository();

            foreach (var entry in clusterToDataIndices)
            {
                int clusterId = entry.Key;
                List<string> dataIndices = entry.Value;

                if (dataIndices.Count == 0) continue;

                var filter = Builders<RawDataDocument>.Filter.In(d => d.Id, dataIndices);
                var rawDataDocs = await rawDataRepo.FindDocumentsAsync(filter);

                // 공급업체 및 부서명 추출
                HashSet<string> uniqueProds = new HashSet<string>();
                HashSet<string> uniqueDepts = new HashSet<string>();

                foreach (var doc in rawDataDocs)
                {
                    // 공급업체명
                    if (doc.Data.TryGetValue(DataHandler.prod_col_name, out var prod) && prod != null)
                        uniqueProds.Add(prod.ToString());

                    // 부서명
                    if (doc.Data.TryGetValue(DataHandler.dept_col_name, out var dept) && dept != null)
                        uniqueDepts.Add(dept.ToString());
                }

                // 쉼표로 구분된 문자열로 변환
                string combinedProds = string.Join(",", uniqueProds);
                string combinedDepts = string.Join(",", uniqueDepts);

                // 문자열 길이 제한
                if (combinedProds.Length > 32767)
                    combinedProds = combinedProds.Substring(0, 32767);

                if (combinedDepts.Length > 32767)
                    combinedDepts = combinedDepts.Substring(0, 32767);

                // enhancedTable에 값 설정
                foreach (DataRow row in enhancedTable.Rows)
                {
                    if (!row.IsNull("ClusterID") && Convert.ToInt32(row["ClusterID"]) == clusterId)
                    {
                        row[DataHandler.prod_col_name] = combinedProds;
                        row[DataHandler.dept_col_name] = combinedDepts;
                    }
                }
            }

            return enhancedTable;
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


            // 컨트롤 활성화 
            //EnablePagingControls(false);

            // 이벤트 등록은 옵션에 따라 결정
            if (attachEvents)
            {
                AttachPagingEvents();
            }

            // 초기 페이징 상태
            UpdatePaginationInfo();

            DataHandler.RegisterDataGridView(dataGridView_delete_col2);
        }

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
            foreach (DataGridView dgv in new[] { dataGridView_origin, dataGridView_keyword })
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

       

        
       
       
        public void CreateCheckDataGridView(DataGridView dgv, DataTable dt)
        {
            // 조건에 맞는 데이터만 필터링
            var filteredData = dt.AsEnumerable()
                .Where(row =>
                    Convert.ToInt32(row["ClusterID"]) <= 0 ||
                    Convert.ToInt32(row["ClusterID"]) == Convert.ToInt32(row["ID"]))
                .CopyToDataTable();

            dgv.DataSource = filteredData;

            // ID 컬럼 숨기기
            dgv.Columns["ID"].Visible = false;
            // ClusterID 컬럼 숨기기
            dgv.Columns["ClusterID"].Visible = false;

            // dataIndex 컬럼 숨기기
            dgv.Columns["dataIndex"].Visible = false;

            if (dgv.Columns["Count"] != null)
            {
                dgv.Columns["Count"].DefaultCellStyle.Format = "N0"; // 천 단위 구분자
                dgv.Columns["Count"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            }

            if (dgv.Columns["합산금액"] != null)
            {
                dgv.Columns["합산금액"].DefaultCellStyle.Format = "N0"; // 천 단위 구분자
                dgv.Columns["합산금액"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            // DataGridView 속성 설정
            dgv.AllowUserToAddRows = false;
            //dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // 나머지 컬럼들은 읽기 전용으로 설정
            for (int i = 1; i < dgv.Columns.Count; i++)
            {
                dgv.Columns[i].ReadOnly = true;
            }

            dgv.Columns["클러스터명"].ReadOnly = false;  // 클러스터명 편집 가능
            //dgv.CellEndEdit += DataGridView_CellEndEdit;
            //dgv.Font = new System.Drawing.Font("맑은 고딕", 14.25F);
            dgv.Font = new System.Drawing.Font("맑은 고딕", 9F);
            // "클러스터명" 컬럼의 배경색을 연노란색으로 설정
            dgv.Columns["클러스터명"].DefaultCellStyle.BackColor = System.Drawing.Color.LightYellow;
        }



        public DataTable ConvertDataGridViewToCustomDataTable(DataGridView dgv)
        {
            try
            {
                // 새 DataTable 생성
                DataTable result = new DataTable();

                // 열 정보 가져오기
                List<int> columnsToKeep = new List<int>();
                List<int> decimalColumns = new List<int>();

                for (int i = 0; i < dgv.Columns.Count; i++)
                {
                    // 1, 0, 1, 6번 컬럼 제외 (인덱스 기준)
                    if (i != 0 && i != 1 && i != 6)
                    {
                        columnsToKeep.Add(i);

                        // 4, 5번 컬럼은 decimal로 변환 (인덱스 기준)
                        if (i == 4 || i == 5)
                        {
                            decimalColumns.Add(i);
                        }
                    }
                }

                // 유지할 열 추가
                int newColIndex = 0;
                foreach (int originalIndex in columnsToKeep)
                {
                    DataGridViewColumn originalColumn = dgv.Columns[originalIndex];
                    Type dataType = typeof(string); // 기본 타입은 string

                    // decimal로 변환할 열 처리
                    if (decimalColumns.Contains(originalIndex))
                    {
                        dataType = typeof(decimal);
                    }

                    result.Columns.Add(originalColumn.HeaderText, dataType);
                    newColIndex++;
                }

                // 행 데이터 추가
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (!row.IsNewRow)
                    {
                        DataRow newRow = result.NewRow();
                        newColIndex = 0;

                        foreach (int originalIndex in columnsToKeep)
                        {
                            object cellValue = row.Cells[originalIndex].Value;

                            // 2번, 3번 컬럼의 경우 100자로 제한
                            if (originalIndex == 2 || originalIndex == 3)
                            {
                                if (cellValue != null && cellValue != DBNull.Value)
                                {
                                    string strValue = cellValue.ToString();
                                    if (strValue.Length > 100)
                                    {
                                        cellValue = strValue.Substring(0, 97) + "...";
                                    }
                                }
                            }


                            // decimal 컬럼 처리
                            if (decimalColumns.Contains(originalIndex))
                            {
                                if (cellValue != null && cellValue != DBNull.Value)
                                {
                                    // 숫자로 변환 시도
                                    if (decimal.TryParse(cellValue.ToString(), out decimal decValue))
                                    {
                                        newRow[newColIndex] = decValue;
                                    }
                                    else
                                    {
                                        newRow[newColIndex] = 0m; // 변환 실패 시 0으로 설정
                                    }
                                }
                                else
                                {
                                    newRow[newColIndex] = 0m;
                                }
                            }
                            else
                            {
                                newRow[newColIndex] = cellValue ?? DBNull.Value;
                            }

                            newColIndex++;
                        }

                        result.Rows.Add(newRow);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DataGridView 변환 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Excel로 데이터를 내보내는 함수 - MongoDB 버전으로 개선
        /// </summary>
        public async Task ExportToExcelAsync(List<string> columnList, bool hiddenTableYN = false)
        {
            try
            {
                using (var progress = new ProcessProgressForm())
                {
                    progress.Show();
                    await progress.UpdateProgressHandler(5, "데이터 내보내기 준비 중...");
                    await Task.Delay(10);

                    // 1단계: export_result 데이터 테이블 생성 (raw_data 컬렉션에서 데이터 로드)
                    DataTable export_result = null;

                    await Task.Run(async () =>
                    {
                        try
                        {
                            // MongoDB에서 raw_data 문서 조회
                            var rawDataRepo = new RawDataRepository();

                            // 필터 설정 - 숨겨진 문서 처리
                            var filter = hiddenTableYN ?
                                Builders<RawDataDocument>.Filter.Empty :
                                Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false);

                            await progress.UpdateProgressHandler(10, "MongoDB 데이터 조회 중...");

                            // 모든 문서 가져오기 - 페이징 사용 (대용량 데이터 처리)
                            List<RawDataDocument> allDocuments = new List<RawDataDocument>();
                            int batchSize = 1000;
                            int currentBatch = 0;
                            bool hasMoreData = true;

                            while (hasMoreData)
                            {
                                var skip = currentBatch * batchSize;
                                var sort = Builders<RawDataDocument>.Sort.Ascending(d => d.Id);

                                var batch = await rawDataRepo.FindDocumentsAsync(filter, sort, skip, batchSize);

                                if (batch.Count == 0)
                                {
                                    hasMoreData = false;
                                }
                                else
                                {
                                    allDocuments.AddRange(batch);
                                    currentBatch++;

                                    // 진행 상황 업데이트 (5% ~ 50% 사이로 배분)
                                    int progressValue = 10 + (int)(40.0 * allDocuments.Count / (currentBatch * batchSize + 1));
                                    await progress.UpdateProgressHandler(progressValue, $"데이터 로드 중... ({allDocuments.Count:N0}건)");
                                }
                            }

                            Debug.WriteLine($"총 {allDocuments.Count:N0}개 문서 로드 완료");
                            await progress.UpdateProgressHandler(50, "데이터 변환 중...");

                            // MongoDB 문서를 DataTable로 변환
                            export_result = ConvertRawDocumentsToEnhancedDataTable(allDocuments, columnList);

                            // 클러스터링 정보 추가
                            await progress.UpdateProgressHandler(60, "클러스터 정보 추가 중...");
                            await AddClusterInfoToExportDataAsync(export_result);

                            await progress.UpdateProgressHandler(70, "데이터 내보내기 준비 완료");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"데이터 로드 중 오류: {ex.Message}\n{ex.StackTrace}");
                            throw; // 예외를 상위로 전파
                        }
                    });

                    // 2단계: cluster_result 데이터 테이블 생성
                    await progress.UpdateProgressHandler(75, "클러스터 정보 변환 중...");
                    DataTable cluster_result = ConvertDataGridViewToCustomDataTable(dataGridView_classify);

                    // 3단계: Excel 저장
                    await progress.UpdateProgressHandler(90, "Excel 파일 저장 중...");
                    DataHandler.SaveDataTableToExcel(cluster_result, export_result);

                    await progress.UpdateProgressHandler(100, "Excel 파일 저장 완료");
                    await Task.Delay(500); // 완료 메시지 표시
                }

                // 저장 완료 메시지
                MessageBox.Show("Excel 파일로 내보내기가 완료되었습니다.", "내보내기 완료",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excel 파일 저장 중 오류 발생: {ex.Message}");
                MessageBox.Show($"Excel 파일 저장 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// MongoDB 문서를 확장된 DataTable로 변환하는 메서드 (컬럼 필터링 포함)
        /// </summary>
        private DataTable ConvertRawDocumentsToEnhancedDataTable(List<RawDataDocument> documents, List<string> columnList)
        {
            DataTable dataTable = new DataTable();

            // 기본 컬럼 추가
            dataTable.Columns.Add("id", typeof(string));
            dataTable.Columns.Add("import_date", typeof(DateTime));

            // columnList에 명시된 컬럼만 추가
            foreach (string columnName in columnList)
            {
                if (!dataTable.Columns.Contains(columnName))
                {
                    dataTable.Columns.Add(columnName);
                }
            }

            // 클러스터명 컬럼 추가 (없을 경우)
            if (!dataTable.Columns.Contains("클러스터명"))
            {
                dataTable.Columns.Add("클러스터명", typeof(string));
            }

            // 문서 데이터를 DataTable에 추가
            foreach (var doc in documents)
            {
                DataRow row = dataTable.NewRow();
                row["id"] = doc.Id;
                row["import_date"] = doc.ImportDate;

                // 동적 데이터 필드 추가 (columnList에 있는 것만)
                if (doc.Data != null)
                {
                    foreach (var kvp in doc.Data)
                    {
                        if (columnList.Contains(kvp.Key) && dataTable.Columns.Contains(kvp.Key))
                        {
                            row[kvp.Key] = kvp.Value ?? DBNull.Value;
                        }
                    }
                }

                // 일단 클러스터명은 비워둠 (나중에 채울 예정)
                row["클러스터명"] = "";

                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        /// <summary>
        /// 내보내기 데이터에 클러스터 정보 추가
        /// </summary>
        private async Task AddClusterInfoToExportDataAsync(DataTable exportData)
        {
            if (exportData == null || exportData.Rows.Count == 0)
                return;

            try
            {
                // 클러스터링 데이터 로드
                var clusteringRepo = new ClusteringRepository();
                var allClusters = await clusteringRepo.GetAllAsync();

                // 클러스터 ID별 이름 매핑 생성
                Dictionary<int, string> clusterNameMap = new Dictionary<int, string>();
                foreach (var cluster in allClusters.Where(c => c.ClusterId == c.ClusterNumber)) // 상위 클러스터만 선택
                {
                    clusterNameMap[cluster.ClusterNumber] = cluster.ClusterName;
                }

                // 문서 ID별 클러스터 매핑 생성
                Dictionary<string, int> docIdToClusterMap = new Dictionary<string, int>();
                foreach (var cluster in allClusters)
                {
                    if (cluster.DataIndices != null)
                    {
                        foreach (var docId in cluster.DataIndices)
                        {
                            // 클러스터가 다른 클러스터에 병합된 경우 최상위 클러스터 ID 사용
                            int topClusterId = cluster.ClusterId > 0 ? cluster.ClusterId : cluster.ClusterNumber;
                            docIdToClusterMap[docId] = topClusterId;
                        }
                    }
                }

                // exportData의 각 행에 클러스터 정보 추가
                foreach (DataRow row in exportData.Rows)
                {
                    if (row["id"] != DBNull.Value)
                    {
                        string docId = row["id"].ToString();

                        if (docIdToClusterMap.TryGetValue(docId, out int clusterId) &&
                            clusterNameMap.TryGetValue(clusterId, out string clusterName))
                        {
                            row["클러스터명"] = clusterName;
                        }
                    }
                }

                Debug.WriteLine($"클러스터 정보 추가 완료: {exportData.Rows.Count}행");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터 정보 추가 중 오류: {ex.Message}");
                // 오류 발생해도 계속 진행 (부분적으로라도 내보내기 위함)
            }
        }

        private async void btn_save_excel_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
               $"Excel File을 생성하시겠습니까?",
               "Excel 생성",
               MessageBoxButtons.YesNo,
               MessageBoxIcon.Question
           );

            if (result == DialogResult.Yes)
            {
                

                await ExportToExcelAsync(process_col_list, DataHandler.hiddenData);
            }
            else
            {
                return;
            }


        }

        // MongoDB에서 visible 컬럼 목록 가져오기
        private async Task GetColumnListAsync()
        {
            try
            {
                process_col_list = new List<string>();

                // MongoDB의 column_mapping 컬렉션에서 visible 컬럼 가져오기
                var columnMappingRepo = new ColumnMappingRepository();
                var visibleColumns = await columnMappingRepo.GetVisibleColumnsAsync();

                foreach (var column in visibleColumns)
                {
                    process_col_list.Add(column.OriginalName);
                }

                // import_date 제외 (필요한 경우)
                process_col_list.Remove("import_date");
                Debug.WriteLine($"process_col_list count: {process_col_list.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"컬럼 목록 조회 중 오류: {ex.Message}");
                throw; // 상위 메서드에서 처리하도록 예외 전파
            }
        }



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



        private async void dataGridView_classify_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            Debug.WriteLine("call dataGridView_classify_CellValueChanged");
            // "클러스터명" 컬럼이 변경되었을 때만 처리

            if (e.ColumnIndex == dataGridView_classify.Columns["클러스터명"].Index && e.RowIndex >= 0)
            {
                //UpdateClusterName(dataGridView_keyword, DataHandler.processTable, DataHandler.finalClusteringData, e.RowIndex);

                // 수정된 값 가져오기
                string newValue = dataGridView_classify.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";

                if ("".Equals(newValue))
                {
                    //함수 재조회로 변경
                    CreateCheckDataGridView(dataGridView_classify, DataHandler.finalClusteringData);
                    return;
                }

                // DataHandler.finalClusteringData 업데이트
                int id = Convert.ToInt32(dataGridView_classify.Rows[e.RowIndex].Cells["ID"].Value);
                DataRow[] rows = DataHandler.finalClusteringData.Select($"ID = {id}");
                if (rows.Length > 0)
                {
                    rows[0]["클러스터명"] = newValue;
                }

                Debug.WriteLine($"변경된 클러스터명 {newValue}");

                // 변경 사항 저장
                DataHandler.finalClusteringData.AcceptChanges();

                //화면 갱신
                await LoadPagedDataAsync();

                //PopulateDataGridViewWithClusterNames(dataGridView_keyword, DataHandler.processTable, DataHandler.finalClusteringData);
            }

        }


        private async void restore_col_btn_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> restore_list = GetCheckedRowsData(dataGridView_delete_col2);
                Debug.WriteLine($"선택된 컬럼 수: {restore_list.Count}, 컬럼 목록: {string.Join(", ", restore_list)}");

                // 선택된 컬럼이 없는 경우 (restore_list.Count == 0) - 이 부분이 수정됨
                // 모든 컬럼을 숨기는 작업으로 처리
                // 이 조건 검사와 MessageBox 표시 부분 제거

                // 진행 상황 표시 폼 생성
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "컬럼 가시성 업데이트 준비 중...");

                    // UI에 바로 적용 - 선택된 열만 표시
                    foreach (DataGridViewColumn column in dataGridView_keyword.Columns)
                    {
                        // 부서명, 공급업체명, 세목명, 타겟열, 클러스터명은 제외
                        if (column.Name.Equals(DataHandler.dept_col_name) ||
                            column.Name.Equals(DataHandler.prod_col_name) ||
                            column.Name.Equals(DataHandler.sub_acc_col_name))
                        {
                            column.Visible = true;
                            continue;
                        }

                        if (DataHandler.levelName.Contains(column.Name) ||
                            "클러스터명".Equals(column.Name))
                        {
                            column.Visible = true;
                            continue;
                        }

                        // 시스템 컬럼은 항상 숨김
                        if (column.Name == "id" || column.Name == "raw_data_id" ||
                            column.Name == "import_date" || column.Name == "processed_date" ||
                            column.Name == "cluster_id" || column.Name == "cluster_name" ||
                            column.Name == "is_hidden")
                        {
                            column.Visible = false;
                            continue;
                        }

                        // 체크 여부에 따라 표시/숨김 설정
                        column.Visible = restore_list.Contains(column.Name);
                        Debug.WriteLine($"컬럼 가시성 설정: {column.Name}, Visible: {column.Visible}");
                    }

                    await progressForm.UpdateProgressHandler(30, "MongoDB 업데이트 중...");

                    // MongoDB 컬렉션에서 컬럼 가시성 업데이트
                    // 동시에 여러 컬럼을 업데이트하기 위한 Task 목록
                    List<Task> updateTasks = new List<Task>();

                    // 컬럼 목록 가져오기
                    var columnMappingRepo = new ColumnMappingRepository();
                    var allColumns = await columnMappingRepo.GetAllAsync();

                    // 모든 컬럼에 대해 업데이트 작업 생성
                    foreach (var column in allColumns)
                    {
                        // 필수 컬럼 로직은 그대로 유지
                        if (column.OriginalName.Equals(DataHandler.dept_col_name) ||
                            column.OriginalName.Equals(DataHandler.prod_col_name) ||
                            column.OriginalName.Equals(DataHandler.sub_acc_col_name) ||
                            DataHandler.levelName.Contains(column.OriginalName) ||
                            "클러스터명".Equals(column.OriginalName))
                        {
                            continue;
                        }

                        // MongoDB에서 컬럼 매핑 정보 업데이트
                        bool isVisible = restore_list.Contains(column.OriginalName);

                        // 변경이 필요한 경우만 업데이트
                        if (column.IsVisible != isVisible)
                        {
                            updateTasks.Add(UpdateColumnVisibilityInMongoAsync(column.OriginalName, isVisible));
                        }
                    }

                    // 모든 업데이트 작업 완료 대기
                    if (updateTasks.Count > 0)
                    {
                        await Task.WhenAll(updateTasks);
                        Debug.WriteLine($"{updateTasks.Count}개 컬럼 가시성 업데이트 완료");
                    }

                    await progressForm.UpdateProgressHandler(70, "컬럼 목록 업데이트 중...");

                    // 컬럼 목록 업데이트
                    await GetColumnListAsync();

                    // dataGridView_delete_col2 다시 업데이트 (비동기 메서드 사용)
                    await AddSelectedColumnToGridAsync(dataGridView_delete_col2, dataGridView_keyword);

                    await progressForm.UpdateProgressHandler(100, "컬럼 가시성 업데이트 완료");
                    await Task.Delay(300); // 완료 메시지 표시를 위한 지연
                    progressForm.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"컬럼 가시성 업데이트 중 오류: {ex.Message}");
                MessageBox.Show($"컬럼 가시성 업데이트 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // MongoDB에서 컬럼 가시성 업데이트하는 비동기 메서드
        private async Task UpdateColumnVisibilityInMongoAsync(string columnName, bool isVisible)
        {
            try
            {
                var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                var columnCollection = await mongoManager.GetCollectionAsync<BsonDocument>("column_mapping");

                var filter = Builders<BsonDocument>.Filter.Eq("original_name", columnName);
                var update = Builders<BsonDocument>.Update.Set("is_visible", isVisible);

                var result = await columnCollection.UpdateOneAsync(filter, update);
                Debug.WriteLine($"컬럼 '{columnName}' 가시성 업데이트: Visible={isVisible}, 결과={result.ModifiedCount}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB {columnName} 컬럼 가시성 업데이트 오류: {ex.Message}");
                // 오류 발생 시에도 계속 진행 (개별 컬럼 업데이트 실패가 전체에 영향을 주지 않도록)
            }
        }

        private void del_col_list_allcheck_CheckedChanged(object sender, EventArgs e)
        {
            // 모든 행의 체크박스 상태 변경
            foreach (DataGridViewRow row in dataGridView_delete_col2.Rows)
            {
                row.Cells[0].Value = del_col_list_allcheck.Checked;
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
            await LoadPagedDataAsync();
        }

        // 페이징된 데이터 로드 메서드 (MongoDB 사용) - raw_data 활용 수정
        // LoadPagedDataAsync 함수를 수정하여 isAlreadyProgress 매개변수 추가
        private async Task LoadPagedDataAsync(bool isAlreadyProgress = false)
        {
            if (isProcessingSearch) return;

            try
            {
                isProcessingSearch = true;

                // isAlreadyProgress가 true면 별도의 프로그레스바를 표시하지 않음
                if (!isAlreadyProgress)
                {
                    using (var loadingForm = new ProcessProgressForm())
                    {
                        loadingForm.Show();
                        await loadingForm.UpdateProgressHandler(10, "데이터 로드 준비 중...");
                        await Task.Delay(10);

                        await PerformLoadPagedData(loadingForm.UpdateProgressHandler);

                        await loadingForm.UpdateProgressHandler(100, "데이터 로드 완료");
                        await Task.Delay(100);
                        loadingForm.Close();
                    }
                }
                else
                {
                    // 외부에서 프로그레스바가 이미 표시되고 있는 경우
                    await PerformLoadPagedData(null);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"페이지 데이터 로드 중 오류: {ex.Message}\n{ex.StackTrace}");
                if (!isAlreadyProgress) // 이미 외부 프로그레스바가 있으면 메시지 박스를 표시하지 않음
                {
                    MessageBox.Show($"데이터 로드 중 오류 발생: {ex.Message}", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                isProcessingSearch = false;
            }
        }

        // 실제 데이터 로드 로직을 분리하는 헬퍼 메서드
        // 실제 데이터 로드 로직을 분리하는 헬퍼 메서드
        private async Task PerformLoadPagedData(ProcessProgressForm.UpdateProgressDelegate progressHandler = null)
        {
            // 컬럼 가시성 정보
            List<ColumnMappingDocument> visibleColumns = null;
            DataTable pageData = null;

            await Task.Run(async () =>
            {
                try
                {
                    // 1. MongoDB에서 visible 컬럼 목록 조회
                    var columnMappingRepo = new ColumnMappingRepository();
                    visibleColumns = await columnMappingRepo.GetVisibleColumnsAsync();
                    Debug.WriteLine($"조회된 가시적 컬럼 수: {visibleColumns.Count}");

                    // 진행 상황 업데이트 (해당하는 경우) - 수정된 부분
                    if (progressHandler != null)
                    {
                        await progressHandler(20, "컬럼 정보 로드 완료");
                    }

                    // 2. MongoDB에서 raw_data 로드
                    var mongoConverter = new MongoDataConverter();
                    var (documents, totalCount) = await mongoConverter.GetPagedRawDataAsync(
                        currentPage, pageSize, DataHandler.hiddenData);

                    // 메타데이터 업데이트
                    totalRows = (int)totalCount;
                    totalPages = (int)Math.Ceiling((double)totalRows / pageSize);

                    // MongoDB 문서를 DataTable로 변환
                    pageData = ConvertRawDocumentsToDataTable(documents);
                    Debug.WriteLine($"변환된 pageData 컬럼 수: {pageData.Columns.Count}");

                    // 진행 상황 업데이트 (해당하는 경우) - 수정된 부분
                    if (progressHandler != null)
                    {
                        await progressHandler(70, "데이터 로드 완료");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"데이터 로드 작업 중 오류: {ex.Message}");
                    throw; // 예외를 상위로 전파
                }
            });

            // 진행 상황 업데이트 (해당하는 경우) - 수정된 부분
            if (progressHandler != null)
            {
                await progressHandler(80, "UI 업데이트 중...");
            }

            // UI 업데이트
            try
            {
                // DataGridView 업데이트
                if (pageData != null)
                {
                    // 원본 그리드와 키워드 그리드 모두 동일한 데이터로 설정
                    ConfigureDataGridView(pageData, dataGridView_origin);
                    ConfigureDataGridView(pageData, dataGridView_keyword);

                    Debug.WriteLine($"dataGridView_keyword 설정 완료 (컬럼 수: {dataGridView_keyword.Columns.Count})");

                    // 컬럼 가시성 적용
                    if (visibleColumns != null && visibleColumns.Count > 0)
                    {
                        ApplyColumnVisibilityExplicit(dataGridView_keyword, visibleColumns);
                        Debug.WriteLine("컬럼 가시성 적용 완료");
                    }
                }

                // dataGridView_delete_col2 업데이트 - 컬럼 목록 채우기 (비동기 메서드 사용)
                await AddSelectedColumnToGridAsync(dataGridView_delete_col2, dataGridView_keyword);
                Debug.WriteLine($"dataGridView_delete_col2 설정 완료 (행 수: {dataGridView_delete_col2.Rows.Count})");

                UpdatePaginationInfo();
                ApplyGridFormatting();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UI 업데이트 중 오류: {ex.Message}\n{ex.StackTrace}");
            }

            // 진행 상황 업데이트 (해당하는 경우) - 수정된 부분
            if (progressHandler != null)
            {
                await progressHandler(90, "데이터 로드 마무리 중...");
            }
        }

        // 컬럼 가시성 적용 함수 - 명시적 처리 방식
        private void ApplyColumnVisibilityExplicit(DataGridView dgv, List<ColumnMappingDocument> visibleColumns)
        {
            if (dgv == null || visibleColumns == null || visibleColumns.Count == 0)
            {
                Debug.WriteLine("ApplyColumnVisibilityExplicit: 파라미터가 null이거나 빈 컬렉션입니다.");
                return;
            }

            // 가시적 컬럼 목록 생성
            HashSet<string> visibleColumnNames = new HashSet<string>(
                visibleColumns.Select(c => c.OriginalName)
            );

            Debug.WriteLine($"ApplyColumnVisibilityExplicit: visibleColumnNames 개수 = {visibleColumnNames.Count}");

            // 항상 표시해야 하는 필수 컬럼 목록
            HashSet<string> essentialColumns = new HashSet<string>();

            // 클러스터명 추가
            essentialColumns.Add("클러스터명");

            // 데이터 처리 관련 필수 컬럼 추가
            if (!string.IsNullOrEmpty(DataHandler.dept_col_name))
                essentialColumns.Add(DataHandler.dept_col_name);

            if (!string.IsNullOrEmpty(DataHandler.prod_col_name))
                essentialColumns.Add(DataHandler.prod_col_name);

            if (!string.IsNullOrEmpty(DataHandler.sub_acc_col_name))
                essentialColumns.Add(DataHandler.sub_acc_col_name);

            // 레벨 컬럼 추가
            if (DataHandler.levelName != null)
            {
                foreach (var levelName in DataHandler.levelName)
                {
                    if (!string.IsNullOrEmpty(levelName))
                        essentialColumns.Add(levelName);
                }
            }

            Debug.WriteLine($"필수 컬럼 목록: {string.Join(", ", essentialColumns)}");

            // 항상 숨겨야 하는 시스템 컬럼 목록
            HashSet<string> systemColumns = new HashSet<string>
    {
        "id", "import_date", "is_hidden"
    };

            // 모든 컬럼 상태 로깅 (디버깅)
            foreach (DataGridViewColumn column in dgv.Columns)
            {
                Debug.WriteLine($"컬럼 처리 전: {column.Name}, Visible: {column.Visible}");
            }

            // 각 컬럼에 대해 가시성 설정
            foreach (DataGridViewColumn column in dgv.Columns)
            {
                try
                {
                    string columnName = column.Name;

                    // 시스템 컬럼은 항상 숨김
                    if (systemColumns.Contains(columnName))
                    {
                        column.Visible = false;
                        Debug.WriteLine($"시스템 컬럼 숨김: {columnName}");
                        continue;
                    }

                    // 필수 컬럼은 항상 표시
                    if (essentialColumns.Contains(columnName))
                    {
                        column.Visible = true;
                        Debug.WriteLine($"필수 컬럼 표시: {columnName}");
                        continue;
                    }

                    // 가시적 컬럼 목록에 있는 컬럼만 표시
                    bool isVisible = visibleColumnNames.Contains(columnName);
                    column.Visible = isVisible;
                    Debug.WriteLine($"일반 컬럼 가시성 설정: {columnName}, Visible: {isVisible}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"컬럼 가시성 설정 중 오류: {column.Name}, {ex.Message}");
                }
            }

            // 모든 컬럼 상태 로깅 (디버깅)
            foreach (DataGridViewColumn column in dgv.Columns)
            {
                Debug.WriteLine($"컬럼 처리 후: {column.Name}, Visible: {column.Visible}");
            }
        }

        // 필수 컬럼 목록을 가져오는 헬퍼 함수 추가
        private HashSet<string> GetEssentialColumns()
        {
            HashSet<string> essentialColumns = new HashSet<string>();

            // 클러스터명 추가
            essentialColumns.Add("클러스터명");

            // 데이터 처리 관련 필수 컬럼 추가
            if (!string.IsNullOrEmpty(DataHandler.dept_col_name))
                essentialColumns.Add(DataHandler.dept_col_name);

            if (!string.IsNullOrEmpty(DataHandler.prod_col_name))
                essentialColumns.Add(DataHandler.prod_col_name);

            if (!string.IsNullOrEmpty(DataHandler.sub_acc_col_name))
                essentialColumns.Add(DataHandler.sub_acc_col_name);

            // 레벨 컬럼 추가
            if (DataHandler.levelName != null)
            {
                foreach (var levelName in DataHandler.levelName)
                {
                    if (!string.IsNullOrEmpty(levelName))
                        essentialColumns.Add(levelName);
                }
            }

            return essentialColumns;
        }


        // MongoDB RawDataDocument를 DataTable로 변환
        private DataTable ConvertRawDocumentsToDataTable(List<RawDataDocument> documents)
        {
            DataTable dataTable = new DataTable();

            // 기본 컬럼 추가
            dataTable.Columns.Add("id", typeof(string));
            dataTable.Columns.Add("import_date", typeof(DateTime));
            dataTable.Columns.Add("is_hidden", typeof(bool));

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
                row["is_hidden"] = doc.IsHidden;

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

        // MongoDB ProcessDataDocument를 DataTable로 변환
        // DataTable에 컬럼 정확히 처리하도록 변환 함수 개선
        private DataTable ConvertProcessDocumentsToDataTable(List<ProcessDataDocument> documents)
        {
            DataTable dataTable = new DataTable();

            Debug.WriteLine($"ConvertProcessDocumentsToDataTable 시작 (문서 수: {documents.Count})");

            // 기본 컬럼 추가
            dataTable.Columns.Add("id", typeof(string));
            dataTable.Columns.Add("raw_data_id", typeof(string));
            dataTable.Columns.Add("import_date", typeof(DateTime));
            dataTable.Columns.Add("processed_date", typeof(DateTime));
            dataTable.Columns.Add("cluster_id", typeof(int));
            dataTable.Columns.Add("cluster_name", typeof(string));
            dataTable.Columns.Add("클러스터명", typeof(string)); // 클러스터명 컬럼 추가

            // 동적 컬럼 수집 - 모든 문서의 Data 필드를 검사하여 컬럼 통합
            HashSet<string> columnSet = new HashSet<string>();

            foreach (var doc in documents)
            {
                if (doc.Data != null)
                {
                    foreach (var key in doc.Data.Keys)
                    {
                        columnSet.Add(key);
                    }
                }
            }

            Debug.WriteLine($"동적 컬럼 수집 완료: {columnSet.Count}개 컬럼 발견");

            // 컬럼 추가
            foreach (var columnName in columnSet)
            {
                if (!dataTable.Columns.Contains(columnName))
                {
                    dataTable.Columns.Add(columnName);
                    Debug.WriteLine($"컬럼 추가: {columnName}");
                }
            }

            // 문서 데이터를 DataTable에 추가
            foreach (var doc in documents)
            {
                DataRow row = dataTable.NewRow();
                row["id"] = doc.Id;
                row["raw_data_id"] = doc.RawDataId;
                row["import_date"] = doc.ImportDate;
                row["processed_date"] = doc.ProcessedDate;

                // 클러스터 정보
                if (doc.ClusterId.HasValue)
                {
                    row["cluster_id"] = doc.ClusterId.Value;
                }
                else
                {
                    row["cluster_id"] = DBNull.Value;
                }

                row["cluster_name"] = doc.ClusterName ?? "";
                row["클러스터명"] = doc.ClusterName ?? "";

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

            Debug.WriteLine($"ConvertProcessDocumentsToDataTable 완료: {dataTable.Rows.Count}행, {dataTable.Columns.Count}열");
            return dataTable;
        }


        // DataTable에 클러스터링 정보 추가
        private async Task<DataTable> AddClusteringInfoToDataTableAsync(DataTable dataTable)
        {
            try
            {
                // 클러스터명 컬럼이 없으면 추가
                if (!dataTable.Columns.Contains("클러스터명"))
                {
                    dataTable.Columns.Add("클러스터명", typeof(string));
                }

                // 메모리 캐싱된 클러스터링 데이터 활용
                DataTable clusteringData;
                if (DataHandler.finalClusteringData != null && DataHandler.finalClusteringData.Rows.Count > 0)
                {
                    Debug.WriteLine("메모리에 캐싱된 클러스터링 데이터 사용");
                    clusteringData = DataHandler.finalClusteringData;
                }
                else
                {
                    // MongoDB에서 클러스터링 데이터 가져오기
                    Debug.WriteLine("MongoDB에서 클러스터링 데이터 로드");
                    var clusteringRepo = new ClusteringRepository();
                    clusteringData = await clusteringRepo.ToDataTableAsync();

                    // 데이터를 메모리에 캐싱
                    DataHandler.finalClusteringData = clusteringData.Copy();
                }

                // 클러스터 ID와 이름 매핑 생성
                Dictionary<int, string> clusterNameMap = new Dictionary<int, string>();
                foreach (DataRow row in clusteringData.Rows)
                {
                    if (row["ID"] != DBNull.Value && row["ClusterID"] != DBNull.Value)
                    {
                        int id = Convert.ToInt32(row["ID"]);
                        int clusterId = Convert.ToInt32(row["ClusterID"]);

                        if (id == clusterId) // ID와 ClusterID가 일치하는 경우만
                        {
                            string clusterName = row["클러스터명"]?.ToString();
                            if (!string.IsNullOrEmpty(clusterName))
                            {
                                clusterNameMap[id] = clusterName;
                            }
                        }
                    }
                }

                // 클러스터 ID와 dataIndex 매핑 생성
                Dictionary<int, HashSet<string>> clusterDataIndices = new Dictionary<int, HashSet<string>>();
                foreach (DataRow row in clusteringData.Rows)
                {
                    if (row["ClusterID"] != DBNull.Value)
                    {
                        int clusterId = Convert.ToInt32(row["ClusterID"]);
                        string dataIndices = row["dataIndex"]?.ToString();

                        if (!string.IsNullOrEmpty(dataIndices))
                        {
                            if (!clusterDataIndices.ContainsKey(clusterId))
                            {
                                clusterDataIndices[clusterId] = new HashSet<string>();
                            }

                            foreach (string index in dataIndices.Split(',').Select(s => s.Trim()))
                            {
                                if (!string.IsNullOrEmpty(index))
                                {
                                    clusterDataIndices[clusterId].Add(index);
                                }
                            }
                        }
                    }
                }

                // dataTable의 각 행에 클러스터명 설정
                foreach (DataRow row in dataTable.Rows)
                {
                    if (row["raw_data_id"] != DBNull.Value)
                    {
                        string rawDataId = row["raw_data_id"].ToString();

                        // 각 클러스터 ID에 대해 확인
                        foreach (var entry in clusterDataIndices)
                        {
                            int clusterId = entry.Key;
                            var dataIndices = entry.Value;

                            if (dataIndices.Contains(rawDataId) && clusterNameMap.ContainsKey(clusterId))
                            {
                                row["클러스터명"] = clusterNameMap[clusterId];
                                break;
                            }
                        }
                    }
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터링 정보 추가 중 오류: {ex.Message}");
                return dataTable; // 오류 시 원본 데이터 반환
            }
        }

        // DataGridView 설정 및 구성
        // DataGridView 설정 함수 개선 (컬럼 가시성 유지)
        public void ConfigureDataGridView(DataTable dataTable, DataGridView dataGridView)
        {
            if (dataTable == null)
            {
                Debug.WriteLine("ConfigureDataGridView: dataTable이 null입니다.");
                return;
            }

            Debug.WriteLine($"ConfigureDataGridView: 시작 (컬럼 수: {dataTable.Columns.Count})");

            // 현재 컬럼 가시성 상태 저장
            Dictionary<string, bool> columnVisibility = new Dictionary<string, bool>();
            if (dataGridView.Columns.Count > 0)
            {
                foreach (DataGridViewColumn column in dataGridView.Columns)
                {
                    columnVisibility[column.Name] = column.Visible;
                }
            }

            // DataGridView의 DataSource를 DataTable로 설정
            dataGridView.DataSource = dataTable;

            // 필수 시스템 컬럼 숨김 처리
            string[] hiddenColumns = { "id", "import_date", "is_hidden" };
            foreach (string colName in hiddenColumns)
            {
                if (dataGridView.Columns.Contains(colName))
                {
                    dataGridView.Columns[colName].Visible = false;
                }
            }

            // 이전에 저장한 컬럼 가시성 상태 복원
            foreach (var pair in columnVisibility)
            {
                if (dataGridView.Columns.Contains(pair.Key))
                {
                    dataGridView.Columns[pair.Key].Visible = pair.Value;
                }
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

                // 숨겨진 행이면 회색 스타일 적용
                if (isHidden)
                {
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.LightGray;
                    row.DefaultCellStyle.ForeColor = System.Drawing.Color.DarkGray;
                }
            }

            Debug.WriteLine($"ConfigureDataGridView: 완료 (컬럼 수: {dataGridView.Columns.Count})");
        }

        // 페이지 크기 변경
        private async void cmb_pageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmb_pageSize.SelectedItem != null)
            {
                pageSize = Convert.ToInt32(cmb_pageSize.SelectedItem);
                currentPage = 1; // 페이지 크기 변경 시 첫 페이지로
                await LoadPagedDataAsync();
            }
        }

        // 컬럼 목록을 그리드에 추가하는 함수 개선
        // 컬럼 목록을 그리드에 추가하는 함수 개선 - 직접 호출 방식
        public async Task AddSelectedColumnToGridAsync(DataGridView targetDgv, DataGridView sourceDgv)
        {
            Debug.WriteLine($"AddSelectedColumnToGrid 시작: targetDgv={targetDgv.Name}, sourceDgv={sourceDgv.Name}");

            // 모든 경우에 컬럼 초기화 (기존 내용 클리어)
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
                Name = "Data",  // 고정된 컬럼명 사용
                HeaderText = "컬럼명"
            };
            targetDgv.Columns.Add(textColumn);

            // GridView 설정
            targetDgv.AllowUserToAddRows = false;
            targetDgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            targetDgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            targetDgv.Columns["Data"].ReadOnly = true;  // 데이터 컬럼은 읽기 전용
            targetDgv.Columns["CheckBox"].ReadOnly = false;  // 체크박스 컬럼만 편집 가능
            targetDgv.Font = new System.Drawing.Font("맑은 고딕", 14.25F);

            // 필수 컬럼 목록 가져오기
            HashSet<string> essentialColumns = GetEssentialColumns();

            // 시스템 컬럼 정의
            HashSet<string> systemColumns = new HashSet<string>
    {
        "id", "import_date", "is_hidden", "raw_data_id",
        "processed_date", "cluster_id", "cluster_name"
    };

            Debug.WriteLine($"필수 컬럼: {string.Join(", ", essentialColumns)}");

            // 소스 DataGridView에서 컬럼 목록 추출
            // 컬럼 목록과 가시성 상태를 저장할 리스트
            List<(string Name, bool Visible)> columnList = new List<(string Name, bool Visible)>();

            // 먼저 컬럼 정보 수집
            foreach (DataGridViewColumn sourceColumn in sourceDgv.Columns)
            {
                string columnName = sourceColumn.Name;
                bool isVisible = sourceColumn.Visible;

                // 시스템 컬럼이나 필수 컬럼은 제외
                if (systemColumns.Contains(columnName) || essentialColumns.Contains(columnName))
                {
                    continue;
                }

                // 컬럼 정보 저장
                columnList.Add((columnName, isVisible));
            }

            Debug.WriteLine($"추가할 컬럼 수: {columnList.Count}");

            // 컬럼 정보가 없는 경우 - 더 안전한 접근 방식 사용
            if (columnList.Count == 0)
            {
                try
                {
                    Debug.WriteLine("컬럼 정보가 없어 MongoDB에서 조회합니다.");

                    // 비동기로 MongoDB에서 컬럼 정보 조회 (안전하게 처리)
                    var columnMappingRepo = new ColumnMappingRepository();
                    var allColumns = await columnMappingRepo.GetVisibleColumnsAsync();

                    // 컬럼 정보 추가
                    foreach (var column in allColumns)
                    {
                        // 필수 컬럼이나 시스템 컬럼 제외
                        if (essentialColumns.Contains(column.OriginalName) ||
                            systemColumns.Contains(column.OriginalName))
                        {
                            continue;
                        }

                        // 컬럼 정보 추가 - 가시성은 true로 설정 (GetVisibleColumnsAsync에서 이미 필터링됨)
                        columnList.Add((column.OriginalName, true));
                    }

                    Debug.WriteLine($"MongoDB에서 불러온 컬럼 수: {columnList.Count}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"MongoDB에서 컬럼 정보 조회 중 오류: {ex.Message}");

                    // 오류 발생 시 기본 컬럼 사용 (안전망)
                    columnList.Add(("연도", true));
                    columnList.Add(("월", true));
                    columnList.Add(("회사 코드", true));
                }
            }

            // 수집된 컬럼 정보로 행 추가
            foreach (var column in columnList)
            {
                int rowIndex = targetDgv.Rows.Add();
                targetDgv.Rows[rowIndex].Cells["CheckBox"].Value = column.Visible;
                targetDgv.Rows[rowIndex].Cells["Data"].Value = column.Name;

                Debug.WriteLine($"컬럼 추가: {column.Name}, Visible: {column.Visible}");
            }

            Debug.WriteLine($"AddSelectedColumnToGrid 완료: {targetDgv.Rows.Count}개 행 추가됨");
        }



        //2025.04.28
        //상세 항목을 불러오는게 어려운 상황이라 우선 구현 skip
        private void dataGridView_classify_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            
            // 헤더 행 클릭시 무시
            if (e.RowIndex < 0)
                return;

            // "클러스터명" 컬럼 클릭 시 이벤트 무시
            if (e.ColumnIndex >= 0 && dataGridView_classify.Columns[e.ColumnIndex].Name == "클러스터명")
                return;
            
            /*
            // 선택된 행에서 ClusterID 또는 ID 값 가져오기
            DataGridViewRow selectedRow = dataGridView_classify.Rows[e.RowIndex];

            // ClusterID 컬럼이 있으면 사용, 없으면 ID 컬럼 사용
            int selectedClusterId;
            string selectedClusterName;
            if (selectedRow.Cells["ClusterID"] != null && selectedRow.Cells["ClusterID"].Value != null)
            {
                selectedClusterId = Convert.ToInt32(selectedRow.Cells["ClusterID"].Value);
                selectedClusterName = selectedRow.Cells["클러스터명"].Value.ToString();
            }
            else if (selectedRow.Cells["ID"] != null && selectedRow.Cells["ID"].Value != null)
            {
                selectedClusterId = Convert.ToInt32(selectedRow.Cells["ID"].Value);
                selectedClusterName = selectedRow.Cells["클러스터명"].Value.ToString();
            }
            else
            {
                // ID, ClusterID 모두 없는 경우 예외 처리
                MessageBox.Show("클러스터 ID를 찾을 수 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 팝업 폼 생성 및 표시
            ShowClusterDetailPopup(selectedClusterId , selectedClusterName);
            */
        }

        private void ShowClusterDetailPopup(int selectedClusterId ,string selectedClusterName)
        {
            // 팝업용 Form 생성
            Form popupForm = new Form
            {
                Text = $"클러스터 상세 내역 (클러스터명: {selectedClusterName})",
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(1800, 1000),
                MinimizeBox = false,
                MaximizeBox = true,
                FormBorderStyle = FormBorderStyle.Sizable
            };

            // DataGridView 생성
            DataGridView detailGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                Font = new System.Drawing.Font("맑은 고딕", 9F)
            };

            // DataGridView 초기화
            detailGridView.Rows.Clear();
            detailGridView.Columns.Clear();

          

            // 원본 DataTable의 컬럼들 추가
            foreach (DataColumn col in DataHandler.finalClusteringData.Columns)
            {
                detailGridView.Columns.Add(col.ColumnName, col.ColumnName);
            }

            // 데이터 필터링 및 추가
            foreach (DataRow row in DataHandler.finalClusteringData.Rows)
            {
                if (!row.IsNull("ClusterID") && Convert.ToInt32(row["ClusterID"]) == selectedClusterId)
                {
                    int rowIndex = detailGridView.Rows.Add();
                    
                    for (int i = 0; i < DataHandler.finalClusteringData.Columns.Count; i++)
                    {
                        // 합산금액 컬럼은 포맷 적용
                        if ("합산금액".Equals(DataHandler.finalClusteringData.Columns[i].ColumnName))
                        {
                            //detailGridView.Rows[rowIndex].Cells[i].Value = FormatToKoreanUnit(Convert.ToDecimal(row[i]));
                            detailGridView.Rows[rowIndex].Cells[i].Value = Convert.ToDecimal(row[i]);
                        }
                        else
                        {
                            detailGridView.Rows[rowIndex].Cells[i].Value = row[i];
                        }
                    }
                }
            }

            // 필요한 컬럼 숨기기
            if (detailGridView.Columns["ID"] != null)
                detailGridView.Columns["ID"].Visible = false;

            if (detailGridView.Columns["ClusterID"] != null)
                detailGridView.Columns["ClusterID"].Visible = false;

            if (detailGridView.Columns["dataIndex"] != null)
                detailGridView.Columns["dataIndex"].Visible = false;

            if (detailGridView.Columns["import_date"] != null)
                detailGridView.Columns["import_date"].Visible = false;

            // Count 컬럼 포맷 설정
            if (detailGridView.Columns["Count"] != null)
            {
                detailGridView.Columns["Count"].DefaultCellStyle.Format = "N0";
                detailGridView.Columns["Count"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            // 기타 DataGridView 속성 설정
            detailGridView.AllowUserToAddRows = false;
            detailGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            detailGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            detailGridView.ReadOnly = false;

            // 나머지 컬럼들은 읽기 전용으로 설정
            for (int i = 1; i < detailGridView.Columns.Count; i++)
            {
                detailGridView.Columns[i].ReadOnly = true;
            }

            // 클러스터명 컬럼 설정
            if (detailGridView.Columns["클러스터명"] != null)
            {
                detailGridView.Columns["클러스터명"].ReadOnly = true;
                detailGridView.Columns["클러스터명"].Width = 400;
                detailGridView.Columns["클러스터명"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            }

            // 타겟 컬럼 너비 고정
            if (DataHandler.levelName.Count > 0)
            {
                string lastLevelName = DataHandler.levelName[DataHandler.levelName.Count - 1];
                if (detailGridView.Columns[lastLevelName] != null)
                {
                    detailGridView.Columns[lastLevelName].Width = 400;
                    detailGridView.Columns[lastLevelName].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                }
            }

            // 나머지 컬럼은 자동 크기 조정
            for (int i = 1; i < detailGridView.Columns.Count; i++)
            {
                string colName = detailGridView.Columns[i].Name;
                if (colName != "클러스터명" &&
                    colName != DataHandler.prod_col_name &&
                    (DataHandler.levelName.Count == 0 || colName != DataHandler.levelName[DataHandler.levelName.Count - 1]))
                {
                    detailGridView.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                }
            }

            // SortCompare 이벤트 핸들러 추가
            detailGridView.SortCompare -= DataHandler.money_SortCompare;
            detailGridView.SortCompare += DataHandler.money_SortCompare;

            // 컬럼 순서 재배치
            List<string> desiredOrder = new List<string>
                {
                    "Count",
                    DataHandler.sub_acc_col_name,
                    DataHandler.levelName[DataHandler.levelName.Count - 1],
                    DataHandler.prod_col_name,
                    DataHandler.dept_col_name,
                    "합산금액"
                };

            // 기존 컬럼 위치 저장
            Dictionary<string, int> originalIndices = new Dictionary<string, int>();
            for (int i = 0; i < detailGridView.Columns.Count; i++)
            {
                originalIndices[detailGridView.Columns[i].Name] = i;
            }

            // 새로운 컬럼 순서 설정
            for (int i = 0; i < desiredOrder.Count; i++)
            {
                string colName = desiredOrder[i];
                if (detailGridView.Columns.Contains(colName))
                {
                    detailGridView.Columns[colName].DisplayIndex = i;
                }
            }

            // 나머지 컬럼들은 안전하게 순서 설정
            var remainingColumns = detailGridView.Columns.Cast<DataGridViewColumn>()
                .Where(col => !desiredOrder.Contains(col.Name))
                .ToList();

            int nextIndex = desiredOrder.Count;
            foreach (var col in remainingColumns)
            {
                try
                {
                    col.DisplayIndex = nextIndex++;
                }
                catch (ArgumentOutOfRangeException)
                {
                    // 오류 발생 시 최대 허용 인덱스로 설정
                    col.DisplayIndex = detailGridView.Columns.Count - 1;
                }
            }

            // 팝업 폼에 DataGridView 추가 및 표시
            popupForm.Controls.Add(detailGridView);
            popupForm.ShowDialog();
        }
    }
}