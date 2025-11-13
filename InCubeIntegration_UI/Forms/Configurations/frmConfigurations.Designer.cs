namespace InCubeIntegration_UI
{
    partial class frmConfigurations
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmConfigurations));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tpGeneralConfigurations = new System.Windows.Forms.TabPage();
            this.pnlGeneralConfigurations = new System.Windows.Forms.Panel();
            this.tblConfig = new System.Windows.Forms.TableLayoutPanel();
            this.btnSaveChanges = new System.Windows.Forms.Button();
            this.cmbConfiguration = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tpFilesManagementJobs = new System.Windows.Forms.TabPage();
            this.btnDeleteFilesJob = new System.Windows.Forms.Button();
            this.btnEditFilesJob = new System.Windows.Forms.Button();
            this.btnAddFilesJob = new System.Windows.Forms.Button();
            this.lsvFilesJobs = new System.Windows.Forms.ListView();
            this.colJobID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colJobName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colJobType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSourceFolder = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colFileExtension = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabControl1.SuspendLayout();
            this.tpGeneralConfigurations.SuspendLayout();
            this.pnlGeneralConfigurations.SuspendLayout();
            this.tpFilesManagementJobs.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tpGeneralConfigurations);
            this.tabControl1.Controls.Add(this.tpFilesManagementJobs);
            this.tabControl1.Location = new System.Drawing.Point(2, 5);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(391, 376);
            this.tabControl1.TabIndex = 0;
            // 
            // tpGeneralConfigurations
            // 
            this.tpGeneralConfigurations.Controls.Add(this.pnlGeneralConfigurations);
            this.tpGeneralConfigurations.Controls.Add(this.btnSaveChanges);
            this.tpGeneralConfigurations.Controls.Add(this.cmbConfiguration);
            this.tpGeneralConfigurations.Controls.Add(this.label1);
            this.tpGeneralConfigurations.Location = new System.Drawing.Point(4, 22);
            this.tpGeneralConfigurations.Name = "tpGeneralConfigurations";
            this.tpGeneralConfigurations.Padding = new System.Windows.Forms.Padding(3);
            this.tpGeneralConfigurations.Size = new System.Drawing.Size(383, 350);
            this.tpGeneralConfigurations.TabIndex = 0;
            this.tpGeneralConfigurations.Text = "General Configurations";
            this.tpGeneralConfigurations.UseVisualStyleBackColor = true;
            // 
            // pnlGeneralConfigurations
            // 
            this.pnlGeneralConfigurations.AutoScroll = true;
            this.pnlGeneralConfigurations.Controls.Add(this.tblConfig);
            this.pnlGeneralConfigurations.Location = new System.Drawing.Point(0, 36);
            this.pnlGeneralConfigurations.Name = "pnlGeneralConfigurations";
            this.pnlGeneralConfigurations.Size = new System.Drawing.Size(383, 279);
            this.pnlGeneralConfigurations.TabIndex = 6;
            // 
            // tblConfig
            // 
            this.tblConfig.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.tblConfig.ColumnCount = 2;
            this.tblConfig.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 22.12644F));
            this.tblConfig.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 77.87357F));
            this.tblConfig.Location = new System.Drawing.Point(4, 8);
            this.tblConfig.Margin = new System.Windows.Forms.Padding(2);
            this.tblConfig.Name = "tblConfig";
            this.tblConfig.RowCount = 2;
            this.tblConfig.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 27F));
            this.tblConfig.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 27F));
            this.tblConfig.Size = new System.Drawing.Size(362, 58);
            this.tblConfig.TabIndex = 5;
            // 
            // btnSaveChanges
            // 
            this.btnSaveChanges.Location = new System.Drawing.Point(287, 320);
            this.btnSaveChanges.Margin = new System.Windows.Forms.Padding(2);
            this.btnSaveChanges.Name = "btnSaveChanges";
            this.btnSaveChanges.Size = new System.Drawing.Size(91, 25);
            this.btnSaveChanges.TabIndex = 3;
            this.btnSaveChanges.Text = "Save Changes";
            this.btnSaveChanges.UseVisualStyleBackColor = true;
            this.btnSaveChanges.Click += new System.EventHandler(this.btnSaveConfigChanges_Click);
            // 
            // cmbConfiguration
            // 
            this.cmbConfiguration.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbConfiguration.FormattingEnabled = true;
            this.cmbConfiguration.Location = new System.Drawing.Point(81, 10);
            this.cmbConfiguration.Margin = new System.Windows.Forms.Padding(2);
            this.cmbConfiguration.Name = "cmbConfiguration";
            this.cmbConfiguration.Size = new System.Drawing.Size(285, 21);
            this.cmbConfiguration.TabIndex = 1;
            this.cmbConfiguration.SelectedIndexChanged += new System.EventHandler(this.cmbConfiguration_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 12);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Configuration";
            // 
            // tpFilesManagementJobs
            // 
            this.tpFilesManagementJobs.Controls.Add(this.btnDeleteFilesJob);
            this.tpFilesManagementJobs.Controls.Add(this.btnEditFilesJob);
            this.tpFilesManagementJobs.Controls.Add(this.btnAddFilesJob);
            this.tpFilesManagementJobs.Controls.Add(this.lsvFilesJobs);
            this.tpFilesManagementJobs.Location = new System.Drawing.Point(4, 22);
            this.tpFilesManagementJobs.Name = "tpFilesManagementJobs";
            this.tpFilesManagementJobs.Padding = new System.Windows.Forms.Padding(3);
            this.tpFilesManagementJobs.Size = new System.Drawing.Size(383, 350);
            this.tpFilesManagementJobs.TabIndex = 1;
            this.tpFilesManagementJobs.Text = "Files Management Jobs";
            this.tpFilesManagementJobs.UseVisualStyleBackColor = true;
            // 
            // btnDeleteFilesJob
            // 
            this.btnDeleteFilesJob.Location = new System.Drawing.Point(199, 6);
            this.btnDeleteFilesJob.Name = "btnDeleteFilesJob";
            this.btnDeleteFilesJob.Size = new System.Drawing.Size(75, 23);
            this.btnDeleteFilesJob.TabIndex = 11;
            this.btnDeleteFilesJob.Text = "Delete";
            this.btnDeleteFilesJob.UseVisualStyleBackColor = true;
            this.btnDeleteFilesJob.Click += new System.EventHandler(this.btnDeleteFilesJob_Click);
            // 
            // btnEditFilesJob
            // 
            this.btnEditFilesJob.Location = new System.Drawing.Point(100, 6);
            this.btnEditFilesJob.Name = "btnEditFilesJob";
            this.btnEditFilesJob.Size = new System.Drawing.Size(75, 23);
            this.btnEditFilesJob.TabIndex = 10;
            this.btnEditFilesJob.Text = "Edit";
            this.btnEditFilesJob.UseVisualStyleBackColor = true;
            this.btnEditFilesJob.Click += new System.EventHandler(this.btnEditFilesJob_Click);
            // 
            // btnAddFilesJob
            // 
            this.btnAddFilesJob.Location = new System.Drawing.Point(6, 6);
            this.btnAddFilesJob.Name = "btnAddFilesJob";
            this.btnAddFilesJob.Size = new System.Drawing.Size(75, 23);
            this.btnAddFilesJob.TabIndex = 9;
            this.btnAddFilesJob.Text = "Add";
            this.btnAddFilesJob.UseVisualStyleBackColor = true;
            this.btnAddFilesJob.Click += new System.EventHandler(this.btnAddFilesJob_Click);
            // 
            // lsvFilesJobs
            // 
            this.lsvFilesJobs.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colJobID,
            this.colJobName,
            this.colJobType,
            this.colSourceFolder,
            this.colFileExtension});
            this.lsvFilesJobs.FullRowSelect = true;
            this.lsvFilesJobs.HideSelection = false;
            this.lsvFilesJobs.Location = new System.Drawing.Point(6, 36);
            this.lsvFilesJobs.MultiSelect = false;
            this.lsvFilesJobs.Name = "lsvFilesJobs";
            this.lsvFilesJobs.Size = new System.Drawing.Size(371, 308);
            this.lsvFilesJobs.TabIndex = 8;
            this.lsvFilesJobs.UseCompatibleStateImageBehavior = false;
            this.lsvFilesJobs.View = System.Windows.Forms.View.Details;
            // 
            // colJobID
            // 
            this.colJobID.Text = "JobID";
            this.colJobID.Width = 42;
            // 
            // colJobName
            // 
            this.colJobName.Text = "Job Name";
            this.colJobName.Width = 102;
            // 
            // colJobType
            // 
            this.colJobType.Text = "Job Type";
            this.colJobType.Width = 78;
            // 
            // colSourceFolder
            // 
            this.colSourceFolder.Text = "Source Folder";
            this.colSourceFolder.Width = 96;
            // 
            // colFileExtension
            // 
            this.colFileExtension.Text = "File Ext";
            this.colFileExtension.Width = 48;
            // 
            // frmConfigurations
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(194)))), ((int)(((byte)(217)))), ((int)(((byte)(247)))));
            this.ClientSize = new System.Drawing.Size(401, 383);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "frmConfigurations";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Integration Configurations";
            this.Load += new System.EventHandler(this.frmConfigurations_Load);
            this.tabControl1.ResumeLayout(false);
            this.tpGeneralConfigurations.ResumeLayout(false);
            this.tpGeneralConfigurations.PerformLayout();
            this.pnlGeneralConfigurations.ResumeLayout(false);
            this.tpFilesManagementJobs.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tpGeneralConfigurations;
        private System.Windows.Forms.ComboBox cmbConfiguration;
        private System.Windows.Forms.TableLayoutPanel tblConfig;
        private System.Windows.Forms.Button btnSaveChanges;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel pnlGeneralConfigurations;
        private System.Windows.Forms.TabPage tpFilesManagementJobs;
        private System.Windows.Forms.Button btnDeleteFilesJob;
        private System.Windows.Forms.Button btnEditFilesJob;
        private System.Windows.Forms.Button btnAddFilesJob;
        private System.Windows.Forms.ListView lsvFilesJobs;
        private System.Windows.Forms.ColumnHeader colJobID;
        private System.Windows.Forms.ColumnHeader colJobName;
        private System.Windows.Forms.ColumnHeader colJobType;
        private System.Windows.Forms.ColumnHeader colSourceFolder;
        private System.Windows.Forms.ColumnHeader colFileExtension;
    }
}