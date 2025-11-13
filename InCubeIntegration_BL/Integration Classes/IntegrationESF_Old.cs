using InCubeIntegration_DAL;
using InCubeLibrary;
using Microsoft.Dynamics.GP.eConnect;
using Microsoft.Dynamics.GP.eConnect.Serialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace InCubeIntegration_BL
{
    public class IntegrationESF_Old : IntegrationBase
    {
        enum requiredColumns
        {
            ID, CustomerCode, EmployeeCode, TerritoryCode, Sequence, Day, Week
        }

        QueryBuilder QueryBuilderObject = new QueryBuilder();
        string sConnectionString = "";
        InCubeErrors err = InCubeErrors.Error;
        private long UserID;
        string DateFormat = "MM/dd/yyyy HH:mm:ss";
        string StockDateFormat = "yyyy/MM/dd";
        InCubeQuery qry;
        CultureInfo EsES = new CultureInfo("es-ES");
        public IntegrationESF_Old(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            db_ERP = new InCubeDatabase();
            InCubeErrors err = db_ERP.Open("InCubeSQL", "InVanSend");
            if (err != InCubeErrors.Success)
            {
                WriteMessage("Unable to connect to GP database");
                return;
            }
            db_ERP_GET = new InCubeDatabase();
            err = db_ERP_GET.Open("InCubeGet", "InVanGet");
            if (err != InCubeErrors.Success)
            {
                WriteMessage("Unable to connect to Staging database");
                return;
            }

            sConnectionString = db_ERP.GetConnection().ConnectionString;
            UserID = CurrentUserID;
        }

        #region UpdateItem

        public override void UpdateItem()
        {
            try
            {
                if (CoreGeneral.Common.GeneralConfigurations.SiteSymbol.ToLower() == "esfnew")
                    return;

                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;

                object field = new object();

                string PacktypeID = string.Empty;

                DataTable DT = new DataTable();
                qry = new InCubeQuery(@"SELECT [ItemCode]
      ,[ItemName]
      ,[ItemCategoryCode]
      ,[ItemCategoryDescription]
      ,[ItemGroupCode]
      ,[ItemGroupName]
      ,[ItemBrandCode]
      ,[ItemBrandName]
      ,[DivisionCode]
      ,[DivisionName]
      ,[Barcode]
	  ,ISNULL(MIN(V.TaxPct),0) TaxRate
      ,ISNULL([Status],1) [Status]
      ,[FLAG] FROM Items I
	  LEFT JOIN ItemVAT V ON V.Item = I.ItemCode
	  where Flag='0' GROUP BY [ItemCode]
      ,[ItemName]
      ,[ItemCategoryCode]
      ,[ItemCategoryDescription]
      ,[ItemGroupCode]
      ,[ItemGroupName]
      ,[ItemBrandCode]
      ,[ItemBrandName]
      ,[DivisionCode]
      ,[DivisionName]
      ,[Barcode]
      ,ISNULL([Status],1)
      ,[FLAG] ORDER BY ITEMCODE", db_ERP_GET);
                err = qry.Execute();
                DT = qry.GetDataTable();

                DataTable DT2 = new DataTable();
                qry = new InCubeQuery(@"SELECT DISTINCT ITEMCODE,ITEMUOM,CONVERSIONFACTOR FROM ITEMS WHERE Flag='0' ORDER BY CONVERSIONFACTOR ASC", db_ERP_GET);
                err = qry.Execute();
                DT2 = qry.GetDataTable();

                ClearProgress();
                SetProgressMax(DT.Rows.Count);

                foreach (DataRow row in DT.Rows)
                {
                    ReportProgress("Updating Items");

                    string status = "1";
                    if (status == "INACTIVE") { status = "1"; } else { status = "0"; }
                    string ItemCode = row["ItemCode"].ToString().Trim();
                    string itemDescriptionEnglish = row["ItemName"].ToString().Trim();
                    string itemDescriptionArabic = row["ItemName"].ToString().Trim();
                    string DivisionCode = row["ItemBrandCode"].ToString().Trim();
                    string DivisionNameEnglish = row["ItemBrandName"].ToString().Trim();
                    string CategoryCode = DivisionCode + row["ItemCategoryCode"].ToString().Trim();
                    string CategoryNameEnglish = row["ItemCategoryDescription"].ToString().Trim();
                    string Brand = row["ItemBrandName"].ToString().Trim();
                    string Orgin = row["ItemBrandCode"].ToString().Trim();
                    string TCAllowed = "0";
                    if (TCAllowed == "Y") { TCAllowed = "1"; } else { TCAllowed = "0"; }
                    string barcode = row["Barcode"].ToString().Trim();
                    string PackGroup = row["ItemGroupName"].ToString().Trim();
                    string PackGroupCode = row["ItemGroupCode"].ToString().Trim();
                    string PackGroupID = string.Empty;
                    decimal TaxRate = decimal.Parse(row["TaxRate"].ToString());
                    if (ItemCode == string.Empty)
                        continue;

                    #region ItemDivision

                    string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode = '" + DivisionCode + "'", db_vms);

                    if (DivisionID == string.Empty)
                    {
                        DivisionID = GetFieldValue("Division", "isnull(MAX(DivisionID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("DivisionID", DivisionID);
                        QueryBuilderObject.SetField("DivisionCode", "'" + DivisionCode + "'");
                        QueryBuilderObject.SetField("OrganizationID", OrganizationID.ToString());
                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        err = QueryBuilderObject.InsertQueryString("Division", db_vms);

                        QueryBuilderObject.SetField("DivisionID", DivisionID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + DivisionNameEnglish + "'");
                        err = QueryBuilderObject.InsertQueryString("DivisionLanguage", db_vms);

                        QueryBuilderObject.SetField("DivisionID", DivisionID);  // Arabic Description
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "'" + DivisionNameEnglish + "'");
                        err = QueryBuilderObject.InsertQueryString("DivisionLanguage", db_vms);
                    }

                    #endregion

                    #region ItemCategory

                    string ItemCategoryID = GetFieldValue("ItemCategory", "ItemCategoryID", "ItemCategoryCode = '" + CategoryCode + "'", db_vms);

                    if (ItemCategoryID == string.Empty)
                    {
                        ItemCategoryID = GetFieldValue("ItemCategory", "isnull(MAX(ItemCategoryID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID);
                        QueryBuilderObject.SetField("ItemCategoryCode", "'" + CategoryCode + "'");
                        QueryBuilderObject.SetField("DivisionID", DivisionID.ToString());
                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        err = QueryBuilderObject.InsertQueryString("ItemCategory", db_vms);

                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + CategoryNameEnglish + "'");
                        err = QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", db_vms);

                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID);  // Arabic Description
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "'" + CategoryNameEnglish + "'");
                        err = QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", db_vms);

                    }
                    else
                    {
                        string _existItemCategoryID = GetFieldValue("ItemCategory", "ItemCategoryID", "ItemCategoryCode = '" + CategoryCode + "' AND DivisionID =" + DivisionID.ToString(), db_vms);
                        if (ItemCategoryID == string.Empty)
                        {
                            WriteMessage("\r\n");
                            WriteMessage(" Item Category " + CategoryNameEnglish + " is defined twice , the duplicated division : " + DivisionNameEnglish);
                        }
                    }

                    #endregion

                    #region Brand

                    string BrandID = GetFieldValue("BrandLanguage", "BrandID", "Description = '" + Brand + "' and LanguageID=1", db_vms).Trim();
                    if (BrandID == string.Empty)
                    {

                        BrandID = GetFieldValue("Brand", "isnull(MAX(BrandID),0) + 1", db_vms);
                        QueryBuilderObject.SetField("BrandID", BrandID);
                        err = QueryBuilderObject.InsertQueryString("Brand", db_vms);

                        QueryBuilderObject.SetField("BrandID", BrandID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + Brand + "'");
                        err = QueryBuilderObject.InsertQueryString("BrandLanguage", db_vms);

                    }
                    #endregion

                    #region Item
                    string ItemID = "";

                    ItemID = GetFieldValue("Item", "ItemID", "ItemCode='" + ItemCode + "' and PackDefinition='" + DivisionCode + "'", db_vms);
                    if (ItemID == string.Empty)
                    {
                        ItemID = GetFieldValue("Item", "isnull(MAX(ItemID),0) + 1", db_vms);
                    }

                    string ExistItem = GetFieldValue("Item", "ItemID", "ItemID = " + ItemID, db_vms);
                    if (ExistItem != string.Empty) // Exist Item --- Update Query
                    {
                        TOTALUPDATED++;

                        QueryBuilderObject.SetField("ItemCode", "'" + ItemCode + "'");
                        QueryBuilderObject.SetField("InActive", status);
                        QueryBuilderObject.SetField("Origin", "'" + Orgin + "'");
                        QueryBuilderObject.SetField("BrandID", BrandID);
                        QueryBuilderObject.SetField("TemporaryCredit", TCAllowed);
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        err = QueryBuilderObject.UpdateQueryString("Item", " ItemID = " + ItemID, db_vms);

                    }
                    else // New Item --- Insert Query
                    {
                        TOTALINSERTED++;

                        QueryBuilderObject.SetField("ItemID", ItemID);
                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());
                        QueryBuilderObject.SetField("ItemCode", "'" + ItemCode + "'");
                        QueryBuilderObject.SetField("InActive", status);
                        QueryBuilderObject.SetField("PackDefinition", "'" + DivisionCode + "'");
                        QueryBuilderObject.SetField("TemporaryCredit", TCAllowed);
                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("ItemType", "1");
                        QueryBuilderObject.SetField("Origin", "'" + Orgin + "'");
                        QueryBuilderObject.SetField("BrandID", BrandID);
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                        err = QueryBuilderObject.InsertQueryString("Item", db_vms);
                    }

                    #endregion

                    #region ItemLanguage

                    ExistItem = GetFieldValue("ItemLanguage", "ItemID", "ItemID = " + ItemID + " AND LanguageID = 1", db_vms);
                    if (ExistItem != string.Empty)
                    {
                        QueryBuilderObject.SetField("Description", "'" + itemDescriptionEnglish + "'");
                        QueryBuilderObject.UpdateQueryString("ItemLanguage", " ItemID =" + ItemID + " AND LanguageID = 1", db_vms);

                    }
                    else
                    {
                        QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + itemDescriptionEnglish + "'");
                        err = QueryBuilderObject.InsertQueryString("ItemLanguage", db_vms);
                    }

                    if (itemDescriptionArabic != string.Empty)
                    {
                        ExistItem = GetFieldValue("ItemLanguage", "ItemID", "ItemID = " + ItemID + " AND LanguageID = 2", db_vms); // ARABIC
                        if (ExistItem != string.Empty)
                        {
                            QueryBuilderObject.SetField("Description", "N'" + itemDescriptionArabic + "'");
                            QueryBuilderObject.UpdateQueryString("ItemLanguage", " ItemID =" + ItemID + " AND LanguageID = 2", db_vms);
                        }
                        else
                        {
                            QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                            QueryBuilderObject.SetField("LanguageID", "2");
                            QueryBuilderObject.SetField("Description", "N'" + itemDescriptionArabic + "-" + DivisionNameEnglish + "'");
                            err = QueryBuilderObject.InsertQueryString("ItemLanguage", db_vms);
                        }
                    }
                    #endregion

                    #region PackGroup

                    PackGroupID = GetFieldValue("PackGroupLanguage", "PackGroupID", "Description = '" + PackGroup + "'", db_vms);

                    if (PackGroupID == string.Empty)
                    {
                        PackGroupID = GetFieldValue("PackGroup", "isnull(MAX(PackGroupID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("PackGroupID", PackGroupID);

                        err = QueryBuilderObject.InsertQueryString("PackGroup", db_vms);

                        QueryBuilderObject.SetField("PackGroupID", PackGroupID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + PackGroup + "'");
                        err = QueryBuilderObject.InsertQueryString("PackGroupLanguage", db_vms);

                        QueryBuilderObject.SetField("PackGroupID", PackGroupID);  // Arabic Description
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "'" + PackGroup + "'");
                        QueryBuilderObject.InsertQueryString("PackGroupLanguage", db_vms);
                    }
                    else
                    {
                        QueryBuilderObject.SetField("Description", "'" + PackGroup + "'");
                        err = QueryBuilderObject.UpdateQueryString("PackGroupLanguage", "PackGroupID='" + PackGroupID + "'", db_vms);
                    }

                    #endregion

                    #region UPDATE/INSERT PACK
                    bool first = true;
                    decimal caseConversion = 0;
                    foreach (DataRow DrP in DT2.Select("ITEMCODE='" + ItemCode + "'"))
                    {
                        string packQty = DrP["ConversionFactor"].ToString().Trim();
                        decimal tempQty = decimal.Parse(DrP["ConversionFactor"].ToString().Trim());

                        if (first)
                        {
                            if (tempQty < 1)
                            {
                                caseConversion = Math.Round(1 / decimal.Parse(packQty), 0);
                                packQty = "1";
                            }
                            first = false;
                        }
                        else if (tempQty == 1)
                        {
                            if (DT2.Rows.Count == 1)
                            {

                            }
                            packQty = caseConversion.ToString();
                        }
                        else if (tempQty < 1 && !first)
                        {
                            packQty = Math.Round((caseConversion * tempQty), 0).ToString();
                        }
                        if (decimal.Parse(packQty) == 0) packQty = "1";

                        string PackDescriptionEnglish = DrP["ItemUOM"].ToString().Trim();

                        #region PackType

                        PacktypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " Description = '" + PackDescriptionEnglish + "'", db_vms);

                        if (PacktypeID == string.Empty)
                        {
                            PacktypeID = GetFieldValue("PackType", "isnull(MAX(PackTypeID),0) + 1", db_vms);

                            QueryBuilderObject.SetField("PackTypeID", PacktypeID);
                            err = QueryBuilderObject.InsertQueryString("PackType", db_vms);

                            QueryBuilderObject.SetField("PackTypeID", PacktypeID);
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + PackDescriptionEnglish + "'");
                            err = QueryBuilderObject.InsertQueryString("PackTypeLanguage", db_vms);

                            QueryBuilderObject.SetField("PackTypeID", PacktypeID);
                            QueryBuilderObject.SetField("LanguageID", "2");
                            QueryBuilderObject.SetField("Description", "N'" + PackDescriptionEnglish + "'");
                            err = QueryBuilderObject.InsertQueryString("PackTypeLanguage", db_vms);

                        }

                        #endregion

                        int PackID = 1;

                        ExistItem = GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " and PackTypeID = " + PacktypeID, db_vms);
                        if (ExistItem != string.Empty)
                        {
                            PackID = int.Parse(GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " and PackTypeID = " + PacktypeID, db_vms));
                            QueryBuilderObject.SetField("Quantity", packQty);
                            QueryBuilderObject.SetField("Barcode", "'" + barcode + "'");
                            QueryBuilderObject.SetField("Width", TaxRate.ToString());
                            err = QueryBuilderObject.UpdateQueryString("Pack", "PackID = " + PackID, db_vms);
                        }
                        else
                        {
                            PackID = int.Parse(GetFieldValue("Pack", "ISNULL(MAX(PackID),0) + 1", db_vms));

                            QueryBuilderObject.SetField("PackID", PackID.ToString());
                            QueryBuilderObject.SetField("Barcode", "'" + barcode + "'");
                            QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                            QueryBuilderObject.SetField("PackTypeID", PacktypeID);
                            QueryBuilderObject.SetField("Quantity", packQty);
                            QueryBuilderObject.SetField("EquivalencyFactor", "0");
                            QueryBuilderObject.SetField("Width", TaxRate.ToString());
                            err = QueryBuilderObject.InsertQueryString("Pack", db_vms);
                        }

                        string packGroupDetail = ExistItem = GetFieldValue("PackGroupDetail", "PackID", "PackID = " + PackID + " and PackGroupID = " + PackGroupID, db_vms);
                        if (packGroupDetail == string.Empty)
                        {
                            QueryBuilderObject.SetField("PackID", PackID.ToString());
                            QueryBuilderObject.SetField("PackGroupID", PackGroupID.ToString());
                            err = QueryBuilderObject.InsertQueryString("PackGroupDetail", db_vms);
                        }
                        UpdateFlag("Items", "ItemCode='" + ItemCode + "' and ItemUOM='" + PackDescriptionEnglish + "'");
                    }
                    #endregion
                }

                DT.Dispose();

                WriteMessage("\r\n");
                WriteMessage("<<< ITEMS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        #endregion

        #region UpdateCustomer

        public override void UpdateCustomer()
        {
            if (CoreGeneral.Common.GeneralConfigurations.SiteSymbol.ToLower() == "esfnew")
                return;

            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            object field = new object();

            string PacktypeID = string.Empty;
            
            DataTable DT = new DataTable();
            qry = new InCubeQuery(string.Format(@"SELECT C.*, ISNULL(V.Taxable,0) Taxable, ISNULL(V.TRN ,'') TRNO
                                    FROM CustomersEmployees C
                                    INNER JOIN {0}..Employee E ON E.EmployeeCode = C.SalesmanCode COLLATE SQL_Latin1_General_CP1_CI_AS
                                    LEFT JOIN CustomerVAT V ON V.CustomerNo = C.CUSTOMEROUTLETCODE
                                    WHERE C.Flag <> '1' AND E.InActive = 0", db_vms.GetConnection().Database), db_ERP_GET);
            err = qry.Execute();
            DT = qry.GetDataTable();

            ClearProgress();
            SetProgressMax(DT.Rows.Count);

            foreach (DataRow row in DT.Rows)
            {
                try
                {

                    ReportProgress("Updating Customers");

                    #region Variables
                    string CustomerCode = row["CUSTOMERGROUPCODE"].ToString().Trim();
                    string CustomerName = row["CUSTOMERGROUPNAME"].ToString().Trim();
                    string outletBarCode = row["CUSTOMEROUTLETCODE"].ToString().Trim();
                    string outletCode = row["CUSTOMEROUTLETCODE"].ToString().Trim();
                    string CustomerOutletDescriptionEnglish = row["CUSTOMEROUTLETNAME"].ToString().Trim();
                    string CustomerOutletDescriptionArabic = row["CUSTOMEROUTLETNAME"].ToString().Trim();
                    string Phonenumber = row["TELEPHONE"].ToString().Trim();
                    string Faxnumber = row["MOBILE"].ToString().Trim();
                    if (Phonenumber.Length >= 40) Phonenumber.Substring(0, 39);
                    if (Faxnumber.Length >= 40) Faxnumber.Substring(0, 39);
                    string Email = "";
                    string CustomerAddressEnglish = row["ADDRESS"].ToString().Trim();
                    string CustomerAddressArabic = row["ADDRESS"].ToString().Trim();
                    int Taxable = int.Parse(row["Taxable"].ToString());
                    string TRNO = row["TRNO"].ToString();
                    string CustomerNewGroup = row["CATEGORYNAME"].ToString().Trim();
                    string CustomerGroupDescription = row["CATEGORYNAME"].ToString().Trim();
                    string IsCreditCustomer = row["CUSTOMERTYPE"].ToString().Trim();
                    string CreditLimit = row["CREDITLIMIT"].ToString().Trim();
                    if (CreditLimit.Equals(string.Empty)) CreditLimit = "1000";
                    string Balance = "0";
                    string Paymentterms = row["PAYMENTTERMDAYS"].ToString().Trim();
                    string OnHold = row["Status"].ToString().Trim().ToLower() == "yes" ? "1" : "0";
                    string CustomerType = string.Empty;
                    if (Paymentterms.Equals(string.Empty)) Paymentterms = "30";
                    if (decimal.Parse(Paymentterms) > 0)
                    {
                        CustomerType = "2";
                    }
                    else
                    {
                        CustomerType = "1";
                    }

                    string inActive = "active";
                    if (inActive.ToLower().Equals("active"))
                    {
                        inActive = "0";
                    }
                    else
                    {
                        inActive = "1";
                    }
                    string DivisionCode = row["DIVISIONCODE"].ToString().Trim();
                    string STATE = row["STATE"].ToString().Trim();
                    string CITY = row["CITY"].ToString().Trim();
                    string B2B_Invoices = "0";
                    string GPSlongitude = "0";
                    string GPSlatitude = "0";
                    string Classification = row["STATUS"].ToString().Trim();

                    if (CustomerCode == string.Empty)
                        continue;

                    string CustomerID = "0";

                    string ExistCustomer = "";
                    #endregion

                    #region Get Customer Group
                    string GroupID2 = string.Empty;
                    string GroupID = GetFieldValue("CustomerGroupLanguage", "GroupID", " Description = '" + CustomerNewGroup.Trim() + "'  AND LanguageID = 1", db_vms);

                    if (GroupID == string.Empty)
                    {
                        GroupID = GetFieldValue("CustomerGroup", "isnull(MAX(GroupID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                        err = QueryBuilderObject.InsertQueryString("CustomerGroup", db_vms);

                        QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + CustomerNewGroup + "'");
                        err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);

                        QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "N'" + CustomerNewGroup + "'");
                        err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);
                    }

                    #endregion

                    #region Customer
                    CustomerID = GetFieldValue("Customer", "CustomerID", "CustomerCode = '" + CustomerCode.Replace("'", "''") + "'", db_vms);
                    if (CustomerID == string.Empty)
                    {
                        CustomerID = GetFieldValue("Customer", "isnull(MAX(CustomerID),0) + 1", db_vms);
                    }
                    ExistCustomer = GetFieldValue("Customer", "CustomerID", "CustomerID = " + CustomerID, db_vms);
                    if (ExistCustomer != string.Empty) // Exist Customer --- Update Query
                    {
                        TOTALUPDATED++;

                        QueryBuilderObject.SetField("CustomerCode", "'" + CustomerCode.Replace("'", "''") + "'");
                        QueryBuilderObject.SetField("Phone", "'" + Phonenumber + "'");
                        QueryBuilderObject.SetField("Fax", "'" + Faxnumber + "'");
                        QueryBuilderObject.SetField("OnHold", OnHold);
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        err = QueryBuilderObject.UpdateQueryString("Customer", " CustomerID = " + CustomerID, db_vms);

                    }
                    else // New Customer --- Insert Query
                    {
                        TOTALINSERTED++;

                        QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                        QueryBuilderObject.SetField("Phone", "'" + Phonenumber + "'");
                        QueryBuilderObject.SetField("Fax", "'" + Faxnumber + "'");
                        QueryBuilderObject.SetField("Email", "' '");
                        QueryBuilderObject.SetField("CustomerCode", "'" + CustomerCode.Replace("'", "''") + "'");
                        QueryBuilderObject.SetField("OnHold", OnHold);
                        QueryBuilderObject.SetField("StreetID", "0");
                        QueryBuilderObject.SetField("StreetAddress", "0");
                        QueryBuilderObject.SetField("InActive", "0");
                        QueryBuilderObject.SetField("New", "0");

                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        err = QueryBuilderObject.InsertQueryString("Customer", db_vms);
                    }

                    #endregion

                    #region CustomerLanguage
                    ExistCustomer = GetFieldValue("CustomerLanguage", "CustomerID", "CustomerID = " + CustomerID + " AND LanguageID = 1", db_vms);
                    if (ExistCustomer != string.Empty) // Exist CustomerLanguage --- Update Query
                    {
                        QueryBuilderObject.SetField("Description", "'" + CustomerName + "'");
                        QueryBuilderObject.SetField("Address", "'" + CustomerAddressEnglish + "'");
                        err = QueryBuilderObject.UpdateQueryString("CustomerLanguage", "  CustomerID = " + CustomerID + " AND LanguageID = 1", db_vms);
                    }
                    else  // New CustomerLanguage --- Insert Query
                    {
                        QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + CustomerName + "'");
                        QueryBuilderObject.SetField("Address", "'" + CustomerAddressEnglish + "'");
                        err = QueryBuilderObject.InsertQueryString("CustomerLanguage", db_vms);
                    }

                    ExistCustomer = GetFieldValue("CustomerLanguage", "CustomerID", "CustomerID = " + CustomerID + " AND LanguageID = 2", db_vms); // ARABIC
                    if (ExistCustomer != string.Empty) // Exist CustomerLanguage --- Update Query
                    {
                        QueryBuilderObject.SetField("Description", "N'" + CustomerName + "'");
                        QueryBuilderObject.SetField("Address", "N'" + CustomerAddressArabic + "'");
                        QueryBuilderObject.UpdateQueryString("CustomerLanguage", "  CustomerID = " + CustomerID + " AND LanguageID = 2", db_vms);
                    }
                    else  // New CustomerLanguage --- Insert Query
                    {
                        QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "N'" + CustomerName + "'");
                        QueryBuilderObject.SetField("Address", "N'" + CustomerAddressArabic + "'");
                        err = QueryBuilderObject.InsertQueryString("CustomerLanguage", db_vms);
                    }

                    #endregion

                    #region Customer Account

                    int AccountID = 1;

                    ExistCustomer = GetFieldValue("AccountCust", "AccountID", "CustomerID = " + CustomerID, db_vms);
                    if (ExistCustomer != string.Empty)
                    {
                        AccountID = int.Parse(GetFieldValue("AccountCust", "AccountID", "CustomerID = " + CustomerID, db_vms));

                        QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                        QueryBuilderObject.SetField("Balance", Balance);
                        err = QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + AccountID.ToString(), db_vms);

                    }
                    else
                    {
                        AccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));

                        QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                        QueryBuilderObject.SetField("AccountTypeID", "1");
                        QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                        QueryBuilderObject.SetField("Balance", Balance);
                        QueryBuilderObject.SetField("GL", "0");
                        QueryBuilderObject.SetField("OrganizationID", OrganizationID.ToString());
                        QueryBuilderObject.SetField("CurrencyID", "1");
                        err = QueryBuilderObject.InsertQueryString("Account", db_vms);

                        QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                        QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                        err = QueryBuilderObject.InsertQueryString("AccountCust", db_vms);

                        QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + CustomerOutletDescriptionEnglish.Trim() + " Account'");
                        err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);

                        QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "N'" + CustomerOutletDescriptionArabic.Trim() + " Account'");
                        err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
                    }
                    #endregion

                    CreateCustomerOutlet(STATE, CITY, AccountID.ToString(), GroupID, GroupID2, B2B_Invoices, CustomerType, outletCode, Paymentterms, CustomerOutletDescriptionEnglish, CustomerAddressEnglish, CustomerOutletDescriptionArabic, CustomerAddressArabic, Phonenumber, Faxnumber, OnHold, Taxable, TRNO, CustomerCode, CreditLimit, Balance, GPSlongitude, GPSlatitude, outletBarCode, Email, DivisionCode, inActive);
                }
                catch (Exception ex)
                {
                    WriteMessage("\r\n");
                    WriteMessage("customer failed ");
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                }
            }

            string qrystr = @"INSERT INTO CustomerPrice
