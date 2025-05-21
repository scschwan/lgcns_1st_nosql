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
            panel_top = new Panel();
            select_all_btn = new Button();
            status_label = new Label();
            unmerge_selected_btn = new Button();
            close_btn = new Button();
            detail_title_label = new Label();
            detail_grid_view = new DataGridView();
            panel_top.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)detail_grid_view).BeginInit();
            SuspendLayout();
            // 
            // panel_top
            // 
            panel_top.Controls.Add(select_all_btn);
            panel_top.Controls.Add(status_label);
            panel_top.Controls.Add(unmerge_selected_btn);
            panel_top.Controls.Add(close_btn);
            panel_top.Controls.Add(detail_title_label);
            panel_top.Dock = DockStyle.Top;
            panel_top.Location = new Point(0, 0);
            panel_top.Name = "panel_top";
            panel_top.Size = new Size(1784, 50);
            panel_top.TabIndex = 0;
            // 
            // select_all_btn
            // 
            select_all_btn.Location = new Point(633, 12);
            select_all_btn.Name = "select_all_btn";
            select_all_btn.Size = new Size(116, 28);
            select_all_btn.TabIndex = 4;
            select_all_btn.Text = "모두 선택";
            select_all_btn.UseVisualStyleBackColor = true;
            // 
            // status_label
            // 
            status_label.AutoSize = true;
            status_label.Location = new Point(316, 20);
            status_label.Name = "status_label";
            status_label.Size = new Size(42, 15);
            status_label.TabIndex = 3;
            status_label.Text = "총 0개";
            // 
            // unmerge_selected_btn
            // 
            unmerge_selected_btn.Location = new Point(755, 12);
            unmerge_selected_btn.Name = "unmerge_selected_btn";
            unmerge_selected_btn.Size = new Size(133, 28);
            unmerge_selected_btn.TabIndex = 2;
            unmerge_selected_btn.Text = "선택 항목 병합 해제";
            unmerge_selected_btn.UseVisualStyleBackColor = true;
            // 
            // close_btn
            // 
            close_btn.Location = new Point(1008, 12);
            close_btn.Name = "close_btn";
            close_btn.Size = new Size(75, 28);
            close_btn.TabIndex = 1;
            close_btn.Text = "닫기";
            close_btn.UseVisualStyleBackColor = true;
            // 
            // detail_title_label
            // 
            detail_title_label.AutoSize = true;
            detail_title_label.Font = new Font("맑은 고딕", 12F, FontStyle.Bold, GraphicsUnit.Point, 129);
            detail_title_label.Location = new Point(12, 14);
            detail_title_label.Name = "detail_title_label";
            detail_title_label.Size = new Size(150, 21);
            detail_title_label.TabIndex = 0;
            detail_title_label.Text = "클러스터 세부 정보";
            // 
            // detail_grid_view
            // 
            detail_grid_view.AllowUserToAddRows = false;
            detail_grid_view.Dock = DockStyle.Fill;
            detail_grid_view.Location = new Point(0, 50);
            detail_grid_view.Name = "detail_grid_view";
            detail_grid_view.RowHeadersVisible = false;
            detail_grid_view.RowTemplate.Height = 23;
            detail_grid_view.Size = new Size(1784, 861);
            detail_grid_view.TabIndex = 1;
            // 
            // ClusterDetailPopup
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1784, 911);
            Controls.Add(detail_grid_view);
            Controls.Add(panel_top);
            Font = new Font("맑은 고딕", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ClusterDetailPopup";
            StartPosition = FormStartPosition.CenterParent;
            Text = "클러스터 세부 정보";
            panel_top.ResumeLayout(false);
            panel_top.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)detail_grid_view).EndInit();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel_top;
        private System.Windows.Forms.Label detail_title_label;
        private System.Windows.Forms.Button close_btn;
        private System.Windows.Forms.Button unmerge_selected_btn;
        private System.Windows.Forms.Label status_label;
        private System.Windows.Forms.DataGridView detail_grid_view;
        private System.Windows.Forms.Button select_all_btn;
    }
}