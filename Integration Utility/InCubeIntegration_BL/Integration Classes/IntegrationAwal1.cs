using InCubeLibrary;
using System;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using InCubeIntegration_DAL;

namespace InCubeIntegration_BL
{
    public class IntegrationAwal : IntegrationBase
    {
        private OleDbConnection Conn;
        private string ConnectionString;
        private string DateFormat;
        private InCubeErrors err;
        private QueryBuilder QueryBuilderObject;
        private long UserID;

        public IntegrationAwal(long CurrentUserID, ExecutionManager ExecManager) 
            : base(ExecManager)
        {
            this.QueryBuilderObject = new QueryBuilder();
            this.DateFormat = "dd/MMM/yyyy";
            this.OrganizationID = "";
            this.ConnectionString = "";
            base.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase) + @"\DataSources.xml";
            XmlDocument document = new XmlDocument();
            document.Load(base.CurrentDirectory);
            this.ConnectionString = document.SelectSingleNode("Connections/Connection[Name = 'InCubeSQL']/Data").InnerText;
            this.Conn = new OleDbConnection(this.ConnectionString);
            try
            {
                this.Conn.Open();
                if (this.Conn.State != ConnectionState.Open)
                {
                    MessageBox.Show("Unable to connect to Intermediate database", "InVan", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                    return;
                }
                this.Conn.Close();
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to connect to Intermediate database", "InVan", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }
            this.UserID = CurrentUserID;
        }

        private void AddUpdateSalesperson(string SalespersonID, string SalespersonCode, string SalespersonNameArabic, string SalespersonNameEnglish, string Phone, ref int TOTALUPDATED, ref int TOTALINSERTED, string DivisionID, string CreditLimit, string Balance, string EmplyeeTypeID)
        {
            string str2 = "";
            if (base.GetFieldValue("Employee", "EmployeeID", "EmployeeID = " + SalespersonID, base.db_vms) == string.Empty)
            {
                TOTALINSERTED++;
                this.QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                this.QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                this.QueryBuilderObject.SetField("EmployeeCode", "'" + SalespersonCode + "'");
                this.QueryBuilderObject.SetField("NationalIDNumber", "0");
                this.QueryBuilderObject.SetField("OrganizationID", this.OrganizationID);
                this.QueryBuilderObject.SetField("InActive", "0");
                this.QueryBuilderObject.SetField("OnHold", "0");
                this.QueryBuilderObject.SetField("EmployeeTypeID", EmplyeeTypeID);
                this.QueryBuilderObject.SetField("CreatedBy", this.UserID.ToString());
                this.QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                this.QueryBuilderObject.SetField("UpdatedBy", this.UserID.ToString());
                this.QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                this.QueryBuilderObject.InsertQueryString("Employee", base.db_vms);
            }
            else
            {
                this.QueryBuilderObject.SetField("OnHold", "0");
                this.QueryBuilderObject.SetField("UpdatedBy", this.UserID.ToString());
                this.QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                this.QueryBuilderObject.UpdateQueryString("Employee", "EmployeeID = " + SalespersonID, base.db_vms);
            }
            if (base.GetFieldValue("EmployeeLanguage", "EmployeeID", "EmployeeID = " + SalespersonID + " AND LanguageID = 1", base.db_vms) != string.Empty)
            {
                TOTALUPDATED++;
                this.QueryBuilderObject.SetField("Description", "'" + SalespersonNameEnglish + "'");
                this.QueryBuilderObject.UpdateQueryString("EmployeeLanguage", "EmployeeID = " + SalespersonID + " AND LanguageID = 1", base.db_vms);
            }
            else
            {
                this.QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                this.QueryBuilderObject.SetField("LanguageID", "1");
                this.QueryBuilderObject.SetField("Description", "'" + SalespersonNameEnglish + "'");
                this.QueryBuilderObject.SetField("Address", "''");
                this.QueryBuilderObject.InsertQueryString("EmployeeLanguage", base.db_vms);
            }
            if (base.GetFieldValue("EmployeeLanguage", "EmployeeID", "EmployeeID = " + SalespersonID + " AND LanguageID = 2", base.db_vms) != string.Empty)
            {
                TOTALUPDATED++;
                this.QueryBuilderObject.SetField("Description", "N'" + SalespersonNameArabic + "'");
                this.QueryBuilderObject.UpdateQueryString("EmployeeLanguage", "EmployeeID = " + SalespersonID + " AND LanguageID = 2", base.db_vms);
            }
            else
            {
                this.QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                this.QueryBuilderObject.SetField("LanguageID", "2");
                this.QueryBuilderObject.SetField("Description", "N'" + SalespersonNameArabic + "'");
                this.QueryBuilderObject.SetField("Address", "''");
                this.QueryBuilderObject.InsertQueryString("EmployeeLanguage", base.db_vms);
            }
            string fieldValue = base.GetFieldValue("EmployeeOperator", "OperatorID", "EmployeeID = " + SalespersonID, base.db_vms);
            if (fieldValue == string.Empty)
            {
                fieldValue = base.GetFieldValue("Operator", "MAX(OperatorID)+1", base.db_vms);
            }
            this.err = base.ExistObject("Operator", "OperatorID", "OperatorID = " + fieldValue, base.db_vms);
            if (this.err == InCubeErrors.DBNoMoreRows)
            {
                this.QueryBuilderObject.SetField("OperatorID", fieldValue);
                this.QueryBuilderObject.SetField("OperatorName", "'" + SalespersonCode + "'");
                this.QueryBuilderObject.SetField("FrontOffice", "1");
                this.QueryBuilderObject.SetField("LoginTypeID", "1");
                this.QueryBuilderObject.InsertQueryString("Operator", base.db_vms);
            }
            this.err = base.ExistObject("EmployeeOperator", "EmployeeID", "EmployeeID = " + SalespersonID, base.db_vms);
            if (this.err == InCubeErrors.DBNoMoreRows)
            {
                this.QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                this.QueryBuilderObject.SetField("OperatorID", fieldValue);
                this.QueryBuilderObject.InsertQueryString("EmployeeOperator", base.db_vms);
            }
            object field = null;
            string str4 = "";
            InCubeQuery query = new InCubeQuery("Select DivisionID from Division", base.db_vms);
            query.Execute();
            this.err = query.FindFirst();
            while (this.err == InCubeErrors.Success)
            {
                query.GetField(0, ref field);
                str4 = field.ToString();
                if (base.GetFieldValue("EmployeeDivision", "EmployeeID", "EmployeeID = " + SalespersonID + " AND DivisionID = " + str4, base.db_vms) == string.Empty)
                {
                    this.QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                    this.QueryBuilderObject.SetField("DivisionID", str4);
                    this.QueryBuilderObject.InsertQueryString("EmployeeDivision", base.db_vms);
                }
                this.err = query.FindNext();
            }
            query.Close();
            if (base.GetFieldValue("EmployeeOrganization", "EmployeeID", "EmployeeID = " + SalespersonID, base.db_vms) == string.Empty)
            {
                this.QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                this.QueryBuilderObject.SetField("OrganizationID", this.OrganizationID);
                this.QueryBuilderObject.InsertQueryString("EmployeeOrganization", base.db_vms);
            }
            int num = 1;
            str2 = base.GetFieldValue("AccountEmp", "AccountID", "EmployeeID = " + SalespersonID, base.db_vms);
            if (str2 == string.Empty)
            {
                num = int.Parse(base.GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", base.db_vms));
                this.QueryBuilderObject.SetField("AccountID", num.ToString());
                this.QueryBuilderObject.SetField("AccountTypeID", "2");
                this.QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                this.QueryBuilderObject.SetField("Balance", Balance);
                this.QueryBuilderObject.SetField("GL", "0");
                this.QueryBuilderObject.SetField("OrganizationID", this.OrganizationID);
                this.QueryBuilderObject.SetField("CurrencyID", "1");
                this.QueryBuilderObject.InsertQueryString("Account", base.db_vms);
                this.QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                this.QueryBuilderObject.SetField("AccountID", num.ToString());
                this.QueryBuilderObject.InsertQueryString("AccountEmp", base.db_vms);
                this.QueryBuilderObject.SetField("AccountID", num.ToString());
                this.QueryBuilderObject.SetField("LanguageID", "1");
                this.QueryBuilderObject.SetField("Description", "'" + SalespersonNameEnglish.Trim() + " Account'");
                this.QueryBuilderObject.InsertQueryString("AccountLanguage", base.db_vms);
                this.QueryBuilderObject.SetField("AccountID", num.ToString());
                this.QueryBuilderObject.SetField("LanguageID", "2");
                this.QueryBuilderObject.SetField("Description", "N'" + SalespersonNameArabic.Trim() + " Account'");
                this.QueryBuilderObject.InsertQueryString("AccountLanguage", base.db_vms);
            }
            else
            {
                this.QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                this.QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + str2, base.db_vms);
            }
        }

        private void AddUpdateWarehouse(string WarehouseID, string WarehouseCode, string WarehouceName, string Address, string VehicleRegNum, string SalesmanCode, ref int TOTALUPDATED, ref int TOTALINSERTED, string WarehouseType, string HelperCode)
        {
            if (base.GetFieldValue("Warehouse", "WarehouseID", "WarehouseID = " + WarehouseID, base.db_vms) != string.Empty)
            {
                TOTALUPDATED++;
                this.QueryBuilderObject.SetField("Phone", "''");
                this.QueryBuilderObject.SetField("WarehouseCode", "'" + WarehouseCode + "'");
                this.QueryBuilderObject.SetField("WarehouseTypeID", WarehouseType);
                this.QueryBuilderObject.SetField("UpdatedBy", this.UserID.ToString());
                this.QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                this.QueryBuilderObject.UpdateQueryString("Warehouse", " WarehouseID = " + WarehouseID, base.db_vms);
            }
            else
            {
                TOTALINSERTED++;
                this.QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                this.QueryBuilderObject.SetField("Phone", "''");
                this.QueryBuilderObject.SetField("WarehouseCode", "'" + WarehouseCode + "'");
                this.QueryBuilderObject.SetField("OrganizationID", this.OrganizationID);
                this.QueryBuilderObject.SetField("WarehouseTypeID", WarehouseType);
                this.QueryBuilderObject.SetField("CreatedBy", this.UserID.ToString());
                this.QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                this.QueryBuilderObject.SetField("UpdatedBy", this.UserID.ToString());
                this.QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                this.QueryBuilderObject.InsertQueryString("Warehouse", base.db_vms);
            }
            if (base.GetFieldValue("WarehouseLanguage", "WarehouseID", "WarehouseID = " + WarehouseID + " AND LanguageID = 1", base.db_vms) != string.Empty)
            {
                this.QueryBuilderObject.SetField("Description", "'" + WarehouceName + "'");
                this.QueryBuilderObject.SetField("Address", "'" + Address + "'");
                this.QueryBuilderObject.UpdateQueryString("WarehouseLanguage", " WarehouseID =" + WarehouseID + " AND LanguageID = 1", base.db_vms);
            }
            else
            {
                this.QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                this.QueryBuilderObject.SetField("LanguageID", "1");
                this.QueryBuilderObject.SetField("Description", "'" + WarehouceName + "'");
                this.QueryBuilderObject.SetField("Address", "'" + Address + "'");
                this.QueryBuilderObject.InsertQueryString("WarehouseLanguage", base.db_vms);
            }
            if (base.GetFieldValue("WarehouseLanguage", "WarehouseID", "WarehouseID = " + WarehouseID + " AND LanguageID = 2", base.db_vms) != string.Empty)
            {
                this.QueryBuilderObject.SetField("Description", "N'" + WarehouceName + "'");
                this.QueryBuilderObject.SetField("Address", "N'" + Address + "'");
                this.QueryBuilderObject.UpdateQueryString("WarehouseLanguage", " WarehouseID =" + WarehouseID + " AND LanguageID = 2", base.db_vms);
            }
            else
            {
                this.QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                this.QueryBuilderObject.SetField("LanguageID", "2");
                this.QueryBuilderObject.SetField("Description", "N'" + WarehouceName + "'");
                this.QueryBuilderObject.SetField("Address", "N'" + Address + "'");
                this.QueryBuilderObject.InsertQueryString("WarehouseLanguage", base.db_vms);
            }
            if (base.GetFieldValue("WarehouseZone", "WarehouseID", "WarehouseID = " + WarehouseID, base.db_vms) == string.Empty)
            {
                this.QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                this.QueryBuilderObject.SetField("ZoneID", "1");
                this.QueryBuilderObject.InsertQueryString("WarehouseZone", base.db_vms);
            }
            if (base.GetFieldValue("WarehouseZoneLanguage", "WarehouseID", "WarehouseID = " + WarehouseID + " AND LanguageID = 1", base.db_vms) == string.Empty)
            {
                this.QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                this.QueryBuilderObject.SetField("ZoneID", "1");
                this.QueryBuilderObject.SetField("LanguageID", "1");
                this.QueryBuilderObject.SetField("Description", "'" + WarehouceName + " Zone'");
                this.QueryBuilderObject.InsertQueryString("WarehouseZoneLanguage", base.db_vms);
            }
            if (base.GetFieldValue("WarehouseZoneLanguage", "WarehouseID", "WarehouseID = " + WarehouseID + " AND LanguageID = 2", base.db_vms) == string.Empty)
            {
                this.QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                this.QueryBuilderObject.SetField("ZoneID", "1");
                this.QueryBuilderObject.SetField("LanguageID", "2");
                this.QueryBuilderObject.SetField("Description", "N'" + WarehouceName + " Zone'");
                this.QueryBuilderObject.InsertQueryString("WarehouseZoneLanguage", base.db_vms);
            }
            if (WarehouseType == "2")
            {
                if (base.GetFieldValue("Vehicle", "VehicleID", "VehicleID = " + WarehouseID, base.db_vms) == string.Empty)
                {
                    this.QueryBuilderObject.SetField("VehicleID", WarehouseID);
                    this.QueryBuilderObject.SetField("PlateNO", "'" + VehicleRegNum + "'");
                    this.QueryBuilderObject.SetField("TypeID", "1");
                    this.QueryBuilderObject.SetField("CreatedBy", this.UserID.ToString());
                    this.QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                    this.QueryBuilderObject.SetField("UpdatedBy", this.UserID.ToString());
                    this.QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                    this.QueryBuilderObject.InsertQueryString("Vehicle", base.db_vms);
                }
                string fieldValue = base.GetFieldValue("Employee", "EmployeeID", " EmployeeCode = '" + SalesmanCode + "'", base.db_vms);
                if (fieldValue == string.Empty)
                {
                    WriteMessage("\r\n");
                    WriteMessage("Warning Vehicle Code : (" + WarehouseCode + ") is not assigned to any salesperson");
                    WriteMessage("\r\n");
                }
                string str3 = base.GetFieldValue("Vehicle", "VehicleID", "VehicleID=" + WarehouseID, base.db_vms);
                if (!fieldValue.Trim().Equals(string.Empty) && !str3.Trim().Equals(string.Empty))
                {
                    if (base.GetFieldValue("EmployeeVehicle", "VehicleID", "EmployeeID = " + fieldValue, base.db_vms) == string.Empty)
                    {
                        this.QueryBuilderObject.SetField("VehicleID", str3);
                        this.QueryBuilderObject.SetField("EmployeeID", fieldValue);
                        this.QueryBuilderObject.InsertQueryString("EmployeeVehicle", base.db_vms);
                    }
                    else
                    {
                        WriteMessage("\r\n");
                        WriteMessage("Warning Salesperson Code : (" + SalesmanCode + ") is assigned to 2 vehicles , Second Vehicle Code : (" + WarehouseCode + ") this row is skipped");
                        WriteMessage("\r\n");
                    }
                }
                if (!fieldValue.Trim().Equals(string.Empty) && !HelperCode.Trim().Equals(string.Empty))
                {
                    string str4 = base.GetFieldValue("Employee", "EmployeeID", " EmployeeCode = '" + HelperCode + "'", base.db_vms);
                    if (str4 != string.Empty)
                    {
                        if (base.GetFieldValue("EmployeeHelper", "HelperID", "employeeID = " + fieldValue, base.db_vms) == string.Empty)
                        {
                            this.QueryBuilderObject.SetField("HelperID", str4);
                            this.QueryBuilderObject.SetField("EmployeeID", fieldValue);
                            this.QueryBuilderObject.InsertQueryString("EmployeeHelper", base.db_vms);
                        }
                        else
                        {
                            WriteMessage("\r\n");
                            WriteMessage("Warning Salesperson Code : (" + SalesmanCode + ") is assigned to 2 Helpers , Second Helper Code : (" + HelperCode + ") this row is skipped");
                            WriteMessage("\r\n");
                        }
                    }
                }
            }
        }

        private void CreateCustomerOutlet(string CustomerCode, string CustomerGroupDescription, string IsCreditCustomer, string Paymentterms, string CustomerDescriptionEnglish, string CustomerAddressEnglish, string CustomerDescriptionArabic, string CustomerAddressArabic, string Phonenumber, string Faxnumber, string OnHold, string Taxable, string HeadOfficeCode, string CreditLimit, string Balance, string CustomerBarCode, string email, string isKeyAccount)
        {
            int num2;
            string str = "";
            int num = int.Parse(base.GetFieldValue("Customer", "CustomerID", "CustomerCode='" + HeadOfficeCode + "'", base.db_vms));
            string str2 = base.GetFieldValue("CustomerGroupLanguage", "GroupID", " Description = '" + CustomerGroupDescription + "'  AND LanguageID = 1", base.db_vms);
            if (str2 == string.Empty)
            {
                str2 = base.GetFieldValue("CustomerGroup", "isnull(MAX(GroupID),0) + 1", base.db_vms);
                this.QueryBuilderObject.SetField("GroupID", str2.ToString());
                this.QueryBuilderObject.InsertQueryString("CustomerGroup", base.db_vms);
                this.QueryBuilderObject.SetField("GroupID", str2.ToString());
                this.QueryBuilderObject.SetField("LanguageID", "1");
                this.QueryBuilderObject.SetField("Description", "'" + CustomerGroupDescription + "'");
                this.QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", base.db_vms);
                this.QueryBuilderObject.SetField("GroupID", str2.ToString());
                this.QueryBuilderObject.SetField("LanguageID", "2");
                this.QueryBuilderObject.SetField("Description", "N'" + CustomerGroupDescription + "'");
                this.QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", base.db_vms);
            }
            string fieldValue = "1";
            if (IsCreditCustomer == "2")
            {
                num2 = 2;
                fieldValue = base.GetFieldValue("PaymentTerm", "PaymentTermID", "SimplePeriodWidth = " + Paymentterms, base.db_vms);
                if (fieldValue == string.Empty)
                {
                    fieldValue = base.GetFieldValue("PaymentTerm", "isnull(MAX(PaymentTermID),0) + 1", base.db_vms);
                    this.QueryBuilderObject.SetField("PaymentTermID", fieldValue);
                    this.QueryBuilderObject.SetField("PaymentTermTypeID", "1");
                    this.QueryBuilderObject.SetField("SimplePeriodWidth", Paymentterms);
                    this.QueryBuilderObject.SetField("SimplePeriodID", "1");
                    this.QueryBuilderObject.InsertQueryString("PaymentTerm", base.db_vms);
                    this.QueryBuilderObject.SetField("PaymentTermID", fieldValue);
                    this.QueryBuilderObject.SetField("LanguageID", "1");
                    this.QueryBuilderObject.SetField("Description", "'Every " + Paymentterms + " Days'");
                    this.QueryBuilderObject.InsertQueryString("PaymentTermLanguage", base.db_vms);
                    this.QueryBuilderObject.SetField("PaymentTermID", fieldValue);
                    this.QueryBuilderObject.SetField("LanguageID", "2");
                    this.QueryBuilderObject.SetField("Description", "'Every " + Paymentterms + " Days'");
                    this.QueryBuilderObject.InsertQueryString("PaymentTermLanguage", base.db_vms);
                }
            }
            else
            {
                num2 = 1;
            }
            string str4 = base.GetFieldValue("CustomerOutlet", "OutletID", string.Concat(new object[] { "CustomerID = ", num, " AND CustomerCode = '", CustomerCode, "'" }), base.db_vms);
            if (!str4.Trim().Equals(string.Empty))
            {
                this.QueryBuilderObject.SetField("CustomerTypeID", num2.ToString());
                this.QueryBuilderObject.SetField("OnHold", OnHold);
                this.QueryBuilderObject.SetField("Barcode", "'" + CustomerBarCode + "'");
                this.QueryBuilderObject.SetField("UpdatedBy", this.UserID.ToString());
                this.QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                this.QueryBuilderObject.SetField("IsKeyAccount", isKeyAccount);
                this.QueryBuilderObject.UpdateQueryString("CustomerOutlet", string.Concat(new object[] { "  CustomerID = ", num, " AND OutletID = ", str4 }), base.db_vms);
            }
            else
            {
                str4 = base.GetFieldValue("CustomerOutlet", "isnull(MAX(OutletID),0) + 1", "CustomerID = " + num, base.db_vms);
                this.QueryBuilderObject.SetField("CustomerID", num.ToString());
                this.QueryBuilderObject.SetField("OutletID", str4);
                this.QueryBuilderObject.SetField("CustomerCode", "'" + CustomerCode + "'");
                this.QueryBuilderObject.SetField("Barcode", "'" + CustomerBarCode + "'");
                this.QueryBuilderObject.SetField("Taxeable", Taxable);
                this.QueryBuilderObject.SetField("CustomerTypeID", num2.ToString());
                this.QueryBuilderObject.SetField("CurrencyID", "1");
                this.QueryBuilderObject.SetField("OnHold", OnHold);
                this.QueryBuilderObject.SetField("StreetAddress", "0");
                this.QueryBuilderObject.SetField("InActive", "0");
                this.QueryBuilderObject.SetField("Notes", "0");
                this.QueryBuilderObject.SetField("SkipCreditCheck", "0");
                this.QueryBuilderObject.SetField("PaymentTermID", fieldValue);
                this.QueryBuilderObject.SetField("CreatedBy", this.UserID.ToString());
                this.QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                this.QueryBuilderObject.SetField("UpdatedBy", this.UserID.ToString());
                this.QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                this.QueryBuilderObject.SetField("IsKeyAccount", isKeyAccount);
                this.QueryBuilderObject.InsertQueryString("CustomerOutlet", base.db_vms);
            }
            new InCubeQuery(base.db_vms, string.Concat(new object[] { "Delete From CustomerOutletGroup Where CustomerID = ", num, " AND OutletID = ", str4 })).ExecuteNonQuery();
            if (base.ExistObject("CustomerOutletGroup", "GroupID", string.Concat(new object[] { "CustomerID = ", num, " AND OutletID = ", str4 }), base.db_vms) != InCubeErrors.Success)
            {
                this.QueryBuilderObject.SetField("CustomerID", num.ToString());
                this.QueryBuilderObject.SetField("OutletID", str4);
                this.QueryBuilderObject.SetField("GroupID", str2.ToString());
                this.QueryBuilderObject.InsertQueryString("CustomerOutletGroup", base.db_vms);
            }
            if (base.GetFieldValue("CustomerOutletLanguage", "OutletID", string.Concat(new object[] { "CustomerID = ", num, " AND OutletID = ", str4, " AND LanguageID = 1" }), base.db_vms) != string.Empty)
            {
                this.QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish + "'");
                this.QueryBuilderObject.SetField("Address", "'" + CustomerAddressEnglish + "'");
                this.QueryBuilderObject.UpdateQueryString("CustomerOutletLanguage", string.Concat(new object[] { "  CustomerID = ", num, " AND OutletID = ", str4, " AND LanguageID = 1" }), base.db_vms);
            }
            else
            {
                this.QueryBuilderObject.SetField("CustomerID", num.ToString());
                this.QueryBuilderObject.SetField("OutletID", str4);
                this.QueryBuilderObject.SetField("LanguageID", "1");
                this.QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish + "'");
                this.QueryBuilderObject.SetField("Address", "'" + CustomerAddressEnglish + "'");
                this.QueryBuilderObject.InsertQueryString("CustomerOutletLanguage", base.db_vms);
            }
            if (base.GetFieldValue("CustomerOutletLanguage", "OutletID", string.Concat(new object[] { "CustomerID = ", num, " AND OutletID = ", str4, " AND LanguageID = 2" }), base.db_vms) != string.Empty)
            {
                this.QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionArabic + "'");
                this.QueryBuilderObject.SetField("Address", "N'" + CustomerAddressArabic + "'");
                this.QueryBuilderObject.UpdateQueryString("CustomerOutletLanguage", string.Concat(new object[] { "  CustomerID = ", num, " AND OutletID = ", str4, " AND LanguageID = 2" }), base.db_vms);
            }
            else
            {
                this.QueryBuilderObject.SetField("CustomerID", num.ToString());
                this.QueryBuilderObject.SetField("OutletID", str4);
                this.QueryBuilderObject.SetField("LanguageID", "2");
                this.QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionArabic + "'");
                this.QueryBuilderObject.SetField("Address", "N'" + CustomerAddressArabic + "'");
                this.QueryBuilderObject.InsertQueryString("CustomerOutletLanguage", base.db_vms);
            }
            int num3 = 1;
            int num4 = 1;
            if (base.GetFieldValue("AccountCust", "AccountID", "CustomerID = " + num, base.db_vms) != string.Empty)
            {
                num4 = int.Parse(base.GetFieldValue("AccountCust", "AccountID", "CustomerID = " + num, base.db_vms));
            }
            str = base.GetFieldValue("AccountCustOut", "AccountID", string.Concat(new object[] { "CustomerID = ", num, " AND OutletID = ", str4 }), base.db_vms);
            if (str == string.Empty)
            {
                num3 = int.Parse(base.GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", base.db_vms));
                this.QueryBuilderObject.SetField("AccountID", num3.ToString());
                this.QueryBuilderObject.SetField("AccountTypeID", "1");
                this.QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                this.QueryBuilderObject.SetField("Balance", Balance);
                this.QueryBuilderObject.SetField("GL", "0");
                this.QueryBuilderObject.SetField("OrganizationID", this.OrganizationID);
                this.QueryBuilderObject.SetField("CurrencyID", "1");
                this.QueryBuilderObject.SetField("ParentAccountID", num4.ToString());
                this.QueryBuilderObject.InsertQueryString("Account", base.db_vms);
                this.QueryBuilderObject.SetField("CustomerID", num.ToString());
                this.QueryBuilderObject.SetField("OutletID", str4);
                this.QueryBuilderObject.SetField("AccountID", num3.ToString());
                this.QueryBuilderObject.InsertQueryString("AccountCustOut", base.db_vms);
                this.QueryBuilderObject.SetField("AccountID", num3.ToString());
                this.QueryBuilderObject.SetField("LanguageID", "1");
                this.QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish.Trim() + " Account'");
                this.QueryBuilderObject.InsertQueryString("AccountLanguage", base.db_vms);
                this.QueryBuilderObject.SetField("AccountID", num3.ToString());
                this.QueryBuilderObject.SetField("LanguageID", "2");
                this.QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionArabic.Trim() + " Account'");
                this.QueryBuilderObject.InsertQueryString("AccountLanguage", base.db_vms);
            }
            else
            {
                this.QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                this.QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + str, base.db_vms);
            }
        }

        private void DefaultOrganization()
        {
            this.OrganizationID = base.GetFieldValue("Organization", "OrganizationID", "OrganizationID = 1", base.db_vms);
            if (this.OrganizationID == string.Empty)
            {
                this.OrganizationID = "1";
                this.QueryBuilderObject.SetField("OrganizationID", "1");
                this.QueryBuilderObject.SetField("CreatedBy", this.UserID.ToString());
                this.QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.Date.ToString(this.DateFormat) + "'");
                this.QueryBuilderObject.SetField("UpdatedBy", this.UserID.ToString());
                this.QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.Date.ToString(this.DateFormat) + "'");
                this.QueryBuilderObject.InsertQueryString("Organization", base.db_vms);
                this.QueryBuilderObject.SetField("OrganizationID", "1");
                this.QueryBuilderObject.SetField("LanguageID", "1");
                this.QueryBuilderObject.SetField("Description", "'Default Organization'");
                this.QueryBuilderObject.InsertQueryString("OrganizationLanguage", base.db_vms);
            }
        }

        public override void SendInvoices(bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate)
        {
            object field = "";
            object obj3 = "";
            object obj4 = "";
            object obj5 = "";
            object obj6 = "";
            object obj7 = "";
            object obj8 = "";
            object obj9 = "";
            object obj10 = "";
            object obj11 = "";
            object obj12 = "";
            object obj13 = "";
            object obj14 = "";
            object obj15 = "";
            object obj16 = "";
            object obj17 = "";
            object obj18 = "";
            object obj19 = "";
            string str = "";
            object obj20 = "";
            object obj21 = "";
            if (this.Conn.State == ConnectionState.Closed)
            {
                this.Conn.Open();
            }
            OleDbTransaction transaction = null;
            WriteMessage("\r\nSending Invoices");
            string queryString = "\r\nSELECT     \r\n[Transaction].TransactionID, \r\nWarehouse.WarehouseCode, \r\nEmployee.EmployeeCode,\r\n[Transaction].TransactionDate, \r\nCustomerOutlet.CustomerCode,\r\n[Transaction].NetTotal,\r\n[Transaction].GrossTotal,\r\n[Transaction].RemainingAmount,\r\n[Transaction].Discount,\r\n[Transaction].HelperID,\r\n[Transaction].TransactionTypeID,\r\n[Transaction].SalesMode,\r\n[Transaction].SourceTransactionID\r\n\r\n\r\nFROM [Transaction] INNER JOIN\r\nEmployee ON [Transaction].EmployeeID = Employee.EmployeeID  INNER JOIN\r\nWarehouse ON [Transaction].WarehouseID = Warehouse.WarehouseID INNER JOIN\r\nCustomerOutlet ON [Transaction].CustomerID = CustomerOutlet.CustomerID AND [Transaction].OutletID = CustomerOutlet.OutletID\r\nWHERE     ([Transaction].Synchronized = 0) AND  ([Transaction].Voided = 0) AND ([Transaction].TransactionTypeID = 1 or [Transaction].TransactionTypeID = 3)";
            if (!AllSalespersons)
            {
                queryString = queryString + " AND [Transaction].EmployeeID = " + Salesperson;
            }
            InCubeQuery query = new InCubeQuery(base.db_vms, queryString);
            this.err = query.Execute();
            this.err = query.FindFirst();
            while (this.err == InCubeErrors.Success)
            {
                try
                {
                    transaction = this.Conn.BeginTransaction();
                    this.err = query.GetField(0, ref field);
                    this.err = query.GetField(1, ref obj4);
                    this.err = query.GetField(2, ref obj6);
                    this.err = query.GetField(3, ref obj3);
                    this.err = query.GetField(4, ref obj5);
                    this.err = query.GetField(5, ref obj7);
                    this.err = query.GetField(6, ref obj8);
                    this.err = query.GetField(7, ref obj17);
                    this.err = query.GetField(8, ref obj16);
                    this.err = query.GetField(9, ref obj18);
                    this.err = query.GetField(10, ref obj19);
                    this.err = query.GetField(11, ref obj20);
                    this.err = query.GetField(12, ref obj21);
                    string str3 = string.Empty;
                    if (((obj18 != null) && (obj18 != DBNull.Value)) && (obj18.ToString() != string.Empty))
                    {
                        str3 = base.GetFieldValue("Employee", "Employeecode", "EmployeeID = '" + obj18 + "'", base.db_vms);
                    }
                    if (obj19.ToString() == "1")
                    {
                        str = "INV";
                    }
                    if (obj19.ToString() == "3")
                    {
                        str = "EXINV";
                    }
                    DateTime time = DateTime.Parse(obj3.ToString());
                    if (obj21 == null)
                    {
                        obj21 = string.Empty;
                    }
                    string cmdText = string.Concat(new object[] { 
                        "Insert into AWAL_INVAN_SALE_INV_HEAD \r\n(AISIH_INVAN_REF,AISIH_CUST_CODE,AISIH_SALE_PER_CODE,AISIH_DOC_DT,AISIH_COMP_ID,AISIH_ORN_REF,AISIH_LOCN_CODE,\r\n AISIH_CURR_CODE,AISIH_SHIP_MODE,AISIH_DEL_DATE,AISIH_SUB_TOTAL,AISIH_DISCOUNT,AISIH_TAX,AISIH_TOTAL,AISIH_REM_AMT,AISIH_PROCESSED,AISIH_TRF_DATE,AISIH_TYPE,AISIH_HELP_CODE,AISIH_INV_TYPE,AISIH_EXCH_REF) \r\n values \r\n('", field.ToString(), "','", obj5.ToString(), "','", obj6.ToString(), "','", time.ToString("dd/MMM/yyyy"), "','001','D','", obj4.ToString(), "',\r\n 'BD' ,'0','", time.ToString("dd/MMM/yyyy"), "',", obj8.ToString(), ",", obj16.ToString(), 
                        ",0,", obj7.ToString(), ", ", obj17.ToString(), " ,0,'", DateTime.Now.ToString("dd/MMM/yyyy"), "','", str, "','", str3, "','", obj20, "','", obj21, "' )"
                     });
                    WriteMessage(cmdText + "\r\n");
                    OleDbCommand command = new OleDbCommand(cmdText, this.Conn, transaction);
                    command.ExecuteNonQuery();
                    command.Dispose();
                    string str5 = "SELECT     \r\nTransactionDetail.TransactionID,\r\nTransactionDetail.BatchNo,\r\nTransactionDetail.Quantity,\r\nTransactionDetail.Price, \r\nTransactionDetail.ExpiryDate, \r\nTransactionDetail.Discount, \r\nTransactionDetail.Tax, \r\nItem.ItemCode, \r\nItemLanguage.Description AS ItemName, \r\nPackTypeLanguage.Description AS PackName, \r\nPack.Quantity AS PcsInCse, \r\nTransactionDetail.PackID,\r\nTransactionDetail.Quantity * TransactionDetail.Price + TransactionDetail.Tax - TransactionDetail.Discount AS Value\r\n\r\nFROM TransactionDetail INNER JOIN\r\nPack ON TransactionDetail.PackID = Pack.PackID INNER JOIN\r\nItem ON Pack.ItemID = Item.ItemID INNER JOIN\r\nItemLanguage ON Pack.ItemID = ItemLanguage.ItemID INNER JOIN\r\nPackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID\r\nWHERE (PackTypeLanguage.LanguageID = 1) AND (ItemLanguage.LanguageID = 1) AND (TransactionDetail.TransactionID = '" + field.ToString() + "')";
                    InCubeQuery query2 = new InCubeQuery(base.db_vms, str5);
                    this.err = query2.Execute();
                    this.err = query2.FindFirst();
                    if (this.err == InCubeErrors.Success)
                    {
                        goto Label_0649;
                    }
                    throw new Exception("No details found");
                Label_0471:
                    this.err = query2.GetField(7, ref obj10);
                    this.err = query2.GetField(2, ref obj12);
                    this.err = query2.GetField(12, ref obj11);
                    this.err = query2.GetField(3, ref obj13);
                    this.err = query2.GetField(6, ref obj15);
                    this.err = query2.GetField(7, ref obj10);
                    this.err = query2.GetField(9, ref obj9);
                    this.err = query2.GetField(5, ref obj16);
                    this.err = query2.GetField(1, ref obj14);
                    decimal num = 0M;
                    decimal num2 = 0M;
                    if (obj16.ToString() != "")
                    {
                        num = decimal.Parse(obj16.ToString());
                    }
                    if (obj13.ToString() != "")
                    {
                        num2 = decimal.Parse(obj13.ToString());
                    }
                    if (obj15.ToString() != "")
                    {
                        decimal.Parse(obj15.ToString());
                    }
                    OleDbCommand command2 = new OleDbCommand(string.Concat(new object[] { "Insert into AWAL_INVAN_SALE_INV_DETAIL \r\n(AISID_INVAN_REF,AISID_SKU_CODE,AISID_UOM_CODE,AISID_QTY,AISID_RATE,\r\n AISID_DISCOUNT,AISID_TAX,AISID_BATCH_NO) \r\nvalues ('", field.ToString(), "','", obj10.ToString(), "',N'", obj9.ToString(), "',", obj12.ToString(), ",", num2.ToString(), ",", num.ToString(), ",0,'", obj14, "')" }), this.Conn, transaction);
                    command2.ExecuteNonQuery();
                    command2.Dispose();
                    this.err = query2.FindNext();
                Label_0649:
                    if (this.err == InCubeErrors.Success)
                    {
                        goto Label_0471;
                    }
                    WriteMessage("\r\n" + field.ToString() + " - OK");
                    this.QueryBuilderObject.SetField("Synchronized", "1");
                    this.QueryBuilderObject.UpdateQueryString("[Transaction]", " TransactionID = '" + field.ToString() + "'", base.db_vms);
                    transaction.Commit();
                }
                catch (Exception exception)
                {
                    StreamWriter writer = new StreamWriter("errorInv.log", true);
                    writer.Write(exception.ToString());
                    writer.Close();
                    transaction.Rollback();
                    WriteMessage("\r\n" + field.ToString() + " - FAILED!");
                }
                this.err = query.FindNext();
            }
            this.Conn.Close();
        }

        public override void SendReciepts(bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate)
        {
            WriteMessage("\r\nSend Payment");
            object field = null;
            object obj3 = null;
            object obj4 = null;
            object obj5 = null;
            object obj6 = null;
            object obj7 = null;
            object obj8 = null;
            object obj9 = null;
            object obj10 = null;
            object obj11 = null;
            object obj12 = null;
            object obj13 = null;
            if (this.Conn.State == ConnectionState.Closed)
            {
                this.Conn.Open();
            }
            string queryString = "SELECT\r\n     \r\nCustomerPayment.CustomerPaymentID, \r\nCustomerOutlet.CustomerCode, \r\nEmployee.EmployeeCode, \r\nCustomerPayment.PaymentDate, \r\nCustomerPayment.AppliedAmount, \r\nCustomerPayment.VoucherNumber, \r\nCustomerPayment.VoucherDate, \r\nCustomerPayment.VoucherOwner, \r\nBank.Code, \r\nCustomerPayment.BranchID,\r\nCustomerPayment.TransactionID,\r\nCustomerPayment.PaymentTypeID\r\n\r\nFROM CustomerOutlet RIGHT OUTER JOIN\r\nCustomerPayment ON CustomerOutlet.OutletID = CustomerPayment.OutletID AND \r\nCustomerOutlet.CustomerID = CustomerPayment.CustomerID INNER JOIN\r\nEmployee ON CustomerPayment.EmployeeID = Employee.EmployeeID LEFT outer JOIN\r\nBank ON Bank.BankID = CustomerPayment.BankID\r\nWHERE (CustomerPayment.Synchronized = 0)\r\nAND (CustomerPayment.PaymentDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + "' \r\nAND  CustomerPayment.PaymentDate < '" + ToDate.Date.AddDays(1.0).ToString("yyyy/MM/dd") + "')";
            if (!AllSalespersons)
            {
                queryString = queryString + " AND CustomerPayment.EmployeeID = " + Salesperson;
            }
            WriteMessage(queryString + "\r\n");
            InCubeQuery query = new InCubeQuery(base.db_vms, queryString);
            query.Execute();
            this.err = query.FindFirst();
            while (this.err == InCubeErrors.Success)
            {
                string str2;
                this.err = query.GetField(0, ref field);
                this.err = query.GetField(1, ref obj3);
                this.err = query.GetField(2, ref obj4);
                this.err = query.GetField(3, ref obj5);
                this.err = query.GetField(4, ref obj6);
                this.err = query.GetField(5, ref obj7);
                this.err = query.GetField(6, ref obj8);
                this.err = query.GetField(7, ref obj9);
                this.err = query.GetField(8, ref obj10);
                this.err = query.GetField(9, ref obj11);
                this.err = query.GetField(10, ref obj12);
                this.err = query.GetField(11, ref obj13);
                if (obj13.ToString() == "1")
                {
                    str2 = string.Concat(new object[] { 
                        "Insert into AWAL_INVAN_COLLECTION \r\n(AIC_INVAN_REF,AIC_CUST_CODE,\r\n AIC_PAY_DATE,AIC_PAY_TYPE,AIC_AMOUNT,\r\n AIC_VCHR_NO,AIC_VCHR_OWNER,AIC_VCHR_DATE,AIC_INVOICE_ID,AIC_BANK_CODE,AIC_SALEMAN_CODE,AIC_CURR_CODE,AIC_SUB_ACNT_CODE,AIC_DIVN_CODE,AIC_DEPT_CODE,AIC_PROCESSED,AIC_TRF_DATE\r\n ) \r\nvalues \r\n('", field, "','", obj3, "','", DateTime.Parse(obj5.ToString()).ToString("dd/MMM/yyyy"), "',\r\n '", obj13, "',", obj6, ",'',\r\n '','','", obj12, "','','", obj4, "','BD','", obj3, 
                        "','003','ACC',0,'", DateTime.Now.ToString("dd/MMM/yyyy"), "')"
                     });
                }
                else
                {
                    str2 = string.Concat(new object[] { 
                        "Insert into AWAL_INVAN_COLLECTION \r\n(AIC_INVAN_REF,AIC_CUST_CODE,\r\n AIC_PAY_DATE,AIC_PAY_TYPE,AIC_AMOUNT,\r\n AIC_VCHR_NO,AIC_VCHR_OWNER,AIC_VCHR_DATE,AIC_INVOICE_ID,AIC_BANK_CODE,AIC_SALEMAN_CODE,AIC_CURR_CODE,AIC_SUB_ACNT_CODE,AIC_DIVN_CODE,AIC_DEPT_CODE,AIC_PROCESSED,AIC_TRF_DATE\r\n ) \r\nvalues \r\n('", field, "','", obj3, "','", DateTime.Parse(obj5.ToString()).ToString("dd/MMM/yyyy"), "',\r\n '", obj13, "',", obj6, ",'", obj7, "',\r\n '", obj9, "','", DateTime.Parse(obj8.ToString()).ToString("dd/MMM/yyyy"), 
                        "','", obj12, "','", obj10, "','", obj4, "','BD','", obj3, "','003','ACC',0,'", DateTime.Now.ToString("dd/MMM/yyyy"), "')"
                     });
                }
                WriteMessage(str2 + "\r\n");
                OleDbCommand command = new OleDbCommand(str2, this.Conn);
                command.ExecuteNonQuery();
                command.Dispose();
                this.err = query.FindNext();
            }
        }

        public override void SendReturn(bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate)
        {
            object field = "";
            object obj3 = "";
            object obj4 = "";
            object obj5 = "";
            object obj6 = "";
            object obj7 = "";
            object obj8 = "";
            object obj9 = "";
            object obj10 = "";
            object obj11 = "";
            object obj12 = "";
            object obj13 = "";
            object obj14 = "";
            object obj15 = "";
            object obj16 = "";
            object obj17 = "";
            object obj18 = "";
            object obj19 = "";
            object obj20 = "";
            string str = "";
            string str2 = "";
            object obj21 = "";
            if (this.Conn.State == ConnectionState.Closed)
            {
                this.Conn.Open();
            }
            OleDbTransaction transaction = null;
            WriteMessage("\r\nSending Returns");
            string queryString = "\r\nSELECT     \r\n[Transaction].TransactionID, \r\nWarehouse.WarehouseCode, \r\nEmployee.EmployeeCode,\r\n[Transaction].TransactionDate, \r\nCustomerOutlet.CustomerCode,\r\n[Transaction].NetTotal,\r\n[Transaction].GrossTotal,\r\n[Transaction].RemainingAmount,\r\n[Transaction].Discount,\r\n[Transaction].HelperID,\r\n[Transaction].TransactionTypeID,\r\n[Transaction].SourceTransactionID\r\n\r\nFROM [Transaction] INNER JOIN\r\nEmployee ON [Transaction].EmployeeID = Employee.EmployeeID  INNER JOIN\r\nWarehouse ON [Transaction].WarehouseID = Warehouse.WarehouseID INNER JOIN\r\nCustomerOutlet ON [Transaction].CustomerID = CustomerOutlet.CustomerID AND [Transaction].OutletID = CustomerOutlet.OutletID\r\nWHERE     ([Transaction].Synchronized = 0) AND  ([Transaction].Voided = 0) AND ([Transaction].TransactionTypeID = 2 OR [Transaction].TransactionTypeID = 4)";
            if (!AllSalespersons)
            {
                queryString = queryString + " AND [Transaction].EmployeeID = " + Salesperson;
            }
            InCubeQuery query = new InCubeQuery(base.db_vms, queryString);
            this.err = query.Execute();
            this.err = query.FindFirst();
            while (this.err == InCubeErrors.Success)
            {
                try
                {
                    transaction = this.Conn.BeginTransaction();
                    this.err = query.GetField(0, ref field);
                    this.err = query.GetField(1, ref obj4);
                    this.err = query.GetField(2, ref obj6);
                    this.err = query.GetField(3, ref obj3);
                    this.err = query.GetField(4, ref obj5);
                    this.err = query.GetField(5, ref obj7);
                    this.err = query.GetField(6, ref obj8);
                    this.err = query.GetField(7, ref obj17);
                    this.err = query.GetField(8, ref obj16);
                    this.err = query.GetField(9, ref obj18);
                    this.err = query.GetField(10, ref obj19);
                    this.err = query.GetField(11, ref obj21);
                    string str4 = string.Empty;
                    if (((obj18 != null) && (obj18 != DBNull.Value)) && (obj18.ToString() != string.Empty))
                    {
                        str4 = base.GetFieldValue("Employee", "Employeecode", "EmployeeID = '" + obj18 + "'", base.db_vms);
                    }
                    if (obj19.ToString() == "2")
                    {
                        str = "SRTN";
                    }
                    if (obj19.ToString() == "4")
                    {
                        str = "EXSRTN";
                    }
                    DateTime time = DateTime.Parse(obj3.ToString());
                    if (obj21 == null)
                    {
                        obj21 = string.Empty;
                    }
                    string cmdText = string.Concat(new object[] { 
                        "Insert into AWAL_INVAN_SALE_INV_HEAD \r\n(AISIH_INVAN_REF,AISIH_CUST_CODE,AISIH_SALE_PER_CODE,AISIH_DOC_DT,AISIH_COMP_ID,AISIH_ORN_REF,AISIH_LOCN_CODE,\r\n AISIH_CURR_CODE,AISIH_SHIP_MODE,AISIH_DEL_DATE,AISIH_SUB_TOTAL,AISIH_DISCOUNT,AISIH_TAX,AISIH_TOTAL,AISIH_REM_AMT,AISIH_PROCESSED,AISIH_TRF_DATE,AISIH_TYPE,AISIH_HELP_CODE,AISIH_EXCH_REF) \r\n values \r\n('", field.ToString(), "','", obj5.ToString(), "','", obj6.ToString(), "','", time.ToString("dd/MMM/yyyy"), "','001','D','", obj4.ToString(), "',\r\n 'BD' ,'0','", time.ToString("dd/MMM/yyyy"), "',", obj8.ToString(), ",", obj16.ToString(), 
                        ",0,", obj7.ToString(), ", ", obj17.ToString(), " ,0,'", DateTime.Now.ToString("dd/MMM/yyyy"), "','", str, "','", str4, "','", obj21, "' )"
                     });
                    WriteMessage(cmdText + "\r\n");
                    OleDbCommand command = new OleDbCommand(cmdText, this.Conn, transaction);
                    command.ExecuteNonQuery();
                    command.Dispose();
                    string str6 = "SELECT     \r\nTransactionDetail.TransactionID,\r\nTransactionDetail.BatchNo,\r\nTransactionDetail.Quantity,\r\nTransactionDetail.Price, \r\nTransactionDetail.ExpiryDate, \r\nTransactionDetail.Discount, \r\nTransactionDetail.Tax, \r\nItem.ItemCode, \r\nItemLanguage.Description AS ItemName, \r\nPackTypeLanguage.Description AS PackName, \r\nPack.Quantity AS PcsInCse, \r\nTransactionDetail.PackID,\r\nTransactionDetail.Quantity * TransactionDetail.Price + TransactionDetail.Tax - TransactionDetail.Discount AS Value,\r\nTransactionDetail.PackStatusID\r\n\r\nFROM TransactionDetail INNER JOIN\r\nPack ON TransactionDetail.PackID = Pack.PackID INNER JOIN\r\nItem ON Pack.ItemID = Item.ItemID INNER JOIN\r\nItemLanguage ON Pack.ItemID = ItemLanguage.ItemID INNER JOIN\r\nPackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID\r\nWHERE (PackTypeLanguage.LanguageID = 1) AND (ItemLanguage.LanguageID = 1) AND (TransactionDetail.TransactionID = '" + field.ToString() + "')";
                    InCubeQuery query2 = new InCubeQuery(base.db_vms, str6);
                    this.err = query2.Execute();
                    this.err = query2.FindFirst();
                    if (this.err == InCubeErrors.Success)
                    {
                        goto Label_069E;
                    }
                    throw new Exception("No details found");
                Label_0456:
                    this.err = query2.GetField(7, ref obj10);
                    this.err = query2.GetField(2, ref obj12);
                    this.err = query2.GetField(12, ref obj11);
                    this.err = query2.GetField(3, ref obj13);
                    this.err = query2.GetField(6, ref obj15);
                    this.err = query2.GetField(7, ref obj10);
                    this.err = query2.GetField(9, ref obj9);
                    this.err = query2.GetField(5, ref obj16);
                    this.err = query2.GetField(1, ref obj14);
                    this.err = query2.GetField(13, ref obj20);
                    decimal num = 0M;
                    decimal num2 = 0M;
                    if (obj16.ToString() != "")
                    {
                        num = decimal.Parse(obj16.ToString());
                    }
                    if (obj13.ToString() != "")
                    {
                        num2 = decimal.Parse(obj13.ToString());
                    }
                    if (obj15.ToString() != "")
                    {
                        decimal.Parse(obj15.ToString());
                    }
                    if (obj20.ToString() == "1")
                    {
                        str2 = "IDM";
                    }
                    if (obj20.ToString() == "2")
                    {
                        str2 = "IEX";
                    }
                    if (obj20.ToString() == "3")
                    {
                        str2 = "IGR";
                    }
                    OleDbCommand command2 = new OleDbCommand(string.Concat(new object[] { 
                        "Insert into AWAL_INVAN_SALE_INV_DETAIL \r\n(AISID_INVAN_REF,AISID_SKU_CODE,AISID_UOM_CODE,AISID_QTY,AISID_RATE,\r\n AISID_DISCOUNT,AISID_TAX,AISID_BATCH_NO,AISID_reason_code) \r\nvalues ('", field.ToString(), "','", obj10.ToString(), "',N'", obj9.ToString(), "',", obj12.ToString(), ",", num2.ToString(), ",", num.ToString(), ",0,'", obj14, "','", str2, 
                        "')"
                     }), this.Conn, transaction);
                    command2.ExecuteNonQuery();
                    command2.Dispose();
                    this.err = query2.FindNext();
                Label_069E:
                    if (this.err == InCubeErrors.Success)
                    {
                        goto Label_0456;
                    }
                    WriteMessage("\r\n" + field.ToString() + " - OK");
                    this.QueryBuilderObject.SetField("Synchronized", "1");
                    this.QueryBuilderObject.UpdateQueryString("[Transaction]", " TransactionID = '" + field.ToString() + "'", base.db_vms);
                    transaction.Commit();
                }
                catch (Exception exception)
                {
                    StreamWriter writer = new StreamWriter("errorInv.log", true);
                    writer.Write(exception.ToString());
                    writer.Close();
                    transaction.Rollback();
                    WriteMessage("\r\n" + field.ToString() + " - FAILED!");
                }
                this.err = query.FindNext();
            }
            this.Conn.Close();
        }

        public override void SendTransfers(bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate)
        {
            object field = "";
            object obj3 = "";
            object obj4 = "";
            object obj5 = "";
            object obj6 = "";
            object obj7 = "";
            object obj8 = "";
            object obj9 = "";
            object obj10 = "";
            if (this.Conn.State == ConnectionState.Closed)
            {
                this.Conn.Open();
            }
            OleDbTransaction transaction = null;
            WriteMessage("\r\nSending Load Requests");
            string queryString = "SELECT \r\nWarehouseTransaction.TransactionID,\r\nWarehouseTransaction.TransactionDate,\r\nWarehouse.Warehousecode AS ToWh,\r\nWarehouse_1.Warehousecode AS FromWh,\r\nWarehouseTransaction.LoadDate,\r\nWarehouseTransaction.TransactionTypeID\r\n\r\n\r\nFROM WarehouseTransaction INNER JOIN\r\nWarehouse ON WarehouseTransaction.WarehouseID = Warehouse.WarehouseID INNER JOIN\r\nWarehouse AS Warehouse_1 ON WarehouseTransaction.RefWarehouseID = Warehouse_1.WarehouseID\r\n\r\nWhere \r\n     WarehouseTransaction.Synchronized = 0 \r\nAND (WarehouseTransaction.TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + "' AND  WarehouseTransaction.TransactionDate <= '" + ToDate.Date.AddDays(1.0).ToString("yyyy/MM/dd") + "') \r\nAND  (WarehouseTransaction.TransactionTypeID = 1 OR WarehouseTransaction.TransactionTypeID = 2 ) AND (WarehouseTransaction.WarehouseTransactionStatusID = 2 OR WarehouseTransaction.WarehouseTransactionStatusID = 4)";
            if (!AllSalespersons)
            {
                queryString = queryString + " AND RequestedBy = " + Salesperson;
            }
            WriteMessage(queryString + "\r\n");
            InCubeQuery query = new InCubeQuery(base.db_vms, queryString);
            this.err = query.Execute();
            this.err = query.FindFirst();
            while (this.err == InCubeErrors.Success)
            {
                try
                {
                    transaction = this.Conn.BeginTransaction();
                    this.err = query.GetField(0, ref field);
                    this.err = query.GetField(1, ref obj3);
                    this.err = query.GetField(2, ref obj4);
                    this.err = query.GetField(3, ref obj5);
                    this.err = query.GetField(4, ref obj6);
                    this.err = query.GetField(5, ref obj10);
                    DateTime time = DateTime.Parse(obj3.ToString());
                    DateTime now = DateTime.Now;
                    if (DateTime.TryParse(obj6.ToString(), out now))
                    {
                        now = DateTime.Parse(obj6.ToString());
                    }
                    if (!(obj10.ToString() == "1"))
                    {
                        goto Label_042D;
                    }
                    string cmdText = string.Concat(new object[] { "Insert into AWAL_INVAN_REQ_HEAD \r\n(AIRH_INVAN_REF,AIRH_LOCN_CODE,AIRH_CHARGE_CODE,AIRH_DOC_DT,AIRH_DEL_REQD_DT,AIRH_COMP_ID,AIRH_ORN_REF,AIRH_PROCESSED,AIRH_TRF_DATE) \r\n values \r\n('", field, "','", obj4, "','", obj5, "','", time.ToString("dd/MMM/yyyy"), "','", now.ToString("dd/MMM/yyyy"), "','001','D',0,'", DateTime.Now.ToString("dd/MMM/yyyy"), "')" });
                    WriteMessage(cmdText + "\r\n");
                    OleDbCommand command = new OleDbCommand(cmdText, this.Conn, transaction);
                    command.ExecuteNonQuery();
                    command.Dispose();
                    string str3 = "SELECT WhTransDetail.Quantity, Item.ItemCode, PackTypeLanguage.Description\r\nFROM WhTransDetail INNER JOIN\r\nPack ON WhTransDetail.PackID = Pack.PackID INNER JOIN\r\nItem ON Pack.ItemID = Item.ItemID INNER JOIN\r\nPackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID\r\nWHERE (PackTypeLanguage.LanguageID = 1)\r\nAND\r\nWhTransDetail.TransactionID = '" + field + "'";
                    InCubeQuery query2 = new InCubeQuery(base.db_vms, str3);
                    this.err = query2.Execute();
                    this.err = query2.FindFirst();
                    if (this.err == InCubeErrors.Success)
                    {
                        goto Label_03C7;
                    }
                    throw new Exception("No details found");
                Label_0314:
                    this.err = query2.GetField(0, ref obj9);
                    this.err = query2.GetField(1, ref obj8);
                    this.err = query2.GetField(2, ref obj7);
                    OleDbCommand command2 = new OleDbCommand(string.Concat(new object[] { "Insert into AWAL_INVAN_REQ_DETAIL\r\n(AIRD_INVAN_REF,AIRD_SKU_CODE,AIRD_UOM_CODE,AIRD_QTY) \r\n values \r\n('", field, "','", obj8, "','", obj7, "',", obj9, ")" }), this.Conn, transaction);
                    command2.ExecuteNonQuery();
                    command2.Dispose();
                    this.err = query2.FindNext();
                Label_03C7:
                    if (this.err == InCubeErrors.Success)
                    {
                        goto Label_0314;
                    }
                    WriteMessage("\r\n" + field.ToString() + " - OK");
                    this.err = new InCubeQuery(base.db_vms, "Update WarehouseTransaction SET Synchronized = 1 and WarehouseTransaction.WarehouseTransactionStatusID = 4 where TransactionID = '" + field + "'").Execute();
                    transaction.Commit();
                    goto Label_06A2;
                Label_042D:;
                    string str4 = string.Concat(new object[] { "Insert into AWAL_INVAN_LTO_HEAD \r\n(AILOH_INVAN_REF,AILOH_FROM_LOCN_CODE,AILOH_TO_LOCN_CODE,AILOH_DOC_DT,AILOH_COMP_ID,AILOH_ORN_REF,AILOH_PROCESSED,AILOH_TRF_DATE) \r\n values \r\n('", field, "','", obj4, "','", obj5, "','", time.ToString("dd/MMM/yyyy"), "','001','D',0,'", DateTime.Now.ToString("dd/MMM/yyyy"), "')" });
                    WriteMessage(str4 + "\r\n");
                    OleDbCommand command3 = new OleDbCommand(str4, this.Conn, transaction);
                    command3.ExecuteNonQuery();
                    command3.Dispose();
                    string str5 = "SELECT WhTransDetail.Quantity, Item.ItemCode, PackTypeLanguage.Description\r\nFROM WhTransDetail INNER JOIN\r\nPack ON WhTransDetail.PackID = Pack.PackID INNER JOIN\r\nItem ON Pack.ItemID = Item.ItemID INNER JOIN\r\nPackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID\r\nWHERE (PackTypeLanguage.LanguageID = 1)\r\nAND\r\nWhTransDetail.TransactionID = '" + field + "'";
                    InCubeQuery query4 = new InCubeQuery(base.db_vms, str5);
                    this.err = query4.Execute();
                    this.err = query4.FindFirst();
                    if (this.err == InCubeErrors.Success)
                    {
                        goto Label_05ED;
                    }
                    throw new Exception("No details found");
                Label_053A:
                    this.err = query4.GetField(0, ref obj9);
                    this.err = query4.GetField(1, ref obj8);
                    this.err = query4.GetField(2, ref obj7);
                    OleDbCommand command4 = new OleDbCommand(string.Concat(new object[] { "Insert into AWAL_INVAN_LTO_DETAIL\r\n(AILOD_INVAN_REF,AILOD_SKU_CODE,AILOD_UOM_CODE,AILOD_QTY) \r\n values \r\n('", field, "','", obj8, "','", obj7, "',", obj9, ")" }), this.Conn, transaction);
                    command4.ExecuteNonQuery();
                    command4.Dispose();
                    this.err = query4.FindNext();
                Label_05ED:
                    if (this.err == InCubeErrors.Success)
                    {
                        goto Label_053A;
                    }
                    WriteMessage("\r\n" + field.ToString() + " - OK");
                    this.err = new InCubeQuery(base.db_vms, "Update WarehouseTransaction SET Synchronized = 1 where TransactionID = '" + field + "'").Execute();
                    transaction.Commit();
                }
                catch (Exception exception)
                {
                    StreamWriter writer = new StreamWriter("errorTRF.log", true);
                    writer.Write(exception.ToString());
                    writer.Close();
                    transaction.Rollback();
                    WriteMessage("\r\n" + field.ToString() + " - FAILED!");
                }
            Label_06A2:
                this.err = query.FindNext();
            }
            this.Conn.Close();
        }

        private void UpdateBanks()
        {
            new object();
            string cmdText = "SELECT BankCode,Description,Branchcode,BranchDescription FROM Bank";
            DataTable dataTable = new DataTable();
            OleDbCommand selectCommand = new OleDbCommand(cmdText, this.Conn);
            OleDbDataAdapter adapter = new OleDbDataAdapter(selectCommand);
            adapter.Fill(dataTable);
            adapter.Dispose();
            ClearProgress();
            SetProgressMax(dataTable.Rows.Count);
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                string str2 = dataTable.Rows[i][0].ToString();
                string str3 = dataTable.Rows[i][1].ToString();
                dataTable.Rows[i][2].ToString();
                dataTable.Rows[i][3].ToString();
                if (base.GetFieldValue("Bank", "BankID", "Code = '" + str2 + "'", base.db_vms) == string.Empty)
                {
                    string fieldValue = base.GetFieldValue("Bank", "isnull(MAX(BankID),0) + 1", base.db_vms);
                    this.QueryBuilderObject.SetField("BankID", fieldValue);
                    this.QueryBuilderObject.SetField("Code", "'" + str2 + "'");
                    this.QueryBuilderObject.InsertQueryString("Bank", base.db_vms);
                    this.QueryBuilderObject.SetField("BankID", fieldValue);
                    this.QueryBuilderObject.SetField("LanguageID", "1");
                    this.QueryBuilderObject.SetField("Description", "'" + str3 + "'");
                    this.QueryBuilderObject.InsertQueryString("BankLanguage", base.db_vms);
                    this.QueryBuilderObject.SetField("BankID", fieldValue);
                    this.QueryBuilderObject.SetField("LanguageID", "2");
                    this.QueryBuilderObject.SetField("Description", "'" + str3 + "'");
                    this.QueryBuilderObject.InsertQueryString("BankLanguage", base.db_vms);
                    this.QueryBuilderObject.SetField("BankID", fieldValue);
                    this.QueryBuilderObject.SetField("BranchID", fieldValue);
                    this.QueryBuilderObject.InsertQueryString("BankBranch", base.db_vms);
                    this.QueryBuilderObject.SetField("BankID", fieldValue);
                    this.QueryBuilderObject.SetField("BranchID", fieldValue);
                    this.QueryBuilderObject.SetField("LanguageID", "1");
                    this.QueryBuilderObject.SetField("Description", "'" + str3 + "'");
                    this.QueryBuilderObject.InsertQueryString("BankBranchLanguage", base.db_vms);
                    this.QueryBuilderObject.SetField("BankID", fieldValue);
                    this.QueryBuilderObject.SetField("BranchID", fieldValue);
                    this.QueryBuilderObject.SetField("LanguageID", "2");
                    this.QueryBuilderObject.SetField("Description", "'" + str3 + "'");
                    this.QueryBuilderObject.InsertQueryString("BankBranchLanguage", base.db_vms);
                }
            }
            dataTable.Dispose();
        }

        public override void UpdateCustomer(int EmployeeID)
        {
            try
            {
                int num = 0;
                int num2 = 0;
                int num3 = 0;
                new object();
                string cmdText = "SELECT  \r\nCustomerCode,CustomerBarcode,ArabicDescription,EnglishDescription,Phone,Fax,Email,MainAddressEnglish,MainAddressArabic,Taxable,Groups,IsCredit,Creditlimit,Balance,PaymentTerms,OnHold,MasterCustomerCode,isKeyAccount\r\nFROM Customer";
                this.DefaultOrganization();
                DataTable dataTable = new DataTable();
                OleDbCommand selectCommand = new OleDbCommand(cmdText, this.Conn);
                OleDbDataAdapter adapter = new OleDbDataAdapter(selectCommand);
                adapter.Fill(dataTable);
                adapter.Dispose();
                ClearProgress();
                SetProgressMax(dataTable.Rows.Count);
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    string str13;
                    ReportProgress("Updating customers ");
                    string customerCode = dataTable.Rows[i][0].ToString();
                    string customerBarCode = dataTable.Rows[i][1].ToString();
                    string customerDescriptionArabic = dataTable.Rows[i][2].ToString();
                    string customerDescriptionEnglish = dataTable.Rows[i][3].ToString();
                    string phonenumber = dataTable.Rows[i][4].ToString();
                    string faxnumber = dataTable.Rows[i][5].ToString();
                    string email = dataTable.Rows[i][6].ToString();
                    string customerAddressEnglish = dataTable.Rows[i][7].ToString();
                    string customerAddressArabic = dataTable.Rows[i][8].ToString();
                    string taxable = dataTable.Rows[i][9].ToString();
                    string customerGroupDescription = dataTable.Rows[i][10].ToString();
                    if (dataTable.Rows[i][11].ToString().Equals("0"))
                    {
                        str13 = "1";
                    }
                    else
                    {
                        str13 = "2";
                    }
                    string fieldValue = dataTable.Rows[i][12].ToString();
                    string str15 = dataTable.Rows[i][13].ToString();
                    string paymentterms = dataTable.Rows[i][14].ToString();
                    string onHold = dataTable.Rows[i][15].ToString();
                    string headOfficeCode = dataTable.Rows[i][0x10].ToString();
                    string isKeyAccount = dataTable.Rows[i][0x11].ToString();
                    if (customerCode != string.Empty)
                    {
                        if (isKeyAccount == string.Empty)
                        {
                            isKeyAccount = "0";
                        }
                        string str20 = "0";
                        if (headOfficeCode == string.Empty)
                        {
                            str20 = base.GetFieldValue("Customer", "CustomerID", "CustomerCode = '" + customerCode + "'", base.db_vms);
                            if (str20 == string.Empty)
                            {
                                str20 = base.GetFieldValue("Customer", "isnull(MAX(CustomerID),0) + 1", base.db_vms);
                            }
                            if (base.GetFieldValue("Customer", "CustomerID", "CustomerID = " + str20, base.db_vms) != string.Empty)
                            {
                                num++;
                                this.QueryBuilderObject.SetField("CustomerCode", "'" + customerCode + "'");
                                this.QueryBuilderObject.SetField("Phone", "'" + phonenumber + "'");
                                this.QueryBuilderObject.SetField("Fax", "'" + faxnumber + "'");
                                this.QueryBuilderObject.SetField("OnHold", num3.ToString());
                                this.QueryBuilderObject.SetField("UpdatedBy", this.UserID.ToString());
                                this.QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                                this.QueryBuilderObject.UpdateQueryString("Customer", " CustomerID = " + str20, base.db_vms);
                            }
                            else
                            {
                                num2++;
                                this.QueryBuilderObject.SetField("CustomerID", str20.ToString());
                                this.QueryBuilderObject.SetField("Phone", "'" + phonenumber + "'");
                                this.QueryBuilderObject.SetField("Fax", "'" + faxnumber + "'");
                                this.QueryBuilderObject.SetField("Email", "'" + email + "'");
                                this.QueryBuilderObject.SetField("CustomerCode", "'" + customerCode + "'");
                                this.QueryBuilderObject.SetField("OnHold", num3.ToString());
                                this.QueryBuilderObject.SetField("StreetID", "0");
                                this.QueryBuilderObject.SetField("StreetAddress", "0");
                                this.QueryBuilderObject.SetField("Inactive", "0");
                                this.QueryBuilderObject.SetField("New", "0");
                                this.QueryBuilderObject.SetField("CreatedBy", this.UserID.ToString());
                                this.QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                                this.QueryBuilderObject.SetField("UpdatedBy", this.UserID.ToString());
                                this.QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                                this.QueryBuilderObject.InsertQueryString("Customer", base.db_vms);
                            }
                            if (base.GetFieldValue("CustomerLanguage", "CustomerID", "CustomerID = " + str20 + " AND LanguageID = 1", base.db_vms) != string.Empty)
                            {
                                this.QueryBuilderObject.SetField("Description", "'" + customerDescriptionEnglish + "'");
                                this.QueryBuilderObject.SetField("Address", "'" + customerAddressEnglish + "'");
                                this.QueryBuilderObject.UpdateQueryString("CustomerLanguage", "  CustomerID = " + str20 + " AND LanguageID = 1", base.db_vms);
                            }
                            else
                            {
                                this.QueryBuilderObject.SetField("CustomerID", str20.ToString());
                                this.QueryBuilderObject.SetField("LanguageID", "1");
                                this.QueryBuilderObject.SetField("Description", "'" + customerDescriptionEnglish + "'");
                                this.QueryBuilderObject.SetField("Address", "'" + customerAddressEnglish + "'");
                                this.QueryBuilderObject.InsertQueryString("CustomerLanguage", base.db_vms);
                            }
                            if (base.GetFieldValue("CustomerLanguage", "CustomerID", "CustomerID = " + str20 + " AND LanguageID = 2", base.db_vms) != string.Empty)
                            {
                                this.QueryBuilderObject.SetField("Description", "N'" + customerDescriptionArabic + "'");
                                this.QueryBuilderObject.SetField("Address", "N'" + customerAddressArabic + "'");
                                this.QueryBuilderObject.UpdateQueryString("CustomerLanguage", "  CustomerID = " + str20 + " AND LanguageID = 2", base.db_vms);
                            }
                            else
                            {
                                this.QueryBuilderObject.SetField("CustomerID", str20.ToString());
                                this.QueryBuilderObject.SetField("LanguageID", "2");
                                this.QueryBuilderObject.SetField("Description", "N'" + customerDescriptionArabic + "'");
                                this.QueryBuilderObject.SetField("Address", "N'" + customerAddressArabic + "'");
                                this.QueryBuilderObject.InsertQueryString("CustomerLanguage", base.db_vms);
                            }
                            int num5 = 1;
                            if (base.GetFieldValue("AccountCust", "AccountID", "CustomerID = " + str20, base.db_vms) != string.Empty)
                            {
                                num5 = int.Parse(base.GetFieldValue("AccountCust", "AccountID", "CustomerID = " + str20, base.db_vms));
                                this.QueryBuilderObject.SetField("CreditLimit", fieldValue);
                                this.QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + num5.ToString(), base.db_vms);
                            }
                            else
                            {
                                num5 = int.Parse(base.GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", base.db_vms));
                                this.QueryBuilderObject.SetField("AccountID", num5.ToString());
                                this.QueryBuilderObject.SetField("AccountTypeID", "1");
                                this.QueryBuilderObject.SetField("CreditLimit", fieldValue);
                                this.QueryBuilderObject.SetField("Balance", str15);
                                this.QueryBuilderObject.SetField("GL", "0");
                                this.QueryBuilderObject.SetField("OrganizationID", this.OrganizationID);
                                this.QueryBuilderObject.SetField("CurrencyID", "1");
                                this.QueryBuilderObject.InsertQueryString("Account", base.db_vms);
                                this.QueryBuilderObject.SetField("CustomerID", str20.ToString());
                                this.QueryBuilderObject.SetField("AccountID", num5.ToString());
                                this.QueryBuilderObject.InsertQueryString("AccountCust", base.db_vms);
                                this.QueryBuilderObject.SetField("AccountID", num5.ToString());
                                this.QueryBuilderObject.SetField("LanguageID", "1");
                                this.QueryBuilderObject.SetField("Description", "'" + customerDescriptionEnglish.Trim() + " Account'");
                                this.QueryBuilderObject.InsertQueryString("AccountLanguage", base.db_vms);
                                this.QueryBuilderObject.SetField("AccountID", num5.ToString());
                                this.QueryBuilderObject.SetField("LanguageID", "2");
                                this.QueryBuilderObject.SetField("Description", "N'" + customerDescriptionArabic.Trim() + " Account'");
                                this.QueryBuilderObject.InsertQueryString("AccountLanguage", base.db_vms);
                            }
                            this.CreateCustomerOutlet(customerCode, customerGroupDescription, str13, paymentterms, customerDescriptionEnglish, customerAddressEnglish, customerDescriptionArabic, customerAddressArabic, phonenumber, faxnumber, onHold, taxable, customerCode, fieldValue, str15, customerBarCode, email, isKeyAccount);
                        }
                        else
                        {
                            this.CreateCustomerOutlet(customerCode, customerGroupDescription, str13, paymentterms, customerDescriptionEnglish, customerAddressEnglish, customerDescriptionArabic, customerAddressArabic, phonenumber, faxnumber, onHold, taxable, headOfficeCode, fieldValue, str15, customerBarCode, email, isKeyAccount);
                        }
                    }
                }
                dataTable.Dispose();
                WriteMessage("\r\n");
                WriteMessage(string.Concat(new object[] { "<<< CUSTOMERS >>> Total Updated = ", num, " , Total Inserted = ", num2 }));
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
            finally
            {
                this.Conn.Close();
            }
        }

        public override void UpdateItem(int EmployeeID)
        {
            try
            {
                int num = 0;
                int num2 = 0;
                new object();
                string cmdText = "SELECT  \r\nItemCode,ItemdescriptionEnglish,ItemdescriptionArabic,\r\nItemdivision,Divisiondescription,Itemcategory,Categorydescription,\r\nBrand,Origin,Pack,Packquantity,Packbarcode\r\nFROM Items";
                this.DefaultOrganization();
                DataTable dataTable = new DataTable();
                OleDbCommand selectCommand = new OleDbCommand(cmdText, this.Conn);
                OleDbDataAdapter adapter = new OleDbDataAdapter(selectCommand);
                adapter.Fill(dataTable);
                adapter.Dispose();
                ClearProgress();
                SetProgressMax(dataTable.Rows.Count);
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    ReportProgress("Updating Items ");
                    string str2 = dataTable.Rows[i][0].ToString();
                    string str3 = dataTable.Rows[i][1].ToString();
                    string str4 = dataTable.Rows[i][2].ToString();
                    string str5 = dataTable.Rows[i][3].ToString();
                    string str6 = dataTable.Rows[i][4].ToString();
                    string str7 = dataTable.Rows[i][5].ToString();
                    string str8 = dataTable.Rows[i][6].ToString();
                    dataTable.Rows[i][7].ToString();
                    dataTable.Rows[i][8].ToString();
                    string str9 = dataTable.Rows[i][9].ToString();
                    string fieldValue = dataTable.Rows[i][10].ToString();
                    string str11 = dataTable.Rows[i][11].ToString();
                    string str12 = base.GetFieldValue("Division", "DivisionID", "DivisionCode = '" + str5 + "'", base.db_vms);
                    if (str12 == string.Empty)
                    {
                        str12 = base.GetFieldValue("Division", "isnull(MAX(DivisionID),0) + 1", base.db_vms);
                        this.QueryBuilderObject.SetField("DivisionID", str12);
                        this.QueryBuilderObject.SetField("DivisionCode", "'" + str5 + "'");
                        this.QueryBuilderObject.SetField("OrganizationID", this.OrganizationID);
                        this.QueryBuilderObject.SetField("CreatedBy", this.UserID.ToString());
                        this.QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                        this.QueryBuilderObject.SetField("UpdatedBy", this.UserID.ToString());
                        this.QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                        this.QueryBuilderObject.InsertQueryString("Division", base.db_vms);
                        this.QueryBuilderObject.SetField("DivisionID", str12);
                        this.QueryBuilderObject.SetField("LanguageID", "1");
                        this.QueryBuilderObject.SetField("Description", "'" + str6 + "'");
                        this.QueryBuilderObject.InsertQueryString("DivisionLanguage", base.db_vms);
                        this.QueryBuilderObject.SetField("DivisionID", str12);
                        this.QueryBuilderObject.SetField("LanguageID", "2");
                        this.QueryBuilderObject.SetField("Description", "'" + str6 + "'");
                        this.QueryBuilderObject.InsertQueryString("DivisionLanguage", base.db_vms);
                    }
                    string str13 = base.GetFieldValue("ItemCategory", "ItemCategoryID", "ItemCategoryCode = '" + str7 + "'", base.db_vms);
                    if (str13 == string.Empty)
                    {
                        str13 = base.GetFieldValue("ItemCategory", "isnull(MAX(ItemCategoryID),0) + 1", base.db_vms);
                        this.QueryBuilderObject.SetField("ItemCategoryID", str13);
                        this.QueryBuilderObject.SetField("ItemCategoryCode", "'" + str7 + "'");
                        this.QueryBuilderObject.SetField("DivisionID", str12.ToString());
                        this.QueryBuilderObject.SetField("CreatedBy", this.UserID.ToString());
                        this.QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                        this.QueryBuilderObject.SetField("UpdatedBy", this.UserID.ToString());
                        this.QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                        this.QueryBuilderObject.InsertQueryString("ItemCategory", base.db_vms);
                        this.QueryBuilderObject.SetField("ItemCategoryID", str13);
                        this.QueryBuilderObject.SetField("LanguageID", "1");
                        this.QueryBuilderObject.SetField("Description", "'" + str8 + "'");
                        this.QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", base.db_vms);
                        this.QueryBuilderObject.SetField("ItemCategoryID", str13);
                        this.QueryBuilderObject.SetField("LanguageID", "2");
                        this.QueryBuilderObject.SetField("Description", "'" + str8 + "'");
                        this.QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", base.db_vms);
                    }
                    else
                    {
                        base.GetFieldValue("ItemCategory", "ItemCategoryID", "ItemCategoryCode = '" + str7 + "' AND DivisionID =" + str12.ToString(), base.db_vms);
                        if (str13 == string.Empty)
                        {
                            WriteMessage("\r\n");
                            WriteMessage(" Item Category " + str8 + " is defined twice , the duplicated division : " + str6);
                        }
                    }
                    string str14 = base.GetFieldValue("PackTypeLanguage", "PackTypeID", " Description = '" + str9 + "' AND LanguageID = 1", base.db_vms);
                    if (str14 == string.Empty)
                    {
                        str14 = base.GetFieldValue("PackType", "isnull(MAX(PackTypeID),0) + 1", base.db_vms);
                        this.QueryBuilderObject.SetField("PackTypeID", str14);
                        this.QueryBuilderObject.InsertQueryString("PackType", base.db_vms);
                        this.QueryBuilderObject.SetField("PackTypeID", str14);
                        this.QueryBuilderObject.SetField("LanguageID", "1");
                        this.QueryBuilderObject.SetField("Description", "'" + str9 + "'");
                        this.QueryBuilderObject.InsertQueryString("PackTypeLanguage", base.db_vms);
                        this.QueryBuilderObject.SetField("PackTypeID", str14);
                        this.QueryBuilderObject.SetField("LanguageID", "2");
                        this.QueryBuilderObject.SetField("Description", "N'" + str9 + "'");
                        this.QueryBuilderObject.InsertQueryString("PackTypeLanguage", base.db_vms);
                    }
                    string str15 = "";
                    str15 = base.GetFieldValue("Item", "ItemID", "ItemCode='" + str2 + "'", base.db_vms);
                    if (str15 == string.Empty)
                    {
                        str15 = base.GetFieldValue("Item", "isnull(MAX(ItemID),0) + 1", base.db_vms);
                    }
                    if (base.GetFieldValue("Item", "ItemID", "ItemID = " + str15, base.db_vms) != string.Empty)
                    {
                        num++;
                        this.QueryBuilderObject.SetField("ItemCode", "'" + str2 + "'");
                        this.QueryBuilderObject.SetField("InActive", "0");
                        this.QueryBuilderObject.SetField("UpdatedBy", this.UserID.ToString());
                        this.QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                        this.QueryBuilderObject.UpdateQueryString("Item", " ItemID = " + str15, base.db_vms);
                    }
                    else
                    {
                        num2++;
                        this.QueryBuilderObject.SetField("ItemID", str15);
                        this.QueryBuilderObject.SetField("ItemCategoryID", str13.ToString());
                        this.QueryBuilderObject.SetField("ItemCode", "'" + str2 + "'");
                        this.QueryBuilderObject.SetField("InActive", "0");
                        this.QueryBuilderObject.SetField("CreatedBy", this.UserID.ToString());
                        this.QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                        this.QueryBuilderObject.SetField("UpdatedBy", this.UserID.ToString());
                        this.QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                        this.QueryBuilderObject.SetField("ItemType", "1");
                        this.QueryBuilderObject.InsertQueryString("Item", base.db_vms);
                    }
                    if (base.GetFieldValue("ItemLanguage", "ItemID", "ItemID = " + str15 + " AND LanguageID = 1", base.db_vms) != string.Empty)
                    {
                        this.QueryBuilderObject.SetField("Description", "'" + str3 + "'");
                        this.QueryBuilderObject.UpdateQueryString("ItemLanguage", " ItemID =" + str15 + " AND LanguageID = 1", base.db_vms);
                    }
                    else
                    {
                        this.QueryBuilderObject.SetField("ItemID", str15.ToString());
                        this.QueryBuilderObject.SetField("LanguageID", "1");
                        this.QueryBuilderObject.SetField("Description", "'" + str3 + "'");
                        this.QueryBuilderObject.InsertQueryString("ItemLanguage", base.db_vms);
                    }
                    if (str4 != string.Empty)
                    {
                        if (base.GetFieldValue("ItemLanguage", "ItemID", "ItemID = " + str15 + " AND LanguageID = 2", base.db_vms) != string.Empty)
                        {
                            this.QueryBuilderObject.SetField("Description", "N'" + str4 + "'");
                            this.QueryBuilderObject.UpdateQueryString("ItemLanguage", " ItemID =" + str15 + " AND LanguageID = 2", base.db_vms);
                        }
                        else
                        {
                            this.QueryBuilderObject.SetField("ItemID", str15.ToString());
                            this.QueryBuilderObject.SetField("LanguageID", "2");
                            this.QueryBuilderObject.SetField("Description", "N'" + str4 + "'");
                            this.QueryBuilderObject.InsertQueryString("ItemLanguage", base.db_vms);
                        }
                    }
                    int num4 = 1;
                    if (base.GetFieldValue("Pack", "PackID", "ItemID = " + str15 + " and PackTypeID = " + str14, base.db_vms) != string.Empty)
                    {
                        num4 = int.Parse(base.GetFieldValue("Pack", "PackID", "ItemID = " + str15 + " and PackTypeID = " + str14, base.db_vms));
                        this.QueryBuilderObject.SetField("Barcode", "'" + str11 + "'");
                        this.QueryBuilderObject.UpdateQueryString("Pack", "PackID = " + num4, base.db_vms);
                    }
                    else
                    {
                        this.QueryBuilderObject.SetField("PackID", int.Parse(base.GetFieldValue("Pack", "ISNULL(MAX(PackID),0) + 1", base.db_vms)).ToString());
                        this.QueryBuilderObject.SetField("Barcode", "'" + str11 + "'");
                        this.QueryBuilderObject.SetField("ItemID", str15.ToString());
                        this.QueryBuilderObject.SetField("PackTypeID", str14);
                        this.QueryBuilderObject.SetField("Quantity", fieldValue);
                        this.QueryBuilderObject.SetField("EquivalencyFactor", "0");
                        this.QueryBuilderObject.SetField("HasSerialNumber", "0");
                        this.QueryBuilderObject.InsertQueryString("Pack", base.db_vms);
                    }
                }
                dataTable.Dispose();
                WriteMessage("\r\n");
                WriteMessage(string.Concat(new object[] { "<<< ITEMS >>> Total Updated = ", num, " , Total Inserted = ", num2 }));
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }

        public override void UpdatePrice(int EmployeeID)
        {
            int tOTALUPDATED = 0;
            new object();
            this.UpdatePriceList(ref tOTALUPDATED);
            WriteMessage("\r\n");
            WriteMessage("<<< PRICE >>> Total Updated = " + tOTALUPDATED);
        }

        private void UpdatePriceList(ref int TOTALUPDATED)
        {
            object field = new object();
            string cmdText = "SELECT \r\nPricelistCode,PriceListDescription,ItemCode,UOM,Price,Tax,IsdefaultPricelist,CustomergroupDescription,customerCode \r\nFROM PriceList";
            DataTable dataTable = new DataTable();
            OleDbCommand selectCommand = new OleDbCommand(cmdText, this.Conn);
            OleDbDataAdapter adapter = new OleDbDataAdapter(selectCommand);
            adapter.Fill(dataTable);
            adapter.Dispose();
            ClearProgress();
            SetProgressMax(dataTable.Rows.Count);
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                ReportProgress("Updating Price lists ");
                TOTALUPDATED++;
                string str2 = dataTable.Rows[i][0].ToString();
                string str3 = dataTable.Rows[i][1].ToString();
                string str4 = dataTable.Rows[i][2].ToString();
                string str5 = dataTable.Rows[i][3].ToString();
                string s = dataTable.Rows[i][4].ToString();
                string fieldValue = dataTable.Rows[i][5].ToString();
                string str8 = dataTable.Rows[i][6].ToString();
                string str9 = dataTable.Rows[i][7].ToString();
                string str10 = dataTable.Rows[i][8].ToString();
                if ((((str2 != string.Empty) && (str4 != string.Empty)) && ((str5 != string.Empty) && (s != string.Empty))) && (str8 != string.Empty))
                {
                    s = Math.Round(decimal.Parse(s), 3).ToString();
                    string str11 = "1";
                    this.err = base.ExistObject("PriceListLanguage", "Description", " Description = '" + str3 + "'", base.db_vms);
                    if (this.err == InCubeErrors.Success)
                    {
                        str11 = base.GetFieldValue("PriceListLanguage", "PriceListID", " Description = '" + str3 + "'", base.db_vms);
                        this.QueryBuilderObject.SetField("PriceListCode", "'" + str2 + "'");
                        this.QueryBuilderObject.UpdateQueryString("PriceList", " PriceListID = " + str11, base.db_vms);
                    }
                    else
                    {
                        str11 = base.GetFieldValue("PriceList", "ISNULL(MAX(PriceListID),0) + 1", base.db_vms);
                        this.QueryBuilderObject.SetField("PriceListID", str11);
                        this.QueryBuilderObject.SetField("PriceListCode", "'" + str2 + "'");
                        this.QueryBuilderObject.SetField("StartDate", "'" + DateTime.Now.Date.ToString(this.DateFormat) + "'");
                        this.QueryBuilderObject.SetField("EndDate", "'" + DateTime.Now.Date.AddYears(10).ToString(this.DateFormat) + "'");
                        this.QueryBuilderObject.SetField("Priority", "1");
                        this.QueryBuilderObject.InsertQueryString("PriceList", base.db_vms);
                        this.QueryBuilderObject.SetField("PriceListID", str11);
                        this.QueryBuilderObject.SetField("LanguageID", "1");
                        this.QueryBuilderObject.SetField("Description", "'" + str3 + "'");
                        this.QueryBuilderObject.InsertQueryString("PriceListLanguage", base.db_vms);
                        this.QueryBuilderObject.SetField("PriceListID", str11);
                        this.QueryBuilderObject.SetField("LanguageID", "2");
                        this.QueryBuilderObject.SetField("Description", "'" + str3 + "'");
                        this.QueryBuilderObject.InsertQueryString("PriceListLanguage", base.db_vms);
                    }
                    if (str8 == "1")
                    {
                        this.QueryBuilderObject.SetField("KeyValue", str11);
                        this.QueryBuilderObject.UpdateQueryString("Configuration", "KeyName = 'DefaultPriceListID' AND EmployeeID = -1", base.db_vms);
                    }
                    this.err = base.ExistObject("PriceQuantityRange", "PriceQuantityRangeID", " PriceQuantityRangeID = 1", base.db_vms);
                    if (this.err != InCubeErrors.Success)
                    {
                        this.QueryBuilderObject.SetField("PriceQuantityRangeID", "1");
                        this.QueryBuilderObject.SetField("RangeStart", "1");
                        this.QueryBuilderObject.SetField("RangeEnd", "9999999");
                        this.QueryBuilderObject.InsertQueryString("PriceQuantityRange", base.db_vms);
                    }
                    string str12 = base.GetFieldValue("Item", "ItemID", "ItemCode = '" + str4 + "'", base.db_vms);
                    if (str12 != string.Empty)
                    {
                        string str14 = base.GetFieldValue("Pack", "PackID", "ItemID = " + str12 + " AND PackTypeID = " + base.GetFieldValue("PackTypeLanguage", "PackTypeID", " LanguageID = 1 and Description = '" + str5 + "'", base.db_vms), base.db_vms);
                        int num2 = 1;
                        string str15 = base.GetFieldValue("PriceDefinition", "Price", "PackID = " + str14 + " AND PriceListID = " + str11, base.db_vms);
                        if (str15.Equals(string.Empty))
                        {
                            num2 = int.Parse(base.GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", base.db_vms));
                            this.QueryBuilderObject.SetField("PriceDefinitionID", num2.ToString());
                            this.QueryBuilderObject.SetField("QuantityRangeID", "1");
                            this.QueryBuilderObject.SetField("PackID", str14);
                            this.QueryBuilderObject.SetField("CurrencyID", "1");
                            this.QueryBuilderObject.SetField("Tax", fieldValue);
                            this.QueryBuilderObject.SetField("Price", s);
                            this.QueryBuilderObject.SetField("PriceListID", str11);
                            this.QueryBuilderObject.InsertQueryString("PriceDefinition", base.db_vms);
                        }
                        else
                        {
                            num2 = int.Parse(base.GetFieldValue("PriceDefinition", "PriceDefinitionID", "PackID = " + str14 + " AND PriceListID = " + str11, base.db_vms));
                            if (!str15.Equals(s.ToString()))
                            {
                                this.QueryBuilderObject.SetField("Price", s);
                                this.QueryBuilderObject.UpdateQueryString("PriceDefinition", string.Concat(new object[] { "PackID = ", str14, " AND PriceListID = ", str11, " AND PriceDefinitionID = ", num2 }), base.db_vms);
                            }
                        }
                        if ((str9 != string.Empty) && (str8 != "1"))
                        {
                            string str16 = base.GetFieldValue("CustomerGroupLanguage", "GroupID", " Description = '" + str9 + "'", base.db_vms);
                            if (base.GetFieldValue("GroupPrice", "GroupID", " GroupID = " + str16 + " And PriceListID = " + str11, base.db_vms) == string.Empty)
                            {
                                this.QueryBuilderObject.SetField("GroupID", str16);
                                this.QueryBuilderObject.SetField("PriceListID", str11);
                                this.QueryBuilderObject.InsertQueryString("GroupPrice", base.db_vms);
                            }
                        }
                        if ((str10 != string.Empty) && (str8 != "1"))
                        {
                            string str18 = base.GetFieldValue("Customer", "CustomerID", " CustomerCode = '" + str10 + "'", base.db_vms);
                            if (str18 != string.Empty)
                            {
                                InCubeQuery query = new InCubeQuery("SELECT OutletID FROM CustomerOutlet Where CustomerID = " + str18, base.db_vms);
                                this.err = query.Execute();
                                this.err = query.FindFirst();
                                while (this.err == InCubeErrors.Success)
                                {
                                    query.GetField(0, ref field);
                                    string str19 = field.ToString();
                                    if (str19 == string.Empty)
                                    {
                                        MessageBox.Show("Not exist customer outlet");
                                    }
                                    this.err = base.ExistObject("CustomerPrice", "PriceListID", " CustomerID = " + str18 + " AND OutletID = " + str19, base.db_vms);
                                    if (this.err != InCubeErrors.Success)
                                    {
                                        this.QueryBuilderObject.SetField("CustomerID", str18);
                                        this.QueryBuilderObject.SetField("OutletID", str19);
                                        this.QueryBuilderObject.SetField("PriceListID", str11);
                                        this.QueryBuilderObject.InsertQueryString("CustomerPrice", base.db_vms);
                                    }
                                    this.err = query.FindNext();
                                }
                            }
                            else
                            {
                                str18 = base.GetFieldValue("CustomerOutlet", "CustomerID", " CustomerCode = '" + str10 + "'", base.db_vms);
                                string str20 = base.GetFieldValue("CustomerOutlet", "OutletID", " CustomerCode = '" + str10 + "'", base.db_vms);
                                if (str18 == string.Empty)
                                {
                                    MessageBox.Show("Not exist customer" + str10 + " ");
                                }
                                this.err = base.ExistObject("CustomerPrice", "PriceListID", " CustomerID = " + str18 + " AND OutletID = " + str20, base.db_vms);
                                if (this.err != InCubeErrors.Success)
                                {
                                    this.QueryBuilderObject.SetField("CustomerID", str18);
                                    this.QueryBuilderObject.SetField("OutletID", str20);
                                    this.QueryBuilderObject.SetField("PriceListID", str11);
                                    this.QueryBuilderObject.InsertQueryString("CustomerPrice", base.db_vms);
                                }
                            }
                        }
                    }
                }
            }
            dataTable.Dispose();
        }

        void UpdateRoutes()
        {
            try
            {
                int num = 0;
                int num2 = 0;
                new object();
                this.DefaultOrganization();
                string cmdText = "SELECT ROUTENAME, SALESMANCODE, CUSTOMERCODE FROM ROUTES";
                DataTable dataTable = new DataTable();
                OleDbCommand selectCommand = new OleDbCommand(cmdText, this.Conn);
                OleDbDataAdapter adapter = new OleDbDataAdapter(selectCommand);
                adapter.Fill(dataTable);
                adapter.Dispose();
                ClearProgress();
                SetProgressMax(dataTable.Rows.Count);
                new InCubeQuery(base.db_vms, "Delete From RouteCustomer").ExecuteNonQuery();
                new InCubeQuery(base.db_vms, "Delete From CustOutTerritory").ExecuteNonQuery();
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    ReportProgress("Updating Routes ");
                    num2++;
                    string str2 = dataTable.Rows[i][0].ToString();
                    string str3 = dataTable.Rows[i][1].ToString();
                    string str4 = dataTable.Rows[i][2].ToString();
                    if (str2 != string.Empty)
                    {
                        string fieldValue = base.GetFieldValue("RouteLanguage", "RouteID", " Description = '" + str2 + "' AND LanguageID = 1", base.db_vms);
                        if (fieldValue == string.Empty)
                        {
                            fieldValue = base.GetFieldValue("Route", "isnull(MAX(RouteID),0) + 1", base.db_vms);
                        }
                        string str6 = base.GetFieldValue("CustomerOutlet", "CustomerID", " CustomerCode = '" + str4 + "'", base.db_vms);
                        string str7 = base.GetFieldValue("CustomerOutlet", "OutletID", " CustomerCode = '" + str4 + "'", base.db_vms);
                        if (str7 != string.Empty)
                        {
                            string str8 = base.GetFieldValue("Employee", "EmployeeID", " EmployeeCode = '" + str3 + "'", base.db_vms);
                            if (str8 != string.Empty)
                            {
                                if (base.ExistObject("Territory", "TerritoryID", "TerritoryID = " + fieldValue, base.db_vms) != InCubeErrors.Success)
                                {
                                    num++;
                                    this.QueryBuilderObject.SetField("TerritoryID", fieldValue);
                                    this.QueryBuilderObject.SetField("OrganizationID", this.OrganizationID);
                                    this.QueryBuilderObject.SetField("CreatedBy", this.UserID.ToString());
                                    this.QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                                    this.QueryBuilderObject.SetField("UpdatedBy", this.UserID.ToString());
                                    this.QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                                    this.QueryBuilderObject.InsertQueryString("Territory", base.db_vms);
                                }
                                if (base.ExistObject("Route", "RouteID", "RouteID = " + fieldValue, base.db_vms) != InCubeErrors.Success)
                                {
                                    DateTime time = DateTime.Parse(DateTime.Now.Date.AddHours(7.0).ToString());
                                    DateTime time2 = DateTime.Parse(DateTime.Now.Date.AddHours(23.0).ToString());
                                    this.QueryBuilderObject.SetField("RouteID", fieldValue);
                                    this.QueryBuilderObject.SetField("Inactive", "0");
                                    this.QueryBuilderObject.SetField("TerritoryID", fieldValue);
                                    this.QueryBuilderObject.SetField("EstimatedStart", "'" + time.ToString("dd/MMM/yyyy HH:mm tt") + "'");
                                    this.QueryBuilderObject.SetField("EstimatedEnd", "'" + time2.ToString("dd/MMM/yyyy HH:mm tt") + "'");
                                    this.QueryBuilderObject.SetField("CreatedBy", this.UserID.ToString());
                                    this.QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                                    this.QueryBuilderObject.SetField("UpdatedBy", this.UserID.ToString());
                                    this.QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                                    this.QueryBuilderObject.InsertQueryString("Route", base.db_vms);
                                }
                                if (base.ExistObject("RouteLanguage", "RouteID", "RouteID = " + fieldValue + " AND LanguageID = 1", base.db_vms) != InCubeErrors.Success)
                                {
                                    this.QueryBuilderObject.SetField("RouteID", fieldValue);
                                    this.QueryBuilderObject.SetField("LanguageID", "1");
                                    this.QueryBuilderObject.SetField("Description", "'" + str2 + "'");
                                    this.QueryBuilderObject.InsertQueryString("RouteLanguage", base.db_vms);
                                }
                                if (base.ExistObject("TerritoryLanguage", "TerritoryID", "TerritoryID = " + fieldValue + " AND LanguageID = 1", base.db_vms) != InCubeErrors.Success)
                                {
                                    this.QueryBuilderObject.SetField("TerritoryID", fieldValue);
                                    this.QueryBuilderObject.SetField("LanguageID", "1");
                                    this.QueryBuilderObject.SetField("Description", "'" + str2 + "'");
                                    this.QueryBuilderObject.InsertQueryString("TerritoryLanguage", base.db_vms);
                                }
                                if (base.ExistObject("RouteLanguage", "RouteID", "RouteID = " + fieldValue + " AND LanguageID = 2", base.db_vms) != InCubeErrors.Success)
                                {
                                    this.QueryBuilderObject.SetField("RouteID", fieldValue);
                                    this.QueryBuilderObject.SetField("LanguageID", "2");
                                    this.QueryBuilderObject.SetField("Description", "'" + str2 + "'");
                                    this.QueryBuilderObject.InsertQueryString("RouteLanguage", base.db_vms);
                                }
                                if (base.ExistObject("TerritoryLanguage", "TerritoryID", "TerritoryID = " + fieldValue + " AND LanguageID = 2", base.db_vms) != InCubeErrors.Success)
                                {
                                    this.QueryBuilderObject.SetField("TerritoryID", fieldValue);
                                    this.QueryBuilderObject.SetField("LanguageID", "2");
                                    this.QueryBuilderObject.SetField("Description", "N'" + str2 + "'");
                                    this.QueryBuilderObject.InsertQueryString("TerritoryLanguage", base.db_vms);
                                }
                                if (base.ExistObject("RouteCustomer", "RouteID", "RouteID = " + fieldValue + " AND CustomerID = " + str6, base.db_vms) != InCubeErrors.Success)
                                {
                                    num++;
                                    this.QueryBuilderObject.SetField("RouteID", fieldValue);
                                    this.QueryBuilderObject.SetField("CustomerID", str6);
                                    this.QueryBuilderObject.SetField("OutletID", str7);
                                    this.QueryBuilderObject.InsertQueryString("RouteCustomer", base.db_vms);
                                }
                                if (base.ExistObject("RouteVisitPattern", "RouteID", "RouteID = " + fieldValue, base.db_vms) != InCubeErrors.Success)
                                {
                                    num++;
                                    this.QueryBuilderObject.SetField("RouteID", fieldValue);
                                    this.QueryBuilderObject.SetField("Week", "1");
                                    this.QueryBuilderObject.SetField("Sunday", "1");
                                    this.QueryBuilderObject.SetField("Monday", "1");
                                    this.QueryBuilderObject.SetField("Tuesday", "1");
                                    this.QueryBuilderObject.SetField("Wednesday", "1");
                                    this.QueryBuilderObject.SetField("Thursday", "1");
                                    this.QueryBuilderObject.SetField("Friday", "1");
                                    this.QueryBuilderObject.SetField("Saturday", "1");
                                    this.QueryBuilderObject.InsertQueryString("RouteVisitPattern", base.db_vms);
                                }
                                if ((base.ExistObject("Employee", "EmployeeID", "EmployeeID = " + str8, base.db_vms) == InCubeErrors.Success) && (base.ExistObject("EmployeeTerritory", "EmployeeID", "EmployeeID = " + str8 + " AND TerritoryID = " + fieldValue, base.db_vms) != InCubeErrors.Success))
                                {
                                    num++;
                                    this.QueryBuilderObject.SetField("EmployeeID", str8);
                                    this.QueryBuilderObject.SetField("TerritoryID", fieldValue);
                                    this.QueryBuilderObject.InsertQueryString("EmployeeTerritory", base.db_vms);
                                }
                                if (base.ExistObject("CustOutTerritory", "TerritoryID", "TerritoryID = " + fieldValue + " AND CustomerID = " + str6 + " AND OutletID = " + str7, base.db_vms) != InCubeErrors.Success)
                                {
                                    num++;
                                    this.QueryBuilderObject.SetField("CustomerID", str6);
                                    this.QueryBuilderObject.SetField("OutletID", str7);
                                    this.QueryBuilderObject.SetField("TerritoryID", fieldValue);
                                    this.QueryBuilderObject.InsertQueryString("CustOutTerritory", base.db_vms);
                                }
                            }
                        }
                    }
                }
                dataTable.Dispose();
                WriteMessage("\r\n");
                WriteMessage("<<< ROUTE >>> Total Inserted = " + num);
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
            finally
            {
                this.Conn.Close();
            }
        }

        public override void UpdateSalesPerson()
        {
            try
            {
                int tOTALUPDATED = 0;
                int tOTALINSERTED = 0;
                this.UpdateBanks();
                new object();
                string cmdText = "Select EmployeeCode,NameE,NameA,Phone,Creditlimit,Balance,Division,EmployeeTypeID FROM Employee";
                this.DefaultOrganization();
                DataTable dataTable = new DataTable();
                OleDbCommand selectCommand = new OleDbCommand(cmdText, this.Conn);
                OleDbDataAdapter adapter = new OleDbDataAdapter(selectCommand);
                adapter.Fill(dataTable);
                adapter.Dispose();
                ClearProgress();
                SetProgressMax(dataTable.Rows.Count);
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    ReportProgress("Updating Employee ");
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
                        string salespersonID = base.GetFieldValue("Employee", "EmployeeID", "Employeecode = '" + salespersonCode + "'", base.db_vms);
                        if (salespersonID == string.Empty)
                        {
                            salespersonID = base.GetFieldValue("Employee", "isnull(MAX(EmployeeID),0) + 1", base.db_vms);
                        }
                        this.AddUpdateSalesperson(salespersonID, salespersonCode, salespersonNameArabic, salespersonNameEnglish, phone, ref tOTALUPDATED, ref tOTALINSERTED, divisionID, creditLimit, balance, emplyeeTypeID);
                    }
                }
                dataTable.Dispose();
                WriteMessage("\r\n");
                WriteMessage(string.Concat(new object[] { "<<< SALESPERSON >>> Total Updated = ", tOTALUPDATED, " , Total Inserted = ", tOTALINSERTED }));
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
            finally
            {
                this.Conn.Close();
            }
        }

        public override void UpdateStock(bool UpdateAll, string WarehouseID, bool CaseChecked, DateTime StockDate)
        {
            this.UpdateStockForWarehouse();
        }

        private void UpdateStockForWarehouse()
        {
            try
            {
                int num = 0;
                object field = new object();
                InCubeQuery query = new InCubeQuery(base.db_vms, "Select WarehouseID from Warehouse");
                query.Execute();
                InCubeErrors errors = query.FindFirst();
                while (errors == InCubeErrors.Success)
                {
                    query.GetField(0, ref field);
                    string vehicleID = field.ToString();
                    if (!base.IsVehicleUploaded(vehicleID, base.db_vms))
                    {
                        string sqlQuery = "delete from WarehouseStock Where WarehouseID = " + vehicleID;
                        this.QueryBuilderObject.RunQuery(sqlQuery, base.db_vms);
                    }
                    errors = query.FindNext();
                }
                query.Close();
                string cmdText = "SELECT WarehouseCode, ITEMCODE, UOM,Quantity,Batch,EXPIRYDATE FROM Stock";
                this.DefaultOrganization();
                DataTable dataTable = new DataTable();
                OleDbCommand selectCommand = new OleDbCommand(cmdText, this.Conn);
                OleDbDataAdapter adapter = new OleDbDataAdapter(selectCommand);
                adapter.Fill(dataTable);
                adapter.Dispose();
                ClearProgress();
                SetProgressMax(dataTable.Rows.Count);
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    ReportProgress("Updating Stock ");
                    num++;
                    string str4 = dataTable.Rows[i][0].ToString();
                    string str5 = dataTable.Rows[i][1].ToString();
                    string str6 = dataTable.Rows[i][2].ToString();
                    string fieldValue = dataTable.Rows[i][3].ToString();
                    string str8 = dataTable.Rows[i][4].ToString();
                    string s = dataTable.Rows[i][5].ToString();
                    if (str4 != string.Empty)
                    {
                        if (str8 == string.Empty)
                        {
                            str8 = "NoBatch";
                        }
                        try
                        {
                            s = DateTime.Parse(s).ToString(this.DateFormat);
                        }
                        catch
                        {
                            s = DateTime.Now.AddDays(10.0).ToString(this.DateFormat);
                        }
                        string str10 = base.GetFieldValue("Warehouse", "WarehouseID", "WarehouseCode = '" + str4 + "'", base.db_vms);
                        string str11 = base.GetFieldValue("Item", "ItemID", "ItemCode = '" + str5 + "'", base.db_vms);
                        string str13 = base.GetFieldValue("Pack", "PackID", "ItemID = " + str11 + " AND PackTypeID = " + base.GetFieldValue("PackTypeLanguage", "PackTypeID", " LanguageID = 1 and Description = '" + str6 + "'", base.db_vms), base.db_vms);
                        if (((str10 != string.Empty) && (str11 != string.Empty)) && !base.IsVehicleUploaded(str10, base.db_vms))
                        {
                            num++;
                            InCubeQuery query2 = new InCubeQuery("Select PackID from Pack where ItemID = " + str11, base.db_vms);
                            query2.Execute();
                            for (errors = query2.FindFirst(); errors == InCubeErrors.Success; errors = query2.FindNext())
                            {
                                query2.GetField(0, ref field);
                                string str15 = field.ToString();
                                string str16 = "0";
                                if (base.ExistObject("WarehouseStock", "PackID", "WarehouseID = " + str10 + " AND ZoneID = 1 AND PackID = " + str15 + " AND ExpiryDate = '" + s + "' AND BatchNo = '" + str8 + "'", base.db_vms) != InCubeErrors.Success)
                                {
                                    this.QueryBuilderObject.SetField("WarehouseID", str10);
                                    this.QueryBuilderObject.SetField("ZoneID", "1");
                                    this.QueryBuilderObject.SetField("PackID", str15);
                                    this.QueryBuilderObject.SetField("ExpiryDate", "'" + s + "'");
                                    this.QueryBuilderObject.SetField("BatchNo", "'" + str8 + "'");
                                    this.QueryBuilderObject.SetField("SampleQuantity", "0");
                                    if (str15 == str13)
                                    {
                                        this.QueryBuilderObject.SetField("Quantity", fieldValue);
                                        this.QueryBuilderObject.SetField("BaseQuantity", fieldValue);
                                    }
                                    else
                                    {
                                        this.QueryBuilderObject.SetField("Quantity", str16);
                                        this.QueryBuilderObject.SetField("BaseQuantity", str16);
                                    }
                                    this.QueryBuilderObject.InsertQueryString("WarehouseStock", base.db_vms);
                                }
                                else if (str15 == str13)
                                {
                                    this.QueryBuilderObject.SetField("Quantity", fieldValue);
                                    this.QueryBuilderObject.SetField("BaseQuantity", fieldValue);
                                    this.QueryBuilderObject.UpdateQueryString("WarehouseStock", "WarehouseID = " + str10 + " AND ZoneID = 1 AND PackID = " + str13 + " AND ExpiryDate = '" + s + "' AND BatchNo = '" + str8 + "'", base.db_vms);
                                }
                                if (base.ExistObject("DailyWarehouseStock", "PackID", "WarehouseID = " + str10 + " AND ZoneID = 1 AND PackID = " + str15 + " AND ExpiryDate = '" + s + "' AND BatchNo = '" + str8 + "'", base.db_vms) != InCubeErrors.Success)
                                {
                                    this.QueryBuilderObject.SetField("WarehouseID", str10);
                                    this.QueryBuilderObject.SetField("ZoneID", "1");
                                    this.QueryBuilderObject.SetField("PackID", str15);
                                    this.QueryBuilderObject.SetField("ExpiryDate", "'" + s + "'");
                                    this.QueryBuilderObject.SetField("StockDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                                    this.QueryBuilderObject.SetField("BatchNo", "'" + str8 + "'");
                                    if (str15 == str13)
                                    {
                                        this.QueryBuilderObject.SetField("Quantity", fieldValue);
                                    }
                                    else
                                    {
                                        this.QueryBuilderObject.SetField("Quantity", str16);
                                    }
                                    this.QueryBuilderObject.SetField("ProductionDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                                    this.QueryBuilderObject.SetField("SampleQuantity", "0");
                                    this.QueryBuilderObject.InsertQueryString("DailyWarehouseStock", base.db_vms);
                                }
                                else if (str15 == str13)
                                {
                                    this.QueryBuilderObject.SetField("Quantity", fieldValue);
                                    this.QueryBuilderObject.UpdateQueryString("DailyWarehouseStock", "WarehouseID = " + str10 + " AND ZoneID = 1 AND PackID = " + str13 + " AND ExpiryDate = '" + s + "' AND BatchNo = '" + str8 + "'", base.db_vms);
                                }
                            }
                        }
                    }
                }
                dataTable.Dispose();
                WriteMessage("\r\n");
                WriteMessage("<<< STOCK Updated >>> Total Updated = " + num);
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }

        public override void UpdateWarehouse()
        {
            try
            {
                int tOTALUPDATED = 0;
                int tOTALINSERTED = 0;
                new object();
                string cmdText = "SELECT  \r\nWarehouseCode,Description,Platenumber,VehicleType,SalespersonCode,HelperCode\r\nFROM Vehicle";
                this.DefaultOrganization();
                DataTable dataTable = new DataTable();
                OleDbCommand selectCommand = new OleDbCommand(cmdText, this.Conn);
                OleDbDataAdapter adapter = new OleDbDataAdapter(selectCommand);
                adapter.Fill(dataTable);
                adapter.Dispose();
                ClearProgress();
                SetProgressMax(dataTable.Rows.Count);
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    ReportProgress("Updating WAREHOUSE ");
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
                        warehouseID = base.GetFieldValue("Warehouse", "WarehouseID", "WarehouseCode='" + warehouseCode + "'", base.db_vms);
                        if (warehouseID == string.Empty)
                        {
                            warehouseID = base.GetFieldValue("Warehouse", "isnull(MAX(WarehouseID),0) + 1", base.db_vms);
                        }
                        this.AddUpdateWarehouse(warehouseID, warehouseCode, warehouceName, address, vehicleRegNum, salesmanCode, ref tOTALUPDATED, ref tOTALINSERTED, warehouseType, helperCode);
                    }
                }
                dataTable.Dispose();
                WriteMessage("\r\n");
                WriteMessage(string.Concat(new object[] { "<<< WAREHOUSE >>> Total Updated = ", tOTALUPDATED, " , Total Inserted = ", tOTALINSERTED }));
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
            finally
            {
                this.Conn.Close();
            }
        }
    }
}
