using InCubeIntegration_DAL;
using Microsoft.Dynamics.GP.eConnect;
using Microsoft.Dynamics.GP.eConnect.Serialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using InCubeLibrary;

namespace InCubeIntegration_BL
{
    class IntegrationJeema : IntegrationBase
    {
        QueryBuilder QueryBuilderObject = new QueryBuilder();

        InCubeErrors err;
        SqlConnection db_ERP_con;
        string sConnectionString = "";
        private long UserID;
        string DateFormat = "dd/MMM/yyyy";
        InCubeQuery incubeQry = null;
        public IntegrationJeema(long CurrentUserID, ExecutionManager ExecManager)
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
            //BillToShipTo();

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

            string SelectItems = @"SELECT 
                                           ITMCLSCD,ITEMNMBR, ITEMDESC,
                                           CREATDDT, MODIFDT,USCATVLS_5,
                                           USCATVLS_4,ITMSHNAM ,DEX_ROW_ID,
                                           ITMGEDSC
                                           
                                           FROM IV00101 I";

            //You can filter your query by IV00101.ITMGEDSC=’3AADF’.  All dairy items have a field value of ‘3AADF’ and poultry products have ‘3AAPF’.  Also,  IV00101.ITEMTYPE<>2  (discontinued)  should be added in the filter so that discontinued items will not be included in the query result.
            SelectItems += " WHERE I.ITEMTYPE<>2 and ITEMNMBR like 'FG%' ";

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

            QueryBuilderObject.SetField("InActive", "1");
            QueryBuilderObject.UpdateQueryString("Item", db_vms);

            InCubeQuery ItemQuery = new InCubeQuery(db_ERP, SelectItems);
            err = ItemQuery.Execute();

            ClearProgress();
            SetProgressMax(ItemQuery.GetDataTable().Rows.Count);
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

                ItemQuery.GetField("ITEMNMBR", ref field);
                string ItemCode = field.ToString().Trim();

                #region Get Barcode

                string realBarcode = GetFieldValue("IV00110", "PLANNERNAME", "PLANNERID = '" + ItemCode + "'", db_ERP);
                //string realBarcode = ItemCode;
                #endregion

                //ItemQuery.GetField("DEX_ROW_ID", ref field);
                string ItemID = string.Empty;

                ItemQuery.GetField("ITEMDESC", ref field);
                string Itemdesc = field.ToString().Trim();

                ItemQuery.GetField("USCATVLS_5", ref field);
                string Brand = field.ToString().Trim();

                ItemQuery.GetField("USCATVLS_4", ref field);
                string Orgin = field.ToString().Trim();

                ItemQuery.GetField("ITMSHNAM", ref field);
                string PackDefinition = field.ToString().Trim();


                #region Item
                //err = ExistObject("Item", "ItemID", "itemcode = '" + ItemCode+"'", db_vms);
                ItemID = GetFieldValue("Item", "ItemID", "itemcode = '" + ItemCode + "'", db_vms).Trim();
                if (!ItemID.Equals(string.Empty)) // Exist Item --- Update Query
                {
                    TOTALUPDATED++;

                    QueryBuilderObject.SetField("ItemCode", "'" + ItemCode + "'");
                    QueryBuilderObject.SetField("PackDefinition", "'" + PackDefinition + "'");

                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    QueryBuilderObject.SetField("InActive", "0");

                    QueryBuilderObject.UpdateQueryString("Item", " ItemID = " + ItemID, db_vms);

                    QueryBuilderObject.SetField("Description", "'" + Itemdesc + "'");
                    QueryBuilderObject.UpdateQueryString("ItemLanguage", " ItemID =" + ItemID + " AND LanguageID = 1", db_vms);
                }
                else  // New Item --- Insert Query
                {
                    TOTALINSERTED++;
                    ItemID = GetFieldValue("item", "ISNULL(MAX(itemid),0) + 1", db_vms).Trim();
                    QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                    QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());
                    QueryBuilderObject.SetField("ItemCode", "'" + ItemCode + "'");
                    QueryBuilderObject.SetField("PackDefinition", "'" + PackDefinition + "'");
                    QueryBuilderObject.SetField("InActive", "0");

                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    err = QueryBuilderObject.InsertQueryString("Item", db_vms);

                    QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + Itemdesc + "'");
                    QueryBuilderObject.InsertQueryString("ItemLanguage", db_vms);
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

                        QueryBuilderObject.SetField("Barcode", "'" + realBarcode + "'");
                        QueryBuilderObject.SetField("Quantity", UOM.ToString());
                        err = QueryBuilderObject.UpdateQueryString("Pack", "PackID = " + PackID, db_vms);

                    }
                    else
                    {
                        PackID = int.Parse(GetFieldValue("Pack", "ISNULL(MAX(PackID),0) + 1", db_vms));

                        QueryBuilderObject.SetField("PackID", PackID.ToString());
                        QueryBuilderObject.SetField("Barcode", "'" + realBarcode + "'");
                        QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                        QueryBuilderObject.SetField("PackTypeID", PacktypeID.ToString());
                        QueryBuilderObject.SetField("Quantity", UOM.ToString());

                        QueryBuilderObject.InsertQueryString("Pack", db_vms);

                    }
                    err = ItemPriceQuery.FindNext();
                }

                #endregion

                err = ItemQuery.FindNext();
            }

            ItemQuery.Close();
            WriteMessage("\r\n");
            WriteMessage("<<< ITEMS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        #endregion

        #region UpdateCustomer

        public override void UpdateCustomer()
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

            //            string SelectCustomer = @"SELECT  
            //                                              A.CRLMTAMT,A.CUSTNAME, A.ADDRESS1, 
            //                                              A.PHONE1, A.FAX, A.CUSTNMBR, A.CUSTCLAS as PRCLEVEL,A.DEX_ROW_ID,
            //                                              A.PYMTRMID,A.CREATDDT, A.MODIFDT,A.INACTIVE, A.HOLD ,A.SLPRSNID,A.SALSTERR
            //
            //FROM RM00101 AS A INNER JOIN RM00303 AS B ON A.SALSTERR=B.SALSTERR    
            //WHERE A.SALSTERR <> '' and  A.INACTIVE=0 and CUSTCLAS not in ('DOUBTFULL','') and A.SALSTERR<>'' and A.SALSTERR<>'JEEMA' ";

            string SelectCustomer = @"SELECT X.CRLMTAMT,X.CUSTNAME, X.ADDRESS1, X.PHONE1, X.FAX, X.CUSTNMBR, X.CUSTCLAS,X.PRCLEVEL,X.DEX_ROW_ID,
X.PYMTRMID,X.CREATDDT, X.MODIFDT,X.INACTIVE,X.HOLD,X.SLPRSNID,X.SALSTERR ,X.BilltoId,Z.CUSTNAME as BilltoName,Z.TAXSCHID as Taxeable,X.TXRGNNUM as TaxNumber
from 
(
SELECT  
A.CRLMTAMT,A.CUSTNAME, A.ADDRESS1,A.PHONE1, A.FAX, A.CUSTNMBR, A.CUSTCLAS, A.PRCLEVEL,A.DEX_ROW_ID,A.TXRGNNUM,
A.PYMTRMID,A.CREATDDT, A.MODIFDT,A.INACTIVE, A.HOLD ,A.SLPRSNID,A.SALSTERR,CASE WHEN A.CPRCSTNM = '' THEN A.CUSTNMBR WHEN ISNULL(A.CPRCSTNM,'')='' THEN A.CUSTNMBR ELSE A.CPRCSTNM END  BilltoId
FROM RM00101 AS A LEFT OUTER JOIN RM00303 AS B ON A.SALSTERR=B.SALSTERR   
WHERE  A.CUSTCLAS not in ('DOUBTFULL-DEBTS','') and A.SALSTERR<>'' and A.SALSTERR<>'JEEMA'
) X Inner JOIN RM00101 Z ON X.BillToId = z.CUSTNMBR
";

            InCubeQuery CustomerQuery = new InCubeQuery(db_ERP, SelectCustomer);
            CustomerQuery.Execute();

            ClearProgress();
            SetProgressMax(CustomerQuery.GetDataTable().Rows.Count);

            err = CustomerQuery.FindFirst();
            WriteMessage("\r\n");
            WriteMessage("Customers Received, Start updating");

            while (err == InCubeErrors.Success)
            {
                ReportProgress("Updating Customers");

                //CustomerQuery.GetField("DEX_ROW_ID", ref field);
                //int CustomerID = int.Parse(field.ToString());

                CustomerQuery.GetField("PHONE1", ref field);
                string Phone = field.ToString().Trim();

                CustomerQuery.GetField("BilltoId", ref field);
                string ParentCode = field.ToString().Trim();

                CustomerQuery.GetField("BilltoName", ref field);
                string ParentName = field.ToString().Trim();


                CustomerQuery.GetField("FAX", ref field);
                string Fax = field.ToString().Trim();

                CustomerQuery.GetField("HOLD", ref field);
                int OnHold = int.Parse(field.ToString());

                CustomerQuery.GetField("INACTIVE", ref field);
                int InActive = int.Parse(field.ToString());

                CustomerQuery.GetField("CUSTNMBR", ref field);
                string CustomerBarcode = field.ToString().Trim();

                CustomerQuery.GetField("CUSTNAME", ref field);
                string CustomerName = field.ToString().Trim();

                CustomerQuery.GetField("ADDRESS1", ref field);
                string CustomerAddress = field.ToString().Trim();

                CustomerQuery.GetField("PRCLEVEL", ref field);
                string CustomerPRCLEVEL = field.ToString().Trim();

                CustomerQuery.GetField("PYMTRMID", ref field);
                string PYMTRMID = field.ToString().Trim();

                CustomerQuery.GetField("SALSTERR", ref field);
                string RouteAdd = field.ToString().Trim();

                CustomerQuery.GetField("SLPRSNID", ref field);
                string CustomerType = field.ToString().ToLower().Trim();

                CustomerQuery.GetField("Taxeable", ref field);
                string Taxeable = field.ToString().ToLower().Trim();


                CustomerQuery.GetField("TaxNumber", ref field);
                string TaxNumber = field.ToString().ToLower().Trim();
                int paymenttermid = -1;
                int customertypeid = 1;

                AddUpdatePaymentTerm(PYMTRMID,ref customertypeid, ref paymenttermid);
                //End of Comment

                //CustomerPYMTRMID = CustomerPYMTRMID.ToUpper().Replace("DAYS", "").Trim();
                ////CustomerPYMTRMID = CustomerPYMTRMID.ToUpper().Replace("CASH", "0").Trim();
                //CustomerPYMTRMID = CustomerPYMTRMID.ToUpper().Replace("NET", "").Trim();
                //CustomerPYMTRMID = CustomerPYMTRMID.ToUpper().Replace("C.O.D.", "0").Trim();
                //CustomerPYMTRMID = CustomerPYMTRMID.ToUpper().Replace("C.O.D", "0").Trim();
                //CustomerPYMTRMID = CustomerPYMTRMID.ToUpper().Replace("Credit", "").Trim();
                //CustomerPYMTRMID = CustomerPYMTRMID.ToUpper().Replace("days", "").Trim();
                ////CustomerPYMTRMID = CustomerPYMTRMID.ToUpper().Replace("NET", "").Trim();
                //int Credit;
                //if (CustomerPYMTRMID.ToString().Trim().ToLower().Equals("cash"))
                //{
                //    Credit = 1;
                //}
                //else
                //{
                //    Credit = 2;
                //}

                //string PaymentTermID = "1";
                //if (CustomerBarcode == "10864")
                //{

                //}


                //if (CustomerPYMTRMID != "0" && !CustomerPYMTRMID.ToString().Trim().Equals(string.Empty) && !CustomerPYMTRMID.ToString().Trim().Equals("CASH IN ADVANCE      "))
                //{
                //    //Credit = 2;
                //    decimal PaymentTermsValue = 30;

                //    if (!decimal.TryParse(CustomerPYMTRMID, out PaymentTermsValue))
                //    {
                //        CustomerPYMTRMID = PaymentTermsValue.ToString();
                //    }

                //    err = ExistObject("PaymentTerm", "PaymentTermID", "SimplePeriodWidth = " + CustomerPYMTRMID, db_vms);
                //    if (err != InCubeErrors.Success)
                //    {
                //        PaymentTermID = GetFieldValue("PaymentTerm", "isnull(MAX(PaymentTermID),0) + 1", db_vms);

                //        QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                //        QueryBuilderObject.SetField("PaymentTermTypeID", "1");
                //        QueryBuilderObject.SetField("SimplePeriodWidth", CustomerPYMTRMID);
                //        QueryBuilderObject.SetField("SimplePeriodID", "1"); //Days
                //        QueryBuilderObject.InsertQueryString("PaymentTerm", db_vms);

                //        QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                //        QueryBuilderObject.SetField("LanguageID", "1");
                //        QueryBuilderObject.SetField("Description", "'Every " + CustomerPYMTRMID + " Days'");
                //        QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms);
                //    }
                //}
                //WriteMessage("Update Payments");
                CustomerQuery.GetField("CRLMTAMT", ref field);
                string creditlimitmain = GetFieldValue("RM00101", "CRLMTAMT", "CUSTNMBR ='" + ParentCode + "'", db_ERP).Trim();
                decimal CustomerCREATDDT = 0;
                if (!creditlimitmain.Equals(string.Empty))
                    CustomerCREATDDT = decimal.Parse(creditlimitmain);

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
                    err = QueryBuilderObject.InsertQueryString("CustomerGroup", db_vms);

                    QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + CustomerPRCLEVEL + "'");
                    err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);

                }
                INVANCustomerGroupQuery.Close();
                //WriteMessage("Update customer Group");
                #endregion

                #region Customer

                string CustomerID = GetFieldValue("Customer", "CustomerID", "CustomerCode='" + ParentCode + "'", db_vms).Trim();

                //err = ExistObject("Customer", "CustomerID", "CustomerID = " + CustomerID, db_vms);
                if (!CustomerID.Equals(string.Empty)) // Exist Customer --- Update Query
                {
                    TOTALUPDATED++;

                    QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                    QueryBuilderObject.SetField("Fax", "'" + Fax + "'");
                    QueryBuilderObject.SetField("OnHold", "0");
                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "GETDATE()");

                    err = QueryBuilderObject.UpdateQueryString("Customer", " CustomerID = " + CustomerID, db_vms);

                    QueryBuilderObject.SetField("Description", "'" + ParentName + "'");
                    QueryBuilderObject.SetField("Address", "'" + ParentName + "'");
                    err = QueryBuilderObject.UpdateQueryString("CustomerLanguage", "  CustomerID = " + CustomerID + " AND LanguageID = 1", db_vms);
                }
                else // New Customer --- Insert Query
                {

                    TOTALINSERTED++;
                    CustomerID = GetFieldValue("Customer", "isnull(MAX(CustomerID),0) + 1", db_vms);
                    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                    QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                    QueryBuilderObject.SetField("Fax", "'" + Fax + "'");
                    QueryBuilderObject.SetField("Email", "' '");
                    QueryBuilderObject.SetField("CustomerCode", "'" + ParentCode + "'");
                    QueryBuilderObject.SetField("OnHold", "0");
                    QueryBuilderObject.SetField("StreetID", "0");
                    QueryBuilderObject.SetStringField("StreetAddress", RouteAdd);
                    QueryBuilderObject.SetField("InActive", "0");
                    QueryBuilderObject.SetField("New", "0");

                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "GETDATE()");

                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "GETDATE()");
                    err = QueryBuilderObject.InsertQueryString("Customer", db_vms);

                    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + ParentName + "'");
                    QueryBuilderObject.SetField("Address", "'" + ParentName + "'");
                    QueryBuilderObject.InsertQueryString("CustomerLanguage", db_vms);
                }
                //WriteMessage("update customer language");
                #region Customer Account 
                string BalanceStr = GetFieldValue("RM00103", "CUSTBLNC", "CUSTNMBR ='" + ParentCode + "'", db_ERP);
                int ParentAccountID = 1;
                decimal Balance = 0;
                if (BalanceStr != string.Empty)
                {
                    Balance = decimal.Parse(BalanceStr);
                    if (Balance > 99999999) Balance = 99999999;
                }
                err = ExistObject("AccountCust", "AccountID", "CustomerID = " + CustomerID + "", db_vms);

                if (err == InCubeErrors.Success)
                {
                    ParentAccountID = int.Parse(GetFieldValue("AccountCust", "AccountID", "CustomerID = " + CustomerID + "", db_vms));

                    QueryBuilderObject.SetField("CreditLimit", CustomerCREATDDT.ToString());
                    QueryBuilderObject.SetField("Balance", Balance.ToString());
                    err = QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + ParentAccountID.ToString(), db_vms);

                }
                else
                {
                    ParentAccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));

                    QueryBuilderObject.SetField("AccountID", ParentAccountID.ToString());
                    QueryBuilderObject.SetField("AccountTypeID", "1");
                    QueryBuilderObject.SetField("CreditLimit", "99999");// CustomerCREATDDT.ToString());
                    QueryBuilderObject.SetField("Balance", Balance.ToString());
                    QueryBuilderObject.SetField("GL", "0");
                    QueryBuilderObject.SetField("OrganizationID", "1");
                    QueryBuilderObject.SetField("CurrencyID", "1");
                    QueryBuilderObject.InsertQueryString("Account", db_vms);

                    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                    //QueryBuilderObject.SetField("OutletID", OutletID);
                    QueryBuilderObject.SetField("AccountID", ParentAccountID.ToString());
                    QueryBuilderObject.InsertQueryString("AccountCust", db_vms);

                    QueryBuilderObject.SetField("AccountID", ParentAccountID.ToString());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + CustomerName.Trim() + " Account'");
                    err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
                }
                //WriteMessage("update customer account");
                #endregion

                #endregion

                #region CustomerLanguage

                #endregion

                #region Customer Outlet and language

                string CustomerOutletCode= CustomerBarcode;
                string SelectCustomerOutlet = "SELECT ltrim(rtrim(cast(CUSTNMBR as nvarchar(50)))) as ADDRESSSCODE FROM RM00102 Where CUSTNMBR = '" + CustomerBarcode + "'";

                InCubeQuery CustomerOutletQuery = new InCubeQuery(db_ERP, SelectCustomerOutlet);
                CustomerOutletQuery.Execute();
                err = CustomerOutletQuery.FindFirst();
                while (err == InCubeErrors.Success)
                {
                    CustomerOutletQuery.GetField("ADDRESSSCODE", ref field);
                    //CustomerOutletCode = field.ToString();

                    string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerID = " + CustomerID + " AND CustomerCode = '" + CustomerOutletCode + "'", db_vms);
                    if (!OutletID.Trim().Equals(string.Empty))
                    {

                        QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                        QueryBuilderObject.SetField("Fax", "'" + Fax + "'");
                        QueryBuilderObject.SetField("StreetAddress", RouteAdd);
                        //QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                        QueryBuilderObject.SetField("CustomerTypeID", customertypeid.ToString()); //HardCoded -1- Cash -2- Credit
                        QueryBuilderObject.SetField("OnHold", OnHold.ToString());
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "GETDATE()");
                        QueryBuilderObject.SetField("PaymentTermID", paymenttermid.ToString());
                        QueryBuilderObject.SetField("InActive", InActive.ToString());

                        if (Taxeable.Equals(string.Empty) || Taxeable == "")
                        {
                            QueryBuilderObject.SetField("Taxeable", "0");
                        }
                        else
                        {
                            QueryBuilderObject.SetField("Taxeable", "1");

                        }
                        QueryBuilderObject.SetField("TaxNumber", "'" + TaxNumber + "'");
                        err = QueryBuilderObject.UpdateQueryString("CustomerOutlet", "  CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
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
                        QueryBuilderObject.SetField("Email", "' '");
                        //QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                        if (Taxeable.Equals(string.Empty) || Taxeable == "")
                        {
                            QueryBuilderObject.SetField("Taxeable", "0");
                        }
                        else
                        {
                            QueryBuilderObject.SetField("Taxeable", "1");

                        }
                        QueryBuilderObject.SetField("TaxNumber", "'" + TaxNumber + "'");
                        QueryBuilderObject.SetField("CustomerTypeID", customertypeid.ToString()); //HardCoded -1- Cash -2- Credit

                        QueryBuilderObject.SetField("CurrencyID", "1");
                        QueryBuilderObject.SetField("OnHold", OnHold.ToString());
                        QueryBuilderObject.SetField("GPSLatitude", "0");
                        QueryBuilderObject.SetField("GPSLongitude", "0");
                        QueryBuilderObject.SetField("StreetID", "0");
                        QueryBuilderObject.SetField("StreetAddress", RouteAdd);
                        QueryBuilderObject.SetField("InActive", InActive.ToString());
                        QueryBuilderObject.SetField("Notes", "0");
                        QueryBuilderObject.SetField("SkipCreditCheck", "0");
                        QueryBuilderObject.SetField("PaymentTermID", paymenttermid.ToString());

                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("CreatedDate", "GETDATE()");

                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "GETDATE()");
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
                        QueryBuilderObject.InsertQueryString("CustomerOutletGroup", db_vms);
                    }
                    err = ExistObject("CustomerOutletLanguage", "OutletID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                    if (err == InCubeErrors.Success)
                    {
                        QueryBuilderObject.SetField("Description", "'" + CustomerName + "'");
                        QueryBuilderObject.SetField("Address", "'" + CustomerAddress + "'");
                        QueryBuilderObject.UpdateQueryString("CustomerOutletLanguage", "  CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " AND LanguageID = 1", db_vms);
                    }
                    else
                    {
                        QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                        QueryBuilderObject.SetField("OutletID", OutletID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + CustomerName + "'");
                        QueryBuilderObject.SetField("Address", "'" + CustomerAddress + "'");
                        QueryBuilderObject.InsertQueryString("CustomerOutletLanguage", db_vms);
                    }

                    BalanceStr = GetFieldValue("RM00103", "CUSTBLNC", "CUSTNMBR ='" + CustomerBarcode + "'", db_ERP);
                    int AccountID = 1;
                    Balance = 0;
                    if (BalanceStr != string.Empty)
                    {
                        Balance = decimal.Parse(BalanceStr);
                        if (Balance > 99999999) Balance = 99999999;
                    }

                    err = ExistObject("AccountCustOut", "AccountID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                    string CustOutCredit = GetFieldValue("RM00101", "CRLMTAMT", "CUSTNMBR ='" + CustomerBarcode + "'", db_ERP).Trim();
                    if (CustOutCredit != string.Empty)
                    {
                        CustomerCREATDDT = decimal.Parse(CustOutCredit);
                    }
                    else
                    {
                        CustomerCREATDDT = 0;
                    }

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
                        QueryBuilderObject.SetField("CreditLimit", "99999");// CustomerCREATDDT.ToString());
                        QueryBuilderObject.SetField("Balance", Balance.ToString());
                        QueryBuilderObject.SetField("GL", "0");
                        QueryBuilderObject.SetField("OrganizationID", "1");
                        QueryBuilderObject.SetField("CurrencyID", "1");
                        QueryBuilderObject.SetField("ParentAccountID", ParentAccountID.ToString());
                        QueryBuilderObject.InsertQueryString("Account", db_vms);

                        QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                        QueryBuilderObject.SetField("OutletID", OutletID);
                        QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                        QueryBuilderObject.InsertQueryString("AccountCustOut", db_vms);

                        QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + CustomerName.Trim() + " Account'");
                        err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
                    }

                    err = CustomerOutletQuery.FindNext();
                }

                CustomerOutletQuery.Close();

                #endregion

                err = CustomerQuery.FindNext();
            }
            CustomerQuery.Close();
            WriteMessage("\r\n");
            WriteMessage("<<< CUSTOMERS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        private void AddUpdatePaymentTerm(string PYMTRMID, ref int customertypeid, ref int paymenttermidnew)
        {
            InCubeErrors err;
            object field = new object();

            string SelectPaymentTerm = @"SELECT S.PYMTRMID,S.DISCTYPE,S.DISCDTDS,S.DUEDTDS,S.USEGRPER from SY03300 S
Where S.PYMTRMID = '" + PYMTRMID + "'";

            InCubeQuery PaymentTermQuery = new InCubeQuery(db_ERP, SelectPaymentTerm);
            PaymentTermQuery.Execute();
            err = PaymentTermQuery.FindFirst();


            PaymentTermQuery.GetField("PYMTRMID", ref field);
            string PaymentTermType = field.ToString().Trim();

            PaymentTermQuery.GetField("DISCDTDS", ref field);
            int DISCDTDS = Convert.ToInt32(field);

            PaymentTermQuery.GetField("DUEDTDS", ref field);
            int DUEDTDS = Convert.ToInt32(field);


            if (DUEDTDS == 0)
            {
                if (DISCDTDS == 0)
                {
                    customertypeid = 1;
                    paymenttermidnew = 0;
                }
                else
                {
                    //int PaymentTermID;
                    customertypeid = 2;
                    paymenttermidnew = 0;
                    err = ExistObject("PaymentTermLanguage", "PaymentTermID", "Description = '" + PYMTRMID + "'", db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        paymenttermidnew = int.Parse(GetFieldValue("PaymentTerm", "isnull(MAX(PaymentTermID),0) + 1", db_vms));

                        QueryBuilderObject.SetField("PaymentTermID", paymenttermidnew.ToString().Trim());
                        QueryBuilderObject.SetField("PaymentTermTypeID", "1");
                        //QueryBuilderObject.SetField("SimplePeriodWidth", DISCDTDS.ToString().Trim());
                        QueryBuilderObject.SetField("SimplePeriodID", "1"); //Days
                        QueryBuilderObject.InsertQueryString("PaymentTerm", db_vms);

                        QueryBuilderObject.SetField("PaymentTermID", paymenttermidnew.ToString().Trim());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + PYMTRMID.ToString().Trim() + "'");
                        QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms);
                    }
                    else
                    {
                        paymenttermidnew = int.Parse(GetFieldValue("PaymentTermLanguage", "PaymentTermID", "Description = '" + PYMTRMID + "'", db_vms));

                        QueryBuilderObject.SetField("PaymentTermID", paymenttermidnew.ToString().Trim());
                        QueryBuilderObject.SetField("PaymentTermTypeID", "1");
                        //QueryBuilderObject.SetField("SimplePeriodWidth", DISCDTDS.ToString().Trim());
                        QueryBuilderObject.SetField("SimplePeriodID", "1"); //Days
                        err = QueryBuilderObject.UpdateQueryString("PaymentTerm", "  PaymentTermID = " + paymenttermidnew, db_vms);

                        QueryBuilderObject.SetField("PaymentTermID", paymenttermidnew.ToString().Trim());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + PYMTRMID.ToString().Trim() + "'");
                        err = QueryBuilderObject.UpdateQueryString("PaymentTermLanguage", "  PaymentTermID = " + paymenttermidnew + " AND LanguageID = 1", db_vms);
                    }
                }
            }
            if (DUEDTDS > 0 && DISCDTDS == 0)
            {
                //int PaymentTermID;
                customertypeid = 2;
                paymenttermidnew = 0;
                err = ExistObject("PaymentTermLanguage", "PaymentTermID", "Description = '" + PYMTRMID + "'", db_vms);
                if (err != InCubeErrors.Success)
                {
                    paymenttermidnew = int.Parse(GetFieldValue("PaymentTerm", "isnull(MAX(PaymentTermID),0) + 1", db_vms));

                    QueryBuilderObject.SetField("PaymentTermID", paymenttermidnew.ToString().Trim());
                    QueryBuilderObject.SetField("PaymentTermTypeID", "1");
                    if (DUEDTDS < 11 || DUEDTDS == 15)
                    {
                        QueryBuilderObject.SetField("SimplePeriodWidth", DUEDTDS.ToString().Trim());
                    }
                    else
                    {

                        int DaysToAdd = DUEDTDS + 15;
                        QueryBuilderObject.SetField("SimplePeriodWidth", DaysToAdd.ToString().Trim());
                    }
                    QueryBuilderObject.SetField("SimplePeriodID", "1"); //Days
                    QueryBuilderObject.InsertQueryString("PaymentTerm", db_vms);

                    QueryBuilderObject.SetField("PaymentTermID", paymenttermidnew.ToString().Trim());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + PYMTRMID.ToString().Trim() + "'");
                    QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms);
                }
                else
                {
                    paymenttermidnew = int.Parse(GetFieldValue("PaymentTermLanguage", "PaymentTermID", "Description = '" + PYMTRMID + "'", db_vms));

                    QueryBuilderObject.SetField("PaymentTermID", paymenttermidnew.ToString().Trim());
                    QueryBuilderObject.SetField("PaymentTermTypeID", "1");
                    if (DUEDTDS < 11 || DUEDTDS == 15)
                    {
                        QueryBuilderObject.SetField("SimplePeriodWidth", DUEDTDS.ToString().Trim());
                    }
                    else
                    {
                        int DaysToAdd = DUEDTDS + 15;
                        QueryBuilderObject.SetField("SimplePeriodWidth", DaysToAdd.ToString().Trim());
                    }
                    QueryBuilderObject.SetField("SimplePeriodID", "1"); //Days
                    err = QueryBuilderObject.UpdateQueryString("PaymentTerm", "  PaymentTermID = " + paymenttermidnew, db_vms);

                    QueryBuilderObject.SetField("PaymentTermID", paymenttermidnew.ToString().Trim());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + PYMTRMID.ToString().Trim() + "'");
                    err = QueryBuilderObject.UpdateQueryString("PaymentTermLanguage", "  PaymentTermID = " + paymenttermidnew + " AND LanguageID = 1", db_vms);
                }
            }
            if (DUEDTDS > 0 && DISCDTDS == 1)
                {
                    customertypeid = 2;
                    paymenttermidnew = 0;
                    err = ExistObject("PaymentTermLanguage", "PaymentTermID", "Description = '" + PYMTRMID + "'", db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        paymenttermidnew = int.Parse(GetFieldValue("PaymentTerm", "isnull(MAX(PaymentTermID),0) + 1", db_vms));

                        QueryBuilderObject.SetField("PaymentTermID", paymenttermidnew.ToString().Trim());
                        QueryBuilderObject.SetField("PaymentTermTypeID", "2");
                        //QueryBuilderObject.SetField("SimplePeriodWidth", "0");
                        QueryBuilderObject.SetField("SimplePeriodID", "0");
                        //QueryBuilderObject.SetField("ComplexPeriodWidth", "31");
                        QueryBuilderObject.SetField("ComplexPeriodID", "3");
                        if (DUEDTDS < 11)
                        {
                            QueryBuilderObject.SetField("GracePeriod", DUEDTDS.ToString().Trim());
                        }
                        else
                        {
                            int DaysToAdd = DUEDTDS + 15;
                            QueryBuilderObject.SetField("GracePeriod", DaysToAdd.ToString().Trim());
                        }

                        //QueryBuilderObject.SetField("GracePeriod", DUEDTDS.ToString().Trim());
                        QueryBuilderObject.SetField("GracePeriodTypeID", "1");
                        QueryBuilderObject.InsertQueryString("PaymentTerm", db_vms);

                        QueryBuilderObject.SetField("PaymentTermID", paymenttermidnew.ToString().Trim());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + PYMTRMID.ToString().Trim() + "'");
                        QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms);
                    }
                    else
                    {
                        paymenttermidnew = int.Parse(GetFieldValue("PaymentTermLanguage", "PaymentTermID", "Description = '" + PYMTRMID + "'", db_vms));

                        QueryBuilderObject.SetField("PaymentTermID", paymenttermidnew.ToString().Trim());
                        QueryBuilderObject.SetField("PaymentTermTypeID", "2");
                        //QueryBuilderObject.SetField("SimplePeriodWidth", "0");
                        QueryBuilderObject.SetField("SimplePeriodID", "0");
                        //QueryBuilderObject.SetField("ComplexPeriodWidth", "31");
                        QueryBuilderObject.SetField("ComplexPeriodID", "3");
                        if (DUEDTDS < 11)
                        {
                            QueryBuilderObject.SetField("GracePeriod", DUEDTDS.ToString().Trim());
                        }
                        else
                        {
                            int DaysToAdd = DUEDTDS + 15;
                            QueryBuilderObject.SetField("GracePeriod", DaysToAdd.ToString().Trim());
                        }
                        //QueryBuilderObject.SetField("GracePeriod", DUEDTDS.ToString().Trim());
                        QueryBuilderObject.SetField("GracePeriodTypeID", "1");
                        err = QueryBuilderObject.UpdateQueryString("PaymentTerm", "  PaymentTermID = " + paymenttermidnew, db_vms);

                        QueryBuilderObject.SetField("PaymentTermID", paymenttermidnew.ToString().Trim());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + PYMTRMID.ToString().Trim() + "'");
                        err = QueryBuilderObject.UpdateQueryString("PaymentTermLanguage", "  PaymentTermID = " + paymenttermidnew + " AND LanguageID = 1", db_vms);
                    }
                }

                if (PaymentTermType == "3")
                {
                    paymenttermidnew = 0;
                    err = ExistObject("PaymentTermLanguage", "PaymentTermID", "Description = '" + PYMTRMID + "'", db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        paymenttermidnew = int.Parse(GetFieldValue("PaymentTerm", "isnull(MAX(PaymentTermID),0) + 1", db_vms));

                        QueryBuilderObject.SetField("PaymentTermID", paymenttermidnew.ToString().Trim());
                        QueryBuilderObject.SetField("PaymentTermTypeID", "2");
                        //QueryBuilderObject.SetField("SimplePeriodWidth", "0");
                        QueryBuilderObject.SetField("SimplePeriodID", "0");
                        //QueryBuilderObject.SetField("ComplexPeriodWidth", "31");
                        QueryBuilderObject.SetField("ComplexPeriodID", "3");
                        if (DUEDTDS < 11)
                        {
                            QueryBuilderObject.SetField("GracePeriod", DUEDTDS.ToString().Trim());
                        }
                        else
                        {
                            int DaysToAdd = DUEDTDS + 15;
                            QueryBuilderObject.SetField("GracePeriod", DaysToAdd.ToString().Trim());
                        }

                        //QueryBuilderObject.SetField("GracePeriod", "0");
                        QueryBuilderObject.SetField("GracePeriodTypeID", "3");
                        QueryBuilderObject.InsertQueryString("PaymentTerm", db_vms);

                        QueryBuilderObject.SetField("PaymentTermID", paymenttermidnew.ToString().Trim());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", PYMTRMID.ToString().Trim());
                        QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms);
                    }
                    else
                    {
                        paymenttermidnew = int.Parse(GetFieldValue("PaymentTermLanguage", "PaymentTermID", "Description = '" + PYMTRMID + "'", db_vms));

                        QueryBuilderObject.SetField("PaymentTermID", paymenttermidnew.ToString().Trim());
                        QueryBuilderObject.SetField("PaymentTermTypeID", "2");
                        //QueryBuilderObject.SetField("SimplePeriodWidth", "0");
                        QueryBuilderObject.SetField("SimplePeriodID", "0");
                        //QueryBuilderObject.SetField("ComplexPeriodWidth", "31");
                        QueryBuilderObject.SetField("ComplexPeriodID", "3");
                        if (DUEDTDS < 11)
                        {
                            QueryBuilderObject.SetField("GracePeriod", DUEDTDS.ToString().Trim());
                        }
                        else
                        {
                            int DaysToAdd = DUEDTDS + 15;
                            QueryBuilderObject.SetField("GracePeriod", DaysToAdd.ToString().Trim());
                        }
                        //QueryBuilderObject.SetField("GracePeriod", "0");
                        QueryBuilderObject.SetField("GracePeriodTypeID", "3");
                        err = QueryBuilderObject.UpdateQueryString("PaymentTerm", "  PaymentTermID = " + paymenttermidnew, db_vms);

                        QueryBuilderObject.SetField("PaymentTermID", paymenttermidnew.ToString().Trim());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", PYMTRMID.ToString().Trim());
                        err = QueryBuilderObject.UpdateQueryString("PaymentTermLanguage", "  PaymentTermID = " + paymenttermidnew + " AND LanguageID = 1", db_vms);
                    }
                }
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

            string SelectWarehouse = @"
