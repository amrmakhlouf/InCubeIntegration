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
            this.grdTableColumns = new System.Windows.Forms.DataGridView();
            this.colField = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colType = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.label2 = new System.Windows.Forms.Label();
            this.txtDescription = new System.Windows.Forms.RichTextBox();
            this.txtStagingTable = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtProc1 = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtProc2 = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtProc3 = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.cmbImportType = new System.Windows.Forms.ComboBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tpInfo = new System.Windows.Forms.TabPage();
            this.tpColumns = new System.Windows.Forms.TabPage();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.grdTableColumns)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tpInfo.SuspendLayout();
            this.tpColumns.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(80, 317);
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
            this.label1.Location = new System.Drawing.Point(16, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Import Type";
            // 
            // txtImportType
            // 
            this.txtImportType.Location = new System.Drawing.Point(88, 15);
            this.txtImportType.Name = "txtImportType";
            this.txtImportType.Size = new System.Drawing.Size(237, 20);
            this.txtImportType.TabIndex = 2;
            // 
            // grdTableColumns
            // 
            this.grdTableColumns.BackgroundColor = System.Drawing.Color.White;
            this.grdTableColumns.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdTableColumns.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colField,
            this.colType});
            this.grdTableColumns.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdTableColumns.Location = new System.Drawing.Point(3, 3);
            this.grdTableColumns.Name = "grdTableColumns";
            this.grdTableColumns.Size = new System.Drawing.Size(340, 244);
            this.grdTableColumns.TabIndex = 3;
            // 
            // colField
            // 
            this.colField.HeaderText = "Column Title";
            this.colField.Name = "colField";
            this.colField.Width = 150;
            // 
            // colType
            // 
            this.colType.HeaderText = "Column Type";
            this.colType.Name = "colType";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Decription";
            // 
            // txtDescription
            // 
            this.txtDescription.Location = new System.Drawing.Point(88, 46);
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.Size = new System.Drawing.Size(237, 96);
            this.txtDescription.TabIndex = 5;
            this.txtDescription.Text = "";
            // 
            // txtStagingTable
            // 
            this.txtStagingTable.Location = new System.Drawing.Point(88, 148);
            this.txtStagingTable.Name = "txtStagingTable";
            this.txtStagingTable.Size = new System.Drawing.Size(237, 20);
            this.txtStagingTable.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(16, 151);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(72, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Staging Table";
            // 
            // txtProc1
            // 
            this.txtProc1.Location = new System.Drawing.Point(88, 174);
            this.txtProc1.Name = "txtProc1";
            this.txtProc1.Size = new System.Drawing.Size(237, 20);
            this.txtProc1.TabIndex = 9;
            this.txtProc1.TextChanged += new System.EventHandler(this.txtProc1_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(16, 177);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(65, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Procedure 1";
            // 
            // txtProc2
            // 
            this.txtProc2.Location = new System.Drawing.Point(88, 200);
            this.txtProc2.Name = "txtProc2";
            this.txtProc2.ReadOnly = true;
            this.txtProc2.Size = new System.Drawing.Size(237, 20);
            this.txtProc2.TabIndex = 11;
            this.txtProc2.TextChanged += new System.EventHandler(this.txtProc2_TextChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(16, 203);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(65, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Procedure 2";
            // 
            // txtProc3
            // 
            this.txtProc3.Location = new System.Drawing.Point(88, 226);
            this.txtProc3.Name = "txtProc3";
            this.txtProc3.ReadOnly = true;
            this.txtProc3.Size = new System.Drawing.Size(237, 20);
            this.txtProc3.TabIndex = 13;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(16, 229);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(65, 13);
            this.label6.TabIndex = 12;
            this.label6.Text = "Procedure 3";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(13, 11);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(66, 13);
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
            this.tabControl1.Controls.Add(this.tpColumns);
            this.tabControl1.Location = new System.Drawing.Point(12, 35);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(354, 276);
            this.tabControl1.TabIndex = 16;
            // 
            // tpInfo
            // 
            this.tpInfo.Controls.Add(this.txtDescription);
            this.tpInfo.Controls.Add(this.label1);
            this.tpInfo.Controls.Add(this.txtImportType);
            this.tpInfo.Controls.Add(this.txtProc3);
            this.tpInfo.Controls.Add(this.label2);
            this.tpInfo.Controls.Add(this.label6);
            this.tpInfo.Controls.Add(this.label3);
            this.tpInfo.Controls.Add(this.txtProc2);
            this.tpInfo.Controls.Add(this.txtStagingTable);
            this.tpInfo.Controls.Add(this.label5);
            this.tpInfo.Controls.Add(this.label4);
            this.tpInfo.Controls.Add(this.txtProc1);
            this.tpInfo.Location = new System.Drawing.Point(4, 22);
            this.tpInfo.Name = "tpInfo";
            this.tpInfo.Padding = new System.Windows.Forms.Padding(3);
            this.tpInfo.Size = new System.Drawing.Size(346, 250);
            this.tpInfo.TabIndex = 0;
            this.tpInfo.Text = "Import Information";
            this.tpInfo.UseVisualStyleBackColor = true;
            // 
            // tpColumns
            // 
            this.tpColumns.Controls.Add(this.grdTableColumns);
            this.tpColumns.Location = new System.Drawing.Point(4, 22);
            this.tpColumns.Name = "tpColumns";
            this.tpColumns.Padding = new System.Windows.Forms.Padding(3);
            this.tpColumns.Size = new System.Drawing.Size(346, 250);
            this.tpColumns.TabIndex = 1;
            this.tpColumns.Text = "Columns";
            this.tpColumns.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(217, 317);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(92, 23);
            this.btnCancel.TabIndex = 18;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // frmDesignInterface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(377, 349);
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
            ((System.ComponentModel.ISupportInitialize)(this.grdTableColumns)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tpInfo.ResumeLayout(false);
            this.tpInfo.PerformLayout();
            this.tpColumns.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtImportType;
        private System.Windows.Forms.DataGridView grdTableColumns;
        private System.Windows.Forms.DataGridViewTextBoxColumn colField;
        private System.Windows.Forms.DataGridViewComboBoxColumn colType;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RichTextBox txtDescription;
        private System.Windows.Forms.TextBox txtStagingTable;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtProc1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtProc2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtProc3;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cmbImportType;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tpInfo;
        private System.Windows.Forms.TabPage tpColumns;
        private System.Windows.Forms.Button btnCancel;
    }
}