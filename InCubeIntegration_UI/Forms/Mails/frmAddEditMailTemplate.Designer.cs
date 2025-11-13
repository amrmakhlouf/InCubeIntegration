namespace InCubeIntegration_UI
{
    partial class frmAddEditMailTemplate
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
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.txtSubject = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtID = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.cmbSenderProfile = new System.Windows.Forms.ComboBox();
            this.txtHeader = new System.Windows.Forms.RichTextBox();
            this.txtFooter = new System.Windows.Forms.RichTextBox();
            this.txtTo = new System.Windows.Forms.RichTextBox();
            this.txtCC = new System.Windows.Forms.RichTextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(9, 235);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(20, 13);
            this.label9.TabIndex = 50;
            this.label9.Text = "To";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(9, 175);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(37, 13);
            this.label8.TabIndex = 49;
            this.label8.Text = "Footer";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(9, 115);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(42, 13);
            this.label7.TabIndex = 48;
            this.label7.Text = "Header";
            // 
            // txtSubject
            // 
            this.txtSubject.Location = new System.Drawing.Point(93, 86);
            this.txtSubject.Name = "txtSubject";
            this.txtSubject.Size = new System.Drawing.Size(194, 20);
            this.txtSubject.TabIndex = 3;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(9, 89);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(43, 13);
            this.label6.TabIndex = 47;
            this.label6.Text = "Subject";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(9, 63);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(73, 13);
            this.label5.TabIndex = 46;
            this.label5.Text = "Sender Profile";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(93, 34);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(194, 20);
            this.txtName.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 37);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 45;
            this.label2.Text = "Name";
            // 
            // txtID
            // 
            this.txtID.Location = new System.Drawing.Point(93, 8);
            this.txtID.Name = "txtID";
            this.txtID.ReadOnly = true;
            this.txtID.Size = new System.Drawing.Size(65, 20);
            this.txtID.TabIndex = 44;
            this.txtID.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 11);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(18, 13);
            this.label1.TabIndex = 43;
            this.label1.Text = "ID";
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(212, 352);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 8;
            this.btnSave.Text = "Save";
            this.btnSave.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // cmbSenderProfile
            // 
            this.cmbSenderProfile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSenderProfile.FormattingEnabled = true;
            this.cmbSenderProfile.Location = new System.Drawing.Point(93, 60);
            this.cmbSenderProfile.Name = "cmbSenderProfile";
            this.cmbSenderProfile.Size = new System.Drawing.Size(194, 21);
            this.cmbSenderProfile.TabIndex = 2;
            // 
            // txtHeader
            // 
            this.txtHeader.Location = new System.Drawing.Point(93, 112);
            this.txtHeader.Name = "txtHeader";
            this.txtHeader.Size = new System.Drawing.Size(194, 54);
            this.txtHeader.TabIndex = 4;
            this.txtHeader.Text = "";
            // 
            // txtFooter
            // 
            this.txtFooter.Location = new System.Drawing.Point(93, 172);
            this.txtFooter.Name = "txtFooter";
            this.txtFooter.Size = new System.Drawing.Size(194, 54);
            this.txtFooter.TabIndex = 5;
            this.txtFooter.Text = "";
            // 
            // txtTo
            // 
            this.txtTo.Location = new System.Drawing.Point(93, 232);
            this.txtTo.Name = "txtTo";
            this.txtTo.Size = new System.Drawing.Size(194, 54);
            this.txtTo.TabIndex = 6;
            this.txtTo.Text = "";
            // 
            // txtCC
            // 
            this.txtCC.Location = new System.Drawing.Point(93, 292);
            this.txtCC.Name = "txtCC";
            this.txtCC.Size = new System.Drawing.Size(194, 54);
            this.txtCC.TabIndex = 7;
            this.txtCC.Text = "";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 295);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(21, 13);
            this.label3.TabIndex = 55;
            this.label3.Text = "CC";
            // 
            // frmAddEditMailTemplate
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 378);
            this.Controls.Add(this.txtCC);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtTo);
            this.Controls.Add(this.txtFooter);
            this.Controls.Add(this.txtHeader);
            this.Controls.Add(this.cmbSenderProfile);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.txtSubject);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtID);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnSave);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "frmAddEditMailTemplate";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add/Edit Mail Template";
            this.Load += new System.EventHandler(this.frmAddEditMailTemplate_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtSubject;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtID;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.ComboBox cmbSenderProfile;
        private System.Windows.Forms.RichTextBox txtHeader;
        private System.Windows.Forms.RichTextBox txtFooter;
        private System.Windows.Forms.RichTextBox txtTo;
        private System.Windows.Forms.RichTextBox txtCC;
        private System.Windows.Forms.Label label3;
    }
}