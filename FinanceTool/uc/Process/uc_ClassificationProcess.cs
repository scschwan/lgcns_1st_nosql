using FinanceTool.Data;
using FinanceTool.MongoModels;
using FinanceTool.Repositories;
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
    public partial class uc_Classification
    {
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
                    form.LoadUserControl(userControlHandler.uc_detailClustering, form.subClusteringToolStripMenuItem);
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
                cmb_pageSize.Items.AddRange(new object[] { 1000, 2000, 5000, 10000 });
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
        /// MongoDB 문서를 확장된 DataTable로 변환하는 메서드 (컬럼 필터링 포함)
        /// </summary>
        private DataTable ConvertRawDocumentsToEnhancedDataTable(List<RawDataDocument> documents, List<string> columnList)
        {
            DataTable dataTable = new DataTable();



            // 기본 컬럼 추가
            dataTable.Columns.Add("id", typeof(string));
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

            // *** 개선: 금액 컬럼명 가져오기 ***
            string moneyColumnName = null;
            if (DataHandler.levelName != null && DataHandler.levelName.Count > 0)
            {
                moneyColumnName = DataHandler.levelName[0];
                Debug.WriteLine($"금액 컬럼으로 설정: {moneyColumnName}");
            }

            // columnList에 명시된 컬럼만 추가
            foreach (string columnName in columnList)
            {

                //생략 컬럼 추가
                if ("_id".Equals(columnName) || "is_hidden".Equals(columnName))
                {
                    continue;
                }

                Debug.WriteLine($" 표기하는 컬럼 정보 : {columnName}");
                if (!dataTable.Columns.Contains(columnName))
                {
                    //dataTable.Columns.Add(columnName);
                    // *** 개선: 금액 컬럼은 decimal 타입으로 지정 ***
                    if (!string.IsNullOrEmpty(moneyColumnName) && columnName.Equals(moneyColumnName))
                    {
                        dataTable.Columns.Add(columnName, typeof(decimal));
                        Debug.WriteLine($"  → decimal 타입으로 추가: {columnName}");
                    }
                    else
                    {
                        dataTable.Columns.Add(columnName);
                        Debug.WriteLine($"  → string 타입으로 추가: {columnName}");
                    }
                }
            }



            // 문서 데이터를 DataTable에 추가
            foreach (var doc in documents)
            {
                DataRow row = dataTable.NewRow();
                row["id"] = doc.Id;
                //row["import_date"] = doc.ImportDate;

                // 동적 데이터 필드 추가 (columnList에 있는 것만)
                if (doc.Data != null)
                {
                    foreach (var kvp in doc.Data)
                    {
                        if (columnList.Contains(kvp.Key) && dataTable.Columns.Contains(kvp.Key))
                        {
                            // *** 개선: 금액 컬럼은 decimal로 변환 ***
                            if (!string.IsNullOrEmpty(moneyColumnName) && kvp.Key.Equals(moneyColumnName))
                            {
                                // 금액 데이터 변환 처리
                                if (kvp.Value != null)
                                {
                                    decimal moneyValue = 0m;

                                    // 다양한 타입에 대한 변환 처리
                                    if (kvp.Value is decimal decimalValue)
                                    {
                                        moneyValue = decimalValue;
                                    }
                                    else if (kvp.Value is double doubleValue)
                                    {
                                        moneyValue = (decimal)doubleValue;
                                    }
                                    else if (kvp.Value is int intValue)
                                    {
                                        moneyValue = intValue;
                                    }
                                    else if (kvp.Value is long longValue)
                                    {
                                        moneyValue = longValue;
                                    }
                                    else
                                    {
                                        // 문자열인 경우 파싱 시도
                                        string strValue = kvp.Value.ToString();

                                        // 쉼표 제거 및 공백 제거
                                        strValue = strValue.Replace(",", "").Trim();

                                        if (decimal.TryParse(strValue, out decimal parsedValue))
                                        {
                                            moneyValue = parsedValue;
                                        }
                                        else
                                        {
                                            Debug.WriteLine($"금액 변환 실패: {kvp.Key} = {kvp.Value} (타입: {kvp.Value.GetType()})");
                                        }
                                    }

                                    row[kvp.Key] = moneyValue;
                                }
                                else
                                {
                                    row[kvp.Key] = DBNull.Value;
                                }
                            }
                            else
                            {
                                // 일반 컬럼은 기존 방식대로 처리
                                row[kvp.Key] = kvp.Value ?? DBNull.Value;
                            }
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

                    // DataTable의 "ID" 컬럼에서 직접 MongoDB 문서 ID 가져오기
                    string docId = row["id"]?.ToString();

                    if (!string.IsNullOrEmpty(docId))
                    {
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

        /// <summary>
        /// 서버 백업 경로에 파일 복사 (개선된 버전)
        /// </summary>
        private async Task<string> CreateServerBackupAsync(string originalFilePath)
        {
            try
            {
                ObjectId currentSessionId = DataHandler_classification.GetCurrentSessionId();

                if (currentSessionId == ObjectId.Empty)
                {
                    Debug.WriteLine("현재 세션 ID가 설정되지 않았습니다.");
                    return null;
                }

                // 원본 파일명 추출
                string originalFileName = Path.GetFileName(originalFilePath);

                // DataHandler를 통해 백업 파일 경로 생성
                string backupPath = DataHandler_classification.GenerateExcelCompletedFilePath(currentSessionId, originalFileName);

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

    }
}
