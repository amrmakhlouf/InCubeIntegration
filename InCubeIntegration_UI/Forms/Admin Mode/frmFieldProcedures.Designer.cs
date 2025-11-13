namespace InCubeIntegration_UI
{
    partial class frmFieldProcedures
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
            this.lsvFields = new System.Windows.Forms.ListView();
            this.clmFieldID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.clmFieldName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.grdProcedures = new System.Windows.Forms.DataGridView();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.grdProcParams = new System.Windows.Forms.DataGridView();
            this.picDown = new System.Windows.Forms.PictureBox();
            this.picUp = new System.Windows.Forms.PictureBox();
            this.clmSeq = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmProcName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmProcType = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.clmExecTable = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmReadExecDetails = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.clmExecDetailsQry = new System.Windows.Forms.DataGridViewButtonColumn();
            this.clmMailTemplateID = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.label1 = new System.Windows.Forms.Label();
            this.clmParamName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmParamType = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.clmIsBuiltInParam = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.clmBuiltInParamValue = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.clmConstantValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.grdProcedures)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdProcParams)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picUp)).BeginInit();
            this.SuspendLayout();
            // 
            // lsvFields
            // 
            this.lsvFields.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.clmFieldID,
            this.clmFieldName});
            this.lsvFields.Location = new System.Drawing.Point(3, 12);
            this.lsvFields.Name = "lsvFields";
            this.lsvFields.Size = new System.Drawing.Size(220, 566);
            this.lsvFields.TabIndex = 0;
            this.lsvFields.UseCompatibleStateImageBehavior = false;
            this.lsvFields.View = System.Windows.Forms.View.Details;
            // 
            // clmFieldID
            // 
            this.clmFieldID.Text = "FieldID";
            // 
            // clmFieldName
            // 
            this.clmFieldName.Text = "Field Name";
            this.clmFieldName.Width = 175;
            // 
            // grdProcedures
            // 
            this.grdProcedures.BackgroundColor = System.Drawing.Color.White;
            this.grdProcedures.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdProcedures.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.clmSeq,
            this.clmProcName,
            this.clmProcType,
            this.clmExecTable,
            this.clmReadExecDetails,
            this.clmExecDetailsQry,
            this.clmMailTemplateID});
            this.grdProcedures.Location = new System.Drawing.Point(6, 19);
            this.grdProcedures.MultiSelect = false;
            this.grdProcedures.Name = "grdProcedures";
            this.grdProcedures.Size = new System.Drawing.Size(747, 234);
            this.grdProcedures.TabIndex = 1;
            this.grdProcedures.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.grdProcedures_CellContentClick);
            this.grdProcedures.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.grdProcedures_CellValueChanged);
            this.grdProcedures.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.grdProcedures_RowsAdded);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.picDown);
            this.groupBox1.Controls.Add(this.picUp);
            this.groupBox1.Controls.Add(this.btnSave);
            this.groupBox1.Controls.Add(this.grdProcParams);
            this.groupBox1.Controls.Add(this.grdProcedures);
            this.groupBox1.Location = new System.Drawing.Point(231, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(804, 566);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Procedures";
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(678, 537);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            // 
            // grdProcParams
            // 
            this.grdProcParams.BackgroundColor = System.Drawing.Color.White;
            this.grdProcParams.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdProcParams.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.clmParamName,
            this.clmParamType,
            this.clmIsBuiltInParam,
            this.clmBuiltInParamValue,
            this.clmConstantValue});
            this.grdProcParams.Location = new System.Drawing.Point(6, 285);
            this.grdProcParams.MultiSelect = false;
            this.grdProcParams.Name = "grdProcParams";
            this.grdProcParams.Size = new System.Drawing.Size(747, 240);
            this.grdProcParams.TabIndex = 2;
            // 
            // picDown
            // 
            this.picDown.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picDown.Image = global::InCubeIntegration_UI.Properties.Resources.down;
            this.picDown.Location = new System.Drawing.Point(758, 100);
            this.picDown.Name = "picDown";
            this.picDown.Size = new System.Drawing.Size(40, 40);
            this.picDown.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picDown.TabIndex = 36;
            this.picDown.TabStop = false;
            this.picDown.Click += new System.EventHandler(this.picDown_Click);
            // 
            // picUp
            // 
            this.picUp.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picUp.Image = global::InCubeIntegration_UI.Properties.Resources.up;
            this.picUp.Location = new System.Drawing.Point(758, 54);
            this.picUp.Name = "picUp";
            this.picUp.Size = new System.Drawing.Size(40, 40);
            this.picUp.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picUp.TabIndex = 35;
            this.picUp.TabStop = false;
            this.picUp.Click += new System.EventHandler(this.picUp_Click);
            // 
            // clmSeq
            // 
            this.clmSeq.FillWeight = 40F;
            this.clmSeq.Frozen = true;
            this.clmSeq.HeaderText = "Seq";
            this.clmSeq.Name = "clmSeq";
            this.clmSeq.ReadOnly = true;
            this.clmSeq.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.clmSeq.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.clmSeq.Width = 30;
            // 
            // clmProcName
            // 
            this.clmProcName.Frozen = true;
            this.clmProcName.HeaderText = "Procedure Name";
            this.clmProcName.Name = "clmProcName";
            this.clmProcName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.clmProcName.Width = 150;
            // 
            // clmProcType
            // 
            this.clmProcType.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.clmProcType.HeaderText = "Procedure Type";
            this.clmProcType.Name = "clmProcType";
            this.clmProcType.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // clmExecTable
            // 
            this.clmExecTable.HeaderText = "Execution Table";
            this.clmExecTable.Name = "clmExecTable";
            this.clmExecTable.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.clmExecTable.Width = 120;
            // 
            // clmReadExecDetails
            // 
            this.clmReadExecDetails.HeaderText = "Read Exec Details";
            this.clmReadExecDetails.Name = "clmReadExecDetails";
            this.clmReadExecDetails.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.clmReadExecDetails.Width = 70;
            // 
            // clmExecDetailsQry
            // 
            this.clmExecDetailsQry.HeaderText = "Execution Details Query";
            this.clmExecDetailsQry.Name = "clmExecDetailsQry";
            this.clmExecDetailsQry.ReadOnly = true;
            this.clmExecDetailsQry.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.clmExecDetailsQry.Text = "";
            this.clmExecDetailsQry.Width = 80;
            // 
            // clmMailTemplateID
            // 
            this.clmMailTemplateID.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.clmMailTemplateID.HeaderText = "Mail Template";
            this.clmMailTemplateID.Name = "clmMailTemplateID";
            this.clmMailTemplateID.ReadOnly = true;
            this.clmMailTemplateID.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.clmMailTemplateID.Width = 150;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 269);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(112, 13);
            this.label1.TabIndex = 37;
            this.label1.Text = "Procedure Parameters";
            // 
            // clmParamName
            // 
            this.clmParamName.HeaderText = "Parameter Name";
            this.clmParamName.Name = "clmParamName";
            this.clmParamName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // clmParamType
            // 
            this.clmParamType.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.clmParamType.HeaderText = "Parameter Type";
            this.clmParamType.Name = "clmParamType";
            this.clmParamType.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // clmIsBuiltInParam
            // 
            this.clmIsBuiltInParam.HeaderText = "Built in";
            this.clmIsBuiltInParam.Name = "clmIsBuiltInParam";
            this.clmIsBuiltInParam.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.clmIsBuiltInParam.Width = 50;
            // 
            // clmBuiltInParamValue
            // 
            this.clmBuiltInParamValue.FillWeight = 150F;
            this.clmBuiltInParamValue.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.clmBuiltInParamValue.HeaderText = "Built In Parameter Value";
            this.clmBuiltInParamValue.Name = "clmBuiltInParamValue";
            this.clmBuiltInParamValue.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.clmBuiltInParamValue.Width = 150;
            // 
            // clmConstantValue
            // 
            this.clmConstantValue.HeaderText = "Constant Value";
            this.clmConstantValue.Name = "clmConstantValue";
            this.clmConstantValue.Width = 200;
            // 
            // frmFieldProcedures
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1041, 590);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.lsvFields);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "frmFieldProcedures";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Configure Field Procedures";
            this.Load += new System.EventHandler(this.frmFieldProcedures_Load);
            ((System.ComponentModel.ISupportInitialize)(this.grdProcedures)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdProcParams)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picUp)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView lsvFields;
        private System.Windows.Forms.DataGridView grdProcedures;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.DataGridView grdProcParams;
        private System.Windows.Forms.ColumnHeader clmFieldID;
        private System.Windows.Forms.ColumnHeader clmFieldName;
        private System.Windows.Forms.PictureBox picDown;
        private System.Windows.Forms.PictureBox picUp;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmSeq;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmProcName;
        private System.Windows.Forms.DataGridViewComboBoxColumn clmProcType;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmExecTable;
        private System.Windows.Forms.DataGridViewCheckBoxColumn clmReadExecDetails;
        private System.Windows.Forms.DataGridViewButtonColumn clmExecDetailsQry;
        private System.Windows.Forms.DataGridViewComboBoxColumn clmMailTemplateID;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmParamName;
        private System.Windows.Forms.DataGridViewComboBoxColumn clmParamType;
        private System.Windows.Forms.DataGridViewCheckBoxColumn clmIsBuiltInParam;
        private System.Windows.Forms.DataGridViewComboBoxColumn clmBuiltInParamValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmConstantValue;
    }
}