using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClosedXML.Excel;

using FinanceTool.MongoModels;
using FinanceTool.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using ClosedXML.Excel;
using FinanceTool.Data;
using System.Collections.Concurrent;
using System.Runtime;


namespace FinanceTool
{
    public partial class uc_FileLoad : UserControl
    {
        // 기존 필드
        private List<string> process_col_list = new List<string>();
        private string selectedStandColumn = "";
        //private HashSet<int> hiddenRows = new HashSet<int>();

        // SQLite 관련 추가 필드
        
        private int currentPage = 1;
        private int pageSize = 1000;
        private int totalPages = 1;
        private int totalRows = 0;

        private bool excelLoadinitFlag = true;

        // uc_FileLoad 클래스에서 멤버 변수 추가
        private bool _fileLoaded = false;

        // MongoDB Repository 객체
        private RawDataRepository rawDataRepo = new RawDataRepository();

        public uc_FileLoad()
        {
            InitializeComponent();
            InitializePagingControls(false);

        }

        private void InitializePagingControls(bool attachEvents)
        {
            // 콤보박스 초기화
            cmb_pageSize.Items.Clear();
            cmb_pageSize.Items.AddRange(new object[] { 500, 1000, 2000, 5000 });
            cmb_pageSize.SelectedIndex = 1; // 기본값 1000
            cmb_pageSize.DropDownStyle = ComboBoxStyle.DropDownList;

            // NumericUpDown 설정
            num_pageNumber.Minimum = 1;
            num_pageNumber.Maximum = 1;
            num_pageNumber.Value = 1;


            // 컨트롤 비활성화 (파일 로드 전)
            EnablePagingControls(false);

            // 이벤트 등록은 옵션에 따라 결정
            if (attachEvents)
            {
                AttachPagingEvents();
            }

            // 초기 페이징 상태
            UpdatePaginationInfo();

            DataHandler.RegisterDataGridView(dataGridView_delete_col);
            DataHandler.RegisterDataGridView(dataGridView_delete_data);
        }

        // 페이징 이벤트 등록 메서드 (별도로 분리)
        private void AttachPagingEvents()
        {
            // 이벤트 등록
            cmb_pageSize.SelectedIndexChanged += cmb_pageSize_SelectedIndexChanged;
            num_pageNumber.ValueChanged += num_pageNumber_ValueChanged;
            //btn_prevPage.Click += btn_prevPage_Click;
            //btn_nextPage.Click += btn_nextPage_Click;
        }

        // 페이징 컨트롤 활성화/비활성화 메서드
        private void EnablePagingControls(bool enabled)
        {
            btn_prevPage.Enabled = enabled;
            btn_nextPage.Enabled = enabled;
            num_pageNumber.Enabled = enabled;
            cmb_pageSize.Enabled = enabled;
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
            await LoadMongoPagedDataAsync();
        }

        private void uc_FileLoad_Load(object sender, EventArgs e)
        {
            // 초기화 시 아무 작업도 하지 않음
        }

        private async void btn_selectFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title = "엑셀 파일 선택"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // MongoDB 초기화 상태 확인
                    var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                    bool isInitialized = await mongoManager.EnsureInitializedAsync();

