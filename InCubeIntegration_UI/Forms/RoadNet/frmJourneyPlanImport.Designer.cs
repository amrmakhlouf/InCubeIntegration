namespace InCubeIntegration_UI
{
    partial class frmJourneyPlanImport
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
            this.cmbRegion = new System.Windows.Forms.ComboBox();
            this.btnRetrieve = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbSessions = new System.Windows.Forms.ComboBox();
            this.grdJourneyPlan = new System.Windows.Forms.DataGridView();
            this.btnImport = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.grdJourneyPlan)).BeginInit();
            this.SuspendLayout();
            // 
            // cmbRegion
            // 
            this.cmbRegion.FormattingEnabled = true;
            this.cmbRegion.Location = new System.Drawing.Point(60, 10);
            this.cmbRegion.Name = "cmbRegion";
            this.cmbRegion.Size = new System.Drawing.Size(200, 21);
            this.cmbRegion.TabIndex = 15;
            this.cmbRegion.SelectedIndexChanged += new System.EventHandler(this.cmbRegion_SelectedIndexChanged);
            // 
            // btnRetrieve
            // 
            this.btnRetrieve.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRetrieve.Location = new System.Drawing.Point(822, 10);
            this.btnRetrieve.Name = "btnRetrieve";
            this.btnRetrieve.Size = new System.Drawing.Size(75, 48);
            this.btnRetrieve.TabIndex = 12;
            this.btnRetrieve.Text = "Retrieve";
            this.btnRetrieve.UseVisualStyleBackColor = true;
            this.btnRetrieve.Click += new System.EventHandler(this.btnRetrieve_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 13);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 14;
            this.label2.Text = "Region";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 40);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "Session";
            // 
            // cmbSessions
            // 
            this.cmbSessions.FormattingEnabled = true;
            this.cmbSessions.Location = new System.Drawing.Point(60, 37);
            this.cmbSessions.Name = "cmbSessions";
            this.cmbSessions.Size = new System.Drawing.Size(200, 21);
            this.cmbSessions.TabIndex = 10;
            // 
            // grdJourneyPlan
            // 
            this.grdJourneyPlan.AllowUserToAddRows = false;
            this.grdJourneyPlan.AllowUserToDeleteRows = false;
            this.grdJourneyPlan.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grdJourneyPlan.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.grdJourneyPlan.BackgroundColor = System.Drawing.Color.White;
            this.grdJourneyPlan.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdJourneyPlan.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grdJourneyPlan.Location = new System.Drawing.Point(13, 75);
            this.grdJourneyPlan.Name = "grdJourneyPlan";
            this.grdJourneyPlan.Size = new System.Drawing.Size(884, 413);
            this.grdJourneyPlan.TabIndex = 16;
            // 
            // btnImport
            // 
            this.btnImport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnImport.Enabled = false;
            this.btnImport.Location = new System.Drawing.Point(822, 494);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(75, 27);
            this.btnImport.TabIndex = 17;
            this.btnImport.Text = "Import";
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
            // 
            // frmJourneyPlanImport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(904, 527);
            this.Controls.Add(this.btnImport);
            this.Controls.Add(this.grdJourneyPlan);
            this.Controls.Add(this.cmbRegion);
            this.Controls.Add(this.btnRetrieve);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmbSessions);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "frmJourneyPlanImport";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Journey Plan Import";
            this.Load += new System.EventHandler(this.frmJourneyPlanImport_Load);
            ((System.ComponentModel.ISupportInitialize)(this.grdJourneyPlan)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbRegion;
        private System.Windows.Forms.Button btnRetrieve;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbSessions;
        private System.Windows.Forms.DataGridView grdJourneyPlan;
        private System.Windows.Forms.Button btnImport;
    }
}