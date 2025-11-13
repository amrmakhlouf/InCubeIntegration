namespace InCubeIntegration_UI
{
    partial class frmDesignInterface
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
            this.btnSave = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.txtImportType = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtDescription = new System.Windows.Forms.RichTextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.cmbImportType = new System.Windows.Forms.ComboBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tpInfo = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txtSheet3Staging = new System.Windows.Forms.TextBox();
            this.txtSheet2Staging = new System.Windows.Forms.TextBox();
            this.txtSheet3Desc = new System.Windows.Forms.TextBox();
            this.txtSheet2Desc = new System.Windows.Forms.TextBox();
            this.cbSheet3 = new System.Windows.Forms.CheckBox();
            this.cbSheet2 = new System.Windows.Forms.CheckBox();
            this.txtSheet1Staging = new System.Windows.Forms.TextBox();
            this.txtSheet1Desc = new System.Windows.Forms.TextBox();
            this.cbSheet1 = new System.Windows.Forms.CheckBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.tpProcs = new System.Windows.Forms.TabPage();
            this.txtProc3 = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtProc2 = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtProc1 = new System.Windows.Forms.TextBox();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tpInfo.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tpProcs.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(86, 328);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(92, 23);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Import Type";
            // 
            // txtImportType
            // 
            this.txtImportType.Location = new System.Drawing.Point(83, 15);
            this.txtImportType.Name = "txtImportType";
            this.txtImportType.Size = new System.Drawing.Size(262, 20);
            this.txtImportType.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Decription";
            // 
            // txtDescription
            // 
            this.txtDescription.Location = new System.Drawing.Point(83, 46);
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.Size = new System.Drawing.Size(262, 96);
            this.txtDescription.TabIndex = 2;
            this.txtDescription.Text = "";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(13, 11);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(63, 13);
            this.label7.TabIndex = 14;
            this.label7.Text = "Import Type";
            // 
            // cmbImportType
            // 
            this.cmbImportType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbImportType.FormattingEnabled = true;
            this.cmbImportType.Location = new System.Drawing.Point(104, 8);
            this.cmbImportType.Name = "cmbImportType";
            this.cmbImportType.Size = new System.Drawing.Size(175, 21);
            this.cmbImportType.TabIndex = 15;
            this.cmbImportType.SelectedIndexChanged += new System.EventHandler(this.cmbImportType_SelectedIndexChanged);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tpInfo);
            this.tabControl1.Controls.Add(this.tpProcs);
            this.tabControl1.Location = new System.Drawing.Point(12, 35);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(362, 290);
            this.tabControl1.TabIndex = 16;
            // 
            // tpInfo
            // 
            this.tpInfo.Controls.Add(this.groupBox1);
            this.tpInfo.Controls.Add(this.txtDescription);
            this.tpInfo.Controls.Add(this.label1);
            this.tpInfo.Controls.Add(this.txtImportType);
            this.tpInfo.Controls.Add(this.label2);
            this.tpInfo.Location = new System.Drawing.Point(4, 22);
            this.tpInfo.Name = "tpInfo";
            this.tpInfo.Padding = new System.Windows.Forms.Padding(3);
            this.tpInfo.Size = new System.Drawing.Size(354, 264);
            this.tpInfo.TabIndex = 0;
            this.tpInfo.Text = "Import Information";
            this.tpInfo.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.txtSheet3Staging);
            this.groupBox1.Controls.Add(this.txtSheet2Staging);
            this.groupBox1.Controls.Add(this.txtSheet3Desc);
            this.groupBox1.Controls.Add(this.txtSheet2Desc);
            this.groupBox1.Controls.Add(this.cbSheet3);
            this.groupBox1.Controls.Add(this.cbSheet2);
            this.groupBox1.Controls.Add(this.txtSheet1Staging);
            this.groupBox1.Controls.Add(this.txtSheet1Desc);
            this.groupBox1.Controls.Add(this.cbSheet1);
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Location = new System.Drawing.Point(9, 148);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(336, 109);
            this.groupBox1.TabIndex = 12;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Sheets:";
            // 
            // txtSheet3Staging
            // 
            this.txtSheet3Staging.Location = new System.Drawing.Point(203, 81);
            this.txtSheet3Staging.Name = "txtSheet3Staging";
            this.txtSheet3Staging.ReadOnly = true;
            this.txtSheet3Staging.Size = new System.Drawing.Size(123, 20);
            this.txtSheet3Staging.TabIndex = 10;
            // 
            // txtSheet2Staging
            // 
            this.txtSheet2Staging.Location = new System.Drawing.Point(203, 57);
            this.txtSheet2Staging.Name = "txtSheet2Staging";
            this.txtSheet2Staging.ReadOnly = true;
            this.txtSheet2Staging.Size = new System.Drawing.Size(123, 20);
            this.txtSheet2Staging.TabIndex = 7;
            // 
            // txtSheet3Desc
            // 
            this.txtSheet3Desc.Location = new System.Drawing.Point(74, 81);
            this.txtSheet3Desc.Name = "txtSheet3Desc";
            this.txtSheet3Desc.ReadOnly = true;
            this.txtSheet3Desc.Size = new System.Drawing.Size(123, 20);
            this.txtSheet3Desc.TabIndex = 9;
            // 
            // txtSheet2Desc
            // 
            this.txtSheet2Desc.Location = new System.Drawing.Point(74, 57);
            this.txtSheet2Desc.Name = "txtSheet2Desc";
            this.txtSheet2Desc.ReadOnly = true;
            this.txtSheet2Desc.Size = new System.Drawing.Size(123, 20);
            this.txtSheet2Desc.TabIndex = 6;
            // 
            // cbSheet3
            // 
            this.cbSheet3.AutoSize = true;
            this.cbSheet3.Location = new System.Drawing.Point(6, 83);
            this.cbSheet3.Name = "cbSheet3";
            this.cbSheet3.Size = new System.Drawing.Size(63, 17);
            this.cbSheet3.TabIndex = 8;
            this.cbSheet3.Text = "Sheet 3";
            this.cbSheet3.UseVisualStyleBackColor = true;
            this.cbSheet3.CheckedChanged += new System.EventHandler(this.cbSheet3_CheckedChanged);
            // 
            // cbSheet2
            // 
            this.cbSheet2.AutoSize = true;
            this.cbSheet2.Location = new System.Drawing.Point(6, 59);
            this.cbSheet2.Name = "cbSheet2";
            this.cbSheet2.Size = new System.Drawing.Size(63, 17);
            this.cbSheet2.TabIndex = 5;
            this.cbSheet2.Text = "Sheet 2";
            this.cbSheet2.UseVisualStyleBackColor = true;
            this.cbSheet2.CheckedChanged += new System.EventHandler(this.cbSheet2_CheckedChanged);
            // 
            // txtSheet1Staging
            // 
            this.txtSheet1Staging.Location = new System.Drawing.Point(203, 33);
            this.txtSheet1Staging.Name = "txtSheet1Staging";
            this.txtSheet1Staging.Size = new System.Drawing.Size(123, 20);
            this.txtSheet1Staging.TabIndex = 4;
            // 
            // txtSheet1Desc
            // 
            this.txtSheet1Desc.Location = new System.Drawing.Point(74, 33);
            this.txtSheet1Desc.Name = "txtSheet1Desc";
            this.txtSheet1Desc.Size = new System.Drawing.Size(123, 20);
            this.txtSheet1Desc.TabIndex = 3;
            // 
            // cbSheet1
            // 
            this.cbSheet1.AutoSize = true;
            this.cbSheet1.Checked = true;
            this.cbSheet1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbSheet1.Enabled = false;
            this.cbSheet1.Location = new System.Drawing.Point(6, 35);
            this.cbSheet1.Name = "cbSheet1";
            this.cbSheet1.Size = new System.Drawing.Size(63, 17);
            this.cbSheet1.TabIndex = 8;
            this.cbSheet1.Text = "Sheet 1";
            this.cbSheet1.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(229, 13);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(73, 13);
            this.label9.TabIndex = 11;
            this.label9.Text = "Staging Table";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(106, 13);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(60, 13);
            this.label8.TabIndex = 10;
            this.label8.Text = "Description";
            // 
            // tpProcs
            // 
            this.tpProcs.Controls.Add(this.txtProc3);
            this.tpProcs.Controls.Add(this.label6);
            this.tpProcs.Controls.Add(this.txtProc2);
            this.tpProcs.Controls.Add(this.label5);
            this.tpProcs.Controls.Add(this.label4);
            this.tpProcs.Controls.Add(this.txtProc1);
            this.tpProcs.Location = new System.Drawing.Point(4, 22);
            this.tpProcs.Name = "tpProcs";
            this.tpProcs.Size = new System.Drawing.Size(354, 264);
            this.tpProcs.TabIndex = 2;
            this.tpProcs.Text = "Procedures";
            this.tpProcs.UseVisualStyleBackColor = true;
            // 
            // txtProc3
            // 
            this.txtProc3.Location = new System.Drawing.Point(89, 60);
            this.txtProc3.Name = "txtProc3";
            this.txtProc3.ReadOnly = true;
            this.txtProc3.Size = new System.Drawing.Size(237, 20);
            this.txtProc3.TabIndex = 19;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 63);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(65, 13);
            this.label6.TabIndex = 18;
            this.label6.Text = "Procedure 3";
            // 
            // txtProc2
            // 
            this.txtProc2.Location = new System.Drawing.Point(89, 34);
            this.txtProc2.Name = "txtProc2";
            this.txtProc2.ReadOnly = true;
            this.txtProc2.Size = new System.Drawing.Size(237, 20);
            this.txtProc2.TabIndex = 17;
            this.txtProc2.TextChanged += new System.EventHandler(this.txtProc2_TextChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 37);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(65, 13);
            this.label5.TabIndex = 16;
            this.label5.Text = "Procedure 2";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 11);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(65, 13);
            this.label4.TabIndex = 14;
            this.label4.Text = "Procedure 1";
            // 
            // txtProc1
            // 
            this.txtProc1.Location = new System.Drawing.Point(89, 8);
            this.txtProc1.Name = "txtProc1";
            this.txtProc1.Size = new System.Drawing.Size(237, 20);
            this.txtProc1.TabIndex = 15;
            this.txtProc1.TextChanged += new System.EventHandler(this.txtProc1_TextChanged);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(217, 328);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(92, 23);
            this.btnCancel.TabIndex = 18;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(303, 8);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(59, 21);
            this.btnExport.TabIndex = 19;
            this.btnExport.Text = "Export";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Visible = false;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // frmDesignInterface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(385, 357);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.cmbImportType);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.btnSave);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "frmDesignInterface";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Design Interface";
            this.Load += new System.EventHandler(this.frmDesignInterface_Load);
            this.tabControl1.ResumeLayout(false);
            this.tpInfo.ResumeLayout(false);
            this.tpInfo.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tpProcs.ResumeLayout(false);
            this.tpProcs.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtImportType;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RichTextBox txtDescription;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cmbImportType;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tpInfo;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.TabPage tpProcs;
        private System.Windows.Forms.TextBox txtProc3;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtProc2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtProc1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox txtSheet3Staging;
        private System.Windows.Forms.TextBox txtSheet2Staging;
        private System.Windows.Forms.TextBox txtSheet3Desc;
        private System.Windows.Forms.TextBox txtSheet2Desc;
        private System.Windows.Forms.CheckBox cbSheet3;
        private System.Windows.Forms.CheckBox cbSheet2;
        private System.Windows.Forms.TextBox txtSheet1Staging;
        private System.Windows.Forms.TextBox txtSheet1Desc;
        private System.Windows.Forms.CheckBox cbSheet1;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
    }
}