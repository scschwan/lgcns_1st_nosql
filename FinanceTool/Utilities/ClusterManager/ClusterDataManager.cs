using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTool;


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
    /// 키워드 목록으로 클러스터 ID 조회
    /// </summary>
    public List<int> GetClusterIdsByKeywords(string columnName, List<string> keywords)
    {
        var clusterIds = new HashSet<int>();

        if (!_columnIndexes.ContainsKey(columnName))
            return new List<int>();

        var columnIndex = _columnIndexes[columnName];

        foreach (string keyword in keywords)
        {
            if (columnIndex.TryGetValue(keyword, out HashSet<int> ids))
            {
                clusterIds.UnionWith(ids);
            }
        }

        return clusterIds.ToList();
    }


    /// <summary>
    /// 영어 텍스트인지 확인
    /// </summary>
    private bool IsEnglishText(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        // 영어 알파벳이 하나라도 있으면 영어로 판단 (더 민감하게 감지)
        bool hasEnglish = text.Any(c => c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z');
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
