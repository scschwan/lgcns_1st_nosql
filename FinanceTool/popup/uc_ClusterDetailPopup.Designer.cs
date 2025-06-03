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
            // 메인 컨테이너
            this.tableLayoutMain = new TableLayoutPanel();
            this.panel_top = new Panel();
            this.pnl_data = new Panel();

            // 기존 컨트롤들
            this.select_all_btn = new Button();
            this.status_label = new Label();
            this.unmerge_selected_btn = new Button();
            this.close_btn = new Button();
            this.detail_title_label = new Label();
            this.detail_grid_view = new DataGridView();

            // 컨트롤 초기화 시작
            this.tableLayoutMain.SuspendLayout();
            this.panel_top.SuspendLayout();
            this.pnl_data.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.detail_grid_view)).BeginInit();
            this.SuspendLayout();

            // 
            // tableLayoutMain
            // 
            this.tableLayoutMain.ColumnCount = 1;
            this.tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.tableLayoutMain.Controls.Add(this.panel_top, 0, 0);
            this.tableLayoutMain.Controls.Add(this.pnl_data, 0, 1);
            this.tableLayoutMain.Dock = DockStyle.Fill;
            this.tableLayoutMain.Location = new Point(0, 0);
            this.tableLayoutMain.Name = "tableLayoutMain";
            this.tableLayoutMain.RowCount = 2;
            this.tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            this.tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.tableLayoutMain.Size = new Size(1200, 800);
            this.tableLayoutMain.TabIndex = 0;

            // 
            // panel_top
            // 
            this.panel_top.BackColor = Color.WhiteSmoke;
            this.panel_top.Controls.Add(this.select_all_btn);
            this.panel_top.Controls.Add(this.status_label);
            this.panel_top.Controls.Add(this.unmerge_selected_btn);
            this.panel_top.Controls.Add(this.close_btn);
            this.panel_top.Controls.Add(this.detail_title_label);
            this.panel_top.Dock = DockStyle.Fill;
            this.panel_top.Location = new Point(0, 0);
            this.panel_top.Name = "panel_top";
            this.panel_top.Size = new Size(1200, 60);
            this.panel_top.TabIndex = 0;

            // 
            // pnl_data
            // 
            this.pnl_data.Controls.Add(this.detail_grid_view);
            this.pnl_data.Dock = DockStyle.Fill;
            this.pnl_data.Location = new Point(5, 65);
            this.pnl_data.Margin = new Padding(5);
            this.pnl_data.Name = "pnl_data";
            this.pnl_data.Size = new Size(1190, 730);
            this.pnl_data.TabIndex = 1;

            // 
            // select_all_btn
            // 
            this.select_all_btn.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Left)));
            this.select_all_btn.Font = new Font("맑은 고딕", 12F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.select_all_btn.Location = new Point(400, 15);
            this.select_all_btn.Name = "select_all_btn";
            this.select_all_btn.Size = new Size(120, 30);
            this.select_all_btn.TabIndex = 4;
            this.select_all_btn.Text = "모두 선택";
            this.select_all_btn.UseVisualStyleBackColor = true;

            // 
            // status_label
            // 
            this.status_label.AutoSize = true;
            this.status_label.Font = new Font("맑은 고딕", 12F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.status_label.Location = new Point(250, 21);
            this.status_label.Name = "status_label";
            this.status_label.Size = new Size(42, 21);
            this.status_label.TabIndex = 3;
            this.status_label.Text = "총 0개";

            // 
            // unmerge_selected_btn
            // 
            this.unmerge_selected_btn.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.unmerge_selected_btn.Font = new Font("맑은 고딕", 12F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.unmerge_selected_btn.Location = new Point(900, 15);
            this.unmerge_selected_btn.Name = "unmerge_selected_btn";
            this.unmerge_selected_btn.Size = new Size(180, 30);
            this.unmerge_selected_btn.TabIndex = 2;
            this.unmerge_selected_btn.Text = "선택 항목 병합 해제";
            this.unmerge_selected_btn.UseVisualStyleBackColor = true;
            this.unmerge_selected_btn.Click += unmerge_selected_btn_Click;

            // 
            // close_btn
            // 
            this.close_btn.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.close_btn.Font = new Font("맑은 고딕", 12F, FontStyle.Bold, GraphicsUnit.Point, 129);
            this.close_btn.Location = new Point(1100, 15);
            this.close_btn.Name = "close_btn";
            this.close_btn.Size = new Size(80, 30);
            this.close_btn.TabIndex = 1;
            this.close_btn.Text = "닫기";
            this.close_btn.UseVisualStyleBackColor = true;

            // 
            // detail_title_label
            // 
            this.detail_title_label.AutoSize = true;
            this.detail_title_label.Font = new Font("맑은 고딕", 14F, FontStyle.Bold, GraphicsUnit.Point, 129);
            this.detail_title_label.Location = new Point(15, 18);
            this.detail_title_label.Name = "detail_title_label";
            this.detail_title_label.Size = new Size(169, 25);
            this.detail_title_label.TabIndex = 0;
            this.detail_title_label.Text = "클러스터 세부 정보";

            // 
            // detail_grid_view
            // 
            this.detail_grid_view.AllowUserToAddRows = false;
            this.detail_grid_view.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
            | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.detail_grid_view.BackgroundColor = Color.White;
            this.detail_grid_view.BorderStyle = BorderStyle.Fixed3D;
            this.detail_grid_view.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.detail_grid_view.EnableHeadersVisualStyles = false;
            this.detail_grid_view.GridColor = Color.LightGray;
            this.detail_grid_view.Location = new Point(10, 10);
            this.detail_grid_view.MinimumSize = new Size(600, 400);
            this.detail_grid_view.Name = "detail_grid_view";
            this.detail_grid_view.RowHeadersVisible = false;
            this.detail_grid_view.RowTemplate.Height = 25;
            this.detail_grid_view.Size = new Size(1170, 710);
            this.detail_grid_view.TabIndex = 1;

            // 
            // ClusterDetailPopup
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.White;
            this.ClientSize = new Size(1200, 800);
            this.Controls.Add(this.tableLayoutMain);
            this.Font = new Font("맑은 고딕", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            this.MinimumSize = new Size(800, 600);
            this.Name = "ClusterDetailPopup";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "클러스터 세부 정보";

            // 컨트롤 정리
            this.tableLayoutMain.ResumeLayout(false);
            this.panel_top.ResumeLayout(false);
            this.panel_top.PerformLayout();
            this.pnl_data.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.detail_grid_view)).EndInit();
            this.ResumeLayout(false);
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