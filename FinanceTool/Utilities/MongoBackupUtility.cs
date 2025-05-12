// 백업 및 복원 유틸리티 클래스
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FinanceTool.Utilities
{
    public class MongoBackupUtility
    {
        // MongoDB 백업 도구 경로 (설치 경로에 맞게 조정 필요)
        private static string mongodumpPath = @"C:\Program Files\MongoDB\Tools\mongodump.exe";
        private static string mongorestorePath = @"C:\Program Files\MongoDB\Tools\mongorestore.exe";

        // 데이터베이스 연결 정보 (MongoDBManager와 공유하는 것이 좋음)
        private static string connectionString = "mongodb://localhost:27017";
        private static string databaseName = "finance_tool";

        public static async Task BackupAsync(string backupPath)
        {
            try
            {
                // 백업 경로가 없으면 생성
                Directory.CreateDirectory(backupPath);

                // mongodump 명령어 실행
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = mongodumpPath,
                    Arguments = $"--uri=\"{connectionString}\" --db={databaseName} --out=\"{backupPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                        throw new Exception($"백업 실패: {error}");

                    Debug.WriteLine($"백업 성공: {output}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"백업 중 오류: {ex.Message}");
                throw;
            }
        }

        public static async Task RestoreAsync(string backupPath)
        {
            try
            {
                // 백업 경로 확인
                if (!Directory.Exists(Path.Combine(backupPath, databaseName)))
                    throw new DirectoryNotFoundException($"백업 경로가 유효하지 않습니다: {backupPath}");

                // mongorestore 명령어 실행
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = mongorestorePath,
                    Arguments = $"--uri=\"{connectionString}\" --db={databaseName} \"{Path.Combine(backupPath, databaseName)}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                        throw new Exception($"복원 실패: {error}");

                    Debug.WriteLine($"복원 성공: {output}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"복원 중 오류: {ex.Message}");
                throw;
            }
        }
    }
}