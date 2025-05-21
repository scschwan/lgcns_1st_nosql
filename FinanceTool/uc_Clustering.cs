﻿using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using FinanceTool;
using FinanceTool.MongoModels;
using FinanceTool.Repositories;
using Microsoft.VisualBasic.Devices;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace FinanceTool
{
    public partial class uc_Clustering : UserControl
    {

        DataTable mergeClusterDataTable;
        DataTable checkClusterDataTable;

        private decimal decimalDivider = 1;
        private string decimalDividerName = "원";
        private string selectecLv1Name = "";
        private bool equalsSearchYN = false;
        private bool andSearchYN = false;

        private bool isFinishSession = false;

        List<string> merge_keyword_list;
        List<string> check_keyword_list;
        List<string> supplier_keyword_list;

        // 전역 인스턴스 생성
        private static RecomandKeywordManager _recomandKeywordManager;



        public uc_Clustering()
        {
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
                // merge_check_table에서 체크된 항목이 있는지 확인
                bool hasCheckedItems = false;
                foreach (DataGridViewRow row in merge_check_table.Rows)
                {
                    if (row.Cells["CheckBox"].Value != null && Convert.ToBoolean(row.Cells["CheckBox"].Value))
                    {
                        hasCheckedItems = true;
                        break;
                    }
                }

                if (hasCheckedItems)
                {
                    ShowMergeClusterDetail();
                }
                else if (merge_check_table.SelectedRows.Count > 0)
                {
                    // 선택된 행이 있으면 해당 행을 체크하고 세부 정보 표시
                    DataGridViewRow row = merge_check_table.SelectedRows[0];
                    row.Cells["CheckBox"].Value = true;
                    ShowMergeClusterDetail();
                }
                else
                {
                    MessageBox.Show("세부 정보를 확인할 클러스터를 선택해주세요.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
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

                // RawData 정보로 보강
                mergeClusterDataTable = await EnrichWithRawTableDataAsync(DataHandler.finalClusteringData);


                // 검색 UI 초기화
                set_keyword_combo_list();
                create_merge_keyword_list(true);

                // 최초 수행 시 별도 수행
                supplier_keyword_list = ExtractUniqueSupplierKeywords(mergeClusterDataTable, 0);

                // 메인 UI 스레드로 돌아가서 DataGridView 설정
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

                            if (dataGridView_modified.Rows.Count > 0)
                            {
                                dataGridView_modified.DataSource = null;
                                dataGridView_modified.Rows.Clear();
                                dataGridView_modified.Columns.Clear();
                            }

                            // 초기 데이터 로드 후 업데이트
                            UpdateModifiedDataGridView();

                            // DataGridView 속성 설정
                            dataGridView_modified.AllowUserToAddRows = false;
                            dataGridView_modified.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                            dataGridView_modified.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                            dataGridView_modified.Font = new System.Drawing.Font("맑은 고딕", 14.25F);

                            // 나머지 컬럼들은 읽기 전용으로 설정
                            for (int i = 1; i < dataGridView_modified.Columns.Count; i++)
                            {
                                dataGridView_modified.Columns[i].ReadOnly = true;
                            }

                            dataGridView_modified.CellClick += dataGridView_keyword_CellClick;
                            dataGridView_modified.SortCompare += DataHandler.money_SortCompare;

                            Debug.WriteLine("LoadSeparatorsAndRemovers");
                            LoadSeparatorsAndRemovers();
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"initUI 메서드 오류: {ex.Message}");
                MessageBox.Show($"클러스터링 데이터 로드 중 오류가 발생했습니다.\n{ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // MongoDB에 클러스터링 데이터 저장하는 새 헬퍼 메서드
        private async Task SaveClusteringDataToMongoDBAsync(DataTable clusteringData)
        {
            try
            {
                var clusteringRepo = new ClusteringRepository();
                List<ClusteringResultDocument> documents = new List<ClusteringResultDocument>();

                foreach (DataRow row in clusteringData.Rows)
                {
                    int clusterId = -1;
                    int clusterNumber = Convert.ToInt32(row["ID"]);

                    // ClusterID 처리 (병합 상태 확인)
                    if (row["ClusterID"] != DBNull.Value)
                    {
                        clusterId = Convert.ToInt32(row["ClusterID"]);
                    }

                    var clusterDoc = new ClusteringResultDocument
                    {
                        ClusterNumber = clusterNumber,
                        ClusterId = clusterId,
                        ClusterName = row["클러스터명"].ToString(),
                        Keywords = row["키워드목록"].ToString().Split(',').Select(k => k.Trim()).ToList(),
                        Count = Convert.ToInt32(row["Count"]),
                        TotalAmount = Convert.ToDecimal(row["합산금액"])
                    };

                    // dataIndex 처리
                    if (!row.IsNull("dataIndex") && !string.IsNullOrEmpty(row["dataIndex"].ToString()))
                    {
                        clusterDoc.DataIndices = row["dataIndex"].ToString()
                                               .Split(',')
                                               .Select(id => id.Trim())
                                               .Where(id => !string.IsNullOrEmpty(id))
                                               .ToList();
                    }

                    documents.Add(clusterDoc);
                }

                // 데이터 일괄 저장
                if (documents.Count > 0)
                {
                    await clusteringRepo.CreateManyAsync(documents);
                    Debug.WriteLine($"{documents.Count}개의 클러스터 데이터를 MongoDB에 저장했습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터 데이터 MongoDB 저장 오류: {ex.Message}");
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
            dgv.Font = new System.Drawing.Font("맑은 고딕", 14.25F);

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


        private void dataGridView_keyword_CellClick(object sender, DataGridViewCellEventArgs e)
        {

            DataGridView dgv = sender as DataGridView;
            int valueIndex = 0;
            if (dgv == null) return;

            //키워드 요약 테이블은 0번, 키워드 추천 테이블은 1번
            // 그리드뷰 이름에 따라 valueIndex 설정
            if (dgv.Name == "dataGridView_modified")
            {
                valueIndex = 0; // dataGridView_modified일 경우 0번 인덱스 사용
            }
            else if (dgv.Name == "dataGridView_recoman_keyword")
            {
                valueIndex = 1; // dataGridView_recoman_keyword일 경우 1번 인덱스 사용
            }
            else
            {
                Debug.WriteLine($"Unknown DataGridView: {dgv.Name}");
                return; // 알 수 없는 그리드뷰인 경우 처리 중단
            }

            Debug.WriteLine($"DataGridView_CellContentClick start => dgv.Name : {dgv.Name} , valueIndex : {valueIndex}");
            // 체크박스 컬럼이 아닌 다른 컬럼 클릭 시
            //if (e.ColumnIndex == 0 && e.RowIndex >= 0)
            if (e.ColumnIndex == valueIndex && e.RowIndex >= 0)
            {
                string keyword = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();

                List<string> MathcingPairs = new List<string>();

                Debug.WriteLine($"selected keyword : {keyword}");

                //2025.03.07 -정확히 일치하는 항목만 검색
                //MathcingPairs = DataHandler.FindMachKeyword(merge_keyword_list, keyword);
                MathcingPairs = DataHandler.FindMachEqualsKeyword(merge_keyword_list, keyword);


                if (MathcingPairs.Count > 0)
                {
                    if (mergeClusterDataTable == null || mergeClusterDataTable.Columns.Count == 0)
                    {
                        Debug.WriteLine("mergeClusterDataTable이 null이거나 컬럼이 없습니다. CreateFilteredDataGridView 호출을 건너뜁니다.");
                        return; // 또는 적절한 오류 처리
                    }

                    //CreateFilteredDataGridView(merge_cluster_table, DataHandler.finalClusteringData, MathcingPairs);
                    if (!"".Equals(except_keyword.Text))
                    {
                        CreateFilteredDataGridView(merge_cluster_table, mergeClusterDataTable, MathcingPairs, except_keyword.Text.ToString());
                    }
                    else
                    {
                        CreateFilteredDataGridView(merge_cluster_table, mergeClusterDataTable, MathcingPairs);
                    }


                    merge_all_check.Checked = false;

                    change_row_count();
                }
                else
                {
                    merge_cluster_table.DataSource = null;
                    merge_cluster_table.Rows.Clear();
                    merge_cluster_table.Columns.Clear();

                    if (DataHandler.dragSelections.ContainsKey(merge_cluster_table))
                    {
                        DataHandler.dragSelections[merge_cluster_table].Clear();
                    }

                    change_row_count();
                }

                //테이블 표기 순서 변경
                // 맨 오른쪽으로 보낼 컬럼들
                /*
                List<string> columnsToMoveRight = new List<string> { "클러스터명", "키워드목록" };

                foreach (string columnName in columnsToMoveRight)
                {
                    if (merge_cluster_table.Columns.Contains(columnName))
                    {
                        // 해당 컬럼을 맨 마지막 인덱스로 이동
                        merge_cluster_table.Columns[columnName].DisplayIndex = merge_cluster_table.Columns.Count - 1;
                    }
                }
                */


            }
        }


        public async Task<DataTable> EnrichWithRawTableDataAsync(DataTable inputTable)
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


        private async void create_merge_keyword_list(bool isAlreadyProgress = false)
        {
            if (isAlreadyProgress)
            {
                string target_keyword = "";

                //combobox로 검색할 경우
                if (merge_search_radio1.Checked)
                {
                    if (merge_keyword_combo.SelectedIndex != 0)
                    {
                        target_keyword = merge_keyword_combo.SelectedItem.ToString();
                    }
                }
                //직접 검색할 경우
                else if (merge_search_radio2.Checked)
                {
                    if (!"".Equals(merge_search_keyword.Text.ToString()) && merge_search_keyword.Text != null)
                    {
                        target_keyword = merge_search_keyword.Text.ToString();
                    }
                }

                List<string> MathcingPairs = new List<string>();

                if (!"".Equals(target_keyword))
                {
                    //쉼표 포함 시 다중검색 기능 표기
                    if (target_keyword.Contains(","))
                    {
                        andSearchYN = true;
                    }

                    Debug.WriteLine($"andSearchYN : {andSearchYN} ,equalsSearchYN : {equalsSearchYN} ");

                    // 키워드 검색 또는 공급업체 검색 여부에 따라 다른 리스트 사용
                    List<string> searchList = keyword_radio1.Checked ? merge_keyword_list : supplier_keyword_list;

                    //완전 일치 검색
                    if (equalsSearchYN)
                    {
                        MathcingPairs = DataHandler.FindMachEqualsKeyword(searchList, target_keyword);
                    }
                    //포함 검색
                    else
                    {

                        MathcingPairs = DataHandler.FindMachKeyword(searchList, target_keyword);
                    }

                    if (MathcingPairs.Count == 0)
                    {
                        Debug.WriteLine($"such result == 0");
                        merge_cluster_table.DataSource = null;
                        merge_cluster_table.Rows.Clear();
                        merge_cluster_table.Columns.Clear();
                        if (DataHandler.dragSelections.ContainsKey(merge_cluster_table))
                        {
                            DataHandler.dragSelections[merge_cluster_table].Clear();
                        }

                        change_row_count();
                        return;
                    }

                    if (mergeClusterDataTable == null || mergeClusterDataTable.Columns.Count == 0)
                    {
                        Debug.WriteLine("mergeClusterDataTable이 null이거나 컬럼이 없습니다. CreateFilteredDataGridView 호출을 건너뜁니다.");
                        return; // 또는 적절한 오류 처리
                    }

                    if (!"".Equals(except_keyword.Text))
                    {
                        CreateFilteredDataGridView(merge_cluster_table, mergeClusterDataTable, MathcingPairs, except_keyword.Text.ToString(), keyword_radio2.Checked);
                    }
                    else
                    {
                        CreateFilteredDataGridView(merge_cluster_table, mergeClusterDataTable, MathcingPairs, null, keyword_radio2.Checked);
                    }
                }
                //전체 검색
                else
                {
                    if (mergeClusterDataTable == null || mergeClusterDataTable.Columns.Count == 0)
                    {
                        Debug.WriteLine("mergeClusterDataTable이 null이거나 컬럼이 없습니다. CreateFilteredDataGridView 호출을 건너뜁니다.");
                        return; // 또는 적절한 오류 처리
                    }

                    CreateFilteredDataGridView(merge_cluster_table, mergeClusterDataTable, MathcingPairs, null, keyword_radio2.Checked);
                }

                merge_all_check.Checked = false;
                andSearchYN = false;
                change_row_count();
            }
            else
            {
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();

                    string target_keyword = "";

                    //combobox로 검색할 경우
                    if (merge_search_radio1.Checked)
                    {
                        if (merge_keyword_combo.SelectedIndex != 0)
                        {
                            target_keyword = merge_keyword_combo.SelectedItem.ToString();
                        }
                    }
                    //직접 검색할 경우
                    else if (merge_search_radio2.Checked)
                    {
                        if (!"".Equals(merge_search_keyword.Text.ToString()) && merge_search_keyword.Text != null)
                        {
                            target_keyword = merge_search_keyword.Text.ToString();
                        }
                    }

                    await progressForm.UpdateProgressHandler(10, "데이터 검색 시작");
                    await Task.Delay(10);

                    List<string> MathcingPairs = new List<string>();

                    if (!"".Equals(target_keyword))
                    {
                        //쉼표 포함 시 다중검색 기능 표기
                        if (target_keyword.Contains(","))
                        {
                            andSearchYN = true;
                        }

                        Debug.WriteLine($"andSearchYN : {andSearchYN} ,equalsSearchYN : {equalsSearchYN} ");

                        // 키워드 검색 또는 공급업체 검색 여부에 따라 다른 리스트 사용
                        List<string> searchList = keyword_radio1.Checked ? merge_keyword_list : supplier_keyword_list;

                        //완전 일치 검색
                        if (equalsSearchYN)
                        {
                            MathcingPairs = DataHandler.FindMachEqualsKeyword(searchList, target_keyword);
                        }
                        //포함 검색
                        else
                        {
                            MathcingPairs = DataHandler.FindMachKeyword(searchList, target_keyword);
                        }

                        if (MathcingPairs.Count == 0)
                        {
                            Debug.WriteLine($"such result == 0");
                            merge_cluster_table.DataSource = null;
                            merge_cluster_table.Rows.Clear();
                            merge_cluster_table.Columns.Clear();
                            if (DataHandler.dragSelections.ContainsKey(merge_cluster_table))
                            {
                                DataHandler.dragSelections[merge_cluster_table].Clear();
                            }

                            change_row_count();

                            await progressForm.UpdateProgressHandler(100);
                            await Task.Delay(10);
                            progressForm.Close();

                            return;
                        }

                        await progressForm.UpdateProgressHandler(40, "데이터 검색 중...");
                        await Task.Delay(10);

                        if (mergeClusterDataTable == null || mergeClusterDataTable.Columns.Count == 0)
                        {
                            Debug.WriteLine("mergeClusterDataTable이 null이거나 컬럼이 없습니다. CreateFilteredDataGridView 호출을 건너뜁니다.");
                            return; // 또는 적절한 오류 처리
                        }

                        if (!"".Equals(except_keyword.Text))
                        {
                            CreateFilteredDataGridView(merge_cluster_table, mergeClusterDataTable, MathcingPairs, except_keyword.Text.ToString(), keyword_radio2.Checked);
                        }
                        else
                        {
                            CreateFilteredDataGridView(merge_cluster_table, mergeClusterDataTable, MathcingPairs, null, keyword_radio2.Checked);
                        }
                    }
                    //전체 검색
                    else
                    {
                        await progressForm.UpdateProgressHandler(40, "데이터 검색 중...");
                        await Task.Delay(10);

                        if (mergeClusterDataTable == null || mergeClusterDataTable.Columns.Count == 0)
                        {
                            Debug.WriteLine("mergeClusterDataTable이 null이거나 컬럼이 없습니다. CreateFilteredDataGridView 호출을 건너뜁니다.");
                            return; // 또는 적절한 오류 처리
                        }

                        CreateFilteredDataGridView(merge_cluster_table, mergeClusterDataTable, MathcingPairs, null, keyword_radio2.Checked);
                    }

                    await progressForm.UpdateProgressHandler(90, "데이터 검색 완료");
                    await Task.Delay(10);

                    merge_all_check.Checked = false;
                    andSearchYN = false;
                    change_row_count();

                    await progressForm.UpdateProgressHandler(100);
                    await Task.Delay(10);
                    progressForm.Close();
                }
            }
        }

        private void change_row_count()
        {
            int rowCount = merge_cluster_table.RowCount;

            cluster_count.Text = $"행 수  : {rowCount}";

            int unClusterCount = GetCountOfNegativeOneClusterIDs(DataHandler.finalClusteringData);
            string unClusterCountMoney = GetSumOfNegativeTotalMoney(DataHandler.finalClusteringData);
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
            if (!dataTable.Columns.Contains("ClusterID"))
                return 0;

            // LINQ를 사용하여 ClusterID가 -1인 행 개수 계산
            int count = dataTable.AsEnumerable()
                                 .Count(row => row.Field<int>("ClusterID") == -1);

            return count;
        }

        public string GetSumOfNegativeTotalMoney(DataTable dataTable)
        {
            // DataTable이 null인지 확인
            if (dataTable == null)
                return FormatToKoreanUnit(0);

            // "ClusterID" 컬럼이 존재하는지 확인
            if (!dataTable.Columns.Contains("ClusterID"))
                return FormatToKoreanUnit(0);

            // "합산금액" 컬럼이 존재하는지 확인
            if (!dataTable.Columns.Contains("합산금액"))
                return FormatToKoreanUnit(0);

            // LINQ를 사용하여 ClusterID가 -1인 행들의 합산금액 총합 계산
            decimal sum = dataTable.AsEnumerable()
                                  .Where(row => row.Field<int>("ClusterID") == -1)
                                  .Sum(row => row.Field<decimal>("합산금액"));

            return FormatToKoreanUnit(sum);
        }

        private void create_check_keyword_list()
        {
            string target_keyword = "";

            //combobox로 검색할 경우
            if (check_search_radio1.Checked)
            {
                if (check_search_combo.SelectedIndex != 0)
                {
                    target_keyword = check_search_combo.SelectedItem.ToString();
                }
            }
            //직접 검색할 경우
            else if (check_search_radio2.Checked)
            {
                if (!"".Equals(check_search_keyword.Text.ToString()) && check_search_keyword.Text != null)
                {
                    target_keyword = check_search_keyword.Text.ToString();
                }
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
                        CreateCheckDataGridView(merge_check_table, DataHandler.finalClusteringData, MathcingPairs);
                    }

                }
                //전체 검색
                else
                {
                    CreateCheckDataGridView(merge_check_table, DataHandler.finalClusteringData, MathcingPairs);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }

        //checkFlag = 0 : merge table combo box
        //checkFlag = 1 : check table combo box
        public List<string> ExtractUniqueKeywords(DataTable dataTable, int checkFlag)
        {
            HashSet<string> uniqueKeywords = new HashSet<string>();

            foreach (DataRow row in dataTable.Rows)
            {


                if (!row.IsNull("ClusterID"))
                {
                    int clusterId = Convert.ToInt32(row["ClusterID"]);
                    //병합된 cluster의 키워드는 merge table combo box에서는 skip
                    if (checkFlag == 0 && clusterId > 0)
                    {
                        continue;
                    }

                    //clusterID가 없는 데이터는 check table combo box에서 skip
                    if (checkFlag == 1 && clusterId < 0)
                    {
                        continue;
                    }

                }
                else
                {
                    //clusterID가 없는 데이터는 check table combo box에서 skip
                    if (checkFlag == 1)
                    {
                        continue;
                    }
                }


                // 키워드목록 컬럼의 데이터 가져오기 (null 체크 포함)
                string keywordList = row["키워드목록"]?.ToString();

                if (!string.IsNullOrEmpty(keywordList))
                {
                    // 쉼표로 구분된 키워드를 분리하고 각각 HashSet에 추가
                    string[] keywords = keywordList.Split(',');
                    foreach (string keyword in keywords)
                    {
                        // 앞뒤 공백 제거 후 추가
                        string trimmedKeyword = keyword.Trim();
                        if (!string.IsNullOrEmpty(trimmedKeyword))
                        {
                            uniqueKeywords.Add(trimmedKeyword);
                        }
                    }
                }
            }

            // HashSet을 List로 변환하여 반환 (정렬된 상태로)
            return uniqueKeywords.OrderBy(k => k).ToList();
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
                if (!row.IsNull("ClusterID"))
                {
                    int clusterId = Convert.ToInt32(row["ClusterID"]);
                    //병합된 cluster의 키워드는 merge table combo box에서는 skip
                    if (checkFlag == 0 && clusterId > 0)
                    {
                        continue;
                    }

                    //clusterID가 없는 데이터는 check table combo box에서 skip
                    if (checkFlag == 1 && clusterId < 0)
                    {
                        continue;
                    }
                }
                else
                {
                    //clusterID가 없는 데이터는 check table combo box에서 skip
                    if (checkFlag == 1)
                    {
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

        private void set_keyword_combo_list()
        {
            merge_keyword_combo.Items.Clear();
            merge_keyword_combo.Items.Add("키워드 선택");

            check_search_combo.Items.Clear();
            check_search_combo.Items.Add("키워드 선택");

            // 키워드 리스트 생성
            merge_keyword_list = ExtractUniqueKeywords(DataHandler.finalClusteringData, 0);
            check_keyword_list = ExtractUniqueKeywords(DataHandler.finalClusteringData, 1);

            // 공급업체 리스트는 mergeClusterDataTable에서 생성
            if (mergeClusterDataTable != null && mergeClusterDataTable.Columns.Contains(DataHandler.prod_col_name))
            {
                supplier_keyword_list = ExtractUniqueSupplierKeywords(mergeClusterDataTable, 0);
                Debug.WriteLine($"공급업체 리스트 생성 완료: {supplier_keyword_list.Count}개 항목");
            }
            else
            {
                supplier_keyword_list = new List<string>();
                Debug.WriteLine("공급업체 리스트 생성 실패: mergeClusterDataTable이 null이거나 컬럼이 없음");
            }

            // 현재 선택된 검색 모드에 따라 콤보박스 내용 업데이트
            UpdateSearchComboBox();

            // 검증 콤보박스는 항상 키워드 기준
            foreach (string keyword in check_keyword_list)
            {
                if ("\\".Equals(keyword))
                {
                    continue;
                }
                check_search_combo.Items.Add(keyword);
            }

            check_search_combo.SelectedIndex = 0; // 첫 번째 열 선택
        }

        public void CreateFilteredDataGridView(DataGridView dgv, DataTable dt, List<string> filterWords, string except_keyword = null, bool isSupplierSearch = false)
        {
            // DataGridView 초기화
            dgv.DataSource = null;
            dgv.Rows.Clear();
            dgv.Columns.Clear();
            if (DataHandler.dragSelections.ContainsKey(dgv))
            {
                DataHandler.dragSelections[dgv].Clear();
            }

            Debug.WriteLine($"filterWords : {string.Join(",", filterWords)}");

            // CheckBox 컬럼 추가
            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn();
            checkColumn.Name = "CheckBox";
            checkColumn.HeaderText = "";
            checkColumn.Width = 50;
            checkColumn.ThreeState = false;
            checkColumn.Frozen = true;
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
                bool skipRow = false;
                if (!row.IsNull("ClusterID"))  // ClusterID가 null이 아니고
                {
                    int clusterId = Convert.ToInt32(row["ClusterID"]);
                    if (clusterId > 0)  // -1 이 아니면 skip
                    {
                        skipRow = true;
                    }
                }

                if (!skipRow)  // ClusterID 조건을 통과한 경우만 처리
                {
                    if (filterWords.Count > 0)
                    {
                        // 검색 대상 컬럼 결정 (키워드 또는 공급업체)
                        string searchColumnName = isSupplierSearch ? DataHandler.prod_col_name : "키워드목록";
                        string searchColumnValue = row[searchColumnName].ToString();

                        string searchColumnReplace = searchColumnValue.Replace(" ", "");

                        //제외 키워드 포함 시 해당 행 skip
                        //다중 키워드 기능 추가
                        if (!string.IsNullOrEmpty(except_keyword))
                        {
                            // 제외 키워드가 콤마로 구분되어 있는지 확인하고 리스트로 변환
                            List<string> exceptKeywordList = except_keyword.Split(',')
                                                            .Select(k => k.Trim())
                                                            .Where(k => !string.IsNullOrEmpty(k))
                                                            .ToList();

                            // 제외 키워드 목록 중 하나라도 포함되어 있으면 skip
                            if (exceptKeywordList.Any(exWord => searchColumnReplace.Contains(exWord)))
                            {
                                continue;
                            }
                        }

                        // 검색 방식에 따라 리스트 생성 방법 다르게 적용
                        List<string> valueList;
                        if (isSupplierSearch)
                        {
                            // 공급업체는 그냥 문자열 자체 사용
                            valueList = new List<string> { searchColumnValue.Trim() };
                        }
                        else
                        {
                            // 키워드 목록은 쉼표로 구분
                            valueList = searchColumnValue.Split(',')
                                                   .Select(k => k.Trim())
                                                   .Where(k => !string.IsNullOrEmpty(k))
                                                   .ToList();
                        }

                        //쉼표 기반 and 조건 검색 && 완전 일치일 경우
                        if (andSearchYN && equalsSearchYN)
                        {
                            // filterWords의 모든 단어가 valueList에 포함되어 있는지 확인 (대소문자 무시)
                            if (filterWords.All(filterWord => valueList.Any(value =>
                                string.Equals(value, filterWord, StringComparison.OrdinalIgnoreCase))))
                            {
                                int rowIndex = dgv.Rows.Add();
                                dgv.Rows[rowIndex].Cells["CheckBox"].Value = false;
                                for (int i = 0; i < dt.Columns.Count; i++)
                                {
                                    //합산금액 컬럼은 수정
                                    if ("합산금액".Equals(dt.Columns[i].ColumnName))
                                    {
                                        dgv.Rows[rowIndex].Cells[i + 1].Value = FormatToKoreanUnit(Convert.ToDecimal(row[i]));
                                    }
                                    else
                                    {
                                        dgv.Rows[rowIndex].Cells[i + 1].Value = row[i];
                                    }
                                }
                            }
                        }
                        //그 외 검색
                        else
                        {
                            // 공급업체 검색은 포함 여부만 체크, 키워드 검색은 정확한 일치 체크
                            bool match = false;
                            if (isSupplierSearch)
                            {
                                // 공급업체 검색은 포함 여부 체크
                                match = valueList.Any(value => filterWords.Any(filterWord =>
                                    value.IndexOf(filterWord, StringComparison.OrdinalIgnoreCase) >= 0));
                            }
                            else
                            {
                                // 키워드 검색은 정확한 일치 체크
                                match = valueList.Any(value => filterWords.Any(filterWord =>
                                    string.Equals(value, filterWord, StringComparison.OrdinalIgnoreCase)));
                            }

                            if (match)
                            {
                                int rowIndex = dgv.Rows.Add();
                                dgv.Rows[rowIndex].Cells["CheckBox"].Value = false;
                                for (int i = 0; i < dt.Columns.Count; i++)
                                {
                                    //합산금액 컬럼은 수정
                                    if ("합산금액".Equals(dt.Columns[i].ColumnName))
                                    {
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

            // ID 컬럼 숨기기
            dgv.Columns["ID"].Visible = false;
            // ClusterID 컬럼 숨기기
            dgv.Columns["ClusterID"].Visible = false;

            // dataIndex 컬럼 숨기기
            dgv.Columns["dataIndex"].Visible = false;

            // import_date 컬럼 숨기기
            if (dgv.Columns["import_date"] != null)
            {
                dgv.Columns["import_date"].Visible = false;
            }

            if (dgv.Columns["Count"] != null)
            {
                dgv.Columns["Count"].DefaultCellStyle.Format = "N0"; // 천 단위 구분자
                dgv.Columns["Count"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            // DataGridView 속성 설정
            dgv.AllowUserToAddRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.ReadOnly = false;

            dgv.Columns["CheckBox"].ReadOnly = false;  // 체크박스 컬럼만 편집 가능

            // 나머지 컬럼들은 읽기 전용으로 설정
            for (int i = 1; i < dgv.Columns.Count; i++)
            {
                dgv.Columns[i].ReadOnly = true;
            }

            dgv.Columns["클러스터명"].ReadOnly = true;

            // 컬럼 너비 고정 설정

            // 클러스터명 컬럼의 너비 고정 (200픽셀로 설정)
            if (dgv.Columns["CheckBox"] != null)
            {
                dgv.Columns["CheckBox"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dgv.Columns["CheckBox"].Resizable = DataGridViewTriState.False;
            }

            // 클러스터명 컬럼의 너비 고정 (200픽셀로 설정)
            if (dgv.Columns["클러스터명"] != null)
            {
                dgv.Columns["클러스터명"].Width = 400;
                dgv.Columns["클러스터명"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            }

            // 타겟 컬럼 너비 고정
            if (DataHandler.levelName.Count > 0)
            {
                string lastLevelName = DataHandler.levelName[DataHandler.levelName.Count - 1];
                if (dgv.Columns[lastLevelName] != null)
                {
                    dgv.Columns[lastLevelName].Width = 400;
                    dgv.Columns[lastLevelName].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                }
            }

            // 공급업체 컬럼 너비 고정
            /*
            if (dgv.Columns[DataHandler.prod_col_name] != null)
            {
                dgv.Columns[DataHandler.prod_col_name].Width = 200;
                dgv.Columns[DataHandler.prod_col_name].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            }
            */

            // 나머지 컬럼은 자동 크기 조정
            for (int i = 1; i < dgv.Columns.Count; i++)
            {
                string colName = dgv.Columns[i].Name;
                if (colName != "클러스터명" &&
                    colName != DataHandler.prod_col_name &&
                    (DataHandler.levelName.Count == 0 || colName != DataHandler.levelName[DataHandler.levelName.Count - 1]))
                {
                    dgv.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                }
            }
            dgv.Font = new System.Drawing.Font("맑은 고딕", 9F);

            dgv.SortCompare -= DataHandler.money_SortCompare;
            dgv.SortCompare += DataHandler.money_SortCompare;

            //2025.04.28
            // 컬럼 순서 재배치
            //선택박스, Count수, 세목열, 타겟열, 공급업체열, 부서열,금액열
            List<string> desiredOrder = new List<string>
                {
                    "CheckBox",
                    "Count",
                    DataHandler.sub_acc_col_name,
                    DataHandler.levelName[DataHandler.levelName.Count - 1],
                    DataHandler.prod_col_name,
                    DataHandler.dept_col_name,
                    "합산금액"
                };

            // 기존 컬럼 위치 저장
            Dictionary<string, int> originalIndices = new Dictionary<string, int>();
            for (int i = 0; i < dgv.Columns.Count; i++)
            {
                originalIndices[dgv.Columns[i].Name] = i;
            }

            // 새로운 컬럼 순서 설정
            for (int i = 0; i < desiredOrder.Count; i++)
            {
                string colName = desiredOrder[i];
                if (dgv.Columns.Contains(colName))
                {
                    dgv.Columns[colName].DisplayIndex = i;
                }
            }

            // 나머지 컬럼들은 기존 순서 유지
            // 나머지 컬럼들은 기존 순서 유지하되 우선 순위가 낮은 컬럼으로 배치
            var remainingColumns = dgv.Columns.Cast<DataGridViewColumn>()
                                     .Where(col => !desiredOrder.Contains(col.Name))
                                     .OrderBy(col => originalIndices[col.Name])
                                     .ToList();

            int nextIndex = desiredOrder.Count;
            int columnCount = dgv.Columns.Count;

            foreach (var col in remainingColumns)
            {
                // DisplayIndex가 열 개수보다 작은지 확인
                if (nextIndex < columnCount)
                {
                    col.DisplayIndex = nextIndex++;
                }
                else
                {
                    // 범위를 벗어나면 오류를 기록하고 처리를 중단
                    Debug.WriteLine($"DisplayIndex({nextIndex})가 열 개수({columnCount})를 초과했습니다. 열: {col.Name}");
                    break;
                }
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
                if (!row.IsNull("ClusterID"))  // ClusterID가 null이 아니고
                {
                    int clusterId = Convert.ToInt32(row["ClusterID"]);
                    int rowId = Convert.ToInt32(row["ID"]);
                    if (clusterId == rowId)  // 0이 아니면 스킵
                    {
                        skipRow = false;
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
            //dgv.Font = new System.Drawing.Font("맑은 고딕", 14.25F);
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
            List<int> checkedData = new List<int>();
            foreach (DataGridViewRow row in dgv.Rows)
            {
                // CheckBox 컬럼(0번째)이 체크되었는지 확인
                if (row.Cells[0].Value != null &&
                    Convert.ToBoolean(row.Cells[0].Value) == true)
                {
                    // ID 컬럼의 값을 안전하게 int로 변환
                    if (row.Cells["ID"].Value != null)
                    {
                        if (int.TryParse(row.Cells["ID"].Value.ToString(), out int id))
                        {
                            checkedData.Add(id);
                        }
                        else
                        {
                            Debug.WriteLine($"ID 값 '{row.Cells["ID"].Value}'을(를) 정수로 변환할 수 없습니다.");
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

        public async Task MergeAndCreateNewCluster(DataTable dataTable, List<int> targetIds, string clusterName = null, string clusterID = null)
        {
            if (targetIds == null || targetIds.Count == 0) return;

            int newClusterNumber;
            bool isNewCluster = true;
            // 새 클러스터 번호 생성 (Repository 메서드 사용)
            var clusteringRepo = new ClusteringRepository();

            try
            {
                Debug.WriteLine($"clusterName : {clusterName} clusterID : {clusterID} ");
                // clusterID가 주어진 경우, 해당 ID를 사용
                if (clusterID != null)
                {
                    if (int.TryParse(clusterID, out newClusterNumber))
                    {
                        isNewCluster = false;  // 기존 클러스터에 병합
                                               // MongoDB에서 해당 ID의 존재 여부 확인 (중요)
                       
                        var existingCluster = await clusteringRepo.GetByClusterNumberAsync(newClusterNumber);
                        Debug.WriteLine($"existingCluster : {existingCluster} clusterID : {clusterID} ");
                        if (existingCluster == null)
                        {
                            throw new Exception($"클러스터 번호 {newClusterNumber}를 가진 클러스터가 MongoDB에 존재하지 않습니다.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("유효하지 않은 클러스터 ID입니다.");
                    }
                }
                else
                {

                    newClusterNumber = await clusteringRepo.GetNextClusterNumberAsync();
                    Debug.WriteLine($"MongoDB에서 생성한 새 클러스터 번호: {newClusterNumber}");
                }

                // 병합할 행들의 데이터 수집
                var targetRows = dataTable.AsEnumerable()
                    .Where(row => targetIds.Contains(Convert.ToInt32(row["ID"])))
                    .ToList();

                if (targetRows.Count == 0) return;

                // 병합 데이터 준비
                string mergedClusterName = "";

                if (clusterName == null || "".Equals(clusterName))
                {
                    mergedClusterName = string.Join("_",
                        targetRows.Select(row => row["클러스터명"].ToString()));

                    // 20자 제한 처리
                    if (mergedClusterName.Length > 20)
                    {
                        mergedClusterName = mergedClusterName.Substring(0, 17) + "...";
                    }
                }
                else
                {
                    mergedClusterName = clusterName;
                }

                string mergedKeywords = string.Join(",",
                    targetRows.Select(row => row["키워드목록"].ToString()));

                int totalCount = targetRows.Sum(row =>
                    Convert.ToInt32(row["Count"]));

                decimal totalAmount = targetRows.Sum(row =>
                    Convert.ToDecimal(row["합산금액"]));

                // 데이터 인덱스 수집
                HashSet<string> dataIndicesSet = new HashSet<string>();
                foreach (var row in targetRows)
                {
                    string indices = row["dataIndex"].ToString();
                    if (!string.IsNullOrEmpty(indices))
                    {
                        foreach (string index in indices.Split(',')
                            .Select(i => i.Trim())
                            .Where(i => !string.IsNullOrEmpty(i)))
                        {
                            dataIndicesSet.Add(index);
                        }
                    }
                }
                List<string> dataIndices = dataIndicesSet.ToList();

                if (isNewCluster)
                {
                    // 새로운 행 추가 (새 클러스터 생성 시)
                    DataRow newRow = dataTable.NewRow();
                    newRow["ID"] = newClusterNumber;
                    newRow["ClusterID"] = newClusterNumber; // 자신의 ID를 ClusterID로 설정 (병합 클러스터)
                    newRow["클러스터명"] = mergedClusterName;
                    newRow["키워드목록"] = mergedKeywords;
                    newRow["Count"] = totalCount;
                    newRow["합산금액"] = totalAmount;
                    newRow["dataIndex"] = string.Join(",", dataIndices);
                    dataTable.Rows.Add(newRow);
                }
                else
                {
                    // 기존 클러스터 정보 업데이트
                    DataRow existingRow = dataTable.AsEnumerable()
                        .FirstOrDefault(row => Convert.ToInt32(row["ID"]) == newClusterNumber);

                    if (existingRow != null)
                    {
                        // 기존 클러스터 데이터 통합
                        string existingKeywords = existingRow["키워드목록"].ToString();
                        int existingCount = Convert.ToInt32(existingRow["Count"]);
                        decimal existingAmount = Convert.ToDecimal(existingRow["합산금액"]);
                        string existingIndices = existingRow["dataIndex"].ToString();

                        // 클러스터명 업데이트 (사용자 지정 클러스터명이 있으면 그것 사용)
                        if (clusterName != null && !string.IsNullOrEmpty(clusterName))
                        {
                            existingRow["클러스터명"] = clusterName;
                        }

                        // 키워드 목록 병합 (중복 제거)
                        HashSet<string> keywordSet = new HashSet<string>(
                            existingKeywords.Split(',').Select(k => k.Trim()));

                        foreach (string keyword in mergedKeywords.Split(',').Select(k => k.Trim()))
                        {
                            keywordSet.Add(keyword);
                        }

                        string combinedKeywords = string.Join(",", keywordSet);

                        // 데이터 인덱스 병합 (중복 제거)
                        HashSet<string> indexSet = new HashSet<string>(
                            existingIndices.Split(',').Select(i => i.Trim()));

                        foreach (string index in dataIndices)
                        {
                            indexSet.Add(index);
                        }

                        string combinedIndices = string.Join(",", indexSet);

                        // 값 업데이트
                        existingRow["키워드목록"] = combinedKeywords;
                        existingRow["Count"] = existingCount + totalCount;
                        existingRow["합산금액"] = existingAmount + totalAmount;
                        existingRow["dataIndex"] = combinedIndices;
                    }
                }

                // 병합된 행들의 ClusterID 업데이트
                foreach (var row in targetRows)
                {
                    row["ClusterID"] = newClusterNumber;
                }

                // 변경사항 적용
                dataTable.AcceptChanges();

                // MongoDB에 병합 결과 저장
                //var clusteringRepo = new ClusteringRepository();

                // 키워드 배열 준비
                List<string> keywordsList = mergedKeywords
                    .Split(',')
                    .Select(k => k.Trim())
                    .Where(k => !string.IsNullOrEmpty(k))
                    .Distinct()
                    .ToList();

                if (isNewCluster)
                {
                    // 새 클러스터 생성
                    var newCluster = new ClusteringResultDocument
                    {
                        ClusterNumber = newClusterNumber,
                        ClusterId = newClusterNumber, // 병합된 클러스터는 자신의 번호를 ClusterId로 가짐
                        ClusterName = mergedClusterName,
                        Keywords = keywordsList,
                        Count = totalCount,
                        TotalAmount = totalAmount,
                        DataIndices = dataIndices,
                        CreatedAt = DateTime.Now
                    };

                    var newId = await clusteringRepo.CreateAsync(newCluster);
                    Debug.WriteLine($"새 클러스터 생성 완료: ID={newId}, ClusterNumber={newClusterNumber}");
                }
                else
                {
                    // 기존 클러스터 업데이트
                    bool updateSuccess = await clusteringRepo.UpdateClusterFullInfoAsync(
                        newClusterNumber,
                        mergedClusterName,
                        keywordsList,
                        totalCount,
                        totalAmount,
                        dataIndices
                    );

                    if (!updateSuccess)
                    {
                        Debug.WriteLine($"경고: 클러스터 {newClusterNumber} 업데이트 실패");
                    }
                }

                // 병합된 클러스터들의 ClusterId 업데이트
                foreach (int targetId in targetIds)
                {
                    if (targetId != newClusterNumber) // 자기 자신은 제외
                    {
                        bool updateSuccess = await clusteringRepo.UpdateClusterIdAsync(targetId, newClusterNumber);
                        if (!updateSuccess)
                        {
                            Debug.WriteLine($"경고: 클러스터 {targetId}의 소속 ID 업데이트 실패");
                        }
                    }
                }

                // 데이터 보강
                mergeClusterDataTable = await EnrichWithRawTableDataAsync(dataTable);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터 병합 오류: {ex.Message}");
                MessageBox.Show($"클러스터 병합 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                    .Where(row => row["ClusterID"] != DBNull.Value &&
                           targetIds.Contains(Convert.ToInt32(row["ClusterID"])))
                    .ToList();

                Debug.WriteLine($"병합 해제할 하위 클러스터 수: {childRows.Count}");

                // 하위 클러스터들의 ClusterID 초기화
                foreach (var row in childRows)
                {
                    row["ClusterID"] = -1; // 미병합 상태로 변경
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
                        var childClusters = await clusteringRepo.GetChildClustersAsync(targetId);
                        foreach (var child in childClusters)
                        {
                            await clusteringRepo.UpdateClusterIdAsync(child.ClusterNumber, -1);
                            Debug.WriteLine($"클러스터 {child.ClusterNumber}의 병합 상태 해제");
                        }

                        // 3. 클러스터 자체 삭제
                        await clusteringRepo.DeleteByClusterNumberAsync(targetId);
                        Debug.WriteLine($"클러스터 {targetId} 삭제 완료");
                    }
                }

                mergeClusterDataTable = await EnrichWithRawTableDataAsync(dataTable);

                Debug.WriteLine("병합 해제 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터 삭제 오류: {ex.Message}");
                MessageBox.Show($"클러스터 삭제 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void merge_search_button_Click(object sender, EventArgs e)
        {
            create_merge_keyword_list();
        }

        private void merge_all_check_CheckedChanged(object sender, EventArgs e)
        {
            // 모든 행의 체크박스 상태 변경
            foreach (DataGridViewRow row in merge_cluster_table.Rows)
            {
                row.Cells[0].Value = merge_all_check.Checked;
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            List<int> mergeIDlList = GetCheckedRowsData(merge_cluster_table);

            if (mergeIDlList.Count == 0)
            {
                MessageBox.Show("병합 대상을 선택하셔야 합니다.", "알림",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Warning);

                return;
            }

            using (var progressForm = new ProcessProgressForm())
            {
                progressForm.Show();
                await progressForm.UpdateProgressHandler(10, "클러스터 병합 시작");
                await Task.Delay(10);

                // 비동기 메서드 await 추가
                await MergeAndCreateNewCluster(DataHandler.finalClusteringData, mergeIDlList);

                await progressForm.UpdateProgressHandler(50, "클러스터 병합 진행중...");
                await Task.Delay(10);

                set_keyword_combo_list();

                //검색조건 초기화
                merge_keyword_combo.SelectedIndex = 0;
                merge_search_keyword.Text = "";

                await progressForm.UpdateProgressHandler(70, "클러스터 병합 결과 출력 중...");
                await Task.Delay(10);

                create_merge_keyword_list(true);
                create_check_keyword_list();

                await progressForm.UpdateProgressHandler(100);
                await Task.Delay(10);
                progressForm.Close();

                MessageBox.Show("클러스터 병합이 완료되었습니다.", "Info",
                                       MessageBoxButtons.OK,
                                       MessageBoxIcon.Information);
            }

            // 병합 작업 후 업데이트
            UpdateModifiedDataGridView();
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

            await deleteClusterId(DataHandler.finalClusteringData, mergeIDlList);

            set_keyword_combo_list();

            //검색조건 초기화
            check_search_combo.SelectedIndex = 0;
            check_search_keyword.Text = "";

            create_merge_keyword_list();
            create_check_keyword_list();

            // 병합 작업 후 업데이트
            UpdateModifiedDataGridView();

            MessageBox.Show(this,"클러스터 병합 해제가 완료되었습니다.", "Info",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Information);

            // 포커스 명시적 복원
            this.Focus(); // UserControl에 포커스
            if (this.ParentForm != null)
                this.ParentForm.Activate(); // 부모 폼 활성화
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

                await progressForm.UpdateProgressHandler(50, "클러스터명 변경 진행중...");
                await Task.Delay(10);

                // 변경 사항 저장
                DataHandler.finalClusteringData.AcceptChanges();
                mergeClusterDataTable = await EnrichWithRawTableDataAsync(DataHandler.finalClusteringData);

                await progressForm.UpdateProgressHandler(70, "클러스터명 변경 결과 출력 중...");
                await Task.Delay(10);

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

        private void merge_search_radio1_CheckedChanged(object sender, EventArgs e)
        {
            merge_search_keyword.Enabled = merge_search_radio2.Checked;

        }

        private void merge_search_radio2_CheckedChanged(object sender, EventArgs e)
        {
            merge_search_keyword.Enabled = merge_search_radio2.Checked;
        }

        private void check_search_radio1_CheckedChanged(object sender, EventArgs e)
        {
            check_search_keyword.Enabled = check_search_radio2.Checked;
        }

        private void check_search_radio2_CheckedChanged(object sender, EventArgs e)
        {
            check_search_keyword.Enabled = check_search_radio2.Checked;
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

        private async void ShowMergeConfirmation(int clusterCount)
        {
            if (isFinishSession)
            {
                DialogResult dupleCheckResult = MessageBox.Show(
                $"현재 페이지에서 수정된 정보를 기준으로 Export 페이지를 갱신하기 위해 "
                + "기존 Export 페이지의 수정 내역을 초기화합니다."
                + "현재 페이지 정보를 기준으로 Export 페이지로 이동하시겠습니까?"
                + "\n(원치 않으실 경우 상단 메뉴바 > Export 항목을 클릭하여 이동 가능합니다. )",
                "Export 페이지 초기화 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
                );

                if (dupleCheckResult != DialogResult.Yes)
                {
                    return;
                }
            }

            DialogResult result = MessageBox.Show(
                $"Clustering 병합 테이블에 {clusterCount} 개의 Cluster가 남아있습니다.\n해당 Cluster를 'Undefined'로 일괄 통합하시겠습니까?\n(클러스터명은 다음 페이지에서 수정 가능합니다.)",
                "클러스터 통합 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                // 남은 clustering 항목 일괄 병합
                List<int> checkedData = DataHandler.finalClusteringData.AsEnumerable()
                                      .Where(row => Convert.ToInt32(row["clusterID"]) < 0)
                                      .Select(row => Convert.ToInt32(row["ID"])) // 정수 ID 가져오기
                                      .ToList();

                await MergeAndCreateNewCluster(DataHandler.finalClusteringData, checkedData, "Undefined");

                set_keyword_combo_list();

                //검색조건 초기화
                merge_keyword_combo.SelectedIndex = 0;
                merge_search_keyword.Text = "";

                create_merge_keyword_list();
                create_check_keyword_list();

                //Export 페이지로 이동
                userControlHandler.uc_classification.initUI();

                if (this.ParentForm is Form1 form)
                {
                    form.LoadUserControl(userControlHandler.uc_classification);
                }
            }
            else
            {
                return;
            }
        }


        private void complete_btn_Click(object sender, EventArgs e)
        {
            int clusterCount = GetCountOfNegativeOneClusterIDs(DataHandler.finalClusteringData);
            if (clusterCount > 0)
            {
                ShowMergeConfirmation(clusterCount);
            }
            else
            {
                userControlHandler.uc_classification.initUI();

                if (this.ParentForm is Form1 form)
                {
                    form.LoadUserControl(userControlHandler.uc_classification);
                }
            }
            isFinishSession = true;
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
            decimalDividerName = decimal_combo.SelectedItem.ToString();

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

                set_keyword_combo_list();

                //검색조건 초기화
                merge_keyword_combo.SelectedIndex = 0;
                merge_search_keyword.Text = "";

                await progressForm.UpdateProgressHandler(70, "클러스터 병합 결과 출력 중...");
                await Task.Delay(10);

                create_merge_keyword_list(true);
                create_check_keyword_list();

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

            // 병합 작업 후 업데이트
            UpdateModifiedDataGridView();
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

        private void keyword_radio1_CheckedChanged(object sender, EventArgs e)
        {
            // 검색 모드가 변경되면 콤보박스와 검색어 초기화
            //merge_keyword_combo.SelectedIndex = 0;
            merge_search_keyword.Text = "";

            // 필요에 따라 콤보박스 내용 업데이트
            UpdateSearchComboBox();
        }

        private void UpdateSearchComboBox()
        {
            merge_keyword_combo.Items.Clear();

            if (keyword_radio1.Checked)
            {
                // 키워드 검색 모드
                merge_keyword_combo.Items.Add("키워드 선택");
                foreach (string keyword in merge_keyword_list)
                {
                    if ("\\".Equals(keyword))
                    {
                        continue;
                    }
                    merge_keyword_combo.Items.Add(keyword);
                }
            }
            else
            {
                // 공급업체 검색 모드
                merge_keyword_combo.Items.Add("공급업체 선택");
                if (supplier_keyword_list != null)
                {
                    foreach (string supplier in supplier_keyword_list)
                    {
                        if ("\\".Equals(supplier))
                        {
                            continue;
                        }
                        merge_keyword_combo.Items.Add(supplier);
                    }
                }

            }

            merge_keyword_combo.SelectedIndex = 0;
        }

        // 키워드별 데이터를 저장할 클래스
        class KeywordData
        {
            public int Count { get; set; }
            public decimal TotalAmount { get; set; }
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
                dataGridView_modified.Font = new System.Drawing.Font("맑은 고딕", 14.25F);

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
                    foreach (var cluster in clustersToMerge)
                    {
                        var childClusters = await clusteringRepo.GetChildClustersAsync(cluster.ClusterNumber);
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

                    // UI 갱신
                    set_keyword_combo_list();
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

                            // UI 스레드에서 실행
                            if (this.InvokeRequired)
                            {
                                Debug.WriteLine("this.InvokeRequired => true");
                                this.Invoke(new Action(() =>
                                {
                                    // 화면 갱신
                                    Debug.WriteLine("this.InvokeRequired => true => set_keyword_combo_list();");
                                    set_keyword_combo_list();

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
                                }));
                            }
                            else
                            {
                                Debug.WriteLine("this.InvokeRequired => false");
                                // 화면 갱신
                                Debug.WriteLine("this.InvokeRequired => false =>set_keyword_combo_list()");
                                set_keyword_combo_list();
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
    }
}
