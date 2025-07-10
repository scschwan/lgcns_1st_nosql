// =====================================
// 1계층: 데이터 관리자 (ClusterDataManager)
// =====================================
namespace FinanceTool;
using System.Data;
using System.Diagnostics;


public class ClusterDataManager
{
    private DataTable _fullClusterData;
    private Dictionary<string, HashSet<int>> _keywordIndex;
    private Dictionary<string, HashSet<int>> _supplierIndex;
    private Dictionary<int, DataRow> _clusterRowIndex;
    private readonly object _lockObject = new object();

    
    private Dictionary<string, Dictionary<string, HashSet<int>>> _columnIndexes;
    
    // 검색 가능한 컬럼 정의 (5개)
    private string[] GetSearchableColumns()
    {
        return new string[]
        {
            "키워드목록",                    // 기존
            DataHandler.prod_col_name,       // 기존 - 공급업체
            DataHandler.levelName[1],        // 신규 - 타겟 열
            DataHandler.sub_acc_col_name,    // 신규 - 계정 열  
            DataHandler.dept_col_name        // 신규 - 코스트 센터 열
        };
    }

    public DataTable FullData => _fullClusterData;

    /// <summary>
    /// 정확한 값 검색 (DataHandler.FindMachEqualsKeyword 대체)
    /// </summary>
    public List<string> SearchExactValues(string columnName, string keyword)
    {
        try
        {
            if (string.IsNullOrEmpty(keyword) || !_columnIndexes.ContainsKey(columnName))
            {
                return new List<string>();
            }

            var columnIndex = _columnIndexes[columnName];
            var result = new List<string>();

            // 영어 검색인지 확인
            bool isEnglishSearch = IsEnglishText(keyword);
            Debug.WriteLine($"검색어 '{keyword}' - 영어 검색: {isEnglishSearch}");

            // 쉼표로 구분된 키워드 처리
            if (keyword.Contains(","))
            {
                var keywords = keyword.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k));
                foreach (string kw in keywords)
                {
                    var matches = FindExactMatches(columnIndex, kw, isEnglishSearch);
                    result.AddRange(matches);
                }
            }
            else
            {
                // 단일 키워드 정확 매칭
                var matches = FindExactMatches(columnIndex, keyword, isEnglishSearch);
                result.AddRange(matches);
            }

