using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FinanceTool
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // MongoDB 연결 초기화
                // Data.MongoDBManager가 싱글톤 패턴이므로 Instance 속성에 접근하면 자동으로 초기화됨
                Data.MongoDBManager mongoManager = Data.MongoDBManager.Instance;

                // 필요시 데이터베이스 리셋 옵션 설정
                // mongoManager.ResetDatabaseOnStartup = false; // 기본값은 false, true로 설정하면 초기화

                // MongoDB 인덱스 생성 (필요한 경우)
                Task.Run(async () =>
                {
                    try
                    {
                        // 인덱스 생성은 몽고DB 연결 후 비동기로 실행
                        await CreateMongoDBIndexesAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"MongoDB 인덱스 생성 중 오류: {ex.Message}");
                        // 인덱스 생성 실패는 애플리케이션 실행에 치명적이지 않으므로 계속 진행
                    }
                });

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // 애플리케이션 예외 처리 등록
                Application.ThreadException += Application_ThreadException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                // 메인 폼 표시
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                // 초기화 중 치명적 오류 발생 시 사용자에게 알림
                MessageBox.Show($"애플리케이션 초기화 중 오류가 발생했습니다:\n\n{ex.Message}\n\n애플리케이션을 종료합니다.",
                               "치명적 오류",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
            }
            // Cleanup 부분 주석 처리 또는 제거 - MongoDBManager에 구현되지 않은 경우
            
            finally
            {
                try
                {
                    // 애플리케이션 종료 시 리소스 정리
                    Data.MongoDBManager.Instance.Cleanup();
                }
                catch (Exception ex)
                {
                    // 리소스 정리 중 오류는 로그만 남기고 무시
                    Debug.WriteLine($"MongoDB 연결 정리 중 오류: {ex.Message}");
                }
            }
            
        }

        // MongoDB 인덱스 생성 메서드
        private static async Task CreateMongoDBIndexesAsync()
        {
            var mongoManager = Data.MongoDBManager.Instance;

            // raw_data 컬렉션에 인덱스 생성
            var rawDataCollection = mongoManager.GetCollection<MongoModels.RawDataDocument>("raw_data");
            await rawDataCollection.Indexes.CreateOneAsync(
                MongoDB.Driver.Builders<MongoModels.RawDataDocument>.IndexKeys.Ascending(d => d.ImportDate));
            await rawDataCollection.Indexes.CreateOneAsync(
                MongoDB.Driver.Builders<MongoModels.RawDataDocument>.IndexKeys.Ascending(d => d.IsHidden));

            // process_data 컬렉션에 인덱스 생성
            var processDataCollection = mongoManager.GetCollection<MongoModels.ProcessDataDocument>("process_data");
            await processDataCollection.Indexes.CreateOneAsync(
                MongoDB.Driver.Builders<MongoModels.ProcessDataDocument>.IndexKeys.Ascending(d => d.RawDataId));

            // clustering_results 컬렉션에 인덱스 생성
            var clusteringCollection = mongoManager.GetCollection<MongoModels.ClusteringResultDocument>("clustering_results");
            await clusteringCollection.Indexes.CreateOneAsync(
                MongoDB.Driver.Builders<MongoModels.ClusteringResultDocument>.IndexKeys.Ascending(d => d.ClusterId));

            // 전체 텍스트 검색 인덱스 생성 (MongoDB 4.0 이상에서 지원)
            await rawDataCollection.Indexes.CreateOneAsync(
                MongoDB.Driver.Builders<MongoModels.RawDataDocument>.IndexKeys.Text("$**"));

            Debug.WriteLine("MongoDB 인덱스가 성공적으로 생성되었습니다.");
        }

        // UI 스레드 예외 처리
        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            HandleUnhandledException(e.Exception);
        }

        // 비 UI 스레드 예외 처리
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleUnhandledException(e.ExceptionObject as Exception);
        }

        // 공통 예외 처리 로직
        private static void HandleUnhandledException(Exception ex)
        {
            try
            {
                // 예외 정보 로깅 (파일 또는 데이터베이스에 저장할 수 있음)
                string errorMessage = $"오류 발생 시간: {DateTime.Now}\n오류 메시지: {ex.Message}\n스택 트레이스: {ex.StackTrace}";
                Debug.WriteLine(errorMessage);

                // 로그 파일에 저장 (선택사항)
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FinanceTool", "error_log.txt");

                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                File.AppendAllText(logPath, errorMessage + "\n\n");

                // 사용자에게 알림
                MessageBox.Show($"애플리케이션에서 오류가 발생했습니다.\n\n{ex.Message}\n\n자세한 정보는 오류 로그를 확인하세요: {logPath}",
                               "애플리케이션 오류",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
            }
            catch
            {
                // 예외 처리 중 또 다른 예외가 발생하면 기본 메시지만 표시
                MessageBox.Show("애플리케이션에서 처리되지 않은 오류가 발생했습니다.",
                               "치명적 오류",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
            }
        }
    }
}