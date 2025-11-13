namespace InCubeIntegration_UI
{
    partial class frmEmployeesImportingMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmEmployeesImportingMain));
            this.btnUpdateEmployee = new System.Windows.Forms.Button();
            this.cmbWarehouse = new System.Windows.Forms.ComboBox();
            this.cmbOrganization = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtEmployeeCode = new System.Windows.Forms.TextBox();
            this.txtEmployeeName = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtDeviceSerial = new System.Windows.Forms.TextBox();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtUserName = new System.Windows.Forms.TextBox();
            this.txtVehicleCode = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.btnFillFields = new System.Windows.Forms.Button();
            this.tblPrimary = new System.Windows.Forms.TableLayoutPanel();
            this.lblEmailNatID = new System.Windows.Forms.Label();
            this.txtEmailNatID = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.cmbSecurityGroup = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.txtDeviceName = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.txtOrderSeq = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.txtPaymentSeq = new System.Windows.Forms.TextBox();
            this.label20 = new System.Windows.Forms.Label();
            this.txtAppPaymentSeq = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.txtReturnOrderSeq = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.txtTerritoryCode = new System.Windows.Forms.TextBox();
            this.label18 = new System.Windows.Forms.Label();
            this.txtRouteCode = new System.Windows.Forms.TextBox();
            this.tblExtended = new System.Windows.Forms.TableLayoutPanel();
            this.txtInvoiceSeq = new System.Windows.Forms.TextBox();
            this.label21 = new System.Windows.Forms.Label();
            this.txtNewCustSeq = new System.Windows.Forms.TextBox();
            this.label22 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.tblPrimary.SuspendLayout();
            this.tblExtended.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnUpdateEmployee
            // 
            this.btnUpdateEmployee.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnUpdateEmployee.Enabled = false;
            this.btnUpdateEmployee.Location = new System.Drawing.Point(548, 143);
            this.btnUpdateEmployee.Name = "btnUpdateEmployee";
            this.btnUpdateEmployee.Size = new System.Drawing.Size(138, 29);
            this.btnUpdateEmployee.TabIndex = 10;
            this.btnUpdateEmployee.Text = "Add / Update Employee";
            this.btnUpdateEmployee.UseVisualStyleBackColor = true;
            this.btnUpdateEmployee.Click += new System.EventHandler(this.btnUpdateEmployee_Click);
            // 
            // cmbWarehouse
            // 
            this.cmbWarehouse.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbWarehouse.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbWarehouse.FormattingEnabled = true;
            this.cmbWarehouse.Location = new System.Drawing.Point(450, 3);
            this.cmbWarehouse.Name = "cmbWarehouse";
            this.cmbWarehouse.Size = new System.Drawing.Size(236, 21);
            this.cmbWarehouse.TabIndex = 1;
            this.cmbWarehouse.SelectedIndexChanged += new System.EventHandler(this.cmbWarehouse_SelectedIndexChanged);
            // 
            // cmbOrganization
            // 
            this.cmbOrganization.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbOrganization.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbOrganization.FormattingEnabled = true;
            this.cmbOrganization.Location = new System.Drawing.Point(106, 3);
            this.cmbOrganization.Name = "cmbOrganization";
            this.cmbOrganization.Size = new System.Drawing.Size(235, 21);
            this.cmbOrganization.TabIndex = 0;
            this.cmbOrganization.SelectedIndexChanged += new System.EventHandler(this.cmbOrganization_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Left;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 28);
            this.label1.TabIndex = 3;
            this.label1.Text = "Organization";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Left;
            this.label2.Location = new System.Drawing.Point(347, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 28);
            this.label2.TabIndex = 4;
            this.label2.Text = "Warehouse";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(3, 56);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(97, 28);
            this.label3.TabIndex = 6;
            this.label3.Text = "Employee Code";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtEmployeeCode
            // 
            this.txtEmployeeCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtEmployeeCode.Location = new System.Drawing.Point(106, 59);
            this.txtEmployeeCode.Name = "txtEmployeeCode";
            this.txtEmployeeCode.Size = new System.Drawing.Size(235, 20);
            this.txtEmployeeCode.TabIndex = 4;
            this.txtEmployeeCode.TextChanged += new System.EventHandler(this.txtEmployeeCode_TextChanged);
            this.txtEmployeeCode.KeyUp += new System.Windows.Forms.KeyEventHandler(this.txtEmployeeCode_KeyUp);
            // 
            // txtEmployeeName
            // 
            this.txtEmployeeName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtEmployeeName.Location = new System.Drawing.Point(450, 59);
            this.txtEmployeeName.Name = "txtEmployeeName";
            this.txtEmployeeName.Size = new System.Drawing.Size(236, 20);
            this.txtEmployeeName.TabIndex = 5;
            this.txtEmployeeName.TextChanged += new System.EventHandler(this.txtEmployeeName_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(347, 56);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(97, 28);
            this.label4.TabIndex = 8;
            this.label4.Text = "Employee Name";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtDeviceSerial
            // 
            this.txtDeviceSerial.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDeviceSerial.Location = new System.Drawing.Point(450, 31);
            this.txtDeviceSerial.Name = "txtDeviceSerial";
            this.txtDeviceSerial.Size = new System.Drawing.Size(236, 20);
            this.txtDeviceSerial.TabIndex = 3;
            this.txtDeviceSerial.TextChanged += new System.EventHandler(this.txtDeviceSerial_TextChanged);
            // 
            // txtPassword
            // 
            this.txtPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPassword.Location = new System.Drawing.Point(450, 87);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(236, 20);
            this.txtPassword.TabIndex = 7;
            this.txtPassword.TextChanged += new System.EventHandler(this.txtPassword_TextChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Location = new System.Drawing.Point(347, 28);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(97, 28);
            this.label6.TabIndex = 12;
            this.label6.Text = "Device Serial";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtUserName
            // 
            this.txtUserName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtUserName.Location = new System.Drawing.Point(106, 87);
            this.txtUserName.Name = "txtUserName";
            this.txtUserName.Size = new System.Drawing.Size(235, 20);
            this.txtUserName.TabIndex = 6;
            this.txtUserName.TextChanged += new System.EventHandler(this.txtUserName_TextChanged);
            // 
            // txtVehicleCode
            // 
            this.txtVehicleCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtVehicleCode.Location = new System.Drawing.Point(106, 3);
            this.txtVehicleCode.Name = "txtVehicleCode";
            this.txtVehicleCode.Size = new System.Drawing.Size(235, 20);
            this.txtVehicleCode.TabIndex = 10;
            this.txtVehicleCode.TabStop = false;
            this.txtVehicleCode.TextChanged += new System.EventHandler(this.txtVehicleCode_TextChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label7.Location = new System.Drawing.Point(3, 84);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(97, 28);
            this.label7.TabIndex = 18;
            this.label7.Text = "User Name";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label8.Location = new System.Drawing.Point(347, 84);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(97, 28);
            this.label8.TabIndex = 19;
            this.label8.Text = "Password";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnFillFields
            // 
            this.btnFillFields.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnFillFields.Enabled = false;
            this.btnFillFields.Location = new System.Drawing.Point(548, 143);
            this.btnFillFields.Name = "btnFillFields";
            this.btnFillFields.Size = new System.Drawing.Size(138, 29);
            this.btnFillFields.TabIndex = 9;
            this.btnFillFields.Text = "Fill Other Fields";
            this.btnFillFields.UseVisualStyleBackColor = true;
            this.btnFillFields.Click += new System.EventHandler(this.btnFillFields_Click);
            // 
            // tblPrimary
            // 
            this.tblPrimary.ColumnCount = 4;
            this.tblPrimary.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tblPrimary.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tblPrimary.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tblPrimary.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tblPrimary.Controls.Add(this.lblEmailNatID, 0, 4);
            this.tblPrimary.Controls.Add(this.txtEmailNatID, 1, 4);
            this.tblPrimary.Controls.Add(this.label9, 0, 1);
            this.tblPrimary.Controls.Add(this.btnFillFields, 3, 5);
            this.tblPrimary.Controls.Add(this.cmbSecurityGroup, 1, 1);
            this.tblPrimary.Controls.Add(this.label1, 0, 0);
            this.tblPrimary.Controls.Add(this.cmbOrganization, 1, 0);
            this.tblPrimary.Controls.Add(this.label7, 0, 3);
            this.tblPrimary.Controls.Add(this.label2, 2, 0);
            this.tblPrimary.Controls.Add(this.txtUserName, 1, 3);
            this.tblPrimary.Controls.Add(this.txtPassword, 3, 3);
            this.tblPrimary.Controls.Add(this.cmbWarehouse, 3, 0);
            this.tblPrimary.Controls.Add(this.txtEmployeeCode, 1, 2);
            this.tblPrimary.Controls.Add(this.label6, 2, 1);
            this.tblPrimary.Controls.Add(this.txtEmployeeName, 3, 2);
            this.tblPrimary.Controls.Add(this.txtDeviceSerial, 3, 1);
            this.tblPrimary.Controls.Add(this.label3, 0, 2);
            this.tblPrimary.Controls.Add(this.label8, 2, 3);
            this.tblPrimary.Controls.Add(this.label4, 2, 2);
            this.tblPrimary.Location = new System.Drawing.Point(12, 32);
            this.tblPrimary.Name = "tblPrimary";
            this.tblPrimary.RowCount = 6;
            this.tblPrimary.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16F));
            this.tblPrimary.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16F));
            this.tblPrimary.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16F));
            this.tblPrimary.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16F));
            this.tblPrimary.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16F));
            this.tblPrimary.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tblPrimary.Size = new System.Drawing.Size(689, 175);
            this.tblPrimary.TabIndex = 22;
            // 
            // lblEmailNatID
            // 
            this.lblEmailNatID.AutoSize = true;
            this.lblEmailNatID.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEmailNatID.Location = new System.Drawing.Point(3, 112);
            this.lblEmailNatID.Name = "lblEmailNatID";
            this.lblEmailNatID.Size = new System.Drawing.Size(97, 28);
            this.lblEmailNatID.TabIndex = 21;
            this.lblEmailNatID.Text = "Email";
            this.lblEmailNatID.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtEmailNatID
            // 
            this.txtEmailNatID.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtEmailNatID.Location = new System.Drawing.Point(106, 115);
            this.txtEmailNatID.Name = "txtEmailNatID";
            this.txtEmailNatID.Size = new System.Drawing.Size(235, 20);
            this.txtEmailNatID.TabIndex = 8;
            this.txtEmailNatID.TextChanged += new System.EventHandler(this.txtEmail_TextChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label9.Location = new System.Drawing.Point(3, 28);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(97, 28);
            this.label9.TabIndex = 6;
            this.label9.Text = "Security Group";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmbSecurityGroup
            // 
            this.cmbSecurityGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbSecurityGroup.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSecurityGroup.FormattingEnabled = true;
            this.cmbSecurityGroup.Location = new System.Drawing.Point(106, 31);
            this.cmbSecurityGroup.Name = "cmbSecurityGroup";
            this.cmbSecurityGroup.Size = new System.Drawing.Size(235, 21);
            this.cmbSecurityGroup.TabIndex = 2;
            this.cmbSecurityGroup.SelectedIndexChanged += new System.EventHandler(this.cmbSecurityGroup_SelectedIndexChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Tahoma", 10F);
            this.label10.Location = new System.Drawing.Point(15, 9);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(120, 17);
            this.label10.TabIndex = 23;
            this.label10.Text = "Primary Properties";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Tahoma", 10F);
            this.label11.Location = new System.Drawing.Point(15, 219);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(132, 17);
            this.label11.TabIndex = 24;
            this.label11.Text = "Extended Properties";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label12.Location = new System.Drawing.Point(3, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(97, 28);
            this.label12.TabIndex = 25;
            this.label12.Text = "Vehicle Code";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label13.Location = new System.Drawing.Point(347, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(97, 28);
            this.label13.TabIndex = 27;
            this.label13.Text = "Device Name";
            this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtDeviceName
            // 
            this.txtDeviceName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDeviceName.Location = new System.Drawing.Point(450, 3);
            this.txtDeviceName.Name = "txtDeviceName";
            this.txtDeviceName.Size = new System.Drawing.Size(236, 20);
            this.txtDeviceName.TabIndex = 10;
            this.txtDeviceName.TabStop = false;
            this.txtDeviceName.TextChanged += new System.EventHandler(this.txtDeviceName_TextChanged);
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label14.Location = new System.Drawing.Point(3, 56);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(97, 28);
            this.label14.TabIndex = 29;
            this.label14.Text = "Order Seq";
            this.label14.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtOrderSeq
            // 
            this.txtOrderSeq.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtOrderSeq.Location = new System.Drawing.Point(106, 59);
            this.txtOrderSeq.Name = "txtOrderSeq";
            this.txtOrderSeq.Size = new System.Drawing.Size(235, 20);
            this.txtOrderSeq.TabIndex = 13;
            this.txtOrderSeq.TabStop = false;
            this.txtOrderSeq.TextChanged += new System.EventHandler(this.txtOrderSeq_TextChanged);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label15.Location = new System.Drawing.Point(347, 56);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(97, 28);
            this.label15.TabIndex = 31;
            this.label15.Text = "Payment Seq";
            this.label15.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtPaymentSeq
            // 
            this.txtPaymentSeq.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPaymentSeq.Location = new System.Drawing.Point(450, 59);
            this.txtPaymentSeq.Name = "txtPaymentSeq";
            this.txtPaymentSeq.Size = new System.Drawing.Size(236, 20);
            this.txtPaymentSeq.TabIndex = 14;
            this.txtPaymentSeq.TabStop = false;
            this.txtPaymentSeq.TextChanged += new System.EventHandler(this.txtPaymentSeq_TextChanged);
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label20.Location = new System.Drawing.Point(347, 84);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(97, 28);
            this.label20.TabIndex = 33;
            this.label20.Text = "Applied Payment Seq";
            this.label20.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtAppPaymentSeq
            // 
            this.txtAppPaymentSeq.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtAppPaymentSeq.Location = new System.Drawing.Point(450, 87);
            this.txtAppPaymentSeq.Name = "txtAppPaymentSeq";
            this.txtAppPaymentSeq.Size = new System.Drawing.Size(236, 20);
            this.txtAppPaymentSeq.TabIndex = 16;
            this.txtAppPaymentSeq.TabStop = false;
            this.txtAppPaymentSeq.TextChanged += new System.EventHandler(this.txtAppPaymentSeq_TextChanged);
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label17.Location = new System.Drawing.Point(3, 84);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(97, 28);
            this.label17.TabIndex = 35;
            this.label17.Text = "Return Order Seq";
            this.label17.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtReturnOrderSeq
            // 
            this.txtReturnOrderSeq.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtReturnOrderSeq.Location = new System.Drawing.Point(106, 87);
            this.txtReturnOrderSeq.Name = "txtReturnOrderSeq";
            this.txtReturnOrderSeq.Size = new System.Drawing.Size(235, 20);
            this.txtReturnOrderSeq.TabIndex = 15;
            this.txtReturnOrderSeq.TabStop = false;
            this.txtReturnOrderSeq.TextChanged += new System.EventHandler(this.txtReturnOrderSeq_TextChanged);
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label16.Location = new System.Drawing.Point(3, 28);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(97, 28);
            this.label16.TabIndex = 37;
            this.label16.Text = "Territory Code";
            this.label16.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtTerritoryCode
            // 
            this.txtTerritoryCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtTerritoryCode.Location = new System.Drawing.Point(106, 31);
            this.txtTerritoryCode.Name = "txtTerritoryCode";
            this.txtTerritoryCode.ReadOnly = true;
            this.txtTerritoryCode.Size = new System.Drawing.Size(235, 20);
            this.txtTerritoryCode.TabIndex = 11;
            this.txtTerritoryCode.TabStop = false;
            this.txtTerritoryCode.TextChanged += new System.EventHandler(this.txtTerritoryCode_TextChanged);
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label18.Location = new System.Drawing.Point(347, 28);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(97, 28);
            this.label18.TabIndex = 39;
            this.label18.Text = "Route Code";
            this.label18.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtRouteCode
            // 
            this.txtRouteCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtRouteCode.Location = new System.Drawing.Point(450, 31);
            this.txtRouteCode.Name = "txtRouteCode";
            this.txtRouteCode.ReadOnly = true;
            this.txtRouteCode.Size = new System.Drawing.Size(236, 20);
            this.txtRouteCode.TabIndex = 12;
            this.txtRouteCode.TabStop = false;
            this.txtRouteCode.TextChanged += new System.EventHandler(this.txtRouteCode_TextChanged);
            // 
            // tblExtended
            // 
            this.tblExtended.ColumnCount = 4;
            this.tblExtended.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tblExtended.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tblExtended.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tblExtended.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tblExtended.Controls.Add(this.txtInvoiceSeq, 1, 4);
            this.tblExtended.Controls.Add(this.label21, 0, 4);
            this.tblExtended.Controls.Add(this.txtNewCustSeq, 3, 4);
            this.tblExtended.Controls.Add(this.label22, 2, 4);
            this.tblExtended.Controls.Add(this.label19, 1, 5);
            this.tblExtended.Controls.Add(this.label5, 0, 5);
            this.tblExtended.Controls.Add(this.label12, 0, 0);
            this.tblExtended.Controls.Add(this.label18, 2, 1);
            this.tblExtended.Controls.Add(this.txtVehicleCode, 1, 0);
            this.tblExtended.Controls.Add(this.txtRouteCode, 3, 1);
            this.tblExtended.Controls.Add(this.label13, 2, 0);
            this.tblExtended.Controls.Add(this.btnUpdateEmployee, 3, 5);
            this.tblExtended.Controls.Add(this.txtTerritoryCode, 1, 1);
            this.tblExtended.Controls.Add(this.label16, 0, 1);
            this.tblExtended.Controls.Add(this.txtDeviceName, 3, 0);
            this.tblExtended.Controls.Add(this.label14, 0, 2);
            this.tblExtended.Controls.Add(this.txtReturnOrderSeq, 1, 3);
            this.tblExtended.Controls.Add(this.label17, 0, 3);
            this.tblExtended.Controls.Add(this.txtOrderSeq, 1, 2);
            this.tblExtended.Controls.Add(this.label15, 2, 2);
            this.tblExtended.Controls.Add(this.txtAppPaymentSeq, 3, 3);
            this.tblExtended.Controls.Add(this.label20, 2, 3);
            this.tblExtended.Controls.Add(this.txtPaymentSeq, 3, 2);
            this.tblExtended.Enabled = false;
            this.tblExtended.Location = new System.Drawing.Point(12, 248);
            this.tblExtended.Name = "tblExtended";
            this.tblExtended.RowCount = 6;
            this.tblExtended.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16F));
            this.tblExtended.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16F));
            this.tblExtended.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16F));
            this.tblExtended.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16F));
            this.tblExtended.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16F));
            this.tblExtended.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tblExtended.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tblExtended.Size = new System.Drawing.Size(689, 175);
            this.tblExtended.TabIndex = 40;
            // 
            // txtInvoiceSeq
            // 
            this.txtInvoiceSeq.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtInvoiceSeq.Location = new System.Drawing.Point(106, 115);
            this.txtInvoiceSeq.Name = "txtInvoiceSeq";
            this.txtInvoiceSeq.Size = new System.Drawing.Size(235, 20);
            this.txtInvoiceSeq.TabIndex = 43;
            this.txtInvoiceSeq.TabStop = false;
            this.txtInvoiceSeq.TextChanged += new System.EventHandler(this.txtInvoiceSeq_TextChanged);
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label21.Location = new System.Drawing.Point(3, 112);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(97, 28);
            this.label21.TabIndex = 46;
            this.label21.Text = "Invoice Seq";
            this.label21.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtNewCustSeq
            // 
            this.txtNewCustSeq.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtNewCustSeq.Location = new System.Drawing.Point(450, 115);
            this.txtNewCustSeq.Name = "txtNewCustSeq";
            this.txtNewCustSeq.Size = new System.Drawing.Size(236, 20);
            this.txtNewCustSeq.TabIndex = 44;
            this.txtNewCustSeq.TabStop = false;
            this.txtNewCustSeq.TextChanged += new System.EventHandler(this.txtNewCustSeq_TextChanged);
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label22.Location = new System.Drawing.Point(347, 112);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(97, 28);
            this.label22.TabIndex = 45;
            this.label22.Text = "New Customer Seq";
            this.label22.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label19.ForeColor = System.Drawing.Color.Red;
            this.label19.Location = new System.Drawing.Point(106, 140);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(235, 35);
            this.label19.TabIndex = 42;
            this.label19.Text = "Document sequence will be updated only if it doesn\'t exists";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.ForeColor = System.Drawing.Color.Red;
            this.label5.Location = new System.Drawing.Point(3, 140);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(97, 35);
            this.label5.TabIndex = 41;
            this.label5.Text = "** Note:";
            // 
            // frmEmployeesImportingMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(240)))), ((int)(((byte)(250)))));
            this.ClientSize = new System.Drawing.Size(715, 433);
            this.Controls.Add(this.tblExtended);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.tblPrimary);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmEmployeesImportingMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Update Employee";
            this.Load += new System.EventHandler(this.frmEmployeesImportingMain_Load);
            this.tblPrimary.ResumeLayout(false);
            this.tblPrimary.PerformLayout();
            this.tblExtended.ResumeLayout(false);
            this.tblExtended.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnUpdateEmployee;
        private System.Windows.Forms.ComboBox cmbWarehouse;
        private System.Windows.Forms.ComboBox cmbOrganization;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtEmployeeCode;
        private System.Windows.Forms.TextBox txtEmployeeName;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtDeviceSerial;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtUserName;
        private System.Windows.Forms.TextBox txtVehicleCode;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button btnFillFields;
        private System.Windows.Forms.TableLayoutPanel tblPrimary;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox cmbSecurityGroup;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtDeviceName;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox txtOrderSeq;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox txtPaymentSeq;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.TextBox txtAppPaymentSeq;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.TextBox txtReturnOrderSeq;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox txtTerritoryCode;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox txtRouteCode;
        private System.Windows.Forms.TableLayoutPanel tblExtended;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtInvoiceSeq;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.TextBox txtNewCustSeq;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Label lblEmailNatID;
        private System.Windows.Forms.TextBox txtEmailNatID;
    }
}