            return result.Distinct().ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SearchExactValues 오류: {ex.Message}");
            return new List<string>();
        }
    }

    /// <summary>
    /// 부분 문자열 검색 (DataHandler.FindMachKeyword 대체)
    /// </summary>
    public List<string> SearchContainsValues(string columnName, string keyword)
    {
        try
        {
            if (string.IsNullOrEmpty(keyword) || !_columnIndexes.ContainsKey(columnName))
            {
                return new List<string>();
            }

            var columnIndex = _columnIndexes[columnName];
            var result = new List<string>();

            // 영어 검색인지 확인
            bool isEnglishSearch = IsEnglishText(keyword);

            // 2글자 이상인 경우 CompareByTwoChars 로직 적용
            if (keyword.Length >= 2)
            {
                foreach (var kvp in columnIndex)
                {
                    bool isMatch = false;

                    if (isEnglishSearch)
                    {
                        // 영어인 경우: 대소문자 무시 + 기존 로직
                        isMatch = CompareByTwoCharsIgnoreCase(keyword, kvp.Key) ||
                                 CompareByTwoChars(keyword, kvp.Key);
                    }
                    else
                    {
                        // 한글인 경우: 기존 로직만
                        isMatch = CompareByTwoChars(keyword, kvp.Key);
                    }

                    if (isMatch)
                    {
                        result.Add(kvp.Key);
                    }
                }
            }
            else
            {
                // 1글자인 경우 Contains 검색
                foreach (var kvp in columnIndex)
                {
                    bool isMatch = false;

                    if (isEnglishSearch)
                    {
                        // 영어인 경우: 대소문자 무시
                        isMatch = kvp.Key.ToUpper().Contains(keyword.ToUpper());
                    }
                    else
                    {
                        // 한글인 경우: 기존 로직
                        isMatch = kvp.Key.Contains(keyword);
                    }

                    if (isMatch)
                    {
                        result.Add(kvp.Key);
                    }
                }
            }

            return result.Distinct().ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SearchContainsValues 오류: {ex.Message}");
            return new List<string>();
        }
    }

    /// <summary>
    /// 영어 텍스트인지 확인
    /// </summary>
    private bool IsEnglishText(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        // 영어 알파벳이 하나라도 있으면 영어로 판단 (더 민감하게 감지)
        bool hasEnglish = text.Any(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'));
        return hasEnglish;
    }

    /// <summary>
    /// 정확 매칭 검색 (대소문자 무시 옵션)
    /// </summary>
    private List<string> FindExactMatches(Dictionary<string, HashSet<int>> columnIndex, string keyword, bool ignoreCase)
    {
        var matches = new List<string>();

        if (ignoreCase)
        {
            string upperKeyword = keyword.ToUpper();
            foreach (var kvp in columnIndex)
            {
                if (kvp.Key.ToUpper() == upperKeyword)
                {
                    matches.Add(kvp.Key);
                }
            }
        }
        else
        {
            // 기존 로직: 정확 매칭
            if (columnIndex.ContainsKey(keyword))
            {
                matches.Add(keyword);
            }
        }

        return matches;
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
    /// 2글자 기준 비교 로직 (DataHandler.CompareByTwoChars 대체)
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

    /// <summary>
    /// 병합 상태에 따른 키워드 필터링 (ExtractUniqueKeywords 대체)
    /// </summary>
    public List<string> FilterValuesByMergeStatus(List<string> values, string columnName, bool mergedOnly = false)
    {
        try
        {
            if (!_columnIndexes.ContainsKey(columnName))
            {
                return new List<string>();
            }

            var columnIndex = _columnIndexes[columnName];
            var filteredValues = new HashSet<string>();

            foreach (string value in values)
            {
                if (!columnIndex.ContainsKey(value)) continue;

                var clusterIds = columnIndex[value];
                foreach (int clusterId in clusterIds)
                {
                    var row = GetClusterRow(clusterId);
                    if (row == null) continue;

                    // 병합 상태 확인
                    if (!row.IsNull("ClusterID") && !row.IsNull("ID"))
                    {
                        int clusterIdValue = Convert.ToInt32(row["ClusterID"]);
                        int idValue = Convert.ToInt32(row["ID"]);

                        if (mergedOnly)
                        {
                            // 병합된 클러스터만: ClusterID > 0 && ClusterID == ID
                            if (clusterIdValue > 0 && clusterIdValue == idValue)
                            {
                                filteredValues.Add(value);
                                break;
                            }
                        }
                        else
                        {
                            // 병합되지 않은 클러스터만: ClusterID <= 0 || ClusterID == ID
                            if (clusterIdValue <= 0 || clusterIdValue == idValue)
                            {
                                filteredValues.Add(value);
                                break;
                            }
                        }
                    }
                }
            }

            return filteredValues.OrderBy(v => v).ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"FilterValuesByMergeStatus 오류: {ex.Message}");
            return new List<string>();
        }
    }



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

  

    private void BuildSearchIndexes()
    {
        _columnIndexes = new Dictionary<string, Dictionary<string, HashSet<int>>>();
        _clusterRowIndex = new Dictionary<int, DataRow>();

        var searchableColumns = GetSearchableColumns();

        // 각 검색 가능한 컬럼별로 인덱스 구조 초기화
        foreach (string columnName in searchableColumns)
        {
            if (!string.IsNullOrEmpty(columnName))
            {
                _columnIndexes[columnName] = new Dictionary<string, HashSet<int>>();
            }
        }

        var lockObj = new object();

        // 병렬 처리로 인덱스 구축
        Parallel.ForEach(_fullClusterData.AsEnumerable(), new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        }, row =>
        {
            if (!int.TryParse(row["ID"]?.ToString(), out int clusterId)) return;

            // 행 인덱스 구축
            lock (lockObj)
            {
                _clusterRowIndex[clusterId] = row;
            }

            // 각 검색 가능한 컬럼에 대해 인덱스 구축
            foreach (string columnName in searchableColumns)
            {
                if (string.IsNullOrEmpty(columnName)) continue;

                // DataTable에 해당 컬럼이 존재하는지 확인
                if (!_fullClusterData.Columns.Contains(columnName)) continue;

                string columnValue = row[columnName]?.ToString() ?? "";
                if (!string.IsNullOrEmpty(columnValue))
                {
                    IndexColumnValue(columnName, columnValue, clusterId, lockObj);
                }
            }
        });

        Debug.WriteLine($"컬럼별 인덱스 구축 완료:");
        foreach (var column in _columnIndexes)
        {
            Debug.WriteLine($"  - {column.Key}: {column.Value.Count}개 고유값");
        }
    }

    private void IndexColumnValue(string columnName, string columnValue, int clusterId, object lockObj)
    {
        // 컬럼 타입에 따른 인덱싱 방식
        if (columnName == "키워드목록" || columnName == DataHandler.prod_col_name)
        {
            // 쉼표로 분리된 값들 각각 인덱싱 (기존 방식)
            var values = columnValue.Split(',').Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v));
            foreach (string value in values)
            {
                AddToIndex(columnName, value, clusterId, lockObj);
            }
        }
        else
        {
            // 단일 값 인덱싱 (새로 추가되는 컬럼들)
            AddToIndex(columnName, columnValue.Trim(), clusterId, lockObj);
        }
    }

    private void AddToIndex(string columnName, string value, int clusterId, object lockObj)
    {
        lock (lockObj)
        {
            if (!_columnIndexes[columnName].ContainsKey(value))
                _columnIndexes[columnName][value] = new HashSet<int>();
            _columnIndexes[columnName][value].Add(clusterId);
        }
    }



    /// <summary>
    /// 특정 컬럼에서 검색 가능한 모든 값 목록 조회
    /// </summary>
    public List<string> GetColumnValues(string columnName)
    {
        if (_columnIndexes.ContainsKey(columnName))
        {
            return _columnIndexes[columnName].Keys.OrderBy(k => k).ToList();
        }
        return new List<string>();
    }

    /// <summary>
    /// 컬럼별 검색
    /// </summary>
    public HashSet<int> SearchByColumn(string columnName, List<string> keywords, bool exactMatch = false, bool useAnd = true)
    {
        if (!_columnIndexes.ContainsKey(columnName))
        {
            Debug.WriteLine($"검색 불가능한 컬럼: {columnName}");
            return new HashSet<int>();
        }

        HashSet<int> result = null;
        var columnIndex = _columnIndexes[columnName];

        foreach (string keyword in keywords)
        {
            HashSet<int> keywordMatches = new HashSet<int>();

            if (exactMatch)
            {
                // 정확한 값 매칭
                if (columnIndex.TryGetValue(keyword, out HashSet<int> exactIds))
                    keywordMatches = exactIds;
            }
            else
            {
                // 부분 문자열 매칭 (Contains)
                var matchingKeys = columnIndex.Keys.Where(k => k.Contains(keyword));
                foreach (string matchKey in matchingKeys)
                    keywordMatches.UnionWith(columnIndex[matchKey]);
            }

            // AND/OR 로직 적용
            if (useAnd)
            {
                result = result == null ? keywordMatches : result.Intersect(keywordMatches).ToHashSet();
                if (result.Count == 0) break; // 교집합이 없으면 조기 종료
            }
            else // OR 검색
            {
                result = result == null ? keywordMatches : result.Union(keywordMatches).ToHashSet();
            }
        }

        return result ?? new HashSet<int>();
    }

    /// <summary>
    /// 검색 가능한 컬럼 목록 조회
    /// </summary>
    public Dictionary<string, string> GetSearchableColumnInfo()
    {
        var columns = new Dictionary<string, string>
        {
            { "키워드목록", "키워드" },
            { DataHandler.prod_col_name, DataHandler.prod_col_name },
            { DataHandler.levelName[1], DataHandler.levelName[1] },
            { DataHandler.sub_acc_col_name, DataHandler.sub_acc_col_name },
            { DataHandler.dept_col_name, DataHandler.dept_col_name }
        };

        // 실제 데이터에 존재하는 컬럼만 반환
        return columns.Where(c => !string.IsNullOrEmpty(c.Key) &&
                                 _fullClusterData.Columns.Contains(c.Key))
                      .ToDictionary(c => c.Key, c => c.Value);
    }


