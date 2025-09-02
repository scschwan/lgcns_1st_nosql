using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FinanceTool.MongoModels;
using MongoDB.Bson;
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
        /// 검증 결과 클래스
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }
        }


        /// <summary>
        /// 세션 표시용 데이터 클래스
        /// </summary>
        public class SessionDisplayData
        {
            public ObjectId Id { get; set; }
            public bool IsSelected { get; set; } = false; // 체크박스용 속성 추가
            public string SessionName { get; set; }
            public string AccountColumnName { get; set; }
            public string AmountColumnName { get; set; }
            public decimal TotalAmount { get; set; }
            public string TotalAmountFormatted { get; set; }
            public decimal TotalRows { get; set; }
            public string TotalRowsFormatted { get; set; }
            public int FileCount { get; set; }
            public string Status { get; set; }
            public string StatusDisplay { get; set; }
            public DateTime CreatedDate { get; set; }
            public string CreatedDateFormatted { get; set; }
            public DateTime? CompletedDate { get; set; }
            public string CompletedDateFormatted { get; set; }
            public string ResultFilePath { get; set; }
            // *** 새로 추가: 계정명 내용 표시용 ***
            public string AccountName { get; set; } // MongoDB에서 가져온 실제 계정명
            public string AccountNameFormatted
            {
                get
                {
                    if (string.IsNullOrEmpty(AccountName))
                        return "";

                    // 병합된 계정명인 경우 (쉼표 포함)
                    if (AccountName.Contains(","))
                    {
                        var accountNames = AccountName.Split(',')
                            .Select(name => name.Trim())
                            .Where(name => !string.IsNullOrEmpty(name))
                            .ToArray();

                        if (accountNames.Length <= 2)
                        {
                            return string.Join(", ", accountNames);
                        }
                        else
                        {
                            return $"{accountNames[0]}, {accountNames[1]}... ({accountNames.Length}개)";
                        }
                    }
                    else
                    {
                        return AccountName;
                    }
                }
            }
        }


        //////////////////////////////////세션 테이블 관련 함수////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// SessionDisplayData 생성 (기존 CreateFileDisplayData와 유사)
        /// </summary>
        private SessionDisplayData CreateSessionDisplayData(FileSessionDocument session)
        {
            return new SessionDisplayData
            {
                Id = session.Id,
                SessionName = session.SessionName,
                AccountColumnName = session.AccountColumnName,
                AmountColumnName = session.AmountColumnName,
                AccountName = session.AccountName,
                //AccountNameFormatted = session.AccountName ?? "",
                TotalAmount = session.TotalAmount,
                TotalAmountFormatted = session.TotalAmount.ToString("N0") + " 원",
                TotalRows = session.TotalRows,
                TotalRowsFormatted = session.TotalRows.ToString("N0"),
                FileCount = session.FileIds?.Count ?? 0,
                Status = session.Status,
                StatusDisplay = GetStatusDisplay(session.Status),
                CreatedDate = session.CreatedDate,
                CreatedDateFormatted = session.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
                CompletedDate = session.CompletedDate,
                CompletedDateFormatted = session.CompletedDate?.ToString("yyyy-MM-dd HH:mm") ?? "",
                ResultFilePath = session.ResultFilePath,
                IsSelected = false
            };
        }



        /// <summary>
        /// 세션 목록 새로고침
        /// </summary>
        private async Task RefreshSessionsList()
        {
            try
            {
                // MongoDB에서 최신 세션 목록 조회
                var sessions = await _fileSessionRepository.GetAllAsync();
                var sessionDisplayList = new List<SessionDisplayData>();

                foreach (var session in sessions)
                {
                    var displayData = CreateSessionDisplayData(session);
                    sessionDisplayList.Add(displayData);
                }

                // DataGridView 업데이트
                dgv_sessions.DataSource = sessionDisplayList;

                Debug.WriteLine($"세션 목록 새로고침 완료: {sessionDisplayList.Count}개 세션");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 목록 새로고침 오류: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// 세션 목록 DataGridView 초기화
        /// </summary>
        private void InitializeSessionsDataGridView()
        {
            dgv_sessions.AutoGenerateColumns = false;
            dgv_sessions.AllowUserToAddRows = false;
            dgv_sessions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            //dgv_sessions.MultiSelect = false;
            dgv_sessions.MultiSelect = true;

            // *** 행 높이 고정 설정 추가 ***
            dgv_sessions.AllowUserToResizeRows = false;  // 사용자가 행 높이 조절 못하게
            dgv_sessions.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing; // 행 헤더 크기 조절 방지
            dgv_sessions.RowTemplate.Height = 25; // 기본 행 높이 설정 (픽셀)
            dgv_sessions.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None; // 자동 높이 조절 방지


            // 컬럼 정의
            dgv_sessions.Columns.Clear();

            // 체크박스 컬럼 추가 (좌측 첫 번째, 고정)
            var checkBoxColumn = new DataGridViewCheckBoxColumn
            {
                Name = "SelectedSession",
                HeaderText = "",
                DataPropertyName = "IsSelected",
                Width = 30,
                ThreeState = false,
                Frozen = true // 스크롤 시에도 고정
            };
            dgv_sessions.Columns.Add(checkBoxColumn);

            // 세션명 컬럼
            // *** 세션명 컬럼 (편집 가능) ***
            var sessionNameColumn = new DataGridViewTextBoxColumn
            {
                Name = "SessionName",
                HeaderText = "세션명 (편집가능)",
                DataPropertyName = "SessionName",
                Width = 250,
                DefaultCellStyle = {
                    BackColor =System.Drawing.Color.LightYellow
                },
                Frozen = true, // 스크롤 시에도 고정
                ReadOnly = false // 편집 가능하도록 설정

            };
            dgv_sessions.Columns.Add(sessionNameColumn);

            // 계정명 컬럼
            var accountColumnColumn = new DataGridViewTextBoxColumn
            {
                Name = "AccountColumnName",
                HeaderText = "계정명 컬럼",
                DataPropertyName = "AccountColumnName",
                Width = 120,
                ReadOnly = true
            };
            dgv_sessions.Columns.Add(accountColumnColumn);

            // *** 새로 추가: 계정명 내용 컬럼 ***
            var accountNameColumn = new DataGridViewTextBoxColumn
            {
                Name = "AccountName",
                HeaderText = "계정명 내용",
                DataPropertyName = "AccountNameFormatted", // 새 속성 필요
                Width = 250,
                ReadOnly = true
            };
            dgv_sessions.Columns.Add(accountNameColumn);

            // 금액 컬럼
            var amountColumnColumn = new DataGridViewTextBoxColumn
            {
                Name = "AmountColumnName",
                HeaderText = "금액 컬럼",
                DataPropertyName = "AmountColumnName",
                Width = 120,
                ReadOnly = true
            };
            dgv_sessions.Columns.Add(amountColumnColumn);



            // 총 행수 컬럼
            var totalRowsColumn = new DataGridViewTextBoxColumn
            {
                Name = "TotalRows",
                HeaderText = "총 행수",
                //DataPropertyName = "TotalRows",
                DataPropertyName = "TotalRowsFormatted",
                Width = 80,
                ReadOnly = true
            };
            dgv_sessions.Columns.Add(totalRowsColumn);

            // 합산 금액 컬럼
            var totalAmountColumn = new DataGridViewTextBoxColumn
            {
                Name = "TotalAmount",
                HeaderText = "합산 금액",
                DataPropertyName = "TotalAmountFormatted",
                Width = 120,
                ReadOnly = true
            };
            dgv_sessions.Columns.Add(totalAmountColumn);

            // 상태 컬럼 (completed_date 기준으로 표시)
            var statusColumn = new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "상태",
                DataPropertyName = "StatusDisplay",
                Width = 80,
                ReadOnly = true
            };
            dgv_sessions.Columns.Add(statusColumn);



            // 완료일 컬럼 (completed_date 표시)
            var completedDateColumn = new DataGridViewTextBoxColumn
            {
                Name = "CompletedDate",
                HeaderText = "완료일",
                DataPropertyName = "CompletedDateFormatted",
                Width = 130,
                ReadOnly = true
            };
            dgv_sessions.Columns.Add(completedDateColumn);

            // 결과 다운로드 버튼 컬럼 (result_file_path 기준으로 활성화/비활성화)
            var downloadButtonColumn = new DataGridViewButtonColumn
            {
                Name = "DownloadButton",
                HeaderText = "다운로드",
                Text = "📥",
                UseColumnTextForButtonValue = true,
                Width = 80
            };
            dgv_sessions.Columns.Add(downloadButtonColumn);

            // 삭제 버튼 컬럼 추가
            var deleteButtonColumn = new DataGridViewButtonColumn
            {
                Name = "DeleteSessionButton",
                HeaderText = "삭제",
                Text = "🗑️",
                UseColumnTextForButtonValue = true,
                Width = 60
            };
            dgv_sessions.Columns.Add(deleteButtonColumn);


            // 파일 개수 컬럼
            var fileCountColumn = new DataGridViewTextBoxColumn
            {
                Name = "FileCount",
                HeaderText = "파일 수",
                DataPropertyName = "FileCount",
                Width = 70,
                ReadOnly = true
            };
            dgv_sessions.Columns.Add(fileCountColumn);

            // 생성일 컬럼
            var createdDateColumn = new DataGridViewTextBoxColumn
            {
                Name = "CreatedDate",
                HeaderText = "생성일",
                DataPropertyName = "CreatedDateFormatted",
                Width = 130,
                ReadOnly = true
            };
            dgv_sessions.Columns.Add(createdDateColumn);


            // 이벤트 핸들러 등록
            dgv_sessions.CellContentClick += Dgv_sessions_CellContentClick;
            //dgv_sessions.CellValueChanged += Dgv_sessions_CellValueChanged;
            dgv_sessions.CellFormatting += Dgv_sessions_CellFormatting;
            dgv_sessions.CellValueChanged += Dgv_sessions_CellValueChanged; // 세션명 편집용
            dgv_sessions.CellValidating += Dgv_sessions_CellValidating; // 유효성 검사용
            dgv_sessions.CellToolTipTextNeeded += Dgv_sessions_CellToolTipTextNeeded; // *** 새로 추가 ***
        }

        /// <summary>
        /// 세션 삭제 후 파일 그리드 업데이트
        /// </summary>
        private void UpdateFileGridAfterSessionDeletion(List<ObjectId> affectedFileIds)
        {
            try
            {
                var fileList = dgv_files.DataSource as List<FileDisplayData>;
                if (fileList == null) return;

                // 해당 파일들의 SessionId 초기화
                foreach (var fileData in fileList)
                {
                    if (affectedFileIds.Contains(fileData.Id))
                    {
                        fileData.SessionId = null;
                        Debug.WriteLine($"파일 세션 해제: {fileData.OriginalFilename}");
                    }
                }

                // 그리드 새로고침
                dgv_files.Invalidate();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파일 그리드 업데이트 오류: {ex.Message}");
            }
        }



        /// <summary>
        /// 세션 결과 파일 다운로드
        /// </summary>
        private async Task DownloadSessionResult(SessionDisplayData sessionData)
        {
            try
            {
                // 1. 세션 상태 및 결과 파일 경로 확인
                if (sessionData.Status != "completed")
                {
                    MessageBox.Show($"'{sessionData.SessionName}' 세션이 아직 완료되지 않았습니다.\n완료된 세션만 다운로드할 수 있습니다.",
                        "다운로드 불가", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 2. MongoDB에서 최신 세션 정보 조회
                var sessionDoc = await _fileSessionRepository.GetByIdAsync(sessionData.Id);
                if (sessionDoc == null)
                {
                    MessageBox.Show("세션 정보를 찾을 수 없습니다.", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (string.IsNullOrEmpty(sessionDoc.ResultFilePath))
                {
                    MessageBox.Show($"'{sessionData.SessionName}' 세션의 결과 파일 경로가 설정되지 않았습니다.\n" +
                                  "Excel 파일을 다시 생성해주세요.", "다운로드 불가",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 3. 서버 파일 존재 여부 확인
                if (!File.Exists(sessionDoc.ResultFilePath))
                {
                    MessageBox.Show($"서버에서 결과 파일을 찾을 수 없습니다.\n" +
                                  $"파일 경로: {sessionDoc.ResultFilePath}\n\n" +
                                  "파일이 삭제되었거나 이동되었을 수 있습니다.",
                        "파일 없음", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 4. 다운로드 경로 선택
                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    string originalFileName = Path.GetFileName(sessionDoc.ResultFilePath);
                    string nameWithoutTimestamp = RemoveTimestampFromFileName(originalFileName);

                    saveDialog.Title = "세션 결과 파일 다운로드";
                    saveDialog.Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*";
                    saveDialog.FileName = nameWithoutTimestamp;
                    saveDialog.DefaultExt = "xlsx";
                    saveDialog.AddExtension = true;

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        string downloadPath = saveDialog.FileName;

                        // 5. 파일 복사 진행
                        using (var progressForm = new ProcessProgressForm())
                        {
                            progressForm.Show();
                            await progressForm.UpdateProgressHandler(10, "다운로드 준비 중...");

                            bool downloadSuccess = await PerformFileDownload(sessionDoc.ResultFilePath, downloadPath, progressForm.UpdateProgressHandler);

                            if (downloadSuccess)
                            {
                                await progressForm.UpdateProgressHandler(100, "다운로드 완료");
                                await Task.Delay(500);

                                // 6. 다운로드 완료 후 파일 열기 여부 확인
                                DialogResult openResult = MessageBox.Show(
                                    $"'{sessionData.SessionName}' 세션 결과 파일이 성공적으로 다운로드되었습니다.\n\n" +
                                    $"저장 경로: {downloadPath}\n\n" +
                                    "다운로드된 파일을 열어보시겠습니까?",
                                    "다운로드 완료", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                                if (openResult == DialogResult.Yes)
                                {
                                    try
                                    {
                                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                                        {
                                            FileName = downloadPath,
                                            UseShellExecute = true
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"파일 열기 실패: {ex.Message}");
                                        MessageBox.Show($"파일을 열 수 없습니다.\n수동으로 파일을 열어주세요.\n\n경로: {downloadPath}",
                                            "파일 열기 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show("파일 다운로드에 실패했습니다.", "다운로드 실패",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 결과 다운로드 중 오류: {ex.Message}");
                MessageBox.Show($"다운로드 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 실제 파일 다운로드 수행
        /// </summary>
        private async Task<bool> PerformFileDownload(string sourcePath, string destinationPath, ProcessProgressForm.UpdateProgressDelegate progressCallback)
        {
            try
            {
                await progressCallback(20, "파일 정보 확인 중...");

                // 파일 크기 확인
                FileInfo sourceFileInfo = new FileInfo(sourcePath);
                long totalBytes = sourceFileInfo.Length;
                long copiedBytes = 0;

                await progressCallback(30, "파일 복사 시작...");

                // 대용량 파일을 위한 스트림 복사
                using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
                using (var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[81920]; // 80KB 버퍼
                    int bytesRead;

                    while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await destinationStream.WriteAsync(buffer, 0, bytesRead);
                        copiedBytes += bytesRead;

                        // 진행률 계산 및 업데이트
                        int progressPercent = (int)(30 + (copiedBytes * 60 / totalBytes));
                        await progressCallback(progressPercent, $"파일 복사 중... ({FormatFileSize(copiedBytes)}/{FormatFileSize(totalBytes)})");
                    }
                }

                await progressCallback(95, "다운로드 검증 중...");

                // 파일 크기 검증
                FileInfo destinationFileInfo = new FileInfo(destinationPath);
                if (destinationFileInfo.Length != totalBytes)
                {
                    Debug.WriteLine($"파일 크기 불일치: 원본={totalBytes}, 복사본={destinationFileInfo.Length}");
                    return false;
                }

                Debug.WriteLine($"파일 다운로드 성공: {sourcePath} → {destinationPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파일 다운로드 중 오류: {ex.Message}");

                // 실패 시 부분적으로 복사된 파일 삭제
                try
                {
                    if (File.Exists(destinationPath))
                    {
                        File.Delete(destinationPath);
                        Debug.WriteLine("부분적으로 복사된 파일 삭제됨");
                    }
                }
                catch (Exception deleteEx)
                {
                    Debug.WriteLine($"부분 복사 파일 삭제 실패: {deleteEx.Message}");
                }

                return false;
            }
        }

        /// <summary>
        /// 파일명에서 타임스탬프 제거
        /// </summary>
        private string RemoveTimestampFromFileName(string fileName)
        {
            try
            {
                // 파일명 패턴: {sessionId}_{timestamp}_{originalName}.xlsx
                // 예: 507f1f77_20250716_143052_결과.xlsx → 결과.xlsx

                string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                string extension = Path.GetExtension(fileName);

                // '_'로 분할하여 세션ID와 타임스탬프 부분 제거
                string[] parts = nameWithoutExt.Split('_');

                if (parts.Length >= 3)
                {
                    // 처음 두 부분(세션ID, 타임스탬프)을 제거하고 나머지 결합
                    string cleanName = string.Join("_", parts.Skip(2));
                    return cleanName + extension;
                }

                // 패턴이 맞지 않으면 원본 파일명 반환
                return fileName;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파일명 정리 중 오류: {ex.Message}");
                return fileName;
            }
        }

        /// <summary>
        /// 선택된 파일들의 기본 검증
        /// </summary>
        private ValidationResult ValidateSelectedFiles(List<FileDisplayData> selectedFiles)
        {
            foreach (var file in selectedFiles)
            {
                // 계정명 컬럼 선택 확인
                if (string.IsNullOrEmpty(file.AccountColumnName))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"'{file.OriginalFilename}' 파일의 계정명 컬럼을 선택해주세요."
                    };
                }

                // 금액 컬럼 선택 확인
                if (string.IsNullOrEmpty(file.AmountColumnName))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"'{file.OriginalFilename}' 파일의 금액 컬럼을 선택해주세요."
                    };
                }
            }

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// 개별 파일의 실제 데이터 처리
        /// </summary>
        private async Task<(int RowCount, decimal TotalAmount)> ProcessFileForSession(FileDisplayData file)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string filePath = Path.Combine(UPLOAD_FOLDER, file.StoredFilename);
                    int rowCount = 0;
                    decimal totalAmount = 0;

                    using (var document = SpreadsheetDocument.Open(filePath, false))
                    {
                        var workbookPart = document.WorkbookPart;
                        var worksheet = workbookPart.WorksheetParts.First().Worksheet;
                        var sheetData = worksheet.GetFirstChild<SheetData>();

                        var allRows = sheetData.Elements<Row>().ToList();
                        if (allRows.Count <= 1) return (0, 0); // 헤더만 있는 경우

                        // 헤더에서 금액 컬럼 인덱스 찾기
                        var headerRow = allRows.First();
                        var headerCells = headerRow.Elements<Cell>().ToList();
                        int amountColumnIndex = -1;

                        for (int i = 0; i < headerCells.Count; i++)
                        {
                            string cellValue = GetCellValue(headerCells[i], workbookPart);
                            if (cellValue.Trim().Equals(file.AmountColumnName.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                amountColumnIndex = i;
                                break;
                            }
                        }

                        if (amountColumnIndex == -1) return (0, 0);

                        // 데이터 행들 처리
                        for (int rowIndex = 1; rowIndex < allRows.Count; rowIndex++)
                        {
                            var row = allRows[rowIndex];
                            var cells = row.Elements<Cell>().ToList();

                            if (amountColumnIndex < cells.Count)
                            {
                                string cellValue = GetCellValue(cells[amountColumnIndex], workbookPart);
                                if (decimal.TryParse(cellValue.Replace(",", ""), out decimal amount))
                                {
                                    totalAmount += amount;
                                }
                                rowCount++;
                            }
                        }
                    }

                    return (rowCount, totalAmount);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"파일 데이터 처리 오류: {ex.Message}");
                    return (0, 0);
                }
            });
        }



        /// <summary>
        /// 세션 생성 시에도 AccountName 정보 포함하도록 수정
        /// </summary>
        private void AddSessionToGrid(SessionDisplayData sessionData)
        {
            try
            {
                var currentSessions = (dgv_sessions.DataSource as List<SessionDisplayData>) ?? new List<SessionDisplayData>();
                currentSessions.Add(sessionData);
                //dgv_sessions.DataSource = currentSessions.ToList();

                // *** DataSource 재설정으로 강제 새로고침 ***
                dgv_sessions.DataSource = null; // 기존 바인딩 해제
                dgv_sessions.DataSource = currentSessions.ToList(); // 새 데이터로 바인딩

                // *** 추가: AccountName 컬럼 새로고침 강제 실행 ***
                dgv_sessions.Refresh();

                Debug.WriteLine($"세션 추가: {sessionData.SessionName}, AccountName: {sessionData.AccountName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 그리드 추가 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 파일들과 세션의 호환성 검증
        /// </summary>
        private ValidationResult ValidateFilesCompatibilityWithSession(List<FileDisplayData> files, SessionDisplayData session)
        {
            foreach (var file in files)
            {
                // 계정명 컬럼 일치 확인
                if (!file.AccountColumnName.Trim().Equals(session.AccountColumnName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"파일 '{file.OriginalFilename}'의 계정명 컬럼이 세션과 일치하지 않습니다.\n\n" +
                                     $"• 파일: '{file.AccountColumnName}'\n" +
                                     $"• 세션: '{session.AccountColumnName}'"
                    };
                }

                // 금액 컬럼 일치 확인
                if (!file.AmountColumnName.Trim().Equals(session.AmountColumnName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"파일 '{file.OriginalFilename}'의 금액 컬럼이 세션과 일치하지 않습니다.\n\n" +
                                     $"• 파일: '{file.AmountColumnName}'\n" +
                                     $"• 세션: '{session.AmountColumnName}'"
                    };
                }

                // 계정명 내용 일치 확인
                string fileAccountContent = file.AccountContents.FirstOrDefault()?.Trim().ToUpper() ?? "";
                // 세션의 계정명 내용은 세션에 포함된 첫 번째 파일에서 가져와야 함 (추후 구현 시 고려)
                // 현재는 단순히 동일한 계정명 컬럼을 사용하는지만 확인
            }

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// 기존 세션에 파일들 추가
        /// </summary>
        private async Task<(bool Success, SessionDisplayData UpdatedSession)> AddFilesToExistingSession(
            List<FileDisplayData> files, SessionDisplayData targetSession,
            ProcessProgressForm.UpdateProgressDelegate progressCallback)
        {
            try
            {
                await progressCallback(20, "파일 데이터 처리 중...");

                // 추가할 파일들의 데이터 합산
                int additionalRows = 0;
                decimal additionalAmount = 0;

                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    var fileData = await ProcessFileForSession(file);

                    additionalRows += fileData.RowCount;
                    additionalAmount += fileData.TotalAmount;

                    int progress = 20 + ((i + 1) * 40 / files.Count);
                    await progressCallback(progress, $"파일 처리 중... ({i + 1}/{files.Count})");
                }

                await progressCallback(70, "MongoDB 업데이트 중...");

                // MongoDB에서 세션 업데이트
                var fileIds = files.Select(f => f.Id).ToList();

                // 세션에 파일 ID 추가
                foreach (var fileId in fileIds)
                {
                    await _fileSessionRepository.AddFileToSessionAsync(targetSession.Id, fileId);
                }

                // 세션 총합 업데이트
                decimal newTotalAmount = targetSession.TotalAmount + additionalAmount;
                decimal newTotalRows = targetSession.TotalRows + additionalRows;
                int newFileCount = targetSession.FileCount + files.Count;

                await _fileSessionRepository.UpdateSessionTotalsAsync(targetSession.Id, newTotalAmount, newTotalRows);

                // 파일들의 session_id 업데이트
                foreach (var file in files)
                {
                    await _uploadedFileRepository.UpdateSessionIdAsync(file.Id, targetSession.Id);
                    file.SessionId = targetSession.Id;
                }

                await progressCallback(85, "세션 정보 업데이트 중...");

                // 메모리의 세션 데이터 업데이트
                targetSession.TotalAmount = newTotalAmount;
                targetSession.TotalAmountFormatted = newTotalAmount.ToString("N0") + " 원";
                targetSession.TotalRows = newTotalRows;
                targetSession.TotalRowsFormatted = newTotalRows.ToString("N0");
                targetSession.FileCount = newFileCount;

                return (true, targetSession);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션에 파일 추가 처리 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 세션들 병합 처리
        /// </summary>
        private async Task<(bool Success, SessionDisplayData MergedSession, List<SessionDisplayData> DeletedSessions)>
        MergeSessions(List<SessionDisplayData> sessions, ProcessProgressForm.UpdateProgressDelegate progressCallback)
        {
            try
            {
                // 첫 번째 세션을 기준으로 병합
                var targetSession = sessions[0];
                var sessionsToDelete = sessions.Skip(1).ToList();

                await progressCallback(20, "세션 정보 수집 중...");

                // 모든 세션의 계정명 수집
                var allAccountNames = new List<string>();
                var allFileIds = new List<ObjectId>();
                decimal totalAmount = 0;
                decimal totalRows = 0;

                foreach (var session in sessions)
                {
                    // MongoDB에서 실제 세션 정보 조회
                    var sessionDoc = await _fileSessionRepository.GetByIdAsync(session.Id);
                    if (sessionDoc != null)
                    {
                        // 계정명 수집 (중복 제거)
                        if (!string.IsNullOrEmpty(sessionDoc.AccountName))
                        {
                            // 이미 병합된 세션인 경우 쉼표로 분할된 계정명들 처리
                            var accountNames = sessionDoc.AccountName.Split(',')
                                .Select(name => name.Trim())
                                .Where(name => !string.IsNullOrEmpty(name));

                            foreach (var accountName in accountNames)
                            {
                                if (!allAccountNames.Contains(accountName, StringComparer.OrdinalIgnoreCase))
                                {
                                    allAccountNames.Add(accountName);
                                }
                            }
                        }

                        // 파일 ID 수집
                        if (sessionDoc.FileIds != null)
                        {
                            allFileIds.AddRange(sessionDoc.FileIds);
                        }

                        // 금액 및 행수 합산
                        totalAmount += sessionDoc.TotalAmount;
                        totalRows += sessionDoc.TotalRows;
                    }
                }

                await progressCallback(50, "MongoDB 업데이트 중...");

                // 통합된 계정명 문자열 생성
                string mergedAccountName = string.Join(",", allAccountNames);

                // 통합된 계정명 컬럼 생성 (첫 번째 세션의 컬럼명 사용, 필요시 변경)
                string mergedAccountColumnName = targetSession.AccountColumnName;

                // 세션명 업데이트 (병합 정보 반영)
                string mergedSessionName = $"{targetSession.SessionName}_병합_{sessions.Count}개세션";

                // 대상 세션 업데이트
                await _fileSessionRepository.UpdateMergedSessionAsync(
                    targetSession.Id,
                    mergedSessionName,
                    mergedAccountName,
                    mergedAccountColumnName,
                    allFileIds.Distinct().ToList(),
                    totalAmount,
                    totalRows
                );

                await progressCallback(70, "기존 세션들 삭제 중...");

                // 삭제할 세션들 처리
                foreach (var sessionToDelete in sessionsToDelete)
                {
                    await _fileSessionRepository.DeleteAsync(sessionToDelete.Id);
                }

                await progressCallback(90, "UI 데이터 업데이트 중...");

                // 업데이트된 세션 정보로 UI 데이터 갱신
                targetSession.SessionName = mergedSessionName;
                targetSession.AccountColumnName = mergedAccountColumnName;
                //targetSession.AccountColumnName = mergedAccountName;
                targetSession.AccountName = mergedAccountName;
                targetSession.TotalAmount = totalAmount;
                targetSession.TotalAmountFormatted = totalAmount.ToString("N0") + " 원";
                targetSession.TotalRows = totalRows;
                targetSession.TotalRowsFormatted = totalRows.ToString("N0");
                targetSession.FileCount = allFileIds.Distinct().Count();

                return (true, targetSession, sessionsToDelete);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 병합 오류: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// 세션 그리드에서 세션들 제거
        /// </summary>
        private void RemoveSessionsFromGrid(List<SessionDisplayData> sessionsToRemove)
        {
            try
            {
                var sessionList = dgv_sessions.DataSource as List<SessionDisplayData>;
                if (sessionList != null)
                {
                    foreach (var session in sessionsToRemove)
                    {
                        sessionList.RemoveAll(s => s.Id == session.Id);
                    }
                    dgv_sessions.DataSource = sessionList.ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 그리드에서 제거 오류: {ex.Message}");
            }
        }


        /// <summary>
        /// 세션들의 호환성 검증
        /// </summary>
        private ValidationResult ValidateSessionsCompatibility(List<SessionDisplayData> sessions)
        {
            var referenceSession = sessions[0];
            string refAmountColumn = referenceSession.AmountColumnName.Trim().ToUpper();

            for (int i = 1; i < sessions.Count; i++)
            {
                var currentSession = sessions[i];
                string currAmountColumn = currentSession.AmountColumnName.Trim().ToUpper();

                // 금액 컬럼 일치 확인만 수행 (계정명 컬럼 검증 제거)
                if (refAmountColumn != currAmountColumn)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"금액 컬럼이 일치하지 않습니다.\n\n" +
                                     $"• {referenceSession.SessionName}: '{referenceSession.AmountColumnName}'\n" +
                                     $"• {currentSession.SessionName}: '{currentSession.AmountColumnName}'"
                    };
                }

                // TODO: 헤더 구조 검증 강화 (필요시 추가)
                // 각 세션의 파일들이 동일한 헤더 구조를 가지는지 확인
            }

            // 병합될 계정명들 표시
            var allAccountNames = sessions.Select(s => s.AccountColumnName).Distinct().ToList();
            if (allAccountNames.Count > 1)
            {
                string message = $"서로 다른 계정명 컬럼을 가진 세션들이 병합됩니다:\n\n" +
                                string.Join("\n", allAccountNames.Select((name, i) => $"• {name}")) +
                                "\n\n계속 진행하시겠습니까?";

                var result = MessageBox.Show(message, "계정명 컬럼 차이 확인",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "사용자가 서로 다른 계정명 컬럼 병합을 취소했습니다."
                    };
                }
            }

            return new ValidationResult { IsValid = true };
        }


        /// <summary>
        /// 선택된 세션 목록 반환
        /// </summary>
        private List<SessionDisplayData> GetSelectedSessions()
        {
            var selectedSessions = new List<SessionDisplayData>();
            var sessionList = dgv_sessions.DataSource as List<SessionDisplayData>;

            if (sessionList != null)
            {
                selectedSessions.AddRange(sessionList.Where(s => s.IsSelected));
            }

            return selectedSessions;
        }

        /// <summary>
        /// 파일 로드 화면으로 전환
        /// </summary>
        private async void NavigateToFileLoadScreen()
        {
            try
            {
                // 1. 먼저 화면 전환 (Handle 생성)
                if (this.ParentForm is Form1 form)
                {
                    form.LoadUserControl(userControlHandler.uc_fileLoad);
                }

                // 2. UI가 로드된 후 약간의 딜레이
                await Task.Delay(200);

                // 3. 페이징 컨트롤 초기화
                userControlHandler.uc_fileLoad.InitializePagingControls(true);

                // 4. Handle 생성 확인 후 fileload.cs의 함수들 호출
                if (userControlHandler.uc_fileLoad.IsHandleCreated)
                {
                    // *** fileload.cs의 함수들 직접 호출 ***
                    await userControlHandler.uc_fileLoad.LoadMongoPagedDataAsync();

                    // DataHandler.excelData가 설정되어 있는지 확인 후 호출
                    if (DataHandler.excelData != null && DataHandler.excelData.Columns.Count > 0)
                    {
                        //Debug.WriteLine($"DataColumnCollection: [{string.Join(", ", DataHandler.excelData.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}]");

                        await userControlHandler.uc_fileLoad.AddMongoColumnsToGrid(
                            userControlHandler.uc_fileLoad.dataGridView_delete_col,
                            DataHandler.excelData.Columns
                        );

                        //Debug.WriteLine($"DataColumnCollection after: [{string.Join(", ", DataHandler.excelData.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}]");

                        userControlHandler.uc_fileLoad.GetMongoColumnList(DataHandler.excelData.Columns);
                        userControlHandler.uc_fileLoad.SetupColumnLists();

                        //공급업체 표준화 로드
                        await userControlHandler.uc_fileLoad.InitializeStandardizationControls();


                    }
                    else
                    {
                        Debug.WriteLine("DataHandler.excelData가 설정되지 않았습니다.");
                    }
                }
                else
                {
                    // Handle이 없으면 동기적으로 생성 후 호출
                    userControlHandler.uc_fileLoad.CreateControl();
                    await Task.Delay(100);

                    await userControlHandler.uc_fileLoad.LoadMongoPagedDataAsync();

                    if (DataHandler.excelData != null && DataHandler.excelData.Columns.Count > 0)
                    {
                        await userControlHandler.uc_fileLoad.AddMongoColumnsToGrid(
                            userControlHandler.uc_fileLoad.dataGridView_delete_col,
                            DataHandler.excelData.Columns
                        );

                        userControlHandler.uc_fileLoad.GetMongoColumnList(DataHandler.excelData.Columns);
                        userControlHandler.uc_fileLoad.SetupColumnLists();

                        //공급업체 표준화 로드
                        await userControlHandler.uc_fileLoad.InitializeStandardizationControls();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"화면 전환 오류: {ex.Message}");
                MessageBox.Show($"화면 전환 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


    }

}
