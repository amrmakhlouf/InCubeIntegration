namespace InCubeIntegration_UI
{
    partial class frmUserAccess
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmUserAccess));
            this.trvPrivileges = new System.Windows.Forms.TreeView();
            this.cmbUserCode = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbUserName = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // trvPrivileges
            // 
            this.trvPrivileges.CheckBoxes = true;
            this.trvPrivileges.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.trvPrivileges.Location = new System.Drawing.Point(12, 66);
            this.trvPrivileges.Name = "trvPrivileges";
            this.trvPrivileges.Size = new System.Drawing.Size(336, 347);
            this.trvPrivileges.TabIndex = 0;
            this.trvPrivileges.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.trvPrivileges_AfterCheck);
            this.trvPrivileges.KeyDown += new System.Windows.Forms.KeyEventHandler(this.trvPrivileges_KeyDown);
            this.trvPrivileges.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.trvPrivileges_KeyPress);
            this.trvPrivileges.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.trvPrivileges_PreviewKeyDown);
            // 
            // cmbUserCode
            // 
            this.cmbUserCode.FormattingEnabled = true;
            this.cmbUserCode.Location = new System.Drawing.Point(80, 12);
            this.cmbUserCode.Name = "cmbUserCode";
            this.cmbUserCode.Size = new System.Drawing.Size(268, 21);
            this.cmbUserCode.TabIndex = 3;
            this.cmbUserCode.SelectedIndexChanged += new System.EventHandler(this.cmbUserCode_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 15);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Privileges of";
            // 
            // cmbUserName
            // 
            this.cmbUserName.FormattingEnabled = true;
            this.cmbUserName.Location = new System.Drawing.Point(12, 36);
            this.cmbUserName.Name = "cmbUserName";
            this.cmbUserName.Size = new System.Drawing.Size(336, 21);
            this.cmbUserName.TabIndex = 6;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(354, 66);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(161, 347);
            this.label1.TabIndex = 7;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // frmUserAccess
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(194)))), ((int)(((byte)(217)))), ((int)(((byte)(247)))));
            this.ClientSize = new System.Drawing.Size(527, 425);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmbUserName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cmbUserCode);
            this.Controls.Add(this.trvPrivileges);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "frmUserAccess";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Users Privileges";
            this.Load += new System.EventHandler(this.frmUserAccess_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView trvPrivileges;
        private System.Windows.Forms.ComboBox cmbUserCode;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbUserName;
        private System.Windows.Forms.Label label1;
    }
}