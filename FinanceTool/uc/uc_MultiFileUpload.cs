using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FinanceTool.MongoModels;
using FinanceTool.Repositories;
using MongoDB.Bson;
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

                // 기존 업로드된 파일들 로드
                LoadExistingFiles();
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
            dgv_files.MultiSelect = false;

            // 컬럼 정의
            dgv_files.Columns.Clear();

            // 체크박스 컬럼 추가
            var checkBoxColumn = new DataGridViewCheckBoxColumn
            {
                Name = "Check",
                HeaderText = "",
                DataPropertyName = "IsSelected",
                Width = 30,
                ThreeState = false
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
                DataPropertyName = "TotalRows",
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
        /// 선택된 파일들 일괄 삭제
        /// </summary>
        private async void btn_delete_selected_Click(object sender, EventArgs e)
        {
            try
            {
                var selectedFiles = GetSelectedFiles();
                if (selectedFiles.Count == 0)
                {
                    MessageBox.Show("삭제할 파일을 선택해주세요.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"선택된 {selectedFiles.Count}개의 파일을 삭제하시겠습니까?\n\n" +
                    "※ 서버의 실제 파일과 데이터베이스 정보가 모두 삭제됩니다.",
                    "파일 삭제 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes) return;

                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();

                    for (int i = 0; i < selectedFiles.Count; i++)
                    {
                        var fileData = selectedFiles[i];
                        int progress = 10 + ((i + 1) * 80 / selectedFiles.Count);

                        await progressForm.UpdateProgressHandler(progress,
                            $"파일 삭제 중... ({i + 1}/{selectedFiles.Count})");

                        // 서버 파일 삭제
                        string filePath = Path.Combine(UPLOAD_FOLDER, fileData.StoredFilename);
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }

                        // MongoDB 데이터 삭제
                        await _uploadedFileRepository.DeleteAsync(fileData.Id);
                    }

                    await progressForm.UpdateProgressHandler(95, "목록 새로고침...");

                    // 전체 목록 다시 로드
                    LoadExistingFiles();

                    await progressForm.UpdateProgressHandler(100, "삭제 완료");
                    await Task.Delay(500);
                }

                MessageBox.Show($"{selectedFiles.Count}개의 파일이 성공적으로 삭제되었습니다.",
                    "삭제 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"일괄 삭제 오류: {ex.Message}");
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
        /// 파일의 컬럼 정보 업데이트
        /// </summary>
        private async Task UpdateFileColumnInfo(FileDisplayData fileData)
        {
            try
            {
                // 계정명과 금액 컬럼이 모두 선택된 경우에만 처리
                if (string.IsNullOrEmpty(fileData.AccountColumnName) ||
                    string.IsNullOrEmpty(fileData.AmountColumnName))
                {
                    return;
                }

                // 엑셀 파일에서 금액 컬럼의 합계 계산
                decimal totalAmount = await CalculateTotalAmountFromExcel(
                    fileData.StoredFilename, fileData.AmountColumnName);

                fileData.TotalAmount = totalAmount;
                fileData.TotalAmountFormatted = totalAmount.ToString("N0") + " 원";

                // MongoDB 업데이트
                await _uploadedFileRepository.UpdateColumnInfoAsync(
                    fileData.Id, fileData.AccountColumnName, fileData.AmountColumnName, totalAmount);

                // DataGridView 새로고침
                dgv_files.Invalidate();

                Debug.WriteLine($"파일 {fileData.OriginalFilename}의 컬럼 정보 업데이트 완료: " +
                               $"계정명={fileData.AccountColumnName}, 금액={fileData.AmountColumnName}, " +
                               $"합계={totalAmount:N0}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파일 컬럼 정보 업데이트 오류: {ex.Message}");
                MessageBox.Show($"파일 정보 업데이트 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                AccountColumnName = file.AccountColumnName,
                AmountColumnName = file.AmountColumnName,
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
            public int TotalRows { get; set; }
            public string AccountColumnName { get; set; }
            public string AmountColumnName { get; set; }
            public decimal TotalAmount { get; set; }
            public string TotalAmountFormatted { get; set; }
            public ObjectId? SessionId { get; set; }
            public string ProcessingStatus { get; set; }
        }
    }

   
}
