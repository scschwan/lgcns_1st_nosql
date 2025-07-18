namespace FinanceTool
{
    partial class ClusterDetailPopup
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tableLayoutMain = new TableLayoutPanel();
            panel_top = new Panel();
            select_all_btn = new Button();
            status_label = new Label();
            unmerge_selected_btn = new Button();
            close_btn = new Button();
            detail_title_label = new Label();
            pnl_data = new Panel();
            detail_grid_view = new DataGridView();
            tableLayoutMain.SuspendLayout();
            panel_top.SuspendLayout();
            pnl_data.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)detail_grid_view).BeginInit();
            SuspendLayout();
            // 
            // tableLayoutMain
            // 
            tableLayoutMain.ColumnCount = 1;
            tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutMain.Controls.Add(panel_top, 0, 0);
            tableLayoutMain.Controls.Add(pnl_data, 0, 1);
            tableLayoutMain.Dock = DockStyle.Fill;
            tableLayoutMain.Location = new Point(0, 0);
            tableLayoutMain.Name = "tableLayoutMain";
            tableLayoutMain.RowCount = 2;
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutMain.Size = new Size(1200, 800);
            tableLayoutMain.TabIndex = 0;
            // 
            // panel_top
            // 
            panel_top.BackColor = Color.WhiteSmoke;
            panel_top.Controls.Add(select_all_btn);
            panel_top.Controls.Add(status_label);
            panel_top.Controls.Add(unmerge_selected_btn);
            panel_top.Controls.Add(close_btn);
            panel_top.Controls.Add(detail_title_label);
            panel_top.Dock = DockStyle.Fill;
            panel_top.Location = new Point(3, 3);
            panel_top.Name = "panel_top";
            panel_top.Size = new Size(1194, 54);
            panel_top.TabIndex = 0;
            // 
            // select_all_btn
            // 
            select_all_btn.BackColor = Color.DodgerBlue;
            select_all_btn.FlatStyle = FlatStyle.Flat;
            select_all_btn.Font = new Font("Pretendard", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            select_all_btn.ForeColor = Color.White;
            select_all_btn.Location = new Point(475, 16);
            select_all_btn.Name = "select_all_btn";
            select_all_btn.Size = new Size(120, 30);
            select_all_btn.TabIndex = 4;
            select_all_btn.Text = "모두 선택";
            select_all_btn.UseVisualStyleBackColor = false;
            // 
            // status_label
            // 
            status_label.AutoSize = true;
            status_label.Font = new Font("Pretendard", 12F, FontStyle.Regular, GraphicsUnit.Point, 129);
            status_label.Location = new Point(309, 20);
            status_label.Name = "status_label";
            status_label.Size = new Size(57, 21);
            status_label.TabIndex = 3;
            status_label.Text = "총 0개";
            // 
            // unmerge_selected_btn
            // 
            unmerge_selected_btn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            unmerge_selected_btn.BackColor = Color.Crimson;
            unmerge_selected_btn.FlatStyle = FlatStyle.Flat;
            unmerge_selected_btn.Font = new Font("Pretendard", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            unmerge_selected_btn.ForeColor = Color.White;
            unmerge_selected_btn.Location = new Point(894, 15);
            unmerge_selected_btn.Name = "unmerge_selected_btn";
            unmerge_selected_btn.Size = new Size(180, 30);
            unmerge_selected_btn.TabIndex = 2;
            unmerge_selected_btn.Text = "선택 항목 병합 해제";
            unmerge_selected_btn.UseVisualStyleBackColor = false;
            unmerge_selected_btn.Click += unmerge_selected_btn_Click;
            // 
            // close_btn
            // 
            close_btn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            close_btn.BackColor = Color.LimeGreen;
            close_btn.FlatStyle = FlatStyle.Flat;
            close_btn.Font = new Font("Pretendard", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            close_btn.ForeColor = Color.White;
            close_btn.Location = new Point(1094, 15);
            close_btn.Name = "close_btn";
            close_btn.Size = new Size(80, 28);
            close_btn.TabIndex = 1;
            close_btn.Text = "닫기";
            close_btn.UseVisualStyleBackColor = false;
            // 
            // detail_title_label
            // 
            detail_title_label.AutoSize = true;
            detail_title_label.Font = new Font("Pretendard", 14F, FontStyle.Bold, GraphicsUnit.Point, 129);
            detail_title_label.Location = new Point(15, 18);
            detail_title_label.Name = "detail_title_label";
            detail_title_label.Size = new Size(178, 25);
            detail_title_label.TabIndex = 0;
            detail_title_label.Text = "클러스터 세부 정보";
            // 
            // pnl_data
            // 
            pnl_data.Controls.Add(detail_grid_view);
            pnl_data.Dock = DockStyle.Fill;
            pnl_data.Location = new Point(5, 65);
            pnl_data.Margin = new Padding(5);
            pnl_data.Name = "pnl_data";
            pnl_data.Size = new Size(1190, 730);
            pnl_data.TabIndex = 1;
            // 
            // detail_grid_view
            // 
            detail_grid_view.AllowUserToAddRows = false;
            detail_grid_view.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            detail_grid_view.BackgroundColor = Color.White;
            detail_grid_view.BorderStyle = BorderStyle.Fixed3D;
            detail_grid_view.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            detail_grid_view.EnableHeadersVisualStyles = false;
            detail_grid_view.GridColor = Color.LightGray;
            detail_grid_view.Location = new Point(10, 10);
            detail_grid_view.MinimumSize = new Size(600, 400);
            detail_grid_view.Name = "detail_grid_view";
            detail_grid_view.RowHeadersVisible = false;
            detail_grid_view.Size = new Size(1170, 710);
            detail_grid_view.TabIndex = 1;
            // 
            // ClusterDetailPopup
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1200, 800);
            Controls.Add(tableLayoutMain);
            Font = new Font("Pretendard", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            MinimumSize = new Size(800, 600);
            Name = "ClusterDetailPopup";
            StartPosition = FormStartPosition.CenterParent;
            Text = "클러스터 세부 정보";
            tableLayoutMain.ResumeLayout(false);
            panel_top.ResumeLayout(false);
            panel_top.PerformLayout();
            pnl_data.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)detail_grid_view).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutMain;
        private Panel pnl_data;

        // 기존 컨트롤들 (모든 컨트롤명 유지)
        private System.Windows.Forms.Panel panel_top;
        private System.Windows.Forms.Label detail_title_label;
        private System.Windows.Forms.Button close_btn;
        private System.Windows.Forms.Button unmerge_selected_btn;
        private System.Windows.Forms.Label status_label;
        private System.Windows.Forms.DataGridView detail_grid_view;
        private System.Windows.Forms.Button select_all_btn;
    }
}