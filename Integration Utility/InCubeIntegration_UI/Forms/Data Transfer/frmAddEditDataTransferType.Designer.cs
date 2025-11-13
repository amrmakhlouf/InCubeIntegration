namespace InCubeIntegration_UI
{
    partial class frmAddEditDataTransferType
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
            this.label1 = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.cmbDestDB = new System.Windows.Forms.ComboBox();
            this.txtSelectQuery = new System.Windows.Forms.RichTextBox();
            this.txtID = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.txtDestTable = new System.Windows.Forms.TextBox();
            this.cmbTransferMethod = new System.Windows.Forms.ComboBox();
            this.btnAddDatabaseConnection = new System.Windows.Forms.Button();
            this.btnEditDestConn = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.cmbSrcDB = new System.Windows.Forms.ComboBox();
            this.btnEditSrcConn = new System.Windows.Forms.Button();
            this.cbIdentity = new System.Windows.Forms.CheckBox();
            this.cbSetConstantValues = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(18, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "ID";
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(446, 344);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 7;
            this.btnSave.Text = "Save";
            this.btnSave.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // cmbDestDB
            // 
            this.cmbDestDB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDestDB.FormattingEnabled = true;
            this.cmbDestDB.Location = new System.Drawing.Point(137, 263);
            this.cmbDestDB.Name = "cmbDestDB";
            this.cmbDestDB.Size = new System.Drawing.Size(194, 21);
            this.cmbDestDB.TabIndex = 4;
            // 
            // txtSelectQuery
            // 
            this.txtSelectQuery.Location = new System.Drawing.Point(137, 71);
            this.txtSelectQuery.Name = "txtSelectQuery";
            this.txtSelectQuery.Size = new System.Drawing.Size(384, 157);
            this.txtSelectQuery.TabIndex = 2;
            this.txtSelectQuery.Text = "";
            // 
            // txtID
            // 
            this.txtID.Location = new System.Drawing.Point(137, 12);
            this.txtID.Name = "txtID";
            this.txtID.ReadOnly = true;
            this.txtID.Size = new System.Drawing.Size(65, 20);
            this.txtID.TabIndex = 100;
            this.txtID.TabStop = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 44);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Name";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 74);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(68, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Select Query";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 266);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(117, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Destination Connection";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 296);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(90, 13);
            this.label5.TabIndex = 8;
            this.label5.Text = "Destination Table";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 326);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(85, 13);
            this.label6.TabIndex = 9;
            this.label6.Text = "Transfer Method";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(137, 41);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(384, 20);
            this.txtName.TabIndex = 1;
            this.txtName.TextChanged += new System.EventHandler(this.txtName_TextChanged);
            this.txtName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtName_KeyDown);
            // 
            // txtDestTable
            // 
            this.txtDestTable.Location = new System.Drawing.Point(137, 293);
            this.txtDestTable.Name = "txtDestTable";
            this.txtDestTable.Size = new System.Drawing.Size(194, 20);
            this.txtDestTable.TabIndex = 5;
            this.txtDestTable.Text = " ";
            // 
            // cmbTransferMethod
            // 
            this.cmbTransferMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTransferMethod.FormattingEnabled = true;
            this.cmbTransferMethod.Location = new System.Drawing.Point(137, 323);
            this.cmbTransferMethod.Name = "cmbTransferMethod";
            this.cmbTransferMethod.Size = new System.Drawing.Size(194, 21);
            this.cmbTransferMethod.TabIndex = 6;
            // 
            // btnAddDatabaseConnection
            // 
            this.btnAddDatabaseConnection.Location = new System.Drawing.Point(395, 233);
            this.btnAddDatabaseConnection.Name = "btnAddDatabaseConnection";
            this.btnAddDatabaseConnection.Size = new System.Drawing.Size(52, 52);
            this.btnAddDatabaseConnection.TabIndex = 7;
            this.btnAddDatabaseConnection.TabStop = false;
            this.btnAddDatabaseConnection.Text = "Add";
            this.btnAddDatabaseConnection.UseVisualStyleBackColor = true;
            this.btnAddDatabaseConnection.Click += new System.EventHandler(this.btnAddDestDB_Click);
            // 
            // btnEditDestConn
            // 
            this.btnEditDestConn.Location = new System.Drawing.Point(337, 262);
            this.btnEditDestConn.Name = "btnEditDestConn";
            this.btnEditDestConn.Size = new System.Drawing.Size(52, 23);
            this.btnEditDestConn.TabIndex = 8;
            this.btnEditDestConn.TabStop = false;
            this.btnEditDestConn.Text = "Edit";
            this.btnEditDestConn.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnEditDestConn.UseVisualStyleBackColor = true;
            this.btnEditDestConn.Click += new System.EventHandler(this.btnEditConnection_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 237);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(98, 13);
            this.label7.TabIndex = 103;
            this.label7.Text = "Source Connection";
            // 
            // cmbSrcDB
            // 
            this.cmbSrcDB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSrcDB.FormattingEnabled = true;
            this.cmbSrcDB.Location = new System.Drawing.Point(137, 234);
            this.cmbSrcDB.Name = "cmbSrcDB";
            this.cmbSrcDB.Size = new System.Drawing.Size(194, 21);
            this.cmbSrcDB.TabIndex = 3;
            // 
            // btnEditSrcConn
            // 
            this.btnEditSrcConn.Location = new System.Drawing.Point(337, 233);
            this.btnEditSrcConn.Name = "btnEditSrcConn";
            this.btnEditSrcConn.Size = new System.Drawing.Size(52, 23);
            this.btnEditSrcConn.TabIndex = 104;
            this.btnEditSrcConn.TabStop = false;
            this.btnEditSrcConn.Text = "Edit";
            this.btnEditSrcConn.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnEditSrcConn.UseVisualStyleBackColor = true;
            this.btnEditSrcConn.Click += new System.EventHandler(this.btnEditConnection_Click);
            // 
            // cbIdentity
            // 
            this.cbIdentity.AutoSize = true;
            this.cbIdentity.Location = new System.Drawing.Point(337, 295);
            this.cbIdentity.Name = "cbIdentity";
            this.cbIdentity.Size = new System.Drawing.Size(120, 17);
            this.cbIdentity.TabIndex = 105;
            this.cbIdentity.Text = "Has Identity Column";
            this.cbIdentity.UseVisualStyleBackColor = true;
            // 
            // cbSetConstantValues
            // 
            this.cbSetConstantValues.AutoSize = true;
            this.cbSetConstantValues.Location = new System.Drawing.Point(337, 325);
            this.cbSetConstantValues.Name = "cbSetConstantValues";
            this.cbSetConstantValues.Size = new System.Drawing.Size(122, 17);
            this.cbSetConstantValues.TabIndex = 106;
            this.cbSetConstantValues.Text = "Set Constant Values";
            this.cbSetConstantValues.UseVisualStyleBackColor = true;
            // 
            // frmAddEditDataTransferType
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(529, 376);
            this.Controls.Add(this.cbSetConstantValues);
            this.Controls.Add(this.cbIdentity);
            this.Controls.Add(this.btnEditSrcConn);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.cmbSrcDB);
            this.Controls.Add(this.btnEditDestConn);
            this.Controls.Add(this.btnAddDatabaseConnection);
            this.Controls.Add(this.cmbTransferMethod);
            this.Controls.Add(this.txtDestTable);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtID);
            this.Controls.Add(this.txtSelectQuery);
            this.Controls.Add(this.cmbDestDB);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MinimizeBox = false;
            this.Name = "frmAddEditDataTransferType";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add/Edit Data Transfer Type";
            this.Load += new System.EventHandler(this.frmAddEditDataTransferType_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.ComboBox cmbDestDB;
        private System.Windows.Forms.RichTextBox txtSelectQuery;
        private System.Windows.Forms.TextBox txtID;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.TextBox txtDestTable;
        private System.Windows.Forms.ComboBox cmbTransferMethod;
        private System.Windows.Forms.Button btnAddDatabaseConnection;
        private System.Windows.Forms.Button btnEditDestConn;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cmbSrcDB;
        private System.Windows.Forms.Button btnEditSrcConn;
        private System.Windows.Forms.CheckBox cbIdentity;
        private System.Windows.Forms.CheckBox cbSetConstantValues;
    }
}