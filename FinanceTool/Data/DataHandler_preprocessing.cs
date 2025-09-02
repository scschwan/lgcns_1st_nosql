using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static FinanceTool.DataHandler;

namespace FinanceTool.Data
{
    internal class DataHandler_preprocessing
    {

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
                if (!DataHandler.processTable.Columns.Contains(DataHandler.dept_col_name) ||
                    !DataHandler.processTable.Columns.Contains(DataHandler.prod_col_name))
                {
                    throw new Exception($"필수 컬럼이 없습니다. (부서: {DataHandler.dept_col_name}, 제품: {DataHandler.prod_col_name})");
                }

                // 2. 필수 컬럼 확인 및 추가
                if (!newTable.Columns.Contains(DataHandler.dept_col_name))
                    newTable.Columns.Add(DataHandler.dept_col_name, DataHandler.processTable.Columns[DataHandler.dept_col_name].DataType);

                if (!newTable.Columns.Contains(DataHandler.prod_col_name))
                    newTable.Columns.Add(DataHandler.prod_col_name, DataHandler.processTable.Columns[DataHandler.prod_col_name].DataType);

                // 3. inputTable의 모든 컬럼 추가
                foreach (DataColumn col in inputTable.Columns)
                {
                    if (col.ColumnName.Equals(DataHandler.dept_col_name) || col.ColumnName.Equals(DataHandler.prod_col_name))
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
                    if (i < DataHandler.processTable.Rows.Count)
                    {
                        newRow[DataHandler.dept_col_name] = DataHandler.processTable.Rows[i][DataHandler.dept_col_name];
                        newRow[DataHandler.prod_col_name] = DataHandler.processTable.Rows[i][DataHandler.prod_col_name];
                    }
                    else if (DataHandler.processTable.Rows.Count > 0)
                    {
                        // 인덱스가 범위를 벗어나면 첫 번째 행 데이터 사용
                        newRow[DataHandler.dept_col_name] = DataHandler.processTable.Rows[0][DataHandler.dept_col_name];
                        newRow[DataHandler.prod_col_name] = DataHandler.processTable.Rows[0][DataHandler.prod_col_name];
                    }
                    else
                    {
                        // processTable이 비어있을 경우
                        newRow[DataHandler.dept_col_name] = DBNull.Value;
                        newRow[DataHandler.prod_col_name] = DBNull.Value;
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


        public static DataTable ProcessShortStringsToNull(DataTable inputTable)
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

    }
}
