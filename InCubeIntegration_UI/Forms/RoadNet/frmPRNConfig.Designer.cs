namespace InCubeIntegration_UI
{
    partial class frmPRNConfig
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
            this.picUp = new System.Windows.Forms.PictureBox();
            this.PicDown = new System.Windows.Forms.PictureBox();
            this.btnInclude = new System.Windows.Forms.Button();
            this.btnExclude = new System.Windows.Forms.Button();
            this.grdIncludedColumns = new System.Windows.Forms.DataGridView();
            this.colColumnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPosition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colWidth = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lbExcludedColumns = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtSample = new System.Windows.Forms.RichTextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.pnl1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.txtOrderID = new System.Windows.Forms.TextBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.pnl2 = new System.Windows.Forms.Panel();
            this.cbWrapText = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.picUp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PicDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grdIncludedColumns)).BeginInit();
            this.pnl1.SuspendLayout();
            this.pnl2.SuspendLayout();
            this.SuspendLayout();
            // 
            // picUp
            // 
            this.picUp.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picUp.Image = global::InCubeIntegration_UI.Properties.Resources.up;
            this.picUp.Location = new System.Drawing.Point(3, 101);
            this.picUp.Name = "picUp";
            this.picUp.Size = new System.Drawing.Size(40, 40);
            this.picUp.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picUp.TabIndex = 43;
            this.picUp.TabStop = false;
            this.picUp.Click += new System.EventHandler(this.picUp_Click);
            // 
            // PicDown
            // 
            this.PicDown.Cursor = System.Windows.Forms.Cursors.Hand;
            this.PicDown.Image = global::InCubeIntegration_UI.Properties.Resources.down;
            this.PicDown.Location = new System.Drawing.Point(3, 147);
            this.PicDown.Name = "PicDown";
            this.PicDown.Size = new System.Drawing.Size(40, 40);
            this.PicDown.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.PicDown.TabIndex = 44;
            this.PicDown.TabStop = false;
            this.PicDown.Click += new System.EventHandler(this.PicDown_Click);
            // 
            // btnInclude
            // 
            this.btnInclude.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnInclude.Location = new System.Drawing.Point(380, 115);
            this.btnInclude.Name = "btnInclude";
            this.btnInclude.Size = new System.Drawing.Size(35, 26);
            this.btnInclude.TabIndex = 45;
            this.btnInclude.Text = "<";
            this.btnInclude.UseVisualStyleBackColor = true;
            this.btnInclude.Click += new System.EventHandler(this.btnInclude_Click);
            // 
            // btnExclude
            // 
            this.btnExclude.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExclude.Location = new System.Drawing.Point(380, 147);
            this.btnExclude.Name = "btnExclude";
            this.btnExclude.Size = new System.Drawing.Size(35, 26);
            this.btnExclude.TabIndex = 42;
            this.btnExclude.Text = ">";
            this.btnExclude.UseVisualStyleBackColor = true;
            this.btnExclude.Click += new System.EventHandler(this.btnExclude_Click);
            // 
            // grdIncludedColumns
            // 
            this.grdIncludedColumns.AllowUserToAddRows = false;
            this.grdIncludedColumns.AllowUserToDeleteRows = false;
            this.grdIncludedColumns.AllowUserToResizeRows = false;
            this.grdIncludedColumns.BackgroundColor = System.Drawing.Color.White;
            this.grdIncludedColumns.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdIncludedColumns.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colColumnName,
            this.colPosition,
            this.colWidth});
            this.grdIncludedColumns.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.grdIncludedColumns.Location = new System.Drawing.Point(53, 3);
            this.grdIncludedColumns.MultiSelect = false;
            this.grdIncludedColumns.Name = "grdIncludedColumns";
            this.grdIncludedColumns.RowHeadersVisible = false;
            this.grdIncludedColumns.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdIncludedColumns.Size = new System.Drawing.Size(322, 300);
            this.grdIncludedColumns.TabIndex = 46;
            this.grdIncludedColumns.CurrentCellDirtyStateChanged += new System.EventHandler(this.grdIncludedColumns_CurrentCellDirtyStateChanged);
            this.grdIncludedColumns.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.grdIncludedColumns_EditingControlShowing);
            // 
            // colColumnName
            // 
            this.colColumnName.DataPropertyName = "ColumnName";
            this.colColumnName.HeaderText = "Column Name";
            this.colColumnName.Name = "colColumnName";
            this.colColumnName.ReadOnly = true;
            this.colColumnName.Width = 150;
            // 
            // colPosition
            // 
            this.colPosition.DataPropertyName = "Position";
            this.colPosition.HeaderText = "Position";
            this.colPosition.Name = "colPosition";
            this.colPosition.ReadOnly = true;
            this.colPosition.Width = 60;
            // 
            // colWidth
            // 
            this.colWidth.DataPropertyName = "Width";
            this.colWidth.HeaderText = "Width";
            this.colWidth.Name = "colWidth";
            this.colWidth.Width = 60;
            // 
            // lbExcludedColumns
            // 
            this.lbExcludedColumns.FormattingEnabled = true;
            this.lbExcludedColumns.Location = new System.Drawing.Point(422, 3);
            this.lbExcludedColumns.Name = "lbExcludedColumns";
            this.lbExcludedColumns.Size = new System.Drawing.Size(190, 303);
            this.lbExcludedColumns.TabIndex = 47;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 315);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(42, 13);
            this.label1.TabIndex = 48;
            this.label1.Text = "Sample";
            // 
            // txtSample
            // 
            this.txtSample.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSample.Location = new System.Drawing.Point(53, 312);
            this.txtSample.Name = "txtSample";
            this.txtSample.ReadOnly = true;
            this.txtSample.Size = new System.Drawing.Size(559, 156);
            this.txtSample.TabIndex = 49;
            this.txtSample.Text = "";
            // 
            // btnSave
            // 
            this.btnSave.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSave.Location = new System.Drawing.Point(533, 474);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(79, 26);
            this.btnSave.TabIndex = 50;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // pnl1
            // 
            this.pnl1.Controls.Add(this.btnOk);
            this.pnl1.Controls.Add(this.txtOrderID);
            this.pnl1.Controls.Add(this.label2);
            this.pnl1.Location = new System.Drawing.Point(12, 12);
            this.pnl1.Name = "pnl1";
            this.pnl1.Size = new System.Drawing.Size(234, 121);
            this.pnl1.TabIndex = 51;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 14);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(202, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Enter valid order ID for sample generation";
            // 
            // txtOrderID
            // 
            this.txtOrderID.Location = new System.Drawing.Point(17, 43);
            this.txtOrderID.Name = "txtOrderID";
            this.txtOrderID.Size = new System.Drawing.Size(199, 20);
            this.txtOrderID.TabIndex = 1;
            this.txtOrderID.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtOrderID_KeyDown);
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(76, 69);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 2;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // pnl2
            // 
            this.pnl2.Controls.Add(this.cbWrapText);
            this.pnl2.Controls.Add(this.grdIncludedColumns);
            this.pnl2.Controls.Add(this.picUp);
            this.pnl2.Controls.Add(this.btnSave);
            this.pnl2.Controls.Add(this.PicDown);
            this.pnl2.Controls.Add(this.label1);
            this.pnl2.Controls.Add(this.txtSample);
            this.pnl2.Controls.Add(this.lbExcludedColumns);
            this.pnl2.Controls.Add(this.btnExclude);
            this.pnl2.Controls.Add(this.btnInclude);
            this.pnl2.Location = new System.Drawing.Point(12, 12);
            this.pnl2.Name = "pnl2";
            this.pnl2.Size = new System.Drawing.Size(624, 503);
            this.pnl2.TabIndex = 52;
            this.pnl2.Visible = false;
            // 
            // cbWrapText
            // 
            this.cbWrapText.AutoSize = true;
            this.cbWrapText.Checked = true;
            this.cbWrapText.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbWrapText.Location = new System.Drawing.Point(53, 474);
            this.cbWrapText.Name = "cbWrapText";
            this.cbWrapText.Size = new System.Drawing.Size(76, 17);
            this.cbWrapText.TabIndex = 51;
            this.cbWrapText.Text = "Wrap Text";
            this.cbWrapText.UseVisualStyleBackColor = true;
            this.cbWrapText.CheckedChanged += new System.EventHandler(this.cbWrapText_CheckedChanged);
            // 
            // frmPRNConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(641, 527);
            this.Controls.Add(this.pnl1);
            this.Controls.Add(this.pnl2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "frmPRNConfig";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "PRN Config";
            this.Load += new System.EventHandler(this.frmPRNConfig_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picUp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PicDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grdIncludedColumns)).EndInit();
            this.pnl1.ResumeLayout(false);
            this.pnl1.PerformLayout();
            this.pnl2.ResumeLayout(false);
            this.pnl2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox picUp;
        private System.Windows.Forms.PictureBox PicDown;
        private System.Windows.Forms.Button btnInclude;
        private System.Windows.Forms.Button btnExclude;
        private System.Windows.Forms.DataGridView grdIncludedColumns;
        private System.Windows.Forms.DataGridViewTextBoxColumn colColumnName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPosition;
        private System.Windows.Forms.DataGridViewTextBoxColumn colWidth;
        private System.Windows.Forms.ListBox lbExcludedColumns;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox txtSample;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Panel pnl1;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.TextBox txtOrderID;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel pnl2;
        private System.Windows.Forms.CheckBox cbWrapText;
    }
}