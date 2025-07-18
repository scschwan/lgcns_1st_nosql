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

        // === 페이지 표시 메서드들 ===
        private void DisplayPage2nd()
        {
            try
            {
                if (_fullDataTable2nd == null || _fullDataTable2nd.Rows.Count == 0)
                {
                    dataGridView_2nd.DataSource = null;
                    return;
                }

                int startIndex = (_currentPage2nd - 1) * _pageSize2nd;
                int endIndex = Math.Min(startIndex + _pageSize2nd, _fullDataTable2nd.Rows.Count);

                DataTable pageData = _fullDataTable2nd.Clone();

                for (int i = startIndex; i < endIndex; i++)
                {
                    if (i < _fullDataTable2nd.Rows.Count)
                    {
                        pageData.ImportRow(_fullDataTable2nd.Rows[i]);
                    }
                }

                dataGridView_2nd.DataSource = pageData;

                if (dataGridView_2nd.Columns["raw_data_id"] != null)
                    dataGridView_2nd.Columns["raw_data_id"].Visible = false;

                if (dataGridView_2nd.Columns["_id"] != null)
                    dataGridView_2nd.Columns["_id"].Visible = false;

                if (dataGridView_2nd.Columns["import_date"] != null)
                    dataGridView_2nd.Columns["import_date"].Visible = false;

                if (dataGridView_2nd.Columns["is_hidden"] != null)
                    dataGridView_2nd.Columns["is_hidden"].Visible = false;

                Debug.WriteLine($"dataGridView_2nd 페이지 {_currentPage2nd} 표시: {pageData.Rows.Count}개 행");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"dataGridView_2nd 페이지 표시 오류: {ex.Message}");
            }
        }

        private void DisplayPageTransform()
        {
            try
            {
                if (_fullDataTableTransform == null || _fullDataTableTransform.Rows.Count == 0)
                {
                    dataGridView_transform.DataSource = null;
                    return;
                }

                int startIndex = (_currentPageTransform - 1) * _pageSizeTransform;
                int endIndex = Math.Min(startIndex + _pageSizeTransform, _fullDataTableTransform.Rows.Count);

                DataTable pageData = _fullDataTableTransform.Clone();

                for (int i = startIndex; i < endIndex; i++)
                {
                    if (i < _fullDataTableTransform.Rows.Count)
                    {
                        pageData.ImportRow(_fullDataTableTransform.Rows[i]);
                    }
                }

                dataGridView_transform.DataSource = pageData;

                if (dataGridView_transform.Columns["raw_data_id"] != null)
                    dataGridView_transform.Columns["raw_data_id"].Visible = false;

                if (dataGridView_transform.Columns["_id"] != null)
                    dataGridView_transform.Columns["_id"].Visible = false;

                if (dataGridView_transform.Columns["import_date"] != null)
                    dataGridView_transform.Columns["import_date"].Visible = false;

                if (dataGridView_transform.Columns["is_hidden"] != null)
                    dataGridView_transform.Columns["is_hidden"].Visible = false;

                Debug.WriteLine($"dataGridView_transform 페이지 {_currentPageTransform} 표시: {pageData.Rows.Count}개 행");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"dataGridView_transform 페이지 표시 오류: {ex.Message}");
            }
        }

        // === 페이징 컨트롤 업데이트 ===
        private void UpdatePaginationControls2nd()
        {
            try
            {
                int totalRecords = _fullDataTable2nd?.Rows.Count ?? 0;

                //lbl_pagination.Text = $"페이지: {_currentPage2nd}";
                lbl_pagination2.Text = $"/ {_totalPages2nd} (총 {totalRecords:N0}개)";

                num_pageNumber.Maximum = Math.Max(1, _totalPages2nd);
                if (num_pageNumber.Value != _currentPage2nd)
                    num_pageNumber.Value = _currentPage2nd;

                btn_prevPage.Enabled = _currentPage2nd > 1;
                btn_nextPage.Enabled = _currentPage2nd < _totalPages2nd;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"dataGridView_2nd 페이징 컨트롤 업데이트 오류: {ex.Message}");
            }
        }

        private void UpdatePaginationControlsTransform()
        {
            try
            {
                int totalRecords = _fullDataTableTransform?.Rows.Count ?? 0;

                //lbl_pagination3.Text = $"페이지: {_currentPageTransform}";
                lbl_pagination4.Text = $"/ {_totalPagesTransform} (총 {totalRecords:N0}개)";

                num_pageNumber2.Maximum = Math.Max(1, _totalPagesTransform);
                if (num_pageNumber2.Value != _currentPageTransform)
                    num_pageNumber2.Value = _currentPageTransform;

                btn_prevPage2.Enabled = _currentPageTransform > 1;
                btn_nextPage2.Enabled = _currentPageTransform < _totalPagesTransform;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"dataGridView_transform 페이징 컨트롤 업데이트 오류: {ex.Message}");
            }
        }

        // === 데이터 설정 메서드들 ===
        private void SetFullDataTable2nd(DataTable fullData)
        {
            try
            {
                _fullDataTable2nd = fullData;
                _currentPage2nd = 1;

                if (fullData != null && fullData.Rows.Count > 0)
                {
                    _totalPages2nd = (int)Math.Ceiling((double)fullData.Rows.Count / _pageSize2nd);
                    EnablePaginationControls2nd(true);
                }
                else
                {
                    _totalPages2nd = 1;
                    EnablePaginationControls2nd(false);
                }

                DisplayPage2nd();
                UpdatePaginationControls2nd();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"dataGridView_2nd 전체 데이터 설정 오류: {ex.Message}");
            }
        }

        private void SetFullDataTableTransform(DataTable fullData)
        {
            try
            {
                _fullDataTableTransform = fullData;
                _currentPageTransform = 1;

                if (fullData != null && fullData.Rows.Count > 0)
                {
                    _totalPagesTransform = (int)Math.Ceiling((double)fullData.Rows.Count / _pageSizeTransform);
                    EnablePaginationControlsTransform(true);
                }
                else
                {
                    _totalPagesTransform = 1;
                    EnablePaginationControlsTransform(false);
                }

                DisplayPageTransform();
                UpdatePaginationControlsTransform();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"dataGridView_transform 전체 데이터 설정 오류: {ex.Message}");
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

      
        /// <summary>
        /// 기존 EnrichTransformDataWithMongoData 메서드를 대체하는 호출부
        /// </summary>
        public async Task<DataTable> EnrichTransformDataWithMongoData(DataTable transformDataTable)
        {
            try
            {
                Debug.WriteLine("EnrichTransformDataWithMongoData 시작");

                // MongoDB 연결 확인
                await Data.MongoDBManager.Instance.EnsureInitializedAsync();

                // 1. 가시적 컬럼 목록 조회
                var columnMappingFilter = Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("is_visible", true);
                var columnMappingsResult = await Data.MongoDBManager.Instance.FindDocumentsAsync<MongoDB.Bson.BsonDocument>(
                    "column_mapping",
                    columnMappingFilter);

                // 시각화될 컬럼명 추출
                List<string> visibleColumns = new List<string>();
                foreach (var doc in columnMappingsResult)
                {
                    if (doc.Contains("original_name"))
                    {
                        string originalName = doc["original_name"].AsString;
                        visibleColumns.Add(originalName);
                    }
                }

                Debug.WriteLine($"시각화될 컬럼: {string.Join(", ", visibleColumns)}");

                if (visibleColumns.Count == 0)
                {
                    Debug.WriteLine("표시할 컬럼이 없습니다. 원본 테이블 복사본 반환");
                    return transformDataTable.Copy();
                }

                // 2. 안전한 결과 테이블 생성 (uc_Clustering 패턴)
                DataTable resultTable = CreateSafeResultTable(transformDataTable, visibleColumns);

                // 3. raw_data_id 수집 및 유효성 검증
                var rawDataIds = new HashSet<string>();
                var rowToIdMap = new Dictionary<int, string>();

                for (int i = 0; i < transformDataTable.Rows.Count; i++)
                {
                    DataRow row = transformDataTable.Rows[i];
                    if (row["raw_data_id"] != DBNull.Value && row["raw_data_id"] != null)
                    {
                        string rawDataId = row["raw_data_id"].ToString();
                        if (!string.IsNullOrEmpty(rawDataId))
                        {
                            rawDataIds.Add(rawDataId);
                            rowToIdMap[i] = rawDataId;
                        }
                    }
                }

                if (rawDataIds.Count == 0)
                {
                    Debug.WriteLine("유효한 raw_data_id가 없습니다.");
                    return CopyDataSafely(transformDataTable, resultTable);
                }

                Debug.WriteLine($"보강할 raw_data_id: {rawDataIds.Count}개");

                // 4. MongoDB에서 안전한 배치 조회
                var mongoDataLookup = await LoadMongoDataSafely(rawDataIds);
                Debug.WriteLine($"MongoDB 데이터 로드 완료: {mongoDataLookup.Count}개");

                // 5. 안전한 데이터 보강 (행별 순차 처리)
                await EnrichDataSafely(transformDataTable, resultTable, mongoDataLookup, visibleColumns, rowToIdMap);

                Debug.WriteLine($"EnrichTransformDataWithMongoData 완료: {resultTable.Rows.Count}행");
                return resultTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 데이터 보강 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
                // 예외 발생 시 원본 데이터 테이블의 복사본 반환
                return transformDataTable.Copy();
            }
        }

        /// <summary>
        /// 안전한 결과 테이블 생성 (uc_Clustering 패턴)
        /// </summary>
        private DataTable CreateSafeResultTable(DataTable sourceTable, List<string> visibleColumns)
        {
            DataTable resultTable = new DataTable();

            try
            {
                
                //1. 가시적 컬럼들 추가 (중복 제외)
                foreach (string columnName in visibleColumns)
                {
                    if (!resultTable.Columns.Contains(columnName))
                    {
                        resultTable.Columns.Add(columnName, typeof(string));
                    }
                }
                // 2. 먼저 원본 테이블의 컬럼들 추가
                foreach (DataColumn sourceColumn in sourceTable.Columns)
                {
                    Type columnType = sourceColumn.DataType;
                    // 안전성을 위해 모든 컬럼을 string 타입으로 통일
                    resultTable.Columns.Add(sourceColumn.ColumnName, typeof(string));
                }


                Debug.WriteLine($"결과 테이블 컬럼 생성 완료: {resultTable.Columns.Count}개");
                return resultTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"결과 테이블 생성 오류: {ex.Message}");
                // 오류 시 최소한의 테이블 반환
                DataTable fallbackTable = new DataTable();
                fallbackTable.Columns.Add("raw_data_id", typeof(string));
                return fallbackTable;
            }
        }

        /// <summary>
        /// MongoDB 데이터 안전 로드 (uc_Clustering 배치 패턴)
        /// </summary>
        private async Task<Dictionary<string, Dictionary<string, object>>> LoadMongoDataSafely(HashSet<string> rawDataIds)
        {
            var mongoDataLookup = new Dictionary<string, Dictionary<string, object>>();

            try
            {
                var rawDataRepo = new Repositories.RawDataRepository();
                const int batchSize = 10000; // 안전한 배치 크기
                var idList = rawDataIds.ToList();

                // 배치별 순차 처리 (병렬 처리 제거로 안정성 확보)
                for (int i = 0; i < idList.Count; i += batchSize)
                {
                    int currentBatchSize = Math.Min(batchSize, idList.Count - i);
                    var batchIds = idList.GetRange(i, currentBatchSize);

                    try
                    {
                        var batchFilter = Builders<MongoModels.RawDataDocument>.Filter.In(d => d.Id, batchIds);
                        var batchRawDatas = await rawDataRepo.FindDocumentsAsync(batchFilter);

                        foreach (var rawData in batchRawDatas)
                        {
                            if (rawData.Data != null)
                            {
                                mongoDataLookup[rawData.Id] = rawData.Data;
                            }
                        }

                        if (i % (batchSize * 5) == 0) // 매 5번째 배치마다 로깅
                        {
                            Debug.WriteLine($"MongoDB 배치 로드 진행: {i + currentBatchSize}/{idList.Count}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"배치 {i / batchSize + 1} 로드 오류: {ex.Message}");
                        // 배치 오류 시 다음 배치 계속 진행
                    }
                }

                return mongoDataLookup;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 데이터 로드 전체 오류: {ex.Message}");
                return new Dictionary<string, Dictionary<string, object>>();
            }
        }

        /// <summary>
        /// 안전한 데이터 복사 (NewRow() 대신 직접 구성)
        /// </summary>
        private DataTable CopyDataSafely(DataTable sourceTable, DataTable targetTable)
        {
            try
            {
                for (int i = 0; i < sourceTable.Rows.Count; i++)
                {
                    DataRow sourceRow = sourceTable.Rows[i];

                    // 값 배열 직접 구성 (NewRow() 사용 안함)
                    object[] rowValues = new object[targetTable.Columns.Count];

                    // 각 컬럼별로 안전하게 값 설정
                    for (int j = 0; j < targetTable.Columns.Count; j++)
                    {
                        string columnName = targetTable.Columns[j].ColumnName;

                        if (sourceTable.Columns.Contains(columnName))
                        {
                            object sourceValue = sourceRow[columnName];
                            rowValues[j] = sourceValue == null || sourceValue == DBNull.Value ?
                                           string.Empty : sourceValue.ToString();
                        }
                        else
                        {
                            rowValues[j] = string.Empty;
                        }
                    }

                    // 직접 행 추가 (NewRow() 대신)
                    targetTable.Rows.Add(rowValues);
                }

                Debug.WriteLine($"안전한 데이터 복사 완료: {targetTable.Rows.Count}행");
                return targetTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"안전한 데이터 복사 오류: {ex.Message}");
                return targetTable;
            }
        }

        /// <summary>
        /// 안전한 데이터 보강 (순차 처리)
        /// </summary>
        private async Task EnrichDataSafely(
            DataTable sourceTable,
            DataTable targetTable,
            Dictionary<string, Dictionary<string, object>> mongoLookup,
            List<string> visibleColumns,
            Dictionary<int, string> rowToIdMap)
        {
            try
            {
                Debug.WriteLine($"안전한 데이터 보강 시작: {sourceTable.Rows.Count}행");

                // 순차 처리로 안정성 확보 (병렬 처리 제거)
                for (int i = 0; i < sourceTable.Rows.Count; i++)
                {
                    try
                    {
                        DataRow sourceRow = sourceTable.Rows[i];

                        // 값 배열 직접 구성
                        object[] enrichedValues = new object[targetTable.Columns.Count];

                        // 1. 원본 데이터 복사
                        for (int j = 0; j < targetTable.Columns.Count; j++)
                        {
                            string columnName = targetTable.Columns[j].ColumnName;

                            if (sourceTable.Columns.Contains(columnName))
                            {
                                object sourceValue = sourceRow[columnName];
                                enrichedValues[j] = sourceValue == null || sourceValue == DBNull.Value ?
                                                   string.Empty : sourceValue.ToString();
                            }
                            else
                            {
                                enrichedValues[j] = string.Empty;
                            }
                        }

                        // 2. MongoDB 데이터로 보강
                        if (rowToIdMap.TryGetValue(i, out string rawDataId) &&
                            mongoLookup.TryGetValue(rawDataId, out var mongoData))
                        {
                            foreach (string visibleColumn in visibleColumns)
                            {
                                if (targetTable.Columns.Contains(visibleColumn) &&
                                    mongoData.TryGetValue(visibleColumn, out object mongoValue))
                                {
                                    int columnIndex = targetTable.Columns.IndexOf(visibleColumn);
                                    if (columnIndex >= 0 && columnIndex < enrichedValues.Length)
                                    {
                                        enrichedValues[columnIndex] = mongoValue == null ?
                                                                     string.Empty : mongoValue.ToString();
                                    }
                                }
                            }
                        }

                        // 직접 행 추가
                        targetTable.Rows.Add(enrichedValues);

                        // 진행 상황 로깅
                        if (i > 0 && i % 50000 == 0)
                        {
                            Debug.WriteLine($"데이터 보강 진행: {i}/{sourceTable.Rows.Count}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"행 {i} 보강 오류: {ex.Message}");

                        // 오류 시 기본 행 추가
                        try
                        {
                            object[] fallbackValues = new object[targetTable.Columns.Count];
                            for (int k = 0; k < fallbackValues.Length; k++)
                            {
                                fallbackValues[k] = string.Empty;
                            }
                            targetTable.Rows.Add(fallbackValues);
                        }
                        catch (Exception fallbackEx)
                        {
                            Debug.WriteLine($"대체 행 추가도 실패: {fallbackEx.Message}");
                        }
                    }
                }

                Debug.WriteLine($"안전한 데이터 보강 완료: {targetTable.Rows.Count}행");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"데이터 보강 전체 오류: {ex.Message}");
            }
        }

        private bool searchYN = false;
        private async Task create_merge_keyword_list(bool progressYN = false)
        {
            try
            {
                searchYN = true;

                if (progressYN)
                {
                    using (var progressForm = new ProcessProgressForm())
                    {
                        Debug.WriteLine("create_merge_keyword_list start ");
                        progressForm.Show();
                        await progressForm.UpdateProgressHandler(10, "키워드 요약 테이블 생성 중...");

                        await ProcessMergeKeywordListWithProgress(progressForm.UpdateProgressHandler);

                        await progressForm.UpdateProgressHandler(100, "완료");
                    }
                }
                else
                {
                    // 프로그레스 없이 진행
                    await ProcessMergeKeywordListWithProgress(null);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"키워드 리스트 생성 오류: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
            finally
            {
                Debug.WriteLine($"create_merge_keyword_list complete");
                searchYN = false;
            }
        }

        // 키워드 병합 처리 함수 (개선버전)
        // 키워드 병합 처리 함수 (개선버전 - 병렬 처리 적용)
        private async Task<(ConcurrentDictionary<string, int> keywordFrequency,
                   ConcurrentDictionary<string, ConcurrentBag<string>> keywordToRawDataIds)>
    ProcessKeywordsUltraSpeed(DataTable transformDataTable, List<string> keywordColumns)
        {
            try
            {
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 극한 속도 키워드 처리 시작: {transformDataTable.Rows.Count}행");

                // 극한 병렬 설정 - CPU 코어 수의 16배 (192GB RAM 활용)
                int extremeParallelism = Environment.ProcessorCount * 16; // 16코어 * 16 = 256 스레드
                const int ultraBatchSize = 50000; // 대용량 배치

                // 결과 저장용 스레드 안전 컬렉션
                var keywordFrequency = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var keywordToRawDataIds = new ConcurrentDictionary<string, ConcurrentBag<string>>(StringComparer.OrdinalIgnoreCase);

                // 1단계: 데이터를 메모리에 최적화하여 로드 (극한 메모리 사용)
                var rowDataCache = new UltraSpeedRowData[transformDataTable.Rows.Count];

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 메모리 캐싱 시작...");

                // 메모리 캐싱을 병렬로 수행 (극한 속도)
                await Task.Run(() =>
                {
                    Parallel.For(0, transformDataTable.Rows.Count,
                        new ParallelOptions { MaxDegreeOfParallelism = extremeParallelism },
                        i =>
                        {
                            try
                            {
                                var row = transformDataTable.Rows[i];
                                string rawDataId = row["raw_data_id"]?.ToString();

                                if (!string.IsNullOrEmpty(rawDataId))
                                {
                                    var keywords = new string[keywordColumns.Count];
                                    for (int j = 0; j < keywordColumns.Count; j++)
                                    {
                                        keywords[j] = row[keywordColumns[j]]?.ToString()?.Trim();
                                    }

                                    rowDataCache[i] = new UltraSpeedRowData
                                    {
                                        RawDataId = rawDataId,
                                        Keywords = keywords
                                    };
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 행 {i} 캐싱 오류: {ex.Message}");
                            }
                        });
                });

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 메모리 캐싱 완료");

                // 2단계: 배치별 극한 병렬 처리
                var batches = new List<UltraSpeedRowData[]>();

                for (int i = 0; i < rowDataCache.Length; i += ultraBatchSize)
                {
                    int batchSize = Math.Min(ultraBatchSize, rowDataCache.Length - i);
                    var batch = new UltraSpeedRowData[batchSize];
                    Array.Copy(rowDataCache, i, batch, 0, batchSize);
                    batches.Add(batch);
                }

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 극한 병렬 키워드 처리 시작: {batches.Count}개 배치");

                // 극한 병렬 배치 처리
                await Task.Run(() =>
                {
                    Parallel.ForEach(batches,
                        new ParallelOptions { MaxDegreeOfParallelism = extremeParallelism },
                        batch =>
                        {
                            try
                            {
                                // 배치별 로컬 결과 (메모리 효율성)
                                var localKeywordFreq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                                var localKeywordToIds = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

                                // 배치 내 데이터 처리 (극한 속도)
                                foreach (var rowData in batch)
                                {
                                    if (rowData.RawDataId == null) continue;

                                    foreach (var keyword in rowData.Keywords)
                                    {
                                        if (string.IsNullOrWhiteSpace(keyword)) continue;

                                        // 로컬 집계
                                        if (!localKeywordFreq.ContainsKey(keyword))
                                        {
                                            localKeywordFreq[keyword] = 0;
                                            localKeywordToIds[keyword] = new HashSet<string>();
                                        }

                                        localKeywordFreq[keyword]++;
                                        localKeywordToIds[keyword].Add(rowData.RawDataId);
                                    }
                                }

                                // 글로벌 결과에 병합 (스레드 안전)
                                foreach (var kvp in localKeywordFreq)
                                {
                                    keywordFrequency.AddOrUpdate(kvp.Key, kvp.Value, (k, v) => v + kvp.Value);
                                }

                                foreach (var kvp in localKeywordToIds)
                                {
                                    keywordToRawDataIds.AddOrUpdate(
                                        kvp.Key,
                                        new ConcurrentBag<string>(kvp.Value),
                                        (k, existingBag) =>
                                        {
                                            foreach (var id in kvp.Value)
                                            {
                                                existingBag.Add(id);
                                            }
                                            return existingBag;
                                        }
                                    );
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 배치 처리 오류: {ex.Message}");
                            }
                        });
                });

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 극한 속도 키워드 처리 완료: {keywordFrequency.Count}개 고유 키워드");

                return (keywordFrequency, keywordToRawDataIds);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 극한 속도 키워드 처리 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 극한 속도용 행 데이터 구조체 (메모리 효율성)
        /// </summary>
        private struct UltraSpeedRowData
        {
            public string RawDataId { get; set; }
            public string[] Keywords { get; set; }
        }

        /// <summary>
        /// ProcessMergeKeywordListWithProgress에서 기존 키워드 추출 부분을 이것으로 교체
        /// </summary>
        private async Task ProcessMergeKeywordListWithProgress(ProcessProgressForm.UpdateProgressDelegate progress)
        {
            try
            {
                // 진행 상황 업데이트 래퍼 함수
                async Task UpdateProgress(int percentage, string message = null)
                {
                    if (progress != null)
                    {
                        await progress(percentage, message);
                    }
                }

                await UpdateProgress(15, "키워드 데이터 로딩 중...");

                // 1. 키워드 데이터 확인
                if (transformDataTable == null || transformDataTable.Rows.Count == 0)
                {
                    Debug.WriteLine("데이터 테이블이 비어 있습니다.");
                    return;
                }

                // 2. 키워드 컬럼 식별 (Column0부터 시작하는 컬럼들)
                List<string> keywordColumns = new List<string>();
                foreach (DataColumn column in transformDataTable.Columns)
                {
                    if (column.ColumnName.StartsWith("Column") &&
                        int.TryParse(column.ColumnName.Substring(6), out int colIndex) &&
                        colIndex >= 0)
                    {
                        keywordColumns.Add(column.ColumnName);
                    }
                }

                Debug.WriteLine($"키워드 컬럼: {string.Join(", ", keywordColumns)}");

                if (keywordColumns.Count == 0)
                {
                    Debug.WriteLine("키워드 컬럼을 찾을 수 없습니다.");
                    return;
                }

                await UpdateProgress(20, "키워드 추출 중...");

                // 3. 극한 속도 키워드 처리 (기존 Parallel.ForEach 대체)
                var (keywordFrequency, keywordToRawDataIds) = await ProcessKeywordsUltraSpeed(transformDataTable, keywordColumns);
                
                await UpdateProgress(40, $"키워드별 금액 합산 중... ({keywordFrequency.Count}개 키워드)");

                // 4. 금액 정보를 극한 속도로 처리
                var rawDataToMoney = new ConcurrentDictionary<string, decimal>();

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 극한 속도 금액 정보 로드 시작: {DataHandler.moneyDataTable.Rows.Count}개 행");

                if (DataHandler.moneyDataTable != null && DataHandler.moneyDataTable.Rows.Count > 0)
                {
                    

                    // 금액 데이터를 극한 병렬 처리로 로드
                    await Task.Run(() =>
                    {
                        int extremeParallelism = Environment.ProcessorCount * 16;

                        Parallel.ForEach(DataHandler.moneyDataTable.AsEnumerable(),
                            new ParallelOptions { MaxDegreeOfParallelism = extremeParallelism },
                            moneyRow =>
                            {
                                try
                                {
                                    // raw_data_id 확인
                                    if (moneyRow.Table.Columns.Contains("raw_data_id") && moneyRow["raw_data_id"] != DBNull.Value)
                                    {
                                        string rawDataId = moneyRow["raw_data_id"].ToString();
                                        if (!string.IsNullOrEmpty(rawDataId))
                                        {
                                            // 금액 값 추출 (기존 로직 유지)
                                            object moneyValue = null;

                                            if (moneyRow.Table.Columns.Count > 1)
                                            {
                                                if (moneyRow.Table.Columns[0].ColumnName != "raw_data_id")
                                                {
                                                    moneyValue = moneyRow[0];
                                                }
                                                else if (moneyRow.Table.Columns.Count > 1)
                                                {
                                                    moneyValue = moneyRow[1];
                                                }
                                            }

                                            if (moneyValue == null || moneyValue == DBNull.Value)
                                            {
                                                string moneyColumnName = DataHandler.levelName[0];
                                                if (moneyRow.Table.Columns.Contains(moneyColumnName))
                                                {
                                                    moneyValue = moneyRow[moneyColumnName];
                                                }
                                            }

                                            if (moneyValue != null && moneyValue != DBNull.Value)
                                            {
                                                if (decimal.TryParse(moneyValue.ToString(), out decimal amount))
                                                {
                                                    rawDataToMoney.TryAdd(rawDataId, amount);
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 금액 처리 중 오류: {ex.Message}");
                                }
                            }
                        );
                    });

                    
                }

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 금액 정보 로드 완료: {rawDataToMoney.Count}개");

                await UpdateProgress(60, "키워드별 금액 합산 중...");

                // 5. 키워드별 금액 합산 (극한 병렬 처리)
                var keywordTotalMoney = new ConcurrentDictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

                await Task.Run(() =>
                {
                    int extremeParallelism = Environment.ProcessorCount * 16;

                    Parallel.ForEach(keywordToRawDataIds,
                        new ParallelOptions { MaxDegreeOfParallelism = extremeParallelism },
                        pair =>
                        {
                            try
                            {
                                string keyword = pair.Key;
                                var rawDataIds = pair.Value.Distinct().ToList(); // 중복 제거

                                decimal totalAmount = 0;
                                foreach (string rawDataId in rawDataIds)
                                {
                                    if (rawDataToMoney.TryGetValue(rawDataId, out decimal amount))
                                    {
                                        totalAmount += amount;
                                    }
                                }

                                keywordTotalMoney.TryAdd(keyword, totalAmount);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 키워드별 금액 합산 중 오류: {ex.Message}");
                            }
                        }
                    );
                });

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 키워드별 금액 합산 완료: {keywordTotalMoney.Count}개");

                await UpdateProgress(80, "요약 데이터 생성 중...");

                // 6. 나머지 로직은 기존과 동일 (결과 DataTable 생성 등)
                modifiedDataTable = new DataTable();
                modifiedDataTable.Columns.Add("Value", typeof(string));
                modifiedDataTable.Columns.Add("Count", typeof(int));
                modifiedDataTable.Columns.Add("합산금액", typeof(string));

                // 키워드 빈도 기준으로 정렬 (내림차순)
                var sortedKeywords = keywordFrequency.OrderByDescending(pair => pair.Value)
                                                    .ThenBy(pair => pair.Key);

                foreach (var pair in sortedKeywords)
                {
                    string keyword = pair.Key;
                    int count = pair.Value;
                    decimal totalMoney = keywordTotalMoney.TryGetValue(keyword, out decimal money) ? money : 0;

                    // 금액 포맷팅
                    string formattedMoney = FormatToKoreanUnit(totalMoney);

                    modifiedDataTable.Rows.Add(keyword, count, formattedMoney);
                }

                await UpdateProgress(90, "UI 업데이트 중...");

                // 7. UI 업데이트 (기존 로직과 동일)
                await Task.Run(() =>
                {
                    if (Application.OpenForms.Count > 0)
                    {
                        Application.OpenForms[0].Invoke((MethodInvoker)delegate
                        {
                            if (sum_keyword_table.Rows.Count > 0)
                            {
                                sum_keyword_table.Rows.Clear();
                                sum_keyword_table.Columns.Clear();
                            }

                            // 원본 DataTable의 컬럼들 추가
                            foreach (DataColumn col in modifiedDataTable.Columns)
                            {
                                sum_keyword_table.Columns.Add(col.ColumnName, col.ColumnName);
                            }

                            // 데이터 추가
                            foreach (DataRow row in modifiedDataTable.Rows)
                            {
                                int rowIndex = sum_keyword_table.Rows.Add();

                                // 데이터 채우기
                                for (int i = 0; i < modifiedDataTable.Columns.Count; i++)
                                {
                                    sum_keyword_table.Rows[rowIndex].Cells[i].Value = row[i];
                                }
                            }

                            // DataGridView 속성 설정
                            sum_keyword_table.AllowUserToAddRows = false;
                            sum_keyword_table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                            sum_keyword_table.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                            sum_keyword_table.Font = new System.Drawing.Font("Pretendard", 14.25F);

                            // Count 컬럼(1번 인덱스)에 천 단위 콤마 포맷팅 적용
                            if (sum_keyword_table.Columns.Count > 1)
                            {
                                sum_keyword_table.Columns[1].DefaultCellStyle.Format = "N0";
                            }

                            // 나머지 컬럼들은 읽기 전용으로 설정
                            for (int i = 1; i < sum_keyword_table.Columns.Count; i++)
                            {
                                sum_keyword_table.Columns[i].ReadOnly = true;
                            }
                        });
                    }
                });

                await UpdateProgress(100, "완료된 결과: " + modifiedDataTable.Rows.Count + "개 키워드");
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 극한 속도 키워드 요약 테이블 생성 완료: {modifiedDataTable.Rows.Count}개 키워드");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"키워드 분석 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }


        public async Task<DataTable> EnrichTransformDataWithRawData(DataTable transformDataTable)
        {
            try
            {
                // 원본 데이터를 수정하지 않도록 복사본 생성
                DataTable resultTable = new DataTable();

                // MongoDB 연결 확인
                await Data.MongoDBManager.Instance.EnsureInitializedAsync();

                // 1. is_visible=true인 컬럼 목록 가져오기
                var columnMappingFilter = Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("is_visible", true);
                var columnMappingsResult = await Data.MongoDBManager.Instance.FindDocumentsAsync<MongoDB.Bson.BsonDocument>(
                    "column_mapping",
                    columnMappingFilter);

                // 시각화될 컬럼명 추출
                List<string> visibleColumns = new List<string>();
                foreach (var doc in columnMappingsResult)
                {
                    if (doc.Contains("original_name"))
                    {
                        string originalName = doc["original_name"].AsString;
                        visibleColumns.Add(originalName);
                    }
                }

                Debug.WriteLine($"시각화될 컬럼: {string.Join(", ", visibleColumns)}");

                if (visibleColumns.Count == 0)
                {
                    Debug.WriteLine("표시할 컬럼이 없습니다. column_mapping 컬렉션의 is_visible 속성을 확인하세요.");
                    return transformDataTable.Copy();
                }

                // 2. 결과 테이블에 컬럼 구성
                // 먼저 visibleColumns 추가
                foreach (string column in visibleColumns)
                {
                    resultTable.Columns.Add(column, typeof(string));
                }

                // 그 다음 원본 transformDataTable의 컬럼 추가 (중복 제외)
                foreach (DataColumn column in transformDataTable.Columns)
                {
                    if (!resultTable.Columns.Contains(column.ColumnName))
                    {
                        resultTable.Columns.Add(column.ColumnName, column.DataType);
                    }
                }

                // 3. 원본 데이터의 모든 행 복사
                foreach (DataRow originalRow in transformDataTable.Rows)
                {
                    DataRow newRow = resultTable.NewRow();

                    // 원본 테이블의 모든 컬럼 값을 새 행에 복사
                    foreach (DataColumn column in transformDataTable.Columns)
                    {
                        if (resultTable.Columns.Contains(column.ColumnName))
                        {
                            newRow[column.ColumnName] = originalRow[column.ColumnName];
                        }
                    }

                    resultTable.Rows.Add(newRow);
                }

                // 4. raw_data_id 컬럼이 있는지 확인
                if (!resultTable.Columns.Contains("raw_data_id"))
                {
                    Debug.WriteLine("transformDataTable에 raw_data_id 컬럼이 없습니다.");
                    return resultTable;
                }

                // 5. RawData 저장소 생성
                var rawDataRepo = new Repositories.RawDataRepository();

                // 6. 모든 행의 raw_data_id 목록 수집
                HashSet<string> rawDataIds = new HashSet<string>();
                Dictionary<string, List<DataRow>> idToRowsMap = new Dictionary<string, List<DataRow>>();

                foreach (DataRow row in resultTable.Rows)
                {
                    if (row["raw_data_id"] != DBNull.Value)
                    {
                        string rawDataId = row["raw_data_id"].ToString();
                        if (!string.IsNullOrEmpty(rawDataId))
                        {
                            rawDataIds.Add(rawDataId);

                            if (!idToRowsMap.ContainsKey(rawDataId))
                            {
                                idToRowsMap[rawDataId] = new List<DataRow>();
                            }
                            idToRowsMap[rawDataId].Add(row);
                        }
                    }
                }

                if (rawDataIds.Count == 0)
                {
                    Debug.WriteLine("유효한 raw_data_id가 없습니다.");
                    return resultTable;
                }

                Debug.WriteLine($"보강할 raw_data_id: {rawDataIds.Count}개");

                // 7. 배치 처리로 원본 데이터 가져오기
                const int batchSize = 10000;
                List<string> idList = rawDataIds.ToList();

                // 안전한 배치 처리
                for (int i = 0; i < idList.Count; i += batchSize)
                {
                    int currentBatchSize = Math.Min(batchSize, idList.Count - i);
                    if (i >= idList.Count || currentBatchSize <= 0)
                        continue;

                    List<string> batchIds = idList.GetRange(i, currentBatchSize);

                    // MongoDB ID 형식으로 필터 생성
                    var batchFilter = Builders<MongoModels.RawDataDocument>.Filter.In(d => d.Id, batchIds);
                    var batchRawDatas = await rawDataRepo.FindDocumentsAsync(batchFilter);

                    // 조회된 데이터를 매핑
                    foreach (var rawData in batchRawDatas)
                    {
                        string id = rawData.Id;

                        if (idToRowsMap.ContainsKey(id) && rawData.Data != null)
                        {
                            foreach (DataRow resultRow in idToRowsMap[id])
                            {
                                foreach (string column in visibleColumns)
                                {
                                    if (rawData.Data.ContainsKey(column) && resultTable.Columns.Contains(column))
                                    {
                                        resultRow[column] = rawData.Data[column]?.ToString() ?? string.Empty;
                                    }
                                }
                            }
                        }
                    }
                }

                Debug.WriteLine("EnrichTransformDataWithRawData 완료");
                return resultTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 데이터 보강 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
                // 예외 발생 시 원본 데이터 테이블의 복사본 반환
                return transformDataTable.Copy();
            }
        }

        public DataTable FilterTransformDataByKeyword(DataTable viewTransformDataTable, DataTable originalTransformDataTable, string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
                return viewTransformDataTable.Copy();

            DataTable resultTable = viewTransformDataTable.Clone();

            // 원본 transformDataTable의 컬럼명 목록 가져오기
            List<string> originalColumnNames = new List<string>();
            foreach (DataColumn col in originalTransformDataTable.Columns)
            {
                originalColumnNames.Add(col.ColumnName);
            }
            Debug.WriteLine($"originalColumnNames  : {string.Join(',', originalColumnNames)}");

            // viewTransformDataTable의 각 행에 대해 검색
            for (int rowIndex = 0; rowIndex < viewTransformDataTable.Rows.Count; rowIndex++)
            {
                DataRow row = viewTransformDataTable.Rows[rowIndex];
                bool containsKeyword = false;

                // 원본 컬럼명에 해당하는 컬럼만 검사
                foreach (string colName in originalColumnNames)
                {
                    if (viewTransformDataTable.Columns.Contains(colName) &&
                        row[colName] != null &&
                        row[colName] != DBNull.Value)
                    {
                        string cellValue = row[colName].ToString();

                        if (cellValue.Equals(keyword, StringComparison.Ordinal))
                        {
                            containsKeyword = true;
                            break;
                        }
                    }
                }

                if (containsKeyword)
                {
                    resultTable.Rows.Add(row.ItemArray);
                }
            }

            return resultTable;
        }


        public void CreateFilteredDataGridView(DataGridView dgv, DataTable dt, List<string> filterWords)
        {
            // DataGridView 초기화
            dgv.DataSource = null;
            dgv.Rows.Clear();
            dgv.Columns.Clear();
            if (DataHandler.dragSelections.ContainsKey(dgv))
            {
                DataHandler.dragSelections[dgv].Clear();
            }

            // CheckBox 컬럼 추가
            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn();
            checkColumn.Name = "CheckBox";
            checkColumn.HeaderText = "";
            checkColumn.Width = 50;
            checkColumn.ThreeState = false;
            checkColumn.FillWeight = 20;  // 다른 컬럼들보다 작은 값 설정

            dgv.Columns.Add(checkColumn);


            // 원본 DataTable의 컬럼들 추가
            foreach (DataColumn col in dt.Columns)
            {
                dgv.Columns.Add(col.ColumnName, col.ColumnName);
            }


            // 데이터 필터링 및 추가
            foreach (DataRow row in dt.Rows)
            {
                if (filterWords.Count > 0)
                {
                    string firstColumnValue = row[0].ToString();

                    // list<string>의 항목과 비교
                    if (filterWords.Any(word => firstColumnValue.Contains(word)))
                    {
                        int rowIndex = dgv.Rows.Add();
                        dgv.Rows[rowIndex].Cells["CheckBox"].Value = false;  // 체크박스 초기값

                        // 데이터 채우기
                        for (int i = 0; i < dt.Columns.Count; i++)
                        {
                            dgv.Rows[rowIndex].Cells[i + 1].Value = row[i];  // +1은 체크박스 컬럼 때문

                        }

                    }
                }
                else
                {
                    int rowIndex = dgv.Rows.Add();
                    dgv.Rows[rowIndex].Cells["CheckBox"].Value = false;  // 체크박스 초기값

                    // 데이터 채우기
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        dgv.Rows[rowIndex].Cells[i + 1].Value = row[i];  // +1은 체크박스 컬럼 때문
                    }
                }

            }

            // DataGridView 속성 설정
            dgv.AllowUserToAddRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.ReadOnly = false;
            dgv.Columns["CheckBox"].ReadOnly = false;  // 체크박스 컬럼만 편집 가능
            dgv.Font = new System.Drawing.Font("Pretendard", 14.25F);


            //dgv.Columns[2].DefaultCellStyle.Format = "N0";
            //dgv.Columns[3].DefaultCellStyle.Format = "N0";

            //Debug.WriteLine($"dgv.Columns[1] : {dgv.Columns[1].Name}");
            //Debug.WriteLine($"dgv.Columns[2] : {dgv.Columns[2].Name}");

            // Count 컬럼(1번 인덱스)에 천 단위 콤마 포맷팅 적용
            if (dgv.Columns.Count > 2)
            {
                dgv.Columns[2].DefaultCellStyle.Format = "N0";
            }

            // 나머지 컬럼들은 읽기 전용으로 설정
            for (int i = 1; i < dgv.Columns.Count; i++)
            {
                dgv.Columns[i].ReadOnly = true;
            }


        }

        public string FormatToKoreanUnit(decimal number)
        {
            // 절대값으로 계산 후 나중에 부호 처리
            bool isNegative = number < 0;
            number = Math.Abs(number);


            string result;
            decimal divideNum = 0;


            divideNum = Math.Round(number / decimalDivider, 2);

            // 소수점 이하가 없는 경우 (정수인 경우)
            if (divideNum == Math.Truncate(divideNum))
            {
                result = string.Format("{0:N0}", divideNum) + " " + decimalDividerName;

            }
            // 소수점 둘째 자리가 0인 경우 (예: 10.5)
            else if (divideNum * 10 % 1 == 0)
            {
                result = string.Format("{0:N1}", divideNum) + " " + decimalDividerName;
            }
            //소수점 2째자리 표기
            else
            {
                result = string.Format("{0:N2}", divideNum) + " " + decimalDividerName;
            }




            // 음수 처리
            if (isNegative && divideNum != 0)
            {
                result = "-" + result;
            }

            return result;
        }


        private void keyword_search_button_Click(object sender, EventArgs e)
        {
            _ = DoKeywordSearchAsync(sender, e);
            
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

            return checkedData;
        }

        //데이터 치환 함수
        public void ReplaceDataTableValues(List<string> targetList, DataTable dt, string replaceText, int startColumnIndex)
        {
            // 모든 행 순회
            foreach (DataRow row in dt.Rows)
            {
                // startColumnIndex부터 마지막 컬럼까지만 순회
                for (int colIndex = startColumnIndex; colIndex < dt.Columns.Count; colIndex++)
                {
                    string currentValue = row[colIndex]?.ToString() ?? "";

                    // targetList의 문자열과 일치하는지 확인
                    if (targetList.Any(target => currentValue.Equals(target, StringComparison.OrdinalIgnoreCase)))
                    {
                        row[colIndex] = replaceText;
                    }
                }
            }
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

        // User Control을 Form으로 감싸서 보여주는 방법
        public void ShowUserControlAsDialog(UserControl userControl)
        {
            Debug.WriteLine("ShowUserControlAsDialog start");

            // 폼 생성 및 기본 설정
            Form form = new Form
            {
                Text = "Clustering 결과 확인",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                Size = new Size(1900, 1000), // 적절한 초기 크기 지정
                MinimizeBox = true,
                MaximizeBox = true
            };

            // 진행 상태 표시를 위한 컨트롤 추가
            Panel loadingPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.White
            };

            Label loadingLabel = new Label
            {
                Text = "데이터 렌더링 중...",
                Font = new System.Drawing.Font("Pretendard", 14),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            loadingPanel.Controls.Add(loadingLabel);
            form.Controls.Add(loadingPanel);

            // UserControl을 form에 추가하기 전에 먼저 폼 표시 (비모달 방식)
            Debug.WriteLine("ShowUserControlAsDialog - Show form");
            form.Show();

            // 백그라운드 작업으로 데이터 렌더링 및 컨트롤 초기화 완료
            Task.Run(() => {
                // 약간의 지연을 통해 로딩 메시지가 먼저 표시될 수 있도록 함
                Task.Delay(100).Wait();

                form.Invoke((MethodInvoker)delegate {
                    Debug.WriteLine("ShowUserControlAsDialog - Adding UserControl");

                    // 이미 초기화된 UserControl을 Form에 추가
                    userControl.Dock = DockStyle.Fill;
                    form.Controls.Add(userControl);

                    // 로딩 패널 제거
                    form.Controls.Remove(loadingPanel);
                    loadingPanel.Dispose();

                    // 필요시 폼 크기 조정
                    form.ClientSize = new Size(
                        Math.Min(Screen.PrimaryScreen.WorkingArea.Width - 100, userControl.Width),
                        Math.Min(Screen.PrimaryScreen.WorkingArea.Height - 100, userControl.Height)
                    );

                    Debug.WriteLine("ShowUserControlAsDialog - UserControl added and rendered");
                });
            });

            Debug.WriteLine("ShowUserControlAsDialog - immediate return");

            // 이 메서드는 즉시 반환됨 (비모달)
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
                    form.LoadUserControl(userControlHandler.uc_clustering);
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

        private void search_keyword_detail_list()
        {
            if (sum_keyword_table.SelectedCells.Count > 0)
            {
                int rowIndex = sum_keyword_table.SelectedCells[0].RowIndex;
                string keyword = sum_keyword_table.Rows[rowIndex].Cells[0].Value.ToString();

                DataTable filteredTable = FilterTransformDataByKeyword(viewTransformDataTable, transformDataTable, keyword);
                SetFullDataTable2nd(filteredTable); // 페이징 적용

                Debug.WriteLine($"키워드 '{keyword}'를 포함하는 행: {filteredTable.Rows.Count}개");
            }
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

        // 비동기 작업을 수행하는 내부 함수
        private async Task DoKeywordSearchAsync(object sender, EventArgs e)
        {

            //searchYN =true 이면 대기
            while (searchYN)
            {
                await Task.Delay(10);
            }
            string target_keyword = "";

           
            if (!"".Equals(search_keyword.Text.ToString()) && search_keyword.Text != null)
            {
                target_keyword = search_keyword.Text.ToString();
            }

            Debug.WriteLine($"검색 키워드 target_keyword : {target_keyword}");

            //List<string> lowlevelList = DataHandler.GetColumnValuesAsList(DataHandler.lowLevelData, 0);
            List<string> valuelList = DataHandler.GetColumnValuesAsList(modifiedDataTable, 0);

            List<string> MathcingPairs = new List<string>();

            if (!"".Equals(target_keyword))
            {
                //MathcingPairs = DataHandler.FindMachKeyword(valuelList, target_keyword);

                // 개선된 코드:
                MathcingPairs = FindImprovedKeywordMatches(valuelList, target_keyword);

                Debug.WriteLine($"MathcingPairs.Count : {MathcingPairs.Count}");
                if (MathcingPairs.Count == 0)
                {
                    match_keyword_table.DataSource = null;
                    match_keyword_table.Rows.Clear();
                    match_keyword_table.Columns.Clear();
                    if (DataHandler.dragSelections.ContainsKey(match_keyword_table))
                    {
                        DataHandler.dragSelections[match_keyword_table].Clear();
                    }
                    return;
                }
            }

            
            CreateFilteredDataGridView(match_keyword_table, modifiedDataTable, MathcingPairs);
            
            // 마지막 부분만 수정:
            //DataTable filteredTable = FilterTransformDataByKeyword(viewTransformDataTable, transformDataTable, target_keyword);
            //SetFullDataTable2nd(filteredTable); // 페이징 적용

            check_all_keyword_list.Checked = false;

            //modified_keyword.Text = target_keyword;
        }

        /// <summary>
        /// 개선된 키워드 검색 함수 (영어 대소문자 무시 + 2글자 기준 매칭)
        /// </summary>
        private List<string> FindImprovedKeywordMatches(List<string> keywordList, string searchKeyword)
        {
            if (string.IsNullOrEmpty(searchKeyword) || keywordList == null || keywordList.Count == 0)
            {
                return new List<string>();
            }

            var matchingKeywords = new List<string>();
            bool isEnglishSearch = IsEnglishText(searchKeyword);

            Debug.WriteLine($"검색어 '{searchKeyword}' - 영어 검색: {isEnglishSearch}");

            foreach (string keyword in keywordList)
            {
                if (string.IsNullOrEmpty(keyword)) continue;

                bool isMatch = false;

                if (isEnglishSearch)
                {
                    // 영어인 경우: 대소문자 무시 + 2글자 기준 매칭
                    if (searchKeyword.Length >= 2)
                    {
                        isMatch = CompareByTwoCharsIgnoreCase(searchKeyword, keyword) ||
                                 keyword.ToUpper().Contains(searchKeyword.ToUpper());
                    }
                    else
                    {
                        // 1글자인 경우 대소문자 무시 Contains
                        isMatch = keyword.ToUpper().Contains(searchKeyword.ToUpper());
                    }
                }
                else
                {
                    // 한글인 경우: 기존 로직 + 2글자 기준 매칭
                    if (searchKeyword.Length >= 2)
                    {
                        isMatch = CompareByTwoChars(searchKeyword, keyword) ||
                                 keyword.Contains(searchKeyword);
                    }
                    else
                    {
                        // 1글자인 경우 기존 Contains
                        isMatch = keyword.Contains(searchKeyword);
                    }
                }

                if (isMatch)
                {
                    matchingKeywords.Add(keyword);
                }
            }

            Debug.WriteLine($"매칭된 키워드 수: {matchingKeywords.Count}");
            return matchingKeywords;
        }

        /// <summary>
        /// 영어 텍스트인지 확인
        /// </summary>
        private bool IsEnglishText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            // 영어 알파벳이 하나라도 있으면 영어로 판단
            bool hasEnglish = text.Any(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'));
            return hasEnglish;
        }

        /// <summary>
        /// 2글자 기준 비교 로직 (대소문자 무시)
        /// </summary>
        private bool CompareByTwoCharsIgnoreCase(string baseWord, string targetWord)
        {
            if (targetWord.Length < 2) return false;
            if (baseWord.Length < 2) return targetWord.ToUpper().Contains(baseWord.ToUpper());

            // 기준 단어를 2글자씩 자르기 (대문자 변환)
            var baseParts = new List<string>();
            string upperBaseWord = baseWord.ToUpper();
            for (int i = 0; i < upperBaseWord.Length - 1; i++)
            {
                baseParts.Add(upperBaseWord.Substring(i, 2));
            }

            // 대상 단어를 2글자씩 자르기 (대문자 변환)
            var targetParts = new List<string>();
            string upperTargetWord = targetWord.ToUpper();
            for (int i = 0; i < upperTargetWord.Length - 1; i++)
            {
                targetParts.Add(upperTargetWord.Substring(i, 2));
            }

            // 공통된 2글자 조합 확인
            return baseParts.Any(b => targetParts.Contains(b));
        }

        /// <summary>
        /// 2글자 기준 비교 로직 (한글용 - 기존 방식)
        /// </summary>
        private bool CompareByTwoChars(string baseWord, string targetWord)
        {
            if (targetWord.Length < 2) return false;
            if (baseWord.Length < 2) return targetWord.Contains(baseWord);

            // 기준 단어를 2글자씩 자르기
            var baseParts = new List<string>();
            for (int i = 0; i < baseWord.Length - 1; i++)
            {
                baseParts.Add(baseWord.Substring(i, 2));
            }

            // 대상 단어를 2글자씩 자르기
            var targetParts = new List<string>();
            for (int i = 0; i < targetWord.Length - 1; i++)
            {
                targetParts.Add(targetWord.Substring(i, 2));
            }

            // 공통된 2글자 조합 확인
            return baseParts.Any(b => targetParts.Contains(b));
        }



    }

}