SELECT CO.CustomerID,CO.OutletID,CP.PriceListID FROM
(SELECT DISTINCT CustomerID,PriceListID FROM CustomerPrice) CP
INNER JOIN CustomerOutlet CO ON CO.CustomerID = CP.CustomerID
EXCEPT 
SELECT * FROM CustomerPrice";
            qry = new InCubeQuery(db_vms, qrystr);
            qry.ExecuteNonQuery();

            qry = new InCubeQuery(db_vms, "sp_PrepareForMatchingCustomers");
            qry.ExecuteStoredProcedure();

            DT.Dispose();

            WriteMessage("\r\n");
            WriteMessage("<<< CUSTOMERS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }
        private void CreateCustomerOutlet(string state, string city, string parentAccount, string GroupID, string GroupID2, string B2B_Inv, string CustType, string CustomerCode, string Paymentterms, string CustomerDescriptionEnglish, string CustomerAddressEnglish, string CustomerDescriptionArabic, string CustomerAddressArabic, string Phonenumber, string Faxnumber, string OnHold, int Taxable, string TRNO, string HeadOfficeCode, string CreditLimit, string Balance, string Longitude, string latitude, string CustomerBarCode, string email, string divisionCode, string inactive)
        {
            int CustomerID;
            InCubeErrors err;

            if (Longitude == string.Empty)
                Longitude = "0";

            if (latitude == string.Empty)
                latitude = "0";

            string ExistCustomer = "";

            CustomerID = int.Parse(GetFieldValue("Customer", "CustomerID", "CustomerCode='" + HeadOfficeCode.Replace("'", "''") + "'", db_vms));

            string PaymentTermID = "1";

            if (CustType.ToLower().Equals("2") || CustType.ToLower().Equals("3"))
            {

                PaymentTermID = GetFieldValue("PaymentTerm", "PaymentTermID", "SimplePeriodWidth = " + Paymentterms, db_vms);
                if (PaymentTermID == string.Empty)
                {
                    PaymentTermID = GetFieldValue("PaymentTerm", "isnull(MAX(PaymentTermID),0) + 1", db_vms);

                    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                    QueryBuilderObject.SetField("PaymentTermTypeID", "1");
                    QueryBuilderObject.SetField("SimplePeriodWidth", Paymentterms);
                    QueryBuilderObject.SetField("SimplePeriodID", "1"); //Days
                    err = QueryBuilderObject.InsertQueryString("PaymentTerm", db_vms);

                    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'Every " + Paymentterms + " Days'");
                    err = QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms);
                }
            }

            #region Customer Outlet and language

            string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerID = " + CustomerID + " AND CustomerCode = '" + CustomerCode.Replace("'", "''") + "'", db_vms);
            if (!OutletID.Trim().Equals(string.Empty))
            {
                QueryBuilderObject.SetField("Phone", "'" + Phonenumber + "'");
                QueryBuilderObject.SetField("Fax", "'" + Faxnumber + "'");
                QueryBuilderObject.SetField("Email", "'" + email + "'");
                QueryBuilderObject.SetField("CustomerTypeID", CustType); //HardCoded -1- Cash -2- Credit
                QueryBuilderObject.SetField("OnHold", OnHold);
                QueryBuilderObject.SetField("Taxeable", Taxable.ToString());
                QueryBuilderObject.SetField("TaxNumber", "'" + TRNO + "'");
                QueryBuilderObject.SetField("Inactive", inactive);
                QueryBuilderObject.SetField("BillsOpenNumber", B2B_Inv);
                QueryBuilderObject.SetField("Barcode", "'" + CustomerBarCode + "'");
                QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("PreferredVisitTimeFrom", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("PreferredVisitTimeTo", "'" + DateTime.Now.ToString(DateFormat) + "'");
                err = QueryBuilderObject.UpdateQueryString("CustomerOutlet", "  CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);

            }
            else
            {
                OutletID = GetFieldValue("CustomerOutlet", "isnull(MAX(OutletID),0) + 1", "CustomerID = " + CustomerID, db_vms);

                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("CustomerCode", "'" + CustomerCode.Replace("'", "''") + "'");
                QueryBuilderObject.SetField("Barcode", "'" + CustomerBarCode + "'");
                QueryBuilderObject.SetField("Phone", "'" + Phonenumber + "'");
                QueryBuilderObject.SetField("Fax", "'" + Faxnumber + "'");
                QueryBuilderObject.SetField("Email", "'" + email + "'");
                QueryBuilderObject.SetField("Taxeable", Taxable.ToString());
                QueryBuilderObject.SetField("TaxNumber", "'" + TRNO + "'");
                QueryBuilderObject.SetField("BillsOpenNumber", B2B_Inv);
                QueryBuilderObject.SetField("CustomerTypeID", CustType);
                QueryBuilderObject.SetField("CurrencyID", "1");
                QueryBuilderObject.SetField("OnHold", OnHold);
                QueryBuilderObject.SetField("GPSLatitude", Longitude);
                QueryBuilderObject.SetField("GPSLongitude", latitude);
                QueryBuilderObject.SetField("StreetAddress", "0");
                QueryBuilderObject.SetField("Inactive", inactive);
                QueryBuilderObject.SetField("Notes", "0");
                QueryBuilderObject.SetField("SkipCreditCheck", "0");
                QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                err = QueryBuilderObject.InsertQueryString("CustomerOutlet", db_vms);
            }

            //InCubeQuery DeleteCustomerGroup = new InCubeQuery(db_vms, "Delete From CustomerOutletGroup Where CustomerID = " + CustomerID + " AND OutletID = " + OutletID);
            //err=DeleteCustomerGroup.ExecuteNonQuery();

            err = ExistObject("CustomerOutletGroup", "GroupID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " and GroupID=" + GroupID.Trim() + "", db_vms);
            if (err != InCubeErrors.Success)
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                err = QueryBuilderObject.InsertQueryString("CustomerOutletGroup", db_vms);
            }
            if (!GroupID2.Trim().Equals(string.Empty))
            {
                err = ExistObject("CustomerOutletGroup", "GroupID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " and GroupID=" + GroupID2.Trim() + "", db_vms);
                if (err != InCubeErrors.Success)
                {
                    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                    QueryBuilderObject.SetField("OutletID", OutletID);
                    QueryBuilderObject.SetField("GroupID", GroupID2.ToString());
                    err = QueryBuilderObject.InsertQueryString("CustomerOutletGroup", db_vms);
                }
            }
            //if (!PriceListCode.Trim().Equals(string.Empty) && !PriceListCode.Trim().ToLower().Equals("ge001"))
            //{
            //    string PriceListID = GetFieldValue("PriceList", "PriceListID", "PriceListCode='" + PriceListCode + "'", db_vms).Trim();
            //    InCubeQuery DeletePriceDefinitionQuery = new InCubeQuery(db_vms, "Delete From CustomerPrice where CustomerID=" + CustomerID + " and OutletID=" + OutletID + "");
            //    DeletePriceDefinitionQuery.ExecuteNonQuery();
            //    //string existCustomerPrice = GetFieldValue("CustomerPrice", "PriceListID", "CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and priceListID=" + PriceListID + "", db_vms).Trim();
            //    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
            //    QueryBuilderObject.SetField("OutletID", OutletID);
            //    QueryBuilderObject.SetField("PriceListID", PriceListID);
            //    err = QueryBuilderObject.InsertQueryString("CustomerPrice", db_vms);
            //}
            ////else if (err == InCubeErrors.Success)
            //{

            //    QueryBuilderObject.SetField("GroupID", GroupID.ToString());
            //    QueryBuilderObject.UpdateQueryString("CustomerOutletGroup","CustomerID="+CustomerID.ToString()+" and OutletID="+OutletID+"", db_vms);
            //}
            string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode='" + divisionCode + "'", db_vms).Trim();
            string CustOutDiv = GetFieldValue("CustomerOutletDivision", "customerID", "customerID=" + CustomerID.ToString() + " and OutletID=" + OutletID + " and DivisionID=" + DivisionID + "", db_vms).Trim();
            if (CustOutDiv.Equals(string.Empty))
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID.ToString());
                QueryBuilderObject.SetField("DivisionID", DivisionID.ToString().Trim());
                err = QueryBuilderObject.InsertQueryString("CustomerOutletDivision", db_vms);
                //string CustDivi = string.Format("insert into CustomerOutletDivision values({0},{1},{2})", CustomerID, OutletID, divisionID);
                //qry = new InCubeQuery(CustDivi,db_vms);
                //err = qry.ExecuteNonQuery();
            }
            //string ExistRouteCustomer = GetFieldValue("RouteCustomer", "CustomerID=" + CustomerID.ToString() + " and OutletID=" + OutletID + "", db_vms);
            //if (ExistRouteCustomer == stri
            //}
            ExistCustomer = GetFieldValue("CustomerOutletLanguage", "OutletID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " AND LanguageID = 1", db_vms);
            if (ExistCustomer != string.Empty)
            {
                QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish + "'");
                QueryBuilderObject.SetField("Address", "'" + CustomerAddressEnglish + "'");
                err = QueryBuilderObject.UpdateQueryString("CustomerOutletLanguage", "  CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " AND LanguageID = 1", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish + "'");
                QueryBuilderObject.SetField("Address", "'" + CustomerAddressEnglish + "'");
                err = QueryBuilderObject.InsertQueryString("CustomerOutletLanguage", db_vms);
            }

            ExistCustomer = GetFieldValue("CustomerOutletLanguage", "OutletID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " AND LanguageID = 2", db_vms); //Arabic
            if (ExistCustomer != string.Empty)
            {
                QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionArabic + "'");
                QueryBuilderObject.SetField("Address", "N'" + CustomerAddressArabic + "'");
                QueryBuilderObject.UpdateQueryString("CustomerOutletLanguage", "  CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " AND LanguageID = 2", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionArabic + "'");
                QueryBuilderObject.SetField("Address", "N'" + CustomerAddressArabic + "'");
                err = QueryBuilderObject.InsertQueryString("CustomerOutletLanguage", db_vms);
            }

            #region Territory


            //string deleteFromRoutecustomer = string.Format("delete from RouteCustomer where customerID={0} and OutletID={1}", CustomerID, OutletID);
            //qry = new InCubeQuery(deleteFromRoutecustomer, db_vms);
            //err = qry.ExecuteNonQuery();

            //string deleteFromRoutecustomer = string.Format("delete from CustOutTerritory where customerID={0} and OutletID={1}", CustomerID, OutletID);
            //qry = new InCubeQuery(deleteFromRoutecustomer, db_vms);
            //err = qry.ExecuteNonQuery();


            //string TerritoryID = GetFieldValue("Territory", "TerritoryID", "TerritoryCode='" + SALESMAN_CODE + "'", db_vms);
            //if (TerritoryID == string.Empty)
            //{
            //    TerritoryID = GetFieldValue("[Territory]", "isnull(max(TerritoryID),0)+1", db_vms);
            //    QueryBuilderObject.SetField("TerritoryID", TerritoryID);
            //    QueryBuilderObject.SetField("OrganizationID", "1");
            //    QueryBuilderObject.SetField("TerritoryCode", "'" + SALESMAN_CODE + "'");
            //    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
            //    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
            //    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
            //    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
            //    err = QueryBuilderObject.InsertQueryString("Territory", db_vms);

            //    QueryBuilderObject.SetField("TerritoryID", TerritoryID);
            //    QueryBuilderObject.SetField("LanguageID", "1");
            //    QueryBuilderObject.SetField("Description", "'" + SALESMAN_NAME + "'");
            //    QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);

            //    QueryBuilderObject.SetField("TerritoryID", TerritoryID);
            //    QueryBuilderObject.SetField("LanguageID", "2");
            //    QueryBuilderObject.SetField("Description", "'" + SALESMAN_NAME + "'");
            //    err = QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);

            //}
            //else
            //{
            //    QueryBuilderObject.SetField("Description", "'" + SALESMAN_NAME + "'");
            //    err = QueryBuilderObject.UpdateQueryString("TerritoryLanguage", "TerritoryID=" + TerritoryID + "", db_vms);

            //}
            //string deleteCustTerr = "delete from CustOutTerritory where customerid=" + CustomerID + " AND OutletID = " + OutletID + "";
            //qry = new InCubeQuery(deleteCustTerr, db_vms);
            //err = qry.ExecuteNonQuery();

            //err = ExistObject("CustOutTerritory", "TerritoryID", "TerritoryID = " + TerritoryID + " AND CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
            //if (err != InCubeErrors.Success)
            //{

            //    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
            //    QueryBuilderObject.SetField("OutletID", OutletID);
            //    QueryBuilderObject.SetField("TerritoryID", TerritoryID);
            //    err = QueryBuilderObject.InsertQueryString("CustOutTerritory", db_vms);
            //    string getNewEmployee = GetFieldValue("EmployeeTerritory", "EmployeeID", "TerritoryID=" + TerritoryID + "", db_vms).Trim();
            //    if (!getNewEmployee.Equals(string.Empty))
            //    {
            //        QueryBuilderObject.SetField("EmployeeID", getNewEmployee);
            //        err = QueryBuilderObject.UpdateQueryString("[Transaction]", "CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and RemainingAmount>0 ", db_vms);


            //    }
            //}

            //string RouteID = GetFieldValue("[Route]", "RouteID", "RouteCode='" + SALESMAN_CODE + "'", db_vms);
            //if (RouteID == string.Empty)
            //{
            //    RouteID = GetFieldValue("[Route]", "isnull(max(RouteID),0)+1", db_vms);
            //    QueryBuilderObject.SetField("RouteID", RouteID);
            //    QueryBuilderObject.SetField("Inactive", "0");
            //    QueryBuilderObject.SetField("TerritoryID", TerritoryID);
            //    QueryBuilderObject.SetField("RouteCode", "'" + SALESMAN_CODE + "'");
            //    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
            //    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
            //    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
            //    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
            //    err = QueryBuilderObject.InsertQueryString("[Route]", db_vms);

            //    QueryBuilderObject.SetField("RouteID", RouteID);
            //    QueryBuilderObject.SetField("LanguageID", "1");
            //    QueryBuilderObject.SetField("Description", "'" + SALESMAN_NAME + "'");
            //    QueryBuilderObject.InsertQueryString("RouteLanguage", db_vms);

            //    QueryBuilderObject.SetField("RouteID", RouteID);
            //    QueryBuilderObject.SetField("Week", "0");
            //    QueryBuilderObject.SetField("Sunday", "1");
            //    QueryBuilderObject.SetField("Monday", "1");
            //    QueryBuilderObject.SetField("Tuesday", "1");
            //    QueryBuilderObject.SetField("Wednesday", "1");
            //    QueryBuilderObject.SetField("Thursday", "1");
            //    QueryBuilderObject.SetField("Friday", "1");
            //    QueryBuilderObject.SetField("Saturday", "1");
            //    QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
            //}
            //else
            //{
            //    QueryBuilderObject.SetField("Description", "'" + SALESMAN_CODE + "'");
            //    err = QueryBuilderObject.UpdateQueryString("RouteLanguage", "RouteID=" + RouteID + "", db_vms);

            //}
            //deleteCustTerr = "delete from RouteCustomer where customerid=" + CustomerID + " AND OutletID = " + OutletID + "";
            //qry = new InCubeQuery(deleteCustTerr, db_vms);
            //err = qry.ExecuteNonQuery();

            //err = ExistObject("RouteCustomer", "RouteID", "RouteID = " + RouteID + " AND CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
            //if (err != InCubeErrors.Success)
            //{

            //    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
            //    QueryBuilderObject.SetField("OutletID", OutletID);
            //    QueryBuilderObject.SetField("RouteID", RouteID);
            //    err = QueryBuilderObject.InsertQueryString("RouteCustomer", db_vms);
            //}

            #endregion

            #region Customer Outlet Account
            int AccountID = 1;

            ExistCustomer = GetFieldValue("AccountCustOut", "AccountID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
            if (ExistCustomer == string.Empty)
            {
                AccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("AccountTypeID", "1");
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.SetField("Balance", Balance);
                QueryBuilderObject.SetField("GL", "0");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID.ToString());
                QueryBuilderObject.SetField("CurrencyID", "1");
                QueryBuilderObject.SetField("ParentAccountID", parentAccount);
                err = QueryBuilderObject.InsertQueryString("Account", db_vms);

                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                err = QueryBuilderObject.InsertQueryString("AccountCustOut", db_vms);

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish.Trim() + " Account'");
                err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionArabic.Trim() + " Account'");
                err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
            }
            else
            {
                AccountID = int.Parse(ExistCustomer);
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                //QueryBuilderObject.SetField("Balance", Balance);
                err = QueryBuilderObject.UpdateQueryString("Account", "AccountID=" + ExistCustomer + "", db_vms);
            }

            string Parent2 = AccountID.ToString();
            ExistCustomer = GetFieldValue("AccountCustOutDiv", "AccountID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " and DivisionID=" + DivisionID + "", db_vms);
            if (ExistCustomer == string.Empty)
            {
                AccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("AccountTypeID", "1");
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.SetField("Balance", Balance);
                QueryBuilderObject.SetField("GL", "0");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID.ToString());
                QueryBuilderObject.SetField("ParentAccountID", Parent2);
                QueryBuilderObject.SetField("CurrencyID", "1");
                err = QueryBuilderObject.InsertQueryString("Account", db_vms);

                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("DivisionID", DivisionID);
                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                err = QueryBuilderObject.InsertQueryString("AccountCustOutDiv", db_vms);

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish.Trim() + " Account'");
                err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionArabic.Trim() + " Account'");
                err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
            }
            else
            {
                AccountID = int.Parse(ExistCustomer);
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                //QueryBuilderObject.SetField("Balance", Balance);
                err = QueryBuilderObject.UpdateQueryString("Account", "AccountID=" + ExistCustomer + "", db_vms);
            }
            string custCode = CustomerCode;
            string Region = state;
            string StateDescription = state;
            string AreaLocation = city;
            string streetDescription = city;
            string StateID = GetFieldValue("StateLanguage", "StateID", "Description='" + Region.Replace("'", "''") + "' and LanguageID=1", db_vms).Trim();
            if (StateID.Equals(string.Empty))
            {
                StateID = GetFieldValue("State", "isnull(max(StateID),0)+1", db_vms).Trim();
                QueryBuilderObject.SetField("CountryID", "1");
                QueryBuilderObject.SetField("StateID", StateID);
                err = QueryBuilderObject.InsertQueryString("State", db_vms);
                QueryBuilderObject.SetField("CountryID", "1");
                QueryBuilderObject.SetField("StateID", StateID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + Region + "'");
                err = QueryBuilderObject.InsertQueryString("StateLanguage", db_vms);
                QueryBuilderObject.SetField("CountryID", "1");
                QueryBuilderObject.SetField("StateID", StateID);
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "'" + Region.Replace("'", "''") + "'");
                err = QueryBuilderObject.InsertQueryString("StateLanguage", db_vms);
            }
            string CityID = GetFieldValue("CityLanguage", "CityID", "Description='" + StateDescription.Replace("'", "''") + "' and LanguageID=1", db_vms).Trim();
            if (CityID.Equals(string.Empty))
            {
                CityID = GetFieldValue("City", "isnull(max(CityID),0)+1", db_vms).Trim();
                QueryBuilderObject.SetField("CountryID", "1");
                QueryBuilderObject.SetField("StateID", StateID);
                QueryBuilderObject.SetField("CityID", CityID);
                err = QueryBuilderObject.InsertQueryString("City", db_vms);
                QueryBuilderObject.SetField("CountryID", "1");
                QueryBuilderObject.SetField("StateID", StateID);
                QueryBuilderObject.SetField("CityID", CityID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + StateDescription.Replace("'", "''") + "'");
                err = QueryBuilderObject.InsertQueryString("CityLanguage", db_vms);
                QueryBuilderObject.SetField("CountryID", "1");
                QueryBuilderObject.SetField("StateID", StateID);
                QueryBuilderObject.SetField("CityID", CityID);
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "'" + StateDescription + "'");
                err = QueryBuilderObject.InsertQueryString("CityLanguage", db_vms);
            }
            string AreaID = GetFieldValue("AreaLanguage", "AreaID", "Description='" + AreaLocation + "' and LanguageID=1", db_vms).Trim();
            if (AreaID.Equals(string.Empty))
            {
                AreaID = GetFieldValue("Area", "isnull(max(AreaID),0)+1", db_vms).Trim();
                QueryBuilderObject.SetField("CountryID", "1");
                QueryBuilderObject.SetField("StateID", StateID);
                QueryBuilderObject.SetField("CityID", CityID);
                QueryBuilderObject.SetField("AreaID", AreaID);
                err = QueryBuilderObject.InsertQueryString("Area", db_vms);
                QueryBuilderObject.SetField("CountryID", "1");
                QueryBuilderObject.SetField("StateID", StateID);
                QueryBuilderObject.SetField("CityID", CityID);
                QueryBuilderObject.SetField("AreaID", AreaID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + AreaLocation + "'");
                err = QueryBuilderObject.InsertQueryString("AreaLanguage", db_vms);
                QueryBuilderObject.SetField("CountryID", "1");
                QueryBuilderObject.SetField("StateID", StateID);
                QueryBuilderObject.SetField("CityID", CityID);
                QueryBuilderObject.SetField("AreaID", AreaID);
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "'" + AreaLocation + "'");
                err = QueryBuilderObject.InsertQueryString("AreaLanguage", db_vms);
            }
            string StreetID = GetFieldValue("StreetLanguage", "StreetID", "Description='" + streetDescription + "' and LanguageID=1", db_vms).Trim();
            if (StreetID.Equals(string.Empty))
            {
                StreetID = GetFieldValue("Street", "isnull(max(StreetID),0)+1", db_vms).Trim();
                QueryBuilderObject.SetField("CountryID", "1");
                QueryBuilderObject.SetField("StateID", StateID);
                QueryBuilderObject.SetField("CityID", CityID);
                QueryBuilderObject.SetField("AreaID", AreaID);
                QueryBuilderObject.SetField("StreetID", StreetID);
                err = QueryBuilderObject.InsertQueryString("Street", db_vms);
                QueryBuilderObject.SetField("CountryID", "1");
                QueryBuilderObject.SetField("StateID", StateID);
                QueryBuilderObject.SetField("CityID", CityID);
                QueryBuilderObject.SetField("AreaID", AreaID);
                QueryBuilderObject.SetField("StreetID", StreetID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + AreaLocation + "'");
                err = QueryBuilderObject.InsertQueryString("StreetLanguage", db_vms);
                QueryBuilderObject.SetField("CountryID", "1");
                QueryBuilderObject.SetField("StateID", StateID);
                QueryBuilderObject.SetField("CityID", CityID);
                QueryBuilderObject.SetField("AreaID", AreaID);
                QueryBuilderObject.SetField("StreetID", StreetID);
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "'" + AreaLocation + "'");
                err = QueryBuilderObject.InsertQueryString("StreetLanguage", db_vms);
            }
            if (!CustomerID.Equals(string.Empty) && !OutletID.Equals(string.Empty))
            {
                QueryBuilderObject.SetField("StreetID", StreetID);
                err = QueryBuilderObject.UpdateQueryString("CustomerOutlet", "CustomerID=" + CustomerID + " and OutletID=" + OutletID + "", db_vms);
            }
            //if (!VEHICLECODE.Trim().Equals(string.Empty)) AddUpdateWarehouse("2", VEHICLECODE, VEHICLECODE, ref totalu, ref totali, "1");
            //if (!SALESMAN_CODE.Trim().Equals(string.Empty)) AddUpdateSalesperson("2", SALESMAN_CODE, SALESMAN_NAME, ref totalu, ref totali, DivisionID, "1");
            //if (!superVisorCode.Trim().Equals(string.Empty)) AddUpdateSalesperson("4", superVisorCode, superVisorName, ref totalu, ref totali, DivisionID, "1");
            //if (!SalesManagerCode.Trim().Equals(string.Empty)) AddUpdateSalesperson("9", SalesManagerCode, SalesManagerName, ref totalu, ref totali, DivisionID, "1");
            if (err == InCubeErrors.Success)
            {

                UpdateFlag("CustomersEmployees", "CustomerOutletCode='" + CustomerCode.Replace("'", "''") + "'");

            }

            #endregion


            #endregion
        }

        #endregion

        #region Update Geographical Location
        public override void UpdateGeographicalLocation()
        {
            try
            {
                DataTable DT = new DataTable();
                string select = string.Format("select * from Customer");
                qry = new InCubeQuery(select, db_ERP);
                err = qry.Execute();
                DT = qry.GetDataTable();
                int TOTALUPDATE = 0;
                int TOTALINSERT = 0;
                ClearProgress();
                SetProgressMax(DT.Rows.Count);
                foreach (DataRow dr in DT.Rows)
                {
                    ReportProgress("Updating Locations");

                    string custCode = dr["CustomerOutletCode"].ToString().Trim();
                    string Region = dr["State"].ToString().Trim();
                    string StateDescription = dr["City"].ToString().Trim();
                    string AreaLocation = StateDescription + " " + dr["Area"].ToString().Trim();
                    string streetDescription = StateDescription + " " + dr["Street"].ToString().Trim();
                    string StateID = GetFieldValue("StateLanguage", "StateID", "Description='" + Region + "' and LanguageID=1", db_vms).Trim();
                    if (StateID.Equals(string.Empty))
                    {
                        StateID = GetFieldValue("State", "isnull(max(StateID),0)+1", db_vms).Trim();
                        QueryBuilderObject.SetField("CountryID", "1");
                        QueryBuilderObject.SetField("StateID", StateID);
                        err = QueryBuilderObject.InsertQueryString("State", db_vms);
                        QueryBuilderObject.SetField("CountryID", "1");
                        QueryBuilderObject.SetField("StateID", StateID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + Region + "'");
                        err = QueryBuilderObject.InsertQueryString("StateLanguage", db_vms);
                        QueryBuilderObject.SetField("CountryID", "1");
                        QueryBuilderObject.SetField("StateID", StateID);
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "'" + Region + "'");
                        err = QueryBuilderObject.InsertQueryString("StateLanguage", db_vms);
                    }
                    string CityID = GetFieldValue("CityLanguage", "CityID", "Description='" + StateDescription + "' and LanguageID=1", db_vms).Trim();
                    if (CityID.Equals(string.Empty))
                    {
                        CityID = GetFieldValue("City", "isnull(max(CityID),0)+1", db_vms).Trim();
                        QueryBuilderObject.SetField("CountryID", "1");
                        QueryBuilderObject.SetField("StateID", StateID);
                        QueryBuilderObject.SetField("CityID", CityID);
                        err = QueryBuilderObject.InsertQueryString("City", db_vms);
                        QueryBuilderObject.SetField("CountryID", "1");
                        QueryBuilderObject.SetField("StateID", StateID);
                        QueryBuilderObject.SetField("CityID", CityID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + StateDescription + "'");
                        err = QueryBuilderObject.InsertQueryString("CityLanguage", db_vms);
                        QueryBuilderObject.SetField("CountryID", "1");
                        QueryBuilderObject.SetField("StateID", StateID);
                        QueryBuilderObject.SetField("CityID", CityID);
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "'" + StateDescription + "'");
                        err = QueryBuilderObject.InsertQueryString("CityLanguage", db_vms);
                    }
                    string AreaID = GetFieldValue("AreaLanguage", "AreaID", "Description='" + AreaLocation + "' and LanguageID=1", db_vms).Trim();
                    if (AreaID.Equals(string.Empty))
                    {
                        AreaID = GetFieldValue("Area", "isnull(max(AreaID),0)+1", db_vms).Trim();
                        QueryBuilderObject.SetField("CountryID", "1");
                        QueryBuilderObject.SetField("StateID", StateID);
                        QueryBuilderObject.SetField("CityID", CityID);
                        QueryBuilderObject.SetField("AreaID", AreaID);
                        err = QueryBuilderObject.InsertQueryString("Area", db_vms);
                        QueryBuilderObject.SetField("CountryID", "1");
                        QueryBuilderObject.SetField("StateID", StateID);
                        QueryBuilderObject.SetField("CityID", CityID);
                        QueryBuilderObject.SetField("AreaID", AreaID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + AreaLocation + "'");
                        err = QueryBuilderObject.InsertQueryString("AreaLanguage", db_vms);
                        QueryBuilderObject.SetField("CountryID", "1");
                        QueryBuilderObject.SetField("StateID", StateID);
                        QueryBuilderObject.SetField("CityID", CityID);
                        QueryBuilderObject.SetField("AreaID", AreaID);
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "'" + AreaLocation + "'");
                        err = QueryBuilderObject.InsertQueryString("AreaLanguage", db_vms);
                    }
                    string StreetID = GetFieldValue("StreetLanguage", "StreetID", "Description='" + streetDescription + "' and LanguageID=1", db_vms).Trim();
                    if (StreetID.Equals(string.Empty))
                    {
                        StreetID = GetFieldValue("Street", "isnull(max(StreetID),0)+1", db_vms).Trim();
                        QueryBuilderObject.SetField("CountryID", "1");
                        QueryBuilderObject.SetField("StateID", StateID);
                        QueryBuilderObject.SetField("CityID", CityID);
                        QueryBuilderObject.SetField("AreaID", AreaID);
                        QueryBuilderObject.SetField("StreetID", StreetID);
                        err = QueryBuilderObject.InsertQueryString("Street", db_vms);
                        QueryBuilderObject.SetField("CountryID", "1");
                        QueryBuilderObject.SetField("StateID", StateID);
                        QueryBuilderObject.SetField("CityID", CityID);
                        QueryBuilderObject.SetField("AreaID", AreaID);
                        QueryBuilderObject.SetField("StreetID", StreetID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + streetDescription + "'");
                        err = QueryBuilderObject.InsertQueryString("StreetLanguage", db_vms);
                        QueryBuilderObject.SetField("CountryID", "1");
                        QueryBuilderObject.SetField("StateID", StateID);
                        QueryBuilderObject.SetField("CityID", CityID);
                        QueryBuilderObject.SetField("AreaID", AreaID);
                        QueryBuilderObject.SetField("StreetID", StreetID);
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "'" + streetDescription + "'");
                        err = QueryBuilderObject.InsertQueryString("StreetLanguage", db_vms);
                    }
                    string CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode='" + custCode.Replace("'", "''") + "'", db_vms).Trim();
                    string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerCode='" + custCode.Replace("'", "''") + "'", db_vms).Trim();
                    if (CustomerID.Equals(string.Empty) || OutletID.Equals(string.Empty)) continue;
                    QueryBuilderObject.SetField("StreetID", StreetID);
                    err = QueryBuilderObject.UpdateQueryString("CustomerOutlet", "CustomerID=" + CustomerID + " and OutletID=" + OutletID + "", db_vms);
                }
                DT.Dispose();
                WriteMessage("\r\n");
                WriteMessage("<<< Geo Location >>> Total Updated = " + TOTALUPDATE + " , Total Inserted = " + TOTALINSERT);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        #endregion

        #region UpdateVehicles
        public override void UpdateWarehouse()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            object field = new object();

            
            DataTable DT = new DataTable();
            qry = new InCubeQuery("select LoadingWarehouse from Wh_MAster GROUP BY MAIN_WH_CODE", db_ERP_GET);
            err = qry.Execute();
            DT = qry.GetDataTable();

            ClearProgress();
            SetProgressMax(DT.Rows.Count);

            foreach (DataRow row in DT.Rows)
            {
                ReportProgress("Updating Vehicles");

                //Warehouse Code	
                //Description	
                //Plate number	
                //Type 
                //Salesperson code

                string WarehouseCode = row["LoadingWarehouse"].ToString().Trim();
                string Barcode = row["LoadingWarehouse"].ToString().Trim();
                string VehicleCode = string.Empty; //row["RouteCode"].ToString().Trim();
                string WarehouceName = row["LoadingWarehouse"].ToString().Trim();
                string VehicleName = string.Empty;// row["RouteCode"].ToString().Trim();
                string VehicleRegNum = string.Empty;// row["PlateNumber"].ToString().Trim();
                string WarehouseType = string.Empty;// row["Type"].ToString().Trim();
                string SalesmanCode = string.Empty;// row["SalesManCode"].ToString().Trim();


                if (WarehouseCode == string.Empty)
                    continue;

                string Address = "";
                string WarehouseID = "";

                WarehouseID = GetFieldValue("Warehouse", "WarehouseID", "Barcode='" + WarehouseCode + "'", db_vms);
                if (WarehouseID == string.Empty)
                {
                    WarehouseID = GetFieldValue("Warehouse", "isnull(MAX(WarehouseID),0) + 1", db_vms);
                }

                AddUpdateWarehouse(WarehouseCode, WarehouseID, WarehouseCode, WarehouceName, Address, VehicleRegNum, SalesmanCode, ref TOTALUPDATED, ref TOTALINSERTED, "1", Barcode);
                //UpdateFlag("IN_WH_VH", "vehiclecode='" + WarehouseCode + "'");
            }
            DT = new DataTable();
            qry = new InCubeQuery("select VEHICLECODE,VEHICLECODE,LoadingWarehouse from Wh_MAster GROUP BY VEHICLECODE,VEHICLECODE,LoadingWarehouse", db_ERP_GET);
            err = qry.Execute();
            DT = qry.GetDataTable();

            ClearProgress();
            SetProgressMax(DT.Rows.Count);

            foreach (DataRow row in DT.Rows)
            {
                ReportProgress("Updating Vehicles");

                //Warehouse Code	
                //Description	
                //Plate number	
                //Type 
                //Salesperson code

                string WarehouseCode = row["VEHICLECODE"].ToString().Trim();
                string VehicleCode = string.Empty; //row["RouteCode"].ToString().Trim();
                string WarehouceName = row["VEHICLECODE"].ToString().Trim();
                string refNo = row["LoadingWarehouse"].ToString().Trim();
                string VehicleName = string.Empty;// row["RouteCode"].ToString().Trim();
                string VehicleRegNum = string.Empty;// row["PlateNumber"].ToString().Trim();
                string WarehouseType = string.Empty;// row["Type"].ToString().Trim();
                string SalesmanCode = string.Empty;// row["SalesManCode"].ToString().Trim();


                if (WarehouseCode == string.Empty)
                    continue;

                string Address = "";
                string WarehouseID = "";
                string loadingWH = GetFieldValue("Warehouse", "WarehouseID", "WarehouseCode='" + refNo + "'", db_vms);
                WarehouseID = GetFieldValue("Warehouse", "WarehouseID", "Barcode='" + WarehouseCode + "'", db_vms);
                if (WarehouseID == string.Empty)
                {
                    WarehouseID = GetFieldValue("Warehouse", "isnull(MAX(WarehouseID),0) + 1", db_vms);
                }

                AddUpdateWarehouse(loadingWH, WarehouseID, WarehouseCode, WarehouceName, Address, VehicleRegNum, SalesmanCode, ref TOTALUPDATED, ref TOTALINSERTED, "2", refNo);
                //UpdateFlag("IN_WH_VH", "vehiclecode='" + WarehouseCode + "'");
            }
            DT.Dispose();

            WriteMessage("\r\n");
            WriteMessage("<<< WAREHOUSE >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        private void AddUpdateWarehouse(string loadingWH, string WarehouseID, string WarehouseCode, string WarehouceName, string Address, string VehicleRegNum, string SalesmanCode, ref int TOTALUPDATED, ref int TOTALINSERTED, string WarehouseType, string code2)
        {


            string ExitWarehouse = "";

            ExitWarehouse = GetFieldValue("Warehouse", "WarehouseID", "WarehouseID = " + WarehouseID, db_vms);
            if (ExitWarehouse != string.Empty) // Exist Warehouse --- Update Query
            {
                TOTALUPDATED++;

                QueryBuilderObject.SetField("Phone", "''");
                QueryBuilderObject.SetField("Barcode", "'" + WarehouseCode + "'");
                if (WarehouseType.Trim().Equals("2")) { QueryBuilderObject.SetField("WarehouseCode", "'" + WarehouseCode + "'"); } else { QueryBuilderObject.SetField("WarehouseCode", "'" + code2 + "'"); }
                QueryBuilderObject.SetField("OrganizationID", "1");
                QueryBuilderObject.SetField("WarehouseTypeID", WarehouseType);
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                err = QueryBuilderObject.UpdateQueryString("Warehouse", " WarehouseID = " + WarehouseID, db_vms);

            }
            else  // New Warehouse --- Insert Query
            {
                TOTALINSERTED++;

                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("Phone", "''");
                QueryBuilderObject.SetField("Barcode", "'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("OrganizationID", "1");
                QueryBuilderObject.SetField("WarehouseTypeID", WarehouseType);
                if (WarehouseType.Trim().Equals("2")) { QueryBuilderObject.SetField("WarehouseCode", "'" + WarehouseCode + "'"); } else { QueryBuilderObject.SetField("WarehouseCode", "'" + code2 + "'"); }
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                err = QueryBuilderObject.InsertQueryString("Warehouse", db_vms);
                //if (err == InCubeErrors.Success) UpdateFlag("glumast", "locode='" + WarehouseCode + "'");
            }

            ExitWarehouse = GetFieldValue("WarehouseLanguage", "WarehouseID", "WarehouseID = " + WarehouseID + " AND LanguageID = 1", db_vms);
            if (ExitWarehouse != string.Empty)
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
                err = QueryBuilderObject.InsertQueryString("WarehouseLanguage", db_vms);
            }

            ExitWarehouse = GetFieldValue("WarehouseLanguage", "WarehouseID", "WarehouseID = " + WarehouseID + " AND LanguageID = 2", db_vms); //Arabic
            if (ExitWarehouse != string.Empty)
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

            #region WarehouseZone/Vehicle/VehicleSalesPerson

            ExitWarehouse = GetFieldValue("WarehouseZone", "WarehouseID", "WarehouseID = " + WarehouseID, db_vms);
            if (ExitWarehouse == string.Empty)
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("ZoneID", "1");
                err = QueryBuilderObject.InsertQueryString("WarehouseZone", db_vms);
            }

            ExitWarehouse = GetFieldValue("WarehouseZoneLanguage", "WarehouseID", "WarehouseID = " + WarehouseID + " AND LanguageID = 1", db_vms);
            if (ExitWarehouse == string.Empty)
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("ZoneID", "1");
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + WarehouceName + " Zone'");
                err = QueryBuilderObject.InsertQueryString("WarehouseZoneLanguage", db_vms);
            }

            ExitWarehouse = GetFieldValue("WarehouseZoneLanguage", "WarehouseID", "WarehouseID = " + WarehouseID + " AND LanguageID = 2", db_vms); //Arabic
            if (ExitWarehouse == string.Empty)
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("ZoneID", "1");
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + WarehouceName + " Zone'");
                err = QueryBuilderObject.InsertQueryString("WarehouseZoneLanguage", db_vms);
            }
            if (WarehouseType == "2")
            {
                ExitWarehouse = GetFieldValue("Vehicle", "VehicleID", "VehicleID = " + WarehouseID, db_vms);
                if (ExitWarehouse == string.Empty)
                {
                    QueryBuilderObject.SetField("VehicleID", WarehouseID);
                    QueryBuilderObject.SetField("PlateNO", "'" + VehicleRegNum + "'");
                    QueryBuilderObject.SetField("TypeID", "1");

                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    err = QueryBuilderObject.InsertQueryString("Vehicle", db_vms);
                }

                string SalesPersonID = GetFieldValue("Employee", "EmployeeID", " EmployeeCode='" + WarehouseCode.Trim() + "'", db_vms);

                if (SalesPersonID == string.Empty)
                {
                    WriteMessage("\r\n");
                    WriteMessage("Warning Vehicle Code : (" + WarehouseCode + ") is not assigned to any salesperson");
                    WriteMessage("\r\n");
                }

                string VehicleID = GetFieldValue("Vehicle", "VehicleID", "VehicleID=" + WarehouseID, db_vms);

                if (!SalesPersonID.Trim().Equals(string.Empty) && !VehicleID.Trim().Equals(string.Empty))
                {
                    ExitWarehouse = GetFieldValue("EmployeeVehicle", "VehicleID", "EmployeeID = " + SalesPersonID, db_vms);
                    if (ExitWarehouse == string.Empty)
                    {
                        QueryBuilderObject.SetField("VehicleID", VehicleID);
                        QueryBuilderObject.SetField("EmployeeID", SalesPersonID);
                        err = QueryBuilderObject.InsertQueryString("EmployeeVehicle", db_vms);
                    }
                    else
                    {
                        WriteMessage("\r\n");
                        WriteMessage("Warning Salesperson Code : (" + SalesmanCode + ") is assigned to 2 vehicles , Second Vehicle Code : (" + WarehouseCode + ") this row is skipped");
                        WriteMessage("\r\n");
                    }
                }
                string VehicleWH = GetFieldValue("VehicleLoadingWh", "vehicleID", "vehicleID=" + VehicleID + " and WarehouseID=" + loadingWH + "", db_ERP_GET);
                if (VehicleWH == string.Empty)
                {
                    QueryBuilderObject.SetField("VehicleID", VehicleID);
                    QueryBuilderObject.SetField("WarehouseID", loadingWH);
                    err = QueryBuilderObject.InsertQueryString("VehicleLoadingWh", db_vms);
                }
            }
            //if (err == InCubeErrors.Success)
            //{ 
            //err=UpdateFlag("stfoot",""
            //}
            #endregion
        }

        #endregion

        #region UpdateSalesPerson

        public override void UpdateSalesPerson()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            UpdateBanks();

            object field = new object();

            DataTable DT = new DataTable();
            qry = new InCubeQuery(@"select W.SalesManCode,C.SalesmanName,W.VehicleCode,C.DivisionCode from CustomerMaster C inner join VehicleMaster W on C.SalesManCode=W.SalesManCode
group by W.SalesManCode,C.SalesmanName,W.VehicleCode,C.DivisionCode", db_ERP_GET);
            err = qry.Execute();
            DT = qry.GetDataTable();
            ClearProgress();
            SetProgressMax(DT.Rows.Count);
            foreach (DataRow row in DT.Rows)
            {
                try
                {
                    ReportProgress("Updating Salespersons");

                    string vehicelCode = row["VehicleCode"].ToString().Trim();
                    string SupervisorCode = "";// row["SupervisorCode"].ToString().Trim();
                    string Supervisor = "";// row["SupervisorName"].ToString().Trim();
                    string RoutemanagerCode = "";// row["SalesManagerCode"].ToString().Trim();
                    string Routemanager = "";// row["SalesManagerName"].ToString().Trim();
                    if (Routemanager.Trim().Equals(string.Empty)) Routemanager = "0";
                    if (Supervisor.Trim().Equals(string.Empty)) Supervisor = "0";
                    string DivisionCode = row["DivisionCode"].ToString().Trim();
                    string SalesManCode = row["SalesManCode"].ToString().Trim();
                    string SalesMan = row["SalesmanName"].ToString().Trim();
                    if (SalesMan.Trim().Equals(string.Empty)) SalesMan = "0";
                    string channelDescription = "";// row["ChannelName"].ToString().Trim();
                    string CreditLimit = "1000000";
                    Dictionary<string, string> empDic = new Dictionary<string, string>();
                    string ExistEmployee = string.Empty;
                    string supervisorID = string.Empty;
                    if (!empDic.ContainsKey(SupervisorCode))
                    {
                        empDic.Add(SupervisorCode, Supervisor + "4");
                    }
                    if (!empDic.ContainsKey(RoutemanagerCode))
                    {
                        empDic.Add(RoutemanagerCode, Routemanager + "1");
                    }
                    if (!empDic.ContainsKey(SalesManCode))
                    {
                        empDic.Add(SalesManCode, SalesMan + "2");
                    }
                    // string CustomerDescription = row["CustomerGroup"].ToString().Trim();
                    string employeeID = string.Empty;
                    foreach (KeyValuePair<string, string> str in empDic)
                    {
                        if (str.Key.ToString().Trim().Equals(string.Empty)) continue;
                        employeeID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode='" + str.Key + "'", db_vms);

                        if (employeeID == string.Empty)
                        {
                            employeeID = GetFieldValue("Employee", "isnull(max(EmployeeID),0)+1", db_vms);
                            QueryBuilderObject.SetField("EmployeeID", employeeID);
                            QueryBuilderObject.SetField("EmployeeCode", "'" + str.Key + "'");
                            QueryBuilderObject.SetField("OrganizationID", "1");
                            QueryBuilderObject.SetField("Inactive", "0");
                            QueryBuilderObject.SetField("EmployeeTypeID", str.Value.Substring(str.Value.Length - 1, 1));
                            QueryBuilderObject.SetField("OnHold", "0");
                            QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                            QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                            QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                            QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                            err = QueryBuilderObject.InsertQueryString("Employee", db_vms);

                        }
                        else
                        {
                            QueryBuilderObject.SetField("EmployeeTypeID", str.Value.Substring(str.Value.Length - 1, 1));
                            QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                            QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                            err = QueryBuilderObject.UpdateQueryString("Employee", "EmployeeID=" + employeeID + "", db_vms);
                        }
                        ExistEmployee = GetFieldValue("EmployeeLanguage", "EmployeeID", "EmployeeID = " + employeeID + " AND LanguageID = 1", db_vms);
                        if (ExistEmployee != string.Empty)
                        {
                            TOTALUPDATED++;
                            QueryBuilderObject.SetField("Description", "'" + str.Value.Substring(0, str.Value.Length - 1) + "'");
                            err = QueryBuilderObject.UpdateQueryString("EmployeeLanguage", "EmployeeID = " + employeeID + " AND LanguageID = 1", db_vms);
                        }
                        else
                        {
                            QueryBuilderObject.SetField("EmployeeID", employeeID);
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + str.Value.Substring(0, str.Value.Length - 1) + "'");
                            QueryBuilderObject.SetField("Address", "''");
                            err = QueryBuilderObject.InsertQueryString("EmployeeLanguage", db_vms);
                        }

                        ExistEmployee = GetFieldValue("EmployeeLanguage", "EmployeeID", "EmployeeID = " + employeeID + " AND LanguageID = 2", db_vms);
                        if (ExistEmployee != string.Empty)
                        {
                            TOTALUPDATED++;
                            QueryBuilderObject.SetField("Description", "N'" + str.Value.Substring(0, str.Value.Length - 1) + "'");
                            err = QueryBuilderObject.UpdateQueryString("EmployeeLanguage", "EmployeeID = " + employeeID + " AND LanguageID = 2", db_vms);
                        }
                        else
                        {
                            QueryBuilderObject.SetField("EmployeeID", employeeID);
                            QueryBuilderObject.SetField("LanguageID", "2");
                            QueryBuilderObject.SetField("Description", "N'" + str.Value.Substring(0, str.Value.Length - 1) + "'");
                            QueryBuilderObject.SetField("Address", "''");
                            err = QueryBuilderObject.InsertQueryString("EmployeeLanguage", db_vms);
                        }

                        ExistEmployee = GetFieldValue("Operator", "OperatorID", "OperatorID = " + employeeID, db_vms);
                        if (ExistEmployee == string.Empty)
                        {
                            QueryBuilderObject.SetField("OperatorID", employeeID);
                            QueryBuilderObject.SetField("OperatorName", "'" + str.Key + "'");
                            //QueryBuilderObject.SetField("Status", "0");
                            QueryBuilderObject.SetField("FrontOffice", "1");
                            err = QueryBuilderObject.InsertQueryString("Operator", db_vms);
                        }

                        ExistEmployee = GetFieldValue("EmployeeOrganization", "EmployeeID", "EmployeeID = " + employeeID, db_vms);
                        if (ExistEmployee == string.Empty)
                        {
                            QueryBuilderObject.SetField("EmployeeID", employeeID);
                            QueryBuilderObject.SetField("OrganizationID", OrganizationID.ToString());
                            err = QueryBuilderObject.InsertQueryString("EmployeeOrganization", db_vms);
                        }

                        int AccountEmpID = 1;

                        ExistEmployee = GetFieldValue("AccountEmp", "AccountID", "EmployeeID = " + employeeID, db_vms);
                        if (ExistEmployee == string.Empty)
                        {
                            AccountEmpID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));

                            QueryBuilderObject.SetField("AccountID", AccountEmpID.ToString());
                            QueryBuilderObject.SetField("AccountTypeID", "2");
                            QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                            QueryBuilderObject.SetField("Balance", "0");
                            QueryBuilderObject.SetField("GL", "0");
                            QueryBuilderObject.SetField("OrganizationID", OrganizationID.ToString());
                            QueryBuilderObject.SetField("CurrencyID", "1");
                            err = QueryBuilderObject.InsertQueryString("Account", db_vms);

                            QueryBuilderObject.SetField("EmployeeID", employeeID);
                            QueryBuilderObject.SetField("AccountID", AccountEmpID.ToString());
                            err = QueryBuilderObject.InsertQueryString("AccountEmp", db_vms);

                            QueryBuilderObject.SetField("AccountID", AccountEmpID.ToString());
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + str.Value.Substring(0, str.Value.Length - 1).Trim() + " Account'");
                            err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);

                            QueryBuilderObject.SetField("AccountID", AccountEmpID.ToString());
                            QueryBuilderObject.SetField("LanguageID", "2");
                            QueryBuilderObject.SetField("Description", "N'" + str.Value.Substring(0, str.Value.Length - 1).Trim() + " Account'");
                            err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
                        }
                        else
                        {
                            QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                            QueryBuilderObject.UpdateQueryString("Account", "AccountID=" + AccountEmpID + "", db_vms);
                            QueryBuilderObject.SetField("Description", "'" + str.Value.Substring(0, str.Value.Length - 1).Trim() + " Account'");
                            err = QueryBuilderObject.UpdateQueryString("AccountLanguage", "AccountID=" + AccountEmpID + "", db_vms);
                        }

                        string ExistEmp = string.Empty;


                        if (str.Value.Substring(str.Value.Length - 1, 1).Equals("4"))
                        {
                            supervisorID = employeeID;

                        }
                        if (str.Value.Substring(str.Value.Length - 1, 1).Equals("1"))
                        {
                            string GroupID = GetFieldValue("CustomerGroupLanguage", "GroupID", " Description = '" + channelDescription + "'  AND LanguageID = 1", db_vms);
                            if (GroupID == string.Empty)
                            { continue; }
                            ExistEmp = GetFieldValue("EmployeeCustomerGroup", "GroupID", "EmployeeID=" + employeeID + " and GroupID=" + GroupID + "", db_vms);
                            if (ExistEmp == string.Empty)
                            {
                                QueryBuilderObject.SetField("EmployeeID", employeeID);
                                QueryBuilderObject.SetField("GroupID", GroupID);
                                err = QueryBuilderObject.InsertQueryString("EmployeeCustomerGroup", db_vms);
                            }
                        }
                        string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode = '" + DivisionCode + "'", db_vms);
                        if (DivisionID != string.Empty)
                        {
                            string ExistEmployeeDivision = GetFieldValue("EmployeeDivision", "employeeID", "EmployeeID=" + employeeID + " and DivisionID=" + DivisionID + "", db_vms);
                            if (ExistEmployeeDivision == string.Empty)
                            {
                                QueryBuilderObject.SetField("EmployeeID", employeeID);
                                QueryBuilderObject.SetField("DivisionID", DivisionID);
                                err = QueryBuilderObject.InsertQueryString("EmployeeDivision", db_vms);
                            }
                        }
                        string ExistsecurityGroup = GetFieldValue("OperatorSecurityGroup", "OperatorID", "OperatorID=" + employeeID + " and SecurityGroupID=2", db_vms);
                        if (ExistsecurityGroup == string.Empty)
                        {
                            QueryBuilderObject.SetField("OperatorID", employeeID);
                            QueryBuilderObject.SetField("SecurityGroupID", "2");
                            err = QueryBuilderObject.InsertQueryString("OperatorSecurityGroup", db_vms);
                        }

                        if (vehicelCode != string.Empty)
                        {
                            string VehicleID = GetFieldValue("Warehouse", "WarehouseID", "WarehouseCode='" + vehicelCode + "'", db_vms);
                            if (!VehicleID.Equals(string.Empty))
                            {
                                QueryBuilderObject.SetField("EmployeeID", employeeID);
                                QueryBuilderObject.SetField("VehicleID", VehicleID);
                                err = QueryBuilderObject.InsertQueryString("EmployeeVehicle", db_vms);
                            }
                        }

                    }










                    //    string EmployeeCode = row["EmployeeCode"].ToString().Trim();
                    //    string EmployeeNameEnglish = row["EmployeeNameEnglish"].ToString().Trim();
                    //    string EmployeeNameArabic = row["EmployeeNameArabic"].ToString().Trim();
                    //    string Phone = row["Phone"].ToString().Trim();
                    //    string CreditLimit = row["CreditLimit"].ToString().Trim();
                    //    string Balance = row["Balance"].ToString().Trim();
                    //    string DivisionCode = row["DivisionCode"].ToString().Trim();
                    //    string EmployeeType = row["EmployeeTypeCode"].ToString().Trim();

                    //    if (EmployeeCode == string.Empty)
                    //        continue;

                    //    string employeeID = "";

                    //    employeeID = GetFieldValue("Employee", "EmployeeID", "Employeecode = '" + EmployeeCode + "'", db_vms);
                    //    if (employeeID == string.Empty)
                    //    {
                    //        employeeID = GetFieldValue("Employee", "isnull(MAX(EmployeeID),0) + 1", db_vms);
                    //    }

                    //    string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode = '" + DivisionCode + "'", db_vms);

                    //    if (DivisionID == string.Empty)
                    //    {
                    //        DivisionID = "1";
                    //    }

                    //    AddUpdateSalesperson(EmployeeType, employeeID, EmployeeCode, EmployeeNameArabic, EmployeeNameEnglish, Phone, ref TOTALUPDATED, ref TOTALINSERTED, DivisionID, CreditLimit, Balance);
                    //}
                    //foreach (DataRow row in DT.Rows)
                    //{
                    //    ReportProgress();
                    //    IntegrationForm.lblProgress.Text = "Updating Salespersons" + " " + IntegrationForm.progressBar1.Value + " / " + SetProgressMax(;
                    //    

                    //    //Employee Code	
                    //    //Employee name English	
                    //    //Employee name Arabic	
                    //    //Phone	
                    //    //Credit limit	
                    //    //Balance	
                    //    //Division

                    //    string EmployeeCode = row["EmployeeCode"].ToString().Trim();

                    //    string supervisorCode = row["SupervisorCode"].ToString().Trim();
                    //    if (EmployeeCode == string.Empty)
                    //        continue;

                    //    string employeeID = "";
                    //    string supervisorID = "";
                    //    string ExistEmployee = "";
                    //    string delete = string.Format("Delete from EmployeeSupervisor");
                    //     qry = new InCubeQuery(delete, db_vms);
                    //     qry.ExecuteNonQuery();

                    //    employeeID = GetFieldValue("Employee", "EmployeeID", "Employeecode = '" + EmployeeCode + "'", db_vms);
                    //    if (employeeID == string.Empty)
                    //    {
                    //        continue;
                    //    }

                    //    supervisorID = GetFieldValue("Employee", "EmployeeID", "Employeecode = '" + supervisorCode + "'", db_vms);

                    //    if (supervisorID == string.Empty)
                    //    {
                    //        continue;
                    //    }
                    //    ExistEmployee = GetFieldValue("EmployeeSupervisor", "EmployeeID", "EmployeeID=" + employeeID + " and SupervisorID=" + supervisorID + "", db_vms);
                    //    if (ExistEmployee.Trim() != string.Empty)
                    //    {
                    //        QueryBuilderObject.SetField("EmployeeID", employeeID);
                    //        QueryBuilderObject.SetField("SupervisorID", supervisorID);
                    //        QueryBuilderObject.InsertQueryString("EmployeeSupervisor", db_vms);
                    //    }

                    //}
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                }
            }
            DT.Dispose();

            WriteMessage("\r\n");
            WriteMessage("<<< SALESPERSON >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }
        
        #endregion

        #region UpdatePrice
        public override void UpdatePrice()
        {
            if (CoreGeneral.Common.GeneralConfigurations.SiteSymbol.ToLower() == "esfnew")
                return;

            int TOTALUPDATED = 0;
            object field = new object();
            UpdatePriceList(ref TOTALUPDATED);

            qry = new InCubeQuery(db_vms, "sp_CopyDefaultPricesToRetailVan");
            err = qry.ExecuteStoredProcedure();
            WriteMessage("\r\nGetting default prices for items with no prices: " + (err == InCubeErrors.Success ? "Success" : "Failure"));

            WriteMessage("\r\n");
            WriteMessage("<<< PRICE >>> Total Updated = " + TOTALUPDATED);
        }
        private void UpdatePriceList(ref int TOTALUPDATED)
        {
            object field = new object();

            DataTable DT = new DataTable();
            try
            {

                qry = new InCubeQuery("SELECT P.* FROM Price P INNER JOIN ITEMS I ON I.ITEMCODE=P.ITEMCODE where p.Flag='Y' ", db_ERP_GET);
                err = qry.Execute();
                DT = qry.GetDataTable();
                ClearProgress();
                SetProgressMax(DT.Rows.Count);
                foreach (DataRow row in DT.Rows)
                {
                    string ItemCode = row["ItemCode"].ToString().Trim();
                    string PriceListName = row["PriceListCode"].ToString().Trim();

                    if (PriceListName.Trim().Equals(string.Empty))
                    {
                        PriceListName = "No Name";

                    }
                    ReportProgress("Updating Price (" + PriceListName + ")");
                    string UOM = row["ITEMUOM"].ToString().Trim();
                    string Price = row["Price"].ToString().Trim();
                    string IsDeafult = row["IsDefaultPrice"].ToString().Trim();
                    string CreationDate = "2015-01-01";
                    string ModDate = "2030-01-01";
                    if (!ModDate.Equals(string.Empty))
                        ModDate = ModDate.Replace("9999", DateTime.Today.Year.ToString());
                    string packQty = row["ConversionFactor"].ToString().Trim();
                    string CustomerCode = string.Empty;
                    string CustomerGroup = string.Empty;
                    CustomerCode = row["CustomerCode"].ToString().Trim();
                    CustomerGroup = row["CustomerGroupCode"].ToString().Trim();
                    if (CustomerCode.Equals("0") || CustomerCode.Equals("-1")) CustomerCode = string.Empty;
                    if (CustomerGroup.Equals("0") || CustomerGroup.Equals("-1")) CustomerGroup = string.Empty;

                    DateTime PriceCreatDate = DateTime.Parse(CreationDate).AddDays(-1);
                    DateTime PriceEndDate = DateTime.Parse(ModDate);

                    #region Customer Check

                    string CustomerID = "";

                    if (CustomerID.Equals(string.Empty))
                    {
                        CustomerID = GetFieldValue("Customer", "CustomerID", "CustomerCode = '" + CustomerCode.Replace("'", "''") + "'", db_vms);
                    }

                    if (CustomerID.Equals(string.Empty))
                    {
                        CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode = '" + CustomerCode.Replace("'", "''") + "'", db_vms);
                    }

                    if (CustomerID.Equals(string.Empty) && !CustomerCode.Equals(string.Empty))
                    {
                        continue;
                    }

                    #endregion

                    string PriceListID = "1";

                    err = ExistObject("PriceListLanguage", "Description", " Description = '" + PriceListName + "'", db_vms);//#####

                    if (err == InCubeErrors.Success)
                    {
                        PriceListID = GetFieldValue("PriceListLanguage", "PriceListID", " Description = '" + PriceListName + "'", db_vms);// #####

                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");//*********
                        err = QueryBuilderObject.UpdateQueryString("PriceList", "PriceListID=" + PriceListID + "", db_vms);

                        QueryBuilderObject.SetField("Description", "'" + PriceListName + "'");//#####
                        err = QueryBuilderObject.UpdateQueryString("PriceListLanguage", "PriceListID=" + PriceListID + "", db_vms);//#####
                    }
                    else
                    {
                        PriceListID = GetFieldValue("PriceList", "ISNULL(MAX(PriceListID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("PriceListCode", "'" + PriceListName + "'");
                        QueryBuilderObject.SetField("StartDate", "'" + DateTime.Now.Date.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("Priority", "1");
                        QueryBuilderObject.SetField("OrganizationID", "1");//TO BE ADDED TO THE NEW RELEASE INTEGRATION
                        err = QueryBuilderObject.InsertQueryString("PriceList", db_vms);

                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + PriceListName + "'");
                        err = QueryBuilderObject.InsertQueryString("PriceListLanguage", db_vms);


                    }

                    if (!IsDeafult.ToLower().Trim().Equals("N"))
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



                    if (!CustomerCode.Equals(string.Empty))
                    {
                        CustomerID = GetFieldValue("Customer", "CustomerID", " CustomerCode = '" + CustomerCode.Replace("'", "''") + "'", db_vms);
                        if (CustomerID != string.Empty)
                        {
                            InCubeQuery CMD = new InCubeQuery("SELECT OutletID FROM CustomerOutlet Where CustomerID = " + CustomerID, db_vms);
                            err = CMD.Execute();
                            err = CMD.FindFirst();
                            while (err == InCubeErrors.Success)
                            {
                                CMD.GetField(0, ref field);
                                string outletid = field.ToString();

                                err = ExistObject("CustomerPrice", "PriceListID", " CustomerID = " + CustomerID + " AND OutletID = " + outletid + " and pricelistID=" + PriceListID + "", db_vms);
                                if (err != InCubeErrors.Success)
                                {
                                    QueryBuilderObject.SetField("CustomerID", CustomerID);
                                    QueryBuilderObject.SetField("OutletID", outletid);
                                    QueryBuilderObject.SetField("PriceListID", PriceListID);
                                    err = QueryBuilderObject.InsertQueryString("CustomerPrice", db_vms);
                                }

                                err = CMD.FindNext();
                            }
                        }
                        else
                        {
                            CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", " CustomerCode = '" + CustomerCode.Replace("'", "''") + "'", db_vms);
                            string OutletID = GetFieldValue("CustomerOutlet", "OutletID", " CustomerCode = '" + CustomerCode.Replace("'", "''") + "'", db_vms);

                            err = ExistObject("CustomerPrice", "PriceListID", " CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                            if (err != InCubeErrors.Success && !OutletID.Equals(string.Empty))
                            {
                                QueryBuilderObject.SetField("CustomerID", CustomerID);
                                QueryBuilderObject.SetField("OutletID", OutletID);
                                QueryBuilderObject.SetField("PriceListID", PriceListID);
                                QueryBuilderObject.InsertQueryString("CustomerPrice", db_vms);
                            }
                        }
                    }

                    if (CustomerGroup != string.Empty && IsDeafult.ToLower().Trim().Equals("false"))
                    {
                        string GroupID = GetFieldValue("CustomerGroupLanguage", "GroupID", " Description = '" + CustomerGroup + "'", db_vms);

                        if (GroupID.Equals(string.Empty))
                        {
                            //******continue;
                        }

                        string currentGroup = GetFieldValue("GroupPrice", "GroupID", " GroupID = " + GroupID + " And PriceListID = " + PriceListID, db_vms);
                        if (currentGroup == string.Empty)
                        {
                            QueryBuilderObject.SetField("GroupID", GroupID);
                            QueryBuilderObject.SetField("PriceListID", PriceListID);
                            QueryBuilderObject.InsertQueryString("GroupPrice", db_vms);
                        }
                    }

                    // THE FOLLOWING QUERY WAS CHANGED BECAUSE WE ARE ALWAYS SUPPOSED TO GET THE BIGGEST UOM PRICE
                    string PackID = GetFieldValue("Pack", "top(1)PackID", "ItemID in (select ItemID from Item where ItemCode='" + ItemCode + "') and PackTypeID in ( select packtypeid from packtypelanguage where languageid=1 and description ='" + UOM + "') order by quantity Desc", db_vms).Trim();
                    if (PackID.Equals(string.Empty))
                    {
                        continue;
                    }
                    decimal TaxRate = decimal.Parse(GetFieldValue("Pack", "ISNULL(Width,0)", "PackID = " + PackID, db_vms));
                    TOTALUPDATED++;

                    int PriceDefinitionID = 1;
                    string currentPrice = GetFieldValue("PriceDefinition", "Price", "PackID = " + PackID + " AND PriceListID = " + PriceListID, db_vms);
                    if (currentPrice.Equals(string.Empty))
                    {
                        PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms));

                        QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID.ToString());
                        QueryBuilderObject.SetField("QuantityRangeID", "1");
                        QueryBuilderObject.SetField("PackID", PackID);
                        QueryBuilderObject.SetField("CurrencyID", "1");
                        QueryBuilderObject.SetField("Tax", TaxRate.ToString());
                        QueryBuilderObject.SetField("Price", Price.ToString());
                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        err = QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);
                    }
                    else
                    {
                        PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "PriceDefinitionID", "PackID = " + PackID + " AND PriceListID = " + PriceListID, db_vms));
                        if (!currentPrice.Equals(Price.ToString()))
                        {
                            QueryBuilderObject.SetField("Price", Price.ToString());
                            QueryBuilderObject.SetField("Tax", TaxRate.ToString());
                            err = QueryBuilderObject.UpdateQueryString("PriceDefinition", "PackID = " + PackID + " AND PriceListID = " + PriceListID + " AND PriceDefinitionID = " + PriceDefinitionID, db_vms);
                        }
                        else
                        { err = InCubeErrors.Success; }
                    }

                    UpdateFlag("Price", "ItemCode='" + ItemCode + "' and PriceListCode='" + PriceListName + "'");
                }
                #region UOM Price
                DT = new DataTable();
                string SelectGroup = @"select P.PackID,P.ItemID,PD.Price,P.Quantity,PD.PriceListID from Pack P inner join PriceDefinition PD on PD.PacKID=P.PackID and P.Quantity>1  order by P.Quantity Desc";
                InCubeQuery GroupQuery = new InCubeQuery(db_vms, SelectGroup);
                err = GroupQuery.Execute();
                DT = GroupQuery.GetDataTable();
                ClearProgress();
                SetProgressMax(DT.Rows.Count);
                foreach (DataRow row in DT.Rows)
                {
                    ReportProgress("CREATING OTHER UOMS PRICE");
                    string ItemID = row["ItemID"].ToString().Trim();
                    string PackID = row["PackID"].ToString().Trim();
                    string Price = row["Price"].ToString().Trim();
                    string Quantity = row["Quantity"].ToString().Trim();
                    string PriceListID = row["PriceListID"].ToString().Trim();
                    if (decimal.Parse(Quantity) == 0) continue;
                    //string basePack = GetFieldValue("Pack", "top(1)PackID", "ItemID=" + ItemID + " and Quantity<1 order by Quantity ", db_vms).Trim();

                    SelectGroup = @"select P.PackID, P.Quantity, ISNULL(P.Width,0) TaxRate from Pack P where ItemID=" + ItemID + "  and PackID <> " + PackID + " order by Quantity ";
                    GroupQuery = new InCubeQuery(db_vms, SelectGroup);
                    err = GroupQuery.Execute();
                    DataTable DT2 = new DataTable();
                    DT2 = GroupQuery.GetDataTable();
                    if (DT2.Rows.Count > 1) { }
                    foreach (DataRow row2 in DT2.Rows)
                    {
                        string basePack = row2["PackID"].ToString().Trim();
                        decimal TaxRate = decimal.Parse(row2["TaxRate"].ToString().Trim());
                        string BaseQuantity = row2["Quantity"].ToString().Trim();
                        if (basePack.Equals(string.Empty)) continue;
                        decimal basePrice = 0;
                        basePrice = decimal.Round(((decimal.Parse(Price) / decimal.Parse(Quantity)) * decimal.Parse(BaseQuantity)), 2);
                        //double x =Convert.ToDouble( Math.Round(((decimal.Parse(Price) / decimal.Parse(Quantity)) * decimal.Parse(BaseQuantity)), 3));
                        string existPrice = GetFieldValue("PriceDefinition", "Price", "PackID=" + basePack + " and PriceListID=" + PriceListID + "", db_vms).Trim();
                        if (existPrice.Equals(string.Empty))
                        {
                            int PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms));
                            QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID.ToString());
                            QueryBuilderObject.SetField("QuantityRangeID", "1");
                            QueryBuilderObject.SetField("PackID", basePack);
                            QueryBuilderObject.SetField("CurrencyID", "1");
                            QueryBuilderObject.SetField("Tax", TaxRate.ToString());
                            QueryBuilderObject.SetField("Price", basePrice.ToString());
                            QueryBuilderObject.SetField("PriceListID", PriceListID);
                            err = QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);
                        }
                        else
                        {
                            if (decimal.Parse(existPrice) != basePrice)
                            {
                                QueryBuilderObject.SetField("Price", basePrice.ToString());
                                QueryBuilderObject.SetField("Tax", TaxRate.ToString());
                                err = QueryBuilderObject.UpdateQueryString("PriceDefinition", "priceListID=" + PriceListID + " and packID=" + basePack + "", db_vms);
                            }
                        }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            DT.Dispose();

        }
        #endregion

        #region Bank
        private void UpdateBanks()
        {

            object field = new object();

            DataTable DT = new DataTable();
            qry = new InCubeQuery("SELECT * FROM [Bank]", db_ERP);
            err = qry.Execute();
            DT = qry.GetDataTable();

            ClearProgress();
            SetProgressMax(DT.Rows.Count);

            foreach (DataRow row in DT.Rows)
            {
                //Bank Code 
                //Description English   	
                //Description Arabic  	
                //Branch code   	
                //Branch Description English   	
                //Branch Description Arabic  


                string BankCode = row[0].ToString().Trim();
                string BankDescriptionEnglish = row[1].ToString().Trim();
                string BankDescriptionArabic = row[2].ToString().Trim();
                string Branchcode = row[3].ToString().Trim();
                string BranchDescriptionEnglish = row[4].ToString().Trim();
                string BranchDescriptionArabic = row[5].ToString().Trim();


                if (BankCode == string.Empty)
                    continue;

                err = ExistObject("Bank", "BankID", "BankID = " + BankCode, db_vms);

                if (err != InCubeErrors.Success)
                {
                    string BankID = GetFieldValue("Bank", "ISNULL(MAX(BankID),0) + 1", db_vms);

                    QueryBuilderObject.SetField("BankID", BankID);
                    QueryBuilderObject.SetField("Code", BankCode);
                    QueryBuilderObject.InsertQueryString("Bank", db_vms);

                    QueryBuilderObject.SetField("BankID", BankID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", BankDescriptionEnglish);
                    QueryBuilderObject.InsertQueryString("BankLanguage", db_vms);


                    QueryBuilderObject.SetField("BankID", BankID);
                    QueryBuilderObject.SetField("LanguageID", "2");
                    QueryBuilderObject.SetField("Description", "'" + BankDescriptionArabic + "'");
                    QueryBuilderObject.InsertQueryString("BankLanguage", db_vms);

                    string BranchID = GetFieldValue("BankBranch", "ISNULL(MAX(BranchID),0) + 1", " BankID = " + BankID, db_vms);

                    QueryBuilderObject.SetField("BankID", BankID);
                    QueryBuilderObject.SetField("BranchID", BranchID);
                    QueryBuilderObject.InsertQueryString("BankBranch", db_vms);

                    QueryBuilderObject.SetField("BankID", BankID);
                    QueryBuilderObject.SetField("BranchID", BranchID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + BranchDescriptionEnglish + "'");
                    QueryBuilderObject.InsertQueryString("BankBranchLanguage", db_vms);

                    QueryBuilderObject.SetField("BankID", BankID);
                    QueryBuilderObject.SetField("BranchID", BranchID);
                    QueryBuilderObject.SetField("LanguageID", "2");
                    QueryBuilderObject.SetField("Description", "'" + BranchDescriptionArabic + "'");
                    QueryBuilderObject.InsertQueryString("BankBranchLanguage", db_vms);
                }
            }


            DT.Dispose();
        }

        #endregion
        
        #region Update Discount

        public override void UpdateDiscount()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;


            object field = new object();

            InCubeQuery DeleteDiscountQuery = new InCubeQuery(db_vms, "Delete From Discount");
            DeleteDiscountQuery.ExecuteNonQuery();

            DataTable DT = new DataTable();
            qry = new InCubeQuery("SELECT * FROM [Discount]", db_ERP);
            err = qry.Execute();
            DT = qry.GetDataTable();

            ClearProgress();
            SetProgressMax(DT.Rows.Count);

            foreach (DataRow row in DT.Rows)
            {
                ReportProgress("Updating Discounts");

                //Customer code	
                //Item Code	
                //Pack	
                //Discount 

                string CustomerCode = row[0].ToString().Trim();
                string ItemCode = row[1].ToString().Trim();
                string UOMdesc = row[2].ToString().Trim();
                string dis = row[3].ToString().Trim();

                if (CustomerCode == string.Empty)
                    continue;

                string CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", " CustomerCode = '" + CustomerCode + "'", db_vms);
                string OutletID = GetFieldValue("CustomerOutlet", "OutletID", " CustomerCode = '" + CustomerCode + "'", db_vms);

                if (OutletID == string.Empty)
                {
                    continue;
                }

                string ItemID = GetFieldValue("Item", "ItemID", "ItemCode='" + ItemCode + "'", db_vms);

                if (ItemID == string.Empty)
                {
                    continue;
                }

                string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " LanguageID = 1 and Description = '" + UOMdesc + "'", db_vms);

                if (PackTypeID == string.Empty)
                {
                    continue;
                }

                string PackID = GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " AND PackTypeID = " + PackTypeID, db_vms);

                if (PackID == string.Empty)
                {
                    continue;
                }

                decimal Discount = decimal.Parse(dis);

                string MAXID = GetFieldValue("Discount", " IsNull(MAX(DiscountID),0) + 1 ", db_vms);

                err = ExistObject("Discount", "Discount", " CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                if (err == InCubeErrors.Success)
                {
                    TOTALUPDATED++;

                    string DiscountID = GetFieldValue("Discount", "DiscountID", " CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                    QueryBuilderObject.SetField("Discount", Discount.ToString());
                    QueryBuilderObject.UpdateQueryString("Discount", "DiscountID = " + DiscountID, db_vms);
                }
                else
                {
                    TOTALINSERTED++;

                    QueryBuilderObject.SetField("DiscountID", MAXID);
                    QueryBuilderObject.SetField("PackID", PackID);
                    QueryBuilderObject.SetField("CustomerID", CustomerID);
                    QueryBuilderObject.SetField("OutletID", OutletID);
                    QueryBuilderObject.SetField("Discount", Discount.ToString());
                    QueryBuilderObject.SetField("FOC", "0");
                    QueryBuilderObject.SetField("StartDate", "'" + DateTime.Now.Date.ToString("dd/MMM/yyyy") + "'");
                    QueryBuilderObject.SetField("EndDate", "'" + DateTime.Now.Date.AddYears(10).ToString("dd/MMM/yyyy") + "'");
                    QueryBuilderObject.InsertQueryString("Discount", db_vms);
                }
            }

            DT.Dispose();

            WriteMessage("\r\n");
            WriteMessage("<<< CUSTOMERS DISCOUNT >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        #endregion

        #region Update Route
        public override void UpdateRoutes()
        {
            try
            {
                WriteMessage("\r\nUpdating Routes ..");
                OleDbConnection excelConn = new OleDbConnection();
                List<string> connectionStrings = new List<string>();
                connectionStrings.Add(string.Format(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=""Excel 12.0 Xml;HDR=YES"";", CoreGeneral.Common.StartupPath + "\\Routes.xlsx"));
                connectionStrings.Add(string.Format(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=""Excel 8.0;HDR=YES"";", CoreGeneral.Common.StartupPath + "\\Routes.xlsx"));
                connectionStrings.Add(string.Format(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=""Excel 8.0;HDR=Yes;IMEX=1"";", CoreGeneral.Common.StartupPath + "\\Routes.xlsx"));

                for (int i = 0; i < connectionStrings.Count; i++)
                {
                    try
                    {
                        excelConn.ConnectionString = connectionStrings[i];
                        excelConn.Open();
                        break;
                    }
                    catch (Exception ex2)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex2.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        if (i == connectionStrings.Count - 1)
                            throw new Exception("Failed in openning excel file");
                    }
                }

                DataTable dtData = excelConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                string sheet1 = dtData.Rows[0]["TABLE_NAME"].ToString();
                dtData = excelConn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, null);
                string where = " WHERE ";
                string queryString = "SELECT ";
                foreach (string colName in Enum.GetNames(typeof(requiredColumns)))
                {
                    queryString += "[" + colName + "],";
                    where += "[" + colName + "] IS NOT NULL AND ";
                    DataRow[] dr = dtData.Select("TABLE_NAME = '" + sheet1 + "' AND COLUMN_NAME = '" + colName + "'");
                    if (dr.Length == 0)
                    {
                        throw new Exception("Column [" + colName + "] doesn't exist in sheet " + sheet1);
                    }
                }

                queryString = queryString.Substring(0, queryString.Length - 1) + " FROM [" + sheet1 + "] " + where.Substring(0, where.Length - 5);

                OleDbCommand excelCmd = new OleDbCommand(queryString, excelConn);
                excelCmd.CommandTimeout = 3600000;
                OleDbDataReader odr = excelCmd.ExecuteReader();

                InCubeQuery qry = new InCubeQuery(db_vms, "DELETE FROM Routes_Temp");
                if (qry.ExecuteNonQuery() != InCubeErrors.Success)
                    throw new Exception("Error in erasing old routes data");

                SqlBulkCopy bulk = new SqlBulkCopy(db_vms.GetConnection());
                bulk.DestinationTableName = "Routes_Temp";
                bulk.BulkCopyTimeout = 3600000;
                bulk.WriteToServer(odr);

                string countAll = GetFieldValue("Routes_Temp", "COUNT(*)", db_vms);
                WriteMessage("\r\nAll rows found are " + countAll);
                WriteMessage("\r\nProcessing ... This may take a few minutes ..");

                qry = new InCubeQuery(db_vms, "sp_UpdateRouteCustomers");
                if (qry.ExecuteStoredProcedure() != InCubeErrors.Success)
                {
                    //WriteMessage(qry.GetCurrentException().Message);
                }
                string countSuccess = GetFieldValue("Routes_Temp", "COUNT(*)", "Result = 'Record Added Successfully'", db_vms);
                WriteMessage("\r\nAdded successfully rows: " + countSuccess);
                WriteMessage("\r\nYou can see more details by checking table Routes_Temp\r\n");
            }
            catch (Exception ex)
            {
                WriteMessage("\r\n" + ex.Message + "\r\n");
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        
        #endregion

        #region Update KPI
        public override void UpdateKPI()
        {
            try
            {
                qry = new InCubeQuery(db_vms, "sp_CalculateBrandAchievements");
                if (qry.ExecuteStoredProcedure() != InCubeErrors.Success)
                {
                    WriteMessage("Error in calculating achievements: " + qry.GetCurrentException().Message);
                }
                else
                {
                    WriteMessage("Brands achievements is calculated ..");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        #endregion

        #region SendInvoices
        private bool FillCustomerInfoFromGP(string CustomerCode, taSopHdrIvcInsert salesHdr)
        {
            try
            {
                string QueryString = string.Format(@"SELECT TOP 1 
LTRIM(RTRIM(ISNULL(SHIPMTHD,''))) SHIPMTHD,
LTRIM(RTRIM(ISNULL(CITY,''))) CITY,
LTRIM(RTRIM(ISNULL(STATE,''))) STATE,
LTRIM(RTRIM(ISNULL(ZIP,''))) ZIP, 
LTRIM(RTRIM(ISNULL(COUNTRY,''))) COUNTRY,
LTRIM(RTRIM(ISNULL(CNTCPRSN,''))) CNTCPRSN,
LTRIM(RTRIM(ISNULL(PHONE1,''))) PHONE1,
LTRIM(RTRIM(ISNULL(PHONE2,''))) PHONE2,
LTRIM(RTRIM(ISNULL(PHONE3,''))) PHONE3,
LTRIM(RTRIM(ISNULL(FAX,''))) FAX,
LTRIM(RTRIM(ISNULL(SLPRSNID,''))) SLPRSNID,
LTRIM(RTRIM(ISNULL(TAXSCHID,''))) TAXSCHID,
LTRIM(RTRIM(ISNULL(SALSTERR,''))) SALSTERR,
LTRIM(RTRIM(ISNULL(ADDRESS1,''))) ADDRESS1,
LTRIM(RTRIM(ISNULL(ADDRESS2,''))) ADDRESS2,
LTRIM(RTRIM(ISNULL(ADDRESS3,''))) ADDRESS3,
LTRIM(RTRIM(ISNULL(PYMTRMID,''))) PYMTRMID
FROM RM00101 WHERE LTRIM(RTRIM(CUSTNMBR)) = '{0}'", CustomerCode.Trim().Replace("'", "''"));

                InCubeQuery inCubeQuery = new InCubeQuery(db_ERP, QueryString);
                inCubeQuery.Execute();

                DataTable dtGPCustInfo = inCubeQuery.GetDataTable();
                if (dtGPCustInfo == null || dtGPCustInfo.Rows.Count == 0)
                    return false;

                //salesHdr.SHIPMTHD = dtGPCustInfo.Rows[0]["SHIPMTHD"].ToString();
                //salesHdr.CITY = dtGPCustInfo.Rows[0]["CITY"].ToString();
                //salesHdr.STATE = dtGPCustInfo.Rows[0]["STATE"].ToString();
                //salesHdr.ZIPCODE = dtGPCustInfo.Rows[0]["ZIP"].ToString();
                //salesHdr.COUNTRY = dtGPCustInfo.Rows[0]["COUNTRY"].ToString();
                //salesHdr.CNTCPRSN = dtGPCustInfo.Rows[0]["CNTCPRSN"].ToString();
                //salesHdr.PHNUMBR1 = dtGPCustInfo.Rows[0]["PHONE1"].ToString();
                //salesHdr.PHNUMBR2 = dtGPCustInfo.Rows[0]["PHONE2"].ToString();
                //salesHdr.PHNUMBR3 = dtGPCustInfo.Rows[0]["PHONE3"].ToString();
                //salesHdr.FAXNUMBR = dtGPCustInfo.Rows[0]["FAX"].ToString();

                salesHdr.TAXSCHID = dtGPCustInfo.Rows[0]["TAXSCHID"].ToString();
                salesHdr.DEFTAXSCHDS = 0;
                salesHdr.ADDRESS1 = dtGPCustInfo.Rows[0]["ADDRESS1"].ToString();
                salesHdr.ADDRESS2 = dtGPCustInfo.Rows[0]["ADDRESS2"].ToString();
                salesHdr.ADDRESS3 = dtGPCustInfo.Rows[0]["ADDRESS3"].ToString();
                salesHdr.SALSTERR = dtGPCustInfo.Rows[0]["SALSTERR"].ToString();
                salesHdr.PYMTRMID = dtGPCustInfo.Rows[0]["PYMTRMID"].ToString();
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return false;
            }
        }
        private decimal RoundDecimalToLower(object Value, int Digits)
        {
            decimal result = 0;
            try
            {
                double temp = Convert.ToDouble(Value);
                temp = temp * Math.Pow(10, Digits);
                int integerPart = (int)temp;
                temp = integerPart / Math.Pow(10, Digits);
                result = (decimal)temp;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return result;
        }

        private decimal GetRoundedDecimal(object Value, int Digits)
        {
            decimal result = 0;
            try
            {
                double temp = Convert.ToDouble(Value);
                temp = temp * Math.Pow(10, Digits);
                int integerPart = (int)temp;
                double fraction = temp - integerPart;
                if (fraction >= 0.5)
                    integerPart += 1;
                temp = integerPart / Math.Pow(10, Digits);
                result = (decimal)temp;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return result;
        }
        public string GetHeaderQuery(IntegrationField Field, DateTime FromDate, DateTime ToDate, bool AllSalespersons, string Salesperson)
        {
            string QueryString = "";
            try
            {
                switch (Field)
                {
                    case IntegrationField.Sales_S:
                    case IntegrationField.Returns_S:
                        QueryString = @"SELECT T.TransactionID, T.TransactionDate, CO.CustomerCode, W.Barcode AS WarehouseCode, 
E.EmployeeCode, COL.Description, CO.CustomerID, CO.OutletID
, CASE WHEN T.TransactionTypeID IN (1,3) THEN T.LPONumber ELSE T.Notes END LPONumber
,T.NetTotal HeaderNet, T.GrossTotal HeaderGross, T.Discount HeaderDiscount, T.Tax HeaderTax
FROM [Transaction] T 
INNER JOIN CustomerOutletLanguage COL ON T.CustomerID = COL.CustomerID AND T.OutletID = COL.OutletID AND COL.LanguageID = 1
INNER JOIN CustomerOutlet CO ON T.CustomerID = CO.CustomerID AND T.OutletID = CO.OutletID
INNER JOIN Employee E ON E.EmployeeID = T.EmployeeID 
INNER JOIN EmployeeVehicle EV ON EV.EmployeeID = T.EmployeeID
INNER JOIN Warehouse W ON W.WarehouseID = EV.VehicleID
WHERE T.Synchronized = 0 AND T.Voided = 0 AND T.TransactionTypeID IN (" + (Field == IntegrationField.Sales_S ? "1,3" : "2,4") + ")";

                        if (!Filters.ExtraSendFilter.Equals(string.Empty))
                        {
                            QueryString += " AND T.TransactionID = '" + Filters.ExtraSendFilter + "'";
                        }
                        else
                        {
                            QueryString += " AND T.TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + "'";
                            QueryString += " AND T.TransactionDate < '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + "'";
                            if (!AllSalespersons)
                            {
                                QueryString += " AND T.EmployeeID = " + Salesperson;
                            }
                        }
                        break;
                    case IntegrationField.Orders_S:
                        QueryString = @"SELECT     

            SO.OrderID TransactionID, 
            SO.OrderDate TransactionDate, 
            CustomerOutlet.CustomerCode AS CustomerCode, 
            Warehouse.Barcode AS WarehouseCode, 
           
            Employee.EmployeeCode, 
            CustomerOutletLanguage.Description,
            CustomerOutlet.CustomerID,
            CustomerOutlet.OutletID,

            CustomerOutletLanguage.Address,
            CustomerOutlet.CustomerCode as OutletCode,
CustomerOutlet.StreetAddress,
SO.LPO LPONumber,
SO.PromotedDiscount,
0 FinalDiscount,
SO.NetTotal

            FROM         SalesOrder SO INNER JOIN
            CustomerOutletLanguage ON SO.CustomerID = CustomerOutletLanguage.CustomerID AND SO.OUTLETID=CUSTOMEROUTLETLANGUAGE.OUTLETID AND CUSTOMEROUTLETLANGUAGE.LANGUAGEID=1 INNER JOIN
            CustomerOutlet ON SO.CustomerID = CustomerOutlet.CustomerID AND SO.OutletID = CustomerOutlet.OutletID AND 
            CustomerOutletLanguage.CustomerID = CustomerOutlet.CustomerID AND CustomerOutletLanguage.OutletID = CustomerOutlet.OutletID INNER JOIN
           EmployeeVehicle EV ON EV.EmployeeID = SO.EmployeeID 
			left join Warehouse on Warehouse.WarehouseID = ev.VehicleID
            left outer join Warehouse Wh on Wh.WarehouseID=vl.WarehouseID and Wh.WarehouseTypeID=1 inner join 
            Employee ON SO.EmployeeID = Employee.EmployeeID 
              WHERE (SO.Synchronized = 0)  AND SO.OrderStatusID IN (1,2)
			   ";

                        if (!Filters.ExtraSendFilter.Equals(string.Empty))
                        {
                            QueryString += " AND SO.TransactionID = '" + Filters.ExtraSendFilter + "'";
                        }
                        else
                        {
                            QueryString += " AND SO.OrderDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + "'";
                            QueryString += " AND SO.OrderDate <= '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + "'";
                        }

                        if (!AllSalespersons)
                        {
                            QueryString += " AND SO.EmployeeID = " + Salesperson;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return QueryString;
        }
        private string GetDetailsQuery(IntegrationField Field, string TransID, int CustomerID, int OutletID)
        {
            string QueryString = "";
            try
            {
                switch (Field)
                {
                    case IntegrationField.Sales_S:
                    case IntegrationField.Returns_S:
                        QueryString = string.Format(@"SELECT TD.PackID, TD.Quantity, TD.Price, TD.Discount, TD.ExpiryDate, IL.Description AS ItemName, 
I.ItemCode AS Barcode, PTL.Description AS PackName, TD.SalesTransactionTypeID, TD.PackStatusID,
TD.Tax, TD.UsedPriceListID, ISNULL(P.Width,3) Width
FROM TransactionDetail TD 
INNER JOIN Pack P ON P.PackID = TD.PackID
INNER JOIN Item I ON I.ItemID = P.ItemID
INNER JOIN ItemLanguage IL ON IL.ItemID = P.ItemID AND IL.LanguageID = 1
INNER JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1
WHERE TD.TransactionID = '{0}' AND TD.CustomerID = {1} AND TD.OutletID = {2}
ORDER BY P.Width DESC", TransID, CustomerID, OutletID);
                        break;
                    case IntegrationField.Orders_S:
                        QueryString = @"SELECT     
SOD.OrderID,
SOD.BatchNo,
SOD.Quantity,
SOD.Price, 
SOD.ExpiryDate, 
SOD.Discount, 
ItemLanguage.Description AS ItemName, 
Item.ItemCode as Barcode, 
PackTypeLanguage.Description AS PackName, 
Pack.Quantity AS PcsInCse, 
SOD.PackID,
1 FOCTypeID,
SOD.UsedPriceListID,
SOD.SalesTransactionTypeID
FROM SalesOrderDetail SOD INNER JOIN
Pack ON SOD.PackID = Pack.PackID INNER JOIN
ItemLanguage ON Pack.ItemID = ItemLanguage.ItemID INNER JOIN
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID INNER JOIN
Item ON ItemLanguage.ItemID = Item.ItemID
WHERE (PackTypeLanguage.LanguageID = 1) AND (ItemLanguage.LanguageID = 1) 
AND (SOD.OrderID = '" + TransID.ToString() + "') AND (SOD.CustomerID = '" + CustomerID.ToString() + "')";
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return QueryString;
        }
        public Result SendTransactionWithTax(string filename, bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate, IntegrationField Field, bool SendTax)
        {
            //Tax is commented: to undo, make create taxes zero, and uncommetn tax amount and TAXSHDLID in header adn details, and uncommetn read tax from DB
            Result res = Result.UnKnown;
            try
            {
                //Declarations
                InCubeQuery inCubeQuery;
                int processID = 0, totalSuccess = 0, totalFailure = 0, CustomerID = 0, OutletID = 0, SalesTransactionTypeID = 0, PackStatusID = 0, LINSEQ = 0, PackID = 0;
                short SOPTYPE = 0;
                string TransactionID = "", CustomerName = "", CustomerCode = "", WarehouseCode = "", EmployeeCode = "", QryStr = "", result = "", LPONumber = "", _DOCID = "";
                string ItemCode = "", packCode = "", STRPack = "";
                decimal TOTAL = 0, DiscountTotal = 0, TaxTotal = 0, HeaderGross = 0, HeaderDiscount = 0, HeaderTax = 0, HeaderNet = 0, Width = 0, OriginalDiscount = 0, MiscDiscount = 0, LineMisc = 0;
                decimal Quantity = 0, LineDiscount = 0, DiscountPerOne = 0, BaseUOMPrice = 0, LineTax = 0, XtndPrice = 0;
                DateTime ExpiryDate = DateTime.Today, TransactionDate;
                SOPTransactionType salesOrder = new SOPTransactionType();
                taSopHdrIvcInsert salesHdr = new taSopHdrIvcInsert();
                DataTable dtDetails = new DataTable();
                List<taSopLineIvcInsert_ItemsTaSopLineIvcInsert> LineItems = new List<taSopLineIvcInsert_ItemsTaSopLineIvcInsert>();
                List<taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert> TaxLines = new List<taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert>();
                List<taSopLotAuto_ItemsTaSopLotAuto> LotLines = new List<taSopLotAuto_ItemsTaSopLotAuto>();
                taSopLineIvcInsert_ItemsTaSopLineIvcInsert salesLine = new taSopLineIvcInsert_ItemsTaSopLineIvcInsert();
                taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert taxLine = new taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert();
                taSopLotAuto_ItemsTaSopLotAuto lotLine = new taSopLotAuto_ItemsTaSopLotAuto();
                eConnectType eConnect = new eConnectType();
                
                switch (Field)
                {
                    case IntegrationField.Sales_S:
                        WriteMessage("\r\n" + "Sending Invoices");
                        SOPTYPE = 3;
                        _DOCID = "DTS";
                        break;
                    case IntegrationField.Returns_S:
                        WriteMessage("\r\n" + "Sending Returns");
                        SOPTYPE = 4;
                        _DOCID = "RTN-DTS";
                        break;
                    case IntegrationField.Orders_S:
                        WriteMessage("\r\n" + "Sending Orders");
                        SOPTYPE = 2;
                        _DOCID = "ORD-STD";
                        break;
                }

                string QueryString = GetHeaderQuery(Field, FromDate, ToDate, AllSalespersons, Salesperson);
                
                inCubeQuery = new InCubeQuery(db_vms, QueryString);
                if (inCubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Header query failed !!"));
                }

                DataTable dtHeader = inCubeQuery.GetDataTable();
                //Logger.WriteLog("SendTransactions", dtHeader.Rows.Count.ToString(), QueryString, LoggingType.Information, LoggingFiles.errorInv);
                if (dtHeader.Rows.Count == 0)
                {
                    res = Result.NoRowsFound;
                    WriteMessage("There are no transactions to send ..");
                }
                else
                {
                    ClearProgress();
                    SetProgressMax(dtHeader.Rows.Count);
                }

                for (int m = 0; m < dtHeader.Rows.Count; m++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = "";
                        TOTAL = 0;
                        LINSEQ = 0;
                        DiscountTotal = 0;
                        TaxTotal = 0;

                        TransactionID = dtHeader.Rows[m]["TransactionID"].ToString();
                        CustomerID = Convert.ToInt32(dtHeader.Rows[m]["CustomerID"]);
                        OutletID = Convert.ToInt32(dtHeader.Rows[m]["OutletID"]);

                        ReportProgress("Sending Transaction: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, TransactionID);
                        filters.Add(9, CustomerID.ToString() + ":" + OutletID.ToString());
                        filters.Add(10, SendTax ? "1" : "0");
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        TransactionDate = Convert.ToDateTime(dtHeader.Rows[m]["TransactionDate"]);
                        CustomerCode = dtHeader.Rows[m]["CustomerCode"].ToString();
                        if (CoreGeneral.Common.GeneralConfigurations.SiteSymbol.ToLower() == "esfnew")
                        {
                            string[] CustomerCodeParts = CustomerCode.Split(new char[] { '_' });
                            if (CustomerCodeParts.Length == 2)
                                CustomerCode = CustomerCodeParts[0];
                        }
                        EmployeeCode = dtHeader.Rows[m]["EmployeeCode"].ToString();
                        CustomerName = dtHeader.Rows[m]["Description"].ToString();
                        WarehouseCode = dtHeader.Rows[m]["WarehouseCode"].ToString();
                        LPONumber = dtHeader.Rows[m]["LPONumber"].ToString();
                        HeaderNet = decimal.Parse(dtHeader.Rows[m]["HeaderNet"].ToString());
                        HeaderGross = decimal.Parse(dtHeader.Rows[m]["HeaderGross"].ToString());
                        HeaderDiscount = decimal.Parse(dtHeader.Rows[m]["HeaderDiscount"].ToString());
                        HeaderTax = decimal.Parse(dtHeader.Rows[m]["HeaderTax"].ToString());

                        salesHdr = new taSopHdrIvcInsert();
                        salesHdr.SOPTYPE = SOPTYPE;
                        salesHdr.SOPNUMBE = TransactionID;
                        salesHdr.CSTPONBR = LPONumber;
                        salesHdr.SLPRSNID = EmployeeCode;
                        salesHdr.DOCDATE = TransactionDate.ToString("yyyy-MM-dd");
                        salesHdr.CUSTNMBR = CustomerCode.ToString().Trim();
                        salesHdr.CUSTNAME = CustomerName.ToString().Trim();
                        salesHdr.USER2ENT = "InCube";
                        salesHdr.REFRENCE = EmployeeCode + "-" + TransactionDate.ToString("ddMMyy");
                        salesHdr.LOCNCODE = WarehouseCode;
                        salesHdr.DOCID = _DOCID;
                        salesHdr.TRDISAMTSpecified = true;
                        salesHdr.CREATECOMM = 1;

                        if (Field != IntegrationField.Returns_S)
                        {
                            salesHdr.BACHNUMB = EmployeeCode + DateTime.Today.ToString();
                            salesHdr.DISAVAMTSpecified = true;
                            salesHdr.DISAVAMT = 0;
                            salesHdr.ShipToName = CustomerName;
                        }
                        else
                        {
                            salesHdr.ORIGTYPE = 0;
                            salesHdr.FRTTXAMT = 0;
                            salesHdr.MSCTXAMT = 0;
                            salesHdr.MSTRNUMB = 0;
                            salesHdr.FREIGHT = 0;
                            salesHdr.MISCAMNT = 0;
                            salesHdr.DISTKNAM = 0;
                            salesHdr.BACHNUMB = EmployeeCode + "-" + TransactionDate.ToString("ddMMyy");
                        }

                        salesHdr.SHIPMTHD = "";
                        salesHdr.CITY = "";
                        salesHdr.STATE = "";
                        salesHdr.ZIPCODE = "";
                        salesHdr.COUNTRY = "";
                        salesHdr.CNTCPRSN = "";
                        salesHdr.PHNUMBR1 = "";
                        salesHdr.PHNUMBR2 = "";
                        salesHdr.PHNUMBR3 = "";
                        salesHdr.FAXNUMBR = "";
                        salesHdr.ADDRESS1 = "";
                        salesHdr.ADDRESS2 = "";
                        salesHdr.ADDRESS3 = "";
                        salesHdr.PYMTRMID = "";
                        salesHdr.SALSTERR = "";
                        
                        if (SendTax)
                            salesHdr.CREATETAXES = 0;
                        else
                            salesHdr.CREATETAXES = 1;

                        salesHdr.USINGHEADERLEVELTAXES = 0;
                        if (!FillCustomerInfoFromGP(CustomerCode, salesHdr))
                        {
                            result = "New customer not avaialble in GP [" + CustomerCode + "]";
                            throw new Exception(result);
                        }

                        DataTable dtTransDetails = new DataTable();
                        dtTransDetails.Columns.Add("TriggerID");
                        dtTransDetails.Columns.Add("TransactionID");
                        dtTransDetails.Columns.Add("GrossTotal");
                        dtTransDetails.Columns.Add("DiscountTotal");
                        dtTransDetails.Columns.Add("TaxTotal");
                        dtTransDetails.Columns.Add("NetTotal");
                        dtTransDetails.Columns.Add("PackID");
                        dtTransDetails.Columns.Add("ItemCode");
                        dtTransDetails.Columns.Add("Quantity");
                        dtTransDetails.Columns.Add("LineQuantity");
                        dtTransDetails.Columns.Add("Price");
                        dtTransDetails.Columns.Add("UNITPRCE");
                        dtTransDetails.Columns.Add("XTNDPRCE");
                        dtTransDetails.Columns.Add("Discount");
                        dtTransDetails.Columns.Add("MRKDNAMT");
                        dtTransDetails.Columns.Add("LineDiscount");
                        dtTransDetails.Columns.Add("LineMisc");
                        dtTransDetails.Columns.Add("Tax");
                        dtTransDetails.Columns.Add("TAXAMNT");


                        QryStr = GetDetailsQuery(Field, TransactionID, CustomerID, OutletID);
                        inCubeQuery = new InCubeQuery(db_vms, QryStr);
                        err = inCubeQuery.Execute();
                        dtDetails = inCubeQuery.GetDataTable();

                        LineItems = new List<taSopLineIvcInsert_ItemsTaSopLineIvcInsert>();
                        TaxLines = new List<taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert>();
                        LotLines = new List<taSopLotAuto_ItemsTaSopLotAuto>();

                        if (dtDetails.Rows.Count == 0)
                        {
                            result = "No details found , Invoice Number = " + TransactionID.ToString();
                            throw new Exception(result);
                        }

                        MiscDiscount = 0;
                        for (int i = 0; i < dtDetails.Rows.Count; i++)
                        {
                            dtTransDetails.Rows.Add(dtTransDetails.NewRow());
                            PackID = Convert.ToInt16(dtDetails.Rows[i]["PackID"]);
                            Quantity = GetRoundedDecimal(dtDetails.Rows[i]["Quantity"], 5);
                            BaseUOMPrice = GetRoundedDecimal(dtDetails.Rows[i]["Price"], 2);
                            OriginalDiscount = GetRoundedDecimal(dtDetails.Rows[i]["Discount"], 2);
                            DiscountPerOne = RoundDecimalToLower(OriginalDiscount / Quantity, 2);
                            LineDiscount = DiscountPerOne * Quantity;
                            LineMisc = OriginalDiscount - LineDiscount;
                            MiscDiscount += LineMisc;
                            ExpiryDate = Convert.ToDateTime(dtDetails.Rows[i]["ExpiryDate"]);
                            STRPack = dtDetails.Rows[i]["ItemName"].ToString().Trim();
                            ItemCode = dtDetails.Rows[i]["Barcode"].ToString().Trim();
                            packCode = dtDetails.Rows[i]["PackName"].ToString().Trim();
                            SalesTransactionTypeID = Convert.ToInt16(dtDetails.Rows[i]["SalesTransactionTypeID"]);
                            PackStatusID = Convert.ToInt16(dtDetails.Rows[i]["PackStatusID"]);
                            Width = Convert.ToDecimal(dtDetails.Rows[i]["Width"]);
                            LINSEQ = LINSEQ + 16384;

                            taxLine = new taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert();
                            taxLine.LNITMSEQ = LINSEQ;
                            taxLine.CUSTNMBR = CustomerCode.ToString().Trim();
                            taxLine.TAXDTLID = CoreGeneral.Common.GeneralConfigurations.TAXDTLID;
                            taxLine.SOPTYPE = SOPTYPE;

                            salesLine = new taSopLineIvcInsert_ItemsTaSopLineIvcInsert();
                            salesLine.SOPTYPE = SOPTYPE;
                            salesLine.DOCID = _DOCID;
                            salesLine.ALLOCATE = 1;
                            if (CoreGeneral.Common.GeneralConfigurations.SiteSymbol.ToLower() == "esfnew")
                            {
                                salesLine.AutoAssignBin = 1;
                            }
                            salesLine.SOPNUMBE = TransactionID;
                            salesLine.DOCDATE = TransactionDate.ToString("yyyy-MM-dd");
                            salesLine.CUSTNMBR = CustomerCode;
                            salesLine.AUTOALLOCATELOT = 0;
                            salesLine.SALSTERR = salesHdr.SALSTERR;
                            salesLine.SLPRSNID = salesHdr.SLPRSNID;
                            salesLine.LOCNCODE = WarehouseCode;
                            salesLine.ITEMNMBR = ItemCode;
                            salesLine.ITEMDESC = STRPack;
                            salesLine.ADDRESS1 = salesHdr.ADDRESS1;
                            salesLine.ADDRESS2 = salesHdr.ADDRESS2;
                            salesLine.ADDRESS3 = salesHdr.ADDRESS3;

                            if (Field != IntegrationField.Returns_S)
                            {
                                salesLine.AUTOALLOCATESERIAL = 0;
                                salesLine.QTYFULFISpecified = true;
                                salesLine.QTYFULFI = Quantity;
                                salesLine.MRKDNAMT = DiscountPerOne;
                                if (SalesTransactionTypeID == 2 && BaseUOMPrice == 0)
                                {
                                    BaseUOMPrice = 0;
                                    XtndPrice = 0;
                                }
                                else
                                {
                                    XtndPrice = BaseUOMPrice * Quantity - LineDiscount;
                                }
                            }
                            else
                            {
                                decimal baseQty = 0;
                                LineDiscount = 0;
                                GetBaseQuantity_RET(PackID, Quantity, ref packCode, ref baseQty, ref BaseUOMPrice, ref XtndPrice);
                                Quantity = baseQty;

                                salesLine.COMMNTID = "TEST";
                                lotLine = new taSopLotAuto_ItemsTaSopLotAuto();
                                switch (PackStatusID)
                                {
                                    case 1: // Damaged
                                        salesLine.QTYDMGED = Quantity;
                                        salesLine.QTYINSVC = 0;
                                        salesLine.QTYONHND = 0;
                                        lotLine.QTYTYPE = 5;
                                        lotLine.QUANTITY = 0;
                                        lotLine.EXPNDATE = ExpiryDate.ToString("yyyy-MM-dd");
                                        break;
                                    case 2://Expired
                                        salesLine.QTYDMGED = Quantity;
                                        salesLine.QTYINSVC = 0;
                                        salesLine.QTYONHND = 0;
                                        lotLine.QTYTYPE = 5;
                                        lotLine.QUANTITY = 0;
                                        break;
                                    case 3://In Good Condition
                                        salesLine.QTYONHND = Quantity;
                                        salesLine.QTYDMGED = 0;
                                        salesLine.QTYINSVC = 0;
                                        lotLine.QTYTYPE = 1;
                                        lotLine.EXPNDATE = ExpiryDate.ToString("yyyy-MM-dd");
                                        lotLine.MFGDATE = ExpiryDate.AddYears(-1).ToString("yyyy-MM-dd");
                                        break;
                                }
                            }

                            if (SendTax)
                            {
                                LineTax = GetRoundedDecimal(dtDetails.Rows[i]["Tax"], 2);
                            }
                            else
                            {
                                LineTax = 0;
                            }

                            XtndPrice = GetRoundedDecimal(XtndPrice.ToString(), 2);
                            salesLine.UOFM = packCode;
                            salesLine.QUANTITY = Quantity;

                            salesLine.LNITMSEQ = LINSEQ;
                            salesLine.QTYRTRND = 0;
                            salesLine.QTYINUSE = 0;
                            salesLine.UNITCOST = 0;
                            salesLine.UNITCOSTSpecified = false;
                            salesLine.NONINVEN = 0;
                            salesLine.DROPSHIP = 0;
                            salesLine.QTYTBAOR = 0;

                            DiscountTotal += LineDiscount;
                            TaxTotal += LineTax;
                            TOTAL += XtndPrice;

                            salesLine.XTNDPRCE = XtndPrice;
                            salesLine.UNITPRCE = BaseUOMPrice;
                            salesLine.MRKDNAMTSpecified = true;

                            if (SendTax)
                            {
                                salesLine.TAXAMNT = LineTax;
                                salesLine.TAXSCHID = salesHdr.TAXSCHID;
                            }

                            if (Field == IntegrationField.Returns_S)
                            {
                                lotLine.AUTOCREATELOT = 0;
                                lotLine.DOCID = _DOCID;
                                lotLine.LNITMSEQ = LINSEQ;
                                lotLine.ITEMNMBR = salesLine.ITEMNMBR;
                                lotLine.SOPNUMBE = salesLine.SOPNUMBE;
                                lotLine.SOPTYPE = salesLine.SOPTYPE;
                                lotLine.UOFM = packCode;
                                lotLine.LOCNCODE = salesLine.LOCNCODE;
                                lotLine.LOTNUMBR = DateTime.Now.ToString("yyyyMMddhhmmss");
                                lotLine.QUANTITY = salesLine.QUANTITY;
                                LotLines.Add(lotLine);
                            }

                            LineItems.Add(salesLine);

                            taxLine.SALESAMT = XtndPrice;
                            taxLine.STAXAMNT = LineTax;

                            if (SendTax && LineTax > 0)
                                TaxLines.Add(taxLine);

                            dtTransDetails.Rows[i]["TriggerID"] = TriggerID;
                            dtTransDetails.Rows[i]["TransactionID"] = TransactionID;
                            dtTransDetails.Rows[i]["GrossTotal"] = HeaderGross;
                            dtTransDetails.Rows[i]["DiscountTotal"] = HeaderDiscount;
                            dtTransDetails.Rows[i]["TaxTotal"] = HeaderTax;
                            dtTransDetails.Rows[i]["NetTotal"] = HeaderNet;
                            dtTransDetails.Rows[i]["PackID"] = dtDetails.Rows[i]["PackID"];
                            dtTransDetails.Rows[i]["ItemCode"] = ItemCode;
                            dtTransDetails.Rows[i]["Quantity"] = dtDetails.Rows[i]["Quantity"];
                            dtTransDetails.Rows[i]["LineQuantity"] = salesLine.QUANTITY;
                            dtTransDetails.Rows[i]["Price"] = dtDetails.Rows[i]["Price"];
                            dtTransDetails.Rows[i]["UNITPRCE"] = salesLine.UNITPRCE;
                            dtTransDetails.Rows[i]["XTNDPRCE"] = salesLine.XTNDPRCE;
                            dtTransDetails.Rows[i]["Discount"] = dtDetails.Rows[i]["Discount"];
                            dtTransDetails.Rows[i]["MRKDNAMT"] = salesLine.MRKDNAMT;
                            dtTransDetails.Rows[i]["LineDiscount"] = LineDiscount;
                            dtTransDetails.Rows[i]["LineMisc"] = LineMisc;
                            dtTransDetails.Rows[i]["Tax"] = dtDetails.Rows[i]["Tax"];
                            dtTransDetails.Rows[i]["TAXAMNT"] = salesLine.TAXAMNT;
                        }

                        salesHdr.SUBTOTAL = TOTAL;
                        //salesHdr.TRDISAMT = 0;
                        salesHdr.TRDISAMT = MiscDiscount;
                        salesHdr.DOCAMNT = TOTAL - MiscDiscount;
                        if (SendTax)
                        {
                            salesHdr.TAXAMNT = TaxTotal;
                            salesHdr.DOCAMNT += salesHdr.TAXAMNT;
                        }

                        ExportSentTransactionDetails(dtTransDetails);

                        decimal AllowedVariance = dtDetails.Rows.Count * 0.01m;
                        if (Math.Abs(HeaderGross - (salesHdr.SUBTOTAL + DiscountTotal)) > AllowedVariance)
                        {
                            result = "Variance in Gross between header and details of [" + TransactionID + "] is high, Header value = " + HeaderGross + ", Details sum = " + (salesHdr.SUBTOTAL + DiscountTotal);
                            throw new Exception(result);
                        }
                        if (Math.Abs(HeaderTax - salesHdr.TAXAMNT) > AllowedVariance)
                        {
                            result = "Variance in Tax for header and details of [" + TransactionID + "] is high, Header value = " + HeaderTax + ", Details sum = " + salesHdr.TAXAMNT;
                            throw new Exception(result);
                        }
                        if (Math.Abs(HeaderNet - salesHdr.DOCAMNT) > AllowedVariance)
                        {
                            result = "Variance in Net for header and details of [" + TransactionID + "] is high, Header value = " + HeaderNet + ", Details sum = " + salesHdr.DOCAMNT;
                            throw new Exception(result);
                        }

                        salesOrder.taSopLineIvcInsert_Items = LineItems.ToArray();
                        salesOrder.taSopLineIvcTaxInsert_Items = TaxLines.ToArray();
                        salesOrder.taSopLotAuto_Items = LotLines.ToArray();
                        salesOrder.taSopHdrIvcInsert = salesHdr;

                        eConnect = new eConnectType();
                        SOPTransactionType[] MySopTransactionType = { salesOrder };
                        eConnect.SOPTransactionType = MySopTransactionType;
                        string salesOrderDocument;
                        string fname = filename + TransactionID.ToString().Trim() + ".xml";

                        //Create XML
                        FileStream fs = new FileStream(fname, FileMode.Create);
                        XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                        XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                        serializer.Serialize(writer, eConnect);
                        writer.Close();

                        //Call EConnect
                        eConnectMethods eConCall = new eConnectMethods();
                        XmlDocument xmldoc = new XmlDocument();
                        xmldoc.Load(fname);
                        salesOrderDocument = xmldoc.OuterXml;
                        eConCall.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, salesOrderDocument, EnumTypes.SchemaValidationType.None, "");

                        if (Field != IntegrationField.Orders_S)
                        {
                            //Set Synchronized Flag
                            inCubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE [Transaction] SET Synchronized = 1 WHERE TransactionID = '{0}' AND CustomerID = {1} AND OutletID = {2}", TransactionID, CustomerID, OutletID));
                            err = inCubeQuery.Execute();
                            //This query to set SALSTERR and SLPRSNID since sending to GP doesnt affect those fields
                            inCubeQuery = new InCubeQuery(db_ERP, string.Format("UPDATE SOP10200 SET SALSTERR = '{0}', SLPRSNID = '{1}' WHERE SOPNUMBE = '{2}' AND SOPTYPE = {3}", salesHdr.SALSTERR, salesHdr.SLPRSNID, TransactionID, SOPTYPE));
                            err = inCubeQuery.Execute();
                        }
                        else
                        {
                            //Set Synchronized Flag
                            inCubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE [SalesOrder] SET Synchronized = 1 WHERE OrderID = '{0}' AND CustomerID = {1} AND OutletID = {2}", TransactionID, CustomerID, OutletID));
                            err = inCubeQuery.Execute();
                        }

                        res = Result.Success;
                        result = "Success";
                        WriteMessage("Success ..");
                        totalSuccess++;
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, TransactionID, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        if (result == string.Empty)
                            result = "Failure";
                        if (ex.ToString().Contains("Error Description = Duplicate document number"))
                        {
                            result = "Already avaialble in GP, flag will be set to 1";
                            if (Field != IntegrationField.Orders_S)
                            {
                                //Set Synchronized Flag
                                inCubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE [Transaction] SET Synchronized = 1 WHERE TransactionID = '{0}' AND CustomerID = {1} AND OutletID = {2}", TransactionID, CustomerID, OutletID));
                                err = inCubeQuery.Execute();
                            }
                            else
                            {
                                //Set Synchronized Flag
                                inCubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE [SalesOrder] SET Synchronized = 1 WHERE OrderID = '{0}' AND CustomerID = {1} AND OutletID = {2}", TransactionID, CustomerID, OutletID));
                                err = inCubeQuery.Execute();
                            }
                        }
                        else if (ex.Message.Contains("he Quantity entered for this Lot is not available"))
                        {
                            result = "GP Stock posting error";
                        }
                        WriteMessage(result);
                        totalFailure++;
                        if (res == Result.UnKnown)
                        {
                            res = Result.Failure;
                        }
                    }
                    finally
                    {
                        execManager.LogIntegrationEnding(processID, res, "", result);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private void ExportSentTransactionDetails(DataTable dtTransDetails)
        {
            try
            {
                SqlBulkCopy bulk = new SqlBulkCopy(db_vms.GetConnection());
                bulk.DestinationTableName = "SentTransactionDetails";
                foreach (DataColumn col in dtTransDetails.Columns)
                    bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                bulk.BulkCopyTimeout = 120;
                bulk.WriteToServer(dtTransDetails);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public override void SendInvoices()
        {
            SendTransactionWithTax(CoreGeneral.Common.StartupPath + "\\E-Connect\\", Filters.EmployeeID == -1, Filters.EmployeeID.ToString(), Filters.FromDate, Filters.ToDate, IntegrationField.Sales_S, true);
            return;
        }
        
        #endregion

        #region SendReturns

        public override void SendReturn()
        {
            SendTransactionWithTax(CoreGeneral.Common.StartupPath + "\\E-Connect\\", Filters.EmployeeID == -1, Filters.EmployeeID.ToString(), Filters.FromDate, Filters.ToDate, IntegrationField.Returns_S, true);
        }
        
        #endregion

        #region SendReciepts

        public override void SendReciepts()
        {
            SerializeSalesOrderObjectSendPayment(CoreGeneral.Common.StartupPath + "\\E-Connect\\", Filters.EmployeeID == -1, Filters.EmployeeID.ToString(), Filters.FromDate, Filters.ToDate);
        }

        public int SerializeSalesOrderObjectSendPayment(string filename, bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate)
        {
            int ret = 0;
            WriteMessage("\r\n" + "Send Payment");


            //InCubeRow CustomerPaymentRow = new InCubeRow();
            //InCubeTable CustomerPayment = new InCubeTable();

            object field = new object();
            DateTime date;
            string PaymentID = "";
            //string TransactionID = "";
            string PaymentComment = "";
            string PaymentType = "";
            string BankID = "";
            decimal Amount = 0.0m;
            string ChNumber = "";
            DateTime ChDate;
            string CustomerCode = "";
            bool Check = false;
            object SalesPersonCode = "";
            object CustomerName = "";
            object EmployeeCode = "";

            //err = CustomerPayment.Open(db_vms, "CustomerPayment");
            RMCashReceiptsType CashType = new RMCashReceiptsType();

            string QueryString = @"SELECT 
                                         CustomerPayment.CustomerPaymentID AS PaymentID, 
                                         SUM(CustomerPayment.AppliedAmount) AS Amount, 
                                         PaymentTypeLanguage.PaymentTypeID AS PaymentType, 
                                         CustomerPayment.VoucherNumber, 
                                         CustomerPayment.VoucherDate, 
                                         CustomerPayment.BankID, 
                                         CustomerOutlet.CustomerCode, 
                                         CustomerPayment.PaymentDate,
                                         Employee.EmployeeCode,
                                         CustomerOutletLanguage.Description

                                   FROM  CustomerOutlet RIGHT OUTER JOIN
                                         CustomerPayment ON CustomerOutlet.OutletID = CustomerPayment.OutletID AND 
                                         CustomerOutlet.CustomerID = CustomerPayment.CustomerID LEFT OUTER JOIN
                                         PaymentTypeLanguage ON CustomerPayment.PaymentTypeID = PaymentTypeLanguage.PaymentTypeID INNER JOIN
                                         Employee ON CustomerPayment.EmployeeID = Employee.EmployeeID INNER JOIN
                                         CustomerOutletLanguage ON CustomerOutlet.CustomerID = CustomerOutletLanguage.CustomerID AND 
                                         CustomerOutlet.OutletID = CustomerOutletLanguage.OutletID 

                                   Where  CustomerPayment.PaymentTypeID <> 4 AND CustomerPayment.PaymentStatusID <> 5
                                          AND (PaymentTypeLanguage.LanguageID = 1) 
                                          AND (CustomerOutletLanguage.LanguageID = 1) 
                                          AND (CustomerPayment.Synchronized = 0) 
                                          AND (CustomerPayment.PaymentDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
                                          AND  CustomerPayment.PaymentDate <= '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"')";


            if (!AllSalespersons)
            {
                QueryString += "    AND CustomerPayment.EmployeeID = " + Salesperson;
            }


            QueryString += @"      GROUP BY   CustomerPayment.CustomerPaymentID, PaymentTypeLanguage.PaymentTypeID, PaymentTypeLanguage.LanguageID, 
                                              CustomerPayment.VoucherNumber, CustomerPayment.VoucherDate, CustomerPayment.BankID, CustomerOutlet.CustomerCode, 
                                              CustomerPayment.Synchronized, CustomerPayment.PaymentDate,Employee.EmployeeCode, CustomerPayment.PaymentTypeID,CustomerOutletLanguage.Description";



            InCubeQuery CustomerPaymentQuery = new InCubeQuery(db_vms, QueryString);

            err = CustomerPaymentQuery.Execute();
            err = CustomerPaymentQuery.FindFirst();
            while (err == InCubeErrors.Success)
            {
                //                                                                                                     0                                         1                                          2                                 3                               4                    5                           6                        7   
                try
                {
                    Check = false;
                    int CO = CustomerPaymentQuery.GetDataTable().Rows.Count;
                    err = CustomerPaymentQuery.GetField(0, ref field);
                    PaymentID = field.ToString();
                    err = CustomerPaymentQuery.GetField(1, ref field);
                    Amount = decimal.Parse(field.ToString());
                    err = CustomerPaymentQuery.GetField(2, ref field);
                    PaymentType = field.ToString();
                    err = CustomerPaymentQuery.GetField(3, ref field);
                    ChNumber = field.ToString();
                    if (ChNumber.Length > 12)
                        ChNumber = ChNumber.Substring(ChNumber.Length - 12, 12);

                    err = CustomerPaymentQuery.GetField(4, ref field);
                    if (field != null && !string.IsNullOrEmpty(field.ToString()) && DateTime.TryParse(field.ToString(), out ChDate))
                        ChNumber += "-" + ChDate.ToString("ddMMMyy");
                    err = CustomerPaymentQuery.GetField(5, ref field);
                    if (field.ToString() == "")
                    {
                        BankID = "";
                    }
                    else
                    {
                        BankID = field.ToString();
                    }
                    err = CustomerPaymentQuery.GetField(6, ref field);
                    CustomerCode = field.ToString();
                    if (CoreGeneral.Common.GeneralConfigurations.SiteSymbol.ToLower() == "esfnew")
                    {
                        string[] CustomerCodeParts = CustomerCode.Split(new char[] { '_' });
                        if (CustomerCodeParts.Length == 2)
                            CustomerCode = CustomerCodeParts[0];
                    }
                    err = CustomerPaymentQuery.GetField(7, ref field);
                    date = DateTime.Parse(field.ToString());

                    #region Payment Comment
                    err = CustomerPaymentQuery.GetField(8, ref SalesPersonCode);
                    PaymentComment = SalesPersonCode.ToString() + "-";

                    InCubeQuery qry = new InCubeQuery(db_vms, string.Format(@"IF ((SELECT COUNT(*) FROM CustomerPayment WHERE CustomerPaymentID = '{0}') = 1)
	SELECT TransactionID FROM CustomerPayment WHERE CustomerPaymentID = '{0}';
ELSE
	SELECT 'MULTIPLE';", PaymentID));
                    if (qry.ExecuteScalar(ref field) == InCubeErrors.Success)
                        PaymentComment += field.ToString();

                    if (PaymentComment.Length > 30)
                        PaymentComment = PaymentComment.Substring(PaymentComment.Length - 30, 30);
                    #endregion

                    err = CustomerPaymentQuery.GetField(9, ref CustomerName);

                    string cashCheckbookId = "ADCB-02";
                    string chequeCheckbookId = "ADCB-02-CHK";




                    if (PaymentType == "1")// "Cash"
                    {

                        #region Cash
                        Check = true;

                        taRMCashReceiptInsert CustomerPaymentCash = new taRMCashReceiptInsert();
                        CustomerPaymentCash.CUSTNMBR = CustomerCode;
                        CustomerPaymentCash.DOCNUMBR = PaymentID;
                        CustomerPaymentCash.ORTRXAMT = Amount;
                        CustomerPaymentCash.GLPOSTDT = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.DOCDATE = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.CSHRCTYP = 1;//0=check , 1=cash , 2= credit card
                        CustomerPaymentCash.CHEKBKID = cashCheckbookId; //"DX -CASH";
                        CustomerPaymentCash.CURNCYID = "DHS";
                        string Batch = "RV" + date.ToString("dd/MM/yy").Replace("/", "") + SalesPersonCode.ToString();
                        CustomerPaymentCash.BACHNUMB = Batch;
                        CustomerPaymentCash.CURNCYID = "";
                        CustomerPaymentCash.CREATEDIST = 1;

                        CustomerPaymentCash.TRXDSCRN = PaymentComment;
                        eConnectType eConnect = new eConnectType();
                        //DataRow[] DetailsPaymentRow = CustomerPayment.GetDataTable().Select("CustomerPaymentID='" + PaymentID + "'");
                        //RMApplyType[] TYPE = new RMApplyType[DetailsPaymentRow.Length];
                        //for (int i = 0; i < DetailsPaymentRow.Length; i++)
                        //{
                        //    taRMApply RMApply = new taRMApply();
                        //    RMApplyType ApplyType = new RMApplyType();
                        //    DataRow Row = DetailsPaymentRow[i];
                        //    RMApply.APTODCNM = Row["TransactionID"].ToString();
                        //    RMApply.APFRDCNM = PaymentID.ToString();
                        //    RMApply.APPTOAMT = decimal.Parse(Row["AppliedAmount"].ToString());
                        //    RMApply.APFRDCTY = 9;
                        //    RMApply.APTODCTY = 1;
                        //    RMApply.APPLYDATE = date.ToString("yyyy-MM-dd");
                        //    RMApply.GLPOSTDT = DateTime.Now.ToString("yyyy-MM-dd");
                        //    ApplyType.taRMApply = RMApply;
                        //    TYPE[i] = ApplyType;

                        //}
                        //eConnect.RMApplyType = TYPE;
                        CashType.taRMCashReceiptInsert = CustomerPaymentCash;
                        RMCashReceiptsType[] CashReceipts = { CashType };
                        eConnect.RMCashReceiptsType = CashReceipts;
                        string fname = filename + PaymentID.ToString().Trim() + ".xml";
                        FileStream fs = new FileStream(fname, FileMode.Create);
                        XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                        XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                        serializer.Serialize(writer, eConnect);
                        writer.Close();
                        eConnectMethods eConCall1 = new eConnectMethods();
                        string salesOrderDocument = "";
                        XmlDocument xmldoc = new XmlDocument();
                        xmldoc.Load(fname);
                        salesOrderDocument = xmldoc.OuterXml;
                        try
                        {
                            eConCall1.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, salesOrderDocument, EnumTypes.SchemaValidationType.None, "");
                        }
                        catch (eConnectException exp)
                        {
                            Check = false;
                            Console.Write(exp.ToString());
                            StreamWriter wrt1 = new StreamWriter("errorPay.log", true);
                            wrt1.Write(exp.ToString());
                            wrt1.Close();
                        }
                        #endregion
                    }
                    else if (PaymentType == "2")// "Current Dated Cheque"
                    {
                        #region Current Dated Cheque
                        Check = true;

                        taRMCashReceiptInsert CustomerPaymentCash = new taRMCashReceiptInsert();
                        CustomerPaymentCash.CUSTNMBR = CustomerCode;
                        CustomerPaymentCash.DOCNUMBR = PaymentID;
                        CustomerPaymentCash.ORTRXAMT = Amount;
                        CustomerPaymentCash.GLPOSTDT = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.DOCDATE = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.CSHRCTYP = 0;
                        CustomerPaymentCash.CURNCYID = "DHS";
                        string Batch = "RV" + date.ToString("dd/MM/yy").Replace("/", "") + SalesPersonCode.ToString();
                        CustomerPaymentCash.BACHNUMB = Batch;
                        CustomerPaymentCash.CURNCYID = "";
                        CustomerPaymentCash.CREATEDIST = 1;
                        CustomerPaymentCash.TRXDSCRN = PaymentComment;
                        CustomerPaymentCash.CHEKNMBR = ChNumber;
                        CustomerPaymentCash.CHEKBKID = chequeCheckbookId;
                        eConnectType eConnect = new eConnectType();
                        //DataRow[] DetailsPaymentRow = CustomerPayment.GetDataTable().Select("CustomerPaymentID='" + PaymentID + "'");
                        //RMApplyType[] TYPE = new RMApplyType[DetailsPaymentRow.Length];
                        //for (int i = 0; i < DetailsPaymentRow.Length; i++)
                        //{
                        //    taRMApply RMApply = new taRMApply();
                        //    RMApplyType ApplyType = new RMApplyType();
                        //    DataRow Row = DetailsPaymentRow[i];
                        //    RMApply.APTODCNM = Row["TransactionID"].ToString();
                        //    RMApply.APFRDCNM = PaymentID.ToString();
                        //    RMApply.APPTOAMT = decimal.Parse(Row["AppliedAmount"].ToString());
                        //    RMApply.APFRDCTY = 9;
                        //    RMApply.APTODCTY = 1;
                        //    RMApply.APPLYDATE = date.ToString("yyyy-MM-dd");
                        //    RMApply.GLPOSTDT = DateTime.Now.ToString("yyyy-MM-dd");
                        //    ApplyType.taRMApply = RMApply;
                        //    TYPE[i] = ApplyType;
                        //}
                        //eConnect.RMApplyType = TYPE;
                        CashType.taRMCashReceiptInsert = CustomerPaymentCash;
                        RMCashReceiptsType[] CashReceipts = { CashType };
                        eConnect.RMCashReceiptsType = CashReceipts;
                        string fname = filename + PaymentID.ToString().Trim() + ".xml";
                        FileStream fs = new FileStream(fname, FileMode.Create);
                        XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                        // Serialize using the XmlTextWriter.
                        XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                        serializer.Serialize(writer, eConnect);
                        writer.Close();
                        eConnectMethods eConCall1 = new eConnectMethods();
                        string salesOrderDocument = "";
                        XmlDocument xmldoc = new XmlDocument();
                        xmldoc.Load(fname);
                        salesOrderDocument = xmldoc.OuterXml;
                        try
                        {
                            eConCall1.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, salesOrderDocument, EnumTypes.SchemaValidationType.None, "");
                        }
                        catch (eConnectException exp)
                        {
                            Check = false;
                            Console.Write(exp.ToString());
                            StreamWriter wrt5 = new StreamWriter("errorPay.log", true);
                            wrt5.Write(exp.ToString());
                            wrt5.Close();
                        }
                        #endregion
                    }
                    //else if (PaymentType == "4")// "Credit Note"
                    //{
                    //    #region Apply Return to Invoices
                    //    Check = true;
                    //    eConnectType eConnect = new eConnectType();
                    //    DataRow[] DetailsPaymentRow = CustomerPayment.GetDataTable().Select("CustomerPaymentID='" + PaymentID + "'");
                    //    RMApplyType[] TYPE = new RMApplyType[DetailsPaymentRow.Length];
                    //    string Batch = "RV" + date.ToString("dd/MM/yy").Replace("/", "") + SalesPersonCode.ToString();
                    //    CustomerPaymentCash.BACHNUMB = Batch;
                    //    for (int i = 0; i < DetailsPaymentRow.Length; i++)
                    //    {
                    //        taRMApply RMApply = new taRMApply();
                    //        RMApplyType ApplyType = new RMApplyType();
                    //        DataRow Row = DetailsPaymentRow[i];
                    //        RMApply.APTODCNM = Row["TransactionID"].ToString();
                    //        RMApply.APFRDCNM = Row["VoucherNmuber"].ToString();
                    //        RMApply.APPTOAMT = decimal.Parse(Row["RemainingAmount"].ToString());
                    //        RMApply.APFRDCTY = 8;
                    //        RMApply.APTODCTY = 1;
                    //        RMApply.APPLYDATE = date.ToString("yyyy-MM-dd");
                    //        RMApply.GLPOSTDT = DateTime.Now.ToString("yyyy-MM-dd");
                    //        ApplyType.taRMApply = RMApply;
                    //        TYPE[i] = ApplyType;
                    //    }
                    //    eConnect.RMApplyType = TYPE;
                    //    string fname = filename + PaymentID.ToString().Trim() + ".xml";
                    //    FileStream fs = new FileStream(fname, FileMode.Create);
                    //    XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                    //    XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                    //    serializer.Serialize(writer, eConnect);
                    //    writer.Close();
                    //    eConnectMethods eConCall1 = new eConnectMethods();
                    //    string salesOrderDocument = "";
                    //    XmlDocument xmldoc = new XmlDocument();
                    //    xmldoc.Load(fname);
                    //    salesOrderDocument = xmldoc.OuterXml;
                    //    try
                    //    {
                    //        eConCall1.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, salesOrderDocument, EnumTypes.SchemaValidationType.None, "");
                    //    }
                    //    catch (eConnectException exp)
                    //    {
                    //        Check = false;
                    //        Console.Write(exp.ToString());
                    //        StreamWriter wrt5 = new StreamWriter("errorPay.log", true);
                    //        wrt5.Write(exp.ToString());
                    //        wrt5.Close();
                    //    }

                    //    #endregion
                    //}
                    else if (PaymentType == "3")// "Post Dated Cheque"
                    {
                        #region Post Dated Cheque
                        //                        Check = true;

                        //                        //PDCRVNO : Receipt Number
                        //                        //PDCRVDT: Receipt Date (due date of the PDC) 
                        //                        //PDCCUSTID: Customer Code
                        //                        //PDCDOCNO: ?? Cheque no
                        //                        //PDCDOCDT: ?? Receipt Date
                        //                        //PDCBANK: BankID 
                        //                        //PDCAMOUNT: PDC Amount
                        //                        //PDCONHANDDATE:  – collected date (current date)

                        //                        string Query = @"Insert into PDCOPEN (PDCRVNO, PDCRVDT, PDCCUSTID, PDCDOCNO, PDCDOCDT, PDCBANK, PDCAMOUNT, PDCONHANDDATE) 
                        //                        values ('" + PaymentID + "', '" + date.ToString("yyyy-MM-dd") + "', '" + CustomerCode + "', '" + ChNumber + "', '" + date.ToString("yyyy-MM-dd") + "', '" + BankID + "', " + Amount + ", '" + DateTime.Now.ToString("yyyy-MM-dd") + "')";

                        //                        InCubeQuery PDCOPEN = new InCubeQuery(db_ERP, Query);
                        //                        err = PDCOPEN.Execute();

                        //                        MessageBox.Show("Insert into PDCOPEN " + err.ToString() + " Exception :" + PDCOPEN.GetCurrentException().ToString() + " Query : " + Query);

                        //                        //BranchID: 3 letter code for branch (DXB,AUH)
                        //                        //ReceiptNo: Receipt Number.
                        //                        //ReceiptDate: Receipt Date. This is NOT the current date. It’s the cheque date (PDC)
                        //                        //CustomerID: Customer Code
                        //                        //CustomerName: Customer Name.
                        //                        //CheckBookID: Chequebookid
                        //                        //CurrencyID: its fixed "DHS"
                        //                        //Bank: Bankid
                        //                        //ChequeNumber: Cheque Number.
                        //                        //DueDate:Receipt Date
                        //                        //Amount: PDC Amount
                        //                        //Status: 0 (zero)
                        //                        //PrintCount: always 0 
                        //                        //SPID: SalespersonID (S100, S200)

                        //                        string Branch = "DXB";
                        //                        InCubeQuery GetBranchCMD = new InCubeQuery(db_ERP, @"Select SALSTERR From RM00101 Where CUSTNMBR = '" + CustomerCode + "'");
                        //                        GetBranchCMD.Execute();
                        //                        GetBranchCMD.FindFirst();
                        //                        GetBranchCMD.GetField(0, ref field);
                        //                        Branch = field.ToString();

                        //                        Query = @"Insert into PDCDetail (BranchID, ReceiptNo, ReceiptDate, CustomerID, CustomerName, CheckBookID, CurrencyID, Bank, ChequeNumber, DueDate, Amount, Status, PrintCount, SPID) 
                        //                        Values ('" + Branch + "', '" + PaymentID + "', '" + date.ToString("yyyy-MM-dd") + "', '" + CustomerCode + "', '" + CustomerName + "', '" + chequeCheckbookId + "', 'DHS', '" + BankID + "', '" + ChNumber + "', '" + date.ToString("yyyy-MM-dd") + "', " + Amount + ", 0, 0, '" + SalesPersonCode.ToString() + "')";

                        //                        InCubeQuery PDCDETAIL = new InCubeQuery(db_ERP, Query);
                        //                        err = PDCDETAIL.Execute();

                        //                        MessageBox.Show("Insert into PDCOPEN " + err.ToString() + " Exception :" + PDCOPEN.GetCurrentException().ToString() + " Query : " + Query);

                        #endregion

                        #region Current Dated Cheque
                        Check = true;

                        taRMCashReceiptInsert CustomerPaymentCash = new taRMCashReceiptInsert();
                        CustomerPaymentCash.CUSTNMBR = CustomerCode;
                        CustomerPaymentCash.DOCNUMBR = PaymentID;
                        CustomerPaymentCash.ORTRXAMT = Amount;
                        CustomerPaymentCash.GLPOSTDT = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.DOCDATE = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.CSHRCTYP = 0;
                        CustomerPaymentCash.CURNCYID = "DHS";
                        //string Batch = "RV" + date.ToString("dd/MM/yy").Replace("/", "");//
                        CustomerPaymentCash.BACHNUMB = "PDC-" + DateTime.Now.ToString("MMM") + GetSalespersonCode(SalesPersonCode);
                        CustomerPaymentCash.CURNCYID = "";
                        CustomerPaymentCash.CREATEDIST = 1;
                        CustomerPaymentCash.TRXDSCRN = PaymentComment;
                        CustomerPaymentCash.CHEKNMBR = ChNumber;
                        CustomerPaymentCash.CHEKBKID = chequeCheckbookId;
                        eConnectType eConnect = new eConnectType();
                        //DataRow[] DetailsPaymentRow = CustomerPayment.GetDataTable().Select("CustomerPaymentID='" + PaymentID + "'");
                        //RMApplyType[] TYPE = new RMApplyType[DetailsPaymentRow.Length];
                        //for (int i = 0; i < DetailsPaymentRow.Length; i++)
                        //{
                        //    taRMApply RMApply = new taRMApply();
                        //    RMApplyType ApplyType = new RMApplyType();
                        //    DataRow Row = DetailsPaymentRow[i];
                        //    RMApply.APTODCNM = Row["TransactionID"].ToString();
                        //    RMApply.APFRDCNM = PaymentID.ToString();
                        //    RMApply.APPTOAMT = decimal.Parse(Row["RemainingAmount"].ToString());
                        //    RMApply.APFRDCTY = 9;
                        //    RMApply.APTODCTY = 1;
                        //    RMApply.APPLYDATE = date.ToString("yyyy-MM-dd");
                        //    RMApply.GLPOSTDT = DateTime.Now.ToString("yyyy-MM-dd");
                        //    ApplyType.taRMApply = RMApply;
                        //    TYPE[i] = ApplyType;
                        //}
                        //eConnect.RMApplyType = TYPE;
                        CashType.taRMCashReceiptInsert = CustomerPaymentCash;
                        RMCashReceiptsType[] CashReceipts = { CashType };
                        eConnect.RMCashReceiptsType = CashReceipts;
                        string fname = filename + PaymentID.ToString().Trim() + ".xml";
                        FileStream fs = new FileStream(fname, FileMode.Create);
                        XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                        // Serialize using the XmlTextWriter.
                        XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                        serializer.Serialize(writer, eConnect);
                        writer.Close();
                        eConnectMethods eConCall1 = new eConnectMethods();
                        string salesOrderDocument = "";
                        XmlDocument xmldoc = new XmlDocument();
                        xmldoc.Load(fname);
                        salesOrderDocument = xmldoc.OuterXml;
                        try
                        {
                            eConCall1.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, salesOrderDocument, EnumTypes.SchemaValidationType.None, "");
                        }
                        catch (eConnectException exp)
                        {
                            Check = false;
                            Console.Write(exp.ToString());
                            StreamWriter wrt5 = new StreamWriter("errorPay.log", true);
                            wrt5.Write(exp.ToString());
                            wrt5.Close();
                        }
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    Check = false;
                    StreamWriter wrt = new StreamWriter("errorPay.log", true);
                    wrt.Write(ex.ToString());
                    wrt.Close();
                }
                finally
                {
                    if (Check)
                    {
                        StreamWriter wrt4 = new StreamWriter("errorPay.log", true);
                        wrt4.Write(PaymentID.ToString() + " - OK\r\n");
                        wrt4.Close();
                        InCubeQuery UpdateQuery = new InCubeQuery(db_vms, "update  CustomerPayment set Synchronized=1 where CustomerPaymentID='" + PaymentID + "'");
                        err = UpdateQuery.Execute();
                        WriteMessage("\r\n" + PaymentID.ToString() + " - OK");
                    }
                    else
                    {
                        if (PaymentType != "Post Dated Cheque")
                        {
                            WriteMessage("\r\n" + PaymentID.ToString() + " - FAILED!");
                            ret++;
                        }
                        else
                        {
                            WriteMessage("\r\n" + PaymentID.ToString() + " - PDC (Skipped)");
                        }
                    }
                }
                err = CustomerPaymentQuery.FindNext();
            }

            //CustomerPayment.Close();
            return ret;
        }

        #endregion

        #region SendTransfers

        public override void SendTransfers()
        {
            SerializeSalesOrderObjectSendTransfers(CoreGeneral.Common.StartupPath + "\\E-Connect\\", Filters.EmployeeID == -1, Filters.EmployeeID.ToString(), Filters.FromDate, Filters.ToDate);
        }

        public int SerializeSalesOrderObjectSendTransfers(string filename, bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate)
        {

            //taIVTransferHeaderInsert

            //BACHNUMB       //Batch number
            //IVDOCNBR       //IV document number
            //DOCDATE        // Document date
            //POSTTOGL       //Post to GL: 0=False; 1=True


            //taIVTransferLineInsert

            //IVDOCNBR       //Document number
            //ITEMNMBR       //Item number
            //TRXQTY         //Transaction quantity
            //TRXLOCTN       //Transaction location
            //TRNSTLOC       //Transfer to location

            WriteMessage("\r\n" + "Sending Transfers");


            IVInventoryTransferType TransferTransaction = new IVInventoryTransferType();
            taIVTransferHeaderInsert TransferHeader = new taIVTransferHeaderInsert();
            taIVTransferLineInsert_ItemsTaIVTransferLineInsert TransferDetails = new taIVTransferLineInsert_ItemsTaIVTransferLineInsert();

            object BatchNo = "";
            object TransferID = "";
            object TransferDate = "";
            object POSTED = "0";
            object TransferFromWarhouse = "";
            object TransferToWarhouse = "";

            string QueryString = @"SELECT 
WarehouseTransaction.TransactionID,
WarehouseTransaction.TransactionDate,
(case(WarehouseTransaction.TransactionTypeID ) when 1 then Warehouse.WarehouseCode else Warehouse_1.WarehouseCode end)AS ToWh,
(case(WarehouseTransaction.TransactionTypeID ) when 2 then Warehouse.WarehouseCode else Warehouse_1.WarehouseCode end)AS FromWh

FROM WarehouseTransaction INNER JOIN
Warehouse ON WarehouseTransaction.WarehouseID = Warehouse.WarehouseID INNER JOIN
Warehouse AS Warehouse_1 ON WarehouseTransaction.RefWarehouseID = Warehouse_1.WarehouseID

Where 
     WarehouseTransaction.Synchronized = 0 
AND (WarehouseTransaction.TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
AND  WarehouseTransaction.TransactionDate <= '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"') 
AND ( WarehouseTransaction.TransactionTypeID = 1 or WarehouseTransaction.TransactionTypeID = 2)";

            if (!AllSalespersons)
            {
                QueryString += " AND RequestedBy = " + Salesperson;
            }

            InCubeQuery GetTransferInformation = new InCubeQuery(db_vms, QueryString);

            err = GetTransferInformation.Execute();
            err = GetTransferInformation.FindFirst();

            while (err == InCubeErrors.Success)
            {
                try
                {
                    object field = new object();

                    #region Get Transfer Information
                    {
                        err = GetTransferInformation.GetField(0, ref TransferID);
                        err = GetTransferInformation.GetField(1, ref TransferDate);
                        err = GetTransferInformation.GetField(2, ref TransferToWarhouse);
                        err = GetTransferInformation.GetField(3, ref TransferFromWarhouse);
                    }
                    #endregion


                    string STATE = "BCH"; //= GetFieldValue("ALNIntegration", "IntLocation", " ColdStore = '" + TransferFromWarhouse + "'", db_ERP);

                    string TransferDetailsQuery = @"SELECT WhTransDetail.Quantity, Item.ItemCode Barcode,BatchNo,ExpiryDate,WhTransDetail.PackID 
FROM WhTransDetail INNER JOIN
Pack ON WhTransDetail.PackID = Pack.PackID INNER JOIN
Item ON Pack.ItemID = Item.ItemID
Where
WhTransDetail.TransactionID = '" + TransferID + "'";

                    InCubeQuery TransferDetailQuery = new InCubeQuery(db_vms, TransferDetailsQuery);
                    TransferDetailQuery.Execute();

                    DataTable TransferDetailTable = TransferDetailQuery.GetDataTable();

                    int count = TransferDetailTable.Rows.Count;

                    List<taIVTransferLineInsert_ItemsTaIVTransferLineInsert> TransferLineItemsList = new List<taIVTransferLineInsert_ItemsTaIVTransferLineInsert>();
                    List<taIVTransferLotInsert_ItemsTaIVTransferLotInsert> transLotList = new List<taIVTransferLotInsert_ItemsTaIVTransferLotInsert>();
                    ClearProgress();
                    SetProgressMax(count);


                    foreach (DataRow Row in TransferDetailTable.Rows)
                    {
                        ReportProgress("Sending Transfers");

                        decimal Quantity = decimal.Parse(Row["Quantity"].ToString());
                        string Barcode = Row["Barcode"].ToString();
                        string Batch = DateTime.Now.ToString("yyyyMMddhhmmss");// Row["BatchNo"].ToString();
                        string expiryDate = Row["expiryDate"].ToString();

                        //string packCode = salesTxRow["PackName"].ToString().Trim();
                        //string packID = salesTxRow["PackID"].ToString().Trim();
                        //decimal baseQty = 0;
                        //GetBaseQuantity(packID, Quantity, ref packCode, ref baseQty);
                        //Quantity = baseQty;

                        taIVTransferLineInsert_ItemsTaIVTransferLineInsert TransferLineItem = new taIVTransferLineInsert_ItemsTaIVTransferLineInsert();
                        //taIVTransferLotInsert_ItemsTaIVTransferLotInsert transLot = new taIVTransferLotInsert_ItemsTaIVTransferLotInsert();
                        TransferLineItem.IVDOCNBR = TransferID.ToString();
                        TransferLineItem.ITEMNMBR = Barcode;
                        TransferLineItem.TRXQTY = Quantity;
                        TransferLineItem.TRXLOCTN = TransferFromWarhouse.ToString();
                        TransferLineItem.TRNSTLOC = TransferToWarhouse.ToString();

                        //transLot.ITEMNMBR = TransferLineItem.ITEMNMBR;
                        //transLot.AUTOCREATELOT = 0;
                        //transLot.LOCNCODE = TransferLineItem.TRXLOCTN;
                        //transLot.SERLTQTY = TransferLineItem.TRXQTY;
                        //transLot.IVDOCNBR = TransferLineItem.IVDOCNBR;
                        //transLot.TOLOCNCODE = TransferLineItem.TRNSTLOC;
                        //transLot.LOTNUMBR = Batch;
                        //transLot.EXPNDATE = expiryDate;

                        TransferLineItemsList.Add(TransferLineItem);
                        GetLotFromGP(ref transLotList, TransferLineItem.IVDOCNBR, TransferFromWarhouse.ToString(), TransferToWarhouse.ToString(), TransferLineItem.ITEMNMBR, "", Quantity);
                        //transLotList.Add(transLot);
                    }


                    TransferHeader.BACHNUMB = STATE + "_" + DateTime.Parse(TransferDate.ToString()).ToString("ddMMyy");
                    TransferHeader.IVDOCNBR = TransferID.ToString();
                    TransferHeader.DOCDATE = DateTime.Parse(TransferDate.ToString()).ToString("yyyy-MM-dd");
                    TransferHeader.POSTTOGL = 0;


                    TransferTransaction.taIVTransferHeaderInsert = TransferHeader;
                    TransferTransaction.taIVTransferLineInsert_Items = TransferLineItemsList.ToArray();
                    TransferTransaction.taIVTransferLotInsert_Items = transLotList.ToArray();

                    eConnectType eConnect = new eConnectType();
                    IVInventoryTransferType[] MyIVInventoryTransferType = { TransferTransaction };
                    eConnect.IVInventoryTransferType = MyIVInventoryTransferType;

                    string TransferDocument;

                    #region Create xml file
                    {
                        string fname = filename + TransferID.ToString().Trim() + ".xml";
                        FileStream fs = new FileStream(fname, FileMode.Create);
                        XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                        XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                        serializer.Serialize(writer, eConnect);
                        writer.Close();

                        XmlDocument xmldoc = new XmlDocument();
                        xmldoc.Load(fname);
                        TransferDocument = xmldoc.OuterXml;
                    }
                    #endregion

                    eConnectMethods eConCall = new eConnectMethods();


                    #region  Send xml file to GPs

                    eConCall.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, TransferDocument, EnumTypes.SchemaValidationType.None, "");

                    #endregion

                    InCubeQuery UpdateQuery = new InCubeQuery(db_vms, "Update WarehouseTransaction SET Synchronized = 1 where TransactionID = '" + TransferID + "'");
                    err = UpdateQuery.Execute();
                    if (err == InCubeErrors.Success)
                    {
                        InCubeQuery INSERT_GP = new InCubeQuery(db_ERP, "INSERT INTO INCUBEINTEGRATION(TRANSACTIONID,INTEGRATIONDATE) VALUES ('" + TransferID + "',GETDATE())");
                        err = INSERT_GP.ExecuteNonQuery();
                    }
                    WriteMessage("\r\n" + TransferID.ToString() + " - OK");
                    StreamWriter wrt = new StreamWriter("errortrf.log", true);
                    wrt.Write("\n" + TransferID.ToString() + " OK\r\n");
                    wrt.Close();
                }//end try
                catch (Exception ex)
                {
                    StreamWriter wrt = new StreamWriter("errortrf.log", true);
                    wrt.Write(ex.ToString());
                    wrt.Close();
                    WriteMessage("\r\n" + TransferID.ToString() + " - FAILED!");
                }
                err = GetTransferInformation.FindNext();
            }

            return 0;
        }

        #endregion

        public override void SendOrders()
        {
            SendTransactionWithTax(CoreGeneral.Common.StartupPath + "\\E-Connect\\", Filters.EmployeeID == -1, Filters.EmployeeID.ToString(), Filters.FromDate, Filters.ToDate, IntegrationField.Orders_S, true);
        }

        #region New Customers
        public override void SendNewCustomers()
        {
            try
            {
                DataTable dt = new DataTable();
                string getCustomers = string.Format(@"select CO.CustomerCode CustomerOutletCode,COL.Description as CustomerOutletName,
(case (CO.CustomerTypeID) when 1 then 0 else case (CO.CustomerTypeID) when 2 then 1 else 9 end end )as CustomerType,
 (case when(D.DivisionCode) is null then 'lgt' else D.DivisionCode end )as DivisionCode,
 (case when(DL.Description) is null then 'lgt' else DL.Description end ) as DivisionName,
COL.Address,ConL.Description as ContactName,CO.Phone as Telephone,CO.Phone as Mobile,E.EmployeeCode as SalesManCode,EL.description as SalesmanName,
Ter.TerritoryCode as RouteCode
from RouteNewCustomer RC
inner join RouteHistory RH on RC.RouteHistoryID=RH.RouteHistoryID
inner join CustomerOutlet CO on RC.NewCustomerID=CO.CustomerID
left outer join Contact Con on Con.customerID=CO.CustomerID
left outer join ContactLanguage ConL on ConL.ContactID=Con.ContactID and ConL.LanguageID=1
inner join CustomerOutletLanguage COL on CO.CustomerID=COL.CustomerID and CO.OutletID=COL.OutletID and COL.LanguageID=1
left outer join CustomerOutletDivision CD on CO.CustomerID=CD.CustomerID and CO.OutletID=CD.OutletID 
left outer join Division D on CD.DivisionID=D.DivisionID 
left outer join DivisionLAnguage DL on D.DivisionID=DL.DivisionID and DL.LanguageID=1
inner join Employee E on RH.EmployeeID=E.EmployeeID
inner join EmployeeLanguage EL on E.EmployeeID=EL.EmployeeID and EL.LanguageID=1
inner join Territory Ter on RH.TerritoryID=Ter.TerritoryID
inner join CustomerTypeLanguage CTL on CTL.CustomerTypeID=CO.CustomerTypeID and CTL.LanguageID=1
where CO.BlockNumber is null

");
                InCubeQuery transferQry = new InCubeQuery(getCustomers, db_vms);
                transferQry.Execute();
                dt = transferQry.GetDataTable();
                DataTable detailDT = new DataTable();
                ClearProgress();
                SetProgressMax(dt.Rows.Count);

                foreach (DataRow dr in dt.Rows)
                {
                    ReportProgress("Sending New Customers");

                    string CustomerOutletCode = dr["CustomerOutletCode"].ToString().Trim();
                    string CustomerOutletName = dr["CustomerOutletName"].ToString().Trim();
                    string CustomerType = dr["CustomerType"].ToString().Trim();
                    string DivisionCode = dr["DivisionCode"].ToString().Trim();
                    string DivisionName = dr["DivisionName"].ToString().Trim();
                    string Address = dr["Address"].ToString().Trim();
                    string ContactName = dr["ContactName"].ToString().Trim();
                    string Telephone = dr["Telephone"].ToString().Trim();
                    string Mobile = dr["Mobile"].ToString().Trim();
                    string SalesManCode = dr["SalesManCode"].ToString().Trim();
                    string SalesManName = dr["SalesManName"].ToString().Trim();
                    string RouteCode = dr["RouteCode"].ToString().Trim();

                    string checkCustInAX = GetFieldValue("IN_NewCustCreation", "CustomerOutletCode", "CustomerOutletCode='" + CustomerOutletCode + "'", db_ERP).Trim();
                    if (checkCustInAX.Equals(string.Empty))
                    {

                        string insertNew = string.Format(@" insert into IN_NewCustCreation
(
CustomerOutletCode,
CustomerOutletName,
CustomerType,
DivisionCode,
DivisionName,
Address,
ContactName,
Telephone,
Mobile,
SalesManCode,
SalesManName,
RouteCode,
Flag
) 
Values
(
'{0}',
'{1}',
'{2}',
'{3}',
'{4}',
'{5}',
'{6}',
'{7}',
'{8}',
'{9}',
'{10}',
'{11}',0
)
", CustomerOutletCode,
    CustomerOutletName,
    CustomerType,
    "0",
    "ESF",
    Address,
    ContactName,
    Telephone,
    Mobile,
    SalesManCode,
    SalesManName,
    RouteCode);

                        InCubeQuery qry = new InCubeQuery(insertNew, db_ERP);
                        err = qry.ExecuteNonQuery();
                        if (err == InCubeErrors.Success)
                        {
                            string updateBlockNumber = string.Format("update CustomerOutlet set BlockNumber='1' where CustomerCode='{0}'", CustomerOutletCode);
                            InCubeQuery AX_Qry = new InCubeQuery(updateBlockNumber, db_vms);
                            err = AX_Qry.ExecuteNonQuery();
                        }
                        else
                        {

                        }
                    }

                }


            }
            catch
            {

            }
        }
        #endregion

        #region Update OutStanding
        public override void OutStanding()
        {
            try
            {
                WriteMessage("Updating Outstanding ...");
                InCubeQuery qry = new InCubeQuery(db_vms, "UpdateOutstanding");
                err = qry.ExecuteStoredProcedure();
                WriteMessage("Result: " + err.ToString());
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        #endregion
        
        private void GetBaseQuantity_RET(int PackID, decimal Quantity, ref string baseUOM, ref decimal baseQuantity, ref decimal BaseUOMPrice, ref decimal XtndPrice)
        {

            try
            {
                string itemID = GetFieldValue("Pack", "ItemID", "PackID=" + PackID + "", db_vms).Trim();
                decimal PackQuantity = decimal.Parse(GetFieldValue("Pack", "Quantity", "PackID=" + PackID + "", db_vms).Trim());
                decimal BiggestQuantity = decimal.Parse(GetFieldValue("Pack", "top(1) Quantity", "ItemID=" + itemID + " order by Quantity Desc", db_vms).Trim());
                baseQuantity = Quantity * PackQuantity / BiggestQuantity;
                baseQuantity = decimal.Round(baseQuantity, 5);
                string PackTypeID = GetFieldValue("Pack", "top(1) PackTypeID", "ItemID=" + itemID + " order by Quantity Desc", db_vms).Trim();
                string UOM = GetFieldValue("PackTypeLanguage", "Description", "PackTypeID=" + PackTypeID + " and languageID=1", db_vms).Trim();
                baseUOM = UOM;
                BaseUOMPrice = BaseUOMPrice * BiggestQuantity / PackQuantity;
                BaseUOMPrice = decimal.Round(BaseUOMPrice, 2, MidpointRounding.AwayFromZero);
                XtndPrice = baseQuantity * BaseUOMPrice;
                XtndPrice = decimal.Round(XtndPrice, 2, MidpointRounding.AwayFromZero);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }


        }
        private InCubeErrors UpdateFlag(string TableName, string Criteria)
        {
            try
            {
                string query = string.Format("UPDATE {0} SET Flag = '1' WHERE {1}", TableName, Criteria);
                InCubeQuery qry = new InCubeQuery(query, db_ERP_GET);
                err = qry.ExecuteNonQuery();
                return err;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return err;
        }
        private void AccounteRecursiveFunction(int accountID, decimal balance, bool IsIncrease)
        {
            try
            {
                string update = string.Empty;
                if (accountID == 0) return;
                if (IsIncrease)
                {
                    update = string.Format("update Account set Balance =Balance+" + balance.ToString() + " where AccountID=" + accountID + "");
                }
                else
                {
                    update = string.Format("update Account set Balance =Balance-" + balance.ToString() + " where AccountID=" + accountID + "");
                }
                InCubeQuery qry = new InCubeQuery(update, db_vms);
                err = qry.ExecuteNonQuery();
                accountID = int.Parse(GetFieldValue("Account", "isnull(ParentAccountID,0)", "AccountID=" + accountID + "", db_vms).Trim());
                AccounteRecursiveFunction(accountID, balance, IsIncrease);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }


        }
        private string GetSalespersonCode(object SalesPersonCode)
        {
            object field = "";
            InCubeQuery query = new InCubeQuery(db_vms, "select EmployeeCode from Employee where EmployeeID =" + SalesPersonCode + "");
            err = query.Execute();
            err = query.FindFirst();
            if (err == InCubeErrors.Success)
            {
                err = query.GetField(0, ref field);
            }
            return field.ToString();
        }
        private void GetLotFromGP(ref List<taIVTransferLotInsert_ItemsTaIVTransferLotInsert> GP_Lot_List, string trxNo, string FromLocation, string ToLocation, string ItemCode, string UOM, decimal Quantity)
        {
            taIVTransferLotInsert_ItemsTaIVTransferLotInsert transLot = new taIVTransferLotInsert_ItemsTaIVTransferLotInsert();

            try
            {
                string select = string.Format(@"SELECT I.ITEMNMBR,V.EXPNDATE,V.LOTNUMBR, SUM(I.QTYONHND) GP_Qty,NULL,0,SUM(I.QTYONHND),-1 
FROM IV00102 I 
INNER JOIN IV00300 V ON I.ITEMNMBR=V.ITEMNMBR AND I.LOCNCODE=V.LOCNCODE
INNER JOIN IV00101 Z ON I.ITEMNMBR=Z.ITEMNMBR
INNER JOIN IV40202 IV ON Z.UOMSCHDL=IV.UOMSCHDL
INNER JOIN IV40201 UO ON IV.UOMSCHDL=UO.UOMSCHDL AND IV.UOFM=UO.BASEUOFM
WHERE I.LOCNCODE='{0}' AND I.QTYONHND>0 and I.ITEMNMBR='{1}' 
GROUP BY I.ITEMNMBR,V.EXPNDATE,V.LOTNUMBR", FromLocation, ItemCode);
                InCubeQuery qry = new InCubeQuery(select, db_ERP);
                qry.Execute();
                DataTable tbl = new DataTable();
                tbl = qry.GetDataTable();
                foreach (DataRow dr in tbl.Rows)
                {
                    decimal GPquantity = Convert.ToDecimal(dr["GP_Qty"].ToString().Trim());
                    decimal tempQty = Quantity;
                    Quantity = Quantity - GPquantity;
                    if (Quantity <= 0)
                    {
                        transLot.ITEMNMBR = ItemCode;
                        transLot.AUTOCREATELOT = 0;
                        transLot.LOCNCODE = FromLocation;
                        transLot.SERLTQTY = tempQty;
                        transLot.IVDOCNBR = trxNo;
                        transLot.TOLOCNCODE = ToLocation;
                        transLot.LOTNUMBR = dr["LOTNUMBR"].ToString().Trim();
                        transLot.EXPNDATE = DateTime.Parse(dr["EXPNDATE"].ToString().Trim()).ToString("yyyy-MM-dd");
                        GP_Lot_List.Add(transLot);
                        transLot = new taIVTransferLotInsert_ItemsTaIVTransferLotInsert();
                        break;
                    }
                    else
                    {
                        transLot.ITEMNMBR = ItemCode;
                        transLot.AUTOCREATELOT = 0;
                        transLot.LOCNCODE = FromLocation;
                        transLot.SERLTQTY = GPquantity;
                        transLot.IVDOCNBR = trxNo;
                        transLot.TOLOCNCODE = ToLocation;
                        transLot.LOTNUMBR = dr["LOTNUMBR"].ToString().Trim();
                        transLot.EXPNDATE = DateTime.Parse(dr["EXPNDATE"].ToString().Trim()).ToString("yyyy-MM-dd");
                        GP_Lot_List.Add(transLot);
                        transLot = new taIVTransferLotInsert_ItemsTaIVTransferLotInsert();
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        public override void SendDownPayments()
        {
            SerializeSalesOrderObjectSendDownPayment(CoreGeneral.Common.StartupPath + "\\E-Connect\\", Filters.EmployeeID == -1, Filters.EmployeeID.ToString(), Filters.FromDate, Filters.ToDate);
        }

        public int SerializeSalesOrderObjectSendDownPayment(string filename, bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate)
        {
            int ret = 0;
            WriteMessage("\r\n" + "Send Payment");


            object field = new object();
            DateTime date;
            string PaymentID = "";
            string PaymentType = "";
            string BankID = "";
            decimal Amount = 0.0m;
            string ChNumber = "";
            object ChDate = "";
            string CustomerCode = "";
            bool Check = false;
            object SalesPersonCode = "";
            object CustomerName = "";
            object EmployeeCode = "";

            RMCashReceiptsType CashType = new RMCashReceiptsType();

            string QueryString = @"SELECT 
                                         CustomerUnallocatedPayment.CustomerPaymentID AS PaymentID, 
                                         SUM(CustomerUnallocatedPayment.PaidAmount) AS Amount, 
                                         PaymentTypeLanguage.PaymentTypeID AS PaymentType, 
                                         CustomerUnallocatedPayment.VoucherNumber, 
                                         CustomerUnallocatedPayment.VoucherDate, 
                                         CustomerUnallocatedPayment.BankID, 
                                         CustomerOutlet.CustomerCode, 
                                         CustomerUnallocatedPayment.PaymentDate,
                                         Employee.EmployeeCode,
                                         CustomerOutletLanguage.Description

                                   FROM  CustomerOutlet RIGHT OUTER JOIN
                                         CustomerUnallocatedPayment ON CustomerOutlet.OutletID = CustomerUnallocatedPayment.OutletID AND 
                                         CustomerOutlet.CustomerID = CustomerUnallocatedPayment.CustomerID LEFT OUTER JOIN
                                         PaymentTypeLanguage ON CustomerUnallocatedPayment.PaymentTypeID = PaymentTypeLanguage.PaymentTypeID INNER JOIN
                                         Employee ON CustomerUnallocatedPayment.EmployeeID = Employee.EmployeeID INNER JOIN
                                         CustomerOutletLanguage ON CustomerOutlet.CustomerID = CustomerOutletLanguage.CustomerID AND 
                                         CustomerOutlet.OutletID = CustomerOutletLanguage.OutletID 

                                   Where (PaymentTypeLanguage.LanguageID = 1) 
                                          AND (CustomerOutletLanguage.LanguageID = 1) 
                                          AND (CustomerUnallocatedPayment.Synchronised = 0) 
                                          AND (CustomerUnallocatedPayment.PaymentDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
                                          AND  CustomerUnallocatedPayment.PaymentDate <= '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"')";


            if (!AllSalespersons)
            {
                QueryString += "    AND CustomerUnallocatedPayment.EmployeeID = " + Salesperson;
            }


            QueryString += @"      GROUP BY   CustomerUnallocatedPayment.CustomerPaymentID, PaymentTypeLanguage.PaymentTypeID, PaymentTypeLanguage.LanguageID, 
                                              CustomerUnallocatedPayment.VoucherNumber, CustomerUnallocatedPayment.VoucherDate, CustomerUnallocatedPayment.BankID, CustomerOutlet.CustomerCode, 
                                              CustomerUnallocatedPayment.Synchronised, CustomerUnallocatedPayment.PaymentDate,Employee.EmployeeCode, CustomerUnallocatedPayment.PaymentTypeID,CustomerOutletLanguage.Description";



            InCubeQuery CustomerPaymentQuery = new InCubeQuery(db_vms, QueryString);

            err = CustomerPaymentQuery.Execute();
            err = CustomerPaymentQuery.FindFirst();
            while (err == InCubeErrors.Success)
            {
                //                                                                                                     0                                         1                                          2                                 3                               4                    5                           6                        7   
                try
                {
                    Check = false;
                    int CO = CustomerPaymentQuery.GetDataTable().Rows.Count;
                    err = CustomerPaymentQuery.GetField(0, ref field);
                    PaymentID = field.ToString();
                    err = CustomerPaymentQuery.GetField(1, ref field);
                    Amount = decimal.Parse(field.ToString());
                    err = CustomerPaymentQuery.GetField(2, ref field);
                    PaymentType = field.ToString();
                    err = CustomerPaymentQuery.GetField(3, ref field);
                    ChNumber = field.ToString();
                    err = CustomerPaymentQuery.GetField(4, ref field);
                    ChDate = field;
                    err = CustomerPaymentQuery.GetField(5, ref field);
                    if (field.ToString() == "")
                    {
                        BankID = "";
                    }
                    else
                    {
                        BankID = field.ToString();
                    }
                    err = CustomerPaymentQuery.GetField(6, ref field);
                    CustomerCode = field.ToString();
                    err = CustomerPaymentQuery.GetField(7, ref field);
                    date = DateTime.Parse(field.ToString());
                    err = CustomerPaymentQuery.GetField(8, ref SalesPersonCode);

                    err = CustomerPaymentQuery.GetField(9, ref CustomerName);

                    string cashCheckbookId = "ADCB-02";
                    string chequeCheckbookId = "ADCB-02-CHK";




                    if (PaymentType == "1")// "Cash"
                    {

                        #region Cash
                        Check = true;

                        taRMCashReceiptInsert CustomerPaymentCash = new taRMCashReceiptInsert();
                        CustomerPaymentCash.CUSTNMBR = CustomerCode;
                        CustomerPaymentCash.DOCNUMBR = PaymentID;
                        CustomerPaymentCash.ORTRXAMT = Amount;
                        CustomerPaymentCash.GLPOSTDT = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.DOCDATE = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.CSHRCTYP = 1;//0=check , 1=cash , 2= credit card
                        CustomerPaymentCash.CHEKBKID = cashCheckbookId; //"DX -CASH";
                        CustomerPaymentCash.CURNCYID = "DHS";
                        string Batch = "RV" + date.ToString("dd/MM/yy").Replace("/", "") + SalesPersonCode.ToString();
                        CustomerPaymentCash.BACHNUMB = Batch;
                        CustomerPaymentCash.CURNCYID = "";
                        CustomerPaymentCash.CREATEDIST = 1;

                        CustomerPaymentCash.TRXDSCRN = GetSalespersonCode(SalesPersonCode) + "-DownPayment";
                        eConnectType eConnect = new eConnectType();

                        CashType.taRMCashReceiptInsert = CustomerPaymentCash;
                        RMCashReceiptsType[] CashReceipts = { CashType };
                        eConnect.RMCashReceiptsType = CashReceipts;
                        string fname = filename + PaymentID.ToString().Trim() + ".xml";
                        FileStream fs = new FileStream(fname, FileMode.Create);
                        XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                        XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                        serializer.Serialize(writer, eConnect);
                        writer.Close();
                        eConnectMethods eConCall1 = new eConnectMethods();
                        string salesOrderDocument = "";
                        XmlDocument xmldoc = new XmlDocument();
                        xmldoc.Load(fname);
                        salesOrderDocument = xmldoc.OuterXml;
                        try
                        {
                            eConCall1.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, salesOrderDocument, EnumTypes.SchemaValidationType.None, "");
                        }
                        catch (eConnectException exp)
                        {
                            Check = false;
                            Console.Write(exp.ToString());
                            StreamWriter wrt1 = new StreamWriter("errorPay.log", true);
                            wrt1.Write(exp.ToString());
                            wrt1.Close();
                        }
                        #endregion
                    }
                    else if (PaymentType == "2")// "Current Dated Cheque"
                    {
                        #region Current Dated Cheque
                        Check = true;

                        taRMCashReceiptInsert CustomerPaymentCash = new taRMCashReceiptInsert();
                        CustomerPaymentCash.CUSTNMBR = CustomerCode;
                        CustomerPaymentCash.DOCNUMBR = PaymentID;
                        CustomerPaymentCash.ORTRXAMT = Amount;
                        CustomerPaymentCash.GLPOSTDT = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.DOCDATE = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.CSHRCTYP = 0;
                        CustomerPaymentCash.CURNCYID = "DHS";
                        string Batch = "RV" + date.ToString("dd/MM/yy").Replace("/", "") + SalesPersonCode.ToString();
                        CustomerPaymentCash.BACHNUMB = Batch;
                        CustomerPaymentCash.CURNCYID = "";
                        CustomerPaymentCash.CREATEDIST = 1;
                        CustomerPaymentCash.TRXDSCRN = GetSalespersonCode(SalesPersonCode) + "-DownPayment";
                        CustomerPaymentCash.CHEKNMBR = ChNumber;
                        CustomerPaymentCash.CHEKBKID = chequeCheckbookId;
                        eConnectType eConnect = new eConnectType();

                        CashType.taRMCashReceiptInsert = CustomerPaymentCash;
                        RMCashReceiptsType[] CashReceipts = { CashType };
                        eConnect.RMCashReceiptsType = CashReceipts;
                        string fname = filename + PaymentID.ToString().Trim() + ".xml";
                        FileStream fs = new FileStream(fname, FileMode.Create);
                        XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                        // Serialize using the XmlTextWriter.
                        XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                        serializer.Serialize(writer, eConnect);
                        writer.Close();
                        eConnectMethods eConCall1 = new eConnectMethods();
                        string salesOrderDocument = "";
                        XmlDocument xmldoc = new XmlDocument();
                        xmldoc.Load(fname);
                        salesOrderDocument = xmldoc.OuterXml;
                        try
                        {
                            eConCall1.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, salesOrderDocument, EnumTypes.SchemaValidationType.None, "");
                        }
                        catch (eConnectException exp)
                        {
                            Check = false;
                            Console.Write(exp.ToString());
                            StreamWriter wrt5 = new StreamWriter("errorPay.log", true);
                            wrt5.Write(exp.ToString());
                            wrt5.Close();
                        }
                        #endregion
                    }
                    else if (PaymentType == "4")// "Credit Note"
                    {
                        #region Apply Return to Invoices
                        Check = true;
                        eConnectType eConnect = new eConnectType();

                        string fname = filename + PaymentID.ToString().Trim() + ".xml";
                        FileStream fs = new FileStream(fname, FileMode.Create);
                        XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                        XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                        serializer.Serialize(writer, eConnect);
                        writer.Close();
                        eConnectMethods eConCall1 = new eConnectMethods();
                        string salesOrderDocument = "";
                        XmlDocument xmldoc = new XmlDocument();
                        xmldoc.Load(fname);
                        salesOrderDocument = xmldoc.OuterXml;
                        try
                        {
                            eConCall1.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, salesOrderDocument, EnumTypes.SchemaValidationType.None, "");
                        }
                        catch (eConnectException exp)
                        {
                            Check = false;
                            Console.Write(exp.ToString());
                            StreamWriter wrt5 = new StreamWriter("errorPay.log", true);
                            wrt5.Write(exp.ToString());
                            wrt5.Close();
                        }

                        #endregion
                    }
                    else if (PaymentType == "3")// "Post Dated Cheque"
                    {
                        #region Post Dated Cheque
                        //                        Check = true;

                        //                        //PDCRVNO : Receipt Number
                        //                        //PDCRVDT: Receipt Date (due date of the PDC) 
                        //                        //PDCCUSTID: Customer Code
                        //                        //PDCDOCNO: ?? Cheque no
                        //                        //PDCDOCDT: ?? Receipt Date
                        //                        //PDCBANK: BankID 
                        //                        //PDCAMOUNT: PDC Amount
                        //                        //PDCONHANDDATE:  – collected date (current date)

                        //                        string Query = @"Insert into PDCOPEN (PDCRVNO, PDCRVDT, PDCCUSTID, PDCDOCNO, PDCDOCDT, PDCBANK, PDCAMOUNT, PDCONHANDDATE) 
                        //                        values ('" + PaymentID + "', '" + date.ToString("yyyy-MM-dd") + "', '" + CustomerCode + "', '" + ChNumber + "', '" + date.ToString("yyyy-MM-dd") + "', '" + BankID + "', " + Amount + ", '" + DateTime.Now.ToString("yyyy-MM-dd") + "')";

                        //                        InCubeQuery PDCOPEN = new InCubeQuery(db_ERP, Query);
                        //                        err = PDCOPEN.Execute();

                        //                        MessageBox.Show("Insert into PDCOPEN " + err.ToString() + " Exception :" + PDCOPEN.GetCurrentException().ToString() + " Query : " + Query);

                        //                        //BranchID: 3 letter code for branch (DXB,AUH)
                        //                        //ReceiptNo: Receipt Number.
                        //                        //ReceiptDate: Receipt Date. This is NOT the current date. It’s the cheque date (PDC)
                        //                        //CustomerID: Customer Code
                        //                        //CustomerName: Customer Name.
                        //                        //CheckBookID: Chequebookid
                        //                        //CurrencyID: its fixed "DHS"
                        //                        //Bank: Bankid
                        //                        //ChequeNumber: Cheque Number.
                        //                        //DueDate:Receipt Date
                        //                        //Amount: PDC Amount
                        //                        //Status: 0 (zero)
                        //                        //PrintCount: always 0 
                        //                        //SPID: SalespersonID (S100, S200)

                        //                        string Branch = "DXB";
                        //                        InCubeQuery GetBranchCMD = new InCubeQuery(db_ERP, @"Select SALSTERR From RM00101 Where CUSTNMBR = '" + CustomerCode + "'");
                        //                        GetBranchCMD.Execute();
                        //                        GetBranchCMD.FindFirst();
                        //                        GetBranchCMD.GetField(0, ref field);
                        //                        Branch = field.ToString();

                        //                        Query = @"Insert into PDCDetail (BranchID, ReceiptNo, ReceiptDate, CustomerID, CustomerName, CheckBookID, CurrencyID, Bank, ChequeNumber, DueDate, Amount, Status, PrintCount, SPID) 
                        //                        Values ('" + Branch + "', '" + PaymentID + "', '" + date.ToString("yyyy-MM-dd") + "', '" + CustomerCode + "', '" + CustomerName + "', '" + chequeCheckbookId + "', 'DHS', '" + BankID + "', '" + ChNumber + "', '" + date.ToString("yyyy-MM-dd") + "', " + Amount + ", 0, 0, '" + SalesPersonCode.ToString() + "')";

                        //                        InCubeQuery PDCDETAIL = new InCubeQuery(db_ERP, Query);
                        //                        err = PDCDETAIL.Execute();

                        //                        MessageBox.Show("Insert into PDCOPEN " + err.ToString() + " Exception :" + PDCOPEN.GetCurrentException().ToString() + " Query : " + Query);

                        #endregion

                        #region Current Dated Cheque
                        Check = true;

                        taRMCashReceiptInsert CustomerPaymentCash = new taRMCashReceiptInsert();
                        CustomerPaymentCash.CUSTNMBR = CustomerCode;
                        CustomerPaymentCash.DOCNUMBR = PaymentID;
                        CustomerPaymentCash.ORTRXAMT = Amount;
                        CustomerPaymentCash.GLPOSTDT = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.DOCDATE = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.CSHRCTYP = 0;
                        CustomerPaymentCash.CURNCYID = "DHS";
                        string Batch = "RV" + date.ToString("dd/MM/yy").Replace("/", "") + GetSalespersonCode(SalesPersonCode);
                        CustomerPaymentCash.BACHNUMB = "PDC-" + DateTime.Now.ToString("MMM");
                        CustomerPaymentCash.CURNCYID = "";
                        CustomerPaymentCash.CREATEDIST = 1;
                        CustomerPaymentCash.TRXDSCRN = GetSalespersonCode(SalesPersonCode) + "-DownPayment";
                        CustomerPaymentCash.CHEKNMBR = ChNumber;
                        CustomerPaymentCash.CHEKBKID = chequeCheckbookId;
                        eConnectType eConnect = new eConnectType();

                        CashType.taRMCashReceiptInsert = CustomerPaymentCash;
                        RMCashReceiptsType[] CashReceipts = { CashType };
                        eConnect.RMCashReceiptsType = CashReceipts;
                        string fname = filename + PaymentID.ToString().Trim() + ".xml";
                        FileStream fs = new FileStream(fname, FileMode.Create);
                        XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                        // Serialize using the XmlTextWriter.
                        XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                        serializer.Serialize(writer, eConnect);
                        writer.Close();
                        eConnectMethods eConCall1 = new eConnectMethods();
                        string salesOrderDocument = "";
                        XmlDocument xmldoc = new XmlDocument();
                        xmldoc.Load(fname);
                        salesOrderDocument = xmldoc.OuterXml;
                        try
                        {
                            eConCall1.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, salesOrderDocument, EnumTypes.SchemaValidationType.None, "");
                        }
                        catch (eConnectException exp)
                        {
                            Check = false;
                            Console.Write(exp.ToString());
                            StreamWriter wrt5 = new StreamWriter("errorPay.log", true);
                            wrt5.Write(exp.ToString());
                            wrt5.Close();
                        }
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    Check = false;
                    StreamWriter wrt = new StreamWriter("errorPay.log", true);
                    wrt.Write(ex.ToString());
                    wrt.Close();
                }
                finally
                {
                    if (Check)
                    {
                        StreamWriter wrt4 = new StreamWriter("errorPay.log", true);
                        wrt4.Write(PaymentID.ToString() + " - OK\r\n");
                        wrt4.Close();
                        InCubeQuery UpdateQuery = new InCubeQuery(db_vms, "update  CustomerUnallocatedPayment set Synchronised=1 where CustomerPaymentID='" + PaymentID + "'");
                        err = UpdateQuery.Execute();
                        WriteMessage("\r\n" + PaymentID.ToString() + " - OK");
                    }
                    else
                    {
                        if (PaymentType != "Post Dated Cheque")
                        {
                            WriteMessage("\r\n" + PaymentID.ToString() + " - FAILED!");
                            ret++;
                        }
                        else
                        {
                            WriteMessage("\r\n" + PaymentID.ToString() + " - PDC (Skipped)");
                        }
                    }
                }
            }

            return ret;
        }
    }
}