using InCubeIntegration_DAL;
using InCubeLibrary;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml;

namespace InCubeIntegration_BL
{
    public class IntegrationAreen : IntegrationBase
    {
        enum DiscountTypes
        {
            HeaderDiscount,
            DetailDiscount,
            NoDiscount
        }

        #region DECLARATION

        string ItemsOrg = "1";
        InCubeQuery incubeQuery;
        InCubeTransaction dbTrans = null;
        QueryBuilder QueryBuilderObject = new QueryBuilder();
        InCubeErrors err;
        private long UserID;
        string DateFormat = "dd-MMM-yy";
        InCubeQuery qry;
        string ConnectionString = string.Empty;
        OracleConnection Conn;
        OracleCommand cmdHDR;
        OracleDataAdapter adp;
        #endregion

        #region CONSTRUCTOR
        public IntegrationAreen(long CurrentUserID, ExecutionManager ExecManager)
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
        void UpdateItem_Old()
        {
            try
            {

                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;

                object field = new object();

                DataTable DT = new DataTable();
                string select = "SELECT  * from XXARE_ITEMS_OUT_STG where PROCESS_FLAG='N'";

                DT = OLEDB_Datatable(select);
                //QueryBuilderObject.SetField("InActive", "1");
                //QueryBuilderObject.UpdateQueryString("Item", db_vms);

                ClearProgress();
                SetProgressMax(DT.Rows.Count);

                foreach (DataRow row in DT.Rows)
                {
                    ReportProgress();

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
                    //Pack barcode    11
                    string SRNO = row["SRNO"].ToString().Trim();
                    string status = row["ITEM_STATUS_CODE"].ToString().Trim();
                    if (status == "Active") { status = "0"; } else { status = "1"; }
                    string ItemCode = row["SUPPLIER_CODE"].ToString().Trim();
                    string itemDescriptionEnglish = row["ITEM_NAME"].ToString().Trim();
                    string itemDescriptionArabic = row["ITEM_NAME"].ToString().Trim();
                    string DivisionCode = row["DIVISION_CODE"].ToString().Trim();
                    string DivisionNameEnglish = row["DIVISION_NAME"].ToString().Trim();
                    //if (DivisionCode.Equals("2")) { itemDescriptionEnglish = "METS " + itemDescriptionEnglish; itemDescriptionArabic = "METS " + itemDescriptionArabic; }
                    string CategoryCode = row["ITEM_CATEGORY_CODE"].ToString().Trim() + "-" + DivisionCode;
                    string CategoryNameEnglish = row["ITEM_CATEGORY_DESC"].ToString() + "/" + DivisionNameEnglish;//row["ItCategory"].ToString().Trim();
                    string Brand = row["ITEM_BRAND_NAME"].ToString().Trim();
                    string Orgin = row["ITEM_CODE"].ToString().Trim();// row["ITEM_ANLY_CODE_10"].ToString().Trim();// row["Origin"].ToString().Trim();
                    string TCAllowed = "N"; //row["TCAllowed"].ToString().Trim();
                    if (TCAllowed == "Y") { TCAllowed = "1"; } else { TCAllowed = "0"; }
                    string PackDescriptionEnglish = row["ITEM_UOM"].ToString().Trim();
                    string packQty = row["CONVERSION_FACTOR"].ToString().Trim();
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

                    string barcode = row["BARCODE"].ToString().Trim();
                    string PackDefinition = row["ITEM_UOM"].ToString().Trim();
                    string PackGroup = row["ITEM_GROUP_NAME"].ToString().Trim();
                    string PackGroupCode = row["ITEM_GROUP_CODE"].ToString().Trim();
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

                    string ItemCategoryID = GetFieldValue("ItemCategory", "ItemCategoryID", "ItemCategoryCode = '" + CategoryCode + "' AND DivisionID =" + DivisionID.ToString(), db_vms);

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

                    //#region ItemBrand

                    //string BrandID = GetFieldValue("BrandLanguage", "BrandID", "Description = '" + Brand + "' and LanguageID=1", db_vms);

                    //if (BrandID == string.Empty)
                    //{
                    //    BrandID = GetFieldValue("Brand", "isnull(MAX(BrandID),0) + 1", db_vms);

                    //    QueryBuilderObject.SetField("BrandID", BrandID);

                    //    err = QueryBuilderObject.InsertQueryString("Brand", db_vms);

                    //    QueryBuilderObject.SetField("BrandID", BrandID);
                    //    QueryBuilderObject.SetField("LanguageID", "1");
                    //    QueryBuilderObject.SetField("Description", "'" + Brand + "'");
                    //    err = QueryBuilderObject.InsertQueryString("BrandLanguage", db_vms);

                    //    QueryBuilderObject.SetField("BrandID", BrandID);  // Arabic Description
                    //    QueryBuilderObject.SetField("LanguageID", "2");
                    //    QueryBuilderObject.SetField("Description", "'" + Brand + "'");
                    //    err = QueryBuilderObject.InsertQueryString("BrandLanguage", db_vms);
                    //}

                    //#endregion

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

                    ItemID = GetFieldValue("Item", "ItemID", "Origin='" + Orgin + "'", db_vms);
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
                        QueryBuilderObject.SetField("EquivalencyFactor", "0");
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
                    if (err == InCubeErrors.Success)
                    {
                        UpdateFlag("XXARE_ITEMS_OUT_STG", "SRNO=" + SRNO + "");
                    }
                }

                DT.Dispose();

                WriteMessage("\r\n");
                WriteMessage("<<< ITEMS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public override void UpdateItem()
        {
            try
            {
                #region (Declarations)

                int TOTALUPDATED = 0, TOTALINSERTED = 0;
                string ItemCode, ItemName, CategoryCode, CategoryName, DivisionCode, DivisionName, Origin, UOM, Factor, Barcode, GroupName, GroupCode, Brand;
                decimal PackQty = 0;

                #endregion

                DataTable dtItems = new DataTable();
                string QueryString = @"SELECT SUPPLIER_CODE,ITEM_NAME,ITEM_CATEGORY_CODE,ITEM_CATEGORY_DESC,ITEM_BRAND_NAME,ITEM_CODE
,ITEM_UOM,CONVERSION_FACTOR,BARCODE,ITEM_GROUP_NAME,ITEM_GROUP_CODE FROM
(SELECT Max(SRNO) SRNO
FROM XXARE_ITEMS_OUT_STG
GROUP BY SUPPLIER_CODE,ITEM_UOM) TBL
INNER JOIN XXARE_ITEMS_OUT_STG ITM ON ITM.SRNO = TBL.SRNO
WHERE ITM.ITEM_STATUS_CODE = 'Active'";

                dtItems = GetDataTable(QueryString);
                if (err != InCubeErrors.Success)
                    return;

                QueryString = "SELECT ItemID FROM Item";
                incubeQuery = new InCubeQuery(db_vms, QueryString);
                err = incubeQuery.Execute();
                if (err != InCubeErrors.Success)
                    return;

                DataTable dt = new DataTable();
                dt = incubeQuery.GetDataTable();
                List<string> ItemIDs = new List<string>();
                foreach (DataRow dr in dt.Rows)
                    ItemIDs.Add(dr["ItemID"].ToString());



                ClearProgress();
                SetProgressMax(dtItems.Rows.Count);

                for (int i = 0; i < dtItems.Rows.Count; i++)
                {
                    //if (int.Parse(GetFieldValue("Item", "COUNT(*)", "InActive = 0", db_vms)) != i)
                    //    MessageBox.Show(i.ToString());

                    ReportProgress("Items");

                    ItemCode = dtItems.Rows[i]["SUPPLIER_CODE"].ToString().Trim();
                    ItemName = dtItems.Rows[i]["ITEM_NAME"].ToString().Trim();
                    CategoryCode = dtItems.Rows[i]["ITEM_CATEGORY_CODE"].ToString().Trim();
                    CategoryName = dtItems.Rows[i]["ITEM_CATEGORY_DESC"].ToString().Trim();
                    DivisionCode = dtItems.Rows[i]["ITEM_BRAND_NAME"].ToString().Trim();
                    DivisionName = DivisionCode;
                    Brand = DivisionCode;
                    Origin = dtItems.Rows[i]["ITEM_CODE"].ToString().Trim();
                    UOM = dtItems.Rows[i]["ITEM_UOM"].ToString().Trim();
                    Factor = dtItems.Rows[i]["CONVERSION_FACTOR"].ToString().Trim();
                    Barcode = dtItems.Rows[i]["BARCODE"].ToString().Trim();
                    GroupName = dtItems.Rows[i]["ITEM_GROUP_NAME"].ToString().Trim();
                    GroupCode = dtItems.Rows[i]["ITEM_GROUP_CODE"].ToString().Trim();

                    if (ItemCode == string.Empty)
                        continue;
                    if (!decimal.TryParse(Factor, out PackQty))
                        continue;

                    #region ItemDivision

                    string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode = '" + DivisionCode + "'", db_vms);

                    if (DivisionID == string.Empty)
                    {
                        DivisionID = GetFieldValue("Division", "isnull(MAX(DivisionID),0) + 1", db_vms);

                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("DivisionID", DivisionID);
                        QueryBuilderObject.SetStringField("DivisionCode", DivisionCode);
                        QueryBuilderObject.SetField("OrganizationID", ItemsOrg);
                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("CreatedDate", "GETDATE()");
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "GETDATE()");
                        err = QueryBuilderObject.InsertQueryString("Division", db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }

                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("DivisionID", DivisionID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetStringField("Description", DivisionName);
                        err = QueryBuilderObject.InsertQueryString("DivisionLanguage", db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }

                        //QueryBuilderObject = new QueryBuilder();
                        //QueryBuilderObject.SetField("DivisionID", DivisionID);
                        //QueryBuilderObject.SetField("LanguageID", "2");
                        //QueryBuilderObject.SetStringField("Description", DivisionName);
                        //err = QueryBuilderObject.InsertQueryString("DivisionLanguage", db_vms);
                        //if (err != InCubeErrors.Success)
                        //{
                        //    WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                        //    continue;
                        //}
                    }

                    #endregion

                    #region ItemCategory

                    string ItemCategoryID = GetFieldValue("ItemCategory", "ItemCategoryID", "ItemCategoryCode = '" + CategoryCode + "'", db_vms);

                    if (ItemCategoryID == string.Empty)
                    {
                        ItemCategoryID = GetFieldValue("ItemCategory", "isnull(MAX(ItemCategoryID),0) + 1", db_vms);

                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID);
                        QueryBuilderObject.SetStringField("ItemCategoryCode", CategoryCode);
                        QueryBuilderObject.SetField("DivisionID", DivisionID);
                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("CreatedDate", "GETDATE()");
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "GETDATE()");
                        err = QueryBuilderObject.InsertQueryString("ItemCategory", db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }

                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetStringField("Description", CategoryName);
                        err = QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }

                        //QueryBuilderObject = new QueryBuilder();
                        //QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID);
                        //QueryBuilderObject.SetField("LanguageID", "2");
                        //QueryBuilderObject.SetStringField("Description", CategoryName);
                        //err = QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", db_vms);
                        //if (err != InCubeErrors.Success)
                        //{
                        //    WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                        //    continue;
                        //}
                    }
                    else
                    {
                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("DivisionID", DivisionID);
                        err = QueryBuilderObject.UpdateQueryString("ItemCategory", "ItemCategoryID = " + ItemCategoryID, db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }
                    }

                    #endregion

                    #region PackType

                    string PacktypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " Description = '" + UOM + "' AND LanguageID = 1", db_vms);

                    if (PacktypeID == string.Empty)
                    {
                        PacktypeID = GetFieldValue("PackType", "isnull(MAX(PackTypeID),0) + 1", db_vms);

                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("PackTypeID", PacktypeID);
                        err = QueryBuilderObject.InsertQueryString("PackType", db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }

                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("PackTypeID", PacktypeID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetStringField("Description", UOM);
                        err = QueryBuilderObject.InsertQueryString("PackTypeLanguage", db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }

                        //QueryBuilderObject = new QueryBuilder();
                        //QueryBuilderObject.SetField("PackTypeID", PacktypeID);
                        //QueryBuilderObject.SetField("LanguageID", "2");
                        //QueryBuilderObject.SetStringField("Description", UOM);
                        //err = QueryBuilderObject.InsertQueryString("PackTypeLanguage", db_vms);
                        //if (err != InCubeErrors.Success)
                        //{
                        //    WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                        //    continue;
                        //}
                    }

                    #endregion

                    #region Brand

                    string BrandID = GetFieldValue("BrandLanguage", "BrandID", " Description = '" + Brand + "' AND LanguageID = 1", db_vms);
                    if (BrandID == string.Empty && Brand.Trim() != string.Empty)
                    {
                        BrandID = GetFieldValue("Brand", "isnull(MAX(BrandID),0) + 1", db_vms);

                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("BrandID", BrandID);
                        err = QueryBuilderObject.InsertQueryString("Brand", db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }

                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("BrandID", BrandID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + Brand + "'");
                        err = QueryBuilderObject.InsertQueryString("BrandLanguage", db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }

                        //QueryBuilderObject = new QueryBuilder();
                        //QueryBuilderObject.SetField("BrandID", BrandID);
                        //QueryBuilderObject.SetField("LanguageID", "2");
                        //QueryBuilderObject.SetField("Description", "N'" + Brand + "'");
                        //err = QueryBuilderObject.InsertQueryString("BrandLanguage", db_vms);
                        //if (err != InCubeErrors.Success)
                        //{
                        //    WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                        //    continue;
                        //}
                    }

                    #endregion

                    #region Item

                    string ItemID = GetFieldValue("Item", "ItemID", "ItemCode = '" + ItemCode + "'", db_vms);
                    if (ItemID == string.Empty)
                    {
                        ItemID = GetFieldValue("Item", "isnull(MAX(ItemID),0) + 1", db_vms);

                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("ItemID", ItemID);
                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID);
                        QueryBuilderObject.SetStringField("ItemCode", ItemCode);
                        QueryBuilderObject.SetField("InActive", "0");
                        QueryBuilderObject.SetStringField("PackDefinition", "");
                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("CreatedDate", "GETDATE()");
                        QueryBuilderObject.SetField("ItemType", "1");
                        QueryBuilderObject.SetStringField("Origin", Origin);
                        QueryBuilderObject.SetField("BrandID", BrandID);
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "GETDATE()");

                        err = QueryBuilderObject.InsertQueryString("Item", db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }

                        TOTALINSERTED++;
                    }
                    else
                    {
                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID);
                        QueryBuilderObject.SetField("InActive", "0");
                        QueryBuilderObject.SetStringField("Origin", Origin);
                        QueryBuilderObject.SetField("BrandID", BrandID);
                        QueryBuilderObject.SetField("UpdatedDate", "GETDATE()");

                        err = QueryBuilderObject.UpdateQueryString("Item", "ItemID = " + ItemID, db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }

                        TOTALUPDATED++;
                        if (ItemIDs.Contains(ItemID))
                            ItemIDs.Remove(ItemID);
                    }

                    #endregion

                    #region ItemLanguage

                    string ExistItem = GetFieldValue("ItemLanguage", "ItemID", "ItemID = " + ItemID + " AND LanguageID = 1", db_vms);
                    if (ExistItem != string.Empty)
                    {
                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetStringField("Description", ItemName);
                        err = QueryBuilderObject.UpdateQueryString("ItemLanguage", " ItemID =" + ItemID + " AND LanguageID = 1", db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }
                    }
                    else
                    {
                        QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetStringField("Description", ItemName);
                        err = QueryBuilderObject.InsertQueryString("ItemLanguage", db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }
                    }

