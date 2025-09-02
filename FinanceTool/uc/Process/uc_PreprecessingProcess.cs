using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTool
{
    public partial class uc_preprocessing 
    {

        private void LoadSeparatorsAndRemovers()
        {
            // 프로그램 시작 시 로드
            DataHandler.spManager = new SeparatorManager();

            // 데이터 가져오기 및 중복 제거
            List<string> seperate_list = DataHandler.spManager.Separators
                .Distinct()  // 중복 제거
                .ToList();   // List로 변환

            List<string> remove_list = DataHandler.spManager.Removers
                .Distinct()  // 중복 제거
                .ToList();   // List로 변환

            //구분자 리스트 추가
            create_seperate_table(dataGridView_seperator, seperate_list);

            //불용어 리스트 추가
            create_seperate_table(dataGridView_remove, remove_list);
        }

        private void create_seperate_table(DataGridView dgv, List<string> data_list)
        {
            // DataGridView 초기화
            dgv.DataSource = null;
            dgv.Rows.Clear();
            dgv.Columns.Clear();

            // 체크박스 컬럼 추가
            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn
            {
                Name = "CheckBox",
                HeaderText = "",
                Width = 50,
                ThreeState = false,
                FillWeight = 20
            };
            dgv.Columns.Add(checkColumn);

            // 데이터 컬럼 추가
            DataGridViewTextBoxColumn dataColumn = new DataGridViewTextBoxColumn
            {
                Name = "Data",
                HeaderText = "데이터"
            };
            dgv.Columns.Add(dataColumn);

            // 데이터 리스트의 각 항목을 행으로 추가
            foreach (string data in data_list)
            {
                int rowIndex = dgv.Rows.Add();
                dgv.Rows[rowIndex].Cells["CheckBox"].Value = false;
                dgv.Rows[rowIndex].Cells["Data"].Value = data;
            }

            dgv.AllowUserToAddRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.Columns["Data"].ReadOnly = true;  // 체크박스 컬럼만 편집 가능
            dgv.Columns["CheckBox"].ReadOnly = false;  // 체크박스 컬럼만 편집 가능
            dgv.Font = new System.Drawing.Font("Pretendard", 14.25F);
        }

        // 데이터 크기에 따른 적응형 배치 크기 설정
        private int DetermineBatchSize(int totalItems)
        {
            // 작은 데이터셋 (1만 건 이하)
            if (totalItems < 10000)
                return 1000;
            // 중간 데이터셋 (1만~10만 건)
            else if (totalItems < 100000)
                return 5000;
            // 대용량 데이터셋 (10만 건 이상)
            else
                return 10000;
        }


        private object GetMoneyValue(DataRow moneyRow)
        {
            try
            {
                // DataHandler.levelName[0]를 금액 컬럼명으로 사용
                string moneyColumnName = DataHandler.levelName[0];

                // 1순위: 원래 컬럼명으로 찾기
                if (moneyRow.Table.Columns.Contains(moneyColumnName))
                {
                    return moneyRow[moneyColumnName];
                }

                // 2순위: Column0으로 찾기 (ExtractColumnToNewTable 결과)
                if (moneyRow.Table.Columns.Contains("Column0"))
                {
                    return moneyRow["Column0"];
                }

                // 3순위: 첫 번째 컬럼이 raw_data_id가 아닌 경우
                if (moneyRow.Table.Columns.Count > 1)
                {
                    string firstColName = moneyRow.Table.Columns[0].ColumnName;
                    if (!firstColName.Equals("raw_data_id", StringComparison.OrdinalIgnoreCase))
                    {
                        return moneyRow[0];
                    }
                    else if (moneyRow.Table.Columns.Count > 1)
                    {
                        return moneyRow[1]; // 두 번째 컬럼
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] GetMoneyValue 오류: {ex.Message}");
                return null;
            }
        }

       
        private void add_seperate_keyword()
        {
            // TextBox에 입력된 텍스트를 가져옴
            string inputText = new_seper_word.Text.Trim();

            // 텍스트가 비어있지 않은 경우 ListBox에 추가
            if (!string.IsNullOrEmpty(inputText))
            {
                //DataHandler.separator.Add(inputText);
                DataHandler.spManager.AddSeparator(inputText);
                new_seper_word.Clear(); // TextBox 초기화

                Debug.WriteLine($"_separatorManager.getSeparators() : {DataHandler.spManager.getSeparators()}");
                Debug.WriteLine($"_separatorManager : {string.Join(",", DataHandler.spManager.Separators)}");
            }

            List<string> seper_list = DataHandler.spManager.Separators
           .Distinct()  // 중복 제거
           .ToList();   // List로 변환

            //불용어 리스트 추가
            create_seperate_table(dataGridView_seperator, seper_list);
        }


        private void add_remove_keyword()
        {
            // TextBox에 입력된 텍스트를 가져옴
            string inputText = new_remove_word.Text.Trim();

            // 텍스트가 비어있지 않은 경우 ListBox에 추가
            if (!string.IsNullOrEmpty(inputText))
            {
                //DataHandler.remover.Add(inputText);
                DataHandler.spManager.AddRemover(inputText);
                new_remove_word.Clear(); // TextBox 초기화
            }

            Debug.WriteLine($"_separatorManager.getRemover() : {DataHandler.spManager.getRemover()}");

            List<string> remove_list = DataHandler.spManager.Removers
           .Distinct()  // 중복 제거
           .ToList();   // List로 변환

            //불용어 리스트 추가
            create_seperate_table(dataGridView_remove, remove_list);
        }


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

            Debug.WriteLine(String.Join(", ", checkedData));

            return checkedData;
        }
    }
}
