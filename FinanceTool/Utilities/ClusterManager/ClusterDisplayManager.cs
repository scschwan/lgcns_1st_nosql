using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTool
{

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
                //_pageSizeControl.Items.AddRange(new object[] { 100, 200, 500, 1000, 2000 });
                _pageSizeControl.Items.AddRange(new object[] { 1000, 2000, 5000, 10000 });
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
            //if (_targetGrid.Columns["ID"] != null) _targetGrid.Columns["ID"].Visible = true;
            //if (_targetGrid.Columns["ClusterID"] != null) _targetGrid.Columns["ClusterID"].Visible = true;
            if (_targetGrid.Columns["ID"] != null) _targetGrid.Columns["ID"].Visible = false;
            if (_targetGrid.Columns["ClusterID"] != null) _targetGrid.Columns["ClusterID"].Visible = false;
            if (_targetGrid.Columns["ClusterSubID"] != null) _targetGrid.Columns["ClusterSubID"].Visible = false;
            if (_targetGrid.Columns["_id"] != null) _targetGrid.Columns["_id"].Visible = false;
            if (_targetGrid.Columns["is_hidden"] != null) _targetGrid.Columns["is_hidden"].Visible = false;
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

        // 새로 추가: 다중 컬럼 검색 지원
        public Dictionary<string, SearchColumnCriteria> ColumnCriteria { get; set; } = new Dictionary<string, SearchColumnCriteria>();
        public bool IsMultiColumnSearch { get; set; } = false;

        // 새로 추가: 빈 검색어 처리
        public bool IsFullSearch { get; set; } = false;
        public bool IsSubSearchMode { get; set; } = false;
        public List<int> BaseSearchResults { get; set; } = new List<int>();



        public static SearchCriteria FromMultiColumn(Dictionary<string, SearchColumnCriteria> columnCriteria, List<string> excludeKeywords = null)
        {
            var criteria = new SearchCriteria
            {
                ColumnCriteria = columnCriteria,
                ExcludeKeywords = excludeKeywords ?? new List<string>(),
                IsMultiColumnSearch = true
            };

            return criteria;
        }
    }


    public class SearchResult
    {
        public DataTable Data { get; set; }
        public int TotalCount { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public SearchCriteria SearchCriteria { get; set; }
        public string Error { get; set; }
    }

    // 새로운 클래스 추가 (uc_Clustering.cs 내부)
    public class ParsedKeywords
    {
        public List<string> AndKeywords { get; set; } = new List<string>();
        public List<string> OrKeywords { get; set; } = new List<string>();
    }
    // =====================================
    // 통합 관리자 (Facade 패턴)
    // =====================================
}
