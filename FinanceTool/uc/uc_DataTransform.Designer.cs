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
            tableLayoutMain = new TableLayoutPanel();
            pnl_left = new Panel();
            tableLayoutLeft = new TableLayoutPanel();
            pnl_original_header = new Panel();
            label10 = new Label();
            pnl_original_data = new Panel();
            dataGridView_2nd = new DataGridView();
            pnl_original_pagination = new Panel();
            btn_nextPage = new Button();
            btn_prevPage = new Button();
            cmb_pageSize = new ComboBox();
            lbl_pageSizeText = new Label();
            lbl_pagination = new Label();
            num_pageNumber = new NumericUpDown();
            lbl_pagination2 = new Label();
            pnl_transform_header = new Panel();
            label1 = new Label();
            pnl_transform_data = new Panel();
            dataGridView_transform = new DataGridView();
            pnl_transform_pagination = new Panel();
            btn_nextPage2 = new Button();
            btn_prevPage2 = new Button();
            cmb_pageSize2 = new ComboBox();
            label6 = new Label();
            lbl_pagination3 = new Label();
            num_pageNumber2 = new NumericUpDown();
            lbl_pagination4 = new Label();
            pnl_right = new Panel();
            tableLayoutRight = new TableLayoutPanel();
            groupBox1 = new GroupBox();
            sum_keyword_table = new DataGridView();
            label5 = new Label();
            decimal_combo = new ComboBox();
            groupbox2 = new GroupBox();
            modified_keyword = new TextBox();
            label4 = new Label();
            search_keyword = new TextBox();
            check_all_keyword_list = new CheckBox();
            change_keyword = new Button();
            keyword_search_button = new Button();
            match_keyword_table = new DataGridView();
            groupBox3 = new GroupBox();
            prod_col_check = new CheckBox();
            button5 = new Button();
            label3 = new Label();
            button2 = new Button();
            dept_col_check = new CheckBox();
            tableLayoutMain.SuspendLayout();
            pnl_left.SuspendLayout();
            tableLayoutLeft.SuspendLayout();
            pnl_original_header.SuspendLayout();
            pnl_original_data.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_2nd).BeginInit();
            pnl_original_pagination.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)num_pageNumber).BeginInit();
            pnl_transform_header.SuspendLayout();
            pnl_transform_data.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_transform).BeginInit();
            pnl_transform_pagination.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)num_pageNumber2).BeginInit();
            pnl_right.SuspendLayout();
            tableLayoutRight.SuspendLayout();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)sum_keyword_table).BeginInit();
            groupbox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)match_keyword_table).BeginInit();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutMain
            // 
            tableLayoutMain.ColumnCount = 2;
            tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
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
            pnl_left.Controls.Add(tableLayoutLeft);
            pnl_left.Dock = DockStyle.Fill;
            pnl_left.Location = new Point(10, 10);
            pnl_left.Margin = new Padding(10);
            pnl_left.Name = "pnl_left";
            pnl_left.Size = new Size(1122, 997);
            pnl_left.TabIndex = 0;
            // 
            // tableLayoutLeft
            // 
            tableLayoutLeft.ColumnCount = 1;
            tableLayoutLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutLeft.Controls.Add(pnl_original_header, 0, 0);
            tableLayoutLeft.Controls.Add(pnl_original_data, 0, 1);
            tableLayoutLeft.Controls.Add(pnl_original_pagination, 0, 2);
            tableLayoutLeft.Controls.Add(pnl_transform_header, 0, 3);
            tableLayoutLeft.Controls.Add(pnl_transform_data, 0, 4);
            tableLayoutLeft.Controls.Add(pnl_transform_pagination, 0, 5);
            tableLayoutLeft.Dock = DockStyle.Fill;
            tableLayoutLeft.Location = new Point(0, 0);
            tableLayoutLeft.Name = "tableLayoutLeft";
            tableLayoutLeft.RowCount = 6;
            tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutLeft.Size = new Size(1122, 997);
            tableLayoutLeft.TabIndex = 0;
            // 
            // pnl_original_header
            // 
            pnl_original_header.Controls.Add(label10);
            pnl_original_header.Dock = DockStyle.Fill;
            pnl_original_header.Location = new Point(3, 3);
            pnl_original_header.Name = "pnl_original_header";
            pnl_original_header.Size = new Size(1116, 44);
            pnl_original_header.TabIndex = 0;
            // 
            // label10
            // 
            label10.Anchor = AnchorStyles.None;
            label10.BackColor = Color.SteelBlue;
            label10.Font = new Font("맑은 고딕", 18F, FontStyle.Bold);
            label10.ForeColor = Color.White;
            label10.Location = new Point(383, 0);
            label10.Name = "label10";
            label10.Size = new Size(380, 40);
            label10.TabIndex = 48;
            label10.Text = "원본 키워드 데이터";
            label10.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pnl_original_data
            // 
            pnl_original_data.Controls.Add(dataGridView_2nd);
            pnl_original_data.Dock = DockStyle.Fill;
            pnl_original_data.Location = new Point(5, 55);
            pnl_original_data.Margin = new Padding(5);
            pnl_original_data.Name = "pnl_original_data";
            pnl_original_data.Size = new Size(1112, 388);
            pnl_original_data.TabIndex = 1;
            // 
            // dataGridView_2nd
            // 
            dataGridView_2nd.AllowUserToAddRows = false;
            dataGridView_2nd.AllowUserToDeleteRows = false;
            dataGridView_2nd.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView_2nd.BackgroundColor = Color.White;
            dataGridView_2nd.BorderStyle = BorderStyle.Fixed3D;
            dataGridView_2nd.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView_2nd.EnableHeadersVisualStyles = false;
            dataGridView_2nd.GridColor = Color.LightGray;
            dataGridView_2nd.Location = new Point(5, 5);
            dataGridView_2nd.MinimumSize = new Size(400, 200);
            dataGridView_2nd.Name = "dataGridView_2nd";
            dataGridView_2nd.ReadOnly = true;
            dataGridView_2nd.Size = new Size(1102, 378);
            dataGridView_2nd.TabIndex = 0;
            // 
            // pnl_original_pagination
            // 
            pnl_original_pagination.Controls.Add(btn_nextPage);
            pnl_original_pagination.Controls.Add(btn_prevPage);
            pnl_original_pagination.Controls.Add(cmb_pageSize);
            pnl_original_pagination.Controls.Add(lbl_pageSizeText);
            pnl_original_pagination.Controls.Add(lbl_pagination);
            pnl_original_pagination.Controls.Add(num_pageNumber);
            pnl_original_pagination.Controls.Add(lbl_pagination2);
            pnl_original_pagination.Dock = DockStyle.Fill;
            pnl_original_pagination.Location = new Point(3, 451);
            pnl_original_pagination.Name = "pnl_original_pagination";
            pnl_original_pagination.Size = new Size(1116, 44);
            pnl_original_pagination.TabIndex = 2;
            // 
            // btn_nextPage
            // 
            btn_nextPage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btn_nextPage.AutoSize = true;
            btn_nextPage.Font = new Font("맑은 고딕", 14.25F);
            btn_nextPage.Location = new Point(1014, 8);
            btn_nextPage.Name = "btn_nextPage";
            btn_nextPage.Size = new Size(86, 35);
            btn_nextPage.TabIndex = 60;
            btn_nextPage.Text = "다음 ▶";
            btn_nextPage.UseVisualStyleBackColor = true;
            // 
            // btn_prevPage
            // 
            btn_prevPage.AutoSize = true;
            btn_prevPage.Font = new Font("맑은 고딕", 14.25F);
            btn_prevPage.Location = new Point(450, 7);
            btn_prevPage.Name = "btn_prevPage";
            btn_prevPage.Size = new Size(86, 35);
            btn_prevPage.TabIndex = 59;
            btn_prevPage.Text = "◀ 이전";
            btn_prevPage.UseVisualStyleBackColor = true;
            // 
            // cmb_pageSize
            // 
            cmb_pageSize.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            cmb_pageSize.FormattingEnabled = true;
            cmb_pageSize.Location = new Point(135, 10);
            cmb_pageSize.Name = "cmb_pageSize";
            cmb_pageSize.Size = new Size(121, 33);
            cmb_pageSize.TabIndex = 58;
            // 
            // lbl_pageSizeText
            // 
            lbl_pageSizeText.AutoSize = true;
            lbl_pageSizeText.Font = new Font("맑은 고딕", 14F);
            lbl_pageSizeText.Location = new Point(4, 13);
            lbl_pageSizeText.Name = "lbl_pageSizeText";
            lbl_pageSizeText.Size = new Size(125, 25);
            lbl_pageSizeText.TabIndex = 57;
            lbl_pageSizeText.Text = "페이지 크기 :";
            // 
            // lbl_pagination
            // 
            lbl_pagination.AutoSize = true;
            lbl_pagination.Font = new Font("맑은 고딕", 14F);
            lbl_pagination.Location = new Point(542, 12);
            lbl_pagination.Name = "lbl_pagination";
            lbl_pagination.Size = new Size(80, 25);
            lbl_pagination.TabIndex = 54;
            lbl_pagination.Text = "페이지 :";
            // 
            // num_pageNumber
            // 
            num_pageNumber.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            num_pageNumber.Location = new Point(635, 10);
            num_pageNumber.Name = "num_pageNumber";
            num_pageNumber.Size = new Size(52, 33);
            num_pageNumber.TabIndex = 56;
            // 
            // lbl_pagination2
            // 
            lbl_pagination2.AutoSize = true;
            lbl_pagination2.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            lbl_pagination2.Location = new Point(693, 13);
            lbl_pagination2.Name = "lbl_pagination2";
            lbl_pagination2.Size = new Size(118, 25);
            lbl_pagination2.TabIndex = 55;
            lbl_pagination2.Text = "/ 0 (총 0 행)";
            // 
            // pnl_transform_header
            // 
            pnl_transform_header.Controls.Add(label1);
            pnl_transform_header.Dock = DockStyle.Fill;
            pnl_transform_header.Location = new Point(3, 501);
            pnl_transform_header.Name = "pnl_transform_header";
            pnl_transform_header.Size = new Size(1116, 44);
            pnl_transform_header.TabIndex = 3;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.None;
            label1.BackColor = Color.SteelBlue;
            label1.Font = new Font("맑은 고딕", 18F, FontStyle.Bold);
            label1.ForeColor = Color.White;
            label1.Location = new Point(383, 4);
            label1.Name = "label1";
            label1.Size = new Size(380, 40);
            label1.TabIndex = 49;
            label1.Text = "변환 키워드 데이터";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pnl_transform_data
            // 
            pnl_transform_data.Controls.Add(dataGridView_transform);
            pnl_transform_data.Dock = DockStyle.Fill;
            pnl_transform_data.Location = new Point(5, 553);
            pnl_transform_data.Margin = new Padding(5);
            pnl_transform_data.Name = "pnl_transform_data";
            pnl_transform_data.Size = new Size(1112, 388);
            pnl_transform_data.TabIndex = 4;
            // 
            // dataGridView_transform
            // 
            dataGridView_transform.AllowUserToAddRows = false;
            dataGridView_transform.AllowUserToDeleteRows = false;
            dataGridView_transform.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView_transform.BackgroundColor = Color.White;
            dataGridView_transform.BorderStyle = BorderStyle.Fixed3D;
            dataGridView_transform.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView_transform.EnableHeadersVisualStyles = false;
            dataGridView_transform.GridColor = Color.LightGray;
            dataGridView_transform.Location = new Point(5, 5);
            dataGridView_transform.MinimumSize = new Size(400, 200);
            dataGridView_transform.Name = "dataGridView_transform";
            dataGridView_transform.ReadOnly = true;
            dataGridView_transform.Size = new Size(1102, 378);
            dataGridView_transform.TabIndex = 0;
            // 
            // pnl_transform_pagination
            // 
            pnl_transform_pagination.Controls.Add(btn_nextPage2);
            pnl_transform_pagination.Controls.Add(btn_prevPage2);
            pnl_transform_pagination.Controls.Add(cmb_pageSize2);
            pnl_transform_pagination.Controls.Add(label6);
            pnl_transform_pagination.Controls.Add(lbl_pagination3);
            pnl_transform_pagination.Controls.Add(num_pageNumber2);
            pnl_transform_pagination.Controls.Add(lbl_pagination4);
            pnl_transform_pagination.Dock = DockStyle.Fill;
            pnl_transform_pagination.Location = new Point(3, 949);
            pnl_transform_pagination.Name = "pnl_transform_pagination";
            pnl_transform_pagination.Size = new Size(1116, 45);
            pnl_transform_pagination.TabIndex = 5;
            // 
            // btn_nextPage2
            // 
            btn_nextPage2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btn_nextPage2.AutoSize = true;
            btn_nextPage2.Font = new Font("맑은 고딕", 14.25F);
            btn_nextPage2.Location = new Point(1014, 8);
            btn_nextPage2.Name = "btn_nextPage2";
            btn_nextPage2.Size = new Size(86, 35);
            btn_nextPage2.TabIndex = 67;
            btn_nextPage2.Text = "다음 ▶";
            btn_nextPage2.UseVisualStyleBackColor = true;
            // 
            // btn_prevPage2
            // 
            btn_prevPage2.AutoSize = true;
            btn_prevPage2.Font = new Font("맑은 고딕", 14.25F);
            btn_prevPage2.Location = new Point(450, 7);
            btn_prevPage2.Name = "btn_prevPage2";
            btn_prevPage2.Size = new Size(86, 35);
            btn_prevPage2.TabIndex = 66;
            btn_prevPage2.Text = "◀ 이전";
            btn_prevPage2.UseVisualStyleBackColor = true;
            // 
            // cmb_pageSize2
            // 
            cmb_pageSize2.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            cmb_pageSize2.FormattingEnabled = true;
            cmb_pageSize2.Location = new Point(135, 10);
            cmb_pageSize2.Name = "cmb_pageSize2";
            cmb_pageSize2.Size = new Size(121, 33);
            cmb_pageSize2.TabIndex = 65;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("맑은 고딕", 14F);
            label6.Location = new Point(4, 13);
            label6.Name = "label6";
            label6.Size = new Size(125, 25);
            label6.TabIndex = 64;
            label6.Text = "페이지 크기 :";
            // 
            // lbl_pagination3
            // 
            lbl_pagination3.AutoSize = true;
            lbl_pagination3.Font = new Font("맑은 고딕", 14F);
            lbl_pagination3.Location = new Point(542, 12);
            lbl_pagination3.Name = "lbl_pagination3";
            lbl_pagination3.Size = new Size(80, 25);
            lbl_pagination3.TabIndex = 61;
            lbl_pagination3.Text = "페이지 :";
            // 
            // num_pageNumber2
            // 
            num_pageNumber2.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            num_pageNumber2.Location = new Point(635, 10);
            num_pageNumber2.Name = "num_pageNumber2";
            num_pageNumber2.Size = new Size(52, 33);
            num_pageNumber2.TabIndex = 63;
            // 
            // lbl_pagination4
            // 
            lbl_pagination4.AutoSize = true;
            lbl_pagination4.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            lbl_pagination4.Location = new Point(693, 13);
            lbl_pagination4.Name = "lbl_pagination4";
            lbl_pagination4.Size = new Size(118, 25);
            lbl_pagination4.TabIndex = 62;
            lbl_pagination4.Text = "/ 0 (총 0 행)";
            // 
            // pnl_right
            // 
            pnl_right.Controls.Add(tableLayoutRight);
            pnl_right.Dock = DockStyle.Fill;
            pnl_right.Location = new Point(1152, 10);
            pnl_right.Margin = new Padding(10);
            pnl_right.Name = "pnl_right";
            pnl_right.Size = new Size(742, 997);
            pnl_right.TabIndex = 1;
            // 
            // tableLayoutRight
            // 
            tableLayoutRight.ColumnCount = 1;
            tableLayoutRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutRight.Controls.Add(groupBox1, 0, 0);
            tableLayoutRight.Controls.Add(groupbox2, 0, 1);
            tableLayoutRight.Controls.Add(groupBox3, 0, 2);
            tableLayoutRight.Dock = DockStyle.Fill;
            tableLayoutRight.Location = new Point(0, 0);
            tableLayoutRight.Name = "tableLayoutRight";
            tableLayoutRight.RowCount = 3;
            tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 42.6278839F));
            tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 47.2417259F));
            tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            tableLayoutRight.Size = new Size(742, 997);
            tableLayoutRight.TabIndex = 0;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(sum_keyword_table);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(decimal_combo);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Font = new Font("맑은 고딕", 15.75F);
            groupBox1.Location = new Point(5, 5);
            groupBox1.Margin = new Padding(5);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(732, 415);
            groupBox1.TabIndex = 25;
            groupBox1.TabStop = false;
            groupBox1.Text = "키워드 요약";
            // 
            // sum_keyword_table
            // 
            sum_keyword_table.AllowUserToAddRows = false;
            sum_keyword_table.AllowUserToDeleteRows = false;
            sum_keyword_table.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            sum_keyword_table.BackgroundColor = Color.White;
            sum_keyword_table.BorderStyle = BorderStyle.Fixed3D;
            sum_keyword_table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Window;
            dataGridViewCellStyle1.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle1.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.False;
            sum_keyword_table.DefaultCellStyle = dataGridViewCellStyle1;
            sum_keyword_table.EnableHeadersVisualStyles = false;
            sum_keyword_table.GridColor = Color.LightGray;
            sum_keyword_table.Location = new Point(10, 80);
            sum_keyword_table.MinimumSize = new Size(300, 150);
            sum_keyword_table.Name = "sum_keyword_table";
            sum_keyword_table.ReadOnly = true;
            sum_keyword_table.Size = new Size(712, 325);
            sum_keyword_table.TabIndex = 36;
            sum_keyword_table.CellClick += dataGridView_modified_CellClick;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            label5.Location = new Point(10, 40);
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
            decimal_combo.Location = new Point(70, 37);
            decimal_combo.Name = "decimal_combo";
            decimal_combo.Size = new Size(100, 33);
            decimal_combo.TabIndex = 24;
            // 
            // groupbox2
            // 
            groupbox2.Controls.Add(modified_keyword);
            groupbox2.Controls.Add(label4);
            groupbox2.Controls.Add(search_keyword);
            groupbox2.Controls.Add(check_all_keyword_list);
            groupbox2.Controls.Add(change_keyword);
            groupbox2.Controls.Add(keyword_search_button);
            groupbox2.Controls.Add(match_keyword_table);
            groupbox2.Dock = DockStyle.Fill;
            groupbox2.Font = new Font("맑은 고딕", 15.75F);
            groupbox2.Location = new Point(5, 430);
            groupbox2.Margin = new Padding(5);
            groupbox2.Name = "groupbox2";
            groupbox2.Size = new Size(732, 461);
            groupbox2.TabIndex = 30;
            groupbox2.TabStop = false;
            groupbox2.Text = "키워드 변환";
            // 
            // modified_keyword
            // 
            modified_keyword.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            modified_keyword.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            modified_keyword.Location = new Point(350, 34);
            modified_keyword.Name = "modified_keyword";
            modified_keyword.PlaceholderText = "키워드 입력 가능";
            modified_keyword.Size = new Size(280, 33);
            modified_keyword.TabIndex = 34;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            label4.Location = new Point(150, 81);
            label4.Name = "label4";
            label4.Size = new Size(178, 25);
            label4.TabIndex = 30;
            label4.Text = "다음 키워드로 변경";
            // 
            // search_keyword
            // 
            search_keyword.Enabled = false;
            search_keyword.Font = new Font("맑은 고딕", 14.25F);
            search_keyword.Location = new Point(350, 73);
            search_keyword.Name = "search_keyword";
            search_keyword.PlaceholderText = "키워드 입력";
            search_keyword.Size = new Size(280, 33);
            search_keyword.TabIndex = 30;
            search_keyword.Visible = false;
            search_keyword.KeyDown += search_keyword_KeyDown;
            // 
            // check_all_keyword_list
            // 
            check_all_keyword_list.AutoSize = true;
            check_all_keyword_list.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            check_all_keyword_list.Location = new Point(10, 81);
            check_all_keyword_list.Name = "check_all_keyword_list";
            check_all_keyword_list.Size = new Size(114, 29);
            check_all_keyword_list.TabIndex = 29;
            check_all_keyword_list.Text = "전체 선택";
            check_all_keyword_list.UseVisualStyleBackColor = true;
            check_all_keyword_list.CheckedChanged += check_all_keyword_list_CheckedChanged;
            // 
            // change_keyword
            // 
            change_keyword.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            change_keyword.AutoSize = true;
            change_keyword.BackColor = Color.Orange;
            change_keyword.FlatStyle = FlatStyle.Flat;
            change_keyword.Font = new Font("맑은 고딕", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            change_keyword.ForeColor = Color.White;
            change_keyword.Location = new Point(650, 74);
            change_keyword.Name = "change_keyword";
            change_keyword.Size = new Size(69, 35);
            change_keyword.TabIndex = 26;
            change_keyword.Text = "변  환";
            change_keyword.UseVisualStyleBackColor = false;
            change_keyword.Click += change_keyword_Click;
            // 
            // keyword_search_button
            // 
            keyword_search_button.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            keyword_search_button.AutoSize = true;
            keyword_search_button.BackColor = Color.DodgerBlue;
            keyword_search_button.FlatStyle = FlatStyle.Flat;
            keyword_search_button.Font = new Font("맑은 고딕", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            keyword_search_button.ForeColor = Color.White;
            keyword_search_button.Location = new Point(650, 33);
            keyword_search_button.Name = "keyword_search_button";
            keyword_search_button.Size = new Size(69, 37);
            keyword_search_button.TabIndex = 24;
            keyword_search_button.Text = "검  색";
            keyword_search_button.UseVisualStyleBackColor = false;
            keyword_search_button.Click += keyword_search_button_Click;
            // 
            // match_keyword_table
            // 
            match_keyword_table.AllowUserToAddRows = false;
            match_keyword_table.AllowUserToDeleteRows = false;
            match_keyword_table.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            match_keyword_table.BackgroundColor = Color.White;
            match_keyword_table.BorderStyle = BorderStyle.Fixed3D;
            match_keyword_table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = SystemColors.Window;
            dataGridViewCellStyle2.Font = new Font("맑은 고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            dataGridViewCellStyle2.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            match_keyword_table.DefaultCellStyle = dataGridViewCellStyle2;
            match_keyword_table.EnableHeadersVisualStyles = false;
            match_keyword_table.GridColor = Color.LightGray;
            match_keyword_table.Location = new Point(15, 126);
            match_keyword_table.MinimumSize = new Size(300, 200);
            match_keyword_table.Name = "match_keyword_table";
            match_keyword_table.ReadOnly = true;
            match_keyword_table.Size = new Size(704, 329);
            match_keyword_table.TabIndex = 23;
            match_keyword_table.CellClick += match_keyword_table_CellClick;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(prod_col_check);
            groupBox3.Controls.Add(button5);
            groupBox3.Controls.Add(label3);
            groupBox3.Controls.Add(button2);
            groupBox3.Controls.Add(dept_col_check);
            groupBox3.Dock = DockStyle.Fill;
            groupBox3.Font = new Font("맑은 고딕", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 129);
            groupBox3.Location = new Point(5, 901);
            groupBox3.Margin = new Padding(5);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(732, 91);
            groupBox3.TabIndex = 36;
            groupBox3.TabStop = false;
            groupBox3.Text = "Cluster 설정";
            // 
            // prod_col_check
            // 
            prod_col_check.AutoSize = true;
            prod_col_check.Checked = true;
            prod_col_check.CheckState = CheckState.Checked;
            prod_col_check.Font = new Font("맑은 고딕", 14.25F);
            prod_col_check.Location = new Point(240, 35);
            prod_col_check.Name = "prod_col_check";
            prod_col_check.Size = new Size(107, 29);
            prod_col_check.TabIndex = 29;
            prod_col_check.Text = "공급업체";
            prod_col_check.UseVisualStyleBackColor = true;
            prod_col_check.CheckedChanged += prod_col_check_CheckedChanged;
            // 
            // button5
            // 
            button5.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button5.AutoSize = true;
            button5.BackColor = Color.LimeGreen;
            button5.FlatStyle = FlatStyle.Flat;
            button5.Font = new Font("맑은 고딕", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            button5.ForeColor = Color.White;
            button5.Location = new Point(600, 30);
            button5.Name = "button5";
            button5.Size = new Size(122, 37);
            button5.TabIndex = 35;
            button5.Text = "완  료";
            button5.UseVisualStyleBackColor = false;
            button5.Click += button5_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("맑은 고딕", 14.25F);
            label3.Location = new Point(10, 37);
            label3.Name = "label3";
            label3.Size = new Size(95, 25);
            label3.TabIndex = 26;
            label3.Text = "필수 포함";
            // 
            // button2
            // 
            button2.AutoSize = true;
            button2.BackColor = Color.Magenta;
            button2.FlatStyle = FlatStyle.Flat;
            button2.Font = new Font("맑은 고딕", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            button2.ForeColor = Color.White;
            button2.Location = new Point(450, 30);
            button2.Name = "button2";
            button2.Size = new Size(127, 35);
            button2.TabIndex = 26;
            button2.Text = "Cluster 확인";
            button2.UseVisualStyleBackColor = false;
            button2.Click += button2_Click;
            // 
            // dept_col_check
            // 
            dept_col_check.AutoSize = true;
            dept_col_check.Checked = true;
            dept_col_check.CheckState = CheckState.Checked;
            dept_col_check.Font = new Font("맑은 고딕", 14.25F);
            dept_col_check.Location = new Point(115, 35);
            dept_col_check.Name = "dept_col_check";
            dept_col_check.Size = new Size(126, 29);
            dept_col_check.TabIndex = 28;
            dept_col_check.Text = "코스트센터";
            dept_col_check.UseVisualStyleBackColor = true;
            dept_col_check.CheckedChanged += dept_col_check_CheckedChanged;
            // 
            // uc_DataTransform
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            Controls.Add(tableLayoutMain);
            MinimumSize = new Size(1280, 800);
            Name = "uc_DataTransform";
            Size = new Size(1904, 1017);
            tableLayoutMain.ResumeLayout(false);
            pnl_left.ResumeLayout(false);
            tableLayoutLeft.ResumeLayout(false);
            pnl_original_header.ResumeLayout(false);
            pnl_original_data.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView_2nd).EndInit();
            pnl_original_pagination.ResumeLayout(false);
            pnl_original_pagination.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)num_pageNumber).EndInit();
            pnl_transform_header.ResumeLayout(false);
            pnl_transform_data.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView_transform).EndInit();
            pnl_transform_pagination.ResumeLayout(false);
            pnl_transform_pagination.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)num_pageNumber2).EndInit();
            pnl_right.ResumeLayout(false);
            tableLayoutRight.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)sum_keyword_table).EndInit();
            groupbox2.ResumeLayout(false);
            groupbox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)match_keyword_table).EndInit();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
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
        private Panel pnl_original_data;
        private Panel pnl_transform_data;
        private Panel pnl_original_header;
        private Panel pnl_transform_header;
        private Panel pnl_original_pagination;
        private Panel pnl_transform_pagination;

        // 기존 컨트롤들
        private DataGridView dataGridView_2nd;
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
        private Label label10;
        private Label label1;
    }
}
