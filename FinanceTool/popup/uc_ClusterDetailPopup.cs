using FinanceTool.MongoModels;
using FinanceTool.Repositories;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FinanceTool
{
    public partial class ClusterDetailPopup : Form
    {
        private int _currentClusterId = -1;
        private decimal _decimalDivider = 1;
        private string _decimalDividerName = "원";

        // 이벤트 정의 - 병합 해제가 완료되었을 때 발생
        public event EventHandler<ClusterUnmergeEventArgs> UnmergeCompleted;

        // 이벤트 인자 클래스
        public class ClusterUnmergeEventArgs : EventArgs
        {
            public List<int> UnmergedClusterIds { get; set; }
            public int ParentClusterId { get; set; }
            public bool RefreshRequired { get; set; } = true;
        }

        public ClusterDetailPopup()
        {
            InitializeComponent();

            // DataGridView 초기 설정
            detail_grid_view.AllowUserToAddRows = false;
            detail_grid_view.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            detail_grid_view.Font = new Font("맑은 고딕", 9F);
            detail_grid_view.RowHeadersVisible = false;
            detail_grid_view.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            detail_grid_view.ScrollBars = ScrollBars.Both;

            // 폼 크기 조정 - 더 넓게 설정
            this.Width = 1400; // 폼 너비 증가
            this.Height = 700; // 폼 높이 증가

            // 금액 정렬 이벤트 등록
            detail_grid_view.SortCompare += DataHandler.money_SortCompare;

            // 버튼 클릭 이벤트 등록
            unmerge_selected_btn.Click += UnmergeSelectedItems_Click;
            close_btn.Click += (s, e) => this.Close();
            select_all_btn.Click += SelectAll_Click;

            // 데이터그리드뷰 등록
            DataHandler.RegisterDataGridView(detail_grid_view);

            // 폼 속성 설정
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
        }

        public async Task ShowClusterDetail(int clusterId)
        {
            _currentClusterId = clusterId;

            // 제목 업데이트
            this.Text = $"클러스터 ID {clusterId} 세부 정보";
            detail_title_label.Text = $"클러스터 ID {clusterId} 세부 정보";

            try
            {
                // 1. MongoDB에서 클러스터에 속한 하위 항목 조회
                var clusteringRepo = new ClusteringRepository();
                var childClusters = await clusteringRepo.GetChildClustersAsync(clusterId);

                if (childClusters == null || childClusters.Count == 0)
                {
                    MessageBox.Show("선택한 클러스터에 병합된 하위 항목이 없습니다.", "정보",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                    return;
                }

                // 2. 상태 레이블 업데이트
                status_label.Text = $"총 {childClusters.Count}개 항목";

                // 3. 하위 클러스터 정보를 DataTable로 변환
                var detailDataTable = ConvertToDataTable(childClusters);

                // 4. 세부 정보를 보강하기 위해 raw_data 정보 가져오기
                var enrichedDetailTable = await EnrichWithRawTableDataAsync(detailDataTable);

                // 5. DataGridView에 표시
                PopulateDetailDataGridView(enrichedDetailTable);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터 세부정보 표시 오류: {ex.Message}");
                MessageBox.Show($"클러스터 세부정보를 불러오는 중 오류가 발생했습니다.\n{ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private DataTable ConvertToDataTable(List<ClusteringResultDocument> clusters)
        {
            var detailDataTable = new DataTable();

            // UI 표시용 컬럼 추가 (기존 코드와 호환성 유지)
            detailDataTable.Columns.Add("ID", typeof(int));
            detailDataTable.Columns.Add("ClusterID", typeof(int));
            detailDataTable.Columns.Add("클러스터명", typeof(string));
            detailDataTable.Columns.Add("키워드목록", typeof(string));
            detailDataTable.Columns.Add("Count", typeof(int));
            detailDataTable.Columns.Add("합산금액", typeof(decimal));
            detailDataTable.Columns.Add("dataIndex", typeof(string));
            detailDataTable.Columns.Add("_MongoId", typeof(string));

            // 데이터 채우기
            foreach (var cluster in clusters)
            {
                DataRow row = detailDataTable.NewRow();

                // MongoDB 문서 속성을 행에 매핑
                row["_MongoId"] = cluster.Id;
                row["ID"] = cluster.ClusterNumber;
                row["ClusterID"] = cluster.ClusterId;
                row["클러스터명"] = cluster.ClusterName;
                row["키워드목록"] = string.Join(",", cluster.Keywords);
                row["Count"] = cluster.Count;
                row["합산금액"] = cluster.TotalAmount;
                row["dataIndex"] = string.Join(",", cluster.DataIndices);

                detailDataTable.Rows.Add(row);
            }

            return detailDataTable;
        }

        private async Task<DataTable> EnrichWithRawTableDataAsync(DataTable inputTable)
        {
            DataTable resultTable = inputTable.Copy();

            try
            {
                // 1. MongoDB에서 is_visible=true인 컬럼 목록 가져오기
                var columnMappingRepo = new ColumnMappingRepository();
                var visibleColumns = await columnMappingRepo.GetVisibleColumnsAsync();

                if (visibleColumns.Count == 0)
                {
                    Debug.WriteLine("표시할 컬럼이 없습니다. column_mapping 컬렉션을 확인하세요.");
                    return resultTable;
                }

                // 2. 결과 테이블에 컬럼 추가
                foreach (var column in visibleColumns)
                {
                    if (!resultTable.Columns.Contains(column.OriginalName))
                    {
                        resultTable.Columns.Add(column.OriginalName, typeof(string));
                    }
                }

                // 3. 모든 행에서 조회할 ID 목록 수집
                HashSet<string> rawDataIds = new HashSet<string>();
                Dictionary<string, List<DataRow>> idToRowsMap = new Dictionary<string, List<DataRow>>();

                foreach (DataRow row in resultTable.Rows)
                {
                    string dataIndices = row["dataIndex"]?.ToString();
                    if (string.IsNullOrEmpty(dataIndices))
                        continue;

                    // 쉼표로 구분된 경우 모든 ID를 처리
                    string[] indices = dataIndices.Split(',');
                    foreach (string indexStr in indices)
                    {
                        string trimmedIndex = indexStr.Trim();
                        if (string.IsNullOrEmpty(trimmedIndex))
                            continue;

                        rawDataIds.Add(trimmedIndex);

                        // ID를 키로, 해당 ID를 참조하는 행들을 값으로 저장
                        if (!idToRowsMap.ContainsKey(trimmedIndex))
                        {
                            idToRowsMap[trimmedIndex] = new List<DataRow>();
                        }
                        idToRowsMap[trimmedIndex].Add(row);
                    }
                }

                if (rawDataIds.Count == 0)
                    return resultTable;

                // 4. MongoDB에서 raw_data 문서 조회
                var rawDataRepo = new RawDataRepository();
                var filter = Builders<RawDataDocument>.Filter.In(d => d.Id, rawDataIds.ToList());
                var rawDataDocuments = await rawDataRepo.FindDocumentsAsync(filter);

                // 5. 조회된 데이터를 결과 테이블에 매핑
                foreach (var doc in rawDataDocuments)
                {
                    if (idToRowsMap.ContainsKey(doc.Id))
                    {
                        foreach (DataRow resultRow in idToRowsMap[doc.Id])
                        {
                            foreach (var column in visibleColumns)
                            {
                                string columnName = column.OriginalName;
                                if (doc.Data.ContainsKey(columnName))
                                {
                                    resultRow[columnName] = doc.Data[columnName]?.ToString() ?? "";
                                }
                            }
                        }
                    }
                }

                Debug.WriteLine($"raw_data 문서 {rawDataDocuments.Count}개로 클러스터링 데이터를 보강했습니다.");
                return resultTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RAW_TABLE 데이터 추가 중 오류 발생: {ex.Message}");
                return resultTable;
            }
        }

        // ClusterDetailPopup.cs의 PopulateDetailDataGridView 함수 수정
        private void PopulateDetailDataGridView(DataTable dt)
        {
            // DataGridView 초기화
            detail_grid_view.DataSource = null;
            detail_grid_view.Rows.Clear();
            detail_grid_view.Columns.Clear();
            if (DataHandler.dragSelections.ContainsKey(detail_grid_view))
            {
                DataHandler.dragSelections[detail_grid_view].Clear();
            }

            // 데이터그리드뷰 속성 설정 - 고정 컬럼 사용 활성화
            detail_grid_view.RowHeadersVisible = false;
            detail_grid_view.AutoGenerateColumns = false;
            detail_grid_view.AllowUserToResizeRows = false;

            // CheckBox 컬럼 추가
            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn();
            checkColumn.Name = "CheckBox";
            checkColumn.HeaderText = "";
            checkColumn.Width = 50;
            checkColumn.MinimumWidth = 50;
            checkColumn.ThreeState = false;
            checkColumn.FillWeight = 20;
            checkColumn.Frozen = true; // 컬럼 고정 (스크롤해도 항상 보임)
            checkColumn.Resizable = DataGridViewTriState.False; // 크기 변경 불가능

            detail_grid_view.Columns.Add(checkColumn);

            // 원본 DataTable의 컬럼들 추가
            foreach (DataColumn col in dt.Columns)
            {
                if (col.ColumnName != "ID" && // 클러스터번호 컬럼 제외
                    col.ColumnName != "_MongoId") // MongoDB ID 컬럼 제외
                {
                    DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
                    column.Name = col.ColumnName;
                    column.HeaderText = col.ColumnName;
                    column.DataPropertyName = col.ColumnName;

                    // 컬럼 너비 설정 (3배 이상 늘림)
                    if (col.ColumnName == "클러스터명")
                    {
                        column.Width = 300; // 기존 300 * 3
                    }
                    else if (col.ColumnName == "키워드목록")
                    {
                        column.Width = 300; // 키워드 목록도 넓게
                    }
                    else if (col.ColumnName == DataHandler.prod_col_name)
                    {
                        column.Width = 200; // 공급업체 컬럼
                    }
                    else if (col.ColumnName == DataHandler.dept_col_name)
                    {
                        column.Width = 250; // 부서 컬럼
                    }
                    else if (col.ColumnName == "합산금액")
                    {
                        column.Width = 200;
                        column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    }
                    else if (col.ColumnName == "Count")
                    {
                        column.Width = 100;
                        column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    }
                    else if (DataHandler.levelName.Count > 0 && col.ColumnName == DataHandler.levelName[DataHandler.levelName.Count - 1])
                    {
                        column.Width = 600; // 타겟 컬럼
                    }
                    else
                    {
                        column.Width = 200; // 기타 컬럼
                    }

                    detail_grid_view.Columns.Add(column);
                }
            }

            // 데이터 행 추가 (DataSource 사용하지 않고 직접 행 추가)
            foreach (DataRow row in dt.Rows)
            {
                int rowIndex = detail_grid_view.Rows.Add();
                detail_grid_view.Rows[rowIndex].Cells["CheckBox"].Value = false;

                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    string colName = dt.Columns[i].ColumnName;

                    // ID와 _MongoId는 건너뛰기
                    if (colName == "ID" || colName == "_MongoId")
                        continue;

                    // 이미 존재하는 컬럼인지 확인 (중복 방지)
                    if (detail_grid_view.Columns.Contains(colName))
                    {
                        if (colName == "합산금액" && row[i] != DBNull.Value)
                        {
                            // 금액 포맷팅
                            detail_grid_view.Rows[rowIndex].Cells[colName].Value = FormatToKoreanUnit(Convert.ToDecimal(row[i]));
                        }
                        else
                        {
                            detail_grid_view.Rows[rowIndex].Cells[colName].Value = row[i];
                        }
                    }
                }

                // 원본 데이터 연결을 위해 Tag에 ID 저장
                if (dt.Columns.Contains("ID"))
                {
                    detail_grid_view.Rows[rowIndex].Tag = row["ID"];
                }
            }

            // 불필요한 컬럼 숨기기
            // ClusterID 컬럼 숨기기
            if (detail_grid_view.Columns.Contains("ClusterID"))
                detail_grid_view.Columns["ClusterID"].Visible = false;

            // dataIndex 컬럼 숨기기
            if (detail_grid_view.Columns.Contains("dataIndex"))
                detail_grid_view.Columns["dataIndex"].Visible = false;

            // MongoDB 관련 필드 숨기기
            if (detail_grid_view.Columns.Contains("cluster_id"))
                detail_grid_view.Columns["cluster_id"].Visible = false;

            if (detail_grid_view.Columns.Contains("cluster_number"))
                detail_grid_view.Columns["cluster_number"].Visible = false;

            // Count 컬럼 형식 설정
            if (detail_grid_view.Columns.Contains("Count"))
            {
                detail_grid_view.Columns["Count"].DefaultCellStyle.Format = "N0"; // 천 단위 구분자
                detail_grid_view.Columns["Count"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            // DataGridView 속성 설정
            detail_grid_view.AllowUserToAddRows = false;
            detail_grid_view.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None; // 컬럼 자동 크기 조정 비활성화
            detail_grid_view.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            detail_grid_view.ReadOnly = false;

            detail_grid_view.Columns["CheckBox"].ReadOnly = false;  // 체크박스 컬럼만 편집 가능

            // 나머지 컬럼들은 읽기 전용으로 설정
            for (int i = 1; i < detail_grid_view.Columns.Count; i++)
            {
                detail_grid_view.Columns[i].ReadOnly = true;
            }

            // 금액 정렬 이벤트 핸들러 등록
            detail_grid_view.SortCompare -= DataHandler.money_SortCompare;
            detail_grid_view.SortCompare += DataHandler.money_SortCompare;

            // 스크롤바 설정 - 수평 스크롤바 항상 표시
            detail_grid_view.ScrollBars = ScrollBars.Both;

            // 우선 순위가 높은 컬럼 배치 (merge_cluster_table과 유사하게)
            ArrangeColumnsOrder();

           
        }

        private void ArrangeColumnsOrder()
        {
            //선택박스, Count수, 세목열, 타겟열, 공급업체열, 부서열,금액열
            List<string> desiredOrder = new List<string>
            {
                "CheckBox",
                "Count",
                DataHandler.sub_acc_col_name,
                DataHandler.levelName.Count > 0 ? DataHandler.levelName[DataHandler.levelName.Count - 1] : "",
                DataHandler.prod_col_name,
                DataHandler.dept_col_name,
                "합산금액"
            };

            // 기존 컬럼 위치 저장
            Dictionary<string, int> originalIndices = new Dictionary<string, int>();
            for (int i = 0; i < detail_grid_view.Columns.Count; i++)
            {
                originalIndices[detail_grid_view.Columns[i].Name] = i;
            }

            // 새로운 컬럼 순서 설정
            for (int i = 0; i < desiredOrder.Count; i++)
            {
                string colName = desiredOrder[i];
                if (!string.IsNullOrEmpty(colName) && detail_grid_view.Columns.Contains(colName))
                {
                    detail_grid_view.Columns[colName].DisplayIndex = i;
                }
            }

            // 나머지 컬럼들은 기존 순서 유지하되 우선 순위가 낮은 컬럼으로 배치
            var remainingColumns = detail_grid_view.Columns.Cast<DataGridViewColumn>()
                                 .Where(col => !desiredOrder.Contains(col.Name))
                                 .OrderBy(col => originalIndices[col.Name])
                                 .ToList();

            int nextIndex = desiredOrder.Count(d => !string.IsNullOrEmpty(d) && detail_grid_view.Columns.Contains(d));

            foreach (var col in remainingColumns)
            {
                if (nextIndex < detail_grid_view.Columns.Count)
                {
                    col.DisplayIndex = nextIndex++;
                }
                else
                {
                    Debug.WriteLine($"DisplayIndex({nextIndex})가 열 개수({detail_grid_view.Columns.Count})를 초과했습니다. 열: {col.Name}");
                    break;
                }
            }
        }

        // ClusterDetailPopup.cs의 UnmergeSelectedItems_Click 함수 수정
        private async void UnmergeSelectedItems_Click(object sender, EventArgs e)
        {
            // 체크된 행에서 클러스터 ID 목록 가져오기
            List<int> selectedClusterIds = new List<int>();

            foreach (DataGridViewRow row in detail_grid_view.Rows)
            {
                if (row.Cells["CheckBox"].Value != null &&
                    Convert.ToBoolean(row.Cells["CheckBox"].Value) == true)
                {
                    if (row.Tag != null && int.TryParse(row.Tag.ToString(), out int clusterId))
                    {
                        selectedClusterIds.Add(clusterId);
                    }
                }
            }

            if (selectedClusterIds.Count == 0)
            {
                MessageBox.Show("병합 해제할 항목을 선택해주세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 확인 메시지
            DialogResult result = MessageBox.Show(
                $"선택한 {selectedClusterIds.Count}개 항목을 병합에서 해제하시겠습니까?",
                "병합 해제 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            try
            {
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "선택한 클러스터 병합 해제 시작");

                    // ClusteringRepository 인스턴스 생성
                    var clusteringRepo = new ClusteringRepository();

                    // 진행 상황 계산을 위한 변수
                    int totalItems = selectedClusterIds.Count;
                    int processedItems = 0;

                    // 부모 클러스터 정보 가져오기
                    var parentCluster = await clusteringRepo.GetByClusterNumberAsync(_currentClusterId);
                    if (parentCluster == null)
                    {
                        MessageBox.Show("부모 클러스터 정보를 찾을 수 없습니다.", "오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // 디버깅 정보
                    Debug.WriteLine($"병합 해제 시작 - 부모 클러스터 상태:");
                    Debug.WriteLine($"  클러스터 번호: {parentCluster.ClusterNumber}");
                    Debug.WriteLine($"  클러스터 이름: {parentCluster.ClusterName}");
                    Debug.WriteLine($"  데이터 인덱스 개수: {parentCluster.DataIndices?.Count ?? 0}");
                    Debug.WriteLine($"  Count: {parentCluster.Count}");
                    Debug.WriteLine($"  합산금액: {parentCluster.TotalAmount}");

                    // 하위 항목 조회
                    var childClusters = await clusteringRepo.GetChildClustersAsync(_currentClusterId);
                    Debug.WriteLine($"  하위 클러스터 개수(병합 해제 전): {childClusters.Count}");

                    // 선택된 항목의 세부 정보
                    Debug.WriteLine($"선택된 항목 ({selectedClusterIds.Count}개):");
                    foreach (int clusterId in selectedClusterIds)
                    {
                        var cluster = await clusteringRepo.GetByClusterNumberAsync(clusterId);
                        if (cluster != null)
                        {
                            Debug.WriteLine($"  클러스터 번호: {cluster.ClusterNumber}, Count: {cluster.Count}, 데이터 인덱스 개수: {cluster.DataIndices?.Count ?? 0}");
                        }
                    }

                    // 부모 클러스터의 남아있는 하위 클러스터 계산
                    int remainingChildCount = childClusters.Count - selectedClusterIds.Count;
                    Debug.WriteLine($"  남아있을 하위 클러스터 개수(예상): {remainingChildCount}");

                    // 병합 해제 작업 진행 - ClusterId만 업데이트하고 부모 클러스터 상태는 수정하지 않음
                    foreach (int clusterId in selectedClusterIds)
                    {
                        // MongoDB에서 클러스터 ID 업데이트
                        await clusteringRepo.UpdateClusterIdAsync(clusterId, -1);

                        // 메모리 상의 DataHandler.finalClusteringData도 업데이트
                        var rows = DataHandler.finalClusteringData.Select($"ID = {clusterId}");
                        if (rows.Length > 0)
                        {
                            rows[0]["ClusterID"] = -1;
                        }

                        // 진행 상황 업데이트
                        processedItems++;
                        int progress = 10 + (int)((double)processedItems / totalItems * 40);
                        await progressForm.UpdateProgressHandler(progress, $"{processedItems}/{totalItems} 처리 중...");
                    }

                    // 변경사항 적용
                    DataHandler.finalClusteringData.AcceptChanges();

                    await progressForm.UpdateProgressHandler(50, "부모 클러스터 상태 확인 중...");

                    // 병합 해제 후 부모 클러스터의 하위 클러스터 다시 조회
                    var updatedChildClusters = await clusteringRepo.GetChildClustersAsync(_currentClusterId);
                    Debug.WriteLine($"  하위 클러스터 개수(병합 해제 후): {updatedChildClusters.Count}");

                    // 실제 남아있는 하위 클러스터가 있는지 확인
                    if (updatedChildClusters.Count > 0)
                    {
                        Debug.WriteLine("  남아있는 하위 클러스터가 있으므로 부모 클러스터 유지");

                        // 부모 클러스터 상태 재계산
                        await progressForm.UpdateProgressHandler(70, "부모 클러스터 상태 업데이트 중...");

                        // 모든 하위 클러스터의 데이터 인덱스와 Count 등을 합산
                        HashSet<string> allDataIndices = new HashSet<string>();
                        int totalCount = 0;
                        decimal totalAmount = 0;

                        foreach (var childCluster in updatedChildClusters)
                        {
                            foreach (var index in childCluster.DataIndices)
                            {
                                allDataIndices.Add(index);
                            }
                            totalCount += childCluster.Count;
                            totalAmount += childCluster.TotalAmount;
                        }

                        Debug.WriteLine($"  재계산된 데이터 인덱스 개수: {allDataIndices.Count}");
                        Debug.WriteLine($"  재계산된 Count: {totalCount}");
                        Debug.WriteLine($"  재계산된 합산금액: {totalAmount}");

                        // 부모 클러스터 업데이트
                        await clusteringRepo.UpdateClusterFullInfoAsync(
                            _currentClusterId,
                            parentCluster.ClusterName,
                            parentCluster.Keywords,
                            totalCount,
                            totalAmount,
                            allDataIndices.ToList()
                        );

                        // DataHandler.finalClusteringData 업데이트
                        var parentRows = DataHandler.finalClusteringData.Select($"ID = {_currentClusterId}");
                        if (parentRows.Length > 0)
                        {
                            parentRows[0]["Count"] = totalCount;
                            parentRows[0]["합산금액"] = totalAmount;
                            parentRows[0]["dataIndex"] = string.Join(",", allDataIndices);
                        }

                        // 세부 정보 다시 표시
                        await progressForm.UpdateProgressHandler(90, "데이터 갱신 중...");
                        await ShowClusterDetail(_currentClusterId);
                    }
                    else
                    {
                        Debug.WriteLine("  남아있는 하위 클러스터가 없으므로 부모 클러스터 삭제");

                        // 부모 클러스터 삭제
                        await progressForm.UpdateProgressHandler(70, "빈 부모 클러스터 삭제 중...");
                        await clusteringRepo.DeleteByClusterNumberAsync(_currentClusterId);

                        // DataHandler.finalClusteringData에서도 제거
                        var parentRows = DataHandler.finalClusteringData.Select($"ID = {_currentClusterId}");
                        if (parentRows.Length > 0)
                        {
                            parentRows[0].Delete();
                            DataHandler.finalClusteringData.AcceptChanges();
                        }

                        await progressForm.UpdateProgressHandler(90, "데이터 갱신 중...");
                    }

                    // 이벤트 발생 - 부모 컨트롤에 병합 해제 완료 알림
                    var unmergeEventArgs = new ClusterUnmergeEventArgs
                    {
                        UnmergedClusterIds = selectedClusterIds,
                        ParentClusterId = _currentClusterId,
                        RefreshRequired = true
                    };

                    UnmergeCompleted?.Invoke(this, unmergeEventArgs);

                    await progressForm.UpdateProgressHandler(100);
                    progressForm.Close();

                    if (updatedChildClusters.Count > 0)
                    {
                        MessageBox.Show($"선택한 {selectedClusterIds.Count}개 항목이 병합에서 해제되었습니다.", "완료",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"선택한 {selectedClusterIds.Count}개 항목이 병합에서 해제되었습니다.\n부모 클러스터가 비어 있어 삭제되었습니다.", "완료",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Close();  // 팝업 닫기
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"병합 해제 오류: {ex.Message}");
                MessageBox.Show($"병합 해제 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SelectAll_Click(object sender, EventArgs e)
        {
            bool checkState = select_all_btn.Text == "모두 선택";

            // 모든 행 체크박스 상태 변경
            foreach (DataGridViewRow row in detail_grid_view.Rows)
            {
                row.Cells["CheckBox"].Value = checkState;
            }

            // 버튼 텍스트 변경
            select_all_btn.Text = checkState ? "모두 해제" : "모두 선택";
        }

        // 금액 포맷팅 함수
        private string FormatToKoreanUnit(decimal number)
        {
            // 절대값으로 계산 후 나중에 부호 처리
            bool isNegative = number < 0;
            number = Math.Abs(number);

            string result;
            decimal divideNum = Math.Round(number / _decimalDivider, 2);

            // 소수점 이하가 없는 경우 (정수인 경우)
            if (divideNum == Math.Truncate(divideNum))
            {
                result = string.Format("{0:N0}", divideNum) + " " + _decimalDividerName;
            }
            // 소수점 둘째 자리가 0인 경우 (예: 10.5)
            else if (divideNum * 10 % 1 == 0)
            {
                result = string.Format("{0:N1}", divideNum) + " " + _decimalDividerName;
            }
            //소수점 2째자리 표기
            else
            {
                result = string.Format("{0:N2}", divideNum) + " " + _decimalDividerName;
            }

            // 음수 처리
            if (isNegative && divideNum != 0)
            {
                result = "-" + result;
            }

            return result;
        }

        // 외부에서 단위 설정을 변경할 수 있는 메서드
        public void SetDecimalDivider(decimal divider, string dividerName)
        {
            _decimalDivider = divider;
            _decimalDividerName = dividerName;

            // 현재 표시된 데이터가 있으면 새로고침
            if (detail_grid_view.Rows.Count > 0 && _currentClusterId > 0)
            {
                ShowClusterDetail(_currentClusterId).ConfigureAwait(false);
            }
        }
    }
}