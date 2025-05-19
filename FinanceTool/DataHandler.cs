using ClosedXML.Excel;
// 파일 상단에 추가할 네임스페이스
using FinanceTool.Models.MongoModels;
using FinanceTool.MongoModels;
using FinanceTool.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json
    ;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace FinanceTool
{
    public class DataHandler
    {
        //2025.02.17
        //fileload page 에서 정제된 table
        public static DataTable processTable = new DataTable();

        public static DataTable excelData = new DataTable();
        public static DataTable preprocessedData = new DataTable();
        public static DataTable lowLevelData = new DataTable();
        public static DataTable moneyDataTable = new DataTable();

        public static DataTable recomandKeywordTable = new DataTable();

        //2025.02.13
        //clustering 저장 테이블
        public static DataTable firstClusteringData = new DataTable();
        public static DataTable secondClusteringData = new DataTable();
        public static DataTable finalClusteringData = new DataTable();


        public static int moneyIndex = 0;
        public static List<int> levelList = new List<int>();
        public static List<string> levelName = new List<string>();
        /*
        public static HashSet<string> separator = new HashSet<string> { " ", ",", ".", "/", "(", ")", "_", "#", "~", "*", "[", "]", "!", ":", "%", "-", "'", "&" };

        public static HashSet<string> remover = new HashSet<string> { 
                                               // 월
                                               "12월", "11월", "10월", "9월", "8월", "7월", "6월", "5월", "4월", "3월", "2월", "1월",
                                               // 명수
                                               "9명", "8명", "7명", "6명", "5명", "4명", "3명", "2명", "1명", "0명",
                                               // 연도
                                               "1년", "2년", "3년", "4년", "5년", "6년", "7년", "8년", "9년",
                                               // 숫자
                                               "0", "1", "2", "3", "4", "5", "6", "7", "8", "9"
                                            };
        */

        // 다른 CS 파일에서
        //public static SeparatorManager spManager = new SeparatorManager();
        public static SeparatorManager spManager;

        public static string dept_col_name;
        public static string prod_col_name;
        public static string sub_acc_col_name;

        public static bool dept_col_yn = true;
        public static bool prod_col_yn = true;

        public static bool hiddenData = false;

        public static string tempFilePath = Path.Combine(Path.GetTempPath(), "finance_data_temp.json");
        public static Data.MongoDBManager mongoDBManager = Data.MongoDBManager.Instance;

        public static RawDataRepository rawDataRepo = new RawDataRepository();
        public static ProcessDataRepository processDataRepo = new ProcessDataRepository();
        public static ClusteringRepository clusteringRepo = new ClusteringRepository();

        public static List<string> visibleColumns  = new List<string>();

        

        public static async Task<DataTable> CreateDataTableFromColumnNamesAsync(DataTable sourceTable, List<string> columnNames)
        {
            // 새로운 DataTable을 생성
            DataTable resultTable = new DataTable();

            Debug.WriteLine($"Selected columns: {String.Join(", ", columnNames)}");
            Debug.WriteLine($"sourceTable.Columns.Count: {sourceTable.Columns.Count}");

            // 전달된 컬럼명에 대응하는 열 인덱스를 찾아서 추가
            foreach (string columnName in columnNames)
            {
                // 소스 테이블에서 컬럼 인덱스 찾기
                int columnIndex = sourceTable.Columns.IndexOf(columnName);
                if (columnIndex >= 0)
                {
                    // 컬럼을 찾으면 결과 테이블에 추가
                    Debug.WriteLine($"Found column: {columnName} at index {columnIndex}");
                    DataColumn sourceColumn = sourceTable.Columns[columnIndex];
                    resultTable.Columns.Add(columnName, sourceColumn.DataType);
                }
                else
                {
                    // 컬럼을 찾지 못한 경우 경고 메시지 기록
                    Debug.WriteLine($"Warning: Column {columnName} not found in source table");
                    // 선택적으로 예외를 발생시키거나 계속 진행할 수 있음
                    throw new ArgumentException($"Column '{columnName}' not found in source table");
                }
            }

            // raw_data_id 컬럼 추가 (string 타입으로)
            resultTable.Columns.Add("raw_data_id", typeof(string));

            // sourceTable에서 데이터를 가져와서 resultTable에 추가
            foreach (DataRow row in sourceTable.Rows)
            {
                DataRow newRow = resultTable.NewRow();

                // 선택된 각 컬럼의 데이터 복사
                for (int i = 0; i < columnNames.Count; i++)
                {
                    string columnName = columnNames[i];
                    if (sourceTable.Columns.Contains(columnName))
                    {
                        newRow[i] = row[columnName];
                    }
                }

                // raw_data_id 추가 (string 타입으로)
                if (sourceTable.Columns.Contains("raw_data_id") &&
                    row["raw_data_id"] != DBNull.Value)
                {
                    newRow["raw_data_id"] = row["raw_data_id"].ToString();
                }
                else
                {
                    newRow["raw_data_id"] = DBNull.Value;
                }

                resultTable.Rows.Add(newRow);
            }

            return resultTable;
        }

        //2025.02.17
        //preprocessing 에서 사용되는 함수
        //keyword 추출 dataTable 생성
        public static DataTable CombineDataTables(DataTable inputTable)
        {
            DataTable newTable = new DataTable();
            try
            {
                // 1. 필수 컬럼 존재 여부 확인
                if (!processTable.Columns.Contains(dept_col_name) ||
                    !processTable.Columns.Contains(prod_col_name))
                {
                    throw new Exception($"필수 컬럼이 없습니다. (부서: {dept_col_name}, 제품: {prod_col_name})");
                }

                // 2. 필수 컬럼 확인 및 추가
                if (!newTable.Columns.Contains(dept_col_name))
                    newTable.Columns.Add(dept_col_name, processTable.Columns[dept_col_name].DataType);

                if (!newTable.Columns.Contains(prod_col_name))
                    newTable.Columns.Add(prod_col_name, processTable.Columns[prod_col_name].DataType);

                // 3. inputTable의 모든 컬럼 추가
                foreach (DataColumn col in inputTable.Columns)
                {
                    if (col.ColumnName.Equals(dept_col_name) || col.ColumnName.Equals(prod_col_name))
                    {
                        Debug.WriteLine($"중복 컬럼 확인 col.ColumnName : {col.ColumnName}");
                        continue;
                    }

                    //Debug.WriteLine($"컬럼명 확인 col.ColumnName : {col.ColumnName}");

                    newTable.Columns.Add(col.ColumnName, col.DataType);
                    //Debug.WriteLine($"inputTable Columnsname : {col.ColumnName}");
                }

                // 4. 데이터 복사
                for (int i = 0; i < inputTable.Rows.Count; i++)
                {
                    DataRow newRow = newTable.NewRow();

                    // 같은 인덱스의 행에서 데이터 가져오기
                    if (i < processTable.Rows.Count)
                    {
                        newRow[dept_col_name] = processTable.Rows[i][dept_col_name];
                        newRow[prod_col_name] = processTable.Rows[i][prod_col_name];
                    }
                    else if (processTable.Rows.Count > 0)
                    {
                        // 인덱스가 범위를 벗어나면 첫 번째 행 데이터 사용
                        newRow[dept_col_name] = processTable.Rows[0][dept_col_name];
                        newRow[prod_col_name] = processTable.Rows[0][prod_col_name];
                    }
                    else
                    {
                        // processTable이 비어있을 경우
                        newRow[dept_col_name] = DBNull.Value;
                        newRow[prod_col_name] = DBNull.Value;
                    }

                    // inputTable의 현재 행 데이터 복사
                    foreach (DataColumn col in inputTable.Columns)
                    {
                        newRow[col.ColumnName] = inputTable.Rows[i][col.ColumnName];
                    }

                    newTable.Rows.Add(newRow);
                }

                return newTable;
            }
            catch (Exception ex)
            {
                throw new Exception($"테이블 생성 중 오류 발생: {ex.Message}");
            }
        }



        //2025.01.23
        //progress dialog 창
        public class ProgressDialog : Form
        {
            public ProgressBar progressBar;
            private Label statusLabel;

            public ProgressDialog()
            {
                InitializeComponents();
            }

            private void InitializeComponents()
            {
                this.Width = 400;
                this.Height = 120;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.StartPosition = FormStartPosition.CenterScreen;
                this.ControlBox = false;
                this.Text = "처리 중...";

               
                progressBar = new ProgressBar
                {
                    Style = ProgressBarStyle.Blocks,
                    Location = new Point(20, 20),
                    Width = 360,
                    Size = new System.Drawing.Size(340, 30),
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0
                };

                statusLabel = new Label
                {
                    Location = new Point(20, 60),
                    Size = new System.Drawing.Size(340, 20),                    
                    TextAlign = ContentAlignment.MiddleCenter,
                    Text = "데이터 처리 중... (0%)"
                };

                this.Controls.Add(progressBar);
                this.Controls.Add(statusLabel);
            }

            public async Task UpdateProgress(int percentage, string status = null)
            {
                if (InvokeRequired)
                {
                    await Invoke(async () => await UpdateProgress(percentage, status));
                    return;
                }

                progressBar.Value = percentage;
                statusLabel.Text = status ?? $"데이터 처리 중입니다... ({percentage}%)";

                if (percentage >= 100)
                {
                    statusLabel.Text = "처리가 완료되었습니다. (100%)";
                    await Task.Delay(500); // 0.5초 대기
                }
            }
        }

        public static async Task<DataTable> SplitColumnByModel(DataTable inputTable, int limit)
        {
            if (inputTable == null || inputTable.Columns.Count < 1)
            {
                throw new ArgumentException("입력 DataTable이 유효하지 않습니다.");
            }

            using var progress = new ProgressDialog();
            DataTable resultTable = null;

            var processTask = Task.Run(async () =>
            {
                var extractor = new KeywordExtractor(1); // exe 파일 사용
                                                         //var extractor = new KeywordExtractor(0); // python_code 파일 사용
                resultTable = await extractor.ExtractKeywordsFromDataTable(inputTable, 0, limit, 10000,
                                    new Progress<int>(async value => await progress.UpdateProgress(value)));
            });

            progress.Show();
            await processTask;
            await Task.Delay(500); // 완료 후 0.5초 대기
            progress.Close();

            Debug.WriteLine($"처리 결과: {resultTable.Rows.Count}행, {resultTable.Columns.Count}열");

            // 결과 검증
            foreach (DataRow row in resultTable.Rows)
            {
                var rowValues = row.ItemArray.Select(x => x?.ToString() ?? "").ToList();
                //Debug.WriteLine($"행 데이터: {string.Join(", ", rowValues)}");
            }

            return resultTable;
        }

        public static DataTable ReplaceSeparatorInColumn(DataTable inputTable, int columnIndex, string target, string mode)
        {
            // 입력 DataTable이 비어 있거나 열 인덱스가 유효하지 않은 경우 처리
            if (inputTable == null || inputTable.Columns.Count <= columnIndex)
            {
                Debug.WriteLine($"inputTable.Columns.Count : {inputTable.Columns.Count} , columnIndex : {columnIndex}");
                throw new ArgumentException("유효하지 않은 DataTable 또는 열 인덱스입니다.");
            }

            // 새로운 DataTable 생성 및 대상 열 추가
            DataTable outputTable = new DataTable();
            outputTable.Columns.Add(inputTable.Columns[columnIndex].ColumnName, typeof(string)); // 대상 열 이름 유지

            // 각 행의 데이터를 처리하여 새로운 DataTable에 추가
            foreach (DataRow row in inputTable.Rows)
            {
                if (row[columnIndex] != DBNull.Value) // 값이 null이 아닌 경우 처리
                {
                    string originalValue = row[columnIndex].ToString();
                    string modifiedValue = ReplaceSeparators(originalValue, target, mode);
                    Console.WriteLine("origin : " + originalValue);
                    Console.WriteLine("modifiedValue : " + modifiedValue);

                    // 수정된 값을 새로운 DataTable에 추가
                    outputTable.Rows.Add(modifiedValue);
                }
                else
                {
                    // null 값은 그대로 추가
                    outputTable.Rows.Add(DBNull.Value);
                    Console.WriteLine("Null");
                }
            }
            return outputTable;
        }

        
        private static string ReplaceSeparators(string input, string target, string mode)
        {
            if (mode == "separate")
            {
                //Debug.WriteLine($"manager.Separators : {string.Join(",", spManager.Separators)}");
                //Debug.WriteLine($"manager.getSeparator; : {string.Join(",", manager.getSeparators())}");
                // List<string> separator를 사용
                //foreach (string sep in manager.Separators)
                foreach (string sep in spManager.Separators)
                {
                    input = input.Replace(sep, target);
                }
            }
            else if (mode == "remove")
            {
                // List<string> remover를 사용
                foreach (string rem in spManager.Removers)
                {
                    input = input.Replace(rem, target);
                }
            }
            return input;
        }

        public static DataTable SplitColumnBySeparator(DataTable inputTable, string separator)
        {
            // 입력 DataTable이 유효한지 확인
            if (inputTable == null || inputTable.Columns.Count != 1)
            {
                throw new ArgumentException("입력 DataTable은 반드시 하나의 열만 포함해야 합니다.");
            }

            if (string.IsNullOrEmpty(separator))
            {
                throw new ArgumentException("separator는 null이거나 빈 문자열일 수 없습니다.");
            }

            // 새로운 DataTable 생성
            DataTable outputTable = new DataTable();

            // 각 행의 데이터를 읽고 separator 기준으로 분리
            foreach (DataRow row in inputTable.Rows)
            {
                if (row[0] != DBNull.Value) // 값이 null이 아닌 경우
                {
                    string[] splitValues = row[0].ToString().Split(new string[] { separator }, StringSplitOptions.None);

                    // 분리된 값들의 개수에 따라 열 추가
                    while (outputTable.Columns.Count < splitValues.Length)
                    {
                        //outputTable.Columns.Add($"Column{outputTable.Columns.Count + 1}", typeof(string));
                        outputTable.Columns.Add($"Column{outputTable.Columns.Count}", typeof(string));
                    }

                    // 새 행 추가
                    DataRow newRow = outputTable.NewRow();
                    for (int i = 0; i < splitValues.Length; i++)
                    {
                        newRow[i] = splitValues[i];
                    }
                    outputTable.Rows.Add(newRow);
                }
                else
                {
                    // null 값 처리: 빈 행 추가
                    DataRow emptyRow = outputTable.NewRow();
                    outputTable.Rows.Add(emptyRow);
                }
            }

            return outputTable;
        }

      

        public static DataTable ProcessUnderscoresInAllColumn(DataTable inputTable)
        {
            // 입력 검증: DataTable이 null이거나 열이 0개일 경우 예외 발생
            if (inputTable == null || inputTable.Columns.Count == 0)
            {
                throw new ArgumentException("입력 DataTable은 반드시 열이 하나 이상 있어야 합니다.");
            }

            // 새로운 DataTable 생성
            DataTable outputTable = new DataTable();

            // 기존 열의 이름과 타입을 유지하면서 새로운 DataTable의 열을 추가
            foreach (DataColumn column in inputTable.Columns)
            {
                outputTable.Columns.Add(column.ColumnName, column.DataType);
            }

            // 각 행의 데이터를 처리
            foreach (DataRow row in inputTable.Rows)
            {
                // 새로운 행을 생성
                DataRow newRow = outputTable.NewRow();

                // 각 열의 값을 처리
                for (int colIndex = 0; colIndex < inputTable.Columns.Count; colIndex++)
                {
                    if (row[colIndex] != DBNull.Value) // 값이 null이 아닌 경우 처리
                    {
                        string originalValue = row[colIndex].ToString();
                        string modifiedValue = ProcessString(originalValue);

                        // 처리된 값을 새로운 행에 추가
                        newRow[colIndex] = modifiedValue;
                    }
                    else
                    {
                        // null 값은 그대로 추가
                        newRow[colIndex] = DBNull.Value;
                    }
                }

                // 처리된 행을 새로운 DataTable에 추가
                outputTable.Rows.Add(newRow);
            }

            return outputTable;
        }

        private static string ProcessString(string input)
        {
            // 앞뒤의 "_" 제거
            string trimmed = input.Trim('_');

            // 연속된 "_"를 하나로 축소
            string collapsed = Regex.Replace(trimmed, "_+", "_");

            return collapsed;
        }

        public static List<string> GetColumnValuesAsList(DataTable table, int columnIndex)
        {
            // 반환할 리스트 초기화
            List<string> result = new List<string>();

            // DataTable의 행을 순회하며 특정 열의 값을 리스트에 추가
            foreach (DataRow row in table.Rows)
            {
                // 열의 값을 문자열로 변환하여 추가 (null 값은 빈 문자열로 처리)
                result.Add(row[columnIndex]?.ToString() ?? string.Empty);
            }

            return result;
        }

     

        //2025.02.13
        //키워드 기준 매칭 데이터 비교
        public static List<string> FindMachKeyword(List<string> listA, string search_keyword)
        {
            // 결과를 저장할 리스트
            List<string> output = new List<string>();

            // B의 각 값을 A와 비교
            foreach (string valueA in listA)
            {
                
                
                // A와 B의 값이 서로 포함 관계인지 확인
                if (CompareByTwoChars(search_keyword, valueA))
                {
                    // 포함 관계가 있으면 출력 리스트에 추가
                    //Debug.WriteLine($"포함 키워드 대상 감지 : search_keyword : {search_keyword} valueA : {valueA}");
                    output.Add(valueA);
                }
            }
            
            //Debug.WriteLine($"output List : {string.Join(",", output)}");

            return output;
        }

        public static List<string> FindMachEqualsKeyword(List<string> listA, string search_keyword)
        {
            // 결과를 저장할 리스트
            List<string> output = new List<string>();

            // search_keyword가 쉼표를 포함하는지 확인
            if (search_keyword.Contains(","))
            {
                // 쉼표로 분리하여 리스트로 변환
                List<string> searchKeywords = search_keyword.Split(',')
                                                           .Select(k => k.Trim())
                                                           .Where(k => !string.IsNullOrEmpty(k))
                                                           .ToList();

                // 각 검색 키워드에 대해
                foreach (string keyword in searchKeywords)
                {
                    foreach (string valueA in listA)
                    {
                        // 정확히 일치하는지 확인
                        if (keyword.Equals(valueA))
                        {
                            // 일치하면 출력 리스트에 추가
                            output.Add(valueA);
                            break; // 현재 keyword에 대한 검색 종료
                        }
                    }
                }
            }
            else
            {
                // 쉼표가 없는 경우 기존 로직 유지
                foreach (string valueA in listA)
                {
                    if (search_keyword.Equals(valueA))
                    {
                        output.Add(valueA);
                        break;
                    }
                }
            }

            return output;
        }



        public static void SaveDataTableToExcel(DataTable firstTable, DataTable secondTable = null)
        {
            try
            {
                // SaveFileDialog 생성
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
                    saveFileDialog.Title = "Save Excel File";
                    saveFileDialog.DefaultExt = "xlsx";
                    saveFileDialog.AddExtension = true;

                    // 대화 상자를 띄워 사용자로부터 경로를 입력받음
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = saveFileDialog.FileName;

                        // 엑셀 파일 생성 및 저장
                        using (var workbook = new XLWorkbook())
                        {
                            // 첫 번째 테이블은 항상 추가
                            var firstSheet = workbook.Worksheets.Add(firstTable, "Clustering 결과");

                            // 두 번째 테이블이 있으면 추가
                            if (secondTable != null && secondTable.Rows.Count > 0)
                            {
                                var secondSheet = workbook.Worksheets.Add(secondTable, "Clustering 원본");
                            }

                            workbook.SaveAs(filePath);

                            /*
                            string message = secondTable != null ?
                                "Excel file이 두 개의 시트로 생성되었습니다." :
                                "Excel file이 생성되었습니다.";
                            */
                            string message = "Excel file이 생성되었습니다.";
                            MessageBox.Show($"{message}\n{filePath}",
                                           "Success",
                                           MessageBoxButtons.OK,
                                           MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel file 생성이 실패하였습니다:\n{ex.Message}",
                               "Error",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
            }
        }

        public static DataTable ExtractColumnToNewTable(DataTable inputTable, int index)
        {
            // 유효성 검사
            if (inputTable == null)
                throw new ArgumentNullException(nameof(inputTable));
            if (index < 0 || index >= inputTable.Columns.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "유효하지 않은 열 인덱스입니다.");

            // 새로운 데이터테이블 생성
            DataTable resultTable = new DataTable();
            resultTable.Columns.Add(inputTable.Columns[index].ColumnName, inputTable.Columns[index].DataType);

            // 여기서 raw_data_id 컬럼 타입을 decimal 대신 string으로 변경
            resultTable.Columns.Add("raw_data_id", typeof(string)); // decimal -> string으로 변경

            // 데이터 복사
            foreach (DataRow row in inputTable.Rows)
            {
                DataRow newRow = resultTable.NewRow();
                newRow[0] = row[index];

                // raw_data_id도 추가 - 문자열로 저장
                if (row["raw_data_id"] != null && row["raw_data_id"] != DBNull.Value)
                {
                    newRow[1] = row["raw_data_id"].ToString(); // 문자열로 변환하여 저장
                }
                else
                {
                    newRow[1] = DBNull.Value;
                }

                resultTable.Rows.Add(newRow);
            }

            return resultTable;
        }

        public static DataTable ProcessShortStringsToNull(DataTable inputTable )
        {
            // 입력 검증: DataTable이 null이면 예외 발생
            if (inputTable == null)
            {
                throw new ArgumentException("입력 DataTable은 null일 수 없습니다.");
            }

            // 새로운 DataTable 생성 (기존 DataTable의 구조 유지)
            DataTable outputTable = inputTable.Copy();  // 구조와 데이터를 복사

            // 각 행의 데이터를 처리
            foreach (DataRow row in outputTable.Rows)
            {
                
                // 각 열을 순회
                for (int colIndex = 0; colIndex < outputTable.Columns.Count; colIndex++)
                {
                    // 셀 값이 null이 아니고 글자 길이가 1 이하면 null로 변경
                    if (row[colIndex] != DBNull.Value)
                    {
                        string cellValue = row[colIndex].ToString();
                        if (cellValue.Length <= 1)
                        {
                            row[colIndex] = DBNull.Value;  // 글자 길이가 1 이하인 경우 null로 변경
                        }
                    }
                }
               
                
            }

            return outputTable;
        }


        //2025.02.13
        //키워드 비교 함수
        //2글자씩 slice 하여 비교
        public static bool CompareByTwoChars(string baseWord, string targetWord)
        {

            if (targetWord.Length < 2)
            {
                return false;
            }

            // 2글자 미만인 경우 처리
            if (baseWord.Length < 2 )
            {
                //return false;
                return targetWord.Contains(baseWord);

            }

            // 기준 단어를 2글자씩 자르기
            List<string> baseParts = new List<string>();
            for (int i = 0; i < baseWord.Length - 1; i++)
            {
                baseParts.Add(baseWord.Substring(i, 2));
            }

            // 대상 단어를 2글자씩 자르기
            List<string> targetParts = new List<string>();
            for (int i = 0; i < targetWord.Length - 1; i++)
            {
                targetParts.Add(targetWord.Substring(i, 2));
            }

            // 두 리스트 간에 공통된 2글자 조합이 있는지 확인
            return baseParts.Any(b => targetParts.Contains(b));
        }

        //2025.02.13
        //dataTable Clustering 함수 구현
        //2025.05.12 -> mongodb 변환에 따라 삭제 예정

        public static async Task<DataTable> CreateSetGroupDataTableAsync(DataTable sourceTable, DataTable moneyDataTable, bool secondyn = false)
        {
            // 시작 시간 측정 (성능 모니터링용)
            var stopwatch = Stopwatch.StartNew();
            Debug.WriteLine("CreateSetGroupDataTableAsync 수행 시작");
            Debug.WriteLine($"sourceTable 행 수: {sourceTable.Rows.Count}");

            // 결과 DataTable 생성 - 컬럼 구조 명확히 정의
            DataTable resultTable = new DataTable();
            resultTable.Columns.Add("ID", typeof(int));
            resultTable.Columns.Add("ClusterID", typeof(int));
            resultTable.Columns.Add("클러스터명", typeof(string));
            resultTable.Columns.Add("키워드목록", typeof(string));
            resultTable.Columns.Add("Count", typeof(int));
            resultTable.Columns.Add("합산금액", typeof(decimal));
            resultTable.Columns.Add("dataIndex", typeof(string));

            try
            {
                // 성능 최적화: 미리 충분한 용량 할당
                resultTable.MinimumCapacity = Math.Max(100, sourceTable.Rows.Count / 10);

                // 금액 정보를 저장할 딕셔너리 (raw_data_id -> 금액)
                Dictionary<string, decimal> moneyLookup = new Dictionary<string, decimal>(sourceTable.Rows.Count);

                // 1. 먼저 moneyDataTable에서 금액 정보 로드 (기존 로직)
                if (moneyDataTable != null && moneyDataTable.Columns.Count > 0)
                {
                    // 금액 컬럼명 가져오기
                    string moneyColumnName = moneyDataTable.Columns[0].ColumnName;

                    foreach (DataRow row in moneyDataTable.Rows)
                    {
                        if (row["raw_data_id"] != DBNull.Value && row[moneyColumnName] != DBNull.Value)
                        {
                            string rawDataId = row["raw_data_id"].ToString();
                            if (!string.IsNullOrEmpty(rawDataId) &&
                                decimal.TryParse(row[moneyColumnName].ToString(), out decimal money))
                            {
                                moneyLookup[rawDataId] = money;
                            }
                        }
                    }

                    Debug.WriteLine($"moneyDataTable에서 로드한 금액 정보: {moneyLookup.Count}개");
                }

                // 2. MongoDB process_view_data 컬렉션에서 money 정보 로드 (신규 추가)
                var processViewRepo = new Repositories.ProcessViewRepository();

                // 필요한 raw_data_id 목록 추출
                HashSet<string> neededIds = new HashSet<string>();
                foreach (DataRow row in sourceTable.Rows)
                {
                    if (row["raw_data_id"] != DBNull.Value)
                    {
                        string rawDataId = row["raw_data_id"].ToString();
                        if (!string.IsNullOrEmpty(rawDataId))
                        {
                            neededIds.Add(rawDataId);
                        }
                    }
                }

                // 아직 금액 정보가 없는 ID만 필터링
                var missingMoneyIds = neededIds
                    .Where(id => !moneyLookup.ContainsKey(id))
                    .ToList();

                Debug.WriteLine($"process_view_data에서 로드할 금액 정보: {missingMoneyIds.Count}개");

                // 배치 처리로 MongoDB에서 금액 정보 로드
                if (missingMoneyIds.Count > 0)
                {
                    const int MongoDBbatchSize = 1000;

                    // MongoDB 연결 확인
                    await Data.MongoDBManager.Instance.EnsureInitializedAsync();

                    for (int i = 0; i < missingMoneyIds.Count; i += MongoDBbatchSize)
                    {
                        int currentBatchSize = Math.Min(MongoDBbatchSize, missingMoneyIds.Count - i);
                        var batchIds = missingMoneyIds.GetRange(i, currentBatchSize);

                        // ID 목록으로 process_view_data 조회
                        var filter = Builders<MongoModels.ProcessViewDocument>.Filter.In(d => d.RawDataId, batchIds);
                        var processViewDocs = await processViewRepo.FindDocumentsAsync(filter);

                        // 조회된 데이터에서 money 정보 추출
                        foreach (var doc in processViewDocs)
                        {
                            if (!string.IsNullOrEmpty(doc.RawDataId) && doc.Money != null)
                            {
                                decimal amount = 0;

                                // Money 필드 타입에 따른 처리 (다양한 타입 지원)
                                if (doc.Money is decimal decimalAmount)
                                {
                                    amount = decimalAmount;
                                }
                                else if (doc.Money is double doubleAmount)
                                {
                                    amount = (decimal)doubleAmount;
                                }
                                else if (doc.Money is int intAmount)
                                {
                                    amount = intAmount;
                                }
                                else if (doc.Money is long longAmount)
                                {
                                    amount = longAmount;
                                }
                                else if (doc.Money is string strAmount && decimal.TryParse(strAmount, out decimal parsedAmount))
                                {
                                    amount = parsedAmount;
                                }
                                else
                                {
                                    // 다른 타입인 경우 ToString 후 파싱 시도
                                    string moneyStr = doc.Money.ToString();
                                    if (!string.IsNullOrEmpty(moneyStr) && decimal.TryParse(moneyStr, out decimal parsedValue))
                                    {
                                        amount = parsedValue;
                                    }
                                }

                                // 파싱된 금액이 0이 아니면 저장
                                if (amount != 0)
                                {
                                    moneyLookup[doc.RawDataId] = amount;
                                }
                            }
                        }
                    }

                    Debug.WriteLine($"process_view_data에서 로드한 금액 정보: {moneyLookup.Count}개");
                }

                Debug.WriteLine($"금액 정보 로드 완료: {moneyLookup.Count}개, 소요 시간: {stopwatch.ElapsedMilliseconds}ms");

                // 부서/공급업체 정보를 위한 딕셔너리 (명확한 크기 지정)
                Dictionary<string, string> deptLookup = new Dictionary<string, string>(sourceTable.Rows.Count);
                Dictionary<string, string> prodLookup = new Dictionary<string, string>(sourceTable.Rows.Count);

                // 부서/공급업체 정보가 필요한 경우에만 MongoDB에서 로드
                if (secondyn && (dept_col_yn || prod_col_yn))
                {
                    // MongoDB 연결 확인
                    bool mongoConnected = await Data.MongoDBManager.Instance.EnsureInitializedAsync();
                    if (!mongoConnected)
                    {
                        throw new Exception("MongoDB 연결에 실패했습니다.");
                    }

                    // ProcessView 저장소에서 부서/공급업체 정보 로드
                    
                    var processDataRepo = new Repositories.ProcessDataRepository();

                   

                    // 성능 최적화: 필요한 ID만 쿼리하는 필터 사용
                    if (neededIds.Count > 0)
                    {
                        // 배치 처리 도입: 대량 데이터 처리 최적화
                        const int mongoBatchSize = 1000; // MongoDB 권장 최대 배치 크기

                        foreach (var idBatch in BatchIdsForQuery(neededIds, mongoBatchSize))
                        {
                            var filter = Builders<MongoModels.ProcessViewDocument>.Filter.In(d => d.RawDataId, idBatch);
                            var batchDocs = await processViewRepo.FindDocumentsAsync(filter);

                            foreach (var doc in batchDocs)
                            {
                                if (!string.IsNullOrEmpty(doc.RawDataId))
                                {
                                    if (dept_col_yn && !string.IsNullOrEmpty(doc.Department))
                                    {
                                        deptLookup[doc.RawDataId] = doc.Department;
                                    }
                                    if (prod_col_yn && !string.IsNullOrEmpty(doc.Supplier))
                                    {
                                        prodLookup[doc.RawDataId] = doc.Supplier;
                                    }
                                }
                            }
                        }
                    }

                    Debug.WriteLine($"부서 정보 캐싱: {deptLookup.Count}개, 공급업체 정보 캐싱: {prodLookup.Count}개");
                }

                // 집합 셋을 관리할 딕셔너리
                Dictionary<string, (int ID, int Count, decimal SumValue, HashSet<string> SourceIndices)> setGroups =
                    new Dictionary<string, (int, int, decimal, HashSet<string>)>();

                // 그룹 ID 카운터
                int nextGroupId = 0;

                // 시스템 리소스에 맞게 병렬 처리 최적화
                int batchSize = CalculateOptimalBatchSize(sourceTable.Rows.Count);
                int processorCount = Environment.ProcessorCount;
                int maxDegreeOfParallelism = Math.Max(1, processorCount - 1);

                Debug.WriteLine($"병렬 처리 설정: 최대 {maxDegreeOfParallelism}개 스레드, 배치 크기 {batchSize}");

                // 데이터 그룹화 - 병렬 처리 최적화, 더 작은 배치 크기로 처리
                var lockObj = new object();
                var rowBatches = SplitIntoOptimalBatches(sourceTable.Rows.Count, batchSize);

                await Task.Run(() => {
                    Parallel.ForEach(rowBatches,
                        new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                        batchRange => {
                            // 배치별 로컬 Dictionary (병합 전 임시 저장용)
                            var batchGroups = new Dictionary<string, (int Count, decimal SumValue, HashSet<string> Ids)>();

                            for (int rowIndex = batchRange.Start; rowIndex < batchRange.End; rowIndex++)
                            {
                                if (rowIndex >= sourceTable.Rows.Count) continue;

                                DataRow row = sourceTable.Rows[rowIndex];

                                // raw_data_id 가져오기 및 유효성 검사
                                if (row["raw_data_id"] == DBNull.Value)
                                    continue;

                                string rawDataId = row["raw_data_id"].ToString();
                                if (string.IsNullOrEmpty(rawDataId))
                                    continue;

                                // 키워드 집합 생성
                                List<string> setElements = new List<string>();

                                // 키워드 컬럼 처리
                                foreach (DataColumn col in sourceTable.Columns)
                                {
                                    // 메타데이터 컬럼 제외
                                    if (IsMetaDataColumn(col.ColumnName))
                                        continue;

                                    if (row[col] != DBNull.Value && !string.IsNullOrWhiteSpace(row[col].ToString()))
                                    {
                                        setElements.Add(row[col].ToString().Trim());
                                    }
                                }

                                // 부서/공급업체 정보 추가 (필요시)
                                if (secondyn && dept_col_yn && deptLookup.TryGetValue(rawDataId, out string deptValue))
                                {
                                    setElements.Add(deptValue);
                                }

                                if (secondyn && prod_col_yn && prodLookup.TryGetValue(rawDataId, out string prodValue))
                                {
                                    setElements.Add(prodValue);
                                }

                                // 키워드가 없는 경우 건너뛰기
                                if (setElements.Count == 0)
                                    continue;

                                // 집합을 정렬하여 일관성 유지
                                setElements.Sort();

                                // 집합 셋 문자열 생성
                                string setKey = string.Join(",", setElements);

                                // 금액 정보 조회
                                decimal refValue = 0;
                                moneyLookup.TryGetValue(rawDataId, out refValue);

                                // 배치별 로컬 Dictionary에 추가 또는 업데이트
                                if (!batchGroups.ContainsKey(setKey))
                                {
                                    batchGroups[setKey] = (1, refValue, new HashSet<string> { rawDataId });
                                }
                                else
                                {
                                    var existing = batchGroups[setKey];
                                    existing.Ids.Add(rawDataId);
                                    batchGroups[setKey] = (existing.Count + 1, existing.SumValue + refValue, existing.Ids);
                                }
                            }

                            // 전역 setGroups로 병합 (lock 사용)
                            lock (lockObj)
                            {
                                foreach (var entry in batchGroups)
                                {
                                    string setKey = entry.Key;
                                    var batchGroup = entry.Value;

                                    if (!setGroups.ContainsKey(setKey))
                                    {
                                        int id = nextGroupId++;
                                        setGroups[setKey] = (id, batchGroup.Count, batchGroup.SumValue, batchGroup.Ids);
                                    }
                                    else
                                    {
                                        var existing = setGroups[setKey];
                                        // 기존 HashSet에 새 ID 추가
                                        foreach (var id in batchGroup.Ids)
                                        {
                                            existing.SourceIndices.Add(id);
                                        }
                                        setGroups[setKey] = (
                                            existing.ID,
                                            existing.Count + batchGroup.Count,
                                            existing.SumValue + batchGroup.SumValue,
                                            existing.SourceIndices
                                        );
                                    }
                                }
                            }
                        });
                });

                Debug.WriteLine($"데이터 그룹화 완료: {setGroups.Count}개 그룹, 소요 시간: {stopwatch.ElapsedMilliseconds}ms");

                // 결과 DataTable에 행 추가 - 단일 스레드로 안전하게 처리
                // ID로 정렬
                var sortedGroups = setGroups.OrderBy(g => g.Value.ID).ToList();

                foreach (var group in sortedGroups)
                {
                    // 그룹 요소 배열
                    string setKey = group.Key;
                    string[] elements = setKey.Split(',');
                    var groupValue = group.Value;

                    try
                    {
                        // 핵심 버그 수정 부분: resultTable.NewRow() 호출 전에 테이블 상태 확인
                        if (resultTable.Columns.Count < 7)
                        {
                            Debug.WriteLine($"오류: 결과 테이블 컬럼 부족 - 현재 {resultTable.Columns.Count}개 컬럼 존재");
                            continue;
                        }

                        // 새 행 생성 (스레드 안전하게 처리)
                        DataRow newRow = resultTable.NewRow();

                        // 모든 컬럼에 값 명시적 할당
                        newRow["ID"] = groupValue.ID;
                        newRow["ClusterID"] = -1;
                        newRow["클러스터명"] = string.Join("_", elements);
                        newRow["키워드목록"] = setKey;
                        newRow["Count"] = groupValue.Count;
                        newRow["합산금액"] = groupValue.SumValue;
                        newRow["dataIndex"] = string.Join(",", groupValue.SourceIndices);

                        // 행 추가 (안전하게)
                        resultTable.Rows.Add(newRow);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"행 생성 중 오류: {ex.Message} - 그룹 ID: {groupValue.ID}");
                        // 오류 발생해도 계속 진행
                    }
                }

                Debug.WriteLine($"결과 테이블 생성 완료: {resultTable.Rows.Count}개 행, 총 소요 시간: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CreateSetGroupDataTableAsync 오류: {ex.Message}\n{ex.StackTrace}");

                // 오류 발생 시 빈 테이블 반환하지 않고 기본 컬럼만 있는 테이블 반환
                if (resultTable.Rows.Count == 0)
                {
                    Debug.WriteLine("오류 발생으로 인해 빈 결과 테이블 반환");
                }
            }

            return resultTable;
        }

        // 헬퍼 메서드들
        private static bool IsMetaDataColumn(string columnName)
        {
            // 메타데이터 컬럼 목록
            string[] metaColumns = { "raw_data_id", "id", "process_data_id", "import_date" };
            return metaColumns.Contains(columnName);
        }

        private static int CalculateOptimalBatchSize(int totalItems)
        {
            // 최적의 배치 크기 계산 (항목 수 기준)
            if (totalItems < 10000) return 100;
            if (totalItems < 100000) return 1000;
            return 2000;
        }

        private static List<(int Start, int End)> SplitIntoOptimalBatches(int totalItems, int batchSize)
        {
            var batches = new List<(int Start, int End)>();
            for (int i = 0; i < totalItems; i += batchSize)
            {
                int end = Math.Min(i + batchSize, totalItems);
                batches.Add((i, end));
            }
            return batches;
        }

        private static IEnumerable<List<string>> BatchIdsForQuery(HashSet<string> ids, int batchSize)
        {
            var idList = ids.ToList();
            for (int i = 0; i < idList.Count; i += batchSize)
            {
                yield return idList.Skip(i).Take(batchSize).ToList();
            }
        }

        //2025.02.18
        //clustering 결과 datagridview 생성 함수
        public static void SetupDataGridView(DataGridView dgv, DataTable dt)
        {
            // 조건에 맞는 데이터만 필터링
            var filteredData = dt.AsEnumerable()
                .Where(row =>
                    Convert.ToInt32(row["ClusterID"]) <= 0 ||
                    Convert.ToInt32(row["ClusterID"]) == Convert.ToInt32(row["ID"]))
                .CopyToDataTable();

            dgv.DataSource = filteredData;

            // ID 컬럼 숨기기
            if (dgv.Columns["ID"] != null)
            {
                dgv.Columns["ID"].Visible = false;
            }

            // ClusterID 컬럼 숨기기
            dgv.Columns["ClusterID"].Visible = false;

            // dataIndex 컬럼 숨기기
            dgv.Columns["dataIndex"].Visible = false;

            // Count 컬럼 형식 지정
            if (dgv.Columns["Count"] != null)
            {
                dgv.Columns["Count"].DefaultCellStyle.Format = "N0"; // 천 단위 구분자
                dgv.Columns["Count"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            // 합산금액 컬럼 형식 지정
            if (dgv.Columns["합산금액"] != null)
            {
                dgv.Columns["합산금액"].DefaultCellStyle.Format = "N0"; // 천 단위 구분자
                dgv.Columns["합산금액"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            if (dgv.Columns["클러스터명"] != null)
            {
                dgv.Columns["클러스터명"].ReadOnly = true;
            }

            // 나머지 컬럼들은 읽기 전용
            if (dgv.Columns["키워드목록"] != null)
            {
                dgv.Columns["키워드목록"].ReadOnly = true;
            }

            // 기본 설정
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;

            // MongoDB 컬렉션에 클러스터링 결과 저장
            //SaveClusteringResultsToMongoDB(filteredData);
        }

       

        private static bool isProcessingSelection = false;

        public static void SyncDataGridViewSelections(DataGridView dataGridView1, DataGridView dataGridView2)
        {
            // 첫 번째 DataGridView의 SelectionChanged 이벤트 핸들러
            dataGridView1.SelectionChanged += (sender, e) =>
            {
                if (isProcessingSelection) return;  // 재귀적 호출 방지

                try
                {
                    isProcessingSelection = true;

                    if (dataGridView1.CurrentRow != null)
                    {
                        int selectedIndex = dataGridView1.CurrentRow.Index;

                        // 두 번째 DataGridView에 같은 행 인덱스가 있는지 확인
                        if (selectedIndex < dataGridView2.Rows.Count)
                        {
                            dataGridView2.ClearSelection();
                            dataGridView2.Rows[selectedIndex].Selected = true;
                            dataGridView2.CurrentCell = dataGridView2.Rows[selectedIndex].Cells[0];
                        }
                    }
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine(ex.Message);
                }
                finally
                {
                    isProcessingSelection = false;
                }
            };

            // 두 번째 DataGridView의 SelectionChanged 이벤트 핸들러
            dataGridView2.SelectionChanged += (sender, e) =>
            {
                if (isProcessingSelection) return;  // 재귀적 호출 방지

                try
                {
                    isProcessingSelection = true;

                    if (dataGridView2.CurrentRow != null)
                    {
                        int selectedIndex = dataGridView2.CurrentRow.Index;

                        // 첫 번째 DataGridView에 같은 행 인덱스가 있는지 확인
                        if (selectedIndex < dataGridView1.Rows.Count)
                        {
                            dataGridView1.ClearSelection();
                            dataGridView1.Rows[selectedIndex].Selected = true;
                            dataGridView1.CurrentCell = dataGridView1.Rows[selectedIndex].Cells[0];
                        }
                    }
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine(ex.Message);
                }
                finally
                {
                    isProcessingSelection = false;
                }
            };          
        }


        // DataGridView별로 선택된 셀들을 추적하기 위한 딕셔너리
        // 마우스 다운/업 이벤트를 사용하여 선택 영역 추적
        
        public static Dictionary<DataGridView, List<DataGridViewCell>> dragSelections = new Dictionary<DataGridView, List<DataGridViewCell>>();

        public static void RegisterDataGridView(DataGridView dgv)
        {
            // 초기화
            dragSelections[dgv] = new List<DataGridViewCell>();

            // 이벤트 핸들러 등록
            dgv.MouseUp += DataGridView_MouseUp;
            dgv.CellContentClick += DataGridView_CellContentClick;
        }

       

        public static void DataGridView_MouseUp(object sender, MouseEventArgs e)
        {
            
            DataGridView dgv = sender as DataGridView;
            if (dgv == null) return;

            // 마우스 업 시 현재 선택된 셀 저장
            dragSelections[dgv].Clear();
            foreach (DataGridViewCell cell in dgv.SelectedCells)
            {
                dragSelections[dgv].Add(cell);
            }

            // 디버그용
            Debug.WriteLine($"선택된 셀 수: {dragSelections[dgv].Count}");
        }

        public static void DataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

            
            DataGridView dgv = sender as DataGridView;
            if (dgv == null) return;

            //Debug.WriteLine($"DataGridView_CellContentClick start => dragSelections[dgv].Count : {dragSelections[dgv].Count}");
            // 체크박스 컬럼 클릭 시 (체크박스 컬럼이 0번 컬럼이라고 가정)
            //if (e.ColumnIndex == 0 && e.RowIndex >= 0)
            if (e.ColumnIndex == 0 && e.RowIndex >= 0)
            {
                // 클릭된 셀의 체크박스 상태 확인
                DataGridViewCheckBoxCell clickedCell = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewCheckBoxCell;

                
                if (clickedCell == null) return;

                bool newValue = !(Convert.ToBoolean(clickedCell.Value)); // 현재 값의 반대로 설정

                // 마우스 업에서 저장한 선택 영역 사용
                if (dragSelections.ContainsKey(dgv) && dragSelections[dgv].Count > 0)
                {
                    // 저장된 선택 영역의 모든 체크박스 상태 변경
                    foreach (DataGridViewCell cell in dragSelections[dgv])
                    {
                        if (cell.ColumnIndex == 0) // 체크박스 컬럼인 경우
                        {
                            Debug.WriteLine($"cell.RowIndex : {cell.RowIndex}");
                            
                            DataGridViewCheckBoxCell checkCell = dgv.Rows[cell.RowIndex].Cells[0] as DataGridViewCheckBoxCell;
                            if (checkCell != null)
                                checkCell.Value = newValue;
                        }
                    }
                }
                else
                {
                    // 저장된 선택 영역이 없으면 클릭된 셀만 변경
                    clickedCell.Value = newValue;
                }
                // 마우스 다운 시 현재 선택된 셀 저장
                dragSelections[dgv].Clear();
                // 데이터그리드뷰 새로고침
                dgv.Refresh();
            }
            //Debug.WriteLine("DataGridView_CellContentClick end");
        }

        // 커스텀 정렬 이벤트 핸들러
        public static void money_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            // 디버깅을 위한 로깅 추가
            //Debug.WriteLine($"SortCompare 호출됨: DataGridView={sender.GetType().Name}, Column={e.Column.Name}, HeaderText={e.Column.HeaderText}");
            try
            {
                // 정렬이 어떤 DataGridView에서 발생했는지 확인
                // 디버그 로그 추가
                //Debug.WriteLine($"정렬 시도: Column={e.Column.Name}, HeaderText={e.Column.HeaderText}, ValueType={e.Column.ValueType}");

                // 두 값이 모두 null이면 동등하게 처리
                if ((e.CellValue1 == null || e.CellValue1 == DBNull.Value) &&
                    (e.CellValue2 == null || e.CellValue2 == DBNull.Value))
                {
                    e.SortResult = 0;
                    e.Handled = true;
                    return;
                }

                // 값1이 null이면 값2보다 작게 처리
                if (e.CellValue1 == null || e.CellValue1 == DBNull.Value)
                {
                    e.SortResult = -1;
                    e.Handled = true;
                    return;
                }

                // 값2가 null이면 값1보다 크게 처리
                if (e.CellValue2 == null || e.CellValue2 == DBNull.Value)
                {
                    e.SortResult = 1;
                    e.Handled = true;
                    return;
                }

                // "금액" 컬럼에 대해서만 커스텀 정렬 적용
                if (e.Column.Name == "합산금액" || e.Column.HeaderText == "합산금액" || e.Column.Name == "total_money" || e.Column.HeaderText == "total_money")
                {
                    //Debug.WriteLine($"커스텀 정렬 적용: Column={e.Column.Name}, HeaderText={e.Column.HeaderText}");
                    // 셀 값에서 숫자만 추출
                    Decimal val1 = ExtractNumber(e.CellValue1?.ToString() ?? "");
                    Decimal val2 = ExtractNumber(e.CellValue2?.ToString() ?? "");

                    //Debug.WriteLine($"비교 값: {e.CellValue1} ({val1}) vs {e.CellValue2} ({val2})");

                    // 숫자 기준으로 비교
                    e.SortResult = val1.CompareTo(val2);
                    // 이벤트 처리 완료 표시
                    e.Handled = true;

                    //Debug.WriteLine("커스텀 정렬 완료");
                }
                else if (e.Column.ValueType == typeof(string))
                {
                    //Debug.WriteLine($"[string]기본 정렬 사용: Column={e.Column.Name}, HeaderText={e.Column.HeaderText} , ValueType={e.Column.ValueType}");
                    // 문자열 타입에 대한 안전한 처리 추가
                    string value1 = e.CellValue1?.ToString() ?? string.Empty;
                    string value2 = e.CellValue2?.ToString() ?? string.Empty;

                    e.SortResult = string.Compare(value1, value2);
                    e.Handled = true;
                }
                else
                {
                    //Debug.WriteLine($"[default]기본 정렬 사용: Column={e.Column.Name}, HeaderText={e.Column.HeaderText} , ValueType={e.Column.ValueType}");
                }
            }
            catch (Exception ex)
            {
                // 예외 발생 시 로그 기록
                Debug.WriteLine($"정렬 중 예외 발생: {ex.Message}");

                // 기본 정렬 사용
                Debug.WriteLine($"기본 정렬 사용: Column={e.Column.Name}, HeaderText={e.Column.HeaderText}");
                e.Handled = false;
            }
           
        }

        // 문자열에서 숫자만 추출하는 함수
        private static Decimal ExtractNumber(string text)
        {
            /*
            if (string.IsNullOrEmpty(text))
                return 0;

            // 마이너스 부호 여부 확인
            bool isNegative = text.Trim().StartsWith("-");

            // 숫자만 추출 (마이너스 부호 제외)
            string numericPart = new string(text.Where(c => char.IsDigit(c)).ToArray());

            // 숫자 부분이 비어 있으면 0 반환
            if (string.IsNullOrEmpty(numericPart))
                return 0;

            // 숫자로 변환
            if (Decimal.TryParse(numericPart, out Decimal result))
            {
                // 마이너스 부호가 있었다면 결과값을 음수로 변환
                return isNegative ? -result : result;
            }

            return 0;
            */
            if (string.IsNullOrEmpty(text))
            {
                Debug.WriteLine($"text is null?? {text}");
                return 0;
            }

            //Debug.WriteLine($"tExtractNumber text :  {text}");
            //, 값 변환

            // 콤마 제거
            string cleanText = text.Replace(",", "");

            // 정규식으로 숫자 패턴 추출 (부호, 숫자, 소수점 포함)
            Match match = Regex.Match(cleanText, @"(-?\d+(\.\d+)?)");

            if (match.Success && Decimal.TryParse(match.Value, out Decimal result))
            {
                return result;
            }
            else
            {
               
                Debug.WriteLine($"text is match.Success  : {match.Success}, match.Value :  {match.Value} " );
                if (Decimal.TryParse(match.Value, out Decimal result33))
                {
                    Debug.WriteLine($",Convert.ToDecimal(match.Value) :  {Convert.ToDecimal(match.Value)}");
                }
                
            }

            return 0;
        }

        public static async Task<DataTable> ConfigureDataGridViewAsync(DataTable dataTable, DataGridView dataGridView)
        {
            // MongoDB에서 hidden 상태 확인하여 DataTable에 적용
            if (dataTable.Columns.Contains("raw_data_id"))
            {
                // hiddenYN 컬럼이 없으면 추가
                if (!dataTable.Columns.Contains("hiddenYN"))
                {
                    dataTable.Columns.Add("hiddenYN", typeof(int));
                }

                // raw_data_id 기준으로 MongoDB에서 hidden 상태 확인
                foreach (DataRow row in dataTable.Rows)
                {
                    try
                    {
                        // 기본값 설정
                        row["hiddenYN"] = 1; // 기본적으로 표시

                        if (row["raw_data_id"] != DBNull.Value && row["raw_data_id"] != null)
                        {
                            // raw_data_id가 이미 string 타입으로 저장되어 있다고 가정
                            string rawDataIdString = row["raw_data_id"].ToString();

                            if (!string.IsNullOrEmpty(rawDataIdString))
                            {
                                // 문자열 ID로 문서 조회
                                var document = await rawDataRepo.GetByIdAsync(rawDataIdString);
                                if (document != null)
                                {
                                    // IsHidden 속성 확인하여 적용
                                    row["hiddenYN"] = ((FinanceTool.MongoModels.RawDataDocument)document).IsHidden ? 0 : 1;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"row hiddenYN 처리 오류: {ex.Message}");
                        // 오류 시 기본값 유지
                    }
                }
            }

            // DataGridView의 DataSource를 DataTable로 설정
            dataGridView.DataSource = dataTable;

            // hiddenYN 컬럼이 있는지 확인
            if (dataTable.Columns.Contains("hiddenYN"))
            {
                // hiddenYN 컬럼을 숨김
                dataGridView.Columns["hiddenYN"].Visible = false;

                // 각 행을 순회하며 hiddenYN 값이 0인 경우 스타일 적용
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    // hiddenYN 컬럼의 값이 0인지 확인
                    if (row.Cells["hiddenYN"].Value != null && row.Cells["hiddenYN"].Value.ToString() == "0")
                    {
                        // 배경색과 글자색 변경
                        row.DefaultCellStyle.BackColor = Color.LightGray;
                        row.DefaultCellStyle.ForeColor = Color.DarkGray;
                    }
                }
            }

            // 특정 컬럼 숨기기
            if (dataGridView.Columns["raw_data_id"] != null)
            {
                dataGridView.Columns["raw_data_id"].Visible = false;
            }

            if (dataGridView.Columns["hiddenYN"] != null)
            {
                dataGridView.Columns["hiddenYN"].Visible = false;
            }

            if (dataGridView.Columns["import_date"] != null)
            {
                dataGridView.Columns["import_date"].Visible = false;
            }

            return dataTable;
        }

        // MongoDB에서 데이터를 가져와 DataTable로 변환하는 Helper 메서드 추가
        public static async Task<DataTable> GetDataTableFromRawDataAsync(string collectionName = "raw_data")
        {
            try
            {
                var documents = await rawDataRepo.GetAllAsync();
                return ConvertDocumentsToDataTable(documents);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 데이터 가져오기 오류: {ex.Message}");
                throw;
            }
        }

        public static async Task<DataTable> GetDataTableFromProcessDataAsync(string collectionName = "process_data")
        {
            try
            {
                var documents = await processDataRepo.GetAllAsync();

                // 첫 번째 문서의 타입 정보 로깅
                if (documents.Count > 0)
                {
                    var doc = documents[0];
                    Debug.WriteLine($"ProcessDataDocument - Id 타입: {doc.Id?.GetType().Name}, RawDataId 타입: {doc.RawDataId?.GetType().Name}");

                    if (doc.Data != null)
                    {
                        foreach (var key in doc.Data.Keys)
                        {
                            Debug.WriteLine($"Data[{key}] 타입: {(doc.Data[key] != null ? doc.Data[key].GetType().Name : "null")}");
                        }
                    }
                }

                return ConvertProcessDocumentsToDataTable(documents);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MongoDB 데이터 가져오기 오류: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                // 내부 예외가 있다면 기록
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Debug.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }

                throw;
            }
        }

        // RawDataDocument를 DataTable로 변환
        private static DataTable ConvertDocumentsToDataTable(List<RawDataDocument> documents)
        {
            DataTable dataTable = new DataTable();

            // 기본 컬럼 추가
            dataTable.Columns.Add("raw_data_id", typeof(string));
            dataTable.Columns.Add("import_date", typeof(DateTime));
            dataTable.Columns.Add("hiddenYN", typeof(int));

            // 첫 번째 문서의 데이터를 기반으로 동적 컬럼 추가
            if (documents.Count > 0 && documents[0].Data != null)
            {
                foreach (var key in documents[0].Data.Keys)
                {
                    // 이미 추가된 컬럼은 건너뜀
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
                row["raw_data_id"] = doc.Id;
                row["import_date"] = doc.ImportDate;
                row["hiddenYN"] = doc.IsHidden ? 0 : 1;

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

        // ProcessDataDocument를 DataTable로 변환
        private static DataTable ConvertProcessDocumentsToDataTable(List<ProcessDataDocument> documents)
        {
            DataTable dataTable = new DataTable();

            // 필수 컬럼만 추가
            dataTable.Columns.Add("id", typeof(string));
            dataTable.Columns.Add("raw_data_id", typeof(string));

            // 첫 번째 문서의 데이터를 기반으로 동적 데이터 컬럼 추가
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
                row["raw_data_id"] = doc.RawDataId;

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
    }
}
