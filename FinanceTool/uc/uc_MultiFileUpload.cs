using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using FinanceTool.MongoModels;
using FinanceTool.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
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

        // uc_FileLoad.cs에 추가
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (this.Visible)
            {
                // 화면이 보여질 때만 레이아웃 재계산
                RefreshLayouts();
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
        /// 계정명별 파티션 분석 (병렬 처리 적용)
        /// </summary>
        private async Task<PartitionAnalysisResult> AnalyzeAccountPartitionsAsync(List<FileDisplayData> selectedFiles, ProcessProgressForm.UpdateProgressDelegate progressCallback)
        {
            try
            {
                await progressCallback(5, "파일 그룹화 분석 중...");

                var partitions = new List<AccountPartition>();
                var accountGroups = new Dictionary<string, List<FileDisplayData>>();

                // 파일들을 계정명별로 그룹화
                foreach (var file in selectedFiles)
                {
                    foreach (var accountName in file.AccountContents)
                    {
                        if (!accountGroups.ContainsKey(accountName))
                        {
                            accountGroups[accountName] = new List<FileDisplayData>();
                        }
                        accountGroups[accountName].Add(file);
                    }
                }

                await progressCallback(15, "계정별 데이터 계산 중...");

                // 병렬 처리로 각 계정명별 파티션 생성
                var semaphore = new SemaphoreSlim(Environment.ProcessorCount * 2); // CPU 코어수의 2배로 제한
                var partitionTasks = new List<Task<AccountPartition>>();

                int totalGroups = accountGroups.Count;
                int completedGroups = 0;

                foreach (var accountGroup in accountGroups)
                {
                    var task = ProcessAccountGroupAsync(accountGroup.Key, accountGroup.Value, semaphore, async (progress) =>
                    {
                        Interlocked.Increment(ref completedGroups);
                        int overallProgress = 15 + (completedGroups * 60 / totalGroups);
                        await progressCallback(overallProgress, $"계정별 데이터 계산 중... ({completedGroups}/{totalGroups}) - {accountGroup.Key}");
                    });

                    partitionTasks.Add(task);
                }

                // 모든 파티션 처리 완료 대기
                var results = await Task.WhenAll(partitionTasks);
                partitions.AddRange(results.Where(r => r != null));

                await progressCallback(80, "파티션 검증 중...");

                // 파티션 검증
                if (partitions.Count == 0)
                {
                    return new PartitionAnalysisResult
                    {
                        IsValid = false,
                        ErrorMessage = "계정명을 기준으로 파티션을 생성할 수 없습니다.\n선택된 파일들의 계정명 정보를 확인해주세요."
                    };
                }

                await progressCallback(90, "파티션 분석 완료");

                return new PartitionAnalysisResult
                {
                    IsValid = true,
                    Partitions = partitions
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파티션 분석 오류: {ex.Message}");
                return new PartitionAnalysisResult
                {
                    IsValid = false,
                    ErrorMessage = $"파티션 분석 중 오류가 발생했습니다: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 개별 계정 그룹 처리 (병렬 처리용)
        /// </summary>
        private async Task<AccountPartition> ProcessAccountGroupAsync(
            string accountName,
            List<FileDisplayData> files,
            SemaphoreSlim semaphore,
            Func<int, Task> progressCallback)
        {
            await semaphore.WaitAsync();

            try
            {
                var distinctFiles = files.Distinct().ToList();

                var partition = new AccountPartition
                {
                    AccountName = accountName,
                    Files = distinctFiles,
                    FileCount = distinctFiles.Count,
                    SessionName = GeneratePartitionSessionName(accountName, distinctFiles),
                    TotalRows = 0,
                    TotalAmount = 0
                };

                // 병렬로 각 파일의 데이터 계산
                var fileTasks = distinctFiles.Select(async file =>
                {
                    return await Task.Run(() => CalculateAccountSpecificData(file, accountName));
                });

                var fileResults = await Task.WhenAll(fileTasks);

                // 결과 합산
                foreach (var result in fileResults)
                {
                    partition.TotalRows += result.RowCount;
                    partition.TotalAmount += result.Amount;
                }

                await progressCallback(0); // 진행률 업데이트 호출

                return partition;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"계정 그룹 처리 오류 [{accountName}]: {ex.Message}");
                return null;
            }
            finally
            {
                semaphore.Release();
            }
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
        /// 특정 계정명에 대한 파일 데이터 계산
        /// </summary>
        private (int RowCount, decimal Amount) CalculateAccountSpecificData(FileDisplayData file, string targetAccountName)
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
                    if (allRows.Count <= 1) return (0, 0);

                    // 헤더에서 컬럼 인덱스 찾기
                    var headerRow = allRows.First();
                    var headerCells = headerRow.Elements<Cell>().ToList();

                    int accountColumnIndex = FindColumnIndex(headerCells, file.AccountColumnName, workbookPart);
                    int amountColumnIndex = FindColumnIndex(headerCells, file.AmountColumnName, workbookPart);

                    if (accountColumnIndex == -1 || amountColumnIndex == -1) return (0, 0);

                    // 해당 계정명에 해당하는 행들만 처리
                    for (int rowIndex = 1; rowIndex < allRows.Count; rowIndex++)
                    {
                        var row = allRows[rowIndex];
                        var cells = row.Elements<Cell>().ToList();

                        if (accountColumnIndex < cells.Count && amountColumnIndex < cells.Count)
                        {
                            string accountValue = GetCellValue(cells[accountColumnIndex], workbookPart);

                            // 해당 계정명과 일치하는 행만 계산
                            if (accountValue.Trim().Equals(targetAccountName.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                string amountValue = GetCellValue(cells[amountColumnIndex], workbookPart);
                                if (decimal.TryParse(amountValue.Replace(",", ""), out decimal amount))
                                {
                                    totalAmount += amount;
                                }
                                rowCount++;
                            }
                        }
                    }
                }

                return (rowCount, totalAmount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"계정별 데이터 계산 오류: {ex.Message}");
                return (0, 0);
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
        /// 계정명 컬럼 정보만 업데이트
        /// </summary>
        private async Task UpdateAccountColumnInfo(FileDisplayData fileData)
        {
            try
            {
                using(var progressForm = new ProcessProgressForm())
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
        /// 특정 파일의 그리드 행만 새로고침
        /// </summary>
        private void RefreshFileGridRow(FileDisplayData fileData)
        {
            try
            {
                var fileList = dgv_files.DataSource as List<FileDisplayData>;
                if (fileList != null)
                {
                    var index = fileList.FindIndex(f => f.Id == fileData.Id);
                    if (index >= 0)
                    {
                        // 특정 행만 무효화해서 다시 그리기
                        dgv_files.InvalidateRow(index);

                        // 콤보박스 아이템도 다시 설정
                        UpdateComboBoxItems();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"그리드 행 새로고침 오류: {ex.Message}");
                // 전체 그리드 새로고침으로 폴백
                dgv_files.Invalidate();
            }
        }

        /// <summary>
        /// 계정명 컬럼 내용 검증 및 추출
        /// </summary>
        private async Task<ValidationResult> ValidateAndExtractAccountContent(FileDisplayData fileData)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string filePath = Path.Combine(UPLOAD_FOLDER, fileData.StoredFilename);
                    var accountContents = new HashSet<string>();

                    using (var document = SpreadsheetDocument.Open(filePath, false))
                    {
                        var workbookPart = document.WorkbookPart;
                        var worksheet = workbookPart.WorksheetParts.First().Worksheet;
                        var sheetData = worksheet.GetFirstChild<SheetData>();

                        var allRows = sheetData.Elements<Row>().ToList();
                        if (allRows.Count <= 1)
                        {
                            // 빈 데이터도 UI에 반영
                            Application.OpenForms[0].Invoke((MethodInvoker)delegate
                            {
                                fileData.AccountContents = new List<string>();
                                fileData.AccountContentFormatted = "";
                            });
                            return new ValidationResult { IsValid = true };
                        }

                        // 헤더에서 계정명 컬럼 인덱스 찾기
                        var headerRow = allRows.First();
                        var headerCells = headerRow.Elements<Cell>().ToList();
                        int accountColumnIndex = -1;

                        for (int i = 0; i < headerCells.Count; i++)
                        {
                            string cellValue = GetCellValue(headerCells[i], workbookPart);
                            if (cellValue.Trim().Equals(fileData.AccountColumnName.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                accountColumnIndex = i;
                                break;
                            }
                        }

                        if (accountColumnIndex == -1)
                        {
                            return new ValidationResult
                            {
                                IsValid = false,
                                ErrorMessage = $"계정명 컬럼 '{fileData.AccountColumnName}'을 찾을 수 없습니다."
                            };
                        }

                        // 데이터 행들에서 계정명 내용 추출
                        for (int rowIndex = 1; rowIndex < allRows.Count; rowIndex++)
                        {
                            var row = allRows[rowIndex];
                            var cells = row.Elements<Cell>().ToList();

                            if (accountColumnIndex < cells.Count)
                            {
                                string cellValue = GetCellValue(cells[accountColumnIndex], workbookPart);
                                if (!string.IsNullOrWhiteSpace(cellValue))
                                {
                                    accountContents.Add(cellValue.Trim());
                                }
                            }
                        }
                    }

                    var uniqueContents = accountContents.ToList();

                    // UI 스레드에서 데이터 업데이트
                    Application.OpenForms[0].Invoke((MethodInvoker)delegate
                    {
                        fileData.AccountContents = uniqueContents;
                        // 계정명 내용 포맷팅 개선 (최대 3개 표시 + 개수 정보)
                        if (uniqueContents.Count == 0)
                        {
                            fileData.AccountContentFormatted = "";
                        }
                        else if (uniqueContents.Count == 1)
                        {
                            fileData.AccountContentFormatted = uniqueContents[0];
                        }
                        else if (uniqueContents.Count <= 3)
                        {
                            fileData.AccountContentFormatted = string.Join(", ", uniqueContents) + $" ({uniqueContents.Count}개)";
                        }
                        else
                        {
                            fileData.AccountContentFormatted = string.Join(", ", uniqueContents.Take(3)) + $"... ({uniqueContents.Count}개)";
                        }
                    });

                    // *** 핵심 변경: 40개 초과 시 경고하되 처리는 허용 ***
                    if (uniqueContents.Count > 40)
                    {
                        string warningMessage = $"계정명 종류가 40개를 초과했습니다. ({uniqueContents.Count}개)\n" +
                                              "성능상 권장하지 않지만 처리를 계속합니다.\n" +
                                              "처음 5개 계정명:\n" +
                                              string.Join("\n", uniqueContents.Take(5).Select((v, i) => $"{i + 1}. {v}"));

                        // 비동기로 경고 표시 (처리는 계속)
                        Application.OpenForms[0].BeginInvoke((MethodInvoker)delegate
                        {
                            //MessageBox.Show(warningMessage, "계정명 개수 경고",  MessageBoxButtons.OK, MessageBoxIcon.Warning);

                            Form progressForm = Application.OpenForms.Cast<Form>()
                            .FirstOrDefault(f => f.GetType().Name == "ProcessProgressForm");

                            if (progressForm != null)
                            {
                                progressForm.Hide();
                            }

                            MessageBox.Show(warningMessage, "계정명 개수 경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                            if (progressForm != null)
                            {
                                progressForm.Show();
                            }
                        });
                    }
                    // 1~40개는 정상 처리
                    else if (uniqueContents.Count > 1)
                    {
                        string infoMessage = $"계정명이 {uniqueContents.Count}개 감지되었습니다.\n" +
                                           "세션 생성 시 계정명별로 분리됩니다.\n" +
                                           "감지된 계정명:\n" +
                                           string.Join("\n", uniqueContents.Take(10).Select((v, i) => $"{i + 1}. {v}")) +
                                           (uniqueContents.Count > 10 ? $"\n... 외 {uniqueContents.Count - 10}개 더" : "");

                        // 정보성 메시지 (선택사항)
                        Application.OpenForms[0].BeginInvoke((MethodInvoker)delegate
                        {
                            /*
                            var result = MessageBox.Show(infoMessage + "\n계속 진행하시겠습니까?",
                                "계정명 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                            if (result != DialogResult.Yes)
                            {
                                // 사용자가 취소한 경우 계정명 컬럼 선택 해제
                                fileData.AccountColumnName = "";
                                fileData.AccountContents.Clear();
                                fileData.AccountContentFormatted = "";
                            }
                            */

                            // progressForm이 있다면 일시적으로 숨기기
                            Form progressForm = Application.OpenForms.Cast<Form>()
                                .FirstOrDefault(f => f.GetType().Name == "ProcessProgressForm");

                            if (progressForm != null)
                            {
                                progressForm.Hide();
                            }

                            try
                            {
                                var result = MessageBox.Show(infoMessage + "\n계속 진행하시겠습니까?",
                                    "계정명 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                                if (result != DialogResult.Yes)
                                {
                                    fileData.AccountColumnName = "";
                                    fileData.AccountContents.Clear();
                                    fileData.AccountContentFormatted = "";
                                }
                            }
                            finally
                            {
                                // progressForm 다시 표시
                                /*
                                if (progressForm != null)
                                {
                                    progressForm.Show();
                                }
                                */
                            }
                        });
                    }

                    return new ValidationResult { IsValid = true };
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"계정명 내용 검증 오류: {ex.Message}");
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"계정명 컬럼 검증 중 오류가 발생했습니다: {ex.Message}"
                    };
                }
            });
        }

        /// <summary>
        /// 금액 컬럼 검증 및 합계 계산
        /// </summary>
        private async Task<ValidationResult> ValidateAndCalculateAmount(FileDisplayData fileData)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string filePath = Path.Combine(UPLOAD_FOLDER, fileData.StoredFilename);
                    decimal totalAmount = 0;
                    var nonNumericValues = new List<string>();

                    using (var document = SpreadsheetDocument.Open(filePath, false))
                    {
                        var workbookPart = document.WorkbookPart;
                        var worksheet = workbookPart.WorksheetParts.First().Worksheet;
                        var sheetData = worksheet.GetFirstChild<SheetData>();

                        var allRows = sheetData.Elements<Row>().ToList();
                        if (allRows.Count <= 1)
                        {
                            // *** UI 스레드에서 데이터 업데이트 ***
                            Application.OpenForms[0].Invoke((MethodInvoker)delegate
                            {
                                fileData.TotalAmount = 0;
                                fileData.TotalAmountFormatted = "0 원";
                            });
                            return new ValidationResult { IsValid = true };
                        }

                        // 헤더에서 금액 컬럼 인덱스 찾기
                        var headerRow = allRows.First();
                        var headerCells = headerRow.Elements<Cell>().ToList();
                        int amountColumnIndex = -1;

                        for (int i = 0; i < headerCells.Count; i++)
                        {
                            string cellValue = GetCellValue(headerCells[i], workbookPart);
                            if (cellValue.Trim().Equals(fileData.AmountColumnName.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                amountColumnIndex = i;
                                break;
                            }
                        }

                        if (amountColumnIndex == -1)
                        {
                            return new ValidationResult
                            {
                                IsValid = false,
                                ErrorMessage = $"금액 컬럼 '{fileData.AmountColumnName}'을 찾을 수 없습니다."
                            };
                        }

                        // 데이터 행들에서 금액 검증 및 합계 계산
                        for (int rowIndex = 1; rowIndex < allRows.Count; rowIndex++)
                        {
                            var row = allRows[rowIndex];
                            var cells = row.Elements<Cell>().ToList();

                            if (amountColumnIndex < cells.Count)
                            {
                                string cellValue = GetCellValue(cells[amountColumnIndex], workbookPart);
                                if (!string.IsNullOrWhiteSpace(cellValue))
                                {
                                    string cleanValue = cellValue.Replace(",", "").Trim();
                                    if (decimal.TryParse(cleanValue, out decimal amount))
                                    {
                                        totalAmount += amount;
                                    }
                                    else
                                    {
                                        if (nonNumericValues.Count < 10)
                                        {
                                            nonNumericValues.Add(cellValue);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // *** UI 스레드에서 데이터 업데이트 ***
                    Application.OpenForms[0].Invoke((MethodInvoker)delegate
                    {
                        if (nonNumericValues.Count == 0)
                        {
                            fileData.TotalAmount = totalAmount;
                            fileData.TotalAmountFormatted = totalAmount.ToString("N0") + " 원";

                        }

                    });

                    // 숫자가 아닌 값이 있는 경우 오류
                    if (nonNumericValues.Count > 0)
                    {
                        string errorMessage = "금액 컬럼은 숫자 값만 존재해야 합니다.\n\n" +
                                            "숫자가 아닌 값들:\n" +
                                            string.Join("\n", nonNumericValues.Select((v, i) => $"{i + 1}. '{v}'"));

                        return new ValidationResult
                        {
                            IsValid = false,
                            ErrorMessage = errorMessage
                        };
                    }

                    return new ValidationResult { IsValid = true };
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"금액 검증 오류: {ex.Message}");
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"금액 컬럼 검증 중 오류가 발생했습니다: {ex.Message}"
                    };
                }
            });
        }



        /// <summary>
        /// 엑셀 파일에서 지정된 컬럼의 합계 계산
        /// </summary>
        private async Task<decimal> CalculateTotalAmountFromExcel(string storedFilename, string amountColumnName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string filePath = Path.Combine(UPLOAD_FOLDER, storedFilename);
                    decimal total = 0;

                    using (var document = SpreadsheetDocument.Open(filePath, false))
                    {
                        var workbookPart = document.WorkbookPart;
                        var worksheet = workbookPart.WorksheetParts.First().Worksheet;
                        var sheetData = worksheet.GetFirstChild<SheetData>();

                        // 첫 번째 행에서 컬럼 인덱스 찾기
                        var headerRow = sheetData.Elements<Row>().FirstOrDefault();
                        if (headerRow == null) return 0;

                        int amountColumnIndex = -1;
                        var headerCells = headerRow.Elements<Cell>().ToList();

                        for (int i = 0; i < headerCells.Count; i++)
                        {
                            string cellValue = GetCellValue(headerCells[i], workbookPart);
                            if (cellValue.Trim().Equals(amountColumnName.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                amountColumnIndex = i;
                                break;
                            }
                        }

                        if (amountColumnIndex == -1) return 0;

                        // 데이터 행들에서 금액 합계 계산
                        foreach (var row in sheetData.Elements<Row>().Skip(1)) // 헤더 제외
                        {
                            var cells = row.Elements<Cell>().ToList();
                            if (amountColumnIndex < cells.Count)
                            {
                                string cellValue = GetCellValue(cells[amountColumnIndex], workbookPart);
                                if (decimal.TryParse(cellValue.Replace(",", ""), out decimal amount))
                                {
                                    total += amount;
                                }
                            }
                        }
                    }

                    return total;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"엑셀 금액 합계 계산 오류: {ex.Message}");
                    return 0;
                }
            });
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
        /// 엑셀 파일 정보 추출 (컬럼명, 행 수)
        /// </summary>
        private async Task<(List<string> columns, int rows)> ExtractExcelInfo(string filePath)
        {
            return await Task.Run(() =>
            {
                var columns = new List<string>();
                int rows = 0;

                try
                {
                    using (var document = SpreadsheetDocument.Open(filePath, false))
                    {
                        var workbookPart = document.WorkbookPart;
                        var worksheet = workbookPart.WorksheetParts.First().Worksheet;
                        var sheetData = worksheet.GetFirstChild<SheetData>();

                        var allRows = sheetData.Elements<Row>().ToList();
                        //rows = allRows.Count;
                        int actualDataRows = 0;

                        foreach (var row in allRows)
                        {
                            bool hasData = false;
                            foreach (var cell in row.Elements<Cell>())
                            {
                                string cellValue = GetCellValue(cell, workbookPart);
                                if (!string.IsNullOrWhiteSpace(cellValue))
                                {
                                    hasData = true;
                                    break;
                                }
                            }
                            if (hasData) actualDataRows++;
                        }

                        rows = actualDataRows;

                        // 첫 번째 행에서 컬럼명 추출
                        if (allRows.Count > 0)
                        {
                            var headerRow = allRows.First();
                            foreach (var cell in headerRow.Elements<Cell>())
                            {
                                string columnName = GetCellValue(cell, workbookPart);
                                if (!string.IsNullOrWhiteSpace(columnName))
                                {
                                    columns.Add(columnName.Trim());
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"엑셀 정보 추출 오류: {ex.Message}");
                    //string exMsg = $"엑셀 정보 추출 오류: {ex.Message}";
                    columns.Add($"엑셀 정보 추출 오류: {ex.Message}");
                    return (columns, -1); // 헤더 제외한 데이터 행 수
                }

                return (columns, Math.Max(0, rows - 1)); // 헤더 제외한 데이터 행 수
            });
        }

        /// <summary>
        /// 엑셀 셀 값 읽기
        /// </summary>
        private string GetCellValue(Cell cell, WorkbookPart workbookPart)
        {
            if (cell?.CellValue == null) return string.Empty;

            string value = cell.CellValue.InnerXml;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                var stringTablePart = workbookPart.SharedStringTablePart;
                if (stringTablePart != null)
                {
                    value = stringTablePart.SharedStringTable.ElementAt(int.Parse(value)).InnerText;
                }
            }

            return value ?? string.Empty;
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
        /// 파일 크기 포맷팅
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
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


        //////////////////////////////////세션 테이블 관련 함수////////////////////////////////////////////////////////////////////////
        ///
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
        /// 세션명 유효성 검사
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션명 유효성 검사 오류: {ex.Message}");
            }
        }

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

                if (columnName == "DownloadButton")
                {
                    var sessionData = dgv_sessions.Rows[e.RowIndex].DataBoundItem as SessionDisplayData;
                    if (sessionData != null)
                    {
                        var cell = dgv_sessions.Rows[e.RowIndex].Cells[e.ColumnIndex];

                        // result_file_path가 없으면 비활성화
                        if (string.IsNullOrEmpty(sessionData.ResultFilePath))
                        {
                            cell.Style.ForeColor = System.Drawing.Color.Gray;
                            cell.Style.BackColor = System.Drawing.Color.LightGray;
                            cell.ToolTipText = "다운로드할 결과 파일이 없습니다.";
                        }
                        else
                        {
                            cell.Style.ForeColor = System.Drawing.Color.Black;
                            cell.Style.BackColor = System.Drawing.Color.White;
                            cell.ToolTipText = "결과 파일 다운로드";
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
                    await DownloadSessionResult(sessionData);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 그리드 클릭 처리 오류: {ex.Message}");
            }
        }


        /// <summary>
        /// 세션 삭제 처리
        /// </summary>
        /// <summary>
        /// 세션 삭제 처리
        /// </summary>
        private async Task DeleteSession(SessionDisplayData sessionData, int rowIndex)
        {
            try
            {
                // 삭제 확인
                var result = MessageBox.Show(
                    $"'{sessionData.SessionName}' 세션을 삭제하시겠습니까?\n\n" +
                    $"• 파일 개수: {sessionData.FileCount}개\n" +
                    $"• 합산 금액: {sessionData.TotalAmountFormatted}\n\n" +
                    "※ 세션 정보만 삭제되며, 업로드된 파일은 유지됩니다.\n" +
                    "※ 연결된 파일들은 다시 개별 파일로 분리됩니다.",
                    "세션 삭제 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes) return;

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

                            MessageBox.Show("세션이 삭제되었습니다.", "삭제 완료",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                    MessageBox.Show(
                    $"세션이 성공적으로 삭제되었습니다.\n\n" +
                    $"• 해제된 파일: {affectedFiles.Count}개",
                    "삭제 완료",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                    );
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
        /// 세션 결과 다운로드 (추후 구현)
        /// </summary>
        private async Task DownloadSessionResult(SessionDisplayData sessionData)
        {
            try
            {
                // result_file_path가 없으면 처리하지 않음
                if (string.IsNullOrEmpty(sessionData.ResultFilePath))
                {
                    MessageBox.Show("다운로드할 결과 파일이 없습니다.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // TODO: 실제 파일 다운로드 로직 구현
                MessageBox.Show($"결과 파일 다운로드:\n{sessionData.ResultFilePath}", "다운로드",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"결과 다운로드 오류: {ex.Message}");
                MessageBox.Show($"결과 다운로드 중 오류가 발생했습니다.\n{ex.Message}",
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
        /// 다중 파일 선택 시 추가 검증
        /// </summary>
        /// <summary>
        /// 다중 파일 선택 시 추가 검증 (계정명 내용 검증 완화)
        /// </summary>
        private ValidationResult ValidateMultipleFiles(List<FileDisplayData> selectedFiles)
        {
            if (selectedFiles.Count < 2)
                return new ValidationResult { IsValid = true };

            // 첫 번째 파일을 기준으로 설정
            var referenceFile = selectedFiles[0];
            string refAccountColumn = referenceFile.AccountColumnName.Trim().ToUpper();
            string refAmountColumn = referenceFile.AmountColumnName.Trim().ToUpper();
            var refColumns = referenceFile.DetectedColumns.Select(c => c.Trim().ToUpper()).OrderBy(c => c).ToList();

            // 나머지 파일들과 비교
            for (int i = 1; i < selectedFiles.Count; i++)
            {
                var currentFile = selectedFiles[i];
                string currAccountColumn = currentFile.AccountColumnName.Trim().ToUpper();
                string currAmountColumn = currentFile.AmountColumnName.Trim().ToUpper();

                // 계정명 컬럼 일치 확인
                if (refAccountColumn != currAccountColumn)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"계정명 컬럼이 일치하지 않습니다.\\n\\n" +
                                     $"• {referenceFile.OriginalFilename}: '{referenceFile.AccountColumnName}'\\n" +
                                     $"• {currentFile.OriginalFilename}: '{currentFile.AccountColumnName}'"
                    };
                }

                // 금액 컬럼 일치 확인
                if (refAmountColumn != currAmountColumn)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"금액 컬럼이 일치하지 않습니다.\\n\\n" +
                                     $"• {referenceFile.OriginalFilename}: '{referenceFile.AmountColumnName}'\\n" +
                                     $"• {currentFile.OriginalFilename}: '{currentFile.AmountColumnName}'"
                    };
                }

                // *** 변경: 계정명 내용 일치 확인 제거 (다중 계정명 허용) ***
                // 기존 계정명 내용 일치 검증 로직 주석 처리
                /*
                if (refAccountContent != currAccountContent)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"계정명 내용이 일치하지 않습니다..."
                    };
                }
                */

                // 전체 컬럼 구조 일치 확인 (헤더 구조 검증 강화)
                var currColumns = currentFile.DetectedColumns.Select(c => c.Trim().ToUpper()).OrderBy(c => c).ToList();

                if (refColumns.Count != currColumns.Count || !refColumns.SequenceEqual(currColumns))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"컬럼 구조가 일치하지 않습니다.\\n\\n" +
                                     $"• {referenceFile.OriginalFilename}: {refColumns.Count}개 컬럼\\n" +
                                     $"• {currentFile.OriginalFilename}: {currColumns.Count}개 컬럼\\n\\n" +
                                     "파일들의 헤더 구조가 정확히 일치해야 합니다."
                    };
                }
            }

            // *** 새로 추가: 다중 계정명 확인 메시지 ***
            var allAccountContents = selectedFiles.SelectMany(f => f.AccountContents ?? new List<string>()).Distinct().ToList();
            if (allAccountContents.Count > 1)
            {
                string message = $"선택된 파일들에서 {allAccountContents.Count}개의 서로 다른 계정명이 발견되었습니다.\\n\\n" +
                                "발견된 계정명:\\n" +
                                string.Join("\\n", allAccountContents.Take(10).Select((v, i) => $"{i + 1}. {v}")) +
                                (allAccountContents.Count > 10 ? $"\\n... 외 {allAccountContents.Count - 10}개 더" : "") +
                                "\\n\\n세션 생성 시 계정명별로 자동 분리됩니다.\\n계속 진행하시겠습니까?";

                var result = MessageBox.Show(message, "다중 계정명 확인",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "사용자가 다중 계정명 처리를 취소했습니다."
                    };
                }
            }

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// 세션 데이터 생성
        /// </summary>
        private async Task<SessionDisplayData> CreateSessionData(List<FileDisplayData> selectedFiles,
            ProcessProgressForm.UpdateProgressDelegate progressCallback)
        {
            try
            {
                await progressCallback(20, "세션 정보 계산 중...");

                // 세션 기본 정보 설정
                var firstFile = selectedFiles[0];
                string sessionName = GenerateSessionName(selectedFiles);

                // 합계 계산
                int totalRows = 0;
                decimal totalAmount = 0;

                await progressCallback(40, "파일별 데이터 합산 중...");

                // 각 파일의 실제 데이터를 읽어서 합산
                for (int i = 0; i < selectedFiles.Count; i++)
                {
                    var file = selectedFiles[i];
                    var fileData = await ProcessFileForSession(file);

                    totalRows += fileData.RowCount;
                    totalAmount += fileData.TotalAmount;

                    // 진행률 업데이트
                    int progress = 40 + ((i + 1) * 30 / selectedFiles.Count);
                    await progressCallback(progress, $"파일 처리 중... ({i + 1}/{selectedFiles.Count})");
                }

                await progressCallback(75, "MongoDB에 세션 저장 중...");

                // MongoDB에 세션 저장
                var fileIds = selectedFiles.Select(f => f.Id).ToList();
                var sessionDocument = new FileSessionDocument
                {
                    SessionName = sessionName,
                    AccountColumnName = firstFile.AccountColumnName,
                    AmountColumnName = firstFile.AmountColumnName,
                    TotalAmount = totalAmount,
                    TotalRows = totalRows,
                    Status = "processing",
                    CreatedDate = DateTime.UtcNow,
                    FileIds = fileIds
                };

                await _fileSessionRepository.CreateAsync(sessionDocument);

                // 파일들의 session_id 업데이트
                foreach (var file in selectedFiles)
                {
                    await _uploadedFileRepository.UpdateSessionIdAsync(file.Id, sessionDocument.Id);
                    file.SessionId = sessionDocument.Id; // 메모리에서도 업데이트
                }

                await progressCallback(85, "세션 데이터 생성 완료...");

                // 화면 표시용 데이터 생성
                return new SessionDisplayData
                {
                    Id = sessionDocument.Id,
                    SessionName = sessionName,
                    AccountColumnName = firstFile.AccountColumnName,
                    AmountColumnName = firstFile.AmountColumnName,
                    TotalAmount = totalAmount,
                    TotalAmountFormatted = totalAmount.ToString("N0") + " 원",
                    TotalRows = totalRows,
                    TotalRowsFormatted = totalRows.ToString("N0"),
                    FileCount = selectedFiles.Count,
                    Status = "processing",
                    StatusDisplay = "처리중",
                    CreatedDate = DateTime.UtcNow,
                    CreatedDateFormatted = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"),
                    ResultFilePath = null
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 데이터 생성 오류: {ex.Message}");
                throw;
            }
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
        /// 세션명 자동 생성
        /// </summary>
        private string GenerateSessionName(List<FileDisplayData> selectedFiles)
        {
            if (selectedFiles.Count == 1)
            {
                // 단일 파일인 경우 파일명 기반
                string baseName = Path.GetFileNameWithoutExtension(selectedFiles[0].OriginalFilename);
                return $"{baseName}_세션";
            }
            else
            {
                // 다중 파일인 경우 날짜와 파일 수 기반
                string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
                return $"{dateStr}_{selectedFiles[0].AccountColumnName}_{selectedFiles.Count}개파일_세션";
            }
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
