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
            // 메인 컨테이너
            this.tableLayoutMain = new TableLayoutPanel();
            this.pnl_left = new Panel();
            this.pnl_right = new Panel();

            // 좌측 패널 컨트롤들
            this.tableLayoutLeft = new TableLayoutPanel();
            this.pnl_original_section = new Panel();
            this.pnl_keyword_section = new Panel();
            this.pnl_pagination_section = new Panel();

            // 우측 패널 컨트롤들  
            this.tableLayoutRight = new TableLayoutPanel();
            this.groupBox1 = new GroupBox();
            this.groupBox3 = new GroupBox();

            // 기존 컨트롤들
            this.dataGridView_origin = new DataGridView();
            this.dataGridView_keyword = new DataGridView();
            this.dataGridView_classify = new DataGridView();
            this.label1 = new Label();
            this.label2 = new Label();
            this.label3 = new Label();
            this.label7 = new Label();
            this.del_col_list_allcheck = new CheckBox();
            this.dataGridView_delete_col2 = new DataGridView();
            this.restore_col_btn = new Button();
            this.button5 = new Button();
            this.btn_nextPage = new Button();
            this.btn_prevPage = new Button();
            this.cmb_pageSize = new ComboBox();
            this.lbl_pageSizeText = new Label();
            this.lbl_pagination = new Label();
            this.num_pageNumber = new NumericUpDown();
            this.lbl_pagination2 = new Label();

            // 컨트롤 초기화 시작
            this.tableLayoutMain.SuspendLayout();
            this.pnl_left.SuspendLayout();
            this.pnl_right.SuspendLayout();
            this.tableLayoutLeft.SuspendLayout();
            this.tableLayoutRight.SuspendLayout();
            this.pnl_original_section.SuspendLayout();
            this.pnl_keyword_section.SuspendLayout();
            this.pnl_pagination_section.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_origin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_keyword)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_classify)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_delete_col2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_pageNumber)).BeginInit();
            this.SuspendLayout();

            // 
            // tableLayoutMain
            // 
            this.tableLayoutMain.ColumnCount = 2;
            this.tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 500F));
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
            this.pnl_left.Controls.Add(this.tableLayoutLeft);
            this.pnl_left.Dock = DockStyle.Fill;
            this.pnl_left.Location = new Point(10, 10);
            this.pnl_left.Margin = new Padding(10);
            this.pnl_left.Name = "pnl_left";
            this.pnl_left.Size = new Size(1384, 997);
            this.pnl_left.TabIndex = 0;

            // 
            // pnl_right
            // 
            this.pnl_right.Controls.Add(this.tableLayoutRight);
            this.pnl_right.Dock = DockStyle.Fill;
            this.pnl_right.Location = new Point(1414, 10);
            this.pnl_right.Margin = new Padding(10);
            this.pnl_right.Name = "pnl_right";
            this.pnl_right.Size = new Size(480, 997);
            this.pnl_right.TabIndex = 1;

            // 
            // tableLayoutLeft
            // 
            this.tableLayoutLeft.ColumnCount = 1;
            this.tableLayoutLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.tableLayoutLeft.Controls.Add(this.pnl_original_section, 0, 0);
            this.tableLayoutLeft.Controls.Add(this.pnl_keyword_section, 0, 1);
            this.tableLayoutLeft.Controls.Add(this.pnl_pagination_section, 0, 2);
            this.tableLayoutLeft.Dock = DockStyle.Fill;
            this.tableLayoutLeft.Location = new Point(0, 0);
            this.tableLayoutLeft.Name = "tableLayoutLeft";
            this.tableLayoutLeft.RowCount = 3;
            this.tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            this.tableLayoutLeft.Size = new Size(1384, 997);
            this.tableLayoutLeft.TabIndex = 0;

            // 
            // pnl_original_section
            // 
            this.pnl_original_section.Controls.Add(this.label1);
            this.pnl_original_section.Controls.Add(this.dataGridView_origin);
            this.pnl_original_section.Dock = DockStyle.Fill;
            this.pnl_original_section.Location = new Point(5, 5);
            this.pnl_original_section.Margin = new Padding(5);
            this.pnl_original_section.Name = "pnl_original_section";
            this.pnl_original_section.Size = new Size(1374, 458);
            this.pnl_original_section.TabIndex = 0;

            // 
            // pnl_keyword_section
            // 
            this.pnl_keyword_section.Controls.Add(this.label2);
            this.pnl_keyword_section.Controls.Add(this.dataGridView_keyword);
            this.pnl_keyword_section.Dock = DockStyle.Fill;
            this.pnl_keyword_section.Location = new Point(5, 473);
            this.pnl_keyword_section.Margin = new Padding(5);
            this.pnl_keyword_section.Name = "pnl_keyword_section";
            this.pnl_keyword_section.Size = new Size(1374, 458);
            this.pnl_keyword_section.TabIndex = 1;

            // 
            // pnl_pagination_section
            // 
            this.pnl_pagination_section.Controls.Add(this.button5);
            this.pnl_pagination_section.Controls.Add(this.btn_nextPage);
            this.pnl_pagination_section.Controls.Add(this.btn_prevPage);
            this.pnl_pagination_section.Controls.Add(this.cmb_pageSize);
            this.pnl_pagination_section.Controls.Add(this.lbl_pageSizeText);
            this.pnl_pagination_section.Controls.Add(this.lbl_pagination);
            this.pnl_pagination_section.Controls.Add(this.num_pageNumber);
            this.pnl_pagination_section.Controls.Add(this.lbl_pagination2);
            this.pnl_pagination_section.Dock = DockStyle.Fill;
            this.pnl_pagination_section.Location = new Point(0, 937);
            this.pnl_pagination_section.Name = "pnl_pagination_section";
            this.pnl_pagination_section.Size = new Size(1384, 60);
            this.pnl_pagination_section.TabIndex = 2;

            // 
            // tableLayoutRight
            // 
            this.tableLayoutRight.ColumnCount = 1;
            this.tableLayoutRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.tableLayoutRight.Controls.Add(this.groupBox1, 0, 0);
            this.tableLayoutRight.Controls.Add(this.groupBox3, 0, 1);
            this.tableLayoutRight.Dock = DockStyle.Fill;
            this.tableLayoutRight.Location = new Point(0, 0);
            this.tableLayoutRight.Name = "tableLayoutRight";
            this.tableLayoutRight.RowCount = 2;
            this.tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
            this.tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
            this.tableLayoutRight.Size = new Size(480, 997);
            this.tableLayoutRight.TabIndex = 0;

            // 
            // dataGridView_origin
            // 
            this.dataGridView_origin.AllowUserToAddRows = false;
            this.dataGridView_origin.AllowUserToDeleteRows = false;
            this.dataGridView_origin.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
            | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.dataGridView_origin.BackgroundColor = Color.White;
            this.dataGridView_origin.BorderStyle = BorderStyle.Fixed3D;
            this.dataGridView_origin.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView_origin.EnableHeadersVisualStyles = false;
            this.dataGridView_origin.GridColor = Color.LightGray;
            this.dataGridView_origin.Location = new Point(10, 50);
            this.dataGridView_origin.MinimumSize = new Size(400, 200);
            this.dataGridView_origin.Name = "dataGridView_origin";
            this.dataGridView_origin.ReadOnly = true;
            this.dataGridView_origin.Size = new Size(1354, 398);
            this.dataGridView_origin.TabIndex = 0;

            // 
            // dataGridView_keyword
            // 
            this.dataGridView_keyword.AllowUserToAddRows = false;
            this.dataGridView_keyword.AllowUserToDeleteRows = false;
            this.dataGridView_keyword.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
            | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.dataGridView_keyword.BackgroundColor = Color.White;
            this.dataGridView_keyword.BorderStyle = BorderStyle.Fixed3D;
            this.dataGridView_keyword.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView_keyword.EnableHeadersVisualStyles = false;
            this.dataGridView_keyword.GridColor = Color.LightGray;
            this.dataGridView_keyword.Location = new Point(10, 50);
            this.dataGridView_keyword.MinimumSize = new Size(400, 200);
            this.dataGridView_keyword.Name = "dataGridView_keyword";
            this.dataGridView_keyword.ReadOnly = true;
            this.dataGridView_keyword.Size = new Size(1354, 398);
            this.dataGridView_keyword.TabIndex = 1;

            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new Font("맑은 고딕", 26.25F);
            this.label1.Location = new Point(10, 5);
            this.label1.Name = "label1";
            this.label1.Size = new Size(207, 47);
            this.label1.TabIndex = 3;
            this.label1.Text = "원본 테이블";

            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new Font("맑은 고딕", 26.25F);
            this.label2.Location = new Point(10, 5);
            this.label2.Name = "label2";
            this.label2.Size = new Size(202, 47);
            this.label2.TabIndex = 4;
            this.label2.Text = "Export 결과";

            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.dataGridView_classify);
            this.groupBox1.Dock = DockStyle.Fill;
            this.groupBox1.Font = new Font("맑은 고딕", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.groupBox1.Location = new Point(5, 5);
            this.groupBox1.Margin = new Padding(5);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new Size(470, 638);
            this.groupBox1.TabIndex = 61;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Clustering 결과";

            // 
            // dataGridView_classify
            // 
            this.dataGridView_classify.AllowUserToAddRows = false;
            this.dataGridView_classify.AllowUserToDeleteRows = false;
            this.dataGridView_classify.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
            | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.dataGridView_classify.BackgroundColor = Color.White;
            this.dataGridView_classify.BorderStyle = BorderStyle.Fixed3D;
            this.dataGridView_classify.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView_classify.EnableHeadersVisualStyles = false;
            this.dataGridView_classify.GridColor = Color.LightGray;
            this.dataGridView_classify.Location = new Point(10, 65);
            this.dataGridView_classify.MinimumSize = new Size(300, 200);
            this.dataGridView_classify.Name = "dataGridView_classify";
            this.dataGridView_classify.Size = new Size(450, 563);
            this.dataGridView_classify.TabIndex = 2;
            this.dataGridView_classify.CellClick += dataGridView_classify_CellClick;
            this.dataGridView_classify.CellValueChanged += dataGridView_classify_CellValueChanged;

            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new Font("맑은 고딕", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            this.label3.ForeColor = Color.IndianRed;
            this.label3.Location = new Point(10, 40);
            this.label3.Name = "label3";
            this.label3.Size = new Size(249, 17);
            this.label3.TabIndex = 48;
            this.label3.Text = "※ 클러스터명은 직접 수정이 가능합니다.";

            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label7);
            this.groupBox3.Controls.Add(this.del_col_list_allcheck);
            this.groupBox3.Controls.Add(this.dataGridView_delete_col2);
            this.groupBox3.Controls.Add(this.restore_col_btn);
            this.groupBox3.Dock = DockStyle.Fill;
            this.groupBox3.Font = new Font("맑은 고딕", 16F);
            this.groupBox3.Location = new Point(5, 653);
            this.groupBox3.Margin = new Padding(5);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new Size(470, 339);
            this.groupBox3.TabIndex = 44;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "제거 열 설정";

            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new Font("맑은 고딕", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            this.label7.ForeColor = Color.IndianRed;
            this.label7.Location = new Point(10, 40);
            this.label7.Name = "label7";
            this.label7.Size = new Size(267, 17);
            this.label7.TabIndex = 45;
            this.label7.Text = "※ 선택한 열 정보만 출력하도록 지원합니다.";

            // 
            // del_col_list_allcheck
            // 
            this.del_col_list_allcheck.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.del_col_list_allcheck.AutoSize = true;
            this.del_col_list_allcheck.Font = new Font("맑은 고딕", 14.25F);
            this.del_col_list_allcheck.Location = new Point(350, 65);
            this.del_col_list_allcheck.Name = "del_col_list_allcheck";
            this.del_col_list_allcheck.Size = new Size(114, 29);
            this.del_col_list_allcheck.TabIndex = 43;
            this.del_col_list_allcheck.Text = "전체 선택";
            this.del_col_list_allcheck.UseVisualStyleBackColor = true;
            this.del_col_list_allcheck.CheckedChanged += del_col_list_allcheck_CheckedChanged;

            // 
            // dataGridView_delete_col2
            // 
            this.dataGridView_delete_col2.AllowUserToAddRows = false;
            this.dataGridView_delete_col2.AllowUserToDeleteRows = false;
            this.dataGridView_delete_col2.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
            | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.dataGridView_delete_col2.BackgroundColor = Color.White;
            this.dataGridView_delete_col2.BorderStyle = BorderStyle.Fixed3D;
            this.dataGridView_delete_col2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView_delete_col2.EnableHeadersVisualStyles = false;
            this.dataGridView_delete_col2.GridColor = Color.LightGray;
            this.dataGridView_delete_col2.Location = new Point(10, 65);
            this.dataGridView_delete_col2.MinimumSize = new Size(250, 150);
            this.dataGridView_delete_col2.Name = "dataGridView_delete_col2";
            this.dataGridView_delete_col2.Size = new Size(330, 230);
            this.dataGridView_delete_col2.TabIndex = 42;

            // 
            // restore_col_btn
            // 
            this.restore_col_btn.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
            this.restore_col_btn.AutoSize = true;
            this.restore_col_btn.Font = new Font("맑은 고딕", 14F);
            this.restore_col_btn.Location = new Point(330, 300);
            this.restore_col_btn.Name = "restore_col_btn";
            this.restore_col_btn.Size = new Size(131, 35);
            this.restore_col_btn.TabIndex = 14;
            this.restore_col_btn.Text = "선택 열 적용";
            this.restore_col_btn.UseVisualStyleBackColor = true;
            this.restore_col_btn.Click += restore_col_btn_Click;

            // 
            // button5
            // 
            this.button5.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.button5.AutoSize = true;
            this.button5.Font = new Font("맑은 고딕", 14.25F);
            this.button5.Location = new Point(1250, 12);
            this.button5.Name = "button5";
            this.button5.Size = new Size(122, 35);
            this.button5.TabIndex = 46;
            this.button5.Text = "Excel 저장";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += btn_save_excel_Click;

            // 페이징 컨트롤들
            //

            // btn_nextPage
            // 
            this.btn_nextPage.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.btn_nextPage.AutoSize = true;
            this.btn_nextPage.Font = new Font("맑은 고딕", 14.25F);
            this.btn_nextPage.Location = new Point(950, 12);
            this.btn_nextPage.Name = "btn_nextPage";
            this.btn_nextPage.Size = new Size(86, 35);
            this.btn_nextPage.TabIndex = 60;
            this.btn_nextPage.Text = "다음 ▶";
            this.btn_nextPage.UseVisualStyleBackColor = true;
            this.btn_nextPage.Click += btn_nextPage_Click;

            // 
            // btn_prevPage
            // 
            this.btn_prevPage.AutoSize = true;
            this.btn_prevPage.Font = new Font("맑은 고딕", 14.25F);
            this.btn_prevPage.Location = new Point(520, 11);
            this.btn_prevPage.Name = "btn_prevPage";
            this.btn_prevPage.Size = new Size(86, 35);
            this.btn_prevPage.TabIndex = 59;
            this.btn_prevPage.Text = "◀ 이전";
            this.btn_prevPage.UseVisualStyleBackColor = true;
            this.btn_prevPage.Click += btn_prevPage_Click;

            // 
            // cmb_pageSize
            // 
            this.cmb_pageSize.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.cmb_pageSize.FormattingEnabled = true;
            this.cmb_pageSize.Location = new Point(130, 14);
            this.cmb_pageSize.Name = "cmb_pageSize";
            this.cmb_pageSize.Size = new Size(121, 33);
            this.cmb_pageSize.TabIndex = 58;

            // 
            // lbl_pageSizeText
            // 
            this.lbl_pageSizeText.AutoSize = true;
            this.lbl_pageSizeText.Font = new Font("맑은 고딕", 14F);
            this.lbl_pageSizeText.Location = new Point(10, 17);
            this.lbl_pageSizeText.Name = "lbl_pageSizeText";
            this.lbl_pageSizeText.Size = new Size(125, 25);
            this.lbl_pageSizeText.TabIndex = 57;
            this.lbl_pageSizeText.Text = "페이지 크기 :";

            // 
            // lbl_pagination
            // 
            this.lbl_pagination.AutoSize = true;
            this.lbl_pagination.Font = new Font("맑은 고딕", 14F);
            this.lbl_pagination.Location = new Point(612, 17);
            this.lbl_pagination.Name = "lbl_pagination";
            this.lbl_pagination.Size = new Size(80, 25);
            this.lbl_pagination.TabIndex = 54;
            this.lbl_pagination.Text = "페이지 :";

            // 
            // num_pageNumber
            // 
            this.num_pageNumber.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.num_pageNumber.Location = new Point(700, 15);
            this.num_pageNumber.Name = "num_pageNumber";
            this.num_pageNumber.Size = new Size(52, 33);
            this.num_pageNumber.TabIndex = 56;

            // 
            // lbl_pagination2
            // 
            this.lbl_pagination2.AutoSize = true;
            this.lbl_pagination2.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.lbl_pagination2.Location = new Point(758, 18);
            this.lbl_pagination2.Name = "lbl_pagination2";
            this.lbl_pagination2.Size = new Size(118, 25);
            this.lbl_pagination2.TabIndex = 55;
            this.lbl_pagination2.Text = "/ 0 (총 0 행)";

            // 
            // uc_Classification
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.White;
            this.Controls.Add(this.tableLayoutMain);
            this.MinimumSize = new Size(1600, 1000);
            this.Name = "uc_Classification";

            // 컨트롤 정리
            this.tableLayoutMain.ResumeLayout(false);
            this.pnl_left.ResumeLayout(false);
            this.pnl_right.ResumeLayout(false);
            this.tableLayoutLeft.ResumeLayout(false);
            this.tableLayoutRight.ResumeLayout(false);
            this.pnl_original_section.ResumeLayout(false);
            this.pnl_original_section.PerformLayout();
            this.pnl_keyword_section.ResumeLayout(false);
            this.pnl_keyword_section.PerformLayout();
            this.pnl_pagination_section.ResumeLayout(false);
            this.pnl_pagination_section.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_origin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_keyword)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_classify)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_delete_col2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_pageNumber)).EndInit();
            this.ResumeLayout(false);
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
        private Label label1;
        private Label label2;
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
    }
}
