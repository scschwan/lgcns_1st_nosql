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
        /// ÇØ´ç ¾ÖÇÃ¸®ÄÉÀÌ¼ÇÀÇ ÁÖ ÁøÀÔÁ¡ÀÔ´Ï´Ù.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {

                

                Process currentProcess = Process.GetCurrentProcess();
                currentProcess.PriorityClass = ProcessPriorityClass.High; // 또는 ProcessPriorityClass.High

                // MongoDB ¿¬°á ÃÊ±âÈ­
                // Data.MongoDBManager°¡ ½Ì±ÛÅæ ÆÐÅÏÀÌ¹Ç·Î Instance ¼Ó¼º¿¡ Á¢±ÙÇÏ¸é ÀÚµ¿À¸·Î ÃÊ±âÈ­µÊ
                Data.MongoDBManager mongoManager = Data.MongoDBManager.Instance;

                // ÇÊ¿ä½Ã µ¥ÀÌÅÍº£ÀÌ½º ¸®¼Â ¿É¼Ç ¼³Á¤
                // mongoManager.ResetDatabaseOnStartup = false; // ±âº»°ªÀº false, true·Î ¼³Á¤ÇÏ¸é ÃÊ±âÈ­

                // MongoDB ÀÎµ¦½º »ý¼º (ÇÊ¿äÇÑ °æ¿ì)
                Task.Run(async () =>
                {
                    try
                    {
                        // ÀÎµ¦½º »ý¼ºÀº ¸ù°íDB ¿¬°á ÈÄ ºñµ¿±â·Î ½ÇÇà
                        await CreateMongoDBIndexesAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"MongoDB ÀÎµ¦½º »ý¼º Áß ¿À·ù: {ex.Message}");
                        // ÀÎµ¦½º »ý¼º ½ÇÆÐ´Â ¾ÖÇÃ¸®ÄÉÀÌ¼Ç ½ÇÇà¿¡ Ä¡¸íÀûÀÌÁö ¾ÊÀ¸¹Ç·Î °è¼Ó ÁøÇà
                    }
                });

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // ¾ÖÇÃ¸®ÄÉÀÌ¼Ç ¿¹¿Ü Ã³¸® µî·Ï
                Application.ThreadException += Application_ThreadException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                // ¸ÞÀÎ Æû Ç¥½Ã
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                // ÃÊ±âÈ­ Áß Ä¡¸íÀû ¿À·ù ¹ß»ý ½Ã »ç¿ëÀÚ¿¡°Ô ¾Ë¸²
                MessageBox.Show($"¾ÖÇÃ¸®ÄÉÀÌ¼Ç ÃÊ±âÈ­ Áß ¿À·ù°¡ ¹ß»ýÇß½À´Ï´Ù:\n\n{ex.Message}\n\n¾ÖÇÃ¸®ÄÉÀÌ¼ÇÀ» Á¾·áÇÕ´Ï´Ù.",
                               "Ä¡¸íÀû ¿À·ù",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
            }
            // Cleanup ºÎºÐ ÁÖ¼® Ã³¸® ¶Ç´Â Á¦°Å - MongoDBManager¿¡ ±¸ÇöµÇÁö ¾ÊÀº °æ¿ì

            finally
            {
                try
                {
                    // ¾ÖÇÃ¸®ÄÉÀÌ¼Ç Á¾·á ½Ã ¸®¼Ò½º Á¤¸®
                    Data.MongoDBManager.Instance.Cleanup();
                }
                catch (Exception ex)
                {
                    // ¸®¼Ò½º Á¤¸® Áß ¿À·ù´Â ·Î±×¸¸ ³²±â°í ¹«½Ã
                    Debug.WriteLine($"MongoDB ¿¬°á Á¤¸® Áß ¿À·ù: {ex.Message}");
                }
            }

        }

        // MongoDB ÀÎµ¦½º »ý¼º ¸Þ¼­µå
        private static async Task CreateMongoDBIndexesAsync()
        {
            var mongoManager = Data.MongoDBManager.Instance;

            // raw_data ÄÃ·º¼Ç¿¡ ÀÎµ¦½º »ý¼º
            var rawDataCollection = await mongoManager.GetCollectionAsync<MongoModels.RawDataDocument>("raw_data");
            await rawDataCollection.Indexes.CreateOneAsync(
                MongoDB.Driver.Builders<MongoModels.RawDataDocument>.IndexKeys.Ascending(d => d.ImportDate));
            await rawDataCollection.Indexes.CreateOneAsync(
                MongoDB.Driver.Builders<MongoModels.RawDataDocument>.IndexKeys.Ascending(d => d.IsHidden));

            // process_data ÄÃ·º¼Ç¿¡ ÀÎµ¦½º »ý¼º
            var processDataCollection = await mongoManager.GetCollectionAsync<MongoModels.ProcessDataDocument>("process_data");
            await processDataCollection.Indexes.CreateOneAsync(
                MongoDB.Driver.Builders<MongoModels.ProcessDataDocument>.IndexKeys.Ascending(d => d.RawDataId));

            // clustering_results ÄÃ·º¼Ç¿¡ ÀÎµ¦½º »ý¼º
            var clusteringCollection = await mongoManager.GetCollectionAsync<MongoModels.ClusteringResultDocument>("clustering_results");
            await clusteringCollection.Indexes.CreateOneAsync(
                MongoDB.Driver.Builders<MongoModels.ClusteringResultDocument>.IndexKeys.Ascending(d => d.ClusterId));

            // ÀüÃ¼ ÅØ½ºÆ® °Ë»ö ÀÎµ¦½º »ý¼º (MongoDB 4.0 ÀÌ»ó¿¡¼­ Áö¿ø)
            await rawDataCollection.Indexes.CreateOneAsync(
                MongoDB.Driver.Builders<MongoModels.RawDataDocument>.IndexKeys.Text("$**"));

            Debug.WriteLine("MongoDB ÀÎµ¦½º°¡ ¼º°øÀûÀ¸·Î »ý¼ºµÇ¾ú½À´Ï´Ù.");
        }

        // UI ½º·¹µå ¿¹¿Ü Ã³¸®
        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            HandleUnhandledException(e.Exception);
        }

        // ºñ UI ½º·¹µå ¿¹¿Ü Ã³¸®
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleUnhandledException(e.ExceptionObject as Exception);
        }

        // °øÅë ¿¹¿Ü Ã³¸® ·ÎÁ÷
        private static void HandleUnhandledException(Exception ex)
        {
            try
            {
                // ¿¹¿Ü Á¤º¸ ·Î±ë (ÆÄÀÏ ¶Ç´Â µ¥ÀÌÅÍº£ÀÌ½º¿¡ ÀúÀåÇÒ ¼ö ÀÖÀ½)
                string errorMessage = $"¿À·ù ¹ß»ý ½Ã°£: {DateTime.Now}\n¿À·ù ¸Þ½ÃÁö: {ex.Message}\n½ºÅÃ Æ®·¹ÀÌ½º: {ex.StackTrace}";
                Debug.WriteLine(errorMessage);

                // ·Î±× ÆÄÀÏ¿¡ ÀúÀå (¼±ÅÃ»çÇ×)
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FinanceTool", "error_log.txt");

                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                File.AppendAllText(logPath, errorMessage + "\n\n");

                // »ç¿ëÀÚ¿¡°Ô ¾Ë¸²
                MessageBox.Show($"¾ÖÇÃ¸®ÄÉÀÌ¼Ç¿¡¼­ ¿À·ù°¡ ¹ß»ýÇß½À´Ï´Ù.\n\n{ex.Message}\n\nÀÚ¼¼ÇÑ Á¤º¸´Â ¿À·ù ·Î±×¸¦ È®ÀÎÇÏ¼¼¿ä: {logPath}",
                               "¾ÖÇÃ¸®ÄÉÀÌ¼Ç ¿À·ù",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
            }
            catch
            {
                // ¿¹¿Ü Ã³¸® Áß ¶Ç ´Ù¸¥ ¿¹¿Ü°¡ ¹ß»ýÇÏ¸é ±âº» ¸Þ½ÃÁö¸¸ Ç¥½Ã
                MessageBox.Show("¾ÖÇÃ¸®ÄÉÀÌ¼Ç¿¡¼­ Ã³¸®µÇÁö ¾ÊÀº ¿À·ù°¡ ¹ß»ýÇß½À´Ï´Ù.",
                               "Ä¡¸íÀû ¿À·ù",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
            }
        }
    }
}