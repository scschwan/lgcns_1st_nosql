﻿using DocumentFormat.OpenXml.Presentation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FinanceTool
{
    public partial class uc_preprocessing : UserControl
    {
        bool iskeywordExtractor = false;
        private bool isProcessingSelection = false;
        DataTable modifiedDataTable;
        DataTable originKeywordDataTable;

        
        private bool _dataLoaded = false;

        private bool isExtractRunning = false;

        List<string> selectedColumnNames = new List<string>();

        public uc_preprocessing()
        {
            InitializeComponent();
        }

        public async Task initUI()
        {
            Debug.WriteLine($"[preprocessing]processTable.Columns.Count : {DataHandler.processTable.Columns.Count}");
            Debug.WriteLine($"[preprocessing]processTable.Rows.Count : {DataHandler.processTable.Rows.Count}");

            // 데이터베이스에 전처리 뷰 생성
            //CreatePreprocessingView();

            // 수정 전에 선택된 컬럼명 확인
            string moneyColumnName = DataHandler.levelName[0]; // 금액 컬럼명
            string targetColumnName = DataHandler.levelName[1]; // 타겟 컬럼명

            // 컬럼명으로 인덱스 찾기
            int moneyColumnIndex = DataHandler.processTable.Columns.IndexOf(moneyColumnName);
            int targetColumnIndex = DataHandler.processTable.Columns.IndexOf(targetColumnName);

            // 수정된 방식: 컬럼명을 사용하여 modifiedDataTable을 생성
            selectedColumnNames = DataHandler.levelName; // 선택된 컬럼명 목록



            //originKeywordDataTable = await DataHandler.CreateDataTableFromColumnsAsync(DataHandler.processTable, DataHandler.levelList);
            originKeywordDataTable = await DataHandler.CreateDataTableFromColumnNamesAsync(DataHandler.processTable, selectedColumnNames);

            dataGridView_target.DataSource = originKeywordDataTable;
            _dataLoaded = true;

            // 금액 컬럼 visible false
            dataGridView_target.Columns[0].Visible = false;
            // raw_data_id visible false
            dataGridView_target.Columns["raw_data_id"].Visible = false;
            dataGridView_target.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

           

            // 찾은 인덱스로 데이터 테이블 생성
            DataHandler.moneyDataTable = DataHandler.ExtractColumnToNewTable(DataHandler.processTable, moneyColumnIndex);
            DataHandler.lowLevelData = DataHandler.ExtractColumnToNewTable(DataHandler.processTable, targetColumnIndex);

            //modifiedDataTable = await DataHandler.CreateDataTableFromColumnsAsync(DataHandler.processTable, DataHandler.levelList);


            
            modifiedDataTable = await DataHandler.CreateDataTableFromColumnNamesAsync(DataHandler.processTable, selectedColumnNames);

            //DataHandler.moneyDataTable = DataHandler.ExtractColumnToNewTable(modifiedDataTable, 0);
            //DataHandler.moneyDataTable = DataHandler.ExtractColumnToNewTable(DataHandler.processTable, DataHandler.levelList[0]);


            Debug.WriteLine($"modifiedDataTable.Columns.Count : {modifiedDataTable.Columns.Count}");
            Debug.WriteLine($"modifiedDataTable.Columns.Count : {modifiedDataTable.Columns[2].ColumnName}");

            //DataHandler.separator = _separatorManager.Separators;
            //DataHandler.remover = _separatorManager.Removers;

            // 구분자 및 불용어 목록 추가
            LoadSeparatorsAndRemovers();

            // 데이터그리드뷰 간 선택 동기화
            DataHandler.SyncDataGridViewSelections(dataGridView_target, dataGridView_applied);

            DataHandler.RegisterDataGridView(dataGridView_seperator);
            DataHandler.RegisterDataGridView(dataGridView_remove);
        }

        

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
            dgv.Font = new System.Drawing.Font("맑은 고딕", 14.25F);
        }


        private async void btn_apply_Click(object sender, EventArgs e)
        {
            //await ApplyToAllData(dt => DataHandler.ReplaceSeparatorInColumn(dt, dt.Columns.Count - 1, "_", "separate"));
            // 키워드 추출이 이미 수행되었다면 데이터 초기화 후 재수행
            if (iskeywordExtractor)
            {
                //modifiedDataTable = await DataHandler.CreateDataTableFromColumnsAsync(DataHandler.processTable, DataHandler.levelList);
                modifiedDataTable = await DataHandler.CreateDataTableFromColumnNamesAsync(DataHandler.processTable, selectedColumnNames);
            }

            // raw_data_id 컬럼 정보 임시 저장
            DataColumn rawDataIdColumn = null;
            Dictionary<string, object> rawDataIdValues = new Dictionary<string, object>();



            // 컬럼 객체 저장
            rawDataIdColumn = modifiedDataTable.Columns["raw_data_id"];

            // 각 행의 raw_data_id 값을 저장
            foreach (DataRow row in modifiedDataTable.Rows)
            {
                // 행 식별을 위한 고유 키 생성 (여기서는 행 인덱스 사용)
                string rowKey = modifiedDataTable.Rows.IndexOf(row).ToString();
                rawDataIdValues[rowKey] = row["raw_data_id"];
            }

            // 컬럼 제거
            modifiedDataTable.Columns.Remove("raw_data_id");

            modifiedDataTable = DataHandler.ReplaceSeparatorInColumn(modifiedDataTable, modifiedDataTable.Columns.Count -1, "_", "separate");

            // 새 raw_data_id 컬럼 추가
            DataColumn newRawDataIdColumn = new DataColumn("raw_data_id", rawDataIdColumn.DataType);
            modifiedDataTable.Columns.Add(newRawDataIdColumn);


            // 각 행에 저장해둔 값 복원
            for (int i = 0; i < modifiedDataTable.Rows.Count && i < rawDataIdValues.Count; i++)
            {
                // 같은 위치의 행에 값 복원
                modifiedDataTable.Rows[i]["raw_data_id"] = rawDataIdValues[i.ToString()];
            }

            //dataGridView_applied.DataSource = modifiedDataTable;
            dataGridView_applied.DataSource = DataHandler.CombineDataTables(modifiedDataTable);
            dataGridView_applied.Columns["raw_data_id"].Visible = false;
            iskeywordExtractor = true;
            isProcessingSelection = false;
        }

        private async void remove_apply_btn_Click(object sender, EventArgs e)
        {
            if (iskeywordExtractor)
            {
                //modifiedDataTable = await DataHandler.CreateDataTableFromColumnsAsync(DataHandler.processTable, DataHandler.levelList);
                modifiedDataTable = await DataHandler.CreateDataTableFromColumnNamesAsync(DataHandler.processTable, selectedColumnNames);
            }


            // raw_data_id 컬럼 정보 임시 저장
            DataColumn rawDataIdColumn = null;
            Dictionary<string, object> rawDataIdValues = new Dictionary<string, object>();



            // 컬럼 객체 저장
            rawDataIdColumn = modifiedDataTable.Columns["raw_data_id"];

            // 각 행의 raw_data_id 값을 저장
            foreach (DataRow row in modifiedDataTable.Rows)
            {
                // 행 식별을 위한 고유 키 생성 (여기서는 행 인덱스 사용)
                string rowKey = modifiedDataTable.Rows.IndexOf(row).ToString();
                rawDataIdValues[rowKey] = row["raw_data_id"];
            }

            // 컬럼 제거
            modifiedDataTable.Columns.Remove("raw_data_id");

            //await ApplyToAllData(dt => DataHandler.ReplaceSeparatorInColumn(dt, dt.Columns.Count - 1, "", "remove"));
            modifiedDataTable = DataHandler.ReplaceSeparatorInColumn(modifiedDataTable, modifiedDataTable.Columns.Count - 1, "", "remove");

            // 새 raw_data_id 컬럼 추가
            DataColumn newRawDataIdColumn = new DataColumn("raw_data_id", rawDataIdColumn.DataType);
            modifiedDataTable.Columns.Add(newRawDataIdColumn);


            // 각 행에 저장해둔 값 복원
            for (int i = 0; i < modifiedDataTable.Rows.Count && i < rawDataIdValues.Count; i++)
            {
                // 같은 위치의 행에 값 복원
                modifiedDataTable.Rows[i]["raw_data_id"] = rawDataIdValues[i.ToString()];
            }

            //dataGridView_applied.DataSource = modifiedDataTable;
            dataGridView_applied.DataSource = DataHandler.CombineDataTables(modifiedDataTable);
            dataGridView_applied.Columns["raw_data_id"].Visible = false;
            iskeywordExtractor = true;
            isProcessingSelection = false;
        }


        private async void keyword_seper_split_Click(object sender, EventArgs e)
        {
            try
            {
                isExtractRunning = true;
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "데이터 준비 중...");

                    // 1. 키워드 추출이 이미 수행되었다면 데이터 초기화
                    if (iskeywordExtractor)
                    {
                        modifiedDataTable = await DataHandler.CreateDataTableFromColumnNamesAsync(DataHandler.processTable, selectedColumnNames);
                        await progressForm.UpdateProgressHandler(15, "데이터 초기화 완료");
                    }

                    int totalRows = modifiedDataTable.Rows.Count;
                    Debug.WriteLine($"총 처리할 행 수: {totalRows}");

                    // 시스템 리소스에 맞게 병렬 처리 최적화
                    int cpuCount = Environment.ProcessorCount;
                    int maxDegreeOfParallelism = Math.Max(1, cpuCount - 1); // 시스템에 하나의 코어는 남겨둠

                    // 데이터 크기에 따른 적응형 배치 크기 결정
                    int batchSize = DetermineBatchSize(totalRows);

                    await progressForm.UpdateProgressHandler(20, "ID 정보 추출 중...");

                    // 2. raw_data_id 컬럼 정보 임시 저장
                    DataColumn rawDataIdColumn = modifiedDataTable.Columns["raw_data_id"];
                    var rawDataIdValues = new ConcurrentDictionary<string, object>();

                    // 병렬로 ID 값 추출
                    await Task.Run(() => {
                        Parallel.For(0, modifiedDataTable.Rows.Count,
                            new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                            i => {
                                rawDataIdValues[i.ToString()] = modifiedDataTable.Rows[i]["raw_data_id"];
                            });
                    });

                    // 컬럼 제거
                    modifiedDataTable.Columns.Remove("raw_data_id");
                    await progressForm.UpdateProgressHandler(25, "1/5 단계: 구분자 변환 시작...");

                    // 3. 구분자 변환 - 병렬 처리
                    modifiedDataTable = await Task.Run(() => {
                        return DataHandler.ReplaceSeparatorInColumn(
                            modifiedDataTable,
                            modifiedDataTable.Columns.Count - 1,
                            "_",
                            "separate"
                        );
                    });
                    await progressForm.UpdateProgressHandler(35, "2/5 단계: 불용어 제거 시작...");

                    // 4. 불용어 제거 - 병렬 처리
                    modifiedDataTable = await Task.Run(() => {
                        return DataHandler.ReplaceSeparatorInColumn(
                            modifiedDataTable,
                            modifiedDataTable.Columns.Count - 1,
                            "",
                            "remove"
                        );
                    });
                    await progressForm.UpdateProgressHandler(50, "3/5 단계: 문자열 전처리 시작...");

                    // 5. 구분자 기반 추출 전 전처리 - 병렬 처리
                    modifiedDataTable = await Task.Run(() => {
                        // 병렬 처리 옵션 설정
                        var options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };

                        // 짧은 문자열 전처리
                        var processedTable = DataHandler.ProcessShortStringsToNull(modifiedDataTable);

                        // 밑줄 문자 전처리
                        return DataHandler.ProcessUnderscoresInAllColumn(processedTable);
                    });
                    await progressForm.UpdateProgressHandler(70, "4/5 단계: 구분자 기반 분할 시작...");

                    // 6. 구분자 기반 추출 - 병렬 처리
                    modifiedDataTable = await Task.Run(() => {
                        return DataHandler.SplitColumnBySeparator(modifiedDataTable, "_");
                    });
                    await progressForm.UpdateProgressHandler(85, "5/5 단계: ID 정보 복원 중...");

                    // 7. 새 raw_data_id 컬럼 추가 및 값 복원
                    DataColumn newRawDataIdColumn = new DataColumn("raw_data_id", rawDataIdColumn.DataType);
                    modifiedDataTable.Columns.Add(newRawDataIdColumn);

                    // 수정된 ID 값 복원 코드 - 안전하게 순차 처리
                    await Task.Run(() => {
                        // 행 수와 ID 값 수의 불일치 확인 및 로깅
                        int rowCount = modifiedDataTable.Rows.Count;
                        int idCount = rawDataIdValues.Count;
                        Debug.WriteLine($"ID 복원 - 현재 테이블 행 수: {rowCount}, 저장된 ID 값 수: {idCount}");

                        // 안전하게 순차 처리
                        for (int i = 0; i < rowCount && i < idCount; i++)
                        {
                            if (rawDataIdValues.TryGetValue(i.ToString(), out var value))
                            {
                                try
                                {
                                    modifiedDataTable.Rows[i]["raw_data_id"] = value;
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"ID 복원 중 오류(인덱스 {i}): {ex.Message}");
                                    // 오류 발생 시 건너뛰고 계속 진행
                                }
                            }
                        }
                    });
                    await progressForm.UpdateProgressHandler(95, "결과 데이터 표시 중...");

                    // 8. 결과 표시
                    dataGridView_applied.DataSource = DataHandler.CombineDataTables(modifiedDataTable);
                    dataGridView_applied.Columns["raw_data_id"].Visible = false;
                    iskeywordExtractor = true;
                    isProcessingSelection = true;

                    if (!nlp_groupBox.Visible)
                    {
                        nlp_groupBox.Visible = true;
                    }

                    await progressForm.UpdateProgressHandler(100, "키워드 추출 완료");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"키워드 추출 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine($"키워드 추출 오류: {ex.Message}");
            }
            finally
            {
                isExtractRunning = false;
            }
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

        private async void keyword_model_split_Click(object sender, EventArgs e)
        {
            try
            {
                isExtractRunning = true;
                Console.WriteLine("Model_split start");
                Stopwatch sw = Stopwatch.StartNew();

                // 시스템 환경 정보 로깅 및 Java 환경 설정
                int cpuCount = Environment.ProcessorCount;
                Debug.WriteLine($"사용 가능한 CPU 코어 수: {cpuCount}");

                // Java 환경 설정 
                string javaPath = Path.Combine(Application.StartupPath, "java");
                if (Directory.Exists(javaPath))
                {
                    Environment.SetEnvironmentVariable("JAVA_HOME", javaPath);
                    string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
                    Environment.SetEnvironmentVariable("PATH", $"{Path.Combine(javaPath, "bin")};{pathEnv}");

                    // OpenMP, MKL 등 병렬 처리 라이브러리 최적화 설정
                    Environment.SetEnvironmentVariable("OMP_NUM_THREADS", Math.Max(1, cpuCount - 1).ToString());
                    Environment.SetEnvironmentVariable("MKL_NUM_THREADS", Math.Max(1, cpuCount - 1).ToString());
                }

                // raw_data_id 컬럼 정보 임시 저장
                DataColumn rawDataIdColumn = null;
                var rawDataIdValues = new Dictionary<string, object>();

                int totalRows = modifiedDataTable.Rows.Count;
                Debug.WriteLine($"총 처리할 행 수: {totalRows}");

                // 컬럼 객체 저장
                if (modifiedDataTable.Columns.Contains("raw_data_id"))
                {
                    rawDataIdColumn = modifiedDataTable.Columns["raw_data_id"];

                    // 각 행의 raw_data_id 값을 저장
                    for (int i = 0; i < modifiedDataTable.Rows.Count; i++)
                    {
                        string rowKey = i.ToString();
                        rawDataIdValues[rowKey] = modifiedDataTable.Rows[i]["raw_data_id"];
                    }

                    // 컬럼 제거
                    modifiedDataTable.Columns.Remove("raw_data_id");
                }

                // 메모리 최적화를 위한 GC 실행
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // NLP 함수 호출 - 기본 매개변수 유지
                modifiedDataTable = await DataHandler.SplitColumnByModel(modifiedDataTable, ai_limit_cnt);

                // 새 raw_data_id 컬럼 추가
                DataColumn newRawDataIdColumn = new DataColumn("raw_data_id", rawDataIdColumn?.DataType ?? typeof(string));
                modifiedDataTable.Columns.Add(newRawDataIdColumn);

                // ID 값 복원 - 안전한 방식으로
                int rowCount = modifiedDataTable.Rows.Count;
                int idCount = rawDataIdValues.Count;
                Debug.WriteLine($"ID 복원 - 현재 테이블 행 수: {rowCount}, 저장된 ID 값 수: {idCount}");

                // 최소 행 수만큼만 복원
                int maxIndex = Math.Min(rowCount, idCount);
                for (int i = 0; i < maxIndex; i++)
                {
                    try
                    {
                        if (rawDataIdValues.TryGetValue(i.ToString(), out var value))
                        {
                            modifiedDataTable.Rows[i]["raw_data_id"] = value;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"ID 복원 중 오류(인덱스 {i}): {ex.Message}");
                    }
                }

                // 결과 UI에 표시
                dataGridView_applied.DataSource = DataHandler.CombineDataTables(modifiedDataTable);
                dataGridView_applied.Columns["raw_data_id"].Visible = false;

                sw.Stop();
                Debug.WriteLine($"Excel → SQLite 변환 완료. 소요 시간: {sw.ElapsedMilliseconds}ms, 행 수: {dataGridView_applied.Rows.Count}");
                Console.WriteLine("Model_split end");

                iskeywordExtractor = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"NLP 키워드 추출 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine($"NLP 키워드 추출 오류: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                isExtractRunning = false;
            }
        }


        private async void remove_1key_Click(object sender, EventArgs e)
        {
            try
            {
                isExtractRunning = true;
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();
                    await progressForm.UpdateProgressHandler(10, "1글자 키워드 제거 시작...");
                    Application.DoEvents(); //

                    // 이전 처리 결과의 영향을 받지 않도록 데이터 재초기화 확인
                    if (iskeywordExtractor)
                    {
                        await progressForm.UpdateProgressHandler(15, "이전 처리 내역 확인 중...");
                        Debug.WriteLine("키워드 처리 상태 확인: 이미 처리된 데이터 감지됨");
                    }

                    int totalRows = modifiedDataTable.Rows.Count;
                    Debug.WriteLine($"총 처리할 행 수: {totalRows}");

                    // raw_data_id 컬럼 정보 임시 저장 - 행 순서 유지를 위해 Dictionary 사용
                    await progressForm.UpdateProgressHandler(20, "ID 정보 추출 중...");
                    Application.DoEvents(); //
                    DataColumn rawDataIdColumn = null;
                    Dictionary<int, object> rawDataIdValues = new Dictionary<int, object>();

                    // 컬럼 존재 확인 (반복 실행 시 예외 방지)
                    if (modifiedDataTable.Columns.Contains("raw_data_id"))
                    {
                        rawDataIdColumn = modifiedDataTable.Columns["raw_data_id"];

                        // ID 값을 원래 행 인덱스와 함께 저장
                        for (int i = 0; i < totalRows; i++)
                        {
                            rawDataIdValues[i] = modifiedDataTable.Rows[i]["raw_data_id"];
                        }

                        // 컬럼 제거
                        modifiedDataTable.Columns.Remove("raw_data_id");
                    }
                    else
                    {
                        Debug.WriteLine("경고: raw_data_id 컬럼이 존재하지 않습니다!");
                        // 컬럼이 없으면 빈 컬럼 생성하여 이후 단계 진행
                        rawDataIdColumn = new DataColumn("raw_data_id", typeof(string));
                    }

                    await progressForm.UpdateProgressHandler(30, "유효 항목 분석 중...");
                    Application.DoEvents(); //

                    // 각 행에서 유효한 항목 수 계산 - 직렬 처리로 변경하여 문제 최소화
                    int maxValidItemsPerRow = 0;
                    Dictionary<int, List<object>> validItemsByRow = new Dictionary<int, List<object>>();

                    for (int rowIdx = 0; rowIdx < totalRows; rowIdx++)
                    {
                        if (rowIdx % 10000 == 0)
                        {
                            await progressForm.UpdateProgressHandler(
                                30 + (int)(20.0 * rowIdx / totalRows),
                                $"항목 분석 중... ({rowIdx}/{totalRows})"
                            );
                        }

                        var row = modifiedDataTable.Rows[rowIdx];
                        var validItems = new List<object>();

                        // 각 셀에서 1글자가 아닌 항목만 추출
                        foreach (var item in row.ItemArray)
                        {
                            string value = item?.ToString() ?? "";
                            if (value.Length != 1)
                            {
                                validItems.Add(item);
                            }
                        }

                        validItemsByRow[rowIdx] = validItems;
                        maxValidItemsPerRow = Math.Max(maxValidItemsPerRow, validItems.Count);
                    }

                    await progressForm.UpdateProgressHandler(55, "결과 테이블 생성 중...");
                    Application.DoEvents(); //

                    // 새로운 DataTable 생성
                    DataTable result = new DataTable();
                    for (int i = 0; i < maxValidItemsPerRow; i++)
                    {
                        result.Columns.Add($"Column{i}", typeof(string));
                    }

                    // 행 순서를 유지하며 결과에 추가
                    await progressForm.UpdateProgressHandler(65, "행 순서 유지하며 결과 구성 중...");
                    Application.DoEvents(); //
                    for (int rowIdx = 0; rowIdx < totalRows; rowIdx++)
                    {
                        if (rowIdx % 10000 == 0)
                        {
                            await progressForm.UpdateProgressHandler(
                                65 + (int)(20.0 * rowIdx / totalRows),
                                $"결과 구성 중... ({rowIdx}/{totalRows})"
                            );
                        }

                        DataRow newRow = result.NewRow();

                        if (validItemsByRow.TryGetValue(rowIdx, out var items))
                        {
                            for (int colIdx = 0; colIdx < items.Count && colIdx < result.Columns.Count; colIdx++)
                            {
                                newRow[colIdx] = items[colIdx];
                            }
                        }

                        result.Rows.Add(newRow);
                    }

                    // 새 테이블로 교체
                    modifiedDataTable = result;
                    Debug.WriteLine($"처리 후 행 수: {modifiedDataTable.Rows.Count}, 열 수: {modifiedDataTable.Columns.Count}");

                    // 새 raw_data_id 컬럼 추가
                    await progressForm.UpdateProgressHandler(90, "ID 정보 복원 중...");
                    Application.DoEvents(); //
                    DataColumn newRawDataIdColumn = new DataColumn("raw_data_id", rawDataIdColumn?.DataType ?? typeof(string));
                    modifiedDataTable.Columns.Add(newRawDataIdColumn);

                    // ID 값 순서대로 복원
                    int count = Math.Min(modifiedDataTable.Rows.Count, rawDataIdValues.Count);
                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            if (rawDataIdValues.TryGetValue(i, out var value))
                            {
                                modifiedDataTable.Rows[i]["raw_data_id"] = value;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"ID 복원 중 오류(인덱스 {i}): {ex.Message}");
                        }
                    }

                    // 결과 UI에 표시
                    await progressForm.UpdateProgressHandler(100, "1글자 키워드 제거 완료");
                    dataGridView_applied.DataSource = DataHandler.CombineDataTables(modifiedDataTable);

                    // 마지막으로 raw_data_id 컬럼 숨김 처리
                    if (dataGridView_applied.Columns.Contains("raw_data_id"))
                    {
                        dataGridView_applied.Columns["raw_data_id"].Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"1글자 키워드 제거 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine($"1글자 키워드 제거 오류: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                isExtractRunning = false;
            }
        }



        private async void btn_complete_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("btn_complete_Click start");
            Debug.WriteLine($"modifiedDataTable.Columns.Count : {modifiedDataTable.Columns.Count} modifiedDataTable.Rows.Count :  {modifiedDataTable.Rows.Count}");

            // 유효성 검사 (기존 코드와 동일)
            if (modifiedDataTable.Columns.Count < 4 && !isProcessingSelection)
            {
                MessageBox.Show("키워드 추출이 완료되지 않았습니다.", "알림",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var progressForm = new ProcessProgressForm())
                {
                    progressForm.Show();

                    if (isExtractRunning)
                    {
                        while (isExtractRunning)
                        {
                            await progressForm.UpdateProgressHandler(5, "전처리 데이터 저장 준비 중...");
                            await Task.Delay(10);
                        }
                    }

                    await progressForm.UpdateProgressHandler(10, "MongoDB 연결 확인 중...");

                    // MongoDB 연결 확인
                    bool mongoConnected = await Data.MongoDBManager.Instance.EnsureInitializedAsync();
                    if (!mongoConnected)
                    {
                        throw new Exception("MongoDB 연결에 실패했습니다.");
                    }

                    await progressForm.UpdateProgressHandler(15, "전처리 데이터 저장 준비 중...");

                    // SQLite 저장 대신 MongoDB 저장 함수 호출
                    await SaveProcessDataToMongoDBAsync(modifiedDataTable, progressForm.UpdateProgressHandler);
                    Debug.WriteLine("SaveProcessDataToMongoDB 수행 완료");

                    await progressForm.UpdateProgressHandler(70, "데이터 전송 중...");

                    Debug.WriteLine("UI 초기화 시작");
                    // initUI가 이미 Task를 반환하므로 추가 Task.Run 없이 직접 await
                    await userControlHandler.uc_dataTransform.initUI();

                    await progressForm.UpdateProgressHandler(90, "데이터 전송 완료");

                    if (this.ParentForm is Form1 form)
                    {
                        Debug.WriteLine("btn_complete_Click -> LoadUserControl start");
                        form.LoadUserControl(userControlHandler.uc_dataTransform);
                        Debug.WriteLine("btn_complete_Click -> LoadUserControl complete");
                        await progressForm.UpdateProgressHandler(100, "데이터 저장 완료");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 처리 완료 중 오류 발생: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine($"데이터 처리 완료 오류: {ex.Message}");
            }
        }

        private async Task SaveProcessDataToMongoDBAsync(DataTable dataTable, ProcessProgressForm.UpdateProgressDelegate progress)
        {
            try
            {
                await progress(20, "MongoDB 컬렉션 준비 중...");

                // 처리 시작 시간 기록 (성능 측정용)
                var startTime = DateTime.Now;
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SaveProcessDataToMongoDBAsync 시작");

                // 프로세스 뷰 저장소 생성
                var processViewRepo = new Repositories.ProcessViewRepository();
                var processDataRepo = new Repositories.ProcessDataRepository();
                var rawDataRepo = new Repositories.RawDataRepository();

                // 기존 문서 수 확인
                var emptyFilter = MongoDB.Driver.Builders<MongoModels.ProcessViewDocument>.Filter.Empty;
                long existingCount = await Data.MongoDBManager.Instance.GetCollectionAsync<MongoModels.ProcessViewDocument>("process_view_data")
                    .Result.CountDocumentsAsync(emptyFilter);

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 기존 process_view_data 컬렉션 문서 수: {existingCount}개");

                // dataGridView_applied에서 데이터 가져오기
                var dgvApplied = dataGridView_applied;
                int totalRows = dgvApplied.Rows.Count;

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] dataGridView_applied 행 수: {totalRows}개");

                if (totalRows == 0)
                {
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] dataGridView_applied가 비어있습니다. 저장할 데이터가 없습니다.");
                    await progress(100, "저장할 데이터가 없습니다.");
                    return;
                }

                // 부서/공급업체 정보 캐싱
                Dictionary<string, string> deptCache = new Dictionary<string, string>();
                Dictionary<string, string> prodCache = new Dictionary<string, string>();

                // 부서/공급업체 정보가 필요한 경우 미리 로드
                if (DataHandler.dept_col_yn || DataHandler.prod_col_yn)
                {
                    await progress(25, "부서/공급업체 정보 로드 중...");

                    // RawData 컬렉션에서 부서/공급업체 정보 로드
                    var processDataDocs = await processDataRepo.GetAllAsync();

                    foreach (var doc in processDataDocs)
                    {
                        if (!string.IsNullOrEmpty(doc.RawDataId) && doc.Data != null)
                        {
                            if (DataHandler.dept_col_yn && doc.Data.ContainsKey(DataHandler.dept_col_name))
                            {
                                string deptValue = doc.Data[DataHandler.dept_col_name]?.ToString();
                                if (!string.IsNullOrEmpty(deptValue))
                                {
                                    deptCache[doc.RawDataId] = deptValue;
                                }
                            }

                            if (DataHandler.prod_col_yn && doc.Data.ContainsKey(DataHandler.prod_col_name))
                            {
                                string prodValue = doc.Data[DataHandler.prod_col_name]?.ToString();
                                if (!string.IsNullOrEmpty(prodValue))
                                {
                                    prodCache[doc.RawDataId] = prodValue;
                                }
                            }
                        }
                    }

                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 부서 정보 캐싱: {deptCache.Count}개, 공급업체 정보 캐싱: {prodCache.Count}개");
                }

                int processedRows = 0;
                int validRows = 0;
                int invalidRows = 0;

                // 시스템 리소스에 맞게 최적화된 스레드 수 계산
                var processorCount = Environment.ProcessorCount;
                int optimalThreads = Math.Max(2, Math.Min(processorCount - 1, 8));

                // 데이터 크기에 따른 적응형 배치 크기 결정
                int batchSize = DetermineBatchSize(totalRows);
                int batchCount = (int)Math.Ceiling(totalRows / (double)batchSize);

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 병렬 처리 시작: {optimalThreads}개 스레드, {batchCount}개 배치, 배치당 {batchSize}행");
                await progress(30, $"데이터 변환 준비 중... (총 {totalRows}행)");

                // MongoDB 문서 리스트 생성
                var processViewDocuments = new List<MongoModels.ProcessViewDocument>();

                // 1. DataHandler.moneyDataTable을 Dictionary로 변환하여 빠른 조회 가능하게 함
                Dictionary<string, object> moneyDataMap = new Dictionary<string, object>();

                // 수정 필요한 코드 부분:
                if (DataHandler.moneyDataTable != null)
                {
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] DataHandler.moneyDataTable에서 금액 정보 로드 중... 행 수: {DataHandler.moneyDataTable.Rows.Count}개");

                    // DataHandler.levelName[0]를 금액 컬럼명으로 사용
                    string moneyColumnName = DataHandler.levelName[0];
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 금액 컬럼명: {moneyColumnName}");

                    foreach (DataRow moneyRow in DataHandler.moneyDataTable.Rows)
                    {
                        if (moneyRow["raw_data_id"] != DBNull.Value)
                        {
                            string rawDataId = moneyRow["raw_data_id"].ToString();
                            if (!string.IsNullOrEmpty(rawDataId))
                            {
                                // 주의: DataHandler.ExtractColumnToNewTable 함수는 특정 컬럼을 추출하여
                                // 첫 번째 컬럼으로 만들므로, moneyRow[0] 또는 moneyRow[1]에서 값을 가져와야 함
                                // moneyDataTable의 실제 구조 확인 필요
                                object moneyValue = null;

                                // DataHandler.moneyDataTable의 구조에 따라 아래 부분 수정
                                // ExtractColumnToNewTable 함수의 구현에 따르면 데이터는 Column0에 있을 가능성이 높음
                                if (moneyRow.Table.Columns.Contains(moneyColumnName))
                                {
                                    moneyValue = moneyRow[moneyColumnName];
                                }
                                else if (moneyRow.Table.Columns.Contains("Column0"))
                                {
                                    moneyValue = moneyRow["Column0"];
                                }
                                else if (moneyRow.Table.Columns.Count > 1)
                                {
                                    // 첫 번째 컬럼이 raw_data_id가 아닌 경우 두 번째 컬럼에 금액이 있을 수 있음
                                    moneyValue = moneyRow[1];
                                }

                                if (moneyValue != null && moneyValue != DBNull.Value)
                                {
                                    moneyDataMap[rawDataId] = moneyValue;
                                    // 디버깅을 위해 처음 몇 개 값 출력
                                    if (moneyDataMap.Count <= 5)
                                    {
                                        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 금액 매핑: rawDataId={rawDataId}, 금액={moneyValue}");
                                    }
                                }
                            }
                        }
                    }

                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 금액 정보 매핑 생성 완료: {moneyDataMap.Count}개");
                }

                // 2. raw_data_id와 process_data_id 매핑 준비
                Dictionary<string, string> processDataToRawDataMap = new Dictionary<string, string>();
                Dictionary<string, string> rawDataToProcessDataMap = new Dictionary<string, string>();

                var allProcessData = await processDataRepo.GetAllAsync();
                foreach (var doc in allProcessData)
                {
                    if (doc.Id != null && !string.IsNullOrEmpty(doc.RawDataId))
                    {
                        processDataToRawDataMap[doc.Id] = doc.RawDataId;
                        rawDataToProcessDataMap[doc.RawDataId] = doc.Id;
                    }
                }

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ID 매핑 준비 완료: {processDataToRawDataMap.Count}개");

                // 3. dataGridView_applied에서 행 별로 데이터 추출
                for (int rowIndex = 0; rowIndex < dgvApplied.Rows.Count; rowIndex++)
                {
                    if (rowIndex % 100 == 0)
                    {
                        int percentage = (int)(30 + (rowIndex * 30.0 / totalRows));
                        await progress(percentage, $"문서 변환 중... ({rowIndex}/{totalRows})");
                    }

                    processedRows++;

                    // raw_data_id 확인 - "raw_data_id" 컬럼 또는 숨겨진 컬럼일 수 있음
                    string rawDataId = null;

                    // DataGridView의 행에서 raw_data_id 찾기 (숨겨진 컬럼일 수 있음)
                    for (int colIndex = 0; colIndex < dgvApplied.Columns.Count; colIndex++)
                    {
                        if (dgvApplied.Columns[colIndex].Name.Equals("raw_data_id", StringComparison.OrdinalIgnoreCase))
                        {
                            rawDataId = dgvApplied.Rows[rowIndex].Cells[colIndex].Value?.ToString();
                            break;
                        }
                    }

                    // DataGridView에서 raw_data_id를 찾지 못했다면 원본 DataTable에서 찾기 시도
                    if (string.IsNullOrEmpty(rawDataId) && dataTable.Columns.Contains("raw_data_id"))
                    {
                        if (rowIndex < dataTable.Rows.Count)
                        {
                            rawDataId = dataTable.Rows[rowIndex]["raw_data_id"]?.ToString();
                        }
                    }

                    // process_data_id 확인 - 마찬가지로 숨겨진 컬럼일 수 있음
                    string processDataId = null;
                    for (int colIndex = 0; colIndex < dgvApplied.Columns.Count; colIndex++)
                    {
                        if (dgvApplied.Columns[colIndex].Name.Equals("process_data_id", StringComparison.OrdinalIgnoreCase))
                        {
                            processDataId = dgvApplied.Rows[rowIndex].Cells[colIndex].Value?.ToString();
                            break;
                        }
                    }

                    // process_data_id가 없으면 원본 DataTable에서 찾기 시도
                    if (string.IsNullOrEmpty(processDataId) && dataTable.Columns.Contains("process_data_id"))
                    {
                        if (rowIndex < dataTable.Rows.Count)
                        {
                            processDataId = dataTable.Rows[rowIndex]["process_data_id"]?.ToString();
                        }
                    }

                    // raw_data_id와 process_data_id를 서로 보완
                    if (string.IsNullOrEmpty(rawDataId) && !string.IsNullOrEmpty(processDataId))
                    {
                        if (processDataToRawDataMap.TryGetValue(processDataId, out string mappedRawDataId))
                        {
                            rawDataId = mappedRawDataId;
                        }
                    }
                    else if (!string.IsNullOrEmpty(rawDataId) && string.IsNullOrEmpty(processDataId))
                    {
                        if (rawDataToProcessDataMap.TryGetValue(rawDataId, out string mappedProcessDataId))
                        {
                            processDataId = mappedProcessDataId;
                        }
                    }

                    // rawDataId 유효성 검사
                    bool isValidRawDataId = !string.IsNullOrEmpty(rawDataId) &&
                                           MongoDB.Bson.ObjectId.TryParse(rawDataId, out _);

                    if (!isValidRawDataId)
                    {
                        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 행 {rowIndex}: 유효하지 않은 rawDataId='{rawDataId}', 건너뜀");
                        invalidRows++;
                        continue; // 유효하지 않은 경우 건너뛰기
                    }

                    // 4. 금액 데이터 처리 - DataHandler.moneyDataTable에서 가져옴
                    object moneyValue = null;
                    if (moneyDataMap.TryGetValue(rawDataId, out object mappedMoneyValue))
                    {
                        moneyValue = mappedMoneyValue;
                    }

                    // 5. 키워드 목록 추출 - dataGridView_applied에서 2번째 컬럼부터 마지막 컬럼까지
                    var finalKeywords = new List<string>();

                    // 2번째 컬럼부터 시작 (일반적으로 0번째는 체크박스, 1번째는 금액 또는 다른 데이터)
                    // 컬럼 이름으로 1번을 찾지 않고 실제 인덱스 사용
                    for (int colIndex = 2; colIndex < dgvApplied.Columns.Count; colIndex++)
                    {
                        // 메타데이터 컬럼 건너뛰기
                        if (dgvApplied.Columns[colIndex].Name.Equals("raw_data_id", StringComparison.OrdinalIgnoreCase) ||
                            dgvApplied.Columns[colIndex].Name.Equals("process_data_id", StringComparison.OrdinalIgnoreCase) ||
                            dgvApplied.Columns[colIndex].Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // 셀 값이 있으면 키워드로 추가
                        object cellValue = dgvApplied.Rows[rowIndex].Cells[colIndex].Value;
                        if (cellValue != null && cellValue != DBNull.Value)
                        {
                            string keyword = cellValue.ToString();
                            if (!string.IsNullOrWhiteSpace(keyword))
                            {
                                finalKeywords.Add(keyword.Trim());
                            }
                        }
                    }

                    // 6. ProcessViewDocument 생성
                    var processViewDoc = new MongoModels.ProcessViewDocument
                    {
                        ProcessDataId = processDataId,
                        RawDataId = rawDataId,
                        Keywords = new MongoModels.KeywordInfo
                        {
                            FinalKeywords = finalKeywords
                        },
                        Money = moneyValue,
                        LastModifiedDate = DateTime.Now
                    };

                    // 부서/공급업체 정보 추가
                    if (DataHandler.dept_col_yn && deptCache.TryGetValue(rawDataId, out string deptValue))
                    {
                        processViewDoc.Department = deptValue;
                    }

                    if (DataHandler.prod_col_yn && prodCache.TryGetValue(rawDataId, out string prodValue))
                    {
                        processViewDoc.Supplier = prodValue;
                    }

                    // 디버그 로깅 
                    if (processViewDocuments.Count < 5)
                    {
                        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 생성된 문서 샘플 #{processViewDocuments.Count + 1}: " +
                                      $"ProcessDataId={processDataId}, RawDataId={rawDataId}, " +
                                      $"Keywords={string.Join(", ", finalKeywords)}, " +
                                      $"Money={moneyValue}, " +
                                      $"Department={processViewDoc.Department}, " +
                                      $"Supplier={processViewDoc.Supplier}");
                    }

                    processViewDocuments.Add(processViewDoc);
                    validRows++;
                }

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 문서 변환 완료: 총 {processedRows}개 처리, {validRows}개 유효, {invalidRows}개 무효");

                if (processViewDocuments.Count == 0)
                {
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 삽입할 유효한 문서가 없습니다.");
                    await progress(100, "삽입할 유효한 문서가 없습니다.");
                    return;
                }

                // 문서를 배치로 분할하여 MongoDB에 삽입
                await progress(60, $"MongoDB에 데이터 삽입 중... ({processViewDocuments.Count}건)");

                // 배치 단위로 분할
                var batches = new List<List<MongoModels.ProcessViewDocument>>();
                for (int i = 0; i < processViewDocuments.Count; i += batchSize)
                {
                    batches.Add(processViewDocuments.Skip(i).Take(batchSize).ToList());
                }

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 총 {batches.Count}개 배치로 분할됨");

                // 병렬 삽입 작업 수행 전 유효성 추가 점검
                foreach (var batch in batches)
                {
                    // 각 배치의 모든 문서가 유효한 raw_data_id를 가지는지 확인
                    for (int i = batch.Count - 1; i >= 0; i--)
                    {
                        var doc = batch[i];
                        if (string.IsNullOrEmpty(doc.RawDataId) || !MongoDB.Bson.ObjectId.TryParse(doc.RawDataId, out _))
                        {
                            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 유효하지 않은 rawDataId='{doc.RawDataId}' 문서 제거");
                            batch.RemoveAt(i);
                        }
                    }
                }

                // 성공한 삽입 수 추적
                int successfulInserts = 0;

                // 병렬 삽입 작업 (SemaphoreSlim을 사용하여 동시성 제한)
                using (var semaphore = new SemaphoreSlim(optimalThreads))
                {
                    var tasks = new List<Task>();
                    int completedBatches = 0;

                    foreach (var batch in batches)
                    {
                        if (batch.Count == 0)
                        {
                            // 빈 배치는 건너뛰기
                            completedBatches++;
                            continue;
                        }

                        await semaphore.WaitAsync();

                        tasks.Add(Task.Run(async () => {
                            try
                            {
                                // 재시도 로직 (최대 3회)
                                for (int attempt = 1; attempt <= 3; attempt++)
                                {
                                    try
                                    {
                                        await processViewRepo.InsertManyAsync(batch);

                                        // 성공 시 카운트 증가
                                        Interlocked.Add(ref successfulInserts, batch.Count);

                                        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 배치 삽입 성공: {batch.Count}개 문서");
                                        break; // 성공시 루프 탈출
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 배치 삽입 {attempt}번째 시도 실패: {ex.Message}");

                                        if (attempt < 3)
                                        {
                                            // 마지막 시도가 아니면 잠시 대기 후 재시도
                                            await Task.Delay(1000 * attempt);
                                        }
                                        else if (batch.Count > 1)
                                        {
                                            // 마지막 시도에서도 실패하고 배치 크기가 1보다 크면
                                            // 개별 문서 단위로 처리
                                            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 개별 문서 단위로 처리 시도 (총 {batch.Count}개)");

                                            foreach (var doc in batch)
                                            {
                                                try
                                                {
                                                    await processViewRepo.InsertOneAsync(doc);
                                                    Interlocked.Increment(ref successfulInserts);
                                                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 문서 삽입 성공: ProcessDataId={doc.ProcessDataId}");
                                                }
                                                catch (Exception docEx)
                                                {
                                                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 문서 삽입 실패: ProcessDataId={doc.ProcessDataId}, RawDataId={doc.RawDataId}, 오류={docEx.Message}");
                                                }
                                            }
                                        }
                                    }
                                }

                                int completed = Interlocked.Increment(ref completedBatches);
                                int percentage = (int)(60 + (completed * 20.0 / batches.Count));
                                await progress(percentage, $"MongoDB 저장 중: {completed}/{batches.Count} 배치 완료");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 배치 삽입 중 오류: {ex.Message}");
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }));
                    }

                    await Task.WhenAll(tasks);
                }

                // 최종 확인
                long finalCount = await processViewRepo.CountDocumentsAsync();
                long insertedCount = finalCount - existingCount;

                var endTime = DateTime.Now;
                var duration = endTime - startTime;

                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MongoDB 저장 완료: {insertedCount}개 문서 삽입됨 ({successfulInserts}개 성공)");
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 처리 시간: {duration.TotalSeconds:F2}초 (시작: {startTime:HH:mm:ss.fff}, 종료: {endTime:HH:mm:ss.fff})");

                await progress(80, $"데이터 저장 완료: {insertedCount}개 문서 삽입됨");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MongoDB 저장 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }




        // AI 관련 설정
        int ai_limit_cnt = 4;

        private void ai_limit_count_ValueChanged(object sender, EventArgs e)
        {
            ai_limit_cnt = (int)ai_limit_count.Value;
            Debug.WriteLine($"ai_limit_cnt : {ai_limit_cnt}");
        }

        // 구분자 및 불용어 관리 관련 메서드
        private void new_seper_word_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                add_seperate_keyword();
                // Enter 키가 다른 동작을 막도록 처리
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void seper_add_btn_Click(object sender, EventArgs e)
        {
            add_seperate_keyword();
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

        private void tb_remove_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                add_remove_keyword();

                // Enter 키가 다른 동작을 막도록 처리
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void remove_add_btn_Click(object sender, EventArgs e)
        {
            add_remove_keyword();
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

        private void seper_list_allcheck_CheckedChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView_seperator.Rows)
            {
                row.Cells[0].Value = seper_list_allcheck.Checked;
            }
        }

        private void remove_list_allcheck_CheckedChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView_remove.Rows)
            {
                row.Cells[0].Value = remove_list_allcheck.Checked;
            }
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

        private void seper_del_btn_Click(object sender, EventArgs e)
        {
            List<string> seper_del_list = GetCheckedRowsData(dataGridView_seperator);

            if (seper_del_list.Count == 0)
            {
                MessageBox.Show("구분자 변환 제거 대상을 선택하셔야 합니다.", "알림",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Warning);

                return;
            }

            foreach (string seperate in seper_del_list)
            {
                //_separatorManager.Separators.Remove(seperate);
                DataHandler.spManager.RemoveSeparator(seperate);
            }

            for (int i = dataGridView_seperator.Rows.Count - 1; i >= 0; i--)
            {
                DataGridViewRow row = dataGridView_seperator.Rows[i];

                // columnListDgv의 두 번째 컬럼(체크박스 다음)의 값 확인
                string seperData = row.Cells[1].Value?.ToString();
                if (seper_del_list.Contains(seperData))
                {
                    dataGridView_seperator.Rows.RemoveAt(i);
                }
            }
        }

        private void remove_del_btn_Click(object sender, EventArgs e)
        {
            List<string> remove_del_list = GetCheckedRowsData(dataGridView_remove);

            if (remove_del_list.Count == 0)
            {
                MessageBox.Show("불용어 항목 제거 대상을 선택하셔야 합니다.", "알림",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Warning);

                return;
            }

            foreach (string remove in remove_del_list)
            {
                //DataHandler.remover.Remove(remove);
                DataHandler.spManager.RemoveRemover(remove);
            }

            for (int i = dataGridView_remove.Rows.Count - 1; i >= 0; i--)
            {
                DataGridViewRow row = dataGridView_remove.Rows[i];

                // columnListDgv의 두 번째 컬럼(체크박스 다음)의 값 확인
                string removeData = row.Cells[1].Value?.ToString();
                if (remove_del_list.Contains(removeData))
                {
                    dataGridView_remove.Rows.RemoveAt(i);
                }
            }
        }
    }
}