                    //ExistItem = GetFieldValue("ItemLanguage", "ItemID", "ItemID = " + ItemID + " AND LanguageID = 2", db_vms);
                    //if (ExistItem != string.Empty)
                    //{
                    //    QueryBuilderObject = new QueryBuilder();
                    //    QueryBuilderObject.SetStringField("Description", ItemName);
                    //    err = QueryBuilderObject.UpdateQueryString("ItemLanguage", " ItemID =" + ItemID + " AND LanguageID = 2", db_vms);
                    //    if (err != InCubeErrors.Success)
                    //    {
                    //        WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                    //        continue;
                    //    }
                    //}
                    //else
                    //{
                    //    QueryBuilderObject = new QueryBuilder();
                    //    QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                    //    QueryBuilderObject.SetField("LanguageID", "2");
                    //    QueryBuilderObject.SetStringField("Description", ItemName);
                    //    err = QueryBuilderObject.InsertQueryString("ItemLanguage", db_vms);
                    //    if (err != InCubeErrors.Success)
                    //    {
                    //        WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                    //        continue;
                    //    }
                    //}

                    #endregion

                    #region Pack

                    string PackID = GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " AND PackTypeID = " + PacktypeID, db_vms);
                    if (PackID != string.Empty)
                    {
                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetStringField("Barcode", Barcode);
                        QueryBuilderObject.SetField("Quantity", Factor);
                        err = QueryBuilderObject.UpdateQueryString("Pack", "PackID = " + PackID, db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }
                    }
                    else
                    {
                        PackID = GetFieldValue("Pack", "ISNULL(MAX(PackID),0) + 1", db_vms);

                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("PackID", PackID);
                        QueryBuilderObject.SetStringField("Barcode", Barcode);
                        QueryBuilderObject.SetField("ItemID", ItemID);
                        QueryBuilderObject.SetField("PackTypeID", PacktypeID);
                        QueryBuilderObject.SetField("Quantity", Factor);
                        QueryBuilderObject.SetField("EquivalencyFactor", "0");
                        err = QueryBuilderObject.InsertQueryString("Pack", db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }
                    }

                    #endregion

                    #region PackGroup

                    string PackGroupID = GetFieldValue("PackGroup", "PackGroupID", "PackGroupCode = '" + GroupCode + "'", db_vms);

                    if (PackGroupID == string.Empty)
                    {
                        PackGroupID = GetFieldValue("PackGroup", "isnull(MAX(PackGroupID),0) + 1", db_vms);

                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("PackGroupID", PackGroupID);
                        QueryBuilderObject.SetStringField("PackGroupCode", GroupCode);
                        err = QueryBuilderObject.InsertQueryString("PackGroup", db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }

                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("PackGroupID", PackGroupID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetStringField("Description", GroupName);
                        err = QueryBuilderObject.InsertQueryString("PackGroupLanguage", db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }

                        //QueryBuilderObject = new QueryBuilder();
                        //QueryBuilderObject.SetField("PackGroupID", PackGroupID);
                        //QueryBuilderObject.SetField("LanguageID", "2");
                        //QueryBuilderObject.SetStringField("Description", GroupName);
                        //err = QueryBuilderObject.InsertQueryString("PackGroupLanguage", db_vms);
                        //if (err != InCubeErrors.Success)
                        //{
                        //    WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                        //    continue;
                        //}
                    }
                    else
                    {
                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetStringField("Description", GroupName);
                        err = QueryBuilderObject.UpdateQueryString("PackGroupLanguage", "PackGroupID = " + PackGroupID, db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }
                    }

                    string PackGroupDetailID = GetFieldValue("PackGroupDetail", "PackGroupID", "PackID = " + PackID, db_vms);
                    if (PackGroupDetailID == string.Empty)
                    {
                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("PackID", PackID);
                        QueryBuilderObject.SetField("PackGroupID", PackGroupID);
                        err = QueryBuilderObject.InsertQueryString("PackGroupDetail", db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }
                    }
                    else if (PackGroupDetailID != PackGroupID)
                    {
                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("PackGroupID", PackGroupID);
                        err = QueryBuilderObject.UpdateQueryString("PackGroupDetail", "PackID = " + PackID, db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nError in adding item [" + ItemCode + "]");
                            continue;
                        }
                    }

                    #endregion
                }

                if (ItemIDs.Count > 0)
                {
                    QueryString = "UPDATE Item SET InActive = 1 WHERE ItemID IN (";
                    foreach (string itemID in ItemIDs)
                    {
                        QueryString += itemID + ",";
                    }
                    QueryString = QueryString.Substring(0, QueryString.Length - 1) + ")";
                    incubeQuery = new InCubeQuery(db_vms, QueryString);
                    err = incubeQuery.ExecuteNonQuery();
                }

