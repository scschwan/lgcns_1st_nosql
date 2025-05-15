using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace FinanceTool
{
    

    public class KeywordExtractor
    {
       
        private readonly string _pythonPath;
        private readonly string _scriptPath;

        private int call_type;

        //형태소 분석기 호출
        //0 : .py script 파일 , 1 : exe 파일
        public KeywordExtractor(int type)
        {

            call_type = type;
            if (type == 0)
            {
                _pythonPath = "python.exe";
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                //_scriptPath = Path.GetFullPath(Path.Combine(baseDir, "keyword_extractor.py"));
                //_scriptPath = Path.GetFullPath(Path.Combine(baseDir, "komoran_keyword_extractor.py"));
                _scriptPath = Path.GetFullPath(Path.Combine(baseDir, "okt_keyword_extractor.py"));


                Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
                Console.WriteLine($"Base Directory: {baseDir}");
                Console.WriteLine($"Script Path: {_scriptPath}");

                if (!File.Exists(_scriptPath))
                {
                    throw new FileNotFoundException($"Python script not found at: {_scriptPath}");
                }
            }
            else
            {
                // exe 파일만 사용하므로 python 경로는 제거
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                _scriptPath = Path.GetFullPath(Path.Combine(baseDir, "okt_keyword_extractor.exe"));

                Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
                Console.WriteLine($"Base Directory: {baseDir}");
                Console.WriteLine($"Executable Path: {_scriptPath}");

                if (!File.Exists(_scriptPath))
                {
                    throw new FileNotFoundException($"Executable not found at: {_scriptPath}");
                }
            }
            
        }

        //private SemaphoreSlim _batchSemaphore = new SemaphoreSlim(1, 1);
        private SemaphoreSlim _batchSemaphore = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount * 4),
    Math.Max(1, Environment.ProcessorCount * 4));



        public async Task<DataTable> ExtractKeywordsFromDataTable(DataTable table,  int columnIndex, int limit, int batchSize = 100, IProgress<int> progress = null)
        {
            var allProcessedData = new List<ProcessedTextData>();
            var batches = GetBatches_by_textData(table, columnIndex, batchSize).ToList();
            var totalBatches = batches.Count;

            Debug.WriteLine($"배치 처리 시작: 총 {table.Rows.Count}개 데이터, batchSize : {batchSize}개 배치");

            Debug.WriteLine($"배치 처리 시작: 총 {table.Rows.Count}개 데이터, {totalBatches}개 배치");

            for (int i = 0; i < batches.Count; i++)
            {
                await _batchSemaphore.WaitAsync();
                try
                {
                    var batch = batches[i];
                    //Debug.WriteLine($"배치 {i} 처리 시작");

                    var processedBatch = call_type == 0
                        ? await ProcessBatch_by_python_code(batch, limit)
                        : await ProcessBatch(batch, limit);

                    allProcessedData.AddRange(processedBatch);
                    
                    // 처리 로직
                    var percentage = (i + 1) * 100 / totalBatches;
                    progress?.Report(percentage);
                }
                finally
                {
                    _batchSemaphore.Release();
                    //Debug.WriteLine($"배치 {i} 처리 완료");
                }
            }

            Debug.WriteLine($"전체 처리 완료: {allProcessedData.Count}개 결과");

            if (allProcessedData.Count > 0)
            {
                return ConvertToDataTable(allProcessedData);
            }else
            {
                Debug.WriteLine("처리 결과 없음!!!!!");
                return table;
            }
            
        }

        
        

        private IEnumerable<List<TextData>> GetBatches_by_textData(DataTable table, int columnIndex, int batchSize)
        {
            var currentBatch = new List<TextData>();
            var currentRowCount = 0;
            int lastRow = -1;

            for (int row = 0; row < table.Rows.Count; row++)
            {
                if (lastRow != row)
                {
                    currentRowCount++;
                    lastRow = row;
                }

                for (int col = 0; col < table.Columns.Count; col++)
                {
                    if (table.Rows[row][col] != DBNull.Value)
                    {
                        string processedValue = table.Rows[row][col].ToString();
                        if (!string.IsNullOrWhiteSpace(processedValue))
                        {
                            currentBatch.Add(new TextData { Row = row, Col = col, Text = processedValue });
                        }
                    }
                }

                if (currentRowCount >= batchSize)
                {
                    yield return currentBatch;
                    currentBatch = new List<TextData>();
                    currentRowCount = 0;
                }
            }

            if (currentBatch.Count > 0)
            {
                yield return currentBatch;
            }
        }

        
        //25.01.23
        //신규 함수
        private async Task<List<ProcessedTextData>> ProcessBatch(List<TextData> texts, int limit)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _scriptPath,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(_scriptPath),
                StandardInputEncoding = new UTF8Encoding(false),
                StandardOutputEncoding = new UTF8Encoding(false)
            };

            try
            {
                using var process = new Process { StartInfo = startInfo };
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        outputBuilder.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        errorBuilder.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var input = new PythonInput
                {
                    Texts = texts,
                    Limit = limit
                };

                string inputJson = JsonSerializer.Serialize(input, new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                Debug.WriteLine($"전송할 JSON 데이터: {inputJson}");

                await process.StandardInput.WriteLineAsync(inputJson);
                await process.StandardInput.FlushAsync();
                process.StandardInput.Close();


                if (await Task.WhenAny(Task.Run(() => process.WaitForExit()), Task.Delay(30000)) == Task.Delay(30000))
                {
                    process.Kill();
                    throw new Exception("Python 프로세스 타임아웃");
                }


                // 에러 확인
                string errors = errorBuilder.ToString();
                if (!string.IsNullOrEmpty(errors))
                {
                    Debug.WriteLine($"Python 에러 발생: {errors}");
                    //throw new Exception($"Python 스크립트 에러: {errors}");
                }

                await process.WaitForExitAsync();

                string output = outputBuilder.ToString().Replace("\n", "").Replace("\r", "").Trim();
                if (output[0] == '\uFEFF') output = output.Substring(1);

                if (string.IsNullOrEmpty(output))
                    return new List<ProcessedTextData>();

                try
                {
                    byte[] data = Convert.FromBase64String(output);
                    string decodedJson = Encoding.UTF8.GetString(data);

                    Debug.WriteLine($"디코딩된 JSON: {decodedJson}");

                    return JsonSerializer.Deserialize<List<ProcessedTextData>>(decodedJson);
                }
                catch (FormatException ex)
                {
                    //Debug.WriteLine($"Base64 디코딩 오류: {ex.Message}");
                    //Debug.WriteLine($"잘못된 Base64 문자열: {output}");
                    // Base64 문자열 상세 정보 출력
                    Debug.WriteLine($"Base64 디코딩 오류: {ex.Message}");
                    Debug.WriteLine($"문자열 길이: {output.Length}");
                    Debug.WriteLine($"처음 100자: [{output.Substring(0, Math.Min(100, output.Length))}]");
                    Debug.WriteLine($"마지막 100자: [{output.Substring(Math.Max(0, output.Length - 100))}]");

                    return new List<ProcessedTextData>();
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine($"JSON 파싱 오류: {ex.Message}");
                    Debug.WriteLine($"파싱 시도한 데이터: {output}");
                    return new List<ProcessedTextData>();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"데이터 처리 오류: {ex.Message}");
                    return new List<ProcessedTextData>();
                }
               
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                return new List<ProcessedTextData>();
            }
        }

        private async Task<List<ProcessedTextData>> ProcessBatch_by_python_code(List<TextData> texts, int limit)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = $"\"{_scriptPath}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(_scriptPath),
                StandardInputEncoding = new UTF8Encoding(false),
                StandardOutputEncoding = new UTF8Encoding(false)
            };

            try
            {
                using var process = new Process { StartInfo = startInfo };
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        outputBuilder.AppendLine(e.Data);
                        Debug.WriteLine($"Python 출력: {e.Data}");
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data) && !e.Data.Contains("UserWarning"))
                    {
                        errorBuilder.AppendLine(e.Data);
                        Debug.WriteLine($"Python 에러: {e.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var input = new PythonInput
                {
                    Texts = texts,
                    Limit = limit
                };

                string inputJson = JsonSerializer.Serialize(input, new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                Debug.WriteLine($"전송할 JSON 데이터: {inputJson}");

                await process.StandardInput.WriteLineAsync(inputJson);
                await process.StandardInput.FlushAsync();
                process.StandardInput.Close();

                if (await Task.WhenAny(Task.Run(() => process.WaitForExit()), Task.Delay(30000)) == Task.Delay(30000))
                {
                    process.Kill();
                    throw new Exception("Python 프로세스 타임아웃");
                }


                // 에러 확인
                string errors = errorBuilder.ToString();
                if (!string.IsNullOrEmpty(errors))
                {
                    Debug.WriteLine($"Python 에러 발생: {errors}");
                    //throw new Exception($"Python 스크립트 에러: {errors}");
                }

                string output = outputBuilder.ToString()
                    .Replace("\n", "")
                    .Replace("\r", "")
                    .Replace(" ", "")
                    .Trim();

                if (output.Length > 0 && output[0] == '\uFEFF')
                    output = output.Substring(1);

                if (string.IsNullOrEmpty(output))
                    return new List<ProcessedTextData>();

                try
                {
                    byte[] data = Convert.FromBase64String(output);
                    string decodedJson = Encoding.UTF8.GetString(data);

                    Debug.WriteLine($"디코딩된 JSON: {decodedJson}");

                    return JsonSerializer.Deserialize<List<ProcessedTextData>>(decodedJson);
                }
                catch (FormatException ex)
                {
                    //Debug.WriteLine($"Base64 디코딩 오류: {ex.Message}");
                    //Debug.WriteLine($"잘못된 Base64 문자열: {output}");
                    // Base64 문자열 상세 정보 출력
                    Debug.WriteLine($"Base64 디코딩 오류: {ex.Message}");
                    Debug.WriteLine($"문자열 길이: {output.Length}");
                    Debug.WriteLine($"처음 100자: [{output.Substring(0, Math.Min(100, output.Length))}]");
                    Debug.WriteLine($"마지막 100자: [{output.Substring(Math.Max(0, output.Length - 100))}]");

                    return new List<ProcessedTextData>(); 
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine($"JSON 파싱 오류: {ex.Message}");
                    Debug.WriteLine($"파싱 시도한 데이터: {output}");
                    return new List<ProcessedTextData>();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"데이터 처리 오류: {ex.Message}");
                    return new List<ProcessedTextData>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"처리 중 오류 발생: {ex.Message}");
                return new List<ProcessedTextData>();
            }
        }


        private DataTable ConvertToDataTable(List<ProcessedTextData> processedData)
        {
            var maxRow = processedData.Max(d => d.Row);
            var maxCol = processedData.Max(d => d.Col);

            var dt = new DataTable();
            for (int i = 0; i <= maxCol; i++)
                dt.Columns.Add($"Col{i}", typeof(string));

            for (int i = 0; i <= maxRow; i++)
                dt.Rows.Add(dt.NewRow());

            foreach (var data in processedData)
                dt.Rows[data.Row][data.Col] = data.Text;

            return dt;
        }

        // 응답 클래스 정의 (기존과 동일)
        public class PythonResponse
        {
            public List<KeywordResult> Results { get; set; } = new List<KeywordResult>();
        }

        public class KeywordResult
        {
            public List<string> Keywords { get; set; } = new List<string>();
        }

        public class ProcessedTextData
        {
            public int Row { get; set; }
            public int Col { get; set; }
            public string Text { get; set; }
        }

        public class TextData
        {
            public int Row { get; set; }
            public int Col { get; set; }
            public string Text { get; set; }
        }

        public class PythonInput
        {
            public List<TextData> Texts { get; set; }
            public int Limit { get; set; }
        }

        
    }

    
}
