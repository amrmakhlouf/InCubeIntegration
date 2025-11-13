namespace InCubeIntegration_UI
{
    partial class frmIntegration
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmIntegration));
            this.txtMessages = new System.Windows.Forms.TextBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tpImport = new System.Windows.Forms.TabPage();
            this.lblFormWidth = new System.Windows.Forms.Label();
            this.pnlTextSearchFilter = new System.Windows.Forms.Panel();
            this.txtTextSearch = new System.Windows.Forms.TextBox();
            this.pnlImportEmployeeFilter = new System.Windows.Forms.Panel();
            this.cmbImportEmployee = new System.Windows.Forms.ComboBox();
            this.lblImportEmployee = new System.Windows.Forms.Label();
            this.cbImportAllEmployees = new System.Windows.Forms.CheckBox();
            this.pnlMasterDateFilter = new System.Windows.Forms.Panel();
            this.txtCustCode = new System.Windows.Forms.TextBox();
            this.cbOpenInvoicesOnly = new System.Windows.Forms.CheckBox();
            this.dtpImportToDate = new System.Windows.Forms.DateTimePicker();
            this.lmlImportToDate = new System.Windows.Forms.Label();
            this.dtpImportFromDate = new System.Windows.Forms.DateTimePicker();
            this.lblImportFromDate = new System.Windows.Forms.Label();
            this.btnUpdateFromERP = new System.Windows.Forms.Button();
            this.UpdateProgressBar = new InCubeIntegration_UI.CustomProgressBar();
            this.ChkUpdateStock = new System.Windows.Forms.CheckBox();
            this.gbUpdateStock = new System.Windows.Forms.GroupBox();
            this.lblStockDate = new System.Windows.Forms.Label();
            this.dtpStockDate = new System.Windows.Forms.DateTimePicker();
            this.lblWarehouse = new System.Windows.Forms.Label();
            this.cmbWarehouse = new System.Windows.Forms.ComboBox();
            this.cbxUpdateStockAllVans = new System.Windows.Forms.CheckBox();
            this.gbImportItems = new System.Windows.Forms.GroupBox();
            this.lsvUpdateItems = new System.Windows.Forms.ListView();
            this.cbUpdateAllItems = new System.Windows.Forms.CheckBox();
            this.tpSend = new System.Windows.Forms.TabPage();
            this.SendProgressBar = new InCubeIntegration_UI.CustomProgressBar();
            this.gbSendItems = new System.Windows.Forms.GroupBox();
            this.lsvSendItems = new System.Windows.Forms.ListView();
            this.cbSendAllItems = new System.Windows.Forms.CheckBox();
            this.pnlSendEmployeeFilter = new System.Windows.Forms.Panel();
            this.cmbSendEmployee = new System.Windows.Forms.ComboBox();
            this.lblSendEmployee = new System.Windows.Forms.Label();
            this.cbSendAllEmployees = new System.Windows.Forms.CheckBox();
            this.gbFilterSend = new System.Windows.Forms.GroupBox();
            this.cbSendTax = new System.Windows.Forms.CheckBox();
            this.lblDocNo = new System.Windows.Forms.Label();
            this.lblOR = new System.Windows.Forms.Label();
            this.txtInv = new System.Windows.Forms.TextBox();
            this.dtpSendToDate = new System.Windows.Forms.DateTimePicker();
            this.dtpSendFromDate = new System.Windows.Forms.DateTimePicker();
            this.lblSendFromDate = new System.Windows.Forms.Label();
            this.lblSendToDate = new System.Windows.Forms.Label();
            this.btnSendToERP = new System.Windows.Forms.Button();
            this.tpSpecial = new System.Windows.Forms.TabPage();
            this.panel1 = new System.Windows.Forms.Panel();
            this.dtpSF_ToDate = new System.Windows.Forms.DateTimePicker();
            this.label2 = new System.Windows.Forms.Label();
            this.dtpSF_FromDate = new System.Windows.Forms.DateTimePicker();
            this.label3 = new System.Windows.Forms.Label();
            this.gpSpecialActions = new System.Windows.Forms.GroupBox();
            this.lsvSpecialActions = new System.Windows.Forms.ListView();
            this.cbRunAllSpecialActions = new System.Windows.Forms.CheckBox();
            this.btnRunSpecialActions = new System.Windows.Forms.Button();
            this.SpecialAccessProgressBar = new InCubeIntegration_UI.CustomProgressBar();
            this.lblVersion = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.itF_O_S_EnvRecArqPalmService1 = new InCubeIntegration_BL.BRF_SAP_WS.ITF_O_S_EnvRecArqPalmService();
            this.tabControl1.SuspendLayout();
            this.tpImport.SuspendLayout();
            this.pnlTextSearchFilter.SuspendLayout();
            this.pnlImportEmployeeFilter.SuspendLayout();
            this.pnlMasterDateFilter.SuspendLayout();
            this.gbUpdateStock.SuspendLayout();
            this.gbImportItems.SuspendLayout();
            this.tpSend.SuspendLayout();
            this.gbSendItems.SuspendLayout();
            this.pnlSendEmployeeFilter.SuspendLayout();
            this.gbFilterSend.SuspendLayout();
            this.tpSpecial.SuspendLayout();
            this.panel1.SuspendLayout();
            this.gpSpecialActions.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtMessages
            // 
            this.txtMessages.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.txtMessages.Location = new System.Drawing.Point(7, 326);
            this.txtMessages.Multiline = true;
            this.txtMessages.Name = "txtMessages";
            this.txtMessages.ReadOnly = true;
            this.txtMessages.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtMessages.Size = new System.Drawing.Size(515, 145);
            this.txtMessages.TabIndex = 85;
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tpImport);
            this.tabControl1.Controls.Add(this.tpSend);
            this.tabControl1.Controls.Add(this.tpSpecial);
            this.tabControl1.Location = new System.Drawing.Point(7, 8);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1115, 295);
            this.tabControl1.TabIndex = 35;
            // 
            // tpImport
            // 
            this.tpImport.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(194)))), ((int)(((byte)(217)))), ((int)(((byte)(247)))));
            this.tpImport.Controls.Add(this.lblFormWidth);
            this.tpImport.Controls.Add(this.pnlTextSearchFilter);
            this.tpImport.Controls.Add(this.pnlImportEmployeeFilter);
            this.tpImport.Controls.Add(this.pnlMasterDateFilter);
            this.tpImport.Controls.Add(this.btnUpdateFromERP);
            this.tpImport.Controls.Add(this.UpdateProgressBar);
            this.tpImport.Controls.Add(this.ChkUpdateStock);
            this.tpImport.Controls.Add(this.gbUpdateStock);
            this.tpImport.Controls.Add(this.gbImportItems);
            this.tpImport.Location = new System.Drawing.Point(4, 22);
            this.tpImport.Name = "tpImport";
            this.tpImport.Padding = new System.Windows.Forms.Padding(3);
            this.tpImport.Size = new System.Drawing.Size(1107, 269);
            this.tpImport.TabIndex = 0;
            this.tpImport.Text = "Import Master Data";
            // 
            // lblFormWidth
            // 
            this.lblFormWidth.Location = new System.Drawing.Point(513, 132);
            this.lblFormWidth.Name = "lblFormWidth";
            this.lblFormWidth.Size = new System.Drawing.Size(543, 13);
            this.lblFormWidth.TabIndex = 85;
            this.lblFormWidth.Text = "Form Width";
            this.lblFormWidth.Visible = false;
            // 
            // pnlTextSearchFilter
            // 
            this.pnlTextSearchFilter.Controls.Add(this.txtTextSearch);
            this.pnlTextSearchFilter.Location = new System.Drawing.Point(516, 76);
            this.pnlTextSearchFilter.Name = "pnlTextSearchFilter";
            this.pnlTextSearchFilter.Size = new System.Drawing.Size(489, 29);
            this.pnlTextSearchFilter.TabIndex = 45;
            this.pnlTextSearchFilter.Visible = false;
            // 
            // txtTextSearch
            // 
            this.txtTextSearch.ForeColor = System.Drawing.Color.Silver;
            this.txtTextSearch.Location = new System.Drawing.Point(9, 3);
            this.txtTextSearch.Name = "txtTextSearch";
            this.txtTextSearch.Size = new System.Drawing.Size(277, 20);
            this.txtTextSearch.TabIndex = 44;
            this.txtTextSearch.Text = "Enter any text to minimize integrated data";
            this.txtTextSearch.MouseDown += new System.Windows.Forms.MouseEventHandler(this.txtTextSearch_MouseDown);
            // 
            // pnlImportEmployeeFilter
            // 
            this.pnlImportEmployeeFilter.Controls.Add(this.cmbImportEmployee);
            this.pnlImportEmployeeFilter.Controls.Add(this.lblImportEmployee);
            this.pnlImportEmployeeFilter.Controls.Add(this.cbImportAllEmployees);
            this.pnlImportEmployeeFilter.Location = new System.Drawing.Point(7, 6);
            this.pnlImportEmployeeFilter.Name = "pnlImportEmployeeFilter";
            this.pnlImportEmployeeFilter.Size = new System.Drawing.Size(375, 29);
            this.pnlImportEmployeeFilter.TabIndex = 40;
            this.pnlImportEmployeeFilter.Visible = false;
            // 
            // cmbImportEmployee
            // 
            this.cmbImportEmployee.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbImportEmployee.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbImportEmployee.Enabled = false;
            this.cmbImportEmployee.FormattingEnabled = true;
            this.cmbImportEmployee.Location = new System.Drawing.Point(65, 5);
            this.cmbImportEmployee.Name = "cmbImportEmployee";
            this.cmbImportEmployee.Size = new System.Drawing.Size(241, 21);
            this.cmbImportEmployee.TabIndex = 37;
            // 
            // lblImportEmployee
            // 
            this.lblImportEmployee.AutoSize = true;
            this.lblImportEmployee.BackColor = System.Drawing.Color.Transparent;
            this.lblImportEmployee.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblImportEmployee.Location = new System.Drawing.Point(6, 8);
            this.lblImportEmployee.Name = "lblImportEmployee";
            this.lblImportEmployee.Size = new System.Drawing.Size(53, 13);
            this.lblImportEmployee.TabIndex = 39;
            this.lblImportEmployee.Text = "Employee";
            // 
            // cbImportAllEmployees
            // 
            this.cbImportAllEmployees.AutoSize = true;
            this.cbImportAllEmployees.BackColor = System.Drawing.Color.Transparent;
            this.cbImportAllEmployees.Checked = true;
            this.cbImportAllEmployees.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbImportAllEmployees.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.cbImportAllEmployees.Location = new System.Drawing.Point(329, 7);
            this.cbImportAllEmployees.Name = "cbImportAllEmployees";
            this.cbImportAllEmployees.Size = new System.Drawing.Size(37, 17);
            this.cbImportAllEmployees.TabIndex = 38;
            this.cbImportAllEmployees.Text = "All";
            this.cbImportAllEmployees.UseVisualStyleBackColor = false;
            this.cbImportAllEmployees.CheckedChanged += new System.EventHandler(this.cbAllEmployees_CheckedChanged);
            // 
            // pnlMasterDateFilter
            // 
            this.pnlMasterDateFilter.Controls.Add(this.txtCustCode);
            this.pnlMasterDateFilter.Controls.Add(this.cbOpenInvoicesOnly);
            this.pnlMasterDateFilter.Controls.Add(this.dtpImportToDate);
            this.pnlMasterDateFilter.Controls.Add(this.lmlImportToDate);
            this.pnlMasterDateFilter.Controls.Add(this.dtpImportFromDate);
            this.pnlMasterDateFilter.Controls.Add(this.lblImportFromDate);
            this.pnlMasterDateFilter.Location = new System.Drawing.Point(516, 31);
            this.pnlMasterDateFilter.Name = "pnlMasterDateFilter";
            this.pnlMasterDateFilter.Size = new System.Drawing.Size(489, 29);
            this.pnlMasterDateFilter.TabIndex = 41;
            this.pnlMasterDateFilter.Visible = false;
            // 
            // txtCustCode
            // 
            this.txtCustCode.ForeColor = System.Drawing.Color.Silver;
            this.txtCustCode.Location = new System.Drawing.Point(398, 4);
            this.txtCustCode.Name = "txtCustCode";
            this.txtCustCode.Size = new System.Drawing.Size(88, 20);
            this.txtCustCode.TabIndex = 44;
            this.txtCustCode.Text = "Payer";
            this.txtCustCode.MouseDown += new System.Windows.Forms.MouseEventHandler(this.txtCustCode_MouseDown);
            // 
            // cbOpenInvoicesOnly
            // 
            this.cbOpenInvoicesOnly.AutoSize = true;
            this.cbOpenInvoicesOnly.Checked = true;
            this.cbOpenInvoicesOnly.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbOpenInvoicesOnly.Location = new System.Drawing.Point(297, 6);
            this.cbOpenInvoicesOnly.Name = "cbOpenInvoicesOnly";
            this.cbOpenInvoicesOnly.Size = new System.Drawing.Size(95, 17);
            this.cbOpenInvoicesOnly.TabIndex = 43;
            this.cbOpenInvoicesOnly.Text = "Open Invoices";
            this.cbOpenInvoicesOnly.UseVisualStyleBackColor = true;
            // 
            // dtpImportToDate
            // 
            this.dtpImportToDate.Checked = false;
            this.dtpImportToDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpImportToDate.Location = new System.Drawing.Point(178, 4);
            this.dtpImportToDate.Name = "dtpImportToDate";
            this.dtpImportToDate.ShowCheckBox = true;
            this.dtpImportToDate.Size = new System.Drawing.Size(108, 20);
            this.dtpImportToDate.TabIndex = 42;
            // 
            // lmlImportToDate
            // 
            this.lmlImportToDate.AutoSize = true;
            this.lmlImportToDate.BackColor = System.Drawing.Color.Transparent;
            this.lmlImportToDate.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lmlImportToDate.Location = new System.Drawing.Point(158, 8);
            this.lmlImportToDate.Name = "lmlImportToDate";
            this.lmlImportToDate.Size = new System.Drawing.Size(19, 13);
            this.lmlImportToDate.TabIndex = 41;
            this.lmlImportToDate.Text = "To";
            // 
            // dtpImportFromDate
            // 
            this.dtpImportFromDate.Checked = false;
            this.dtpImportFromDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpImportFromDate.Location = new System.Drawing.Point(38, 4);
            this.dtpImportFromDate.Name = "dtpImportFromDate";
            this.dtpImportFromDate.ShowCheckBox = true;
            this.dtpImportFromDate.Size = new System.Drawing.Size(108, 20);
            this.dtpImportFromDate.TabIndex = 40;
            // 
            // lblImportFromDate
            // 
            this.lblImportFromDate.AutoSize = true;
            this.lblImportFromDate.BackColor = System.Drawing.Color.Transparent;
            this.lblImportFromDate.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblImportFromDate.Location = new System.Drawing.Point(6, 8);
            this.lblImportFromDate.Name = "lblImportFromDate";
            this.lblImportFromDate.Size = new System.Drawing.Size(31, 13);
            this.lblImportFromDate.TabIndex = 39;
            this.lblImportFromDate.Text = "From";
            // 
            // btnUpdateFromERP
            // 
            this.btnUpdateFromERP.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnUpdateFromERP.Location = new System.Drawing.Point(9, 197);
            this.btnUpdateFromERP.Name = "btnUpdateFromERP";
            this.btnUpdateFromERP.Size = new System.Drawing.Size(118, 30);
            this.btnUpdateFromERP.TabIndex = 23;
            this.btnUpdateFromERP.Text = "Start Update";
            this.btnUpdateFromERP.Click += new System.EventHandler(this.btnUpdateFromERP_Click);
            // 
            // UpdateProgressBar
            // 
            this.UpdateProgressBar.CustomText = null;
            this.UpdateProgressBar.DisplayStyle = InCubeIntegration_UI.ProgressBarDisplayText.Mixed;
            this.UpdateProgressBar.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.UpdateProgressBar.Location = new System.Drawing.Point(9, 236);
            this.UpdateProgressBar.Name = "UpdateProgressBar";
            this.UpdateProgressBar.Size = new System.Drawing.Size(488, 29);
            this.UpdateProgressBar.Step = 30;
            this.UpdateProgressBar.TabIndex = 84;
            // 
            // ChkUpdateStock
            // 
            this.ChkUpdateStock.AutoSize = true;
            this.ChkUpdateStock.BackColor = System.Drawing.Color.Transparent;
            this.ChkUpdateStock.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.ChkUpdateStock.Location = new System.Drawing.Point(17, 133);
            this.ChkUpdateStock.Name = "ChkUpdateStock";
            this.ChkUpdateStock.Size = new System.Drawing.Size(52, 17);
            this.ChkUpdateStock.TabIndex = 34;
            this.ChkUpdateStock.Text = "Stock";
            this.ChkUpdateStock.UseVisualStyleBackColor = false;
            this.ChkUpdateStock.CheckedChanged += new System.EventHandler(this.ChkUpdateStock_CheckedChanged);
            // 
            // gbUpdateStock
            // 
            this.gbUpdateStock.BackColor = System.Drawing.Color.Transparent;
            this.gbUpdateStock.Controls.Add(this.lblStockDate);
            this.gbUpdateStock.Controls.Add(this.dtpStockDate);
            this.gbUpdateStock.Controls.Add(this.lblWarehouse);
            this.gbUpdateStock.Controls.Add(this.cmbWarehouse);
            this.gbUpdateStock.Controls.Add(this.cbxUpdateStockAllVans);
            this.gbUpdateStock.Enabled = false;
            this.gbUpdateStock.ForeColor = System.Drawing.SystemColors.ControlText;
            this.gbUpdateStock.Location = new System.Drawing.Point(7, 132);
            this.gbUpdateStock.Name = "gbUpdateStock";
            this.gbUpdateStock.Size = new System.Drawing.Size(490, 60);
            this.gbUpdateStock.TabIndex = 27;
            this.gbUpdateStock.TabStop = false;
            // 
            // lblStockDate
            // 
            this.lblStockDate.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblStockDate.Location = new System.Drawing.Point(318, 27);
            this.lblStockDate.Name = "lblStockDate";
            this.lblStockDate.Size = new System.Drawing.Size(65, 12);
            this.lblStockDate.TabIndex = 29;
            this.lblStockDate.Text = "Stock Date";
            // 
            // dtpStockDate
            // 
            this.dtpStockDate.Checked = false;
            this.dtpStockDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpStockDate.Location = new System.Drawing.Point(386, 23);
            this.dtpStockDate.Name = "dtpStockDate";
            this.dtpStockDate.Size = new System.Drawing.Size(84, 20);
            this.dtpStockDate.TabIndex = 18;
            // 
            // lblWarehouse
            // 
            this.lblWarehouse.AutoSize = true;
            this.lblWarehouse.BackColor = System.Drawing.Color.Transparent;
            this.lblWarehouse.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblWarehouse.Location = new System.Drawing.Point(6, 27);
            this.lblWarehouse.Name = "lblWarehouse";
            this.lblWarehouse.Size = new System.Drawing.Size(62, 13);
            this.lblWarehouse.TabIndex = 5;
            this.lblWarehouse.Text = "Warehouse";
            // 
            // cmbWarehouse
            // 
            this.cmbWarehouse.Enabled = false;
            this.cmbWarehouse.FormattingEnabled = true;
            this.cmbWarehouse.Location = new System.Drawing.Point(68, 23);
            this.cmbWarehouse.Name = "cmbWarehouse";
            this.cmbWarehouse.Size = new System.Drawing.Size(198, 21);
            this.cmbWarehouse.TabIndex = 3;
            // 
            // cbxUpdateStockAllVans
            // 
            this.cbxUpdateStockAllVans.AutoSize = true;
            this.cbxUpdateStockAllVans.BackColor = System.Drawing.Color.Transparent;
            this.cbxUpdateStockAllVans.Checked = true;
            this.cbxUpdateStockAllVans.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbxUpdateStockAllVans.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.cbxUpdateStockAllVans.Location = new System.Drawing.Point(272, 26);
            this.cbxUpdateStockAllVans.Name = "cbxUpdateStockAllVans";
            this.cbxUpdateStockAllVans.Size = new System.Drawing.Size(37, 17);
            this.cbxUpdateStockAllVans.TabIndex = 4;
            this.cbxUpdateStockAllVans.Text = "All";
            this.cbxUpdateStockAllVans.UseVisualStyleBackColor = false;
            this.cbxUpdateStockAllVans.CheckedChanged += new System.EventHandler(this.cbxUpdateStockAllVans_CheckedChanged);
            // 
            // gbImportItems
            // 
            this.gbImportItems.Controls.Add(this.lsvUpdateItems);
            this.gbImportItems.Controls.Add(this.cbUpdateAllItems);
            this.gbImportItems.Location = new System.Drawing.Point(7, 39);
            this.gbImportItems.Name = "gbImportItems";
            this.gbImportItems.Size = new System.Drawing.Size(487, 90);
            this.gbImportItems.TabIndex = 0;
            this.gbImportItems.TabStop = false;
            this.gbImportItems.Text = "Update The Following Master";
            // 
            // lsvUpdateItems
            // 
            this.lsvUpdateItems.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lsvUpdateItems.CheckBoxes = true;
            this.lsvUpdateItems.HideSelection = false;
            this.lsvUpdateItems.Location = new System.Drawing.Point(9, 19);
            this.lsvUpdateItems.Name = "lsvUpdateItems";
            this.lsvUpdateItems.Size = new System.Drawing.Size(428, 65);
            this.lsvUpdateItems.TabIndex = 34;
            this.lsvUpdateItems.UseCompatibleStateImageBehavior = false;
            this.lsvUpdateItems.View = System.Windows.Forms.View.List;
            this.lsvUpdateItems.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.lvUpdateItems_ItemChecked);
            // 
            // cbUpdateAllItems
            // 
            this.cbUpdateAllItems.AutoSize = true;
            this.cbUpdateAllItems.Location = new System.Drawing.Point(443, 19);
            this.cbUpdateAllItems.Name = "cbUpdateAllItems";
            this.cbUpdateAllItems.Size = new System.Drawing.Size(37, 17);
            this.cbUpdateAllItems.TabIndex = 35;
            this.cbUpdateAllItems.Text = "All";
            this.cbUpdateAllItems.UseVisualStyleBackColor = true;
            this.cbUpdateAllItems.CheckedChanged += new System.EventHandler(this.cbxUpdateAllItems_CheckedChanged);
            // 
            // tpSend
            // 
            this.tpSend.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(194)))), ((int)(((byte)(217)))), ((int)(((byte)(247)))));
            this.tpSend.Controls.Add(this.SendProgressBar);
            this.tpSend.Controls.Add(this.gbSendItems);
            this.tpSend.Controls.Add(this.pnlSendEmployeeFilter);
            this.tpSend.Controls.Add(this.gbFilterSend);
            this.tpSend.Controls.Add(this.btnSendToERP);
            this.tpSend.Location = new System.Drawing.Point(4, 22);
            this.tpSend.Name = "tpSend";
            this.tpSend.Padding = new System.Windows.Forms.Padding(3);
            this.tpSend.Size = new System.Drawing.Size(1107, 269);
            this.tpSend.TabIndex = 1;
            this.tpSend.Text = "Send Transaction";
            // 
            // SendProgressBar
            // 
            this.SendProgressBar.CustomText = null;
            this.SendProgressBar.DisplayStyle = InCubeIntegration_UI.ProgressBarDisplayText.Mixed;
            this.SendProgressBar.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.SendProgressBar.Location = new System.Drawing.Point(9, 236);
            this.SendProgressBar.Name = "SendProgressBar";
            this.SendProgressBar.Size = new System.Drawing.Size(488, 29);
            this.SendProgressBar.Step = 30;
            this.SendProgressBar.TabIndex = 92;
            // 
            // gbSendItems
            // 
            this.gbSendItems.Controls.Add(this.lsvSendItems);
            this.gbSendItems.Controls.Add(this.cbSendAllItems);
            this.gbSendItems.Location = new System.Drawing.Point(7, 39);
            this.gbSendItems.Name = "gbSendItems";
            this.gbSendItems.Size = new System.Drawing.Size(487, 90);
            this.gbSendItems.TabIndex = 46;
            this.gbSendItems.TabStop = false;
            this.gbSendItems.Text = "Send The Following Documents";
            // 
            // lsvSendItems
            // 
            this.lsvSendItems.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lsvSendItems.CheckBoxes = true;
            this.lsvSendItems.HideSelection = false;
            this.lsvSendItems.Location = new System.Drawing.Point(9, 19);
            this.lsvSendItems.Name = "lsvSendItems";
            this.lsvSendItems.Size = new System.Drawing.Size(428, 65);
            this.lsvSendItems.TabIndex = 34;
            this.lsvSendItems.UseCompatibleStateImageBehavior = false;
            this.lsvSendItems.View = System.Windows.Forms.View.List;
            this.lsvSendItems.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.lsvSendItems_ItemChecked);
            // 
            // cbSendAllItems
            // 
            this.cbSendAllItems.AutoSize = true;
            this.cbSendAllItems.Location = new System.Drawing.Point(443, 19);
            this.cbSendAllItems.Name = "cbSendAllItems";
            this.cbSendAllItems.Size = new System.Drawing.Size(37, 17);
            this.cbSendAllItems.TabIndex = 35;
            this.cbSendAllItems.Text = "All";
            this.cbSendAllItems.UseVisualStyleBackColor = true;
            this.cbSendAllItems.CheckedChanged += new System.EventHandler(this.cbSendAllItems_CheckedChanged);
            // 
            // pnlSendEmployeeFilter
            // 
            this.pnlSendEmployeeFilter.Controls.Add(this.cmbSendEmployee);
            this.pnlSendEmployeeFilter.Controls.Add(this.lblSendEmployee);
            this.pnlSendEmployeeFilter.Controls.Add(this.cbSendAllEmployees);
            this.pnlSendEmployeeFilter.Location = new System.Drawing.Point(8, 5);
            this.pnlSendEmployeeFilter.Name = "pnlSendEmployeeFilter";
            this.pnlSendEmployeeFilter.Size = new System.Drawing.Size(375, 29);
            this.pnlSendEmployeeFilter.TabIndex = 45;
            // 
            // cmbSendEmployee
            // 
            this.cmbSendEmployee.Enabled = false;
            this.cmbSendEmployee.FormattingEnabled = true;
            this.cmbSendEmployee.Location = new System.Drawing.Point(65, 5);
            this.cmbSendEmployee.Name = "cmbSendEmployee";
            this.cmbSendEmployee.Size = new System.Drawing.Size(241, 21);
            this.cmbSendEmployee.TabIndex = 37;
            // 
            // lblSendEmployee
            // 
            this.lblSendEmployee.AutoSize = true;
            this.lblSendEmployee.BackColor = System.Drawing.Color.Transparent;
            this.lblSendEmployee.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblSendEmployee.Location = new System.Drawing.Point(6, 8);
            this.lblSendEmployee.Name = "lblSendEmployee";
            this.lblSendEmployee.Size = new System.Drawing.Size(53, 13);
            this.lblSendEmployee.TabIndex = 39;
            this.lblSendEmployee.Text = "Employee";
            // 
            // cbSendAllEmployees
            // 
            this.cbSendAllEmployees.AutoSize = true;
            this.cbSendAllEmployees.BackColor = System.Drawing.Color.Transparent;
            this.cbSendAllEmployees.Checked = true;
            this.cbSendAllEmployees.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbSendAllEmployees.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.cbSendAllEmployees.Location = new System.Drawing.Point(329, 7);
            this.cbSendAllEmployees.Name = "cbSendAllEmployees";
            this.cbSendAllEmployees.Size = new System.Drawing.Size(37, 17);
            this.cbSendAllEmployees.TabIndex = 38;
            this.cbSendAllEmployees.Text = "All";
            this.cbSendAllEmployees.UseVisualStyleBackColor = false;
            this.cbSendAllEmployees.CheckedChanged += new System.EventHandler(this.cbSendAllEmployees_CheckedChanged);
            // 
            // gbFilterSend
            // 
            this.gbFilterSend.Controls.Add(this.cbSendTax);
            this.gbFilterSend.Controls.Add(this.lblDocNo);
            this.gbFilterSend.Controls.Add(this.lblOR);
            this.gbFilterSend.Controls.Add(this.txtInv);
            this.gbFilterSend.Controls.Add(this.dtpSendToDate);
            this.gbFilterSend.Controls.Add(this.dtpSendFromDate);
            this.gbFilterSend.Controls.Add(this.lblSendFromDate);
            this.gbFilterSend.Controls.Add(this.lblSendToDate);
            this.gbFilterSend.Location = new System.Drawing.Point(7, 133);
            this.gbFilterSend.Name = "gbFilterSend";
            this.gbFilterSend.Size = new System.Drawing.Size(490, 60);
            this.gbFilterSend.TabIndex = 2;
            this.gbFilterSend.TabStop = false;
            this.gbFilterSend.Text = "Filter On";
            // 
            // cbSendTax
            // 
            this.cbSendTax.AutoSize = true;
            this.cbSendTax.Checked = true;
            this.cbSendTax.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbSendTax.Location = new System.Drawing.Point(202, 38);
            this.cbSendTax.Name = "cbSendTax";
            this.cbSendTax.Size = new System.Drawing.Size(253, 17);
            this.cbSendTax.TabIndex = 48;
            this.cbSendTax.Text = "Send Tax (Applicable to Sales and Orders Only)";
            this.cbSendTax.UseVisualStyleBackColor = true;
            this.cbSendTax.Visible = false;
            // 
            // lblDocNo
            // 
            this.lblDocNo.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblDocNo.Location = new System.Drawing.Point(8, 39);
            this.lblDocNo.Name = "lblDocNo";
            this.lblDocNo.Size = new System.Drawing.Size(47, 13);
            this.lblDocNo.TabIndex = 47;
            this.lblDocNo.Text = "Doc No";
            this.lblDocNo.Visible = false;
            // 
            // lblOR
            // 
            this.lblOR.AutoSize = true;
            this.lblOR.BackColor = System.Drawing.Color.Transparent;
            this.lblOR.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblOR.ForeColor = System.Drawing.Color.Red;
            this.lblOR.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblOR.Location = new System.Drawing.Point(389, 29);
            this.lblOR.Name = "lblOR";
            this.lblOR.Size = new System.Drawing.Size(25, 13);
            this.lblOR.TabIndex = 46;
            this.lblOR.Text = "OR";
            this.lblOR.Visible = false;
            // 
            // txtInv
            // 
            this.txtInv.Location = new System.Drawing.Point(73, 36);
            this.txtInv.Name = "txtInv";
            this.txtInv.Size = new System.Drawing.Size(116, 20);
            this.txtInv.TabIndex = 30;
            this.txtInv.Visible = false;
            // 
            // dtpSendToDate
            // 
            this.dtpSendToDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpSendToDate.Location = new System.Drawing.Point(250, 14);
            this.dtpSendToDate.Name = "dtpSendToDate";
            this.dtpSendToDate.Size = new System.Drawing.Size(117, 20);
            this.dtpSendToDate.TabIndex = 29;
            // 
            // dtpSendFromDate
            // 
            this.dtpSendFromDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpSendFromDate.Location = new System.Drawing.Point(73, 14);
            this.dtpSendFromDate.Name = "dtpSendFromDate";
            this.dtpSendFromDate.Size = new System.Drawing.Size(116, 20);
            this.dtpSendFromDate.TabIndex = 29;
            // 
            // lblSendFromDate
            // 
            this.lblSendFromDate.AutoSize = true;
            this.lblSendFromDate.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblSendFromDate.Location = new System.Drawing.Point(8, 18);
            this.lblSendFromDate.Name = "lblSendFromDate";
            this.lblSendFromDate.Size = new System.Drawing.Size(57, 13);
            this.lblSendFromDate.TabIndex = 28;
            this.lblSendFromDate.Text = "From Date";
            // 
            // lblSendToDate
            // 
            this.lblSendToDate.AutoSize = true;
            this.lblSendToDate.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblSendToDate.Location = new System.Drawing.Point(199, 18);
            this.lblSendToDate.Name = "lblSendToDate";
            this.lblSendToDate.Size = new System.Drawing.Size(45, 13);
            this.lblSendToDate.TabIndex = 28;
            this.lblSendToDate.Text = "To Date";
            // 
            // btnSendToERP
            // 
            this.btnSendToERP.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnSendToERP.Location = new System.Drawing.Point(9, 197);
            this.btnSendToERP.Name = "btnSendToERP";
            this.btnSendToERP.Size = new System.Drawing.Size(118, 30);
            this.btnSendToERP.TabIndex = 35;
            this.btnSendToERP.Text = "Start Send";
            this.btnSendToERP.Click += new System.EventHandler(this.btnSendToERP_Click);
            // 
            // tpSpecial
            // 
            this.tpSpecial.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(194)))), ((int)(((byte)(217)))), ((int)(((byte)(247)))));
            this.tpSpecial.Controls.Add(this.panel1);
            this.tpSpecial.Controls.Add(this.gpSpecialActions);
            this.tpSpecial.Controls.Add(this.btnRunSpecialActions);
            this.tpSpecial.Controls.Add(this.SpecialAccessProgressBar);
            this.tpSpecial.Location = new System.Drawing.Point(4, 22);
            this.tpSpecial.Name = "tpSpecial";
            this.tpSpecial.Size = new System.Drawing.Size(1107, 269);
            this.tpSpecial.TabIndex = 2;
            this.tpSpecial.Text = "Special Functions";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.dtpSF_ToDate);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.dtpSF_FromDate);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Location = new System.Drawing.Point(8, 5);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(489, 29);
            this.panel1.TabIndex = 94;
            // 
            // dtpSF_ToDate
            // 
            this.dtpSF_ToDate.Checked = false;
            this.dtpSF_ToDate.Location = new System.Drawing.Point(281, 4);
            this.dtpSF_ToDate.Name = "dtpSF_ToDate";
            this.dtpSF_ToDate.Size = new System.Drawing.Size(200, 20);
            this.dtpSF_ToDate.TabIndex = 42;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label2.Location = new System.Drawing.Point(255, 8);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(19, 13);
            this.label2.TabIndex = 41;
            this.label2.Text = "To";
            // 
            // dtpSF_FromDate
            // 
            this.dtpSF_FromDate.Checked = false;
            this.dtpSF_FromDate.Location = new System.Drawing.Point(38, 4);
            this.dtpSF_FromDate.Name = "dtpSF_FromDate";
            this.dtpSF_FromDate.Size = new System.Drawing.Size(200, 20);
            this.dtpSF_FromDate.TabIndex = 40;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label3.Location = new System.Drawing.Point(6, 8);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(31, 13);
            this.label3.TabIndex = 39;
            this.label3.Text = "From";
            // 
            // gpSpecialActions
            // 
            this.gpSpecialActions.Controls.Add(this.lsvSpecialActions);
            this.gpSpecialActions.Controls.Add(this.cbRunAllSpecialActions);
            this.gpSpecialActions.Location = new System.Drawing.Point(7, 39);
            this.gpSpecialActions.Name = "gpSpecialActions";
            this.gpSpecialActions.Size = new System.Drawing.Size(487, 90);
            this.gpSpecialActions.TabIndex = 48;
            this.gpSpecialActions.TabStop = false;
            this.gpSpecialActions.Text = "Run The Following Actions";
            // 
            // lsvSpecialActions
            // 
            this.lsvSpecialActions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lsvSpecialActions.CheckBoxes = true;
            this.lsvSpecialActions.HideSelection = false;
            this.lsvSpecialActions.Location = new System.Drawing.Point(9, 19);
            this.lsvSpecialActions.Name = "lsvSpecialActions";
            this.lsvSpecialActions.Size = new System.Drawing.Size(428, 65);
            this.lsvSpecialActions.TabIndex = 34;
            this.lsvSpecialActions.UseCompatibleStateImageBehavior = false;
            this.lsvSpecialActions.View = System.Windows.Forms.View.List;
            // 
            // cbRunAllSpecialActions
            // 
            this.cbRunAllSpecialActions.AutoSize = true;
            this.cbRunAllSpecialActions.Location = new System.Drawing.Point(443, 19);
            this.cbRunAllSpecialActions.Name = "cbRunAllSpecialActions";
            this.cbRunAllSpecialActions.Size = new System.Drawing.Size(37, 17);
            this.cbRunAllSpecialActions.TabIndex = 35;
            this.cbRunAllSpecialActions.Text = "All";
            this.cbRunAllSpecialActions.UseVisualStyleBackColor = true;
            // 
            // btnRunSpecialActions
            // 
            this.btnRunSpecialActions.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnRunSpecialActions.Location = new System.Drawing.Point(9, 197);
            this.btnRunSpecialActions.Name = "btnRunSpecialActions";
            this.btnRunSpecialActions.Size = new System.Drawing.Size(118, 30);
            this.btnRunSpecialActions.TabIndex = 47;
            this.btnRunSpecialActions.Text = "Run";
            this.btnRunSpecialActions.Click += new System.EventHandler(this.btnRunSpecialActions_Click);
            // 
            // SpecialAccessProgressBar
            // 
            this.SpecialAccessProgressBar.CustomText = null;
            this.SpecialAccessProgressBar.DisplayStyle = InCubeIntegration_UI.ProgressBarDisplayText.Mixed;
            this.SpecialAccessProgressBar.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.SpecialAccessProgressBar.Location = new System.Drawing.Point(9, 236);
            this.SpecialAccessProgressBar.Name = "SpecialAccessProgressBar";
            this.SpecialAccessProgressBar.Size = new System.Drawing.Size(488, 29);
            this.SpecialAccessProgressBar.Step = 30;
            this.SpecialAccessProgressBar.TabIndex = 93;
            // 
            // lblVersion
            // 
            this.lblVersion.Font = new System.Drawing.Font("Tahoma", 10F);
            this.lblVersion.Location = new System.Drawing.Point(368, 4);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(149, 23);
            this.lblVersion.TabIndex = 91;
            this.lblVersion.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 309);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 13);
            this.label1.TabIndex = 92;
            this.label1.Text = "Output";
            // 
            // itF_O_S_EnvRecArqPalmService1
            // 
            this.itF_O_S_EnvRecArqPalmService1.Credentials = null;
            this.itF_O_S_EnvRecArqPalmService1.Url = "http://spouxp2d:50200/XISOAPAdapter/MessageServlet?channel=:BS_PALM_05:CC_PALM_SO" +
    "AP_SND&version=3.0&Sender.Service=CC_PALM_SOAP_SND&Interface=ITF_O_S_EnvRecArqPa" +
    "lm^BS_PALM_05";
            this.itF_O_S_EnvRecArqPalmService1.UseDefaultCredentials = false;
            // 
            // frmIntegration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(194)))), ((int)(((byte)(217)))), ((int)(((byte)(247)))));
            this.ClientSize = new System.Drawing.Size(1127, 476);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.txtMessages);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "frmIntegration";
            this.Padding = new System.Windows.Forms.Padding(5);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Sonic Integration Utility";
            this.Load += new System.EventHandler(this.frmIntegration_Load);
            this.tabControl1.ResumeLayout(false);
            this.tpImport.ResumeLayout(false);
            this.tpImport.PerformLayout();
            this.pnlTextSearchFilter.ResumeLayout(false);
            this.pnlTextSearchFilter.PerformLayout();
            this.pnlImportEmployeeFilter.ResumeLayout(false);
            this.pnlImportEmployeeFilter.PerformLayout();
            this.pnlMasterDateFilter.ResumeLayout(false);
            this.pnlMasterDateFilter.PerformLayout();
            this.gbUpdateStock.ResumeLayout(false);
            this.gbUpdateStock.PerformLayout();
            this.gbImportItems.ResumeLayout(false);
            this.gbImportItems.PerformLayout();
            this.tpSend.ResumeLayout(false);
            this.gbSendItems.ResumeLayout(false);
            this.gbSendItems.PerformLayout();
            this.pnlSendEmployeeFilter.ResumeLayout(false);
            this.pnlSendEmployeeFilter.PerformLayout();
            this.gbFilterSend.ResumeLayout(false);
            this.gbFilterSend.PerformLayout();
            this.tpSpecial.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.gpSpecialActions.ResumeLayout(false);
            this.gpSpecialActions.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

