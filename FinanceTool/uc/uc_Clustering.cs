using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using FinanceTool.MongoModels;
using FinanceTool.Repositories;
using Microsoft.VisualBasic.Devices;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace FinanceTool
{
    public partial class uc_Clustering : UserControl
    {

        DataTable mergeClusterDataTable;

        private decimal decimalDivider = 1;
        private string decimalDividerName = "원";
        private string selectecLv1Name = "";
        private bool equalsSearchYN = false;
        private bool andSearchYN = false;

        private bool isFinishSession = false;
        private bool isCheckedTableObject = false;

        List<string> merge_keyword_list;
        List<string> check_keyword_list;
        List<string> supplier_keyword_list;


        // 이 변수는 유지 (새로운 아키텍처에서 완전히 대체되지 않음)
        private bool _allSelectedInCurrentFilter = false;

        //2025.06.02
        //검색엔진 객체 신규 생성
        private ClusteringManager _clusteringManager;

        // 새로 추가: 검색 내 검색 관련
        private List<int> _baseSearchResults = new List<int>();
        private bool _isSubSearchMode = false;

        // 전역 인스턴스 생성
        private static RecomandKeywordManager _recomandKeywordManager;


        public uc_Clustering()
        {
            // 시스템 성능 최적화 (한 번만 실행됨)
            SystemPerformanceOptimizer.OptimizeSystemForUltraSpeed();

            InitializeComponent();



            // 통화 단위가 변경될 때 팝업에도 적용
            decimal_combo.SelectedIndexChanged += (s, e) =>
            {
                double divider = Math.Pow(1000, decimal_combo.SelectedIndex);
                if (decimal_combo.SelectedIndex == 3)
                    divider = divider / 10; // 억 원은 10 나누기


            };

            // 컨텍스트 메뉴 초기화
            InitializeContextMenu();
        }

        // 4. 컨텍스트 메뉴 초기화
        private void InitializeContextMenu()
        {
            // 컨텍스트 메뉴 생성
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem viewDetailsItem = new ToolStripMenuItem("세부 정보 보기");

            viewDetailsItem.Click += (s, e) =>
            {
                // *** 핵심 개선: 우클릭한 행을 우선시 ***

                // 1. 먼저 현재 우클릭된 행 정보 확인
                DataGridViewRow rightClickedRow = null;
                if (merge_check_table.SelectedRows.Count > 0)
                {
                    rightClickedRow = merge_check_table.SelectedRows[0];
                }
                else if (merge_check_table.CurrentRow != null)
                {
                    rightClickedRow = merge_check_table.CurrentRow;
                }

                if (rightClickedRow == null)
                {
                    MessageBox.Show("세부 정보를 확인할 클러스터를 선택해주세요.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 2. 모든 기존 체크 상태 초기화
                foreach (DataGridViewRow row in merge_check_table.Rows)
                {
                    if (row.Cells["CheckBox"].Value != null)
                    {
                        row.Cells["CheckBox"].Value = false;
                    }
                }

                // 3. 우클릭한 행만 체크 상태로 설정
                rightClickedRow.Cells["CheckBox"].Value = true;

                // 4. 세부 정보 표시
                ShowMergeClusterDetail();

                Debug.WriteLine($"컨텍스트 메뉴: 행 {rightClickedRow.Index}를 선택하고 세부정보 표시");
            };

            contextMenu.Items.Add(viewDetailsItem);
            merge_check_table.ContextMenuStrip = contextMenu;
        }

        public async void initUI()
        {
            try
            {

                // MongoDB에서 클러스터링 데이터 로드
                var clusteringRepo = new ClusteringRepository();
                DataTable mongoClusterData = await clusteringRepo.ToDataTableAsync();
                // 수정 후 코드
                //DataTable mongoClusterData = await clusteringRepo.GetUnmergedClustersAsDataTableAsync();

                // 기존 데이터가 있다면 사용, 없으면 secondClusteringData 사용
                if (mongoClusterData != null && mongoClusterData.Rows.Count > 0)
                {
                    Debug.WriteLine($"MongoDB에서 {mongoClusterData.Rows.Count}개의 클러스터 데이터를 로드했습니다.");
                    DataHandler.finalClusteringData = mongoClusterData;
                }
                else
                {
                    Debug.WriteLine("MongoDB에 데이터가 없어 메모리 데이터를 사용합니다.");
                    DataHandler.finalClusteringData = DataHandler.secondClusteringData.Copy();

                    // 초기 실행 시 MongoDB에 데이터 저장 (선택적)
                    if (DataHandler.finalClusteringData != null && DataHandler.finalClusteringData.Rows.Count > 0)
                    {
                        await SaveClusteringDataToMongoDBAsync(DataHandler.finalClusteringData);
                    }
                }

                Debug.WriteLine($"[CreateCheckDataGridView] DataHandler.finalClusteringData 총 컬럼 수: {DataHandler.finalClusteringData.Columns.Count}");
                for (int i = 0; i < DataHandler.finalClusteringData.Columns.Count; i++)
                {
                    Debug.WriteLine($"  컬럼 {i}: Name='{DataHandler.finalClusteringData.Columns[i].ColumnName}'" +
                        $", DataType='{DataHandler.finalClusteringData.Columns[i].DataType}'");
                }


                // RawData 정보로 보강
                mergeClusterDataTable = await EnrichWithRawTableDataAsync(DataHandler.finalClusteringData);

                
                // 최초 수행 시 별도 수행
                supplier_keyword_list = ExtractUniqueSupplierKeywords(mergeClusterDataTable, 0);

                // 4. *** 핵심 수정: ClusteringManager를 초기화 ***
                _clusteringManager = new ClusteringManager();
                await _clusteringManager.InitializeAsync(mergeClusterDataTable, merge_cluster_table,
                    num_pageNumber, cmb_pageSize, btn_prevPage, btn_nextPage, lbl_pagination2, merge_all_check);

                // 검색 UI 초기화 (새로 추가)
                InitializeSearchUI();

                // 5. *** 수정: 초기 전체 검색을 안전하게 실행 ***
                await PerformInitialSearch();

                // 6. 나머지 UI 초기화 작업들 (기존 코드 그대로 유지)
                await InitializeRemainingUI();

                //세부 목록 재조회
                create_check_keyword_list();

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"initUI 메서드 오류: {ex.Message}");
                MessageBox.Show($"클러스터링 데이터 로드 중 오류가 발생했습니다.\n{ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        
        private void merge_cluster_table_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0 && e.RowIndex >= 0) // 체크박스 컬럼
            {
                // 현재 페이지의 선택 상태 저장
                //SaveCurrentSelectionState();
                _clusteringManager.SaveCurrentSelectionState();

                // 전체 선택 체크박스 상태 업데이트
                UpdateMergeAllCheckState();
            }
        }


        
        private void dataGridView_lv1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;

            if (dgv == null) return;

            List<string> keywords;

            //Debug.WriteLine($"DataGridView_CellContentClick start => dragSelections[dgv].Count : {dragSelections[dgv].Count}");
            // 체크박스 컬럼이 아닌 다른 컬럼 클릭 시
            //if (e.ColumnIndex == 0 && e.RowIndex >= 0)
            if (e.ColumnIndex != 0 && e.RowIndex >= 0)
            {
                string lv1Name = dataGridView_lv1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();

                selectecLv1Name = lv1Name;
                Lv1Item selectedItem = _recomandKeywordManager.GetLv1Item(lv1Name);


                if (selectedItem != null)
                {
                    keywords = selectedItem.Keywords;
                    create_keyword_table(dataGridView_recoman_keyword, keywords, false);
                }
            }


        }


        private async void dataGridView_keyword_CellClick(object sender, DataGridViewCellEventArgs e)
        {

            DataGridView dgv = sender as DataGridView;
            if (dgv == null) return;

            // 데이터 컬럼 클릭 시 (1번 컬럼)
            if (e.ColumnIndex == 1 && e.RowIndex >= 0)
            {
                string keyword = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
                if (string.IsNullOrEmpty(keyword)) return;

                Debug.WriteLine($"키워드 클릭: {keyword}");

                try
                {
                    // ClusteringManager를 통한 정확 매칭 검색
                    var matchingKeywords = _clusteringManager.SearchExact("키워드목록", keyword);

                    if (matchingKeywords.Count > 0)
                    {
                        // 다중 컬럼 검색 조건 구성
                        var columnCriteria = new Dictionary<string, SearchColumnCriteria>
                        {
                            ["키워드목록"] = new SearchColumnCriteria
                            {
                                Keywords = matchingKeywords,
                                ExactMatch = true,
                                UseAnd = false
                            }
                        };

                        // 제외 키워드 추가
                        var excludeKeywords = GetExcludeKeywords();

                        // 검색 실행
                        await _clusteringManager.SearchMultipleColumnsAsync(columnCriteria, excludeKeywords);

                        // 선택 상태 초기화
                        merge_all_check.Checked = false;
                        change_row_count();

                        Debug.WriteLine($"키워드 검색 완료: {matchingKeywords.Count}개 키워드");
                    }
                    else
                    {
                        // 검색 결과 없음 - 빈 테이블 표시
                        await ShowEmptySearchResult();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"키워드 검색 오류: {ex.Message}");
                    MessageBox.Show($"키워드 검색 중 오류가 발생했습니다: {ex.Message}", "오류",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // 6. 새로운 이벤트 핸들러: 공급업체별 테이블 클릭 시 검색
        private async void dataGridView_supply_summary_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            if (dgv == null) return;

            // 데이터 컬럼 클릭 시 (1번 컬럼)
            if (e.ColumnIndex == 1 && e.RowIndex >= 0)
            {
                string supplier = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
                if (string.IsNullOrEmpty(supplier)) return;

                Debug.WriteLine($"공급업체 클릭: {supplier}");

                try
                {
                    // ClusteringManager를 통한 정확 매칭 검색
                    var matchingSuppliers = _clusteringManager.SearchExact(DataHandler.prod_col_name, supplier);

                    if (matchingSuppliers.Count > 0)
                    {
                        // 다중 컬럼 검색 조건 구성
                        var columnCriteria = new Dictionary<string, SearchColumnCriteria>
                        {
                            [DataHandler.prod_col_name] = new SearchColumnCriteria
                            {
                                Keywords = matchingSuppliers,
                                ExactMatch = true,
                                UseAnd = false
                            }
                        };

                        // 제외 키워드 추가
                        var excludeKeywords = GetExcludeKeywords();

                        // 검색 실행
                        await _clusteringManager.SearchMultipleColumnsAsync(columnCriteria, excludeKeywords);

                        // 선택 상태 초기화
                        merge_all_check.Checked = false;
                        change_row_count();

                        Debug.WriteLine($"공급업체 검색 완료: {matchingSuppliers.Count}개 공급업체");
                    }
                    else
                    {
                        // 검색 결과 없음 - 빈 테이블 표시
                        await ShowEmptySearchResult();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"공급업체 검색 오류: {ex.Message}");
                    MessageBox.Show($"공급업체 검색 중 오류가 발생했습니다: {ex.Message}", "오류",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

       
        private async void merge_search_button_Click(object sender, EventArgs e)
        {
            //create_merge_keyword_list();

            try
            {
                string searchKeyword = merge_search_keyword.Text?.Trim() ?? "";

                // 검색어가 없을 때 사용자에게 확인
                /*
                if (string.IsNullOrEmpty(searchKeyword))
                {
                    if (_isSubSearchMode && _baseSearchResults.Count > 0)
                    {
                        Debug.WriteLine("결과 내 재검색: 이전 검색 결과 표시");
                    }
                    else
                    {
                        var result = MessageBox.Show(
                            "검색어가 입력되지 않았습니다. 전체 데이터를 표시하시겠습니까?",
                            "전체 데이터 검색",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question
                        );

                        if (result == DialogResult.No)
                        {
                            return;
                        }
                        Debug.WriteLine("전체 데이터 검색 실행");
                    }
                }
                */
                await create_merge_keyword_list();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"검색 버튼 클릭 오류: {ex.Message}");
                MessageBox.Show($"검색 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void merge_all_check_CheckedChanged(object sender, EventArgs e)
        {
            if (isCheckedTableObject)
            {
                return;
            }
            try
            {
                bool selectAll = merge_all_check.Checked;

                // 1. 현재 표시된 그리드의 모든 행 체크박스 변경
                foreach (DataGridViewRow row in merge_cluster_table.Rows)
                {
                    row.Cells["CheckBox"].Value = selectAll;
                }

                // 2. 현재 필터 결과의 모든 클러스터 ID 처리
                if (selectAll)
                {
                    // 전체 선택: 현재 필터 결과의 모든 ID를 선택 목록에 추가
                    var currentFilterIds = GetCurrentFilterClusterIds();
                    foreach (int clusterId in currentFilterIds)
                    {
                        _clusteringManager.AddToSelection(clusterId);
                    }
                }
                else
                {
                    // 전체 해제: 현재 필터 결과의 모든 ID를 선택 목록에서 제거
                    var currentFilterIds = GetCurrentFilterClusterIds();
                    foreach (int clusterId in currentFilterIds)
                    {
                        _clusteringManager.RemoveFromSelection(clusterId);
                    }
                }

                // 3. 선택 상태 저장
                _clusteringManager.SaveCurrentSelectionState();

                // 4. 전체 선택 플래그 업데이트
                _allSelectedInCurrentFilter = selectAll;

                Debug.WriteLine($"전체 선택 변경: {selectAll}, 대상 항목: {GetCurrentFilterClusterIds().Count}개");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"전체 선택 처리 오류: {ex.Message}");
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // 현재 선택 상태 저장 후 선택된 ID 목록 조회
                _clusteringManager.SaveCurrentSelectionState();
                var selectedClusterIds = _clusteringManager.GetSelectedClusterIds();

                if (selectedClusterIds.Count == 0)
                {
                    MessageBox.Show("병합할 클러스터를 선택해주세요.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }


                DialogResult result = MessageBox.Show(
                    $"선택된 {selectedClusterIds.Count}개의 클러스터를 병합하시겠습니까?",
                    "클러스터 병합 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    using (var progressForm = new ProcessProgressForm())
                    {
                        progressForm.Show();
                        await progressForm.UpdateProgressHandler(10, "클러스터 병합 시작");
                        await Task.Delay(10);


                        // 기존 병합 로직 호출 (cluster_number 리스트 전달)
                        List<int> clusterNumbersToMerge = selectedClusterIds.ToList();

                        await MergeAndCreateNewCluster(DataHandler.finalClusteringData, clusterNumbersToMerge);

                        await progressForm.UpdateProgressHandler(50, "클러스터 병합 중...");
                        await Task.Delay(10);

                        // 병합 후 선택 상태 초기화
                        //_selectedClusterNumbers.Clear();
                        merge_all_check.Checked = false;

                        // 데이터 다시 로드
                        await create_merge_keyword_list(true);

                        await progressForm.UpdateProgressHandler(100, "클러스터 병합 완료");
                        await Task.Delay(10);

                    }

                    MessageBox.Show("클러스터 병합이 완료되었습니다.", "완료",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터 병합 중 오류: {ex.Message}");
                MessageBox.Show($"클러스터 병합 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void merge_cancel_button_Click(object sender, EventArgs e)
        {
            List<int> mergeIDlList = GetCheckedRowsData(merge_check_table);

            if (mergeIDlList.Count == 0)
            {
                MessageBox.Show("병합 해제 대상을 선택하셔야 합니다.", "알림",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Warning);

                return;
            }

            using (var progressForm = new ProcessProgressForm())
            {
                progressForm.Show();
                await progressForm.UpdateProgressHandler(10, "클러스터 병합 해제 시작");
                await Task.Delay(10);

               

                await deleteClusterId(DataHandler.finalClusteringData, mergeIDlList);

                await progressForm.UpdateProgressHandler(50, "클러스터 병합 해제 중...");
                await Task.Delay(10);

                //검색조건 초기화
                check_search_keyword.Text = "";

                create_merge_keyword_list();
                create_check_keyword_list();

                await progressForm.UpdateProgressHandler(80, "클러스터 목록 재 조회중...");
                await Task.Delay(10);


                // 병합 작업 후 업데이트
                UpdateModifiedDataGridView();
                UpdateSupplySummaryDataGridView();

                MessageBox.Show(this, "클러스터 병합 해제가 완료되었습니다.", "Info",
                                       MessageBoxButtons.OK,
                                       MessageBoxIcon.Information);

                // 포커스 명시적 복원
                this.Focus(); // UserControl에 포커스
                if (this.ParentForm != null)
                    this.ParentForm.Activate(); // 부모 폼 활성화
            }
                
        }
        // 클러스터명 원래 값을 저장할 Dictionary 추가 (클래스의 멤버 변수로 선언)
        private Dictionary<int, string> originalClusterNames = new Dictionary<int, string>();

        private void DataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            if (dgv == null) return;

            // 클러스터명 컬럼만 처리
            if (e.ColumnIndex == dgv.Columns["클러스터명"].Index)
            {
                // 현재 값 저장
                int rowId = e.RowIndex;
                string currentValue = dgv.Rows[rowId].Cells[e.ColumnIndex].Value?.ToString() ?? "";

                // 같은 키가 이미 있을 경우 업데이트, 없으면 추가
                if (originalClusterNames.ContainsKey(rowId))
                    originalClusterNames[rowId] = currentValue;
                else
                    originalClusterNames.Add(rowId, currentValue);

                Debug.WriteLine($"셀 편집 시작: 행 {rowId}, 원래 값: {currentValue}");
            }
        }

        private async void DataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            if (dgv == null) return;

            // 클러스터명 컬럼이 아니면 종료
            if (e.ColumnIndex != dgv.Columns["클러스터명"].Index)
            {
                Debug.WriteLine("클러스터명 컬럼이 아닙니다. 편집 처리를 건너뜁니다.");
                return;
            }

            // 수정된 값 가져오기
            string newValue = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";

            // 원래 값 확인 (없으면 빈 문자열 사용)
            string originalValue = "";
            if (originalClusterNames.ContainsKey(e.RowIndex))
                originalValue = originalClusterNames[e.RowIndex];

            // 값이 변경되지 않았으면 종료
            if (newValue == originalValue)
            {
                Debug.WriteLine("값이 변경되지 않았습니다. 업데이트를 건너뜁니다.");
                return;
            }

            // 값이 비어있으면 종료
            if (string.IsNullOrEmpty(newValue))
            {
                Debug.WriteLine("새 값이 비어 있습니다. 업데이트를 건너뜁니다.");
                create_check_keyword_list();
                return;
            }

            using (var progressForm = new ProcessProgressForm())
            {
                progressForm.Show();
                await progressForm.UpdateProgressHandler(10, "클러스터명 변경 시작");
                await Task.Delay(10);

                // DataHandler.finalClusteringData 업데이트
                int id = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["ID"].Value);
                DataRow[] rows = DataHandler.finalClusteringData.Select($"ID = {id}");
                if (rows.Length > 0)
                {
                    rows[0]["클러스터명"] = newValue;
                }

                await progressForm.UpdateProgressHandler(30, "메모리 데이터 업데이트 완료");

                // 2. MongoDB 업데이트 추가
                var clusteringRepo = new ClusteringRepository();
                bool mongoUpdateSuccess = await clusteringRepo.UpdateClusterNameAsync(id, newValue);

                if (!mongoUpdateSuccess)
                {
                    throw new Exception("MongoDB 클러스터명 업데이트 실패");
                }

                await progressForm.UpdateProgressHandler(60, "MongoDB 업데이트 완료");
                // 변경 사항 저장
                DataHandler.finalClusteringData.AcceptChanges();
                mergeClusterDataTable = await EnrichWithRawTableDataAsync(DataHandler.finalClusteringData);

                await progressForm.UpdateProgressHandler(70, "클러스터명 변경 결과 출력 중...");
                await Task.Delay(10);

                // 4. ClusteringManager 데이터 새로고침
                if (_clusteringManager != null)
                {
                    await _clusteringManager.RefreshDataAsync(mergeClusterDataTable);
                }

                create_check_keyword_list();

                await progressForm.UpdateProgressHandler(100);
                await Task.Delay(10);
                progressForm.Close();

                MessageBox.Show("클러스터명 변경이 완료되었습니다.", "Info",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Information);
            }

            // 사용한 원래 값 정보 삭제
            if (originalClusterNames.ContainsKey(e.RowIndex))
                originalClusterNames.Remove(e.RowIndex);
        }

        private void check_search_button_Click(object sender, EventArgs e)
        {
            create_check_keyword_list();
        }

        private void merge_search_keyword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                create_merge_keyword_list();   // 호출하고 싶은 함수
                e.SuppressKeyPress = true;  // 비프음 방지
            }
        }

        private void check_search_keyword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                create_check_keyword_list();   // 호출하고 싶은 함수
                e.SuppressKeyPress = true;  // 비프음 방지
            }
        }

        private async void complete_btn_Click(object sender, EventArgs e)
        {
            try
            {
                using (var progressForm = new ProcessProgressForm())
                {

                    int clusterCount = GetCountOfNegativeOneClusterIDs(DataHandler.finalClusteringData);

                    if (clusterCount > 0)
                    {
                        //await progressForm.UpdateProgressHandler(20, "병합 클러스터링 통합 시작...");

                        // 최대 속도 처리를 위한 극한 병렬 클러스터 통합
                        //await ProcessMaxSpeedClusterMergeAsync(clusterCount, progressForm.UpdateProgressHandler);

                        DialogResult result = MessageBox.Show(
                                   $"{clusterCount}개의 클러스터가 남아있습니다.\n 자동으로 병합하시겠습니까?",
                                   "클러스터 병합 확인",
                                   MessageBoxButtons.YesNo,
                                   MessageBoxIcon.Question
                               );

                        if (result == DialogResult.No)
                        {
                            return;
                        }

                    }

                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "병합 클러스터링 완료 처리 시작...");

                    if (clusterCount > 0)
                    {
                        await progressForm.UpdateProgressHandler(20, "병합 클러스터링 통합 시작...");

                        // 최대 속도 처리를 위한 극한 병렬 클러스터 통합
                        await ProcessMaxSpeedClusterMergeAsync(clusterCount, progressForm.UpdateProgressHandler);
                    }


                    await progressForm.UpdateProgressHandler(60, "최종 데이터 검증...");

                    // 최대 속도 최종 처리
                    //await MaxSpeedFinalizeAsync(progressForm.UpdateProgressHandler);


                    // *** 6단계: DataTable 업데이트 (기존 행 업데이트 + 병합된 클러스터들의 ClusterID 변경) ***
                    //await UpdateDataTableAfterMerge(DataHandler.finalClusteringData, targetIds, newClusterNumber, isNewCluster);

                    // *** 7단계: 데이터 보강 (동기적 처리로 일관성 보장) ***
                    //mergeClusterDataTable = await EnrichWithRawTableDataAsync(DataHandler.finalClusteringData);

                    // *** 8단계: ClusteringManager 데이터 새로고침 ***
                    if (_clusteringManager != null)
                    {
                        await _clusteringManager.RefreshDataAsync(mergeClusterDataTable);
                    }

                    //Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 병합 완료: {newClusterNumber}");

                    await progressForm.UpdateProgressHandler(80, "데이터 정렬 중...");

                    // 병합 클러스터 리스트 생성
                    create_check_keyword_list();

                    // 병합 작업 후 업데이트
                    UpdateModifiedDataGridView();
                    UpdateSupplySummaryDataGridView();

                    //merge_cluster_table.Rows.Clear();

                    //dataGridView_modified.Rows.Clear();


                    await progressForm.UpdateProgressHandler(100, "클러스터링 완료");

                    // 다음 페이지로 이동
                    userControlHandler.uc_classification.initUI();

                    if (this.ParentForm is Form1 form)
                    {
                        form.LoadUserControl(userControlHandler.uc_classification);
                    }

                    isFinishSession = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"complete_btn_Click 오류: {ex.Message}");
                MessageBox.Show($"클러스터 완료 처리 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            //decimalDividerName = decimal_combo.SelectedItem.ToString();
            // 단위명 설정
            switch (decimal_combo.SelectedIndex)
            {
                case 0: decimalDividerName = "원"; break;
                case 1: decimalDividerName = "천원"; break;
                case 2: decimalDividerName = "백만원"; break;
                case 3: decimalDividerName = "억원"; break;
                default: decimalDividerName = "원"; break;
            }

            // ClusteringManager에 단위 정보 전달
            if (_clusteringManager != null)
            {
                _clusteringManager.UpdateCurrencyFormat(decimalDivider, decimalDividerName);
                // 현재 표시된 데이터 새로고침
                _clusteringManager.RefreshCurrentDisplay();
            }

            //리스트 재 조회
            // 나머지 초기화 로직
            //await Task.Run(() => create_merge_keyword_list());
            await Task.Run(() =>
            {
                if (Application.OpenForms.Count > 0)
                {
                    Application.OpenForms[0].Invoke((MethodInvoker)delegate
                    {
                        merge_cluster_table.DataSource = null;
                        create_merge_keyword_list();

                        merge_check_table.DataSource = null;

                        create_check_keyword_list();

                        //2025.04.23
                        //추천 키워드 리스트 재조회
                        UpdateModifiedDataGridView();
                        UpdateSupplySummaryDataGridView();
                    });
                }
            });
        }

        private async void merge_addon_btn_Click(object sender, EventArgs e)
        {
            List<int> mergeIDlList = GetCheckedRowsData(merge_cluster_table);
            if (mergeIDlList.Count < 1)
            {
                MessageBox.Show("병합 테이블에서 추가 병합을 진행할 대상을 선택하셔야 합니다.", "알림",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Warning);

                return;
            }

            List<int> mergeClusterIDlList = GetCheckedRowsData(merge_check_table);
            if (mergeClusterIDlList.Count < 1)
            {
                MessageBox.Show("추가 병합 수행 시 병합 결과 확인 테이블에서 \n 병합 시킬 클러스터를 선택하셔야 합니다.", "알림",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Warning);

                return;
            }

            if (mergeClusterIDlList.Count > 1)
            {
                MessageBox.Show("추가 병합 수행 시 병합 결과 확인 테이블에서 \n 병합 시킬 클러스터 1개만 선택해주세요.", "알림",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Warning);

                return;
            }

            using (var progressForm = new ProcessProgressForm())
            {
                progressForm.Show();
                await progressForm.UpdateProgressHandler(10, "클러스터 병합 시작");
                await Task.Delay(10);

                string mergeAddClusterID = mergeClusterIDlList[0].ToString(); // 문자열로 변경
                int checkIndex = GetCheckedRowsIndex(merge_check_table);

                Debug.WriteLine($" checkIndex : {checkIndex}");

                // clusterID 매개변수를 문자열로 전달
                await MergeAndCreateNewCluster(DataHandler.finalClusteringData, mergeIDlList, null, mergeAddClusterID);

                await progressForm.UpdateProgressHandler(50, "클러스터 병합 진행중...");
                await Task.Delay(10);


                //검색조건 초기화
                merge_search_keyword.Text = "";

                await progressForm.UpdateProgressHandler(70, "클러스터 병합 결과 출력 중...");
                await Task.Delay(10);

                create_merge_keyword_list(true);
                create_check_keyword_list();

                // 병합 작업 후 업데이트
                UpdateModifiedDataGridView();
                UpdateSupplySummaryDataGridView();

                //추가 병합의 경우만 포커스 셀 변경
                await Task.Run(() =>
                {
                    if (Application.OpenForms.Count > 0)
                    {
                        Application.OpenForms[0].Invoke((MethodInvoker)delegate
                        {
                            merge_check_table.ClearSelection();
                            merge_check_table.Rows[checkIndex].Selected = true;
                            merge_check_table.CurrentCell = merge_check_table.Rows[checkIndex].Cells[0];
                        });
                    }
                });

                await progressForm.UpdateProgressHandler(100);
                await Task.Delay(10);
                progressForm.Close();

                MessageBox.Show("클러스터 병합이 완료되었습니다.", "Info",
                                       MessageBoxButtons.OK,
                                       MessageBoxIcon.Information);
            }


        }

        private void lv1_add_btn_Click(object sender, EventArgs e)
        {
            add_lv1_keyword();
        }

        private void new_lv1_word_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                add_lv1_keyword();
                // Enter 키가 다른 동작을 막도록 처리
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void lv1_del_btn_Click(object sender, EventArgs e)
        {
            List<string> lv1_del_list = GetCheckedRowsStringData(dataGridView_lv1);

            if (lv1_del_list.Count == 0)
            {
                MessageBox.Show("Lv1 제거 대상을 선택하셔야 합니다.", "알림",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Warning);

                return;
            }

            foreach (string seperate in lv1_del_list)
            {
                //_separatorManager.Separators.Remove(seperate);
                _recomandKeywordManager.RemoveLv1Item(seperate);
            }

            for (int i = dataGridView_lv1.Rows.Count - 1; i >= 0; i--)
            {
                DataGridViewRow row = dataGridView_lv1.Rows[i];

                // columnListDgv의 두 번째 컬럼(체크박스 다음)의 값 확인
                string seperData = row.Cells[1].Value?.ToString();
                if (lv1_del_list.Contains(seperData))
                {
                    dataGridView_lv1.Rows.RemoveAt(i);
                }
            }
        }

        private void reco_add_btn_Click(object sender, EventArgs e)
        {
            add_reco_keyword();
        }


        private void new_reco_word_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                add_reco_keyword();
                // Enter 키가 다른 동작을 막도록 처리
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void reco_del_btn_Click(object sender, EventArgs e)
        {
            List<string> reco_keyword_del_list = GetCheckedRowsStringData(dataGridView_recoman_keyword);

            if (reco_keyword_del_list.Count == 0)
            {
                MessageBox.Show("추천 키워드 제거 대상을 선택하셔야 합니다.", "알림",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Warning);

                return;
            }

            foreach (string seperate in reco_keyword_del_list)
            {
                //_separatorManager.Separators.Remove(seperate);
                _recomandKeywordManager.RemoveKeyword(selectecLv1Name, seperate);
            }

            for (int i = dataGridView_recoman_keyword.Rows.Count - 1; i >= 0; i--)
            {
                DataGridViewRow row = dataGridView_recoman_keyword.Rows[i];

                // columnListDgv의 두 번째 컬럼(체크박스 다음)의 값 확인
                string seperData = row.Cells[1].Value?.ToString();
                if (reco_keyword_del_list.Contains(seperData))
                {
                    dataGridView_recoman_keyword.Rows.RemoveAt(i);
                }
            }
        }

        private void excep_search_checkbox_CheckedChanged(object sender, EventArgs e)
        {
            except_keyword.Text = "";
            except_keyword.Enabled = excep_search_checkbox.Checked;

        }

        private void equal_search_checkbox_CheckedChanged(object sender, EventArgs e)
        {
            equalsSearchYN = equal_search_checkbox.Checked;
        }

        private void except_keyword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                create_merge_keyword_list();   // 호출하고 싶은 함수
                e.SuppressKeyPress = true;  // 비프음 방지
            }
        }

        private async void union_cluster_btn_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. 체크된 항목들 찾기
                List<int> checkedClusterIds = new List<int>();

                foreach (DataGridViewRow row in merge_check_table.Rows)
                {
                    // 체크된 항목의 ClusterID 수집
                    DataGridViewCheckBoxCell checkCell = row.Cells[0] as DataGridViewCheckBoxCell;
                    if (checkCell != null && checkCell.Value != null && Convert.ToBoolean(checkCell.Value))
                    {
                        int clusterId = Convert.ToInt32(row.Cells["ID"].Value); // ID 열 사용
                        checkedClusterIds.Add(clusterId);
                    }
                }

                // 2. 체크된 항목이 1개 이하인 경우 종료
                if (checkedClusterIds.Count < 2)
                {
                    MessageBox.Show("클러스터 간 병합 수행 시\n2개 이상의 병합된 클러스터를 선택해주세요.",
                        "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "클러스터 병합 시작");

                    // 3. MongoDB에서 클러스터 번호 얻기
                    var clusteringRepo = new ClusteringRepository();
                    int newClusterNumber = await clusteringRepo.GetNextClusterNumberAsync();

                    await progressForm.UpdateProgressHandler(20, "클러스터 정보 수집 중");

                    // 4. 병합할 클러스터 정보 수집
                    List<ClusteringResultDocument> clustersToMerge = new List<ClusteringResultDocument>();
                    foreach (int clusterId in checkedClusterIds)
                    {
                        var cluster = await clusteringRepo.GetByClusterNumberAsync(clusterId);
                        if (cluster != null)
                        {
                            clustersToMerge.Add(cluster);
                        }
                    }

                    if (clustersToMerge.Count < 2)
                    {
                        progressForm.Close();
                        MessageBox.Show("병합할 유효한 클러스터가 2개 미만입니다.", "알림",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    await progressForm.UpdateProgressHandler(30, "병합 대상 확인 중");

                    // 병합 대상 클러스터들이 모두 상위 클러스터인지 확인 (cluster_number = cluster_id)
                    bool allParentClusters = clustersToMerge.All(c => c.ClusterId == c.ClusterNumber);
                    if (!allParentClusters)
                    {
                        progressForm.Close();
                        MessageBox.Show("선택한 클러스터 중 일부가 이미 다른 클러스터에 속해 있습니다.\n상위 클러스터만 선택해주세요.",
                            "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    await progressForm.UpdateProgressHandler(40, "클러스터 병합 처리 중");

                    // 5. 병합 정보 생성
                    string combinedClusterName = string.Join("_",
                        clustersToMerge.Select(c => c.ClusterName));

                    // 20자 제한 처리
                    if (combinedClusterName.Length > 20)
                    {
                        combinedClusterName = combinedClusterName.Substring(0, 17) + "...";
                    }

                    // 키워드 중복 제거하여 병합
                    HashSet<string> keywordSet = new HashSet<string>();
                    foreach (var cluster in clustersToMerge)
                    {
                        foreach (var keyword in cluster.Keywords)
                        {
                            keywordSet.Add(keyword);
                        }
                    }

                    // 데이터 인덱스 중복 제거하여 병합
                    HashSet<string> dataIndicesSet = new HashSet<string>();
                    foreach (var cluster in clustersToMerge)
                    {
                        foreach (var index in cluster.DataIndices)
                        {
                            dataIndicesSet.Add(index);
                        }
                    }

                    // 카운트 및 금액 합산
                    int totalCount = clustersToMerge.Sum(c => c.Count);
                    decimal totalAmount = clustersToMerge.Sum(c => c.TotalAmount);

                    // 6. 각 병합 대상 클러스터의 하위 클러스터 ID 수집
                    await progressForm.UpdateProgressHandler(50, "하위 클러스터 수집 중");

                    List<int> allChildClusterNumbers = new List<int>();

                    //2025.07.31
                    //변경대상 목록에 세부클러스터링 항목도 추가
                    foreach (var cluster in clustersToMerge)
                    {
                        var childClusters = await clusteringRepo.GetChildClustersWithSubClusterAsync(cluster.ClusterNumber);
                        allChildClusterNumbers.AddRange(childClusters.Select(c => c.ClusterNumber));
                    }

                    // 7. MongoDB에 새 병합 클러스터 생성
                    await progressForm.UpdateProgressHandler(60, "새 클러스터 생성 중");

                    var newCluster = new ClusteringResultDocument
                    {
                        ClusterNumber = newClusterNumber,
                        ClusterId = newClusterNumber, // 병합된 클러스터는 자신의 번호가 ClusterId
                        ClusterName = combinedClusterName,
                        Keywords = keywordSet.ToList(),
                        Count = totalCount,
                        TotalAmount = totalAmount,
                        DataIndices = dataIndicesSet.ToList(),
                        CreatedAt = DateTime.Now
                    };

                    // 새 클러스터 생성
                    string newClusterId = await clusteringRepo.CreateAsync(newCluster);

                    await progressForm.UpdateProgressHandler(70, "하위 클러스터 관계 업데이트 중");

                    // 8. 모든 하위 클러스터의 ClusterId를 새 클러스터 번호로 변경
                    foreach (int childNumber in allChildClusterNumbers)
                    {
                        await clusteringRepo.UpdateClusterIdAsync(childNumber, newClusterNumber);
                    }

                    await progressForm.UpdateProgressHandler(80, "기존 클러스터 삭제 중");

                    // 9. 병합 대상 상위 클러스터 삭제
                    foreach (var cluster in clustersToMerge)
                    {
                        await clusteringRepo.DeleteByClusterNumberAsync(cluster.ClusterNumber);
                    }

                    await progressForm.UpdateProgressHandler(85, "메모리 데이터 동기화 중");

                    // 10. DataTable 업데이트 (메모리 내 변경)

                    // 새 클러스터 행 추가
                    DataRow newRow = DataHandler.finalClusteringData.NewRow();
                    newRow["ID"] = newClusterNumber;
                    newRow["ClusterID"] = newClusterNumber;
                    if (DataHandler.finalClusteringData.Columns.Contains("ClusterSubID"))
                    {
                        newRow["ClusterSubID"] = -1;
                    }
                    newRow["클러스터명"] = combinedClusterName;
                    newRow["키워드목록"] = string.Join(",", keywordSet);
                    newRow["Count"] = totalCount;
                    newRow["합산금액"] = totalAmount;
                    newRow["dataIndex"] = string.Join(",", dataIndicesSet);
                    DataHandler.finalClusteringData.Rows.Add(newRow);

                    // 하위 클러스터들의 ClusterID 업데이트
                    foreach (DataRow row in DataHandler.finalClusteringData.Rows)
                    {
                        if (row["ClusterID"] != DBNull.Value)
                        {
                            int rowClusterId = Convert.ToInt32(row["ClusterID"]);
                            // 병합 대상 클러스터를 참조하는 행들의 ClusterID 변경
                            if (checkedClusterIds.Contains(rowClusterId))
                            {
                                row["ClusterID"] = newClusterNumber;
                            }
                        }
                    }

                    // 병합 대상 상위 클러스터 행 삭제
                    for (int i = DataHandler.finalClusteringData.Rows.Count - 1; i >= 0; i--)
                    {
                        DataRow row = DataHandler.finalClusteringData.Rows[i];
                        int rowId = Convert.ToInt32(row["ID"]);

                        // 병합 대상 클러스터 행 삭제
                        if (checkedClusterIds.Contains(rowId))
                        {
                            DataHandler.finalClusteringData.Rows.RemoveAt(i);
                        }
                    }

                    // 변경사항 적용
                    DataHandler.finalClusteringData.AcceptChanges();

                    await progressForm.UpdateProgressHandler(90, "데이터 새로고침 중");

                    // 데이터 다시 불러오기
                    mergeClusterDataTable = await EnrichWithRawTableDataAsync(DataHandler.finalClusteringData);

                    
                    create_merge_keyword_list(true);
                    create_check_keyword_list();

                    await progressForm.UpdateProgressHandler(100, "완료");
                    progressForm.Close();

                    MessageBox.Show("클러스터 병합이 완료되었습니다.", "Info",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터 병합 오류: {ex.Message}");
                MessageBox.Show($"클러스터 병합 중 오류가 발생했습니다: {ex.Message}", "오류",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
            }
        }

     
        private void button2_Click(object sender, EventArgs e)
        {
            ShowMergeClusterDetail();

        }

        private async void auto_cluster_btn_Click(object sender, EventArgs e)
        {
            try
            {
                // 현재 활성화된 탭 확인
                TabPage activeTab = tabControl1.SelectedTab;
                DataGridView targetDataGridView = null;
                List<string> searchKeywordList = null;
                bool isSupplierSearch = false;

                // 활성화된 탭에 따라 대상 DataGridView와 검색 리스트 결정
                if (activeTab.Name == "tabPage1" || activeTab.Controls.Contains(dataGridView_modified))
                {
                    targetDataGridView = dataGridView_modified;
                    searchKeywordList = merge_keyword_list;
                    isSupplierSearch = false;
                }
                else if (activeTab.Name == "tabPage2" || activeTab.Controls.Contains(dataGridView_supply_summary))
                {
                    targetDataGridView = dataGridView_supply_summary;
                    searchKeywordList = supplier_keyword_list;
                    isSupplierSearch = true;
                }
                else
                {
                    MessageBox.Show("활성화된 탭에서 자동 클러스터링을 지원하지 않습니다.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 체크박스가 선택된 항목들 수집
                List<string> selectedKeywords = new List<string>();
                foreach (DataGridViewRow row in targetDataGridView.Rows)
                {
                    if (row.Cells[0].Value != null && Convert.ToBoolean(row.Cells[0].Value) == true)
                    {
                        // 키워드 컬럼에서 값 추출 (0번은 체크박스이므로 1번 컬럼)
                        if (row.Cells.Count > 1 && row.Cells[1].Value != null)
                        {
                            selectedKeywords.Add(row.Cells[1].Value.ToString());
                        }
                    }
                }

                if (selectedKeywords.Count == 0)
                {
                    MessageBox.Show("자동 클러스터링할 항목을 선택해주세요.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                DialogResult result = MessageBox.Show(
                    $"선택된 {selectedKeywords.Count}개 항목을 개별적으로 자동 클러스터링하시겠습니까?",
                    "자동 클러스터링 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    using (var progressForm = new ProcessProgressForm())
                    {
                        progressForm.Show();
                        await progressForm.UpdateProgressHandler(10, "자동 클러스터링 시작");
                        await Task.Delay(10);

                        int totalItems = selectedKeywords.Count;
                        int processedItems = 0;

                        // 각 선택된 키워드에 대해 개별적으로 병합 수행
                        foreach (string keyword in selectedKeywords)
                        {
                            try
                            {
                                await progressForm.UpdateProgressHandler(
                                    10 + (processedItems * 70 / totalItems),
                                    $"클러스터링 중: {keyword} ({processedItems + 1}/{totalItems})"
                                );


                                // 수정된 코드 (ClusteringManager 사용)
                                List<string> matchingPairs;
                                string searchColumnName = isSupplierSearch ? DataHandler.prod_col_name : "키워드목록";

                                // ClusteringManager를 통한 정확 매칭 검색
                                matchingPairs = _clusteringManager.SearchExact(searchColumnName, keyword);

                                if (matchingPairs.Count > 0)
                                {
                                    // 검색 조건 생성
                                    var searchCriteria = new SearchCriteria
                                    {
                                        Keywords = matchingPairs,
                                        ExcludeKeywords = null,
                                        IsSupplierSearch = isSupplierSearch,
                                        ExactMatch = true,
                                        AndSearch = false
                                    };

                                    // ClusteringManager를 사용하여 검색 수행
                                    await _clusteringManager.SearchAsync(searchCriteria);

                                    // 검색 결과에서 cluster_id == -1인 항목만 필터링하여 병합 대상 수집
                                    var currentResultIds = _clusteringManager.GetCurrentResultClusterIds();
                                    List<int> validClusterIds = new List<int>();

                                    foreach (int clusterId in currentResultIds)
                                    {
                                        // DataHandler.finalClusteringData에서 해당 클러스터의 상태 확인
                                        var clusterRow = DataHandler.finalClusteringData.AsEnumerable()
                                            .FirstOrDefault(row => Convert.ToInt32(row["ID"]) == clusterId);

                                        if (clusterRow != null)
                                        {
                                            int clusterIdValue = Convert.ToInt32(clusterRow["ClusterID"]);
                                            // cluster_id == -1인 미병합 상태인 경우만 추가
                                            if (clusterIdValue == -1)
                                            {
                                                validClusterIds.Add(clusterId);
                                            }
                                        }
                                    }

                                    await MergeAndCreateNewCluster(DataHandler.finalClusteringData, validClusterIds, keyword);
                                    Debug.WriteLine($"키워드 '{keyword}'로 {validClusterIds.Count}개 클러스터 병합 완료");
                                }
                                else
                                {
                                    Debug.WriteLine($"키워드 '{keyword}': 매칭되는 클러스터 없음");
                                }

                                processedItems++;
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"키워드 '{keyword}' 처리 중 오류: {ex.Message}");
                                // 개별 키워드 처리 실패해도 계속 진행
                                processedItems++;
                            }
                        }

                        await progressForm.UpdateProgressHandler(80, "데이터 새로고침 중...");
                        await Task.Delay(10);

                        // 병합 작업 후 데이터 새로고침
                        
                        await create_merge_keyword_list(true);
                        create_check_keyword_list();
                        UpdateModifiedDataGridView();

                        await progressForm.UpdateProgressHandler(100, "자동 클러스터링 완료");
                        await Task.Delay(10);
                    }

                    MessageBox.Show($"자동 클러스터링이 완료되었습니다.\n처리된 항목: {selectedKeywords.Count}개", "완료",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 체크박스 선택 상태 초기화
                    foreach (DataGridViewRow row in targetDataGridView.Rows)
                    {
                        if (row.Cells[0].Value != null)
                        {
                            row.Cells[0].Value = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"자동 클러스터링 중 오류: {ex.Message}");
                MessageBox.Show($"자동 클러스터링 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void column_search_combo_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (column_search_combo.SelectedIndex < 0) return;

                string selectedDisplayName = column_search_combo.SelectedItem.ToString();
                string selectedColumnName = _clusteringManager.ConvertDisplayNameToColumnName(selectedDisplayName);

                Debug.WriteLine($"컬럼 선택 변경: {selectedDisplayName} -> {selectedColumnName}");

                

                // 검색 컬럼 변경 시 기존 검색 결과 유지하거나 초기화 선택
                // 여기서는 기존 검색 결과를 유지하는 방식으로 구현
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"컬럼 선택 이벤트 처리 오류: {ex.Message}");
            }
        }

        private void sub_search_checkbox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (sub_search_checkbox.Checked)
                {
                    // 현재 검색 결과를 기준으로 저장
                    _baseSearchResults = _clusteringManager.GetCurrentResultClusterIds();
                    _isSubSearchMode = true;

                    sub_search_info_label.Text = $"검색 결과 내 재검색 ({_baseSearchResults.Count}개 결과 저장됨)";
                    Debug.WriteLine($"검색 내 검색 모드 활성화: {_baseSearchResults.Count}개 결과 저장");
                }
                else
                {
                    // 검색 내 검색 모드 해제
                    _isSubSearchMode = false;
                    _baseSearchResults.Clear();
                    sub_search_info_label.Text = "";
                    Debug.WriteLine("검색 내 검색 모드 비활성화");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"검색 내 검색 체크박스 변경 오류: {ex.Message}");
            }
        }

    }
}
