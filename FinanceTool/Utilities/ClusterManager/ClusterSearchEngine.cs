using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTool;

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
    public async Task<SearchResult> ExecuteSearchAsync(SearchCriteria criteria, bool subClusterYN = false)
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                HashSet<int> candidateIds = null;

                // 빈 검색어 처리
                if (criteria.IsFullSearch ||
                    criteria.Keywords?.Count == 0 && criteria.ColumnCriteria?.Count == 0)
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
                var filteredIds = FilterByMergeStatus(candidateIds ?? new HashSet<int>(), subClusterYN);

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

    private List<int> FilterByMergeStatus(HashSet<int> candidateIds, bool useSubClustering = false)
    {
        string clusterIdColumn = useSubClustering ? "ClusterSubID" : "ClusterID";

        return candidateIds.AsParallel()
            .Where(id =>
            {
                var row = _dataManager.GetClusterRow(id);
                if (row == null) return false;

                if (!row.IsNull("ClusterID") && !row.IsNull("ID"))
                {
                    //int clusterId = Convert.ToInt32(row["ClusterID"]);
                    int clusterId = Convert.ToInt32(row[clusterIdColumn]);
                    int rowId = Convert.ToInt32(row["ID"]);
                    return clusterId == -1 || clusterId != rowId && clusterId < 0;
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
