namespace InCubeIntegration_UI
{
    partial class frmRoadNetStandardInstructions
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
            this.grdRoutes = new System.Windows.Forms.DataGridView();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnLoadExcel = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.colCustomerID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.OutletID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCustomerCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOutletCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStandardInstructions = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPreSP = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.grdRoutes)).BeginInit();
            this.SuspendLayout();
            // 
            // grdRoutes
            // 
            this.grdRoutes.AllowUserToAddRows = false;
            this.grdRoutes.AllowUserToDeleteRows = false;
            this.grdRoutes.AllowUserToResizeRows = false;
            this.grdRoutes.BackgroundColor = System.Drawing.Color.White;
            this.grdRoutes.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdRoutes.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colCustomerID,
            this.OutletID,
            this.colCustomerCode,
            this.colOutletCode,
            this.colStandardInstructions,
            this.colPreSP});
            this.grdRoutes.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.grdRoutes.Location = new System.Drawing.Point(12, 38);
            this.grdRoutes.Name = "grdRoutes";
            this.grdRoutes.Size = new System.Drawing.Size(686, 341);
            this.grdRoutes.TabIndex = 6;
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(56, 12);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(444, 20);
            this.txtSearch.TabIndex = 5;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            // 
            // btnLoadExcel
            // 
            this.btnLoadExcel.Location = new System.Drawing.Point(595, 10);
            this.btnLoadExcel.Name = "btnLoadExcel";
            this.btnLoadExcel.Size = new System.Drawing.Size(103, 23);
            this.btnLoadExcel.TabIndex = 9;
            this.btnLoadExcel.Text = "Excel Load";
            this.btnLoadExcel.UseVisualStyleBackColor = true;
            this.btnLoadExcel.Click += new System.EventHandler(this.btnLoadFromExcel_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(623, 385);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 8;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Search";
            // 
            // colCustomerID
            // 
            this.colCustomerID.DataPropertyName = "CustomerID";
            this.colCustomerID.HeaderText = "CustomerID";
            this.colCustomerID.Name = "colCustomerID";
            this.colCustomerID.ReadOnly = true;
            this.colCustomerID.Visible = false;
            // 
            // OutletID
            // 
            this.OutletID.DataPropertyName = "OutletID";
            this.OutletID.HeaderText = "OutletID";
            this.OutletID.Name = "OutletID";
            this.OutletID.ReadOnly = true;
            this.OutletID.Visible = false;
            // 
            // colCustomerCode
            // 
            this.colCustomerCode.DataPropertyName = "CustomerCode";
            this.colCustomerCode.HeaderText = "Customer Code";
            this.colCustomerCode.Name = "colCustomerCode";
            this.colCustomerCode.ReadOnly = true;
            this.colCustomerCode.Width = 120;
            // 
            // colOutletCode
            // 
            this.colOutletCode.DataPropertyName = "OutletCode";
            this.colOutletCode.HeaderText = "OutletCode";
            this.colOutletCode.Name = "colOutletCode";
            this.colOutletCode.ReadOnly = true;
            this.colOutletCode.Width = 120;
            // 
            // colStandardInstructions
            // 
            this.colStandardInstructions.DataPropertyName = "StandardInstructions";
            this.colStandardInstructions.HeaderText = "Standard Instructions";
            this.colStandardInstructions.Name = "colStandardInstructions";
            this.colStandardInstructions.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colStandardInstructions.Width = 400;
            // 
            // colPreSP
            // 
            this.colPreSP.DataPropertyName = "PreSP";
            this.colPreSP.HeaderText = "PreSP";
            this.colPreSP.Name = "colPreSP";
            this.colPreSP.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colPreSP.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colPreSP.Visible = false;
            // 
            // frmRoadNetSpecialInstructions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(711, 412);
            this.Controls.Add(this.grdRoutes);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.btnLoadExcel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "frmRoadNetSpecialInstructions";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "RoadNet Standard Instructions";
            this.Load += new System.EventHandler(this.frmRoadNetStandardInstructions_Load);
            ((System.ComponentModel.ISupportInitialize)(this.grdRoutes)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView grdRoutes;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Button btnLoadExcel;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCustomerID;
        private System.Windows.Forms.DataGridViewTextBoxColumn OutletID;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCustomerCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOutletCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStandardInstructions;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPreSP;
    }
}