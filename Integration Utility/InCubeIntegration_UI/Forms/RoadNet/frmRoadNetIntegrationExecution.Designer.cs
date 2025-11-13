namespace InCubeIntegration_UI
{
    partial class frmRoadNetIntegrationExecution
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
            this.txtResults = new System.Windows.Forms.RichTextBox();
            this.dgvImportResults = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dgvImportResults)).BeginInit();
            this.SuspendLayout();
            // 
            // txtResults
            // 
            this.txtResults.Location = new System.Drawing.Point(0, 0);
            this.txtResults.Name = "txtResults";
            this.txtResults.ReadOnly = true;
            this.txtResults.Size = new System.Drawing.Size(657, 116);
            this.txtResults.TabIndex = 0;
            this.txtResults.Text = "";
            // 
            // dgvImportResults
            // 
            this.dgvImportResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvImportResults.Location = new System.Drawing.Point(0, 122);
            this.dgvImportResults.Name = "dgvImportResults";
            this.dgvImportResults.Size = new System.Drawing.Size(657, 282);
            this.dgvImportResults.TabIndex = 1;
            // 
            // frmRoadNetIntegrationExecution
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(657, 406);
            this.Controls.Add(this.dgvImportResults);
            this.Controls.Add(this.txtResults);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "frmRoadNetIntegrationExecution";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "frmRoadNetIntegrationExecution";
            this.Load += new System.EventHandler(this.frmRoadNetIntegrationExecution_Load);
            this.Shown += new System.EventHandler(this.frmRoadNetIntegrationExecution_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.dgvImportResults)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox txtResults;
        private System.Windows.Forms.DataGridView dgvImportResults;
    }
}