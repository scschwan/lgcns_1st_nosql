namespace FinanceTool
{
    partial class uc_clusteringPopup
    {
        /// <summary> 
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 구성 요소 디자이너에서 생성한 코드

        /// <summary> 
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            // 메인 컨테이너
            this.tableLayoutMain = new TableLayoutPanel();
            this.pnl_left = new Panel();
            this.pnl_right = new Panel();

            // 헤더 패널들
            this.pnl_left_header = new Panel();
            this.pnl_right_header = new Panel();

            // 기존 컨트롤들
            this.dataGridView1 = new DataGridView();
            this.dataGridView2 = new DataGridView();
            this.label1 = new Label();
            this.label2 = new Label();

            // 컨트롤 초기화 시작
            this.tableLayoutMain.SuspendLayout();
            this.pnl_left.SuspendLayout();
            this.pnl_right.SuspendLayout();
            this.pnl_left_header.SuspendLayout();
            this.pnl_right_header.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).BeginInit();
            this.SuspendLayout();

            // 
            // tableLayoutMain
            // 
            this.tableLayoutMain.ColumnCount = 2;
            this.tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tableLayoutMain.Controls.Add(this.pnl_left, 0, 0);
            this.tableLayoutMain.Controls.Add(this.pnl_right, 1, 0);
            this.tableLayoutMain.Dock = DockStyle.Fill;
            this.tableLayoutMain.Location = new Point(0, 0);
            this.tableLayoutMain.Name = "tableLayoutMain";
            this.tableLayoutMain.RowCount = 1;
            this.tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.tableLayoutMain.Size = new Size(1904, 1017);
            this.tableLayoutMain.TabIndex = 0;

            // 
            // pnl_left
            // 
            this.pnl_left.Controls.Add(this.pnl_left_header);
            this.pnl_left.Controls.Add(this.dataGridView1);
            this.pnl_left.Dock = DockStyle.Fill;
            this.pnl_left.Location = new Point(10, 10);
            this.pnl_left.Margin = new Padding(10);
            this.pnl_left.Name = "pnl_left";
            this.pnl_left.Size = new Size(932, 997);
            this.pnl_left.TabIndex = 0;

            // 
            // pnl_right
            // 
            this.pnl_right.Controls.Add(this.pnl_right_header);
            this.pnl_right.Controls.Add(this.dataGridView2);
            this.pnl_right.Dock = DockStyle.Fill;
            this.pnl_right.Location = new Point(962, 10);
            this.pnl_right.Margin = new Padding(10);
            this.pnl_right.Name = "pnl_right";
            this.pnl_right.Size = new Size(932, 997);
            this.pnl_right.TabIndex = 1;

            // 
            // pnl_left_header
            // 
            this.pnl_left_header.BackColor = Color.SteelBlue;
            this.pnl_left_header.Controls.Add(this.label1);
            this.pnl_left_header.Dock = DockStyle.Top;
            this.pnl_left_header.Location = new Point(0, 0);
            this.pnl_left_header.Name = "pnl_left_header";
            this.pnl_left_header.Size = new Size(932, 80);
            this.pnl_left_header.TabIndex = 0;

            // 
            // pnl_right_header
            // 
            this.pnl_right_header.BackColor = Color.DarkOrange;
            this.pnl_right_header.Controls.Add(this.label2);
            this.pnl_right_header.Dock = DockStyle.Top;
            this.pnl_right_header.Location = new Point(0, 0);
            this.pnl_right_header.Name = "pnl_right_header";
            this.pnl_right_header.Size = new Size(932, 80);
            this.pnl_right_header.TabIndex = 1;

            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
            | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.dataGridView1.BackgroundColor = Color.White;
            this.dataGridView1.BorderStyle = BorderStyle.Fixed3D;
            this.dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.EnableHeadersVisualStyles = false;
            this.dataGridView1.GridColor = Color.LightGray;
            this.dataGridView1.Location = new Point(10, 90);
            this.dataGridView1.MinimumSize = new Size(400, 300);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.RowTemplate.Height = 25;
            this.dataGridView1.Size = new Size(912, 897);
            this.dataGridView1.TabIndex = 0;

            // 
            // dataGridView2
            // 
            this.dataGridView2.AllowUserToAddRows = false;
            this.dataGridView2.AllowUserToDeleteRows = false;
            this.dataGridView2.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
            | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.dataGridView2.BackgroundColor = Color.White;
            this.dataGridView2.BorderStyle = BorderStyle.Fixed3D;
            this.dataGridView2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView2.EnableHeadersVisualStyles = false;
            this.dataGridView2.GridColor = Color.LightGray;
            this.dataGridView2.Location = new Point(10, 90);
            this.dataGridView2.MinimumSize = new Size(400, 300);
            this.dataGridView2.Name = "dataGridView2";
            this.dataGridView2.ReadOnly = true;
            this.dataGridView2.RowHeadersVisible = false;
            this.dataGridView2.RowTemplate.Height = 25;
            this.dataGridView2.Size = new Size(912, 897);
            this.dataGridView2.TabIndex = 1;

            // 
            // label1
            // 
            this.label1.Anchor = ((AnchorStyles)((AnchorStyles.Left | AnchorStyles.Right)));
            this.label1.AutoSize = false;
            this.label1.Font = new Font("맑은 고딕", 28F, FontStyle.Bold, GraphicsUnit.Point, 129);
            this.label1.ForeColor = Color.White;
            this.label1.Location = new Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new Size(932, 80);
            this.label1.TabIndex = 2;
            this.label1.Text = "1st Clustering";
            this.label1.TextAlign = ContentAlignment.MiddleCenter;

            // 
            // label2
            // 
            this.label2.Anchor = ((AnchorStyles)((AnchorStyles.Left | AnchorStyles.Right)));
            this.label2.AutoSize = false;
            this.label2.Font = new Font("맑은 고딕", 28F, FontStyle.Bold, GraphicsUnit.Point, 129);
            this.label2.ForeColor = Color.White;
            this.label2.Location = new Point(0, 0);
            this.label2.Name = "label2";
            this.label2.Size = new Size(932, 80);
            this.label2.TabIndex = 3;
            this.label2.Text = "2nd Clustering";
            this.label2.TextAlign = ContentAlignment.MiddleCenter;

            // 
            // uc_clusteringPopup
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.White;
            this.Controls.Add(this.tableLayoutMain);
            this.MinimumSize = new Size(1200, 800);
            this.Name = "uc_clusteringPopup";

            // 컨트롤 정리
            this.tableLayoutMain.ResumeLayout(false);
            this.pnl_left.ResumeLayout(false);
            this.pnl_right.ResumeLayout(false);
            this.pnl_left_header.ResumeLayout(false);
            this.pnl_right_header.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        // 메인 레이아웃
        private TableLayoutPanel tableLayoutMain;
        private Panel pnl_left;
        private Panel pnl_right;
        private Panel pnl_left_header;
        private Panel pnl_right_header;

        // 기존 컨트롤들 (모든 컨트롤명 유지)
        private DataGridView dataGridView1;
        private DataGridView dataGridView2;
        private Label label1;
        private Label label2;
    }
}
