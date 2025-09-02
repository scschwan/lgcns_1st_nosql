using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FinanceTool.MongoModels;
using FinanceTool.Repositories;
using MongoDB.Bson;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTool
{
    public partial class uc_MultiFileUpload
    {
        // uc_FileLoad.cs에 추가
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (this.Visible)
            {
                // 화면이 보여질 때만 레이아웃 재계산
                //RefreshLayouts();
                RefreshDataAndLayouts();

            }
        }

        /// <summary>
        /// 데이터 새로고침 및 레이아웃 재계산
        /// </summary>
        private async void RefreshDataAndLayouts()
        {
            try
            {
                // 1단계: 레이아웃 재계산 (기존 로직)
                RefreshLayouts();

                // 2단계: 데이터 새로고침
                await RefreshAllData();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"데이터 및 레이아웃 새로고침 오류: {ex.Message}");
            }
        }


        private void RefreshLayouts()
        {
            this.SuspendLayout();

            // TableLayoutPanel 재계산
            if (this.tableLayoutMain != null)
            {
                this.tableLayoutMain.SuspendLayout();
                this.tableLayoutMain.ResumeLayout(true);
                this.tableLayoutMain.PerformLayout();
            }

            this.ResumeLayout(true);
            this.PerformLayout();
        }

        /// <summary>
        /// 모든 데이터 새로고침 (파일 목록 + 세션 목록)
        /// </summary>
        private async Task RefreshAllData()
        {
            try
            {
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(20, "파일 목록 새로고침 중...");

                    // 파일 목록 새로고침
                    await RefreshFilesList();

                    await progressForm.UpdateProgressHandler(70, "세션 목록 새로고침 중...");

                    // 세션 목록 새로고침
                    await RefreshSessionsList();

                    await progressForm.UpdateProgressHandler(100, "새로고침 완료");
                    await Task.Delay(300);
                }

                Debug.WriteLine("데이터 새로고침 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"데이터 새로고침 오류: {ex.Message}");
                MessageBox.Show($"데이터 새로고침 중 오류가 발생했습니다.\n{ex.Message}",
                    "새로고침 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 파일 목록 새로고침
        /// </summary>
        private async Task RefreshFilesList()
        {
            try
            {
                // 현재 선택된 콤보박스 상태 백업
                var comboBoxStates = BackupComboBoxStates();

                // MongoDB에서 최신 파일 목록 조회
                var uploadedFiles = await _uploadedFileRepository.GetAllAsync();
                var displayDataList = new List<FileDisplayData>();

                foreach (var file in uploadedFiles)
                {
                    var displayData = CreateFileDisplayData(file);
                    displayDataList.Add(displayData);
                }

                // DataGridView 업데이트
                dgv_files.DataSource = displayDataList;

                // 콤보박스 아이템 재설정
                UpdateComboBoxItems();

                // 백업된 콤보박스 상태 복원
                await RestoreComboBoxStates(comboBoxStates);

                Debug.WriteLine($"파일 목록 새로고침 완료: {displayDataList.Count}개 파일");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파일 목록 새로고침 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 현재 콤보박스 상태 백업
        /// </summary>
        private Dictionary<ObjectId, (string AccountColumn, string AmountColumn)> BackupComboBoxStates()
        {
            var comboBoxStates = new Dictionary<ObjectId, (string AccountColumn, string AmountColumn)>();

            try
            {
                var currentList = dgv_files.DataSource as List<FileDisplayData>;
                if (currentList != null)
                {
                    foreach (var fileData in currentList)
                    {
                        comboBoxStates[fileData.Id] = (
                            fileData.AccountColumnName ?? "",
                            fileData.AmountColumnName ?? ""
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"콤보박스 상태 백업 오류: {ex.Message}");
            }

            return comboBoxStates;
        }

        /// <summary>
        /// 백업된 콤보박스 상태 복원
        /// </summary>
        private async Task RestoreComboBoxStates(Dictionary<ObjectId, (string AccountColumn, string AmountColumn)> comboBoxStates)
        {
            try
            {
                await Task.Delay(100); // UI 업데이트 대기

                foreach (DataGridViewRow row in dgv_files.Rows)
                {
                    var fileData = row.DataBoundItem as FileDisplayData;
                    if (fileData != null && comboBoxStates.ContainsKey(fileData.Id))
                    {
                        var (accountColumn, amountColumn) = comboBoxStates[fileData.Id];

                        // 콤보박스 값 복원
                        if (!string.IsNullOrEmpty(accountColumn))
                        {
                            SetComboBoxValueSafe(row, "AccountColumn", accountColumn);
                            fileData.AccountColumnName = accountColumn;
                        }
                        if (!string.IsNullOrEmpty(amountColumn))
                        {
                            SetComboBoxValueSafe(row, "AmountColumn", amountColumn);
                            fileData.AmountColumnName = amountColumn;
                        }
                    }
                }

                Debug.WriteLine("콤보박스 상태 복원 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"콤보박스 상태 복원 오류: {ex.Message}");
            }
        }

       
        /// <summary>
        /// 상태 표시 텍스트 변환
        /// </summary>
        private string GetStatusDisplay(string status)
        {
            return status switch
            {
                "processing" => "처리중",
                "completed" => "완료",
                "failed" => "실패",
                _ => status
            };
        }

        private UploadedFileRepository _uploadedFileRepository;
        private FileSessionRepository _fileSessionRepository;
        private const string UPLOAD_FOLDER = @"C:\Dmillions\excel_upload";



        /// <summary>
        /// 업로드 폴더 존재 확인 및 생성
        /// </summary>
        private void EnsureUploadFolderExists()
        {
            try
            {
                if (!Directory.Exists(UPLOAD_FOLDER))
                {
                    Directory.CreateDirectory(UPLOAD_FOLDER);
                    Debug.WriteLine($"업로드 폴더 생성: {UPLOAD_FOLDER}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"업로드 폴더 생성 오류: {ex.Message}");
                MessageBox.Show($"업로드 폴더 생성 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            try
            {
                // dgv_files 초기화
                InitializeFilesDataGridView();

                // dgv_sessions 초기화 추가
                InitializeSessionsDataGridView();

                // 기존 업로드된 파일들 로드
                LoadExistingFiles();

                // 기존 세션들 로드
                LoadExistingSessions();

                DataHandler.RegisterDataGridView(dgv_files);
                DataHandler.RegisterDataGridView(dgv_sessions);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UI 초기화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 파일 목록 DataGridView 초기화
        /// </summary>
        private void InitializeFilesDataGridView()
        {
            dgv_files.AutoGenerateColumns = false;
            dgv_files.AllowUserToAddRows = false;
            dgv_files.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv_files.MultiSelect = true;

            // *** 행 높이 고정 설정 추가 ***
            dgv_files.AllowUserToResizeRows = false;  // 사용자가 행 높이 조절 못하게
            dgv_files.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing; // 행 헤더 크기 조절 방지
            dgv_files.RowTemplate.Height = 25; // 기본 행 높이 설정 (픽셀)
            dgv_files.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None; // 자동 높이 조절 방지

            // *** 툴팁 기능 활성화 ***
            dgv_files.ShowCellToolTips = true;

            // 컬럼 정의
            dgv_files.Columns.Clear();

            // 체크박스 컬럼 추가
            var checkBoxColumn = new DataGridViewCheckBoxColumn
            {
                Name = "Check",
                HeaderText = "",
                DataPropertyName = "IsSelected",
                Width = 30,
                ThreeState = false,
                Frozen = true // 스크롤 시에도 고정
            };
            dgv_files.Columns.Add(checkBoxColumn);

            // 파일명 컬럼
            var fileNameColumn = new DataGridViewTextBoxColumn
            {
                Name = "FileName",
                HeaderText = "파일명",
                DataPropertyName = "OriginalFilename",
                Width = 200,
                ReadOnly = true
            };
            dgv_files.Columns.Add(fileNameColumn);



            // 총 행수 컬럼
            var rowCountColumn = new DataGridViewTextBoxColumn
            {
                Name = "TotalRows",
                HeaderText = "총 행수",
                //DataPropertyName = "TotalRows",
                DataPropertyName = "TotalRowsFormatted",
                Width = 80,
                ReadOnly = true
            };
            dgv_files.Columns.Add(rowCountColumn);



            // 계정명 컬럼 선택 (콤보박스)
            var accountColumnCombo = new DataGridViewComboBoxColumn
            {
                Name = "AccountColumn",
                HeaderText = "계정명 컬럼",
                DataPropertyName = "AccountColumnName",
                Width = 120,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
            };
            dgv_files.Columns.Add(accountColumnCombo);

            // 계정명 내용 컬럼 설정 개선
            var accountContentColumn = new DataGridViewTextBoxColumn
            {
                Name = "AccountContent",
                HeaderText = "계정명 내용",
                DataPropertyName = "AccountContentFormatted",
                Width = 200, // 폭 증가
                ReadOnly = true
            };
            dgv_files.Columns.Add(accountContentColumn);

            // 금액 컬럼 선택 (콤보박스)
            var amountColumnCombo = new DataGridViewComboBoxColumn
            {
                Name = "AmountColumn",
                HeaderText = "금액 컬럼",
                DataPropertyName = "AmountColumnName",
                Width = 120,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
            };
            dgv_files.Columns.Add(amountColumnCombo);

            // 합산 금액 컬럼
            var totalAmountColumn = new DataGridViewTextBoxColumn
            {
                Name = "TotalAmount",
                HeaderText = "합산 금액",
                DataPropertyName = "TotalAmountFormatted",
                Width = 120,
                ReadOnly = true
            };
            dgv_files.Columns.Add(totalAmountColumn);

            // 삭제 버튼 컬럼 추가
            var deleteButtonColumn = new DataGridViewButtonColumn
            {
                Name = "DeleteButton",
                HeaderText = "삭제",
                Text = "🗑️",
                UseColumnTextForButtonValue = true,
                Width = 60
            };
            dgv_files.Columns.Add(deleteButtonColumn);

            // 처리 상태 컬럼
            var statusColumn = new DataGridViewTextBoxColumn
            {
                Name = "ProcessingStatus",
                HeaderText = "상태",
                DataPropertyName = "ProcessingStatus",
                Width = 80,
                ReadOnly = true
            };
            dgv_files.Columns.Add(statusColumn);



            // 감지된 컬럼 목록
            var detectedColumnsColumn = new DataGridViewTextBoxColumn
            {
                Name = "DetectedColumns",
                HeaderText = "감지된 컬럼",
                DataPropertyName = "DetectedColumnsFormatted",
                Width = 250,
                ReadOnly = true
            };
            dgv_files.Columns.Add(detectedColumnsColumn);

            // 파일 크기 컬럼
            var fileSizeColumn = new DataGridViewTextBoxColumn
            {
                Name = "FileSize",
                HeaderText = "파일 크기",
                DataPropertyName = "FileSizeFormatted",
                Width = 100,
                ReadOnly = true
            };
            dgv_files.Columns.Add(fileSizeColumn);

            // 업로드 날짜 컬럼
            var uploadDateColumn = new DataGridViewTextBoxColumn
            {
                Name = "UploadDate",
                HeaderText = "업로드 날짜",
                DataPropertyName = "UploadDateFormatted",
                Width = 150,
                ReadOnly = true
            };
            dgv_files.Columns.Add(uploadDateColumn);

            // 이벤트 핸들러 등록
            dgv_files.CellValueChanged += Dgv_files_CellValueChanged;
            dgv_files.CurrentCellDirtyStateChanged += Dgv_files_CurrentCellDirtyStateChanged;
            dgv_files.CellContentClick += Dgv_files_CellContentClick; // 버튼 클릭용


            // *** 이 줄 추가 ***
            dgv_files.DataError += Dgv_files_DataError;

            // *** 새로 추가: 셀 툴팁 이벤트 ***
            dgv_files.CellToolTipTextNeeded += Dgv_files_CellToolTipTextNeeded;
        }


        /// <summary>
        /// 선택된 파일들의 체크 해제
        /// </summary>
        private void ClearFileSelections(List<FileDisplayData> selectedFiles)
        {
            foreach (var file in selectedFiles)
            {
                file.IsSelected = false;
            }
            dgv_files.Invalidate(); // 그리드 새로고침
        }

        


        /// <summary>
        /// 파티션된 세션들 생성 (병렬 처리 적용)
        /// </summary>
        private async Task<List<SessionDisplayData>> CreatePartitionedSessionsAsync(
            List<AccountPartition> partitions,
            List<FileDisplayData> originalFiles,
            ProcessProgressForm.UpdateProgressDelegate progressCallback)
        {
            var createdSessions = new List<SessionDisplayData>();
            var semaphore = new SemaphoreSlim(Math.Min(5, partitions.Count)); // 최대 5개 동시 처리

            try
            {
                int completedSessions = 0;
                var sessionTasks = partitions.Select(async (partition, index) =>
                {
                    await semaphore.WaitAsync();

                    try
                    {
                        // MongoDB에 세션 저장
                        var fileIds = partition.Files.Select(f => f.Id).ToList();
                        var sessionDocument = new FileSessionDocument
                        {
                            SessionName = partition.SessionName,
                            AccountColumnName = partition.Files.First().AccountColumnName,
                            AmountColumnName = partition.Files.First().AmountColumnName,
                            TotalAmount = partition.TotalAmount,
                            TotalRows = partition.TotalRows,
                            Status = "processing",
                            CreatedDate = DateTime.UtcNow,
                            FileIds = fileIds,
                            AccountName = partition.AccountName
                        };

                        await _fileSessionRepository.CreateAsync(sessionDocument);

                        // 파일들의 session_id 업데이트 (병렬 처리)
                        var updateTasks = partition.Files.Select(async file =>
                        {
                            await _uploadedFileRepository.UpdateSessionIdAsync(file.Id, sessionDocument.Id);
                            file.SessionId = sessionDocument.Id;
                        });

                        await Task.WhenAll(updateTasks);

                        // 진행률 업데이트
                        Interlocked.Increment(ref completedSessions);
                        int progress = 20 + (completedSessions * 60 / partitions.Count);
                        await progressCallback(progress, $"세션 생성 중... ({completedSessions}/{partitions.Count}) - {partition.AccountName}");

                        // 화면 표시용 데이터 생성
                        return new SessionDisplayData
                        {
                            Id = sessionDocument.Id,
                            SessionName = partition.SessionName,
                            AccountColumnName = partition.Files.First().AccountColumnName,
                            AmountColumnName = partition.Files.First().AmountColumnName,
                            AccountName = partition.AccountName, // *** AccountName 정보 포함 ***
                            TotalAmount = partition.TotalAmount,
                            TotalAmountFormatted = partition.TotalAmount.ToString("N0") + " 원",
                            TotalRows = partition.TotalRows,
                            TotalRowsFormatted = partition.TotalRows.ToString("N0"),
                            FileCount = partition.FileCount,
                            Status = "processing",
                            StatusDisplay = "처리중",
                            CreatedDate = DateTime.UtcNow,
                            CreatedDateFormatted = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"),
                            ResultFilePath = null
                        };
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                var results = await Task.WhenAll(sessionTasks);
                createdSessions.AddRange(results);

                return createdSessions;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파티션 세션 생성 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 파티션별 세션명 생성 (새로운 규칙 적용)
        /// </summary>
        private string GeneratePartitionSessionName(string accountName, List<FileDisplayData> files)
        {
            string dateStr = DateTime.Now.ToString("yyyy-MM-dd");

            if (files.Count == 1)
            {
                // 단일 파일: {계정명}_{파일명}_{생성일}
                string fileName = Path.GetFileNameWithoutExtension(files[0].OriginalFilename);
                return $"{accountName}_{fileName}_{dateStr}";
            }
            else
            {
                // 다중 파일: {계정명}_{파일수}개파일_{생성일}
                return $"{accountName}_{files.Count}개파일_{dateStr}";
            }
        }

        /// <summary>
        /// 컬럼 인덱스 찾기 헬퍼 메서드
        /// </summary>
        private int FindColumnIndex(List<Cell> headerCells, string columnName, WorkbookPart workbookPart)
        {
            for (int i = 0; i < headerCells.Count; i++)
            {
                string cellValue = GetCellValue(headerCells[i], workbookPart);
                if (cellValue.Trim().Equals(columnName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }


        /// <summary>
        /// 파일과 연관된 세션 의존성 확인
        /// </summary>
        private async Task<List<SessionDisplayData>> CheckFileSessionDependency(ObjectId fileId)
        {
            var relatedSessions = new List<SessionDisplayData>();

            try
            {
                var sessionList = dgv_sessions.DataSource as List<SessionDisplayData>;
                if (sessionList == null) return relatedSessions;

                foreach (var sessionDisplay in sessionList)
                {
                    // MongoDB에서 실제 세션 정보 조회
                    var sessionDoc = await _fileSessionRepository.GetByIdAsync(sessionDisplay.Id);
                    if (sessionDoc?.FileIds != null && sessionDoc.FileIds.Contains(fileId))
                    {
                        relatedSessions.Add(sessionDisplay);
                    }
                }

                return relatedSessions;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 의존성 확인 오류: {ex.Message}");
                return relatedSessions;
            }
        }

        /// <summary>
        /// 세션 의존성 경고 다이얼로그 표시
        /// </summary>
        private void ShowSessionDependencyWarning(string fileName, List<SessionDisplayData> relatedSessions)
        {
            string sessionList = string.Join("\n", relatedSessions.Select((s, i) => $"{i + 1}. {s.SessionName}"));

            string message = $"'{fileName}' 파일을 삭제할 수 없습니다.\n\n" +
                            "해당 파일이 다음 세션에서 사용 중입니다:\n\n" +
                            sessionList + "\n\n" +
                            "파일을 삭제하려면 먼저 연관된 세션들을 삭제해주세요.";

            MessageBox.Show(message, "파일 삭제 불가",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// 파일 삭제 후 그리드 업데이트 (콤보박스 상태 보존)
        /// </summary>
        private async Task UpdateFileGridAfterDeletion(int deletedRowIndex)
        {
            try
            {
                // 삭제 전 다른 파일들의 콤보박스 상태 백업
                var currentList = (dgv_files.DataSource as List<FileDisplayData>) ?? new List<FileDisplayData>();
                var comboBoxStates = new Dictionary<ObjectId, (string AccountColumn, string AmountColumn)>();

                for (int i = 0; i < currentList.Count; i++)
                {
                    if (i != deletedRowIndex) // 삭제될 행 제외
                    {
                        var fileData = currentList[i];
                        comboBoxStates[fileData.Id] = (
                            fileData.AccountColumnName ?? "",
                            fileData.AmountColumnName ?? ""
                        );
                    }
                }

                // 삭제된 항목 제거
                currentList.RemoveAt(deletedRowIndex);

                // DataSource 재설정
                dgv_files.DataSource = currentList.ToList();

                // 콤보박스 아이템 재설정
                UpdateComboBoxItems();

                // 백업된 상태 복원
                await Task.Delay(100); // UI 업데이트 대기

                foreach (DataGridViewRow row in dgv_files.Rows)
                {
                    var fileData = row.DataBoundItem as FileDisplayData;
                    if (fileData != null && comboBoxStates.ContainsKey(fileData.Id))
                    {
                        var (accountColumn, amountColumn) = comboBoxStates[fileData.Id];

                        // 콤보박스 값 복원
                        if (!string.IsNullOrEmpty(accountColumn))
                        {
                            SetComboBoxValueSafe(row, "AccountColumn", accountColumn);
                        }
                        if (!string.IsNullOrEmpty(amountColumn))
                        {
                            SetComboBoxValueSafe(row, "AmountColumn", amountColumn);
                        }
                    }
                }

                Debug.WriteLine($"파일 삭제 후 그리드 업데이트 완료. 남은 파일: {currentList.Count}개");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파일 삭제 후 그리드 업데이트 오류: {ex.Message}");
                // 오류 발생 시 전체 새로고침
                var currentList = (dgv_files.DataSource as List<FileDisplayData>) ?? new List<FileDisplayData>();
                dgv_files.DataSource = currentList.ToList();
                UpdateComboBoxItems();
            }
        }

        /// <summary>
        /// 안전한 콤보박스 값 설정 (오류 방지)
        /// </summary>
        private void SetComboBoxValueSafe(DataGridViewRow row, string columnName, string value)
        {
            try
            {
                var cell = row.Cells[columnName];
                if (cell is DataGridViewComboBoxCell comboCell && !string.IsNullOrEmpty(value))
                {
                    // 콤보박스에 해당 값이 있는지 확인 후 설정
                    if (comboCell.Items.Contains(value))
                    {
                        cell.Value = value;
                        Debug.WriteLine($"콤보박스 값 복원: {columnName} = {value}");
                    }
                    else
                    {
                        Debug.WriteLine($"콤보박스에 값 없음: {columnName} = {value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"콤보박스 값 설정 오류 [{columnName}]: {ex.Message}");
            }
        }


        /// <summary>
        /// 선택된 파일 목록 반환
        /// </summary>
        private List<FileDisplayData> GetSelectedFiles()
        {
            var selectedFiles = new List<FileDisplayData>();
            var fileList = dgv_files.DataSource as List<FileDisplayData>;

            if (fileList != null)
            {
                selectedFiles.AddRange(fileList.Where(f => f.IsSelected));
            }

            return selectedFiles;
        }

        /// <summary>
        /// 특정 셀의 콤보박스 값 설정
        /// </summary>
        private void SetComboBoxValue(FileDisplayData fileData, string columnName, string value)
        {
            try
            {
                var fileList = dgv_files.DataSource as List<FileDisplayData>;
                if (fileList != null)
                {
                    var rowIndex = fileList.FindIndex(f => f.Id == fileData.Id);
                    if (rowIndex >= 0)
                    {
                        var cell = dgv_files.Rows[rowIndex].Cells[columnName];
                        if (cell is DataGridViewComboBoxCell comboCell && !string.IsNullOrEmpty(value))
                        {
                            // 콤보박스에 해당 값이 있는지 확인 후 설정
                            if (comboCell.Items.Contains(value))
                            {
                                cell.Value = value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"콤보박스 값 설정 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 특정 파일의 그리드 행만 새로고침 (콤보박스 제외)
        /// </summary>
        private void RefreshFileGridRowSpecific(FileDisplayData fileData)
        {
            try
            {
                var fileList = dgv_files.DataSource as List<FileDisplayData>;
                if (fileList != null)
                {
                    var index = fileList.FindIndex(f => f.Id == fileData.Id);
                    if (index >= 0)
                    {
                        // 컬럼명으로 인덱스 찾아서 새로고침
                        int accountContentColumnIndex = dgv_files.Columns["AccountContent"].Index;
                        int totalAmountColumnIndex = dgv_files.Columns["TotalAmount"].Index;

                        dgv_files.InvalidateCell(accountContentColumnIndex, index);
                        dgv_files.InvalidateCell(totalAmountColumnIndex, index);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"특정 그리드 행 새로고침 오류: {ex.Message}");
            }
        }

        
        /// <summary>
        /// 콤보박스 아이템 업데이트
        /// </summary>
        private void UpdateComboBoxItems()
        {
            try
            {
                foreach (DataGridViewRow row in dgv_files.Rows)
                {
                    var fileData = row.DataBoundItem as FileDisplayData;
                    if (fileData?.DetectedColumns != null)
                    {
                        // 계정명 컬럼 콤보박스
                        var accountCombo = row.Cells["AccountColumn"] as DataGridViewComboBoxCell;
                        if (accountCombo != null)
                        {
                            accountCombo.Items.Clear();
                            accountCombo.Items.Add(""); // 빈 선택
                            foreach (string column in fileData.DetectedColumns)
                            {
                                accountCombo.Items.Add(column);
                            }
                        }

                        // 금액 컬럼 콤보박스
                        var amountCombo = row.Cells["AmountColumn"] as DataGridViewComboBoxCell;
                        if (amountCombo != null)
                        {
                            amountCombo.Items.Clear();
                            amountCombo.Items.Add(""); // 빈 선택
                            foreach (string column in fileData.DetectedColumns)
                            {
                                amountCombo.Items.Add(column);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"콤보박스 아이템 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// FileDisplayData 객체 생성
        /// </summary>
        private FileDisplayData CreateFileDisplayData(UploadedFileDocument file)
        {
            return new FileDisplayData
            {
                Id = file.Id,
                OriginalFilename = file.OriginalFilename,
                StoredFilename = file.StoredFilename,
                FilePath = file.FilePath,
                FileSize = file.FileSize,
                FileSizeFormatted = FormatFileSize(file.FileSize),
                UploadDate = file.UploadDate,
                UploadDateFormatted = file.UploadDate.ToString("yyyy-MM-dd HH:mm"),
                DetectedColumns = file.DetectedColumns,
                DetectedColumnsFormatted = string.Join(", ", file.DetectedColumns),
                TotalRows = file.TotalRows,
                TotalRowsFormatted = file.TotalRows > 0 ? file.TotalRows.ToString("N0") : "0",
                AccountColumnName = file.AccountColumnName,
                AmountColumnName = file.AmountColumnName,

                // *** MongoDB에서 저장된 정보 활용 ***
                AccountContents = file.AccountContents ?? new List<string>(),
                AccountContentFormatted = file.AccountContents?.Count > 0 ?
            string.Join(", ", file.AccountContents.Take(3)) +
            (file.AccountContents.Count > 3 ? "..." : "") : "",
                TotalAmount = file.TotalAmount,
                TotalAmountFormatted = file.TotalAmount > 0 ? file.TotalAmount.ToString("N0") + " 원" : "",
                SessionId = file.SessionId,
                ProcessingStatus = file.ProcessingStatus
            };
        }

        /// <summary>
        /// DataGridView 바인딩용 파일 표시 데이터 클래스
        /// </summary>
        public class FileDisplayData
        {
            public ObjectId Id { get; set; }

            public bool IsSelected { get; set; } = false; // 체크박스용 속성 추가
            public string OriginalFilename { get; set; }
            public string StoredFilename { get; set; }
            public string FilePath { get; set; }
            public long FileSize { get; set; }
            public string FileSizeFormatted { get; set; }
            public DateTime UploadDate { get; set; }
            public string UploadDateFormatted { get; set; }
            public List<string> DetectedColumns { get; set; } = new List<string>();
            public string DetectedColumnsFormatted { get; set; }
            public decimal TotalRows { get; set; }
            public string TotalRowsFormatted { get; set; }
            public string AccountColumnName { get; set; }
            public string AmountColumnName { get; set; }

            // *** 새로 추가: 계정명 내용 관련 속성들 ***
            public List<string> AccountContents { get; set; } = new List<string>(); // 중복 제거된 계정명 내용들
            public string AccountContentFormatted { get; set; } // 화면 표시용 (최대 5개까지)


            public decimal TotalAmount { get; set; }
            public string TotalAmountFormatted { get; set; }
            public ObjectId? SessionId { get; set; }
            public string ProcessingStatus { get; set; }
        }

       
    }
}
