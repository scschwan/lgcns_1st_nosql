// =====================================
// 1계층: 데이터 관리자 (ClusterDataManager)
// =====================================
using FinanceTool.Data;
using System.Data;
using System.Diagnostics;

public class ClusterDataManager
{
    private DataTable _fullClusterData;
    private Dictionary<string, HashSet<int>> _keywordIndex;
    private Dictionary<string, HashSet<int>> _supplierIndex;
    private Dictionary<int, DataRow> _clusterRowIndex;
    private readonly object _lockObject = new object();

    public DataTable FullData => _fullClusterData;
    public int TotalCount => _fullClusterData?.Rows.Count ?? 0;

    /// <summary>
    /// 메모리에 전체 클러스터 데이터 로딩 및 인덱스 구축
    /// </summary>
    public async Task LoadAndIndexDataAsync(DataTable clusterData)
    {
        await Task.Run(() =>
        {
            lock (_lockObject)
            {
                _fullClusterData = clusterData.Copy();
                BuildSearchIndexes();
            }
        });
    }

    /// <summary>
    /// 고속 검색을 위한 인덱스 구축 (병렬 처리)
    /// </summary>
    private void BuildSearchIndexes()
    {
        _keywordIndex = new Dictionary<string, HashSet<int>>();
        _supplierIndex = new Dictionary<string, HashSet<int>>();
        _clusterRowIndex = new Dictionary<int, DataRow>();

        var lockKeyword = new object();
        var lockSupplier = new object();
        var lockRow = new object();

        // 병렬 처리로 인덱스 구축 (16코어 CPU 활용)
        Parallel.ForEach(_fullClusterData.AsEnumerable(), new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        }, row =>
        {
            if (!int.TryParse(row["ID"]?.ToString(), out int clusterId)) return;

            // 행 인덱스 구축
            lock (lockRow)
            {
                _clusterRowIndex[clusterId] = row;
            }

            // 키워드 인덱스 구축
            string keywords = row["키워드목록"]?.ToString() ?? "";
            if (!string.IsNullOrEmpty(keywords))
            {
                var keywordList = keywords.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k));
                foreach (string keyword in keywordList)
                {
                    lock (lockKeyword)
                    {
                        if (!_keywordIndex.ContainsKey(keyword))
                            _keywordIndex[keyword] = new HashSet<int>();
                        _keywordIndex[keyword].Add(clusterId);
                    }
                }
            }

