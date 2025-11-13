using InCubeIntegration_DAL;
using InCubeLibrary;
using Microsoft.Dynamics.GP.eConnect;
using Microsoft.Dynamics.GP.eConnect.Serialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace InCubeIntegration_BL
{
    public class IntegrationAIN : IntegrationBase
    {
        QueryBuilder QueryBuilderObject = new QueryBuilder();
        InCubeErrors err;
        string sConnectionString = "";
        private long UserID;
        string DateFormat = "dd/MMM/yyyy";

        public IntegrationAIN(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            db_ERP = new InCubeDatabase();
            InCubeErrors err = db_ERP.Open("InCubeSQL", "InVan");
            if (err != InCubeErrors.Success)
            {
                WriteMessage("Unable to connect to GP database");
                Initialized = false;
                return;
            }
            sConnectionString = db_ERP.GetConnection().ConnectionString;
            UserID = CurrentUserID;
        }

        #region UpdateItem

        public override void UpdateItem()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            InCubeErrors err;
            object field = new object();

            DateTime _modificationDate = GetIntegrationModificationDateNew(db_vms);

            if (db_ERP.GetConnection().State != ConnectionState.Open)
            {
                WriteMessage("\r\n");
                WriteMessage("Cannot connect to GP database , please check the connection");
                return;
            }

            string SelectItems = @"SELECT 
                                           ITMCLSCD,ITEMNMBR, ITMSHNAM,
                                           CREATDDT, MODIFDT,USCATVLS_5,
                                           USCATVLS_4,ITMSHNAM ,DEX_ROW_ID,
                                           ITMGEDSC 
                                           
                                           FROM IV00101 I";

            //You can filter your query by IV00101.ITMGEDSC=’3AADF’.  All dairy items have a field value of ‘3AADF’ and poultry products have ‘3AAPF’.  Also,  IV00101.ITEMTYPE<>2  (discontinued)  should be added in the filter so that discontinued items will not be included in the query result.
            SelectItems += @" WHERE I.ITEMTYPE<>2 AND (I.ITMGEDSC='3AADF' OR I.ITMGEDSC='3AAPF') And 
                              MODIFDT  >= '" + _modificationDate.ToString(DateFormat) + "' and MODIFDT  < '" + DateTime.Now.AddDays(1).ToString(DateFormat) + "'";

            #region ItemDivision - Add default divisions

            err = ExistObject("Division", "DivisionID", "DivisionID = 1", db_vms);
            if (err != InCubeErrors.Success)
            {
                QueryBuilderObject.SetField("DivisionID", "1");
                QueryBuilderObject.SetField("DivisionCode", "'3AADF'");
                QueryBuilderObject.SetField("OrganizationID", "1");

                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.InsertQueryString("Division", db_vms);

                QueryBuilderObject.SetField("DivisionID", "1");
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'Dairy'");
                QueryBuilderObject.InsertQueryString("DivisionLanguage", db_vms);
            }

            err = ExistObject("Division", "DivisionID", "DivisionID = 2", db_vms);
            if (err != InCubeErrors.Success)
            {
                QueryBuilderObject.SetField("DivisionID", "2");
                QueryBuilderObject.SetField("DivisionCode", "'3AAPF'");
                QueryBuilderObject.SetField("OrganizationID", "1");

                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.InsertQueryString("Division", db_vms);

                QueryBuilderObject.SetField("DivisionID", "2");
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'Poultry'");
                QueryBuilderObject.InsertQueryString("DivisionLanguage", db_vms);

            }


            #endregion

            //QueryBuilderObject.SetField("InActive", "1");
            //QueryBuilderObject.UpdateQueryString("Item", db_vms);

            InCubeQuery ItemQuery = new InCubeQuery(db_ERP, SelectItems);
            InCubeErrors error = ItemQuery.Execute();
            DataTable tbl = new DataTable();
            tbl = ItemQuery.GetDataTable();

            ClearProgress();
            SetProgressMax(ItemQuery.GetDataTable().Rows.Count);
            string deactivatingQry = "";
            int deactivated = 0;

            err = ItemQuery.FindFirst();
            while (err == InCubeErrors.Success)
            {
                ReportProgress("Updating Items");

                int ItemCategoryID = 0;
                int ItemDivisionID = 1;

                err = ItemQuery.GetField("ITMGEDSC", ref field);

                if (field.ToString().Trim().Equals("3AAPF")) ItemDivisionID = 2;

                ItemQuery.GetField("ITMCLSCD", ref field);
                string ItemCategory = field.ToString().Trim();

                #region ItemCategory

                string SelectItemCategory = "Select ItemCategoryID From ItemCategoryLanguage Where Description= '" + ItemCategory + "'";
                InCubeQuery ItemCategoryQuery = new InCubeQuery(db_vms, SelectItemCategory);
                ItemCategoryQuery.Execute();
                err = ItemCategoryQuery.FindFirst();

                if (err == InCubeErrors.Success)
                {
                    ItemCategoryQuery.GetField(0, ref field);
                    ItemCategoryID = int.Parse(field.ToString());
                }
                else if (err == InCubeErrors.DBNoMoreRows)
                {
                    ItemCategoryID = int.Parse(GetFieldValue("ItemCategory", "isnull(MAX(ItemCategoryID),0) + 1", db_vms));

                    QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());
                    QueryBuilderObject.SetField("ItemCategoryCode", "0");
                    QueryBuilderObject.SetField("DivisionID", ItemDivisionID.ToString());

                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    QueryBuilderObject.InsertQueryString("ItemCategory", db_vms);

                    QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + ItemCategory + "'");
                    QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", db_vms);
                }
                ItemCategoryQuery.Close();

                #endregion

                #region Item Brand
                string brandID = GetFieldValue("BrandLanguage", "BrandID", "Description='" + ItemCategory + "'", db_vms).Trim();
                if (brandID.Equals(string.Empty))
                {
                    brandID = GetFieldValue("Brand", "isnull(MAX(BrandID),0) + 1", db_vms).Trim();
                    QueryBuilderObject.SetField("BrandID", brandID);
                    err = QueryBuilderObject.InsertQueryString("Brand", db_vms);

                    QueryBuilderObject.SetField("BrandID", brandID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + ItemCategory + "'");
                    err = QueryBuilderObject.InsertQueryString("BrandLanguage", db_vms);
                }
                #endregion

                ItemQuery.GetField("ITEMNMBR", ref field);
                string ItemCode = field.ToString().Trim();

                #region Get Barcode

                string realBarcode = GetFieldValue("IV00110", "PLANNERNAME", "PLANNERID = '" + ItemCode + "'", db_ERP);
                //string realBarcode = ItemCode;
                #endregion
                // HERE WE SHOULD MAKE A CHANGE 
                //ItemQuery.GetField("DEX_ROW_ID", ref field);
                //int ItemID = int.Parse(field.ToString());
                string ItemID = string.Empty;
                ItemQuery.GetField("ITMSHNAM", ref field);
                string Itemdesc = field.ToString().Trim();

                ItemQuery.GetField("USCATVLS_5", ref field);
                string Brand = field.ToString().Trim();

                ItemQuery.GetField("USCATVLS_4", ref field);
                string Orgin = field.ToString().Trim();

                ItemQuery.GetField("ITMSHNAM", ref field);
                string PackDefinition = field.ToString().Trim();

                #region Item

                ItemID = GetFieldValue("Item", "ItemID", "ItemCode = '" + ItemCode + "'", db_vms).Trim();
                if (!ItemID.Equals(string.Empty)) // Exist Item --- Update Query
                {
                    TOTALUPDATED++;

                    QueryBuilderObject.SetField("ItemCode", "'" + ItemCode + "'");
                    QueryBuilderObject.SetField("PackDefinition", "'" + PackDefinition + "'");

                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    QueryBuilderObject.SetField("InActive", "0");

                    err = QueryBuilderObject.UpdateQueryString("Item", " ItemID = " + ItemID, db_vms);
                }
                else  // New Item --- Insert Query
                {
                    TOTALINSERTED++;
                    ItemID = GetFieldValue("Item", "isnull(MAX(ItemID),0) + 1", db_vms).Trim();
                    QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                    QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());
                    QueryBuilderObject.SetField("ItemCode", "'" + ItemCode + "'");
                    QueryBuilderObject.SetField("PackDefinition", "'" + PackDefinition + "'");
                    QueryBuilderObject.SetField("InActive", "0");
                    QueryBuilderObject.SetField("BrandID", brandID);
                    QueryBuilderObject.SetField("ItemType", "1");
                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    err = QueryBuilderObject.InsertQueryString("Item", db_vms);
                }
                deactivatingQry += ItemID + ",";
                #endregion

                #region ItemLanguage
                err = ExistObject("ItemLanguage", "ItemID", "ItemID = " + ItemID, db_vms);
                if (err == InCubeErrors.Success) // Exist Item --- Update Query
                {
                    QueryBuilderObject.SetField("Description", "'" + Itemdesc + "'");
                    QueryBuilderObject.UpdateQueryString("ItemLanguage", " ItemID =" + ItemID + " AND LanguageID = 1", db_vms);
                }
                else if (err == InCubeErrors.DBNoMoreRows) // New Item --- Insert Query
                {
                    QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + Itemdesc + "'");
                    err = QueryBuilderObject.InsertQueryString("ItemLanguage", db_vms);
                }
                #endregion

                #region UPDATE/INSERT PACK , PACKDETAIL

                string ItemPriceStr = "SELECT UOFM,QTYBSUOM FROM IV00108 Where (ITEMNMBR = '" + ItemCode.Trim() + "') GROUP BY UOFM, QTYBSUOM ";
                InCubeQuery ItemPriceQuery = new InCubeQuery(db_ERP, ItemPriceStr);
                err = ItemPriceQuery.Execute();
                err = ItemPriceQuery.FindFirst();

                while (err == InCubeErrors.Success)
                {
                    decimal UOM = 1;
                    ItemPriceQuery.GetField("QTYBSUOM", ref field);
                    if (field.ToString().Trim() != string.Empty)
                    {
                        UOM = decimal.Parse(field.ToString());
                    }

                    ItemPriceQuery.GetField("UOFM", ref field);
                    string UOMDESC = field.ToString().Trim();

                    #region PackType

                    int PacktypeID = 1;
                    err = ExistObject("PackTypeLanguage", "PackTypeID", " Description = '" + UOMDESC + "'", db_vms);
                    if (err == InCubeErrors.Success)
                    {
                        PacktypeID = int.Parse(GetFieldValue("PackTypeLanguage", "PackTypeID", " Description = '" + UOMDESC + "'", db_vms));
                    }
                    else
                    {
                        PacktypeID = int.Parse(GetFieldValue("PackType", "ISNULL(MAX(PackTypeID),0) + 1", db_vms));

                        QueryBuilderObject.SetField("PackTypeID", PacktypeID.ToString());
                        QueryBuilderObject.InsertQueryString("PackType", db_vms);

                        QueryBuilderObject.SetField("PackTypeID", PacktypeID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + UOMDESC + "'");
                        QueryBuilderObject.InsertQueryString("PackTypeLanguage", db_vms);
                    }

                    #endregion

                    int PackID = 1;

                    err = ExistObject("Pack", "PackID", "ItemID = " + ItemID + " and PackTypeID = " + PacktypeID, db_vms);

                    if (err == InCubeErrors.Success)
                    {
                        PackID = int.Parse(GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " and PackTypeID = " + PacktypeID, db_vms));

                        //QueryBuilderObject.SetField("Barcode", "'" + realBarcode + "'");
                        QueryBuilderObject.SetField("Quantity", UOM.ToString());
                        QueryBuilderObject.UpdateQueryString("Pack", "PackID = " + PackID, db_vms);

                    }
                    else
                    {
                        PackID = int.Parse(GetFieldValue("Pack", "ISNULL(MAX(PackID),0) + 1", db_vms));

                        QueryBuilderObject.SetField("PackID", PackID.ToString());
                        QueryBuilderObject.SetField("Barcode", "'" + realBarcode + "'");
                        QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                        QueryBuilderObject.SetField("PackTypeID", PacktypeID.ToString());
                        QueryBuilderObject.SetField("Quantity", UOM.ToString());

                        err = QueryBuilderObject.InsertQueryString("Pack", db_vms);

                    }
                    err = ItemPriceQuery.FindNext();
                }

                #endregion

                err = ItemQuery.FindNext();
            }
            if (deactivatingQry != "")
            {
                deactivatingQry = "UPDATE ITEM SET InActive = 1 WHERE ItemID NOT IN (" + deactivatingQry.Substring(0, deactivatingQry.Length - 1) + ")";
                InCubeQuery qry = new InCubeQuery(db_vms, deactivatingQry);
                qry.ExecuteNonQuery(ref deactivated);
            }
            ItemQuery.Close();
            WriteMessage("\r\n");
            WriteMessage("<<< ITEMS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED + ", Total Deactivated = " + deactivated);
        }

        #endregion

        #region UpdateCustomer

        private string AddCustomerGeographicalInfo(string Country, string State, string City, string Area, string Street)
        {
            string StreetID = "0";
            try
            {
                string CountryID = GetFieldValue("Country", "CountryID", "CountryCode = '" + Country + "'", db_vms).Trim();
                if (CountryID.Equals(string.Empty))
                {
                    CountryID = GetFieldValue("Country", "ISNULL(MAX(CountryID),0) + 1", db_vms).Trim();
                    QueryBuilderObject.SetField("CountryID", CountryID);
                    QueryBuilderObject.SetStringField("CountryCode", Country);
                    err = QueryBuilderObject.InsertQueryString("Country", db_vms);
                    QueryBuilderObject.SetField("CountryID", CountryID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", Country);
                    err = QueryBuilderObject.InsertQueryString("CountryLanguage", db_vms);
                }
                string StateID = GetFieldValue("State", "StateID", "StateCode = '" + State + "'", db_vms).Trim();
                if (StateID.Equals(string.Empty))
                {
                    StateID = GetFieldValue("State", "ISNULL(MAX(StateID),0) + 1", db_vms).Trim();
                    QueryBuilderObject.SetField("CountryID", CountryID);
                    QueryBuilderObject.SetField("StateID", StateID);
                    QueryBuilderObject.SetStringField("StateCode", State);
                    err = QueryBuilderObject.InsertQueryString("State", db_vms);
                    QueryBuilderObject.SetField("CountryID", CountryID);
                    QueryBuilderObject.SetField("StateID", StateID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", State);
                    err = QueryBuilderObject.InsertQueryString("StateLanguage", db_vms);
                }
                string CityID = GetFieldValue("City", "CityID", "CityCode = '" + City + "'", db_vms).Trim();
                if (CityID.Equals(string.Empty))
                {
                    CityID = GetFieldValue("City", "ISNULL(MAX(CityID),0) + 1", db_vms).Trim();
                    QueryBuilderObject.SetField("CountryID", CountryID);
                    QueryBuilderObject.SetField("StateID", StateID);
                    QueryBuilderObject.SetField("CityID", CityID);
                    QueryBuilderObject.SetStringField("CityCode", City);
                    err = QueryBuilderObject.InsertQueryString("City", db_vms);
                    QueryBuilderObject.SetField("CountryID", CountryID);
                    QueryBuilderObject.SetField("StateID", StateID);
                    QueryBuilderObject.SetField("CityID", CityID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", City);
                    err = QueryBuilderObject.InsertQueryString("CityLanguage", db_vms);
                }
                string AreaID = GetFieldValue("Area", "AreaID", "AreaCode = '" + Area + "'", db_vms).Trim();
                if (AreaID.Equals(string.Empty))
                {
                    AreaID = GetFieldValue("Area", "ISNULL(MAX(AreaID),0) + 1", db_vms).Trim();
                    QueryBuilderObject.SetField("CountryID", CountryID);
                    QueryBuilderObject.SetField("StateID", StateID);
                    QueryBuilderObject.SetField("CityID", CityID);
                    QueryBuilderObject.SetField("AreaID", AreaID);
                    QueryBuilderObject.SetStringField("AreaCode", Area);
                    err = QueryBuilderObject.InsertQueryString("Area", db_vms);
                    QueryBuilderObject.SetField("CountryID", CountryID);
                    QueryBuilderObject.SetField("StateID", StateID);
                    QueryBuilderObject.SetField("CityID", CityID);
                    QueryBuilderObject.SetField("AreaID", AreaID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", Area);
                    err = QueryBuilderObject.InsertQueryString("AreaLanguage", db_vms);
                }
                StreetID = GetFieldValue("Street", "StreetID", "StreetCode = '" + Street + "'", db_vms).Trim();
                if (StreetID.Equals(string.Empty))
                {
                    StreetID = GetFieldValue("Street", "ISNULL(MAX(StreetID),0) + 1", db_vms).Trim();
                    QueryBuilderObject.SetField("CountryID", CountryID);
                    QueryBuilderObject.SetField("StateID", StateID);
                    QueryBuilderObject.SetField("CityID", CityID);
                    QueryBuilderObject.SetField("AreaID", AreaID);
                    QueryBuilderObject.SetField("StreetID", StreetID);
                    QueryBuilderObject.SetStringField("StreetCode", Street);
                    err = QueryBuilderObject.InsertQueryString("Street", db_vms);
                    QueryBuilderObject.SetField("CountryID", CountryID);
                    QueryBuilderObject.SetField("StateID", StateID);
                    QueryBuilderObject.SetField("CityID", CityID);
                    QueryBuilderObject.SetField("AreaID", AreaID);
                    QueryBuilderObject.SetField("StreetID", StreetID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", Street);
                    err = QueryBuilderObject.InsertQueryString("StreetLanguage", db_vms);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return StreetID;
        }
        public override void UpdateCustomer()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            InCubeErrors err;
            object field = new object();

            DateTime _modificationDate = GetIntegrationModificationDateNew(db_vms);

            if (db_ERP.GetConnection().State != ConnectionState.Open)
            {
                WriteMessage("\r\n");
                WriteMessage("Cannot connect to GP database , please check the connection");
                return;
            }

            //            string SelectCustomer = @"SELECT  
            //                                              A.CRLMTAMT,A.CUSTNAME, A.ADDRESS1, 
            //                                              A.PHONE1, A.FAX, A.CUSTNMBR, A.PRCLEVEL,A.DEX_ROW_ID,
            //                                              A.PYMTRMID,A.CREATDDT, A.MODIFDT,A.INACTIVE, A.HOLD ,A.SLPRSNID
            //
            //FROM RM00101 AS A INNER JOIN RM00303 AS B ON A.SALSTERR=B.SALSTERR    
            //WHERE B.COUNTRY in (SELECT IntLocation FROM ALNIntegration)  
            //AND A.INACTIVE=0 AND HOLD=0
            //And A.MODIFDT  >= '" + _modificationDate.ToString(DateFormat) + "' and A.MODIFDT  < '" + DateTime.Now.AddDays(1).ToString(DateFormat) + "'";
            //            //AND (LEN(A.SALSTERR)=3 or LEN(A.SALSTERR)=4)

            string SelectCustomer = @"SELECT 
                                              A.CRLMTAMT,A.CUSTNAME, A.ADDRESS1, A.ADDRESS2, A.STATE,
                                              A.PHONE1, A.FAX, A.CUSTNMBR, A.PRCLEVEL,A.DEX_ROW_ID,
                                               A.PYMTRMID,A.CREATDDT, A.MODIFDT,A.INACTIVE, A.HOLD ,A.SLPRSNID,CASE WHEN ISNULL(T.TXDTLPCT,0) > 0 THEN 1 ELSE 0 END Tax,TXRGNNUM
FROM RM00101 AS A INNER JOIN RM00303 AS B ON A.SALSTERR=B.SALSTERR 
LEFT JOIN TX00201 T ON T.TAXDTLID = A.TAXSCHID
WHERE B.COUNTRY in (SELECT IntLocation FROM ALNIntegration)  
AND A.INACTIVE=0 AND HOLD=0
And A.MODIFDT  >= '" + _modificationDate.ToString(DateFormat) + "' and A.MODIFDT  < '" + DateTime.Now.AddDays(1).ToString(DateFormat) + "'";

            InCubeQuery CustomerQuery = new InCubeQuery(db_ERP, SelectCustomer);
            CustomerQuery.Execute();

            ClearProgress();
            SetProgressMax(CustomerQuery.GetDataTable().Rows.Count);
            err = CustomerQuery.FindFirst();

            while (err == InCubeErrors.Success)
            {
                ReportProgress("Updating Customers");



                CustomerQuery.GetField("PHONE1", ref field);
                string Phone = field.ToString().Trim();

                CustomerQuery.GetField("FAX", ref field);
                string Fax = field.ToString().Trim();

                CustomerQuery.GetField("HOLD", ref field);
                int OnHold = int.Parse(field.ToString());

                CustomerQuery.GetField("CUSTNMBR", ref field);
                string CustomerBarcode = field.ToString().Trim();

                CustomerQuery.GetField("CUSTNAME", ref field);
                string CustomerName = field.ToString().Trim();

                CustomerQuery.GetField("STATE", ref field);
                string CustomerCity = field.ToString().Trim();

                CustomerQuery.GetField("ADDRESS1", ref field);
                string CustomerAddress = field.ToString().Trim();

                CustomerQuery.GetField("ADDRESS2", ref field);
                string OutletAddress = field.ToString().Trim();

                //if (CustomerAddress != string.Empty && CustomerCity != string.Empty)
                //    CustomerAddress = CustomerCity + " - " + CustomerAddress;
                //else if (CustomerCity != string.Empty)
                //    CustomerAddress = CustomerCity;

                CustomerQuery.GetField("PRCLEVEL", ref field);
                string CustomerPRCLEVEL = field.ToString().Trim();

                CustomerQuery.GetField("PYMTRMID", ref field);
                string CustomerPYMTRMID = field.ToString().Trim();


                CustomerQuery.GetField("SLPRSNID", ref field);
                string CustomerType = field.ToString().ToLower().Trim();


                CustomerQuery.GetField("Tax", ref field);
                string Taxable = field.ToString().ToLower().Trim();


                CustomerQuery.GetField("TXRGNNUM", ref field);
                string Taxno = field.ToString().ToLower().Trim();

                string StreetID = AddCustomerGeographicalInfo("UAE", CustomerCity, CustomerCity, CustomerCity, CustomerCity);
                string CustomerID = GetFieldValue("Customer", "CustomerID", " CustomerCode='" + CustomerBarcode + "'", db_vms);// field.ToString();
                if (CustomerID.Trim() == "") CustomerID = GetFieldValue("Customer", "isnull(MAX(CustomerID),0)+1", db_vms);


                CustomerPYMTRMID = CustomerPYMTRMID.ToUpper().Replace("DAYS", "").Trim();
                CustomerPYMTRMID = CustomerPYMTRMID.ToUpper().Replace("CASH", "0").Trim();
                CustomerPYMTRMID = CustomerPYMTRMID.ToUpper().Replace("NET", "").Trim();
                CustomerPYMTRMID = CustomerPYMTRMID.ToUpper().Replace("C.O.D.", "0").Trim();
                CustomerPYMTRMID = CustomerPYMTRMID.ToUpper().Replace("C.O.D", "0").Trim();

                int Credit;
                string PaymentTermID = "1";

                if (CustomerType == "cash")
                    Credit = 1;
                else
                    Credit = 2;

                //if (CustomerPYMTRMID == "0")
                //{
                //    Credit = 1;
                //}

                if (CustomerPYMTRMID != "0")
                {
                    //Credit = 2;
                    decimal PaymentTermsValue = 30;

                    if (!decimal.TryParse(CustomerPYMTRMID, out PaymentTermsValue))
                    {
                        CustomerPYMTRMID = PaymentTermsValue.ToString();
                    }

                    err = ExistObject("PaymentTerm", "PaymentTermID", "SimplePeriodWidth = " + CustomerPYMTRMID, db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        PaymentTermID = GetFieldValue("PaymentTerm", "isnull(MAX(PaymentTermID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                        QueryBuilderObject.SetField("PaymentTermTypeID", "1");
                        QueryBuilderObject.SetField("SimplePeriodWidth", CustomerPYMTRMID);
                        QueryBuilderObject.SetField("SimplePeriodID", "1"); //Days
                        QueryBuilderObject.InsertQueryString("PaymentTerm", db_vms);

                        QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'Every " + CustomerPYMTRMID + " Days'");
                        QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms);
                    }
                }

                CustomerQuery.GetField("CRLMTAMT", ref field);
                decimal CustomerCREATDDT = decimal.Parse(field.ToString());
                if (CustomerCREATDDT > 99999999) CustomerCREATDDT = 99999999;

                int GroupID = -1;

                #region Get Customer Group

                string SelectCustomerGroup = "Select GroupID From CustomerGroupLanguage Where Description='" + CustomerPRCLEVEL + "'";
                InCubeQuery INVANCustomerGroupQuery = new InCubeQuery(db_vms, SelectCustomerGroup);
                INVANCustomerGroupQuery.Execute();
                err = INVANCustomerGroupQuery.FindFirst();

                if (err == InCubeErrors.Success)
                {
                    INVANCustomerGroupQuery.GetField(0, ref field);
                    GroupID = int.Parse(field.ToString());
                }
                else if (err == InCubeErrors.DBNoMoreRows)
                {
                    GroupID = int.Parse(GetFieldValue("CustomerGroup", "isnull(MAX(GroupID),0) + 1", db_vms));

                    QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                    QueryBuilderObject.InsertQueryString("CustomerGroup", db_vms);

                    QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + CustomerPRCLEVEL + "'");
                    QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);

                }
                INVANCustomerGroupQuery.Close();

                #endregion

                #region Customer
                err = ExistObject("Customer", "CustomerID", "CustomerID = " + CustomerID, db_vms);
                if (err == InCubeErrors.Success) // Exist Customer --- Update Query
                {
                    TOTALUPDATED++;

                    QueryBuilderObject.SetField("CustomerCode", "'" + CustomerBarcode + "'");
                    QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                    QueryBuilderObject.SetField("Fax", "'" + Fax + "'");
                    QueryBuilderObject.SetField("OnHold", OnHold.ToString());
                    QueryBuilderObject.SetField("StreetID", StreetID);
                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetDateField("UpdatedDate", DateTime.Now);

                    QueryBuilderObject.UpdateQueryString("Customer", " CustomerID = " + CustomerID, db_vms);
                }
                else if (err == InCubeErrors.DBNoMoreRows) // New Customer --- Insert Query
                {
                    TOTALINSERTED++;

                    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                    QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                    QueryBuilderObject.SetField("Fax", "'" + Fax + "'");
                    QueryBuilderObject.SetField("Email", "' '");
                    QueryBuilderObject.SetField("CustomerCode", "'" + CustomerBarcode + "'");
                    QueryBuilderObject.SetField("OnHold", OnHold.ToString());
                    QueryBuilderObject.SetField("StreetID", StreetID);
                    QueryBuilderObject.SetField("StreetAddress", "0");
                    QueryBuilderObject.SetField("InActive", "0");
                    QueryBuilderObject.SetField("New", "0");

                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetDateField("CreatedDate", DateTime.Now);

                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetDateField("UpdatedDate", DateTime.Now);
                    err = QueryBuilderObject.InsertQueryString("Customer", db_vms);
                }
                #endregion

                #region CustomerLanguage
                err = ExistObject("CustomerLanguage", "CustomerID", "CustomerID = " + CustomerID, db_vms);
                if (err == InCubeErrors.Success) // Exist CustomerLanguage --- Update Query
                {
                    QueryBuilderObject.SetField("Description", "'" + CustomerName + "'");
                    QueryBuilderObject.SetField("Address", "'" + CustomerAddress + "'");
                    QueryBuilderObject.UpdateQueryString("CustomerLanguage", "  CustomerID = " + CustomerID + " AND LanguageID = 1", db_vms);
                }
                else if (err == InCubeErrors.DBNoMoreRows) // New CustomerLanguage --- Insert Query
                {
                    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + CustomerName + "'");
                    QueryBuilderObject.SetField("Address", "'" + CustomerAddress + "'");
                    err = QueryBuilderObject.InsertQueryString("CustomerLanguage", db_vms);
                }
                #endregion

                #region Customer Outlet and language

                string CustomerOutletCode;
                //string SelectCustomerOutlet = "SELECT ltrim(rtrim(cast(CUSTNMBR as nvarchar(50)))) + ltrim(rtrim(cast(DEX_ROW_ID as nvarchar(50)))) as ADDRESSSCODE FROM RM00102 Where CUSTNMBR = '" + CustomerBarcode + "'";
                CustomerOutletCode = CustomerBarcode;
                //InCubeQuery CustomerOutletQuery = new InCubeQuery(db_Gp, SelectCustomerOutlet);
                //CustomerOutletQuery.Execute();
                //err = CustomerOutletQuery.FindFirst();

                //while (err == InCubeErrors.Success)
                //{
                //    CustomerOutletQuery.GetField("ADDRESSSCODE", ref field);
                //    CustomerOutletCode = field.ToString();

                string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerID = " + CustomerID + " AND CustomerCode = '" + CustomerOutletCode + "'", db_vms);
                if (!OutletID.Trim().Equals(string.Empty))
                {

                    QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                    QueryBuilderObject.SetField("Fax", "'" + Fax + "'");
                    QueryBuilderObject.SetField("Taxeable", Taxable.ToString());
                    QueryBuilderObject.SetField("TaxNumber", "'" + Taxno + "'");
                    QueryBuilderObject.SetField("CustomerTypeID", Credit.ToString()); //HardCoded -1- Cash -2- Credit
                    QueryBuilderObject.SetField("OnHold", OnHold.ToString());
                    QueryBuilderObject.SetField("StreetID", StreetID);
                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetDateField("UpdatedDate", DateTime.Now);

                    QueryBuilderObject.UpdateQueryString("CustomerOutlet", "  CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                }
                else
                {
                    OutletID = GetFieldValue("CustomerOutlet", "isnull(MAX(OutletID),0) + 1", "CustomerID = " + CustomerID, db_vms);


                    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                    QueryBuilderObject.SetField("OutletID", OutletID);
                    QueryBuilderObject.SetField("CustomerCode", "'" + CustomerOutletCode + "'");
                    QueryBuilderObject.SetField("Barcode", "'" + CustomerBarcode + "'");
                    QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                    QueryBuilderObject.SetField("Fax", "'" + Fax + "'");
                    QueryBuilderObject.SetField("Taxeable", Taxable.ToString());
                    QueryBuilderObject.SetField("TaxNumber", "'" + Taxno + "'");
                    QueryBuilderObject.SetField("Email", "' '");
                    QueryBuilderObject.SetField("CustomerTypeID", Credit.ToString());
                    QueryBuilderObject.SetField("CurrencyID", "1");
                    QueryBuilderObject.SetField("OnHold", OnHold.ToString());
                    QueryBuilderObject.SetField("GPSLatitude", "0");
                    QueryBuilderObject.SetField("GPSLongitude", "0");
                    QueryBuilderObject.SetField("StreetID", StreetID);
                    QueryBuilderObject.SetField("StreetAddress", "0");
                    QueryBuilderObject.SetField("InActive", "0");
                    QueryBuilderObject.SetField("Notes", "0");
                    QueryBuilderObject.SetField("SkipCreditCheck", "0");
                    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);

                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    err = QueryBuilderObject.InsertQueryString("CustomerOutlet", db_vms);
                }

                InCubeQuery DeleteCustomerGroup = new InCubeQuery(db_vms, "Delete From CustomerOutletGroup Where CustomerID = " + CustomerID + " AND OutletID = " + OutletID);
                DeleteCustomerGroup.ExecuteNonQuery();

                err = ExistObject("CustomerOutletGroup", "GroupID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                if (err != InCubeErrors.Success)
                {
                    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                    QueryBuilderObject.SetField("OutletID", OutletID);
                    QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                    err = QueryBuilderObject.InsertQueryString("CustomerOutletGroup", db_vms);
                }

                err = ExistObject("CustomerOutletLanguage", "OutletID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                if (err == InCubeErrors.Success)
                {
                    QueryBuilderObject.SetField("Description", "'" + CustomerName + "'");
                    QueryBuilderObject.SetField("Address", "'" + OutletAddress + "'");
                    QueryBuilderObject.UpdateQueryString("CustomerOutletLanguage", "  CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " AND LanguageID = 1", db_vms);
                }
                else
                {
                    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                    QueryBuilderObject.SetField("OutletID", OutletID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + CustomerName + "'");
                    QueryBuilderObject.SetField("Address", "'" + OutletAddress + "'");
                    err = QueryBuilderObject.InsertQueryString("CustomerOutletLanguage", db_vms);
                }

                int AccountID = 1;

                string BalanceStr = GetFieldValue("RM00103", "CUSTBLNC", "CUSTNMBR ='" + CustomerBarcode + "'", db_ERP);

                decimal Balance = 0;
                if (BalanceStr != string.Empty)
                {
                    Balance = decimal.Parse(BalanceStr);
                    if (Balance > 99999999) Balance = 99999999;
                }

                err = ExistObject("AccountCustOut", "AccountID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                if (err == InCubeErrors.Success)
                {
                    AccountID = int.Parse(GetFieldValue("AccountCustOut", "AccountID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms));

                    QueryBuilderObject.SetField("CreditLimit", CustomerCREATDDT.ToString());
                    QueryBuilderObject.SetField("Balance", Balance.ToString());
                    QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + AccountID.ToString(), db_vms);

                }
                else
                {
                    AccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));

                    QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                    QueryBuilderObject.SetField("AccountTypeID", "1");
                    QueryBuilderObject.SetField("CreditLimit", CustomerCREATDDT.ToString());
                    QueryBuilderObject.SetField("Balance", Balance.ToString());
                    QueryBuilderObject.SetField("GL", "0");
                    QueryBuilderObject.SetField("OrganizationID", "1");
                    QueryBuilderObject.SetField("CurrencyID", "1");
                    err = QueryBuilderObject.InsertQueryString("Account", db_vms);

                    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                    QueryBuilderObject.SetField("OutletID", OutletID);
                    QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                    err = QueryBuilderObject.InsertQueryString("AccountCustOut", db_vms);

                    QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + CustomerName.Trim() + " Account'");
                    err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
                }

                //    err = CustomerOutletQuery.FindNext();
                //}

                //CustomerOutletQuery.Close();

                #endregion

                err = CustomerQuery.FindNext();
            }

            CustomerQuery.Close();
            WriteMessage("\r\n");
            WriteMessage("<<< CUSTOMERS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        #endregion

        #region UpdateVehicles

        public override void UpdateWarehouse()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            string OrganizationID = "1";

            InCubeErrors err;
            object field = new object();

            if (!db_ERP.IsOpened())
            {
                WriteMessage("\r\n");
                WriteMessage("Cannot connect to GP database , please check the connection");
                return;
            }

            UpdateOrganization();

            string SelectWarehouse = @"SELECT    
                                                 LOCNCODE,
                                                 LOCNDSCR,
                                                 PHONE1,
                                                 COUNTRY,
                                                 ADDRESS1,
                                                 DEX_ROW_ID, 
                                                 ZIPCODE, 
                                                 STATE
 
                                                 FROM IV40700
                                                 
WHERE 
STATE IN (SELECT IntLocation FROM ALNIntegration) AND NOT LOCNCODE IN (SELECT ColdStore FROM ALNIntegration) 
AND (LEN(LOCNCODE)= 3 or LEN(LOCNCODE)= 4)";


            InCubeQuery WarehouseQuery = new InCubeQuery(db_ERP, SelectWarehouse);
            WarehouseQuery.Execute();
            ClearProgress();
            SetProgressMax(WarehouseQuery.GetDataTable().Rows.Count);
            err = WarehouseQuery.FindFirst();

            while (err == InCubeErrors.Success)
            {
                ReportProgress("Updating Vehicles");

                WarehouseQuery.GetField("LOCNCODE", ref field);
                string WarehouseCode = field.ToString().Trim();

                WarehouseQuery.GetField("LOCNDSCR", ref field);
                string WarehouceName = field.ToString().Trim();

                WarehouseQuery.GetField("PHONE1", ref field);
                string Phone = field.ToString().Trim();

                string Address = "";

                //WarehouseQuery.GetField("DEX_ROW_ID", ref field);
                string WarehouseID = GetFieldValue("Warehouse", "WarehouseID", " WarehouseCode='" + WarehouseCode + "'", db_vms);// field.ToString();
                if (WarehouseID.Trim() == "") WarehouseID = GetFieldValue("Warehouse", "isnull(MAX(WarehouseID),0)+1", db_vms);
                WarehouseQuery.GetField("ZIPCODE", ref field);
                string VehicleRegNum = field.ToString().Trim();

                WarehouseQuery.GetField("STATE", ref field);
                string Depot = field.ToString().Trim();

                OrganizationID = GetFieldValue("OrganizationLanguage", "OrganizationID", "Description = '" + Depot + "'", db_vms);
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

                string SalesmanCode = WarehouseCode;

                AddUpdateWarehouse(WarehouseID, WarehouseCode, WarehouceName, Phone, Address, VehicleRegNum, SalesmanCode, ref TOTALUPDATED, ref TOTALINSERTED, OrganizationID);

                err = WarehouseQuery.FindNext();
            }

            WarehouseQuery.Close();
            WriteMessage("\r\n");
            WriteMessage("<<< VEHICLES >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        private void AddUpdateWarehouse(string WarehouseID, string WarehouseCode, string WarehouceName, string Phone, string Address, string VehicleRegNum, string SalesmanCode, ref int TOTALUPDATED, ref int TOTALINSERTED, string OrganizationID)
        {
            InCubeErrors err;

            err = ExistObject("Warehouse", "WarehouseID", "WarehouseID = " + WarehouseID, db_vms);
            if (err == InCubeErrors.Success) // Exist Warehouse --- Update Query
            {
                TOTALUPDATED++;

                QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                QueryBuilderObject.SetField("Barcode", "'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);

                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.UpdateQueryString("Warehouse", " WarehouseID = " + WarehouseID, db_vms);

            }
            else if (err == InCubeErrors.DBNoMoreRows) // New Warehouse --- Insert Query
            {
                TOTALINSERTED++;

                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                QueryBuilderObject.SetField("Barcode", "'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("WarehouseCode", "'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.SetField("WarehouseTypeID", "2");
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetDateField("CreatedDate", DateTime.Now);
                QueryBuilderObject.SetField("Inactive", "0");
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetDateField("UpdatedDate", DateTime.Now);

                QueryBuilderObject.InsertQueryString("Warehouse", db_vms);

            }

            err = ExistObject("WarehouseLanguage", "WarehouseID", "WarehouseID = " + WarehouseID, db_vms);
            if (err == InCubeErrors.Success)
            {
                QueryBuilderObject.SetField("Description", "'" + WarehouceName + "'");
                QueryBuilderObject.SetField("Address", "'" + Address + "'");
                QueryBuilderObject.UpdateQueryString("WarehouseLanguage", " WarehouseID =" + WarehouseID + " AND LanguageID = 1", db_vms);
            }
            else if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + WarehouceName + "'");
                QueryBuilderObject.SetField("Address", "'" + Address + "'");
                QueryBuilderObject.InsertQueryString("WarehouseLanguage", db_vms);
            }

            #region WarehouseZone/Vehicle/VehicleSalesPerson

            err = ExistObject("WarehouseZone", "WarehouseID", "WarehouseID = " + WarehouseID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("ZoneID", "1");
                QueryBuilderObject.InsertQueryString("WarehouseZone", db_vms);
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

            err = ExistObject("Vehicle", "VehicleID", "VehicleID = " + WarehouseID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
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
            else
            {
                QueryBuilderObject.SetField("PlateNO", "'" + VehicleRegNum + "'");
                QueryBuilderObject.UpdateQueryString("Vehicle", "VehicleID = " + WarehouseID, db_vms);
            }

            string SalesPersonID = GetFieldValue("Employee", "EmployeeID", " EmployeeCode = '" + SalesmanCode + "'", db_vms);
            string VehicleID = GetFieldValue("Vehicle", "VehicleID", "VehicleID=" + WarehouseID, db_vms);

            if (!SalesPersonID.Trim().Equals(string.Empty) && !VehicleID.Trim().Equals(string.Empty))
            {
                InCubeQuery DeleteRouteCustomerQuery = new InCubeQuery(db_vms, "Delete From EmployeeVehicle where  VehicleID = " + WarehouseID + " or EmployeeID = " + SalesPersonID);
                DeleteRouteCustomerQuery.ExecuteNonQuery();

                err = ExistObject("EmployeeVehicle", "VehicleID", "VehicleID = " + WarehouseID + " AND EmployeeID = " + SalesPersonID, db_vms);
                if (err != InCubeErrors.Success)
                {
                    QueryBuilderObject.SetField("VehicleID", VehicleID);
                    QueryBuilderObject.SetField("EmployeeID", SalesPersonID);
                    QueryBuilderObject.InsertQueryString("EmployeeVehicle", db_vms);
                }
            }
            #endregion
        }

        #endregion

        #region UpdateWarehouse

        public override void UpdateMainWarehouse()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            string OrganizationID = "1";

            InCubeErrors err;
            object field = new object();

            if (!db_ERP.IsOpened())
            {
                WriteMessage("\r\n");
                WriteMessage("Cannot connect to GP database , please check the connection");
                return;
            }

            UpdateOrganization();

            string SelectWarehouse = @"SELECT    
                                                 LOCNCODE,
                                                 LOCNDSCR,
                                                 PHONE1,
                                                 COUNTRY,
                                                 ADDRESS1,
                                                 DEX_ROW_ID, 
                                                 ZIPCODE, 
                                                 STATE
 
                                                 FROM IV40700
                                                 
WHERE 
STATE IN (SELECT IntLocation FROM ALNIntegration) AND LOCNCODE IN (SELECT ColdStore FROM ALNIntegration) 
AND (LEN(LOCNCODE)=3 or LEN(LOCNCODE)=4)";


            InCubeQuery WarehouseQuery = new InCubeQuery(db_ERP, SelectWarehouse);
            WarehouseQuery.Execute();
            ClearProgress();
            SetProgressMax(WarehouseQuery.GetDataTable().Rows.Count);

            err = WarehouseQuery.FindFirst();

            while (err == InCubeErrors.Success)
            {
                ReportProgress("Updating Warehouse");

                WarehouseQuery.GetField("LOCNCODE", ref field);
                string WarehouseCode = field.ToString().Trim();

                WarehouseQuery.GetField("LOCNDSCR", ref field);
                string WarehouceName = field.ToString().Trim();

                WarehouseQuery.GetField("PHONE1", ref field);
                string Phone = field.ToString().Trim();

                string Address = "";

                //WarehouseQuery.GetField("DEX_ROW_ID", ref field);
                //string WarehouseID = field.ToString();
                string WarehouseID = GetFieldValue("Warehouse", "WarehouseID", " WarehouseCode='" + WarehouseCode + "'", db_vms);// field.ToString();
                if (WarehouseID.Trim() == "") WarehouseID = GetFieldValue("Warehouse", "isnull(MAX(WarehouseID),0)+1", db_vms);


                WarehouseQuery.GetField("ZIPCODE", ref field);
                string VehicleRegNum = field.ToString().Trim();

                WarehouseQuery.GetField("STATE", ref field);
                string Depot = field.ToString().Trim();

                OrganizationID = GetFieldValue("OrganizationLanguage", "OrganizationID", "Description = '" + Depot + "'", db_vms);
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

                string SalesmanCode = WarehouseCode;

                AddUpdateMainWarehouse(WarehouseID, WarehouseCode, WarehouceName, Phone, Address, VehicleRegNum, SalesmanCode, ref TOTALUPDATED, ref TOTALINSERTED, OrganizationID);

                err = WarehouseQuery.FindNext();
            }

            WarehouseQuery.Close();
            WriteMessage("\r\n");
            WriteMessage("<<< WAREHOUSE >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        private void AddUpdateMainWarehouse(string WarehouseID, string WarehouseCode, string WarehouceName, string Phone, string Address, string VehicleRegNum, string SalesmanCode, ref int TOTALUPDATED, ref int TOTALINSERTED, string OrganizationID)
        {
            InCubeErrors err;

            err = ExistObject("Warehouse", "WarehouseID", "WarehouseID = " + WarehouseID, db_vms);
            if (err == InCubeErrors.Success) // Exist Warehouse --- Update Query
            {
                TOTALUPDATED++;

                QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                QueryBuilderObject.SetField("Barcode", "'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);

                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetDateField("UpdatedDate", DateTime.Now);

                err = QueryBuilderObject.UpdateQueryString("Warehouse", " WarehouseID = " + WarehouseID, db_vms);

            }
            else if (err == InCubeErrors.DBNoMoreRows) // New Warehouse --- Insert Query
            {
                TOTALINSERTED++;

                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                QueryBuilderObject.SetField("Barcode", "'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("WarehouseCode", "'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.SetField("WarehouseTypeID", "1");
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetDateField("CreatedDate", DateTime.Now);
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetDateField("UpdatedDate", DateTime.Now);
                QueryBuilderObject.SetField("Inactive", "0");
                QueryBuilderObject.InsertQueryString("Warehouse", db_vms);

            }

            err = ExistObject("WarehouseLanguage", "WarehouseID", "WarehouseID = " + WarehouseID, db_vms);
            if (err == InCubeErrors.Success)
            {
                QueryBuilderObject.SetField("Description", "'" + WarehouceName + "'");
                QueryBuilderObject.SetField("Address", "'" + Address + "'");
                QueryBuilderObject.UpdateQueryString("WarehouseLanguage", " WarehouseID =" + WarehouseID + " AND LanguageID = 1", db_vms);
            }
            else if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + WarehouceName + "'");
                QueryBuilderObject.SetField("Address", "'" + Address + "'");
                QueryBuilderObject.InsertQueryString("WarehouseLanguage", db_vms);
            }

            #region WarehouseZone

            err = ExistObject("WarehouseZone", "WarehouseID", "WarehouseID = " + WarehouseID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("ZoneID", "1");
                QueryBuilderObject.InsertQueryString("WarehouseZone", db_vms);
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

            #endregion
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

            if (!db_ERP.IsOpened())
            {
                WriteMessage("\r\n");
                WriteMessage("Cannot connect to GP database , please check the connection");
                return;
            }

            UpdateOrganization();

            string SelectSalesperson = @"SELECT    
                                                 LOCNCODE,
                                                 ADDRESS1,
                                                 ZIPCODE, 
                                                 STATE,
                                                 DEX_ROW_ID,
                                                 ADDRESS2,
                                                 PHONE1,
                                                 PHONE2

                                                 FROM IV40700

                                                 WHERE 
                                                 STATE IN (SELECT IntLocation FROM ALNIntegration) 
                                                 AND NOT LOCNCODE IN (SELECT ColdStore FROM ALNIntegration) 
                                                 AND (LEN(LOCNCODE)=3 or LEN(LOCNCODE)=4)";

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

            InCubeQuery SalespersonQuery = new InCubeQuery(db_ERP, SelectSalesperson);
            SalespersonQuery.Execute();

            ClearProgress();
            SetProgressMax(SalespersonQuery.GetDataTable().Rows.Count);

            err = SalespersonQuery.FindFirst();

            while (err == InCubeErrors.Success)
            {
                ReportProgress("Updating Salesperon");

                SalespersonQuery.GetField("LOCNCODE", ref field);
                string SalespersonCode = field.ToString().Trim();

                // SalespersonQuery.GetField("DEX_ROW_ID", ref field);
                //string SalespersonID = field.ToString().Trim();
                string SalespersonID = GetFieldValue("Employee", "EmployeeID", " EmployeeCode='" + SalespersonCode + "' and InActive=0", db_vms);// field.ToString();
                if (SalespersonID.Trim() == "") SalespersonID = GetFieldValue("Employee", "isnull(MAX(EmployeeID),0)+1", db_vms);

                if (SalespersonCode.Length == 3)
                {
                    DivisionID = "1";
                }
                else if (SalespersonCode.Length == 4)
                {
                    DivisionID = "2";
                }

                SalespersonQuery.GetField("ADDRESS1", ref field);
                string SalespersonName = field.ToString().Trim();

                SalespersonQuery.GetField("ADDRESS2", ref field);
                string SupervisorName = field.ToString().Trim();

                SalespersonQuery.GetField("PHONE2", ref field);
                string SupervisorPhone = field.ToString().Trim();
                long SV_Phone = 0;
                if (long.TryParse(SupervisorPhone, out SV_Phone) && SV_Phone == 0)
                    SupervisorPhone = "";

                SalespersonQuery.GetField("PHONE1", ref field);
                string Phone = field.ToString().Trim();
                long SM_Phone = 0;
                if (long.TryParse(Phone, out SM_Phone) && SM_Phone == 0)
                    Phone = "";

                string Address = "";

                SalespersonQuery.GetField("STATE", ref field);
                OrganizationID = GetFieldValue("OrganizationLanguage", "OrganizationID", "Description = '" + field.ToString().Trim() + "'", db_vms);

                AddUpdateSalesperson(SalespersonID, SalespersonCode, SalespersonName, Phone, Address, ref TOTALUPDATED, ref TOTALINSERTED, DivisionID, OrganizationID, SupervisorName, SupervisorPhone);

                err = SalespersonQuery.FindNext();
            }

            SalespersonQuery.Close();
            WriteMessage("\r\n");
            WriteMessage("<<< SALESPERSON >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        private void AddUpdateSalesperson(string SalespersonID, string SalespersonCode, string SalespersonName, string Phone, string Address, ref int TOTALUPDATED, ref int TOTALINSERTED, string DivisionID, string OrganizationID, string SupervisorName, string SupervisorPhone)
        {
            InCubeErrors err;
            err = ExistObject("Employee", "EmployeeID", "EmployeeID = " + SalespersonID, db_vms);
            if (err != InCubeErrors.Success)// New Salesperon --- Insert Query
            {
                TOTALINSERTED++;

                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                QueryBuilderObject.SetField("EmployeeCode", "'" + SalespersonCode + "'");
                QueryBuilderObject.SetField("NationalIDNumber", "0");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.SetField("InActive", "0");
                QueryBuilderObject.SetField("OnHold", "0");
                QueryBuilderObject.SetField("EmployeeTypeID", "2");

                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.InsertQueryString("Employee", db_vms);
            }
            else
            {

                QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.SetField("EmployeeTypeID", "2");

                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.UpdateQueryString("Employee", "EmployeeID=" + SalespersonID, db_vms);
            }

            err = ExistObject("EmployeeLanguage", "EmployeeID", "EmployeeID = " + SalespersonID, db_vms);
            if (err == InCubeErrors.Success)
            {
                TOTALUPDATED++;
                QueryBuilderObject.SetField("Description", "'" + SalespersonName + "'");
                QueryBuilderObject.SetField("Address", "'" + Address + "'");
                QueryBuilderObject.UpdateQueryString("EmployeeLanguage", "EmployeeID = " + SalespersonID, db_vms);
            }
            else if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + SalespersonName + "'");
                QueryBuilderObject.SetField("Address", "'" + Address + "'");
                QueryBuilderObject.InsertQueryString("EmployeeLanguage", db_vms);
            }


            string operatorID = GetFieldValue("EmployeeOperator", "OperatorID", "EmployeeID = " + SalespersonID, db_vms);
            if (operatorID == string.Empty)
            {
                operatorID = GetFieldValue("Operator", "MAX(OperatorID)+1", db_vms);
            }
            err = ExistObject("Operator", "OperatorID", "OperatorID = " + operatorID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("OperatorID", operatorID);
                QueryBuilderObject.SetField("OperatorName", "'" + SalespersonCode + "'");
                QueryBuilderObject.SetField("FrontOffice", "1");
                QueryBuilderObject.SetField("LoginTypeID", "1");
                QueryBuilderObject.InsertQueryString("Operator", db_vms);
            }
            err = ExistObject("EmployeeOperator", "EmployeeID", "EmployeeID = " + SalespersonID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("OperatorID", operatorID);
                QueryBuilderObject.InsertQueryString("EmployeeOperator", db_vms);
            }

            //err = ExistObject("EmployeeOperator", "EmployeeID", "EmployeeID = " + SalespersonID, db_vms);
            //if (err == InCubeErrors.DBNoMoreRows)
            //{

            //    QueryBuilderObject.SetField("EmployeeID", SalespersonID);
            //    QueryBuilderObject.SetField("OperatorID", SalespersonID);
            //    QueryBuilderObject.InsertQueryString("EmployeeOperator", db_vms);

            //    QueryBuilderObject.SetField("OperatorID", SalespersonID);
            //    QueryBuilderObject.SetField("OperatorName", "'" + SalespersonCode + "'");
            //    QueryBuilderObject.SetField("LoginTypeID", "1");
            //    //QueryBuilderObject.SetField("OperatorPassword", "'" + SalespersonCode + "'");
            //    //QueryBuilderObject.SetField("Status", "0");
            //    QueryBuilderObject.SetField("FrontOffice", "1");
            //    QueryBuilderObject.InsertQueryString("Operator", db_vms);
            //}

            #region Supervisor

            if (SupervisorName != string.Empty)
            {
                string DeleteSupervisor = "delete from EmployeeSupervisor where EmployeeID = " + SalespersonID;
                QueryBuilderObject.RunQuery(DeleteSupervisor, db_vms);

                string SupervisorID = GetFieldValue("EmployeeLanguage", "EmployeeID", "Description = '" + SupervisorName + "' AND LanguageID = 1", db_vms);
                if (SupervisorID == string.Empty)
                {
                    string MaxDXrow = GetFieldValue("IV40700", "Max(DEX_ROW_ID)", " STATE IN (SELECT IntLocation FROM ALNIntegration) AND NOT LOCNCODE IN (SELECT ColdStore FROM ALNIntegration) AND (LEN(LOCNCODE)=3 or LEN(LOCNCODE)=4) ", db_ERP);

                    int _empID = int.Parse(MaxDXrow) + int.Parse(SalespersonID);
                    SupervisorID = _empID.ToString();

                    QueryBuilderObject.SetField("EmployeeID", SupervisorID);
                    QueryBuilderObject.SetField("Mobile", "'" + SupervisorPhone + "'");
                    QueryBuilderObject.SetField("EmployeeCode", "'" + SupervisorID + "'");
                    QueryBuilderObject.SetField("NationalIDNumber", "0");
                    QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                    QueryBuilderObject.SetField("InActive", "0");
                    QueryBuilderObject.SetField("OnHold", "0");
                    QueryBuilderObject.SetField("EmployeeTypeID", "4");
                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetDateField("CreatedDate", DateTime.Now);
                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetDateField("UpdatedDate", DateTime.Now);
                    QueryBuilderObject.InsertQueryString("Employee", db_vms);

                    QueryBuilderObject.SetField("EmployeeID", SupervisorID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + SupervisorName + "'");
                    QueryBuilderObject.SetField("Address", "''");
                    QueryBuilderObject.InsertQueryString("EmployeeLanguage", db_vms);
                }
                else
                {
                    QueryBuilderObject.SetField("Mobile", "'" + SupervisorPhone + "'");
                    QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                    QueryBuilderObject.SetField("EmployeeTypeID", "4");
                    QueryBuilderObject.UpdateQueryString("Employee", " EmployeeID = " + SupervisorID, db_vms);
                }

                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("SupervisorID", SupervisorID);
                QueryBuilderObject.InsertQueryString("EmployeeSupervisor", db_vms);
            }

            #endregion

            err = ExistObject("EmployeeDivision", "EmployeeID", "EmployeeID = " + SalespersonID + " AND DivisionID = " + DivisionID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("DivisionID", DivisionID);
                QueryBuilderObject.SetField("IsDefault", "0");
                QueryBuilderObject.InsertQueryString("EmployeeDivision", db_vms);
            }

            err = ExistObject("EmployeeOrganization", "EmployeeID", "EmployeeID = " + SalespersonID + " and OrganizationID=" + OrganizationID + "", db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.InsertQueryString("EmployeeOrganization", db_vms);
            }

            err = ExistObject("EmployeeOrganization", "EmployeeID", "EmployeeID = " + SalespersonID + " and OrganizationID=1", db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("OrganizationID", "1");
                QueryBuilderObject.InsertQueryString("EmployeeOrganization", db_vms);
            }

            int AccountID = 1;

            err = ExistObject("AccountEmp", "AccountID", "EmployeeID = " + SalespersonID, db_vms);
            if (err != InCubeErrors.Success)
            {
                AccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("AccountTypeID", "2");
                QueryBuilderObject.SetField("CreditLimit", "0");
                QueryBuilderObject.SetField("Balance", "0");
                QueryBuilderObject.SetField("GL", "0");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.SetField("CurrencyID", "1");
                QueryBuilderObject.InsertQueryString("Account", db_vms);

                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.InsertQueryString("AccountEmp", db_vms);

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + SalespersonName.Trim() + " Account'");
                QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
            }
        }

        #endregion

        #region UpdatePrice

        public override void UpdatePrice()
        {
            int TOTALUPDATED = 0;

            InCubeErrors err;
            object field = new object();

            if (!db_ERP.IsOpened())
            {
                WriteMessage("\r\n");
                WriteMessage("Cannot connect to GP database , please check the connection");
                return;
            }

            InCubeQuery DeletePriceDefinitionQuery = new InCubeQuery(db_vms, "Delete From PriceDefinition");
            DeletePriceDefinitionQuery.ExecuteNonQuery();

            // for Al-Ain, the dfault price list is designated as "STANDARD"
            UpdatePriceList("STANDARD", "1", true, ref TOTALUPDATED);

            string SelectGroup = @"Select GroupID,Description from CustomerGroupLanguage";
            InCubeQuery GroupQuery = new InCubeQuery(db_vms, SelectGroup);
            GroupQuery.Execute();
            err = GroupQuery.FindFirst();

            while (err == InCubeErrors.Success)
            {


                GroupQuery.GetField("GroupID", ref field);
                string GroupID = field.ToString().Trim();

                GroupQuery.GetField("Description", ref field);
                string GroupName = field.ToString().Trim();

                UpdatePriceList(GroupName, GroupID, false, ref TOTALUPDATED);

                err = GroupQuery.FindNext();
            }
            GroupQuery.Close();

            WriteMessage("\r\n");
            WriteMessage("<<< PRICE >>> Total Updated = " + TOTALUPDATED);
        }
        private void UpdatePriceList(string priceListName, string GroupID, bool defaultList, ref int TOTALUPDATED)
        {
            object field = new object();
            InCubeErrors err;


            string priceqry = @"SELECT ITEMNMBR,
, UOMPRICE, QTYBSUOM,UOFM FROM IV00108 WHERE ltrim(rtrim(PRCLEVEL))='" + priceListName + @"'
                    AND ITEMNMBR IN (SELECT ITEMNMBR FROM IV00101 I WHERE I.ITEMTYPE<>2 AND (I.ITMGEDSC='3AADF' OR I.ITMGEDSC='3AAPF'))";

            string PriceQry = @"SELECT P.ITEMNMBR,P.PRCLEVEL, P.UOMPRICE, P.QTYBSUOM,P.UOFM
,ISNULL(V.TXDTLPCT,0) VAT 
FROM IV00108 P
INNER JOIN IV00101 I ON I.ITEMNMBR = P.ITEMNMBR
LEFT JOIN TX00201 V ON V.TXDTLBSE = I.TAXOPTNS and V.TAXDTLID = 'VATSLS+5'
 WHERE ltrim(rtrim(P.PRCLEVEL))='" + priceListName + @"'
AND (I.ITMGEDSC='3AADF' OR I.ITMGEDSC='3AAPF')";

            InCubeQuery ItemPriceQuery = new InCubeQuery(db_ERP, PriceQry);
            ItemPriceQuery.Execute();

            ClearProgress();
            SetProgressMax(ItemPriceQuery.GetDataTable().Rows.Count);

            string PriceListID = "1";

            err = ExistObject("PriceListLanguage", "Description", " Description = '" + priceListName + "'", db_vms);
            if (err == InCubeErrors.Success)
            {
                PriceListID = GetFieldValue("PriceListLanguage", "PriceListID", " Description = '" + priceListName + "'", db_vms);
            }
            else
            {
                PriceListID = GetFieldValue("PriceList", "ISNULL(MAX(PriceListID),0) + 1", db_vms);

                QueryBuilderObject.SetField("PriceListID", PriceListID);
                QueryBuilderObject.SetField("PriceListCode", PriceListID);
                QueryBuilderObject.SetDateField("StartDate", DateTime.Now.Date);
                QueryBuilderObject.SetDateField("EndDate", DateTime.Now.Date.AddYears(10));
                QueryBuilderObject.SetField("Priority", "1");
                QueryBuilderObject.SetField("PriceListTypeID", "1");
                QueryBuilderObject.SetField("IsDeleted", "0");
                QueryBuilderObject.SetField("OrganizationID", "1");
                QueryBuilderObject.SetField("StockStatusID", "-1");
                QueryBuilderObject.InsertQueryString("PriceList", db_vms);

                QueryBuilderObject.SetField("PriceListID", PriceListID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + priceListName + "'");
                QueryBuilderObject.InsertQueryString("PriceListLanguage", db_vms);
            }

            if (defaultList)
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

            err = ItemPriceQuery.FindFirst();

            while (err == InCubeErrors.Success)
            {
                ReportProgress("Updating Price (" + priceListName + ")");

                TOTALUPDATED++;

                ItemPriceQuery.GetField("ITEMNMBR", ref field);
                string ItemCode = field.ToString();

                ItemPriceQuery.GetField("UOMPRICE", ref field);
                decimal Price = Math.Round(decimal.Parse(field.ToString()), 4);

                ItemPriceQuery.GetField("UOFM", ref field);
                string PackCode = field.ToString();

                ItemPriceQuery.GetField("VAT", ref field);
                string Tax = field.ToString();

                err = ExistObject("GroupPrice", "GroupID", " GroupID = " + GroupID + " AND PriceListID = " + PriceListID, db_vms);
                if (err != InCubeErrors.Success)
                {
                    QueryBuilderObject.SetField("GroupID", GroupID);
                    QueryBuilderObject.SetField("PriceListID", PriceListID);
                    QueryBuilderObject.InsertQueryString("GroupPrice", db_vms);
                }

                string PackID = GetFieldValue("Pack inner join item on pack.itemid = item.itemid", "pack.PackID", " item.itemcode = '" + ItemCode.ToString().Trim() + "' and item.inactive=0", db_vms);
                if (!PackID.Equals(string.Empty))
                {
                    int PriceDefinitionID = 1;
                    string currentPrice = GetFieldValue("PriceDefinition", "Price", "PackID = " + PackID + " AND PriceListID = " + PriceListID, db_vms);
                    if (currentPrice.Equals(string.Empty))
                    {
                        // if there is no default price level, then always insert one.
                        PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms));

                        QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID.ToString());
                        QueryBuilderObject.SetField("QuantityRangeID", "1");
                        QueryBuilderObject.SetField("PackID", PackID);
                        QueryBuilderObject.SetField("CurrencyID", "1");
                        QueryBuilderObject.SetField("Tax", Tax);
                        QueryBuilderObject.SetField("Price", Price.ToString());
                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("MinPrice", "-1");
                        QueryBuilderObject.SetField("MaxPrice", "-1");

                        err = QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);
                        if (err != InCubeErrors.Success)
                        {

                        }
                    }
                    else
                    {
                        PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "PriceDefinitionID", "PackID = " + PackID + " AND PriceListID = " + PriceListID, db_vms));

                        if (!currentPrice.Equals(Price.ToString()))
                        {
                            QueryBuilderObject.SetField("Price", Price.ToString());
                            err = QueryBuilderObject.UpdateQueryString("PriceDefinition", "PackID = " + PackID + "AND Tax= " + Tax + " AND PriceListID = " + PriceListID + " AND PriceDefinitionID = " + PriceDefinitionID, db_vms);
                            if (err != InCubeErrors.Success)
                            {

                            }
                        }
                    }
                }
                err = ItemPriceQuery.FindNext();
            }
            ItemPriceQuery.Close();
        }

        #endregion

        #region UpdareBalanceCreditLimit

        public override void UpdateBalanceCreditLimit()
        {
            if (!db_ERP.IsOpened())
            {
                WriteMessage("\r\n");
                WriteMessage("Cannot connect to GP database , please check the connection");
                return;
            }

            #region Customer

            int TOTALUPDATED = 0;

            InCubeErrors err;
            object field = new object();

            string SelectBalance = @"SELECT  RM00101.CUSTNMBR, RM00101.CRLMTAMT,
                                              RM00103.CUSTBLNC FROM  RM00101 
                                              INNER JOIN RM00103 ON RM00101.CUSTNMBR = RM00103.CUSTNMBR";

            InCubeQuery BalanceQuery = new InCubeQuery(db_ERP, SelectBalance);
            BalanceQuery.Execute();

            ClearProgress();
            SetProgressMax(BalanceQuery.GetDataTable().Rows.Count);

            err = BalanceQuery.FindFirst();

            while (err == InCubeErrors.Success)
            {
                ReportProgress("Updating Balance");

                BalanceQuery.GetField("CUSTNMBR", ref field);
                string CustomerCode = field.ToString();

                BalanceQuery.GetField("CRLMTAMT", ref field);

                decimal CustomerCREATDDT = 0;


                CustomerCREATDDT = decimal.Parse(field.ToString());


                if (CustomerCREATDDT > 99999999) CustomerCREATDDT = 99999999;

                decimal Balance = 0;
                string BalanceStr = GetFieldValue("RM00103", "CUSTBLNC", "CUSTNMBR ='" + CustomerCode + "'", db_ERP);
                if (BalanceStr != string.Empty)
                {
                    Balance = decimal.Parse(BalanceStr);
                    if (Balance > 99999999) Balance = 99999999;
                }

                TOTALUPDATED++;

                string CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", " Barcode = '" + CustomerCode.ToString() + "'", db_vms);
                string OutletID = GetFieldValue("CustomerOutlet", "OutletID", " Barcode = '" + CustomerCode.ToString() + "'", db_vms);

                string AccountID = GetFieldValue("AccountCustOut", "AccountID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);

                if (AccountID != string.Empty)
                {
                    QueryBuilderObject.SetField("Balance", Balance.ToString());
                    QueryBuilderObject.SetField("CreditLimit", CustomerCREATDDT.ToString());
                    QueryBuilderObject.UpdateQueryString("Account", " AccountID = " + AccountID.ToString(), db_vms);
                }

                err = BalanceQuery.FindNext();
            }

            BalanceQuery.Close();
            WriteMessage("\r\n");
            WriteMessage("<<< CUSTOMER BALANCE >>> Total Updated = " + TOTALUPDATED);
            #endregion

            #region Salesperson

            QueryBuilderObject.SetField("CreditLimit", "0");
            QueryBuilderObject.SetField("Balance", "0");
            QueryBuilderObject.UpdateQueryString("Account", "AccountTypeID = 2", db_vms);

            /*            TOTALUPDATED = 0;
                        string SelectSalesPersonBalance = @"SELECT CustomerOutlet.Barcode AS CustomerCode, Employee.EmployeeCode, AccountCustOutDivEmp.CustomerID, AccountCustOutDivEmp.OutletID, 
                                  AccountCustOutDivEmp.EmployeeID, AccountCustOutDivEmp.AccountID
                                  FROM AccountCustOutDivEmp INNER JOIN
                                  Account ON AccountCustOutDivEmp.AccountID = Account.AccountID INNER JOIN
                                  CustomerOutlet ON AccountCustOutDivEmp.CustomerID = CustomerOutlet.CustomerID AND 
                                  AccountCustOutDivEmp.OutletID = CustomerOutlet.OutletID INNER JOIN
                                  Employee ON AccountCustOutDivEmp.EmployeeID = Employee.EmployeeID";
                        InCubeQuery SalesPersonBalanceQuery = new InCubeQuery(db_vms, SelectSalesPersonBalance);
                        SalesPersonBalanceQuery.Execute();
                        ClearProgress();
                        SetProgressMax(SalesPersonBalanceQuery.GetDataTable().Rows.Count;
                        err = SalesPersonBalanceQuery.FindFirst();
                        while (err == InCubeErrors.Success)
                        {
                            ReportProgress();
                            IntegrationForm.lblProgress.Text = "Updating Salesperson Balance" + " " + IntegrationForm.progressBar1.Value + " / " + SetProgressMax(;
                            
                            SalesPersonBalanceQuery.GetField(0, ref field);
                            string CustomerCode = field.ToString();
                            SalesPersonBalanceQuery.GetField(1, ref field);
                            string EmployeeCode = field.ToString();
                            SalesPersonBalanceQuery.GetField(2, ref field);
                            string CustomerID = field.ToString();
                            SalesPersonBalanceQuery.GetField(3, ref field);
                            string OutletID = field.ToString();
                            SalesPersonBalanceQuery.GetField(4, ref field);
                            string SalespersonID = field.ToString();
                            SalesPersonBalanceQuery.GetField(5, ref field);
                            string AccountID = field.ToString();
                            decimal totalInvoice, totalReturn, TotalPayment, Balance;
                            totalInvoice = 0;
                            totalReturn = 0;
                            TotalPayment = 0;
                            Balance = 0;
                            SelectSalesPersonBalance = @"SELECT RM20101.RMDTYPAL, SUM(RM20101.CURTRXAM) AS Expr1 FROM RM20101
            INNER JOIN SOP30200 ON RM20101.CUSTNMBR = SOP30200.CUSTNMBR AND RM20101.DOCNUMBR = SOP30200.SOPNUMBE
            GROUP BY RM20101.SLPRSNID, RM20101.CUSTNMBR, RM20101.RMDTYPAL
            HAVING (RM20101.RMDTYPAL < 10) AND (RM20101.SLPRSNID = '" + SalespersonID + "') AND (RM20101.CUSTNMBR = '" + CustomerCode + "') AND (SUM(RM20101.CURTRXAM) > 0)";
                            InCubeQuery Query = new InCubeQuery(db_Gp, SelectSalesPersonBalance);
                            Query.Execute();
                            err = Query.FindFirst();
                            while (err == InCubeErrors.Success)
                            {
                                Query.GetField(0, ref field);
                                int RMDTYPAL = int.Parse(field.ToString());
                                Query.GetField(1, ref field);
                                decimal value = decimal.Parse(field.ToString());
                                if (RMDTYPAL < 7) totalInvoice += value;
                                if (RMDTYPAL == 8) totalReturn += value;
                                err = Query.FindNext();
                            }
                            Query.Close();
                            Balance = totalInvoice;
                            SelectSalesPersonBalance = @"SELECT RM20101.RMDTYPAL, SUM(RM20101.CURTRXAM) AS Expr1
                                                         FROM RM00101 INNER JOIN
                                                         RM20101 ON RM00101.CUSTNMBR = RM20101.CUSTNMBR
                                                         GROUP BY RM00101.CUSTNMBR, RM00101.SLPRSNID, RM20101.RMDTYPAL
                                                         HAVING (RM00101.SLPRSNID = '" + SalespersonID + "') AND  (RM00101.CUSTNMBR = '" + CustomerCode + "')  AND  (SUM(RM20101.CURTRXAM) > 0) AND (RM20101.RMDTYPAL = 9 OR   RM20101.RMDTYPAL = 7)";
                            Query = new InCubeQuery(db_Gp, SelectSalesPersonBalance);
                            Query.Execute();

                            err = Query.FindFirst();

                            while (err == InCubeErrors.Success)
                            {
                                Query.GetField(1, ref field);
                                decimal value = decimal.Parse(field.ToString());

                                TotalPayment += value;

                                err = Query.FindNext();
                            }
                            Query.Close();

                            Balance = Balance - totalReturn - TotalPayment;

                            QueryBuilderObject.SetField("Balance", Balance.ToString());
                            QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + AccountID, db_vms);
                            err = SalesPersonBalanceQuery.FindNext();
                        }

                        SalesPersonBalanceQuery.Close();
                        WriteMessage("\r\n");
                        WriteMessage("<<< CUSTOMER SALESPERSON BALANCE >>> Total Updated = " + TOTALUPDATED);
            */
            #endregion
        }

        #endregion

        #region UpdateInvoiceAmount

        public override void UpdateCreditInvoiceAmount()
        {
            int TOTALUPDATED = 0;

            InCubeErrors err;
            object field = new object();

            if (!db_ERP.IsOpened())
            {
                WriteMessage("\r\n");
                WriteMessage("Cannot connect to GP database , please check the connection");
                return;
            }

            string SelectInvoices = @"SELECT
T.TransactionID,
T.RemainingAmount

FROM [Transaction] T

inner join customeroutlet C on T.customerid = c.customerid and T.outletid = c.outletid
where (T.TransactionTypeID = 1 or T.TransactionTypeID = 3) and C.CustomerTypeID = 2";

            InCubeQuery InvoicesQuery = new InCubeQuery(db_vms, SelectInvoices);
            InvoicesQuery.Execute();

            ClearProgress();
            SetProgressMax(InvoicesQuery.GetDataTable().Rows.Count);

            err = InvoicesQuery.FindFirst();

            while (err == InCubeErrors.Success)
            {

                ReportProgress("Updating Invoices Amount");

                InvoicesQuery.GetField("TransactionID", ref field);
                string SalesTransactionID = field.ToString();

                InvoicesQuery.GetField("RemainingAmount", ref field);
                string RemainingAmount = field.ToString();

                err = ExistObject("RM20101", "DOCNUMBR", "DOCNUMBR = '" + SalesTransactionID + "'", db_ERP);
                if (err == InCubeErrors.Success)
                {
                    TOTALUPDATED++;
                    string Ramount = GetFieldValue("RM20101", "CURTRXAM", "DOCNUMBR = '" + SalesTransactionID + "'", db_ERP);
                    if (Ramount.Equals(string.Empty))
                    {
                        Ramount = "0";
                    }
                    QueryBuilderObject.SetField("RemainingAmount", Ramount);
                    QueryBuilderObject.UpdateQueryString("[Transaction]", "TransactionID = '" + SalesTransactionID.ToString().Trim() + "'", db_vms);
                }

                err = InvoicesQuery.FindNext();
            }
            InvoicesQuery.Close();

            WriteMessage("\r\n");
            WriteMessage("<<< UPDATED INVOICES >>> Total Updated = " + TOTALUPDATED);
        }

        #endregion

        #region UpdateInvoice

        public override void UpdateInvoice()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            InCubeErrors err;
            object field = new object();

            if (!db_ERP.IsOpened())
            {
                WriteMessage("\r\n");
                WriteMessage("Cannot connect to GP database , please check the connection");
                return;
            }

            string SelectInvoices = @"SELECT     
SOP30200.SOPTYPE, 
SOP30200.SOPNUMBE, 
SOP30200.ORIGNUMB, 
SOP30200.DOCID, 
SOP30200.DOCDATE, 
SOP30200.LOCNCODE,
ltrim(rtrim(cast(RM00102.CUSTNMBR as nvarchar(50)))) + ltrim(rtrim(cast(RM00102.DEX_ROW_ID as nvarchar(50)))) as ADDRESSSCODE, 
SOP30200.SUBTOTAL, 
SOP30200.SLPRSNID, 
RM20101.CURTRXAM, 
SOP30200.CUSTNMBR
 
FROM SOP30200 INNER JOIN  RM20101 ON SOP30200.SOPNUMBE = RM20101.DOCNUMBR 
INNER JOIN RM00101 ON SOP30200.CUSTNMBR = RM00101.CUSTNMBR 
INNER JOIN RM00102 ON SOP30200.CUSTNMBR = RM00102.CUSTNMBR AND SOP30200.PRSTADCD = RM00102.ADRSCODE

WHERE  (SOP30200.SOPTYPE = 3) AND (RM20101.CURTRXAM > 0) 
and (RM20101.RMDTYPAL = 1) and (RM00101.SHIPMTHD = 'SBU5')";   // RM00101.SHIPMTHD = 'SBU5' Should be Changed.

            InCubeQuery InvoicesQuery = new InCubeQuery(db_ERP, SelectInvoices);
            InvoicesQuery.Execute();

            ClearProgress();
            SetProgressMax(InvoicesQuery.GetDataTable().Rows.Count);

            err = InvoicesQuery.FindFirst();

            while (err == InCubeErrors.Success)
            {
                ReportProgress("Updating Invoices");

                InvoicesQuery.GetField(6, ref field);
                string CustomerNumber = field.ToString().Trim();

                InvoicesQuery.GetField(8, ref field);
                string SalesID = field.ToString().Trim();

                InvoicesQuery.GetField(7, ref field);
                string Total = field.ToString();

                InvoicesQuery.GetField(1, ref field);
                string DocNumber = field.ToString().Trim();

                InvoicesQuery.GetField(4, ref field);
                DateTime Date = DateTime.Parse(field.ToString());

                InvoicesQuery.GetField(9, ref field);
                string Ramount = field.ToString();

                InvoicesQuery.GetField(10, ref field);
                string CustomerCode = field.ToString().Trim();

                err = ExistObject("[Transaction]", "TransactionID", "TransactionID ='" + DocNumber + "'", db_vms);
                if (err == InCubeErrors.Success)
                {
                    err = InvoicesQuery.FindNext();
                    continue;
                }


                string CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode = '" + CustomerNumber + "' AND Barcode = '" + CustomerCode + "'", db_vms);
                string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerCode = '" + CustomerNumber + "' AND Barcode = '" + CustomerCode + "'", db_vms);

                if (CustomerID == string.Empty)
                {
                    err = InvoicesQuery.FindNext();
                    continue;
                }

                string SalespersonID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode ='" + SalesID + "'", db_vms);

                if (SalespersonID == string.Empty)
                {
                    err = InvoicesQuery.FindNext();
                    continue;
                }

                TOTALINSERTED++;

                QueryBuilderObject.SetField("CustomerID", CustomerID);
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("TransactionID", "'" + DocNumber + "'");
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("TransactionDate", "'" + Date.ToString("dd/MMM/yyyy") + "'");
                QueryBuilderObject.SetField("TransactionTypeID", "1");
                QueryBuilderObject.SetField("Discount", "0");
                QueryBuilderObject.SetField("Synchronized", "1");
                QueryBuilderObject.SetField("RemainingAmount", Ramount);
                QueryBuilderObject.SetField("Grosstotal", Total);
                QueryBuilderObject.SetField("Nettotal", Total);
                QueryBuilderObject.SetField("Posted", "0");

                QueryBuilderObject.InsertQueryString("[Transaction]", db_vms);

                err = InvoicesQuery.FindNext();
            }

            InvoicesQuery.Close();
            WriteMessage("\r\n");
            WriteMessage("<<< INVOICES >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        #endregion

        #region UpdateStock

        public override void UpdateStock()
        {
            if (!db_ERP.IsOpened())
            {
                WriteMessage("\r\n");
                WriteMessage("Cannot connect to GP database , please check the connection");
                return;
            }

            if (Filters.WarehouseID != -1)
                UpdateStockForWarehouse(Filters.WarehouseID.ToString(), Filters.StockDate);
            else
            {
                InCubeErrors err;
                object field = new object();
                InCubeQuery WarehouseQuery = new InCubeQuery(db_vms, "SELECT Vehicle.VehicleID FROM Warehouse INNER JOIN Vehicle ON Warehouse.WarehouseID = Vehicle.VehicleID");
                WarehouseQuery.Execute();

                err = WarehouseQuery.FindFirst();
                while (err == InCubeErrors.Success)
                {
                    WarehouseQuery.GetField(0, ref field);

                    int WarehouseID = Convert.ToInt16(field);

                    UpdateStockForWarehouse(WarehouseID.ToString(), Filters.StockDate);

                    err = WarehouseQuery.FindNext();
                }
            }
        }

        private void UpdateStockForWarehouse(string WarehouseID, DateTime StockDate)
        {
            int TOTALUPDATED = 0;

            InCubeErrors err;
            object field = new object();

            #region One Vehicle

            string DeleteStock = "delete from WarehouseStock where WarehouseID = " + WarehouseID;
            err = QueryBuilderObject.RunQuery(DeleteStock, db_vms);
            string WarehouseCode = GetFieldValue("Warehouse", "Barcode", "WarehouseID = " + WarehouseID, db_vms);
            string SelectStock = @"SELECT ITEMNMBR,TRXQTY AS QTY, DOCDATE As Expiry, TRNSTLOC FROM IV10001 
                                 INNER JOIN IV10000 ON IV10000.IVDOCNBR=IV10001.IVDOCNBR 
                                 WHERE DOCDATE= '" + StockDate.Date.ToString("yyyy/MM/dd") + @"'
                                     AND IV10001.IVDOCTYP=3
                                     AND TRNSTLOC = '" + WarehouseCode + "'";
            InCubeQuery StockQuery = new InCubeQuery(db_ERP, SelectStock);
            err = StockQuery.Execute();

            ClearProgress();
            SetProgressMax(StockQuery.GetDataTable().Rows.Count);

            err = StockQuery.FindFirst();

            while (err == InCubeErrors.Success)
            {
                ReportProgress("Updating Stock (" + WarehouseCode + ")");

                StockQuery.GetField("ITEMNMBR", ref field);
                string ItemCode = field.ToString();


                StockQuery.GetField("Expiry", ref field);
                string ExpiryDate = field.ToString();

                string LOT;

                try
                {
                    ExpiryDate = DateTime.Parse(ExpiryDate).ToString("yyyy/MM/dd");
                    LOT = (DateTime.Parse(ExpiryDate)).ToString("yyyy/MM/dd");
                }
                catch
                {
                    ExpiryDate = DateTime.Now.AddYears(10).ToString("yyyy/MM/dd");
                    LOT = (DateTime.Parse(ExpiryDate)).ToString("yyyy/MM/dd");
                }


                StockQuery.GetField("QTY", ref field); ///
                decimal PcsQty = decimal.Parse(field.ToString());

                decimal ConversionFactor = 1;

                string ConversionFactorString = GetFieldValue("Pack inner join item on pack.itemid = item.itemid", "pack.Quantity", " item.itemcode = '" + ItemCode.ToString().Trim() + "' and item.inactive=0", db_vms);

                if (ConversionFactorString == string.Empty)
                {
                    err = StockQuery.FindNext();
                    continue;
                }
                else
                {
                    ConversionFactor = decimal.Parse(ConversionFactorString);
                }

                int FQpsc = 0;
                bool CaseChecked = true;
                if (CaseChecked)
                    FQpsc = (int)(PcsQty % ConversionFactor);
                else
                {
                    PcsQty = Math.Round(PcsQty);
                    FQpsc = (int)(Math.Round(PcsQty % ConversionFactor));
                }

                FQpsc = (int)PcsQty;
                int FQcsc = FQpsc;

                InCubeQuery PackQuery = new InCubeQuery(db_vms, "Select pack.PackID,pack.Quantity From Pack inner join item on pack.itemid = item.itemid Where item.itemcode='" + ItemCode.Trim() + "'  and item.inactive=0");
                PackQuery.Execute();
                err = PackQuery.FindFirst();

                while (err == InCubeErrors.Success)
                {

                    TOTALUPDATED++;
                    PackQuery.GetField(0, ref field);
                    string PackID = field.ToString();

                    bool IsCase = false;
                    PackQuery.GetField(1, ref field);

                    if (field.ToString() != "1")
                    {
                        IsCase = true;
                    }

                    err = ExistObject("WarehouseStock", "PackID", "WarehouseID = " + WarehouseID + " AND ZoneID = 1 AND PackID = " + PackID + " AND ExpiryDate = '" + ExpiryDate + "' AND BatchNo = '" + LOT + "'", db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                        QueryBuilderObject.SetField("ZoneID", "1");
                        QueryBuilderObject.SetField("PackID", PackID);
                        QueryBuilderObject.SetField("ExpiryDate", "'" + ExpiryDate + "'");
                        QueryBuilderObject.SetField("BatchNo", "'" + LOT + "'");
                        QueryBuilderObject.SetField("SampleQuantity", "0");
                        QueryBuilderObject.SetField("StockStatusID", "-1");
                        if (IsCase)
                        {
                            QueryBuilderObject.SetField("Quantity", FQcsc.ToString());
                            QueryBuilderObject.SetField("BaseQuantity", FQcsc.ToString());
                        }
                        else
                        {
                            QueryBuilderObject.SetField("Quantity", FQpsc.ToString());
                            QueryBuilderObject.SetField("BaseQuantity", FQpsc.ToString());
                        }

                        QueryBuilderObject.InsertQueryString("WarehouseStock", db_vms);

                        //QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                        //QueryBuilderObject.SetField("ZoneID", "1");
                        //QueryBuilderObject.SetField("PackID", PackID);
                        //QueryBuilderObject.SetField("ExpiryDate", "'" + ExpiryDate + "'");
                        //QueryBuilderObject.SetField("StockDate", "'" + DateTime.Now.ToString("yyyy/MM/dd") + "'");
                        //QueryBuilderObject.SetField("BatchNo", "'" + LOT + "'");

                        //if (IsCase)
                        //{
                        //    QueryBuilderObject.SetField("Quantity", FQcsc.ToString());
                        //}
                        //else
                        //{
                        //    QueryBuilderObject.SetField("Quantity", FQpsc.ToString());
                        //}
                        //QueryBuilderObject.SetField("ProductionDate", "'" + DateTime.Now.ToString("yyyy/MM/dd") + "'");
                        //QueryBuilderObject.SetField("SampleQuantity", "0");

                        //QueryBuilderObject.InsertQueryString("DailyWarehouseStock", db_vms);
                    }
                    else
                    {
                        if (IsCase)
                        {
                            QueryBuilderObject.SetField("Quantity", FQcsc.ToString());
                            QueryBuilderObject.SetField("BaseQuantity", FQcsc.ToString());
                        }
                        else
                        {
                            QueryBuilderObject.SetField("Quantity", FQpsc.ToString());
                            QueryBuilderObject.SetField("BaseQuantity", FQpsc.ToString());
                        }

                        QueryBuilderObject.UpdateQueryString("WarehouseStock", "WarehouseID = " + WarehouseID + " AND ZoneID = 1 AND PackID = " + PackID + " AND ExpiryDate = '" + ExpiryDate + "' AND BatchNo = '" + LOT + "'", db_vms);
                    }

                    err = PackQuery.FindNext();
                }

                err = StockQuery.FindNext();

            }

            StockQuery.Close();
            WriteMessage("\r\n");
            WriteMessage("<<< STOCK " + WarehouseCode + "  Updated >>> Total Updated = " + TOTALUPDATED);

            //ClassStartOFDay.StartofDay(db_vms, WarehouseID);

            #endregion
        }

        #endregion

        #region Update Discount

        public override void UpdateDiscount()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            InCubeErrors err;
            object field = new object();

            if (!db_ERP.IsOpened())
            {
                WriteMessage("\r\n");
                WriteMessage("Cannot connect to GP database , please check the connection");
                return;
            }

            InCubeQuery DeleteDiscountQuery = new InCubeQuery(db_vms, "Delete From DiscountAssignment");
            err = DeleteDiscountQuery.ExecuteNonQuery();
            DeleteDiscountQuery = new InCubeQuery(db_vms, "Delete From Discount");
            err = DeleteDiscountQuery.ExecuteNonQuery();


            string SelectCustomer = @"SELECT  
                                              A.DEX_ROW_ID,
                                              ltrim(rtrim(cast(C.CUSTNMBR as nvarchar(50)))) as ADDRESSSCODE,

                                              A.CUSTNMBR,
                                              A.CUSTDISC

FROM RM00101 AS A 
INNER JOIN RM00303 AS B ON A.SALSTERR=B.SALSTERR    
Inner join RM00102 as C ON  A.CUSTNMBR = C.CUSTNMBR

WHERE B.COUNTRY in (SELECT IntLocation FROM ALNIntegration) 
AND A.INACTIVE=0 AND HOLD=0 AND A.CUSTDISC > 0";

            //AND (LEN(A.SALSTERR)=3 or LEN(A.SALSTERR)=4)
            //--+ ltrim(rtrim(cast(C.DEX_ROW_ID as nvarchar(50)))) as ADDRESSSCODE,
            InCubeQuery CustomerQuery = new InCubeQuery(db_ERP, SelectCustomer);
            CustomerQuery.Execute();

            ClearProgress();
            SetProgressMax(CustomerQuery.GetDataTable().Rows.Count);

            err = CustomerQuery.FindFirst();

            while (err == InCubeErrors.Success)
            {

                ReportProgress("Updating Discounts");
                CustomerQuery.GetField("ADDRESSSCODE", ref field);
                string CustomerCode = field.ToString();

                //  CustomerQuery.GetField("DEX_ROW_ID", ref field);
                string CustomerID = GetFieldValue("Customer ", "CustomerID", " CustomerCode = '" + CustomerCode.Trim() + "'", db_vms);

                // CustomerQuery.GetField("ADDRESSSCODE", ref field);
                string OutletID = GetFieldValue("CustomerOutlet", "OutletID", " CustomerCode = '" + CustomerCode.Trim() + "' AND CustomerID = " + CustomerID, db_vms);

                if (OutletID == string.Empty)
                {
                    err = CustomerQuery.FindNext();
                    continue;
                }

                CustomerQuery.GetField("CUSTDISC", ref field);
                decimal Discount = decimal.Parse(field.ToString());
                Discount /= 100;

                string MAXID = GetFieldValue("Discount", " IsNull(MAX(DiscountID),0) + 1 ", db_vms);

                err = ExistObject("DiscountAssignment DA inner join Discount D on DA.DiscountID=D.DiscountID", "D.Discount", " DA.CustomerID = " + CustomerID + " AND DA.OutletID = " + OutletID, db_vms);
                if (err == InCubeErrors.Success)
                {
                    TOTALUPDATED++;

                    string DiscountID = GetFieldValue("DiscountAssignment", "DiscountID", " CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                    QueryBuilderObject.SetField("Discount", Discount.ToString());
                    err = QueryBuilderObject.UpdateQueryString("Discount", "DiscountID = " + DiscountID, db_vms);
                }
                else
                {
                    TOTALINSERTED++;
                    QueryBuilderObject.SetField("DiscountID", MAXID);
                    QueryBuilderObject.SetField("AllItems", "1");
                    QueryBuilderObject.SetField("Discount", Discount.ToString());
                    QueryBuilderObject.SetField("StartDate", "'" + DateTime.Now.Date.ToString("dd/MMM/yyyy") + "'");
                    QueryBuilderObject.SetField("EndDate", "'" + DateTime.Now.Date.AddYears(10).ToString("dd/MMM/yyyy") + "'");
                    QueryBuilderObject.SetField("FOC", "0");
                    QueryBuilderObject.SetField("DiscountTypeID", "1");
                    QueryBuilderObject.SetField("TypeID", "1");
                    err = QueryBuilderObject.InsertQueryString("Discount", db_vms);

                    string MAX_ASS_ID = GetFieldValue("DiscountAssignment", " IsNull(MAX(DiscountAssignmentID),0) + 1 ", db_vms);

                    QueryBuilderObject.SetField("DiscountAssignmentID", MAX_ASS_ID);
                    QueryBuilderObject.SetField("DiscountID", MAXID);
                    QueryBuilderObject.SetField("CustomerID", CustomerID);
                    QueryBuilderObject.SetField("OutletID", OutletID);
                    err = QueryBuilderObject.InsertQueryString("DiscountAssignment", db_vms);

                    if (err != InCubeErrors.Success)
                    {

                    }

                }

                err = CustomerQuery.FindNext();
            }

            CustomerQuery.Close();

            WriteMessage("\r\n");
            WriteMessage("<<< CUSTOMERS DISCOUNT >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        #endregion

        #region Update Route
        // Each customer is assigned to a route code (RM00101.SALSTERR) and each route code is assigned to a depot code (RM00303.COUNTRY).  Please refer to the following field definitions.  
        public override void UpdateRoutes()
        {
            UpdateRouteInformation();
            UpdateRouteCustomer();
        }

        private void UpdateRouteInformation()
        {
            int TOTALINSERTED = 0;

            InCubeErrors err;
            object field = new object();

            if (!db_ERP.IsOpened())
            {
                WriteMessage("\r\n");
                WriteMessage("Cannot connect to GP database , please check the connection");
                return;
            }

            UpdateOrganization();

            string SelectRoute = @"SELECT distinct A.SALSTERR ,D.LOCNDSCR , D.STATE
FROM RM00101 AS A 
INNER JOIN RM00303 AS B ON A.SALSTERR=B.SALSTERR  
Inner join IV40700 as D ON A.SALSTERR = D.LOCNCODE
WHERE 
B.COUNTRY in (SELECT IntLocation FROM ALNIntegration) 
order by A.SALSTERR";

            //AND (LEN(A.SALSTERR)=3 or LEN(A.SALSTERR)=4)

            InCubeQuery RouteQuery = new InCubeQuery(db_ERP, SelectRoute);
            RouteQuery.Execute();

            ClearProgress();
            SetProgressMax(RouteQuery.GetDataTable().Rows.Count);

            err = RouteQuery.FindFirst();

            while (err == InCubeErrors.Success)
            {
                ReportProgress("Updating Routes");

                RouteQuery.GetField("SALSTERR", ref field);
                string RouteID = field.ToString().Trim();
                string EmployeeCode = field.ToString().Trim();

                if (RouteID.Equals(string.Empty))
                {
                    err = RouteQuery.FindNext();
                    continue;
                }

                RouteQuery.GetField("LOCNDSCR", ref field);
                string RouteName = field.ToString().Trim();

                RouteQuery.GetField("STATE", ref field);

                if (field.ToString().Trim().Equals(string.Empty))
                {
                    err = RouteQuery.FindNext();
                    continue;
                }

                string OrganizationID = GetFieldValue("OrganizationLanguage", "OrganizationID", "Description = '" + field.ToString().Trim() + "'", db_vms);

                if (OrganizationID.Equals(string.Empty))
                {
                    err = RouteQuery.FindNext();
                    continue;
                }

                string EmployeeID = GetFieldValue("Employee", "EmployeeID", " EmployeeCode = '" + EmployeeCode + "'", db_vms);

                //string DivisionID = GetFieldValue("EmployeeDivision", "DivisionID", "EmployeeID = " + EmployeeID, db_vms);

                //if (DivisionID == string.Empty)
                //{
                //    DivisionID = "1";
                //}

                err = ExistObject("Territory", "TerritoryID", "TerritoryID = " + RouteID, db_vms);
                if (err != InCubeErrors.Success)
                {
                    TOTALINSERTED++;
                    QueryBuilderObject.SetField("TerritoryID", RouteID);
                    QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                    QueryBuilderObject.SetField("TerritoryCode", "'" + RouteID + "'");

                    QueryBuilderObject.SetField("CreatedBy", "9999");
                    QueryBuilderObject.SetDateField("CreatedDate", DateTime.Now);

                    QueryBuilderObject.SetField("UpdatedBy", "9999");
                    QueryBuilderObject.SetDateField("UpdatedDate", DateTime.Now);

                    QueryBuilderObject.InsertQueryString("Territory", db_vms);
                }
                err = ExistObject("Route", "RouteID", "RouteID = " + RouteID, db_vms);
                if (err != InCubeErrors.Success)
                {
                    TOTALINSERTED++;

                    DateTime EstimatedStart = DateTime.Parse(DateTime.Now.Date.AddHours(7).ToString());
                    DateTime EstimatedEnd = DateTime.Parse(DateTime.Now.Date.AddHours(23).ToString());

                    QueryBuilderObject.SetField("RouteID", RouteID);
                    QueryBuilderObject.SetField("Inactive", "0");
                    QueryBuilderObject.SetField("TerritoryID", RouteID);
                    QueryBuilderObject.SetDateField("EstimatedStart", EstimatedStart);
                    QueryBuilderObject.SetDateField("EstimatedEnd", EstimatedEnd);
                    QueryBuilderObject.SetField("RouteCode", "'" + RouteID + "'");
                    QueryBuilderObject.SetField("CreatedBy", "9999");
                    QueryBuilderObject.SetDateField("CreatedDate", DateTime.Now);

                    QueryBuilderObject.SetField("UpdatedBy", "9999");
                    QueryBuilderObject.SetDateField("UpdatedDate", DateTime.Now);

                    QueryBuilderObject.InsertQueryString("Route", db_vms);
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

                err = ExistObject("TerritoryLanguage", "TerritoryID", "TerritoryID = " + RouteID + " AND LanguageID = 1", db_vms);
                if (err != InCubeErrors.Success)
                {
                    QueryBuilderObject.SetField("TerritoryID", RouteID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + RouteName + "'");
                    QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);
                }

                err = ExistObject("RouteLanguage", "RouteID", "RouteID = " + RouteID + " AND LanguageID = 1", db_vms);
                if (err != InCubeErrors.Success)
                {
                    QueryBuilderObject.SetField("RouteID", RouteID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + RouteName + "'");
                    QueryBuilderObject.InsertQueryString("RouteLanguage", db_vms);
                }

                err = RouteQuery.FindNext();
            }

            RouteQuery.Close();
            WriteMessage("\r\n");
            WriteMessage("<<< ROUTE >>> Total Inserted = " + TOTALINSERTED);
        }

        private void UpdateRouteCustomer()
        {
            int TOTALINSERTED = 0;

            InCubeErrors err;
            object field = new object();

            DateTime _modificationDate = GetIntegrationModificationDateNew(db_vms);

            if (!db_ERP.IsOpened())
            {
                WriteMessage("\r\n");
                WriteMessage("Cannot connect to GP database , please check the connection");
                return;
            }

            UpdateOrganization();
            //ltrim(rtrim(cast(C.CUSTNMBR as nvarchar(50)))) + ltrim(rtrim(cast(C.DEX_ROW_ID as nvarchar(50)))) as ADDRESSSCODE
            string SelectRoute = @"SELECT A.SALSTERR ,A.DEX_ROW_ID, 
ltrim(rtrim(cast(C.CUSTNMBR as nvarchar(50)))) as ADDRESSSCODE ,A.CUSTNMBR 
FROM
RM00101 AS A 
INNER JOIN RM00303 AS B ON A.SALSTERR = B.SALSTERR 
Inner join RM00102 as C ON A.CUSTNMBR = C. CUSTNMBR 
WHERE
B.COUNTRY in (SELECT IntLocation FROM ALNIntegration) 
AND A.INACTIVE=0 AND HOLD= 0
order by A.CUSTNMBR";

            //and C.MODIFDT >= '2011-03-09' and C.MODIFDT < '2011-03-10' 

            InCubeQuery RouteQuery = new InCubeQuery(db_ERP, SelectRoute);
            RouteQuery.Execute();

            ClearProgress();
            SetProgressMax(RouteQuery.GetDataTable().Rows.Count);

            err = RouteQuery.FindFirst();

            InCubeQuery DeleteRouteCustomerQuery = new InCubeQuery(db_vms, "Delete From RouteCustomer");
            DeleteRouteCustomerQuery.ExecuteNonQuery();

            InCubeQuery DeleteCustomerOutletTerritoryQuery = new InCubeQuery(db_vms, "Delete From CustOutTerritory");
            DeleteCustomerOutletTerritoryQuery.ExecuteNonQuery();

            while (err == InCubeErrors.Success)
            {
                ReportProgress("Updating Route Customers");

                RouteQuery.GetField("SALSTERR", ref field);
                string[] Routes = field.ToString().Trim().Split('-');

                //RouteQuery.GetField("DEX_ROW_ID", ref field);
                //string CustomerID = field.ToString().Trim();

                RouteQuery.GetField("ADDRESSSCODE", ref field);
                string CustomerID = GetFieldValue("Customer", "CustomerID", " CustomerCode = '" + field.ToString().Trim() + "'", db_vms);
                string OutletID = GetFieldValue("CustomerOutlet", "OutletID", " CustomerCode = '" + field.ToString().Trim() + "' AND CustomerID = " + CustomerID, db_vms);

                if (OutletID == string.Empty)
                {
                    err = RouteQuery.FindNext();
                    continue;
                }

                for (int i = 0; i < Routes.Length; i++)
                {
                    string RouteID = Routes[i].Trim();

                    err = ExistObject("Route", "RouteID", "RouteID = " + RouteID, db_vms);
                    if (err == InCubeErrors.Success)
                    {
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
                            QueryBuilderObject.SetField("CustomerID", CustomerID);
                            QueryBuilderObject.SetField("OutletID", OutletID);
                            QueryBuilderObject.SetField("TerritoryID", RouteID);
                            err = QueryBuilderObject.InsertQueryString("CustOutTerritory", db_vms);
                        }

                        string OrganizationID = GetFieldValue("Territory", "OrganizationID", " TerritoryID = " + RouteID, db_vms);
                        if (OrganizationID != string.Empty)
                        {
                            string AccountID = GetFieldValue("AccountCustOut", "AccountID", " CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                            if (!AccountID.Equals(string.Empty))
                            {
                                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                                err = QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + AccountID, db_vms);
                            }
                        }
                    }
                }
                err = RouteQuery.FindNext();
            }

            RouteQuery.Close();
            WriteMessage("\r\n");
            WriteMessage("<<< ROUTE CUSTOMER >>> Total Inserted = " + TOTALINSERTED);
        }

        #endregion

        #region SendInvoices

        private decimal TrimDecimal(decimal decNumber, int NoOfDigits)
        {
            try
            {
                string decStr = decNumber.ToString();
                string[] decParts = decStr.Split(new char[] { '.' });
                if (decParts.Length == 2)
                {
                    if (decParts[1].Length > NoOfDigits)
                    {
                        decParts[1] = decParts[1].Substring(0, NoOfDigits);
                    }
                    decStr = decParts[0] + "." + decParts[1];
                }
                decNumber = decimal.Parse(decStr);
            }
            catch
            {

            }
            return decNumber;
        }
        public override void SendInvoices()
        {
            SendInvoiceWithVAT(CoreGeneral.Common.StartupPath + "\\E-Connect\\", Filters.EmployeeID == -1, Filters.EmployeeID.ToString(), Filters.FromDate, Filters.ToDate, IntegrationField.Sales_S);
        }

        private void SetSynchronizedFlag(string TransactionID, string SALESMODE, string Notes, string TransChar)
        {
            try
            {
                string OtherChar = TransChar == "I" ? "R" : "I";
                InCubeQuery UpdateQuery;
                if (!SALESMODE.ToString().ToLower().Equals("var"))
                {
                    UpdateQuery = new InCubeQuery(db_vms, "Update [Transaction] SET Synchronized = 1 where TransactionID = '" + TransactionID.ToString() + "'");
                    err = UpdateQuery.Execute();
                }
                else
                {
                    if (Notes.ToString().Contains(OtherChar))
                    {
                        UpdateQuery = new InCubeQuery(db_vms, string.Format("Update [Transaction] SET Synchronized = 1, Notes = ISNULL(Notes,'') + '{0}' where TransactionID = '{1}'", TransChar, TransactionID));
                        err = UpdateQuery.Execute();
                    }
                    else if (!Notes.ToString().Contains(TransChar))
                    {
                        UpdateQuery = new InCubeQuery(db_vms, string.Format("Update [Transaction] SET Notes = ISNULL(Notes,'') + '{0}' where TransactionID = '{1}'", TransChar, TransactionID));
                        err = UpdateQuery.Execute();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void FillCustomerInfoFromGP(string CustomerCode, taSopHdrIvcInsert salesHdr)
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
LTRIM(RTRIM(ISNULL(PYMTRMID,''))) PYMTRMID,
LTRIM(RTRIM(ISNULL(PRCLEVEL,''))) PRCLEVEL
FROM RM00101 WHERE LTRIM(RTRIM(CUSTNMBR)) = '{0}'", CustomerCode.Trim().Replace("'", "''"));

                InCubeQuery inCubeQuery = new InCubeQuery(db_ERP, QueryString);
                inCubeQuery.Execute();

                DataTable dtGPCustInfo = inCubeQuery.GetDataTable();

                if (dtGPCustInfo.Rows[0]["SHIPMTHD"] != DBNull.Value)
                    salesHdr.SHIPMTHD = dtGPCustInfo.Rows[0]["SHIPMTHD"].ToString();
                if (dtGPCustInfo.Rows[0]["CITY"] != DBNull.Value)
                    salesHdr.CITY = dtGPCustInfo.Rows[0]["CITY"].ToString();
                if (dtGPCustInfo.Rows[0]["STATE"] != DBNull.Value)
                    salesHdr.STATE = dtGPCustInfo.Rows[0]["STATE"].ToString();
                if (dtGPCustInfo.Rows[0]["ZIP"] != DBNull.Value)
                    salesHdr.ZIPCODE = dtGPCustInfo.Rows[0]["ZIP"].ToString();
                if (dtGPCustInfo.Rows[0]["COUNTRY"] != DBNull.Value)
                    salesHdr.COUNTRY = dtGPCustInfo.Rows[0]["COUNTRY"].ToString();
                if (dtGPCustInfo.Rows[0]["CNTCPRSN"] != DBNull.Value)
                    salesHdr.CNTCPRSN = dtGPCustInfo.Rows[0]["CNTCPRSN"].ToString();
                if (dtGPCustInfo.Rows[0]["PHONE1"] != DBNull.Value)
                    salesHdr.PHNUMBR1 = dtGPCustInfo.Rows[0]["PHONE1"].ToString();
                if (dtGPCustInfo.Rows[0]["PHONE2"] != DBNull.Value)
                    salesHdr.PHNUMBR2 = dtGPCustInfo.Rows[0]["PHONE2"].ToString();
                if (dtGPCustInfo.Rows[0]["PHONE3"] != DBNull.Value)
                    salesHdr.PHNUMBR3 = dtGPCustInfo.Rows[0]["PHONE3"].ToString();
                if (dtGPCustInfo.Rows[0]["FAX"] != DBNull.Value)
                    salesHdr.FAXNUMBR = dtGPCustInfo.Rows[0]["FAX"].ToString();
                if (dtGPCustInfo.Rows[0]["TAXSCHID"] != DBNull.Value)
                    salesHdr.TAXSCHID = dtGPCustInfo.Rows[0]["TAXSCHID"].ToString();
                if (dtGPCustInfo.Rows[0]["ADDRESS1"] != DBNull.Value)
                    salesHdr.ADDRESS1 = dtGPCustInfo.Rows[0]["ADDRESS1"].ToString();
                if (dtGPCustInfo.Rows[0]["ADDRESS2"] != DBNull.Value)
                    salesHdr.ADDRESS2 = dtGPCustInfo.Rows[0]["ADDRESS2"].ToString();
                if (dtGPCustInfo.Rows[0]["ADDRESS3"] != DBNull.Value)
                    salesHdr.ADDRESS3 = dtGPCustInfo.Rows[0]["ADDRESS3"].ToString();
                if (dtGPCustInfo.Rows[0]["PYMTRMID"] != DBNull.Value)
                    salesHdr.ADDRESS3 = dtGPCustInfo.Rows[0]["PYMTRMID"].ToString();
                //if (dtGPCustInfo.Rows[0]["PRCLEVEL"] != DBNull.Value)
                //    salesHdr.PRCLEVEL = dtGPCustInfo.Rows[0]["PRCLEVEL"].ToString();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public Result SendInvoiceWithVAT(string filename, bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate, IntegrationField Field)
        {
            Result res = Result.UnKnown;
            try
            {
                //Declarations
                InCubeQuery inCubeQuery;
                int processID = 0, totalSuccess = 0, totalFailure = 0, CustomerID = 0, OutletID = 0, CustomerTypeID = 0, FOCTypeID = 0, PackStatusID = 0, CreationReason = 0;
                short SOPTYPE = 0;
                string TransactionID = "", CustomerName = "", CustomerCode = "", WarehouseCode = "", EmployeeCode = "", QryStr = "", result = "";
                string OutletCode = "", CustomerRefNo = "", SALESMODE = "", Notes = "", Customeraddress1 = "", SLPRSNID = "", _DOCID = "";
                DateTime TransactionDate;
                SOPTransactionType salesOrder = new SOPTransactionType();
                taSopHdrIvcInsert salesHdr = new taSopHdrIvcInsert();
                DataTable dtDetails = new DataTable();
                List<taSopLineIvcInsert_ItemsTaSopLineIvcInsert> LineItems = new List<taSopLineIvcInsert_ItemsTaSopLineIvcInsert>();
                List<taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert> TaxLines = new List<taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert>();

                decimal Quantity = 0, LineDiscount = 0, unitPrice = 0, OriginalTax = 0, BasePrice = 0, LineTax = 0;
                string ItemCode = "", packCode = "", STRPack = "";
                taSopLineIvcInsert_ItemsTaSopLineIvcInsert salesLine = new taSopLineIvcInsert_ItemsTaSopLineIvcInsert();
                taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert taxLine = new taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert();
                eConnectType eConnect = new eConnectType();

                string strDOC = "";
                string QueryString = "";

                switch (Field)
                {
                    case IntegrationField.Sales_S:
                        WriteMessage("\r\n" + "Sending Invoices");
                        SOPTYPE = 3;
                        QueryString = @"SELECT     

            [Transaction].TransactionID, 
            [Transaction].TransactionDate, 
            CustomerOutlet.Barcode AS CustomerCode, 
            Warehouse.Barcode AS WarehouseCode, 
            Employee.EmployeeCode, 
            CustomerOutletLanguage.Description,
            CustomerOutlet.CustomerID,
            CustomerOutlet.OutletID,
            CustomerOutletLanguage.Address,
            CustomerOutlet.CustomerCode as OutletCode,[Transaction].CustomerRefNo,
            CASE WHEN [Transaction].CUSTOMERID IN (SELECT CUSTOMERID FROM ROUTE WHERE ROUTEID=[Transaction].ROUTEID) THEN 'VAR' ELSE 'SAL' END SALESMODE,
            ISNULL([Transaction].Notes,'') Notes,
            CustomerOutlet.CustomerTypeID, ISNULL([Transaction].CreationReason,0) CreationReason
            FROM         [Transaction] INNER JOIN
            CustomerOutletLanguage ON [Transaction].CustomerID = CustomerOutletLanguage.CustomerID INNER JOIN
            CustomerOutlet ON [Transaction].CustomerID = CustomerOutlet.CustomerID AND [Transaction].OutletID = CustomerOutlet.OutletID AND 
            CustomerOutletLanguage.CustomerID = CustomerOutlet.CustomerID AND CustomerOutletLanguage.OutletID = CustomerOutlet.OutletID INNER JOIN
            EmployeeVehicle ON [Transaction].EmployeeID = EmployeeVehicle.EmployeeID INNER JOIN
            Warehouse ON EmployeeVehicle.VehicleID = Warehouse.WarehouseID INNER JOIN
            Employee ON [Transaction].EmployeeID = Employee.EmployeeID
            WHERE [Transaction].Synchronized = 0 AND [Transaction].Voided = 0 
            AND [Transaction].TransactionTypeID IN (1,3)
            AND [Transaction].TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
            AND [Transaction].TransactionDate < '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + "'";
                        break;

                    case IntegrationField.Returns_S:
                        WriteMessage("\r\n" + "Sending Returns");
                        SOPTYPE = 4;
                        QueryString = @"SELECT     
            [Transaction].TransactionID, 
            [Transaction].TransactionDate, 
            CustomerOutlet.Barcode AS CustomerCode, 
            Warehouse.Barcode AS WarehouseCode, 
            Employee.EmployeeCode, 
            CustomerOutletLanguage.Description,
            CustomerOutlet.CustomerID,CustomerOutlet.OutletID,
            CustomerOutletLanguage.Address,
            CustomerOutlet.CustomerCode as OutletCode,[Transaction].CustomerRefNo,
            CASE WHEN [Transaction].CUSTOMERID IN (SELECT CUSTOMERID FROM ROUTE WHERE ROUTEID=[Transaction].ROUTEID) THEN 'VAR' ELSE 'RTN' END SALESMODE,
            ISNULL([Transaction].Notes,'') Notes, CustomerOutlet.CustomerTypeID, ISNULL([Transaction].CreationReason,0) CreationReason
            FROM [Transaction] INNER JOIN
            CustomerOutletLanguage ON [Transaction].CustomerID = CustomerOutletLanguage.CustomerID INNER JOIN
            CustomerOutlet ON [Transaction].CustomerID = CustomerOutlet.CustomerID AND [Transaction].OutletID = CustomerOutlet.OutletID AND 
            CustomerOutletLanguage.CustomerID = CustomerOutlet.CustomerID AND CustomerOutletLanguage.OutletID = CustomerOutlet.OutletID INNER JOIN
            EmployeeVehicle ON [Transaction].EmployeeID = EmployeeVehicle.EmployeeID INNER JOIN
            Warehouse ON EmployeeVehicle.VehicleID = Warehouse.WarehouseID INNER JOIN
            Employee ON [Transaction].EmployeeID = Employee.EmployeeID
            WHERE ([Transaction].Synchronized = 0) AND ([Transaction].Voided = 0) AND ([Transaction].TransactionTypeID = 2 or [Transaction].TransactionTypeID = 4 or ([Transaction].TransactionTypeID = 1 and [Transaction].CreationReason=2 
and isnull((select count(*) from transactiondetail td where [Transaction].transactionid=td.transactionid and 
 [Transaction].customerid=td.customerid and  [Transaction].outletid=td.outletid and td.PackStatusID<>0 ),0)>0)) AND 
            ([Transaction].TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
            AND [Transaction].TransactionDate < '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + "')";
                        break;
                }

                if (!AllSalespersons)
                {
                    QueryString += " AND [Transaction].EmployeeID = " + Salesperson;
                }

                inCubeQuery = new InCubeQuery(db_vms, QueryString);
                if (inCubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Header query failed !!"));
                }

                DataTable dtHeader = inCubeQuery.GetDataTable();
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

                        TransactionID = dtHeader.Rows[m]["TransactionID"].ToString();
                        CustomerID = Convert.ToInt32(dtHeader.Rows[m]["CustomerID"]);
                        OutletID = Convert.ToInt32(dtHeader.Rows[m]["OutletID"]);

                        ReportProgress("Sending invoice: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, TransactionID);
                        filters.Add(9, CustomerID.ToString());
                        filters.Add(10, OutletID.ToString());
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        decimal TOTAL = 0;
                        int LINSEQ = 0;

                        TransactionDate = Convert.ToDateTime(dtHeader.Rows[m]["TransactionDate"]);
                        CustomerCode = dtHeader.Rows[m]["CustomerCode"].ToString();
                        EmployeeCode = dtHeader.Rows[m]["EmployeeCode"].ToString();
                        CustomerName = dtHeader.Rows[m]["Description"].ToString();
                        OutletCode = dtHeader.Rows[m]["OutletCode"].ToString();
                        WarehouseCode = dtHeader.Rows[m]["WarehouseCode"].ToString();
                        Customeraddress1 = dtHeader.Rows[m]["Address"].ToString();
                        CustomerRefNo = dtHeader.Rows[m]["CustomerRefNo"].ToString();
                        SALESMODE = dtHeader.Rows[m]["SALESMODE"].ToString();
                        Notes = dtHeader.Rows[m]["Notes"].ToString();
                        CustomerTypeID = Convert.ToInt16(dtHeader.Rows[m]["CustomerTypeID"]);
                        CreationReason = Convert.ToInt16(dtHeader.Rows[m]["CreationReason"]);

                        string COUNTRY = GetFieldValue("IV40700", "STATE", " rtrim(ltrim(LOCNCODE)) = '" + EmployeeCode + "'", db_ERP);
                        strDOC = COUNTRY.Trim();
                        if (strDOC.Trim() == string.Empty)
                        {
                            throw new Exception("State is not found in table IV40700 form employee ('" + EmployeeCode + "')");
                        }

                        switch (Field)
                        {
                            case IntegrationField.Sales_S:
                                if (CustomerTypeID == 1)
                                {
                                    SLPRSNID = "CASH";
                                    _DOCID = "CSP";
                                }
                                else
                                {
                                    SLPRSNID = "CREDIT";
                                    _DOCID = "CRP";
                                }
                                break;
                            case IntegrationField.Returns_S:
                                if (CustomerTypeID == 1)
                                {
                                    SLPRSNID = "CASH";
                                    _DOCID = "RCS";
                                }
                                if (CustomerTypeID == 2)
                                {
                                    SLPRSNID = "CREDIT";
                                    _DOCID = "RCR";
                                }
                                break;
                        }

                        salesHdr = new taSopHdrIvcInsert();
                        salesHdr.SOPTYPE = SOPTYPE;
                        salesHdr.SOPNUMBE = TransactionID.ToString().Trim();
                        salesHdr.CSTPONBR = CustomerRefNo.ToString();
                        salesHdr.SLPRSNID = SLPRSNID;
                        salesHdr.DOCDATE = TransactionDate.ToString("yyyy-MM-dd");
                        salesHdr.CUSTNMBR = CustomerCode.ToString().Trim();
                        salesHdr.CUSTNAME = CustomerName.ToString().Trim();
                        salesHdr.SALSTERR = EmployeeCode.ToString().Trim();
                        salesHdr.TRDISAMTSpecified = true;
                        salesHdr.BACHNUMB = strDOC + "-" + TransactionDate.ToString("ddMMyy");
                        salesHdr.USER2ENT = "InCube";
                        salesHdr.ShipToName = CustomerName.ToString().Trim();
                        salesHdr.LOCNCODE = WarehouseCode.ToString().Trim();
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
                        salesHdr.CREATETAXES = 0;
                        salesHdr.USINGHEADERLEVELTAXES = 0;
                        FillCustomerInfoFromGP(CustomerCode, salesHdr);

                        switch (Field)
                        {
                            case IntegrationField.Sales_S:
                                QryStr = @"SELECT     
TransactionDetail.TransactionID,
TransactionDetail.BatchNo,
TransactionDetail.Quantity,
TransactionDetail.Price, 
TransactionDetail.ExpiryDate, 
TransactionDetail.Discount, 
ItemLanguage.Description AS ItemName, 
Item.ItemCode as Barcode, 
PackTypeLanguage.Description AS PackName, 
Pack.Quantity AS PcsInCse, 
TransactionDetail.PackID,
ISNULL(TransactionDetail.FOCTypeID,1) FOCTypeID,
-1 PackStatusID,
TransactionDetail.Tax,
TransactionDetail.BasePrice

FROM TransactionDetail INNER JOIN
Pack ON TransactionDetail.PackID = Pack.PackID INNER JOIN
ItemLanguage ON Pack.ItemID = ItemLanguage.ItemID INNER JOIN
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID INNER JOIN
Item ON ItemLanguage.ItemID = Item.ItemID
WHERE (PackTypeLanguage.LanguageID = 1) AND (ItemLanguage.LanguageID = 1) AND (TransactionDetail.TransactionID = '" + TransactionID.ToString() + "') AND (TransactionDetail.CustomerID = " + CustomerID.ToString() + ")  AND (TransactionDetail.OutletID = " + OutletID.ToString() + ")";
                                break;
                            case IntegrationField.Returns_S:
                                QryStr = @"SELECT     
TransactionDetail.TransactionID,
TransactionDetail.BatchNo,
TransactionDetail.Quantity,
(case(TransactionDetail.PackStatusID) when 0 then 0 else TransactionDetail.Price end ) Price,
TransactionDetail.ExpiryDate, 
TransactionDetail.Discount, 
ItemLanguage.Description AS ItemName, 
Item.ItemCode AS Barcode, 
PackTypeLanguage.Description AS PackName, 
Pack.Quantity AS PcsInCse, 
TransactionDetail.PackID,
0 FOCTypeID,
ISNULL(TransactionDetail.PackStatusID,0) PackStatusID,
TransactionDetail.Tax,
TransactionDetail.BasePrice
FROM TransactionDetail INNER JOIN  
Pack ON TransactionDetail.PackID = Pack.PackID INNER JOIN
ItemLanguage ON Pack.ItemID = ItemLanguage.ItemID INNER JOIN
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID INNER JOIN
Item ON ItemLanguage.ItemID = Item.ItemID
WHERE (PackTypeLanguage.LanguageID = 1) AND (TransactionDetail.PackStatusID<>0)  AND (ItemLanguage.LanguageID = 1) AND (TransactionDetail.TransactionID = '" + TransactionID.ToString() + "') AND TransactionDetail.CustomerID = " + CustomerID.ToString() + "  AND TransactionDetail.OutletID = " + OutletID.ToString();

                                break;
                        }

                        inCubeQuery = new InCubeQuery(db_vms, QryStr);
                        err = inCubeQuery.Execute();
                        dtDetails = inCubeQuery.GetDataTable();

                        LineItems = new List<taSopLineIvcInsert_ItemsTaSopLineIvcInsert>();
                        TaxLines = new List<taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert>();

                        if (dtDetails.Rows.Count == 0)
                        {
                            throw new Exception("No details found , Invoice Number = " + TransactionID.ToString());
                        }

                        decimal DiscountTotal = 0, TaxTotal = 0;

                        for (int i = 0; i < dtDetails.Rows.Count; i++)
                        {
                            Quantity = Convert.ToDecimal(dtDetails.Rows[i]["Quantity"]);
                            FOCTypeID = Convert.ToInt16(dtDetails.Rows[i]["FOCTypeID"]);
                            PackStatusID = Convert.ToInt16(dtDetails.Rows[i]["PackStatusID"]);
                            ItemCode = dtDetails.Rows[i]["Barcode"].ToString().Trim();
                            packCode = dtDetails.Rows[i]["PackName"].ToString().Trim();
                            STRPack = dtDetails.Rows[i]["ItemName"].ToString().Trim().Split('_')[0];
                            LineDiscount = Convert.ToDecimal(dtDetails.Rows[i]["Discount"]);
                            unitPrice = Convert.ToDecimal(dtDetails.Rows[i]["Price"]);
                            OriginalTax = Convert.ToDecimal(dtDetails.Rows[i]["Tax"]);
                            LineTax = 0;
                            BasePrice = Convert.ToDecimal(dtDetails.Rows[i]["BasePrice"]);
                            LINSEQ = LINSEQ + 16384;

                            if (Field == IntegrationField.Returns_S && SALESMODE == "VAR")
                            {
                                unitPrice = 0;
                                OriginalTax = 0;
                                PackStatusID = 3;
                            }
                            else if (Field == IntegrationField.Sales_S && unitPrice == 0 && FOCTypeID != 2)
                            {
                                OriginalTax = 0;
                            }
                            taxLine = new taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert();
                            taxLine.LNITMSEQ = LINSEQ;
                            taxLine.CUSTNMBR = CustomerCode.ToString().Trim();
                            taxLine.TAXDTLID = "VATSLS+5";
                            taxLine.SOPTYPE = SOPTYPE;

                            salesLine = new taSopLineIvcInsert_ItemsTaSopLineIvcInsert();
                            salesLine.CUSTNMBR = CustomerCode.ToString().Trim();
                            salesLine.AUTOALLOCATELOT = 0;
                            salesLine.ALLOCATE = 1;
                            salesLine.SALSTERR = EmployeeCode.ToString().Trim();
                            salesLine.SLPRSNID = SLPRSNID;
                            salesLine.SOPNUMBE = TransactionID.ToString().Trim();
                            salesLine.LOCNCODE = WarehouseCode.ToString().Trim();
                            salesLine.DOCID = _DOCID;
                            salesLine.DOCDATE = TransactionDate.ToString("yyyy-MM-dd");
                            salesLine.SOPTYPE = SOPTYPE;
                            salesLine.ITEMNMBR = ItemCode;
                            salesLine.UOFM = packCode;
                            salesLine.ITEMDESC = STRPack;
                            salesLine.QUANTITY = Quantity;
                            salesLine.LNITMSEQ = LINSEQ;
                            salesLine.UNITCOST = 0;
                            salesLine.UNITCOSTSpecified = false;
                            salesLine.NONINVEN = 0;
                            salesLine.ADDRESS1 = salesHdr.ADDRESS1;
                            salesLine.ADDRESS2 = salesHdr.ADDRESS2;
                            salesLine.ADDRESS3 = salesHdr.ADDRESS3;

                            if (Field == IntegrationField.Returns_S)
                            {
                                salesLine.COMMNTID = SALESMODE.ToString();
                                salesLine.QTYRTRND = 0;
                                salesLine.QTYINUSE = 0;
                                salesLine.DROPSHIP = 0;
                                salesLine.QTYTBAOR = 0;

                                switch (PackStatusID)
                                {
                                    case 1: // Damaged
                                        salesLine.QTYDMGED = Quantity;
                                        salesLine.QTYINSVC = 0;
                                        salesLine.QTYONHND = 0;
                                        break;
                                    case 2://Expired
                                        salesLine.QTYINSVC = Quantity;
                                        salesLine.QTYDMGED = 0;
                                        salesLine.QTYONHND = 0;
                                        break;
                                    case 3://In Good Condition
                                        salesLine.QTYONHND = Quantity;
                                        salesLine.QTYDMGED = 0;
                                        salesLine.QTYINSVC = 0;
                                        break;
                                }
                            }
                            else if (Field == IntegrationField.Sales_S)
                            {
                                salesLine.QTYFULFISpecified = true;
                                salesLine.QTYFULFI = Quantity;
                            }

                            LineDiscount = Math.Round(LineDiscount, 2, MidpointRounding.AwayFromZero);
                            DiscountTotal += LineDiscount;
                            decimal XTNDPRCE = Math.Round(Quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
                            if (OriginalTax > 0)
                            {
                                decimal taxPerc = 0;
                                if (XTNDPRCE > 0)
                                {
                                    taxPerc = OriginalTax / (XTNDPRCE - LineDiscount) * 100;
                                    taxPerc = Math.Round(taxPerc, 2, MidpointRounding.AwayFromZero);
                                    LineTax = Math.Round((XTNDPRCE - LineDiscount) * taxPerc / 100, 2, MidpointRounding.AwayFromZero);
                                }
                                else
                                {
                                    taxPerc = OriginalTax / (BasePrice * Quantity) * 100;
                                    taxPerc = Math.Round(taxPerc, 2, MidpointRounding.AwayFromZero);
                                    LineTax = Math.Round(BasePrice * Quantity * taxPerc / 100, 2, MidpointRounding.AwayFromZero);
                                }
                            }
                            TOTAL += XTNDPRCE;
                            TaxTotal += LineTax;

                            salesLine.UNITPRCE = unitPrice;
                            salesLine.TAXAMNT = LineTax;
                            salesLine.XTNDPRCE = XTNDPRCE;
                            salesLine.MRKDNAMTSpecified = true;

                            taxLine.SALESAMT = XTNDPRCE;
                            taxLine.STAXAMNT = LineTax;

                            LineItems.Add(salesLine);
                            if (LineTax > 0)
                                TaxLines.Add(taxLine);
                        }

                        salesHdr.DOCAMNT = TOTAL - DiscountTotal + TaxTotal;
                        salesHdr.TRDISAMT = DiscountTotal;
                        salesHdr.TAXAMNT = TaxTotal;
                        salesHdr.SUBTOTAL = TOTAL;

                        if (Field == IntegrationField.Sales_S && TOTAL == 0)
                        {
                            switch (FOCTypeID)
                            {
                                case 1:
                                    _DOCID = "FOCG";
                                    break;
                                case 2:
                                    _DOCID = "FOCP";
                                    break;
                                case 3:
                                    _DOCID = "FOCS";
                                    break;
                            }
                        }

                        salesHdr.DOCID = _DOCID;
                        salesOrder.taSopLineIvcInsert_Items = LineItems.ToArray();
                        salesOrder.taSopLineIvcTaxInsert_Items = TaxLines.ToArray();
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


                        SetSynchronizedFlag(TransactionID, SALESMODE, Notes, Field == IntegrationField.Sales_S ? "I" : "R");
                        //This query to set SALSTERR and SLPRSNID since sending to GP doesnt affect those fields
                        InCubeQuery UpdateQuery = new InCubeQuery(db_ERP, "Update SOP10200 SET SALSTERR = '" + EmployeeCode.ToString() + "', SLPRSNID = '" + SLPRSNID + "' where  SOPNUMBE = '" + TransactionID.ToString() + "' AND SOPTYPE = 3");
                        err = UpdateQuery.Execute();

                        res = Result.Success;
                        result = "Success";
                        WriteMessage("Success ..");
                        totalSuccess++;
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        result = ex.Message;
                        if (ex.ToString().Contains("Error Description = Duplicate document number"))
                        {
                            WriteMessage("Already avaialble in GP, flag will be set to 1");
                            SetSynchronizedFlag(TransactionID, SALESMODE, Notes, Field == IntegrationField.Sales_S ? "I" : "R");
                        }
                        else
                        {
                            WriteMessage("FAILED!");
                        }
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
        public int SerializeSalesOrderObjectSendInvoice(string filename, bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate)
        {
            int ret = 0;
            try
            {
                string strDOC = "";
                string _DOCID = "";
                WriteMessage("\r\n" + "Sending Invoices");

                SOPTransactionType salesOrder = new SOPTransactionType();
                taSopHdrIvcInsert salesHdr = new taSopHdrIvcInsert();
                object TransactionID = "";
                object TransactionDate = "";
                object CustomerName = "";
                object CustomerCode = "";
                object WarehouseCode = "";
                object EmployeeCode = "";
                string STRPack = "";
                object CustomerID = "";
                object OutletID = "";
                object Customeraddress = "";
                object Customeraddress1 = "";
                object Customeraddress2 = "";
                object OutletCode = "";
                object CustomerRefNo = "";
                object SALESMODE = "";
                object Notes = "";
                int FOCTypeID = 1;
                DateTime date;


                if (UpdateCustomerInfoFromGP(FromDate, ToDate) != InCubeErrors.Success)
                {
                    WriteMessage("\r\n Falied to update customer info fom GP");
                    return 0;
                }

                string QueryString = @"SELECT     

            [Transaction].TransactionID, 
            [Transaction].TransactionDate, 
            CustomerOutlet.Barcode AS CustomerCode, 
            Warehouse.Barcode AS WarehouseCode, 
            Employee.EmployeeCode, 
            CustomerOutletLanguage.Description,
            CustomerOutlet.CustomerID,
            CustomerOutlet.OutletID,
            CustomerOutletLanguage.Address,
            CustomerOutlet.CustomerCode as OutletCode,[Transaction].CustomerRefNo,
            CASE WHEN [Transaction].CUSTOMERID IN (SELECT CUSTOMERID FROM ROUTE WHERE ROUTEID=[Transaction].ROUTEID) THEN 'VAR' ELSE 'SAL' END SALESMODE,
            ISNULL([Transaction].Notes,'') Notes
            FROM         [Transaction] INNER JOIN
            CustomerOutletLanguage ON [Transaction].CustomerID = CustomerOutletLanguage.CustomerID INNER JOIN
            CustomerOutlet ON [Transaction].CustomerID = CustomerOutlet.CustomerID AND [Transaction].OutletID = CustomerOutlet.OutletID AND 
            CustomerOutletLanguage.CustomerID = CustomerOutlet.CustomerID AND CustomerOutletLanguage.OutletID = CustomerOutlet.OutletID INNER JOIN
            EmployeeVehicle ON [Transaction].EmployeeID = EmployeeVehicle.EmployeeID INNER JOIN
            Warehouse ON EmployeeVehicle.VehicleID = Warehouse.WarehouseID INNER JOIN
            Employee ON [Transaction].EmployeeID = Employee.EmployeeID
            WHERE [Transaction].Synchronized = 0 AND [Transaction].Voided = 0 
            AND [Transaction].TransactionTypeID IN (1,3)
            AND [Transaction].TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
            AND [Transaction].TransactionDate < '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + "'";

                if (!AllSalespersons)
                {
                    QueryString += " AND [Transaction].EmployeeID = " + Salesperson;
                }

                InCubeQuery GetSalesTransactionInformation = new InCubeQuery(db_vms, QueryString);

                err = GetSalesTransactionInformation.Execute();
                ClearProgress();
                SetProgressMax(GetSalesTransactionInformation.GetDataTable().Rows.Count);
                err = GetSalesTransactionInformation.FindFirst();
                while (err == InCubeErrors.Success)
                {
                    try
                    {

                        ReportProgress("Sending Invoices");
                        Customeraddress = "";
                        Customeraddress1 = "";
                        Customeraddress2 = "";
                        object field = new object();
                        decimal TOTAL = 0;
                        int LINSEQ = 0;
                        string SLPRSNID = "";
                        _DOCID = "";

                        #region Get SalesTransaction Information
                        {
                            err = GetSalesTransactionInformation.GetField(0, ref TransactionID);
                            err = GetSalesTransactionInformation.GetField(1, ref TransactionDate);
                            err = GetSalesTransactionInformation.GetField(2, ref CustomerCode);
                            err = GetSalesTransactionInformation.GetField(3, ref WarehouseCode);
                            err = GetSalesTransactionInformation.GetField(4, ref EmployeeCode);
                            err = GetSalesTransactionInformation.GetField(5, ref CustomerName);
                            err = GetSalesTransactionInformation.GetField(6, ref CustomerID);
                            err = GetSalesTransactionInformation.GetField(7, ref OutletID);
                            err = GetSalesTransactionInformation.GetField(8, ref Customeraddress);
                            err = GetSalesTransactionInformation.GetField(9, ref OutletCode);
                            err = GetSalesTransactionInformation.GetField(10, ref CustomerRefNo);
                            err = GetSalesTransactionInformation.GetField(11, ref SALESMODE);
                            err = GetSalesTransactionInformation.GetField(12, ref Notes);
                        }
                        #endregion

                        GetAddress(CustomerCode.ToString(), ref Customeraddress, ref Customeraddress1, ref Customeraddress2);
                        date = DateTime.Parse(TransactionDate.ToString());

                        string CustomerType = GetFieldValue("Customeroutlet", "CustomerTypeID", " CustomerID = " + CustomerID.ToString() + " AND OutletID = " + OutletID.ToString(), db_vms);
                        if (CustomerType.Trim() == "1")
                        {
                            StreamWriter wrt = new StreamWriter("errorInv.log", true);
                            wrt.Write(" THE DOCID IS CASH (CSP)");
                            wrt.Close();

                            SLPRSNID = "CASH";
                            _DOCID = "CSP";
                        }
                        else if (CustomerType.Trim() == "2")
                        {
                            StreamWriter wrt = new StreamWriter("errorInv.log", true);
                            wrt.Write(" THE DOCID IS CREDIT (CRP)");
                            wrt.Close();
                            SLPRSNID = "CREDIT";
                            _DOCID = "CRP";
                        }
                        else if (CustomerType.Trim().Equals(string.Empty))
                        {
                            throw new Exception("CONNECTION WAS DROPPED");
                        }

                        string COUNTRY = GetFieldValue("IV40700", "STATE", " rtrim(ltrim(LOCNCODE)) = '" + EmployeeCode + "'", db_ERP);
                        strDOC = COUNTRY.Trim();

                        if (strDOC.Trim() == string.Empty)
                        {
                            throw new Exception("State is not found in table IV40700 form employee ('" + EmployeeCode + "')");
                        }

                        //salesHdr.TAXSCHID = GetFieldValue("AINCustomerInfoGP", "TAXSCHID", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                        salesHdr.CREATETAXES = 1;
                        salesHdr.USINGHEADERLEVELTAXES = 0;

                        #region Details

                        string dtlQryStr = @"SELECT     
TransactionDetail.TransactionID,
TransactionDetail.BatchNo,
TransactionDetail.Quantity,
TransactionDetail.Price, 
TransactionDetail.ExpiryDate, 
TransactionDetail.Discount, 
ItemLanguage.Description AS ItemName, 
Item.ItemCode as Barcode, 
PackTypeLanguage.Description AS PackName, 
Pack.Quantity AS PcsInCse, 
TransactionDetail.PackID,
TransactionDetail.FOCTypeID,
TransactionDetail.Tax,
TransactionDetail.BasePrice

FROM TransactionDetail INNER JOIN
Pack ON TransactionDetail.PackID = Pack.PackID INNER JOIN
ItemLanguage ON Pack.ItemID = ItemLanguage.ItemID INNER JOIN
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID INNER JOIN
Item ON ItemLanguage.ItemID = Item.ItemID
WHERE (PackTypeLanguage.LanguageID = 1) AND (ItemLanguage.LanguageID = 1) AND (TransactionDetail.TransactionID = '" + TransactionID.ToString() + "') AND (TransactionDetail.CustomerID = " + CustomerID.ToString() + ")  AND (TransactionDetail.OutletID = " + OutletID.ToString() + ")";


                        InCubeQuery dtlQry = new InCubeQuery(db_vms, dtlQryStr, 1000000);
                        err = dtlQry.Execute();
                        DataRow[] detailsList = dtlQry.GetDataTable().Select();
                        int count = detailsList.Length;
                        int[] detailsListLNITMSEQ = new int[count];
                        List<taSopLineIvcInsert_ItemsTaSopLineIvcInsert> LineItems = new List<taSopLineIvcInsert_ItemsTaSopLineIvcInsert>();

                        if (count == 0)
                        {
                            throw new Exception("No details found , Invoice Number = " + TransactionID.ToString());
                        }

                        decimal TaxTotal = 0;
                        decimal DiscountTotal = 0;
                        for (int i = 0; i < count; i++)
                        {

                            DataRow salesTxRow = detailsList[i];

                            decimal Quantity = decimal.Parse(salesTxRow["Quantity"].ToString().Trim());

                            DateTime expdate = (DateTime)salesTxRow["ExpiryDate"];

                            if (!int.TryParse(salesTxRow["FOCTypeID"].ToString().Trim(), out FOCTypeID))
                            {
                                FOCTypeID = 1;
                            }

                            string ItemCode = salesTxRow["Barcode"].ToString().Trim();
                            string packCode = salesTxRow["PackName"].ToString().Trim();
                            STRPack = salesTxRow["ItemName"].ToString().Trim();
                            decimal Discount = decimal.Parse(salesTxRow["Discount"].ToString());
                            decimal unitPrice = decimal.Parse(salesTxRow["Price"].ToString());
                            decimal LineTax = decimal.Parse(salesTxRow["Tax"].ToString());
                            decimal BasePrice = decimal.Parse(salesTxRow["BasePrice"].ToString());

                            #region line items
                            taSopLineIvcInsert_ItemsTaSopLineIvcInsert salesLine = new taSopLineIvcInsert_ItemsTaSopLineIvcInsert();
                            LINSEQ = LINSEQ + 16384;
                            salesLine.ITEMNMBR = ItemCode;
                            salesLine.UNITCOST = 0;
                            salesLine.UNITCOSTSpecified = false;
                            salesLine.NONINVEN = 0;

                            if (Customeraddress != null)
                                salesLine.ADDRESS1 = Customeraddress.ToString().Trim();
                            if (Customeraddress1 != null)
                                salesLine.ADDRESS2 = Customeraddress1.ToString().Trim();
                            if (Customeraddress2 != null)
                                salesLine.ADDRESS3 = Customeraddress2.ToString().Trim();

                            salesLine.CUSTNMBR = CustomerCode.ToString().Trim();

                            salesLine.SALSTERR = EmployeeCode.ToString().Trim();
                            salesLine.SLPRSNID = SLPRSNID;

                            salesLine.SOPNUMBE = TransactionID.ToString().Trim();
                            salesLine.LOCNCODE = WarehouseCode.ToString().Trim();
                            salesLine.DOCID = _DOCID;
                            salesLine.DOCDATE = date.ToString("yyyy-MM-dd");
                            salesLine.SOPTYPE = 3;
                            salesLine.UOFM = packCode;
                            STRPack = STRPack.Split('_')[0];
                            salesLine.ITEMDESC = STRPack;
                            detailsListLNITMSEQ[i] = LINSEQ;
                            salesLine.LNITMSEQ = LINSEQ;
                            salesLine.AUTOALLOCATELOT = 0;
                            salesLine.ALLOCATE = 1;
                            salesLine.QTYFULFISpecified = true;
                            salesLine.QTYFULFI = Quantity;
                            salesLine.QUANTITY = Quantity;

                            #region price and discount

                            Discount = Math.Round(Discount, 2, MidpointRounding.AwayFromZero);
                            DiscountTotal += Discount;
                            decimal XTNDPRCE = Math.Round(Quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
                            if (LineTax > 0)
                            {
                                decimal taxPerc = 0;
                                if (XTNDPRCE > 0)
                                {
                                    taxPerc = LineTax / (XTNDPRCE - Discount) * 100;
                                    taxPerc = Math.Round(taxPerc, 2, MidpointRounding.AwayFromZero);
                                    LineTax = Math.Round(XTNDPRCE * taxPerc / 100, 2);
                                }
                                else
                                {
                                    taxPerc = LineTax / (BasePrice * Quantity) * 100;
                                    taxPerc = Math.Round(taxPerc, 2, MidpointRounding.AwayFromZero);
                                    LineTax = Math.Round(BasePrice * Quantity * taxPerc / 100, 2);
                                }
                            }
                            TaxTotal += LineTax;

                            //salesLine.TAXAMNT = LineTax;
                            salesLine.UNITPRCE = unitPrice;
                            TOTAL += XTNDPRCE;
                            salesLine.XTNDPRCE = XTNDPRCE;
                            salesLine.MRKDNAMTSpecified = true;
                            //salesLine.TAXSCHID = salesHdr.TAXSCHID;
                            #endregion

                            LineItems.Add(salesLine);
                            #endregion
                        }

                        dtlQry.Close();

                        #endregion


                        #region invoice header

                        salesHdr.SOPTYPE = 3;
                        salesHdr.SOPNUMBE = TransactionID.ToString().Trim();
                        salesHdr.DOCDATE = date.ToString("yyyy-MM-dd");
                        salesHdr.CUSTNMBR = CustomerCode.ToString().Trim();
                        salesHdr.CUSTNAME = CustomerName.ToString().Trim();
                        salesHdr.PYMTRMID = GetPaytrmID(CustomerCode.ToString()).Trim();
                        salesHdr.CSTPONBR = CustomerRefNo.ToString();//HERE IS THE NEW CUSTOMER REF FIELD.
                        salesHdr.SALSTERR = WarehouseCode.ToString().Trim();
                        //salesHdr.TAXAMNT = TaxTotal;
                        salesHdr.TRDISAMT = DiscountTotal;
                        salesHdr.TRDISAMTSpecified = true;
                        salesHdr.BACHNUMB = strDOC + "-" + date.ToString("ddMMyy");
                        salesHdr.SUBTOTAL = TOTAL;
                        StreamWriter XX = new StreamWriter("errorInv.log", true);
                        XX.Write(" TOTAL IS " + TOTAL.ToString());
                        XX.Close();
                        salesHdr.DOCAMNT = TOTAL - DiscountTotal;

                        if (TOTAL == 0)
                        {
                            switch (FOCTypeID)
                            {
                                case 1:
                                    _DOCID = "FOCG";
                                    break;
                                case 2:
                                    _DOCID = "FOCP";
                                    break;
                                case 3:
                                    _DOCID = "FOCS";
                                    break;
                            }
                        }

                        salesHdr.DOCID = _DOCID;
                        salesHdr.USER2ENT = "InCube";
                        salesHdr.ShipToName = CustomerName.ToString().Trim();
                        salesHdr.LOCNCODE = WarehouseCode.ToString().Trim();

                        if (Customeraddress != null)
                            salesHdr.ADDRESS1 = Customeraddress.ToString().Trim();
                        if (Customeraddress1 != null)
                            salesHdr.ADDRESS2 = Customeraddress1.ToString().Trim();
                        if (Customeraddress2 != null)
                            salesHdr.ADDRESS3 = Customeraddress2.ToString().Trim();


                        salesHdr.SHIPMTHD = GetFieldValue("AINCustomerInfoGP", "SHIPMTHD", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                        salesHdr.CITY = GetFieldValue("AINCustomerInfoGP", "CITY", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                        salesHdr.STATE = GetFieldValue("AINCustomerInfoGP", "STATE", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                        salesHdr.ZIPCODE = GetFieldValue("AINCustomerInfoGP", "ZIPCODE", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                        salesHdr.COUNTRY = GetFieldValue("AINCustomerInfoGP", "COUNTRY", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                        salesHdr.CNTCPRSN = GetFieldValue("AINCustomerInfoGP", "CNTCPRSN", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                        salesHdr.PHNUMBR1 = GetFieldValue("AINCustomerInfoGP", "PHNUMBR1", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                        salesHdr.PHNUMBR2 = GetFieldValue("AINCustomerInfoGP", "PHNUMBR2", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                        salesHdr.PHNUMBR3 = GetFieldValue("AINCustomerInfoGP", "PHNUMBR3", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                        salesHdr.FAXNUMBR = GetFieldValue("AINCustomerInfoGP", "FAXNUMBR", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                        salesHdr.SLPRSNID = SLPRSNID;// GetFieldValue("AINCustomerInfoGP", "SLPRSNID", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);

                        //salesHdr.FRTSCHID = "VAT@5%";
                        //salesHdr.FREIGTBLE = 1;
                        //       salesHdr.FRTTXAMT = 0;
                        //salesHdr.MSCSCHID = "VAT@5%";
                        //salesHdr.MISCTBLE = 1;
                        //salesHdr.MSCTXAMT = 0;

                        #endregion


                        salesOrder.taSopLineIvcInsert_Items = LineItems.ToArray();
                        salesOrder.taSopHdrIvcInsert = salesHdr;

                        eConnectType eConnect = new eConnectType();
                        SOPTransactionType[] MySopTransactionType = { salesOrder };
                        eConnect.SOPTransactionType = MySopTransactionType;
                        string salesOrderDocument;

                        #region Create xml file

                        string fname = filename + TransactionID.ToString().Trim() + ".xml";

                        // MessageBox.Show(filename);
                        FileStream fs = new FileStream(fname, FileMode.Create);
                        XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                        XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                        serializer.Serialize(writer, eConnect);
                        writer.Close();


                        // MessageBox.Show("Write File");
                        #endregion

                        #region  Send xml file to GPs

                        //  MessageBox.Show(fname);
                        eConnectMethods eConCall = new eConnectMethods();

                        XmlDocument xmldoc = new XmlDocument();
                        xmldoc.Load(fname);
                        salesOrderDocument = xmldoc.OuterXml;
                        //eConCall.CreateEntity(sConnectionString, salesOrderDocument);
                        eConCall.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, salesOrderDocument, EnumTypes.SchemaValidationType.None, "");

                        #endregion

                        SetSynchronizedFlag(TransactionID.ToString(), SALESMODE.ToString(), Notes.ToString(), "I");
                        //This query to set SALSTERR and SLPRSNID since sending to GP doesnt affect those fields
                        InCubeQuery UpdateQuery = new InCubeQuery(db_ERP, "Update SOP10200 SET SALSTERR = '" + EmployeeCode.ToString() + "', SLPRSNID = '" + SLPRSNID + "' where  SOPNUMBE = '" + TransactionID.ToString() + "' AND SOPTYPE = 3");
                        err = UpdateQuery.Execute();

                        WriteMessage("\r\n" + TransactionID.ToString() + " - OK");
                        XX = new StreamWriter("errorInv.log", true);
                        XX.Write("\n" + TransactionID.ToString() + " OK\r\n");
                        XX.Close();

                    }
                    catch (Exception ex)
                    {
                        StreamWriter wrt = new StreamWriter("errorInv.log", true);
                        wrt.Write(ex.ToString());
                        wrt.Close();

                        if (ex.ToString().Contains("Error Description = Duplicate document number"))
                        {
                            WriteMessage("\r\n" + TransactionID.ToString() + " - Already avaialble in GP, flag will be set to 1");
                            SetSynchronizedFlag(TransactionID.ToString(), SALESMODE.ToString(), Notes.ToString(), "I");
                        }
                        else
                        {
                            WriteMessage("\r\n" + TransactionID.ToString() + " - FAILED!");
                        }
                        ret++;
                    }
                    err = GetSalesTransactionInformation.FindNext();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return ret;
        }

        #endregion

        #region SendReturns

        public override void SendReturn()
        {
            SendInvoiceWithVAT(CoreGeneral.Common.StartupPath + "\\E-Connect\\", Filters.EmployeeID == -1, Filters.EmployeeID.ToString(), Filters.FromDate, Filters.ToDate, IntegrationField.Returns_S);
        }

        public int SerializeSalesOrderObjectSendTransactionReturn(string filename, bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate)
        {
            int ret = 0;
            string strDOC = "";
            string _DOCID = "";
            WriteMessage("\r\n" + "Send Return");
            InCubeRow SalesTransactionRow1 = new InCubeRow();
            InCubeRow PackRow = new InCubeRow();
            InCubeTable SalesTransactionDetail = new InCubeTable();
            InCubeTable PackTable = new InCubeTable();
            SOPTransactionType salesOrder = new SOPTransactionType();
            taSopHdrIvcInsert salesHdr = new taSopHdrIvcInsert();


            object TransactionID = "";
            object TransactionDate = "";
            object CustomerName = "";
            object CustomerCode = "";
            object WarehouseCode = "";
            object EmployeeCode = "";
            object CustomerID = "";
            object OutletID = "";
            string STRPack = "";
            object CustomerAddress = "";
            object CustomerAddress1 = "";
            object CustomerAddress2 = "";
            object OutletCode = "";
            object SALESMODE = "";
            object Notes = "";
            DateTime date;
            string SLPRSNID = "";
            object CustomerRefNo = "";
            if (UpdateCustomerInfoFromGP(FromDate, ToDate) != InCubeErrors.Success)
            {
                WriteMessage("\r\n Falied to update customer information fom GP");
                return 0;
            }

            string QueryString = @"SELECT     
            [Transaction].TransactionID, 
            [Transaction].TransactionDate, 
            CustomerOutlet.Barcode AS CustomerCode, 
            Warehouse.Barcode AS WarehouseCode, 
            Employee.EmployeeCode, 
            CustomerOutletLanguage.Description,
            CustomerOutlet.CustomerID,CustomerOutlet.OutletID,
            CustomerOutletLanguage.Address,
            CustomerOutlet.CustomerCode as OutletCode,[Transaction].CustomerRefNo,
            CASE WHEN [Transaction].CUSTOMERID IN (SELECT CUSTOMERID FROM ROUTE WHERE ROUTEID=[Transaction].ROUTEID) THEN 'VAR' ELSE 'RTN' END SALESMODE,
            ISNULL([Transaction].Notes,'') Notes
            FROM [Transaction] INNER JOIN
            CustomerOutletLanguage ON [Transaction].CustomerID = CustomerOutletLanguage.CustomerID INNER JOIN
            CustomerOutlet ON [Transaction].CustomerID = CustomerOutlet.CustomerID AND [Transaction].OutletID = CustomerOutlet.OutletID AND 
            CustomerOutletLanguage.CustomerID = CustomerOutlet.CustomerID AND CustomerOutletLanguage.OutletID = CustomerOutlet.OutletID INNER JOIN
            EmployeeVehicle ON [Transaction].EmployeeID = EmployeeVehicle.EmployeeID INNER JOIN
            Warehouse ON EmployeeVehicle.VehicleID = Warehouse.WarehouseID INNER JOIN
            Employee ON [Transaction].EmployeeID = Employee.EmployeeID
            WHERE ([Transaction].Synchronized = 0) AND ([Transaction].Voided = 0) AND ([Transaction].TransactionTypeID = 2 or [Transaction].TransactionTypeID = 4 or ([Transaction].TransactionTypeID = 1 and [Transaction].CreationReason=2 
and isnull((select count(*) from transactiondetail td where [Transaction].transactionid=td.transactionid and 
 [Transaction].customerid=td.customerid and  [Transaction].outletid=td.outletid and td.PackStatusID<>0 ),0)>0)) AND 
            ([Transaction].TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
            AND [Transaction].TransactionDate < '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + "')";

            if (!AllSalespersons)
            {
                QueryString += " AND [Transaction].EmployeeID = " + Salesperson;
            }

            InCubeQuery GetSalesTransactionInformation = new InCubeQuery(db_vms, QueryString);

            err = GetSalesTransactionInformation.Execute();
            ClearProgress();
            SetProgressMax(GetSalesTransactionInformation.GetDataTable().Rows.Count);
            err = GetSalesTransactionInformation.FindFirst();
            while (err == InCubeErrors.Success)
            {
                ReportProgress("Sending Returns");

                object field = new object();
                decimal TOTAL = 0;
                int LINTMSEQ = 0;

                #region  Get SalesTransaction Information
                {
                    err = GetSalesTransactionInformation.GetField(0, ref TransactionID);
                    err = GetSalesTransactionInformation.GetField(1, ref TransactionDate);
                    err = GetSalesTransactionInformation.GetField(2, ref CustomerCode);
                    err = GetSalesTransactionInformation.GetField(3, ref WarehouseCode);
                    err = GetSalesTransactionInformation.GetField(4, ref EmployeeCode);
                    err = GetSalesTransactionInformation.GetField(5, ref CustomerName);
                    err = GetSalesTransactionInformation.GetField(6, ref CustomerID);
                    err = GetSalesTransactionInformation.GetField(7, ref OutletID);
                    err = GetSalesTransactionInformation.GetField(8, ref CustomerAddress);
                    err = GetSalesTransactionInformation.GetField(9, ref OutletCode);
                    err = GetSalesTransactionInformation.GetField(10, ref CustomerRefNo);
                    err = GetSalesTransactionInformation.GetField(11, ref SALESMODE);
                    err = GetSalesTransactionInformation.GetField(12, ref Notes);
                }
                #endregion

                try
                {
                    GetAddress(CustomerCode.ToString(), ref CustomerAddress, ref CustomerAddress1, ref CustomerAddress2);
                    date = DateTime.Parse(TransactionDate.ToString());

                    salesHdr.SOPTYPE = 4;

                    salesHdr.SOPNUMBE = TransactionID.ToString();
                    salesHdr.CSTPONBR = CustomerRefNo.ToString();//HERE IS THE NEW FIELD CustomerRefNo.
                    salesHdr.SLPRSNID = EmployeeCode.ToString();

                    salesHdr.DOCDATE = date.ToString("dd-MM-yyyy");
                    salesHdr.CUSTNMBR = CustomerCode.ToString();
                    //CustomerName = CustomerName.ToString().Split('-')[0];
                    salesHdr.CUSTNAME = CustomerName.ToString();
                    int CustomerIDInt = int.Parse(CustomerID.ToString());
                    int OutletIDInt = int.Parse(OutletID.ToString());
                    SLPRSNID = "";
                    _DOCID = "";

                    string CustomerType = GetFieldValue("Customeroutlet", "CustomerTypeID", " CustomerID = " + CustomerID.ToString() + " AND OutletID = " + OutletID.ToString(), db_vms);
                    if (CustomerType.Trim() == "1")
                    {
                        SLPRSNID = "CASH";
                        _DOCID = "RCS";
                    }
                    else if (CustomerType.Trim() == "2")
                    {
                        SLPRSNID = "CREDIT";
                        _DOCID = "RCR";
                    }
                    else if (CustomerType.Trim().Equals(string.Empty))
                    {
                        throw new Exception("CONNECTION WAS DROPPED");
                    }
                    string COUNTRY = GetFieldValue("IV40700", "STATE", " rtrim(ltrim(LOCNCODE)) = '" + EmployeeCode + "'", db_ERP);
                    strDOC = COUNTRY.Trim();

                    salesHdr.SHIPMTHD = GetFieldValue("AINCustomerInfoGP", "SHIPMTHD", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                    salesHdr.CITY = GetFieldValue("AINCustomerInfoGP", "CITY", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                    salesHdr.STATE = GetFieldValue("AINCustomerInfoGP", "STATE", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                    salesHdr.ZIPCODE = GetFieldValue("AINCustomerInfoGP", "ZIPCODE", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                    salesHdr.COUNTRY = GetFieldValue("AINCustomerInfoGP", "COUNTRY", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                    salesHdr.CNTCPRSN = GetFieldValue("AINCustomerInfoGP", "CNTCPRSN", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                    salesHdr.PHNUMBR1 = GetFieldValue("AINCustomerInfoGP", "PHNUMBR1", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                    salesHdr.PHNUMBR2 = GetFieldValue("AINCustomerInfoGP", "PHNUMBR2", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                    salesHdr.PHNUMBR3 = GetFieldValue("AINCustomerInfoGP", "PHNUMBR3", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                    salesHdr.FAXNUMBR = GetFieldValue("AINCustomerInfoGP", "FAXNUMBR", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                    salesHdr.SLPRSNID = SLPRSNID;// GetFieldValue("AINCustomerInfoGP", "SLPRSNID", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                    salesHdr.TAXSCHID = GetFieldValue("AINCustomerInfoGP", "TAXSCHID", " CustomerCode = '" + CustomerCode.ToString().Trim() + "'", db_vms);
                    salesHdr.CREATETAXES = 1;
                    salesHdr.USINGHEADERLEVELTAXES = 0;

                    salesHdr.SALSTERR = EmployeeCode.ToString();

                    if (CustomerAddress != null)
                        salesHdr.ADDRESS1 = CustomerAddress.ToString().Trim();
                    if (CustomerAddress1 != null)
                        salesHdr.ADDRESS2 = CustomerAddress1.ToString().Trim();
                    if (CustomerAddress2 != null)
                        salesHdr.ADDRESS3 = CustomerAddress2.ToString().Trim();

                    salesHdr.PYMTRMID = GetPaytrmID(CustomerCode.ToString()).Trim();

                    string dtlQryStr = @"SELECT     
TransactionDetail.TransactionID,
TransactionDetail.BatchNo,
TransactionDetail.Quantity,
(case(TransactionDetail.PackStatusID) when 0 then 0 else TransactionDetail.Price end ) Price,
TransactionDetail.ExpiryDate, 
TransactionDetail.Discount, 
ItemLanguage.Description AS ItemName, 
Item.ItemCode AS Barcode, 
PackTypeLanguage.Description AS PackName, 
Pack.Quantity AS PcsInCse, 
TransactionDetail.PackID,
Item.ItemCode,
TransactionDetail.PackStatusID,
TransactionDetail.Tax
FROM TransactionDetail INNER JOIN  
Pack ON TransactionDetail.PackID = Pack.PackID INNER JOIN
ItemLanguage ON Pack.ItemID = ItemLanguage.ItemID INNER JOIN
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID INNER JOIN
Item ON ItemLanguage.ItemID = Item.ItemID
WHERE (PackTypeLanguage.LanguageID = 1) AND (TransactionDetail.PackStatusID<>0)  AND (ItemLanguage.LanguageID = 1) AND (TransactionDetail.TransactionID = '" + TransactionID.ToString() + "') AND TransactionDetail.CustomerID = " + CustomerID.ToString() + "  AND TransactionDetail.OutletID = " + OutletID.ToString();

                    InCubeQuery dtlQry = new InCubeQuery(db_vms, dtlQryStr, 1000000);
                    err = dtlQry.Execute();
                    DataRow[] detailsList = dtlQry.GetDataTable().Select();

                    int count = detailsList.Length;
                    int[] detailsListLNITMSEQ = new int[count];
                    taSopLineIvcInsert_ItemsTaSopLineIvcInsert[] LineItems = new taSopLineIvcInsert_ItemsTaSopLineIvcInsert[count];
                    taSopLotAuto_ItemsTaSopLotAuto[] LotNumberItems = new taSopLotAuto_ItemsTaSopLotAuto[count];

                    if (count == 0)
                    {
                        throw new Exception("No details found , Invoice Number = " + TransactionID.ToString());
                    }

                    decimal DiscountTotal = 0;
                    for (int i = 0; i < count; i++)
                    {
                        DataRow salesTxRow = detailsList[i];

                        taSopLotAuto_ItemsTaSopLotAuto LotNumber = new taSopLotAuto_ItemsTaSopLotAuto();
                        taSopLineIvcInsert_ItemsTaSopLineIvcInsert salesLine = new taSopLineIvcInsert_ItemsTaSopLineIvcInsert();
                        salesLine.CUSTNMBR = CustomerCode.ToString();
                        salesLine.AUTOALLOCATELOT = 0;
                        salesLine.ALLOCATE = 1;


                        salesLine.SALSTERR = EmployeeCode.ToString().Trim();
                        salesLine.SLPRSNID = SLPRSNID;

                        salesLine.SOPNUMBE = TransactionID.ToString();
                        // if (SALESMODE.ToString().ToLower().Equals("var")) salesLine.SOPNUMBE = "VAR"+TransactionID.ToString();
                        salesLine.LOCNCODE = WarehouseCode.ToString();
                        salesLine.DOCID = _DOCID;
                        salesLine.DOCDATE = date.ToString("yyyy-MM-dd");
                        salesLine.SOPTYPE = 4;
                        string PackStatus = salesTxRow["PackStatusID"].ToString();
                        decimal Discount = decimal.Parse(salesTxRow["Discount"].ToString());
                        Discount = Math.Round(Discount, 2, MidpointRounding.AwayFromZero);
                        DiscountTotal += Discount;

                        field = salesTxRow["Price"];
                        if (SALESMODE.ToString().ToLower().Equals("var"))
                        {
                            field = "0";
                            PackStatus = "3";
                        }
                        decimal unitPrice = decimal.Parse(field.ToString());

                        field = salesTxRow["PackID"];

                        salesLine.ITEMNMBR = salesTxRow["Barcode"].ToString().Trim();

                        salesLine.UOFM = salesTxRow["PackName"].ToString().Trim();

                        STRPack = salesTxRow["ItemName"].ToString().Trim();
                        salesLine.ITEMDESC = STRPack;

                        field = salesTxRow["Quantity"];
                        decimal Quantity = decimal.Parse(field.ToString());


                        switch (PackStatus)
                        {
                            case "1": // Damaged
                                salesLine.QTYDMGED = Quantity;
                                salesLine.QTYINSVC = 0;
                                salesLine.QTYONHND = 0;
                                break;
                            case "2"://Expired
                                salesLine.QTYINSVC = Quantity;
                                salesLine.QTYDMGED = 0;
                                salesLine.QTYONHND = 0;
                                break;
                            case "3"://In Good Condition
                                salesLine.QTYONHND = Quantity;
                                salesLine.QTYDMGED = 0;
                                salesLine.QTYINSVC = 0;
                                break;
                        }


                        salesLine.QUANTITY = Quantity;

                        salesLine.COMMNTID = SALESMODE.ToString();


                        #region price and discount

                        Discount = Math.Round(Discount, 3, MidpointRounding.AwayFromZero);
                        Discount = Math.Round(Discount / Quantity, 3, MidpointRounding.AwayFromZero) * Quantity;
                        //unitPrice = Math.Round(unitPrice, 3, MidpointRounding.AwayFromZero);

                        decimal XTNDPRCE = Math.Round(Quantity * unitPrice, 2, MidpointRounding.AwayFromZero);

                        salesLine.UNITPRCE = unitPrice;
                        TOTAL += XTNDPRCE;
                        salesLine.XTNDPRCE = XTNDPRCE;
                        //salesLine.TAXSCHID = salesHdr.TAXSCHID;
                        //salesLine.ITMTSHID = salesHdr.TAXSCHID;
                        salesLine.MRKDNAMTSpecified = true;

                        #endregion

                        salesLine.QTYRTRND = 0;
                        salesLine.QTYINUSE = 0;
                        salesLine.NONINVEN = 0;
                        salesLine.DROPSHIP = 0;
                        salesLine.QTYTBAOR = 0;

                        LINTMSEQ = LINTMSEQ + 16384;
                        salesLine.LNITMSEQ = LINTMSEQ;
                        detailsListLNITMSEQ[i] = LINTMSEQ;
                        field = salesTxRow["ExpiryDate"];
                        DateTime LotDate = DateTime.Parse(field.ToString());
                        field = salesTxRow["BatchNo"];
                        string BATCH = field.ToString();

                        salesLine.NONINVEN = 0;
                        salesLine.UNITCOST = 0;
                        salesLine.UNITCOSTSpecified = false;

                        if (CustomerAddress != null)
                            salesLine.ADDRESS1 = CustomerAddress.ToString().Trim();
                        if (CustomerAddress1 != null)
                            salesLine.ADDRESS2 = CustomerAddress1.ToString().Trim();
                        if (CustomerAddress2 != null)
                            salesLine.ADDRESS3 = CustomerAddress2.ToString().Trim();

                        LineItems[i] = salesLine;
                    }

                    salesHdr.ORIGTYPE = 0;
                    salesHdr.TAXAMNT = 0;
                    salesHdr.FRTTXAMT = 0;
                    salesHdr.MSCTXAMT = 0;
                    salesHdr.MSTRNUMB = 0;
                    salesHdr.FREIGHT = 0;
                    salesHdr.MISCAMNT = 0;
                    salesHdr.ORIGTYPE = 0;
                    salesHdr.DISTKNAM = 0;
                    salesHdr.MRKDNAMT = 0;
                    salesHdr.FRTTXAMT = 0;
                    salesHdr.USER2ENT = "InCube";

                    salesHdr.BACHNUMB = strDOC + "-" + date.ToString("ddMMyy"); ;

                    salesHdr.SUBTOTAL = TOTAL;
                    salesHdr.TRDISAMT = DiscountTotal;
                    salesHdr.TRDISAMTSpecified = true;
                    salesHdr.DOCAMNT = TOTAL - DiscountTotal;
                    salesHdr.DOCID = _DOCID;
                    salesHdr.LOCNCODE = WarehouseCode.ToString();
                    salesHdr.DOCDATE = date.ToString("yyyy-MM-dd");

                    salesOrder.taSopLineIvcInsert_Items = LineItems;
                    salesOrder.taSopHdrIvcInsert = salesHdr;

                    eConnectType eConnect = new eConnectType();
                    SOPTransactionType[] MySopTransactionType = { salesOrder };
                    eConnect.SOPTransactionType = MySopTransactionType;

                    string salesOrderDocument;

                    #region Create xml file

                    string fname = filename + TransactionID.ToString().Trim() + ".xml";

                    FileStream fs = new FileStream(fname, FileMode.Create);
                    XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                    XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                    serializer.Serialize(writer, eConnect);
                    writer.Close();



                    #endregion

                    try
                    {

                        #region  Send xml file to GPs

                        eConnectMethods eConCall = new eConnectMethods();

                        XmlDocument xmldoc = new XmlDocument();
                        xmldoc.Load(fname);
                        salesOrderDocument = xmldoc.OuterXml;
                        eConCall.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, salesOrderDocument, EnumTypes.SchemaValidationType.None, "");

                        #endregion

                        SetSynchronizedFlag(TransactionID.ToString(), SALESMODE.ToString(), Notes.ToString(), "R");
                        //This query to set SALSTERR and SLPRSNID since sending to GP doesnt affect those fields
                        InCubeQuery UpdateQuery = new InCubeQuery(db_ERP, "Update SOP10200 SET SALSTERR = '" + EmployeeCode.ToString() + "', SLPRSNID = '" + SLPRSNID + "' where  SOPNUMBE = '" + TransactionID.ToString() + "' AND SOPTYPE = 4");
                        err = UpdateQuery.Execute();

                        WriteMessage("\r\n" + TransactionID.ToString() + "-Done");
                        StreamWriter wrt1 = new StreamWriter("errorret.log", true);
                        wrt1.Write(TransactionID.ToString() + " - OK\r\n");
                        wrt1.Close();
                    }
                    catch (eConnectException exp)
                    {
                        Console.Write(exp.ToString());
                        StreamWriter wrt = new StreamWriter("errorret.log", true);
                        wrt.Write(exp.ToString());
                        wrt.Close();
                        if (exp.ToString().Contains("Error Description = Duplicate document number"))
                        {
                            WriteMessage("\r\n" + TransactionID.ToString() + " - Already avaialble in GP, flag will be set to 1");
                            SetSynchronizedFlag(TransactionID.ToString(), SALESMODE.ToString(), Notes.ToString(), "R");
                        }
                        else
                        {
                            WriteMessage("\r\n" + TransactionID.ToString() + " - FAILED!");
                        }
                        ret++;
                    }
                    TOTAL = 0;
                }
                catch (Exception ex)
                {
                    Console.Write(ex.ToString());
                    StreamWriter wrt = new StreamWriter("errorret.log", true);
                    wrt.Write(ex.ToString());
                    wrt.Close();
                    WriteMessage("\r\n" + TransactionID.ToString() + " - FAILED!");
                    ret = -1;
                }
                err = GetSalesTransactionInformation.FindNext();
            }

            return ret;
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


            InCubeRow CustomerPaymentRow = new InCubeRow();
            InCubeTable CustomerPayment = new InCubeTable();

            object field = new object();
            DateTime date;
            string PaymentID = "";
            string TransactionID = "";
            string PaymentType = "";
            string BankID = "";
            decimal Amount = 0.0m;
            string ChNumber = "";
            object ChDate = "";
            string CustomerCode = "";
            bool Check = false;
            object SalesPersonCode = "";
            object CustomerName = "";

            err = CustomerPayment.Open(db_vms, "CustomerPayment");
            RMCashReceiptsType CashType = new RMCashReceiptsType();

            string QueryString = @"SELECT 
                                         CustomerPayment.CustomerPaymentID AS PaymentID, 
                                         SUM(CustomerPayment.AppliedAmount) AS Amount, 
                                         PaymentTypeLanguage.PaymentTypeID AS PaymentType, 
                                         CustomerPayment.VoucherNumber, 
                                         CustomerPayment.VoucherDate, 
                                         CustomerPayment.BankID, 
                                         CustomerOutlet.Barcode, 
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

                                   Where (PaymentTypeLanguage.LanguageID = 1) 
                                          AND (CustomerOutletLanguage.LanguageID = 1) 
                                          AND (CustomerPayment.Synchronized = 0) 
                                          AND (CustomerPayment.PaymentStatusID <> 5)
                                          AND (CustomerPayment.PaymentDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
                                          AND  CustomerPayment.PaymentDate <= '" + ToDate.Date.ToString("yyyy/MM/dd") + @"')";


            if (!AllSalespersons)
            {
                QueryString += "    AND CustomerPayment.EmployeeID = " + Salesperson;
            }


            QueryString += @"      GROUP BY   CustomerPayment.CustomerPaymentID, PaymentTypeLanguage.PaymentTypeID, PaymentTypeLanguage.LanguageID, 
                                              CustomerPayment.VoucherNumber, CustomerPayment.VoucherDate, CustomerPayment.BankID, CustomerOutlet.Barcode, 
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

                    string cashCheckbookId = "DX -CASH";
                    string chequeCheckbookId = "DXCH";

                    InCubeQuery codeQry = new InCubeQuery(db_vms, "SELECT     FFOrganizationCheckCode.CashCode, FFOrganizationCheckCode.ChequeCode FROM  FFOrganizationCheckCode INNER JOIN EmployeeOrganization ON FFOrganizationCheckCode.OrganizationID = EmployeeOrganization.OrganizationID WHERE EmployeeOrganization.EmployeeID = " + SalesPersonCode.ToString());
                    codeQry.Execute();
                    if (codeQry.FindFirst() == InCubeErrors.Success)
                    {
                        codeQry.GetField("CashCode", ref field);
                        cashCheckbookId = field.ToString();
                        codeQry.GetField("ChequeCode", ref field);
                        chequeCheckbookId = field.ToString();
                    }
                    codeQry.Close();


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
                        CustomerPaymentCash.CSHRCTYP = 1;
                        CustomerPaymentCash.CHEKBKID = cashCheckbookId; //"DX -CASH";
                        CustomerPaymentCash.CURNCYID = "DHS";
                        string Batch = "RV" + date.ToString("dd/MM/yy").Replace("/", "") + GetSalespersonCode(SalesPersonCode);
                        CustomerPaymentCash.BACHNUMB = Batch;
                        CustomerPaymentCash.CURNCYID = "";
                        CustomerPaymentCash.CREATEDIST = 1;
                        CustomerPaymentCash.TRXDSCRN = "Apply to" + TransactionID;
                        eConnectType eConnect = new eConnectType();
                        DataRow[] DetailsPaymentRow = CustomerPayment.GetDataTable().Select("CustomerPaymentID='" + PaymentID + "'");
                        RMApplyType[] TYPE = new RMApplyType[DetailsPaymentRow.Length];
                        for (int i = 0; i < DetailsPaymentRow.Length; i++)
                        {
                            taRMApply RMApply = new taRMApply();
                            RMApplyType ApplyType = new RMApplyType();
                            DataRow Row = DetailsPaymentRow[i];
                            RMApply.APTODCNM = Row["TransactionID"].ToString();
                            RMApply.APFRDCNM = PaymentID.ToString();
                            RMApply.APPTOAMT = decimal.Parse(Row["RemainingAmount"].ToString());
                            RMApply.APFRDCTY = 9;
                            RMApply.APTODCTY = 1;
                            RMApply.APPLYDATE = date.ToString("yyyy-MM-dd");
                            RMApply.GLPOSTDT = DateTime.Now.ToString("yyyy-MM-dd");
                            ApplyType.taRMApply = RMApply;
                            TYPE[i] = ApplyType;

                        }
                        eConnect.RMApplyType = TYPE;
                        CashType.taRMCashReceiptInsert = CustomerPaymentCash;
                        RMCashReceiptsType[] CashReceipts = { CashType };
                        eConnect.RMCashReceiptsType = CashReceipts;

                        string salesOrderDocument = "";

                        #region Create xml file

                        string fname = filename + PaymentID.ToString().Trim() + ".xml";

                        FileStream fs = new FileStream(fname, FileMode.Create);
                        XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                        XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                        serializer.Serialize(writer, eConnect);
                        writer.Close();



                        #endregion

                        try
                        {

                            #region  Send xml file to GPs

                            eConnectMethods eConCall = new eConnectMethods();

                            XmlDocument xmldoc = new XmlDocument();
                            xmldoc.Load(fname);
                            salesOrderDocument = xmldoc.OuterXml;
                            eConCall.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, salesOrderDocument, EnumTypes.SchemaValidationType.None, "");

                            #endregion

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
                        string Batch = "RV" + date.ToString("dd/MM/yy").Replace("/", "") + GetSalespersonCode(SalesPersonCode);
                        CustomerPaymentCash.BACHNUMB = Batch;
                        CustomerPaymentCash.CURNCYID = "";
                        CustomerPaymentCash.CREATEDIST = 1;
                        CustomerPaymentCash.TRXDSCRN = "Apply to" + TransactionID;
                        CustomerPaymentCash.CHEKNMBR = ChNumber;
                        CustomerPaymentCash.CHEKBKID = chequeCheckbookId;
                        eConnectType eConnect = new eConnectType();
                        DataRow[] DetailsPaymentRow = CustomerPayment.GetDataTable().Select("CustomerPaymentID='" + PaymentID + "'");
                        RMApplyType[] TYPE = new RMApplyType[DetailsPaymentRow.Length];
                        for (int i = 0; i < DetailsPaymentRow.Length; i++)
                        {
                            taRMApply RMApply = new taRMApply();
                            RMApplyType ApplyType = new RMApplyType();
                            DataRow Row = DetailsPaymentRow[i];
                            RMApply.APTODCNM = Row["TransactionID"].ToString();
                            RMApply.APFRDCNM = PaymentID.ToString();
                            RMApply.APPTOAMT = decimal.Parse(Row["RemainingAmount"].ToString());
                            RMApply.APFRDCTY = 9;
                            RMApply.APTODCTY = 1;
                            RMApply.APPLYDATE = date.ToString("yyyy-MM-dd");
                            RMApply.GLPOSTDT = DateTime.Now.ToString("yyyy-MM-dd");
                            ApplyType.taRMApply = RMApply;
                            TYPE[i] = ApplyType;
                        }
                        eConnect.RMApplyType = TYPE;
                        CashType.taRMCashReceiptInsert = CustomerPaymentCash;
                        RMCashReceiptsType[] CashReceipts = { CashType };
                        eConnect.RMCashReceiptsType = CashReceipts;

                        string salesOrderDocument = "";

                        #region Create xml file

                        string fname = filename + PaymentID.ToString().Trim() + ".xml";

                        FileStream fs = new FileStream(fname, FileMode.Create);
                        XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                        XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                        serializer.Serialize(writer, eConnect);
                        writer.Close();



                        #endregion

                        try
                        {
                            #region  Send xml file to GPs

                            eConnectMethods eConCall = new eConnectMethods();

                            XmlDocument xmldoc = new XmlDocument();
                            xmldoc.Load(fname);
                            salesOrderDocument = xmldoc.OuterXml;
                            eConCall.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, salesOrderDocument, EnumTypes.SchemaValidationType.None, "");

                            #endregion
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
                        DataRow[] DetailsPaymentRow = CustomerPayment.GetDataTable().Select("CustomerPaymentID='" + PaymentID + "'");
                        RMApplyType[] TYPE = new RMApplyType[DetailsPaymentRow.Length];
                        for (int i = 0; i < DetailsPaymentRow.Length; i++)
                        {
                            taRMApply RMApply = new taRMApply();
                            RMApplyType ApplyType = new RMApplyType();
                            DataRow Row = DetailsPaymentRow[i];
                            RMApply.APTODCNM = Row["TransactionID"].ToString();
                            RMApply.APFRDCNM = Row["VoucherNmuber"].ToString();
                            RMApply.APPTOAMT = decimal.Parse(Row["RemainingAmount"].ToString());
                            RMApply.APFRDCTY = 8;
                            RMApply.APTODCTY = 1;
                            RMApply.APPLYDATE = date.ToString("yyyy-MM-dd");
                            RMApply.GLPOSTDT = DateTime.Now.ToString("yyyy-MM-dd");
                            ApplyType.taRMApply = RMApply;
                            TYPE[i] = ApplyType;
                        }
                        eConnect.RMApplyType = TYPE;

                        string salesOrderDocument = "";

                        #region Create xml file

                        string fname = filename + PaymentID.ToString().Trim() + ".xml";

                        FileStream fs = new FileStream(fname, FileMode.Create);
                        XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                        XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                        serializer.Serialize(writer, eConnect);
                        writer.Close();



                        #endregion

                        try
                        {
                            #region  Send xml file to GPs

                            eConnectMethods eConCall = new eConnectMethods();

                            XmlDocument xmldoc = new XmlDocument();
                            xmldoc.Load(fname);
                            salesOrderDocument = xmldoc.OuterXml;
                            eConCall.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, salesOrderDocument, EnumTypes.SchemaValidationType.None, "");

                            #endregion
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
                        Check = true;

                        //PDCRVNO : Receipt Number
                        //PDCRVDT: Receipt Date (due date of the PDC) 
                        //PDCCUSTID: Customer Code
                        //PDCDOCNO: ?? Cheque no
                        //PDCDOCDT: ?? Receipt Date
                        //PDCBANK: BankID 
                        //PDCAMOUNT: PDC Amount
                        //PDCONHANDDATE:  – collected date (current date)

                        string Query = @"Insert into PDCOPEN (PDCRVNO, PDCRVDT, PDCCUSTID, PDCDOCNO, PDCDOCDT, PDCBANK, PDCAMOUNT, PDCONHANDDATE) 
                        values ('" + PaymentID + "', '" + date.ToString("yyyy-MM-dd") + "', '" + CustomerCode + "', '" + ChNumber + "', '" + date.ToString("yyyy-MM-dd") + "', '" + BankID + "', " + Amount + ", '" + DateTime.Now.ToString("yyyy-MM-dd") + "')";

                        InCubeQuery PDCOPEN = new InCubeQuery(db_ERP, Query);
                        err = PDCOPEN.Execute();

                        WriteMessage("Insert into PDCOPEN " + err.ToString() + " Exception :" + PDCOPEN.GetCurrentException().ToString() + " Query : " + Query);

                        //BranchID: 3 letter code for branch (DXB,AUH)
                        //ReceiptNo: Receipt Number.
                        //ReceiptDate: Receipt Date. This is NOT the current date. It’s the cheque date (PDC)
                        //CustomerID: Customer Code
                        //CustomerName: Customer Name.
                        //CheckBookID: Chequebookid
                        //CurrencyID: its fixed "DHS"
                        //Bank: Bankid
                        //ChequeNumber: Cheque Number.
                        //DueDate:Receipt Date
                        //Amount: PDC Amount
                        //Status: 0 (zero)
                        //PrintCount: always 0 
                        //SPID: SalespersonID (S100, S200)

                        string Branch = "DXB";
                        InCubeQuery GetBranchCMD = new InCubeQuery(db_ERP, @"Select SALSTERR From RM00101 Where CUSTNMBR = '" + CustomerCode + "'");
                        GetBranchCMD.Execute();
                        GetBranchCMD.FindFirst();
                        GetBranchCMD.GetField(0, ref field);
                        Branch = field.ToString();

                        Query = @"Insert into PDCDetail (BranchID, ReceiptNo, ReceiptDate, CustomerID, CustomerName, CheckBookID, CurrencyID, Bank, ChequeNumber, DueDate, Amount, Status, PrintCount, SPID) 
                        Values ('" + Branch + "', '" + PaymentID + "', '" + date.ToString("yyyy-MM-dd") + "', '" + CustomerCode + "', '" + CustomerName + "', '" + chequeCheckbookId + "', 'DHS', '" + BankID + "', '" + ChNumber + "', '" + date.ToString("yyyy-MM-dd") + "', " + Amount + ", 0, 0, '" + SalesPersonCode.ToString() + "')";

                        InCubeQuery PDCDETAIL = new InCubeQuery(db_ERP, Query);
                        err = PDCDETAIL.Execute();

                        WriteMessage("Insert into PDCOPEN " + err.ToString() + " Exception :" + PDCOPEN.GetCurrentException().ToString() + " Query : " + Query);

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

            CustomerPayment.Close();
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
Warehouse.Barcode AS ToWh,
Warehouse_1.Barcode AS FromWh

FROM WarehouseTransaction INNER JOIN
Warehouse ON WarehouseTransaction.WarehouseID = Warehouse.WarehouseID INNER JOIN
Warehouse AS Warehouse_1 ON WarehouseTransaction.RefWarehouseID = Warehouse_1.WarehouseID

Where 
     WarehouseTransaction.Synchronized = 0 
AND (WarehouseTransaction.TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' AND  WarehouseTransaction.TransactionDate <= '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"') 
AND  WarehouseTransaction.TransactionTypeID = 1 AND WarehouseTransaction.WarehouseTransactionStatusID = 4";

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


                    string STATE = GetFieldValue("ALNIntegration", "IntLocation", " ColdStore = '" + TransferFromWarhouse + "'", db_ERP);

                    string TransferDetailsQuery = @"SELECT WhTransDetail.Quantity, Item.ItemCode Barcode
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

                    ClearProgress();
                    SetProgressMax(count);

                    foreach (DataRow Row in TransferDetailTable.Rows)
                    {
                        ReportProgress("Sending Transfers");

                        decimal Quantity = decimal.Parse(Row["Quantity"].ToString());
                        string Barcode = Row["Barcode"].ToString();

                        taIVTransferLineInsert_ItemsTaIVTransferLineInsert TransferLineItem = new taIVTransferLineInsert_ItemsTaIVTransferLineInsert();
                        TransferLineItem.IVDOCNBR = TransferID.ToString();
                        TransferLineItem.ITEMNMBR = Barcode;
                        TransferLineItem.TRXQTY = Quantity;
                        TransferLineItem.TRXLOCTN = TransferFromWarhouse.ToString();
                        TransferLineItem.TRNSTLOC = TransferToWarhouse.ToString();

                        TransferLineItemsList.Add(TransferLineItem);
                    }


                    TransferHeader.BACHNUMB = "ISS" + STATE + "_" + DateTime.Parse(TransferDate.ToString()).ToString("ddMMyy");
                    TransferHeader.IVDOCNBR = TransferID.ToString();
                    TransferHeader.DOCDATE = DateTime.Parse(TransferDate.ToString()).ToString("yyyy-MM-dd");
                    TransferHeader.POSTTOGL = 0;

                    TransferTransaction.taIVTransferHeaderInsert = TransferHeader;
                    TransferTransaction.taIVTransferLineInsert_Items = TransferLineItemsList.ToArray();

                    eConnectType eConnect = new eConnectType();
                    IVInventoryTransferType[] MyIVInventoryTransferType = { TransferTransaction };
                    eConnect.IVInventoryTransferType = MyIVInventoryTransferType;

                    string TransferDocument = "";

                    #region Create xml file

                    string fname = filename + TransferID.ToString().Trim() + ".xml";

                    FileStream fs = new FileStream(fname, FileMode.Create);
                    XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                    XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                    serializer.Serialize(writer, eConnect);
                    writer.Close();



                    #endregion

                    #region  Send xml file to GPs

                    eConnectMethods eConCall = new eConnectMethods();

                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.Load(fname);
                    TransferDocument = xmldoc.OuterXml;
                    eConCall.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, TransferDocument, EnumTypes.SchemaValidationType.None, "");

                    #endregion

                    InCubeQuery UpdateQuery = new InCubeQuery(db_vms, "Update WarehouseTransaction SET Synchronized = 1 where TransactionID = '" + TransferID + "'");
                    err = UpdateQuery.Execute();
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

        public override void Close()
        {

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

            InCubeQuery OrganizationQuery = new InCubeQuery(db_ERP, SelectOrganization);
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

            InCubeQuery CustomerQuery = new InCubeQuery(db_ERP, @"SELECT     ADDRESS1, ADDRESS2, ADDRESS3
FROM         RM00101
WHERE     CUSTNMBR='" + CustomerCode + "'");
            err = CustomerQuery.Execute();
            err = CustomerQuery.FindFirst();
            if (err == InCubeErrors.Success)
            {
                err = CustomerQuery.GetField(0, ref Address1);
                err = CustomerQuery.GetField(1, ref Address2);
                err = CustomerQuery.GetField(2, ref Address3);
                //err = CustomerQuery.FindNext();
            }

        }
        private string GetPaytrmID(string CustomerCode)
        {
            object FIELD = "";
            InCubeQuery GetPaytrmID = new InCubeQuery(db_ERP, @"SELECT  PYMTRMID from RM00101 WHERE CUSTNMBR='" + CustomerCode + "'   ");
            err = GetPaytrmID.Execute();
            err = GetPaytrmID.FindFirst();
            while (err == InCubeErrors.Success)
            {
                err = GetPaytrmID.GetField(0, ref FIELD);
                err = GetPaytrmID.FindNext();
            }
            return FIELD.ToString();
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

        private InCubeErrors UpdateCustomerInfoFromGP(DateTime FromDate, DateTime ToDate)
        {
            try
            {
                object CustomerCode = null;
                object field = null;

                #region Create AINCustomerInfoGP Table
                string InfoTable = @"if not exists (select name from sys.objects where type = 'U' and name = 'aincustomerinfogp')
begin

CREATE TABLE [dbo].[AINCustomerInfoGP](
    [CustomerCode] [nvarchar](50) NOT NULL,
    [InfoDate] [datetime] NOT NULL,
    [SHIPMTHD] [nvarchar](50) NULL,
    [CITY] [nvarchar](50) NULL,
    [STATE] [nvarchar](50) NULL,
    [ZIPCODE] [nvarchar](50) NULL,
    [COUNTRY] [nvarchar](50) NULL,
    [CNTCPRSN] [nvarchar](50) NULL,
    [PHNUMBR1] [nvarchar](50) NULL,
    [PHNUMBR2] [nvarchar](50) NULL,
    [PHNUMBR3] [nvarchar](50) NULL,
    [FAXNUMBR] [nvarchar](50) NULL,
    [SLPRSNID] [nvarchar](50) NULL,
    [TAXSCHID] [nvarchar](50) NULL,
 CONSTRAINT [PK_AINCustomerInfoGP] PRIMARY KEY CLUSTERED 
(
    [CustomerCode] ASC,
    [InfoDate] ASC
)
) ON [PRIMARY]
end

Delete from AINCustomerInfoGP where CONVERT(varchar,InfoDate,102) < CONVERT(varchar,GETDATE(),102)";

                InCubeQuery InfoQry = new InCubeQuery(db_vms, InfoTable);
                err = InfoQry.Execute();

                #endregion

                if (err == InCubeErrors.Success)
                {

                    string SelectCustomers = @"SELECT Distinct  

            CustomerOutlet.Barcode AS CustomerCode

            FROM [Transaction] INNER JOIN
            CustomerOutlet ON [Transaction].CustomerID = CustomerOutlet.CustomerID AND [Transaction].OutletID = CustomerOutlet.OutletID 

            WHERE ([Transaction].Synchronized = 0) AND ([Transaction].TransactionTypeID in (1,2,3,4)) AND 
            ([Transaction].TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
            AND [Transaction].TransactionDate <= '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"') AND
            CustomerOutlet.Barcode Not In (Select CustomerCode From AINCustomerInfoGP)";

                    InCubeQuery _query = new InCubeQuery(db_vms, SelectCustomers);
                    _query.Execute();

                    ClearProgress();
                    SetProgressMax(_query.GetDataTable().Rows.Count);

                    err = _query.FindFirst();

                    while (err == InCubeErrors.Success)
                    {
                        ReportProgress("Updating Customer Info");

                        err = _query.GetField(0, ref CustomerCode);

                        string QueryString = @"
Select Top (1) SHIPMTHD,'','','','' ,'' ,'' ,'' ,'' ,'' ,'' ,'' from RM00101
Where SHIPMTHD <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top (1) '', CITY,'','','' ,'' ,'' ,'' ,'' ,'' ,'' ,'' from RM00101
Where CITY <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top (1) '','', STATE,'','' ,'' ,'' ,'' ,'' ,'' ,'' ,'' from RM00101
Where STATE <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top (1) '','','', ZIP, ''  ,'' ,'' ,'' ,'' ,'' ,'' ,'' from RM00101
Where ZIP <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top (1) '','','','', COUNTRY , '','' ,'' ,'' ,'' ,'' ,'' from RM00101
Where COUNTRY <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top (1) '','','','','' , CNTCPRSN,'' ,'' ,'' ,'' ,'' ,'' from RM00101
Where CNTCPRSN <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top (1) '','','','','' ,'' , PHONE1 ,'' ,'' ,'' ,'' ,'' from RM00101
Where PHONE1 <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top (1) '','','','','' ,'' ,'' , PHONE2,'' ,'' ,'' ,'' from RM00101
Where PHONE2 <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top (1) '','','','','' ,'' ,'' ,'' , PHONE3,'' ,'' ,'' from RM00101
Where PHONE3 <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top (1) '','','','','' ,'' ,'' ,'' ,'' , FAX,'' ,'' from RM00101
Where FAX <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top (1) '','','','','' ,'' ,'' ,'' ,'' ,'' , SLPRSNID ,'' from RM00101
Where (SLPRSNID = 'CREDIT' or SLPRSNID = 'CASH') AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top (1) '','','','','' ,'' ,'' ,'' ,'' ,'' ,'' , TAXSCHID from RM00101
Where TAXSCHID <> '' AND CUSTNMBR = '" + CustomerCode + @"'";

                        InCubeQuery AdditionalInfo = new InCubeQuery(db_ERP, QueryString);
                        err = AdditionalInfo.Execute();
                        err = AdditionalInfo.FindFirst();

                        if (err == InCubeErrors.Success)
                        {
                            AdditionalInfo.GetField(0, ref field);
                            string SHIPMTHD = field.ToString();

                            AdditionalInfo.GetField(1, ref field);
                            string CITY = field.ToString();

                            AdditionalInfo.GetField(2, ref field);
                            string STATE = field.ToString();

                            AdditionalInfo.GetField(3, ref field);
                            string ZIPCODE = field.ToString();

                            AdditionalInfo.GetField(4, ref field);
                            string COUNTRY = field.ToString();

                            AdditionalInfo.GetField(5, ref field);
                            string CNTCPRSN = field.ToString();

                            AdditionalInfo.GetField(6, ref field);
                            string PHNUMBR1 = field.ToString();

                            AdditionalInfo.GetField(7, ref field);
                            string PHNUMBR2 = field.ToString();

                            AdditionalInfo.GetField(8, ref field);
                            string PHNUMBR3 = field.ToString();

                            AdditionalInfo.GetField(9, ref field);
                            string FAXNUMBR = field.ToString();

                            AdditionalInfo.GetField(10, ref field);
                            string SLPRSNID = field.ToString();

                            AdditionalInfo.GetField(11, ref field);
                            string TAXSCHID = field.ToString();

                            QueryString = @" Insert Into AINCustomerInfoGP (CustomerCode, InfoDate, SHIPMTHD, CITY, STATE, ZIPCODE, COUNTRY, CNTCPRSN, PHNUMBR1, PHNUMBR2, PHNUMBR3, FAXNUMBR, SLPRSNID, TAXSCHID)
Values ('" + CustomerCode + "',GETDATE(),'" + SHIPMTHD + "','" + CITY + "','" + STATE + "','" + ZIPCODE + "','" + COUNTRY + "','" + CNTCPRSN + "','" + PHNUMBR1 + "','" + PHNUMBR2 + "','" + PHNUMBR3 + "','" + FAXNUMBR + "','" + SLPRSNID + "','" + TAXSCHID + "')";

                            InCubeQuery CMD = new InCubeQuery(db_vms, QueryString);
                            CMD.ExecuteNonQuery();
                            CMD.Close();

                        }

                        AdditionalInfo.Close();

                        err = _query.FindNext();
                    }

                    _query.Close();

                }
                else
                {
                    StreamWriter wrt = new StreamWriter("errorInv.log", true);
                    wrt.Write(InfoQry.GetCurrentException());
                    wrt.Close();
                    return InCubeErrors.Error;
                }

                WriteMessage("Done");

                return InCubeErrors.Success;

            }
            catch (Exception ex)
            {
                StreamWriter wrt = new StreamWriter("errorInv.log", true);
                wrt.Write(ex.ToString());
                wrt.Close();
                return InCubeErrors.Error;
            }
        }
    }
}