/// <summary>
/// 모든 클러스터 ID 조회
/// </summary>
public HashSet<int> GetAllClusterIds()
    {
        return new HashSet<int>(_clusterRowIndex.Keys);
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

        // _keywordIndex 대신 _columnIndexes 사용
        if (_columnIndexes == null || !_columnIndexes.ContainsKey("키워드목록"))
        {
            Debug.WriteLine("키워드 인덱스가 초기화되지 않았습니다.");
            return clusterIds;
        }

        HashSet<int> excludeIds = new HashSet<int>();
        var keywordIndex = _columnIndexes["키워드목록"];

        foreach (string excludeKeyword in excludeKeywords)
        {
            if (string.IsNullOrEmpty(excludeKeyword)) continue;

            // 영어 검색인지 확인
            bool isEnglishSearch = IsEnglishText(excludeKeyword);

            // 부분 매칭으로 제외 키워드 검색
            foreach (var kvp in keywordIndex)
            {
                bool isMatch = false;

                if (isEnglishSearch)
                {
                    // 영어인 경우: 대소문자 무시
                    isMatch = kvp.Key.ToUpper().Contains(excludeKeyword.ToUpper());
                }
                else
                {
                    // 한글인 경우: 기존 로직
                    if (excludeKeyword.Length >= 2)
                    {
                        isMatch = CompareByTwoChars(excludeKeyword, kvp.Key);
                    }
                    else
                    {
                        isMatch = kvp.Key.Contains(excludeKeyword);
                    }
                }

                if (isMatch)
                {
                    excludeIds.UnionWith(kvp.Value);
                }
            }
        }

        var result = clusterIds.Except(excludeIds).ToHashSet();
        Debug.WriteLine($"제외 키워드 적용: 전체 {clusterIds.Count}개 → 필터링 후 {result.Count}개");

        return result;
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
    /// 다중 컬럼 검색 지원 (새로 추가)
    /// </summary>
    private HashSet<int> ExecuteMultiColumnSearch(SearchCriteria criteria)
    {
        HashSet<int> result = null;

        foreach (var columnCriteria in criteria.ColumnCriteria)
        {
            string columnName = columnCriteria.Key;
            var searchCriteria = columnCriteria.Value;

            HashSet<int> columnResult = _dataManager.SearchByColumn(
                columnName,
                searchCriteria.Keywords,
                searchCriteria.ExactMatch,
                searchCriteria.UseAnd
            );

            // 컬럼 간 결합 (기본적으로 AND)
            result = result == null ? columnResult : result.Intersect(columnResult).ToHashSet();

            if (result.Count == 0) break; // 교집합이 없으면 조기 종료
        }

        return result ?? new HashSet<int>();
    }

    // 기존 ExecuteSearchAsync는 하위 호환성을 위해 유지하되 내부적으로 새 방식 사용
    public async Task<SearchResult> ExecuteSearchAsync(SearchCriteria criteria)
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                HashSet<int> candidateIds = null;

                // 빈 검색어 처리
                if (criteria.IsFullSearch ||
                    (criteria.Keywords?.Count == 0 && criteria.ColumnCriteria?.Count == 0))
                {
                    if (criteria.IsSubSearchMode && criteria.BaseSearchResults?.Count > 0)
                    {
                        // 결과 내 재검색: 이전 검색 결과만 반환
                        candidateIds = new HashSet<int>(criteria.BaseSearchResults);
                        Debug.WriteLine($"결과 내 재검색: {candidateIds.Count}개 항목");
                    }
                    else
                    {
                        // 전체 검색: 모든 데이터 반환
                        candidateIds = _dataManager.GetAllClusterIds();
                        Debug.WriteLine($"전체 검색: {candidateIds.Count}개 항목");
                    }
                }
                //기존 검색 로직
                else
                {
                    // 기존 검색 로직
                    if (criteria.IsMultiColumnSearch)
                    {
                        candidateIds = ExecuteMultiColumnSearch(criteria);
                    }
                    else
                    {
                        candidateIds = ExecuteLegacySearch(criteria);
                    }
                }

                // 제외 키워드 적용
                if (criteria.ExcludeKeywords?.Count > 0)
                {
                    try
                    {
                        candidateIds = _dataManager.ExcludeByKeywords(candidateIds ?? new HashSet<int>(), criteria.ExcludeKeywords);
                        Debug.WriteLine($"제외 키워드 적용 완료: {criteria.ExcludeKeywords.Count}개 키워드");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"제외 키워드 적용 오류: {ex.Message}");
                        // 제외 키워드 적용 실패 시에도 검색은 계속 진행
                    }
                }

                // 병합 상태 필터링
                var filteredIds = FilterByMergeStatus(candidateIds ?? new HashSet<int>());

                // 결과 DataTable 생성
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
                    SearchCriteria = criteria,
                    Error = ex.Message
                };
            }
        });
    }

    /// <summary>
    /// 기존 방식 검색 실행 (하위 호환성)
    /// </summary>
    private HashSet<int> ExecuteLegacySearch(SearchCriteria criteria)
    {
        HashSet<int> candidateIds = null;

        // 1. 키워드 검색
        if (criteria.IsKeywordSearch && criteria.Keywords?.Count > 0)
        {
            candidateIds = _dataManager.SearchByColumn("키워드목록", criteria.Keywords, criteria.ExactMatch, criteria.AndSearch);
        }
        // 2. 공급업체 검색
        else if (criteria.IsSupplierSearch && criteria.Keywords?.Count > 0)
        {
            candidateIds = _dataManager.SearchByColumn(DataHandler.prod_col_name, criteria.Keywords, criteria.ExactMatch, criteria.AndSearch);
        }
        // 3. 전체 검색
        else
        {
            candidateIds = _dataManager.GetAllClusterIds();
        }

        return candidateIds;
    }

    private List<int> FilterByMergeStatus(HashSet<int> candidateIds)
    {
        return candidateIds.AsParallel()
            .Where(id =>
            {
                var row = _dataManager.GetClusterRow(id);
                if (row == null) return false;

                if (!row.IsNull("ClusterID") && !row.IsNull("ID"))
                {
                    int clusterId = Convert.ToInt32(row["ClusterID"]);
                    int rowId = Convert.ToInt32(row["ID"]);
                    return clusterId == -1 || (clusterId != rowId && clusterId < 0);
                }
                return false;
            })
            .ToList();
    }

    private DataTable CreateResultDataTable(List<int> clusterIds)
    {
        if (_dataManager.FullData == null) return new DataTable();

        DataTable resultTable = _dataManager.FullData.Clone();

        var rows = clusterIds.AsParallel()
            .Select(id => _dataManager.GetClusterRow(id))
            .Where(row => row != null)
            .OrderByDescending(row => Convert.ToInt32(row["ID"]))
            .ToList();

        foreach (var row in rows)
        {
            resultTable.ImportRow(row);
        }

        return resultTable;
    }
}