            // 공급업체 인덱스 구축
            string supplier = row[DataHandler.prod_col_name]?.ToString() ?? "";
            if (!string.IsNullOrEmpty(supplier))
            {
                var supplierList = supplier.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
                foreach (string sup in supplierList)
                {
                    lock (lockSupplier)
                    {
                        if (!_supplierIndex.ContainsKey(sup))
                            _supplierIndex[sup] = new HashSet<int>();
                        _supplierIndex[sup].Add(clusterId);
                    }
                }
            }
        });

        Debug.WriteLine($"인덱스 구축 완료 - 키워드: {_keywordIndex.Count}개, 공급업체: {_supplierIndex.Count}개, 클러스터: {_clusterRowIndex.Count}개");
    }

    /// <summary>
    /// 모든 클러스터 ID 조회
    /// </summary>
    public HashSet<int> GetAllClusterIds()
    {
        return new HashSet<int>(_clusterRowIndex.Keys);
    }

   

    /// <summary>
    /// 키워드 기반 클러스터 ID 고속 검색
    /// </summary>
    public HashSet<int> GetClusterIdsByKeywords(List<string> keywords, bool exactMatch = false)
    {
        if (keywords == null || keywords.Count == 0)
            return new HashSet<int>(_clusterRowIndex.Keys);

        HashSet<int> result = null;

        foreach (string keyword in keywords)
        {
            HashSet<int> keywordMatches = new HashSet<int>();

            if (exactMatch)
            {
                // 정확한 키워드 매칭
                if (_keywordIndex.TryGetValue(keyword, out HashSet<int> exactIds))
                {
                    keywordMatches = exactIds;
                }
            }
            else
            {
                // 부분 매칭 (Contains)
                var matchingKeys = _keywordIndex.Keys.Where(k => k.Contains(keyword));
                foreach (string matchKey in matchingKeys)
                {
                    keywordMatches.UnionWith(_keywordIndex[matchKey]);
                }
            }

            result = result == null ? keywordMatches : result.Intersect(keywordMatches).ToHashSet();

            if (result.Count == 0) break; // 교집합이 없으면 조기 종료
        }

        return result ?? new HashSet<int>();
    }

    /// <summary>
    /// 공급업체 기반 클러스터 ID 고속 검색
    /// </summary>
    public HashSet<int> GetClusterIdsBySupplier(List<string> suppliers)
    {
        if (suppliers == null || suppliers.Count == 0)
            return new HashSet<int>(_clusterRowIndex.Keys);

        HashSet<int> result = new HashSet<int>();

        foreach (string supplier in suppliers)
        {
            var matchingKeys = _supplierIndex.Keys.Where(k => k.Contains(supplier));
            foreach (string matchKey in matchingKeys)
            {
                result.UnionWith(_supplierIndex[matchKey]);
            }
        }

        return result;
    }

    /// <summary>
    /// 클러스터 ID로 DataRow 조회
    /// </summary>
    public DataRow GetClusterRow(int clusterId)
    {
        return _clusterRowIndex.TryGetValue(clusterId, out DataRow row) ? row : null;
    }

    /// <summary>
    /// 제외 키워드 적용
    /// </summary>
    public HashSet<int> ExcludeByKeywords(HashSet<int> clusterIds, List<string> excludeKeywords)
    {
        if (excludeKeywords == null || excludeKeywords.Count == 0) return clusterIds;

        HashSet<int> excludeIds = new HashSet<int>();
        foreach (string excludeKeyword in excludeKeywords)
        {
            var matchingKeys = _keywordIndex.Keys.Where(k => k.Contains(excludeKeyword));
            foreach (string matchKey in matchingKeys)
            {
                excludeIds.UnionWith(_keywordIndex[matchKey]);
            }
        }

        return clusterIds.Except(excludeIds).ToHashSet();
    }
}

// =====================================
// 2계층: 검색/필터링 엔진 (ClusterSearchEngine)
// =====================================
public class ClusterSearchEngine
{
    private readonly ClusterDataManager _dataManager;

    public ClusterSearchEngine(ClusterDataManager dataManager)
    {
        _dataManager = dataManager;
    }

    /// <summary>
    /// 통합 검색 실행 (메모리 기반 고속 처리)
    /// </summary>
    public async Task<SearchResult> ExecuteSearchAsync(SearchCriteria criteria)
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                HashSet<int> candidateIds = null;

                // 1. 키워드 검색
                if (criteria.IsKeywordSearch && criteria.Keywords?.Count > 0)
                {
                    candidateIds = _dataManager.GetClusterIdsByKeywords(criteria.Keywords, criteria.ExactMatch);
                }
                // 2. 공급업체 검색
                else if (criteria.IsSupplierSearch && criteria.Keywords?.Count > 0)
                {
                    candidateIds = _dataManager.GetClusterIdsBySupplier(criteria.Keywords);
                }
                // 3. 전체 검색
                else
                {
                    //candidateIds = new HashSet<int>(_dataManager._clusterRowIndex.Keys);
                    candidateIds = _dataManager.GetAllClusterIds();
                }

                // 4. 제외 키워드 적용
                if (criteria.ExcludeKeywords?.Count > 0)
                {
                    candidateIds = _dataManager.ExcludeByKeywords(candidateIds, criteria.ExcludeKeywords);
                }