SELECT    
LOCNCODE,
LOCNDSCR,
PHONE1,
IV40700.COUNTRY,
ADDRESS1,
IV40700.DEX_ROW_ID, 
ZIPCODE, 
STATE
FROM IV40700  INNER JOIN rm00303 ON  IV40700.LOCNCODE=rm00303.SALSTERR where  LOCNCODE between '1' and '999' and len(LOCNCODE)=3
";


            InCubeQuery WarehouseQuery = new InCubeQuery(db_ERP, SelectWarehouse);
            err = WarehouseQuery.Execute();
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

                string WarehouseID = GetFieldValue("warehouse", "warehouseID", "warehousecode='" + WarehouseCode + "'", db_vms).Trim();
                if (WarehouseID.Equals(string.Empty))
                {
                    WarehouseID = GetFieldValue("Warehouse", "ISNULL(MAX(warehouseID),0) + 1", db_vms).Trim();
                }

                WarehouseQuery.GetField("ZIPCODE", ref field);
                string VehicleRegNum = field.ToString().Trim();

                WarehouseQuery.GetField("STATE", ref field);
                string Depot = field.ToString().Trim();

                OrganizationID = GetFieldValue("OrganizationLanguage", "OrganizationID", "Description = 'Default Organization'", db_vms);
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
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.SetField("WarehouseTypeID", "2");
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                err = QueryBuilderObject.InsertQueryString("Warehouse", db_vms);

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

            string SalesPersonID = GetFieldValue("Employee", "EmployeeID", " EmployeeCode = '" + SalesmanCode + "'", db_vms);
            string VehicleID = GetFieldValue("Vehicle", "VehicleID", "VehicleID=" + WarehouseID, db_vms);

            if (!SalesPersonID.Trim().Equals(string.Empty) && !VehicleID.Trim().Equals(string.Empty))
            {
                err = ExistObject("EmployeeVehicle", "VehicleID", "VehicleID = " + WarehouseID + " AND EmployeeID = " + SalesPersonID, db_vms);
                if (err != InCubeErrors.Success)
                {
                    QueryBuilderObject.SetField("VehicleID", VehicleID);
                    QueryBuilderObject.SetField("EmployeeID", SalesPersonID);
                    QueryBuilderObject.InsertQueryString("EmployeeVehicle", db_vms);
                }
            }

#if MGC
            err = ExistObject("VehicleLoadingWarehouse", "VehicleID", "WarehouseID = " + WarehouseID + " AND VehicleID = " + VehicleID, db_vms);
            if (err != InCubeErrors.Success)
            {
                QueryBuilderObject.SetField("WarehouseID", "1");
                QueryBuilderObject.SetField("VehicleID", VehicleID);
                QueryBuilderObject.InsertQueryString("VehicleLoadingWarehouse", db_vms);
            }

