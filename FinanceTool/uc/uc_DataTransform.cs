using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Wordprocessing;
using FinanceTool.MongoModels;
using FinanceTool.Repositories;
using Microsoft.VisualBasic.Devices;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
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
    public partial class uc_DataTransform : UserControl
    {

        DataTable originDataTable;
        DataTable transformDataTable;
        DataTable viewTransformDataTable;
        DataTable modifiedDataTable;

        private bool isProcessingSelection = false;
        private decimal decimalDivider = 1;
        private string decimalDividerName = "원";
        private int keywordColumnsCount = 0;

        private bool isFinishSession = false;


        // === 페이징 관련 멤버 변수 추가 ===
        private DataTable _fullDataTable2nd = null;
        private int _currentPage2nd = 1;
        private int _pageSize2nd = 1000;
        private int _totalPages2nd = 1;

        private DataTable _fullDataTableTransform = null;
        private int _currentPageTransform = 1;
        private int _pageSizeTransform = 1000;
        private int _totalPagesTransform = 1;



        public uc_DataTransform()
        {
            InitializeComponent();
        }

        private void InitializePaginationEvents()
        {
            try
            {
                //현재 세션명 출력
                current_sessionName.Text = "현재 세션명 : " + DataHandler.currentSessionName;

                // dataGridView_2nd용 설정
                cmb_pageSize.Items.Clear();
                //cmb_pageSize.Items.AddRange(new object[] { 50, 100, 200, 500, 1000 });
                cmb_pageSize.Items.AddRange(new object[] { 1000,2000,5000,10000 });
                cmb_pageSize.SelectedItem = _pageSize2nd;
                cmb_pageSize.DropDownStyle = ComboBoxStyle.DropDownList;
                cmb_pageSize.SelectedIndexChanged += cmb_pageSize_SelectedIndexChanged2nd;

                num_pageNumber.Minimum = 1;
                num_pageNumber.ValueChanged += num_pageNumber_ValueChanged2nd;
                btn_prevPage.Click += btn_prevPage_Click2nd;
                btn_nextPage.Click += btn_nextPage_Click2nd;

                // dataGridView_transform용 설정
                cmb_pageSize2.Items.Clear();
                //cmb_pageSize2.Items.AddRange(new object[] { 50, 100, 200, 500, 1000 });
                cmb_pageSize2.Items.AddRange(new object[] { 1000, 2000, 5000, 10000 });
                cmb_pageSize2.SelectedItem = _pageSizeTransform;
                cmb_pageSize2.DropDownStyle = ComboBoxStyle.DropDownList;
                cmb_pageSize2.SelectedIndexChanged += cmb_pageSize_SelectedIndexChangedTransform;

                num_pageNumber2.Minimum = 1;
                num_pageNumber2.ValueChanged += num_pageNumber_ValueChangedTransform;
                btn_prevPage2.Click += btn_prevPage_ClickTransform;
                btn_nextPage2.Click += btn_nextPage_ClickTransform;

                // 초기 비활성화
                EnablePaginationControls2nd(false);
                EnablePaginationControlsTransform(false);

                Debug.WriteLine("페이징 이벤트 핸들러 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"페이징 초기화 오류: {ex.Message}");
            }
        }



        // initUI 메서드 수정
        public async Task initUI()
        {
            try
            {
                Debug.WriteLine("data Transform initUI -> MongoDB 데이터 로드 시작");

                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "MongoDB 연결 확인 중...");

                    // MongoDB 연결 확인
                    bool mongoConnected = await Data.MongoDBManager.Instance.EnsureInitializedAsync();
                    if (!mongoConnected)
                    {
                        throw new Exception("MongoDB 연결에 실패했습니다.");
                    }

                    await progressForm.UpdateProgressHandler(20, "ProcessView 데이터 로드 중...");

                    // ProcessView 저장소 인스턴스 생성
                    var processViewRepo = new Repositories.ProcessViewRepository();

                    // MongoDB에서 process_view_data 컬렉션의 문서 조회
                    var filter = Builders<MongoModels.ProcessViewDocument>.Filter.Empty;
                    var sort = Builders<MongoModels.ProcessViewDocument>.Sort.Descending(d => d.LastModifiedDate);

                    var processViewDocs = await processViewRepo.GetAllAsync();

                    await progressForm.UpdateProgressHandler(30, $"ProcessView 데이터 변환 중...");

                    // ProcessView 문서를 DataTable로 변환 - 키워드 바로 매핑
                    DataTable viewData = new DataTable();

                    // 필요한 메타데이터 컬럼 추가
                    viewData.Columns.Add("raw_data_id", typeof(string)); // raw_data_id 직접 사용

                    // 각 키워드를 별도 컬럼으로 추가
                    int maxKeywordColumns = 0;

                    // 전처리: 먼저 최대 키워드 컬럼 수를 결정
                    foreach (var doc in processViewDocs)
                    {
                        int keywordCount = doc.Keywords?.FinalKeywords?.Count ?? 0;
                        maxKeywordColumns = Math.Max(maxKeywordColumns, keywordCount);
                    }

                    // 키워드 컬럼 생성 (Column0부터 시작)
                    for (int i = 0; i < maxKeywordColumns; i++)
                    {
                        viewData.Columns.Add($"Column{i}", typeof(string));
                    }

                    Debug.WriteLine($"생성된 컬럼 구조: {string.Join(", ", viewData.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");

                    // 문서를 DataTable로 변환
                    await Task.Run(() => {
                        foreach (var doc in processViewDocs)
                        {
                            DataRow row = viewData.NewRow();
                            row["raw_data_id"] = doc.RawDataId; // 직접 raw_data_id 사용

                            // 키워드들을 Column0부터 바로 매핑
                            var keywords = doc.Keywords?.FinalKeywords ?? new List<string>();
                            for (int i = 0; i < keywords.Count && i < maxKeywordColumns; i++)
                            {
                                row[$"Column{i}"] = keywords[i];
                            }

                            viewData.Rows.Add(row);
                        }
                    });

                    await progressForm.UpdateProgressHandler(40, "데이터 설정 중...");

                    // DataTable 설정
                    originDataTable = viewData;
                    transformDataTable = viewData.Copy();

                    Debug.WriteLine("data Transform initUI -> transformDataTable 설정 완료");

                    // ProcessView에서 바로 금액 정보를 가져오므로 추가 로드 필요 없음
                    // 대신 moneyDataTable을 초기화
                    //await progressForm.UpdateProgressHandler(50, "금액 데이터 설정 중...");
                    //await SetupMoneyDataTable();

                    // 원본 데이터로 뷰 데이터 보강 (극한 성능 적용)
                    await progressForm.UpdateProgressHandler(60, "원본 데이터 보강 중...");
                    viewTransformDataTable = await EnrichTransformDataWithMongoData(transformDataTable);

                    Debug.WriteLine("data Transform initUI -> DataGridView 바인딩 설정 완료");

                    // 메인 UI 스레드로 돌아가서 UI 컨트롤 업데이트
                    await Task.Run(() =>
                    {
                        if (Application.OpenForms.Count > 0)
                        {
                            Application.OpenForms[0].Invoke((MethodInvoker)delegate
                            {
                                // 정렬 처리 설정
                                sum_keyword_table.SortCompare += DataHandler.money_SortCompare;
                                match_keyword_table.SortCompare += DataHandler.money_SortCompare;
                            });
                        }
                    });

                    // 나머지 초기화 로직
                    await progressForm.UpdateProgressHandler(70, "키워드 병합 리스트 생성 중...");

                    // create_merge_keyword_list 함수 호출 - 새로운 ProcessMergeKeywordListWithProgress 호출
                    await create_merge_keyword_list();
                    Debug.WriteLine("data Transform initUI -> create_merge_keyword_list 완료");


                    Debug.WriteLine("data Transform initUI -> set_keyword_combo_list 설정 완료");

                    // 메인 UI 스레드로 돌아가서 DataHandler 등록
                    await Task.Run(() =>
                    {
                        if (Application.OpenForms.Count > 0)
                        {
                            Application.OpenForms[0].Invoke((MethodInvoker)delegate
                            {
                                Debug.WriteLine("RegisterDataGridView -> match_keyword_table");
                                DataHandler.RegisterDataGridView(match_keyword_table);

                                // 이벤트 핸들러 중복 등록 방지
                                decimal_combo.SelectedIndexChanged -= decimal_combo_SelectedIndexChanged; // 기존 핸들러 제거
                                decimal_combo.SelectedIndex = 0;
                                decimal_combo.SelectedIndexChanged += decimal_combo_SelectedIndexChanged;
                            });
                        }
                    });

                    // 최종 결과를 화면에 표시
                    await Task.Run(() =>
                    {
                        if (Application.OpenForms.Count > 0)
                        {
                            Application.OpenForms[0].Invoke((MethodInvoker)delegate
                            {



                                // 페이징 이벤트 핸들러 초기화 (아직 안했다면)
                                if (cmb_pageSize.Items.Count == 0)
                                {
                                    InitializePaginationEvents();
                                }

                                // 보강된 viewTransformDataTable를 페이징으로 표시
                                Debug.WriteLine($"viewTransformDataTable 페이징 준비: {viewTransformDataTable.Rows.Count}개 행");
                                SetFullDataTable2nd(viewTransformDataTable);

                                Debug.WriteLine($"viewTransformDataTable 페이징 표시 완료");

                            });
                        }
                    });

                    await progressForm.UpdateProgressHandler(100, "데이터 로드 완료");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"initUI 오류: {ex.Message}\n{ex.StackTrace}");
                await Task.Run(() =>
                {
                    MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {ex.Message}",
                                  "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
        }



        // === 페이징 컨트롤 활성화/비활성화 ===
        private void EnablePaginationControls2nd(bool enabled)
        {
            btn_prevPage.Enabled = enabled;
            btn_nextPage.Enabled = enabled;
            num_pageNumber.Enabled = enabled;
            cmb_pageSize.Enabled = enabled;
        }

        private void EnablePaginationControlsTransform(bool enabled)
        {
            btn_prevPage2.Enabled = enabled;
            btn_nextPage2.Enabled = enabled;
            num_pageNumber2.Enabled = enabled;
            cmb_pageSize2.Enabled = enabled;
        }

        // === dataGridView_2nd 페이징 이벤트 핸들러들 ===
        private async void num_pageNumber_ValueChanged2nd(object sender, EventArgs e)
        {
            if (num_pageNumber.Value < 1 || num_pageNumber.Value > _totalPages2nd)
                return;

            if (_currentPage2nd == (int)num_pageNumber.Value)
                return;

            _currentPage2nd = (int)num_pageNumber.Value;
            DisplayPage2nd();
            UpdatePaginationControls2nd();
        }

        private async void btn_prevPage_Click2nd(object sender, EventArgs e)
        {
            if (_currentPage2nd > 1)
            {
                num_pageNumber.Value--;
            }
        }

        private async void btn_nextPage_Click2nd(object sender, EventArgs e)
        {
            if (_currentPage2nd < _totalPages2nd)
            {
                num_pageNumber.Value++;
            }
        }

        private async void cmb_pageSize_SelectedIndexChanged2nd(object sender, EventArgs e)
        {
            if (cmb_pageSize.SelectedItem != null)
            {
                _pageSize2nd = (int)cmb_pageSize.SelectedItem;
                _currentPage2nd = 1;
                if (_fullDataTable2nd != null)
                    SetFullDataTable2nd(_fullDataTable2nd);
            }
        }

        // === dataGridView_transform 페이징 이벤트 핸들러들 ===
        private async void num_pageNumber_ValueChangedTransform(object sender, EventArgs e)
        {
            if (num_pageNumber2.Value < 1 || num_pageNumber2.Value > _totalPagesTransform)
                return;

            if (_currentPageTransform == (int)num_pageNumber2.Value)
                return;

            _currentPageTransform = (int)num_pageNumber2.Value;
            DisplayPageTransform();
            UpdatePaginationControlsTransform();
        }

        private async void btn_prevPage_ClickTransform(object sender, EventArgs e)
        {
            if (_currentPageTransform > 1)
            {
                num_pageNumber2.Value--;
            }
        }

        private async void btn_nextPage_ClickTransform(object sender, EventArgs e)
        {
            if (_currentPageTransform < _totalPagesTransform)
            {
                num_pageNumber2.Value++;
            }
        }

        private async void cmb_pageSize_SelectedIndexChangedTransform(object sender, EventArgs e)
        {
            if (cmb_pageSize2.SelectedItem != null)
            {
                _pageSizeTransform = (int)cmb_pageSize2.SelectedItem;
                _currentPageTransform = 1;
                if (_fullDataTableTransform != null)
                    SetFullDataTableTransform(_fullDataTableTransform);
            }
        }

    
      
       

        private void keyword_search_button_Click(object sender, EventArgs e)
        {
            _ = DoKeywordSearchAsync(sender, e);
            
        }

        private async void change_keyword_Click(object sender, EventArgs e)
        {
            string target_keyword = "";

            if ("".Equals(modified_keyword.Text.ToString()) || modified_keyword.Text == null)
            {
                MessageBox.Show("변환 키워드를 입력하셔야 합니다.", "알림",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Warning);

                return;
            }
            else
            {
                target_keyword = modified_keyword.Text.ToString();
            }
            
            //1.선택된 테이블 내 키워드 목록 출력
            List<string> changeValuelList = GetCheckedRowsData(match_keyword_table);

            if (changeValuelList.Count == 0)
            {
                MessageBox.Show("키워드 변환 대상을 선택하셔야 합니다.", "알림",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Warning);

                return;
            }

            using (var progressForm = new ProcessProgressForm())
            {
                progressForm.Show();
                await progressForm.UpdateProgressHandler(10, "키워드 변환 중...");
                await Task.Delay(10);

                //2.dataTransform dataTable 내 키워드 일괄 변환
                //2,2 -> dataTable 에서 일일히 찾아가면서 변환
                //0,1번 index는 부서,공급업체명 일 것이라 가정하므로 2번 index부터 치환(현재는 부서,공급업체명을 표기하지 않는다)
                ReplaceDataTableValues(changeValuelList, transformDataTable, target_keyword, 0);

                await progressForm.UpdateProgressHandler(30, "키워드 변환 내역 저장 중...");
                await Task.Delay(10);

                //viewTransformDataTable 도 변환 
                Debug.WriteLine("EnrichTransformDataWithRawData start");
                viewTransformDataTable = await EnrichTransformDataWithRawData(transformDataTable);
                Debug.WriteLine("EnrichTransformDataWithRawData end");


                await progressForm.UpdateProgressHandler(60, "변환 키워드 기반 요약 정보 재 산출 중...");
                await Task.Delay(10);


                Debug.WriteLine("data Transform change_keyword_Click -> create_merge_keyword_list & set_keyword_combo_list 설정 시작");

                //3.변경된 키워드 기반 리스트 재 생성
                await create_merge_keyword_list();
                await Task.Delay(10);
               

                Debug.WriteLine("data Transform change_keyword_Click -> set_keyword_combo_list 설정 완료");


                await progressForm.UpdateProgressHandler(90, "화면 완료...");
                await Task.Delay(10);


                await progressForm.UpdateProgressHandler(100);
                await Task.Delay(10);
                progressForm.Close();

            }
           

            MessageBox.Show("키워드 변환이 완료되었습니다.", "Info",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Information);


            match_keyword_table.DataSource = null;
            match_keyword_table.Rows.Clear();
            match_keyword_table.Columns.Clear();
            if (DataHandler.dragSelections.ContainsKey(match_keyword_table))
            {
                DataHandler.dragSelections[match_keyword_table].Clear();
            }
            
            dataGridView_transform.DataSource = null;
            dataGridView_transform.Rows.Clear();
            dataGridView_transform.Columns.Clear();

            //search_keyword_detail_list();

            //변환된 행 값으로 자동 선택

            bool exactMatch = true;

            for (int i = 0; i < sum_keyword_table.Rows.Count; i++)
            {
                if (sum_keyword_table.Rows[i].Cells[0].Value != null)
                {
                    string cellValue = sum_keyword_table.Rows[i].Cells[0].Value.ToString();

                    bool match = exactMatch
                        ? cellValue.Equals(target_keyword, StringComparison.OrdinalIgnoreCase)
                        : cellValue.IndexOf(target_keyword, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (match)
                    {
                        // 현재 선택 모두 해제
                        sum_keyword_table.ClearSelection();

                        // 행 선택
                        sum_keyword_table.Rows[i].Selected = true;

                        // 선택한 행이 보이도록 스크롤
                        sum_keyword_table.FirstDisplayedScrollingRowIndex = i;

                    }
                }
            }
            /*
            // 키워드를 사용하여 transformDataTable 필터링
            DataTable filteredTable = FilterTransformDataByKeyword(viewTransformDataTable, transformDataTable, target_keyword);

            // 필터링된 결과를 다른 DataGridView에 표시
            dataGridView_2nd.DataSource = null;
            dataGridView_2nd.Rows.Clear();
            dataGridView_2nd.Columns.Clear();
            dataGridView_2nd.DataSource = filteredTable;
            //dataGridView_2nd.Columns["import_date"].Visible = false;

            if (dataGridView_2nd.Columns["raw_data_id"] != null)
            {
                dataGridView_2nd.Columns["raw_data_id"].Visible = false;
            }
            */

            // 마지막 부분만 수정:
            DataTable filteredTable = FilterTransformDataByKeyword(viewTransformDataTable, transformDataTable, target_keyword);
            SetFullDataTable2nd(filteredTable); // 페이징 적용

        }

        private void check_all_keyword_list_CheckedChanged(object sender, EventArgs e)
        {
            // 모든 행의 체크박스 상태 변경
            foreach (DataGridViewRow row in match_keyword_table.Rows)
            {
                row.Cells[0].Value = check_all_keyword_list.Checked;
            }
        }

     
        private async void button2_Click(object sender, EventArgs e)
        {
            try
            {
                // 간단한 로딩 메시지만 표시
                using (var waitCursor = new WaitCursor())
                {
                    // 데이터 로드 작업을 백그라운드 스레드에서 처리
                    await Task.Run(async () => {
                        if (DataHandler.firstClusteringData.Rows.Count == 0)
                        {
                            DataHandler.firstClusteringData = await DataHandler.CreateSetGroupDataTableAsync(originDataTable, DataHandler.moneyDataTable);
                        }
                        if (DataHandler.secondClusteringData.Rows.Count == 0)
                        {
                            DataHandler.secondClusteringData = await DataHandler.CreateSetGroupDataTableAsync(transformDataTable, DataHandler.moneyDataTable, true);
                        }
                    });

                    // 팝업 컨트롤 생성 및 초기화 (UI 스레드에서)
                    uc_clusteringPopup popup_control = new uc_clusteringPopup();
                    popup_control.initUI();

                    // 비모달 방식으로 팝업 표시
                    ShowUserControlAsDialog(popup_control);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"클러스터링 팝업 표시 중 오류: {ex.Message}");
                MessageBox.Show($"데이터 처리 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 간단한 대기 커서 클래스
        public class WaitCursor : IDisposable
        {
            private Cursor _previousCursor;

            public WaitCursor()
            {
                _previousCursor = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
            }

            public void Dispose()
            {
                Cursor.Current = _previousCursor;
            }
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            if (isFinishSession)
            {
                DialogResult dupleCheckResult = MessageBox.Show(
                $"현재 페이지에서 수정된 정보를 기준으로 Clustering 페이지를 갱신하기 위해 "
                + "기존 Clustering 페이지의 수정 내역을 초기화합니다."
                + "현재 페이지 정보를 기준으로 Clustering 페이지로 이동하시겠습니까?"
                + "\n(원치 않으실 경우 상단 메뉴바 > Clustering 항목을 클릭하여 이동 가능합니다. )",
                "Clustering 페이지 초기화 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
                );

                if (dupleCheckResult != DialogResult.Yes)
                {
                    return;
                }
                else
                {
                    DataHandler.finalClusteringData = null;

                    //db 초기화
                     // 필요한 Repository 인스턴스들 생성
                    var clusteringRepository = new ClusteringRepository();
                    Debug.WriteLine(" 컬렉션 초기화 시작...");

                    // 1. clustering_results 컬렉션 초기화
                    await clusteringRepository.DeleteManyAsync(FilterDefinition<ClusteringResultDocument>.Empty);
                    Debug.WriteLine("clustering_results 컬렉션 초기화 완료");

                }
            }

            using (var progressForm = new ProcessProgressForm())
            {
                progressForm.Show();
                await progressForm.UpdateProgressHandler(10, "데이터 저장 준비 중...");
                await Task.Delay(10);
                DataHandler.secondClusteringData = await DataHandler.CreateSetGroupDataTableAsync(transformDataTable, DataHandler.moneyDataTable, true);

                Debug.WriteLine("CreateSetGroupDataTable 수행 완료");

                DataHandler.recomandKeywordTable = modifiedDataTable;

                await progressForm.UpdateProgressHandler(30, "데이터 저장 준비 중...");
                await Task.Delay(10);

                userControlHandler.uc_clustering.initUI();

                await progressForm.UpdateProgressHandler(40, "화면 구성 중...");
                await Task.Delay(10);


                if (this.ParentForm is Form1 form)
                {
                    form.LoadUserControl(userControlHandler.uc_clustering ,form.classificationToolStripMenuItem);
                }
                await progressForm.UpdateProgressHandler(90, "화면 완료...");
                await Task.Delay(10);

                isFinishSession = true;

                await progressForm.UpdateProgressHandler(100);
                await Task.Delay(10);
                progressForm.Close();

            }
            
          
        }

        private void search_keyword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                keyword_search_button_Click(sender, e);   // 호출하고 싶은 함수
                e.SuppressKeyPress = true;  // 비프음 방지
            }
        }


        private async void dept_col_check_CheckedChanged(object sender, EventArgs e)
        {
            DataHandler.dept_col_yn = dept_col_check.Checked;

            //기존 clustering 결과는 초기화
            if (DataHandler.secondClusteringData.Rows.Count > 0)
            {
                DataHandler.secondClusteringData = await DataHandler.CreateSetGroupDataTableAsync(transformDataTable, DataHandler.moneyDataTable, true);
            }
            
        }

        private async void prod_col_check_CheckedChanged(object sender, EventArgs e)
        {
            DataHandler.prod_col_yn = prod_col_check.Checked;

            //기존 clustering 결과는 초기화
            if (DataHandler.secondClusteringData.Rows.Count > 0)
            {
                DataHandler.secondClusteringData = await DataHandler.CreateSetGroupDataTableAsync(transformDataTable, DataHandler.moneyDataTable, true);
            }
            
        }

        private void dataGridView_modified_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            search_keyword_detail_list();
        }

        private void match_keyword_table_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (match_keyword_table.SelectedCells.Count > 0)
            {
                int rowIndex = match_keyword_table.SelectedCells[0].RowIndex;
                string keyword = match_keyword_table.Rows[rowIndex].Cells[1].Value.ToString();

                DataTable filteredTable = FilterTransformDataByKeyword(viewTransformDataTable, transformDataTable, keyword);
                SetFullDataTableTransform(filteredTable); // 페이징 적용

                Debug.WriteLine($"키워드 '{keyword}'를 포함하는 행: {filteredTable.Rows.Count}개");
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
            decimalDividerName = decimal_combo.SelectedItem.ToString();

            //리스트 재 조회
            // 나머지 초기화 로직
            //await Task.Run(() => create_merge_keyword_list(true));
            //create_merge_keyword_list(true);
            // Task.Run을 사용하여 create_merge_keyword_list를 실행하고 완료될 때까지 기다림
                    await Task.Run(() => {
                        // UI 스레드에서 실행해야 하는 부분이 있다면 Invoke 사용
                        this.Invoke((MethodInvoker)delegate {
                            create_merge_keyword_list(true);                           
                        });
                    });

            if (match_keyword_table.Rows.Count > 0)
            {
                Debug.WriteLine("keyword_search_button_Click 함수 호출");
                await DoKeywordSearchAsync(sender, e);
            }

        }

        


    }

}
