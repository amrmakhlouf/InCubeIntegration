namespace InCubeIntegration_UI
{
    partial class frmLoadRequestImport
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
            this.lsvContents = new System.Windows.Forms.ListView();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.txtExcelPath = new System.Windows.Forms.RichTextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnImport = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Excel Path";
            // 
            // lsvContents
            // 
            this.lsvContents.Location = new System.Drawing.Point(75, 61);
            this.lsvContents.Name = "lsvContents";
            this.lsvContents.Size = new System.Drawing.Size(700, 366);
            this.lsvContents.TabIndex = 1;
            this.lsvContents.UseCompatibleStateImageBehavior = false;
            this.lsvContents.View = System.Windows.Forms.View.Details;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(781, 6);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(78, 49);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "Browse";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // txtExcelPath
            // 
            this.txtExcelPath.Location = new System.Drawing.Point(75, 6);
            this.txtExcelPath.Name = "txtExcelPath";
            this.txtExcelPath.ReadOnly = true;
            this.txtExcelPath.Size = new System.Drawing.Size(700, 49);
            this.txtExcelPath.TabIndex = 3;
            this.txtExcelPath.Text = "";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 61);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Contents";
            // 
            // btnImport
            // 
            this.btnImport.Enabled = false;
            this.btnImport.Location = new System.Drawing.Point(781, 378);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(78, 49);
            this.btnImport.TabIndex = 5;
            this.btnImport.Text = "Import";
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
            // 
            // frmLoadRequestImport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(194)))), ((int)(((byte)(217)))), ((int)(((byte)(247)))));
            this.ClientSize = new System.Drawing.Size(866, 429);
            this.Controls.Add(this.btnImport);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtExcelPath);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.lsvContents);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "frmLoadRequestImport";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Load Request Import";
            this.Load += new System.EventHandler(this.frmLoadRequestImport_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListView lsvContents;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.RichTextBox txtExcelPath;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnImport;
    }
}