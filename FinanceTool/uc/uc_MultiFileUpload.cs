using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using FinanceTool.MongoModels;
using FinanceTool.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static FinanceTool.uc_MultiFileUpload;

namespace FinanceTool
{
    public partial class uc_MultiFileUpload : UserControl
    {
        public uc_MultiFileUpload()
        {
            InitializeComponent();
            _uploadedFileRepository = new UploadedFileRepository();
            _fileSessionRepository = new FileSessionRepository();

            // 업로드 폴더 생성
            EnsureUploadFolderExists();

            // UI 초기화
            InitializeUI();
        }



        /// <summary>
        /// 셀 툴팁 텍스트 제공 이벤트
        /// </summary>
        private void Dgv_files_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    var columnName = dgv_files.Columns[e.ColumnIndex].Name;
                    var fileData = dgv_files.Rows[e.RowIndex].DataBoundItem as FileDisplayData;

                    if (fileData != null)
                    {
                        // 계정명 내용 컬럼의 툴팁
                        if (columnName == "AccountContent" && fileData.AccountContents?.Count > 0)
                        {
                            if (fileData.AccountContents.Count <= 5)
                            {
                                e.ToolTipText = $"전체 계정명 ({fileData.AccountContents.Count}개):\\n" +
                                              string.Join("\\n", fileData.AccountContents.Select((v, i) => $"{i + 1}. {v}"));
                            }
                            else
                            {
                                e.ToolTipText = $"전체 계정명 ({fileData.AccountContents.Count}개):\\n" +
                                              string.Join("\\n", fileData.AccountContents.Take(10).Select((v, i) => $"{i + 1}. {v}")) +
                                              $"\\n... 외 {fileData.AccountContents.Count - 10}개 더\\n\\n※ 마우스를 올려두면 전체 목록을 확인할 수 있습니다.";
                            }
                        }
                        // 감지된 컬럼 목록의 툴팁
                        else if (columnName == "DetectedColumns" && fileData.DetectedColumns?.Count > 0)
                        {
                            e.ToolTipText = $"전체 컬럼 ({fileData.DetectedColumns.Count}개):\\n" +
                                          string.Join("\\n", fileData.DetectedColumns.Select((v, i) => $"{i + 1}. {v}"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"툴팁 생성 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// DataGridView 데이터 오류 처리
        /// </summary>
        private void Dgv_files_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            try
            {
                // 콤보박스 관련 오류인 경우 무시하고 기본값으로 설정
                if (e.Exception is ArgumentException || e.Exception is FormatException)
                {
                    var cell = dgv_files[e.ColumnIndex, e.RowIndex];
                    if (cell is DataGridViewComboBoxCell)
                    {
                        cell.Value = ""; // 빈 값으로 초기화
                    }
                }

                // 오류를 처리했음을 표시
                e.ThrowException = false;

                Debug.WriteLine($"DataGridView 오류 처리: {e.Exception?.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DataError 이벤트 처리 오류: {ex.Message}");
                e.ThrowException = false;
            }
        }


        /// <summary>
        /// 세션 생성 버튼 클릭
        /// </summary>
        /// <summary>
        /// 세션 생성 버튼 클릭 (병렬 처리 적용)
        /// </summary>
        private async void btn_create_sessions_Click(object sender, EventArgs e)
        {
            try
            {
                // 1단계: 선택된 파일들 확인
                var selectedFiles = GetSelectedFiles();
                if (selectedFiles.Count == 0)
                {
                    MessageBox.Show("세션을 생성할 파일을 선택해주세요.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 2단계: 계정명, 금액 컬럼 선택 검증
                var validationResult = ValidateSelectedFiles(selectedFiles);
                if (!validationResult.IsValid)
                {
                    MessageBox.Show(validationResult.ErrorMessage, "검증 오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 3단계: 계정명별 파티션 분석 (병렬 처리)
                var partitionResult = new PartitionAnalysisResult();

                // 진행 상황 표시
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();

                    // 3단계: 계정명별 파티션 분석 (병렬 처리)
                    partitionResult = await AnalyzeAccountPartitionsAsync(selectedFiles, progressForm.UpdateProgressHandler);
                    if (!partitionResult.IsValid)
                    {
                        MessageBox.Show(partitionResult.ErrorMessage, "파티션 분석 오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    await progressForm.UpdateProgressHandler(95, "미리보기 준비 중...");
                    await Task.Delay(200); // UI 업데이트 시간 확보
                }
                if (partitionResult == null)
                {
                    MessageBox.Show(partitionResult.ErrorMessage, "파티션 분석 오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 4단계: 파티션 결과 미리보기 및 사용자 확인
                using (var previewDialog = new SessionPartitionPreviewDialog(partitionResult.Partitions))
                {
                    if (previewDialog.ShowDialog() != DialogResult.OK)
                    {
                        return; // 사용자 취소
                    }

                    var approvedPartitions = previewDialog.GetApprovedPartitions();

                    // 5단계: 승인된 파티션들로 세션 생성 (병렬 처리)
                    using (var progressForm = new ProcessProgressForm())
                    {
                        progressForm.Show();
                        await progressForm.UpdateProgressHandler(10, "다중 세션 생성 시작...");

                        var createdSessions = await CreatePartitionedSessionsAsync(approvedPartitions, selectedFiles, progressForm.UpdateProgressHandler);

                        if (createdSessions.Count > 0)
                        {
                            await progressForm.UpdateProgressHandler(90, "세션 목록 업데이트...");

                            // dgv_sessions에 생성된 세션들 추가
                            foreach (var session in createdSessions)
                            {
                                AddSessionToGrid(session);
                            }

                            // 선택된 파일들의 체크 해제
                            ClearFileSelections(selectedFiles);

                            await progressForm.UpdateProgressHandler(100, "완료");
                            await Task.Delay(500);

                            MessageBox.Show($"{createdSessions.Count}개의 세션이 성공적으로 생성되었습니다.", "완료",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파티션 세션 생성 오류: {ex.Message}");
                MessageBox.Show($"세션 생성 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        /// <summary>
        /// 셀 콘텐츠 클릭 이벤트 (삭제 버튼 처리)
        /// </summary>
        private async void Dgv_files_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                // 삭제 버튼 클릭 확인
                if (e.ColumnIndex == dgv_files.Columns["DeleteButton"].Index && e.RowIndex >= 0)
                {
                    var fileData = dgv_files.Rows[e.RowIndex].DataBoundItem as FileDisplayData;
                    if (fileData != null)
                    {
                        await DeleteFile(fileData, e.RowIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"셀 클릭 처리 오류: {ex.Message}");
            }
        }


        /// <summary>
        /// 콤보박스 즉시 업데이트를 위한 이벤트 핸들러
        /// </summary>
        private void Dgv_files_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgv_files.IsCurrentCellDirty && dgv_files.CurrentCell is DataGridViewComboBoxCell)
            {
                dgv_files.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        /// <summary>
        /// 셀 값 변경 이벤트 핸들러 (계정명/금액 컬럼 선택 시)
        /// </summary>
        private async void Dgv_files_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;

                var row = dgv_files.Rows[e.RowIndex];
                var fileData = row.DataBoundItem as FileDisplayData;

                if (fileData == null) return;

                // 계정명 컬럼 선택이 변경된 경우만 계정명 검증
                if (dgv_files.Columns[e.ColumnIndex].Name == "AccountColumn")
                {
                    await UpdateAccountColumnInfo(fileData);
                }
                // 금액 컬럼 선택이 변경된 경우만 금액 검증
                else if (dgv_files.Columns[e.ColumnIndex].Name == "AmountColumn")
                {
                    await UpdateAmountColumnInfo(fileData);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"셀 값 변경 처리 오류: {ex.Message}");
            }
        }



        /// <summary>
        /// 파일 업로드 버튼 클릭
        /// </summary>
        private async void btn_upload_files_Click(object sender, EventArgs e)
        {
            try
            {
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "Excel Files|*.xlsx;*.xls";
                    openFileDialog.Multiselect = true;
                    openFileDialog.Title = "업로드할 엑셀 파일을 선택하세요";

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        using (var progressForm = new ProcessProgressForm())
                        {
                            progressForm.Show();
                            await progressForm.UpdateProgressHandler(10, "파일 업로드 시작...");

                            var newFiles = new List<FileDisplayData>();
                            int fileCount = openFileDialog.FileNames.Length;

                            for (int i = 0; i < fileCount; i++)
                            {
                                string filePath = openFileDialog.FileNames[i];
                                int progress = 10 + ((i + 1) * 80 / fileCount);

                                await progressForm.UpdateProgressHandler(progress,
                                    $"파일 처리 중... ({i + 1}/{fileCount})");

                                var fileDisplayData = await ProcessUploadedFile(filePath);
                                if (fileDisplayData != null)
                                {
                                    newFiles.Add(fileDisplayData);
                                }
                            }

                            await progressForm.UpdateProgressHandler(95, "파일 목록 업데이트...");

                            // DataGridView에 새 파일들 추가
                            var currentList = (dgv_files.DataSource as List<FileDisplayData>) ?? new List<FileDisplayData>();
                            currentList.AddRange(newFiles);
                            dgv_files.DataSource = currentList.ToList(); // 새 리스트로 재할당

                            UpdateComboBoxItems();

                            await progressForm.UpdateProgressHandler(100, "완료");
                            await Task.Delay(100);

                            MessageBox.Show($"{newFiles.Count}개의 파일이 성공적으로 업로드되었습니다.",
                            "업로드 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }


                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파일 업로드 오류: {ex.Message}");
                MessageBox.Show($"파일 업로드 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        //////////////////////////////////세션 테이블 관련 함수////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// 세션 그리드 셀 값 변경 이벤트 (세션명 편집 처리)
        /// </summary>
        private async void Dgv_sessions_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;

                var columnName = dgv_sessions.Columns[e.ColumnIndex].Name;
                var sessionData = dgv_sessions.Rows[e.RowIndex].DataBoundItem as SessionDisplayData;

                if (sessionData == null) return;

                // 세션명 편집 처리
                if (columnName == "SessionName")
                {
                    var newSessionName = dgv_sessions.Rows[e.RowIndex].Cells["SessionName"].Value?.ToString()?.Trim();

                    //Debug.WriteLine($"세션 정보 업데이트 로직 진입 columnName : {columnName}  ,sessionData.SessionName : {sessionData.SessionName}  newSessionName  : {newSessionName}");
                    //Debug.WriteLine($"string.IsNullOrEmpty(newSessionName) : {string.IsNullOrEmpty(newSessionName)}    newSessionName != sessionData.SessionName  : {newSessionName != sessionData.SessionName}");

                    //if (!string.IsNullOrEmpty(newSessionName) && newSessionName != sessionData.SessionName)
                    if (!string.IsNullOrEmpty(newSessionName))
                    {
                        //Debug.WriteLine($"세션 정보 업데이트 start");
                        await UpdateSessionName(sessionData, newSessionName, e.RowIndex);
                    }
                }
                // 세션명 편집 처리
                if (columnName == "WorkerName")
                {
                    var newWorkerName = dgv_sessions.Rows[e.RowIndex].Cells["WorkerName"].Value?.ToString()?.Trim();

                    //Debug.WriteLine($"세션 정보 업데이트 로직 진입 columnName : {columnName}  ,sessionData.SessionName : {sessionData.SessionName}  newSessionName  : {newSessionName}");
                    //Debug.WriteLine($"string.IsNullOrEmpty(newSessionName) : {string.IsNullOrEmpty(newSessionName)}    newSessionName != sessionData.SessionName  : {newSessionName != sessionData.SessionName}");

                    //if (!string.IsNullOrEmpty(newSessionName) && newSessionName != sessionData.SessionName)
                    if (!string.IsNullOrEmpty(newWorkerName))
                    {
                        //Debug.WriteLine($"세션 정보 업데이트 start");
                        await UpdateWorkerName(sessionData, newWorkerName, e.RowIndex);
                    }
                }
                else
                {
                    //Debug.WriteLine($"세션 정보 업데이트 아님 columnName : {columnName}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 정보 업데이트 오류: {ex.Message}");
                MessageBox.Show($"세션 정보 업데이트 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 세션명/작업자명 유효성 검사
        /// </summary>
        private void Dgv_sessions_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            try
            {
                if (dgv_sessions.Columns[e.ColumnIndex].Name == "SessionName")
                {
                    var newSessionName = e.FormattedValue?.ToString()?.Trim();

                    if (string.IsNullOrEmpty(newSessionName))
                    {
                        e.Cancel = true;
                        MessageBox.Show("세션명은 비어있을 수 없습니다.", "입력 오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // 세션명 길이 제한 (20자)
                    if (newSessionName.Length > 50)
                    {
                        e.Cancel = true;
                        MessageBox.Show("세션명은 50자를 초과할 수 없습니다.", "입력 오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // 중복 세션명 검사
                    var currentSessions = dgv_sessions.DataSource as List<SessionDisplayData>;
                    var currentSessionData = dgv_sessions.Rows[e.RowIndex].DataBoundItem as SessionDisplayData;

                    if (currentSessions != null && currentSessionData != null)
                    {
                        var duplicateSession = currentSessions.FirstOrDefault(s =>
                            s.Id != currentSessionData.Id &&
                            s.SessionName.Equals(newSessionName, StringComparison.OrdinalIgnoreCase));

                        if (duplicateSession != null)
                        {
                            e.Cancel = true;
                            MessageBox.Show($"'{newSessionName}' 세션명이 이미 존재합니다.\n다른 이름을 사용해주세요.", "중복 오류",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }
                else if (dgv_sessions.Columns[e.ColumnIndex].Name == "WorkerName")
                {
                    var newWorkerName = e.FormattedValue?.ToString()?.Trim();


                    // 세션명 길이 제한 (20자)
                    if (newWorkerName.Length > 50)
                    {
                        e.Cancel = true;
                        MessageBox.Show("작업자명은 50자를 초과할 수 없습니다.", "입력 오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션명/작업자명 유효성 검사 오류: {ex.Message}");
            }
        }


        /// <summary>
        /// 세션 그리드 셀 포맷팅 (다운로드 버튼 상태 처리)
        /// </summary>
        private void Dgv_sessions_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                var columnName = dgv_sessions.Columns[e.ColumnIndex].Name;

                // SessionName 컬럼만 LightYellow로 강제 설정
                if (columnName == "SessionName")
                {
                    e.CellStyle.BackColor = System.Drawing.Color.LightYellow;
                    e.CellStyle.SelectionBackColor = System.Drawing.Color.Orange; // 선택 시 색상
                }

                // WorkerName 컬럼도 LightYellow로 강제 설정
                if (columnName == "WorkerName")
                {
                    e.CellStyle.BackColor = System.Drawing.Color.LightYellow;
                    e.CellStyle.SelectionBackColor = System.Drawing.Color.Orange; // 선택 시 색상
                }

                if (columnName == "DownloadButton")
                {
                    var sessionData = dgv_sessions.Rows[e.RowIndex].DataBoundItem as SessionDisplayData;
                    if (sessionData != null)
                    {
                        var cell = dgv_sessions.Rows[e.RowIndex].Cells[e.ColumnIndex];

                        // 완료 상태이고 결과 파일 경로가 있으면 활성화
                        if (sessionData.Status == "completed" && !string.IsNullOrEmpty(sessionData.ResultFilePath))
                        {
                            cell.Style.ForeColor = System.Drawing.Color.Blue;
                            cell.Style.BackColor = System.Drawing.Color.LightBlue;
                            cell.ToolTipText = "클릭하여 결과 파일을 다운로드하세요.";
                        }
                        else
                        {
                            cell.Style.ForeColor = System.Drawing.Color.Gray;
                            cell.Style.BackColor = System.Drawing.Color.LightGray;

                            if (sessionData.Status != "completed")
                            {
                                cell.ToolTipText = "세션이 완료된 후 다운로드할 수 있습니다.";
                            }
                            else
                            {
                                cell.ToolTipText = "다운로드할 결과 파일이 없습니다.";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"셀 포맷팅 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 세션 그리드 셀 콘텐츠 클릭 이벤트
        /// </summary>
        private async void Dgv_sessions_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;

                var sessionData = dgv_sessions.Rows[e.RowIndex].DataBoundItem as SessionDisplayData;
                if (sessionData == null) return;

                // 삭제 버튼 클릭
                if (dgv_sessions.Columns[e.ColumnIndex].Name == "DeleteSessionButton")
                {
                    await DeleteSession(sessionData, e.RowIndex);
                }
                // 다운로드 버튼 클릭
                else if (dgv_sessions.Columns[e.ColumnIndex].Name == "DownloadButton")
                {
                    //await DownloadSessionResult(sessionData);
                    await DownloadSessionResult(sessionData);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 그리드 클릭 처리 오류: {ex.Message}");
            }
        }


        private async void btn_add_to_session_Click(object sender, EventArgs e)
        {
            try
            {
                // 1단계: 선택된 파일들 확인
                var selectedFiles = GetSelectedFiles();
                if (selectedFiles.Count == 0)
                {
                    MessageBox.Show("세션에 추가할 파일을 선택해주세요.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 2단계: 선택된 세션 확인
                var selectedSessions = GetSelectedSessions();
                if (selectedSessions.Count == 0)
                {
                    MessageBox.Show("파일을 추가할 세션을 선택해주세요.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (selectedSessions.Count > 1)
                {
                    MessageBox.Show("파일을 추가할 세션을 하나만 선택해주세요.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var targetSession = selectedSessions[0];

                // 3단계: 파일 검증 (계정명, 금액 컬럼 선택 확인)
                var validationResult = ValidateSelectedFiles(selectedFiles);
                if (!validationResult.IsValid)
                {
                    MessageBox.Show(validationResult.ErrorMessage, "검증 오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 4단계: 세션과의 호환성 검증
                var compatibilityResult = ValidateFilesCompatibilityWithSession(selectedFiles, targetSession);
                if (!compatibilityResult.IsValid)
                {
                    MessageBox.Show(compatibilityResult.ErrorMessage, "호환성 오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 5단계: 세션에 파일 추가 처리
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "세션에 파일 추가 중...");

                    var result = await AddFilesToExistingSession(selectedFiles, targetSession, progressForm.UpdateProgressHandler);

                    if (result.Success)
                    {
                        await progressForm.UpdateProgressHandler(90, "UI 업데이트 중...");

                        // UI 업데이트
                        UpdateSessionInGrid(result.UpdatedSession);
                        ClearFileSelections(selectedFiles);

                        await progressForm.UpdateProgressHandler(100, "완료");
                        await Task.Delay(500);

                        MessageBox.Show($"{selectedFiles.Count}개의 파일이 '{targetSession.SessionName}' 세션에 추가되었습니다.",
                            "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션에 파일 추가 오류: {ex.Message}");
                MessageBox.Show($"세션에 파일 추가 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async void btn_merge_sessions_Click(object sender, EventArgs e)
        {
            try
            {
                // 1단계: 선택된 세션들 확인
                var selectedSessions = GetSelectedSessions();
                if (selectedSessions.Count < 2)
                {
                    MessageBox.Show("병합할 세션을 2개 이상 선택해주세요.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 2단계: 세션들의 호환성 검증
                var compatibilityResult = ValidateSessionsCompatibility(selectedSessions);
                if (!compatibilityResult.IsValid)
                {
                    MessageBox.Show(compatibilityResult.ErrorMessage, "호환성 오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 3단계: 병합 확인
                var result = MessageBox.Show(
                    $"선택된 {selectedSessions.Count}개의 세션을 병합하시겠습니까?\n\n" +
                    $"• 총 파일 수: {selectedSessions.Sum(s => s.FileCount)}개\n" +
                    $"• 총 합산 금액: {selectedSessions.Sum(s => s.TotalAmount):N0} 원\n\n" +
                    "※ 첫 번째 세션을 제외한 나머지 세션들은 삭제됩니다.",
                    "세션 병합 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes) return;

                // 4단계: 세션 병합 처리
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "세션 병합 중...");

                    var mergeResult = await MergeSessions(selectedSessions, progressForm.UpdateProgressHandler);

                    if (mergeResult.Success)
                    {
                        await progressForm.UpdateProgressHandler(90, "UI 업데이트 중...");

                        // UI 업데이트
                        UpdateSessionInGrid(mergeResult.MergedSession);
                        RemoveSessionsFromGrid(mergeResult.DeletedSessions);

                        await progressForm.UpdateProgressHandler(100, "완료");
                        await Task.Delay(500);

                        MessageBox.Show("세션 병합이 완료되었습니다.", "완료",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 병합 오류: {ex.Message}");
                MessageBox.Show($"세션 병합 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        /// <summary>
        /// 세션 툴팁에 AccountName 정보도 포함
        /// </summary>
        private void Dgv_sessions_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    var columnName = dgv_sessions.Columns[e.ColumnIndex].Name;
                    var sessionData = dgv_sessions.Rows[e.RowIndex].DataBoundItem as SessionDisplayData;

                    if (sessionData != null)
                    {
                        // AccountName 컬럼의 툴팁
                        if (columnName == "AccountName" && !string.IsNullOrEmpty(sessionData.AccountName))
                        {
                            if (sessionData.AccountName.Contains(","))
                            {
                                var accountNames = sessionData.AccountName.Split(',')
                                    .Select(name => name.Trim())
                                    .Where(name => !string.IsNullOrEmpty(name))
                                    .ToArray();

                                e.ToolTipText = $"병합된 계정명 ({accountNames.Length}개):\n" +
                                              string.Join("\n", accountNames.Select((name, i) => $"{i + 1}. {name}"));
                            }
                            else
                            {
                                e.ToolTipText = $"계정명: {sessionData.AccountName}";
                            }
                        }
                        // AccountColumnName 컬럼의 툴팁 (기존)
                        else if (columnName == "AccountColumnName")
                        {
                            if (sessionData.AccountColumnName.Contains(","))
                            {
                                var accountColumns = sessionData.AccountColumnName.Split(',')
                                    .Select(name => name.Trim())
                                    .Where(name => !string.IsNullOrEmpty(name))
                                    .ToArray();

                                e.ToolTipText = $"병합된 계정명 컬럼 ({accountColumns.Length}개):\n" +
                                              string.Join("\n", accountColumns.Select((name, i) => $"{i + 1}. {name}"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"툴팁 생성 오류: {ex.Message}");
            }
        }



        private async void btn_complete_Click(object sender, EventArgs e)
        {
            try
            {
                // 1단계: 선택된 세션들 확인
                var selectedSessions = GetSelectedSessions();
                if (selectedSessions.Count == 0)
                {
                    MessageBox.Show("처리할 세션을 선택해주세요.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (selectedSessions.Count > 1)
                {
                    MessageBox.Show("처리할 세션은 1개만 선택해주세요.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var selectedSession = selectedSessions.First();

                // 현재 세션 ID를 전역 변수에 저장
                DataHandler.SetCurrentSessionId(selectedSession.Id);
                Debug.WriteLine($"완료 처리 대상 세션 설정: {selectedSession.SessionName} (ID: {selectedSession.Id})");


                // 2단계: 사용자 확인
                var confirmResult = MessageBox.Show(
                            $"선택된 {selectedSessions.Count}개의 세션을 처리하시겠습니까?\n\n" +
                            "처리 내용:\n" +
                            "• 기존 raw_data, process_data 컬렉션 초기화\n" +
                            "• 세션별 계정명 데이터 추출 및 raw_data 저장\n" +
                            "• 자동으로 파일 로드 화면으로 이동\n\n" +
                            "※ 이 작업은 취소할 수 없습니다.",
                            "계정분석 시작 확인",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question
                        );

                if (confirmResult != DialogResult.Yes) return;

                // 3단계: 진행 상황 표시 및 처리 실행
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    progressForm.SetTitle("계정분석 처리 중...");

                    // SessionDataProcessor 생성 및 실행
                    var processor = new SessionDataProcessor();

                    try
                    {
                        var result = await processor.ProcessFullWorkflowAsync(
                            selectedSessions,
                             async (percentage, message) => await progressForm.UpdateProgressHandler(percentage, message)
                        );

                        await progressForm.UpdateProgressHandler(95, "결과 처리 중...");

                        if (result.Success)
                        {
                            await progressForm.UpdateProgressHandler(100, "처리 완료");
                            await Task.Delay(500);

                            // 4단계: 성공 시 결과 표시
                            string successMessage = $"계정분석 처리가 완료되었습니다.\n\n" +
                                                  $"• 처리된 세션: {selectedSessions.Count}개\n" +
                                                  $"• 처리된 파일: {result.ProcessedFileCount}개\n" +
                                                  $"• 처리된 데이터: {result.ProcessedRowCount:N0}건\n\n" +
                                                  "파일 로드 화면으로 이동합니다.";

                            MessageBox.Show(successMessage, "처리 완료",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

                            //현재 세션명 저장
                            DataHandler.currentSessionName = selectedSession.SessionName;

                            // 5단계: 자동으로 fileLoad.cs 화면으로 전환
                            NavigateToFileLoadScreen();


                        }
                        else
                        {
                            // 실패 시 오류 메시지 표시
                            string errorMessage = $"계정분석 처리 중 오류가 발생했습니다.\n\n" +
                                                 $"오류 내용: {result.ErrorMessage}\n\n" +
                                                 "로그를 확인하시거나 다시 시도해주세요.";

                            MessageBox.Show(errorMessage, "처리 실패",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    finally
                    {
                        // 리소스 정리
                        processor.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"계정분석 시작 오류: {ex.Message}");
                MessageBox.Show($"계정분석 시작 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btn_del_sessions_Click(object sender, EventArgs e)
        {
            try
            {
                // 1단계: 선택된 세션들 확인
                var selectedSessions = GetSelectedSessions();

                if (selectedSessions.Count == 0)
                {
                    MessageBox.Show("삭제할 세션을 선택해주세요.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 2단계: 일괄 삭제 확인 (한 번만)
                int totalFiles = selectedSessions.Sum(s => s.FileCount);
                decimal totalAmount = selectedSessions.Sum(s => s.TotalAmount);

                var confirmResult = MessageBox.Show(
                    $"선택된 {selectedSessions.Count}개의 세션을 삭제하시겠습니까?\\n\\n",
                    "세션 일괄 삭제 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (confirmResult != DialogResult.Yes) return;

                // 3단계: 진행 상황 표시하며 일괄 삭제 수행
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(5, "세션 일괄 삭제 시작...");

                    int deletedCount = 0;
                    int failedCount = 0;
                    var failedSessions = new List<string>();

                    for (int i = 0; i < selectedSessions.Count; i++)
                    {
                        var session = selectedSessions[i];
                        int currentProgress = 5 + ((i + 1) * 90 / selectedSessions.Count);

                        await progressForm.UpdateProgressHandler(currentProgress,
                            $"세션 삭제 중... ({i + 1}/{selectedSessions.Count}): {session.SessionName}");

                        try
                        {
                            // 현재 DataSource에서 해당 세션의 인덱스 찾기
                            var currentSessionList = (dgv_sessions.DataSource as List<SessionDisplayData>) ?? new List<SessionDisplayData>();
                            int rowIndex = currentSessionList.FindIndex(s => s.Id == session.Id);

                            if (rowIndex >= 0)
                            {
                                // skipConfirmation = true로 호출하여 개별 확인 생략
                                await DeleteSession(session, rowIndex, skipConfirmation: true);
                                deletedCount++;
                                Debug.WriteLine($"세션 삭제 성공: {session.SessionName}");
                            }
                            else
                            {
                                Debug.WriteLine($"세션을 목록에서 찾을 수 없음: {session.SessionName}");
                                failedCount++;
                                failedSessions.Add(session.SessionName);
                            }
                        }
                        catch (Exception deleteEx)
                        {
                            Debug.WriteLine($"세션 삭제 실패: {session.SessionName} - {deleteEx.Message}");
                            failedCount++;
                            failedSessions.Add(session.SessionName);
                        }

                        // UI 업데이트를 위한 짧은 지연
                        await Task.Delay(100);
                    }

                    await progressForm.UpdateProgressHandler(100, "일괄 삭제 완료");
                    await Task.Delay(500);

                    // 4단계: 결과 메시지 표시
                    string resultMessage = $"세션 일괄 삭제가 완료되었습니다.\\n\\n" +
                                          $"• 삭제 성공: {deletedCount}개\\n";

                    if (failedCount > 0)
                    {
                        resultMessage += $"• 삭제 실패: {failedCount}개\\n\\n" +
                                        "실패한 세션:\\n" +
                                        string.Join("\\n", failedSessions.Select(name => $"  - {name}"));

                        MessageBox.Show(resultMessage, "일괄 삭제 완료 (일부 실패)",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show(resultMessage, "일괄 삭제 완료",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    Debug.WriteLine($"세션 일괄 삭제 완료 - 성공: {deletedCount}, 실패: {failedCount}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 일괄 삭제 오류: {ex.Message}");
                MessageBox.Show($"세션 일괄 삭제 중 오류가 발생했습니다.\\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    /// ProcessProgressForm 제목 설정을 위한 확장 (필요시)
    /// </summary>
    public static class ProcessProgressFormExtensions
    {
        public static void SetTitle(this ProcessProgressForm form, string title)
        {
            if (form != null)
            {
                form.Text = title;
            }
        }
    }

    public partial class SessionPartitionPreviewDialog : Form
    {
        private List<AccountPartition> _partitions;
        private DataGridView dgvPreview;
        private Button btnOK;
        private Button btnCancel;

        public SessionPartitionPreviewDialog(List<AccountPartition> partitions)
        {
            _partitions = partitions;
            InitializeComponent();
            LoadPartitionData();
        }

        private void InitializeComponent()
        {
            this.Text = "세션 생성 미리보기";
            this.Size = new Size(850, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 상단 안내 레이블
            var lblInfo = new Label
            {
                Text = "계정명별로 분리된 세션들입니다. 세션명을 클릭하여 편집할 수 있습니다.",
                Location = new Point(10, 10),
                Size = new Size(800, 40),
                Font = new System.Drawing.Font("Malgun Gothic", 9),
                ForeColor = System.Drawing.Color.DarkBlue
            };
            this.Controls.Add(lblInfo);

            // DataGridView
            dgvPreview = new DataGridView
            {
                Location = new Point(10, 50),
                Size = new Size(800, 350),
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            // 컬럼 정의
            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "AccountName",
                HeaderText = "계정명",
                DataPropertyName = "AccountName",
                Width = 120,
                ReadOnly = true
            });

            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SessionName",
                HeaderText = "세션명 (편집가능)",
                DataPropertyName = "SessionName",
                Width = 300,
                ReadOnly = false
            });

            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FileCount",
                HeaderText = "파일 수",
                DataPropertyName = "FileCount",
                Width = 80,
                ReadOnly = true
            });

            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TotalRows",
                HeaderText = "총 행수",
                DataPropertyName = "TotalRowsFormatted",
                Width = 100,
                ReadOnly = true
            });

            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TotalAmount",
                HeaderText = "합산 금액",
                DataPropertyName = "TotalAmountFormatted",
                Width = 120,
                ReadOnly = true
            });

            this.Controls.Add(dgvPreview);

            // 버튼들
            btnCancel = new Button
            {
                Text = "취소",
                Location = new Point(630, 420),
                Size = new Size(70, 30),
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(btnCancel);

            btnOK = new Button
            {
                Text = "세션 생성",
                Location = new Point(710, 420),
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK
            };
            this.Controls.Add(btnOK);

            this.CancelButton = btnCancel;
            this.AcceptButton = btnOK;
        }

        private void LoadPartitionData()
        {
            // 표시용 데이터 준비
            var displayData = _partitions.Select(p => new
            {
                AccountName = p.AccountName,
                SessionName = p.SessionName,
                FileCount = p.FileCount,
                TotalRowsFormatted = p.TotalRows.ToString("N0"),
                TotalAmountFormatted = p.TotalAmount.ToString("N0") + " 원"
            }).ToList();

            dgvPreview.DataSource = displayData;
        }

        /// <summary>
        /// 사용자가 승인한 파티션들 반환 (편집된 세션명 포함)
        /// </summary>
        public List<AccountPartition> GetApprovedPartitions()
        {
            var approvedPartitions = new List<AccountPartition>();

            for (int i = 0; i < dgvPreview.Rows.Count; i++)
            {
                var row = dgvPreview.Rows[i];
                if (i < _partitions.Count)
                {
                    var partition = _partitions[i];

                    // 사용자가 편집한 세션명 적용
                    var editedSessionName = row.Cells["SessionName"].Value?.ToString();
                    if (!string.IsNullOrEmpty(editedSessionName))
                    {
                        partition.SessionName = editedSessionName.Trim();
                    }

                    approvedPartitions.Add(partition);
                }
            }

            return approvedPartitions;
        }

    }


    /// <summary>
    /// 계정명 파티션 정보
    /// </summary>
    public class AccountPartition
    {
        public string AccountName { get; set; }
        public string SessionName { get; set; }
        public List<FileDisplayData> Files { get; set; } = new List<FileDisplayData>();
        public int FileCount { get; set; }
        public decimal TotalRows { get; set; }
        public decimal TotalAmount { get; set; }
    }

    /// <summary>
    /// 파티션 분석 결과
    /// </summary>
    public class PartitionAnalysisResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<AccountPartition> Partitions { get; set; } = new List<AccountPartition>();
    }

}
