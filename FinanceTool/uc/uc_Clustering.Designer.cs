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
            tableLayoutMain = new TableLayoutPanel();
            pnl_left = new Panel();
            groupBox1 = new GroupBox();
            tableLayoutLeft = new TableLayoutPanel();
            pnl_merge_header = new Panel();
            groupBox5 = new GroupBox();
            keyword_radio1 = new RadioButton();
            keyword_radio2 = new RadioButton();
            uncluster_count_money = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            uncluster_count = new Label();
            cluster_count = new Label();
            pnl_merge_controls = new Panel();
            excep_search_checkbox = new CheckBox();
            equal_search_checkbox = new CheckBox();
            except_keyword = new TextBox();
            merge_addon_btn = new Button();
            merge_search_radio2 = new RadioButton();
            label5 = new Label();
            decimal_combo = new ComboBox();
            merge_search_radio1 = new RadioButton();
            merge_keyword_combo = new ComboBox();
            merge_search_keyword = new TextBox();
            merge_all_check = new CheckBox();
            button1 = new Button();
            merge_search_button = new Button();
            pnl_merge_data = new Panel();
            merge_cluster_table = new DataGridView();
            pnl_merge_pagination = new Panel();
            btn_nextPage = new Button();
            btn_prevPage = new Button();
            cmb_pageSize = new ComboBox();
            lbl_pageSizeText = new Label();
            lbl_pagination = new Label();
            num_pageNumber = new NumericUpDown();
            lbl_pagination2 = new Label();
            pnl_right = new Panel();
            tableLayoutRight = new TableLayoutPanel();
            groupBox3 = new GroupBox();
            label1 = new Label();
            groupBox4 = new GroupBox();
            dataGridView_recoman_keyword = new DataGridView();
            new_reco_word = new TextBox();
            reco_del_btn = new Button();
            reco_add_btn = new Button();
            gb_separator = new GroupBox();
            dataGridView_lv1 = new DataGridView();
            new_lv1_word = new TextBox();
            lv1_del_btn = new Button();
            lv1_add_btn = new Button();
            dataGridView_modified = new DataGridView();
            groupbox2 = new GroupBox();
            button2 = new Button();
            union_cluster_btn = new Button();
            label7 = new Label();
            complete_btn = new Button();
            check_search_radio2 = new RadioButton();
            check_search_radio1 = new RadioButton();
            check_search_combo = new ComboBox();
            check_search_keyword = new TextBox();
            merge_cancel_button = new Button();
            check_search_button = new Button();
            merge_check_table = new DataGridView();
            tableLayoutMain.SuspendLayout();
            pnl_left.SuspendLayout();
            groupBox1.SuspendLayout();
            tableLayoutLeft.SuspendLayout();
            pnl_merge_header.SuspendLayout();
            groupBox5.SuspendLayout();
            pnl_merge_controls.SuspendLayout();
            pnl_merge_data.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)merge_cluster_table).BeginInit();
            pnl_merge_pagination.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)num_pageNumber).BeginInit();
            pnl_right.SuspendLayout();
            tableLayoutRight.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_recoman_keyword).BeginInit();
            gb_separator.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_lv1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView_modified).BeginInit();
            groupbox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)merge_check_table).BeginInit();
            SuspendLayout();
            // 
            // tableLayoutMain
            // 
            tableLayoutMain.ColumnCount = 2;
            tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            tableLayoutMain.Controls.Add(pnl_left, 0, 0);
            tableLayoutMain.Controls.Add(pnl_right, 1, 0);
            tableLayoutMain.Dock = DockStyle.Fill;
            tableLayoutMain.Location = new Point(0, 0);
            tableLayoutMain.Name = "tableLayoutMain";
            tableLayoutMain.RowCount = 1;
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutMain.Size = new Size(1904, 1017);
            tableLayoutMain.TabIndex = 0;
            // 
            // pnl_left
            // 
            pnl_left.Controls.Add(groupBox1);
            pnl_left.Dock = DockStyle.Fill;
            pnl_left.Location = new Point(10, 10);
            pnl_left.Margin = new Padding(10);
            pnl_left.Name = "pnl_left";
            pnl_left.Size = new Size(1217, 997);
            pnl_left.TabIndex = 0;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(tableLayoutLeft);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Font = new Font("맑은 고딕", 15.75F);
            groupBox1.Location = new Point(0, 0);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(1217, 997);
            groupBox1.TabIndex = 42;
            groupBox1.TabStop = false;
            groupBox1.Text = "Clustering 병합";
            // 
            // tableLayoutLeft
            // 
            tableLayoutLeft.ColumnCount = 1;
            tableLayoutLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutLeft.Controls.Add(pnl_merge_header, 0, 0);
            tableLayoutLeft.Controls.Add(pnl_merge_controls, 0, 1);
            tableLayoutLeft.Controls.Add(pnl_merge_data, 0, 2);
            tableLayoutLeft.Controls.Add(pnl_merge_pagination, 0, 3);
            tableLayoutLeft.Dock = DockStyle.Fill;
            tableLayoutLeft.Location = new Point(3, 31);
            tableLayoutLeft.Name = "tableLayoutLeft";
            tableLayoutLeft.RowCount = 4;
            tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
            tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
            tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutLeft.Size = new Size(1211, 963);
            tableLayoutLeft.TabIndex = 0;
            // 
            // pnl_merge_header
            // 
            pnl_merge_header.Controls.Add(groupBox5);
            pnl_merge_header.Controls.Add(uncluster_count_money);
            pnl_merge_header.Controls.Add(label4);
            pnl_merge_header.Controls.Add(label3);
            pnl_merge_header.Controls.Add(label2);
            pnl_merge_header.Controls.Add(uncluster_count);
            pnl_merge_header.Controls.Add(cluster_count);
            pnl_merge_header.Dock = DockStyle.Fill;
            pnl_merge_header.Location = new Point(3, 3);
            pnl_merge_header.Name = "pnl_merge_header";
            pnl_merge_header.Size = new Size(1205, 74);
            pnl_merge_header.TabIndex = 0;
            // 
            // groupBox5
            // 
            groupBox5.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            groupBox5.Controls.Add(keyword_radio1);
            groupBox5.Controls.Add(keyword_radio2);
            groupBox5.Location = new Point(749, 5);
            groupBox5.Name = "groupBox5";
            groupBox5.Size = new Size(300, 40);
            groupBox5.TabIndex = 54;
            groupBox5.TabStop = false;
            // 
            // keyword_radio1
            // 
            keyword_radio1.AutoSize = true;
            keyword_radio1.Checked = true;
            keyword_radio1.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            keyword_radio1.Location = new Point(6, 8);
            keyword_radio1.Name = "keyword_radio1";
            keyword_radio1.Size = new Size(132, 29);
            keyword_radio1.TabIndex = 52;
            keyword_radio1.TabStop = true;
            keyword_radio1.Text = "키워드 검색";
            keyword_radio1.UseVisualStyleBackColor = true;
            keyword_radio1.CheckedChanged += keyword_radio1_CheckedChanged;
            // 
            // keyword_radio2
            // 
            keyword_radio2.AutoSize = true;
            keyword_radio2.Font = new Font("맑은 고딕", 14.25F);
            keyword_radio2.Location = new Point(144, 8);
            keyword_radio2.Name = "keyword_radio2";
            keyword_radio2.Size = new Size(151, 29);
            keyword_radio2.TabIndex = 53;
            keyword_radio2.Text = "공급업체 검색";
            keyword_radio2.UseVisualStyleBackColor = true;
            // 
            // uncluster_count_money
            // 
            uncluster_count_money.AutoSize = true;
            uncluster_count_money.Location = new Point(10, 50);
            uncluster_count_money.Name = "uncluster_count_money";
            uncluster_count_money.Size = new Size(179, 30);
            uncluster_count_money.TabIndex = 51;
            uncluster_count_money.Text = "미병합 합산금액 :";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("맑은 고딕", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            label4.ForeColor = Color.IndianRed;
            label4.Location = new Point(480, 33);
            label4.Name = "label4";
            label4.Size = new Size(140, 17);
            label4.TabIndex = 50;
            label4.Text = "   추가할 수 있습니다.";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("맑은 고딕", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            label3.ForeColor = Color.IndianRed;
            label3.Location = new Point(480, 8);
            label3.Name = "label3";
            label3.Size = new Size(331, 17);
            label3.TabIndex = 49;
            label3.Text = "※ 제외 항목 입력 시 , 를 활용하여 제외 키워드 항목을";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("맑은 고딕", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            label2.ForeColor = Color.IndianRed;
            label2.Location = new Point(350, 60);
            label2.Name = "label2";
            label2.Size = new Size(345, 17);
            label2.TabIndex = 48;
            label2.Text = "※ 검색어 입력 시 , 를 활용하여 AND 검색이 가능합니다.";
            // 
            // uncluster_count
            // 
            uncluster_count.AutoSize = true;
            uncluster_count.Location = new Point(10, 28);
            uncluster_count.Name = "uncluster_count";
            uncluster_count.Size = new Size(159, 30);
            uncluster_count.TabIndex = 46;
            uncluster_count.Text = "미병합 Cluster :";
            // 
            // cluster_count
            // 
            cluster_count.AutoSize = true;
            cluster_count.Location = new Point(10, 5);
            cluster_count.Name = "cluster_count";
            cluster_count.Size = new Size(74, 30);
            cluster_count.TabIndex = 43;
            cluster_count.Text = "행 수 :";
            // 
            // pnl_merge_controls
            // 
            pnl_merge_controls.Controls.Add(excep_search_checkbox);
            pnl_merge_controls.Controls.Add(equal_search_checkbox);
            pnl_merge_controls.Controls.Add(except_keyword);
            pnl_merge_controls.Controls.Add(merge_addon_btn);
            pnl_merge_controls.Controls.Add(merge_search_radio2);
            pnl_merge_controls.Controls.Add(label5);
            pnl_merge_controls.Controls.Add(decimal_combo);
            pnl_merge_controls.Controls.Add(merge_search_radio1);
            pnl_merge_controls.Controls.Add(merge_keyword_combo);
            pnl_merge_controls.Controls.Add(merge_search_keyword);
            pnl_merge_controls.Controls.Add(merge_all_check);
            pnl_merge_controls.Controls.Add(button1);
            pnl_merge_controls.Controls.Add(merge_search_button);
            pnl_merge_controls.Dock = DockStyle.Fill;
            pnl_merge_controls.Location = new Point(3, 83);
            pnl_merge_controls.Name = "pnl_merge_controls";
            pnl_merge_controls.Size = new Size(1205, 114);
            pnl_merge_controls.TabIndex = 1;
            // 
            // excep_search_checkbox
            // 
            excep_search_checkbox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            excep_search_checkbox.AutoSize = true;
            excep_search_checkbox.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            excep_search_checkbox.Location = new Point(799, 85);
            excep_search_checkbox.Name = "excep_search_checkbox";
            excep_search_checkbox.Size = new Size(170, 29);
            excep_search_checkbox.TabIndex = 48;
            excep_search_checkbox.Text = "검색 제외 항목 :";
            excep_search_checkbox.UseVisualStyleBackColor = true;
            excep_search_checkbox.CheckedChanged += excep_search_checkbox_CheckedChanged;
            // 
            // equal_search_checkbox
            // 
            equal_search_checkbox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            equal_search_checkbox.AutoSize = true;
            equal_search_checkbox.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            equal_search_checkbox.Location = new Point(599, 85);
            equal_search_checkbox.Name = "equal_search_checkbox";
            equal_search_checkbox.Size = new Size(204, 29);
            equal_search_checkbox.TabIndex = 47;
            equal_search_checkbox.Text = "검색 조건 완전 일치";
            equal_search_checkbox.UseVisualStyleBackColor = true;
            equal_search_checkbox.CheckedChanged += equal_search_checkbox_CheckedChanged;
            // 
            // except_keyword
            // 
            except_keyword.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            except_keyword.Enabled = false;
            except_keyword.Font = new Font("맑은 고딕", 14.25F);
            except_keyword.Location = new Point(979, 83);
            except_keyword.Name = "except_keyword";
            except_keyword.PlaceholderText = "제외 항목 입력";
            except_keyword.Size = new Size(200, 33);
            except_keyword.TabIndex = 45;
            except_keyword.KeyDown += except_keyword_KeyDown;
            // 
            // merge_addon_btn
            // 
            merge_addon_btn.AutoSize = true;
            merge_addon_btn.Font = new Font("맑은 고딕", 14.25F);
            merge_addon_btn.Location = new Point(220, 83);
            merge_addon_btn.Name = "merge_addon_btn";
            merge_addon_btn.Size = new Size(122, 35);
            merge_addon_btn.TabIndex = 42;
            merge_addon_btn.Text = "추가 병합";
            merge_addon_btn.UseVisualStyleBackColor = true;
            merge_addon_btn.Click += merge_addon_btn_Click;
            // 
            // merge_search_radio2
            // 
            merge_search_radio2.AutoSize = true;
            merge_search_radio2.Font = new Font("맑은 고딕", 14.25F);
            merge_search_radio2.Location = new Point(700, 45);
            merge_search_radio2.Name = "merge_search_radio2";
            merge_search_radio2.Size = new Size(132, 29);
            merge_search_radio2.TabIndex = 41;
            merge_search_radio2.Text = "검색어 입력";
            merge_search_radio2.UseVisualStyleBackColor = true;
            merge_search_radio2.CheckedChanged += merge_search_radio2_CheckedChanged;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            label5.Location = new Point(350, 88);
            label5.Name = "label5";
            label5.Size = new Size(50, 25);
            label5.TabIndex = 35;
            label5.Text = "단위";
            // 
            // decimal_combo
            // 
            decimal_combo.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            decimal_combo.FormattingEnabled = true;
            decimal_combo.Items.AddRange(new object[] { "원", "천원", "백만원", "억원" });
            decimal_combo.Location = new Point(406, 85);
            decimal_combo.Name = "decimal_combo";
            decimal_combo.Size = new Size(80, 33);
            decimal_combo.TabIndex = 24;
            decimal_combo.SelectedIndexChanged += decimal_combo_SelectedIndexChanged;
            // 
            // merge_search_radio1
            // 
            merge_search_radio1.AutoSize = true;
            merge_search_radio1.Checked = true;
            merge_search_radio1.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            merge_search_radio1.Location = new Point(700, 10);
            merge_search_radio1.Name = "merge_search_radio1";
            merge_search_radio1.Size = new Size(132, 29);
            merge_search_radio1.TabIndex = 40;
            merge_search_radio1.TabStop = true;
            merge_search_radio1.Text = "검색어 선택";
            merge_search_radio1.UseVisualStyleBackColor = true;
            merge_search_radio1.CheckedChanged += merge_search_radio1_CheckedChanged;
            // 
            // merge_keyword_combo
            // 
            merge_keyword_combo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            merge_keyword_combo.Font = new Font("맑은 고딕", 14.25F);
            merge_keyword_combo.FormattingEnabled = true;
            merge_keyword_combo.Location = new Point(840, 8);
            merge_keyword_combo.Name = "merge_keyword_combo";
            merge_keyword_combo.Size = new Size(299, 33);
            merge_keyword_combo.TabIndex = 39;
            merge_keyword_combo.Text = "검색어 선택";
            // 
            // merge_search_keyword
            // 
            merge_search_keyword.Enabled = false;
            merge_search_keyword.Font = new Font("맑은 고딕", 14.25F);
            merge_search_keyword.Location = new Point(840, 43);
            merge_search_keyword.Name = "merge_search_keyword";
            merge_search_keyword.PlaceholderText = "검색 키워드 입력";
            merge_search_keyword.Size = new Size(300, 33);
            merge_search_keyword.TabIndex = 38;
            merge_search_keyword.KeyDown += merge_search_keyword_KeyDown;
            // 
            // merge_all_check
            // 
            merge_all_check.AutoSize = true;
            merge_all_check.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            merge_all_check.Location = new Point(10, 88);
            merge_all_check.Name = "merge_all_check";
            merge_all_check.Size = new Size(114, 29);
            merge_all_check.TabIndex = 37;
            merge_all_check.Text = "전체 선택";
            merge_all_check.UseVisualStyleBackColor = true;
            merge_all_check.CheckedChanged += merge_all_check_CheckedChanged;
            // 
            // button1
            // 
            button1.AutoSize = true;
            button1.Font = new Font("맑은 고딕", 14.25F);
            button1.Location = new Point(130, 83);
            button1.Name = "button1";
            button1.Size = new Size(63, 35);
            button1.TabIndex = 36;
            button1.Text = "병합";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // merge_search_button
            // 
            merge_search_button.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            merge_search_button.AutoSize = true;
            merge_search_button.Font = new Font("맑은 고딕", 14.25F);
            merge_search_button.Location = new Point(1049, 41);
            merge_search_button.Name = "merge_search_button";
            merge_search_button.Size = new Size(63, 35);
            merge_search_button.TabIndex = 35;
            merge_search_button.Text = "검색";
            merge_search_button.UseVisualStyleBackColor = true;
            merge_search_button.Click += merge_search_button_Click;
            // 
            // pnl_merge_data
            // 
            pnl_merge_data.Controls.Add(merge_cluster_table);
            pnl_merge_data.Dock = DockStyle.Fill;
            pnl_merge_data.Location = new Point(5, 205);
            pnl_merge_data.Margin = new Padding(5);
            pnl_merge_data.Name = "pnl_merge_data";
            pnl_merge_data.Size = new Size(1201, 703);
            pnl_merge_data.TabIndex = 2;
            // 
            // merge_cluster_table
            // 
            merge_cluster_table.AllowUserToAddRows = false;
            merge_cluster_table.AllowUserToDeleteRows = false;
            merge_cluster_table.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            merge_cluster_table.BackgroundColor = Color.White;
            merge_cluster_table.BorderStyle = BorderStyle.Fixed3D;
            merge_cluster_table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            merge_cluster_table.EnableHeadersVisualStyles = false;
            merge_cluster_table.GridColor = Color.LightGray;
            merge_cluster_table.Location = new Point(5, 5);
            merge_cluster_table.MinimumSize = new Size(500, 300);
            merge_cluster_table.Name = "merge_cluster_table";
            merge_cluster_table.ReadOnly = true;
            merge_cluster_table.Size = new Size(1191, 693);
            merge_cluster_table.TabIndex = 34;
            merge_cluster_table.CellContentClick += merge_cluster_table_CellContentClick;
            // 
            // pnl_merge_pagination
            // 
            pnl_merge_pagination.Controls.Add(btn_nextPage);
            pnl_merge_pagination.Controls.Add(btn_prevPage);
            pnl_merge_pagination.Controls.Add(cmb_pageSize);
            pnl_merge_pagination.Controls.Add(lbl_pageSizeText);
            pnl_merge_pagination.Controls.Add(lbl_pagination);
            pnl_merge_pagination.Controls.Add(num_pageNumber);
            pnl_merge_pagination.Controls.Add(lbl_pagination2);
            pnl_merge_pagination.Dock = DockStyle.Fill;
            pnl_merge_pagination.Location = new Point(3, 916);
            pnl_merge_pagination.Name = "pnl_merge_pagination";
            pnl_merge_pagination.Size = new Size(1205, 44);
            pnl_merge_pagination.TabIndex = 3;
            // 
            // btn_nextPage
            // 
            btn_nextPage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btn_nextPage.AutoSize = true;
            btn_nextPage.Font = new Font("맑은 고딕", 14.25F);
            btn_nextPage.Location = new Point(1099, 8);
            btn_nextPage.Name = "btn_nextPage";
            btn_nextPage.Size = new Size(86, 35);
            btn_nextPage.TabIndex = 67;
            btn_nextPage.Text = "다음 ▶";
            btn_nextPage.UseVisualStyleBackColor = true;
            // 
            // btn_prevPage
            // 
            btn_prevPage.AutoSize = true;
            btn_prevPage.Font = new Font("맑은 고딕", 14.25F);
            btn_prevPage.Location = new Point(500, 7);
            btn_prevPage.Name = "btn_prevPage";
            btn_prevPage.Size = new Size(86, 35);
            btn_prevPage.TabIndex = 66;
            btn_prevPage.Text = "◀ 이전";
            btn_prevPage.UseVisualStyleBackColor = true;
            // 
            // cmb_pageSize
            // 
            cmb_pageSize.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            cmb_pageSize.FormattingEnabled = true;
            cmb_pageSize.Location = new Point(140, 10);
            cmb_pageSize.Name = "cmb_pageSize";
            cmb_pageSize.Size = new Size(121, 33);
            cmb_pageSize.TabIndex = 65;
            // 
            // lbl_pageSizeText
            // 
            lbl_pageSizeText.AutoSize = true;
            lbl_pageSizeText.Font = new Font("맑은 고딕", 14F);
            lbl_pageSizeText.Location = new Point(10, 13);
            lbl_pageSizeText.Name = "lbl_pageSizeText";
            lbl_pageSizeText.Size = new Size(125, 25);
            lbl_pageSizeText.TabIndex = 64;
            lbl_pageSizeText.Text = "페이지 크기 :";
            // 
            // lbl_pagination
            // 
            lbl_pagination.AutoSize = true;
            lbl_pagination.Font = new Font("맑은 고딕", 14F);
            lbl_pagination.Location = new Point(592, 12);
            lbl_pagination.Name = "lbl_pagination";
            lbl_pagination.Size = new Size(80, 25);
            lbl_pagination.TabIndex = 61;
            lbl_pagination.Text = "페이지 :";
            // 
            // num_pageNumber
            // 
            num_pageNumber.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            num_pageNumber.Location = new Point(685, 10);
            num_pageNumber.Name = "num_pageNumber";
            num_pageNumber.Size = new Size(52, 33);
            num_pageNumber.TabIndex = 63;
            // 
            // lbl_pagination2
            // 
            lbl_pagination2.AutoSize = true;
            lbl_pagination2.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            lbl_pagination2.Location = new Point(743, 13);
            lbl_pagination2.Name = "lbl_pagination2";
            lbl_pagination2.Size = new Size(118, 25);
            lbl_pagination2.TabIndex = 62;
            lbl_pagination2.Text = "/ 0 (총 0 행)";
            // 
            // pnl_right
            // 
            pnl_right.Controls.Add(tableLayoutRight);
            pnl_right.Dock = DockStyle.Fill;
            pnl_right.Location = new Point(1247, 10);
            pnl_right.Margin = new Padding(10);
            pnl_right.Name = "pnl_right";
            pnl_right.Size = new Size(647, 997);
            pnl_right.TabIndex = 1;
            // 
            // tableLayoutRight
            // 
            tableLayoutRight.ColumnCount = 1;
            tableLayoutRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutRight.Controls.Add(groupBox3, 0, 0);
            tableLayoutRight.Controls.Add(groupbox2, 0, 1);
            tableLayoutRight.Dock = DockStyle.Fill;
            tableLayoutRight.Location = new Point(0, 0);
            tableLayoutRight.Name = "tableLayoutRight";
            tableLayoutRight.RowCount = 2;
            tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutRight.Size = new Size(647, 997);
            tableLayoutRight.TabIndex = 0;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(label1);
            groupBox3.Controls.Add(groupBox4);
            groupBox3.Controls.Add(gb_separator);
            groupBox3.Controls.Add(dataGridView_modified);
            groupBox3.Dock = DockStyle.Fill;
            groupBox3.Font = new Font("맑은 고딕", 15.75F);
            groupBox3.Location = new Point(5, 5);
            groupBox3.Margin = new Padding(5);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(637, 488);
            groupBox3.TabIndex = 43;
            groupBox3.TabStop = false;
            groupBox3.Text = "검색 키워드 추천";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("맑은 고딕", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            label1.Location = new Point(10, 35);
            label1.Name = "label1";
            label1.Size = new Size(159, 25);
            label1.TabIndex = 46;
            label1.Text = "상위 키워드 목록";
            // 
            // groupBox4
            // 
            groupBox4.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            groupBox4.Controls.Add(dataGridView_recoman_keyword);
            groupBox4.Controls.Add(new_reco_word);
            groupBox4.Controls.Add(reco_del_btn);
            groupBox4.Controls.Add(reco_add_btn);
            groupBox4.Font = new Font("맑은 고딕", 16F);
            groupBox4.Location = new Point(327, 215);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new Size(300, 265);
            groupBox4.TabIndex = 45;
            groupBox4.TabStop = false;
            groupBox4.Text = "추천 키워드 선택";
            // 
            // dataGridView_recoman_keyword
            // 
            dataGridView_recoman_keyword.AllowUserToAddRows = false;
            dataGridView_recoman_keyword.AllowUserToDeleteRows = false;
            dataGridView_recoman_keyword.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView_recoman_keyword.BackgroundColor = Color.White;
            dataGridView_recoman_keyword.BorderStyle = BorderStyle.Fixed3D;
            dataGridView_recoman_keyword.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Window;
            dataGridViewCellStyle1.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle1.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.False;
            dataGridView_recoman_keyword.DefaultCellStyle = dataGridViewCellStyle1;
            dataGridView_recoman_keyword.EnableHeadersVisualStyles = false;
            dataGridView_recoman_keyword.GridColor = Color.LightGray;
            dataGridView_recoman_keyword.Location = new Point(10, 75);
            dataGridView_recoman_keyword.MinimumSize = new Size(200, 120);
            dataGridView_recoman_keyword.Name = "dataGridView_recoman_keyword";
            dataGridView_recoman_keyword.Size = new Size(280, 180);
            dataGridView_recoman_keyword.TabIndex = 43;
            // 
            // new_reco_word
            // 
            new_reco_word.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            new_reco_word.Location = new Point(10, 35);
            new_reco_word.Name = "new_reco_word";
            new_reco_word.PlaceholderText = "신규 항목 입력";
            new_reco_word.Size = new Size(145, 33);
            new_reco_word.TabIndex = 27;
            new_reco_word.KeyDown += new_reco_word_KeyDown;
            // 
            // reco_del_btn
            // 
            reco_del_btn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            reco_del_btn.AutoSize = true;
            reco_del_btn.Font = new Font("맑은 고딕", 14.25F);
            reco_del_btn.Location = new Point(230, 33);
            reco_del_btn.Name = "reco_del_btn";
            reco_del_btn.Size = new Size(60, 35);
            reco_del_btn.TabIndex = 24;
            reco_del_btn.Text = "제거";
            reco_del_btn.UseVisualStyleBackColor = true;
            reco_del_btn.Click += reco_del_btn_Click;
            // 
            // reco_add_btn
            // 
            reco_add_btn.AutoSize = true;
            reco_add_btn.Font = new Font("맑은 고딕", 14.25F);
            reco_add_btn.Location = new Point(176, 33);
            reco_add_btn.Name = "reco_add_btn";
            reco_add_btn.Size = new Size(60, 35);
            reco_add_btn.TabIndex = 23;
            reco_add_btn.Text = "추가";
            reco_add_btn.UseVisualStyleBackColor = true;
            reco_add_btn.Click += reco_add_btn_Click;
            // 
            // gb_separator
            // 
            gb_separator.Controls.Add(dataGridView_lv1);
            gb_separator.Controls.Add(new_lv1_word);
            gb_separator.Controls.Add(lv1_del_btn);
            gb_separator.Controls.Add(lv1_add_btn);
            gb_separator.Font = new Font("맑은 고딕", 16F);
            gb_separator.Location = new Point(10, 215);
            gb_separator.Name = "gb_separator";
            gb_separator.Size = new Size(311, 265);
            gb_separator.TabIndex = 36;
            gb_separator.TabStop = false;
            gb_separator.Text = "Lv1 선택";
            // 
            // dataGridView_lv1
            // 
            dataGridView_lv1.AllowUserToAddRows = false;
            dataGridView_lv1.AllowUserToDeleteRows = false;
            dataGridView_lv1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView_lv1.BackgroundColor = Color.White;
            dataGridView_lv1.BorderStyle = BorderStyle.Fixed3D;
            dataGridView_lv1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = SystemColors.Window;
            dataGridViewCellStyle2.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle2.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            dataGridView_lv1.DefaultCellStyle = dataGridViewCellStyle2;
            dataGridView_lv1.EnableHeadersVisualStyles = false;
            dataGridView_lv1.GridColor = Color.LightGray;
            dataGridView_lv1.Location = new Point(10, 75);
            dataGridView_lv1.MinimumSize = new Size(200, 120);
            dataGridView_lv1.Name = "dataGridView_lv1";
            dataGridView_lv1.Size = new Size(291, 180);
            dataGridView_lv1.TabIndex = 43;
            // 
            // new_lv1_word
            // 
            new_lv1_word.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            new_lv1_word.Location = new Point(10, 35);
            new_lv1_word.Name = "new_lv1_word";
            new_lv1_word.PlaceholderText = "신규 항목 입력";
            new_lv1_word.Size = new Size(149, 33);
            new_lv1_word.TabIndex = 27;
            new_lv1_word.KeyDown += new_lv1_word_KeyDown;
            // 
            // lv1_del_btn
            // 
            lv1_del_btn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lv1_del_btn.AutoSize = true;
            lv1_del_btn.Font = new Font("맑은 고딕", 14.25F);
            lv1_del_btn.Location = new Point(241, 33);
            lv1_del_btn.Name = "lv1_del_btn";
            lv1_del_btn.Size = new Size(60, 35);
            lv1_del_btn.TabIndex = 24;
            lv1_del_btn.Text = "제거";
            lv1_del_btn.UseVisualStyleBackColor = true;
            lv1_del_btn.Click += lv1_del_btn_Click;
            // 
            // lv1_add_btn
            // 
            lv1_add_btn.AutoSize = true;
            lv1_add_btn.Font = new Font("맑은 고딕", 14.25F);
            lv1_add_btn.Location = new Point(175, 33);
            lv1_add_btn.Name = "lv1_add_btn";
            lv1_add_btn.Size = new Size(60, 35);
            lv1_add_btn.TabIndex = 23;
            lv1_add_btn.Text = "추가";
            lv1_add_btn.UseVisualStyleBackColor = true;
            lv1_add_btn.Click += lv1_add_btn_Click;
            // 
            // dataGridView_modified
            // 
            dataGridView_modified.AllowUserToAddRows = false;
            dataGridView_modified.AllowUserToDeleteRows = false;
            dataGridView_modified.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView_modified.BackgroundColor = Color.White;
            dataGridView_modified.BorderStyle = BorderStyle.Fixed3D;
            dataGridView_modified.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView_modified.EnableHeadersVisualStyles = false;
            dataGridView_modified.GridColor = Color.LightGray;
            dataGridView_modified.Location = new Point(10, 65);
            dataGridView_modified.MinimumSize = new Size(300, 100);
            dataGridView_modified.Name = "dataGridView_modified";
            dataGridView_modified.ReadOnly = true;
            dataGridView_modified.Size = new Size(617, 140);
            dataGridView_modified.TabIndex = 23;
            // 
            // groupbox2
            // 
            groupbox2.Controls.Add(button2);
            groupbox2.Controls.Add(union_cluster_btn);
            groupbox2.Controls.Add(label7);
            groupbox2.Controls.Add(complete_btn);
            groupbox2.Controls.Add(check_search_radio2);
            groupbox2.Controls.Add(check_search_radio1);
            groupbox2.Controls.Add(check_search_combo);
            groupbox2.Controls.Add(check_search_keyword);
            groupbox2.Controls.Add(merge_cancel_button);
            groupbox2.Controls.Add(check_search_button);
            groupbox2.Controls.Add(merge_check_table);
            groupbox2.Dock = DockStyle.Fill;
            groupbox2.Font = new Font("맑은 고딕", 15.75F);
            groupbox2.Location = new Point(5, 503);
            groupbox2.Margin = new Padding(5);
            groupbox2.Name = "groupbox2";
            groupbox2.Size = new Size(637, 489);
            groupbox2.TabIndex = 44;
            groupbox2.TabStop = false;
            groupbox2.Text = "Clustering 병합 결과 확인";
            // 
            // button2
            // 
            button2.AutoSize = true;
            button2.Font = new Font("맑은 고딕", 14.25F);
            button2.Location = new Point(10, 35);
            button2.Name = "button2";
            button2.Size = new Size(195, 35);
            button2.TabIndex = 49;
            button2.Text = "선택 항목 상세 보기";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // union_cluster_btn
            // 
            union_cluster_btn.AutoSize = true;
            union_cluster_btn.Font = new Font("맑은 고딕", 14.25F);
            union_cluster_btn.Location = new Point(220, 35);
            union_cluster_btn.Name = "union_cluster_btn";
            union_cluster_btn.Size = new Size(195, 35);
            union_cluster_btn.TabIndex = 48;
            union_cluster_btn.Text = "선택 항목 간 병합";
            union_cluster_btn.UseVisualStyleBackColor = true;
            union_cluster_btn.Click += union_cluster_btn_Click;
            // 
            // label7
            // 
            label7.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label7.AutoSize = true;
            label7.Font = new Font("맑은 고딕", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            label7.ForeColor = Color.IndianRed;
            label7.Location = new Point(10, 420);
            label7.Name = "label7";
            label7.Size = new Size(249, 17);
            label7.TabIndex = 47;
            label7.Text = "※ 클러스터명은 직접 수정이 가능합니다.";
            // 
            // complete_btn
            // 
            complete_btn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            complete_btn.AutoSize = true;
            complete_btn.Font = new Font("맑은 고딕", 14.25F);
            complete_btn.Location = new Point(545, 445);
            complete_btn.Name = "complete_btn";
            complete_btn.Size = new Size(80, 35);
            complete_btn.TabIndex = 45;
            complete_btn.Text = "완료";
            complete_btn.UseVisualStyleBackColor = true;
            complete_btn.Click += complete_btn_Click;
            // 
            // check_search_radio2
            // 
            check_search_radio2.AutoSize = true;
            check_search_radio2.Font = new Font("맑은 고딕", 14.25F);
            check_search_radio2.Location = new Point(250, 110);
            check_search_radio2.Name = "check_search_radio2";
            check_search_radio2.Size = new Size(132, 29);
            check_search_radio2.TabIndex = 33;
            check_search_radio2.Text = "키워드 입력";
            check_search_radio2.UseVisualStyleBackColor = true;
            check_search_radio2.CheckedChanged += check_search_radio2_CheckedChanged;
            // 
            // check_search_radio1
            // 
            check_search_radio1.AutoSize = true;
            check_search_radio1.Checked = true;
            check_search_radio1.Font = new Font("맑은 고딕", 14.25F);
            check_search_radio1.Location = new Point(250, 75);
            check_search_radio1.Name = "check_search_radio1";
            check_search_radio1.Size = new Size(132, 29);
            check_search_radio1.TabIndex = 32;
            check_search_radio1.TabStop = true;
            check_search_radio1.Text = "키워드 선택";
            check_search_radio1.UseVisualStyleBackColor = true;
            check_search_radio1.CheckedChanged += check_search_radio1_CheckedChanged;
            // 
            // check_search_combo
            // 
            check_search_combo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            check_search_combo.Font = new Font("맑은 고딕", 14.25F);
            check_search_combo.FormattingEnabled = true;
            check_search_combo.Location = new Point(390, 73);
            check_search_combo.Name = "check_search_combo";
            check_search_combo.Size = new Size(235, 33);
            check_search_combo.TabIndex = 31;
            check_search_combo.Text = "키워드 선택";
            // 
            // check_search_keyword
            // 
            check_search_keyword.Enabled = false;
            check_search_keyword.Font = new Font("맑은 고딕", 14.25F);
            check_search_keyword.Location = new Point(390, 108);
            check_search_keyword.Name = "check_search_keyword";
            check_search_keyword.PlaceholderText = "키워드 입력";
            check_search_keyword.Size = new Size(140, 33);
            check_search_keyword.TabIndex = 30;
            check_search_keyword.KeyDown += check_search_keyword_KeyDown;
            // 
            // merge_cancel_button
            // 
            merge_cancel_button.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            merge_cancel_button.AutoSize = true;
            merge_cancel_button.Font = new Font("맑은 고딕", 14.25F);
            merge_cancel_button.Location = new Point(10, 445);
            merge_cancel_button.Name = "merge_cancel_button";
            merge_cancel_button.Size = new Size(195, 35);
            merge_cancel_button.TabIndex = 26;
            merge_cancel_button.Text = "선택 항목 병합 해제";
            merge_cancel_button.UseVisualStyleBackColor = true;
            merge_cancel_button.Click += merge_cancel_button_Click;
            // 
            // check_search_button
            // 
            check_search_button.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            check_search_button.AutoSize = true;
            check_search_button.Font = new Font("맑은 고딕", 14.25F);
            check_search_button.Location = new Point(545, 108);
            check_search_button.Name = "check_search_button";
            check_search_button.Size = new Size(80, 35);
            check_search_button.TabIndex = 24;
            check_search_button.Text = "검색";
            check_search_button.UseVisualStyleBackColor = true;
            check_search_button.Click += check_search_button_Click;
            // 
            // merge_check_table
            // 
            merge_check_table.AllowUserToAddRows = false;
            merge_check_table.AllowUserToDeleteRows = false;
            merge_check_table.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            merge_check_table.BackgroundColor = Color.White;
            merge_check_table.BorderStyle = BorderStyle.Fixed3D;
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = SystemColors.Control;
            dataGridViewCellStyle3.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle3.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.True;
            merge_check_table.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            merge_check_table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = SystemColors.Window;
            dataGridViewCellStyle4.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle4.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle4.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = DataGridViewTriState.False;
            merge_check_table.DefaultCellStyle = dataGridViewCellStyle4;
            merge_check_table.EnableHeadersVisualStyles = false;
            merge_check_table.GridColor = Color.LightGray;
            merge_check_table.Location = new Point(10, 150);
            merge_check_table.MinimumSize = new Size(300, 200);
            merge_check_table.Name = "merge_check_table";
            merge_check_table.ReadOnly = true;
            dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = SystemColors.Control;
            dataGridViewCellStyle5.Font = new Font("돋움체", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle5.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle5.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = DataGridViewTriState.True;
            merge_check_table.RowHeadersDefaultCellStyle = dataGridViewCellStyle5;
            merge_check_table.Size = new Size(615, 260);
            merge_check_table.TabIndex = 23;
            // 
            // uc_Clustering
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            Controls.Add(tableLayoutMain);
            MinimumSize = new Size(1280, 800);
            Name = "uc_Clustering";
            Size = new Size(1904, 1017);
            tableLayoutMain.ResumeLayout(false);
            pnl_left.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            tableLayoutLeft.ResumeLayout(false);
            pnl_merge_header.ResumeLayout(false);
            pnl_merge_header.PerformLayout();
            groupBox5.ResumeLayout(false);
            groupBox5.PerformLayout();
            pnl_merge_controls.ResumeLayout(false);
            pnl_merge_controls.PerformLayout();
            pnl_merge_data.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)merge_cluster_table).EndInit();
            pnl_merge_pagination.ResumeLayout(false);
            pnl_merge_pagination.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)num_pageNumber).EndInit();
            pnl_right.ResumeLayout(false);
            tableLayoutRight.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            groupBox4.ResumeLayout(false);
            groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_recoman_keyword).EndInit();
            gb_separator.ResumeLayout(false);
            gb_separator.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_lv1).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView_modified).EndInit();
            groupbox2.ResumeLayout(false);
            groupbox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)merge_check_table).EndInit();
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
