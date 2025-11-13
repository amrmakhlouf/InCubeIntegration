using InCubeIntegration_DAL;
using InCubeLibrary;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;
using InCubeIntegration_BL;

namespace InCubeIntegration_UI
{
    public partial class frmEmployeesImportingMain : Form
    {
        SqlConnection sqlConn;
        SqlDataAdapter sqlAdp;
        string queryString;
        
        SqlCommand sqlCmd;
        object field;
        bool _isLoading = false;

        public frmEmployeesImportingMain()
        {
            InitializeComponent();
            this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
            InCubeDatabase db = new InCubeDatabase();
            db.Open("InCube", "frmEmployeesImportingMain");
            sqlConn = db.GetConnection();
        }

        private string GetScalarValue(string TableName, string columnName, string WhereStr)
        {
            string result = "";
            try
            {
                queryString = "SELECT " + columnName + " FROM " + TableName;
                if (WhereStr != string.Empty)
                    queryString += " WHERE " + WhereStr;
                sqlCmd = new SqlCommand(queryString, sqlConn);
                if (sqlConn.State != ConnectionState.Open)
                    sqlConn.Open();
                field = sqlCmd.ExecuteScalar();
                if (field != null)
                    result = field.ToString();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                result = "Error";
            }
            return result;
        }

        private void btnUpdateEmployee_Click(object sender, EventArgs e)
        {
            try
            {
                string orgID = cmbOrganization.SelectedValue.ToString();
                string warehouseID = cmbWarehouse.SelectedValue.ToString();
                string securityGroupID = cmbSecurityGroup.SelectedValue.ToString();
                string employeeCode = txtEmployeeCode.Text.Replace("'", "''");
                string empName = txtEmployeeName.Text.Replace("'", "''");
                string serial = txtDeviceSerial.Text;
                string userName = txtUserName.Text;
                string password = txtPassword.Text;
                string email="";
                string nationalIDNumber ="";
                email = txtEmailNatID.Text;
                
                string vehicleCode = txtVehicleCode.Text.Replace("'", "''");
                string deviceName = txtDeviceName.Text.Replace("'", "''");
                string orderSeq = txtOrderSeq.Text;
                string paySeq = txtPaymentSeq.Text;
                string appPaySeq = txtAppPaymentSeq.Text;
                string rtnOrderSeq = txtReturnOrderSeq.Text;
                string invSeq = txtInvoiceSeq.Text;
                string newCustSeq = txtNewCustSeq.Text;
                string territoryCode = txtTerritoryCode.Text.Replace("'", "''");
                string routeCode = txtRouteCode.Text.Replace("'", "''");

                InCubeSecurityClass cls = new InCubeSecurityClass();
                password = cls.EncryptData(password);

                //Employee
                string employeeID = GetScalarValue("Employee", "EmployeeID", "EmployeeCode = '" + employeeCode + "'");
                if (employeeID == string.Empty)
                {
                    employeeID = GetScalarValue("Employee", "ISNULL(MAX(EmployeeID),0)+1", "");
                    queryString = string.Format("INSERT INTO Employee (EmployeeID,EmployeeCode,Phone,Mobile,OrganizationID,Inactive,EmployeeTypeID,OnHold,CreatedBy,CreatedDate,HourlyRegularRate,HourlyOvertimeRate,MinHours,MaxOTHours,IsSuperUser,Discount,Email,NationalIDNumber) VALUES ({0},'{1}','','','{2}','False','2','False','{5}',GETDATE(),NULL,NULL,NULL,NULL,NULL,NULL,'{3}','{4}');", employeeID, employeeCode, orgID, email, nationalIDNumber, CoreGeneral.Common.CurrentSession.EmployeeID);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in creating employee");
                        return;
                    }
                }
                else
                {
                    queryString = string.Format("UPDATE Employee SET Inactive = 0, EmployeeTypeID = 2, OnHold = 0, Email = '{0}', NationalIDNumber = '{2}', UpdatedBy = {3}, UpdatedDate = GETDATE() WHERE EmployeeID = {1}", email, employeeID, nationalIDNumber, CoreGeneral.Common.CurrentSession.EmployeeID);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in creating employee");
                        return;
                    }
                }

