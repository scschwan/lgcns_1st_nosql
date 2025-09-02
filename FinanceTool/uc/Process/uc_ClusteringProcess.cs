using FinanceTool.MongoModels;
using FinanceTool.Repositories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTool
{
    public partial class uc_Clustering
    {
       
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
            try
            {
                isCheckedTableObject = true;
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


        // 공급업체 키워드 추출 함수
        public List<string> ExtractUniqueSupplierKeywords(DataTable dataTable, int checkFlag)
        {
            HashSet<string> uniqueKeywords = new HashSet<string>();

            // dataTable이 null이거나 공급업체 컬럼이 없으면 빈 리스트 반환
            if (dataTable == null)
            {
                Debug.WriteLine($"ExtractUniqueSupplierKeywords: dataTable이 null입니다.");
                return new List<string>();
            }

            if (!dataTable.Columns.Contains(DataHandler.prod_col_name))
            {
                Debug.WriteLine($"ExtractUniqueSupplierKeywords: dataTable에 {DataHandler.prod_col_name} 컬럼이 없거나 테이블이 null입니다.");
                return new List<string>();
            }

            foreach (DataRow row in dataTable.Rows)
            {
                if (!row.IsNull("ClusterID") && !row.IsNull("ID"))
                {
                    int clusterId = Convert.ToInt32(row["ClusterID"]);
                    int id = Convert.ToInt32(row["ID"]);

                    //*** 핵심 수정: checkFlag = 0일 때 병합되지 않은 클러스터만 포함 ***
                    if (checkFlag == 0)
                    {
                        // 병합되지 않은 클러스터만 포함: ClusterID <= 0 또는 ClusterID == ID
                        if (clusterId > 0 && clusterId != id)
                        {
                            // 이미 다른 클러스터에 병합된 하위 클러스터는 제외
                            continue;
                        }
                    }
                    else if (checkFlag == 1)
                    {
                        // 병합된 클러스터만 포함: ClusterID > 0 && ClusterID == ID
                        if (clusterId <= 0 || clusterId != id)
                        {
                            continue;
                        }
                    }
                }
                else
                {
                    // ClusterID나 ID가 없는 데이터 처리
                    if (checkFlag == 1)
                    {
                        // check table에서는 클러스터 정보가 없는 데이터는 skip
                        continue;
                    }
                }

                // 공급업체 컬럼 데이터 가져오기 (null 체크 포함)
                string supplierValue = row[DataHandler.prod_col_name]?.ToString();

                if (!string.IsNullOrEmpty(supplierValue))
                {
                    // 공급업체 값을 그대로 추가 (쉼표로 나누지 않음)
                    string trimmedValue = supplierValue.Trim();
                    if (!string.IsNullOrEmpty(trimmedValue))
                    {
                        uniqueKeywords.Add(trimmedValue);
                    }
                }
            }

            // HashSet을 List로 변환하여 반환 (정렬된 상태로)
            return uniqueKeywords.OrderBy(k => k).ToList();
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
                if (!row.IsNull("ClusterID"))  // ClusterID가 null이 아니고
                {
                    int clusterId = Convert.ToInt32(row["ClusterID"]);
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
            // ClusterID 컬럼 숨기기
            dgv.Columns["ClusterID"].Visible = false;

            // ClusterSubID 컬럼 숨기기
            dgv.Columns["ClusterSubID"].Visible = false;

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

        // 최대 속도 클러스터 통합 처리 (리소스 과다 사용)
        private async Task ProcessMaxSpeedClusterMergeAsync(int clusterCount,
            ProcessProgressForm.UpdateProgressDelegate progress)
        {
            try
            {
                await progress(10, $"클러스터링 처리 시작... ({clusterCount}개 클러스터)");

                var currentTargetIds = new ConcurrentBag<int>();
                var unmergedClusters = new ConcurrentBag<ClusteringResultDocument>();
                int processedCount = 0;
                int errorCount = 0;
                int mergedCount = 0;

                Debug.WriteLine($"DataTable 총 행 수: {DataHandler.finalClusteringData.Rows.Count}");

                await Task.Run(() =>
                {
                    Parallel.ForEach(DataHandler.finalClusteringData.AsEnumerable(),
                        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 4 },
                        row =>
                        {
                            try
                            {
                                // 안전한 데이터 추출
                                if (row["ID"] == null || row["ID"] == DBNull.Value ||
                                    row["ClusterID"] == null || row["ClusterID"] == DBNull.Value)
                                {
                                    Debug.WriteLine("null 값이 포함된 행 발견");
                                    Interlocked.Increment(ref errorCount);
                                    return;
                                }

                                if (!int.TryParse(row["ID"].ToString(), out int id) ||
                                    !int.TryParse(row["ClusterID"].ToString(), out int clusterId))
                                {
                                    Debug.WriteLine($"변환 실패: ID={row["ID"]}, ClusterID={row["ClusterID"]}");
                                    Interlocked.Increment(ref errorCount);
                                    return;
                                }

                                if (clusterId < 0) // 미병합 클러스터
                                {
                                    var clusterDoc = new ClusteringResultDocument
                                    {
                                        ClusterNumber = id,
                                        ClusterName = row["클러스터명"]?.ToString() ?? "",
                                        Keywords = row["키워드목록"]?.ToString()?.Split(',')
                                            .Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)).ToList() ?? new List<string>(),
                                        Count = Convert.ToInt32(row["Count"]),
                                        TotalAmount = Convert.ToDecimal(row["합산금액"]),
                                        DataIndices = row["dataIndex"]?.ToString()?.Split(',')
                                            .Select(i => i.Trim()).Where(i => !string.IsNullOrEmpty(i)).ToList() ?? new List<string>()
                                    };
                                    unmergedClusters.Add(clusterDoc);
                                    currentTargetIds.Add(id);
                                }
                                else
                                {
                                    Interlocked.Increment(ref mergedCount);
                                    //Debug.WriteLine($"병합된 클러스터: id {id}, clusterId: {clusterId}");
                                }

                                Interlocked.Increment(ref processedCount);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"행 처리 오류: {ex.Message}");
                                Interlocked.Increment(ref errorCount);
                            }
                        }
                    );
                });

                var targetIdsList = currentTargetIds.ToList();

                Debug.WriteLine($"=== 처리 결과 통계 ===");
                Debug.WriteLine($"DataTable 총 행 수: {DataHandler.finalClusteringData.Rows.Count}");
                Debug.WriteLine($"처리된 행 수: {processedCount}");
                Debug.WriteLine($"오류 행 수: {errorCount}");
                Debug.WriteLine($"병합된 클러스터 수: {mergedCount}");
                Debug.WriteLine($"미병합 클러스터 수: {currentTargetIds.Count}");
                Debug.WriteLine($"예상 데이터 수: 1308개");

                var unmergedList = unmergedClusters.ToList();
                if (unmergedList.Count == 0) return;

                await progress(40, $"데이터 통합 중... ({unmergedList.Count}개 클러스터)");

                // 2단계: *** 단일 스레드에서 통합 클러스터 생성 ***
                var clusteringRepo = new ClusteringRepository();

                // 새 통합 클러스터 번호 생성 (1개만)
                int unifiedClusterNumber = await clusteringRepo.GetNextClusterNumberAsync();

                // 모든 데이터를 단일 클러스터로 통합
                var allKeywords = new HashSet<string>();
                var allDataIndices = new HashSet<string>();
                int totalCount = 0;
                decimal totalAmount = 0;

                foreach (var cluster in unmergedList)
                {
                    // 키워드 통합 (중복 제거)
                    foreach (var keyword in cluster.Keywords)
                    {
                        allKeywords.Add(keyword);
                    }

                    // 데이터 인덱스 통합 (중복 제거)
                    foreach (var index in cluster.DataIndices)
                    {
                        allDataIndices.Add(index);
                    }

                    // 카운트 및 금액 누적
                    totalCount += cluster.Count;
                    totalAmount += cluster.TotalAmount;
                }

                // 3단계: 단일 "Undefined" 클러스터 생성
                var unifiedCluster = new ClusteringResultDocument
                {
                    ClusterNumber = unifiedClusterNumber,
                    ClusterId = unifiedClusterNumber, // 자신이 상위 클러스터
                    ClusterName = "Undefined",
                    Keywords = allKeywords.ToList(),
                    Count = totalCount,
                    TotalAmount = totalAmount,
                    DataIndices = allDataIndices.ToList(),
                    CreatedAt = DateTime.Now
                };

                await progress(50, "통합 클러스터 생성 중...");

                // MongoDB에 단일 클러스터 생성
                await clusteringRepo.CreateAsync(unifiedCluster);

                await progress(75, "클러스터 관계 업데이트 중...");

                // 4단계: 병렬로 각 미병합 클러스터의 ClusterId를 통합 클러스터로 업데이트
                var updateTasks = unmergedList.Select(async cluster =>
                {
                    try
                    {
                        await clusteringRepo.UpdateClusterIdAsync(cluster.ClusterNumber, unifiedClusterNumber);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"클러스터 {cluster.ClusterNumber} 업데이트 오류: {ex.Message}");
                    }
                }).ToArray();

                await Task.WhenAll(updateTasks);

                await progress(90, "메모리 데이터 동기화 중...");

                // *** 6단계: DataTable 업데이트 (기존 행 업데이트 + 병합된 클러스터들의 ClusterID 변경) ***
                await UpdateDataTableAfterMerge(DataHandler.finalClusteringData, targetIdsList, unifiedClusterNumber, true);

                // *** 7단계: 데이터 보강 (동기적 처리로 일관성 보장) ***
                mergeClusterDataTable = await EnrichWithRawTableDataAsync(DataHandler.finalClusteringData);

                Debug.WriteLine($"통합 클러스터 생성 완료: ID={unifiedClusterNumber}, Count={totalCount}, Amount={totalAmount}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"극한 속도 클러스터 처리 오류: {ex.Message}");
                throw;
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


        //2025.04.25
        //추천 키워드 갱신 함수
        // uc_clustering.cs에 추가할 새 메서드
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
                    .Where(row => row.Field<int>("ClusterID") == -1)
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
                    .Where(row => row.Field<int>("ClusterID") == -1)
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
                        if (row.Cells["ClusterID"] != null && row.Cells["ClusterID"].Value != null)
                        {
                            int clusterID = Convert.ToInt32(row.Cells["ClusterID"].Value);
                            if (clusterId == clusterID)
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
                // 새 팝업 창 생성
                using (ClusterDetailPopup popup = new ClusterDetailPopup())
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

                            // 이 부분이 중요합니다!
                            mergeClusterDataTable = await EnrichWithRawTableDataAsync(DataHandler.finalClusteringData);

                            await _clusteringManager.RefreshDataAsync(mergeClusterDataTable);
                            var searchCriteria = CreateSearchCriteriaFromCurrentUI();
                            await _clusteringManager.SearchAsync(searchCriteria);

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


       
    }
}