                    if (!isInitialized)
                    {
                        MessageBox.Show("MongoDB 초기화에 실패했습니다.", "오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // MongoDB 데이터 존재 여부 확인
                    var rawDataCollection = await mongoManager.GetCollectionAsync<RawDataDocument>("raw_data");
                    var filter = Builders<RawDataDocument>.Filter.Empty;
                    long documentCount = await rawDataCollection.CountDocumentsAsync(filter);

                    bool dataExists = documentCount > 0;
                    bool resetRequired = MongoDBManager.ResetDatabaseOnStartup;

                    // 파일 최초 load의 경우 바로 초기화
                    if (excelLoadinitFlag && resetRequired)
                    {
                        using (var progressForm = new ProcessProgressForm())
                        {
                            progressForm.Show();
                            progressForm.UpdateProgressHandler(0, "MongoDB 데이터베이스 초기화 준비 중...");

                            // MongoDB 데이터베이스 초기화 - 진행 상황을 표시하면서 초기화
                            await mongoManager.ResetDatabaseAsync(progressForm.UpdateProgressHandler);

                            // 완료 메시지
                            await progressForm.UpdateProgressHandler(100, "초기화 완료");
                            await Task.Delay(500); // 사용자가 완료 메시지를 볼 수 있도록 짧은 지연
                            progressForm.Close();
                        }
                        excelLoadinitFlag = false;
                    }
                    // 기존 데이터가 있거나 MongoDB 리셋이 필요한 경우
                    else if (dataExists || resetRequired)
                    {
                        if (dataExists)
                        {
                            DialogResult result = MessageBox.Show(
                                "파일을 새로 업로드 할 경우 \n기존 업로드 내역 및 작업 내용이 모두 초기화 됩니다.\n" +
                                "파일을 계속 업로드 하시겠습니까?",
                                "경고",
                                MessageBoxButtons.OKCancel,
                                MessageBoxIcon.Warning
                            );

                            if (result == DialogResult.Cancel)
                            {
                                return; // 사용자가 취소함
                            }
                        }

                        // 컬렉션의 문서 수에 따라 프로그레스바 표시
                        using (var progressForm = new ProcessProgressForm())
                        {
                            progressForm.Show();
                            progressForm.UpdateProgressHandler(0, "MongoDB 데이터베이스 초기화 중...");

                            // MongoDB 데이터베이스 초기화 - 진행 상황을 표시하면서 초기화
                            await mongoManager.ResetDatabaseAsync(progressForm.UpdateProgressHandler);

                            // 완료 메시지
                            await progressForm.UpdateProgressHandler(100, "초기화 완료");
                            await Task.Delay(500); // 사용자가 완료 메시지를 볼 수 있도록 짧은 지연
                            progressForm.Close();
                        }
                        Debug.WriteLine("MongoDB 데이터베이스 초기화 완료");
                    }

                    // 파일 로드 진행
                    string filePath = openFileDialog.FileName;
                    await LoadExcelDataAsync(filePath);
                    lbl_filename.Text = filePath;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"파일 로드 준비 중 오류가 발생했습니다: {ex.Message}", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Debug.WriteLine($"파일 로드 준비 오류: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        

        private async Task LoadExcelDataAsync(string filePath)
        {
            try
            {
                using (var progress = new ProcessProgressForm())
                {
                    progress.Show();
                    await progress.UpdateProgressHandler(5, "파일 업로드 준비 중...");

                    // 시스템 정보 확인
                    int cpuCount = Environment.ProcessorCount;
                    Debug.WriteLine($"시스템 정보: CPU 코어 {cpuCount}개 ");

                    // 메모리 효율적인 스트림 방식으로 엑셀 로딩
                    await progress.UpdateProgressHandler(10, "Excel 파일 스트림 준비 중...");

                    // 데이터 저장용 테이블
                    var excelData = new DataTable();
                    long totalRows = 0;

                    await Task.Run(async () => {
                        try
                        {
                            // 파일 크기 확인
                            var fileInfo = new FileInfo(filePath);
                            long fileSizeMB = fileInfo.Length / (1024 * 1024);
                            Debug.WriteLine($"파일 크기: {fileSizeMB}MB");
                            await progress.UpdateProgressHandler(12, $"파일 크기: {fileSizeMB}MB, 로딩 준비 중...");

                            // 최적화된 파일 스트림 설정
                            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 65536))
                            {
                                Stopwatch sw = Stopwatch.StartNew();

                                await progress.UpdateProgressHandler(15, "헤더 정보 로딩 중...");

                                // 헤더만 먼저 읽기
                                using (var workbook = new XLWorkbook(fs))
                                {
                                    var worksheet = workbook.Worksheets.First();

                                    // 헤더 정보 추출
                                    var headerRow = worksheet.Row(1);
                                    foreach (var cell in headerRow.CellsUsed())
                                    {
                                        string colName = cell.Value.ToString();
                                        excelData.Columns.Add(colName);
                                    }

                                    // 전체 행 수 계산
                                    totalRows = worksheet.LastRowUsed().RowNumber();
                                }

                                sw.Stop();
                                Debug.WriteLine($"헤더 로딩 완료: {sw.ElapsedMilliseconds}ms, 총 {totalRows}행 감지됨");
                                await progress.UpdateProgressHandler(20, $"총 {totalRows:N0}행 감지됨, 데이터 로딩 준비 중...");

                                // 파일 다시 열기 (스트림 리셋)
                                fs.Position = 0;

                                // 청크 기반 데이터 로딩
                                sw.Restart();

                                // 청크 크기 결정
                                int chunkSize = CalculateOptimalChunkSize(fileSizeMB);
                                int chunkCount = (int)Math.Ceiling((totalRows - 1) / (double)chunkSize);

                                await progress.UpdateProgressHandler(22, $"청크 기반 로딩 준비: {chunkCount}개 청크 (청크당 {chunkSize}행)");

                                // 데이터 로딩
                                using (var workbook = new XLWorkbook(fs))
                                {
                                    var worksheet = workbook.Worksheets.First();

                                    // 병렬 처리 옵션

                                    //var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = cpuCount };
                                    var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = cpuCount * 10 };

                                    // 청크 단위로 데이터 로드
                                    for (int chunk = 0; chunk < chunkCount; chunk++)
                                    {
                                        int startRow = chunk * chunkSize + 2; // 헤더 다음부터
                                        int endRow = Math.Min(startRow + chunkSize - 1, (int)totalRows);

                                        // 진행 상황 업데이트
                                        int progressPercentage = 25 + (int)((double)chunk / chunkCount * 30);
                                        await progress.UpdateProgressHandler(progressPercentage,
                                            $"청크 {chunk + 1}/{chunkCount} 로딩 중 ({startRow}-{endRow}/{totalRows})");

                                        // 병렬 처리용 컬렉션
                                        var chunkRows = new ConcurrentBag<DataRow>();

                                        // 병렬 처리로 행 데이터 로드
                                        await Task.Run(() => {
                                            Parallel.For(startRow, endRow + 1, parallelOptions, rowIndex => {
                                                try
                                                {
                                                    var xlRow = worksheet.Row(rowIndex);
                                                    DataRow newRow = excelData.NewRow();

                                                    // 각 셀 데이터 처리
                                                    foreach (var cell in xlRow.CellsUsed())
                                                    {
                                                        int columnIndex = cell.Address.ColumnNumber - 1;
                                                        if (columnIndex < excelData.Columns.Count)
                                                        {
                                                            // 값 추출 및 변환
                                                            object value = null;
                                                            if (!cell.IsEmpty())
                                                            {
                                                                value = ConvertCellValue(cell);
                                                            }

                                                            newRow[columnIndex] = value ?? DBNull.Value;
                                                        }
                                                    }

                                                    chunkRows.Add(newRow);
                                                }
                                                catch (Exception ex)
                                                {
                                                    Debug.WriteLine($"행 {rowIndex} 처리 중 오류: {ex.Message}");
                                                }
                                            });
                                        });

                                        // 처리된 행을 테이블에 추가
                                        foreach (var row in chunkRows)
                                        {
                                            excelData.Rows.Add(row);
                                        }

                                        // 메모리 관리
                                        if (totalRows > 500000 && chunk % 5 == 0)
                                        {
                                            GC.Collect();
                                            GC.WaitForPendingFinalizers();
                                        }
                                    }
                                }

                                sw.Stop();
                                Debug.WriteLine($"데이터 로딩 완료: {sw.ElapsedMilliseconds}ms, 총 {excelData.Rows.Count}행 처리됨");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"엑셀 로딩 오류: {ex.Message}");
                            throw;
                        }
                    });

                    await progress.UpdateProgressHandler(55, "데이터 로딩 완료, MongoDB 저장 준비 중...");

                    // MongoDB 데이터 컨버터 생성
                    MongoDataConverter mongoConverter = new MongoDataConverter();

                    // 병렬 처리로 MongoDB 저장
                    var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = cpuCount };

                    // MongoDB 저장 호출
                    List<RawDataDocument> documents = await mongoConverter.ConvertExcelToMongoDBAsync(
                        excelData, Path.GetFileName(filePath), progress.UpdateProgressHandler, parallelOptions);

                    await progress.UpdateProgressHandler(85, "UI 초기화 중...");

                    // 나머지 UI 설정 코드...
                    if (!_fileLoaded)
                    {
                        AttachPagingEvents();
                        _fileLoaded = true;
                    }

                    EnablePagingControls(true);
                    DataHandler.excelData = excelData;
                    currentPage = 1;
                    pageSize = 1000;
                    await LoadMongoPagedDataAsync(true);
                    await AddMongoColumnsToGrid(dataGridView_delete_col, excelData.Columns);
                    await progress.UpdateProgressHandler(95, "컬럼 정보 설정 중...");
                    GetMongoColumnList(excelData.Columns);
                    SetupColumnLists();

                    await progress.UpdateProgressHandler(100, "데이터 로드 완료!");
                    progress.Close();

                    Debug.WriteLine("File loading completed successfully with MongoDB");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"엑셀 파일 로드 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 셀 값 변환 헬퍼 메서드
        private object ConvertCellValue(IXLCell cell)
        {
            try
            {
                if (cell.IsEmpty())
                    return null;

                // 셀 타입에 따른 값 변환
                switch (cell.DataType)
                {
                    case XLDataType.Number:
                        double numValue = cell.GetValue<double>();
                        // 정수형으로 변환 가능한지 확인
                        if (Math.Abs(numValue - Math.Round(numValue)) < double.Epsilon)
                        {
                            if (numValue >= long.MinValue && numValue <= long.MaxValue)
                                return (long)numValue;
                            else
                                return numValue;
                        }
                        return numValue;

                    case XLDataType.DateTime:
                        return cell.GetValue<DateTime>();

                    case XLDataType.Boolean:
                        return cell.GetValue<bool>();

                    case XLDataType.Text:
                    default:
                        return cell.GetValue<string>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"셀 값 변환 오류: {ex.Message}");
                return cell.Value.ToString();
            }
        }

        // 최적의 청크 크기 계산
        private int CalculateOptimalChunkSize(long fileSizeMB)
        {
            // 파일 크기에 따른 청크 크기 최적화
            if (fileSizeMB > 200)
                return 2000; // 매우 큰 파일
            else if (fileSizeMB > 100)
                return 5000; // 큰 파일
            else if (fileSizeMB > 50)
                return 10000; // 중간 크기 파일
            else
                return 20000; // 작은 파일
        }

        // MongoDB 기반으로 페이징 데이터 로드
        private async Task LoadMongoPagedDataAsync(bool progressYN = false)
        {
            // 파일이 로드되지 않았으면 아무 작업도 수행하지 않음
            if (!_fileLoaded)
            {
                Debug.WriteLine("파일이 로드되지 않아 페이징 작업을 건너뜁니다.");
                return;
            }

            try
            {
                // MongoDB 데이터 컨버터
                MongoDataConverter mongoConverter = new MongoDataConverter();

                if (progressYN)
                {
                    // MongoDB에서 페이징된 데이터 가져오기
                    var filter = Builders<RawDataDocument>.Filter.Empty;

                    // hiddenData가 false인 경우, 숨겨진 문서 제외
                    if (!DataHandler.hiddenData)
                    {
                        filter = Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false);
                    }

                    var (documents, totalCount) = await mongoConverter.GetPagedRawDataAsync(
                        currentPage, pageSize, DataHandler.hiddenData);



                    // 페이징 메타데이터 계산
                    totalRows = (int)totalCount;
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                    // MongoDB 문서를 DataTable로 변환
                    DataTable pageData = ConvertMongoDocumentsToDataTable(documents);

                    // UI 업데이트는 메인 스레드에서 수행
                    this.BeginInvoke(new Action(() =>
                    {
                        ConfigureDataGridView(pageData, dataGridView_target);
                        ConfigureDataGridView(pageData, dataGridView_process);
                        UpdatePaginationInfo();
                        ApplyGridFormatting();
                    }));
                }
                else
                {
                    using (var loadingForm = new ProcessProgressForm())
                    {
                        loadingForm.Show();
                        loadingForm.UpdateProgressHandler(10);

                        // MongoDB에서 페이징된 데이터 가져오기
                        var result = await Task.Run(async () =>
                        {
                            // MongoDB에서 페이징된 데이터 가져오기
                            var filter = Builders<RawDataDocument>.Filter.Empty;

                            // hiddenData가 false인 경우, 숨겨진 문서 제외
                            if (!DataHandler.hiddenData)
                            {
                                filter = Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false);
                            }

                            var (documents, totalCount) = await mongoConverter.GetPagedRawDataAsync(
                            currentPage, pageSize, DataHandler.hiddenData);

                            // 페이징 메타데이터 계산
                            totalRows = (int)totalCount;
                            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                            loadingForm.UpdateProgressHandler(70);

                            // MongoDB 문서를 DataTable로 변환
                            return ConvertMongoDocumentsToDataTable(documents);
                        });

                        loadingForm.UpdateProgressHandler(80);

                        // UI 업데이트는 메인 스레드에서 수행
                        this.BeginInvoke(new Action(() =>
                        {
                            ConfigureDataGridView(result, dataGridView_target);
                            ConfigureDataGridView(result, dataGridView_process);
                            UpdatePaginationInfo();
                            ApplyGridFormatting();
                        }));

                        loadingForm.UpdateProgressHandler(100);
                        await Task.Delay(300);
                        loadingForm.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 페이지 데이터 로드 중 오류: {ex.Message}");
                MessageBox.Show($"데이터 로드 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // MongoDB 문서를 DataTable로 변환하는 헬퍼 메서드
        private DataTable ConvertMongoDocumentsToDataTable(List<RawDataDocument> documents)
        {
            DataTable dataTable = new DataTable();

            // 기본 컬럼 추가
            dataTable.Columns.Add("id", typeof(string));
            dataTable.Columns.Add("import_date", typeof(DateTime));
            dataTable.Columns.Add("is_hidden", typeof(bool));  // hiddenYN 대신 is_hidden 사용

            // 첫 번째 문서의 데이터를 기반으로 동적 컬럼 추가
            if (documents.Count > 0 && documents[0].Data != null)
            {
                foreach (var key in documents[0].Data.Keys)
                {
                    if (!dataTable.Columns.Contains(key))
                    {
                        dataTable.Columns.Add(key);
                    }
                }
            }

            // 문서 데이터를 DataTable에 추가
            foreach (var doc in documents)
            {
                DataRow row = dataTable.NewRow();
                row["id"] = doc.Id;
                row["import_date"] = doc.ImportDate;
                row["is_hidden"] = doc.IsHidden;  // 직접 is_hidden 값 사용

                // 동적 데이터 필드 추가
                if (doc.Data != null)
                {
                    foreach (var kvp in doc.Data)
                    {
                        if (dataTable.Columns.Contains(kvp.Key))
                        {
                            row[kvp.Key] = kvp.Value ?? DBNull.Value;
                        }
                    }
                }

                dataTable.Rows.Add(row);
            }

            return dataTable;
        }


        // MongoDB 컬럼 목록을 그리드에 추가
        private async Task AddMongoColumnsToGrid(DataGridView targetDgv, DataColumnCollection columns)
        {
            // 대상 DataGridView 초기화
            targetDgv.DataSource = null;
            targetDgv.Rows.Clear();
            targetDgv.Columns.Clear();

            if (DataHandler.dragSelections.ContainsKey(targetDgv))
            {
                DataHandler.dragSelections[targetDgv].Clear();
            }

            // 체크박스 컬럼 추가
            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn
            {
                Name = "CheckBox",
                HeaderText = "",
                Width = 50,
                ThreeState = false,
                FillWeight = 20
            };
            targetDgv.Columns.Add(checkColumn);

            // 데이터 컬럼 추가
            DataGridViewTextBoxColumn textColumn = new DataGridViewTextBoxColumn
            {
                Name = "Data",
                HeaderText = "컬럼명"
            };
            targetDgv.Columns.Add(textColumn);

            // GridView 설정
            targetDgv.AllowUserToAddRows = false;
            targetDgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            targetDgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            targetDgv.Columns["Data"].ReadOnly = true;
            targetDgv.Columns["CheckBox"].ReadOnly = false;
            targetDgv.Font = new System.Drawing.Font("맑은 고딕", 14.25F);

            // 컬럼 추가
            foreach (DataColumn column in columns)
            {
                if (!column.ColumnName.Equals("id") &&
                    !column.ColumnName.Equals("import_date") &&
                    !column.ColumnName.Equals("hiddenYN"))
                {
                    int rowIndex = targetDgv.Rows.Add();
                    targetDgv.Rows[rowIndex].Cells["CheckBox"].Value = true;
                    targetDgv.Rows[rowIndex].Cells["Data"].Value = column.ColumnName;
                }
            }
        }

        // MongoDB 컬럼 목록 가져오기
        private void GetMongoColumnList(DataColumnCollection columns)
        {
            process_col_list = new List<string>();

            foreach (DataColumn column in columns)
            {
                if (column.ColumnName != "id" &&
                    column.ColumnName != "import_date" &&
                    column.ColumnName != "hiddenYN")
                {
                    // 선택된 열(visibleColumns)이 있는 경우 그 열만 포함
                    if (DataHandler.visibleColumns == null ||
                        DataHandler.visibleColumns.Count == 0 ||
                        DataHandler.visibleColumns.Contains(column.ColumnName))
                    {
                        process_col_list.Add(column.ColumnName);
                    }
                }
            }

            Debug.WriteLine($"MongoDB process_col_list count: {process_col_list.Count}");
        }

        // 컬럼 목록 설정
        private void SetupColumnLists()
        {
            try
            {
                // ComboBox에 열 이름 추가 (공통 로직)
                SetupComboBox(stand_col_combo, "데이터 삭제 기준 열 선택");
                SetupComboBox(sub_acc_col_combo, "세목 열 선택");
                SetupComboBox(dept_col_combo, "부서 열 선택");
                SetupComboBox(prod_col_combo, "공급업체 열 선택");
                SetupComboBox(cmb_target, "키워드 대상 열 선택");
                SetupComboBox(cmb_money, "금액 열 선택");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"컬럼 목록 설정 중 오류 발생: {ex.Message}");
            }
        }

        // ComboBox 설정 공통 로직
        private void SetupComboBox(ComboBox comboBox, string defaultText)
        {
            comboBox.Items.Clear();
            comboBox.Items.Add(defaultText);

            foreach (string column in process_col_list)
            {
                comboBox.Items.Add(column);
            }

            if (comboBox.Items.Count > 0)
                comboBox.SelectedIndex = 0;
        }


        public void ConfigureDataGridView(DataTable dataTable, DataGridView dataGridView)
        {
            // DataGridView의 DataSource를 DataTable로 설정
            dataGridView.DataSource = dataTable;

            // id와 import_date 컬럼을 항상 숨김 처리
            if (dataGridView.Columns.Contains("id"))
            {
                dataGridView.Columns["id"].Visible = false;
            }

            if (dataGridView.Columns.Contains("import_date"))
            {
                dataGridView.Columns["import_date"].Visible = false;
            }

            // is_hidden 컬럼이 있다면 그것도 숨김
            if (dataGridView.Columns.Contains("is_hidden"))
            {
                dataGridView.Columns["is_hidden"].Visible = false;
            }

            // 각 행을 순회하며 is_hidden 필드에 따라 스타일 적용
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                bool isHidden = false;

                // is_hidden 컬럼 확인
                if (dataGridView.Columns.Contains("is_hidden") &&
                    row.Cells["is_hidden"].Value != null)
                {
                    isHidden = Convert.ToBoolean(row.Cells["is_hidden"].Value);
                }
                // 기존 hiddenYN 컬럼 확인 (하위 호환성 유지)
                else if (dataGridView.Columns.Contains("hiddenYN") &&
                         row.Cells["hiddenYN"].Value != null &&
                         row.Cells["hiddenYN"].Value.ToString() == "0")
                {
                    isHidden = true;
                }

                // 숨겨진 행이면 회색 스타일 적용
                if (isHidden)
                {
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.LightGray;
                    row.DefaultCellStyle.ForeColor = System.Drawing.Color.DarkGray;
                }
            }
        }

        // 페이징 정보 업데이트
        private void UpdatePaginationInfo()
        {
            // NumericUpDown 범위 설정
            num_pageNumber.Maximum = Math.Max(1, totalPages);

            // 현재 페이지 값 설정 (이벤트 발생 방지를 위해 조건 체크)
            if (num_pageNumber.Value != currentPage)
                num_pageNumber.Value = currentPage;

            // 라벨 텍스트 업데이트
            lbl_pagination2.Text = $"/ {totalPages} (총 {totalRows:N0}행)";

            // 버튼 활성화/비활성화
            btn_prevPage.Enabled = currentPage > 1;
            btn_nextPage.Enabled = currentPage < totalPages;
        }

        // 그리드 형식 적용
        private void ApplyGridFormatting()
        {
            foreach (DataGridView dgv in new[] { dataGridView_target, dataGridView_process })
            {
                // AutoSizeColumnsMode 설정 제거
                dgv.AllowUserToAddRows = false;
                dgv.ReadOnly = true;
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

                // 헤더 스타일 설정
                dgv.EnableHeadersVisualStyles = false;
                dgv.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.LightSteelBlue;
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.Black;
                dgv.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("맑은 고딕", 9.0f, FontStyle.Bold);

                // 셀 폰트 설정
                dgv.DefaultCellStyle.Font = new System.Drawing.Font("맑은 고딕", 9.0f);
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

        // 페이지 크기 변경
        private async void cmb_pageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmb_pageSize.SelectedItem != null)
            {
                pageSize = Convert.ToInt32(cmb_pageSize.SelectedItem);
                currentPage = 1; // 페이지 크기 변경 시 첫 페이지로
                await LoadMongoPagedDataAsync();
            }
        }

       

       
     

        

        //체크 항목 데이터 수집
        public List<string> GetCheckedRowsData(DataGridView dgv)
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

        public class NonNumericData
        {
            public int RowIndex { get; set; }
            public string Value { get; set; }
        }

        //금액 컬럼 검증 로직
        //숫자 데이터가 아닌 데이터가 있다면 false return
        private (bool isAllNumeric, List<NonNumericData> nonNumericList) CheckNumericColumn(DataGridView dgv, string columnName)
        {
            var nonNumericList = new List<NonNumericData>();

            try
            {
                if (!dgv.Columns.Contains(columnName))
                {
                    throw new ArgumentException($"컬럼 '{columnName}'이 존재하지 않습니다.");
                }

                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.IsNewRow) continue;

                    var cellValue = row.Cells[columnName].Value;

                    if (cellValue == null || cellValue == DBNull.Value || string.IsNullOrWhiteSpace(cellValue.ToString()))
                        continue;

                    string strValue = cellValue.ToString().Trim();

                    if (!decimal.TryParse(strValue, out _))
                    {
                        nonNumericList.Add(new NonNumericData
                        {
                            RowIndex = row.Index,
                            Value = strValue
                        });
                    }
                }

                return (nonNumericList.Count == 0, nonNumericList);
            }
            catch (Exception ex)
            {
                throw new Exception($"컬럼 검사 중 오류 발생: {ex.Message}");
            }
        }

        private async void btn_complete_Click(object sender, EventArgs e)
        {
            //data Validation 
            if (sub_acc_col_combo.SelectedIndex < 1)
            {
                MessageBox.Show("세목 열을 선택하셔야 합니다.", "알림",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Warning);
                return;
            }

            if (dept_col_combo.SelectedIndex < 1)
            {
                MessageBox.Show("부서 열을 선택하셔야 합니다.", "알림",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Warning);
                return;
            }

            if (prod_col_combo.SelectedIndex < 1)
            {
                MessageBox.Show("공급업체 열을 선택하셔야 합니다.", "알림",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Warning);
                return;
            }

            if (cmb_money.SelectedIndex < 1)
            {
                MessageBox.Show("금액 열을 선택하셔야 합니다.", "알림",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning);
                return;
            }
            else
            {
                // 금액 컬럼 데이터 유효성 검증
                var (isAllNumeric, nonNumericData) = await CheckNumericColumnAsync(cmb_money.SelectedItem.ToString());
                if (!isAllNumeric)
                {
                    var firstError = nonNumericData[0];
                    MessageBox.Show(
                        $"금액 열은 숫자값만 있어야 합니다.\n행 번호 : {firstError.RowIndex + 1}, 행 값 : {firstError.Value}",
                        "알림",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }
            }

            if (cmb_target.SelectedIndex < 1)
            {
                MessageBox.Show("타겟 열을 선택하셔야 합니다.", "알림",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    progressForm.UpdateProgressHandler(10);

                    // 선택된 컬럼 목록 생성
                    List<string> selectedColumns = new List<string>();
                    selectedColumns.Add(sub_acc_col_combo.SelectedItem.ToString());
                    selectedColumns.Add(dept_col_combo.SelectedItem.ToString());
                    selectedColumns.Add(prod_col_combo.SelectedItem.ToString());
                    selectedColumns.Add(cmb_money.SelectedItem.ToString());
                    selectedColumns.Add(cmb_target.SelectedItem.ToString());

                    // 필요한 전역 변수 설정
                    DataHandler.sub_acc_col_name = sub_acc_col_combo.SelectedItem.ToString();
                    DataHandler.dept_col_name = dept_col_combo.SelectedItem.ToString();
                    DataHandler.prod_col_name = prod_col_combo.SelectedItem.ToString();
                    DataHandler.levelList.Clear();
                    DataHandler.levelName.Clear();

                    // 금액 컬럼 인덱스 설정 (프로세스 테이블에서는 0)
                    DataHandler.levelList.Add(5);
                    DataHandler.moneyIndex = 5;
                    DataHandler.levelName.Add(cmb_money.SelectedItem.ToString());

                    // 타겟 컬럼 인덱스 설정 (프로세스 테이블에서는 1)
                    DataHandler.levelList.Add(6);
                    DataHandler.levelName.Add(cmb_target.SelectedItem.ToString());

                    progressForm.UpdateProgressHandler(30);

                    // MongoDB 방식으로 변경
                    MongoDataConverter mongoConverter = new MongoDataConverter();
                    await mongoConverter.PrepareProcessDataAsync(selectedColumns);

                    progressForm.UpdateProgressHandler(70);

                    // 레거시 코드와의 호환을 위해 DataHandler.processTable 설정
                    // 변경 전: DataHandler.processTable = DBManager.Instance.ExecuteQuery("SELECT * FROM process_data");

                    // 변경 후: MongoDB에서 데이터 가져와 DataTable로 변환
                    DataHandler.processTable = await DataHandler.GetDataTableFromProcessDataAsync();

                    progressForm.UpdateProgressHandler(90);
                    progressForm.UpdateProgressHandler(100);
                }

                // 다음 단계로 이동
                await userControlHandler.uc_Preprocessing.initUI();

                if (this.ParentForm is Form1 form)
                {
                    form.LoadUserControl(userControlHandler.uc_Preprocessing);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"처리 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine($"btn_complete_Click 오류: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // 숫자 컬럼 체크 함수 - MongoDB 버전
        private async Task<(bool isAllNumeric, List<NonNumericData> nonNumericList)> CheckNumericColumnAsync(string columnName)
        {
            var nonNumericList = new List<NonNumericData>();

            try
            {
                // MongoDB에서 해당 필드를 가진 모든 문서 조회
                var filter = Builders<RawDataDocument>.Filter.Ne($"Data.{columnName}", BsonNull.Value);

                // 숨겨진 문서는 제외
                if (DataHandler.hiddenData)
                {
                    filter = Builders<RawDataDocument>.Filter.And(
                        filter,
                        Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false)
                    );
                }

                var documents = await rawDataRepo.FindDocumentsAsync(filter);
                int rowIndex = 0;

                foreach (var doc in documents)
                {
                    if (doc.Data != null && doc.Data.ContainsKey(columnName) && doc.Data[columnName] != null)
                    {
                        var value = doc.Data[columnName];
                        string strValue = value.ToString().Trim();

                        if (!string.IsNullOrEmpty(strValue))
                        {
                            // 숫자로 변환 가능한지 확인
                            if (!decimal.TryParse(strValue, out _))
                            {
                                nonNumericList.Add(new NonNumericData
                                {
                                    RowIndex = rowIndex,
                                    Value = strValue
                                });
                            }
                        }
                    }
                    rowIndex++;
                }

                return (nonNumericList.Count == 0, nonNumericList);
            }
            catch (Exception ex)
            {
                throw new Exception($"컬럼 검사 중 오류 발생: {ex.Message}");
            }
        }



        private void del_col_list_allcheck_CheckedChanged(object sender, EventArgs e)
        {
            // 모든 행의 체크박스 상태 변경
            foreach (DataGridViewRow row in dataGridView_delete_col.Rows)
            {
                row.Cells[0].Value = del_col_list_allcheck.Checked;
            }
        }

        private void del_data_list_allcheck_CheckedChanged(object sender, EventArgs e)
        {
            // 모든 행의 체크박스 상태 변경
            foreach (DataGridViewRow row in dataGridView_delete_data.Rows)
            {
                row.Cells[0].Value = del_data_list_allcheck.Checked;
            }
        }

        private void restore_col_btn_Click(object sender, EventArgs e)
        {
            List<string> restore_list = GetCheckedRowsData(dataGridView_delete_col);

            // 선택된 열 목록 저장
            DataHandler.visibleColumns = new List<string>(restore_list);

            for (int i = 0; i < dataGridView_process.Columns.Count; i++)
            {
                if (restore_list.Contains(dataGridView_process.Columns[i].Name))
                {
                    dataGridView_process.Columns[i].Visible = true;
                }
                else
                {
                    dataGridView_process.Columns[i].Visible = false;
                }
            }

            // MongoDB 컬렉션에서 컬럼 가시성 업데이트
            foreach (DataColumn column in DataHandler.excelData.Columns)
            {
                if (column.ColumnName != "id" &&
                    column.ColumnName != "import_date" &&
                    column.ColumnName != "hiddenYN")
                {
                    bool isVisible = restore_list.Contains(column.ColumnName);
                    try
                    {
                        // MongoDB에서 컬럼 매핑 정보 업데이트
                        UpdateColumnVisibilityInMongo(column.ColumnName, isVisible);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"컬럼 가시성 업데이트 오류: {ex.Message}");
                    }
                }
            }

            // 선택된 열만 사용하도록 컬럼 목록 업데이트
            GetMongoColumnList(DataHandler.excelData.Columns);
            SetupColumnLists();
        }

        // MongoDB에서 컬럼 가시성 업데이트하는 새 메서드
        private async void UpdateColumnVisibilityInMongo(string columnName, bool isVisible)
        {
            try
            {
                var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                var columnCollection = await mongoManager.GetCollectionAsync<BsonDocument>("column_mapping");

                var filter = Builders<BsonDocument>.Filter.Eq("original_name", columnName);
                var update = Builders<BsonDocument>.Update.Set("is_visible", isVisible);

                await columnCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 컬럼 가시성 업데이트 오류: {ex.Message}");
                // 오류 무시하고 계속 진행
            }
        }


        private void stand_col_combo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (stand_col_combo.SelectedIndex == 0)
            {
                return;
            }

            selectedStandColumn = stand_col_combo.SelectedItem.ToString();
            PopulateDeleteDataGrid(selectedStandColumn);
        }

        // 삭제 데이터 그리드 채우기
        // 삭제 데이터 그리드 채우기 - MongoDB 버전으로 변환
        private async void PopulateDeleteDataGrid(string columnName)
        {
            try
            {
                // MongoDB에서 고유 값 가져오기
                // 이전 코드: string query = $"SELECT DISTINCT {columnName} FROM raw_data WHERE {columnName} IS NOT NULL ORDER BY {columnName}";
                // 이전 코드: DataTable distinctValues = DBManager.Instance.ExecuteQuery(query);

                // MongoDB에서 필드의 고유 값 가져오기
                var distinctValues = await GetDistinctValuesFromMongoAsync(columnName);

                // DataGridView 초기화
                dataGridView_delete_data.DataSource = null;
                dataGridView_delete_data.Rows.Clear();
                dataGridView_delete_data.Columns.Clear();
                if (DataHandler.dragSelections.ContainsKey(dataGridView_delete_data))
                {
                    DataHandler.dragSelections[dataGridView_delete_data].Clear();
                }

                // 체크박스 컬럼 추가
                DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn
                {
                    Name = "CheckBox",
                    HeaderText = "",
                    Width = 50,
                    ThreeState = false,
                    FillWeight = 20
                };
                dataGridView_delete_data.Columns.Add(checkColumn);

                // 데이터 컬럼 추가
                DataGridViewTextBoxColumn dataColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Data",
                    HeaderText = "데이터"
                };
                dataGridView_delete_data.Columns.Add(dataColumn);

                // 데이터 리스트의 각 항목을 행으로 추가
                foreach (var value in distinctValues)
                {
                    if (value != null && !string.IsNullOrEmpty(value.ToString()))
                    {
                        int rowIndex = dataGridView_delete_data.Rows.Add();
                        dataGridView_delete_data.Rows[rowIndex].Cells["CheckBox"].Value = false;
                        dataGridView_delete_data.Rows[rowIndex].Cells["Data"].Value = value.ToString();
                    }
                }

                dataGridView_delete_data.AllowUserToAddRows = false;
                dataGridView_delete_data.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGridView_delete_data.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dataGridView_delete_data.ReadOnly = false;
                dataGridView_delete_data.Columns["CheckBox"].ReadOnly = false;  // 체크박스 컬럼만 편집 가능
                dataGridView_delete_data.Font = new System.Drawing.Font("맑은 고딕", 14.25F);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"데이터 로드 중 오류: {ex.Message}");
                MessageBox.Show($"데이터 로드 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // MongoDB에서 필드의 고유값 가져오기
        private async Task<List<object>> GetDistinctValuesFromMongoAsync(string fieldName)
        {
            // 필드가 존재하는 모든 문서에서 고유 값을 가져오기
            //var filter = Builders<RawDataDocument>.Filter.Ne($"Data.{fieldName}", BsonNull.Value);
            var filterBuilder = Builders<RawDataDocument>.Filter;
            var filter = filterBuilder.And(
                filterBuilder.Exists($"Data.{fieldName}"),
                filterBuilder.Ne($"Data.{fieldName}", BsonNull.Value)
            );

            // 숨겨진 문서는 제외
            if (DataHandler.hiddenData)
            {
                filter = Builders<RawDataDocument>.Filter.And(
                    filter,
                    Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false)
                );
            }

            var distinctValues = new List<object>();
            var documents = await rawDataRepo.FindDocumentsAsync(filter);

            // 문서에서 해당 필드의 고유 값을 추출
            var valueSet = new HashSet<string>();
            foreach (var doc in documents)
            {
                if (doc.Data != null && doc.Data.ContainsKey(fieldName) && doc.Data[fieldName] != null)
                {
                    string value = doc.Data[fieldName].ToString();
                    if (!string.IsNullOrEmpty(value) && !valueSet.Contains(value))
                    {
                        valueSet.Add(value);
                        distinctValues.Add(value);
                    }
                }
            }

            // 값을 정렬
            distinctValues.Sort((a, b) => string.Compare(a.ToString(), b.ToString()));

            return distinctValues;
        }

        private async void restore_del_data_btn_Click(object sender, EventArgs e)
        {
            try
            {
                // 모든 그리드 선택 초기화
                InitializeCursors();

                DataHandler.hiddenData = false;

                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    progressForm.UpdateProgressHandler(20);

                    // MongoDB에서 모든 데이터의 is_hidden을 false로 설정
                    await Task.Run(async () =>
                    {
                        var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                        var rawDataCollection = await mongoManager.GetCollectionAsync<RawDataDocument>("raw_data");

                        var filter = Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, true);
                        var update = Builders<RawDataDocument>.Update.Set(d => d.IsHidden, false);

                        await rawDataCollection.UpdateManyAsync(filter, update);
                    });

                    progressForm.UpdateProgressHandler(50);

                    // UI 업데이트
                    this.BeginInvoke(new Action(() =>
                    {
                        // 모든 행 표시 설정 & 스타일 초기화
                        RestoreAllRowsVisibility();
                    }));

                    progressForm.UpdateProgressHandler(70);

                    // 데이터 다시 로드
                    if (!string.IsNullOrEmpty(selectedStandColumn))
                    {
                        PopulateDeleteDataGrid(selectedStandColumn);
                    }

                    progressForm.UpdateProgressHandler(90);

                    // 페이지 데이터 리로드
                    await LoadMongoPagedDataAsync();
                    progressForm.UpdateProgressHandler(100);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 복원 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 모든 행 표시 상태 및 스타일 복원
        private void RestoreAllRowsVisibility()
        {
            // Process 그리드
            foreach (DataGridViewRow row in dataGridView_process.Rows)
            {
                row.Visible = true;
            }

            // Target 그리드 - 스타일 초기화
            foreach (DataGridViewRow row in dataGridView_target.Rows)
            {
                row.DefaultCellStyle.BackColor = dataGridView_target.DefaultCellStyle.BackColor;
                row.DefaultCellStyle.ForeColor = dataGridView_target.DefaultCellStyle.ForeColor;
            }
        }

        private async void delete_data_btn_Click(object sender, EventArgs e)
        {
            List<string> delList = GetCheckedRowsData(dataGridView_delete_data);

            if (delList.Count == 0)
            {
                MessageBox.Show("데이터 삭제 대상을 선택하셔야 합니다.", "알림",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Warning);
                return;
            }

            DataHandler.hiddenData = true;

            try
            {
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    progressForm.UpdateProgressHandler(10);

                    // MongoDB에서 숨기기 처리
                    int hiddenCount = 0;
                    await Task.Run(async () =>
                    {
                        // MongoDB 저장소 접근
                        var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                        var rawDataCollection = await mongoManager.GetCollectionAsync<RawDataDocument>("raw_data");

                        // 각 값에 대해 해당하는 문서 숨기기
                        foreach (string value in delList)
                        {
                            // 필드 값이 일치하는 문서 찾기
                            var filter = Builders<RawDataDocument>.Filter.Eq($"Data.{selectedStandColumn}", value);
                            var update = Builders<RawDataDocument>.Update.Set("is_hidden", true);

                            // 업데이트 실행
                            var result = await rawDataCollection.UpdateManyAsync(filter, update);
                            hiddenCount += (int)result.ModifiedCount;
                        }
                    });

                    progressForm.UpdateProgressHandler(30);

                    // UI 업데이트는 메인 스레드에서 수행
                    this.BeginInvoke(new Action(() =>
                    {
                        // 모든 그리드 선택 초기화
                        InitializeCursors();
                    }));

                    progressForm.UpdateProgressHandler(80);

                    // 삭제 데이터 목록에서 처리된 항목 제거
                    RemoveProcessedRows(delList);

                    progressForm.UpdateProgressHandler(90);

                    // 페이지 데이터 리로드 (MongoDB 버전 메서드 호출)
                    await LoadMongoPagedDataAsync();

                    progressForm.UpdateProgressHandler(100);
                }

                MessageBox.Show($"{delList.Count}개 항목이 숨겨졌습니다.", "정보",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 숨기기 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // 그리드 커서 초기화 메서드 분리
        private void InitializeCursors()
        {
            // 모든 그리드 선택 해제
            foreach (DataGridView dgv in new[] { dataGridView_target, dataGridView_process })
            {
                try
                {
                    // 현재 선택을 모두 제거
                    dgv.ClearSelection();

                    // 현재 셀을 null로 설정
                    dgv.CurrentCell = null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"그리드 커서 초기화 실패: {ex.Message}");
                }
            }

            // 이전에 처리했던 방식대로 추가 조치
            Application.DoEvents(); // UI 업데이트 허용
        }

        // 처리된 행을 삭제 데이터 그리드에서 제거
        private void RemoveProcessedRows(List<string> values)
        {
            for (int i = dataGridView_delete_data.Rows.Count - 1; i >= 0; i--)
            {
                DataGridViewRow row = dataGridView_delete_data.Rows[i];
                string value = row.Cells["Data"].Value?.ToString();
                if (values.Contains(value))
                {
                    dataGridView_delete_data.Rows.RemoveAt(i);
                }
            }
        }



        // 그리드뷰 셀 선택 이벤트 핸들러
        private void dataGridView_target_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView_target.SelectedRows.Count > 0)
            {
            }
          
        }

        private void dataGridView_target_CellClick(object sender, DataGridViewCellEventArgs e)
        {
          
        }

        private async void delete_search_button_Click(object sender, EventArgs e)
        {
            // 현재 선택된 컬럼이 없으면 메시지 표시 후 종료
            if (string.IsNullOrEmpty(selectedStandColumn) || stand_col_combo.SelectedIndex == 0)
            {
                MessageBox.Show("검색하기 전에 기준 열을 선택해주세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 검색 키워드 가져오기
            string searchText = delete_search_keyword.Text.Trim();

            // 키워드가 비어있으면 모든 항목 표시
            if (string.IsNullOrEmpty(searchText))
            {
                PopulateDeleteDataGrid(selectedStandColumn);
                return;
            }

            try
            {
                // 키워드를 쉼표(,)로 분리
                string[] keywords = searchText.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                              .Select(k => k.Trim())
                                              .Where(k => !string.IsNullOrEmpty(k))
                                              .ToArray();

                if (keywords.Length == 0)
                {
                    PopulateDeleteDataGrid(selectedStandColumn);
                    return;
                }

                // MongoDB에서 검색 - 정규식 검색 사용
                var filteredValues = await SearchMongoFieldByKeywordsAsync(selectedStandColumn, keywords);

                // DataGridView 초기화 및 데이터 표시
                PopulateDeleteDataGridWithResults(filteredValues);

                // 결과 메시지 표시 (선택사항)
                if (filteredValues.Count == 0)
                {
                    MessageBox.Show($"검색 결과가 없습니다.", "검색 결과",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"검색 중 오류 발생: {ex.Message}");
                MessageBox.Show($"데이터 검색 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // MongoDB에서 검색을 위한 새 메서드
        private async Task<List<string>> SearchMongoFieldByKeywordsAsync(string fieldName, string[] keywords)
        {
            var resultValues = new List<string>();
            var valueSet = new HashSet<string>(); // 중복 방지를 위한 Set

            foreach (string keyword in keywords)
            {
                // 정규식 패턴 생성 (대소문자 구분 없이 검색)
                var regexPattern = new BsonRegularExpression(keyword, "i");

                // 필드 값이 검색 키워드를 포함하는 문서 필터
                var filter = Builders<RawDataDocument>.Filter.Regex($"Data.{fieldName}", regexPattern);

                // 숨겨진 문서는 제외
                if (DataHandler.hiddenData)
                {
                    filter = Builders<RawDataDocument>.Filter.And(
                        filter,
                        Builders<RawDataDocument>.Filter.Eq(d => d.IsHidden, false)
                    );
                }

                // 문서 조회
                var documents = await rawDataRepo.FindDocumentsAsync(filter);

                // 결과에서 필드 값 추출
                foreach (var doc in documents)
                {
                    if (doc.Data != null && doc.Data.ContainsKey(fieldName) && doc.Data[fieldName] != null)
                    {
                        string value = doc.Data[fieldName].ToString();
                        if (!string.IsNullOrEmpty(value) && !valueSet.Contains(value))
                        {
                            valueSet.Add(value);
                            resultValues.Add(value);
                        }
                    }
                }
            }

            // 결과 정렬
            resultValues.Sort();

            return resultValues;
        }

        // 필터링된 결과로 DataGridView 채우기 - 기존 함수 유지
        private void PopulateDeleteDataGridWithResults(List<string> filteredValues)
        {
            // DataGridView 초기화
            dataGridView_delete_data.DataSource = null;
            dataGridView_delete_data.Rows.Clear();
            dataGridView_delete_data.Columns.Clear();
            if (DataHandler.dragSelections.ContainsKey(dataGridView_delete_data))
            {
                DataHandler.dragSelections[dataGridView_delete_data].Clear();
            }

            // 체크박스 컬럼 추가
            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn
            {
                Name = "CheckBox",
                HeaderText = "",
                Width = 50,
                ThreeState = false,
                FillWeight = 20
            };
            dataGridView_delete_data.Columns.Add(checkColumn);

            // 데이터 컬럼 추가
            DataGridViewTextBoxColumn dataColumn = new DataGridViewTextBoxColumn
            {
                Name = "Data",
                HeaderText = "데이터"
            };
            dataGridView_delete_data.Columns.Add(dataColumn);

            // 필터링된 데이터 추가
            foreach (string value in filteredValues)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    int rowIndex = dataGridView_delete_data.Rows.Add();
                    dataGridView_delete_data.Rows[rowIndex].Cells["CheckBox"].Value = false;
                    dataGridView_delete_data.Rows[rowIndex].Cells["Data"].Value = value;
                }
            }

            // DataGridView 설정
            dataGridView_delete_data.AllowUserToAddRows = false;
            dataGridView_delete_data.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView_delete_data.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView_delete_data.ReadOnly = false;
            dataGridView_delete_data.Columns["CheckBox"].ReadOnly = false;
            dataGridView_delete_data.Font = new System.Drawing.Font("맑은 고딕", 14.25F);
        }

        // 필터링된 결과로 DataGridView 채우기
       

        private void delete_search_keyword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                delete_search_button_Click(sender, e);
                e.SuppressKeyPress = true;  // 비프음 방지
            }
        }

       
    }

    

    // 간단한 로딩 폼
    public class LoadingForm : Form
    {
        private Label messageLabel;

        public LoadingForm(string message)
        {
            InitializeComponent(message);
        }

        private void InitializeComponent(string message)
        {
            this.Width = 300;
            this.Height = 100;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = System.Drawing.Color.White;

            messageLabel = new Label();
            messageLabel.Text = message;
            messageLabel.AutoSize = false;
            messageLabel.Size = new Size(280, 60);
            messageLabel.Location = new Point(10, 20);
            messageLabel.TextAlign = ContentAlignment.MiddleCenter;
            messageLabel.Font = new System.Drawing.Font("맑은 고딕", 12F, FontStyle.Regular);

            this.Controls.Add(messageLabel);
        }
    }
}