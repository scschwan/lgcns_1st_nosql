using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTool
{
    
    
    public partial class uc_Clustering
    {

      
        // === 페이징 초기화 메서드 ===


        // === 페이징 컨트롤 활성화/비활성화 ===
        private void EnablePaginationControlsMerge(bool enabled)
        {
            btn_prevPage.Enabled = enabled;
            btn_nextPage.Enabled = enabled;
            num_pageNumber.Enabled = enabled;
            cmb_pageSize.Enabled = enabled;
        }


       

        /// <summary>
        /// 검색 UI 초기화 (새로 추가)
        /// </summary>
        private void InitializeSearchUI()
        {
            // 1. 검색 컬럼 콤보박스 초기화
            column_search_combo.Items.Clear();

            // ClusteringManager에서 검색 가능한 컬럼 정보 가져오기
            var searchableColumns = _clusteringManager.GetSearchableColumns();

            //column_search_combo.Items.Add("컬럼 선택");
            foreach (var column in searchableColumns)
            {
                column_search_combo.Items.Add(column.Value); // 표시명 (키워드, 공급업체, 타겟, 계정, 코스트센터)
            }

            // *** 수정: 첫 번째 항목(인덱스 0)을 기본 선택으로 설정 ***
            if (column_search_combo.Items.Count > 0)
            {
                column_search_combo.SelectedIndex = 0;
            }

            // 2. 검색 내 검색 체크박스 초기화
            sub_search_checkbox.Checked = false;
            sub_search_checkbox.Text = "결과 내 재검색";


            Debug.WriteLine("검색 UI 초기화 완료");
        }

        // *** 새로 추가: 초기 전체 검색 메서드 ***
        private async Task PerformInitialSearch()
        {
            try
            {
                // 전체 검색 조건으로 초기 검색 실행
                var searchCriteria = new SearchCriteria
                {
                    Keywords = new List<string>(), // 빈 키워드 = 전체 검색
                    ExcludeKeywords = null,
                    ExactMatch = false,
                    AndSearch = false
                };

                await _clusteringManager.SearchAsync(searchCriteria);

                // 선택 상태 초기화
                merge_all_check.Checked = false;
                change_row_count();

                Debug.WriteLine("초기 전체 검색 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"초기 검색 실행 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 UI 상태에서 SearchCriteria 생성
        /// </summary>
        private SearchCriteria CreateSearchCriteriaFromCurrentUI()
        {
            try
            {
                string targetKeyword = merge_search_keyword.Text?.Trim() ?? "";

                // 검색어가 없으면 전체 검색
                if (string.IsNullOrEmpty(targetKeyword))
                {
                    if (_isSubSearchMode && _baseSearchResults.Count > 0)
                    {
                        // 결과 내 재검색 모드: 이전 검색 결과만 표시
                        return new SearchCriteria
                        {
                            Keywords = new List<string>(),
                            ExcludeKeywords = GetExcludeKeywords(),
                            ExactMatch = equalsSearchYN,
                            AndSearch = andSearchYN,
                            IsSubSearchMode = true,
                            BaseSearchResults = _baseSearchResults
                        };
                    }
                    else
                    {
                        // 일반 모드: 전체 데이터 검색
                        return new SearchCriteria
                        {
                            Keywords = new List<string>(),
                            ExcludeKeywords = GetExcludeKeywords(),
                            ExactMatch = equalsSearchYN,
                            AndSearch = andSearchYN,
                            IsFullSearch = true
                        };
                    }
                }

                // 현재 선택된 컬럼 확인
                string currentColumn = GetSelectedSearchColumn();
                Debug.WriteLine($"검색 컬럼: {currentColumn}, 키워드: {targetKeyword}");

                // ClusteringManager를 통한 검색 실행
                List<string> matchingKeywords;
                if (equalsSearchYN)
                {
                    matchingKeywords = _clusteringManager.SearchExact(currentColumn, targetKeyword);
                }
                else
                {
                    matchingKeywords = _clusteringManager.SearchContains(currentColumn, targetKeyword);
                }

                Debug.WriteLine($"매칭된 키워드: {matchingKeywords.Count}개");

                // 다중 컬럼 검색 조건 구성
                var columnCriteria = new Dictionary<string, SearchColumnCriteria>();

                if (matchingKeywords.Count > 0)
                {
                    columnCriteria[currentColumn] = new SearchColumnCriteria
                    {
                        Keywords = matchingKeywords,
                        ExactMatch = true, // 이미 매칭된 키워드들이므로 정확 매칭
                        UseAnd = andSearchYN
                    };
                }

                return new SearchCriteria
                {
                    ColumnCriteria = columnCriteria,
                    IsMultiColumnSearch = true,
                    ExcludeKeywords = GetExcludeKeywords(),
                    ExactMatch = equalsSearchYN,
                    AndSearch = andSearchYN
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"검색 조건 생성 오류: {ex.Message}");
                return new SearchCriteria();
            }
        }

        /// <summary>
        /// 빈 검색 결과 표시
        /// </summary>
        private async Task ShowEmptySearchResult()
        {
            try
            {
                // 빈 결과 표시
                await _clusteringManager.DisplaySpecificClustersAsync(new List<int>());

                // 페이징 컨트롤 비활성화
                EnablePaginationControlsMerge(false);

                change_row_count();

                Debug.WriteLine("빈 검색 결과 표시 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"빈 검색 결과 표시 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 선택된 검색 컬럼 반환 (keyword_radio 대체)
        /// </summary>
        private string GetSelectedSearchColumn()
        {
            // *** 수정: 인덱스 0부터 유효한 컬럼으로 처리 ***
            if (column_search_combo.SelectedIndex >= 0)
            {
                string selectedDisplayName = column_search_combo.SelectedItem.ToString();
                string columnName = _clusteringManager.ConvertDisplayNameToColumnName(selectedDisplayName);

                // 실제 데이터에 해당 컬럼이 존재하는지 확인
                if (_clusteringManager.HasDataInColumn(columnName))
                {
                    Debug.WriteLine($"선택된 검색 컬럼: {selectedDisplayName} -> {columnName}");
                    return columnName;
                }
                else
                {
                    Debug.WriteLine($"경고: 선택된 컬럼 '{columnName}'에 데이터가 없습니다.");
                    return "키워드목록"; // 기본값: 키워드
                }
            }
            else
            {
                Debug.WriteLine("검색 컬럼이 선택되지 않아 기본값(키워드목록) 사용");
                return "키워드목록"; // 기본값: 키워드
            }

        }

        /// <summary>
        /// 검색 조건으로 실제 검색 수행
        /// </summary>
        private async Task PerformSearchWithCriteria(Dictionary<string, SearchColumnCriteria> columnCriteria, bool isAlreadyProgress)
        {
            try
            {
                // 제외 키워드 처리
                List<string> excludeKeywords = null;
                if (!string.IsNullOrEmpty(except_keyword.Text))
                {
                    excludeKeywords = except_keyword.Text.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)).ToList();
                }

                // 검색 내 검색 모드 처리
                if (_isSubSearchMode && _baseSearchResults.Count > 0)
                {
                    await PerformSubSearch(columnCriteria, excludeKeywords, isAlreadyProgress);
                }
                else
                {
                    await PerformNormalSearch(columnCriteria, excludeKeywords, isAlreadyProgress);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"검색 수행 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 일반 검색 수행
        /// </summary>
        private async Task PerformNormalSearch(Dictionary<string, SearchColumnCriteria> columnCriteria, List<string> excludeKeywords, bool isAlreadyProgress)
        {
            if (isAlreadyProgress)
            {
                // 다중 컬럼 검색 수행
                await _clusteringManager.SearchMultipleColumnsAsync(columnCriteria, excludeKeywords);
            }
            else
            {
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "데이터 검색 시작");

                    await _clusteringManager.SearchMultipleColumnsAsync(columnCriteria, excludeKeywords);

                    await progressForm.UpdateProgressHandler(100, "데이터 검색 완료");
                    await Task.Delay(10);
                }
            }

            merge_all_check.Checked = false;
            change_row_count();
        }

        /// <summary>
        /// 검색 내 검색 수행
        /// </summary>
        private async Task PerformSubSearch(Dictionary<string, SearchColumnCriteria> columnCriteria, List<string> excludeKeywords, bool isAlreadyProgress)
        {
            // 전체 검색 수행
            var searchResult = await _clusteringManager.SearchMultipleColumnsAsync(columnCriteria, excludeKeywords);

            // 기준 검색 결과와 교집합
            var currentResults = _clusteringManager.GetCurrentResultClusterIds();
            var filteredResults = currentResults.Intersect(_baseSearchResults).ToList();

            Debug.WriteLine($"검색 내 검색 결과: 전체 {currentResults.Count}개 → 필터링 후 {filteredResults.Count}개");

            // 필터링된 결과로 UI 업데이트 (추가 구현 필요)
            await DisplayFilteredSubSearchResults(filteredResults);
        }

        /// <summary>
        /// 검색 내 검색 결과를 화면에 표시
        /// </summary>
        private async Task DisplayFilteredSubSearchResults(List<int> filteredClusterIds)
        {
            try
            {
                // ClusteringManager의 새 메서드 사용
                await _clusteringManager.DisplaySpecificClustersAsync(filteredClusterIds);

                merge_all_check.Checked = false;
                change_row_count();

                Debug.WriteLine($"검색 내 검색 결과 표시 완료: {filteredClusterIds?.Count ?? 0}개");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"검색 내 검색 결과 표시 오류: {ex.Message}");
                MessageBox.Show($"검색 내 검색 결과 표시 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void change_row_count()
        {
            int rowCount = merge_cluster_table.RowCount;

            cluster_count.Text = $"행 수  : {rowCount}";

            int unClusterCount = GetCountOfNegativeOneClusterIDs(DataHandler.finalClusteringData);
            string unClusterCountMoney = GetSumOfNegativeTotalMoney(DataHandler.finalClusteringData);
            uncluster_count.Text = $"미병합 Cluster  : {unClusterCount}";
            uncluster_count_money.Text = $"미병합 합산금액  : {unClusterCountMoney}";
        }

        /// <summary>
        /// 제외 키워드 목록 추출
        /// </summary>
        private List<string> GetExcludeKeywords()
        {
            if (string.IsNullOrEmpty(except_keyword.Text))
                return new List<string>();

            var excludeKeywords = except_keyword.Text.Split(',')
                                                  .Select(k => k.Trim())
                                                  .Where(k => !string.IsNullOrEmpty(k))
                                                  .ToList();

            Debug.WriteLine($"제외 키워드 {excludeKeywords.Count}개: {string.Join(", ", excludeKeywords)}");
            return excludeKeywords;
        }


        private async Task create_merge_keyword_list(bool isAlreadyProgress = false)
        {
            if (isAlreadyProgress)
            {
                string target_keyword = "";

                if (!"".Equals(merge_search_keyword.Text.ToString()) && merge_search_keyword.Text != null)
                {
                    target_keyword = merge_search_keyword.Text.ToString();
                }

                // 검색어가 없으면 전체 검색
                if (string.IsNullOrEmpty(target_keyword))
                {
                    await PerformSearchWithCriteria(new Dictionary<string, SearchColumnCriteria>(), isAlreadyProgress);
                    return;
                }

                // 키워드 파싱
                var parsedKeywords = ParseComplexKeywords(target_keyword);
                Debug.WriteLine($"파싱 결과 - AND: [{string.Join(", ", parsedKeywords.AndKeywords)}], OR: [{string.Join(", ", parsedKeywords.OrKeywords)}]");

                // 파싱된 키워드가 없으면 전체 검색
                if (parsedKeywords.AndKeywords.Count == 0 && parsedKeywords.OrKeywords.Count == 0)
                {
                    await PerformSearchWithCriteria(new Dictionary<string, SearchColumnCriteria>(), isAlreadyProgress);
                    return;
                }


                // *** 핵심 변경: 기존 DataHandler 함수 대신 ClusteringManager 사용 ***
                string searchColumn = GetSelectedSearchColumn();
                List<string> matchingKeywords;

                /*
                if (equalsSearchYN)
                {
                    // 완전일치 검색 - ClusteringManager 사용
                    matchingKeywords = _clusteringManager.SearchExact(searchColumn, target_keyword);
                    Debug.WriteLine($"완전일치 검색 결과: {matchingKeywords.Count}개 키워드");
                }
                else
                {
                    // 부분일치 검색 - ClusteringManager 사용
                    matchingKeywords = _clusteringManager.SearchContains(searchColumn, target_keyword);
                    Debug.WriteLine($"부분일치 검색 결과: {matchingKeywords.Count}개 키워드");
                }
                 // AND/OR 검색 조건 파싱
                bool useAndSearch = andSearchYN;

                Debug.WriteLine($"검색 실행 - 컬럼: {searchColumn}, 키워드: {target_keyword}, 완전일치: {equalsSearchYN}, AND: {useAndSearch}");

                */

                // 복합 조건 검색 수행
                //matchingKeywords = _clusteringManager.SearchWithComplexConditions(searchColumn, parsedKeywords, equalsSearchYN);
                // 복합 조건 검색 수행
                var matchingClusterIds = _clusteringManager.SearchWithComplexConditions(searchColumn, parsedKeywords, equalsSearchYN, _baseSearchResults, _isSubSearchMode);

                Debug.WriteLine($"복합 조건 검색 결과: {matchingClusterIds.Count}개 클러스터");

                // 클러스터 ID를 통해 키워드 목록 재구성 (기존 로직 호환을 위해)
                //matchingKeywords = GetKeywordsByClusterIds(matchingClusterIds, searchColumn);
                // 클러스터 ID를 통해 원래 검색 키워드와 매칭되는 키워드만 재구성
                //matchingKeywords = GetKeywordsByClusterIds(matchingClusterIds, searchColumn, parsedKeywords, equalsSearchYN);

                //Debug.WriteLine($"복합 조건 검색 결과: {matchingKeywords.Count}개 키워드");
                //Debug.WriteLine($"매칭된 키워드 목록: [{string.Join(", ", matchingKeywords)}]");


                // 다중 컬럼 검색 조건 구성
                var columnCriteria = new Dictionary<string, SearchColumnCriteria>();

                if (matchingClusterIds.Count > 0)
                {
                    /*
                    columnCriteria[searchColumn] = new SearchColumnCriteria
                    {
                        Keywords = matchingKeywords,
                        ExactMatch = true, // 이미 매칭된 키워드들이므로 정확 매칭
                        //UseAnd = useAndSearch
                        UseAnd = false
                    };

                    await PerformSearchWithCriteria(columnCriteria, isAlreadyProgress);
                    */
                    // PerformSearchWithCriteria 우회하고 직접 결과 표시
                    await _clusteringManager.DisplaySpecificClustersAsync(matchingClusterIds);

                    // 선택 상태 초기화
                    merge_all_check.Checked = false;
                    change_row_count();

                    Debug.WriteLine($"복합 조건 검색 완료: {matchingClusterIds.Count}개 클러스터 표시");
                }
                else
                {
                    // *** 수정: 검색 결과가 없을 때 빈 테이블 표시 ***
                    await ShowEmptySearchResult();
                    Debug.WriteLine("검색 결과 없음 - 빈 테이블 표시");
                }

            }
            else
            {
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();

                    string target_keyword = "";

                    if (!"".Equals(merge_search_keyword.Text.ToString()) && merge_search_keyword.Text != null)
                    {
                        target_keyword = merge_search_keyword.Text.ToString();
                    }

                    await progressForm.UpdateProgressHandler(10, "데이터 검색 시작");
                    await Task.Delay(10);

                    // 검색어가 없으면 전체 검색
                    if (string.IsNullOrEmpty(target_keyword))
                    {
                        await progressForm.UpdateProgressHandler(40, "전체 데이터 검색 중...");
                        await Task.Delay(10);

                        await PerformSearchWithCriteria(new Dictionary<string, SearchColumnCriteria>(), isAlreadyProgress);

                        await progressForm.UpdateProgressHandler(100, "전체 데이터 검색 완료");
                        await Task.Delay(10);
                        progressForm.Close();
                        return;
                    }
                    // 키워드 파싱
                    var parsedKeywords = ParseComplexKeywords(target_keyword);
                    Debug.WriteLine($"파싱 결과 - AND: [{string.Join(", ", parsedKeywords.AndKeywords)}], OR: [{string.Join(", ", parsedKeywords.OrKeywords)}]");

                    // 파싱된 키워드가 없으면 전체 검색
                    if (parsedKeywords.AndKeywords.Count == 0 && parsedKeywords.OrKeywords.Count == 0)
                    {
                        await progressForm.UpdateProgressHandler(40, "전체 데이터 검색 중...");
                        await Task.Delay(10);

                        await PerformSearchWithCriteria(new Dictionary<string, SearchColumnCriteria>(), isAlreadyProgress);

                        await progressForm.UpdateProgressHandler(100, "전체 데이터 검색 완료");
                        await Task.Delay(10);
                        progressForm.Close();
                        return;
                    }

                    // *** 핵심 변경: 기존 DataHandler 함수 대신 ClusteringManager 사용 ***
                    string searchColumn = GetSelectedSearchColumn();
                    List<string> matchingKeywords;

                    await progressForm.UpdateProgressHandler(20, $"'{searchColumn}' 컬럼에서 검색 중...");
                    await Task.Delay(10);
                    /*
                    if (equalsSearchYN)
                    {
                        // 완전일치 검색 - ClusteringManager 사용
                        matchingKeywords = _clusteringManager.SearchExact(searchColumn, target_keyword);
                        Debug.WriteLine($"완전일치 검색 결과: {matchingKeywords.Count}개 키워드");
                    }
                    else
                    {
                        // 부분일치 검색 - ClusteringManager 사용
                        matchingKeywords = _clusteringManager.SearchContains(searchColumn, target_keyword);
                        Debug.WriteLine($"부분일치 검색 결과: {matchingKeywords.Count}개 키워드");
                    }

                    // AND/OR 검색 조건 파싱
                    bool useAndSearch = andSearchYN;

                    Debug.WriteLine($"검색 실행 - 컬럼: {searchColumn}, 키워드: {target_keyword}, 완전일치: {equalsSearchYN}, AND: {useAndSearch}");
                    */

                    // 복합 조건 검색 수행
                    //matchingKeywords = _clusteringManager.SearchWithComplexConditions(searchColumn, parsedKeywords, equalsSearchYN);
                    // 복합 조건 검색 수행
                    //var matchingClusterIds = _clusteringManager.SearchWithComplexConditions(searchColumn, parsedKeywords, equalsSearchYN);
                    var matchingClusterIds = _clusteringManager.SearchWithComplexConditions(searchColumn, parsedKeywords, equalsSearchYN, _baseSearchResults, _isSubSearchMode);

                    Debug.WriteLine($"복합 조건 검색 결과: {matchingClusterIds.Count}개 클러스터");

                    // 클러스터 ID를 통해 키워드 목록 재구성 (기존 로직 호환을 위해)
                    //matchingKeywords = GetKeywordsByClusterIds(matchingClusterIds, searchColumn);
                    // 클러스터 ID를 통해 원래 검색 키워드와 매칭되는 키워드만 재구성
                    //matchingKeywords = GetKeywordsByClusterIds(matchingClusterIds, searchColumn, parsedKeywords, equalsSearchYN);

                    //Debug.WriteLine($"복합 조건 검색 결과: {matchingKeywords.Count}개 키워드");
                    //Debug.WriteLine($"매칭된 키워드 목록: [{string.Join(", ", matchingKeywords)}]");
                    // 다중 컬럼 검색 조건 구성
                    var columnCriteria = new Dictionary<string, SearchColumnCriteria>();

                    if (matchingClusterIds.Count > 0)
                    {
                        /*
                        columnCriteria[searchColumn] = new SearchColumnCriteria
                        {
                            Keywords = matchingKeywords,
                            ExactMatch = true, // 이미 매칭된 키워드들이므로 정확 매칭
                            //UseAnd = useAndSearch
                            UseAnd = false
                        };
                        */

                        await progressForm.UpdateProgressHandler(40, "데이터 검색 중...");
                        await Task.Delay(10);

                        //await PerformSearchWithCriteria(columnCriteria, isAlreadyProgress);

                        await _clusteringManager.DisplaySpecificClustersAsync(matchingClusterIds);

                        // 선택 상태 초기화
                        merge_all_check.Checked = false;
                        change_row_count();

                        Debug.WriteLine($"복합 조건 검색 완료: {matchingClusterIds.Count}개 클러스터 표시");
                    }
                    else
                    {
                        // *** 수정: 검색 결과가 없을 때 빈 테이블 표시 ***
                        await ShowEmptySearchResult();
                        Debug.WriteLine("검색 결과 없음 - 빈 테이블 표시");
                    }



                    await progressForm.UpdateProgressHandler(90, "데이터 검색 완료");
                    await Task.Delay(10);

                    await progressForm.UpdateProgressHandler(100);
                    await Task.Delay(10);
                    progressForm.Close();
                }
            }

        }


        private void create_check_keyword_list()
        {
            string target_keyword = "";

            if (!"".Equals(check_search_keyword.Text.ToString()) && check_search_keyword.Text != null)
            {
                target_keyword = check_search_keyword.Text.ToString();
            }

            List<string> MathcingPairs = new List<string>();
            try
            {
                if (!"".Equals(target_keyword))
                {
                    MathcingPairs = DataHandler.FindMachKeyword(check_keyword_list, target_keyword);
                    if (MathcingPairs.Count == 0)
                    {
                        merge_check_table.DataSource = null;
                        merge_check_table.Rows.Clear();
                        merge_check_table.Columns.Clear();
                        if (DataHandler.dragSelections.ContainsKey(merge_check_table))
                        {
                            DataHandler.dragSelections[merge_check_table].Clear();
                        }

                        return;
                    }
                    else
                    {
                        CreateCheckDataGridView(merge_check_table, DataHandler.finalClusteringData, MathcingPairs);
                    }

                }
                //전체 검색
                else
                {
                    CreateCheckDataGridView(merge_check_table, DataHandler.finalClusteringData, MathcingPairs);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }

        private void add_lv1_keyword()
        {
            // TextBox에 입력된 텍스트를 가져옴
            string inputText = new_lv1_word.Text.Trim();

            // 텍스트가 비어있지 않은 경우 ListBox에 추가
            if (!string.IsNullOrEmpty(inputText))
            {
                //DataHandler.separator.Add(inputText);
                _recomandKeywordManager.AddLv1Item(inputText);
                new_lv1_word.Clear(); // TextBox 초기화
            }

            List<string> lv1_list = _recomandKeywordManager.Lv1List
           .Distinct()  // 중복 제거
           .ToList();   // List로 변환

            //lv1 리스트 추가
            create_keyword_table(dataGridView_lv1, lv1_list);
        }


        private void add_reco_keyword()
        {
            // TextBox에 입력된 텍스트를 가져옴
            string inputText = new_reco_word.Text.Trim();

            // 텍스트가 비어있지 않은 경우 ListBox에 추가
            if (!string.IsNullOrEmpty(inputText))
            {
                //DataHandler.separator.Add(inputText);
                _recomandKeywordManager.AddKeyword(selectecLv1Name, inputText);
                new_reco_word.Clear(); // TextBox 초기화
            }

            Lv1Item selectedItem = _recomandKeywordManager.GetLv1Item(selectecLv1Name);


            if (selectedItem != null)
            {
                List<string> keywords = selectedItem.Keywords;
                create_keyword_table(dataGridView_recoman_keyword, keywords, false);
            }
        }

        // 키워드별 데이터를 저장할 클래스
        class KeywordData
        {
            public int Count { get; set; }
            public decimal TotalAmount { get; set; }
        }


        /////////////////////////////검색 헬퍼 메서드///////////////////////////////////


        // 키워드 파싱 메서드 추가
        private ParsedKeywords ParseComplexKeywords(string searchText)
        {
            var result = new ParsedKeywords();

            if (string.IsNullOrEmpty(searchText))
                return result;

            // | 기준으로 먼저 분리
            string[] orParts = searchText.Split('|');

            if (orParts.Length == 1)
            {
                // | 없음: A,B → AND 조건
                result.AndKeywords = searchText.Split(',')
                    .Select(k => k.Trim())
                    .Where(k => !string.IsNullOrEmpty(k))
                    .ToList();
            }
            else
            {
                // | 있음: A,B|C,D → A AND (B OR C OR D)
                // 첫 번째 부분에서 마지막 키워드 제외하고 AND
                var firstPart = orParts[0].Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)).ToList();
                if (firstPart.Count > 1)
                {
                    result.AndKeywords.AddRange(firstPart.Take(firstPart.Count - 1));
                    result.OrKeywords.Add(firstPart.Last());
                }
                else
                {
                    result.OrKeywords.AddRange(firstPart);
                }

                // 나머지 부분들은 모두 OR
                for (int i = 1; i < orParts.Length; i++)
                {
                    var keywords = orParts[i].Split(',')
                        .Select(k => k.Trim())
                        .Where(k => !string.IsNullOrEmpty(k));
                    result.OrKeywords.AddRange(keywords);
                }
            }

            return result;
        }

        // DataTable에서 ClusterID가 -1인 행 개수 구하기
        public int GetCountOfNegativeOneClusterIDs(DataTable dataTable)
        {
            // DataTable이 null인지 확인
            if (dataTable == null)
                return 0;

            // "ClusterID" 컬럼이 존재하는지 확인
            if (!dataTable.Columns.Contains("ClusterID"))
                return 0;

            // LINQ를 사용하여 ClusterID가 -1인 행 개수 계산
            int count = dataTable.AsEnumerable()
                                 .Count(row => row.Field<int>("ClusterID") == -1);

            return count;
        }

        public string GetSumOfNegativeTotalMoney(DataTable dataTable)
        {
            // DataTable이 null인지 확인
            if (dataTable == null)
                return FormatToKoreanUnit(0);

            // "ClusterID" 컬럼이 존재하는지 확인
            if (!dataTable.Columns.Contains("ClusterID"))
                return FormatToKoreanUnit(0);

            // "합산금액" 컬럼이 존재하는지 확인
            if (!dataTable.Columns.Contains("합산금액"))
                return FormatToKoreanUnit(0);

            // LINQ를 사용하여 ClusterID가 -1인 행들의 합산금액 총합 계산
            decimal sum = dataTable.AsEnumerable()
                                  .Where(row => row.Field<int>("ClusterID") == -1)
                                  .Sum(row => row.Field<decimal>("합산금액"));

            return FormatToKoreanUnit(sum);
        }

    }
}
