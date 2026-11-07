namespace TestProjectWinForm
{
    partial class AutoTestTool
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AutoTestTool));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripOpenYAML = new System.Windows.Forms.ToolStripMenuItem();
            this.lstTestCases = new System.Windows.Forms.ListBox();
            this.btnExecute = new System.Windows.Forms.Button();
            this.btnExecuteAll = new System.Windows.Forms.Button();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnStress_test = new System.Windows.Forms.Button();
            this.LabelLog = new System.Windows.Forms.Label();
            this.LabelTestCase = new System.Windows.Forms.Label();
            this.textBox_StressTimes = new System.Windows.Forms.TextBox();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(470, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // ToolStripMenuItem
            // 
            this.ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripOpenYAML});
            this.ToolStripMenuItem.Name = "ToolStripMenuItem";
            this.ToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
            this.ToolStripMenuItem.Text = "Files";
            // 
            // toolStripOpenYAML
            // 
            this.toolStripOpenYAML.Name = "toolStripOpenYAML";
            this.toolStripOpenYAML.Size = new System.Drawing.Size(152, 22);
            this.toolStripOpenYAML.Text = "Open YAML";
            this.toolStripOpenYAML.Click += new System.EventHandler(this.ToolStripMenuItem_Click);
            // 
            // lstTestCases
            // 
            this.lstTestCases.FormattingEnabled = true;
            this.lstTestCases.ItemHeight = 12;
            this.lstTestCases.Items.AddRange(new object[] {
            "開啟用例文檔后請在該區域選擇要執行的用例"});
            this.lstTestCases.Location = new System.Drawing.Point(13, 44);
            this.lstTestCases.Name = "lstTestCases";
            this.lstTestCases.Size = new System.Drawing.Size(296, 100);
            this.lstTestCases.TabIndex = 1;
            // 
            // btnExecute
            // 
            this.btnExecute.Location = new System.Drawing.Point(383, 44);
            this.btnExecute.Name = "btnExecute";
            this.btnExecute.Size = new System.Drawing.Size(75, 23);
            this.btnExecute.TabIndex = 2;
            this.btnExecute.Text = "Execute";
            this.btnExecute.UseVisualStyleBackColor = true;
            this.btnExecute.Click += new System.EventHandler(this.btnExecute_Click);
            // 
            // btnExecuteAll
            // 
            this.btnExecuteAll.Location = new System.Drawing.Point(383, 84);
            this.btnExecuteAll.Name = "btnExecuteAll";
            this.btnExecuteAll.Size = new System.Drawing.Size(75, 23);
            this.btnExecuteAll.TabIndex = 3;
            this.btnExecuteAll.Text = "Execute All";
            this.btnExecuteAll.UseVisualStyleBackColor = true;
            this.btnExecuteAll.Click += new System.EventHandler(this.btnExecuteAll_Click);
            // 
            // rtbLog
            // 
            this.rtbLog.Location = new System.Drawing.Point(12, 168);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.Size = new System.Drawing.Size(446, 152);
            this.rtbLog.TabIndex = 4;
            this.rtbLog.Text = resources.GetString("rtbLog.Text");
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(13, 325);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(32, 12);
            this.lblStatus.TabIndex = 5;
            this.lblStatus.Text = "Status";
            // 
            // btnStress_test
            // 
            this.btnStress_test.Location = new System.Drawing.Point(383, 121);
            this.btnStress_test.Name = "btnStress_test";
            this.btnStress_test.Size = new System.Drawing.Size(75, 23);
            this.btnStress_test.TabIndex = 6;
            this.btnStress_test.Text = "Stress Test";
            this.btnStress_test.UseVisualStyleBackColor = true;
            this.btnStress_test.Click += new System.EventHandler(this.Stress_test_Click);
            // 
            // LabelLog
            // 
            this.LabelLog.AutoSize = true;
            this.LabelLog.Location = new System.Drawing.Point(13, 152);
            this.LabelLog.Name = "LabelLog";
            this.LabelLog.Size = new System.Drawing.Size(80, 12);
            this.LabelLog.TabIndex = 7;
            this.LabelLog.Text = "Log(日誌打印)";
            // 
            // LabelTestCase
            // 
            this.LabelTestCase.AutoSize = true;
            this.LabelTestCase.Location = new System.Drawing.Point(13, 28);
            this.LabelTestCase.Name = "LabelTestCase";
            this.LabelTestCase.Size = new System.Drawing.Size(102, 12);
            this.LabelTestCase.TabIndex = 8;
            this.LabelTestCase.Text = "Test case(測試用例)";
            // 
            // textBox_StressTimes
            // 
            this.textBox_StressTimes.Location = new System.Drawing.Point(318, 121);
            this.textBox_StressTimes.Name = "textBox_StressTimes";
            this.textBox_StressTimes.Size = new System.Drawing.Size(59, 22);
            this.textBox_StressTimes.TabIndex = 9;
            this.textBox_StressTimes.Tag = "";
            this.textBox_StressTimes.Text = "壓測次數";
            this.textBox_StressTimes.TextChanged += new System.EventHandler(this.textBox_StressTimes_TextChanged);
            // 
            // AutoTestTool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(470, 344);
            this.Controls.Add(this.textBox_StressTimes);
            this.Controls.Add(this.LabelTestCase);
            this.Controls.Add(this.LabelLog);
            this.Controls.Add(this.btnStress_test);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.rtbLog);
            this.Controls.Add(this.btnExecuteAll);
            this.Controls.Add(this.btnExecute);
            this.Controls.Add(this.lstTestCases);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "AutoTestTool";
            this.Text = "AutoTestTool";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AutoTestTool_FormClosing);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripOpenYAML;
        private System.Windows.Forms.ListBox lstTestCases;
        private System.Windows.Forms.Button btnExecute;
        private System.Windows.Forms.Button btnExecuteAll;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnStress_test;
        private System.Windows.Forms.Label LabelLog;
        private System.Windows.Forms.Label LabelTestCase;
        private System.Windows.Forms.TextBox textBox_StressTimes;
    }
}

