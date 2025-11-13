using InCubeIntegration_DAL;
using InCubeLibrary;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Xml;
using Oracle.ManagedDataAccess.Client;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace InCubeIntegration_BL
{
    class IntegrationAwal : IntegrationBase
    {
        CultureInfo EsES = new CultureInfo("es-ES");
        private OracleConnection Conn;
        private OracleTransaction transaction = null;
        private OracleCommand cmd;
        private OracleDataAdapter adp;
        private string LastExecError = "";
        private string LastQueryString = "";
        private InCubeErrors err;
        private long UserID;
        private string SiteSymbol = "";
        private bool AllowTax = false;
        private bool AllowExcise = false;
        QueryBuilder QueryBuilderObject = new QueryBuilder();
        string DateFormat = "dd/MMM/yyyy";
        new string OrganizationID = "";
        InCubeQuery inCubeQuery;

        public IntegrationAwal(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            
            string _dataSourceFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase) + "\\DataSources.xml";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_dataSourceFilePath);
            string strConnectionString = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'InCubeSQL']/Data").InnerText;
            Conn = new OracleConnection(strConnectionString);
            SiteSymbol = CoreGeneral.Common.GeneralConfigurations.SiteSymbol;
            AllowTax = bool.Parse(GetFieldValue("Configuration", "KeyValue", "KeyName = 'AllowTax'", db_vms));
            AllowExcise = bool.Parse(GetFieldValue("Configuration", "KeyValue", "KeyName = 'AllowRetailTaxOnItems'", db_vms));
            try
            {
                Conn.Open();
                if (Conn.State != ConnectionState.Open)
                {
                    WriteMessage("Unable to connect to Intermediate database");
                    Initialized = false;
                }
                Conn.Close();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                Initialized = false;
            }
            UserID = CurrentUserID;
        }

        private void AddUpdateSalesperson(string SalespersonID, string SalespersonCode, string SalespersonNameArabic, string SalespersonNameEnglish, string Phone, ref int TOTALUPDATED, ref int TOTALINSERTED, string DivisionID, string CreditLimit, string Balance, string EmplyeeTypeID)
        {
            string str2 = "";
            if (GetFieldValue("Employee", "EmployeeID", "EmployeeID = " + SalespersonID, db_vms) == string.Empty)
            {
                TOTALINSERTED++;
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                QueryBuilderObject.SetField("EmployeeCode", "'" + SalespersonCode + "'");
                QueryBuilderObject.SetField("NationalIDNumber", "0");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.SetField("InActive", "0");
                QueryBuilderObject.SetField("OnHold", "0");
                QueryBuilderObject.SetField("EmployeeTypeID", EmplyeeTypeID);
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.InsertQueryString("Employee", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("OnHold", "0");
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.UpdateQueryString("Employee", "EmployeeID = " + SalespersonID, db_vms);
            }
            if (GetFieldValue("EmployeeLanguage", "EmployeeID", "EmployeeID = " + SalespersonID + " AND LanguageID = 1", db_vms) != string.Empty)
            {
                TOTALUPDATED++;
                QueryBuilderObject.SetField("Description", "'" + SalespersonNameEnglish + "'");
                QueryBuilderObject.UpdateQueryString("EmployeeLanguage", "EmployeeID = " + SalespersonID + " AND LanguageID = 1", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + SalespersonNameEnglish + "'");
                QueryBuilderObject.SetField("Address", "''");
                QueryBuilderObject.InsertQueryString("EmployeeLanguage", db_vms);
            }
            if (GetFieldValue("EmployeeLanguage", "EmployeeID", "EmployeeID = " + SalespersonID + " AND LanguageID = 2", db_vms) != string.Empty)
            {
                ;
                //TOTALUPDATED++;
                //QueryBuilderObject.SetField("Description", "N'" + SalespersonNameArabic + "'");
                //QueryBuilderObject.UpdateQueryString("EmployeeLanguage", "EmployeeID = " + SalespersonID + " AND LanguageID = 2", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + SalespersonNameArabic + "'");
                QueryBuilderObject.SetField("Address", "''");
                QueryBuilderObject.InsertQueryString("EmployeeLanguage", db_vms);
            }
            string fieldValue = GetFieldValue("EmployeeOperator", "OperatorID", "EmployeeID = " + SalespersonID, db_vms);
            if (fieldValue == string.Empty)
            {
                fieldValue = GetFieldValue("Operator", "MAX(OperatorID)+1", db_vms);
            }
            err = ExistObject("Operator", "OperatorID", "OperatorID = " + fieldValue, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("OperatorID", fieldValue);
                QueryBuilderObject.SetField("OperatorName", "'" + SalespersonCode + "'");
                QueryBuilderObject.SetField("FrontOffice", "1");
                QueryBuilderObject.SetField("LoginTypeID", "1");
                QueryBuilderObject.InsertQueryString("Operator", db_vms);
            }
            err = ExistObject("EmployeeOperator", "EmployeeID", "EmployeeID = " + SalespersonID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("OperatorID", fieldValue);
                QueryBuilderObject.InsertQueryString("EmployeeOperator", db_vms);
            }
            object field = null;
            string str4 = "";
            InCubeQuery query = new InCubeQuery("Select DivisionID from Division", db_vms);
            query.Execute();
            err = query.FindFirst();
            while (err == InCubeErrors.Success)
            {
                query.GetField(0, ref field);
                str4 = field.ToString();
                if (GetFieldValue("EmployeeDivision", "EmployeeID", "EmployeeID = " + SalespersonID + " AND DivisionID = " + str4, db_vms) == string.Empty)
                {
                    QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                    QueryBuilderObject.SetField("DivisionID", str4);
                    QueryBuilderObject.InsertQueryString("EmployeeDivision", db_vms);
                }
                err = query.FindNext();
            }
            query.Close();
            if (GetFieldValue("EmployeeOrganization", "EmployeeID", "EmployeeID = " + SalespersonID, db_vms) == string.Empty)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.InsertQueryString("EmployeeOrganization", db_vms);
            }
            int num = 1;
            str2 = GetFieldValue("AccountEmp", "AccountID", "EmployeeID = " + SalespersonID, db_vms);
            if (str2 == string.Empty)
            {
                num = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));
                QueryBuilderObject.SetField("AccountID", num.ToString());
                QueryBuilderObject.SetField("AccountTypeID", "2");
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.SetField("Balance", Balance);
                QueryBuilderObject.SetField("GL", "0");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.SetField("CurrencyID", "1");
                QueryBuilderObject.InsertQueryString("Account", db_vms);
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("AccountID", num.ToString());
                QueryBuilderObject.InsertQueryString("AccountEmp", db_vms);
                QueryBuilderObject.SetField("AccountID", num.ToString());
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + SalespersonNameEnglish.Trim() + " Account'");
                QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
                QueryBuilderObject.SetField("AccountID", num.ToString());
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + SalespersonNameArabic.Trim() + " Account'");
                QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + str2, db_vms);
            }
        }

        private void AddUpdateWarehouse(string WarehouseID, string WarehouseCode, string WarehouceName, string Address, string VehicleRegNum, string SalesmanCode, ref int TOTALUPDATED, ref int TOTALINSERTED, string WarehouseType, string HelperCode)
        {
            if (GetFieldValue("Warehouse", "WarehouseID", "WarehouseID = " + WarehouseID, db_vms) != string.Empty)
            {
                TOTALUPDATED++;
                QueryBuilderObject.SetField("Phone", "''");
                QueryBuilderObject.SetField("WarehouseCode", "'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("WarehouseTypeID", WarehouseType);
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.UpdateQueryString("Warehouse", " WarehouseID = " + WarehouseID, db_vms);
            }
            else
            {
                TOTALINSERTED++;
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("Phone", "''");
                QueryBuilderObject.SetField("WarehouseCode", "'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.SetField("WarehouseTypeID", WarehouseType);
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                InCubeErrors errors = QueryBuilderObject.InsertQueryString("Warehouse", db_vms);
            }
            if (GetFieldValue("WarehouseLanguage", "WarehouseID", "WarehouseID = " + WarehouseID + " AND LanguageID = 1", db_vms) != string.Empty)
            {
                QueryBuilderObject.SetField("Description", "'" + WarehouceName + "'");
                QueryBuilderObject.SetField("Address", "'" + Address + "'");
                QueryBuilderObject.UpdateQueryString("WarehouseLanguage", " WarehouseID =" + WarehouseID + " AND LanguageID = 1", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + WarehouceName + "'");
                QueryBuilderObject.SetField("Address", "'" + Address + "'");
                QueryBuilderObject.InsertQueryString("WarehouseLanguage", db_vms);
            }
            if (GetFieldValue("WarehouseLanguage", "WarehouseID", "WarehouseID = " + WarehouseID + " AND LanguageID = 2", db_vms) != string.Empty)
            {
                QueryBuilderObject.SetField("Description", "N'" + WarehouceName + "'");
                QueryBuilderObject.SetField("Address", "N'" + Address + "'");
                QueryBuilderObject.UpdateQueryString("WarehouseLanguage", " WarehouseID =" + WarehouseID + " AND LanguageID = 2", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + WarehouceName + "'");
                QueryBuilderObject.SetField("Address", "N'" + Address + "'");
                QueryBuilderObject.InsertQueryString("WarehouseLanguage", db_vms);
            }
            if (GetFieldValue("WarehouseZone", "WarehouseID", "WarehouseID = " + WarehouseID, db_vms) == string.Empty)
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("ZoneID", "1");
                QueryBuilderObject.InsertQueryString("WarehouseZone", db_vms);
            }
            if (GetFieldValue("WarehouseZoneLanguage", "WarehouseID", "WarehouseID = " + WarehouseID + " AND LanguageID = 1", db_vms) == string.Empty)
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("ZoneID", "1");
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + WarehouceName + " Zone'");
                QueryBuilderObject.InsertQueryString("WarehouseZoneLanguage", db_vms);
            }
            if (GetFieldValue("WarehouseZoneLanguage", "WarehouseID", "WarehouseID = " + WarehouseID + " AND LanguageID = 2", db_vms) == string.Empty)
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("ZoneID", "1");
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + WarehouceName + " Zone'");
                QueryBuilderObject.InsertQueryString("WarehouseZoneLanguage", db_vms);
            }
            if (WarehouseType == "2")
            {
                if (GetFieldValue("Vehicle", "VehicleID", "VehicleID = " + WarehouseID, db_vms) == string.Empty)
                {
                    QueryBuilderObject.SetField("VehicleID", WarehouseID);
                    QueryBuilderObject.SetField("PlateNO", "'" + VehicleRegNum + "'");
                    QueryBuilderObject.SetField("TypeID", "1");
                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    QueryBuilderObject.InsertQueryString("Vehicle", db_vms);
                }
                string fieldValue = GetFieldValue("Employee", "EmployeeID", " EmployeeCode = '" + SalesmanCode + "'", db_vms);
                if (fieldValue == string.Empty)
                {
                    WriteMessage(" ");
                    WriteMessage("Warning Vehicle Code : (" + WarehouseCode + ") is not assigned to any salesperson");
                    WriteMessage(" ");
                }
                string str3 = GetFieldValue("Vehicle", "VehicleID", "VehicleID=" + WarehouseID, db_vms);
                if (!fieldValue.Trim().Equals(string.Empty) && !str3.Trim().Equals(string.Empty))
                {
                    if (GetFieldValue("EmployeeVehicle", "VehicleID", "EmployeeID = " + fieldValue, db_vms) == string.Empty)
                    {
                        QueryBuilderObject.SetField("VehicleID", str3);
                        QueryBuilderObject.SetField("EmployeeID", fieldValue);
                        QueryBuilderObject.InsertQueryString("EmployeeVehicle", db_vms);
                    }
                    else
                    {
                        WriteMessage(" ");
                        WriteMessage("Warning Salesperson Code : (" + SalesmanCode + ") is assigned to 2 vehicles , Second Vehicle Code : (" + WarehouseCode + ") this row is skipped");
                        WriteMessage(" ");
                    }
                }
                if (!fieldValue.Trim().Equals(string.Empty) && !HelperCode.Trim().Equals(string.Empty))
                {
                    string str4 = GetFieldValue("Employee", "EmployeeID", " EmployeeCode = '" + HelperCode + "'", db_vms);
                    if (str4 != string.Empty)
                    {
                        if (GetFieldValue("EmployeeHelper", "HelperID", "employeeID = " + fieldValue, db_vms) == string.Empty)
                        {
                            QueryBuilderObject.SetField("HelperID", str4);
                            QueryBuilderObject.SetField("EmployeeID", fieldValue);
                            QueryBuilderObject.InsertQueryString("EmployeeHelper", db_vms);
                        }
                        else
                        {
                            WriteMessage(" ");
                            WriteMessage("Warning Salesperson Code : (" + SalesmanCode + ") is assigned to 2 Helpers , Second Helper Code : (" + HelperCode + ") this row is skipped");
                            WriteMessage(" ");
                        }
                    }
                }
            }
        }

        private void CreateCustomerOutlet(string CustomerCode, string CustomerGroupDescription, int CustomerTypeID, string Paymentterms, string CustomerDescriptionEnglish, string CustomerAddressEnglish, string CustomerDescriptionArabic, string CustomerAddressArabic, string Phonenumber, string Faxnumber, string OnHold, int Taxable, string HeadOfficeCode, string CreditLimit, string Balance, string CustomerBarCode, string email, string isKeyAccount, string taxNumber, string ConfigGroup)
        {
            string str = "";
            int CustomerID = int.Parse(GetFieldValue("Customer", "CustomerID", "CustomerCode='" + HeadOfficeCode + "'", db_vms));
            string CustomerGroupID = GetFieldValue("CustomerGroupLanguage", "GroupID", " Description = '" + CustomerGroupDescription + "'  AND LanguageID = 1", db_vms);
            if (CustomerGroupID == string.Empty)
            {
                CustomerGroupID = GetFieldValue("CustomerGroup", "isnull(MAX(GroupID),0) + 1", db_vms);
                QueryBuilderObject.SetField("GroupID", CustomerGroupID.ToString());
                QueryBuilderObject.InsertQueryString("CustomerGroup", db_vms);
                QueryBuilderObject.SetField("GroupID", CustomerGroupID.ToString());
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + CustomerGroupDescription + "'");
                QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);
                QueryBuilderObject.SetField("GroupID", CustomerGroupID.ToString());
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + CustomerGroupDescription + "'");
                QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);
            }
            string ConfigGroupID = GetFieldValue("CustomerGroupLanguage", "GroupID", " Description = '" + ConfigGroup + "'  AND LanguageID = 1", db_vms);
            if (ConfigGroupID == string.Empty)
            {
                ConfigGroupID = GetFieldValue("CustomerGroup", "isnull(MAX(GroupID),0) + 1", db_vms);
                QueryBuilderObject.SetField("GroupID", ConfigGroupID.ToString());
                QueryBuilderObject.InsertQueryString("CustomerGroup", db_vms);
                QueryBuilderObject.SetField("GroupID", ConfigGroupID.ToString());
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + ConfigGroup + "'");
                QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);
                QueryBuilderObject.SetField("GroupID", ConfigGroupID.ToString());
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + ConfigGroup + "'");
                QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);
            }
            string PaymentTermID = "1";
            if (CustomerTypeID == 2)
            {
                PaymentTermID = GetFieldValue("tbl_PT_Desc", "PaymentTermID", "Description = '" + CustomerGroupDescription + "'", db_vms);
                if (PaymentTermID == string.Empty)
                {
                    PaymentTermID = GetFieldValue("PaymentTerm", "isnull(MAX(PaymentTermID),0) + 1", db_vms);
                    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                    QueryBuilderObject.SetField("PaymentTermTypeID", "1");
                    QueryBuilderObject.SetField("SimplePeriodWidth", Paymentterms);
                    QueryBuilderObject.SetField("SimplePeriodID", "1");
                    QueryBuilderObject.InsertQueryString("PaymentTerm", db_vms);
                    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                    QueryBuilderObject.SetStringField("Description", CustomerGroupDescription);
                    QueryBuilderObject.InsertQueryString("tbl_PT_Desc", db_vms);
                    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", CustomerGroupDescription);
                    QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms);
                }
            }
            string OutletID = GetFieldValue("CustomerOutlet", "OutletID", string.Concat(new object[] { "CustomerID = ", CustomerID, " AND CustomerCode = '", CustomerCode, "'" }), db_vms);
            if (!OutletID.Trim().Equals(string.Empty))
            {
                QueryBuilderObject.SetField("Taxeable", Taxable.ToString());
                if (taxNumber != string.Empty)
                {
                    QueryBuilderObject.SetStringField("TaxNumber", taxNumber);
                }

                QueryBuilderObject.SetField("CustomerTypeID", CustomerTypeID.ToString());
                QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                QueryBuilderObject.SetField("OnHold", OnHold);
                QueryBuilderObject.SetField("Barcode", "'" + CustomerBarCode + "'");
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("IsKeyAccount", isKeyAccount);
                QueryBuilderObject.UpdateQueryString("CustomerOutlet", string.Concat(new object[] { "  CustomerID = ", CustomerID, " AND OutletID = ", OutletID }), db_vms);
            }
            else
            {
                OutletID = GetFieldValue("CustomerOutlet", "isnull(MAX(OutletID),0) + 1", "CustomerID = " + CustomerID, db_vms);
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("CustomerCode", "'" + CustomerCode + "'");
                QueryBuilderObject.SetField("Barcode", "'" + CustomerBarCode + "'");
                QueryBuilderObject.SetField("Taxeable", Taxable.ToString());
                if (taxNumber != string.Empty)
                {
                    QueryBuilderObject.SetStringField("TaxNumber", taxNumber);
                }
                QueryBuilderObject.SetField("CustomerTypeID", CustomerTypeID.ToString());
                QueryBuilderObject.SetField("CurrencyID", "1");
                QueryBuilderObject.SetField("OnHold", OnHold);
                QueryBuilderObject.SetField("StreetAddress", "0");
                QueryBuilderObject.SetField("InActive", "0");
                QueryBuilderObject.SetField("Notes", "0");
                QueryBuilderObject.SetField("SkipCreditCheck", "0");
                QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("IsKeyAccount", isKeyAccount);
                QueryBuilderObject.InsertQueryString("CustomerOutlet", db_vms);
            }
            inCubeQuery = new InCubeQuery(db_vms, string.Format("DELETE FROM CustomerOutletGroup WHERE CustomerID = {0} AND OutletID = {1}", CustomerID, OutletID));
            inCubeQuery.ExecuteNonQuery();

            QueryBuilderObject = new QueryBuilder();
            QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
            QueryBuilderObject.SetField("OutletID", OutletID);
            QueryBuilderObject.SetField("GroupID", CustomerGroupID.ToString());
            QueryBuilderObject.InsertQueryString("CustomerOutletGroup", db_vms);

            QueryBuilderObject = new QueryBuilder();
            QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
            QueryBuilderObject.SetField("OutletID", OutletID);
            QueryBuilderObject.SetField("GroupID", ConfigGroupID.ToString());
            QueryBuilderObject.InsertQueryString("CustomerOutletGroup", db_vms);

            if (GetFieldValue("CustomerOutletLanguage", "OutletID", string.Concat(new object[] { "CustomerID = ", CustomerID, " AND OutletID = ", OutletID, " AND LanguageID = 1" }), db_vms) != string.Empty)
            {
                QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish + "'");
                QueryBuilderObject.SetField("Address", "'" + CustomerAddressEnglish + "'");
                QueryBuilderObject.UpdateQueryString("CustomerOutletLanguage", string.Concat(new object[] { "  CustomerID = ", CustomerID, " AND OutletID = ", OutletID, " AND LanguageID = 1" }), db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish + "'");
                QueryBuilderObject.SetField("Address", "'" + CustomerAddressEnglish + "'");
                QueryBuilderObject.InsertQueryString("CustomerOutletLanguage", db_vms);
            }
            //if (GetFieldValue("CustomerOutletLanguage", "OutletID", string.Concat(new object[] { "CustomerID = ", CustomerID, " AND OutletID = ", OutletID, " AND LanguageID = 2" }), db_vms) != string.Empty)
            //{
            //    QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionArabic + "'");
            //    QueryBuilderObject.SetField("Address", "N'" + CustomerAddressArabic + "'");
            //    QueryBuilderObject.UpdateQueryString("CustomerOutletLanguage", string.Concat(new object[] { "  CustomerID = ", num, " AND OutletID = ", str4, " AND LanguageID = 2" }), db_vms);
            //}
            //else
            //{
            //    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
            //    QueryBuilderObject.SetField("OutletID", OutletID);
            //    QueryBuilderObject.SetField("LanguageID", "2");
            //    QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionArabic + "'");
            //    QueryBuilderObject.SetField("Address", "N'" + CustomerDescriptionArabic + "'");
            //    QueryBuilderObject.InsertQueryString("CustomerOutletLanguage", db_vms);
            //}
            int num3 = 1;
            int num4 = 1;
            if (GetFieldValue("AccountCust", "AccountID", "CustomerID = " + CustomerID, db_vms) != string.Empty)
            {
                num4 = int.Parse(GetFieldValue("AccountCust", "AccountID", "CustomerID = " + CustomerID, db_vms));
            }
            str = GetFieldValue("AccountCustOut", "AccountID", string.Concat(new object[] { "CustomerID = ", CustomerID, " AND OutletID = ", OutletID }), db_vms);
            if (str == string.Empty)
            {
                num3 = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));
                QueryBuilderObject.SetField("AccountID", num3.ToString());
                QueryBuilderObject.SetField("AccountTypeID", "1");
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.SetField("Balance", Balance);
                QueryBuilderObject.SetField("GL", "0");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.SetField("CurrencyID", "1");
                QueryBuilderObject.SetField("ParentAccountID", num4.ToString());
                QueryBuilderObject.InsertQueryString("Account", db_vms);
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("AccountID", num3.ToString());
                QueryBuilderObject.InsertQueryString("AccountCustOut", db_vms);
                QueryBuilderObject.SetField("AccountID", num3.ToString());
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish.Trim() + " Account'");
                QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
                QueryBuilderObject.SetField("AccountID", num3.ToString());
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionArabic.Trim() + " Account'");
                QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + str, db_vms);
            }
        }

        private void DefaultOrganization()
        {
            OrganizationID = GetFieldValue("Organization", "OrganizationID", "OrganizationID = 1", db_vms);
            if (OrganizationID == string.Empty)
            {
                OrganizationID = "1";
                QueryBuilderObject.SetField("OrganizationID", "1");
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.Date.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.Date.ToString(DateFormat) + "'");
                QueryBuilderObject.InsertQueryString("Organization", db_vms);
                QueryBuilderObject.SetField("OrganizationID", "1");
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'Default Organization'");
                QueryBuilderObject.InsertQueryString("OrganizationLanguage", db_vms);
            }
        }
        string FormedInsertQuery = "";
        string FormedColumnsString = "";
        string FormedValuesString = "";
        private void StartInsertString(string TableName)
        {
            FormedInsertQuery = "";
            FormedColumnsString = "INSERT INTO " + TableName.ToUpper() + " (";
            FormedValuesString = "VALUES (";
        }
        private void AmendColumn(string ColumnName, ColumnType colType, object Value)
        {
            FormedColumnsString += ColumnName.ToUpper() + ",";
            switch (colType)
            {
                case ColumnType.Datetime:
                    DateTime time = DateTime.Parse(Value.ToString());
                    FormedValuesString += "TO_DATE('" + time.ToString("dd/MM/yyyy") + "', 'DD/MM/YYYY'),";
                    break;
                case ColumnType.Decimal:
                case ColumnType.Int:
                    FormedValuesString += Value.ToString() + ",";
                    break;
                default:
                    FormedValuesString += "'" + Value.ToString().Replace("'", "''") + "',";
                    break;
            }
        }
        private void EndInsertString()
        {
            FormedInsertQuery = string.Format(@"{0})
{1})", FormedColumnsString.Substring(0, FormedColumnsString.Length - 1), FormedValuesString.Substring(0, FormedValuesString.Length - 1));
        }

        public override void SendInvoices()
        {
            string TransactionID = "";
            DateTime TransactionDate;
            string WarehouseCode = "";
            string CustomerCode = "";
            string EmployeeCode = "";
            decimal NetTotal = 0;
            decimal GrossTotal = 0;
            string PackName = "";
            string ItemCode = "";
            decimal LineValue = 0;
            decimal Quantity = 0;
            decimal Price = 0;
            decimal LineDiscount = 0;
            string BatchNo = "";
            decimal LineTax = 0;
            decimal LineExcise = 0;
            decimal Discount = 0;
            decimal RemainingAmount = 0;
            string HelperID = "";
            string TransactionTypeID = "";
            string TransType = "";
            string SalesMode = "";
            decimal Tax = 0;
            decimal Excise = 0;

            bool success = false;

            WriteMessage("\r\nSending Invoices ..\r\n");
            string queryString = @" SELECT      [Transaction].TransactionID,  Warehouse.WarehouseCode,  Employee.EmployeeCode, [Transaction].TransactionDate,  CustomerOutlet.CustomerCode
, [Transaction].NetTotal, [Transaction].GrossTotal, [Transaction].RemainingAmount, [Transaction].Discount, [Transaction].HelperID
, [Transaction].TransactionTypeID, [Transaction].SalesMode,[Transaction].Tax,[Transaction].ExciseTax  FROM [Transaction] 
INNER JOIN Employee ON [Transaction].EmployeeID = Employee.EmployeeID 
INNER JOIN  Warehouse ON [Transaction].WarehouseID = Warehouse.WarehouseID  
INNER JOIN CustomerOutlet ON [Transaction].CustomerID = CustomerOutlet.CustomerID AND [Transaction].OutletID = CustomerOutlet.OutletID 
WHERE ([Transaction].Voided = 0 or [Transaction].Voided is null) AND   ([Transaction].Synchronized = 0) AND ([Transaction].TransactionTypeID = 1 or [Transaction].TransactionTypeID = 3)";
            queryString += string.Format("  AND [Transaction].TransactionDate >= '{0}' AND [Transaction].TransactionDate <'{1}' "
                                         , Filters.FromDate.ToString("yyyy/MM/dd"), Filters.ToDate.AddDays(1).ToString("yyyy/MM/dd"));
            if (Filters.EmployeeID != -1)
            {
                queryString = queryString + " AND [Transaction].EmployeeID = " + Filters.EmployeeID;
            }

            inCubeQuery = new InCubeQuery(db_vms, queryString);
            err = inCubeQuery.Execute();
            DataTable dtHeader = inCubeQuery.GetDataTable();

            for (int i = 0; i < dtHeader.Rows.Count; i++)
            {
                try
                {
                    success = true;
                    OpenTransaction();
                    TransactionID = dtHeader.Rows[i]["TransactionID"].ToString();
                    WriteMessage("\r\n" + TransactionID.ToString() + ": ");
                    WarehouseCode = dtHeader.Rows[i]["WarehouseCode"].ToString();
                    EmployeeCode = dtHeader.Rows[i]["EmployeeCode"].ToString();
                    TransactionDate = Convert.ToDateTime(dtHeader.Rows[i]["TransactionDate"]);
                    CustomerCode = dtHeader.Rows[i]["CustomerCode"].ToString();
                    NetTotal = Convert.ToDecimal(dtHeader.Rows[i]["NetTotal"]);
                    GrossTotal = Convert.ToDecimal(dtHeader.Rows[i]["GrossTotal"]);
                    RemainingAmount = Convert.ToDecimal(dtHeader.Rows[i]["RemainingAmount"]);
                    Discount = Convert.ToDecimal(dtHeader.Rows[i]["Discount"]);
                    HelperID = dtHeader.Rows[i]["HelperID"].ToString();
                    TransactionTypeID = dtHeader.Rows[i]["TransactionTypeID"].ToString();
                    SalesMode = dtHeader.Rows[i]["SalesMode"].ToString();
                    Tax = 0;
                    if (AllowTax)
                        Tax = Convert.ToDecimal(dtHeader.Rows[i]["Tax"]);
                    Excise = 0;
                    if (AllowExcise)
                        Excise = Convert.ToDecimal(dtHeader.Rows[i]["ExciseTax"]);

                    string HelperCode = GetFieldValue("Employee", "Employeecode", "EmployeeID = '" + HelperID + "'", db_vms);
                    TransType = "";
                    switch (TransactionTypeID)
                    {
                        case "1":
                            TransType = "INV";
                            break;
                        case "3":
                            TransType = "EXINV";
                            break;
                    }

                    StartInsertString("AWAL_INVAN_SALE_INV_HEAD");
                    AmendColumn("AISIH_INVAN_REF", ColumnType.String, TransactionID);
                    AmendColumn("AISIH_CUST_CODE", ColumnType.String, CustomerCode);
                    AmendColumn("AISIH_SALE_PER_CODE", ColumnType.String, EmployeeCode);
                    AmendColumn("AISIH_DOC_DT", ColumnType.Datetime, TransactionDate);
                    AmendColumn("AISIH_COMP_ID", ColumnType.String, "001");
                    AmendColumn("AISIH_ORN_REF", ColumnType.String, "D");
                    AmendColumn("AISIH_LOCN_CODE", ColumnType.String, WarehouseCode);
                    AmendColumn("AISIH_CURR_CODE", ColumnType.String, CoreGeneral.Common.GeneralConfigurations.CurrencyCode);
                    AmendColumn("AISIH_SHIP_MODE", ColumnType.String, "0");
                    AmendColumn("AISIH_DEL_DATE", ColumnType.Datetime, TransactionDate);
                    AmendColumn("AISIH_SUB_TOTAL", ColumnType.Decimal, GrossTotal);
                    AmendColumn("AISIH_DISCOUNT", ColumnType.Decimal, Discount);
                    AmendColumn("AISIH_TAX", ColumnType.Decimal, 0);
                    AmendColumn("AISIH_TOTAL", ColumnType.Decimal, NetTotal);
                    AmendColumn("AISIH_REM_AMT", ColumnType.Decimal, RemainingAmount);
                    AmendColumn("AISIH_PROCESSED", ColumnType.Decimal, 0);
                    AmendColumn("AISIH_TRF_DATE", ColumnType.Int, "SYSDATE");
                    AmendColumn("AISIH_TYPE", ColumnType.String, TransType);
                    AmendColumn("AISIH_HELP_CODE", ColumnType.String, HelperCode);
                    AmendColumn("AISIH_INV_TYPE", ColumnType.String, SalesMode);
                    if (AllowTax)
                    {
                        AmendColumn("AISIH_TAX_TOTAL", ColumnType.Decimal, Tax);
                    }
                    if (AllowExcise)
                    {
                        AmendColumn("AISIH_EXTAX_TOTAL", ColumnType.Decimal, Excise);
                    }
                    EndInsertString();

                    if (ExecuteOraCMD(FormedInsertQuery, true) != Result.Success)
                    {
                        success = false;
                        WriteMessage("Failed in sending header");
                        Logger.WriteLog(TransactionID.ToString() + " Header", LastQueryString, LastExecError, LoggingType.Error, LoggingFiles.errorInv);
                    }

                    if (success)
                    {
                        string DetailsQuery = @"SELECT TransactionDetail.TransactionID, TransactionDetail.BatchNo, TransactionDetail.Quantity
, TransactionDetail.Price,  TransactionDetail.ExpiryDate,  TransactionDetail.Discount,  TransactionDetail.Tax,  Item.ItemCode
,  ItemLanguage.Description AS ItemName,  PackTypeLanguage.Description AS PackName,  Pack.Quantity AS PcsInCse,  TransactionDetail.PackID
, TransactionDetail.Quantity * TransactionDetail.Price + TransactionDetail.Tax - TransactionDetail.Discount AS Value, TransactionDetail.ExciseTax  
FROM TransactionDetail 
INNER JOIN Pack ON TransactionDetail.PackID = Pack.PackID 
INNER JOIN Item ON Pack.ItemID = Item.ItemID 
INNER JOIN ItemLanguage ON Pack.ItemID = ItemLanguage.ItemID 
INNER JOIN PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID 
WHERE (PackTypeLanguage.LanguageID = 1) AND (ItemLanguage.LanguageID = 1) 
AND (TransactionDetail.TransactionID = '" + TransactionID.ToString() + "')";

                        inCubeQuery = new InCubeQuery(db_vms, DetailsQuery);
                        err = inCubeQuery.Execute();
                        DataTable dtDetails = inCubeQuery.GetDataTable();
                        if (dtDetails.Rows.Count == 0)
                        {
                            throw new Exception("No details found");
                        }
                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        {
                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            Quantity = Convert.ToDecimal(dtDetails.Rows[j]["Quantity"]);
                            LineValue = Convert.ToDecimal(dtDetails.Rows[j]["Value"]);
                            Price = Convert.ToDecimal(dtDetails.Rows[j]["Price"]);
                            PackName = dtDetails.Rows[j]["PackName"].ToString();
                            LineDiscount = Convert.ToDecimal(dtDetails.Rows[j]["Discount"]);
                            BatchNo = dtDetails.Rows[j]["BatchNo"].ToString();
                            LineTax = 0;
                            LineExcise = 0;
                            if (AllowTax)
                            {
                                LineTax = Convert.ToDecimal(dtDetails.Rows[j]["Tax"]);
                            }
                            if (AllowExcise)
                            {
                                LineExcise = Convert.ToDecimal(dtDetails.Rows[j]["ExciseTax"]);
                            }
                            StartInsertString("AWAL_INVAN_SALE_INV_DETAIL");
                            AmendColumn("AISID_INVAN_REF", ColumnType.String, TransactionID);
                            AmendColumn("AISID_SKU_CODE", ColumnType.String, ItemCode);
                            AmendColumn("AISID_UOM_CODE", ColumnType.String, PackName);
                            AmendColumn("AISID_QTY", ColumnType.Decimal, Quantity);
                            AmendColumn("AISID_RATE", ColumnType.Decimal, Price);
                            AmendColumn("AISID_DISCOUNT", ColumnType.Decimal, LineDiscount);
                            AmendColumn("AISID_TAX", ColumnType.Decimal, 0);
                            AmendColumn("AISID_BATCH_NO", ColumnType.String, BatchNo);
                            if (AllowTax)
                                AmendColumn("AISID_VAT", ColumnType.Decimal, LineTax);
                            if (AllowExcise)
                                AmendColumn("AISID_EXTAX", ColumnType.Decimal, LineExcise);
                            EndInsertString();

                            if (ExecuteOraCMD(FormedInsertQuery, true) != Result.Success)
                            {
                                success = false;
                                WriteMessage("Failed in sending details");
                                Logger.WriteLog(TransactionID.ToString() + " Detail", LastQueryString, LastExecError, LoggingType.Error, LoggingFiles.errorInv);
                                break;
                            }
                        }
                    }
                    if (success)
                    {
                        if (CommitTransaction() != Result.Success)
                        {
                            WriteMessage("Failed in committing transaction");
                        }
                        else
                        {
                            WriteMessage("Success ..");
                            QueryBuilderObject.SetField("Synchronized", "1");
                            QueryBuilderObject.UpdateQueryString("[Transaction]", " TransactionID = '" + TransactionID.ToString() + "'", db_vms);
                        }
                    }
                    else
                    {
                        RollbackTransaction();
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                    RollbackTransaction();
                }
            }
        }

        public override void SendReciepts()
        {
            WriteMessage("\r\nSending Payments .. ");

            object PaymentID = null;
            object obj3 = null;
            object obj4 = null;
            object obj5 = null;
            object obj6 = null;
            object obj7 = null;
            object obj8 = null;
            object obj9 = null;
            object obj10 = null;
            object obj11 = null;
            object TransactionID = null;
            object obj13 = null;
            if (Conn.State == ConnectionState.Closed)
            {
                Conn.Open();
            }
            string queryString = @"SELECT CustomerPayment.CustomerPaymentID, CustomerOutlet.CustomerCode,  Employee.EmployeeCode
,  CustomerPayment.PaymentDate,  CustomerPayment.AppliedAmount,  CustomerPayment.VoucherNumber
,  CustomerPayment.VoucherDate,  CustomerPayment.VoucherOwner,  Bank.Code,  CustomerPayment.BranchID
, CustomerPayment.TransactionID, CustomerPayment.PaymentTypeID  
FROM CustomerOutlet 
RIGHT OUTER JOIN CustomerPayment ON CustomerOutlet.OutletID = CustomerPayment.OutletID AND  CustomerOutlet.CustomerID = CustomerPayment.CustomerID 
INNER JOIN Employee ON CustomerPayment.EmployeeID = Employee.EmployeeID 
LEFT outer JOIN Bank ON Bank.BankID = CustomerPayment.BankID 
WHERE (CustomerPayment.Synchronized = 0) 
AND (CustomerPayment.PaymentDate >= '" + Filters.FromDate.Date.ToString("yyyy/MM/dd") + 
"'  AND  CustomerPayment.PaymentDate < '" + Filters.ToDate.Date.AddDays(1.0).ToString("yyyy/MM/dd") + "')";
            queryString += string.Format("  AND [CustomerPayment].PaymentDate >= '{0}' AND [CustomerPayment].PaymentDate <'{1}' "
                                       , Filters.FromDate.ToString("yyyy/MM/dd"), Filters.ToDate.AddDays(1).ToString("yyyy/MM/dd"));
            if (Filters.EmployeeID != -1)
            {
                queryString = queryString + " AND CustomerPayment.EmployeeID = " + Filters.EmployeeID;
            }
            
            InCubeQuery query = new InCubeQuery(db_vms, queryString);
            query.Execute();
            err = query.FindFirst();
            if (err != InCubeErrors.Success)
            {
                WriteMessage("No payments to send ..");
                return;
            }

            ClearProgress();
            SetProgressMax(query.GetDataTable().Rows.Count);
            while (err == InCubeErrors.Success)
            {
                Result res = Result.UnKnown;
                try
                {
                    string str2;
                    err = query.GetField(0, ref PaymentID);
                    err = query.GetField(1, ref obj3);
                    err = query.GetField(2, ref obj4);
                    err = query.GetField(3, ref obj5);
                    err = query.GetField(4, ref obj6);
                    err = query.GetField(5, ref obj7);
                    err = query.GetField(6, ref obj8);
                    err = query.GetField(7, ref obj9);
                    err = query.GetField(8, ref obj10);
                    err = query.GetField(9, ref obj11);
                    err = query.GetField(10, ref TransactionID);
                    err = query.GetField(11, ref obj13);

                    WriteMessage("\r\nPayment " + PaymentID + " over invoice " + TransactionID + ": ");

                    if (obj13.ToString() == "1")
                    {
                        str2 = string.Concat(new object[] {
                        "Insert into AWAL_INVAN_COLLECTION  (AIC_INVAN_REF,AIC_CUST_CODE,  AIC_PAY_DATE,AIC_PAY_TYPE,AIC_AMOUNT,  AIC_VCHR_NO,AIC_VCHR_OWNER,AIC_VCHR_DATE,AIC_INVOICE_ID,AIC_BANK_CODE,AIC_SALEMAN_CODE,AIC_CURR_CODE,AIC_SUB_ACNT_CODE,AIC_DIVN_CODE,AIC_DEPT_CODE,AIC_PROCESSED,AIC_TRF_DATE  )  values  ('", PaymentID, "','", obj3, "','", DateTime.Parse(obj5.ToString()).ToString("dd/MMM/yyyy"), "',  '", obj13, "',", obj6, ",'',  '','','", TransactionID, "','','", obj4, "','",CoreGeneral.Common.GeneralConfigurations.CurrencyCode,"','", obj3,
                        "','003','ACC',0,'", DateTime.Now.ToString("dd/MMM/yyyy"), "')"
                     });
                    }
                    else
                    {
                        str2 = string.Concat(new object[] {
                        "Insert into AWAL_INVAN_COLLECTION  (AIC_INVAN_REF,AIC_CUST_CODE,  AIC_PAY_DATE,AIC_PAY_TYPE,AIC_AMOUNT,  AIC_VCHR_NO,AIC_VCHR_OWNER,AIC_VCHR_DATE,AIC_INVOICE_ID,AIC_BANK_CODE,AIC_SALEMAN_CODE,AIC_CURR_CODE,AIC_SUB_ACNT_CODE,AIC_DIVN_CODE,AIC_DEPT_CODE,AIC_PROCESSED,AIC_TRF_DATE  )  values  ('", PaymentID, "','", obj3, "','", DateTime.Parse(obj5.ToString()).ToString("dd/MMM/yyyy"), "',  '", obj13, "',", obj6, ",'", obj7, "',  '", obj9, "','", DateTime.Parse(obj8.ToString()).ToString("dd/MMM/yyyy"),
                        "','", TransactionID, "','", obj10, "','", obj4, "','",CoreGeneral.Common.GeneralConfigurations.CurrencyCode,"','", obj3, "','003','ACC',0,'", DateTime.Now.ToString("dd/MMM/yyyy"), "')"
                     });

                    }
                    //WriteMessage(str2 + " ");
                    res = ExecuteOraCMD(str2, false);
                    
                }
                catch (Exception ex)
                {
                    res = Result.Failure;
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                }
                finally
                {
                    if (res == Result.Success)
                    {
                        query = new InCubeQuery(db_vms, string.Format("UPDATE CustomerPayment SET Synchronized = 1 WHERE CustomerPaymentID = '{0}' AND TransactionID = '{1}'", PaymentID, TransactionID));
                        query.ExecuteNonQuery();
                        WriteMessage(" Success ..");
                    }
                    else
                    {
                        WriteMessage(" Failure!!");
                    }
                }
                err = query.FindNext();
            }
        }
        private Result OpenTransaction()
        {
            Result res = Result.UnKnown;
            try
            {
                transaction = null;
                if (Conn.State != ConnectionState.Open)
                    Conn.Open();
                transaction = Conn.BeginTransaction();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result CommitTransaction()
        {
            Result res = Result.UnKnown;
            try
            {
                transaction.Commit();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result RollbackTransaction()
        {
            Result res = Result.UnKnown;
            try
            {
                transaction.Rollback();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result GetOracleDataTable(string cmdText, ref DataTable dtData)
        {
            Result res = Result.UnKnown;
            try
            {
                LastQueryString = cmdText;
                LastExecError = "";
                adp = new OracleDataAdapter(cmdText, Conn);
                adp.Fill(dtData);
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                LastExecError = ex.Message;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                adp.Dispose();
            }
            return res;
        }
        private Result ExecuteOraCMD(string cmdText, bool WithinDbTrans)
        {
            Result res = Result.UnKnown;
            try
            {
                LastQueryString = cmdText;
                LastExecError = "";
                cmd = new OracleCommand(cmdText, Conn);
                if (WithinDbTrans)
                    cmd.Transaction = transaction;
                cmd.ExecuteNonQuery();
                
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                LastExecError = ex.Message;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                cmd.Dispose();
            }
            return res;
        }
        public override void SendReturn()
        {
            string TransactionID = "";
            DateTime TransactionDate;
            string WarehouseCode = "";
            string CustomerCode = "";
            string EmployeeCode = "";
            decimal NetTotal = 0;
            decimal GrossTotal = 0;
            string PackName = "";
            string ItemCode = "";
            decimal Value = 0;
            decimal Quantity = 0;
            decimal Price = 0;
            string BatchNo = "";
            decimal LineTax = 0;
            decimal LineExcise = 0;
            decimal Discount = 0;
            decimal LineDiscount = 0;
            decimal RemainingAmount = 0;
            string HelperID = "";
            string TransactionTypeID = "";
            string CustomerRefNo = "";
            string PackStatusID = "";
            string TransType = "";
            string ReasonCode = "";
            decimal TaxTotal = 0;
            decimal ExciseTotal = 0;
            bool success = false;

            WriteMessage("\r\nSending Returns ..\r\n");
            string HeaderQuery = @"SELECT        [Transaction].TransactionID, Warehouse.WarehouseCode, Employee.EmployeeCode, [Transaction].TransactionDate, CustomerOutlet.CustomerCode, 
[Transaction].NetTotal, [Transaction].GrossTotal, 
                         [Transaction].RemainingAmount, [Transaction].Discount, [Transaction].HelperID, 
[Transaction].TransactionTypeID, [Transaction].Tax, ISNULL([Transaction].CustomerRefNo,'') CustomerRefNo, [Transaction].ExciseTax
FROM            [Transaction] INNER JOIN
                         Employee ON [Transaction].EmployeeID = Employee.EmployeeID INNER JOIN
                         Warehouse ON [Transaction].WarehouseID = Warehouse.WarehouseID INNER JOIN
                         CustomerOutlet ON [Transaction].CustomerID = CustomerOutlet.CustomerID AND [Transaction].OutletID = CustomerOutlet.OutletID
WHERE       ([Transaction].Synchronized = 0) AND ([Transaction].TransactionTypeID = 2 OR
                         [Transaction].TransactionTypeID = 4)";
            HeaderQuery += string.Format("  AND [Transaction].TransactionDate >= '{0}' AND [Transaction].TransactionDate <'{1}' "
                                        , Filters.FromDate.ToString("yyyy/MM/dd"),Filters.ToDate.AddDays(1).ToString("yyyy/MM/dd"));
            if (Filters.EmployeeID != -1)
            {
                HeaderQuery = HeaderQuery + " AND [Transaction].EmployeeID = " + Filters.EmployeeID;
            }

            inCubeQuery = new InCubeQuery(db_vms, HeaderQuery);
            err = inCubeQuery.Execute();
            DataTable dtHeader = inCubeQuery.GetDataTable();

            for (int j = 0; j < dtHeader.Rows.Count; j++)
            {
                try
                {
                    success = true;
                    OpenTransaction();
                    TransactionID = dtHeader.Rows[j]["TransactionID"].ToString();
                    WriteMessage("\r\n" + TransactionID.ToString() + ": ");
                    WarehouseCode = dtHeader.Rows[j]["WarehouseCode"].ToString();
                    EmployeeCode = dtHeader.Rows[j]["EmployeeCode"].ToString();
                    TransactionDate = Convert.ToDateTime(dtHeader.Rows[j]["TransactionDate"]);
                    CustomerCode = dtHeader.Rows[j]["CustomerCode"].ToString();
                    NetTotal = Convert.ToDecimal(dtHeader.Rows[j]["NetTotal"]);
                    GrossTotal = Convert.ToDecimal(dtHeader.Rows[j]["GrossTotal"]);
                    RemainingAmount = Convert.ToDecimal(dtHeader.Rows[j]["RemainingAmount"]);
                    Discount = Convert.ToDecimal(dtHeader.Rows[j]["Discount"]);
                    HelperID = dtHeader.Rows[j]["HelperID"].ToString();
                    TransactionTypeID = dtHeader.Rows[j]["TransactionTypeID"].ToString();
                    CustomerRefNo = dtHeader.Rows[j]["CustomerRefNo"].ToString();
                    TaxTotal = 0;
                    if (AllowTax)
                        TaxTotal = Convert.ToDecimal(dtHeader.Rows[j]["Tax"]);
                    ExciseTotal = 0;
                    if (AllowExcise)
                        ExciseTotal = Convert.ToDecimal(dtHeader.Rows[j]["ExciseTax"]);
                    string HelperCode = GetFieldValue("Employee", "Employeecode", "EmployeeID = '" + HelperID + "'", db_vms);
                    TransType = "";
                    switch (TransactionTypeID)
                    {
                        case "2":
                            TransType = "SRTN";
                            break;
                        case "4":
                            TransType = "EXSRTN";
                            break;
                    } 
                    
                    StartInsertString("AWAL_INVAN_SALE_INV_HEAD");
                    AmendColumn("AISIH_INVAN_REF", ColumnType.String, TransactionID);
                    AmendColumn("AISIH_CUST_CODE", ColumnType.String, CustomerCode);
                    AmendColumn("AISIH_SALE_PER_CODE", ColumnType.String, EmployeeCode);
                    AmendColumn("AISIH_DOC_DT", ColumnType.Datetime, TransactionDate);
                    AmendColumn("AISIH_COMP_ID", ColumnType.String, "001");
                    AmendColumn("AISIH_ORN_REF", ColumnType.String, "D");
                    AmendColumn("AISIH_LOCN_CODE", ColumnType.String, WarehouseCode);
                    AmendColumn("AISIH_CURR_CODE", ColumnType.String, CoreGeneral.Common.GeneralConfigurations.CurrencyCode);
                    AmendColumn("AISIH_SHIP_MODE", ColumnType.String, "0");
                    AmendColumn("AISIH_DEL_DATE", ColumnType.Datetime, TransactionDate);
                    AmendColumn("AISIH_SUB_TOTAL", ColumnType.Decimal, GrossTotal);
                    AmendColumn("AISIH_DISCOUNT", ColumnType.Decimal, Discount);
                    AmendColumn("AISIH_TAX", ColumnType.Int, 0);
                    AmendColumn("AISIH_TOTAL", ColumnType.Decimal, NetTotal);
                    AmendColumn("AISIH_REM_AMT", ColumnType.Decimal, RemainingAmount);
                    AmendColumn("AISIH_PROCESSED", ColumnType.Int, 0);
                    AmendColumn("AISIH_TRF_DATE", ColumnType.Int, "SYSDATE");
                    AmendColumn("AISIH_TYPE", ColumnType.String, TransType);
                    AmendColumn("AISIH_HELP_CODE", ColumnType.String, HelperCode);
                    AmendColumn("AISIH_LPO_NO", ColumnType.String, CustomerRefNo);
                    if (AllowTax)
                    {
                        AmendColumn("AISIH_TAX_TOTAL", ColumnType.Decimal, TaxTotal);
                    }
                    if (AllowExcise)
                    {
                        AmendColumn("AISIH_EXTAX_TOTAL", ColumnType.Decimal, ExciseTotal);
                    }
                    EndInsertString();

                    if (ExecuteOraCMD(FormedInsertQuery, true) != Result.Success)
                    {
                        success = false;
                        WriteMessage("Failed in sending header");
                        Logger.WriteLog(TransactionID.ToString() + " Header", LastQueryString, LastExecError, LoggingType.Error, LoggingFiles.errorRet);
                    }

                    if (success)
                    {
                        string DetailsQuery = @"SELECT TransactionDetail.TransactionID, TransactionDetail.BatchNo, TransactionDetail.Quantity
, TransactionDetail.Price,  TransactionDetail.ExpiryDate,  TransactionDetail.Discount,  TransactionDetail.Tax,  Item.ItemCode
,  ItemLanguage.Description AS ItemName,  PackTypeLanguage.Description AS PackName,  Pack.Quantity AS PcsInCse,  TransactionDetail.PackID
, TransactionDetail.Quantity * TransactionDetail.Price + TransactionDetail.Tax - TransactionDetail.Discount AS Value, TransactionDetail.PackStatusID
, TransactionDetail.ExciseTax
FROM TransactionDetail 
INNER JOIN Pack ON TransactionDetail.PackID = Pack.PackID 
INNER JOIN Item ON Pack.ItemID = Item.ItemID 
INNER JOIN ItemLanguage ON Pack.ItemID = ItemLanguage.ItemID 
INNER JOIN PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID 
WHERE (PackTypeLanguage.LanguageID = 1) AND (ItemLanguage.LanguageID = 1) 
AND (TransactionDetail.TransactionID = '" + TransactionID.ToString() + "')";

                        inCubeQuery = new InCubeQuery(db_vms, DetailsQuery);
                        err = inCubeQuery.Execute();
                        DataTable dtDetails = inCubeQuery.GetDataTable();
                        if (dtDetails.Rows.Count == 0)
                        {
                            throw new Exception("No details found");
                        }
                        for (int i = 0; i < dtDetails.Rows.Count; i++)
                        {
                            Quantity = Convert.ToDecimal(dtDetails.Rows[i]["Quantity"]);
                            Value = Convert.ToDecimal(dtDetails.Rows[i]["Value"]);
                            Price = Convert.ToDecimal(dtDetails.Rows[i]["Price"]);
                            LineTax = 0;
                            LineExcise = 0;
                            if (AllowTax)
                                LineTax = Convert.ToDecimal(dtDetails.Rows[i]["Tax"]);
                            if (AllowExcise)
                                LineExcise = Convert.ToDecimal(dtDetails.Rows[i]["ExciseTax"]);
                            ItemCode = dtDetails.Rows[i]["ItemCode"].ToString();
                            PackName = dtDetails.Rows[i]["PackName"].ToString();
                            LineDiscount = Convert.ToDecimal(dtDetails.Rows[i]["Discount"]);
                            BatchNo = dtDetails.Rows[i]["BatchNo"].ToString();
                            PackStatusID = dtDetails.Rows[i]["PackStatusID"].ToString();

                            ReasonCode = "";
                            switch (PackStatusID)
                            {
                                case "1":
                                    ReasonCode = "IDM";
                                    break;
                                case "2":
                                    ReasonCode = "IEX";
                                    break;
                                case "3":
                                    ReasonCode = "IGR";
                                    break;
                            }

                            StartInsertString("AWAL_INVAN_SALE_INV_DETAIL");
                            AmendColumn("AISID_INVAN_REF", ColumnType.String, TransactionID);
                            AmendColumn("AISID_SKU_CODE", ColumnType.String, ItemCode);
                            AmendColumn("AISID_UOM_CODE", ColumnType.String, PackName);
                            AmendColumn("AISID_QTY", ColumnType.Decimal, Quantity);
                            AmendColumn("AISID_RATE", ColumnType.Decimal, Price);
                            AmendColumn("AISID_DISCOUNT", ColumnType.Decimal, LineDiscount);
                            AmendColumn("AISID_TAX", ColumnType.Int, 0);
                            AmendColumn("AISID_BATCH_NO", ColumnType.String, BatchNo);
                            AmendColumn("AISID_reason_code", ColumnType.String, ReasonCode);
                            if (AllowTax)
                                AmendColumn("AISID_VAT", ColumnType.Decimal, LineTax);
                            if (AllowExcise)
                                AmendColumn("AISID_EXTAX", ColumnType.Decimal, LineExcise);
                            EndInsertString();
                            
                            if (ExecuteOraCMD(FormedInsertQuery, true) != Result.Success)
                            {
                                success = false;
                                WriteMessage("Failed in sending details");
                                Logger.WriteLog(TransactionID.ToString() + " Detail", LastQueryString, LastExecError, LoggingType.Error, LoggingFiles.errorRet);
                                break;
                            }
                        }
                    }
                    if (success)
                    {
                        if (CommitTransaction() != Result.Success)
                        {
                            WriteMessage("Failed in committing transaction");
                        }
                        else
                        {
                            WriteMessage("Success ..");
                            QueryBuilderObject.SetField("Synchronized", "1");
                            QueryBuilderObject.UpdateQueryString("[Transaction]", " TransactionID = '" + TransactionID.ToString() + "'", db_vms);
                        }
                    }
                    else
                    {
                        RollbackTransaction();
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                    RollbackTransaction();
                }
            }
        }

        public override void SendTransfers()
        {
            WriteMessage("\r\nSending Load Requests .. ");
            object TransactionID = "";
            object obj3 = "";
            object obj4 = "";
            object obj5 = "";
            object obj6 = "";
            object obj10 = "";
            object obj11 = "";
            object obj13 = "";
            object TransType = "";
            if (Conn.State == ConnectionState.Closed)
            {
                Conn.Open();
            }
            string queryString = @"SELECT  WarehouseTransaction.TransactionID, WarehouseTransaction.TransactionDate
, Warehouse.Warehousecode AS ToWh, Warehouse_1.Warehousecode AS FromWh, WarehouseTransaction.LoadDate
, WarehouseTransaction.TransactionTypeID   
FROM WarehouseTransaction 
INNER JOIN Warehouse ON WarehouseTransaction.WarehouseID = Warehouse.WarehouseID 
INNER JOIN Warehouse AS Warehouse_1 ON WarehouseTransaction.RefWarehouseID = Warehouse_1.WarehouseID  
Where       WarehouseTransaction.Synchronized = 0  
AND (WarehouseTransaction.TransactionDate >= '" + Filters.FromDate.Date.ToString("yyyy/MM/dd") + 
"' AND  WarehouseTransaction.TransactionDate <= '" + Filters.ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") 
+ "')  AND  (WarehouseTransaction.TransactionTypeID = 1 OR WarehouseTransaction.TransactionTypeID = 2 ) "
+ "AND (WarehouseTransaction.WarehouseTransactionStatusID = 2 OR WarehouseTransaction.WarehouseTransactionStatusID = 4)";
            if (Filters.EmployeeID != -1)
            {
                queryString = queryString + " AND RequestedBy = " + Filters.EmployeeID;
            }
            InCubeQuery query = new InCubeQuery(db_vms, queryString);
            err = query.Execute();
            err = query.FindFirst();
            if (err != InCubeErrors.Success)
            {
                WriteMessage("No load requests to send ..");
                return;
            }

            while (err == InCubeErrors.Success)
            {
                Result res = Result.UnKnown;
                try
                {
                    string str2;
                    string str3;
                    InCubeQuery query2;
                    OpenTransaction();
                    err = query.GetField(0, ref TransactionID);
                    WriteMessage("Transaction No: " + TransactionID + ": ");
                    err = query.GetField(1, ref obj3);
                    err = query.GetField(2, ref obj4);
                    err = query.GetField(3, ref obj5);
                    err = query.GetField(4, ref obj6);
                    err = query.GetField(5, ref TransType);
                    DateTime time = DateTime.Parse(obj3.ToString());
                    DateTime now = DateTime.Now;
                    if (DateTime.TryParse(obj6.ToString(), out now))
                    {
                        now = DateTime.Parse(obj6.ToString());
                    }
                    if (TransType.ToString() == "1")
                    {
                        str2 = string.Concat(new object[] { "Insert into AWAL_INVAN_REQ_HEAD  (AIRH_INVAN_REF,AIRH_LOCN_CODE,AIRH_CHARGE_CODE,AIRH_DOC_DT,AIRH_DEL_REQD_DT,AIRH_COMP_ID,AIRH_ORN_REF,AIRH_PROCESSED,AIRH_TRF_DATE)   values  ('", TransactionID, "','", obj4, "','", obj5, "','", time.ToString("dd/MMM/yyyy"), "','", now.ToString("dd/MMM/yyyy"), "','001','D',0,'", DateTime.Now.ToString("dd/MMM/yyyy"), "')" });
                        res = ExecuteOraCMD(str2, true);
                        if (res != Result.Success)
                            throw new Exception("Error inserting header");
                        str3 = "SELECT WhTransDetail.Quantity, Item.ItemCode, PackTypeLanguage.Description FROM WhTransDetail INNER JOIN Pack ON WhTransDetail.PackID = Pack.PackID INNER JOIN Item ON Pack.ItemID = Item.ItemID INNER JOIN PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID WHERE (PackTypeLanguage.LanguageID = 1) AND WhTransDetail.TransactionID = '" + TransactionID + "'";
                        query2 = new InCubeQuery(db_vms, str3);
                        err = query2.Execute();
                        err = query2.FindFirst();
                        if (err != InCubeErrors.Success)
                        {
                            throw new Exception("No details found");
                        }
                        while (err == InCubeErrors.Success)
                        {
                            err = query2.GetField(0, ref obj13);
                            err = query2.GetField(1, ref obj11);
                            err = query2.GetField(2, ref obj10);
                            res = ExecuteOraCMD(string.Concat(new object[] { "Insert into AWAL_INVAN_REQ_DETAIL (AIRD_INVAN_REF,AIRD_SKU_CODE,AIRD_UOM_CODE,AIRD_QTY)   values  ('", TransactionID, "','", obj11, "','", obj10, "',", obj13, ")" }), true);
                            if (res != Result.Success)
                                throw new Exception("Error inserting detail line");
                            err = query2.FindNext();
                        }
                    }
                    else
                    {
                        str2 = string.Concat(new object[] { "Insert into AWAL_INVAN_LTO_HEAD  (AILOH_INVAN_REF,AILOH_FROM_LOCN_CODE,AILOH_TO_LOCN_CODE,AILOH_DOC_DT,AILOH_COMP_ID,AILOH_ORN_REF,AILOH_PROCESSED,AILOH_TRF_DATE)   values  ('", TransactionID, "','", obj4, "','", obj5, "','", time.ToString("dd/MMM/yyyy"), "','001','D',0,'", DateTime.Now.ToString("dd/MMM/yyyy"), "')" });
                        res = ExecuteOraCMD(str2, true);
                        if (res != Result.Success)
                            throw new Exception("Error inserting header");
                        str3 = "SELECT WhTransDetail.Quantity, Item.ItemCode, PackTypeLanguage.Description FROM WhTransDetail INNER JOIN Pack ON WhTransDetail.PackID = Pack.PackID INNER JOIN Item ON Pack.ItemID = Item.ItemID INNER JOIN PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID WHERE (PackTypeLanguage.LanguageID = 1) AND WhTransDetail.TransactionID = '" + TransactionID + "'";
                        query2 = new InCubeQuery(db_vms, str3);
                        err = query2.Execute();
                        err = query2.FindFirst();
                        if (err != InCubeErrors.Success)
                        {
                            throw new Exception("No details found");
                        }
                        while (err == InCubeErrors.Success)
                        {
                            err = query2.GetField(0, ref obj13);
                            err = query2.GetField(1, ref obj11);
                            err = query2.GetField(2, ref obj10);

                            string queryStr = string.Concat(new object[] { "Insert into AWAL_INVAN_LTO_DETAIL (AILOD_INVAN_REF,AILOD_SKU_CODE,AILOD_UOM_CODE,AILOD_QTY)   values  ('", TransactionID, "','", obj11, "','", obj10, "',", obj13, ")" });
                            res = ExecuteOraCMD(queryStr, true);
                            if (res != Result.Success)
                                throw new Exception("Error inserting detail line");
                            err = query2.FindNext();
                        }
                    }
                }
                catch (Exception ex)
                {
                    res = Result.Failure;
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                }
                finally
                {
                    if (res == Result.Success)
                        res = CommitTransaction();
                    else
                        RollbackTransaction();

                    if (res == Result.Success)
                    {
                        query = new InCubeQuery(db_vms, string.Format("UPDATE WarehouseTransaction SET Synchronized = 1 WHERE TransactionID = '{0}'", TransactionID));
                        query.ExecuteNonQuery();
                        WriteMessage(" Success ..");
                    }
                    else
                    {
                        WriteMessage(" Failure!!");
                    }
                }
                err = query.FindNext();
            }
            Conn.Close();
        }
        private bool UpdateCreditInvoiceAmount()
        {
            string sql = "update [Transaction] set RemainingAmount=0 where  Synchronized=1 and TransactionTypeID in(1,3,6) and RemainingAmount>0 and Voided<>1 ";
            InCubeQuery query = new InCubeQuery(sql, db_vms);
            query.ExecuteNonQuery();
            return query.GetCurrentException() == null;
        }
        public override void UpdateInvoice()
        {


            DefaultOrganization();
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            InCubeErrors err;

            object field = new object();

            #region Get Invoices

            DataTable DT = new DataTable();
            #region Query
            string cmdText = "Select TRANSACTIONNO,COSTOMERCODE,OUTLETCODE,SALESMANCODE,TRANSACTIONDATE,NETAMOUNT,REMAININGAMOUNT From  INVAN_MATCHING_NEW  ";
            DataTable dataTable = new DataTable();
            GetOracleDataTable(cmdText, ref DT);
            
            #endregion


            WriteMessage(" ");
            WriteMessage("<<< Updating Invoices >>>  ");

            if (DT.Rows.Count > 0)
                UpdateCreditInvoiceAmount();
            ClearProgress();
            SetProgressMax(DT.Rows.Count);

            for (int i = 0; i < DT.Rows.Count; i++)
            {
                try
                {

                    ReportProgress("Updating Invoices");



                    //  string TranType = DT.Rows[i][0].ToString();//TRANSACTION_TYPE
                    string DocNumber = DT.Rows[i]["TRANSACTIONNO"].ToString();//TRANSACTION_NUMBER
                    DateTime Date = DateTime.Parse(DT.Rows[i]["TRANSACTIONDATE"].ToString());//TRANSACTION_DATE
                    string CustomerNumber = DT.Rows[i]["COSTOMERCODE"].ToString();//CUSTOMERCODE
                    string OutletNumber = DT.Rows[i]["OUTLETCODE"].ToString();//CUSTOMERCODE
                    string SalesID = DT.Rows[i]["SALESMANCODE"].ToString();//SALESPERSON_CODE
                    string Total = DT.Rows[i]["NETAMOUNT"].ToString();//TOTAL_AMOUNT
                    string Ramount = DT.Rows[i]["REMAININGAMOUNT"].ToString();//REMAINING_AMOUNT
                    string Note = "";// DT.Rows[i]["Memo"].ToString();//VOIDED

                    string CustomerID = GetFieldValue("Customer", "CustomerID", "CustomerCode = '" + CustomerNumber + "'", db_vms);
                    string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerCode = '" + OutletNumber + "' and CustomerID=" + CustomerID, db_vms);
                    string AccountID = GetFieldValue("AccountCustOut", "AccountID", "CustomerID = " + CustomerID + " and OutletID=" + OutletID, db_vms);

                    if (CustomerID == string.Empty || OutletID == string.Empty || AccountID == string.Empty)
                    {
                        continue;
                    }

                    string SalespersonID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode ='" + SalesID + "'", db_vms);

                    if (SalespersonID == string.Empty)
                    {
                        continue;
                    }

                    // string RouteID = GetFieldValue("EmployeeTerritory", "TerritoryID", "EmployeeID = " + SalespersonID, db_vms);


                    err = ExistObject("[Transaction]", "TransactionID", "TransactionID ='" + DocNumber + "' and CustomerID=" + CustomerID, db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        TOTALINSERTED++;

                        QueryBuilderObject.SetField("CustomerID", CustomerID);
                        QueryBuilderObject.SetField("OutletID", OutletID);
                        QueryBuilderObject.SetField("TransactionID", "'" + DocNumber + "'");
                        QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                        QueryBuilderObject.SetDateField("TransactionDate", Date);
                        QueryBuilderObject.SetField("TransactionTypeID", "1");
                        QueryBuilderObject.SetField("Discount", "0");
                        QueryBuilderObject.SetField("Synchronized", "1");
                        QueryBuilderObject.SetField("RemainingAmount", Ramount);
                        QueryBuilderObject.SetField("Grosstotal", Total);
                        QueryBuilderObject.SetField("Nettotal", Total);
                        QueryBuilderObject.SetField("Posted", "1");
                        QueryBuilderObject.SetField("DivisionID", "-1");
                        QueryBuilderObject.SetField("AccountID", AccountID);
                        QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                        QueryBuilderObject.SetField("Notes", "'" + Note + "'");

                        QueryBuilderObject.InsertQueryString("[Transaction]", db_vms);
                    }
                    else if (err == InCubeErrors.Success)
                    {
                        TOTALUPDATED++;

                        QueryBuilderObject.SetField("RemainingAmount", Ramount);
                        QueryBuilderObject.SetField("Notes", "'" + Note + "'");
                        QueryBuilderObject.UpdateQueryString("[Transaction]", "TransactionID ='" + DocNumber + "' and CustomerID=" + CustomerID, db_vms);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                }
            }

            if (UpdateAccountBalances())
            { WriteMessage(" <<Update Acount Balances>>"); }


            WriteMessage(" ");
            WriteMessage("<<< INVOICES >>> Total Inserted = " + TOTALINSERTED + " Total Updated = " + TOTALUPDATED);

            #endregion

        }
        private bool UpdateAccountBalances()
        {
            string sql = @" update a set a.balance=b.balance
from account a inner join (
SELECT A.AccountID, isnull(SUM(RemainingAmount),0) balance
FROM CustomerOutlet C
inner join AccountCustOut A on a.CustomerID=c.CustomerID and a.OutletID=c.OutletID 
LEFT JOIN [Transaction] trans ON trans.CustomerID = C.CustomerID AND trans.OutletID = C.OutletID
and TransactionTypeID IN (1,3,6) AND trans.Voided <> 0
GROUP BY C.CustomerID, C.OutletID,A.AccountID) b
on a.AccountID=b.AccountID";
            InCubeQuery query = new InCubeQuery(sql, db_vms);
            query.ExecuteNonQuery();
            return query.GetCurrentException() == null;
        }
        private void UpdateBanks()
        {
            object obj2 = new object();
            string cmdText = "SELECT BankCode,Description,Branchcode,BranchDescription FROM Bank";
            DataTable dataTable = new DataTable();
            GetOracleDataTable(cmdText, ref dataTable);
            ClearProgress();
            SetProgressMax(dataTable.Rows.Count);
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                string str2 = dataTable.Rows[i][0].ToString();
                string str3 = dataTable.Rows[i][1].ToString();
                string str4 = dataTable.Rows[i][2].ToString();
                string str5 = dataTable.Rows[i][3].ToString();
                if (GetFieldValue("Bank", "BankID", "Code = '" + str2 + "'", db_vms) == string.Empty)
                {
                    string fieldValue = GetFieldValue("Bank", "isnull(MAX(BankID),0) + 1", db_vms);
                    QueryBuilderObject.SetField("BankID", fieldValue);
                    QueryBuilderObject.SetField("Code", "'" + str2 + "'");
                    QueryBuilderObject.InsertQueryString("Bank", db_vms);
                    QueryBuilderObject.SetField("BankID", fieldValue);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + str3 + "'");
                    QueryBuilderObject.InsertQueryString("BankLanguage", db_vms);
                    QueryBuilderObject.SetField("BankID", fieldValue);
                    QueryBuilderObject.SetField("LanguageID", "2");
                    QueryBuilderObject.SetField("Description", "'" + str3 + "'");
                    QueryBuilderObject.InsertQueryString("BankLanguage", db_vms);
                    QueryBuilderObject.SetField("BankID", fieldValue);
                    QueryBuilderObject.SetField("BranchID", fieldValue);
                    QueryBuilderObject.InsertQueryString("BankBranch", db_vms);
                    QueryBuilderObject.SetField("BankID", fieldValue);
                    QueryBuilderObject.SetField("BranchID", fieldValue);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + str3 + "'");
                    QueryBuilderObject.InsertQueryString("BankBranchLanguage", db_vms);
                    QueryBuilderObject.SetField("BankID", fieldValue);
                    QueryBuilderObject.SetField("BranchID", fieldValue);
                    QueryBuilderObject.SetField("LanguageID", "2");
                    QueryBuilderObject.SetField("Description", "'" + str3 + "'");
                    QueryBuilderObject.InsertQueryString("BankBranchLanguage", db_vms);
                }
            }
            dataTable.Dispose();
        }

        public override void UpdateCustomer()
        {
            try
            {
                int num = 0;
                int num2 = 0;
                int CustomerTypeID = 0;
                object obj2 = new object();
                string cmdText = "SELECT   CustomerCode,CustomerBarcode,ArabicDescription,EnglishDescription,Phone,Fax,Email,MainAddressEnglish,MainAddressArabic,Taxable,Groups,IsCredit,Creditlimit,Balance,PaymentTerms,OnHold,MasterCustomerCode,isKeyAccount,TaxNumber,InActive,IsCreditOnly FROM Customer";
                DefaultOrganization();
                DataTable dataTable = new DataTable();
                GetOracleDataTable(cmdText, ref dataTable);
                ClearProgress();
                SetProgressMax(dataTable.Rows.Count);
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    ReportProgress("Updating customers");

                    string customerCode = dataTable.Rows[i]["CustomerCode"].ToString();
                    string customerBarCode = dataTable.Rows[i]["CustomerBarcode"].ToString();
                    string customerDescriptionArabic = dataTable.Rows[i]["ArabicDescription"].ToString();
                    customerDescriptionArabic = customerDescriptionArabic.Substring(0, Math.Min(100, customerDescriptionArabic.Length));
                    string customerDescriptionEnglish = dataTable.Rows[i]["EnglishDescription"].ToString();
                    customerDescriptionEnglish = customerDescriptionEnglish.Substring(0, Math.Min(100, customerDescriptionEnglish.Length));
                    string phonenumber = dataTable.Rows[i]["Phone"].ToString();
                    string faxnumber = dataTable.Rows[i]["Fax"].ToString();
                    string email = dataTable.Rows[i]["Email"].ToString();
                    string customerAddressEnglish = dataTable.Rows[i]["MainAddressEnglish"].ToString();
                    customerAddressEnglish = customerAddressEnglish.Substring(0, Math.Min(100, customerAddressEnglish.Length));
                    string customerAddressArabic = dataTable.Rows[i]["MainAddressArabic"].ToString();
                    customerAddressArabic = customerAddressArabic.Substring(0, Math.Min(100, customerAddressArabic.Length));
                    int taxable = 0;
                    if (AllowTax && dataTable.Rows[i]["Taxable"].ToString() != string.Empty)
                    {
                        taxable = int.Parse(dataTable.Rows[i]["Taxable"].ToString());
                    }
                    string customerGroupDescription = dataTable.Rows[i]["Groups"].ToString();
                    string isCredit = dataTable.Rows[i]["IsCredit"].ToString();
                    string CreditOnly = dataTable.Rows[i]["IsCreditOnly"].ToString();
                    string ConfigGroup = "";
                    if (isCredit == "0")
                    {
                        CustomerTypeID = 1;
                        ConfigGroup = "Config_Cash";
                    }
                    else if (isCredit == "1")
                    {
                        CustomerTypeID = 2;
                        if (CreditOnly == "0")
                        {
                            ConfigGroup = "Config_CashCredit";
                        }
                        else
                        {
                            ConfigGroup = "Config_Credit";
                        }
                    }
                    string Creditlimit = dataTable.Rows[i]["Creditlimit"].ToString();
                    string Balance = dataTable.Rows[i]["Balance"].ToString();

                    string paymentterms = "";
                    if (CoreGeneral.Common.GeneralConfigurations.DefaultPaymentTermDays != 0)
                        paymentterms = CoreGeneral.Common.GeneralConfigurations.DefaultPaymentTermDays.ToString();
                    else
                        paymentterms = dataTable.Rows[i]["PaymentTerms"].ToString();

                    string onHold = dataTable.Rows[i]["OnHold"].ToString();
                    string headOfficeCode = dataTable.Rows[i]["MasterCustomerCode"].ToString();
                    string isKeyAccount = dataTable.Rows[i]["isKeyAccount"].ToString();
                    string taxNumber = "";
                    if (AllowTax)
                        taxNumber = dataTable.Rows[i]["TaxNumber"].ToString();
                    string InActive = dataTable.Rows[i]["InActive"].ToString();
                    if (customerCode != string.Empty)
                    {
                        if (isKeyAccount == string.Empty)
                        {
                            isKeyAccount = "0";
                        }
                        string CustomerID = "0";
                        if (headOfficeCode == string.Empty)
                        {
                            CustomerID = GetFieldValue("Customer", "CustomerID", "CustomerCode = '" + customerCode + "'", db_vms);
                            if (CustomerID == string.Empty)
                            {
                                CustomerID = GetFieldValue("Customer", "isnull(MAX(CustomerID),0) + 1", db_vms);
                            }
                            if (GetFieldValue("Customer", "CustomerID", "CustomerID = " + CustomerID, db_vms) != string.Empty)
                            {
                                num++;
                                QueryBuilderObject.SetField("CustomerCode", "'" + customerCode + "'");
                                QueryBuilderObject.SetField("Phone", "'" + phonenumber + "'");
                                QueryBuilderObject.SetField("Fax", "'" + faxnumber + "'");
                                QueryBuilderObject.SetField("OnHold", "0");
                                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                                QueryBuilderObject.UpdateQueryString("Customer", " CustomerID = " + CustomerID, db_vms);
                            }
                            else
                            {
                                num2++;
                                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                                QueryBuilderObject.SetField("Phone", "'" + phonenumber + "'");
                                QueryBuilderObject.SetField("Fax", "'" + faxnumber + "'");
                                QueryBuilderObject.SetField("Email", "'" + email + "'");
                                QueryBuilderObject.SetField("CustomerCode", "'" + customerCode + "'");
                                QueryBuilderObject.SetField("OnHold", "0");
                                QueryBuilderObject.SetField("StreetID", "0");
                                QueryBuilderObject.SetField("StreetAddress", "0");
                                QueryBuilderObject.SetField("Inactive", "0");
                                QueryBuilderObject.SetField("New", "0");
                                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                                InCubeErrors errors = QueryBuilderObject.InsertQueryString("Customer", db_vms);
                            }
                            if (GetFieldValue("CustomerLanguage", "CustomerID", "CustomerID = " + CustomerID + " AND LanguageID = 1", db_vms) != string.Empty)
                            {
                                QueryBuilderObject.SetField("Description", "'" + customerDescriptionEnglish + "'");
                                QueryBuilderObject.SetField("Address", "'" + customerAddressEnglish + "'");
                                QueryBuilderObject.UpdateQueryString("CustomerLanguage", "  CustomerID = " + CustomerID + " AND LanguageID = 1", db_vms);
                            }
                            else
                            {
                                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                                QueryBuilderObject.SetField("LanguageID", "1");
                                QueryBuilderObject.SetField("Description", "'" + customerDescriptionEnglish + "'");
                                QueryBuilderObject.SetField("Address", "'" + customerAddressEnglish + "'");
                                QueryBuilderObject.InsertQueryString("CustomerLanguage", db_vms);
                            }
                            //if (GetFieldValue("CustomerLanguage", "CustomerID", "CustomerID = " + CustomerID + " AND LanguageID = 2", db_vms) == string.Empty)
                            //{
                            //    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                            //    QueryBuilderObject.SetField("LanguageID", "2");
                            //    QueryBuilderObject.SetField("Description", "N'" + customerDescriptionEnglish + "'");
                            //    QueryBuilderObject.SetField("Address", "N'" + customerAddressArabic + "'");
                            //    QueryBuilderObject.InsertQueryString("CustomerLanguage", db_vms);
                            //}
                            int AccountID = 1;
                            if (GetFieldValue("AccountCust", "AccountID", "CustomerID = " + CustomerID, db_vms) != string.Empty)
                            {
                                AccountID = int.Parse(GetFieldValue("AccountCust", "AccountID", "CustomerID = " + CustomerID, db_vms));
                                QueryBuilderObject.SetField("CreditLimit", Creditlimit);
                                QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + AccountID.ToString(), db_vms);
                            }
                            else
                            {
                                AccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));
                                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                                QueryBuilderObject.SetField("AccountTypeID", "1");
                                QueryBuilderObject.SetField("CreditLimit", Creditlimit);
                                QueryBuilderObject.SetField("Balance", Balance);
                                QueryBuilderObject.SetField("GL", "0");
                                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                                QueryBuilderObject.SetField("CurrencyID", "1");
                                QueryBuilderObject.InsertQueryString("Account", db_vms);
                                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                                QueryBuilderObject.InsertQueryString("AccountCust", db_vms);
                                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                                QueryBuilderObject.SetField("LanguageID", "1");
                                QueryBuilderObject.SetField("Description", "'" + customerDescriptionEnglish.Trim() + " Account'");
                                QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
                                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                                QueryBuilderObject.SetField("LanguageID", "2");
                                QueryBuilderObject.SetField("Description", "N'" + customerDescriptionArabic.Trim() + " Account'");
                                QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
                            }
                            CreateCustomerOutlet(customerCode, customerGroupDescription, CustomerTypeID, paymentterms, customerDescriptionEnglish, customerAddressEnglish, customerDescriptionArabic, customerAddressArabic, phonenumber, faxnumber, onHold, taxable, customerCode, Creditlimit, Balance, customerBarCode, email, isKeyAccount, taxNumber, ConfigGroup);
                        }
                        else
                        {
                            CreateCustomerOutlet(customerCode, customerGroupDescription, CustomerTypeID, paymentterms, customerDescriptionEnglish, customerAddressEnglish, customerDescriptionArabic, customerAddressArabic, phonenumber, faxnumber, onHold, taxable, headOfficeCode, Creditlimit, Balance, customerBarCode, email, isKeyAccount, taxNumber, ConfigGroup);
                        }
                    }
                }
                dataTable.Dispose();
                WriteMessage(" ");
                WriteMessage(string.Concat(new object[] { "<<< CUSTOMERS >>> Total Updated = ", num, " , Total Inserted = ", num2 }));
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
            finally
            {
                Conn.Close();
            }
        }

        public override void UpdateItem()
        {
            try
            {
                try
                {
                    int num = 0;
                    int num2 = 0;
                    object obj2 = new object();
                    string cmdText = string.Format("SELECT   ItemCode,ItemdescriptionEnglish,ItemdescriptionArabic, Itemdivision,Divisiondescription,Itemcategory,Categorydescription, Brand,Origin,Pack,Packquantity,Packbarcode{0}{1},InActive FROM Items", AllowTax ? ",Tax" : "", AllowExcise ? ",EXTAX" : "");
                    
                    DefaultOrganization();
                    DataTable dataTable = new DataTable();
                    GetOracleDataTable(cmdText, ref dataTable);
                    ClearProgress();
                    SetProgressMax(dataTable.Rows.Count);
                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        ReportProgress("Updating Items");

                        string ItemCode = dataTable.Rows[i]["ItemCode"].ToString();
                        string ItemdescriptionEnglish = dataTable.Rows[i]["ItemdescriptionEnglish"].ToString();
                        string ItemdescriptionArabic = dataTable.Rows[i]["ItemdescriptionArabic"].ToString();
                        string Itemdivision = dataTable.Rows[i]["Itemdivision"].ToString();
                        string Divisiondescription = dataTable.Rows[i]["Divisiondescription"].ToString();
                        string Itemcategory = dataTable.Rows[i]["Itemcategory"].ToString();
                        string Categorydescription = dataTable.Rows[i]["Categorydescription"].ToString();
                        string Brand = dataTable.Rows[i]["Brand"].ToString();
                        string Origin = dataTable.Rows[i]["Origin"].ToString();
                        string Pack = dataTable.Rows[i]["Pack"].ToString();
                        string Packquantity = dataTable.Rows[i]["Packquantity"].ToString();
                        string Packbarcode = dataTable.Rows[i]["Packbarcode"].ToString();
                        string TaxStr = "0";
                        string ExciseTaxStr = "0";
                        decimal TaxValue = 0;
                        decimal ExciseValue = 0;
                        if (AllowTax)
                        {
                            TaxStr = dataTable.Rows[i]["Tax"].ToString();
                            decimal.TryParse(TaxStr, out TaxValue);
                        }
                        if (AllowExcise)
                        {
                            ExciseTaxStr = dataTable.Rows[i]["EXTAX"].ToString();
                            decimal.TryParse(ExciseTaxStr, out ExciseValue);
                        }
                        
                        string InActive = dataTable.Rows[i]["InActive"].ToString();

                        string ItemID = "";
                        ItemID = GetFieldValue("Item", "ItemID", "ItemCode = '" + ItemCode + "'", db_vms);
                        if (InActive == "1")
                        {
                            if (ItemID != string.Empty)
                            {
                                QueryBuilderObject = new QueryBuilder();
                                QueryBuilderObject.SetField("Inactive", "1");
                                QueryBuilderObject.UpdateQueryString("Item", "ItemID = " + ItemID, db_vms);
                            }
                            continue;
                        }

                        string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode = '" + Itemdivision + "'", db_vms);
                        if (DivisionID == string.Empty)
                        {
                            DivisionID = GetFieldValue("Division", "isnull(MAX(DivisionID),0) + 1", db_vms);
                            QueryBuilderObject.SetField("DivisionID", DivisionID);
                            QueryBuilderObject.SetField("DivisionCode", "'" + Itemdivision + "'");
                            QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                            QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                            QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                            QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                            QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                            QueryBuilderObject.InsertQueryString("Division", db_vms);
                            QueryBuilderObject.SetField("DivisionID", DivisionID);
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + Divisiondescription + "'");
                            QueryBuilderObject.InsertQueryString("DivisionLanguage", db_vms);
                            QueryBuilderObject.SetField("DivisionID", DivisionID);
                            QueryBuilderObject.SetField("LanguageID", "2");
                            QueryBuilderObject.SetField("Description", "'" + Divisiondescription + "'");
                            QueryBuilderObject.InsertQueryString("DivisionLanguage", db_vms);
                        }
                        string ItemCategoryID = GetFieldValue("ItemCategory", "ItemCategoryID", "ItemCategoryCode = '" + Itemcategory + "'", db_vms);
                        if (ItemCategoryID == string.Empty)
                        {
                            ItemCategoryID = GetFieldValue("ItemCategory", "isnull(MAX(ItemCategoryID),0) + 1", db_vms);
                            QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID);
                            QueryBuilderObject.SetField("ItemCategoryCode", "'" + Itemcategory + "'");
                            QueryBuilderObject.SetField("DivisionID", DivisionID.ToString());
                            QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                            QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                            QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                            QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                            QueryBuilderObject.InsertQueryString("ItemCategory", db_vms);
                            QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID);
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + Categorydescription + "'");
                            QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", db_vms);
                            QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID);
                            QueryBuilderObject.SetField("LanguageID", "2");
                            QueryBuilderObject.SetField("Description", "'" + Categorydescription + "'");
                            QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", db_vms);
                        }
                        else
                        {
                            string str16 = GetFieldValue("ItemCategory", "ItemCategoryID", "ItemCategoryCode = '" + Itemcategory + "' AND DivisionID =" + DivisionID.ToString(), db_vms);
                            if (ItemCategoryID == string.Empty)
                            {
                                WriteMessage(" ");
                                WriteMessage(" Item Category " + Categorydescription + " is defined twice , the duplicated division : " + Divisiondescription);
                            }
                        }
                        string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " Description = '" + Pack + "' AND LanguageID = 1", db_vms);
                        if (PackTypeID == string.Empty)
                        {
                            PackTypeID = GetFieldValue("PackType", "isnull(MAX(PackTypeID),0) + 1", db_vms);
                            QueryBuilderObject.SetField("PackTypeID", PackTypeID);
                            QueryBuilderObject.InsertQueryString("PackType", db_vms);
                            QueryBuilderObject.SetField("PackTypeID", PackTypeID);
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + Pack + "'");
                            QueryBuilderObject.InsertQueryString("PackTypeLanguage", db_vms);
                            QueryBuilderObject.SetField("PackTypeID", PackTypeID);
                            QueryBuilderObject.SetField("LanguageID", "2");
                            QueryBuilderObject.SetField("Description", "N'" + Pack + "'");
                            QueryBuilderObject.InsertQueryString("PackTypeLanguage", db_vms);
                        }
                        if (ItemID == string.Empty)
                        {
                            num2++;
                            ItemID = GetFieldValue("Item", "isnull(MAX(ItemID),0) + 1", db_vms);
                            QueryBuilderObject.SetField("ItemID", ItemID);
                            QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());
                            QueryBuilderObject.SetField("ItemCode", "'" + ItemCode + "'");
                            QueryBuilderObject.SetField("InActive", "0");
                            QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                            QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                            QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                            QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                            QueryBuilderObject.SetField("ItemType", "1");
                            QueryBuilderObject.InsertQueryString("Item", db_vms);
                        }
                        else
                        {
                            num++;
                            QueryBuilderObject.SetField("ItemCode", "'" + ItemCode + "'");
                            QueryBuilderObject.SetField("InActive", "0");
                            QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                            QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                            QueryBuilderObject.UpdateQueryString("Item", " ItemID = " + ItemID, db_vms);
                        }

                        if (GetFieldValue("ItemLanguage", "ItemID", "ItemID = " + ItemID + " AND LanguageID = 1", db_vms) != string.Empty)
                        {
                            QueryBuilderObject.SetField("Description", "'" + ItemdescriptionEnglish + "'");
                            QueryBuilderObject.UpdateQueryString("ItemLanguage", " ItemID =" + ItemID + " AND LanguageID = 1", db_vms);
                        }
                        else
                        {
                            QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + ItemdescriptionEnglish + "'");
                            QueryBuilderObject.InsertQueryString("ItemLanguage", db_vms);
                        }
                        if (ItemdescriptionArabic != string.Empty)
                        {
                            //if (GetFieldValue("ItemLanguage", "ItemID", "ItemID = " + ItemID + " AND LanguageID = 2", db_vms) == string.Empty)
                            //{
                            //    QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                            //    QueryBuilderObject.SetField("LanguageID", "2");
                            //    QueryBuilderObject.SetField("Description", "N'" + ItemdescriptionArabic + "'");
                            //    QueryBuilderObject.InsertQueryString("ItemLanguage", db_vms);
                            //}
                        }

                        string PackID = GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " and PackTypeID = " + PackTypeID, db_vms);
                        if (PackID != string.Empty)
                        {
                            //QueryBuilderObject.SetField("Barcode", "'" + Packbarcode + "'");
                            QueryBuilderObject.SetField("Quantity", Packquantity);
                            QueryBuilderObject.SetField("Width", TaxValue.ToString());
                            QueryBuilderObject.SetField("Height", ExciseValue.ToString());
                            QueryBuilderObject.UpdateQueryString("Pack", "PackID = " + PackID, db_vms);
                        }
                        else
                        {
                            PackID = GetFieldValue("Pack", "ISNULL(MAX(PackID),0) + 1", db_vms);
                            QueryBuilderObject.SetField("PackID", PackID);
                            QueryBuilderObject.SetField("Barcode", "'" + Packbarcode + "'");
                            QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                            QueryBuilderObject.SetField("PackTypeID", PackTypeID);
                            QueryBuilderObject.SetField("Quantity", Packquantity);
                            QueryBuilderObject.SetField("EquivalencyFactor", "0");
                            QueryBuilderObject.SetField("HasSerialNumber", "0");
                            QueryBuilderObject.SetField("Width", TaxValue.ToString());
                            QueryBuilderObject.SetField("Height", ExciseValue.ToString());
                            QueryBuilderObject.InsertQueryString("Pack", db_vms);
                        }

                        if (Pack.ToLower() == "ctn")
                        {
                            QueryBuilderObject = new QueryBuilder();
                            QueryBuilderObject.SetField("DefaultPackID", PackID);
                            QueryBuilderObject.UpdateQueryString("Item", "ItemID = " + ItemID, db_vms);
                        }
                    }
                    dataTable.Dispose();
                    WriteMessage(" ");
                    WriteMessage(string.Concat(new object[] { "<<< ITEMS >>> Total Updated = ", num, " , Total Inserted = ", num2 }));
                }
                catch (Exception exception)
                {
                    throw new Exception(exception.Message);
                }
            }
            finally
            {
            }
        }

        public override void UpdatePrice()
        {
            int tOTALUPDATED = 0;
            object obj2 = new object();
            UpdatePriceList(ref tOTALUPDATED);
            WriteMessage(" ");
            WriteMessage("<<< PRICE >>> Total Updated = " + tOTALUPDATED);
        }

        private void UpdatePriceList(ref int TOTALUPDATED)
        {
            object field = new object();
            string cmdText = "SELECT  PricelistCode,PriceListDescription,ItemCode,UOM,Price,0 Tax,IsdefaultPricelist,CustomergroupDescription,customerCode  FROM PriceList";
            DataTable dataTable = new DataTable();
            GetOracleDataTable(cmdText, ref dataTable);
            ClearProgress();
            SetProgressMax(dataTable.Rows.Count);
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                ReportProgress("Updating Price lists");

                TOTALUPDATED++;
                string PricelistCode = dataTable.Rows[i]["PricelistCode"].ToString();
                string PriceListDescription = dataTable.Rows[i]["PriceListDescription"].ToString();
                string ItemCode = dataTable.Rows[i]["ItemCode"].ToString();
                string UOM = dataTable.Rows[i]["UOM"].ToString();
                string Price = dataTable.Rows[i]["Price"].ToString();
                string IsdefaultPricelist = dataTable.Rows[i]["IsdefaultPricelist"].ToString();
                string CustomergroupDescription = dataTable.Rows[i]["CustomergroupDescription"].ToString();
                string customerCode = dataTable.Rows[i]["customerCode"].ToString();
                if ((((PricelistCode != string.Empty) && (ItemCode != string.Empty)) && ((UOM != string.Empty) && (Price != string.Empty))) && (IsdefaultPricelist != string.Empty))
                {
                    Price = Math.Round(decimal.Parse(Price), 3).ToString();
                    string PriceListID = "1";
                    string PriceListType = "1";
                    err = ExistObject("PriceListLanguage", "Description", " Description = '" + PriceListDescription.Replace("'", "''") + "'", db_vms);
                    if (err == InCubeErrors.Success)
                    {
                        PriceListID = GetFieldValue("PriceListLanguage", "PriceListID", " Description = '" + PriceListDescription.Replace("'", "''") + "'", db_vms);
                        if (AllowExcise)
                            PriceListType = GetFieldValue("PriceList", "PriceListTypeID", " PriceListID = " + PriceListID, db_vms);

                        QueryBuilderObject.SetStringField("PriceListCode", PricelistCode);
                        QueryBuilderObject.UpdateQueryString("PriceList", " PriceListID = " + PriceListID, db_vms);
                    }
                    else if (err != InCubeErrors.Error)
                    {
                        PriceListID = GetFieldValue("PriceList", "ISNULL(MAX(PriceListID),0) + 1", db_vms);
                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetStringField("PriceListCode", PricelistCode);
                        QueryBuilderObject.SetField("StartDate", "GETDATE()");
                        QueryBuilderObject.SetField("EndDate", "DATEADD(yyyy,10,GETDATE())");
                        QueryBuilderObject.SetField("Priority", "1");
                        QueryBuilderObject.InsertQueryString("PriceList", db_vms);
                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetStringField("Description", PriceListDescription);
                        QueryBuilderObject.InsertQueryString("PriceListLanguage", db_vms);
                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetStringField("Description", PriceListDescription);
                        QueryBuilderObject.InsertQueryString("PriceListLanguage", db_vms);
                    }
                    if (IsdefaultPricelist == "1")
                    {
                        QueryBuilderObject.SetField("KeyValue", PriceListID);
                        QueryBuilderObject.UpdateQueryString("Configuration", "KeyName = 'DefaultPriceListID' AND EmployeeID = -1", db_vms);
                    }
                    err = ExistObject("PriceQuantityRange", "PriceQuantityRangeID", " PriceQuantityRangeID = 1", db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        QueryBuilderObject.SetField("PriceQuantityRangeID", "1");
                        QueryBuilderObject.SetField("RangeStart", "1");
                        QueryBuilderObject.SetField("RangeEnd", "9999999");
                        QueryBuilderObject.InsertQueryString("PriceQuantityRange", db_vms);
                    }
                    string ItemID = GetFieldValue("Item", "ItemID", "ItemCode = '" + ItemCode + "'", db_vms);

                    if (ItemID != string.Empty)
                    {
                        string PackID = GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " AND PackTypeID = " + GetFieldValue("PackTypeLanguage", "PackTypeID", " LanguageID = 1 and Description = '" + UOM + "'", db_vms), db_vms);
                        string TaxRate = "0";
                        if (AllowTax && PriceListType == "1")
                            TaxRate = GetFieldValue("Pack", "Width", "PackID = " + PackID, db_vms);
                        if (AllowExcise && PriceListType == "3")
                            TaxRate = GetFieldValue("Pack", "Height", "PackID = " + PackID, db_vms);
                        
                        int PriceDefinitionID = 1;
                        string str15 = GetFieldValue("PriceDefinition", "Price", "PackID = " + PackID + " AND PriceListID = " + PriceListID, db_vms);
                        if (str15.Equals(string.Empty))
                        {
                            PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms));
                            QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID.ToString());
                            QueryBuilderObject.SetField("QuantityRangeID", "1");
                            QueryBuilderObject.SetField("PackID", PackID);
                            QueryBuilderObject.SetField("CurrencyID", "1");
                            QueryBuilderObject.SetField("Tax", TaxRate);
                            QueryBuilderObject.SetField("Price", Price);
                            QueryBuilderObject.SetField("PriceListID", PriceListID);
                            QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);
                        }
                        else
                        {
                            PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "PriceDefinitionID", "PackID = " + PackID + " AND PriceListID = " + PriceListID, db_vms));
                            if (!str15.Equals(Price.ToString()))
                            {
                                QueryBuilderObject.SetField("Price", Price);
                                QueryBuilderObject.SetField("Tax", TaxRate);
                                QueryBuilderObject.UpdateQueryString("PriceDefinition", string.Concat(new object[] { "PackID = ", PackID, " AND PriceListID = ", PriceListID, " AND PriceDefinitionID = ", PriceDefinitionID }), db_vms);
                            }
                        }
                        if ((CustomergroupDescription != string.Empty) && (IsdefaultPricelist != "1"))
                        {
                            string GroupID = GetFieldValue("CustomerGroupLanguage", "GroupID", " Description = '" + CustomergroupDescription + "'", db_vms);
                            if (GroupID != string.Empty)
                            {
                                if (GetFieldValue("GroupPrice", "GroupID", " GroupID = " + GroupID + " And PriceListID = " + PriceListID, db_vms) == string.Empty)
                                {
                                    QueryBuilderObject.SetField("GroupID", GroupID);
                                    QueryBuilderObject.SetField("PriceListID", PriceListID);
                                    QueryBuilderObject.InsertQueryString("GroupPrice", db_vms);
                                }
                            }
                        }
                        if ((customerCode != string.Empty) && (IsdefaultPricelist != "1"))
                        {
                            string CustomerID = GetFieldValue("Customer", "CustomerID", " CustomerCode = '" + customerCode + "'", db_vms);
                            if (CustomerID != string.Empty)
                            {
                                InCubeQuery query = new InCubeQuery("SELECT OutletID FROM CustomerOutlet Where CustomerID = " + CustomerID, db_vms);
                                err = query.Execute();
                                err = query.FindFirst();
                                while (err == InCubeErrors.Success)
                                {
                                    query.GetField(0, ref field);
                                    string OutletID = field.ToString();
                                    if (OutletID == string.Empty)
                                    {
                                        WriteMessage("Not exist customer outlet");
                                    }
                                    err = ExistObject("CustomerPrice", "PriceListID", " CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                                    if (err != InCubeErrors.Success)
                                    {
                                        QueryBuilderObject.SetField("CustomerID", CustomerID);
                                        QueryBuilderObject.SetField("OutletID", OutletID);
                                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                                        QueryBuilderObject.InsertQueryString("CustomerPrice", db_vms);
                                    }
                                    err = query.FindNext();
                                }
                            }
                            else
                            {
                                CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", " CustomerCode = '" + customerCode + "'", db_vms);
                                string OutletID = GetFieldValue("CustomerOutlet", "OutletID", " CustomerCode = '" + customerCode + "'", db_vms);
                                if (CustomerID != string.Empty && OutletID != string.Empty)
                                {
                                    err = ExistObject("CustomerPrice", "PriceListID", " CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                                    if (err != InCubeErrors.Success)
                                    {
                                        QueryBuilderObject.SetField("CustomerID", CustomerID);
                                        QueryBuilderObject.SetField("OutletID", OutletID);
                                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                                        QueryBuilderObject.InsertQueryString("CustomerPrice", db_vms);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            dataTable.Dispose();
        }

        public override void UpdateRoutes()
        {
            try
            {
                int num = 0;
                int num2 = 0;
                object obj2 = new object();
                DefaultOrganization();
                string cmdText = "SELECT ROUTENAME, SALESMANCODE, CUSTOMERCODE FROM ROUTES";
                DataTable dataTable = new DataTable();
                GetOracleDataTable(cmdText, ref dataTable);
                ClearProgress();
                SetProgressMax(dataTable.Rows.Count);
                new InCubeQuery(db_vms, "Delete From RouteCustomer").ExecuteNonQuery();
                new InCubeQuery(db_vms, "Delete From CustOutTerritory").ExecuteNonQuery();
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    ReportProgress("Updating Routes");

                    num2++;
                    string str2 = dataTable.Rows[i][0].ToString();
                    string str3 = dataTable.Rows[i][1].ToString();
                    string str4 = dataTable.Rows[i][2].ToString();
                    if (str2 != string.Empty)
                    {
                        string fieldValue = GetFieldValue("RouteLanguage", "RouteID", " Description = '" + str2 + "' AND LanguageID = 1", db_vms);
                        if (fieldValue == string.Empty)
                        {
                            fieldValue = GetFieldValue("Route", "isnull(MAX(RouteID),0) + 1", db_vms);
                        }
                        string str6 = GetFieldValue("CustomerOutlet", "CustomerID", " CustomerCode = '" + str4 + "'", db_vms);
                        string str7 = GetFieldValue("CustomerOutlet", "OutletID", " CustomerCode = '" + str4 + "'", db_vms);
                        if (str7 != string.Empty)
                        {
                            string str8 = GetFieldValue("Employee", "EmployeeID", " EmployeeCode = '" + str3 + "'", db_vms);
                            if (str8 != string.Empty)
                            {
                                if (ExistObject("Territory", "TerritoryID", "TerritoryID = " + fieldValue, db_vms) != InCubeErrors.Success)
                                {
                                    num++;
                                    QueryBuilderObject.SetField("TerritoryID", fieldValue);
                                    QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                                    QueryBuilderObject.InsertQueryString("Territory", db_vms);
                                }
                                if (ExistObject("Route", "RouteID", "RouteID = " + fieldValue, db_vms) != InCubeErrors.Success)
                                {
                                    DateTime time = DateTime.Parse(DateTime.Now.Date.AddHours(7.0).ToString());
                                    DateTime time2 = DateTime.Parse(DateTime.Now.Date.AddHours(23.0).ToString());
                                    QueryBuilderObject.SetField("RouteID", fieldValue);
                                    QueryBuilderObject.SetField("Inactive", "0");
                                    QueryBuilderObject.SetField("TerritoryID", fieldValue);
                                    QueryBuilderObject.SetField("EstimatedStart", "'" + time.ToString("dd/MMM/yyyy HH:mm tt") + "'");
                                    QueryBuilderObject.SetField("EstimatedEnd", "'" + time2.ToString("dd/MMM/yyyy HH:mm tt") + "'");
                                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                                    QueryBuilderObject.InsertQueryString("Route", db_vms);
                                }
                                if (ExistObject("RouteLanguage", "RouteID", "RouteID = " + fieldValue + " AND LanguageID = 1", db_vms) != InCubeErrors.Success)
                                {
                                    QueryBuilderObject.SetField("RouteID", fieldValue);
                                    QueryBuilderObject.SetField("LanguageID", "1");
                                    QueryBuilderObject.SetField("Description", "'" + str2 + "'");
                                    QueryBuilderObject.InsertQueryString("RouteLanguage", db_vms);
                                }
                                if (ExistObject("TerritoryLanguage", "TerritoryID", "TerritoryID = " + fieldValue + " AND LanguageID = 1", db_vms) != InCubeErrors.Success)
                                {
                                    QueryBuilderObject.SetField("TerritoryID", fieldValue);
                                    QueryBuilderObject.SetField("LanguageID", "1");
                                    QueryBuilderObject.SetField("Description", "'" + str2 + "'");
                                    QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);
                                }
                                if (ExistObject("RouteLanguage", "RouteID", "RouteID = " + fieldValue + " AND LanguageID = 2", db_vms) != InCubeErrors.Success)
                                {
                                    QueryBuilderObject.SetField("RouteID", fieldValue);
                                    QueryBuilderObject.SetField("LanguageID", "2");
                                    QueryBuilderObject.SetField("Description", "'" + str2 + "'");
                                    QueryBuilderObject.InsertQueryString("RouteLanguage", db_vms);
                                }
                                if (ExistObject("TerritoryLanguage", "TerritoryID", "TerritoryID = " + fieldValue + " AND LanguageID = 2", db_vms) != InCubeErrors.Success)
                                {
                                    QueryBuilderObject.SetField("TerritoryID", fieldValue);
                                    QueryBuilderObject.SetField("LanguageID", "2");
                                    QueryBuilderObject.SetField("Description", "N'" + str2 + "'");
                                    QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);
                                }
                                if (ExistObject("RouteCustomer", "RouteID", "RouteID = " + fieldValue + " AND CustomerID = " + str6, db_vms) != InCubeErrors.Success)
                                {
                                    num++;
                                    QueryBuilderObject.SetField("RouteID", fieldValue);
                                    QueryBuilderObject.SetField("CustomerID", str6);
                                    QueryBuilderObject.SetField("OutletID", str7);
                                    QueryBuilderObject.InsertQueryString("RouteCustomer", db_vms);
                                }
                                if (ExistObject("RouteVisitPattern", "RouteID", "RouteID = " + fieldValue, db_vms) != InCubeErrors.Success)
                                {
                                    num++;
                                    QueryBuilderObject.SetField("RouteID", fieldValue);
                                    QueryBuilderObject.SetField("Week", "1");
                                    QueryBuilderObject.SetField("Sunday", "1");
                                    QueryBuilderObject.SetField("Monday", "1");
                                    QueryBuilderObject.SetField("Tuesday", "1");
                                    QueryBuilderObject.SetField("Wednesday", "1");
                                    QueryBuilderObject.SetField("Thursday", "1");
                                    QueryBuilderObject.SetField("Friday", "1");
                                    QueryBuilderObject.SetField("Saturday", "1");
                                    QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                                }
                                if ((ExistObject("Employee", "EmployeeID", "EmployeeID = " + str8, db_vms) == InCubeErrors.Success) && (ExistObject("EmployeeTerritory", "EmployeeID", "EmployeeID = " + str8 + " AND TerritoryID = " + fieldValue, db_vms) != InCubeErrors.Success))
                                {
                                    num++;
                                    QueryBuilderObject.SetField("EmployeeID", str8);
                                    QueryBuilderObject.SetField("TerritoryID", fieldValue);
                                    QueryBuilderObject.InsertQueryString("EmployeeTerritory", db_vms);
                                }
                                if (ExistObject("CustOutTerritory", "TerritoryID", "TerritoryID = " + fieldValue + " AND CustomerID = " + str6 + " AND OutletID = " + str7, db_vms) != InCubeErrors.Success)
                                {
                                    num++;
                                    QueryBuilderObject.SetField("CustomerID", str6);
                                    QueryBuilderObject.SetField("OutletID", str7);
                                    QueryBuilderObject.SetField("TerritoryID", fieldValue);
                                    QueryBuilderObject.InsertQueryString("CustOutTerritory", db_vms);
                                }
                            }
                        }
                    }
                }
                dataTable.Dispose();
                WriteMessage(" ");
                WriteMessage("<<< ROUTE >>> Total Inserted = " + num);
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
            finally
            {
                Conn.Close();
            }
        }

        public override void UpdateSalesPerson()
        {
            try
            {
                int tOTALUPDATED = 0;
                int tOTALINSERTED = 0;
                UpdateBanks();
                object obj2 = new object();
                string cmdText = "Select EmployeeCode,NameE,NameA,Phone,Creditlimit,Balance,Division,EmployeeTypeID FROM Employee";
                DefaultOrganization();
                DataTable dataTable = new DataTable();
                GetOracleDataTable(cmdText, ref dataTable);
                ClearProgress();
                SetProgressMax(dataTable.Rows.Count);
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    ReportProgress("Updating Employee");

                    string salespersonCode = dataTable.Rows[i][0].ToString();
                    string salespersonNameEnglish = dataTable.Rows[i][1].ToString();
                    string salespersonNameArabic = dataTable.Rows[i][2].ToString();
                    string phone = dataTable.Rows[i][3].ToString();
                    string creditLimit = dataTable.Rows[i][4].ToString();
                    string balance = dataTable.Rows[i][5].ToString();
                    string divisionID = dataTable.Rows[i][6].ToString();
                    string emplyeeTypeID = dataTable.Rows[i][7].ToString();
                    if (salespersonCode != string.Empty)
                    {
                        string salespersonID = GetFieldValue("Employee", "EmployeeID", "Employeecode = '" + salespersonCode + "'", db_vms);
                        if (salespersonID == string.Empty)
                        {
                            salespersonID = GetFieldValue("Employee", "isnull(MAX(EmployeeID),0) + 1", db_vms);
                        }
                        AddUpdateSalesperson(salespersonID, salespersonCode, salespersonNameArabic, salespersonNameEnglish, phone, ref tOTALUPDATED, ref tOTALINSERTED, divisionID, creditLimit, balance, emplyeeTypeID);
                    }
                }
                dataTable.Dispose();
                WriteMessage(" ");
                WriteMessage(string.Concat(new object[] { "<<< SALESPERSON >>> Total Updated = ", tOTALUPDATED, " , Total Inserted = ", tOTALINSERTED }));
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
            finally
            {
                Conn.Close();
            }
        }

        public override void UpdateStock()
        {

            if (Filters.WarehouseID == -1)
            {
                InCubeQuery query = new InCubeQuery(db_vms, "select WarehouseID from Warehouse where WarehouseTypeID=2");
                DataTable DT = new DataTable();
                err = query.Execute();
                if (err == InCubeErrors.Success)
                {
                    DT = query.GetDataTable();
                    foreach (DataRow row in DT.Rows)
                    {
                        UpdateStockForWarehouse(false, row["WarehouseID"].ToString());
                    }
                }
            }
            else
            {
                UpdateStockForWarehouse(false, Filters.WarehouseID.ToString());
            }
        }

        private void UpdateStockForWarehouse(bool UpdateAll, string _warehouseID)
        {
            try
            {
                int TOTALUPDATED = 0;
                InCubeErrors err;
                object field = new object();

                #region Update Stock

                #region(Check if vehicle Uploaded)
                string WarehouseCode = "";
                WarehouseCode = GetFieldValue("Warehouse", "WarehouseCode", " WarehouseID = " + _warehouseID, db_vms);

                string Query = @"SELECT Uploaded FROM RouteHistory WHERE RouteHistoryID = 
                         (SELECT MAX(RouteHistoryID) FROM RouteHistory WHERE VehicleID = " + _warehouseID + ")";
                InCubeQuery incubeQuery = new InCubeQuery(Query, db_vms);
                err = incubeQuery.ExecuteScalar(ref field);
                if (field != null && field != DBNull.Value && !string.IsNullOrEmpty(field.ToString()) && field.ToString() == "1")
                {
                    WriteMessage("Vehicle (" + WarehouseCode + ") is uploaded");
                    return;
                }

                #endregion
                string DeleteStock = "";
                string SelectWarehouses = "";

                if (UpdateAll)
                {
                    DeleteStock = "delete from WarehouseStock";
                    SelectWarehouses = @"SELECT WarehouseCode, ItemCode, UOM,Quantity,Batch,Expirydate FROM V_Stock";
                }
                else
                {
                    if (IsVehicleUploaded(_warehouseID, db_vms))
                    {
                        WriteMessage(" ");
                        WriteMessage("<<< you cant update the stock for vehicle " + WarehouseCode + " because it is uploaded    >>> Total Updated = " + TOTALUPDATED);
                        return;
                    }
                    DeleteStock = "delete from WarehouseStock Where WarehouseID = " + _warehouseID;
                    SelectWarehouses = "SELECT WarehouseCode, ITEMCODE, UOM,Quantity,Batch,EXPIRYDATE FROM Stock where WarehouseCode='" + WarehouseCode + "'";
                }
                QueryBuilderObject.RunQuery(DeleteStock, db_vms);
                DefaultOrganization();
                DataTable dtWarehouses = new DataTable();

                GetOracleDataTable(SelectWarehouses, ref dtWarehouses);

                ClearProgress();
                SetProgressMax(dtWarehouses.Rows.Count);

                for (int i = 0; i < dtWarehouses.Rows.Count; i++)
                {
                    ReportProgress("Updating Stock" + "(" + WarehouseCode + ") ");

                    TOTALUPDATED++;

                    string VanCode = dtWarehouses.Rows[i][0].ToString();

                    string ItemCode = dtWarehouses.Rows[i][1].ToString();

                    string UOM = dtWarehouses.Rows[i][2].ToString();

                    string Quantity = dtWarehouses.Rows[i][3].ToString();

                    string Batch = dtWarehouses.Rows[i][4].ToString();

                    string ExpiryDate = dtWarehouses.Rows[i][5].ToString();

                    if (VanCode == string.Empty)
                        continue;

                    if (Batch == string.Empty)
                    {
                        Batch = "NoBatch";
                    }

                    try
                    {
                        ExpiryDate = DateTime.Parse(ExpiryDate, EsES).ToString(DateFormat);
                    }
                    catch
                    {
                        ExpiryDate = DateTime.Now.AddDays(10).ToString(DateFormat);
                    }
                    // WriteMessage("read data "+i+"\r\n ");
                    string WarehouseID = GetFieldValue("Warehouse", "WarehouseID", "WarehouseCode = '" + VanCode + "'", db_vms);
                    string ItemID = GetFieldValue("Item", "ItemID", "ItemCode = '" + ItemCode + "'", db_vms);
                    string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " LanguageID = 1 and Description = '" + UOM + "'", db_vms);
                    string PackID = GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " AND PackTypeID = " + PackTypeID, db_vms);
                    //WriteMessage("data " + i +"("+ WarehouseID+","+ItemID+","+PackTypeID+","+PackID+")\r\n ");

                    if (WarehouseID == string.Empty)
                    {
                        continue;
                    }

                    if (ItemID == string.Empty)
                    {
                        continue;
                    }

                    TOTALUPDATED++;

                    string query = "Select PackID from Pack where ItemID = " + ItemID; // if no packtypeid (UOM) defined in pack for this item 
                    InCubeQuery CMD = new InCubeQuery(query, db_vms);
                    CMD.Execute();
                    err = CMD.FindFirst();

                    while (err == InCubeErrors.Success)
                    {

                        try
                        {

                            CMD.GetField(0, ref field);
                            string _packid = field.ToString();

                            string _quantity = "0";

                            err = ExistObject("WarehouseStock", "PackID", "WarehouseID = " + WarehouseID + " AND ZoneID = 1 AND PackID = " + _packid + " AND ExpiryDate = '" + ExpiryDate + "' AND BatchNo = '" + Batch + "'", db_vms);
                            if (err != InCubeErrors.Success)
                            {
                                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                                QueryBuilderObject.SetField("ZoneID", "1");
                                QueryBuilderObject.SetField("PackID", _packid);
                                QueryBuilderObject.SetField("ExpiryDate", "'" + ExpiryDate + "'");
                                QueryBuilderObject.SetField("BatchNo", "'" + Batch + "'");
                                QueryBuilderObject.SetField("SampleQuantity", "0");

                                if (_packid == PackID)
                                {
                                    QueryBuilderObject.SetField("Quantity", Quantity);
                                    QueryBuilderObject.SetField("BaseQuantity", Quantity);
                                }
                                else
                                {
                                    QueryBuilderObject.SetField("Quantity", _quantity);
                                    QueryBuilderObject.SetField("BaseQuantity", _quantity);
                                }

                                QueryBuilderObject.InsertQueryString("WarehouseStock", db_vms);
                            }
                            else if (_packid == PackID)
                            {
                                QueryBuilderObject.SetField("Quantity", Quantity);
                                QueryBuilderObject.SetField("BaseQuantity", Quantity);
                                QueryBuilderObject.UpdateQueryString("WarehouseStock", "WarehouseID = " + WarehouseID + " AND ZoneID = 1 AND PackID = " + PackID + " AND ExpiryDate = '" + ExpiryDate + "' AND BatchNo = '" + Batch + "'", db_vms);
                            }

                            //err = ExistObject("DailyWarehouseStock", "PackID", "WarehouseID = " + WarehouseID + " AND ZoneID = 1 AND PackID = " + _packid + " AND ExpiryDate = '" + ExpiryDate + "' AND BatchNo = '" + Batch + "'", db_vms);
                            //if (err != InCubeErrors.Success)
                            //{
                            //    QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                            //    QueryBuilderObject.SetField("ZoneID", "1");
                            //    QueryBuilderObject.SetField("PackID", _packid);
                            //    QueryBuilderObject.SetField("ExpiryDate", "'" + ExpiryDate + "'");
                            //    QueryBuilderObject.SetField("StockDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                            //    QueryBuilderObject.SetField("BatchNo", "'" + Batch + "'");

                            //    if (_packid == PackID)
                            //    {
                            //        QueryBuilderObject.SetField("Quantity", Quantity);
                            //    }
                            //    else
                            //    {
                            //        QueryBuilderObject.SetField("Quantity", _quantity);
                            //    }

                            //    QueryBuilderObject.SetField("ProductionDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                            //    QueryBuilderObject.SetField("SampleQuantity", "0");
                            //    QueryBuilderObject.InsertQueryString("DailyWarehouseStock", db_vms);
                            //}
                            //else if (_packid == PackID)
                            //{
                            //    QueryBuilderObject.SetField("Quantity", Quantity);
                            //    QueryBuilderObject.UpdateQueryString("DailyWarehouseStock", "WarehouseID = " + WarehouseID + " AND ZoneID = 1 AND PackID = " + PackID + " AND ExpiryDate = '" + ExpiryDate + "' AND BatchNo = '" + Batch + "'", db_vms);
                            //}



                        }
                        catch (Exception ex)
                        {
                            StreamWriter writer = new StreamWriter("errorInv.log", true);
                            writer.Write(ex.StackTrace);
                            writer.Close();
                        }
                        err = CMD.FindNext();
                    }
                    // WriteMessage("insert " + i + "\r\n ");

                }

                dtWarehouses.Dispose();
                WriteMessage(" ");
                WriteMessage("<<< STOCK Updated  " + "(" + WarehouseCode + ") >>> Total Updated = " + TOTALUPDATED);

                #endregion
            }
            catch (Exception ex)
            {
                StreamWriter writer = new StreamWriter("errorInv.log", true);
                writer.Write(ex.StackTrace);
                writer.Close();
            }
            finally
            {
            }
        }

        public override void UpdateWarehouse()
        {
            try
            {
                int tOTALUPDATED = 0;
                int tOTALINSERTED = 0;
                object obj2 = new object();
                string cmdText = "SELECT   WarehouseCode,Description,Platenumber,VehicleType,SalespersonCode,HelperCode FROM Vehicle";
                DefaultOrganization();
                DataTable dataTable = new DataTable();
                GetOracleDataTable(cmdText, ref dataTable);
                ClearProgress();
                SetProgressMax(dataTable.Rows.Count);
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    ReportProgress("Updating WAREHOUSE");

                    string warehouseCode = dataTable.Rows[i][0].ToString();
                    string warehouceName = dataTable.Rows[i][1].ToString();
                    string vehicleRegNum = dataTable.Rows[i][2].ToString();
                    string warehouseType = dataTable.Rows[i][3].ToString();
                    string salesmanCode = dataTable.Rows[i][4].ToString();
                    string helperCode = dataTable.Rows[i][5].ToString();
                    if (warehouseCode != string.Empty)
                    {
                        string address = "";
                        string warehouseID = "";
                        warehouseID = GetFieldValue("Warehouse", "WarehouseID", "WarehouseCode='" + warehouseCode + "'", db_vms);
                        if (warehouseID == string.Empty)
                        {
                            warehouseID = GetFieldValue("Warehouse", "isnull(MAX(WarehouseID),0) + 1", db_vms);
                        }
                        AddUpdateWarehouse(warehouseID, warehouseCode, warehouceName, address, vehicleRegNum, salesmanCode, ref tOTALUPDATED, ref tOTALINSERTED, warehouseType, helperCode);
                    }
                }
                dataTable.Dispose();
                WriteMessage(" ");
                WriteMessage(string.Concat(new object[] { "<<< WAREHOUSE >>> Total Updated = ", tOTALUPDATED, " , Total Inserted = ", tOTALINSERTED }));
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }

        public override void OutStanding()
        {
            try
            {
                GetOracleMasterData(IntegrationField.Outstanding_U);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public void GetOracleMasterData(IntegrationField field)
        {
            Result res = Result.UnKnown;
            string result = "";
            string MasterName = field.ToString().Remove(field.ToString().Length - 2, 2);
            if (CoreGeneral.Common.userPrivileges.UpdateFieldsAccess.ContainsKey(field))
                MasterName = CoreGeneral.Common.userPrivileges.UpdateFieldsAccess[field].Description;
            Dictionary<int, string> filters;
            int ProcessID = 0;
            int rowsCount = 0;

            WriteMessage("\r\nRetrieving " + MasterName + " from Oracle ...");
            try
            {
                //Log begining of read from Oracle
                filters = new Dictionary<int, string>();
                filters.Add(1, MasterName);
                filters.Add(2, "Reading from Oracle");
                ProcessID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                inCubeQuery = new InCubeQuery(db_vms, "SELECT OracleTable,StagingTable,OracleQuery FROM Int_StagingTables WHERE FieldID = " + FieldID);
                inCubeQuery.Execute();
                DataTable dtStaging = inCubeQuery.GetDataTable();

                foreach (DataRow dr in dtStaging.Rows)
                {
                    string OracleTable = dr["OracleTable"].ToString();
                    string OracleQuery = dr["OracleQuery"].ToString().Trim();
                    if (OracleQuery == string.Empty)
                    {
                        OracleQuery = "SELECT * FROM " + OracleTable;
                    }
                    string StagingTable = dr["StagingTable"].ToString();

                    result = SaveTable(OracleQuery, StagingTable, ref rowsCount);
                    if (result == "Success")
                        res = Result.Success;

                    WriteMessage(result);
                    if (res != Result.Success)
                        break;
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("Error !!!");
            }
            finally
            {
                if (res != Result.Success)
                {
                    Initialized = false;
                    WriteMessage("\r\nProcess terminated!!");
                }
                else
                {
                    WriteMessage("\r\nProcessing with SQL procedures ..");
                }
                execManager.LogIntegrationEnding(ProcessID, res, "", res == Result.Success ? "Rows retrieved: " + rowsCount : "");
                WriteMessage("\r\n");
            }
        }
        private string SaveTable(string OracleQuery, string TableName, ref int RowsCount)
        {
            try
            {
                //Get first row in staging table
                inCubeQuery = new InCubeQuery(db_vms, "SELECT TOP 1 * FROM [" + TableName + "]");
                if (inCubeQuery.Execute() != InCubeErrors.Success)
                    return "Error reading from staging table";
                DataTable dtStaging = inCubeQuery.GetDataTable();
                if (dtStaging == null)
                    return "Error reading from staging table";

                //Open Oracle reader
                OracleDataReader dr = null;
                //string getQry = "";
                try
                {
                    Conn.Open();
                    cmd = new OracleCommand(OracleQuery, Conn);
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 1800;
                    dr = cmd.ExecuteReader();
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\nQuery: " + OracleQuery, LoggingType.Error, LoggingFiles.InCubeLog);
                    return "Error in opening Oracle data reader";
                }

                //Bulk copy
                try
                {
                    Dictionary<string, string> ColumnsMapping = new Dictionary<string, string>();
                    DataTable dtData = new DataTable();
                    dtData.Columns.Add("TriggerID");
                    dtData.Columns.Add("ID");
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        string OracleColumn = dr.GetName(i);
                        if (dtStaging.Columns.Contains(OracleColumn))
                        {
                            dtData.Columns.Add(dtStaging.Columns[OracleColumn].ColumnName);
                            ColumnsMapping.Add(OracleColumn, dtStaging.Columns[OracleColumn].ColumnName);
                        }
                    }

                    int count = 0;
                    int ID = 0;
                    DataRow dRow = null;
                    SqlBulkCopy bulk = new SqlBulkCopy(db_vms.GetConnection());
                    bulk.DestinationTableName = TableName;
                    foreach (DataColumn col in dtData.Columns)
                        bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    bulk.BulkCopyTimeout = 300;

                    while (dr.Read())
                    {
                        dRow = dtData.NewRow();
                        dRow["ID"] = ++ID;
                        dRow["TriggerID"] = TriggerID;
                        foreach (KeyValuePair<string, string> pair in ColumnsMapping)
                        {
                            dRow[pair.Value] = dr[pair.Key];
                        }
                        dtData.Rows.Add(dRow);
                        count++;
                        if (count == 1000)
                        {
                            try
                            {
                                bulk.WriteToServer(dtData);
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                            }
                            dtData.Rows.Clear();
                            count = 0;
                        }
                    }

                    if (count > 0)
                    {
                        try
                        {
                            bulk.WriteToServer(dtData);
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        }
                        dtData.Rows.Clear();
                        count = 0;
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                    return "Error writing to staging table";
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return ex.Message;
            }
            return "Success";
        }

    }
}