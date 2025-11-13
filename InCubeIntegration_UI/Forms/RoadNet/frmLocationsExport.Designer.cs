namespace InCubeIntegration_UI
{
    partial class frmLocationsExport
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
            this.label6 = new System.Windows.Forms.Label();
            this.btnGetLocations = new System.Windows.Forms.Button();
            this.cmbCustomerGroups = new System.Windows.Forms.ComboBox();
            this.cbAllGroups = new System.Windows.Forms.CheckBox();
            this.cbAllLocations = new System.Windows.Forms.CheckBox();
            this.grdLocations = new System.Windows.Forms.DataGridView();
            this.colCheck = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colCustomerCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCustomerName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRegionID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAccountType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAddress1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAddress2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPhoneNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLatitude = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLongitude = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colVisitPatternSet = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFixedServiceTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colVariableServiceTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDropSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOpenTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCloseTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTW1Start = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTW1Stop = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTW2Start = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTW2Stop = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblSelectedLocations = new System.Windows.Forms.Label();
            this.btnSendLocations = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.grdLocations)).BeginInit();
            this.SuspendLayout();
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(2, 10);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(66, 13);
            this.label6.TabIndex = 19;
            this.label6.Text = "Organization";
            // 
            // btnGetLocations
            // 
            this.btnGetLocations.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGetLocations.Location = new System.Drawing.Point(1167, 9);
            this.btnGetLocations.Name = "btnGetLocations";
            this.btnGetLocations.Size = new System.Drawing.Size(103, 24);
            this.btnGetLocations.TabIndex = 18;
            this.btnGetLocations.Text = "Find Locations";
            this.btnGetLocations.UseVisualStyleBackColor = true;
            this.btnGetLocations.Click += new System.EventHandler(this.btnGetLocations_Click);
            // 
            // cmbCustomerGroups
            // 
            this.cmbCustomerGroups.Enabled = false;
            this.cmbCustomerGroups.FormattingEnabled = true;
            this.cmbCustomerGroups.Location = new System.Drawing.Point(71, 7);
            this.cmbCustomerGroups.Name = "cmbCustomerGroups";
            this.cmbCustomerGroups.Size = new System.Drawing.Size(200, 21);
            this.cmbCustomerGroups.TabIndex = 17;
            // 
            // cbAllGroups
            // 
            this.cbAllGroups.AutoSize = true;
            this.cbAllGroups.Checked = true;
            this.cbAllGroups.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbAllGroups.Location = new System.Drawing.Point(277, 9);
            this.cbAllGroups.Name = "cbAllGroups";
            this.cbAllGroups.Size = new System.Drawing.Size(37, 17);
            this.cbAllGroups.TabIndex = 20;
            this.cbAllGroups.Text = "All";
            this.cbAllGroups.UseVisualStyleBackColor = true;
            this.cbAllGroups.CheckedChanged += new System.EventHandler(this.cbAllGroups_CheckedChanged);
            // 
            // cbAllLocations
            // 
            this.cbAllLocations.AutoSize = true;
            this.cbAllLocations.Enabled = false;
            this.cbAllLocations.Location = new System.Drawing.Point(5, 49);
            this.cbAllLocations.Name = "cbAllLocations";
            this.cbAllLocations.Size = new System.Drawing.Size(37, 17);
            this.cbAllLocations.TabIndex = 22;
            this.cbAllLocations.Text = "All";
            this.cbAllLocations.UseVisualStyleBackColor = true;
            this.cbAllLocations.CheckedChanged += new System.EventHandler(this.cbAllLocations_CheckedChanged);
            // 
            // grdLocations
            // 
            this.grdLocations.AllowUserToAddRows = false;
            this.grdLocations.AllowUserToDeleteRows = false;
            this.grdLocations.AllowUserToResizeRows = false;
            this.grdLocations.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grdLocations.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdLocations.BackgroundColor = System.Drawing.Color.White;
            this.grdLocations.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdLocations.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colCheck,
            this.colCustomerCode,
            this.colCustomerName,
            this.colRegionID,
            this.colAccountType,
            this.colAddress1,
            this.colAddress2,
            this.colPhoneNo,
            this.colLatitude,
            this.colLongitude,
            this.colVisitPatternSet,
            this.colFixedServiceTime,
            this.colVariableServiceTime,
            this.colDropSize,
            this.colOpenTime,
            this.colCloseTime,
            this.colTW1Start,
            this.colTW1Stop,
            this.colTW2Start,
            this.colTW2Stop});
            this.grdLocations.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.grdLocations.Location = new System.Drawing.Point(5, 72);
            this.grdLocations.Name = "grdLocations";
            this.grdLocations.Size = new System.Drawing.Size(1265, 486);
            this.grdLocations.TabIndex = 21;
            this.grdLocations.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.grdLocations_CellValueChanged);
            this.grdLocations.CurrentCellDirtyStateChanged += new System.EventHandler(this.grdLocations_CurrentCellDirtyStateChanged);
            // 
            // colCheck
            // 
            this.colCheck.FalseValue = "0";
            this.colCheck.FillWeight = 1F;
            this.colCheck.HeaderText = "";
            this.colCheck.MinimumWidth = 30;
            this.colCheck.Name = "colCheck";
            this.colCheck.TrueValue = "1";
            // 
            // colCustomerCode
            // 
            this.colCustomerCode.DataPropertyName = "CustomerCode";
            this.colCustomerCode.FillWeight = 4F;
            this.colCustomerCode.HeaderText = "Customer Code";
            this.colCustomerCode.MinimumWidth = 50;
            this.colCustomerCode.Name = "colCustomerCode";
            this.colCustomerCode.ReadOnly = true;
            // 
            // colCustomerName
            // 
            this.colCustomerName.DataPropertyName = "CustomerName";
            this.colCustomerName.FillWeight = 6F;
            this.colCustomerName.HeaderText = "Customer Name";
            this.colCustomerName.MinimumWidth = 100;
            this.colCustomerName.Name = "colCustomerName";
            this.colCustomerName.ReadOnly = true;
            // 
            // colRegionID
            // 
            this.colRegionID.DataPropertyName = "RegionID";
            this.colRegionID.FillWeight = 2F;
            this.colRegionID.HeaderText = "Region ID";
            this.colRegionID.MinimumWidth = 50;
            this.colRegionID.Name = "colRegionID";
            this.colRegionID.ReadOnly = true;
            // 
            // colAccountType
            // 
            this.colAccountType.DataPropertyName = "AccountType";
            this.colAccountType.FillWeight = 3F;
            this.colAccountType.HeaderText = "Account Type";
            this.colAccountType.MinimumWidth = 50;
            this.colAccountType.Name = "colAccountType";
            this.colAccountType.ReadOnly = true;
            // 
            // colAddress1
            // 
            this.colAddress1.DataPropertyName = "Address1";
            this.colAddress1.FillWeight = 4F;
            this.colAddress1.HeaderText = "Address 1";
            this.colAddress1.MinimumWidth = 80;
            this.colAddress1.Name = "colAddress1";
            this.colAddress1.ReadOnly = true;
            // 
            // colAddress2
            // 
            this.colAddress2.DataPropertyName = "Address2";
            this.colAddress2.FillWeight = 4F;
            this.colAddress2.HeaderText = "Address 2";
            this.colAddress2.MinimumWidth = 80;
            this.colAddress2.Name = "colAddress2";
            this.colAddress2.ReadOnly = true;
            // 
            // colPhoneNo
            // 
            this.colPhoneNo.DataPropertyName = "PhoneNo";
            this.colPhoneNo.FillWeight = 3F;
            this.colPhoneNo.HeaderText = "PhoneNo";
            this.colPhoneNo.MinimumWidth = 50;
            this.colPhoneNo.Name = "colPhoneNo";
            this.colPhoneNo.ReadOnly = true;
            // 
            // colLatitude
            // 
            this.colLatitude.DataPropertyName = "Latitude";
            this.colLatitude.FillWeight = 3F;
            this.colLatitude.HeaderText = "Latitude";
            this.colLatitude.MinimumWidth = 50;
            this.colLatitude.Name = "colLatitude";
            this.colLatitude.ReadOnly = true;
            // 
            // colLongitude
            // 
            this.colLongitude.DataPropertyName = "Longitude";
            this.colLongitude.FillWeight = 3F;
            this.colLongitude.HeaderText = "Longitude";
            this.colLongitude.MinimumWidth = 50;
            this.colLongitude.Name = "colLongitude";
            this.colLongitude.ReadOnly = true;
            // 
            // colVisitPatternSet
            // 
            this.colVisitPatternSet.DataPropertyName = "VisitPatternSet";
            this.colVisitPatternSet.FillWeight = 3F;
            this.colVisitPatternSet.HeaderText = "Visit Pattern Set";
            this.colVisitPatternSet.MinimumWidth = 50;
            this.colVisitPatternSet.Name = "colVisitPatternSet";
            this.colVisitPatternSet.ReadOnly = true;
            // 
            // colFixedServiceTime
            // 
            this.colFixedServiceTime.DataPropertyName = "FixedServiceTime";
            this.colFixedServiceTime.FillWeight = 2F;
            this.colFixedServiceTime.HeaderText = "Fixed Service Time";
            this.colFixedServiceTime.MinimumWidth = 50;
            this.colFixedServiceTime.Name = "colFixedServiceTime";
            this.colFixedServiceTime.ReadOnly = true;
            // 
            // colVariableServiceTime
            // 
            this.colVariableServiceTime.DataPropertyName = "VariableServiceTime";
            this.colVariableServiceTime.FillWeight = 2F;
            this.colVariableServiceTime.HeaderText = "Variable Service Time";
            this.colVariableServiceTime.MinimumWidth = 50;
            this.colVariableServiceTime.Name = "colVariableServiceTime";
            this.colVariableServiceTime.ReadOnly = true;
            // 
            // colDropSize
            // 
            this.colDropSize.DataPropertyName = "DropSize";
            this.colDropSize.FillWeight = 2F;
            this.colDropSize.HeaderText = "Drop Size";
            this.colDropSize.MinimumWidth = 50;
            this.colDropSize.Name = "colDropSize";
            this.colDropSize.ReadOnly = true;
            // 
            // colOpenTime
            // 
            this.colOpenTime.DataPropertyName = "OpenTime";
            this.colOpenTime.FillWeight = 2F;
            this.colOpenTime.HeaderText = "Open Time";
            this.colOpenTime.MinimumWidth = 50;
            this.colOpenTime.Name = "colOpenTime";
            this.colOpenTime.ReadOnly = true;
            // 
            // colCloseTime
            // 
            this.colCloseTime.DataPropertyName = "CloseTime";
            this.colCloseTime.FillWeight = 2F;
            this.colCloseTime.HeaderText = "Close Time";
            this.colCloseTime.MinimumWidth = 50;
            this.colCloseTime.Name = "colCloseTime";
            this.colCloseTime.ReadOnly = true;
            // 
            // colTW1Start
            // 
            this.colTW1Start.DataPropertyName = "TW1Start";
            this.colTW1Start.FillWeight = 2F;
            this.colTW1Start.HeaderText = "TW1 Start";
            this.colTW1Start.MinimumWidth = 50;
            this.colTW1Start.Name = "colTW1Start";
            this.colTW1Start.ReadOnly = true;
            // 
            // colTW1Stop
            // 
            this.colTW1Stop.DataPropertyName = "TW1Stop";
            this.colTW1Stop.FillWeight = 2F;
            this.colTW1Stop.HeaderText = "TW1 Stop";
            this.colTW1Stop.MinimumWidth = 50;
            this.colTW1Stop.Name = "colTW1Stop";
            this.colTW1Stop.ReadOnly = true;
            // 
            // colTW2Start
            // 
            this.colTW2Start.DataPropertyName = "TW2Start";
            this.colTW2Start.FillWeight = 2F;
            this.colTW2Start.HeaderText = "TW2 Start";
            this.colTW2Start.MinimumWidth = 50;
            this.colTW2Start.Name = "colTW2Start";
            this.colTW2Start.ReadOnly = true;
            // 
            // colTW2Stop
            // 
            this.colTW2Stop.DataPropertyName = "TW2Stop";
            this.colTW2Stop.FillWeight = 2F;
            this.colTW2Stop.HeaderText = "TW2 Stop";
            this.colTW2Stop.MinimumWidth = 50;
            this.colTW2Stop.Name = "colTW2Stop";
            this.colTW2Stop.ReadOnly = true;
            // 
            // lblSelectedLocations
            // 
            this.lblSelectedLocations.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSelectedLocations.Location = new System.Drawing.Point(970, 566);
            this.lblSelectedLocations.Name = "lblSelectedLocations";
            this.lblSelectedLocations.Size = new System.Drawing.Size(172, 18);
            this.lblSelectedLocations.TabIndex = 23;
            this.lblSelectedLocations.Text = "Selected: 0/0";
            this.lblSelectedLocations.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnSendLocations
            // 
            this.btnSendLocations.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSendLocations.Enabled = false;
            this.btnSendLocations.Location = new System.Drawing.Point(1167, 563);
            this.btnSendLocations.Name = "btnSendLocations";
            this.btnSendLocations.Size = new System.Drawing.Size(103, 24);
            this.btnSendLocations.TabIndex = 24;
            this.btnSendLocations.Text = "Send Locations";
            this.btnSendLocations.UseVisualStyleBackColor = true;
            this.btnSendLocations.Click += new System.EventHandler(this.btnSendLocations_Click);
            // 
            // frmLocationsExport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1276, 591);
            this.Controls.Add(this.btnSendLocations);
            this.Controls.Add(this.lblSelectedLocations);
            this.Controls.Add(this.cbAllLocations);
            this.Controls.Add(this.grdLocations);
            this.Controls.Add(this.cbAllGroups);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.btnGetLocations);
            this.Controls.Add(this.cmbCustomerGroups);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "frmLocationsExport";
            this.ShowIcon = false;
            this.Text = "Export Locations";
            this.Load += new System.EventHandler(this.frmLocationsExport_Load);
            ((System.ComponentModel.ISupportInitialize)(this.grdLocations)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnGetLocations;
        private System.Windows.Forms.ComboBox cmbCustomerGroups;
        private System.Windows.Forms.CheckBox cbAllGroups;
        private System.Windows.Forms.CheckBox cbAllLocations;
        private System.Windows.Forms.DataGridView grdLocations;
        private System.Windows.Forms.Label lblSelectedLocations;
        private System.Windows.Forms.Button btnSendLocations;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colCheck;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCustomerCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCustomerName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRegionID;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAccountType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAddress1;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAddress2;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPhoneNo;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLatitude;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLongitude;
        private System.Windows.Forms.DataGridViewTextBoxColumn colVisitPatternSet;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFixedServiceTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colVariableServiceTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDropSize;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOpenTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCloseTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTW1Start;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTW1Stop;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTW2Start;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTW2Stop;
    }
}