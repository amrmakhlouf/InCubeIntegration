using InCubeIntegration_DAL;
using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.IO;
using System.Xml;
using InCubeLibrary;

namespace InCubeIntegration_BL
{
    public class IntegrationAbuIssa : IntegrationBase
    {
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
        int rowsAffected = 0;
        //SqlConnection db_GP_con;
        private long UserID;
        string DateFormat = "dd-MMM-yy";
        InCubeQuery qry;
        OracleConnection Conn;
        string ConnectionString = string.Empty;
        #endregion

        #region CONSTRUCTOR
        public IntegrationAbuIssa(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(CoreGeneral.Common.StartupPath + "\\DataSources.xml");
            ConnectionString = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'InCubeSQL']/Data").InnerText;

            Conn = new OracleConnection(ConnectionString);//"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST= 192.168.0.151)(PORT=1521)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME= vvsdb )));User Id= sun;Password= sun;Persist Security Info=True");

            try
            {
                Conn.Open();
                if (Conn.State != ConnectionState.Open)
                {
                    WriteMessage("Unable to connect to ORACLE database");
                    return;
                }
                else
                {
                    Conn.Close();
                }
            }
            catch
            {
                WriteMessage("Unable to connect to ORACLE database");
                return;
            }
            UserID = CurrentUserID;
        }
        #endregion

        #region GET
        #region Update Items
        public override void UpdateItem()
        {
            try
            {

                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;

                object field = new object();

                
                //InCubeQuery DeletePriceDefinitionQuery = new InCubeQuery(db_vms, "Delete From PackGroupDetail");
                //DeletePriceDefinitionQuery.ExecuteNonQuery();
                DataTable DT = new DataTable();
                if (Conn.State == ConnectionState.Closed)
                {
                    Conn.Open();
                }
                OracleDataAdapter adapter = new OracleDataAdapter(@"SELECT * FROM VU_ITEMS", Conn);
                adapter.Fill(DT);




                //QueryBuilderObject.SetField("InActive", "1");
                //QueryBuilderObject.UpdateQueryString("Item", db_vms);

                ClearProgress();
                SetProgressMax(DT.Rows.Count);

                foreach (DataRow row in DT.Rows)
                {
                    ReportProgress("Updating Items");
                    
                    //Item Code 0	
                    //Item description English 1	
                    //Item description Arabic	2
                    //Item division 3
                    //Division description	4
                    //Item category	5
                    //Category description	6
                    //Brand	7
                    //Origin	8
                    //Pack 	9
                    //Pack quantity	10
                    //Pack barcode    11   UPDATE IC_ITEM_BARCODE WHERE IB_RID=VU_ITEMS.IB_RID
                    string IC_RID = row["IC_RID"].ToString().Trim();
                    string IM_RID = row["IM_RID"].ToString().Trim();
                    string IB_RID = row["IB_RID"].ToString().Trim();
                    string status = "ACTIVE";// row["Satus"].ToString().Trim();
                    if (status == "INACTIVE") { status = "1"; } else { status = "0"; }
                    string ItemCode = row["ITEMCODE"].ToString().Trim();
                    string itemDescriptionEnglish = row["ITEMDESCRIPTIONENGLISH"].ToString().Trim();
                    string itemDescriptionArabic = row["ITEMDESCRIPTIONENGLISH"].ToString().Trim();
                    string DivisionCode = row["ITEMCATEGORY"].ToString().Trim();
                    string DivisionNameEnglish = row["CATEGORYDESCRIPTION"].ToString().Trim();
                    //if (DivisionCode.Equals("2")) { itemDescriptionEnglish = "METS " + itemDescriptionEnglish; itemDescriptionArabic = "METS " + itemDescriptionArabic; }
                    string CategoryCode = row["ITEMCATEGORY"].ToString().Trim();
                    string CategoryNameEnglish = row["CATEGORYDESCRIPTION"].ToString();//row["ItCategory"].ToString().Trim();
                    string Brand = row["BRAND"].ToString().Trim();
                    string Orgin = row["ORIGIN"].ToString().Trim();// row["Origin"].ToString().Trim();
                    string TCAllowed = "N"; //row["TCAllowed"].ToString().Trim();
                    if (TCAllowed == "Y") { TCAllowed = "1"; } else { TCAllowed = "0"; }
                    string PackDescriptionEnglish = row["PACK"].ToString().Trim();
                    string packQty = row["PACKQUANTITY"].ToString().Trim();
                    try
                    {
                        //decimal x = 0;
                        //int y = 0;
                        //x = decimal.Parse(packQty);

                        //if (x > 1)
                        //{
                        //    PackDescriptionEnglish = "CASE";
                        //}
                        //else
                        //{
                        //    PackDescriptionEnglish = "PCS";
                        //}
                    }
                    catch
                    {

                    }

                    string barcode = row["PACKBARCODE"].ToString().Trim();
                    string PackDefinition = row["INTERNALITEMCODE"].ToString().Trim();
                    string PackGroup = row["BRAND"].ToString().Trim();
                    string PackGroupCode = row["BRAND"].ToString().Trim();
                    string PackGroupID = string.Empty;
                    if (ItemCode == string.Empty)
                        continue;

                    //if (DivisionCode == string.Empty)
                    //{
                    //    throw new Exception("Error in Item , Blank Division Code Item Code (" + ItemCode + ") >> File Name : " + Files[i] + "  Line number : " + counter.ToString());
                    //}
                    //if (CategoryCode == string.Empty)
                    //{
                    //    throw new Exception("Error in Item , Blank Category Code Item Code (" + ItemCode + ") >> File Name : " + Files[i] + "  Line number : " + counter.ToString());
                    //}

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
                        QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", db_vms);

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


                    #region PackType

                    string PacktypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " Description = '" + PackDescriptionEnglish + "' AND LanguageID = 1", db_vms);

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
                        QueryBuilderObject.SetField("PackDefinition", "'" + PackDefinition + "'");
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

                    int PackID = 1;

                    ExistItem = GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " and PackTypeID = " + PacktypeID, db_vms);
                    if (ExistItem != string.Empty)
                    {
                        PackID = int.Parse(GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " and PackTypeID = " + PacktypeID, db_vms));

                        QueryBuilderObject.SetField("Barcode", "'" + barcode + "'");
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

                    if (err == InCubeErrors.Success)
                    {

                        UpdateFlag("IC_PRODUCT_CODE", "ROWID='" + IC_RID + "'");
                        UpdateFlag("IC_ITEM_MASTER", "ROWID='" + IM_RID + "'");
                        UpdateFlag("IC_ITEM_BARCODE", "ROWID='" + IB_RID + "'");

                    }
                    #endregion
                }

                DT.Dispose();

