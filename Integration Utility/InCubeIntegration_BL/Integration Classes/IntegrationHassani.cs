using InCubeIntegration_DAL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.Data.SqlClient;
using System.IO;
using System.Xml;

namespace InCubeIntegration_BL
{
    class IntegrationHassani : IntegrationBase
    {
        OracleCommand OracleCMD = null;
        OracleDataReader OracleReader = null;
        SqlBulkCopy bulk = null;
        OracleTransaction OraTrans = null;

        InCubeQuery incubeQuery;
        enum DiscountTypes
        {
            HeaderDiscount,
            DetailDiscount,
            NoDiscount,
            FOC
        }
        /*
        INCUBE LOG TABLE :
        
CREATE TABLE [dbo].[InCubeLog](
	[TransactionID] [nvarchar](50) NULL,
	[ErrorDate] [datetime] NULL,
	[ErrorDescription] [ntext] NULL,
	[ErrorLocation] [nvarchar](50) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

        */

        #region DECLARATION
        QueryBuilder QueryBuilderObject = new QueryBuilder();
        InCubeErrors err;
        //SqlConnection db_GP_con;
        private long UserID;
        string DateFormat = "dd-MMM-yy";
        InCubeQuery qry;
        OracleConnection Conn;
        string OrganizationID = string.Empty;
        string ConnectionString = string.Empty;

        #endregion

        #region CONSTRUCTOR
        public IntegrationHassani(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(CoreGeneral.Common.StartupPath + "\\DataSources.xml");
            ConnectionString = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'InCubeSQL']/Data").InnerText;
            Conn = new OracleConnection(ConnectionString);
            try
            {
                Conn.Open();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("Unable to connect to ERP database");
                Initialized = false;
                return;
            }
            finally
            {
                if (Conn != null && Conn.State == ConnectionState.Open)
                    Conn.Close();
            }
            UserID = CurrentUserID;
        }
        #endregion

        #region GET

        #region Update Items
        public override void UpdateItem()
        {
            GetGeneralMasterData();
        }
        private void UpdateItem_old()
        {
            try
            {

                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;
                object field = new object();

                DefaultOrganization();
                DataTable DT = new DataTable("Items");

                string GetTransaction = @"SELECT I.*,U.WITU_UOM_CODE,U.WITU_UOM_CONV 
FROM OW_ITEM_MAS I 
INNER JOIN OW_ITEM_UOM_MAS U ON I.WITM_ITEM_CODE=U.WITU_ITEM_CODE
INNER JOIN (SELECT WITM_ITEM_CODE,MAX(WITM_CR_DT) WITM_CR_DT FROM OW_ITEM_MAS GROUP BY WITM_ITEM_CODE) MI ON MI.WITM_ITEM_CODE = I.WITM_ITEM_CODE AND MI.WITM_CR_DT = I.WITM_CR_DT
INNER JOIN (SELECT WITU_ITEM_CODE,WITU_UOM_CODE,MAX(WITU_CR_DT) WITU_CR_DT FROM OW_ITEM_UOM_MAS GROUP BY WITU_ITEM_CODE,WITU_UOM_CODE ) MU 
ON MU.WITU_ITEM_CODE = U.WITU_ITEM_CODE AND MU.WITU_CR_DT = U.WITU_CR_DT AND MU.WITU_UOM_CODE = U.WITU_UOM_CODE
WHERE WITM_PROC_FLAG = 'N' AND
((I.WITM_DIVISION='DAIRY' AND I.WITM_CATG_CODE = '002')) OR ((I.WITM_DIVISION = 'FOOD' AND I.WITM_CATG_CODE='00001')) 
OR ((I.WITM_DIVISION = 'BOTH' AND I.WITM_CATG_CODE='003')) OR ((I.WITM_DIVISION = '0' AND I.WITM_CATG_CODE='0'))
ORDER BY I.WITM_ITEM_CODE";
                DT = OLEDB_Datatable(GetTransaction);
                SaveTable(DT, "Stg_Items", false);

                //err = QueryBuilderObject.UpdateQueryString("Item", " InActive = 1", db_vms);

                ClearProgress();
                SetProgressMax(DT.Rows.Count);
                foreach (DataRow row in DT.Rows)
                {
                    ReportProgress("Updating Items");

                    string ItemCode = row["WITM_ITEM_CODE"].ToString().Trim();
                    string itemDescriptionEnglish = row["WITM_ITEM_DESC"].ToString().Trim();
                    string itemDescriptionArabic = row["WITM_ITEM_DESC"].ToString().Trim();
                    string CategoryCode = row["WITM_CATG_CODE"].ToString().Trim();
                    string CategoryNameEnglish = row["WITM_CATG_DESC"].ToString();
                    string width = "0";
                    if (row["WITM_ITEM_ANLY_10"].ToString().Trim() == "I-STOCK")
                    {
                        width = "5";
                    }
                    string inActive;
                    if (row["WITM_DIVISION"].ToString().Trim() == "FOOD" || row["WITM_DIVISION"].ToString().Trim() == "DAIRY" || row["WITM_DIVISION"].ToString().Trim() == "BOTH")
                    {
                        inActive = "0";
                    }
                    else
                    {
                        inActive = "1";
                    }
                    string DivisionCode;
                    string DivisionNameEnglish;


                    if (row["WITM_CATG_CODE"].ToString().Trim() == "00001")
                    {
                        DivisionCode = "1";// row["ComapnyID"].ToString().Trim();
                        DivisionNameEnglish = "HGC";//row["CompanyName"].ToString().Trim();
                    }
                    else if (row["WITM_CATG_CODE"].ToString().Trim() == "002")
                    {
                        DivisionCode = "002";// row["ComapnyID"].ToString().Trim();
                        DivisionNameEnglish = "DAIRY";//row["CompanyName"].ToString().Trim(); 
                    }
                    else
                    {
                        DivisionCode = "003";// row["ComapnyID"].ToString().Trim();
                        DivisionNameEnglish = "BOTH";//row["CompanyName"].ToString().Trim(); 
                    }
                    string Brand = row["WITM_BRAND_CODE"].ToString().Trim();
                    if (Brand.Equals(string.Empty)) Brand = "N\\A";
                    string Orgin = row["WITM_ORIGIN"].ToString().Trim();// row["Origin"].ToString().Trim();
                    string TCAllowed = "N"; //row["TCAllowed"].ToString().Trim();
                    if (TCAllowed == "Y") { TCAllowed = "1"; } else { TCAllowed = "0"; }
                    string PackDescriptionEnglish = row["WITU_UOM_CODE"].ToString().Trim();
                    string packQty = row["WITU_UOM_CONV"].ToString().Trim();

                    string barcode = row["WITM_ITEM_CODE"].ToString().Trim();
                    string PackDefinition = row["WITM_BASE_UOM"].ToString().Trim();
                    string PackGroup = row["WITM_CATG_DESC"].ToString().Trim();
                    string PackGroupCode = row["WITM_CATG_CODE"].ToString().Trim();
                    string PackGroupID = string.Empty;
                    if (ItemCode == string.Empty)
                        continue;

                    #region ItemDivision

                    string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode = '" + DivisionCode + "'", db_vms);
                    if (DivisionID == string.Empty)
                        continue;
                    //if (DivisionID == string.Empty)
                    //{
                    //    DivisionID = GetFieldValue("Division", "isnull(MAX(DivisionID),0) + 1", db_vms);

                    //    QueryBuilderObject.SetField("DivisionID", DivisionID);
                    //    QueryBuilderObject.SetField("DivisionCode", "'" + DivisionCode + "'");
                    //    QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                    //    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    //    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    //    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    //    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    //    err = QueryBuilderObject.InsertQueryString("Division", db_vms);

                    //    QueryBuilderObject.SetField("DivisionID", DivisionID);
                    //    QueryBuilderObject.SetField("LanguageID", "1");
                    //    QueryBuilderObject.SetField("Description", "'" + DivisionNameEnglish + "'");
                    //    err = QueryBuilderObject.InsertQueryString("DivisionLanguage", db_vms);

                    //    QueryBuilderObject.SetField("DivisionID", DivisionID);  // Arabic Description
                    //    QueryBuilderObject.SetField("LanguageID", "2");
                    //    QueryBuilderObject.SetField("Description", "'" + DivisionNameEnglish + "'");
                    //    err = QueryBuilderObject.InsertQueryString("DivisionLanguage", db_vms);
                    //}

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
                        QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", db_vms);

                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID);  // Arabic Description
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "'" + CategoryNameEnglish + "'");
                        err = QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", db_vms);

                    }
                    else
                    {
                        string _existItemCategoryID = GetFieldValue("ItemCategory", "ItemCategoryID", "ItemCategoryCode = '" + CategoryCode + "'", db_vms);

                        QueryBuilderObject.SetField("DivisionID", DivisionID.ToString());

                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        err = QueryBuilderObject.UpdateQueryString("ItemCategory", " ItemCategoryID = '" + ItemCategoryID + "'", db_vms);
                    }

                    #endregion

                    #region PackType

