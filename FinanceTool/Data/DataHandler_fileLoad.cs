using FinanceTool.MongoModels;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTool.Data
{
    internal class DataHandler_fileLoad
    {
        private static bool isColumnOrderInitialized = false;

        /// <summary>
        /// MongoDB에서 컬럼 순서 정보 로드
        /// </summary>
        public static async Task LoadColumnOrderFromMongoDB()
        {
            try
            {
                var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                var columnCollection = await mongoManager.GetCollectionAsync<ColumnMappingDocument>("column_mapping");

                var columnMappings = await columnCollection.Find(_ => true)
                    .SortBy(c => c.Sequence)
                    .ToListAsync();

                DataHandler.columnDisplayOrder.Clear();
                foreach (var mapping in columnMappings)
                {
                    DataHandler.columnDisplayOrder[mapping.OriginalName] = mapping.Sequence;
                }

                isColumnOrderInitialized = true;
                Debug.WriteLine($"컬럼 순서 로드 완료: {DataHandler.columnDisplayOrder.Count}개");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"컬럼 순서 로드 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// DataGridView의 현재 컬럼 순서를 메모리에 업데이트
        /// </summary>
        public static void UpdateColumnDisplayOrder(DataGridView dgv)
        {
            try
            {
                DataHandler.columnDisplayOrder.Clear();

                // DisplayIndex 순서대로 정렬하여 sequence 부여
                var sortedColumns = dgv.Columns.Cast<DataGridViewColumn>()
                    .Where(col => col.Visible && !IsSystemColumn(col.Name))
                    .OrderBy(col => col.DisplayIndex)
                    .ToList();

                for (int i = 0; i < sortedColumns.Count; i++)
                {
                    DataHandler.columnDisplayOrder[sortedColumns[i].Name] = i;
                }

                Debug.WriteLine($"메모리 컬럼 순서 업데이트 완료: {DataHandler.columnDisplayOrder.Count}개");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"컬럼 순서 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 컬럼 순서를 적용하여 DataTable 생성
        /// </summary>
        public static DataTable ApplyColumnOrder(DataTable sourceTable)
        {
            if (!isColumnOrderInitialized || DataHandler.columnDisplayOrder.Count == 0)
            {
                return sourceTable; // 순서 정보가 없으면 원본 반환
            }

            try
            {
                var orderedTable = sourceTable.Clone(); // 구조만 복사

                // 시스템 컬럼 먼저 추가 (id, import_date, is_hidden 등)
                var systemColumns = sourceTable.Columns.Cast<DataColumn>()
                    .Where(col => IsSystemColumn(col.ColumnName))
                    .ToList();

                // 사용자 정의 순서대로 컬럼 재배치
                var userColumns = sourceTable.Columns.Cast<DataColumn>()
                    .Where(col => !IsSystemColumn(col.ColumnName) && DataHandler.columnDisplayOrder.ContainsKey(col.ColumnName))
                    .OrderBy(col => DataHandler.columnDisplayOrder[col.ColumnName])
                    .ToList();

                // 순서가 없는 컬럼들 (새로 추가된 컬럼)
                var unorderedColumns = sourceTable.Columns.Cast<DataColumn>()
                    .Where(col => !IsSystemColumn(col.ColumnName) && !DataHandler.columnDisplayOrder.ContainsKey(col.ColumnName))
                    .ToList();

                // 시스템 컬럼 제거 후 사용자 순서대로 재추가
                orderedTable.Columns.Clear();

                // 1. 시스템 컬럼 추가
                foreach (var sysCol in systemColumns)
                {
                    //2025.07.31
                    //시스템컬럼은 미사용
                    //orderedTable.Columns.Add(sysCol.ColumnName, sysCol.DataType);
                }

                // 2. 순서가 있는 사용자 컬럼 추가
                foreach (var userCol in userColumns)
                {
                    orderedTable.Columns.Add(userCol.ColumnName, userCol.DataType);
                }

                // 3. 순서가 없는 컬럼 추가
                foreach (var unorderedCol in unorderedColumns)
                {
                    orderedTable.Columns.Add(unorderedCol.ColumnName, unorderedCol.DataType);
                }

                // 데이터 복사
                foreach (DataRow row in sourceTable.Rows)
                {
                    orderedTable.ImportRow(row);
                }

                return orderedTable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"컬럼 순서 적용 오류: {ex.Message}");
                return sourceTable; // 오류 시 원본 반환
            }
        }

        /// <summary>
        /// 시스템 컬럼 여부 확인
        /// </summary>
        private static bool IsSystemColumn(string columnName)
        {
            return columnName == "id" ||
                   columnName == "_id" ||
                   columnName == "import_date" ||
                   columnName == "is_hidden" ||
                   columnName == "hiddenYN";
        }

        /// <summary>
        /// MongoDB에 컬럼 순서 저장
        /// </summary>
        public static async Task SaveColumnOrderToMongoDB()
        {
            try
            {
                var mongoManager = FinanceTool.Data.MongoDBManager.Instance;
                var columnCollection = await mongoManager.GetCollectionAsync<ColumnMappingDocument>("column_mapping");

                var updateTasks = new List<Task>();

                foreach (var kvp in DataHandler.columnDisplayOrder)
                {
                    var filter = Builders<ColumnMappingDocument>.Filter.Eq(c => c.OriginalName, kvp.Key);
                    var update = Builders<ColumnMappingDocument>.Update.Set(c => c.Sequence, kvp.Value);

                    updateTasks.Add(columnCollection.UpdateOneAsync(filter, update));
                }

                await Task.WhenAll(updateTasks);
                Debug.WriteLine($"컬럼 순서 MongoDB 저장 완료: {DataHandler.columnDisplayOrder.Count}개");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"컬럼 순서 저장 오류: {ex.Message}");
            }
        }


        public static async Task<DataTable> GetDataTableFromProcessDataAsync(string collectionName = "process_data")
        {
            try
            {
                var documents = await DataHandler.processDataRepo.GetAllAsync();

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