                WriteMessage("\r\n");
                WriteMessage("<<< ITEMS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
            }
            catch
            {

            }
        }
        #endregion

        #region UpdateCustomer

        public override void UpdateCustomer()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            object field = new object();

            
            DataTable DT = new DataTable();
            if (Conn.State == ConnectionState.Closed)
            {
                Conn.Open();
            }
            OracleDataAdapter adapter = new OracleDataAdapter(@"SELECT  * FROM VU_CUSTOMERS where UP_FLAG='N'", Conn);
            adapter.Fill(DT);


            ClearProgress();
            SetProgressMax(DT.Rows.Count);

            foreach (DataRow row in DT.Rows)
            {
                try
                {

                    ReportProgress("Updating Customers");

                    #region Variables
                    string IC_RID = row["RID"].ToString().Trim();
                    string PriceListCode = row["ROUTECODE"].ToString().Trim();
                    string CustomerCode = row["CUSTOMERCODE"].ToString().Trim();

                    string CustomerName = row["GENERALDESCRIPTION"].ToString().Trim();
                    string outletBarCode = row["CUSTOMERBARCODE"].ToString().Trim();
                    string outletCode = row["CUSTOMERCODE"].ToString().Trim();
                    string CustomerOutletDescriptionEnglish = row["GENERALDESCRIPTION"].ToString().Trim();
                    string CustomerOutletDescriptionArabic = row["GENERALDESCRIPTION"].ToString().Trim();


                    CustomerCode = outletCode;
                    CustomerName = CustomerOutletDescriptionEnglish;
                    string Phonenumber = row["PHONE"].ToString().Trim();
                    string Faxnumber = row["PHONE"].ToString().Trim();
                    if (Phonenumber.Length >= 40) Phonenumber.Substring(0, 39);
                    if (Faxnumber.Length >= 40) Faxnumber.Substring(0, 39);
                    string Email = row["EMAIL"].ToString().Trim();
                    string CustomerAddressEnglish = row["MAINADDRESS"].ToString().Trim();
                    string CustomerAddressArabic = row["MAINADDRESS"].ToString().Trim();
                    string Taxable = "0"; //row[9].ToString().Trim();
                    //string channelDescription = row["Channel"].ToString().Trim();
                    //string ChannelCode = row["ChannelCode"].ToString().Trim();
                    string CustomerNewGroup = row["CUSTOMERGROUP"].ToString().Trim();// string.Empty;
                    //if (CustomerName.Trim().Replace(" ", "").ToLower() != "nogroup")
                    //{
                    //    CustomerNewGroup = row["CustomerGroup"].ToString().Trim();
                    //}

                    string CustomerGroupDescription = row["CUSTOMERGROUP"].ToString().Trim();
                    //string IsCreditCustomer = row["CustomerType"].ToString().Trim();
                    string CreditLimit = row["CREDITLIMIT"].ToString().Trim();
                    if (CreditLimit.Equals(string.Empty)) CreditLimit = "1000";
                    string Balance = "0";// row["BALANCE"].ToString().Trim();
                    string Paymentterms = row["PAYMENTTERMS"].ToString().Trim();
                    string OnHold = row["ONHOLD"].ToString().Trim();
                    if (OnHold.ToLower().Equals("i"))
                    {
                        OnHold = "1";
                    }
                    else
                    {
                        OnHold = "0";
                    }
                    string CustomerType = row["ISCREDIT"].ToString().Trim();// string.Empty;// row["CustomerPeymentTerms"].ToString().Trim();

                    if (CustomerType.ToLower().Equals("ch"))
                    {
                        CustomerType = "1";
                    }
                    else
                    {
                        CustomerType = "2";
                    }

                    string inActive = row["ONHOLD"].ToString().Trim();
                    if (inActive.ToLower().Equals("i"))
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
                    //string Classification = row["CUST_ANLY_CODE_04"].ToString().Trim();
                    string RouteCode = row["ROUTECODE"].ToString().Trim();
                    string SalesManCode = row["ROUTECODE"].ToString().Trim();
                    string SalesMan = row["ROUTECODE"].ToString().Trim();
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
                        //QueryBuilderObject.SetField("Balance", Balance);
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
                    CreateCustomerOutlet(IC_RID, AccountID.ToString(), SalesManCode, GroupID, GroupID2, B2B_Invoices, CustomerType, outletCode, PriceListCode, Paymentterms, CustomerOutletDescriptionEnglish, CustomerAddressEnglish, CustomerOutletDescriptionArabic, CustomerAddressArabic, Phonenumber, Faxnumber, OnHold, Taxable, CustomerCode, CreditLimit, Balance, GPSlongitude, GPSlatitude, outletBarCode, Email, companyID, inActive, SalesMan);
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

        private void CreateCustomerOutlet(string IC_RID, string parentAccount, string SALESMAN_CODE, string GroupID, string GroupID2, string B2B_Inv, string CustType, string CustomerCode, string PriceListCode, string Paymentterms, string CustomerDescriptionEnglish, string CustomerAddressEnglish, string CustomerDescriptionArabic, string CustomerAddressArabic, string Phonenumber, string Faxnumber, string OnHold, string Taxable, string HeadOfficeCode, string CreditLimit, string Balance, string Longitude, string latitude, string CustomerBarCode, string email, string divisionID, string inactive, string SALESMAN_NAME)
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

            string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerID = " + CustomerID + " AND CustomerCode = '" + CustomerCode + "'", db_vms);
            if (!OutletID.Trim().Equals(string.Empty))
            {
                QueryBuilderObject.SetField("Phone", "'" + Phonenumber + "'");
                QueryBuilderObject.SetField("Fax", "'" + Faxnumber + "'");
                QueryBuilderObject.SetField("Email", "'" + email + "'");
                QueryBuilderObject.SetField("CustomerTypeID", CustType); //HardCoded -1- Cash -2- Credit
                QueryBuilderObject.SetField("OnHold", OnHold);
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
                QueryBuilderObject.SetField("CustomerCode", "'" + CustomerCode + "'");
                QueryBuilderObject.SetField("Barcode", "'" + CustomerBarCode + "'");
                QueryBuilderObject.SetField("Phone", "'" + Phonenumber + "'");
                QueryBuilderObject.SetField("Fax", "'" + Faxnumber + "'");
                QueryBuilderObject.SetField("Email", "'" + email + "'");
                QueryBuilderObject.SetField("Taxeable", Taxable);
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



            //err = ExistObject("CustOutTerritory", "TerritoryID", "TerritoryID = " + TerritoryID + " AND CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
            //if (err != InCubeErrors.Success)
            //{

            //    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
            //    QueryBuilderObject.SetField("OutletID", OutletID);
            //    QueryBuilderObject.SetField("TerritoryID", TerritoryID);
            //    err = QueryBuilderObject.InsertQueryString("CustOutTerritory", db_vms);
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

            // IN THE FOLLOWING AM TAKING ALL DIVISIONS AND CREATE ACCOUNTS FOR THE CUSTOMERS FOR EACH DIVISION
            string getdivisions = string.Format("select DivisionID,DivisionCode from Division");
            InCubeQuery accQry = new InCubeQuery(getdivisions, db_vms);
            err = accQry.Execute();
            DataTable accTbl = new DataTable();
            accTbl = accQry.GetDataTable();
            foreach (DataRow acr in accTbl.Rows)
            {
                ExistCustomer = GetFieldValue("AccountCustOutDiv", "AccountID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " and DivisionID=" + acr["DivisionID"].ToString().Trim() + "", db_vms);
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
                    QueryBuilderObject.SetField("DivisionID", acr["DivisionID"].ToString().Trim());
                    QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                    err = QueryBuilderObject.InsertQueryString("AccountCustOutDiv", db_vms);

                    QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + acr["DivisionCode"].ToString().Trim() + "" + CustomerDescriptionEnglish.Trim() + " Account'");
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
            }
            //THE FOLLOWING UPDATE IS TO DISTRIBUTE THE CREDIT LIMIT OVER ACCOUNTCUSTOUTDIVEMP
            string ResetAccount = @"
update a set a.creditlimit= (ac.creditlimit*(dcl.creditlimitpercentage/100))
from AccountCustOutDiv acod
inner join account a on a.accountid=acod.accountid 
inner join account ac on a.ParentAccountID=ac.AccountID
inner join [DivisionsCreditLimit] dcl on acod.CustomerID=dcl.CustomerID and acod.OutletID=dcl.OutletID
and acod.DivisionID=dcl.DivisionID and dcl.employeeid=-1
";
            InCubeQuery CL_Query = new InCubeQuery(ResetAccount, db_vms);
            err = CL_Query.ExecuteNonQuery();


            if (err == InCubeErrors.Success)
            {
                err = UpdateFlag("IC_VAN_CUST_M", "ROWID='" + IC_RID + "'");
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
        //        SetProgressMax(DT.Rows.Count);
        //        foreach (DataRow dr in DT.Rows)
        //        {
        //            ReportProgress();
        //            IntegrationForm.lblProgress.Text = "Updating Locations" + " " + IntegrationForm.progressBar1.Value + " / " + SetProgressMax(;
        //            Application.DoEvents();
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

                //InCubeQuery DeletePriceDefinitionQuery = new InCubeQuery(db_vms, "Delete From PriceDefinition");
                //DeletePriceDefinitionQuery.ExecuteNonQuery();

                // for Al-Ain, the dfault price list is designated as "STANDARD"
                //UpdatePriceList("EXTPRCLVL", "1", "1", true, ref TOTALUPDATED);
                if (Conn.State == ConnectionState.Closed) Conn.Open();
                DataTable DT = new DataTable();
                string SelectGroup = @"SELECT * FROM VU_PRICELIST";

                OracleDataAdapter adapter = new OracleDataAdapter(SelectGroup, Conn);
                adapter.Fill(DT);
                //InCubeQuery GroupQuery = new InCubeQuery(db_vms, SelectGroup);
                //err = GroupQuery.Execute();
                //DT = GroupQuery.GetDataTable();
                ClearProgress();
                SetProgressMax(DT.Rows.Count);
                if (DT.Rows.Count > 0)
                {
                    WriteExceptions("PRICE UPDATE BEGIN.. NOT COMPLETED YET NUMBER OF RECORDS IS  " + DT.Rows.Count.ToString() + "", "PRICE UPDATE BEGIN..", false);
                }
                else
                {
                    WriteExceptions("NO PRICEES FOUND", "NO PRICEES FOUND", false);
                }

                InCubeQuery qry = new InCubeQuery(db_vms, "DELETE FROM PriceDefinition");
                qry.ExecuteNonQuery(ref rowsAffected);
                WriteExceptions(rowsAffected + " rows were deleted", "Old Prices Deleted", false);

                foreach (DataRow row in DT.Rows)
                {
                    ReportProgress("Updating Prices");

                    bool defaultList = false;
                    string CustomerGroupID = string.Empty;
                    string CUST_CODE = row["CUSTOMERCODE"].ToString().Trim();
                    string PL_CODE = row["PRICELISTCODE"].ToString().Trim();
                    string PL_NAME = row["PRICELISTDESCRIPTION"].ToString().Trim();
                    string ITEM_CODE = row["ITEMCODE"].ToString().Trim();
                    string UOM_CODE = row["UOM"].ToString().Trim();
                    string PRICE = row["PRICE"].ToString().Trim();
                    string IS_DEFAULT = row["ISDEFAULTPRICE"].ToString().Trim();
                    string MST_RID = row["MST_RID"].ToString().Trim();
                    string DTL_RID = row["DTL_RID"].ToString().Trim();
                    string CUST_RID = row["CUST_RID"].ToString().Trim();
                    string startDate = DateTime.Parse(row["VALIDFROMDATE"].ToString().Trim()).ToString("yyyy/MM/dd");
                    string EndDate = DateTime.Parse(row["VALIDToDATE"].ToString().Trim()).ToString("yyyy/MM/dd");
                    string CUSTOMERID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode='" + CUST_CODE + "'", db_vms).Trim();
                    string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerCode='" + CUST_CODE + "'", db_vms).Trim();

                    if (IS_DEFAULT.ToLower().Equals("y"))
                    {
                        defaultList = true;
                    }
                    else
                    {
                        defaultList = false;
                    }

                    if (CUSTOMERID.Equals(string.Empty) && !defaultList) continue;

                    string PriceListID = "1";

                    string itemID = GetFieldValue("Item", "ItemID", "ItemCode='" + ITEM_CODE + "'", db_vms).Trim();
                    if (itemID.Equals(string.Empty)) continue;
                    string packTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + UOM_CODE + "'", db_vms).Trim();
                    if (packTypeID.Equals(string.Empty)) continue;
                    string packID = GetFieldValue("Pack", "PackID", "ItemID=" + itemID + " and PackTypeID=" + packTypeID + "", db_vms).Trim();
                    if (packID.Equals(string.Empty)) continue;

                    err = ExistObject("PriceList", "PriceListID", " PriceListCode = '" + PL_CODE + "'", db_vms);
                    if (err == InCubeErrors.Success)
                    {
                        PriceListID = GetFieldValue("PriceList", "PriceListID", " PriceListCode = '" + PL_CODE + "'", db_vms);
                        //QueryBuilderObject.SetField("StartDate", "'" + startDate + "'");
                        QueryBuilderObject.SetDateField("StartDate", new DateTime(2000,1,1));
                        //QueryBuilderObject.SetField("EndDate", "'" + EndDate + "'");
                        QueryBuilderObject.SetDateField("EndDate", new DateTime(2030, 1, 1));
                        err = QueryBuilderObject.UpdateQueryString("PriceList", "PRICELISTID="+PriceListID, db_vms);
                    }
                    else
                    {
                        PriceListID = GetFieldValue("PriceList", "ISNULL(MAX(PriceListID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("PriceListCode", "'" + PL_CODE + "'");
                        QueryBuilderObject.SetDateField("StartDate", new DateTime(2000, 1, 1));
                        //QueryBuilderObject.SetField("StartDate", "'" + startDate + "'");
                        QueryBuilderObject.SetDateField("EndDate", new DateTime(2030, 1, 1));
                        //QueryBuilderObject.SetField("EndDate", "'" + EndDate + "'");
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

                        QueryBuilderObject.SetField("KeyValue", PriceListID);
                        QueryBuilderObject.UpdateQueryString("Configuration", "KeyName = 'DefaultReturnPriceList' AND EmployeeID = -1", db_vms);
                    }

                    err = ExistObject("PriceQuantityRange", "PriceQuantityRangeID", " PriceQuantityRangeID = 1", db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        QueryBuilderObject.SetField("PriceQuantityRangeID", "1");
                        QueryBuilderObject.SetField("RangeStart", "1");
                        QueryBuilderObject.SetField("RangeEnd", "9999999");
                        QueryBuilderObject.InsertQueryString("PriceQuantityRange", db_vms);
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
                        QueryBuilderObject.SetField("Tax", "0");
                        QueryBuilderObject.SetField("Price", PRICE.ToString());
                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        err = QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);

                    }
                    else
                    {
                        PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "PriceDefinitionID", "PackID = " + packID + " AND PriceListID = " + PriceListID, db_vms));

                        if (!currentPrice.Equals(PRICE.ToString()))
                        {
                            QueryBuilderObject.SetField("Price", PRICE.ToString());
                            err = QueryBuilderObject.UpdateQueryString("PriceDefinition", "PackID = " + packID + " AND PriceListID = " + PriceListID + " AND PriceDefinitionID = " + PriceDefinitionID, db_vms);
                        }
                    }

                    if (!CustomerGroupID.Equals(string.Empty))
                    {
                        qry = new InCubeQuery("DELETE FROM GroupPrice where GroupID=" + CustomerGroupID + "", db_vms);
                        err = qry.ExecuteNonQuery();
                        DataTable DTBL = new DataTable();
                        QueryBuilderObject.SetField("GroupID", CustomerGroupID);
                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        err = QueryBuilderObject.InsertQueryString("GroupPrice", db_vms);
                    }
                    if (!CUSTOMERID.Equals(string.Empty))
                    {
                        qry = new InCubeQuery("DELETE FROM customerPrice where CustomerID=" + CUSTOMERID + " and PriceListID=" + PriceListID + "", db_vms);
                        err = qry.ExecuteNonQuery();
                        DataTable DTBL = new DataTable();
                        QueryBuilderObject.SetField("CustomerID", CUSTOMERID);
                        QueryBuilderObject.SetField("outletID", OutletID);
                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        err = QueryBuilderObject.InsertQueryString("CustomerPrice", db_vms);
                    }

                    UpdateFlag("IC_SCM_CUSPRICELIST_MST", "ROWID='" + MST_RID + "'");
                    UpdateFlag("IC_ITEM_PRICES", "ROWID='" + MST_RID + "'");
                    UpdateFlag("IC_SCM_CUSPRICELIST_DTL", "ROWID='" + DTL_RID + "'");
                    UpdateFlag("IC_SCM_CUSPRICELIST_CUST", "ROWID='" + CUST_RID + "'");
                    //UpdateFlag("IC_ITEM_PRICES", "ROWID='" + CUST_RID + "'");
                }


                if (DT.Rows.Count > 0)
                {
                    WriteExceptions("PRICE UPDATE COMPLETED.. NUMBER OF RECORDS IS  " + DT.Rows.Count.ToString() + "", "PRICE UPDATE BEGIN..", false);
                }
                InCubeQuery incubeQuery = new InCubeQuery(db_vms, "UPDATE_INACTIVE_ITEMS");
                err = incubeQuery.ExecuteStoredProcedure();
                //err = DatabaseSpecialFunctions.RunStoredProcedure(db_vms, "UPDATE_INACTIVE_ITEMS");
                #region COMMENTED
                //DT = new DataTable();
                //SelectGroup = @"select P.PackID,P.ItemID,PD.Price,P.Quantity,PD.PriceListID from Pack P inner join PriceDefinition PD on PD.PacKID=P.PackID and P.Quantity>1 and P.PackTypeID=16";
                //GroupQuery = new InCubeQuery(db_vms, SelectGroup);
                //err = GroupQuery.Execute();
                //DT = GroupQuery.GetDataTable();
                //ClearProgress();
                //SetProgressMax(DT.Rows.Count);
                //foreach (DataRow row in DT.Rows)
                //{
                //    ReportProgress();
                //    IntegrationForm.lblProgress.Text = "Updating Base UOM Prices" + " " + IntegrationForm.progressBar1.Value + " / " + SetProgressMax(;
                //    string ItemID = row["ItemID"].ToString().Trim();
                //    string PackID = row["PackID"].ToString().Trim();
                //    string Price = row["Price"].ToString().Trim();
                //    string Quantity = row["Quantity"].ToString().Trim();
                //    string PriceListID = row["PriceListID"].ToString().Trim();
                //    if (decimal.Parse(Quantity) == 0) continue;
                //    string basePack = GetFieldValue("Pack", "PackID", "ItemID=" + ItemID + " and Quantity=1", db_vms).Trim();
                //    if (basePack.Equals(string.Empty)) continue;
                //    decimal basePrice = 0;
                //    basePrice = decimal.Round((decimal.Parse(Price) / decimal.Parse(Quantity)), 3);

                //    string existPrice = GetFieldValue("PriceDefinition", "Price", "PackID=" + basePack + " and PriceListID="+PriceListID+"", db_vms).Trim();
                //    if (existPrice.Equals(string.Empty))
                //    {
                //        int PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms));
                //        QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID.ToString());
                //        QueryBuilderObject.SetField("QuantityRangeID", "1");
                //        QueryBuilderObject.SetField("PackID", basePack);
                //        QueryBuilderObject.SetField("CurrencyID", "1");
                //        QueryBuilderObject.SetField("Tax", "0");
                //        QueryBuilderObject.SetField("Price", basePrice.ToString());
                //        QueryBuilderObject.SetField("PriceListID", PriceListID);
                //        err = QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);
                //    }
                //    else
                //    {
                //        if (decimal.Parse(existPrice) != basePrice)
                //        {
                //            QueryBuilderObject.SetField("Price", basePrice.ToString());
                //            err = QueryBuilderObject.UpdateQueryString("PriceDefinition", "priceListID=" + PriceListID + " and packID=" + basePack + "", db_vms);
                //        }
                //    }
                //}
                #endregion
                WriteMessage("\r\n");
                WriteMessage("<<< PRICE >>> Total Updated = " + TOTALUPDATED);
            }
            catch (Exception ex)
            {
                WriteExceptions("EXCEPTION HAPPENNED <<" + ex.Message + ">>","PRICE UPDATE ERROR", false);
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
            OracleDataAdapter adapter = new OracleDataAdapter(@"select * from VU_SALESMAN", Conn);
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

                string SalespersonCode = dr["EMPLOYEECODE"].ToString().Trim();
                string companycode = dr["companycode"].ToString().Trim();
                DivisionID = "";// GetFieldValue("Division", "DivisionID", "DivisionCode='" + companycode + "'", db_vms);
                //if (DivisionID.Equals(string.Empty)) ; //continue;
                string SalespersonName = dr["EMPLOYEENAME"].ToString().Trim();
                string EMPLOYEE_TYPE = "2";
                string VehicleCode = dr["LOCATIONCODE"].ToString().Trim();
                string VehicleID = GetFieldValue("warehouse", "WarehouseID", "WarehouseCode='" + VehicleCode + "'", db_vms);
                string RowID = dr["RID"].ToString().Trim();

                OrganizationID = "1";

                AddUpdateSalesperson(RowID, EMPLOYEE_TYPE, SalespersonCode, SalespersonName, ref TOTALUPDATED, ref TOTALINSERTED, DivisionID, OrganizationID, VehicleID);


            }

            DT.Dispose();
            WriteMessage("\r\n");
            WriteMessage("<<< SALESPERSON >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        private void AddUpdateSalesperson(string RowID, string employeeType, string SalespersonCode, string SalespersonName, ref int TOTALUPDATED, ref int TOTALINSERTED, string DivisionID, string OrganizationID, string VehicleID)
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



            err = ExistObject("EmployeeDivision", "EmployeeID", "EmployeeID = " + SalespersonID + " AND DivisionID = " + DivisionID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("DivisionID", DivisionID);
                err = QueryBuilderObject.InsertQueryString("EmployeeDivision", db_vms);
            }

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
                UpdateFlag("IC_SALESMAN", "ROWID='" + RowID + "'");


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
            OracleDataAdapter adapter = new OracleDataAdapter(@"select * from VU_WAREHOUSE", Conn);
            adapter.Fill(DT);




            ClearProgress();
            SetProgressMax(DT.Rows.Count);



            foreach (DataRow dr in DT.Rows)
            {
                ReportProgress("Updating Vehicles");

                string WarehouseCode = dr["WarehouseCode"].ToString().Trim();
                string ROWID = dr["RID"].ToString().Trim();

                string WarehouceName = dr["WarehouseDescription"].ToString().Trim();

                string WarehouseType = string.Empty;
                WarehouseType = "2";
                if (WarehouseCode.ToLower().Equals("q3")) WarehouseType = "1";
                
                OrganizationID = "1";// GetFieldValue("OrganizationLanguage", "OrganizationID", "Description = 'Default Organization'", db_vms);

                AddUpdateWarehouse(ROWID, WarehouseType, WarehouseCode, WarehouceName, ref TOTALUPDATED, ref TOTALINSERTED, OrganizationID);


            }

            DT.Dispose();
            WriteMessage("\r\n");
            WriteMessage("<<< VEHICLES >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        private void AddUpdateWarehouse(string ROWID, string warehouseType, string WarehouseCode, string WarehouceName, ref int TOTALUPDATED, ref int TOTALINSERTED, string OrganizationID)
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
                err = QueryBuilderObject.InsertQueryString("WarehouseZoneLanguage", db_vms);
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
            if (err == InCubeErrors.DBNoMoreRows || err == InCubeErrors.Success)
            {
                UpdateFlag("IC_LOCATIONS", "ROWID='" + ROWID + "'");
            }


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
            catch
            {

            }
        }

        #endregion

        #region UpdateStock

        public override void UpdateStock()
        {

            if (Conn.State == ConnectionState.Closed) Conn.Open();

            if (!db_vms.IsOpened())
            {
                WriteMessage("\r\n");
                WriteMessage("Cannot connect to GP database , please check the connection");
                return;
            }
            string WHCode = string.Empty;
            if (Filters.WarehouseID != -1)
            {
                WHCode = GetFieldValue("WAREHOUSE", "BARCODE", "WarehouseID=" + Filters.WarehouseID, db_vms);
                UpdateStockForWarehouse(Filters.WarehouseID.ToString(), Filters.StockDate, WHCode);
            }
            else
            {

                InCubeErrors err;
                object field = new object();
                //InCubeQuery WarehouseQuery = new InCubeQuery(db_vms, "SELECT Vehicle.VehicleID,Warehouse.Barcode FROM Warehouse INNER JOIN Vehicle ON Warehouse.WarehouseID = Vehicle.VehicleID");
                InCubeQuery WarehouseQuery = new InCubeQuery(db_vms, "SELECT WAREHOUSEID,BARCODE FROM Warehouse");
                err = WarehouseQuery.Execute();

                err = WarehouseQuery.FindFirst();
                while (err == InCubeErrors.Success)
                {
                    WarehouseQuery.GetField(0, ref field);

                    int WarehouseID = Convert.ToInt32(field);
                    WarehouseQuery.GetField(1, ref field);
                    WHCode = field.ToString();

                    UpdateStockForWarehouse(WarehouseID.ToString(), Filters.StockDate, WHCode);

                    err = WarehouseQuery.FindNext();
                }
            }
        }

        private void UpdateStockForWarehouse(string WarehouseID, DateTime StockDate, string WarehouseCode)
        {
            //  int count = 0;
            int TOTALUPDATED = 0;
            string WHCODE = string.Empty;
            try
            {
                List<string> DownloadedVehicles = new List<string>();
                object field = new object();
                #region Update Stock

                field = new object();
                string CheckUploaded = string.Format("select top(1)uploaded,deviceserial from RouteHistory where vehicleid=" + WarehouseID + " ORDER BY RouteHistoryID Desc ");
                qry = new InCubeQuery(CheckUploaded, db_vms);
                err = qry.Execute();
                err = qry.FindFirst();
                err = qry.GetField("uploaded", ref field);
                string uploaded = field.ToString().Trim();
                err = qry.GetField("deviceserial", ref field);
                string deviceserial = field.ToString().Trim();
                //if (uploaded.ToString().Trim().Equals(string.Empty) || uploaded.ToString().Trim().Equals("System.Object")) continue;
                if (!uploaded.ToString().Trim().Equals(string.Empty) && !uploaded.ToString().Trim().Equals("System.Object"))
                {
                    if (Convert.ToBoolean(uploaded.ToString().Trim()))
                    {
                        WriteMessage("\r\n");
                        WriteMessage("<<< The Route " + WarehouseCode + " is not downloaded . No stock will be added .>>> Total Updated = " + TOTALUPDATED);
                        return;
                    }

                }

                qry = new InCubeQuery("DELETE FROM WAREHOUSESTOCK WHERE WarehouseID=" + WarehouseID, db_vms);
                err = qry.ExecuteNonQuery();

                DataTable DTBL = new DataTable();
                //OracleDataAdapter adapter = new OracleDataAdapter(@"SELECT * from v_OS_LOCN_CURR_STK where LCS_STK_QTY_BU>0 and  LCS_LOCN_CODE ='PD01' and LCS_ITEM_CODE='915S MJ SW4-DP'", Conn);
                OracleDataAdapter adapter = new OracleDataAdapter(@"SELECT * from VU_WAREHOUSESTOCK where UP_FLAG='N' AND WAREHOUSECODE='" + WarehouseCode + "' order by RID asc", Conn);
                adapter.Fill(DTBL);

                ClearProgress();
                SetProgressMax(DTBL.Rows.Count);
                WriteExceptions("the number of items are " + DTBL.Rows.Count.ToString() + "", "Number of Items", false);

                foreach (DataRow row in DTBL.Rows)
                {

                    ReportProgress("Updating Stock");

                    string vehicleCode = row["WAREHOUSECODE"].ToString().Trim();
                    WHCODE = vehicleCode;
                    string ItemCode = row["ITEMCODE"].ToString().Trim();
                    string PackTypeCode = row["UOM"].ToString().Trim();
                    string RID = row["RID"].ToString().Trim();
                    string Expirydate = row["EXPIRYDATE"].ToString().Trim();

                    if (!Expirydate.Equals(string.Empty))
                    {
                        Expirydate = DateTime.Parse(Expirydate).ToString(DateFormat);
                    }
                    else
                    {
                        Expirydate = DateTime.Parse("01/01/1990 00:00:00").ToString(DateFormat);
                    }
                    string Quantity = row["QUANTITY"].ToString().Trim();
                    string Batch = row["Batch"].ToString().Trim();

                    //string DivisionCode = "6G2";// row["CompanyID"].ToString().Trim();
                    //string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode='" + DivisionCode + "'", db_vms).Trim();
                    //if (DivisionID.Equals(string.Empty))
                    //    continue;

                    string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + PackTypeCode + "'", db_vms).Trim();
                    if (PackTypeID.Equals(string.Empty))
                        continue;
                    string ItemID = GetFieldValue("Item", "ItemID", "ItemCode='" + ItemCode + "' ", db_vms).Trim();
                    if (ItemID.Equals(string.Empty))
                        continue;
                    string PackID = GetFieldValue("Pack", "PackID", "ItemID=" + ItemID + " and packTypeID=" + PackTypeID + "", db_vms).Trim();
                    if (PackID.Equals(string.Empty))
                        continue;
                    string vehicleID = GetFieldValue("warehouse", "WarehouseID", "Barcode='" + vehicleCode + "'", db_vms).Trim();
                    if (vehicleID.Equals(string.Empty))
                        continue;

                    string UOMdesc = string.Empty;// row[2].ToString().Trim();


                    if (Batch == string.Empty)
                    {
                        Batch = "1990/01/01";
                    }

                    WriteExceptions("Route " + vehicleCode + " is downloaded ...", "Device is downloaded", false);

                    //string updateReady = string.Format("update readyDevice set Ready=0,ReadyDate='{1}' where Routecode='{0}'", vehicleCode, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                    //qry = new InCubeQuery(updateReady, db_vms);
                    //err = qry.ExecuteNonQuery();

                    TOTALUPDATED++;
                    DownloadedVehicles.Add(vehicleCode);
                    string query = "Select PackID from Pack where ItemID = " + ItemID;
                    InCubeQuery CMD = new InCubeQuery(query, db_vms);
                    CMD.Execute();
                    err = CMD.FindFirst();

                    WriteExceptions("proceeding with stock insertion", "Starting Stock update", false);
                    while (err == InCubeErrors.Success)
                    {
                        CMD.GetField(0, ref field);
                        string _packid = field.ToString();
                        string _quantity = "0";
                        string logQty = string.Empty;

                        string existStock = GetFieldValue("WarehouseStock", "PackID", "WarehouseID = " + vehicleID + " AND ZoneID = 1 AND PackID = " + _packid + " AND BatchNo = '" + Batch + "' and ExpiryDate='" + Expirydate + "'", db_vms).Trim();
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

                            err = QueryBuilderObject.InsertQueryString("WarehouseStock", db_vms);
                            if (err == InCubeErrors.Success)
                            {

                                UpdateFlag("IC_ITEM_DEPARTMENT_LOCATIONS", "ROWID='" + RID + "'");
                                UpdateFlag("IC_I_D_L_PATCHES", "ROWID='" + RID + "'");
                            }
                            else
                            {

                            }

                        }
                        else if (_packid == PackID)
                        {

                            string beforeQty = GetFieldValue("WarehouseStock", "Quantity", "WarehouseID = " + vehicleID + " AND ZoneID = 1 AND PackID = " + _packid + " AND BatchNo = '" + Batch + "' and ExpiryDate='" + Expirydate + "'", db_vms).Trim();
                            if (beforeQty.Equals(string.Empty)) beforeQty = "0";
                            WriteExceptions("OLD STOCK QUANTITY BEFORE UPDATE  = " + beforeQty + " ,THE ADDED QUANTITY = " + Quantity + " , THE TOTAL = " + (decimal.Parse(beforeQty) + decimal.Parse(Quantity)) + "  transaction =  ---- pack id is " + _packid + "", "UPDATE Stock ", false);
                            QueryBuilderObject.SetField("Quantity", Quantity);
                            QueryBuilderObject.SetField("BaseQuantity", Quantity);
                            err = QueryBuilderObject.UpdateQueryString("WarehouseStock", "WarehouseID = " + vehicleID + " AND ZoneID = 1 AND PackID = " + PackID + " AND BatchNo = '" + Batch + "'  and ExpiryDate='" + Expirydate + "'", db_vms);
                            if (err == InCubeErrors.Success)
                            {
                                UpdateFlag("IC_ITEM_DEPARTMENT_LOCATIONS", "ROWID='" + RID + "'");
                                UpdateFlag("IC_I_D_L_PATCHES", "ROWID='" + RID + "'");
                                WriteExceptions("stock updated correctly , transaction = ---- pack id is " + _packid + "------ quantity=" + Quantity + "", "UPDATE Stock ", false);
                                if (err == InCubeErrors.Success) { WriteExceptions("updating intermediate transaction success , transaction =", "UPDATE Stock ", false); }
                                else { WriteExceptions("updating intermediate transaction Failed transaction = ", "Inserting Stock ", false); }

                            }
                            else
                            {
                                WriteExceptions("stock updated Failed ****** , transaction =  ---- pack id is " + _packid + "------ quantity=" + Quantity + "", "Inserting Stock ", false);
                            }
                        }

                        err = CMD.FindNext();
                    }
                }


            }
            catch
            {

            }
            WriteMessage("\r\n");
            WriteMessage("<<< STOCK Updated ," + WHCODE + ">>> Total Updated = " + TOTALUPDATED);

            //ClassStartOFDay.StartofDay(db_vms, WarehouseID);

                #endregion
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
        //            SetProgressMax( = CustomerQuery.GetDataTable().Rows.Count;

        //            err = CustomerQuery.FindFirst();

        //            while (err == InCubeErrors.Success)
        //            {

        //                ReportProgress();

        //                IntegrationForm.lblProgress.Text = "Updating Discounts " + " " + IntegrationForm.progressBar1.Value + " / " + SetProgressMax(;
        //                Application.DoEvents();

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
        //                SetProgressMax(DT.Rows.Count);
        //                object field = new object();

        //                foreach (DataRow dr in dt.Rows)
        //                {
        //                    ReportProgress();
        //                    Application.DoEvents();
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


        //                    IntegrationForm.lblProgress.Text = "Updating OUTSTANDING" + " " + IntegrationForm.progressBar1.Value + " / " + SetProgressMax(;

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

        #region SEND MOSAFER CUSTOMERS
        /* FOLLOWING IS TO CREATE THE CUSTOMER SYNC TABLE 
         
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[OracleCustomers](
	[CustomerID] [int] NULL,
	[SentToORacle] [bit] NULL,
	[SendDate] [datetime] NULL
) ON [PRIMARY]
GO

         * */
       
        public override void SendNewCustomers()
        {
            if (Conn.State == ConnectionState.Closed)
            {
                Conn.Open();
            }
            OracleTransaction _tran = null;
            try
            {
                

               

                DataTable dt = new DataTable();
                string getCustomers = string.Format(@"SELECT CO.CUSTOMERCODE,COL.DESCRIPTION CUSTOMERNAME,CO.PHONE,CO.EMAIL,COL.ADDRESS,CO.CUSTOMERID FROM CUSTOMEROUTLET CO
INNER JOIN CUSTOMEROUTLETLANGUAGE COL ON CO.CUSTOMERID=COL.CUSTOMERID AND CO.OUTLETID=COL.OUTLETID AND COL.LANGUAGEID=1
WHERE CO.CUSTOMERID NOT IN (SELECT CUSTOMERID FROM OracleCustomers)

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

                    string CUSTOMERCODE = dr["CUSTOMERCODE"].ToString().Trim();
                    string CUSTOMERNAME = dr["CUSTOMERNAME"].ToString().Trim();
                    string PHONE = dr["PHONE"].ToString().Trim();
                    string EMAIL = dr["EMAIL"].ToString().Trim();
                    string ADDRESS = dr["ADDRESS"].ToString().Trim();
                    string CUSTOMERID = dr["CUSTOMERID"].ToString().Trim();
                    _tran = Conn.BeginTransaction();
                    string checkCustInAX = "";
                    if (checkCustInAX.Equals(string.Empty))
                    {

                        string insertNew = string.Format(@" insert into VAN_CUSTOMERS
(
CUSTOMERCODE,
CUSTOMERNAME,
PHONE,
EMAIL,
ADDRESS
) 
Values
(
'{0}',
'{1}',
'{2}',
'{3}',
'{4}'
)
", CUSTOMERCODE, CUSTOMERNAME, PHONE, EMAIL, ADDRESS);

                        WriteExceptions("INSERTING CUSTOMER  " + CUSTOMERCODE + " INSERT STATEMENT IS: " + insertNew + "", "INSERT CUSTOMERS", false);
                        OracleCommand cmdHDR = new OracleCommand(insertNew, Conn);
                        cmdHDR.Transaction = _tran;
                        err = ExecuteNonQuery(cmdHDR, CUSTOMERCODE, "CUSTOMER");
                        cmdHDR.Dispose();
                        if (err == InCubeErrors.Success)
                        {
                            string updateBlockNumber = string.Format("INSERT INTO OracleCustomers VALUES({0},1,GETDATE())", CUSTOMERID);
                            InCubeQuery AX_Qry = new InCubeQuery(updateBlockNumber, db_vms);
                            err = AX_Qry.ExecuteNonQuery();
                            _tran.Commit();
                        }
                        else
                        {
                            if (_tran != null) _tran.Rollback();
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

        #region SendInvoices
        public override void SendInvoices()
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
                string invoices = string.Format(@"select substring(COL.DESCRIPTION,1,50) DOC_SPECIAL_NAME, CO.CustomerTypeID, CO.CustomerCode CUS_ACCOUNT_NO,T.TransactionID DOC_PROV_NO,E.EmployeeCode SM_CODE,T.TransactionDate DOC_PROV_DATE,T.TransactionDate DOC_DATE,T.TransactionDate CREATION_DATE,
(case(T.TransactionTypeID) when (1) then 'IN' else 
case(T.TransactionTypeID) when (3) then 'IN' else
case(T.TransactionTypeID) when (2) then 'CR' else
case(T.TransactionTypeID) when (4) then 'CR' else 'XX' end end end end) as DT_CODE,(T.Discount+T.promotedDiscount) DOC_DISCOUNT_AMOUNT,((T.Discount+T.promotedDiscount)/(case(NetTotal) when 0 then 1 else NetTotal end)) DOC_DISCOUNT_PCT,
T.Synchronized,T.RemainingAmount,T.GrossTotal,T.GPSLatitude,ed.DeviceSerial CREATION_TERM,'QAR' CUR_CODE,1 SD_EXCHANGE_RATE
,T.GPSLongitude,T.Voided,T.Notes DOC_NARRATION,TransactionStatusID,RouteID,NetTotal DOC_TOTAL_AMOUNT,
T.RouteHistoryID,(case(T.SalesMode) when 1 then '08' else case(T.SalesMode) when 2 then '07' else 'X' end end) as SAL_TYPE,
WH.WarehouseCode SALES_MAN_LOC,
SL.Description as Street, CL.Description as City,D.DivisionCode DEP_CODE,CP.AppliedAmount PAY_AMOUNT,'' SRM_ID
from [Transaction] T
inner join CustomerOutlet CO on T.CustomerID=CO.CustomerID and T.OutletID=CO.OutletID
inner join CustomerOutletLanguage COL on COL.CustomerID=T.CustomerID and COL.OutletID=T.OutletID and LanguageID=1
left outer join PaymentTerm PT on CO.PaymentTermID=PT.PaymentTermID
left join Street S on CO.StreetID=S.StreetID
left join StreetLanguage SL on CO.StreetID=SL.StreetID and SL.LanguageID=1
left join City C on S.CityID=C.CityID and S.StateID=C.StateID and S.CountryID=C.CountryID
left join CityLanguage CL on CL.CityID=C.CityID and CL.LanguageID=1
inner join Employee E on T.EmployeeID=E.EmployeeID
left outer join EmployeeDevice ed on E.EmployeeID=ed.EmployeeID
inner join Warehouse WH on T.WarehouseID=WH.WarehouseID 
inner join Division D on D.DivisionID=T.DivisionID
left outer join CustomerPayment cp on T.transactionID=cp.transactionID and CP.RemainingAmount=T.RemainingAmount
where T.Synchronized=0  AND 
(isnull(T.Notes,'0')<>'ERP') AND (T.TransactionTypeID in(1,2,3,4)) and T.Voided=0 {0}
group by 
CO.CustomerCode,T.TransactionID,E.EmployeeCode,T.TransactionDate,PT.SimplePeriodWidth,
T.TransactionTypeID,T.DiscountAuthorization,T.Discount,T.Synchronized,T.RemainingAmount,T.GrossTotal,T.GPSLatitude
,T.GPSLongitude,T.Voided,T.Notes,TransactionStatusID,RouteID,NetTotal,T.Tax,T.CurrencyID,WH.Barcode,T.CreatedBy,T.CreatedDate,T.UpdatedBy,
T.UpdatedDate,T.Downloaded,T.VisitNo,T.RouteHistoryID,T.AccountID,T.PromotedDiscount,CO.CustomerTypeID,T.SalesMode,COL.Address,COL.Description,
SL.Description, CL.Description,CP.AppliedAmount,ed.DeviceSerial,WH.WarehouseCode,D.DivisionCode
                 
                ", sp);
                //the query above used to have date range : AND 
                //(T.TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
                //AND T.TransactionDate <= '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"')
                #region Invoice Header
                WriteExceptions("ENTERING THE TRANSACTION BLOCK", "TRANSACTION", false);
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

                int detailCounter = 0;
                #endregion
                foreach (DataRow dr in dt.Rows)
                {

                    #region Fill Header Variables

                    string DEP_CODE = string.Empty;
                    string DOC_PROV_NO = string.Empty;
                    string DOC_PROV_DATE = string.Empty;
                    string DOC_DATE = string.Empty;
                    string DT_CODE = string.Empty;
                    string SM_CODE = string.Empty;
                    string SAL_TYPE = string.Empty;
                    string DOC_TOTAL_AMOUNT = string.Empty;
                    string CUS_ACCOUNT_NO = string.Empty;
                    string DOC_DISCOUNT_PCT = string.Empty;
                    string DOC_DISCOUNT_AMOUNT = string.Empty;
                    string DOC_NARRATION = string.Empty;
                    string CREATION_DATE = string.Empty;
                    string CREATION_TERM = string.Empty;
                    string CUR_CODE = string.Empty;
                    string SD_EXCHANGE_RATE = string.Empty;
                    string SALES_MAN_LOC = string.Empty;
                    string PAY_AMOUNT = string.Empty;
                    string CUSTOMERTYPEID = string.Empty;
                    WriteExceptions("INSERTING TRANSACTION " + DOC_PROV_NO + " STARTED", "TRANSACTION", false);
                    DEP_CODE = "6G2";// dr["DEP_CODE"].ToString().Trim();
                    DOC_PROV_NO = dr["DOC_PROV_NO"].ToString().Trim();
                    DOC_PROV_DATE = dr["DOC_PROV_DATE"].ToString().Trim();
                    DOC_DATE = dr["DOC_DATE"].ToString().Trim();
                    DT_CODE = dr["DT_CODE"].ToString().Trim();
                    SM_CODE = dr["SM_CODE"].ToString().Trim();
                    SAL_TYPE = dr["SAL_TYPE"].ToString().Trim();
                    DOC_TOTAL_AMOUNT = dr["DOC_TOTAL_AMOUNT"].ToString().Trim();
                    CUS_ACCOUNT_NO = dr["CUS_ACCOUNT_NO"].ToString().Trim();
                    DOC_DISCOUNT_PCT = dr["DOC_DISCOUNT_PCT"].ToString().Trim();
                    DOC_DISCOUNT_AMOUNT = dr["DOC_DISCOUNT_AMOUNT"].ToString().Trim();
                    DOC_NARRATION = dr["DOC_NARRATION"].ToString().Trim();
                    CREATION_DATE = dr["CREATION_DATE"].ToString().Trim();
                    CREATION_TERM = dr["CREATION_TERM"].ToString().Trim();
                    CUR_CODE = dr["CUR_CODE"].ToString().Trim();
                    SD_EXCHANGE_RATE = dr["SD_EXCHANGE_RATE"].ToString().Trim();
                    SALES_MAN_LOC = dr["SALES_MAN_LOC"].ToString().Trim();
                    PAY_AMOUNT = dr["PAY_AMOUNT"].ToString().Trim();
                    CUSTOMERTYPEID = dr["CUSTOMERTYPEID"].ToString().Trim();
                    string DOC_SPECIAL_NAME = dr["DOC_SPECIAL_NAME"].ToString().Trim();
                    string IC_ACCOUNT_NO = CUS_ACCOUNT_NO;
                    if (SAL_TYPE.Equals("08"))
                    {
                        PAY_AMOUNT = DOC_TOTAL_AMOUNT;
                        CUS_ACCOUNT_NO = string.Empty;
                    }
                    else if (SAL_TYPE.Equals("07"))
                    {
                        IC_ACCOUNT_NO = string.Empty;
                    }
                    else if (SAL_TYPE.Equals("X"))
                    {
                        if (CUSTOMERTYPEID.Equals("1"))
                        {
                            SAL_TYPE = "08";
                            CUS_ACCOUNT_NO = string.Empty;
                            PAY_AMOUNT = DOC_TOTAL_AMOUNT;
                        }
                        else if (CUSTOMERTYPEID.Equals("2"))
                        {
                            SAL_TYPE = "07";
                            IC_ACCOUNT_NO = string.Empty;
                        }
                    }



                    detailCounter = 0;
                    _tran = Conn.BeginTransaction();
                    object field = new object();


                    #endregion


                    #region sub


                    OracleCommand cmdGet = new OracleCommand("select DOC_PROV_NO from OFF_SALE_DOCUMENTS where DOC_PROV_NO='" + DOC_PROV_NO + "'", Conn);
                    cmdGet.Transaction = _tran;
                    string ExistTransactions = GetValue(cmdGet);
                    cmdGet.Dispose();
                    if (ExistTransactions.Equals(string.Empty))
                    {
                        WriteExceptions(" TRANSACTION " + DOC_PROV_NO + " DOES NOT EXIST IN ORACLE", "TRANSACTION", false);
                        #region Insert the Header
                        ReportProgress("Sending Transactions");
                        string updatedDate = DateTime.Now.ToString(DateFormat);
                        string insertInvoices = string.Format(@"insert into OFF_SALE_DOCUMENTS
(DEP_CODE
,DOC_PROV_NO
,DOC_PROV_DATE
,DOC_DATE
,DT_CODE
,SM_CODE
,SAL_TYPE
,DOC_TOTAL_AMOUNT
,CUS_ACCOUNT_NO
,DOC_DISCOUNT_PCT
,DOC_DISCOUNT_AMOUNT
,DOC_NARRATION
,CREATION_DATE
,CREATION_TERM
,CUR_CODE
,SD_EXCHANGE_RATE
,SALES_MAN_LOC
,PAY_AMOUNT,SALE_REF_NO,DOC_SPECIAL_NAME,IC_ACCOUNT_NO)
Values
(
'" + DEP_CODE
+ "','" + DOC_PROV_NO
+ "','" + DateTime.Parse(DOC_PROV_DATE).ToString("dd-MMM-yyyy")
+ "','" + DateTime.Parse(DOC_DATE).ToString("dd-MMM-yyyy")
+ "','" + DT_CODE
+ "','" + SM_CODE
+ "','" + SAL_TYPE
+ "','" + DOC_TOTAL_AMOUNT
+ "','" + CUS_ACCOUNT_NO
+ "','" + DOC_DISCOUNT_PCT
+ "','" + DOC_DISCOUNT_AMOUNT
+ "','" + DOC_NARRATION
+ "','" + DateTime.Parse(CREATION_DATE).ToString("dd-MMM-yyyy")
+ "','" + CREATION_TERM
+ "','" + CUR_CODE
+ "','" + SD_EXCHANGE_RATE
+ "','" + SALES_MAN_LOC
+ "','" + PAY_AMOUNT + "','" + DOC_PROV_NO + "','" + DOC_SPECIAL_NAME + "','" + IC_ACCOUNT_NO + "')");
                        WriteExceptions("INSERTING TRANSACTION HEADER " + DOC_PROV_NO + " INSERT STATEMENT IS: " + insertInvoices + "", "TRANSACTION", false);
                        OracleCommand cmdHDR = new OracleCommand(insertInvoices, Conn);
                        cmdHDR.Transaction = _tran;
                        err = ExecuteNonQuery(cmdHDR, DOC_PROV_NO, "SALES");
                        cmdHDR.Dispose();
                        #endregion


                    #endregion
                #endregion
                        if (err == InCubeErrors.Success)
                        {
                            WriteExceptions("INSERTING TRANSACTION HEADER " + DOC_PROV_NO + " WAS SUCCESSFULL", "TRANSACTION", false);
                            #region Invoice details

                            tranDetail = string.Format(@"select CO.Barcode,td.TransactionID,I.PackDefinition IM_INV_NO,TD.BatchNo SSB_BATCH_NO,TD.ExpiryDate SSB_EXPIRY_DATE,BasePrice IM_INV_NO,BasePrice DD_SELL_PRICE,
(case (discount) when 0 then PRICE else  PRICE-(Discount/td.quantity) end) DD_SELL_PRICE_DISCOUNTED,(case (discount) when 0 then PRICE else  PRICE-(Discount/td.quantity) end) DD_SELL_PRICE_FINAL,
(baseprice*td.quantity-discount) DD_SELL_VALUE_FINAL,0 DD_SELL_COST_TOTAL,td.quantity DD_QTY,td.quantity DD_OUTSTANDING_QTY,case baseprice when 0 then 0 else (discount/(BasePrice*td.Quantity)) end DD_LINE_DISCOUNT_PCT,
discount DD_LINE_DISCOUNT_AMOUNT,(case(salestransactiontypeid) when (1) then 'N' else
case(salestransactiontypeid) when (5) then 'N' else 'Y' end end )   DD_FOC_IND ,
Pr.PromotionID PROMOTION_ID,td.quantity DD_INV_QTY,0 DD_TAX_RATE,0 DD_TAX_VALUE,0 SDD_COMM_PCT,P.barcode SSB_IM_BARCODE,
PL.PriceListCode PriceListCode

from TransactionDetail TD 
inner join CustomerOutlet CO on CO.CustomerID=TD.CustomerID and CO.OutletID=TD.OutletID
inner join Pack P on TD.PackID=P.PackID 
inner join Item I on I.ItemID=P.ItemID
INNER JOIN ITEMLANGUAGE IL ON I.ITEMID=IL.ITEMID AND IL.LANGUAGEID=1
INNER JOIN PACKTYPELANGUAGE PTL ON PTL.PACKTYPEID=P.PACKTYPEID AND PTL.LANGUAGEID=1
left outer join PromotionBenefitHistory pbh on pbh.TransactionID=td.transactionid and td.packid=pbh.packid and (td.Quantity=pbh.PackQuantity or td.discount=pbh.PackDiscountValue)
left outer join promotion pr on pbh.promotionid = pr.promotionid 
LEFT OUTER JOIN PriceList PL ON TD.UsedPriceListID = PL.PriceListID
                                where TD.TransactionID='" + DOC_PROV_NO + "'");

                            invoiceQry = new InCubeQuery(tranDetail, db_vms);
                            err = invoiceQry.Execute();
                            detailtbl = new DataTable();
                            detailtbl = invoiceQry.GetDataTable();
                            WriteExceptions("#################################################################", "TRANSACTION DETAIL", false);
                            WriteExceptions("NUMBER OF DETAILS FOR TRANSACTION", "TRANSACTION DETAIL", false);
                            WriteExceptions("NUMBER OF DETAILS FOR TRANSACTION ( " + DOC_PROV_NO + ") ARE " + detailtbl.Rows.Count.ToString() + "", "TRANSACTION DETAIL", false);
                            if (detailtbl.Rows.Count == 0)
                            {
                                string updateSync = string.Format("update [Transaction] set Synchronized=0 where TransactionID='" + DOC_PROV_NO + "'");
                                qry = new InCubeQuery(updateSync, db_vms);
                                err = qry.ExecuteNonQuery();
                                err = InCubeErrors.Error;
                            }
                            WriteExceptions("INSERTING TRANSACTION DETAIL  " + DOC_PROV_NO + " STARTED", "TRANSACTION", false);
                            detailCounter = 0;
                            foreach (DataRow det in detailtbl.Rows)
                            {
                                detailCounter++;

                                string DD_LINE_NO = string.Empty;
                                string IM_INV_NO = string.Empty;
                                string LOC_CODE = string.Empty;
                                string DD_SELL_PRICE = string.Empty;
                                string DD_SELL_PRICE_DISCOUNTED = string.Empty;
                                string DD_SELL_PRICE_FINAL = string.Empty;
                                string DD_SELL_VALUE_FINAL = string.Empty;
                                string DD_SELL_COST_TOTAL = string.Empty;
                                string DD_QTY = string.Empty;
                                string DD_OUTSTANDING_QTY = string.Empty;
                                string DD_LINE_DISCOUNT_PCT = string.Empty;
                                string DD_LINE_DISCOUNT_AMOUNT = string.Empty;
                                string DD_FOC_IND = string.Empty;
                                string PROMOTION_ID = string.Empty;
                                string DD_INV_QTY = string.Empty;
                                string DD_TAX_RATE = string.Empty;
                                string DD_TAX_VALUE = string.Empty;
                                string SDD_COMM_PCT = string.Empty;
                                string DD_STM_ID = string.Empty;

                                DD_LINE_NO = detailCounter.ToString();
                                IM_INV_NO = det["IM_INV_NO"].ToString().Trim();
                                LOC_CODE = SALES_MAN_LOC;// det["LOC_CODE"].ToString().Trim();
                                DD_SELL_PRICE = det["DD_SELL_PRICE"].ToString().Trim();
                                DD_SELL_PRICE_DISCOUNTED = det["DD_SELL_PRICE_DISCOUNTED"].ToString().Trim();
                                DD_SELL_PRICE_FINAL = det["DD_SELL_PRICE_FINAL"].ToString().Trim();
                                DD_SELL_VALUE_FINAL = det["DD_SELL_VALUE_FINAL"].ToString().Trim();
                                DD_SELL_COST_TOTAL = det["DD_SELL_COST_TOTAL"].ToString().Trim();
                                DD_QTY = det["DD_QTY"].ToString().Trim();
                                DD_OUTSTANDING_QTY = det["DD_OUTSTANDING_QTY"].ToString().Trim();
                                DD_LINE_DISCOUNT_PCT = det["DD_LINE_DISCOUNT_PCT"].ToString().Trim();
                                DD_LINE_DISCOUNT_AMOUNT = det["DD_LINE_DISCOUNT_AMOUNT"].ToString().Trim();
                                DD_FOC_IND = det["DD_FOC_IND"].ToString().Trim();
                                PROMOTION_ID = det["PROMOTION_ID"].ToString().Trim();
                                DD_INV_QTY = det["DD_INV_QTY"].ToString().Trim();
                                DD_TAX_RATE = det["DD_TAX_RATE"].ToString().Trim();
                                DD_TAX_VALUE = det["DD_TAX_VALUE"].ToString().Trim();
                                SDD_COMM_PCT = det["SDD_COMM_PCT"].ToString().Trim();
                                DD_STM_ID = det["PriceListCode"].ToString().Trim();

                                if (DD_FOC_IND.Equals("Y"))
                                {
                                    DD_SELL_PRICE = "0";
                                    DD_SELL_PRICE_DISCOUNTED = "0";
                                    DD_SELL_PRICE_FINAL = "0";
                                    DD_SELL_VALUE_FINAL = "0";
                                }

                                insertDetail = string.Format(@"INSERT INTO OFF_SALE_DOCUMENT_DETAILS
(DEP_CODE
,DOC_PROV_NO
,DD_LINE_NO
,IM_INV_NO
,LOC_CODE
,DD_SELL_PRICE
,DD_SELL_PRICE_DISCOUNTED
,DD_SELL_PRICE_FINAL
,DD_SELL_VALUE_FINAL
,DD_SELL_COST_TOTAL
,DD_QTY
,DD_OUTSTANDING_QTY
,DD_LINE_DISCOUNT_PCT
,DD_LINE_DISCOUNT_AMOUNT
,DD_FOC_IND
,PROMOTION_ID
,DD_INV_QTY
,DD_TAX_RATE
,DD_TAX_VALUE
,SDD_COMM_PCT
,SALE_REF_NO
,DD_STM_ID)
VALUES
(
'" + DEP_CODE
+ "','" + DOC_PROV_NO
+ "','" + DD_LINE_NO
+ "','" + IM_INV_NO
+ "','" + LOC_CODE
+ "','" + DD_SELL_PRICE
+ "','" + DD_SELL_PRICE_DISCOUNTED
+ "','" + DD_SELL_PRICE_FINAL
+ "','" + DD_SELL_VALUE_FINAL
+ "','" + DD_SELL_COST_TOTAL
+ "','" + DD_QTY
+ "','" + DD_OUTSTANDING_QTY
+ "','" + DD_LINE_DISCOUNT_PCT
+ "','" + DD_LINE_DISCOUNT_AMOUNT
+ "','" + DD_FOC_IND
+ "','" + PROMOTION_ID
+ "','" + DD_INV_QTY
+ "','" + DD_TAX_RATE
+ "','" + DD_TAX_VALUE
+ "','" + SDD_COMM_PCT
+ "','" + DOC_PROV_NO
+ "','" + DD_STM_ID
+ "')");


                                OracleCommand cmdDR = new OracleCommand(insertDetail, Conn);
                                cmdDR.Transaction = _tran;
                                err = ExecuteNonQuery(cmdDR, DOC_PROV_NO, "SALES");
                                if (err == InCubeErrors.Success)
                                {
                                    WriteExceptions("INSERTING TRANSACTION DETAIL" + DOC_PROV_NO + " WAS SUCCESSFULL", "TRANSACTION", false);
                                    WriteExceptions("INSERTING TRANSACTION BATCHES" + DOC_PROV_NO + " STARTED", "TRANSACTION", false);
                                    string SSB_DEP_CODE = DEP_CODE;
                                    string SSB_PROV_NO = DOC_PROV_NO;
                                    string SSB_IM_INV_NO = IM_INV_NO;
                                    string SSB_IM_BARCODE = det["SSB_IM_BARCODE"].ToString().Trim();
                                    string SSB_QTY = DD_QTY;
                                    string SSB_LINE_NO = detailCounter.ToString();
                                    string SSB_EXPIRY_DATE = det["SSB_EXPIRY_DATE"].ToString().Trim();
                                    string SSB_BATCH_NO = det["SSB_BATCH_NO"].ToString().Trim();
                                    string insertBatch = string.Format(@"INSERT INTO IC_SALES_BATCHES
(
SSB_DEP_CODE
,SSB_PROV_NO
,SSB_IM_INV_NO
,SSB_IM_BARCODE
,SSB_QTY
,SSB_LINE_NO
,SSB_EXPIRY_DATE
,SSB_BATCH_NO
)
VALUES
(
'" + SSB_DEP_CODE
+ "','" + SSB_PROV_NO
+ "','" + SSB_IM_INV_NO
+ "','" + SSB_IM_BARCODE
+ "','" + SSB_QTY
+ "','" + SSB_LINE_NO + "','" + DateTime.Parse(SSB_EXPIRY_DATE).ToString("dd-MMM-yyyy") + "','" + SSB_BATCH_NO + "')");
                                    cmdDR = new OracleCommand(insertBatch, Conn);
                                    cmdDR.Transaction = _tran;
                                    err = ExecuteNonQuery(cmdDR, DOC_PROV_NO, "SALES");
                                    if (err == InCubeErrors.Success)
                                    {
                                        WriteExceptions("INSERTING TRANSACTION BATCHES WAS SUCCESSFULL" + DOC_PROV_NO + "", "TRANSACTION", false);
                                    }
                                    else
                                    {
                                        WriteExceptions("INSERTING TRANSACTION BATCHES FAILED" + DOC_PROV_NO + " THE INSERT STATEMENT WAS " + insertBatch + "", "TRANSACTION", false);
                                        break;
                                    }
                                }
                                else
                                {
                                    WriteExceptions("INSERTING TRANSACTION DETAIL FAILED" + DOC_PROV_NO + " THE INSERT STATEMENT WAS " + insertDetail + "", "TRANSACTION", false);
                                    break;
                                }

                                cmdDR.Dispose();

                            }
                            #endregion
                        }

                        if (err == InCubeErrors.Success)
                        {
                            WriteExceptions("INSERTING ALL TRANSACTION WAS SUCCESSFULL.. UPDATING SYNCHRONIZED" + DOC_PROV_NO + "", "TRANSACTION", false);
                            string update = string.Format("update [Transaction] set Synchronized=1 where TransactionID='{0}'", DOC_PROV_NO);
                            invoiceQry = new InCubeQuery(update, db_vms);
                            err = invoiceQry.ExecuteNonQuery();
                            if (err == InCubeErrors.Success)
                            {
                                WriteExceptions("UPDATING SYNCHRONIZED FOR " + DOC_PROV_NO + " WAS SUCCESSFULL", "TRANSACTION", false);
                            }
                            else
                            {
                                WriteExceptions("UPDATING SYNCHRONIZED FOR " + DOC_PROV_NO + " FAILED !", "TRANSACTION", false);
                            }
                            WriteMessage("\r\n" + DOC_PROV_NO + " - Transaction sent Successfully");
                            TOTALINSERTED++;
                            _tran.Commit();
                        }
                        else
                        {
                            WriteMessage("\r\n" + DOC_PROV_NO + " - Transaction Failed");
                            _tran.Rollback();
                        }
                    }
                    else
                    {
                        WriteExceptions("TRANSACTION ALREADY EXIST IN ORACLE" + DOC_PROV_NO + "", "EXISTING TRANSACTION", false);
                        WriteExceptions("UPDATING SYNCHRONIZED FIELD " + DOC_PROV_NO + "", "EXISTING TRANSACTION", false);
                        string update = string.Format("update [Transaction] set Synchronized=1 where TransactionID='{0}'", DOC_PROV_NO);
                        invoiceQry = new InCubeQuery(update, db_vms);
                        err = invoiceQry.ExecuteNonQuery();
                        if (err == InCubeErrors.Success)
                        {
                            WriteExceptions("UPDATING SYNCHRONIZED FOR " + DOC_PROV_NO + " WAS SUCCESSFULL", "EXISTING TRANSACTION", false);
                        }
                        else
                        {
                            WriteExceptions("UPDATING SYNCHRONIZED FOR " + DOC_PROV_NO + " FAILED !", "EXISTING TRANSACTION", false);
                        }
                        WriteMessage("\r\n" + DOC_PROV_NO + " - Transaction Failed");
                        WriteExceptions(DOC_PROV_NO + " - Transaction exists in ORACLE", "error", true);
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
        #endregion

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
                DataTable dt;
                string sp = string.Empty;
                if (Filters.EmployeeID != -1)
                {
                    sp = " AND T.EmployeeID = " + Filters.EmployeeID;
                }
                string invoices = string.Format(@"select CO.CustomerCode,T.TransactionID,E.EmployeeCode,T.TransactionDate,
                    T.TransactionTypeID,T.DiscountAuthorization,T.Discount,Null,T.Synchronized,T.RemainingAmount,SourceTransactionID,T.GrossTotal,T.GPSLatitude
                    ,T.GPSLongitude,T.Voided,T.Notes,TransactionStatusID,RouteID,NetTotal,T.Tax,Posted,T.CurrencyID,WH.Barcode,T.CreatedBy,T.CreatedDate,T.UpdatedBy,
                    T.UpdatedDate,T.Downloaded,T.VisitNo,T.RouteHistoryID,T.AccountID,T.PromotedDiscount,d.DivisionID,(case(T.SalesMode) when 3 then 2 else T.SalesMode end ) as SalesMode,
                    COL.Address,(case(isnull(CO.CustomerTypeID,1)) when 1 then 'CASH'else cast(PT.SimplePeriodWidth as nvarchar(30)) end) as Term,COL.Description as CustName,
                    SL.Description as Street, CL.Description as City
                    from [Transaction] T
                    inner join CustomerOutlet CO on T.CustomerID=CO.CustomerID and T.OutletID=CO.OutletID
                    inner join CustomerOutletLanguage COL on COL.CustomerID=T.CustomerID and COL.OutletID=T.OutletID and LanguageID=1
                    left outer join PaymentTerm PT on CO.PaymentTermID=PT.PaymentTermID
                    left join Street S on CO.StreetID=S.StreetID
                    left join StreetLanguage SL on CO.StreetID=SL.StreetID and SL.LanguageID=1
                    left join City C on S.CityID=C.CityID and S.StateID=C.StateID and S.CountryID=C.CountryID
                    left join CityLanguage CL on CL.CityID=C.CityID and CL.LanguageID=1
                    inner join Employee E on T.EmployeeID=E.EmployeeID
                    inner join Warehouse WH on T.WarehouseID=WH.WarehouseID
                    left join Division d on T.DivisionID=d.DivisionID
                    where Synchronized=0 
                    AND (isnull(T.Notes,'0')<>'ERP') AND (T.TransactionTypeID = 2 or T.TransactionTypeID = 4) and T.Voided=0 {0}
                    group by 
                    CO.CustomerCode,T.TransactionID,E.EmployeeCode,T.TransactionDate,PT.SimplePeriodWidth,
                    T.TransactionTypeID,T.DiscountAuthorization,T.Discount,T.Synchronized,T.RemainingAmount,SourceTransactionID,T.GrossTotal,T.GPSLatitude
                    ,T.GPSLongitude,T.Voided,T.Notes,TransactionStatusID,RouteID,NetTotal,T.Tax,Posted,T.CurrencyID,WH.Barcode,T.CreatedBy,T.CreatedDate,T.UpdatedBy,
                    T.UpdatedDate,T.Downloaded,T.VisitNo,T.RouteHistoryID,T.AccountID,T.PromotedDiscount,d.DivisionID,CO.CustomerTypeID,T.SalesMode,COL.Address,COL.Description,
                    SL.Description, CL.Description
union
select CO.CustomerCode,T.TransactionID,E.EmployeeCode,T.TransactionDate,
                    T.TransactionTypeID,T.DiscountAuthorization,T.Discount,Null,T.Synchronized,T.RemainingAmount,SourceTransactionID,T.GrossTotal,T.GPSLatitude
                    ,T.GPSLongitude,T.Voided,T.Notes,TransactionStatusID,RouteID,NetTotal,T.Tax,Posted,T.CurrencyID,WH.Barcode,T.CreatedBy,T.CreatedDate,T.UpdatedBy,
                    T.UpdatedDate,T.Downloaded,T.VisitNo,T.RouteHistoryID,T.AccountID,T.PromotedDiscount,d.DivisionID,(case(T.SalesMode) when 3 then 2 else T.SalesMode end ) as SalesMode,
                    COL.Address,(case(isnull(CO.CustomerTypeID,1)) when 1 then 'CASH'else cast(PT.SimplePeriodWidth as nvarchar(30)) end) as Term,COL.Description as CustName,
                    SL.Description as Street, CL.Description as City
                    from [Transaction] T
                    inner join CustomerOutlet CO on T.CustomerID=CO.CustomerID and T.OutletID=CO.OutletID
                    inner join CustomerOutletLanguage COL on COL.CustomerID=T.CustomerID and COL.OutletID=T.OutletID and LanguageID=1
                    left outer join PaymentTerm PT on CO.PaymentTermID=PT.PaymentTermID
                    left join Street S on CO.StreetID=S.StreetID
                    left join StreetLanguage SL on CO.StreetID=SL.StreetID and SL.LanguageID=1
                    left join City C on S.CityID=C.CityID and S.StateID=C.StateID and S.CountryID=C.CountryID
                    left join CityLanguage CL on CL.CityID=C.CityID and CL.LanguageID=1
                    inner join Employee E on T.EmployeeID=E.EmployeeID
                    inner join Warehouse WH on T.WarehouseID=WH.WarehouseID
                    left join Division d on T.DivisionID=d.DivisionID
                    where Synchronized=0 
                    AND (isnull(T.Notes,'0')<>'ERP') AND (T.TransactionTypeID = 5) and T.Voided=0 and T.SourceTransactionID is null {0}
                    group by 
                    CO.CustomerCode,T.TransactionID,E.EmployeeCode,T.TransactionDate,PT.SimplePeriodWidth,
                    T.TransactionTypeID,T.DiscountAuthorization,T.Discount,T.Synchronized,T.RemainingAmount,SourceTransactionID,T.GrossTotal,T.GPSLatitude
                    ,T.GPSLongitude,T.Voided,T.Notes,TransactionStatusID,RouteID,NetTotal,T.Tax,Posted,T.CurrencyID,WH.Barcode,T.CreatedBy,T.CreatedDate,T.UpdatedBy,
                    T.UpdatedDate,T.Downloaded,T.VisitNo,T.RouteHistoryID,T.AccountID,T.PromotedDiscount,d.DivisionID,CO.CustomerTypeID,T.SalesMode,COL.Address,COL.Description,
                    SL.Description, CL.Description", sp);


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
                foreach (DataRow dr in dt.Rows)
                {
                    _tran = Conn.BeginTransaction();

                    string CSRH_SYS_ID = string.Empty;
                    //string maxSysID = @"select * from openquery(test,'select CSRH_SYS_ID.NextVal from OT_CUST_SALE_RET_HEAD')";
                    OracleCommand cmdGet = new OracleCommand("select CSRH_SYS_ID.NextVal  from DUAL", Conn);
                    cmdGet.Transaction = _tran;
                    string maxSysID = GetValue(cmdGet);
                    cmdGet.Dispose();

                    if (!maxSysID.Equals(string.Empty))
                    {
                        CSRH_SYS_ID = maxSysID;
                    }
                    else { return; }
                    string InvoiceNumberComplete = string.Empty;
                    string[] prefixArr = new string[2];
                    prefixArr = dr["TransactionID"].ToString().Trim().Split('-');
                    InvoiceNumberComplete = dr["TransactionID"].ToString().Trim();
                    string CSRH_COMP_CODE = "001";
                    string CSRH_TXN_CODE = prefixArr[0];
                    string CSRH_NO = prefixArr[1];
                    string CSRH_DT = DateTime.Parse(dr["TransactionDate"].ToString()).ToString(DateFormat);
                    string CSRH_DOC_SRC_LOCN_CODE = dr["Barcode"].ToString().Trim();
                    string CSRH_AMD_NO = "0";
                    string CSRH_REF_FROM = "D";
                    string CSRH_REF_FROM_NUM = "1";
                    string CSRH_LOCN_CODE = dr["Barcode"].ToString().Trim();
                    string CSRH_DEL_LOCN_CODE = dr["Barcode"].ToString().Trim();
                    string CSRH_CUST_CODE = dr["CustomerCode"].ToString().Trim();
                    string CSRH_CURR_CODE = "AED";
                    string CSRH_SHIP_TO_ADDR_CODE = dr["CustomerCode"].ToString().Trim();// dr["Address"].ToString().Trim();
                    string CSRH_BILL_TO_ADDR_CODE = dr["CustomerCode"].ToString().Trim();// dr["Address"].ToString().Trim();
                    string CSRH_EXGE_RATE = "1";
                    string CSRH_DISC_PERC = "0";
                    string CSRH_FC_DISC_VAL = "0";
                    string CSRH_SM_CODE = dr["EmployeeCode"].ToString().Trim();
                    string CSRH_STATUS = "NULL";
                    string CSRH_PRINT_STATUS = "NULL";
                    string CSRH_COS_FIN_STATUS = "NULL";
                    string CSRH_ANNOTATION = "INVAN " + dr["TransactionID"].ToString().Trim();
                    string CSRH_CUST_NAME = dr["CustName"].ToString().Trim().Replace("'", "''");
                    string CSRH_SHIP_ADDR_LINE_1 = dr["Address"].ToString().Trim();
                    string CSRH_SHIP_ADDR_LINE_2 = dr["Address"].ToString().Trim();
                    string CSRH_SHIP_ADDR_LINE_3 = dr["Address"].ToString().Trim();
                    string CSRH_CR_UID = dr["EmployeeCode"].ToString().Trim();
                    string CSRH_CR_DT = DateTime.Parse(dr["TransactionDate"].ToString()).ToString(DateFormat);
                    string NULL = "NULL";
                    string CSRI_GRADE_CODE_1 = "A";
                    string CSRI_GRADE_CODE_2 = "G2";
                    string CSRH_CAL_YEAR = DateTime.Parse(dr["TransactionDate"].ToString()).Year.ToString();
                    string CSRH_CAL_PERIOD = DateTime.Parse(dr["TransactionDate"].ToString()).Month.ToString();
                    cmdGet = new OracleCommand("select aper_acnt_year from FM_ACNT_PERIOD where aper_cal_year=" + CSRH_CAL_YEAR + " and aper_cal_month=" + CSRH_CAL_PERIOD + " and aper_comp_code='" + CSRH_COMP_CODE + "'", Conn);
                    cmdGet.Transaction = _tran;
                    string CSRH_ACNT_YEAR = GetValue(cmdGet);
                    string CSRH_APPR_STATUS = "1";


                    ReportProgress("Sending Returns");
                    string ff = dr[26].ToString();
                    string insertInvoices = string.Format(@"insert into OT_CUST_SALE_RET_HEAD (CSRH_SYS_ID,CSRH_COMP_CODE
                         ,CSRH_TXN_CODE
                         ,CSRH_NO 
                         ,CSRH_DT
                         ,CSRH_DOC_SRC_LOCN_CODE
                         ,CSRH_AMD_NO 
                         ,CSRH_REF_FROM 
                         ,CSRH_REF_FROM_NUM 
                         ,CSRH_LOCN_CODE 
                         ,CSRH_DEL_LOCN_CODE 
                         ,CSRH_CUST_CODE
                         ,CSRH_CURR_CODE
                         ,CSRH_SHIP_TO_ADDR_CODE 
                         ,CSRH_BILL_TO_ADDR_CODE
                         ,CSRH_EXGE_RATE
                         ,CSRH_DISC_PERC
                         ,CSRH_FC_DISC_VAL 
                         ,CSRH_SM_CODE
                         ,CSRH_STATUS
                         ,CSRH_PRINT_STATUS
                         ,CSRH_COS_FIN_STATUS
                         ,CSRH_ANNOTATION
                         ,CSRH_CUST_NAME 
                         ,CSRH_SHIP_ADDR_LINE_1
                         ,CSRH_SHIP_ADDR_LINE_2
                         ,CSRH_SHIP_ADDR_LINE_3 
                         ,CSRH_CR_UID
                         ,CSRH_CR_DT
                        ,CSRH_ACNT_YEAR
                        ,CSRH_CAL_YEAR
                        ,CSRH_CAL_PERIOD,CSRH_APPR_STATUS
                         ) VALUES 
                        (" + CSRH_SYS_ID
                        + ",'" + CSRH_COMP_CODE
                        + "','" + CSRH_TXN_CODE
                        + "'," + CSRH_NO
                        + ",'" + CSRH_DT
                        + "','" + CSRH_DOC_SRC_LOCN_CODE
                        + "'," + CSRH_AMD_NO
                        + ",'" + CSRH_REF_FROM
                        + "'," + CSRH_REF_FROM_NUM
                        + ",'" + CSRH_LOCN_CODE
                        + "','" + CSRH_DEL_LOCN_CODE
                        + "','" + CSRH_CUST_CODE
                        + "','" + CSRH_CURR_CODE
                        + "','" + CSRH_SHIP_TO_ADDR_CODE
                        + "','" + CSRH_BILL_TO_ADDR_CODE
                        + "'," + CSRH_EXGE_RATE
                        + "," + CSRH_DISC_PERC
                        + "," + CSRH_FC_DISC_VAL
                        + ",'" + CSRH_SM_CODE
                        + "'," + CSRH_STATUS
                        + "," + CSRH_PRINT_STATUS
                        + "," + CSRH_COS_FIN_STATUS
                        + ",'" + CSRH_ANNOTATION
                        + "','" + CSRH_CUST_NAME
                        + "','" + CSRH_SHIP_ADDR_LINE_1
                        + "','" + CSRH_SHIP_ADDR_LINE_2
                        + "','" + CSRH_SHIP_ADDR_LINE_3
                        + "','" + CSRH_CR_UID
                        + "','" + CSRH_CR_DT
                        + "'," + CSRH_ACNT_YEAR
                        + "," + CSRH_CAL_YEAR
                        + "," + CSRH_CAL_PERIOD
                        + "," + CSRH_APPR_STATUS + ")");
                    OracleCommand cmdHDR = new OracleCommand(insertInvoices, Conn);
                    cmdHDR.Transaction = _tran;
                    err = ExecuteNonQuery(cmdHDR, dr["TransactionID"].ToString().Trim(), "RETURN");
                    cmdHDR.Dispose();



                    if (err == InCubeErrors.Success)
                    {
                        if (!dr["TransactionTypeID"].ToString().Trim().Equals("5"))
                        {

                            tranDetail = string.Format(@"select CO.Barcode,TransactionID,I.ItemCode,P.Quantity UQTY,BatchNo,PackStatusID,TD.Quantity AS QTY,Price,BasePrice,
                                Tax,BaseTax,Warehoused,ExpiryDate,Discount,ReturnReason,SalesTransactionTypeID,BaseDiscount,FOCTypeID,Sequence,case(P.Quantity)when 1 then 1 else 2 end 
                                ,IL.DESCRIPTION AS ITEMNAME,PTL.DESCRIPTION AS UOM,P.PackID from 
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
                            foreach (DataRow det in detailtbl.Rows)
                            {
                                string CSRI_SYS_ID = string.Empty;

                                cmdGet = new OracleCommand("select CSRI_SYS_ID.NextVal from DUAL", Conn);
                                cmdGet.Transaction = _tran;
                                maxSysID = GetValue(cmdGet);
                                cmdGet.Dispose();

                                if (!maxSysID.Trim().Equals(string.Empty))
                                {
                                    CSRI_SYS_ID = maxSysID;
                                }
                                else { return; }
                                string PackID = det["PackID"].ToString().Trim();

                                string CSRI_CSRH_SYS_ID = CSRH_SYS_ID;
                                string CSRI_ITEM_CODE = det["ItemCode"].ToString().Trim();
                                string CSRI_ITEM_STK_YN_NUM = "1";
                                string CSRI_ITEM_DESC = det["ITEMNAME"].ToString().Trim().Replace("'", "''");
                                string CSRI_UOM_CODE = det["UOM"].ToString().Trim();
                                string CSRI_QTY = det["QTY"].ToString().Trim();
                                string CSRI_QTY_LS = "0";
                                string CSRI_QTY_BU = (decimal.Parse(det["UQTY"].ToString().Trim()) * decimal.Parse(det["QTY"].ToString().Trim())).ToString();
                                string CSRI_INVI_QTY_BU = CSRI_QTY;// "0";
                                string CSRI_DNI_QTY_BU = "0";
                                string CSRI_RATE = det["Price"].ToString().Trim();
                                string CSRI_DISC_PERC = "0";
                                string CSRI_FC_DISC_VAL = "0";
                                string CSRI_FC_VAL = (decimal.Parse(CSRI_RATE) * decimal.Parse(CSRI_QTY)).ToString();
                                string CSRI_FC_VAL_AFT_H_DISC = CSRI_FC_VAL;
                                string CSRI_FC_TAX_AMT = "0";
                                string CSRI_UPD_STK_YN = "Y";
                                string CSRI_LOCN_CODE = CSRH_LOCN_CODE;
                                string CSRI_DEL_LOCN_CODE = CSRH_LOCN_CODE;
                                string CSRI_SM_CODE = CSRH_SM_CODE;
                                string CSRI_CR_UID = CSRH_SM_CODE;
                                string CSRI_CR_DT = CSRH_DT;
                                string CSRI_FC_ACT_VAL = CSRI_RATE;
                                string CSRI_FLEX_01 = string.Empty;
                                if (decimal.Parse(CSRI_RATE) == 0)
                                {
                                    string PackGroupID = GetFieldValue("PackGroupDetail", "PackGroupID", "PackID=" + PackID + "", db_vms).Trim();
                                    string Alloc = GetFieldValue("VVS_AllocationCodes", "AllocationCode", "PackGroupID=" + PackGroupID + " ", db_vms).Trim();
                                    if (Alloc.Equals(string.Empty)) Alloc = "200369";
                                    CSRI_FLEX_01 = Alloc;
                                }

                                insertDetail = string.Format(@"insert into OT_CUST_SALE_RET_ITEM (
                                CSRI_SYS_ID	
                                ,CSRI_CSRH_SYS_ID	
                                ,CSRI_CSRR_SYS_ID	
                                ,CSRI_DNI_SYS_ID	
                                ,CSRI_INVI_SYS_ID	
                                ,CSRI_ITEM_CODE	
                                ,CSRI_ITEM_STK_YN_NUM	
                                ,CSRI_ITEM_DESC	
                                ,CSRI_UOM_CODE	
                                ,CSRI_QTY	
                                ,CSRI_QTY_LS	
                                ,CSRI_QTY_BU	
                                ,CSRI_INVI_QTY_BU	
                                ,CSRI_DNI_QTY_BU	
                                ,CSRI_RATE	
                                ,CSRI_DISC_PERC	
                                ,CSRI_FC_DISC_VAL	
                                ,CSRI_FC_VAL	
                                ,CSRI_FC_VAL_AFT_H_DISC	
                                ,CSRI_FC_TAX_AMT	
                                ,CSRI_UPD_STK_YN	
                                ,CSRI_ITEM_LENGTH	
                                ,CSRI_ITEM_WIDTH	
                                ,CSRI_ITEM_HEIGHT	
                                ,CSRI_ITEM_AREA	
                                ,CSRI_LOCN_CODE	
                                ,CSRI_DEL_LOCN_CODE	
                                ,CSRI_SM_CODE	
                                ,CSRI_BOE_NO	
                                ,CSRI_RES_CODE	
                                ,CSRI_ACTH_CODE	
                                ,CSRI_CR_UID	
                                ,CSRI_CR_DT	
                                ,CSRI_FC_ACT_VAL
                                ,CSRI_GRADE_CODE_1
                                ,CSRI_GRADE_CODE_2,CSRI_FLEX_01
                                )
                                Values
                                (" + CSRI_SYS_ID
                                    + "," + CSRI_CSRH_SYS_ID
                                    + "," + NULL
                                    + "," + NULL
                                    + "," + NULL
                                    + ",'" + CSRI_ITEM_CODE
                                    + "'," + CSRI_ITEM_STK_YN_NUM
                                    + ",'" + CSRI_ITEM_DESC.Replace("'", "")
                                    + "','" + CSRI_UOM_CODE
                                    + "'," + CSRI_QTY
                                    + "," + CSRI_QTY_LS
                                    + "," + CSRI_QTY_BU
                                    + "," + CSRI_INVI_QTY_BU
                                    + "," + CSRI_DNI_QTY_BU
                                    + "," + CSRI_RATE
                                    + "," + CSRI_DISC_PERC
                                    + "," + CSRI_FC_DISC_VAL
                                    + "," + CSRI_FC_VAL
                                    + "," + CSRI_FC_VAL_AFT_H_DISC
                                    + "," + CSRI_FC_TAX_AMT
                                    + ",'" + CSRI_UPD_STK_YN
                                    + "'," + NULL
                                    + "," + NULL
                                    + "," + NULL
                                    + "," + NULL
                                    + ",'" + CSRI_LOCN_CODE
                                    + "','" + CSRI_DEL_LOCN_CODE
                                    + "','" + CSRI_SM_CODE
                                    + "'," + NULL
                                    + "," + NULL
                                    + "," + NULL
                                    + ",'" + CSRI_SM_CODE
                                    + "','" + CSRI_CR_DT
                                    + "'," + CSRI_FC_ACT_VAL
                                    + ",'" + CSRI_GRADE_CODE_1
                                    + "','" + CSRI_GRADE_CODE_2 + "','" + CSRI_FLEX_01 + "'"
                                    + ")");


                                OracleCommand cmdDR = new OracleCommand(insertDetail, Conn);
                                cmdDR.Transaction = _tran;
                                err = ExecuteNonQuery(cmdDR, dr["TransactionID"].ToString().Trim(), "RETURN");
                                cmdDR.Dispose();

                                if (err != InCubeErrors.Success)
                                {
                                    break;
                                    //   WriteMessage("\r\n" + InvoiceNumberComplete + " - Transaction Details Failed");
                                }
                                else
                                {
                                    //                                string updateOS_LOCN_CURR = string.Format(@"UPDATE OS_LOCN_CURR_STK SET LCS_RCVD_QTY_BU=LCS_RCVD_QTY_BU+{0}
                                    //WHERE LCS_ITEM_CODE='{1}' AND LCS_GRADE_CODE_2='G2' AND LCS_GRADE_CODE_1='A' AND LCS_LOCN_CODE='{2}' AND LCS_COMP_CODE='{3}'", CSRI_QTY_BU, CSRI_ITEM_CODE,CSRH_DEL_LOCN_CODE,CSRH_COMP_CODE);
                                    //                                OracleCommand UPDT = new OracleCommand(updateOS_LOCN_CURR, Conn, _tran);
                                    //                                err = ExecuteNonQuery(UPDT);
                                    //                                cmdDR.Dispose();
                                    if (err != InCubeErrors.Success)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        #region AUTHENTICATION
                        if (err == InCubeErrors.Success)
                        {
                            string TAUTH_SYS_ID = string.Empty;
                            string TAUTH_HEAD_SYS_ID = string.Empty;
                            string TAUTH_COMP_CODE = string.Empty;
                            string TAUTH_TXN_CODE = string.Empty;
                            string TAUTH_DOC_NO = string.Empty;
                            string TAUTH_SEQ_NO = string.Empty;
                            string TAUTH_GROUP_CODE = string.Empty;
                            string TAUTH_APPR_UID = string.Empty;
                            string TAUTH_APPR_DT = string.Empty;
                            string TAUTH_REMARKS = string.Empty;
                            string TAUTH_CR_UID = string.Empty;
                            string TAUTH_CR_DT = string.Empty;
                            string TAUTH_UPD_UID = string.Empty;
                            string TAUTH_UPD_DT = string.Empty;
                            string TAUTH_DOC_DT = string.Empty;
                            cmdGet = new OracleCommand("select TAUTH_SYS_ID.NEXTVAL from DUAL", Conn);
                            cmdGet.Transaction = _tran;
                            maxSysID = GetValue(cmdGet);
                            cmdGet.Dispose();

                            if (!maxSysID.ToString().Trim().Equals(string.Empty))
                            {
                                TAUTH_SYS_ID = maxSysID;
                            }
                            else { return; }

                            TAUTH_HEAD_SYS_ID = CSRH_SYS_ID;
                            TAUTH_COMP_CODE = "001";
                            TAUTH_TXN_CODE = CSRH_TXN_CODE;
                            TAUTH_DOC_NO = CSRH_NO;
                            TAUTH_SEQ_NO = "1";
                            TAUTH_GROUP_CODE = "1";
                            TAUTH_APPR_UID = "ORION";
                            TAUTH_APPR_DT = DateTime.Now.ToString(DateFormat).Trim();
                            TAUTH_REMARKS = "INVAN";
                            TAUTH_CR_UID = "ORION";
                            TAUTH_CR_DT = DateTime.Now.ToString(DateFormat).Trim();
                            TAUTH_DOC_DT = CSRH_DT;
                            string InsertAuth = string.Format(@" INSERT INTO OT_TXN_AUTH(TAUTH_SYS_ID
,TAUTH_HEAD_SYS_ID
,TAUTH_COMP_CODE
,TAUTH_TXN_CODE
,TAUTH_DOC_NO
,TAUTH_SEQ_NO
,TAUTH_GROUP_CODE
,TAUTH_APPR_UID
,TAUTH_APPR_DT
,TAUTH_REMARKS
,TAUTH_CR_UID
,TAUTH_CR_DT
,TAUTH_DOC_DT) VALUES
(
" + TAUTH_SYS_ID +
"," + TAUTH_HEAD_SYS_ID +
",'" + TAUTH_COMP_CODE +
"','" + TAUTH_TXN_CODE
+ "'," + TAUTH_DOC_NO
+ "," + TAUTH_SEQ_NO
+ ",'" + TAUTH_GROUP_CODE
+ "','" + TAUTH_APPR_UID
+ "','" + TAUTH_APPR_DT
+ "','" + TAUTH_REMARKS
+ "','" + TAUTH_CR_UID
+ "','" + TAUTH_CR_DT
+ "','" + TAUTH_DOC_DT + "')");

                            cmdHDR = new OracleCommand(InsertAuth, Conn);
                            cmdHDR.Transaction = _tran;
                            err = ExecuteNonQuery(cmdHDR, dr["TransactionID"].ToString().Trim(), "RETURN");
                            cmdHDR.Dispose();

                        }

                        #endregion

                        if (err == InCubeErrors.Success)
                        {
                            string updateHD = string.Format("update [Transaction] set Synchronized=1 where TransactionID='{0}'", InvoiceNumberComplete);
                            invoiceQry = new InCubeQuery(updateHD, db_vms);
                            err = invoiceQry.ExecuteNonQuery();
                        }
                        if (err == InCubeErrors.Success)
                        {
                            WriteMessage("\r\n" + InvoiceNumberComplete + " - Transaction sent Successfully");
                            TOTALINSERTED++;
                            _tran.Commit();
                        }
                        else
                        {
                            WriteMessage("\r\n" + InvoiceNumberComplete + " - Transaction  Failed");

                            _tran.Rollback();
                        }

                    }
                    else
                    {
                        WriteMessage("\r\n" + InvoiceNumberComplete + " - Transaction Failed");
                        _tran.Rollback();
                    }
                }
                WriteMessage("\r\n");
                // WriteMessage("<<< Returns >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);

            }
            catch
            {
                if (_tran != null) _tran.Rollback();
            }
            finally
            { if (_tran != null) _tran.Dispose(); }
        }

        #endregion

        #region SendReciepts
        public override void SendReciepts()
        {
            if (Conn.State == ConnectionState.Closed)
            {
                Conn.Open();
            }

            OracleTransaction _tran = null;
            try
            {
                WriteExceptions("*****ENTERING THE PAYMENT SENDING BLOCK*****", "PAYMENTS", false);
                #region Another Query

                #endregion
                //SOME TRANSACTIONS ARE WITHOUT DETAILS ==> CUSTOMER PAYMENT FOR THOSE TRANSACTIONS WILL NOT APPEAR BECAUSE OF DIVISION ID
                //TO DO : Handle the date format
                DataTable dt = new DataTable();
                string receipt = string.Format(@"select CO.CustomerCode CUST_LDGR_ACNO,CP.PaymentDate AR_DOC_DATE,CP.CustomerPaymentID AR_SRL_NO,CP.TransactionID IN_SRL_NO,
(case(CP.PaymentTypeID) when 1 then 'CSH' ELSE 
CASE(CP.PaymentTypeID) when 2 then 'CHQ' ELSE
CASE(CP.PaymentTypeID) when 3 then 'PDC' ELSE
CASE(CP.PaymentTypeID) when 4 then 'CN' ELSE'XX' END END END END) AR_DOC_TYPE,E.EmployeeCode EMPLOYEE_NUMBER,
CP.VoucherNumber AR_CHEQUE_NUMBER,CP.VoucherDate AR_CHEQUE_DATE,CP.VoucherOwner AR_CHEQUE_OWNER,B.Code AR_BANK_CODE,B.Code,CP.AppliedAmount AR_NET_AMOUNT,
substring(D.DivisionCode,1,1) AR_COMPANY_NUMBER,'6' COMPANY_NUMBER
from CustomerPayment CP 
inner join CustomerOutlet CO on CO.CustomerID=CP.CustomerID and CO.OutletID=CP.OutletID 
inner join Employee E on CP.EmployeeID=E.EmployeeID 
left outer join Bank B on CP.BankID=B.BankID 
inner join division d on d.DivisionID=cp.DivisionID
where (CP.Synchronized = 0) 
 and CP.PaymentStatusID <>5 and CP.EmployeeID<>0
Group By
CO.CustomerCode,CP.PaymentDate,CP.CustomerPaymentID,CP.TransactionID,CP.PaymentTypeID,E.EmployeeCode,
CP.VoucherNumber,CP.VoucherDate,CP.VoucherOwner,B.Code,B.Code,CP.Notes,CP.CurrencyID,CP.AppliedAmount,D.DivisionCode
                     ");
                if (Filters.EmployeeID != -1)
                {
                    receipt += "    AND CustomerPayment.EmployeeID = " + Filters.EmployeeID;
                }
                InCubeQuery receiptQry = new InCubeQuery(receipt, db_vms);
                err = receiptQry.Execute();
                dt = receiptQry.GetDataTable();

                string insertReceipt = string.Empty;
                string proddate = string.Empty;
                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;
                ClearProgress();
                SetProgressMax(dt.Rows.Count);
                WriteExceptions("NUMBER OF RECEIPTS IS " + dt.Rows.Count + "", "RECEIPTS", false);
                foreach (DataRow dr in dt.Rows)
                {
                    _tran = Conn.BeginTransaction();

                    string AR_COMPANY_NUMBER = dr["AR_COMPANY_NUMBER"].ToString().Trim();
                    string AR_SRL_NO = dr["AR_SRL_NO"].ToString().Trim();
                    WriteExceptions("SENDING PAYMENT <" + AR_SRL_NO + "> ", "RECEIPTS", false);
                    string IN_SRL_NO = dr["IN_SRL_NO"].ToString().Trim();
                    string CUST_LDGR_ACNO = dr["CUST_LDGR_ACNO"].ToString().Trim();
                    string AR_DOC_DATE = dr["AR_DOC_DATE"].ToString().Trim();
                    string AR_DOC_TYPE = dr["AR_DOC_TYPE"].ToString().Trim();
                    string AR_NET_AMOUNT = dr["AR_NET_AMOUNT"].ToString().Trim();
                    string AR_CHEQUE_NUMBER = dr["AR_CHEQUE_NUMBER"].ToString().Trim();
                    string AR_CHEQUE_DATE = dr["AR_CHEQUE_DATE"].ToString().Trim();
                    if (!AR_CHEQUE_DATE.Equals(string.Empty))
                    { AR_CHEQUE_DATE = DateTime.Parse(AR_CHEQUE_DATE).ToString("dd-MMM-yyyy"); }
                    string AR_CHEQUE_OWNER = dr["AR_CHEQUE_OWNER"].ToString().Trim();
                    string AR_BANK_CODE = dr["AR_BANK_CODE"].ToString().Trim();
                    string COMPANY_NUMBER = dr["COMPANY_NUMBER"].ToString().Trim();
                    string EMPLOYEE_NUMBER = dr["EMPLOYEE_NUMBER"].ToString().Trim();
                    string UPLD_FLAG = "N";
                    WriteExceptions("CHECKING IF THIS PAYMENT ALREADY EXIST IN ORACLE ", "RECEIPTS", false);
                    OracleCommand cmdGet = new OracleCommand(" select AR_SRL_NO from IC_VAN_REC_COLLS where AR_SRL_NO='" + AR_SRL_NO + "' AND IN_SRL_NO='" + IN_SRL_NO + "'", Conn);
                    cmdGet.Transaction = _tran;
                    string EXIST_PAYMENT = GetValue(cmdGet);
                    //string existInERP = GetFieldValue("CustomerPayment", "CustomerPaymentID", "CustomerPaymentID='" + dr[2].ToString().Trim() + "' and TransactionID='" + dr[3].ToString().Trim() + "'", db_vms).Trim();
                    if (!EXIST_PAYMENT.Equals(string.Empty))
                    {
                        WriteExceptions("THE PAYMENT " + AR_SRL_NO + " ALREADY EXIST", "RECEIPTS", false);
                        continue;
                    }
                    else
                    {
                        WriteExceptions("THE PAYMENT " + AR_SRL_NO + " DOES NOT EXIST... INSERTING", "RECEIPTS", false);
                    }
                    TOTALINSERTED++;
                    ReportProgress("Sending Receipts");

                    string insertPayment = string.Format(@"INSERT INTO IC_VAN_REC_COLLS
(
AR_COMPANY_NUMBER
,AR_SRL_NO
,IN_SRL_NO
,CUST_LDGR_ACNO
,AR_DOC_DATE
,AR_DOC_TYPE
,AR_NET_AMOUNT
,AR_CHEQUE_NUMBER
,AR_CHEQUE_DATE
,AR_CHEQUE_OWNER
,AR_BANK_CODE
,COMPANY_NUMBER
,EMPLOYEE_NUMBER
,UPLD_FLAG
)
VALUES
(
'" + AR_COMPANY_NUMBER
+ "','" + AR_SRL_NO
+ "','" + IN_SRL_NO
+ "','" + CUST_LDGR_ACNO
+ "','" + DateTime.Parse(AR_DOC_DATE).ToString("dd-MMM-yyyy")
+ "','" + AR_DOC_TYPE
+ "','" + AR_NET_AMOUNT
+ "','" + AR_CHEQUE_NUMBER
+ "','" + AR_CHEQUE_DATE
+ "','" + AR_CHEQUE_OWNER
+ "','" + AR_BANK_CODE
+ "','" + COMPANY_NUMBER
+ "','" + EMPLOYEE_NUMBER
+ "','" + UPLD_FLAG + "')"
);
                    WriteExceptions("THE PAYMENT INSERT STATEMENT IS << " + insertPayment + ">> ", "RECEIPTS", false);
                    OracleCommand cmdHDR = new OracleCommand(insertPayment, Conn);
                    cmdHDR.Transaction = _tran;
                    err = ExecuteNonQuery(cmdHDR, AR_SRL_NO, "PAY");
                    cmdHDR.Dispose();

                    if (err != InCubeErrors.Success)
                    {
                        WriteExceptions("THE PAYMENT " + AR_SRL_NO + " INSERT FAILED", "RECEIPTS", false);
                        _tran.Rollback();
                        WriteMessage("\r\n" + dr[2].ToString() + " - Receipt Failed");
                    }
                    else
                    {
                        WriteExceptions("THE PAYMENT " + AR_SRL_NO + " INSERT SUCCEEDED", "RECEIPTS", false);
                        WriteExceptions("UPDATING INVAN CUSTOMERPAYMENT TABLE", "RECEIPTS", false);
                        string update = string.Format("update CustomerPayment set Synchronized=1 where CustomerPaymentID='{0}' and TransactionID='{1}'", dr[2].ToString().Trim(), dr[3].ToString().Trim());
                        receiptQry = new InCubeQuery(update, db_vms);
                        err = receiptQry.ExecuteNonQuery();
                        if (err != InCubeErrors.Success)
                        {
                            WriteExceptions("FAILED TO UPDATE INVAN CUSTOMERPAYMENT TABLE", "RECEIPTS", false);
                            _tran.Rollback();
                            WriteMessage("\r\n" + dr[2].ToString() + " - Receipt Failed");
                        }
                        else
                        {
                            WriteExceptions("SUCCESS UPDATING INVAN CUSTOMERPAYMENT TABLE", "RECEIPTS", false);
                            _tran.Commit();
                            WriteMessage("\r\n" + dr[2].ToString() + " - Receipt Transfered Successfully");

                        }
                    }

                }
                WriteMessage("\r\n");
                WriteMessage("<<< Receipts >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);

            }
            catch (Exception ex)
            {
                WriteExceptions("HANDLED EXCEPTION << " + ex.Message + ">> ", "RECEIPTS", false);
                if (_tran != null) _tran.Rollback();

            }
            finally
            { if (_tran != null)_tran.Dispose(); }
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
select WH.Barcode LOC_CODE,WHT.TransactionID ADJ_DOC_NO,TransactionTypeID,TransactionDate ADJ_DATE ,EE.EmployeeCode,E.EmployeeCode UP_NAME,
'notes',Synchronized,isnull(WHT.ProductionDate,getdate()),WHH.Barcode LOC_CODE_TO,Downloaded,WarehouseTransactionStatusID,D.DivisionCode DEP_CODE
,'10' AR_CODE,'DT' DT_CODE,'0' ADJ_GRV_NO,D.DivisionCode FROM_DEP,D.DivisionCode TO_DEP--,WHT.Notes ADJ_REMARKS

From WarehouseTransaction WHT inner join Warehouse WH on WHT.WarehouseID=WH.WarehouseID 
inner join Warehouse WHH on WHT.RefWarehouseID=WHH.WarehouseID
inner join Employee E on WHT.ImplementedBy=E.EmployeeID 
inner join Employee EE on WHT.RequestedBy=EE.EmployeeID
inner join Division D on D.DivisionID=WHT.divisionID
where WHT.Synchronized=0 and (WHT.TransactionTypeID=1 or WHT.TransactionTypeID=2) and (WarehouseTransactionStatusID=2 or WarehouseTransactionStatusID=1 )

Group By
WH.Barcode,WHT.TransactionID,TransactionTypeID,TransactionDate,EE.EmployeeCode,E.EmployeeCode,
Synchronized,WHT.ProductionDate,WHH.Barcode,Posted,Downloaded,WarehouseTransactionStatusID,CreationSourceID,D.DivisionID,D.DivisionCode
 
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
                    _tran = Conn.BeginTransaction();
                    object field = new object();
                    ReportProgress("Sending Transfers");

                    string DEP_CODE = string.Empty;
                    string ADJ_DOC_NO = string.Empty;
                    string AR_CODE = string.Empty;
                    string DT_CODE = string.Empty;
                    string UP_NAME = string.Empty;
                    string ADJ_GRV_NO = string.Empty;
                    string ADJ_DATE = string.Empty;
                    string LOC_CODE = string.Empty;
                    string LOC_CODE_TO = string.Empty;
                    string FROM_DEP = string.Empty;
                    string TO_DEP = string.Empty;
                    string ADJ_REMARKS = string.Empty;
                    string tr_Type_ID = string.Empty;


                    DEP_CODE = "VBA";// dr["DEP_CODE"].ToString().Trim();
                    ADJ_DOC_NO = dr["ADJ_DOC_NO"].ToString().Trim();
                    AR_CODE = dr["AR_CODE"].ToString().Trim();
                    DT_CODE = dr["DT_CODE"].ToString().Trim();
                    UP_NAME = dr["UP_NAME"].ToString().Trim();
                    ADJ_GRV_NO = dr["ADJ_GRV_NO"].ToString().Trim();
                    ADJ_DATE = dr["ADJ_DATE"].ToString().Trim();
                    LOC_CODE = dr["LOC_CODE"].ToString().Trim();
                    LOC_CODE_TO = dr["LOC_CODE_TO"].ToString().Trim();
                    FROM_DEP = "VBA";// dr["FROM_DEP"].ToString().Trim();
                    TO_DEP = "VBA";// dr["TO_DEP"].ToString().Trim();
                    ADJ_REMARKS = "INVAN";
                    tr_Type_ID = dr["TransactionTypeID"].ToString().Trim();

                    OracleCommand cmdGet = new OracleCommand("select ADJ_DOC_NO from IC_ADJUSTMENTS where ADJ_DOC_NO='" + ADJ_DOC_NO + "' and DEP_CODE='VBA'", Conn);
                    cmdGet.Transaction = _tran;
                    string ExistTransactions = GetValue(cmdGet);
                    cmdGet.Dispose();
                    if (ExistTransactions.Equals(string.Empty))
                    {
                        ReportProgress("Sending Transactions");

                        string updatedDate = DateTime.Now.ToString(DateFormat);
                        string insertLR = string.Format(@"insert into IC_ADJUSTMENTS (
DEP_CODE
,ADJ_DOC_NO
,AR_CODE
,DT_CODE
,UP_NAME
,ADJ_GRV_NO
,ADJ_DATE
,LOC_CODE
,LOC_CODE_TO
,FROM_DEP
,TO_DEP
,ADJ_REMARKS
,CR_DR_FLAG
)
VALUES
('" + DEP_CODE
+ "','" + ADJ_DOC_NO
+ "','" + AR_CODE
+ "','" + DT_CODE
+ "','" + UP_NAME
+ "','" + ADJ_GRV_NO
+ "','" + DateTime.Parse(ADJ_DATE).ToString("dd-MMM-yyyy")
+ "','" + LOC_CODE_TO
+ "','" + LOC_CODE
+ "','" + FROM_DEP
+ "','" + TO_DEP
+ "','" + ADJ_REMARKS
+ "','" + tr_Type_ID
+ "')");

                        OracleCommand cmdHDR = new OracleCommand(insertLR, Conn);
                        cmdHDR.Transaction = _tran;
                        err = ExecuteNonQuery(cmdHDR, ADJ_DOC_NO, "WH");
                        cmdHDR.Dispose();
                    #endregion

                        if (err == InCubeErrors.Success)
                        {
                            #region DETAILS
                            string transfDet = string.Format(@"select TransactionID ADJ_DOC_NO,I.PackDefinition IM_INV_NO,ExpiryDate IM_EXPIRY_DATE,WHT.Quantity as AD_QTY,BatchNo IM_BATCH_NO
					,D.DivisionCode as DEP_CODE,0 AD_VALUE

from WhTransDetail WHT 
inner join Warehouse WH on WH.WarehouseID=WHT.WarehouseID 
inner join Pack P on WHT.PackID=P.PackID 
inner join Item I on I.ItemID=P.ItemID
inner join ItemLanguage IL on I.ItemID=IL.ItemID and IL.languageID=1
inner join PackTypeLanguage PTL on P.PackTypeID=PTL.PackTypeID and PTL.LanguageID=1
inner join Division D on D.DivisionID=WHT.DivisionID
where TransactionID='" + ADJ_DOC_NO + "'");
                            transferQry = new InCubeQuery(transfDet, db_vms);
                            err = transferQry.Execute();
                            detailDT = transferQry.GetDataTable();
                            string proddate = string.Empty;
                            string expi = string.Empty;
                            WriteExceptions("#################################################################", "WAREHOUSE TRANSACTION DETAIL", false);
                            WriteExceptions("NUMBER OF DETAILS FOR WAREHOUSE TRANSACTION IS " + detailDT.Rows.Count.ToString() + "", "WAREHOUSE TRANSACTION DETAIL", false);
                            WriteExceptions("NUMBER OF DETAILS FOR TRANSACTION ( " + ADJ_DOC_NO + ") ARE " + detailDT.Rows.Count.ToString() + "", "WAREHOUSE TRANSACTION DETAIL", false);
                            if (detailDT.Rows.Count == 0)
                            {

                                string updateSync = string.Format("update WarehouseTransaction set Synchronized=0 where TransactionID='" + ADJ_DOC_NO + "'");
                                qry = new InCubeQuery(updateSync, db_vms);
                                err = qry.ExecuteNonQuery();
                                err = InCubeErrors.Error;
                            }
                            int counter = 0;
                            foreach (DataRow det in detailDT.Rows)
                            {
                                counter++;
                                //string DEP_CODE = string.Empty;
                                //string ADJ_DOC_NO = string.Empty;
                                string AD_LINE_NO = string.Empty;
                                string IM_INV_NO = string.Empty;
                                string AD_QTY = string.Empty;
                                string AD_VALUE = string.Empty;
                                string AD_REMARK = string.Empty;

                                AD_LINE_NO = counter.ToString();
                                IM_INV_NO = det["IM_INV_NO"].ToString().Trim();
                                AD_QTY = det["AD_QTY"].ToString().Trim();
                                AD_VALUE = "0";
                                AD_REMARK = "INVAN";

                                string insertdetailWH = string.Format(@"insert into IC_ADJUSTMENT_DETAILS
(
DEP_CODE
,ADJ_DOC_NO
,AD_LINE_NO
,IM_INV_NO
,AD_QTY
,AD_VALUE
,AD_REMARK
,LOC_CODE
,IC_CODE
)
values
('" + DEP_CODE
+ "','" + ADJ_DOC_NO
+ "','" + AD_LINE_NO
+ "','" + IM_INV_NO
+ "','" + AD_QTY
+ "','" + AD_VALUE
+ "','" + AD_REMARK + "','" + LOC_CODE_TO + "',1)");

                                OracleCommand cmdDR = new OracleCommand(insertdetailWH, Conn);
                                cmdDR.Transaction = _tran;
                                err = ExecuteNonQuery(cmdDR, ADJ_DOC_NO, "WH");
                                cmdDR.Dispose();
                                if (err == InCubeErrors.Success)
                                {
                                    string SAB_DEP_CODE = DEP_CODE;
                                    string SAB_DOC_NO = ADJ_DOC_NO;
                                    string SAB_IM_INV_NO = IM_INV_NO;
                                    string SAB_QTY = AD_QTY;
                                    string IM_EXPIRY_DATE = det["IM_EXPIRY_DATE"].ToString().Trim();
                                    string IM_BATCH_NO = det["IM_BATCH_NO"].ToString().Trim();


                                    string insertBatch = string.Format(@"INSERT INTO IC_ADJ_BATCHES
(
SAB_DEP_CODE
,SAB_DOC_NO
,SAB_IM_INV_NO
,SAB_QTY
,IM_EXPIRY_DATE
,IM_BATCH_NO

)
VALUES
(
'" + SAB_DEP_CODE
+ "','" + SAB_DOC_NO
+ "','" + SAB_IM_INV_NO
+ "','" + SAB_QTY
+ "','" + DateTime.Parse(IM_EXPIRY_DATE).ToString("dd-MMM-yyyy")
+ "','" + IM_BATCH_NO + "')");
                                    cmdDR = new OracleCommand(insertBatch, Conn);
                                    cmdDR.Transaction = _tran;
                                    err = ExecuteNonQuery(cmdDR, SAB_DOC_NO, "WH");
                                    if (err == InCubeErrors.Success)
                                    {

                                    }
                                    else
                                    {
                                        break;
                                    }

                                }
                                else
                                {
                                    break;
                                }
                            }
                            #endregion
                        }
                        if (err == InCubeErrors.Success)
                        {
                            string update = string.Format("update [WarehouseTransaction] set Synchronized=1 where TransactionID='{0}'", ADJ_DOC_NO);
                            qry = new InCubeQuery(update, db_vms);
                            err = qry.ExecuteNonQuery();
                            WriteMessage("\r\n" + ADJ_DOC_NO + " - Transaction sent Successfully");
                            TOTALINSERTED++;
                            _tran.Commit();
                        }
                        else
                        {
                            WriteMessage("\r\n" + ADJ_DOC_NO + " - Transaction Failed");
                            _tran.Rollback();
                        }
                    }
                    else
                    {
                        WriteMessage("\r\n" + ADJ_DOC_NO + " - Transaction Failed");
                        WriteExceptions(ADJ_DOC_NO + " - Transaction exists in Orion", "error", true);
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

        #region SendOrders
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
                /*
                 select substring(COL.DESCRIPTION,1,50) DOC_SPECIAL_NAME, CO.CustomerTypeID, CO.CustomerCode CUS_ACCOUNT_NO,T.TransactionID DOC_PROV_NO,E.EmployeeCode SM_CODE,T.TransactionDate DOC_PROV_DATE,T.TransactionDate DOC_DATE,T.TransactionDate CREATION_DATE,
(case(T.TransactionTypeID) when (1) then 'IN' else 
case(T.TransactionTypeID) when (3) then 'IN' else
case(T.TransactionTypeID) when (2) then 'CR' else
case(T.TransactionTypeID) when (4) then 'CR' else 'XX' end end end end) as DT_CODE,(T.Discount+T.promotedDiscount) DOC_DISCOUNT_AMOUNT,((T.Discount+T.promotedDiscount)/(case(NetTotal) when 0 then 1 else NetTotal end)) DOC_DISCOUNT_PCT,
T.Synchronized,T.RemainingAmount,T.GrossTotal,T.GPSLatitude,ed.DeviceSerial CREATION_TERM,'QAR' CUR_CODE,1 SD_EXCHANGE_RATE
,T.GPSLongitude,T.Voided,T.Notes DOC_NARRATION,NetTotal DOC_TOTAL_AMOUNT,
T.RouteHistoryID,(case(T.SalesMode) when 1 then '08' else case(T.SalesMode) when 2 then '07' else 'X' end end) as SAL_TYPE,
WH.WarehouseCode SALES_MAN_LOC,
SL.Description as Street, CL.Description as City,D.DivisionCode DEP_CODE,CP.AppliedAmount PAY_AMOUNT,'' SRM_ID
from [Transaction] T
inner join CustomerOutlet CO on T.CustomerID=CO.CustomerID and T.OutletID=CO.OutletID
inner join CustomerOutletLanguage COL on COL.CustomerID=T.CustomerID and COL.OutletID=T.OutletID and LanguageID=1
left outer join PaymentTerm PT on CO.PaymentTermID=PT.PaymentTermID
left join Street S on CO.StreetID=S.StreetID
left join StreetLanguage SL on CO.StreetID=SL.StreetID and SL.LanguageID=1
left join City C on S.CityID=C.CityID and S.StateID=C.StateID and S.CountryID=C.CountryID
left join CityLanguage CL on CL.CityID=C.CityID and CL.LanguageID=1
inner join Employee E on T.EmployeeID=E.EmployeeID
left outer join EmployeeDevice ed on E.EmployeeID=ed.EmployeeID
inner join Warehouse WH on T.WarehouseID=WH.WarehouseID 
inner join Division D on D.DivisionID=T.DivisionID
left outer join CustomerPayment cp on T.transactionID=cp.transactionID and CP.RemainingAmount=T.RemainingAmount
where T.Synchronized=0  AND 
(isnull(T.Notes,'0')<>'ERP') AND (T.TransactionTypeID in(1,2,3,4)) and T.Voided=0 
group by 
CO.CustomerCode,T.TransactionID,E.EmployeeCode,T.TransactionDate,PT.SimplePeriodWidth,
T.TransactionTypeID,T.DiscountAuthorization,T.Discount,T.Synchronized,T.RemainingAmount,T.GrossTotal,T.GPSLatitude
,T.GPSLongitude,T.Voided,T.Notes,TransactionStatusID,RouteID,NetTotal,T.Tax,T.CurrencyID,WH.Barcode,T.CreatedBy,T.CreatedDate,T.UpdatedBy,
T.UpdatedDate,T.Downloaded,T.VisitNo,T.RouteHistoryID,T.AccountID,T.PromotedDiscount,CO.CustomerTypeID,T.SalesMode,COL.Address,COL.Description,
SL.Description, CL.Description,CP.AppliedAmount,ed.DeviceSerial,WH.WarehouseCode,D.DivisionCode
                 
                 */

                string invoices = string.Format(@"
                  select substring(COL.DESCRIPTION,1,50) DOC_SPECIAL_NAME, CO.CustomerTypeID, CO.CustomerCode CUS_ACCOUNT_NO,T.ORDERID DOC_PROV_NO,E.EmployeeCode SM_CODE,T.ORDERDATE DOC_PROV_DATE,
T.ORDERDATE DOC_DATE,T.ORDERDATE CREATION_DATE,
'IN' as DT_CODE,(T.Discount+T.promotedDiscount) DOC_DISCOUNT_AMOUNT,((T.Discount+T.promotedDiscount)/(case(NetTotal) when 0 then 1 else NetTotal end)) DOC_DISCOUNT_PCT,
T.Synchronized,T.GrossTotal,T.GPSLatitude,ed.DeviceSerial CREATION_TERM,'QAR' CUR_CODE,1 SD_EXCHANGE_RATE
,T.GPSLongitude,SON.Note DOC_NARRATION,NetTotal DOC_TOTAL_AMOUNT,
T.RouteHistoryID,
(case(CO.CustomerTypeID) when 1 then '01' else case(CO.CustomerTypeID) when 2 then '02' else 'X' end end) as SAL_TYPE,
WH.WarehouseCode SALES_MAN_LOC,
SL.Description as Street, CL.Description as City,'' DEP_CODE,'' SRM_ID,T.DesiredDeliveryDate AS DOC_EXP_DELIVERY_DATE
from SalesORder T
inner join CustomerOutlet CO on T.CustomerID=CO.CustomerID and T.OutletID=CO.OutletID
inner join CustomerOutletLanguage COL on COL.CustomerID=T.CustomerID and COL.OutletID=T.OutletID and LanguageID=1
left outer join PaymentTerm PT on CO.PaymentTermID=PT.PaymentTermID
left join Street S on CO.StreetID=S.StreetID
left join StreetLanguage SL on CO.StreetID=SL.StreetID and SL.LanguageID=1
left join City C on S.CityID=C.CityID and S.StateID=C.StateID and S.CountryID=C.CountryID
left join CityLanguage CL on CL.CityID=C.CityID and CL.LanguageID=1
LEFT OUTER JOIN SalesOrderNote SON ON T.ORDERID=SON.OrderID
inner join Employee E on t.EmployeeID=E.EmployeeID
left outer join EmployeeDevice ed on E.EmployeeID=ed.EmployeeID
LEFT OUTER JOIN EmployeeVehicle EV ON E.EmployeeID=EV.EmployeeID
left outer join Warehouse WH on EV.VEHICLEID=WH.WarehouseID 
where T.Synchronized=0  AND 
(isnull(SON.Note,'0')<>'ERP') and T.OrderStatusID in (1,2) 
group by 
CO.CustomerCode,T.ORDERID,E.EmployeeCode,T.ORDERDATE,PT.SimplePeriodWidth,
T.Discount,T.Synchronized,T.GrossTotal,T.GPSLatitude
,T.GPSLongitude,SON.Note,NetTotal,T.Tax,WH.Barcode,T.CreatedBy,T.CreatedDate,T.UpdatedBy,
T.UpdatedDate,T.Downloaded,T.VisitNo,T.RouteHistoryID,T.PromotedDiscount,CO.CustomerTypeID,COL.Address,COL.Description,
SL.Description, CL.Description,ed.DeviceSerial,WH.WarehouseCode,T.DesiredDeliveryDate

                ");
                //the query above used to have date range : AND 
                //(T.TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
                //AND T.TransactionDate <= '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"')
                #region Invoice Header
                WriteExceptions("ENTERING THE TRANSACTION BLOCK", "TRANSACTION", false);
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

                int detailCounter = 0;
                #endregion
                foreach (DataRow dr in dt.Rows)
                {

                    #region Fill Header Variables

                    string DEP_CODE = string.Empty;
                    string DOC_PROV_NO = string.Empty;
                    string DOC_PROV_DATE = string.Empty;
                    string DOC_DATE = string.Empty;
                    string DT_CODE = string.Empty;
                    string SM_CODE = string.Empty;
                    string SAL_TYPE = string.Empty;
                    string DOC_TOTAL_AMOUNT = string.Empty;
                    string CUS_ACCOUNT_NO = string.Empty;
                    string DOC_DISCOUNT_PCT = string.Empty;
                    string DOC_DISCOUNT_AMOUNT = string.Empty;
                    string DOC_NARRATION = string.Empty;
                    string CREATION_DATE = string.Empty;
                    string CREATION_TERM = string.Empty;
                    string CUR_CODE = string.Empty;
                    string SD_EXCHANGE_RATE = string.Empty;
                    string SALES_MAN_LOC = string.Empty;
                    string PAY_AMOUNT = string.Empty;
                    string CUSTOMERTYPEID = string.Empty;
                    WriteExceptions("INSERTING TRANSACTION " + DOC_PROV_NO + " STARTED", "TRANSACTION", false);
                    DEP_CODE = "VBA";// "6G2";// dr["DEP_CODE"].ToString().Trim();
                    DOC_PROV_NO = dr["DOC_PROV_NO"].ToString().Trim();
                    DOC_PROV_DATE = dr["DOC_PROV_DATE"].ToString().Trim();
                    DOC_DATE = dr["DOC_DATE"].ToString().Trim();
                    DT_CODE = dr["DT_CODE"].ToString().Trim();
                    SM_CODE = dr["SM_CODE"].ToString().Trim();
                    SAL_TYPE = dr["SAL_TYPE"].ToString().Trim();
                    DOC_TOTAL_AMOUNT = dr["DOC_TOTAL_AMOUNT"].ToString().Trim();
                    CUS_ACCOUNT_NO = dr["CUS_ACCOUNT_NO"].ToString().Trim();
                    DOC_DISCOUNT_PCT = dr["DOC_DISCOUNT_PCT"].ToString().Trim();
                    DOC_DISCOUNT_AMOUNT = dr["DOC_DISCOUNT_AMOUNT"].ToString().Trim();
                    DOC_NARRATION = dr["DOC_NARRATION"].ToString().Trim();
                    CREATION_DATE = dr["CREATION_DATE"].ToString().Trim();
                    CREATION_TERM = dr["CREATION_TERM"].ToString().Trim();
                    CUR_CODE = dr["CUR_CODE"].ToString().Trim();
                    SD_EXCHANGE_RATE = dr["SD_EXCHANGE_RATE"].ToString().Trim();
                    SALES_MAN_LOC = dr["SALES_MAN_LOC"].ToString().Trim();
                    CUSTOMERTYPEID = dr["CUSTOMERTYPEID"].ToString().Trim();
                    string DOC_SPECIAL_NAME = dr["DOC_SPECIAL_NAME"].ToString().Trim();
                    //string IC_ACCOUNT_NO = CUS_ACCOUNT_NO;

                    string DOC_EXP_DELIVERY_DATE = dr["DOC_EXP_DELIVERY_DATE"].ToString().Trim();
                    string SRM_ID = dr["SRM_ID"].ToString().Trim();

                    //if (SAL_TYPE.Equals("08"))
                    //{
                    //    PAY_AMOUNT = DOC_TOTAL_AMOUNT;
                    //    CUS_ACCOUNT_NO = string.Empty;
                    //}
                    //else if (SAL_TYPE.Equals("07"))
                    //{
                    //    IC_ACCOUNT_NO = string.Empty;
                    //}
                    //else if (SAL_TYPE.Equals("X"))
                    //{
                    //    if (CUSTOMERTYPEID.Equals("1"))
                    //    {
                    //        SAL_TYPE = "08";
                    //        CUS_ACCOUNT_NO = string.Empty;
                    //        PAY_AMOUNT = DOC_TOTAL_AMOUNT;
                    //    }
                    //    else if (CUSTOMERTYPEID.Equals("2"))
                    //    {
                    //        SAL_TYPE = "07";
                    //        IC_ACCOUNT_NO = string.Empty;
                    //    }
                    //}



                    detailCounter = 0;
                    _tran = Conn.BeginTransaction();
                    object field = new object();

                    #endregion


                    #region sub


                    OracleCommand cmdGet = new OracleCommand("select DOC_PROV_NO from OFF_SALE_DOCUMENTS where DOC_PROV_NO='" + DOC_PROV_NO + "'", Conn);
                    cmdGet.Transaction = _tran;
                    string ExistTransactions = GetValue(cmdGet);
                    cmdGet.Dispose();
                    if (ExistTransactions.Equals(string.Empty))
                    {
                        WriteExceptions(" TRANSACTION " + DOC_PROV_NO + " DOES NOT EXIST IN ORACLE", "TRANSACTION", false);
                        #region Insert the Header
                        ReportProgress("Sending Transactions");

                        string updatedDate = DateTime.Now.ToString(DateFormat);
                        string insertInvoices = string.Format(@"insert into ICORD_SALE_DOCUMENTS
(DEP_CODE
,DOC_PROV_NO
,DOC_PROV_DATE
,DOC_DATE
,DT_CODE
,SM_CODE
,SAL_TYPE
,DOC_TOTAL_AMOUNT
,CUS_ACCOUNT_NO
,DOC_DISCOUNT_PCT
,DOC_DISCOUNT_AMOUNT
,DOC_NARRATION
,CREATION_DATE
,CREATION_TERM
,CUR_CODE
,SD_EXCHANGE_RATE
,SALES_MAN_LOC
,SRM_ID,DOC_EXP_DELIVERY_DATE,DOC_STATUS)
Values
(
'" + DEP_CODE
+ "','" + DOC_PROV_NO
+ "',TO_DATE('" + DateTime.Parse(DOC_PROV_DATE).ToString("dd-MMM-yyyy hh:mm:ss")
+ "','DD-MON-YY HH24:MI:SS'),TO_DATE('" + DateTime.Parse(DOC_DATE).ToString("dd-MMM-yyyy hh:mm:ss")
+ "','DD-MON-YY HH24:MI:SS'),'" + DT_CODE
+ "','" + SM_CODE
+ "','" + SAL_TYPE
+ "','" + DOC_TOTAL_AMOUNT
+ "','" + CUS_ACCOUNT_NO
+ "','" + DOC_DISCOUNT_PCT
+ "','" + DOC_DISCOUNT_AMOUNT
+ "','" + DOC_NARRATION
+ "',TO_DATE('" + DateTime.Parse(CREATION_DATE).ToString("dd-MMM-yyyy hh:mm:ss")
+ "','DD-MON-YY HH24:MI:SS'),'" + CREATION_TERM
+ "','" + CUR_CODE
+ "','" + SD_EXCHANGE_RATE
+ "','" + SALES_MAN_LOC
+ "','" + SRM_ID + "',TO_DATE('" + DateTime.Parse(DOC_EXP_DELIVERY_DATE).ToString("dd-MMM-yyyy hh:mm:ss") + "','DD-MON-YY HH24:MI:SS'),1)");
                        WriteExceptions("INSERTING TRANSACTION HEADER " + DOC_PROV_NO + " INSERT STATEMENT IS: " + insertInvoices + "", "TRANSACTION", false);
                        OracleCommand cmdHDR = new OracleCommand(insertInvoices, Conn);
                        cmdHDR.Transaction = _tran;
                        err = ExecuteNonQuery(cmdHDR, DOC_PROV_NO, "SALES");
                        cmdHDR.Dispose();
                        #endregion
                        

                    #endregion
                #endregion
                        if (err == InCubeErrors.Success)
                        {
                            WriteExceptions("INSERTING TRANSACTION HEADER " + DOC_PROV_NO + " WAS SUCCESSFULL", "TRANSACTION", false);
                            #region Invoice details

                            tranDetail = string.Format(@"select CO.Barcode,td.ORDERID,I.PackDefinition IM_INV_NO,(CASE(BASEPRICE) WHEN 0 THEN PRICE ELSE BASEPRICE END) IM_INV_NO,(CASE(BASEPRICE) WHEN 0 THEN PRICE ELSE BASEPRICE END) DD_SELL_PRICE,
(case (discount) when 0 then (CASE(BASEPRICE) WHEN 0 THEN PRICE ELSE BASEPRICE END) else  (CASE(BASEPRICE) WHEN 0 THEN PRICE ELSE BASEPRICE END)-(Discount/td.quantity) end) DD_SELL_PRICE_DISCOUNTED,(case (discount) when 0 then (CASE(BASEPRICE) WHEN 0 THEN PRICE ELSE BASEPRICE END) else  (CASE(BASEPRICE) WHEN 0 THEN PRICE ELSE BASEPRICE END)-(Discount/td.quantity) end) DD_SELL_PRICE_FINAL,
((CASE(BASEPRICE) WHEN 0 THEN PRICE ELSE BASEPRICE END)*td.quantity-discount) DD_SELL_VALUE_FINAL,0 DD_SELL_COST_TOTAL,td.quantity DD_QTY,td.quantity DD_OUTSTANDING_QTY,case baseprice when 0 then 0 else (discount/((CASE(BASEPRICE) WHEN 0 THEN PRICE ELSE BASEPRICE END)*td.Quantity)) end DD_LINE_DISCOUNT_PCT,
discount DD_LINE_DISCOUNT_AMOUNT,(case(salestransactiontypeid) when (1) then 'N' else
case(salestransactiontypeid) when (5) then 'N' else 'Y' end end )   DD_FOC_IND ,
Pr.PromotionID PROMOTION_ID,td.quantity DD_INV_QTY,0 DD_TAX_RATE,0 DD_TAX_VALUE,0 SDD_COMM_PCT,P.barcode SSB_IM_BARCODE

from SALESORDERDETAIL TD 
inner join CustomerOutlet CO on CO.CustomerID=TD.CustomerID and CO.OutletID=TD.OutletID
inner join Pack P on TD.PackID=P.PackID 
inner join Item I on I.ItemID=P.ItemID
INNER JOIN ITEMLANGUAGE IL ON I.ITEMID=IL.ITEMID AND IL.LANGUAGEID=1
INNER JOIN PACKTYPELANGUAGE PTL ON PTL.PACKTYPEID=P.PACKTYPEID AND PTL.LANGUAGEID=1
left outer join PromotionBenefitHistory pbh on pbh.TransactionID=td.ORDERID and td.packid=pbh.packid 
left outer join promotion pr on pbh.promotionid = pr.promotionid 
                                where TD.ORDERID='" + DOC_PROV_NO + "'");

                            invoiceQry = new InCubeQuery(tranDetail, db_vms);
                            err = invoiceQry.Execute();
                            detailtbl = new DataTable();
                            detailtbl = invoiceQry.GetDataTable();
                            WriteExceptions("#################################################################", "TRANSACTION DETAIL", false);
                            WriteExceptions("NUMBER OF DETAILS FOR TRANSACTION", "TRANSACTION DETAIL", false);
                            WriteExceptions("NUMBER OF DETAILS FOR TRANSACTION ( " + DOC_PROV_NO + ") ARE " + detailtbl.Rows.Count.ToString() + "", "TRANSACTION DETAIL", false);
                            if (detailtbl.Rows.Count == 0)
                            {
                                string updateSync = string.Format("update [SALESORDER] set Synchronized=0 where ORDERID='" + DOC_PROV_NO + "'");
                                qry = new InCubeQuery(updateSync, db_vms);
                                err = qry.ExecuteNonQuery();
                                err = InCubeErrors.Error;
                            }
                            WriteExceptions("INSERTING TRANSACTION DETAIL  " + DOC_PROV_NO + " STARTED", "TRANSACTION", false);
                            detailCounter = 0;
                            foreach (DataRow det in detailtbl.Rows)
                            {
                                detailCounter++;


                                string DD_LINE_NO = string.Empty;
                                string IM_INV_NO = string.Empty;
                                string LOC_CODE = string.Empty;
                                string DD_SELL_PRICE = string.Empty;
                                string DD_SELL_PRICE_DISCOUNTED = string.Empty;
                                string DD_SELL_PRICE_FINAL = string.Empty;
                                string DD_SELL_VALUE_FINAL = string.Empty;
                                string DD_SELL_COST_TOTAL = string.Empty;
                                string DD_QTY = string.Empty;
                                string DD_OUTSTANDING_QTY = string.Empty;
                                string DD_LINE_DISCOUNT_PCT = string.Empty;
                                string DD_LINE_DISCOUNT_AMOUNT = string.Empty;
                                string DD_FOC_IND = string.Empty;
                                string PROMOTION_ID = string.Empty;
                                string DD_INV_QTY = string.Empty;
                                string DD_TAX_RATE = string.Empty;
                                string DD_TAX_VALUE = string.Empty;
                                string SDD_COMM_PCT = string.Empty;

                                DD_LINE_NO = detailCounter.ToString();
                                IM_INV_NO = det["IM_INV_NO"].ToString().Trim();
                                LOC_CODE = SALES_MAN_LOC;// det["LOC_CODE"].ToString().Trim();
                                DD_SELL_PRICE = det["DD_SELL_PRICE"].ToString().Trim();
                                DD_SELL_PRICE_DISCOUNTED = det["DD_SELL_PRICE_DISCOUNTED"].ToString().Trim();
                                DD_SELL_PRICE_FINAL = det["DD_SELL_PRICE_FINAL"].ToString().Trim();
                                DD_SELL_VALUE_FINAL = det["DD_SELL_VALUE_FINAL"].ToString().Trim();
                                DD_SELL_COST_TOTAL = det["DD_SELL_COST_TOTAL"].ToString().Trim();
                                DD_QTY = det["DD_QTY"].ToString().Trim();
                                DD_OUTSTANDING_QTY = det["DD_OUTSTANDING_QTY"].ToString().Trim();
                                DD_LINE_DISCOUNT_PCT = det["DD_LINE_DISCOUNT_PCT"].ToString().Trim();
                                DD_LINE_DISCOUNT_AMOUNT = det["DD_LINE_DISCOUNT_AMOUNT"].ToString().Trim();
                                DD_FOC_IND = det["DD_FOC_IND"].ToString().Trim();
                                PROMOTION_ID = det["PROMOTION_ID"].ToString().Trim();
                                DD_INV_QTY = det["DD_INV_QTY"].ToString().Trim();
                                DD_TAX_RATE = det["DD_TAX_RATE"].ToString().Trim();
                                DD_TAX_VALUE = det["DD_TAX_VALUE"].ToString().Trim();
                                SDD_COMM_PCT = det["SDD_COMM_PCT"].ToString().Trim();
                                if (DD_FOC_IND.Equals("Y"))
                                {
                                    DD_SELL_PRICE = "0";
                                    DD_SELL_PRICE_DISCOUNTED = "0";
                                    DD_SELL_PRICE_FINAL = "0";
                                    DD_SELL_VALUE_FINAL = "0";
                                }

                                insertDetail = string.Format(@"INSERT INTO icord_sale_document_details
(DEP_CODE
,DOC_PROV_NO
,DD_LINE_NO
,IM_INV_NO
,LOC_CODE
,DD_SELL_PRICE
,DD_SELL_PRICE_DISCOUNTED
,DD_SELL_PRICE_FINAL
,DD_SELL_VALUE_FINAL
,DD_SELL_COST_TOTAL
,DD_QTY
,DD_OUTSTANDING_QTY
,DD_LINE_DISCOUNT_PCT
,DD_LINE_DISCOUNT_AMOUNT
,DD_FOC_IND
,PROMOTION_ID
,DD_INV_QTY
,DD_TAX_RATE
,DD_TAX_VALUE
,SDD_COMM_PCT,DD_STATUS)
VALUES
(
'" + DEP_CODE
+ "','" + DOC_PROV_NO
+ "','" + DD_LINE_NO
+ "','" + IM_INV_NO
+ "','" + LOC_CODE
+ "','" + DD_SELL_PRICE
+ "','" + DD_SELL_PRICE_DISCOUNTED
+ "','" + DD_SELL_PRICE_FINAL
+ "','" + DD_SELL_VALUE_FINAL
+ "','" + DD_SELL_COST_TOTAL
+ "','" + DD_QTY
+ "','" + DD_OUTSTANDING_QTY
+ "','" + DD_LINE_DISCOUNT_PCT
+ "','" + DD_LINE_DISCOUNT_AMOUNT
+ "','" + DD_FOC_IND
+ "','" + PROMOTION_ID
+ "','" + DD_INV_QTY
+ "','" + DD_TAX_RATE
+ "','" + DD_TAX_VALUE
+ "','" + SDD_COMM_PCT + "','0')");


                                OracleCommand cmdDR = new OracleCommand(insertDetail, Conn);
                                cmdDR.Transaction = _tran;
                                err = ExecuteNonQuery(cmdDR, DOC_PROV_NO, "SALES");


                                cmdDR.Dispose();

                            }
                            #endregion
                        }


                        if (err == InCubeErrors.Success)
                        {
                            WriteExceptions("INSERTING ALL TRANSACTION WAS SUCCESSFULL.. UPDATING SYNCHRONIZED" + DOC_PROV_NO + "", "TRANSACTION", false);
                            string update = string.Format("update [SalesOrder] set Synchronized=1 where OrderID='{0}'", DOC_PROV_NO);
                            invoiceQry = new InCubeQuery(update, db_vms);
                            err = invoiceQry.ExecuteNonQuery();
                            WriteMessage("\r\n" + DOC_PROV_NO + " - Transaction sent Successfully");
                            TOTALINSERTED++;
                            _tran.Commit();
                        }
                        else
                        {
                            WriteMessage("\r\n" + DOC_PROV_NO + " - Transaction Failed");
                            _tran.Rollback();
                        }
                    }
                    else
                    {
                        WriteExceptions("TRANSACTION ALREADY EXIST IN ORACLE" + DOC_PROV_NO + "", "TRANSACTION", false);
                        WriteMessage("\r\n" + DOC_PROV_NO + " - Transaction Failed");
                        WriteExceptions(DOC_PROV_NO + " - Transaction exists in ORACLE", "error", true);
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
        #endregion

        #region SendOrderInvoices
        public override void SendOrderInvoices()
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
                string invoices = string.Format(@"select substring(COL.DESCRIPTION,1,50) DOC_SPECIAL_NAME, CO.CustomerTypeID, CO.CustomerCode CUS_ACCOUNT_NO,T.TransactionID DOC_PROV_NO,E.EmployeeCode SM_CODE,T.TransactionDate DOC_PROV_DATE,T.TransactionDate DOC_DATE,T.TransactionDate CREATION_DATE,
(case(T.TransactionTypeID) when (1) then 'IN' else 
case(T.TransactionTypeID) when (3) then 'IN' else
case(T.TransactionTypeID) when (2) then 'CR' else
case(T.TransactionTypeID) when (4) then 'CR' else 'XX' end end end end) as DT_CODE,(T.Discount+T.promotedDiscount) DOC_DISCOUNT_AMOUNT,((T.Discount+T.promotedDiscount)/(case(T.NetTotal) when 0 then 1 else T.NetTotal end)) DOC_DISCOUNT_PCT,
T.Synchronized,T.RemainingAmount,T.GrossTotal,T.GPSLatitude,ed.DeviceSerial CREATION_TERM,'QAR' CUR_CODE,1 SD_EXCHANGE_RATE
,T.GPSLongitude,T.Voided,T.Notes DOC_NARRATION,TransactionStatusID,RouteID,T.NetTotal DOC_TOTAL_AMOUNT,
T.RouteHistoryID,(case(T.SalesMode) when 1 then '08' else case(T.SalesMode) when 2 then '07' else 'X' end end) as SAL_TYPE,
WH.WarehouseCode SALES_MAN_LOC,
SL.Description as Street, CL.Description as City,D.DivisionCode DEP_CODE,CP.AppliedAmount PAY_AMOUNT,'' SRM_ID,T.SourceTransactionID doc_no_have_previous,T.SourceTransactionID dd_line_no_have_previous
from [Transaction] T
inner join CustomerOutlet CO on T.CustomerID=CO.CustomerID and T.OutletID=CO.OutletID
inner join CustomerOutletLanguage COL on COL.CustomerID=T.CustomerID and COL.OutletID=T.OutletID and LanguageID=1
left outer join PaymentTerm PT on CO.PaymentTermID=PT.PaymentTermID
left join Street S on CO.StreetID=S.StreetID
left join StreetLanguage SL on CO.StreetID=SL.StreetID and SL.LanguageID=1
left join City C on S.CityID=C.CityID and S.StateID=C.StateID and S.CountryID=C.CountryID
left join CityLanguage CL on CL.CityID=C.CityID and CL.LanguageID=1
inner join Employee E on T.EmployeeID=E.EmployeeID
left outer join EmployeeDevice ed on E.EmployeeID=ed.EmployeeID
LEFT OUTER join Warehouse WH on T.WarehouseID=WH.WarehouseID 
inner join Division D on D.DivisionID=T.DivisionID
left outer join CustomerPayment cp on T.transactionID=cp.transactionID and CP.RemainingAmount=T.RemainingAmount
INNER JOIN SALESORDER SOR ON T.SourceTransactionID=SOR.ORDERID
where T.Synchronized=0  AND 
(isnull(T.Notes,'0')<>'ERP') AND (T.TransactionTypeID in(1)) and T.Voided=0 and T.SourceTransactionID is not null {0}
group by 
CO.CustomerCode,T.TransactionID,E.EmployeeCode,T.TransactionDate,PT.SimplePeriodWidth,
T.TransactionTypeID,T.DiscountAuthorization,T.Discount,T.Synchronized,T.RemainingAmount,T.GrossTotal,T.GPSLatitude
,T.GPSLongitude,T.Voided,T.Notes,TransactionStatusID,RouteID,T.NetTotal,T.Tax,T.CurrencyID,WH.Barcode,T.CreatedBy,T.CreatedDate,T.UpdatedBy,
T.UpdatedDate,T.Downloaded,T.VisitNo,T.RouteHistoryID,T.AccountID,T.PromotedDiscount,CO.CustomerTypeID,T.SalesMode,COL.Address,COL.Description,
SL.Description, CL.Description,CP.AppliedAmount,ed.DeviceSerial,WH.WarehouseCode,D.DivisionCode,T.SourceTransactionID
                 
                ", sp);
                //the query above used to have date range : AND 
                //(T.TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
                //AND T.TransactionDate <= '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"')
                #region Invoice Header
                WriteExceptions("ENTERING THE TRANSACTION BLOCK", "TRANSACTION", false);
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

                int detailCounter = 0;
                #endregion
                foreach (DataRow dr in dt.Rows)
                {

                    #region Fill Header Variables

                    string DEP_CODE = string.Empty;
                    string DOC_PROV_NO = string.Empty;
                    string DOC_PROV_DATE = string.Empty;
                    string DOC_DATE = string.Empty;
                    string DT_CODE = string.Empty;
                    string SM_CODE = string.Empty;
                    string SAL_TYPE = string.Empty;
                    string DOC_TOTAL_AMOUNT = string.Empty;
                    string CUS_ACCOUNT_NO = string.Empty;
                    string DOC_DISCOUNT_PCT = string.Empty;
                    string DOC_DISCOUNT_AMOUNT = string.Empty;
                    string DOC_NARRATION = string.Empty;
                    string CREATION_DATE = string.Empty;
                    string CREATION_TERM = string.Empty;
                    string CUR_CODE = string.Empty;
                    string SD_EXCHANGE_RATE = string.Empty;
                    string SALES_MAN_LOC = string.Empty;
                    string PAY_AMOUNT = string.Empty;
                    string CUSTOMERTYPEID = string.Empty;
                    string doc_no_have_previous = string.Empty;
                    string dd_line_no_have_previous = string.Empty;

                    doc_no_have_previous = dr["doc_no_have_previous"].ToString().Trim();
                    dd_line_no_have_previous = dr["dd_line_no_have_previous"].ToString().Trim();
                    WriteExceptions("INSERTING TRANSACTION " + DOC_PROV_NO + " STARTED", "TRANSACTION", false);
                    DEP_CODE = "VBA";// dr["DEP_CODE"].ToString().Trim();
                    DOC_PROV_NO = dr["DOC_PROV_NO"].ToString().Trim();
                    DOC_PROV_DATE = dr["DOC_PROV_DATE"].ToString().Trim();
                    DOC_DATE = dr["DOC_DATE"].ToString().Trim();
                    DT_CODE = dr["DT_CODE"].ToString().Trim();
                    SM_CODE = dr["SM_CODE"].ToString().Trim();
                    SAL_TYPE = dr["SAL_TYPE"].ToString().Trim();
                    DOC_TOTAL_AMOUNT = dr["DOC_TOTAL_AMOUNT"].ToString().Trim();
                    CUS_ACCOUNT_NO = dr["CUS_ACCOUNT_NO"].ToString().Trim();
                    DOC_DISCOUNT_PCT = dr["DOC_DISCOUNT_PCT"].ToString().Trim();
                    DOC_DISCOUNT_AMOUNT = dr["DOC_DISCOUNT_AMOUNT"].ToString().Trim();
                    DOC_NARRATION = dr["DOC_NARRATION"].ToString().Trim();
                    CREATION_DATE = dr["CREATION_DATE"].ToString().Trim();
                    CREATION_TERM = dr["CREATION_TERM"].ToString().Trim();
                    CUR_CODE = dr["CUR_CODE"].ToString().Trim();
                    SD_EXCHANGE_RATE = dr["SD_EXCHANGE_RATE"].ToString().Trim();
                    SALES_MAN_LOC = dr["SALES_MAN_LOC"].ToString().Trim();
                    PAY_AMOUNT = dr["PAY_AMOUNT"].ToString().Trim();
                    CUSTOMERTYPEID = dr["CUSTOMERTYPEID"].ToString().Trim();
                    string DOC_SPECIAL_NAME = dr["DOC_SPECIAL_NAME"].ToString().Trim();
                    string IC_ACCOUNT_NO = CUS_ACCOUNT_NO;
                    if (SAL_TYPE.Equals("08"))
                    {
                        PAY_AMOUNT = DOC_TOTAL_AMOUNT;
                        CUS_ACCOUNT_NO = string.Empty;
                    }
                    else if (SAL_TYPE.Equals("07"))
                    {
                        IC_ACCOUNT_NO = string.Empty;
                    }
                    else if (SAL_TYPE.Equals("X"))
                    {
                        if (CUSTOMERTYPEID.Equals("1"))
                        {
                            SAL_TYPE = "08";
                            CUS_ACCOUNT_NO = string.Empty;
                            PAY_AMOUNT = DOC_TOTAL_AMOUNT;
                        }
                        else if (CUSTOMERTYPEID.Equals("2"))
                        {
                            SAL_TYPE = "07";
                            IC_ACCOUNT_NO = string.Empty;
                        }
                    }



                    detailCounter = 0;
                    _tran = Conn.BeginTransaction();
                    object field = new object();

                    #endregion


                    #region sub


                    OracleCommand cmdGet = new OracleCommand("select DOC_PROV_NO from OFF_SALE_DOCUMENTS where DOC_PROV_NO='" + DOC_PROV_NO + "'", Conn);
                    cmdGet.Transaction = _tran;
                    string ExistTransactions = GetValue(cmdGet);
                    cmdGet.Dispose();
                    if (ExistTransactions.Equals(string.Empty))
                    {
                        WriteExceptions(" TRANSACTION " + DOC_PROV_NO + " DOES NOT EXIST IN ORACLE", "TRANSACTION", false);
                        #region Insert the Header
                        ReportProgress("Sending Transactions");
                        string updatedDate = DateTime.Now.ToString(DateFormat);
                        string insertInvoices = string.Format(@"insert into OFF_SALE_DOCUMENTS
(DEP_CODE
,DOC_PROV_NO
,DOC_PROV_DATE
,DOC_DATE
,DT_CODE
,SM_CODE
,SAL_TYPE
,DOC_TOTAL_AMOUNT
,CUS_ACCOUNT_NO
,DOC_DISCOUNT_PCT
,DOC_DISCOUNT_AMOUNT
,DOC_NARRATION
,CREATION_DATE
,CREATION_TERM
,CUR_CODE
,SD_EXCHANGE_RATE
,SALES_MAN_LOC
,PAY_AMOUNT,SALE_REF_NO,DOC_SPECIAL_NAME,IC_ACCOUNT_NO)
Values
(
'" + DEP_CODE
+ "','" + DOC_PROV_NO
+ "','" + DateTime.Parse(DOC_PROV_DATE).ToString("dd-MMM-yyyy")
+ "','" + DateTime.Parse(DOC_DATE).ToString("dd-MMM-yyyy")
+ "','" + DT_CODE
+ "','" + SM_CODE
+ "','" + SAL_TYPE
+ "','" + DOC_TOTAL_AMOUNT
+ "','" + CUS_ACCOUNT_NO
+ "','" + DOC_DISCOUNT_PCT
+ "','" + DOC_DISCOUNT_AMOUNT
+ "','" + DOC_NARRATION
+ "','" + DateTime.Parse(CREATION_DATE).ToString("dd-MMM-yyyy")
+ "','" + CREATION_TERM
+ "','" + CUR_CODE
+ "','" + SD_EXCHANGE_RATE
+ "','" + SALES_MAN_LOC
+ "','" + PAY_AMOUNT + "','" + DOC_PROV_NO + "','" + DOC_SPECIAL_NAME + "','" + IC_ACCOUNT_NO + "')");
                        WriteExceptions("INSERTING TRANSACTION HEADER " + DOC_PROV_NO + " INSERT STATEMENT IS: " + insertInvoices + "", "TRANSACTION", false);
                        OracleCommand cmdHDR = new OracleCommand(insertInvoices, Conn);
                        cmdHDR.Transaction = _tran;
                        err = ExecuteNonQuery(cmdHDR, DOC_PROV_NO, "SALES");
                        cmdHDR.Dispose();
                        #endregion


                    #endregion
                #endregion
                        if (err == InCubeErrors.Success)
                        {
                            WriteExceptions("INSERTING TRANSACTION HEADER " + DOC_PROV_NO + " WAS SUCCESSFULL", "TRANSACTION", false);
                            #region Invoice details

                            tranDetail = string.Format(@"select CO.Barcode,td.TransactionID,I.PackDefinition IM_INV_NO,BatchNo SSB_BATCH_NO,ExpiryDate SSB_EXPIRY_DATE,BasePrice IM_INV_NO,BasePrice DD_SELL_PRICE,
(case (discount) when 0 then PRICE else  PRICE-(Discount/td.quantity) end) DD_SELL_PRICE_DISCOUNTED,(case (discount) when 0 then PRICE else  PRICE-(Discount/td.quantity) end) DD_SELL_PRICE_FINAL,
(baseprice*td.quantity-discount) DD_SELL_VALUE_FINAL,0 DD_SELL_COST_TOTAL,td.quantity DD_QTY,td.quantity DD_OUTSTANDING_QTY,case baseprice when 0 then 0 else (discount/(BasePrice*td.Quantity)) end DD_LINE_DISCOUNT_PCT,
discount DD_LINE_DISCOUNT_AMOUNT,(case(salestransactiontypeid) when (1) then 'N' else
case(salestransactiontypeid) when (5) then 'N' else 'Y' end end )   DD_FOC_IND ,
Pr.PromotionID PROMOTION_ID,td.quantity DD_INV_QTY,0 DD_TAX_RATE,0 DD_TAX_VALUE,0 SDD_COMM_PCT,P.barcode SSB_IM_BARCODE,
(CASE WHEN PRICE =0 THEN CASE WHEN BASEPRICE=0 THEN '' ELSE PL.PriceListCode END ELSE  PL.PriceListCode END)  PriceListCode

from TransactionDetail TD 
inner join CustomerOutlet CO on CO.CustomerID=TD.CustomerID and CO.OutletID=TD.OutletID
inner join Pack P on TD.PackID=P.PackID 
inner join Item I on I.ItemID=P.ItemID
INNER JOIN ITEMLANGUAGE IL ON I.ITEMID=IL.ITEMID AND IL.LANGUAGEID=1
INNER JOIN PACKTYPELANGUAGE PTL ON PTL.PACKTYPEID=P.PACKTYPEID AND PTL.LANGUAGEID=1
left outer join PromotionBenefitHistory pbh on pbh.TransactionID=td.transactionid and td.packid=pbh.packid and (td.Quantity=pbh.PackQuantity or td.discount=pbh.PackDiscountValue)
left outer join promotion pr on pbh.promotionid = pr.promotionid 
LEFT OUTER JOIN PriceList PL ON TD.UsedPriceListID = PL.PriceListID
                                where TD.TransactionID='" + DOC_PROV_NO + "'");

                            invoiceQry = new InCubeQuery(tranDetail, db_vms);
                            err = invoiceQry.Execute();
                            detailtbl = new DataTable();
                            detailtbl = invoiceQry.GetDataTable();
                            WriteExceptions("#################################################################", "TRANSACTION DETAIL", false);
                            WriteExceptions("NUMBER OF DETAILS FOR TRANSACTION", "TRANSACTION DETAIL", false);
                            WriteExceptions("NUMBER OF DETAILS FOR TRANSACTION ( " + DOC_PROV_NO + ") ARE " + detailtbl.Rows.Count.ToString() + "", "TRANSACTION DETAIL", false);
                            if (detailtbl.Rows.Count == 0)
                            {
                                string updateSync = string.Format("update [Transaction] set Synchronized=0 where TransactionID='" + DOC_PROV_NO + "'");
                                qry = new InCubeQuery(updateSync, db_vms);
                                err = qry.ExecuteNonQuery();
                                err = InCubeErrors.Error;
                            }
                            WriteExceptions("INSERTING TRANSACTION DETAIL  " + DOC_PROV_NO + " STARTED", "TRANSACTION", false);
                            detailCounter = 0;
                            foreach (DataRow det in detailtbl.Rows)
                            {
                                detailCounter++;


                                string DD_LINE_NO = string.Empty;
                                string IM_INV_NO = string.Empty;
                                string LOC_CODE = string.Empty;
                                string DD_SELL_PRICE = string.Empty;
                                string DD_SELL_PRICE_DISCOUNTED = string.Empty;
                                string DD_SELL_PRICE_FINAL = string.Empty;
                                string DD_SELL_VALUE_FINAL = string.Empty;
                                string DD_SELL_COST_TOTAL = string.Empty;
                                string DD_QTY = string.Empty;
                                string DD_OUTSTANDING_QTY = string.Empty;
                                string DD_LINE_DISCOUNT_PCT = string.Empty;
                                string DD_LINE_DISCOUNT_AMOUNT = string.Empty;
                                string DD_FOC_IND = string.Empty;
                                string PROMOTION_ID = string.Empty;
                                string DD_INV_QTY = string.Empty;
                                string DD_TAX_RATE = string.Empty;
                                string DD_TAX_VALUE = string.Empty;
                                string SDD_COMM_PCT = string.Empty;
                                string DD_STM_ID = string.Empty;

                                DD_LINE_NO = detailCounter.ToString();
                                IM_INV_NO = det["IM_INV_NO"].ToString().Trim();
                                LOC_CODE = SALES_MAN_LOC;// det["LOC_CODE"].ToString().Trim();
                                DD_SELL_PRICE = det["DD_SELL_PRICE"].ToString().Trim();
                                DD_SELL_PRICE_DISCOUNTED = det["DD_SELL_PRICE_DISCOUNTED"].ToString().Trim();
                                DD_SELL_PRICE_FINAL = det["DD_SELL_PRICE_FINAL"].ToString().Trim();
                                DD_SELL_VALUE_FINAL = det["DD_SELL_VALUE_FINAL"].ToString().Trim();
                                DD_SELL_COST_TOTAL = det["DD_SELL_COST_TOTAL"].ToString().Trim();
                                DD_QTY = det["DD_QTY"].ToString().Trim();
                                DD_OUTSTANDING_QTY = det["DD_OUTSTANDING_QTY"].ToString().Trim();
                                DD_LINE_DISCOUNT_PCT = det["DD_LINE_DISCOUNT_PCT"].ToString().Trim();
                                DD_LINE_DISCOUNT_AMOUNT = det["DD_LINE_DISCOUNT_AMOUNT"].ToString().Trim();
                                DD_FOC_IND = det["DD_FOC_IND"].ToString().Trim();
                                PROMOTION_ID = det["PROMOTION_ID"].ToString().Trim();
                                DD_INV_QTY = det["DD_INV_QTY"].ToString().Trim();
                                DD_TAX_RATE = det["DD_TAX_RATE"].ToString().Trim();
                                DD_TAX_VALUE = det["DD_TAX_VALUE"].ToString().Trim();
                                SDD_COMM_PCT = det["SDD_COMM_PCT"].ToString().Trim();
                                DD_STM_ID = det["PriceListCode"].ToString().Trim();
                                 
                                if (DD_FOC_IND.Equals("Y"))
                                {
                                    DD_SELL_PRICE = "0";
                                    DD_SELL_PRICE_DISCOUNTED = "0";
                                    DD_SELL_PRICE_FINAL = "0";
                                    DD_SELL_VALUE_FINAL = "0";
                                }

                                insertDetail = string.Format(@"INSERT INTO OFF_SALE_DOCUMENT_DETAILS
(DEP_CODE
,DOC_PROV_NO
,DD_LINE_NO
,IM_INV_NO
,LOC_CODE
,DD_SELL_PRICE
,DD_SELL_PRICE_DISCOUNTED
,DD_SELL_PRICE_FINAL
,DD_SELL_VALUE_FINAL
,DD_SELL_COST_TOTAL
,DD_QTY
,DD_OUTSTANDING_QTY
,DD_LINE_DISCOUNT_PCT
,DD_LINE_DISCOUNT_AMOUNT
,DD_FOC_IND
,PROMOTION_ID
,DD_INV_QTY
,DD_TAX_RATE
,DD_TAX_VALUE
,SDD_COMM_PCT
,SALE_REF_NO
,DD_STM_ID
,doc_no_have_previous 
,dd_line_no_have_previous )
VALUES
(
'" + DEP_CODE
+ "','" + DOC_PROV_NO
+ "','" + DD_LINE_NO
+ "','" + IM_INV_NO
+ "','" + LOC_CODE
+ "','" + DD_SELL_PRICE
+ "','" + DD_SELL_PRICE_DISCOUNTED
+ "','" + DD_SELL_PRICE_FINAL
+ "','" + DD_SELL_VALUE_FINAL
+ "','" + DD_SELL_COST_TOTAL
+ "','" + DD_QTY
+ "','" + DD_OUTSTANDING_QTY
+ "','" + DD_LINE_DISCOUNT_PCT
+ "','" + DD_LINE_DISCOUNT_AMOUNT
+ "','" + DD_FOC_IND
+ "','" + PROMOTION_ID
+ "','" + DD_INV_QTY
+ "','" + DD_TAX_RATE
+ "','" + DD_TAX_VALUE
+ "','" + SDD_COMM_PCT
+ "','" + DOC_PROV_NO
+ "','" + DD_STM_ID
+ "','" + doc_no_have_previous
+ "','" + dd_line_no_have_previous
+ "')");


                                OracleCommand cmdDR = new OracleCommand(insertDetail, Conn);
                                cmdDR.Transaction = _tran;
                                err = ExecuteNonQuery(cmdDR, DOC_PROV_NO, "SALES");
                                if (err == InCubeErrors.Success)
                                {
                                    WriteExceptions("INSERTING TRANSACTION DETAIL" + DOC_PROV_NO + " WAS SUCCESSFULL", "TRANSACTION", false);
                                    WriteExceptions("INSERTING TRANSACTION BATCHES" + DOC_PROV_NO + " STARTED", "TRANSACTION", false);
                                    string SSB_DEP_CODE = DEP_CODE;
                                    string SSB_PROV_NO = DOC_PROV_NO;
                                    string SSB_IM_INV_NO = IM_INV_NO;
                                    string SSB_IM_BARCODE = det["SSB_IM_BARCODE"].ToString().Trim();
                                    string SSB_QTY = DD_QTY;
                                    string SSB_LINE_NO = detailCounter.ToString();
                                    string SSB_EXPIRY_DATE = det["SSB_EXPIRY_DATE"].ToString().Trim();
                                    string SSB_BATCH_NO = det["SSB_BATCH_NO"].ToString().Trim();
                                    string insertBatch = string.Format(@"INSERT INTO IC_SALES_BATCHES
(
SSB_DEP_CODE
,SSB_PROV_NO
,SSB_IM_INV_NO
,SSB_IM_BARCODE
,SSB_QTY
,SSB_LINE_NO
,SSB_EXPIRY_DATE
,SSB_BATCH_NO
)
VALUES
(
'" + SSB_DEP_CODE
+ "','" + SSB_PROV_NO
+ "','" + SSB_IM_INV_NO
+ "','" + SSB_IM_BARCODE
+ "','" + SSB_QTY
+ "','" + SSB_LINE_NO + "','" + DateTime.Parse(SSB_EXPIRY_DATE).ToString("dd-MMM-yyyy") + "','" + SSB_BATCH_NO + "')");
                                    cmdDR = new OracleCommand(insertBatch, Conn);
                                    cmdDR.Transaction = _tran;
                                    err = ExecuteNonQuery(cmdDR, DOC_PROV_NO, "SALES");
                                    if (err == InCubeErrors.Success)
                                    {
                                        WriteExceptions("INSERTING TRANSACTION BATCHES WAS SUCCESSFULL" + DOC_PROV_NO + "", "TRANSACTION", false);
                                    }
                                    else
                                    {
                                        WriteExceptions("INSERTING TRANSACTION BATCHES FAILED" + DOC_PROV_NO + " THE INSERT STATEMENT WAS " + insertBatch + "", "TRANSACTION", false);
                                        break;
                                    }
                                }
                                else
                                {
                                    WriteExceptions("INSERTING TRANSACTION DETAIL FAILED" + DOC_PROV_NO + " THE INSERT STATEMENT WAS " + insertDetail + "", "TRANSACTION", false);
                                    break;
                                }

                                cmdDR.Dispose();

                            }
                            #endregion
                        }

                        if (err == InCubeErrors.Success)
                        {
                            WriteExceptions("INSERTING ALL TRANSACTION WAS SUCCESSFULL.. UPDATING SYNCHRONIZED" + DOC_PROV_NO + "", "TRANSACTION", false);
                            string update = string.Format("update [Transaction] set Synchronized=1 where TransactionID='{0}'", DOC_PROV_NO);
                            invoiceQry = new InCubeQuery(update, db_vms);
                            err = invoiceQry.ExecuteNonQuery();
                            WriteMessage("\r\n" + DOC_PROV_NO + " - Transaction sent Successfully");
                            TOTALINSERTED++;
                            _tran.Commit();
                        }
                        else
                        {
                            WriteMessage("\r\n" + DOC_PROV_NO + " - Transaction Failed");
                            _tran.Rollback();
                        }
                    }
                    else
                    {
                        WriteExceptions("TRANSACTION ALREADY EXIST IN ORACLE" + DOC_PROV_NO + "", "TRANSACTION", false);
                        WriteMessage("\r\n" + DOC_PROV_NO + " - Transaction Failed");
                        WriteExceptions(DOC_PROV_NO + " - Transaction exists in ORACLE", "error", true);
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
        #endregion

        #endregion

        #region GENERIC
        private InCubeErrors UpdateFlag(string TableName, string Criteria)
        {
            try
            {
                string query = string.Format("update {0} set UP_FLAG='Y' where {1}", TableName, Criteria);
                OracleCommand cmdHDR = new OracleCommand(query, Conn);
                err = ExecuteNonQuery(cmdHDR);
                cmdHDR.Dispose();
                return err;
            }
            catch
            {

            }
            return err;
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
            catch
            {
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
            catch
            {

            }
        }
        public override void Close()
        {
            db_vms.Close();
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
        //                SetProgressMax( = tbl.Rows.Count;
        //                int TOTALUPDATED = 0;
        //                int TOTALINSERTED = 0;
        //                foreach (DataRow dr in tbl.Rows)
        //                {
        //                    ReportProgress();
        //                    IntegrationForm.lblProgress.Text = "Updating Outstanding" + " " + IntegrationForm.progressBar1.Value + " / " + SetProgressMax(;
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
            }
            catch (Exception ex)
            {
                WriteExceptions(ex.Message, "error", true);

                return InCubeErrors.Error;
            }
            return InCubeErrors.Success;
        }
        private InCubeErrors InsertLog(string transactionID, string errorDescription, string errorLocation)
        {
            InCubeErrors logError = InCubeErrors.Error;
            try
            {
                string insert = string.Format(@"insert into InCubeLog values('{0}','{1}','{2}','{3}')", transactionID, DateTime.Now.ToString(), errorDescription, errorLocation);
                InCubeQuery logQry = new InCubeQuery(insert, db_vms);
                logError = logQry.ExecuteNonQuery();

            }
            catch
            {

            }
            return logError;
        }
        public void ConstantRun()
        {
            try
            {
                InCubeErrors err;
                WriteExceptions("ENTERING THE CONSTANTRUN BLOCK", "CONSTANTRUN", true);
                if (Conn.State == ConnectionState.Closed)
                { Conn.Open(); }
                WriteExceptions("OLEDB CONNECTION STATUS IS " + Conn.State.ToString() + "", "CONSTANTRUN", true);
                DataTable DT = new DataTable();
                string SELECT = "SELECT * FROM SCM_VANINTEGRATION_LOGS WHERE  SYNC_FLAG=0";
                InCubeQuery qry = new InCubeQuery(SELECT, db_vms);
                err = qry.Execute();
                WriteExceptions("SELECT ERROR STATUS=" + err.ToString() + "", "CONSTANTRUN", true);
                DT = qry.GetDataTable();
                WriteExceptions("NUMBER OF RECORDS SELECTED=" + DT.Rows.Count + "", "CONSTANTRUN", true);
                ClearProgress();
                SetProgressMax(DT.Rows.Count);
                foreach (DataRow dr in DT.Rows)
                {
                    ReportProgress("INSERTING STATUS IN ORACLE");

                    string INSERT = string.Format(@"INSERT INTO SCM_VANINTEGRATION_LOGS
(
SVL_LOC_CODE,
SVL_DEP_CODE,
SVL_DATE,
SVL_FLAG
) 
VALUES
(
'{0}',
'{1}',
'{2}',
'{3}'
)", dr["SVL_LOC_CODE"].ToString(), dr["SVL_DEP_CODE"].ToString(), dr["SVL_DATE"].ToString(), dr["SVL_FLAG"].ToString());
                    WriteExceptions("INSERT STATEMENT IS <<" + INSERT + ">>", "CONSTANTRUN", true);
                    //qry = new InCubeQuery(INSERT, db_vms);
                    //err = qry.ExecuteNonQuery();
                    OracleCommand cmdHDR = new OracleCommand(INSERT, Conn);
                    err = ExecuteNonQuery(cmdHDR);
                    cmdHDR.Dispose();
                    if (err == InCubeErrors.Success)
                    {
                        WriteExceptions("INSERTING INTO ORACLE SUCCESS", "CONSTANTRUN", true);
                        qry = new InCubeQuery("UPDATE SCM_VANINTEGRATION_LOGS SET SYNC_FLAG=1 WHERE SVL_LOC_CODE='" + dr["SVL_LOC_CODE"].ToString() + "'", db_vms);
                        err = qry.ExecuteNonQuery();
                        if (err == InCubeErrors.Success)
                        {
                            WriteExceptions("UPDATING SCM_VANINTEGRATION_LOGS SUCCESS", "CONSTANTRUN", true);
                        }
                    }
                    qry.Close();
                }
            }
            catch (Exception ex)
            {
                WriteExceptions("EXCEPTION: " + ex.Message + "", "CONSTANTRUN", true);
            }
            finally
            {

            }
            WriteExceptions("ATTEMPT TO SLEEP", "CONSTANTRUN", true);
            //System.Threading.Thread.Sleep(6000);
            WriteExceptions("SLEEP COMPLETED", "CONSTANTRUN", true);
            //ConstantRun();
        }
        #endregion


    }
}
