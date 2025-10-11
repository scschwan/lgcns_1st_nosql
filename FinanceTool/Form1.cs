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

        // 현재 활성화된 메뉴 아이템을 추적하는 변수
        private ToolStripMenuItem _activeMenuItem = null;

        // 기본 메뉴 색상 (비활성)
        private readonly System.Drawing.Color DefaultMenuBackColor = System.Drawing.Color.Transparent;
        private readonly System.Drawing.Color DefaultMenuForeColor = System.Drawing.Color.Black;

        // 활성 메뉴 색상
        private readonly System.Drawing.Color ActiveMenuBackColor = System.Drawing.Color.LightBlue;  // 또는 원하는 색상
        private readonly System.Drawing.Color ActiveMenuForeColor = System.Drawing.Color.Navy;       // 또는 원하는 색상


        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 모든 메뉴 아이템을 기본 색상으로 초기화
        /// </summary>
        private void ResetAllMenuColors()
        {
            fileUploadToolStripMenuItem.BackColor = DefaultMenuBackColor;
            fileUploadToolStripMenuItem.ForeColor = DefaultMenuForeColor;

            fileLoadToolStripMenuItem.BackColor = DefaultMenuBackColor;
            fileLoadToolStripMenuItem.ForeColor = DefaultMenuForeColor;

            dataPreprocessingToolStripMenuItem.BackColor = DefaultMenuBackColor;
            dataPreprocessingToolStripMenuItem.ForeColor = DefaultMenuForeColor;

            dataAnalToolStripMenuItem.BackColor = DefaultMenuBackColor;
            dataAnalToolStripMenuItem.ForeColor = DefaultMenuForeColor;

            classificationToolStripMenuItem.BackColor = DefaultMenuBackColor;
            classificationToolStripMenuItem.ForeColor = DefaultMenuForeColor;

            exportToolStripMenuItem.BackColor = DefaultMenuBackColor;
            exportToolStripMenuItem.ForeColor = DefaultMenuForeColor;

            subClusteringToolStripMenuItem.BackColor = DefaultMenuBackColor;
            subClusteringToolStripMenuItem.ForeColor = DefaultMenuForeColor;
        }

        /// <summary>
        /// 특정 메뉴 아이템을 활성 상태로 표시
        /// </summary>
        /// <param name="menuItem">강조할 메뉴 아이템</param>
        private void SetActiveMenu(ToolStripMenuItem menuItem)
        {
            // 모든 메뉴를 기본 색상으로 초기화
            ResetAllMenuColors();

            // 선택된 메뉴만 활성 색상으로 변경
            if (menuItem != null)
            {
                menuItem.BackColor = ActiveMenuBackColor;
                menuItem.ForeColor = ActiveMenuForeColor;
                _activeMenuItem = menuItem;
            }
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
            SetActiveMenu(fileUploadToolStripMenuItem);

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

        /// <summary>
        /// UserControl 로드 및 해당 메뉴 강조
        /// </summary>
        /// <param name="control">로드할 UserControl</param>
        /// <param name="menuItem">강조할 메뉴 아이템 (선택사항)</param>
        public void LoadUserControl(UserControl control, ToolStripMenuItem menuItem = null)
        {
            // 기존 컨트롤 제거
            mainPanel.Controls.Clear();

            // 새 컨트롤 추가 및 레이아웃 설정
            control.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(control);

            // 레이아웃 업데이트
            control.Invalidate();
            mainPanel.Invalidate();

            // 메뉴 강조 표시
            if (menuItem != null)
            {
                SetActiveMenu(menuItem);
            }
        }

        private void fileLoadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadUserControl(userControlHandler.uc_fileLoad, fileLoadToolStripMenuItem);
        }

        private void dataPreprocessingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadUserControl(userControlHandler.uc_Preprocessing, dataPreprocessingToolStripMenuItem);
        }

        private void dataAnalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadUserControl(userControlHandler.uc_dataTransform, dataAnalToolStripMenuItem);
        }

        private void classificationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadUserControl(userControlHandler.uc_clustering, classificationToolStripMenuItem);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadUserControl(userControlHandler.uc_classification, exportToolStripMenuItem);
        }


        private void fileUploadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadUserControl(userControlHandler.uc_multiFileUpload, fileUploadToolStripMenuItem);
        }

        private void subClusteringToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadUserControl(userControlHandler.uc_detailClustering, subClusteringToolStripMenuItem);
        }
    }

}