#endregion

        private System.Windows.Forms.TextBox txtMessages;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tpImport;
        private System.Windows.Forms.Panel pnlImportEmployeeFilter;
        private System.Windows.Forms.Label lblImportFromDate;
        private System.Windows.Forms.GroupBox gbImportItems;
        private System.Windows.Forms.ListView lsvUpdateItems;
        private System.Windows.Forms.CheckBox cbUpdateAllItems;
        private System.Windows.Forms.Button btnUpdateFromERP;
        private System.Windows.Forms.CheckBox ChkUpdateStock;
        private System.Windows.Forms.GroupBox gbUpdateStock;
        private System.Windows.Forms.Label lblStockDate;
        private System.Windows.Forms.DateTimePicker dtpStockDate;
        private System.Windows.Forms.Label lblWarehouse;
        private System.Windows.Forms.ComboBox cmbWarehouse;
        private System.Windows.Forms.CheckBox cbxUpdateStockAllVans;
        private System.Windows.Forms.TabPage tpSend;
        private System.Windows.Forms.GroupBox gbFilterSend;
        private System.Windows.Forms.TextBox txtInv;
        private System.Windows.Forms.DateTimePicker dtpSendToDate;
        private System.Windows.Forms.DateTimePicker dtpSendFromDate;
        private System.Windows.Forms.Label lblSendFromDate;
        private System.Windows.Forms.Label lblSendToDate;
        private System.Windows.Forms.Button btnSendToERP;
        private System.Windows.Forms.GroupBox gbSendItems;
        private System.Windows.Forms.ListView lsvSendItems;
        private System.Windows.Forms.CheckBox cbSendAllItems;
        private System.Windows.Forms.Panel pnlSendEmployeeFilter;
        private System.Windows.Forms.ComboBox cmbSendEmployee;
        private System.Windows.Forms.Label lblSendEmployee;
        private System.Windows.Forms.CheckBox cbSendAllEmployees;
        private System.Windows.Forms.Label lblDocNo;
        private System.Windows.Forms.Label lblOR;
        private System.Windows.Forms.Label lblVersion;
