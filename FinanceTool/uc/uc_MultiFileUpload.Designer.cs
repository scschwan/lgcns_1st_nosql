namespace FinanceTool
{
    partial class uc_MultiFileUpload
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
            DataGridViewCellStyle dataGridViewCellStyle13 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle14 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle15 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle16 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle17 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle18 = new DataGridViewCellStyle();
            tableLayoutMain = new TableLayoutPanel();
            pnl_header = new Panel();
            tableLayoutHeader = new TableLayoutPanel();
            pnl_title_area = new Panel();
            btn_add_to_session = new Button();
            btn_create_sessions = new Button();
            btn_upload_files = new Button();
            lbl_instruction = new Label();
            lbl_title = new Label();
            pnl_button_area = new Panel();
            btn_merge_sessions = new Button();
            btn_complete = new Button();
            tableLayoutContent = new TableLayoutPanel();
            pnl_left = new Panel();
            dgv_files = new DataGridView();
            col_file_check = new DataGridViewCheckBoxColumn();
            col_filename = new DataGridViewTextBoxColumn();
            col_row_count = new DataGridViewTextBoxColumn();
            col_account_column = new DataGridViewComboBoxColumn();
            col_amount_column = new DataGridViewComboBoxColumn();
            col_total_amount = new DataGridViewTextBoxColumn();
            lbl_files = new Label();
            pnl_right = new Panel();
            dgv_sessions = new DataGridView();
            col_session_name = new DataGridViewTextBoxColumn();
            col_file_list = new DataGridViewTextBoxColumn();
            col_session_account = new DataGridViewTextBoxColumn();
            col_session_rows = new DataGridViewTextBoxColumn();
            col_session_amount = new DataGridViewTextBoxColumn();
            col_session_status = new DataGridViewCheckBoxColumn();
            col_download = new DataGridViewButtonColumn();
            lbl_sessions = new Label();
            tableLayoutMain.SuspendLayout();
            pnl_header.SuspendLayout();
            tableLayoutHeader.SuspendLayout();
            pnl_title_area.SuspendLayout();
            pnl_button_area.SuspendLayout();
            tableLayoutContent.SuspendLayout();
            pnl_left.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgv_files).BeginInit();
            pnl_right.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgv_sessions).BeginInit();
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
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutMain.Size = new Size(1904, 1271);
            tableLayoutMain.TabIndex = 0;
            // 
            // pnl_header
            // 
            pnl_header.BackColor = Color.WhiteSmoke;
            pnl_header.Controls.Add(tableLayoutHeader);
            pnl_header.Dock = DockStyle.Fill;
            pnl_header.Location = new Point(0, 0);
            pnl_header.Margin = new Padding(0);
            pnl_header.Name = "pnl_header";
            pnl_header.Size = new Size(1904, 150);
            pnl_header.TabIndex = 0;
            // 
            // tableLayoutHeader
            // 
            tableLayoutHeader.ColumnCount = 2;
            tableLayoutHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 59.9789925F));
            tableLayoutHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40.0210075F));
            tableLayoutHeader.Controls.Add(pnl_title_area, 0, 0);
            tableLayoutHeader.Controls.Add(pnl_button_area, 1, 0);
            tableLayoutHeader.Dock = DockStyle.Fill;
            tableLayoutHeader.Location = new Point(0, 0);
            tableLayoutHeader.Margin = new Padding(0);
            tableLayoutHeader.Name = "tableLayoutHeader";
            tableLayoutHeader.RowCount = 1;
            tableLayoutHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutHeader.Size = new Size(1904, 150);
            tableLayoutHeader.TabIndex = 0;
            // 
            // pnl_title_area
            // 
            pnl_title_area.Controls.Add(btn_add_to_session);
            pnl_title_area.Controls.Add(btn_create_sessions);
            pnl_title_area.Controls.Add(btn_upload_files);
            pnl_title_area.Controls.Add(lbl_instruction);
            pnl_title_area.Controls.Add(lbl_title);
            pnl_title_area.Dock = DockStyle.Fill;
            pnl_title_area.Location = new Point(10, 12);
            pnl_title_area.Margin = new Padding(10, 12, 10, 12);
            pnl_title_area.Name = "pnl_title_area";
            pnl_title_area.Size = new Size(1122, 126);
            pnl_title_area.TabIndex = 0;
            // 
            // btn_add_to_session
            // 
            btn_add_to_session.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btn_add_to_session.BackColor = Color.IndianRed;
            btn_add_to_session.FlatStyle = FlatStyle.Flat;
            btn_add_to_session.Font = new Font("Pretendard", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            btn_add_to_session.ForeColor = Color.White;
            btn_add_to_session.Location = new Point(794, 62);
            btn_add_to_session.Margin = new Padding(3, 4, 3, 4);
            btn_add_to_session.MinimumSize = new Size(120, 44);
            btn_add_to_session.Name = "btn_add_to_session";
            btn_add_to_session.Size = new Size(150, 50);
            btn_add_to_session.TabIndex = 2;
            btn_add_to_session.Text = "기존 세션 추가";
            btn_add_to_session.UseVisualStyleBackColor = false;
            btn_add_to_session.Visible = false;
            btn_add_to_session.Click += btn_add_to_session_Click;
            // 
            // btn_create_sessions
            // 
            btn_create_sessions.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btn_create_sessions.BackColor = Color.Orange;
            btn_create_sessions.FlatStyle = FlatStyle.Flat;
            btn_create_sessions.Font = new Font("Pretendard", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            btn_create_sessions.ForeColor = Color.White;
            btn_create_sessions.Location = new Point(950, 62);
            btn_create_sessions.Margin = new Padding(3, 4, 3, 4);
            btn_create_sessions.MinimumSize = new Size(120, 44);
            btn_create_sessions.Name = "btn_create_sessions";
            btn_create_sessions.Size = new Size(150, 50);
            btn_create_sessions.TabIndex = 1;
            btn_create_sessions.Text = "세션 생성";
            btn_create_sessions.UseVisualStyleBackColor = false;
            btn_create_sessions.Click += btn_create_sessions_Click;
            // 
            // btn_upload_files
            // 
            btn_upload_files.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btn_upload_files.BackColor = Color.DodgerBlue;
            btn_upload_files.FlatStyle = FlatStyle.Flat;
            btn_upload_files.Font = new Font("Pretendard", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            btn_upload_files.ForeColor = Color.White;
            btn_upload_files.Location = new Point(8, 62);
            btn_upload_files.Margin = new Padding(3, 4, 3, 4);
            btn_upload_files.MinimumSize = new Size(150, 44);
            btn_upload_files.Name = "btn_upload_files";
            btn_upload_files.Size = new Size(180, 50);
            btn_upload_files.TabIndex = 0;
            btn_upload_files.Text = "Excel 파일 업로드";
            btn_upload_files.UseVisualStyleBackColor = false;
            btn_upload_files.Click += btn_upload_files_Click;
            // 
            // lbl_instruction
            // 
            lbl_instruction.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lbl_instruction.Font = new Font("Pretendard", 10F);
            lbl_instruction.ForeColor = Color.Gray;
            lbl_instruction.Location = new Point(194, 12);
            lbl_instruction.Name = "lbl_instruction";
            lbl_instruction.Size = new Size(670, 30);
            lbl_instruction.TabIndex = 1;
            lbl_instruction.Text = "여러 Excel 파일을 업로드하고 계정명/금액 컬럼을 선택한 후, 동일한 컬럼명끼리 세션을 생성하세요.";
            lbl_instruction.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lbl_title
            // 
            lbl_title.AutoSize = true;
            lbl_title.Font = new Font("Pretendard", 16F, FontStyle.Bold);
            lbl_title.ForeColor = Color.DarkSlateGray;
            lbl_title.Location = new Point(5, 12);
            lbl_title.Name = "lbl_title";
            lbl_title.Size = new Size(183, 30);
            lbl_title.TabIndex = 0;
            lbl_title.Text = "다중 파일 업로드";
            // 
            // pnl_button_area
            // 
            pnl_button_area.Controls.Add(btn_merge_sessions);
            pnl_button_area.Controls.Add(btn_complete);
            pnl_button_area.Dock = DockStyle.Fill;
            pnl_button_area.Location = new Point(1152, 12);
            pnl_button_area.Margin = new Padding(10, 12, 10, 12);
            pnl_button_area.Name = "pnl_button_area";
            pnl_button_area.Size = new Size(742, 126);
            pnl_button_area.TabIndex = 1;
            // 
            // btn_merge_sessions
            // 
            btn_merge_sessions.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btn_merge_sessions.BackColor = Color.IndianRed;
            btn_merge_sessions.FlatStyle = FlatStyle.Flat;
            btn_merge_sessions.Font = new Font("Pretendard", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            btn_merge_sessions.ForeColor = Color.White;
            btn_merge_sessions.Location = new Point(367, 62);
            btn_merge_sessions.Margin = new Padding(3, 4, 3, 4);
            btn_merge_sessions.MinimumSize = new Size(120, 44);
            btn_merge_sessions.Name = "btn_merge_sessions";
            btn_merge_sessions.Size = new Size(150, 50);
            btn_merge_sessions.TabIndex = 3;
            btn_merge_sessions.Text = "세션 병합";
            btn_merge_sessions.UseVisualStyleBackColor = false;
            btn_merge_sessions.Click += btn_merge_sessions_Click;
            // 
            // btn_complete
            // 
            btn_complete.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btn_complete.BackColor = Color.LimeGreen;
            btn_complete.FlatStyle = FlatStyle.Flat;
            btn_complete.Font = new Font("Pretendard", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            btn_complete.ForeColor = Color.White;
            btn_complete.Location = new Point(574, 62);
            btn_complete.Margin = new Padding(3, 4, 3, 4);
            btn_complete.MinimumSize = new Size(120, 44);
            btn_complete.Name = "btn_complete";
            btn_complete.Size = new Size(150, 50);
            btn_complete.TabIndex = 2;
            btn_complete.Text = "계정 분석 시작";
            btn_complete.UseVisualStyleBackColor = false;
            btn_complete.Click += btn_complete_Click;
            // 
            // tableLayoutContent
            // 
            tableLayoutContent.ColumnCount = 2;
            tableLayoutContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tableLayoutContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tableLayoutContent.Controls.Add(pnl_left, 0, 0);
            tableLayoutContent.Controls.Add(pnl_right, 1, 0);
            tableLayoutContent.Dock = DockStyle.Fill;
            tableLayoutContent.Location = new Point(0, 150);
            tableLayoutContent.Margin = new Padding(0);
            tableLayoutContent.Name = "tableLayoutContent";
            tableLayoutContent.RowCount = 1;
            tableLayoutContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutContent.Size = new Size(1904, 1121);
            tableLayoutContent.TabIndex = 1;
            // 
            // pnl_left
            // 
            pnl_left.Controls.Add(dgv_files);
            pnl_left.Controls.Add(lbl_files);
            pnl_left.Dock = DockStyle.Fill;
            pnl_left.Location = new Point(10, 12);
            pnl_left.Margin = new Padding(10, 12, 5, 12);
            pnl_left.Name = "pnl_left";
            pnl_left.Size = new Size(1127, 1097);
            pnl_left.TabIndex = 0;
            // 
            // dgv_files
            // 
            dgv_files.AllowUserToAddRows = false;
            dgv_files.AllowUserToDeleteRows = false;
            dataGridViewCellStyle13.BackColor = Color.AliceBlue;
            dgv_files.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle13;
            dgv_files.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgv_files.BackgroundColor = Color.White;
            dgv_files.BorderStyle = BorderStyle.Fixed3D;
            dataGridViewCellStyle14.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle14.BackColor = Color.LightSteelBlue;
            dataGridViewCellStyle14.Font = new Font("Pretendard", 10F, FontStyle.Bold);
            dataGridViewCellStyle14.ForeColor = Color.Black;
            dataGridViewCellStyle14.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle14.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle14.WrapMode = DataGridViewTriState.True;
            dgv_files.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle14;
            dgv_files.ColumnHeadersHeight = 35;
            dgv_files.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv_files.Columns.AddRange(new DataGridViewColumn[] { col_file_check, col_filename, col_row_count, col_account_column, col_amount_column, col_total_amount });
            dataGridViewCellStyle15.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle15.BackColor = SystemColors.Window;
            dataGridViewCellStyle15.Font = new Font("Pretendard", 9F);
            dataGridViewCellStyle15.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle15.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle15.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle15.WrapMode = DataGridViewTriState.False;
            dgv_files.DefaultCellStyle = dataGridViewCellStyle15;
            dgv_files.EnableHeadersVisualStyles = false;
            dgv_files.GridColor = Color.LightGray;
            dgv_files.Location = new Point(0, 50);
            dgv_files.Margin = new Padding(3, 4, 3, 4);
            dgv_files.MinimumSize = new Size(800, 500);
            dgv_files.Name = "dgv_files";
            dgv_files.RowHeadersVisible = false;
            dgv_files.RowTemplate.Height = 30;
            dgv_files.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv_files.Size = new Size(1127, 1035);
            dgv_files.TabIndex = 0;
            // 
            // col_file_check
            // 
            col_file_check.FillWeight = 8F;
            col_file_check.HeaderText = "선택";
            col_file_check.MinimumWidth = 50;
            col_file_check.Name = "col_file_check";
            col_file_check.Width = 60;
            // 
            // col_filename
            // 
            col_filename.FillWeight = 35F;
            col_filename.HeaderText = "파일명";
            col_filename.MinimumWidth = 200;
            col_filename.Name = "col_filename";
            col_filename.ReadOnly = true;
            col_filename.Width = 350;
            // 
            // col_row_count
            // 
            col_row_count.FillWeight = 12F;
            col_row_count.HeaderText = "행 수";
            col_row_count.MinimumWidth = 80;
            col_row_count.Name = "col_row_count";
            col_row_count.ReadOnly = true;
            // 
            // col_account_column
            // 
            col_account_column.FillWeight = 20F;
            col_account_column.HeaderText = "대계정 컬럼";
            col_account_column.MinimumWidth = 120;
            col_account_column.Name = "col_account_column";
            col_account_column.Resizable = DataGridViewTriState.True;
            col_account_column.SortMode = DataGridViewColumnSortMode.Automatic;
            col_account_column.Width = 200;
            // 
            // col_amount_column
            // 
            col_amount_column.FillWeight = 20F;
            col_amount_column.HeaderText = "금액 컬럼";
            col_amount_column.MinimumWidth = 120;
            col_amount_column.Name = "col_amount_column";
            col_amount_column.Resizable = DataGridViewTriState.True;
            col_amount_column.SortMode = DataGridViewColumnSortMode.Automatic;
            col_amount_column.Width = 200;
            // 
            // col_total_amount
            // 
            col_total_amount.FillWeight = 17F;
            col_total_amount.HeaderText = "합산금액";
            col_total_amount.MinimumWidth = 100;
            col_total_amount.Name = "col_total_amount";
            col_total_amount.ReadOnly = true;
            col_total_amount.Width = 170;
            // 
            // lbl_files
            // 
            lbl_files.BackColor = Color.SteelBlue;
            lbl_files.Dock = DockStyle.Top;
            lbl_files.Font = new Font("Pretendard", 12F, FontStyle.Bold, GraphicsUnit.Point, 129);
            lbl_files.ForeColor = Color.White;
            lbl_files.Location = new Point(0, 0);
            lbl_files.Name = "lbl_files";
            lbl_files.Size = new Size(1127, 50);
            lbl_files.TabIndex = 1;
            lbl_files.Text = "업로드된 파일 목록";
            lbl_files.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pnl_right
            // 
            pnl_right.Controls.Add(dgv_sessions);
            pnl_right.Controls.Add(lbl_sessions);
            pnl_right.Dock = DockStyle.Fill;
            pnl_right.Location = new Point(1147, 12);
            pnl_right.Margin = new Padding(5, 12, 10, 12);
            pnl_right.Name = "pnl_right";
            pnl_right.Size = new Size(747, 1097);
            pnl_right.TabIndex = 1;
            // 
            // dgv_sessions
            // 
            dgv_sessions.AllowUserToAddRows = false;
            dgv_sessions.AllowUserToDeleteRows = false;
            dataGridViewCellStyle16.BackColor = Color.AliceBlue;
            dgv_sessions.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle16;
            dgv_sessions.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgv_sessions.BackgroundColor = Color.White;
            dgv_sessions.BorderStyle = BorderStyle.Fixed3D;
            dataGridViewCellStyle17.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle17.BackColor = Color.LightSteelBlue;
            dataGridViewCellStyle17.Font = new Font("Pretendard", 10F, FontStyle.Bold);
            dataGridViewCellStyle17.ForeColor = Color.Black;
            dataGridViewCellStyle17.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle17.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle17.WrapMode = DataGridViewTriState.True;
            dgv_sessions.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle17;
            dgv_sessions.ColumnHeadersHeight = 35;
            dgv_sessions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv_sessions.Columns.AddRange(new DataGridViewColumn[] { col_session_name, col_file_list, col_session_account, col_session_rows, col_session_amount, col_session_status, col_download });
            dataGridViewCellStyle18.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle18.BackColor = SystemColors.Window;
            dataGridViewCellStyle18.Font = new Font("Pretendard", 9F);
            dataGridViewCellStyle18.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle18.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle18.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle18.WrapMode = DataGridViewTriState.False;
            dgv_sessions.DefaultCellStyle = dataGridViewCellStyle18;
            dgv_sessions.EnableHeadersVisualStyles = false;
            dgv_sessions.GridColor = Color.LightGray;
            dgv_sessions.Location = new Point(0, 50);
            dgv_sessions.Margin = new Padding(3, 4, 3, 4);
            dgv_sessions.MinimumSize = new Size(500, 500);
            dgv_sessions.Name = "dgv_sessions";
            dgv_sessions.RowHeadersVisible = false;
            dgv_sessions.RowTemplate.Height = 30;
            dgv_sessions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv_sessions.Size = new Size(747, 1035);
            dgv_sessions.TabIndex = 0;
            // 
            // col_session_name
            // 
            col_session_name.FillWeight = 18F;
            col_session_name.HeaderText = "세션명";
            col_session_name.MinimumWidth = 80;
            col_session_name.Name = "col_session_name";
            col_session_name.Width = 120;
            // 
            // col_file_list
            // 
            col_file_list.FillWeight = 25F;
            col_file_list.HeaderText = "파일목록";
            col_file_list.MinimumWidth = 100;
            col_file_list.Name = "col_file_list";
            col_file_list.ReadOnly = true;
            col_file_list.Width = 150;
            // 
            // col_session_account
            // 
            col_session_account.FillWeight = 15F;
            col_session_account.HeaderText = "대계정";
            col_session_account.MinimumWidth = 70;
            col_session_account.Name = "col_session_account";
            col_session_account.ReadOnly = true;
            // 
            // col_session_rows
            // 
            col_session_rows.FillWeight = 12F;
            col_session_rows.HeaderText = "행 수";
            col_session_rows.MinimumWidth = 60;
            col_session_rows.Name = "col_session_rows";
            col_session_rows.ReadOnly = true;
            col_session_rows.Width = 80;
            // 
            // col_session_amount
            // 
            col_session_amount.FillWeight = 18F;
            col_session_amount.HeaderText = "합산금액";
            col_session_amount.MinimumWidth = 80;
            col_session_amount.Name = "col_session_amount";
            col_session_amount.ReadOnly = true;
            col_session_amount.Width = 120;
            // 
            // col_session_status
            // 
            col_session_status.FillWeight = 12F;
            col_session_status.HeaderText = "작업완료";
            col_session_status.MinimumWidth = 60;
            col_session_status.Name = "col_session_status";
            col_session_status.Width = 80;
            // 
            // col_download
            // 
            col_download.FillWeight = 13F;
            col_download.HeaderText = "다운로드";
            col_download.MinimumWidth = 70;
            col_download.Name = "col_download";
            col_download.Text = "다운로드";
            col_download.UseColumnTextForButtonValue = true;
            col_download.Width = 90;
            // 
            // lbl_sessions
            // 
            lbl_sessions.BackColor = Color.SteelBlue;
            lbl_sessions.Dock = DockStyle.Top;
            lbl_sessions.Font = new Font("Pretendard", 12F, FontStyle.Bold, GraphicsUnit.Point, 129);
            lbl_sessions.ForeColor = Color.White;
            lbl_sessions.Location = new Point(0, 0);
            lbl_sessions.Name = "lbl_sessions";
            lbl_sessions.Size = new Size(747, 50);
            lbl_sessions.TabIndex = 1;
            lbl_sessions.Text = "생성된 세션 목록";
            lbl_sessions.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // uc_MultiFileUpload
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            Controls.Add(tableLayoutMain);
            Margin = new Padding(3, 4, 3, 4);
            MinimumSize = new Size(1280, 1000);
            Name = "uc_MultiFileUpload";
            Size = new Size(1904, 1271);
            tableLayoutMain.ResumeLayout(false);
            pnl_header.ResumeLayout(false);
            tableLayoutHeader.ResumeLayout(false);
            pnl_title_area.ResumeLayout(false);
            pnl_title_area.PerformLayout();
            pnl_button_area.ResumeLayout(false);
            tableLayoutContent.ResumeLayout(false);
            pnl_left.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgv_files).EndInit();
            pnl_right.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgv_sessions).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
        private System.Windows.Forms.Panel pnl_header;
        private System.Windows.Forms.TableLayoutPanel tableLayoutHeader;
        private System.Windows.Forms.Panel pnl_title_area;
        private System.Windows.Forms.Label lbl_instruction;
        private System.Windows.Forms.Label lbl_title;
        private System.Windows.Forms.Panel pnl_button_area;
        private System.Windows.Forms.Button btn_create_sessions;
        private System.Windows.Forms.Button btn_upload_files;
        private System.Windows.Forms.TableLayoutPanel tableLayoutContent;
        private System.Windows.Forms.Panel pnl_left;
        private System.Windows.Forms.DataGridView dgv_files;
        private System.Windows.Forms.DataGridViewCheckBoxColumn col_file_check;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_filename;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_row_count;
        private System.Windows.Forms.DataGridViewComboBoxColumn col_account_column;
        private System.Windows.Forms.DataGridViewComboBoxColumn col_amount_column;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_total_amount;
        private System.Windows.Forms.Label lbl_files;
        private System.Windows.Forms.Panel pnl_right;
        private System.Windows.Forms.DataGridView dgv_sessions;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_session_name;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_file_list;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_session_account;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_session_rows;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_session_amount;
        private System.Windows.Forms.DataGridViewCheckBoxColumn col_session_status;
        private System.Windows.Forms.DataGridViewButtonColumn col_download;
        private System.Windows.Forms.Label lbl_sessions;
        private Button btn_complete;
        private Button btn_add_to_session;
        private Button btn_merge_sessions;
    }
}
