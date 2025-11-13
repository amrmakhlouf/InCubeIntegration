namespace InCubeIntegration_UI
{
    partial class frmOperationsMonitoring
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
            this.dtpFromDate = new System.Windows.Forms.DateTimePicker();
            this.dtpToDate = new System.Windows.Forms.DateTimePicker();
            this.lsvResults = new System.Windows.Forms.ListView();
            this.btnFind = new System.Windows.Forms.Button();
            this.cmbField = new System.Windows.Forms.ComboBox();
            this.cbAllFields = new System.Windows.Forms.CheckBox();
            this.btnBack = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnExport = new System.Windows.Forms.Button();
            this.lblLines = new System.Windows.Forms.Label();
            this.lblDuration = new System.Windows.Forms.Label();
            this.lblView = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dtpFromDate
            // 
            this.dtpFromDate.CustomFormat = "dd/MMM/yyyy  HH:mm";
            this.dtpFromDate.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpFromDate.Location = new System.Drawing.Point(52, 19);
            this.dtpFromDate.Name = "dtpFromDate";
            this.dtpFromDate.Size = new System.Drawing.Size(158, 20);
            this.dtpFromDate.TabIndex = 0;
            // 
            // dtpToDate
            // 
            this.dtpToDate.CustomFormat = "dd/MMM/yyyy  HH:mm";
            this.dtpToDate.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpToDate.Location = new System.Drawing.Point(534, 19);
            this.dtpToDate.Name = "dtpToDate";
            this.dtpToDate.Size = new System.Drawing.Size(158, 20);
            this.dtpToDate.TabIndex = 1;
            // 
            // lsvResults
            // 
            this.lsvResults.FullRowSelect = true;
            this.lsvResults.Location = new System.Drawing.Point(12, 101);
            this.lsvResults.Name = "lsvResults";
            this.lsvResults.Size = new System.Drawing.Size(706, 282);
            this.lsvResults.TabIndex = 2;
            this.lsvResults.UseCompatibleStateImageBehavior = false;
            this.lsvResults.View = System.Windows.Forms.View.Details;
            this.lsvResults.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lsvResults_MouseDoubleClick);
            // 
            // btnFind
            // 
            this.btnFind.Location = new System.Drawing.Point(617, 51);
            this.btnFind.Name = "btnFind";
            this.btnFind.Size = new System.Drawing.Size(75, 23);
            this.btnFind.TabIndex = 3;
            this.btnFind.Text = "Find";
            this.btnFind.UseVisualStyleBackColor = true;
            this.btnFind.Click += new System.EventHandler(this.btnFind_Click);
            // 
            // cmbField
            // 
            this.cmbField.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbField.Enabled = false;
            this.cmbField.FormattingEnabled = true;
            this.cmbField.Location = new System.Drawing.Point(52, 53);
            this.cmbField.Name = "cmbField";
            this.cmbField.Size = new System.Drawing.Size(157, 21);
            this.cmbField.TabIndex = 4;
            // 
            // cbAllFields
            // 
            this.cbAllFields.AutoSize = true;
            this.cbAllFields.Checked = true;
            this.cbAllFields.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbAllFields.Location = new System.Drawing.Point(223, 55);
            this.cbAllFields.Name = "cbAllFields";
            this.cbAllFields.Size = new System.Drawing.Size(37, 17);
            this.cbAllFields.TabIndex = 5;
            this.cbAllFields.Text = "All";
            this.cbAllFields.UseVisualStyleBackColor = true;
            this.cbAllFields.CheckedChanged += new System.EventHandler(this.cbAllFields_CheckedChanged);
            // 
            // btnBack
            // 
            this.btnBack.Location = new System.Drawing.Point(563, 389);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(75, 23);
            this.btnBack.TabIndex = 6;
            this.btnBack.Text = "Back";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Visible = false;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 56);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Field";
            this.label1.Visible = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(30, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "From";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(497, 22);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(20, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "To";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.dtpFromDate);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.dtpToDate);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.btnFind);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.cmbField);
            this.groupBox1.Controls.Add(this.cbAllFields);
            this.groupBox1.Location = new System.Drawing.Point(12, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(706, 92);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(643, 389);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(75, 23);
            this.btnExport.TabIndex = 11;
            this.btnExport.Text = "Export";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // lblLines
            // 
            this.lblLines.AutoSize = true;
            this.lblLines.Location = new System.Drawing.Point(332, 394);
            this.lblLines.Name = "lblLines";
            this.lblLines.Size = new System.Drawing.Size(38, 13);
            this.lblLines.TabIndex = 12;
            this.lblLines.Text = "Lines: ";
            // 
            // lblDuration
            // 
            this.lblDuration.AutoSize = true;
            this.lblDuration.Location = new System.Drawing.Point(418, 394);
            this.lblDuration.Name = "lblDuration";
            this.lblDuration.Size = new System.Drawing.Size(80, 13);
            this.lblDuration.TabIndex = 13;
            this.lblDuration.Text = "Total Duration: ";
            // 
            // lblView
            // 
            this.lblView.AutoSize = true;
            this.lblView.Location = new System.Drawing.Point(18, 394);
            this.lblView.Name = "lblView";
            this.lblView.Size = new System.Drawing.Size(79, 13);
            this.lblView.TabIndex = 14;
            this.lblView.Text = "View: Summary";
            // 
            // frmOperationsMonitoring
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(724, 419);
            this.Controls.Add(this.lblView);
            this.Controls.Add(this.lblDuration);
            this.Controls.Add(this.lblLines);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.lsvResults);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "frmOperationsMonitoring";
            this.ShowIcon = false;
            this.Text = "Operations Monitoring";
            this.Load += new System.EventHandler(this.frmOperationsMonitoring_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DateTimePicker dtpFromDate;
        private System.Windows.Forms.DateTimePicker dtpToDate;
        private System.Windows.Forms.ListView lsvResults;
        private System.Windows.Forms.Button btnFind;
        private System.Windows.Forms.ComboBox cmbField;
        private System.Windows.Forms.CheckBox cbAllFields;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Label lblLines;
        private System.Windows.Forms.Label lblDuration;
        private System.Windows.Forms.Label lblView;
    }
}