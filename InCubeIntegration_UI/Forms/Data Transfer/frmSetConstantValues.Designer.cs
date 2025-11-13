namespace InCubeIntegration_UI
{
    partial class frmSetConstantValues
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
            this.cmbColumns = new System.Windows.Forms.ComboBox();
            this.dtpDateValue = new System.Windows.Forms.DateTimePicker();
            this.txtTextValue = new System.Windows.Forms.TextBox();
            this.lsvConstantValues = new System.Windows.Forms.ListView();
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.label1 = new System.Windows.Forms.Label();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnAddEdit = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbBooleanValue = new System.Windows.Forms.ComboBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.lblType = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // cmbColumns
            // 
            this.cmbColumns.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbColumns.FormattingEnabled = true;
            this.cmbColumns.Location = new System.Drawing.Point(60, 6);
            this.cmbColumns.Name = "cmbColumns";
            this.cmbColumns.Size = new System.Drawing.Size(159, 21);
            this.cmbColumns.TabIndex = 0;
            this.cmbColumns.SelectedIndexChanged += new System.EventHandler(this.cmbColumns_SelectedIndexChanged);
            // 
            // dtpDateValue
            // 
            this.dtpDateValue.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDateValue.Location = new System.Drawing.Point(60, 32);
            this.dtpDateValue.Name = "dtpDateValue";
            this.dtpDateValue.Size = new System.Drawing.Size(159, 20);
            this.dtpDateValue.TabIndex = 1;
            this.dtpDateValue.Visible = false;
            // 
            // txtTextValue
            // 
            this.txtTextValue.Location = new System.Drawing.Point(212, 159);
            this.txtTextValue.Name = "txtTextValue";
            this.txtTextValue.Size = new System.Drawing.Size(159, 20);
            this.txtTextValue.TabIndex = 2;
            this.txtTextValue.Visible = false;
            // 
            // lsvConstantValues
            // 
            this.lsvConstantValues.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colType,
            this.colValue});
            this.lsvConstantValues.FullRowSelect = true;
            this.lsvConstantValues.Location = new System.Drawing.Point(12, 64);
            this.lsvConstantValues.Name = "lsvConstantValues";
            this.lsvConstantValues.Size = new System.Drawing.Size(369, 188);
            this.lsvConstantValues.TabIndex = 4;
            this.lsvConstantValues.UseCompatibleStateImageBehavior = false;
            this.lsvConstantValues.View = System.Windows.Forms.View.Details;
            this.lsvConstantValues.SelectedIndexChanged += new System.EventHandler(this.lsvConstantValues_SelectedIndexChanged);
            // 
            // colName
            // 
            this.colName.Text = "Column Name";
            this.colName.Width = 140;
            // 
            // colType
            // 
            this.colType.Text = "Column Type";
            this.colType.Width = 100;
            // 
            // colValue
            // 
            this.colValue.Text = "Value";
            this.colValue.Width = 120;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(42, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Column";
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(306, 31);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(75, 23);
            this.btnDelete.TabIndex = 6;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Visible = false;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnAddEdit
            // 
            this.btnAddEdit.Location = new System.Drawing.Point(225, 31);
            this.btnAddEdit.Name = "btnAddEdit";
            this.btnAddEdit.Size = new System.Drawing.Size(75, 23);
            this.btnAddEdit.TabIndex = 7;
            this.btnAddEdit.Text = "Add";
            this.btnAddEdit.UseVisualStyleBackColor = true;
            this.btnAddEdit.Click += new System.EventHandler(this.btnAddEdit_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 38);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Value";
            // 
            // cmbBooleanValue
            // 
            this.cmbBooleanValue.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBooleanValue.FormattingEnabled = true;
            this.cmbBooleanValue.Location = new System.Drawing.Point(106, 95);
            this.cmbBooleanValue.Name = "cmbBooleanValue";
            this.cmbBooleanValue.Size = new System.Drawing.Size(159, 21);
            this.cmbBooleanValue.TabIndex = 9;
            this.cmbBooleanValue.Visible = false;
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(306, 258);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 10;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // lblType
            // 
            this.lblType.AutoSize = true;
            this.lblType.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblType.Location = new System.Drawing.Point(225, 9);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(35, 13);
            this.lblType.TabIndex = 11;
            this.lblType.Text = "Type";
            // 
            // frmSetConstantValues
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(393, 286);
            this.Controls.Add(this.lblType);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.txtTextValue);
            this.Controls.Add(this.cmbBooleanValue);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnAddEdit);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lsvConstantValues);
            this.Controls.Add(this.dtpDateValue);
            this.Controls.Add(this.cmbColumns);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "frmSetConstantValues";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Set Constant Values";
            this.Load += new System.EventHandler(this.frmSetConstantValues_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbColumns;
        private System.Windows.Forms.DateTimePicker dtpDateValue;
        private System.Windows.Forms.TextBox txtTextValue;
        private System.Windows.Forms.ListView lsvConstantValues;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnAddEdit;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbBooleanValue;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.ColumnHeader colValue;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Label lblType;
    }
}