                //EmployeeLanguage
                string empLangID = GetScalarValue("EmployeeLanguage", "EmployeeID", "EmployeeID = " + employeeID + " AND LanguageID = 1");
                if (empLangID == string.Empty)
                {
                    queryString = string.Format("INSERT INTO EmployeeLanguage (EmployeeID,LanguageID,Description,Address) VALUES ({0},'1','{1}','');", employeeID, empName);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in creating employee's description");
                        return;
                    }
                }
                else
                {
                    queryString = string.Format("UPDATE EmployeeLanguage SET Description = '{0}' WHERE EmployeeID = {1} AND LanguageID = 1", empName, employeeID);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in updating employee's description");
                        return;
                    }
                }

                //EmployeeOrganization
                string empOrgID = GetScalarValue("EmployeeOrganization", "EmployeeID", "EmployeeID = " + employeeID + " AND OrganizationID = " + orgID);
                if (empOrgID == string.Empty)
                {
                    queryString = string.Format("INSERT INTO EmployeeOrganization (EmployeeID,OrganizationID) VALUES ('{0}','{1}');", employeeID, orgID);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in linking employee to organization");
                        return;
                    }
                }

                //Operator
                string opID = GetScalarValue("Operator", "OperatorID", "OperatorName = '" + userName + "'");
                if (opID == string.Empty)
                {
                    opID = GetScalarValue("Operator", "ISNULL(MAX(OperatorID),0)+1","");
                    queryString = string.Format("INSERT INTO Operator (OperatorID,OperatorName,OperatorPassword,FrontOffice,LoginTypeID,PasswordChangeDate,IsLocked) VALUES ({0},'{1}','{2}','True','1',GETDATE(),NULL);", opID, userName, password);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in creating employee's operator");
                        return;
                    }
                }
                else
                {
                    queryString = string.Format("UPDATE Operator SET OperatorPassword= '{0}' WHERE OperatorID = {1}", password, opID);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in creating employee's operator");
                        return;
                    }
                }

                //EmployeeOperator
                string empOpID = GetScalarValue("EmployeeOperator", "EmployeeID", string.Format("OperatorID = {0} AND EmployeeID = {1}", opID, employeeID));
                if (empOpID == string.Empty)
                {
                    queryString = string.Format(@"DELETE FROM EmployeeOperator WHERE EmployeeID = {0} OR OperatorID = {1};
INSERT INTO EmployeeOperator (EmployeeID,OperatorID) VALUES ({0},{1});", employeeID, opID);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in linking employee to operator");
                        return;
                    }
                }
                //else if (empOpID != employeeID)
                //{
                //    //operator reserved
                //    MessageBox.Show("User name already used for another employee, the employee will not have a FO user, ");
                //}

                //OperatorSecurityGroup
                string opGrpID = GetScalarValue("OperatorSecurityGroup", "SecurityGroupID", "OperatorID = " + opID + " AND SecurityGroupID = " + securityGroupID);
                if (opGrpID == string.Empty)
                {
                    queryString = string.Format("INSERT INTO OperatorSecurityGroup (OperatorID,SecurityGroupID) VALUES ({0},{1})", opID, securityGroupID);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in linking employee to security group");
                        return;
                    }
                }

                //Warehouse
                string vehWHID = GetScalarValue("Warehouse", "WarehouseID", "WarehouseCode = '" + vehicleCode + " Vehicle'");
                if (vehWHID == string.Empty)
                {
                    vehWHID = GetScalarValue("Warehouse", "ISNULL(MAX(WarehouseID),0)+1", "");

                    queryString = string.Format("INSERT INTO Warehouse (WarehouseID,Phone,Fax,Barcode,OrganizationID,CreatedBy,CreatedDate,WarehouseTypeID,WarehouseCode,Inactive) VALUES ({0},NULL,NULL,'',{1},'{3}',GETDATE(),2,'{2} Vehicle','False')", vehWHID, orgID, vehicleCode, CoreGeneral.Common.CurrentSession.EmployeeID);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in creating employee's vehicle");
                        return;
                    }
                }
                //WarehouseLanguage
                if (GetScalarValue("WarehouseLanguage", "WarehouseID", "WarehouseID = " + vehWHID) == string.Empty)
                {
                    queryString = string.Format("INSERT INTO WarehouseLanguage (WarehouseID,LanguageID,Description,Address) VALUES ('{0}','1','{1} Vehicle',NULL)", vehWHID, employeeCode);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in creating employee's vehicle");
                        return;
                    }
                }
                //WarehouseZone
                if (GetScalarValue("WarehouseZone", "WarehouseID", "WarehouseID = " + vehWHID) == string.Empty)
                {
                    queryString = string.Format("INSERT INTO WarehouseZone (WarehouseID,ZoneID) VALUES ('{0}','1')", vehWHID);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in creating employee's vehicle");
                        return;
                    }
                }
                //Vehicle
                if (GetScalarValue("Vehicle", "VehicleID", "VehicleID = " + vehWHID) == string.Empty)
                {
                    queryString = string.Format("INSERT INTO Vehicle (VehicleID,PlateNO,TypeID,Odometer,CapacityQuantity,CapacityAmount,CapacityVolume,CreatedBy,CreatedDate,WarrantyExpiry,LicenseExpiryDate) VALUES ('{0}','{1} Vehicle','1','0','0.000000000','0.000000000','0.000000000',{2},GETDATE(),GETDATE(),GETDATE())", vehWHID, vehicleCode, CoreGeneral.Common.CurrentSession.EmployeeID);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in creating employee's vehicle");
                        return;
                    }
                }
                //EmployeeVehicle
                if (ExecuteCommand(string.Format("DELETE FROM EmployeeVehicle WHERE EmployeeID = {0} OR VehicleID = {1}", employeeID, vehWHID)) != "Error")
                {
                    queryString = string.Format("INSERT INTO EmployeeVehicle (VehicleID,EmployeeID) VALUES ('{0}','{1}')", vehWHID, employeeID);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in creating employee's vehicle");
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Error in creating employee's vehicle");
                    return;
                }
                //VehicleLoadingWH
                if (GetScalarValue("VehicleLoadingWh", "VehicleID", "VehicleID = " + vehWHID + " AND WarehouseID = " + warehouseID) == string.Empty)
                {
                    queryString = string.Format("INSERT INTO VehicleLoadingWh (VehicleID,WarehouseID) VALUES ({0},{1})", vehWHID, warehouseID);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in creating employee's vehicle");
                        return;
                    }
                }

                //Device
                string serialExist = GetScalarValue("Device", "Serial", "Serial = '" + serial + "'");
                if (serialExist == string.Empty)
                {
                    queryString = string.Format("INSERT INTO Device (Serial,CreatedBy,CreatedDate,WarrantyExpiry,PurchaseDate,ModelNumber,Brand,AvailableForUsage,OrganizationID) VALUES ('{0}','{2}',GETDATE(),GETDATE(),GETDATE(),'0','0','True',{1})", serial, orgID, CoreGeneral.Common.CurrentSession.EmployeeID);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in creating employee's device");
                        return;
                    }
                }

                //DeviceLanguage
                string deviceDesc = GetScalarValue("DeviceLanguage", "Description", "Serial = '" + serial + "'");
                if (deviceDesc == string.Empty)
                {
                    queryString = string.Format("INSERT INTO DeviceLanguage (Serial,LanguageID,Description,Remarks) VALUES ('{0}','1','{1}','0')", serial, deviceName);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in creating employee's device");
                        return;
                    }
                }
                else
                {
                    queryString = string.Format("UPDATE DeviceLanguage SET Description = '{1}' WHERE  Serial = '{0}'", serial, deviceName);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in creating employee's device");
                        return;
                    }
                }

                //EmployeeDevice
                string deviceEmp = GetScalarValue("EmployeeDevice", "EmployeeID", string.Format("EmployeeID = {0} AND DeviceSerial = '{1}'", employeeID, serial));
                if (deviceEmp == string.Empty)
                {
                    queryString = string.Format(@"DELETE FROM EmployeeDevice WHERE EmployeeID = {0} OR DeviceSerial = '{1}'
INSERT INTO EmployeeDevice (EmployeeID,DeviceSerial) VALUES ('{0}','{1}')", employeeID, serial);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in creating employee's device");
                        return;
                    }
                }
                else if (deviceEmp != employeeID)
                {
                    queryString = string.Format("UPDATE EmployeeDevice SET EmployeeID = {0} WHERE DeviceSerial = '{1}'", employeeID, serial);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in creating employee's device");
                        return;
                    }
                }

                //Document Sequence
                string docSequenceExist = GetScalarValue("DocumentSequence", "EmployeeID", "EmployeeID = " + employeeID);
                if (docSequenceExist == string.Empty)
                {
                    queryString = string.Format("INSERT INTO DocumentSequence (EmployeeID, MaxTransactionOrderID, MaxTransactionPaymentID,MaxAppliedPaymentID,MaxReturnOrderID,MaxTransactionInvoiceID,MaxNewCustomerCodeID) VALUES ({0}, '{1}', '{2}','{3}','{4}','{5}','{6}')", employeeID, orderSeq, paySeq, appPaySeq, rtnOrderSeq, invSeq, newCustSeq);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in creating employee's document sequence");
                        return;
                    }
                }
                else
                {
                    docSequenceExist = GetScalarValue("DocumentSequence", "MaxTransactionOrderID", "EmployeeID = " + employeeID);
                    if (docSequenceExist == string.Empty)
                    {
                        queryString = string.Format("UPDATE DocumentSequence SET MaxTransactionOrderID = '{1}' WHERE EmployeeID = {0}", employeeID, orderSeq);
                        if (ExecuteCommand(queryString) == "Error")
                        {
                            MessageBox.Show("Error in updating employee's document sequence");
                            return;
                        }
                    }
                    docSequenceExist = GetScalarValue("DocumentSequence", "MaxTransactionPaymentID", "EmployeeID = " + employeeID);
                    if (docSequenceExist == string.Empty)
                    {
                        queryString = string.Format("UPDATE DocumentSequence SET MaxTransactionPaymentID = '{1}' WHERE EmployeeID = {0}", employeeID, paySeq);
                        if (ExecuteCommand(queryString) == "Error")
                        {
                            MessageBox.Show("Error in updating employee's document sequence");
                            return;
                        }
                    }
                    docSequenceExist = GetScalarValue("DocumentSequence", "MaxAppliedPaymentID", "EmployeeID = " + employeeID);
                    if (docSequenceExist == string.Empty)
                    {
                        queryString = string.Format("UPDATE DocumentSequence SET MaxAppliedPaymentID = '{1}' WHERE EmployeeID = {0}", employeeID, appPaySeq);
                        if (ExecuteCommand(queryString) == "Error")
                        {
                            MessageBox.Show("Error in updating employee's document sequence");
                            return;
                        }
                    }
                    docSequenceExist = GetScalarValue("DocumentSequence", "MaxReturnOrderID", "EmployeeID = " + employeeID);
                    if (docSequenceExist == string.Empty)
                    {
                        queryString = string.Format("UPDATE DocumentSequence SET MaxReturnOrderID = '{1}' WHERE EmployeeID = {0}", employeeID, rtnOrderSeq);
                        if (ExecuteCommand(queryString) == "Error")
                        {
                            MessageBox.Show("Error in updating employee's document sequence");
                            return;
                        }
                    }
                    docSequenceExist = GetScalarValue("DocumentSequence", "MaxTransactionInvoiceID", "EmployeeID = " + employeeID);
                    if (docSequenceExist == string.Empty)
                    {
                        queryString = string.Format("UPDATE DocumentSequence SET MaxTransactionInvoiceID = '{1}' WHERE EmployeeID = {0}", employeeID, invSeq);
                        if (ExecuteCommand(queryString) == "Error")
                        {
                            MessageBox.Show("Error in updating employee's document sequence");
                            return;
                        }
                    }
                    docSequenceExist = GetScalarValue("DocumentSequence", "MaxNewCustomerCodeID", "EmployeeID = " + employeeID);
                    if (docSequenceExist == string.Empty)
                    {
                        queryString = string.Format("UPDATE DocumentSequence SET MaxNewCustomerCodeID = '{1}' WHERE EmployeeID = {0}", employeeID, newCustSeq);
                        if (ExecuteCommand(queryString) == "Error")
                        {
                            MessageBox.Show("Error in updating employee's document sequence");
                            return;
                        }
                    }
                }

                //Territory
                string territoryID = GetScalarValue("Territory", "TerritoryID", "TerritoryCode = '" + territoryCode + "'");
                if (territoryID == string.Empty)
                {
                    territoryID = GetScalarValue("Territory", "ISNULL(MAX(TerritoryID),0)+1", "");
                    queryString = string.Format("INSERT INTO Territory (TerritoryID,OrganizationID,CreatedBy,CreatedDate,TerritoryCode) VALUES ({0},{1},{3},GETDATE(),'{2}')", territoryID, orgID, territoryCode, CoreGeneral.Common.CurrentSession.EmployeeID);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in updating employee's territory");
                        return;
                    }
                }

                //TerritoryLanguage
                string territoryLangID = GetScalarValue("TerritoryLanguage", "TerritoryID", "TerritoryID = " + territoryID);
                if (territoryLangID == string.Empty)
                {
                    queryString = string.Format("INSERT INTO TerritoryLanguage (TerritoryID,LanguageID,Description) VALUES ({0},1,'{1}');", territoryID, territoryCode);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in updating employee's territory");
                        return;
                    }
                }

                //EmployeeTerritory
                string territoryEmpID = GetScalarValue("EmployeeTerritory", "EmployeeID", "TerritoryID = " + territoryID);
                if (territoryEmpID == string.Empty)
                {
                    queryString = string.Format("INSERT INTO EmployeeTerritory (EmployeeID,TerritoryID) VALUES ({0},{1});", employeeID, territoryID);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in updating employee's territory");
                        return;
                    }
                }
                else if (territoryEmpID != employeeID)
                {
                    queryString = string.Format("UPDATE EmployeeTerritory SET EmployeeID = {0} WHERE TerritoryID= {1}", employeeID, territoryID);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in updating employee's territory");
                        return;
                    }
                }

                //Route
                string[] days = new string[] { "Sat", "Sun", "Mon", "Tue", "Wed", "Thu", "Fri" };
                foreach (string day in days)
                {
                    bool Sun = day == "Sun", Mon = day == "Mon", Tue = day == "Tue", Wed = day == "Wed", Thu = day == "Thu", Fri = day == "Fri", Sat = day == "Sat";
                    routeCode = territoryCode + "-" + day;
                    string routeID = GetScalarValue("Route", "RouteID", "RouteCode = '" + routeCode + "'");
                    if (routeID == string.Empty)
                    {
                        routeID = GetScalarValue("Route", "ISNULL(MAX(RouteID),0)+1", "");
                        queryString = string.Format("INSERT INTO Route (RouteID,Inactive,TerritoryID,EstimatedStart,EstimatedEnd,CreatedBy,CreatedDate,CustomerID,OutletID,RouteCode,SalesSMSTemplateID,NewCustomerTemplateID,NewCustomerID,NewOutletID) VALUES ({0},'False',{1},NULL,NULL,'{3}',GETDATE(),'-1','-1','{2}',NULL,NULL,NULL,NULL)", routeID, territoryID, routeCode, CoreGeneral.Common.CurrentSession.EmployeeID);
                        if (ExecuteCommand(queryString) == "Error")
                        {
                            MessageBox.Show("Error in updating employee's route");
                            return;
                        }
                    }

                    //RouteLanguage
                    string routeLangID = GetScalarValue("RouteLanguage", "RouteID", "RouteID = " + routeID);
                    if (routeLangID == string.Empty)
                    {
                        queryString = string.Format("INSERT INTO RouteLanguage (RouteID,LanguageID,Description) VALUES ({0},'1','{1}')", routeID, routeCode);
                        if (ExecuteCommand(queryString) == "Error")
                        {
                            MessageBox.Show("Error in updating employee's route");
                            return;
                        }
                    }

                    //RouteVisitPattern
                    string rvpExists = GetScalarValue("RouteVisitPattern", "RouteID", "RouteID = " + routeID);
                    if (rvpExists == string.Empty)
                    {
                        queryString = string.Format(@"INSERT INTO RouteVisitPattern (RouteID,Week,Sunday,Monday,Tuesday,Wednesday,Thursday,Friday,Saturday) VALUES ({0},1,'{1}','{2}','{3}','{4}','{5}','{6}','{7}');", routeID, Sun, Mon, Tue, Wed, Thu, Fri, Sat);
                    }
                    else
                    {
                        queryString = string.Format(@"UPDATE RouteVisitPattern SET Sunday = '{1}',Monday = '{2}',Tuesday = '{3}',Wednesday = '{4}',Thursday = '{5}',Friday = '{6}',Saturday = '{7}' WHERE RouteID = {0};", routeID, Sun, Mon, Tue, Wed, Thu, Fri, Sat);
                    }
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in updating employee's route");
                        return;
                    }
                }
                
                //Account
                //AccountEmp
                string accountID = GetScalarValue("AccountEmp", "AccountID", "EmployeeID = " + employeeID);
                if (accountID == string.Empty)
                {
                    accountID = GetScalarValue("Account", "ISNULL(Max(AccountID),0)+1", "");
                    queryString = string.Format(@"INSERT INTO Account (AccountID,AccountTypeID,CreditLimit,Balance,OrganizationID,CurrencyID) VALUES ({0},2,999999999,0,{1},1);
                                                          INSERT INTO AccountEmp (AccountID,EmployeeID) VALUES ({0},{2})", accountID, orgID, employeeID);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in updating employee's account");
                        return;
                    }
                }

                //AccountLanguage
                string accountLangID = GetScalarValue("AccountLanguage", "AccountID", "AccountID = " + accountID);
                if (accountLangID == string.Empty)
                {
                    queryString = string.Format("INSERT INTO AccountLanguage (AccountID,LanguageID,Description) VALUES ({0},1,'{1} Account')", accountID, employeeCode);
                    if (ExecuteCommand(queryString) == "Error")
                    {
                        MessageBox.Show("Error in updating employee's account");
                        return;
                    }
                }

                MessageBox.Show("Employee added / updated successfully ..");
                tblPrimary.Enabled = true;
                tblExtended.Enabled = false;
                foreach (Control ctrl in tblPrimary.Controls)
                {
                    try
                    {
                        if (ctrl.Name.Substring(0,3) == "txt")
                            ((TextBox)ctrl).Clear();
                    }
                    catch { }
                }
                foreach (Control ctrl in tblExtended.Controls)
                {
                    try
                    {
                        if (ctrl.Name.Substring(0, 3) == "txt")
                            ((TextBox)ctrl).Clear();
                    }
                    catch { }
                }
                cmbOrganization.Focus();

            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        

        private string ExecuteCommand(string CommandString)
        {
            int results = 0;
            try
            {
                sqlCmd = new SqlCommand(CommandString, sqlConn);
                if (sqlConn.State != ConnectionState.Open)
                    sqlConn.Open();
                results = sqlCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                return "Error";
            }
            return results.ToString();
        }

        private void frmEmployeesImportingMain_Load(object sender, EventArgs e)
        {
            try
            {
                _isLoading = true;
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;

                if (sqlConn.State != ConnectionState.Open)
                    sqlConn.Open();
                queryString = "SELECT WarehouseCode,WarehouseID FROM Warehouse WHERE WarehouseTypeID = 1 AND OrganizationID IN (" + CoreGeneral.Common.userPrivileges.Organizations + ")";
                sqlAdp = new SqlDataAdapter(queryString, sqlConn);
                DataTable dtWH = new DataTable();
                sqlAdp.Fill(dtWH);
                if (dtWH != null && dtWH.Rows.Count > 0)
                {
                    cmbWarehouse.DataSource = dtWH;
                    cmbWarehouse.DisplayMember = "WarehouseCode";
                    cmbWarehouse.ValueMember = "WarehouseID";
                }
                queryString = "SELECT O.OrganizationID,O.OrganizationCode + ' - ' + OL.Description Org FROM Organization O INNER JOIN OrganizationLanguage OL ON OL.OrganizationID = O.OrganizationID AND OL.LanguageID = 1 AND O.OrganizationID IN (" + CoreGeneral.Common.userPrivileges.Organizations + ")";
                sqlAdp = new SqlDataAdapter(queryString, sqlConn);
                DataTable dtOrg = new DataTable();
                sqlAdp.Fill(dtOrg);
                if (dtOrg != null && dtOrg.Rows.Count > 0)
                {
                    cmbOrganization.DataSource = dtOrg;
                    cmbOrganization.ValueMember = "OrganizationID";
                    cmbOrganization.DisplayMember = "Org";
                }

                queryString = @"SELECT SG.SecurityGroupID,SGL.Description 
FROM SecurityGroup SG 
INNER JOIN SecurityGroupLanguage SGL ON SGL.SecurityGroupID = SG.SecurityGroupID AND SGL.LanguageID = 1 AND SG.OrganizationID IN (" + CoreGeneral.Common.userPrivileges.Organizations + ")";
                sqlAdp = new SqlDataAdapter(queryString, sqlConn);
                DataTable dtGroups = new DataTable();
                sqlAdp.Fill(dtGroups);
                if (dtGroups != null && dtGroups.Rows.Count > 0)
                {
                    cmbSecurityGroup.DataSource = dtGroups;
                    cmbSecurityGroup.ValueMember = "SecurityGroupID";
                    cmbSecurityGroup.DisplayMember = "Description";
                }

                txtEmployeeCode.Focus();
                txtEmployeeCode.SelectAll();

                _isLoading = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot open DB Connection, modify connection string and try again");
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                this.Close();
            }
        }

        private void btnFillFields_Click(object sender, EventArgs e)
        {
            try
            {
                if (txtVehicleCode.Text.Trim().Equals(string.Empty))
                    txtVehicleCode.Text = txtEmployeeCode.Text;
                if (txtDeviceName.Text.Trim().Equals(string.Empty))
                    txtDeviceName.Text = txtEmployeeCode.Text + " device";
                if (txtOrderSeq.Text.Trim().Equals(string.Empty))
                    txtOrderSeq.Text = "O-" + txtEmailNatID.Text + "-000000";
                if (txtPaymentSeq.Text.Trim().Equals(string.Empty))
                    txtPaymentSeq.Text = "P-" + txtEmailNatID.Text + "-000000";
                if (txtAppPaymentSeq.Text.Trim().Equals(string.Empty))
                    txtAppPaymentSeq.Text = "A-" + txtEmailNatID.Text + "-000000";
                if (txtReturnOrderSeq.Text.Trim().Equals(string.Empty))
                    txtReturnOrderSeq.Text = "R-" + txtEmailNatID.Text + "-000000";
                if (txtInvoiceSeq.Text.Trim().Equals(string.Empty))
                    txtInvoiceSeq.Text = "INV-" + txtEmailNatID.Text + "-000000";
                if (txtNewCustSeq.Text.Trim().Equals(string.Empty))
                    txtNewCustSeq.Text = "DOC-" + txtEmailNatID.Text + "-000000";
                //if (txtTerritoryCode.Text.Trim().Equals(string.Empty))
                txtTerritoryCode.Text = txtEmployeeCode.Text;
                if (txtRouteCode.Text.Trim().Equals(string.Empty))
                    txtRouteCode.Text = txtEmployeeCode.Text;
                
                tblExtended.Enabled = true;
                tblPrimary.Enabled = false;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void cmbOrganization_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnFillFields.Enabled = CheckPrimaryFields();
        }

        private bool CheckExtendedFields()
        {
            try
            {
                foreach (Control ctrl in tblExtended.Controls)
                {
                    try
                    {
                        if (ctrl.Name.Substring(0, 3) == "txt" && ((TextBox)ctrl).Text == string.Empty)
                            return false;
                    }
                    catch { }
                }

                //if (txtVehicleCode.Text.Equals(string.Empty))
                //    return false;
                //if (txtDeviceName.Text.Equals(string.Empty))
                //    return false;
                //if (txtTerritoryCode.Text.Equals(string.Empty))
                //    return false;
                //if (txtRouteCode.Text.Equals(string.Empty))
                //    return false;
                //if (txtOrderSeq.Text.Equals(string.Empty))
                //    return false;
                //if (txtPaymentSeq.Text.Equals(string.Empty))
                //    return false;
                //if (txtAppPaymentSeq.Text.Equals(string.Empty))
                //    return false;
                //if (txtReturnOrderSeq.Text.Equals(string.Empty))
                //    return false;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                return false;
            }
            return true;
        }
        private bool CheckPrimaryFields()
        {
            try
            {
                foreach (Control ctrl in tblPrimary.Controls)
                {
                    try
                    {
                        if (ctrl.Name.Substring(0, 3) == "txt" && ((TextBox)ctrl).Text == string.Empty)
                            return false;
                    }
                    catch { }
                }
                if (cmbOrganization.SelectedValue == null)
                    return false;
                if (cmbWarehouse.SelectedValue == null)
                    return false;
                if (cmbSecurityGroup.SelectedValue == null)
                    return false;
                //if (txtDeviceSerial.Text.Equals(string.Empty))
                //    return false;
                //if (txtEmployeeCode.Text.Equals(string.Empty))
                //    return false;
                //if (txtEmployeeName.Text.Equals(string.Empty))
                //    return false;
                //if (txtUserName.Text.Equals(string.Empty))
                //    return false;
                //if (txtPassword.Text.Equals(string.Empty))
                //    return false;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                return false;
            }
            return true;
        }

        private void cmbWarehouse_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnFillFields.Enabled = CheckPrimaryFields();
        }

        private void cmbSecurityGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnFillFields.Enabled = CheckPrimaryFields();
        }

        private void txtDeviceSerial_TextChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnFillFields.Enabled = CheckPrimaryFields();
        }

        private void txtEmployeeCode_TextChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnFillFields.Enabled = CheckPrimaryFields();
        }

        private void txtEmployeeName_TextChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnFillFields.Enabled = CheckPrimaryFields();
        }

        private void txtUserName_TextChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnFillFields.Enabled = CheckPrimaryFields();
        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnFillFields.Enabled = CheckPrimaryFields();
        }

        private void txtVehicleCode_TextChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnUpdateEmployee.Enabled = CheckExtendedFields();
        }

        private void txtDeviceName_TextChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnUpdateEmployee.Enabled = CheckExtendedFields();
        }

        private void txtTerritoryCode_TextChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnUpdateEmployee.Enabled = CheckExtendedFields();
        }

        private void txtRouteCode_TextChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnUpdateEmployee.Enabled = CheckExtendedFields();
        }

        private void txtOrderSeq_TextChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnUpdateEmployee.Enabled = CheckExtendedFields();
        }

        private void txtPaymentSeq_TextChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnUpdateEmployee.Enabled = CheckExtendedFields();
        }

        private void txtReturnOrderSeq_TextChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnUpdateEmployee.Enabled = CheckExtendedFields();
        }

        private void txtAppPaymentSeq_TextChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnUpdateEmployee.Enabled = CheckExtendedFields();
        }

        private void txtNewCustSeq_TextChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnUpdateEmployee.Enabled = CheckExtendedFields();
        }

        private void txtInvoiceSeq_TextChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnUpdateEmployee.Enabled = CheckExtendedFields();
        }

        private void txtEmail_TextChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                btnFillFields.Enabled = CheckPrimaryFields();
        }

        private void txtEmployeeCode_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyData == Keys.Enter && !txtEmployeeCode.Text.Equals(string.Empty))
                {
                    sqlAdp = new SqlDataAdapter(string.Format(@"SELECT TOP 1 E.EmployeeCode,E.Email MailBox,ED.DeviceSerial,O.OperatorName UserName,O.OperatorPassword Password,VW.WarehouseID MainWHID
,OSG.SecurityGroupID,EL.Description Name,V.WarehouseCode VehicleCode,DL.Description DeviceName,T.TerritoryCode,R.RouteCode
,DS.MaxTransactionOrderID,DS.MaxTransactionPaymentID,DS.MaxReturnOrderID,DS.MaxTransactionInvoiceID,DS.MaxAppliedPaymentID,DS.MaxNewCustomerCodeID
FROM Employee E
LEFT JOIN EmployeeDevice ED ON ED.EmployeeID = E.EmployeeID
LEFT JOIN EmployeeOperator EO ON EO.EmployeeID = E.EmployeeID
LEFT JOIN Operator O ON O.OperatorID = EO.OperatorID
LEFT JOIN EmployeeVehicle EV ON EV.EmployeeID = E.EmployeeID
LEFT JOIN VehicleLoadingWh VW ON VW.VehicleID = EV.VehicleID
LEFT JOIN OperatorSecurityGroup OSG ON OSG.OperatorID = O.OperatorID
LEFT JOIN EmployeeLanguage EL ON EL.EmployeeID = E.EmployeeID AND EL.LanguageID = 1
LEFT JOIN Warehouse V ON V.WarehouseID = EV.VehicleID
LEFT JOIN DeviceLanguage DL ON DL.Serial = ED.DeviceSerial AND DL.LanguageID = 1
LEFT JOIN EmployeeTerritory ET ON ET.EmployeeID = E.EmployeeID
LEFT JOIN Territory T ON T.TerritoryID = ET.TerritoryID
LEFT JOIN Route R ON R.TerritoryID = T.TerritoryID
LEFT JOIN DocumentSequence DS ON DS.EmployeeID = E.EmployeeID
WHERE E.EmployeeCode = '{0}' AND E.OrganizationID IN ({1})", txtEmployeeCode.Text, CoreGeneral.Common.userPrivileges.Organizations), sqlConn);
                    DataTable dtEmployeeInfo = new DataTable();
                    sqlAdp.Fill(dtEmployeeInfo);
                    if (dtEmployeeInfo != null && dtEmployeeInfo.Rows.Count > 0)
                    {
                        if (dtEmployeeInfo.Rows[0]["MainWHID"] != DBNull.Value && dtEmployeeInfo.Rows[0]["MainWHID"].ToString() != "")
                        {
                            cmbWarehouse.SelectedValue = dtEmployeeInfo.Rows[0]["MainWHID"].ToString();
                        }
                        if (dtEmployeeInfo.Rows[0]["SecurityGroupID"] != DBNull.Value && dtEmployeeInfo.Rows[0]["SecurityGroupID"].ToString() != "")
                        {
                            cmbSecurityGroup.SelectedValue = dtEmployeeInfo.Rows[0]["SecurityGroupID"].ToString();
                        }
                        if (dtEmployeeInfo.Rows[0]["DeviceSerial"] != DBNull.Value && dtEmployeeInfo.Rows[0]["DeviceSerial"].ToString() != "")
                        {
                            txtDeviceSerial.Text = dtEmployeeInfo.Rows[0]["DeviceSerial"].ToString();
                        }
                        if (dtEmployeeInfo.Rows[0]["DeviceName"] != DBNull.Value && dtEmployeeInfo.Rows[0]["DeviceName"].ToString() != "")
                        {
                            txtDeviceName.Text = dtEmployeeInfo.Rows[0]["DeviceName"].ToString();
                        }
                        if (dtEmployeeInfo.Rows[0]["Name"] != DBNull.Value && dtEmployeeInfo.Rows[0]["Name"].ToString() != "")
                        {
                            txtEmployeeName.Text = dtEmployeeInfo.Rows[0]["Name"].ToString();
                        }
                        if (dtEmployeeInfo.Rows[0]["UserName"] != DBNull.Value && dtEmployeeInfo.Rows[0]["UserName"].ToString() != "")
                        {
                            txtUserName.Text = dtEmployeeInfo.Rows[0]["UserName"].ToString();
                        }
                        if (dtEmployeeInfo.Rows[0]["Password"] != DBNull.Value && dtEmployeeInfo.Rows[0]["Password"].ToString() != "")
                        {
                            InCubeSecurityClass cls = new InCubeSecurityClass();
                            txtPassword.Text = cls.DecryptData(dtEmployeeInfo.Rows[0]["Password"].ToString());
                        }
                        if (dtEmployeeInfo.Rows[0]["MailBox"] != DBNull.Value && dtEmployeeInfo.Rows[0]["MailBox"].ToString() != "")
                        {
                            txtEmailNatID.Text = dtEmployeeInfo.Rows[0]["MailBox"].ToString();
                        }
                        if (dtEmployeeInfo.Rows[0]["VehicleCode"] != DBNull.Value && dtEmployeeInfo.Rows[0]["VehicleCode"].ToString() != "")
                        {
                            txtVehicleCode.Text = dtEmployeeInfo.Rows[0]["VehicleCode"].ToString();
                        }
                        if (dtEmployeeInfo.Rows[0]["DeviceName"] != DBNull.Value && dtEmployeeInfo.Rows[0]["DeviceName"].ToString() != "")
                        {
                            txtDeviceName.Text = dtEmployeeInfo.Rows[0]["DeviceName"].ToString();
                        }
                        if (dtEmployeeInfo.Rows[0]["TerritoryCode"] != DBNull.Value && dtEmployeeInfo.Rows[0]["TerritoryCode"].ToString() != "")
                        {
                            txtTerritoryCode.Text = dtEmployeeInfo.Rows[0]["TerritoryCode"].ToString();
                        }
                        if (dtEmployeeInfo.Rows[0]["RouteCode"] != DBNull.Value && dtEmployeeInfo.Rows[0]["RouteCode"].ToString() != "")
                        {
                            txtRouteCode.Text = dtEmployeeInfo.Rows[0]["RouteCode"].ToString();
                        }
                        if (dtEmployeeInfo.Rows[0]["MaxAppliedPaymentID"] != DBNull.Value && dtEmployeeInfo.Rows[0]["MaxAppliedPaymentID"].ToString() != "")
                        {
                            txtAppPaymentSeq.Text = dtEmployeeInfo.Rows[0]["MaxAppliedPaymentID"].ToString();
                        }
                        if (dtEmployeeInfo.Rows[0]["MaxTransactionInvoiceID"] != DBNull.Value && dtEmployeeInfo.Rows[0]["MaxTransactionInvoiceID"].ToString() != "")
                        {
                            txtInvoiceSeq.Text = dtEmployeeInfo.Rows[0]["MaxTransactionInvoiceID"].ToString();
                        }
                        if (dtEmployeeInfo.Rows[0]["MaxNewCustomerCodeID"] != DBNull.Value && dtEmployeeInfo.Rows[0]["MaxNewCustomerCodeID"].ToString() != "")
                        {
                            txtNewCustSeq.Text = dtEmployeeInfo.Rows[0]["MaxNewCustomerCodeID"].ToString();
                        }
                        if (dtEmployeeInfo.Rows[0]["MaxTransactionPaymentID"] != DBNull.Value && dtEmployeeInfo.Rows[0]["MaxTransactionPaymentID"].ToString() != "")
                        {
                            txtPaymentSeq.Text = dtEmployeeInfo.Rows[0]["MaxTransactionPaymentID"].ToString();
                        }
                        if (dtEmployeeInfo.Rows[0]["MaxTransactionOrderID"] != DBNull.Value && dtEmployeeInfo.Rows[0]["MaxTransactionOrderID"].ToString() != "")
                        {
                            txtOrderSeq.Text = dtEmployeeInfo.Rows[0]["MaxTransactionOrderID"].ToString();
                        }
                        if (dtEmployeeInfo.Rows[0]["MaxReturnOrderID"] != DBNull.Value && dtEmployeeInfo.Rows[0]["MaxReturnOrderID"].ToString() != "")
                        {
                            txtReturnOrderSeq.Text = dtEmployeeInfo.Rows[0]["MaxReturnOrderID"].ToString();
                        }
                    }
                }
                else
                {
                    //a       a    1 0 1
                    //ab      ab   2 0 2
                    //abc     abc  3 0 3
                    //abcd    abcd 4 0 4
                    //abcde   bcde 5 1 4
                    //abcdef  cdef 6 2 4
                    txtUserName.Text = txtEmployeeCode.Text.Substring(Math.Max(0, txtEmployeeCode.Text.Length - 4), Math.Min(4, txtEmployeeCode.Text.Length));
                    txtPassword.Text = txtUserName.Text;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}
