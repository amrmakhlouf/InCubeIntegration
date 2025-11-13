using InCubeIntegration_DAL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Xml;

namespace InCubeIntegration_BL
{
    class IntegrationUnitra : IntegrationBase
    {
        QueryBuilder QueryBuilderObject = new QueryBuilder();
        InCubeErrors err = InCubeErrors.Error;
        private long UserID;
        string DateFormat = "yyyy/MM/dd HH:mm:ss";
        InCubeQuery qry;
        InCubeDatabase dbERP;

        public IntegrationUnitra(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            string _dataSourceFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase) + "\\DataSources.xml";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_dataSourceFilePath);
            string strConnectionString = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'InCubeSQL']/Data").InnerText;
            dbERP = new InCubeDatabase();
            InCubeErrors err = dbERP.Open("InCubeSQL", "IntegrationUnitra");
            UserID = CurrentUserID;
        }

        public override void UpdateItem()
        {
            try
            {

                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;

                object field = new object();

                string PacktypeID = string.Empty;

                DataTable DT = new DataTable();
                qry = new InCubeQuery("SELECT * FROM ItemMasterView where CHANGEFLG='Y'", dbERP);
                err = qry.Execute();
                DT = qry.GetDataTable();
                //QueryBuilderObject.SetField("InActive", "1");
                //QueryBuilderObject.UpdateQueryString("Item", db_vms);
                ClearProgress();
                SetProgressMax(DT.Rows.Count);

                foreach (DataRow row in DT.Rows)
                {
                    ReportProgress("Updating Items");
                    ;

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

                    string status = row["Satus"].ToString().Trim();
                    if (status == "INACTIVE") { status = "1"; } else { status = "0"; }
                    string ItemCode = row["ItCode"].ToString().Trim();
                    string itemDescriptionEnglish = row["ItName"].ToString().Trim();
                    string itemDescriptionArabic = row["ItName"].ToString().Trim();
                    string DivisionCode = row["ComapnyID"].ToString().Trim();
                    string SAPDivision = row["SAPDivision"].ToString().Trim();
                    string DivisionID = GetFieldValue("SAPDivisions", "DivisionID", "CompanyCode = '" + DivisionCode + "' AND SAPDivision = '" + SAPDivision + "'", db_vms);
                    string DivisionNameEnglish = row["CompanyName"].ToString().Trim();
                    //if (DivisionCode.Equals("2")) { itemDescriptionEnglish = "METS " + itemDescriptionEnglish; itemDescriptionArabic = "METS " + itemDescriptionArabic; }
                    string CategoryCode = DivisionCode + SAPDivision + row["ItCategoryCode"].ToString().Trim();
                    string CategoryNameEnglish = row["ItCategory"].ToString().Trim();
                    string Brand = row["ItBrand"].ToString().Trim();
                    string Orgin = row["ItBrandCode"].ToString().Trim();// row["Origin"].ToString().Trim();
                    string TCAllowed = row["TCAllowed"].ToString().Trim();
                    if (TCAllowed == "Y") { TCAllowed = "1"; } else { TCAllowed = "0"; }
                    string PackDescriptionEnglish = row["ITUNIT"].ToString().Trim();
                    string packQty = row["ItFactor"].ToString().Trim();
                    //Tax will be provided 0 or integer number, exempted will be handled in furture
                    decimal tax = decimal.Parse(row["VatPercentage"].ToString());

                    try
                    {
                        decimal x = 0;
                        x = decimal.Parse(packQty);

                        if (x > 1)
                        {
                            //PackDescriptionEnglish = "CASE"+x.ToString();
                        }
                        else
                        {
                            //PackDescriptionEnglish = "PCS";
                        }
                        PacktypeID = row["PackID"].ToString().Trim();
                    }
                    catch
                    {

                    }

                    string barcode = row["ItemBarcode"].ToString().Trim();
                    string PackGroup = row["ItGroup"].ToString().Trim();
                    string PackGroupCode = row["ItGroupCode"].ToString().Trim();
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

                    //#region ItemDivision

                    //string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode = '" + DivisionCode + "'", db_vms);

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
                    //else
                    //{

                    //    QueryBuilderObject.SetField("Description", "'" + DivisionNameEnglish + "'");
                    //    err = QueryBuilderObject.UpdateQueryString("DivisionLanguage", "LanguageID=1 and DivisionID=" + DivisionID, db_vms);

                    //    QueryBuilderObject.SetField("Description", "'" + DivisionNameEnglish + "'");
                    //    err = QueryBuilderObject.UpdateQueryString("DivisionLanguage", "LanguageID=2 and DivisionID=" + DivisionID, db_vms);
                    //}

                    //#endregion

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

                    string ItemID = "";

                    ItemID = GetFieldValue("Item", "ItemID", "ItemCode='" + ItemCode + "' and PackDefinition='" + DivisionCode + SAPDivision + "'", db_vms);
                    if (ItemID == string.Empty)
                    {
                        ItemID = GetFieldValue("Item", "isnull(MAX(ItemID),0) + 1", db_vms);
                    }

                    #region Item

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
                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());
                        err = QueryBuilderObject.UpdateQueryString("Item", " ItemID = " + ItemID, db_vms);

                    }
                    else // New Item --- Insert Query
                    {
                        TOTALINSERTED++;

                        QueryBuilderObject.SetField("ItemID", ItemID);
                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());
                        QueryBuilderObject.SetField("ItemCode", "'" + ItemCode + "'");
                        QueryBuilderObject.SetField("InActive", status);
                        QueryBuilderObject.SetField("PackDefinition", "'" + DivisionCode + SAPDivision + "'");
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
                        QueryBuilderObject.SetField("Quantity", packQty);
                        QueryBuilderObject.SetField("Barcode", "'" + barcode + "'");
                        QueryBuilderObject.SetField("SerialSeparator", "'" + ItemCode.Substring(0, Math.Min(10, ItemCode.Length)) + "'");
                        QueryBuilderObject.SetField("Weight", tax.ToString());
                        err = QueryBuilderObject.UpdateQueryString("Pack", "PackID = " + PackID, db_vms);
                    }
                    else
                    {
                        PackID = int.Parse(GetFieldValue("Pack", "ISNULL(MAX(PackID),0) + 1", db_vms));

                        QueryBuilderObject.SetField("PackID", PackID.ToString());
                        QueryBuilderObject.SetField("Barcode", "'" + barcode + "'");
                        QueryBuilderObject.SetField("SerialSeparator", "'" + ItemCode.Substring(0, Math.Min(10, ItemCode.Length)) + "'");
                        QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                        QueryBuilderObject.SetField("PackTypeID", PacktypeID);
                        QueryBuilderObject.SetField("Quantity", packQty);
                        QueryBuilderObject.SetField("EquivalencyFactor", "0");
                        QueryBuilderObject.SetField("Weight", tax.ToString());
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
                        err = UpdateFlag("SAP_ItemMaster_Table", "ITCODE='" + ItemCode + "' and ITUNIT='" + PackDescriptionEnglish + "'");
                    }
                }

                DT.Dispose();

                qry = new InCubeQuery(db_vms, @"UPDATE PD SET Tax = ISNULL(P.Weight,0)
FROM PriceDefinition PD
INNER JOIN PriceList PL ON PL.PriceListID = PD.PriceListID
INNER JOIN Pack P ON P.PackID = PD.PackID
WHERE PL.PriceListTypeID = 1");
                qry.ExecuteNonQuery();

                WriteMessage("\r\n");
                WriteMessage("<<< ITEMS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
            }
            catch (Exception ex)
            {
                WriteExceptions("Error = " + ex.Message, "Items", false);
            }
        }
        public override void UpdateCustomer()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            object field = new object();

            DataTable DT = new DataTable();
            qry = new InCubeQuery("SELECT * FROM CustomerMasterView", dbERP);
            err = qry.Execute();
            DT = qry.GetDataTable();

            ClearProgress();
            SetProgressMax(DT.Rows.Count);

            foreach (DataRow row in DT.Rows)
            {
                try
                {
                    ReportProgress("Updating Customers");

                    //Customer Code	  0
                    //Customer Barcode	 1
                    //English Description  2	
                    //Arabic Description   3
                    //Phone	 4
                    //Fax	 5
                    //Email	 6
                    //Main Address English 7
                    //Main Address Arabic 8
                    //Taxable	9
                    //Group description  10	
                    //Is Credit	11
                    //Credit limit  12	
                    //Balance  13	
                    //Payment Terms  14
                    //On Hold	15
                    //GPS longitude  16
                    //GPS latitude	17
                    //Master Customer Code  18
                    #region Variables
                    string CustomerCode = row["CustomerGroupCode"].ToString().Trim();
                    string CustomerName = row["CustomerName2"].ToString().Trim();
                    string outletBarCode = row["CustomerCode"].ToString().Trim();
                    string outletCode = row["VISAAC"].ToString().Trim();
                    string CustomerOutletDescriptionEnglish = row["CustomerName"].ToString().Trim();
                    string CustomerOutletDescriptionArabic = row["CustomerName"].ToString().Trim();


                    CustomerCode = outletCode;
                    if (CustomerName.Equals(string.Empty))
                        CustomerName = CustomerOutletDescriptionEnglish;
                    string Phonenumber = row["Telephone"].ToString().Trim();
                    string Faxnumber = row["Mobile"].ToString().Trim();
                    string Email = row["Email"].ToString().Trim();
                    string CustomerAddressEnglish = row["Address"].ToString().Trim();
                    string CustomerAddressArabic = row["Address"].ToString().Trim();
                    string Taxable = row["TaxClassification"].ToString().Trim();
                    string TRNO = row["VATRegNo"].ToString().Trim();
                    string channelDescription = row["Channel"].ToString().Trim();
                    string ChannelCode = row["ChannelCode"].ToString().Trim();
                    string CustomerNewGroup = string.Empty;
                    if (CustomerName.Trim().Replace(" ", "").ToLower() != "nogroup")
                    {
                        CustomerNewGroup = row["CustomerGroup"].ToString().Trim();
                    }
                    string custommgroup1Code = row["CustomGroup1"].ToString().Trim();
                    string custommgroup1Name = row["CustomGroup1Name"].ToString().Trim();
                    string custommgroup2Code = row["CustomGroup2"].ToString().Trim();
                    string custommgroup2Name = row["CustomGroup2Name"].ToString().Trim();
                    string CustomerOutletGroupCode = row["CustomerGroupCode"].ToString().Trim();
                    string CustomerOutletGroup = row["CustomerGroup"].ToString().Trim();
                    string CustomerGroupDescription = row["Channel"].ToString().Trim();
                    //string IsCreditCustomer = row["CustomerType"].ToString().Trim();
                    string CreditLimit = row["CreditLimit"].ToString().Trim();
                    if (CreditLimit.Equals(string.Empty)) CreditLimit = "1000";
                    string Balance = "0";// row[13].ToString().Trim();
                    string Paymentterms = row["CreditDays"].ToString().Trim();
                    string OnHold = "0";// row["OnHold"].ToString().Trim();
                    string CustomerType = row["CustomerPeymentTerms"].ToString().Trim();
                    string inActive = row["Status"].ToString().Trim();
                    if (inActive.ToLower().Equals("active"))
                    {
                        inActive = "0";
                    }
                    else
                    {
                        inActive = "1";
                    }
                    string companyCode = row["COMPANYID"].ToString().Trim();
                    string SAPDivision = row["SAPDivision"].ToString().Trim();
                    string companyID = GetFieldValue("SAPDivisions", "DivisionID", "CompanyCode = '" + companyCode + "' AND SAPDivision = '" + SAPDivision + "'", db_vms);

                    if (CustomerType == "CREDIT")
                    {
                        CustomerType = "2";
                    }
                    else if (CustomerType == "CASH")
                    {
                        CustomerType = "1";
                    }
                    else
                    {
                        CustomerType = "3";
                    }
                    List<DaySequence> daySequence = new List<DaySequence>();
                    DaySequence ds = new DaySequence();



                    string B2B_Invoices = "50";// row["NumberOfInvoices"].ToString().Trim();
                    string GPSlongitude = "0";// row[16].ToString().Trim();
                    string GPSlatitude = "0";// row[17].ToString().Trim();


                    string SupervisorCode = row["SupervisorCode"].ToString().Trim();
                    string Supervisor = row["Supervisor"].ToString().Trim();
                    string RoutemanagerCode = row["RouteManagerCode"].ToString().Trim();
                    string Routemanager = row["RouteManager"].ToString().Trim();
                    string RouteCode = row["Route"].ToString().Trim();
                    string SalesManCode = row["SalesManCode"].ToString().Trim();
                    string SalesMan = row["SalesMan"].ToString().Trim();
                    Dictionary<string, string> empDic = new Dictionary<string, string>();
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

                    if (CustomerCode == string.Empty)
                        continue;

                    object CustomerID = "0";

                    string ExistCustomer = "";
                    #endregion

                    string ChannelID = "";
                    ChannelID = GetFieldValue("Channel", "ChannelID", "ChannelCode = '" + ChannelCode + "'", db_vms).Trim();
                    if (ChannelID.Equals(string.Empty))
                    {
                        ChannelID = GetFieldValue("Channel", "isnull(MAX(ChannelID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("ChannelID", ChannelID);
                        QueryBuilderObject.SetField("ChannelCode", "'" + ChannelCode + "'");
                        QueryBuilderObject.InsertQueryString("Channel", db_vms);

                        QueryBuilderObject.SetField("ChannelID", ChannelID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + channelDescription + "'");
                        QueryBuilderObject.InsertQueryString("ChannelLanguage", db_vms);

                    }
                    #region Get Customer Group
                    string GroupID2 = string.Empty;
                    string GroupID3 = string.Empty;
                    string GroupID4 = string.Empty;
                    string GroupID = GetFieldValue("CustomerGroupLanguage", "GroupID", " Description = '" + channelDescription.Trim() + "'  AND LanguageID = 1", db_vms);

                    if (GroupID == string.Empty)
                    {
                        GroupID = GetFieldValue("CustomerGroup", "isnull(MAX(GroupID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                        QueryBuilderObject.SetField("GroupCode", "'" + ChannelCode + "'");
                        QueryBuilderObject.SetField("ChannelID", ChannelID);
                        err = QueryBuilderObject.InsertQueryString("CustomerGroup", db_vms);

                        QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + channelDescription + "'");
                        err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);

                        QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "N'" + channelDescription + "'");
                        err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);
                    }
                    else
                    {
                        QueryBuilderObject.SetField("ChannelID", ChannelID);
                        err = QueryBuilderObject.UpdateQueryString("CustomerGroup", "GroupID=" + GroupID.ToString(), db_vms);
                    }

                    if (!custommgroup1Code.Trim().Equals(string.Empty))
                    {
                        GroupID2 = GetFieldValue("CustomerGroupLanguage", "GroupID", " Description = '" + custommgroup1Name.Trim() + "'  AND LanguageID = 1", db_vms);

                        if (GroupID2 == string.Empty)
                        {
                            GroupID2 = GetFieldValue("CustomerGroup", "isnull(MAX(GroupID),0) + 1", db_vms);

                            QueryBuilderObject.SetField("GroupID", GroupID2.ToString());
                            QueryBuilderObject.SetField("GroupCode", "'" + custommgroup1Code + "'");
                            QueryBuilderObject.SetField("ChannelID", ChannelID);
                            err = QueryBuilderObject.InsertQueryString("CustomerGroup", db_vms);

                            QueryBuilderObject.SetField("GroupID", GroupID2.ToString());
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + custommgroup1Name + "'");
                            err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);

                            QueryBuilderObject.SetField("GroupID", GroupID2.ToString());
                            QueryBuilderObject.SetField("LanguageID", "2");
                            QueryBuilderObject.SetField("Description", "N'" + custommgroup1Name + "'");
                            err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);
                        }
                        else
                        {
                            QueryBuilderObject.SetField("ChannelID", ChannelID);
                            err = QueryBuilderObject.UpdateQueryString("CustomerGroup", "GroupID=" + GroupID2.ToString(), db_vms);
                        }
                    }
                    if (!custommgroup2Code.Trim().Equals(string.Empty))
                    {
                        GroupID3 = GetFieldValue("CustomerGroupLanguage", "GroupID", " Description = '" + custommgroup2Name.Trim() + "'  AND LanguageID = 1", db_vms);

                        if (GroupID3 == string.Empty)
                        {
                            GroupID3 = GetFieldValue("CustomerGroup", "isnull(MAX(GroupID),0) + 1", db_vms);

                            QueryBuilderObject.SetField("GroupID", GroupID3.ToString());
                            QueryBuilderObject.SetField("GroupCode", "'" + custommgroup2Code + "'");
                            QueryBuilderObject.SetField("ChannelID", ChannelID);
                            err = QueryBuilderObject.InsertQueryString("CustomerGroup", db_vms);

                            QueryBuilderObject.SetField("GroupID", GroupID3.ToString());
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + custommgroup2Name + "'");
                            err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);

                            QueryBuilderObject.SetField("GroupID", GroupID3.ToString());
                            QueryBuilderObject.SetField("LanguageID", "2");
                            QueryBuilderObject.SetField("Description", "N'" + custommgroup2Name + "'");
                            err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);
                        }
                        else
                        {
                            QueryBuilderObject.SetField("ChannelID", ChannelID);
                            err = QueryBuilderObject.UpdateQueryString("CustomerGroup", "GroupID=" + GroupID3.ToString(), db_vms);
                        }
                    }
                    if (!CustomerOutletGroupCode.Trim().Equals(string.Empty))
                    {
                        GroupID4 = GetFieldValue("CustomerGroupLanguage", "GroupID", " Description = '" + CustomerOutletGroup.Trim() + "'  AND LanguageID = 1", db_vms);

                        if (GroupID4 == string.Empty)
                        {
                            GroupID4 = GetFieldValue("CustomerGroup", "isnull(MAX(GroupID),0) + 1", db_vms);

                            QueryBuilderObject.SetField("GroupID", GroupID4.ToString());
                            QueryBuilderObject.SetField("GroupCode", "'" + CustomerOutletGroupCode + "'");
                            QueryBuilderObject.SetField("ChannelID", ChannelID);
                            err = QueryBuilderObject.InsertQueryString("CustomerGroup", db_vms);

                            QueryBuilderObject.SetField("GroupID", GroupID4.ToString());
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + CustomerOutletGroup + "'");
                            err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);

                            QueryBuilderObject.SetField("GroupID", GroupID4.ToString());
                            QueryBuilderObject.SetField("LanguageID", "2");
                            QueryBuilderObject.SetField("Description", "N'" + CustomerOutletGroup + "'");
                            err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);
                        }
                        else
                        {
                            QueryBuilderObject.SetField("ChannelID", ChannelID);
                            err = QueryBuilderObject.UpdateQueryString("CustomerGroup", "GroupID=" + GroupID4.ToString(), db_vms);
                        }
                    }
                    #endregion




                    #region Customer
                    qry = new InCubeQuery(db_vms, string.Format(@"SELECT TOP 1 CustomerID FROM (
SELECT 1 RNK, CO.CustomerID FROM CustomerOutlet CO INNER JOIN CustomerOutletDivision COD ON COD.CustomerID = CO.CustomerID AND COD.OutletID = CO.OutletID WHERE CO.CustomerCode = '{0}' AND COD.DivisionID = {1}
UNION
SELECT 2 RNK, CO.CustomerID FROM CustomerOutlet CO WHERE CO.CustomerCode = '{2}'
) T ORDER BY T.RNK", outletBarCode, companyID, CustomerCode));
                    if (qry.ExecuteScalar(ref CustomerID) != InCubeErrors.Success)
                        continue;

                    if (CustomerID == null || CustomerID == DBNull.Value || string.IsNullOrEmpty(CustomerID.ToString()))
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

                    CreateCustomerOutlet(AccountID.ToString(), daySequence, supervisorID, GroupID, GroupID2, B2B_Invoices, CustomerType, outletCode, channelDescription, Paymentterms, CustomerOutletDescriptionEnglish, CustomerAddressEnglish, CustomerOutletDescriptionArabic, CustomerAddressArabic, Phonenumber, Faxnumber, OnHold, Taxable, TRNO, CustomerCode, CreditLimit, Balance, GPSlongitude, GPSlatitude, outletBarCode, Email, companyID, inActive, GroupID3, GroupID4);
                }
                catch
                {
                    WriteMessage("\r\n");
                    WriteMessage("customer failed ");

                }
            }

            qry = new InCubeQuery(db_vms, "sp_SupervisorsTerritories");
            qry.ExecuteStoredProcedure();
            DT.Dispose();

            WriteMessage("\r\n");
            WriteMessage("<<< CUSTOMERS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }
        private void CreateCustomerOutlet(string parentAccount, List<DaySequence> daySequenc, string supervisorID, string GroupID, string GroupID2, string B2B_Inv, string CustType, string CustomerCode, string channelDescription, string Paymentterms, string CustomerDescriptionEnglish, string CustomerAddressEnglish, string CustomerDescriptionArabic, string CustomerAddressArabic, string Phonenumber, string Faxnumber, string OnHold, string Taxable, string TRNO, string HeadOfficeCode, string CreditLimit, string Balance, string Longitude, string latitude, string CustomerBarCode, string email, string divisionID, string inactive, string GroupID3, string GroupID4)
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
                    QueryBuilderObject.InsertQueryString("PaymentTerm", db_vms);

                    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'Every " + Paymentterms + " Days'");
                    QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms);

                    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                    QueryBuilderObject.SetField("LanguageID", "2");
                    QueryBuilderObject.SetField("Description", "'Every " + Paymentterms + " Days'");
                    err = QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms);
                }
            }

            #region Customer Outlet and language

            string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerID = " + CustomerID + " AND Barcode = '" + CustomerBarCode + "'", db_vms);
            if (!OutletID.Trim().Equals(string.Empty))
            {
                QueryBuilderObject.SetField("Phone", "'" + Phonenumber + "'");
                QueryBuilderObject.SetField("Fax", "'" + Faxnumber + "'");
                QueryBuilderObject.SetField("Email", "'" + email + "'");
                QueryBuilderObject.SetField("Taxeable", Taxable);
                QueryBuilderObject.SetField("TaxNumber", "'" + TRNO + "'");
                QueryBuilderObject.SetField("CustomerTypeID", CustType); //HardCoded -1- Cash -2- Credit
                QueryBuilderObject.SetField("OnHold", OnHold);
                QueryBuilderObject.SetField("Inactive", inactive);
                QueryBuilderObject.SetField("BillsOpenNumber", B2B_Inv);
                QueryBuilderObject.SetField("CustomerCode", "'" + CustomerCode + "'");
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
                QueryBuilderObject.SetField("TaxNumber", "'" + TRNO + "'");
                QueryBuilderObject.SetField("BillsOpenNumber", B2B_Inv);
                QueryBuilderObject.SetField("CustomerTypeID", CustType);
                QueryBuilderObject.SetField("CurrencyID", "1");
                QueryBuilderObject.SetField("OnHold", OnHold);
                QueryBuilderObject.SetField("GPSLatitude", Longitude);
                QueryBuilderObject.SetField("GPSLongitude", latitude);
                QueryBuilderObject.SetField("StreetAddress", "0");
                QueryBuilderObject.SetField("Inactive", inactive);
                QueryBuilderObject.SetField("Notes", "'0'");
                QueryBuilderObject.SetField("SkipCreditCheck", "0");
                QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                QueryBuilderObject.SetField("OrganizationID", "1");
                QueryBuilderObject.SetField("IsKeyAccount", "0");
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
            if (!GroupID3.Trim().Equals(string.Empty))
            {
                err = ExistObject("CustomerOutletGroup", "GroupID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " and GroupID=" + GroupID3.Trim() + "", db_vms);
                if (err != InCubeErrors.Success)
                {
                    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                    QueryBuilderObject.SetField("OutletID", OutletID);
                    QueryBuilderObject.SetField("GroupID", GroupID3.ToString());
                    err = QueryBuilderObject.InsertQueryString("CustomerOutletGroup", db_vms);
                }
            }
            if (!GroupID4.Trim().Equals(string.Empty))
            {
                err = ExistObject("CustomerOutletGroup", "GroupID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " and GroupID=" + GroupID4.Trim() + "", db_vms);
                if (err != InCubeErrors.Success)
                {
                    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                    QueryBuilderObject.SetField("OutletID", OutletID);
                    QueryBuilderObject.SetField("GroupID", GroupID4.ToString());
                    err = QueryBuilderObject.InsertQueryString("CustomerOutletGroup", db_vms);
                }
            }
            //else if (err == InCubeErrors.Success)
            //{

            //    QueryBuilderObject.SetField("GroupID", GroupID.ToString());
            //    QueryBuilderObject.UpdateQueryString("CustomerOutletGroup","CustomerID="+CustomerID.ToString()+" and OutletID="+OutletID+"", db_vms);
            //}
            string CustOutDiv = GetFieldValue("CustomerOutletDivision", "customerID", "customerID=" + CustomerID.ToString() + " and OutletID=" + OutletID + " and DivisionID=" + divisionID + "", db_vms).Trim();
            if (CustOutDiv.Equals(string.Empty))
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID.ToString());
                QueryBuilderObject.SetField("DivisionID", divisionID.ToString().Trim());
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
            ExistCustomer = GetFieldValue("AccountCustOutDiv", "AccountID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " and DivisionID=" + divisionID + "", db_vms);
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
            //if (err == InCubeErrors.Success)
            //{
            //    err = UpdateFlag("accmst", "ConvCode='" + CustomerCode + "'");
            //}

            #endregion


            #endregion
        }
        public override void UpdateNewCustomer()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            object field = new object();

            DataTable DT = new DataTable();
            qry = new InCubeQuery("select * from CustomerMasterView vw inner join NewCustomer c on vw.customercode=c.customercode", dbERP);
            err = qry.Execute();
            DT = qry.GetDataTable();

            ClearProgress();
            SetProgressMax(DT.Rows.Count);

            foreach (DataRow row in DT.Rows)
            {
                try
                {
                    ReportProgress("Updating Customers");

                    //Customer Code	  0
                    //Customer Barcode	 1
                    //English Description  2	
                    //Arabic Description   3
                    //Phone	 4
                    //Fax	 5
                    //Email	 6
                    //Main Address English 7
                    //Main Address Arabic 8
                    //Taxable	9
                    //Group description  10	
                    //Is Credit	11
                    //Credit limit  12	
                    //Balance  13	
                    //Payment Terms  14
                    //On Hold	15
                    //GPS longitude  16
                    //GPS latitude	17
                    //Master Customer Code  18
                    #region Variables
                    string CustomerCode = row["CustomerCode"].ToString().Trim();
                    string CustomerName = row["CustomerGroup"].ToString().Trim();
                    string outletBarCode = row["CustomerCode"].ToString().Trim();
                    string outletCode = row["VISAAC"].ToString().Trim();
                    string CustomerOutletDescriptionEnglish = row["CustomerName"].ToString().Trim();
                    string CustomerOutletDescriptionArabic = row["CustomerName"].ToString().Trim();


                    //CustomerCode = outletCode;
                    CustomerName = CustomerOutletDescriptionEnglish;
                    string Phonenumber = row["Telephone"].ToString().Trim();
                    string Faxnumber = row["Mobile"].ToString().Trim();
                    string Email = row["Email"].ToString().Trim();
                    string CustomerAddressEnglish = row["Address"].ToString().Trim();
                    string CustomerAddressArabic = row["Address"].ToString().Trim();
                    string Taxable = "0"; //row[9].ToString().Trim();
                    string channelDescription = row["Channel"].ToString().Trim();
                    string ChannelCode = row["ChannelCode"].ToString().Trim();
                    string CustomerNewGroup = string.Empty;
                    if (CustomerName.Trim().Replace(" ", "").ToLower() != "nogroup")
                    {
                        CustomerNewGroup = row["CustomerGroup"].ToString().Trim();
                    }

                    string CustomerGroupDescription = row["Channel"].ToString().Trim();
                    //string IsCreditCustomer = row["CustomerType"].ToString().Trim();
                    string CreditLimit = row["CreditLimit"].ToString().Trim();
                    if (CreditLimit.Equals(string.Empty)) CreditLimit = "1000";
                    string Balance = "0";// row[13].ToString().Trim();
                    string Paymentterms = row["CreditDays"].ToString().Trim();
                    string OnHold = "0";// row["OnHold"].ToString().Trim();
                    string CustomerType = row["CustomerPeymentTerms"].ToString().Trim();
                    string inActive = row["Status"].ToString().Trim();
                    if (inActive.ToLower().Equals("active"))
                    {
                        inActive = "0";
                    }
                    else
                    {
                        inActive = "1";
                    }
                    string companyID = row["COMPANYID"].ToString().Trim();

                    if (CustomerType == "CREDIT")
                    {
                        CustomerType = "2";
                    }
                    else if (CustomerType == "CASH")
                    {
                        CustomerType = "1";
                    }
                    else
                    {
                        CustomerType = "3";
                    }
                    List<DaySequence> daySequence = new List<DaySequence>();
                    DaySequence ds = new DaySequence();



                    string B2B_Invoices = "20";// row["NumberOfInvoices"].ToString().Trim();
                    string GPSlongitude = "0";// row[16].ToString().Trim();
                    string GPSlatitude = "0";// row[17].ToString().Trim();


                    string SupervisorCode = row["SupervisorCode"].ToString().Trim();
                    string Supervisor = row["Supervisor"].ToString().Trim();
                    string RoutemanagerCode = row["RouteManagerCode"].ToString().Trim();
                    string Routemanager = row["RouteManager"].ToString().Trim();
                    string RouteCode = row["Route"].ToString().Trim();
                    string SalesManCode = row["SalesManCode"].ToString().Trim();
                    string SalesMan = row["SalesMan"].ToString().Trim();
                    Dictionary<string, string> empDic = new Dictionary<string, string>();
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

                    if (CustomerCode == string.Empty)
                        continue;

                    string CustomerID = "0";

                    string ExistCustomer = "";
                    #endregion

                    #region Get Customer Group
                    string GroupID2 = string.Empty;
                    string GroupID = GetFieldValue("CustomerGroupLanguage", "GroupID", " Description = '" + channelDescription.Trim() + "'  AND LanguageID = 1", db_vms);

                    if (GroupID == string.Empty)
                    {
                        GroupID = GetFieldValue("CustomerGroup", "isnull(MAX(GroupID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                        err = QueryBuilderObject.InsertQueryString("CustomerGroup", db_vms);

                        QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + channelDescription + "'");
                        err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);

                        QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "N'" + channelDescription + "'");
                        err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);
                    }
                    if (!CustomerNewGroup.Trim().Equals(string.Empty))
                    {
                        GroupID2 = GetFieldValue("CustomerGroupLanguage", "GroupID", " Description = '" + CustomerNewGroup.Trim() + "'  AND LanguageID = 1", db_vms);

                        if (GroupID2 == string.Empty)
                        {
                            GroupID2 = GetFieldValue("CustomerGroup", "isnull(MAX(GroupID),0) + 1", db_vms);

                            QueryBuilderObject.SetField("GroupID", GroupID2.ToString());
                            err = QueryBuilderObject.InsertQueryString("CustomerGroup", db_vms);

                            QueryBuilderObject.SetField("GroupID", GroupID2.ToString());
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + CustomerNewGroup + "'");
                            err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);

                            QueryBuilderObject.SetField("GroupID", GroupID2.ToString());
                            QueryBuilderObject.SetField("LanguageID", "2");
                            QueryBuilderObject.SetField("Description", "N'" + CustomerNewGroup + "'");
                            err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);
                        }
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

                        QueryBuilderObject.SetField("CustomerCode", "'" + outletCode + "'");
                        QueryBuilderObject.SetField("Phone", "'" + Phonenumber + "'");
                        QueryBuilderObject.SetField("Fax", "'" + Faxnumber + "'");
                        QueryBuilderObject.SetField("OnHold", OnHold);
                        QueryBuilderObject.SetField("New", "0");
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                        err = QueryBuilderObject.UpdateQueryString("Customer", " CustomerID = " + CustomerID, db_vms);

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

                    #endregion
                    UpdateNewCustomerOutlet(AccountID.ToString(), daySequence, supervisorID, GroupID, GroupID2, B2B_Invoices, CustomerType, outletCode, channelDescription, Paymentterms, CustomerOutletDescriptionEnglish, CustomerAddressEnglish, CustomerOutletDescriptionArabic, CustomerAddressArabic, Phonenumber, Faxnumber, OnHold, Taxable, CustomerCode, CreditLimit, Balance, GPSlongitude, GPSlatitude, outletBarCode, Email, companyID, inActive, CustomerID);
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
        private void UpdateNewCustomerOutlet(string parentAccount, List<DaySequence> daySequenc, string supervisorID, string GroupID, string GroupID2, string B2B_Inv, string CustType, string CustomerCode, string channelDescription, string Paymentterms, string CustomerDescriptionEnglish, string CustomerAddressEnglish, string CustomerDescriptionArabic, string CustomerAddressArabic, string Phonenumber, string Faxnumber, string OnHold, string Taxable, string HeadOfficeCode, string CreditLimit, string Balance, string Longitude, string latitude, string CustomerBarCode, string email, string divisionID, string inactive, string customerID)
        {
            int CustomerID;
            InCubeErrors err;

            if (Longitude == string.Empty)
                Longitude = "0";

            if (latitude == string.Empty)
                latitude = "0";

            string ExistCustomer = "";

            CustomerID = int.Parse(customerID);// int.Parse(GetFieldValue("Customer", "CustomerID", "CustomerCode='" + CustomerCode + "'", db_vms));

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
                    QueryBuilderObject.InsertQueryString("PaymentTerm", db_vms);

                    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'Every " + Paymentterms + " Days'");
                    QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms);

                    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                    QueryBuilderObject.SetField("LanguageID", "2");
                    QueryBuilderObject.SetField("Description", "'Every " + Paymentterms + " Days'");
                    err = QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms);
                }
            }

            #region Customer Outlet and language

            string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerID = " + CustomerID + " AND CustomerCode = '" + CustomerBarCode + "'", db_vms);
            if (!OutletID.Trim().Equals(string.Empty))
            {
                QueryBuilderObject.SetField("CustomerCode", "'" + CustomerCode + "'");
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
            //else if (err == InCubeErrors.Success)
            //{

            //    QueryBuilderObject.SetField("GroupID", GroupID.ToString());
            //    QueryBuilderObject.UpdateQueryString("CustomerOutletGroup","CustomerID="+CustomerID.ToString()+" and OutletID="+OutletID+"", db_vms);
            //}
            string CustOutDiv = GetFieldValue("CustomerOutletDivision", "customerID", "customerID=" + CustomerID.ToString() + " and OutletID=" + OutletID + " and DivisionID=" + divisionID + "", db_vms).Trim();
            if (CustOutDiv.Equals(string.Empty))
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID.ToString());
                QueryBuilderObject.SetField("DivisionID", divisionID.ToString().Trim());
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


            #region Customer Outlet Account
            int AccountID = 1;

            ExistCustomer = GetFieldValue("AccountCustOut", "AccountID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
            if (ExistCustomer != string.Empty)
            {
                AccountID = int.Parse(ExistCustomer);
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                //QueryBuilderObject.SetField("Balance", Balance);
                err = QueryBuilderObject.UpdateQueryString("Account", "AccountID=" + ExistCustomer + "", db_vms);
            }
            string Parent2 = AccountID.ToString();
            ExistCustomer = GetFieldValue("AccountCustOutDiv", "AccountID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " and DivisionID=" + divisionID + "", db_vms);
            if (ExistCustomer != string.Empty)
            {
                AccountID = int.Parse(ExistCustomer);
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                //QueryBuilderObject.SetField("Balance", Balance);
                err = QueryBuilderObject.UpdateQueryString("Account", "AccountID=" + ExistCustomer + "", db_vms);
            }
            //if (err == InCubeErrors.Success)
            //{
            //    err = UpdateFlag("accmst", "ConvCode='" + CustomerCode + "'");
            //}

            #endregion


            #endregion
        }
        public override void UpdateGeographicalLocation()
        {
            try
            {
                DataTable DT = new DataTable();
                string select = string.Format("select VISAAC,Division,SalesLocation,SalesArea,SalesCenter from CustomerMasterView");
                qry = new InCubeQuery(select, dbERP);
                err = qry.Execute();
                DT = qry.GetDataTable();
                int TOTALUPDATE = 0;
                int TOTALINSERT = 0;
                ClearProgress();
                SetProgressMax(DT.Rows.Count);
                foreach (DataRow dr in DT.Rows)
                {
                    ReportProgress("Updating Locations");
                    string custCode = dr["VISAAC"].ToString().Trim();
                    string Region = dr["Division"].ToString().Trim();
                    string StateDescription = dr["SalesLocation"].ToString().Trim();
                    string AreaLocation = StateDescription + " " + dr["SalesArea"].ToString().Trim();
                    string streetDescription = StateDescription + " " + dr["SalesCenter"].ToString().Trim();
                    string CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode='" + custCode + "'", db_vms).Trim();
                    string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerCode='" + custCode + "'", db_vms).Trim();
                    if (CustomerID.Equals(string.Empty) || OutletID.Equals(string.Empty)) continue;
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
                        if (err != InCubeErrors.Success)
                        {

                        }
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
                        if (err != InCubeErrors.Success)
                        {

                        }
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
                        if (err != InCubeErrors.Success)
                        {

                        }
                    }

                    QueryBuilderObject.SetField("StreetID", StreetID);
                    err = QueryBuilderObject.UpdateQueryString("CustomerOutlet", "CustomerID=" + CustomerID + " and OutletID=" + OutletID + "", db_vms);
                }
                DT.Dispose();
                WriteMessage("\r\n");
                WriteMessage("<<< Geo Location >>> Total Updated = " + TOTALUPDATE + " , Total Inserted = " + TOTALINSERT);
            }
            catch
            {

            }
        }
        public override void UpdateContractedFOC()
        {
            try
            {
                WriteExceptions("Entering Contract FOC Block", "Contracted FOC", false);
                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;
                // string getContract = string.Format("select * from CONTRACTEDFOCTRANSFER where SYNCHRONIZED=0 and cast(datepart(month,convert(datetime,('01/'+PERIOD+' 00:00:00'),103)) as varchar)=cast(datepart(month,convert(datetime,getdate(),103)) as varchar)");
                string getContract = string.Format("select * from CONTRACTEDFOCTRANSFER where SYNCHRONIZED=0 ");
                InCubeQuery qry = new InCubeQuery(getContract, dbERP);
                err = qry.Execute();
                DataTable dt = new DataTable();
                dt = qry.GetDataTable();
                ClearProgress();
                SetProgressMax(dt.Rows.Count);
                WriteExceptions("Contract FOC returned <<" + dt.Rows.Count + ">> Records from ERP", "Contracted FOC", false);
                foreach (DataRow dr in dt.Rows)
                {
                    ReportProgress("Updating ContractedFOC");
                    WriteExceptions("Contract FOC Filling Variables", "Contracted FOC", false);
                    string CustomerCode = dr["VISACCCUSTOMER"].ToString().Trim();
                    WriteExceptions("Contract FOC CustomerCode <<" + CustomerCode + ">> ", "Contracted FOC", false);
                    string contractID = dr["CONTRACTID"].ToString().Trim();
                    WriteExceptions("Contract FOC contractID <<" + contractID + ">> ", "Contracted FOC", false);
                    string itemCode = dr["ITEMCODE"].ToString().Trim();
                    WriteExceptions("Contract FOC itemCode <<" + itemCode + ">> ", "Contracted FOC", false);
                    string quantity = dr["QUANTITY"].ToString().Trim();
                    WriteExceptions("Contract FOC quantity <<" + quantity + ">> ", "Contracted FOC", false);
                    string contractType = dr["CONTRACTTYPE"].ToString().Trim();
                    WriteExceptions("Contract FOC contractType <<" + contractType + ">> ", "Contracted FOC", false);
                    string inactive = dr["INACTIVE"].ToString().Trim();
                    WriteExceptions("Contract FOC inactive <<" + inactive + ">> ", "Contracted FOC", false);
                    string RouteCode = dr["RouteCode"].ToString().Trim();
                    WriteExceptions("Contract FOC RouteCode <<" + RouteCode + ">> ", "Contracted FOC", false);
                    string companyCode = dr["COMPANYID"].ToString().Trim();
                    string SAPDivision = dr["SAPDivision"].ToString().Trim();
                    string DivisionID = GetFieldValue("SAPDivisions", "DivisionID", "CompanyCode = '" + companyCode + "' AND SAPDivision = '" + SAPDivision + "'", db_vms);
                    WriteExceptions("Contract FOC DivisionID <<" + DivisionID + ">> ", "Contracted FOC", false);
                    string NewStartDate = dr["ValidFrom"].ToString().Trim();
                    WriteExceptions("Contract FOC NewStartDate <<" + NewStartDate + ">> ", "Contracted FOC", false);
                    string NEwEndDate = dr["ValidTo"].ToString().Trim();
                    WriteExceptions("Contract FOC NEwEndDate <<" + NEwEndDate + ">> ", "Contracted FOC", false);
                    string Factor = dr["itFactor"].ToString().Trim();
                    WriteExceptions("Contract FOC Factor <<" + Factor + ">> ", "Contracted FOC", false);
                    string itUnit = dr["itUnit"].ToString().Trim();
                    WriteExceptions("Contract FOC itUnit <<" + itUnit + ">> ", "Contracted FOC", false);
                    string ContractDescription = dr["Description"].ToString().Trim();
                    WriteExceptions("Contract FOC ContractDescription <<" + ContractDescription + ">> ", "Contracted FOC", false);
                    if (itUnit.Equals("CASE"))
                    {

                    }
                    string startDate = string.Empty;
                    string endDate = string.Empty;
                    if (NewStartDate.Trim().Equals(string.Empty) && NEwEndDate.Trim().Equals(string.Empty))
                    {
                        startDate = DateTime.Now.Subtract(TimeSpan.FromDays(DateTime.Now.Day - 1)).ToString("yyyy/MM/dd");
                        endDate = DateTime.Parse(startDate).AddMonths(1).ToString("yyyy/MM/dd");
                    }
                    else
                    {
                        //startDate = DateTime.Parse(NewStartDate).Subtract(TimeSpan.FromDays(DateTime.Parse(NewStartDate).Day - 1)).ToString("yyyy/MM/dd");
                        //endDate = DateTime.Parse(NEwEndDate).AddMonths(1).ToString("yyyy/MM/dd");
                        //THIS HAS BEEN CHANGED TO HANDLE THE CASE WHERE WE RECEIVE START AND END DATE FOR THE CONTRACTED FOC
                        startDate = DateTime.Parse(NewStartDate).ToString("yyyy/MM/dd");
                        endDate = DateTime.Parse(NEwEndDate).ToString("yyyy/MM/dd");
                    }
                    string ForceGivingAllFOC = dr["FORCEGIVINGALLFOC"].ToString().Trim();
                    WriteExceptions("Contract FOC ForceGivingAllFOC <<" + ForceGivingAllFOC + ">> ", "Contracted FOC", false);
                    string VehicleID = GetFieldValue("Warehouse", "WarehouseID", "WarehouseCode='" + RouteCode + "'", db_vms);
                    WriteExceptions("Contract FOC VehicleID <<" + VehicleID + ">> ", "Contracted FOC", false);
                    string CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode='" + CustomerCode + "'", db_vms);
                    WriteExceptions("Contract FOC CustomerID <<" + CustomerID + ">> ", "Contracted FOC", false);
                    if (CustomerID.Trim().Equals(string.Empty))
                    {
                        WriteExceptions("Contract FOC CustomerID is empty, Skipping this loop ", "Contracted FOC", false);
                        continue;
                    }
                    string outletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerCode='" + CustomerCode + "'", db_vms);
                    WriteExceptions("Contract FOC outletID <<" + outletID + ">> ", "Contracted FOC", false);
                    if (outletID.Trim().Equals(string.Empty))
                    {
                        WriteExceptions("Contract FOC outletID is empty, Skipping this loop ", "Contracted FOC", false);
                        continue;
                    }

                    string TerritoryID = GetFieldValue("CustOutTerritory", "TOP 1 TerritoryID", "CustomerID = " + CustomerID + " AND OutletID = " + outletID, db_vms);
                    WriteExceptions("Contract FOC TerritoryID <<" + TerritoryID + ">> ", "Contracted FOC", false);
                    if (TerritoryID.Trim().Equals(string.Empty))
                    {
                        WriteExceptions("Contract FOC TerritoryID is empty, Skipping this loop ", "Contracted FOC", false);
                        continue;
                    }

                    string packID = GetFieldValue("Pack", "PackID", "ItemID in (select ItemID from Item where ItemCode='" + itemCode + "') and Quantity=" + Factor + "", db_vms);
                    WriteExceptions("Contract FOC packID <<" + packID + ">> ", "Contracted FOC", false);
                    if (packID.Trim().Equals(string.Empty))
                    {
                        continue;
                    }
                    object field = new object();
                    string CheckUploaded = string.Format("select top(1)uploaded,deviceserial from RouteHistory where vehicleid=" + VehicleID + " ORDER BY RouteHistoryID Desc ");
                    qry = new InCubeQuery(CheckUploaded, db_vms);
                    err = qry.Execute();
                    err = qry.FindFirst();
                    err = qry.GetField("uploaded", ref field);
                    string uploaded = field.ToString().Trim();
                    err = qry.GetField("deviceserial", ref field);
                    string deviceserial = field.ToString().Trim();
                    if (!uploaded.ToString().Trim().Equals(string.Empty) && !uploaded.ToString().Trim().Equals("System.Object"))
                    {
                        if (Convert.ToBoolean(uploaded.ToString().Trim()))
                        {
                            WriteMessage("\r\n");
                            WriteMessage("<<< The Route " + RouteCode + " is not downloaded . No stock will be added .>>> Total Updated = " + TOTALUPDATED);
                            // continue;
                        }

                    }
                    //string CheckUploaded = string.Format("select top(1)uploaded,deviceserial from RouteHistory where EmployeeID=" + employeeID + " ORDER BY RouteHistoryID Desc ");
                    //qry = new InCubeQuery(CheckUploaded, db_vms);
                    //err = qry.Execute();
                    //err = qry.FindFirst();
                    //err = qry.GetField("uploaded", ref field);
                    //string uploaded = field.ToString().Trim();
                    //err = qry.GetField("deviceserial", ref field);
                    //string deviceserial = field.ToString().Trim();
                    //if (!uploaded.ToString().Trim().Equals(string.Empty) && !uploaded.ToString().Trim().Equals("System.Object"))
                    //{
                    //    if (Convert.ToBoolean(uploaded.ToString().Trim()))
                    //    {
                    //        WriteMessage("\r\n");
                    //        WriteMessage("<<< The Route " + RouteCode + " is not downloaded . No stock will be added .>>> Total Updated = " + TOTALUPDATED);
                    //        continue;
                    //    }

                    //}

                    //string routeID=GetFieldValue("RouteCustomer","RouteID","CustomerID")
                    string existContract = GetFieldValue("ContractedFOC", "ContractID", "CustomerID=" + CustomerID + " and OutletID=" + outletID + " and ContractID=" + contractID + "", db_vms);
                    if (existContract.Trim().Equals(string.Empty))
                    {
                        WriteExceptions("Contract FOC contractID <<" + contractID + ">> Does NOT exist, insert..", "Contracted FOC", false);
                        QueryBuilderObject.SetField("CustomerID", CustomerID);
                        QueryBuilderObject.SetField("OutletID", outletID);
                        QueryBuilderObject.SetField("ContractID", contractID);
                        QueryBuilderObject.SetField("PeriodTypeID", "3");
                        QueryBuilderObject.SetField("ContractTypeID", contractType);
                        QueryBuilderObject.SetField("ForceGivingAllFOC", ForceGivingAllFOC);
                        QueryBuilderObject.SetField("Inactive", inactive);
                        QueryBuilderObject.SetField("UpdateDate", "'" + DateTime.Now.ToString("yyyy/MM/dd") + "'");
                        QueryBuilderObject.SetField("ValidFrom", "'" + startDate + "'");
                        QueryBuilderObject.SetField("ValidTo", "'" + endDate + "'");
                        QueryBuilderObject.SetField("IsTransferable", "0");
                        QueryBuilderObject.SetField("IsOneTime", "0");
                        QueryBuilderObject.SetField("DivisionID", DivisionID);
                        QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                        err = QueryBuilderObject.InsertQueryString("ContractedFOC", db_vms);
                        WriteExceptions("Contract FOC contractID <<" + contractID + ">> insert result is <<" + err.ToString() + ">>", "Contracted FOC", false);
                        TOTALINSERTED++;
                    }
                    else
                    {
                        WriteExceptions("Contract FOC contractID <<" + contractID + ">> already exist, update..", "Contracted FOC", false);
                        QueryBuilderObject.SetField("ForceGivingAllFOC", ForceGivingAllFOC);
                        QueryBuilderObject.SetField("Inactive", inactive);
                        QueryBuilderObject.SetField("UpdateDate", "'" + DateTime.Now.ToString("yyyy/MM/dd") + "'");
                        QueryBuilderObject.SetField("ValidFrom", "'" + startDate + "'");
                        QueryBuilderObject.SetField("ValidTo", "'" + endDate + "'");
                        QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                        err = QueryBuilderObject.UpdateQueryString("ContractedFOC", "CustomerID=" + CustomerID + " and OutletID=" + outletID + " and ContractID=" + contractID + "", db_vms);
                        WriteExceptions("Contract FOC contractID <<" + contractID + ">> update result is <<" + err.ToString() + ">>", "Contracted FOC", false);
                        TOTALUPDATED++;
                    }

                    string existContractLanguage = GetFieldValue("ContractedFOCLanguage", "ContractID", "ContractID=" + contractID + "", db_vms).Trim();
                    if (existContractLanguage.Equals(string.Empty))
                    {
                        QueryBuilderObject.SetField("ContractID", contractID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + ContractDescription + "'");
                        err = QueryBuilderObject.InsertQueryString("ContractedFOCLanguage", db_vms);
                    }

                    string existDetail = GetFieldValue("ContractedFOCDetail", "ContractID", "CustomerID=" + CustomerID + " and OutletID=" + outletID + " and ContractID=" + contractID + " and PackID=" + packID + "", db_vms);

                    if (existDetail.Trim().Equals(string.Empty))
                    {
                        WriteExceptions("Contract FOC contractID <<" + contractID + ">> Detail Does Not, insert..", "Contracted FOC", false);
                        //string rowID = GetFieldValue("ContractedFOCDetail", "isnull(max(RowID),0)+1", "CustomerID=" + CustomerID + " and OutletID=" + outletID + " and ContractID=" + contractID + " and PackID=" + packID + "", db_vms);
                        QueryBuilderObject.SetField("CustomerID", CustomerID);
                        QueryBuilderObject.SetField("OutletID", outletID);
                        QueryBuilderObject.SetField("ContractID", contractID);
                        //QueryBuilderObject.SetField("RowID", rowID);
                        QueryBuilderObject.SetField("PackID", packID);
                        QueryBuilderObject.SetField("Quantity", quantity);
                        QueryBuilderObject.SetField("RemainingQuantity", quantity);
                        QueryBuilderObject.SetField("TempQuantity", "-1");
                        err = QueryBuilderObject.InsertQueryString("ContractedFOCDetail", db_vms);
                        WriteExceptions("Contract FOC contractID <<" + contractID + ">> Detail insert result is <<" + err.ToString() + ">>", "Contracted FOC", false);
                    }
                    else
                    {
                        WriteExceptions("Contract FOC contractID <<" + contractID + ">> Detail already exist, update..", "Contracted FOC", false);
                        QueryBuilderObject.SetField("Quantity", quantity);
                        err = QueryBuilderObject.UpdateQueryString("ContractedFOCDetail", "CustomerID=" + CustomerID + " and OutletID=" + outletID + " and ContractID=" + contractID + " and PackID=" + packID + "", db_vms);
                        WriteExceptions("Contract FOC contractID <<" + contractID + ">> Detail update result is <<" + err.ToString() + ">>", "Contracted FOC", false);
                    }
                    if (err == InCubeErrors.Success)
                    {
                        string updateFlag = "update SAP_CONTRACTEDFOCTRANSFER_Table set SYNCHRONIZED=1 where VISACCCUSTOMER='" + CustomerCode + "' and convert(numeric,contractID)=" + contractID + " and ItemCode='" + itemCode + "'";
                        InCubeQuery flagQry = new InCubeQuery(updateFlag, dbERP);
                        err = flagQry.ExecuteNonQuery();
                    }
                }
                WriteMessage("\r\n");
                WriteMessage("<<< ContractedFOC>>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
            }
            catch (Exception ex)
            {
                WriteExceptions("Contract FOC handled exception, <<" + ex.Message + ">>--------> StackTrace<<<" + ex.StackTrace.ToString() + ">>>", "Contracted FOC", false);
            }
        }
        public override void UpdateWarehouse()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            object field = new object();

            DataTable DT = new DataTable();
            qry = new InCubeQuery("select Distinct (locode) as MainWarehouseCode,ccode from glumast where van=0 and changeflg='Y' and isnull(ccode,'')<>''", dbERP);
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

                string WarehouseCode = row["MainWarehouseCode"].ToString().Trim();
                string Barcode = row["ccode"].ToString().Trim();
                string VehicleCode = string.Empty; //row["RouteCode"].ToString().Trim();
                string WarehouceName = row["MainWarehouseCode"].ToString().Trim();
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
                UpdateFlag("glumast", "locode='" + WarehouseCode + "'");
            }
            DT = new DataTable();
            qry = new InCubeQuery("select Distinct locode as RouteCode,ccode as refno from glumast where changeflg='Y' and van=1  and isnull(ccode,'')<>''", dbERP);
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

                string WarehouseCode = row["RouteCode"].ToString().Trim();
                string VehicleCode = string.Empty; //row["RouteCode"].ToString().Trim();
                string WarehouceName = row["RouteCode"].ToString().Trim();
                string refNo = row["refno"].ToString().Trim();
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
                UpdateFlag("glumast", "locode='" + WarehouseCode + "'");
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
                QueryBuilderObject.SetField("Description", "'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("Address", "'" + Address + "'");
                QueryBuilderObject.UpdateQueryString("WarehouseLanguage", " WarehouseID =" + WarehouseID + " AND LanguageID = 1", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("Address", "'" + Address + "'");
                err = QueryBuilderObject.InsertQueryString("WarehouseLanguage", db_vms);
            }

            ExitWarehouse = GetFieldValue("WarehouseLanguage", "WarehouseID", "WarehouseID = " + WarehouseID + " AND LanguageID = 2", db_vms); //Arabic
            if (ExitWarehouse != string.Empty)
            {
                QueryBuilderObject.SetField("Description", "N'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("Address", "N'" + Address + "'");
                QueryBuilderObject.UpdateQueryString("WarehouseLanguage", " WarehouseID =" + WarehouseID + " AND LanguageID = 2", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + WarehouseCode + "'");
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

                string SalesPersonID = GetFieldValue("Employee", "EmployeeID", " EmployeeCode='" + SalesmanCode.Trim() + "'", db_vms);

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
                        //WriteMessage("Warning Salesperson Code : (" + SalesmanCode + ") is assigned to 2 vehicles , Second Vehicle Code : (" + WarehouseCode + ") this row is skipped");
                        WriteMessage("\r\n");
                    }
                }
                string VehicleWH = GetFieldValue("VehicleLoadingWh", "vehicleID", "vehicleID=" + VehicleID + " and WarehouseID=" + loadingWH + "", db_vms);
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
        public override void UpdateSalesPerson()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            UpdateBanks();

            object field = new object();

            DataTable DT = new DataTable();
            qry = new InCubeQuery(@"select ''as Channel,supervisorCode,Supervisor,ManagerCode RouteManagerCode,Manager RouteManager,SalesMan,RTCODE SalesManCode,CompanyID,locode as VehicleCode,
loname as VehicleName,MainWarehouse as LoadingWH, case when van is null or van='NULL'  then 1 else 2 end as WarehouseType,RTCODE
from SAP_RouteMaster_Table WHERE CHANGEFLG='Y' and locode<> MainWarehouse group By supervisorCode,Supervisor,ManagerCode,Manager,SalesMan,SalesManCode,CompanyID,loname,locode,van,MainWarehouse,RTCODE", dbERP);
            err = qry.Execute();
            DT = qry.GetDataTable();

            ClearProgress();
            SetProgressMax(DT.Rows.Count);

            foreach (DataRow row in DT.Rows)
            {
                try
                {
                    ReportProgress("Updating Salespersons");

                    //Employee Code	
                    //Employee name English	
                    //Employee name Arabic	
                    //Phone	
                    //Credit limit	
                    //Balance	
                    //Division
                    string SupervisorCode = row["SupervisorCode"].ToString().Trim();
                    string Supervisor = row["Supervisor"].ToString().Trim();
                    string RoutemanagerCode = row["RouteManagerCode"].ToString().Trim();
                    string Routemanager = row["RouteManager"].ToString().Trim();
                    if (Routemanager.Trim().Equals(string.Empty)) Routemanager = "0";
                    if (Supervisor.Trim().Equals(string.Empty)) Supervisor = "0";
                    string DivisionCode = row["CompanyID"].ToString().Trim();

                    string SalesManCode = row["SalesManCode"].ToString().Trim();
                    string SalesMan = row["SalesMan"].ToString().Trim();
                    if (SalesMan.Trim().Equals(string.Empty)) SalesMan = "0";
                    string channelDescription = row["Channel"].ToString().Trim();
                    string VehicleCode = row["VehicleCode"].ToString().Trim();
                    string VehicleName = row["VehicleName"].ToString().Trim();
                    string LoadingWH = row["LoadingWH"].ToString().Trim();
                    string WarehouseType = row["WarehouseType"].ToString().Trim();


                    string TerritoryCode = row["RTCODE"].ToString().Trim();

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
                            //QueryBuilderObject.SetField("EmployeeTypeID", str.Value.Substring(str.Value.Length - 1, 1));
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

                        if (str.Value.Substring(str.Value.Length - 1, 1).Trim().Equals("2"))
                        {

                            string WarehouseID = GetFieldValue("Warehouse", "WarehouseID", "Barcode='" + LoadingWH + "'", db_vms);
                            if (WarehouseID == string.Empty)
                            {
                                WarehouseID = GetFieldValue("Warehouse", "isnull(MAX(WarehouseID),0) + 1", db_vms);
                            }
                            AddUpdateWarehouse(WarehouseID, WarehouseID, LoadingWH, LoadingWH, LoadingWH, LoadingWH, "", ref TOTALUPDATED, ref TOTALINSERTED, "1", LoadingWH);
                            string VehicleID = GetFieldValue("Warehouse", "WarehouseID", "Barcode='" + VehicleCode + "'", db_vms);
                            if (VehicleID == string.Empty)
                            {
                                VehicleID = GetFieldValue("Warehouse", "isnull(MAX(WarehouseID),0) + 1", db_vms);

                            }
                            AddUpdateWarehouse(WarehouseID, VehicleID, VehicleCode, VehicleName, "", "", SalesManCode, ref TOTALUPDATED, ref TOTALINSERTED, "2", LoadingWH);

                            #region Territory

                            string TerritoryID = GetFieldValue("Territory", "TerritoryID", "TerritoryCode='" + TerritoryCode + "'", db_vms);
                            if (TerritoryID == string.Empty)
                            {
                                TerritoryID = GetFieldValue("[Territory]", "isnull(max(TerritoryID),0)+1", db_vms);
                                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                                QueryBuilderObject.SetField("OrganizationID", "1");
                                QueryBuilderObject.SetField("TerritoryCode", "'" + TerritoryCode + "'");
                                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                                err = QueryBuilderObject.InsertQueryString("Territory", db_vms);

                                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                                QueryBuilderObject.SetField("LanguageID", "1");
                                QueryBuilderObject.SetField("Description", "'" + TerritoryCode + "'");
                                QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);

                                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                                QueryBuilderObject.SetField("LanguageID", "2");
                                QueryBuilderObject.SetField("Description", "'" + TerritoryCode + "'");
                                err = QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);

                            }
                            else
                            {
                                QueryBuilderObject.SetField("Description", "'" + TerritoryCode + "'");
                                err = QueryBuilderObject.UpdateQueryString("TerritoryLanguage", "TerritoryID=" + TerritoryID + "", db_vms);
                            }
                            string existTerrDiv = GetFieldValue("TerritoryDivision", "TerritoryID", "TerritoryID=" + TerritoryID + " and DivisionID=" + DivisionID + "", db_vms).Trim();
                            if (existTerrDiv.Equals(string.Empty))
                            {
                                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                                QueryBuilderObject.SetField("DivisionID", DivisionID);
                                err = QueryBuilderObject.InsertQueryString("TerritoryDivision", db_vms);
                            }

                            string ExistEmployeeTerr = GetFieldValue("EmployeeTerritory", "employeeID", "EmployeeID=" + employeeID + " and TerritoryID=" + TerritoryID + "", db_vms);
                            if (ExistEmployeeTerr == string.Empty)
                            {
                                QueryBuilderObject.SetField("EmployeeID", employeeID);
                                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                                err = QueryBuilderObject.InsertQueryString("EmployeeTerritory", db_vms);
                            }

                            //UpdateFlag("SAP_RouteMaster_Table", "RTCODE='" + SalesManCode + "'");
                            #endregion

                        }
                    }

                }
                catch
                {

                }
            }
            DT.Dispose();

            WriteMessage("\r\n");
            WriteMessage("<<< SALESPERSON >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }
        public override void UpdateTarget()
        {
            try
            {


                DataTable dt = new DataTable();
                DataTable tbl = new DataTable();
                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;

                #region Monthly Target Territory

                dt = new DataTable();
                string selectMonthTargetTerr = string.Format(@"select ROUTECODE,QUANTITY,REVENUE,CompanyID,PERIOD ,('01/'+PERIOD+' 00:00:00')as FromDate,(str(day(DATEADD(s,-1,DATEADD(mm, DATEDIFF(m,0,PERIOD+'/01')+1,0))))+'/'+PERIOD+' 23:59:59')as ToDate,BRANDCODE,CATEGORY,DAYQTY,DAYREVENUE from SALESTARGETDIVISION  
where CHANGEFLG='Y' group by QUANTITY,REVENUE,ROUTECODE,PERIOD,CompanyID,BRANDCODE,CATEGORY,DAYQTY,DAYREVENUE   ");
                qry = new InCubeQuery(selectMonthTargetTerr, dbERP);
                err = qry.Execute();
                dt = qry.GetDataTable();
                TOTALUPDATED = 0;
                TOTALINSERTED = 0;
                ClearProgress();
                SetProgressMax(dt.Rows.Count);

                //IMPORTANT : THIS IS THE QUERY THAT SHOULD BE USED WITH MONTHLY TARGET [select ROUTECODE,sum(QUANTITY),sum(REVENUE),('01/'+PERIOD)as SD,('31/'+PERIOD)as ED,BRANDCODE,CATEGORY from SALESTARGETDIVISION  
                //group by ROUTECODE,PERIOD,BRANDCODE,CATEGORY]
                foreach (DataRow dr in dt.Rows)
                {

                    ReportProgress("Updating Territory Target");

                    string AcheivmentID = "1";
                    string RouteCode = dr["ROUTECODE"].ToString().Trim();
                    string Quantity = dr["QUANTITY"].ToString().Trim();
                    string CompanyID = dr["CompanyID"].ToString().Trim();
                    string Amount = dr["REVENUE"].ToString().Trim();
                    string DLYDATE = dr["PERIOD"].ToString().Trim();
                    string value = string.Empty;
                    if (!Amount.Equals(string.Empty))
                    {
                        if (decimal.Parse(Amount) > 0)
                        {
                            AcheivmentID = "13";
                            value = Amount;
                        }
                        else
                        {
                            AcheivmentID = "15";
                            value = Quantity;
                        }
                    }


                    string BrandCode = dr["BRANDCODE"].ToString().Trim();
                    string CategoryCode = dr["CATEGORY"].ToString().Trim();
                    string xc = dr["fromDate"].ToString().Trim();
                    string fromDate = DateTime.Parse(dr["fromDate"].ToString().Trim()).ToString("yyyy/MM/dd HH:mm:ss");
                    string xx = dr["ToDate"].ToString().Trim();
                    string ToDate = DateTime.Parse(dr["ToDate"].ToString().Trim()).ToString("yyyy/MM/dd HH:mm:ss");

                    string RouteID = GetFieldValue("Territory", "TerritoryID", "TerritoryCode='" + RouteCode + "'", db_vms);
                    if (RouteID.Trim().Equals(string.Empty))
                    {
                        continue;
                    }

                    string selectItems = string.Format("select ItemID from Item where Origin='" + BrandCode + "' and ItemCategoryID in (select ItemCategoryID from ItemCategory where ItemCategoryCode='" + CompanyID + CategoryCode + "')");
                    qry = new InCubeQuery(selectItems, db_vms);
                    err = qry.Execute();
                    tbl = new DataTable();
                    tbl = qry.GetDataTable();
                    string targetID = string.Empty;
                    foreach (DataRow row in tbl.Rows)
                    {
                        string itemid = row["ItemID"].ToString().Trim();
                        string packQty = GetFieldValue("Pack", "Quantity", "ItemID=" + itemid + " and Quantity>1", db_vms).Trim();
                        if (!packQty.Equals(string.Empty))
                        {
                            value = (decimal.Parse(value) * decimal.Parse(packQty)).ToString();
                        }
                        string existTargetAmount = GetFieldValue("AchievementTargetTerritory", "TargetID", "AchievementID=" + AcheivmentID + " and TerritoryID=" + RouteID + " and ItemID=" + row["ItemID"].ToString().Trim() + " and FromDate='" + fromDate + "' and ToDate='" + ToDate + "'", db_vms);
                        if (existTargetAmount.Trim().Equals(string.Empty))
                        {
                            string existTarget = GetFieldValue("AchievementTargetTerritory", "max(TargetID)", "AchievementID=" + AcheivmentID + " and TerritoryID=" + RouteID + " and ItemID=" + row["ItemID"].ToString().Trim() + " ", db_vms);
                            if (!existTarget.Trim().Equals(string.Empty))
                            {
                                targetID = (int.Parse(existTarget.Trim()) + 1).ToString();
                            }
                            else
                            {
                                targetID = "0";
                            }

                            QueryBuilderObject.SetField("TargetID", targetID);
                            QueryBuilderObject.SetField("AchievementID", AcheivmentID);
                            QueryBuilderObject.SetField("TerritoryID", RouteID);
                            QueryBuilderObject.SetField("Value", value);
                            QueryBuilderObject.SetField("ItemID", row["ItemID"].ToString().Trim());
                            QueryBuilderObject.SetField("FromDate", "'" + DateTime.Parse(fromDate).ToString("yyyy/MM/dd HH:mm:ss") + "'");
                            QueryBuilderObject.SetField("ToDate", "'" + DateTime.Parse(ToDate).ToString("yyyy/MM/dd HH:mm:ss") + "'");
                            err = QueryBuilderObject.InsertQueryString("AchievementTargetTerritory", db_vms);
                            TOTALINSERTED++;
                        }
                        else
                        {

                            QueryBuilderObject.SetField("Value", "Value+" + value);
                            err = QueryBuilderObject.UpdateQueryString("AchievementTargetTerritory", "TargetID=" + existTargetAmount + " and AchievementID=" + AcheivmentID + " and TerritoryID=" + RouteID + " and ItemID=" + row["ItemID"].ToString().Trim() + " and FromDate='" + fromDate + "' and ToDate='" + ToDate + "'", db_vms);
                            TOTALUPDATED++;
                        }

                        if (err == InCubeErrors.Success)
                        {
                            err = UpdateFlag("SALESTARGETDIVISION", "RouteCode='" + RouteCode + "' and CATEGORY='" + CategoryCode + "' and PERIOD='" + DLYDATE + "'");
                        }
                        else
                        {

                        }

                    }

                }

                WriteMessage("\r\n");
                WriteMessage("<<< Monthly Territory Target >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);

                #endregion

                #region Daily Target Territory

                //                dt = new DataTable();
                //                string DailyTar = string.Format(@"select ROUTECODE,QUANTITY,REVENUE ,('01/'+PERIOD+' 00:00:00')as FromDate,(str(day(DATEADD(s,-1,DATEADD(mm, DATEDIFF(m,0,PERIOD+'/01')+1,0))))+'/'+PERIOD+' 23:59:59')as ToDate,BRANDCODE,CATEGORY,DAYQTY,DAYREVENUE from SALESTARGETDIVISION  
                //where CHANGEFLG='Y' group by QUANTITY,REVENUE,ROUTECODE,PERIOD,BRANDCODE,CATEGORY,DAYQTY,DAYREVENUE  ");
                //                qry = new InCubeQuery(DailyTar, dbERP);
                //                err = qry.Execute();
                //                dt = qry.GetDataTable();
                TOTALUPDATED = 0;
                TOTALINSERTED = 0;
                ClearProgress();
                SetProgressMax(dt.Rows.Count);

                //IMPORTANT : THIS IS THE QUERY THAT SHOULD BE USED WITH MONTHLY TARGET [select ROUTECODE,sum(QUANTITY),sum(REVENUE),('01/'+PERIOD)as SD,('31/'+PERIOD)as ED,BRANDCODE,CATEGORY from SALESTARGETDIVISION  
                //group by ROUTECODE,PERIOD,BRANDCODE,CATEGORY]
                foreach (DataRow dr in dt.Rows)
                {
                    ReportProgress("Updating Territory Target");

                    string AcheivmentID = "1";
                    string RouteCode = dr["ROUTECODE"].ToString().Trim();
                    string Quantity = dr["DAYQTY"].ToString().Trim();
                    string Amount = dr["DAYREVENUE"].ToString().Trim();
                    string value = string.Empty;
                    if (!Amount.Equals(string.Empty))
                    {
                        if (decimal.Parse(Amount) > 0)
                        {
                            AcheivmentID = "1";
                            value = Amount;
                        }
                        else
                        {
                            AcheivmentID = "3";
                            value = Quantity;
                        }
                    }


                    string BrandCode = dr["BRANDCODE"].ToString().Trim();
                    string CategoryCode = dr["CATEGORY"].ToString().Trim();
                    string fromDate = DateTime.Parse(dr["fromDate"].ToString().Trim()).ToString("yyyy/MM/dd HH:mm:ss");
                    string ToDate = DateTime.Parse(dr["ToDate"].ToString().Trim()).ToString("yyyy/MM/dd HH:mm:ss");

                    string RouteID = GetFieldValue("Territory", "TerritoryID", "TerritoryCode='" + RouteCode + "'", db_vms);
                    if (RouteID.Trim().Equals(string.Empty))
                    {
                        continue;
                    }

                    string selectItems = string.Format("select ItemID from Item where Origin='" + BrandCode + "' and ItemCategoryID in (select ItemCategoryID from ItemCategory where ItemCategoryCode='" + CategoryCode + "')");
                    qry = new InCubeQuery(selectItems, db_vms);
                    err = qry.Execute();
                    tbl = new DataTable();
                    tbl = qry.GetDataTable();
                    string targetID = string.Empty;
                    foreach (DataRow row in tbl.Rows)
                    {
                        string itemid = row["ItemID"].ToString().Trim();
                        string packQty = GetFieldValue("Pack", "Quantity", "ItemID=" + itemid + " and Quantity>1", db_vms).Trim();
                        if (!packQty.Equals(string.Empty))
                        {
                            value = (decimal.Parse(value) * decimal.Parse(packQty)).ToString();
                        }
                        string existTargetAmount = GetFieldValue("AchievementTargetTerritory", "TargetID", "AchievementID=" + AcheivmentID + " and TerritoryID=" + RouteID + " and ItemID=" + row["ItemID"].ToString().Trim() + " and FromDate='" + fromDate + "' and ToDate='" + ToDate + "'", db_vms);
                        if (existTargetAmount.Trim().Equals(string.Empty))
                        {
                            string existTarget = GetFieldValue("AchievementTargetTerritory", "max(TargetID)", "AchievementID=" + AcheivmentID + " and TerritoryID=" + RouteID + " and ItemID=" + row["ItemID"].ToString().Trim() + " ", db_vms);
                            if (!existTarget.Trim().Equals(string.Empty))
                            {
                                targetID = (int.Parse(existTarget.Trim()) + 1).ToString();
                            }
                            else
                            {
                                targetID = "0";
                            }

                            QueryBuilderObject.SetField("TargetID", targetID);
                            QueryBuilderObject.SetField("AchievementID", AcheivmentID);
                            QueryBuilderObject.SetField("TerritoryID", RouteID);
                            QueryBuilderObject.SetField("Value", value);
                            QueryBuilderObject.SetField("ItemID", row["ItemID"].ToString().Trim());
                            QueryBuilderObject.SetField("FromDate", "'" + DateTime.Parse(fromDate).ToString("yyyy/MM/dd HH:mm:ss") + "'");
                            QueryBuilderObject.SetField("ToDate", "'" + DateTime.Parse(ToDate).ToString("yyyy/MM/dd HH:mm:ss") + "'");
                            err = QueryBuilderObject.InsertQueryString("AchievementTargetTerritory", db_vms);
                            TOTALINSERTED++;
                        }
                        else
                        {

                            QueryBuilderObject.SetField("Value", "Value+" + value);
                            err = QueryBuilderObject.UpdateQueryString("AchievementTargetTerritory", "TargetID=" + existTargetAmount + " and AchievementID=" + AcheivmentID + " and TerritoryID=" + RouteID + " and ItemID=" + row["ItemID"].ToString().Trim() + " and FromDate='" + fromDate + "' and ToDate='" + ToDate + "'", db_vms);
                            TOTALUPDATED++;
                        }

                        if (err == InCubeErrors.Success)
                        {
                            err = UpdateFlag("SALESTARGETDIVISION", "RouteCode='" + RouteCode + "' and CATEGORY='" + CategoryCode + "' and DLYDATE='" + DateTime.Parse(fromDate).ToString("yyyy/MM/dd") + "'");
                        }
                        else
                        {

                        }


                    }

                }

                WriteMessage("\r\n");
                WriteMessage("<<< Daily Territory Target >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);

                #endregion


                #region Monthly Target Route
                //                DataTable dt = new DataTable();
                //                string selectMonthTarget = string.Format(@"select ROUTECODE,sum(QUANTITY) as QUANTITY,sum(REVENUE) as REVENUE,('01/'+PERIOD+' 00:00:00')as FromDate,('31/'+PERIOD+' 23:59:59')as ToDate,BRANDCODE,CATEGORY from SALESTARGETDIVISION  
                //where CHANGEFLG='Y' group by ROUTECODE,PERIOD,BRANDCODE,CATEGORY  ");
                //                InCubeQuery qry = new InCubeQuery(selectMonthTarget, dbERP);
                //                err = qry.Execute();
                //                dt = qry.GetDataTable();
                //                DataTable tbl;
                //                int TOTALUPDATED = 0;
                //                int TOTALINSERTED = 0;
                //                ClearProgress();
                //                SetProgressMaxdt.Rows.Count;
                //                //IMPORTANT : THIS IS THE QUERY THAT SHOULD BE USED WITH MONTHLY TARGET [select ROUTECODE,sum(QUANTITY),sum(REVENUE),('01/'+PERIOD)as SD,('31/'+PERIOD)as ED,BRANDCODE,CATEGORY from SALESTARGETDIVISION  
                //                //group by ROUTECODE,PERIOD,BRANDCODE,CATEGORY]
                //                foreach (DataRow dr in dt.Rows)
                //                {


                //                    IntegrationForm.progressBar1.Value++;
                //                    IntegrationForm.lblProgress.Text = "Updating Monthly Route Target" + " " + IntegrationForm.progressBar1.Value + " / " + IntegrationForm.progressBar1.Maximum;
                //                    ;
                //                    string AcheivmentID = "1";
                //                    string RouteCode = dr["ROUTECODE"].ToString().Trim();
                //                    string Quantity = dr["QUANTITY"].ToString().Trim();
                //                    string Amount = dr["REVENUE"].ToString().Trim();
                //                    string value = string.Empty;
                //                    if (!Amount.Equals(string.Empty))
                //                    {
                //                        if (decimal.Parse(Amount) > 0)
                //                        {
                //                            AcheivmentID = "13";
                //                            value = Amount;
                //                        }
                //                        else
                //                        {
                //                            AcheivmentID = "15";
                //                            value = Quantity;
                //                        }
                //                    }


                //                    string BrandCode = dr["BRANDCODE"].ToString().Trim();
                //                    string CategoryCode = dr["CATEGORY"].ToString().Trim();
                //                    string fromDate = DateTime.Parse(dr["fromDate"].ToString().Trim()).ToString("yyyy/MM/dd HH:mm:ss");
                //                    string ToDate = DateTime.Parse(dr["ToDate"].ToString().Trim()).ToString("yyyy/MM/dd HH:mm:ss");

                //                    string RouteID = GetFieldValue("[Route]", "RouteID", "RouteCode='" + RouteCode + "'", db_vms);
                //                    if (RouteID.Trim().Equals(string.Empty))
                //                    {
                //                        continue;
                //                    }

                //                    string selectItems = string.Format("select ItemID from Item where Origin='" + BrandCode + "' and ItemCategoryID in (select ItemCategoryID from ItemCategory where ItemCategoryCode='" + CategoryCode + "')");
                //                    qry = new InCubeQuery(selectItems, db_vms);
                //                    err = qry.Execute();
                //                    tbl = new DataTable();
                //                    tbl = qry.GetDataTable();
                //                    string targetID = string.Empty;
                //                    foreach (DataRow row in tbl.Rows)
                //                    {
                //                        string existTarget = GetFieldValue("AchievementTargetRoute", "max(TargetID)", "AchievementID=" + AcheivmentID + " and RouteID=" + RouteID + " and ItemID=" + row["ItemID"].ToString().Trim() + "", db_vms);
                //                        if (!existTarget.Trim().Equals(string.Empty))
                //                        {
                //                            targetID = (int.Parse(existTarget.Trim()) + 1).ToString();
                //                        }
                //                        else
                //                        {
                //                            targetID = "0";
                //                        }
                //                        string itemid = row["ItemID"].ToString().Trim();
                //                        QueryBuilderObject.SetField("TargetID", targetID);
                //                        QueryBuilderObject.SetField("AchievementID", AcheivmentID);
                //                        QueryBuilderObject.SetField("RouteID", RouteID);
                //                        QueryBuilderObject.SetField("Value", value);
                //                        QueryBuilderObject.SetField("ItemID", row["ItemID"].ToString().Trim());
                //                        QueryBuilderObject.SetField("FromDate", "'" + DateTime.Parse(fromDate).ToString("yyyy/MM/dd HH:mm:ss") + "'");
                //                        QueryBuilderObject.SetField("ToDate", "'" + DateTime.Parse(ToDate).ToString("yyyy/MM/dd HH:mm:ss") + "'");
                //                        err = QueryBuilderObject.InsertQueryString("AchievementTargetRoute", db_vms);
                //                        TOTALINSERTED++;
                //                        if (err == InCubeErrors.Success)
                //                        {
                //                            //err = UpdateFlag("SALESTARGETDIVISION", "RouteCode='" + RouteCode + "' and CATEGORY='" + CategoryCode + "' and DLYDATE='" + DateTime.Parse(fromDate).ToString("yyyy/MM/dd") + "'");
                //                        }
                //                        else
                //                        {

                //                        }
                //                    }

                //                }


                //                WriteMessage("\r\n");
                //                WriteMessage("<<< Monthly Route Target >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);

                //                #endregion
                //#region Daily Target Route
                //dt = new DataTable();
                //string selectTarget = string.Format("select ROUTECODE,QUANTITY,REVENUE,BRANDCODE,CATEGORY,(convert(datetime, str(year(getdate()))+'/'+str(month(getdate()))+'/'+str(day(getdate()))+' '+'00:00:00',101)) as fromDate,convert(datetime, str(year(getdate()))+'/'+str(month(getdate()))+'/'+str(day(getdate()))+' '+'23:59:59',101) as ToDate from SALESTARGETDIVISION where (DLYDATE >= convert(datetime, ('{0} 00:00:00'),101) and DLYDATE < dateadd(day,1, convert(datetime, ('{0} 00:00:00'),101))) and CHANGEFLG='Y'", DateTime.Today.ToString("yyyy/MM/dd"));
                //qry = new InCubeQuery(selectTarget, dbERP);
                //err = qry.Execute();
                //dt = qry.GetDataTable();

                // TOTALUPDATED = 0;
                // TOTALINSERTED = 0;
                //ClearProgress();
                //SetProgressMaxdt.Rows.Count;
                //foreach (DataRow dr in dt.Rows)
                //{

                //    IntegrationForm.progressBar1.Value++;
                //    IntegrationForm.lblProgress.Text = "Updating RouteTarget" + " " + IntegrationForm.progressBar1.Value + " / " + IntegrationForm.progressBar1.Maximum;
                //    ;

                //    string AcheivmentID = "1";
                //    string RouteCode = dr["ROUTECODE"].ToString().Trim();
                //    string Quantity = dr["QUANTITY"].ToString().Trim();
                //    string Amount = dr["REVENUE"].ToString().Trim();
                //    string value = string.Empty;
                //    if (!Amount.Equals(string.Empty))
                //    {
                //        if (decimal.Parse(Amount) > 0)
                //        {
                //            AcheivmentID = "3";
                //            value = Amount;
                //        }
                //        else
                //        {
                //            AcheivmentID = "1";
                //            value = Quantity;
                //        }
                //    }


                //    string BrandCode = dr["BRANDCODE"].ToString().Trim();
                //    string CategoryCode = dr["CATEGORY"].ToString().Trim();
                //    string fromDate = DateTime.Parse(dr["fromDate"].ToString().Trim()).ToString("yyyy/MM/dd HH:mm:ss");
                //    string ToDate = DateTime.Parse(dr["ToDate"].ToString().Trim()).ToString("yyyy/MM/dd HH:mm:ss");

                //    string RouteID = GetFieldValue("RouteLanguage", "RouteID", "Description like '" + RouteCode + "%'", db_vms);
                //    if (RouteID.Trim().Equals(string.Empty))
                //    {
                //        continue;
                //    }

                //    string selectItems = string.Format("select ItemID from Item where Origin='" + BrandCode + "' and ItemCategoryID in (select ItemCategoryID from ItemCategory where ItemCategoryCode='" + CategoryCode + "')");
                //    qry = new InCubeQuery(selectItems, db_vms);
                //    err = qry.Execute();
                //    tbl = new DataTable();
                //    tbl = qry.GetDataTable();
                //    string targetID = string.Empty;
                //    foreach (DataRow row in tbl.Rows)
                //    {
                //        string existTarget = GetFieldValue("AchievementTargetRoute", "max(TargetID)", "AchievementID=" + AcheivmentID + " and RouteID=" + RouteID + " and ItemID=" + row["ItemID"].ToString().Trim() + "", db_vms);
                //        if (!existTarget.Trim().Equals(string.Empty))
                //        {
                //            targetID = (int.Parse(existTarget.Trim()) + 1).ToString();
                //        }
                //        else
                //        {
                //            targetID = "0";
                //        }
                //        string itemid = row["ItemID"].ToString().Trim();
                //        QueryBuilderObject.SetField("TargetID", targetID);
                //        QueryBuilderObject.SetField("AchievementID", AcheivmentID);
                //        QueryBuilderObject.SetField("RouteID", RouteID);
                //        QueryBuilderObject.SetField("Value", value);
                //        QueryBuilderObject.SetField("ItemID", row["ItemID"].ToString().Trim());
                //        QueryBuilderObject.SetField("FromDate", "'" + DateTime.Parse(fromDate).ToString("yyyy/MM/dd") + "'");
                //        QueryBuilderObject.SetField("ToDate", "'" + DateTime.Parse(ToDate).ToString("yyyy/MM/dd") + "'");
                //        err = QueryBuilderObject.InsertQueryString("AchievementTargetRoute", db_vms);
                //        TOTALINSERTED++;
                //        if (err == InCubeErrors.Success)
                //        {
                //            err = UpdateFlag("SALESTARGETDIVISION", "RouteCode='" + RouteCode + "' and CATEGORY='" + CategoryCode + "' and DLYDATE='" + DateTime.Parse(fromDate).ToString("yyyy/MM/dd") + "'");
                //        }
                //        else
                //        {

                //        }
                //    }

                //}


                //WriteMessage("\r\n");
                //WriteMessage("<<< Route Target >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
                #endregion
            }
            catch
            {

            }
        }
        public override void UpdatePOSM()
        {
            try
            {
                InCubeQuery qry;
                DataTable dt = new DataTable();
                string getPOSM = string.Format("select * from POSMItemMasterView");
                qry = new InCubeQuery(getPOSM, dbERP);
                err = qry.Execute();
                dt = qry.GetDataTable();
                foreach (DataRow dr in dt.Rows)
                {
                    string POSMName = dr["ItName"].ToString().Trim();
                    if (POSMName.Equals(string.Empty))
                    {
                        continue;
                    }
                    string existPOSM = GetFieldValue("AccessPointLanguage", "AccessPointID", "replace(ltrim(rtrim(lower(Description))),' ','')='" + POSMName.ToLower().Trim().Replace(" ", "") + "'", db_vms);
                    string POSMID = string.Empty;
                    if (existPOSM.Trim().Equals(string.Empty))
                    {
                        POSMID = GetFieldValue("AccessPoint", "isnull(max(AccessPointID),0)+1", db_vms);
                        QueryBuilderObject.SetField("AccessPointID", POSMID);
                        err = QueryBuilderObject.InsertQueryString("AccessPoint", db_vms);

                        QueryBuilderObject.SetField("AccessPointID", POSMID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + POSMName + "'");
                        err = QueryBuilderObject.InsertQueryString("AccessPointLanguage", db_vms);

                        QueryBuilderObject.SetField("AccessPointID", POSMID);
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "'" + POSMName + "'");
                        err = QueryBuilderObject.InsertQueryString("AccessPointLanguage", db_vms);

                    }
                    else
                    {
                        QueryBuilderObject.SetField("Description", "'" + POSMName + "'");
                        err = QueryBuilderObject.UpdateQueryString("AccessPointLanguage", "AccessPointID=" + existPOSM.Trim() + "", db_vms);
                    }
                }
            }
            catch
            {

            }
        }
        public override void SendOrders()
        {
            try
            {
                InCubeQuery qry;
                DataTable dt;
                string sp = string.Empty;
                if (Filters.EmployeeID != -1)
                {
                    sp = " AND SO.EmployeeID = " + Filters.EmployeeID;
                }

                string getOrders = string.Format(@"SELECT  CO.Barcode CUSTOMERCODE, SO.OrderID, SO.DesiredDeliveryDate, SO.OrderDate VISITDATE, SO.Synchronized, 
 (CASE WHEN SO.OrderStatusID IN( 9,11,16) THEN 1 ELSE 0 END )IsDelivered, 'NOTE1'Note, (CASE WHEN SO.OrderStatusID IN( 13,12) THEN 1 ELSE 0 END )IsVoided, SO.ConfirmationSignature, E.EmployeeCode, 
 SO.OrderStatusID, SO.Downloaded, (CASE WHEN SO.OrderStatusID IN(10) THEN 1 ELSE 0 END )Deleted, SO.LPO, SO.NetTotal, 
 SO.PromotedDiscount, SO.WarehouseTransactionID,SO.DivisionID,SO.RouteHistoryID,SO.OrderTypeID,SO.Discount,SO.SourceOrderID
 ,DIV.CompanyCode,DIV.OrderDivisionID,DIV.SAPDivision
FROM SALESORDER SO INNER JOIN
 dbo.CustomerOutlet CO ON SO.CustomerID = CO.CustomerID AND SO.OutletID = CO.OutletID INNER JOIN
 dbo.Employee E ON SO.EmployeeID = E.EmployeeID
 INNER JOIN SAPDivisions DIV ON DIV.DIVISIONID=SO.DIVISIONID
WHERE SO.Synchronized = 0 {0}
", sp);
                qry = new InCubeQuery(getOrders, db_vms);
                err = qry.Execute();
                dt = qry.GetDataTable();
                WriteExceptions(getOrders, "Getting orders", false);

                DataTable tbl = new DataTable();
                foreach (DataRow dr in dt.Rows)
                {
                    string OrderID = string.Empty;

                    OrderID = dr["OrderID"].ToString();

                    string insertOrder = string.Format(@"INSERT INTO SalesOrder 
(CustomerCode
,OrderID
,DesiredDeliveryDate
,VisitDate
,Synchronized
,IsDelivered
,Note
,IsVoided
,ConfirmationSignature
,EmployeeCode
,OrderStatusID
,Downloaded
,Deleted
,LPO
,NetTotal
,PromotedDiscount
,WarehouseTransactionID
,DivisionID
,RouteHistoryID
,OrderTypeID
,Discount
,SourceOrderID)

VALUES ('{0}','{1}',convert(datetime,'{2}',101),convert(datetime,'{3}',101),'{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}','{18}','{19}','{20}','{21}')",
 dr["CustomerCode"].ToString(),
 dr["OrderID"].ToString(),
 DateTime.Parse(dr["DesiredDeliveryDate"].ToString()).ToString("yyyy/MM/dd"),
 DateTime.Parse(dr["VisitDate"].ToString()).ToString("yyyy/MM/dd"),
 dr["Synchronized"].ToString(),
 dr["IsDelivered"].ToString(),
 dr["Note"].ToString(),
 dr["IsVoided"].ToString(),
 dr["ConfirmationSignature"].ToString(),
 dr["EmployeeCode"].ToString(),
 dr["OrderStatusID"].ToString(),
 dr["Downloaded"].ToString(),
 dr["Deleted"].ToString(),
 dr["LPO"].ToString(),
 dr["NetTotal"].ToString(),
 dr["PromotedDiscount"].ToString(),
 dr["WarehouseTransactionID"].ToString(),
 dr["OrderDivisionID"].ToString(),
 dr["RouteHistoryID"].ToString(),
 dr["OrderTypeID"].ToString(),
 dr["Discount"].ToString(),
 dr["SourceOrderID"].ToString().Trim());

                    qry = new InCubeQuery(insertOrder, dbERP);
                    WriteExceptions(insertOrder, "Inserting header for " + OrderID, false);
                    err = qry.ExecuteNonQuery();
                    if (err == InCubeErrors.Success)
                    {
                        string getDetails = string.Format(@"select SOD.OrderID, dbo.Item.ItemCode,isnull( CAST(dbo.Pack.Quantity AS decimal(9, 0)) ,0)AS PackQuatity, isnull( SOD.Quantity,0) Quantity , 
                      isnull(SOD.Price,0)Price, isnull(SOD.Tax,0)Tax, isnull(SOD.Discount,0)Discount, isnull(SOD.ActualDeliveredQuantity,0)ActualDeliveredQuantity, 
                      isnull(SOD.ActualPrice,0)ActualPrice, isnull(SOD.ActualTax,0)ActualTax, isnull(SOD.ActualDiscount,0) ActualDiscountAmount, isnull(SOD.Discount,0) DiscountAmount, 
                      isnull(SOD.DiscountTypeID,0) DiscountType,isnull(CO.Barcode,'') CustomerCode,isnull(PTL.Description,'') UOM, isnull(dbo.Division.DivisionCode,'') CompanyID,isnull(SOD.SalesTransactionTypeID,0)SalesTransactionTypeID,
					  isnull(SOD.ReturnReason,0)ReturnReason,isnull(SOD.PackStatusID,0)PackStatusID,SOD.ExpiryDate,SOD.FINALDISCOUNT,
                      ISNULL(PD.Price,0) * SOD.Quantity ExciseTax, isnull(SOD.BaseDiscount,0) BaseDiscount
                      FROM SalesOrderDetail SOD 
                      INNER JOIN Pack ON SOD.PackID = dbo.Pack.PackID INNER JOIN
					  PACKTYPELANGUAGE PTL ON dbo.Pack.PACKTYPEID=PTL.PACKTYPEID AND PTL.LANGUAGEID=1 INNER JOIN 
                      dbo.Item ON dbo.Pack.ItemID = dbo.Item.ItemID INNER JOIN
                      dbo.Division ON SOD.DivisionID = dbo.Division.DivisionID 
					  INNER JOIN CUSTOMEROUTLET CO ON SOD.CUSTOMERID=CO.CUSTOMERID AND SOD.OUTLETID=CO.OUTLETID
					  LEFT JOIN PriceDefinition PD ON PD.PacKID = SOD.PackID AND PD.PriceListID = 75
					  WHERE SOD.OrderID='{0}'
					  ", OrderID);
                        WriteExceptions(getDetails, "Getting details for " + OrderID, false);
                        qry = new InCubeQuery(getDetails, db_vms);
                        err = qry.Execute();
                        tbl = qry.GetDataTable();
                        //for (int i = 0; i < tbl.Rows.Count; i++)
                        //{
                        //    for (int j = 0; j < tbl.Rows[i].ItemArray.Length; j++)
                        //    {
                        //        if (tbl.Rows[i][j].ToString().Trim().Equals(string.Empty))
                        //        {
                        //            try
                        //            {
                        //                tbl.Rows[i][j] = "0";
                        //            }
                        //            catch (Exception ex)
                        //            { 

                        //            }
                        //        }
                        //    }
                        //}
                        foreach (DataRow drow in tbl.Rows)
                        {

                            string DOrderID = string.Empty;
                            string ItemCode = string.Empty;
                            string PackQuatity = string.Empty;
                            string Quantity = string.Empty;
                            string Price = string.Empty;
                            string Tax = string.Empty;
                            string DDiscount = string.Empty;
                            string BaseDiscount = string.Empty;
                            string ActualDeliveredQuantity = string.Empty;
                            string ActualPrice = string.Empty;
                            string AcutalTax = string.Empty;
                            string ActualDiscountAmount = string.Empty;
                            string DiscountAmount = string.Empty;
                            string DiscountType = string.Empty;
                            string DCustomerCode = string.Empty;
                            string UOM = string.Empty;
                            string CompanyID = string.Empty;
                            string SalesTransactionTypeID = string.Empty;
                            string ReturnReason = string.Empty;
                            string PackStatusID = string.Empty;
                            string ExpiryDate = string.Empty;
                            string ExciseTax = string.Empty;

                            DOrderID = drow["OrderID"].ToString();
                            ItemCode = drow["ItemCode"].ToString();
                            PackQuatity = drow["PackQuatity"].ToString();
                            Quantity = drow["Quantity"].ToString();
                            Price = drow["Price"].ToString();
                            Tax = drow["Tax"].ToString();
                            ExciseTax = drow["ExciseTax"].ToString();
                            DDiscount = drow["DiscountAmount"].ToString();
                            BaseDiscount = drow["BaseDiscount"].ToString();
                            ActualDeliveredQuantity = drow["ActualDeliveredQuantity"].ToString();
                            ActualPrice = drow["ActualPrice"].ToString();
                            AcutalTax = drow["ActualTax"].ToString();
                            ActualDiscountAmount = drow["ActualDiscountAmount"].ToString();
                            DiscountAmount = drow["FINALDISCOUNT"].ToString();
                            DiscountType = drow["DiscountType"].ToString();
                            DCustomerCode = drow["CustomerCode"].ToString();
                            UOM = drow["UOM"].ToString();
                            CompanyID = drow["CompanyID"].ToString();
                            SalesTransactionTypeID = drow["SalesTransactionTypeID"].ToString();
                            ReturnReason = drow["ReturnReason"].ToString();
                            PackStatusID = drow["PackStatusID"].ToString();
                            if (!drow["ExpiryDate"].ToString().Trim().Equals(string.Empty))
                                ExpiryDate = DateTime.Parse(drow["ExpiryDate"].ToString()).ToString("yyyy/MM/dd");

                            //QueryBuilderObject.SetField("OrderID", "'"+drow[0].ToString()+"'");
                            //QueryBuilderObject.SetField("ItemCode", "'" + drow[1].ToString()+"'");
                            //QueryBuilderObject.SetField("PackQuatity", "'" + drow[2].ToString()+"'");
                            //QueryBuilderObject.SetField("Quantity", "'" + drow[3].ToString()+"'");
                            //QueryBuilderObject.SetField("Price", "'" + drow[4].ToString()+"'");
                            //QueryBuilderObject.SetField("Tax", "'" + drow[5].ToString()+"'");
                            //QueryBuilderObject.SetField("Discount", "'" + drow[6].ToString()+"'");
                            //QueryBuilderObject.SetField("ActualDeliveredQuantity", "'" + drow[7].ToString()+"'");
                            //QueryBuilderObject.SetField("ActualPrice", "'" + drow[8].ToString()+"'");
                            //QueryBuilderObject.SetField("AcutalTax", "'" + drow[9].ToString()+"'");
                            //QueryBuilderObject.SetField("ActualDiscountAmount", "'" + drow[10].ToString()+"'");
                            //QueryBuilderObject.SetField("DiscountAmount", "'" + drow[11].ToString()+"'");
                            //QueryBuilderObject.SetField("DiscountType", "'" + drow[12].ToString()+"'");
                            //QueryBuilderObject.SetField("CustomerCode", "'" + dr[0].ToString() + "'");
                            //QueryBuilderObject.SetField("CompanyID", "'" + drow[13].ToString() + "'");
                            //QueryBuilderObject.SetField("UOM", "'" + drow[14].ToString() + "'");
                            //err=QueryBuilderObject.InsertQueryString("SalesOrderDetail", dbERP);

                            string insertDetail = string.Format(@"insert into SalesOrderDetail(OrderID
,ItemCode
,PackQuatity
,Quantity
,Price
,Tax
,Discount
,ActualDeliveredQuantity
,ActualPrice
,AcutalTax
,ActualDiscountAmount
,DiscountAmount
,DiscountType
,CustomerCode
,UOM
,CompanyID
,SalesTransactionTypeID
,ReturnReason
,PackStatusID
,ExpiryDate
,ExcisePrice
,ExciseAmount
,BaseDiscount)
                                                            values('{0}','{1}',{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},'{13}','{14}','{15}',{16},{17},{18},'{19}',{20},{21},{22})",
                                                              DOrderID
, ItemCode
, PackQuatity
, Quantity
, Price
, Tax
, DDiscount
, ActualDeliveredQuantity
, ActualPrice
, AcutalTax
, ActualDiscountAmount
, DiscountAmount
, DiscountType
, DCustomerCode
, UOM
, CompanyID
, SalesTransactionTypeID
, ReturnReason
, PackStatusID
, ExpiryDate
, decimal.Parse(ExciseTax) / decimal.Parse(Quantity)
, ExciseTax
, BaseDiscount);
                            qry = new InCubeQuery(insertDetail, dbERP);
                            WriteExceptions(insertDetail, "Inserting details for " + DOrderID + " and item " + ItemCode, false);
                            err = qry.ExecuteNonQuery();


                        }
                        if (err == InCubeErrors.Success)
                        {

                            string update = string.Format("update SalesOrder set synchronized=1 where OrderID='{0}' ", dr["OrderID"].ToString().Trim());
                            qry = new InCubeQuery(update, db_vms);
                            err = qry.ExecuteNonQuery();
                        }
                    }


                }
            }
            catch (Exception ex)
            {
                WriteExceptions(ex.Message, "Exception", false);
            }
        }
        public override void UpdateOrders()
        {
            //I STOPPED BECAUSE WHEN THE ERP RECEIVES THE SALES ORDER , THEN IT SHOULD RETURN STOCK . RIGHT ???
            InCubeQuery qry;
            DataTable dt;
            string getOrders = string.Format(@"select CustomerCode,OrderID,DesiredDeliveryDate,VisitDate,Synchronized,IsDelivered,Note,IsVoided,ConfirmationSignature,EmployeeCode,OrderStatusID,Downloaded,Deleted,LPO,NetTotal,PromotedDiscount,WarehouseTransactionID
from SalesOrder where orderStatusID<>1");
            qry = new InCubeQuery(getOrders, dbERP);
            err = qry.Execute();
            dt = new DataTable();
            dt = qry.GetDataTable();
            DataTable tbl = new DataTable();
            foreach (DataRow dr in dt.Rows)
            {
                string customerID = GetFieldValue("customerOutlet", "CustomerID", "CustomerCode='" + dr["CustomerCode"].ToString() + "'", db_vms);
                if (customerID.Trim() == string.Empty) continue;
                string outletID = GetFieldValue("customerOutlet", "OutletID", "CustomerID=" + customerID + "", db_vms);
                if (outletID.Trim() == string.Empty) continue;
                QueryBuilderObject.SetField("OrderStatusID", dr["OrderStatusID"].ToString().Trim());
                err = QueryBuilderObject.UpdateQueryString("SalesOrder", "customerID=" + customerID + " and OutletID=" + outletID + " and OrderID='" + dr["OrderID"].ToString().Trim() + "' and OrderStatusID=1", db_vms);
                if (err == InCubeErrors.Success)
                {
                    string deleteDetail = string.Format("delete from SalesOrderDetail where OrderID='{0}'", dr["OrderID"].ToString().Trim());
                    qry = new InCubeQuery(deleteDetail, db_vms);
                    err = qry.ExecuteNonQuery();
                    if (err == InCubeErrors.Success)
                    {
                        string selectDetail = string.Format("select OrderID,ItemCode,PackQuatity,Quantity,Price,Tax,Discount,DiscountType,CustomerCode from SalesOrderDetail where OrderID='{0}'", dr["OrderID"].ToString().Trim());
                        qry = new InCubeQuery(selectDetail, dbERP);
                        err = qry.Execute();
                        tbl = qry.GetDataTable();
                        foreach (DataRow row in tbl.Rows)
                        {
                            string PackID = GetFieldValue("Pack", "PackID", "ItemID in (select ItemID from Item where ItemCode='" + row["ItemCode"].ToString().Trim() + "') and Quantity=" + row["PackQuantity"].ToString().Trim() + "", dbERP);
                            if (!PackID.Trim().Equals(string.Empty))
                            {
                                QueryBuilderObject.SetField("OrderID", dr["OrderID"].ToString().Trim());
                                QueryBuilderObject.SetField("PackID", PackID);
                                QueryBuilderObject.SetField("Quantity", row["Quantity"].ToString().Trim());
                                QueryBuilderObject.SetField("Price", row["Price"].ToString().Trim());
                                QueryBuilderObject.SetField("Tax", row["Tax"].ToString().Trim());
                                QueryBuilderObject.SetField("Discount", row["Discount"].ToString().Trim());
                                QueryBuilderObject.SetField("DiscountType", row["DiscountType"].ToString().Trim());
                                QueryBuilderObject.SetField("CustomerID", customerID.Trim());
                                QueryBuilderObject.SetField("OutletID", outletID.Trim());
                                QueryBuilderObject.SetField("SalesTransactionTypeID", "1");
                                err = QueryBuilderObject.InsertQueryString("SalesOrderDetail", db_vms);
                            }
                        }

                    }
                }


            }

        }
        public override void UpdatePrice()
        {
            int TOTALUPDATED = 0;
            object field = new object();
            InCubeQuery qry = new InCubeQuery(db_vms, "sp_GetExcisePL");
            qry.ExecuteStoredProcedure();
            UpdatePriceFromSAP();
            //UpdatePriceList(ref TOTALUPDATED);

            qry = new InCubeQuery(db_vms, @"UPDATE PD SET Tax = ISNULL(P.Weight,0)
FROM PriceDefinition PD
INNER JOIN PriceList PL ON PL.PriceListID = PD.PriceListID
INNER JOIN Pack P ON P.PackID = PD.PackID
WHERE PL.PriceListTypeID = 1");
            qry.ExecuteNonQuery();

            WriteMessage("\r\n");
            WriteMessage("<<< PRICE >>> Total Updated = " + TOTALUPDATED);
        }
        private void UpdatePriceFromSAP()
        {
            try
            {
                string CUST_CODE = string.Empty;

                string ITEM_CODE = string.Empty;
                string LIST_NAME = string.Empty;
                string SALES_GRP = string.Empty;
                string UOM = string.Empty;
                string PRICE = string.Empty;
                decimal TAX = 0;
                string CR_DATE = string.Empty;
                string MOD_DATE = string.Empty;
                string PRICEKEY = string.Empty;
                string HIENR = string.Empty;
                string LGORT = string.Empty;
                string LOEVM_KO = string.Empty;
                object field = null;
                List<string> appliedList = new List<string>();
                InCubeQuery priceQry = new InCubeQuery("SELECT * FROM CustomerPriceView where CHANGEFLG='Y' or CHANGEFLG is null", dbERP);
                err = priceQry.Execute();
                DataTable TBL = priceQry.GetDataTable();


                InCubeQuery QRY;
                DataTable INNER = new DataTable();
                string MiscOrgID = "1";


                #region GROUP PRICE
                foreach (DataRow dr in TBL.Select("CustomGroup is not null and CustomGroup<>'' and CustomerCode='' and CustomerGroup=''"))
                {
                    CUST_CODE = dr["CustomerCode"].ToString().Trim();
                    ITEM_CODE = dr["Material"].ToString().Trim();
                    SALES_GRP = dr["CustomGroup"].ToString().Trim();
                    UOM = dr["UOM"].ToString().Trim();
                    PRICE = dr["PRICE"].ToString().Trim();
                    CR_DATE = dr["ValidityFrom"].ToString().Trim();
                    MOD_DATE = dr["ValidityTo"].ToString().Trim();
                    HIENR = dr["CustomerGroup"].ToString().Trim();
                    PRICEKEY = dr["PriceKey"].ToString().Trim();
                    LIST_NAME = PRICEKEY + "-" + SALES_GRP + "-" + HIENR + "-" + CUST_CODE;
                    if (!MOD_DATE.Equals(string.Empty))
                        MOD_DATE = MOD_DATE.Replace("9999", DateTime.Today.Year.ToString());
                    DateTime PriceCreatDate = DateTime.Parse(CR_DATE);
                    DateTime PriceEndDate = DateTime.Parse(MOD_DATE);
                    string DivisionCode = dr["CompanyID"].ToString().Trim() + dr["SAPDivision"].ToString().Trim();
                    string PACKID = GetFieldValue("Pack", "PACKID", @"ItemID in (select ItemID from Item where ItemCode='" + ITEM_CODE + "'  and PackDefinition='" + DivisionCode + "') and PackTypeID in (select PackTypeID from PackTypeLanguage where Description ='" + UOM + "')", db_vms).Trim();
                    if (PACKID.Equals(string.Empty)) continue;
                    string definitionBatch = string.Empty;
                    string PriceListTypeID = string.Empty;
                    PriceListTypeID = "1";

                    string checkPricelistExist = GetFieldValue("PriceList", "PriceListID", "PriceListCode='" + SALES_GRP + "' and PriceListTypeID=" + PriceListTypeID + "", db_vms).Trim();
                    string PriceListID = string.Empty;
                    if (!checkPricelistExist.Equals(string.Empty))
                    {
                        PriceListID = checkPricelistExist;// GetFieldValue("PriceListLanguage", "PriceListID", " Description = '" + PriceListName + "'", db_vms);// #####
                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");//*********
                        QueryBuilderObject.SetField("Priority", "3");
                        err = QueryBuilderObject.UpdateQueryString("PriceList", "PriceListID=" + PriceListID + "", db_vms);

                        QueryBuilderObject.SetField("Description", "'" + LIST_NAME + "'");//#####
                        err = QueryBuilderObject.UpdateQueryString("PriceListLanguage", "PriceListID=" + PriceListID + "", db_vms);//#####
                    }
                    else
                    {
                        PriceListID = GetFieldValue("PriceList", "ISNULL(MAX(PriceListID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("PriceListCode", "'" + SALES_GRP + "'");
                        QueryBuilderObject.SetField("StartDate", "'" + DateTime.Now.Date.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("Priority", "3");
                        QueryBuilderObject.SetField("PriceListTypeID", PriceListTypeID);
                        QueryBuilderObject.SetField("OrganizationID", MiscOrgID);//TO BE ADDED TO THE NEW RELEASE INTEGRATION
                        err = QueryBuilderObject.InsertQueryString("PriceList", db_vms);

                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + LIST_NAME + "'");
                        err = QueryBuilderObject.InsertQueryString("PriceListLanguage", db_vms);
                    }
                    string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + UOM + "' AND LanguageID = 1", db_vms);
                    if (PackTypeID.Equals(string.Empty))
                    {
                        continue;
                    }

                    string PackQuantity = GetFieldValue("Pack", "Quantity", "ItemID in (select ItemID from Item where ItemCode='" + ITEM_CODE + "' and PackDefinition='" + DivisionCode + "')  AND PackTypeID = " + PackTypeID + "", db_vms);
                    InCubeQuery PackQuery = new InCubeQuery(db_vms, @"
SELECT     
Pack.PACKID, 
Pack.Quantity, 
Pack.PackTypeID, 
PackTypeLanguage.Description,
ISNULL(Pack.Weight,0)
FROM Pack INNER JOIN
Item ON Pack.ItemID = Item.ItemID INNER JOIN
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID
WHERE 
Item.ItemCode = '" + ITEM_CODE.Trim() + @"' AND Item.PackDefinition='" + DivisionCode + @"' and
PackTypeLanguage.LanguageID = 1");

                    PackQuery.Execute();
                    err = PackQuery.FindFirst();

                    while (err == InCubeErrors.Success)
                    {
                        decimal loopPrice = decimal.Parse(PRICE);

                        PackQuery.GetField(0, ref field);
                        PACKID = field.ToString();

                        PackQuery.GetField(1, ref field);
                        decimal ConversionFactor = decimal.Parse(field.ToString());

                        PackQuery.GetField(2, ref field);
                        string LoopPackTypeID = field.ToString().Trim();

                        PackQuery.GetField(4, ref field);
                        TAX = decimal.Parse(field.ToString());

                        if (LoopPackTypeID != PackTypeID)
                        {
                            loopPrice = Math.Round(ConversionFactor * (decimal.Parse(PRICE) / decimal.Parse(PackQuantity)), 3);
                        }

                        int PriceDefinitionID = 1;
                        string currentPrice = GetFieldValue("PriceDefinition", "PRICE", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID + " " + definitionBatch + "", db_vms);
                        if (currentPrice.Equals(string.Empty))
                        {
                            PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms));

                            QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID.ToString());
                            QueryBuilderObject.SetField("QuantityRangeID", "1");
                            QueryBuilderObject.SetField("PACKID", PACKID);
                            QueryBuilderObject.SetField("CurrencyID", "1");
                            QueryBuilderObject.SetField("Tax", TAX.ToString());
                            QueryBuilderObject.SetField("PRICE", loopPrice.ToString());
                            QueryBuilderObject.SetField("PriceListID", PriceListID);
                            //if (!BATCH.Equals(string.Empty))
                            //{
                            //    QueryBuilderObject.SetField("BatchNo", "'" + BATCH + "'");
                            //    QueryBuilderObject.SetField("ExpiryDate", "'" + DateTime.Parse(BEXP_DATE).ToString(DateFormat) + "'");
                            //}
                            err = QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);
                        }
                        else
                        {
                            PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "PriceDefinitionID", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID + " " + definitionBatch + "", db_vms));
                            if (!currentPrice.Equals(PRICE.ToString()))
                            {
                                QueryBuilderObject.SetField("PRICE", loopPrice.ToString());
                                //QueryBuilderObject.SetField("Tax", TAX.ToString());
                                QueryBuilderObject.UpdateQueryString("PriceDefinition", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID + " AND PriceDefinitionID = " + PriceDefinitionID + " " + definitionBatch + "", db_vms);
                            }
                        }
                        //THE FOLLOWING FUNCTION IS TO FILL THE PRICEENDDATE TABLE WHICH WILL LATER DELETE THE EXPIRED PRICES
                        PricePeriodControl(PriceDefinitionID.ToString(), PriceCreatDate, PriceEndDate);

                        string updateView = string.Format("update SAP_CustomerPrice_Table set CHANGEFLG='N' where customercode='{0}' and customgroup='{1}' and customergroup='{2}' and Material='{3}' and UOM='{4}'", CUST_CODE, SALES_GRP, HIENR, ITEM_CODE, UOM);
                        InCubeQuery updateCustQry = new InCubeQuery(dbERP, updateView);
                        err = updateCustQry.ExecuteNonQuery();

                        err = PackQuery.FindNext();

                    }
                    if (!appliedList.Contains(SALES_GRP)) { appliedList.Add(SALES_GRP); } else { continue; }
                    string GroupIDExist = GetFieldValue("CustomerGroup", "GroupID", "GroupCode='" + SALES_GRP + "'", db_vms).Trim();
                    string checkCHLPrc = GetFieldValue("GroupPrice", "GroupID", "GroupID=" + GroupIDExist + " and PriceListID=" + PriceListID + "", db_vms).Trim();
                    if (checkCHLPrc.Equals(string.Empty))
                    {
                        QueryBuilderObject.SetField("GroupID", GroupIDExist);
                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        err = QueryBuilderObject.InsertQueryString("GroupPrice", db_vms);
                    }

                }
                #endregion

                #region HIRARCHY PRICE
                foreach (DataRow dr in TBL.Select("CustomerGroup is not null and CustomerGroup<>'' and CustomGroup='' and CustomerCode=''   "))
                {
                    CUST_CODE = dr["CustomerCode"].ToString().Trim();
                    ITEM_CODE = dr["Material"].ToString().Trim();

                    SALES_GRP = dr["CustomGroup"].ToString().Trim();
                    UOM = dr["UOM"].ToString().Trim();
                    PRICE = dr["PRICE"].ToString().Trim();
                    //IS_DEFAULT = "";// dr["IS_DEFAULT"].ToString().Trim();
                    CR_DATE = dr["ValidityFrom"].ToString().Trim();
                    MOD_DATE = dr["ValidityTo"].ToString().Trim();
                    //BATCH = dr["BATCH"].ToString().Trim();
                    //BEXP_DATE = dr["BEXP_DATE"].ToString().Trim();
                    //DIS_CHL = dr["DIS_CHL"].ToString().Trim();
                    HIENR = dr["CustomerGroup"].ToString().Trim();
                    //LGORT = dr["LGORT"].ToString().Trim();
                    //LOEVM_KO = dr["LOEVM_KO"].ToString().Trim();
                    PRICEKEY = dr["PriceKey"].ToString().Trim();
                    LIST_NAME = PRICEKEY + "-" + SALES_GRP + "-" + HIENR + "-" + CUST_CODE;// dr["LIST_NAME"].ToString().Trim();
                    if (!MOD_DATE.Equals(string.Empty))
                        MOD_DATE = MOD_DATE.Replace("9999", DateTime.Today.Year.ToString());
                    DateTime PriceCreatDate = DateTime.Parse(CR_DATE);
                    DateTime PriceEndDate = DateTime.Parse(MOD_DATE);
                    string DivisionCode = dr["CompanyID"].ToString().Trim() + dr["SAPDivision"].ToString().Trim();
                    string PACKID = GetFieldValue("Pack", "PACKID", @"ItemID in (select ItemID from Item where ItemCode='" + ITEM_CODE + "'  and PackDefinition='" + DivisionCode + "') and PackTypeID in (select PackTypeID from PackTypeLanguage where Description ='" + UOM + "')", db_vms).Trim();
                    if (PACKID.Equals(string.Empty)) continue;
                    //TAX = decimal.Parse(GetFieldValue("Pack", "ISNULL(Weight,0)", "PackID = " + PACKID, db_vms));

                    string definitionBatch = string.Empty;
                    string PriceListTypeID = string.Empty;
                    PriceListTypeID = "1";
                    //if (!BATCH.Equals(string.Empty)) { PriceListTypeID = "4"; definitionBatch = "and BatchNo='" + BATCH + "'"; }
                    //if (BEXP_DATE.Equals(string.Empty)) { BEXP_DATE = "1990-01-01"; }

                    string checkPricelistExist = GetFieldValue("PriceList", "PriceListID", "PriceListCode='" + HIENR + "' and PriceListTypeID=" + PriceListTypeID + "", db_vms).Trim();
                    string PriceListID = string.Empty;
                    if (!checkPricelistExist.Equals(string.Empty))
                    {
                        PriceListID = checkPricelistExist;// GetFieldValue("PriceListLanguage", "PriceListID", " Description = '" + PriceListName + "'", db_vms);// #####
                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");//*********
                        QueryBuilderObject.SetField("Priority", "2");
                        err = QueryBuilderObject.UpdateQueryString("PriceList", "PriceListID=" + PriceListID + "", db_vms);

                        QueryBuilderObject.SetField("Description", "'" + LIST_NAME + "'");//#####
                        err = QueryBuilderObject.UpdateQueryString("PriceListLanguage", "PriceListID=" + PriceListID + "", db_vms);//#####
                    }
                    else
                    {
                        PriceListID = GetFieldValue("PriceList", "ISNULL(MAX(PriceListID),0) + 1", db_vms);
                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("PriceListCode", "'" + HIENR + "'");
                        QueryBuilderObject.SetField("StartDate", "'" + DateTime.Now.Date.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("Priority", "2");
                        QueryBuilderObject.SetField("PriceListTypeID", PriceListTypeID);
                        QueryBuilderObject.SetField("OrganizationID", MiscOrgID);//TO BE ADDED TO THE NEW RELEASE INTEGRATION
                        err = QueryBuilderObject.InsertQueryString("PriceList", db_vms);

                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + LIST_NAME + "'");
                        err = QueryBuilderObject.InsertQueryString("PriceListLanguage", db_vms);
                    }
                    string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + UOM + "' AND LanguageID = 1", db_vms);
                    if (PackTypeID.Equals(string.Empty))
                    {
                        continue;
                    }

                    string PackQuantity = GetFieldValue("Pack", "Quantity", "ItemID in (select ItemID from Item where ItemCode='" + ITEM_CODE + "' and PackDefinition='" + DivisionCode + "')  AND PackTypeID = " + PackTypeID + "", db_vms);
                    InCubeQuery PackQuery = new InCubeQuery(db_vms, @"
SELECT     
Pack.PACKID, 
Pack.Quantity, 
Pack.PackTypeID, 
PackTypeLanguage.Description,
ISNULL(Pack.Weight,0)
FROM Pack INNER JOIN
Item ON Pack.ItemID = Item.ItemID INNER JOIN
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID
WHERE 
Item.ItemCode = '" + ITEM_CODE.Trim() + @"' AND Item.PackDefinition='" + DivisionCode + @"' and
PackTypeLanguage.LanguageID = 1");


                    PackQuery.Execute();
                    err = PackQuery.FindFirst();

                    while (err == InCubeErrors.Success)
                    {
                        decimal loopPrice = decimal.Parse(PRICE);

                        PackQuery.GetField(0, ref field);
                        PACKID = field.ToString();

                        PackQuery.GetField(1, ref field);
                        decimal ConversionFactor = decimal.Parse(field.ToString());

                        PackQuery.GetField(2, ref field);
                        string LoopPackTypeID = field.ToString().Trim();

                        PackQuery.GetField(4, ref field);
                        TAX = decimal.Parse(field.ToString());

                        if (LoopPackTypeID != PackTypeID)
                        {
                            loopPrice = Math.Round(ConversionFactor * (decimal.Parse(PRICE) / decimal.Parse(PackQuantity)), 3);
                        }

                        int PriceDefinitionID = 1;

                        string currentPrice = GetFieldValue("PriceDefinition", "PRICE", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID + " " + definitionBatch + "", db_vms);
                        if (currentPrice.Equals(string.Empty))
                        {
                            PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms));

                            QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID.ToString());
                            QueryBuilderObject.SetField("QuantityRangeID", "1");
                            QueryBuilderObject.SetField("PACKID", PACKID);
                            QueryBuilderObject.SetField("CurrencyID", "1");
                            QueryBuilderObject.SetField("Tax", TAX.ToString());
                            QueryBuilderObject.SetField("PRICE", loopPrice.ToString());
                            QueryBuilderObject.SetField("PriceListID", PriceListID);
                            //if (!BATCH.Equals(string.Empty))
                            //{
                            //    QueryBuilderObject.SetField("BatchNo", "'" + BATCH + "'");
                            //    QueryBuilderObject.SetField("ExpiryDate", "'" + DateTime.Parse(BEXP_DATE).ToString(DateFormat) + "'");
                            //}
                            err = QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);
                        }
                        else
                        {
                            PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "PriceDefinitionID", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID + " " + definitionBatch + "", db_vms));
                            if (!currentPrice.Equals(PRICE.ToString()))
                            {
                                //QueryBuilderObject.SetField("Tax", TAX.ToString());
                                QueryBuilderObject.SetField("PRICE", loopPrice.ToString());
                                QueryBuilderObject.UpdateQueryString("PriceDefinition", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID + " AND PriceDefinitionID = " + PriceDefinitionID + " " + definitionBatch + "", db_vms);
                            }
                        }
                        //THE FOLLOWING FUNCTION IS TO FILL THE PRICEENDDATE TABLE WHICH WILL LATER DELETE THE EXPIRED PRICES
                        PricePeriodControl(PriceDefinitionID.ToString(), PriceCreatDate, PriceEndDate);

                        string updateView = string.Format("update SAP_CustomerPrice_Table set CHANGEFLG='N' where customercode='{0}' and customgroup='{1}' and customergroup='{2}' and Material='{3}' and UOM='{4}'", CUST_CODE, SALES_GRP, HIENR, ITEM_CODE, UOM);
                        InCubeQuery updateCustQry = new InCubeQuery(dbERP, updateView);
                        err = updateCustQry.ExecuteNonQuery();

                        err = PackQuery.FindNext();
                    }

                    if (!appliedList.Contains(HIENR)) { appliedList.Add(HIENR); } else { continue; }
                    string GroupIDExist = GetFieldValue("CustomerGroup", "GroupID", "GroupCode='" + HIENR + "'", db_vms).Trim();
                    string checkCHLPrc = GetFieldValue("GroupPrice", "GroupID", "GroupID=" + GroupIDExist + " and PriceListID=" + PriceListID + "", db_vms).Trim();
                    if (checkCHLPrc.Equals(string.Empty))
                    {
                        QueryBuilderObject.SetField("GroupID", GroupIDExist);
                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        err = QueryBuilderObject.InsertQueryString("GroupPrice", db_vms);
                    }

                }
                #endregion

                #region CUSTOMER PRICE
                foreach (DataRow dr in TBL.Select("CustomerCode is not null and CustomerCode<>''  "))
                {
                    CUST_CODE = dr["CustomerCode"].ToString().Trim();
                    ITEM_CODE = dr["Material"].ToString().Trim();

                    SALES_GRP = dr["CustomGroup"].ToString().Trim();
                    UOM = dr["UOM"].ToString().Trim();
                    PRICE = dr["PRICE"].ToString().Trim();
                    //IS_DEFAULT = "";// dr["IS_DEFAULT"].ToString().Trim();
                    CR_DATE = dr["ValidityFrom"].ToString().Trim();
                    MOD_DATE = dr["ValidityTo"].ToString().Trim();
                    //BATCH = dr["BATCH"].ToString().Trim();
                    //BEXP_DATE = dr["BEXP_DATE"].ToString().Trim();
                    //DIS_CHL = dr["DIS_CHL"].ToString().Trim();
                    HIENR = dr["CustomerGroup"].ToString().Trim();
                    //LGORT = dr["LGORT"].ToString().Trim();
                    //LOEVM_KO = dr["LOEVM_KO"].ToString().Trim();
                    PRICEKEY = dr["PriceKey"].ToString().Trim();
                    LIST_NAME = PRICEKEY + "-" + SALES_GRP + "-" + HIENR + "-" + CUST_CODE;// dr["LIST_NAME"].ToString().Trim();
                    if (!MOD_DATE.Equals(string.Empty))
                        MOD_DATE = MOD_DATE.Replace("9999", DateTime.Today.Year.ToString());
                    DateTime PriceCreatDate = DateTime.Parse(CR_DATE);
                    DateTime PriceEndDate = DateTime.Parse(MOD_DATE);
                    string DivisionCode = dr["CompanyID"].ToString().Trim() + dr["SAPDivision"].ToString().Trim();
                    string PACKID = GetFieldValue("Pack", "PACKID", @"ItemID in (select ItemID from Item where ItemCode='" + ITEM_CODE + "'  and PackDefinition='" + DivisionCode + "') and PackTypeID in (select PackTypeID from PackTypeLanguage where Description ='" + UOM + "')", db_vms).Trim();
                    if (PACKID.Equals(string.Empty)) continue;

                    string definitionBatch = string.Empty;
                    string PriceListTypeID = string.Empty;
                    PriceListTypeID = "1";
                    //if (!BATCH.Equals(string.Empty)) { PriceListTypeID = "4"; definitionBatch = "and BatchNo='" + BATCH + "'"; }
                    //if (BEXP_DATE.Equals(string.Empty)) { BEXP_DATE = "1990-01-01"; }


                    string checkPricelistExist = GetFieldValue("PriceList", "PriceListID", "PriceListCode='" + CUST_CODE + "' and PriceListTypeID=" + PriceListTypeID + "", db_vms).Trim();
                    string PriceListID = string.Empty;
                    if (!checkPricelistExist.Equals(string.Empty))
                    {
                        PriceListID = checkPricelistExist;// GetFieldValue("PriceListLanguage", "PriceListID", " Description = '" + PriceListName + "'", db_vms);// #####
                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");//*********
                        QueryBuilderObject.SetField("Priority", "1");
                        err = QueryBuilderObject.UpdateQueryString("PriceList", "PriceListID=" + PriceListID + "", db_vms);

                        QueryBuilderObject.SetField("Description", "'" + LIST_NAME + "'");//#####
                        err = QueryBuilderObject.UpdateQueryString("PriceListLanguage", "PriceListID=" + PriceListID + "", db_vms);//#####
                    }
                    else
                    {
                        PriceListID = GetFieldValue("PriceList", "ISNULL(MAX(PriceListID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("PriceListCode", "'" + CUST_CODE + "'");
                        QueryBuilderObject.SetField("StartDate", "'" + DateTime.Now.Date.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("Priority", "1");
                        QueryBuilderObject.SetField("PriceListTypeID", PriceListTypeID);
                        QueryBuilderObject.SetField("OrganizationID", MiscOrgID);//TO BE ADDED TO THE NEW RELEASE INTEGRATION
                        err = QueryBuilderObject.InsertQueryString("PriceList", db_vms);

                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + LIST_NAME + "'");
                        err = QueryBuilderObject.InsertQueryString("PriceListLanguage", db_vms);
                    }






                    string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + UOM + "' AND LanguageID = 1", db_vms);
                    if (PackTypeID.Equals(string.Empty))
                    {
                        continue;
                    }

                    string PackQuantity = GetFieldValue("Pack", "Quantity", "ItemID in (select ItemID from Item where ItemCode='" + ITEM_CODE + "' and PackDefinition='" + DivisionCode + "')  AND PackTypeID = " + PackTypeID + "", db_vms);
                    InCubeQuery PackQuery = new InCubeQuery(db_vms, @"
SELECT     
Pack.PACKID, 
Pack.Quantity, 
Pack.PackTypeID, 
PackTypeLanguage.Description,
ISNULL(Pack.Weight,0)
FROM Pack INNER JOIN
Item ON Pack.ItemID = Item.ItemID INNER JOIN
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID
WHERE 
Item.ItemCode = '" + ITEM_CODE.Trim() + @"' AND Item.PackDefinition='" + DivisionCode + @"' and
PackTypeLanguage.LanguageID = 1");


                    PackQuery.Execute();
                    err = PackQuery.FindFirst();

                    while (err == InCubeErrors.Success)
                    {
                        decimal loopPrice = decimal.Parse(PRICE);

                        PackQuery.GetField(0, ref field);
                        PACKID = field.ToString();

                        PackQuery.GetField(1, ref field);
                        decimal ConversionFactor = decimal.Parse(field.ToString());

                        PackQuery.GetField(2, ref field);
                        string LoopPackTypeID = field.ToString().Trim();

                        PackQuery.GetField(4, ref field);
                        TAX = decimal.Parse(field.ToString());

                        if (LoopPackTypeID != PackTypeID)
                        {
                            loopPrice = Math.Round(ConversionFactor * (decimal.Parse(PRICE) / decimal.Parse(PackQuantity)), 3);
                        }
                        int PriceDefinitionID = 1;
                        string currentPrice = GetFieldValue("PriceDefinition", "PRICE", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID + " " + definitionBatch + "", db_vms);
                        if (currentPrice.Equals(string.Empty))
                        {
                            PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms));

                            QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID.ToString());
                            QueryBuilderObject.SetField("QuantityRangeID", "1");
                            QueryBuilderObject.SetField("PACKID", PACKID);
                            QueryBuilderObject.SetField("CurrencyID", "1");
                            QueryBuilderObject.SetField("Tax", TAX.ToString());
                            QueryBuilderObject.SetField("PRICE", loopPrice.ToString());
                            QueryBuilderObject.SetField("PriceListID", PriceListID);
                            //if (!BATCH.Equals(string.Empty))
                            //{
                            //    QueryBuilderObject.SetField("BatchNo", "'" + BATCH + "'");
                            //    QueryBuilderObject.SetField("ExpiryDate", "'" + DateTime.Parse(BEXP_DATE).ToString(DateFormat) + "'");
                            //}
                            err = QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);
                        }
                        else
                        {
                            PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "PriceDefinitionID", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID + " " + definitionBatch + "", db_vms));
                            if (!currentPrice.Equals(PRICE.ToString()))
                            {
                                //QueryBuilderObject.SetField("Tax", TAX.ToString());
                                QueryBuilderObject.SetField("PRICE", loopPrice.ToString());
                                QueryBuilderObject.UpdateQueryString("PriceDefinition", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID + " AND PriceDefinitionID = " + PriceDefinitionID + " " + definitionBatch + "", db_vms);
                            }
                        }
                        //THE FOLLOWING FUNCTION IS TO FILL THE PRICEENDDATE TABLE WHICH WILL LATER DELETE THE EXPIRED PRICES
                        PricePeriodControl(PriceDefinitionID.ToString(), PriceCreatDate, PriceEndDate);

                        string updateView = string.Format("update SAP_CustomerPrice_Table set CHANGEFLG='N' where customercode='{0}' and customgroup='{1}' and customergroup='{2}' and Material='{3}' and UOM='{4}'", CUST_CODE, SALES_GRP, HIENR, ITEM_CODE, UOM);
                        InCubeQuery updateCustQry = new InCubeQuery(dbERP, updateView);
                        err = updateCustQry.ExecuteNonQuery();

                        err = PackQuery.FindNext();
                    }
                    string GetCustomers = string.Format(@"Select CO.CustomerID,CO.OutletID from CustomerOutlet CO 
WHERE CO.CUSTOMERCODE='{0}'
 ", CUST_CODE);
                    QRY = new InCubeQuery(GetCustomers, db_vms);
                    err = QRY.Execute();
                    INNER = QRY.GetDataTable();
                    if (CUST_CODE.ToLower().Equals("0000401441"))
                    {

                    }
                    foreach (DataRow DR in INNER.Rows)
                    {

                        string CustomerID = DR["CustomerID"].ToString().Trim();
                        string OutletID = DR["OutletID"].ToString().Trim();
                        //if (CustomerID.Equals("4528")) { }
                        //QRY = new InCubeQuery("DELETE FROM CUSTOMERPRICE WHERE CUSTOMERID=" + CustomerID + " AND OUTLETID=" + OutletID + " and priceListID in (select PriceListID from pricedefinition where packid=" + PACKID + ")", db_vms);
                        //err = QRY.ExecuteNonQuery();
                        QueryBuilderObject.SetField("CustomerID", CustomerID);
                        QueryBuilderObject.SetField("OutletID", OutletID);
                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        err = QueryBuilderObject.InsertQueryString("CustomerPrice", db_vms);

                    }
                }
                #endregion

                #region DEFAULT PRICE
                foreach (DataRow dr in TBL.Select("CustomGroup='' and CustomerCode='' and CustomerGroup=''"))
                {

                    CUST_CODE = dr["CustomerCode"].ToString().Trim();
                    ITEM_CODE = dr["Material"].ToString().Trim();

                    SALES_GRP = dr["CustomGroup"].ToString().Trim();
                    UOM = dr["UOM"].ToString().Trim();
                    PRICE = dr["PRICE"].ToString().Trim();
                    //IS_DEFAULT = "";// dr["IS_DEFAULT"].ToString().Trim();
                    CR_DATE = dr["ValidityFrom"].ToString().Trim();
                    MOD_DATE = dr["ValidityTo"].ToString().Trim();
                    //BATCH = dr["BATCH"].ToString().Trim();
                    //BEXP_DATE = dr["BEXP_DATE"].ToString().Trim();
                    //DIS_CHL = dr["DIS_CHL"].ToString().Trim();
                    HIENR = dr["CustomerGroup"].ToString().Trim();
                    //LGORT = dr["LGORT"].ToString().Trim();
                    //LOEVM_KO = dr["LOEVM_KO"].ToString().Trim();
                    PRICEKEY = dr["PriceKey"].ToString().Trim();
                    LIST_NAME = PRICEKEY + "-" + SALES_GRP + "-" + HIENR + "-" + CUST_CODE;// dr["LIST_NAME"].ToString().Trim();
                    if (!MOD_DATE.Equals(string.Empty))
                        MOD_DATE = MOD_DATE.Replace("9999", DateTime.Today.Year.ToString());
                    DateTime PriceCreatDate = DateTime.Parse(CR_DATE);
                    DateTime PriceEndDate = DateTime.Parse(MOD_DATE).AddYears(5);
                    string DivisionCode = dr["CompanyID"].ToString().Trim() + dr["SAPDivision"].ToString().Trim();
                    string PACKID = GetFieldValue("Pack", "PACKID", @"ItemID in (select ItemID from Item where ItemCode='" + ITEM_CODE + "' and PackDefinition='" + DivisionCode + "') and PackTypeID in (select PackTypeID from PackTypeLanguage where Description ='" + UOM + "')", db_vms).Trim();
                    if (PACKID.Equals(string.Empty)) continue;

                    string checkPricelistExist = GetFieldValue("PriceListLanguage", "PriceListID", "Description='" + LIST_NAME + "'", db_vms).Trim();
                    string PriceListID = string.Empty;
                    if (!checkPricelistExist.Equals(string.Empty))
                    {
                        PriceListID = checkPricelistExist;// GetFieldValue("PriceListLanguage", "PriceListID", " Description = '" + PriceListName + "'", db_vms);// #####
                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");//*********
                        err = QueryBuilderObject.UpdateQueryString("PriceList", "PriceListID=" + PriceListID + "", db_vms);

                        QueryBuilderObject.SetField("Description", "'" + LIST_NAME + "'");//#####
                        err = QueryBuilderObject.UpdateQueryString("PriceListLanguage", "PriceListID=" + PriceListID + "", db_vms);//#####
                    }
                    else
                    {
                        PriceListID = GetFieldValue("PriceList", "ISNULL(MAX(PriceListID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("PriceListCode", "'" + LIST_NAME + "'");
                        QueryBuilderObject.SetField("StartDate", "'" + DateTime.Now.Date.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("Priority", "5");
                        QueryBuilderObject.SetField("OrganizationID", MiscOrgID);//TO BE ADDED TO THE NEW RELEASE INTEGRATION
                        err = QueryBuilderObject.InsertQueryString("PriceList", db_vms);

                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + LIST_NAME + "'");
                        err = QueryBuilderObject.InsertQueryString("PriceListLanguage", db_vms);

                        QueryBuilderObject.SetField("KeyValue", PriceListID);
                        err = QueryBuilderObject.UpdateQueryString("Configuration", "KeyName = 'DefaultPriceListID' AND EmployeeID = -1", db_vms);
                    }






                    string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + UOM + "' AND LanguageID = 1", db_vms);
                    if (PackTypeID.Equals(string.Empty))
                    {
                        continue;
                    }

                    string PackQuantity = GetFieldValue("Pack", "Quantity", "ItemID in (select ItemID from Item where ItemCode='" + ITEM_CODE + "' and PackDefinition='" + DivisionCode + "')  AND PackTypeID = " + PackTypeID + "", db_vms);
                    InCubeQuery PackQuery = new InCubeQuery(db_vms, @"
SELECT     
Pack.PACKID, 
Pack.Quantity, 
Pack.PackTypeID, 
PackTypeLanguage.Description,
ISNULL(Pack.Weight,0)
FROM Pack INNER JOIN
Item ON Pack.ItemID = Item.ItemID INNER JOIN
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID
WHERE 
Item.ItemCode = '" + ITEM_CODE.Trim() + @"' AND Item.PackDefinition='" + DivisionCode + @"' and
PackTypeLanguage.LanguageID = 1");

                    PackQuery.Execute();
                    err = PackQuery.FindFirst();

                    while (err == InCubeErrors.Success)
                    {
                        decimal loopPrice = decimal.Parse(PRICE);

                        PackQuery.GetField(0, ref field);
                        PACKID = field.ToString();

                        PackQuery.GetField(1, ref field);
                        decimal ConversionFactor = decimal.Parse(field.ToString());

                        PackQuery.GetField(2, ref field);
                        string LoopPackTypeID = field.ToString().Trim();

                        PackQuery.GetField(4, ref field);
                        TAX = decimal.Parse(field.ToString());

                        if (LoopPackTypeID != PackTypeID)
                        {
                            loopPrice = Math.Round(ConversionFactor * (decimal.Parse(PRICE) / decimal.Parse(PackQuantity)), 6);
                        }
                        int PriceDefinitionID = 1;
                        string currentPrice = GetFieldValue("PriceDefinition", "PRICE", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID, db_vms);
                        if (currentPrice.Equals(string.Empty))
                        {
                            PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms));

                            QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID.ToString());
                            QueryBuilderObject.SetField("QuantityRangeID", "1");
                            QueryBuilderObject.SetField("PACKID", PACKID);
                            QueryBuilderObject.SetField("CurrencyID", "1");
                            QueryBuilderObject.SetField("Tax", TAX.ToString());
                            QueryBuilderObject.SetField("PRICE", loopPrice.ToString());
                            QueryBuilderObject.SetField("PriceListID", PriceListID);
                            err = QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);
                        }
                        else
                        {
                            PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "PriceDefinitionID", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID, db_vms));
                            if (!currentPrice.Equals(PRICE.ToString()))
                            {
                                QueryBuilderObject.SetField("PRICE", loopPrice.ToString());
                                //QueryBuilderObject.SetField("Tax", TAX.ToString());
                                QueryBuilderObject.UpdateQueryString("PriceDefinition", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID + " AND PriceDefinitionID = " + PriceDefinitionID, db_vms);
                            }
                        }
                        //THE FOLLOWING FUNCTION IS TO FILL THE PRICEENDDATE TABLE WHICH WILL LATER DELETE THE EXPIRED PRICES
                        PricePeriodControl(PriceDefinitionID.ToString(), PriceCreatDate, PriceEndDate);

                        string updateView = string.Format("update SAP_CustomerPrice_Table set CHANGEFLG='N' where customercode='{0}' and customgroup='{1}' and customergroup='{2}' and Material='{3}' and UOM='{4}'", CUST_CODE, SALES_GRP, HIENR, ITEM_CODE, UOM);
                        InCubeQuery updateCustQry = new InCubeQuery(dbERP, updateView);
                        err = updateCustQry.ExecuteNonQuery();

                        err = PackQuery.FindNext();
                    }

                }
                #endregion

                #region COMMENTED
                DataTable DT = new DataTable();
                string SelectGroup = @"select P.PackID,P.ItemID,PD.Price,P.Quantity,PD.PriceListID,ISNULL(P.Weight,0) Tax from Pack P inner join PriceDefinition PD on PD.PacKID=P.PackID and P.Quantity>1";
                InCubeQuery GroupQuery = new InCubeQuery(db_vms, SelectGroup);
                err = GroupQuery.Execute();
                DT = GroupQuery.GetDataTable();
                ClearProgress();
                SetProgressMax(DT.Rows.Count);
                foreach (DataRow row in DT.Rows)
                {
                    ReportProgress("Updating Base UOM Prices");
                    string ItemID = row["ItemID"].ToString().Trim();
                    string PackID = row["PackID"].ToString().Trim();
                    string Price = row["Price"].ToString().Trim();
                    string Quantity = row["Quantity"].ToString().Trim();
                    TAX = decimal.Parse(row["Tax"].ToString());
                    string PriceListID = row["PriceListID"].ToString().Trim();
                    if (decimal.Parse(Quantity) == 0) continue;
                    string basePack = GetFieldValue("Pack", "PackID", "ItemID=" + ItemID + " and Quantity=1", db_vms).Trim();
                    if (basePack.Equals(string.Empty)) continue;
                    decimal basePrice = 0;
                    basePrice = decimal.Round((decimal.Parse(Price) / decimal.Parse(Quantity)), 3);

                    string existPrice = GetFieldValue("PriceDefinition", "Price", "PackID=" + basePack + " and PriceListID=" + PriceListID + "", db_vms).Trim();
                    if (existPrice.Equals(string.Empty))
                    {
                        int PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms));
                        QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID.ToString());
                        QueryBuilderObject.SetField("QuantityRangeID", "1");
                        QueryBuilderObject.SetField("PackID", basePack);
                        QueryBuilderObject.SetField("CurrencyID", "1");
                        QueryBuilderObject.SetField("Tax", TAX.ToString());
                        QueryBuilderObject.SetField("Price", basePrice.ToString());
                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        err = QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);
                    }
                    else
                    {
                        if (decimal.Parse(existPrice) != basePrice)
                        {
                            QueryBuilderObject.SetField("Price", basePrice.ToString());
                            //QueryBuilderObject.SetField("Tax", TAX.ToString());
                            err = QueryBuilderObject.UpdateQueryString("PriceDefinition", "priceListID=" + PriceListID + " and packID=" + basePack + "", db_vms);
                        }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                WriteExceptions(ex.Message, "Prices", false);
            }
        }
        private void PricePeriodControl(string priceDefinitionID, DateTime startDate, DateTime endDate)
        {
            try
            {
                string existDefinition = GetFieldValue("PriceEndDate", "PriceDefinitionID", "PriceDefinitionID=" + priceDefinitionID + "", db_vms).Trim();
                if (existDefinition.Equals(string.Empty))
                {
                    QueryBuilderObject.SetField("priceDefinitionID", priceDefinitionID);
                    QueryBuilderObject.SetField("StartDate", "'" + startDate.ToString(DateFormat) + "'");
                    QueryBuilderObject.SetField("EndDate", "'" + endDate.ToString(DateFormat) + "'");
                    err = QueryBuilderObject.InsertQueryString("PriceEndDate", db_vms);
                }
                else
                {
                    QueryBuilderObject.SetField("StartDate", "'" + startDate.ToString(DateFormat) + "'");
                    QueryBuilderObject.SetField("EndDate", "'" + endDate.ToString(DateFormat) + "'");
                    err = QueryBuilderObject.UpdateQueryString("PriceEndDate", "PriceDefinitionID=" + priceDefinitionID + "", db_vms);
                }
            }
            catch
            {

            }
        }
        private void UpdateBanks()
        {

            object field = new object();

            DataTable DT = new DataTable();
            qry = new InCubeQuery("SELECT * FROM [Bank]", dbERP);
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
        public override void UpdateDiscount()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;


            object field = new object();

            InCubeQuery DeleteDiscountQuery = new InCubeQuery(db_vms, "Delete From Discount");
            DeleteDiscountQuery.ExecuteNonQuery();

            DataTable DT = new DataTable();
            qry = new InCubeQuery("SELECT * FROM [Discount]", dbERP);
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
        public override void UpdateRoutes()
        {
            int TOTALINSERTED = 0;


            object field = new object();
            DataTable DT = new DataTable();
            qry = new InCubeQuery("SELECT * FROM CustomerMasterView", dbERP);
            err = qry.Execute();
            DT = qry.GetDataTable();
            ClearProgress();
            SetProgressMax(DT.Rows.Count);
            foreach (DataRow row in DT.Rows)
            {
                string VisitPatternType = row["VisitPatternType"].ToString().Trim();
                string CustomerCode = row["CustomerGroupCode"].ToString().Trim();
                string CustomerName = row["CustomerGroup"].ToString().Trim();
                string outletBarCode = row["CustomerCode"].ToString().Trim();
                string outletCode = row["VISAAC"].ToString().Trim();
                string CustomerOutletDescriptionEnglish = row["CustomerName"].ToString().Trim();
                string CustomerOutletDescriptionArabic = row["CustomerName"].ToString().Trim();
                if (CustomerName.Trim().Replace(" ", "").ToLower() == "nogroup")
                {
                    CustomerCode = outletCode;
                    CustomerName = CustomerOutletDescriptionEnglish;
                }
                CustomerCode = outletCode;
                CustomerName = CustomerOutletDescriptionEnglish;
                string Phonenumber = row["Telephone"].ToString().Trim();
                string Faxnumber = row["Mobile"].ToString().Trim();
                string Email = row["Email"].ToString().Trim();
                string CustomerAddressEnglish = row["Address"].ToString().Trim();
                string CustomerAddressArabic = row["Address"].ToString().Trim();
                string channelDescription = row["Channel"].ToString().Trim();
                string ChannelCode = row["ChannelCode"].ToString().Trim();
                string CustomerGroupDescription = row["Channel"].ToString().Trim();
                string CreditLimit = row["CreditLimit"].ToString().Trim();
                string Paymentterms = row["CreditDays"].ToString().Trim();
                string CustomerType = row["CustomerPeymentTerms"].ToString().Trim();
                string companyCode = row["COMPANYID"].ToString().Trim();
                string SAPDivision = row["SAPDivision"].ToString().Trim();
                string companyID = GetFieldValue("SAPDivisions", "DivisionID", "CompanyCode = '" + companyCode + "' AND SAPDivision = '" + SAPDivision + "'", db_vms);

                if (CustomerType == "CREDIT")
                {
                    CustomerType = "2";
                }
                else if (CustomerType == "CASH")
                {
                    CustomerType = "1";
                }
                else
                {
                    CustomerType = "3";
                }
                #region Filling Route Sequence
                List<DaySequence> daySequence = new List<DaySequence>();
                DaySequence ds = new DaySequence();
                if (row["Day1"].ToString().Trim() != "0")
                {
                    ds.WeekDay = "SAT";
                    ds.VisitSequence = row["seq1"].ToString().Trim();
                    if (ds.VisitSequence.Equals(string.Empty)) ds.VisitSequence = "0";
                    daySequence.Add(ds);

                    ds = new DaySequence();
                }
                if (row["Day2"].ToString().Trim() != "0")
                {
                    ds.WeekDay = "SUN";
                    ds.VisitSequence = row["seq2"].ToString().Trim();
                    if (ds.VisitSequence.Equals(string.Empty)) ds.VisitSequence = "0";
                    daySequence.Add(ds);
                    ds = new DaySequence();
                }
                if (row["Day3"].ToString().Trim() != "0")
                {
                    ds.WeekDay = "MON";
                    ds.VisitSequence = row["seq3"].ToString().Trim();
                    if (ds.VisitSequence.Equals(string.Empty)) ds.VisitSequence = "0";
                    daySequence.Add(ds);
                    ds = new DaySequence();
                }
                if (row["Day4"].ToString().Trim() != "0")
                {
                    ds.WeekDay = "TUE";
                    ds.VisitSequence = row["seq4"].ToString().Trim();
                    if (ds.VisitSequence.Equals(string.Empty)) ds.VisitSequence = "0";
                    daySequence.Add(ds);
                    ds = new DaySequence();
                }
                if (row["Day5"].ToString().Trim() != "0")
                {
                    ds.WeekDay = "WED";
                    ds.VisitSequence = row["seq5"].ToString().Trim();
                    if (ds.VisitSequence.Equals(string.Empty)) ds.VisitSequence = "0";
                    daySequence.Add(ds);
                    ds = new DaySequence();
                }
                if (row["Day6"].ToString().Trim() != "0")
                {
                    ds.WeekDay = "THU";
                    ds.VisitSequence = row["seq6"].ToString().Trim();
                    if (ds.VisitSequence.Equals(string.Empty)) ds.VisitSequence = "0";
                    daySequence.Add(ds);
                    ds = new DaySequence();
                }
                if (row["Day7"].ToString().Trim() != "0")
                {
                    ds.WeekDay = "FRI";
                    ds.VisitSequence = "999999";
                    daySequence.Add(ds);
                    ds = new DaySequence();
                }

                #endregion


                string SupervisorCode = row["SupervisorCode"].ToString().Trim();
                string Supervisor = row["Supervisor"].ToString().Trim();
                string RoutemanagerCode = row["RouteManagerCode"].ToString().Trim();
                string Routemanager = row["RouteManager"].ToString().Trim();
                string RouteCode = row["Route"].ToString().Trim();
                string SalesManCode = row["SalesManCode"].ToString().Trim();
                string SalesMan = row["SalesMan"].ToString().Trim();
                if (SalesMan.Trim().Equals(string.Empty)) SalesMan = "0";
                if (Routemanager.Trim().Equals(string.Empty)) Routemanager = "0";
                if (Supervisor.Trim().Equals(string.Empty)) Supervisor = "0";
                Dictionary<string, string> empDic = new Dictionary<string, string>();
                string supervisorID = string.Empty;
                if (RouteCode.Trim().Equals(string.Empty)) continue;
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

                #region Territory
                string CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode='" + outletCode + "'", db_vms);
                string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerCode='" + outletCode + "'", db_vms);
                if (CustomerID.Trim().Equals(string.Empty) || OutletID.Trim().Equals(string.Empty)) continue;
                string deleteFromRoutecustomer = string.Format("delete from RouteCustomer where customerID={0} and OutletID={1}", CustomerID, OutletID);
                qry = new InCubeQuery(deleteFromRoutecustomer, db_vms);
                err = qry.ExecuteNonQuery();

                deleteFromRoutecustomer = string.Format("delete from CustOutTerritory where customerID={0} and OutletID={1}", CustomerID, OutletID);
                qry = new InCubeQuery(deleteFromRoutecustomer, db_vms);
                err = qry.ExecuteNonQuery();


                string TerritoryID = GetFieldValue("Territory", "TerritoryID", "TerritoryCode='" + RouteCode + "'", db_vms);
                if (TerritoryID == string.Empty)
                {
                    TerritoryID = GetFieldValue("[Territory]", "isnull(max(TerritoryID),0)+1", db_vms);
                    QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                    QueryBuilderObject.SetField("OrganizationID", "1");
                    QueryBuilderObject.SetField("TerritoryCode", "'" + RouteCode + "'");
                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    err = QueryBuilderObject.InsertQueryString("Territory", db_vms);

                    QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + RouteCode + "'");
                    QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);

                    QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                    QueryBuilderObject.SetField("LanguageID", "2");
                    QueryBuilderObject.SetField("Description", "'" + RouteCode + "'");
                    err = QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);

                }
                else
                {
                    QueryBuilderObject.SetField("Description", "'" + RouteCode + "'");
                    err = QueryBuilderObject.UpdateQueryString("TerritoryLanguage", "TerritoryID=" + TerritoryID + "", db_vms);

                }
                string existTerrDiv = GetFieldValue("TerritoryDivision", "TerritoryID", "TerritoryID=" + TerritoryID + " and DivisionID=" + companyID + "", db_vms).Trim();
                if (existTerrDiv.Equals(string.Empty))
                {
                    QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                    QueryBuilderObject.SetField("DivisionID", companyID);
                    err = QueryBuilderObject.InsertQueryString("TerritoryDivision", db_vms);
                }

                #endregion

                #region Route
                foreach (DaySequence daySequ in daySequence)
                {
                    CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode='" + outletCode + "'", db_vms);
                    OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerCode='" + outletCode + "'", db_vms);
                    string RouteID = GetFieldValue("RouteLanguage", "RouteID", "Description='" + RouteCode + "-" + daySequ.WeekDay + VisitPatternType + "'", db_vms);
                    if (RouteID == string.Empty)
                    {
                        DateTime EstimatedStart = DateTime.Parse(DateTime.Now.Date.AddHours(7).ToString());
                        DateTime EstimatedEnd = DateTime.Parse(DateTime.Now.Date.AddHours(23).ToString());

                        RouteID = GetFieldValue("[Route]", "isnull(max(RouteID),0)+1", db_vms);
                        QueryBuilderObject.SetField("RouteID", RouteID);
                        QueryBuilderObject.SetField("Inactive", "0");
                        QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                        QueryBuilderObject.SetField("EstimatedStart", "'" + EstimatedStart.ToString("dd/MMM/yyyy HH:mm tt") + "'");
                        QueryBuilderObject.SetField("EstimatedEnd", "'" + EstimatedEnd.ToString("dd/MMM/yyyy HH:mm tt") + "'");
                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");


                        QueryBuilderObject.SetField("RouteCode", "'" + RouteCode + "'");
                        err = QueryBuilderObject.InsertQueryString("Route", db_vms);

                        QueryBuilderObject.SetField("RouteID", RouteID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + RouteCode + "-" + daySequ.WeekDay + VisitPatternType + "'");
                        err = QueryBuilderObject.InsertQueryString("RouteLanguage", db_vms);

                        QueryBuilderObject.SetField("RouteID", RouteID);
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "'" + RouteCode + "-" + daySequ.WeekDay + VisitPatternType + "'");
                        QueryBuilderObject.InsertQueryString("RouteLanguage", db_vms);

                    }
                    else
                    {
                        QueryBuilderObject.SetField("Description", "'" + RouteCode + "-" + daySequ.WeekDay + VisitPatternType + "'");
                        err = QueryBuilderObject.UpdateQueryString("RouteLanguage", "RouteID=" + RouteID + "", db_vms);
                    }
                    #region Visit Pattern

                    RouteVisitPatternHandler(RouteID, daySequ.WeekDay, VisitPatternType);

                    #endregion
                    daySequ.RouteID = RouteID;
                    string ExistEmp = string.Empty;
                    string ExistCustomer = string.Empty;
                    foreach (KeyValuePair<string, string> str in empDic)
                    {
                        string employeeID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode='" + str.Key + "'", db_vms);

                        if (str.Value.Substring(str.Value.Length - 1, 1).Equals("4"))
                        {
                            ExistCustomer = GetFieldValue("SupervisorRoute", "EmployeeID=" + supervisorID + " and RouteID=" + ds.RouteID + "", db_vms);
                            if (ExistCustomer == string.Empty)
                            {
                                QueryBuilderObject.SetField("EmployeeID", supervisorID);
                                QueryBuilderObject.SetField("RouteID", ds.RouteID);
                                err = QueryBuilderObject.InsertQueryString("SupervisorRoute", db_vms);
                            }

                        }
                        if (str.Value.Substring(str.Value.Length - 1, 1).Equals("1"))
                        {

                        }
                        if (str.Value.Substring(str.Value.Length - 1, 1).Equals("2"))
                        {
                            ExistEmp = GetFieldValue("EmployeeTerritory", "EmployeeID", "EmployeeID=" + employeeID + " and TerritoryID=" + TerritoryID + "", db_vms);
                            if (ExistEmp == string.Empty)
                            {
                                QueryBuilderObject.SetField("EmployeeID", employeeID);
                                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                                err = QueryBuilderObject.InsertQueryString("EmployeeTerritory", db_vms);
                            }
                        }
                    }

                    if (CustomerID == string.Empty)
                    {
                        continue;
                    }

                    ExistCustomer = GetFieldValue("RouteCustomer", "RouteID", "CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and RouteID=" + daySequ.RouteID + "", db_vms);
                    if (ExistCustomer == string.Empty)
                    {

                        string getRouteCustomer = GetFieldValue("RouteCustomer", "RouteID", "CustomerID=" + CustomerID + " and OutletID=" + OutletID + "", db_vms).Trim();
                        string getSequence = GetFieldValue("RouteCustomer", "Sequence", "CustomerID=" + CustomerID + " and OutletID=" + OutletID + "", db_vms).Trim();
                        string getterr1 = GetFieldValue("Territory", "TerritoryID", "TerritoryCode=" + RouteCode + "", db_vms).Trim();
                        if (getRouteCustomer.Equals(string.Empty)) // then the customer does not exist on any route 
                        {

                            QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                            QueryBuilderObject.SetField("OutletID", OutletID);
                            QueryBuilderObject.SetField("RouteID", daySequ.RouteID);
                            QueryBuilderObject.SetField("Sequence", daySequ.VisitSequence);
                            err = QueryBuilderObject.InsertQueryString("RouteCustomer", db_vms);

                            QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                            QueryBuilderObject.SetField("OutletID", OutletID);
                            QueryBuilderObject.SetField("TerritoryID", getterr1);
                            err = QueryBuilderObject.InsertQueryString("CustOutTerritory", db_vms);
                            //if (err == InCubeErrors.Success)
                            //{
                            //    string getNewEmployee = GetFieldValue("EmployeeTerritory", "EmployeeID", "TerritoryID=" + getterr1 + "", db_vms).Trim();
                            //    if (!getNewEmployee.Equals(string.Empty))
                            //    {
                            //        QueryBuilderObject.SetField("EmployeeID", getNewEmployee);
                            //        err = QueryBuilderObject.UpdateQueryString("[Transaction]", "CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and RemainingAmount>0 ", db_vms);
                            //    }

                            //}
                        }
                        else//then the customer exist on other route 
                        {
                            //here i made a change to handle the case of changing the sequence only , if both getRouteCustomer and daySequ.RouteID was equal , this means that the customer is still in the same route
                            string getRouteTerr1 = GetFieldValue("[Route]", "TerritoryID", "RouteID=" + getRouteCustomer + "", db_vms).Trim();
                            string getRouteTerr2 = GetFieldValue("[Route]", "TerritoryID", "RouteID=" + daySequ.RouteID + "", db_vms).Trim();
                            if (getRouteTerr1.Equals(getRouteTerr2) && !getRouteCustomer.Equals(daySequ.RouteID))//same customer can be in different routes within the same territory
                            {
                                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                                QueryBuilderObject.SetField("OutletID", OutletID);
                                QueryBuilderObject.SetField("RouteID", daySequ.RouteID);
                                QueryBuilderObject.SetField("Sequence", daySequ.VisitSequence);
                                err = QueryBuilderObject.InsertQueryString("RouteCustomer", db_vms);
                            }
                            else if (getRouteTerr1.Equals(getRouteTerr2) && getRouteCustomer.Equals(daySequ.RouteID)) //here the customer visit sequence is only changed 
                            {
                                QueryBuilderObject.SetField("Sequence", daySequ.VisitSequence);
                                err = QueryBuilderObject.UpdateQueryString("RouteCustomer", "customerID=" + CustomerID.ToString() + " and OutletID=" + OutletID + " and RouteID=" + daySequ.RouteID + "", db_vms);
                            }
                            else
                            {
                                string GetTerrDiv1 = GetFieldValue("TerritoryDivision", "DivisionID", "TerritoryID=" + getRouteTerr1 + "", db_vms).Trim();
                                string GetTerrDiv2 = GetFieldValue("TerritoryDivision", "DivisionID", "TerritoryID=" + getRouteTerr2 + "", db_vms).Trim();
                                if (GetTerrDiv1.Equals(GetTerrDiv2))//the same customer cannot be on different Territories with same DivisionID
                                {
                                    string deleteRC = string.Format("delete from RouteCustomer where CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and RouteID=" + getRouteCustomer + "");
                                    qry = new InCubeQuery(deleteRC, db_vms);
                                    err = qry.ExecuteNonQuery();
                                    string DeleteTerritoryCust = string.Format("delete from CustOutTerritory where CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and TerritoryID=" + getRouteTerr1 + " ");
                                    qry = new InCubeQuery(DeleteTerritoryCust, db_vms);
                                    err = qry.ExecuteNonQuery();

                                    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                                    QueryBuilderObject.SetField("OutletID", OutletID);
                                    QueryBuilderObject.SetField("RouteID", daySequ.RouteID);
                                    QueryBuilderObject.SetField("Sequence", daySequ.VisitSequence);
                                    err = QueryBuilderObject.InsertQueryString("RouteCustomer", db_vms);

                                    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                                    QueryBuilderObject.SetField("OutletID", OutletID);
                                    QueryBuilderObject.SetField("TerritoryID", getRouteTerr2);
                                    err = QueryBuilderObject.InsertQueryString("CustOutTerritory", db_vms);
                                    //if (err == InCubeErrors.Success)
                                    //{
                                    //    string getNewEmployee = GetFieldValue("EmployeeTerritory", "EmployeeID", "TerritoryID=" + getRouteTerr2 + "", db_vms).Trim();
                                    //    if (!getNewEmployee.Equals(string.Empty))
                                    //    {
                                    //        QueryBuilderObject.SetField("EmployeeID", getNewEmployee);
                                    //        QueryBuilderObject.UpdateQueryString("[Transaction]", "CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and RemainingAmount>0 ", db_vms);
                                    //    }

                                    //}

                                }
                                else//the same customer can be on different Territories with different DivisionID
                                {

                                    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                                    QueryBuilderObject.SetField("OutletID", OutletID);
                                    QueryBuilderObject.SetField("RouteID", daySequ.RouteID);
                                    QueryBuilderObject.SetField("Sequence", daySequ.VisitSequence);
                                    err = QueryBuilderObject.InsertQueryString("RouteCustomer", db_vms);

                                    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                                    QueryBuilderObject.SetField("OutletID", OutletID);
                                    QueryBuilderObject.SetField("TerritoryID", getRouteTerr2);
                                    err = QueryBuilderObject.InsertQueryString("CustOutTerritory", db_vms);
                                }
                            }
                        }

                    }


                    if (!CustomerID.Trim().Equals(string.Empty) && !OutletID.Trim().Equals(string.Empty) && (outletCode.ToLower().StartsWith("zcr") || outletCode.ToLower().StartsWith("m-zcr")))
                    {
                        QueryBuilderObject.SetField("CustomerID", CustomerID);
                        QueryBuilderObject.SetField("OutletID", OutletID);

                        err = QueryBuilderObject.UpdateQueryString("Route", "RouteCode='" + RouteCode + "'", db_vms);
                    }
                    //string ExistCustomerTerritory = GetFieldValue("CustOutTerritory", "TerritoryID", "TerritoryID=" + TerritoryID + " and CustomerID=" + CustomerID + " and OutletID=" + OutletID + "", db_vms).Trim();
                    //if (ExistCustomerTerritory == string.Empty)
                    //{
                    //    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                    //    QueryBuilderObject.SetField("OutletID", OutletID);
                    //    QueryBuilderObject.SetField("TerritoryID", daySequ.RouteID);
                    //    err = QueryBuilderObject.InsertQueryString("CustOutTerritory", db_vms);
                    //}
                    string updateView = string.Format("update SAP_CustomerMaster_Table set CHANGEFLG='N' where CustomerID='{0}'", outletBarCode);
                    InCubeQuery updateCustQry = new InCubeQuery(dbERP, updateView);
                    err = updateCustQry.ExecuteNonQuery();
                    if (err != InCubeErrors.Success)
                    {

                    }
                }

                #endregion

                ReportProgress("Updating Routes");

                #region Commented
                //Route Name	
                //Salesman code	
                //Customer code

                //string RouteName = row[0].ToString().Trim();
                //string EmployeeCode = row[1].ToString().Trim();
                //string CustomerCode = row[2].ToString().Trim();

                //if (RouteName == string.Empty)
                //    continue;

                //string RouteID = GetFieldValue("RouteLanguage", "RouteID", " Description = '" + RouteName + "' AND LanguageID = 1", db_vms);
                //if (RouteID == string.Empty)
                //{
                //    RouteID = GetFieldValue("Route", "isnull(MAX(RouteID),0) + 1", db_vms);
                //}

                //string CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", " CustomerCode = '" + CustomerCode + "'", db_vms);
                //string OutletID = GetFieldValue("CustomerOutlet", "OutletID", " CustomerCode = '" + CustomerCode + "'", db_vms);

                //if (OutletID == string.Empty)
                //{
                //    continue;
                //}

                //string EmployeeID = GetFieldValue("Employee", "EmployeeID", " EmployeeCode = '" + EmployeeCode + "'", db_vms);

                //if (EmployeeID == string.Empty)
                //{
                //    continue;
                //}

                //string DivisionID = GetFieldValue("EmployeeDivision", "DivisionID", "EmployeeID = " + EmployeeID, db_vms);

                //if (DivisionID == string.Empty)
                //{
                //    continue;
                //}

                //err = ExistObject("Territory", "TerritoryID", "TerritoryID = " + RouteID, db_vms);
                //if (err != InCubeErrors.Success)
                //{
                //    TOTALINSERTED++;
                //    QueryBuilderObject.SetField("TerritoryID", RouteID);
                //    QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                //    QueryBuilderObject.SetField("DivisionID", DivisionID);

                //    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                //    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                //    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                //    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                //    QueryBuilderObject.InsertQueryString("Territory", db_vms);

                //}

                //err = ExistObject("Route", "RouteID", "RouteID = " + RouteID, db_vms);
                //if (err != InCubeErrors.Success)
                //{
                //    DateTime EstimatedStart = DateTime.Parse(DateTime.Now.Date.AddHours(7).ToString());
                //    DateTime EstimatedEnd = DateTime.Parse(DateTime.Now.Date.AddHours(23).ToString());

                //    QueryBuilderObject.SetField("RouteID", RouteID);
                //    QueryBuilderObject.SetField("Inactive", "0");
                //    QueryBuilderObject.SetField("TerritoryID", RouteID);
                //    QueryBuilderObject.SetField("EstimatedStart", "'" + EstimatedStart.ToString("dd/MMM/yyyy HH:mm tt") + "'");
                //    QueryBuilderObject.SetField("EstimatedEnd", "'" + EstimatedEnd.ToString("dd/MMM/yyyy HH:mm tt") + "'");

                //    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                //    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                //    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                //    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                //    QueryBuilderObject.InsertQueryString("Route", db_vms);
                //}

                //err = ExistObject("RouteLanguage", "RouteID", "RouteID = " + RouteID + " AND LanguageID = 1", db_vms);
                //if (err != InCubeErrors.Success)
                //{
                //    QueryBuilderObject.SetField("RouteID", RouteID);
                //    QueryBuilderObject.SetField("LanguageID", "1");
                //    QueryBuilderObject.SetField("Description", "'" + RouteName + "'");
                //    QueryBuilderObject.InsertQueryString("RouteLanguage", db_vms);
                //}

                //err = ExistObject("TerritoryLanguage", "TerritoryID", "TerritoryID = " + RouteID + " AND LanguageID = 1", db_vms);
                //if (err != InCubeErrors.Success)
                //{
                //    QueryBuilderObject.SetField("TerritoryID", RouteID);
                //    QueryBuilderObject.SetField("LanguageID", "1");
                //    QueryBuilderObject.SetField("Description", "'" + RouteName + "'");
                //    QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);
                //}

                //err = ExistObject("RouteLanguage", "RouteID", "RouteID = " + RouteID + " AND LanguageID = 2", db_vms);
                //if (err != InCubeErrors.Success)
                //{
                //    QueryBuilderObject.SetField("RouteID", RouteID);
                //    QueryBuilderObject.SetField("LanguageID", "2");
                //    QueryBuilderObject.SetField("Description", "'" + RouteName + "'");
                //    QueryBuilderObject.InsertQueryString("RouteLanguage", db_vms);
                //}

                //err = ExistObject("TerritoryLanguage", "TerritoryID", "TerritoryID = " + RouteID + " AND LanguageID = 2", db_vms);
                //if (err != InCubeErrors.Success)
                //{
                //    QueryBuilderObject.SetField("TerritoryID", RouteID);
                //    QueryBuilderObject.SetField("LanguageID", "2");
                //    QueryBuilderObject.SetField("Description", "N'" + RouteName + "'");
                //    QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);
                //}

                //err = ExistObject("RouteCustomer", "RouteID", "RouteID = " + RouteID + " AND CustomerID = " + CustomerID, db_vms);
                //if (err != InCubeErrors.Success)
                //{
                //    TOTALINSERTED++;
                //    QueryBuilderObject.SetField("RouteID", RouteID);
                //    QueryBuilderObject.SetField("CustomerID", CustomerID);
                //    QueryBuilderObject.SetField("OutletID", OutletID);
                //    QueryBuilderObject.InsertQueryString("RouteCustomer", db_vms);
                //}

                //err = ExistObject("RouteVisitPattern", "RouteID", "RouteID = " + RouteID, db_vms);
                //if (err != InCubeErrors.Success)
                //{
                //    TOTALINSERTED++;

                //    QueryBuilderObject.SetField("RouteID", RouteID);
                //    QueryBuilderObject.SetField("Week", "1");
                //    QueryBuilderObject.SetField("Sunday", "1");
                //    QueryBuilderObject.SetField("Monday", "1");
                //    QueryBuilderObject.SetField("Tuesday", "1");
                //    QueryBuilderObject.SetField("Wednesday", "1");
                //    QueryBuilderObject.SetField("Thursday", "1");
                //    QueryBuilderObject.SetField("Friday", "1");
                //    QueryBuilderObject.SetField("Saturday", "1");

                //    QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                //}

                //err = ExistObject("Employee", "EmployeeID", "EmployeeID = " + EmployeeID, db_vms);
                //if (err == InCubeErrors.Success)
                //{
                //    err = ExistObject("EmployeeTerritory", "EmployeeID", "EmployeeID = " + EmployeeID + " AND TerritoryID = " + RouteID, db_vms);
                //    if (err != InCubeErrors.Success)
                //    {
                //        TOTALINSERTED++;

                //        QueryBuilderObject.SetField("EmployeeID", EmployeeID);
                //        QueryBuilderObject.SetField("TerritoryID", RouteID);
                //        QueryBuilderObject.InsertQueryString("EmployeeTerritory", db_vms);
                //    }
                //}

                //err = ExistObject("CustOutTerritory", "TerritoryID", "TerritoryID = " + RouteID + " AND CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                //if (err != InCubeErrors.Success)
                //{
                //    TOTALINSERTED++;
                //    QueryBuilderObject.SetField("CustomerID", CustomerID);
                //    QueryBuilderObject.SetField("OutletID", OutletID);
                //    QueryBuilderObject.SetField("TerritoryID", RouteID);
                //    QueryBuilderObject.InsertQueryString("CustOutTerritory", db_vms);
                //}
                #endregion
            }

            //string delete = "delete from CustOutTerritory";
            //qry = new InCubeQuery(delete, db_vms);
            //err = qry.ExecuteNonQuery();

            string CustOutTerr = string.Format(@"INSERT INTO CustOutTerritory (CustomerID,OutletID,TerritoryID)
SELECT RC.CustomerID,RC.OutletID,TerritoryID
FROM RouteCustomer RC
INNER JOIN Route R ON R.RouteID = RC.RouteID
EXCEPT
SELECT CustomerID,OutletID,TerritoryID FROM CustOutTerritory");
            qry = new InCubeQuery(CustOutTerr, db_vms);
            err = qry.ExecuteNonQuery();

            DT.Dispose();

            WriteMessage("\r\n");
            WriteMessage("<<< ROUTE >>> Total Inserted = " + TOTALINSERTED);
        }
        public override void SendPrice()
        {
            try
            {
                int TOTALINSERTED = 0;
                object field = null;
                string ListType = string.Format(@"select (select KeyValue from Configuration where KeyName='CashPriceListForCreditCustomers' ) as CPFC,(select KeyValue from Configuration where KeyName='DefaultPriceListID') as DefaultList,(select KeyValue from Configuration where KeyName='AllowDummyPriceList') as DummyList");
                InCubeQuery priceQry = new InCubeQuery(ListType, db_vms);
                err = priceQry.Execute();
                priceQry.FindFirst();
                priceQry.GetField("DefaultList", ref field);
                string DefaultList = field.ToString().Trim();
                priceQry.GetField("CPFC", ref field);
                string CashListForCredit = field.ToString().Trim();
                priceQry.GetField("DummyList", ref field);
                string DummyList = field.ToString().Trim();

                DataTable dt = new DataTable();
                string prices = string.Format(@"SELECT  dbo.PriceList.PriceListCode, dbo.PriceListLanguage.Description, dbo.CustomerOutlet.CustomerCode, 
(case(dbo.PriceList.PriceListID) when 2 then 'Dummy Price List' else case(dbo.PriceList.PriceListID) when 1 then 'Cash List For Credit Customer' else case(dbo.PriceList.PriceListID) when 1 then 'Default Price List' else 'Normal List' end end end) as ListType

FROM         dbo.PriceList INNER JOIN
dbo.PriceListLanguage ON dbo.PriceList.PriceListID = dbo.PriceListLanguage.PriceListID left outer JOIN
dbo.CustomerPrice ON dbo.PriceList.PriceListID = dbo.CustomerPrice.PriceListID left outer JOIN
dbo.CustomerOutlet ON dbo.CustomerPrice.CustomerID = dbo.CustomerOutlet.CustomerID AND dbo.CustomerPrice.OutletID = dbo.CustomerOutlet.OutletID
where dbo.PriceList.IsDeleted=0 and EndDate>=getdate()

", DummyList, CashListForCredit, DefaultList);
                //priceQry = new InCubeQuery("delete from PriceMaster", dbERP);
                //err = priceQry.ExecuteNonQuery();
                //priceQry = new InCubeQuery("delete from PriceList", dbERP);
                //err = priceQry.ExecuteNonQuery();
                priceQry = new InCubeQuery(prices, db_vms);
                err = priceQry.Execute();
                dt = priceQry.GetDataTable();
                ClearProgress();
                SetProgressMax(dt.Rows.Count);
                DataTable tbl = new DataTable();

                foreach (DataRow dr in dt.Rows)
                {
                    ReportProgress();
                    //string insertPrice = string.Format(@"insert into PriceList(PriceListCode,CustomerCode,PriceListType,PriceListName) values ('{0}','{1}','{2}','{3}')", dr["PriceListCode"].ToString(),
                    //    dr["CustomerCode"].ToString(), dr["ListType"].ToString(), dr["Description"].ToString());
                    //priceQry = new InCubeQuery(insertPrice, dbERP);
                    //err=priceQry.ExecuteNonQuery();

                    if (err == InCubeErrors.Success)
                    {
                        //HERE I HAVE MADE A CHANGE OF ADDING A PRICELOG TABLE AND CREATE A LOG TO RECORD ANY CHANGES IN THE PRICES ONLY, BECAUSE SENDING PRICES WAS TAKING A LONG TIME.
                        string Definition = string.Format(@"SELECT     dbo.PriceList.PriceListCode,  dbo.Item.ItemCode, dbo.Pack.Quantity as PackQuantity, 
dbo.PriceLog.Price,dbo.PriceLog.Action,dbo.PriceLog.PriceDefinitionID,dbo.PriceLog.ActionDate
FROM         dbo.PriceList INNER JOIN
dbo.PriceLog ON dbo.PriceLog.PriceListID = dbo.PriceList.PriceListID INNER JOIN
dbo.Pack ON dbo.PriceLog.PacKID = dbo.Pack.PackID INNER JOIN
dbo.Item ON dbo.Pack.ItemID = dbo.Item.ItemID where dbo.PriceList.PriceListCode='{0}' and Synchronized='N'
", dr["PriceListCode"].ToString().Trim());
                        qry = new InCubeQuery(Definition, db_vms);
                        err = qry.Execute();
                        tbl = new DataTable();
                        tbl = qry.GetDataTable();
                        string insert = string.Empty;
                        foreach (DataRow row in tbl.Rows)
                        {
                            switch (row["Action"].ToString().Trim())
                            {
                                case "I":
                                    insert = string.Format("insert into PriceMaster values('{0}','{1}','{2}','{3}',cast(cast('{4}' as decimal(9,0)) as int),'{5}','{6}',{7},'N','{8}','{9}')",
                               dr["PriceListCode"].ToString().Trim(), dr["Description"].ToString().Trim(), dr["CustomerCode"].ToString().Trim(), row["ItemCode"].ToString().Trim(), row["PackQuantity"].ToString().Trim(), row["Price"].ToString().Trim(), dr["ListType"].ToString().Trim(), row["PriceDefinitionID"].ToString().Trim(), "I", DateTime.Parse(row["ActionDate"].ToString().Trim()).ToString(DateFormat));
                                    qry = new InCubeQuery(insert, dbERP);
                                    err = qry.ExecuteNonQuery();
                                    break;
                                case "U":
                                    insert = string.Format("insert into PriceMaster values('{0}','{1}','{2}','{3}',cast(cast('{4}' as decimal(9,0)) as int),'{5}','{6}',{7},'N','{8}','{9}')",
                                dr["PriceListCode"].ToString().Trim(), dr["Description"].ToString().Trim(), dr["CustomerCode"].ToString().Trim(), row["ItemCode"].ToString().Trim(), row["PackQuantity"].ToString().Trim(), row["Price"].ToString().Trim(), dr["ListType"].ToString().Trim(), row["PriceDefinitionID"].ToString().Trim(), "U", DateTime.Parse(row["ActionDate"].ToString().Trim()).ToString(DateFormat));
                                    qry = new InCubeQuery(insert, dbERP);
                                    err = qry.ExecuteNonQuery();
                                    break;
                                case "D":
                                    insert = string.Format("insert into PriceMaster values('{0}','{1}','{2}','{3}',cast(cast('{4}' as decimal(9,0)) as int),'{5}','{6}',{7},'N','{8}','{9}')",
                               dr["PriceListCode"].ToString().Trim(), dr["Description"].ToString().Trim(), dr["CustomerCode"].ToString().Trim(), row["ItemCode"].ToString().Trim(), row["PackQuantity"].ToString().Trim(), row["Price"].ToString().Trim(), dr["ListType"].ToString().Trim(), row["PriceDefinitionID"].ToString().Trim(), "D", DateTime.Parse(row["ActionDate"].ToString().Trim()).ToString(DateFormat));
                                    qry = new InCubeQuery(insert, dbERP);
                                    err = qry.ExecuteNonQuery();
                                    break;
                            }

                            string deleteRow = "update PriceLog set synchronized='Y' where PriceDefinitionID=" + row["PriceDefinitionID"].ToString().Trim() + "";
                            qry = new InCubeQuery(deleteRow, db_vms);
                            err = qry.ExecuteNonQuery();

                            TOTALINSERTED++;
                        }


                    }

                }
                WriteMessage("\r\n");
                WriteMessage("<<< Prices >>> Total Inserted = " + TOTALINSERTED);
            }
            catch
            {

            }
        }
        public override void SendPromotion()
        {
            try
            {
                InCubeQuery qry;
                DataTable dt;
                string deletePromotions = string.Empty;
                //TO DO : DELETE THE PROMOTIONS FROM INTERMEDIATE TABLES BEFORE YOU SEND THE PROMOTIONS
                deletePromotions = string.Format("delete from Promotion");
                qry = new InCubeQuery(deletePromotions, dbERP);
                err = qry.ExecuteNonQuery();
                deletePromotions = string.Format("delete from CustomerPromotion");
                qry = new InCubeQuery(deletePromotions, dbERP);
                err = qry.ExecuteNonQuery();
                deletePromotions = string.Format("delete from PromotionOptionDetail");
                qry = new InCubeQuery(deletePromotions, dbERP);
                err = qry.ExecuteNonQuery();
                deletePromotions = string.Format("delete from PromotionOptionDetailGroup");
                qry = new InCubeQuery(deletePromotions, dbERP);
                err = qry.ExecuteNonQuery();
                string insertPromo = string.Empty;
                string Promotion = string.Empty;
                Promotion = @"SELECT     dbo.Promotion.PromotionID, dbo.Promotion.StartDate, dbo.Promotion.EndDate, dbo.Promotion.IsRepeated, dbo.Promotion.RepeatCount, dbo.Promotion.Inactive, 
                      dbo.PromotionLanguage.Description, dbo.PromotionGroup.Code, dbo.PromotionGroupLanguage.Description AS Promo
                      FROM    dbo.Promotion left outer JOIN
                      dbo.PromotionGroupAssignment ON dbo.Promotion.PromotionID = dbo.PromotionGroupAssignment.PromotionID left outer JOIN
                      dbo.PromotionGroupLanguage ON dbo.PromotionGroupAssignment.PromotionGroupID = dbo.PromotionGroupLanguage.PromotionGroupID AND 
                      dbo.PromotionGroupLanguage.LanguageID = 1 left outer JOIN
                      dbo.PromotionLanguage ON dbo.Promotion.PromotionID = dbo.PromotionLanguage.PromotionID AND dbo.PromotionLanguage.LanguageID = 1 left outer JOIN
                      dbo.PromotionGroup ON dbo.PromotionGroupAssignment.PromotionGroupID = dbo.PromotionGroup.PromotionGroupID";
                qry = new InCubeQuery(Promotion, db_vms);
                dt = new DataTable();
                err = qry.Execute();
                dt = qry.GetDataTable();
                int TOTALINSERTED = 0;
                ClearProgress();
                SetProgressMax(dt.Rows.Count);
                foreach (DataRow dr in dt.Rows)
                {
                    ReportProgress("Sending Promotion");

                    insertPromo = string.Format(@"insert into Promotion (PromotionID,StartDate,EndDate,IsRepeated,RepeatCount,Inactive,Description,PromotionGroupCode,PromotionGroupName,Integrated) 
                                                   values({0},CONVERT(DATETIME,'{1}',101),CONVERT(DATETIME,'{2}',101),'{3}','{4}','{5}','{6}','{7}','{8}','{9}')",
                                                     dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), dr[4].ToString(), dr[5].ToString(), dr[6].ToString(), dr[7].ToString(), dr[8].ToString(), "1");
                    qry = new InCubeQuery(insertPromo, dbERP);
                    err = qry.ExecuteNonQuery();
                    if (err == InCubeErrors.Success) TOTALINSERTED++;

                }

                WriteMessage("\r\n");
                WriteMessage("<<< Promotion>>> Total Inserted = " + TOTALINSERTED);


                Promotion = @"SELECT     dbo.CustomerPromotion.PromotionID, dbo.CustomerOutlet.CustomerCode, dbo.CustomerGroupLanguage.Description, dbo.CustomerPromotion.AllCustomers
FROM         dbo.CustomerPromotion left outer JOIN
                      dbo.CustomerOutlet ON dbo.CustomerPromotion.CustomerID = dbo.CustomerOutlet.CustomerID left outer JOIN
                      dbo.CustomerOutletGroup ON dbo.CustomerOutlet.CustomerID = dbo.CustomerOutletGroup.CustomerID AND 
                      dbo.CustomerOutlet.OutletID = dbo.CustomerOutletGroup.OutletID left outer JOIN
                      dbo.CustomerGroupLanguage ON dbo.CustomerOutletGroup.GroupID = dbo.CustomerGroupLanguage.GroupID
GROUP BY dbo.CustomerPromotion.PromotionID, dbo.CustomerOutlet.CustomerCode, dbo.CustomerGroupLanguage.Description, dbo.CustomerPromotion.AllCustomers";
                qry = new InCubeQuery(Promotion, db_vms);
                dt = new DataTable();
                err = qry.Execute();
                dt = qry.GetDataTable();
                TOTALINSERTED = 0;
                ClearProgress();
                SetProgressMax(dt.Rows.Count);
                foreach (DataRow dr in dt.Rows)
                {
                    ReportProgress("Sending Promotion");

                    insertPromo = string.Format(@"insert into CustomerPromotion (PromotionID,CustomerCode,CustomerGroupID,AllCustomers) 
                                                   values({0},'{1}','{2}','{3}')",
                                                     dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString());
                    qry = new InCubeQuery(insertPromo, dbERP);
                    err = qry.ExecuteNonQuery();
                    if (err == InCubeErrors.Success) TOTALINSERTED++;
                }
                WriteMessage("\r\n");
                WriteMessage("<<< Promotion>>> Total Inserted = " + TOTALINSERTED);

                Promotion = @"SELECT     dbo.PromotionOptionDetail.PromotionID, dbo.PromotionOptionDetail.PromotionOptionID, dbo.PromotionOptionDetail.PromotionOptionTypeID, 
                      dbo.PromotionOptionDetail.PromotionOptionDetailID, dbo.PromotionOptionDetail.PromotionOptionDetailTypeID, dbo.Item.ItemCode, dbo.Pack.Quantity, 
                      dbo.PackGroupLanguage.Description, dbo.PromotionOptionDetail.Value, dbo.PromotionOptionDetail.Range
FROM         dbo.PromotionOptionDetail LEFT OUTER JOIN
                      dbo.Pack ON dbo.PromotionOptionDetail.PackID = dbo.Pack.PackID LEFT OUTER JOIN
                      dbo.Item ON dbo.Pack.ItemID = dbo.Item.ItemID INNER JOIN
                      dbo.PackGroupLanguage ON dbo.PromotionOptionDetail.PackGroupID = dbo.PackGroupLanguage.PackGroupID AND dbo.PackGroupLanguage.LanguageID = 1";

                qry = new InCubeQuery(Promotion, db_vms);
                dt = new DataTable();
                err = qry.Execute();
                dt = qry.GetDataTable();
                TOTALINSERTED = 0;
                ClearProgress();
                SetProgressMax(dt.Rows.Count);
                foreach (DataRow dr in dt.Rows)
                {
                    ReportProgress("Sending Promotion");

                    insertPromo = string.Format(@"insert into PromotionOptionDetail (PromotionID,PromotionOptionID,PromotionOptionTypeID,PromotionOptionDetailID,PromotionOptionDetailTypeID,ItemCode,PackQty,ItemGroupDescription,Value,Range) 
                                                   values('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}')",
                                                     dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), dr[4].ToString(), dr[5].ToString(), dr[6].ToString(), dr[7].ToString(), dr[8].ToString(), dr[9].ToString());
                    qry = new InCubeQuery(insertPromo, dbERP);
                    err = qry.ExecuteNonQuery();
                    if (err == InCubeErrors.Success) TOTALINSERTED++;
                }
                WriteMessage("\r\n");
                WriteMessage("<<< Promotion>>> Total Inserted = " + TOTALINSERTED);


                Promotion = @"SELECT     dbo.PromotionOptionDetailGroup.PromotionID, dbo.PromotionOptionDetailGroup.PromotionOptionID, dbo.PromotionOptionDetailGroup.PromotionOptionDetailID, 
                      dbo.PackGroupLanguage.Description, dbo.Item.ItemCode, dbo.Pack.Quantity AS PackQty, dbo.PromotionOptionDetailGroup.Quantity
FROM         dbo.PromotionOptionDetailGroup INNER JOIN
                      dbo.Pack ON dbo.PromotionOptionDetailGroup.PackID = dbo.Pack.PackID INNER JOIN
                      dbo.Item ON dbo.Pack.ItemID = dbo.Item.ItemID INNER JOIN
                      dbo.PackGroupLanguage ON dbo.PromotionOptionDetailGroup.PackGroupID = dbo.PackGroupLanguage.PackGroupID AND dbo.PackGroupLanguage.LanguageID = 1";

                qry = new InCubeQuery(Promotion, db_vms);
                dt = new DataTable();
                err = qry.Execute();
                dt = qry.GetDataTable();
                TOTALINSERTED = 0;
                ClearProgress();
                SetProgressMax(dt.Rows.Count);
                foreach (DataRow dr in dt.Rows)
                {
                    ReportProgress("Sending Promotion");

                    insertPromo = string.Format(@"insert into PromotionOptionDetailGroup (PromotionID,PromotionOptionID,PromotionOptionDetailID,ItemGroupDescription,ItemCode,PackQty,Quantity) 
                                                   values({0},{1},{2},'{3}','{4}',{5},{6})",
                                                     dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), dr[4].ToString(), dr[5].ToString(), dr[6].ToString());
                    qry = new InCubeQuery(insertPromo, dbERP);
                    err = qry.ExecuteNonQuery();
                    if (err == InCubeErrors.Success) TOTALINSERTED++;
                }
                WriteMessage("\r\n");
                WriteMessage("<<< Promotion>>> Total Inserted = " + TOTALINSERTED);

            }
            catch
            {

            }
        }
        public override void SendPOSM()
        {
            DataTable dt = new DataTable();
            string selectPOSM = string.Format(@"
SELECT     dbo.CustomerAccessPoint.Code, dbo.CustomerOutlet.CustomerCode, dbo.AccessPointLanguage.Description, dbo.CustomerAccessPoint.Barcode, dbo.Item.ItemCode, 
                      dbo.Pack.Quantity, dbo.AccessPointCapacity.MinQty
FROM         dbo.AccessPoint INNER JOIN
                      dbo.AccessPointCapacity ON dbo.AccessPoint.AccessPointID = dbo.AccessPointCapacity.AccessPointID INNER JOIN
                      dbo.AccessPointLanguage ON dbo.AccessPoint.AccessPointID = dbo.AccessPointLanguage.AccessPointID and dbo.AccessPointLanguage.LanguageID=1 INNER JOIN
                      dbo.CustomerAccessPoint ON dbo.AccessPoint.AccessPointID = dbo.CustomerAccessPoint.AccessPointID INNER JOIN
                      dbo.Pack ON dbo.AccessPointCapacity.PackID = dbo.Pack.PackID INNER JOIN
                      dbo.Item ON dbo.Pack.ItemID = dbo.Item.ItemID INNER JOIN
                      dbo.CustomerOutlet ON dbo.CustomerAccessPoint.CustomerID = dbo.CustomerOutlet.CustomerID");
            InCubeQuery POSMqry = new InCubeQuery(selectPOSM, db_vms);
            POSMqry.Execute();
            dt = POSMqry.GetDataTable();
            int TOTALINSERTED = 0;
            ClearProgress();
            SetProgressMax(dt.Rows.Count);
            foreach (DataRow dr in dt.Rows)
            {
                ReportProgress("Sending POSM ");

                string insertPOSM = string.Format(@"insert into POSM_Master values('{0}','{1}','{2}','{3}','{4}',{5},{6})", dr[0].ToString(), dr[1].ToString(),
                    dr[2].ToString(), dr[3].ToString(), dr[4].ToString(), dr[5].ToString(), dr[6].ToString());
                POSMqry = new InCubeQuery(selectPOSM, db_vms);
                err = POSMqry.ExecuteNonQuery();
                if (err != InCubeErrors.Success)
                {
                    WriteMessage("\r\n" + dr[3].ToString() + " " + dr[3].ToString() + " - POSM Failed");
                }
                else
                {
                    TOTALINSERTED++;
                }
            }

            WriteMessage("\r\n");
            WriteMessage("<<< POSM>>> Total Inserted = " + TOTALINSERTED);

        }
        public override void SendReciepts()
        {
            try
            {
                DataTable dt = new DataTable();
                string receipt = string.Format(@"select (case when CO.Barcode is null then CO.CustomerCode else CO.Barcode end) as Barcode ,CP.PaymentDate,CP.CustomerPaymentID,CP.TransactionID,CP.PaymentTypeID,E.EmployeeCode,
CP.VoucherNumber,CP.VoucherDate,CP.VoucherOwner,B.Code,B.Code,CP.Notes,CP.CurrencyID,CP.AppliedAmount,CP.Synchronized,CP.GPSLatitude,CP.GPSLongitude,
CP.PaymentStatusID,CP.SourceTransactionID,CP.RemainingAmount,CP.Posted,isnull(CP.Downloaded,0),CP.VisitNo,CP.RouteHistoryID,CP.AppliedPaymentID,CP.AccountID,isnull(CP.BounceDate,'1990/01/01'),
DIV.DIVISIONCODE ,MW.WAREHOUSECODE as Plant 
from CustomerPayment CP 
inner join CustomerOutlet CO on CO.CustomerID=CP.CustomerID and CO.OutletID=CP.OutletID 
inner join Employee E on CP.EmployeeID=E.EmployeeID 
left outer join Bank B on CP.BankID=B.BankID 
inner join [Transaction] t on t.transactionID=cp.transactionID and t.divisionID=cp.divisionID and t.customerid=cp.customerid
inner JOIN DIVISION DIV ON DIV.DIVISIONID=  (CASE CP.DIVISIONID WHEN -1 THEN (ISNULL(T.SalesRepID,-1)) ELSE CP.DivisionID END)
left outer join WAREHOUSEDIVISION MW on MW.ROUTECODE=E.EMPLOYEECODE AND MW.DIVISIONID=CP.DIVISIONID
where (CP.Synchronized = 0)  AND CP.DivisionID < 3
 and (CO.CustomerTypeID=2 or CO.CustomerTypeID=3) and CP.PaymentTypeID<>4 and CP.PaymentStatusID<>5 and t.salesmode=2
Group By
CO.Barcode,CP.PaymentDate,CP.CustomerPaymentID,CP.TransactionID,CP.PaymentTypeID,E.EmployeeCode,
CP.VoucherNumber,CP.VoucherDate,CP.VoucherOwner,B.Code,B.Code,CP.Notes,CP.CurrencyID,CP.AppliedAmount,CP.Synchronized,CP.GPSLatitude,CP.GPSLongitude,
CP.PaymentStatusID,CP.SourceTransactionID,CP.RemainingAmount,CP.Posted,CP.Downloaded,CP.VisitNo,CP.RouteHistoryID,CP.AppliedPaymentID,CP.AccountID,CP.BounceDate,
CO.CustomerTypeID,CustomerCode,DIV.DIVISIONCODE,MW.WAREHOUSECODE");


                if (Filters.EmployeeID != -1)
                {
                    receipt += "    AND CustomerPayment.EmployeeID = " + Filters.EmployeeID;
                }
                InCubeQuery receiptQry = new InCubeQuery(receipt, db_vms);
                err = receiptQry.Execute();
                dt = receiptQry.GetDataTable();
                string insertReceipt = string.Empty;
                string proddate = string.Empty;
                string SAP_InvoiceNumber = "";
                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;
                ClearProgress();
                SetProgressMax(dt.Rows.Count);
                //WriteExceptions_Pay("Payments Count=" + dt.Rows.Count.ToString() + "", "CUSTOMER PAYMENTS",false);
                foreach (DataRow dr in dt.Rows)
                {
                    //Start Sending
                    WriteExceptions_Pay("Payment number: " + dr[2].ToString().Trim() + " For transaction: " + dr[3].ToString().Trim(), "Start sending ..", true, false);
                    SAP_InvoiceNumber = dr[3].ToString().Trim();
                    //if (SAP_InvoiceNumber.ToUpper().StartsWith("U"))
                    //{
                    //    SAP_InvoiceNumber = SAP_InvoiceNumber.Substring(1);
                    //}
                    string CHKEmployeeID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode='" + dr[5].ToString().Trim() + "'", db_vms).Trim();
                    string MaxSequence = string.Empty;
                    object field = new object();
                    string vehicleID = GetFieldValue("Warehouse", "WarehouseID", "WarehouseCode='" + dr[5].ToString().Trim() + "'", db_vms).Trim();
                    string CheckUploaded = string.Format("select top(1)uploaded,deviceserial from RouteHistory where EmployeeID=" + CHKEmployeeID + " ORDER BY RouteHistoryID Desc ");
                    qry = new InCubeQuery(CheckUploaded, db_vms);
                    err = qry.Execute();
                    err = qry.FindFirst();
                    err = qry.GetField("uploaded", ref field);
                    string uploaded = field.ToString().Trim();
                    err = qry.GetField("deviceserial", ref field);
                    string deviceserial = field.ToString().Trim();
                    if (uploaded.ToString().Trim().Equals(string.Empty) || uploaded.ToString().Trim().Equals("System.Object")) continue;
                    if (!uploaded.ToString().Trim().Equals(string.Empty) && !uploaded.ToString().Trim().Equals("System.Object"))
                    {

                        if (Convert.ToBoolean(uploaded.ToString().Trim()))
                        {
                            string countSt = GetFieldValue("WarehouseTransaction", "count(*)", "WarehouseID=" + vehicleID + " and WarehouseTransactionStatusID=1", db_vms).Trim();
                            if (!countSt.Equals("0"))
                            {
                                WriteMessage("\r\n");
                                WriteMessage("<<< The Route " + dr[5].ToString().Trim() + " is not downloaded . No Transactions will be Transfered .>>> Total Updated = " + TOTALUPDATED);
                                //WriteExceptions_Pay("", "Device is not downloaded !!", false, false);
                                //WriteExceptions_Pay("Route " + dr[2].ToString().Trim() + " was not downloaded ---- transaction = " + dr[1].ToString().Trim() + "", "Device not downloaded", true);
                                //WriteExceptions_Pay(" ATTENTION! THERE ARE LOAD REQUESTS THAT ARE NEITHER APPROVED NOR CANCELLED IN THE DATABASE , COUNT IS (" + countSt + "), NO SALES TRANSACTIONS WERE SENT FOR THIS ROUTE", "Inserting WH Transaction ", false);
                            }
                            WriteExceptions_Pay("", "Route " + dr[5].ToString().Trim() + " is not downloaded", false, true);
                            continue;
                        }
                    }
                    //WriteExceptions_Pay("Route " + dr[2].ToString().Trim() + " is downloaded ...", "Device is downloaded", false);
                    string existInERP = GetFieldValue("CustomerPayment", "CustomerPaymentID", "CustomerPaymentID='" + dr[2].ToString().Trim() + "' and TransactionID='" + SAP_InvoiceNumber + "'", dbERP).Trim();
                    if (!existInERP.Equals(string.Empty))
                    {
                        WriteExceptions_Pay("", "Payment already exist in ERP, igonre sending", false, true);
                        continue;
                    }
                    WriteExceptions_Pay("", "Payment doesn't exist in staging database and it will be inserted", false, false);
                    TOTALINSERTED++;
                    ReportProgress("Sending Receipts");

                    if (dr[9].ToString().Equals(string.Empty))
                    {
                        proddate = DateTime.Today.ToString();
                    }
                    else
                    {
                        proddate = dr[7].ToString();
                    }


                    insertReceipt = string.Format(@"insert into CustomerPayment values ('{0}',convert(datetime,'{1}',101),'{2}','{3}',{4},'{5}','{6}',convert(datetime,'{7}',101),'{8}','{9}','{10}',
'{11}','{12}','{13}','{14}','{15}','{16}','{17}','{18}','{19}','{20}','{21}','{22}','{23}','{24}','{25}','{26}','{27}','{28}','{29}')", dr[0].ToString(), DateTime.Parse(dr[1].ToString()).ToString("yyyy/MM/dd"), dr[2].ToString(),
    SAP_InvoiceNumber, dr[4].ToString(), dr[5].ToString(),
    dr[6].ToString(), DateTime.Parse(proddate).ToString("yyyy/MM/dd"), dr[8].ToString(),
    dr[9].ToString(), dr[10].ToString(), dr[11].ToString(),
    dr[12].ToString(), dr[13].ToString(), dr[14].ToString(),
    dr[15].ToString(), dr[16].ToString(), dr[17].ToString(),
    dr[18].ToString(), dr[19].ToString(), dr[20].ToString(),
     dr[21].ToString(), dr[22].ToString(), dr[23].ToString(),
    dr[24].ToString(), dr[25].ToString(), dr[26].ToString(),
    dr[27].ToString(), DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), dr["Plant"].ToString());
                    receiptQry = new InCubeQuery(insertReceipt, dbERP);
                    err = receiptQry.ExecuteNonQuery();
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\n" + dr[2].ToString() + " - Receipt Failed");
                        WriteExceptions_Pay("", "Inserting failed .. the insert statement is:\r\n" + insertReceipt, false, true);
                    }
                    else
                    {
                        WriteExceptions_Pay("", "Inserting successfully .. Counting rows for this payment in staging table", false, false);
                        existInERP = GetFieldValue("CustomerPayment", "CustomerPaymentID", "CustomerPaymentID='" + dr[2].ToString().Trim() + "' and TransactionID='" + SAP_InvoiceNumber + "'", dbERP).Trim();
                        if (!existInERP.Equals(string.Empty))
                        {
                            WriteExceptions_Pay("", "Payment exist in ERP, synchronized flag will be set", false, false);
                            string update = string.Format("update CustomerPayment set Synchronized=1 where CustomerPaymentID='{0}' and TransactionID='{1}'", dr[2].ToString().Trim(), dr[3].ToString().Trim());
                            receiptQry = new InCubeQuery(update, db_vms);
                            err = receiptQry.ExecuteNonQuery();
                            WriteExceptions_Pay("", "The result of setting the flag is: " + err.ToString(), false, true);
                            WriteMessage("\r\n" + dr[2].ToString() + " - Receipt Transfered Successfully");
                        }
                        else
                        {
                            WriteExceptions_Pay("", "Payment does not exist in ERP, synchronized flag will not be set", false, true);
                        }
                    }
                }

                string updateSafi = string.Format("update CustomerPayment set Synchronized=1 where DivisionID = 3");
                qry = new InCubeQuery(updateSafi, db_vms);
                err = qry.ExecuteNonQuery();

                WriteMessage("\r\n");
                WriteMessage("<<< Receipts >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);

            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public override void SendTransfers()
        {
            try
            {
                DataTable dt = new DataTable();
                string transfers = string.Format(@"SELECT WH.Barcode,WHT.TransactionID,TransactionTypeID,TransactionDate,EE.EmployeeCode,E.EmployeeCode,'notes',Synchronized,
ISNULL(WHT.ProductionDate,GETDATE()),MW.WAREHOUSECODE,Posted,Downloaded,WarehouseTransactionStatusID,CreationSourceID,
DIV.DivisionID,RouteHistoryID,TransactionOperationID,DIV.DIVISIONID
FROM
(SELECT DISTINCT WT.TransactionID,WT.WarehouseID,WT.ImplementedBy,WT.RequestedBy,IC.DivisionID,WT.Synchronized
,WT.TransactionTypeID,WT.TransactionDate,WT.Posted,WT.Downloaded,WT.WarehouseTransactionStatusID,WT.CreationSourceID
,WT.RouteHistoryID,WT.TransactionOperationID,WT.ProductionDate
FROM WarehouseTransaction WT
INNER JOIN WhTransDetail WD ON WD.TransactionID = WT.TransactionID AND WD.WarehouseID = WT.WarehouseID 
AND WD.DivisionID = WT.DivisionID
INNER JOIN Pack P ON P.PackID = WD.PackID
INNER JOIN Item I ON I.ItemID = P.ItemID
INNER JOIN ItemCategory IC ON IC.ItemCategoryID = I.ItemCategoryID
LEFT JOIN SentWarehouseTransactions ST ON ST.TransactionID = WT.TransactionID AND ST.DivisionID = IC.DivisionID
WHERE WT.DivisionID < 3 AND ST.TransactionID IS NULL
AND WT.TransactionDate >= '{0}' AND WT.TransactionDate < '{1}') WHT
INNER join Warehouse WH on WHT.WarehouseID=WH.WarehouseID 
INNER join Employee E on WHT.ImplementedBy=E.EmployeeID 
INNER join Employee EE on WHT.RequestedBy=EE.EmployeeID
INNER JOIN DIVISION DIV ON DIV.DIVISIONID=WHT.DIVISIONID
LEFT JOIN WAREHOUSEDIVISION MW on MW.ROUTECODE=E.EMPLOYEECODE AND MW.DIVISIONID=WHT.DIVISIONID
WHERE  ((WHT.TransactionTypeID = 1 AND WarehouseTransactionStatusID IN (1,2,4))
OR (WHT.TransactionTypeID IN (2,6,7,5) AND WHT.DivisionID IN (1,2)))
GROUP BY
WH.Barcode,WHT.TransactionID,TransactionTypeID,TransactionDate,EE.EmployeeCode,E.EmployeeCode,
Synchronized,WHT.ProductionDate,MW.WAREHOUSECODE,Posted,Downloaded,WarehouseTransactionStatusID,
CreationSourceID,DIV.DivisionID,RouteHistoryID,TransactionOperationID ,DIV.DIVISIONID
", Filters.FromDate.ToString("yyyy/MM/dd"), Filters.ToDate.AddDays(1).ToString("yyyy/MM/dd"));
                InCubeQuery transferQry = new InCubeQuery(transfers, db_vms);
                transferQry.Execute();
                dt = transferQry.GetDataTable();
                DataTable detailDT = new DataTable();
                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;
                ClearProgress();
                SetProgressMax(dt.Rows.Count);

                string warehouseTransactionID = "";
                string DivisionID = "";
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                StringBuilder Message;
                bool Rollback = false;
                bool SetSynchronized = false;
                foreach (DataRow dr in dt.Rows)
                {
                    warehouseTransactionID = "";
                    DivisionID = "";
                    Rollback = false;
                    SetSynchronized = false;
                    Message = new StringBuilder();
                    sw.Reset();
                    sw.Start();
                    try
                    {
                        ReportProgress("Sending Transfers");

                        warehouseTransactionID = dr[1].ToString().Trim();
                        DivisionID = dr[17].ToString().Trim();
                        if (warehouseTransactionID.ToLower().StartsWith("u")) warehouseTransactionID = warehouseTransactionID.Remove(0, 1);
                        Message.AppendLine("TransactionID: " + warehouseTransactionID + ", DivisionID: " + DivisionID);
                        string existInERP = GetFieldValue("WarehouseTransaction", "TransactionID", "TransactionID='" + warehouseTransactionID + "' AND DivisionID=" + DivisionID + "", dbERP).Trim();
                        if (!existInERP.Equals(string.Empty))
                        {
                            Message.AppendLine("Transaction exists in ERP");
                            SetSynchronized = true;
                            continue;
                        }

                        string insertTransfer = string.Format(@"insert into WarehouseTransaction values ('{0}','{1}','{2}',convert(datetime,'{3}',101),'{4}','{5}','{6}','{7}',convert(datetime,'{8}',101),'{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}','{18}')"
                            , dr[0].ToString(), warehouseTransactionID, dr[2].ToString(), DateTime.Parse(dr[3].ToString()).ToString("yyyy/MM/dd"), dr[4].ToString(), dr[5].ToString(), dr[6].ToString(),
                            dr[7].ToString(), DateTime.Parse(dr[8].ToString()).ToString("yyyy/MM/dd"), dr[9].ToString(), dr[10].ToString(), dr[11].ToString(), dr[12].ToString(), dr[13].ToString(), DivisionID, dr[15].ToString(), dr[16].ToString(), DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), dr["WAREHOUSECODE"].ToString());
                        transferQry = new InCubeQuery(insertTransfer, dbERP);
                        err = transferQry.ExecuteNonQuery();
                        if (err != InCubeErrors.Success)
                        {
                            Message.AppendLine("Header info insertion failed!!");
                            continue;
                        }
                        sw.Stop();
                        Message.AppendLine("Header info inserted successfully.. Elapsed time: " + sw.ElapsedMilliseconds + "ms");
                        sw.Reset();
                        sw.Start();

                        string transfDet = string.Format(@"SELECT
W.Barcode,WD.TransactionID,WD.ZoneID,I.ItemCode
,CASE WHEN ROUND(SUM(WD.Quantity),2) % 1 <> 0 AND P2.PackID IS NOT NULL THEN 1 ELSE P.Quantity END Quantity,'1990-01-01 00:00:00.000' ExpiryDate
,CASE WHEN ROUND(SUM(WD.Quantity),2) % 1 <> 0 AND P2.PackID IS NOT NULL THEN ROUND(SUM(WD.Quantity)*P.Quantity,0) ELSE ROUND(SUM(WD.Quantity),2) END Quantity
,WD.Balanced,LTRIM(RTRIM(WD.BatchNo)) BatchNo,'1990-01-01 00:00:00.000' ProductionDate
,CASE WHEN (ROUND(SUM(WD.Quantity),2) % 1 <> 0 AND P2.PackID IS NOT NULL) OR P.Quantity = 1 THEN 1 ELSE 2 END,WD.DivisionID,WD.PackStatusID
FROM WhTransDetail WD
INNER JOIN Warehouse W ON W.WarehouseID = WD.WarehouseID
INNER JOIN Pack P ON P.PackID = WD.PackID
LEFT JOIN Pack P2 ON P2.ItemID = P.ItemID AND P2.Quantity = 1
INNER JOIN Item I ON I.ItemID = P.ItemID
INNER JOIN ItemCategory IC ON IC.ItemCategoryID = I.ItemCategoryID
WHERE WD.TransactionID = '{0}' AND IC.DivisionID = {1}
GROUP BY W.Barcode,WD.TransactionID,WD.ZoneID,I.ItemCode,P.Quantity,WD.Balanced,P2.PackID
,WD.DivisionID,WD.PackStatusID,LTRIM(RTRIM(WD.BatchNo))
", warehouseTransactionID, DivisionID);

                        transferQry = new InCubeQuery(transfDet, db_vms);
                        err = transferQry.Execute();
                        if (err != InCubeErrors.Success)
                        {
                            Message.AppendLine("Error in reading details, header will be deleted");
                            Message.AppendLine("Query: " + transferQry);
                            Rollback = true;
                            continue;
                        }
                        detailDT = transferQry.GetDataTable();
                        if (detailDT.Rows.Count == 0 && dr[2].ToString() != "7")
                        {
                            Message.AppendLine("No details were retrieved, header will be deleted");
                            Message.AppendLine("Query: " + transferQry);
                            Rollback = true;
                            continue;
                        }
                        string InvanDetailsCount = GetFieldValue(@"WhTransDetail WD
INNER JOIN Pack P ON P.PackID = WD.PackID
INNER JOIN Item I ON I.ItemID = P.ItemID
INNER JOIN ItemCategory IC ON IC.ItemCategoryID = I.ItemCategoryID", "ROUND(SUM(WD.Quantity),3)", "TransactionID = '" + dr[1].ToString() + "' AND IC.DivisionID = " + dr[14].ToString(), db_vms).Trim();

                        Message.AppendLine("Details read successfully, number of detail lines is: " + detailDT.Rows.Count + " and total quantity is: " + InvanDetailsCount);
                        sw.Stop();
                        Message.AppendLine("Time elapsed in reading is: " + sw.ElapsedMilliseconds + "ms");
                        sw.Reset();
                        sw.Start();

                        string proddate = string.Empty;
                        string expi = string.Empty;

                        bool ErrorInDetails = false;
                        foreach (DataRow det in detailDT.Rows)
                        {
                            try
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
                                string insertDet = string.Format(@"insert into WhTransDetail values ('{0}','{1}','{2}','{3}',cast(cast('{4}' as decimal) as int),convert(datetime,'{5}',101),'{6}','{7}','{8}',convert(datetime,'{9}',101),'{10}','{11}','{12}')",
                                    det[0].ToString(), warehouseTransactionID, det[2].ToString(), det[3].ToString(), det[4].ToString(), DateTime.Parse(expi).ToString("yyyy/MM/dd"), det[6].ToString(),
                                    det[7].ToString(), det[8].ToString(), DateTime.Parse(proddate).ToString("yyyy/MM/dd"), det[10].ToString(), dr[17].ToString(), det[12].ToString());
                                transferQry = new InCubeQuery(insertDet, dbERP);
                                err = transferQry.ExecuteNonQuery();
                                if (err != InCubeErrors.Success)
                                {
                                    ErrorInDetails = true;
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
                                ErrorInDetails = true;
                                break;
                            }
                        }

                        sw.Stop();
                        if (ErrorInDetails)
                        {
                            Message.AppendLine("Error was encountered in inserting details, trasnaction will be rolled back");
                            Rollback = true;
                            continue;
                        }
                        else
                        {
                            Message.AppendLine("Inserting elapsed time: " + sw.ElapsedMilliseconds + "ms");
                        }
                        sw.Reset();

                        string ERPDetailsCount = GetFieldValue("WhTransDetail", "ROUND(SUM(Quantity),3)", "TransactionID='" + warehouseTransactionID + "'  and CompanyID='" + dr[17].ToString() + "'", dbERP).Trim();
                        if (InvanDetailsCount.Equals(ERPDetailsCount))
                        {
                            Message.AppendLine("ERP quantity = " + ERPDetailsCount + " which matches InVan, trasnaction will be set as sent.");
                            SetSynchronized = true;
                        }
                        else
                        {
                            Message.AppendLine("ERP quantity = " + ERPDetailsCount + " which doesn't matches InVan, trasnaction will be rolled back");
                            Rollback = true;
                        }

                        WriteMessage("\r\n" + dr[1].ToString() + " - Transaction Transfered Successfully");
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
                    }
                    finally
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, Message.ToString(), LoggingType.Information, LoggingFiles.errorTSFR);
                        if (Rollback)
                        {
                            string deleteTrx = string.Format("delete from warehouseTransaction where TransactionID='" + warehouseTransactionID + "'  and DivisionID=" + DivisionID + "");
                            qry = new InCubeQuery(deleteTrx, dbERP);
                            err = qry.ExecuteNonQuery();

                            deleteTrx = string.Format("delete from WhTransDetail where TransactionID='" + warehouseTransactionID + "'  and CompanyID='" + DivisionID + "'");
                            qry = new InCubeQuery(deleteTrx, dbERP);
                            err = qry.ExecuteNonQuery();
                        }
                        else if (SetSynchronized)
                        {
                            //Update synchronized
                            string updateHD = string.Format("INSERT INTO SentWarehouseTransactions (TransactionID,DivisionID,SendingTime) VALUES ('{0}',{1},GETDATE())", warehouseTransactionID, DivisionID);
                            transferQry = new InCubeQuery(updateHD, db_vms);
                            err = transferQry.ExecuteNonQuery();
                        }
                    }
                }

                //string updateSafi = string.Format("update WarehouseTransaction set Synchronized=1 where DivisionID = 3");
                //qry = new InCubeQuery(updateSafi, db_vms);
                //err = qry.ExecuteNonQuery();

                WriteMessage("\r\n");
                WriteMessage("<<< Transfers >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);

            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
        }
        private InCubeErrors UpdateFlag(string TableName, string Criteria)
        {
            try
            {
                string query = string.Format("update {0} set CHANGEFLG='N' where {1}", TableName, Criteria);
                InCubeQuery qry = new InCubeQuery(query, dbERP);
                err = qry.ExecuteNonQuery();
                return err;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
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
        private void WriteExceptions_Pay(string header, string description, bool start, bool end)
        {
            try
            {
                string filename = "PAY-Log" + DateTime.Today.Year.ToString() + DateTime.Today.Month.ToString() + DateTime.Today.Day.ToString() + ".log";
                string filePath = CoreGeneral.Common.StartupPath + "\\" + filename;
                if (!File.Exists(filePath))
                {
                    FileStream fs = File.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite);
                    fs.Close();
                    fs.Dispose();
                }
                StreamWriter wrt = new StreamWriter(filename, true);
                if (start)
                    wrt.Write("\r\n===========================================================================");
                if (header.Equals(string.Empty))
                    wrt.Write("\r\n" + header);
                wrt.Write("\r\n" + DateTime.Now.ToString() + ": " + description);
                if (end)
                    wrt.Write("\r\n===========================================================================");
                wrt.Close();
            }
            catch
            {

            }
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
        private void RouteVisitPatternHandler(string RouteID, string VisitDay, string PatternType)
        {
            try
            {
                string deletePattern = "delete from routevisitpattern where routeid=" + RouteID;
                qry = new InCubeQuery(deletePattern, db_vms);
                err = qry.ExecuteNonQuery();
                switch (VisitDay)
                {
                    case "SAT":
                        if (PatternType.Equals("0"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "1");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }
                        else if (PatternType.Equals("1"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "1");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "2");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "3");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "1");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "4");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }
                        else if (PatternType.Equals("2"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "2");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "1");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "3");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "4");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "1");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }



                        break;
                    case "SUN":
                        if (PatternType.Equals("0"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "1");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }
                        else if (PatternType.Equals("1"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "1");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "2");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "3");
                            QueryBuilderObject.SetField("Sunday", "1");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "4");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }
                        else if (PatternType.Equals("2"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "2");
                            QueryBuilderObject.SetField("Sunday", "1");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "3");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "4");
                            QueryBuilderObject.SetField("Sunday", "1");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }


                        break;
                    case "MON":
                        if (PatternType.Equals("0"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "1");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }
                        else if (PatternType.Equals("1"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "1");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "2");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "3");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "1");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "4");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }
                        else if (PatternType.Equals("2"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "2");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "1");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "3");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "4");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "1");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }

                        break;
                    case "TUE":
                        if (PatternType.Equals("0"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "1");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }
                        else if (PatternType.Equals("1"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "1");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "2");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "3");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "1");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "4");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }
                        else if (PatternType.Equals("2"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "2");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "1");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "3");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "4");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "1");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }



                        break;
                    case "WED":
                        if (PatternType.Equals("0"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "1");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }
                        else if (PatternType.Equals("1"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "1");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "2");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "3");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "1");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "4");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }
                        else if (PatternType.Equals("2"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "2");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "1");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "3");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "4");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "1");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }


                        break;
                    case "THU":
                        if (PatternType.Equals("0"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "1");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }
                        else if (PatternType.Equals("1"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "1");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "2");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "3");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "1");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "4");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }
                        else if (PatternType.Equals("2"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "2");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "1");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "3");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "4");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "1");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }


                        break;
                    case "FRI":
                        if (PatternType.Equals("0"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "1");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }
                        else if (PatternType.Equals("1"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "1");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "2");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "3");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "1");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "4");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }
                        else if (PatternType.Equals("2"))
                        {
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "1");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "2");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "1");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "3");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "0");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                            QueryBuilderObject.SetField("RouteID", RouteID);
                            QueryBuilderObject.SetField("Week", "4");
                            QueryBuilderObject.SetField("Sunday", "0");
                            QueryBuilderObject.SetField("Monday", "0");
                            QueryBuilderObject.SetField("Tuesday", "0");
                            QueryBuilderObject.SetField("Wednesday", "0");
                            QueryBuilderObject.SetField("Thursday", "0");
                            QueryBuilderObject.SetField("Friday", "1");
                            QueryBuilderObject.SetField("Saturday", "0");
                            err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                        }


                        break;
                }
            }
            catch
            {

            }

        }
    }
}