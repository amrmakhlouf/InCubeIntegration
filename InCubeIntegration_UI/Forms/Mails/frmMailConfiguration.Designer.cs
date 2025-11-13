namespace InCubeIntegration_UI
{
    partial class frmMailConfiguration
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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tpSenderProfile = new System.Windows.Forms.TabPage();
            this.tpMailTemplates = new System.Windows.Forms.TabPage();
            this.lsvSenderProfile = new System.Windows.Forms.ListView();
            this.btnDeleteProfile = new System.Windows.Forms.Button();
            this.btnEditProfile = new System.Windows.Forms.Button();
            this.btnAddProfile = new System.Windows.Forms.Button();
            this.colProfileName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colHost = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPort = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colMailAddress = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDisplayName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSSL = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnDeleteTemplate = new System.Windows.Forms.Button();
            this.btnEditTemplate = new System.Windows.Forms.Button();
            this.btnAddTemplate = new System.Windows.Forms.Button();
            this.lsvTemplates = new System.Windows.Forms.ListView();
            this.colTemplateName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSenderProfile = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSubject = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colRecipients = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabControl1.SuspendLayout();
            this.tpSenderProfile.SuspendLayout();
            this.tpMailTemplates.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tpSenderProfile);
            this.tabControl1.Controls.Add(this.tpMailTemplates);
            this.tabControl1.Location = new System.Drawing.Point(9, 8);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(695, 318);
            this.tabControl1.TabIndex = 0;
            // 
            // tpSenderProfile
            // 
            this.tpSenderProfile.Controls.Add(this.btnDeleteProfile);
            this.tpSenderProfile.Controls.Add(this.btnEditProfile);
            this.tpSenderProfile.Controls.Add(this.btnAddProfile);
            this.tpSenderProfile.Controls.Add(this.lsvSenderProfile);
            this.tpSenderProfile.Location = new System.Drawing.Point(4, 22);
            this.tpSenderProfile.Name = "tpSenderProfile";
            this.tpSenderProfile.Padding = new System.Windows.Forms.Padding(3);
            this.tpSenderProfile.Size = new System.Drawing.Size(687, 292);
            this.tpSenderProfile.TabIndex = 0;
            this.tpSenderProfile.Text = "Sender Profile";
            this.tpSenderProfile.UseVisualStyleBackColor = true;
            // 
            // tpMailTemplates
            // 
            this.tpMailTemplates.Controls.Add(this.btnDeleteTemplate);
            this.tpMailTemplates.Controls.Add(this.btnEditTemplate);
            this.tpMailTemplates.Controls.Add(this.btnAddTemplate);
            this.tpMailTemplates.Controls.Add(this.lsvTemplates);
            this.tpMailTemplates.Location = new System.Drawing.Point(4, 22);
            this.tpMailTemplates.Name = "tpMailTemplates";
            this.tpMailTemplates.Padding = new System.Windows.Forms.Padding(3);
            this.tpMailTemplates.Size = new System.Drawing.Size(687, 292);
            this.tpMailTemplates.TabIndex = 1;
            this.tpMailTemplates.Text = "Mail Templates";
            this.tpMailTemplates.UseVisualStyleBackColor = true;
            // 
            // lsvSenderProfile
            // 
            this.lsvSenderProfile.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colProfileName,
            this.colHost,
            this.colPort,
            this.colMailAddress,
            this.colDisplayName,
            this.colSSL});
            this.lsvSenderProfile.FullRowSelect = true;
            this.lsvSenderProfile.Location = new System.Drawing.Point(6, 40);
            this.lsvSenderProfile.MultiSelect = false;
            this.lsvSenderProfile.Name = "lsvSenderProfile";
            this.lsvSenderProfile.Size = new System.Drawing.Size(672, 246);
            this.lsvSenderProfile.TabIndex = 0;
            this.lsvSenderProfile.UseCompatibleStateImageBehavior = false;
            this.lsvSenderProfile.View = System.Windows.Forms.View.Details;
            // 
            // btnDeleteProfile
            // 
            this.btnDeleteProfile.Location = new System.Drawing.Point(199, 10);
            this.btnDeleteProfile.Name = "btnDeleteProfile";
            this.btnDeleteProfile.Size = new System.Drawing.Size(75, 23);
            this.btnDeleteProfile.TabIndex = 7;
            this.btnDeleteProfile.Text = "Delete";
            this.btnDeleteProfile.UseVisualStyleBackColor = true;
            this.btnDeleteProfile.Click += new System.EventHandler(this.btnDeleteProfile_Click);
            // 
            // btnEditProfile
            // 
            this.btnEditProfile.Location = new System.Drawing.Point(100, 10);
            this.btnEditProfile.Name = "btnEditProfile";
            this.btnEditProfile.Size = new System.Drawing.Size(75, 23);
            this.btnEditProfile.TabIndex = 6;
            this.btnEditProfile.Text = "Edit";
            this.btnEditProfile.UseVisualStyleBackColor = true;
            this.btnEditProfile.Click += new System.EventHandler(this.btnEditProfile_Click);
            // 
            // btnAddProfile
            // 
            this.btnAddProfile.Location = new System.Drawing.Point(6, 10);
            this.btnAddProfile.Name = "btnAddProfile";
            this.btnAddProfile.Size = new System.Drawing.Size(75, 23);
            this.btnAddProfile.TabIndex = 5;
            this.btnAddProfile.Text = "Add";
            this.btnAddProfile.UseVisualStyleBackColor = true;
            this.btnAddProfile.Click += new System.EventHandler(this.btnAddProfile_Click);
            // 
            // colProfileName
            // 
            this.colProfileName.Text = "Profile Name";
            this.colProfileName.Width = 127;
            // 
            // colHost
            // 
            this.colHost.Text = "Host";
            this.colHost.Width = 164;
            // 
            // colPort
            // 
            this.colPort.Text = "Port";
            this.colPort.Width = 56;
            // 
            // colMailAddress
            // 
            this.colMailAddress.Text = "Mail Address";
            this.colMailAddress.Width = 145;
            // 
            // colDisplayName
            // 
            this.colDisplayName.Text = "Display Name";
            this.colDisplayName.Width = 128;
            // 
            // colSSL
            // 
            this.colSSL.Text = "SSL";
            this.colSSL.Width = 44;
            // 
            // btnDeleteTemplate
            // 
            this.btnDeleteTemplate.Location = new System.Drawing.Point(199, 10);
            this.btnDeleteTemplate.Name = "btnDeleteTemplate";
            this.btnDeleteTemplate.Size = new System.Drawing.Size(75, 23);
            this.btnDeleteTemplate.TabIndex = 11;
            this.btnDeleteTemplate.Text = "Delete";
            this.btnDeleteTemplate.UseVisualStyleBackColor = true;
            this.btnDeleteTemplate.Click += new System.EventHandler(this.btnDeleteTemplate_Click);
            // 
            // btnEditTemplate
            // 
            this.btnEditTemplate.Location = new System.Drawing.Point(100, 10);
            this.btnEditTemplate.Name = "btnEditTemplate";
            this.btnEditTemplate.Size = new System.Drawing.Size(75, 23);
            this.btnEditTemplate.TabIndex = 10;
            this.btnEditTemplate.Text = "Edit";
            this.btnEditTemplate.UseVisualStyleBackColor = true;
            this.btnEditTemplate.Click += new System.EventHandler(this.btnEditTemplate_Click);
            // 
            // btnAddTemplate
            // 
            this.btnAddTemplate.Location = new System.Drawing.Point(6, 10);
            this.btnAddTemplate.Name = "btnAddTemplate";
            this.btnAddTemplate.Size = new System.Drawing.Size(75, 23);
            this.btnAddTemplate.TabIndex = 9;
            this.btnAddTemplate.Text = "Add";
            this.btnAddTemplate.UseVisualStyleBackColor = true;
            this.btnAddTemplate.Click += new System.EventHandler(this.btnAddTemplate_Click);
            // 
            // lsvTemplates
            // 
            this.lsvTemplates.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colTemplateName,
            this.colSenderProfile,
            this.colSubject,
            this.colRecipients});
            this.lsvTemplates.FullRowSelect = true;
            this.lsvTemplates.Location = new System.Drawing.Point(6, 40);
            this.lsvTemplates.MultiSelect = false;
            this.lsvTemplates.Name = "lsvTemplates";
            this.lsvTemplates.Size = new System.Drawing.Size(672, 246);
            this.lsvTemplates.TabIndex = 8;
            this.lsvTemplates.UseCompatibleStateImageBehavior = false;
            this.lsvTemplates.View = System.Windows.Forms.View.Details;
            // 
            // colTemplateName
            // 
            this.colTemplateName.Text = "Template Name";
            this.colTemplateName.Width = 127;
            // 
            // colSenderProfile
            // 
            this.colSenderProfile.Text = "Sender Profile";
            this.colSenderProfile.Width = 164;
            // 
            // colSubject
            // 
            this.colSubject.Text = "Subject";
            this.colSubject.Width = 56;
            // 
            // colRecipients
            // 
            this.colRecipients.Text = "Recipients";
            this.colRecipients.Width = 145;
            // 
            // frmMailConfiguration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(708, 328);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "frmMailConfiguration";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Mail Configuration";
            this.Load += new System.EventHandler(this.frmMailConfiguration_Load);
            this.tabControl1.ResumeLayout(false);
            this.tpSenderProfile.ResumeLayout(false);
            this.tpMailTemplates.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tpSenderProfile;
        private System.Windows.Forms.TabPage tpMailTemplates;
        private System.Windows.Forms.ListView lsvSenderProfile;
        private System.Windows.Forms.Button btnDeleteProfile;
        private System.Windows.Forms.Button btnEditProfile;
        private System.Windows.Forms.Button btnAddProfile;
        private System.Windows.Forms.ColumnHeader colProfileName;
        private System.Windows.Forms.ColumnHeader colHost;
        private System.Windows.Forms.ColumnHeader colPort;
        private System.Windows.Forms.ColumnHeader colMailAddress;
        private System.Windows.Forms.ColumnHeader colDisplayName;
        private System.Windows.Forms.ColumnHeader colSSL;
        private System.Windows.Forms.Button btnDeleteTemplate;
        private System.Windows.Forms.Button btnEditTemplate;
        private System.Windows.Forms.Button btnAddTemplate;
        private System.Windows.Forms.ListView lsvTemplates;
        private System.Windows.Forms.ColumnHeader colTemplateName;
        private System.Windows.Forms.ColumnHeader colSenderProfile;
        private System.Windows.Forms.ColumnHeader colSubject;
        private System.Windows.Forms.ColumnHeader colRecipients;
    }
}