#endif

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
WHERE LOCNCODE in ('HAT-STORE'   ,'DIB-STORE'  ,'AVE-STORE')
";


            InCubeQuery WarehouseQuery = new InCubeQuery(db_ERP, SelectWarehouse);
            err = WarehouseQuery.Execute();
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

                string WarehouseID = GetFieldValue("warehouse", "warehouseID", "warehousecode='" + WarehouseCode + "'", db_vms).Trim();
                if (WarehouseID.Equals(string.Empty))
                {
                    WarehouseID = GetFieldValue("Warehouse", "ISNULL(MAX(warehouseID),0) + 1", db_vms).Trim();
                }

                WarehouseQuery.GetField("ZIPCODE", ref field);
                string VehicleRegNum = field.ToString().Trim();

                WarehouseQuery.GetField("STATE", ref field);
                string Depot = field.ToString().Trim();

                OrganizationID = "1";//GetFieldValue("OrganizationLanguage", "OrganizationID", "Description = '" + Depot + "'", db_vms);
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
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.UpdateQueryString("Warehouse", " WarehouseID = " + WarehouseID, db_vms);

            }
            else if (err == InCubeErrors.DBNoMoreRows) // New Warehouse --- Insert Query
            {
                TOTALINSERTED++;

                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                QueryBuilderObject.SetField("Barcode", "'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.SetField("WarehouseTypeID", "1");
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

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

            string SelectSalesperson = @"select DEX_ROW_ID,SLPRSNID,SLPRSNFN,ADDRESS2,PHONE1,PHONE2,ADDRESS1  FROM rm00301 where INACTIVE <>1
";

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
            err = SalespersonQuery.Execute();

            ClearProgress();
            SetProgressMax(SalespersonQuery.GetDataTable().Rows.Count);

            err = SalespersonQuery.FindFirst();

            while (err == InCubeErrors.Success)
            {
                ReportProgress("Updating Salesperon");

                SalespersonQuery.GetField("SLPRSNID", ref field);
                string SalespersonCode = field.ToString().Trim();


                string SalespersonID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode='" + SalespersonCode + "'", db_vms).Trim();
                if (SalespersonID.Equals(string.Empty))
                {
                    SalespersonID = GetFieldValue("Employee", "ISNULL(MAX(EmployeeID),0) + 1", db_vms).Trim();
                }

                //if (SalespersonCode.Length == 3)
                //{
                DivisionID = "1";
                //}
                //else if (SalespersonCode.Length == 4)
                //{
                //    DivisionID = "2";
                //}

                SalespersonQuery.GetField("SLPRSNFN", ref field);
                string SalespersonName = field.ToString().Trim();

                SalespersonQuery.GetField("ADDRESS2", ref field);
                string SupervisorName = field.ToString().Trim();

                SalespersonQuery.GetField("PHONE2", ref field);
                string SupervisorPhone = field.ToString().Trim();
                SalespersonQuery.GetField("PHONE1", ref field);
                string Phone = field.ToString().Trim();
                SalespersonQuery.GetField("ADDRESS1", ref field);
                string Address = field.ToString().Trim();
                SalespersonQuery.GetField("ADDRESS3", ref field);
                string Type = field.ToString().Trim();

                switch (Type.ToLower())
                {
                    case "cr":
                        Type = "2";
                        break;
                    case "sup":
                        Type = "4";
                        break;
                    case "dr":
                        Type = "3";
                        break;
                    default:
                        Type = "1";
                        break;
                }
                //SalespersonQuery.GetField("LOCNDSCR", ref field);
                OrganizationID = GetFieldValue("OrganizationLanguage", "OrganizationID", "Description = 'Default Organization'", db_vms);

                AddUpdateSalesperson(Type, SalespersonID, SalespersonCode, SalespersonName, Phone, Address, ref TOTALUPDATED, ref TOTALINSERTED, DivisionID, OrganizationID, SupervisorName, SupervisorPhone);

                err = SalespersonQuery.FindNext();
            }

            SalespersonQuery.Close();
            WriteMessage("\r\n");
            WriteMessage("<<< SALESPERSON >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        private void AddUpdateSalesperson(string employeeType, string SalespersonID, string SalespersonCode, string SalespersonName, string Phone, string Address, ref int TOTALUPDATED, ref int TOTALINSERTED, string DivisionID, string OrganizationID, string SupervisorName, string SupervisorPhone)
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
                QueryBuilderObject.SetField("EmployeeTypeID", employeeType);

                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                err = QueryBuilderObject.InsertQueryString("Employee", db_vms);
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

            err = ExistObject("Operator", "EmployeeID", "EmployeeID = " + SalespersonID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("OperatorName", "'" + SalespersonCode + "'");
                //QueryBuilderObject.SetField("OperatorPassword", "'" + SalespersonCode + "'");
                QueryBuilderObject.SetField("Status", "0");
                QueryBuilderObject.SetField("FrontOffice", "1");
                QueryBuilderObject.InsertQueryString("Operator", db_vms);
            }
            if (SupervisorPhone.Length > 10)
                SupervisorPhone = SupervisorPhone.Substring(0, 10);

            #region Supervisor

            //if (SupervisorName != string.Empty)
            //{
            //    string DeleteSupervisor = "delete from EmployeeSupervisor where EmployeeID = " + SalespersonID;
            //    QueryBuilderObject.RunQuery(DeleteSupervisor, db_vms);

            //    string SupervisorID = GetFieldValue("EmployeeLanguage", "EmployeeID", "Description = '" + SupervisorName + "' AND LanguageID = 1", db_vms);
            //    if (SupervisorID == string.Empty)
            //    {
            //        string MaxDXrow = GetFieldValue("rm00301", "Max(DEX_ROW_ID)", " STATE IN (SELECT IntLocation FROM ALNIntegration) AND NOT LOCNCODE IN (SELECT ColdStore FROM ALNIntegration) AND (LEN(LOCNCODE)=3 or LEN(LOCNCODE)=4) ", db_ERP);

            //        int _empID = int.Parse(MaxDXrow) + int.Parse(SalespersonID);
            //        SupervisorID = _empID.ToString();

            //        QueryBuilderObject.SetField("EmployeeID", SupervisorID);
            //        QueryBuilderObject.SetField("Phone", "'" + SupervisorPhone + "'");
            //        QueryBuilderObject.SetField("Mobile", "'" + SupervisorPhone + "'");
            //        QueryBuilderObject.SetField("EmployeeCode", "'" + SupervisorID + "'");
            //        QueryBuilderObject.SetField("NationalIDNumber", "0");
            //        QueryBuilderObject.SetField("OrganizationID", OrganizationID);
            //        QueryBuilderObject.SetField("InActive", "0");
            //        QueryBuilderObject.SetField("OnHold", "0");
            //        QueryBuilderObject.SetField("EmployeeTypeID", "4");
            //        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
            //        QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
            //        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
            //        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
            //        QueryBuilderObject.InsertQueryString("Employee", db_vms);

            //        QueryBuilderObject.SetField("EmployeeID", SupervisorID);
            //        QueryBuilderObject.SetField("LanguageID", "1");
            //        QueryBuilderObject.SetField("Description", "'" + SupervisorName + "'");
            //        QueryBuilderObject.SetField("Address", "''");
            //        QueryBuilderObject.InsertQueryString("EmployeeLanguage", db_vms);
            //    }
            //    else
            //    {
            //        QueryBuilderObject.SetField("Phone", "'" + SupervisorPhone + "'");
            //        QueryBuilderObject.SetField("Mobile", "'" + SupervisorPhone + "'");
            //        QueryBuilderObject.SetField("EmployeeTypeID", "4");

            //        QueryBuilderObject.UpdateQueryString("Employee", " EmployeeID = " + SupervisorID, db_vms);
            //    }
            //    QueryBuilderObject.SetField("EmployeeID", SalespersonID);
            //    QueryBuilderObject.SetField("SupervisorID", SupervisorID);
            //    QueryBuilderObject.InsertQueryString("EmployeeSupervisor", db_vms);
            //}

            #endregion

            err = ExistObject("EmployeeDivision", "EmployeeID", "EmployeeID = " + SalespersonID + " AND DivisionID = " + DivisionID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("DivisionID", DivisionID);
                QueryBuilderObject.InsertQueryString("EmployeeDivision", db_vms);
            }

            err = ExistObject("EmployeeOrganization", "EmployeeID", "EmployeeID = " + SalespersonID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.InsertQueryString("EmployeeOrganization", db_vms);
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

                incubeQry = new InCubeQuery(db_vms, dataTableCreationQuery);
                if (incubeQry.Execute() == InCubeErrors.Success)
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
        public override void UpdatePrice()
        {
            try
            {
                WriteMessage("\r\nChecking connection to GP ...");
                if (!db_ERP.IsOpened())
                {
                    WriteMessage("\r\nCannot connect to GP database , please check the connection");
                    return;
                }

                WriteMessage("\r\nReading from GP ...");

                string PricesAssignmentQry = @"SELECT DISTINCT PRCLEVEL,CUSTNMBR FROM RM00101 WHERE SALSTERR <> '' AND  INACTIVE=0 and CUSTCLAS not in ('DOUBTFULL','') and PRCLEVEL not in ('EXTPRCLVL') and SALSTERR<>'' and SALSTERR<>'JEEMA' ";
                incubeQry = new InCubeQuery(PricesAssignmentQry, db_ERP);
                DataTable dtPriceAssignment = new DataTable();
                if (incubeQry.Execute() != InCubeErrors.Success)
                {
                    WriteMessage("\r\nError reading price list assignment, check log");
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, PricesAssignmentQry, LoggingType.Error, LoggingFiles.InCubeLog);
                    return;
                }
                dtPriceAssignment = incubeQry.GetDataTable();

                string PriceDefinitionQry = @"SELECT DISTINCT P.PRCLEVEL, R.ISDEFAULT, P.ITEMNMBR,P.UOFM, P.UOMPRICE, ISNULL(V.TXDTLPCT,0) VAT , ISNULL(E.TXDTLAMT ,0) EXCISE
FROM IV00108 P 
INNER JOIN (SELECT 'EXTPRCLVL' PRCLEVEL, 1 IsDefault UNION SELECT DISTINCT PRCLEVEL, 0 FROM RM00101
WHERE SALSTERR <> '' AND INACTIVE=0 AND CUSTCLAS NOT IN ('DOUBTFULL','') AND PRCLEVEL NOT IN ('EXTPRCLVL') AND SALSTERR<>'' AND SALSTERR<>'JEEMA') R ON R.PRCLEVEL = P.PRCLEVEL
INNER JOIN IV00101 I ON I.ITEMNMBR = P.ITEMNMBR
LEFT JOIN 
(SELECT TAXSCHID, SUM(TXDTLPCT) TXDTLPCT  
FROM  TX00102 H 
INNER JOIN TX00201 D ON  H.TAXDTLID = D.TAXDTLID
WHERE D.TAXDTLID IN  ('SLS-TAX-VAT','SLS-TAX-VAT-0%')
GROUP BY TAXSCHID) V ON V.TAXSCHID = I.ITMTSHID  
LEFT JOIN 
(SELECT TAXSCHID , SUM(TXDTLAMT) TXDTLAMT 
FROM  TX00102 H 
INNER JOIN TX00201 D ON  H.TAXDTLID = D.TAXDTLID  WHERE D.TAXDTLID IN  ('SLS-TAX-EXCISE')
GROUP BY TAXSCHID) E ON E.TAXSCHID = I.ITMTSHID 
AND I.ITEMTYPE <> 2 AND P.ITEMNMBR LIKE 'FG%'";
                incubeQry = new InCubeQuery(PriceDefinitionQry, db_ERP);
                DataTable dtPriceDefinition = new DataTable();
                if (incubeQry.Execute() != InCubeErrors.Success)
                {
                    WriteMessage("\r\nError reading price list definition, check log");
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, PriceDefinitionQry, LoggingType.Error, LoggingFiles.InCubeLog);
                    return;
                }
                dtPriceDefinition = incubeQry.GetDataTable();

                int TotalRows = 0;
                TotalRows = dtPriceDefinition.Rows.Count + dtPriceAssignment.Rows.Count;
                WriteMessage("\r\nSaving to staging tables ...");

                if (SaveTable(dtPriceAssignment, "stg_PriceAssignment", true) != Result.Success)
                {
                    WriteMessage("\r\nError reading price list definition, check log");
                    return;
                }

                if (SaveTable(dtPriceDefinition, "stg_PriceDefinition", true) != Result.Success)
                {
                    WriteMessage("\r\nError reading price list definition, check log");
                    return;
                }

                WriteMessage("\r\nRunning stored procedure ...");

                Procedure Proc = new Procedure("sp_UpdatePrices");
                Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID);
                Proc.AddParameter("@ResultID", ParamType.Integer, "0", ParamDirection.Output);
                Proc.AddParameter("@Skipped", ParamType.Integer, "0", ParamDirection.Output);
                Proc.AddParameter("@Updated", ParamType.Integer, "0", ParamDirection.Output);
                //Proc.AddParameter("@Message", ParamType.Nvarchar, "", ParamDirection.Output);
                Result result = ExecuteStoredProcedure(Proc);
                int ResultID = 0;
                int.TryParse(Proc.Parameters["@ResultID"].ParameterValue.ToString(), out ResultID);
                if (result != Result.Success || ResultID != 1)
                {
                    //string message = Proc.Parameters["@Message"].ParameterValue.ToString();
                    WriteMessage(" Failed!!! ");
                }
                else
                {
                    int Skipped = 0, Updated = 0;
                    int.TryParse(Proc.Parameters["@Skipped"].ParameterValue.ToString(), out Skipped);
                    int.TryParse(Proc.Parameters["@Updated"].ParameterValue.ToString(), out Updated);

                    WriteMessage(string.Format("Success .. Total rows = {0}, Skipped = {1}, Updated = {2}", TotalRows, Skipped, Updated));
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public void UpdatePrice_old()
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

            InCubeQuery DeleteCustomerPriceQuery = new InCubeQuery(db_vms, "Delete From CustomerPrice");
            DeleteCustomerPriceQuery.ExecuteNonQuery();


            // for Al-Ain, the dfault price list is designated as "STANDARD"
            UpdatePriceList("EXTPRCLVL", "1", "1", true, ref TOTALUPDATED);

            string SelectGroup = @"select distinct CUSTNMBR,PRCLEVEL from RM00101 WHERE SALSTERR <> '' and  INACTIVE=0 and CUSTCLAS not in ('DOUBTFULL','') and PRCLEVEL not in ('EXTPRCLVL') and SALSTERR<>'' and SALSTERR<>'JEEMA' ";
            InCubeQuery GroupQuery = new InCubeQuery(db_ERP, SelectGroup);
            GroupQuery.Execute();
            err = GroupQuery.FindFirst();

            while (err == InCubeErrors.Success)
            {
                GroupQuery.GetField("CUSTNMBR", ref field);
                string CustCode = field.ToString().Trim();
                GroupQuery.GetField("PRCLEVEL", ref field);
                string PLDesc = field.ToString().Trim();

                string SelectCustomer = @"select CustomerID,OutletID from CustomerOutlet WHERE InActive = 0 AND CustomerCode='" + CustCode + "' and Barcode='" + CustCode + "'";
                InCubeQuery CustQry = new InCubeQuery(db_vms, SelectCustomer);
                err = CustQry.Execute();
                err = CustQry.FindFirst();
                CustQry.GetField("CustomerID", ref field);
                string CustID = field.ToString().Trim();
                CustQry.GetField("OutletID", ref field);
                string outletID = field.ToString().Trim();
                if (CustID != string.Empty)
                {
                    UpdatePriceList(PLDesc, CustID, outletID, false, ref TOTALUPDATED);
                }


                err = GroupQuery.FindNext();
            }
            GroupQuery.Close();

            WriteMessage("\r\n");
            WriteMessage("<<< PRICE >>> Total Updated = " + TOTALUPDATED);
        }
        private void UpdatePriceList(string priceListName, string CustID, string outletID, bool defaultList, ref int TOTALUPDATED)
        {
            object field = new object();
            InCubeErrors err;




            string priceQry = @"SELECT ITEMNMBR,PRCLEVEL, UOMPRICE, QTYBSUOM,UOFM FROM IV00108 WHERE ltrim(rtrim(PRCLEVEL))='" + priceListName.Trim() + "' AND ITEMNMBR IN (SELECT ITEMNMBR FROM IV00101 I WHERE I.ITEMTYPE<>2 and ITEMNMBR like 'FG%') order by PRCLEVEL";
            string PriceQry = string.Format(@"SELECT P.ITEMNMBR,P.PRCLEVEL, P.UOMPRICE, P.QTYBSUOM,P.UOFM ,I.ITMTSHID ,ISNULL(V.TXDTLPCT,0) VAT , ISNULL(E.TXDTLAMT ,0) EXCISE
FROM IV00108 P INNER JOIN IV00101 I ON I.ITEMNMBR = P.ITEMNMBR

LEFT JOIN 
(SELECT TAXSCHID ,  SUM(TXDTLPCT)  TXDTLPCT  FROM  TX00102 H INNER JOIN TX00201 D ON  H.TAXDTLID = D.TAXDTLID  WHERE D.TAXDTLID IN  ('SLS-TAX-VAT','SLS-TAX-VAT-0%')
GROUP BY TAXSCHID) V ON V.TAXSCHID = I.ITMTSHID  
LEFT JOIN 
(SELECT TAXSCHID , SUM(TXDTLAMT) TXDTLAMT FROM  TX00102 H INNER JOIN TX00201 D ON  H.TAXDTLID = D.TAXDTLID  WHERE D.TAXDTLID IN  ('SLS-TAX-EXCISE')
GROUP BY TAXSCHID) E ON E.TAXSCHID = I.ITMTSHID 
WHERE LTRIM(RTRIM(P.PRCLEVEL))='{0}' 
AND I.ITEMTYPE <> 2 AND P.ITEMNMBR LIKE 'FG%'
ORDER BY PRCLEVEL", priceListName.Trim());

            //            string PriceQry = string.Format(@"SELECT P.ITEMNMBR,P.PRCLEVEL, P.UOMPRICE, P.QTYBSUOM,P.UOFM
            //,ISNULL(V.TXDTLPCT,0) VAT ,ISNULL(E.TXDTLAMT ,0) Excise
            //FROM IV00108 P
            //INNER JOIN IV00101 I ON I.ITEMNMBR = P.ITEMNMBR
            //LEFT JOIN TX00201 V ON V.TAXDTLID = I.ITMTSHID AND V.TAXDTLID = 'SALES-TAX-VAT'
            //LEFT JOIN TX00201 E ON E.TAXDTLID = I.ITMTSHID AND E.TAXDTLID = 'SALES-TAX-EDT'
            //WHERE LTRIM(RTRIM(P.PRCLEVEL))='{0}' 
            //AND I.ITEMTYPE <> 2 AND P.ITEMNMBR LIKE 'FG%'
            //ORDER BY PRCLEVEL", priceListName.Trim());


            InCubeQuery ItemPriceQuery = new InCubeQuery(db_ERP, PriceQry); //change PriceQry instead of priceQry
            err = ItemPriceQuery.Execute();

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
                QueryBuilderObject.SetField("StartDate", "'" + DateTime.Now.Date.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("EndDate", "'" + DateTime.Now.Date.AddYears(10).ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("Priority", "1");
                err = QueryBuilderObject.InsertQueryString("PriceList", db_vms);

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
            //if pln exist
            string ExcisePriceListID = "-1";
            ExcisePriceListID = GetFieldValue("PriceList", "PriceListID", "PriceListCode = 'ExcisePL' AND PriceListTypeID = 3", db_vms);
            if (ExcisePriceListID == string.Empty)
            {
                ExcisePriceListID = GetFieldValue("PriceList", "ISNULL(MAX(PriceListID),0) + 1", db_vms);

                QueryBuilderObject.SetField("PriceListID", ExcisePriceListID);
                QueryBuilderObject.SetField("PriceListCode", "'ExcisePL'");
                QueryBuilderObject.SetField("StartDate", "'" + DateTime.Now.Date.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("EndDate", "'" + DateTime.Now.Date.AddYears(20).ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("Priority", "1");
                QueryBuilderObject.SetField("PriceListTypeID", "3");
                err = QueryBuilderObject.InsertQueryString("PriceList", db_vms);

                QueryBuilderObject.SetField("PriceListID", ExcisePriceListID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'Excise price list'");
                QueryBuilderObject.InsertQueryString("PriceListLanguage", db_vms);

                QueryBuilderObject.SetField("KeyValue", ExcisePriceListID);
                QueryBuilderObject.UpdateQueryString("Configuration", "KeyName = 'DefaultRetailPriceListID' AND EmployeeID = -1", db_vms);
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

                ItemPriceQuery.GetField("VAT", ref field);
                decimal VAT = Math.Round(decimal.Parse(field.ToString()), 4);

                ItemPriceQuery.GetField("Excise", ref field);
                decimal Excise = Math.Round(decimal.Parse(field.ToString()), 4);

                ItemPriceQuery.GetField("UOFM", ref field);
                string PackCode = field.ToString();

                err = ExistObject("CustomerPrice", "CustomerID", " CustomerID = " + CustID + " and OutletID='" + outletID + "' AND PriceListID = " + PriceListID, db_vms);
                if (err != InCubeErrors.Success && !defaultList)
                {
                    QueryBuilderObject.SetField("CustomerID", CustID);
                    QueryBuilderObject.SetField("OutletID", outletID);
                    QueryBuilderObject.SetField("PriceListID", PriceListID);
                    err = QueryBuilderObject.InsertQueryString("CustomerPrice", db_vms);
                }

                string PackID = GetFieldValue("Pack inner join item on pack.itemid = item.itemid", "pack.PackID", " item.itemcode = '" + ItemCode.ToString().Trim() + "' and PackTypeID in (select packtypeID from Packtypelanguage where description='" + PackCode + "')", db_vms);
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
                        QueryBuilderObject.SetField("Tax", VAT.ToString());
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
                            QueryBuilderObject.SetField("Tax", VAT.ToString());
                            QueryBuilderObject.UpdateQueryString("PriceDefinition", "PackID = " + PackID + " AND PriceListID = " + PriceListID + " AND PriceDefinitionID = " + PriceDefinitionID, db_vms);
                        }
                    }
                    //if price list level is default
                    PriceDefinitionID = 1;
                    string currentExcise = GetFieldValue("PriceDefinition", "Price", "PackID = " + PackID + " AND PriceListID = " + ExcisePriceListID, db_vms);
                    if (currentExcise.Equals(string.Empty))
                    {
                        PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms));

                        QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID.ToString());
                        QueryBuilderObject.SetField("QuantityRangeID", "1");
                        QueryBuilderObject.SetField("PackID", PackID);
                        QueryBuilderObject.SetField("CurrencyID", "1");
                        //QueryBuilderObject.SetField("Tax", Excise.ToString());
                        QueryBuilderObject.SetField("Tax", "100");
                        QueryBuilderObject.SetField("Price", Excise.ToString());
                        //QueryBuilderObject.SetField("Price", Price.ToString());
                        QueryBuilderObject.SetField("PriceListID", ExcisePriceListID);
                        err = QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);

                    }
                    else
                    {
                        PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "PriceDefinitionID", "PackID = " + PackID + " AND PriceListID = " + ExcisePriceListID, db_vms));

                        if (!currentExcise.Equals(Excise.ToString()))
                        {
                            QueryBuilderObject.SetField("Price", Excise.ToString());
                            //QueryBuilderObject.SetField("Price", Price.ToString());
                            QueryBuilderObject.SetField("Tax", "100");
                            //QueryBuilderObject.SetField("Tax", Excise.ToString());
                            QueryBuilderObject.UpdateQueryString("PriceDefinition", "PackID = " + PackID + " AND PriceListID = " + ExcisePriceListID + " AND PriceDefinitionID = " + PriceDefinitionID, db_vms);
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
            try
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                string SelectBalance = @"SELECT  RM00101.CUSTNMBR, RM00101.CRLMTAMT,RM00101.CPRCSTNM,RM00103.CUSTBLNC,
CASE WHEN RM00101.CUSTNMBR = RM00101.CPRCSTNM THEN 1 ELSE 0 END AS Parent
FROM  RM00101 
INNER JOIN RM00103 ON RM00101.CUSTNMBR = RM00103.CUSTNMBR 
WHERE RM00101.INACTIVE=0";

                InCubeQuery SelectBalanceQRY = new InCubeQuery(SelectBalance, db_ERP);
                DataTable dt2 = new DataTable();
                err = SelectBalanceQRY.Execute();
                dt2 = SelectBalanceQRY.GetDataTable();

                sw.Stop();
                if (dt2 != null && dt2.Rows.Count > 0)
                {

                    InCubeQuery incubeQuery = new InCubeQuery(db_vms, "TRUNCATE TABLE Stg_CustomerBalance");
                    incubeQuery.ExecuteNonQuery();
                    WriteMessage("\r\nFilling GP data into Sonic staging table ..");
                    SqlBulkCopy bulk = new SqlBulkCopy(db_vms.GetConnection());
                    bulk.DestinationTableName = "Stg_CustomerBalance";
                    foreach (DataColumn col in dt2.Columns)
                        bulk.ColumnMappings.Add(col.Caption, col.Caption);
                    bulk.WriteToServer(dt2);
                }
                else
                {
                    WriteMessage("Error !!");
                }

                sw.Stop();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }

        }
        //        {
        //            if (!db_ERP.IsOpened())
        //            {
        //                WriteMessage("\r\n");
        //                WriteMessage("Cannot connect to GP database , please check the connection");
        //                return;
        //            }

        //            #region Customer

        //            int TOTALUPDATED = 0;

        //            InCubeErrors err;
        //            object field = new object();

        //            string SelectBalance = @"SELECT  RM00101.CUSTNMBR, RM00101.CRLMTAMT,
        //                                              RM00103.CUSTBLNC FROM  RM00101 
        //                                              INNER JOIN RM00103 ON RM00101.CUSTNMBR = RM00103.CUSTNMBR 
        //WHERE RM00101.SALSTERR <> '' and RM00101.INACTIVE=0 
        //";

        //            InCubeQuery BalanceQuery = new InCubeQuery(db_ERP, SelectBalance);
        //            err = BalanceQuery.Execute();

        //            ClearProgress();
        //            SetProgressMax(BalanceQuery.GetDataTable().Rows.Count);

        //            err = BalanceQuery.FindFirst();

        //            while (err == InCubeErrors.Success)
        //            {
        //                ReportProgress("Updating Balance");

        //                BalanceQuery.GetField("CUSTNMBR", ref field);
        //                string CustomerCode = field.ToString();

        //                BalanceQuery.GetField("CRLMTAMT", ref field);

        //                decimal CustomerCREATDDT = 0;


        //                CustomerCREATDDT = decimal.Parse(field.ToString());


        //                if (CustomerCREATDDT > 99999999) CustomerCREATDDT = 99999999;

        //                decimal Balance = 0;
        //                string BalanceStr = GetFieldValue("RM00103", "CUSTBLNC", "CUSTNMBR ='" + CustomerCode + "'", db_ERP);
        //                if (BalanceStr != string.Empty)
        //                {
        //                    Balance = decimal.Parse(BalanceStr);
        //                    if (Balance > 99999999) Balance = 99999999;
        //                }

        //                TOTALUPDATED++;

        //                string CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", " Barcode = '" + CustomerCode.ToString() + "'", db_vms);
        //                string OutletID = GetFieldValue("CustomerOutlet", "OutletID", " Barcode = '" + CustomerCode.ToString() + "'", db_vms);

        //                string AccountID = GetFieldValue("AccountCustOut", "AccountID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
        //                string CustAccountID = GetFieldValue("AccountCust", "AccountID", "CustomerID = " + CustomerID , db_vms);

        //                if (AccountID != string.Empty)
        //                {
        //                    QueryBuilderObject.SetField("Balance", Balance.ToString());
        //                    QueryBuilderObject.SetField("CreditLimit", CustomerCREATDDT.ToString());
        //                    QueryBuilderObject.UpdateQueryString("Account", " AccountID = " + AccountID.ToString(), db_vms);



        //                    //string CustomerBalance = "Select SUM(Balance) AS Balance From Account Where ParentAccountID='" + CustAccountID + "'";
        //                    //InCubeQuery CustomerBalanceUpdateQuery = new InCubeQuery(db_vms, CustomerBalance);
        //                    //CustomerBalanceUpdateQuery.Execute();
        //                    //err = CustomerBalanceUpdateQuery.FindFirst();
        //                    //CustomerBalanceUpdateQuery.GetField("Balance", ref field);
        //                    //string CustBalance = field.ToString().Trim();

        //                    //QueryBuilderObject.SetField("Balance", CustBalance.ToString());
        //                    //QueryBuilderObject.UpdateQueryString("Account", " AccountID = " + CustAccountID.ToString(), db_vms);



        //                }

        //                err = BalanceQuery.FindNext();
        //            }

        //            string CustomerBalance = "Update A Set A.Balance = A2.Balance From Account A LEFT Join(Select ParentAccountID, Round(Sum(Balance), 2) AS Balance  from Account Where ParentAccountID IS NOT NULL Group By ParentAccountID) A2 ON A.AccountID = A2.ParentAccountID Where A.ParentAccountID IS NULL";
        //            InCubeQuery CustomerBalanceUpdateQuery = new InCubeQuery(db_vms, CustomerBalance);
        //            CustomerBalanceUpdateQuery.ExecuteNonQuery();


        //            BalanceQuery.Close();
        //            WriteMessage("\r\n");
        //            WriteMessage("<<< CUSTOMER BALANCE >>> Total Updated = " + TOTALUPDATED);
        //            #endregion

        //            #region Salesperson

        //            QueryBuilderObject.SetField("CreditLimit", "0");
        //            QueryBuilderObject.SetField("Balance", "0");
        //            QueryBuilderObject.UpdateQueryString("Account", "AccountTypeID = 2", db_vms);

        //            /*            TOTALUPDATED = 0;
        //                        string SelectSalesPersonBalance = @"SELECT CustomerOutlet.Barcode AS CustomerCode, Employee.EmployeeCode, AccountCustOutDivEmp.CustomerID, AccountCustOutDivEmp.OutletID, 
        //                                  AccountCustOutDivEmp.EmployeeID, AccountCustOutDivEmp.AccountID
        //                                  FROM AccountCustOutDivEmp INNER JOIN
        //                                  Account ON AccountCustOutDivEmp.AccountID = Account.AccountID INNER JOIN
        //                                  CustomerOutlet ON AccountCustOutDivEmp.CustomerID = CustomerOutlet.CustomerID AND 
        //                                  AccountCustOutDivEmp.OutletID = CustomerOutlet.OutletID INNER JOIN
        //                                  Employee ON AccountCustOutDivEmp.EmployeeID = Employee.EmployeeID";
        //                        InCubeQuery SalesPersonBalanceQuery = new InCubeQuery(db_vms, SelectSalesPersonBalance);
        //                        SalesPersonBalanceQuery.Execute();
        //                        ClearProgress();
        //                        SetProgressMax(SalesPersonBalanceQuery.GetDataTable().Rows.Count;
        //                        err = SalesPersonBalanceQuery.FindFirst();
        //                        while (err == InCubeErrors.Success)
        //                        {
        //                            ReportProgress();
        //                            IntegrationForm.lblProgress.Text = "Updating Salesperson Balance" + " " + IntegrationForm.progressBar1.Value + " / " + IntegrationForm.progressBar1.Maximum;

        //                            SalesPersonBalanceQuery.GetField(0, ref field);
        //                            string CustomerCode = field.ToString();
        //                            SalesPersonBalanceQuery.GetField(1, ref field);
        //                            string EmployeeCode = field.ToString();
        //                            SalesPersonBalanceQuery.GetField(2, ref field);
        //                            string CustomerID = field.ToString();
        //                            SalesPersonBalanceQuery.GetField(3, ref field);
        //                            string OutletID = field.ToString();
        //                            SalesPersonBalanceQuery.GetField(4, ref field);
        //                            string SalespersonID = field.ToString();
        //                            SalesPersonBalanceQuery.GetField(5, ref field);
        //                            string AccountID = field.ToString();
        //                            decimal totalInvoice, totalReturn, TotalPayment, Balance;
        //                            totalInvoice = 0;
        //                            totalReturn = 0;
        //                            TotalPayment = 0;
        //                            Balance = 0;
        //                            SelectSalesPersonBalance = @"SELECT RM20101.RMDTYPAL, SUM(RM20101.CURTRXAM) AS Expr1 FROM RM20101
        //            INNER JOIN SOP30200 ON RM20101.CUSTNMBR = SOP30200.CUSTNMBR AND RM20101.DOCNUMBR = SOP30200.SOPNUMBE
        //            GROUP BY RM20101.SLPRSNID, RM20101.CUSTNMBR, RM20101.RMDTYPAL
        //            HAVING (RM20101.RMDTYPAL < 10) AND (RM20101.SLPRSNID = '" + SalespersonID + "') AND (RM20101.CUSTNMBR = '" + CustomerCode + "') AND (SUM(RM20101.CURTRXAM) > 0)";
        //                            InCubeQuery Query = new InCubeQuery(db_ERP, SelectSalesPersonBalance);
        //                            Query.Execute();
        //                            err = Query.FindFirst();
        //                            while (err == InCubeErrors.Success)
        //                            {
        //                                Query.GetField(0, ref field);
        //                                int RMDTYPAL = int.Parse(field.ToString());
        //                                Query.GetField(1, ref field);
        //                                decimal value = decimal.Parse(field.ToString());
        //                                if (RMDTYPAL < 7) totalInvoice += value;
        //                                if (RMDTYPAL == 8) totalReturn += value;
        //                                err = Query.FindNext();
        //                            }
        //                            Query.Close();
        //                            Balance = totalInvoice;
        //                            SelectSalesPersonBalance = @"SELECT RM20101.RMDTYPAL, SUM(RM20101.CURTRXAM) AS Expr1
        //                                                         FROM RM00101 INNER JOIN
        //                                                         RM20101 ON RM00101.CUSTNMBR = RM20101.CUSTNMBR
        //                                                         GROUP BY RM00101.CUSTNMBR, RM00101.SLPRSNID, RM20101.RMDTYPAL
        //                                                         HAVING (RM00101.SLPRSNID = '" + SalespersonID + "') AND  (RM00101.CUSTNMBR = '" + CustomerCode + "')  AND  (SUM(RM20101.CURTRXAM) > 0) AND (RM20101.RMDTYPAL = 9 OR   RM20101.RMDTYPAL = 7)";
        //                            Query = new InCubeQuery(db_ERP, SelectSalesPersonBalance);
        //                            Query.Execute();

        //                            err = Query.FindFirst();

        //                            while (err == InCubeErrors.Success)
        //                            {
        //                                Query.GetField(1, ref field);
        //                                decimal value = decimal.Parse(field.ToString());

        //                                TotalPayment += value;

        //                                err = Query.FindNext();
        //                            }
        //                            Query.Close();

        //                            Balance = Balance - totalReturn - TotalPayment;

        //                            QueryBuilderObject.SetField("Balance", Balance.ToString());
        //                            QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + AccountID, db_vms);
        //                            err = SalesPersonBalanceQuery.FindNext();
        //                        }

        //                        SalesPersonBalanceQuery.Close();
        //                        WriteMessage("\r\n");
        //                        WriteMessage("<<< CUSTOMER SALESPERSON BALANCE >>> Total Updated = " + TOTALUPDATED);
        //            */
        //            #endregion
        //        }



        #endregion

        //        #region UpdateInvoiceAmount

        //        public override void UpdateCreditInvoiceAmount()
        //        {
        //            int TOTALUPDATED = 0;

        //            InCubeErrors err;
        //            object field = new object();

        //            if (!db_ERP.IsOpened())
        //            {
        //                WriteMessage("\r\n");
        //                WriteMessage("Cannot connect to GP database , please check the connection");
        //                return;
        //            }

        //            string SelectInvoices = @"SELECT
        //T.TransactionID,
        //T.RemainingAmount
        //
        //FROM [Transaction] T
        //
        //inner join customeroutlet C on T.customerid = c.customerid and T.outletid = c.outletid
        //where (T.TransactionTypeID = 1 or T.TransactionTypeID = 3) and C.CustomerTypeID = 2";

        //            InCubeQuery InvoicesQuery = new InCubeQuery(db_vms, SelectInvoices);
        //            InvoicesQuery.Execute();

        //            ClearProgress();
        //            SetProgressMax(InvoicesQuery.GetDataTable().Rows.Count;

        //            err = InvoicesQuery.FindFirst();

        //            while (err == InCubeErrors.Success)
        //            {

        //                ReportProgress();

        //                IntegrationForm.lblProgress.Text = "Updating Invoices Amount " + " " + IntegrationForm.progressBar1.Value + " / " + IntegrationForm.progressBar1.Maximum;
        //                

        //                InvoicesQuery.GetField("TransactionID", ref field);
        //                string SalesTransactionID = field.ToString();

        //                InvoicesQuery.GetField("RemainingAmount", ref field);
        //                string RemainingAmount = field.ToString();

        //                err = ExistObject("RM20101", "DOCNUMBR", "DOCNUMBR = '" + SalesTransactionID + "'", db_ERP);
        //                if (err == InCubeErrors.Success)
        //                {
        //                    TOTALUPDATED++;
        //                    string Ramount = GetFieldValue("RM20101", "CURTRXAM", "DOCNUMBR = '" + SalesTransactionID + "'", db_ERP);
        //                    if (Ramount.Equals(string.Empty))
        //                    {
        //                        Ramount = "0";
        //                    }
        //                    QueryBuilderObject.SetField("RemainingAmount", Ramount);
        //                    QueryBuilderObject.UpdateQueryString("[Transaction]", "TransactionID = '" + SalesTransactionID.ToString().Trim() + "'", db_vms);
        //                }

        //                err = InvoicesQuery.FindNext();
        //            }
        //            InvoicesQuery.Close();

        //            WriteMessage("\r\n");
        //            WriteMessage("<<< UPDATED INVOICES >>> Total Updated = " + TOTALUPDATED);
        //        }

        //        #endregion

        //        #region UpdateInvoice

        //        public override void UpdateInvoice()
        //        {
        //            int TOTALUPDATED = 0;
        //            int TOTALINSERTED = 0;

        //            InCubeErrors err;
        //            object field = new object();

        //            if (!db_ERP.IsOpened())
        //            {
        //                WriteMessage("\r\n");
        //                WriteMessage("Cannot connect to GP database , please check the connection");
        //                return;
        //            }

        //            string SelectInvoices = @"SELECT     
        //SOP30200.SOPTYPE, 
        //SOP30200.SOPNUMBE, 
        //SOP30200.ORIGNUMB, 
        //SOP30200.DOCID, 
        //SOP30200.DOCDATE, 
        //SOP30200.LOCNCODE,
        //ltrim(rtrim(cast(RM00102.CUSTNMBR as nvarchar(50)))) + ltrim(rtrim(cast(RM00102.DEX_ROW_ID as nvarchar(50)))) as ADDRESSSCODE, 
        //SOP30200.SUBTOTAL, 
        //SOP30200.SLPRSNID, 
        //RM20101.CURTRXAM, 
        //SOP30200.CUSTNMBR
        // 
        //FROM SOP30200 INNER JOIN  RM20101 ON SOP30200.SOPNUMBE = RM20101.DOCNUMBR 
        //INNER JOIN RM00101 ON SOP30200.CUSTNMBR = RM00101.CUSTNMBR 
        //INNER JOIN RM00102 ON SOP30200.CUSTNMBR = RM00102.CUSTNMBR AND SOP30200.PRSTADCD = RM00102.ADRSCODE
        //
        //WHERE  (SOP30200.SOPTYPE = 3) AND (RM20101.CURTRXAM > 0) 
        //and (RM20101.RMDTYPAL = 1) and (RM00101.SHIPMTHD = 'SBU5')";   // RM00101.SHIPMTHD = 'SBU5' Should be Changed.

        //            InCubeQuery InvoicesQuery = new InCubeQuery(db_ERP, SelectInvoices);
        //            InvoicesQuery.Execute();

        //            ClearProgress();
        //            SetProgressMax(InvoicesQuery.GetDataTable().Rows.Count;

        //            err = InvoicesQuery.FindFirst();

        //            while (err == InCubeErrors.Success)
        //            {
        //                ReportProgress();

        //                IntegrationForm.lblProgress.Text = "Updating Invoices" + " " + IntegrationForm.progressBar1.Value + " / " + IntegrationForm.progressBar1.Maximum;
        //                


        //                InvoicesQuery.GetField(6, ref field);
        //                string CustomerNumber = field.ToString().Trim();

        //                InvoicesQuery.GetField(8, ref field);
        //                string SalesID = field.ToString().Trim();

        //                InvoicesQuery.GetField(7, ref field);
        //                string Total = field.ToString();

        //                InvoicesQuery.GetField(1, ref field);
        //                string DocNumber = field.ToString().Trim();

        //                InvoicesQuery.GetField(4, ref field);
        //                DateTime Date = DateTime.Parse(field.ToString());

        //                InvoicesQuery.GetField(9, ref field);
        //                string Ramount = field.ToString();

        //                InvoicesQuery.GetField(10, ref field);
        //                string CustomerCode = field.ToString().Trim();

        //                err = ExistObject("[Transaction]", "TransactionID", "TransactionID ='" + DocNumber + "'", db_vms);
        //                if (err == InCubeErrors.Success)
        //                {
        //                    err = InvoicesQuery.FindNext();
        //                    continue;
        //                }


        //                string CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode = '" + CustomerNumber + "' AND Barcode = '" + CustomerCode + "'", db_vms);
        //                string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerCode = '" + CustomerNumber + "' AND Barcode = '" + CustomerCode + "'", db_vms);

        //                if (CustomerID == string.Empty)
        //                {
        //                    err = InvoicesQuery.FindNext();
        //                    continue;
        //                }

        //                string SalespersonID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode ='" + SalesID + "'", db_vms);

        //                if (SalespersonID == string.Empty)
        //                {
        //                    err = InvoicesQuery.FindNext();
        //                    continue;
        //                }

        //                TOTALINSERTED++;

        //                QueryBuilderObject.SetField("CustomerID", CustomerID);
        //                QueryBuilderObject.SetField("OutletID", OutletID);
        //                QueryBuilderObject.SetField("TransactionID", "'" + DocNumber + "'");
        //                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
        //                QueryBuilderObject.SetField("TransactionDate", "'" + Date.ToString("dd/MMM/yyyy") + "'");
        //                QueryBuilderObject.SetField("TransactionTypeID", "1");
        //                QueryBuilderObject.SetField("Discount", "0");
        //                QueryBuilderObject.SetField("Synchronized", "1");
        //                QueryBuilderObject.SetField("RemainingAmount", Ramount);
        //                QueryBuilderObject.SetField("Grosstotal", Total);
        //                QueryBuilderObject.SetField("Nettotal", Total);
        //                QueryBuilderObject.SetField("Posted", "0");

        //                QueryBuilderObject.InsertQueryString("[Transaction]", db_vms);

        //                err = InvoicesQuery.FindNext();
        //            }

        //            InvoicesQuery.Close();
        //            WriteMessage("\r\n");
        //            WriteMessage("<<< INVOICES >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        //        }

        //        #endregion

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
                err = WarehouseQuery.Execute();

                err = WarehouseQuery.FindFirst();
                while (err == InCubeErrors.Success)
                {
                    WarehouseQuery.GetField(0, ref field);

                    int WarehouseID = Convert.ToInt32(field);
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
            InCubeQuery StockQuery;
            #region One Vehicle
            string uploaded = string.Empty;
            string deviceserial = string.Empty;
            string DeleteStock = string.Empty;
            string CheckUploaded = string.Format("select top(1)uploaded,deviceserial from RouteHistory where vehicleid=" + WarehouseID + " order by RouteHistoryID desc ");
            StockQuery = new InCubeQuery(CheckUploaded, db_vms);
            err = StockQuery.Execute();
            err = StockQuery.FindFirst();
            err = StockQuery.GetField("uploaded", ref field);
            uploaded = field.ToString().Trim();
            err = StockQuery.GetField("deviceserial", ref field);
            deviceserial = field.ToString().Trim();
            if (!uploaded.ToString().Trim().Equals(string.Empty) && !uploaded.ToString().Trim().Equals("System.Object"))
            {
                if (Convert.ToBoolean(uploaded.ToString().Trim()))
                {
                    WriteMessage("\r\n");
                    WriteMessage("<<< The device " + deviceserial + " is not downloaded . No stock will be added .>>> Total Updated = " + TOTALUPDATED);
                    return;
                }
                else
                {
                    DeleteStock = "delete from WarehouseStock where WarehouseID = " + WarehouseID;
                    QueryBuilderObject.RunQuery(DeleteStock, db_vms);

                }
            }
            else
            {

                DeleteStock = "delete from WarehouseStock where WarehouseID = " + WarehouseID;
                QueryBuilderObject.RunQuery(DeleteStock, db_vms);
            }

            //DeleteStock = "delete from WarehouseStock where WarehouseID = " + WarehouseID+" and BatchNo ";
            //QueryBuilderObject.RunQuery(DeleteStock, db_vms);
            string WarehouseCode = GetFieldValue("Warehouse", "Barcode", "WarehouseID = " + WarehouseID, db_vms);
            //            string GPStock = @"select HD.GPNo,LN.ITEMNMBR,SUM(LN.ATYALLOC) ATYALLOC,LN.UOFM --,HD.DOCDATE,RMS.SALSTERR
            //from JMGPHDRWORK HD
            //INNER JOIN JMGPLINEWORK LN ON HD.GPNo = LN.GPNo
            //INNER JOIN RM00301 RMS ON HD.SLPRSNID = RMS.SLPRSNID
            //where HD.DOCDATE =convert(datetime, '" + DateTime.Today.ToString() + @"',103)
            //and RMS.SALSTERR = '" + WarehouseCode + @"' and HD.FLAG NOT IN ('P','F')
            //GROUP BY HD.GPNo,LN.ITEMNMBR,LN.UOFM";

            string GPStock = @"select HD.GPNo,LN.ITEMNMBR,SUM(LN.ATYALLOC-LN.QTYRTRND) ATYALLOC,LN.UOFM --,HD.DOCDATE,RMS.SALSTERR
from JMGPHDRWORK HD
INNER JOIN JMGPLINEWORK LN ON HD.GPNo = LN.GPNo
INNER JOIN RM00301 RMS ON HD.SLPRSNID = RMS.SLPRSNID
where RMS.SALSTERR = '" + WarehouseCode + @"' and HD.FLAG NOT IN ('P','F')
GROUP BY HD.GPNo,LN.ITEMNMBR,LN.UOFM
HAVING SUM(LN.ATYALLOC-LN.QTYRTRND)> 0";

            //            string SelectStock = @"SELECT     dbo.IV00101.ITEMNMBR, dbo.IV00102.LOCNCODE, dbo.IV00101.UOMSCHDL, dbo.IV00102.QTYONHND
            //FROM         dbo.IV00101 INNER JOIN
            //                      dbo.IV00102 ON dbo.IV00101.ITEMNMBR = dbo.IV00102.ITEMNMBR
            //WHERE     rtrim(ltrim(dbo.IV00102.LOCNCODE))='" + WarehouseCode + @"' and (dbo.IV00101.ITMCLSCD = 'FG') AND (dbo.IV00102.QTYONHND <> 0)
            //
            //";


            StockQuery = new InCubeQuery(db_ERP, GPStock);
            err = StockQuery.Execute();

            ClearProgress();
            SetProgressMax(StockQuery.GetDataTable().Rows.Count);

            err = StockQuery.FindFirst();

            while (err == InCubeErrors.Success)
            {
                ReportProgress("Updating Stock (" + WarehouseCode + ")");

                StockQuery.GetField("ITEMNMBR", ref field);
                string ItemCode = field.ToString().Trim();

                StockQuery.GetField("UOFM", ref field);
                string UOM = field.ToString().Trim();

                //StockQuery.GetField("Expiry", ref field);
                string ExpiryDate = DateTime.Now.AddYears(10).ToString("yyyy/MM/dd");
                StockQuery.GetField("GPNo", ref field);
                string LOT = field.ToString().Trim();

                try
                {
                    ExpiryDate = DateTime.Parse(ExpiryDate).ToString("yyyy/MM/dd");
                    //LOT = (DateTime.Parse(ExpiryDate)).ToString("yyyy/MM/dd");
                }
                catch
                {
                    ExpiryDate = DateTime.Now.AddYears(10).ToString("yyyy/MM/dd");
                    //LOT = (DateTime.Parse(ExpiryDate)).ToString("yyyy/MM/dd");
                }


                StockQuery.GetField("ATYALLOC", ref field); ///
                decimal PcsQty = decimal.Parse(field.ToString());

                decimal ConversionFactor = 1;

                string ConversionFactorString = GetFieldValue("Pack inner join item on pack.itemid = item.itemid", "pack.Quantity", " item.itemcode = '" + ItemCode.ToString().Trim() + "' and PackTypeID in (select PackTypeID from PackTypeLanguage where description='" + UOM + "')", db_vms);

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

                InCubeQuery PackQuery = new InCubeQuery(db_vms, "Select pack.PackID,pack.Quantity From Pack inner join item on pack.itemid = item.itemid Where item.itemcode='" + ItemCode.Trim() + "' and PackTypeID in (select PackTypeID from PackTypeLanguage where description='" + UOM + "')");
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

                        err = QueryBuilderObject.InsertQueryString("WarehouseStock", db_vms);

                        QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                        QueryBuilderObject.SetField("ZoneID", "1");
                        QueryBuilderObject.SetField("PackID", PackID);
                        QueryBuilderObject.SetField("ExpiryDate", "'" + ExpiryDate + "'");
                        QueryBuilderObject.SetField("StockDate", "'" + DateTime.Now.ToString("yyyy/MM/dd") + "'");
                        QueryBuilderObject.SetField("BatchNo", "'" + LOT + "'");

                        if (IsCase)
                        {
                            QueryBuilderObject.SetField("Quantity", FQcsc.ToString());
                        }
                        else
                        {
                            QueryBuilderObject.SetField("Quantity", FQpsc.ToString());
                        }
                        QueryBuilderObject.SetField("ProductionDate", "'" + DateTime.Now.ToString("yyyy/MM/dd") + "'");
                        QueryBuilderObject.SetField("SampleQuantity", "0");

                        err = QueryBuilderObject.InsertQueryString("DailyWarehouseStock", db_vms);
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
            WriteMessage("<<< STOCK " + WarehouseCode + "  Updated ,DeviceID  " + deviceserial + ">>> Total Updated = " + TOTALUPDATED);

            //ClassStartOFDay.StartofDay(db_vms, WarehouseID);

            #endregion
        }

        #endregion

        //        #region Update Discount

        //        public override void UpdateDiscount()
        //        {
        //            int TOTALUPDATED = 0;
        //            int TOTALINSERTED = 0;

        //            InCubeErrors err;
        //            object field = new object();

        //            if (!db_ERP.IsOpened())
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

        //            InCubeQuery CustomerQuery = new InCubeQuery(db_ERP, SelectCustomer);
        //            CustomerQuery.Execute();

        //            ClearProgress();
        //            SetProgressMax(CustomerQuery.GetDataTable().Rows.Count;

        //            err = CustomerQuery.FindFirst();

        //            while (err == InCubeErrors.Success)
        //            {

        //                ReportProgress();

        //                IntegrationForm.lblProgress.Text = "Updating Discounts " + " " + IntegrationForm.progressBar1.Value + " / " + IntegrationForm.progressBar1.Maximum;
        //                

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

        //        #endregion

        #region Update Route
        // Each customer is assigned to a route code (RM00101.SALSTERR) and each route code is assigned to a depot code (RM00303.COUNTRY).  Please refer to the following field definitions.  

        public override void UpdateRoutes()
        {
            try
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

                InCubeQuery RouteQuery = new InCubeQuery(db_ERP, SelectRoute);
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
                    if (CustomerID == "15893")
                    {

                    }
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

        #region SendInvoices

        public override void SendInvoices()
        {
            SerializeSalesOrderObjectSendInvoice(CoreGeneral.Common.StartupPath + "\\E-Connect\\", Filters.EmployeeID == -1, Filters.EmployeeID.ToString(),Filters.FromDate, Filters.ToDate);
        }

        public int SerializeSalesOrderObjectSendInvoice(string filename, bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate)
        {
            int ret = 0;
            string strDOC = "";
            string _DOCID = "";
            decimal TradmarkDiscount = 0;
            WriteMessage("\r\n" + "Sending Invoices");

            SOPTransactionType salesOrder = new SOPTransactionType();
            taSopHdrIvcInsert salesHdr = new taSopHdrIvcInsert();
            object TransactionID = "";
            object TransactionDate = "";
            object CustomerName = "";
            object CustomerCode = "";
            object WarehouseCode = "";
            object EmployeeCode = "";
            List<GP_JMGPInvdLINE> GPList = new List<GP_JMGPInvdLINE>();
            string STRPack = "";
            object CustomerID = "";
            object OutletID = "";
            object Customeraddress = "";
            object Customeraddress1 = "";
            object Customeraddress2 = "";
            object OutletCode = "";
            object salesTerritory = "";
            object tempGatePass = "";
            object LPONumber = "";
            object PromoDiscount = "";
            object NetTotal = "";
            object ExciseTax = "";
            string gatePassID = string.Empty;
            int FOCTypeID = 1;
            DateTime date;

            //gatePassID = gatePassID.Substring(0, gatePassID.Length - 1);
            string QueryString = @"SELECT     

            [Transaction].TransactionID, 
            [Transaction].TransactionDate, 
            CustomerOutlet.CustomerCode AS CustomerCode, 
            Warehouse.Barcode AS WarehouseCode, 
           
            Employee.EmployeeCode, 
            CustomerOutletLanguage.Description,
            CustomerOutlet.CustomerID,
            CustomerOutlet.OutletID,

            CustomerOutletLanguage.Address,
            CustomerOutlet.CustomerCode as OutletCode,
CustomerOutlet.StreetAddress, isnull(Wh.Barcode,'XXXXX') AS MainWH,
[Transaction].LPONumber,[Transaction].PromotedDiscount, [Transaction].NetTotal, [Transaction].ExciseTax

            FROM         [Transaction] INNER JOIN
            CustomerOutletLanguage ON [Transaction].CustomerID = CustomerOutletLanguage.CustomerID INNER JOIN
            CustomerOutlet ON [Transaction].CustomerID = CustomerOutlet.CustomerID AND [Transaction].OutletID = CustomerOutlet.OutletID AND 
            CustomerOutletLanguage.CustomerID = CustomerOutlet.CustomerID AND CustomerOutletLanguage.OutletID = CustomerOutlet.OutletID INNER JOIN
           Warehouse ON [Transaction].WarehouseID = Warehouse.WarehouseID left outer JOIN
            VehicleLoadingWh vl on [Transaction].WarehouseID =vl.VehicleID
            left outer join Warehouse Wh on Wh.WarehouseID=vl.WarehouseID and Wh.WarehouseTypeID=1 inner join 
            Employee ON [Transaction].EmployeeID = Employee.EmployeeID 
              WHERE ([Transaction].Synchronized = 0)  AND 

           ([Transaction].Voided = 0) AND ([Transaction].TransactionTypeID = 1 or [Transaction].TransactionTypeID = 3) AND 
            ([Transaction].TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
            AND [Transaction].TransactionDate <= '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + "')";

            if (!AllSalespersons)
            {
                QueryString += " AND [Transaction].EmployeeID = " + Salesperson;
            }

            InCubeQuery GetSalesTransactionInformation = new InCubeQuery(db_vms, QueryString);

            err = GetSalesTransactionInformation.Execute();
            err = GetSalesTransactionInformation.FindFirst();
            while (err == InCubeErrors.Success)
            {
                try
                {
                    Customeraddress = "";
                    Customeraddress1 = "";
                    Customeraddress2 = "";
                    object field = new object();
                    decimal TOTAL = 0;
                    int LINSEQ = 0;

                    #region Get SalesTransaction Information
                    {
                        err = GetSalesTransactionInformation.GetField(0, ref TransactionID);
                        err = GetSalesTransactionInformation.GetField(1, ref TransactionDate);
                        err = GetSalesTransactionInformation.GetField(2, ref CustomerCode);
                        err = GetSalesTransactionInformation.GetField(4, ref EmployeeCode);
                        err = GetSalesTransactionInformation.GetField(5, ref CustomerName);
                        err = GetSalesTransactionInformation.GetField(6, ref CustomerID);
                        err = GetSalesTransactionInformation.GetField(7, ref OutletID);
                        err = GetSalesTransactionInformation.GetField(8, ref Customeraddress);
                        err = GetSalesTransactionInformation.GetField(9, ref OutletCode);
                        err = GetSalesTransactionInformation.GetField(10, ref salesTerritory);
                        err = GetSalesTransactionInformation.GetField(11, ref WarehouseCode);
                        err = GetSalesTransactionInformation.GetField(12, ref LPONumber);
                        err = GetSalesTransactionInformation.GetField(13, ref PromoDiscount);
                        err = GetSalesTransactionInformation.GetField(14, ref NetTotal);
                        err = GetSalesTransactionInformation.GetField(15, ref ExciseTax);
                    }
                    #endregion

                    string gatePassNumber = string.Format("select top(1) BatchNo from TransactionDetail where TransactionID='" + TransactionID.ToString().Trim() + "'");
                    InCubeQuery gatePassQuery = new InCubeQuery(db_vms, gatePassNumber);

                    err = gatePassQuery.Execute();
                    err = gatePassQuery.FindFirst();
                    while (err == InCubeErrors.Success)
                    {
                        gatePassQuery.GetField(0, ref tempGatePass);
                        gatePassID = tempGatePass.ToString().Trim();
                        //gatePassID = "1990/01/01";// 10-12-2012 ADDED AFTER BAIJUS EMAIL ABOUT THE BATCH NUMBE GETTING CHANGED .
                        err = gatePassQuery.FindNext();
                    }
                    GetAddress(CustomerCode.ToString(), ref Customeraddress, ref Customeraddress1, ref Customeraddress2);
                    date = DateTime.Parse(TransactionDate.ToString());

                    string CustomerType = GetFieldValue("Customeroutlet", "CustomerTypeID", " CustomerID = " + CustomerID.ToString() + " AND OutletID = " + OutletID.ToString(), db_vms);
                    if (CustomerType.ToLower() == "1")
                    {
                        //SLPRSNID = "CASH";
                        _DOCID = "CASHCUSTOMER";
                    }
                    else if (CustomerType.ToLower() == "2")
                    {
                        //SLPRSNID = "CREDIT";
                        _DOCID = "CREDIT CUSTOMER";
                    }
                    else if (CustomerType.ToLower() == "4")
                    {
                        _DOCID = "CORPORATE";
                    }

                    //string COUNTRY = GetFieldValue("RM00303", "COUNTRY", " SALSTERR = '" + EmployeeCode + "'", db_ERP); from 
                    string COUNTRY = GetFieldValue("IV40700", "STATE", " rtrim(ltrim(LOCNCODE)) = '" + EmployeeCode + "'", db_ERP);
                    strDOC = COUNTRY.Trim();

                    QueryString = @"
Select Top 1 SHIPMTHD,'','','','' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'', '' from RM00101
Where SHIPMTHD <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '', CITY,'','','' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' , '' from RM00101
Where CITY <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','', STATE,'','' ,'' ,'' ,'' ,'' ,'' ,'' ,'','' ,'' ,'' ,'' ,'' from RM00101
Where STATE <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','', ZIP, ''  ,'' ,'' ,'' ,'' ,'' ,'' ,'','' ,'' ,'' ,'' ,'' from RM00101
Where ZIP <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','', COUNTRY , '','' ,'' ,'' ,'' ,'' ,'','' ,'' ,'' ,'' ,'' from RM00101
Where COUNTRY <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' , CNTCPRSN,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,''from RM00101
Where CNTCPRSN <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' , PHONE1 ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' from RM00101
Where PHONE1 <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' , PHONE2,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' from RM00101
Where PHONE2 <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' ,'' , PHONE3,'' ,'' ,'' ,'' ,'' ,'', '' ,'' from RM00101
Where PHONE3 <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' ,'' ,'' , FAX,'' ,'' ,'' ,'' ,'', '', '' from RM00101
Where FAX <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' ,'' ,'' ,'' , SLPRSNID ,'' ,'' ,'' ,'', '', '' from RM00101
Where  SLPRSNID = '" + EmployeeCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' ,'' ,'' ,'' ,'' , TAXSCHID ,'' ,'' ,'', '', '' from RM00101
Where TAXSCHID <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' ,'' ,'' ,'' ,'' , '', PRBTADCD ,'' ,'', '', '' from RM00101
Where PRBTADCD <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' ,'' ,'' ,'' ,'' , '', '', PRSTADCD, '', '', '' from RM00101
Where PRSTADCD <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' ,'' ,'' ,'' ,'' , '', '', '', TAXEXMT1 , '', '' from RM00101
Where TAXEXMT1 <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' ,'' ,'' ,'' ,'' , '', '', '', '', TAXEXMT2 ,'' from RM00101
Where TAXEXMT2 <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' ,'' ,'' ,'' ,'' , '', '', '', '', '', TXRGNNUM from RM00101
Where TXRGNNUM <> '' AND CUSTNMBR = '" + CustomerCode + @"'
";

                    InCubeQuery AdditionalInfo = new InCubeQuery(db_ERP, QueryString);
                    AdditionalInfo.Execute();

                    DataTable AdditionalInfoTbl = AdditionalInfo.GetDataTable();
                    AdditionalInfo.Close();

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
TransactionDetail.FOCTypeID

FROM TransactionDetail INNER JOIN
Pack ON TransactionDetail.PackID = Pack.PackID INNER JOIN
ItemLanguage ON Pack.ItemID = ItemLanguage.ItemID INNER JOIN
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID INNER JOIN
Item ON ItemLanguage.ItemID = Item.ItemID
WHERE (PackTypeLanguage.LanguageID = 1) AND (ItemLanguage.LanguageID = 1) AND (TransactionDetail.TransactionID = '" + TransactionID.ToString() + "')";


                    InCubeQuery dtlQry = new InCubeQuery(db_vms, dtlQryStr);
                    err = dtlQry.Execute();
                    DataRow[] detailsList = dtlQry.GetDataTable().Select();

                    dtlQry = new InCubeQuery(db_vms, "SELECT PBH.PACKID, POD.VALUE AS PromValue, (POD.VALUE + POD.RANGE) As PromRange FROM PROMOTIONBENEFITHISTORY PBH INNER JOIN PROMOTIONOPTIONDETAIL POD ON POD.PROMOTIONID = PBH.PROMOTIONID AND(POD.REFERENCEOPTIONID = PBH.PROMOTIONOPTIONID) WHERE TRANSACTIONID = '" + TransactionID.ToString() + "' ");
                    err = dtlQry.Execute();
                    DataTable dtPromDetails = dtlQry.GetDataTable();

                    int count = detailsList.Length;
                    int[] detailsListLNITMSEQ = new int[count];
                    List<taSopLineIvcInsert_ItemsTaSopLineIvcInsert> LineItems = new List<taSopLineIvcInsert_ItemsTaSopLineIvcInsert>();

                    ClearProgress();
                    SetProgressMax(count);


                    if (count == 0)
                    {
                        throw new Exception("No details found , Invoice Number = " + TransactionID.ToString());
                    }

                    GPList = new List<GP_JMGPInvdLINE>();
                    GP_JMGPInvdLINE JMGP;

                    string SelectTaxSCHID = @"SELECT TAXSCHID FROM RM00101 WHERE CUSTNMBR ='" + salesHdr.CUSTNMBR + "'";
                    InCubeQuery TaxQuery = new InCubeQuery(db_ERP, SelectTaxSCHID);
                    err = TaxQuery.Execute();
                    err = TaxQuery.FindFirst();
                    TaxQuery.GetField("TAXSCHID", ref field);
                    string TAXSCHID = field.ToString().Trim();
                    //salesHdr.TAXSCHID = TAXSCHID;

                    for (int i = 0; i < count; i++)
                    {
                        ReportProgress("Sending Invoices");

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
                        decimal PromValue = 0;
                        decimal PromRange = 0;
                        int PackID = int.Parse(salesTxRow["PackID"].ToString());
                        DataRow[] drPromDetail = dtPromDetails.Select("PackID = " + PackID);
                        if (drPromDetail.Length > 0)
                        {
                            //Read prom details
                            PromValue = decimal.Parse(drPromDetail[0]["PromValue"].ToString());
                            PromRange = decimal.Parse(drPromDetail[0]["PromRange"].ToString());
                        }

                        #region line items
                        taSopLineIvcInsert_ItemsTaSopLineIvcInsert salesLine = new taSopLineIvcInsert_ItemsTaSopLineIvcInsert();
                        LINSEQ = LINSEQ + 16384;
                        salesLine.ITEMNMBR = ItemCode;
                        salesLine.UNITCOST = 0;
                        salesLine.UNITCOSTSpecified = false;
                        salesLine.NONINVEN = 0;
                        salesLine.ADDRESS1 = Customeraddress.ToString().Trim();
                        salesLine.ADDRESS2 = Customeraddress1.ToString().Trim();
                        salesLine.ADDRESS3 = Customeraddress2.ToString().Trim();
                        salesLine.CUSTNMBR = CustomerCode.ToString().Trim();
                        //Add Promotion Slabs
                        salesLine.COMMENT_1 = PromValue.ToString("#.##").Trim() + " - " + PromRange.ToString("#.##").Trim();

                        salesLine.SALSTERR = salesTerritory.ToString().Trim();
                        salesLine.SLPRSNID = EmployeeCode.ToString().Trim();

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
                        salesLine.ALLOCATE = 0;
                        salesLine.AUTOALLOCATESERIAL = 0;
                        salesLine.QTYFULFI = 0;

                        salesLine.QTYFULFISpecified = true;
                        salesLine.QTYFULFI = Quantity;
                        salesLine.QUANTITY = Math.Floor(Quantity * 100) / 100;
                        salesLine.SHIPMTHD = "DELIVERY";
                        //ItemCode
                        string SelectItem = @"SELECT 
                                           ITMTSHID
                                           
                                           FROM IV00101 I";


                        SelectItem += " WHERE I.ITEMTYPE<>2 and ITEMNMBR like 'FG%' and I.ITEMNMBR ='" + ItemCode + "'";
                        InCubeQuery ItemQuery = new InCubeQuery(db_ERP, SelectItem);
                        err = ItemQuery.Execute();
                        err = ItemQuery.FindFirst();
                        ItemQuery.GetField("ITMTSHID", ref field);
                        string ITMTSHID = field.ToString().Trim();
                        salesLine.ITMTSHID = ITMTSHID;

                        #region price and discount

                        //Discount = Math.Round(Discount, 2, MidpointRounding.AwayFromZero);
                        Discount = Math.Round(Discount / Quantity, 2, MidpointRounding.AwayFromZero);
                        //Discount = Discount / Quantity;
                        //unitPrice = Math.Round(unitPrice, 3, MidpointRounding.AwayFromZero);

                        //decimal XTNDPRCE = Math.Round(Quantity * unitPrice, 2, MidpointRounding.AwayFromZero) - (Discount * Quantity);
                        //decimal XTNDPRCE = Math.Round((Math.Floor(unitPrice * 100) / 100) - (Math.Floor(Discount * 100) / 100), 2, MidpointRounding.AwayFromZero) * (Math.Floor(Quantity * 100) / 100);
                        //decimal XTNDPRCE = Math.Round((unitPrice - Discount) * (Quantity), 2, MidpointRounding.AwayFromZero) ;
                        decimal XTNDPRCE = (unitPrice - Discount) * (Quantity);

                        salesLine.UNITPRCE = Math.Round(unitPrice, 3, MidpointRounding.AwayFromZero);
                        TOTAL += (XTNDPRCE);
                        salesLine.XTNDPRCE = XTNDPRCE;
                        salesLine.MRKDNAMTSpecified = true;
                        salesLine.MRKDNAMT = Discount;
                        #endregion


                        LineItems.Add(salesLine);
                        #endregion


                        JMGP = new GP_JMGPInvdLINE();
                        JMGP.TransactionID = TransactionID.ToString().Trim();
                        JMGP.ItemNumber = ItemCode;
                        JMGP.GatePass = gatePassID;
                        JMGP.Quantity = Quantity;

                        GPList.Add(JMGP);

                    }

                    #region invoice header
                    salesHdr.DEFPRICING = 1;
                    salesHdr.SOPTYPE = 3;
                    salesHdr.SOPNUMBE = TransactionID.ToString().Trim();
                    salesHdr.DOCDATE = date.ToString("yyyy-MM-dd");
                    salesHdr.CUSTNMBR = CustomerCode.ToString().Trim();
                    salesHdr.CUSTNAME = CustomerName.ToString().Trim();
                    salesHdr.PYMTRMID = GetPaytrmID(CustomerCode.ToString()).Trim();
                    salesHdr.REFRENCE = gatePassID;
                    salesHdr.SALSTERR = salesTerritory.ToString().Substring(0, 3);
                    salesHdr.CSTPONBR = LPONumber.ToString();
                    salesHdr.BACHNUMB = gatePassID;
                    salesHdr.SUBTOTAL = TOTAL;

                    // decimal DOCAMT = Math.Round(TOTAL - TradmarkDiscount - decimal.Parse(PromoDiscount.ToString()) , 2, MidpointRounding.AwayFromZero);
                    //salesHdr.DOCAMNT = Math.Floor(DOCAMT * 100) / 100;
                    //decimal DOCAMT = Math.Floor((TOTAL - TradmarkDiscount - decimal.Parse(PromoDiscount.ToString()))* 100) / 100;
                    decimal DOCAMT = TOTAL - TradmarkDiscount - decimal.Parse(PromoDiscount.ToString());
                    salesHdr.DOCAMNT = DOCAMT;
                    DOCAMT = DOCAMT + Convert.ToDecimal(ExciseTax);
                    //salesHdr.DOCAMNT = TOTAL - TradmarkDiscount - decimal.Parse(PromoDiscount.ToString());

                    //Checking Difference between InVan and GP
                    decimal TotalNetAmount = Convert.ToDecimal(NetTotal);
                    decimal FinalValue = (DOCAMT + (DOCAMT * 5) / 100);
                    decimal Diff = TotalNetAmount - FinalValue;
                    //salesHdr.MISCAMNT = Math.Abs(Diff);
                    if (Diff > 0)
                    {
                        salesHdr.MISCAMNT = Math.Abs(Diff);
                        //salesHdr.TRDISAMT = decimal.Parse(PromoDiscount.ToString()) + TradmarkDiscount;
                    }
                    else
                    {
                        salesHdr.MISCAMNT = Diff;
                        //salesHdr.TRDISAMT = decimal.Parse(PromoDiscount.ToString()) + TradmarkDiscount + Math.Abs(Diff);
                    }

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
                    salesHdr.ADDRESS1 = Customeraddress.ToString().Trim();
                    salesHdr.ADDRESS2 = Customeraddress1.ToString().Trim();
                    salesHdr.ADDRESS3 = Customeraddress2.ToString().Trim();
                    salesHdr.TRDISAMTSpecified = true;
                    salesHdr.TRDISAMT = decimal.Parse(PromoDiscount.ToString()) + TradmarkDiscount;
                    salesHdr.SHIPMTHD = "DELIVERY";
                    salesHdr.CITY = "";
                    salesHdr.STATE = "";
                    salesHdr.ZIPCODE = "";
                    salesHdr.COUNTRY = "";
                    salesHdr.CNTCPRSN = "";
                    salesHdr.PHNUMBR1 = "";
                    salesHdr.PHNUMBR2 = "";
                    salesHdr.PHNUMBR3 = "";
                    salesHdr.FAXNUMBR = "";
                    salesHdr.SLPRSNID = EmployeeCode.ToString().Trim();
                    salesHdr.CREATETAXES = 1;
                    salesHdr.DEFTAXSCHDS = 1;

                    string FRMSValues = @"SELECT FRTSCHID, MSCSCHID FROM SOP40100";
                    InCubeQuery FRMSQuery = new InCubeQuery(db_ERP, FRMSValues);
                    err = FRMSQuery.Execute();
                    err = FRMSQuery.FindFirst();
                    FRMSQuery.GetField("FRTSCHID", ref field);
                    string FRTSCHID = field.ToString().Trim();
                    salesHdr.FRTSCHID = FRTSCHID;
                    FRMSQuery.GetField("MSCSCHID", ref field);
                    string MSCSCHID = field.ToString().Trim();
                    salesHdr.MSCSCHID = MSCSCHID;
                    salesHdr.FREIGTBLE = 0;
                    salesHdr.MISCTBLE = 0;

                    try
                    {
                        foreach (DataRow row in AdditionalInfoTbl.Rows)
                        {
                            if (row[0].ToString() != string.Empty)
                            {
                                salesHdr.SHIPMTHD = row[0].ToString();
                            }
                            if (row[1].ToString() != string.Empty)
                            {
                                salesHdr.CITY = row[1].ToString();
                            }
                            if (row[2].ToString() != string.Empty)
                            {
                                salesHdr.STATE = row[2].ToString();
                            }
                            if (row[3].ToString() != string.Empty)
                            {
                                salesHdr.ZIPCODE = row[3].ToString();
                            }
                            if (row[4].ToString() != string.Empty)
                            {
                                salesHdr.COUNTRY = row[4].ToString();
                            }
                            if (row[5].ToString() != string.Empty)
                            {
                                salesHdr.CNTCPRSN = row[5].ToString();
                            }
                            if (row[6].ToString() != string.Empty)
                            {
                                salesHdr.PHNUMBR1 = row[6].ToString();
                            }
                            if (row[7].ToString() != string.Empty)
                            {
                                salesHdr.PHNUMBR2 = row[7].ToString();
                            }
                            if (row[8].ToString() != string.Empty)
                            {
                                salesHdr.PHNUMBR3 = row[8].ToString();
                            }
                            if (row[9].ToString() != string.Empty)
                            {
                                salesHdr.FAXNUMBR = row[9].ToString();
                            }
                            if (row[10].ToString() != string.Empty)
                            {
                                salesHdr.SLPRSNID = EmployeeCode.ToString().Trim();
                            }
                            if (row[11].ToString() != string.Empty)
                            {
                                salesHdr.TAXSCHID = row[11].ToString();
                            }
                            if (row[12].ToString() != string.Empty)
                            {
                                salesHdr.PRBTADCD = row[12].ToString();
                            }
                            if (row[13].ToString() != string.Empty)
                            {
                                salesHdr.PRSTADCD = row[13].ToString();
                            }
                            if (row[14].ToString() != string.Empty)
                            {
                                salesHdr.TAXEXMT1 = row[14].ToString();
                            }
                            if (row[15].ToString() != string.Empty)
                            {
                                salesHdr.TAXEXMT2 = row[15].ToString();
                            }
                            if (row[16].ToString() != string.Empty)
                            {
                                salesHdr.TXRGNNUM = row[16].ToString();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        StreamWriter writer2 = new StreamWriter("errorInv.log", true);
                        writer2.Write(ex.ToString());
                        writer2.Close();
                    }

                    //salesHdr.CITY = GetFieldValue("RM00101", "CITY", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND CITY <> ''", db_ERP);
                    //salesHdr.STATE = GetFieldValue("RM00101", "STATE", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND STATE <> ''", db_ERP);
                    //salesHdr.ZIPCODE = GetFieldValue("RM00101", "ZIP", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND ZIP <> ''", db_ERP);
                    //salesHdr.COUNTRY = GetFieldValue("RM00101", "COUNTRY", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND COUNTRY <> ''", db_ERP);
                    //salesHdr.CNTCPRSN = GetFieldValue("RM00101", "CNTCPRSN", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND CNTCPRSN <> ''", db_ERP);
                    //salesHdr.PHNUMBR1 = GetFieldValue("RM00101", "PHONE1", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND PHONE1 <> ''", db_ERP);
                    //salesHdr.PHNUMBR2 = GetFieldValue("RM00101", "PHONE2", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND PHONE2 <> ''", db_ERP);
                    //salesHdr.PHNUMBR3 = GetFieldValue("RM00101", "PHONE3", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND PHONE3 <> ''", db_ERP);
                    //salesHdr.FAXNUMBR = GetFieldValue("RM00101", "FAX", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND FAX <> ''", db_ERP);
                    //salesHdr.SLPRSNID = GetFieldValue("RM00101", "SLPRSNID", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND (SLPRSNID = 'CREDIT' or SLPRSNID = 'CASH')", db_ERP);
                    //salesHdr.TAXSCHID = GetFieldValue("RM00101", "TAXSCHID", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND TAXSCHID <> ''", db_ERP);

                    #endregion




                    salesOrder.taSopLineIvcInsert_Items = LineItems.ToArray();
                    salesOrder.taSopHdrIvcInsert = salesHdr;

                    eConnectType eConnect = new eConnectType();
                    SOPTransactionType[] MySopTransactionType = { salesOrder };
                    eConnect.SOPTransactionType = MySopTransactionType;
                    string salesOrderDocument;
                    #region Create xml file
                    {
                        string fname = filename + TransactionID.ToString().Trim() + ".xml";
                        FileStream fs = new FileStream(fname, FileMode.Create);
                        XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                        XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                        serializer.Serialize(writer, eConnect);
                        writer.Close();

                        XmlDocument xmldoc = new XmlDocument();
                        xmldoc.Load(fname);
                        salesOrderDocument = xmldoc.OuterXml;
                    }
                    #endregion
                    eConnectMethods eConCall = new eConnectMethods();


                    #region  Send xml file to GPs

                    eConCall.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, salesOrderDocument, EnumTypes.SchemaValidationType.None, "");

                    #endregion

                    InCubeQuery UpdateQuery = new InCubeQuery(db_vms, "Update [Transaction] SET Synchronized = 1 where TransactionID = '" + TransactionID.ToString() + "'");
                    err = UpdateQuery.Execute();

                    //This query to set SALSTERR and SLPRSNID since sending to GP doesnt affect those fields
                    UpdateQuery = new InCubeQuery(db_ERP, "Update SOP10200 SET SALSTERR = '" + salesTerritory.ToString() + "', SLPRSNID = '" + EmployeeCode.ToString().Trim() + "' where  SOPNUMBE = '" + TransactionID.ToString() + "' AND SOPTYPE = 3");
                    err = UpdateQuery.Execute();
                    InCubeQuery StockGPp;
                    string check = string.Empty;
                    string insertStockGP = string.Empty;
                    string updateStockGP = string.Empty;
                    object obj = "";
                    foreach (GP_JMGPInvdLINE strr in GPList)
                    {
                        check = string.Format("select SOPNUMBE from JMGPInvdLINE where SOPNUMBE='{0}' and ITEMNMBR='{1}'", strr.TransactionID, strr.ItemNumber);
                        StockGPp = new InCubeQuery(check, db_ERP);
                        StockGPp.ExecuteScalar(ref obj);
                        if (obj == null || obj.ToString().Trim().Equals(string.Empty))
                        {
                            insertStockGP = string.Format("INSERT INTO JMGPInvdLINE values('{0}','{1}','{2}',{3})", strr.TransactionID, strr.ItemNumber, strr.GatePass, strr.Quantity);
                            StockGPp = new InCubeQuery(insertStockGP, db_ERP);
                            StockGPp.ExecuteNonQuery();
                        }
                        else
                        {
                            updateStockGP = string.Format("update JMGPInvdLINE set QTYINVCD=QTYINVCD+{0} where SOPNUMBE='{1}' and ITEMNMBR='{2}'", strr.Quantity, strr.TransactionID, strr.ItemNumber);
                            StockGPp = new InCubeQuery(updateStockGP, db_ERP);
                            err = StockGPp.ExecuteNonQuery();
                        }

                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\n" + strr.TransactionID.ToString() + " - JMGPInvdLINE Table error");
                        }
                    }
                    //err = DatabaseSpecialFunctions.RunStoredProcedure(db_vms, "spHandleDocumentSequence");
                    WriteMessage("\r\n" + TransactionID.ToString() + " - OK");
                    StreamWriter wrt = new StreamWriter("errorInv.log", true);
                    wrt.Write("\n" + TransactionID.ToString() + " OK\r\n");
                    wrt.Close();

                }
                catch (Exception ex)
                {
                    StreamWriter wrt = new StreamWriter("errorInv.log", true);
                    wrt.Write(ex.ToString());
                    wrt.Close();
                    WriteMessage("\r\n" + TransactionID.ToString() + " - FAILED!");
                    ret++;
                }
                err = GetSalesTransactionInformation.FindNext();
            }
            return ret;
        }

        #endregion

        #region 


        public override void SendReturn()
        {
            SerializeSalesOrderObjectSendTransactionReturn(CoreGeneral.Common.StartupPath + "\\E-Connect\\", Filters.EmployeeID == -1, Filters.EmployeeID.ToString(), Filters.FromDate, Filters.ToDate);
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
            DateTime date;
            string SLPRSNID = "";

            string QueryString = @"SELECT     

            [Transaction].TransactionID, 
            [Transaction].TransactionDate, 
            CustomerOutlet.CustomerCode AS CustomerCode, 
            Warehouse.Barcode AS WarehouseCode, 
           
            Employee.EmployeeCode, 
            CustomerOutletLanguage.Description,
            CustomerOutlet.CustomerID,
            CustomerOutlet.OutletID,

            CustomerOutletLanguage.Address,
            CustomerOutlet.CustomerCode as OutletCode, isnull(Wh.Barcode,'XXXXX') AS MainWH
            FROM         [Transaction] INNER JOIN
            CustomerOutletLanguage ON [Transaction].CustomerID = CustomerOutletLanguage.CustomerID INNER JOIN
            CustomerOutlet ON [Transaction].CustomerID = CustomerOutlet.CustomerID AND [Transaction].OutletID = CustomerOutlet.OutletID AND 
            CustomerOutletLanguage.CustomerID = CustomerOutlet.CustomerID AND CustomerOutletLanguage.OutletID = CustomerOutlet.OutletID INNER JOIN
           Warehouse ON [Transaction].WarehouseID = Warehouse.WarehouseID left outer JOIN
            VehicleLoadingWh vl on [Transaction].WarehouseID =vl.VehicleID
            left outer join Warehouse Wh on Wh.WarehouseID=vl.WarehouseID and Wh.WarehouseTypeID=1 inner join 
            Employee ON [Transaction].EmployeeID = Employee.EmployeeID 
            WHERE ([Transaction].Synchronized = 0) AND ([Transaction].TransactionTypeID = 2 or [Transaction].TransactionTypeID = 4) AND 
            ([Transaction].TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
            AND [Transaction].TransactionDate <= '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + "')";

            if (!AllSalespersons)
            {
                QueryString += " AND [Transaction].EmployeeID = " + Salesperson;
            }

            InCubeQuery GetSalesTransactionInformation = new InCubeQuery(db_vms, QueryString);

            err = GetSalesTransactionInformation.Execute();
            err = GetSalesTransactionInformation.FindFirst();
            while (err == InCubeErrors.Success)
            {
                object field = new object();
                decimal TOTAL = 0;
                int LINTMSEQ = 0;

                #region  Get SalesTransaction Information
                {
                    err = GetSalesTransactionInformation.GetField(0, ref TransactionID);
                    err = GetSalesTransactionInformation.GetField(1, ref TransactionDate);
                    err = GetSalesTransactionInformation.GetField(2, ref CustomerCode);
                    err = GetSalesTransactionInformation.GetField(4, ref EmployeeCode);
                    err = GetSalesTransactionInformation.GetField(5, ref CustomerName);
                    err = GetSalesTransactionInformation.GetField(6, ref CustomerID);
                    err = GetSalesTransactionInformation.GetField(7, ref OutletID);
                    err = GetSalesTransactionInformation.GetField(8, ref CustomerAddress);
                    err = GetSalesTransactionInformation.GetField(9, ref OutletCode);
                    err = GetSalesTransactionInformation.GetField(10, ref WarehouseCode);
                }
                #endregion

                try
                {
                    GetAddress(CustomerCode.ToString(), ref CustomerAddress, ref CustomerAddress1, ref CustomerAddress2);
                    date = DateTime.Parse(TransactionDate.ToString());

                    salesHdr.SOPTYPE = 4;

                    salesHdr.SOPNUMBE = TransactionID.ToString();

                    salesHdr.SLPRSNID = EmployeeCode.ToString();

                    salesHdr.DOCDATE = date.ToString("dd-MM-yyyy");
                    salesHdr.CUSTNMBR = CustomerCode.ToString();
                    CustomerName = CustomerName.ToString().Split('-')[0];
                    salesHdr.CUSTNAME = CustomerName.ToString();
                    int CustomerIDInt = int.Parse(CustomerID.ToString());
                    int OutletIDInt = int.Parse(OutletID.ToString());

                    string CustomerType = GetFieldValue("Customeroutlet", "CustomerTypeID", " CustomerID = " + CustomerID.ToString() + " AND OutletID = " + OutletID.ToString(), db_vms);
                    if (CustomerType.ToLower() == "1")
                    {
                        SLPRSNID = "CASH";
                        _DOCID = "RCS";
                    }
                    else if (CustomerType.ToLower() == "2")
                    {
                        SLPRSNID = "CREDIT";
                        _DOCID = "RCR";
                    }

                    //string COUNTRY = GetFieldValue("RM00303", "COUNTRY", " SALSTERR = '" + EmployeeCode + "'", db_ERP);
                    string COUNTRY = GetFieldValue("IV40700", "STATE", " rtrim(ltrim(LOCNCODE)) = '" + EmployeeCode + "'", db_ERP);
                    strDOC = COUNTRY.Trim();

                    QueryString = @"
Select Top 1 SHIPMTHD,'','','','' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'', '' from RM00101
Where SHIPMTHD <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '', CITY,'','','' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' , '' from RM00101
Where CITY <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','', STATE,'','' ,'' ,'' ,'' ,'' ,'' ,'' ,'','' ,'' ,'' ,'' ,'' from RM00101
Where STATE <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','', ZIP, ''  ,'' ,'' ,'' ,'' ,'' ,'' ,'','' ,'' ,'' ,'' ,'' from RM00101
Where ZIP <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','', COUNTRY , '','' ,'' ,'' ,'' ,'' ,'','' ,'' ,'' ,'' ,'' from RM00101
Where COUNTRY <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' , CNTCPRSN,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,''from RM00101
Where CNTCPRSN <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' , PHONE1 ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' from RM00101
Where PHONE1 <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' , PHONE2,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' ,'' from RM00101
Where PHONE2 <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' ,'' , PHONE3,'' ,'' ,'' ,'' ,'' ,'', '' ,'' from RM00101
Where PHONE3 <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' ,'' ,'' , FAX,'' ,'' ,'' ,'' ,'', '', '' from RM00101
Where FAX <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' ,'' ,'' ,'' , SLPRSNID ,'' ,'' ,'' ,'', '', '' from RM00101
Where  SLPRSNID = '" + EmployeeCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' ,'' ,'' ,'' ,'' , TAXSCHID ,'' ,'' ,'', '', '' from RM00101
Where TAXSCHID <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' ,'' ,'' ,'' ,'' , '', PRBTADCD ,'' ,'', '', '' from RM00101
Where PRBTADCD <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' ,'' ,'' ,'' ,'' , '', '', PRSTADCD, '', '', '' from RM00101
Where PRSTADCD <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' ,'' ,'' ,'' ,'' , '', '', '', TAXEXMT1 , '', '' from RM00101
Where TAXEXMT1 <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' ,'' ,'' ,'' ,'' , '', '', '', '', TAXEXMT2 ,'' from RM00101
Where TAXEXMT2 <> '' AND CUSTNMBR = '" + CustomerCode + @"'
Union
Select Top 1 '','','','','' ,'' ,'' ,'' ,'' ,'' ,'' , '', '', '', '', '', TXRGNNUM from RM00101
Where TXRGNNUM <> '' AND CUSTNMBR = '" + CustomerCode + @"'";

                    InCubeQuery AdditionalInfo = new InCubeQuery(db_ERP, QueryString);
                    AdditionalInfo.Execute();

                    DataTable AdditionalInfoTbl = AdditionalInfo.GetDataTable();
                    AdditionalInfo.Close();

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
                    salesHdr.SLPRSNID = "";
                    salesHdr.TAXSCHID = "";
                    salesHdr.CREATETAXES = 1;
                    salesHdr.DEFTAXSCHDS = 1;
                    salesHdr.FREIGTBLE = 0;
                    salesHdr.MISCTBLE = 0;

                    try
                    {
                        foreach (DataRow row in AdditionalInfoTbl.Rows)
                        {
                            if (row[0].ToString() != string.Empty)
                            {
                                salesHdr.SHIPMTHD = row[0].ToString();
                            }
                            if (row[1].ToString() != string.Empty)
                            {
                                salesHdr.CITY = row[1].ToString();
                            }
                            if (row[2].ToString() != string.Empty)
                            {
                                salesHdr.STATE = row[2].ToString();
                            }
                            if (row[3].ToString() != string.Empty)
                            {
                                salesHdr.ZIPCODE = row[3].ToString();
                            }
                            if (row[4].ToString() != string.Empty)
                            {
                                salesHdr.COUNTRY = row[4].ToString();
                            }
                            if (row[5].ToString() != string.Empty)
                            {
                                salesHdr.CNTCPRSN = row[5].ToString();
                            }
                            if (row[6].ToString() != string.Empty)
                            {
                                salesHdr.PHNUMBR1 = row[6].ToString();
                            }
                            if (row[7].ToString() != string.Empty)
                            {
                                salesHdr.PHNUMBR2 = row[7].ToString();
                            }
                            if (row[8].ToString() != string.Empty)
                            {
                                salesHdr.PHNUMBR3 = row[8].ToString();
                            }
                            if (row[9].ToString() != string.Empty)
                            {
                                salesHdr.FAXNUMBR = row[9].ToString();
                            }
                            if (row[10].ToString() != string.Empty)
                            {
                                salesHdr.SLPRSNID = row[10].ToString();
                            }
                            if (row[11].ToString() != string.Empty)
                            {
                                salesHdr.TAXSCHID = row[11].ToString();
                            }
                            if (row[12].ToString() != string.Empty)
                            {
                                salesHdr.PRBTADCD = row[12].ToString();
                            }
                            if (row[13].ToString() != string.Empty)
                            {
                                salesHdr.PRSTADCD = row[13].ToString();
                            }
                            if (row[14].ToString() != string.Empty)
                            {
                                salesHdr.TAXEXMT1 = row[14].ToString();
                            }
                            if (row[15].ToString() != string.Empty)
                            {
                                salesHdr.TAXEXMT2 = row[15].ToString();
                            }
                            if (row[16].ToString() != string.Empty)
                            {
                                salesHdr.TXRGNNUM = row[16].ToString();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        StreamWriter writer2 = new StreamWriter("errorInv.log", true);
                        writer2.Write(ex.ToString());
                        writer2.Close();
                    }

                    salesHdr.SALSTERR = EmployeeCode.ToString();
                    salesHdr.ADDRESS1 = CustomerAddress.ToString();
                    salesHdr.ADDRESS2 = CustomerAddress1.ToString();
                    salesHdr.ADDRESS3 = CustomerAddress2.ToString();
                    salesHdr.PYMTRMID = GetPaytrmID(CustomerCode.ToString()).Trim();

                    //salesHdr.CITY = GetFieldValue("RM00101", "CITY", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND CITY <> ''", db_ERP);
                    //salesHdr.STATE = GetFieldValue("RM00101", "STATE", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND STATE <> ''", db_ERP);
                    //salesHdr.ZIPCODE = GetFieldValue("RM00101", "ZIP", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND ZIP <> ''", db_ERP);
                    //salesHdr.COUNTRY = GetFieldValue("RM00101", "COUNTRY", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND COUNTRY <> ''", db_ERP);
                    //salesHdr.CNTCPRSN = GetFieldValue("RM00101", "CNTCPRSN", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND CNTCPRSN <> ''", db_ERP);
                    //salesHdr.PHNUMBR1 = GetFieldValue("RM00101", "PHONE1", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND PHONE1 <> ''", db_ERP);
                    //salesHdr.PHNUMBR2 = GetFieldValue("RM00101", "PHONE2", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND PHONE2 <> ''", db_ERP);
                    //salesHdr.PHNUMBR3 = GetFieldValue("RM00101", "PHONE3", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND PHONE3 <> ''", db_ERP);
                    //salesHdr.FAXNUMBR = GetFieldValue("RM00101", "FAX", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND FAX <> ''", db_ERP);
                    //salesHdr.SLPRSNID = GetFieldValue("RM00101", "SLPRSNID", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND (SLPRSNID = 'CREDIT' or SLPRSNID = 'CASH')", db_ERP);
                    //salesHdr.TAXSCHID = GetFieldValue("RM00101", "TAXSCHID", " CUSTNMBR = '" + CustomerCode.ToString().Trim() + "' AND TAXSCHID <> ''", db_ERP);

                    string dtlQryStr = @"SELECT     
TransactionDetail.TransactionID,
TransactionDetail.BatchNo,
TransactionDetail.Quantity,
TransactionDetail.Price, 
TransactionDetail.ExpiryDate, 
TransactionDetail.Discount, 
ItemLanguage.Description AS ItemName, 
Item.ItemCode AS Barcode, 
PackTypeLanguage.Description AS PackName, 
Pack.Quantity AS PcsInCse, 
TransactionDetail.PackID,
Item.ItemCode,
TransactionDetail.PackStatusID

FROM TransactionDetail INNER JOIN
Pack ON TransactionDetail.PackID = Pack.PackID INNER JOIN
ItemLanguage ON Pack.ItemID = ItemLanguage.ItemID INNER JOIN
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID INNER JOIN
Item ON ItemLanguage.ItemID = Item.ItemID

WHERE (PackTypeLanguage.LanguageID = 1) AND (ItemLanguage.LanguageID = 1) AND (TransactionDetail.TransactionID = '" + TransactionID.ToString() + "')";

                    InCubeQuery dtlQry = new InCubeQuery(db_vms, dtlQryStr);
                    err = dtlQry.Execute();
                    DataRow[] detailsList = dtlQry.GetDataTable().Select();

                    int count = detailsList.Length;
                    int[] detailsListLNITMSEQ = new int[count];
                    taSopLineIvcInsert_ItemsTaSopLineIvcInsert[] LineItems = new taSopLineIvcInsert_ItemsTaSopLineIvcInsert[count];
                    taSopLotAuto_ItemsTaSopLotAuto[] LotNumberItems = new taSopLotAuto_ItemsTaSopLotAuto[count];

                    ClearProgress();
                    SetProgressMax(count);

                    if (count == 0)
                    {
                        throw new Exception("No details found , Invoice Number = " + TransactionID.ToString());
                    }

                    for (int i = 0; i < count; i++)
                    {
                        ReportProgress("Sending Returns");

                        DataRow salesTxRow = detailsList[i];

                        taSopLotAuto_ItemsTaSopLotAuto LotNumber = new taSopLotAuto_ItemsTaSopLotAuto();
                        taSopLineIvcInsert_ItemsTaSopLineIvcInsert salesLine = new taSopLineIvcInsert_ItemsTaSopLineIvcInsert();
                        salesLine.CUSTNMBR = CustomerCode.ToString();
                        salesLine.AUTOALLOCATELOT = 0;
                        salesLine.ALLOCATE = 1;


                        salesLine.SALSTERR = EmployeeCode.ToString().Substring(0, 3);
                        salesLine.SLPRSNID = SLPRSNID;

                        salesLine.SOPNUMBE = TransactionID.ToString();
                        salesLine.LOCNCODE = WarehouseCode.ToString();
                        salesLine.DOCID = _DOCID;
                        salesLine.DOCDATE = date.ToString("yyyy-MM-dd");
                        salesLine.SOPTYPE = 4;

                        field = salesTxRow["Discount"];
                        decimal Discount;
                        if (!decimal.TryParse(field.ToString(), out Discount)) Discount = 0;
                        field = salesTxRow["Price"];
                        decimal unitPrice = decimal.Parse(field.ToString());

                        field = salesTxRow["PackID"];

                        salesLine.ITEMNMBR = salesTxRow["Barcode"].ToString().Trim();

                        salesLine.UOFM = salesTxRow["PackName"].ToString().Trim();

                        STRPack = salesTxRow["ItemName"].ToString().Trim();
                        salesLine.ITEMDESC = STRPack;

                        field = salesTxRow["Quantity"];
                        decimal Quantity = decimal.Parse(field.ToString());
                        string PackStatus = salesTxRow["PackStatusID"].ToString();

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

                        salesLine.COMMNTID = "TEST";

                        salesLine.SHIPMTHD = "DELIVERY";
                        //ItemCode
                        string SelectItem = @"SELECT 
                                           ITMTSHID
                                           
                                           FROM IV00101 I";


                        SelectItem += " WHERE I.ITEMTYPE<>2 and ITEMNMBR like 'FG%' and I.ITEMNMBR ='" + salesLine.ITEMNMBR + "'";
                        InCubeQuery ItemQuery = new InCubeQuery(db_ERP, SelectItem);
                        err = ItemQuery.Execute();
                        err = ItemQuery.FindFirst();
                        ItemQuery.GetField("ITMTSHID", ref field);
                        string ITMTSHID = field.ToString().Trim();
                        salesLine.ITMTSHID = ITMTSHID;
                        salesLine.TAXSCHID = salesHdr.TAXSCHID;

                        #region price and discount

                        Discount = Math.Round(Discount, 3, MidpointRounding.AwayFromZero);
                        Discount = Math.Round(Discount / Quantity, 3, MidpointRounding.AwayFromZero) * Quantity;
                        //unitPrice = Math.Round(unitPrice, 3, MidpointRounding.AwayFromZero);

                        decimal XTNDPRCE = Math.Round(Quantity * unitPrice, 2, MidpointRounding.AwayFromZero);

                        salesLine.UNITPRCE = unitPrice;
                        TOTAL += XTNDPRCE;
                        salesLine.XTNDPRCE = XTNDPRCE;

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
                        salesLine.ADDRESS1 = CustomerAddress.ToString();
                        salesLine.ADDRESS2 = CustomerAddress1.ToString();
                        salesLine.ADDRESS3 = CustomerAddress2.ToString();
                        LineItems[i] = salesLine;
                    }

                    //salesHdr.SHIPMTHD = Shipment;

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
                    salesHdr.DOCAMNT = TOTAL;
                    salesHdr.DOCID = _DOCID;
                    salesHdr.LOCNCODE = WarehouseCode.ToString();
                    salesHdr.DOCDATE = date.ToString("yyyy-MM-dd");

                    salesOrder.taSopLineIvcInsert_Items = LineItems;
                    salesOrder.taSopHdrIvcInsert = salesHdr;

                    eConnectType eConnect = new eConnectType();
                    SOPTransactionType[] MySopTransactionType = { salesOrder };
                    eConnect.SOPTransactionType = MySopTransactionType;

                    string salesOrderDocument;
                    string fname = filename + TransactionID.ToString().Trim() + ".xml";
                    #region  Create xml file
                    {
                        FileStream fs = new FileStream(fname, FileMode.Create);
                        XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                        XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                        serializer.Serialize(writer, eConnect);
                        writer.Close();

                    }
                    #endregion
                    eConnectMethods eConCall = new eConnectMethods();
                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.Load(fname);
                    salesOrderDocument = xmldoc.OuterXml;
                    try
                    {

                        #region  Send xml file to GPs
                        {
                            eConCall.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, salesOrderDocument, EnumTypes.SchemaValidationType.None, "");
                        }
                        #endregion

                        InCubeQuery UpdateQuery = new InCubeQuery(db_vms, "Update [Transaction] SET Synchronized = 1 where TransactionID = '" + TransactionID.ToString() + "'");
                        err = UpdateQuery.Execute();


                        //This query to set SALSTERR and SLPRSNID since sending to GP doesnt affect those fields
                        UpdateQuery = new InCubeQuery(db_ERP, "Update SOP10200 SET SALSTERR = '" + EmployeeCode.ToString().Substring(0, 3) + "', SLPRSNID = '" + SLPRSNID + "' where  SOPNUMBE = '" + TransactionID.ToString() + "' AND SOPTYPE = 4");
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
                        WriteMessage("\r\n" + TransactionID.ToString() + " - FAILED!");
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
            DateTime PDCChDate;
            string CustomerCode = "";
            bool Check = false;
            object SalesPersonCode = "";
            object CustomerName = "";
            DateTime InvDate;

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
                                          AND (CustomerPayment.PaymentDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"' 
                                          AND  CustomerPayment.PaymentDate <= '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"')";


            if (!AllSalespersons)
            {
                QueryString += "    AND CustomerPayment.EmployeeID = " + Salesperson;
            }


            QueryString += @"      GROUP BY   CustomerPayment.CustomerPaymentID, PaymentTypeLanguage.PaymentTypeID, PaymentTypeLanguage.LanguageID, 
                                              CustomerPayment.VoucherNumber, CustomerPayment.VoucherDate, CustomerPayment.BankID, CustomerOutlet.Barcode, 
                                              CustomerPayment.Synchronized, CustomerPayment.PaymentDate,Employee.EmployeeCode, CustomerPayment.PaymentTypeID,CustomerOutletLanguage.Description ORDER BY CustomerPayment.CustomerPaymentID";



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
                    if (field is null || field.ToString() == "")
                    {
                        ChDate = "1990/01/01";
                        PDCChDate = DateTime.Parse("1990/01/01");
                    }
                    else
                    {
                        ChDate = field;
                        PDCChDate = DateTime.Parse(field.ToString());
                    }


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

                    err = CustomerPaymentQuery.GetField(10, ref field);
                    TransactionID = field.ToString();

                    err = CustomerPaymentQuery.GetField(11, ref field);
                    InvDate = DateTime.Parse(field.ToString());

                    //string cashCheckbookId = "CBD-C/A-AED";
                    string cashCheckbookId = "Cash";
                    string chequeCheckbookId = "CDC-HHT";

                    //InCubeQuery codeQry = new InCubeQuery(db_vms, "SELECT     FFOrganizationCheckCode.CashCode, FFOrganizationCheckCode.ChequeCode FROM  FFOrganizationCheckCode INNER JOIN EmployeeOrganization ON FFOrganizationCheckCode.OrganizationID = EmployeeOrganization.OrganizationID WHERE EmployeeOrganization.EmployeeID = " + SalesPersonCode.ToString());
                    //codeQry.Execute();
                    //if (codeQry.FindFirst() == InCubeErrors.Success)
                    //{
                    //    codeQry.GetField("CashCode", ref field);
                    //    cashCheckbookId = field.ToString();
                    //    codeQry.GetField("ChequeCode", ref field);
                    //    chequeCheckbookId = field.ToString();
                    //}
                    //codeQry.Close();


                    if (PaymentType == "1")// "Cash"
                    {

                        #region Cash
                        Check = true;

                        taRMCashReceiptInsert CustomerPaymentCash = new taRMCashReceiptInsert();
                        CustomerPaymentCash.CUSTNMBR = CustomerCode;
                        CustomerPaymentCash.DOCNUMBR = PaymentID;
                        decimal TotalValue = Math.Round(Amount, 5, MidpointRounding.AwayFromZero);
                        CustomerPaymentCash.ORTRXAMT = TotalValue;
                        CustomerPaymentCash.GLPOSTDT = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.DOCDATE = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.CSHRCTYP = 1;//0=check , 1=cash , 2= credit card
                        CustomerPaymentCash.CHEKBKID = chequeCheckbookId; //"DX -CASH";
                        CustomerPaymentCash.CURNCYID = "DHS";
                        //string Batch = "RV" + date.ToString("dd/MM/yy").Replace("/", "") + GetSalespersonCode(SalesPersonCode);
                        string Batch = "RV" + date.ToString("dd/MM/yy").Replace("/", "");
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
                            decimal APPTOAMTVALUE = Math.Round(decimal.Parse(Row["AppliedAmount"].ToString()), 5, MidpointRounding.AwayFromZero);
                            RMApply.APPTOAMT = APPTOAMTVALUE;
                            //RMApply.APPTOAMT = Math.Floor(decimal.Parse(Row["AppliedAmount"].ToString())*100)/100;
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
                        decimal TotalValue = Math.Round(Amount, 5, MidpointRounding.AwayFromZero);
                        CustomerPaymentCash.ORTRXAMT = TotalValue;
                        //CustomerPaymentCash.ORTRXAMT = Math.Floor(Amount * 100) / 100;
                        CustomerPaymentCash.GLPOSTDT = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.DOCDATE = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.CSHRCTYP = 0;
                        CustomerPaymentCash.CURNCYID = "DHS";
                        //string Batch = "RV" + date.ToString("dd/MM/yy").Replace("/", "") + GetSalespersonCode(SalesPersonCode);
                        string Batch = "RV" + date.ToString("dd/MM/yy").Replace("/", "");
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
                            decimal APPTOAMTVALUE = Math.Round(decimal.Parse(Row["AppliedAmount"].ToString()), 5, MidpointRounding.AwayFromZero);
                            RMApply.APPTOAMT = APPTOAMTVALUE;
                            //RMApply.APPTOAMT = Math.Floor(decimal.Parse(Row["AppliedAmount"].ToString()) * 100) / 100;
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
                        DataRow[] DetailsPaymentRow = CustomerPayment.GetDataTable().Select("CustomerPaymentID='" + PaymentID + "'");
                        RMApplyType[] TYPE = new RMApplyType[DetailsPaymentRow.Length];
                        for (int i = 0; i < DetailsPaymentRow.Length; i++)
                        {
                            taRMApply RMApply = new taRMApply();
                            RMApplyType ApplyType = new RMApplyType();
                            DataRow Row = DetailsPaymentRow[i];
                            RMApply.APTODCNM = Row["TransactionID"].ToString();
                            RMApply.APFRDCNM = Row["VoucherNmuber"].ToString();
                            decimal APPTOAMTVALUE = Math.Round(decimal.Parse(Row["AppliedAmount"].ToString()) / 100, 5, MidpointRounding.AwayFromZero);
                            RMApply.APPTOAMT = APPTOAMTVALUE;
                            //RMApply.APPTOAMT = Math.Floor(decimal.Parse(Row["AppliedAmount"].ToString()) * 100) / 100;
                            RMApply.APFRDCTY = 8;
                            RMApply.APTODCTY = 1;
                            RMApply.APPLYDATE = date.ToString("yyyy-MM-dd");
                            RMApply.GLPOSTDT = DateTime.Now.ToString("yyyy-MM-dd");
                            ApplyType.taRMApply = RMApply;
                            TYPE[i] = ApplyType;
                        }
                        eConnect.RMApplyType = TYPE;
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
                        Check = true;

                        incubeQry = new InCubeQuery(db_vms, "Exec sp_SendPDCReceipts '" + PaymentID + "'");
                        err = incubeQry.ExecuteNonQuery();

                        ////PDCRVNO : Receipt Number
                        ////PDCRVDT: Receipt Date (due date of the PDC) 
                        ////PDCCUSTID: Customer Code
                        ////PDCDOCNO: ?? Cheque no
                        ////PDCDOCDT: ?? Receipt Date
                        ////PDCBANK: BankID 
                        ////PDCAMOUNT: PDC Amount
                        ////PDCONHANDDATE:  – collected date (current date)

                        ////string Query = @"Insert into PDCOPEN (PDCRVNO, PDCRVDT, PDCCUSTID, PDCDOCNO, PDCDOCDT, PDCBANK, PDCAMOUNT, PDCONHANDDATE) 
                        ////                        values ('" + PaymentID + "', '" + date.ToString("yyyy-MM-dd") + "', '" + CustomerCode + "', '" + ChNumber + "', '" + date.ToString("yyyy-MM-dd") + "', '" + BankID + "', " + Amount + ", '" + DateTime.Now.ToString("yyyy-MM-dd") + "')";
                        //string PaymentNumber = "";
                        //PaymentNumber = GetFieldValue("Stg_PDCSeries", "ActualSeries", db_vms);

                        //string LastPaymentID = "";
                        //LastPaymentID = GetFieldValue("Stg_PDCSeries", "LastPayID", db_vms);
                        //string Branch = "DXB";
                        //InCubeQuery GetBranchCMD = new InCubeQuery(db_ERP, @"Select SALSTERR From RM00101 Where CUSTNMBR = '" + CustomerCode + "'");
                        //GetBranchCMD.Execute();
                        //GetBranchCMD.FindFirst();
                        //GetBranchCMD.GetField(0, ref field);
                        //Branch = field.ToString();

                        //string ExistSeries = "";
                        //ExistSeries = GetFieldValue("PDC00002", "DOCNUMBR", " DOCNUMBR = '" + PaymentNumber + "'", db_ERP);

                        //if (ExistSeries.ToString() == "" || ExistSeries.ToString() is null)
                        //{
                        //    string Query = @"Insert into PDC00002 (DOCNUMBR, pdcDocType, pdcDocStatus, DOCID, DOCDATE, CUSTNMBR, CHEKBKID, CHEKNMBR, CHEKDATE, CHEKAMNT, CURTRXAM, CURNCYID, BANKID, SLPRSNID, CHKCOMNT, pdcXChangeDocNumber, pdcMarkedToPost, pdcDocStatus1, CBKIDCHK, CMXFTDATE, TIME1, USERID, pdcMasterDocNumber, TIMESPRT) 
                        //                        Values ('" + PaymentNumber.ToString() + "', 1, 8, 'PDC-HHT-RCPT', '" + date.ToString("yyyy-MM-dd") + "', '" + CustomerCode + "', '', '" + ChNumber + "' ,'" + PDCChDate.ToString("yyyy-MM-dd") + "', " + Amount + ", '0', 'AED', '', '' , '" + PaymentID + "', '', 0, 0, '', '" + PDCChDate.ToString("yyyy-MM-dd") + "', '1900-01-01 08:04:19.000', '" + SalesPersonCode.ToString() + "', '', 0)";

                        //    InCubeQuery PDCDETAIL = new InCubeQuery(db_ERP, Query);
                        //    err = PDCDETAIL.Execute();
                        //}

                        //string Query2 = @"Insert into PDC00007 (DOCNUMBR, APTODCTY, APTODCNM, APTVCHNM, APPTOAMT, APTODCDT, USERID, pdcDocType) 
                        //                        values ('" + PaymentNumber.ToString() + "',1,'" + TransactionID + "',''," + Amount + ", '" + InvDate.ToString("yyyy-MM-dd") + "', '" + SalesPersonCode.ToString() + "', 1)";

                        //InCubeQuery PDCOPEN = new InCubeQuery(db_ERP, Query2);
                        //err = PDCOPEN.Execute();

                        ////MessageBox.Show("Insert into PDCOPEN " + err.ToString() + " Exception :" + PDCOPEN.GetCurrentException().ToString() + " Query : " + Query);

                        ////BranchID: 3 letter code for branch (DXB,AUH)
                        ////ReceiptNo: Receipt Number.
                        ////ReceiptDate: Receipt Date. This is NOT the current date. It’s the cheque date (PDC)
                        ////CustomerID: Customer Code
                        ////CustomerName: Customer Name.
                        ////CheckBookID: Chequebookid
                        ////CurrencyID: its fixed "DHS"
                        ////Bank: Bankid
                        ////ChequeNumber: Cheque Number.
                        ////DueDate:Receipt Date
                        ////Amount: PDC Amount
                        ////Status: 0 (zero)
                        ////PrintCount: always 0 
                        ////SPID: SalespersonID (S100, S200)



                        ////Query = @"Insert into PDCDetail (BranchID, ReceiptNo, ReceiptDate, CustomerID, CustomerName, CheckBookID, CurrencyID, Bank, ChequeNumber, DueDate, Amount, Status, PrintCount, SPID) 
                        ////                        Values ('" + Branch + "', '" + PaymentID + "', '" + date.ToString("yyyy-MM-dd") + "', '" + CustomerCode + "', '" + CustomerName + "', '" + chequeCheckbookId + "', 'DHS', '" + BankID + "', '" + ChNumber + "', '" + date.ToString("yyyy-MM-dd") + "', " + Amount + ", 0, 0, '" + SalesPersonCode.ToString() + "')";




                        //if (LastPaymentID != PaymentID)
                        //{
                        //    incubeQry = new InCubeQuery(db_vms, "UPDATE Stg_PDCSeries SET Series = 'P' + CAST((RIGHT(Series,7) + 1) AS NVARCHAR)");
                        //    err = incubeQry.ExecuteNonQuery();

                        //    incubeQry = new InCubeQuery(db_vms, "Update Stg_PDCSeries SET ActualSeries = 'HHT' + FORMAT(CAST(RIGHT(Series,6) AS INT) , '000000')");
                        //    err = incubeQry.ExecuteNonQuery();
                        //}

                        //incubeQry = new InCubeQuery(db_vms, "Update Stg_PDCSeries SET LastPayID = '" + PaymentID + "'");
                        //err = incubeQry.ExecuteNonQuery();
                        ////MessageBox.Show("Insert into PDCOPEN " + err.ToString() + " Exception :" + PDCOPEN.GetCurrentException().ToString() + " Query : " + Query);

                        #endregion

                        ////#region Current Dated Cheque
                        ////Check = true;

                        ////taRMCashReceiptInsert CustomerPaymentCash = new taRMCashReceiptInsert();
                        ////CustomerPaymentCash.CUSTNMBR = CustomerCode;
                        ////CustomerPaymentCash.DOCNUMBR = PaymentID;
                        ////decimal TotalValue = Math.Round(Amount, 3, MidpointRounding.AwayFromZero);
                        ////CustomerPaymentCash.ORTRXAMT = TotalValue;
                        //////CustomerPaymentCash.ORTRXAMT = Math.Floor(Amount * 100) / 100;
                        ////CustomerPaymentCash.GLPOSTDT = date.ToString("yyyy-MM-dd");
                        ////CustomerPaymentCash.DOCDATE = date.ToString("yyyy-MM-dd");
                        ////CustomerPaymentCash.CSHRCTYP = 0;
                        ////CustomerPaymentCash.CURNCYID = "DHS";
                        //////string Batch = "RV" + date.ToString("dd/MM/yy").Replace("/", "") + GetSalespersonCode(SalesPersonCode);
                        ////string Batch = "RV" + date.ToString("dd/MM/yy").Replace("/", "");
                        ////CustomerPaymentCash.BACHNUMB = "PDC";
                        ////CustomerPaymentCash.CURNCYID = "";
                        ////CustomerPaymentCash.CREATEDIST = 1;
                        ////CustomerPaymentCash.TRXDSCRN = "Apply to" + TransactionID;
                        ////CustomerPaymentCash.CHEKNMBR = ChNumber;
                        ////CustomerPaymentCash.CHEKBKID = chequeCheckbookId;
                        ////eConnectType eConnect = new eConnectType();
                        ////DataRow[] DetailsPaymentRow = CustomerPayment.GetDataTable().Select("CustomerPaymentID='" + PaymentID + "'");
                        ////RMApplyType[] TYPE = new RMApplyType[DetailsPaymentRow.Length];
                        ////for (int i = 0; i < DetailsPaymentRow.Length; i++)
                        ////{
                        ////    taRMApply RMApply = new taRMApply();
                        ////    RMApplyType ApplyType = new RMApplyType();
                        ////    DataRow Row = DetailsPaymentRow[i];
                        ////    RMApply.APTODCNM = Row["TransactionID"].ToString();
                        ////    RMApply.APFRDCNM = PaymentID.ToString();
                        ////    decimal APPTOAMTVALUE = Math.Round(decimal.Parse(Row["AppliedAmount"].ToString()), 3, MidpointRounding.AwayFromZero);
                        ////    RMApply.APPTOAMT = APPTOAMTVALUE;
                        ////    //RMApply.APPTOAMT = Math.Floor(decimal.Parse(Row["AppliedAmount"].ToString()) * 100) / 100;
                        ////    RMApply.APFRDCTY = 9;
                        ////    RMApply.APTODCTY = 1;
                        ////    RMApply.APPLYDATE = date.ToString("yyyy-MM-dd");
                        ////    RMApply.GLPOSTDT = DateTime.Now.ToString("yyyy-MM-dd");
                        ////    ApplyType.taRMApply = RMApply;
                        ////    TYPE[i] = ApplyType;
                        ////}
                        ////eConnect.RMApplyType = TYPE;
                        ////CashType.taRMCashReceiptInsert = CustomerPaymentCash;
                        ////RMCashReceiptsType[] CashReceipts = { CashType };
                        ////eConnect.RMCashReceiptsType = CashReceipts;
                        ////string fname = filename + PaymentID.ToString().Trim() + ".xml";
                        ////FileStream fs = new FileStream(fname, FileMode.Create);
                        ////XmlTextWriter writer = new XmlTextWriter(fs, new UTF8Encoding());
                        ////// Serialize using the XmlTextWriter.
                        ////XmlSerializer serializer = new XmlSerializer(eConnect.GetType());
                        ////serializer.Serialize(writer, eConnect);
                        ////writer.Close();
                        ////eConnectMethods eConCall1 = new eConnectMethods();
                        ////string salesOrderDocument = "";
                        ////XmlDocument xmldoc = new XmlDocument();
                        ////xmldoc.Load(fname);
                        ////salesOrderDocument = xmldoc.OuterXml;
                        ////try
                        ////{
                        ////    eConCall1.eConnect_EntryPoint(sConnectionString, EnumTypes.ConnectionStringType.SqlClient, salesOrderDocument, EnumTypes.SchemaValidationType.None, "");
                        ////}
                        ////catch (eConnectException exp)
                        ////{
                        ////    Check = false;
                        ////    Console.Write(exp.ToString());
                        ////    StreamWriter wrt5 = new StreamWriter("errorPay.log", true);
                        ////    wrt5.Write(exp.ToString());
                        ////    wrt5.Close();
                        ////}
                        //#endregion
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
            SerializeSalesOrderObjectSendTransfers(CoreGeneral.Common.StartupPath + "\\E-Connect\\",Filters.EmployeeID == -1, Filters.EmployeeID.ToString(), Filters.FromDate, Filters.ToDate);
        }

        public void SerializeSalesOrderObjectSendTransfers(string filename, bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate)
        {
            try
            {

                WriteMessage("\r\n" + "Sending Transfers");

                DataTable dt = new DataTable();
                string EmpFilter = "";
                if (!AllSalespersons)
                {
                    EmpFilter = "AND WT.RequestedBy = " + Salesperson;
                }
                string transfers = string.Format(@"
SELECT distinct WT.TransactionID, WT.RouteHistoryID, W.WarehouseCode, W2.WarehouseCode, 
CASE WT.TransactionTypeID WHEN 1 THEN 1 ELSE 2 END TransactionType, WT.TransactionDate, WT.LoadDate,
CASE WT.WarehouseTransactionStatusID WHEN 1 THEN 1 ELSE 2 END TransactionStatus,WT.SourceTransactionID,SUM(WD.Quantity) AS TotalQTY,
E.EmployeeCode, EL.Description EmployeeName, SL.Description Street, AL.Description Area, T.TerritoryCode RouteCode
FROM WarehouseTransaction WT
INNER JOIN Warehouse W ON W.WarehouseID = WT.WarehouseID
inner join WhTransDetail WD on wt.TransactionID=wd.TransactionID and wd.WarehouseID=wt.WarehouseID
LEFT JOIN Warehouse W2 ON W2.WarehouseID = WT.RefWarehouseID
INNER JOIN Employee E ON WT.RequestedBy = E.EmployeeID
INNER JOIN EmployeeLanguage EL ON EL.EmployeeID = E.EmployeeID AND EL.LanguageID = 1
LEFT JOIN EmployeeTerritory ET ON E.EmployeeID = ET.EmployeeID
LEFT JOIN Territory T ON ET.TerritoryID = T.TerritoryID
LEFT JOIN Street S ON S.StreetID = E.StreetID
LEFT JOIN StreetLanguage SL ON SL.StreetID = S.StreetID AND SL.LanguageID = 1
LEFT JOIN AreaLanguage AL ON S.AreaID = AL.AreaID AND AL.LanguageID = 1
WHERE Synchronized = 0
AND TransactionTypeID IN (1) AND WarehouseTransactionStatusID IN (1,2)
AND WT.TransactionDate >= '{0}' AND WT.TransactionDate < '{1}'
{2}
Group By WT.TransactionID, WT.RouteHistoryID, W.WarehouseCode, W2.WarehouseCode,WT.TransactionTypeID
,WT.TransactionDate, WT.LoadDate,WT.WarehouseTransactionStatusID,WT.SourceTransactionID,E.EmployeeCode,EL.Description,SL.Description,AL.Description,T.TerritoryCode
", FromDate.Date.ToString("yyyy/MM/dd"), ToDate.Date.AddDays(1).ToString("yyyy/MM/dd"), EmpFilter);

                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "Transfer Qry:\r\n" + transfers, LoggingType.Information, LoggingFiles.errorTSFR);
                incubeQry = new InCubeQuery(transfers, db_vms);
                err = incubeQry.Execute();
                dt = incubeQry.GetDataTable();
                DataTable detailDT = new DataTable();
                ClearProgress();
                SetProgressMax(dt.Rows.Count);

                foreach (DataRow dr in dt.Rows)
                {
                    string result = "";
                    try
                    {
                        ReportProgress("Sending Transfers");

                        string JM_Gate_Pass_No = string.Empty;
                        string DocumentID = string.Empty;
                        string SalespersonID = string.Empty;
                        string Name = string.Empty;
                        string LocationID = string.Empty;
                        string LocationName = string.Empty;
                        string JM_Truck_No = string.Empty;
                        string JM_Total_Qty_Issued = string.Empty;
                        string UserDefined1 = string.Empty;
                        string UserDefined4 = string.Empty;
                        string TotalQTY = "0";
                        string RouteCode = string.Empty;
                        string TransactionNo = string.Empty;
                        string Street = string.Empty;
                        string Area = string.Empty;
                        string OrderID = string.Empty;
                        string GatePassType = string.Empty;
                        result = "";

                        TransactionNo = dr["TransactionID"].ToString().Trim();
                        WriteMessage("\r\n" + TransactionNo + ": ");

                        SalespersonID = dr["EmployeeCode"].ToString().Trim();
                        Name = dr["EmployeeName"].ToString().Trim();
                        RouteCode = dr["RouteCode"].ToString().Trim();
                        
                        TotalQTY = dr["TotalQTY"].ToString().Trim();
                        Street = dr["Street"].ToString().Trim();
                        Area = dr["Area"].ToString().Trim();
                        OrderID = dr["SourceTransactionID"].ToString().Trim();
                        if (OrderID == "" || OrderID == null)
                        {
                            GatePassType = "VanSales";
                        }
                        else
                        {
                            GatePassType = "Delivery";
                        }

                        LocationName = GetFieldValue("IV40700", "LOCNDSCR", " rtrim(ltrim(LOCNCODE)) = '" + Area + "'", db_ERP);
                        JM_Gate_Pass_No = GetFieldValue("Stg_GatePass", "GatePassNo", "GatePassType = '" + Street + "'", db_vms);
                        string GatePassSequence = GetFieldValue("Stg_GatePass", "Sequence", "GatePassType = '" + Street + "'", db_vms);
                        
                        //if (Street == "HATTA")
                        //{
                        //    JM_Gate_Pass_No = GatePassSequence;
                        //}
                        //else
                        //{
                            JM_Gate_Pass_No = JM_Gate_Pass_No + GatePassSequence;
                        //}

                        DateTime DocumentDateTime;
                        if (Street == "DXBMAIN" || Street == "HATTA")
                        {
                            if (dr["LoadDate"] == null || dr["LoadDate"] == DBNull.Value || string.IsNullOrEmpty(dr["LoadDate"].ToString()))
                            {
                                result = "Load date has no value!!";
                                continue;
                            }
                            if (!DateTime.TryParse(dr["LoadDate"].ToString(), out DocumentDateTime))
                            {
                                result = "Load date couldn't be parsed, provided date value is " + dr["LoadDate"];
                                continue;
                            }
                        }
                        else
                        {
                            object OrderDate = null;
                            incubeQry = new InCubeQuery(db_vms, string.Format(@"SELECT TOP 1 DA.ScheduleDate
FROM SalesOrder  SO
INNER JOIN DeliveryAssignment DA ON DA.OrderID = SO.OrderID AND DA.CustomerID = SO.CustomerID AND DA.OutletID = SO.OutletID
WHERE WarehouseTransactionID = '{0}'
ORDER BY DA.AssignmentDate DESC", TransactionNo));
                            incubeQry.ExecuteScalar(ref OrderDate);
                            if (OrderDate == null || OrderDate == DBNull.Value || string.IsNullOrEmpty(OrderDate.ToString()))
                            {
                                result = "Order date has no value!!";
                                continue;
                            }
                            if (!DateTime.TryParse(OrderDate.ToString(), out DocumentDateTime))
                            {
                                result = "Order date couldn't be parsed, provided date value is " + OrderDate;
                                continue;
                            }
                        }

                        string HeaderQuery = string.Format(@"INSERT INTO JMGPHDRWORK
(GPNo,Load_No,DOCID,DOCTYPE,DOCDATE,SLPRSNID,NAME,LOCATNID,LOCATNNM,Helper_ID,SHelperID,TruckNo,Status,
STSDESCR,JMTotalQtyIssued,JMTotalQtyReturned,TotalQtyDamaged,USERDEF1,USERDEF2,USRDEF03,USRDEF04,USRDAT01,
USRDAT02,CREATDDT,CRUSRID,MODIFDT,MDFUSRID,PRTDTTKN,PRNTSTUS,JM_Printing_Time,FLAG,Flags,JM_Remarks)
VALUES ('{8}',1,'{5}',1,'{0}','{3}','{4}','{6}','{7}','','','From GP',0,'From GP',
{1},0,0,'{6}','','','{2}','','','{9}','Sonic','','',0,0,'','',0,'')",
    DocumentDateTime.ToString("yyyy/MM/dd"), TotalQTY, RouteCode.ToString(), SalespersonID.ToString(), Name.ToString(), Street.ToString(), Area.ToString(), LocationName.ToString(), JM_Gate_Pass_No.ToString(), DocumentDateTime.ToString("yyyy/MM/dd"));
                        incubeQry = new InCubeQuery(db_ERP, HeaderQuery);
                        if (incubeQry.ExecuteNonQuery() != InCubeErrors.Success)
                        {
                            result = "Inserting into header table was not successful, Query: " + HeaderQuery;
                            continue;
                        }

                        string transfDet = string.Format(@"SELECT RANK() OVER(ORDER BY P.ItemID, P.PackTypeID) Line, 
I.ItemCode, PTL.Description UOM, SUM(WD.Quantity) QTY, WD.BatchNo,WD.ExpiryDate,P.PackID
FROM WhTransDetail WD
INNER JOIN Pack P ON P.PackID = WD.PackID
INNER JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1
INNER JOIN Item I ON I.ItemID = P.ItemID
WHERE WD.TransactionID = '" + TransactionNo + "'GROUP BY P.ItemID, P.PackTypeID, I.ItemCode, PTL.Description, WD.BatchNo,WD.ExpiryDate,P.PackID");

                        incubeQry = new InCubeQuery(transfDet, db_vms);
                        err = incubeQry.Execute();
                        detailDT = incubeQry.GetDataTable();
                        foreach (DataRow det in detailDT.Rows)
                        {

                            string ItemCode = string.Empty;
                            string UOM = string.Empty;
                            string QTY = "0";
                            string PackID = "0";
                            string BatchNo = string.Empty;
                            string ExpiryDate = string.Empty;
                            PackID = det["PackID"].ToString().Trim();
                            ItemCode = det["ItemCode"].ToString().Trim();
                            UOM = det["UOM"].ToString().Trim();
                            string Line = "0";
                            Line = det["Line"].ToString().Trim();
                            QTY = det["QTY"].ToString().Trim();
                            BatchNo = det["BatchNo"].ToString().Trim();
                            ExpiryDate = det["ExpiryDate"].ToString().Trim();

                            string DetailsQuery = string.Format(@"INSERT INTO JMGPLINEWORK
(GPNo,ITEMNMBR,LINCOUNT,Load_No,UOFM,LNITMSEQ,ATYALLOC,QTYRTRND,QTYDMGED,QTYMATCH,QTYREMAI,QTYBKORD,QTYCANCE,
QTYCOMTD,QTYCUE,AMNTDUE,AMNTPAID,AMTPDLYR,USERDEF1,USERDEF2,USRDEF03,USRDAT01,USRDAT02,FLAG,FLGINVRS,Flags,
CREATDDT,CRUSRID,MODIFDT,MDFUSRID)
VALUES ('{6}','{0}','{3}',1,'{1}',0,{5},0,0,0,0,0,0,0,0,0,0,0,'{4}','','','','',
'','',0,'{2}','Sonic','','')",
    ItemCode, UOM, DocumentDateTime.ToString("yyyy/MM/dd"), Line, Area.ToString(), QTY, JM_Gate_Pass_No.ToString());
                            incubeQry = new InCubeQuery(db_ERP, DetailsQuery);
                            incubeQry.ExecuteNonQuery();

                            //                            string WarehouseStockHistoryQuery = string.Format(@"INSERT INTO WarehouseStockHistory 
                            //(EmployeeID,TransactionMainTypeID,TransactionID,PackID,BatchNo,QuantityChange,ExpiryDate,IsMinusStock,DivisionID,RouteHistoryID,Sequence,PackSequence)
                            //VALUES (0,2,'{0}','{1}','{2}','{3}','{4}',0,-1,-1,-1,-1);",
                            //    TransactionNo.ToString(), PackID, BatchNo, QTY, ExpiryDate);
                            //                            incubeQry = new InCubeQuery(db_vms, WarehouseStockHistoryQuery);
                            //                            err = incubeQry.ExecuteNonQuery();

                            string WarehouseStockHistoryArchQuery = string.Format(@"INSERT INTO WarehouseStockHistoryArc 
(EmployeeID,TransactionMainTypeID,TransactionID,PackID,BatchNo,QuantityChange,ExpiryDate,IsMinusStock,DivisionID,RouteHistoryID,Sequence,PackSequence)
VALUES (0,2,'{0}','{1}','{2}','{3}','{4}',0,-1,-1,-1,-1);",
    TransactionNo.ToString(), PackID, BatchNo, QTY, ExpiryDate);
                            incubeQry = new InCubeQuery(db_vms, WarehouseStockHistoryArchQuery);
                            err = incubeQry.ExecuteNonQuery();
                        }

                        //Update Load Request Status
                        incubeQry = new InCubeQuery(db_vms, "UPDATE WarehouseTransaction SET Synchronized = 1, WarehouseTransactionStatusID = 3 WHERE TransactionID =  '" + TransactionNo.ToString() + "'");
                        err = incubeQry.ExecuteNonQuery();

                        incubeQry = new InCubeQuery(db_vms, "UPDATE Stg_GatePass SET Sequence = Sequence + 1 Where GatePassType = '" + Street + "'");
                        err = incubeQry.ExecuteNonQuery();

                        incubeQry = new InCubeQuery(db_vms, string.Format(@"UPDATE SO SET OrderStatusID = 5
FROM SalesOrder  SO
INNER JOIN DeliveryAssignment DA ON DA.OrderID = SO.OrderID AND DA.CustomerID = SO.CustomerID AND DA.OutletID = SO.OutletID
WHERE WarehouseTransactionID = '{0}'
AND SO.OrderStatusID = 4", TransactionNo));
                        err = incubeQry.ExecuteNonQuery();

                       result = "GatePass No" + JM_Gate_Pass_No + " Inserted Successfully";
                    }
                    catch (Exception ex)
                    {
                        result = "Unhandled exception, check errorTSFR file!!";
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
                    }
                    finally
                    {
                        WriteMessage(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
        }
        
        #endregion

        public override void Close()
        {
            db_ERP.Close();
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
                err = CustomerQuery.FindNext();
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
        private void UpdateExpiryDate(DataRow[] txDetails, int[] LNITMSEQList)
        {

            object UOM = "";
            object ItemCode = "";
            object Pack = "";
            object Lot = "";

            try
            {
                for (int i = 0; i < txDetails.Length; i++)
                {
                    DateTime exDate = (DateTime)txDetails[i]["ExpiryDate"];
                    Pack = txDetails[i]["PackID"];
                    Lot = txDetails[i]["BatchNo"];
                    string TransactionID = txDetails[i]["TransactionID"].ToString();
                    ItemCode = txDetails[i]["ItemCode"];
                    string DATALOT = exDate.ToString("dd/MM/yyyy");
                    SqlCommand cmd2 = new SqlCommand("update SOP10201 Set EXPNDATE=CONVERT(DATETIME,'" + exDate.ToString("yyyy-MM-dd") + "',102)  where SOPTYPE =4  AND SOPNUMBE='" + TransactionID + "' AND  LNITMSEQ=" + LNITMSEQList[i] + " AND  QTYTYPE=1 AND SERLTNUM='" + Lot.ToString() + "' and  ITEMNMBR='" + ItemCode + "' ", db_ERP_con);
                    StreamWriter wrt = new StreamWriter("UpdateLog.log", true);
                    wrt.Write(cmd2.CommandText.ToString());
                    wrt.WriteLine();
                    int a = cmd2.ExecuteNonQuery();
                    wrt.Write(a.ToString());
                    wrt.Close();
                }

            }
            catch (Exception ex1)
            {
                WriteMessage(ex1.Message);
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
        public override void OutStanding()
        {
            try
            {

                WriteExceptions("BEGIN UPDATING BALANCE", "OUTSTANDING", true);
                //                string outstanding = string.Format(@"SELECT     dbo.RM20101.DOCNUMBR, dbo.RM20101.SLPRSNID, dbo.RM20101.ORTRXAMT, dbo.RM20101.CURTRXAM
                //FROM         dbo.RM20101 INNER JOIN
                //                      dbo.RM20201 ON dbo.RM20101.DOCNUMBR = dbo.RM20201.APTODCNM
                //WHERE     (dbo.RM20201.APTODCNM LIKE '%INV-%') AND (NOT (dbo.RM20201.APFRDCNM LIKE '%PAY-%')) AND (dbo.RM20101.DOCDATE > '09-09-2019')
                //and dbo.RM20101.SLPRSNID between '1' and '999999'
                //ORDER BY dbo.RM20101.DOCNUMBR");

                InCubeQuery incubeQuery2 = new InCubeQuery(db_vms, "EXEC sp_GetGPInvoices");
                incubeQuery2.ExecuteNonQuery();

                InCubeQuery UpdateRemainingAmount = new InCubeQuery(db_vms, "UPDATE [Transaction] SET RemainingAmount = 0 WHERE TransactionTypeID IN (1,3) and Synchronized=1");
                err = UpdateRemainingAmount.Execute();

                string outstanding = string.Format(@"SELECT dbo.RM20101.DOCNUMBR, dbo.RM20101.SLPRSNID, dbo.RM20101.ORTRXAMT, dbo.RM20101.CURTRXAM
FROM dbo.RM20101 
Where RM20101.RMDTYPAL  in (1) AND RM20101.VOIDSTTS = 0 AND CURTRXAM > 0 and dbo.RM20101.PYMTRMID <> 'cash'
ORDER BY dbo.RM20101.DOCNUMBR
");


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
                InCubeQuery outstandingQry = new InCubeQuery(outstanding, db_ERP);
                DataTable tbl = new DataTable();
                err = outstandingQry.Execute();
                tbl = outstandingQry.GetDataTable();
                ClearProgress();
                SetProgressMax(tbl.Rows.Count);
                WriteExceptions("SELECTING BALANCE, NUMBER OF RECORDS RETURNED IS " + tbl.Rows.Count.ToString() + "", "OUTSTANDING", true);
                int TOTALUPDATED = 0;
                foreach (DataRow dr in tbl.Rows)
                {
                    ReportProgress("Updating Outstanding");
                    //string CustomerCode = dr["CUSTNMBR"].ToString().Trim();
                    //if (CustomerCode.Equals(string.Empty)) continue;
                    string EmployeeCode = dr["SLPRSNID"].ToString().Trim();
                    if (EmployeeCode.Equals(string.Empty)) { WriteExceptions("SALESMAN CODE IS EMPTY", "OUTSTANDING", true); continue; }
                    string TransactionID = dr["DOCNUMBR"].ToString().Trim();
                    if (TransactionID.Equals(string.Empty)) { WriteExceptions("TRANSACTION ID IS EMPTY", "OUTSTANDING", true); continue; }
                    string RemainingAmnt = dr["CURTRXAM"].ToString().Trim();
                    if (RemainingAmnt.Equals(string.Empty)) WriteExceptions("REMAINING AMOUNT IS EMPTY", "OUTSTANDING", true);
                    string Applied = "0";// dr["APPTOAMT"].ToString().Trim();
                    string AllAmnt = dr["ORTRXAMT"].ToString().Trim();
                    if (AllAmnt.Equals(string.Empty)) WriteExceptions("TOTAL AMOUNT IS EMPTY", "OUTSTANDING", true);
                    string checkTtansactionID = GetFieldValue("[Transaction]", "RemainingAmount", "TransactionID='" + TransactionID + "'", db_vms).Trim();
                    if (checkTtansactionID.Equals(string.Empty))
                    {
                        WriteExceptions("ATTEMPT TO UPDATE A NONE EXISTING INVOICE ...... INVOICE NUMBER = " + TransactionID + "", "NOT EXISTING INVOICE", true);
                    }
                    string spID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode='" + EmployeeCode + "'", db_vms).Trim();

                    if (spID.Equals(string.Empty)) { WriteExceptions("SALESMAN CODE " + EmployeeCode + " DOES NOT EXIST IN DB", "OUTSTANDING", true); continue; }
                    //string customerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode='" + CustomerCode + "'", db_vms).Trim();
                    //if (customerID.Equals(string.Empty)) continue;
                    //string outletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerCode='" + CustomerCode + "'", db_vms).Trim();
                    //if (outletID.Equals(string.Empty)) continue;
                    object field = new object();
                    string CheckUploaded = string.Format("select top(1)uploaded,deviceserial from RouteHistory where EmployeeID=" + spID + " ORDER BY RouteHistoryID Desc ");
                    InCubeQuery qry = new InCubeQuery(CheckUploaded, db_vms);
                    err = qry.Execute();
                    err = qry.FindFirst();
                    err = qry.GetField("uploaded", ref field);
                    string uploaded = field.ToString().Trim();
                    uploaded = "False";
                    err = qry.GetField("deviceserial", ref field);
                    string deviceserial = field.ToString().Trim();
                    if (!uploaded.ToString().Trim().Equals(string.Empty) && !uploaded.ToString().Trim().Equals("System.Object"))
                    {
                        if (Convert.ToBoolean(uploaded.ToString().Trim()))
                        {
                            WriteMessage("\r\n");
                            WriteMessage("<<< The Salesman " + EmployeeCode + " is not downloaded . No Outstanding will be modified .>>> Total Updated = " + TOTALUPDATED);
                            continue;
                        }

                    }
                    err = QueryBuilderObject.SetField("RemainingAmount", RemainingAmnt);
                    err = QueryBuilderObject.SetField("Notes", "'" + "app" + Applied + "-tot" + AllAmnt + "-rem" + RemainingAmnt + "'");
                    err = QueryBuilderObject.UpdateQueryString("[Transaction]", "TransactionID='" + TransactionID + "'", db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        WriteExceptions("TRANSACTION ID " + TransactionID + " WITH DETAILS " + "app" + Applied + "-tot" + AllAmnt + "-rem" + RemainingAmnt + " HAS FAILED", "OUTSTANDING", true);
                    }
                    else
                    {

                    }
                }
                UpdateBalanceCreditLimit();
                InCubeQuery incubeQuery = new InCubeQuery(db_vms, "EXEC sp_UpdateCustomerBalance");
                incubeQuery.ExecuteNonQuery();

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
                wrt.Write("\n" + header + "----" + DateTime.Now.ToString() + "\r\n");
                wrt.Write("\n" + description + "\r\n");
                wrt.Close();
            }
            catch
            {

            }
        }
        public void BillToShipTo()
        {
            try
            {
                DataTable table = new DataTable();
                DataTable rowTable;
                string selectCustomers = string.Format("select * from zz_BillToShipTo");
                InCubeQuery qry = new InCubeQuery(selectCustomers, db_vms);
                err = qry.Execute();
                table = qry.GetDataTable();
                foreach (DataRow dr in table.Rows)
                {
                    string MainCode = dr["MainCode"].ToString().Trim();
                    string MainName = dr["MainName"].ToString().Trim();
                    string OutletCode = dr["OutletCode"].ToString().Trim();
                    string Outletname = dr["OutletName"].ToString().Trim();

                    if (MainCode == "10563-000")
                    {

                    }
                    string ShouldBeMainCustID = GetFieldValue("Customer", "CustomerID", "CustomerCode='" + MainCode + "'", db_vms).Trim();//this should be the new customerID for the new outlet 
                    if (ShouldBeMainCustID.Equals(string.Empty)) continue;
                    string ShouldBeTheNewOutletID = GetFieldValue("CustomerOutlet", "isnull(Max(OutletID),0)+1", "CustomerID='" + ShouldBeMainCustID + "'", db_vms).Trim();

                    string ExistingCustomerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode='" + OutletCode + "'", db_vms).Trim();
                    string ExistingOutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerCode='" + OutletCode + "'", db_vms).Trim();

                    if (ExistingCustomerID.Equals(ShouldBeMainCustID)) continue;
                    //string existMainCustWithOutlet = GetFieldValue("CustomerOutlet", "OutletID", "CustomerID=" + ShouldBeMainCustID + " and OutletID=" + ExistingOutletID + "", db_vms).Trim();
                    //if (!existMainCustWithOutlet.Equals(string.Empty)) continue;

                    //NOW SELECTING THE CUSTOMER OUTLET FROM DATABASE TO INSERT IT AGAIN BUT WITH THE NEW CUSTOMER ID 
                    string existingCustRow = "select CO.*,COL.Address,COL.Description from CustomerOutlet CO inner join CustomerOutletLanguage COL on CO.CustomerID=COL.CustomerID and CO.OutletID=COL.OutletID and COL.LanguageID=1 where CO.CustomerID=" + ExistingCustomerID + " and CO.OutletID=" + ExistingOutletID + "";


                    qry = new InCubeQuery(existingCustRow, db_vms);
                    err = qry.Execute();
                    rowTable = new DataTable();
                    rowTable = qry.GetDataTable();

                    if (rowTable.Rows.Count == 0) continue;
                    #region INSERTING THE NEW CUSTOMER
                    //NOW INSERTING THE NEW CUSTOMEROUTLET WITH THE MAIN CUSTOMER ID 

                    QueryBuilderObject.SetField("CustomerID", ShouldBeMainCustID);
                    QueryBuilderObject.SetField("OutletID", ShouldBeTheNewOutletID);
                    QueryBuilderObject.SetField("CustomerCode", "'" + OutletCode + "'");
                    QueryBuilderObject.SetField("Barcode", "'" + OutletCode + "'");
                    QueryBuilderObject.SetField("Phone", "'" + rowTable.Rows[0]["Phone"].ToString().Trim() + "'");
                    QueryBuilderObject.SetField("Fax", "'" + rowTable.Rows[0]["Fax"].ToString().Trim() + "'");
                    QueryBuilderObject.SetField("Email", "' '");
                    QueryBuilderObject.SetField("Taxeable", "0");
                    QueryBuilderObject.SetField("CustomerTypeID", rowTable.Rows[0]["CustomerTypeID"].ToString().Trim()); //HardCoded -1- Cash -2- Credit
                    QueryBuilderObject.SetField("CurrencyID", "1");
                    QueryBuilderObject.SetField("OnHold", Convert.ToInt32(Convert.ToBoolean(rowTable.Rows[0]["OnHold"].ToString().Trim())).ToString());
                    QueryBuilderObject.SetField("GPSLatitude", "0");
                    QueryBuilderObject.SetField("GPSLongitude", "0");
                    QueryBuilderObject.SetField("StreetID", "0");
                    QueryBuilderObject.SetField("StreetAddress", "0");
                    QueryBuilderObject.SetField("InActive", "0");
                    QueryBuilderObject.SetField("Notes", "0");
                    QueryBuilderObject.SetField("SkipCreditCheck", "0");
                    QueryBuilderObject.SetField("PaymentTermID", rowTable.Rows[0]["PaymentTermID"].ToString().Trim());
                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    err = QueryBuilderObject.InsertQueryString("CustomerOutlet", db_vms);

                    QueryBuilderObject.SetField("CustomerID", ShouldBeMainCustID);
                    QueryBuilderObject.SetField("OutletID", ShouldBeTheNewOutletID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + rowTable.Rows[0]["Description"].ToString().Trim() + "'");
                    QueryBuilderObject.SetField("Address", "'" + rowTable.Rows[0]["Address"].ToString().Trim() + "'");
                    err = QueryBuilderObject.InsertQueryString("CustomerOutletLanguage", db_vms);
                    #endregion

                    #region UPDATING CUSTOMER TABLES
                    QueryBuilderObject.SetField("CustomerID", ShouldBeMainCustID);
                    QueryBuilderObject.SetField("OutletID", ShouldBeTheNewOutletID);
                    err = QueryBuilderObject.UpdateQueryString("[Transaction]", "CustomerID=" + ExistingCustomerID + " and OutletID=" + ExistingOutletID + "", db_vms);

                    QueryBuilderObject.SetField("CustomerID", ShouldBeMainCustID);
                    QueryBuilderObject.SetField("OutletID", ShouldBeTheNewOutletID);
                    err = QueryBuilderObject.UpdateQueryString("[TransactionDetail]", "CustomerID=" + ExistingCustomerID + " and OutletID=" + ExistingOutletID + "", db_vms);

                    QueryBuilderObject.SetField("CustomerID", ShouldBeMainCustID);
                    QueryBuilderObject.SetField("OutletID", ShouldBeTheNewOutletID);
                    err = QueryBuilderObject.UpdateQueryString("[CustomerPayment]", "CustomerID=" + ExistingCustomerID + " and OutletID=" + ExistingOutletID + "", db_vms);

                    QueryBuilderObject.SetField("CustomerID", ShouldBeMainCustID);
                    QueryBuilderObject.SetField("OutletID", ShouldBeTheNewOutletID);
                    err = QueryBuilderObject.UpdateQueryString("[CustomerPrice]", "CustomerID=" + ExistingCustomerID + " and OutletID=" + ExistingOutletID + "", db_vms);

                    QueryBuilderObject.SetField("CustomerID", ShouldBeMainCustID);
                    QueryBuilderObject.SetField("OutletID", ShouldBeTheNewOutletID);
                    err = QueryBuilderObject.UpdateQueryString("[CustomerOutletGroup]", "CustomerID=" + ExistingCustomerID + " and OutletID=" + ExistingOutletID + "", db_vms);

                    QueryBuilderObject.SetField("CustomerID", ShouldBeMainCustID);
                    QueryBuilderObject.SetField("OutletID", ShouldBeTheNewOutletID);
                    err = QueryBuilderObject.UpdateQueryString("[CustomerPromotion]", "CustomerID=" + ExistingCustomerID + " and OutletID=" + ExistingOutletID + "", db_vms);

                    QueryBuilderObject.SetField("CustomerID", ShouldBeMainCustID);
                    QueryBuilderObject.SetField("OutletID", ShouldBeTheNewOutletID);
                    err = QueryBuilderObject.UpdateQueryString("[CustOutTerritory]", "CustomerID=" + ExistingCustomerID + " and OutletID=" + ExistingOutletID + "", db_vms);

                    QueryBuilderObject.SetField("CustomerID", ShouldBeMainCustID);
                    QueryBuilderObject.SetField("OutletID", ShouldBeTheNewOutletID);
                    err = QueryBuilderObject.UpdateQueryString("[RouteCustomer]", "CustomerID=" + ExistingCustomerID + " and OutletID=" + ExistingOutletID + "", db_vms);

                    string ChildAccountID = string.Empty;
                    string ParentAccountID = string.Empty;
                    string checkAccountExist = GetFieldValue("AccountCust", "AccountID", "CustomerID=" + ShouldBeMainCustID + "", db_vms).Trim();
                    if (checkAccountExist.Equals(string.Empty))
                    {
                        ParentAccountID = GetFieldValue("AccountCust", "AccountID", "CustomerID=" + ExistingCustomerID + "", db_vms).Trim();
                        QueryBuilderObject.SetField("CustomerID", ShouldBeMainCustID);
                        err = QueryBuilderObject.UpdateQueryString("[AccountCust]", "CustomerID=" + ExistingCustomerID + "", db_vms);
                    }
                    else
                    {

                        ParentAccountID = checkAccountExist;
                        string DeleteAccountCust = string.Format("delete from AccountCust where customerID=" + ExistingCustomerID + "");
                        qry = new InCubeQuery(DeleteAccountCust, db_vms);
                        err = qry.ExecuteNonQuery();
                    }

                    ChildAccountID = GetFieldValue("AccountCustOut", "AccountID", "CustomerID=" + ExistingCustomerID + " and OutletID=" + ExistingOutletID + "", db_vms).Trim();


                    QueryBuilderObject.SetField("CustomerID", ShouldBeMainCustID);
                    QueryBuilderObject.SetField("OutletID", ShouldBeTheNewOutletID);
                    err = QueryBuilderObject.UpdateQueryString("[AccountCustOut]", "CustomerID=" + ExistingCustomerID + " and OutletID=" + ExistingOutletID + "", db_vms);

                    QueryBuilderObject.SetField("ParentAccountID", ParentAccountID);
                    err = QueryBuilderObject.UpdateQueryString("[Account]", "AccountID=" + ChildAccountID + "", db_vms);

                    QueryBuilderObject.SetField("TargetCustomerID", ShouldBeMainCustID);
                    QueryBuilderObject.SetField("TargetOutletID", ShouldBeTheNewOutletID);
                    err = QueryBuilderObject.UpdateQueryString("[EmployeeKeyHistory]", "TargetCustomerID=" + ExistingCustomerID + " and TargetOutletID=" + ExistingOutletID + "", db_vms);

                    QueryBuilderObject.SetField("UsedForCustomerID", ShouldBeMainCustID);
                    QueryBuilderObject.SetField("UsedForOutletID", ShouldBeTheNewOutletID);
                    err = QueryBuilderObject.UpdateQueryString("[EmployeeKeyHistory]", "UsedForCustomerID=" + ExistingCustomerID + " and UsedForOutletID=" + ExistingOutletID + "", db_vms);

                    #endregion

                    #region DELETE THE EXISTING CUSTOMER

                    string DeleteCustomerLang = string.Format("delete from CustomerOutletLanguage where customerID=" + ExistingCustomerID + " and OutletID=" + ExistingOutletID + "");
                    string DeleteCustomer = string.Format("delete from CustomerOutlet where customerID=" + ExistingCustomerID + " and OutletID=" + ExistingOutletID + "");
                    qry = new InCubeQuery(DeleteCustomerLang, db_vms);
                    err = qry.ExecuteNonQuery();
                    qry = new InCubeQuery(DeleteCustomer, db_vms);
                    err = qry.ExecuteNonQuery();
                    if (err != InCubeErrors.Success)
                    {

                    }
                    #endregion

                }
            }
            catch
            {

            }

        }
    }
}