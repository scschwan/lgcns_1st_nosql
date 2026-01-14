using DocumentFormat.OpenXml.Wordprocessing;
using FinanceTool.Data;
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

     
        // 컨텍스트 메뉴 초기화 (initUI에서 호출)
        private void InitializeContextMenu()
        {
            //현재 세션명 출력
            current_sessionName.Text = "현재 세션명 : " + DataHandler.currentSessionName;

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

                // *** 추가: 세부 클러스터 선택 시 메인 클러스터 ID 찾기 ***
                string detailClusterName = selectedRow.Cells["세부클러스터명"]?.Value?.ToString() ?? "";

                // 세부 클러스터를 선택한 경우 (세부클러스터명에 값이 있는 경우)
                if (!string.IsNullOrEmpty(detailClusterName))
                {
                    Debug.WriteLine($"세부 클러스터 선택됨 - 메인 클러스터 찾기 시작");
                    Debug.WriteLine($"  선택된 클러스터명: {clusterName}");
                    Debug.WriteLine($"  선택된 세부클러스터명: {detailClusterName}");

                    // DataGridView에서 메인 클러스터 찾기
                    // 조건: 클러스터명이 같고, 세부클러스터명이 비어있는 row
                    DataGridViewRow mainClusterRow = null;

                    foreach (DataGridViewRow row in dataGridView_classify.Rows)
                    {
                        if (row.Cells["클러스터명"]?.Value?.ToString() == clusterName)
                        {
                            string rowDetailClusterName = row.Cells["세부클러스터명"]?.Value?.ToString() ?? "";

                            // 세부클러스터명이 비어있는 row = 메인 클러스터
                            if (string.IsNullOrEmpty(rowDetailClusterName))
                            {
                                mainClusterRow = row;
                                break;
                            }
                        }
                    }

                    // 메인 클러스터를 찾은 경우
                    if (mainClusterRow != null && mainClusterRow.Cells["ID"]?.Value != null)
                    {
                        int mainClusterId = Convert.ToInt32(mainClusterRow.Cells["ID"].Value);
                        Debug.WriteLine($"메인 클러스터 찾음 - ID: {mainClusterId}, 클러스터명: {clusterName}");

                        // 메인 클러스터 ID로 변경
                        selectedClusterId = mainClusterId;
                    }
                    else
                    {
                        Debug.WriteLine($"메인 클러스터를 찾지 못함 - 선택된 ID 사용: {selectedClusterId}");
                        // 메인 클러스터를 찾지 못한 경우 경고 메시지 표시
                        MessageBox.Show(
                            $"'{clusterName}' 클러스터의 메인 클러스터를 찾을 수 없습니다.\n" +
                            "'세부클러스터명'이 없는 항목을 선택하여 진입하여 주시길 바랍니다.",
                            "알림",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        return;
                    }
                }

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