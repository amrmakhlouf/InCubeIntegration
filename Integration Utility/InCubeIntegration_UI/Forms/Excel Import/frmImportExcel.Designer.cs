namespace InCubeIntegration_UI
{
    partial class frmImportExcel
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
            this.btnBrowseFile = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbDataType = new System.Windows.Forms.ComboBox();
            this.txtExcelPath = new System.Windows.Forms.RichTextBox();
            this.lbl = new System.Windows.Forms.Label();
            this.btnImport = new System.Windows.Forms.Button();
            this.txtDescription = new System.Windows.Forms.RichTextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txtSkipped = new System.Windows.Forms.TextBox();
            this.txtUpdated = new System.Windows.Forms.TextBox();
            this.txtInserted = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtTotalRows = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.customProgressBar1 = new InCubeIntegration_UI.CustomProgressBar();
            this.btnExportResults = new System.Windows.Forms.Button();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnBrowseFile
            // 
            this.btnBrowseFile.Enabled = false;
            this.btnBrowseFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBrowseFile.Location = new System.Drawing.Point(11, 114);
            this.btnBrowseFile.Name = "btnBrowseFile";
            this.btnBrowseFile.Size = new System.Drawing.Size(99, 25);
            this.btnBrowseFile.TabIndex = 0;
            this.btnBrowseFile.Text = "Browse File";
            this.btnBrowseFile.UseVisualStyleBackColor = true;
            this.btnBrowseFile.Click += new System.EventHandler(this.btnBrowseFile_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Import Data For";
            // 
            // cmbDataType
            // 
            this.cmbDataType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDataType.FormattingEnabled = true;
            this.cmbDataType.Location = new System.Drawing.Point(118, 7);
            this.cmbDataType.Name = "cmbDataType";
            this.cmbDataType.Size = new System.Drawing.Size(175, 21);
            this.cmbDataType.TabIndex = 3;
            this.cmbDataType.SelectedIndexChanged += new System.EventHandler(this.cmbDataType_SelectedIndexChanged);
            // 
            // txtExcelPath
            // 
            this.txtExcelPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtExcelPath.Enabled = false;
            this.txtExcelPath.Location = new System.Drawing.Point(117, 114);
            this.txtExcelPath.Name = "txtExcelPath";
            this.txtExcelPath.ReadOnly = true;
            this.txtExcelPath.Size = new System.Drawing.Size(585, 26);
            this.txtExcelPath.TabIndex = 4;
            this.txtExcelPath.TabStop = false;
            this.txtExcelPath.Text = "";
            // 
            // lbl
            // 
            this.lbl.AutoSize = true;
            this.lbl.Location = new System.Drawing.Point(9, 149);
            this.lbl.Name = "lbl";
            this.lbl.Size = new System.Drawing.Size(123, 13);
            this.lbl.TabIndex = 5;
            this.lbl.Text = "Top 1000 rows in the file";
            // 
            // btnImport
            // 
            this.btnImport.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnImport.Enabled = false;
            this.btnImport.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnImport.Location = new System.Drawing.Point(590, 462);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(111, 34);
            this.btnImport.TabIndex = 6;
            this.btnImport.Text = "Import";
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
            // 
            // txtDescription
            // 
            this.txtDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDescription.Location = new System.Drawing.Point(118, 36);
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.ReadOnly = true;
            this.txtDescription.Size = new System.Drawing.Size(584, 72);
            this.txtDescription.TabIndex = 8;
            this.txtDescription.TabStop = false;
            this.txtDescription.Text = "";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 45);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(60, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Description";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Location = new System.Drawing.Point(12, 500);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox2.Size = new System.Drawing.Size(689, 94);
            this.groupBox2.TabIndex = 11;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Results";
            // 
            // txtSkipped
            // 
            this.txtSkipped.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSkipped.Location = new System.Drawing.Point(512, 37);
            this.txtSkipped.Margin = new System.Windows.Forms.Padding(2);
            this.txtSkipped.Name = "txtSkipped";
            this.txtSkipped.ReadOnly = true;
            this.txtSkipped.Size = new System.Drawing.Size(166, 20);
            this.txtSkipped.TabIndex = 7;
            // 
            // txtUpdated
            // 
            this.txtUpdated.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtUpdated.Location = new System.Drawing.Point(342, 37);
            this.txtUpdated.Margin = new System.Windows.Forms.Padding(2);
            this.txtUpdated.Name = "txtUpdated";
            this.txtUpdated.ReadOnly = true;
            this.txtUpdated.Size = new System.Drawing.Size(166, 20);
            this.txtUpdated.TabIndex = 6;
            // 
            // txtInserted
            // 
            this.txtInserted.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtInserted.Location = new System.Drawing.Point(172, 37);
            this.txtInserted.Margin = new System.Windows.Forms.Padding(2);
            this.txtInserted.Name = "txtInserted";
            this.txtInserted.ReadOnly = true;
            this.txtInserted.Size = new System.Drawing.Size(166, 20);
            this.txtInserted.TabIndex = 5;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label7.Location = new System.Drawing.Point(512, 0);
            this.label7.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(166, 35);
            this.label7.TabIndex = 4;
            this.label7.Text = "Skipped";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtTotalRows
            // 
            this.txtTotalRows.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtTotalRows.Location = new System.Drawing.Point(2, 37);
            this.txtTotalRows.Margin = new System.Windows.Forms.Padding(2);
            this.txtTotalRows.Name = "txtTotalRows";
            this.txtTotalRows.ReadOnly = true;
            this.txtTotalRows.Size = new System.Drawing.Size(166, 20);
            this.txtTotalRows.TabIndex = 2;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Location = new System.Drawing.Point(342, 0);
            this.label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(166, 35);
            this.label6.TabIndex = 3;
            this.label6.Text = "Updated";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(172, 0);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(166, 35);
            this.label5.TabIndex = 2;
            this.label5.Text = "Inserted";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(2, 0);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(166, 35);
            this.label4.TabIndex = 1;
            this.label4.Text = "Total Rows";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 4;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.Controls.Add(this.txtSkipped, 3, 1);
            this.tableLayoutPanel2.Controls.Add(this.txtUpdated, 2, 1);
            this.tableLayoutPanel2.Controls.Add(this.txtInserted, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.label7, 3, 0);
            this.tableLayoutPanel2.Controls.Add(this.txtTotalRows, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.label6, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.label5, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.label4, 0, 0);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(17, 516);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(680, 70);
            this.tableLayoutPanel2.TabIndex = 12;
            // 
            // tabControl1
            // 
            this.tabControl1.Location = new System.Drawing.Point(11, 165);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(691, 291);
            this.tabControl1.TabIndex = 14;
            // 
            // customProgressBar1
            // 
            this.customProgressBar1.CustomText = null;
            this.customProgressBar1.DisplayStyle = InCubeIntegration_UI.ProgressBarDisplayText.Mixed;
            this.customProgressBar1.Location = new System.Drawing.Point(11, 462);
            this.customProgressBar1.Name = "customProgressBar1";
            this.customProgressBar1.Size = new System.Drawing.Size(572, 33);
            this.customProgressBar1.TabIndex = 13;
            // 
            // btnExportResults
            // 
            this.btnExportResults.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExportResults.Enabled = false;
            this.btnExportResults.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExportResults.Location = new System.Drawing.Point(602, 144);
            this.btnExportResults.Name = "btnExportResults";
            this.btnExportResults.Size = new System.Drawing.Size(100, 24);
            this.btnExportResults.TabIndex = 15;
            this.btnExportResults.Text = "Export Results";
            this.btnExportResults.UseVisualStyleBackColor = true;
            this.btnExportResults.Click += new System.EventHandler(this.btnExportResults_Click);
            // 
            // frmImportExcel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(194)))), ((int)(((byte)(217)))), ((int)(((byte)(247)))));
            this.ClientSize = new System.Drawing.Size(722, 605);
            this.Controls.Add(this.btnExportResults);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.customProgressBar1);
            this.Controls.Add(this.tableLayoutPanel2);
            this.Controls.Add(this.btnImport);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtDescription);
            this.Controls.Add(this.lbl);
            this.Controls.Add(this.txtExcelPath);
            this.Controls.Add(this.cmbDataType);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnBrowseFile);
            this.Controls.Add(this.groupBox2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "frmImportExcel";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Excel Import";
            this.Load += new System.EventHandler(this.frmImportExcel_Load);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnBrowseFile;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbDataType;
        private System.Windows.Forms.RichTextBox txtExcelPath;
        private System.Windows.Forms.Label lbl;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.RichTextBox txtDescription;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox txtTotalRows;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtSkipped;
        private System.Windows.Forms.TextBox txtUpdated;
        private System.Windows.Forms.TextBox txtInserted;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private CustomProgressBar customProgressBar1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.Button btnExportResults;
    }
}

