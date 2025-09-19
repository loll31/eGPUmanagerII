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
            chkEnableManagement = new CheckBox();
            rtbLog = new RichTextBox();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(28, 28);
            label1.Name = "label1";
            label1.Size = new Size(182, 30);
            label1.TabIndex = 0;
            label1.Text = "Device InstanceId:";
            // 
            // txtInstanceId
            // 
            txtInstanceId.Location = new Point(212, 25);
            txtInstanceId.Name = "txtInstanceId";
            txtInstanceId.Size = new Size(526, 35);
            txtInstanceId.TabIndex = 1;
            // 
            // chkEnableManagement
            // 
            chkEnableManagement.AutoSize = true;
            chkEnableManagement.Location = new Point(28, 86);
            chkEnableManagement.Name = "chkEnableManagement";
            chkEnableManagement.Size = new Size(298, 34);
            chkEnableManagement.TabIndex = 2;
            chkEnableManagement.Text = "Abilita Gestione Automatica";
            chkEnableManagement.UseVisualStyleBackColor = true;
            // 
            // rtbLog
            // 
            rtbLog.Location = new Point(28, 143);
            rtbLog.Name = "rtbLog";
            rtbLog.ReadOnly = true;
            rtbLog.Size = new Size(710, 469);
            rtbLog.TabIndex = 3;
            rtbLog.Text = "";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(773, 633);
            Controls.Add(rtbLog);
            Controls.Add(chkEnableManagement);
            Controls.Add(txtInstanceId);
            Controls.Add(label1);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtInstanceId;
        private CheckBox chkEnableManagement;
        private RichTextBox rtbLog;
    }
}
