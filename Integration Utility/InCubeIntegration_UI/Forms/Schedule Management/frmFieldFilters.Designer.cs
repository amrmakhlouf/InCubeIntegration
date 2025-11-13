namespace InCubeIntegration_UI
{
    partial class frmFieldFilters
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
            this.btnPlusMinusFrom = new System.Windows.Forms.Button();
            this.nudFrom = new System.Windows.Forms.NumericUpDown();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.nudTo = new System.Windows.Forms.NumericUpDown();
            this.btnPlusMinusTo = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.pnlFromToDateFilters = new System.Windows.Forms.Panel();
            this.tcFilters = new System.Windows.Forms.TabControl();
            this.pnlCheckListFilter = new System.Windows.Forms.Panel();
            this.cbAllOptions = new System.Windows.Forms.CheckBox();
            this.lsvOptions = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.pnlDate = new System.Windows.Forms.Panel();
            this.label5 = new System.Windows.Forms.Label();
            this.btnPlusMinusOn = new System.Windows.Forms.Button();
            this.nudOn = new System.Windows.Forms.NumericUpDown();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nudFrom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTo)).BeginInit();
            this.pnlFromToDateFilters.SuspendLayout();
            this.pnlCheckListFilter.SuspendLayout();
            this.pnlDate.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudOn)).BeginInit();
            this.SuspendLayout();
            // 
            // btnPlusMinusFrom
            // 
            this.btnPlusMinusFrom.Location = new System.Drawing.Point(110, 13);
            this.btnPlusMinusFrom.Name = "btnPlusMinusFrom";
            this.btnPlusMinusFrom.Size = new System.Drawing.Size(30, 23);
            this.btnPlusMinusFrom.TabIndex = 0;
            this.btnPlusMinusFrom.Text = "-";
            this.btnPlusMinusFrom.UseVisualStyleBackColor = true;
            this.btnPlusMinusFrom.Click += new System.EventHandler(this.btnPlusMinusFrom_Click);
            // 
            // nudFrom
            // 
            this.nudFrom.Location = new System.Drawing.Point(146, 15);
            this.nudFrom.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nudFrom.Name = "nudFrom";
            this.nudFrom.Size = new System.Drawing.Size(48, 20);
            this.nudFrom.TabIndex = 1;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(48, 15);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(56, 20);
            this.textBox1.TabIndex = 2;
            this.textBox1.Text = "Today";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(30, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "From";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(200, 18);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(31, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Days";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(200, 53);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(31, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Days";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(11, 53);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(20, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "To";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(48, 50);
            this.textBox2.Name = "textBox2";
            this.textBox2.ReadOnly = true;
            this.textBox2.Size = new System.Drawing.Size(56, 20);
            this.textBox2.TabIndex = 7;
            this.textBox2.Text = "Today";
            // 
            // nudTo
            // 
            this.nudTo.Location = new System.Drawing.Point(146, 50);
            this.nudTo.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nudTo.Name = "nudTo";
            this.nudTo.Size = new System.Drawing.Size(48, 20);
            this.nudTo.TabIndex = 6;
            // 
            // btnPlusMinusTo
            // 
            this.btnPlusMinusTo.Location = new System.Drawing.Point(110, 48);
            this.btnPlusMinusTo.Name = "btnPlusMinusTo";
            this.btnPlusMinusTo.Size = new System.Drawing.Size(30, 23);
            this.btnPlusMinusTo.TabIndex = 5;
            this.btnPlusMinusTo.Text = "+";
            this.btnPlusMinusTo.UseVisualStyleBackColor = true;
            this.btnPlusMinusTo.Click += new System.EventHandler(this.btnPlusMinusTo_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(203, 306);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(77, 23);
            this.btnSave.TabIndex = 10;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // pnlFromToDateFilters
            // 
            this.pnlFromToDateFilters.Controls.Add(this.label1);
            this.pnlFromToDateFilters.Controls.Add(this.btnPlusMinusFrom);
            this.pnlFromToDateFilters.Controls.Add(this.label3);
            this.pnlFromToDateFilters.Controls.Add(this.nudFrom);
            this.pnlFromToDateFilters.Controls.Add(this.label4);
            this.pnlFromToDateFilters.Controls.Add(this.textBox1);
            this.pnlFromToDateFilters.Controls.Add(this.textBox2);
            this.pnlFromToDateFilters.Controls.Add(this.label2);
            this.pnlFromToDateFilters.Controls.Add(this.nudTo);
            this.pnlFromToDateFilters.Controls.Add(this.btnPlusMinusTo);
            this.pnlFromToDateFilters.Location = new System.Drawing.Point(287, 213);
            this.pnlFromToDateFilters.Name = "pnlFromToDateFilters";
            this.pnlFromToDateFilters.Size = new System.Drawing.Size(280, 134);
            this.pnlFromToDateFilters.TabIndex = 11;
            this.pnlFromToDateFilters.Visible = false;
            // 
            // tcFilters
            // 
            this.tcFilters.Location = new System.Drawing.Point(0, 12);
            this.tcFilters.Name = "tcFilters";
            this.tcFilters.SelectedIndex = 0;
            this.tcFilters.Size = new System.Drawing.Size(280, 288);
            this.tcFilters.TabIndex = 13;
            // 
            // pnlCheckListFilter
            // 
            this.pnlCheckListFilter.Controls.Add(this.cbAllOptions);
            this.pnlCheckListFilter.Controls.Add(this.lsvOptions);
            this.pnlCheckListFilter.Location = new System.Drawing.Point(573, 11);
            this.pnlCheckListFilter.Name = "pnlCheckListFilter";
            this.pnlCheckListFilter.Size = new System.Drawing.Size(280, 195);
            this.pnlCheckListFilter.TabIndex = 14;
            this.pnlCheckListFilter.Visible = false;
            // 
            // cbAllOptions
            // 
            this.cbAllOptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.cbAllOptions.AutoSize = true;
            this.cbAllOptions.Location = new System.Drawing.Point(4, 3);
            this.cbAllOptions.Name = "cbAllOptions";
            this.cbAllOptions.Size = new System.Drawing.Size(37, 17);
            this.cbAllOptions.TabIndex = 15;
            this.cbAllOptions.Text = "All";
            this.cbAllOptions.UseVisualStyleBackColor = true;
            this.cbAllOptions.CheckedChanged += new System.EventHandler(this.cbAllOptions_CheckedChanged);
            // 
            // lsvOptions
            // 
            this.lsvOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lsvOptions.CheckBoxes = true;
            this.lsvOptions.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader4,
            this.columnHeader3});
            this.lsvOptions.Location = new System.Drawing.Point(0, 21);
            this.lsvOptions.MultiSelect = false;
            this.lsvOptions.Name = "lsvOptions";
            this.lsvOptions.Size = new System.Drawing.Size(280, 173);
            this.lsvOptions.TabIndex = 14;
            this.lsvOptions.UseCompatibleStateImageBehavior = false;
            this.lsvOptions.View = System.Windows.Forms.View.Details;
            this.lsvOptions.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.lsvOptions_ItemChecked);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "";
            this.columnHeader1.Width = 20;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "ID";
            this.columnHeader4.Width = 27;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Name";
            this.columnHeader3.Width = 185;
            // 
            // pnlDate
            // 
            this.pnlDate.Controls.Add(this.label5);
            this.pnlDate.Controls.Add(this.btnPlusMinusOn);
            this.pnlDate.Controls.Add(this.nudOn);
            this.pnlDate.Controls.Add(this.textBox3);
            this.pnlDate.Controls.Add(this.label8);
            this.pnlDate.Location = new System.Drawing.Point(573, 212);
            this.pnlDate.Name = "pnlDate";
            this.pnlDate.Size = new System.Drawing.Size(280, 134);
            this.pnlDate.TabIndex = 12;
            this.pnlDate.Visible = false;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(11, 18);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(21, 13);
            this.label5.TabIndex = 3;
            this.label5.Text = "On";
            // 
            // btnPlusMinusOn
            // 
            this.btnPlusMinusOn.Location = new System.Drawing.Point(110, 13);
            this.btnPlusMinusOn.Name = "btnPlusMinusOn";
            this.btnPlusMinusOn.Size = new System.Drawing.Size(30, 23);
            this.btnPlusMinusOn.TabIndex = 0;
            this.btnPlusMinusOn.Text = "-";
            this.btnPlusMinusOn.UseVisualStyleBackColor = true;
            this.btnPlusMinusOn.Click += new System.EventHandler(this.btnPlusMinusOn_Click);
            // 
            // nudOn
            // 
            this.nudOn.Location = new System.Drawing.Point(146, 15);
            this.nudOn.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nudOn.Name = "nudOn";
            this.nudOn.Size = new System.Drawing.Size(48, 20);
            this.nudOn.TabIndex = 1;
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(48, 15);
            this.textBox3.Name = "textBox3";
            this.textBox3.ReadOnly = true;
            this.textBox3.Size = new System.Drawing.Size(56, 20);
            this.textBox3.TabIndex = 2;
            this.textBox3.Text = "Today";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(200, 18);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(31, 13);
            this.label8.TabIndex = 4;
            this.label8.Text = "Days";
            // 
            // frmFieldFilters
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(194)))), ((int)(((byte)(217)))), ((int)(((byte)(247)))));
            this.ClientSize = new System.Drawing.Size(919, 331);
            this.Controls.Add(this.pnlDate);
            this.Controls.Add(this.pnlCheckListFilter);
            this.Controls.Add(this.tcFilters);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.pnlFromToDateFilters);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmFieldFilters";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Action Filters";
            this.Load += new System.EventHandler(this.frmFieldFilters_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nudFrom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTo)).EndInit();
            this.pnlFromToDateFilters.ResumeLayout(false);
            this.pnlFromToDateFilters.PerformLayout();
            this.pnlCheckListFilter.ResumeLayout(false);
            this.pnlCheckListFilter.PerformLayout();
            this.pnlDate.ResumeLayout(false);
            this.pnlDate.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudOn)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnPlusMinusFrom;
        private System.Windows.Forms.NumericUpDown nudFrom;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.NumericUpDown nudTo;
        private System.Windows.Forms.Button btnPlusMinusTo;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Panel pnlFromToDateFilters;
        private System.Windows.Forms.TabControl tcFilters;
        private System.Windows.Forms.Panel pnlCheckListFilter;
        private System.Windows.Forms.CheckBox cbAllOptions;
        private System.Windows.Forms.ListView lsvOptions;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.Panel pnlDate;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnPlusMinusOn;
        private System.Windows.Forms.NumericUpDown nudOn;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Label label8;
    }
}