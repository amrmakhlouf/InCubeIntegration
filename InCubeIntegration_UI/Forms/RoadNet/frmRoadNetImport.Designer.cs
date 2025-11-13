namespace InCubeIntegration_UI
{
    partial class frmRoadNetImport
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
            this.cmbSessions = new System.Windows.Forms.ComboBox();
            this.pnlImport = new System.Windows.Forms.Panel();
            this.cmbRegion = new System.Windows.Forms.ComboBox();
            this.btnImport = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.dtpSessionDate = new System.Windows.Forms.DateTimePicker();
            this.cbAllSessions = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.pnlExport = new System.Windows.Forms.Panel();
            this.txtSessionName = new System.Windows.Forms.TextBox();
            this.cbNewSession = new System.Windows.Forms.CheckBox();
            this.btnContinue = new System.Windows.Forms.Button();
            this.btnAddSession = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.cmbSonicSessions = new System.Windows.Forms.ComboBox();
            this.pnlImport.SuspendLayout();
            this.pnlExport.SuspendLayout();
            this.SuspendLayout();
            // 
            // cmbSessions
            // 
            this.cmbSessions.FormattingEnabled = true;
            this.cmbSessions.Location = new System.Drawing.Point(65, 39);
            this.cmbSessions.Name = "cmbSessions";
            this.cmbSessions.Size = new System.Drawing.Size(200, 21);
            this.cmbSessions.TabIndex = 2;
            // 
            // pnlImport
            // 
            this.pnlImport.Controls.Add(this.cmbRegion);
            this.pnlImport.Controls.Add(this.btnImport);
            this.pnlImport.Controls.Add(this.label3);
            this.pnlImport.Controls.Add(this.label2);
            this.pnlImport.Controls.Add(this.dtpSessionDate);
            this.pnlImport.Controls.Add(this.cbAllSessions);
            this.pnlImport.Controls.Add(this.label1);
            this.pnlImport.Controls.Add(this.cmbSessions);
            this.pnlImport.Location = new System.Drawing.Point(12, 7);
            this.pnlImport.Name = "pnlImport";
            this.pnlImport.Size = new System.Drawing.Size(508, 128);
            this.pnlImport.TabIndex = 3;
            // 
            // cmbRegion
            // 
            this.cmbRegion.FormattingEnabled = true;
            this.cmbRegion.Location = new System.Drawing.Point(322, 39);
            this.cmbRegion.Name = "cmbRegion";
            this.cmbRegion.Size = new System.Drawing.Size(130, 21);
            this.cmbRegion.TabIndex = 9;
            // 
            // btnImport
            // 
            this.btnImport.Location = new System.Drawing.Point(417, 76);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(75, 23);
            this.btnImport.TabIndex = 4;
            this.btnImport.Text = "Import";
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(30, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Date";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(275, 42);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Region";
            // 
            // dtpSessionDate
            // 
            this.dtpSessionDate.Location = new System.Drawing.Point(65, 10);
            this.dtpSessionDate.Name = "dtpSessionDate";
            this.dtpSessionDate.Size = new System.Drawing.Size(200, 20);
            this.dtpSessionDate.TabIndex = 5;
            this.dtpSessionDate.ValueChanged += new System.EventHandler(this.dtpSessionDate_ValueChanged);
            // 
            // cbAllSessions
            // 
            this.cbAllSessions.AutoSize = true;
            this.cbAllSessions.Location = new System.Drawing.Point(458, 43);
            this.cbAllSessions.Name = "cbAllSessions";
            this.cbAllSessions.Size = new System.Drawing.Size(37, 17);
            this.cbAllSessions.TabIndex = 4;
            this.cbAllSessions.Text = "All";
            this.cbAllSessions.UseVisualStyleBackColor = true;
            this.cbAllSessions.CheckedChanged += new System.EventHandler(this.cbAllSessions_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 42);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Session";
            // 
            // pnlExport
            // 
            this.pnlExport.Controls.Add(this.txtSessionName);
            this.pnlExport.Controls.Add(this.cbNewSession);
            this.pnlExport.Controls.Add(this.btnContinue);
            this.pnlExport.Controls.Add(this.btnAddSession);
            this.pnlExport.Controls.Add(this.label4);
            this.pnlExport.Controls.Add(this.cmbSonicSessions);
            this.pnlExport.Location = new System.Drawing.Point(12, 44);
            this.pnlExport.Name = "pnlExport";
            this.pnlExport.Size = new System.Drawing.Size(508, 128);
            this.pnlExport.TabIndex = 10;
            this.pnlExport.Visible = false;
            // 
            // txtSessionName
            // 
            this.txtSessionName.Enabled = false;
            this.txtSessionName.Location = new System.Drawing.Point(132, 69);
            this.txtSessionName.Name = "txtSessionName";
            this.txtSessionName.Size = new System.Drawing.Size(200, 20);
            this.txtSessionName.TabIndex = 14;
            // 
            // cbNewSession
            // 
            this.cbNewSession.AutoSize = true;
            this.cbNewSession.Location = new System.Drawing.Point(132, 46);
            this.cbNewSession.Name = "cbNewSession";
            this.cbNewSession.Size = new System.Drawing.Size(88, 17);
            this.cbNewSession.TabIndex = 13;
            this.cbNewSession.Text = "New Session";
            this.cbNewSession.UseVisualStyleBackColor = true;
            this.cbNewSession.CheckedChanged += new System.EventHandler(this.cbNewSession_CheckedChanged);
            // 
            // btnContinue
            // 
            this.btnContinue.Location = new System.Drawing.Point(417, 19);
            this.btnContinue.Name = "btnContinue";
            this.btnContinue.Size = new System.Drawing.Size(75, 23);
            this.btnContinue.TabIndex = 12;
            this.btnContinue.Text = "Continue";
            this.btnContinue.UseVisualStyleBackColor = true;
            this.btnContinue.Click += new System.EventHandler(this.btnContinue_Click);
            // 
            // btnAddSession
            // 
            this.btnAddSession.Enabled = false;
            this.btnAddSession.Location = new System.Drawing.Point(338, 67);
            this.btnAddSession.Name = "btnAddSession";
            this.btnAddSession.Size = new System.Drawing.Size(75, 23);
            this.btnAddSession.TabIndex = 11;
            this.btnAddSession.Text = "Add";
            this.btnAddSession.UseVisualStyleBackColor = true;
            this.btnAddSession.Click += new System.EventHandler(this.btnAddSession_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(15, 22);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(108, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "Sonic Orders Session";
            // 
            // cmbSonicSessions
            // 
            this.cmbSonicSessions.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSonicSessions.FormattingEnabled = true;
            this.cmbSonicSessions.Location = new System.Drawing.Point(132, 19);
            this.cmbSonicSessions.Name = "cmbSonicSessions";
            this.cmbSonicSessions.Size = new System.Drawing.Size(200, 21);
            this.cmbSonicSessions.TabIndex = 4;
            // 
            // frmRoadNetImport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(530, 140);
            this.Controls.Add(this.pnlImport);
            this.Controls.Add(this.pnlExport);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "frmRoadNetImport";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Import from RoadNet";
            this.Load += new System.EventHandler(this.frmRoadNetImport_Load);
            this.pnlImport.ResumeLayout(false);
            this.pnlImport.PerformLayout();
            this.pnlExport.ResumeLayout(false);
            this.pnlExport.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ComboBox cmbSessions;
        private System.Windows.Forms.Panel pnlImport;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DateTimePicker dtpSessionDate;
        private System.Windows.Forms.CheckBox cbAllSessions;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cmbRegion;
        private System.Windows.Forms.Panel pnlExport;
        private System.Windows.Forms.Button btnContinue;
        private System.Windows.Forms.Button btnAddSession;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cmbSonicSessions;
        private System.Windows.Forms.TextBox txtSessionName;
        private System.Windows.Forms.CheckBox cbNewSession;
    }
}