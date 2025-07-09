namespace FinanceTool
{
    partial class uc_FileLoad
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
            pnl_header = new Panel();
            btn_selectFile = new Button();
            lbl_filename = new Label();
            tableLayoutContent = new TableLayoutPanel();
            tableLayoutLeft = new TableLayoutPanel();
            pnl_data_headers = new Panel();
            label9 = new Label();
            label8 = new Label();
            pnl_data_grids = new Panel();
            dataGridView_process = new DataGridView();
            dataGridView_target = new DataGridView();
            pnl_pagination = new Panel();
            btn_nextPage = new Button();
            lbl_pagination2 = new Label();
            num_pageNumber = new NumericUpDown();
            lbl_pagination = new Label();
            btn_prevPage = new Button();
            cmb_pageSize = new ComboBox();
            lbl_pageSizeText = new Label();
            tableLayoutRight = new TableLayoutPanel();
            groupBox3 = new GroupBox();
            restore_col_btn = new Button();
            del_col_list_allcheck = new CheckBox();
            dataGridView_delete_col = new DataGridView();
            label7 = new Label();
            groupBox2 = new GroupBox();
            delete_data_btn = new Button();
            restore_del_data_btn = new Button();
            del_data_list_allcheck = new CheckBox();
            dataGridView_delete_data = new DataGridView();
            delete_search_button = new Button();
            delete_search_keyword = new TextBox();
            stand_col_combo = new ComboBox();
            label4 = new Label();
            groupBox1 = new GroupBox();
            btn_complete = new Button();
            prod_col_combo = new ComboBox();
            label10 = new Label();
            cmb_target = new ComboBox();
            label1 = new Label();
            cmb_money = new ComboBox();
            label3 = new Label();
            dept_col_combo = new ComboBox();
            label6 = new Label();
            sub_acc_col_combo = new ComboBox();
            label5 = new Label();
            label2 = new Label();
            dataGridView2 = new DataGridView();
            tableLayoutMain.SuspendLayout();
            pnl_header.SuspendLayout();
            tableLayoutContent.SuspendLayout();
            tableLayoutLeft.SuspendLayout();
            pnl_data_headers.SuspendLayout();
            pnl_data_grids.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_process).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView_target).BeginInit();
            pnl_pagination.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)num_pageNumber).BeginInit();
            tableLayoutRight.SuspendLayout();
            groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_delete_col).BeginInit();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_delete_data).BeginInit();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView2).BeginInit();
            SuspendLayout();
            // 
            // tableLayoutMain
            // 
            tableLayoutMain.ColumnCount = 1;
            tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutMain.Controls.Add(pnl_header, 0, 0);
            tableLayoutMain.Controls.Add(tableLayoutContent, 0, 1);
            tableLayoutMain.Dock = DockStyle.Fill;
            tableLayoutMain.Location = new Point(0, 0);
            tableLayoutMain.Margin = new Padding(0);
            tableLayoutMain.Name = "tableLayoutMain";
            tableLayoutMain.RowCount = 2;
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutMain.Size = new Size(1904, 1017);
            tableLayoutMain.TabIndex = 0;
            // 
            // pnl_header
            // 
            pnl_header.BackColor = Color.WhiteSmoke;
            pnl_header.Controls.Add(btn_selectFile);
            pnl_header.Controls.Add(lbl_filename);
            pnl_header.Dock = DockStyle.Fill;
            pnl_header.Location = new Point(0, 0);
            pnl_header.Margin = new Padding(0);
            pnl_header.Name = "pnl_header";
            pnl_header.Padding = new Padding(10);
            pnl_header.Size = new Size(1904, 100);
            pnl_header.TabIndex = 0;
            // 
            // btn_selectFile
            // 
            btn_selectFile.AutoSize = true;
            btn_selectFile.BackColor = Color.DodgerBlue;
            btn_selectFile.FlatStyle = FlatStyle.Flat;
            btn_selectFile.Font = new Font("맑은 고딕", 16F, FontStyle.Bold);
            btn_selectFile.ForeColor = Color.White;
            btn_selectFile.Location = new Point(15, 25);
            btn_selectFile.MinimumSize = new Size(120, 40);
            btn_selectFile.Name = "btn_selectFile";
            btn_selectFile.Size = new Size(150, 50);
            btn_selectFile.TabIndex = 1;
            btn_selectFile.Text = "파일 선택";
            btn_selectFile.UseVisualStyleBackColor = false;
            btn_selectFile.Visible = false;
            btn_selectFile.Click += btn_selectFile_Click;
            // 
            // lbl_filename
            // 
            lbl_filename.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lbl_filename.Font = new Font("맑은 고딕", 16F);
            lbl_filename.ForeColor = Color.DarkSlateGray;
            lbl_filename.Location = new Point(180, 35);
            lbl_filename.Name = "lbl_filename";
            lbl_filename.Size = new Size(1700, 30);
            lbl_filename.TabIndex = 0;
            lbl_filename.Text = "Excel 파일을 Upload 해주세요(.xls,xlsx) : ";
            lbl_filename.TextAlign = ContentAlignment.MiddleLeft;
            lbl_filename.Visible = false;
            // 
            // tableLayoutContent
            // 
            tableLayoutContent.ColumnCount = 2;
            tableLayoutContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72F));
            tableLayoutContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            tableLayoutContent.Controls.Add(tableLayoutLeft, 0, 0);
            tableLayoutContent.Controls.Add(tableLayoutRight, 1, 0);
            tableLayoutContent.Dock = DockStyle.Fill;
            tableLayoutContent.Location = new Point(0, 100);
            tableLayoutContent.Margin = new Padding(0);
            tableLayoutContent.Name = "tableLayoutContent";
            tableLayoutContent.RowCount = 1;
            tableLayoutContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutContent.Size = new Size(1904, 917);
            tableLayoutContent.TabIndex = 1;
            // 
            // tableLayoutLeft
            // 
            tableLayoutLeft.ColumnCount = 1;
            tableLayoutLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutLeft.Controls.Add(pnl_data_headers, 0, 0);
            tableLayoutLeft.Controls.Add(pnl_data_grids, 0, 1);
            tableLayoutLeft.Controls.Add(pnl_pagination, 0, 2);
            tableLayoutLeft.Dock = DockStyle.Fill;
            tableLayoutLeft.Location = new Point(10, 10);
            tableLayoutLeft.Margin = new Padding(10, 10, 5, 10);
            tableLayoutLeft.Name = "tableLayoutLeft";
            tableLayoutLeft.RowCount = 3;
            tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            tableLayoutLeft.Size = new Size(1355, 897);
            tableLayoutLeft.TabIndex = 0;
            // 
            // pnl_data_headers
            // 
            pnl_data_headers.Controls.Add(label9);
            pnl_data_headers.Controls.Add(label8);
            pnl_data_headers.Dock = DockStyle.Fill;
            pnl_data_headers.Location = new Point(0, 0);
            pnl_data_headers.Margin = new Padding(0);
            pnl_data_headers.Name = "pnl_data_headers";
            pnl_data_headers.Size = new Size(1355, 60);
            pnl_data_headers.TabIndex = 0;
            // 
            // label9
            // 
            label9.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label9.BackColor = Color.SteelBlue;
            label9.Font = new Font("맑은 고딕", 18F, FontStyle.Bold);
            label9.ForeColor = Color.White;
            label9.Location = new Point(677, 10);
            label9.Name = "label9";
            label9.Size = new Size(678, 40);
            label9.TabIndex = 46;
            label9.Text = "가공 데이터";
            label9.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label8
            // 
            label8.BackColor = Color.SteelBlue;
            label8.Font = new Font("맑은 고딕", 18F, FontStyle.Bold);
            label8.ForeColor = Color.White;
            label8.Location = new Point(0, 10);
            label8.Name = "label8";
            label8.Size = new Size(678, 40);
            label8.TabIndex = 45;
            label8.Text = "원본 데이터";
            label8.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pnl_data_grids
            // 
            pnl_data_grids.Controls.Add(dataGridView_process);
            pnl_data_grids.Controls.Add(dataGridView_target);
            pnl_data_grids.Dock = DockStyle.Fill;
            pnl_data_grids.Location = new Point(0, 60);
            pnl_data_grids.Margin = new Padding(0);
            pnl_data_grids.Name = "pnl_data_grids";
            pnl_data_grids.Size = new Size(1355, 777);
            pnl_data_grids.TabIndex = 1;
            // 
            // dataGridView_process
            // 
            dataGridView_process.AllowUserToAddRows = false;
            dataGridView_process.AllowUserToDeleteRows = false;
            dataGridView_process.AllowUserToResizeRows = false;
            dataGridView_process.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView_process.BackgroundColor = Color.White;
            dataGridView_process.BorderStyle = BorderStyle.Fixed3D;
            dataGridView_process.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView_process.Location = new Point(682, 0);
            dataGridView_process.MinimumSize = new Size(400, 300);
            dataGridView_process.Name = "dataGridView_process";
            dataGridView_process.ReadOnly = true;
            dataGridView_process.RowHeadersVisible = false;
            dataGridView_process.Size = new Size(673, 777);
            dataGridView_process.TabIndex = 20;
            // 
            // dataGridView_target
            // 
            dataGridView_target.AllowUserToAddRows = false;
            dataGridView_target.AllowUserToDeleteRows = false;
            dataGridView_target.AllowUserToResizeRows = false;
            dataGridView_target.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            dataGridView_target.BackgroundColor = Color.White;
            dataGridView_target.BorderStyle = BorderStyle.Fixed3D;
            dataGridView_target.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView_target.Location = new Point(0, 0);
            dataGridView_target.MinimumSize = new Size(400, 300);
            dataGridView_target.Name = "dataGridView_target";
            dataGridView_target.ReadOnly = true;
            dataGridView_target.RowHeadersVisible = false;
            dataGridView_target.Size = new Size(672, 777);
            dataGridView_target.TabIndex = 13;
            dataGridView_target.CellClick += dataGridView_target_CellClick;
            dataGridView_target.SelectionChanged += dataGridView_target_SelectionChanged;
            // 
            // pnl_pagination
            // 
            pnl_pagination.Controls.Add(btn_nextPage);
            pnl_pagination.Controls.Add(lbl_pagination2);
            pnl_pagination.Controls.Add(num_pageNumber);
            pnl_pagination.Controls.Add(lbl_pagination);
            pnl_pagination.Controls.Add(btn_prevPage);
            pnl_pagination.Controls.Add(cmb_pageSize);
            pnl_pagination.Controls.Add(lbl_pageSizeText);
            pnl_pagination.Dock = DockStyle.Fill;
            pnl_pagination.Location = new Point(0, 837);
            pnl_pagination.Margin = new Padding(0);
            pnl_pagination.Name = "pnl_pagination";
            pnl_pagination.Size = new Size(1355, 60);
            pnl_pagination.TabIndex = 2;
            // 
            // btn_nextPage
            // 
            btn_nextPage.Anchor = AnchorStyles.None;
            btn_nextPage.AutoSize = true;
            btn_nextPage.Font = new Font("맑은 고딕", 12F);
            btn_nextPage.Location = new Point(820, 12);
            btn_nextPage.Name = "btn_nextPage";
            btn_nextPage.Size = new Size(86, 35);
            btn_nextPage.TabIndex = 53;
            btn_nextPage.Text = "다음 ▶";
            btn_nextPage.UseVisualStyleBackColor = true;
            btn_nextPage.Click += btn_nextPage_Click;
            // 
            // lbl_pagination2
            // 
            lbl_pagination2.Anchor = AnchorStyles.None;
            lbl_pagination2.AutoSize = true;
            lbl_pagination2.Font = new Font("맑은 고딕", 12F);
            lbl_pagination2.Location = new Point(682, 19);
            lbl_pagination2.Name = "lbl_pagination2";
            lbl_pagination2.Size = new Size(100, 21);
            lbl_pagination2.TabIndex = 48;
            lbl_pagination2.Text = "/ 0 (총 0 행)";
            // 
            // num_pageNumber
            // 
            num_pageNumber.Anchor = AnchorStyles.None;
            num_pageNumber.Font = new Font("맑은 고딕", 12F);
            num_pageNumber.Location = new Point(629, 16);
            num_pageNumber.Name = "num_pageNumber";
            num_pageNumber.Size = new Size(52, 29);
            num_pageNumber.TabIndex = 49;
            // 
            // lbl_pagination
            // 
            lbl_pagination.Anchor = AnchorStyles.None;
            lbl_pagination.AutoSize = true;
            lbl_pagination.Font = new Font("맑은 고딕", 12F);
            lbl_pagination.Location = new Point(552, 19);
            lbl_pagination.Name = "lbl_pagination";
            lbl_pagination.Size = new Size(68, 21);
            lbl_pagination.TabIndex = 25;
            lbl_pagination.Text = "페이지 :";
            // 
            // btn_prevPage
            // 
            btn_prevPage.Anchor = AnchorStyles.None;
            btn_prevPage.AutoSize = true;
            btn_prevPage.Font = new Font("맑은 고딕", 12F);
            btn_prevPage.Location = new Point(448, 12);
            btn_prevPage.Name = "btn_prevPage";
            btn_prevPage.Size = new Size(86, 35);
            btn_prevPage.TabIndex = 52;
            btn_prevPage.Text = "◀ 이전";
            btn_prevPage.UseVisualStyleBackColor = true;
            btn_prevPage.Click += btn_prevPage_Click;
            // 
            // cmb_pageSize
            // 
            cmb_pageSize.Font = new Font("맑은 고딕", 12F);
            cmb_pageSize.FormattingEnabled = true;
            cmb_pageSize.Location = new Point(130, 16);
            cmb_pageSize.Name = "cmb_pageSize";
            cmb_pageSize.Size = new Size(121, 29);
            cmb_pageSize.TabIndex = 51;
            // 
            // lbl_pageSizeText
            // 
            lbl_pageSizeText.AutoSize = true;
            lbl_pageSizeText.Font = new Font("맑은 고딕", 12F);
            lbl_pageSizeText.Location = new Point(15, 19);
            lbl_pageSizeText.Name = "lbl_pageSizeText";
            lbl_pageSizeText.Size = new Size(106, 21);
            lbl_pageSizeText.TabIndex = 50;
            lbl_pageSizeText.Text = "페이지 크기 :";
            // 
            // tableLayoutRight
            // 
            tableLayoutRight.ColumnCount = 1;
            tableLayoutRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutRight.Controls.Add(groupBox3, 0, 0);
            tableLayoutRight.Controls.Add(groupBox2, 0, 1);
            tableLayoutRight.Controls.Add(groupBox1, 0, 2);
            tableLayoutRight.Dock = DockStyle.Fill;
            tableLayoutRight.Location = new Point(1375, 10);
            tableLayoutRight.Margin = new Padding(5, 10, 10, 10);
            tableLayoutRight.Name = "tableLayoutRight";
            tableLayoutRight.RowCount = 3;
            tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            tableLayoutRight.Size = new Size(519, 897);
            tableLayoutRight.TabIndex = 1;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(restore_col_btn);
            groupBox3.Controls.Add(del_col_list_allcheck);
            groupBox3.Controls.Add(dataGridView_delete_col);
            groupBox3.Controls.Add(label7);
            groupBox3.Dock = DockStyle.Fill;
            groupBox3.Font = new Font("맑은 고딕", 12F, FontStyle.Bold);
            groupBox3.Location = new Point(5, 5);
            groupBox3.Margin = new Padding(5);
            groupBox3.Name = "groupBox3";
            groupBox3.Padding = new Padding(8);
            groupBox3.Size = new Size(509, 259);
            groupBox3.TabIndex = 43;
            groupBox3.TabStop = false;
            groupBox3.Text = "제거 열 설정";
            // 
            // restore_col_btn
            // 
            restore_col_btn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            restore_col_btn.AutoSize = true;
            restore_col_btn.BackColor = Color.Orange;
            restore_col_btn.FlatStyle = FlatStyle.Flat;
            restore_col_btn.Font = new Font("맑은 고딕", 11F, FontStyle.Bold);
            restore_col_btn.ForeColor = Color.White;
            restore_col_btn.Location = new Point(361, 210);
            restore_col_btn.MinimumSize = new Size(100, 30);
            restore_col_btn.Name = "restore_col_btn";
            restore_col_btn.Size = new Size(140, 35);
            restore_col_btn.TabIndex = 14;
            restore_col_btn.Text = "선택 열 적용";
            restore_col_btn.UseVisualStyleBackColor = false;
            restore_col_btn.Click += restore_col_btn_Click;
            // 
            // del_col_list_allcheck
            // 
            del_col_list_allcheck.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            del_col_list_allcheck.AutoSize = true;
            del_col_list_allcheck.Font = new Font("맑은 고딕", 11F);
            del_col_list_allcheck.Location = new Point(406, 50);
            del_col_list_allcheck.Name = "del_col_list_allcheck";
            del_col_list_allcheck.Size = new Size(93, 24);
            del_col_list_allcheck.TabIndex = 43;
            del_col_list_allcheck.Text = "전체 선택";
            del_col_list_allcheck.UseVisualStyleBackColor = true;
            del_col_list_allcheck.CheckedChanged += del_col_list_allcheck_CheckedChanged;
            // 
            // dataGridView_delete_col
            // 
            dataGridView_delete_col.AllowUserToAddRows = false;
            dataGridView_delete_col.AllowUserToDeleteRows = false;
            dataGridView_delete_col.AllowUserToResizeRows = false;
            dataGridView_delete_col.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView_delete_col.BackgroundColor = Color.White;
            dataGridView_delete_col.BorderStyle = BorderStyle.Fixed3D;
            dataGridView_delete_col.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView_delete_col.Location = new Point(8, 50);
            dataGridView_delete_col.MinimumSize = new Size(300, 120);
            dataGridView_delete_col.Name = "dataGridView_delete_col";
            dataGridView_delete_col.RowHeadersVisible = false;
            dataGridView_delete_col.Size = new Size(351, 150);
            dataGridView_delete_col.TabIndex = 42;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            label7.ForeColor = Color.IndianRed;
            label7.Location = new Point(8, 25);
            label7.Name = "label7";
            label7.Size = new Size(244, 15);
            label7.TabIndex = 44;
            label7.Text = "※ 선택한 열 정보만 출력하도록 지원합니다.";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(delete_data_btn);
            groupBox2.Controls.Add(restore_del_data_btn);
            groupBox2.Controls.Add(del_data_list_allcheck);
            groupBox2.Controls.Add(dataGridView_delete_data);
            groupBox2.Controls.Add(delete_search_button);
            groupBox2.Controls.Add(delete_search_keyword);
            groupBox2.Controls.Add(stand_col_combo);
            groupBox2.Controls.Add(label4);
            groupBox2.Dock = DockStyle.Fill;
            groupBox2.Font = new Font("맑은 고딕", 12F, FontStyle.Bold);
            groupBox2.Location = new Point(5, 274);
            groupBox2.Margin = new Padding(5);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(8);
            groupBox2.Size = new Size(509, 259);
            groupBox2.TabIndex = 16;
            groupBox2.TabStop = false;
            groupBox2.Text = "데이터 삭제";
            // 
            // delete_data_btn
            // 
            delete_data_btn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            delete_data_btn.AutoSize = true;
            delete_data_btn.BackColor = Color.Crimson;
            delete_data_btn.FlatStyle = FlatStyle.Flat;
            delete_data_btn.Font = new Font("맑은 고딕", 11F, FontStyle.Bold);
            delete_data_btn.ForeColor = Color.White;
            delete_data_btn.Location = new Point(361, 210);
            delete_data_btn.MinimumSize = new Size(100, 30);
            delete_data_btn.Name = "delete_data_btn";
            delete_data_btn.Size = new Size(140, 35);
            delete_data_btn.TabIndex = 14;
            delete_data_btn.Text = "데이터 삭제";
            delete_data_btn.UseVisualStyleBackColor = false;
            delete_data_btn.Click += delete_data_btn_Click;
            // 
            // restore_del_data_btn
            // 
            restore_del_data_btn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            restore_del_data_btn.AutoSize = true;
            restore_del_data_btn.BackColor = Color.LimeGreen;
            restore_del_data_btn.FlatStyle = FlatStyle.Flat;
            restore_del_data_btn.Font = new Font("맑은 고딕", 11F, FontStyle.Bold);
            restore_del_data_btn.ForeColor = Color.White;
            restore_del_data_btn.Location = new Point(361, 169);
            restore_del_data_btn.MinimumSize = new Size(100, 30);
            restore_del_data_btn.Name = "restore_del_data_btn";
            restore_del_data_btn.Size = new Size(140, 35);
            restore_del_data_btn.TabIndex = 44;
            restore_del_data_btn.Text = "데이터 원복";
            restore_del_data_btn.UseVisualStyleBackColor = false;
            restore_del_data_btn.Click += restore_del_data_btn_Click;
            // 
            // del_data_list_allcheck
            // 
            del_data_list_allcheck.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            del_data_list_allcheck.AutoSize = true;
            del_data_list_allcheck.Font = new Font("맑은 고딕", 11F);
            del_data_list_allcheck.Location = new Point(406, 80);
            del_data_list_allcheck.Name = "del_data_list_allcheck";
            del_data_list_allcheck.Size = new Size(93, 24);
            del_data_list_allcheck.TabIndex = 44;
            del_data_list_allcheck.Text = "전체 선택";
            del_data_list_allcheck.UseVisualStyleBackColor = true;
            del_data_list_allcheck.CheckedChanged += del_data_list_allcheck_CheckedChanged;
            // 
            // dataGridView_delete_data
            // 
            dataGridView_delete_data.AllowUserToAddRows = false;
            dataGridView_delete_data.AllowUserToDeleteRows = false;
            dataGridView_delete_data.AllowUserToResizeRows = false;
            dataGridView_delete_data.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView_delete_data.BackgroundColor = Color.White;
            dataGridView_delete_data.BorderStyle = BorderStyle.Fixed3D;
            dataGridView_delete_data.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView_delete_data.Location = new Point(8, 80);
            dataGridView_delete_data.MinimumSize = new Size(300, 100);
            dataGridView_delete_data.Name = "dataGridView_delete_data";
            dataGridView_delete_data.ReadOnly = true;
            dataGridView_delete_data.RowHeadersVisible = false;
            dataGridView_delete_data.Size = new Size(351, 120);
            dataGridView_delete_data.TabIndex = 42;
            // 
            // delete_search_button
            // 
            delete_search_button.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            delete_search_button.AutoSize = true;
            delete_search_button.BackColor = Color.DodgerBlue;
            delete_search_button.FlatStyle = FlatStyle.Flat;
            delete_search_button.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            delete_search_button.ForeColor = Color.White;
            delete_search_button.Location = new Point(431, 52);
            delete_search_button.MinimumSize = new Size(60, 25);
            delete_search_button.Name = "delete_search_button";
            delete_search_button.Size = new Size(70, 31);
            delete_search_button.TabIndex = 45;
            delete_search_button.Text = "검색";
            delete_search_button.UseVisualStyleBackColor = false;
            delete_search_button.Click += delete_search_button_Click;
            // 
            // delete_search_keyword
            // 
            delete_search_keyword.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            delete_search_keyword.Font = new Font("맑은 고딕", 10F);
            delete_search_keyword.Location = new Point(260, 53);
            delete_search_keyword.Name = "delete_search_keyword";
            delete_search_keyword.PlaceholderText = "검색 키워드 입력";
            delete_search_keyword.Size = new Size(165, 25);
            delete_search_keyword.TabIndex = 46;
            delete_search_keyword.KeyDown += delete_search_keyword_KeyDown;
            // 
            // stand_col_combo
            // 
            stand_col_combo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            stand_col_combo.Font = new Font("맑은 고딕", 11F);
            stand_col_combo.FormattingEnabled = true;
            stand_col_combo.Location = new Point(115, 25);
            stand_col_combo.Name = "stand_col_combo";
            stand_col_combo.Size = new Size(386, 28);
            stand_col_combo.TabIndex = 2;
            stand_col_combo.SelectedIndexChanged += stand_col_combo_SelectedIndexChanged;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("맑은 고딕", 11F);
            label4.Location = new Point(8, 28);
            label4.Name = "label4";
            label4.Size = new Size(102, 20);
            label4.TabIndex = 3;
            label4.Text = "기준 열 선택 :";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(btn_complete);
            groupBox1.Controls.Add(prod_col_combo);
            groupBox1.Controls.Add(label10);
            groupBox1.Controls.Add(cmb_target);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(cmb_money);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(dept_col_combo);
            groupBox1.Controls.Add(label6);
            groupBox1.Controls.Add(sub_acc_col_combo);
            groupBox1.Controls.Add(label5);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Font = new Font("맑은 고딕", 12F, FontStyle.Bold);
            groupBox1.Location = new Point(5, 543);
            groupBox1.Margin = new Padding(5);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(8);
            groupBox1.Size = new Size(509, 349);
            groupBox1.TabIndex = 44;
            groupBox1.TabStop = false;
            groupBox1.Text = "필수 항목 설정";
            // 
            // btn_complete
            // 
            btn_complete.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btn_complete.AutoSize = true;
            btn_complete.BackColor = Color.LimeGreen;
            btn_complete.FlatStyle = FlatStyle.Flat;
            btn_complete.Font = new Font("맑은 고딕", 14F, FontStyle.Bold);
            btn_complete.ForeColor = Color.White;
            btn_complete.Location = new Point(351, 300);
            btn_complete.MinimumSize = new Size(100, 35);
            btn_complete.Name = "btn_complete";
            btn_complete.Size = new Size(150, 40);
            btn_complete.TabIndex = 38;
            btn_complete.Text = "완  료";
            btn_complete.UseVisualStyleBackColor = false;
            btn_complete.Click += btn_complete_Click;
            // 
            // prod_col_combo
            // 
            prod_col_combo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            prod_col_combo.Font = new Font("맑은 고딕", 11F);
            prod_col_combo.FormattingEnabled = true;
            prod_col_combo.Location = new Point(140, 155);
            prod_col_combo.Name = "prod_col_combo";
            prod_col_combo.Size = new Size(361, 28);
            prod_col_combo.TabIndex = 39;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Font = new Font("맑은 고딕", 11F);
            label10.Location = new Point(15, 158);
            label10.Name = "label10";
            label10.Size = new Size(97, 20);
            label10.TabIndex = 40;
            label10.Text = "공급업체 열 :";
            // 
            // cmb_target
            // 
            cmb_target.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmb_target.Font = new Font("맑은 고딕", 11F);
            cmb_target.FormattingEnabled = true;
            cmb_target.Location = new Point(140, 225);
            cmb_target.Name = "cmb_target";
            cmb_target.Size = new Size(361, 28);
            cmb_target.TabIndex = 14;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("맑은 고딕", 11F);
            label1.Location = new Point(15, 228);
            label1.Name = "label1";
            label1.Size = new Size(67, 20);
            label1.TabIndex = 15;
            label1.Text = "타겟 열 :";
            // 
            // cmb_money
            // 
            cmb_money.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmb_money.Font = new Font("맑은 고딕", 11F);
            cmb_money.FormattingEnabled = true;
            cmb_money.Location = new Point(140, 190);
            cmb_money.Name = "cmb_money";
            cmb_money.Size = new Size(361, 28);
            cmb_money.TabIndex = 18;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("맑은 고딕", 11F);
            label3.Location = new Point(15, 193);
            label3.Name = "label3";
            label3.Size = new Size(67, 20);
            label3.TabIndex = 19;
            label3.Text = "금액 열 :";
            // 
            // dept_col_combo
            // 
            dept_col_combo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dept_col_combo.Font = new Font("맑은 고딕", 11F);
            dept_col_combo.FormattingEnabled = true;
            dept_col_combo.Location = new Point(140, 120);
            dept_col_combo.Name = "dept_col_combo";
            dept_col_combo.Size = new Size(361, 28);
            dept_col_combo.TabIndex = 21;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("맑은 고딕", 11F);
            label6.Location = new Point(15, 123);
            label6.Name = "label6";
            label6.Size = new Size(117, 20);
            label6.TabIndex = 22;
            label6.Text = "코스트센터 열  :";
            // 
            // sub_acc_col_combo
            // 
            sub_acc_col_combo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            sub_acc_col_combo.Font = new Font("맑은 고딕", 11F);
            sub_acc_col_combo.FormattingEnabled = true;
            sub_acc_col_combo.Location = new Point(140, 85);
            sub_acc_col_combo.Name = "sub_acc_col_combo";
            sub_acc_col_combo.Size = new Size(361, 28);
            sub_acc_col_combo.TabIndex = 23;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("맑은 고딕", 11F);
            label5.Location = new Point(15, 88);
            label5.Name = "label5";
            label5.Size = new Size(67, 20);
            label5.TabIndex = 24;
            label5.Text = "세목 열 :";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("맑은 고딕", 14F);
            label2.Location = new Point(6, 41);
            label2.Name = "label2";
            label2.Size = new Size(132, 25);
            label2.TabIndex = 3;
            label2.Text = "제거 열 선택 :";
            label2.Visible = false;
            // 
            // dataGridView2
            // 
            dataGridView2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView2.Location = new Point(6, 77);
            dataGridView2.Name = "dataGridView2";
            dataGridView2.Size = new Size(458, 180);
            dataGridView2.TabIndex = 41;
            dataGridView2.Visible = false;
            // 
            // uc_FileLoad
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            Controls.Add(tableLayoutMain);
            MinimumSize = new Size(1280, 800);
            Name = "uc_FileLoad";
            Size = new Size(1904, 1017);
            Load += uc_FileLoad_Load;
            tableLayoutMain.ResumeLayout(false);
            pnl_header.ResumeLayout(false);
            pnl_header.PerformLayout();
            tableLayoutContent.ResumeLayout(false);
            tableLayoutLeft.ResumeLayout(false);
            pnl_data_headers.ResumeLayout(false);
            pnl_data_grids.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView_process).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView_target).EndInit();
            pnl_pagination.ResumeLayout(false);
            pnl_pagination.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)num_pageNumber).EndInit();
            tableLayoutRight.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_delete_col).EndInit();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_delete_data).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView2).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
        private System.Windows.Forms.Panel pnl_header;
        private System.Windows.Forms.Button btn_selectFile;
        private System.Windows.Forms.Label lbl_filename;
        private System.Windows.Forms.TableLayoutPanel tableLayoutContent;
        private System.Windows.Forms.TableLayoutPanel tableLayoutLeft;
        private System.Windows.Forms.Panel pnl_data_headers;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Panel pnl_data_grids;
        private System.Windows.Forms.DataGridView dataGridView_process;
        private System.Windows.Forms.DataGridView dataGridView_target;
        private System.Windows.Forms.Panel pnl_pagination;
        private System.Windows.Forms.Button btn_nextPage;
        private System.Windows.Forms.Label lbl_pagination2;
        private System.Windows.Forms.NumericUpDown num_pageNumber;
        private System.Windows.Forms.Label lbl_pagination;
        private System.Windows.Forms.Button btn_prevPage;
        private System.Windows.Forms.ComboBox cmb_pageSize;
        private System.Windows.Forms.Label lbl_pageSizeText;
        private System.Windows.Forms.TableLayoutPanel tableLayoutRight;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button restore_col_btn;
        private System.Windows.Forms.CheckBox del_col_list_allcheck;
        public System.Windows.Forms.DataGridView dataGridView_delete_col;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button delete_data_btn;
        private System.Windows.Forms.Button restore_del_data_btn;
        private System.Windows.Forms.CheckBox del_data_list_allcheck;
        private System.Windows.Forms.DataGridView dataGridView_delete_data;
        private System.Windows.Forms.Button delete_search_button;
        private System.Windows.Forms.TextBox delete_search_keyword;
        private System.Windows.Forms.ComboBox stand_col_combo;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btn_complete;
        private System.Windows.Forms.ComboBox prod_col_combo;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox cmb_target;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmb_money;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox dept_col_combo;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox sub_acc_col_combo;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DataGridView dataGridView2;
    }
}
