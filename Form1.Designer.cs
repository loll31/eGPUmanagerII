namespace eGPUManager
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            txtInstanceId = new TextBox();
            labelGpuInfo = new Label();
            lblGpuInfo = new Label();
            chkEnableManagement = new CheckBox();
            rtbLog = new RichTextBox();
            tableLayoutPanelMain = new TableLayoutPanel();
            pnlTop = new Panel();
            flowLayoutRight = new FlowLayoutPanel();
            flowLayoutNumeric = new FlowLayoutPanel();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Name = "label1";
            label1.TabIndex = 0;
            label1.Text = "Device InstanceId:";
            // 
            // txtInstanceId
            // 
            txtInstanceId.Name = "txtInstanceId";
            txtInstanceId.TabIndex = 1;
            txtInstanceId.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // labelGpuInfo
            // 
            labelGpuInfo.AutoSize = true;
            labelGpuInfo.Name = "labelGpuInfo";
            labelGpuInfo.TabIndex = 9;
            labelGpuInfo.Text = "GPU:";
            // 
            // lblGpuInfo
            // 
            lblGpuInfo.AutoSize = true;
            lblGpuInfo.Name = "lblGpuInfo";
            lblGpuInfo.TabIndex = 10;
            lblGpuInfo.Text = "Unknown";
            lblGpuInfo.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            // 
            // chkEnableManagement
            // 
            chkEnableManagement.AutoSize = true;
            chkEnableManagement.Name = "chkEnableManagement";
            chkEnableManagement.TabIndex = 2;
            chkEnableManagement.Text = "Enable Automatic Management";
            chkEnableManagement.UseVisualStyleBackColor = true;
            // 
            // btnRescan
            // 
            btnRescan = new Button();
            btnRescan.Name = "btnRescan";
            btnRescan.Size = new Size(180, 40);
            btnRescan.TabIndex = 4;
            btnRescan.Text = "Re-scan devices";
            btnRescan.UseVisualStyleBackColor = true;
            btnRescan.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRescan.Click += RescanDevices_Click;
            // 
            // lblRetryCount
            // 
            var lblRetryCount = new Label();
            lblRetryCount.AutoSize = true;
            lblRetryCount.Name = "lblRetryCount";
            lblRetryCount.TabIndex = 5;
            lblRetryCount.Text = "Retry attempts:";
            // 
            // nudRetryCount
            // 
            var nudRetryCount = new NumericUpDown();
            nudRetryCount.Name = "nudRetryCount";
            nudRetryCount.Minimum = 1;
            nudRetryCount.Maximum = 20;
            nudRetryCount.Value = 6;
            nudRetryCount.TabIndex = 6;
            nudRetryCount.ValueChanged += (s,e) => { ((Form1)Application.OpenForms[0]).NudRetryCount_ValueChanged(s,e); };
            // 
            // lblBackoff
            // 
            var lblBackoff = new Label();
            lblBackoff.AutoSize = true;
            lblBackoff.Name = "lblBackoff";
            lblBackoff.TabIndex = 7;
            lblBackoff.Text = "Initial backoff (seconds):";
            // 
            // nudBackoffSeconds
            // 
            var nudBackoffSeconds = new NumericUpDown();
            nudBackoffSeconds.Name = "nudBackoffSeconds";
            nudBackoffSeconds.Minimum = 1;
            nudBackoffSeconds.Maximum = 60;
            nudBackoffSeconds.Value = 1;
            nudBackoffSeconds.TabIndex = 8;
            nudBackoffSeconds.ValueChanged += (s,e) => { ((Form1)Application.OpenForms[0]).NudBackoffSeconds_ValueChanged(s,e); };
            // 
            // rtbLog
            // 
            rtbLog.Name = "rtbLog";
            rtbLog.ReadOnly = true;
            rtbLog.TabIndex = 3;
            rtbLog.Text = "";
            rtbLog.Dock = DockStyle.Fill;

            // tableLayoutPanelMain
            // 
            tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            tableLayoutPanelMain.ColumnCount = 2;
            tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableLayoutPanelMain.RowCount = 4;
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.Dock = DockStyle.Fill;

            // pnlTop (label + textbox)
            // 
            pnlTop.Name = "pnlTop";
            pnlTop.Dock = DockStyle.Fill;
            // Lay out label and textbox inside pnlTop using a nested TableLayoutPanel
            var topLayout = new TableLayoutPanel();
            topLayout.Name = "topLayout";
            topLayout.ColumnCount = 2;
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            topLayout.RowCount = 3;
            topLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            topLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            topLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            topLayout.Dock = DockStyle.Fill;
            topLayout.Padding = new Padding(8);
            label1.AutoSize = true;
            label1.Dock = DockStyle.None;
            label1.TextAlign = ContentAlignment.MiddleLeft;
            label1.Margin = new Padding(0, 0, 0, 0);
            label1.AutoEllipsis = false;
            txtInstanceId.Dock = DockStyle.Fill;
            txtInstanceId.Margin = new Padding(0, 0, 0, 0);
            txtInstanceId.Height = 30;
            lblGpuInfo.Dock = DockStyle.Fill;
            lblGpuInfo.Margin = new Padding(0, 2, 0, 0);
            // service status row
            labelService = new Label();
            labelService.AutoSize = true;
            labelService.Name = "labelService";
            labelService.TabIndex = 11;
            labelService.Text = "Service:";
            lblServiceStatus = new Label();
            lblServiceStatus.AutoSize = false;
            lblServiceStatus.Name = "lblServiceStatus";
            lblServiceStatus.TabIndex = 12;
            lblServiceStatus.Text = "Unknown";
            lblServiceStatus.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            lblServiceStatus.Dock = DockStyle.Fill;
            lblServiceStatus.Height = 40;
            lblServiceStatus.TextAlign = ContentAlignment.TopLeft;

            topLayout.Controls.Add(label1, 0, 0);
            topLayout.Controls.Add(txtInstanceId, 1, 0);
            topLayout.Controls.Add(labelGpuInfo, 0, 1);
            topLayout.Controls.Add(lblGpuInfo, 1, 1);
            topLayout.Controls.Add(labelService, 0, 2);
            topLayout.Controls.Add(lblServiceStatus, 1, 2);
            pnlTop.Controls.Add(topLayout);

            // flowLayoutRight (for rescan button and numeric controls)
            flowLayoutRight.Name = "flowLayoutRight";
            flowLayoutRight.FlowDirection = FlowDirection.TopDown;
            flowLayoutRight.AutoSize = true;
            flowLayoutRight.Dock = DockStyle.Top;
            flowLayoutRight.Padding = new Padding(8, 8, 8, 0);
            flowLayoutRight.Controls.Add(btnRescan);

            // flowLayoutNumeric for retry/backoff controls
            flowLayoutNumeric.Name = "flowLayoutNumeric";
            flowLayoutNumeric.FlowDirection = FlowDirection.LeftToRight;
            flowLayoutNumeric.AutoSize = true;
            flowLayoutNumeric.WrapContents = false;
            flowLayoutNumeric.Padding = new Padding(8, 32, 8, 8);
            lblRetryCount.AutoSize = true;
            lblRetryCount.TextAlign = ContentAlignment.MiddleLeft;
            lblRetryCount.Margin = new Padding(0, 8, 6, 0);
            nudRetryCount.Margin = new Padding(0, 6, 20, 0);
            lblBackoff.AutoSize = true;
            lblBackoff.TextAlign = ContentAlignment.MiddleLeft;
            lblBackoff.Margin = new Padding(0, 8, 6, 0);
            nudBackoffSeconds.Margin = new Padding(0, 6, 0, 0);
            flowLayoutNumeric.Controls.Add(lblRetryCount);
            flowLayoutNumeric.Controls.Add(nudRetryCount);
            flowLayoutNumeric.Controls.Add(lblBackoff);
            flowLayoutNumeric.Controls.Add(nudBackoffSeconds);

            // Place controls into tableLayoutPanel
            tableLayoutPanelMain.Controls.Add(pnlTop, 0, 0);
            tableLayoutPanelMain.SetColumnSpan(pnlTop, 1);
            tableLayoutPanelMain.Controls.Add(flowLayoutRight, 1, 0);
            tableLayoutPanelMain.Controls.Add(flowLayoutNumeric, 0, 1);
            tableLayoutPanelMain.Controls.Add(new Panel() { Width = 10, Height = 10 }, 1, 1);
            tableLayoutPanelMain.Controls.Add(chkEnableManagement, 0, 2);
            tableLayoutPanelMain.Controls.Add(new Panel() { Width = 10, Height = 10 }, 1, 2);
            tableLayoutPanelMain.Controls.Add(rtbLog, 0, 3);
            tableLayoutPanelMain.SetColumnSpan(rtbLog, 2);

            // Add the table layout to the form controls
            Controls.Add(tableLayoutPanelMain);
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(773, 633);
            Name = "Form1";
            Text = "Form1";
            this.Load += new EventHandler(Form1_Load);
            this.Resize += new EventHandler(Form1_Resize);
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtInstanceId;
        private Label labelGpuInfo;
        private Label lblGpuInfo;
        private Label lblServiceStatus;
        private Label labelService;
        private CheckBox chkEnableManagement;
        private Button btnRescan;
        private RichTextBox rtbLog;
        private TableLayoutPanel tableLayoutPanelMain;
        private Panel pnlTop;
        private FlowLayoutPanel flowLayoutRight;
        private FlowLayoutPanel flowLayoutNumeric;
    }
}
