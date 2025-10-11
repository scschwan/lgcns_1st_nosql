using DocumentFormat.OpenXml.Wordprocessing;
using System.Data;

namespace FinanceTool
{
    public partial class Form1 : Form
    {
        public static DataTable excelData = new DataTable();
        private TrialManager trialManager;
        //사용기간 체크 flag
        private bool trialYN = false;

        //mac address 체크 flag
        private bool macCheckYN = false;
        //private bool trialYN = true;

        public Form1()
        {
            InitializeComponent();
        }


        /// <summary>
        /// ÄÁÆ®·Ñ Å©±â ¹× À§Ä¡¸¦ ÀçÁ¶Á¤ÇÏ´Â ¸Þ¼Òµå
        /// </summary>
        private void ResizeControls()
        {
            // ÄÁÆ®·Ñ Å©±â ¹× À§Ä¡ ÀçÁ¶Á¤ ·ÎÁ÷ ±¸Çö
            // °¢ UserControl¿¡ ´ëÇÑ ·¹ÀÌ¾Æ¿ô ¾÷µ¥ÀÌÆ®°¡ ÇÊ¿äÇÑ °æ¿ì Ã³¸®

            // ÇöÀç mainPanel¿¡ Ãß°¡µÈ ÄÁÆ®·ÑµéÀÇ ·¹ÀÌ¾Æ¿ôÀ» ¾÷µ¥ÀÌÆ®
            foreach (System.Windows.Forms.Control control in mainPanel.Controls)
            {
                if (control is UserControl)
                {
                    control.Dock = DockStyle.Fill;
                }
            }
        }

        private async void Form1_Load(object sender, EventArgs e)
        {

            // ¸ÞÀÎ È­¸é ÃÊ±âÈ­ ÀÛ¾÷
            //mainPanel.Controls.Add(userControlHandler.uc_fileLoad);
            mainPanel.Controls.Add(userControlHandler.uc_multiFileUpload);

            // ÆûÀ» ÀÏ½ÃÀûÀ¸·Î ºñÈ°¼ºÈ­ (ProcessProgressFormÀº º°µµ ÆûÀÌ¹Ç·Î ¿µÇâ ¾øÀ½)
            this.Enabled = false;

            try
            {
                //프로그램 권한 검증
                TrialManager trialManager = new TrialManager();

                //2025.07.22 mac address 검증 로직 추가
                if (macCheckYN)
                {
                    await trialManager.checkMacaddress();
                }
                

                if (trialYN)
                {
                    using (var progress = new ProcessProgressForm())
                    {
                        // ¸ð´Þ¸®½º Ã¢À¸·Î Ç¥½Ã
                        progress.Show(this);

                        // ÁøÇà »óÅÂ ¾÷µ¥ÀÌÆ®
                        await progress.UpdateProgressHandler(30, "ÇÁ·Î±×·¥ ÃÊ±âÈ­...");
                        await Task.Delay(10);

                    
                        await trialManager.CheckTrial();

                        // ÁøÇà ¿Ï·á ¹× Æû ´Ý±â
                        await progress.UpdateProgressHandler(100);
                        await Task.Delay(10);
                        progress.Close();
                    }
                }

                // Æû ´Ù½Ã È°¼ºÈ­
                this.Enabled = true;
            }
            catch (Exception ex)
            {
                // ¿À·ù ¹ß»ý ½Ã Ã³¸®
                MessageBox.Show($"ÃÊ±âÈ­ Áß ¿À·ù°¡ ¹ß»ýÇß½À´Ï´Ù: {ex.Message}", "¿À·ù",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {


                // ·¹ÀÌ¾Æ¿ô ÃÖÁ¾ Á¶Á¤
                ResizeControls();
            }


        }

        // ¸ðµç ÄÁÆ®·ÑÀÇ Enabled ¼Ó¼ºÀ» ¼³Á¤ÇÏ´Â Àç±Í ¸Þ¼Òµå


        public void LoadUserControl(UserControl control)
        {
            // ±âÁ¸ ÄÁÆ®·Ñ Á¦°Å
            mainPanel.Controls.Clear();

            // »õ ÄÁÆ®·Ñ Ãß°¡ ¹× ·¹ÀÌ¾Æ¿ô ¼³Á¤
            control.Dock = DockStyle.Fill;
            //control.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            mainPanel.Controls.Add(control);

            // ·¹ÀÌ¾Æ¿ô ¾÷µ¥ÀÌÆ®
            control.Invalidate();
            mainPanel.Invalidate();
        }

        private void fileLoadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadUserControl(userControlHandler.uc_fileLoad);
        }

        private void dataPreprocessingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadUserControl(userControlHandler.uc_Preprocessing);
        }

        private void dataAnalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadUserControl(userControlHandler.uc_dataTransform);
        }

        private void classificationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadUserControl(userControlHandler.uc_clustering);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadUserControl(userControlHandler.uc_classification);
        }


        private void fileUploadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadUserControl(userControlHandler.uc_multiFileUpload);
        }

        private void subClusteringToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadUserControl(userControlHandler.uc_detailClustering);
        }
    }

}
