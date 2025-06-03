namespace FinanceTool
{
    partial class uc_Clustering
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
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();

            // 메인 컨테이너
            this.tableLayoutMain = new TableLayoutPanel();
            this.pnl_left = new Panel();
            this.pnl_right = new Panel();

            // 좌측 패널 컨트롤들
            this.groupBox1 = new GroupBox();
            this.tableLayoutLeft = new TableLayoutPanel();
            this.pnl_merge_header = new Panel();
            this.pnl_merge_controls = new Panel();
            this.pnl_merge_data = new Panel();
            this.pnl_merge_pagination = new Panel();

            // 우측 패널 컨트롤들
            this.tableLayoutRight = new TableLayoutPanel();
            this.groupBox3 = new GroupBox();
            this.groupbox2 = new GroupBox();

            // GroupBox1 컨트롤들 (클러스터링 병합)
            this.groupBox5 = new GroupBox();
            this.keyword_radio1 = new RadioButton();
            this.keyword_radio2 = new RadioButton();
            this.uncluster_count_money = new Label();
            this.label4 = new Label();
            this.label3 = new Label();
            this.label2 = new Label();
            this.excep_search_checkbox = new CheckBox();
            this.equal_search_checkbox = new CheckBox();
            this.uncluster_count = new Label();
            this.except_keyword = new TextBox();
            this.cluster_count = new Label();
            this.merge_addon_btn = new Button();
            this.merge_search_radio2 = new RadioButton();
            this.label5 = new Label();
            this.decimal_combo = new ComboBox();
            this.merge_search_radio1 = new RadioButton();
            this.merge_keyword_combo = new ComboBox();
            this.merge_search_keyword = new TextBox();
            this.merge_all_check = new CheckBox();
            this.button1 = new Button();
            this.merge_search_button = new Button();
            this.merge_cluster_table = new DataGridView();

            // 페이징 컨트롤들
            this.btn_nextPage = new Button();
            this.btn_prevPage = new Button();
            this.cmb_pageSize = new ComboBox();
            this.lbl_pageSizeText = new Label();
            this.lbl_pagination = new Label();
            this.num_pageNumber = new NumericUpDown();
            this.lbl_pagination2 = new Label();

            // GroupBox3 컨트롤들 (검색 키워드 추천)
            this.label1 = new Label();
            this.groupBox4 = new GroupBox();
            this.dataGridView_recoman_keyword = new DataGridView();
            this.new_reco_word = new TextBox();
            this.reco_del_btn = new Button();
            this.reco_add_btn = new Button();
            this.gb_separator = new GroupBox();
            this.dataGridView_lv1 = new DataGridView();
            this.new_lv1_word = new TextBox();
            this.lv1_del_btn = new Button();
            this.lv1_add_btn = new Button();
            this.dataGridView_modified = new DataGridView();

            // GroupBox2 컨트롤들 (클러스터링 병합 결과 확인)
            this.button2 = new Button();
            this.union_cluster_btn = new Button();
            this.label7 = new Label();
            this.complete_btn = new Button();
            this.check_search_radio2 = new RadioButton();
            this.check_search_radio1 = new RadioButton();
            this.check_search_combo = new ComboBox();
            this.check_search_keyword = new TextBox();
            this.merge_cancel_button = new Button();
            this.check_search_button = new Button();
            this.merge_check_table = new DataGridView();

            // 컨트롤 초기화 시작
            this.tableLayoutMain.SuspendLayout();
            this.pnl_left.SuspendLayout();
            this.pnl_right.SuspendLayout();
            this.tableLayoutLeft.SuspendLayout();
            this.tableLayoutRight.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupbox2.SuspendLayout();
            this.pnl_merge_header.SuspendLayout();
            this.pnl_merge_controls.SuspendLayout();
            this.pnl_merge_data.SuspendLayout();
            this.pnl_merge_pagination.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.gb_separator.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.merge_cluster_table)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_pageNumber)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_recoman_keyword)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_lv1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_modified)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.merge_check_table)).BeginInit();
            this.SuspendLayout();

            // 
            // tableLayoutMain
            // 
            this.tableLayoutMain.ColumnCount = 2;
            // 수정: 100% : 450px 고정
            this.tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 450F));
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
            this.pnl_left.Controls.Add(this.groupBox1);
            this.pnl_left.Dock = DockStyle.Fill;
            this.pnl_left.Location = new Point(10, 10);
            this.pnl_left.Margin = new Padding(10);
            this.pnl_left.Name = "pnl_left";
            this.pnl_left.Size = new Size(1312, 997);
            this.pnl_left.TabIndex = 0;

            // 
            // pnl_right
            // 
            this.pnl_right.Controls.Add(this.tableLayoutRight);
            this.pnl_right.Dock = DockStyle.Fill;
            this.pnl_right.Location = new Point(1342, 10);
            this.pnl_right.Margin = new Padding(10);
            this.pnl_right.Name = "pnl_right";
            this.pnl_right.Size = new Size(552, 997);
            this.pnl_right.TabIndex = 1;

            // 
            // groupBox1 (클러스터링 병합)
            // 
            this.groupBox1.Controls.Add(this.tableLayoutLeft);
            this.groupBox1.Dock = DockStyle.Fill;
            this.groupBox1.Font = new Font("맑은 고딕", 15.75F);
            this.groupBox1.Location = new Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new Size(1312, 997);
            this.groupBox1.TabIndex = 42;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Clustering 병합";

            // 
            // tableLayoutLeft
            // 
            this.tableLayoutLeft.ColumnCount = 1;
            this.tableLayoutLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.tableLayoutLeft.Controls.Add(this.pnl_merge_header, 0, 0);
            this.tableLayoutLeft.Controls.Add(this.pnl_merge_controls, 0, 1);
            this.tableLayoutLeft.Controls.Add(this.pnl_merge_data, 0, 2);
            this.tableLayoutLeft.Controls.Add(this.pnl_merge_pagination, 0, 3);
            this.tableLayoutLeft.Dock = DockStyle.Fill;
            this.tableLayoutLeft.Location = new Point(3, 32);
            this.tableLayoutLeft.Name = "tableLayoutLeft";
            this.tableLayoutLeft.RowCount = 4;
            this.tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
            this.tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
            this.tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            this.tableLayoutLeft.Size = new Size(1306, 962);
            this.tableLayoutLeft.TabIndex = 0;

            // 
            // pnl_merge_header
            // 
            this.pnl_merge_header.Controls.Add(this.groupBox5);
            this.pnl_merge_header.Controls.Add(this.uncluster_count_money);
            this.pnl_merge_header.Controls.Add(this.label4);
            this.pnl_merge_header.Controls.Add(this.label3);
            this.pnl_merge_header.Controls.Add(this.label2);
            this.pnl_merge_header.Controls.Add(this.uncluster_count);
            this.pnl_merge_header.Controls.Add(this.cluster_count);
            this.pnl_merge_header.Dock = DockStyle.Fill;
            this.pnl_merge_header.Location = new Point(0, 0);
            this.pnl_merge_header.Name = "pnl_merge_header";
            this.pnl_merge_header.Size = new Size(1306, 80);
            this.pnl_merge_header.TabIndex = 0;

            // 
            // pnl_merge_controls
            // 
            this.pnl_merge_controls.Controls.Add(this.excep_search_checkbox);
            this.pnl_merge_controls.Controls.Add(this.equal_search_checkbox);
            this.pnl_merge_controls.Controls.Add(this.except_keyword);
            this.pnl_merge_controls.Controls.Add(this.merge_addon_btn);
            this.pnl_merge_controls.Controls.Add(this.merge_search_radio2);
            this.pnl_merge_controls.Controls.Add(this.label5);
            this.pnl_merge_controls.Controls.Add(this.decimal_combo);
            this.pnl_merge_controls.Controls.Add(this.merge_search_radio1);
            this.pnl_merge_controls.Controls.Add(this.merge_keyword_combo);
            this.pnl_merge_controls.Controls.Add(this.merge_search_keyword);
            this.pnl_merge_controls.Controls.Add(this.merge_all_check);
            this.pnl_merge_controls.Controls.Add(this.button1);
            this.pnl_merge_controls.Controls.Add(this.merge_search_button);
            this.pnl_merge_controls.Dock = DockStyle.Fill;
            this.pnl_merge_controls.Location = new Point(0, 80);
            this.pnl_merge_controls.Name = "pnl_merge_controls";
            this.pnl_merge_controls.Size = new Size(1306, 120);
            this.pnl_merge_controls.TabIndex = 1;

            // 
            // pnl_merge_data
            // 
            this.pnl_merge_data.Controls.Add(this.merge_cluster_table);
            this.pnl_merge_data.Dock = DockStyle.Fill;
            this.pnl_merge_data.Location = new Point(5, 205);
            this.pnl_merge_data.Margin = new Padding(5);
            this.pnl_merge_data.Name = "pnl_merge_data";
            this.pnl_merge_data.Size = new Size(1296, 702);
            this.pnl_merge_data.TabIndex = 2;

            // 
            // pnl_merge_pagination
            // 
            this.pnl_merge_pagination.Controls.Add(this.btn_nextPage);
            this.pnl_merge_pagination.Controls.Add(this.btn_prevPage);
            this.pnl_merge_pagination.Controls.Add(this.cmb_pageSize);
            this.pnl_merge_pagination.Controls.Add(this.lbl_pageSizeText);
            this.pnl_merge_pagination.Controls.Add(this.lbl_pagination);
            this.pnl_merge_pagination.Controls.Add(this.num_pageNumber);
            this.pnl_merge_pagination.Controls.Add(this.lbl_pagination2);
            this.pnl_merge_pagination.Dock = DockStyle.Fill;
            this.pnl_merge_pagination.Location = new Point(0, 912);
            this.pnl_merge_pagination.Name = "pnl_merge_pagination";
            this.pnl_merge_pagination.Size = new Size(1306, 50);
            this.pnl_merge_pagination.TabIndex = 3;

            // 
            // tableLayoutRight
            // 
            this.tableLayoutRight.ColumnCount = 1;
            this.tableLayoutRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.tableLayoutRight.Controls.Add(this.groupBox3, 0, 0);
            this.tableLayoutRight.Controls.Add(this.groupbox2, 0, 1);
            this.tableLayoutRight.Dock = DockStyle.Fill;
            this.tableLayoutRight.Location = new Point(0, 0);
            this.tableLayoutRight.Name = "tableLayoutRight";
            this.tableLayoutRight.RowCount = 2;
            this.tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.tableLayoutRight.Size = new Size(552, 997);
            this.tableLayoutRight.TabIndex = 0;

            // 
            // groupBox3 (검색 키워드 추천)
            // 
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.groupBox4);
            this.groupBox3.Controls.Add(this.gb_separator);
            this.groupBox3.Controls.Add(this.dataGridView_modified);
            this.groupBox3.Dock = DockStyle.Fill;
            this.groupBox3.Font = new Font("맑은 고딕", 15.75F);
            this.groupBox3.Location = new Point(5, 5);
            this.groupBox3.Margin = new Padding(5);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new Size(542, 488);
            this.groupBox3.TabIndex = 43;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "검색 키워드 추천";

            // 
            // groupbox2 (클러스터링 병합 결과 확인)
            // 
            this.groupbox2.Controls.Add(this.button2);
            this.groupbox2.Controls.Add(this.union_cluster_btn);
            this.groupbox2.Controls.Add(this.label7);
            this.groupbox2.Controls.Add(this.complete_btn);
            this.groupbox2.Controls.Add(this.check_search_radio2);
            this.groupbox2.Controls.Add(this.check_search_radio1);
            this.groupbox2.Controls.Add(this.check_search_combo);
            this.groupbox2.Controls.Add(this.check_search_keyword);
            this.groupbox2.Controls.Add(this.merge_cancel_button);
            this.groupbox2.Controls.Add(this.check_search_button);
            this.groupbox2.Controls.Add(this.merge_check_table);
            this.groupbox2.Dock = DockStyle.Fill;
            this.groupbox2.Font = new Font("맑은 고딕", 15.75F);
            this.groupbox2.Location = new Point(5, 503);
            this.groupbox2.Margin = new Padding(5);
            this.groupbox2.Name = "groupbox2";
            this.groupbox2.Size = new Size(542, 489);
            this.groupbox2.TabIndex = 44;
            this.groupbox2.TabStop = false;
            this.groupbox2.Text = "Clustering 병합 결과 확인";

            // 
            // merge_cluster_table
            // 
            this.merge_cluster_table.AllowUserToAddRows = false;
            this.merge_cluster_table.AllowUserToDeleteRows = false;
            this.merge_cluster_table.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
            | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.merge_cluster_table.BackgroundColor = Color.White;
            this.merge_cluster_table.BorderStyle = BorderStyle.Fixed3D;
            this.merge_cluster_table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.merge_cluster_table.EnableHeadersVisualStyles = false;
            this.merge_cluster_table.GridColor = Color.LightGray;
            this.merge_cluster_table.Location = new Point(5, 5);
            this.merge_cluster_table.MinimumSize = new Size(500, 300);
            this.merge_cluster_table.Name = "merge_cluster_table";
            this.merge_cluster_table.ReadOnly = true;
            this.merge_cluster_table.Size = new Size(1286, 692);
            this.merge_cluster_table.TabIndex = 34;
            this.merge_cluster_table.CellContentClick += merge_cluster_table_CellContentClick;

            // 헤더 영역 컨트롤들
            //

            // groupBox5
            // 
            this.groupBox5.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.groupBox5.Controls.Add(this.keyword_radio1);
            this.groupBox5.Controls.Add(this.keyword_radio2);
            this.groupBox5.Location = new Point(850, 5);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new Size(300, 40);
            this.groupBox5.TabIndex = 54;
            this.groupBox5.TabStop = false;

            // 
            // keyword_radio1
            // 
            this.keyword_radio1.AutoSize = true;
            this.keyword_radio1.Checked = true;
            this.keyword_radio1.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.keyword_radio1.Location = new Point(6, 8);
            this.keyword_radio1.Name = "keyword_radio1";
            this.keyword_radio1.Size = new Size(132, 29);
            this.keyword_radio1.TabIndex = 52;
            this.keyword_radio1.TabStop = true;
            this.keyword_radio1.Text = "키워드 검색";
            this.keyword_radio1.UseVisualStyleBackColor = true;
            this.keyword_radio1.CheckedChanged += keyword_radio1_CheckedChanged;

            // 
            // keyword_radio2
            // 
            this.keyword_radio2.AutoSize = true;
            this.keyword_radio2.Font = new Font("맑은 고딕", 14.25F);
            this.keyword_radio2.Location = new Point(144, 8);
            this.keyword_radio2.Name = "keyword_radio2";
            this.keyword_radio2.Size = new Size(151, 29);
            this.keyword_radio2.TabIndex = 53;
            this.keyword_radio2.Text = "공급업체 검색";
            this.keyword_radio2.UseVisualStyleBackColor = true;

            // 
            // uncluster_count_money
            // 
            this.uncluster_count_money.AutoSize = true;
            this.uncluster_count_money.Location = new Point(10, 50);
            this.uncluster_count_money.Name = "uncluster_count_money";
            this.uncluster_count_money.Size = new Size(179, 30);
            this.uncluster_count_money.TabIndex = 51;
            this.uncluster_count_money.Text = "미병합 합산금액 :";

            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new Font("맑은 고딕", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            this.label4.ForeColor = Color.IndianRed;
            this.label4.Location = new Point(480, 33);
            this.label4.Name = "label4";
            this.label4.Size = new Size(140, 17);
            this.label4.TabIndex = 50;
            this.label4.Text = "   추가할 수 있습니다.";

            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new Font("맑은 고딕", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            this.label3.ForeColor = Color.IndianRed;
            this.label3.Location = new Point(480, 8);
            this.label3.Name = "label3";
            this.label3.Size = new Size(331, 17);
            this.label3.TabIndex = 49;
            this.label3.Text = "※ 제외 항목 입력 시 , 를 활용하여 제외 키워드 항목을";

            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new Font("맑은 고딕", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            this.label2.ForeColor = Color.IndianRed;
            this.label2.Location = new Point(350, 60);
            this.label2.Name = "label2";
            this.label2.Size = new Size(345, 17);
            this.label2.TabIndex = 48;
            this.label2.Text = "※ 검색어 입력 시 , 를 활용하여 AND 검색이 가능합니다.";

            // 
            // uncluster_count
            // 
            this.uncluster_count.AutoSize = true;
            this.uncluster_count.Location = new Point(10, 28);
            this.uncluster_count.Name = "uncluster_count";
            this.uncluster_count.Size = new Size(159, 30);
            this.uncluster_count.TabIndex = 46;
            this.uncluster_count.Text = "미병합 Cluster :";

            // 
            // cluster_count
            // 
            this.cluster_count.AutoSize = true;
            this.cluster_count.Location = new Point(10, 5);
            this.cluster_count.Name = "cluster_count";
            this.cluster_count.Size = new Size(74, 30);
            this.cluster_count.TabIndex = 43;
            this.cluster_count.Text = "행 수 :";

            // 컨트롤 영역 컨트롤들
            //

            // excep_search_checkbox
            // 
            this.excep_search_checkbox.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.excep_search_checkbox.AutoSize = true;
            this.excep_search_checkbox.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.excep_search_checkbox.Location = new Point(900, 85);
            this.excep_search_checkbox.Name = "excep_search_checkbox";
            this.excep_search_checkbox.Size = new Size(170, 29);
            this.excep_search_checkbox.TabIndex = 48;
            this.excep_search_checkbox.Text = "검색 제외 항목 :";
            this.excep_search_checkbox.UseVisualStyleBackColor = true;
            this.excep_search_checkbox.CheckedChanged += excep_search_checkbox_CheckedChanged;

            // 
            // equal_search_checkbox
            // 
            this.equal_search_checkbox.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.equal_search_checkbox.AutoSize = true;
            this.equal_search_checkbox.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.equal_search_checkbox.Location = new Point(700, 85);
            this.equal_search_checkbox.Name = "equal_search_checkbox";
            this.equal_search_checkbox.Size = new Size(204, 29);
            this.equal_search_checkbox.TabIndex = 47;
            this.equal_search_checkbox.Text = "검색 조건 완전 일치";
            this.equal_search_checkbox.UseVisualStyleBackColor = true;
            this.equal_search_checkbox.CheckedChanged += equal_search_checkbox_CheckedChanged;

            // 
            // except_keyword
            // 
            this.except_keyword.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.except_keyword.Enabled = false;
            this.except_keyword.Font = new Font("맑은 고딕", 14.25F);
            this.except_keyword.Location = new Point(1080, 83);
            this.except_keyword.Name = "except_keyword";
            this.except_keyword.PlaceholderText = "제외 항목 입력";
            this.except_keyword.Size = new Size(200, 33);
            this.except_keyword.TabIndex = 45;
            this.except_keyword.KeyDown += except_keyword_KeyDown;

            // 
            // merge_addon_btn
            // 
            this.merge_addon_btn.AutoSize = true;
            this.merge_addon_btn.Font = new Font("맑은 고딕", 14.25F);
            this.merge_addon_btn.Location = new Point(220, 83);
            this.merge_addon_btn.Name = "merge_addon_btn";
            this.merge_addon_btn.Size = new Size(122, 35);
            this.merge_addon_btn.TabIndex = 42;
            this.merge_addon_btn.Text = "추가 병합";
            this.merge_addon_btn.UseVisualStyleBackColor = true;
            this.merge_addon_btn.Click += merge_addon_btn_Click;

            // 
            // merge_search_radio2
            // 
            this.merge_search_radio2.AutoSize = true;
            this.merge_search_radio2.Font = new Font("맑은 고딕", 14.25F);
            this.merge_search_radio2.Location = new Point(700, 45);
            this.merge_search_radio2.Name = "merge_search_radio2";
            this.merge_search_radio2.Size = new Size(132, 29);
            this.merge_search_radio2.TabIndex = 41;
            this.merge_search_radio2.Text = "검색어 입력";
            this.merge_search_radio2.UseVisualStyleBackColor = true;
            this.merge_search_radio2.CheckedChanged += merge_search_radio2_CheckedChanged;

            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.label5.Location = new Point(350, 88);
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
            this.decimal_combo.Location = new Point(406, 85);
            this.decimal_combo.Name = "decimal_combo";
            this.decimal_combo.Size = new Size(80, 33);
            this.decimal_combo.TabIndex = 24;
            this.decimal_combo.SelectedIndexChanged += decimal_combo_SelectedIndexChanged;

            // 
            // merge_search_radio1
            // 
            this.merge_search_radio1.AutoSize = true;
            this.merge_search_radio1.Checked = true;
            this.merge_search_radio1.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.merge_search_radio1.Location = new Point(700, 10);
            this.merge_search_radio1.Name = "merge_search_radio1";
            this.merge_search_radio1.Size = new Size(132, 29);
            this.merge_search_radio1.TabIndex = 40;
            this.merge_search_radio1.TabStop = true;
            this.merge_search_radio1.Text = "검색어 선택";
            this.merge_search_radio1.UseVisualStyleBackColor = true;
            this.merge_search_radio1.CheckedChanged += merge_search_radio1_CheckedChanged;

            // 
            // merge_keyword_combo
            // 
            this.merge_keyword_combo.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.merge_keyword_combo.Font = new Font("맑은 고딕", 14.25F);
            this.merge_keyword_combo.FormattingEnabled = true;
            this.merge_keyword_combo.Location = new Point(840, 8);
            this.merge_keyword_combo.Name = "merge_keyword_combo";
            this.merge_keyword_combo.Size = new Size(400, 33);
            this.merge_keyword_combo.TabIndex = 39;
            this.merge_keyword_combo.Text = "검색어 선택";

            // 
            // merge_search_keyword
            // 
            this.merge_search_keyword.Enabled = false;
            this.merge_search_keyword.Font = new Font("맑은 고딕", 14.25F);
            this.merge_search_keyword.Location = new Point(840, 43);
            this.merge_search_keyword.Name = "merge_search_keyword";
            this.merge_search_keyword.PlaceholderText = "검색 키워드 입력";
            this.merge_search_keyword.Size = new Size(300, 33);
            this.merge_search_keyword.TabIndex = 38;
            this.merge_search_keyword.KeyDown += merge_search_keyword_KeyDown;

            // 
            // merge_all_check
            // 
            this.merge_all_check.AutoSize = true;
            this.merge_all_check.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.merge_all_check.Location = new Point(10, 88);
            this.merge_all_check.Name = "merge_all_check";
            this.merge_all_check.Size = new Size(114, 29);
            this.merge_all_check.TabIndex = 37;
            this.merge_all_check.Text = "전체 선택";
            this.merge_all_check.UseVisualStyleBackColor = true;
            this.merge_all_check.CheckedChanged += merge_all_check_CheckedChanged;

            // 
            // button1
            // 
            this.button1.AutoSize = true;
            this.button1.Font = new Font("맑은 고딕", 14.25F);
            this.button1.Location = new Point(130, 83);
            this.button1.Name = "button1";
            this.button1.Size = new Size(63, 35);
            this.button1.TabIndex = 36;
            this.button1.Text = "병합";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += button1_Click;

            // 
            // merge_search_button
            // 
            this.merge_search_button.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.merge_search_button.AutoSize = true;
            this.merge_search_button.Font = new Font("맑은 고딕", 14.25F);
            this.merge_search_button.Location = new Point(1150, 41);
            this.merge_search_button.Name = "merge_search_button";
            this.merge_search_button.Size = new Size(63, 35);
            this.merge_search_button.TabIndex = 35;
            this.merge_search_button.Text = "검색";
            this.merge_search_button.UseVisualStyleBackColor = true;
            this.merge_search_button.Click += merge_search_button_Click;

            // 페이징 컨트롤들
            //

            // btn_nextPage
            // 
            this.btn_nextPage.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.btn_nextPage.AutoSize = true;
            this.btn_nextPage.Font = new Font("맑은 고딕", 14.25F);
            this.btn_nextPage.Location = new Point(1200, 8);
            this.btn_nextPage.Name = "btn_nextPage";
            this.btn_nextPage.Size = new Size(86, 35);
            this.btn_nextPage.TabIndex = 67;
            this.btn_nextPage.Text = "다음 ▶";
            this.btn_nextPage.UseVisualStyleBackColor = true;

            // 
            // btn_prevPage
            // 
            this.btn_prevPage.AutoSize = true;
            this.btn_prevPage.Font = new Font("맑은 고딕", 14.25F);
            this.btn_prevPage.Location = new Point(500, 7);
            this.btn_prevPage.Name = "btn_prevPage";
            this.btn_prevPage.Size = new Size(86, 35);
            this.btn_prevPage.TabIndex = 66;
            this.btn_prevPage.Text = "◀ 이전";
            this.btn_prevPage.UseVisualStyleBackColor = true;

            // 
            // cmb_pageSize
            // 
            this.cmb_pageSize.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.cmb_pageSize.FormattingEnabled = true;
            this.cmb_pageSize.Location = new Point(140, 10);
            this.cmb_pageSize.Name = "cmb_pageSize";
            this.cmb_pageSize.Size = new Size(121, 33);
            this.cmb_pageSize.TabIndex = 65;

            // 
            // lbl_pageSizeText
            // 
            this.lbl_pageSizeText.AutoSize = true;
            this.lbl_pageSizeText.Font = new Font("맑은 고딕", 14F);
            this.lbl_pageSizeText.Location = new Point(10, 13);
            this.lbl_pageSizeText.Name = "lbl_pageSizeText";
            this.lbl_pageSizeText.Size = new Size(125, 25);
            this.lbl_pageSizeText.TabIndex = 64;
            this.lbl_pageSizeText.Text = "페이지 크기 :";

            // 
            // lbl_pagination
            // 
            this.lbl_pagination.AutoSize = true;
            this.lbl_pagination.Font = new Font("맑은 고딕", 14F);
            this.lbl_pagination.Location = new Point(592, 12);
            this.lbl_pagination.Name = "lbl_pagination";
            this.lbl_pagination.Size = new Size(80, 25);
            this.lbl_pagination.TabIndex = 61;
            this.lbl_pagination.Text = "페이지 :";

            // 
            // num_pageNumber
            // 
            this.num_pageNumber.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.num_pageNumber.Location = new Point(685, 10);
            this.num_pageNumber.Name = "num_pageNumber";
            this.num_pageNumber.Size = new Size(52, 33);
            this.num_pageNumber.TabIndex = 63;

            // 
            // lbl_pagination2
            // 
            this.lbl_pagination2.AutoSize = true;
            this.lbl_pagination2.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.lbl_pagination2.Location = new Point(743, 13);
            this.lbl_pagination2.Name = "lbl_pagination2";
            this.lbl_pagination2.Size = new Size(118, 25);
            this.lbl_pagination2.TabIndex = 62;
            this.lbl_pagination2.Text = "/ 0 (총 0 행)";

            // GroupBox3 컨트롤들 (검색 키워드 추천)
            //

            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new Font("맑은 고딕", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            this.label1.Location = new Point(10, 35);
            this.label1.Name = "label1";
            this.label1.Size = new Size(159, 25);
            this.label1.TabIndex = 46;
            this.label1.Text = "상위 키워드 목록";

            // 
            // dataGridView_modified
            // 
            this.dataGridView_modified.AllowUserToAddRows = false;
            this.dataGridView_modified.AllowUserToDeleteRows = false;
            this.dataGridView_modified.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.dataGridView_modified.BackgroundColor = Color.White;
            this.dataGridView_modified.BorderStyle = BorderStyle.Fixed3D;
            this.dataGridView_modified.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView_modified.EnableHeadersVisualStyles = false;
            this.dataGridView_modified.GridColor = Color.LightGray;
            this.dataGridView_modified.Location = new Point(10, 65);
            this.dataGridView_modified.MinimumSize = new Size(300, 100);
            this.dataGridView_modified.Name = "dataGridView_modified";
            this.dataGridView_modified.ReadOnly = true;
            this.dataGridView_modified.Size = new Size(522, 140);
            this.dataGridView_modified.TabIndex = 23;

            // 
            // gb_separator
            // 
            this.gb_separator.Controls.Add(this.dataGridView_lv1);
            this.gb_separator.Controls.Add(this.new_lv1_word);
            this.gb_separator.Controls.Add(this.lv1_del_btn);
            this.gb_separator.Controls.Add(this.lv1_add_btn);
            this.gb_separator.Font = new Font("맑은 고딕", 16F);
            this.gb_separator.Location = new Point(10, 215);
            this.gb_separator.Name = "gb_separator";
            this.gb_separator.Size = new Size(250, 265);
            this.gb_separator.TabIndex = 36;
            this.gb_separator.TabStop = false;
            this.gb_separator.Text = "Lv1 선택";

            // 
            // dataGridView_lv1
            // 
            this.dataGridView_lv1.AllowUserToAddRows = false;
            this.dataGridView_lv1.AllowUserToDeleteRows = false;
            this.dataGridView_lv1.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
            | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.dataGridView_lv1.BackgroundColor = Color.White;
            this.dataGridView_lv1.BorderStyle = BorderStyle.Fixed3D;
            this.dataGridView_lv1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = SystemColors.Window;
            dataGridViewCellStyle5.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle5.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle5.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = DataGridViewTriState.False;
            this.dataGridView_lv1.DefaultCellStyle = dataGridViewCellStyle5;
            this.dataGridView_lv1.EnableHeadersVisualStyles = false;
            this.dataGridView_lv1.GridColor = Color.LightGray;
            this.dataGridView_lv1.Location = new Point(10, 75);
            this.dataGridView_lv1.MinimumSize = new Size(200, 120);
            this.dataGridView_lv1.Name = "dataGridView_lv1";
            this.dataGridView_lv1.Size = new Size(230, 180);
            this.dataGridView_lv1.TabIndex = 43;

            // 
            // new_lv1_word
            // 
            this.new_lv1_word.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.new_lv1_word.Location = new Point(10, 35);
            this.new_lv1_word.Name = "new_lv1_word";
            this.new_lv1_word.PlaceholderText = "신규 항목 입력";
            this.new_lv1_word.Size = new Size(130, 33);
            this.new_lv1_word.TabIndex = 27;
            this.new_lv1_word.KeyDown += new_lv1_word_KeyDown;

            // 
            // lv1_del_btn
            // 
            this.lv1_del_btn.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.lv1_del_btn.AutoSize = true;
            this.lv1_del_btn.Font = new Font("맑은 고딕", 14.25F);
            this.lv1_del_btn.Location = new Point(180, 33);
            this.lv1_del_btn.Name = "lv1_del_btn";
            this.lv1_del_btn.Size = new Size(60, 35);
            this.lv1_del_btn.TabIndex = 24;
            this.lv1_del_btn.Text = "제거";
            this.lv1_del_btn.UseVisualStyleBackColor = true;
            this.lv1_del_btn.Click += lv1_del_btn_Click;

            // 
            // lv1_add_btn
            // 
            this.lv1_add_btn.AutoSize = true;
            this.lv1_add_btn.Font = new Font("맑은 고딕", 14.25F);
            this.lv1_add_btn.Location = new Point(145, 33);
            this.lv1_add_btn.Name = "lv1_add_btn";
            this.lv1_add_btn.Size = new Size(60, 35);
            this.lv1_add_btn.TabIndex = 23;
            this.lv1_add_btn.Text = "추가";
            this.lv1_add_btn.UseVisualStyleBackColor = true;
            this.lv1_add_btn.Click += lv1_add_btn_Click;

            // 
            // groupBox4
            // 
            this.groupBox4.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.groupBox4.Controls.Add(this.dataGridView_recoman_keyword);
            this.groupBox4.Controls.Add(this.new_reco_word);
            this.groupBox4.Controls.Add(this.reco_del_btn);
            this.groupBox4.Controls.Add(this.reco_add_btn);
            this.groupBox4.Font = new Font("맑은 고딕", 16F);
            this.groupBox4.Location = new Point(270, 215);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new Size(262, 265);
            this.groupBox4.TabIndex = 45;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "추천 키워드 선택";

            // 
            // dataGridView_recoman_keyword
            // 
            this.dataGridView_recoman_keyword.AllowUserToAddRows = false;
            this.dataGridView_recoman_keyword.AllowUserToDeleteRows = false;
            this.dataGridView_recoman_keyword.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
            | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.dataGridView_recoman_keyword.BackgroundColor = Color.White;
            this.dataGridView_recoman_keyword.BorderStyle = BorderStyle.Fixed3D;
            this.dataGridView_recoman_keyword.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = SystemColors.Window;
            dataGridViewCellStyle4.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle4.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle4.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = DataGridViewTriState.False;
            this.dataGridView_recoman_keyword.DefaultCellStyle = dataGridViewCellStyle4;
            this.dataGridView_recoman_keyword.EnableHeadersVisualStyles = false;
            this.dataGridView_recoman_keyword.GridColor = Color.LightGray;
            this.dataGridView_recoman_keyword.Location = new Point(10, 75);
            this.dataGridView_recoman_keyword.MinimumSize = new Size(200, 120);
            this.dataGridView_recoman_keyword.Name = "dataGridView_recoman_keyword";
            this.dataGridView_recoman_keyword.Size = new Size(242, 180);
            this.dataGridView_recoman_keyword.TabIndex = 43;

            // 
            // new_reco_word
            // 
            this.new_reco_word.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.new_reco_word.Location = new Point(10, 35);
            this.new_reco_word.Name = "new_reco_word";
            this.new_reco_word.PlaceholderText = "신규 항목 입력";
            this.new_reco_word.Size = new Size(130, 33);
            this.new_reco_word.TabIndex = 27;
            this.new_reco_word.KeyDown += new_reco_word_KeyDown;

            // 
            // reco_del_btn
            // 
            this.reco_del_btn.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.reco_del_btn.AutoSize = true;
            this.reco_del_btn.Font = new Font("맑은 고딕", 14.25F);
            this.reco_del_btn.Location = new Point(192, 33);
            this.reco_del_btn.Name = "reco_del_btn";
            this.reco_del_btn.Size = new Size(60, 35);
            this.reco_del_btn.TabIndex = 24;
            this.reco_del_btn.Text = "제거";
            this.reco_del_btn.UseVisualStyleBackColor = true;
            this.reco_del_btn.Click += reco_del_btn_Click;

            // 
            // reco_add_btn
            // 
            this.reco_add_btn.AutoSize = true;
            this.reco_add_btn.Font = new Font("맑은 고딕", 14.25F);
            this.reco_add_btn.Location = new Point(145, 33);
            this.reco_add_btn.Name = "reco_add_btn";
            this.reco_add_btn.Size = new Size(60, 35);
            this.reco_add_btn.TabIndex = 23;
            this.reco_add_btn.Text = "추가";
            this.reco_add_btn.UseVisualStyleBackColor = true;
            this.reco_add_btn.Click += reco_add_btn_Click;

            // GroupBox2 컨트롤들 (클러스터링 병합 결과 확인)
            //

            // button2
            // 
            this.button2.AutoSize = true;
            this.button2.Font = new Font("맑은 고딕", 14.25F);
            this.button2.Location = new Point(10, 35);
            this.button2.Name = "button2";
            this.button2.Size = new Size(195, 35);
            this.button2.TabIndex = 49;
            this.button2.Text = "선택 항목 상세 보기";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += button2_Click;

            // 
            // union_cluster_btn
            // 
            this.union_cluster_btn.AutoSize = true;
            this.union_cluster_btn.Font = new Font("맑은 고딕", 14.25F);
            this.union_cluster_btn.Location = new Point(220, 35);
            this.union_cluster_btn.Name = "union_cluster_btn";
            this.union_cluster_btn.Size = new Size(195, 35);
            this.union_cluster_btn.TabIndex = 48;
            this.union_cluster_btn.Text = "선택 항목 간 병합";
            this.union_cluster_btn.UseVisualStyleBackColor = true;
            this.union_cluster_btn.Click += union_cluster_btn_Click;

            // 
            // label7
            // 
            this.label7.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left)));
            this.label7.AutoSize = true;
            this.label7.Font = new Font("맑은 고딕", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            this.label7.ForeColor = Color.IndianRed;
            this.label7.Location = new Point(10, 420);
            this.label7.Name = "label7";
            this.label7.Size = new Size(249, 17);
            this.label7.TabIndex = 47;
            this.label7.Text = "※ 클러스터명은 직접 수정이 가능합니다.";

            // 
            // complete_btn
            // 
            this.complete_btn.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
            this.complete_btn.AutoSize = true;
            this.complete_btn.Font = new Font("맑은 고딕", 14.25F);
            this.complete_btn.Location = new Point(450, 445);
            this.complete_btn.Name = "complete_btn";
            this.complete_btn.Size = new Size(80, 35);
            this.complete_btn.TabIndex = 45;
            this.complete_btn.Text = "완료";
            this.complete_btn.UseVisualStyleBackColor = true;
            this.complete_btn.Click += complete_btn_Click;

            // 
            // check_search_radio2
            // 
            this.check_search_radio2.AutoSize = true;
            this.check_search_radio2.Font = new Font("맑은 고딕", 14.25F);
            this.check_search_radio2.Location = new Point(250, 110);
            this.check_search_radio2.Name = "check_search_radio2";
            this.check_search_radio2.Size = new Size(132, 29);
            this.check_search_radio2.TabIndex = 33;
            this.check_search_radio2.Text = "키워드 입력";
            this.check_search_radio2.UseVisualStyleBackColor = true;
            this.check_search_radio2.CheckedChanged += check_search_radio2_CheckedChanged;

            // 
            // check_search_radio1
            // 
            this.check_search_radio1.AutoSize = true;
            this.check_search_radio1.Checked = true;
            this.check_search_radio1.Font = new Font("맑은 고딕", 14.25F);
            this.check_search_radio1.Location = new Point(250, 75);
            this.check_search_radio1.Name = "check_search_radio1";
            this.check_search_radio1.Size = new Size(132, 29);
            this.check_search_radio1.TabIndex = 32;
            this.check_search_radio1.TabStop = true;
            this.check_search_radio1.Text = "키워드 선택";
            this.check_search_radio1.UseVisualStyleBackColor = true;
            this.check_search_radio1.CheckedChanged += check_search_radio1_CheckedChanged;

            // 
            // check_search_combo
            // 
            this.check_search_combo.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.check_search_combo.Font = new Font("맑은 고딕", 14.25F);
            this.check_search_combo.FormattingEnabled = true;
            this.check_search_combo.Location = new Point(390, 73);
            this.check_search_combo.Name = "check_search_combo";
            this.check_search_combo.Size = new Size(140, 33);
            this.check_search_combo.TabIndex = 31;
            this.check_search_combo.Text = "키워드 선택";

            // 
            // check_search_keyword
            // 
            this.check_search_keyword.Enabled = false;
            this.check_search_keyword.Font = new Font("맑은 고딕", 14.25F);
            this.check_search_keyword.Location = new Point(390, 108);
            this.check_search_keyword.Name = "check_search_keyword";
            this.check_search_keyword.PlaceholderText = "키워드 입력";
            this.check_search_keyword.Size = new Size(140, 33);
            this.check_search_keyword.TabIndex = 30;
            this.check_search_keyword.KeyDown += check_search_keyword_KeyDown;

            // 
            // merge_cancel_button
            // 
            this.merge_cancel_button.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Left)));
            this.merge_cancel_button.AutoSize = true;
            this.merge_cancel_button.Font = new Font("맑은 고딕", 14.25F);
            this.merge_cancel_button.Location = new Point(10, 445);
            this.merge_cancel_button.Name = "merge_cancel_button";
            this.merge_cancel_button.Size = new Size(195, 35);
            this.merge_cancel_button.TabIndex = 26;
            this.merge_cancel_button.Text = "선택 항목 병합 해제";
            this.merge_cancel_button.UseVisualStyleBackColor = true;
            this.merge_cancel_button.Click += merge_cancel_button_Click;

            // 
            // check_search_button
            // 
            this.check_search_button.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.check_search_button.AutoSize = true;
            this.check_search_button.Font = new Font("맑은 고딕", 14.25F);
            this.check_search_button.Location = new Point(450, 108);
            this.check_search_button.Name = "check_search_button";
            this.check_search_button.Size = new Size(80, 35);
            this.check_search_button.TabIndex = 24;
            this.check_search_button.Text = "검색";
            this.check_search_button.UseVisualStyleBackColor = true;
            this.check_search_button.Click += check_search_button_Click;

            // 
            // merge_check_table
            // 
            this.merge_check_table.AllowUserToAddRows = false;
            this.merge_check_table.AllowUserToDeleteRows = false;
            this.merge_check_table.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
            | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.merge_check_table.BackgroundColor = Color.White;
            this.merge_check_table.BorderStyle = BorderStyle.Fixed3D;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Control;
            dataGridViewCellStyle1.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            this.merge_check_table.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.merge_check_table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = SystemColors.Window;
            dataGridViewCellStyle2.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle2.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            this.merge_check_table.DefaultCellStyle = dataGridViewCellStyle2;
            this.merge_check_table.EnableHeadersVisualStyles = false;
            this.merge_check_table.GridColor = Color.LightGray;
            this.merge_check_table.Location = new Point(10, 150);
            this.merge_check_table.MinimumSize = new Size(300, 200);
            this.merge_check_table.Name = "merge_check_table";
            this.merge_check_table.ReadOnly = true;
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = SystemColors.Control;
            dataGridViewCellStyle3.Font = new Font("돋움체", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle3.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.True;
            this.merge_check_table.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.merge_check_table.Size = new Size(520, 260);
            this.merge_check_table.TabIndex = 23;

            // 
            // uc_Clustering
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.White;
            this.Controls.Add(this.tableLayoutMain);
            this.MinimumSize = new Size(1280, 800);
            this.Name = "uc_Clustering";
            //this.Size = new Size(1904, 1017);
            // 수정 후
            //this.Dock = DockStyle.Fill;  // ← 추가 (부모 컨테이너에 맞춰 자동 크기 조정)

            // 컨트롤 정리
            this.tableLayoutMain.ResumeLayout(false);
            this.pnl_left.ResumeLayout(false);
            this.pnl_right.ResumeLayout(false);
            this.tableLayoutLeft.ResumeLayout(false);
            this.tableLayoutRight.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupbox2.ResumeLayout(false);
            this.groupbox2.PerformLayout();
            this.pnl_merge_header.ResumeLayout(false);
            this.pnl_merge_header.PerformLayout();
            this.pnl_merge_controls.ResumeLayout(false);
            this.pnl_merge_controls.PerformLayout();
            this.pnl_merge_data.ResumeLayout(false);
            this.pnl_merge_pagination.ResumeLayout(false);
            this.pnl_merge_pagination.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.gb_separator.ResumeLayout(false);
            this.gb_separator.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.merge_cluster_table)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_pageNumber)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_recoman_keyword)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_lv1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_modified)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.merge_check_table)).EndInit();
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
        private Panel pnl_merge_header;
        private Panel pnl_merge_controls;
        private Panel pnl_merge_data;
        private Panel pnl_merge_pagination;

        // 기존 컨트롤들 (모든 컨트롤명 유지)
        private Button complete_btn;
        private GroupBox groupbox2;
        private RadioButton check_search_radio2;
        private RadioButton check_search_radio1;
        private ComboBox check_search_combo;
        private TextBox check_search_keyword;
        private Button merge_cancel_button;
        private Button check_search_button;
        private DataGridView merge_check_table;
        private GroupBox groupBox1;
        private RadioButton merge_search_radio2;
        private RadioButton merge_search_radio1;
        private ComboBox merge_keyword_combo;
        private TextBox merge_search_keyword;
        private CheckBox merge_all_check;
        private Button button1;
        private Button merge_search_button;
        private DataGridView merge_cluster_table;
        private Button merge_addon_btn;
        private GroupBox groupBox3;
        private Label label5;
        private ComboBox decimal_combo;
        private DataGridView dataGridView_modified;
        private GroupBox groupBox4;
        private DataGridView dataGridView_recoman_keyword;
        private TextBox new_reco_word;
        private Button reco_del_btn;
        private Button reco_add_btn;
        private GroupBox gb_separator;
        private DataGridView dataGridView_lv1;
        private TextBox new_lv1_word;
        private Button lv1_del_btn;
        private Button lv1_add_btn;
        private Label label1;
        private Label label7;
        private Label cluster_count;
        private TextBox except_keyword;
        private Label uncluster_count;
        private CheckBox equal_search_checkbox;
        private CheckBox excep_search_checkbox;
        private Label label3;
        private Label label2;
        private Label label4;
        private Label uncluster_count_money;
        private RadioButton keyword_radio2;
        private RadioButton keyword_radio1;
        private GroupBox groupBox5;
        private Button union_cluster_btn;
        private Button button2;
        private Button btn_nextPage;
        private Button btn_prevPage;
        private ComboBox cmb_pageSize;
        private Label lbl_pageSizeText;
        private Label lbl_pagination;
        private NumericUpDown num_pageNumber;
        private Label lbl_pagination2;
    }
}