                WriteMessage("\r\n");
                WriteMessage("<<< ITEMS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED + " , Total Skipped = " + (dtItems.Rows.Count - TOTALINSERTED - TOTALUPDATED));

                dtItems.Dispose();
                dt.Dispose();
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
            try
            {
                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;

                object field = new object();

                DataTable DT = new DataTable();
                string select = "select * from XXARE_CUSTOMER_OUT_STG where PROCESS_FLAG='N' ";

                DT = DT = OLEDB_Datatable(select);

                ClearProgress();
                SetProgressMax(DT.Rows.Count);

                foreach (DataRow row in DT.Rows)
                {
                    try
                    {
                        ReportProgress("Updating Customers");

                        #region Variables
                        string PriceListCode = "NoCode";// row["CUST_PL_CODE"].ToString().Trim();
                        string CustomerCode = row["CUSTOMER_GROUP_CODE"].ToString().Trim();
                        string CustomerName = row["CUSTOMER_GROUP_NAME"].ToString().Trim();
                        string outletBarCode = row["CUSTOMER_OUTLET_CODE"].ToString().Trim();
                        string outletCode = row["CUSTOMER_OUTLET_CODE"].ToString().Trim();
                        string CustomerOutletDescriptionEnglish = row["CUSTOMER_OUTLET_NAME"].ToString().Trim();
                        CustomerOutletDescriptionEnglish = CustomerName + "-" + CustomerOutletDescriptionEnglish;
                        string CustomerOutletDescriptionArabic = row["CUSTOMER_OUTLET_NAME"].ToString().Trim();
                        string update_flagID = row["SRNO"].ToString().Trim();
                        string customerClassID = row["Division_Code"].ToString().Trim();
                        //CustomerCode = outletCode;
                        //CustomerName = CustomerOutletDescriptionEnglish;
                        string Phonenumber = row["TELEPHONE"].ToString().Trim();
                        string Faxnumber = row["MOBILE"].ToString().Trim();
                        if (Phonenumber.Length >= 40) Phonenumber.Substring(0, 39);
                        if (Faxnumber.Length >= 40) Faxnumber.Substring(0, 39);
                        string Email = "";// row["Email"].ToString().Trim();
                        string CustomerAddressEnglish = row["ADDRESS"].ToString().Trim();// +row["ADDR_LINE_1"].ToString().Trim();
                        string CustomerAddressArabic = row["ADDRESS"].ToString().Trim();// +row["ADDR_LINE_1"].ToString().Trim();
                        string Taxable = "0"; //row[9].ToString().Trim();
                        //string channelDescription = row["Channel"].ToString().Trim();
                        //string ChannelCode = row["ChannelCode"].ToString().Trim();
                        string CustomerNewGroup = row["CATEGORY_NAME"].ToString().Trim();// string.Empty;
                        //if (CustomerName.Trim().Replace(" ", "").ToLower() != "nogroup")
                        //{
                        //    CustomerNewGroup = row["CustomerGroup"].ToString().Trim();
                        //}

                        string CustomerGroupDescription = row["CATEGORY_NAME"].ToString().Trim();
                        string IsCreditCustomer = row["CUSTOMER_TYPE"].ToString().Trim();
                        string CreditLimit = row["CREDIT_LIMIT"].ToString().Trim();
                        if (CreditLimit.Equals(string.Empty)) CreditLimit = "1000";
                        string Balance = "0";// row[13].ToString().Trim();
                        string Paymentterms = row["PAYMENT_TERM_DAYS"].ToString().Trim();
                        int days = 0;
                        if (!int.TryParse(Paymentterms, out days))
                            Paymentterms = "120";
                        string OnHold = "0";// row["OnHold"].ToString().Trim();
                        string CustomerType = string.Empty;// row["CustomerPeymentTerms"].ToString().Trim();

                        if (IsCreditCustomer.Equals("CREDIT"))
                        {
                            CustomerType = "2";
                        }
                        else
                        {
                            CustomerType = "1";
                        }

                        string inActive = "active";// row["Status"].ToString().Trim();
                        if (inActive.ToLower().Equals("active"))
                        {
                            inActive = "0";
                        }
                        else
                        {
                            inActive = "1";
                        }
                        string DivisionCode = row["DIVISION_CODE"].ToString().Trim();
                        string STATE = row["STATE"].ToString().Trim();
                        string CITY = row["CITY"].ToString().Trim();
                        ////List<DaySequence> daySequence = new List<DaySequence>();
                        ////DaySequence ds = new DaySequence();



                        string B2B_Invoices = "0";// row["NumberOfInvoices"].ToString().Trim();
                        string GPSlongitude = "0";// row[16].ToString().Trim();
                        string GPSlatitude = "0";// row[17].ToString().Trim();


                        //string SupervisorCode = row["SupervisorCode"].ToString().Trim();
                        //string Supervisor = row["Supervisor"].ToString().Trim();
                        //string RoutemanagerCode = row["RouteManagerCode"].ToString().Trim();
                        //string Routemanager = row["RouteManager"].ToString().Trim();
                        string Classification = row["STATUS"].ToString().Trim();
                        string RouteCode = row["SALESMAN_CODE"].ToString().Trim();
                        string SalesManCode = row["SALESMAN_CODE"].ToString().Trim();
                        string SalesMan = row["SALESMAN_NAME"].ToString().Trim();

                        string superCode = row["SUPERVISOR_CODE"].ToString().Trim();
                        string superNAME = row["SUPERVISOR_NAME"].ToString().Trim();
                        string SALESMANAGER_CODE = row["SALESMANAGER_CODE"].ToString().Trim();
                        string SALESMANAGER_NAME = row["SALESMANAGER_NAME"].ToString().Trim();
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
                        CreateCustomerOutlet(STATE, CITY, update_flagID, AccountID.ToString(), SalesManCode, GroupID, GroupID2, B2B_Invoices, CustomerType, outletCode, PriceListCode, Paymentterms, CustomerOutletDescriptionEnglish, CustomerAddressEnglish, CustomerOutletDescriptionArabic, CustomerAddressArabic, Phonenumber, Faxnumber, OnHold, Taxable, CustomerCode, CreditLimit, Balance, GPSlongitude, GPSlatitude, outletBarCode, Email, DivisionCode, inActive, SalesMan, superCode, superNAME, SALESMANAGER_CODE, SALESMANAGER_NAME, customerClassID);
                    }
                    catch
                    {
                        WriteMessage("\r\n");
                        WriteMessage("customer failed ");

                    }
                }

                DT.Dispose();

                incubeQuery = new InCubeQuery(db_vms, "sp_UpdateCustomersForSupervisors");
                err = incubeQuery.ExecuteStoredProcedure();

                incubeQuery = new InCubeQuery(db_vms, "sp_AddTerritoryCustomersToGroups");
                incubeQuery.ExecuteStoredProcedure();

                WriteMessage("\r\n");
                WriteMessage("<<< CUSTOMERS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        //private void CreateCustomerOutlet(string state,string city, string SRNO,string parentAccount, string SALESMAN_CODE, string GroupID, string GroupID2, string B2B_Inv, string CustType, string CustomerCode, string PriceListCode, string Paymentterms, string CustomerDescriptionEnglish, string CustomerAddressEnglish, string CustomerDescriptionArabic, string CustomerAddressArabic, string Phonenumber, string Faxnumber, string OnHold, string Taxable, string HeadOfficeCode, string CreditLimit, string Balance, string Longitude, string latitude, string CustomerBarCode, string email, string divisionCode, string inactive, string SALESMAN_NAME,string superVisorCode,string superVisorName,string SalesManagerCode,string SalesManagerName)
        private void CreateCustomerOutlet(string state, string city, string SRNO, string parentAccount, string SALESMAN_CODE, string GroupID, string GroupID2, string B2B_Inv, string CustType, string CustomerCode, string PriceListCode, string Paymentterms, string CustomerDescriptionEnglish, string CustomerAddressEnglish, string CustomerDescriptionArabic, string CustomerAddressArabic, string Phonenumber, string Faxnumber, string OnHold, string Taxable, string HeadOfficeCode, string CreditLimit, string Balance, string Longitude, string latitude, string CustomerBarCode, string email, string divisionCode, string inactive, string SALESMAN_NAME, string superVisorCode, string superVisorName, string SalesManagerCode, string SalesManagerName, string CustomerClassID)
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
                QueryBuilderObject.SetField("CustomerClassID", CustomerClassID);
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
                QueryBuilderObject.SetField("CustomerClassID", CustomerClassID);
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


            string TerritoryID = GetFieldValue("Territory", "TerritoryID", "TerritoryCode='" + SALESMAN_CODE + "'", db_vms);
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
                QueryBuilderObject.SetField("Description", "'" + SALESMAN_NAME + "'");
                QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);

                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "'" + SALESMAN_NAME + "'");
                err = QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);

            }
            else
            {
                QueryBuilderObject.SetField("Description", "'" + SALESMAN_NAME + "'");
                err = QueryBuilderObject.UpdateQueryString("TerritoryLanguage", "TerritoryID=" + TerritoryID + "", db_vms);

            }
            //string deleteCustTerr = "delete from CustOutTerritory where customerid=" + CustomerID + " AND OutletID = " + OutletID + "";
            //qry = new InCubeQuery(deleteCustTerr, db_vms);
            //err = qry.ExecuteNonQuery();

            err = ExistObject("CustOutTerritory", "TerritoryID", "TerritoryID = " + TerritoryID + " AND CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
            if (err != InCubeErrors.Success)
            {

                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                err = QueryBuilderObject.InsertQueryString("CustOutTerritory", db_vms);
                string getNewEmployee = GetFieldValue("EmployeeTerritory", "EmployeeID", "TerritoryID=" + TerritoryID + "", db_vms).Trim();
                if (!getNewEmployee.Equals(string.Empty))
                {
                    QueryBuilderObject.SetField("EmployeeID", getNewEmployee);
                    err = QueryBuilderObject.UpdateQueryString("[Transaction]", "CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and RemainingAmount>0 ", db_vms);


                }
            }

            string RouteID = GetFieldValue("[Route]", "RouteID", "RouteCode='" + SALESMAN_CODE + "'", db_vms);
            if (RouteID == string.Empty)
            {
                RouteID = GetFieldValue("[Route]", "isnull(max(RouteID),0)+1", db_vms);
                QueryBuilderObject.SetField("RouteID", RouteID);
                QueryBuilderObject.SetField("Inactive", "0");
                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                QueryBuilderObject.SetField("RouteCode", "'" + SALESMAN_CODE + "'");
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                err = QueryBuilderObject.InsertQueryString("[Route]", db_vms);

                QueryBuilderObject.SetField("RouteID", RouteID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + SALESMAN_NAME + "'");
                QueryBuilderObject.InsertQueryString("RouteLanguage", db_vms);

                QueryBuilderObject.SetField("RouteID", RouteID);
                QueryBuilderObject.SetField("Week", "0");
                QueryBuilderObject.SetField("Sunday", "1");
                QueryBuilderObject.SetField("Monday", "1");
                QueryBuilderObject.SetField("Tuesday", "1");
                QueryBuilderObject.SetField("Wednesday", "1");
                QueryBuilderObject.SetField("Thursday", "1");
                QueryBuilderObject.SetField("Friday", "1");
                QueryBuilderObject.SetField("Saturday", "1");
                QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("Description", "'" + SALESMAN_CODE + "'");
                err = QueryBuilderObject.UpdateQueryString("RouteLanguage", "RouteID=" + RouteID + "", db_vms);

            }
            //deleteCustTerr = "delete from RouteCustomer where customerid=" + CustomerID + " AND OutletID = " + OutletID + "";
            //qry = new InCubeQuery(deleteCustTerr, db_vms);
            //err = qry.ExecuteNonQuery();

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
            if (CustomerCode == "11380")
            {

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
            int totalu = 0; int totali = 0;
            AddUpdateWarehouse("2", SALESMAN_CODE, SALESMAN_NAME, ref totalu, ref totali, "1");
            AddUpdateSalesperson("2", SALESMAN_CODE, SALESMAN_NAME, ref totalu, ref totali, DivisionID, "1");
            AddUpdateSalesperson("4", superVisorCode, superVisorName, ref totalu, ref totali, DivisionID, "1");
            AddUpdateSalesperson("9", SalesManagerCode, SalesManagerName, ref totalu, ref totali, DivisionID, "1");
            if (err == InCubeErrors.Success)
            {
                UpdateFlag("XXARE_CUSTOMER_OUT_STG", "SRNO=" + SRNO + "");


            }

            #endregion


            #endregion
        }

        #endregion

        #region UpdatePrice

        private DataTable GetDataTable(string Query)
        {
            DataTable dtData = new DataTable();
            err = InCubeErrors.DBNoMoreRows;
            try
            {
                adp = new OracleDataAdapter(Query, Conn);
                adp.Fill(dtData);
                if (dtData != null && dtData.Rows.Count > 0)
                    err = InCubeErrors.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                err = InCubeErrors.Error;
            }
            return dtData;
        }
        private string GetScalarValue(string Query)
        {
            string result = "";
            err = InCubeErrors.DBNoMoreRows;
            try
            {
                OracleCommand cmd = new OracleCommand(Query, Conn);
                if (Conn.State != ConnectionState.Open)
                    Conn.Open();
                object field = cmd.ExecuteScalar();
                if (field != null && field != DBNull.Value)
                {
                    err = InCubeErrors.Success;
                    result = field.ToString();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                err = InCubeErrors.Error;
            }
            return result;
        }
        private void UpdatePriceLists(ref Dictionary<string, string> PriceLists, ref Dictionary<string, string> Groups)
        {
            try
            {
                string PL_CODE = "", PL_NAME = "", PriceListID = "", Group = "";

                DataTable dtPriceLists = GetDataTable(@"
SELECT TBL.PRICELIST_CODE
,(SELECT PRICELIST_NAME FROM XXARE_PRICE_OUT_STG WHERE PRICELIST_CODE = TBL.PRICELIST_CODE AND ROWNUM = 1) PRICELIST_NAME
,(SELECT IS_DEFAULT_PRICELIST FROM XXARE_PRICE_OUT_STG WHERE PRICELIST_CODE = TBL.PRICELIST_CODE AND ROWNUM = 1) IS_DEFAULT_PRICELIST
FROM(SELECT DISTINCT PRICELIST_CODE FROM XXARE_PRICE_OUT_STG) TBL");
                if (err == InCubeErrors.Success)
                {
                    if (!db_vms.IsOpened())
                        err = db_vms.Open("InCube", "IntegrationAreen");
                    if (err == InCubeErrors.Success)
                    {
                        dbTrans = new InCubeTransaction();
                        err = dbTrans.BeginTransaction(db_vms);
                    }
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nUnable to start a DB transaction ..\r\n");
                        return;
                    }

                    //Deactivate current pricelists
                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetField("IsDeleted", "1");
                    err = QueryBuilderObject.UpdateQueryString("PriceList", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("Error in updating old pricelists ..\r\n");
                        throw (new Exception(""));
                    }
                    //delete links of customer to groups or pricelists
                    incubeQuery = new InCubeQuery("DELETE FROM GroupPrice", db_vms);
                    err = incubeQuery.ExecuteNoneQuery(dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("Error in deleting old price groups ..\r\n");
                        throw (new Exception(""));
                    }

                    WriteMessage("\r\nUpdating prices (Stage 1): Updating price lists (" + dtPriceLists.Rows.Count + " rows)..");

                    ClearProgress();
                    SetProgressMax(dtPriceLists.Rows.Count);

                    for (int i = 0; i < dtPriceLists.Rows.Count; i++)
                    {
                        ReportProgress();

                        PL_CODE = dtPriceLists.Rows[i]["PRICELIST_CODE"].ToString().Trim();
                        PL_NAME = dtPriceLists.Rows[i]["PRICELIST_NAME"].ToString().Trim();
                        bool defaultList = false;
                        string IS_DEFAULT = dtPriceLists.Rows[i]["IS_DEFAULT_PRICELIST"].ToString().Trim();
                        if (IS_DEFAULT.Equals(string.Empty)) { defaultList = false; } else { defaultList = true; };

                        PriceListID = GetFieldValue("PriceList", "PriceListID", " PriceListCode = '" + PL_CODE + "'", db_vms, dbTrans);
                        if (PriceListID == string.Empty)
                        {
                            PriceListID = GetFieldValue("PriceList", "ISNULL(MAX(PriceListID),0) + 1", db_vms, dbTrans);

                            QueryBuilderObject.SetField("PriceListID", PriceListID);
                            QueryBuilderObject.SetField("PriceListCode", "'" + PL_CODE + "'");
                            QueryBuilderObject.SetField("StartDate", "'" + DateTime.Now.Date.ToString(DateFormat) + "'");
                            QueryBuilderObject.SetField("EndDate", "'" + DateTime.Now.Date.AddYears(10).ToString(DateFormat) + "'");
                            QueryBuilderObject.SetField("Priority", "1");
                            QueryBuilderObject.SetField("IsDeleted", "0");
                            err = QueryBuilderObject.InsertQueryString("PriceList", db_vms, dbTrans);
                            if (err != InCubeErrors.Success)
                            {
                                WriteMessage("Error in inserting new price list ..\r\n");
                                throw (new Exception(""));
                            }

                            QueryBuilderObject.SetField("PriceListID", PriceListID);
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + PL_NAME + "'");
                            err = QueryBuilderObject.InsertQueryString("PriceListLanguage", db_vms, dbTrans);
                            if (err != InCubeErrors.Success)
                            {
                                WriteMessage("Error in inserting new price list description..\r\n");
                                throw (new Exception(""));
                            }
                        }
                        else
                        {
                            QueryBuilderObject.SetField("IsDeleted", "0");
                            err = QueryBuilderObject.UpdateQueryString("PriceList", "PriceListID = " + PriceListID, db_vms, dbTrans);
                            if (err != InCubeErrors.Success)
                            {
                                WriteMessage("Error in activating old price list..\r\n");
                                throw (new Exception(""));
                            }
                        }

                        Group = GetFieldValue("CustomerGroupLanguage", "GroupID", "Description='" + PL_CODE + "' and LanguageID=1", db_vms, dbTrans).Trim();
                        if (Group.Equals(string.Empty))
                        {
                            Group = GetFieldValue("CustomerGroup", "ISNULL(MAX(GroupID),0) + 1", db_vms, dbTrans);

                            QueryBuilderObject.SetField("GroupID", Group);
                            err = QueryBuilderObject.InsertQueryString("CustomerGroup", db_vms, dbTrans);
                            if (err != InCubeErrors.Success)
                            {
                                WriteMessage("Error in inserting price group ..\r\n");
                                throw (new Exception(""));
                            }

                            QueryBuilderObject.SetField("GroupID", Group);
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("description", "'" + PL_CODE + "'");
                            err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms, dbTrans);
                            if (err != InCubeErrors.Success)
                            {
                                WriteMessage("Error in inserting price group description ..\r\n");
                                throw (new Exception(""));
                            }
                        }
                        if (defaultList)
                        {
                            QueryBuilderObject.SetField("KeyValue", PriceListID);
                            err = QueryBuilderObject.UpdateQueryString("Configuration", "KeyName = 'DefaultPriceListID' AND EmployeeID = -1", db_vms, dbTrans);
                            if (err != InCubeErrors.Success)
                            {
                                WriteMessage("Error in updating default price list ..\r\n");
                                throw (new Exception(""));
                            }
                        }

                        QueryBuilderObject.SetField("GroupID", Group);
                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        err = QueryBuilderObject.InsertQueryString("GroupPrice", db_vms, dbTrans);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("Error in linking price to group ..\r\n");
                            throw (new Exception(""));
                        }

                        PriceLists.Add(PL_CODE, PriceListID);
                        Groups.Add(PL_CODE, Group);
                    }
                }
                else if (err == InCubeErrors.DBNoMoreRows)
                {
                    WriteMessage("\r\nAll prices are up to date ..\r\n");
                    return;
                }
                else
                {
                    WriteMessage("\r\nError in retrieving price lists from Oracle ..\r\n");
                    return;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message != "")
                {
                    WriteMessage("Unexpected error happened ..\r\n");
                }
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                err = InCubeErrors.Error;
            }
            finally
            {
                if (dbTrans != null)
                {
                    if (err == InCubeErrors.Success)
                    {
                        err = dbTrans.Commit();
                    }
                    else
                    {
                        dbTrans.Rollback();
                    }
                }
            }
        }
        private void UpdateCustomersGroups(Dictionary<string, string> PriceLists, Dictionary<string, string> Groups)
        {
            try
            {
                string PL_CODE = "", PriceListID = "", Group = "", CUSTOMER_CODE = "", CustomerID = "";//, OutletCode = "", outletID = "";

                DataTable dtCustomers = GetDataTable("SELECT DISTINCT PRICELIST_CODE,CUSTOMERGROUP_CODE FROM XXARE_PRICE_OUT_STG");

                if (err == InCubeErrors.Success)
                {
                    if (!db_vms.IsOpened())
                        err = db_vms.Open("InCube", "IntegrationAreen");
                    if (err == InCubeErrors.Success)
                    {
                        dbTrans = new InCubeTransaction();
                        err = dbTrans.BeginTransaction(db_vms);
                    }
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nUnable to start a DB transaction ..\r\n");
                        return;
                    }

                    //delete links of customer to groups
                    incubeQuery = new InCubeQuery("DELETE FROM CustomerOutletGroup", db_vms);
                    err = incubeQuery.ExecuteNoneQuery(dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("Error in deleting old groups customers ..\r\n");
                        throw (new Exception(""));
                    }

                    WriteMessage("\r\nUpdating prices (Stage 2): Updating customers price lists (" + dtCustomers.Rows.Count + " rows)..");
                    ClearProgress();
                    SetProgressMax(dtCustomers.Rows.Count);

                    for (int i = 0; i < dtCustomers.Rows.Count; i++)
                    {
                        ReportProgress(i + 1);

                        PL_CODE = dtCustomers.Rows[i]["PRICELIST_CODE"].ToString().Trim();
                        PriceListID = PriceLists[PL_CODE];
                        Group = Groups[PL_CODE];

                        CUSTOMER_CODE = dtCustomers.Rows[i]["CUSTOMERGROUP_CODE"].ToString().Trim();
                        CustomerID = GetFieldValue("Customer", "CustomerID", "CustomerCode='" + CUSTOMER_CODE + "'", db_vms, dbTrans).Trim();
                        if (CustomerID == string.Empty)
                            continue;

                        incubeQuery = new InCubeQuery(db_vms, string.Format(@"INSERT INTO CustomerOutletGroup
SELECT {0},OutletID,{1} FROM CustomerOutlet Where CustomerID = {0}", CustomerID, Group));
                        err = incubeQuery.ExecuteNoneQuery(dbTrans);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("Error in linking custoemr to group ..\r\n");
                            throw (new Exception(""));
                        }
                    }
                }
                else
                {
                    WriteMessage("\r\nError in retrieving customers groups from Oracle ..\r\n");
                    return;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message != "")
                {
                    WriteMessage("Unexpected error happened ..\r\n");
                }
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                err = InCubeErrors.Error;
            }
            finally
            {
                if (dbTrans != null)
                {
                    if (err == InCubeErrors.Success)
                    {
                        err = dbTrans.Commit();
                    }
                    else
                    {
                        dbTrans.Rollback();
                    }
                }
            }
        }

        private void UpdatePriceDefinitions(Dictionary<string, string> PriceLists)
        {
            try
            {
                string ITEM_CODE = "", UOM_CODE = "", PRICE = "", PackID = "", itemID = "", PackTypeID = "";
                int PriceDefinitionID = 0;
                Dictionary<string, string> Packs = new Dictionary<string, string>();

                string totalDetailsCount = GetScalarValue("SELECT Count(Count(*)) FROM XXARE_PRICE_OUT_STG GROUP BY PRICELIST_CODE,ITEM_CODE,ITEM_UOM");
                PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0)+1", db_vms));

                WriteMessage("\r\nUpdating prices (Stage 3): Updating items prices (" + totalDetailsCount + " rows)..");
                ClearProgress();
                SetProgressMax(int.Parse(totalDetailsCount));

                int counter = 0;
                foreach (KeyValuePair<string, string> PLists in PriceLists)
                {
                    try
                    {
                        counter++;

                        DataTable dtPrices = GetDataTable(string.Format(@"SELECT TBL.PRICELIST_CODE, TBL.ITEM_CODE, TBL.ITEM_UOM, STG.PRICE
, (SELECT PRICE FROM XXARE_PRICE_OUT_STG WHERE PRICELIST_CODE = TBL.PRICELIST_CODE AND ITEM_CODE = TBL.ITEM_CODE 
AND ITEM_UOM = TBL.ITEM_UOM AND SRNO = TBL.SRNO) PRICE 
FROM (
SELECT PRICELIST_CODE,ITEM_CODE,ITEM_UOM,Max(SRNO) SRNO 
FROM XXARE_PRICE_OUT_STG 
WHERE PRICELIST_CODE = '{0}' 
GROUP BY PRICELIST_CODE,ITEM_CODE,ITEM_UOM) TBL
INNER JOIN XXARE_PRICE_OUT_STG STG ON STG.PRICELIST_CODE = TBL.PRICELIST_CODE AND STG.ITEM_CODE = TBL.ITEM_CODE AND STG.ITEM_UOM = TBL.ITEM_UOM AND STG.SRNO = TBL.SRNO", PLists.Key));

                        if (err == InCubeErrors.Success)
                        {
                            if (!db_vms.IsOpened())
                                err = db_vms.Open("InCube", "IntegrationAreen");
                            if (err == InCubeErrors.Success)
                            {
                                dbTrans = new InCubeTransaction();
                                err = dbTrans.BeginTransaction(db_vms);
                            }
                            if (err != InCubeErrors.Success)
                            {
                                WriteMessage("Unable to start a DB transaction for pricelist [" + PLists.Key + "] ..\r\n");
                                continue;
                            }

                            incubeQuery = new InCubeQuery("DELETE FROM PriceDefinition WHERE PriceListID = " + PLists.Value, db_vms);
                            err = incubeQuery.ExecuteNoneQuery(dbTrans);
                            if (err != InCubeErrors.Success)
                            {
                                WriteMessage("Error in deleting all prices for pricelist [" + PLists.Key + "] ..\r\n");
                                throw (new Exception());
                            }

                            ClearProgress();
                            SetProgressMax(dtPrices.Rows.Count);

                            for (int i = 0; i < dtPrices.Rows.Count; i++)
                            {
                                ReportProgress(i + 1, string.Format("PriceList [{0}], {1}/{2}", PLists.Key, counter, PriceLists.Count));

                                ITEM_CODE = dtPrices.Rows[i]["ITEM_CODE"].ToString().Trim();
                                UOM_CODE = dtPrices.Rows[i]["ITEM_UOM"].ToString().Trim();
                                PRICE = dtPrices.Rows[i]["PRICE"].ToString().Trim();

                                if (Packs.ContainsKey(ITEM_CODE + "-" + UOM_CODE))
                                {
                                    PackID = Packs[ITEM_CODE + "-" + UOM_CODE];
                                }
                                else
                                {
                                    itemID = GetFieldValue("Item", "ItemID", "Origin='" + ITEM_CODE + "'", db_vms, dbTrans).Trim();
                                    if (itemID.Equals(string.Empty)) continue;
                                    PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + UOM_CODE + "'", db_vms, dbTrans).Trim();
                                    if (PackTypeID.Equals(string.Empty)) continue;
                                    PackID = GetFieldValue("Pack", "PackID", "ItemID=" + itemID + " and PackTypeID=" + PackTypeID + "", db_vms, dbTrans).Trim();
                                    if (PackID.Equals(string.Empty)) continue;
                                    Packs.Add(ITEM_CODE + "-" + UOM_CODE, PackID);
                                }

                                QueryBuilderObject = new QueryBuilder();
                                QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID++.ToString());
                                QueryBuilderObject.SetField("QuantityRangeID", "1");
                                QueryBuilderObject.SetField("PackID", PackID);
                                QueryBuilderObject.SetField("CurrencyID", "1");
                                QueryBuilderObject.SetField("Tax", "0");
                                QueryBuilderObject.SetField("Price", PRICE.ToString());
                                QueryBuilderObject.SetField("PriceListID", PLists.Value);
                                err = QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms, dbTrans);
                                if (err != InCubeErrors.Success)
                                {
                                    WriteMessage("Error in inserting details for pricelist [" + PLists.Key + "] ..\r\n");
                                    throw (new Exception());
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message != "")
                        {
                            WriteMessage("Unexpected error happened ..\r\n");
                        }
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        err = InCubeErrors.Error;
                    }
                    finally
                    {
                        if (dbTrans != null)
                        {
                            if (err == InCubeErrors.Success)
                            {
                                err = dbTrans.Commit();
                            }
                            else
                            {
                                dbTrans.Rollback();
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

        public override void UpdatePrice()
        {
            try
            {
                #region(Declaration)

                //string PriceListID = "1", PL_CODE, PL_NAME;
                //int PriceDefinitionID=1;
                //string CUSTOMER_CODE, CustomerID, outletID, OutletCode;
                //string Group = "";
                //string totalDetailsCount, SrNo, ITEM_CODE, UOM_CODE, PRICE;
                //string itemID, PackTypeID, PackID;
                //long minSrNo = 0, maxSrNo = 0;
                //DataTable dtPrices;
                Dictionary<string, string> PriceLists = new Dictionary<string, string>();
                Dictionary<string, string> Groups = new Dictionary<string, string>();
                //Dictionary<string, string> Packs = new Dictionary<string, string>();

                #endregion

                UpdatePriceLists(ref PriceLists, ref Groups);
                if (err != InCubeErrors.Success)
                    return;

                UpdateCustomersGroups(PriceLists, Groups);
                if (err != InCubeErrors.Success)
                    return;

                UpdatePriceDefinitions(PriceLists);
                //#region (Remove old data)

                //if (!db_vms.IsOpened())
                //    err = db_vms.Open("InCube");
                ////Deactivate current pricelists
                //QueryBuilderObject = new QueryBuilder();
                //QueryBuilderObject.SetField("IsDeleted", "1");
                //err = QueryBuilderObject.UpdateQueryString("PriceList", db_vms);
                ////delete links of customer to groups or pricelists
                //InCubeQuery incubeQuery = new InCubeQuery("DELETE FROM GroupPrice",db_vms);
                //err = incubeQuery.ExecuteNonQuery();
                ////incubeQuery = new InCubeQuery("DELETE FROM CustomerOutletGroup",db_vms);
                ////err = incubeQuery.ExecuteNonQuery();
                //////delete current prices
                ////incubeQuery = new InCubeQuery("DELETE FROM PriceDefinition", db_vms);
                ////err = incubeQuery.ExecuteNonQuery();

                //#endregion



                //                #region(Customers)



                //                #endregion

                //                #region(Prices)

                //                //totalDetailsCount = GetScalarValue("SELECT Count(Count(*)) FROM XXARE_PRICE_OUT_STG WHERE PROCESS_FLAG='N' GROUP BY PRICELIST_CODE,ITEM_CODE,ITEM_UOM");

                //                totalDetailsCount = GetScalarValue("SELECT Count(Count(*)) FROM XXARE_PRICE_OUT_STG GROUP BY PRICELIST_CODE,ITEM_CODE,ITEM_UOM");
                //                //if (err != InCubeErrors.Success || totalDetailsCount == "0")
                //                //{
                //                //    return;
                //                //}

                //                WriteMessage("\r\nUpdating prices (Stage 3): Updating items prices (" + totalDetailsCount + " rows)..");
                //                SetProgressMax( = int.Parse(totalDetailsCount);
                //                ClearProgress();

                //                long srno = 0;
                //                while (err == InCubeErrors.Success)
                //                {
                ////                    dtPrices = GetDataTable(@"SELECT * FROM (
                ////SELECT PRICELIST_CODE,Min(SRNO) MINSRNO,Max(SRNO) MAXSRNO,ITEM_CODE,ITEM_UOM,Min(PRICE) PRICE 
                ////FROM XXARE_PRICE_OUT_STG 
                ////WHERE PROCESS_FLAG='N' 
                ////GROUP BY PRICELIST_CODE,ITEM_CODE,ITEM_UOM 
                ////ORDER BY MINSRNO) TBL
                ////WHERE ROWNUM <= 2500");
                //                    dtPrices = GetDataTable(string.Format(@"SELECT * FROM (
                //SELECT PRICELIST_CODE,Min(SRNO) MINSRNO,Max(SRNO) MAXSRNO,ITEM_CODE,ITEM_UOM,Min(PRICE) PRICE 
                //FROM XXARE_PRICE_OUT_STG 
                //WHERE SRNO > {0}
                //GROUP BY PRICELIST_CODE,ITEM_CODE,ITEM_UOM 
                //ORDER BY MINSRNO) TBL
                //WHERE ROWNUM <= 2500", srno));
                //                    if (err == InCubeErrors.Success)
                //                    {
                //                        minSrNo = long.Parse(dtPrices.Rows[0]["MINSRNO"].ToString());
                //                        maxSrNo = long.Parse(dtPrices.Rows[dtPrices.Rows.Count - 1]["MAXSRNO"].ToString());
                //                        srno = maxSrNo;

                //                        for (int i = 0; i < dtPrices.Rows.Count; i++)
                //                        {
                //                            IntegrationForm.lblProgress.Text = (++IntegrationForm.progressBar1.Value).ToString() + "/" + totalDetailsCount;
                //                            Application.DoEvents();

                //                            PL_CODE = dtPrices.Rows[i]["PRICELIST_CODE"].ToString();
                //                            PriceListID = PriceLists[PL_CODE];
                //                            ITEM_CODE = dtPrices.Rows[i]["ITEM_CODE"].ToString().Trim();
                //                            UOM_CODE = dtPrices.Rows[i]["ITEM_UOM"].ToString().Trim();
                //                            PRICE = dtPrices.Rows[i]["PRICE"].ToString().Trim();

                //                            if (Packs.ContainsKey(ITEM_CODE + "-" + UOM_CODE))
                //                            {
                //                                PackID = Packs[ITEM_CODE + "-" + UOM_CODE];
                //                            }
                //                            else
                //                            {
                //                                itemID = GetFieldValue("Item", "ItemID", "Origin='" + ITEM_CODE + "'", db_vms).Trim();
                //                                if (itemID.Equals(string.Empty)) continue;
                //                                PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + UOM_CODE + "'", db_vms).Trim();
                //                                if (PackTypeID.Equals(string.Empty)) continue;
                //                                PackID = GetFieldValue("Pack", "PackID", "ItemID=" + itemID + " and PackTypeID=" + PackTypeID + "", db_vms).Trim();
                //                                if (PackID.Equals(string.Empty)) continue;
                //                                Packs.Add(ITEM_CODE + "-" + UOM_CODE, PackID);
                //                            }

                //                            //PriceDefinitionID = GetFieldValue("PriceDefinition", "PriceDefinitionID", "PackID = " + PackID + " AND PriceListID = " + PriceListID, db_vms);
                //                            //if (PriceDefinitionID.Equals(string.Empty))
                //                            //{
                //                                //PriceDefinitionID = GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms);
                //                            QueryBuilderObject = new QueryBuilder();
                //                                QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID++.ToString());
                //                                QueryBuilderObject.SetField("QuantityRangeID", "1");
                //                                QueryBuilderObject.SetField("PackID", PackID);
                //                                QueryBuilderObject.SetField("CurrencyID", "1");
                //                                QueryBuilderObject.SetField("Tax", "0");
                //                                QueryBuilderObject.SetField("Price", PRICE.ToString());
                //                                QueryBuilderObject.SetField("PriceListID", PriceListID);
                //                                err = QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);
                //                            //}
                //                            //else
                //                            //{
                //                            //    QueryBuilderObject.SetField("Price", PRICE.ToString());
                //                            //    err = QueryBuilderObject.UpdateQueryString("PriceDefinition", "PackID = " + PackID + " AND PriceListID = " + PriceListID + " AND PriceDefinitionID = " + PriceDefinitionID, db_vms);
                //                            //}
                //                        }

                //                        //cmd = new OracleCommand(string.Format("UPDATE XXARE_PRICE_OUT_STG SET PROCESS_FLAG = 'Y' WHERE SRNO BETWEEN {0} AND {1}", minSrNo, maxSrNo), Conn);
                //                        //if (Conn.State != ConnectionState.Open)
                //                        //    Conn.Open();
                //                        //int affectedRows = cmd.ExecuteNonQuery();
                //                    }
                //                }

                //                WriteMessage("\r\nAll prices are up to date ..\r\n");

                //                #endregion
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

        private void AddUpdateSalesperson(string employeeType, string SalespersonCode, string SalespersonName, ref int TOTALUPDATED, ref int TOTALINSERTED, string DivisionID, string OrganizationID)
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
                    QueryBuilderObject.InsertQueryString("Operator", db_vms);
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

            string territoryID = GetFieldValue("Territory", "TerritoryID", "TerritoryCode='" + SalespersonCode + "'", db_vms).Trim();
            err = ExistObject("EmployeeTerritory", "EmployeeID", "EmployeeID = " + SalespersonID + " AND TerritoryID = " + territoryID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("TerritoryID", territoryID);
                err = QueryBuilderObject.InsertQueryString("EmployeeTerritory", db_vms);
            }

            string warehouseID = GetFieldValue("Warehouse", "WarehouseID", "Barcode='" + SalespersonCode + "'", db_vms).Trim();
            err = ExistObject("EmployeeVehicle", "EmployeeID", "EmployeeID = " + SalespersonID + " AND VehicleID = " + warehouseID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("VehicleID", warehouseID);
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
                QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
            }
        }

        #endregion

        #region UpdateVehicles

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
                err = QueryBuilderObject.UpdateQueryString("WarehouseLanguage", " WarehouseID =" + WarehouseID + " AND LanguageID = 1", db_vms);
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
                string vehID = GetFieldValue("Vehicle", "VehicleID", "vehicleID in (select WarehouseID from Warehouse where WarehouseCode = '" + WarehouseCode + "')", db_vms).Trim();
                if (vehID.Equals(string.Empty)) // Exist Warehouse --- Update Query
                {
                    QueryBuilderObject.SetField("VehicleID", WarehouseID);
                    QueryBuilderObject.SetField("TypeID", "1");

                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    err = QueryBuilderObject.InsertQueryString("Vehicle", db_vms);
                }
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
            catch
            {

            }
        }

        #endregion

        #region UpdateStock

        public override void UpdateStock()
        {
            try
            {
                if (!db_vms.IsOpened())
                {
                    WriteMessage("\r\n");
                    WriteMessage("Cannot connect to GP database , please check the connection");
                    return;
                }
                UpdateStockForMainWarehouse();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void UpdateStockForMainWarehouse()
        {
            try
            {
                int TOTALUPDATED = 0;
                int TOTALSKIPPED = 0;

                WriteMessage("\r\nUpdating stock for main warehouse ...\r\n");
                qry = new InCubeQuery(db_vms, "SELECT TOP 1 WarehouseID FROM VehicleLoadingWh");
                object warehouseID = "";
                qry.ExecuteScalar(ref warehouseID);
                if (warehouseID.ToString() == "")
                {
                    WriteMessage("\r\nNo main warehouse found !!\r\n");
                    return;
                }
                qry = new InCubeQuery(db_vms, "DELETE FROM WarehouseStock WHERE WarehouseID = " + warehouseID);
                qry.ExecuteNonQuery();

                DataTable DTBL = GetDataTable(@"SELECT LCS_ITEM_CODE,ITEM_UOM_CODE,Sum(LCS_STK_QTY_BU) Quantity 
                                                FROM XXARE_ONHAND_QUANTITY_V where LCS_STK_QTY_BU>0 GROUP BY LCS_ITEM_CODE,ITEM_UOM_CODE");
                if (err != InCubeErrors.Success)
                {
                    if (err == InCubeErrors.DBNoMoreRows)
                        WriteMessage("\r\nNo stock found in staging table !!\r\n");
                    else
                        WriteMessage("\r\nError in reading stock from staging table !!\r\n");
                    return;
                }

                ClearProgress();
                SetProgressMax(DTBL.Rows.Count);

                foreach (DataRow row in DTBL.Rows)
                {
                    ReportProgress("Updating Stock");

                    string ItemCode = row["LCS_ITEM_CODE"].ToString().Trim();
                    string PackTypeCode = row["ITEM_UOM_CODE"].ToString().Trim();
                    string Quantity = row["Quantity"].ToString().Trim();
                    string Batch = "1990/01/01";
                    DateTime expiry = new DateTime(2025, 1, 1);

                    string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + PackTypeCode + "'", db_vms).Trim();
                    if (PackTypeID.Equals(string.Empty))
                    {
                        WriteMessage(string.Format("UOM [{0}] is not defined in InVan, skipping row: {1},{0},{2}\r\n", PackTypeCode, ItemCode, Quantity));
                        TOTALSKIPPED++;
                        continue;
                    }

                    string ItemID = GetFieldValue("Item", "ItemID", "Origin='" + ItemCode + "' ", db_vms).Trim();
                    if (ItemID.Equals(string.Empty))
                    {
                        WriteMessage(string.Format("Item [{0}] is not defined in InVan, skipping row: {0},{1},{2}\r\n", ItemCode, PackTypeCode, Quantity));
                        TOTALSKIPPED++;
                        continue;
                    }

                    string PackID = GetFieldValue("Pack", "PackID", "ItemID=" + ItemID + " and packTypeID=" + PackTypeID + "", db_vms).Trim();
                    if (PackID.Equals(string.Empty))
                    {
                        WriteMessage(string.Format("There is no pack defined for Item [{0}] and UOM [{1}], skipping row: {0},{1},{2}\r\n", ItemCode, PackTypeCode, Quantity));
                        TOTALSKIPPED++;
                        continue;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetField("WarehouseID", warehouseID.ToString());
                    QueryBuilderObject.SetField("ZoneID", "1");
                    QueryBuilderObject.SetField("PackID", PackID);
                    QueryBuilderObject.SetDateField("ExpiryDate", expiry);
                    QueryBuilderObject.SetStringField("BatchNo", Batch);
                    QueryBuilderObject.SetField("Quantity", Quantity);
                    QueryBuilderObject.SetField("SampleQuantity", Quantity);
                    QueryBuilderObject.SetField("BaseQuantity", Quantity);
                    QueryBuilderObject.SetField("StockStatusID", "-1");
                    err = QueryBuilderObject.InsertQueryString("WarehouseStock", db_vms);
                    if (err == InCubeErrors.Success)
                    {
                        TOTALUPDATED++;
                    }
                    else
                    {
                        WriteMessage(string.Format("Error in inserting stock for row: {0},{1},{2}\r\n", ItemCode, PackTypeCode, Quantity));
                        TOTALSKIPPED++;
                    }
                }

                qry = new InCubeQuery(db_vms, string.Format(@"INSERT INTO WarehouseStock (WarehouseID,ZoneID,PackID,ExpiryDate,BatchNo,Quantity,SampleQuantity,BaseQuantity,StockStatusID)
SELECT {0},1,AP.PackID,'1990-01-01','1990/01/01',0,0,0,-1
FROM WarehouseStock WH
INNER JOIN Pack P ON P.PackID = WH.PackID
INNER JOIN Pack AP ON AP.ItemID = P.ItemID
LEFT JOIN WarehouseStock WS ON WS.PackID = AP.PackID AND WS.WarehouseID = {0}
WHERE WH.WarehouseID = {0} AND WS.WarehouseID IS NULL", warehouseID));
                qry.ExecuteNonQuery();

                WriteMessage("Updating stock completed, updated: " + TOTALUPDATED + ", skipped: " + TOTALSKIPPED);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void UpdateStockForWarehouse(string WarehouseID, string WarehouseCode)
        {
            //  int count = 0;
            int TOTALUPDATED = 0;
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

                else
                {
                    qry = new InCubeQuery("DELETE FROM WAREHOUSESTOCK WHERE WarehouseID=" + WarehouseID, db_vms);
                    err = qry.ExecuteNonQuery();
                    DataTable DTBL = new DataTable();

                    DTBL = GetDataTable("SELECT * from XXARE_ONHAND_QUANTITY_V where LCS_STK_QTY_BU>0 and  LCS_LOCN_CODE ='" + WarehouseCode + "'");
                    if (err != InCubeErrors.Success)
                        return;

                    ClearProgress();
                    SetProgressMax(DTBL.Rows.Count);
                    WriteExceptions("the number of items are " + DTBL.Rows.Count.ToString() + "", "Number of Items", false);

                    foreach (DataRow row in DTBL.Rows)
                    {
                        ReportProgress("Updating Stock");

                        string vehicleCode = row["LCS_LOCN_CODE"].ToString().Trim();

                        string ItemCode = row["LCS_ITEM_CODE"].ToString().Trim();
                        string PackTypeCode = row["ITEM_UOM_CODE"].ToString().Trim();


                        string Quantity = row["LCS_STK_QTY_BU"].ToString().Trim();
                        string Batch = "1990/01/01";// row["BatchNo"].ToString().Trim();
                        string DivisionCode = "1";// row["CompanyID"].ToString().Trim();
                        string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode=" + DivisionCode + "", db_vms).Trim();
                        if (DivisionID.Equals(string.Empty))
                            continue;

                        string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + PackTypeCode + "'", db_vms).Trim();
                        if (PackTypeID.Equals(string.Empty))
                            continue;

                        string expiry = DateTime.Parse("01/01/1990 00:00:00").ToString(DateFormat);
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
                        string Expirydate = DateTime.Parse("01/01/1990 00:00:00").ToString(DateFormat);

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

                            string existStock = GetFieldValue("WarehouseStock", "PackID", "WarehouseID = " + vehicleID + " AND ZoneID = 1 AND PackID = " + _packid + " AND BatchNo = '" + Batch + "'", db_vms).Trim();
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

                            }
                            else if (_packid == PackID)
                            {

                                string beforeQty = GetFieldValue("WarehouseStock", "Quantity", "WarehouseID = " + vehicleID + " AND ZoneID = 1 AND PackID = " + _packid + " AND BatchNo = '" + Batch + "'", db_vms).Trim();
                                if (beforeQty.Equals(string.Empty)) beforeQty = "0";
                                WriteExceptions("OLD STOCK QUANTITY BEFORE UPDATE  = " + beforeQty + " ,THE ADDED QUANTITY = " + Quantity + " , THE TOTAL = " + (decimal.Parse(beforeQty) + decimal.Parse(Quantity)) + "  transaction =  ---- pack id is " + _packid + "", "UPDATE Stock ", false);
                                QueryBuilderObject.SetField("Quantity", "Quantity+" + Quantity);
                                QueryBuilderObject.SetField("BaseQuantity", "BaseQuantity+" + Quantity);
                                err = QueryBuilderObject.UpdateQueryString("WarehouseStock", "WarehouseID = " + vehicleID + " AND ZoneID = 1 AND PackID = " + PackID + " AND BatchNo = '" + Batch + "'", db_vms);
                                if (err == InCubeErrors.Success)
                                {
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

            }
            catch
            {

            }
            WriteMessage("\r\n");
            WriteMessage("<<< STOCK Updated ,>>> Total Updated = " + TOTALUPDATED);

            //ClassStartOFDay.StartofDay(db_vms, WarehouseID);

            #endregion
        }

        public override void UpdateMainWHStock()
        {
            try
            {
                object field = new object();
                WriteExceptions("STARTING MAIN WH STOCK UPDATE", "MAIN WAREHOUSE STOCK", false);
                string deleteMainStock = string.Format("delete from warehousestock where warehouseid in (select warehouseid from warehouse where warehousetypeid=1)");
                InCubeQuery delQry = new InCubeQuery(deleteMainStock, db_vms);
                err = delQry.ExecuteNonQuery();
                WriteExceptions("MAIN STOCK DELETED", "MAIN WAREHOUSE STOCK", false);
                DataTable DT = new DataTable();
                string select = @"SELECT
mtlcb.segment1 category 
,msi.segment1 item_internal_code 
,msi.description
,mtcr.cross_reference item_code
,msi.primary_uom_code uom_code
,sum(nvl(moqd.primary_transaction_quantity,0)) Tot_Qt
FROM
   MTL_SYSTEM_ITEMS_VL   msi,
   MTL_ONHAND_QUANTITIES_DETAIL moqd,
   mtl_item_categories mtlc,
   mtl_categories_b mtlcb,
   mtl_cross_references mtcr
WHERE
msi.inventory_item_id = mtlc.inventory_item_id
AND msi.organization_id = mtlc.organization_id
AND mtlc.category_id = mtlcb.category_id
AND msi.inventory_item_id = moqd.inventory_item_id (+)
and msi.organization_id = moqd.organization_id (+)
and mtcr.inventory_item_id  =  msi.inventory_item_id
and mtcr.cross_reference_type = 'Supplier Item'
and msi.stock_enabled_flag = 'Y'
and msi.organization_id = '85' 
GROUP BY
mtlcb.segment1
,msi.segment1
,msi.inventory_item_id
,msi.description
,mtcr.cross_reference
,msi.primary_uom_code
ORDER BY 1,3
";

                DT = OLEDB_Datatable(select);
                WriteExceptions("STOCK SELECTED, RECORD COUNT IS " + DT.Rows.Count.ToString() + "", "MAIN WAREHOUSE STOCK", false);

                ClearProgress();
                SetProgressMax(DT.Rows.Count);

                foreach (DataRow row in DT.Rows)
                {
                    ReportProgress("Updating Main WH Stock");

                    string warehouseID = string.Empty;
                    string PackID = string.Empty;
                    string itemcode = row["ITEM_CODE"].ToString().Trim();
                    WriteExceptions("ITEM CODE IS " + itemcode + "", "MAIN WAREHOUSE STOCK", false);
                    string Origin = row["ITEM_INTERNAL_CODE"].ToString().Trim();
                    WriteExceptions("ORIGIN IS " + Origin + "", "MAIN WAREHOUSE STOCK", false);
                    string UOM = row["UOM_CODE"].ToString().Trim();
                    WriteExceptions("UOM IS " + UOM + "", "MAIN WAREHOUSE STOCK", false);
                    string Quantity = row["TOT_QT"].ToString().Trim();
                    string warehousecode = "MAIN";
                    string expiry = DateTime.Parse("01/01/1990 00:00:00").ToString(DateFormat);
                    string batchno = "1990/01/01";
                    WriteExceptions("GETTING WAREHOUSE ID AND PACK ID ", "MAIN WAREHOUSE STOCK", false);
                    warehouseID = GetFieldValue("warehouse", "warehouseID", "warehousecode='" + warehousecode + "'", db_vms).Trim();
                    PackID = GetFieldValue("Pack", "PackID", "ItemID in (select ItemID from Item where Origin='" + Origin + "') and packtypeID in (select packTypeID from PackTypeLanguage where description='" + UOM + "' and languageID=1)", db_vms).Trim();


                    string ItemID = GetFieldValue("Item", "ItemID", "Origin='" + Origin + "'", db_vms).Trim();
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

                        string existStock = GetFieldValue("WarehouseStock", "PackID", "WarehouseID = " + warehouseID + " AND ZoneID = 1 AND PackID = " + _packid + " AND BatchNo = '" + batchno + "'", db_vms).Trim();
                        if (existStock.Equals(string.Empty))
                        {
                            QueryBuilderObject.SetField("WarehouseID", warehouseID);
                            QueryBuilderObject.SetField("ZoneID", "1");
                            QueryBuilderObject.SetField("PackID", _packid);
                            QueryBuilderObject.SetField("ExpiryDate", "'" + expiry + "'");
                            QueryBuilderObject.SetField("BatchNo", "'" + batchno + "'");
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
                            QueryBuilderObject.SetField("StockStatusID", "1");
                            err = QueryBuilderObject.InsertQueryString("WarehouseStock", db_vms);

                        }
                        else if (_packid == PackID)
                        {

                            string beforeQty = GetFieldValue("WarehouseStock", "Quantity", "WarehouseID = " + warehouseID + " AND ZoneID = 1 AND PackID = " + _packid + " AND BatchNo = '" + batchno + "'", db_vms).Trim();
                            if (beforeQty.Equals(string.Empty)) beforeQty = "0";
                            WriteExceptions("OLD STOCK QUANTITY BEFORE UPDATE  = " + beforeQty + " ,THE ADDED QUANTITY = " + Quantity + " , THE TOTAL = " + (decimal.Parse(beforeQty) + decimal.Parse(Quantity)) + "  transaction =  ---- pack id is " + _packid + "", "UPDATE Stock ", false);
                            QueryBuilderObject.SetField("Quantity", "Quantity+" + Quantity);
                            QueryBuilderObject.SetField("BaseQuantity", "BaseQuantity+" + Quantity);
                            err = QueryBuilderObject.UpdateQueryString("WarehouseStock", "WarehouseID = " + warehouseID + " AND ZoneID = 1 AND PackID = " + PackID + " AND BatchNo = '" + batchno + "'", db_vms);
                            if (err == InCubeErrors.Success)
                            {
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


                    // WriteExceptions("WAREHOUSEID IS " + warehouseID + "  AND PACKID IS "+packID+"", "MAIN WAREHOUSE STOCK", false);
                    //string insertStock = string.Format(@" INSERT INTO WAREHOUSESTOCK (WAREHOUSEID,ZONEID,PACKID,EXPIRYDATE,BATCHNO,QUANTITY,SAMPLEQUANTITY,BASEQUANTITY,STOCKSTATUSID) VALUES (" + warehouseID + ",1,"+packID+",'"+expiry+"','"+batchno+"',"+Quantity+",0,"+Quantity+",1)");
                    //WriteExceptions("INSERT STATEMENT IS<<< " + insertStock + ">>>", "MAIN WAREHOUSE STOCK", false);
                    //qry = new InCubeQuery(insertStock, db_vms);
                    //err = qry.ExecuteNonQuery();
                    //if (err == InCubeErrors.Success)
                    //{
                    //    WriteExceptions("INSERT STATEMENT WAS SUCCESSFULL", "MAIN WAREHOUSE STOCK", false);
                    //}
                    //else
                    //{
                    //    WriteExceptions("INSERT STATEMENT FAILED", "MAIN WAREHOUSE STOCK", false);
                    //}
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        #endregion

        #region Update Discount

        public override void UpdateDiscount()
        {
            try
            {
                int maxDiscountID = 1;
                Dictionary<string, int> discounts = new Dictionary<string, int>();
                object field = new object();
                InCubeTransaction dbTrans = new InCubeTransaction();
                if (!db_vms.IsOpened()) db_vms.Open("InCube", "IntegrationAreen");
                err = dbTrans.BeginTransaction(db_vms);
                if (err != InCubeErrors.Success)
                {
                    WriteMessage("\r\nFailed to open sql database transaction");
                    return;
                }

                InCubeQuery DeleteDiscountQuery = new InCubeQuery(db_vms, @"DELETE FROM DiscountLanguage;
DELETE FROM DiscountAssignment;
DELETE FROM Discount;");
                err = DeleteDiscountQuery.ExecuteNoneQuery(dbTrans);
                if (err != InCubeErrors.Success)
                {
                    WriteMessage("Failed to delete old discounts");
                    dbTrans.Rollback();
                    return;
                }

                DataTable DT = new DataTable();
                string select = string.Format(@"select distinct H.Discount_Code,H.Discount_Value,H.Valid_From,nvl(h.valid_to,'01-JAN-2030') Valid_TO,D.PriceList_Code, h.subcategory_code
from xxare_discount_line_out_stg D inner join  xxare_discount_head_out_stg H
 ON h.discount_code= d.discount_code 
WHERE h.process_flag='N' AND D.pricelist_code IS NOT NULL and nvl(h.valid_to,'01-JAN-2030')>(select sysdate from dual) order by  H.Discount_Code");


                DT = OLEDB_Datatable(select);

                ClearProgress();
                SetProgressMax(DT.Rows.Count);

                int discountAssignmentID = 1;

                foreach (DataRow row in DT.Rows)
                {
                    ReportProgress("Updating Discounts");

                    string CustomerGroupCode = row["PriceList_Code"].ToString().Trim();
                    string ItemGroupCode = row["subcategory_code"].ToString().Trim();
                    string DicsountCode = row["Discount_Code"].ToString().Trim();
                    string DiscountValue = row["Discount_Value"].ToString().Trim();
                    string FromDate = row["Valid_From"].ToString().Trim();
                    string ToDate = row["Valid_TO"].ToString().Trim();
                    if (FromDate.Equals(string.Empty)) FromDate = DateTime.Parse("01-JAN-2000").ToString(DateFormat);
                    if (ToDate.Equals(string.Empty)) ToDate = DateTime.Parse("01-JAN-2030").ToString(DateFormat);

                    string CustomerGroupID = GetFieldValue("CustomerGroupLanguage", "GroupID", " Description = '" + CustomerGroupCode + "' and LanguageID=1", db_vms, dbTrans);
                    string ItemGroupID = GetFieldValue("PackGroupLanguage", "PackGroupID", " Description = '" + ItemGroupCode + "' and LanguageID=1", db_vms, dbTrans);

                    if (ItemGroupID == string.Empty)
                    {
                        WriteMessage("\r\nItem group [" + ItemGroupCode + "] not found, discount [" + DicsountCode + "] will be ignored");
                        continue;
                    }

                    if (CustomerGroupID == string.Empty)
                    {
                        WriteMessage("\r\nCustomer group [" + CustomerGroupCode + "] not found, discount [" + DicsountCode + "] will be ignored");
                        continue;
                    }
                    decimal Discount = 0;
                    if (!decimal.TryParse(DiscountValue, out Discount))
                    {
                        WriteMessage("\r\nInvalid discount value [" + DiscountValue + "]");
                        continue;
                    }

                    int discountID = 0;
                    if (!discounts.ContainsKey(DicsountCode))
                    {
                        discountID = maxDiscountID++;
                        discounts.Add(DicsountCode, discountID);
                        QueryBuilderObject.SetField("DiscountID", discountID.ToString());
                        QueryBuilderObject.SetStringField("DiscountCode", DicsountCode);
                        QueryBuilderObject.SetField("PackGroupID", ItemGroupID);
                        QueryBuilderObject.SetField("DiscountTypeID", "1");
                        QueryBuilderObject.SetField("TypeID", "1");
                        QueryBuilderObject.SetField("Discount", Discount.ToString());
                        QueryBuilderObject.SetField("OrganizationID", "1");
                        QueryBuilderObject.SetField("FOC", "0");
                        QueryBuilderObject.SetField("StartDate", "'" + DateTime.Parse(FromDate).ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + DateTime.Parse(ToDate).ToString(DateFormat) + "'");
                        err = QueryBuilderObject.InsertQueryString("Discount", db_vms, dbTrans);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nfailed to insert discount [" + DicsountCode + "] header");
                            dbTrans.Rollback();
                            return;
                        }

                        QueryBuilderObject.SetField("DiscountID", discountID.ToString());
                        QueryBuilderObject.SetStringField("Description", DicsountCode);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        err = QueryBuilderObject.InsertQueryString("DiscountLanguage", db_vms, dbTrans);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nfailed to insert discount [" + DicsountCode + "] description");
                            dbTrans.Rollback();
                            return;
                        }
                    }
                    else
                    {
                        discountID = discounts[DicsountCode];
                    }

                    QueryBuilderObject.SetField("DiscountAssignmentID", (discountAssignmentID++).ToString());
                    QueryBuilderObject.SetField("DiscountID", discountID.ToString());
                    QueryBuilderObject.SetField("CustomerGroupID", CustomerGroupID);
                    err = QueryBuilderObject.InsertQueryString("DiscountAssignment", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nfailed to insert discount [" + DicsountCode + "] assignment");
                        dbTrans.Rollback();
                        return;
                    }
                }
                err = dbTrans.Commit();
                DT.Dispose();

                WriteMessage("\r\n");
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        #endregion

        #endregion

        #region ORION SEND

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
                string invoices = string.Format(@"select T.TransactionID,T.TransactionDate,'AED',Cust.CustomerCode as BillTo,CO.CustomerCode as ShipTo,
(case(isnull(CO.CustomerTypeID,1)) when 1 then 'CASH'else cast(PT.SimplePeriodWidth as nvarchar(30))+' DAYS' end) as Term
,TD.Sequence,I.ItemCode,PTL.Description as UOM,TD.Quantity,TD.Price               
from [Transaction] T
inner join Customer Cust on T.CustomerID=Cust.CustomerID
inner join CustomerOutlet CO on T.CustomerID=CO.CustomerID and T.OutletID=CO.OutletID
left outer join PaymentTerm PT on CO.PaymentTermID=PT.PaymentTermID
inner join TransactionDetail TD on T.TransactionID=TD.TransactionID and T.CustomerID=TD.CustomerID and T.OutletID=TD.OutletID
inner join Pack P on TD.PackID=P.PackID 
inner join Item I on I.ItemID=P.ItemID
INNER JOIN PACKTYPELANGUAGE PTL ON PTL.PACKTYPEID=P.PACKTYPEID AND PTL.LANGUAGEID=1
where Synchronized=0 
AND (isnull(T.Notes,'0')<>'ERP') AND (T.TransactionTypeID = 1 or T.TransactionTypeID = 3) {0}
group by 
T.TransactionID,T.TransactionDate,Cust.CustomerCode,CO.CustomerCode,CO.CustomerTypeID,PT.SimplePeriodWidth,TD.Sequence,I.ItemCode,PTL.Description ,TD.Quantity,TD.Price
                 
                 
                ", sp);
                //the query above used to have date range : AND 
                //(T.TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
                //AND T.TransactionDate <= '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"')
                #region Invoice Header
                InCubeQuery invoiceQry = new InCubeQuery(invoices, db_vms);
                err = invoiceQry.Execute();
                dt = new DataTable();
                dt = invoiceQry.GetDataTable();
                string tranDetail = string.Empty;
                string insertDetail = string.Empty;
                int TOTALINSERTED = 0;
                ClearProgress();
                SetProgressMax(dt.Rows.Count);

                foreach (DataRow dr in dt.Rows)
                {
                    _tran = Conn.BeginTransaction();
                    string TRX_NUMBER = dr["TransactionID"].ToString().Trim();
                    string TRX_DATE = dr["TransactionDate"].ToString().Trim();
                    string CURRENCY = "AED";
                    string BILL_CUSTOMER_NUMBER = dr["BillTo"].ToString().Trim();
                    string SHIP_CUSTOMER_NUMBER = dr["ShipTo"].ToString().Trim();
                    string PAY_TERM = dr["Term"].ToString().Trim();
                    string LINE_NUMBER = dr["Sequence"].ToString().Trim();
                    string ITEM_CODE = dr["ItemCode"].ToString().Trim();
                    string UOM_CODE = dr["UOM"].ToString().Trim();
                    string QTY_INVOICED = dr["Quantity"].ToString().Trim();
                    string SELLING_PRICE = dr["Price"].ToString().Trim();
                    string SALES_ORDER_NUMBER = string.Empty;
                    string SALES_ORDER_LINE_NUMBER = string.Empty;
                    string PROCESS_FLAG = "N";


                    string insert = string.Format(@"insert into XXARE_SALESTXN_STG
(TRX_NUMBER
TRX_DATE
CURRENCY
BILL_CUSTOMER_NUMBER
SHIP_CUSTOMER_NUMBER
PAY_TERM
LINE_NUMBER
ITEM_CODE
UOM_CODE
QTY_INVOICED
SELLING_PRICE
PROCESS_FLAG)
Values
(
'" + TRX_NUMBER + "','" + TRX_DATE + "','" + CURRENCY + "','" + BILL_CUSTOMER_NUMBER + "','" + SHIP_CUSTOMER_NUMBER + "','" + PAY_TERM + "'," + LINE_NUMBER + ",'" + ITEM_CODE + "','" + UOM_CODE + "'," + QTY_INVOICED + "," + SELLING_PRICE + ",'" + PROCESS_FLAG + "')");

                    OracleCommand cmdHDR = new OracleCommand(insert, Conn);
                    cmdHDR.Transaction = _tran;
                    err = ExecuteNonQuery(cmdHDR);
                    cmdHDR.Dispose();

                    if (err == InCubeErrors.Success)
                    {
                        string update = string.Format("update [Transaction] set Synchronized=1 where TransactionID='{0}'", TRX_NUMBER);
                        invoiceQry = new InCubeQuery(update, db_vms);
                        err = invoiceQry.ExecuteNonQuery();
                        WriteMessage("\r\n" + TRX_NUMBER + " - Transaction sent Successfully");
                        TOTALINSERTED++;
                        _tran.Commit();
                    }
                    else
                    {
                        WriteMessage("\r\n" + TRX_NUMBER + " - Transaction Failed");
                        _tran.Rollback();
                    }
                }
                WriteMessage("\r\n");
                //WriteMessage("<<< Transactions >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
                //THIS CALLS THE STORED PROCEDURE THAT FIXES THE DOCUMENT SEQUENCE .
                //err = DatabaseSpecialFunctions.RunStoredProcedure(db_vms, "spHandleDocumentSequence");

            }
            catch (Exception ex)
            {
                if (_tran != null) _tran.Rollback();

                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                if (_tran != null) _tran.Dispose();
            }
        }
        #endregion
        #endregion

        #region SendReturns

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
                #region Another Query

                #endregion
                //SOME TRANSACTIONS ARE WITHOUT DETAILS ==> CUSTOMER PAYMENT FOR THOSE TRANSACTIONS WILL NOT APPEAR BECAUSE OF DIVISION ID
                //TO DO : Handle the date format
                DataTable dt = new DataTable();
                string concat = string.Empty;
                if (Filters.EmployeeID != -1)
                {
                    concat = "    AND CP.EmployeeID = " + Filters.EmployeeID;
                }
                string receipt = string.Format(@"select CP.CustomerPaymentID,CP.PaymentDate,'AED' as Currency,PTL.description as RECEIPT_METHOD,C.CustomerCode as Bill,
CO.CustomerCode as Ship,CP.AppliedAmount,CP.VoucherNumber,CP.VoucherDate,CP.TransactionID,CP.AppliedAmount
from CustomerPayment CP 
inner join CustomerOutlet CO on CO.CustomerID=CP.CustomerID and CO.OutletID=CP.OutletID 
inner join Customer C on C.CustomerID=CP.CustomerID
inner join PaymentTypeLanguage PTL on CP.PaymentTypeID=PTL.PaymentTypeID and PTL.LanguageID=1
where (CP.Synchronized = 0) 
and CP.PaymentTypeID<>4 and CP.PaymentStatusID <>5 {0}
Group By
CO.Barcode,CP.PaymentDate,CP.CustomerPaymentID,CP.TransactionID,CP.PaymentTypeID,
CP.VoucherNumber,CP.VoucherDate,CP.VoucherOwner,CP.Notes,CP.CurrencyID,CP.AppliedAmount,CP.Synchronized,CP.GPSLatitude,CP.GPSLongitude,
CP.PaymentStatusID,CP.SourceTransactionID,CP.RemainingAmount,CP.Posted,CP.Downloaded,CP.VisitNo,CP.RouteHistoryID,CP.AppliedPaymentID,CP.AccountID,CP.BounceDate,
CO.CustomerTypeID
                     ", concat);

                InCubeQuery receiptQry = new InCubeQuery(receipt, db_vms);
                err = receiptQry.Execute();
                dt = receiptQry.GetDataTable();
                string insertReceipt = string.Empty;
                string proddate = string.Empty;
                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;
                ClearProgress();
                SetProgressMax(dt.Rows.Count);
                foreach (DataRow dr in dt.Rows)
                {
                    _tran = Conn.BeginTransaction();
                    string RECEIPT_NUMBER = dr["CustomerPaymentID"].ToString().Trim();
                    string RECEIPT_DATE = dr["PaymentDate"].ToString().Trim();
                    string CURRENCY = dr["Currency"].ToString().Trim();
                    string RECEIPT_METHOD = dr["RECEIPT_METHOD"].ToString().Trim();
                    string BILL_CUSTOMER_NUMBER = dr["Bill"].ToString().Trim();
                    string SHIP_CUSTOMER_NUMBER = dr["Ship"].ToString().Trim();
                    string RECEIPT_AMOUNT = dr["AppliedAmount"].ToString().Trim();
                    string CHECK_NUMBER = dr["VoucherNumber"].ToString().Trim();
                    string CHECK_DATE = dr["VoucherDate"].ToString().Trim();
                    string TRX_NUMBER = dr["TransactionID"].ToString().Trim();
                    string APPLY_AMOUNT = dr["AppliedAmount"].ToString().Trim();
                    string PROCESS_FLAG = "N";

                    string insert = string.Format(@"insert into XXARE_PAYMENTS_STG
(RECEIPT_NUMBER
,RECEIPT_DATE
,CURRENCY
,RECEIPT_METHOD
,BILL_CUSTOMER_NUMBER
,SHIP_CUSTOMER_NUMBER
,RECEIPT_AMOUNT
,CHECK_NUMBER
,CHECK_DATE
,TRX_NUMBER
,APPLY_AMOUNT
,PROCESS_FLAG)
VALUES
(
'" + RECEIPT_NUMBER
+ "' ,'" + RECEIPT_DATE
+ "','" + CURRENCY
+ "','" + RECEIPT_METHOD
+ "' ,'" + BILL_CUSTOMER_NUMBER
+ "' ,'" + SHIP_CUSTOMER_NUMBER
+ "' ," + RECEIPT_AMOUNT
+ " ,'" + CHECK_NUMBER
+ "' ,'" + CHECK_DATE
+ "' ,'" + TRX_NUMBER
+ "' ," + APPLY_AMOUNT
+ " ,'" + PROCESS_FLAG + "')"
);
                    OracleCommand cmdHDR = new OracleCommand(insert, Conn);
                    cmdHDR.Transaction = _tran;
                    err = ExecuteNonQuery(cmdHDR);
                    cmdHDR.Dispose();
                    if (err == InCubeErrors.Success)
                    {
                        string update = string.Format("update [CustomerPayment] set Synchronized=1 where CustomerPaymentID='{0}' and TransactionID='{1}'", RECEIPT_NUMBER, TRX_NUMBER);
                        InCubeQuery invoiceQry = new InCubeQuery(update, db_vms);
                        err = invoiceQry.ExecuteNonQuery();
                        WriteMessage("\r\n" + TRX_NUMBER + " - Transaction sent Successfully");
                        TOTALINSERTED++;
                        _tran.Commit();
                    }
                    else
                    {
                        WriteMessage("\r\n" + RECEIPT_NUMBER + " - Receipt Failed");
                        _tran.Rollback();
                    }
                }
                WriteMessage("\r\n");
                WriteMessage("<<< Receipts >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);

            }
            catch (Exception ex)
            {
                if (_tran != null) _tran.Rollback();
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
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
select WH.Barcode,WHT.TransactionID,TransactionTypeID,TransactionDate ,EE.EmployeeCode,E.EmployeeCode,
'notes',Synchronized,isnull(WHT.ProductionDate,getdate()),WHH.Barcode,Posted,Downloaded,WarehouseTransactionStatusID,CreationSourceID,DivisionID
From WarehouseTransaction WHT inner join Warehouse WH on WHT.WarehouseID=WH.WarehouseID 
inner join Warehouse WHH on WHT.RefWarehouseID=WHH.WarehouseID
inner join Employee E on WHT.ImplementedBy=E.EmployeeID 
inner join Employee EE on WHT.RequestedBy=EE.EmployeeID
where WHT.Synchronized=0 and (WHT.TransactionTypeID=1 or WHT.TransactionTypeID=2) and (WarehouseTransactionStatusID=2 or WarehouseTransactionStatusID=1 )

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
                    _tran = Conn.BeginTransaction();
                    object field = new object();
                    ReportProgress("Sending Transfers");

                    string RH_SYS_ID = string.Empty;
                    string RH_COMP_CODE = string.Empty;
                    string RH_TXN_CODE = string.Empty;
                    string RH_NO = string.Empty;
                    string RH_DT = string.Empty;
                    string RH_DOC_SRC_LOCN_CODE = string.Empty;
                    string RH_AMD_NO = string.Empty;
                    string RH_LOCN_CODE = string.Empty;
                    string RH_REF_FROM = string.Empty;
                    string RH_CHARGE_CODE = string.Empty;
                    string RH_DEL_REQD_DT = string.Empty;
                    string RH_CHARGE_AREA = string.Empty;
                    string RH_CHARGE_AREA_NUM = string.Empty;
                    string RH_REF_FROM_NUM = string.Empty;
                    string RH_APPR_STATUS = string.Empty;
                    string RH_RESVATCL_NUM = string.Empty;
                    string RH_ANNOTATION = string.Empty;
                    string RH_CR_UID = string.Empty;
                    string RH_CR_DT = string.Empty;
                    string RH_PURREQ_YN = string.Empty;

                    OracleCommand cmdGet = new OracleCommand("select RH_SYS_ID.nextval from DUAL", Conn);
                    cmdGet.Transaction = _tran;
                    string maxSysID = GetValue(cmdGet);
                    cmdGet.Dispose();

                    if (!maxSysID.ToString().Trim().Equals(string.Empty))
                    {
                        RH_SYS_ID = maxSysID;
                    }
                    else { return; }

                    string[] prefixArr = new string[2];
                    prefixArr = dr["TransactionID"].ToString().Trim().Split('-');
                    string InvoiceNumberComplete = dr["TransactionID"].ToString().Trim();

                    RH_COMP_CODE = "001";
                    if (dr["TransactionTypeID"].ToString().Trim().Equals("1"))
                    {
                        RH_TXN_CODE = "PMRQ";
                    }
                    else if (dr["TransactionTypeID"].ToString().Trim().Equals("2"))
                    {
                        RH_TXN_CODE = "PMRQ";
                    }

                    RH_NO = prefixArr[1].ToString();
                    RH_DT = DateTime.Parse(dr["TransactionDate"].ToString()).ToString(DateFormat).Trim();
                    RH_DOC_SRC_LOCN_CODE = "001";
                    RH_AMD_NO = "0";
                    RH_LOCN_CODE = dr["Barcode"].ToString().Trim();
                    RH_REF_FROM = "D";
                    RH_CHARGE_CODE = "CWH";
                    RH_DEL_REQD_DT = DateTime.Parse(dr["TransactionDate"].ToString()).ToString(DateFormat).Trim();
                    RH_CHARGE_AREA = "LOCATION";
                    RH_CHARGE_AREA_NUM = "6";
                    RH_REF_FROM_NUM = "1";
                    RH_APPR_STATUS = "0";
                    RH_RESVATCL_NUM = "2";
                    RH_ANNOTATION = "INVAN";
                    RH_CR_UID = dr["EmployeeCode"].ToString().Trim();
                    RH_CR_DT = DateTime.Now.ToString(DateFormat).Trim();
                    RH_PURREQ_YN = string.Empty;
                    string CHKEmployeeID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode='" + dr[2].ToString().Trim() + "'", db_vms).Trim();
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

                    cmdGet = new OracleCommand("select RH_NO from OT_INVOICE_HEAD where INVH_NO='" + RH_NO + "' and INVH_TXN_CODE='" + RH_TXN_CODE + "'", Conn);
                    cmdGet.Transaction = _tran;
                    string ExistTransactions = GetValue(cmdGet);
                    cmdGet.Dispose();
                    if (ExistTransactions.Equals(string.Empty))
                    {
                        string updatedDate = DateTime.Now.ToString(DateFormat);
                        string insertLR = string.Format(@"insert into OT_REQ_HEAD (RH_SYS_ID,
RH_COMP_CODE
,RH_TXN_CODE
,RH_NO
,RH_DT
,RH_DOC_SRC_LOCN_CODE
,RH_AMD_NO
,RH_LOCN_CODE
,RH_REF_FROM
,RH_CHARGE_CODE
,RH_DEL_REQD_DT
,RH_CHARGE_AREA
,RH_CHARGE_AREA_NUM
,RH_REF_FROM_NUM
,RH_APPR_STATUS
,RH_RESVATCL_NUM
,RH_ANNOTATION
,RH_CR_UID
,RH_CR_DT
,RH_PURREQ_YN
,RH_PRINT_STATUS
)
VALUES
(" + RH_SYS_ID
+ " ,'" + RH_COMP_CODE
+ "' ,'" + RH_TXN_CODE
+ "' ," + RH_NO
+ " ,'" + RH_DT
+ "' ,'" + RH_DOC_SRC_LOCN_CODE
+ "' ," + RH_AMD_NO
+ " ,'" + RH_LOCN_CODE
+ "' ,'" + RH_REF_FROM
+ "' ,'" + RH_CHARGE_CODE
+ "' ,'" + RH_DEL_REQD_DT
+ "' ,'" + RH_CHARGE_AREA
+ "' ," + RH_CHARGE_AREA_NUM
+ " ," + RH_REF_FROM_NUM
+ ", " + RH_APPR_STATUS
+ " , " + RH_RESVATCL_NUM
+ " ,'" + RH_ANNOTATION
+ "' ,'" + RH_CR_UID
+ "' ,'" + RH_CR_DT
+ "','N',NULL" + ")");

                        OracleCommand cmdHDR = new OracleCommand(insertLR, Conn);
                        cmdHDR.Transaction = _tran;
                        err = ExecuteNonQuery(cmdHDR);
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
                                string RI_SYS_ID = string.Empty;
                                cmdGet = new OracleCommand("select RI_SYS_ID.NextVal from DUAL", Conn);
                                cmdGet.Transaction = _tran;
                                maxSysID = GetValue(cmdGet);
                                cmdGet.Dispose();

                                if (!maxSysID.Equals(string.Empty))
                                {
                                    RI_SYS_ID = maxSysID;
                                }
                                else { return; }


                                string RI_RH_SYS_ID = RH_SYS_ID;
                                string RI_COMP_CODE = "001";
                                string RI_LOCN_CODE = RH_LOCN_CODE;
                                string RI_ITEM_CODE = det["ItemCode"].ToString();
                                string RI_ITEM_STK_YN_NUM = "1";
                                string RI_UOM_CODE = det["UOMCode"].ToString();
                                string RI_QTY = det["TranQty"].ToString();
                                string RI_QTY_LS = "0";// det["QTYLS"].ToString();
                                string RI_QTY_BU = det["TranQty"].ToString();
                                string RI_CHARGE_AREA = "LOCATION";
                                string RI_CHARGE_AREA_NUM = "6";
                                string RI_CHARGE_CODE = "CWH";
                                string RI_ITEM_DESC = det["ItemName"].ToString();
                                string RI_MII_QTY_BU = "0";
                                string RI_EI_QTY_BU = "0";
                                string RI_EI_PEND_QTY_BU = "0";
                                string RI_PI_QTY_BU = "0";
                                string RI_SI_QTY_BU = "0";
                                string RI_SI_PEND_QTY_BU = "0";
                                string RI_GI_QTY_BU = "0";
                                string RI_PRI_QTY_BU = "0";
                                string RI_RESV_QTY_BU = "0";
                                string RI_DEL_REQD_DT = RH_DEL_REQD_DT;
                                string RI_GRADE_CODE_1 = "A";
                                string RI_GRADE_CODE_2 = "G2";
                                string RI_REQ_QTY = det["TranQty"].ToString();
                                string RI_REQ_QTY_LS = "0";
                                string RI_REQ_QTY_BU = det["TranQty"].ToString();
                                string RI_CR_UID = RH_CR_UID;
                                string RI_CR_DT = RH_CR_DT;

                                string insertdetailWH = string.Format(@"insert into OT_REQ_ITEM( RI_SYS_ID, 
RI_RH_SYS_ID
,RI_COMP_CODE
,RI_LOCN_CODE
,RI_ITEM_CODE
,RI_ITEM_STK_YN_NUM
,RI_UOM_CODE
,RI_QTY
,RI_QTY_LS
,RI_QTY_BU
,RI_CHARGE_AREA
,RI_CHARGE_AREA_NUM
,RI_CHARGE_CODE
,RI_ITEM_DESC
,RI_MII_QTY_BU
,RI_EI_QTY_BU
,RI_EI_PEND_QTY_BU
,RI_PI_QTY_BU
,RI_SI_QTY_BU
,RI_SI_PEND_QTY_BU
,RI_GI_QTY_BU
,RI_PRI_QTY_BU
,RI_RESV_QTY_BU
,RI_DEL_REQD_DT
,RI_GRADE_CODE_1
,RI_GRADE_CODE_2
,RI_REQ_QTY
,RI_REQ_QTY_LS
,RI_REQ_QTY_BU
,RI_CR_UID
,RI_CR_DT)
values
(" + RI_SYS_ID
    + " , " + RI_RH_SYS_ID
    + " , '" + RI_COMP_CODE
    + "' ,'" + RI_LOCN_CODE
    + "' ,'" + RI_ITEM_CODE
    + "' , " + RI_ITEM_STK_YN_NUM
    + " ,'" + RI_UOM_CODE
    + "' , " + RI_QTY
    + " , " + RI_QTY_LS
    + " , " + RI_QTY_BU
    + " , '" + RI_CHARGE_AREA
    + "' , " + RI_CHARGE_AREA_NUM
    + " ,'" + RI_CHARGE_CODE
    + "' ,'" + RI_ITEM_DESC
    + "' , " + RI_MII_QTY_BU
    + " , " + RI_EI_QTY_BU
    + " , " + RI_EI_PEND_QTY_BU
    + " , " + RI_PI_QTY_BU
    + " , " + RI_SI_QTY_BU
    + " , " + RI_SI_PEND_QTY_BU
    + " , " + RI_GI_QTY_BU
    + " , " + RI_PRI_QTY_BU
    + " , " + RI_RESV_QTY_BU
    + " , '" + RI_DEL_REQD_DT
    + "' , '" + RI_GRADE_CODE_1
    + "' , '" + RI_GRADE_CODE_2
    + "' , " + RI_REQ_QTY
    + " , " + RI_REQ_QTY_LS
    + " , " + RI_REQ_QTY_BU
    + " , '" + RI_CR_UID
    + "' , '" + RI_CR_DT + "')");

                                OracleCommand cmdDR = new OracleCommand(insertdetailWH, Conn);
                                cmdDR.Transaction = _tran;
                                err = ExecuteNonQuery(cmdDR);
                                cmdDR.Dispose();
                            }
                            #endregion
                        }
                        if (err == InCubeErrors.Success)
                        {
                            string update = string.Format("update [WarehouseTransaction] set Synchronized=1 where TransactionID='{0}'", InvoiceNumberComplete);
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
            catch (Exception ex)
            {
                if (_tran != null) _tran.Rollback();
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
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
                int org_id = 1;
                //                string updateVoided = string.Format(@"update [Transaction] set Synchronized=0 where Voided=1  AND 
                //            (TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
                //            AND TransactionDate <= '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"')");
                //                qry = new InCubeQuery(updateVoided, db_vms);
                //                err = qry.ExecuteNonQuery();
                string sp = string.Empty;
                if (Filters.EmployeeID != -1)
                {
                    sp = " AND O.EmployeeID = " + Filters.EmployeeID;
                }
                string invoices = string.Format(@"select O.OrderID,'STANDARD',O.OrderDate,C.CustomerCode as Bill,CO.CustomerCode as Ship,'AED'as Currency,
E.EmployeeCode,(case when(PL.PriceListCode) is null then 'XXX' else PL.PriceListCode end) as PriceListCode,(case(isnull(CO.CustomerTypeID,1)) when 1 
then 'CASH'else cast(PT.SimplePeriodWidth as nvarchar(30))+' DAYS' end) as Term,
'A1' as WarehouseCode,I.Origin,PTL.Description as UOM,OD.Quantity,OD.Price,O.DesiredDeliveryDate,
'Standard(Line Invoicing)' as LINE_TYPE,CO.CustomerID,CO.OutletID
from SalesOrder O 
inner join SalesOrderDetail OD on O.CustomerID=OD.CustomerID and O.OutletID=OD.OutletID and O.OrderID=OD.OrderID 
inner join Employee E on O.EmployeeID=E.EmployeeID
LEFT outer join PriceList PL on OD.UsedPriceListID=PL.PriceListID
inner join Customer C on O.CustomerID=C.CustomerID
inner join CustomerOutlet CO on O.CustomerID=CO.CustomerID and O.OutletID=CO.OutletID
inner join PaymentTerm PT on CO.PaymentTermID=PT.PaymentTermID
inner join Pack P on OD.PackID=P.PackID
inner join Item I on P.ItemID=I.ItemID
inner join PackTypeLanguage PTL on P.PackTypeID=PTL.PackTypeID and PTL.LanguageID=1
where O.OrderStatusID=1 and O.Synchronized=0 {0}
group by O.OrderID,O.OrderDate,C.CustomerCode,CO.CustomerCode,C.CustomerCode,
E.EmployeeCode,PL.PriceListCode,CO.CustomerTypeID,PT.SimplePeriodWidth ,I.Origin,PTL.Description,
OD.Quantity,OD.Price,O.DesiredDeliveryDate,O.DesiredDeliveryDate,CO.CustomerID,CO.OutletID ", sp);
                //the query above used to have date range : AND 
                //(T.TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
                //AND T.TransactionDate <= '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"')
                #region Invoice Header
                InCubeQuery invoiceQry = new InCubeQuery(invoices, db_vms);
                err = invoiceQry.Execute();
                dt = new DataTable();
                dt = invoiceQry.GetDataTable();

                if (dt.Rows.Count > 0)
                {
                    string preOrderID = "";
                    string oracleCheck = "SELECT DISTINCT ORDER_NUM FROM XXARE_SALESORDER_STG WHERE ORDER_NUM IN (";
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (preOrderID != dt.Rows[i]["OrderID"].ToString())
                        {
                            preOrderID = dt.Rows[i]["OrderID"].ToString();
                            oracleCheck += "'" + preOrderID + "',";
                        }
                    }
                    oracleCheck = oracleCheck.TrimEnd(new char[] { ',' }) + ")";

                    DataTable dtOraOrders = OLEDB_Datatable(oracleCheck);

                    string rowFilter = "";
                    if (dtOraOrders.Rows.Count > 0)
                    {
                        rowFilter = "OrderID NOT IN (";
                        for (int i = 0; i < dtOraOrders.Rows.Count; i++)
                        {
                            rowFilter += "'" + dtOraOrders.Rows[i]["ORDER_NUM"].ToString() + "',";
                            string update = string.Format("update [SalesOrder] set Synchronized=1,OrderStatusID=11 where OrderID='{0}'", dtOraOrders.Rows[i]["ORDER_NUM"].ToString());
                            invoiceQry = new InCubeQuery(update, db_vms);
                            err = invoiceQry.ExecuteNonQuery();
                        }
                        rowFilter = rowFilter.TrimEnd(new char[] { ',' }) + ")";
                        dt.DefaultView.RowFilter = rowFilter;
                        dt = dt.DefaultView.ToTable();
                    }
                }

                string tranDetail = string.Empty;
                string insertDetail = string.Empty;
                int TOTALINSERTED = 0;
                ClearProgress();
                SetProgressMax(dt.Rows.Count);

                foreach (DataRow dr in dt.Rows)
                {
                    ReportProgress();
                    if (Conn.State != ConnectionState.Open)
                        Conn.Open();
                    _tran = Conn.BeginTransaction();
                    string ORDER_NUM = dr["OrderID"].ToString().Trim();
                    string ORDER_TYPE = "STANDARD";
                    string ORDER_DATE = dr["OrderDate"].ToString().Trim();
                    string CUSTOMER_NO = dr["Bill"].ToString().Trim();
                    string SHIP_TO = dr["Ship"].ToString().Trim();
                    string BILL_TO = dr["Bill"].ToString().Trim();
                    string CURRENCY = "AED";
                    string SALESREP_NUM = dr["EmployeeCode"].ToString().Trim();
                    string PRICE_LIST = dr["PriceListCode"].ToString().Trim();
                    string PAYMENT_TERMS = dr["Term"].ToString().Trim();
                    string WAREHOUSE = "A1";
                    string ITEM_CODE = dr["Origin"].ToString().Trim();
                    string UOM = dr["UOM"].ToString().Trim();
                    string QTY = dr["Quantity"].ToString().Trim();
                    string PRICE_UNIT = dr["Price"].ToString().Trim();
                    string REQUEST_DATE = dr["DesiredDeliveryDate"].ToString().Trim();
                    string SCHEDULED_SHIP_DATE = dr["DesiredDeliveryDate"].ToString().Trim();
                    string LINE_TYPE = "Standard(Line Invoicing)";
                    string VERIFY_FLAG = "N";
                    if (PRICE_LIST.Equals("XXX"))
                    {
                        string CustomerID = dr["CustomerID"].ToString().Trim();
                        string OutletID = dr["OutletID"].ToString().Trim();
                        PRICE_LIST = GetFieldValue("PriceList", "Top(1) PriceListCode", "PriceListID in (select Top(1) PriceListID from GroupPrice where GroupID IN (SELECT GroupID FROM CustomerOutletGroup WHERE CustomerID=" + CustomerID + " and OutletID =" + OutletID + "))", db_vms).Trim();
                        if (PRICE_LIST.Equals(string.Empty)) PRICE_LIST = "68484";
                    }

                    string insert = string.Format(@"insert into XXARE_SALESORDER_STG
(ORDER_NUM
,ORDER_TYPE
,ORDER_DATE
,CUSTOMER_NO
,SHIP_TO
,BILL_TO
,CURRENCY
,SALESREP_NUM
,PRICE_LIST
,PAYMENT_TERMS
,WAREHOUSE
,ITEM_CODE
,UOM
,QTY
,PRICE_UNIT
,REQUEST_DATE
,SCHEDULED_SHIP_DATE
,LINE_TYPE
,VERIFY_FLAG
,ORG_ID)
Values
(
'" + ORDER_NUM
+ "' ,'" + ORDER_TYPE
+ "' ,'" + DateTime.Parse(ORDER_DATE).ToString(DateFormat)
+ "' ,'" + CUSTOMER_NO
+ "' ,'" + SHIP_TO
+ "' ,'" + BILL_TO
+ "' ,'" + CURRENCY
+ "' ,'" + SALESREP_NUM
+ "' ,'" + PRICE_LIST
+ "' ,'" + PAYMENT_TERMS
+ "' ,'" + WAREHOUSE
+ "' ,'" + ITEM_CODE
+ "' ,'" + UOM
+ "' ," + QTY
+ " ," + PRICE_UNIT
+ " ,'" + DateTime.Parse(REQUEST_DATE).ToString(DateFormat)
+ "' ,'" + DateTime.Parse(SCHEDULED_SHIP_DATE).ToString(DateFormat)
+ "' ,'" + LINE_TYPE
+ "' ,'" + VERIFY_FLAG
+ "' ,'" + org_id + "')"
);

                    cmdHDR = new OracleCommand(insert, Conn);
                    cmdHDR.Transaction = _tran;
                    err = ExecuteNonQuery(cmdHDR, ORDER_NUM, "ORDER");
                    cmdHDR.Dispose();

                    if (err == InCubeErrors.Success)
                    {
                        string update = string.Format("update [SalesOrder] set Synchronized=1,OrderStatusID=11 where OrderID='{0}'", ORDER_NUM);
                        invoiceQry = new InCubeQuery(update, db_vms);
                        err = invoiceQry.ExecuteNonQuery();
                        WriteMessage("\r\n" + ORDER_NUM + " - Transaction sent Successfully");
                        TOTALINSERTED++;
                        _tran.Commit();
                    }
                    else
                    {
                        WriteMessage("\r\n" + ORDER_NUM + " - Transaction Failed");
                        _tran.Rollback();
                    }
                }
                WriteMessage("\r\n");
                //WriteMessage("<<< Transactions >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
                //THIS CALLS THE STORED PROCEDURE THAT FIXES THE DOCUMENT SEQUENCE .
                //err = DatabaseSpecialFunctions.RunStoredProcedure(db_vms, "spHandleDocumentSequence");

            }
            catch (Exception ex)
            {
                if (_tran != null) _tran.Rollback();
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                if (_tran != null) _tran.Dispose();
            }
        }
        #endregion

        #endregion

        #endregion

        #region Update OutStanding
        public override void OutStanding()
        {
            try
            {
                #region(Declerations)

                DataTable dtTransactions = new DataTable();
                string QueryString = "", CustomerCode = "", CustomerID = "", OutletID = "", AccountID = "", RouteCode = "", RouteID = "", TransactionType = "", TransactionID = "", TransactionTypeID = "", EmployeeID = "", WarehouseID = "";
                decimal TotalAmount = 0, RemainingAmount = 0;
                long SRNO = 0;
                DateTime Transaction_Date = DateTime.MinValue;
                int TOTALUPDATED = 0, TOTALINSERTED = 0;

                #endregion

                ClearProgress();

                QueryString = string.Format(@"SELECT SRNO,CUSTOMER_CODE,TRANSACTION_TYPE,TRANSACTION_ID,SALESMAN_CODE,TRANSACTION_AMOUNT,REMAINING_AMOUNT,TRANSACTION_DATE 
                                              FROM XXARE_UPDATETRANS_OUT_STG WHERE SRNO IN 
                                              (SELECT Max(SRNO) FROM XXARE_UPDATETRANS_OUT_STG WHERE PROCESS_FLAG = 'N' GROUP BY CUSTOMER_CODE,TRANSACTION_TYPE,TRANSACTION_ID)");
                dtTransactions = GetDataTable(QueryString);

                SetProgressMax(dtTransactions.Rows.Count);
                incubeQuery = new InCubeQuery(db_vms, "UPDATE Int_ActionTrigger SET TotalRows = " + dtTransactions.Rows.Count + " WHERE ID = " + TriggerID);
                incubeQuery.ExecuteNonQuery();

                foreach (DataRow dr in dtTransactions.Rows)
                {
                    try
                    {
                        ReportProgress("Updating Outstandings");

                        SRNO = long.Parse(dr["SRNO"].ToString().Trim());
                        CustomerCode = dr["CUSTOMER_CODE"].ToString().Trim();
                        TransactionID = dr["TRANSACTION_ID"].ToString();
                        TransactionType = dr["TRANSACTION_TYPE"].ToString();
                        TotalAmount = Math.Abs(decimal.Parse(dr["Transaction_Amount"].ToString().Trim()));
                        RemainingAmount = Math.Abs(decimal.Parse(dr["Remaining_Amount"].ToString().Trim()));
                        RouteCode = dr["Salesman_Code"].ToString().Trim();
                        Transaction_Date = DateTime.Parse(dr["TRANSACTION_DATE"].ToString().Trim());

                        CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode = '" + CustomerCode + "'", db_vms);
                        if (CustomerID != string.Empty)
                            OutletID = GetFieldValue("CustomerOutlet", "OutletID", " CustomerID = " + CustomerID + " AND CustomerCode = '" + CustomerCode + "'", db_vms);
                        if (CustomerID == string.Empty || OutletID == string.Empty)
                        {
                            WriteMessage(string.Format("SRNO [{0}]: Customer code [{1}] not defined\r\n", SRNO, CustomerCode));
                            UpdateFlag("XXARE_UPDATETRANS_OUT_STG", string.Format("CUSTOMER_CODE = '{0}' AND TRANSACTION_TYPE = '{1}' AND TRANSACTION_ID = '{2}'", CustomerCode, TransactionType, TransactionID));
                            RecordTransactionHistory(TransactionID, TransactionType, CustomerCode, string.Format("SRNO [{0}]: Customer code [{1}] not defined\r\n", SRNO, CustomerCode), SRNO, TotalAmount, RemainingAmount, RouteCode, Transaction_Date);
                            continue;
                        }

                        if (TransactionType.Trim().ToLower().Equals("return"))
                        {
                            TransactionTypeID = "5";
                        }
                        else
                        {
                            TransactionTypeID = "1";
                        }

                        RouteID = GetFieldValue("[Route]", "RouteID", "RouteCode = '" + RouteCode + "'", db_vms);
                        if (RouteID.Trim().Equals(string.Empty))
                        {
                            WriteMessage(string.Format("SRNO [{0}]: Route code [{1}] not defined\r\n", SRNO, RouteCode));
                            UpdateFlag("XXARE_UPDATETRANS_OUT_STG", string.Format("CUSTOMER_CODE = '{0}' AND TRANSACTION_TYPE = '{1}' AND TRANSACTION_ID = '{2}'", CustomerCode, TransactionType, TransactionID));
                            RecordTransactionHistory(TransactionID, TransactionType, CustomerCode, string.Format("SRNO [{0}]: Route code [{1}] not defined\r\n", SRNO, CustomerCode), SRNO, TotalAmount, RemainingAmount, RouteCode, Transaction_Date);
                            continue;
                        }

                        EmployeeID = GetFieldValue("EmployeeTerritory", "EmployeeID", "TerritoryID IN (SELECT TerritoryID FROM Territory WHERE TerritoryCode = '" + RouteCode + "')", db_vms);
                        if (EmployeeID.Trim().Equals(string.Empty))
                        {
                            WriteMessage(string.Format("SRNO [{0}]: No employee for Route code [{1}]\r\n", SRNO, RouteCode));
                            UpdateFlag("XXARE_UPDATETRANS_OUT_STG", string.Format("CUSTOMER_CODE = '{0}' AND TRANSACTION_TYPE = '{1}' AND TRANSACTION_ID = '{2}'", CustomerCode, TransactionType, TransactionID));
                            RecordTransactionHistory(TransactionID, TransactionType, CustomerCode, string.Format("SRNO [{0}]: No employee defined for route [{1}]\r\n", SRNO, RouteCode), SRNO, TotalAmount, RemainingAmount, RouteCode, Transaction_Date);
                            continue;
                        }

                        WarehouseID = GetFieldValue("Warehouse", "WarehouseID", "Barcode = '" + RouteCode + "'", db_vms);
                        if (WarehouseID.Trim().Equals(string.Empty))
                        {
                            WriteMessage(string.Format("SRNO [{0}]: No vehicle for Route code [{1}]\r\n", SRNO, RouteCode));
                            UpdateFlag("XXARE_UPDATETRANS_OUT_STG", string.Format("CUSTOMER_CODE = '{0}' AND TRANSACTION_TYPE = '{1}' AND TRANSACTION_ID = '{2}'", CustomerCode, TransactionType, TransactionID));
                            RecordTransactionHistory(TransactionID, TransactionType, CustomerCode, string.Format("SRNO [{0}]: No vehicle defined for employee [{1}] not defined\r\n", SRNO, RouteCode), SRNO, TotalAmount, RemainingAmount, RouteCode, Transaction_Date);
                            continue;
                        }

                        AccountID = GetFieldValue("AccountCustOut", "AccountID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                        if (WarehouseID.Trim().Equals(string.Empty))
                        {
                            WriteMessage(string.Format("SRNO [{0}]: No Account for Customer code [{1}]\r\n", SRNO, CustomerCode));
                            UpdateFlag("XXARE_UPDATETRANS_OUT_STG", string.Format("CUSTOMER_CODE = '{0}' AND TRANSACTION_TYPE = '{1}' AND TRANSACTION_ID = '{2}'", CustomerCode, TransactionType, TransactionID));
                            RecordTransactionHistory(TransactionID, TransactionType, CustomerCode, string.Format("SRNO [{0}]: No Account is defined for customer code [{1}]\r\n", SRNO, CustomerCode), SRNO, TotalAmount, RemainingAmount, RouteCode, Transaction_Date);
                            continue;
                        }

                        if (GetFieldValue("[Transaction]", "TransactionID", "TransactionID ='" + TransactionID + "' AND CustomerID = " + CustomerID + " AND TransactionTypeID = " + TransactionTypeID, db_vms) == TransactionID)
                        {
                            QueryBuilderObject.SetField("RemainingAmount", RemainingAmount.ToString());
                            QueryBuilderObject.SetField("voided", "0");
                            QueryBuilderObject.SetField("GrossTotal", TotalAmount.ToString());
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("EmployeeID", EmployeeID);
                            QueryBuilderObject.SetField("UpdatedDate", "GETDATE()");
                            QueryBuilderObject.SetField("AccountID", AccountID);
                            QueryBuilderObject.SetStringField("Notes", "ERP");
                            err = QueryBuilderObject.UpdateQueryString("[Transaction]", "TransactionID ='" + TransactionID + "' AND CustomerID = " + CustomerID + " AND TransactionTypeID = " + TransactionTypeID, db_vms);

                            if (err == InCubeErrors.Success)
                            {
                                TOTALUPDATED++;
                                UpdateFlag("XXARE_UPDATETRANS_OUT_STG", string.Format("CUSTOMER_CODE = '{0}' AND TRANSACTION_TYPE = '{1}' AND TRANSACTION_ID = '{2}'", CustomerCode, TransactionType, TransactionID));
                            }
                            RecordTransactionHistory(TransactionID, TransactionType, CustomerCode, err == InCubeErrors.Success ? "Updated Successfully" : "Update Failed", SRNO, TotalAmount, RemainingAmount, RouteCode, Transaction_Date);
                        }
                        else
                        {
                            QueryBuilderObject = new QueryBuilder();
                            QueryBuilderObject.SetField("CustomerID", CustomerID);
                            QueryBuilderObject.SetField("OutletID", OutletID);
                            QueryBuilderObject.SetStringField("TransactionID", TransactionID);
                            QueryBuilderObject.SetField("EmployeeID", EmployeeID);
                            QueryBuilderObject.SetDateField("TransactionDate", Transaction_Date);
                            QueryBuilderObject.SetField("TransactionTypeID", TransactionTypeID);
                            QueryBuilderObject.SetField("Synchronized", "1");
                            QueryBuilderObject.SetField("RemainingAmount", RemainingAmount.ToString());
                            QueryBuilderObject.SetField("GrossTotal", TotalAmount.ToString());
                            QueryBuilderObject.SetField("Voided", "0");
                            QueryBuilderObject.SetField("TransactionStatusID", "1");
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("NetTotal", TotalAmount.ToString());
                            QueryBuilderObject.SetField("Posted", "1");
                            QueryBuilderObject.SetField("CurrencyID", "1");
                            QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                            QueryBuilderObject.SetField("CreatedBy", EmployeeID);
                            QueryBuilderObject.SetField("CreatedDate", "GETDATE()");
                            QueryBuilderObject.SetField("UpdatedBy", EmployeeID);
                            QueryBuilderObject.SetField("UpdatedDate", "GETDATE()");
                            QueryBuilderObject.SetField("AccountID", AccountID);
                            QueryBuilderObject.SetField("DivisionID", "-1");
                            QueryBuilderObject.SetField("SalesMode", "2");
                            QueryBuilderObject.SetField("Discount", "0");
                            QueryBuilderObject.SetField("VisitNo", "1");
                            QueryBuilderObject.SetStringField("Notes", "ERP");
                            err = QueryBuilderObject.InsertQueryString("[Transaction]", db_vms);

                            if (err == InCubeErrors.Success)
                            {
                                TOTALINSERTED++;
                                UpdateFlag("XXARE_UPDATETRANS_OUT_STG", string.Format("CUSTOMER_CODE = '{0}' AND TRANSACTION_TYPE = '{1}' AND TRANSACTION_ID = '{2}'", CustomerCode, TransactionType, TransactionID));
                            }
                            RecordTransactionHistory(TransactionID, TransactionType, CustomerCode, err == InCubeErrors.Success ? "Inserted Successfully" : "Insert Failed", SRNO, TotalAmount, RemainingAmount, RouteCode, Transaction_Date);
                        }
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage(string.Format("SRNO [{0}]: Insert/Update transaction failed\r\n", SRNO, Transaction_Date));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        RecordTransactionHistory(TransactionID, TransactionType, CustomerCode, "Unexpected error happened: " + ex.Message, SRNO, TotalAmount, RemainingAmount, RouteCode, Transaction_Date);
                    }
                }
                WriteMessage("\r\n<<< Outstanding>>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED + " , Total Skipped = " + (dtTransactions.Rows.Count - TOTALINSERTED - TOTALUPDATED) + "\r\n");
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void RecordTransactionHistory(string TransactionID, string TransactionType, string CustomerCode, string Result, long SRNO, decimal Amount, decimal Remaining, string SalesmanCode, DateTime TransactionDate)
        {
            try
            {
                //string insertQuery = string.Format("INSERT INTO TransactionUpdateHistory ([TransactionID],[TransactionType],[CustomerCode],[Result],[UpdateDate],[SRNO],[TransactionAmount],[RemainingAmount],[SalesmanCode],[TransactionDate],[TriggerID]) VALUES ('{0}','{1}','{2}','{3}',GETDATE(),{4},{5},{6},'{7}',{8},{9})"
                //    , TransactionID, TransactionType, CustomerCode, Result, SRNO, Amount, Remaining, SalesmanCode, DatabaseDateTimeManager.ParseDateAndTimeToSQL(TransactionDate), TriggerID);
                //incubeQuery = new InCubeQuery(db_vms, insertQuery);
                //err = incubeQuery.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        #endregion

        #region GENERIC
        private InCubeErrors UpdateFlagError(string TableName, string Criteria, string Error)
        {
            try
            {
                if (Conn.State == ConnectionState.Closed) Conn.Open();
                string query = string.Format("UPDATE {0} SET PROCESS_FLAG = '{2}' WHERE {1}", TableName, Criteria, Error);
                cmdHDR = new OracleCommand(query, Conn);
                ExecuteNonQuery(cmdHDR);
                cmdHDR.Dispose();

                return InCubeErrors.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return err;
        }
        private InCubeErrors UpdateFlag(string TableName, string Criteria)
        {
            try
            {
                if (Conn.State == ConnectionState.Closed) Conn.Open();
                string query = string.Format("update {0} set PROCESS_FLAG='Y' where {1}", TableName, Criteria);
                OracleCommand cmdHDR = new OracleCommand(query, Conn);
                ExecuteNonQuery(cmdHDR);
                cmdHDR.Dispose();

                return InCubeErrors.Success;
            }
            catch
            {

            }
            return err;
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
                wrt.Write("\n" + header + "----" + DateTime.Now.ToString() + "\r\n");
                wrt.Write("\n" + description + "\r\n");
                wrt.Close();
            }
            catch
            {

            }
        }
        //public override void Close()
        //{
        //    db_vms.Close();
        //}

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
        private DataTable OLEDB_Datatable(string select)
        {
            OracleDataAdapter oledbAdapter = new OracleDataAdapter();
            DataSet ds = new DataSet();
            DataTable tbl = new DataTable();
            
            try
            {
                if (Conn.State == ConnectionState.Closed)
                {
                    Conn.Open();
                }
                oledbAdapter.SelectCommand = new OracleCommand(select, Conn);
                oledbAdapter.Fill(ds);
                oledbAdapter.Dispose();
                Conn.Close();
                tbl = ds.Tables[0];

            }
            catch (Exception ex)
            {
                WriteExceptions(ex.Message, "ORACLE ERROR", false);
            }
            return tbl;
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
        private InCubeErrors InsertLog(string transactionID, string errorDescription, string errorLocation)
        {
            InCubeErrors logError = InCubeErrors.Error;
            try
            {
                string insert = string.Format(@"insert into InCubeLog values('{0}','{1}','{2}','{3}')", transactionID, DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss"), errorDescription, errorLocation);
                InCubeQuery logQry = new InCubeQuery(insert, db_vms);
                logError = logQry.ExecuteNonQuery();

            }
            catch
            {

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
            catch
            {

            }


        }
        #endregion

        private void UpdateOrganization()
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
    }
}