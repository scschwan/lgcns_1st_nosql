namespace FinanceTool
{
    partial class uc_preprocessing
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
            label9 = new Label();
            label10 = new Label();
            tableLayoutContent = new TableLayoutPanel();
            tableLayoutLeft = new TableLayoutPanel();
            dataGridView_target = new DataGridView();
            dataGridView_applied = new DataGridView();
            tableLayoutRight = new TableLayoutPanel();
            gb_separator = new GroupBox();
            seper_apply_btn = new Button();
            seper_list_allcheck = new CheckBox();
            dataGridView_seperator = new DataGridView();
            seper_del_btn = new Button();
            seper_add_btn = new Button();
            new_seper_word = new TextBox();
            groupBox2 = new GroupBox();
            remove_apply_btn = new Button();
            remove_list_allcheck = new CheckBox();
            dataGridView_remove = new DataGridView();
            remove_del_btn = new Button();
            remove_add_btn = new Button();
            new_remove_word = new TextBox();
            groupBox1 = new GroupBox();
            remove_1key = new Button();
            keyword_seper_split = new Button();
            nlp_groupBox = new GroupBox();
            keyword_model_split = new Button();
            label1 = new Label();
            label8 = new Label();
            ai_limit_count = new NumericUpDown();
            label6 = new Label();
            pnl_footer = new Panel();
            button5 = new Button();
            current_sessionName = new Label();
            tableLayoutMain.SuspendLayout();
            pnl_header.SuspendLayout();
            tableLayoutContent.SuspendLayout();
            tableLayoutLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_target).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView_applied).BeginInit();
            tableLayoutRight.SuspendLayout();
            gb_separator.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_seperator).BeginInit();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_remove).BeginInit();
            groupBox1.SuspendLayout();
            nlp_groupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)ai_limit_count).BeginInit();
            pnl_footer.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutMain
            // 
            tableLayoutMain.ColumnCount = 1;
            tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutMain.Controls.Add(pnl_header, 0, 0);
            tableLayoutMain.Controls.Add(tableLayoutContent, 0, 1);
            tableLayoutMain.Controls.Add(pnl_footer, 0, 2);
            tableLayoutMain.Dock = DockStyle.Fill;
            tableLayoutMain.Location = new Point(0, 0);
            tableLayoutMain.Margin = new Padding(0);
            tableLayoutMain.Name = "tableLayoutMain";
            tableLayoutMain.RowCount = 3;
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            tableLayoutMain.Size = new Size(1904, 1017);
            tableLayoutMain.TabIndex = 0;
            // 
            // pnl_header
            // 
            pnl_header.BackColor = Color.WhiteSmoke;
            pnl_header.Controls.Add(current_sessionName);
            pnl_header.Controls.Add(label9);
            pnl_header.Controls.Add(label10);
            pnl_header.Dock = DockStyle.Fill;
            pnl_header.Location = new Point(0, 0);
            pnl_header.Margin = new Padding(0);
            pnl_header.Name = "pnl_header";
            pnl_header.Size = new Size(1904, 80);
            pnl_header.TabIndex = 0;
            // 
            // label9
            // 
            label9.Anchor = AnchorStyles.None;
            label9.BackColor = Color.SteelBlue;
            label9.Font = new Font("Pretendard", 18F, FontStyle.Bold);
            label9.ForeColor = Color.White;
            label9.Location = new Point(860, 20);
            label9.Name = "label9";
            label9.Size = new Size(380, 40);
            label9.TabIndex = 48;
            label9.Text = "키워드 추출 결과";
            label9.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label10
            // 
            label10.Anchor = AnchorStyles.None;
            label10.BackColor = Color.SteelBlue;
            label10.Font = new Font("Pretendard", 18F, FontStyle.Bold);
            label10.ForeColor = Color.White;
            label10.Location = new Point(117, 20);
            label10.Name = "label10";
            label10.Size = new Size(380, 40);
            label10.TabIndex = 47;
            label10.Text = "키워드 추출 대상";
            label10.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tableLayoutContent
            // 
            tableLayoutContent.ColumnCount = 2;
            tableLayoutContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72F));
            tableLayoutContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            tableLayoutContent.Controls.Add(tableLayoutLeft, 0, 0);
            tableLayoutContent.Controls.Add(tableLayoutRight, 1, 0);
            tableLayoutContent.Dock = DockStyle.Fill;
            tableLayoutContent.Location = new Point(0, 80);
            tableLayoutContent.Margin = new Padding(0);
            tableLayoutContent.Name = "tableLayoutContent";
            tableLayoutContent.RowCount = 1;
            tableLayoutContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutContent.Size = new Size(1904, 877);
            tableLayoutContent.TabIndex = 1;
            // 
            // tableLayoutLeft
            // 
            tableLayoutLeft.ColumnCount = 2;
            tableLayoutLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutLeft.Controls.Add(dataGridView_target, 0, 0);
            tableLayoutLeft.Controls.Add(dataGridView_applied, 1, 0);
            tableLayoutLeft.Dock = DockStyle.Fill;
            tableLayoutLeft.Location = new Point(10, 10);
            tableLayoutLeft.Margin = new Padding(10, 10, 5, 10);
            tableLayoutLeft.Name = "tableLayoutLeft";
            tableLayoutLeft.RowCount = 1;
            tableLayoutLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutLeft.Size = new Size(1355, 857);
            tableLayoutLeft.TabIndex = 0;
            // 
            // dataGridView_target
            // 
            dataGridView_target.AllowUserToAddRows = false;
            dataGridView_target.AllowUserToDeleteRows = false;
            dataGridView_target.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView_target.BackgroundColor = Color.White;
            dataGridView_target.BorderStyle = BorderStyle.Fixed3D;
            dataGridView_target.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView_target.Location = new Point(5, 5);
            dataGridView_target.Margin = new Padding(5);
            dataGridView_target.MinimumSize = new Size(400, 300);
            dataGridView_target.Name = "dataGridView_target";
            dataGridView_target.ReadOnly = true;
            dataGridView_target.RowHeadersVisible = false;
            dataGridView_target.Size = new Size(667, 847);
            dataGridView_target.TabIndex = 21;
            // 
            // dataGridView_applied
            // 
            dataGridView_applied.AllowUserToAddRows = false;
            dataGridView_applied.AllowUserToDeleteRows = false;
            dataGridView_applied.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView_applied.BackgroundColor = Color.White;
            dataGridView_applied.BorderStyle = BorderStyle.Fixed3D;
            dataGridView_applied.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView_applied.Location = new Point(682, 5);
            dataGridView_applied.Margin = new Padding(5);
            dataGridView_applied.MinimumSize = new Size(400, 300);
            dataGridView_applied.Name = "dataGridView_applied";
            dataGridView_applied.ReadOnly = true;
            dataGridView_applied.RowHeadersVisible = false;
            dataGridView_applied.Size = new Size(668, 847);
            dataGridView_applied.TabIndex = 28;
            // 
            // tableLayoutRight
            // 
            tableLayoutRight.ColumnCount = 1;
            tableLayoutRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutRight.Controls.Add(gb_separator, 0, 0);
            tableLayoutRight.Controls.Add(groupBox2, 0, 1);
            tableLayoutRight.Controls.Add(groupBox1, 0, 2);
            tableLayoutRight.Controls.Add(nlp_groupBox, 0, 3);
            tableLayoutRight.Dock = DockStyle.Fill;
            tableLayoutRight.Location = new Point(1375, 10);
            tableLayoutRight.Margin = new Padding(5, 10, 10, 10);
            tableLayoutRight.Name = "tableLayoutRight";
            tableLayoutRight.RowCount = 4;
            tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 15F));
            tableLayoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            tableLayoutRight.Size = new Size(519, 857);
            tableLayoutRight.TabIndex = 1;
            // 
            // gb_separator
            // 
            gb_separator.Controls.Add(seper_apply_btn);
            gb_separator.Controls.Add(seper_list_allcheck);
            gb_separator.Controls.Add(dataGridView_seperator);
            gb_separator.Controls.Add(seper_del_btn);
            gb_separator.Controls.Add(seper_add_btn);
            gb_separator.Controls.Add(new_seper_word);
            gb_separator.Dock = DockStyle.Fill;
            gb_separator.Font = new Font("Pretendard", 12F, FontStyle.Bold);
            gb_separator.Location = new Point(5, 5);
            gb_separator.Margin = new Padding(5);
            gb_separator.Name = "gb_separator";
            gb_separator.Padding = new Padding(8);
            gb_separator.Size = new Size(509, 247);
            gb_separator.TabIndex = 22;
            gb_separator.TabStop = false;
            gb_separator.Text = "구분자 변환";
            // 
            // seper_apply_btn
            // 
            seper_apply_btn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            seper_apply_btn.AutoSize = true;
            seper_apply_btn.BackColor = Color.Orange;
            seper_apply_btn.FlatStyle = FlatStyle.Flat;
            seper_apply_btn.Font = new Font("Pretendard", 11F, FontStyle.Bold);
            seper_apply_btn.ForeColor = Color.White;
            seper_apply_btn.Location = new Point(351, 155);
            seper_apply_btn.MinimumSize = new Size(100, 30);
            seper_apply_btn.Name = "seper_apply_btn";
            seper_apply_btn.Size = new Size(150, 35);
            seper_apply_btn.TabIndex = 29;
            seper_apply_btn.Text = "구분자 변환";
            seper_apply_btn.UseVisualStyleBackColor = false;
            seper_apply_btn.Visible = false;
            seper_apply_btn.Click += btn_apply_Click;
            // 
            // seper_list_allcheck
            // 
            seper_list_allcheck.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            seper_list_allcheck.AutoSize = true;
            seper_list_allcheck.Font = new Font("Pretendard", 11F);
            seper_list_allcheck.Location = new Point(416, 65);
            seper_list_allcheck.Name = "seper_list_allcheck";
            seper_list_allcheck.Size = new Size(83, 22);
            seper_list_allcheck.TabIndex = 44;
            seper_list_allcheck.Text = "전체 선택";
            seper_list_allcheck.UseVisualStyleBackColor = true;
            seper_list_allcheck.CheckedChanged += seper_list_allcheck_CheckedChanged;
            // 
            // dataGridView_seperator
            // 
            dataGridView_seperator.AllowUserToAddRows = false;
            dataGridView_seperator.AllowUserToDeleteRows = false;
            dataGridView_seperator.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView_seperator.BackgroundColor = Color.White;
            dataGridView_seperator.BorderStyle = BorderStyle.Fixed3D;
            dataGridView_seperator.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView_seperator.Location = new Point(8, 65);
            dataGridView_seperator.MinimumSize = new Size(250, 80);
            dataGridView_seperator.Name = "dataGridView_seperator";
            dataGridView_seperator.RowHeadersVisible = false;
            dataGridView_seperator.Size = new Size(336, 120);
            dataGridView_seperator.TabIndex = 43;
            // 
            // seper_del_btn
            // 
            seper_del_btn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            seper_del_btn.AutoSize = true;
            seper_del_btn.BackColor = Color.Crimson;
            seper_del_btn.FlatStyle = FlatStyle.Flat;
            seper_del_btn.Font = new Font("Pretendard", 11F, FontStyle.Bold);
            seper_del_btn.ForeColor = Color.White;
            seper_del_btn.Location = new Point(351, 196);
            seper_del_btn.MinimumSize = new Size(100, 30);
            seper_del_btn.Name = "seper_del_btn";
            seper_del_btn.Size = new Size(150, 35);
            seper_del_btn.TabIndex = 24;
            seper_del_btn.Text = "항목 제거";
            seper_del_btn.UseVisualStyleBackColor = false;
            seper_del_btn.Click += seper_del_btn_Click;
            // 
            // seper_add_btn
            // 
            seper_add_btn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            seper_add_btn.AutoSize = true;
            seper_add_btn.BackColor = Color.DodgerBlue;
            seper_add_btn.FlatStyle = FlatStyle.Flat;
            seper_add_btn.Font = new Font("Pretendard", 11F, FontStyle.Bold);
            seper_add_btn.ForeColor = Color.White;
            seper_add_btn.Location = new Point(351, 25);
            seper_add_btn.MinimumSize = new Size(100, 30);
            seper_add_btn.Name = "seper_add_btn";
            seper_add_btn.Size = new Size(150, 35);
            seper_add_btn.TabIndex = 23;
            seper_add_btn.Text = "대상 추가";
            seper_add_btn.UseVisualStyleBackColor = false;
            seper_add_btn.Click += seper_add_btn_Click;
            // 
            // new_seper_word
            // 
            new_seper_word.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            new_seper_word.Font = new Font("Pretendard", 11F);
            new_seper_word.Location = new Point(8, 30);
            new_seper_word.Name = "new_seper_word";
            new_seper_word.PlaceholderText = "신규 변환 대상 입력";
            new_seper_word.Size = new Size(336, 25);
            new_seper_word.TabIndex = 27;
            new_seper_word.KeyDown += new_seper_word_KeyDown;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(remove_apply_btn);
            groupBox2.Controls.Add(remove_list_allcheck);
            groupBox2.Controls.Add(dataGridView_remove);
            groupBox2.Controls.Add(remove_del_btn);
            groupBox2.Controls.Add(remove_add_btn);
            groupBox2.Controls.Add(new_remove_word);
            groupBox2.Dock = DockStyle.Fill;
            groupBox2.Font = new Font("Pretendard", 12F, FontStyle.Bold);
            groupBox2.Location = new Point(5, 262);
            groupBox2.Margin = new Padding(5);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(8);
            groupBox2.Size = new Size(509, 247);
            groupBox2.TabIndex = 27;
            groupBox2.TabStop = false;
            groupBox2.Text = "불용어 제거";
            // 
            // remove_apply_btn
            // 
            remove_apply_btn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            remove_apply_btn.AutoSize = true;
            remove_apply_btn.BackColor = Color.Orange;
            remove_apply_btn.FlatStyle = FlatStyle.Flat;
            remove_apply_btn.Font = new Font("Pretendard", 11F, FontStyle.Bold);
            remove_apply_btn.ForeColor = Color.White;
            remove_apply_btn.Location = new Point(351, 155);
            remove_apply_btn.MinimumSize = new Size(100, 30);
            remove_apply_btn.Name = "remove_apply_btn";
            remove_apply_btn.Size = new Size(150, 35);
            remove_apply_btn.TabIndex = 47;
            remove_apply_btn.Text = "불용어 제거";
            remove_apply_btn.UseVisualStyleBackColor = false;
            remove_apply_btn.Visible = false;
            remove_apply_btn.Click += remove_apply_btn_Click;
            // 
            // remove_list_allcheck
            // 
            remove_list_allcheck.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            remove_list_allcheck.AutoSize = true;
            remove_list_allcheck.Font = new Font("Pretendard", 11F);
            remove_list_allcheck.Location = new Point(416, 65);
            remove_list_allcheck.Name = "remove_list_allcheck";
            remove_list_allcheck.Size = new Size(83, 22);
            remove_list_allcheck.TabIndex = 48;
            remove_list_allcheck.Text = "전체 선택";
            remove_list_allcheck.UseVisualStyleBackColor = true;
            remove_list_allcheck.CheckedChanged += remove_list_allcheck_CheckedChanged;
            // 
            // dataGridView_remove
            // 
            dataGridView_remove.AllowUserToAddRows = false;
            dataGridView_remove.AllowUserToDeleteRows = false;
            dataGridView_remove.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView_remove.BackgroundColor = Color.White;
            dataGridView_remove.BorderStyle = BorderStyle.Fixed3D;
            dataGridView_remove.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView_remove.Location = new Point(8, 65);
            dataGridView_remove.MinimumSize = new Size(250, 80);
            dataGridView_remove.Name = "dataGridView_remove";
            dataGridView_remove.RowHeadersVisible = false;
            dataGridView_remove.Size = new Size(336, 120);
            dataGridView_remove.TabIndex = 45;
            // 
            // remove_del_btn
            // 
            remove_del_btn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            remove_del_btn.AutoSize = true;
            remove_del_btn.BackColor = Color.Crimson;
            remove_del_btn.FlatStyle = FlatStyle.Flat;
            remove_del_btn.Font = new Font("Pretendard", 11F, FontStyle.Bold);
            remove_del_btn.ForeColor = Color.White;
            remove_del_btn.Location = new Point(351, 196);
            remove_del_btn.MinimumSize = new Size(100, 30);
            remove_del_btn.Name = "remove_del_btn";
            remove_del_btn.Size = new Size(150, 35);
            remove_del_btn.TabIndex = 46;
            remove_del_btn.Text = "항목 제거";
            remove_del_btn.UseVisualStyleBackColor = false;
            remove_del_btn.Click += remove_del_btn_Click;
            // 
            // remove_add_btn
            // 
            remove_add_btn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            remove_add_btn.AutoSize = true;
            remove_add_btn.BackColor = Color.DodgerBlue;
            remove_add_btn.FlatStyle = FlatStyle.Flat;
            remove_add_btn.Font = new Font("Pretendard", 11F, FontStyle.Bold);
            remove_add_btn.ForeColor = Color.White;
            remove_add_btn.Location = new Point(351, 25);
            remove_add_btn.MinimumSize = new Size(100, 30);
            remove_add_btn.Name = "remove_add_btn";
            remove_add_btn.Size = new Size(150, 35);
            remove_add_btn.TabIndex = 45;
            remove_add_btn.Text = "대상 추가";
            remove_add_btn.UseVisualStyleBackColor = false;
            remove_add_btn.Click += remove_add_btn_Click;
            // 
            // new_remove_word
            // 
            new_remove_word.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            new_remove_word.Font = new Font("Pretendard", 11F);
            new_remove_word.Location = new Point(8, 30);
            new_remove_word.Name = "new_remove_word";
            new_remove_word.PlaceholderText = "신규 불용어 대상 입력";
            new_remove_word.Size = new Size(336, 25);
            new_remove_word.TabIndex = 25;
            new_remove_word.KeyDown += tb_remove_KeyDown;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(remove_1key);
            groupBox1.Controls.Add(keyword_seper_split);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Font = new Font("Pretendard", 12F, FontStyle.Bold);
            groupBox1.Location = new Point(5, 519);
            groupBox1.Margin = new Padding(5);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(8);
            groupBox1.Size = new Size(509, 118);
            groupBox1.TabIndex = 49;
            groupBox1.TabStop = false;
            groupBox1.Text = "구분자 기반 키워드 추출";
            // 
            // remove_1key
            // 
            remove_1key.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            remove_1key.AutoSize = true;
            remove_1key.BackColor = Color.Orange;
            remove_1key.FlatStyle = FlatStyle.Flat;
            remove_1key.Font = new Font("Pretendard", 11F, FontStyle.Bold);
            remove_1key.ForeColor = Color.White;
            remove_1key.Location = new Point(281, 40);
            remove_1key.MinimumSize = new Size(150, 35);
            remove_1key.Name = "remove_1key";
            remove_1key.Size = new Size(220, 40);
            remove_1key.TabIndex = 39;
            remove_1key.Text = "1글자 키워드 제거";
            remove_1key.UseVisualStyleBackColor = false;
            remove_1key.Click += remove_1key_Click;
            // 
            // keyword_seper_split
            // 
            keyword_seper_split.AutoSize = true;
            keyword_seper_split.BackColor = Color.LimeGreen;
            keyword_seper_split.FlatStyle = FlatStyle.Flat;
            keyword_seper_split.Font = new Font("Pretendard", 11F, FontStyle.Bold);
            keyword_seper_split.ForeColor = Color.White;
            keyword_seper_split.Location = new Point(15, 40);
            keyword_seper_split.MinimumSize = new Size(150, 35);
            keyword_seper_split.Name = "keyword_seper_split";
            keyword_seper_split.Size = new Size(180, 40);
            keyword_seper_split.TabIndex = 38;
            keyword_seper_split.Text = "키워드 추출";
            keyword_seper_split.UseVisualStyleBackColor = false;
            keyword_seper_split.Click += keyword_seper_split_Click;
            // 
            // nlp_groupBox
            // 
            nlp_groupBox.Controls.Add(keyword_model_split);
            nlp_groupBox.Controls.Add(label1);
            nlp_groupBox.Controls.Add(label8);
            nlp_groupBox.Controls.Add(ai_limit_count);
            nlp_groupBox.Controls.Add(label6);
            nlp_groupBox.Dock = DockStyle.Fill;
            nlp_groupBox.Font = new Font("Pretendard", 12F, FontStyle.Bold);
            nlp_groupBox.Location = new Point(5, 647);
            nlp_groupBox.Margin = new Padding(5);
            nlp_groupBox.Name = "nlp_groupBox";
            nlp_groupBox.Padding = new Padding(8);
            nlp_groupBox.Size = new Size(509, 205);
            nlp_groupBox.TabIndex = 29;
            nlp_groupBox.TabStop = false;
            nlp_groupBox.Text = "NLP 기반 키워드 추출";
            nlp_groupBox.Visible = false;
            // 
            // keyword_model_split
            // 
            keyword_model_split.Anchor = AnchorStyles.Bottom;
            keyword_model_split.AutoSize = true;
            keyword_model_split.BackColor = Color.MediumOrchid;
            keyword_model_split.FlatStyle = FlatStyle.Flat;
            keyword_model_split.Font = new Font("Pretendard", 12F, FontStyle.Bold);
            keyword_model_split.ForeColor = Color.White;
            keyword_model_split.Location = new Point(180, 150);
            keyword_model_split.MinimumSize = new Size(120, 35);
            keyword_model_split.Name = "keyword_model_split";
            keyword_model_split.Size = new Size(150, 40);
            keyword_model_split.TabIndex = 34;
            keyword_model_split.Text = "키워드 추출";
            keyword_model_split.UseVisualStyleBackColor = false;
            keyword_model_split.Click += keyword_model_split_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Pretendard", 10F, FontStyle.Bold);
            label1.ForeColor = Color.IndianRed;
            label1.Location = new Point(15, 70);
            label1.Name = "label1";
            label1.Size = new Size(215, 17);
            label1.TabIndex = 37;
            label1.Text = "AI가 추가적으로 키워드를 분할합니다.";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Font = new Font("Pretendard", 11F);
            label8.Location = new Point(130, 110);
            label8.Name = "label8";
            label8.Size = new Size(167, 18);
            label8.TabIndex = 36;
            label8.Text = "글자 이상 키워드 자동 분할";
            // 
            // ai_limit_count
            // 
            ai_limit_count.Font = new Font("Pretendard", 11F);
            ai_limit_count.Location = new Point(40, 108);
            ai_limit_count.Name = "ai_limit_count";
            ai_limit_count.Size = new Size(80, 25);
            ai_limit_count.TabIndex = 35;
            ai_limit_count.TextAlign = HorizontalAlignment.Center;
            ai_limit_count.Value = new decimal(new int[] { 4, 0, 0, 0 });
            ai_limit_count.ValueChanged += ai_limit_count_ValueChanged;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Pretendard", 10F, FontStyle.Bold);
            label6.ForeColor = Color.IndianRed;
            label6.Location = new Point(15, 45);
            label6.Name = "label6";
            label6.Size = new Size(190, 17);
            label6.TabIndex = 31;
            label6.Text = "※ 구분자 기반으로 키워드 추출 후";
            // 
            // pnl_footer
            // 
            pnl_footer.BackColor = Color.WhiteSmoke;
            pnl_footer.Controls.Add(button5);
            pnl_footer.Dock = DockStyle.Fill;
            pnl_footer.Location = new Point(0, 957);
            pnl_footer.Margin = new Padding(0);
            pnl_footer.Name = "pnl_footer";
            pnl_footer.Size = new Size(1904, 60);
            pnl_footer.TabIndex = 2;
            // 
            // button5
            // 
            button5.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button5.AutoSize = true;
            button5.BackColor = Color.LimeGreen;
            button5.FlatStyle = FlatStyle.Flat;
            button5.Font = new Font("Pretendard", 14F, FontStyle.Bold);
            button5.ForeColor = Color.White;
            button5.Location = new Point(1750, 12);
            button5.MinimumSize = new Size(100, 35);
            button5.Name = "button5";
            button5.Size = new Size(140, 40);
            button5.TabIndex = 38;
            button5.Text = "완  료";
            button5.UseVisualStyleBackColor = false;
            button5.Click += btn_complete_Click;
            // 
            // current_sessionName
            // 
            current_sessionName.AutoSize = true;
            current_sessionName.Font = new Font("Pretendard", 18F, FontStyle.Bold, GraphicsUnit.Point, 129);
            current_sessionName.Location = new Point(1375, 40);
            current_sessionName.Name = "current_sessionName";
            current_sessionName.Size = new Size(143, 29);
            current_sessionName.TabIndex = 48;
            current_sessionName.Text = "현재 세션명 : ";
            // 
            // uc_preprocessing
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            Controls.Add(tableLayoutMain);
            MinimumSize = new Size(1280, 800);
            Name = "uc_preprocessing";
            Size = new Size(1904, 1017);
            tableLayoutMain.ResumeLayout(false);
            pnl_header.ResumeLayout(false);
            pnl_header.PerformLayout();
            tableLayoutContent.ResumeLayout(false);
            tableLayoutLeft.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView_target).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView_applied).EndInit();
            tableLayoutRight.ResumeLayout(false);
            gb_separator.ResumeLayout(false);
            gb_separator.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_seperator).EndInit();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_remove).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            nlp_groupBox.ResumeLayout(false);
            nlp_groupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)ai_limit_count).EndInit();
            pnl_footer.ResumeLayout(false);
            pnl_footer.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
        private System.Windows.Forms.Panel pnl_header;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TableLayoutPanel tableLayoutContent;
        private System.Windows.Forms.TableLayoutPanel tableLayoutLeft;
        private System.Windows.Forms.DataGridView dataGridView_target;
        private System.Windows.Forms.DataGridView dataGridView_applied;
        private System.Windows.Forms.TableLayoutPanel tableLayoutRight;
        private System.Windows.Forms.GroupBox gb_separator;
        private System.Windows.Forms.Button seper_apply_btn;
        private System.Windows.Forms.CheckBox seper_list_allcheck;
        private System.Windows.Forms.DataGridView dataGridView_seperator;
        private System.Windows.Forms.Button seper_del_btn;
        private System.Windows.Forms.Button seper_add_btn;
        private System.Windows.Forms.TextBox new_seper_word;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button remove_apply_btn;
        private System.Windows.Forms.CheckBox remove_list_allcheck;
        private System.Windows.Forms.DataGridView dataGridView_remove;
        private System.Windows.Forms.Button remove_del_btn;
        private System.Windows.Forms.Button remove_add_btn;
        private System.Windows.Forms.TextBox new_remove_word;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button remove_1key;
        private System.Windows.Forms.Button keyword_seper_split;
        private System.Windows.Forms.GroupBox nlp_groupBox;
        private System.Windows.Forms.Button keyword_model_split;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown ai_limit_count;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Panel pnl_footer;
        private System.Windows.Forms.Button button5;
        private Label current_sessionName;
    }
}
