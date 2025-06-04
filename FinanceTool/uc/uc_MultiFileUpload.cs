using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
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

            // *** 새로 추가: 계정명 내용 컬럼 ***
            var accountContentColumn = new DataGridViewTextBoxColumn
            {
                Name = "AccountContent",
                HeaderText = "계정명 내용",
                DataPropertyName = "AccountContentFormatted",
                Width = 150,
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

                // 3단계: 다중 파일일 경우 추가 검증
                if (selectedFiles.Count > 1)
                {
                    var multiValidationResult = ValidateMultipleFiles(selectedFiles);
                    if (!multiValidationResult.IsValid)
                    {
                        MessageBox.Show(multiValidationResult.ErrorMessage, "검증 오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                // 4단계: 세션 생성 처리
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "세션 생성 중...");

                    var sessionData = await CreateSessionData(selectedFiles, progressForm.UpdateProgressHandler);

                    if (sessionData != null)
                    {
                        await progressForm.UpdateProgressHandler(90, "세션 목록 업데이트...");

                        // dgv_sessions에 추가
                        AddSessionToGrid(sessionData);

                        // 선택된 파일들의 체크 해제
                        ClearFileSelections(selectedFiles);

                        await progressForm.UpdateProgressHandler(100, "완료");
                        await Task.Delay(500);

                        MessageBox.Show("세션이 성공적으로 생성되었습니다.", "완료",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 생성 오류: {ex.Message}");
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
        /// 파일 삭제 처리
        /// </summary>
        private async Task DeleteFile(FileDisplayData fileData, int rowIndex)
        {
            try
            {
                // 삭제 확인
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

                    // 1. 서버 파일 삭제
                    string filePath = Path.Combine(UPLOAD_FOLDER, fileData.StoredFilename);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        Debug.WriteLine($"서버 파일 삭제: {filePath}");
                    }

                    await progressForm.UpdateProgressHandler(60, "데이터베이스 정보 삭제 중...");

                    // 2. MongoDB 데이터 삭제
                    bool mongoDeleted = await _uploadedFileRepository.DeleteAsync(fileData.Id);
                    if (!mongoDeleted)
                    {
                        throw new Exception("데이터베이스에서 파일 정보 삭제에 실패했습니다.");
                    }

                    await progressForm.UpdateProgressHandler(90, "목록 업데이트 중...");

                    // 3. DataGridView에서 제거
                    var currentList = (dgv_files.DataSource as List<FileDisplayData>) ?? new List<FileDisplayData>();
                    currentList.RemoveAt(rowIndex);
                    dgv_files.DataSource = currentList.ToList();

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

                // 계정명 또는 금액 컬럼 선택이 변경된 경우
                if (dgv_files.Columns[e.ColumnIndex].Name == "AccountColumn" ||
                    dgv_files.Columns[e.ColumnIndex].Name == "AmountColumn")
                {
                    await UpdateFileColumnInfo(fileData);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"셀 값 변경 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 파일의 컬럼 정보 업데이트 (콤보박스 값 유지)
        /// </summary>
        private async Task UpdateFileColumnInfo(FileDisplayData fileData)
        {
            try
            {
                // 현재 선택된 값들 백업
                string currentAccountColumn = fileData.AccountColumnName;
                string currentAmountColumn = fileData.AmountColumnName;

                // 계정명 컬럼이 선택된 경우
                if (!string.IsNullOrEmpty(currentAccountColumn))
                {
                    var accountValidation = await ValidateAndExtractAccountContent(fileData);
                    if (!accountValidation.IsValid)
                    {
                        // 검증 실패 시 사용자에게 알림 후 해당 컬럼만 초기화
                        MessageBox.Show(accountValidation.ErrorMessage, "계정명 컬럼 오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        // 계정명 관련 데이터만 초기화
                        fileData.AccountColumnName = "";
                        fileData.AccountContents.Clear();
                        fileData.AccountContentFormatted = "";
                        currentAccountColumn = "";
                    }
                }

                // 금액 컬럼이 선택된 경우
                if (!string.IsNullOrEmpty(currentAmountColumn))
                {
                    var amountValidation = await ValidateAndCalculateAmount(fileData);
                    if (!amountValidation.IsValid)
                    {
                        // 검증 실패 시 사용자에게 알림 후 해당 컬럼만 초기화
                        MessageBox.Show(amountValidation.ErrorMessage, "금액 컬럼 오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        // 금액 관련 데이터만 초기화
                        fileData.AmountColumnName = "";
                        fileData.TotalAmount = 0;
                        fileData.TotalAmountFormatted = "";
                        currentAmountColumn = "";
                    }
                }

                // MongoDB에 저장 (계정명 컬럼이 선택된 경우에만)
                if (!string.IsNullOrEmpty(currentAccountColumn))
                {
                    bool updated = await _uploadedFileRepository.UpdateColumnInfoWithAccountContentsAsync(
                        fileData.Id,
                        currentAccountColumn,
                        currentAmountColumn ?? "",
                        fileData.TotalAmount,
                        fileData.AccountContents
                    );

                    if (!updated)
                    {
                        Debug.WriteLine($"MongoDB 업데이트 실패: {fileData.OriginalFilename}");
                    }
                }

                // *** 핵심: 콤보박스 값을 다시 설정하여 유지 ***
                SetComboBoxValue(fileData, "AccountColumn", currentAccountColumn);
                SetComboBoxValue(fileData, "AmountColumn", currentAmountColumn);

                // UI 새로고침
                RefreshFileGridRowSpecific(fileData);

                Debug.WriteLine($"파일 {fileData.OriginalFilename} 컬럼 정보 업데이트 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파일 컬럼 정보 업데이트 오류: {ex.Message}");
                MessageBox.Show($"파일 정보 업데이트 중 오류가 발생했습니다.\n{ex.Message}",
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
                            // *** 빈 데이터도 UI에 반영 ***
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

                    // *** UI 스레드에서 데이터 업데이트 ***
                    Application.OpenForms[0].Invoke((MethodInvoker)delegate
                    {
                        fileData.AccountContents = uniqueContents;
                        fileData.AccountContentFormatted = uniqueContents.Count > 0 ?
                            (uniqueContents.Count == 1 ? uniqueContents[0] :
                             string.Join(", ", uniqueContents.Take(3)) + (uniqueContents.Count > 3 ? "..." : "")) : "";
                    });

                    // 중복되지 않은 값이 2개 이상인 경우 오류
                    if (uniqueContents.Count > 1)
                    {
                        var displayContents = uniqueContents.Take(5).ToList();
                        string errorMessage = "계정명 컬럼은 1가지 값만 존재해야 합니다.\n\n" +
                                            $"발견된 값들 ({uniqueContents.Count}개):\n" +
                                            string.Join("\n", displayContents.Select((v, i) => $"{i + 1}. {v}"));

                        if (uniqueContents.Count > 5)
                        {
                            errorMessage += $"\n... 외 {uniqueContents.Count - 5}개 더";
                        }

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
                            await Task.Delay(500);

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
                        rows = allRows.Count;

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
            dgv_sessions.MultiSelect = false;

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
            var sessionNameColumn = new DataGridViewTextBoxColumn
            {
                Name = "SessionName",
                HeaderText = "세션명",
                DataPropertyName = "SessionName",
                Width = 200,
                ReadOnly = true
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
        }

        /// <summary>
        /// 세션 그리드 셀 포맷팅 (다운로드 버튼 상태 처리)
        /// </summary>
        private void Dgv_sessions_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                if (dgv_sessions.Columns[e.ColumnIndex].Name == "DownloadButton")
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
                    var session = await _fileSessionRepository.GetByIdAsync(sessionData.Id.ToString());

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
        private ValidationResult ValidateMultipleFiles(List<FileDisplayData> selectedFiles)
        {
            if (selectedFiles.Count < 2)
                return new ValidationResult { IsValid = true };

            // 첫 번째 파일을 기준으로 설정
            var referenceFile = selectedFiles[0];
            string refAccountColumn = referenceFile.AccountColumnName.Trim().ToUpper();
            string refAmountColumn = referenceFile.AmountColumnName.Trim().ToUpper();
            string refAccountContent = referenceFile.AccountContents.FirstOrDefault()?.Trim().ToUpper() ?? "";
            var refColumns = referenceFile.DetectedColumns.Select(c => c.Trim().ToUpper()).OrderBy(c => c).ToList();

            // 나머지 파일들과 비교
            for (int i = 1; i < selectedFiles.Count; i++)
            {
                var currentFile = selectedFiles[i];
                string currAccountColumn = currentFile.AccountColumnName.Trim().ToUpper();
                string currAmountColumn = currentFile.AmountColumnName.Trim().ToUpper();
                string currAccountContent = currentFile.AccountContents.FirstOrDefault()?.Trim().ToUpper() ?? "";

                // 계정명 컬럼 일치 확인
                if (refAccountColumn != currAccountColumn)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"계정명 컬럼이 일치하지 않습니다.\n\n" +
                                     $"• {referenceFile.OriginalFilename}: '{referenceFile.AccountColumnName}'\n" +
                                     $"• {currentFile.OriginalFilename}: '{currentFile.AccountColumnName}'"
                    };
                }

                // 금액 컬럼 일치 확인
                if (refAmountColumn != currAmountColumn)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"금액 컬럼이 일치하지 않습니다.\n\n" +
                                     $"• {referenceFile.OriginalFilename}: '{referenceFile.AmountColumnName}'\n" +
                                     $"• {currentFile.OriginalFilename}: '{currentFile.AmountColumnName}'"
                    };
                }

                // *** 새로 추가: 계정명 내용 일치 확인 ***
                if (refAccountContent != currAccountContent)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"계정명 내용이 일치하지 않습니다.\n\n" +
                                     $"• {referenceFile.OriginalFilename}: '{referenceFile.AccountContents.FirstOrDefault()}'\n" +
                                     $"• {currentFile.OriginalFilename}: '{currentFile.AccountContents.FirstOrDefault()}'\n\n" +
                                     "동일한 세션으로 묶으려면 계정명 내용이 같아야 합니다."
                    };
                }

                // 전체 컬럼 구조 일치 확인
                var currColumns = currentFile.DetectedColumns.Select(c => c.Trim().ToUpper()).OrderBy(c => c).ToList();

                if (refColumns.Count != currColumns.Count || !refColumns.SequenceEqual(currColumns))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"컬럼 구조가 일치하지 않습니다.\n\n" +
                                     $"• {referenceFile.OriginalFilename}: {refColumns.Count}개 컬럼\n" +
                                     $"• {currentFile.OriginalFilename}: {currColumns.Count}개 컬럼"
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
        /// 세션을 dgv_sessions에 추가
        /// </summary>
        private void AddSessionToGrid(SessionDisplayData sessionData)
        {
            try
            {
                var currentSessions = (dgv_sessions.DataSource as List<SessionDisplayData>) ?? new List<SessionDisplayData>();
                currentSessions.Add(sessionData);
                dgv_sessions.DataSource = currentSessions.ToList();

                Debug.WriteLine($"세션 추가: {sessionData.SessionName}");
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
            string refAccountColumn = referenceSession.AccountColumnName.Trim().ToUpper();
            string refAmountColumn = referenceSession.AmountColumnName.Trim().ToUpper();

            for (int i = 1; i < sessions.Count; i++)
            {
                var currentSession = sessions[i];
                string currAccountColumn = currentSession.AccountColumnName.Trim().ToUpper();
                string currAmountColumn = currentSession.AmountColumnName.Trim().ToUpper();

                // 계정명 컬럼 일치 확인
                if (refAccountColumn != currAccountColumn)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"계정명 컬럼이 일치하지 않습니다.\n\n" +
                                     $"• {referenceSession.SessionName}: '{referenceSession.AccountColumnName}'\n" +
                                     $"• {currentSession.SessionName}: '{currentSession.AccountColumnName}'"
                    };
                }

                // 금액 컬럼 일치 확인
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

                await progressCallback(20, "세션 데이터 수집 중...");

                // 병합할 세션들의 파일 ID 수집
                var allFileIds = new List<ObjectId>();
                decimal totalAmount = targetSession.TotalAmount;
                decimal totalRows = targetSession.TotalRows;
                int totalFileCount = targetSession.FileCount;

                for (int i = 0; i < sessionsToDelete.Count; i++)
                {
                    var session = sessionsToDelete[i];

                    // MongoDB에서 세션 정보 조회하여 파일 ID 수집
                    var sessionDoc = await _fileSessionRepository.GetByIdAsync(session.Id.ToString());
                    if (sessionDoc?.FileIds != null)
                    {
                        allFileIds.AddRange(sessionDoc.FileIds);
                    }

                    totalAmount += session.TotalAmount;
                    totalRows += session.TotalRows;
                    totalFileCount += session.FileCount;

                    int progress = 20 + ((i + 1) * 30 / sessionsToDelete.Count);
                    await progressCallback(progress, $"세션 데이터 수집 중... ({i + 1}/{sessionsToDelete.Count})");
                }

                await progressCallback(60, "파일들의 세션 재할당 중...");

                // 수집된 파일들을 대상 세션으로 재할당
                foreach (var fileId in allFileIds)
                {
                    await _uploadedFileRepository.UpdateSessionIdAsync(fileId, targetSession.Id);
                    await _fileSessionRepository.AddFileToSessionAsync(targetSession.Id, fileId);
                }

                await progressCallback(75, "대상 세션 업데이트 중...");

                // 대상 세션의 총합 업데이트
                await _fileSessionRepository.UpdateSessionTotalsAsync(targetSession.Id, totalAmount, totalRows);

                await progressCallback(85, "병합된 세션들 삭제 중...");

                // 병합된 세션들 삭제
                foreach (var session in sessionsToDelete)
                {
                    await _fileSessionRepository.DeleteAsync(session.Id);
                }

                // 메모리의 대상 세션 데이터 업데이트
                targetSession.TotalAmount = totalAmount;
                targetSession.TotalAmountFormatted = totalAmount.ToString("N0") + " 원";
                targetSession.TotalRows = totalRows;
                targetSession.TotalRowsFormatted = totalRows.ToString("N0");
                targetSession.FileCount = totalFileCount;

                return (true, targetSession, sessionsToDelete);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"세션 병합 처리 오류: {ex.Message}");
                throw;
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
    }


}
