namespace InCubeIntegration_UI
{
    partial class frmImportTargets
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmImportTargets));
            this.Year = new System.Windows.Forms.Label();
            this.btnLoadTargets = new System.Windows.Forms.Button();
            this.cmbYear = new System.Windows.Forms.ComboBox();
            this.txtExcelPath = new System.Windows.Forms.TextBox();
            this.cmbMonth = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.btnImport = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.lblPercentage = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.dgvResults = new System.Windows.Forms.DataGridView();
            this.btnNext = new System.Windows.Forms.Button();
            this.txtPageNo = new System.Windows.Forms.TextBox();
            this.cbShowErrorsOnly = new System.Windows.Forms.CheckBox();
            this.btnPrevious = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).BeginInit();
            this.SuspendLayout();
            // 
            // Year
            // 
            this.Year.AutoSize = true;
            this.Year.Location = new System.Drawing.Point(12, 9);
            this.Year.Name = "Year";
            this.Year.Size = new System.Drawing.Size(29, 13);
            this.Year.TabIndex = 0;
            this.Year.Text = "Year";
            // 
            // btnLoadTargets
            // 
            this.btnLoadTargets.Location = new System.Drawing.Point(880, 32);
            this.btnLoadTargets.Name = "btnLoadTargets";
            this.btnLoadTargets.Size = new System.Drawing.Size(92, 23);
            this.btnLoadTargets.TabIndex = 1;
            this.btnLoadTargets.Text = "Load Targets";
            this.btnLoadTargets.UseVisualStyleBackColor = true;
            this.btnLoadTargets.Click += new System.EventHandler(this.btnLoadTargets_Click);
            // 
            // cmbYear
            // 
            this.cmbYear.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbYear.FormattingEnabled = true;
            this.cmbYear.Location = new System.Drawing.Point(47, 6);
            this.cmbYear.Name = "cmbYear";
            this.cmbYear.Size = new System.Drawing.Size(73, 21);
            this.cmbYear.TabIndex = 2;
            // 
            // txtExcelPath
            // 
            this.txtExcelPath.Location = new System.Drawing.Point(75, 33);
            this.txtExcelPath.Name = "txtExcelPath";
            this.txtExcelPath.ReadOnly = true;
            this.txtExcelPath.Size = new System.Drawing.Size(799, 20);
            this.txtExcelPath.TabIndex = 3;
            // 
            // cmbMonth
            // 
            this.cmbMonth.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMonth.FormattingEnabled = true;
            this.cmbMonth.Location = new System.Drawing.Point(197, 6);
            this.cmbMonth.Name = "cmbMonth";
            this.cmbMonth.Size = new System.Drawing.Size(121, 21);
            this.cmbMonth.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(154, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Month";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(57, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Excel Path";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 68);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(68, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "File contents";
            // 
            // btnImport
            // 
            this.btnImport.Location = new System.Drawing.Point(706, 510);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(92, 23);
            this.btnImport.TabIndex = 9;
            this.btnImport.Text = "Import to InVan";
            this.btnImport.UseVisualStyleBackColor = true;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(15, 510);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(614, 23);
            this.progressBar1.TabIndex = 10;
            // 
            // lblPercentage
            // 
            this.lblPercentage.AutoSize = true;
            this.lblPercentage.Location = new System.Drawing.Point(635, 516);
            this.lblPercentage.Name = "lblPercentage";
            this.lblPercentage.Size = new System.Drawing.Size(0, 13);
            this.lblPercentage.TabIndex = 12;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.BackColor = System.Drawing.Color.Transparent;
            this.lblStatus.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatus.ForeColor = System.Drawing.Color.White;
            this.lblStatus.Location = new System.Drawing.Point(431, 6);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 19);
            this.lblStatus.TabIndex = 13;
            // 
            // dgvResults
            // 
            this.dgvResults.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvResults.BackgroundColor = System.Drawing.Color.White;
            this.dgvResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvResults.Location = new System.Drawing.Point(15, 88);
            this.dgvResults.Name = "dgvResults";
            this.dgvResults.ReadOnly = true;
            this.dgvResults.Size = new System.Drawing.Size(957, 369);
            this.dgvResults.TabIndex = 14;
            // 
            // btnNext
            // 
            this.btnNext.Enabled = false;
            this.btnNext.Location = new System.Drawing.Point(614, 464);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(31, 23);
            this.btnNext.TabIndex = 15;
            this.btnNext.Text = ">";
            this.btnNext.UseVisualStyleBackColor = true;
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // txtPageNo
            // 
            this.txtPageNo.Enabled = false;
            this.txtPageNo.Location = new System.Drawing.Point(500, 465);
            this.txtPageNo.Name = "txtPageNo";
            this.txtPageNo.ReadOnly = true;
            this.txtPageNo.Size = new System.Drawing.Size(100, 20);
            this.txtPageNo.TabIndex = 16;
            // 
            // cbShowErrorsOnly
            // 
            this.cbShowErrorsOnly.AutoSize = true;
            this.cbShowErrorsOnly.Enabled = false;
            this.cbShowErrorsOnly.Location = new System.Drawing.Point(290, 466);
            this.cbShowErrorsOnly.Name = "cbShowErrorsOnly";
            this.cbShowErrorsOnly.Size = new System.Drawing.Size(107, 17);
            this.cbShowErrorsOnly.TabIndex = 18;
            this.cbShowErrorsOnly.Text = "Show errors only";
            this.cbShowErrorsOnly.UseVisualStyleBackColor = true;
            this.cbShowErrorsOnly.CheckedChanged += new System.EventHandler(this.cbShowErrorsOnly_Click);
            // 
            // btnPrevious
            // 
            this.btnPrevious.Enabled = false;
            this.btnPrevious.Location = new System.Drawing.Point(455, 463);
            this.btnPrevious.Name = "btnPrevious";
            this.btnPrevious.Size = new System.Drawing.Size(31, 23);
            this.btnPrevious.TabIndex = 19;
            this.btnPrevious.Text = "<";
            this.btnPrevious.UseVisualStyleBackColor = true;
            this.btnPrevious.Click += new System.EventHandler(this.btnPrevious_Click);
            // 
            // frmImportTargets
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(194)))), ((int)(((byte)(199)))));
            this.ClientSize = new System.Drawing.Size(987, 491);
            this.Controls.Add(this.btnPrevious);
            this.Controls.Add(this.cbShowErrorsOnly);
            this.Controls.Add(this.txtPageNo);
            this.Controls.Add(this.btnNext);
            this.Controls.Add(this.dgvResults);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblPercentage);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.btnImport);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmbMonth);
            this.Controls.Add(this.txtExcelPath);
            this.Controls.Add(this.cmbYear);
            this.Controls.Add(this.btnLoadTargets);
            this.Controls.Add(this.Year);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "frmImportTargets";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Targets Impoting Tool";
            this.Load += new System.EventHandler(this.frmImportTarget_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Year;
        private System.Windows.Forms.Button btnLoadTargets;
        private System.Windows.Forms.ComboBox cmbYear;
        private System.Windows.Forms.TextBox txtExcelPath;
        private System.Windows.Forms.ComboBox cmbMonth;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label lblPercentage;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.DataGridView dgvResults;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.TextBox txtPageNo;
        private System.Windows.Forms.CheckBox cbShowErrorsOnly;
        private System.Windows.Forms.Button btnPrevious;
    }
}

