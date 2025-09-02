// =====================================
// 1계층: 데이터 관리자 (ClusterDataManager)
// =====================================
namespace FinanceTool;

using System.Data;
using System.Diagnostics;



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
    public async Task<SearchResult> SearchMultipleColumnsAsync(Dictionary<string, SearchColumnCriteria> columnCriteria, List<string> excludeKeywords = null , bool clusterSubIDYN = false)
    {
        var criteria = SearchCriteria.FromMultiColumn(columnCriteria, excludeKeywords);
        var result = await _searchEngine.ExecuteSearchAsync(criteria , clusterSubIDYN);
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
                                    Button prevBtn, Button nextBtn, Label paginationLbl, CheckBox selectAll , bool subClusterYN = false)
    {
        // 데이터 로딩 및 인덱싱
        await _dataManager.LoadAndIndexDataAsync(clusterData);

        // UI 초기화
        _displayManager.Initialize(grid, pageNum, pageSize, prevBtn, nextBtn, paginationLbl, selectAll);

        // 초기 전체 데이터 표시
        var initialResult = await _searchEngine.ExecuteSearchAsync(new SearchCriteria() , subClusterYN);
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
    public async Task<SearchResult> SearchAsync(SearchCriteria criteria, bool subClusterYN = false)
    {
        var result = await _searchEngine.ExecuteSearchAsync(criteria, subClusterYN);
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
    /// 정확한 값 검색 - 클러스터 ID 반환
    /// </summary>
    public List<int> SearchExactClusterIds(string columnName, string keyword)
    {
        var matchingKeywords = _dataManager.SearchExactValues(columnName, keyword);
        return _dataManager.GetClusterIdsByKeywords(columnName, matchingKeywords);
    }

    /// <summary>
    /// 부분 문자열 검색 (DataHandler.FindMachKeyword 대체)
    /// </summary>
    public List<string> SearchContains(string columnName, string keyword)
    {
        return _dataManager.SearchContainsValues(columnName, keyword);
    }

    /// <summary>
    /// 부분 문자열 검색 - 클러스터 ID 반환
    /// </summary>
    public List<int> SearchContainsClusterIds(string columnName, string keyword)
    {
        var matchingKeywords = _dataManager.SearchContainsValues(columnName, keyword);
        return _dataManager.GetClusterIdsByKeywords(columnName, matchingKeywords);
    }


    // ClusteringManager 클래스에 추가할 메서드
    public List<int> SearchWithComplexConditions(string columnName, ParsedKeywords parsedKeywords, bool exactMatch
        , List<int> baseSearchResults ,  bool isSubSearchMode  , bool subClusteringYN = false)
    {
        List<int> results = new List<int>();

        // AND 조건만 있는 경우
        if (parsedKeywords.AndKeywords.Count > 0 && parsedKeywords.OrKeywords.Count == 0)
        {
            results = ProcessAndConditions(columnName, parsedKeywords.AndKeywords, exactMatch);
        }

        // OR 조건만 있는 경우  
        if (parsedKeywords.AndKeywords.Count == 0 && parsedKeywords.OrKeywords.Count > 0)
        {
            results =  ProcessOrConditions(columnName, parsedKeywords.OrKeywords, exactMatch);
        }

        // AND + OR 조건이 모두 있는 경우 (A,B|C = A AND (B OR C))
        if (parsedKeywords.AndKeywords.Count > 0 && parsedKeywords.OrKeywords.Count > 0)
        {
            var andResults = ProcessAndConditions(columnName, parsedKeywords.AndKeywords, exactMatch);
            var orResults = ProcessOrConditions(columnName, parsedKeywords.OrKeywords, exactMatch);

            // AND 결과와 OR 결과의 교집합
            results =  andResults.Intersect(orResults).ToList();
        }

        if (results.Count > 0)
        {
            // 자기 자신 제외 로직 추가
            results = FilterOutSelfReferences(results , subClusteringYN , baseSearchResults , isSubSearchMode);
        }
        

        return results;
    }

    /// <summary>
    /// 검색 결과에서 자기 자신을 제외하는 필터링 함수
    /// 검색 결과 내에서 ID, ClusterID, ClusterSubID 값을 추출하여 자동으로 자기 자신을 제외
    /// </summary>
    /// <param name="clusterIds">검색된 클러스터 ID 목록</param>
    /// <returns>필터링된 클러스터 ID 목록</returns>
    private List<int> FilterOutSelfReferences(List<int> clusterIds , bool subClusteringYN , List<int> baseSearchResults, bool isSubSearchMode)
    {
        if (clusterIds == null || clusterIds.Count == 0)
            return new List<int>();

        
        // 필터링 수행 - 제외 목록에 있는 ID는 결과에서 제거
        List<int> filteredResults = new List<int>();
        foreach (int id in clusterIds)
        {
            var clusterRow = GetClusterRow(id);
            if (clusterRow == null) continue;

            int clusterID = Convert.ToInt32(clusterRow["ClusterID"]);
            int clusterSubID = Convert.ToInt32(clusterRow["ClusterSubID"]);

            //병합 클러스터링은 검색결과 제외
            if (id == clusterID || id == clusterSubID)
            {
                Debug.WriteLine($"병합 클러스터링 결과는 제외 ID : {id} , clusterID : {clusterID} , clusterSubID : {clusterSubID}");
                continue;

            }
            //세부클러스터링 검색 조회 대상이 아닐 경우
            else if (subClusteringYN && clusterSubID != -1)
            {
                continue;
            }
            //클러스터링 검색 조회 대상이 아닐 경우
            else if (!subClusteringYN && clusterID > 0 )
            {
                continue;
            }
            //결과내 검색 필터링 기능 추가
            else if (isSubSearchMode && !baseSearchResults.Contains(id))
            {
                continue;
            }
            else
            {
                filteredResults.Add(id);
            }
        }
        Debug.WriteLine($"필터링 결과: 원본 {clusterIds.Count}개 → 필터링 후 {filteredResults.Count}개");
        return filteredResults;
    }


    private List<int> ProcessAndConditions(string columnName, List<string> andKeywords, bool exactMatch)
    {
        var result = new HashSet<int>();
        bool firstKeyword = true;

        foreach (string keyword in andKeywords)
        {
            //List<string> currentResults;
            List<int> currentResults; // ← int로 변경
            if (exactMatch)
            {
                //currentResults = SearchExact(columnName, keyword);
                currentResults = SearchExactClusterIds(columnName, keyword);
            }
            else
            {
                //currentResults = SearchContains(columnName, keyword);
                currentResults = SearchContainsClusterIds(columnName, keyword);
            }
        

            Debug.WriteLine($"AND 키워드 '{keyword}' 검색 결과: {currentResults.Count}건");

            if (firstKeyword)
            {
                //result = new HashSet<string>(currentResults);
                result = new HashSet<int>(currentResults);
                firstKeyword = false;
            }
            else
            {
                // 교집합 처리
                result.IntersectWith(currentResults);
            }

            Debug.WriteLine($"AND 누적 결과: {result.Count}건");
        }

        return result.ToList();
    }

    private List<int> ProcessOrConditions(string columnName, List<string> orKeywords, bool exactMatch)
    {
        var result = new HashSet<int>();

        foreach (string keyword in orKeywords)
        {
            //List<string> currentResults;
            List<int> currentResults; // ← int로 변경
            if (exactMatch)
            {
                //currentResults = SearchExact(columnName, keyword);
                currentResults = SearchExactClusterIds(columnName, keyword);
            }
            else
            {
                //currentResults = SearchContains(columnName, keyword);
                currentResults = SearchContainsClusterIds(columnName, keyword);
            }

            Debug.WriteLine($"OR 키워드 '{keyword}' 검색 결과: {currentResults.Count}건");

            // 합집합 처리
            result.UnionWith(currentResults);

            Debug.WriteLine($"OR 누적 결과: {result.Count}건");
        }

        return result.ToList();
    }

    public DataRow GetClusterRow(int clusterId)
    {
        return _dataManager.GetClusterRow(clusterId);
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


}