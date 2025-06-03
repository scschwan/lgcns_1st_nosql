namespace FinanceTool
{
    partial class uc_DataTransform
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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();

            // 메인 컨테이너
            this.tableLayoutMain = new TableLayoutPanel();
            this.pnl_left = new Panel();
            this.pnl_right = new Panel();

            // 좌측 패널 컨트롤들
            this.tableLayoutLeft = new TableLayoutPanel();
            this.pnl_original_data = new Panel();
            this.pnl_transform_data = new Panel();
            this.pnl_original_header = new Panel();
            this.pnl_transform_header = new Panel();
            this.pnl_original_pagination = new Panel();
            this.pnl_transform_pagination = new Panel();

            // 데이터 그리드뷰
            this.dataGridView_2nd = new DataGridView();
            this.dataGridView_transform = new DataGridView();

            // 헤더 레이블
            this.label1 = new Label();
            this.label2 = new Label();

            // 페이징 컨트롤들 (상단)
            this.btn_nextPage = new Button();
            this.btn_prevPage = new Button();
            this.cmb_pageSize = new ComboBox();
            this.lbl_pageSizeText = new Label();
            this.lbl_pagination = new Label();
            this.num_pageNumber = new NumericUpDown();
            this.lbl_pagination2 = new Label();

            // 페이징 컨트롤들 (하단)
            this.btn_nextPage2 = new Button();
            this.btn_prevPage2 = new Button();
            this.cmb_pageSize2 = new ComboBox();
            this.label6 = new Label();
            this.lbl_pagination3 = new Label();
            this.num_pageNumber2 = new NumericUpDown();
            this.lbl_pagination4 = new Label();

            // 우측 패널 컨트롤들
            this.tableLayoutRight = new TableLayoutPanel();
            this.groupBox1 = new GroupBox();
            this.groupbox2 = new GroupBox();
            this.groupBox3 = new GroupBox();

            // GroupBox1 컨트롤들 (키워드 요약)
            this.sum_keyword_table = new DataGridView();
            this.label5 = new Label();
            this.decimal_combo = new ComboBox();

            // GroupBox2 컨트롤들 (키워드 변환)
            this.modified_keyword = new TextBox();
            this.label4 = new Label();
            this.keyword_search_radio2 = new RadioButton();
            this.keyword_search_radio1 = new RadioButton();
            this.keyword_search_combo = new ComboBox();
            this.search_keyword = new TextBox();
            this.check_all_keyword_list = new CheckBox();
            this.change_keyword = new Button();
            this.keyword_search_button = new Button();
            this.match_keyword_table = new DataGridView();

            // GroupBox3 컨트롤들 (클러스터 설정)
            this.prod_col_check = new CheckBox();
            this.button5 = new Button();
            this.label3 = new Label();
            this.button2 = new Button();
            this.dept_col_check = new CheckBox();

            // 컨트롤 초기화 시작
            this.tableLayoutMain.SuspendLayout();
            this.pnl_left.SuspendLayout();
            this.pnl_right.SuspendLayout();
            this.tableLayoutLeft.SuspendLayout();
            this.tableLayoutRight.SuspendLayout();
            this.pnl_original_data.SuspendLayout();
            this.pnl_transform_data.SuspendLayout();
            this.pnl_original_header.SuspendLayout();
            this.pnl_transform_header.SuspendLayout();
            this.pnl_original_pagination.SuspendLayout();
            this.pnl_transform_pagination.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupbox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_2nd)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_transform)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sum_keyword_table)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.match_keyword_table)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_pageNumber)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_pageNumber2)).BeginInit();
            this.SuspendLayout();

            // 
            // tableLayoutMain
            // 
            this.tableLayoutMain.ColumnCount = 2;
            this.tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            this.tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            // 수정: 100% : 450px 고정 (여백 해결)
            //this.tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));
            //this.tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 650F));
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
            this.pnl_left.Size = new Size(1122, 997);
            this.pnl_left.TabIndex = 0;

            // 
            // pnl_right
            // 
            this.pnl_right.Controls.Add(this.tableLayoutRight);
            this.pnl_right.Dock = DockStyle.Fill;
            this.pnl_right.Location = new Point(1152, 10);
            this.pnl_right.Margin = new Padding(10);
            this.pnl_right.Name = "pnl_right";
            this.pnl_right.Size = new Size(742, 997);
            this.pnl_right.TabIndex = 1;

            // 
            // tableLayoutLeft
            // 
            this.tableLayoutLeft.ColumnCount = 1;
            this.tableLayoutLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.tableLayoutLeft.Controls.Add(this.pnl_original_header, 0, 0);
            this.tableLayoutLeft.Controls.Add(this.pnl_original_data, 0, 1);
            this.tableLayoutLeft.Controls.Add(this.pnl_original_pagination, 0, 2);
            this.tableLayoutLeft.Controls.Add(this.pnl_transform_header, 0, 3);
            this.tableLayoutLeft.Controls.Add(this.pnl_transform_data, 0, 4);
            this.tableLayoutLeft.Controls.Add(this.pnl_transform_pagination, 0, 5);
            this.tableLayoutLeft.Dock = DockStyle.Fill;
            this.tableLayoutLeft.Location = new Point(0, 0);
            this.tableLayoutLeft.Name = "tableLayoutLeft";
            this.tableLayoutLeft.RowCount = 6;
            this.tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            this.tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            this.tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            this.tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            this.tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            this.tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            this.tableLayoutLeft.Size = new Size(1122, 997);
            this.tableLayoutLeft.TabIndex = 0;

            // 
            // pnl_original_header
            // 
            this.pnl_original_header.Controls.Add(this.label1);
            this.pnl_original_header.Dock = DockStyle.Fill;
            this.pnl_original_header.Location = new Point(0, 0);
            this.pnl_original_header.Name = "pnl_original_header";
            this.pnl_original_header.Size = new Size(1122, 50);
            this.pnl_original_header.TabIndex = 0;

            // 
            // pnl_original_data
            // 
            this.pnl_original_data.Controls.Add(this.dataGridView_2nd);
            this.pnl_original_data.Dock = DockStyle.Fill;
            this.pnl_original_data.Location = new Point(5, 55);
            this.pnl_original_data.Margin = new Padding(5);
            this.pnl_original_data.Name = "pnl_original_data";
            this.pnl_original_data.Size = new Size(1112, 363);
            this.pnl_original_data.TabIndex = 1;

            // 
            // pnl_original_pagination
            // 
            this.pnl_original_pagination.Controls.Add(this.btn_nextPage);
            this.pnl_original_pagination.Controls.Add(this.btn_prevPage);
            this.pnl_original_pagination.Controls.Add(this.cmb_pageSize);
            this.pnl_original_pagination.Controls.Add(this.lbl_pageSizeText);
            this.pnl_original_pagination.Controls.Add(this.lbl_pagination);
            this.pnl_original_pagination.Controls.Add(this.num_pageNumber);
            this.pnl_original_pagination.Controls.Add(this.lbl_pagination2);
            this.pnl_original_pagination.Dock = DockStyle.Fill;
            this.pnl_original_pagination.Location = new Point(0, 423);
            this.pnl_original_pagination.Name = "pnl_original_pagination";
            this.pnl_original_pagination.Size = new Size(1122, 50);
            this.pnl_original_pagination.TabIndex = 2;

            // 
            // pnl_transform_header
            // 
            this.pnl_transform_header.Controls.Add(this.label2);
            this.pnl_transform_header.Dock = DockStyle.Fill;
            this.pnl_transform_header.Location = new Point(0, 473);
            this.pnl_transform_header.Name = "pnl_transform_header";
            this.pnl_transform_header.Size = new Size(1122, 50);
            this.pnl_transform_header.TabIndex = 3;

            // 
            // pnl_transform_data
            // 
            this.pnl_transform_data.Controls.Add(this.dataGridView_transform);
            this.pnl_transform_data.Dock = DockStyle.Fill;
            this.pnl_transform_data.Location = new Point(5, 528);
            this.pnl_transform_data.Margin = new Padding(5);
            this.pnl_transform_data.Name = "pnl_transform_data";
            this.pnl_transform_data.Size = new Size(1112, 363);
            this.pnl_transform_data.TabIndex = 4;

            // 
            // pnl_transform_pagination
            // 
            this.pnl_transform_pagination.Controls.Add(this.btn_nextPage2);
            this.pnl_transform_pagination.Controls.Add(this.btn_prevPage2);
            this.pnl_transform_pagination.Controls.Add(this.cmb_pageSize2);
            this.pnl_transform_pagination.Controls.Add(this.label6);
            this.pnl_transform_pagination.Controls.Add(this.lbl_pagination3);
            this.pnl_transform_pagination.Controls.Add(this.num_pageNumber2);
            this.pnl_transform_pagination.Controls.Add(this.lbl_pagination4);
            this.pnl_transform_pagination.Dock = DockStyle.Fill;
            this.pnl_transform_pagination.Location = new Point(0, 896);
            this.pnl_transform_pagination.Name = "pnl_transform_pagination";
            this.pnl_transform_pagination.Size = new Size(1122, 50);
            this.pnl_transform_pagination.TabIndex = 5;

            // 
            // dataGridView_2nd
            // 
            this.dataGridView_2nd.AllowUserToAddRows = false;
            this.dataGridView_2nd.AllowUserToDeleteRows = false;
            this.dataGridView_2nd.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
            | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.dataGridView_2nd.BackgroundColor = Color.White;
            this.dataGridView_2nd.BorderStyle = BorderStyle.Fixed3D;
            this.dataGridView_2nd.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView_2nd.EnableHeadersVisualStyles = false;
            this.dataGridView_2nd.GridColor = Color.LightGray;
            this.dataGridView_2nd.Location = new Point(5, 5);
            this.dataGridView_2nd.MinimumSize = new Size(400, 200);
            this.dataGridView_2nd.Name = "dataGridView_2nd";
            this.dataGridView_2nd.ReadOnly = true;
            this.dataGridView_2nd.Size = new Size(1102, 353);
            this.dataGridView_2nd.TabIndex = 0;

            // 
            // dataGridView_transform
            // 
            this.dataGridView_transform.AllowUserToAddRows = false;
            this.dataGridView_transform.AllowUserToDeleteRows = false;
            this.dataGridView_transform.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
            | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.dataGridView_transform.BackgroundColor = Color.White;
            this.dataGridView_transform.BorderStyle = BorderStyle.Fixed3D;
            this.dataGridView_transform.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView_transform.EnableHeadersVisualStyles = false;
            this.dataGridView_transform.GridColor = Color.LightGray;
            this.dataGridView_transform.Location = new Point(5, 5);
            this.dataGridView_transform.MinimumSize = new Size(400, 200);
            this.dataGridView_transform.Name = "dataGridView_transform";
            this.dataGridView_transform.ReadOnly = true;
            this.dataGridView_transform.Size = new Size(1102, 353);
            this.dataGridView_transform.TabIndex = 0;

            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new Font("맑은 고딕", 21.75F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.label1.Location = new Point(10, 10);
            this.label1.Name = "label1";
            this.label1.Size = new Size(269, 40);
            this.label1.TabIndex = 1;
            this.label1.Text = "원본 키워드 데이터";

            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new Font("맑은 고딕", 21.75F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.label2.Location = new Point(10, 10);
            this.label2.Name = "label2";
            this.label2.Size = new Size(269, 40);
            this.label2.TabIndex = 3;
            this.label2.Text = "변환 키워드 데이터";

            // 
            // tableLayoutRight
            // 
            this.tableLayoutRight.ColumnCount = 1;
            this.tableLayoutRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.tableLayoutRight.Controls.Add(this.groupBox1, 0, 0);
            this.tableLayoutRight.Controls.Add(this.groupbox2, 0, 1);
            this.tableLayoutRight.Controls.Add(this.groupBox3, 0, 2);
            this.tableLayoutRight.Dock = DockStyle.Fill;
            this.tableLayoutRight.Location = new Point(0, 0);
            this.tableLayoutRight.Name = "tableLayoutRight";
            this.tableLayoutRight.RowCount = 3;
            this.tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
            this.tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            this.tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            this.tableLayoutRight.Size = new Size(742, 997);
            this.tableLayoutRight.TabIndex = 0;

            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.sum_keyword_table);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.decimal_combo);
            this.groupBox1.Dock = DockStyle.Fill;
            this.groupBox1.Font = new Font("맑은 고딕", 15.75F);
            this.groupBox1.Location = new Point(5, 5);
            this.groupBox1.Margin = new Padding(5);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new Size(732, 338);
            this.groupBox1.TabIndex = 25;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "키워드 요약";

            // 
            // sum_keyword_table
            // 
            this.sum_keyword_table.AllowUserToAddRows = false;
            this.sum_keyword_table.AllowUserToDeleteRows = false;
            this.sum_keyword_table.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
            | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.sum_keyword_table.BackgroundColor = Color.White;
            this.sum_keyword_table.BorderStyle = BorderStyle.Fixed3D;
            this.sum_keyword_table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Window;
            dataGridViewCellStyle1.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle1.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.False;
            this.sum_keyword_table.DefaultCellStyle = dataGridViewCellStyle1;
            this.sum_keyword_table.EnableHeadersVisualStyles = false;
            this.sum_keyword_table.GridColor = Color.LightGray;
            this.sum_keyword_table.Location = new Point(10, 80);
            this.sum_keyword_table.MinimumSize = new Size(300, 150);
            this.sum_keyword_table.Name = "sum_keyword_table";
            this.sum_keyword_table.ReadOnly = true;
            this.sum_keyword_table.Size = new Size(712, 248);
            this.sum_keyword_table.TabIndex = 36;
            this.sum_keyword_table.CellClick += dataGridView_modified_CellClick;

            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.label5.Location = new Point(10, 40);
            this.label5.Name = "label5";
            this.label5.Size = new Size(50, 25);
            this.label5.TabIndex = 35;
            this.label5.Text = "단위";

            // 
            // decimal_combo
            // 
            this.decimal_combo.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.decimal_combo.FormattingEnabled = true;
            this.decimal_combo.Items.AddRange(new object[] { "원", "천원", "백만원", "억원" });
            this.decimal_combo.Location = new Point(70, 37);
            this.decimal_combo.Name = "decimal_combo";
            this.decimal_combo.Size = new Size(100, 33);
            this.decimal_combo.TabIndex = 24;

            // 
            // groupbox2
            // 
            this.groupbox2.Controls.Add(this.modified_keyword);
            this.groupbox2.Controls.Add(this.label4);
            this.groupbox2.Controls.Add(this.keyword_search_radio2);
            this.groupbox2.Controls.Add(this.keyword_search_radio1);
            this.groupbox2.Controls.Add(this.keyword_search_combo);
            this.groupbox2.Controls.Add(this.search_keyword);
            this.groupbox2.Controls.Add(this.check_all_keyword_list);
            this.groupbox2.Controls.Add(this.change_keyword);
            this.groupbox2.Controls.Add(this.keyword_search_button);
            this.groupbox2.Controls.Add(this.match_keyword_table);
            this.groupbox2.Dock = DockStyle.Fill;
            this.groupbox2.Font = new Font("맑은 고딕", 15.75F);
            this.groupbox2.Location = new Point(5, 353);
            this.groupbox2.Margin = new Padding(5);
            this.groupbox2.Name = "groupbox2";
            this.groupbox2.Size = new Size(732, 538);
            this.groupbox2.TabIndex = 30;
            this.groupbox2.TabStop = false;
            this.groupbox2.Text = "키워드 변환";

            // 
            // modified_keyword
            // 
            this.modified_keyword.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.modified_keyword.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.modified_keyword.Location = new Point(350, 120);
            this.modified_keyword.Name = "modified_keyword";
            this.modified_keyword.PlaceholderText = "키워드 입력 가능";
            this.modified_keyword.Size = new Size(280, 33);
            this.modified_keyword.TabIndex = 34;

            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.label4.Location = new Point(150, 125);
            this.label4.Name = "label4";
            this.label4.Size = new Size(178, 25);
            this.label4.TabIndex = 30;
            this.label4.Text = "다음 키워드로 변경";

            // 
            // keyword_search_radio2
            // 
            this.keyword_search_radio2.AutoSize = true;
            this.keyword_search_radio2.Font = new Font("맑은 고딕", 14.25F);
            this.keyword_search_radio2.Location = new Point(200, 80);
            this.keyword_search_radio2.Name = "keyword_search_radio2";
            this.keyword_search_radio2.Size = new Size(132, 29);
            this.keyword_search_radio2.TabIndex = 33;
            this.keyword_search_radio2.Text = "키워드 입력";
            this.keyword_search_radio2.UseVisualStyleBackColor = true;
            this.keyword_search_radio2.CheckedChanged += keyword_search_radio2_CheckedChanged;

            // 
            // keyword_search_radio1
            // 
            this.keyword_search_radio1.AutoSize = true;
            this.keyword_search_radio1.Checked = true;
            this.keyword_search_radio1.Font = new Font("맑은 고딕", 14.25F);
            this.keyword_search_radio1.Location = new Point(200, 40);
            this.keyword_search_radio1.Name = "keyword_search_radio1";
            this.keyword_search_radio1.Size = new Size(132, 29);
            this.keyword_search_radio1.TabIndex = 32;
            this.keyword_search_radio1.TabStop = true;
            this.keyword_search_radio1.Text = "키워드 선택";
            this.keyword_search_radio1.UseVisualStyleBackColor = true;
            this.keyword_search_radio1.CheckedChanged += keyword_search_radio1_CheckedChanged;

            // 
            // keyword_search_combo
            // 
            this.keyword_search_combo.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.keyword_search_combo.Font = new Font("맑은 고딕", 14.25F);
            this.keyword_search_combo.FormattingEnabled = true;
            this.keyword_search_combo.Location = new Point(350, 38);
            this.keyword_search_combo.Name = "keyword_search_combo";
            this.keyword_search_combo.Size = new Size(370, 33);
            this.keyword_search_combo.TabIndex = 31;
            this.keyword_search_combo.Text = "키워드 선택";

            // 
            // search_keyword
            // 
            this.search_keyword.Enabled = false;
            this.search_keyword.Font = new Font("맑은 고딕", 14.25F);
            this.search_keyword.Location = new Point(350, 78);
            this.search_keyword.Name = "search_keyword";
            this.search_keyword.PlaceholderText = "키워드 입력";
            this.search_keyword.Size = new Size(220, 33);
            this.search_keyword.TabIndex = 30;
            this.search_keyword.KeyDown += search_keyword_KeyDown;

            // 
            // check_all_keyword_list
            // 
            this.check_all_keyword_list.AutoSize = true;
            this.check_all_keyword_list.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.check_all_keyword_list.Location = new Point(15, 125);
            this.check_all_keyword_list.Name = "check_all_keyword_list";
            this.check_all_keyword_list.Size = new Size(114, 29);
            this.check_all_keyword_list.TabIndex = 29;
            this.check_all_keyword_list.Text = "전체 선택";
            this.check_all_keyword_list.UseVisualStyleBackColor = true;
            this.check_all_keyword_list.CheckedChanged += check_all_keyword_list_CheckedChanged;

            // 
            // change_keyword
            // 
            this.change_keyword.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.change_keyword.AutoSize = true;
            this.change_keyword.Font = new Font("맑은 고딕", 14.25F);
            this.change_keyword.Location = new Point(650, 118);
            this.change_keyword.Name = "change_keyword";
            this.change_keyword.Size = new Size(69, 35);
            this.change_keyword.TabIndex = 26;
            this.change_keyword.Text = "변환";
            this.change_keyword.UseVisualStyleBackColor = true;
            this.change_keyword.Click += change_keyword_Click;

            // 
            // keyword_search_button
            // 
            this.keyword_search_button.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.keyword_search_button.AutoSize = true;
            this.keyword_search_button.Font = new Font("맑은 고딕", 14.25F);
            this.keyword_search_button.Location = new Point(580, 76);
            this.keyword_search_button.Name = "keyword_search_button";
            this.keyword_search_button.Size = new Size(69, 35);
            this.keyword_search_button.TabIndex = 24;
            this.keyword_search_button.Text = "검색";
            this.keyword_search_button.UseVisualStyleBackColor = true;
            this.keyword_search_button.Click += keyword_search_button_Click;

            // 
            // match_keyword_table
            // 
            this.match_keyword_table.AllowUserToAddRows = false;
            this.match_keyword_table.AllowUserToDeleteRows = false;
            this.match_keyword_table.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
            | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.match_keyword_table.BackgroundColor = Color.White;
            this.match_keyword_table.BorderStyle = BorderStyle.Fixed3D;
            this.match_keyword_table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = SystemColors.Window;
            dataGridViewCellStyle2.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle2.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            this.match_keyword_table.DefaultCellStyle = dataGridViewCellStyle2;
            this.match_keyword_table.EnableHeadersVisualStyles = false;
            this.match_keyword_table.GridColor = Color.LightGray;
            this.match_keyword_table.Location = new Point(15, 170);
            this.match_keyword_table.MinimumSize = new Size(300, 200);
            this.match_keyword_table.Name = "match_keyword_table";
            this.match_keyword_table.ReadOnly = true;
            this.match_keyword_table.Size = new Size(704, 358);
            this.match_keyword_table.TabIndex = 23;
            this.match_keyword_table.CellClick += match_keyword_table_CellClick;

            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.prod_col_check);
            this.groupBox3.Controls.Add(this.button5);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.button2);
            this.groupBox3.Controls.Add(this.dept_col_check);
            this.groupBox3.Dock = DockStyle.Fill;
            this.groupBox3.Font = new Font("맑은 고딕", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.groupBox3.Location = new Point(5, 901);
            this.groupBox3.Margin = new Padding(5);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new Size(732, 91);
            this.groupBox3.TabIndex = 36;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Cluster 설정";

            // 
            // prod_col_check
            // 
            this.prod_col_check.AutoSize = true;
            this.prod_col_check.Checked = true;
            this.prod_col_check.CheckState = CheckState.Checked;
            this.prod_col_check.Font = new Font("맑은 고딕", 14.25F);
            this.prod_col_check.Location = new Point(240, 35);
            this.prod_col_check.Name = "prod_col_check";
            this.prod_col_check.Size = new Size(107, 29);
            this.prod_col_check.TabIndex = 29;
            this.prod_col_check.Text = "공급업체";
            this.prod_col_check.UseVisualStyleBackColor = true;
            this.prod_col_check.CheckedChanged += prod_col_check_CheckedChanged;

            // 
            // button5
            // 
            this.button5.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.button5.AutoSize = true;
            this.button5.Font = new Font("맑은 고딕", 14.25F);
            this.button5.Location = new Point(600, 30);
            this.button5.Name = "button5";
            this.button5.Size = new Size(122, 35);
            this.button5.TabIndex = 35;
            this.button5.Text = "완료";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += button5_Click;

            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new Font("맑은 고딕", 14.25F);
            this.label3.Location = new Point(10, 37);
            this.label3.Name = "label3";
            this.label3.Size = new Size(95, 25);
            this.label3.TabIndex = 26;
            this.label3.Text = "필수 포함";

            // 
            // button2
            // 
            this.button2.AutoSize = true;
            this.button2.Font = new Font("맑은 고딕", 14.25F);
            this.button2.Location = new Point(450, 30);
            this.button2.Name = "button2";
            this.button2.Size = new Size(127, 35);
            this.button2.TabIndex = 26;
            this.button2.Text = "Cluster 확인";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += button2_Click;

            // 
            // dept_col_check
            // 
            this.dept_col_check.AutoSize = true;
            this.dept_col_check.Checked = true;
            this.dept_col_check.CheckState = CheckState.Checked;
            this.dept_col_check.Font = new Font("맑은 고딕", 14.25F);
            this.dept_col_check.Location = new Point(115, 35);
            this.dept_col_check.Name = "dept_col_check";
            this.dept_col_check.Size = new Size(126, 29);
            this.dept_col_check.TabIndex = 28;
            this.dept_col_check.Text = "코스트센터";
            this.dept_col_check.UseVisualStyleBackColor = true;
            this.dept_col_check.CheckedChanged += dept_col_check_CheckedChanged;

            // 
            // 페이징 컨트롤들 (상단)
            // 

            // btn_nextPage
            // 
            this.btn_nextPage.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.btn_nextPage.AutoSize = true;
            this.btn_nextPage.Font = new Font("맑은 고딕", 14.25F);
            this.btn_nextPage.Location = new Point(1020, 8);
            this.btn_nextPage.Name = "btn_nextPage";
            this.btn_nextPage.Size = new Size(86, 35);
            this.btn_nextPage.TabIndex = 60;
            this.btn_nextPage.Text = "다음 ▶";
            this.btn_nextPage.UseVisualStyleBackColor = true;

            // 
            // btn_prevPage
            // 
            this.btn_prevPage.AutoSize = true;
            this.btn_prevPage.Font = new Font("맑은 고딕", 14.25F);
            this.btn_prevPage.Location = new Point(450, 7);
            this.btn_prevPage.Name = "btn_prevPage";
            this.btn_prevPage.Size = new Size(86, 35);
            this.btn_prevPage.TabIndex = 59;
            this.btn_prevPage.Text = "◀ 이전";
            this.btn_prevPage.UseVisualStyleBackColor = true;

            // 
            // cmb_pageSize
            // 
            this.cmb_pageSize.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.cmb_pageSize.FormattingEnabled = true;
            this.cmb_pageSize.Location = new Point(135, 10);
            this.cmb_pageSize.Name = "cmb_pageSize";
            this.cmb_pageSize.Size = new Size(121, 33);
            this.cmb_pageSize.TabIndex = 58;

            // 
            // lbl_pageSizeText
            // 
            this.lbl_pageSizeText.AutoSize = true;
            this.lbl_pageSizeText.Font = new Font("맑은 고딕", 14F);
            this.lbl_pageSizeText.Location = new Point(4, 13);
            this.lbl_pageSizeText.Name = "lbl_pageSizeText";
            this.lbl_pageSizeText.Size = new Size(125, 25);
            this.lbl_pageSizeText.TabIndex = 57;
            this.lbl_pageSizeText.Text = "페이지 크기 :";

            // 
            // lbl_pagination
            // 
            this.lbl_pagination.AutoSize = true;
            this.lbl_pagination.Font = new Font("맑은 고딕", 14F);
            this.lbl_pagination.Location = new Point(542, 12);
            this.lbl_pagination.Name = "lbl_pagination";
            this.lbl_pagination.Size = new Size(80, 25);
            this.lbl_pagination.TabIndex = 54;
            this.lbl_pagination.Text = "페이지 :";

            // 
            // num_pageNumber
            // 
            this.num_pageNumber.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.num_pageNumber.Location = new Point(635, 10);
            this.num_pageNumber.Name = "num_pageNumber";
            this.num_pageNumber.Size = new Size(52, 33);
            this.num_pageNumber.TabIndex = 56;

            // 
            // lbl_pagination2
            // 
            this.lbl_pagination2.AutoSize = true;
            this.lbl_pagination2.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.lbl_pagination2.Location = new Point(693, 13);
            this.lbl_pagination2.Name = "lbl_pagination2";
            this.lbl_pagination2.Size = new Size(118, 25);
            this.lbl_pagination2.TabIndex = 55;
            this.lbl_pagination2.Text = "/ 0 (총 0 행)";

            // 
            // 페이징 컨트롤들 (하단)
            // 

            // btn_nextPage2
            // 
            this.btn_nextPage2.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.btn_nextPage2.AutoSize = true;
            this.btn_nextPage2.Font = new Font("맑은 고딕", 14.25F);
            this.btn_nextPage2.Location = new Point(1020, 8);
            this.btn_nextPage2.Name = "btn_nextPage2";
            this.btn_nextPage2.Size = new Size(86, 35);
            this.btn_nextPage2.TabIndex = 67;
            this.btn_nextPage2.Text = "다음 ▶";
            this.btn_nextPage2.UseVisualStyleBackColor = true;

            // 
            // btn_prevPage2
            // 
            this.btn_prevPage2.AutoSize = true;
            this.btn_prevPage2.Font = new Font("맑은 고딕", 14.25F);
            this.btn_prevPage2.Location = new Point(450, 7);
            this.btn_prevPage2.Name = "btn_prevPage2";
            this.btn_prevPage2.Size = new Size(86, 35);
            this.btn_prevPage2.TabIndex = 66;
            this.btn_prevPage2.Text = "◀ 이전";
            this.btn_prevPage2.UseVisualStyleBackColor = true;

            // 
            // cmb_pageSize2
            // 
            this.cmb_pageSize2.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.cmb_pageSize2.FormattingEnabled = true;
            this.cmb_pageSize2.Location = new Point(135, 10);
            this.cmb_pageSize2.Name = "cmb_pageSize2";
            this.cmb_pageSize2.Size = new Size(121, 33);
            this.cmb_pageSize2.TabIndex = 65;

            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new Font("맑은 고딕", 14F);
            this.label6.Location = new Point(4, 13);
            this.label6.Name = "label6";
            this.label6.Size = new Size(125, 25);
            this.label6.TabIndex = 64;
            this.label6.Text = "페이지 크기 :";

            // 
            // lbl_pagination3
            // 
            this.lbl_pagination3.AutoSize = true;
            this.lbl_pagination3.Font = new Font("맑은 고딕", 14F);
            this.lbl_pagination3.Location = new Point(542, 12);
            this.lbl_pagination3.Name = "lbl_pagination3";
            this.lbl_pagination3.Size = new Size(80, 25);
            this.lbl_pagination3.TabIndex = 61;
            this.lbl_pagination3.Text = "페이지 :";

            // 
            // num_pageNumber2
            // 
            this.num_pageNumber2.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.num_pageNumber2.Location = new Point(635, 10);
            this.num_pageNumber2.Name = "num_pageNumber2";
            this.num_pageNumber2.Size = new Size(52, 33);
            this.num_pageNumber2.TabIndex = 63;

            // 
            // lbl_pagination4
            // 
            this.lbl_pagination4.AutoSize = true;
            this.lbl_pagination4.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.lbl_pagination4.Location = new Point(693, 13);
            this.lbl_pagination4.Name = "lbl_pagination4";
            this.lbl_pagination4.Size = new Size(118, 25);
            this.lbl_pagination4.TabIndex = 62;
            this.lbl_pagination4.Text = "/ 0 (총 0 행)";

            // 
            // uc_DataTransform
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.White;
            this.Controls.Add(this.tableLayoutMain);
            this.MinimumSize = new Size(1280, 800);
            //this.MinimumSize = new Size(1904, 1017);
            this.Name = "uc_DataTransform";
            this.Size = new Size(1904, 1017);
            //this.Dock = DockStyle.Fill;           // 추가
            //this.MinimumSize = new Size(1280, 800);  // ← 이거만 유지

            // 컨트롤 정리
            this.tableLayoutMain.ResumeLayout(false);
            this.pnl_left.ResumeLayout(false);
            this.pnl_right.ResumeLayout(false);
            this.tableLayoutLeft.ResumeLayout(false);
            this.tableLayoutRight.ResumeLayout(false);
            this.pnl_original_data.ResumeLayout(false);
            this.pnl_transform_data.ResumeLayout(false);
            this.pnl_original_header.ResumeLayout(false);
            this.pnl_original_header.PerformLayout();
            this.pnl_transform_header.ResumeLayout(false);
            this.pnl_transform_header.PerformLayout();
            this.pnl_original_pagination.ResumeLayout(false);
            this.pnl_original_pagination.PerformLayout();
            this.pnl_transform_pagination.ResumeLayout(false);
            this.pnl_transform_pagination.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupbox2.ResumeLayout(false);
            this.groupbox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_2nd)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_transform)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sum_keyword_table)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.match_keyword_table)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_pageNumber)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_pageNumber2)).EndInit();
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
        private Panel pnl_original_data;
        private Panel pnl_transform_data;
        private Panel pnl_original_header;
        private Panel pnl_transform_header;
        private Panel pnl_original_pagination;
        private Panel pnl_transform_pagination;

        // 기존 컨트롤들
        private DataGridView dataGridView_2nd;
        private Label label1;
        private Label label2;
        private DataGridView dataGridView_transform;
        private GroupBox groupBox1;
        private CheckBox prod_col_check;
        private CheckBox dept_col_check;
        private Label label3;
        private Button button2;
        private GroupBox groupbox2;
        private CheckBox check_all_keyword_list;
        private Button change_keyword;
        private Button keyword_search_button;
        private DataGridView match_keyword_table;
        private TextBox modified_keyword;
        private Label label4;
        private RadioButton keyword_search_radio2;
        private RadioButton keyword_search_radio1;
        private ComboBox keyword_search_combo;
        private TextBox search_keyword;
        private Button button5;
        private Label label5;
        private ComboBox decimal_combo;
        private GroupBox groupBox3;
        private DataGridView sum_keyword_table;
        private Button btn_nextPage;
        private Button btn_prevPage;
        private ComboBox cmb_pageSize;
        private Label lbl_pageSizeText;
        private Label lbl_pagination;
        private NumericUpDown num_pageNumber;
        private Label lbl_pagination2;
        private Button btn_nextPage2;
        private Button btn_prevPage2;
        private ComboBox cmb_pageSize2;
        private Label label6;
        private Label lbl_pagination3;
        private NumericUpDown num_pageNumber2;
        private Label lbl_pagination4;
    }
}