#if LegacyUI
        private System.Windows.Forms.ProgressBar SendProgressBar;
        private System.Windows.Forms.ProgressBar UpdateProgressBar;
        private System.Windows.Forms.ProgressBar SpecialAccessProgressBar;
#else
        private CustomProgressBar SendProgressBar;
        private CustomProgressBar UpdateProgressBar;
        private CustomProgressBar SpecialAccessProgressBar;
#endif
        private System.Windows.Forms.Panel pnlMasterDateFilter;
        private System.Windows.Forms.ComboBox cmbImportEmployee;
        private System.Windows.Forms.Label lblImportEmployee;
        private System.Windows.Forms.CheckBox cbImportAllEmployees;
        private System.Windows.Forms.DateTimePicker dtpImportToDate;
        private System.Windows.Forms.Label lmlImportToDate;
        private System.Windows.Forms.DateTimePicker dtpImportFromDate;
        private System.Windows.Forms.CheckBox cbOpenInvoicesOnly;
        private System.Windows.Forms.TextBox txtCustCode;
        private System.Windows.Forms.CheckBox cbSendTax;
        private System.Windows.Forms.TabPage tpSpecial;
        private System.Windows.Forms.GroupBox gpSpecialActions;
        private System.Windows.Forms.ListView lsvSpecialActions;
        private System.Windows.Forms.CheckBox cbRunAllSpecialActions;
        private System.Windows.Forms.Button btnRunSpecialActions;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.DateTimePicker dtpSF_ToDate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DateTimePicker dtpSF_FromDate;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Panel pnlTextSearchFilter;
        private System.Windows.Forms.TextBox txtTextSearch;
        private System.Windows.Forms.Label lblFormWidth;
        private InCubeIntegration_BL.BRF_SAP_WS.ITF_O_S_EnvRecArqPalmService itF_O_S_EnvRecArqPalmService1;
    }
}