                // 5. 병합 상태 필터링 (병렬 처리)
                var filteredIds = candidateIds.AsParallel()
                    .Where(id =>
                    {
                        var row = _dataManager.GetClusterRow(id);
                        if (row == null) return false;

                        // ClusterID 조건 확인
                        if (!row.IsNull("ClusterID") && !row.IsNull("ID"))
                        {
                            int clusterId = Convert.ToInt32(row["ClusterID"]);
                            int rowId = Convert.ToInt32(row["ID"]);

                            // *** 수정된 조건: 병합되지 않은 클러스터만 표시 ***
                            // ClusterID가 -1이거나, ClusterID와 ID가 다르면서 ClusterID < 0인 경우만 포함
                            return clusterId == -1 || (clusterId != rowId && clusterId < 0);
                        }

                        // ClusterID나 ID가 null인 경우는 제외
                        return false;
                    })
                    .ToList();

                // 6. 결과 DataTable 생성
                DataTable resultTable = CreateResultDataTable(filteredIds);

                stopwatch.Stop();

                return new SearchResult
                {
                    Data = resultTable,
                    TotalCount = filteredIds.Count,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    SearchCriteria = criteria
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"검색 실행 오류: {ex.Message}");
                return new SearchResult
                {
                    Data = _dataManager.FullData.Clone(),
                    TotalCount = 0,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    Error = ex.Message
                };
            }
        });
    }

    /// <summary>
    /// 검색 결과를 DataTable로 변환
    /// </summary>
    private DataTable CreateResultDataTable(List<int> clusterIds)
    {
        if (_dataManager.FullData == null) return new DataTable();

        DataTable resultTable = _dataManager.FullData.Clone();

        // 병렬 처리로 행 추가 준비
        var rows = clusterIds.AsParallel()
            .Select(id => _dataManager.GetClusterRow(id))
            .Where(row => row != null)
            .OrderByDescending(row => Convert.ToInt32(row["ID"]))
            .ToList();

        // 결과 테이블에 행 추가 (순차 처리 - DataTable 스레드 안전성)
        foreach (var row in rows)
        {
            resultTable.ImportRow(row);
        }

        return resultTable;
    }
}

// =====================================
// 3계층: UI 표시 관리자 (ClusterDisplayManager)
// =====================================
public class ClusterDisplayManager
{
    private DataTable _currentSearchResult;
    private int _currentPage = 1;
    private int _pageSize = 1000;
    private int _totalPages = 1;
    private HashSet<int> _selectedClusterIds = new HashSet<int>();

    // 페이징 컨트롤 참조
    private DataGridView _targetGrid;
    private NumericUpDown _pageNumberControl;
    private ComboBox _pageSizeControl;
    private Button _prevButton;
    private Button _nextButton;
    private Label _paginationLabel;
    private CheckBox _selectAllCheckbox;

    public int CurrentPage => _currentPage;
    public int TotalPages => _totalPages;
    public int TotalRecords => _currentSearchResult?.Rows.Count ?? 0;
    public HashSet<int> SelectedClusterIds => new HashSet<int>(_selectedClusterIds);

    /// <summary>
    /// 선택 목록에 클러스터 ID 추가
    /// </summary>
    public void AddToSelection(int clusterId)
    {
        _selectedClusterIds.Add(clusterId);
    }

    /// <summary>
    /// 선택 목록에서 클러스터 ID 제거
    /// </summary>
    public void RemoveFromSelection(int clusterId)
    {
        _selectedClusterIds.Remove(clusterId);
    }

    /// <summary>
    /// UI 컨트롤 초기화
    /// </summary>
    public void Initialize(DataGridView grid, NumericUpDown pageNum, ComboBox pageSize,
                          Button prevBtn, Button nextBtn, Label paginationLbl, CheckBox selectAll)
    {
        _targetGrid = grid;
        _pageNumberControl = pageNum;
        _pageSizeControl = pageSize;
        _prevButton = prevBtn;
        _nextButton = nextBtn;
        _paginationLabel = paginationLbl;
        _selectAllCheckbox = selectAll;

        SetupEventHandlers();
        InitializePaginationControls();
    }

