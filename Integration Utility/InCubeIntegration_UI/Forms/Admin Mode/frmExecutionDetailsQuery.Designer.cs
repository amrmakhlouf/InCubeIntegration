namespace InCubeIntegration_UI
{
    partial class frmExecutionDetailsQuery
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
            this.txtExecQuery = new System.Windows.Forms.RichTextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtExecQuery
            // 
            this.txtExecQuery.Location = new System.Drawing.Point(12, 12);
            this.txtExecQuery.Name = "txtExecQuery";
            this.txtExecQuery.Size = new System.Drawing.Size(353, 189);
            this.txtExecQuery.TabIndex = 0;
            this.txtExecQuery.Text = "";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(290, 219);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // frmExecutionDetailsQuery
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(446, 366);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.txtExecQuery);
            this.Name = "frmExecutionDetailsQuery";
            this.Text = "frmExecutionDetailsQuery";
            this.Load += new System.EventHandler(this.frmExecutionDetailsQuery_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox txtExecQuery;
        private System.Windows.Forms.Button button1;
    }
}