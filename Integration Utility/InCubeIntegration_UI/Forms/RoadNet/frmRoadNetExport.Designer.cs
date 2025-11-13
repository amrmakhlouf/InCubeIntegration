namespace InCubeIntegration_UI
{
    partial class frmRoadNetExport
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
            this.dtpOrderDateFrom = new System.Windows.Forms.DateTimePicker();
            this.dtpOrderDateTo = new System.Windows.Forms.DateTimePicker();
            this.dgvOrders = new System.Windows.Forms.DataGridView();
            this.btnGetOrders = new System.Windows.Forms.Button();
            this.cbAll = new System.Windows.Forms.CheckBox();
            this.btnSendOrders = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.dtpDeliveryDateTo = new System.Windows.Forms.DateTimePicker();
            this.dtpDeliveryDateFrom = new System.Windows.Forms.DateTimePicker();
            this.cmbRegion = new System.Windows.Forms.ComboBox();
            this.txtOrderID = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tpFindOrders = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.txtSalesQty = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.txtRtnQty = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.txtSalesOrders = new System.Windows.Forms.TextBox();
            this.txtRtnOrders = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.dtpSessionDate = new System.Windows.Forms.DateTimePicker();
            this.btnSendToRoadNet = new System.Windows.Forms.Button();
            this.tpSessionDetails = new System.Windows.Forms.TabPage();
            this.dgvSessionOrders = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewComboBoxColumn1 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCheck = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.ColOrderNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOrderType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCustomerCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCustomerName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOrderDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDeliveryDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRoute = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRegion = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.colOrderTypeID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSpecialInstructions = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOrderNotes = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrders)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tpFindOrders.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tpSessionDetails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSessionOrders)).BeginInit();
            this.SuspendLayout();
            // 
            // dtpOrderDateFrom
            // 
            this.dtpOrderDateFrom.Location = new System.Drawing.Point(117, 11);
            this.dtpOrderDateFrom.Name = "dtpOrderDateFrom";
            this.dtpOrderDateFrom.Size = new System.Drawing.Size(200, 20);
            this.dtpOrderDateFrom.TabIndex = 0;
            // 
            // dtpOrderDateTo
            // 
            this.dtpOrderDateTo.Location = new System.Drawing.Point(392, 11);
            this.dtpOrderDateTo.Name = "dtpOrderDateTo";
            this.dtpOrderDateTo.Size = new System.Drawing.Size(200, 20);
            this.dtpOrderDateTo.TabIndex = 1;
            // 
            // dgvOrders
            // 
            this.dgvOrders.AllowUserToAddRows = false;
            this.dgvOrders.AllowUserToDeleteRows = false;
            this.dgvOrders.AllowUserToResizeRows = false;
            this.dgvOrders.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvOrders.BackgroundColor = System.Drawing.Color.White;
            this.dgvOrders.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvOrders.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colCheck,
            this.ColOrderNo,
            this.colOrderType,
            this.colCustomerCode,
            this.colCustomerName,
            this.colOrderDate,
            this.colDeliveryDate,
            this.colRoute,
            this.colRegion,
            this.colOrderTypeID,
            this.colSpecialInstructions,
            this.colOrderNotes});
            this.dgvOrders.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.dgvOrders.Location = new System.Drawing.Point(9, 113);
            this.dgvOrders.Name = "dgvOrders";
            this.dgvOrders.Size = new System.Drawing.Size(1121, 404);
            this.dgvOrders.TabIndex = 2;
            this.dgvOrders.CellMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgvOrders_CellMouseDoubleClick);
            this.dgvOrders.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvOrders_CellValueChanged);
            this.dgvOrders.CurrentCellDirtyStateChanged += new System.EventHandler(this.dgvOrders_CurrentCellDirtyStateChanged);
            // 
            // btnGetOrders
            // 
            this.btnGetOrders.Location = new System.Drawing.Point(612, 11);
            this.btnGetOrders.Name = "btnGetOrders";
            this.btnGetOrders.Size = new System.Drawing.Size(75, 73);
            this.btnGetOrders.TabIndex = 6;
            this.btnGetOrders.Text = "Find Orders";
            this.btnGetOrders.UseVisualStyleBackColor = true;
            this.btnGetOrders.Click += new System.EventHandler(this.btnGetOrders_Click);
            // 
            // cbAll
            // 
            this.cbAll.AutoSize = true;
            this.cbAll.Location = new System.Drawing.Point(9, 90);
            this.cbAll.Name = "cbAll";
            this.cbAll.Size = new System.Drawing.Size(37, 17);
            this.cbAll.TabIndex = 7;
            this.cbAll.Text = "All";
            this.cbAll.UseVisualStyleBackColor = true;
            this.cbAll.CheckedChanged += new System.EventHandler(this.cbAll_CheckedChanged);
            // 
            // btnSendOrders
            // 
            this.btnSendOrders.Location = new System.Drawing.Point(868, 515);
            this.btnSendOrders.Name = "btnSendOrders";
            this.btnSendOrders.Size = new System.Drawing.Size(110, 23);
            this.btnSendOrders.TabIndex = 6;
            this.btnSendOrders.Text = "Send To RoadNet";
            this.btnSendOrders.UseVisualStyleBackColor = true;
            this.btnSendOrders.Click += new System.EventHandler(this.btnSendOrders_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(97, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Order Date:    From";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(336, 17);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(20, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "To";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(336, 43);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(20, 13);
            this.label3.TabIndex = 12;
            this.label3.Text = "To";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 43);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(103, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "Delivery Date:  From";
            // 
            // dtpDeliveryDateTo
            // 
            this.dtpDeliveryDateTo.Location = new System.Drawing.Point(392, 37);
            this.dtpDeliveryDateTo.Name = "dtpDeliveryDateTo";
            this.dtpDeliveryDateTo.Size = new System.Drawing.Size(200, 20);
            this.dtpDeliveryDateTo.TabIndex = 3;
            // 
            // dtpDeliveryDateFrom
            // 
            this.dtpDeliveryDateFrom.Location = new System.Drawing.Point(117, 37);
            this.dtpDeliveryDateFrom.Name = "dtpDeliveryDateFrom";
            this.dtpDeliveryDateFrom.Size = new System.Drawing.Size(200, 20);
            this.dtpDeliveryDateFrom.TabIndex = 2;
            // 
            // cmbRegion
            // 
            this.cmbRegion.FormattingEnabled = true;
            this.cmbRegion.Location = new System.Drawing.Point(117, 63);
            this.cmbRegion.Name = "cmbRegion";
            this.cmbRegion.Size = new System.Drawing.Size(200, 21);
            this.cmbRegion.TabIndex = 4;
            // 
            // txtOrderID
            // 
            this.txtOrderID.Location = new System.Drawing.Point(392, 63);
            this.txtOrderID.Name = "txtOrderID";
            this.txtOrderID.Size = new System.Drawing.Size(200, 20);
            this.txtOrderID.TabIndex = 5;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(336, 66);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(50, 13);
            this.label5.TabIndex = 15;
            this.label5.Text = "Order No";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 66);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(41, 13);
            this.label6.TabIndex = 16;
            this.label6.Text = "Region";
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tpFindOrders);
            this.tabControl1.Controls.Add(this.tpSessionDetails);
            this.tabControl1.Location = new System.Drawing.Point(8, 10);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1149, 578);
            this.tabControl1.TabIndex = 17;
            // 
            // tpFindOrders
            // 
            this.tpFindOrders.Controls.Add(this.groupBox1);
            this.tpFindOrders.Controls.Add(this.label7);
            this.tpFindOrders.Controls.Add(this.dtpSessionDate);
            this.tpFindOrders.Controls.Add(this.btnSendToRoadNet);
            this.tpFindOrders.Controls.Add(this.label6);
            this.tpFindOrders.Controls.Add(this.cbAll);
            this.tpFindOrders.Controls.Add(this.dgvOrders);
            this.tpFindOrders.Controls.Add(this.label5);
            this.tpFindOrders.Controls.Add(this.btnGetOrders);
            this.tpFindOrders.Controls.Add(this.label1);
            this.tpFindOrders.Controls.Add(this.txtOrderID);
            this.tpFindOrders.Controls.Add(this.dtpOrderDateFrom);
            this.tpFindOrders.Controls.Add(this.cmbRegion);
            this.tpFindOrders.Controls.Add(this.dtpOrderDateTo);
            this.tpFindOrders.Controls.Add(this.label3);
            this.tpFindOrders.Controls.Add(this.label2);
            this.tpFindOrders.Controls.Add(this.label4);
            this.tpFindOrders.Controls.Add(this.dtpDeliveryDateFrom);
            this.tpFindOrders.Controls.Add(this.dtpDeliveryDateTo);
            this.tpFindOrders.Location = new System.Drawing.Point(4, 22);
            this.tpFindOrders.Name = "tpFindOrders";
            this.tpFindOrders.Padding = new System.Windows.Forms.Padding(3);
            this.tpFindOrders.Size = new System.Drawing.Size(1141, 552);
            this.tpFindOrders.TabIndex = 0;
            this.tpFindOrders.Text = "Find Orders";
            this.tpFindOrders.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.label11);
            this.groupBox1.Controls.Add(this.label10);
            this.groupBox1.Controls.Add(this.txtSalesQty);
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.txtRtnQty);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.txtSalesOrders);
            this.groupBox1.Controls.Add(this.txtRtnOrders);
            this.groupBox1.Location = new System.Drawing.Point(853, 5);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(275, 100);
            this.groupBox1.TabIndex = 23;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Summary";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(6, 71);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(44, 13);
            this.label11.TabIndex = 26;
            this.label11.Text = "Returns";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(6, 45);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(33, 13);
            this.label10.TabIndex = 25;
            this.label10.Text = "Sales";
            // 
            // txtSalesQty
            // 
            this.txtSalesQty.Location = new System.Drawing.Point(165, 42);
            this.txtSalesQty.Name = "txtSalesQty";
            this.txtSalesQty.ReadOnly = true;
            this.txtSalesQty.Size = new System.Drawing.Size(105, 20);
            this.txtSalesQty.TabIndex = 23;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(190, 22);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(54, 13);
            this.label9.TabIndex = 21;
            this.label9.Text = "Quantities";
            // 
            // txtRtnQty
            // 
            this.txtRtnQty.Location = new System.Drawing.Point(165, 68);
            this.txtRtnQty.Name = "txtRtnQty";
            this.txtRtnQty.ReadOnly = true;
            this.txtRtnQty.Size = new System.Drawing.Size(105, 20);
            this.txtRtnQty.TabIndex = 24;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(83, 23);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(38, 13);
            this.label8.TabIndex = 20;
            this.label8.Text = "Orders";
            // 
            // txtSalesOrders
            // 
            this.txtSalesOrders.Location = new System.Drawing.Point(54, 42);
            this.txtSalesOrders.Name = "txtSalesOrders";
            this.txtSalesOrders.ReadOnly = true;
            this.txtSalesOrders.Size = new System.Drawing.Size(105, 20);
            this.txtSalesOrders.TabIndex = 19;
            // 
            // txtRtnOrders
            // 
            this.txtRtnOrders.Location = new System.Drawing.Point(54, 68);
            this.txtRtnOrders.Name = "txtRtnOrders";
            this.txtRtnOrders.ReadOnly = true;
            this.txtRtnOrders.Size = new System.Drawing.Size(105, 20);
            this.txtRtnOrders.TabIndex = 22;
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(590, 528);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(113, 13);
            this.label7.TabIndex = 18;
            this.label7.Text = "Delivery/Session Date";
            // 
            // dtpSessionDate
            // 
            this.dtpSessionDate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.dtpSessionDate.Checked = false;
            this.dtpSessionDate.Location = new System.Drawing.Point(711, 524);
            this.dtpSessionDate.Name = "dtpSessionDate";
            this.dtpSessionDate.ShowCheckBox = true;
            this.dtpSessionDate.Size = new System.Drawing.Size(267, 20);
            this.dtpSessionDate.TabIndex = 17;
            // 
            // btnSendToRoadNet
            // 
            this.btnSendToRoadNet.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSendToRoadNet.Location = new System.Drawing.Point(1021, 523);
            this.btnSendToRoadNet.Name = "btnSendToRoadNet";
            this.btnSendToRoadNet.Size = new System.Drawing.Size(110, 23);
            this.btnSendToRoadNet.TabIndex = 8;
            this.btnSendToRoadNet.Text = "Send To RoadNet";
            this.btnSendToRoadNet.UseVisualStyleBackColor = true;
            this.btnSendToRoadNet.Click += new System.EventHandler(this.btnSendOrders_Click);
            // 
            // tpSessionDetails
            // 
            this.tpSessionDetails.Controls.Add(this.dgvSessionOrders);
            this.tpSessionDetails.Controls.Add(this.btnSendOrders);
            this.tpSessionDetails.Location = new System.Drawing.Point(4, 22);
            this.tpSessionDetails.Name = "tpSessionDetails";
            this.tpSessionDetails.Padding = new System.Windows.Forms.Padding(3);
            this.tpSessionDetails.Size = new System.Drawing.Size(1037, 552);
            this.tpSessionDetails.TabIndex = 1;
            this.tpSessionDetails.Text = "Session Details";
            this.tpSessionDetails.UseVisualStyleBackColor = true;
            // 
            // dgvSessionOrders
            // 
            this.dgvSessionOrders.AllowUserToAddRows = false;
            this.dgvSessionOrders.AllowUserToDeleteRows = false;
            this.dgvSessionOrders.AllowUserToResizeRows = false;
            this.dgvSessionOrders.BackgroundColor = System.Drawing.Color.White;
            this.dgvSessionOrders.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSessionOrders.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1,
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn3,
            this.dataGridViewTextBoxColumn4,
            this.dataGridViewTextBoxColumn5,
            this.dataGridViewTextBoxColumn6,
            this.dataGridViewComboBoxColumn1,
            this.dataGridViewTextBoxColumn7});
            this.dgvSessionOrders.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.dgvSessionOrders.Location = new System.Drawing.Point(3, 6);
            this.dgvSessionOrders.Name = "dgvSessionOrders";
            this.dgvSessionOrders.Size = new System.Drawing.Size(975, 503);
            this.dgvSessionOrders.TabIndex = 3;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.HeaderText = "Order No";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.HeaderText = "Customer Code";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.HeaderText = "Customer Name";
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 150;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.HeaderText = "Order Date";
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.HeaderText = "Delivery Date";
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.HeaderText = "Route";
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            // 
            // dataGridViewComboBoxColumn1
            // 
            this.dataGridViewComboBoxColumn1.HeaderText = "Region";
            this.dataGridViewComboBoxColumn1.Name = "dataGridViewComboBoxColumn1";
            this.dataGridViewComboBoxColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewComboBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.HeaderText = "Special Instructions";
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            this.dataGridViewTextBoxColumn7.Width = 200;
            // 
            // colCheck
            // 
            this.colCheck.Frozen = true;
            this.colCheck.HeaderText = "";
            this.colCheck.Name = "colCheck";
            this.colCheck.Width = 30;
            // 
            // ColOrderNo
            // 
            this.ColOrderNo.FillWeight = 120F;
            this.ColOrderNo.HeaderText = "Order No";
            this.ColOrderNo.Name = "ColOrderNo";
            this.ColOrderNo.ReadOnly = true;
            // 
            // colOrderType
            // 
            this.colOrderType.HeaderText = "Order Type";
            this.colOrderType.Name = "colOrderType";
            this.colOrderType.ReadOnly = true;
            this.colOrderType.Width = 60;
            // 
            // colCustomerCode
            // 
            this.colCustomerCode.HeaderText = "Customer Code";
            this.colCustomerCode.Name = "colCustomerCode";
            this.colCustomerCode.ReadOnly = true;
            this.colCustomerCode.Width = 70;
            // 
            // colCustomerName
            // 
            this.colCustomerName.HeaderText = "Customer Name";
            this.colCustomerName.Name = "colCustomerName";
            this.colCustomerName.ReadOnly = true;
            this.colCustomerName.Width = 120;
            // 
            // colOrderDate
            // 
            this.colOrderDate.HeaderText = "Order Date";
            this.colOrderDate.Name = "colOrderDate";
            this.colOrderDate.ReadOnly = true;
            this.colOrderDate.Width = 110;
            // 
            // colDeliveryDate
            // 
            this.colDeliveryDate.HeaderText = "Delivery Date";
            this.colDeliveryDate.Name = "colDeliveryDate";
            this.colDeliveryDate.ReadOnly = true;
            this.colDeliveryDate.Width = 110;
            // 
            // colRoute
            // 
            this.colRoute.HeaderText = "Route";
            this.colRoute.Name = "colRoute";
            this.colRoute.ReadOnly = true;
            this.colRoute.Width = 70;
            // 
            // colRegion
            // 
            this.colRegion.HeaderText = "Region";
            this.colRegion.Name = "colRegion";
            this.colRegion.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colRegion.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // colOrderTypeID
            // 
            this.colOrderTypeID.HeaderText = "OrderTypeID";
            this.colOrderTypeID.Name = "colOrderTypeID";
            this.colOrderTypeID.ReadOnly = true;
            this.colOrderTypeID.Visible = false;
            // 
            // colSpecialInstructions
            // 
            this.colSpecialInstructions.HeaderText = "Special Instructions";
            this.colSpecialInstructions.Name = "colSpecialInstructions";
            this.colSpecialInstructions.Width = 220;
            // 
            // colOrderNotes
            // 
            this.colOrderNotes.HeaderText = "Order Notes";
            this.colOrderNotes.Name = "colOrderNotes";
            this.colOrderNotes.Width = 220;
            // 
            // frmRoadNetExport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1163, 591);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "frmRoadNetExport";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Export to RoadNet";
            this.Load += new System.EventHandler(this.frmRoadNetIntegration_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrders)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tpFindOrders.ResumeLayout(false);
            this.tpFindOrders.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tpSessionDetails.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSessionOrders)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DateTimePicker dtpOrderDateFrom;
        private System.Windows.Forms.DateTimePicker dtpOrderDateTo;
        private System.Windows.Forms.DataGridView dgvOrders;
        private System.Windows.Forms.Button btnGetOrders;
        private System.Windows.Forms.CheckBox cbAll;
        private System.Windows.Forms.Button btnSendOrders;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.DateTimePicker dtpDeliveryDateTo;
        private System.Windows.Forms.DateTimePicker dtpDeliveryDateFrom;
        private System.Windows.Forms.ComboBox cmbRegion;
        private System.Windows.Forms.TextBox txtOrderID;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tpFindOrders;
        private System.Windows.Forms.Button btnSendToRoadNet;
        private System.Windows.Forms.TabPage tpSessionDetails;
        private System.Windows.Forms.DataGridView dgvSessionOrders;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.DateTimePicker dtpSessionDate;
        private System.Windows.Forms.TextBox txtRtnOrders;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtSalesOrders;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtSalesQty;
        private System.Windows.Forms.TextBox txtRtnQty;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colCheck;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColOrderNo;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOrderType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCustomerCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCustomerName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOrderDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDeliveryDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRoute;
        private System.Windows.Forms.DataGridViewComboBoxColumn colRegion;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOrderTypeID;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSpecialInstructions;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOrderNotes;
    }
}