    /// <summary>
    /// 현재 검색 결과의 모든 클러스터 ID 조회
    /// </summary>
    public List<int> GetCurrentResultClusterIds()
    {
        if (_currentSearchResult == null) return new List<int>();

        List<int> clusterIds = new List<int>();
        foreach (DataRow row in _currentSearchResult.Rows)
        {
            if (int.TryParse(row["ID"]?.ToString(), out int clusterId))
            {
                clusterIds.Add(clusterId);
            }
        }
        return clusterIds;
    }

   

    /// <summary>
    /// 검색 결과 표시 (페이징 적용)
    /// </summary>
    public async Task DisplaySearchResultAsync(SearchResult searchResult)
    {
        await Task.Run(() =>
        {
            Application.OpenForms[0]?.Invoke((MethodInvoker)(() =>
            {
                try
                {
                    _currentSearchResult = searchResult.Data;
                    _currentPage = 1;
                    _selectedClusterIds.Clear();

                    if (_currentSearchResult != null && _currentSearchResult.Rows.Count > 0)
                    {
                        _totalPages = (int)Math.Ceiling((double)_currentSearchResult.Rows.Count / _pageSize);
                        EnablePaginationControls(true);
                    }
                    else
                    {
                        _totalPages = 1;
                        EnablePaginationControls(false);
                    }

                    DisplayCurrentPage();
                    UpdatePaginationInfo();

                    Debug.WriteLine($"검색 결과 표시 완료: {searchResult.TotalCount}건, {searchResult.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"검색 결과 표시 오류: {ex.Message}");
                }
            }));
        });
    }

    /// <summary>
    /// 현재 페이지 데이터 표시
    /// </summary>
    private void DisplayCurrentPage()
    {
        if (_currentSearchResult == null || _targetGrid == null) return;

        int startIndex = (_currentPage - 1) * _pageSize;
        int endIndex = Math.Min(startIndex + _pageSize, _currentSearchResult.Rows.Count);

        // 그리드 초기화
        _targetGrid.DataSource = null;
        _targetGrid.Rows.Clear();
        _targetGrid.Columns.Clear();

        if (DataHandler.dragSelections.ContainsKey(_targetGrid))
        {
            DataHandler.dragSelections[_targetGrid].Clear();
        }

        // 체크박스 컬럼 추가
        DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn()
        {
            Name = "CheckBox",
            HeaderText = "",
            Width = 50,
            ThreeState = false,
            Frozen = true,
            FillWeight = 20
        };
        _targetGrid.Columns.Add(checkColumn);

        // 원본 컬럼들 추가
        foreach (DataColumn col in _currentSearchResult.Columns)
        {
            _targetGrid.Columns.Add(col.ColumnName, col.ColumnName);
        }

        // 현재 페이지 데이터 추가
        for (int i = startIndex; i < endIndex; i++)
        {
            if (i >= _currentSearchResult.Rows.Count) break;

            DataRow sourceRow = _currentSearchResult.Rows[i];
            int rowIndex = _targetGrid.Rows.Add();

            // 체크박스 상태 복원
            if (int.TryParse(sourceRow["ID"]?.ToString(), out int clusterId))
            {
                _targetGrid.Rows[rowIndex].Cells["CheckBox"].Value = _selectedClusterIds.Contains(clusterId);
            }

            // 데이터 복사
            for (int colIndex = 0; colIndex < _currentSearchResult.Columns.Count; colIndex++)
            {
                string columnName = _currentSearchResult.Columns[colIndex].ColumnName;
                object value = sourceRow[colIndex];

                // 금액 포맷팅
                if ("합산금액".Equals(columnName) && decimal.TryParse(value?.ToString(), out decimal amount))
                {
                    _targetGrid.Rows[rowIndex].Cells[colIndex + 1].Value = FormatToKoreanUnit(amount);
                }
                else
                {
                    _targetGrid.Rows[rowIndex].Cells[colIndex + 1].Value = value;
                }
            }
        }

        // 그리드 설정 적용
        ApplyGridSettings();
        UpdateSelectAllCheckbox();
    }

    /// <summary>
    /// 선택 상태 저장
    /// </summary>
    public void SaveCurrentSelectionState()
    {
        if (_targetGrid == null) return;

        foreach (DataGridViewRow row in _targetGrid.Rows)
        {
            if (row.Cells["CheckBox"].Value != null && Convert.ToBoolean(row.Cells["CheckBox"].Value))
            {
                if (int.TryParse(row.Cells["ID"]?.Value?.ToString(), out int clusterId))
                {
                    _selectedClusterIds.Add(clusterId);
                }
            }
            else
            {
                if (int.TryParse(row.Cells["ID"]?.Value?.ToString(), out int clusterId))
                {
                    _selectedClusterIds.Remove(clusterId);
                }
            }
        }
    }

    /// <summary>
    /// 페이지 이동
    /// </summary>
    public async Task NavigateToPageAsync(int pageNumber)
    {
        if (pageNumber < 1 || pageNumber > _totalPages || pageNumber == _currentPage) return;

        SaveCurrentSelectionState();
        _currentPage = pageNumber;

        await Task.Run(() =>
        {
            Application.OpenForms[0]?.Invoke((MethodInvoker)(() =>
            {
                DisplayCurrentPage();
                UpdatePaginationInfo();
            }));
        });
    }

    /// <summary>
    /// 페이지 크기 변경
    /// </summary>
    public async Task ChangePageSizeAsync(int newPageSize)
    {
        SaveCurrentSelectionState();
        _pageSize = newPageSize;
        _currentPage = 1;

        if (_currentSearchResult != null)
        {
            _totalPages = (int)Math.Ceiling((double)_currentSearchResult.Rows.Count / _pageSize);
        }

        await Task.Run(() =>
        {
            Application.OpenForms[0]?.Invoke((MethodInvoker)(() =>
            {
                DisplayCurrentPage();
                UpdatePaginationInfo();
            }));
        });
    }

    // 이벤트 핸들러 및 기타 UI 관련 메서드들...
    private void SetupEventHandlers()
    {
        if (_pageNumberControl != null)
            _pageNumberControl.ValueChanged += async (s, e) => await NavigateToPageAsync((int)_pageNumberControl.Value);

        if (_pageSizeControl != null)
            _pageSizeControl.SelectedIndexChanged += async (s, e) =>
            {
                if (int.TryParse(_pageSizeControl.SelectedItem?.ToString(), out int newSize))
                    await ChangePageSizeAsync(newSize);
            };

        if (_prevButton != null)
            _prevButton.Click += async (s, e) => await NavigateToPageAsync(_currentPage - 1);

        if (_nextButton != null)
            _nextButton.Click += async (s, e) => await NavigateToPageAsync(_currentPage + 1);

        if (_selectAllCheckbox != null)
            _selectAllCheckbox.CheckedChanged += HandleSelectAllChanged;

        if (_targetGrid != null)
            _targetGrid.CellContentClick += HandleCellContentClick;
    }

    private void HandleSelectAllChanged(object sender, EventArgs e)
    {
        if (_selectAllCheckbox == null || _targetGrid == null) return;

        bool selectAll = _selectAllCheckbox.Checked;

        foreach (DataGridViewRow row in _targetGrid.Rows)
        {
            row.Cells["CheckBox"].Value = selectAll;
        }

        SaveCurrentSelectionState();
    }

    private void HandleCellContentClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex == 0 && e.RowIndex >= 0) // 체크박스 컬럼
        {
            SaveCurrentSelectionState();
            UpdateSelectAllCheckbox();
        }
    }

    private void UpdateSelectAllCheckbox()
    {
        if (_selectAllCheckbox == null || _targetGrid == null) return;

        int checkedCount = 0;
        int totalCount = _targetGrid.Rows.Count;

        foreach (DataGridViewRow row in _targetGrid.Rows)
        {
            if (row.Cells["CheckBox"].Value != null && Convert.ToBoolean(row.Cells["CheckBox"].Value))
                checkedCount++;
        }

        _selectAllCheckbox.CheckedChanged -= HandleSelectAllChanged;
        _selectAllCheckbox.Checked = checkedCount == totalCount && totalCount > 0;
        _selectAllCheckbox.CheckedChanged += HandleSelectAllChanged;
    }

    private void UpdatePaginationInfo()
    {
        if (_paginationLabel != null)
        {
            _paginationLabel.Text = $"/ {_totalPages} (총 {TotalRecords:N0}개)";
        }

        if (_pageNumberControl != null)
        {
            _pageNumberControl.Maximum = Math.Max(1, _totalPages);
            if (_pageNumberControl.Value != _currentPage)
                _pageNumberControl.Value = _currentPage;
        }

        if (_prevButton != null)
            _prevButton.Enabled = _currentPage > 1;

        if (_nextButton != null)
            _nextButton.Enabled = _currentPage < _totalPages;
    }

    private void EnablePaginationControls(bool enabled)
    {
        if (_prevButton != null) _prevButton.Enabled = enabled;
        if (_nextButton != null) _nextButton.Enabled = enabled;
        if (_pageNumberControl != null) _pageNumberControl.Enabled = enabled;
        if (_pageSizeControl != null) _pageSizeControl.Enabled = enabled;
    }

    private void InitializePaginationControls()
    {
        if (_pageSizeControl != null)
        {
            _pageSizeControl.Items.Clear();
            _pageSizeControl.Items.AddRange(new object[] { 100, 200, 500, 1000, 2000 });
            _pageSizeControl.SelectedItem = _pageSize;
            _pageSizeControl.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        if (_pageNumberControl != null)
        {
            _pageNumberControl.Minimum = 1;
        }

        EnablePaginationControls(false);
    }

    private void ApplyGridSettings()
    {
        if (_targetGrid == null) return;

        // 컬럼 숨김 처리
        if (_targetGrid.Columns["ID"] != null) _targetGrid.Columns["ID"].Visible = false;
        if (_targetGrid.Columns["ClusterID"] != null) _targetGrid.Columns["ClusterID"].Visible = false;
        if (_targetGrid.Columns["dataIndex"] != null) _targetGrid.Columns["dataIndex"].Visible = false;
        if (_targetGrid.Columns["import_date"] != null) _targetGrid.Columns["import_date"].Visible = false;

        // 숫자 포맷 설정
        if (_targetGrid.Columns["Count"] != null)
        {
            _targetGrid.Columns["Count"].DefaultCellStyle.Format = "N0";
            _targetGrid.Columns["Count"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        // 기본 설정
        _targetGrid.AllowUserToAddRows = false;
        _targetGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _targetGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _targetGrid.ReadOnly = false;
        _targetGrid.Font = new Font("맑은 고딕", 9F);

        // 체크박스 컬럼만 편집 가능
        _targetGrid.Columns["CheckBox"].ReadOnly = false;
        for (int i = 1; i < _targetGrid.Columns.Count; i++)
        {
            _targetGrid.Columns[i].ReadOnly = true;
        }

        // 컬럼 너비 설정
        if (_targetGrid.Columns["클러스터명"] != null)
        {
            _targetGrid.Columns["클러스터명"].Width = 400;
            _targetGrid.Columns["클러스터명"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        }
    }


    private decimal _decimalDivider = 1;
    private string _decimalDividerName = "원";

    // 통화 포맷 업데이트 메서드 추가
    public void UpdateCurrencyFormat(decimal divider, string unitName)
    {
        _decimalDivider = divider;
        _decimalDividerName = unitName;
    }

    // FormatToKoreanUnit 함수 개선
    private string FormatToKoreanUnit(decimal amount)
    {
        if (_decimalDivider <= 1)
            return amount.ToString("N0") + _decimalDividerName;

        decimal dividedAmount = amount / _decimalDivider;
        return dividedAmount.ToString("N0") + _decimalDividerName;
    }

    // 현재 표시 새로고침 메서드 추가
    public void RefreshCurrentDisplay()
    {
        if (_currentSearchResult != null)
        {
            DisplayCurrentPage();
        }
    }
}

// =====================================
// 지원 클래스들
// =====================================
public class SearchCriteria
{
    public List<string> Keywords { get; set; } = new List<string>();
    public List<string> ExcludeKeywords { get; set; } = new List<string>();
    public bool IsKeywordSearch { get; set; } = true;
    public bool IsSupplierSearch { get; set; } = false;
    public bool ExactMatch { get; set; } = false;
    public bool AndSearch { get; set; } = false;
}

public class SearchResult
{
    public DataTable Data { get; set; }
    public int TotalCount { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public SearchCriteria SearchCriteria { get; set; }
    public string Error { get; set; }
}

// =====================================
// 통합 관리자 (Facade 패턴)
// =====================================
public class ClusteringManager
{
    private ClusterDataManager _dataManager;
    private ClusterSearchEngine _searchEngine;
    private ClusterDisplayManager _displayManager;

    public ClusteringManager()
    {
        _dataManager = new ClusterDataManager();
        _searchEngine = new ClusterSearchEngine(_dataManager);
        _displayManager = new ClusterDisplayManager();
    }

    // 통화 포맷 업데이트 래퍼 메서드
    public void UpdateCurrencyFormat(decimal divider, string unitName)
    {
        _displayManager.UpdateCurrencyFormat(divider, unitName);
    }

    // 현재 표시 새로고침 래퍼 메서드
    public void RefreshCurrentDisplay()
    {
        _displayManager.RefreshCurrentDisplay();
    }

    /// <summary>
    /// 선택 목록에 클러스터 ID 추가
    /// </summary>
    public void AddToSelection(int clusterId)
    {
        _displayManager.AddToSelection(clusterId);
    }

    /// <summary>
    /// 선택 목록에서 클러스터 ID 제거
    /// </summary>
    public void RemoveFromSelection(int clusterId)
    {
        _displayManager.RemoveFromSelection(clusterId);
    }

    /// <summary>
    /// 초기화 (UI 컨트롤과 데이터 로딩)
    /// </summary>
    public async Task InitializeAsync(DataTable clusterData, DataGridView grid,
                                    NumericUpDown pageNum, ComboBox pageSize,
                                    Button prevBtn, Button nextBtn, Label paginationLbl, CheckBox selectAll)
    {
        // 데이터 로딩 및 인덱싱
        await _dataManager.LoadAndIndexDataAsync(clusterData);

        // UI 초기화
        _displayManager.Initialize(grid, pageNum, pageSize, prevBtn, nextBtn, paginationLbl, selectAll);

        // 초기 전체 데이터 표시
        var initialResult = await _searchEngine.ExecuteSearchAsync(new SearchCriteria());
        await _displayManager.DisplaySearchResultAsync(initialResult);
    }

    /// <summary>
    /// 현재 검색 결과의 모든 클러스터 ID 조회
    /// </summary>
    public List<int> GetCurrentResultClusterIds()
    {
        return _displayManager.GetCurrentResultClusterIds();
    }


    /// <summary>
    /// 통합 검색 실행
    /// </summary>
    public async Task<SearchResult> SearchAsync(SearchCriteria criteria)
    {
        var result = await _searchEngine.ExecuteSearchAsync(criteria);
        await _displayManager.DisplaySearchResultAsync(result);
        return result;
    }

    /// <summary>
    /// 선택된 클러스터 ID 목록 조회
    /// </summary>
    public List<int> GetSelectedClusterIds()
    {
        _displayManager.SaveCurrentSelectionState();
        return _displayManager.SelectedClusterIds.ToList();
    }

  

    /// <summary>
    /// 현재 선택 상태 저장 (래퍼 메서드)
    /// </summary>
    public void SaveCurrentSelectionState()
    {
        _displayManager.SaveCurrentSelectionState();
    }

    /// <summary>
    /// 데이터 새로고침
    /// </summary>
    public async Task RefreshDataAsync(DataTable newClusterData)
    {
        await _dataManager.LoadAndIndexDataAsync(newClusterData);
        var refreshResult = await _searchEngine.ExecuteSearchAsync(new SearchCriteria());
        await _displayManager.DisplaySearchResultAsync(refreshResult);
    }
}