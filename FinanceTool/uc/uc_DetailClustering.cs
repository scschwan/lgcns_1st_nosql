using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using FinanceTool;
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
    public partial class uc_DetailClustering : UserControl
    {

        DataTable mergeClusterDataTable;

        // *** 세부 클러스터링 전용 데이터 변수 (전역 변수 대신 사용) ***
        private DataTable _detailClusteringData;

        private decimal decimalDivider = 1;
        private string decimalDividerName = "원";
        private string selectecLv1Name = "";
        private bool equalsSearchYN = false;
        private bool andSearchYN = false;

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

        // 새로 추가: 현재 선택된 검색 컬럼
        private string _currentSearchColumn = "키워드";

        // 세부 클러스터링 전용 변수들
        private int _parentClusterId = -1;
        private string _parentClusterName = "";

        // === 페이징 초기화 메서드 ===


        // === 페이징 컨트롤 활성화/비활성화 ===
        private void EnablePaginationControlsMerge(bool enabled)
        {
            btn_prevPage.Enabled = enabled;
            btn_nextPage.Enabled = enabled;
            num_pageNumber.Enabled = enabled;
            cmb_pageSize.Enabled = enabled;
        }


        // === 선택 상태 관리 메서드들 ===
        /// <summary>
        /// 현재 필터 결과의 전체 선택 상태 관리
        /// </summary>

        private HashSet<int> GetCurrentFilterClusterIds()
        {
            return _clusteringManager.GetCurrentResultClusterIds().ToHashSet();
        }

        private void UpdateMergeAllCheckState()
        {
            isCheckedTableObject = true;


            try
            {
                // 현재 검색 결과의 모든 ID 수집
                var currentFilterIds = GetCurrentFilterClusterIds();
                var selectedIds = _clusteringManager.GetSelectedClusterIds();

                // 현재 필터의 모든 항목이 선택되었는지 확인
                bool allSelected = currentFilterIds.Count > 0 &&
                                  currentFilterIds.All(id => selectedIds.Contains(id));

                // 이벤트 핸들러 제거 후 설정 (재귀 호출 방지)
                merge_all_check.CheckedChanged -= merge_all_check_CheckedChanged;
                merge_all_check.Checked = allSelected;
                merge_all_check.CheckedChanged += merge_all_check_CheckedChanged;

                _allSelectedInCurrentFilter = allSelected;

                Debug.WriteLine($"전체 선택 상태 업데이트: {allSelected}, 필터 항목: {currentFilterIds.Count}개, 선택 항목: {selectedIds.Count}개");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"전체 선택 상태 업데이트 오류: {ex.Message}");
            }
            finally
            {
                isCheckedTableObject = false;
            }
        }

        // === 수정된 기존 메서드들 ===


        // 전역 인스턴스 생성
        private static RecomandKeywordManager _recomandKeywordManager;

        /// <summary>
        /// 시스템 성능 극대화 설정 클래스
        /// 192GB RAM과 16코어 CPU 환경에 최적화
        /// </summary>
        public static class SystemPerformanceOptimizer
        {
            private static bool _isOptimized = false;
            private static readonly object _optimizationLock = new object();

            /// <summary>
            /// 시스템 성능 최적화 적용 (한 번만 실행)
            /// </summary>
            public static void OptimizeSystemForUltraSpeed()
            {
                if (_isOptimized) return;

                lock (_optimizationLock)
                {
                    if (_isOptimized) return;

                    try
                    {
                        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 시스템 성능 최적화 시작");

                        // 1. GC 설정 최적화 (192GB RAM 활용)
                        OptimizeGarbageCollection();

                        // 2. 스레드 풀 최적화 (16코어 CPU 활용)
                        OptimizeThreadPool();

                        // 3. .NET 런타임 최적화
                        OptimizeDotNetRuntime();

                        // 4. 메모리 할당 최적화
                        OptimizeMemoryAllocation();

                        _isOptimized = true;
                        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 시스템 성능 최적화 완료");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 시스템 성능 최적화 오류: {ex.Message}");
                    }
                }
            }

            /// <summary>
            /// GC 최적화 (192GB RAM 환경)
            /// </summary>
            private static void OptimizeGarbageCollection()
            {
                try
                {
                    // Server GC 모드 확인 및 설정
                    if (!GCSettings.IsServerGC)
                    {
                        Debug.WriteLine("경고: Server GC가 활성화되지 않음. app.config에 추가 권장:");
                        Debug.WriteLine("<gcServer enabled=\"true\"/>");
                    }

                    // 대용량 메모리 환경을 위한 GC 지연 모드 설정
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

                    // 메모리 압박 임계값 조정 (192GB의 80% 활용)
                    long targetMemoryBytes = 192L * 1024 * 1024 * 1024 * 80 / 100; // 153GB

                    Debug.WriteLine($"GC 최적화 완료 - 목표 메모리: {targetMemoryBytes / 1024 / 1024 / 1024}GB");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"GC 최적화 오류: {ex.Message}");
                }
            }

            /// <summary>
            /// 스레드 풀 최적화 (16코어 CPU 환경)
            /// </summary>
            private static void OptimizeThreadPool()
            {
                try
                {
                    int coreCount = Environment.ProcessorCount; // 16 cores

                    // 작업자 스레드 최적화 (코어당 8-16개 스레드)
                    int minWorkerThreads = coreCount * 8;   // 128개
                    int maxWorkerThreads = coreCount * 16;  // 256개

                    // I/O 완료 포트 스레드 최적화
                    int minCompletionPortThreads = coreCount * 4;  // 64개
                    int maxCompletionPortThreads = coreCount * 8;  // 128개

                    // 최소 스레드 수 설정
                    ThreadPool.SetMinThreads(minWorkerThreads, minCompletionPortThreads);

                    // 최대 스레드 수 설정
                    ThreadPool.SetMaxThreads(maxWorkerThreads, maxCompletionPortThreads);

                    Debug.WriteLine($"스레드 풀 최적화 완료 - Worker: {minWorkerThreads}-{maxWorkerThreads}, " +
                                   $"IOCP: {minCompletionPortThreads}-{maxCompletionPortThreads}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"스레드 풀 최적화 오류: {ex.Message}");
                }
            }

            /// <summary>
            /// .NET 런타임 최적화
            /// </summary>
            private static void OptimizeDotNetRuntime()
            {
                try
                {
                    // JIT 컴파일러 최적화
                    System.Runtime.ProfileOptimization.SetProfileRoot(Path.GetTempPath());
                    System.Runtime.ProfileOptimization.StartProfile("FinanceToolOptimization.prof");

                    Debug.WriteLine(".NET 런타임 최적화 완료");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($".NET 런타임 최적화 오류: {ex.Message}");
                }
            }

            /// <summary>
            /// 메모리 할당 최적화
            /// </summary>
            private static void OptimizeMemoryAllocation()
            {
                try
                {
                    // 대용량 객체를 위한 사전 할당
                    var dummy = new byte[85000]; // LOH 임계값 초과
                    dummy = null;

                    // GC를 한 번 실행하여 초기화
                    GC.Collect(2, GCCollectionMode.Optimized);
                    GC.WaitForPendingFinalizers();

                    Debug.WriteLine("메모리 할당 최적화 완료");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"메모리 할당 최적화 오류: {ex.Message}");
                }
            }

           
        }

       

        public uc_DetailClustering()
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

                //세부 목록 재조회
                create_check_keyword_list();

                // 병합 작업 후 UI 업데이트
                UpdateModifiedDataGridView();
                UpdateSupplySummaryDataGridView();

                Debug.WriteLine($"컨텍스트 메뉴: 행 {rightClickedRow.Index}를 선택하고 세부정보 표시");
            };

            contextMenu.Items.Add(viewDetailsItem);
            merge_check_table.ContextMenuStrip = contextMenu;
        }

        public async void initUI(int parentClusterId, string parentClusterName)
        {
            try
            {
                _parentClusterId = parentClusterId;
                _parentClusterName = parentClusterName;

                Debug.WriteLine($"세부 클러스터링 초기화: 부모 클러스터 {parentClusterId} ({parentClusterName})");

                // *** 1. MongoDB에서 세부 클러스터링 데이터 로드 ***
                var clusteringRepo = new ClusteringRepository();
                DataTable mongoClusterData = await clusteringRepo.GetDetailClustersAsDataTableAsync(_parentClusterId);

                if (mongoClusterData != null && mongoClusterData.Rows.Count > 0)
                {
                    // *** 로컬 변수에 할당 (전역 변수 건드리지 않음) ***
                    _detailClusteringData = mongoClusterData.Copy();
                    Debug.WriteLine($"세부 클러스터링 데이터 {mongoClusterData.Rows.Count}개 로드");
                }
                else
                {
                    Debug.WriteLine("세부 클러스터링 데이터가 없습니다.");
                    MessageBox.Show($"'{parentClusterName}' 클러스터에 속한 데이터가 없습니다.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                   
                    return;
                }

                // *** 추가: 부모 클러스터 데이터 제거 ***
                var rowsToRemove = _detailClusteringData.AsEnumerable()
                    .Where(row => Convert.ToInt32(row["ID"]) == _parentClusterId)
                    .ToList();

                foreach (var row in rowsToRemove)
                {
                    _detailClusteringData.Rows.Remove(row);
                }

                // *** 2. RawData 정보로 보강 (로컬 데이터 사용) ***
                mergeClusterDataTable = await EnrichWithRawTableDataAsync(_detailClusteringData);

                // *** 3. ClusteringManager 초기화 (로컬 데이터 사용) ***
                _clusteringManager = new ClusteringManager();
                await _clusteringManager.InitializeAsync(mergeClusterDataTable, merge_cluster_table,
                    num_pageNumber, cmb_pageSize, btn_prevPage, btn_nextPage, lbl_pagination2, merge_all_check,true);

                // *** 4. 검색 UI 초기화 ***
                InitializeSearchUI();

                // *** 5. 초기 전체 검색 수행 ***
                await PerformInitialSearch();

                // *** 6. 나머지 UI 초기화 ***
                await InitializeRemainingUI();


                // 병합 클러스터 리스트 생성
                create_check_keyword_list();

                // 병합 작업 후 UI 업데이트
                UpdateModifiedDataGridView();
                UpdateSupplySummaryDataGridView();

                Debug.WriteLine("세부 클러스터링 초기화 완료");


                // *** 컬럼 정보 전체 출력 ***
                Debug.WriteLine($"[CreateCheckDataGridView] DataHandler.finalClusteringData 총 행 수: {DataHandler.finalClusteringData.Rows.Count}");
                Debug.WriteLine($"[CreateCheckDataGridView] DataHandler.finalClusteringData 총 컬럼 수: {DataHandler.finalClusteringData.Columns.Count}");
                for (int i = 0; i < DataHandler.finalClusteringData.Columns.Count; i++)
                {
                    Debug.WriteLine($"  컬럼 {i}: Name='{DataHandler.finalClusteringData.Columns[i].ColumnName}'" +
                        $", DataType='{DataHandler.finalClusteringData.Columns[i].DataType}'");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"initUI 메서드 오류: {ex.Message}");
                MessageBox.Show($"클러스터링 데이터 로드 중 오류가 발생했습니다.\n{ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 검색 UI 초기화 (새로 추가)
        /// </summary>
        private void InitializeSearchUI()
        {
            // 1. 검색 컬럼 콤보박스 초기화
            column_search_combo.Items.Clear();

            // ClusteringManager에서 검색 가능한 컬럼 정보 가져오기
            var searchableColumns = _clusteringManager.GetSearchableColumns();

            //column_search_combo.Items.Add("컬럼 선택");
            foreach (var column in searchableColumns)
            {
                column_search_combo.Items.Add(column.Value); // 표시명 (키워드, 공급업체, 타겟, 계정, 코스트센터)
            }

            // *** 수정: 첫 번째 항목(인덱스 0)을 기본 선택으로 설정 ***
            if (column_search_combo.Items.Count > 0)
            {
                column_search_combo.SelectedIndex = 0;
            }

            // 2. 검색 내 검색 체크박스 초기화
            sub_search_checkbox.Checked = false;
            sub_search_checkbox.Text = "결과 내 재검색";

            
            Debug.WriteLine("검색 UI 초기화 완료");
        }


        // *** 새로 추가: 초기 전체 검색 메서드 ***
        private async Task PerformInitialSearch()
        {
            try
            {
                // 전체 검색 조건으로 초기 검색 실행
                var searchCriteria = new SearchCriteria
                {
                    Keywords = new List<string>(), // 빈 키워드 = 전체 검색
                    ExcludeKeywords = null,
                    ExactMatch = false,
                    AndSearch = false
                };

                await _clusteringManager.SearchAsync(searchCriteria ,true);

                // 선택 상태 초기화
                merge_all_check.Checked = false;
                change_row_count();

                Debug.WriteLine("초기 전체 검색 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"초기 검색 실행 오류: {ex.Message}");
            }
        }

        // *** 새로 추가: 나머지 UI 초기화 작업들 (기존 코드 모두 포함) ***
        private async Task InitializeRemainingUI()
        {
            await Task.Run(() =>
            {
                if (Application.OpenForms.Count > 0)
                {
                    Application.OpenForms[0].Invoke((MethodInvoker)delegate
                    {
                        // merge_check_table 초기화
                        merge_check_table.DataSource = null;
                        merge_check_table.Rows.Clear();
                        merge_check_table.Columns.Clear();
                        if (DataHandler.dragSelections.ContainsKey(merge_check_table))
                        {
                            DataHandler.dragSelections[merge_check_table].Clear();
                        }

                        Debug.WriteLine("RegisterDataGridView->match_keyword_table");
                        DataHandler.RegisterDataGridView(merge_cluster_table);
                        DataHandler.RegisterDataGridView(dataGridView_lv1);
                        DataHandler.RegisterDataGridView(dataGridView_recoman_keyword);


                        // 공급업체별 요약 테이블 초기화
                        InitializeSupplySummaryTable();

                        // *** 1번 기능: dataGridView_modified에 체크박스 추가 및 등록 ***
                        DataHandler.RegisterDataGridView(dataGridView_modified);
                        DataHandler.RegisterDataGridView(dataGridView_supply_summary);



                        Debug.WriteLine("RegisterDataGridView->complete");

                        // 이벤트 핸들러 중복 등록 방지
                        decimal_combo.SelectedIndexChanged -= decimal_combo_SelectedIndexChanged;
                        decimal_combo.SelectedIndex = 0;
                        decimal_combo.SelectedIndexChanged += decimal_combo_SelectedIndexChanged;

                        // sorting 기준 변환
                        merge_cluster_table.SortCompare -= DataHandler.money_SortCompare;
                        merge_check_table.SortCompare -= DataHandler.money_SortCompare;
                        dataGridView_modified.SortCompare -= DataHandler.money_SortCompare;

                        merge_cluster_table.SortCompare += DataHandler.money_SortCompare;
                        merge_check_table.SortCompare += DataHandler.money_SortCompare;
                        dataGridView_modified.SortCompare += DataHandler.money_SortCompare;

                        dataGridView_modified.CellClick -= dataGridView_keyword_CellClick;


                        dataGridView_supply_summary.SortCompare -= DataHandler.money_SortCompare;
                        dataGridView_supply_summary.SortCompare += DataHandler.money_SortCompare;
                        dataGridView_supply_summary.CellClick -= dataGridView_supply_summary_CellClick;
                        dataGridView_supply_summary.CellClick += dataGridView_supply_summary_CellClick;

                        if (dataGridView_modified.Rows.Count > 0)
                        {
                            dataGridView_modified.DataSource = null;
                            dataGridView_modified.Rows.Clear();
                            dataGridView_modified.Columns.Clear();
                        }

                        Debug.WriteLine("클러스터링 매니저 초기화 완료");

                        // 초기 데이터 로드 후 업데이트
                        UpdateModifiedDataGridView();

                        // *** 2번 기능: 공급업체별 요약 데이터 업데이트 ***
                        UpdateSupplySummaryDataGridView();

                        // DataGridView 속성 설정
                        dataGridView_modified.AllowUserToAddRows = false;
                        dataGridView_modified.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                        dataGridView_modified.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                        dataGridView_modified.Font = new System.Drawing.Font("Pretendard", 14.25F);

                        // 나머지 컬럼들은 읽기 전용으로 설정
                        for (int i = 2; i < dataGridView_modified.Columns.Count; i++)
                        {
                            dataGridView_modified.Columns[i].ReadOnly = true;
                        }

                        dataGridView_modified.CellClick += dataGridView_keyword_CellClick;
                        dataGridView_modified.SortCompare += DataHandler.money_SortCompare;


                        dataGridView_supply_summary.SortCompare += DataHandler.money_SortCompare;
                        dataGridView_supply_summary.CellClick += dataGridView_supply_summary_CellClick;

                        Debug.WriteLine("LoadSeparatorsAndRemovers");
                        LoadSeparatorsAndRemovers();

                        EnablePaginationControlsMerge(true);
                    });
                }
            });
        }

        // 3. 공급업체별 요약 테이블 초기화 메서드
        private void InitializeSupplySummaryTable()
        {
            try
            {
                if (dataGridView_supply_summary == null) return;

                //2025.07.21
                //공급업체열이 필수가 아니면 로직 종료
                if (!DataHandler.prod_col_yn)
                {
                    // DataGridView 초기화
                    dataGridView_supply_summary.DataSource = null;
                    dataGridView_supply_summary.Rows.Clear();
                    dataGridView_supply_summary.Columns.Clear();
                    return;
                }

                // DataGridView 초기화
                dataGridView_supply_summary.DataSource = null;
                dataGridView_supply_summary.Rows.Clear();
                dataGridView_supply_summary.Columns.Clear();

                if (DataHandler.dragSelections.ContainsKey(dataGridView_supply_summary))
                {
                    DataHandler.dragSelections[dataGridView_supply_summary].Clear();
                }

                // DataGridView 속성 설정
                dataGridView_supply_summary.AllowUserToAddRows = false;
                dataGridView_supply_summary.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGridView_supply_summary.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dataGridView_supply_summary.Font = new System.Drawing.Font("Pretendard", 14.25F);

                // 나머지 컬럼들은 읽기 전용으로 설정
                for (int i = 2; i < dataGridView_supply_summary.Columns.Count; i++)
                {
                    dataGridView_supply_summary.Columns[i].ReadOnly = true;
                }

                Debug.WriteLine("공급업체별 요약 테이블 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"공급업체별 요약 테이블 초기화 오류: {ex.Message}");
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


        /// <summary>
        /// 현재 UI 상태에서 SearchCriteria 생성
        /// </summary>
        private SearchCriteria CreateSearchCriteriaFromCurrentUI()
        {
            try
            {
                string targetKeyword = merge_search_keyword.Text?.Trim() ?? "";

                // 검색어가 없으면 전체 검색
                if (string.IsNullOrEmpty(targetKeyword))
                {
                    if (_isSubSearchMode && _baseSearchResults.Count > 0)
                    {
                        // 결과 내 재검색 모드: 이전 검색 결과만 표시
                        return new SearchCriteria
                        {
                            Keywords = new List<string>(),
                            ExcludeKeywords = GetExcludeKeywords(),
                            ExactMatch = equalsSearchYN,
                            AndSearch = andSearchYN,
                            IsSubSearchMode = true,
                            BaseSearchResults = _baseSearchResults
                        };
                    }
                    else
                    {
                        // 일반 모드: 전체 데이터 검색
                        return new SearchCriteria
                        {
                            Keywords = new List<string>(),
                            ExcludeKeywords = GetExcludeKeywords(),
                            ExactMatch = equalsSearchYN,
                            AndSearch = andSearchYN,
                            IsFullSearch = true
                        };
                    }
                }

                // 현재 선택된 컬럼 확인
                string currentColumn = GetSelectedSearchColumn();
                Debug.WriteLine($"검색 컬럼: {currentColumn}, 키워드: {targetKeyword}");

                // ClusteringManager를 통한 검색 실행
                List<string> matchingKeywords;
                if (equalsSearchYN)
                {
                    matchingKeywords = _clusteringManager.SearchExact(currentColumn, targetKeyword);
                }
                else
                {
                    matchingKeywords = _clusteringManager.SearchContains(currentColumn, targetKeyword);
                }

                Debug.WriteLine($"매칭된 키워드: {matchingKeywords.Count}개");

                // 다중 컬럼 검색 조건 구성
                var columnCriteria = new Dictionary<string, SearchColumnCriteria>();

                if (matchingKeywords.Count > 0)
                {
                    columnCriteria[currentColumn] = new SearchColumnCriteria
                    {
                        Keywords = matchingKeywords,
                        ExactMatch = true, // 이미 매칭된 키워드들이므로 정확 매칭
                        UseAnd = andSearchYN
                    };
                }

                return new SearchCriteria
                {
                    ColumnCriteria = columnCriteria,
                    IsMultiColumnSearch = true,
                    ExcludeKeywords = GetExcludeKeywords(),
                    ExactMatch = equalsSearchYN,
                    AndSearch = andSearchYN
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"검색 조건 생성 오류: {ex.Message}");
                return new SearchCriteria();
            }
        }

        /// <summary>
        /// 제외 키워드 목록 추출
        /// </summary>
        private List<string> GetExcludeKeywords()
        {
            if (string.IsNullOrEmpty(except_keyword.Text))
        return new List<string>();

    var excludeKeywords = except_keyword.Text.Split(',')
                                          .Select(k => k.Trim())
                                          .Where(k => !string.IsNullOrEmpty(k))
                                          .ToList();

    Debug.WriteLine($"제외 키워드 {excludeKeywords.Count}개: {string.Join(", ", excludeKeywords)}");
    return excludeKeywords;
        }

        private void LoadSeparatorsAndRemovers()
        {
            // 프로그램 시작 시 로드
            _recomandKeywordManager = new RecomandKeywordManager();

            Debug.WriteLine("_recomandKeywordManager init complete");

            // 데이터 가져오기 및 중복 제거
            List<string> lv1_list = _recomandKeywordManager.Lv1List
                .Distinct()  // 중복 제거
                .ToList();   // List로 변환



            //구분자 리스트 추가
            create_keyword_table(dataGridView_lv1, lv1_list);


        }

        private void create_keyword_table(DataGridView dgv, List<string> data_list, bool lv1yn = true)
        {
            Debug.WriteLine("lv1 table init start");
            // DataGridView 초기화
            dgv.DataSource = null;
            dgv.Rows.Clear();
            dgv.Columns.Clear();
            if (DataHandler.dragSelections.ContainsKey(dgv))
            {
                DataHandler.dragSelections[dgv].Clear();
            }

            // 체크박스 컬럼 추가
            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn
            {
                Name = "CheckBox",
                HeaderText = "",
                Width = 50,
                ThreeState = false,
                //Frozen = true,
                FillWeight = 20
            };
            dgv.Columns.Add(checkColumn);

            // 데이터 컬럼 추가
            DataGridViewTextBoxColumn dataColumn = new DataGridViewTextBoxColumn
            {
                Name = "Data",
                HeaderText = "데이터"
            };
            dgv.Columns.Add(dataColumn);

            // 데이터 리스트의 각 항목을 행으로 추가
            foreach (string data in data_list)
            {
                int rowIndex = dgv.Rows.Add();
                dgv.Rows[rowIndex].Cells["CheckBox"].Value = false;
                dgv.Rows[rowIndex].Cells["Data"].Value = data;
            }

            Debug.WriteLine("lv1 table init presrsaa");

            dgv.AllowUserToAddRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.Columns["Data"].ReadOnly = true;  // 체크박스 컬럼만 편집 가능
            dgv.Columns["CheckBox"].ReadOnly = false;  // 체크박스 컬럼만 편집 가능
            dgv.Font = new System.Drawing.Font("Pretendard", 14.25F);

            Debug.WriteLine("lv1 table init complete");

            if (lv1yn)
            {
                dgv.CellClick += dataGridView_lv1_CellClick;
            }
            else
            {
                dgv.CellClick += dataGridView_keyword_CellClick;
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
                        await _clusteringManager.SearchMultipleColumnsAsync(columnCriteria, excludeKeywords,true);

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
                        await _clusteringManager.SearchMultipleColumnsAsync(columnCriteria, excludeKeywords , true);

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

        /// <summary>
        /// 빈 검색 결과 표시
        /// </summary>
        private async Task ShowEmptySearchResult()
        {
            try
            {
                // 빈 결과 표시
                await _clusteringManager.DisplaySpecificClustersAsync(new List<int>());

                // 페이징 컨트롤 비활성화
                EnablePaginationControlsMerge(false);

                change_row_count();

                Debug.WriteLine("빈 검색 결과 표시 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"빈 검색 결과 표시 오류: {ex.Message}");
            }
        }


        public async Task<DataTable> EnrichWithRawTableDataAsync(DataTable inputTable)
        {
            DataTable resultTable = inputTable.Copy();
            Debug.WriteLine("EnrichWithRawTableDataAsync start!!");
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
                //var filter = Builders<RawDataDocument>.Filter.In(d => d.Id, rawDataIds.ToList());
                //var rawDataDocuments = await rawDataRepo.FindDocumentsAsync(filter);

                //2025.05.29
                //대용량 batch 처리
                // 수정된 코드
                const int batchSize = 10000;
                var allRawDataDocuments = new List<RawDataDocument>();
                var rawDataIdsList = rawDataIds.ToList();

                // 배치별로 분할하여 조회
                for (int i = 0; i < rawDataIdsList.Count; i += batchSize)
                {
                    var batchIds = rawDataIdsList.Skip(i).Take(batchSize).ToList();
                    var filter = Builders<RawDataDocument>.Filter.In(d => d.Id, batchIds);
                    var batchDocuments = await rawDataRepo.FindDocumentsAsync(filter);
                    allRawDataDocuments.AddRange(batchDocuments);
                }

                var rawDataDocuments = allRawDataDocuments;

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
                Debug.WriteLine("EnrichWithRawTableDataAsync end!!");
                Debug.WriteLine($"raw_data 문서 {rawDataDocuments.Count}개로 클러스터링 데이터를 보강했습니다.");
                return resultTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RAW_TABLE 데이터 추가 중 오류 발생: {ex.Message}");
                return resultTable;
            }
        }


        private async Task create_merge_keyword_list(bool isAlreadyProgress = false)
        {
            if (isAlreadyProgress)
            {
                string target_keyword = "";

                if (!"".Equals(merge_search_keyword.Text.ToString()) && merge_search_keyword.Text != null)
                {
                    target_keyword = merge_search_keyword.Text.ToString();
                }

                // 검색어가 없으면 전체 검색
                if (string.IsNullOrEmpty(target_keyword))
                {
                    await PerformSearchWithCriteria(new Dictionary<string, SearchColumnCriteria>(), isAlreadyProgress);
                    return;
                }



                // 키워드 파싱
                var parsedKeywords = ParseComplexKeywords(target_keyword);
                Debug.WriteLine($"파싱 결과 - AND: [{string.Join(", ", parsedKeywords.AndKeywords)}], OR: [{string.Join(", ", parsedKeywords.OrKeywords)}]");

                // *** 핵심 변경: 기존 DataHandler 함수 대신 ClusteringManager 사용 ***
                string searchColumn = GetSelectedSearchColumn();
                //var matchingClusterIds = _clusteringManager.SearchWithComplexConditions(searchColumn, parsedKeywords, equalsSearchYN ,true );
                var matchingClusterIds = _clusteringManager.SearchWithComplexConditions(searchColumn, parsedKeywords, equalsSearchYN,_baseSearchResults,_isSubSearchMode, true);



                if (matchingClusterIds.Count > 0)
                {
                    // PerformSearchWithCriteria 우회하고 직접 결과 표시
                    await _clusteringManager.DisplaySpecificClustersAsync(matchingClusterIds);

                    // 선택 상태 초기화
                    merge_all_check.Checked = false;
                    change_row_count();
                    Debug.WriteLine($"복합 조건 검색 완료: {matchingClusterIds.Count}개 클러스터 표시");
                }
                else
                {
                    await ShowEmptySearchResult();
                    Debug.WriteLine("검색 결과 없음 - 빈 테이블 표시");
                }

                /*
                 * 
                List<string> matchingKeywords;
                if (equalsSearchYN)
                {
                    // 완전일치 검색 - ClusteringManager 사용
                    matchingKeywords = _clusteringManager.SearchExact(searchColumn, target_keyword);
                    Debug.WriteLine($"완전일치 검색 결과: {matchingKeywords.Count}개 키워드");
                }
                else
                {
                    // 부분일치 검색 - ClusteringManager 사용
                    matchingKeywords = _clusteringManager.SearchContains(searchColumn, target_keyword);
                    Debug.WriteLine($"부분일치 검색 결과: {matchingKeywords.Count}개 키워드");
                }

                // AND/OR 검색 조건 파싱
                bool useAndSearch = andSearchYN;

                Debug.WriteLine($"검색 실행 - 컬럼: {searchColumn}, 키워드: {target_keyword}, 완전일치: {equalsSearchYN}, AND: {useAndSearch}");

                // 다중 컬럼 검색 조건 구성
                var columnCriteria = new Dictionary<string, SearchColumnCriteria>();
               
                if (matchingKeywords.Count > 0)
                {
                    columnCriteria[searchColumn] = new SearchColumnCriteria
                    {
                        Keywords = matchingKeywords,
                        ExactMatch = true, // 이미 매칭된 키워드들이므로 정확 매칭
                        UseAnd = useAndSearch
                    };

                    await PerformSearchWithCriteria(columnCriteria, isAlreadyProgress);
                } 
                else
                {
                    // *** 수정: 검색 결과가 없을 때 빈 테이블 표시 ***
                    await ShowEmptySearchResult();
                    Debug.WriteLine("검색 결과 없음 - 빈 테이블 표시");
                }
                 */

            }
            else
            {
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();

                    string target_keyword = "";

                    if (!"".Equals(merge_search_keyword.Text.ToString()) && merge_search_keyword.Text != null)
                    {
                        target_keyword = merge_search_keyword.Text.ToString();
                    }

                    await progressForm.UpdateProgressHandler(10, "데이터 검색 시작");
                    await Task.Delay(10);

                    // 검색어가 없으면 전체 검색
                    if (string.IsNullOrEmpty(target_keyword))
                    {
                        await progressForm.UpdateProgressHandler(40, "전체 데이터 검색 중...");
                        await Task.Delay(10);

                        await PerformSearchWithCriteria(new Dictionary<string, SearchColumnCriteria>(), isAlreadyProgress);

                        await progressForm.UpdateProgressHandler(100, "전체 데이터 검색 완료");
                        await Task.Delay(10);
                        progressForm.Close();
                        return;
                    }

                    // *** 핵심 변경: 기존 DataHandler 함수 대신 ClusteringManager 사용 ***

                    // 키워드 파싱
                    var parsedKeywords = ParseComplexKeywords(target_keyword);
                    Debug.WriteLine($"파싱 결과 - AND: [{string.Join(", ", parsedKeywords.AndKeywords)}], OR: [{string.Join(", ", parsedKeywords.OrKeywords)}]");


                    // *** 핵심 변경: 기존 DataHandler 함수 대신 ClusteringManager 사용 ***
                    string searchColumn = GetSelectedSearchColumn();

                    await progressForm.UpdateProgressHandler(20, $"'{searchColumn}' 컬럼에서 검색 중...");
                    await Task.Delay(10);

                    //var matchingClusterIds = _clusteringManager.SearchWithComplexConditions(searchColumn, parsedKeywords, equalsSearchYN ,true);
                    var matchingClusterIds = _clusteringManager.SearchWithComplexConditions(searchColumn, parsedKeywords, equalsSearchYN, _baseSearchResults, _isSubSearchMode, true);

                    await progressForm.UpdateProgressHandler(40, "데이터 검색 중...");
                    await Task.Delay(10);

                    if (matchingClusterIds.Count > 0)
                    {
                        // PerformSearchWithCriteria 우회하고 직접 결과 표시
                        await _clusteringManager.DisplaySpecificClustersAsync(matchingClusterIds);

                        // 선택 상태 초기화
                        merge_all_check.Checked = false;
                        change_row_count();
                        Debug.WriteLine($"복합 조건 검색 완료: {matchingClusterIds.Count}개 클러스터 표시");
                    }
                    else
                    {
                        await ShowEmptySearchResult();
                        Debug.WriteLine("검색 결과 없음 - 빈 테이블 표시");
                    }
                    /*
                    string searchColumn = GetSelectedSearchColumn();
                    List<string> matchingKeywords;

                    await progressForm.UpdateProgressHandler(20, $"'{searchColumn}' 컬럼에서 검색 중...");
                    await Task.Delay(10);

                    if (equalsSearchYN)
                    {
                        // 완전일치 검색 - ClusteringManager 사용
                        matchingKeywords = _clusteringManager.SearchExact(searchColumn, target_keyword);
                        Debug.WriteLine($"완전일치 검색 결과: {matchingKeywords.Count}개 키워드");
                    }
                    else
                    {
                        // 부분일치 검색 - ClusteringManager 사용
                        matchingKeywords = _clusteringManager.SearchContains(searchColumn, target_keyword);
                        Debug.WriteLine($"부분일치 검색 결과: {matchingKeywords.Count}개 키워드");
                    }

                    // AND/OR 검색 조건 파싱
                    bool useAndSearch = andSearchYN;

                    Debug.WriteLine($"검색 실행 - 컬럼: {searchColumn}, 키워드: {target_keyword}, 완전일치: {equalsSearchYN}, AND: {useAndSearch}");

                    // 다중 컬럼 검색 조건 구성
                    var columnCriteria = new Dictionary<string, SearchColumnCriteria>();

                    if (matchingKeywords.Count > 0)
                    {
                        columnCriteria[searchColumn] = new SearchColumnCriteria
                        {
                            Keywords = matchingKeywords,
                            ExactMatch = true, // 이미 매칭된 키워드들이므로 정확 매칭
                            UseAnd = useAndSearch
                        };

                        await progressForm.UpdateProgressHandler(40, "데이터 검색 중...");
                        await Task.Delay(10);

                        await PerformSearchWithCriteria(columnCriteria, isAlreadyProgress);
                    }
                    else
                    {
                        // *** 수정: 검색 결과가 없을 때 빈 테이블 표시 ***
                        await ShowEmptySearchResult();
                        Debug.WriteLine("검색 결과 없음 - 빈 테이블 표시");
                    }

                   
                    */
                    await progressForm.UpdateProgressHandler(90, "데이터 검색 완료");
                    await Task.Delay(10);

                    await progressForm.UpdateProgressHandler(100);
                    await Task.Delay(10);
                    progressForm.Close();
                }
            }

        }

        /// <summary>
        /// 현재 선택된 검색 컬럼 반환 (keyword_radio 대체)
        /// </summary>
        private string GetSelectedSearchColumn()
        {
            // *** 수정: 인덱스 0부터 유효한 컬럼으로 처리 ***
            if (column_search_combo.SelectedIndex >= 0)
            {
                string selectedDisplayName = column_search_combo.SelectedItem.ToString();
                string columnName = _clusteringManager.ConvertDisplayNameToColumnName(selectedDisplayName);

                // 실제 데이터에 해당 컬럼이 존재하는지 확인
                if (_clusteringManager.HasDataInColumn(columnName))
                {
                    Debug.WriteLine($"선택된 검색 컬럼: {selectedDisplayName} -> {columnName}");
                    return columnName;
                }
                else
                {
                    Debug.WriteLine($"경고: 선택된 컬럼 '{columnName}'에 데이터가 없습니다.");
                    return "키워드목록"; // 기본값: 키워드
                }
            }
            else
            {
                Debug.WriteLine("검색 컬럼이 선택되지 않아 기본값(키워드목록) 사용");
                return "키워드목록"; // 기본값: 키워드
            }

        }

      

        /// <summary>
        /// 검색 조건으로 실제 검색 수행
        /// </summary>
        private async Task PerformSearchWithCriteria(Dictionary<string, SearchColumnCriteria> columnCriteria, bool isAlreadyProgress)
        {
            try
            {
                // 제외 키워드 처리
                List<string> excludeKeywords = null;
                if (!string.IsNullOrEmpty(except_keyword.Text))
                {
                    excludeKeywords = except_keyword.Text.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)).ToList();
                }

                // 검색 내 검색 모드 처리
                if (_isSubSearchMode && _baseSearchResults.Count > 0)
                {
                    await PerformSubSearch(columnCriteria, excludeKeywords, isAlreadyProgress);
                }
                else
                {
                    await PerformNormalSearch(columnCriteria, excludeKeywords, isAlreadyProgress);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"검색 수행 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 일반 검색 수행
        /// </summary>
        private async Task PerformNormalSearch(Dictionary<string, SearchColumnCriteria> columnCriteria, List<string> excludeKeywords, bool isAlreadyProgress)
        {
            if (isAlreadyProgress)
            {
                // 다중 컬럼 검색 수행
                await _clusteringManager.SearchMultipleColumnsAsync(columnCriteria, excludeKeywords,true);
            }
            else
            {
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "데이터 검색 시작");

                    await _clusteringManager.SearchMultipleColumnsAsync(columnCriteria, excludeKeywords, true);

                    await progressForm.UpdateProgressHandler(100, "데이터 검색 완료");
                    await Task.Delay(10);
                }
            }

            merge_all_check.Checked = false;
            change_row_count();
        }

        /// <summary>
        /// 검색 내 검색 수행
        /// </summary>
        private async Task PerformSubSearch(Dictionary<string, SearchColumnCriteria> columnCriteria, List<string> excludeKeywords, bool isAlreadyProgress)
        {
            // 전체 검색 수행
            var searchResult = await _clusteringManager.SearchMultipleColumnsAsync(columnCriteria, excludeKeywords,true);

            // 기준 검색 결과와 교집합
            var currentResults = _clusteringManager.GetCurrentResultClusterIds();
            var filteredResults = currentResults.Intersect(_baseSearchResults).ToList();

            Debug.WriteLine($"검색 내 검색 결과: 전체 {currentResults.Count}개 → 필터링 후 {filteredResults.Count}개");

            // 필터링된 결과로 UI 업데이트 (추가 구현 필요)
            await DisplayFilteredSubSearchResults(filteredResults);
        }

        /// <summary>
        /// 검색 내 검색 결과를 화면에 표시
        /// </summary>
        private async Task DisplayFilteredSubSearchResults(List<int> filteredClusterIds)
        {
            try
            {
                // ClusteringManager의 새 메서드 사용
                await _clusteringManager.DisplaySpecificClustersAsync(filteredClusterIds);

                merge_all_check.Checked = false;
                change_row_count();

                Debug.WriteLine($"검색 내 검색 결과 표시 완료: {filteredClusterIds?.Count ?? 0}개");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"검색 내 검색 결과 표시 오류: {ex.Message}");
                MessageBox.Show($"검색 내 검색 결과 표시 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void change_row_count()
        {
            int rowCount = merge_cluster_table.RowCount;

            cluster_count.Text = $"행 수  : {rowCount}";

            //int unClusterCount = GetCountOfNegativeOneClusterIDs(DataHandler.finalClusteringData);
            //string unClusterCountMoney = GetSumOfNegativeTotalMoney(DataHandler.finalClusteringData);
            int unClusterCount = GetCountOfNegativeOneClusterIDs(_detailClusteringData);
            string unClusterCountMoney = GetSumOfNegativeTotalMoney(_detailClusteringData);
            uncluster_count.Text = $"미병합 Cluster  : {unClusterCount}";
            uncluster_count_money.Text = $"미병합 합산금액  : {unClusterCountMoney}";
        }

        // DataTable에서 ClusterID가 -1인 행 개수 구하기
        public int GetCountOfNegativeOneClusterIDs(DataTable dataTable)
        {
            // DataTable이 null인지 확인
            if (dataTable == null)
                return 0;

            // "ClusterID" 컬럼이 존재하는지 확인
            if (!dataTable.Columns.Contains("ClusterSubID"))
                return 0;

            // LINQ를 사용하여 ClusterID가 -1인 행 개수 계산
            int count = dataTable.AsEnumerable()
                                 .Count(row => row.Field<int>("ClusterSubID") == -1);

            return count;
        }

        public string GetSumOfNegativeTotalMoney(DataTable dataTable)
        {
            // DataTable이 null인지 확인
            if (dataTable == null)
                return FormatToKoreanUnit(0);

            // "ClusterID" 컬럼이 존재하는지 확인
            if (!dataTable.Columns.Contains("ClusterSubID"))
                return FormatToKoreanUnit(0);

            // "합산금액" 컬럼이 존재하는지 확인
            if (!dataTable.Columns.Contains("합산금액"))
                return FormatToKoreanUnit(0);

            // LINQ를 사용하여 ClusterID가 -1인 행들의 합산금액 총합 계산
            decimal sum = dataTable.AsEnumerable()
                                  .Where(row => row.Field<int>("ClusterSubID") == -1)
                                  .Sum(row => row.Field<decimal>("합산금액"));

            return FormatToKoreanUnit(sum);
        }

        private void create_check_keyword_list()
        {
            string target_keyword = "";

            if (!"".Equals(check_search_keyword.Text.ToString()) && check_search_keyword.Text != null)
            {
                target_keyword = check_search_keyword.Text.ToString();
            }

            List<string> MathcingPairs = new List<string>();
            try
            {
                if (!"".Equals(target_keyword))
                {
                    MathcingPairs = DataHandler.FindMachKeyword(check_keyword_list, target_keyword);
                    if (MathcingPairs.Count == 0)
                    {
                        merge_check_table.DataSource = null;
                        merge_check_table.Rows.Clear();
                        merge_check_table.Columns.Clear();
                        if (DataHandler.dragSelections.ContainsKey(merge_check_table))
                        {
                            DataHandler.dragSelections[merge_check_table].Clear();
                        }

                        return;
                    }
                    else
                    {
                        CreateCheckDataGridView(merge_check_table, _detailClusteringData, MathcingPairs);
                    }

                }
                //전체 검색
                else
                {
                    CreateCheckDataGridView(merge_check_table, _detailClusteringData, MathcingPairs);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }


      
        public void CreateCheckDataGridView(DataGridView dgv, DataTable dt, List<string> filterWords)
        {
            // DataGridView 초기화
            dgv.DataSource = null;
            dgv.Rows.Clear();
            dgv.Columns.Clear();
            if (DataHandler.dragSelections.ContainsKey(dgv))
            {
                DataHandler.dragSelections[dgv].Clear();
            }


            // CheckBox 컬럼 추가
            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn();
            checkColumn.Name = "CheckBox";
            checkColumn.HeaderText = "";
            checkColumn.Width = 50;
            checkColumn.ThreeState = false;
            //checkColumn.Frozen = true;
            checkColumn.FillWeight = 20;  // 다른 컬럼들보다 작은 값 설정

            dgv.Columns.Add(checkColumn);


            // 원본 DataTable의 컬럼들 추가
            foreach (DataColumn col in dt.Columns)
            {
                dgv.Columns.Add(col.ColumnName, col.ColumnName);

            }

            // 데이터 필터링 및 추가
            foreach (DataRow row in dt.Rows)
            {
                // ClusterID 체크
                bool skipRow = true;
                if (!row.IsNull("ClusterSubID"))  // ClusterID가 null이 아니고
                {
                    int clusterId = Convert.ToInt32(row["ClusterSubID"]);
                    int rowId = Convert.ToInt32(row["ID"]);
                    //Debug.WriteLine($"skipRow  clusterID   :  {clusterId}   rowId    : {rowId}");
                    if (clusterId == rowId && clusterId > 0)  // 0이 아니면 스킵
                    {
                        skipRow = false;
                        //Debug.WriteLine($"skipRow false -> clusterID   :  {clusterId}");
                    }
                }

                if (!skipRow)  // ClusterID 조건을 통과한 경우만 처리
                {
                    if (filterWords.Count > 0)
                    {
                        string keywordColumnValue = row["키워드목록"].ToString();

                        if (filterWords.Any(word => keywordColumnValue.Contains(word)))
                        {
                            int rowIndex = dgv.Rows.Add();
                            dgv.Rows[rowIndex].Cells["CheckBox"].Value = false;

                            for (int i = 0; i < dt.Columns.Count; i++)
                            {
                                //합산금액 컬럼은 수정
                                if ("합산금액".Equals(dt.Columns[i].ColumnName))
                                {
                                    //Debug.WriteLine("합산 금액 컬럼 수정 로직 적용");
                                    dgv.Rows[rowIndex].Cells[i + 1].Value = FormatToKoreanUnit(Convert.ToDecimal(row[i]));
                                }
                                else
                                {
                                    dgv.Rows[rowIndex].Cells[i + 1].Value = row[i];
                                }
                            }
                        }
                    }
                    else
                    {
                        int rowIndex = dgv.Rows.Add();
                        dgv.Rows[rowIndex].Cells["CheckBox"].Value = false;

                        for (int i = 0; i < dt.Columns.Count; i++)
                        {
                            //합산금액 컬럼은 수정
                            if ("합산금액".Equals(dt.Columns[i].ColumnName))
                            {
                                //Debug.WriteLine("합산 금액 컬럼 수정 로직 적용");
                                dgv.Rows[rowIndex].Cells[i + 1].Value = FormatToKoreanUnit(Convert.ToDecimal(row[i]));
                            }
                            else
                            {
                                dgv.Rows[rowIndex].Cells[i + 1].Value = row[i];
                            }
                        }
                    }
                }

            }

            Debug.WriteLine($"병합 클러스터 조회 결과  :  {dgv.Rows.Count}");

            // ID 컬럼 숨기기
            dgv.Columns["ID"].Visible = false;
            // ID 컬럼 숨기기
            dgv.Columns["_id"].Visible = false;
            // ClusterID 컬럼 숨기기
            if (dgv.Columns["ClusterID"] != null)
            {
                dgv.Columns["ClusterID"].Visible = false;
            }

            if (dgv.Columns["ClusterSubID"] != null)
            {
                dgv.Columns["ClusterSubID"].Visible = false;
            }

            // dataIndex 컬럼 숨기기
            dgv.Columns["dataIndex"].Visible = false;

            if (dgv.Columns["Count"] != null)
            {
                dgv.Columns["Count"].DefaultCellStyle.Format = "N0"; // 천 단위 구분자
                dgv.Columns["Count"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            }



            // DataGridView 속성 설정
            dgv.AllowUserToAddRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.ReadOnly = false;

            dgv.Columns["CheckBox"].ReadOnly = false;  // 체크박스 컬럼만 편집 가능

            // 나머지 컬럼들은 읽기 전용으로 설정
            for (int i = 1; i < dgv.Columns.Count; i++)
            {
                dgv.Columns[i].ReadOnly = true;
            }
            
            dgv.Columns["클러스터명"].ReadOnly = false;  // 클러스터명 편집 가능
            dgv.CellEndEdit -= DataGridView_CellEndEdit;
            dgv.CellEndEdit += DataGridView_CellEndEdit;
            dgv.CellBeginEdit -= DataGridView_CellBeginEdit; // 중복 등록 방지
            dgv.CellBeginEdit += DataGridView_CellBeginEdit;
            //dgv.Font = new System.Drawing.Font("Pretendard", 14.25F);
            // "클러스터명" 컬럼의 배경색을 연노란색으로 설정
            dgv.Columns["클러스터명"].DefaultCellStyle.BackColor = System.Drawing.Color.LightYellow;

            dgv.Columns["클러스터명"].HeaderText = "세부 클러스터명";


            // ID 컬럼을 기준으로 내림차순 정렬
            dgv.Sort(dgv.Columns["ID"], System.ComponentModel.ListSortDirection.Descending);

            // 정렬 후 0번째 행이 있다면 선택
            if (dgv.Rows.Count > 0)
            {
                dgv.ClearSelection();
                dgv.Rows[0].Selected = true;
                dgv.CurrentCell = dgv.Rows[0].Cells[0];
            }
        }


        public List<string> GetCheckedRowsStringData(DataGridView dgv)
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

        //체크 항목 데이터 수집
        public List<int> GetCheckedRowsData(DataGridView dgv)
        {
            // 현재 페이지의 선택 상태 저장
            if (dgv == merge_cluster_table)
            {
                //SaveCurrentSelectionState();
                //return _selectedClusterNumbers.ToList();
                return _clusteringManager.GetSelectedClusterIds();
            }

            // 기존 로직 (다른 DataGridView용)
            List<int> checkedData = new List<int>();
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Cells[0].Value != null &&
                    Convert.ToBoolean(row.Cells[0].Value) == true)
                {
                    if (row.Cells["ID"].Value != null)
                    {
                        if (int.TryParse(row.Cells["ID"].Value.ToString(), out int id))
                        {
                            checkedData.Add(id);
                        }
                    }
                }
            }
            return checkedData;
        }

        public int GetCheckedRowsIndex(DataGridView dgv)
        {
            int checkedData = 0;
            foreach (DataGridViewRow row in dgv.Rows)
            {
                // CheckBox 컬럼(0번째)이 체크되었는지 확인
                if (row.Cells[0].Value != null &&
                    Convert.ToBoolean(row.Cells[0].Value) == true)
                {
                    checkedData = row.Index;
                    break;
                }
            }
            return checkedData;
        }

        public async Task MergeAndCreateNewCluster(DataTable dataTable, List<int> targetIds,
     string clusterName = null, string clusterID = null)
        {
            if (targetIds == null || targetIds.Count == 0) return;

            try
            {
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 병합 시작: {targetIds.Count}개");

                var clusteringRepo = new ClusteringRepository();

                // 새 클러스터 번호 결정
                int newClusterNumber;
                bool isNewCluster = true;
                ClusteringResultDocument existingCluster = null;

                if (clusterID != null && int.TryParse(clusterID, out newClusterNumber))
                {
                    isNewCluster = false;
                    existingCluster = await clusteringRepo.GetByClusterNumberAsync(newClusterNumber);
                    if (existingCluster == null)
                        throw new Exception($"클러스터 번호 {newClusterNumber} 존재하지 않습니다.");
                }
                else
                {
                    newClusterNumber = await clusteringRepo.GetNextClusterNumberAsync();
                }

                // 2단계: 메모리에서 대상 행들의 데이터 수집
                var targetRowsData = new List<ClusteringResultDocument>();

                foreach (int targetId in targetIds)
                {
                    var row = dataTable.AsEnumerable()
                        .FirstOrDefault(r => Convert.ToInt32(r["ID"]) == targetId);

                    if (row != null)
                    {
                        var rowData = new ClusteringResultDocument
                        {
                            ClusterNumber = targetId,
                            ClusterId = _parentClusterId,
                            ClusterSubId = Convert.ToInt32(row["ClusterSubID"]),
                            ClusterName = row["클러스터명"]?.ToString() ?? "",
                            Keywords = row["키워드목록"]?.ToString()?.Split(',')
                                .Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)).ToList() ?? new List<string>(),
                            Count = Convert.ToInt32(row["Count"]),
                            TotalAmount = Convert.ToDecimal(row["합산금액"]),
                            DataIndices = row["dataIndex"]?.ToString()?.Split(',')
                                .Select(i => i.Trim()).Where(i => !string.IsNullOrEmpty(i)).ToList() ?? new List<string>()
                        };
                        targetRowsData.Add(rowData);
                    }
                }

                // 3단계: 클러스터명 생성
                string mergedClusterName = clusterName ??
                    string.Join("_", targetRowsData.Select(r => r.ClusterName).Take(3)) +
                    (targetRowsData.Count > 3 ? "..." : "");

                if (mergedClusterName.Length > 20)
                    mergedClusterName = mergedClusterName.Substring(0, 17) + "...";

                // *** 4단계: 기존 클러스터에 추가하는 경우 데이터 누적 ***
                if (!isNewCluster && existingCluster != null)
                {
                    Debug.WriteLine($"기존 클러스터 {newClusterNumber}에 데이터 누적");

                    // 키워드 병합 (중복 제거)
                    //var allKeywords = new HashSet<string>(existingCluster.Keywords ?? new List<string>());
                    
                    // 기존 세부 클러스터에 추가
                    var allKeywords = new HashSet<string>(existingCluster.Keywords ?? new List<string>());
                    var allDataIndices = new HashSet<string>(existingCluster.DataIndices ?? new List<string>());


                    foreach (var target in targetRowsData)
                    {
                        foreach (var keyword in target.Keywords)
                            allKeywords.Add(keyword);
                        foreach (var index in target.DataIndices)
                            allDataIndices.Add(index);
                    }

                    // 새로운 누적 데이터 계산
                    int newCount = existingCluster.Count + targetRowsData.Sum(t => t.Count);
                    decimal newTotalAmount = existingCluster.TotalAmount + targetRowsData.Sum(t => t.TotalAmount);

                    // *** ClusteringRepository의 UpdateClusterFullInfoAsync 메서드 사용 ***
                    bool updateSuccess = await clusteringRepo.UpdateClusterFullInfoAsync(
                        newClusterNumber,
                        string.IsNullOrEmpty(clusterName) ? existingCluster.ClusterName : clusterName,
                        allKeywords.ToList(),
                        newCount,
                        newTotalAmount,
                        allDataIndices.ToList()
                    );

                    if (!updateSuccess)
                    {
                        Debug.WriteLine($"기존 클러스터 {newClusterNumber} 업데이트 실패");
                        return;
                    }

                    Debug.WriteLine($"기존 클러스터 {newClusterNumber} 누적 완료: " +
                                   $"Count {existingCluster.Count} -> {newCount}, " +
                                   $"Amount {existingCluster.TotalAmount} -> {newTotalAmount}");
                }
                else
                {
                    // *** 5단계: 새 클러스터 생성 (ClusteringRepository의 MergeOrUpdateClusterAsync 사용) ***
                    Debug.WriteLine($"새 클러스터 {newClusterNumber} 생성");

                    int mergedClusterNumber = await clusteringRepo.MergeDetailClustersAsync(
                        targetIds,
                        mergedClusterName,
                        _parentClusterId // 새 클러스터 생성
                    );

                    if (mergedClusterNumber != newClusterNumber)
                    {
                        Debug.WriteLine($"예상한 클러스터 번호 {newClusterNumber}와 실제 생성된 번호 {mergedClusterNumber}가 다름");
                        newClusterNumber = mergedClusterNumber;
                    }
                }

                // *** 6단계: DataTable 업데이트 (기존 행 업데이트 + 병합된 클러스터들의 ClusterID 변경) ***
                await UpdateDataTableAfterMerge(dataTable, targetIds, newClusterNumber, isNewCluster);


                // *** 7단계: 데이터 보강 (동기적 처리로 일관성 보장) ***
                //_detailClusteringData = dataTable;
                //_detailClusteringData.AcceptChanges();
                //mergeClusterDataTable = await EnrichWithRawTableDataAsync(_detailClusteringData);

                dataTable.AcceptChanges();
                mergeClusterDataTable = await EnrichWithRawTableDataAsync(dataTable);

                // *** 8단계: ClusteringManager 데이터 새로고침 ***
                if (_clusteringManager != null)
                {
                    await _clusteringManager.RefreshDataAsync(mergeClusterDataTable);
                }

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 병합 완료: {newClusterNumber}");

                // 병합 클러스터 리스트 생성
                create_check_keyword_list();

                // 병합 작업 후 UI 업데이트
                UpdateModifiedDataGridView();
                UpdateSupplySummaryDataGridView();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 병합 오류: {ex.Message}");
                throw;
            }
        }
        // *** 새로 추가: DataTable 업데이트 헬퍼 메서드 ***
        private async Task UpdateDataTableAfterMerge(DataTable dataTable, List<int> targetIds,
            int newClusterNumber, bool isNewCluster)
        {
            await Task.Run(() =>
            {
                try
                {
                    // 병합된 클러스터들의 ClusterID 업데이트
                    foreach (int targetId in targetIds)
                    {
                        var targetRow = dataTable.AsEnumerable()
                            .FirstOrDefault(row => Convert.ToInt32(row["ID"]) == targetId);

                        if (targetRow != null)
                        {
                            //targetRow["ClusterID"] = newClusterNumber;
                            targetRow["ClusterSubID"] = newClusterNumber;
                        }
                    }

                    // 기존 클러스터에 update
                    if (!isNewCluster)
                    {
                        var existingRow = dataTable.AsEnumerable()
                            .FirstOrDefault(row => Convert.ToInt32(row["ID"]) == newClusterNumber);

                        if (existingRow != null)
                        {
                            // MongoDB에서 최신 데이터를 가져와서 DataTable 업데이트
                            var clusteringRepo = new ClusteringRepository();
                            var updatedCluster = clusteringRepo.GetByClusterNumberAsync(newClusterNumber).Result;

                            if (updatedCluster != null)
                            {
                                existingRow["클러스터명"] = updatedCluster.ClusterName;
                                existingRow["Count"] = updatedCluster.Count;
                                existingRow["합산금액"] = updatedCluster.TotalAmount;
                                existingRow["키워드목록"] = string.Join(",", updatedCluster.Keywords);
                                existingRow["dataIndex"] = string.Join(",", updatedCluster.DataIndices);
                            }

                            // 병합되는 클러스터들의 ClusterID를 기존 클러스터 번호로 변경
                            foreach (int targetId in targetIds)
                            {
                                var updatedElement = clusteringRepo.UpdateSubClusterIdAsync(targetId, newClusterNumber);

                                if (updatedElement != null)
                                {
                                    //Debug.WriteLine($"MongoDB에서 클러스터 {targetId}의 cluster_id를 {newClusterNumber}로 변경");

                                    var targetRow = dataTable.AsEnumerable()
                                        .FirstOrDefault(row => Convert.ToInt32(row["ID"]) == targetId);

                                    if (targetRow != null)
                                    {
                                        //targetRow["ClusterID"] = newClusterNumber;
                                        targetRow["ClusterSubID"] = newClusterNumber;
                                        
                                        //Debug.WriteLine($"클러스터 {targetId}의 ClusterID를 {newClusterNumber}로 변경");
                                    }
                                }

                            }
                        }

                    }
                    // 새 클러스터가 생성된 경우, 기존 DataTable에서 상위 클러스터 행 찾아서 업데이트
                    else
                    {
                        // *** 핵심 수정: 새 클러스터 행을 DataTable에 추가 ***
                        var clusteringRepo = new ClusteringRepository();
                        var newCluster = clusteringRepo.GetByClusterNumberAsync(newClusterNumber).Result;

                        if (newCluster != null)
                        {
                            DataRow newRow = dataTable.NewRow();
                            newRow["ID"] = newCluster.ClusterNumber;
                            newRow["ClusterID"] = newCluster.ClusterId;
                            newRow["ClusterSubID"] = newCluster.ClusterSubId;
                            newRow["클러스터명"] = newCluster.ClusterName;
                            newRow["키워드목록"] = string.Join(",", newCluster.Keywords);
                            newRow["Count"] = newCluster.Count;
                            newRow["합산금액"] = newCluster.TotalAmount;
                            newRow["dataIndex"] = string.Join(",", newCluster.DataIndices);

                            dataTable.Rows.Add(newRow); // ← 새 행 추가

                            Debug.WriteLine($"새 클러스터 행 추가: ID={newCluster.ClusterNumber}");

                            // 병합되는 클러스터들의 ClusterID를 기존 클러스터 번호로 변경
                            foreach (int targetId in targetIds)
                            {
                                var targetRow = dataTable.AsEnumerable()
                                         .FirstOrDefault(row => Convert.ToInt32(row["ID"]) == targetId);

                                if (targetRow != null)
                                {
                                    targetRow["ClusterSubID"] = newClusterNumber;
                                    //Debug.WriteLine($"클러스터 {targetId}의 ClusterID를 {newClusterNumber}로 변경");
                                }
                            }
                        }
                    }

                    // *** 이제 AcceptChanges() 호출 ***
                    dataTable.AcceptChanges();

                    Debug.WriteLine($"DataTable 업데이트 완료: {targetIds.Count}개 행 처리");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"DataTable 업데이트 오류: {ex.Message}");
                }
            });
        }


        public async Task deleteClusterId(DataTable dataTable, List<int> targetIds)
        {
            try
            {
                Debug.WriteLine($"병합 해제 대상 ID: {string.Join(", ", targetIds)}");

                // 삭제할 행들을 찾아서 리스트에 담기
                var rowsToDelete = dataTable.AsEnumerable()
                    .Where(row => targetIds.Contains(Convert.ToInt32(row["ID"])))
                    .ToList();

                // 찾은 행들을 삭제
                foreach (var row in rowsToDelete)
                {
                    dataTable.Rows.Remove(row);
                }

                // 병합된 하위 클러스터들 찾기
                var childRows = dataTable.AsEnumerable()
                    .Where(row => row["ClusterSubId"] != DBNull.Value &&
                           targetIds.Contains(Convert.ToInt32(row["ClusterSubId"])))
                    .ToList();

                Debug.WriteLine($"병합 해제할 하위 클러스터 수: {childRows.Count}");

                // 하위 클러스터들의 ClusterID 초기화
                foreach (var row in childRows)
                {
                    row["ClusterSubId"] = -1; // 미병합 상태로 변경
                }

                // 변경사항 적용
                dataTable.AcceptChanges();

                // MongoDB에서도 삭제 및 상태 재설정
                var clusteringRepo = new ClusteringRepository();


                foreach (int targetId in targetIds)
                {
                    // 1. 삭제할 클러스터 정보 조회
                    var cluster = await clusteringRepo.GetByClusterNumberAsync(targetId);
                    if (cluster != null)
                    {
                        // 2. 이 클러스터에 병합된 다른 클러스터들의 상태 재설정
                        var childClusters = await clusteringRepo.GetDetailClustersByParentIdAsync(_parentClusterId);
                        var affectedChildren = childClusters.Where(c => c.ClusterSubId == targetId).ToList();
                        foreach (var child in childClusters)
                        {
                            await clusteringRepo.UpdateSubClusterIdAsync(child.ClusterNumber, -1);
                            //Debug.WriteLine($"클러스터 {child.ClusterNumber}의 병합 상태 해제");
                        }

                        // 3. 클러스터 자체 삭제
                        await clusteringRepo.DeleteDetailClusterAndRestoreChildrenAsync(targetId);
                        //Debug.WriteLine($"클러스터 {targetId} 삭제 완료");
                    }
                }

                mergeClusterDataTable = await EnrichWithRawTableDataAsync(dataTable);
                await _clusteringManager.RefreshDataAsync(mergeClusterDataTable);

                var searchCriteria = CreateSearchCriteriaFromCurrentUI();
                await _clusteringManager.SearchAsync(searchCriteria , true);

                // 병합 작업 후 UI 업데이트
                UpdateModifiedDataGridView();
                UpdateSupplySummaryDataGridView();

                Debug.WriteLine("병합 해제 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터 삭제 오류: {ex.Message}");
                MessageBox.Show($"클러스터 삭제 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 세부 클러스터링 완료 시 전역 데이터와 동기화
        /// </summary>
        private async Task SyncDetailClusteringToGlobalData()
        {
            try
            {
                Debug.WriteLine($"세부 클러스터링 데이터를 전역 데이터와 동기화 시작 - 부모 클러스터: {_parentClusterId}");

                if (DataHandler.finalClusteringData == null || _detailClusteringData == null)
                {
                    Debug.WriteLine("동기화할 데이터가 없습니다.");
                    return;
                }

                // *** 1. 전역 데이터에서 해당 부모 클러스터 관련 데이터만 추출 ***
                var globalParentRows = DataHandler.finalClusteringData.AsEnumerable()
                    .Where(row => Convert.ToInt32(row["ClusterID"]) == _parentClusterId)
                    .ToList();

                Debug.WriteLine($"전역 데이터에서 부모 클러스터 {_parentClusterId} 관련 행: {globalParentRows.Count}개");

                // *** 전역 데이터에 ClusterSubID 컬럼이 없으면 추가 ***
                if (!DataHandler.finalClusteringData.Columns.Contains("ClusterSubID"))
                {
                    DataHandler.finalClusteringData.Columns.Add("ClusterSubID", typeof(int));

                    // 기존 모든 행에 기본값 -1 설정 (한 번만 실행)
                    foreach (DataRow existingRow in DataHandler.finalClusteringData.Rows)
                    {
                        existingRow["ClusterSubID"] = -1;
                    }

                    DataHandler.finalClusteringData.AcceptChanges();
                    Debug.WriteLine("전역 데이터에 ClusterSubID 컬럼 추가 및 기본값 설정 완료");
                }

                // *** 2. 로컬 데이터의 클러스터 번호 목록 추출 (효율성을 위해 미리 수집) ***
                var localClusterNumbers = new HashSet<int>(
                    _detailClusteringData.AsEnumerable()
                        .Select(row => Convert.ToInt32(row["ID"]))
                );

                Debug.WriteLine($"로컬 데이터 클러스터 번호: {localClusterNumbers.Count}개");

                // *** 3. 부모 클러스터 관련 행만을 대상으로 수정 작업 수행 ***
                foreach (var globalRow in globalParentRows)
                {
                    int globalClusterNumber = Convert.ToInt32(globalRow["ID"]);

                    // 로컬 데이터에서 해당 클러스터 찾기
                    var localRow = _detailClusteringData.AsEnumerable()
                        .FirstOrDefault(row => Convert.ToInt32(row["ID"]) == globalClusterNumber);

                    if (localRow != null)
                    {
                        // *** 기존 데이터 업데이트 (ClusterSubID 동기화) ***
                        int clusterSubId = Convert.ToInt32(localRow["ClusterSubID"]);

                        globalRow["ClusterSubID"] = clusterSubId;
                        globalRow["클러스터명"] = localRow["클러스터명"];
                        globalRow["키워드목록"] = localRow["키워드목록"];
                        globalRow["Count"] = localRow["Count"];
                        globalRow["합산금액"] = localRow["합산금액"];
                        globalRow["dataIndex"] = localRow["dataIndex"];

                        Debug.WriteLine($"기존 클러스터 {globalClusterNumber} 업데이트 - ClusterSubID: {clusterSubId}");
                    }
                }

                // *** 4. 새로 생성된 세부 상위 클러스터 추가 ***
                // (ClusterSubID == ClusterNumber이면서 전역 데이터에 없는 클러스터)
                var newDetailClusters = _detailClusteringData.AsEnumerable()
                    .Where(localRow =>
                    {
                        int clusterId = Convert.ToInt32(localRow["ID"]);
                        int clusterSubId = Convert.ToInt32(localRow["ClusterSubID"]);

                        // 세부 상위 클러스터이면서 전역 데이터에 없는 경우
                        return clusterSubId == clusterId &&
                               clusterSubId > 0 &&
                               !globalParentRows.Any(globalRow => Convert.ToInt32(globalRow["ID"]) == clusterId);
                    })
                    .ToList();

                foreach (var newLocalRow in newDetailClusters)
                {
                    DataRow newGlobalRow = DataHandler.finalClusteringData.NewRow();
                    newGlobalRow["ID"] = newLocalRow["ID"];
                    newGlobalRow["ClusterID"] = newLocalRow["ClusterID"];
                    newGlobalRow["ClusterSubID"] = newLocalRow["ClusterSubID"];
                    newGlobalRow["클러스터명"] = newLocalRow["클러스터명"];
                    newGlobalRow["키워드목록"] = newLocalRow["키워드목록"];
                    newGlobalRow["Count"] = newLocalRow["Count"];
                    newGlobalRow["합산금액"] = newLocalRow["합산금액"];
                    newGlobalRow["dataIndex"] = newLocalRow["dataIndex"];

                    DataHandler.finalClusteringData.Rows.Add(newGlobalRow);

                    int newClusterNumber = Convert.ToInt32(newLocalRow["ID"]);
                    Debug.WriteLine($"새로운 세부 클러스터 {newClusterNumber} 전역 데이터에 추가");
                }

                // *** 5. 삭제된 세부 클러스터 전역에서도 제거 ***
                // (전역에는 있지만 로컬에는 없는 세부 상위 클러스터)
                var globalRowsToDelete = globalParentRows
                    .Where(globalRow =>
                    {
                        int globalClusterNumber = Convert.ToInt32(globalRow["ID"]);
                        int globalClusterSubId = globalRow["ClusterSubID"] != DBNull.Value ?
                                                Convert.ToInt32(globalRow["ClusterSubID"]) : -1;

                        // 세부 상위 클러스터이면서 로컬 데이터에 없는 경우
                        return globalClusterSubId == globalClusterNumber &&
                               globalClusterSubId > 0 &&
                               !localClusterNumbers.Contains(globalClusterNumber);
                    })
                    .ToList();

                foreach (var rowToDelete in globalRowsToDelete)
                {
                    int deletedClusterNumber = Convert.ToInt32(rowToDelete["ID"]);
                    DataHandler.finalClusteringData.Rows.Remove(rowToDelete);
                    Debug.WriteLine($"삭제된 세부 클러스터 {deletedClusterNumber} 전역 데이터에서 제거");
                }

                // *** 6. 변경사항 저장 ***
                DataHandler.finalClusteringData.AcceptChanges();

                Debug.WriteLine($"세부 클러스터링 데이터 동기화 완료 - 처리된 부모 클러스터 관련 행: {globalParentRows.Count}개");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"데이터 동기화 오류: {ex.Message}");
                throw;
            }
        }

        private async void merge_search_button_Click(object sender, EventArgs e)
        {
            //create_merge_keyword_list();

            try
            {
                
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
                    MessageBox.Show("세부 병합할 클러스터를 선택해주세요.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }


                DialogResult result = MessageBox.Show(
                    $"선택된 {selectedClusterIds.Count}개의 클러스터를 세부 병합하시겠습니까?",
                    "클러스터 세부 병합 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    using (var progressForm = new ProcessProgressForm())
                    {
                        progressForm.Show();
                        await progressForm.UpdateProgressHandler(10, "클러스터 세부 병합 시작");
                        await Task.Delay(10);


                        // 기존 병합 로직 호출 (cluster_number 리스트 전달)
                        List<int> clusterNumbersToMerge = selectedClusterIds.ToList();

                        //await MergeAndCreateNewCluster(DataHandler.finalClusteringData, clusterNumbersToMerge);
                        await MergeAndCreateNewCluster(_detailClusteringData, clusterNumbersToMerge);

                        await progressForm.UpdateProgressHandler(50, "클러스터 세부 병합 중...");
                        await Task.Delay(10);

                        // 병합 후 선택 상태 초기화
                        //_selectedClusterNumbers.Clear();
                        merge_all_check.Checked = false;

                        // 데이터 다시 로드
                        await create_merge_keyword_list(true);

                        await progressForm.UpdateProgressHandler(100, "클러스터 세부 병합 완료");
                        await Task.Delay(10);

                    }

                    // *** 컬럼 정보 전체 출력 ***
                    Debug.WriteLine($"[CreateCheckDataGridView] DataHandler.finalClusteringData 총 행 수: {DataHandler.finalClusteringData.Rows.Count}");
                    Debug.WriteLine($"[CreateCheckDataGridView] DataHandler.finalClusteringData 총 컬럼 수: {DataHandler.finalClusteringData.Columns.Count}");
                    for (int i = 0; i < DataHandler.finalClusteringData.Columns.Count; i++)
                    {
                        Debug.WriteLine($"  컬럼 {i}: Name='{DataHandler.finalClusteringData.Columns[i].ColumnName}'" +
                            $", DataType='{DataHandler.finalClusteringData.Columns[i].DataType}'");
                    }

                    MessageBox.Show("클러스터 세부 병합이 완료되었습니다.", "완료",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터 세부 병합 중 오류: {ex.Message}");
                MessageBox.Show($"클러스터 세부 병합 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void merge_cancel_button_Click(object sender, EventArgs e)
        {
            List<int> mergeIDlList = GetCheckedRowsData(merge_check_table);

            if (mergeIDlList.Count == 0)
            {
                MessageBox.Show("세부 병합 해제 대상을 선택하셔야 합니다.", "알림",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Warning);

                return;
            }

            using (var progressForm = new ProcessProgressForm())
            {
                progressForm.Show();
                await progressForm.UpdateProgressHandler(10, "클러스터 세부 병합 해제 시작");
                await Task.Delay(10);

               

                await deleteClusterId(_detailClusteringData, mergeIDlList);

                await progressForm.UpdateProgressHandler(50, "클러스터 세부 병합 해제 중...");
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

                MessageBox.Show(this, "클러스터 세부 병합 해제가 완료되었습니다.", "Info",
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
                DataRow[] rows = _detailClusteringData.Select($"ID = {id}");
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
                //DataHandler.finalClusteringData.AcceptChanges();
                //mergeClusterDataTable = await EnrichWithRawTableDataAsync(DataHandler.finalClusteringData);
                _detailClusteringData.AcceptChanges();
                mergeClusterDataTable = await EnrichWithRawTableDataAsync(_detailClusteringData);

                await progressForm.UpdateProgressHandler(70, "클러스터명 변경 결과 출력 중...");
                await Task.Delay(10);

                // 4. ClusteringManager 데이터 새로고침
                /*
                if (_clusteringManager != null)
                {
                    await _clusteringManager.RefreshDataAsync(mergeClusterDataTable);
                }
                */
                create_check_keyword_list();

                // 병합 작업 후 UI 업데이트
                UpdateModifiedDataGridView();
                UpdateSupplySummaryDataGridView();

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
                    
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "세부 병합 클러스터링 완료 처리 시작...");

                    await SyncDetailClusteringToGlobalData();
                    await Task.Delay(10);

                    await progressForm.UpdateProgressHandler(70, "Export 페이지 이동 중...");
                    // 다음 페이지로 이동
                    userControlHandler.uc_classification.initUI();

                    if (this.ParentForm is Form1 form)
                    {
                        form.LoadUserControl(userControlHandler.uc_classification);
                    }

                    

                    await progressForm.UpdateProgressHandler(100, "세부 클러스터링 완료");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"complete_btn_Click 오류: {ex.Message}");
                MessageBox.Show($"클러스터 완료 처리 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        public string FormatToKoreanUnit(decimal number)
        {
            // 절대값으로 계산 후 나중에 부호 처리
            bool isNegative = number < 0;
            number = Math.Abs(number);


            string result;
            decimal divideNum = 0;


            divideNum = Math.Round(number / decimalDivider, 2);

            // 소수점 이하가 없는 경우 (정수인 경우)
            if (divideNum == Math.Truncate(divideNum))
            {
                result = string.Format("{0:N0}", divideNum) + " " + decimalDividerName;

            }
            // 소수점 둘째 자리가 0인 경우 (예: 10.5)
            else if (divideNum * 10 % 1 == 0)
            {
                result = string.Format("{0:N1}", divideNum) + " " + decimalDividerName;
            }
            //소수점 2째자리 표기
            else
            {
                result = string.Format("{0:N2}", divideNum) + " " + decimalDividerName;
            }




            // 음수 처리
            if (isNegative && divideNum != 0)
            {
                result = "-" + result;
            }

            return result;
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
                //await MergeAndCreateNewCluster(DataHandler.finalClusteringData, mergeIDlList, null, mergeAddClusterID);
                await MergeAndCreateNewCluster(_detailClusteringData, mergeIDlList, null, mergeAddClusterID);
                

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

        private void add_lv1_keyword()
        {
            // TextBox에 입력된 텍스트를 가져옴
            string inputText = new_lv1_word.Text.Trim();

            // 텍스트가 비어있지 않은 경우 ListBox에 추가
            if (!string.IsNullOrEmpty(inputText))
            {
                //DataHandler.separator.Add(inputText);
                _recomandKeywordManager.AddLv1Item(inputText);
                new_lv1_word.Clear(); // TextBox 초기화
            }

            List<string> lv1_list = _recomandKeywordManager.Lv1List
           .Distinct()  // 중복 제거
           .ToList();   // List로 변환

            //lv1 리스트 추가
            create_keyword_table(dataGridView_lv1, lv1_list);
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

        private void add_reco_keyword()
        {
            // TextBox에 입력된 텍스트를 가져옴
            string inputText = new_reco_word.Text.Trim();

            // 텍스트가 비어있지 않은 경우 ListBox에 추가
            if (!string.IsNullOrEmpty(inputText))
            {
                //DataHandler.separator.Add(inputText);
                _recomandKeywordManager.AddKeyword(selectecLv1Name, inputText);
                new_reco_word.Clear(); // TextBox 초기화
            }

            Lv1Item selectedItem = _recomandKeywordManager.GetLv1Item(selectecLv1Name);


            if (selectedItem != null)
            {
                List<string> keywords = selectedItem.Keywords;
                create_keyword_table(dataGridView_recoman_keyword, keywords, false);
            }
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

        // 키워드별 데이터를 저장할 클래스
        class KeywordData
        {
            public int Count { get; set; }
            public decimal TotalAmount { get; set; }
        }

        //2025.04.25
        //추천 키워드 갱신 함수
        // uc_DetailClustering.cs에 추가할 새 메서드
        private void UpdateModifiedDataGridView()
        {
            // UI 스레드에서 실행되는지 확인
            if (InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateModifiedDataGridView()));
                return;
            }

            // UI 업데이트 시작 전에 SuspendLayout 호출
            dataGridView_modified.SuspendLayout();

            try
            {
                if (mergeClusterDataTable == null || mergeClusterDataTable.Rows.Count == 0)
                {
                    Debug.WriteLine("mergeClusterDataTable이 null이거나 비어있습니다.");
                    return;
                }

                // DataGridView 초기화
                dataGridView_modified.DataSource = null;
                dataGridView_modified.Rows.Clear();
                dataGridView_modified.Columns.Clear();

                if (DataHandler.dragSelections.ContainsKey(dataGridView_modified))
                {
                    DataHandler.dragSelections[dataGridView_modified].Clear();
                }

                // 미병합 클러스터 필터링 (ClusterID == -1)
                var unboundClusters = mergeClusterDataTable.AsEnumerable()
                    //.Where(row => row.Field<int>("ClusterID") == -1)
                    .Where(row => row.Field<int>("ClusterSubID") == -1)
                    .ToList(); // CopyToDataTable 대신 ToList 사용

                if (unboundClusters.Count < 1)
                {
                    return; // 데이터가 없으면 종료
                }

                // 키워드를 그룹화하여 집계할 Dictionary 생성
                Dictionary<string, KeywordData> keywordDict = new Dictionary<string, KeywordData>();


                // 모든 키워드 추출 및 집계
                foreach (var row in unboundClusters)
                {
                    string keywordList = row["키워드목록"].ToString();
                    string[] keywords = keywordList.Split(',');
                    int rowCount = Convert.ToInt32(row["Count"]);
                    decimal rowAmount = Convert.ToDecimal(row["합산금액"]);

                    foreach (string keyword in keywords)
                    {
                        string trimmedKeyword = keyword.Trim();
                        if (string.IsNullOrEmpty(trimmedKeyword))
                            continue;

                        if (keywordDict.ContainsKey(trimmedKeyword))
                        {
                            // 기존 키워드에 값 추가
                            keywordDict[trimmedKeyword].Count += rowCount;
                            keywordDict[trimmedKeyword].TotalAmount += rowAmount;
                        }
                        else
                        {
                            // 새 키워드 추가
                            keywordDict[trimmedKeyword] = new KeywordData
                            {
                                Count = rowCount,
                                TotalAmount = rowAmount
                            };
                        }
                    }
                }

                // 정렬을 위해 리스트로 변환 (Count 기준 내림차순)
                var sortedKeywords = keywordDict.OrderByDescending(kv => kv.Value.Count).ToList();

                // DataGridView 컬럼 설정
                // *** 1번 기능: 좌상단 체크박스 컬럼 추가 ***
                DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn
                {
                    Name = "CheckBox",
                    HeaderText = "",
                    Width = 50,
                    ThreeState = false,
                    FillWeight = 20
                };
                dataGridView_modified.Columns.Add(checkColumn);

                dataGridView_modified.Columns.Add("키워드", "키워드");
                dataGridView_modified.Columns.Add("Count", "Count");
                dataGridView_modified.Columns.Add("합산금액", "합산금액");

                // 데이터 행 추가
                foreach (var keywordEntry in sortedKeywords)
                {
                    int rowIndex = dataGridView_modified.Rows.Add();
                    dataGridView_modified.Rows[rowIndex].Cells["키워드"].Value = keywordEntry.Key;
                    dataGridView_modified.Rows[rowIndex].Cells["Count"].Value = keywordEntry.Value.Count;
                    dataGridView_modified.Rows[rowIndex].Cells["합산금액"].Value =
                        FormatToKoreanUnit(keywordEntry.Value.TotalAmount);
                }

                // 열 형식 지정
                if (dataGridView_modified.Columns["Count"] != null)
                {
                    dataGridView_modified.Columns["Count"].DefaultCellStyle.Format = "N0";
                    dataGridView_modified.Columns["Count"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }

                // DataGridView 속성 설정
                dataGridView_modified.AllowUserToAddRows = false;
                dataGridView_modified.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGridView_modified.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dataGridView_modified.ReadOnly = true;
                dataGridView_modified.Font = new System.Drawing.Font("Pretendard", 14.25F);

                // 정렬 이벤트 핸들러 설정
                dataGridView_modified.SortCompare -= DataHandler.money_SortCompare;
                dataGridView_modified.SortCompare += DataHandler.money_SortCompare;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateModifiedDataGridView 오류: {ex.Message}");

                // 데이터가 없는 경우 빈 그리드 생성
                dataGridView_modified.Columns.Clear();
                dataGridView_modified.Columns.Add("키워드", "키워드");
                dataGridView_modified.Columns.Add("Count", "Count");
                dataGridView_modified.Columns.Add("합산금액", "합산금액");
            }
            finally
            {
                // UI 업데이트 재개
                dataGridView_modified.ResumeLayout();
            }
        }

        private void UpdateSupplySummaryDataGridView()
        {
            // UI 스레드에서 실행되는지 확인
            if (InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateSupplySummaryDataGridView()));
                return;
            }

            //2025.07.21
            //공급업체열이 필수가 아니면 로직 종료
            if (!DataHandler.prod_col_yn)
            {
                // DataGridView 초기화
                dataGridView_supply_summary.DataSource = null;
                dataGridView_supply_summary.Rows.Clear();
                dataGridView_supply_summary.Columns.Clear();
                return;
            }


            // UI 업데이트 시작 전에 SuspendLayout 호출
            dataGridView_supply_summary.SuspendLayout();

            try
            {
                if (mergeClusterDataTable == null || mergeClusterDataTable.Rows.Count == 0)
                {
                    Debug.WriteLine("mergeClusterDataTable이 null이거나 비어있습니다.");
                    return;
                }

                // DataGridView 초기화
                dataGridView_supply_summary.DataSource = null;
                dataGridView_supply_summary.Rows.Clear();
                dataGridView_supply_summary.Columns.Clear();

                if (DataHandler.dragSelections.ContainsKey(dataGridView_supply_summary))
                {
                    DataHandler.dragSelections[dataGridView_supply_summary].Clear();
                }

                // 미병합 클러스터 필터링 (ClusterID == -1)
                var unboundClusters = mergeClusterDataTable.AsEnumerable()
                    //.Where(row => row.Field<int>("ClusterID") == -1)
                    .Where(row => row.Field<int>("ClusterSubID") == -1)
                    .ToList(); // CopyToDataTable 대신 ToList 사용

                if (unboundClusters.Count < 1)
                {
                    return; // 데이터가 없으면 종료
                }

                // 키워드를 그룹화하여 집계할 Dictionary 생성
                Dictionary<string, KeywordData> keywordDict = new Dictionary<string, KeywordData>();


                // 모든 키워드 추출 및 집계
                foreach (var row in unboundClusters)
                {
                    string keywordList = row[DataHandler.prod_col_name].ToString();
                    string[] keywords = keywordList.Split(',');
                    int rowCount = Convert.ToInt32(row["Count"]);
                    decimal rowAmount = Convert.ToDecimal(row["합산금액"]);

                    foreach (string keyword in keywords)
                    {
                        string trimmedKeyword = keyword.Trim();
                        if (string.IsNullOrEmpty(trimmedKeyword))
                            continue;

                        if (keywordDict.ContainsKey(trimmedKeyword))
                        {
                            // 기존 키워드에 값 추가
                            keywordDict[trimmedKeyword].Count += rowCount;
                            keywordDict[trimmedKeyword].TotalAmount += rowAmount;
                        }
                        else
                        {
                            // 새 키워드 추가
                            keywordDict[trimmedKeyword] = new KeywordData
                            {
                                Count = rowCount,
                                TotalAmount = rowAmount
                            };
                        }
                    }
                }

                // 정렬을 위해 리스트로 변환 (Count 기준 내림차순)
                var sortedKeywords = keywordDict.OrderByDescending(kv => kv.Value.Count).ToList();

                // DataGridView 컬럼 설정
                // *** 1번 기능: 좌상단 체크박스 컬럼 추가 ***
                DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn
                {
                    Name = "CheckBox",
                    HeaderText = "",
                    Width = 50,
                    ThreeState = false,
                    FillWeight = 20
                };
                dataGridView_supply_summary.Columns.Add(checkColumn);

                dataGridView_supply_summary.Columns.Add("공급업체", "공급업체");
                dataGridView_supply_summary.Columns.Add("Count", "Count");
                dataGridView_supply_summary.Columns.Add("합산금액", "합산금액");

                // 데이터 행 추가
                foreach (var keywordEntry in sortedKeywords)
                {
                    int rowIndex = dataGridView_supply_summary.Rows.Add();
                    dataGridView_supply_summary.Rows[rowIndex].Cells["공급업체"].Value = keywordEntry.Key;
                    dataGridView_supply_summary.Rows[rowIndex].Cells["Count"].Value = keywordEntry.Value.Count;
                    dataGridView_supply_summary.Rows[rowIndex].Cells["합산금액"].Value =
                        FormatToKoreanUnit(keywordEntry.Value.TotalAmount);
                }

                // 열 형식 지정
                if (dataGridView_supply_summary.Columns["Count"] != null)
                {
                    dataGridView_supply_summary.Columns["Count"].DefaultCellStyle.Format = "N0";
                    dataGridView_supply_summary.Columns["Count"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }

                // DataGridView 속성 설정
                dataGridView_supply_summary.AllowUserToAddRows = false;
                dataGridView_supply_summary.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGridView_supply_summary.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dataGridView_supply_summary.ReadOnly = true;
                dataGridView_supply_summary.Font = new System.Drawing.Font("Pretendard", 14.25F);

                // 정렬 이벤트 핸들러 설정
                dataGridView_supply_summary.SortCompare -= DataHandler.money_SortCompare;
                dataGridView_supply_summary.SortCompare += DataHandler.money_SortCompare;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateSupplySummaryDataGridView 오류: {ex.Message}");

                // 데이터가 없는 경우 빈 그리드 생성
                dataGridView_supply_summary.Columns.Clear();
                dataGridView_supply_summary.Columns.Add("공급업체", "공급업체");
                dataGridView_supply_summary.Columns.Add("Count", "Count");
                dataGridView_supply_summary.Columns.Add("합산금액", "합산금액");
            }
            finally
            {
                // UI 업데이트 재개
                dataGridView_supply_summary.ResumeLayout();
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
                    //bool allParentClusters = clustersToMerge.All(c => c.ClusterId == c.ClusterNumber);
                    bool allParentClusters = clustersToMerge.All(c => c.ClusterSubId == c.ClusterNumber);
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
                    foreach (var cluster in clustersToMerge)
                    {
                        //var childClusters = await clusteringRepo.GetChildClustersAsync(cluster.ClusterNumber);
                        var subchildClusters = await clusteringRepo.GetSubChildClustersAsync(cluster.ClusterNumber);
                        allChildClusterNumbers.AddRange(subchildClusters.Select(c => c.ClusterNumber));
                    }

                    // 7. MongoDB에 새 병합 클러스터 생성
                    await progressForm.UpdateProgressHandler(60, "새 클러스터 생성 중");

                    var newCluster = new ClusteringResultDocument
                    {
                        ClusterNumber = newClusterNumber,
                        //ClusterId = newClusterNumber, // 병합된 클러스터는 자신의 번호가 ClusterId
                        ClusterId = _parentClusterId, // 병합된 클러스터는 자신의 번호가 ClusterId
                        ClusterSubId = newClusterNumber, // 병합된 클러스터는 자신의 번호가 ClusterId
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
                        await clusteringRepo.UpdateClusterSubIdAsync(childNumber, newClusterNumber);
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
                    //DataRow newRow = DataHandler.finalClusteringData.NewRow();
                    DataRow newRow = _detailClusteringData.NewRow();
                    newRow["ID"] = newClusterNumber;
                    newRow["ClusterID"] = _parentClusterId;
                    newRow["ClusterSubID"] = newClusterNumber;
                    newRow["클러스터명"] = combinedClusterName;
                    newRow["키워드목록"] = string.Join(",", keywordSet);
                    newRow["Count"] = totalCount;
                    newRow["합산금액"] = totalAmount;
                    newRow["dataIndex"] = string.Join(",", dataIndicesSet);
                    _detailClusteringData.Rows.Add(newRow);

                    // 하위 클러스터들의 ClusterID 업데이트
                    //foreach (DataRow row in DataHandler.finalClusteringData.Rows)
                    foreach (DataRow row in _detailClusteringData.Rows)
                    {
                        if (row["ClusterSubID"] != DBNull.Value)
                        {
                            int rowClusterId = Convert.ToInt32(row["ClusterSubID"]);
                            // 병합 대상 클러스터를 참조하는 행들의 ClusterID 변경
                            if (checkedClusterIds.Contains(rowClusterId))
                            {
                                row["ClusterSubID"] = newClusterNumber;
                            }
                        }
                    }

                    // 병합 대상 상위 클러스터 행 삭제
                    for (int i = _detailClusteringData.Rows.Count - 1; i >= 0; i--)
                    {
                        DataRow row = _detailClusteringData.Rows[i];
                        int rowId = Convert.ToInt32(row["ID"]);

                        // 병합 대상 클러스터 행 삭제
                        if (checkedClusterIds.Contains(rowId))
                        {
                            _detailClusteringData.Rows.RemoveAt(i);
                        }
                    }

                    // 변경사항 적용
                    _detailClusteringData.AcceptChanges();

                    await progressForm.UpdateProgressHandler(90, "데이터 새로고침 중");

                    // 데이터 다시 불러오기
                    mergeClusterDataTable = await EnrichWithRawTableDataAsync(_detailClusteringData);

                    
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

        // 5. 클러스터 세부 정보 표시 메서드 추가
        // 5. ShowMergeClusterDetail 함수 수정
        private void ShowMergeClusterDetail()
        {
            // 체크된 행에서 클러스터 ID 가져오기
            List<int> checkedClusterIds = new List<int>();

            foreach (DataGridViewRow row in merge_check_table.Rows)
            {
                if (row.Cells["CheckBox"].Value != null &&
                    Convert.ToBoolean(row.Cells["CheckBox"].Value) == true)
                {
                    if (row.Cells["ID"] != null && row.Cells["ID"].Value != null)
                    {
                        int clusterId = Convert.ToInt32(row.Cells["ID"].Value);

                        // 클러스터 ID가 자신과 동일한지 확인 (병합된 클러스터인 경우)
                        if (row.Cells["ClusterSubID"] != null && row.Cells["ClusterSubID"].Value != null)
                        {
                            int clusterSubID = Convert.ToInt32(row.Cells["ClusterSubID"].Value);
                            if (clusterId == clusterSubID)
                            {
                                checkedClusterIds.Add(clusterId);
                            }
                        }
                    }
                }
            }

            if (checkedClusterIds.Count == 0)
            {
                MessageBox.Show("세부 정보를 확인할 병합된 클러스터를 선택해주세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (checkedClusterIds.Count > 1)
            {
                MessageBox.Show("세부 정보는 한 번에 하나의 클러스터만 확인할 수 있습니다.\n여러 클러스터가 선택되었습니다.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 선택된 클러스터 ID로 세부 정보 표시
            int selectedClusterId = checkedClusterIds[0];

            try
            {
                //세부클러스터링용 데이터 복사
                DataHandler.subClusteringData = null;
                DataHandler.subClusteringData = _detailClusteringData.Copy();
                // 새 팝업 창 생성
                using (ClusterDetailPopup popup = new ClusterDetailPopup(true))
                {
                    
                    
                    // 통화 단위 설정
                    double divider = Math.Pow(1000, decimal_combo.SelectedIndex);
                    if (decimal_combo.SelectedIndex == 3)
                        divider = divider / 10; // 억 원은 10 나누기

                    popup.SetDecimalDivider((decimal)divider, decimal_combo.SelectedItem.ToString());

                    // 병합 해제 이벤트 등록 - 이 부분이 중요합니다!
                    popup.UnmergeCompleted += async (sender, e) =>
                    {
                        // UI 갱신
                        if (e.RefreshRequired)
                        {
                            // 메모리 데이터는 이미 업데이트되었으므로 UI만 갱신
                            Debug.WriteLine("popup.UnmergeCompleted start");

                            //데이터 갱신
                            _detailClusteringData = DataHandler.subClusteringData.Copy();
                            _detailClusteringData.AcceptChanges();

                            // 이 부분이 중요합니다!
                            mergeClusterDataTable = await EnrichWithRawTableDataAsync(_detailClusteringData);

                            await _clusteringManager.RefreshDataAsync(mergeClusterDataTable);
                            var searchCriteria = CreateSearchCriteriaFromCurrentUI();
                            await _clusteringManager.SearchAsync(searchCriteria , true);

                            // UI 스레드에서 실행
                            if (this.InvokeRequired)
                            {
                                Debug.WriteLine("this.InvokeRequired => true");
                                this.Invoke(new Action(() =>
                                {


                                    // 화면 갱신
                                    Debug.WriteLine("this.InvokeRequired => true => create_merge_keyword_list();");
                                    create_merge_keyword_list(true);

                                    Debug.WriteLine("this.InvokeRequired => true => create_check_keyword_list();");
                                    create_check_keyword_list();

                                    Debug.WriteLine("this.InvokeRequired => true => change_row_count();");
                                    // 행 수 갱신
                                    change_row_count();

                                    Debug.WriteLine("this.InvokeRequired => true => UpdateModifiedDataGridView();");

                                    // 병합 작업 후 업데이트
                                    UpdateModifiedDataGridView();
                                    UpdateSupplySummaryDataGridView();
                                }));
                            }
                            else
                            {
                                Debug.WriteLine("this.InvokeRequired => false");
                                // 화면 갱신
                                Debug.WriteLine("this.InvokeRequired => false =>create_merge_keyword_list()");
                                create_merge_keyword_list(true);

                                Debug.WriteLine("this.InvokeRequired => false =>create_check_keyword_list()");
                                create_check_keyword_list();

                                Debug.WriteLine("this.InvokeRequired => false =>change_row_count()");
                                // 행 수 갱신
                                change_row_count();

                                Debug.WriteLine("this.InvokeRequired => false =>UpdateModifiedDataGridView()");
                                // 병합 작업 후 업데이트
                                UpdateModifiedDataGridView();
                                UpdateSupplySummaryDataGridView();
                            }
                        }

                        Debug.WriteLine("e.RefreshRequired finished");
                    };

                    Debug.WriteLine("popup.ShowClusterDetail(selectedClusterId).ConfigureAwait(false);");
                    // 세부 정보 표시 및 팝업 표시
                    popup.ShowClusterDetail(selectedClusterId).ConfigureAwait(false);
                    popup.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터 세부 정보 표시 오류: {ex.Message}");
                MessageBox.Show($"클러스터 세부 정보를 불러오는 중 오류가 발생했습니다.\n{ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                                    await _clusteringManager.SearchAsync(searchCriteria , true);

                                    // 검색 결과에서 cluster_id == -1인 항목만 필터링하여 병합 대상 수집
                                    var currentResultIds = _clusteringManager.GetCurrentResultClusterIds();
                                    List<int> validClusterIds = new List<int>();

                                    foreach (int clusterId in currentResultIds)
                                    {
                                        // DataHandler.finalClusteringData에서 해당 클러스터의 상태 확인
                                        var clusterRow = _detailClusteringData.AsEnumerable()
                                            .FirstOrDefault(row => Convert.ToInt32(row["ID"]) == clusterId);

                                        if (clusterRow != null)
                                        {
                                            int clusterIdValue = Convert.ToInt32(clusterRow["ClusterSubID"]);
                                            // cluster_id == -1인 미병합 상태인 경우만 추가
                                            if (clusterIdValue == -1)
                                            {
                                                validClusterIds.Add(clusterId);
                                            }
                                        }
                                    }

                                    //await MergeAndCreateNewCluster(DataHandler.finalClusteringData, validClusterIds, keyword);
                                    await MergeAndCreateNewCluster(_detailClusteringData, validClusterIds, keyword);
                                    
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

        /////////////////////////////검색 헬퍼 메서드///////////////////////////////////

        private ParsedKeywords ParseComplexKeywords(string searchText)
        {
            // uc_Clustering과 동일한 로직
            var result = new ParsedKeywords();

            if (string.IsNullOrEmpty(searchText))
                return result;

            if (searchText.Contains("|"))
            {
                result.OrKeywords = searchText.Split('|')
                    .SelectMany(group => group.Split(','))
                    .Select(k => k.Trim())
                    .Where(k => !string.IsNullOrEmpty(k))
                    .ToList();
            }
            else
            {
                result.AndKeywords = searchText.Split(',')
                    .Select(k => k.Trim())
                    .Where(k => !string.IsNullOrEmpty(k))
                    .ToList();
            }

            return result;
        }




    }
}
