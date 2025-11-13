namespace InCubeIntegration_UI
{
    partial class frmRouteRegion
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
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.grdRoutes = new System.Windows.Forms.DataGridView();
            this.colTerritoryID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRoute = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRegion = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.colPreRegion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label1 = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnManageRegions = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.grdRoutes)).BeginInit();
            this.SuspendLayout();
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(56, 12);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(219, 20);
            this.txtSearch.TabIndex = 0;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            // 
            // grdRoutes
            // 
            this.grdRoutes.AllowUserToAddRows = false;
            this.grdRoutes.AllowUserToDeleteRows = false;
            this.grdRoutes.AllowUserToResizeRows = false;
            this.grdRoutes.BackgroundColor = System.Drawing.Color.White;
            this.grdRoutes.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdRoutes.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colTerritoryID,
            this.colRoute,
            this.colRegion,
            this.colPreRegion});
            this.grdRoutes.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.grdRoutes.Location = new System.Drawing.Point(12, 38);
            this.grdRoutes.Name = "grdRoutes";
            this.grdRoutes.Size = new System.Drawing.Size(391, 309);
            this.grdRoutes.TabIndex = 1;
            // 
            // colTerritoryID
            // 
            this.colTerritoryID.DataPropertyName = "TerritoryID";
            this.colTerritoryID.HeaderText = "TerritoryID";
            this.colTerritoryID.Name = "colTerritoryID";
            this.colTerritoryID.Visible = false;
            // 
            // colRoute
            // 
            this.colRoute.DataPropertyName = "TerritoryCode";
            this.colRoute.HeaderText = "Route";
            this.colRoute.Name = "colRoute";
            this.colRoute.ReadOnly = true;
            this.colRoute.Width = 150;
            // 
            // colRegion
            // 
            this.colRegion.DataPropertyName = "Region";
            this.colRegion.HeaderText = "Region";
            this.colRegion.Name = "colRegion";
            this.colRegion.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colRegion.Width = 150;
            // 
            // colPreRegion
            // 
            this.colPreRegion.DataPropertyName = "PreRegion";
            this.colPreRegion.HeaderText = "PreRegion";
            this.colPreRegion.Name = "colPreRegion";
            this.colPreRegion.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colPreRegion.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colPreRegion.Visible = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Search";
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(328, 353);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnManageRegions
            // 
            this.btnManageRegions.Location = new System.Drawing.Point(300, 10);
            this.btnManageRegions.Name = "btnManageRegions";
            this.btnManageRegions.Size = new System.Drawing.Size(103, 23);
            this.btnManageRegions.TabIndex = 4;
            this.btnManageRegions.Text = "Manage Regions";
            this.btnManageRegions.UseVisualStyleBackColor = true;
            this.btnManageRegions.Visible = false;
            this.btnManageRegions.Click += new System.EventHandler(this.btnManageRegions_Click);
            // 
            // frmRouteRegion
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(415, 384);
            this.Controls.Add(this.btnManageRegions);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.grdRoutes);
            this.Controls.Add(this.txtSearch);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "frmRouteRegion";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Route Region";
            this.Load += new System.EventHandler(this.frmRouteRegion_Load);
            ((System.ComponentModel.ISupportInitialize)(this.grdRoutes)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.DataGridView grdRoutes;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTerritoryID;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRoute;
        private System.Windows.Forms.DataGridViewComboBoxColumn colRegion;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPreRegion;
        private System.Windows.Forms.Button btnManageRegions;
    }
}