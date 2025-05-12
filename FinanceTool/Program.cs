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
        /// �ش� ���ø����̼��� �� �������Դϴ�.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // MongoDB ���� �ʱ�ȭ
                // Data.MongoDBManager�� �̱��� �����̹Ƿ� Instance �Ӽ��� �����ϸ� �ڵ����� �ʱ�ȭ��
                Data.MongoDBManager mongoManager = Data.MongoDBManager.Instance;

                // �ʿ�� �����ͺ��̽� ���� �ɼ� ����
                // mongoManager.ResetDatabaseOnStartup = false; // �⺻���� false, true�� �����ϸ� �ʱ�ȭ

                // MongoDB �ε��� ���� (�ʿ��� ���)
                Task.Run(async () =>
                {
                    try
                    {
                        // �ε��� ������ ����DB ���� �� �񵿱�� ����
                        await CreateMongoDBIndexesAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"MongoDB �ε��� ���� �� ����: {ex.Message}");
                        // �ε��� ���� ���д� ���ø����̼� ���࿡ ġ�������� �����Ƿ� ��� ����
                    }
                });

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // ���ø����̼� ���� ó�� ���
                Application.ThreadException += Application_ThreadException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                // ���� �� ǥ��
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                // �ʱ�ȭ �� ġ���� ���� �߻� �� ����ڿ��� �˸�
                MessageBox.Show($"���ø����̼� �ʱ�ȭ �� ������ �߻��߽��ϴ�:\n\n{ex.Message}\n\n���ø����̼��� �����մϴ�.",
                               "ġ���� ����",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
            }
            // Cleanup �κ� �ּ� ó�� �Ǵ� ���� - MongoDBManager�� �������� ���� ���
            
            finally
            {
                try
                {
                    // ���ø����̼� ���� �� ���ҽ� ����
                    Data.MongoDBManager.Instance.Cleanup();
                }
                catch (Exception ex)
                {
                    // ���ҽ� ���� �� ������ �α׸� ����� ����
                    Debug.WriteLine($"MongoDB ���� ���� �� ����: {ex.Message}");
                }
            }
            
        }

        // MongoDB �ε��� ���� �޼���
        private static async Task CreateMongoDBIndexesAsync()
        {
            var mongoManager = Data.MongoDBManager.Instance;

            // raw_data �÷��ǿ� �ε��� ����
            var rawDataCollection = mongoManager.GetCollection<MongoModels.RawDataDocument>("raw_data");
            await rawDataCollection.Indexes.CreateOneAsync(
                MongoDB.Driver.Builders<MongoModels.RawDataDocument>.IndexKeys.Ascending(d => d.ImportDate));
            await rawDataCollection.Indexes.CreateOneAsync(
                MongoDB.Driver.Builders<MongoModels.RawDataDocument>.IndexKeys.Ascending(d => d.IsHidden));

            // process_data �÷��ǿ� �ε��� ����
            var processDataCollection = mongoManager.GetCollection<MongoModels.ProcessDataDocument>("process_data");
            await processDataCollection.Indexes.CreateOneAsync(
                MongoDB.Driver.Builders<MongoModels.ProcessDataDocument>.IndexKeys.Ascending(d => d.RawDataId));

            // clustering_results �÷��ǿ� �ε��� ����
            var clusteringCollection = mongoManager.GetCollection<MongoModels.ClusteringResultDocument>("clustering_results");
            await clusteringCollection.Indexes.CreateOneAsync(
                MongoDB.Driver.Builders<MongoModels.ClusteringResultDocument>.IndexKeys.Ascending(d => d.ClusterId));

            // ��ü �ؽ�Ʈ �˻� �ε��� ���� (MongoDB 4.0 �̻󿡼� ����)
            await rawDataCollection.Indexes.CreateOneAsync(
                MongoDB.Driver.Builders<MongoModels.RawDataDocument>.IndexKeys.Text("$**"));

            Debug.WriteLine("MongoDB �ε����� ���������� �����Ǿ����ϴ�.");
        }

        // UI ������ ���� ó��
        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            HandleUnhandledException(e.Exception);
        }

        // �� UI ������ ���� ó��
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleUnhandledException(e.ExceptionObject as Exception);
        }

        // ���� ���� ó�� ����
        private static void HandleUnhandledException(Exception ex)
        {
            try
            {
                // ���� ���� �α� (���� �Ǵ� �����ͺ��̽��� ������ �� ����)
                string errorMessage = $"���� �߻� �ð�: {DateTime.Now}\n���� �޽���: {ex.Message}\n���� Ʈ���̽�: {ex.StackTrace}";
                Debug.WriteLine(errorMessage);

                // �α� ���Ͽ� ���� (���û���)
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FinanceTool", "error_log.txt");

                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                File.AppendAllText(logPath, errorMessage + "\n\n");

                // ����ڿ��� �˸�
                MessageBox.Show($"���ø����̼ǿ��� ������ �߻��߽��ϴ�.\n\n{ex.Message}\n\n�ڼ��� ������ ���� �α׸� Ȯ���ϼ���: {logPath}",
                               "���ø����̼� ����",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
            }
            catch
            {
                // ���� ó�� �� �� �ٸ� ���ܰ� �߻��ϸ� �⺻ �޽����� ǥ��
                MessageBox.Show("���ø����̼ǿ��� ó������ ���� ������ �߻��߽��ϴ�.",
                               "ġ���� ����",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
            }
        }
    }
}