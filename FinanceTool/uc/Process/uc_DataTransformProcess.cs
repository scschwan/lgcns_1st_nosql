using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTool
{
    public partial class uc_DataTransform
    {

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