public class SearchColumnCriteria
{
    public List<string> Keywords { get; set; } = new List<string>();
    public bool ExactMatch { get; set; } = false;
    public bool UseAnd { get; set; } = true;
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

    /// <summary>
    /// 특정 클러스터 ID들로 결과 표시
    /// </summary>
    public async Task DisplaySpecificClustersAsync(List<int> clusterIds)
    {
        try
        {
            if (clusterIds == null || clusterIds.Count == 0)
            {
                // 빈 결과 표시
                var emptyResult = new SearchResult
                {
                    Data = _dataManager.FullData.Clone(),
                    TotalCount = 0,
                    ElapsedMilliseconds = 0
                };
                await _displayManager.DisplaySearchResultAsync(emptyResult);
                return;
            }

            // 지정된 클러스터 ID들의 DataTable 생성
            DataTable filteredTable = _dataManager.FullData.Clone();

            var filteredRows = clusterIds
                .Select(id => _dataManager.GetClusterRow(id))
                .Where(row => row != null)
                .OrderByDescending(row => Convert.ToInt32(row["ID"]));

            foreach (var row in filteredRows)
            {
                filteredTable.ImportRow(row);
            }

            var result = new SearchResult
            {
                Data = filteredTable,
                TotalCount = clusterIds.Count,
                ElapsedMilliseconds = 0
            };

            await _displayManager.DisplaySearchResultAsync(result);

            Debug.WriteLine($"특정 클러스터 표시 완료: {clusterIds.Count}개");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"특정 클러스터 표시 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 다중 컬럼 검색 (새로 추가)
    /// </summary>
    public async Task<SearchResult> SearchMultipleColumnsAsync(Dictionary<string, SearchColumnCriteria> columnCriteria, List<string> excludeKeywords = null)
    {
        var criteria = SearchCriteria.FromMultiColumn(columnCriteria, excludeKeywords);
        var result = await _searchEngine.ExecuteSearchAsync(criteria);
        await _displayManager.DisplaySearchResultAsync(result);
        return result;
    }

    /// <summary>
    /// 검색 가능한 컬럼 정보 조회
    /// </summary>
    public Dictionary<string, string> GetSearchableColumns()
    {
        return _dataManager.GetSearchableColumnInfo();
    }

    /// <summary>
    /// 특정 컬럼의 모든 값 목록 조회 (콤보박스용)
    /// </summary>
    public List<string> GetColumnValues(string columnName)
    {
        return _dataManager.GetColumnValues(columnName);
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
    /// 기존 방식 검색 (하위 호환성)
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

    // ClusteringManager.cs 파일에 추가할 메서드들

    /// <summary>
    /// 정확한 값 검색 (DataHandler.FindMachEqualsKeyword 대체)
    /// </summary>
    public List<string> SearchExact(string columnName, string keyword)
    {
        return _dataManager.SearchExactValues(columnName, keyword);
    }

    /// <summary>
    /// 부분 문자열 검색 (DataHandler.FindMachKeyword 대체)
    /// </summary>
    public List<string> SearchContains(string columnName, string keyword)
    {
        return _dataManager.SearchContainsValues(columnName, keyword);
    }

   


    /// <summary>
    /// 표시명을 컬럼명으로 변환 (ConvertDisplayNameToColumnName 대체)
    /// </summary>
    public string ConvertDisplayNameToColumnName(string displayName)
    {
        var columnMapping = new Dictionary<string, string>
    {
        { "키워드", "키워드목록" },
        { "공급업체", DataHandler.prod_col_name },
        { "타겟", DataHandler.levelName[1] },
        { "계정", DataHandler.sub_acc_col_name },
        { "코스트센터", DataHandler.dept_col_name }
    };

        //return columnMapping.TryGetValue(displayName, out string columnName) ? columnName : displayName;
        // null 체크 추가
        if (string.IsNullOrEmpty(displayName))
            return "키워드목록"; // 기본값

        return columnMapping.TryGetValue(displayName, out string columnName) && !string.IsNullOrEmpty(columnName)
            ? columnName
            : displayName;
    }

    /// <summary>
    /// 직접입력 전용 컬럼 확인
    /// </summary>
    public bool IsDirectInputOnlyColumn(string columnName)
    {
        var directInputOnlyColumns = new[]
        {
        DataHandler.levelName?.Count > 1 ? DataHandler.levelName[1] : "",
        DataHandler.sub_acc_col_name
    };

        return directInputOnlyColumns.Contains(columnName);
    }

    /// <summary>
    /// 검색 가능한 컬럼의 실제 데이터 존재 여부 확인
    /// </summary>
    public bool HasDataInColumn(string columnName)
    {
        try
        {
            var values = GetColumnValues(columnName);
            return values != null && values.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 컬럼별 고유값 개수 조회 (성능 모니터링용)
    /// </summary>
    public int GetColumnValueCount(string columnName)
    {
        try
        {
            var values = GetColumnValues(columnName);
            return values?.Count ?? 0;
        }
        catch
        {
            return 0;
        }
    }


}