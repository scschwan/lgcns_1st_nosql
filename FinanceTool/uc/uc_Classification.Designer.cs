namespace FinanceTool
{
    partial class uc_Classification
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
            tableLayoutMain = new TableLayoutPanel();
            pnl_left = new Panel();
            tableLayoutLeft = new TableLayoutPanel();
            pnl_original_section = new Panel();
            dataGridView_origin = new DataGridView();
            pnl_keyword_section = new Panel();
            dataGridView_keyword = new DataGridView();
            pnl_pagination_section = new Panel();
            button5 = new Button();
            btn_nextPage = new Button();
            btn_prevPage = new Button();
            cmb_pageSize = new ComboBox();
            lbl_pageSizeText = new Label();
            lbl_pagination = new Label();
            num_pageNumber = new NumericUpDown();
            lbl_pagination2 = new Label();
            pnl_right = new Panel();
            tableLayoutRight = new TableLayoutPanel();
            groupBox1 = new GroupBox();
            label3 = new Label();
            dataGridView_classify = new DataGridView();
            groupBox3 = new GroupBox();
            label7 = new Label();
            del_col_list_allcheck = new CheckBox();
            dataGridView_delete_col2 = new DataGridView();
            restore_col_btn = new Button();
            label10 = new Label();
            label4 = new Label();
            label1 = new Label();
            tableLayoutMain.SuspendLayout();
            pnl_left.SuspendLayout();
            tableLayoutLeft.SuspendLayout();
            pnl_original_section.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_origin).BeginInit();
            pnl_keyword_section.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_keyword).BeginInit();
            pnl_pagination_section.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)num_pageNumber).BeginInit();
            pnl_right.SuspendLayout();
            tableLayoutRight.SuspendLayout();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_classify).BeginInit();
            groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_delete_col2).BeginInit();
            SuspendLayout();
            // 
            // tableLayoutMain
            // 
            tableLayoutMain.ColumnCount = 2;
            tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 500F));
            tableLayoutMain.Controls.Add(pnl_left, 0, 0);
            tableLayoutMain.Controls.Add(pnl_right, 1, 0);
            tableLayoutMain.Dock = DockStyle.Fill;
            tableLayoutMain.Location = new Point(0, 0);
            tableLayoutMain.Name = "tableLayoutMain";
            tableLayoutMain.RowCount = 1;
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutMain.Size = new Size(1813, 1005);
            tableLayoutMain.TabIndex = 0;
            // 
            // pnl_left
            // 
            pnl_left.Controls.Add(tableLayoutLeft);
            pnl_left.Dock = DockStyle.Fill;
            pnl_left.Location = new Point(10, 10);
            pnl_left.Margin = new Padding(10);
            pnl_left.Name = "pnl_left";
            pnl_left.Size = new Size(1293, 985);
            pnl_left.TabIndex = 0;
            // 
            // tableLayoutLeft
            // 
            tableLayoutLeft.ColumnCount = 1;
            tableLayoutLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutLeft.Controls.Add(pnl_original_section, 0, 0);
            tableLayoutLeft.Controls.Add(pnl_keyword_section, 0, 1);
            tableLayoutLeft.Controls.Add(pnl_pagination_section, 0, 2);
            tableLayoutLeft.Dock = DockStyle.Fill;
            tableLayoutLeft.Location = new Point(0, 0);
            tableLayoutLeft.Name = "tableLayoutLeft";
            tableLayoutLeft.RowCount = 3;
            tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            tableLayoutLeft.Size = new Size(1293, 985);
            tableLayoutLeft.TabIndex = 0;
            // 
            // pnl_original_section
            // 
            pnl_original_section.Controls.Add(label10);
            pnl_original_section.Controls.Add(dataGridView_origin);
            pnl_original_section.Dock = DockStyle.Fill;
            pnl_original_section.Location = new Point(5, 5);
            pnl_original_section.Margin = new Padding(5);
            pnl_original_section.Name = "pnl_original_section";
            pnl_original_section.Size = new Size(1283, 452);
            pnl_original_section.TabIndex = 0;
            // 
            // dataGridView_origin
            // 
            dataGridView_origin.AllowUserToAddRows = false;
            dataGridView_origin.AllowUserToDeleteRows = false;
            dataGridView_origin.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView_origin.BackgroundColor = Color.White;
            dataGridView_origin.BorderStyle = BorderStyle.Fixed3D;
            dataGridView_origin.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView_origin.EnableHeadersVisualStyles = false;
            dataGridView_origin.GridColor = Color.LightGray;
            dataGridView_origin.Location = new Point(10, 50);
            dataGridView_origin.MinimumSize = new Size(400, 200);
            dataGridView_origin.Name = "dataGridView_origin";
            dataGridView_origin.ReadOnly = true;
            dataGridView_origin.Size = new Size(1263, 392);
            dataGridView_origin.TabIndex = 0;
            // 
            // pnl_keyword_section
            // 
            pnl_keyword_section.Controls.Add(label4);
            pnl_keyword_section.Controls.Add(dataGridView_keyword);
            pnl_keyword_section.Dock = DockStyle.Fill;
            pnl_keyword_section.Location = new Point(5, 467);
            pnl_keyword_section.Margin = new Padding(5);
            pnl_keyword_section.Name = "pnl_keyword_section";
            pnl_keyword_section.Size = new Size(1283, 452);
            pnl_keyword_section.TabIndex = 1;
            // 
            // dataGridView_keyword
            // 
            dataGridView_keyword.AllowUserToAddRows = false;
            dataGridView_keyword.AllowUserToDeleteRows = false;
            dataGridView_keyword.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView_keyword.BackgroundColor = Color.White;
            dataGridView_keyword.BorderStyle = BorderStyle.Fixed3D;
            dataGridView_keyword.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView_keyword.EnableHeadersVisualStyles = false;
            dataGridView_keyword.GridColor = Color.LightGray;
            dataGridView_keyword.Location = new Point(10, 50);
            dataGridView_keyword.MinimumSize = new Size(400, 200);
            dataGridView_keyword.Name = "dataGridView_keyword";
            dataGridView_keyword.ReadOnly = true;
            dataGridView_keyword.Size = new Size(1263, 392);
            dataGridView_keyword.TabIndex = 1;
            // 
            // pnl_pagination_section
            // 
            pnl_pagination_section.Controls.Add(button5);
            pnl_pagination_section.Controls.Add(btn_nextPage);
            pnl_pagination_section.Controls.Add(btn_prevPage);
            pnl_pagination_section.Controls.Add(cmb_pageSize);
            pnl_pagination_section.Controls.Add(lbl_pageSizeText);
            pnl_pagination_section.Controls.Add(lbl_pagination);
            pnl_pagination_section.Controls.Add(num_pageNumber);
            pnl_pagination_section.Controls.Add(lbl_pagination2);
            pnl_pagination_section.Dock = DockStyle.Fill;
            pnl_pagination_section.Location = new Point(3, 927);
            pnl_pagination_section.Name = "pnl_pagination_section";
            pnl_pagination_section.Size = new Size(1287, 55);
            pnl_pagination_section.TabIndex = 2;
            // 
            // button5
            // 
            button5.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button5.AutoSize = true;
            button5.BackColor = Color.LimeGreen;
            button5.FlatStyle = FlatStyle.Flat;
            button5.Font = new Font("Pretendard", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            button5.ForeColor = Color.White;
            button5.Location = new Point(1153, 12);
            button5.Name = "button5";
            button5.Size = new Size(122, 37);
            button5.TabIndex = 46;
            button5.Text = "Excel 저장";
            button5.UseVisualStyleBackColor = false;
            button5.Click += btn_save_excel_Click;
            // 
            // btn_nextPage
            // 
            btn_nextPage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btn_nextPage.AutoSize = true;
            btn_nextPage.Font = new Font("Pretendard", 14.25F);
            btn_nextPage.Location = new Point(802, 13);
            btn_nextPage.Name = "btn_nextPage";
            btn_nextPage.Size = new Size(86, 35);
            btn_nextPage.TabIndex = 60;
            btn_nextPage.Text = "다음 ▶";
            btn_nextPage.UseVisualStyleBackColor = true;
            btn_nextPage.Click += btn_nextPage_Click;
            // 
            // btn_prevPage
            // 
            btn_prevPage.AutoSize = true;
            btn_prevPage.Font = new Font("Pretendard", 14.25F);
            btn_prevPage.Location = new Point(363, 12);
            btn_prevPage.Name = "btn_prevPage";
            btn_prevPage.Size = new Size(86, 35);
            btn_prevPage.TabIndex = 59;
            btn_prevPage.Text = "◀ 이전";
            btn_prevPage.UseVisualStyleBackColor = true;
            btn_prevPage.Click += btn_prevPage_Click;
            // 
            // cmb_pageSize
            // 
            cmb_pageSize.Font = new Font("Pretendard", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            cmb_pageSize.FormattingEnabled = true;
            cmb_pageSize.Location = new Point(130, 14);
            cmb_pageSize.Name = "cmb_pageSize";
            cmb_pageSize.Size = new Size(121, 33);
            cmb_pageSize.TabIndex = 58;
            // 
            // lbl_pageSizeText
            // 
            lbl_pageSizeText.AutoSize = true;
            lbl_pageSizeText.Font = new Font("Pretendard", 14F);
            lbl_pageSizeText.Location = new Point(10, 17);
            lbl_pageSizeText.Name = "lbl_pageSizeText";
            lbl_pageSizeText.Size = new Size(125, 25);
            lbl_pageSizeText.TabIndex = 57;
            lbl_pageSizeText.Text = "페이지 크기 :";
            // 
            // lbl_pagination
            // 
            lbl_pagination.AutoSize = true;
            lbl_pagination.Font = new Font("Pretendard", 14F);
            lbl_pagination.Location = new Point(455, 18);
            lbl_pagination.Name = "lbl_pagination";
            lbl_pagination.Size = new Size(80, 25);
            lbl_pagination.TabIndex = 54;
            lbl_pagination.Text = "페이지 :";
            // 
            // num_pageNumber
            // 
            num_pageNumber.Font = new Font("Pretendard", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            num_pageNumber.Location = new Point(543, 16);
            num_pageNumber.Name = "num_pageNumber";
            num_pageNumber.Size = new Size(52, 33);
            num_pageNumber.TabIndex = 56;
            // 
            // lbl_pagination2
            // 
            lbl_pagination2.AutoSize = true;
            lbl_pagination2.Font = new Font("Pretendard", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            lbl_pagination2.Location = new Point(601, 19);
            lbl_pagination2.Name = "lbl_pagination2";
            lbl_pagination2.Size = new Size(118, 25);
            lbl_pagination2.TabIndex = 55;
            lbl_pagination2.Text = "/ 0 (총 0 행)";
            // 
            // pnl_right
            // 
            pnl_right.Controls.Add(tableLayoutRight);
            pnl_right.Dock = DockStyle.Fill;
            pnl_right.Location = new Point(1323, 10);
            pnl_right.Margin = new Padding(10);
            pnl_right.Name = "pnl_right";
            pnl_right.Size = new Size(480, 985);
            pnl_right.TabIndex = 1;
            // 
            // tableLayoutRight
            // 
            tableLayoutRight.ColumnCount = 1;
            tableLayoutRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutRight.Controls.Add(groupBox1, 0, 0);
            tableLayoutRight.Controls.Add(groupBox3, 0, 1);
            tableLayoutRight.Dock = DockStyle.Fill;
            tableLayoutRight.Location = new Point(0, 0);
            tableLayoutRight.Name = "tableLayoutRight";
            tableLayoutRight.RowCount = 2;
            tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
            tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
            tableLayoutRight.Size = new Size(480, 985);
            tableLayoutRight.TabIndex = 0;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(dataGridView_classify);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Font = new Font("Pretendard", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            groupBox1.Location = new Point(5, 5);
            groupBox1.Margin = new Padding(5);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(470, 630);
            groupBox1.TabIndex = 61;
            groupBox1.TabStop = false;
            groupBox1.Text = "Clustering 결과";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Pretendard", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            label3.ForeColor = Color.IndianRed;
            label3.Location = new Point(10, 40);
            label3.Name = "label3";
            label3.Size = new Size(249, 17);
            label3.TabIndex = 48;
            label3.Text = "※ 클러스터명은 직접 수정이 가능합니다.";
            // 
            // dataGridView_classify
            // 
            dataGridView_classify.AllowUserToAddRows = false;
            dataGridView_classify.AllowUserToDeleteRows = false;
            dataGridView_classify.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView_classify.BackgroundColor = Color.White;
            dataGridView_classify.BorderStyle = BorderStyle.Fixed3D;
            dataGridView_classify.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView_classify.EnableHeadersVisualStyles = false;
            dataGridView_classify.GridColor = Color.LightGray;
            dataGridView_classify.Location = new Point(10, 82);
            dataGridView_classify.MinimumSize = new Size(300, 200);
            dataGridView_classify.Name = "dataGridView_classify";
            dataGridView_classify.Size = new Size(450, 538);
            dataGridView_classify.TabIndex = 2;
            dataGridView_classify.CellClick += dataGridView_classify_CellClick;
            dataGridView_classify.CellValueChanged += dataGridView_classify_CellValueChanged;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(label7);
            groupBox3.Controls.Add(del_col_list_allcheck);
            groupBox3.Controls.Add(dataGridView_delete_col2);
            groupBox3.Controls.Add(restore_col_btn);
            groupBox3.Dock = DockStyle.Fill;
            groupBox3.Font = new Font("Pretendard", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            groupBox3.Location = new Point(5, 645);
            groupBox3.Margin = new Padding(5);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(470, 335);
            groupBox3.TabIndex = 44;
            groupBox3.TabStop = false;
            groupBox3.Text = "제거 열 설정";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Pretendard", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            label7.ForeColor = Color.IndianRed;
            label7.Location = new Point(10, 40);
            label7.Name = "label7";
            label7.Size = new Size(267, 17);
            label7.TabIndex = 45;
            label7.Text = "※ 선택한 열 정보만 출력하도록 지원합니다.";
            // 
            // del_col_list_allcheck
            // 
            del_col_list_allcheck.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            del_col_list_allcheck.AutoSize = true;
            del_col_list_allcheck.Font = new Font("Pretendard", 14.25F);
            del_col_list_allcheck.Location = new Point(350, 65);
            del_col_list_allcheck.Name = "del_col_list_allcheck";
            del_col_list_allcheck.Size = new Size(114, 29);
            del_col_list_allcheck.TabIndex = 43;
            del_col_list_allcheck.Text = "전체 선택";
            del_col_list_allcheck.UseVisualStyleBackColor = true;
            del_col_list_allcheck.CheckedChanged += del_col_list_allcheck_CheckedChanged;
            // 
            // dataGridView_delete_col2
            // 
            dataGridView_delete_col2.AllowUserToAddRows = false;
            dataGridView_delete_col2.AllowUserToDeleteRows = false;
            dataGridView_delete_col2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView_delete_col2.BackgroundColor = Color.White;
            dataGridView_delete_col2.BorderStyle = BorderStyle.Fixed3D;
            dataGridView_delete_col2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView_delete_col2.EnableHeadersVisualStyles = false;
            dataGridView_delete_col2.GridColor = Color.LightGray;
            dataGridView_delete_col2.Location = new Point(10, 65);
            dataGridView_delete_col2.MinimumSize = new Size(250, 150);
            dataGridView_delete_col2.Name = "dataGridView_delete_col2";
            dataGridView_delete_col2.Size = new Size(330, 226);
            dataGridView_delete_col2.TabIndex = 42;
            // 
            // restore_col_btn
            // 
            restore_col_btn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            restore_col_btn.AutoSize = true;
            restore_col_btn.BackColor = Color.Orange;
            restore_col_btn.FlatStyle = FlatStyle.Flat;
            restore_col_btn.Font = new Font("Pretendard", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            restore_col_btn.ForeColor = Color.White;
            restore_col_btn.Location = new Point(330, 296);
            restore_col_btn.Name = "restore_col_btn";
            restore_col_btn.Size = new Size(131, 35);
            restore_col_btn.TabIndex = 14;
            restore_col_btn.Text = "선택 열 적용";
            restore_col_btn.UseVisualStyleBackColor = false;
            restore_col_btn.Click += restore_col_btn_Click;
            // 
            // label10
            // 
            label10.Anchor = AnchorStyles.None;
            label10.BackColor = Color.SteelBlue;
            label10.Font = new Font("Pretendard", 18F, FontStyle.Bold);
            label10.ForeColor = Color.White;
            label10.Location = new Point(432, 7);
            label10.Name = "label10";
            label10.Size = new Size(380, 40);
            label10.TabIndex = 57;
            label10.Text = "원본 테이블";
            label10.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            label4.Anchor = AnchorStyles.None;
            label4.BackColor = Color.SteelBlue;
            label4.Font = new Font("Pretendard", 18F, FontStyle.Bold);
            label4.ForeColor = Color.White;
            label4.Location = new Point(432, 7);
            label4.Name = "label4";
            label4.Size = new Size(380, 40);
            label4.TabIndex = 58;
            label4.Text = "Export 결과";
            label4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Pretendard", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            label1.ForeColor = Color.IndianRed;
            label1.Location = new Point(10, 62);
            label1.Name = "label1";
            label1.Size = new Size(417, 17);
            label1.TabIndex = 49;
            label1.Text = "※ 각 항목을 우클릭하여 세부 클러스터링 메뉴로 이동할 수 있습니다.";
            // 
            // uc_Classification
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            Controls.Add(tableLayoutMain);
            MinimumSize = new Size(1600, 1000);
            Name = "uc_Classification";
            Size = new Size(1813, 1005);
            tableLayoutMain.ResumeLayout(false);
            pnl_left.ResumeLayout(false);
            tableLayoutLeft.ResumeLayout(false);
            pnl_original_section.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView_origin).EndInit();
            pnl_keyword_section.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView_keyword).EndInit();
            pnl_pagination_section.ResumeLayout(false);
            pnl_pagination_section.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)num_pageNumber).EndInit();
            pnl_right.ResumeLayout(false);
            tableLayoutRight.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_classify).EndInit();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_delete_col2).EndInit();
            ResumeLayout(false);
        }

        #endregion

        // 메인 레이아웃
        private TableLayoutPanel tableLayoutMain;
        private Panel pnl_left;
        private Panel pnl_right;
        private TableLayoutPanel tableLayoutLeft;
        private TableLayoutPanel tableLayoutRight;

        // 좌측 패널 구성 요소
        private Panel pnl_original_section;
        private Panel pnl_keyword_section;
        private Panel pnl_pagination_section;

        // 기존 컨트롤들 (모든 컨트롤명 유지)
        private DataGridView dataGridView_origin;
        private DataGridView dataGridView_keyword;
        private DataGridView dataGridView_classify;
        private GroupBox groupBox3;
        private CheckBox del_col_list_allcheck;
        private DataGridView dataGridView_delete_col2;
        private Button restore_col_btn;
        private Button button5;
        private Button btn_nextPage;
        private Button btn_prevPage;
        private ComboBox cmb_pageSize;
        private Label lbl_pageSizeText;
        private Label lbl_pagination;
        private NumericUpDown num_pageNumber;
        private Label lbl_pagination2;
        private GroupBox groupBox1;
        private Label label7;
        private Label label3;
        private Label label10;
        private Label label4;
        private Label label1;
    }
}
