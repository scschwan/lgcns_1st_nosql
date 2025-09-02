using ClosedXML.Excel;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTool.Data
{
    internal class DataHandler_classification
    {
        private const string EXCEL_COMPLETED_FOLDER = @"C:\Dmillions\excel_completed";

        /// <summary>
        /// DataTable에서 "id" 컬럼을 제거한 복사본을 생성
        /// </summary>
        private static DataTable RemoveIdColumnFromDataTable(DataTable sourceTable)
        {
            if (sourceTable == null)
                return null;

            try
            {
                // 새로운 DataTable 생성
                DataTable resultTable = new DataTable();

                // "id" 컬럼을 제외한 모든 컬럼 추가
                foreach (DataColumn column in sourceTable.Columns)
                {
                    if (column.ColumnName.ToLower() != "id")  // "id" 컬럼 제외
                    {
                        resultTable.Columns.Add(column.ColumnName, column.DataType);
                    }
                }

                // 데이터 복사 ("id" 컬럼 제외)
                foreach (DataRow sourceRow in sourceTable.Rows)
                {
                    DataRow newRow = resultTable.NewRow();

                    foreach (DataColumn column in resultTable.Columns)
                    {
                        newRow[column.ColumnName] = sourceRow[column.ColumnName];
                    }

                    resultTable.Rows.Add(newRow);
                }

                Debug.WriteLine($"id 컬럼 제거 완료: 원본 {sourceTable.Columns.Count}개 컬럼 → 결과 {resultTable.Columns.Count}개 컬럼");
                return resultTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"id 컬럼 제거 중 오류: {ex.Message}");
                return sourceTable; // 오류 시 원본 반환
            }
        }

        public static string SaveDataTableToExcel(DataTable firstTable, DataTable secondTable = null)
        {
            string fileName = "";
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
                                //var secondSheet = workbook.Worksheets.Add(secondTable, "Clustering 원본");
                                // *** 개선: "id" 컬럼을 제거한 복사본 생성 ***
                                DataTable filteredSecondTable = RemoveIdColumnFromDataTable(secondTable);
                                var secondSheet = workbook.Worksheets.Add(filteredSecondTable, "Clustering 원본");
                            }

                            workbook.SaveAs(filePath);

                            /*
                            string message = secondTable != null ?
                                "Excel file이 두 개의 시트로 생성되었습니다." :
                                "Excel file이 생성되었습니다.";
                            
                            string message = "Excel file이 생성되었습니다.";
                            MessageBox.Show($"{message}\n{filePath}",
                                           "Success",
                                           MessageBoxButtons.OK,
                                           MessageBoxIcon.Information);
                            */
                        }
                        fileName = filePath;
                    }

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel file 생성이 실패하였습니다:\n{ex.Message}",
                               "Error",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
                return fileName;
            }
            return fileName;
        }


        /// <summary>
        /// 현재 세션 ID 가져오기
        /// </summary>
        public static ObjectId GetCurrentSessionId()
        {
            return DataHandler._currentSessionId;
        }



        /// <summary>
        /// Excel 완료 폴더 존재 확인 및 생성
        /// </summary>
        public static void EnsureExcelCompletedFolderExists()
        {
            try
            {
                if (!Directory.Exists(EXCEL_COMPLETED_FOLDER))
                {
                    Directory.CreateDirectory(EXCEL_COMPLETED_FOLDER);
                    Debug.WriteLine($"Excel 완료 폴더 생성: {EXCEL_COMPLETED_FOLDER}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excel 완료 폴더 생성 오류: {ex.Message}");
                MessageBox.Show($"Excel 완료 폴더 생성 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Excel 완료 파일 경로 생성
        /// </summary>
        public static string GenerateExcelCompletedFilePath(ObjectId sessionId, string originalFileName = null)
        {
            try
            {
                // 폴더 존재 확인
                EnsureExcelCompletedFolderExists();

                // 파일명 생성: 세션ID 앞 8자리 + 타임스탬프
                string sessionIdPrefix = sessionId.ToString().Substring(0, 8);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                string fileName;
                if (!string.IsNullOrEmpty(originalFileName))
                {
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
                    fileName = $"{sessionIdPrefix}_{timestamp}_{nameWithoutExt}.xlsx";
                }
                else
                {
                    fileName = $"{sessionIdPrefix}_{timestamp}_결과.xlsx";
                }

                string fullPath = Path.Combine(EXCEL_COMPLETED_FOLDER, fileName);
                Debug.WriteLine($"Excel 완료 파일 경로 생성: {fullPath}");

                return fullPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excel 완료 파일 경로 생성 오류: {ex.Message}");
                return null;
            }
        }

    }
}
