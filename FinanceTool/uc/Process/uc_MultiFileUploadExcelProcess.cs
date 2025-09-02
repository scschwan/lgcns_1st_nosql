using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FinanceTool.MongoModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTool
{
    public partial class uc_MultiFileUpload
    {
        /// <summary>
        /// 계정명별 파티션 분석 (디버깅 강화 + 안정성 개선 버전)
        /// </summary>
        private async Task<PartitionAnalysisResult> AnalyzeAccountPartitionsAsync(List<FileDisplayData> selectedFiles, ProcessProgressForm.UpdateProgressDelegate progressCallback)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                await progressCallback(5, "파일 그룹화 분석 중...");
                await Task.Delay(10);

                Debug.WriteLine($"[파티션분석] 시작 - 선택된 파일: {selectedFiles.Count}개");

                // 1단계: 파일별 계정명 데이터를 미리 캐싱 (병렬 처리)
                Debug.WriteLine($"[파티션분석] 1단계 시작 - 파일 데이터 캐싱");
                var fileAccountCache = await PreloadFileAccountDataAsync(selectedFiles, progressCallback);
                Debug.WriteLine($"[파티션분석] 1단계 완료 - 캐시된 파일: {fileAccountCache.Count}개");

                await progressCallback(25, "계정별 그룹화 중...");
                await Task.Delay(10);

                // 2단계: 메모리 안전한 계정별 그룹화 (청크 단위 처리)
                Debug.WriteLine($"[파티션분석] 2단계 시작 - 계정별 그룹화");

                var accountGroups = new Dictionary<string, List<FileDisplayData>>();
                var totalAccounts = 0;

                // 메모리 안전을 위해 파일별로 순차 처리하여 그룹화
                foreach (var kvp in fileAccountCache)
                {
                    var file = kvp.Key;
                    var accountData = kvp.Value;

                    foreach (var accountName in accountData.Keys)
                    {
                        if (!accountGroups.ContainsKey(accountName))
                        {
                            accountGroups[accountName] = new List<FileDisplayData>();
                        }

                        if (!accountGroups[accountName].Contains(file))
                        {
                            accountGroups[accountName].Add(file);
                        }
                    }

                    totalAccounts += accountData.Count;
                    Debug.WriteLine($"[파티션분석] 그룹화 진행 - {file.OriginalFilename}: {accountData.Count}개 계정, 총 계정: {totalAccounts}개");
                }

                Debug.WriteLine($"[파티션분석] 2단계 완료 - 계정 그룹: {accountGroups.Count}개");

                await progressCallback(40, "계정별 데이터 병렬 계산 중...");
                await Task.Delay(10);

                // 3단계: 안전한 병렬 파티션 생성 (배치 단위 처리)
                Debug.WriteLine($"[파티션분석] 3단계 시작 - 파티션 생성");

                var partitionResults = new List<AccountPartition>();
                var accountGroupsList = accountGroups.ToList();

                // 배치 크기를 줄여서 메모리 압박 완화
                int batchSize = Math.Min(100, Math.Max(10, accountGroupsList.Count / 10));
                int processedCount = 0;

                for (int i = 0; i < accountGroupsList.Count; i += batchSize)
                {
                    var batch = accountGroupsList.Skip(i).Take(batchSize).ToList();

                    Debug.WriteLine($"[파티션분석] 배치 처리 시작 - {i + 1}~{Math.Min(i + batchSize, accountGroupsList.Count)}/{accountGroupsList.Count}");

                    try
                    {
                        // 배치 단위로 병렬 처리
                        var batchResults = batch
                            .AsParallel()
                            .WithDegreeOfParallelism(Math.Min(Environment.ProcessorCount, batch.Count))
                            .Select(accountGroup =>
                            {
                                try
                                {
                                    var partition = ProcessAccountGroupSafe(
                                        accountGroup.Key,
                                        accountGroup.Value,
                                        fileAccountCache,
                                        Interlocked.Increment(ref processedCount),
                                        accountGroupsList.Count
                                    );

                                    return partition;
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[파티션분석] 개별 계정 처리 오류 - {accountGroup.Key}: {ex.Message}");
                                    return null;
                                }
                            })
                            .Where(p => p != null)
                            .ToList();

                        partitionResults.AddRange(batchResults);

                        // 진행률 업데이트
                        var progress = 40 + (processedCount * 40 / accountGroupsList.Count);
                        await progressCallback(progress, $"파티션 생성 중... ({processedCount}/{accountGroupsList.Count})");
                        await Task.Delay(10);

                        Debug.WriteLine($"[파티션분석] 배치 완료 - 처리된 계정: {processedCount}/{accountGroupsList.Count}, 생성된 파티션: {batchResults.Count}개");

                        // 메모리 정리
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[파티션분석] 배치 처리 오류: {ex.Message}");
                        // 배치 실패 시 개별 처리로 폴백
                        foreach (var accountGroup in batch)
                        {
                            try
                            {
                                var partition = ProcessAccountGroupSafe(
                                    accountGroup.Key,
                                    accountGroup.Value,
                                    fileAccountCache,
                                    Interlocked.Increment(ref processedCount),
                                    accountGroupsList.Count
                                );

                                if (partition != null)
                                {
                                    partitionResults.Add(partition);
                                }
                            }
                            catch (Exception individualEx)
                            {
                                Debug.WriteLine($"[파티션분석] 개별 폴백 처리 오류 - {accountGroup.Key}: {individualEx.Message}");
                            }
                        }
                    }
                }

                sw.Stop();

                await progressCallback(90, "파티션 검증 중...");
                await Task.Delay(10);
                Debug.WriteLine($"[파티션분석] 3단계 완료 - 소요시간: {sw.ElapsedMilliseconds:N0}ms, 생성된 파티션: {partitionResults.Count}개");

                // 파티션 검증
                if (partitionResults.Count == 0)
                {
                    Debug.WriteLine($"[파티션분석] 검증 실패 - 파티션이 생성되지 않음");
                    return new PartitionAnalysisResult
                    {
                        IsValid = false,
                        ErrorMessage = "계정명을 기준으로 파티션을 생성할 수 없습니다.\n선택된 파일들의 계정명 정보를 확인해주세요."
                    };
                }

                await progressCallback(100, "파티션 분석 완료");

                Debug.WriteLine($"[파티션분석] 전체 완료 - 총 소요시간: {sw.ElapsedMilliseconds:N0}ms");

                return new PartitionAnalysisResult
                {
                    IsValid = true,
                    Partitions = partitionResults
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[파티션분석] 최상위 오류 발생: {ex.Message}");
                Debug.WriteLine($"[파티션분석] 스택 트레이스: {ex.StackTrace}");
                return new PartitionAnalysisResult
                {
                    IsValid = false,
                    ErrorMessage = $"파티션 분석 중 오류가 발생했습니다: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 개별 계정 그룹을 안전하게 처리
        /// </summary>
        private AccountPartition ProcessAccountGroupSafe(
            string accountName,
            List<FileDisplayData> files,
            Dictionary<FileDisplayData, Dictionary<string, (int RowCount, decimal TotalAmount)>> fileAccountCache,
            int index,
            int totalGroups)
        {
            try
            {
                Debug.WriteLine($"[계정처리] 시작 - {accountName} ({index}/{totalGroups})");

                var distinctFiles = files.Distinct().ToList();

                var partition = new AccountPartition
                {
                    AccountName = accountName,
                    Files = distinctFiles,
                    FileCount = distinctFiles.Count,
                    SessionName = GeneratePartitionSessionName(accountName, distinctFiles),
                    TotalRows = 0,
                    TotalAmount = 0
                };

                // 캐시된 데이터에서 안전하게 계산
                foreach (var file in distinctFiles)
                {
                    try
                    {
                        if (fileAccountCache.TryGetValue(file, out var accountData) &&
                            accountData.TryGetValue(accountName, out var data))
                        {
                            partition.TotalRows += data.RowCount;
                            partition.TotalAmount += data.TotalAmount;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[계정처리] 파일 데이터 오류 - {file.OriginalFilename}: {ex.Message}");
                    }
                }

                Debug.WriteLine($"[계정처리] 완료 - {accountName}: {partition.TotalRows:N0}행, {partition.TotalAmount:N0}원 ({index}/{totalGroups})");
                return partition;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[계정처리] 오류 - {accountName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 파일별 계정명 데이터 사전 로드 (안정성 강화)
        /// </summary>
        private async Task<Dictionary<FileDisplayData, Dictionary<string, (int RowCount, decimal TotalAmount)>>> PreloadFileAccountDataAsync(
            List<FileDisplayData> selectedFiles,
            ProcessProgressForm.UpdateProgressDelegate progressCallback)
        {
            var sw = Stopwatch.StartNew();
            Debug.WriteLine($"[데이터캐싱] 시작 - 캐싱할 파일: {selectedFiles.Count}개");

            var fileAccountCache = new Dictionary<FileDisplayData, Dictionary<string, (int RowCount, decimal TotalAmount)>>();

            // 안전을 위해 파일별로 순차 처리 (메모리 압박 완화)
            for (int i = 0; i < selectedFiles.Count; i++)
            {
                var file = selectedFiles[i];
                try
                {
                    Debug.WriteLine($"[데이터캐싱] 파일 처리 시작 - {file.OriginalFilename} ({i + 1}/{selectedFiles.Count})");

                    var accountData = LoadFileAccountDataSafe(file);
                    fileAccountCache[file] = accountData;

                    // 진행률 업데이트
                    var progress = 5 + ((i + 1) * 20 / selectedFiles.Count);
                    await progressCallback(progress, $"파일 데이터 캐싱 중... ({i + 1}/{selectedFiles.Count}) - {file.OriginalFilename}");

                    Debug.WriteLine($"[데이터캐싱] 파일 완료 - {file.OriginalFilename}: {accountData.Count}개 계정");

                    // 주기적으로 메모리 정리
                    if ((i + 1) % 5 == 0)
                    {
                        GC.Collect();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[데이터캐싱] 파일 오류 - {file.OriginalFilename}: {ex.Message}");
                    fileAccountCache[file] = new Dictionary<string, (int, decimal)>();
                }
            }

            sw.Stop();
            Debug.WriteLine($"[데이터캐싱] 전체 완료 - 소요시간: {sw.ElapsedMilliseconds:N0}ms, 성공한 파일: {fileAccountCache.Count}개");

            return fileAccountCache;
        }

        /// <summary>
        /// 개별 파일의 계정명별 데이터를 안전하게 로드
        /// </summary>
        private Dictionary<string, (int RowCount, decimal TotalAmount)> LoadFileAccountDataSafe(FileDisplayData file)
        {
            try
            {
                string filePath = Path.Combine(UPLOAD_FOLDER, file.StoredFilename);
                var sw = Stopwatch.StartNew();

                Debug.WriteLine($"[파일로드] 시작 - {file.OriginalFilename}");

                using (var document = SpreadsheetDocument.Open(filePath, false))
                {
                    var workbookPart = document.WorkbookPart;
                    var worksheet = workbookPart.WorksheetParts.First().Worksheet;
                    var sheetData = worksheet.GetFirstChild<SheetData>();

                    var allRows = sheetData.Elements<Row>().ToList();
                    if (allRows.Count <= 1)
                    {
                        Debug.WriteLine($"[파일로드] 빈 파일 - {file.OriginalFilename}");
                        return new Dictionary<string, (int, decimal)>();
                    }

                    // 헤더에서 컬럼 인덱스 찾기
                    var headerRow = allRows.First();
                    var headerCells = headerRow.Elements<Cell>().ToList();

                    int accountColumnIndex = FindColumnIndex(headerCells, file.AccountColumnName, workbookPart);
                    int amountColumnIndex = FindColumnIndex(headerCells, file.AmountColumnName, workbookPart);

                    if (accountColumnIndex == -1 || amountColumnIndex == -1)
                    {
                        Debug.WriteLine($"[파일로드] 컬럼 인덱스 오류 - {file.OriginalFilename}: account={accountColumnIndex}, amount={amountColumnIndex}");
                        return new Dictionary<string, (int, decimal)>();
                    }

                    var dataRows = allRows.Skip(1).ToList();

                    Debug.WriteLine($"[파일로드] 병렬 처리 시작 - {file.OriginalFilename}: {dataRows.Count:N0}행");

                    // 메모리 안전한 병렬 처리 (배치 단위)
                    var accountResults = new Dictionary<string, (int RowCount, decimal TotalAmount)>();
                    int batchSize = Math.Min(50000, Math.Max(10000, dataRows.Count / Environment.ProcessorCount));

                    for (int i = 0; i < dataRows.Count; i += batchSize)
                    {
                        var batch = dataRows.Skip(i).Take(batchSize).ToList();

                        try
                        {
                            var batchResults = batch
                                .AsParallel()
                                .WithDegreeOfParallelism(Environment.ProcessorCount)
                                .Select(row =>
                                {
                                    try
                                    {
                                        var cells = row.Elements<Cell>().ToList();
                                        if (accountColumnIndex >= cells.Count || amountColumnIndex >= cells.Count)
                                            return null;

                                        string accountValue = GetCellValue(cells[accountColumnIndex], workbookPart)?.Trim();
                                        if (string.IsNullOrEmpty(accountValue)) return null;

                                        string amountValue = GetCellValue(cells[amountColumnIndex], workbookPart);
                                        decimal amount = 0;
                                        if (!string.IsNullOrEmpty(amountValue))
                                        {
                                            decimal.TryParse(amountValue.Replace(",", ""), out amount);
                                        }

                                        return new { Account = accountValue, Amount = amount };
                                    }
                                    catch
                                    {
                                        return null;
                                    }
                                })
                                .Where(x => x != null)
                                .GroupBy(x => x.Account)
                                .ToList();

                            // 배치 결과를 메인 딕셔너리에 병합
                            foreach (var group in batchResults)
                            {
                                var accountName = group.Key;
                                var rowCount = group.Count();
                                var totalAmount = group.Sum(x => x.Amount);

                                if (accountResults.ContainsKey(accountName))
                                {
                                    var existing = accountResults[accountName];
                                    accountResults[accountName] = (existing.RowCount + rowCount, existing.TotalAmount + totalAmount);
                                }
                                else
                                {
                                    accountResults[accountName] = (rowCount, totalAmount);
                                }
                            }

                            Debug.WriteLine($"[파일로드] 배치 완료 - {file.OriginalFilename}: {i + 1}~{Math.Min(i + batchSize, dataRows.Count)}/{dataRows.Count}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[파일로드] 배치 오류 - {file.OriginalFilename}: {ex.Message}");
                        }
                    }

                    sw.Stop();
                    Debug.WriteLine($"[파일로드] {file.OriginalFilename} 완료 - 소요시간: {sw.ElapsedMilliseconds:N0}ms, 계정: {accountResults.Count}개");

                    return accountResults;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[파일로드] 최상위 오류 - {file.OriginalFilename}: {ex.Message}");
                Debug.WriteLine($"[파일로드] 스택 트레이스: {ex.StackTrace}");
                return new Dictionary<string, (int, decimal)>();
            }
        }

        /// <summary>
        /// 계정명 컬럼 내용 검증 및 추출
        /// </summary>
        /// <summary>
        /// 계정명 컬럼 내용 검증 및 추출 (초고속 병렬 처리 버전)
        /// 192GB 메모리와 멀티코어 CPU를 최대한 활용
        /// </summary>
        private async Task<ValidationResult> ValidateAndExtractAccountContent(FileDisplayData fileData)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string filePath = Path.Combine(UPLOAD_FOLDER, fileData.StoredFilename);
                    var sw = Stopwatch.StartNew();

                    Debug.WriteLine($"[병렬처리] 시작 - 파일: {fileData.OriginalFilename}");

                    using (var document = SpreadsheetDocument.Open(filePath, false))
                    {
                        var workbookPart = document.WorkbookPart;
                        var worksheet = workbookPart.WorksheetParts.First().Worksheet;
                        var sheetData = worksheet.GetFirstChild<SheetData>();

                        var allRows = sheetData.Elements<Row>().ToList();
                        if (allRows.Count <= 1)
                        {
                            Application.OpenForms[0].Invoke((MethodInvoker)delegate
                            {
                                fileData.AccountContents = new List<string>();
                                fileData.AccountContentFormatted = "";
                            });
                            return new ValidationResult { IsValid = true };
                        }

                        Debug.WriteLine($"[병렬처리] 총 행 수: {allRows.Count:N0}");

                        // 1단계: 헤더 분석 (단일 스레드)
                        var headerRow = allRows.First();
                        var headerCells = headerRow.Elements<Cell>().ToList();
                        int accountColumnIndex = -1;

                        for (int i = 0; i < headerCells.Count; i++)
                        {
                            string cellValue = GetCellValue(headerCells[i], workbookPart);
                            if (cellValue.Trim().Equals(fileData.AccountColumnName.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                accountColumnIndex = i;
                                break;
                            }
                        }

                        if (accountColumnIndex == -1)
                        {
                            return new ValidationResult
                            {
                                IsValid = false,
                                ErrorMessage = $"계정명 컬럼 '{fileData.AccountColumnName}'을 찾을 수 없습니다."
                            };
                        }

                        Debug.WriteLine($"[병렬처리] 계정명 컬럼 인덱스: {accountColumnIndex}");

                        // 2단계: 데이터 행만 추출 (헤더 제외)
                        var dataRows = allRows.Skip(1).ToList();

                        // 3단계: 초고속 병렬 처리를 위한 설정
                        var parallelOptions = new ParallelOptions
                        {
                            MaxDegreeOfParallelism = Environment.ProcessorCount * 4, // CPU 코어수의 4배
                            TaskScheduler = TaskScheduler.Default
                        };

                        // 4단계: 메모리 최적화된 병렬 데이터 추출
                        // ConcurrentHashSet 대신 Thread-Safe Dictionary 사용 (더 빠름)
                        var accountDict = new ConcurrentDictionary<string, byte>();
                        var processedCount = 0;
                        var totalRows = dataRows.Count;

                        Debug.WriteLine($"[병렬처리] 병렬 처리 시작 - MaxDegreeOfParallelism: {parallelOptions.MaxDegreeOfParallelism}");

                        // PLINQ를 사용한 초고속 병렬 처리
                        var validAccounts = dataRows
                            .AsParallel()
                            .WithDegreeOfParallelism(parallelOptions.MaxDegreeOfParallelism)
                            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                            .Select((row, index) =>
                            {
                                // 진행률 업데이트 (1000개마다)
                                if (Interlocked.Increment(ref processedCount) % 1000 == 0)
                                {
                                    var progress = (processedCount * 100) / totalRows;
                                    Debug.WriteLine($"[병렬처리] 진행률: {progress}% ({processedCount:N0}/{totalRows:N0})");
                                }

                                var cells = row.Elements<Cell>().ToList();
                                if (accountColumnIndex < cells.Count)
                                {
                                    string cellValue = GetCellValue(cells[accountColumnIndex], workbookPart);
                                    if (!string.IsNullOrWhiteSpace(cellValue))
                                    {
                                        return cellValue.Trim();
                                    }
                                }
                                return null;
                            })
                            .Where(value => !string.IsNullOrEmpty(value))
                            .Distinct() // PLINQ의 Distinct는 병렬로 처리됨
                            .ToList();

                        sw.Stop();
                        Debug.WriteLine($"[병렬처리] 완료 - 소요시간: {sw.ElapsedMilliseconds:N0}ms, 고유 계정명: {validAccounts.Count}개");

                        // 5단계: UI 스레드에서 결과 업데이트
                        Application.OpenForms[0].Invoke((MethodInvoker)delegate
                        {
                            fileData.AccountContents = validAccounts;

                            // 계정명 내용 포맷팅
                            if (validAccounts.Count == 0)
                            {
                                fileData.AccountContentFormatted = "";
                            }
                            else if (validAccounts.Count == 1)
                            {
                                fileData.AccountContentFormatted = validAccounts[0];
                            }
                            else if (validAccounts.Count <= 3)
                            {
                                fileData.AccountContentFormatted = string.Join(", ", validAccounts) + $" ({validAccounts.Count}개)";
                            }
                            else
                            {
                                fileData.AccountContentFormatted = string.Join(", ", validAccounts.Take(3)) + $"... ({validAccounts.Count}개)";
                            }
                        });

                        // 6단계: 계정명 개수별 처리 로직
                        if (validAccounts.Count > 40)
                        {
                            string warningMessage = $"계정명 종류가 40개를 초과했습니다. ({validAccounts.Count}개)\n" +
                                                  $"병렬 처리로 {sw.ElapsedMilliseconds:N0}ms 만에 완료되었습니다.\n" +
                                                  "성능상 권장하지 않지만 처리를 계속합니다.\n" +
                                                  "처음 5개 계정명:\n" +
                                                  string.Join("\n", validAccounts.Take(5).Select((v, i) => $"{i + 1}. {v}"));

                            Application.OpenForms[0].BeginInvoke((MethodInvoker)delegate
                            {
                                Form progressForm = Application.OpenForms.Cast<Form>()
                                    .FirstOrDefault(f => f.GetType().Name == "ProcessProgressForm");

                                if (progressForm != null)
                                {
                                    progressForm.Hide();
                                }

                                MessageBox.Show(warningMessage, "계정명 개수 경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                                if (progressForm != null)
                                {
                                    progressForm.Show();
                                }
                            });
                        }
                        else if (validAccounts.Count > 1)
                        {
                            string infoMessage = $"계정명이 {validAccounts.Count}개 감지되었습니다.\n" +
                                               $"병렬 처리로 {sw.ElapsedMilliseconds:N0}ms 만에 완료되었습니다.\n" +
                                               "세션 생성 시 계정명별로 분리됩니다.\n" +
                                               "감지된 계정명:\n" +
                                               string.Join("\n", validAccounts.Take(10).Select((v, i) => $"{i + 1}. {v}")) +
                                               (validAccounts.Count > 10 ? $"\n... 외 {validAccounts.Count - 10}개 더" : "");

                            Application.OpenForms[0].BeginInvoke((MethodInvoker)delegate
                            {
                                Form progressForm = Application.OpenForms.Cast<Form>()
                                    .FirstOrDefault(f => f.GetType().Name == "ProcessProgressForm");

                                if (progressForm != null)
                                {
                                    progressForm.Hide();
                                }

                                try
                                {
                                    var result = MessageBox.Show(infoMessage + "\n계속 진행하시겠습니까?",
                                        "계정명 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                                    if (result != DialogResult.Yes)
                                    {
                                        fileData.AccountColumnName = "";
                                        fileData.AccountContents.Clear();
                                        fileData.AccountContentFormatted = "";
                                    }
                                }
                                finally
                                {
                                    // progressForm은 의도적으로 재표시하지 않음 (사용자 경험 개선)
                                }
                            });
                        }

                        Debug.WriteLine($"[병렬처리] 전체 완료 - 파일: {fileData.OriginalFilename}, 처리시간: {sw.ElapsedMilliseconds:N0}ms");
                        return new ValidationResult { IsValid = true };
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[병렬처리] 오류 발생: {ex.Message}");
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"계정명 컬럼 검증 중 오류가 발생했습니다: {ex.Message}"
                    };
                }
            });
        }


        /// <summary>
        /// 금액 컬럼 검증 및 합계 계산
        /// </summary>
        /// <summary>
        /// 금액 컬럼 검증 및 합계 계산 (초고속 병렬 처리 버전)
        /// </summary>
        private async Task<ValidationResult> ValidateAndCalculateAmount(FileDisplayData fileData)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string filePath = Path.Combine(UPLOAD_FOLDER, fileData.StoredFilename);
                    var sw = Stopwatch.StartNew();

                    Debug.WriteLine($"[병렬금액처리] 시작 - 파일: {fileData.OriginalFilename}");

                    using (var document = SpreadsheetDocument.Open(filePath, false))
                    {
                        var workbookPart = document.WorkbookPart;
                        var worksheet = workbookPart.WorksheetParts.First().Worksheet;
                        var sheetData = worksheet.GetFirstChild<SheetData>();

                        var allRows = sheetData.Elements<Row>().ToList();
                        if (allRows.Count <= 1)
                        {
                            Application.OpenForms[0].Invoke((MethodInvoker)delegate
                            {
                                fileData.TotalAmount = 0;
                                fileData.TotalAmountFormatted = "0 원";
                            });
                            return new ValidationResult { IsValid = true };
                        }

                        // 헤더에서 금액 컬럼 인덱스 찾기
                        var headerRow = allRows.First();
                        var headerCells = headerRow.Elements<Cell>().ToList();
                        int amountColumnIndex = -1;

                        for (int i = 0; i < headerCells.Count; i++)
                        {
                            string cellValue = GetCellValue(headerCells[i], workbookPart);
                            if (cellValue.Trim().Equals(fileData.AmountColumnName.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                amountColumnIndex = i;
                                break;
                            }
                        }

                        if (amountColumnIndex == -1)
                        {
                            return new ValidationResult
                            {
                                IsValid = false,
                                ErrorMessage = $"금액 컬럼 '{fileData.AmountColumnName}'을 찾을 수 없습니다."
                            };
                        }

                        var dataRows = allRows.Skip(1).ToList();
                        var nonNumericValues = new ConcurrentBag<string>();
                        var processedCount = 0;
                        var totalRows = dataRows.Count;

                        Debug.WriteLine($"[병렬금액처리] 병렬 처리 시작 - 총 {totalRows:N0}행");

                        // PLINQ를 사용한 초고속 병렬 금액 계산
                        var totalAmount = dataRows
                            .AsParallel()
                            .WithDegreeOfParallelism(Environment.ProcessorCount * 4)
                            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                            .Select(row =>
                            {
                                // 진행률 업데이트 (10000개마다)
                                if (Interlocked.Increment(ref processedCount) % 10000 == 0)
                                {
                                    var progress = (processedCount * 100) / totalRows;
                                    Debug.WriteLine($"[병렬금액처리] 진행률: {progress}% ({processedCount:N0}/{totalRows:N0})");
                                }

                                var cells = row.Elements<Cell>().ToList();
                                if (amountColumnIndex < cells.Count)
                                {
                                    string cellValue = GetCellValue(cells[amountColumnIndex], workbookPart);
                                    if (!string.IsNullOrWhiteSpace(cellValue))
                                    {
                                        string cleanValue = cellValue.Replace(",", "").Trim();
                                        if (decimal.TryParse(cleanValue, out decimal amount))
                                        {
                                            return amount;
                                        }
                                        else
                                        {
                                            if (nonNumericValues.Count < 10)
                                            {
                                                nonNumericValues.Add(cellValue);
                                            }
                                        }
                                    }
                                }
                                return 0m;
                            })
                            .Sum(); // PLINQ의 Sum은 자동으로 병렬 처리됨

                        sw.Stop();
                        Debug.WriteLine($"[병렬금액처리] 완료 - 소요시간: {sw.ElapsedMilliseconds:N0}ms, 총액: {totalAmount:N0}");

                        // UI 스레드에서 결과 업데이트
                        Application.OpenForms[0].Invoke((MethodInvoker)delegate
                        {
                            if (nonNumericValues.Count == 0)
                            {
                                fileData.TotalAmount = totalAmount;
                                fileData.TotalAmountFormatted = totalAmount.ToString("N0") + " 원";
                            }
                        });

                        // 숫자가 아닌 값이 있는 경우 오류
                        if (nonNumericValues.Count > 0)
                        {
                            var nonNumericList = nonNumericValues.Take(10).ToList();
                            string errorMessage = "금액 컬럼은 숫자 값만 존재해야 합니다.\n\n" +
                                                "숫자가 아닌 값들:\n" +
                                                string.Join("\n", nonNumericList.Select((v, i) => $"{i + 1}. '{v}'"));

                            return new ValidationResult
                            {
                                IsValid = false,
                                ErrorMessage = errorMessage
                            };
                        }

                        return new ValidationResult { IsValid = true };
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[병렬금액처리] 오류 발생: {ex.Message}");
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"금액 컬럼 검증 중 오류가 발생했습니다: {ex.Message}"
                    };
                }
            });
        }



        /// <summary>
        /// 엑셀 파일 정보 추출 (컬럼명, 행 수)
        /// </summary>
        private async Task<(List<string> columns, int rows)> ExtractExcelInfo(string filePath)
        {
            return await Task.Run(() =>
            {
                var columns = new List<string>();
                int rows = 0;

                try
                {
                    using (var document = SpreadsheetDocument.Open(filePath, false))
                    {
                        var workbookPart = document.WorkbookPart;
                        var worksheet = workbookPart.WorksheetParts.First().Worksheet;
                        var sheetData = worksheet.GetFirstChild<SheetData>();

                        var allRows = sheetData.Elements<Row>().ToList();
                        //rows = allRows.Count;
                        int actualDataRows = 0;

                        foreach (var row in allRows)
                        {
                            bool hasData = false;
                            foreach (var cell in row.Elements<Cell>())
                            {
                                string cellValue = GetCellValue(cell, workbookPart);
                                if (!string.IsNullOrWhiteSpace(cellValue))
                                {
                                    hasData = true;
                                    break;
                                }
                            }
                            if (hasData) actualDataRows++;
                        }

                        rows = actualDataRows;

                        // 첫 번째 행에서 컬럼명 추출
                        if (allRows.Count > 0)
                        {
                            var headerRow = allRows.First();
                            foreach (var cell in headerRow.Elements<Cell>())
                            {
                                string columnName = GetCellValue(cell, workbookPart);
                                if (!string.IsNullOrWhiteSpace(columnName))
                                {
                                    columns.Add(columnName.Trim());
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"엑셀 정보 추출 오류: {ex.Message}");
                    //string exMsg = $"엑셀 정보 추출 오류: {ex.Message}";
                    columns.Add($"엑셀 정보 추출 오류: {ex.Message}");
                    return (columns, -1); // 헤더 제외한 데이터 행 수
                }

                return (columns, Math.Max(0, rows - 1)); // 헤더 제외한 데이터 행 수
            });
        }

        /// <summary>
        /// 엑셀 셀 값 읽기
        /// </summary>
        private string GetCellValue(Cell cell, WorkbookPart workbookPart)
        {
            if (cell?.CellValue == null) return string.Empty;

            string value = cell.CellValue.InnerXml;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                var stringTablePart = workbookPart.SharedStringTablePart;
                if (stringTablePart != null)
                {
                    value = stringTablePart.SharedStringTable.ElementAt(int.Parse(value)).InnerText;
                }
            }

            return value ?? string.Empty;
        }

       
        /// <summary>
        /// 파일 크기 포맷팅
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