                    string PacktypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " Description = '" + PackDescriptionEnglish + "' AND LanguageID = 1", db_vms);
                    //WriteExceptions("ITEM UOM ID IS " + PackDescriptionEnglish + "", "ITEM UPDATE..", false);
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

                    string BrandID = GetFieldValue("BrandLanguage", "BrandID", " Description = '" + Brand + "' AND LanguageID = 1", db_vms);
                    if (BrandID == string.Empty && Brand.Trim() != string.Empty)
                    {
                        BrandID = GetFieldValue("Brand", "isnull(MAX(BrandID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("BrandID", BrandID);
                        err = QueryBuilderObject.InsertQueryString("Brand", db_vms);

                        QueryBuilderObject.SetField("BrandID", BrandID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + Brand + "'");
                        err = QueryBuilderObject.InsertQueryString("BrandLanguage", db_vms);

                        QueryBuilderObject.SetField("BrandID", BrandID);
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "N'" + Brand + "'");
                        err = QueryBuilderObject.InsertQueryString("BrandLanguage", db_vms);

                    }
                    else if (Brand.Trim() == string.Empty) BrandID = null;

                    string ItemID = "";

                    ItemID = GetFieldValue("Item", "ItemID", "ItemCode='" + ItemCode + "'", db_vms);
                    if (ItemID == string.Empty)
                    {
                        ItemID = GetFieldValue("Item", "isnull(MAX(ItemID),0) + 1", db_vms);
                    }

                    #region Item

                    string ExistItem = GetFieldValue("Item", "ItemID", "ItemID = " + ItemID, db_vms);
                    if (ExistItem != string.Empty) // Exist Item --- Update Query
                    {
                        TOTALUPDATED++;
                        QueryBuilderObject.SetField("ItemCode", "'" + ItemCode + "'");
                        QueryBuilderObject.SetField("InActive", "'" + inActive + "'");
                        QueryBuilderObject.SetField("Origin", "'" + Orgin + "'");
                        QueryBuilderObject.SetField("BrandID", BrandID);
                        QueryBuilderObject.SetField("TemporaryCredit", TCAllowed);
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "GETDATE()");
                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID);
                        err = QueryBuilderObject.UpdateQueryString("Item", " ItemID = " + ItemID, db_vms);
                    }
                    else // New Item --- Insert Query
                    {
                        TOTALINSERTED++;
                        QueryBuilderObject.SetField("ItemID", ItemID);
                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());
                        QueryBuilderObject.SetField("ItemCode", "'" + ItemCode + "'");
                        QueryBuilderObject.SetField("InActive", "'" + inActive + "'");
                        QueryBuilderObject.SetField("PackDefinition", "'" + PackDefinition + "'");
                        QueryBuilderObject.SetField("TemporaryCredit", TCAllowed);
                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("ItemType", "1");
                        QueryBuilderObject.SetField("Origin", "'" + Orgin + "'");
                        QueryBuilderObject.SetField("BrandID", BrandID);
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString("yyyy - MM - dd hh: mm:ss") + "'");

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

                    if (err == InCubeErrors.Success)
                    {
                        err = UpdateFlag("OW_ITEM_MAS", "WITM_ITEM_CODE='" + ItemCode + "' ", "WITM");
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

                    int PackID = 1;

                    ExistItem = GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " and PackTypeID = " + PacktypeID, db_vms);
                    if (ExistItem != string.Empty)
                    {
                        PackID = int.Parse(GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " and PackTypeID = " + PacktypeID, db_vms));
                        QueryBuilderObject.SetField("Quantity", "" + packQty + "");
                        QueryBuilderObject.SetField("Width", width);
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
                        QueryBuilderObject.SetField("Width", width);
                        QueryBuilderObject.SetField("EquivalencyFactor", "1");
                        err = QueryBuilderObject.InsertQueryString("Pack", db_vms);
                    }

                    string packGroupDetail = ExistItem = GetFieldValue("PackGroupDetail", "PackID", "PackID = " + PackID + " and PackGroupID = " + PackGroupID, db_vms);
                    if (packGroupDetail == string.Empty)
                    {
                        QueryBuilderObject.SetField("PackID", PackID.ToString());
                        QueryBuilderObject.SetField("PackGroupID", PackGroupID.ToString());
                        err = QueryBuilderObject.InsertQueryString("PackGroupDetail", db_vms);
                    }

                    #endregion
                }

                DT.Dispose();

                //                incubeQuery = new InCubeQuery(db_vms, @"UPDATE I SET Inactive = (CASE WHEN S.WITM_ITEM_CODE IS NULL THEN 1 ELSE 0 END)
                //FROM Item I
                //LEFT JOIN Stg_Items S ON S.WITM_ITEM_CODE = I.ItemCode");
                //                incubeQuery.ExecuteNonQuery();

                //string UpdateInActiveItems = string.Format(@"Update Item Set InActive = 1 Where UpdatedDate < DATEADD(HOUR,-1,getdate())");
                //qry = new InCubeQuery(UpdateInActiveItems, db_vms);
                //err = qry.ExecuteNonQuery();


                WriteMessage("\r\n");
                WriteMessage("<<< ITEMS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        #endregion

        #region UpdateCustomer

        public override void UpdateCustomer()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            object field = new object();

            DefaultOrganization();

            DataTable DT = new DataTable();
            string GET_CUST = string.Format(@"SELECT  * FROM OW_CUST_MAS WHERE WCUS_PROC_FLAG='N'");
            OracleDataAdapter adapter = new OracleDataAdapter(GET_CUST, Conn);
            adapter.Fill(DT);

            ClearProgress();
            SetProgressMax(DT.Rows.Count);

            foreach (DataRow row in DT.Rows)
            {
                try
                {

                    ReportProgress("Updating Customers");

                    #region Variables
                    string PriceListCode = "";// row["CUST_PL_CODE"].ToString().Trim();
                    string CustomerCode = row["WCUS_CUST_CODE"].ToString().Trim();
                    if (!CustomerCode.Equals("C79318"))
                    {
                        //continue;
                    }
                    string CustomerName = row["WCUS_CUST_NAME"].ToString().Trim();
                    string outletBarCode = row["WCUS_CUST_CODE"].ToString().Trim();
                    string outletCode = row["WCUS_CUST_CODE"].ToString().Trim();
                    string CustomerOutletDescriptionEnglish = row["WCUS_CUST_NAME"].ToString().Trim();
                    string CustomerOutletDescriptionArabic = row["WCUS_CUST_NAME"].ToString().Trim();


                    CustomerCode = outletCode;
                    CustomerName = CustomerOutletDescriptionEnglish;
                    string Phonenumber = "";// row["ADDR_LINE_3"].ToString().Trim();
                    string Faxnumber = "";// row["ADDR_LINE_1"].ToString().Trim();
                    if (Phonenumber.Length >= 40) Phonenumber.Substring(0, 39);
                    if (Faxnumber.Length >= 40) Faxnumber.Substring(0, 39);
                    string Email = "";// row["Email"].ToString().Trim();
                    string CustomerAddressEnglish = row["WCUS_ADDR_LINE1"].ToString().Trim() + "-" + row["WCUS_ADDR_LINE2"].ToString().Trim() + "-" + row["WCUS_ADDR_LINE3"].ToString().Trim();
                    string CustomerAddressArabic = row["WCUS_ADDR_LINE1"].ToString().Trim() + "-" + row["WCUS_ADDR_LINE2"].ToString().Trim() + "-" + row["WCUS_ADDR_LINE3"].ToString().Trim();

                    string Street = row["WCUS_ADDR_LINE1"].ToString().Trim();
                    string Area = row["WCUS_ADDR_LINE2"].ToString().Trim();
                    string City = row["WCUS_ADDR_LINE3"].ToString().Trim();

                    string istaxable = row["WCUS_CUST_ANLY_10"].ToString().Trim();
                    string Taxable = "1";
                    if (istaxable == "C-EXEMPT")
                    {
                        Taxable = "0";
                    }
                    else
                    {
                        Taxable = "1";
                    }
                    string TaxNumber = "";
                    TaxNumber = row["WCUS_CUST_FLEX_20"].ToString().Trim();
                    //row[9].ToString().Trim();
                    //string channelDescription = row["Channel"].ToString().Trim();
                    //string ChannelCode = row["ChannelCode"].ToString().Trim();
                    string CustomerNewGroup = row["WCUS_CUST_GROUP"].ToString().Trim();// string.Empty;
                    //if (CustomerName.Trim().Replace(" ", "").ToLower() != "nogroup")
                    //{
                    //    CustomerNewGroup = row["CustomerGroup"].ToString().Trim();
                    //}

                    string CustomerGroupDescription = row["WCUS_CUST_GROUP"].ToString().Trim();
                    //string IsCreditCustomer = row["CustomerType"].ToString().Trim();
                    string CreditLimit = row["WCUS_CREDIT_LIMIT"].ToString().Trim();
                    if (CreditLimit.Equals(string.Empty)) CreditLimit = "1000";
                    string Balance = "0";// row[13].ToString().Trim();
                    string Paymentterms = row["WCUS_PYMT_DAYS"].ToString().Trim();
                    string OnHold = row["WCUS_FRZ_FLAG_YN_NUM"].ToString().Trim();
                    if (OnHold.Equals("1")) { OnHold = "1"; } else { OnHold = "0"; }
                    string CustomerType = row["WCUS_CREDIT_YN"].ToString().Trim();// string.Empty;// row["CustomerPeymentTerms"].ToString().Trim();

                    if (CustomerType.ToLower().Equals("n"))
                    {
                        CustomerType = "1";
                        CreditLimit = "0";
                    }
                    else
                    {
                        CustomerType = "2";
                    }

                    string inActive = row["WCUS_FRZ_FLAG_YN_NUM"].ToString().Trim();
                    if (inActive.ToLower().Equals("1"))
                    {
                        inActive = "1";
                    }
                    else
                    {
                        inActive = "0";
                    }
                    string companyID = "1";// row["COMPANYID"].ToString().Trim();

                    //if (CustomerType == "CREDIT")
                    //{
                    //    CustomerType = "2";
                    //}
                    //else if (CustomerType == "CASH")
                    //{
                    //    CustomerType = "1";
                    //}
                    //else
                    //{
                    //    CustomerType = "3";
                    //}
                    ////List<DaySequence> daySequence = new List<DaySequence>();
                    ////DaySequence ds = new DaySequence();



                    string B2B_Invoices = "0";// row["NumberOfInvoices"].ToString().Trim();
                    string GPSlongitude = "0";// row[16].ToString().Trim();
                    string GPSlatitude = "0";// row[17].ToString().Trim();


                    //string SupervisorCode = row["SupervisorCode"].ToString().Trim();
                    //string Supervisor = row["Supervisor"].ToString().Trim();
                    //string RoutemanagerCode = row["RouteManagerCode"].ToString().Trim();
                    //string Routemanager = row["RouteManager"].ToString().Trim();
                    string RouteCode = row["WCUS_SM_CODE"].ToString().Trim();
                    string SalesManCode = row["WCUS_SM_CODE"].ToString().Trim();
                    string SalesMan = row["WCUS_SM_CODE"].ToString().Trim();
                    Dictionary<string, string> empDic = new Dictionary<string, string>();
                    string supervisorID = string.Empty;

                    if (!empDic.ContainsKey(SalesManCode))
                    {
                        empDic.Add(SalesManCode, SalesMan + "2");
                    }
                    // string CustomerDescription = row["CustomerGroup"].ToString().Trim();

                    if (CustomerCode == string.Empty)
                        continue;

                    string CustomerID = "0";

                    string ExistCustomer = "";
                    #endregion

                    #region Get Customer Group
                    string GroupID2 = string.Empty;
                    string GroupID = string.Empty;
                    GroupID = GetFieldValue("CustomerGroupLanguage", "GroupID", " Description = '" + CustomerNewGroup.Trim() + "'  AND LanguageID = 1", db_vms);

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
                    CustomerID = string.Empty;
                    CustomerID = GetFieldValue("Customer", "CustomerID", "CustomerCode = '" + CustomerCode + "'", db_vms);
                    if (CustomerID == string.Empty)
                    {
                        CustomerID = GetFieldValue("Customer", "isnull(MAX(CustomerID),0) + 1", db_vms);
                    }
                    ExistCustomer = GetFieldValue("Customer", "CustomerID", "CustomerID = " + CustomerID, db_vms);
                    if (ExistCustomer != string.Empty) // Exist Customer --- Update Query
                    {
                        TOTALUPDATED++;

                        QueryBuilderObject.SetField("CustomerCode", "'" + CustomerCode + "'");
                        QueryBuilderObject.SetField("Phone", "'" + Phonenumber + "'");
                        QueryBuilderObject.SetField("Fax", "'" + Faxnumber + "'");
                        QueryBuilderObject.SetField("OnHold", OnHold);
                        QueryBuilderObject.SetField("InActive", inActive);
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
                        QueryBuilderObject.SetField("CustomerCode", "'" + CustomerCode + "'");
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
                        //QueryBuilderObject.SetField("Description", "'" + CustomerName + "'"); //disables updating from orion on hassani
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
                        //QueryBuilderObject.SetField("Description", "N'" + CustomerName + "'");
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
                        QueryBuilderObject.SetField("OrganizationID", OrganizationID);
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
                    //WriteMessage("Taxable " + istaxable + " " + Taxable + " " + inActive + "CustomerID " + CustomerCode + " TaxNumber " + TaxNumber);
                    CreateCustomerOutlet(AccountID.ToString(), SalesManCode, GroupID, GroupID2, B2B_Invoices, CustomerType, outletCode, PriceListCode, Paymentterms, CustomerOutletDescriptionEnglish, CustomerAddressEnglish, CustomerOutletDescriptionArabic, CustomerAddressArabic, Phonenumber, Faxnumber, OnHold, Taxable, CustomerCode, CreditLimit, Balance, GPSlongitude, GPSlatitude, outletBarCode, Email, companyID, inActive, SalesMan, City, Area, Street, TaxNumber);
                }
                catch
                {
                    WriteMessage("\r\n");
                    WriteMessage("customer failed ");

                }
            }

            DT.Dispose();

            WriteMessage("\r\n");
            WriteMessage("<<< CUSTOMERS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        private void CreateCustomerOutlet(string parentAccount, string SALESMAN_CODE, string GroupID, string GroupID2, string B2B_Inv, string CustType, string CustomerCode, string PriceListCode, string Paymentterms, string CustomerDescriptionEnglish, string CustomerAddressEnglish, string CustomerDescriptionArabic, string CustomerAddressArabic, string Phonenumber, string Faxnumber, string OnHold, string Taxable, string HeadOfficeCode, string CreditLimit, string Balance, string Longitude, string latitude, string CustomerBarCode, string email, string divisionID, string inactive, string SALESMAN_NAME, string City, string Area, string Street, string TaxNumber)
        {
            int CustomerID;
            InCubeErrors err;

            if (Longitude == string.Empty)
                Longitude = "0";

            if (latitude == string.Empty)
                latitude = "0";

            string ExistCustomer = "";

            CustomerID = int.Parse(GetFieldValue("Customer", "CustomerID", "CustomerCode='" + HeadOfficeCode + "'", db_vms));





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

                    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                    QueryBuilderObject.SetField("LanguageID", "2");
                    QueryBuilderObject.SetField("Description", "'Every " + Paymentterms + " Days'");
                    err = QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms);
                }
            }

            #region Customer Outlet and language
            string CityID = GetFieldValue("CityLanguage", "CityID", "Description = '" + City + "'", db_vms);
            if (CityID == string.Empty)
            {
                CityID = GetFieldValue("City", "isnull(MAX(CityID),0) + 1", db_vms);
                QueryBuilderObject.SetField("CountryID", "1");
                QueryBuilderObject.SetField("StateID", "1");
                QueryBuilderObject.SetField("CityID", CityID);
                QueryBuilderObject.SetField("CityCode", "'" + City + "'");
                err = QueryBuilderObject.InsertQueryString("City", db_vms);

                QueryBuilderObject.SetField("CountryID", "1");
                QueryBuilderObject.SetField("StateID", "1");
                QueryBuilderObject.SetField("CityID", CityID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + City + "'");
                err = QueryBuilderObject.InsertQueryString("CityLanguage", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("CityCode", "'" + City + "'");
                err = QueryBuilderObject.UpdateQueryString("City", "  CityID = " + CityID, db_vms);

                QueryBuilderObject.SetField("Description", "'" + City + "'");
                err = QueryBuilderObject.UpdateQueryString("CityLanguage", "  CityID = " + CityID, db_vms);
            }

            string AreaID = GetFieldValue("AreaLanguage", "AreaID", "Description = '" + Area + "'", db_vms);
            if (AreaID == string.Empty)
            {
                AreaID = GetFieldValue("Area", "isnull(MAX(AreaID),0) + 1", db_vms);
                QueryBuilderObject.SetField("CountryID", "1");
                QueryBuilderObject.SetField("StateID", "1");
                QueryBuilderObject.SetField("CityID", CityID);
                QueryBuilderObject.SetField("AreaID", AreaID);
                QueryBuilderObject.SetField("AreaCode", "'" + Area + "'");
                err = QueryBuilderObject.InsertQueryString("Area", db_vms);

                QueryBuilderObject.SetField("CountryID", "1");
                QueryBuilderObject.SetField("StateID", "1");
                QueryBuilderObject.SetField("CityID", CityID);
                QueryBuilderObject.SetField("AreaID", AreaID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + Area + "'");
                err = QueryBuilderObject.InsertQueryString("AreaLanguage", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("AreaCode", "'" + Area + "'");
                err = QueryBuilderObject.UpdateQueryString("Area", "  AreaID = " + AreaID, db_vms);

                QueryBuilderObject.SetField("Description", "'" + Area + "'");
                err = QueryBuilderObject.UpdateQueryString("AreaLanguage", "  AreaID = " + AreaID, db_vms);
            }

            string StreetID = GetFieldValue("StreetLanguage", "StreetID", "Description = '" + Street + "'", db_vms);
            if (StreetID == string.Empty)
            {
                StreetID = GetFieldValue("Street", "isnull(MAX(StreetID),0) + 1", db_vms);
                QueryBuilderObject.SetField("CountryID", "1");
                QueryBuilderObject.SetField("StateID", "1");
                QueryBuilderObject.SetField("CityID", CityID);
                QueryBuilderObject.SetField("AreaID", AreaID);
                QueryBuilderObject.SetField("StreetID", StreetID);
                QueryBuilderObject.SetField("StreetCode", "'" + Street + "'");
                err = QueryBuilderObject.InsertQueryString("Street", db_vms);

                QueryBuilderObject.SetField("CountryID", "1");
                QueryBuilderObject.SetField("StateID", "1");
                QueryBuilderObject.SetField("CityID", CityID);
                QueryBuilderObject.SetField("AreaID", AreaID);
                QueryBuilderObject.SetField("StreetID", StreetID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + Street + "'");
                err = QueryBuilderObject.InsertQueryString("StreetLanguage", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("StreetCode", "'" + Street + "'");
                err = QueryBuilderObject.UpdateQueryString("Street", "  StreetID = " + StreetID, db_vms);

                QueryBuilderObject.SetField("Description", "'" + Street + "'");
                err = QueryBuilderObject.UpdateQueryString("StreetLanguage", "  StreetID = " + StreetID, db_vms);
            }


            string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerID = " + CustomerID + " AND CustomerCode = '" + CustomerCode + "'", db_vms);
            if (!OutletID.Trim().Equals(string.Empty))
            {
                QueryBuilderObject.SetField("Phone", "'" + Phonenumber + "'");
                QueryBuilderObject.SetField("Fax", "'" + Faxnumber + "'");
                QueryBuilderObject.SetField("Email", "'" + email + "'");
                //QueryBuilderObject.SetField("CustomerTypeID", CustType); //HardCoded -1- Cash -2- Credit
                if (CustomerCode.StartsWith("CD") || CustomerCode.StartsWith("CV"))
                {
                    QueryBuilderObject.SetField("CustomerTypeID", "1");
                }
                else
                {
                    QueryBuilderObject.SetField("CustomerTypeID", CustType);
                }
                QueryBuilderObject.SetField("OnHold", OnHold);
                QueryBuilderObject.SetField("Inactive", inactive);
                QueryBuilderObject.SetField("BillsOpenNumber", B2B_Inv);
                QueryBuilderObject.SetField("Barcode", "'" + CustomerBarCode + "'");
                QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("PreferredVisitTimeFrom", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("PreferredVisitTimeTo", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("StreetID", StreetID);
                QueryBuilderObject.SetField("Taxeable", Taxable);
                QueryBuilderObject.SetStringField("TaxNumber", TaxNumber);
                err = QueryBuilderObject.UpdateQueryString("CustomerOutlet", "  CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);

            }
            else
            {
                OutletID = GetFieldValue("CustomerOutlet", "isnull(MAX(OutletID),0) + 1", "CustomerID = " + CustomerID, db_vms);

                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("CustomerCode", "'" + CustomerCode + "'");
                QueryBuilderObject.SetField("Barcode", "'" + CustomerBarCode + "'");
                QueryBuilderObject.SetField("Phone", "'" + Phonenumber + "'");
                QueryBuilderObject.SetField("Fax", "'" + Faxnumber + "'");
                QueryBuilderObject.SetField("Email", "'" + email + "'");
                QueryBuilderObject.SetField("Taxeable", Taxable);
                QueryBuilderObject.SetStringField("TaxNumber", TaxNumber);
                QueryBuilderObject.SetField("BillsOpenNumber", B2B_Inv);
                if (CustomerCode.StartsWith("CD") || CustomerCode.StartsWith("CV"))
                {
                    QueryBuilderObject.SetField("CustomerTypeID", "1");
                }
                else
                {
                    QueryBuilderObject.SetField("CustomerTypeID", CustType);
                }
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

                QueryBuilderObject.SetField("StreetID", StreetID);
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
            if (!PriceListCode.Trim().Equals(string.Empty) && !PriceListCode.Trim().ToLower().Equals("ge001"))
            {
                string PriceListID = GetFieldValue("PriceList", "PriceListID", "PriceListCode='" + PriceListCode + "'", db_vms).Trim();
                InCubeQuery DeletePriceDefinitionQuery = new InCubeQuery(db_vms, "Delete From CustomerPrice where CustomerID=" + CustomerID + " and OutletID=" + OutletID + "");
                DeletePriceDefinitionQuery.ExecuteNonQuery();
                //string existCustomerPrice = GetFieldValue("CustomerPrice", "PriceListID", "CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and priceListID=" + PriceListID + "", db_vms).Trim();
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("PriceListID", PriceListID);
                err = QueryBuilderObject.InsertQueryString("CustomerPrice", db_vms);
            }
            //else if (err == InCubeErrors.Success)
            //{

            //    QueryBuilderObject.SetField("GroupID", GroupID.ToString());
            //    QueryBuilderObject.UpdateQueryString("CustomerOutletGroup","CustomerID="+CustomerID.ToString()+" and OutletID="+OutletID+"", db_vms);
            //}
            //string CustOutDiv = GetFieldValue("CustomerOutletDivision", "customerID", "customerID=" + CustomerID.ToString() + " and OutletID=" + OutletID + " and DivisionID=" + divisionID + "", db_vms).Trim();
            //if (CustOutDiv.Equals(string.Empty))
            //{
            //    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
            //    QueryBuilderObject.SetField("OutletID", OutletID.ToString());
            //    QueryBuilderObject.SetField("DivisionID", divisionID.ToString().Trim());
            //    err = QueryBuilderObject.InsertQueryString("CustomerOutletDivision", db_vms);
            //    //string CustDivi = string.Format("insert into CustomerOutletDivision values({0},{1},{2})", CustomerID, OutletID, divisionID);
            //    //qry = new InCubeQuery(CustDivi,db_vms);
            //    //err = qry.ExecuteNonQuery();
            //}
            //string ExistRouteCustomer = GetFieldValue("RouteCustomer", "CustomerID=" + CustomerID.ToString() + " and OutletID=" + OutletID + "", db_vms);
            //if (ExistRouteCustomer == stri
            //}
            ExistCustomer = GetFieldValue("CustomerOutletLanguage", "OutletID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " AND LanguageID = 1", db_vms);
            if (ExistCustomer != string.Empty)
            {
                //QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish + "'"); //commented as per Hassani
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
                //QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionArabic + "'");
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


            string TerritoryID = GetFieldValue("TerritoryLanguage", "TerritoryID", "Description='" + SALESMAN_CODE + "'", db_vms);
            if (TerritoryID == string.Empty)
            {
                TerritoryID = GetFieldValue("[Territory]", "isnull(max(TerritoryID),0)+1", db_vms);
                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                QueryBuilderObject.SetField("OrganizationID", "1");
                QueryBuilderObject.SetField("TerritoryCode", "'" + SALESMAN_CODE + "'");
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                err = QueryBuilderObject.InsertQueryString("Territory", db_vms);

                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + SALESMAN_CODE + "'");
                QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);

                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "'" + SALESMAN_CODE + "'");
                err = QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);

            }
            else
            {
                QueryBuilderObject.SetField("Description", "'" + SALESMAN_CODE + "'");
                err = QueryBuilderObject.UpdateQueryString("TerritoryLanguage", "TerritoryID=" + TerritoryID + "", db_vms);

            }
            err = ExistObject("CustOutTerritory", "TerritoryID", "TerritoryID = " + TerritoryID + " AND CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
            if (err != InCubeErrors.Success)
            {

                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                err = QueryBuilderObject.InsertQueryString("CustOutTerritory", db_vms);
            }

            //Routes
            string RouteID = GetFieldValue("RouteLanguage", "RouteID", "Description='" + SALESMAN_CODE + "'", db_vms);
            if (RouteID == string.Empty)
            {
                RouteID = GetFieldValue("[Route]", "isnull(max(RouteID),0)+1", db_vms);
                QueryBuilderObject.SetField("RouteID", RouteID);
                QueryBuilderObject.SetField("InActive", "0");
                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("RouteCode", "'" + SALESMAN_CODE + "'");
                err = QueryBuilderObject.InsertQueryString("Route", db_vms);

                QueryBuilderObject.SetField("RouteID", RouteID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + SALESMAN_CODE + "'");
                QueryBuilderObject.InsertQueryString("RouteLanguage", db_vms);

                QueryBuilderObject.SetField("RouteID", RouteID);
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "'" + SALESMAN_CODE + "'");
                err = QueryBuilderObject.InsertQueryString("RouteLanguage", db_vms);

            }
            else
            {
                QueryBuilderObject.SetField("Description", "'" + SALESMAN_CODE + "'");
                err = QueryBuilderObject.UpdateQueryString("RouteLanguage", "RouteID=" + RouteID + "", db_vms);

            }
            err = ExistObject("RouteCustomer", "RouteID", "RouteID = " + RouteID + " AND CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
            if (err != InCubeErrors.Success)
            {

                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("RouteID", RouteID);
                err = QueryBuilderObject.InsertQueryString("RouteCustomer", db_vms);
            }

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
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
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
            ExistCustomer = GetFieldValue("AccountCustOutDiv", "AccountID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " and DivisionID=" + divisionID + "", db_vms);
            if (ExistCustomer == string.Empty)
            {
                AccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("AccountTypeID", "1");
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.SetField("Balance", Balance);
                QueryBuilderObject.SetField("GL", "0");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.SetField("ParentAccountID", Parent2);
                QueryBuilderObject.SetField("CurrencyID", "1");
                err = QueryBuilderObject.InsertQueryString("Account", db_vms);

                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("DivisionID", divisionID);
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

            ////Account for DIARY division

            string Diarydiv = GetFieldValue("Division", "DivisionID", "DivisionCode='002'", db_vms).Trim();

            ExistCustomer = GetFieldValue("AccountCustOutDiv", "AccountID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " and DivisionID=" + Diarydiv + "", db_vms);
            if (ExistCustomer == string.Empty)
            {
                AccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("AccountTypeID", "1");
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.SetField("Balance", Balance);
                QueryBuilderObject.SetField("GL", "0");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.SetField("ParentAccountID", Parent2);
                QueryBuilderObject.SetField("CurrencyID", "1");
                err = QueryBuilderObject.InsertQueryString("Account", db_vms);

                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("DivisionID", Diarydiv);
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


            if (err == InCubeErrors.Success)
            {
                err = UpdateFlag("OW_CUST_MAS", "WCUS_CUST_CODE='" + CustomerCode + "' ", "WCUS");
            }

            #endregion


            #endregion
        }

        #endregion

        #region Update Geographical Location
        //public override void UpdateGeographicalLocation()
        //{
        //    try
        //    {
        //        DataTable DT = new DataTable();
        //        string select = string.Format("select CUST_CODE,CUST_ANLY_CODE_04,CUST_ANLY_CODE_07 from OM_CUSTOMER");
        //        qry = new InCubeQuery(select, db_vms);
        //        err = qry.Execute();
        //        DT = qry.GetDataTable();
        //        int TOTALUPDATE = 0;
        //        int TOTALINSERT = 0;
        //        ClearProgress();
        //        SetProgressMax(DT.Rows.Count;
        //        foreach (DataRow dr in DT.Rows)
        //        {
        //            ReportProgress(++;
        //            IntegrationForm.lblProgress.Text = "Updating Locations" + " " + ReportProgress( + " / " + IntegrationForm.progressBar1.Maximum;
        //            ();
        //            string custCode = dr["CUST_CODE"].ToString().Trim();
        //            string Region = dr["CUST_ANLY_CODE_04"].ToString().Trim();
        //            string StateDescription = dr["CUST_ANLY_CODE_04"].ToString().Trim();
        //            string AreaLocation = dr["CUST_ANLY_CODE_07"].ToString().Trim();
        //            string streetDescription = dr["CUST_ANLY_CODE_07"].ToString().Trim();
        //            string StateID = GetFieldValue("StateLanguage", "StateID", "Description='" + Region + "' and LanguageID=1", db_vms).Trim();
        //            if (StateID.Equals(string.Empty))
        //            {
        //                StateID = GetFieldValue("State", "isnull(max(StateID),0)+1", db_vms).Trim();
        //                QueryBuilderObject.SetField("CountryID", "1");
        //                QueryBuilderObject.SetField("StateID", StateID);
        //                err = QueryBuilderObject.InsertQueryString("State", db_vms);
        //                QueryBuilderObject.SetField("CountryID", "1");
        //                QueryBuilderObject.SetField("StateID", StateID);
        //                QueryBuilderObject.SetField("LanguageID", "1");
        //                QueryBuilderObject.SetField("Description", "'" + Region + "'");
        //                err = QueryBuilderObject.InsertQueryString("StateLanguage", db_vms);
        //                QueryBuilderObject.SetField("CountryID", "1");
        //                QueryBuilderObject.SetField("StateID", StateID);
        //                QueryBuilderObject.SetField("LanguageID", "2");
        //                QueryBuilderObject.SetField("Description", "'" + Region + "'");
        //                err = QueryBuilderObject.InsertQueryString("StateLanguage", db_vms);
        //            }
        //            string CityID = GetFieldValue("CityLanguage", "CityID", "Description='" + StateDescription + "' and LanguageID=1", db_vms).Trim();
        //            if (CityID.Equals(string.Empty))
        //            {
        //                CityID = GetFieldValue("City", "isnull(max(CityID),0)+1", db_vms).Trim();
        //                QueryBuilderObject.SetField("CountryID", "1");
        //                QueryBuilderObject.SetField("StateID", StateID);
        //                QueryBuilderObject.SetField("CityID", CityID);
        //                err = QueryBuilderObject.InsertQueryString("City", db_vms);
        //                QueryBuilderObject.SetField("CountryID", "1");
        //                QueryBuilderObject.SetField("StateID", StateID);
        //                QueryBuilderObject.SetField("CityID", CityID);
        //                QueryBuilderObject.SetField("LanguageID", "1");
        //                QueryBuilderObject.SetField("Description", "'" + StateDescription + "'");
        //                err = QueryBuilderObject.InsertQueryString("CityLanguage", db_vms);
        //                QueryBuilderObject.SetField("CountryID", "1");
        //                QueryBuilderObject.SetField("StateID", StateID);
        //                QueryBuilderObject.SetField("CityID", CityID);
        //                QueryBuilderObject.SetField("LanguageID", "2");
        //                QueryBuilderObject.SetField("Description", "'" + StateDescription + "'");
        //                err = QueryBuilderObject.InsertQueryString("CityLanguage", db_vms);
        //            }
        //            string AreaID = GetFieldValue("AreaLanguage", "AreaID", "Description='" + AreaLocation + "' and LanguageID=1", db_vms).Trim();
        //            if (AreaID.Equals(string.Empty))
        //            {
        //                AreaID = GetFieldValue("Area", "isnull(max(AreaID),0)+1", db_vms).Trim();
        //                QueryBuilderObject.SetField("CountryID", "1");
        //                QueryBuilderObject.SetField("StateID", StateID);
        //                QueryBuilderObject.SetField("CityID", CityID);
        //                QueryBuilderObject.SetField("AreaID", AreaID);
        //                err = QueryBuilderObject.InsertQueryString("Area", db_vms);
        //                QueryBuilderObject.SetField("CountryID", "1");
        //                QueryBuilderObject.SetField("StateID", StateID);
        //                QueryBuilderObject.SetField("CityID", CityID);
        //                QueryBuilderObject.SetField("AreaID", AreaID);
        //                QueryBuilderObject.SetField("LanguageID", "1");
        //                QueryBuilderObject.SetField("Description", "'" + AreaLocation + "'");
        //                err = QueryBuilderObject.InsertQueryString("AreaLanguage", db_vms);
        //                QueryBuilderObject.SetField("CountryID", "1");
        //                QueryBuilderObject.SetField("StateID", StateID);
        //                QueryBuilderObject.SetField("CityID", CityID);
        //                QueryBuilderObject.SetField("AreaID", AreaID);
        //                QueryBuilderObject.SetField("LanguageID", "2");
        //                QueryBuilderObject.SetField("Description", "'" + AreaLocation + "'");
        //                err = QueryBuilderObject.InsertQueryString("AreaLanguage", db_vms);
        //            }
        //            string StreetID = GetFieldValue("StreetLanguage", "StreetID", "Description='" + streetDescription + "' and LanguageID=1", db_vms).Trim();
        //            if (StreetID.Equals(string.Empty))
        //            {
        //                StreetID = GetFieldValue("Street", "isnull(max(StreetID),0)+1", db_vms).Trim();
        //                QueryBuilderObject.SetField("CountryID", "1");
        //                QueryBuilderObject.SetField("StateID", StateID);
        //                QueryBuilderObject.SetField("CityID", CityID);
        //                QueryBuilderObject.SetField("AreaID", AreaID);
        //                QueryBuilderObject.SetField("StreetID", StreetID);
        //                err = QueryBuilderObject.InsertQueryString("Street", db_vms);
        //                QueryBuilderObject.SetField("CountryID", "1");
        //                QueryBuilderObject.SetField("StateID", StateID);
        //                QueryBuilderObject.SetField("CityID", CityID);
        //                QueryBuilderObject.SetField("AreaID", AreaID);
        //                QueryBuilderObject.SetField("StreetID", StreetID);
        //                QueryBuilderObject.SetField("LanguageID", "1");
        //                QueryBuilderObject.SetField("Description", "'" + AreaLocation + "'");
        //                err = QueryBuilderObject.InsertQueryString("StreetLanguage", db_vms);
        //                QueryBuilderObject.SetField("CountryID", "1");
        //                QueryBuilderObject.SetField("StateID", StateID);
        //                QueryBuilderObject.SetField("CityID", CityID);
        //                QueryBuilderObject.SetField("AreaID", AreaID);
        //                QueryBuilderObject.SetField("StreetID", StreetID);
        //                QueryBuilderObject.SetField("LanguageID", "2");
        //                QueryBuilderObject.SetField("Description", "'" + AreaLocation + "'");
        //                err = QueryBuilderObject.InsertQueryString("StreetLanguage", db_vms);
        //            }
        //            string CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode='" + custCode + "'", db_vms).Trim();
        //            string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerCode='" + custCode + "'", db_vms).Trim();
        //            if (CustomerID.Equals(string.Empty) || OutletID.Equals(string.Empty)) continue;
        //            QueryBuilderObject.SetField("StreetID", StreetID);
        //            err = QueryBuilderObject.UpdateQueryString("CustomerOutlet", "CustomerID=" + CustomerID + " and OutletID=" + OutletID + "", db_vms);
        //        }
        //        DT.Dispose();

        //        WriteMessage("\r\n");
        //        WriteMessage("<<< Geo Location >>> Total Updated = " + TOTALUPDATE + " , Total Inserted = " + TOTALINSERT);
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}
        #endregion

        #region UpdatePrice

        public override void UpdatePrice()
        {
            int TOTALUPDATED = 0;

            InCubeErrors err;
            object field = new object();
            try
            {
                if (!db_vms.IsOpened())
                {
                    WriteMessage("\r\n");
                    WriteMessage("Cannot connect to GP database , please check the connection");
                    return;
                }
                WriteMessage("Prices integration started ..\r\n");
                DataTable DT = new DataTable();
                //                string SelectGroup = @"SELECT PL.*, I.WITM_ITEM_ANLY_10 FROM
                //(SELECT DISTINCT WPLS_PLIST_CODE,WPLS_PLIST_DESC,WPLS_ITEM_CODE,WPLS_UOM,WPLS_EFF_FM_DT
                //,WPLS_EFF_TO_DT,WPLS_PRICE,WPLS_DFLT_YN,WPLS_CR_DT,WPLS_PROC_FLAG FROM OW_PRICE_LIST_MAS) PL INNER JOIN
                //(SELECT WPLS_PLIST_CODE,WPLS_ITEM_CODE,WPLS_UOM,MAX(WPLS_CR_DT) WPLS_CR_DT,WPLS_PROC_FLAG FROM OW_PRICE_LIST_MAS  GROUP BY WPLS_PLIST_CODE,WPLS_ITEM_CODE,WPLS_UOM,WPLS_PROC_FLAG) P 
                //ON P.WPLS_PLIST_CODE = PL.WPLS_PLIST_CODE AND P.WPLS_ITEM_CODE = PL.WPLS_ITEM_CODE AND P.WPLS_UOM = PL.WPLS_UOM AND P.WPLS_CR_DT = PL.WPLS_CR_DT AND P.WPLS_PROC_FLAG = PL.WPLS_PROC_FLAG
                //INNER JOIN (SELECT DISTINCT WITM_ITEM_CODE,WITM_ITEM_ANLY_10 FROM OW_ITEM_MAS I WHERE (I.WITM_DIVISION='DIARY' AND I.WITM_CATG_CODE = '002') OR (I.WITM_DIVISION = 'FOOD' AND I.WITM_CATG_CODE='00001')) I 
                //ON I.WITM_ITEM_CODE = P.WPLS_ITEM_CODE WHERE I.WITM_ITEM_ANLY_10 = 'I-STOCK' AND PL.WPLS_PROC_FLAG = 'N'";


                string SelectGroup = @"SELECT WPLS_PLIST_CODE,WPLS_PLIST_DESC,WPLS_ITEM_CODE,WPLS_UOM ,WPLS_EFF_FM_DT,WPLS_EFF_TO_DT,ROUND(WPLS_PRICE,5) WPLS_PRICE,WPLS_DFLT_YN,WPLS_CR_DT,WPLS_PROC_FLAG,WITM_ITEM_CODE,WITM_ITEM_ANLY_10,WITM_DIVISION
                FROM (SELECT DISTINCT WITM_ITEM_CODE,WITM_ITEM_ANLY_10,WITM_DIVISION 
                FROM OW_ITEM_MAS I 
                WHERE (I.WITM_DIVISION='DAIRY' AND I.WITM_CATG_CODE = '002') OR (I.WITM_DIVISION = 'FOOD' AND I.WITM_CATG_CODE='00001')) A,( SELECT P.WPLS_PLIST_CODE,P.WPLS_PLIST_DESC,P.WPLS_ITEM_CODE,P.WPLS_UOM ,P.WPLS_EFF_FM_DT,P.WPLS_EFF_TO_DT,P.WPLS_PRICE,P.WPLS_DFLT_YN,P.WPLS_CR_DT,P.WPLS_PROC_FLAG
                FROM  (SELECT DISTINCT WPLS_PLIST_CODE,WPLS_PLIST_DESC,WPLS_ITEM_CODE,WPLS_UOM,WPLS_EFF_FM_DT
                ,WPLS_EFF_TO_DT,WPLS_PRICE,WPLS_DFLT_YN,WPLS_CR_DT,WPLS_PROC_FLAG FROM OW_PRICE_LIST_MAS) P,
                (SELECT WPLS_PLIST_CODE,WPLS_ITEM_CODE,WPLS_UOM,MAX(WPLS_CR_DT) WPLS_CR_DT,WPLS_PROC_FLAG FROM OW_PRICE_LIST_MAS 
                GROUP BY WPLS_PLIST_CODE,WPLS_ITEM_CODE,WPLS_UOM,WPLS_PROC_FLAG)PL
                WHERE P.WPLS_PLIST_CODE = PL.WPLS_PLIST_CODE AND P.WPLS_ITEM_CODE = PL.WPLS_ITEM_CODE AND P.WPLS_UOM = PL.WPLS_UOM AND P.WPLS_CR_DT = PL.WPLS_CR_DT AND P.WPLS_PROC_FLAG = PL.WPLS_PROC_FLAG)B
                WHERE WPLS_ITEM_CODE=WITM_ITEM_CODE AND WPLS_PROC_FLAG='N'
                AND WITM_ITEM_ANLY_10 IS NOT NULL
                GROUP BY WPLS_PLIST_CODE,WPLS_PLIST_DESC,WPLS_ITEM_CODE,WPLS_UOM ,WPLS_EFF_FM_DT,WPLS_EFF_TO_DT,WPLS_PRICE,WPLS_DFLT_YN,WPLS_CR_DT,WPLS_PROC_FLAG,WITM_ITEM_CODE,WITM_ITEM_ANLY_10,WITM_DIVISION";

                OracleDataAdapter adapter = new OracleDataAdapter(SelectGroup, Conn);
                adapter.Fill(DT);
                WriteMessage("Rows found: " + DT.Rows.Count.ToString() + "\r\n");
                SaveTable(DT, "Stg_Prices", false);
                ClearProgress();
                SetProgressMax(DT.Rows.Count);

                foreach (DataRow row in DT.Rows)
                {
                    ReportProgress("Updating Prices");

                    bool defaultList = false;
                    string CustomerGroupID = string.Empty;
                    string PL_CODE = row["WPLS_PLIST_CODE"].ToString().Trim();
                    string PL_NAME = row["WPLS_PLIST_DESC"].ToString().Trim();
                    string ITEM_CODE = row["WPLS_ITEM_CODE"].ToString().Trim();
                    string UOM_CODE = row["WPLS_UOM"].ToString().Trim();
                    string WPLS_EFF_FM_DT = row["WPLS_EFF_FM_DT"].ToString().Trim();
                    string WPLS_EFF_TO_DT = row["WPLS_EFF_TO_DT"].ToString().Trim();
                    decimal PRICE = decimal.Parse(row["WPLS_PRICE"].ToString().Trim());
                    string IS_DEFAULT_PRICE = row["WPLS_DFLT_YN"].ToString().Trim();
                    string Division = row["WITM_DIVISION"].ToString().Trim();

                    if (IS_DEFAULT_PRICE.ToLower().Equals("y"))
                    {
                        defaultList = true;
                    }
                    else
                    {
                        defaultList = false;
                    }
                    string PriceListID = "1";

                    string itemID = GetFieldValue("Item", "ItemID", "ItemCode='" + ITEM_CODE + "'", db_vms).Trim();
                    if (itemID.Equals(string.Empty)) continue;
                    string packTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + UOM_CODE + "'", db_vms).Trim();
                    if (packTypeID.Equals(string.Empty)) continue;
                    string packID = GetFieldValue("Pack", "PackID", "ItemID=" + itemID + " and PackTypeID=" + packTypeID + "", db_vms).Trim();
                    if (packID.Equals(string.Empty)) continue;
                    string tax = GetFieldValue("Pack", "ISNULL(Width,0)", "PackID = " + packID, db_vms).Trim();
                    if (tax.Equals(string.Empty)) continue;


                    WriteMessage("\r\n");
                    WriteMessage("Check PriceList " + PL_CODE);
                    err = ExistObject("PriceList", "PriceListID", " PriceListCode = '" + PL_CODE + "'", db_vms);
                    if (err == InCubeErrors.Success)
                    {
                        PriceListID = GetFieldValue("PriceList", "PriceListID", " PriceListCode = '" + PL_CODE + "'", db_vms);
                        QueryBuilderObject.SetField("StartDate", "'" + DateTime.Parse(WPLS_EFF_FM_DT).ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + DateTime.Parse("31/12/2099").ToString(DateFormat) + "'");
                        QueryBuilderObject.UpdateQueryString("PriceList", "PriceListID=" + PriceListID + "", db_vms);
                        WriteMessage("\r\n");
                        WriteMessage("Update PriceList " + PL_CODE);
                    }
                    else
                    {
                        PriceListID = GetFieldValue("PriceList", "ISNULL(MAX(PriceListID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("PriceListCode", "'" + PL_CODE + "'");
                        QueryBuilderObject.SetField("StartDate", "'" + DateTime.Parse(WPLS_EFF_FM_DT).ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + DateTime.Parse("31/12/2099").ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("Priority", "1");
                        err = QueryBuilderObject.InsertQueryString("PriceList", db_vms);

                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + PL_NAME + "'");
                        QueryBuilderObject.InsertQueryString("PriceListLanguage", db_vms);
                    }

                    if (defaultList)
                    {
                        QueryBuilderObject.SetField("KeyValue", PriceListID);
                        QueryBuilderObject.UpdateQueryString("Configuration", "KeyName = 'DefaultPriceListID' AND EmployeeID = -1", db_vms);
                    }

                    ReportProgress("Updating Price (" + PL_NAME + ")");

                    TOTALUPDATED++;

                    int PriceDefinitionID = 1;
                    string currentPrice = GetFieldValue("PriceDefinition", "Price", "PackID = " + packID + " AND PriceListID = " + PriceListID, db_vms);
                    if (currentPrice.Equals(string.Empty))
                    {
                        // if there is no default price level, then always insert one.
                        PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms));
                        QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID.ToString());
                        QueryBuilderObject.SetField("QuantityRangeID", "1");
                        QueryBuilderObject.SetField("PackID", packID);
                        QueryBuilderObject.SetField("CurrencyID", "1");
                        QueryBuilderObject.SetField("Tax", tax);
                        QueryBuilderObject.SetField("Price", PRICE.ToString());
                        QueryBuilderObject.SetField("MinPrice", PRICE.ToString());
                        QueryBuilderObject.SetField("MaxPrice", PRICE.ToString());
                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        err = QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);
                    }
                    else
                    {
                        PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "PriceDefinitionID", "PackID = " + packID + " AND PriceListID = " + PriceListID, db_vms));
                        //decimal currentMinPrice = decimal.Parse(GetFieldValue("PriceDefinition", "ISNULL(MinPrice,0)", "PriceDefinitionID = " + PriceDefinitionID, db_vms));
                        //decimal currentMaxPrice = decimal.Parse(GetFieldValue("PriceDefinition", "ISNULL(MaxPrice,0)", "PriceDefinitionID = " + PriceDefinitionID, db_vms));
                        //decimal currentTax = decimal.Parse(GetFieldValue("PriceDefinition", "ISNULL(Tax,0)", "PriceDefinitionID = " + PriceDefinitionID, db_vms));

                        //if (decimal.Parse(currentPrice) != PRICE || currentMaxPrice < PRICE || currentMinPrice > PRICE || currentTax != decimal.Parse(tax))
                        if (Division == "FOOD")
                        {
                            //WriteMessage("Update FOOD ..\r\n");
                            QueryBuilderObject = new QueryBuilder();
                            QueryBuilderObject.SetField("Price", PRICE.ToString());
                            QueryBuilderObject.SetField("MaxPrice", PRICE.ToString());
                            QueryBuilderObject.SetField("MinPrice", PRICE.ToString());
                            QueryBuilderObject.SetField("Tax", tax);
                            err = QueryBuilderObject.UpdateQueryString("PriceDefinition", "PackID = " + packID + " AND PriceListID = " + PriceListID + " AND PriceDefinitionID = " + PriceDefinitionID, db_vms);
                        }
                        else
                        {
                            //WriteMessage("Update Dairy ..\r\n");
                            QueryBuilderObject = new QueryBuilder();
                            QueryBuilderObject.SetField("Price", PRICE.ToString());
                            QueryBuilderObject.SetField("Tax", tax);
                            err = QueryBuilderObject.UpdateQueryString("PriceDefinition", "PackID = " + packID + " AND PriceListID = " + PriceListID + " AND PriceDefinitionID = " + PriceDefinitionID, db_vms);
                        }
                    }
                    if (err == InCubeErrors.Success)
                    {
                        err = UpdateFlag("OW_PRICE_LIST_MAS", "WPLS_ITEM_CODE='" + ITEM_CODE + "' ", "WPLS");
                    }
                }

                #region COMMENTED
                //DT = new DataTable();
                //SelectGroup = @"select P.PackID,ISNULL(P.Width,0) Tax,P.ItemID,PD.Price,P.Quantity,PD.PriceListID from Pack P inner join PriceDefinition PD on PD.PacKID=P.PackID  and PD.PriceListID = 1";
                //InCubeQuery GroupQuery = new InCubeQuery(db_vms, SelectGroup);
                //err = GroupQuery.Execute();
                //DT = GroupQuery.GetDataTable();
                //ClearProgress();
                //SetProgressMax(DT.Rows.Count);
                //foreach (DataRow row in DT.Rows)
                //{
                //    ReportProgress("Updating Base UOM Prices");

                //    string ItemID = row["ItemID"].ToString().Trim();
                //    string PackID = row["PackID"].ToString().Trim();
                //    string Price = row["Price"].ToString().Trim();
                //    string tax = row["Tax"].ToString().Trim();
                //    string Quantity = row["Quantity"].ToString().Trim();
                //    string PriceListID = row["PriceListID"].ToString().Trim();
                //    if (decimal.Parse(Quantity) == 0) continue;
                //    string basePack = GetFieldValue("Pack", "PackID", "ItemID=" + ItemID + " and Quantity=1", db_vms).Trim();
                //    if (basePack.Equals(string.Empty)) continue;
                //    decimal basePrice = 0;
                //    basePrice = decimal.Round((decimal.Parse(Price) / decimal.Parse(Quantity)), 3);

                //    string existPrice = GetFieldValue("PriceDefinition", "Price", "PackID=" + basePack + " and PriceListID=" + PriceListID + "", db_vms).Trim();
                //    if (existPrice.Equals(string.Empty))
                //    {
                //        int PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms));
                //        QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID.ToString());
                //        QueryBuilderObject.SetField("QuantityRangeID", "1");
                //        QueryBuilderObject.SetField("PackID", basePack);
                //        QueryBuilderObject.SetField("CurrencyID", "1");
                //        QueryBuilderObject.SetField("Tax", tax);
                //        QueryBuilderObject.SetField("Price", basePrice.ToString());
                //        QueryBuilderObject.SetField("MinPrice", basePrice.ToString());
                //        QueryBuilderObject.SetField("MaxPrice", basePrice.ToString());
                //        QueryBuilderObject.SetField("PriceListID", PriceListID);
                //        err = QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);
                //    }
                //    else
                //    {
                //        int PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "PriceDefinitionID", "PackID = " + basePack + " AND PriceListID = " + PriceListID, db_vms));
                //        //decimal currentMinPrice = decimal.Parse(GetFieldValue("PriceDefinition", "ISNULL(MinPrice,0)", "PriceDefinitionID = " + PriceDefinitionID, db_vms));
                //        //decimal currentMaxPrice = decimal.Parse(GetFieldValue("PriceDefinition", "ISNULL(MaxPrice,0)", "PriceDefinitionID = " + PriceDefinitionID, db_vms));
                //        //decimal currentTax = decimal.Parse(GetFieldValue("PriceDefinition", "ISNULL(Tax,0)", "PriceDefinitionID = " + PriceDefinitionID, db_vms));
                //        //if (decimal.Parse(existPrice) != basePrice || currentMaxPrice < basePrice || currentMinPrice > basePrice || currentTax != decimal.Parse(tax))
                //        {
                //            if (Division == "FOOD")
                //            {
                //                WriteMessage("Update FOOD ..\r\n");
                //                QueryBuilderObject = new QueryBuilder();
                //                QueryBuilderObject.SetField("Price", Price.ToString());
                //                QueryBuilderObject.SetField("MaxPrice", Price.ToString());
                //                QueryBuilderObject.SetField("MinPrice", Price.ToString());
                //                QueryBuilderObject.SetField("Tax", tax);
                //                err = QueryBuilderObject.UpdateQueryString("PriceDefinition", "PackID = " + packID + " AND PriceListID = " + PriceListID + " AND PriceDefinitionID = " + PriceDefinitionID, db_vms);
                //            }
                //            else
                //            {
                //                WriteMessage("Update Dairy ..\r\n");
                //                QueryBuilderObject = new QueryBuilder();
                //                QueryBuilderObject.SetField("Price", PRICE.ToString());
                //                QueryBuilderObject.SetField("Tax", tax);
                //                err = QueryBuilderObject.UpdateQueryString("PriceDefinition", "PackID = " + packID + " AND PriceListID = " + PriceListID + " AND PriceDefinitionID = " + PriceDefinitionID, db_vms);
                //            }
                //            //QueryBuilderObject = new QueryBuilder();
                //            //if (decimal.Parse(existPrice) != basePrice)
                //            //    QueryBuilderObject.SetField("Price", basePrice.ToString());
                //            //if (currentMaxPrice < basePrice)
                //            //    QueryBuilderObject.SetField("MaxPrice", basePrice.ToString());
                //            //if (currentMinPrice > basePrice)
                //            //    QueryBuilderObject.SetField("MinPrice", basePrice.ToString());
                //            //if (currentTax != decimal.Parse(tax))
                //            //    QueryBuilderObject.SetField("Tax", tax);
                //            //err = QueryBuilderObject.UpdateQueryString("PriceDefinition", "PackID = " + basePack + " AND PriceListID = " + PriceListID + " AND PriceDefinitionID = " + PriceDefinitionID, db_vms);
                //        }
                //    }
                //}
                #endregion
                //InCubeQuery itemQry = new InCubeQuery("SP_DELETE_ITEMS_WITH_NO_PRICES", db_vms);
                //err = itemQry.ExecuteStoredProcedure();
                if (DT.Rows.Count > 0)
                {
                    WriteExceptions("PRICE UPDATE COMPLETED.. NUMBER OF RECORDS IS  " + DT.Rows.Count.ToString() + "", "PRICE UPDATE BEGIN..", false);
                }
                WriteMessage("\r\n");
                WriteMessage("<<< PRICE >>> Total Updated = " + TOTALUPDATED);

            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void UpdatePriceList(string priceListName, string CustID, string outletID, bool defaultList, ref int TOTALUPDATED)
        {
            object field = new object();
            InCubeErrors err;


            string priceQry = @"SELECT PL.PL_CODE,PL.PL_NAME,PLI.PLI_ITEM_CODE,PLI.PLI_UOM_CODE,PLI.PLI_RATE
FROM dbo.OM_PRICE_LIST_ITEM PLI
INNER JOIN OM_PRICE_LIST PL
ON PL.PL_CODE=PLI.PLI_PL_CODE ORDER BY PL_CODE
";



            InCubeQuery ItemPriceQuery = new InCubeQuery(db_vms, priceQry);
            err = ItemPriceQuery.Execute();

            ClearProgress();
            SetProgressMax(ItemPriceQuery.GetDataTable().Rows.Count);




            while (err == InCubeErrors.Success)
            {

                err = ItemPriceQuery.FindNext();
            }
            ItemPriceQuery.Close();
        }

        #endregion

        #region UpdateSalesPerson

        public override void UpdateSalesPerson()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;
            string DivisionID = "1";
            string OrganizationID = "1";

            InCubeErrors err;
            object field = new object();

            if (!db_vms.IsOpened())
            {
                WriteMessage("\r\n");
                WriteMessage("Cannot connect to GP database , please check the connection");
                return;
            }

            // UpdateOrganization();

            DataTable DT = new DataTable();
            if (Conn.State == ConnectionState.Closed)
            {
                Conn.Open();
            }
            OracleDataAdapter adapter = new OracleDataAdapter(@"select * from OW_SALESMAN_MAS", Conn);
            adapter.Fill(DT);
            /*
             * We do not maintain salesman information in GP, 
             * instead we assign the salesman name and the helper name for a 
             * particular route code by using the field ADDRESS1 in table IV40700.  
             * Please refer to the following field definitions.
             * Table IV40700	 
             * Field Name 	Field Data
             * LOCNCODE	route code (dairy 3 chars, poultry 4 chars)
             * ADDRESS1	salesman and helper name
             * ZIPCODE	vehicle reg no.
             * STATE	depot code (ALN,AUH,DXB,SHJ,NE)
            */



            ClearProgress();
            SetProgressMax(DT.Rows.Count);



            foreach (DataRow dr in DT.Rows)
            {
                ReportProgress("Updating Salesperon");

                string SalespersonCode = dr["WSMN_SM_CODE"].ToString().Trim();

                DivisionID = "1";// GetFieldValue("Division", "DivisionID", "DivisionCode='" + companycode + "'", db_vms);
                //if (DivisionID.Equals(string.Empty)) ; //continue;
                string SalespersonName = dr["WSMN_SM_NAME"].ToString().Trim();
                string EMPLOYEE_TYPE = "2";
                string VehicleCode = dr["WSMN_VEHICLE_CODE"].ToString().Trim();
                string VehicleID = GetFieldValue("warehouse", "WarehouseID", "WarehouseCode='" + VehicleCode + "'", db_vms);


                OrganizationID = "1";

                AddUpdateSalesperson(EMPLOYEE_TYPE, SalespersonCode, SalespersonName, ref TOTALUPDATED, ref TOTALINSERTED, DivisionID, OrganizationID, VehicleID);

                err = UpdateFlag("OW_SALESMAN_MAS", "WSMN_SM_CODE='" + SalespersonCode + "'", "WSMN");
            }

            DT.Dispose();
            WriteMessage("\r\n");
            WriteMessage("<<< SALESPERSON >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        private void AddUpdateSalesperson(string employeeType, string SalespersonCode, string SalespersonName, ref int TOTALUPDATED, ref int TOTALINSERTED, string DivisionID, string OrganizationID, string VehicleID)
        {
            InCubeErrors err;
            string SalespersonID = string.Empty;
            SalespersonID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode = '" + SalespersonCode + "'", db_vms).Trim();
            if (SalespersonID.Equals(string.Empty))// New Salesperon --- Insert Query
            {
                TOTALINSERTED++;
                SalespersonID = GetFieldValue("Employee", "isnull(MAX(EmployeeID),0) + 1", db_vms);

                QueryBuilderObject.SetField("EmployeeID", SalespersonID);

                QueryBuilderObject.SetField("EmployeeCode", "'" + SalespersonCode + "'");
                QueryBuilderObject.SetField("NationalIDNumber", "0");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.SetField("InActive", "0");
                QueryBuilderObject.SetField("OnHold", "0");
                QueryBuilderObject.SetField("EmployeeTypeID", employeeType);

                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                err = QueryBuilderObject.InsertQueryString("Employee", db_vms);

                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + SalespersonName + "'");

                QueryBuilderObject.InsertQueryString("EmployeeLanguage", db_vms);

                err = ExistObject("Operator", "OperatorID", "OperatorID = " + SalespersonID, db_vms);
                if (err == InCubeErrors.DBNoMoreRows)
                {
                    QueryBuilderObject.SetField("OperatorID", SalespersonID);
                    QueryBuilderObject.SetField("OperatorName", "'" + SalespersonCode + "'");
                    QueryBuilderObject.SetField("FrontOffice", "1");
                    QueryBuilderObject.SetField("LoginTypeID", "1");
                    err = QueryBuilderObject.InsertQueryString("Operator", db_vms);
                }

                err = ExistObject("EmployeeOperator", "EmployeeID", "EmployeeID = " + SalespersonID, db_vms);
                if (err == InCubeErrors.DBNoMoreRows)
                {
                    QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                    QueryBuilderObject.SetField("OperatorID", SalespersonID);
                    QueryBuilderObject.InsertQueryString("EmployeeOperator", db_vms);
                }
            }
            else
            {
                TOTALUPDATED++;
                QueryBuilderObject.SetField("Description", "'" + SalespersonName + "'");

                QueryBuilderObject.UpdateQueryString("EmployeeLanguage", "EmployeeID = " + SalespersonID, db_vms);
            }



            //err = ExistObject("EmployeeDivision", "EmployeeID", "EmployeeID = " + SalespersonID + " AND DivisionID = " + DivisionID, db_vms);
            //if (err == InCubeErrors.DBNoMoreRows)
            //{
            //    QueryBuilderObject.SetField("EmployeeID", SalespersonID);
            //    QueryBuilderObject.SetField("DivisionID", DivisionID);
            //    err = QueryBuilderObject.InsertQueryString("EmployeeDivision", db_vms);
            //}

            err = ExistObject("EmployeeVehicle", "EmployeeID", "VehicleID = " + VehicleID + " AND EmployeeID = " + SalespersonID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("VehicleID", VehicleID);
                err = QueryBuilderObject.InsertQueryString("EmployeeVehicle", db_vms);
            }

            err = ExistObject("EmployeeOrganization", "EmployeeID", "EmployeeID = " + SalespersonID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                err = QueryBuilderObject.InsertQueryString("EmployeeOrganization", db_vms);
            }

            int AccountID = 1;

            err = ExistObject("AccountEmp", "AccountID", "EmployeeID = " + SalespersonID, db_vms);
            if (err != InCubeErrors.Success)
            {
                AccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("AccountTypeID", "2");
                QueryBuilderObject.SetField("CreditLimit", "500000");
                QueryBuilderObject.SetField("Balance", "0");
                QueryBuilderObject.SetField("GL", "0");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.SetField("CurrencyID", "1");
                err = QueryBuilderObject.InsertQueryString("Account", db_vms);

                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                err = QueryBuilderObject.InsertQueryString("AccountEmp", db_vms);

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + SalespersonName.Trim() + " Account'");
                err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
            }

            string TerritoryID = GetFieldValue("TerritoryLanguage", "TerritoryID", "Description='" + SalespersonName + "'", db_vms);
            if (TerritoryID == string.Empty)
            {
                TerritoryID = GetFieldValue("[Territory]", "isnull(max(TerritoryID),0)+1", db_vms);
                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                QueryBuilderObject.SetField("OrganizationID", "1");
                QueryBuilderObject.SetField("TerritoryCode", "'" + SalespersonCode + "'");
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                err = QueryBuilderObject.InsertQueryString("Territory", db_vms);

                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + SalespersonName + "'");
                QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);

                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "'" + SalespersonName + "'");
                err = QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);

            }
            else
            {
                QueryBuilderObject.SetField("Description", "'" + SalespersonName + "'");
                err = QueryBuilderObject.UpdateQueryString("TerritoryLanguage", "TerritoryID=" + TerritoryID + "", db_vms);

            }
            string existsET = GetFieldValue("EmployeeTerritory", "TerritoryID", "EmployeeID=" + SalespersonID + " and TerritoryID=" + TerritoryID + "", db_vms).Trim();
            if (existsET.Equals(string.Empty))
            {
                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                err = QueryBuilderObject.InsertQueryString("EmployeeTerritory", db_vms);
            }
            if (err == InCubeErrors.Success || err == InCubeErrors.DBNoMoreRows)
            {



            }

        }

        #endregion

        #region UpdateVehicles

        public override void UpdateWarehouse()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            string OrganizationID = "1";
            object field = new object();

            if (!db_vms.IsOpened())
            {
                WriteMessage("\r\n");
                WriteMessage("Cannot connect to GP database , please check the connection");
                return;
            }

            UpdateOrganization();
            DataTable DT = new DataTable();
            if (Conn.State == ConnectionState.Closed)
            {
                Conn.Open();
            }
            OracleDataAdapter adapter = new OracleDataAdapter(@"select * from OW_WAREHOUSE_MAS where WLOC_PROC_FLAG='N'", Conn);
            adapter.Fill(DT);




            ClearProgress();
            SetProgressMax(DT.Rows.Count);



            foreach (DataRow dr in DT.Rows)
            {
                ReportProgress("Updating Vehicles");

                string WarehouseCode = dr["WLOC_LOCN_CODE"].ToString().Trim();


                string WarehouceName = dr["WLOC_LOCN_DESC"].ToString().Trim();

                string WarehouseType = string.Empty;
                WarehouseType = "2";
                if (WarehouseCode.ToLower().Equals("aq")) WarehouseType = "1";

                OrganizationID = "1";// GetFieldValue("OrganizationLanguage", "OrganizationID", "Description = 'Default Organization'", db_vms);

                AddUpdateWarehouse(WarehouseType, WarehouseCode, WarehouceName, ref TOTALUPDATED, ref TOTALINSERTED, OrganizationID);
                UpdateFlag("OW_WAREHOUSE_MAS", "WLOC_LOCN_CODE='" + WarehouseCode + "' ", "WLOC");

            }

            DT.Dispose();
            WriteMessage("\r\n");
            WriteMessage("<<< VEHICLES >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        private void AddUpdateWarehouse(string warehouseType, string WarehouseCode, string WarehouceName, ref int TOTALUPDATED, ref int TOTALINSERTED, string OrganizationID)
        {
            InCubeErrors err;
            string WarehouseID = string.Empty;
            WarehouseID = GetFieldValue("Warehouse", "WarehouseID", "WarehouseCode = '" + WarehouseCode + "'", db_vms).Trim();
            if (!WarehouseID.Equals(string.Empty)) // Exist Warehouse --- Update Query
            {
                TOTALUPDATED++;
                QueryBuilderObject.SetField("Barcode", "'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("WarehouseCode", "'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                err = QueryBuilderObject.UpdateQueryString("Warehouse", " WarehouseID = " + WarehouseID, db_vms);

                QueryBuilderObject.SetField("Description", "'" + WarehouceName + "'");
                QueryBuilderObject.UpdateQueryString("WarehouseLanguage", " WarehouseID =" + WarehouseID + " AND LanguageID = 1", db_vms);
            }
            else
            {
                TOTALINSERTED++;
                WarehouseID = GetFieldValue("Warehouse", "isnull(MAX(WarehouseID),0) + 1", db_vms);
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("Barcode", "'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("WarehouseCode", "'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.SetField("WarehouseTypeID", warehouseType);
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                err = QueryBuilderObject.InsertQueryString("Warehouse", db_vms);

                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + WarehouceName + "'");
                QueryBuilderObject.InsertQueryString("WarehouseLanguage", db_vms);
            }



            #region WarehouseZone/Vehicle/VehicleSalesPerson

            err = ExistObject("WarehouseZone", "WarehouseID", "WarehouseID = " + WarehouseID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("ZoneID", "1");
                err = QueryBuilderObject.InsertQueryString("WarehouseZone", db_vms);
            }

            err = ExistObject("WarehouseZoneLanguage", "WarehouseID", "WarehouseID = " + WarehouseID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("ZoneID", "1");
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + WarehouceName + " Zone'");
                QueryBuilderObject.InsertQueryString("WarehouseZoneLanguage", db_vms);
            }


            if (warehouseType.Trim().Equals("2"))
            {
                QueryBuilderObject.SetField("VehicleID", WarehouseID);
                QueryBuilderObject.SetField("TypeID", "1");

                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                err = QueryBuilderObject.InsertQueryString("Vehicle", db_vms);
            }

            //string SalesPersonID = GetFieldValue("Employee", "EmployeeID", " EmployeeCode = '" + SalesmanCode + "'", db_vms);
            //string VehicleID = GetFieldValue("Vehicle", "VehicleID", "VehicleID=" + WarehouseID, db_vms);

            //if (!SalesPersonID.Trim().Equals(string.Empty) && !VehicleID.Trim().Equals(string.Empty))
            //{
            //    err = ExistObject("EmployeeVehicle", "VehicleID", "VehicleID = " + WarehouseID + " AND EmployeeID = " + SalesPersonID, db_vms);
            //    if (err != InCubeErrors.Success)
            //    {
            //        QueryBuilderObject.SetField("VehicleID", VehicleID);
            //        QueryBuilderObject.SetField("EmployeeID", SalesPersonID);
            //        QueryBuilderObject.InsertQueryString("EmployeeVehicle", db_vms);
            //    }
            //}

            #endregion
        }

        #endregion

        #region Update Route
        // VVS WILL MANUALLY CREATE AND ASSIGN THE ROUTES 

        public override void UpdateRoutes()
        {
            try
            {
                int TOTALINSERTED = 0;

                InCubeErrors err;
                object field = new object();

                if (!db_vms.IsOpened())
                {
                    WriteMessage("\r\n");
                    WriteMessage("Cannot connect to GP database , please check the connection");
                    return;
                }

                UpdateOrganization();
                DataTable tbl = new DataTable();

                string SelectRoute = @"SELECT ltrim(rtrim(cast(R.DEX_ROW_ID as nvarchar(50)))) AS ROUTEID,
SP.SLPRSNID,
ltrim(rtrim(cast(CC.DEX_ROW_ID as nvarchar(50)))) AS CUSTOMERID,
CC.CUSTNMBR,
ltrim(rtrim(cast(C.DEX_ROW_ID as nvarchar(50)))) as ADDRESSSCODE,
            (ltrim(rtrim(R.SALSTERR))+'--'+ltrim(rtrim(R.SLTERDSC))) AS ROUTENAME

FROM RM00101 CC 
INNER JOIN rm00301 SP ON CC.SLPRSNID=SP.SLPRSNID
INNER JOIN rm00303 R ON CC.SALSTERR=R.SALSTERR
INNER JOIN RM00102 C ON CC.CUSTNMBR=C.CUSTNMBR
WHERE
CC.INACTIVE=0 AND CC.HOLD=0 and CC.CUSTCLAS not in ('DOUBTFULL','') and CC.SALSTERR<>'' and CC.SALSTERR<>'JEEMA'   order by R.DEX_ROW_ID
";

                InCubeQuery RouteQuery = new InCubeQuery(db_vms, SelectRoute);
                err = RouteQuery.Execute();

                ClearProgress();
                SetProgressMax(RouteQuery.GetDataTable().Rows.Count);

                InCubeQuery DeleteRouteCustomerQuery = new InCubeQuery(db_vms, "Delete From RouteCustomer");
                DeleteRouteCustomerQuery.ExecuteNonQuery();

                InCubeQuery DeleteCustomerOutletTerritoryQuery = new InCubeQuery(db_vms, "Delete From CustOutTerritory");
                err = DeleteCustomerOutletTerritoryQuery.ExecuteNonQuery();
                err = RouteQuery.FindFirst();
                tbl = RouteQuery.GetDataTable();
                while (err == InCubeErrors.Success)
                {
                    ReportProgress("Updating Routes");

                    RouteQuery.GetField("SLPRSNID", ref field);
                    string EmployeeCode = field.ToString().Trim();

                    RouteQuery.GetField("CUSTOMERID", ref field);
                    string CustomerID = field.ToString().Trim();
                    RouteQuery.GetField("ROUTEID", ref field);
                    string RouteID = field.ToString().Trim();
                    //RouteQuery.GetField("ADDRESSSCODE", ref field);
                    //string temp = field.ToString().Trim();
                    RouteQuery.GetField("CUSTNMBR", ref field);
                    field = field.ToString().Trim();
                    string OutletID = GetFieldValue("CustomerOutlet", "OutletID", " CustomerCode = '" + field.ToString().Trim() + "' AND CustomerID = " + CustomerID, db_vms);

                    if (OutletID == string.Empty)
                    {
                        err = RouteQuery.FindNext();
                        continue;
                    }

                    RouteQuery.GetField("CUSTNMBR", ref field);
                    string CustomerCode = field.ToString().Trim();

                    //RouteQuery.GetField("ADDRESS1", ref field);
                    RouteQuery.GetField("ROUTENAME", ref field);
                    string RouteName = field.ToString().Trim();

                    // RouteQuery.GetField("STATE", ref field);
                    string OrganizationID = "1";// GetFieldValue("OrganizationLanguage", "OrganizationID", "Description = 'Default Organization'", db_vms);

                    if (OrganizationID.Equals(string.Empty))
                    {
                        err = RouteQuery.FindNext();
                        continue;
                    }
                    if (int.Parse(RouteID) != 1)
                    {

                    }
                    if (EmployeeCode.Trim().Equals(string.Empty))
                    {

                    }
                    string EmployeeID = GetFieldValue("Employee", "EmployeeID", " EmployeeCode = '" + EmployeeCode + "'", db_vms);
                    if (EmployeeID.Trim().Equals(string.Empty))
                    {

                    }
                    string DivisionID = GetFieldValue("EmployeeDivision", "DivisionID", "EmployeeID = " + EmployeeID, db_vms);

                    if (DivisionID == string.Empty)
                    {
                        DivisionID = "1";
                    }

                    int LoopID = 0;

                    err = ExistObject("Route", "RouteID", "RouteID = " + RouteID, db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        TOTALINSERTED++;
                        LoopID++;
                        QueryBuilderObject.SetField("TerritoryID", RouteID);
                        QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                        //QueryBuilderObject.SetField("DivisionID", DivisionID);

                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                        err = QueryBuilderObject.InsertQueryString("Territory", db_vms);

                        DateTime EstimatedStart = DateTime.Parse(DateTime.Now.Date.AddHours(7).ToString());
                        DateTime EstimatedEnd = DateTime.Parse(DateTime.Now.Date.AddHours(23).ToString());

                        QueryBuilderObject.SetField("RouteID", RouteID);
                        QueryBuilderObject.SetField("Inactive", "0");
                        QueryBuilderObject.SetField("TerritoryID", RouteID);
                        QueryBuilderObject.SetField("EstimatedStart", "'" + EstimatedStart.ToString("dd/MMM/yyyy HH:mm tt") + "'");
                        QueryBuilderObject.SetField("EstimatedEnd", "'" + EstimatedEnd.ToString("dd/MMM/yyyy HH:mm tt") + "'");

                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                        err = QueryBuilderObject.InsertQueryString("Route", db_vms);
                    }

                    err = ExistObject("RouteVisitPattern", "RouteID", "RouteID = " + RouteID, db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        TOTALINSERTED++;
                        //LoopID++;

                        QueryBuilderObject.SetField("RouteID", RouteID);
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

                    err = ExistObject("Employee", "EmployeeID", "EmployeeID = " + EmployeeID, db_vms);
                    if (err == InCubeErrors.Success)
                    {
                        err = ExistObject("EmployeeTerritory", "EmployeeID", "EmployeeID = " + EmployeeID + " AND TerritoryID = " + RouteID, db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            TOTALINSERTED++;

                            QueryBuilderObject.SetField("EmployeeID", EmployeeID);
                            QueryBuilderObject.SetField("TerritoryID", RouteID);
                            QueryBuilderObject.InsertQueryString("EmployeeTerritory", db_vms);
                        }
                    }

                    err = ExistObject("RouteLanguage", "RouteID", "RouteID = " + RouteID + " AND LanguageID = 1", db_vms);
                    if (err != InCubeErrors.Success)
                    {

                        QueryBuilderObject.SetField("RouteID", RouteID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + RouteName + "'");
                        QueryBuilderObject.InsertQueryString("RouteLanguage", db_vms);

                        RouteName = RouteName + " Territory";
                        QueryBuilderObject.SetField("TerritoryID", RouteID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + RouteName + "'");
                        QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);
                    }

                    //err = ExistObject("RouteCustomer", "RouteID", "RouteID = " + RouteID + " AND CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                    err = ExistObject("RouteCustomer", "RouteID", "RouteID = " + RouteID + " AND CustomerID = " + CustomerID, db_vms);

                    if (err != InCubeErrors.Success)
                    {
                        TOTALINSERTED++;
                        QueryBuilderObject.SetField("RouteID", RouteID);
                        QueryBuilderObject.SetField("CustomerID", CustomerID);
                        QueryBuilderObject.SetField("OutletID", OutletID);
                        err = QueryBuilderObject.InsertQueryString("RouteCustomer", db_vms);
                    }

                    err = ExistObject("CustOutTerritory", "TerritoryID", "TerritoryID = " + RouteID + " AND CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        TOTALINSERTED++;
                        QueryBuilderObject.SetField("CustomerID", CustomerID);
                        QueryBuilderObject.SetField("OutletID", OutletID);
                        QueryBuilderObject.SetField("TerritoryID", RouteID);
                        QueryBuilderObject.InsertQueryString("CustOutTerritory", db_vms);
                    }

                    string AccountID = GetFieldValue("AccountCustOut", "AccountID", " CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                    if (!AccountID.Equals(string.Empty))
                    {
                        QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                        QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + AccountID, db_vms);
                    }

                    err = RouteQuery.FindNext();
                }

                RouteQuery.Close();
                WriteMessage("\r\n");
                WriteMessage("<<< ROUTE >>> Total Inserted = " + TOTALINSERTED);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        #endregion

        #region UpdateStock

        //public override void UpdateStock(bool UpdateAll, string WarehouseID, bool CaseChecked, DateTime StockDate)
        //{

        //    if (!db_vms.IsOpened())
        //    {
        //        WriteMessage("\r\n");
        //        WriteMessage("Cannot connect to GP database , please check the connection");
        //        return;
        //    }
        //    string WHCode = string.Empty;
        //    if (!UpdateAll)
        //    {
        //        WHCode = GetFieldValue("WAREHOUSE", "BARCODE", "WarehouseID=" + WarehouseID, db_vms);
        //        UpdateStockForWarehouse(WarehouseID, CaseChecked, StockDate, WHCode);
        //    }
        //    else
        //    {

        //        InCubeErrors err;
        //        object field = new object();
        //        InCubeQuery WarehouseQuery = new InCubeQuery(db_vms, "SELECT Vehicle.VehicleID,Warehouse.Barcode FROM Warehouse INNER JOIN Vehicle ON Warehouse.WarehouseID = Vehicle.VehicleID");
        //        err = WarehouseQuery.Execute();

        //        err = WarehouseQuery.FindFirst();
        //        while (err == InCubeErrors.Success)
        //        {
        //            WarehouseQuery.GetField(0, ref field);

        //            WarehouseID = field.ToString();
        //            WarehouseQuery.GetField(1, ref field);
        //            WHCode = field.ToString();

        //            UpdateStockForWarehouse(WarehouseID, CaseChecked, StockDate, WHCode);

        //            err = WarehouseQuery.FindNext();
        //        }
        //    }
        //}

        //private void UpdateStockForWarehouse(string WarehouseID, bool CaseChecked, DateTime StockDate, string WarehouseCode)
        //{
        //    //  int count = 0;
        //    int TOTALUPDATED = 0;
        //    string WHCODE = string.Empty;
        //    try
        //    {
        //        InCubeErrors GPerror = InCubeErrors.Error;

        //        List<string> DownloadedVehicles = new List<string>();
        //        object field = new object();
        //        #region Update Stock

        //        field = new object();
        //        string CheckUploaded = string.Format("select top(1)uploaded,deviceserial from RouteHistory where vehicleid=" + WarehouseID + " ORDER BY RouteHistoryID Desc ");
        //        qry = new InCubeQuery(CheckUploaded, db_vms);
        //        err = qry.Execute();
        //        err = qry.FindFirst();
        //        err = qry.GetField("uploaded", ref field);
        //        string uploaded = field.ToString().Trim();
        //        err = qry.GetField("deviceserial", ref field);
        //        string deviceserial = field.ToString().Trim();
        //        //if (uploaded.ToString().Trim().Equals(string.Empty) || uploaded.ToString().Trim().Equals("System.Object")) continue;
        //        if (!uploaded.ToString().Trim().Equals(string.Empty) && !uploaded.ToString().Trim().Equals("System.Object"))
        //        {
        //            if (Convert.ToBoolean(uploaded.ToString().Trim()))
        //            {
        //                WriteMessage("\r\n");
        //                WriteMessage("<<< The Route " + WarehouseCode + " is not downloaded . No stock will be added .>>> Total Updated = " + TOTALUPDATED);
        //                return;
        //            }

        //        }



        //        qry = new InCubeQuery("DELETE FROM WAREHOUSESTOCK WHERE WarehouseID=" + WarehouseID, db_vms);
        //        err = qry.ExecuteNonQuery();
        //        DataTable DTBL = new DataTable();
        //        //OleDbDataAdapter adapter = new OleDbDataAdapter(@"SELECT * from v_OS_LOCN_CURR_STK where LCS_STK_QTY_BU>0 and  LCS_LOCN_CODE ='PD01' and LCS_ITEM_CODE='915S MJ SW4-DP'", Conn);
        //        OleDbDataAdapter adapter = new OleDbDataAdapter(@"SELECT * from v_OS_LOCN_CURR_STK where LCS_STK_QTY_BU>0 and  LCS_LOCN_CODE ='" + WarehouseCode + "'", Conn);
        //        adapter.Fill(DTBL);

        //        ClearProgress();
        //        SetProgressMax(DTBL.Rows.Count;
        //        WriteExceptions("the number of items are " + DTBL.Rows.Count.ToString() + "", "Number of Items", false);

        //        foreach (DataRow row in DTBL.Rows)
        //        {

        //            ReportProgress(++;
        //            IntegrationForm.lblProgress.Text = "Updating Stock" + " " + ReportProgress( + " / " + IntegrationForm.progressBar1.Maximum;
        //            ();

        //            string vehicleCode = row["LCS_LOCN_CODE"].ToString().Trim();
        //            WHCODE = vehicleCode;
        //            string ItemCode = row["LCS_ITEM_CODE"].ToString().Trim();
        //            string PackTypeCode = row["ITEM_UOM_CODE"].ToString().Trim();


        //            string Quantity = row["LCS_STK_QTY_BU"].ToString().Trim();
        //            string Batch = "1990/01/01";// row["BatchNo"].ToString().Trim();
        //            string DivisionCode = "1";// row["CompanyID"].ToString().Trim();
        //            string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode=" + DivisionCode + "", db_vms).Trim();
        //            if (DivisionID.Equals(string.Empty))
        //                continue;

        //            string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + PackTypeCode + "'", db_vms).Trim();
        //            if (PackTypeID.Equals(string.Empty))
        //                continue;

        //            string expiry = DateTime.Parse("01/01/1990 00:00:00").ToString(DateFormat);
        //            string ItemID = GetFieldValue("Item", "ItemID", "ItemCode='" + ItemCode + "' ", db_vms).Trim();
        //            if (ItemID.Equals(string.Empty))
        //                continue;
        //            string PackID = GetFieldValue("Pack", "PackID", "ItemID=" + ItemID + " and packTypeID=" + PackTypeID + "", db_vms).Trim();
        //            if (PackID.Equals(string.Empty))
        //                continue;
        //            string vehicleID = GetFieldValue("warehouse", "WarehouseID", "Barcode='" + vehicleCode + "'", db_vms).Trim();
        //            if (vehicleID.Equals(string.Empty))
        //                continue;

        //            string UOMdesc = string.Empty;// row[2].ToString().Trim();
        //            string Expirydate = DateTime.Parse("01/01/1990 00:00:00").ToString(DateFormat);

        //            if (Batch == string.Empty)
        //            {
        //                Batch = "1990/01/01";
        //            }

        //            WriteExceptions("Route " + vehicleCode + " is downloaded ...", "Device is downloaded", false);

        //            //string updateReady = string.Format("update readyDevice set Ready=0,ReadyDate='{1}' where Routecode='{0}'", vehicleCode, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
        //            //qry = new InCubeQuery(updateReady, db_vms);
        //            //err = qry.ExecuteNonQuery();

        //            GPerror = InCubeErrors.Error;
        //            TOTALUPDATED++;
        //            DownloadedVehicles.Add(vehicleCode);
        //            string query = "Select PackID from Pack where ItemID = " + ItemID;
        //            InCubeQuery CMD = new InCubeQuery(query, db_vms);
        //            CMD.Execute();
        //            err = CMD.FindFirst();

        //            WriteExceptions("proceeding with stock insertion", "Starting Stock update", false);
        //            while (err == InCubeErrors.Success)
        //            {
        //                CMD.GetField(0, ref field);
        //                string _packid = field.ToString();
        //                string _quantity = "0";
        //                string logQty = string.Empty;

        //                string existStock = GetFieldValue("WarehouseStock", "PackID", "WarehouseID = " + vehicleID + " AND ZoneID = 1 AND PackID = " + _packid + " AND BatchNo = '" + Batch + "'", db_vms).Trim();
        //                if (existStock.Equals(string.Empty))
        //                {
        //                    QueryBuilderObject.SetField("WarehouseID", vehicleID);
        //                    QueryBuilderObject.SetField("ZoneID", "1");
        //                    QueryBuilderObject.SetField("PackID", _packid);
        //                    QueryBuilderObject.SetField("ExpiryDate", "'" + Expirydate + "'");
        //                    QueryBuilderObject.SetField("BatchNo", "'" + Batch + "'");
        //                    QueryBuilderObject.SetField("SampleQuantity", "0");

        //                    if (_packid == PackID)
        //                    {
        //                        QueryBuilderObject.SetField("Quantity", Quantity);
        //                        QueryBuilderObject.SetField("BaseQuantity", Quantity);
        //                        logQty = Quantity;
        //                    }
        //                    else
        //                    {
        //                        QueryBuilderObject.SetField("Quantity", _quantity);
        //                        QueryBuilderObject.SetField("BaseQuantity", _quantity);
        //                        logQty = _quantity;
        //                    }

        //                    err = QueryBuilderObject.InsertQueryString("WarehouseStock", db_vms);
        //                    if (err != InCubeErrors.Success)
        //                    {

        //                    }

        //                }
        //                else if (_packid == PackID)
        //                {

        //                    string beforeQty = GetFieldValue("WarehouseStock", "Quantity", "WarehouseID = " + vehicleID + " AND ZoneID = 1 AND PackID = " + _packid + " AND BatchNo = '" + Batch + "'", db_vms).Trim();
        //                    if (beforeQty.Equals(string.Empty)) beforeQty = "0";
        //                    WriteExceptions("OLD STOCK QUANTITY BEFORE UPDATE  = " + beforeQty + " ,THE ADDED QUANTITY = " + Quantity + " , THE TOTAL = " + (decimal.Parse(beforeQty) + decimal.Parse(Quantity)) + "  transaction =  ---- pack id is " + _packid + "", "UPDATE Stock ", false);
        //                    QueryBuilderObject.SetField("Quantity", "Quantity+" + Quantity);
        //                    QueryBuilderObject.SetField("BaseQuantity", "BaseQuantity+" + Quantity);
        //                    err = QueryBuilderObject.UpdateQueryString("WarehouseStock", "WarehouseID = " + vehicleID + " AND ZoneID = 1 AND PackID = " + PackID + " AND BatchNo = '" + Batch + "'", db_vms);
        //                    if (err == InCubeErrors.Success)
        //                    {
        //                        WriteExceptions("stock updated correctly , transaction = ---- pack id is " + _packid + "------ quantity=" + Quantity + "", "UPDATE Stock ", false);
        //                        if (err == InCubeErrors.Success) { WriteExceptions("updating intermediate transaction success , transaction =", "UPDATE Stock ", false); }
        //                        else { WriteExceptions("updating intermediate transaction Failed transaction = ", "Inserting Stock ", false); }

        //                    }
        //                    else
        //                    {
        //                        WriteExceptions("stock updated Failed ****** , transaction =  ---- pack id is " + _packid + "------ quantity=" + Quantity + "", "Inserting Stock ", false);
        //                    }
        //                }

        //                err = CMD.FindNext();
        //            }
        //        }


        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //    WriteMessage("\r\n");
        //    WriteMessage("<<< STOCK Updated ," + WHCODE + ">>> Total Updated = " + TOTALUPDATED);

        //    //ClassStartOFDay.StartofDay(db_vms, WarehouseID);

        //        #endregion
        //}

        #endregion

        #region ENHANCED STOCK INTEGRATION
        private void UpdateStockForWarehouseEnhanced(DateTime stockDate)
        {
            try
            {

                int TOTALUPDATED = 0;
                List<string> DownloadedVehicles = new List<string>();
                object field = new object();

                List<string> cancelledTrx = new List<string>();
                DataTable DTBL = new DataTable();

                if (Conn.State == ConnectionState.Closed)
                {
                    Conn.Open();
                }
                //Old Before Batch
                //                OracleDataAdapter adapter = new OracleDataAdapter(@"SELECT (concat(concat('WT-',H.WRH_TXN_CODE),concat('-',LPAD(H.WRH_NO,6,'0'))) ) AS TransactionID,H.WRH_DT DateH,H.WRH_LOCN_CODE LocationTo,'AQ' AS LocationFrom,
                //I.WRAI_ITEM_CODE ItemCode,I.WRAI_UOM_CODE AS UOM,I.WRAI_QTY AS Qty1,I.WRAI_QTY_LS ,'1990/01/01' Batch,'1990-01-01' Expiry,'' SAP_REF_NUM ,'1' DivisionCode,'0' COMPANYCODE,'0' Status,'0' ConversionFactor
                //,I.WRAI_QTY_LS,I.WRAI_SYS_ID,I.WRAI_WRH_SYS_ID
                //FROM OW_REQ_APPR_ITEM_TXN I INNER JOIN OW_REQ_HEAD_TXN H ON H.WRH_SYS_ID=I.WRAI_WRH_SYS_ID where  NVL(H.WRH_PROC_FLAG,'N')='Y' ", Conn);

                //wrai_qty batch_qty,wrai_qty_ls batch_qty_ls batch,


                OracleDataAdapter adapter = new OracleDataAdapter(@"SELECT (CONCAT (CONCAT ('WT-', h.wrh_txn_code),
                CONCAT ('-', LPAD (h.wrh_no, 6, '0')))) AS transactionid,
       h.wrh_dt dateh, h.wrh_locn_code locationto, 'AQ' AS locationfrom,
       i.wrai_item_code itemcode, i.wrai_uom_code AS uom,
       DECODE ((SELECT item_batch_yn_num
                  FROM om_item
                 WHERE item_code = i.wrai_item_code),
               1, i.wrab_batch_qty,
               2, wrai_qty
              ) AS qty1,
       DECODE ((SELECT item_batch_yn_num
                  FROM om_item
                 WHERE item_code = i.wrai_item_code),
               1, i.wrab_batch_qty_ls,
               2, wrai_qty_ls
              ) AS wrai_qty_ls,
       wrab_batch_no batch, '1990-01-01' expiry, '' sap_ref_num,
       '1' divisioncode, '0' companycode, '0' status, '0' conversionfactor,
       i.wrab_sys_id, i.wrai_sys_id, i.wrai_wrh_sys_id, h.wrh_proc_flag
  FROM (SELECT wrai_sys_id, wrai_wrh_sys_id, wrai_item_code, wrai_uom_code,
               wrai_qty, wrai_qty_ls, wrai_ltoi_sys_id, wrai_cr_dt,
               wrai_cr_uid, wrai_qty_bu, wrab_batch_no, wrab_batch_qty,
               wrab_batch_qty_ls, wrab_sys_id
          FROM ow_req_appr_item_txn, ow_req_appr_batch_txn
         WHERE wrai_sys_id = wrab_wrai_sys_id(+)) i
       INNER JOIN
       ow_req_head_txn h ON h.wrh_sys_id = i.wrai_wrh_sys_id 
WHERE NVL (h.wrh_proc_flag, 'N') = 'Y'
", Conn);

                adapter.Fill(DTBL);
                ClearProgress();
                SetProgressMax(DTBL.Rows.Count);
                List<string> transactionsList = new List<string>();
                foreach (DataRow dr in DTBL.Rows)
                {
                    if (!dr["SAP_REF_NUM"].ToString().Trim().Equals(string.Empty))
                    {
                        if (!transactionsList.Contains(dr["SAP_REF_NUM"].ToString().Trim()))
                        {
                            transactionsList.Add(dr["SAP_REF_NUM"].ToString().Trim());
                        }
                    }
                    else
                    {
                        if (!transactionsList.Contains(dr["TransactionID"].ToString().Trim()))
                        {
                            transactionsList.Add(dr["TransactionID"].ToString().Trim());
                        }
                    }
                }
                WriteExceptions("the number of items are " + DTBL.Rows.Count.ToString() + "", "Number of Items", false);

                bool TransactionHeaderUpdated = false;
                //IntegrationForm.progressBar1.Value = 0;
                //IntegrationForm.progressBar1.Maximum = transactionsList.Count;
                foreach (string tranStr in transactionsList)
                {
                    TOTALUPDATED = 0;
                    WriteExceptions("ENTERING STOCK LOOP", "STOCK", false);
                    ReportProgress("Updating Stock");
                    if (GetFieldValue("WH_Sync", "isnull(StatusID,0)", "TransactionID='" + tranStr + "'", db_vms).Trim().Equals("2"))
                    {
                        //THIS TRANSACTION WAS INTEGRATED BEFORE.
                        string query = string.Format("update OW_REQ_HEAD_TXN set WRH_PROC_FLAG='X' where WRH_TXN_CODE='{0}' AND WRH_NO='{1}'", tranStr.Split('-')[1].ToString(), tranStr.Split('-')[2].ToString().TrimStart(new Char[] { '0' }));
                        OracleCommand cmdHDR = new OracleCommand(query, Conn);
                        err = ExecuteNonQuery(cmdHDR, tranStr, "STOCK");
                        cmdHDR.Dispose();
                        WriteExceptions("UPDATE ORION FLAG", "STOCK", false);
                        continue;
                    }
                    TransactionHeaderUpdated = false;
                    #region Combine Procedure
                    /*
                     * 
                     * CREATE Type WHT_Details as Table
(
	[WarehouseID] [int] NOT NULL,
	[TransactionID] [nvarchar](200) NOT NULL,
	[ZoneID] [int] NOT NULL,
	[PackID] [int] NOT NULL,
	[ExpiryDate] [datetime] NOT NULL,
	[Quantity] [numeric](19, 9) NOT NULL,
	[Balanced] [bit] NOT NULL,
	[BatchNo] [nvarchar](200) NOT NULL,
	[ProductionDate] [datetime] NULL,
	[PackStatusID] [smallint] NOT NULL,
	[DivisionID] [int] NOT NULL DEFAULT ((-1)),
	[Downloaded] [bit] NULL DEFAULT ((0)),
	[ApprovedQuantity] [numeric](19, 9) NULL,
	[RequestedQuantity] [numeric](19, 9) NULL
)

                     * 
                     * 
                     * 
                     * 
                     * 
                     * CREATE TABLE [dbo].[WHTransDetailHistory](
	[WarehouseID] [int] NOT NULL,
	[TransactionID] [nvarchar](200) NOT NULL,
	[ZoneID] [int] NOT NULL,
	[PackID] [int] NOT NULL,
	[ExpiryDate] [datetime] NOT NULL,
	[Quantity] [numeric](19, 9) NOT NULL,
	[Balanced] [bit] NOT NULL,
	[BatchNo] [nvarchar](200) NOT NULL,
	[ProductionDate] [datetime] NULL,
	[PackStatusID] [smallint] NOT NULL,
	[DivisionID] [int] NOT NULL DEFAULT ((-1)),
	[Downloaded] [bit] NULL DEFAULT ((0)),
	[ApprovedQuantity] [numeric](19, 9) NULL,
	[RequestedQuantity] [numeric](19, 9) NULL
	
) ON [PRIMARY]
                     * 
                     * 
                     * 
                     * 
                     * 
                     * 
                     * 
                     * CREATE PROCEDURE CombineWHT_Details 
	@TransactionID nvarchar(100)
AS
BEGIN
	SET NOCOUNT ON;
	insert into WHTransDetailHistory select * from Whtransdetail where TransactionID=@TransactionID
Declare @Details WHT_Details;
insert into @Details
SELECT [WarehouseID]
      ,[TransactionID]
      ,[ZoneID]
      ,[PackID]
      ,'2018-01-01'
      ,Sum(isnull(Quantity,0))
      ,[Balanced]
      ,'1990/01/01'
      ,[ProductionDate]
      ,[PackStatusID]
      ,[DivisionID]
      ,0
      ,Sum(isnull([ApprovedQuantity],0))
      ,Sum(isnull([RequestedQuantity],0))
    
  FROM [dbo].[WhTransDetail]
  where transactionid=@TransactionID
  group by 
[WarehouseID]
      ,[TransactionID]
      ,[ZoneID]
      ,[PackID]
      ,[Balanced]
      ,[ProductionDate]
      ,[PackStatusID]
      ,[DivisionID]
     
delete from Whtransdetail where transactionid=@TransactionID
insert into WhTransDetail select * from @Details
END
GO
                     * 
                     * 
                     * 
                     * 
                     * CREATE TABLE [dbo].[WH_Sync](
	[TRANSACTIONID] [nvarchar](50) NOT NULL,
	[StatusID] [int] NOT NULL,
	[StatusDate] [datetime] NULL,
 CONSTRAINT [PK_WH_Sync] PRIMARY KEY CLUSTERED 
(
	[TRANSACTIONID] ASC,
	[StatusID] ASC
)
                    */
                    #endregion
                    qry = new InCubeQuery("CombineWHT_Details", db_vms);
                    qry.AddParameter("@TransactionID", tranStr);
                    err = qry.ExecuteStoredProcedure();
                    if (err != InCubeErrors.Success) continue;
                    InCubeTransaction tran = new InCubeTransaction();
                    tran.BeginTransaction(db_vms);
                    try
                    {
                        //IntegrationForm.progressBar1.Value++;
                        //IntegrationForm.txtMessages.AppendText("\r\n");
                        //IntegrationForm.txtMessages.AppendText("Transaction <<" + tranStr + ">> Update Started..");

                        #region Warehouse Transaction
                        Dictionary<string, string> transactionList = new Dictionary<string, string>();
                        string previousTransaction = string.Empty;
                        foreach (DataRow row in DTBL.Select("TransactionID='" + tranStr + "' or SAP_REF_NUM='" + tranStr + "'"))
                        {
                            string COMPANYCODE = row["COMPANYCODE"].ToString().Trim();
                            string TransactionID = row["TransactionID"].ToString().Trim();
                            string SAP_REF_NUM = row["SAP_REF_NUM"].ToString().Trim();
                            string LocationFrom = row["LocationFrom"].ToString().Trim();
                            string TransactionType = "0"; //GetFieldValue("Warehouse", "WarehouseTypeID", "WarehouseCode='" + LocationFrom + "'", db_vms).Trim();

                            string LocationTo = row["LocationTo"].ToString().Trim();
                            //if (LocationTo.Equals("1131V253")) continue;

                            string DivisionCode = row["DivisionCode"].ToString().Trim();
                            string Date = row["DateH"].ToString().Trim();
                            string Status = row["Status"].ToString().Trim();
                            string ItemCode = row["ItemCode"].ToString().Trim();
                            string UOM = row["UOM"].ToString().Trim();
                            string ConversionFactor = row["ConversionFactor"].ToString().Trim();
                            string Qty1 = row["Qty1"].ToString().Trim();
                            string pieceQuantity = row["WRAI_QTY_LS"].ToString().Trim();

                            string Batch = row["Batch"].ToString().Trim();
                            string Expiry = row["Expiry"].ToString().Trim();

                            if (TransactionID.Equals(string.Empty) && SAP_REF_NUM.Equals(string.Empty)) continue;
                            string OrganizationID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + COMPANYCODE + "'", db_vms, tran).Trim();
                            if (!SAP_REF_NUM.Trim().Equals(string.Empty))
                                TransactionID = SAP_REF_NUM;
                            WriteExceptions("ENTERING STOCK LOOP", "STOCK", false);
                            OrganizationID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + COMPANYCODE + "'", db_vms, tran).Trim();
                            if (cancelledTrx.Contains(TransactionID))
                            {
                                WriteExceptions("TRANSACTION CANCELLED, CONTINUE", "STOCK", false);
                                continue;
                            }

                            string TransactionDate = Date;
                            WriteExceptions("TRANSACTIONID IS " + TransactionID + "", "STOCK", false);

                            //vehicleCode = "1131V152"; WarehouseCode = "1102"; LocationTo = "1131V152"; LocationFrom = "1102";

                            string PackType = UOM;// row["UOM"].ToString().Trim();
                            string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + PackType + "'", db_vms, tran).Trim();
                            string PackTypeID2 = "";
                            string Quantity = Qty1;// row["Qty1"].ToString().Trim();
                            string StkQty = Quantity;
                            if (Batch.Equals(string.Empty)) Batch = "1990/01/01";
                            string originalbatch = string.Empty;
                            string ERP_TRAN_TO_UPDATE = string.Empty;
                            WriteExceptions("FILLING PARAMETERS", "Inserting Stock ", true);
                            if (cancelledTrx.Contains(TransactionID))
                            {
                                WriteExceptions("Line skipped because transaction is cancelled", "Inserting Stock ", true);
                                continue;
                            }
                            if (TransactionType.Equals("0")) { TransactionType = "1"; } else if (TransactionType.Equals("1")) { TransactionType = "2"; StkQty = "-" + StkQty; }// row["TransactionType"].ToString().Trim();
                            string Tran_To_Update = TransactionID;
                            originalbatch = Batch;
                            if (!originalbatch.Equals(string.Empty)) originalbatch = "and LotNumber ='" + Batch + "'";
                            if (Batch.Equals(string.Empty)) Batch = "1990/01/01";
                            string expiry = row["Expiry"].ToString().Trim();
                            if (expiry.Equals(string.Empty) || expiry.Contains("0000"))
                                expiry = "1990-01-01";
                            expiry = DateTime.Parse(expiry).ToString("yyyy-MM-dd");

                            string vehicleID = string.Empty;
                            string WarehouseID = string.Empty;
                            string DivisionID = string.Empty;
                            //DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode='" + DivisionCode + "'", db_vms, tran).Trim();
                            //DivisionID = GetFieldValue("warehousetransaction", "DivisionID", "TransactionID='" + TransactionID + "'", db_vms).Trim();
                            string vehicleCode = string.Empty;
                            string WarehouseCode = string.Empty;
                            if (TransactionType.Equals("2"))
                            {
                                vehicleCode = row["LocationFrom"].ToString().Trim();
                                WarehouseCode = row["LocationTo"].ToString().Trim();
                                vehicleID = GetFieldValue("warehouse", "WarehouseID", "WarehouseCode='" + vehicleCode + "'", db_vms, tran).Trim();
                                WarehouseID = GetFieldValue("warehouse", "WarehouseID", "Barcode='" + WarehouseCode + "'", db_vms, tran).Trim();
                                //get divisionID for each warehousetransaction
                                DivisionID = GetFieldValue("WarehouseTransaction", "DivisionID", "TransactionID='" + TransactionID + "'", db_vms, tran).Trim();
                            }
                            else
                            {
                                vehicleCode = row["LocationTo"].ToString().Trim();
                                WarehouseCode = row["LocationFrom"].ToString().Trim();
                                vehicleID = GetFieldValue("warehouse", "WarehouseID", "WarehouseCode='" + vehicleCode + "'", db_vms, tran).Trim();
                                WarehouseID = GetFieldValue("warehouse", "WarehouseID", "Barcode='" + WarehouseCode + "'", db_vms, tran).Trim();
                                DivisionID = GetFieldValue("WarehouseTransaction", "DivisionID", "TransactionID='" + TransactionID + "'", db_vms, tran).Trim();
                            }
                            if (vehicleID.Equals(string.Empty))
                            {
                                WriteExceptions("Line skipped because vehicleID is empty, vehicleCode = " + vehicleCode, "Inserting Stock ", true);
                                continue;
                            }
                            if (WarehouseID.Equals(string.Empty))
                            {
                                WriteExceptions("Line skipped because WarehouseID is empty, WarehouseCode = " + WarehouseCode, "Inserting Stock ", true);
                                continue;
                            }
                            if (DivisionID.Equals(string.Empty))
                            {
                                WriteExceptions("Line skipped because DivisionID is empty, DivisionCode = " + DivisionCode, "Inserting Stock ", true);
                                continue;
                            }

                            string UploadStatus = GetFieldValue("RouteHistory", "Top(1) Uploaded", " VehicleID=" + vehicleID + " order by RouteHistoryID Desc", db_vms, tran).Trim();
                            if (UploadStatus.Equals(string.Empty)) UploadStatus = "0";
                            if (UploadStatus.ToLower().Equals("false")) { UploadStatus = "0"; }
                            else if (UploadStatus.ToLower().Equals("true"))
                            {
                                WriteExceptions("Line skipped because device is uploaded, VehicleID = " + vehicleID, "Inserting Stock ", true);
                                continue;
                            }

                            string ItemID = GetFieldValue("Item", "ItemID", "ItemCode='" + ItemCode.Trim() + "' and ItemCategoryID in (select ItemCategoryID from ItemCategory where DivisionID in (select DivisionID from Division where DivisionID=" + DivisionID + " and OrganizationID=" + OrganizationID + ")) ", db_vms, tran).Trim();
                            if (ItemID.Equals(string.Empty))
                            {

                                ItemID = GetFieldValue("Item", "ItemID", "ItemCode='" + ItemCode.Trim() + "' and ItemCategoryID in (select ItemCategoryID from ItemCategory where DivisionID in (select DivisionID from Division where DivisionID=" + DivisionID + " and OrganizationID=" + OrganizationID + ")) ", db_vms, tran).Trim();
                                if (ItemID.Equals(string.Empty))
                                {
                                    WriteExceptions("Line skipped because ItemID is empty, ItemCode = " + ItemCode, "Inserting Stock ", true);
                                    continue;
                                }
                            }
                            string PackID2 = "";
                            PackTypeID2 = "";
                            if (!pieceQuantity.Equals("0"))
                            {
                                string GetBaseUOM = GetFieldValue("PackTypeLanguage PTL Inner Join Pack P on PTL.PackTypeID=P.PackTypeID inner join Item I on P.ItemID=I.ItemID", "PTL.Description", "PTL.LanguageID=1 and I.ItemID=" + ItemID + " and P.Quantity=1", db_vms, tran).Trim();
                                // Qty1 = pieceQuantity;
                                UOM = GetBaseUOM;
                                PackTypeID2 = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + UOM + "'", db_vms, tran).Trim();
                                PackID2 = GetFieldValue("Pack", "PackID", "ItemID=" + ItemID + " and packTypeID=" + PackTypeID2 + "", db_vms, tran).Trim();

                            }
                            string PackID = GetFieldValue("Pack", "PackID", "ItemID=" + ItemID + " and packTypeID=" + PackTypeID + "", db_vms, tran).Trim();
                            if (PackID.Equals(string.Empty))
                            {
                                WriteExceptions("Line skipped because PackID is empty, ItemID = " + ItemID + ", PackTypeID = " + PackTypeID, "Inserting Stock ", true);
                                continue;
                            }
                            string EmployeeID = GetFieldValue("EmployeeVehicle", "EmployeeID", "VehicleID=" + vehicleID + "", db_vms, tran).Trim();
                            if (EmployeeID.Equals(string.Empty))
                            {
                                WriteExceptions("Line skipped because EmployeeID is empty, vehicleID = " + vehicleID, "Inserting Stock ", true);
                                continue;
                            }
                            string ref_no = SAP_REF_NUM;
                            WriteExceptions("PARAMETERS ARE FILLED, REF NO IS " + ref_no + "", "Inserting Stock ", true);
                            WriteExceptions("CHECKING IF VEHICLE ID " + vehicleID + "  IS DOWNLOADED", "Inserting Stock ", true);

                            WriteExceptions("CHECKING THE VEHICLES LIST <<LIST COUNT=" + DownloadedVehicles.Count + ">>", "Inserting STOCK ", false);
                            if (PackID.Equals("14100"))
                            {

                            }
                            if (!TransactionHeaderUpdated)
                            {
                                QueryBuilderObject.SetField("Balanced", "1");
                                err = QueryBuilderObject.UpdateQueryString("WhTransDetail", "TransactionID='" + TransactionID + "'", db_vms, tran);

                                WriteExceptions("CHECKING IF THE TRANSACTION " + TransactionID + " EXIST IN WAREHOUSE TRANSACTIONS", "Inserting Stock ", true);
                                string existTransaction = GetFieldValue("WarehouseTransaction", "TransactionID", "TransactionID='" + TransactionID + "'", db_vms, tran);
                                if (existTransaction.Trim().Equals(string.Empty))
                                {
                                    WriteExceptions("TRANSACTION DOES NOT EXIST... INSERTING", "Inserting Stock ", true);
                                    WriteExceptions(" Transaction does not exist Inserting transaction = " + TransactionID + "", "Inserting WH Transaction ", false);
                                    QueryBuilderObject.SetField("WarehouseID", vehicleID);
                                    QueryBuilderObject.SetField("TransactionID", "'" + TransactionID + "'");
                                    QueryBuilderObject.SetField("TransactionTypeID", TransactionType);
                                    QueryBuilderObject.SetField("TransactionDate", "'" + DateTime.Parse(TransactionDate).ToString("yyyy-MM-dd HH:mm:ss") + "'");
                                    QueryBuilderObject.SetField("RequestedBy", EmployeeID);
                                    QueryBuilderObject.SetField("ImplementedBy", UserID.ToString());
                                    QueryBuilderObject.SetField("Synchronized", "0");
                                    QueryBuilderObject.SetField("ProductionDate", "'" + DateTime.Today.ToString("yyyy-MM-dd") + "'");
                                    QueryBuilderObject.SetField("ExecutionDate", "'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'");
                                    QueryBuilderObject.SetField("RefWarehouseID", WarehouseID);
                                    QueryBuilderObject.SetField("Posted", "1");
                                    QueryBuilderObject.SetField("Downloaded", "1");
                                    if (!Convert.ToBoolean(Convert.ToInt32(UploadStatus)) && TransactionType.Trim().Equals("1"))
                                    {
                                        QueryBuilderObject.SetField("WarehouseTransactionStatusID", "4");
                                    }
                                    else
                                    {
                                        QueryBuilderObject.SetField("WarehouseTransactionStatusID", "8");
                                    }
                                    QueryBuilderObject.SetField("CreationSourceID", "1");
                                    QueryBuilderObject.SetField("TransactionOperationID", "1");
                                    QueryBuilderObject.SetField("DivisionID", DivisionID);
                                    QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                                    err = QueryBuilderObject.InsertQueryString("WarehouseTransaction", db_vms, tran);

                                    if (err == InCubeErrors.Success)
                                    {
                                        WriteExceptions("TRANSACTION " + TransactionID + " INSERTED SUCCESSFULLY", "Inserting Stock ", true);
                                        WriteExceptions("Inserting transaction = " + TransactionID + " succeeded !", "Inserting WH Transaction ", false);
                                        if (err == InCubeErrors.Success)
                                        {
                                            string update = string.Format("INSERT INTO WH_Sync (TRANSACTIONID,StatusID,StatusDate) VALUES('{0}',2,GetDate())", TransactionID); //string.Format("update [WarehouseTransaction] set Synchronized=1 where TransactionID='{0}'", InvoiceNumberComplete);
                                            InCubeQuery UpdateQuery = new InCubeQuery(update, db_vms);
                                            err = UpdateQuery.ExecuteNoneQuery(tran);
                                        }
                                    }
                                    else
                                    {
                                        WriteExceptions("FAILED TO INSERT TRANSACTION " + TransactionID + "", "Inserting Stock ", true);
                                        //IntegrationForm.txtMessages.AppendText("\r\n");
                                        //IntegrationForm.txtMessages.AppendText("Transaction <<" + TransactionID + ">> Failed.");
                                        WriteExceptions("Inserting transaction = " + TransactionID + " Failed *******", "Inserting WH Transaction ", false);
                                        throw new Exception("Error");
                                    }
                                }
                                else
                                {
                                    WriteExceptions("already existing transaction = " + TransactionID + "", "Inserting WH Transaction ", false);
                                    if (!Convert.ToBoolean(Convert.ToInt32(UploadStatus)) && TransactionType.Trim().Equals("1"))
                                    {
                                        QueryBuilderObject.SetField("WarehouseTransactionStatusID", "4");
                                        //  QueryBuilderObject.SetField("Posted", "1");
                                    }
                                    else
                                    {
                                        if (TransactionType.Equals("2")) { QueryBuilderObject.SetField("WarehouseTransactionStatusID", "4"); }
                                        else { QueryBuilderObject.SetField("WarehouseTransactionStatusID", "8"); }
                                    }
                                    QueryBuilderObject.SetField("Posted", "1");
                                    QueryBuilderObject.SetField("Synchronized", "0");
                                    QueryBuilderObject.SetField("ExecutionDate", "'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'");
                                    err = QueryBuilderObject.UpdateQueryString("warehouseTransaction", "TransactionID='" + TransactionID + "' and WarehouseTransactionStatusID<>5", db_vms, tran);
                                    if (err == InCubeErrors.Success || QueryBuilderObject.CurrentException == null)
                                    {
                                        WriteExceptions("Updating transaction = " + TransactionID + " succeed", "Inserting WH Transaction ", false);
                                        string update = string.Format("INSERT INTO WH_Sync (TRANSACTIONID,StatusID,StatusDate) VALUES('{0}',2,GetDate())", TransactionID); //string.Format("update [WarehouseTransaction] set Synchronized=1 where TransactionID='{0}'", InvoiceNumberComplete);
                                        InCubeQuery UpdateQuery = new InCubeQuery(update, db_vms);
                                        err = UpdateQuery.ExecuteNoneQuery(tran);
                                    }
                                    else
                                    {
                                        WriteExceptions("Updating transaction = " + TransactionID + " Failed ******", "Inserting WH Transaction ", false);
                                    }
                                    if (err != InCubeErrors.Success) { throw new Exception("Error"); }
                                    string orderID = GetFieldValue("SalesOrder", "OrderID", "WarehouseTransactionID='" + TransactionID + "'", db_vms, tran).Trim();
                                    if (!orderID.Equals(string.Empty))
                                    {
                                        QueryBuilderObject.SetField("OrderStatusID", "5");
                                        err = QueryBuilderObject.UpdateQueryString("SalesOrder", "WarehouseTransactionID='" + TransactionID + "' and OrderStatusID<>5", db_vms, tran);
                                    }
                                }
                                TransactionHeaderUpdated = true;
                            }
                            string existDetail = GetFieldValue("WhTransDetail", "TransactionID", "TransactionID='" + TransactionID + "' and WarehouseID=" + vehicleID + " and PackID=" + PackID, db_vms, tran).Trim();
                            //string existDetail = GetFieldValue("WhTransDetail", "TransactionID", "TransactionID='" + TransactionID + "' and WarehouseID=" + vehicleID + " and PackID=" + PackID + " and Balanced=0", db_vms).Trim();
                            if (existDetail.Equals(string.Empty))
                            {
                                WriteExceptions("inserting details transaction = " + TransactionID + " --- pack id = " + PackID + " ", "Inserting WH Transaction ", false);
                                QueryBuilderObject.SetField("WarehouseID", vehicleID);
                                QueryBuilderObject.SetField("TransactionID", "'" + TransactionID + "'");
                                QueryBuilderObject.SetField("ZoneID", "1");
                                QueryBuilderObject.SetField("PackID", PackID);
                                QueryBuilderObject.SetField("ExpiryDate", "'" + expiry + "'");
                                QueryBuilderObject.SetField("Quantity", Quantity);
                                QueryBuilderObject.SetField("Balanced", "0");
                                QueryBuilderObject.SetField("BatchNo", "'" + Batch + "'");
                                QueryBuilderObject.SetField("PackStatusID", "0");
                                QueryBuilderObject.SetField("DivisionID", DivisionID);
                                QueryBuilderObject.SetField("ApprovedQuantity", Quantity);
                                QueryBuilderObject.SetField("RequestedQuantity", "0");

                                err = QueryBuilderObject.InsertQueryString("WhTransDetail", db_vms, tran);
                                if (err != InCubeErrors.Success) { throw new Exception("Error"); }


                            }
                            else
                            {
                                //string requestedQT = GetFieldValue("WhTransDetail", "isnull(RequestedQuantity,0)", "TransactionID='" + TransactionID + "' and WarehouseID=" + vehicleID + " and PackID=" + PackID + " and BatchNo='" + Batch + "' ", db_vms, tran).Trim();
                                WriteExceptions("updating  details transaction = " + TransactionID + " --- pack id = " + PackID + " ", "Inserting WH Transaction ", false);
                                QueryBuilderObject.SetField("Quantity", Quantity);
                                //QueryBuilderObject.SetField("BatchNo", "'" + Batch + "'");//+ "UPDATED" 
                                // QueryBuilderObject.SetField("RequestedQuantity", requestedQT);
                                QueryBuilderObject.SetField("ApprovedQuantity", Quantity);
                                QueryBuilderObject.SetField("Balanced", "0");
                                QueryBuilderObject.SetField("ExpiryDate", "'" + expiry + "'");
                                QueryBuilderObject.SetField("BatchNo", "'" + Batch + "'");
                                err = QueryBuilderObject.UpdateQueryString("WhTransDetail", "TransactionID='" + TransactionID + "' and WarehouseID=" + vehicleID + " and PackID=" + PackID + " AND ZONEID=1", db_vms, tran);
                                if (err != InCubeErrors.Success) { throw new Exception("Error"); }

                            }
                            if (TransactionType.Equals("1"))
                            {
                                err = UpdateVehicleStock(ref tran, ItemID, vehicleID, expiry, Batch, PackID, StkQty);
                            }
                            else if (TransactionType.Equals("2"))
                            {
                                err = UpdateVehicleStock(ref tran, ItemID, vehicleID, expiry, Batch, PackID, "-" + StkQty);
                            }
                            if (err != InCubeErrors.Success) { throw new Exception("Error"); }

                            // if there is PEC from Item
                            //********************************************************************************
                            if (PackID2.Trim() != "")
                            {
                                existDetail = GetFieldValue("WhTransDetail", "TransactionID", "TransactionID='" + TransactionID + "' and WarehouseID=" + vehicleID + " and PackID=" + PackID2, db_vms, tran).Trim();
                                //string existDetail = GetFieldValue("WhTransDetail", "TransactionID", "TransactionID='" + TransactionID + "' and WarehouseID=" + vehicleID + " and PackID=" + PackID + " and Balanced=0", db_vms).Trim();
                                if (existDetail.Equals(string.Empty))
                                {
                                    WriteExceptions("inserting details transaction = " + TransactionID + " --- pack id = " + PackID2 + " ", "Inserting WH Transaction ", false);
                                    QueryBuilderObject.SetField("WarehouseID", vehicleID);
                                    QueryBuilderObject.SetField("TransactionID", "'" + TransactionID + "'");
                                    QueryBuilderObject.SetField("ZoneID", "1");
                                    QueryBuilderObject.SetField("PackID", PackID2);
                                    QueryBuilderObject.SetField("ExpiryDate", "'" + expiry + "'");
                                    QueryBuilderObject.SetField("Quantity", pieceQuantity);
                                    QueryBuilderObject.SetField("Balanced", "0");
                                    QueryBuilderObject.SetField("BatchNo", "'" + Batch + "'");
                                    QueryBuilderObject.SetField("PackStatusID", "0");
                                    QueryBuilderObject.SetField("DivisionID", DivisionID);
                                    QueryBuilderObject.SetField("ApprovedQuantity", pieceQuantity);
                                    QueryBuilderObject.SetField("RequestedQuantity", "0");

                                    err = QueryBuilderObject.InsertQueryString("WhTransDetail", db_vms, tran);
                                    if (err != InCubeErrors.Success) { throw new Exception("Error"); }


                                }
                                else
                                {
                                    //string requestedQT = GetFieldValue("WhTransDetail", "isnull(RequestedQuantity,0)", "TransactionID='" + TransactionID + "' and WarehouseID=" + vehicleID + " and PackID=" + PackID + " and BatchNo='" + Batch + "' ", db_vms, tran).Trim();
                                    WriteExceptions("updating  details transaction = " + TransactionID + " --- pack id = " + PackID + " ", "Inserting WH Transaction ", false);
                                    QueryBuilderObject.SetField("Quantity", pieceQuantity);
                                    //QueryBuilderObject.SetField("BatchNo", "'" + Batch + "'");//+ "UPDATED" 
                                    // QueryBuilderObject.SetField("RequestedQuantity", requestedQT);
                                    QueryBuilderObject.SetField("ApprovedQuantity", pieceQuantity);
                                    QueryBuilderObject.SetField("Balanced", "0");
                                    QueryBuilderObject.SetField("ExpiryDate", "'" + expiry + "'");
                                    QueryBuilderObject.SetField("BatchNo", "'" + Batch + "'");
                                    err = QueryBuilderObject.UpdateQueryString("WhTransDetail", "TransactionID='" + TransactionID + "' and WarehouseID=" + vehicleID + " and PackID=" + PackID2 + " AND ZONEID=1", db_vms, tran);
                                    if (err != InCubeErrors.Success) { throw new Exception("Error"); }

                                }
                                if (TransactionType.Equals("1"))
                                {
                                    err = UpdateVehicleStock(ref tran, ItemID, vehicleID, expiry, Batch, PackID2, pieceQuantity);
                                }
                                else if (TransactionType.Equals("2"))
                                {
                                    err = UpdateVehicleStock(ref tran, ItemID, vehicleID, expiry, Batch, PackID2, "-" + pieceQuantity);
                                }
                                if (err != InCubeErrors.Success) { throw new Exception("Error"); }
                                //************************************************************************
                            }


                            TOTALUPDATED++;
                            WriteExceptions("END OF  transaction = " + TransactionID + "", "Inserting WH Transaction ", true);
                        }

                        #endregion
                        if (TOTALUPDATED > 0)
                        {
                            tran.Commit();
                            string deleteBalanced = string.Format("update WhTransDetail set quantity=0, ApprovedQuantity=0, balanced=0 where TransactionID='" + tranStr + "' and balanced=1");
                            qry = new InCubeQuery(deleteBalanced, db_vms);
                            err = qry.ExecuteNonQuery();
                            if (err == InCubeErrors.Success)
                            {
                                WriteExceptions(" Updated Quantity to zero successfully, ", "Inserting WH Transaction ", false);
                            }
                            else
                            {
                                WriteExceptions("Updating Quantity to zero Failed *******,", "Inserting WH Transaction ", false);
                            }
                            string query = string.Format("update OW_REQ_HEAD_TXN set WRH_PROC_FLAG='X' where WRH_TXN_CODE='{0}' AND WRH_NO='{1}'", tranStr.Split('-')[1].ToString(), tranStr.Split('-')[2].ToString().TrimStart(new Char[] { '0' }));
                            OracleCommand cmdHDR = new OracleCommand(query, Conn);
                            err = ExecuteNonQuery(cmdHDR, tranStr, "STOCK");
                            cmdHDR.Dispose();
                        }
                        else
                        {
                            tran.Rollback();
                            WriteExceptions("All lines have been skipped so changes were rolled back", "Inserting WH Transaction ", false);
                        }
                    }
                    catch
                    {
                        tran.Rollback();
                    }
                    finally
                    {
                        if (err == InCubeErrors.Success)
                        {
                            if (err != InCubeErrors.Success) { throw new Exception("Error"); }
                            WriteExceptions("END OF  transactions", "Inserting WH Transaction ", true);
                            //IntegrationForm.txtMessages.AppendText("\r\n");
                            //IntegrationForm.txtMessages.AppendText("Transaction <<" + tranStr + ">> Successful.");
                        }
                    }
                }

                DTBL.Dispose();
                //IntegrationForm.txtMessages.AppendText("\r\n");
                //IntegrationForm.txtMessages.AppendText("<<< GATE PASS Updated >>> Total Updated = " + TOTALUPDATED);

            }
            catch (Exception ex)
            {
                WriteExceptions("HANDLED EXCEPTION <<" + ex.Message + ">>", "Inserting WH Transaction ", true);
            }
        }
        private InCubeErrors UpdateVehicleStock(ref InCubeTransaction tran, string ItemID, string vehicleID, string Expirydate, string Batch, string PackID, string Quantity)
        {
            InCubeErrors err = InCubeErrors.Error;
            object field = null;
            try
            {
                string query = "Select PackID from Pack where ItemID = " + ItemID;
                InCubeQuery CMD = new InCubeQuery(query, db_vms);
                CMD.Execute(tran);
                err = CMD.FindFirst();
                if (err != InCubeErrors.Success) { throw new Exception("Error"); }
                WriteExceptions("proceeding with stock insertion", "Starting Stock update", false);
                while (err == InCubeErrors.Success)
                {
                    CMD.GetField(0, ref field);
                    string _packid = field.ToString();
                    string _quantity = "0";
                    string logQty = string.Empty;
                    string existStock = GetFieldValue("WarehouseStock", "PackID", "WarehouseID = " + vehicleID + " AND ZoneID = 1 AND PackID = " + _packid + " AND BatchNo = '" + Batch + "' and expirydate='" + Expirydate + "'", db_vms, tran).Trim();
                    if (existStock.Equals(string.Empty))
                    {
                        QueryBuilderObject.SetField("WarehouseID", vehicleID);
                        QueryBuilderObject.SetField("ZoneID", "1");
                        QueryBuilderObject.SetField("PackID", _packid);
                        QueryBuilderObject.SetField("ExpiryDate", "'" + Expirydate + "'");
                        QueryBuilderObject.SetField("BatchNo", "'" + Batch + "'");
                        QueryBuilderObject.SetField("SampleQuantity", "0");

                        if (_packid == PackID)
                        {
                            QueryBuilderObject.SetField("Quantity", Quantity);
                            QueryBuilderObject.SetField("BaseQuantity", Quantity);
                            logQty = Quantity;
                        }
                        else
                        {
                            QueryBuilderObject.SetField("Quantity", _quantity);
                            QueryBuilderObject.SetField("BaseQuantity", _quantity);
                            logQty = _quantity;
                        }

                        err = QueryBuilderObject.InsertQueryString("WarehouseStock", db_vms, tran);
                        if (err != InCubeErrors.Success) { throw new Exception("Error"); }
                    }
                    else if (_packid == PackID)
                    {
                        string beforeQty = GetFieldValue("WarehouseStock", "SUM(Quantity)", "WarehouseID = " + vehicleID + " AND ZoneID = 1 AND PackID = " + _packid + "", db_vms, tran).Trim();
                        if (beforeQty.Equals(string.Empty)) beforeQty = "0";
                        //WriteExceptions("OLD STOCK QUANTITY BEFORE UPDATE  = " + beforeQty + " ,THE ADDED QUANTITY = " + Quantity + " , THE TOTAL = " + (decimal.Parse(beforeQty) + decimal.Parse(Quantity)) + "  transaction = " + TransactionID + " ---- pack id is " + _packid + "", "UPDATE Stock ", false);
                        QueryBuilderObject.SetField("Quantity", "Quantity+" + Quantity);
                        QueryBuilderObject.SetField("BaseQuantity", "BaseQuantity+" + Quantity);
                        err = QueryBuilderObject.UpdateQueryString("WarehouseStock", "WarehouseID = " + vehicleID + " AND ZoneID = 1 AND PackID = " + PackID + " AND BatchNo = '" + Batch + "' AND EXPIRYDATE='" + Expirydate + "'", db_vms, tran);
                        if (err != InCubeErrors.Success) { throw new Exception("Error"); }
                    }

                    err = CMD.FindNext();
                }
            }
            catch
            {
                //tran.Rollback();
                return InCubeErrors.Error;
            }
            return InCubeErrors.Success;
        }

        #endregion

        #region NEW STOCK INTEGRATION
        public override void UpdateStock()
        {
            UpdateStockForWarehouseEnhanced(Filters.StockDate);// UpdateStockForWarehouseTrxOnly(StockDate);//
        }

        #endregion

        #region Update Discount

        //        public override void UpdateDiscount()
        //        {
        //            int TOTALUPDATED = 0;
        //            int TOTALINSERTED = 0;

        //            InCubeErrors err;
        //            object field = new object();

        //            if (!db_vms.IsOpened())
        //            {
        //                WriteMessage("\r\n");
        //                WriteMessage("Cannot connect to GP database , please check the connection");
        //                return;
        //            }

        //            InCubeQuery DeleteDiscountQuery = new InCubeQuery(db_vms, "Delete From Discount");
        //            DeleteDiscountQuery.ExecuteNonQuery();

        //            string SelectCustomer = @"SELECT  
        //                                              A.DEX_ROW_ID,
        //                                              ltrim(rtrim(cast(C.CUSTNMBR as nvarchar(50)))) + ltrim(rtrim(cast(C.DEX_ROW_ID as nvarchar(50)))) as ADDRESSSCODE,
        //                                              A.CUSTNMBR,
        //                                              A.CUSTDISC
        //
        //FROM RM00101 AS A 
        //INNER JOIN RM00303 AS B ON A.SALSTERR=B.SALSTERR    
        //Inner join RM00102 as C ON  A.CUSTNMBR = C.CUSTNMBR
        //
        //WHERE B.COUNTRY in (SELECT IntLocation FROM ALNIntegration) AND (LEN(A.SALSTERR)=3 or LEN(A.SALSTERR)=4)
        //AND A.INACTIVE=0 AND HOLD=0 AND A.CUSTDISC > 0";

        //            InCubeQuery CustomerQuery = new InCubeQuery(db_vms, SelectCustomer);
        //            CustomerQuery.Execute();

        //            ClearProgress();
        //            SetProgressMax(CustomerQuery.GetDataTable().Rows.Count;

        //            err = CustomerQuery.FindFirst();

        //            while (err == InCubeErrors.Success)
        //            {

        //                ReportProgress(++;

        //                IntegrationForm.lblProgress.Text = "Updating Discounts " + " " + ReportProgress( + " / " + IntegrationForm.progressBar1.Maximum;
        //                ();

        //                CustomerQuery.GetField("DEX_ROW_ID", ref field);
        //                string CustomerID = field.ToString();

        //                CustomerQuery.GetField("ADDRESSSCODE", ref field);
        //                string OutletID = GetFieldValue("CustomerOutlet", "OutletID", " CustomerCode = '" + field.ToString().Trim() + "' AND CustomerID = " + CustomerID, db_vms);

        //                if (OutletID == string.Empty)
        //                {
        //                    err = CustomerQuery.FindNext();
        //                    continue;
        //                }

        //                CustomerQuery.GetField("CUSTDISC", ref field);
        //                decimal Discount = decimal.Parse(field.ToString());
        //                Discount /= 100;

        //                string MAXID = GetFieldValue("Discount", " IsNull(MAX(DiscountID),0) + 1 ", db_vms);

        //                err = ExistObject("Discount", "Discount", " CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
        //                if (err == InCubeErrors.Success)
        //                {
        //                    TOTALUPDATED++;

        //                    string DiscountID = GetFieldValue("Discount", "DiscountID", " CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
        //                    QueryBuilderObject.SetField("Discount", Discount.ToString());
        //                    QueryBuilderObject.UpdateQueryString("Discount", "DiscountID = " + DiscountID, db_vms);
        //                }
        //                else
        //                {
        //                    TOTALINSERTED++;

        //                    QueryBuilderObject.SetField("DiscountID", MAXID);
        //                    QueryBuilderObject.SetField("AllItems", "1");
        //                    QueryBuilderObject.SetField("CustomerID", CustomerID);
        //                    QueryBuilderObject.SetField("OutletID", OutletID);
        //                    QueryBuilderObject.SetField("Discount", Discount.ToString());
        //                    QueryBuilderObject.SetField("FOC", "0");
        //                    QueryBuilderObject.SetField("StartDate", "'" + DateTime.Now.Date.ToString("dd/MMM/yyyy") + "'");
        //                    QueryBuilderObject.SetField("EndDate", "'" + DateTime.Now.Date.AddYears(10).ToString("dd/MMM/yyyy") + "'");
        //                    QueryBuilderObject.InsertQueryString("Discount", db_vms);
        //                }

        //                err = CustomerQuery.FindNext();
        //            }

        //            CustomerQuery.Close();

        //            WriteMessage("\r\n");
        //            WriteMessage("<<< CUSTOMERS DISCOUNT >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        //        }

        #endregion

        #region Update OutStanding
        //        public override void OutStanding()
        //        {
        //            try
        //            {
        //                DataTable dt = new DataTable();
        //                int TOTALUPDATED = 0;
        //                int TOTALINSERTED = 0;
        //                string GetTransaction = string.Format(@"select  * from Outstanding where TRANSACTIONTYPE='SALE' and CHANGEFLG='Y' order by [ROUTE]");
        //                InCubeQuery qry = new InCubeQuery(GetTransaction, dbERP);

        //                err = qry.Execute();
        //                dt = qry.GetDataTable();
        //                ClearProgress();
        //                SetProgressMax(dt.Rows.Count;
        //                object field = new object();

        //                foreach (DataRow dr in dt.Rows)
        //                {
        //                    ReportProgress(++;
        //                    ();
        //                    string customerCode = dr["VISACCCODE"].ToString().Trim();
        //                    string employeeID = string.Empty;
        //                    string TransactionType = "1";
        //                    string TransactionStatus = "1";
        //                    string RouteCode = dr["ROUTE"].ToString().Trim();
        //                    if (RouteCode.Trim().Equals("57"))
        //                    {

        //                    }
        //                    string TotalAmount = dr["TransactionAmount"].ToString().Trim();
        //                    string customerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode='" + customerCode + "'", db_vms);
        //                    string outletID = GetFieldValue("CustomerOutlet", "OutletID", " CustomerID=" + customerID + " and CustomerCode='" + customerCode + "'", db_vms);
        //                    if (customerID.Trim().Equals(string.Empty) || outletID.Trim().Equals(string.Empty)) continue;
        //                    string RouteID = GetFieldValue("[Route]", "RouteID", "RouteCode='" + RouteCode + "'", db_vms);
        //                    if (RouteID.Trim().Equals(string.Empty)) continue;
        //                    employeeID = GetFieldValue("EmployeeTerritory", "EmployeeID", "TerritoryID in (select TerritoryID from Territory where TerritoryCode='" + RouteCode + "')", db_vms);
        //                    if (employeeID.Trim().Equals(string.Empty)) continue;
        //                    string warehouseID = GetFieldValue("Warehouse", "WarehouseID", "Barcode='" + RouteCode + "'", db_vms);
        //                    if (warehouseID.Trim().Equals(string.Empty)) continue;
        //                    string companyCode = dr["CompanyID"].ToString().Trim();
        //                    string accountID = GetFieldValue("AccountCustOutDiv", "AccountID", "CustomerID=" + customerID + " and OutletID=" + outletID + " and DivisionID=" + companyCode + "", db_vms);
        //                    if (accountID.Trim().Equals(string.Empty)) continue;
        //                    field = null;
        //                    string CheckUploaded = string.Format("select top(1)uploaded,deviceserial from RouteHistory where EmployeeID=" + employeeID + " ORDER BY RouteHistoryID Desc ");
        //                    qry = new InCubeQuery(CheckUploaded, db_vms);
        //                    err = qry.Execute();
        //                    err = qry.FindFirst();
        //                    err = qry.GetField("uploaded", ref field);
        //                    if (field == null)
        //                    {
        //                        WriteMessage("\r\n");
        //                        WriteMessage("<<< The Route " + RouteCode + " Does not have a record in Route History table .>>> Total Updated = " + TOTALUPDATED);
        //                        continue;
        //                    }
        //                    string uploaded = field.ToString().Trim();
        //                    err = qry.GetField("deviceserial", ref field);
        //                    string deviceserial = field.ToString().Trim();
        //                    if (!uploaded.ToString().Trim().Equals(string.Empty) && !uploaded.ToString().Trim().Equals("System.Object"))
        //                    {
        //                        if (Convert.ToBoolean(uploaded.ToString().Trim()))
        //                        {
        //                            WriteMessage("\r\n");
        //                            WriteMessage("<<< The Route " + RouteCode + " is not downloaded . No stock will be added .>>> Total Updated = " + TOTALUPDATED);
        //                            continue;
        //                        }

        //                    }


        //                    IntegrationForm.lblProgress.Text = "Updating OUTSTANDING" + " " + ReportProgress( + " / " + IntegrationForm.progressBar1.Maximum;

        //                    string transactionID = dr["TransactionID"].ToString().Trim();
        //                    string remainingAmount = decimal.Parse(dr["RemainingAmount"].ToString().Trim()).ToString();
        //                    string voided = "0";// dr["voided"].ToString().Trim();
        //                    string existTransaction = GetFieldValue("[Transaction]", "TransactionID", "TransactionID='" + transactionID + "'", db_vms);

        //                    string TransactionDate = dr["TRANSACTIONDATE"].ToString().Trim();

        //                    decimal accountVariance = 0;
        //                    if (!existTransaction.Trim().Equals(string.Empty))
        //                    {
        //                        //string TotalApplied = GetFieldValue("CustomerPayment", "sum(AppliedAmount)", "TransactionID='" + transactionID + "'", db_vms).Trim();
        //                        //string InVanTransactionAmount=GetFieldValue(

        //                        string InvanRem = GetFieldValue("[Transaction]", "RemainingAmount", "TransactionID='" + transactionID + "'", db_vms).Trim();
        //                        QueryBuilderObject.SetField("RemainingAmount", remainingAmount);
        //                        QueryBuilderObject.SetField("voided", voided);
        //                        err = QueryBuilderObject.UpdateQueryString("[Transaction]", "TransactionID='" + transactionID + "'", db_vms);
        //                        TOTALUPDATED++;
        //                        if (decimal.Parse(remainingAmount) > decimal.Parse(InvanRem))
        //                        {
        //                            isIncrease = true;
        //                        }
        //                        else
        //                        {
        //                            isIncrease = false;
        //                        }
        //                        accountVariance = Math.Abs(decimal.Parse(remainingAmount) - decimal.Parse(InvanRem));
        //                        AccounteRecursiveFunction(int.Parse(accountID), accountVariance, isIncrease);
        //                        //string updateAccount = string.Format("update Account set Balance=Balance+" + accountVariance + " where AccountID=" + accountID + "");
        //                        //qry = new InCubeQuery(updateAccount, db_vms);
        //                        //err = qry.ExecuteNonQuery();
        //                        //string accountID = GetFieldValue("AccountCustOutDiv", "AccountID", "CustomerID=" + customerID + " and outletID=" + outletID + " and DivisionID=" + companyCode + "", db_vms).Trim();


        //                        if (err == InCubeErrors.Success)
        //                        {
        //                            //string updateFlag = string.Format("update CUSTOUTSTAND set CHANGEFLG='N' where TRNO='" + transactionID + "'");
        //                            //qry = new InCubeQuery(updateFlag, dbERP);
        //                            //err = qry.ExecuteNonQuery();
        //                            //if(err==InCubeErrors.Success)
        //                            //{
        //                            //THE WHERE CLAUSE USED TO HAVE : VISACCCODE='" + customerCode + "' ON 27-02-2012
        //                            err = UpdateFlag("CUSTOUTSTAND", "TRNO='" + transactionID + "' and TRDT='" + DateTime.Parse(TransactionDate).ToString("yyyy/MM/dd") + "'");
        //                            //}
        //                        }
        //                    }
        //                    else if (!Convert.ToBoolean(int.Parse(voided)))
        //                    {
        //                        //SYNCHRONIZED SHOULD BE 1



        //                        //QueryBuilderObject.SetField("CustomerID", customerID.Trim());
        //                        //QueryBuilderObject.SetField("OutletID", outletID.Trim());
        //                        //QueryBuilderObject.SetField("TransactionID", "'" + transactionID.Trim() + "'");
        //                        //QueryBuilderObject.SetField("EmployeeID", employeeID.Trim());
        //                        //QueryBuilderObject.SetField("TransactionDate","'"+ DateTime.Parse(TransactionDate).ToString(DateFormat)+"'");
        //                        //QueryBuilderObject.SetField("TransactionTypeID", "1");
        //                        //QueryBuilderObject.SetField("Synchronized", "1");
        //                        //QueryBuilderObject.SetField("RemainingAmount", remainingAmount.Trim());
        //                        //QueryBuilderObject.SetField("GrossTotal", TotalAmount.Trim());
        //                        //QueryBuilderObject.SetField("Voided", "0");
        //                        //QueryBuilderObject.SetField("TransactionStatusID", "1");
        //                        //QueryBuilderObject.SetField("RouteID", RouteID.Trim());
        //                        //QueryBuilderObject.SetField("NetTotal", TotalAmount.Trim());
        //                        //QueryBuilderObject.SetField("Tax", "0");
        //                        //QueryBuilderObject.SetField("Posted", "1");
        //                        //QueryBuilderObject.SetField("CurrencyID", "1");
        //                        //QueryBuilderObject.SetField("WarehouseID",warehouseID.Trim());
        //                        //QueryBuilderObject.SetField("CreatedBy", employeeID.ToString());
        //                        //QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
        //                        //QueryBuilderObject.SetField("UpdatedBy", employeeID.ToString());
        //                        //QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
        //                        //QueryBuilderObject.SetField("AccountID", accountID.Trim());
        //                        //QueryBuilderObject.SetField("DivisionID", companyCode.Trim());
        //                        //QueryBuilderObject.SetField("SalesMode", "2");
        //                        //QueryBuilderObject.SetField("Discount", "0");
        //                        //QueryBuilderObject.SetField("VisitNo", "1");
        //                        //err=QueryBuilderObject.InsertQueryString("[Transaction'", db_vms);
        //                        isIncrease = true;
        //                        string selecttrn = string.Format(@"insert into [Transaction] (CustomerID,OutletID,TransactionID,EmployeeID,TransactionDate
        //,TransactionTypeID,Synchronized,RemainingAmount,GrossTotal,Voided,TransactionStatusID,RouteID,NetTotal,Tax,Posted,CurrencyID,WarehouseID,
        //CreatedBy,CreatedDate,UpdatedBy,UpdatedDate,AccountID,DivisionID,SalesMode,Discount,VisitNo,Notes) values 
        //({0},{1},'{2}',{3},'{4}',{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},'{18}',{19},'{20}',{21},{22},{23},{24},{25},'ERP')", customerID.Trim(), outletID.Trim(), transactionID.Trim(), employeeID.Trim(), DateTime.Parse(TransactionDate).ToString(DateFormat), 1, 1, remainingAmount.Trim(), TotalAmount.Trim(), 0, 1, RouteID.Trim(), TotalAmount.Trim(), "0", "1", "1", warehouseID.Trim(), employeeID.ToString(), DateTime.Now.ToString(DateFormat), employeeID.ToString(), DateTime.Now.ToString(DateFormat), accountID.Trim(), companyCode.Trim(), 2, 0, 1);
        //                        qry = new InCubeQuery(selecttrn, db_vms);
        //                        err = qry.ExecuteNonQuery();
        //                        if (err == InCubeErrors.Success)
        //                        {
        //                            err = UpdateFlag("CUSTOUTSTAND", "VISACCCODE='" + customerCode + "' and TRNO='" + transactionID + "' and TRDT='" + DateTime.Parse(TransactionDate).ToString("yyyy/MM/dd") + "'");
        //                        }
        //                        TOTALINSERTED++;
        //                        accountVariance = decimal.Parse(remainingAmount);
        //                        AccounteRecursiveFunction(int.Parse(accountID), accountVariance, isIncrease);
        //                    }
        //                    //string updateAccount2 = string.Format("update Account set Balance=Balance+" + accountVariance + " where AccountID=" + accountID + "");
        //                    //qry = new InCubeQuery(updateAccount2, db_vms);
        //                    //err = qry.ExecuteNonQuery();
        //                }

        //                //err=DatabaseSpecialFunctions.RunStoredProcedure(db_vms, "spUpdateTransactionRemainingAmount");
        //                WriteMessage("\r\n");
        //                WriteMessage("<<< Outstanding>>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);

        //            }
        //            catch (Exception ex)
        //            {

        //            }
        //        }
        #endregion

        #endregion

        #region ORION SEND
        public override void SendInvoices()
        {
            try
            {
                string sp = string.Empty;
                if (Filters.EmployeeID != -1)
                {
                    sp = " AND T.EmployeeID = " + Filters.EmployeeID;
                }

                string invoices = string.Format(@"SELECT T.TransactionID, T.CustomerID, T.OutletID, T.TransactionDate, WH.Barcode, CO.CustomerCode, E.EmployeeCode
FROM [Transaction] T
INNER JOIN CustomerOutlet CO ON T.CustomerID = CO.CustomerID AND T.OutletID = CO.OutletID
INNER JOIN Employee E ON T.EmployeeID = E.EmployeeID
INNER JOIN Warehouse WH ON T.WarehouseID = WH.WarehouseID
WHERE T.TransactionDate >= '{0}' AND T.TransactionDate < DATEADD(DD,1,'{1}')
AND T.Synchronized = 0 AND ISNULL(T.Notes,'0') <> 'ERP' AND T.TransactionTypeID IN (1,3) AND T.Voided = 0
AND dbo.IsRouteHistoryUploaded(T.RouteHistoryID) = 0
{2}", Filters.FromDate.ToString("yyyy/MM/dd"), Filters.ToDate.ToString("yyyy/MM/dd"), sp);

                incubeQuery = new InCubeQuery(invoices, db_vms);
                err = incubeQuery.Execute();
                if (err != InCubeErrors.Success)
                {
                    WriteMessage("Error running invoices query");
                    return;
                }
                DataTable dtHeader = new DataTable();
                dtHeader = incubeQuery.GetDataTable();
                if (dtHeader.Rows.Count == 0)
                {
                    WriteMessage("There are no invoices to send ..");
                }
                else
                {
                    ClearProgress();
                    SetProgressMax(dtHeader.Rows.Count);
                    WriteMessage(dtHeader.Rows.Count + " invoices found, sending starts ..");
                }

                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                for (int i = 0; i < dtHeader.Rows.Count; i++)
                {
                    sw.Start();
                    Result res = Result.UnKnown;
                    int processID = 0;
                    string TransactionID = "", updateSynchronizedFlag = "", message = "", CustomerID = "", OutletID = "";
                    try
                    {
                        //Read primary header values
                        TransactionID = dtHeader.Rows[i]["TransactionID"].ToString();
                        CustomerID = dtHeader.Rows[i]["CustomerID"].ToString();
                        OutletID = dtHeader.Rows[i]["OutletID"].ToString();
                        ReportProgress();
                        WriteMessage("\r\n" + TransactionID + ": ");

                        //Prepare update synchronized query
                        updateSynchronizedFlag = string.Format("UPDATE [Transaction] SET Synchronized = 1 WHERE TransactionID = '{0}' AND CustomerID = {1} AND OutletID = {2}", TransactionID, CustomerID, OutletID);

                        //Log attempt
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(1, TransactionID);
                        filters.Add(2, CustomerID + ":" + OutletID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        //Check if sent before or being sent
                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Sales_S, new List<string>(filters.Values), processID, 300);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            res = Result.Duplicate;
                            throw (new Exception("Transaction already sent !!"));
                        }
                        else if (lastRes == Result.Started)
                        {
                            res = Result.Blocked;
                            throw (new Exception("Sending is in progress with another process !!"));
                        }

                        //Read remaining header values
                        DateTime TransactionDate = Convert.ToDateTime(dtHeader.Rows[i]["TransactionDate"]);
                        string WarehouseCode = dtHeader.Rows[i]["Barcode"].ToString();
                        string CustomerCode = dtHeader.Rows[i]["CustomerCode"].ToString();
                        string EmployeeCode = dtHeader.Rows[i]["EmployeeCode"].ToString();

                        //Begin Oracle DB transaction
                        if (Conn.State == ConnectionState.Closed)
                        {
                            Conn.Open();
                        }
                        OraTrans = Conn.BeginTransaction();

                        #region Prepare Oracle header values
                        //WINVH_SYS_ID
                        object WINVH_SYS_ID = null;
                        OracleCMD = new OracleCommand("select WINVH_SYS_ID.nextval from DUAL", Conn);
                        OracleCMD.Transaction = OraTrans;
                        WINVH_SYS_ID = OracleCMD.ExecuteScalar();
                        if (WINVH_SYS_ID == null)
                        {
                            res = Result.Invalid;
                            throw (new Exception("Failed obtaining value for WINVH_SYS_ID next value"));
                        }
                        incubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE Int_ExecutionDetails SET Filter3ID = 3, Filter3Value = '{0}' WHERE ID = {1}", WINVH_SYS_ID, processID));
                        incubeQuery.ExecuteNonQuery();
                        //WINVH_TXN_CODE //WINVH_NO
                        string[] TransIDParts = TransactionID.Split('-');
                        if (TransIDParts.Length != 3)
                        {
                            res = Result.Invalid;
                            throw (new Exception("Transaction ID doesn't have 3 parts as expected"));
                        }
                        string WINVH_TXN_CODE = TransIDParts[1];
                        string WINVH_NO = TransIDParts[2];
                        //WINVH_DT //WINVH_CR_DT
                        string WINVH_DT = TransactionDate.ToString("dd-MMM-yy");
                        #endregion

                        //Insert header in Oracle
                        string insertHeader = string.Format(@"INSERT INTO OW_INVOICE_HEAD_TXN 
(WINVH_SYS_ID,WINVH_COMP_CODE,WINVH_TXN_CODE,WINVH_NO,WINVH_DT,WINVH_LOCN_CODE,WINVH_CUST_CODE,WINVH_DISC_AMT,WINVH_CR_DT,WINVH_CR_UID) VALUES
({0},'{1}','{2}','{3}','{4}','{5}','{6}',{7},'{8}','{9}')"
, WINVH_SYS_ID, "001", WINVH_TXN_CODE, WINVH_NO, WINVH_DT, WarehouseCode, CustomerCode, 0, WINVH_DT, EmployeeCode);
                        OracleCMD = new OracleCommand(insertHeader, Conn);
                        OracleCMD.Transaction = OraTrans;
                        OracleCMD.ExecuteNonQuery();

                        #region Details
                        //Read details from DB
                        string details = string.Format(@"SELECT I.ItemCode, PTL.Description UOM, CASE TD.SalesTransactionTypeID WHEN 1 THEN '2' ELSE '1' END FOC_FLAG, TD.Quantity, TD.Price, TD.Discount, TD.BatchNo
FROM TransactionDetail TD
INNER JOIN Pack P ON P.PackID = TD.PackID
INNER JOIN Item I ON I.ItemID = P.ItemID
INNER JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1
WHERE TD.TransactionID = '{0}' AND TD.CustomerID = {1} AND TD.OutletID = {2}"
, TransactionID, CustomerID, OutletID);

                        incubeQuery = new InCubeQuery(details, db_vms);
                        err = incubeQuery.Execute();
                        if (err != InCubeErrors.Success)
                        {
                            res = Result.Invalid;
                            throw (new Exception("Error reading details"));
                        }
                        DataTable dtDetails = new DataTable();
                        dtDetails = incubeQuery.GetDataTable();
                        if (dtDetails.Rows.Count == 0)
                        {
                            res = Result.Invalid;
                            throw (new Exception("No details found"));
                        }

                        //Loop through details
                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        {
                            //Read from query
                            string ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            string UOM = dtDetails.Rows[j]["UOM"].ToString();
                            string FOC_FLAG = dtDetails.Rows[j]["FOC_FLAG"].ToString();
                            decimal Quantity = Convert.ToDecimal(dtDetails.Rows[j]["Quantity"]);
                            decimal Price = Convert.ToDecimal(dtDetails.Rows[j]["Price"]);
                            decimal Discount = Convert.ToDecimal(dtDetails.Rows[j]["Discount"]);
                            string BatchNo = dtDetails.Rows[j]["BatchNo"].ToString();

                            //WINVH_SYS_ID
                            object WINVI_SYS_ID = null;
                            OracleCMD = new OracleCommand("select WINVI_SYS_ID.NextVal from DUAL", Conn);
                            WINVI_SYS_ID = OracleCMD.ExecuteScalar();
                            if (WINVI_SYS_ID == null)
                            {
                                res = Result.Invalid;
                                throw (new Exception("Failed obtaining value for WINVI_SYS_ID next value"));
                            }

                            //WINVB_BATCH_EXP_DT
                            string WINVB_BATCH_EXP_DT = (new DateTime(1990, 1, 1)).ToString("dd-MMM-yy");

                            //Insert detail
                            string insertDetail = string.Format(@"INSERT INTO OW_INVOICE_ITEM_TXN 
(WINVI_SYS_ID,WINVI_WINVH_SYS_ID,WINVI_ITEM_CODE,WINVI_UOM_CODE,WINVI_FOC_ITEM_YN_NUM,WINVI_QTY,WINVI_QTY_LS,WINVI_RATE,WINVI_DISC_AMT,WINVI_CR_DT,WINVI_CR_UID) VALUES 
({0},{1},'{2}','{3}',{4},{5},{6},{7},{8},'{9}','{10}')"
    , WINVI_SYS_ID, WINVH_SYS_ID, ItemCode, UOM, FOC_FLAG, Quantity, 0, Price - Discount, Discount, WINVH_DT, EmployeeCode);
                            OracleCMD = new OracleCommand(insertDetail, Conn);
                            OracleCMD.Transaction = OraTrans;
                            OracleCMD.ExecuteNonQuery();

                            //Insert batch
                            string insertDetailBatch = string.Format(@"INSERT INTO OW_INVOICE_BATCH_TXN 
(WINVB_SYS_ID,WINVB_WINVH_SYS_ID,WINVB_WINVI_SYS_ID,WINVB_BATCH_NO,WINVB_BATCH_EXP_DT,WINVB_BATCH_QTY,WINVB_BATCH_QTY_LS,WINVB_CR_DT,WINVB_CR_UID) VALUES 
({0},{1},{2},'{3}','{4}',{5},{6},'{7}','{8}')"
    , WINVI_SYS_ID, WINVH_SYS_ID, WINVI_SYS_ID, BatchNo, WINVB_BATCH_EXP_DT, Quantity, 0, WINVH_DT, EmployeeCode);
                            OracleCMD = new OracleCommand(insertDetailBatch, Conn);
                            OracleCMD.Transaction = OraTrans;
                            OracleCMD.ExecuteNonQuery();
                        }
                        #endregion

                        res = Result.Success;
                    }
                    catch (Exception ex)
                    {
                        if (res == Result.UnKnown)
                        {
                            res = Result.Failure;
                            message = "Unhandled exception !!";
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        }
                        else
                        {
                            message = ex.Message;
                        }
                    }
                    finally
                    {
                        try
                        {
                            if (res == Result.Success && OraTrans != null)
                            {
                                OraTrans.Commit();
                                message = "Success";
                            }
                            else if (res != Result.Success && OraTrans != null)
                            {
                                OraTrans.Rollback();
                            }
                            if (OraTrans != null)
                                OraTrans.Dispose();
                            if (Conn.State == ConnectionState.Open)
                            {
                                Conn.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            message = "Error committing Oracle transaction";
                            res = Result.Failure;
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        }
                        WriteMessage(message);

                        if (res == Result.Success || res == Result.Duplicate)
                        {
                            incubeQuery = new InCubeQuery(db_vms, updateSynchronizedFlag);
                            incubeQuery.ExecuteNonQuery();
                        }
                        sw.Stop();
                        execManager.LogIntegrationEnding(processID, res, sw.ElapsedMilliseconds.ToString(), res.ToString());
                        sw.Reset();
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public void SendInvoices_old()
        {
            if (Conn.State == ConnectionState.Closed)
            {
                Conn.Open();
            }

            OracleTransaction _tran = null;


            try
            {
                //TO DO : Handle the date format
                //SALES MODE : 1=CASH , 2=CREDIT, 3=TEMPORARY CREDIT
                DataTable dt;
                string updateVoided = string.Format(@"update [Transaction] set Synchronized=0 where Voided=1  AND 
            (TransactionDate >= '" + Filters.FromDate.Date.ToString("yyyy/MM/dd") + @"' 
            AND TransactionDate <= '" + Filters.ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"')");
                qry = new InCubeQuery(updateVoided, db_vms);
                err = qry.ExecuteNonQuery();
                string sp = string.Empty;
                if (Filters.EmployeeID != -1)
                {
                    sp = " AND T.EmployeeID = " + Filters.EmployeeID;
                }
                string invoices = string.Format(@"select CO.CustomerCode,T.TransactionID,E.EmployeeCode,T.TransactionDate,
                T.TransactionTypeID,T.DiscountAuthorization,T.Discount,Null,T.Synchronized,T.RemainingAmount,SourceTransactionID,T.GrossTotal,T.GPSLatitude
                ,T.GPSLongitude,T.Voided,T.Notes,TransactionStatusID,RouteID,T.NetTotal,T.Tax,Posted,T.CurrencyID,WH.Barcode,T.CreatedBy,T.CreatedDate,T.UpdatedBy,
                T.UpdatedDate,T.Downloaded,T.VisitNo,T.RouteHistoryID,T.AccountID,T.PromotedDiscount,(case(T.SalesMode) when 3 then 2 else T.SalesMode end ) as SalesMode,
                COL.Address,(case(isnull(CO.CustomerTypeID,1)) when 1 then 'CASH'else cast(PT.SimplePeriodWidth as nvarchar(30)) end) as Term,COL.Description as CustName,
                SL.Description as Street, CL.Description as City
                from [Transaction] T
                inner join transactiondetail td on t.transactionid=td.transactionid and T.CustomerID=td.CustomerID and T.OutletID=td.OutletID
                inner join CustomerOutlet CO on T.CustomerID=CO.CustomerID and T.OutletID=CO.OutletID
                inner join CustomerOutletLanguage COL on COL.CustomerID=T.CustomerID and COL.OutletID=T.OutletID and LanguageID=1
                left outer join PaymentTerm PT on CO.PaymentTermID=PT.PaymentTermID
                left join Street S on CO.StreetID=S.StreetID
                left join StreetLanguage SL on CO.StreetID=SL.StreetID and SL.LanguageID=1
                left join City C on S.CityID=C.CityID and S.StateID=C.StateID and S.CountryID=C.CountryID
                left join CityLanguage CL on CL.CityID=C.CityID and CL.LanguageID=1
                inner join Employee E on T.EmployeeID=E.EmployeeID
                inner join Warehouse WH on T.WarehouseID=WH.WarehouseID
                where Synchronized=0   and  
                (isnull(T.Notes,'0')<>'ERP') AND (T.TransactionTypeID = 1 or T.TransactionTypeID = 3) and T.Voided=0 AND T.TRANSACTIONID NOT IN (SELECT TRANSACTIONID FROM HSNI_Invoice_Sync) 
                group by 
                CO.CustomerCode,T.TransactionID,E.EmployeeCode,T.TransactionDate,PT.SimplePeriodWidth,
                T.TransactionTypeID,T.DiscountAuthorization,T.Discount,T.Synchronized,T.RemainingAmount,SourceTransactionID,T.GrossTotal,T.GPSLatitude
                ,T.GPSLongitude,T.Voided,T.Notes,TransactionStatusID,RouteID,T.NetTotal,T.Tax,Posted,T.CurrencyID,WH.Barcode,T.CreatedBy,T.CreatedDate,T.UpdatedBy,
                T.UpdatedDate,T.Downloaded,T.VisitNo,T.RouteHistoryID,T.AccountID,T.PromotedDiscount,CO.CustomerTypeID,T.SalesMode,COL.Address,COL.Description,
                SL.Description, CL.Description
                ", sp);
                //the query above used to have date range : AND 
                //(T.TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
                //AND T.TransactionDate <= '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"')
                #region Invoice Header

                #region Variables
                InCubeQuery invoiceQry = new InCubeQuery(invoices, db_vms);
                err = invoiceQry.Execute();
                dt = new DataTable();
                dt = invoiceQry.GetDataTable();
                string tranDetail = string.Empty;
                DataTable detailtbl;
                string insertDetail = string.Empty;
                string insertDetailBatch = string.Empty;
                int TOTALINSERTED = 0;
                ClearProgress();
                SetProgressMax(dt.Rows.Count);
                string WINVH_SYS_ID = string.Empty;
                string WINVH_COMP_CODE = string.Empty;
                string WINVH_TXN_CODE = string.Empty;
                string WINVH_NO = string.Empty;
                string WINVH_DT = string.Empty;
                string WINVH_LOCN_CODE = string.Empty;
                string WINVH_CUST_CODE = string.Empty;
                string WINVH_DISC_AMT = string.Empty;
                string WINVH_CR_DT = string.Empty;
                string WINVH_CR_UID = string.Empty;

                string WINVI_SYS_ID = string.Empty;
                string WINVI_WINVH_SYS_ID = string.Empty;
                string WINVI_ITEM_CODE = string.Empty;
                string WINVI_UOM_CODE = string.Empty;
                string WINVI_FOC_ITEM_YN_NUM = string.Empty;
                string WINVI_QTY = string.Empty;
                string WINVI_QTY_LS = string.Empty;
                string WINVI_RATE = string.Empty;
                string WINVI_DISC_AMT = string.Empty;
                string WINVI_CR_DT = string.Empty;
                string WINVI_CR_UID = string.Empty;
                string InvoiceNumberComplete = string.Empty;
                string WINVB_BACTH_NO = string.Empty;

                int detailCounter = 0;
                #endregion
                foreach (DataRow dr in dt.Rows)
                {

                    #region Fill Header Variables
                    detailCounter = 0;

                    object field = new object();

                    // don't forget to add the brefix from document sequence----> NO i decided to take it from the transaction .
                    //string prefix = GetFieldValue("DocumentSequence", "MaxTransactionInvoiceID", "EmployeeID in (select EmployeeID from Employee where EmployeeCode='" + INVH_SM_CODE + "'", db_vms).Trim();
                    string[] prefixArr = new string[2];
                    prefixArr = dr["TransactionID"].ToString().Trim().Split('-');
                    InvoiceNumberComplete = dr["TransactionID"].ToString().Trim();
                    WINVH_COMP_CODE = "001";
                    WINVH_TXN_CODE = prefixArr[1].ToString();// dr["Barcode"].ToString().Trim();
                    WINVH_NO = prefixArr[2].ToString();// dr["TransactionID"].ToString().Trim();
                    WINVH_DT = DateTime.Parse(dr["TransactionDate"].ToString()).ToString(DateFormat).Trim();

                    WINVH_LOCN_CODE = dr["Barcode"].ToString().Trim();
                    WINVH_CUST_CODE = dr["CustomerCode"].ToString().Trim();
                    //WINVH_DISC_AMT = dr["Discount"].ToString().Trim();
                    WINVH_DISC_AMT = "0";
                    WINVH_CR_UID = dr["EmployeeCode"].ToString().Trim();
                    WINVH_CR_DT = WINVH_DT;


                    #endregion

                    #region CHECK UPLOADED
                    string M_RouteHistoryID = dr["RouteHistoryID"].ToString().Trim();
                    string M_TransactionTypeID = dr["TransactionTypeID"].ToString().Trim();
                    bool uploaded = true;
                    if (M_TransactionTypeID.Equals("3") || M_TransactionTypeID.Equals("4"))
                    {
                        if (M_RouteHistoryID.Trim().Equals(string.Empty))
                        {
                            uploaded = false;
                        }
                        else
                        {
                            uploaded = Convert.ToBoolean(GetFieldValue("RouteHistory", "isnull(Uploaded,0)", "RouteHistoryID=" + M_RouteHistoryID + "", db_vms).ToString());
                        }
                    }
                    else
                    {
                        uploaded = Convert.ToBoolean(GetFieldValue("RouteHistory", "isnull(Uploaded,0)", "RouteHistoryID=" + M_RouteHistoryID + "", db_vms).ToString());
                    }
                    if (uploaded)
                    {
                        if (Convert.ToBoolean(uploaded.ToString().Trim()))
                        {
                            WriteExceptions("HHT IS NOT DOWNLOADED SKIPPING TRANSACTION (" + InvoiceNumberComplete + ")", "TRANSACTION SKIPPED 2", false);
                            continue;
                        }
                    }
                    #endregion

                    #region sub

                    //string CHKEmployeeID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode='" + dr["EmployeeCode"].ToString().Trim() + "'", db_vms).Trim();
                    //string MaxSequence = string.Empty;
                    //field = new object();
                    //string CheckUploaded = string.Format("select top(1)uploaded,deviceserial from RouteHistory where EmployeeID=" + CHKEmployeeID + " ORDER BY RouteHistoryID Desc ");
                    //qry = new InCubeQuery(CheckUploaded, db_vms);
                    //err = qry.Execute();
                    //err = qry.FindFirst();
                    //err = qry.GetField("uploaded", ref field);
                    //string uploaded = field.ToString().Trim();
                    //err = qry.GetField("deviceserial", ref field);
                    //string deviceserial = field.ToString().Trim();
                    //if (uploaded.ToString().Trim().Equals(string.Empty) || uploaded.ToString().Trim().Equals("System.Object")) continue;
                    //if (!uploaded.ToString().Trim().Equals(string.Empty) && !uploaded.ToString().Trim().Equals("System.Object"))
                    //{
                    //    if (Convert.ToBoolean(uploaded.ToString().Trim()))
                    //    {
                    //        WriteMessage("\r\n");
                    //        WriteMessage("<<< The Route " + dr[2].ToString().Trim() + " is not downloaded . No Transactions will be Transfered .>>> Total Updated = " + TOTALUPDATED);
                    //        WriteExceptions("Route " + dr[2].ToString().Trim() + " was not downloaded ---- transaction = " + dr[1].ToString().Trim() + "", "Device not downloaded", true);
                    //        continue;
                    //    }

                    //}
                    //WriteExceptions("Route " + dr[2].ToString().Trim() + " is downloaded ...", "Device is downloaded", false);
                    _tran = Conn.BeginTransaction();
                    OracleCommand cmdGet = new OracleCommand("select WINVH_SYS_ID.nextval from DUAL", Conn);
                    cmdGet.Transaction = _tran;
                    string maxSysID = GetValue(cmdGet);
                    cmdGet.Dispose();

                    if (!maxSysID.ToString().Trim().Equals(string.Empty))
                    {
                        WINVH_SYS_ID = maxSysID;
                    }
                    else { return; }
                    cmdGet = new OracleCommand("select WINVH_NO from OW_INVOICE_HEAD_TXN where WINVH_NO='" + WINVH_NO + "' and WINVH_TXN_CODE='" + WINVH_TXN_CODE + "'", Conn);
                    cmdGet.Transaction = _tran;
                    string ExistTransactions = GetValue(cmdGet);
                    cmdGet.Dispose();
                    if (ExistTransactions.Equals(string.Empty))
                    {
                        #region Insert the Header
                        ReportProgress("Sending Transactions");

                        string updatedDate = DateTime.Now.ToString(DateFormat);
                        string insertInvoices = string.Format(@"insert into   OW_INVOICE_HEAD_TXN( WINVH_SYS_ID
,WINVH_COMP_CODE
,WINVH_TXN_CODE
,WINVH_NO
,WINVH_DT
,WINVH_LOCN_CODE
,WINVH_CUST_CODE
,WINVH_DISC_AMT
,WINVH_CR_DT
,WINVH_CR_UID
                            ) VALUES 
                             (" + WINVH_SYS_ID +
 ",'" + WINVH_COMP_CODE +
 "','" + WINVH_TXN_CODE +
 "','" + WINVH_NO +
 "','" + WINVH_DT +
 "','" + WINVH_LOCN_CODE +
 "','" + WINVH_CUST_CODE +
 "'," + WINVH_DISC_AMT +
 ",'" + WINVH_CR_DT +
 "','" + WINVH_CR_UID + "')");

                        OracleCommand cmdHDR = new OracleCommand(insertInvoices, Conn);
                        cmdHDR.Transaction = _tran;
                        err = ExecuteNonQuery(cmdHDR, dr["TransactionID"].ToString().Trim(), "SALES");
                        cmdHDR.Dispose();
                        #endregion
                        if (err != InCubeErrors.Success)
                        {

                        }


                        #endregion
                        #endregion
                        if (err == InCubeErrors.Success)
                        {
                            #region Invoice details

                            tranDetail = string.Format(@"select CO.Barcode,TransactionID,I.ItemCode,P.Quantity UQTY,BatchNo,PackStatusID,TD.Quantity AS QTY,Price,BasePrice,
                             Tax,BaseTax,Warehoused,ExpiryDate,Discount,ReturnReason,SalesTransactionTypeID,BaseDiscount,FOCTypeID,Sequence,
case(P.Quantity)when 1 then 1 else 2 end,IL.DESCRIPTION AS ITEMNAME,PTL.DESCRIPTION AS UOM,P.PackID from 
                                TransactionDetail TD inner join CustomerOutlet CO on CO.CustomerID=TD.CustomerID and CO.OutletID=TD.OutletID
                                inner join Pack P on TD.PackID=P.PackID 
                                inner join Item I on I.ItemID=P.ItemID
                                INNER JOIN ITEMLANGUAGE IL ON I.ITEMID=IL.ITEMID AND IL.LANGUAGEID=1
                                INNER JOIN PACKTYPELANGUAGE PTL ON PTL.PACKTYPEID=P.PACKTYPEID AND PTL.LANGUAGEID=1
                                where TransactionID='" + InvoiceNumberComplete + "'");

                            invoiceQry = new InCubeQuery(tranDetail, db_vms);
                            err = invoiceQry.Execute();
                            detailtbl = new DataTable();
                            detailtbl = invoiceQry.GetDataTable();
                            WriteExceptions("#################################################################", "TRANSACTION DETAIL", false);
                            WriteExceptions("NUMBER OF DETAILS FOR TRANSACTION", "TRANSACTION DETAIL", false);
                            WriteExceptions("NUMBER OF DETAILS FOR TRANSACTION ( " + InvoiceNumberComplete + ") ARE " + detailtbl.Rows.Count.ToString() + "", "TRANSACTION DETAIL", false);
                            if (detailtbl.Rows.Count == 0)
                            {
                                string updateSync = string.Format("update [Transaction] set Synchronized=0 where TransactionID='" + dr[1].ToString().Trim() + "'");
                                qry = new InCubeQuery(updateSync, db_vms);
                                err = qry.ExecuteNonQuery();
                                err = InCubeErrors.Error;
                            }


                            foreach (DataRow det in detailtbl.Rows)
                            {
                                detailCounter++;
                                WINVI_SYS_ID = string.Empty;
                                cmdGet = new OracleCommand("select WINVI_SYS_ID.NextVal from DUAL", Conn);
                                cmdGet.Transaction = _tran;
                                maxSysID = GetValue(cmdGet);
                                cmdGet.Dispose();

                                if (!maxSysID.Equals(string.Empty))
                                {
                                    WINVI_SYS_ID = maxSysID;
                                }
                                else { return; }
                                WINVI_DISC_AMT = dr["Discount"].ToString().Trim();
                                string PackID = det["PackID"].ToString().Trim();

                                WINVI_WINVH_SYS_ID = WINVH_SYS_ID;

                                WINVI_ITEM_CODE = det["ItemCode"].ToString().Trim();

                                WINVI_UOM_CODE = det["UOM"].ToString().Trim();
                                if (det["SalesTransactionTypeID"].ToString().Trim().Equals("2") || det["SalesTransactionTypeID"].ToString().Trim().Equals("3") || det["SalesTransactionTypeID"].ToString().Trim().Equals("4"))
                                {
                                    WINVI_FOC_ITEM_YN_NUM = "1";//###
                                }
                                else
                                {
                                    WINVI_FOC_ITEM_YN_NUM = "2";
                                }
                                WINVI_QTY = det["QTY"].ToString().Trim();
                                WINVI_QTY_LS = "0";
                                WINVI_RATE = det["Price"].ToString().Trim();



                                WINVI_CR_UID = WINVH_CR_UID;
                                WINVI_CR_DT = WINVH_CR_DT;
                                WINVB_BACTH_NO = det["BatchNo"].ToString().Trim();

                                insertDetail = string.Format(@"insert into   OW_INVOICE_ITEM_TXN (
                                         WINVI_SYS_ID
,WINVI_WINVH_SYS_ID
,WINVI_ITEM_CODE
,WINVI_UOM_CODE
,WINVI_FOC_ITEM_YN_NUM
,WINVI_QTY
,WINVI_QTY_LS
,WINVI_RATE
,WINVI_DISC_AMT
,WINVI_CR_DT
,WINVI_CR_UID
                                        ) VALUES (" + WINVI_SYS_ID
+ " , " + WINVI_WINVH_SYS_ID
+ ",'" + WINVI_ITEM_CODE
+ "','" + WINVI_UOM_CODE
+ "'," + WINVI_FOC_ITEM_YN_NUM
+ "," + WINVI_QTY
+ "," + WINVI_QTY_LS
+ "," + WINVI_RATE
+ "," + WINVI_DISC_AMT
+ ",'" + WINVI_CR_DT
+ "','" + WINVI_CR_UID + "')");


                                OracleCommand cmdDR = new OracleCommand(insertDetail, Conn);
                                cmdDR.Transaction = _tran;
                                err = ExecuteNonQuery(cmdDR, dr["TransactionID"].ToString().Trim(), "SALES");
                                string _expiryDate = DateTime.Parse("1990/01/01".ToString()).ToString(DateFormat).Trim();
                                //Batches
                                insertDetailBatch = string.Format(@"insert into   OW_INVOICE_BATCH_TXN (
                                         WINVB_SYS_ID
,WINVB_WINVH_SYS_ID
,WINVB_WINVI_SYS_ID
,WINVB_BATCH_NO
,WINVB_BATCH_EXP_DT
,WINVB_BATCH_QTY
,WINVB_BATCH_QTY_LS
,WINVB_CR_DT
,WINVB_CR_UID
                                        ) VALUES (" + WINVI_SYS_ID
+ " , " + WINVI_WINVH_SYS_ID
+ " , " + WINVI_SYS_ID
+ ",'" + WINVB_BACTH_NO
+ "','" + _expiryDate
+ "'," + WINVI_QTY
+ "," + WINVI_QTY_LS
+ ",'" + WINVI_CR_DT
+ "','" + WINVI_CR_UID + "')");

                                OracleCommand cmdDR2 = new OracleCommand(insertDetailBatch, Conn);
                                cmdDR2.Transaction = _tran;
                                err = ExecuteNonQuery(cmdDR2, dr["TransactionID"].ToString().Trim(), "SALES");


                                if (err == InCubeErrors.Success)
                                {
                                    //                                    string updateOS_LOCN_CURR = string.Format(@"UPDATE OS_LOCN_CURR_STK SET LCS_ISSD_QTY_BU =LCS_ISSD_QTY_BU +{0}
                                    //WHERE LCS_ITEM_CODE='{1}' AND LCS_GRADE_CODE_2='G2' AND LCS_GRADE_CODE_1='A' AND LCS_LOCN_CODE='{2}' AND LCS_COMP_CODE='{3}'", INVI_QTY_BU, INVI_ITEM_CODE, INVH_DEL_LOCN_CODE, INVH_COMP_CODE);
                                    //                                    OleDbCommand UPDT = new OleDbCommand(updateOS_LOCN_CURR, Conn, _tran);
                                    //                                    err = ExecuteNonQuery(UPDT);
                                    //                                    cmdDR.Dispose();
                                    if (err != InCubeErrors.Success)
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    break;
                                }

                                cmdDR.Dispose();

                                if (err != InCubeErrors.Success)
                                {
                                    //WriteMessage("\r\n" + InvoiceNumberComplete + " - Transaction Failed");
                                }
                                //else
                                //{
                                //    string update = string.Format("update [Transaction] set Synchronized=1 where TransactionID='{0}'", InvoiceNumberComplete);
                                //    invoiceQry = new InCubeQuery(update, db_vms);
                                //    err = invoiceQry.ExecuteNonQuery();

                                //}
                            }
                            #endregion
                        }



                        if (err == InCubeErrors.Success)
                        {
                            string update = string.Format("update [Transaction] set Synchronized=1 where TransactionID='{0}'", InvoiceNumberComplete);
                            invoiceQry = new InCubeQuery(update, db_vms);
                            int rows = 0;
                            err = invoiceQry.ExecuteNonQuery(ref rows);
                            if (err == InCubeErrors.Success && rows >= 1)
                            {
                                update = string.Format("insert into HSNI_Invoice_Sync Values('{0}',1,GetDate())", InvoiceNumberComplete);
                                invoiceQry = new InCubeQuery(update, db_vms);
                                err = invoiceQry.ExecuteNonQuery();
                                WriteMessage("\r\n" + InvoiceNumberComplete + " - Transaction sent Successfully");
                                TOTALINSERTED++;
                                _tran.Commit();
                            }
                            else
                            {
                                WriteMessage("\r\n" + InvoiceNumberComplete + " - Transaction Failed");
                                _tran.Rollback();
                            }
                        }
                        else
                        {
                            WriteMessage("\r\n" + InvoiceNumberComplete + " - Transaction Failed");
                            _tran.Rollback();
                        }
                    }
                    else
                    {
                        WriteMessage("\r\n" + InvoiceNumberComplete + " - Transaction exists in Orion");
                        WriteExceptions(InvoiceNumberComplete + " - Transaction exists in Orion", "error", true);
                        InsertLog(InvoiceNumberComplete, "Transaction already exists in ORION", "SALES");
                        _tran.Rollback();

                    }

                }

                WriteMessage("\r\n");
                //WriteMessage("<<< Transactions >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
                //THIS CALLS THE STORED PROCEDURE THAT FIXES THE DOCUMENT SEQUENCE .
                //err = DatabaseSpecialFunctions.RunStoredProcedure(db_vms, "spHandleDocumentSequence");

            }
            catch
            {
                if (_tran != null) _tran.Rollback();
            }
            finally
            {
                if (_tran != null) _tran.Dispose();
            }
            //SendCustomerVisits();
        }

        #region SendReturns
        public override void SendReturn()
        {
            if (Conn.State == ConnectionState.Closed)
            {
                Conn.Open();
            }

            OracleTransaction _tran = null;


            try
            {
                //TO DO : Handle the date format
                //SALES MODE : 1=CASH , 2=CREDIT, 3=TEMPORARY CREDIT
                DataTable dt;
                string updateVoided = string.Format(@"update [Transaction] set Synchronized=0 where Voided=1  AND 
            (TransactionDate >= '" + Filters.FromDate.Date.ToString("yyyy/MM/dd") + @"' 
            AND TransactionDate <= '" + Filters.ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"')");
                qry = new InCubeQuery(updateVoided, db_vms);
                err = qry.ExecuteNonQuery();
                string sp = string.Empty;
                if (Filters.EmployeeID != -1)
                {
                    sp = " AND T.EmployeeID = " + Filters.EmployeeID;
                }
                string invoices = string.Format(@"select CO.CustomerCode,T.TransactionID,E.EmployeeCode,T.TransactionDate,
                T.TransactionTypeID,T.DiscountAuthorization,T.Discount,Null,T.Synchronized,T.RemainingAmount,SourceTransactionID,T.GrossTotal,T.GPSLatitude
                ,T.GPSLongitude,T.Voided,T.Notes,TransactionStatusID,RouteID,T.NetTotal,T.Tax,Posted,T.CurrencyID,WH.Barcode,T.CreatedBy,T.CreatedDate,T.UpdatedBy,
                T.UpdatedDate,T.Downloaded,T.VisitNo,T.RouteHistoryID,T.AccountID,T.PromotedDiscount,(case(T.SalesMode) when 3 then 2 else T.SalesMode end ) as SalesMode,
                COL.Address,(case(isnull(CO.CustomerTypeID,1)) when 1 then 'CASH'else cast(PT.SimplePeriodWidth as nvarchar(30)) end) as Term,COL.Description as CustName,
                SL.Description as Street, CL.Description as City,dbo.IsRouteUploadedForRouteHistory(T.RouteHistoryID) Uploaded
                from [Transaction] T
                inner join transactiondetail td on t.transactionid=td.transactionid and T.CustomerID=td.CustomerID and T.OutletID=td.OutletID
                inner join CustomerOutlet CO on T.CustomerID=CO.CustomerID and T.OutletID=CO.OutletID
                inner join CustomerOutletLanguage COL on COL.CustomerID=T.CustomerID and COL.OutletID=T.OutletID and LanguageID=1
                left outer join PaymentTerm PT on CO.PaymentTermID=PT.PaymentTermID
                left join Street S on CO.StreetID=S.StreetID
                left join StreetLanguage SL on CO.StreetID=SL.StreetID and SL.LanguageID=1
                left join City C on S.CityID=C.CityID and S.StateID=C.StateID and S.CountryID=C.CountryID
                left join CityLanguage CL on CL.CityID=C.CityID and CL.LanguageID=1
                inner join Employee E on T.EmployeeID=E.EmployeeID
                inner join Warehouse WH on T.WarehouseID=WH.WarehouseID
                where Synchronized=0   and 
                (isnull(T.Notes,'0')<>'ERP') AND (T.TransactionTypeID = 2 or T.TransactionTypeID = 4) and T.Voided=0 AND T.TRANSACTIONID NOT IN (SELECT TRANSACTIONID FROM HSNI_Invoice_Sync) {0}
                group by 
                CO.CustomerCode,T.TransactionID,E.EmployeeCode,T.TransactionDate,PT.SimplePeriodWidth,
                T.TransactionTypeID,T.DiscountAuthorization,T.Discount,T.Synchronized,T.RemainingAmount,SourceTransactionID,T.GrossTotal,T.GPSLatitude
                ,T.GPSLongitude,T.Voided,T.Notes,TransactionStatusID,RouteID,T.NetTotal,T.Tax,Posted,T.CurrencyID,WH.Barcode,T.CreatedBy,T.CreatedDate,T.UpdatedBy,
                T.UpdatedDate,T.Downloaded,T.VisitNo,T.RouteHistoryID,T.AccountID,T.PromotedDiscount,CO.CustomerTypeID,T.SalesMode,COL.Address,COL.Description,
                SL.Description, CL.Description
                 
                 
                 
                ", sp);
                //the query above used to have date range : AND 
                //(T.TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
                //AND T.TransactionDate <= '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"')
                #region Invoice Header

                #region Variables
                InCubeQuery invoiceQry = new InCubeQuery(invoices, db_vms);
                err = invoiceQry.Execute();
                dt = new DataTable();
                dt = invoiceQry.GetDataTable();
                string tranDetail = string.Empty;
                DataTable detailtbl;
                string insertDetail = string.Empty;
                string insertDetailBatch = string.Empty;
                int TOTALINSERTED = 0;
                ClearProgress();
                SetProgressMax(dt.Rows.Count);
                string WCSRH_SYS_ID = string.Empty;
                string WCSRH_COMP_CODE = string.Empty;
                string WCSRH_TXN_CODE = string.Empty;
                string WCSRH_NO = string.Empty;
                string WCSRH_DT = string.Empty;
                string WCSRH_LOCN_CODE = string.Empty;
                string WCSRH_CUST_CODE = string.Empty;
                string WCSRH_DISC_AMT = string.Empty;
                string WCSRH_CR_DT = string.Empty;
                string WCSRH_CR_UID = string.Empty;

                string WCSRI_SYS_ID = string.Empty;
                string WCSRI_WCSRH_SYS_ID = string.Empty;
                string WCSRI_ITEM_CODE = string.Empty;
                string WCSRI_UOM_CODE = string.Empty;
                string WCSRI_FOC_ITEM_YN_NUM = string.Empty;
                string WCSRI_QTY = string.Empty;
                string WCSRI_QTY_LS = string.Empty;
                string WCSRI_RATE = string.Empty;
                string WCSRI_BACTH_NO = string.Empty;
                string WCSRI_DISC_AMT = string.Empty;
                string WCSRI_CR_DT = string.Empty;
                string WCSRI_CR_UID = string.Empty;
                string InvoiceNumberComplete = string.Empty;
                int detailCounter = 0;
                #endregion
                foreach (DataRow dr in dt.Rows)
                {

                    #region Fill Header Variables
                    detailCounter = 0;

                    object field = new object();

                    // don't forget to add the brefix from document sequence----> NO i decided to take it from the transaction .
                    //string prefix = GetFieldValue("DocumentSequence", "MaxTransactionInvoiceID", "EmployeeID in (select EmployeeID from Employee where EmployeeCode='" + INVH_SM_CODE + "'", db_vms).Trim();
                    string[] prefixArr = new string[2];
                    prefixArr = dr["TransactionID"].ToString().Trim().Split('-');
                    InvoiceNumberComplete = dr["TransactionID"].ToString().Trim();
                    WCSRH_COMP_CODE = "001";
                    WCSRH_TXN_CODE = prefixArr[1].ToString();// dr["Barcode"].ToString().Trim();
                    WCSRH_NO = prefixArr[2].ToString();// dr["TransactionID"].ToString().Trim();
                    WCSRH_DT = DateTime.Parse(dr["TransactionDate"].ToString()).ToString(DateFormat).Trim();

                    WCSRH_LOCN_CODE = dr["Barcode"].ToString().Trim();
                    WCSRH_CUST_CODE = dr["CustomerCode"].ToString().Trim();
                    WCSRH_DISC_AMT = dr["Discount"].ToString().Trim();
                    WCSRH_CR_UID = dr["EmployeeCode"].ToString().Trim();
                    WCSRH_CR_DT = WCSRH_DT;


                    #endregion

                    #region CHECK UPLOADED
                    string M_RouteHistoryID = dr["RouteHistoryID"].ToString().Trim();
                    string M_TransactionTypeID = dr["TransactionTypeID"].ToString().Trim();
                    bool uploaded = true;
                    if (M_TransactionTypeID.Equals("3") || M_TransactionTypeID.Equals("4"))
                    {
                        if (M_RouteHistoryID.Trim().Equals(string.Empty))
                        {
                            uploaded = false;
                        }
                        else
                        {
                            uploaded = Convert.ToBoolean(dr["Uploaded"]);
                        }
                    }
                    else
                    {
                        uploaded = Convert.ToBoolean(dr["Uploaded"]);
                    }
                    if (uploaded)
                    {
                        WriteExceptions("HHT IS NOT DOWNLOADED SKIPPING TRANSACTION (" + InvoiceNumberComplete + ")", "TRANSACTION SKIPPED 2", false);
                        continue;
                    }
                    #endregion

                    #region sub
                    //string CHKEmployeeID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode='" + dr["EmployeeCode"].ToString().Trim() + "'", db_vms).Trim();
                    //string MaxSequence = string.Empty;
                    //field = new object();
                    //string CheckUploaded = string.Format("select top(1)uploaded,deviceserial from RouteHistory where EmployeeID=" + CHKEmployeeID + " ORDER BY RouteHistoryID Desc ");
                    //qry = new InCubeQuery(CheckUploaded, db_vms);
                    //err = qry.Execute();
                    //err = qry.FindFirst();
                    //err = qry.GetField("uploaded", ref field);
                    //string uploaded = field.ToString().Trim();
                    //err = qry.GetField("deviceserial", ref field);
                    //string deviceserial = field.ToString().Trim();
                    //if (uploaded.ToString().Trim().Equals(string.Empty) || uploaded.ToString().Trim().Equals("System.Object")) continue;
                    //if (!uploaded.ToString().Trim().Equals(string.Empty) && !uploaded.ToString().Trim().Equals("System.Object"))
                    //{
                    //    if (Convert.ToBoolean(uploaded.ToString().Trim()))
                    //    {
                    //        WriteMessage("\r\n");
                    //        WriteMessage("<<< The Route " + dr[2].ToString().Trim() + " is not downloaded . No Transactions will be Transfered .>>> Total Updated = " + TOTALUPDATED);
                    //        WriteExceptions("Route " + dr[2].ToString().Trim() + " was not downloaded ---- transaction = " + dr[1].ToString().Trim() + "", "Device not downloaded", true);
                    //        continue;
                    //    }

                    //}
                    //WriteExceptions("Route " + dr[2].ToString().Trim() + " is downloaded ...", "Device is downloaded", false);
                    _tran = Conn.BeginTransaction();
                    OracleCommand cmdGet = new OracleCommand("select WCSRH_SYS_ID.nextval from DUAL", Conn);
                    cmdGet.Transaction = _tran;
                    string maxSysID = GetValue(cmdGet);
                    cmdGet.Dispose();

                    if (!maxSysID.ToString().Trim().Equals(string.Empty))
                    {
                        WCSRH_SYS_ID = maxSysID;
                    }
                    else { return; }

                    cmdGet = new OracleCommand("select WCSRH_NO from OW_CUST_SALE_RET_HEAD_TXN where WCSRH_NO='" + WCSRH_NO + "' and WCSRH_TXN_CODE='" + WCSRH_TXN_CODE + "'", Conn);
                    cmdGet.Transaction = _tran;
                    string ExistTransactions = GetValue(cmdGet);
                    cmdGet.Dispose();
                    if (ExistTransactions.Equals(string.Empty))
                    {
                        #region Insert the Header
                        ReportProgress("Sending Transactions");
                        string updatedDate = DateTime.Now.ToString(DateFormat);
                        string insertInvoices = string.Format(@"insert into   OW_CUST_SALE_RET_HEAD_TXN( WCSRH_SYS_ID
,WCSRH_COMP_CODE
,WCSRH_TXN_CODE
,WCSRH_NO
,WCSRH_DT
,WCSRH_LOCN_CODE
,WCSRH_CUST_CODE
,WCSRH_DISC_AMT
,WCSRH_CR_DT
,WCSRH_CR_UID
                            ) VALUES 
                             (" + WCSRH_SYS_ID +
 ",'" + WCSRH_COMP_CODE +
 "','" + WCSRH_TXN_CODE +
 "','" + WCSRH_NO +
 "','" + WCSRH_DT +
 "','" + WCSRH_LOCN_CODE +
 "','" + WCSRH_CUST_CODE +
 "'," + WCSRH_DISC_AMT +
 ",'" + WCSRH_CR_DT +
 "','" + WCSRH_CR_UID + "')");

                        OracleCommand cmdHDR = new OracleCommand(insertInvoices, Conn);
                        cmdHDR.Transaction = _tran;
                        err = ExecuteNonQuery(cmdHDR, dr["TransactionID"].ToString().Trim(), "SALES");
                        cmdHDR.Dispose();
                        #endregion
                        if (err != InCubeErrors.Success)
                        {

                        }


                        #endregion
                        #endregion
                        if (err == InCubeErrors.Success)
                        {
                            #region Invoice details

                            tranDetail = string.Format(@"select CO.Barcode,TransactionID,I.ItemCode,P.Quantity UQTY,BatchNo,PackStatusID,TD.Quantity AS QTY,Price,BasePrice,
                             Tax,BaseTax,Warehoused,ExpiryDate,Discount,ReturnReason,SalesTransactionTypeID,BaseDiscount,FOCTypeID,Sequence,
case(P.Quantity)when 1 then 1 else 2 end,IL.DESCRIPTION AS ITEMNAME,PTL.DESCRIPTION AS UOM,P.PackID from 
                                TransactionDetail TD inner join CustomerOutlet CO on CO.CustomerID=TD.CustomerID and CO.OutletID=TD.OutletID
                                inner join Pack P on TD.PackID=P.PackID 
                                inner join Item I on I.ItemID=P.ItemID
                                INNER JOIN ITEMLANGUAGE IL ON I.ITEMID=IL.ITEMID AND IL.LANGUAGEID=1
                                INNER JOIN PACKTYPELANGUAGE PTL ON PTL.PACKTYPEID=P.PACKTYPEID AND PTL.LANGUAGEID=1
                                where TransactionID='" + InvoiceNumberComplete + "'");

                            invoiceQry = new InCubeQuery(tranDetail, db_vms);
                            err = invoiceQry.Execute();
                            detailtbl = new DataTable();
                            detailtbl = invoiceQry.GetDataTable();
                            WriteExceptions("#################################################################", "TRANSACTION DETAIL", false);
                            WriteExceptions("NUMBER OF DETAILS FOR TRANSACTION", "TRANSACTION DETAIL", false);
                            WriteExceptions("NUMBER OF DETAILS FOR TRANSACTION ( " + InvoiceNumberComplete + ") ARE " + detailtbl.Rows.Count.ToString() + "", "TRANSACTION DETAIL", false);
                            if (detailtbl.Rows.Count == 0)
                            {
                                string updateSync = string.Format("update [Transaction] set Synchronized=0 where TransactionID='" + dr[1].ToString().Trim() + "'");
                                qry = new InCubeQuery(updateSync, db_vms);
                                err = qry.ExecuteNonQuery();
                                err = InCubeErrors.Error;
                            }


                            foreach (DataRow det in detailtbl.Rows)
                            {
                                detailCounter++;
                                WCSRI_SYS_ID = string.Empty;
                                cmdGet = new OracleCommand("select WCSRI_SYS_ID.NextVal from DUAL", Conn);
                                cmdGet.Transaction = _tran;
                                maxSysID = GetValue(cmdGet);
                                cmdGet.Dispose();

                                if (!maxSysID.Equals(string.Empty))
                                {
                                    WCSRI_SYS_ID = maxSysID;
                                }
                                else { return; }
                                WCSRI_DISC_AMT = dr["Discount"].ToString().Trim();
                                string PackID = det["PackID"].ToString().Trim();
                                WCSRI_WCSRH_SYS_ID = WCSRH_SYS_ID;

                                WCSRI_ITEM_CODE = det["ItemCode"].ToString().Trim();

                                WCSRI_UOM_CODE = det["UOM"].ToString().Trim();
                                if (det["SalesTransactionTypeID"].ToString().Trim().Equals("2") || det["SalesTransactionTypeID"].ToString().Trim().Equals("3") || det["SalesTransactionTypeID"].ToString().Trim().Equals("4"))
                                {
                                    WCSRI_FOC_ITEM_YN_NUM = "1";//###
                                }
                                else
                                {
                                    WCSRI_FOC_ITEM_YN_NUM = "2";
                                }
                                WCSRI_QTY = det["QTY"].ToString().Trim();
                                WCSRI_QTY_LS = "0";
                                WCSRI_RATE = det["Price"].ToString().Trim();

                                WCSRI_BACTH_NO = det["BatchNo"].ToString().Trim();

                                WCSRI_CR_UID = WCSRH_CR_UID;
                                WCSRI_CR_DT = WCSRH_CR_DT;


                                insertDetail = string.Format(@"insert into   OW_CUST_SALE_RET_ITEM_TXN (
                                         WCSRI_SYS_ID
,WCSRI_WCSRH_SYS_ID
,WCSRI_ITEM_CODE
,WCSRI_UOM_CODE
,WCSRI_FOC_ITEM_YN_NUM
,WCSRI_QTY
,WCSRI_QTY_LS
,WCSRI_RATE
,WCSRI_DISC_AMT
,WCSRI_CR_DT
,WCSRI_CR_UID
                                        ) VALUES (" + WCSRI_SYS_ID
+ " , " + WCSRI_WCSRH_SYS_ID
+ ",'" + WCSRI_ITEM_CODE
+ "','" + WCSRI_UOM_CODE
+ "'," + WCSRI_FOC_ITEM_YN_NUM
+ "," + WCSRI_QTY
+ "," + WCSRI_QTY_LS
+ "," + WCSRI_RATE
+ "," + WCSRI_DISC_AMT
+ ",'" + WCSRI_CR_DT
+ "','" + WCSRI_CR_UID + "')");


                                OracleCommand cmdDR = new OracleCommand(insertDetail, Conn);
                                cmdDR.Transaction = _tran;
                                err = ExecuteNonQuery(cmdDR, dr["TransactionID"].ToString().Trim(), "SALES");

                                string _expiryDate = DateTime.Parse("1990/01/01".ToString()).ToString(DateFormat).Trim();
                                //Batches
                                insertDetailBatch = string.Format(@"insert into   OW_CUST_SALE_RET_BATCH_TXN (
                                         WCSRB_SYS_ID
,WCSRB_WCSRH_SYS_ID
,WCSRB_WCSRI_SYS_ID
,WCSRB_BATCH_NO
,WCSRB_BATCH_EXP_DT
,WCSRB_BATCH_QTY
,WCSRB_BATCH_QTY_LS
,WCSRB_CR_DT
,WCSRB_CR_UID
                                        ) VALUES (" + WCSRI_SYS_ID
+ " , " + WCSRI_WCSRH_SYS_ID
+ " , " + WCSRI_SYS_ID
+ ",'" + WCSRI_BACTH_NO
+ "','" + _expiryDate
+ "'," + WCSRI_QTY
+ "," + WCSRI_QTY_LS
+ ",'" + WCSRI_CR_DT
+ "','" + WCSRI_CR_UID + "')");

                                OracleCommand cmdDR2 = new OracleCommand(insertDetailBatch, Conn);
                                cmdDR2.Transaction = _tran;


                                err = ExecuteNonQuery(cmdDR2, dr["TransactionID"].ToString().Trim(), "SALES");
                                if (err == InCubeErrors.Success)
                                {
                                    //                                    string updateOS_LOCN_CURR = string.Format(@"UPDATE OS_LOCN_CURR_STK SET LCS_ISSD_QTY_BU =LCS_ISSD_QTY_BU +{0}
                                    //WHERE LCS_ITEM_CODE='{1}' AND LCS_GRADE_CODE_2='G2' AND LCS_GRADE_CODE_1='A' AND LCS_LOCN_CODE='{2}' AND LCS_COMP_CODE='{3}'", INVI_QTY_BU, INVI_ITEM_CODE, INVH_DEL_LOCN_CODE, INVH_COMP_CODE);
                                    //                                    OleDbCommand UPDT = new OleDbCommand(updateOS_LOCN_CURR, Conn, _tran);
                                    //                                    err = ExecuteNonQuery(UPDT);
                                    //                                    cmdDR.Dispose();
                                    if (err != InCubeErrors.Success)
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    break;
                                }

                                cmdDR.Dispose();

                                if (err != InCubeErrors.Success)
                                {
                                    //WriteMessage("\r\n" + InvoiceNumberComplete + " - Transaction Failed");
                                }
                                //else
                                //{
                                //    string update = string.Format("update [Transaction] set Synchronized=1 where TransactionID='{0}'", InvoiceNumberComplete);
                                //    invoiceQry = new InCubeQuery(update, db_vms);
                                //    err = invoiceQry.ExecuteNonQuery();

                                //}
                            }
                            #endregion
                        }



                        if (err == InCubeErrors.Success)
                        {
                            string update = string.Format("update [Transaction] set Synchronized=1 where TransactionID='{0}'", InvoiceNumberComplete);
                            invoiceQry = new InCubeQuery(update, db_vms);
                            err = invoiceQry.ExecuteNonQuery();
                            update = string.Format("insert into HSNI_Invoice_Sync Values('{0}',1,GetDate())", InvoiceNumberComplete);
                            invoiceQry = new InCubeQuery(update, db_vms);
                            err = invoiceQry.ExecuteNonQuery();
                            WriteMessage("\r\n" + InvoiceNumberComplete + " - Transaction sent Successfully");
                            TOTALINSERTED++;
                            _tran.Commit();
                        }
                        else
                        {
                            WriteMessage("\r\n" + InvoiceNumberComplete + " - Transaction Failed");
                            _tran.Rollback();
                        }
                    }
                    else
                    {
                        WriteMessage("\r\n" + InvoiceNumberComplete + " - Transaction Failed");
                        WriteExceptions(InvoiceNumberComplete + " - Transaction exists in Orion", "error", true);
                        InsertLog(InvoiceNumberComplete, "Transaction already exists in ORION", "SALES");
                        _tran.Rollback();

                    }

                }

                WriteMessage("\r\n");
                //WriteMessage("<<< Transactions >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
                //THIS CALLS THE STORED PROCEDURE THAT FIXES THE DOCUMENT SEQUENCE .
                //err = DatabaseSpecialFunctions.RunStoredProcedure(db_vms, "spHandleDocumentSequence");

            }
            catch
            {
                if (_tran != null) _tran.Rollback();
            }
            finally
            {
                if (_tran != null) _tran.Dispose();
            }
            // SendCustomerVisits();
        }

        #endregion

        #region SendReciepts
        public override void SendReciepts()
        {
            if (Conn.State == ConnectionState.Closed)
            {
                Conn.Open();
            }
            WriteExceptions("Sending started", "Inserting PAYMENTS ", false);
            OracleTransaction _tran = null;
            try
            {
                #region Another Query

                #endregion
                //SOME TRANSACTIONS ARE WITHOUT DETAILS ==> CUSTOMER PAYMENT FOR THOSE TRANSACTIONS WILL NOT APPEAR BECAUSE OF DIVISION ID
                //TO DO : Handle the date format
                WriteExceptions("Getting Query", "Inserting PAYMENTS ", false);
                DataTable dt = new DataTable();
                string receipt = string.Format(@"select CO.CustomerCode,CP.PaymentDate,CP.CustomerPaymentID,CP.TransactionID,(case(CP.PaymentTypeID) when 1 then 'CASH' else 
case(CP.PaymentTypeID) when 2 then 'CHQ' else case(CP.PaymentTypeID) when 3 then 'CHQ' else case(CP.PaymentTypeID) when 4 then 'CC' else 'NA' end end end end ) as PaymentTypeID
,E.EmployeeCode,
                    CP.VoucherNumber,CP.VoucherDate,CP.VoucherOwner,B.Code,B.Code,CP.Notes,CP.CurrencyID,CP.AppliedAmount,CP.Synchronized,CP.GPSLatitude,CP.GPSLongitude,
                    CP.PaymentStatusID,CP.SourceTransactionID,CP.RemainingAmount,CP.Posted,isnull(CP.Downloaded,0),CP.VisitNo,CP.RouteHistoryID,CP.AppliedPaymentID,CP.AccountID,isnull(CP.BounceDate,'1990/01/01'),
                    CP.DivisionID ,E.NationalIDNumber Region,t.transactionDate,w.warehouseCode,dbo.IsRouteUploadedForRouteHistory(CP.RouteHistoryID) Uploaded
                    from CustomerPayment CP 
                    inner join CustomerOutlet CO on CO.CustomerID=CP.CustomerID and CO.OutletID=CP.OutletID 
                    inner join Employee E on CP.EmployeeID=E.EmployeeID 
					inner join [transaction] t on cp.transactionid=t.TransactionID and cp.customerid=t.CustomerID
                    left outer join Bank B on CP.BankID=B.BankID 
                    left outer join EmployeeVehicle ev on E.EmployeeID=EV.EmployeeID
                    left outer join Warehouse w on ev.VehicleID=w.WarehouseID
                    where (CP.Synchronized = 0) 
                    and CP.PaymentTypeID<>4 and CP.PaymentTypeID<>5 and CP.PaymentStatusID <>5 and CP.EmployeeID<>0 AND not exists(SELECT PAYMENTID,transactionid FROM HSNI_Payment_Sync where paymentid=cp.customerpaymentid and transactionid=cp.transactionid)
                    Group By
                    CO.CustomerCode,CP.PaymentDate,CP.CustomerPaymentID,CP.TransactionID,CP.PaymentTypeID,E.EmployeeCode,
                    CP.VoucherNumber,CP.VoucherDate,CP.VoucherOwner,B.Code,B.Code,CP.Notes,CP.CurrencyID,CP.AppliedAmount,CP.Synchronized,CP.GPSLatitude,CP.GPSLongitude,
                    CP.PaymentStatusID,CP.SourceTransactionID,CP.RemainingAmount,CP.Posted,CP.Downloaded,CP.VisitNo,CP.RouteHistoryID,CP.AppliedPaymentID,CP.AccountID,CP.BounceDate,
                    CO.CustomerTypeID,CP.DivisionID,E.NationalIDNumber,t.transactionDate,w.warehouseCode
                     
                     ");
                if (Filters.EmployeeID != -1)
                {
                    receipt += "    AND CustomerPayment.EmployeeID = " + Filters.EmployeeID;
                }
                WriteExceptions("Query Done", "Inserting PAYMENTS ", false);
                InCubeQuery receiptQry = new InCubeQuery(receipt, db_vms);
                err = receiptQry.Execute();
                dt = receiptQry.GetDataTable();
                string insertReceipt = string.Empty;
                string proddate = string.Empty;
                int FailuresCount = 0;
                int SuccessCount = 0;
                int Skipped = 0;
                ClearProgress();
                SetProgressMax(dt.Rows.Count);
                foreach (DataRow dr in dt.Rows)
                {
                    string[] docArr = new string[2];
                    docArr = dr["TransactionID"].ToString().Trim().Split('-');
                    int Inv_Parts = docArr.Length; //This check is added to support having invocies with 2 parts or 3 parts as well
                    string WICL_INV_NO = docArr[Inv_Parts - 1]; //last part in transaction ID
                    string WICL_INV_TXN_CODE = dr["EmployeeCode"].ToString().Trim(); //Was reading the part before last in transation ID but changed to employee code who made the payment
                    string WICL_SYS_ID = string.Empty;
                    string WICL_CUST_CODE = dr["CustomerCode"].ToString();
                    string WICL_INV_DT = DateTime.Parse(dr["transactionDate"].ToString().Trim()).ToString(DateFormat);
                    string WICL_LOCN_CODE = dr["warehouseCode"].ToString();
                    string WICL_COLL_TYPE = dr["PaymentTypeID"].ToString();
                    string WICL_COMP_CODE = "001";
                    string WICL_AMOUNT = dr["AppliedAmount"].ToString();
                    string WICL_INSTR_BANK = dr["Code"].ToString().Trim();
                    string WICL_CHEQUE_NO = dr["VoucherNumber"].ToString().Trim();
                    string WICL_CHEQUE_DT = dr["VoucherDate"].ToString().Trim();
                    if (!WICL_CHEQUE_DT.Equals(string.Empty)) WICL_CHEQUE_DT = DateTime.Parse(WICL_CHEQUE_DT).ToString(DateFormat);
                    string WICL_CR_DT = DateTime.Parse(dr["PaymentDate"].ToString().Trim()).ToString(DateFormat);
                    string WICL_CR_UID = dr["EmployeeCode"].ToString().Trim();

                    bool uploaded = Convert.ToBoolean(dr["Uploaded"]);
                    if (uploaded)
                    {
                        //string countSt = GetFieldValue("WarehouseTransaction", "count(*)", "WarehouseID=" + vehicleID + " and WarehouseTransactionStatusID=1", db_vms).Trim();
                        WriteExceptions(" ROUTE IS NOT DOWNLOADED, PAYMENTID " + dr["CustomerPaymentID"].ToString().Trim() + " IS SKIPPED", "Inserting PAYMENTS ", false);
                        WriteMessage("\r\n" + dr[2].ToString() + " - Receipt skipped because device still uploaded..");
                        Skipped++;
                        continue;
                    }

                    ReportProgress("Sending Receipts");
                    WriteExceptions("Sending..", "Inserting PAYMENTS ", false);
                    _tran = Conn.BeginTransaction();
                    #region Insert Cash
                    WICL_SYS_ID = string.Empty;
                    OracleCommand cmdGet = new OracleCommand("select WICL_SYS_ID.NextVal  from DUAL", Conn);
                    cmdGet.Transaction = _tran;
                    string maxSysID = GetValue(cmdGet);
                    if (!maxSysID.Equals(string.Empty))
                    {
                        WICL_SYS_ID = maxSysID;
                    }
                    else { return; }
                    WriteExceptions("insreting receipt", "Inserting PAYMENTS ", false);
                    insertReceipt = string.Format(@"INSERT INTO OW_INVAN_COLL_TXN(
                            WICL_SYS_ID
,WICL_COMP_CODE
,WICL_INV_TXN_CODE
,WICL_INV_NO
,WICL_INV_DT
,WICL_LOCN_CODE
,WICL_CUST_CODE
,WICL_COLL_TYPE
,WICL_AMOUNT
,WICL_INSTR_BANK
,WICL_CHEQUE_NO
,WICL_CHEQUE_DT
,WICL_CR_DT
,WICL_CR_UID	
                            )
                            Values(
                            " + WICL_SYS_ID
                            + ",'" + WICL_COMP_CODE
                            + "','" + WICL_INV_TXN_CODE
                            + "','" + WICL_INV_NO
                            + "','" + WICL_INV_DT
                            + "','" + WICL_LOCN_CODE
                            + "','" + WICL_CUST_CODE
                            + "','" + WICL_COLL_TYPE
                            + "'," + WICL_AMOUNT
                            + ",'" + WICL_INSTR_BANK
                            + "','" + WICL_CHEQUE_NO
                            + "','" + WICL_CHEQUE_DT
                            + "','" + WICL_CR_DT
                            + "','" + WICL_CR_UID

                            + "')");
                    OracleCommand cmdDR = new OracleCommand(insertReceipt, Conn);
                    cmdDR.Transaction = _tran;
                    err = ExecuteNonQuery(cmdDR, dr["CustomerPaymentID"].ToString().Trim(), "PAYMENT");
                    cmdDR.Dispose();
                    WriteExceptions("close connection with oracle", "Inserting PAYMENTS ", false);


                    #endregion

                    if (err != InCubeErrors.Success)
                    {
                        _tran.Rollback();
                        WriteMessage("\r\n" + dr[2].ToString() + " - Receipt Failed");
                    }
                    else
                    {
                        string update = string.Format("update CustomerPayment set Synchronized=1 where CustomerPaymentID='{0}' and TransactionID='{1}'", dr["CustomerPaymentID"].ToString().Trim(), dr["TransactionID"].ToString().Trim());
                        receiptQry = new InCubeQuery(update, db_vms);
                        err = receiptQry.ExecuteNonQuery();
                        if (err == InCubeErrors.Success)
                        {
                            update = string.Format("insert into HSNI_payment_Sync Values('{0}','{1}',1,GetDate())", dr["CustomerPaymentID"].ToString().Trim(), dr["TransactionID"].ToString().Trim());
                            receiptQry = new InCubeQuery(update, db_vms);
                            err = receiptQry.ExecuteNonQuery();
                            if (err != InCubeErrors.Success)
                            {
                                update = string.Format("update CustomerPayment set Synchronized=0 where CustomerPaymentID='{0}' and TransactionID='{1}'", dr["CustomerPaymentID"].ToString().Trim(), dr["TransactionID"].ToString().Trim());
                                receiptQry = new InCubeQuery(update, db_vms);
                                receiptQry.ExecuteNonQuery();
                            }
                        }
                        if (err != InCubeErrors.Success)
                        {
                            _tran.Rollback();
                            WriteMessage("\r\n" + dr[2].ToString() + " - Receipt Failed");
                            FailuresCount++;
                        }
                        else
                        {
                            _tran.Commit();
                            WriteMessage("\r\n" + dr[2].ToString() + " - Receipt Transfered Successfully");
                            SuccessCount++;
                        }
                    }

                }
                WriteMessage("\r\n");
                WriteMessage("<<< Receipts >>> Total Found = " + dt.Rows.Count + ", Success: " + SuccessCount + ", Failure: " + FailuresCount + ", Skipped: " + Skipped);

            }
            catch
            {
                WriteMessage("<<< Receipts >>> " + err.ToString());
                if (_tran != null) _tran.Rollback();

            }
            finally
            { if (_tran != null) _tran.Dispose(); }
        }
        #endregion

        #region SendTransfers
        public override void SendTransfers()
        {
            if (Conn.State == ConnectionState.Closed)
            {
                Conn.Open();
            }

            OracleTransaction _tran = null;

            try
            {

                //TO DO : Handle the date format
                DataTable dt = new DataTable();
                string transfers = string.Format(@"
 Select WH.Barcode,WHT.TransactionID,TransactionTypeID,TransactionDate ,EE.EmployeeCode,E.EmployeeCode,
'notes',Synchronized,isnull(WHT.ProductionDate,getdate()),WHH.Barcode AS FromBarcode,Posted,Downloaded,WarehouseTransactionStatusID,CreationSourceID,DivisionID,
(CASE (TransactionTypeID) WHEN 1 THEN 'REQUEST' ELSE CASE (TransactionTypeID) WHEN 2 THEN 'RETURN' ELSE '' END END) AS WRH_TYPE
From WarehouseTransaction WHT inner join Warehouse WH on WHT.WarehouseID=WH.WarehouseID 
inner join Warehouse WHH on WHT.RefWarehouseID=WHH.WarehouseID
inner join Employee E on WHT.ImplementedBy=E.EmployeeID 
inner join Employee EE on WHT.RequestedBy=EE.EmployeeID
where  (WHT.TransactionTypeID=1 or WHT.TransactionTypeID=2) and (WarehouseTransactionStatusID IN (1,2)) and WHT.TransactionID not in (select TransactionID from WH_Sync) 
Group By
WH.Barcode,WHT.TransactionID,TransactionTypeID,TransactionDate,EE.EmployeeCode,E.EmployeeCode,
Synchronized,WHT.ProductionDate,WHH.Barcode,Posted,Downloaded,WarehouseTransactionStatusID,CreationSourceID,DivisionID 
");
                InCubeQuery transferQry = new InCubeQuery(transfers, db_vms);
                err = transferQry.Execute();
                dt = transferQry.GetDataTable();
                DataTable detailDT = new DataTable();
                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;
                ClearProgress();
                SetProgressMax(dt.Rows.Count);

                foreach (DataRow dr in dt.Rows)
                {
                    #region HEADER

                    object field = new object();

                    ReportProgress("Sending Transfers");

                    string WRH_SYS_ID = string.Empty;
                    string WRH_COMP_CODE = string.Empty;
                    string WRH_TXN_CODE = string.Empty;
                    string WRH_NO = string.Empty;
                    string WRH_DT = string.Empty;
                    string WRH_LOCN_CODE = string.Empty;
                    string WRH_CR_DT = string.Empty;
                    string WRH_CR_UID = string.Empty;
                    string WRH_TYPE = string.Empty;

                    WRH_CR_UID = dr["EmployeeCode"].ToString().Trim();
                    WRH_CR_DT = DateTime.Now.ToString(DateFormat).Trim();
                    int TransStatusID = int.Parse(dr["WarehouseTransactionStatusID"].ToString().Trim());
                    string DivisionID = dr["DivisionID"].ToString().Trim();
                    if ((DivisionID.Equals("1") || (DivisionID.Equals("3"))) && TransStatusID > 2)
                    {
                        continue;
                    }
                    else if (DivisionID.Equals("2") && TransStatusID < 4)
                    {
                        continue;
                    }
                    string CHKEmployeeID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode='" + dr["EmployeeCode"].ToString().Trim() + "'", db_vms).Trim();
                    string MaxSequence = string.Empty;
                    field = new object();
                    string CheckUploaded = string.Format("select top(1)uploaded,deviceserial from RouteHistory where EmployeeID=" + CHKEmployeeID + " ORDER BY RouteHistoryID Desc ");
                    qry = new InCubeQuery(CheckUploaded, db_vms);
                    err = qry.Execute();
                    err = qry.FindFirst();
                    err = qry.GetField("uploaded", ref field);
                    string uploaded = field.ToString().Trim();
                    err = qry.GetField("deviceserial", ref field);
                    string deviceserial = field.ToString().Trim();
                    //if (uploaded.ToString().Trim().Equals(string.Empty) || uploaded.ToString().Trim().Equals("System.Object")) continue;
                    //if (!uploaded.ToString().Trim().Equals(string.Empty) && !uploaded.ToString().Trim().Equals("System.Object"))
                    //{
                    //    if (Convert.ToBoolean(uploaded.ToString().Trim()))
                    //    {
                    //        WriteMessage("\r\n");
                    //        //WriteMessage("<<< The Route " + dr[2].ToString().Trim() + " is not downloaded . No Transactions will be Transfered .>>> Total Updated = " + TOTALUPDATED);
                    //        //WriteExceptions("Route " + dr[2].ToString().Trim() + " was not downloaded ---- transaction = " + dr[1].ToString().Trim() + "", "Device not downloaded", true);
                    //        //continue;
                    //    }

                    //}


                    _tran = Conn.BeginTransaction();
                    OracleCommand cmdGet = new OracleCommand("select WRH_SYS_ID.nextval from DUAL", Conn);
                    cmdGet.Transaction = _tran;
                    string maxSysID = GetValue(cmdGet);
                    cmdGet.Dispose();

                    if (!maxSysID.ToString().Trim().Equals(string.Empty))
                    {
                        WRH_SYS_ID = maxSysID;
                    }
                    else { return; }

                    string[] prefixArr = new string[2];
                    prefixArr = dr["TransactionID"].ToString().Trim().Split('-');
                    string InvoiceNumberComplete = dr["TransactionID"].ToString().Trim();

                    WRH_COMP_CODE = "001";
                    //if (dr["TransactionTypeID"].ToString().Trim().Equals("1"))
                    //{
                    //    RH_TXN_CODE = "PMRQ";
                    //}
                    //else if (dr["TransactionTypeID"].ToString().Trim().Equals("2"))
                    //{
                    //    RH_TXN_CODE = "PMRQ";
                    //}

                    WRH_NO = prefixArr[2].ToString();
                    WRH_TXN_CODE = prefixArr[1].ToString();
                    WRH_DT = DateTime.Parse(dr["TransactionDate"].ToString()).ToString(DateFormat).Trim();
                    WRH_LOCN_CODE = dr["Barcode"].ToString().Trim();
                    WRH_TYPE = dr["WRH_TYPE"].ToString().Trim();
                    cmdGet = new OracleCommand("select RH_NO from OW_REQ_HEAD_TXN where INVH_NO='" + WRH_NO + "' and INVH_TXN_CODE='" + WRH_TXN_CODE + "'", Conn);
                    cmdGet.Transaction = _tran;
                    string ExistTransactions = GetValue(cmdGet);
                    cmdGet.Dispose();
                    if (ExistTransactions.Equals(string.Empty))
                    {
                        //ReportProgress("Sending Transactions");
                        string updatedDate = DateTime.Now.ToString(DateFormat);
                        string insertLR = string.Format(@"insert into OW_REQ_HEAD_TXN (WRH_SYS_ID
,WRH_COMP_CODE
,WRH_TXN_CODE
,WRH_NO
,WRH_DT
,WRH_LOCN_CODE
,WRH_CR_DT
,WRH_CR_UID
)
VALUES
(" + WRH_SYS_ID
+ " ,'" + WRH_COMP_CODE
+ "' ,'" + WRH_TXN_CODE
+ "'," + WRH_NO
+ " ,'" + WRH_DT
+ "','" + WRH_LOCN_CODE
+ "' ,'" + WRH_CR_DT
+ "' ,'" + WRH_CR_UID
+ "')");

                        OracleCommand cmdHDR = new OracleCommand(insertLR, Conn);
                        cmdHDR.Transaction = _tran;
                        err = ExecuteNonQuery(cmdHDR, dr["TransactionID"].ToString().Trim(), "WH");
                        cmdHDR.Dispose();
                        #endregion

                        if (err == InCubeErrors.Success)
                        {
                            #region DETAILS
                            string transfDet = string.Format(@"select WH.Barcode,TransactionID,ZoneID,I.ItemCode,P.Quantity as QTYLS,ExpiryDate,WHT.Quantity as TranQty,Balanced,BatchNo,
                    ProductionDate, (case(P.Quantity) when 1 then 1 else 2 end),DivisionID,PackStatusID,PTL.Description as UOMCode,IL.Description as ItemName
from WhTransDetail WHT 
inner join Warehouse WH on WH.WarehouseID=WHT.WarehouseID 
inner join Pack P on WHT.PackID=P.PackID 
inner join Item I on I.ItemID=P.ItemID
inner join ItemLanguage IL on I.ItemID=IL.ItemID and IL.languageID=1
inner join PackTypeLanguage PTL on P.PackTypeID=PTL.PackTypeID and PTL.LanguageID=1
where TransactionID='" + InvoiceNumberComplete + "'");
                            transferQry = new InCubeQuery(transfDet, db_vms);
                            err = transferQry.Execute();
                            detailDT = transferQry.GetDataTable();
                            string proddate = string.Empty;
                            string expi = string.Empty;
                            WriteExceptions("#################################################################", "WAREHOUSE TRANSACTION DETAIL", false);
                            WriteExceptions("NUMBER OF DETAILS FOR WAREHOUSE TRANSACTION IS " + detailDT.Rows.Count.ToString() + "", "WAREHOUSE TRANSACTION DETAIL", false);
                            WriteExceptions("NUMBER OF DETAILS FOR TRANSACTION ( " + dr[1].ToString() + ") ARE " + detailDT.Rows.Count.ToString() + "", "WAREHOUSE TRANSACTION DETAIL", false);
                            if (detailDT.Rows.Count == 0)
                            {

                                string updateSync = string.Format("update WarehouseTransaction set Synchronized=0 where TransactionID='" + InvoiceNumberComplete + "'");
                                qry = new InCubeQuery(updateSync, db_vms);
                                err = qry.ExecuteNonQuery();
                                err = InCubeErrors.Error;
                            }
                            foreach (DataRow det in detailDT.Rows)
                            {
                                if (det[9].ToString().Equals(string.Empty))
                                {
                                    proddate = DateTime.Today.ToString();
                                }
                                else
                                {
                                    proddate = det[9].ToString();
                                }
                                if (det[5].ToString().Equals(string.Empty))
                                {
                                    expi = DateTime.Today.AddYears(1).ToString();
                                }
                                else
                                {
                                    expi = det[5].ToString();
                                }
                                string WRI_SYS_ID = string.Empty;
                                cmdGet = new OracleCommand("select WRI_SYS_ID.NextVal from DUAL", Conn);
                                cmdGet.Transaction = _tran;
                                maxSysID = GetValue(cmdGet);
                                cmdGet.Dispose();

                                if (!maxSysID.Equals(string.Empty))
                                {
                                    WRI_SYS_ID = maxSysID;
                                }
                                else { return; }


                                string WRI_WRH_SYS_ID = string.Empty;
                                string WRI_ITEM_CODE = string.Empty;
                                string WRI_UOM_CODE = string.Empty;
                                string WRI_QTY = string.Empty;
                                string WRI_QTY_LS = string.Empty;
                                string WRI_APP_QTY = string.Empty;
                                string WRI_APP_QTY_LS = string.Empty;
                                string WRI_CR_DT = string.Empty;
                                string WRI_CR_UID = string.Empty;

                                WRI_WRH_SYS_ID = WRH_SYS_ID;
                                WRI_ITEM_CODE = det["ItemCode"].ToString();
                                WRI_UOM_CODE = det["UOMCode"].ToString();
                                WRI_QTY = det["TranQty"].ToString();
                                WRI_QTY_LS = "0";// det["QTYLS"].ToString();
                                WRI_APP_QTY = det["TranQty"].ToString();
                                WRI_APP_QTY_LS = det["TranQty"].ToString();
                                WRI_CR_UID = WRH_CR_UID;
                                WRI_CR_DT = WRH_CR_DT;

                                string insertdetailWH = string.Format(@"insert into OW_REQ_ITEM_TXN( WRI_SYS_ID
,WRI_WRH_SYS_ID
,WRI_ITEM_CODE
,WRI_UOM_CODE
,WRI_QTY
,WRI_QTY_LS
,WRI_APP_QTY
,WRI_APP_QTY_LS
,WRI_CR_DT
,WRI_CR_UID)
values
(" + WRI_SYS_ID
+ "," + WRI_WRH_SYS_ID
+ ",'" + WRI_ITEM_CODE
+ "','" + WRI_UOM_CODE
+ "'," + WRI_QTY
+ "," + WRI_QTY_LS
+ "," + WRI_APP_QTY
+ "," + WRI_APP_QTY_LS
+ ",'" + WRI_CR_DT
+ "','" + WRI_CR_UID + "')");

                                OracleCommand cmdDR = new OracleCommand(insertdetailWH, Conn);
                                cmdDR.Transaction = _tran;
                                err = ExecuteNonQuery(cmdDR, dr["TransactionID"].ToString().Trim(), "WH");
                                cmdDR.Dispose();
                            }
                            #endregion
                        }
                        if (err == InCubeErrors.Success)
                        {
                            string update = string.Format("INSERT INTO WH_Sync (TRANSACTIONID,StatusID) VALUES('{0}',1)", InvoiceNumberComplete); //string.Format("update [WarehouseTransaction] set Synchronized=1 where TransactionID='{0}'", InvoiceNumberComplete);
                            qry = new InCubeQuery(update, db_vms);
                            err = qry.ExecuteNonQuery();
                            WriteMessage("\r\n" + InvoiceNumberComplete + " - Transaction sent Successfully");
                            TOTALINSERTED++;
                            _tran.Commit();
                        }
                        else
                        {
                            WriteMessage("\r\n" + InvoiceNumberComplete + " - Transaction Failed");
                            _tran.Rollback();
                        }
                    }
                    else
                    {
                        WriteMessage("\r\n" + InvoiceNumberComplete + " - Transaction Failed");
                        WriteExceptions(InvoiceNumberComplete + " - Transaction exists in Orion", "error", true);
                        _tran.Rollback();

                    }
                }

                WriteMessage("\r\n");
                WriteMessage("<<< Transfers >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);

            }
            catch
            {
                if (_tran != null) _tran.Rollback();
            }
            finally
            {
                if (_tran != null) _tran.Dispose();
            }
        }
        #endregion

        public override void SendOrders()
        {
            if (Conn.State == ConnectionState.Closed)
            {
                Conn.Open();
            }

            OracleTransaction _tran = null;


            try
            {
                //TO DO : Handle the date format
                //SALES MODE : 1=CASH , 2=CREDIT, 3=TEMPORARY CREDIT
                DataTable dt;
                bool AllSalespersons = true;
                string Salesperson = string.Empty;
                string sp = string.Empty;
                if (!AllSalespersons)
                {
                    sp = " AND T.EmployeeID = " + Salesperson;
                }
                string invoices = string.Format(@"select SO.ORDERID,'001' WRH_COMP_CODE,SO.ORDERDATE WSO_DT,W.WAREHOUSECODE WSO_LOCN_CODE,SO.ORDERDATE WSO_CR_DT,E.EMPLOYEECODE WSO_SM_CODE,E.EMPLOYEECODE WSO_CR_UID,
CO.CUSTOMERCODE WSO_CUST_CODE,(SO.Discount+SO.PromotedDiscount) WSO_DISC_AMT,SO.DesiredDeliveryDate WSO_DEL_DT
FROM SALESORDER SO 
INNER JOIN EMPLOYEEVEHICLE EV ON SO.EmployeeID=EV.EMPLOYEEID 
INNER JOIN WAREHOUSE W ON EV.VEHICLEID=W.WAREHOUSEID
INNER JOIN EMPLOYEE E ON SO.EMPLOYEEID=E.EMPLOYEEID
INNER JOIN CUSTOMEROUTLET CO ON SO.CUSTOMERID=CO.CUSTOMERID AND SO.OUTLETID=CO.OUTLETID
WHERE SO.Synchronized=0 AND SO.OrderStatusID IN (2,3,4)

                ", sp);
                //the query above used to have date range : AND 
                //(T.TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
                //AND T.TransactionDate <= '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"')
                #region Invoice Header

                #region Variables
                InCubeQuery invoiceQry = new InCubeQuery(invoices, db_vms);
                err = invoiceQry.Execute();
                dt = new DataTable();
                dt = invoiceQry.GetDataTable();
                string tranDetail = string.Empty;
                DataTable detailtbl;
                string insertDetail = string.Empty;
                int TOTALINSERTED = 0;
                ClearProgress();
                SetProgressMax(dt.Rows.Count);
                string WSO_SYS_ID = string.Empty;
                string WSO_COMP_CODE = string.Empty;
                string WSO_TXN_CODE = string.Empty;
                string WSO_NO = string.Empty;
                string WSO_DT = string.Empty;
                string WSO_LOCN_CODE = string.Empty;
                string WSO_CUST_CODE = string.Empty;
                string WSO_DISC_AMT = string.Empty;
                string WSO_CR_DT = string.Empty;
                string WSO_CR_UID = string.Empty;
                string WSO_SM_CODE = string.Empty;
                string WSO_DEL_DT = string.Empty;

                string WSOI_SYS_ID = string.Empty;
                string WSOI_WSO_SYS_ID = string.Empty;
                string WSOI_ITEM_CODE = string.Empty;
                string WSOI_UOM_CODE = string.Empty;

                string WSOI_QTY = string.Empty;
                string WSOI_QTY_LS = string.Empty;
                string WSOI_RATE = string.Empty;
                string WSOI_DISC_AMT = string.Empty;
                string WSOI_CR_DT = string.Empty;
                string WSOI_CR_UID = string.Empty;
                string InvoiceNumberComplete = string.Empty;

                int detailCounter = 0;
                #endregion
                foreach (DataRow dr in dt.Rows)
                {

                    #region Fill Header Variables
                    detailCounter = 0;
                    _tran = Conn.BeginTransaction();
                    object field = new object();

                    OracleCommand cmdGet = new OracleCommand("select WSO_SYS_ID.nextval from DUAL", Conn);
                    cmdGet.Transaction = _tran;
                    string maxSysID = GetValue(cmdGet);
                    cmdGet.Dispose();

                    if (!maxSysID.ToString().Trim().Equals(string.Empty))
                    {
                        WSO_SYS_ID = maxSysID;
                    }
                    else { return; }
                    // don't forget to add the brefix from document sequence----> NO i decided to take it from the transaction .
                    //string prefix = GetFieldValue("DocumentSequence", "MaxTransactionInvoiceID", "EmployeeID in (select EmployeeID from Employee where EmployeeCode='" + INVH_SM_CODE + "'", db_vms).Trim();
                    string[] prefixArr = new string[2];
                    prefixArr = dr["ORDERID"].ToString().Trim().Split('-');
                    InvoiceNumberComplete = dr["ORDERID"].ToString().Trim();
                    WSO_COMP_CODE = "001";
                    WSO_TXN_CODE = prefixArr[1].ToString();// dr["Barcode"].ToString().Trim();
                    WSO_NO = prefixArr[2].ToString();// dr["TransactionID"].ToString().Trim();
                    WSO_DT = DateTime.Parse(dr["WSO_DT"].ToString()).ToString(DateFormat).Trim();

                    WSO_LOCN_CODE = dr["WSO_LOCN_CODE"].ToString().Trim();
                    WSO_CUST_CODE = dr["WSO_CUST_CODE"].ToString().Trim();
                    WSO_DISC_AMT = dr["WSO_DISC_AMT"].ToString().Trim();
                    WSO_CR_UID = dr["WSO_CR_UID"].ToString().Trim();
                    WSO_CR_DT = WSO_DT;
                    WSO_SM_CODE = dr["WSO_SM_CODE"].ToString().Trim();
                    WSO_DEL_DT = DateTime.Parse(dr["WSO_DEL_DT"].ToString()).ToString(DateFormat).Trim();

                    #endregion


                    #region sub
                    //string CHKEmployeeID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode='" + dr["WSO_SM_CODE"].ToString().Trim() + "'", db_vms).Trim();
                    //string MaxSequence = string.Empty;
                    //field = new object();
                    //string CheckUploaded = string.Format("select top(1)uploaded,deviceserial from RouteHistory where EmployeeID=" + CHKEmployeeID + " ORDER BY RouteHistoryID Desc ");
                    //qry = new InCubeQuery(CheckUploaded, db_vms);
                    //err = qry.Execute();
                    //err = qry.FindFirst();
                    //err = qry.GetField("uploaded", ref field);
                    //string uploaded = field.ToString().Trim();
                    //err = qry.GetField("deviceserial", ref field);
                    //string deviceserial = field.ToString().Trim();
                    //if (uploaded.ToString().Trim().Equals(string.Empty) || uploaded.ToString().Trim().Equals("System.Object")) continue;
                    //if (!uploaded.ToString().Trim().Equals(string.Empty) && !uploaded.ToString().Trim().Equals("System.Object"))
                    //{
                    //    if (Convert.ToBoolean(uploaded.ToString().Trim()))
                    //    {
                    //        WriteMessage("\r\n");
                    //        WriteMessage("<<< The Route " + dr[2].ToString().Trim() + " is not downloaded . No Transactions will be Transfered .>>> Total Updated = " + TOTALUPDATED);
                    //        WriteExceptions("Route " + dr[2].ToString().Trim() + " was not downloaded ---- transaction = " + dr[1].ToString().Trim() + "", "Device not downloaded", true);
                    //        continue;
                    //    }

                    //}
                    //WriteExceptions("Route " + dr[2].ToString().Trim() + " is downloaded ...", "Device is downloaded", false);


                    cmdGet = new OracleCommand("select WSO_NO from OW_SO_HEAD_TXN where WSO_NO='" + WSO_NO + "' and WSO_TXN_CODE='" + WSO_TXN_CODE + "'", Conn);
                    cmdGet.Transaction = _tran;
                    string ExistTransactions = GetValue(cmdGet);
                    cmdGet.Dispose();
                    if (ExistTransactions.Equals(string.Empty))
                    {
                        #region Insert the Header
                        ReportProgress("Sending Transactions");
                        string updatedDate = DateTime.Now.ToString(DateFormat);
                        string insertInvoices = string.Format(@"insert into   OW_SO_HEAD_TXN( WSO_SYS_ID
,WSO_COMP_CODE
,WSO_TXN_CODE
,WSO_NO
,WSO_DT
,WSO_LOCN_CODE
,WSO_CUST_CODE
,WSO_DISC_AMT
,WSO_CR_DT
,WSO_CR_UID
,WSO_SM_CODE
,WSO_DEL_DT
                            ) VALUES 
                             (" + WSO_SYS_ID +
 ",'" + WSO_COMP_CODE +
 "','" + WSO_TXN_CODE +
 "','" + WSO_NO +
 "','" + WSO_DT +
 "','" + WSO_LOCN_CODE +
 "','" + WSO_CUST_CODE +
 "'," + WSO_DISC_AMT +
 ",'" + WSO_CR_DT +
 "','" + WSO_CR_UID + "','" + WSO_SM_CODE + "','" + WSO_DEL_DT + "')");

                        OracleCommand cmdHDR = new OracleCommand(insertInvoices, Conn);
                        cmdHDR.Transaction = _tran;
                        err = ExecuteNonQuery(cmdHDR, dr["ORDERID"].ToString().Trim(), "ORDERS");
                        cmdHDR.Dispose();
                        #endregion
                        if (err != InCubeErrors.Success)
                        {

                        }


                        #endregion
                        #endregion
                        if (err == InCubeErrors.Success)
                        {
                            #region Invoice details

                            tranDetail = string.Format(@"select CO.CUSTOMERCODE,ORDERID,I.ItemCode WSOI_ITEM_CODE,P.Quantity UQTY,TD.Quantity AS WSOI_QTY,Price WSOI_RATE,BasePrice,
                             Tax,BaseTax,Discount WSOI_DISC_AMT,SalesTransactionTypeID,BaseDiscount,
case(P.Quantity)when 1 then 1 else 2 end,IL.DESCRIPTION AS ITEMNAME,PTL.DESCRIPTION AS WSOI_UOM_CODE,P.PackID,'0' WSOI_QTY_LS from 
                                SALESORDERDETAIL TD inner join CustomerOutlet CO on CO.CustomerID=TD.CustomerID and CO.OutletID=TD.OutletID
                                inner join Pack P on TD.PackID=P.PackID 
                                inner join Item I on I.ItemID=P.ItemID
                                INNER JOIN ITEMLANGUAGE IL ON I.ITEMID=IL.ITEMID AND IL.LANGUAGEID=1
                                INNER JOIN PACKTYPELANGUAGE PTL ON PTL.PACKTYPEID=P.PACKTYPEID AND PTL.LANGUAGEID=1
                                where ORDERID='" + InvoiceNumberComplete + "'");

                            invoiceQry = new InCubeQuery(tranDetail, db_vms);
                            err = invoiceQry.Execute();
                            detailtbl = new DataTable();
                            detailtbl = invoiceQry.GetDataTable();
                            WriteExceptions("#################################################################", "TRANSACTION DETAIL", false);
                            WriteExceptions("NUMBER OF DETAILS FOR TRANSACTION", "TRANSACTION DETAIL", false);
                            WriteExceptions("NUMBER OF DETAILS FOR TRANSACTION ( " + InvoiceNumberComplete + ") ARE " + detailtbl.Rows.Count.ToString() + "", "TRANSACTION DETAIL", false);
                            if (detailtbl.Rows.Count == 0)
                            {
                                string updateSync = string.Format("update [SALESORDER] set Synchronized=0 where ORDERID='" + dr["ORDERID"].ToString().Trim() + "'");
                                qry = new InCubeQuery(updateSync, db_vms);
                                err = qry.ExecuteNonQuery();
                                err = InCubeErrors.Error;
                            }


                            foreach (DataRow det in detailtbl.Rows)
                            {
                                detailCounter++;
                                WSOI_SYS_ID = string.Empty;
                                cmdGet = new OracleCommand("select WSOI_SYS_ID.NextVal from DUAL", Conn);
                                cmdGet.Transaction = _tran;
                                maxSysID = GetValue(cmdGet);
                                cmdGet.Dispose();

                                if (!maxSysID.Equals(string.Empty))
                                {
                                    WSOI_SYS_ID = maxSysID;
                                }
                                else { return; }
                                WSOI_DISC_AMT = det["WSOI_DISC_AMT"].ToString().Trim();
                                string PackID = det["PackID"].ToString().Trim();

                                WSOI_WSO_SYS_ID = WSO_SYS_ID;

                                WSOI_ITEM_CODE = det["WSOI_ITEM_CODE"].ToString().Trim();

                                WSOI_UOM_CODE = det["WSOI_UOM_CODE"].ToString().Trim();

                                WSOI_QTY = det["WSOI_QTY"].ToString().Trim();
                                WSOI_QTY_LS = "0";
                                WSOI_RATE = det["WSOI_RATE"].ToString().Trim();



                                WSOI_CR_UID = WSO_CR_UID;
                                WSOI_CR_DT = WSO_CR_DT;


                                insertDetail = string.Format(@"insert into   OW_SO_ITEM_TXN (
                                         WSOI_SYS_ID
,WSOI_WSO_SYS_ID
,WSOI_ITEM_CODE
,WSOI_UOM_CODE
,WSOI_QTY
,WSOI_QTY_LS
,WSOI_RATE
,WSOI_DISC_AMT
,WSOI_CR_DT
,WSOI_CR_UID
                                        ) VALUES (" + WSOI_SYS_ID
+ " , " + WSOI_WSO_SYS_ID
+ ",'" + WSOI_ITEM_CODE
+ "','" + WSOI_UOM_CODE
+ "'," + WSOI_QTY
+ "," + WSOI_QTY_LS
+ "," + WSOI_RATE
+ "," + WSOI_DISC_AMT
+ ",'" + WSOI_CR_DT
+ "','" + WSOI_CR_UID + "')");


                                OracleCommand cmdDR = new OracleCommand(insertDetail, Conn);
                                cmdDR.Transaction = _tran;
                                err = ExecuteNonQuery(cmdDR, dr["ORDERID"].ToString().Trim(), "ORDERS");
                                if (err == InCubeErrors.Success)
                                {
                                    //                                    string updateOS_LOCN_CURR = string.Format(@"UPDATE OS_LOCN_CURR_STK SET LCS_ISSD_QTY_BU =LCS_ISSD_QTY_BU +{0}
                                    //WHERE LCS_ITEM_CODE='{1}' AND LCS_GRADE_CODE_2='G2' AND LCS_GRADE_CODE_1='A' AND LCS_LOCN_CODE='{2}' AND LCS_COMP_CODE='{3}'", INVI_QTY_BU, INVI_ITEM_CODE, INVH_DEL_LOCN_CODE, INVH_COMP_CODE);
                                    //                                    OleDbCommand UPDT = new OleDbCommand(updateOS_LOCN_CURR, Conn, _tran);
                                    //                                    err = ExecuteNonQuery(UPDT);
                                    //                                    cmdDR.Dispose();
                                    if (err != InCubeErrors.Success)
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    break;
                                }

                                cmdDR.Dispose();

                                if (err != InCubeErrors.Success)
                                {
                                    //WriteMessage("\r\n" + InvoiceNumberComplete + " - Transaction Failed");
                                }
                                //else
                                //{
                                //    string update = string.Format("update [Transaction] set Synchronized=1 where TransactionID='{0}'", InvoiceNumberComplete);
                                //    invoiceQry = new InCubeQuery(update, db_vms);
                                //    err = invoiceQry.ExecuteNonQuery();

                                //}
                            }
                            #endregion
                        }



                        if (err == InCubeErrors.Success)
                        {
                            string update = string.Format("update [SALESORDER] set Synchronized=1 where ORDERID='{0}'", InvoiceNumberComplete);
                            invoiceQry = new InCubeQuery(update, db_vms);
                            err = invoiceQry.ExecuteNonQuery();
                            WriteMessage("\r\n" + InvoiceNumberComplete + " - ORDER sent Successfully");
                            TOTALINSERTED++;
                            _tran.Commit();
                        }
                        else
                        {
                            WriteMessage("\r\n" + InvoiceNumberComplete + " - ORDER Failed");
                            _tran.Rollback();
                        }
                    }
                    else
                    {
                        WriteMessage("\r\n" + InvoiceNumberComplete + " - Transaction Failed");
                        WriteExceptions(InvoiceNumberComplete + " - Transaction exists in Orion", "error", true);
                        InsertLog(InvoiceNumberComplete, "Transaction already exists in ORION", "SALES");
                        _tran.Rollback();

                    }

                }

                WriteMessage("\r\n");
                //WriteMessage("<<< Transactions >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
                //THIS CALLS THE STORED PROCEDURE THAT FIXES THE DOCUMENT SEQUENCE .
                //err = DatabaseSpecialFunctions.RunStoredProcedure(db_vms, "spHandleDocumentSequence");

            }
            catch
            {
                if (_tran != null) _tran.Rollback();
            }
            finally
            {
                if (_tran != null) _tran.Dispose();
            }
        }

        private void SendCustomerVisits()
        {
            if (Conn.State == ConnectionState.Closed)
            {
                Conn.Open();
            }

            OracleTransaction _tran = null;

            try
            {
                DataTable tbl = new DataTable();
                string GetVisits = string.Format(@"select RH.CustomerID,RH.OutletID,RH.RouteHistoryID,RH.VisitNo, RH.RouteHistoryID HistoryID,co.customerCode CUST_CODE,E.EmployeeCode SP_CODE,'001' COMPANY_CODE,VSL.Description VISIT_NOTES,CTL.Description CUST_TYPE,
EL.DESCRIPTION 'OWNER','REGULAR VISIT' PURPOSE_VISIT,'' PROJECT_REF,RH.ACTUALSTART TM,RH.ACTUALSTART DT,'' QUOTE_NO,'' LOCATION,'' CONTACT,EL.DESCRIPTION SALESPERSON,
COL.DESCRIPTION COMPANY_NAME,RH.VisitNo VISIT_NO
FROM RouteHistoryDetail_LOG RH 
INNER JOIN ROUTEHISTORY R ON RH.ROUTEHISTORYID=R.RouteHistoryID
INNER JOIN CUSTOMEROUTLET CO ON CO.CUSTOMERID=RH.CUSTOMERID AND RH.OUTLETID=CO.OUTLETID
INNER JOIN EMPLOYEE E ON R.EMPLOYEEID=E.EMPLOYEEID
INNER JOIN EMPLOYEELANGUAGE EL ON EL.EMPLOYEEID=E.EMPLOYEEID AND EL.LANGUAGEID=1
INNER JOIN CUSTOMEROUTLETLANGUAGE COL ON CO.CUSTOMERID=COL.CUSTOMERID AND COL.OUTLETID=CO.OUTLETID AND COL.LANGUAGEID=1
INNER JOIN CUSTOMERTYPELANGUAGE CTL ON CO.CustomerTypeID=CTL.CustomerTypeID AND CTL.LANGUAGEID=1
INNER JOIN VisitStatusLanguage VSL ON RH.VisitStatusID=VSL.VisitStatusID AND VSL.LANGUAGEID=1
WHERE FLAG='Y' AND RH.ActualStart IS NOT NULL AND RH.ActualEnd IS NOT NULL");
                InCubeQuery qry = new InCubeQuery(GetVisits, db_vms);
                qry.Execute();
                tbl = qry.GetDataTable();
                ClearProgress();
                SetProgressMax(tbl.Rows.Count);
                foreach (DataRow dr in tbl.Rows)
                {
                    ReportProgress("Sending Visit History");

                    string VISIT_NO = string.Empty;
                    string COMPANY_NAME = string.Empty;
                    string SALESPERSON = string.Empty;
                    string CONTACT = string.Empty;
                    string LOCATION = string.Empty;
                    string QUOTE_NO = string.Empty;
                    string DT = string.Empty;
                    string TM = string.Empty;
                    string PROJECT_REF = string.Empty;
                    string PURPOSE_VISIT = string.Empty;
                    string OWNER = string.Empty;
                    string CUST_TYPE = string.Empty;
                    string VISIT_NOTES = string.Empty;
                    string COMPANY_CODE = string.Empty;
                    string SP_CODE = string.Empty;
                    string CUST_CODE = string.Empty;

                    VISIT_NO = dr["VISIT_NO"].ToString().Trim();
                    COMPANY_NAME = dr["COMPANY_NAME"].ToString().Trim();
                    SALESPERSON = dr["SALESPERSON"].ToString().Trim();
                    CONTACT = dr["CONTACT"].ToString().Trim();
                    LOCATION = dr["LOCATION"].ToString().Trim();
                    QUOTE_NO = dr["QUOTE_NO"].ToString().Trim();
                    DT = DateTime.Parse(dr["DT"].ToString().Trim()).ToString(DateFormat).Trim();
                    TM = DateTime.Parse(dr["TM"].ToString().Trim()).Hour.ToString() + ":" + DateTime.Parse(dr["TM"].ToString().Trim()).Minute.ToString() + ":" + DateTime.Parse(dr["TM"].ToString().Trim()).Second.ToString();
                    PROJECT_REF = dr["PROJECT_REF"].ToString().Trim();
                    PURPOSE_VISIT = dr["PURPOSE_VISIT"].ToString().Trim();
                    OWNER = dr["OWNER"].ToString().Trim();
                    CUST_TYPE = dr["CUST_TYPE"].ToString().Trim();
                    VISIT_NOTES = dr["VISIT_NOTES"].ToString().Trim();
                    COMPANY_CODE = dr["COMPANY_CODE"].ToString().Trim();
                    SP_CODE = dr["SP_CODE"].ToString().Trim();
                    CUST_CODE = dr["CUST_CODE"].ToString().Trim();
                    string HistoryID = dr["HistoryID"].ToString().Trim();
                    string CustomerID = dr["CustomerID"].ToString().Trim();
                    string OutletID = dr["OutletID"].ToString().Trim();
                    string RouteHistoryID = dr["RouteHistoryID"].ToString().Trim();
                    string VisitNo = dr["VisitNo"].ToString().Trim();

                    _tran = Conn.BeginTransaction();
                    OracleCommand cmdGet = new OracleCommand("select CUST_CODE from vvs_daily_cust_visit where HistoryID='" + HistoryID + "' and CUST_CODE='" + CUST_CODE + "' and VISIT_NO=" + VISIT_NO + "", Conn);
                    cmdGet.Transaction = _tran;
                    string ExistHist = GetValue(cmdGet);
                    cmdGet.Dispose();
                    if (ExistHist.Trim().Equals(string.Empty))
                    {

                        string insert = string.Format(@"INSERT INTO vvs_daily_cust_visit 
(
VISIT_NO
,COMPANY_NAME
,SALESPERSON
,CONTACT
,LOCATION
,QUOTE_NO
,DT
,TM
,PROJECT_REF
,PURPOSE_VISIT
,OWNER
,CUST_TYPE
,VISIT_NOTES
,COMPANY_CODE
,SP_CODE
,CUST_CODE
,HistoryID
)
VALUES
(
" + VISIT_NO
+ ",'" + COMPANY_NAME
+ "','" + SALESPERSON
+ "','" + CONTACT
+ "','" + LOCATION
+ "','" + QUOTE_NO
+ "','" + DT
+ "','" + TM
+ "','" + PROJECT_REF
+ "','" + PURPOSE_VISIT
+ "','" + OWNER
+ "','" + CUST_TYPE
+ "','" + VISIT_NOTES
+ "','" + COMPANY_CODE
+ "','" + SP_CODE
+ "','" + CUST_CODE
+ "','" + HistoryID + "')"
);
                        OracleCommand cmdHDR = new OracleCommand(insert, Conn);
                        cmdHDR.Transaction = _tran;
                        err = ExecuteNonQuery(cmdHDR, HistoryID, "Visit_History");
                        cmdHDR.Dispose();
                        if (err == InCubeErrors.Success)
                        {

                            string update = string.Format("update RouteHistoryDetail_LOG set Flag='N' where CUSTOMERID=" + CustomerID + " AND OUTLETID=" + OutletID + " AND ROUTEHISTORYID=" + RouteHistoryID + " AND VISITNO=" + VisitNo + "");
                            qry = new InCubeQuery(update, db_vms);
                            err = qry.ExecuteNonQuery();
                            _tran.Commit();
                        }
                        else
                        {
                            _tran.Rollback();
                        }
                    }

                }
            }
            catch
            {
                if (_tran != null) _tran.Rollback();
            }
            finally
            {
                if (_tran != null) _tran.Dispose();
            }

        }
        #endregion

        #region Update OutStanding
        private Result SaveTable(DataTable dtData, string TableName, bool AddReportingColumns)
        {
            Result res = Result.Failure;
            try
            {
                if (AddReportingColumns)
                {
                    dtData.Columns.Add("ID", typeof(int));
                    dtData.Columns.Add("ResultID", typeof(int));
                    dtData.Columns.Add("Message", typeof(string));
                    dtData.Columns.Add("Inserted", typeof(bool));
                    dtData.Columns.Add("Updated", typeof(bool));
                    dtData.Columns.Add("Skipped", typeof(bool));
                    for (int i = 0; i < dtData.Rows.Count; i++)
                    {
                        dtData.Rows[i]["ID"] = (i + 1);
                    }
                }
                string columns = "";
                foreach (DataColumn col in dtData.Columns)
                {
                    columns += "\r\n        [" + col.Caption + "] ";
                    switch (col.DataType.Name)
                    {
                        case "Int32":
                            columns += "INT";
                            break;
                        case "Boolean":
                            columns += "BIT";
                            break;
                        default:
                            columns += "NVARCHAR(200)";
                            break;
                    }
                    columns += " NULL,";
                }

                string dataTableCreationQuery = string.Format(@"IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'{0}') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
	EXEC('DROP TABLE {0}');
END

EXEC('
CREATE TABLE {0}({1}
 );

')", TableName, columns.Substring(0, columns.Length - 1));

                incubeQuery = new InCubeQuery(db_vms, dataTableCreationQuery);
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    SqlBulkCopy bulk = new SqlBulkCopy(db_vms.GetConnection());
                    bulk.DestinationTableName = TableName;
                    bulk.WriteToServer(dtData);
                    res = Result.Success;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }

        public override void OutStanding()
        {
            try
            {
                WriteMessage("\r\nReading outstanding from Orion view .. ");
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();
//                DataTable dt = OLEDB_Datatable(@"SELECT ROWNUM,TXN_CODE,DOC_NO,CUST_CODE,TXN_TYPE,INV_AMT,OUTSTANDING_AMT,DOC_DT,DOC_REF FROM INVAN_SOA_NEW WHERE DOC_DT >= TO_DATE('01/01/2017','DD/MM/YYYY') AND CUST_CODE <> 'VAT' AND TXN_CODE IN ('IF01',
//'INU',
//'T-PCN',
//'T-SDN') AND MAIN_ACNT IN ('131000','131005') AND CUST_CODE NOT LIKE '%CV%' ");

                DataTable dt = OLEDB_Datatable(@"SELECT ROWNUM,TXN_CODE,DOC_NO,CUST_CODE,TXN_TYPE,INV_AMT,OUTSTANDING_AMT,DOC_DT,DOC_REF FROM INVAN_SOA_NEW WHERE DOC_DT >= TO_DATE('01/09/2020','DD/MM/YYYY')
AND CUST_CODE <> 'VAT' AND TXN_CODE IN ('IF01',
'INU') AND MAIN_ACNT IN ('131000','131005') AND CUST_CODE NOT LIKE '%CV%' ");

                sw.Stop();
                if (dt != null && dt.Rows.Count > 0)
                {
                    WriteMessage("\r\nOutstanding query finished in " + (sw.ElapsedMilliseconds / 1000.0).ToString() + " seconds, " + dt.Rows.Count.ToString() + " rows were retreived ..");
                    WriteMessage("\r\nClearing old data from InVan DB..");
                    incubeQuery = new InCubeQuery(db_vms, "TRUNCATE TABLE Stg_Outstanding");
                    incubeQuery.ExecuteNonQuery();
                    WriteMessage("\r\nFilling Orion data into InVan staging table ..");
                    SqlBulkCopy bulk = new SqlBulkCopy(db_vms.GetConnection());
                    bulk.DestinationTableName = "Stg_Outstanding";
                    foreach (DataColumn col in dt.Columns)
                        bulk.ColumnMappings.Add(col.Caption, col.Caption);
                    bulk.WriteToServer(dt);
                }
                else
                {
                    WriteMessage("Error !!");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        public override void UpdateBank()
        {
            try
            {
                WriteMessage("\r\nReading DisplayCredit from Orion view .. ");
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                
                DataTable dt = OLEDB_Datatable(@"SELECT ROWNUM,TXN_CODE,DOC_NO,CUST_CODE,TXN_TYPE,INV_AMT,OUTSTANDING_AMT,
DOC_DT,DOC_REF FROM INVAN_SOA_NEW WHERE DOC_DT >= TO_DATE('01/09/2020','DD/MM/YYYY') AND CUST_CODE <> 'VAT' 
AND TXN_CODE IN ('T-PCN') AND MAIN_ACNT IN ('131000','131005') AND CUST_CODE LIKE '%CV%' ");

                sw.Stop();
                if (dt != null && dt.Rows.Count > 0)
                {
                    WriteMessage("\r\nDisplayCredit query finished in " + (sw.ElapsedMilliseconds / 1000.0).ToString() + " seconds, " + dt.Rows.Count.ToString() + " rows were retreived ..");
                    WriteMessage("\r\nClearing old data from InVan DB..");
                    incubeQuery = new InCubeQuery(db_vms, "TRUNCATE TABLE Stg_DisplayCredit");
                    incubeQuery.ExecuteNonQuery();
                    WriteMessage("\r\nFilling Orion data into InVan staging table ..");
                    SqlBulkCopy bulk = new SqlBulkCopy(db_vms.GetConnection());
                    bulk.DestinationTableName = "Stg_DisplayCredit";
                    foreach (DataColumn col in dt.Columns)
                        bulk.ColumnMappings.Add(col.Caption, col.Caption);
                    bulk.WriteToServer(dt);
                }
                else
                {
                    WriteMessage("Error !!");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        #endregion
        #region GENERIC
        private DataTable OLEDB_Datatable(string select)
        {
            OracleDataAdapter oledbAdapter = null;
            DataTable tbl = new DataTable();
            try
            {
                if (Conn.State == ConnectionState.Closed)
                {
                    Conn.Open();
                }
                oledbAdapter = new OracleDataAdapter(select, Conn);
                oledbAdapter.Fill(tbl);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return null;
            }
            finally
            {
                oledbAdapter.Dispose();
                Conn.Close();
            }
            return tbl;
        }
        private InCubeErrors UpdateFlag(string TableName, string Criteria, string TableCode)
        {
            if (Conn.State == ConnectionState.Closed) Conn.Open();

            try
            {
                string query = string.Format("update {0} set " + TableCode + "_PROC_FLAG='Y' where {1}", TableName, Criteria);
                OracleCommand cmdHDR = new OracleCommand(query, Conn);
                cmdHDR.CommandTimeout = 5;
                //WriteExceptions("Update query:\r\n" + query + "\r\nTimeOut:" + cmdHDR.CommandTimeout.ToString(), "Updating flag", false);
                int affectedRows = 0;
                err = ExecuteNonQuery(cmdHDR, ref affectedRows);
                //WriteExceptions("Update flag result:" + err.ToString() + " AND rows affected: " + affectedRows.ToString(), "Updating flag", false);
                cmdHDR.Dispose();
                return err;
            }
            catch (Exception ex)
            {
                err = InCubeErrors.Error;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return err;
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
        private void SetDataRowDefaultValues(ref DataRow dr)
        {
            try
            {
                for (int i = 0; i < dr.ItemArray.Length - 1; i++)
                {
                    if (dr.ItemArray[i] == null || dr.ItemArray[i].ToString().Trim().Equals(string.Empty))
                    {
                        dr.ItemArray[i] = "0";
                    }
                }


            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void WriteExceptions(string description, string header, bool end)
        {
            try
            {
                string symbol = string.Empty;
                if (end)
                {
                    symbol = "===========================================================================";
                }
                string filename = "INV-Log" + DateTime.Today.Year.ToString() + DateTime.Today.Month.ToString() + DateTime.Today.Day.ToString() + ".log";
                string filePath = CoreGeneral.Common.StartupPath + "\\" + filename;
                if (!File.Exists(filePath))
                {
                    FileStream fs = File.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite);
                    fs.Close();
                    fs.Dispose();
                }
                StreamWriter wrt = new StreamWriter(filename, true);
                wrt.Write("\n" + "===========================================================================" + "\r\n");
                wrt.Write("\n" + DateTime.Now.ToString() + header + "----" + DateTime.Now.ToString() + "\r\n");
                wrt.Write("\n" + description + "\r\n");
                wrt.Close();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        public override void Close()
        {
            if (Conn != null && Conn.State == ConnectionState.Open)
            {
                Conn.Close();
            }
            if (Conn != null)
                Conn.Dispose();
            if (OracleCMD != null)
                OracleCMD.Dispose();
            if (OracleReader != null)
                OracleReader.Dispose();
            if (OraTrans != null)
                OraTrans.Dispose();
        }
        public override void UpdateOrganization()
        {
            InCubeErrors err;
            object field = new object();


            string SelectOrganization = @"SELECT  Distinct

                                                 STATE
 
                                                 FROM IV40700

                                                 WHERE 
                                                 STATE IN (SELECT IntLocation FROM ALNIntegration) 
                                                 AND NOT LOCNCODE IN (SELECT ColdStore FROM ALNIntegration) 
                                                 AND (LEN(LOCNCODE)=3 or LEN(LOCNCODE)=4) ";

            /*
             * STATE	depot code (ALN,AUH,DXB,SHJ,NE)
            */

            InCubeQuery OrganizationQuery = new InCubeQuery(db_vms, SelectOrganization);
            OrganizationQuery.Execute();

            err = OrganizationQuery.FindFirst();

            while (err == InCubeErrors.Success)
            {
                OrganizationQuery.GetField("STATE", ref field);
                string OrganizationName = field.ToString().Trim();

                err = ExistObject("OrganizationLanguage", "OrganizationID", "Description = '" + OrganizationName + "'", db_vms);
                if (err != InCubeErrors.Success)
                {
                    int OrganizationID = int.Parse(GetFieldValue("Organization", "MAX(OrganizationID) + 1", db_vms));

                    QueryBuilderObject.SetField("OrganizationID", OrganizationID.ToString());
                    QueryBuilderObject.SetField("ParentOrganizationID", "1");
                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.Date.ToString(DateFormat) + "'");
                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.Date.ToString(DateFormat) + "'");
                    QueryBuilderObject.InsertQueryString("Organization", db_vms);

                    QueryBuilderObject.SetField("OrganizationID", OrganizationID.ToString());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + OrganizationName + "'");
                    QueryBuilderObject.InsertQueryString("OrganizationLanguage", db_vms);
                }

                err = OrganizationQuery.FindNext();
            }
        }
        private void GetAddress(string CustomerCode, ref object Address1, ref object Address2, ref object Address3)
        {

            InCubeQuery CustomerQuery = new InCubeQuery(db_vms, @"SELECT     ADDRESS1, ADDRESS2, ADDRESS3
FROM         RM00101
WHERE     CUSTNMBR='" + CustomerCode + "'");
            err = CustomerQuery.Execute();
            err = CustomerQuery.FindFirst();
            if (err == InCubeErrors.Success)
            {
                err = CustomerQuery.GetField(0, ref Address1);
                err = CustomerQuery.GetField(1, ref Address2);
                err = CustomerQuery.GetField(2, ref Address3);
                err = CustomerQuery.FindNext();
            }

        }
        private string GetPaytrmID(string CustomerCode)
        {
            object FIELD = "";
            InCubeQuery GetPaytrmID = new InCubeQuery(db_vms, @"SELECT  PYMTRMID from RM00101 WHERE CUSTNMBR='" + CustomerCode + "'   ");
            err = GetPaytrmID.Execute();
            err = GetPaytrmID.FindFirst();
            while (err == InCubeErrors.Success)
            {
                err = GetPaytrmID.GetField(0, ref FIELD);
                err = GetPaytrmID.FindNext();
            }
            return FIELD.ToString();
        }
        //private void UpdateExpiryDate(DataRow[] txDetails, int[] LNITMSEQList)
        //{

        //    object UOM = "";
        //    object ItemCode = "";
        //    object Pack = "";
        //    object Lot = "";

        //    try
        //    {
        //        for (int i = 0; i < txDetails.Length; i++)
        //        {
        //            DateTime exDate = (DateTime)txDetails[i]["ExpiryDate"];
        //            Pack = txDetails[i]["PackID"];
        //            Lot = txDetails[i]["BatchNo"];
        //            string TransactionID = txDetails[i]["TransactionID"].ToString();
        //            ItemCode = txDetails[i]["ItemCode"];
        //            string DATALOT = exDate.ToString("dd/MM/yyyy");
        //            SqlCommand cmd2 = new SqlCommand("update SOP10201 Set EXPNDATE=CONVERT(DATETIME,'" + exDate.ToString("yyyy-MM-dd") + "',102)  where SOPTYPE =4  AND SOPNUMBE='" + TransactionID + "' AND  LNITMSEQ=" + LNITMSEQList[i] + " AND  QTYTYPE=1 AND SERLTNUM='" + Lot.ToString() + "' and  ITEMNMBR='" + ItemCode + "' ", db_GP_con);
        //            StreamWriter wrt = new StreamWriter("UpdateLog.log", true);
        //            wrt.Write(cmd2.CommandText.ToString());
        //            wrt.WriteLine();
        //            int a = cmd2.ExecuteNonQuery();
        //            wrt.Write(a.ToString());
        //            wrt.Close();
        //        }

        //    }
        //    catch (Exception ex1)
        //    {
        //        MessageBox.Show(ex1.Message);
        //    }
        //}
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
        //        public override void OutStanding()
        //        {
        //            try
        //            {


        //                string outstanding = string.Format(@"SELECT     dbo.RM20101.DOCNUMBR, dbo.RM20101.SLPRSNID, dbo.RM20101.ORTRXAMT, dbo.RM20101.CURTRXAM
        //FROM         dbo.RM20101 INNER JOIN
        //                      dbo.RM20201 ON dbo.RM20101.DOCNUMBR = dbo.RM20201.APTODCNM
        //WHERE     (dbo.RM20201.APTODCNM LIKE '%INV-%') AND (NOT (dbo.RM20201.APFRDCNM LIKE '%PAY-%')) AND (dbo.RM20101.DOCDATE > '09-09-2011')
        //and dbo.RM20101.SLPRSNID between '1' and '999999'
        //ORDER BY dbo.RM20101.DOCNUMBR");
        //                string outstanding_OLD_Query = @"SELECT rmop.CUSTNMBR ,RMOP.SLPRSNID , 
        //RMOP.DOCNUMBR ,RMOP.ORTRXAMT ,isnull(SUM(RMAP.APPTOAMT),0) as APPTOAMT,
        //RMOP.CURTRXAM ,RMOP.DOCDATE
        //FROM RM20101 RMOP
        //LEFT OUTER JOIN RM20201 RMAP ON RMOP.DOCNUMBR = RMAP.APTODCNM  
        //where APFRDCNM not LIKE 'PAY%' and LEN(APFRDCNM) <= 15 
        //AND VOIDSTTS = 0 
        //and RMOP.CURTRXAM <>0
        //and RMOP.DOCNUMBR like 'INV-%' 
        //group BY RMOP.DOCNUMBR  ,RMOP.CUSTNMBR,RMOP.SLPRSNID  ,RMOP.ORTRXAMT,RMOP.CURTRXAM,RMOP.DOCDATE
        //order by RMOP.DOCDATE desc
        //";
        //                InCubeQuery outstandingQry = new InCubeQuery(outstanding,db_vms);
        //                DataTable tbl = new DataTable();
        //                err=outstandingQry.Execute();
        //                tbl = outstandingQry.GetDataTable();
        //                ClearProgress();
        //                SetProgressMax(tbl.Rows.Count;
        //                int TOTALUPDATED = 0;
        //                int TOTALINSERTED = 0;
        //                foreach (DataRow dr in tbl.Rows)
        //                {
        //                    ReportProgress(++;
        //                    IntegrationForm.lblProgress.Text = "Updating Outstanding" + " " + ReportProgress( + " / " + IntegrationForm.progressBar1.Maximum;
        //                    //string CustomerCode = dr["CUSTNMBR"].ToString().Trim();
        //                    //if (CustomerCode.Equals(string.Empty)) continue;
        //                    string EmployeeCode = dr["SLPRSNID"].ToString().Trim();
        //                    if (EmployeeCode.Equals(string.Empty)) continue;
        //                    string TransactionID = dr["DOCNUMBR"].ToString().Trim();
        //                    if (TransactionID.Equals(string.Empty)) continue;
        //                    string RemainingAmnt = dr["CURTRXAM"].ToString().Trim();
        //                    string Applied = "0";// dr["APPTOAMT"].ToString().Trim();
        //                    string AllAmnt=dr["ORTRXAMT"].ToString().Trim();

        //                    string spID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode='" + EmployeeCode + "'", db_vms).Trim();
        //                    if (spID.Equals(string.Empty)) continue;
        //                    //string customerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode='" + CustomerCode + "'", db_vms).Trim();
        //                    //if (customerID.Equals(string.Empty)) continue;
        //                    //string outletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerCode='" + CustomerCode + "'", db_vms).Trim();
        //                    //if (outletID.Equals(string.Empty)) continue;
        //                    object field = new object();
        //                    string CheckUploaded = string.Format("select top(1)uploaded,deviceserial from RouteHistory where EmployeeID=" + spID + " ORDER BY RouteHistoryID Desc ");
        //                    InCubeQuery qry = new InCubeQuery(CheckUploaded, db_vms);
        //                    err = qry.Execute();
        //                    err = qry.FindFirst();
        //                    err = qry.GetField("uploaded", ref field);
        //                    string uploaded = field.ToString().Trim();
        //                    err = qry.GetField("deviceserial", ref field);
        //                    string deviceserial = field.ToString().Trim();
        //                    if (!uploaded.ToString().Trim().Equals(string.Empty) && !uploaded.ToString().Trim().Equals("System.Object"))
        //                    {
        //                        if (Convert.ToBoolean(uploaded.ToString().Trim()))
        //                        {
        //                            WriteMessage("\r\n");
        //                            WriteMessage("<<< The Salesman " + EmployeeCode + " is not downloaded . No Outstanding will be modified .>>> Total Updated = " + TOTALUPDATED);
        //                            continue;
        //                        }

        //                    }
        //                    err=QueryBuilderObject.SetField("RemainingAmount", RemainingAmnt);
        //                    err=QueryBuilderObject.SetField("Notes", "'"+"app"+Applied+"-tot"+AllAmnt+"-rem"+RemainingAmnt+"'");
        //                    err = QueryBuilderObject.UpdateQueryString("[Transaction]", "TransactionID='" + TransactionID + "'", db_vms);
        //                }


        //            }
        //            catch (Exception ex)
        //            { 

        //            }
        //        }
        private string GetValue(OracleCommand OCommand)
        {
            object obj = null;
            try
            {
                obj = OCommand.ExecuteScalar();
                if (obj == null)
                { return ""; }
            }
            catch
            {
                return "";
            }
            return obj.ToString();
        }
        private InCubeErrors ExecuteNonQuery(OracleCommand OCommand, string transactionID, string TranType)
        {
            try
            {
                int rows = OCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                WriteExceptions(ex.Message, "error", true);
                InsertLog(transactionID, ex.Message, TranType);
                return InCubeErrors.Error;
            }
            return InCubeErrors.Success;
        }
        private InCubeErrors ExecuteNonQuery(OracleCommand OCommand)
        {
            try
            {
                int rows = OCommand.ExecuteNonQuery();
                return InCubeErrors.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return InCubeErrors.Error;
            }
        }
        private InCubeErrors ExecuteNonQuery(OracleCommand OCommand, ref int AffectedRows)
        {
            try
            {
                AffectedRows = OCommand.ExecuteNonQuery();
                return InCubeErrors.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return InCubeErrors.Error;
            }
        }
        private InCubeErrors InsertLog(string transactionID, string errorDescription, string errorLocation)
        {
            InCubeErrors logError = InCubeErrors.Error;
            try
            {
                string insert = string.Format(@"insert into InCubeLog values('{0}','{1}','{2}','{3}')", transactionID, DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss"), errorDescription, errorLocation);
                InCubeQuery logQry = new InCubeQuery(insert, db_vms);
                logError = logQry.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return logError;
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
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }


        }
        #endregion

        public override void RunPostActionFunction()
        {
            try
            {
                if ((IntegrationField)FieldID == IntegrationField.Item_U)
                {
                    incubeQuery = new InCubeQuery(db_vms, string.Format("SELECT WITM_ITEM_CODE FROM Stg_Item WHERE TriggerID = {0} AND ResultID = 1", OrgTriggerID));
                    if (incubeQuery.Execute() == InCubeErrors.Success)
                    {
                        DataTable dt = incubeQuery.GetDataTable();
                        if (dt.Rows.Count > 0)
                        {
                            string WhereStr = "";
                            string OracleQry = "";
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                WhereStr += "'" + dt.Rows[i]["WITM_ITEM_CODE"].ToString() + "',";
                                if (i > 0 && i % 100 == 0)
                                {
                                    WhereStr = WhereStr.Substring(0, WhereStr.Length - 1);

                                    if (Conn.State != ConnectionState.Open)
                                        Conn.Open();
                                    OracleQry = string.Format("UPDATE OW_ITEM_MAS SET WITM_PROC_FLAG='Y' WHERE WITM_ITEM_CODE IN ({0})", WhereStr);
                                    OracleCMD = new OracleCommand(OracleQry, Conn);
                                    OracleCMD.CommandType = CommandType.Text;
                                    OracleCMD.CommandTimeout = 1800;
                                    OracleCMD.ExecuteNonQuery();
                                    WhereStr = "";
                                }
                            }

                            if (WhereStr.Length > 0)
                            {
                                WhereStr = WhereStr.Substring(0, WhereStr.Length - 1);

                                if (Conn.State != ConnectionState.Open)
                                    Conn.Open();
                                OracleQry = string.Format("UPDATE OW_ITEM_MAS SET WITM_PROC_FLAG='Y' WHERE WITM_ITEM_CODE IN ({0})", WhereStr);
                                OracleCMD = new OracleCommand(OracleQry, Conn);
                                OracleCMD.CommandType = CommandType.Text;
                                OracleCMD.CommandTimeout = 1800;
                                OracleCMD.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void GetGeneralMasterData()
        {
            IntegrationField field = (IntegrationField)FieldID;
            Result res = Result.UnKnown;
            string result = "";
            Dictionary<string, string> Staging = new Dictionary<string, string>();
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
                ProcessID = execManager.LogIntegrationBegining(TriggerID, base.OrganizationID, filters);
                incubeQuery = new InCubeQuery(db_vms, "SELECT OracleTable,StagingTable,OracleQuery FROM Int_StagingTables WHERE FieldID = " + FieldID);
                incubeQuery.Execute();
                DataTable dtStaging = incubeQuery.GetDataTable();

                foreach (DataRow dr in dtStaging.Rows)
                {
                    string OracleTable = dr["OracleTable"].ToString();
                    string OracleQuery = dr["OracleQuery"].ToString().Trim();
                    if (OracleQuery == string.Empty)
                    {
                        OracleQuery = "SELECT * FROM " + OracleTable;
                    }
                    string StagingTable = dr["StagingTable"].ToString();

                    result = SaveOracleTable(OracleQuery, StagingTable, ref rowsCount);
                    if (result == "Success")
                        res = Result.Success;

                    WriteMessage(result);
                    if (res != Result.Success)
                        break;
                }
                execManager.UpdateActionTotalRows(TriggerID, rowsCount);
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
        private string SaveOracleTable(string OracleQuery, string TableName, ref int RowsCount)
        {
            try
            {
                //Get first row in staging table
                incubeQuery = new InCubeQuery(db_vms, "SELECT TOP 1 * FROM [" + TableName + "]");
                if (incubeQuery.Execute() != InCubeErrors.Success)
                    return "Error reading from staging table";
                DataTable dtStaging = incubeQuery.GetDataTable();
                if (dtStaging == null)
                    return "Error reading from staging table";

                //Open Oracle reader
                OracleReader = null;
                try
                {
                    if (Conn.State != ConnectionState.Open)
                        Conn.Open();

                    OracleCMD = new OracleCommand(OracleQuery, Conn);
                    OracleCMD.CommandType = CommandType.Text;
                    OracleCMD.CommandTimeout = 1800;
                    OracleReader = OracleCMD.ExecuteReader();
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
                    for (int i = 0; i < OracleReader.FieldCount; i++)
                    {
                        string OracleColumn = OracleReader.GetName(i);
                        if (dtStaging.Columns.Contains(OracleColumn))
                        {
                            dtData.Columns.Add(dtStaging.Columns[OracleColumn].ColumnName);
                            ColumnsMapping.Add(OracleColumn, dtStaging.Columns[OracleColumn].ColumnName);
                        }
                    }

                    int count = 0;
                    int ID = 0;
                    DataRow dRow = null;
                    bulk = new SqlBulkCopy(db_vms.GetConnection());
                    bulk.DestinationTableName = TableName;
                    foreach (DataColumn col in dtData.Columns)
                        bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    bulk.BulkCopyTimeout = 300;

                    while (OracleReader.Read())
                    {
                        dRow = dtData.NewRow();
                        dRow["ID"] = ++ID;
                        dRow["TriggerID"] = TriggerID;
                        foreach (KeyValuePair<string, string> pair in ColumnsMapping)
                        {
                            dRow[pair.Value] = OracleReader[pair.Key];
                        }
                        dtData.Rows.Add(dRow);
                        count++;
                        if (count == 1000)
                        {
                            try
                            {
                                bulk.WriteToServer(dtData);
                                RowsCount += count;
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
                            RowsCount += count;
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
            finally
            {
                if (Conn != null && Conn.State == ConnectionState.Open)
                {
                    Conn.Close();
                }
            }
            return "Success";
        }

        
    }
}