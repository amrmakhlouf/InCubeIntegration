namespace InCubeIntegration_UI
{
    partial class frmDataTransferConfig
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
            this.components = new System.ComponentModel.Container();
            this.lsvTransferTypes = new System.Windows.Forms.ListView();
            this.colID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSource = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDestination = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDestinationTable = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colTransferMethod = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnEdit = new System.Windows.Forms.Button();
            this.Delete = new System.Windows.Forms.Button();
            this.picDown = new System.Windows.Forms.PictureBox();
            this.picUp = new System.Windows.Forms.PictureBox();
            this.lsvGroupIncluded = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader9 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.cmbTransferGroups = new System.Windows.Forms.ComboBox();
            this.lblGroup = new System.Windows.Forms.Label();
            this.btnExclude = new System.Windows.Forms.Button();
            this.PicGroupItemDown = new System.Windows.Forms.PictureBox();
            this.picGroupItemUp = new System.Windows.Forms.PictureBox();
            this.btnInclude = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lsvGroupExcluded = new System.Windows.Forms.ListView();
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnManageGroups = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.picDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picUp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PicGroupItemDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picGroupItemUp)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // lsvTransferTypes
            // 
            this.lsvTransferTypes.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colID,
            this.colName,
            this.colSource,
            this.colDestination,
            this.colDestinationTable,
            this.colTransferMethod});
            this.lsvTransferTypes.FullRowSelect = true;
            this.lsvTransferTypes.Location = new System.Drawing.Point(12, 37);
            this.lsvTransferTypes.MultiSelect = false;
            this.lsvTransferTypes.Name = "lsvTransferTypes";
            this.lsvTransferTypes.Size = new System.Drawing.Size(908, 264);
            this.lsvTransferTypes.TabIndex = 0;
            this.lsvTransferTypes.UseCompatibleStateImageBehavior = false;
            this.lsvTransferTypes.View = System.Windows.Forms.View.Details;
            // 
            // colID
            // 
            this.colID.Text = "ID";
            this.colID.Width = 28;
            // 
            // colName
            // 
            this.colName.Text = "Name";
            this.colName.Width = 144;
            // 
            // colSource
            // 
            this.colSource.Text = "Source";
            this.colSource.Width = 107;
            // 
            // colDestination
            // 
            this.colDestination.Text = "Destination";
            this.colDestination.Width = 117;
            // 
            // colDestinationTable
            // 
            this.colDestinationTable.Text = "Destination Table";
            this.colDestinationTable.Width = 130;
            // 
            // colTransferMethod
            // 
            this.colTransferMethod.Text = "Transfer Method";
            this.colTransferMethod.Width = 203;
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(12, 9);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 2;
            this.btnAdd.Text = "Add";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnEdit
            // 
            this.btnEdit.Location = new System.Drawing.Point(106, 9);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Size = new System.Drawing.Size(75, 23);
            this.btnEdit.TabIndex = 3;
            this.btnEdit.Text = "Edit";
            this.btnEdit.UseVisualStyleBackColor = true;
            this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
            // 
            // Delete
            // 
            this.Delete.Location = new System.Drawing.Point(205, 9);
            this.Delete.Name = "Delete";
            this.Delete.Size = new System.Drawing.Size(75, 23);
            this.Delete.TabIndex = 4;
            this.Delete.Text = "Delete";
            this.Delete.UseVisualStyleBackColor = true;
            this.Delete.Click += new System.EventHandler(this.Delete_Click);
            // 
            // picDown
            // 
            this.picDown.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picDown.Image = global::InCubeIntegration_UI.Properties.Resources.down;
            this.picDown.Location = new System.Drawing.Point(880, 130);
            this.picDown.Name = "picDown";
            this.picDown.Size = new System.Drawing.Size(40, 40);
            this.picDown.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picDown.TabIndex = 34;
            this.picDown.TabStop = false;
            this.picDown.Visible = false;
            this.picDown.Click += new System.EventHandler(this.picDown_Click);
            // 
            // picUp
            // 
            this.picUp.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picUp.Image = global::InCubeIntegration_UI.Properties.Resources.up;
            this.picUp.Location = new System.Drawing.Point(880, 84);
            this.picUp.Name = "picUp";
            this.picUp.Size = new System.Drawing.Size(40, 40);
            this.picUp.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picUp.TabIndex = 33;
            this.picUp.TabStop = false;
            this.picUp.Visible = false;
            this.picUp.Click += new System.EventHandler(this.picUp_Click);
            // 
            // lsvGroupIncluded
            // 
            this.lsvGroupIncluded.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader4,
            this.columnHeader9});
            this.lsvGroupIncluded.FullRowSelect = true;
            this.lsvGroupIncluded.Location = new System.Drawing.Point(50, 19);
            this.lsvGroupIncluded.Name = "lsvGroupIncluded";
            this.lsvGroupIncluded.Size = new System.Drawing.Size(393, 190);
            this.lsvGroupIncluded.TabIndex = 35;
            this.lsvGroupIncluded.UseCompatibleStateImageBehavior = false;
            this.lsvGroupIncluded.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "ID";
            this.columnHeader1.Width = 28;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Name";
            this.columnHeader2.Width = 170;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Destination";
            this.columnHeader4.Width = 117;
            // 
            // columnHeader9
            // 
            this.columnHeader9.Text = "Seq";
            // 
            // cmbTransferGroups
            // 
            this.cmbTransferGroups.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTransferGroups.FormattingEnabled = true;
            this.cmbTransferGroups.Location = new System.Drawing.Point(125, 307);
            this.cmbTransferGroups.Name = "cmbTransferGroups";
            this.cmbTransferGroups.Size = new System.Drawing.Size(218, 21);
            this.cmbTransferGroups.TabIndex = 36;
            this.cmbTransferGroups.SelectedIndexChanged += new System.EventHandler(this.cmbTransferGroups_SelectedIndexChanged);
            // 
            // lblGroup
            // 
            this.lblGroup.AutoSize = true;
            this.lblGroup.Location = new System.Drawing.Point(12, 311);
            this.lblGroup.Name = "lblGroup";
            this.lblGroup.Size = new System.Drawing.Size(78, 13);
            this.lblGroup.TabIndex = 37;
            this.lblGroup.Text = "Transfer Group";
            // 
            // btnExclude
            // 
            this.btnExclude.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExclude.Location = new System.Drawing.Point(472, 403);
            this.btnExclude.Name = "btnExclude";
            this.btnExclude.Size = new System.Drawing.Size(35, 26);
            this.btnExclude.TabIndex = 38;
            this.btnExclude.Text = ">";
            this.toolTip1.SetToolTip(this.btnExclude, "Remove");
            this.btnExclude.UseVisualStyleBackColor = true;
            this.btnExclude.Click += new System.EventHandler(this.btnExclude_Click);
            // 
            // PicGroupItemDown
            // 
            this.PicGroupItemDown.Cursor = System.Windows.Forms.Cursors.Hand;
            this.PicGroupItemDown.Image = global::InCubeIntegration_UI.Properties.Resources.down;
            this.PicGroupItemDown.Location = new System.Drawing.Point(6, 106);
            this.PicGroupItemDown.Name = "PicGroupItemDown";
            this.PicGroupItemDown.Size = new System.Drawing.Size(40, 40);
            this.PicGroupItemDown.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.PicGroupItemDown.TabIndex = 40;
            this.PicGroupItemDown.TabStop = false;
            this.PicGroupItemDown.Click += new System.EventHandler(this.PicGroupItemDown_Click);
            // 
            // picGroupItemUp
            // 
            this.picGroupItemUp.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picGroupItemUp.Image = global::InCubeIntegration_UI.Properties.Resources.up;
            this.picGroupItemUp.Location = new System.Drawing.Point(6, 60);
            this.picGroupItemUp.Name = "picGroupItemUp";
            this.picGroupItemUp.Size = new System.Drawing.Size(40, 40);
            this.picGroupItemUp.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picGroupItemUp.TabIndex = 39;
            this.picGroupItemUp.TabStop = false;
            this.picGroupItemUp.Click += new System.EventHandler(this.picGroupItemUp_Click);
            // 
            // btnInclude
            // 
            this.btnInclude.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnInclude.Location = new System.Drawing.Point(472, 445);
            this.btnInclude.Name = "btnInclude";
            this.btnInclude.Size = new System.Drawing.Size(35, 26);
            this.btnInclude.TabIndex = 41;
            this.btnInclude.Text = "<";
            this.toolTip1.SetToolTip(this.btnInclude, "Add");
            this.btnInclude.UseVisualStyleBackColor = true;
            this.btnInclude.Click += new System.EventHandler(this.btnInclude_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lsvGroupIncluded);
            this.groupBox1.Controls.Add(this.picGroupItemUp);
            this.groupBox1.Controls.Add(this.PicGroupItemDown);
            this.groupBox1.Location = new System.Drawing.Point(12, 334);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(452, 215);
            this.groupBox1.TabIndex = 42;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Included";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.lsvGroupExcluded);
            this.groupBox2.Location = new System.Drawing.Point(514, 334);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(406, 215);
            this.groupBox2.TabIndex = 43;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Excluded";
            // 
            // lsvGroupExcluded
            // 
            this.lsvGroupExcluded.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader5,
            this.columnHeader6,
            this.columnHeader7,
            this.columnHeader8});
            this.lsvGroupExcluded.FullRowSelect = true;
            this.lsvGroupExcluded.Location = new System.Drawing.Point(6, 19);
            this.lsvGroupExcluded.Name = "lsvGroupExcluded";
            this.lsvGroupExcluded.Size = new System.Drawing.Size(393, 190);
            this.lsvGroupExcluded.TabIndex = 35;
            this.lsvGroupExcluded.UseCompatibleStateImageBehavior = false;
            this.lsvGroupExcluded.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "ID";
            this.columnHeader5.Width = 28;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Name";
            this.columnHeader6.Width = 144;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "Source";
            this.columnHeader7.Width = 107;
            // 
            // columnHeader8
            // 
            this.columnHeader8.Text = "Destination";
            this.columnHeader8.Width = 117;
            // 
            // btnManageGroups
            // 
            this.btnManageGroups.Location = new System.Drawing.Point(363, 306);
            this.btnManageGroups.Name = "btnManageGroups";
            this.btnManageGroups.Size = new System.Drawing.Size(93, 23);
            this.btnManageGroups.TabIndex = 44;
            this.btnManageGroups.Text = "Manage Groups";
            this.toolTip1.SetToolTip(this.btnManageGroups, "Screen not ready yet, manage in DB in table Int_DataTrasferGroups");
            this.btnManageGroups.UseVisualStyleBackColor = true;
            this.btnManageGroups.Click += new System.EventHandler(this.btnManageGroups_Click);
            // 
            // frmDataTransferConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(929, 561);
            this.Controls.Add(this.btnManageGroups);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnInclude);
            this.Controls.Add(this.btnExclude);
            this.Controls.Add(this.lblGroup);
            this.Controls.Add(this.cmbTransferGroups);
            this.Controls.Add(this.picDown);
            this.Controls.Add(this.picUp);
            this.Controls.Add(this.Delete);
            this.Controls.Add(this.btnEdit);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.lsvTransferTypes);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MinimizeBox = false;
            this.Name = "frmDataTransferConfig";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Data Transfer Config";
            this.Load += new System.EventHandler(this.frmDataTransferConfig_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picUp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PicGroupItemDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picGroupItemUp)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView lsvTransferTypes;
        private System.Windows.Forms.ColumnHeader colID;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colSource;
        private System.Windows.Forms.ColumnHeader colDestination;
        private System.Windows.Forms.ColumnHeader colDestinationTable;
        private System.Windows.Forms.ColumnHeader colTransferMethod;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnEdit;
        private System.Windows.Forms.Button Delete;
        private System.Windows.Forms.PictureBox picDown;
        private System.Windows.Forms.PictureBox picUp;
        private System.Windows.Forms.ListView lsvGroupIncluded;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ComboBox cmbTransferGroups;
        private System.Windows.Forms.Label lblGroup;
        private System.Windows.Forms.Button btnExclude;
        private System.Windows.Forms.PictureBox PicGroupItemDown;
        private System.Windows.Forms.PictureBox picGroupItemUp;
        private System.Windows.Forms.Button btnInclude;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ListView lsvGroupExcluded;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.ColumnHeader columnHeader8;
        private System.Windows.Forms.Button btnManageGroups;
        private System.Windows.Forms.ColumnHeader columnHeader9;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}