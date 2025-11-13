namespace InCubeIntegration_UI
{
    partial class frmTransactionsManagement
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmTransactionsManagement));
            this.cmbCustName = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnFind = new System.Windows.Forms.Button();
            this.dtpFromDate = new System.Windows.Forms.DateTimePicker();
            this.gbFilters = new System.Windows.Forms.GroupBox();
            this.cmbTransType = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.dtpToDate = new System.Windows.Forms.DateTimePicker();
            this.label3 = new System.Windows.Forms.Label();
            this.cmbCustCode = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cbAllTransactions = new System.Windows.Forms.CheckBox();
            this.lsvTransactions = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.gbTransactions = new System.Windows.Forms.GroupBox();
            this.lblSelected = new System.Windows.Forms.Label();
            this.gbActions = new System.Windows.Forms.GroupBox();
            this.customProgressBar1 = new InCubeIntegration_UI.CustomProgressBar();
            this.cmbCustToCode = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.cmbCustToName = new System.Windows.Forms.ComboBox();
            this.btnApply = new System.Windows.Forms.Button();
            this.rbTransfer = new System.Windows.Forms.RadioButton();
            this.rbVoid = new System.Windows.Forms.RadioButton();
            this.gbFilters.SuspendLayout();
            this.gbTransactions.SuspendLayout();
            this.gbActions.SuspendLayout();
            this.SuspendLayout();
            // 
            // cmbCustName
            // 
            this.cmbCustName.FormattingEnabled = true;
            this.cmbCustName.Location = new System.Drawing.Point(47, 25);
            this.cmbCustName.Name = "cmbCustName";
            this.cmbCustName.Size = new System.Drawing.Size(308, 21);
            this.cmbCustName.TabIndex = 0;
            this.cmbCustName.SelectedIndexChanged += new System.EventHandler(this.cmbCustName_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Name";
            // 
            // btnFind
            // 
            this.btnFind.Location = new System.Drawing.Point(468, 76);
            this.btnFind.Name = "btnFind";
            this.btnFind.Size = new System.Drawing.Size(75, 23);
            this.btnFind.TabIndex = 3;
            this.btnFind.Text = "Find";
            this.btnFind.UseVisualStyleBackColor = true;
            this.btnFind.Click += new System.EventHandler(this.btnFind_Click);
            // 
            // dtpFromDate
            // 
            this.dtpFromDate.Location = new System.Drawing.Point(47, 52);
            this.dtpFromDate.Name = "dtpFromDate";
            this.dtpFromDate.Size = new System.Drawing.Size(200, 20);
            this.dtpFromDate.TabIndex = 4;
            // 
            // gbFilters
            // 
            this.gbFilters.Controls.Add(this.btnFind);
            this.gbFilters.Controls.Add(this.cmbTransType);
            this.gbFilters.Controls.Add(this.label5);
            this.gbFilters.Controls.Add(this.label4);
            this.gbFilters.Controls.Add(this.dtpToDate);
            this.gbFilters.Controls.Add(this.label3);
            this.gbFilters.Controls.Add(this.cmbCustCode);
            this.gbFilters.Controls.Add(this.label2);
            this.gbFilters.Controls.Add(this.dtpFromDate);
            this.gbFilters.Controls.Add(this.label1);
            this.gbFilters.Controls.Add(this.cmbCustName);
            this.gbFilters.Location = new System.Drawing.Point(12, 12);
            this.gbFilters.Name = "gbFilters";
            this.gbFilters.Size = new System.Drawing.Size(549, 113);
            this.gbFilters.TabIndex = 5;
            this.gbFilters.TabStop = false;
            this.gbFilters.Text = "Filters";
            // 
            // cmbTransType
            // 
            this.cmbTransType.FormattingEnabled = true;
            this.cmbTransType.Location = new System.Drawing.Point(47, 78);
            this.cmbTransType.Name = "cmbTransType";
            this.cmbTransType.Size = new System.Drawing.Size(200, 21);
            this.cmbTransType.TabIndex = 10;
            this.cmbTransType.SelectedIndexChanged += new System.EventHandler(this.cmbTransType_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 81);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(31, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Type";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(302, 57);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(19, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "To";
            // 
            // dtpToDate
            // 
            this.dtpToDate.Location = new System.Drawing.Point(343, 52);
            this.dtpToDate.Name = "dtpToDate";
            this.dtpToDate.Size = new System.Drawing.Size(200, 20);
            this.dtpToDate.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 57);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(31, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "From";
            // 
            // cmbCustCode
            // 
            this.cmbCustCode.FormattingEnabled = true;
            this.cmbCustCode.Location = new System.Drawing.Point(424, 25);
            this.cmbCustCode.Name = "cmbCustCode";
            this.cmbCustCode.Size = new System.Drawing.Size(119, 21);
            this.cmbCustCode.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(376, 28);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Code";
            // 
            // cbAllTransactions
            // 
            this.cbAllTransactions.AutoSize = true;
            this.cbAllTransactions.Location = new System.Drawing.Point(10, 20);
            this.cbAllTransactions.Name = "cbAllTransactions";
            this.cbAllTransactions.Size = new System.Drawing.Size(37, 17);
            this.cbAllTransactions.TabIndex = 13;
            this.cbAllTransactions.Text = "All";
            this.cbAllTransactions.UseVisualStyleBackColor = true;
            this.cbAllTransactions.CheckedChanged += new System.EventHandler(this.cbAllTransactions_CheckedChanged);
            // 
            // lsvTransactions
            // 
            this.lsvTransactions.CheckBoxes = true;
            this.lsvTransactions.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader4,
            this.columnHeader3});
            this.lsvTransactions.FullRowSelect = true;
            this.lsvTransactions.HideSelection = false;
            this.lsvTransactions.Location = new System.Drawing.Point(9, 40);
            this.lsvTransactions.MultiSelect = false;
            this.lsvTransactions.Name = "lsvTransactions";
            this.lsvTransactions.Size = new System.Drawing.Size(534, 276);
            this.lsvTransactions.TabIndex = 12;
            this.lsvTransactions.UseCompatibleStateImageBehavior = false;
            this.lsvTransactions.View = System.Windows.Forms.View.Details;
            this.lsvTransactions.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.lsvTransactions_ItemChecked);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Transaction ID";
            this.columnHeader1.Width = 130;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Transaction Date";
            this.columnHeader2.Width = 130;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Transaction Type";
            this.columnHeader4.Width = 130;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Amount";
            this.columnHeader3.Width = 100;
            // 
            // gbTransactions
            // 
            this.gbTransactions.Controls.Add(this.lblSelected);
            this.gbTransactions.Controls.Add(this.lsvTransactions);
            this.gbTransactions.Controls.Add(this.cbAllTransactions);
            this.gbTransactions.Location = new System.Drawing.Point(12, 131);
            this.gbTransactions.Name = "gbTransactions";
            this.gbTransactions.Size = new System.Drawing.Size(549, 322);
            this.gbTransactions.TabIndex = 14;
            this.gbTransactions.TabStop = false;
            this.gbTransactions.Text = "Transacions";
            // 
            // lblSelected
            // 
            this.lblSelected.Location = new System.Drawing.Point(343, 16);
            this.lblSelected.Name = "lblSelected";
            this.lblSelected.Size = new System.Drawing.Size(200, 21);
            this.lblSelected.TabIndex = 14;
            this.lblSelected.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // gbActions
            // 
            this.gbActions.Controls.Add(this.customProgressBar1);
            this.gbActions.Controls.Add(this.cmbCustToCode);
            this.gbActions.Controls.Add(this.label6);
            this.gbActions.Controls.Add(this.cmbCustToName);
            this.gbActions.Controls.Add(this.btnApply);
            this.gbActions.Controls.Add(this.rbTransfer);
            this.gbActions.Controls.Add(this.rbVoid);
            this.gbActions.Enabled = false;
            this.gbActions.Location = new System.Drawing.Point(12, 459);
            this.gbActions.Name = "gbActions";
            this.gbActions.Size = new System.Drawing.Size(549, 100);
            this.gbActions.TabIndex = 15;
            this.gbActions.TabStop = false;
            this.gbActions.Text = "Actions";
            // 
            // customProgressBar1
            // 
            this.customProgressBar1.CustomText = null;
            this.customProgressBar1.DisplayStyle = InCubeIntegration_UI.ProgressBarDisplayText.Mixed;
            this.customProgressBar1.Location = new System.Drawing.Point(10, 70);
            this.customProgressBar1.Name = "customProgressBar1";
            this.customProgressBar1.Size = new System.Drawing.Size(452, 23);
            this.customProgressBar1.TabIndex = 10;
            // 
            // cmbCustToCode
            // 
            this.cmbCustToCode.Enabled = false;
            this.cmbCustToCode.FormattingEnabled = true;
            this.cmbCustToCode.Location = new System.Drawing.Point(445, 40);
            this.cmbCustToCode.Name = "cmbCustToCode";
            this.cmbCustToCode.Size = new System.Drawing.Size(98, 21);
            this.cmbCustToCode.TabIndex = 9;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(405, 44);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(32, 13);
            this.label6.TabIndex = 8;
            this.label6.Text = "Code";
            // 
            // cmbCustToName
            // 
            this.cmbCustToName.Enabled = false;
            this.cmbCustToName.FormattingEnabled = true;
            this.cmbCustToName.Location = new System.Drawing.Point(112, 40);
            this.cmbCustToName.Name = "cmbCustToName";
            this.cmbCustToName.Size = new System.Drawing.Size(286, 21);
            this.cmbCustToName.TabIndex = 6;
            this.cmbCustToName.EnabledChanged += new System.EventHandler(this.cmbCustToName_EnabledChanged);
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(468, 70);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(75, 23);
            this.btnApply.TabIndex = 4;
            this.btnApply.Text = "Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // rbTransfer
            // 
            this.rbTransfer.AutoSize = true;
            this.rbTransfer.Location = new System.Drawing.Point(10, 42);
            this.rbTransfer.Name = "rbTransfer";
            this.rbTransfer.Size = new System.Drawing.Size(79, 17);
            this.rbTransfer.TabIndex = 1;
            this.rbTransfer.Text = "Transfer to";
            this.rbTransfer.UseVisualStyleBackColor = true;
            this.rbTransfer.CheckedChanged += new System.EventHandler(this.rbTransfer_CheckedChanged);
            // 
            // rbVoid
            // 
            this.rbVoid.AutoSize = true;
            this.rbVoid.Checked = true;
            this.rbVoid.Location = new System.Drawing.Point(10, 19);
            this.rbVoid.Name = "rbVoid";
            this.rbVoid.Size = new System.Drawing.Size(87, 17);
            this.rbVoid.TabIndex = 0;
            this.rbVoid.TabStop = true;
            this.rbVoid.Text = "Void (Delete)";
            this.rbVoid.UseVisualStyleBackColor = true;
            // 
            // frmTransactionsManagement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(194)))), ((int)(((byte)(217)))), ((int)(((byte)(247)))));
            this.ClientSize = new System.Drawing.Size(571, 568);
            this.Controls.Add(this.gbActions);
            this.Controls.Add(this.gbTransactions);
            this.Controls.Add(this.gbFilters);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "frmTransactionsManagement";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Transactions Management";
            this.Load += new System.EventHandler(this.frmTransactionsManagement_Load);
            this.gbFilters.ResumeLayout(false);
            this.gbFilters.PerformLayout();
            this.gbTransactions.ResumeLayout(false);
            this.gbTransactions.PerformLayout();
            this.gbActions.ResumeLayout(false);
            this.gbActions.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbCustName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnFind;
        private System.Windows.Forms.DateTimePicker dtpFromDate;
        private System.Windows.Forms.GroupBox gbFilters;
        private System.Windows.Forms.CheckBox cbAllTransactions;
        private System.Windows.Forms.ListView lsvTransactions;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ComboBox cmbTransType;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.DateTimePicker dtpToDate;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cmbCustCode;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.GroupBox gbTransactions;
        private System.Windows.Forms.GroupBox gbActions;
        private System.Windows.Forms.ComboBox cmbCustToCode;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cmbCustToName;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.RadioButton rbTransfer;
        private System.Windows.Forms.RadioButton rbVoid;
        private CustomProgressBar customProgressBar1;
        private System.Windows.Forms.Label lblSelected;
    }
}