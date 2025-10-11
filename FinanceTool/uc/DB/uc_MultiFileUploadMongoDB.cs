using FinanceTool.MongoModels;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTool
{
    public partial class uc_MultiFileUpload
    {


        /// <summary>
        /// 업로드된 파일 처리
        /// </summary>
        private async Task<FileDisplayData> ProcessUploadedFile(string originalFilePath)
        {
            try
            {
                string originalFileName = Path.GetFileName(originalFilePath);

                // 동일 파일명 확인
                var existingFile = await _uploadedFileRepository.GetByFilenameAsync(originalFileName);
                if (existingFile != null)
                {
                    var result = MessageBox.Show(
                        $"'{originalFileName}' 파일이 이미 존재합니다.\n\n" +
                        "계속 업로드하시겠습니까?\n" +
                        "(새 파일명으로 저장됩니다)",
                        "동일 파일명 발견",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    if (result != DialogResult.Yes)
                    {
                        return null; // 업로드 취소
                    }
                }

                string fileExtension = Path.GetExtension(originalFileName);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string storedFileName = $"{timestamp}_{originalFileName}";
                string storedFilePath = Path.Combine(UPLOAD_FOLDER, storedFileName);

                // 파일 복사
                File.Copy(originalFilePath, storedFilePath);

                // 파일 정보 추출
                var fileInfo = new FileInfo(originalFilePath);
                var (detectedColumns, totalRows) = await ExtractExcelInfo(originalFilePath);

                //2025.06.30
                //에러 로직 추가
                if (totalRows < 0)
                {
                    MessageBox.Show(detectedColumns[0],
                                               "업로드 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // MongoDB에 저장
                var uploadedFile = new UploadedFileDocument
                {
                    OriginalFilename = originalFileName,
                    StoredFilename = storedFileName,
                    FilePath = UPLOAD_FOLDER,
                    FileSize = fileInfo.Length,
                    UploadDate = DateTime.UtcNow,
                    DetectedColumns = detectedColumns,
                    TotalRows = totalRows,
                    TotalAmount = 0,
                    ProcessingStatus = "uploaded"
                };

                // MongoDB 삽입 - 삽입 후 Id가 자동으로 생성됨
                await _uploadedFileRepository.CreateAsync(uploadedFile);

                // CreateAsync 후 uploadedFile.Id가 자동으로 설정됨
                return CreateFileDisplayData(uploadedFile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파일 처리 오류: {ex.Message}");
                return null;
            }
        }


        /// <summary>
        /// 기존 업로드된 파일들 로드
        /// </summary>
        private async void LoadExistingFiles()
        {
            try
            {
                var uploadedFiles = await _uploadedFileRepository.GetAllAsync();
                var displayDataList = new List<FileDisplayData>();

                foreach (var file in uploadedFiles)
                {
                    var displayData = CreateFileDisplayData(file);
                    displayDataList.Add(displayData);
                }

                dgv_files.DataSource = displayDataList;

                // 각 행의 콤보박스에 컬럼 목록 설정
                UpdateComboBoxItems();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"기존 파일 로드 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 계정명 컬럼 정보만 업데이트
        /// </summary>
        private async Task UpdateAccountColumnInfo(FileDisplayData fileData)
        {
            try
            {
                using (var progressForm = new ProcessProgressForm())
                {
                    string currentAccountColumn = fileData.AccountColumnName;

                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "계정명 컬럼 데이터 분석 시작...");
                    await Task.Delay(10);

                    // 계정명 컬럼이 선택된 경우만 검증
                    if (!string.IsNullOrEmpty(currentAccountColumn))
                    {
                        await progressForm.UpdateProgressHandler(40, "데이터 분석 중...");
                        await Task.Delay(10);


                        var accountValidation = await ValidateAndExtractAccountContent(fileData);

                        if (!accountValidation.IsValid)
                        {
                            MessageBox.Show(accountValidation.ErrorMessage, "계정명 컬럼 오류",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);

                            // 계정명 관련 데이터만 초기화
                            fileData.AccountColumnName = "";
                            fileData.AccountContents.Clear();
                            fileData.AccountContentFormatted = "";
                            currentAccountColumn = "";
                        }

                        // MongoDB에 계정명 정보만 저장
                        if (!string.IsNullOrEmpty(currentAccountColumn))
                        {
                            //await progressForm.UpdateProgressHandler(60, "데이터 처리 중...");
                            //await Task.Delay(10);


                            bool updated = await _uploadedFileRepository.UpdateAccountColumnInfoAsync(
                                fileData.Id,
                                currentAccountColumn,
                                fileData.AccountContents
                            );

                            if (!updated)
                            {
                                Debug.WriteLine($"MongoDB 계정명 정보 업데이트 실패: {fileData.OriginalFilename}");
                            }
                        }
                    }
                    else
                    {
                        // 계정명 컬럼 선택 해제
                        fileData.AccountContents.Clear();
                        fileData.AccountContentFormatted = "";
                    }

                    //await progressForm.UpdateProgressHandler(80, "데이터 갱신 중...");
                    //await Task.Delay(10);

                    // 콤보박스 값 설정 및 UI 새로고침
                    SetComboBoxValue(fileData, "AccountColumn", currentAccountColumn);
                    RefreshFileGridRowSpecific(fileData);

                    await progressForm.UpdateProgressHandler(100, "계정명 컬럼 분석 완료");
                    await Task.Delay(10);

                    Debug.WriteLine($"계정명 컬럼 정보 업데이트 완료: {fileData.OriginalFilename}");
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"계정명 컬럼 정보 업데이트 오류: {ex.Message}");
                MessageBox.Show($"계정명 컬럼 정보 업데이트 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 금액 컬럼 정보만 업데이트
        /// </summary>
        private async Task UpdateAmountColumnInfo(FileDisplayData fileData)
        {
            try
            {
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "금액 컬럼 데이터 분석 시작...");
                    await Task.Delay(10);

                    string currentAmountColumn = fileData.AmountColumnName;

                    // 금액 컬럼이 선택된 경우만 검증
                    if (!string.IsNullOrEmpty(currentAmountColumn))
                    {
                        await progressForm.UpdateProgressHandler(40, "금액 컬럼 데이터 분석 중...");
                        await Task.Delay(10);

                        var amountValidation = await ValidateAndCalculateAmount(fileData);



                        if (!amountValidation.IsValid)
                        {
                            progressForm.Hide();
                            MessageBox.Show(amountValidation.ErrorMessage, "금액 컬럼 오류",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);

                            // 금액 관련 데이터만 초기화
                            fileData.AmountColumnName = "";
                            fileData.TotalAmount = 0;
                            fileData.TotalAmountFormatted = "";
                            currentAmountColumn = "";
                        }

                        await progressForm.UpdateProgressHandler(80, "금액 컬럼 데이터 저장 중...");
                        await Task.Delay(10);

                        // MongoDB에 금액 정보만 저장
                        if (!string.IsNullOrEmpty(currentAmountColumn))
                        {
                            bool updated = await _uploadedFileRepository.UpdateAmountColumnInfoAsync(
                                fileData.Id,
                                currentAmountColumn,
                                fileData.TotalAmount
                            );

                            if (!updated)
                            {
                                Debug.WriteLine($"MongoDB 금액 정보 업데이트 실패: {fileData.OriginalFilename}");
                            }
                        }
                    }
                    else
                    {
                        // 금액 컬럼 선택 해제
                        fileData.TotalAmount = 0;
                        fileData.TotalAmountFormatted = "";
                    }

                    // 콤보박스 값 설정 및 UI 새로고침
                    SetComboBoxValue(fileData, "AmountColumn", currentAmountColumn);
                    RefreshFileGridRowSpecific(fileData);

                    await progressForm.UpdateProgressHandler(100, "금액 컬럼 데이터 저장 완료");
                    await Task.Delay(10);
                }



                Debug.WriteLine($"금액 컬럼 정보 업데이트 완료: {fileData.OriginalFilename}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"금액 컬럼 정보 업데이트 오류: {ex.Message}");
                MessageBox.Show($"금액 컬럼 정보 업데이트 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        /// <summary>
        /// 파일 삭제 처리
        /// </summary>
        private async Task DeleteFile(FileDisplayData fileData, int rowIndex)
        {
            try
            {
                // 1단계: 연관된 세션 확인
                var relatedSessions = await CheckFileSessionDependency(fileData.Id);

                if (relatedSessions.Count > 0)
                {
                    // 연관된 세션이 있는 경우 삭제 차단
                    ShowSessionDependencyWarning(fileData.OriginalFilename, relatedSessions);
                    return;
                }

                // 2단계: 기존 삭제 확인 로직
                var result = MessageBox.Show(
                    $"'{fileData.OriginalFilename}' 파일을 삭제하시겠습니까?\n\n" +
                    "※ 서버의 실제 파일과 데이터베이스 정보가 모두 삭제됩니다.",
                    "파일 삭제 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes) return;

                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(20, "파일 삭제 중...");

                    // 서버 파일 삭제
                    string filePath = Path.Combine(UPLOAD_FOLDER, fileData.StoredFilename);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        Debug.WriteLine($"서버 파일 삭제: {filePath}");
                    }

                    await progressForm.UpdateProgressHandler(60, "데이터베이스 정보 삭제 중...");

                    // MongoDB 데이터 삭제
                    bool mongoDeleted = await _uploadedFileRepository.DeleteAsync(fileData.Id);
                    if (!mongoDeleted)
                    {
                        throw new Exception("데이터베이스에서 파일 정보 삭제에 실패했습니다.");
                    }

                    await progressForm.UpdateProgressHandler(90, "목록 업데이트 중...");

                    // *** 개선: 다른 파일들의 콤보박스 상태 보존하면서 삭제 ***
                    await UpdateFileGridAfterDeletion(rowIndex);

                    await progressForm.UpdateProgressHandler(100, "삭제 완료");
                    await Task.Delay(500);
                }

                MessageBox.Show("파일이 성공적으로 삭제되었습니다.", "삭제 완료",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파일 삭제 오류: {ex.Message}");
                MessageBox.Show($"파일 삭제 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        //////////////////////////////////세션 테이블 관련 함수////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 세션명 업데이트 처리
        /// </summary>
        private async Task UpdateSessionName(SessionDisplayData sessionData, string newSessionName, int rowIndex)
        {
            string originalSessionName = sessionData.SessionName;

            try
            {
                // UI에서 먼저 업데이트 (즉시 반응)
                sessionData.SessionName = newSessionName;
                dgv_sessions.Rows[rowIndex].Cells["SessionName"].Value = newSessionName;

                // MongoDB 업데이트
                bool updated = await _fileSessionRepository.UpdateSessionNameAsync(sessionData.Id, newSessionName);

                if (updated)
                {
                    Debug.WriteLine($"세션명 업데이트 성공: {originalSessionName} → {newSessionName}");

                    // 성공 표시 (옵션: 조용한 알림)
                    dgv_sessions.Rows[rowIndex].Cells["SessionName"].Style.BackColor = System.Drawing.Color.LightGreen;

                    // 3초 후 배경색 원래대로
                    var timer = new System.Windows.Forms.Timer();
                    timer.Interval = 3000;
                    timer.Tick += (s, e) =>
                    {
                        if (rowIndex < dgv_sessions.Rows.Count)
                        {
                            dgv_sessions.Rows[rowIndex].Cells["SessionName"].Style.BackColor = System.Drawing.Color.White;
                        }
                        timer.Stop();
                        timer.Dispose();
                    };
                    timer.Start();
                }
                else
                {
                    // 실패 시 원래 값으로 복원
                    sessionData.SessionName = originalSessionName;
                    dgv_sessions.Rows[rowIndex].Cells["SessionName"].Value = originalSessionName;

                    MessageBox.Show("세션명 업데이트에 실패했습니다.\n잠시 후 다시 시도해주세요.", "업데이트 실패",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                // 오류 시 원래 값으로 복원
                sessionData.SessionName = originalSessionName;
                dgv_sessions.Rows[rowIndex].Cells["SessionName"].Value = originalSessionName;

                Debug.WriteLine($"세션명 업데이트 오류: {ex.Message}");
                MessageBox.Show($"세션명 업데이트 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 작업자명 업데이트 처리
        /// </summary>
        private async Task UpdateWorkerName(SessionDisplayData sessionData, string newWorkerName, int rowIndex)
        {
            string originalWorkerName = sessionData.WorkerName;

            try
            {
                // UI에서 먼저 업데이트 (즉시 반응)
                sessionData.WorkerName = newWorkerName;
                dgv_sessions.Rows[rowIndex].Cells["WorkerName"].Value = newWorkerName;

                // MongoDB 업데이트
                bool updated = await _fileSessionRepository.UpdateWorkerNameAsync(sessionData.Id, newWorkerName);

                if (updated)
                {
                    Debug.WriteLine($"작업자명 업데이트 성공: {originalWorkerName} → {newWorkerName}");

                    // 성공 표시 (옵션: 조용한 알림)
                    dgv_sessions.Rows[rowIndex].Cells["WorkerName"].Style.BackColor = System.Drawing.Color.LightGreen;

                    // 3초 후 배경색 원래대로
                    var timer = new System.Windows.Forms.Timer();
                    timer.Interval = 3000;
                    timer.Tick += (s, e) =>
                    {
                        if (rowIndex < dgv_sessions.Rows.Count)
                        {
                            dgv_sessions.Rows[rowIndex].Cells["WorkerName"].Style.BackColor = System.Drawing.Color.White;
                        }
                        timer.Stop();
                        timer.Dispose();
                    };
                    timer.Start();
                }
                else
                {
                    // 실패 시 원래 값으로 복원
                    sessionData.WorkerName = originalWorkerName;
                    dgv_sessions.Rows[rowIndex].Cells["WorkerName"].Value = originalWorkerName;

                    MessageBox.Show("작업자명 업데이트에 실패했습니다.\n잠시 후 다시 시도해주세요.", "업데이트 실패",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                // 오류 시 원래 값으로 복원
                sessionData.SessionName = originalWorkerName;
                dgv_sessions.Rows[rowIndex].Cells["WorkerName"].Value = originalWorkerName;

                Debug.WriteLine($"세션명 업데이트 오류: {ex.Message}");
                MessageBox.Show($"세션명 업데이트 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 세션 삭제 처리
        /// </summary>
        private async Task DeleteSession(SessionDisplayData sessionData, int rowIndex, bool skipConfirmation = false)
        {
            try
            {
                // skipConfirmation이 false일 때만 확인 메시지 표시
                if (!skipConfirmation)
                {
                    var result = MessageBox.Show(
                        $"'{sessionData.SessionName}' 세션을 삭제하시겠습니까?\\n\\n" +
                        $"• 파일 개수: {sessionData.FileCount}개\\n" +
                        $"• 합산 금액: {sessionData.TotalAmountFormatted}\\n\\n" +
                        "※ 세션 정보만 삭제되며, 업로드된 파일은 유지됩니다.\\n" +
                        "※ 연결된 파일들은 다시 개별 파일로 분리됩니다.",
                        "세션 삭제 확인",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    if (result != DialogResult.Yes) return;
                }

                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(20, "세션 정보 조회 중...");

                    Debug.WriteLine($"삭제할 세션 ID: {sessionData.Id}");
                    Debug.WriteLine($"세션 ID 타입: {sessionData.Id.GetType()}");

                    // MongoDB에서 세션 정보 조회
                    //var session = await _fileSessionRepository.GetByIdAsync(sessionData.Id.ToString());
                    var session = await _fileSessionRepository.GetByIdAsync(sessionData.Id);

                    if (session == null)
                    {
                        Debug.WriteLine("세션 조회 실패 - 직접 삭제 시도");

                        // 세션이 조회되지 않더라도 강제로 삭제 시도
                        bool directDelete = await _fileSessionRepository.DeleteAsync(sessionData.Id);
                        if (directDelete)
                        {
                            Debug.WriteLine("직접 삭제 성공");

                            // UI에서만 제거
                            var currentSessionsInner = (dgv_sessions.DataSource as List<SessionDisplayData>) ?? new List<SessionDisplayData>();
                            currentSessionsInner.RemoveAt(rowIndex);
                            dgv_sessions.DataSource = currentSessionsInner.ToList();

                            if (!skipConfirmation)
                            {
                                MessageBox.Show("세션이 삭제되었습니다.", "삭제 완료",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            
                            return;
                        }
                        else
                        {
                            throw new Exception("세션 정보를 찾을 수 없고 삭제도 실패했습니다.");
                        }
                    }

                    Debug.WriteLine($"세션 조회 성공: FileIds 개수 = {session.FileIds?.Count ?? 0}");

                    await progressForm.UpdateProgressHandler(40, "연결된 파일들의 세션 해제 중...");

                    // 연결된 파일들의 session_id 초기화
                    var affectedFiles = new List<ObjectId>();
                    if (session.FileIds != null && session.FileIds.Count > 0)
                    {
                        Debug.WriteLine($"처리할 파일 ID 목록: {string.Join(", ", session.FileIds)}");

                        foreach (var fileId in session.FileIds)
                        {
                            try
                            {
                                Debug.WriteLine($"파일 세션 해제 시도: {fileId}");

                                // 방법 1: Unset을 사용한 직접 업데이트
                                var filter = Builders<UploadedFileDocument>.Filter.Eq("_id", fileId);
                                var update = Builders<UploadedFileDocument>.Update.Unset("session_id");

                                var updateResult = await _uploadedFileRepository.Collection.UpdateOneAsync(filter, update);

                                if (updateResult.ModifiedCount > 0)
                                {
                                    affectedFiles.Add(fileId);
                                    Debug.WriteLine($"파일 세션 해제 성공: {fileId}");
                                }
                                else
                                {
                                    Debug.WriteLine($"파일 세션 해제 실패 (수정된 문서 없음): {fileId}");
                                }
                            }
                            catch (Exception fileEx)
                            {
                                Debug.WriteLine($"파일 {fileId} 세션 해제 오류: {fileEx.Message}");
                            }
                        }
                    }

                    await progressForm.UpdateProgressHandler(70, "세션 데이터 삭제 중...");

                    // MongoDB에서 세션 삭제
                    bool sessionDeleted = await _fileSessionRepository.DeleteAsync(sessionData.Id);
                    if (!sessionDeleted)
                    {
                        Debug.WriteLine("세션 삭제 실패했지만 계속 진행");
                    }

                    await progressForm.UpdateProgressHandler(85, "파일 목록 업데이트 중...");

                    // dgv_files에서 해당 파일들의 세션 정보 초기화
                    UpdateFileGridAfterSessionDeletion(affectedFiles);

                    await progressForm.UpdateProgressHandler(95, "세션 목록 업데이트 중...");

                    // dgv_sessions에서 제거
                    var currentSessions = (dgv_sessions.DataSource as List<SessionDisplayData>) ?? new List<SessionDisplayData>();
                    if (rowIndex < currentSessions.Count)
                    {
                        currentSessions.RemoveAt(rowIndex);
                        dgv_sessions.DataSource = currentSessions.ToList();
                    }

                    await progressForm.UpdateProgressHandler(100, "삭제 완료");
                    await Task.Delay(500);

                    if (!skipConfirmation)
                    {
                        MessageBox.Show(
                                        $"세션이 성공적으로 삭제되었습니다.\n\n",
                                        "삭제 완료",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Information
                                        );
                    }
                    
                    Debug.WriteLine($"세션 삭제 완료: {sessionData.SessionName}, 해제된 파일: {affectedFiles.Count}개");

                }



            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 삭제 오류: {ex.Message}");
                Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
                MessageBox.Show($"세션 삭제 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 기존 세션들 로드
        /// </summary>
        private async void LoadExistingSessions()
        {
            try
            {
                var sessions = await _fileSessionRepository.GetAllAsync();
                var sessionDisplayList = new List<SessionDisplayData>();

                foreach (var session in sessions)
                {
                    var displayData = new SessionDisplayData
                    {
                        Id = session.Id,
                        IsSelected = false,
                        SessionName = session.SessionName,
                        AccountColumnName = session.AccountColumnName,
                        AmountColumnName = session.AmountColumnName,
                        AccountName = session.AccountName ?? session.AccountColumnName ?? "", // *** AccountName 정보 추가 (fallback 포함) ***
                        TotalAmount = session.TotalAmount,
                        TotalAmountFormatted = session.TotalAmount.ToString("N0") + " 원",
                        TotalRows = session.TotalRows,
                        TotalRowsFormatted = session.TotalRows > 0 ? session.TotalRows.ToString("N0") : "0",
                        FileCount = session.FileIds?.Count ?? 0,
                        Status = session.Status,
                        StatusDisplay = session.CompletedDate.HasValue ? "완료" : "처리중",
                        CreatedDate = session.CreatedDate,
                        CreatedDateFormatted = session.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
                        CompletedDate = session.CompletedDate,
                        CompletedDateFormatted = session.CompletedDate?.ToString("yyyy-MM-dd HH:mm") ?? "",
                        ResultFilePath = session.ResultFilePath
                    };
                    sessionDisplayList.Add(displayData);
                }

                dgv_sessions.DataSource = sessionDisplayList;

                // 데이터 바인딩 후 색상 새로고침
                await Task.Delay(100);
                //RefreshSessionGridColors();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"기존 세션 로드 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 세션 그리드에서 특정 세션 정보 업데이트
        /// </summary>
        private void UpdateSessionInGrid(SessionDisplayData updatedSession)
        {
            try
            {
                var sessionList = dgv_sessions.DataSource as List<SessionDisplayData>;
                if (sessionList != null)
                {
                    var index = sessionList.FindIndex(s => s.Id == updatedSession.Id);
                    if (index >= 0)
                    {
                        sessionList[index] = updatedSession;
                        dgv_sessions.DataSource = sessionList.ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 그리드 업데이트 오류: {ex.Message}");
            }
        }

    }
}
