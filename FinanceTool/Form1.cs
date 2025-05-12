using DocumentFormat.OpenXml.Wordprocessing;
using System.Data;

namespace FinanceTool
{
    public partial class Form1 : Form
    {
        public static DataTable excelData = new DataTable();
        private TrialManager trialManager;
        private bool trialYN = false;
        //private bool trialYN = true;

        public Form1()
        {
            InitializeComponent();
        }

        private void SetFormLayout()
        {
            // DPI ���� �̺�Ʈ ó�� - �ػ󵵰� ����� �� ȣ���
            this.DpiChanged += (sender, e) => {
                // DPI ���� �� ��Ʈ�� ũ�� �� ��ġ ������
                this.SuspendLayout();
                ResizeControls();
                this.ResumeLayout();
            };

            // �� �������� �̺�Ʈ ó��
            this.ResizeEnd += (sender, e) => {
                ResizeControls();
            };
        }

        /// <summary>
        /// ��Ʈ�� ũ�� �� ��ġ�� �������ϴ� �޼ҵ�
        /// </summary>
        private void ResizeControls()
        {
            // ��Ʈ�� ũ�� �� ��ġ ������ ���� ����
            // �� UserControl�� ���� ���̾ƿ� ������Ʈ�� �ʿ��� ��� ó��

            // ���� mainPanel�� �߰��� ��Ʈ�ѵ��� ���̾ƿ��� ������Ʈ
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

            // ���� ȭ�� �ʱ�ȭ �۾�
            mainPanel.Controls.Add(userControlHandler.uc_fileLoad);

            // ���� �Ͻ������� ��Ȱ��ȭ (ProcessProgressForm�� ���� ���̹Ƿ� ���� ����)
            this.Enabled = false;

            try
            {
                if (trialYN)
                {
                    using (var progress = new ProcessProgressForm())
                    {
                        // ��޸��� â���� ǥ��
                        progress.Show(this);

                        // ���� ���� ������Ʈ
                        await progress.UpdateProgressHandler(30, "���α׷� �ʱ�ȭ...");
                        await Task.Delay(10);

                        // ���� ����Ȯ��
                        TrialManager trialManager = new TrialManager();
                        await trialManager.CheckTrial();

                        // ���� �Ϸ� �� �� �ݱ�
                        await progress.UpdateProgressHandler(100);
                        await Task.Delay(10);
                        progress.Close();
                    }
                }

                // �� �ٽ� Ȱ��ȭ
                this.Enabled = true;
            }
            catch (Exception ex)
            {
                // ���� �߻� �� ó��
                MessageBox.Show($"�ʱ�ȭ �� ������ �߻��߽��ϴ�: {ex.Message}", "����",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
               

                // ���̾ƿ� ���� ����
                ResizeControls();
            }


        }

        // ��� ��Ʈ���� Enabled �Ӽ��� �����ϴ� ��� �޼ҵ�


        public void LoadUserControl(UserControl control)
        {
            // ���� ��Ʈ�� ����
            mainPanel.Controls.Clear();

            // �� ��Ʈ�� �߰� �� ���̾ƿ� ����
            control.Dock = DockStyle.Fill;
            control.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            mainPanel.Controls.Add(control);

            // ���̾ƿ� ������Ʈ
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
            // ��� ó���� ������ ���� �����ͺ��̽� ����
            dbmanager.Instance.ManualDispose();
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadUserControl(userControlHandler.uc_classification);
        }
    }
   
}
