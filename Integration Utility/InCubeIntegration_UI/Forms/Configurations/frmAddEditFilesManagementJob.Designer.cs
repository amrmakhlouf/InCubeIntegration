namespace InCubeIntegration_UI
{
    partial class frmAddEditFilesManagementJob
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
            this.txtDestFolder = new System.Windows.Forms.RichTextBox();
            this.txtSourceFolder = new System.Windows.Forms.RichTextBox();
            this.cmbJobType = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.txtFileExtension = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtID = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.numAge = new System.Windows.Forms.NumericUpDown();
            this.cmbAgeUnit = new System.Windows.Forms.ComboBox();
            this.cbKeepDirectoryStructure = new System.Windows.Forms.CheckBox();
            this.btnComparisonOperator = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numAge)).BeginInit();
            this.SuspendLayout();
            // 
            // txtDestFolder
            // 
            this.txtDestFolder.Location = new System.Drawing.Point(104, 198);
            this.txtDestFolder.Name = "txtDestFolder";
            this.txtDestFolder.Size = new System.Drawing.Size(238, 54);
            this.txtDestFolder.TabIndex = 7;
            this.txtDestFolder.Text = "";
            // 
            // txtSourceFolder
            // 
            this.txtSourceFolder.Location = new System.Drawing.Point(104, 85);
            this.txtSourceFolder.Name = "txtSourceFolder";
            this.txtSourceFolder.Size = new System.Drawing.Size(238, 54);
            this.txtSourceFolder.TabIndex = 3;
            this.txtSourceFolder.Text = "";
            // 
            // cmbJobType
            // 
            this.cmbJobType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbJobType.FormattingEnabled = true;
            this.cmbJobType.Location = new System.Drawing.Point(104, 57);
            this.cmbJobType.Name = "cmbJobType";
            this.cmbJobType.Size = new System.Drawing.Size(134, 21);
            this.cmbJobType.TabIndex = 2;
            this.cmbJobType.SelectedIndexChanged += new System.EventHandler(this.cmbJobType_SelectedIndexChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(8, 201);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(92, 13);
            this.label8.TabIndex = 62;
            this.label8.Text = "Destination Folder";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(8, 88);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(73, 13);
            this.label7.TabIndex = 61;
            this.label7.Text = "Source Folder";
            // 
            // txtFileExtension
            // 
            this.txtFileExtension.Location = new System.Drawing.Point(104, 144);
            this.txtFileExtension.Name = "txtFileExtension";
            this.txtFileExtension.Size = new System.Drawing.Size(65, 20);
            this.txtFileExtension.TabIndex = 4;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(8, 147);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(72, 13);
            this.label6.TabIndex = 60;
            this.label6.Text = "File Extension";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 60);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(31, 13);
            this.label5.TabIndex = 59;
            this.label5.Text = "Type";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(104, 31);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(238, 20);
            this.txtName.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 34);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 58;
            this.label2.Text = "Name";
            // 
            // txtID
            // 
            this.txtID.Location = new System.Drawing.Point(104, 5);
            this.txtID.Name = "txtID";
            this.txtID.ReadOnly = true;
            this.txtID.Size = new System.Drawing.Size(65, 20);
            this.txtID.TabIndex = 57;
            this.txtID.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(18, 13);
            this.label1.TabIndex = 56;
            this.label1.Text = "ID";
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(267, 258);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 8;
            this.btnSave.Text = "Save";
            this.btnSave.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 173);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(45, 13);
            this.label3.TabIndex = 63;
            this.label3.Text = "File Age";
            // 
            // numAge
            // 
            this.numAge.Location = new System.Drawing.Point(140, 171);
            this.numAge.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numAge.Name = "numAge";
            this.numAge.Size = new System.Drawing.Size(65, 20);
            this.numAge.TabIndex = 5;
            // 
            // cmbAgeUnit
            // 
            this.cmbAgeUnit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAgeUnit.FormattingEnabled = true;
            this.cmbAgeUnit.Location = new System.Drawing.Point(215, 170);
            this.cmbAgeUnit.Name = "cmbAgeUnit";
            this.cmbAgeUnit.Size = new System.Drawing.Size(127, 21);
            this.cmbAgeUnit.TabIndex = 6;
            // 
            // cbKeepDirectoryStructure
            // 
            this.cbKeepDirectoryStructure.AutoSize = true;
            this.cbKeepDirectoryStructure.Enabled = false;
            this.cbKeepDirectoryStructure.Location = new System.Drawing.Point(244, 59);
            this.cbKeepDirectoryStructure.Name = "cbKeepDirectoryStructure";
            this.cbKeepDirectoryStructure.Size = new System.Drawing.Size(98, 17);
            this.cbKeepDirectoryStructure.TabIndex = 64;
            this.cbKeepDirectoryStructure.Text = "Keep Dir Struct";
            this.cbKeepDirectoryStructure.UseVisualStyleBackColor = true;
            // 
            // btnComparisonOperator
            // 
            this.btnComparisonOperator.Location = new System.Drawing.Point(104, 169);
            this.btnComparisonOperator.Name = "btnComparisonOperator";
            this.btnComparisonOperator.Size = new System.Drawing.Size(30, 23);
            this.btnComparisonOperator.TabIndex = 65;
            this.btnComparisonOperator.Text = ">";
            this.btnComparisonOperator.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnComparisonOperator.UseVisualStyleBackColor = true;
            this.btnComparisonOperator.Click += new System.EventHandler(this.btnCompareFactor_Click);
            // 
            // frmAddEditFilesManagementJob
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(348, 284);
            this.Controls.Add(this.btnComparisonOperator);
            this.Controls.Add(this.cbKeepDirectoryStructure);
            this.Controls.Add(this.cmbAgeUnit);
            this.Controls.Add(this.numAge);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtDestFolder);
            this.Controls.Add(this.txtSourceFolder);
            this.Controls.Add(this.cmbJobType);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.txtFileExtension);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtID);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnSave);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "frmAddEditFilesManagementJob";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add/Edit Files Management Job";
            this.Load += new System.EventHandler(this.frmAddEditFilesManagementJob_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numAge)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox txtDestFolder;
        private System.Windows.Forms.RichTextBox txtSourceFolder;
        private System.Windows.Forms.ComboBox cmbJobType;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtFileExtension;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtID;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numAge;
        private System.Windows.Forms.ComboBox cmbAgeUnit;
        private System.Windows.Forms.CheckBox cbKeepDirectoryStructure;
        private System.Windows.Forms.Button btnComparisonOperator;
    }
}