namespace FinanceTool.uc
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
            SuspendLayout();
            // 
            // uc_fileUpload
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Name = "uc_fileUpload";
            Size = new Size(1904, 1017);
            ResumeLayout(false);

            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            this.pnl_main = new System.Windows.Forms.Panel();
            this.pnl_content = new System.Windows.Forms.Panel();
            this.pnl_right = new System.Windows.Forms.Panel();
            this.lbl_sessions = new System.Windows.Forms.Label();
            this.dgv_sessions = new System.Windows.Forms.DataGridView();
            this.col_session_name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_file_list = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_session_account = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_session_rows = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_session_amount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_session_status = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.col_download = new System.Windows.Forms.DataGridViewButtonColumn();
            this.pnl_left = new System.Windows.Forms.Panel();
            this.lbl_files = new System.Windows.Forms.Label();
            this.dgv_files = new System.Windows.Forms.DataGridView();
            this.col_file_check = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.col_filename = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_row_count = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_account_column = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.col_amount_column = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.col_total_amount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnl_header = new System.Windows.Forms.Panel();
            this.btn_create_sessions = new System.Windows.Forms.Button();
            this.btn_upload_files = new System.Windows.Forms.Button();
            this.lbl_instruction = new System.Windows.Forms.Label();
            this.lbl_title = new System.Windows.Forms.Label();
            this.pnl_main.SuspendLayout();
            this.pnl_content.SuspendLayout();
            this.pnl_right.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_sessions)).BeginInit();
            this.pnl_left.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_files)).BeginInit();
            this.pnl_header.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnl_main
            // 
            this.pnl_main.Controls.Add(this.pnl_content);
            this.pnl_main.Controls.Add(this.pnl_header);
            this.pnl_main.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl_main.Location = new System.Drawing.Point(0, 0);
            this.pnl_main.Name = "pnl_main";
            this.pnl_main.Size = new System.Drawing.Size(1904, 1017);
            this.pnl_main.TabIndex = 0;
            // 
            // pnl_content
            // 
            this.pnl_content.Controls.Add(this.pnl_right);
            this.pnl_content.Controls.Add(this.pnl_left);
            this.pnl_content.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl_content.Location = new System.Drawing.Point(0, 120);
            this.pnl_content.Name = "pnl_content";
            this.pnl_content.Padding = new System.Windows.Forms.Padding(10);
            this.pnl_content.Size = new System.Drawing.Size(1904, 897);
            this.pnl_content.TabIndex = 1;
            // 
            // pnl_right
            // 
            this.pnl_right.Controls.Add(this.lbl_sessions);
            this.pnl_right.Controls.Add(this.dgv_sessions);
            this.pnl_right.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl_right.Location = new System.Drawing.Point(1154, 10);
            this.pnl_right.Name = "pnl_right";
            this.pnl_right.Size = new System.Drawing.Size(740, 877);
            this.pnl_right.TabIndex = 1;
            // 
            // lbl_sessions
            // 
            this.lbl_sessions.BackColor = System.Drawing.Color.SteelBlue;
            this.lbl_sessions.Dock = System.Windows.Forms.DockStyle.Top;
            this.lbl_sessions.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lbl_sessions.ForeColor = System.Drawing.Color.White;
            this.lbl_sessions.Location = new System.Drawing.Point(0, 0);
            this.lbl_sessions.Name = "lbl_sessions";
            this.lbl_sessions.Size = new System.Drawing.Size(740, 40);
            this.lbl_sessions.TabIndex = 1;
            this.lbl_sessions.Text = "생성된 세션 목록";
            this.lbl_sessions.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // dgv_sessions
            // 
            this.dgv_sessions.AllowUserToAddRows = false;
            this.dgv_sessions.AllowUserToDeleteRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.AliceBlue;
            this.dgv_sessions.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgv_sessions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgv_sessions.BackgroundColor = System.Drawing.Color.White;
            this.dgv_sessions.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.LightSteelBlue;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgv_sessions.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgv_sessions.ColumnHeadersHeight = 35;
            this.dgv_sessions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgv_sessions.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.col_session_name,
            this.col_file_list,
            this.col_session_account,
            this.col_session_rows,
            this.col_session_amount,
            this.col_session_status,
            this.col_download});
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("맑은 고딕", 9F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgv_sessions.DefaultCellStyle = dataGridViewCellStyle3;
            this.dgv_sessions.EnableHeadersVisualStyles = false;
            this.dgv_sessions.GridColor = System.Drawing.Color.LightGray;
            this.dgv_sessions.Location = new System.Drawing.Point(0, 40);
            this.dgv_sessions.Name = "dgv_sessions";
            this.dgv_sessions.RowHeadersVisible = false;
            this.dgv_sessions.RowTemplate.Height = 30;
            this.dgv_sessions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_sessions.Size = new System.Drawing.Size(740, 837);
            this.dgv_sessions.TabIndex = 0;
            // 
            // col_session_name
            // 
            this.col_session_name.HeaderText = "세션명";
            this.col_session_name.Name = "col_session_name";
            this.col_session_name.Width = 120;
            // 
            // col_file_list
            // 
            this.col_file_list.HeaderText = "파일목록";
            this.col_file_list.Name = "col_file_list";
            this.col_file_list.ReadOnly = true;
            this.col_file_list.Width = 150;
            // 
            // col_session_account
            // 
            this.col_session_account.HeaderText = "대계정";
            this.col_session_account.Name = "col_session_account";
            this.col_session_account.ReadOnly = true;
            this.col_session_account.Width = 100;
            // 
            // col_session_rows
            // 
            this.col_session_rows.HeaderText = "행 수";
            this.col_session_rows.Name = "col_session_rows";
            this.col_session_rows.ReadOnly = true;
            this.col_session_rows.Width = 80;
            // 
            // col_session_amount
            // 
            this.col_session_amount.HeaderText = "합산금액";
            this.col_session_amount.Name = "col_session_amount";
            this.col_session_amount.ReadOnly = true;
            this.col_session_amount.Width = 120;
            // 
            // col_session_status
            // 
            this.col_session_status.HeaderText = "작업완료";
            this.col_session_status.Name = "col_session_status";
            this.col_session_status.Width = 80;
            // 
            // col_download
            // 
            this.col_download.HeaderText = "다운로드";
            this.col_download.Name = "col_download";
            this.col_download.Text = "다운로드";
            this.col_download.UseColumnTextForButtonValue = true;
            this.col_download.Width = 90;
            // 
            // pnl_left
            // 
            this.pnl_left.Controls.Add(this.lbl_files);
            this.pnl_left.Controls.Add(this.dgv_files);
            this.pnl_left.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnl_left.Location = new System.Drawing.Point(10, 10);
            this.pnl_left.Name = "pnl_left";
            this.pnl_left.Size = new System.Drawing.Size(1144, 877);
            this.pnl_left.TabIndex = 0;
            // 
            // lbl_files
            // 
            this.lbl_files.BackColor = System.Drawing.Color.SteelBlue;
            this.lbl_files.Dock = System.Windows.Forms.DockStyle.Top;
            this.lbl_files.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lbl_files.ForeColor = System.Drawing.Color.White;
            this.lbl_files.Location = new System.Drawing.Point(0, 0);
            this.lbl_files.Name = "lbl_files";
            this.lbl_files.Size = new System.Drawing.Size(1144, 40);
            this.lbl_files.TabIndex = 1;
            this.lbl_files.Text = "업로드된 파일 목록";
            this.lbl_files.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // dgv_files
            // 
            this.dgv_files.AllowUserToAddRows = false;
            this.dgv_files.AllowUserToDeleteRows = false;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.AliceBlue;
            this.dgv_files.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle4;
            this.dgv_files.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgv_files.BackgroundColor = System.Drawing.Color.White;
            this.dgv_files.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.LightSteelBlue;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle5.ForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgv_files.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            this.dgv_files.ColumnHeadersHeight = 35;
            this.dgv_files.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgv_files.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.col_file_check,
            this.col_filename,
            this.col_row_count,
            this.col_account_column,
            this.col_amount_column,
            this.col_total_amount});
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle6.Font = new System.Drawing.Font("맑은 고딕", 9F);
            dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgv_files.DefaultCellStyle = dataGridViewCellStyle6;
            this.dgv_files.EnableHeadersVisualStyles = false;
            this.dgv_files.GridColor = System.Drawing.Color.LightGray;
            this.dgv_files.Location = new System.Drawing.Point(0, 40);
            this.dgv_files.Name = "dgv_files";
            this.dgv_files.RowHeadersVisible = false;
            this.dgv_files.RowTemplate.Height = 30;
            this.dgv_files.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_files.Size = new System.Drawing.Size(1144, 837);
            this.dgv_files.TabIndex = 0;
            // 
            // col_file_check
            // 
            this.col_file_check.HeaderText = "선택";
            this.col_file_check.Name = "col_file_check";
            this.col_file_check.Width = 60;
            // 
            // col_filename
            // 
            this.col_filename.HeaderText = "파일명";
            this.col_filename.Name = "col_filename";
            this.col_filename.ReadOnly = true;
            this.col_filename.Width = 350;
            // 
            // col_row_count
            // 
            this.col_row_count.HeaderText = "행 수";
            this.col_row_count.Name = "col_row_count";
            this.col_row_count.ReadOnly = true;
            this.col_row_count.Width = 100;
            // 
            // col_account_column
            // 
            this.col_account_column.HeaderText = "대계정 컬럼";
            this.col_account_column.Name = "col_account_column";
            this.col_account_column.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.col_account_column.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.col_account_column.Width = 200;
            // 
            // col_amount_column
            // 
            this.col_amount_column.HeaderText = "금액 컬럼";
            this.col_amount_column.Name = "col_amount_column";
            this.col_amount_column.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.col_amount_column.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.col_amount_column.Width = 200;
            // 
            // col_total_amount
            // 
            this.col_total_amount.HeaderText = "합산금액";
            this.col_total_amount.Name = "col_total_amount";
            this.col_total_amount.ReadOnly = true;
            this.col_total_amount.Width = 170;
            // 
            // pnl_header
            // 
            this.pnl_header.BackColor = System.Drawing.Color.WhiteSmoke;
            this.pnl_header.Controls.Add(this.btn_create_sessions);
            this.pnl_header.Controls.Add(this.btn_upload_files);
            this.pnl_header.Controls.Add(this.lbl_instruction);
            this.pnl_header.Controls.Add(this.lbl_title);
            this.pnl_header.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnl_header.Location = new System.Drawing.Point(0, 0);
            this.pnl_header.Name = "pnl_header";
            this.pnl_header.Size = new System.Drawing.Size(1904, 120);
            this.pnl_header.TabIndex = 0;
            // 
            // btn_create_sessions
            // 
            this.btn_create_sessions.BackColor = System.Drawing.Color.Orange;
            this.btn_create_sessions.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_create_sessions.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btn_create_sessions.ForeColor = System.Drawing.Color.White;
            this.btn_create_sessions.Location = new System.Drawing.Point(250, 65);
            this.btn_create_sessions.Name = "btn_create_sessions";
            this.btn_create_sessions.Size = new System.Drawing.Size(150, 40);
            this.btn_create_sessions.TabIndex = 3;
            this.btn_create_sessions.Text = "세션 생성";
            this.btn_create_sessions.UseVisualStyleBackColor = false;
            // 
            // btn_upload_files
            // 
            this.btn_upload_files.BackColor = System.Drawing.Color.DodgerBlue;
            this.btn_upload_files.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_upload_files.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btn_upload_files.ForeColor = System.Drawing.Color.White;
            this.btn_upload_files.Location = new System.Drawing.Point(20, 65);
            this.btn_upload_files.Name = "btn_upload_files";
            this.btn_upload_files.Size = new System.Drawing.Size(200, 40);
            this.btn_upload_files.TabIndex = 2;
            this.btn_upload_files.Text = "Excel 파일 업로드";
            this.btn_upload_files.UseVisualStyleBackColor = false;
            // 
            // lbl_instruction
            // 
            this.lbl_instruction.AutoSize = true;
            this.lbl_instruction.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lbl_instruction.ForeColor = System.Drawing.Color.Gray;
            this.lbl_instruction.Location = new System.Drawing.Point(20, 40);
            this.lbl_instruction.Name = "lbl_instruction";
            this.lbl_instruction.Size = new System.Drawing.Size(611, 19);
            this.lbl_instruction.TabIndex = 1;
            this.lbl_instruction.Text = "여러 Excel 파일을 업로드하고 계정명/금액 컬럼을 선택한 후, 동일한 컬럼명끼리 세션을 생성하세요.";
            // 
            // lbl_title
            // 
            this.lbl_title.AutoSize = true;
            this.lbl_title.Font = new System.Drawing.Font("맑은 고딕", 16F, System.Drawing.FontStyle.Bold);
            this.lbl_title.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lbl_title.Location = new System.Drawing.Point(15, 10);
            this.lbl_title.Name = "lbl_title";
            this.lbl_title.Size = new System.Drawing.Size(204, 30);
            this.lbl_title.TabIndex = 0;
            this.lbl_title.Text = "다중 파일 업로드";
            // 
            // uc_MultiFileUpload
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.pnl_main);
            this.Name = "uc_MultiFileUpload";
            this.Size = new System.Drawing.Size(1904, 1017);
            this.pnl_main.ResumeLayout(false);
            this.pnl_content.ResumeLayout(false);
            this.pnl_right.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_sessions)).EndInit();
            this.pnl_left.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_files)).EndInit();
            this.pnl_header.ResumeLayout(false);
            this.pnl_header.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnl_main;
        private System.Windows.Forms.Panel pnl_content;
        private System.Windows.Forms.Panel pnl_right;
        private System.Windows.Forms.Label lbl_sessions;
        private System.Windows.Forms.DataGridView dgv_sessions;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_session_name;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_file_list;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_session_account;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_session_rows;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_session_amount;
        private System.Windows.Forms.DataGridViewCheckBoxColumn col_session_status;
        private System.Windows.Forms.DataGridViewButtonColumn col_download;
        private System.Windows.Forms.Panel pnl_left;
        private System.Windows.Forms.Label lbl_files;
        private System.Windows.Forms.DataGridView dgv_files;
        private System.Windows.Forms.DataGridViewCheckBoxColumn col_file_check;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_filename;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_row_count;
        private System.Windows.Forms.DataGridViewComboBoxColumn col_account_column;
        private System.Windows.Forms.DataGridViewComboBoxColumn col_amount_column;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_total_amount;
        private System.Windows.Forms.Panel pnl_header;
        private System.Windows.Forms.Button btn_create_sessions;
        private System.Windows.Forms.Button btn_upload_files;
        private System.Windows.Forms.Label lbl_instruction;
        private System.Windows.Forms.Label lbl_title;
    }
}
