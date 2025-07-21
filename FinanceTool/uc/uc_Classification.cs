using DocumentFormat.OpenXml.Wordprocessing;
using FinanceTool.MongoModels;
using FinanceTool.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
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

        // *** 1. 캐시 관련 변수들 모두 삭제 (클래스 상단에서 제거할 것들) ***
        // private static Dictionary<string, string> _cachedClusterNameMapping = null;
        // private static DateTime _cacheLastUpdated = DateTime.MinValue;
        // private static readonly TimeSpan CacheValidDuration = TimeSpan.FromMinutes(5);

        // 컨텍스트 메뉴 초기화 (initUI에서 호출)
        private void InitializeContextMenu()
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            // 세부 클러스터링 메뉴 항목 추가
            ToolStripMenuItem detailClusteringItem = new ToolStripMenuItem("세부 클러스터링 수행");
            detailClusteringItem.Click += DetailClustering_Click;

            contextMenu.Items.Add(detailClusteringItem);
            dataGridView_classify.ContextMenuStrip = contextMenu;
        }

        // 세부 클러스터링 메뉴 클릭 이벤트
        private void DetailClustering_Click(object sender, EventArgs e)
        {
            try
            {
                // 현재 선택된 행 확인
                if (dataGridView_classify.CurrentRow == null)
                {
                    MessageBox.Show("세부 클러스터링을 수행할 클러스터를 선택해주세요.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 선택된 클러스터 ID 가져오기
                var selectedRow = dataGridView_classify.CurrentRow;
                if (selectedRow.Cells["ID"]?.Value == null)
                {
                    MessageBox.Show("올바른 클러스터가 선택되지 않았습니다.", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int selectedClusterId = Convert.ToInt32(selectedRow.Cells["ID"].Value);
                string clusterName = selectedRow.Cells["클러스터명"]?.Value?.ToString() ?? "";

                Debug.WriteLine($"세부 클러스터링 진입: 클러스터 ID {selectedClusterId}, 이름: {clusterName}");

                // 확인 메시지
                DialogResult result = MessageBox.Show(
                    $"'{clusterName}' 클러스터의 세부 클러스터링을 수행하시겠습니까?",
                    "세부 클러스터링 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // 세부 클러스터링 화면으로 이동
                    NavigateToDetailClustering(selectedClusterId, clusterName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세부 클러스터링 진입 오류: {ex.Message}");
                MessageBox.Show($"세부 클러스터링 진입 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 세부 클러스터링 화면으로 이동
        private void NavigateToDetailClustering(int parentClusterId, string parentClusterName)
        {
            try
            {
                // *** 컬럼 정보 전체 출력 ***
                Debug.WriteLine($"[CreateCheckDataGridView] DataHandler.finalClusteringData 총 행 수: {DataHandler.finalClusteringData.Rows.Count}");
                Debug.WriteLine($"[CreateCheckDataGridView] DataHandler.finalClusteringData 총 컬럼 수: {DataHandler.finalClusteringData.Columns.Count}");
                for (int i = 0; i < DataHandler.finalClusteringData.Columns.Count; i++)
                {
                    Debug.WriteLine($"  컬럼 {i}: Name='{DataHandler.finalClusteringData.Columns[i].ColumnName}'" +
                        $", DataType='{DataHandler.finalClusteringData.Columns[i].DataType}'");
                }

                // 세부 클러스터링 화면 초기화 (부모 클러스터 정보 전달)
                userControlHandler.uc_detailClustering.initUI(parentClusterId, parentClusterName);

                // 화면 전환
                if (this.ParentForm is Form1 form)
                {
                    form.LoadUserControl(userControlHandler.uc_detailClustering);
                }

                Debug.WriteLine($"세부 클러스터링 화면 진입 완료: 부모 클러스터 {parentClusterId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세부 클러스터링 화면 전환 오류: {ex.Message}");
                MessageBox.Show($"세부 클러스터링 화면 전환 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // *** 4. 페이징 초기화 함수 추가 (dataTransform.cs, clustering.cs와 동일한 패턴) ***
        private void InitializePagination()
        {
            try
            {
                // 페이지 크기 콤보박스 초기화
                cmb_pageSize.Items.Clear();
                cmb_pageSize.Items.AddRange(new object[] { 1000, 2000, 5000, 10000});
                cmb_pageSize.SelectedIndex = 0; // 1000을 기본값으로 설정
                pageSize = 1000;

                // 페이지 번호 초기화
                currentPage = 1;
                num_pageNumber.Value = 1;
                num_pageNumber.Minimum = 1;

                // 페이징 컨트롤 초기 상태
                btn_prevPage.Enabled = false;
                btn_nextPage.Enabled = false;

                // 페이징 이벤트 핸들러 연결
                AttachPagingEvents();

                Debug.WriteLine("페이징 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"페이징 초기화 중 오류: {ex.Message}");
            }
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

                    DataHandler.finalClusteringData.AcceptChanges();

                    // *** 페이징 초기화 추가 ***
                    InitializePagination();

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

                    InitializeContextMenu();

                    // *** 컬럼 정보 전체 출력 ***
                    Debug.WriteLine($"[CreateCheckDataGridView] DataHandler.finalClusteringData 총 행 수: {DataHandler.finalClusteringData.Rows.Count}");
                    Debug.WriteLine($"[CreateCheckDataGridView] DataHandler.finalClusteringData 총 컬럼 수: {DataHandler.finalClusteringData.Columns.Count}");
                    /*
                    for (int i = 0; i < DataHandler.finalClusteringData.Columns.Count; i++)
                    {
                        Debug.WriteLine($"  컬럼 {i}: Name='{DataHandler.finalClusteringData.Columns[i].ColumnName}'" +
                            $", DataType='{DataHandler.finalClusteringData.Columns[i].DataType}'");
                    }
                    */
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

            if (!enhancedTable.Columns.Contains("ClusterSubID"))
                enhancedTable.Columns.Add("ClusterSubID");

            enhancedTable.Columns.Add("세부클러스터명");

            // *** 수정: 컬럼 순서 조정 ***
            // 세부클러스터명 컬럼을 클러스터명 다음에 배치
            int clusterNameIndex = enhancedTable.Columns["클러스터명"].Ordinal;
            enhancedTable.Columns["세부클러스터명"].SetOrdinal(clusterNameIndex + 1);

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
                    if (row.IsNull("ClusterSubID"))
                    {
                        row["ClusterSubID"] = -1;
                    }
                   
                }
            }

            // *** 여기에 클러스터명과 세부클러스터명 설정 로직을 한 번만 실행 ***
            foreach (DataRow row in enhancedTable.Rows)
            {
                int clusterId = !row.IsNull("ClusterID") ? Convert.ToInt32(row["ClusterID"]) : -1;
                int clusterSubId = !row.IsNull("ClusterSubID") ? Convert.ToInt32(row["ClusterSubID"]) : -1;
                int id = Convert.ToInt32(row["ID"]);
                string originalClusterName = row["클러스터명"]?.ToString() ?? "";

                // 클러스터명과 세부클러스터명 설정
                if (clusterSubId == id && clusterSubId > 0)
                {
                    // 세부 상위 클러스터인 경우
                    // 부모 클러스터명 찾기
                    var parentCluster = enhancedTable.AsEnumerable()
                        .FirstOrDefault(r => Convert.ToInt32(r["ID"]) == clusterId);

                    row["클러스터명"] = parentCluster?["클러스터명"]?.ToString() ?? originalClusterName;
                    row["세부클러스터명"] = originalClusterName;
                }
                else
                {
                    // 일반 병합 클러스터인 경우
                    row["클러스터명"] = originalClusterName;
                    row["세부클러스터명"] = "";
                }
            }


            // CreateEnhancedClusteringDataAsync 함수 마지막에
            // 커스텀 정렬: 병합 클러스터 다음에 세부 클러스터들이 오도록
            var sortedRows = enhancedTable.AsEnumerable()
                .OrderBy(row =>
                {
                    int clusterId = Convert.ToInt32(row["ClusterID"]);
                    int clusterSubId = row["ClusterSubID"] != DBNull.Value ? Convert.ToInt32(row["ClusterSubID"]) : -1;
                    int id = Convert.ToInt32(row["ID"]);

                    // 정렬 키: "부모클러스터ID_세부여부_ID"
                    if (clusterSubId == id && clusterSubId > 0)
                    {
                        // 세부 클러스터: 부모 ID를 기준으로 하되 세부 표시
                        return $"{clusterId:D10}_1_{id:D10}";
                    }
                    else
                    {
                        // 일반 클러스터: ID를 기준으로 정렬
                        return $"{id:D10}_0_{id:D10}";
                    }
                })
                .ToList();

            // 정렬된 결과로 새 테이블 생성
            DataTable sortedTable = enhancedTable.Clone();
            foreach (var row in sortedRows)
            {
                sortedTable.ImportRow(row);
            }

            //return enhancedTable;
            return sortedTable;
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
                dgv.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Pretendard", 9.0f, FontStyle.Bold);

                // 셀 폰트 설정
                dgv.DefaultCellStyle.Font = new System.Drawing.Font("Pretendard", 9.0f);
            }
        }

       

        
       
       
        public void CreateCheckDataGridView(DataGridView dgv, DataTable dt)
        {
            // 조건에 맞는 데이터만 필터링
            var filteredData = dt.AsEnumerable()
                .Where(row =>
                {
                    int clusterId = Convert.ToInt32(row["ClusterID"]);
                    int id = Convert.ToInt32(row["ID"]);
                    int clusterSubId = row["ClusterSubID"] != DBNull.Value ? Convert.ToInt32(row["ClusterSubID"]) : -1;

                    // 일반 클러스터링 결과만 표시 (세부 클러스터링 결과 제외)
                    //return clusterSubId == -1 && (clusterId <= 0 || clusterId == id);
                    return clusterSubId == id || clusterId == id;
                });
            Debug.WriteLine($"[CreateCheckDataGridView] filteredData 갯수: {filteredData.ToList().Count} ");

            if (filteredData.ToList().Count > 0)
            {
                DataTable filteredTable = filteredData.CopyToDataTable();
                dgv.DataSource = filteredTable;

                // 컬럼 정보 확인
                Debug.WriteLine($"원본 DataTable 컬럼: {string.Join(", ", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");
                Debug.WriteLine($"DataGridView 컬럼: {string.Join(", ", dgv.Columns.Cast<DataGridViewColumn>().Select(c => c.Name))}");


                // ID 컬럼 숨기기
                if (dgv.Columns.Contains("ID"))
                {
                    dgv.Columns["ID"].Visible = false;
                }

                // ID 컬럼 숨기기
                if (dgv.Columns.Contains("ClusterSubID"))
                {
                    dgv.Columns["ClusterSubID"].Visible = false;
                }

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
                                                        //dgv.Font = new System.Drawing.Font("Pretendard", 14.25F);
                dgv.Font = new System.Drawing.Font("Pretendard", 9F);
                // "클러스터명" 컬럼의 배경색을 연노란색으로 설정
                dgv.Columns["클러스터명"].DefaultCellStyle.BackColor = System.Drawing.Color.LightYellow;
            }
            else
            {
                Debug.WriteLine("[CreateCheckDataGridView] 조회 조건 데이터가 없음");
            }
            
        }



        public DataTable ConvertDataGridViewToCustomDataTable(DataGridView dgv)
        {
            try
            {
                // 새 DataTable 생성
                DataTable result = new DataTable();

                // 제외할 컬럼들 (컬럼명 기반)
                HashSet<string> excludedColumns = new HashSet<string>
                    {
                        "ID", "ClusterID", "ClusterSubID", "dataIndex"
                    };

                // Decimal 타입으로 변환할 컬럼들
                HashSet<string> decimalColumns = new HashSet<string>
                    {
                        "Count", "합산금액"
                    };

                // 32767자 제한을 적용할 컬럼들 (긴 텍스트 컬럼)
                HashSet<string> textLimitColumns = new HashSet<string>
                    {
                        "세부클러스터명", "키워드목록"
                    };

                // 포함할 컬럼들 수집 및 DataTable에 추가
                List<string> includedColumns = new List<string>();

                foreach (DataGridViewColumn column in dgv.Columns)
                {
                    string columnName = column.HeaderText; // HeaderText 사용

                    // 제외 컬럼이 아닌 경우에만 추가
                    if (!excludedColumns.Contains(columnName))
                    {
                        includedColumns.Add(columnName);

                        // 컬럼 타입 결정
                        Type columnType = typeof(string); // 기본값
                        if (decimalColumns.Contains(columnName))
                        {
                            columnType = typeof(decimal);
                        }

                        result.Columns.Add(columnName, columnType);
                    }
                }

                // 행 데이터 추가
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (!row.IsNewRow)
                    {
                        DataRow newRow = result.NewRow();

                        for (int i = 0; i < includedColumns.Count; i++)
                        {
                            string columnName = includedColumns[i];

                            // 원본 DataGridView에서 해당 컬럼 찾기
                            DataGridViewColumn sourceColumn = dgv.Columns.Cast<DataGridViewColumn>()
                                .FirstOrDefault(c => c.HeaderText == columnName);

                            if (sourceColumn != null)
                            {
                                object cellValue = row.Cells[sourceColumn.Index].Value;

                                // 32767자 제한 적용 (긴 텍스트 컬럼)
                                if (textLimitColumns.Contains(columnName))
                                {
                                    if (cellValue != null && cellValue != DBNull.Value)
                                    {
                                        string strValue = cellValue.ToString();
                                        if (strValue.Length > 32767)
                                        {
                                            cellValue = strValue.Substring(0, 32760) + "...";
                                        }
                                    }
                                }

                                // Decimal 컬럼 처리
                                if (decimalColumns.Contains(columnName))
                                {
                                    if (cellValue != null && cellValue != DBNull.Value)
                                    {
                                        if (decimal.TryParse(cellValue.ToString().Replace(",", ""), out decimal decValue))
                                        {
                                            newRow[i] = decValue;
                                        }
                                        else
                                        {
                                            newRow[i] = 0m;
                                        }
                                    }
                                    else
                                    {
                                        newRow[i] = 0m;
                                    }
                                }
                                else
                                {
                                    newRow[i] = cellValue ?? DBNull.Value;
                                }
                            }
                            else
                            {
                                // 컬럼을 찾지 못한 경우 기본값
                                if (decimalColumns.Contains(columnName))
                                {
                                    newRow[i] = 0m;
                                }
                                else
                                {
                                    newRow[i] = DBNull.Value;
                                }
                            }
                        }

                        result.Rows.Add(newRow);
                    }
                }

                Debug.WriteLine($"ConvertDataGridViewToCustomDataTable 완료: {result.Rows.Count}행, {result.Columns.Count}컬럼");
                Debug.WriteLine($"포함된 컬럼: {string.Join(", ", includedColumns)}");

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
        public async Task<string> ExportToExcelAsync(List<string> columnList, bool hiddenTableYN = false)
        {
            string savedFilePath = null;
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
                            int batchSize = 10000;
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
                    // DataHandler.SaveDataTableToExcel 메서드를 수정하여 저장 경로 반환
                    savedFilePath = DataHandler.SaveDataTableToExcel(cluster_result, export_result);

                    await progress.UpdateProgressHandler(100, "Excel 파일 저장 완료");
                    await Task.Delay(500); // 완료 메시지 표시
                }

                // 저장 완료 메시지
                /*
                MessageBox.Show("Excel 파일로 내보내기가 완료되었습니다.", "내보내기 완료",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                */
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excel 파일 저장 중 오류 발생: {ex.Message}");
                MessageBox.Show($"Excel 파일 저장 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return savedFilePath;
            }
            return savedFilePath;
        }

        /// <summary>
        /// MongoDB 문서를 확장된 DataTable로 변환하는 메서드 (컬럼 필터링 포함)
        /// </summary>
        private DataTable ConvertRawDocumentsToEnhancedDataTable(List<RawDataDocument> documents, List<string> columnList)
        {
            DataTable dataTable = new DataTable();



            // 기본 컬럼 추가
            //dataTable.Columns.Add("id", typeof(string));
            //dataTable.Columns.Add("import_date", typeof(DateTime));

            // 클러스터명 컬럼 추가 (없을 경우)
            if (!dataTable.Columns.Contains("클러스터명"))
            {
                dataTable.Columns.Add("클러스터명", typeof(string));
            }

            // 세부클러스터명 컬럼 추가
            if (!dataTable.Columns.Contains("세부클러스터명"))
            {
                dataTable.Columns.Add("세부클러스터명", typeof(string));
            }

            // columnList에 명시된 컬럼만 추가
            foreach (string columnName in columnList)
            {
                Debug.WriteLine($" 표기하는 컬럼 정보 : {columnName}");
                if (!dataTable.Columns.Contains(columnName))
                {
                    dataTable.Columns.Add(columnName);
                }
            }

            

            // 문서 데이터를 DataTable에 추가
            foreach (var doc in documents)
            {
                DataRow row = dataTable.NewRow();
                //row["id"] = doc.Id;
                //row["import_date"] = doc.ImportDate;

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
                row["세부클러스터명"] = "";

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

                // 클러스터 ID별 이름 매핑 생성 (일반 클러스터)
                Dictionary<int, string> clusterNameMap = new Dictionary<int, string>();
                foreach (var cluster in allClusters.Where(c => c.ClusterId == c.ClusterNumber))
                {
                    clusterNameMap[cluster.ClusterNumber] = cluster.ClusterName;
                }

                // 세부클러스터 ID별 이름 매핑 생성 (세부 클러스터)
                Dictionary<int, string> detailClusterNameMap = new Dictionary<int, string>();
                foreach (var cluster in allClusters.Where(c => c.ClusterSubId == c.ClusterNumber && c.ClusterSubId > 0))
                {
                    detailClusterNameMap[cluster.ClusterNumber] = cluster.ClusterName;
                }

                // 문서 ID별 클러스터 매핑 생성
                Dictionary<string, int> docIdToClusterMap = new Dictionary<string, int>();
                Dictionary<string, int> docIdToDetailClusterMap = new Dictionary<string, int>();

                foreach (var cluster in allClusters)
                {
                    if (cluster.DataIndices != null)
                    {
                        foreach (var docId in cluster.DataIndices)
                        {
                            // 일반 클러스터 매핑
                            if (!docIdToClusterMap.ContainsKey(docId))
                            {
                                int topClusterId = cluster.ClusterId > 0 ? cluster.ClusterId : cluster.ClusterNumber;
                                docIdToClusterMap[docId] = topClusterId;
                            }

                            // 세부 클러스터 매핑 (세부 클러스터가 있는 경우)
                            if (cluster.ClusterSubId > 0 && cluster.ClusterSubId == cluster.ClusterNumber)
                            {
                                docIdToDetailClusterMap[docId] = cluster.ClusterNumber;
                            }
                        }
                    }
                }
                var rawDataRepo = new RawDataRepository();
                // exportData의 각 행에 클러스터 정보 추가
                // 현재 사용 중인 documents의 ID를 추적하기 위한 변수
                var documents = await rawDataRepo.GetAllAsync(); // 모든 문서 가져오기
                var docIdLookup = documents.ToDictionary(d => d.Id, d => d.Id);

                for (int i = 0; i < exportData.Rows.Count; i++)
                {
                    var row = exportData.Rows[i];

                    // documents의 인덱스를 사용해서 ID 매핑
                    if (i < documents.Count)
                    {
                        string docId = documents[i].Id;

                        // 클러스터명 매핑
                        if (docIdToClusterMap.TryGetValue(docId, out int clusterId) &&
                            clusterNameMap.TryGetValue(clusterId, out string clusterName))
                        {
                            row["클러스터명"] = clusterName;
                        }

                        // 세부클러스터명 매핑
                        if (docIdToDetailClusterMap.TryGetValue(docId, out int detailClusterId) &&
                            detailClusterNameMap.TryGetValue(detailClusterId, out string detailClusterName))
                        {
                            row["세부클러스터명"] = detailClusterName;
                        }
                    }
                }

                Debug.WriteLine($"클러스터 정보 및 세부클러스터 정보 추가 완료: {exportData.Rows.Count}행");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터 정보 추가 중 오류: {ex.Message}");
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
                try
                {
                    using (var progressForm = new ProcessProgressForm())
                    {
                        progressForm.Show();
                        await progressForm.UpdateProgressHandler(5, "Excel 파일 생성 준비 중...");

                        // 1. Excel 파일 생성
                        await progressForm.UpdateProgressHandler(10, "데이터 내보내기 시작...");
                        string userSavedPath = await ExportToExcelAsync(process_col_list, DataHandler.hiddenData);

                        if (!string.IsNullOrEmpty(userSavedPath))
                        {
                            await progressForm.UpdateProgressHandler(60, "Excel 파일 생성 완료");

                            // 2. 서버 백업 경로에 파일 복사
                            await progressForm.UpdateProgressHandler(65, "서버 백업 생성 중...");
                            string backupPath = await CreateServerBackupAsync(userSavedPath);

                            if (!string.IsNullOrEmpty(backupPath))
                            {
                                await progressForm.UpdateProgressHandler(85, "서버 백업 완료");

                                // 3. 세션 정보 업데이트
                                await progressForm.UpdateProgressHandler(90, "세션 정보 업데이트 중...");
                                bool updateResult = await UpdateSessionCompletionAsync(backupPath);

                                if (updateResult)
                                {
                                    await progressForm.UpdateProgressHandler(95, "세션 정보 업데이트 완료");

                                    // 4. 현재 세션 ID 초기화 (선택사항)
                                    await progressForm.UpdateProgressHandler(98, "정리 작업 중...");
                                    // DataHandler.ClearCurrentSessionId(); // 필요시 활성화

                                    await progressForm.UpdateProgressHandler(100, "모든 작업 완료");
                                    await Task.Delay(500);

                                    MessageBox.Show(
                                        "Excel 파일 생성, 서버 백업 및 세션 상태 업데이트가 모두 완료되었습니다.\n\n",
                                        "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                else
                                {
                                    await progressForm.UpdateProgressHandler(95, "세션 정보 업데이트 실패");
                                    await Task.Delay(300);

                                    MessageBox.Show(
                                        "Excel 파일 생성 및 백업은 완료되었으나, 세션 정보 업데이트에 실패했습니다.\n\n" +
                                        $"사용자 저장 경로: {userSavedPath}\n" +
                                        $"서버 백업 경로: {backupPath}",
                                        "일부 완료", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                            else
                            {
                                await progressForm.UpdateProgressHandler(85, "서버 백업 실패");
                                await Task.Delay(300);

                                MessageBox.Show(
                                    "Excel 파일 생성은 완료되었으나, 서버 백업에 실패했습니다.\n\n" +
                                    $"사용자 저장 경로: {userSavedPath}",
                                    "일부 완료", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                        else
                        {
                            await progressForm.UpdateProgressHandler(60, "Excel 파일 생성 실패");
                            await Task.Delay(300);

                            MessageBox.Show("Excel 파일 생성이 취소되었거나 실패했습니다.", "실패",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Excel 파일 생성 프로세스 중 오류: {ex.Message}");
                    MessageBox.Show($"Excel 파일 생성 중 오류가 발생했습니다:\n{ex.Message}", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 세션 완료 정보 업데이트
        /// </summary>
        private async Task<bool> UpdateSessionCompletionAsync(string resultFilePath)
        {
            try
            {
                ObjectId currentSessionId = DataHandler.GetCurrentSessionId();

                if (currentSessionId == ObjectId.Empty)
                {
                    Debug.WriteLine("현재 세션 ID가 설정되지 않아 세션 업데이트를 건너뜁니다.");
                    return false;
                }

                var fileSessionRepo = new FileSessionRepository();

                // 세션 정보 업데이트
                bool updateResult = await fileSessionRepo.UpdateSessionCompletionAsync(
                    currentSessionId,
                    "completed",
                    DateTime.UtcNow,
                    resultFilePath
                );

                if (updateResult)
                {
                    Debug.WriteLine($"세션 완료 정보 업데이트 성공: {currentSessionId}");
                    Debug.WriteLine($"결과 파일 경로: {resultFilePath}");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"세션 완료 정보 업데이트 실패: {currentSessionId}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 완료 정보 업데이트 중 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 서버 백업 경로에 파일 복사 (개선된 버전)
        /// </summary>
        private async Task<string> CreateServerBackupAsync(string originalFilePath)
        {
            try
            {
                ObjectId currentSessionId = DataHandler.GetCurrentSessionId();

                if (currentSessionId == ObjectId.Empty)
                {
                    Debug.WriteLine("현재 세션 ID가 설정되지 않았습니다.");
                    return null;
                }

                // 원본 파일명 추출
                string originalFileName = Path.GetFileName(originalFilePath);

                // DataHandler를 통해 백업 파일 경로 생성
                string backupPath = DataHandler.GenerateExcelCompletedFilePath(currentSessionId, originalFileName);

                if (string.IsNullOrEmpty(backupPath))
                {
                    Debug.WriteLine("백업 파일 경로 생성에 실패했습니다.");
                    return null;
                }

                // 파일 복사
                await Task.Run(() => File.Copy(originalFilePath, backupPath, true));

                Debug.WriteLine($"서버 백업 완료: {backupPath}");
                return backupPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"서버 백업 중 오류: {ex.Message}");
                return null;
            }
        }


        /// <summary>
        /// 현재 세션 ID 가져오기 (구현 필요)
        /// </summary>
        private ObjectId GetCurrentSessionId()
        {
            return DataHandler.GetCurrentSessionId();
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

            if (e.ColumnIndex == dataGridView_classify.Columns["클러스터명"].Index && e.RowIndex >= 0)
            {
                string newValue = dataGridView_classify.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";

                if ("".Equals(newValue))
                {
                    CreateCheckDataGridView(dataGridView_classify, DataHandler.finalClusteringData);
                    return;
                }

                try
                {
                    using (var progressForm = new ProcessProgressForm())
                    {
                        progressForm.Show();
                        await progressForm.UpdateProgressHandler(10, "클러스터명 변경 시작");

                        // 1. DataHandler.finalClusteringData 업데이트
                        int id = Convert.ToInt32(dataGridView_classify.Rows[e.RowIndex].Cells["ID"].Value);
                        DataRow[] rows = DataHandler.finalClusteringData.Select($"ID = {id}");
                        if (rows.Length > 0)
                        {
                            rows[0]["클러스터명"] = newValue;
                        }

                        await progressForm.UpdateProgressHandler(30, "메모리 데이터 업데이트 완료");

                        // 2. MongoDB 클러스터명 업데이트
                        var clusteringRepo = new ClusteringRepository();
                        bool mongoUpdateResult = await clusteringRepo.UpdateClusterNameAsync(id, newValue);

                        if (!mongoUpdateResult)
                        {
                            Debug.WriteLine($"MongoDB 클러스터명 업데이트 실패: ID={id}, 새 이름={newValue}");
                            // 실패 시 메모리 데이터도 롤백
                            MessageBox.Show("클러스터명 변경에 실패했습니다. 다시 시도해주세요.", "오류",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        await progressForm.UpdateProgressHandler(60, "MongoDB 업데이트 완료");

                        // 3. 변경 사항 저장
                        DataHandler.finalClusteringData.AcceptChanges();

                        await progressForm.UpdateProgressHandler(80, "현재 페이지 데이터 갱신 중");

                        // 4. 현재 페이지 데이터 즉시 갱신
                        await LoadPagedDataAsync();

                        await progressForm.UpdateProgressHandler(100, "클러스터명 변경 완료");
                        await Task.Delay(300);
                        progressForm.Close();

                        Debug.WriteLine($"클러스터명 변경 완료: ID={id}, 새 이름={newValue}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"클러스터명 변경 중 오류: {ex.Message}");
                    MessageBox.Show($"클러스터명 변경 중 오류가 발생했습니다: {ex.Message}", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
                            column.Name.Equals(DataHandler.sub_acc_col_name) ||
                            column.Name.Equals("세부클러스터명"))  // *** 추가 ***
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
        // *** 2. 개선된 PerformLoadPagedData 함수 (캐시 제거 + 간단한 실시간 조회) ***
        private async Task PerformLoadPagedData(ProcessProgressForm.UpdateProgressDelegate progressHandler = null)
        {
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

                    if (progressHandler != null)
                    {
                        await progressHandler(50, "Raw 데이터 로드 완료");
                    }

                    // 3. 현재 페이지 데이터의 raw_data ID 목록 추출
                    var currentPageRawDataIds = documents.Select(d => d.Id).ToList();

                    // 4. 현재 페이지 데이터에 대해서만 클러스터명 매핑 조회
                    var clusterNameMapping = await GetClusterNameMappingForPageAsync(currentPageRawDataIds);
                    Debug.WriteLine($"현재 페이지 클러스터 매핑: {clusterNameMapping.Count}개 항목");

                    // *** 추가: 세부클러스터명 매핑도 함께 조회 ***
                    var detailClusterNameMapping = await GetDetailClusterNameMappingForPageAsync(currentPageRawDataIds);
                    Debug.WriteLine($"현재 페이지 세부클러스터 매핑: {detailClusterNameMapping.Count}개 항목");


                    if (progressHandler != null)
                    {
                        await progressHandler(65, "클러스터 매핑 조회 완료");
                    }

                    // 5. MongoDB 문서를 DataTable로 변환 (클러스터명 포함)
                    //pageData = ConvertRawDocumentsToDataTableWithClusterName(documents, clusterNameMapping);
                    pageData = ConvertRawDocumentsToDataTableWithClusterName(documents, clusterNameMapping, detailClusterNameMapping);
                    Debug.WriteLine($"변환된 pageData: {pageData.Rows.Count}행, 클러스터명 매핑: {GetClusterNameMappingStats(pageData)}");

                    if (progressHandler != null)
                    {
                        await progressHandler(70, "데이터 변환 완료");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"데이터 로드 작업 중 오류: {ex.Message}");
                    throw;
                }
            });

            if (progressHandler != null)
            {
                await progressHandler(80, "UI 업데이트 중...");
            }

            // UI 업데이트
            try
            {
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

                    // 클러스터명 컬럼 스타일 적용
                    ApplyClusterNameColumnStyle(dataGridView_keyword);
                }

                await AddSelectedColumnToGridAsync(dataGridView_delete_col2, dataGridView_keyword);
                Debug.WriteLine($"dataGridView_delete_col2 설정 완료 (행 수: {dataGridView_delete_col2.Rows.Count})");

                UpdatePaginationInfo();
                ApplyGridFormatting();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UI 업데이트 중 오류: {ex.Message}\\n{ex.StackTrace}");
            }

            if (progressHandler != null)
            {
                await progressHandler(90, "데이터 로드 마무리 중...");
            }
        }

        // GetClusterNameMappingForPageAsync 함수 다음에 추가
        private async Task<Dictionary<string, string>> GetDetailClusterNameMappingForPageAsync(List<string> rawDataIds)
        {
            var mappingDict = new Dictionary<string, string>();

            if (rawDataIds == null || rawDataIds.Count == 0)
                return mappingDict;

            try
            {
                var clusteringRepo = new ClusteringRepository();

                // cluster_sub_id == cluster_number인 세부 상위 클러스터만 조회
                var filter = Builders<ClusteringResultDocument>.Filter.Where(c =>
                    c.ClusterSubId == c.ClusterNumber && c.ClusterSubId > 0);
                var detailClusters = await clusteringRepo.FindDocumentsAsync(filter);

                // 현재 페이지의 raw_data ID에 대해서만 매핑 생성
                foreach (var cluster in detailClusters)
                {
                    if (cluster.DataIndices != null && !string.IsNullOrEmpty(cluster.ClusterName))
                    {
                        foreach (var dataIndex in cluster.DataIndices)
                        {
                            if (rawDataIds.Contains(dataIndex) && !mappingDict.ContainsKey(dataIndex))
                            {
                                mappingDict[dataIndex] = cluster.ClusterName;
                            }
                        }
                    }
                }

                Debug.WriteLine($"현재 페이지 세부클러스터 매핑 생성: {rawDataIds.Count}개 ID 중 {mappingDict.Count}개 매핑");
                return mappingDict;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세부클러스터명 매핑 조회 중 오류: {ex.Message}");
                return mappingDict;
            }
        }

        // *** 3. 현재 페이지 데이터에 대해서만 클러스터명 매핑 조회 (캐시 제거) ***
        private async Task<Dictionary<string, string>> GetClusterNameMappingForPageAsync(List<string> rawDataIds)
        {
            var mappingDict = new Dictionary<string, string>();

            if (rawDataIds == null || rawDataIds.Count == 0)
                return mappingDict;

            try
            {
                var clusteringRepo = new ClusteringRepository();

                // clustering_results에서 cluster_number == cluster_id인 최종 클러스터만 조회
                var filter = Builders<ClusteringResultDocument>.Filter.Where(c => c.ClusterNumber == c.ClusterId);
                var finalClusters = await clusteringRepo.FindDocumentsAsync(filter);

                // 현재 페이지의 raw_data ID에 대해서만 매핑 생성
                foreach (var cluster in finalClusters)
                {
                    if (cluster.DataIndices != null && !string.IsNullOrEmpty(cluster.ClusterName))
                    {
                        foreach (var dataIndex in cluster.DataIndices)
                        {
                            if (rawDataIds.Contains(dataIndex) && !mappingDict.ContainsKey(dataIndex))
                            {
                                mappingDict[dataIndex] = cluster.ClusterName;
                            }
                        }
                    }
                }

                Debug.WriteLine($"현재 페이지 클러스터 매핑 생성: {rawDataIds.Count}개 ID 중 {mappingDict.Count}개 매핑");
                return mappingDict;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터명 매핑 조회 중 오류: {ex.Message}");
                return mappingDict;
            }
        }
        // 클러스터명 매핑 통계 확인 (새로 추가할 메서드)
        private string GetClusterNameMappingStats(DataTable dataTable)
        {
            if (dataTable == null || !dataTable.Columns.Contains("클러스터명"))
                return "매핑 통계 없음";

            int totalRows = dataTable.Rows.Count;
            int mappedRows = 0;

            foreach (DataRow row in dataTable.Rows)
            {
                if (row["클러스터명"] != null && !string.IsNullOrEmpty(row["클러스터명"].ToString()))
                {
                    mappedRows++;
                }
            }

            return $"{totalRows}행 중 {mappedRows}행 매핑됨 ({(double)mappedRows / totalRows * 100:F1}%)";
        }

        // 클러스터명 컬럼 스타일 적용 (새로 추가할 메서드)
        private void ApplyClusterNameColumnStyle(DataGridView dgv)
        {
            try
            {
                if (dgv.Columns.Contains("클러스터명"))
                {
                    var clusterColumn = dgv.Columns["클러스터명"];

                    // 클러스터명 컬럼 스타일 설정
                    clusterColumn.DefaultCellStyle.BackColor = System.Drawing.Color.LightBlue;
                    clusterColumn.DefaultCellStyle.Font = new System.Drawing.Font("Pretendard", 9.0f, FontStyle.Bold);
                    clusterColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                    // 컬럼 너비 조정
                    clusterColumn.Width = 120;
                    clusterColumn.MinimumWidth = 100;

                    // 항상 표시되도록 설정
                    clusterColumn.Visible = true;

                    Debug.WriteLine("클러스터명 컬럼 스타일 적용 완료");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터명 컬럼 스타일 적용 중 오류: {ex.Message}");
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

            // *** 추가: 세부클러스터명도 필수 컬럼으로 설정 ***
            essentialColumns.Add("세부클러스터명");

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
                        //Debug.WriteLine($"시스템 컬럼 숨김: {columnName}");
                        continue;
                    }

                    // 필수 컬럼은 항상 표시
                    if (essentialColumns.Contains(columnName))
                    {
                        column.Visible = true;
                        //Debug.WriteLine($"필수 컬럼 표시: {columnName}");
                        continue;
                    }

                    // 가시적 컬럼 목록에 있는 컬럼만 표시
                    bool isVisible = visibleColumnNames.Contains(columnName);
                    column.Visible = isVisible;
                    //Debug.WriteLine($"일반 컬럼 가시성 설정: {columnName}, Visible: {isVisible}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"컬럼 가시성 설정 중 오류: {column.Name}, {ex.Message}");
                }
            }

           
        }

        // 필수 컬럼 목록을 가져오는 헬퍼 함수 추가
        private HashSet<string> GetEssentialColumns()
        {
            HashSet<string> essentialColumns = new HashSet<string>();

            // 클러스터명 추가
            essentialColumns.Add("클러스터명");

            // 세부클러스터명 추가
            essentialColumns.Add("세부클러스터명");

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
        // 기존 ConvertRawDocumentsToDataTable 메서드를 아래 메서드로 교체
        private DataTable ConvertRawDocumentsToDataTableWithClusterName(
            List<RawDataDocument> documents,
            Dictionary<string, string> clusterNameMapping,
            Dictionary<string, string> detailClusterNameMapping = null)
        {
            DataTable dataTable = new DataTable();

            // 기본 컬럼 추가
            dataTable.Columns.Add("id", typeof(string));
            dataTable.Columns.Add("import_date", typeof(DateTime));
            dataTable.Columns.Add("is_hidden", typeof(bool));
            dataTable.Columns.Add("클러스터명", typeof(string)); // 클러스터명 컬럼을 먼저 추가
                                                            // *** 여기에 추가 ***
            dataTable.Columns.Add("세부클러스터명", typeof(string)); // 세부클러스터명 컬럼 추가

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

            // 통계 추적
            int mappedCount = 0;
            int totalCount = documents.Count;

            // 문서 데이터를 DataTable에 추가
            foreach (var doc in documents)
            {
                DataRow row = dataTable.NewRow();
                row["id"] = doc.Id;
                row["import_date"] = doc.ImportDate;
                row["is_hidden"] = doc.IsHidden;

                // 클러스터명 매핑
                if (clusterNameMapping != null && clusterNameMapping.TryGetValue(doc.Id, out string clusterName))
                {
                    row["클러스터명"] = clusterName;
                    mappedCount++;
                }
                else
                {
                    row["클러스터명"] = ""; // 매핑되지 않은 경우 빈 문자열
                }
                // *** 여기에 추가 ***
                // 세부클러스터명 설정 로직 (ClusterSubID 기반)
                // *** 추가: 세부클러스터명 매핑 ***
                // 세부클러스터명 매핑
                if (detailClusterNameMapping != null && detailClusterNameMapping.TryGetValue(doc.Id, out string detailClusterName))
                {
                    row["세부클러스터명"] = detailClusterName;
                }
                else
                {
                    row["세부클러스터명"] = ""; // 매핑되지 않은 경우 빈 문자열
                }

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
            

            Debug.WriteLine($"클러스터명이 포함된 DataTable 생성 완료: {dataTable.Rows.Count}행, {mappedCount}개 행에 클러스터명 매핑됨 ({(double)mappedCount / totalCount * 100:F1}%)");
            return dataTable;
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
            targetDgv.Font = new System.Drawing.Font("Pretendard", 14.25F);

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

                //Debug.WriteLine($"컬럼 추가: {column.Name}, Visible: {column.Visible}");
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
         
        }

       
    }
}