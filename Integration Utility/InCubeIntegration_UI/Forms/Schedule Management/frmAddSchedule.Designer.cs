namespace InCubeIntegration_UI
{
    partial class frmAddSchedule
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
            this.btnAdd = new System.Windows.Forms.Button();
            this.cmbType = new System.Windows.Forms.ComboBox();
            this.lblStart = new System.Windows.Forms.Label();
            this.txtStartH = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtStartM = new System.Windows.Forms.TextBox();
            this.txtEndM = new System.Windows.Forms.TextBox();
            this.txtEndH = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.lblDay = new System.Windows.Forms.Label();
            this.cmbDay = new System.Windows.Forms.ComboBox();
            this.txtPeriod = new System.Windows.Forms.TextBox();
            this.lblEvery = new System.Windows.Forms.Label();
            this.lblMinutes = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.tblControls = new System.Windows.Forms.TableLayoutPanel();
            this.tblPeriod = new System.Windows.Forms.TableLayoutPanel();
            this.tblDay = new System.Windows.Forms.TableLayoutPanel();
            this.tblEndTime = new System.Windows.Forms.TableLayoutPanel();
            this.lblEnd = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.tblStartTime = new System.Windows.Forms.TableLayoutPanel();
            this.lblResult = new System.Windows.Forms.Label();
            this.tblControls.SuspendLayout();
            this.tblPeriod.SuspendLayout();
            this.tblDay.SuspendLayout();
            this.tblEndTime.SuspendLayout();
            this.tblStartTime.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(56, 174);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(68, 28);
            this.btnAdd.TabIndex = 8;
            this.btnAdd.Text = "Add";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // cmbType
            // 
            this.cmbType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbType.FormattingEnabled = true;
            this.cmbType.Location = new System.Drawing.Point(56, 19);
            this.cmbType.Name = "cmbType";
            this.cmbType.Size = new System.Drawing.Size(222, 21);
            this.cmbType.TabIndex = 1;
            this.cmbType.SelectedIndexChanged += new System.EventHandler(this.cmbType_SelectedIndexChanged);
            // 
            // lblStart
            // 
            this.lblStart.AutoSize = true;
            this.lblStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStart.Location = new System.Drawing.Point(3, 0);
            this.lblStart.Name = "lblStart";
            this.lblStart.Size = new System.Drawing.Size(44, 24);
            this.lblStart.TabIndex = 2;
            this.lblStart.Text = "Start";
            this.lblStart.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtStartH
            // 
            this.txtStartH.Location = new System.Drawing.Point(53, 3);
            this.txtStartH.Name = "txtStartH";
            this.txtStartH.Size = new System.Drawing.Size(19, 20);
            this.txtStartH.TabIndex = 3;
            this.txtStartH.Text = "00";
            this.txtStartH.TextChanged += new System.EventHandler(this.txtStartH_TextChanged);
            this.txtStartH.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtStartH_KeyPress);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(78, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(9, 24);
            this.label2.TabIndex = 4;
            this.label2.Text = ":";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtStartM
            // 
            this.txtStartM.Location = new System.Drawing.Point(93, 3);
            this.txtStartM.Name = "txtStartM";
            this.txtStartM.Size = new System.Drawing.Size(20, 20);
            this.txtStartM.TabIndex = 4;
            this.txtStartM.Text = "00";
            this.txtStartM.TextChanged += new System.EventHandler(this.txtStartM_TextChanged);
            this.txtStartM.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtStartM_KeyPress);
            // 
            // txtEndM
            // 
            this.txtEndM.Location = new System.Drawing.Point(93, 3);
            this.txtEndM.Name = "txtEndM";
            this.txtEndM.ReadOnly = true;
            this.txtEndM.Size = new System.Drawing.Size(20, 20);
            this.txtEndM.TabIndex = 6;
            this.txtEndM.TextChanged += new System.EventHandler(this.txtEndM_TextChanged);
            this.txtEndM.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtEndM_KeyPress);
            // 
            // txtEndH
            // 
            this.txtEndH.Location = new System.Drawing.Point(53, 3);
            this.txtEndH.Name = "txtEndH";
            this.txtEndH.ReadOnly = true;
            this.txtEndH.Size = new System.Drawing.Size(19, 20);
            this.txtEndH.TabIndex = 5;
            this.txtEndH.TextChanged += new System.EventHandler(this.txtEndH_TextChanged);
            this.txtEndH.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtEndH_KeyPress);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 22);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(41, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Occurs";
            // 
            // lblDay
            // 
            this.lblDay.AutoSize = true;
            this.lblDay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDay.Location = new System.Drawing.Point(3, 0);
            this.lblDay.Name = "lblDay";
            this.lblDay.Size = new System.Drawing.Size(44, 24);
            this.lblDay.TabIndex = 12;
            this.lblDay.Text = "Day";
            this.lblDay.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmbDay
            // 
            this.cmbDay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbDay.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDay.Enabled = false;
            this.cmbDay.FormattingEnabled = true;
            this.cmbDay.Location = new System.Drawing.Point(53, 3);
            this.cmbDay.Name = "cmbDay";
            this.cmbDay.Size = new System.Drawing.Size(160, 21);
            this.cmbDay.TabIndex = 2;
            // 
            // txtPeriod
            // 
            this.txtPeriod.Location = new System.Drawing.Point(53, 3);
            this.txtPeriod.Name = "txtPeriod";
            this.txtPeriod.ReadOnly = true;
            this.txtPeriod.Size = new System.Drawing.Size(31, 20);
            this.txtPeriod.TabIndex = 7;
            this.txtPeriod.TextChanged += new System.EventHandler(this.txtPeriod_TextChanged);
            this.txtPeriod.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtPeriod_KeyPress);
            // 
            // lblEvery
            // 
            this.lblEvery.AutoSize = true;
            this.lblEvery.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEvery.Location = new System.Drawing.Point(3, 0);
            this.lblEvery.Name = "lblEvery";
            this.lblEvery.Size = new System.Drawing.Size(44, 24);
            this.lblEvery.TabIndex = 13;
            this.lblEvery.Text = "Every";
            this.lblEvery.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMinutes
            // 
            this.lblMinutes.AutoSize = true;
            this.lblMinutes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMinutes.Location = new System.Drawing.Point(91, 0);
            this.lblMinutes.Name = "lblMinutes";
            this.lblMinutes.Size = new System.Drawing.Size(122, 24);
            this.lblMinutes.TabIndex = 15;
            this.lblMinutes.Text = "minute(s)";
            this.lblMinutes.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(207, 174);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(71, 28);
            this.btnCancel.TabIndex = 9;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // tblControls
            // 
            this.tblControls.ColumnCount = 1;
            this.tblControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblControls.Controls.Add(this.tblPeriod, 0, 3);
            this.tblControls.Controls.Add(this.tblDay, 0, 0);
            this.tblControls.Controls.Add(this.tblEndTime, 0, 2);
            this.tblControls.Controls.Add(this.tblStartTime, 0, 1);
            this.tblControls.Location = new System.Drawing.Point(56, 48);
            this.tblControls.Name = "tblControls";
            this.tblControls.RowCount = 4;
            this.tblControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tblControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tblControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tblControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tblControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tblControls.Size = new System.Drawing.Size(222, 120);
            this.tblControls.TabIndex = 16;
            // 
            // tblPeriod
            // 
            this.tblPeriod.ColumnCount = 3;
            this.tblPeriod.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tblPeriod.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.tblPeriod.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this.tblPeriod.Controls.Add(this.lblEvery, 0, 0);
            this.tblPeriod.Controls.Add(this.txtPeriod, 1, 0);
            this.tblPeriod.Controls.Add(this.lblMinutes, 2, 0);
            this.tblPeriod.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblPeriod.Location = new System.Drawing.Point(3, 93);
            this.tblPeriod.Name = "tblPeriod";
            this.tblPeriod.RowCount = 1;
            this.tblPeriod.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblPeriod.Size = new System.Drawing.Size(216, 24);
            this.tblPeriod.TabIndex = 19;
            // 
            // tblDay
            // 
            this.tblDay.ColumnCount = 2;
            this.tblDay.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tblDay.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 166F));
            this.tblDay.Controls.Add(this.lblDay, 0, 0);
            this.tblDay.Controls.Add(this.cmbDay, 1, 0);
            this.tblDay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblDay.Location = new System.Drawing.Point(3, 3);
            this.tblDay.Name = "tblDay";
            this.tblDay.RowCount = 1;
            this.tblDay.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblDay.Size = new System.Drawing.Size(216, 24);
            this.tblDay.TabIndex = 20;
            // 
            // tblEndTime
            // 
            this.tblEndTime.ColumnCount = 4;
            this.tblEndTime.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tblEndTime.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tblEndTime.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 15F));
            this.tblEndTime.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 126F));
            this.tblEndTime.Controls.Add(this.lblEnd, 0, 0);
            this.tblEndTime.Controls.Add(this.label3, 2, 0);
            this.tblEndTime.Controls.Add(this.txtEndH, 1, 0);
            this.tblEndTime.Controls.Add(this.txtEndM, 3, 0);
            this.tblEndTime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblEndTime.Location = new System.Drawing.Point(3, 63);
            this.tblEndTime.Name = "tblEndTime";
            this.tblEndTime.RowCount = 1;
            this.tblEndTime.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblEndTime.Size = new System.Drawing.Size(216, 24);
            this.tblEndTime.TabIndex = 18;
            // 
            // lblEnd
            // 
            this.lblEnd.AutoSize = true;
            this.lblEnd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEnd.Location = new System.Drawing.Point(3, 0);
            this.lblEnd.Name = "lblEnd";
            this.lblEnd.Size = new System.Drawing.Size(44, 24);
            this.lblEnd.TabIndex = 2;
            this.lblEnd.Text = "End";
            this.lblEnd.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(78, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(9, 24);
            this.label3.TabIndex = 4;
            this.label3.Text = ":";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tblStartTime
            // 
            this.tblStartTime.ColumnCount = 4;
            this.tblStartTime.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tblStartTime.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tblStartTime.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 15F));
            this.tblStartTime.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 126F));
            this.tblStartTime.Controls.Add(this.lblStart, 0, 0);
            this.tblStartTime.Controls.Add(this.txtStartH, 1, 0);
            this.tblStartTime.Controls.Add(this.label2, 2, 0);
            this.tblStartTime.Controls.Add(this.txtStartM, 3, 0);
            this.tblStartTime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblStartTime.Location = new System.Drawing.Point(3, 33);
            this.tblStartTime.Name = "tblStartTime";
            this.tblStartTime.RowCount = 1;
            this.tblStartTime.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblStartTime.Size = new System.Drawing.Size(216, 24);
            this.tblStartTime.TabIndex = 17;
            // 
            // lblResult
            // 
            this.lblResult.Location = new System.Drawing.Point(200, 84);
            this.lblResult.Name = "lblResult";
            this.lblResult.Size = new System.Drawing.Size(128, 84);
            this.lblResult.TabIndex = 17;
            this.lblResult.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // frmAddSchedule
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(194)))), ((int)(((byte)(217)))), ((int)(((byte)(247)))));
            this.ClientSize = new System.Drawing.Size(330, 208);
            this.Controls.Add(this.lblResult);
            this.Controls.Add(this.tblControls);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.cmbType);
            this.Controls.Add(this.btnAdd);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmAddSchedule";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add Schedule";
            this.Load += new System.EventHandler(this.frmAddSchedule_Load);
            this.tblControls.ResumeLayout(false);
            this.tblPeriod.ResumeLayout(false);
            this.tblPeriod.PerformLayout();
            this.tblDay.ResumeLayout(false);
            this.tblDay.PerformLayout();
            this.tblEndTime.ResumeLayout(false);
            this.tblEndTime.PerformLayout();
            this.tblStartTime.ResumeLayout(false);
            this.tblStartTime.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.ComboBox cmbType;
        private System.Windows.Forms.Label lblStart;
        private System.Windows.Forms.TextBox txtStartH;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtStartM;
        private System.Windows.Forms.TextBox txtEndM;
        private System.Windows.Forms.TextBox txtEndH;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblDay;
        private System.Windows.Forms.ComboBox cmbDay;
        private System.Windows.Forms.TextBox txtPeriod;
        private System.Windows.Forms.Label lblEvery;
        private System.Windows.Forms.Label lblMinutes;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TableLayoutPanel tblControls;
        private System.Windows.Forms.TableLayoutPanel tblStartTime;
        private System.Windows.Forms.TableLayoutPanel tblEndTime;
        private System.Windows.Forms.Label lblEnd;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TableLayoutPanel tblPeriod;
        private System.Windows.Forms.TableLayoutPanel tblDay;
        private System.Windows.Forms.Label lblResult;
    }
}