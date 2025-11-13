using InCubeIntegration_DAL;
using InCubeLibrary;
using Microsoft.Dynamics.GP.eConnect;
using Microsoft.Dynamics.GP.eConnect.Serialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace InCubeIntegration_BL
{
    public class IntegrationESF : IntegrationBase
    {
        QueryBuilder QueryBuilderObject = new QueryBuilder();
        string sConnectionString = "";
        InCubeErrors err = InCubeErrors.Error;
        private long UserID;
        string DateFormat = "MM/dd/yyyy HH:mm:ss";
        string StockDateFormat = "yyyy/MM/dd";
        InCubeQuery qry;
        CultureInfo EsES = new CultureInfo("es-ES");
        public IntegrationESF(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            db_ERP = new InCubeDatabase();
            InCubeErrors err = db_ERP.Open("TGT", "GP_TGT");
            if (err != InCubeErrors.Success)
            {
                WriteMessage("Unable to connect to GP database");
                return;
            }
            db_ERP2 = new InCubeDatabase();
            err = db_ERP2.Open("ESF", "GP_ESF");
            if (err != InCubeErrors.Success)
            {
                WriteMessage("Unable to connect to Staging database");
                return;
            }
            UserID = CurrentUserID;
        }

        #region SendInvoices
        private bool FillCustomerInfoFromGP(string CustomerCode, taSopHdrIvcInsert salesHdr, InCubeDatabase db)
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

                InCubeQuery inCubeQuery = new InCubeQuery(db, QueryString);
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
, T.LPONumber
,T.NetTotal HeaderNet, T.GrossTotal HeaderGross, T.Discount HeaderDiscount, T.Tax HeaderTax, T.DivisionID
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
TD.Tax, TD.ExciseTax, TD.UsedPriceListID, ISNULL(P.Width,3) Width
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
                InCubeDatabase db;
                int processID = 0, totalSuccess = 0, totalFailure = 0, CustomerID = 0, OutletID = 0, DivisionID = 0, SalesTransactionTypeID = 0, PackStatusID = 0, LINSEQ = 0, PackID = 0;
                short SOPTYPE = 0;
                string TransactionID = "", CustomerName = "", CustomerCode = "", WarehouseCode = "", EmployeeCode = "", QryStr = "", result = "", LPONumber = "", _DOCID = "";
                string ItemCode = "", packCode = "", STRPack = "";
                decimal TOTAL = 0, DiscountTotal = 0, TaxTotal = 0, HeaderGross = 0, HeaderDiscount = 0, HeaderTax = 0, HeaderNet = 0, Width = 0, OriginalDiscount = 0, MiscDiscount = 0, LineMisc = 0;
                decimal Quantity = 0, LineDiscount = 0, DiscountPerOne = 0, BaseUOMPrice = 0, LineTax = 0, XtndPrice = 0, LineExcise = 0;
                DateTime ExpiryDate = DateTime.Today, TransactionDate;
                SOPTransactionType salesOrder = new SOPTransactionType();
                taSopHdrIvcInsert salesHdr = new taSopHdrIvcInsert();
                DataTable dtDetails = new DataTable();
                List<taSopLineIvcInsert_ItemsTaSopLineIvcInsert> LineItems = new List<taSopLineIvcInsert_ItemsTaSopLineIvcInsert>();
                List<taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert> TaxLines = new List<taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert>();
                List<taSopLotAuto_ItemsTaSopLotAuto> LotLines = new List<taSopLotAuto_ItemsTaSopLotAuto>();
                taSopLineIvcInsert_ItemsTaSopLineIvcInsert salesLine = new taSopLineIvcInsert_ItemsTaSopLineIvcInsert();
                taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert taxLine = new taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert();
                taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert exciseLine = new taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert();
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
                        DivisionID = Convert.ToInt32(dtHeader.Rows[m]["DivisionID"]);
                        if (DivisionID == 1)
                        {
                            db = db_ERP;
                            sConnectionString = db_ERP.GetConnection().ConnectionString;
                        }
                        else
                        {
                            db = db_ERP2;
                            sConnectionString = db_ERP2.GetConnection().ConnectionString;
                        }

                        ReportProgress("Sending Transaction: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, TransactionID);
                        filters.Add(9, CustomerID.ToString() + ":" + OutletID.ToString());
                        filters.Add(10, SendTax ? "1" : "0");
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        TransactionDate = Convert.ToDateTime(dtHeader.Rows[m]["TransactionDate"]);
                        CustomerCode = dtHeader.Rows[m]["CustomerCode"].ToString();
                        string OutletCode = "";
                        if (CoreGeneral.Common.GeneralConfigurations.SiteSymbol.ToLower() == "esfnew")
                        {
                            string[] CustomerCodeParts = CustomerCode.Split(new char[] { '_' });
                            if (CustomerCodeParts.Length == 2)
                            {
                                CustomerCode = CustomerCodeParts[0];
                                OutletCode = CustomerCodeParts[1];
                            }
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
                        salesHdr.PRSTADCD = OutletCode.ToString().Trim();
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
                        if (!FillCustomerInfoFromGP(CustomerCode, salesHdr, db))
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

                            exciseLine = new taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert();
                            exciseLine.LNITMSEQ = LINSEQ;
                            exciseLine.CUSTNMBR = CustomerCode.ToString().Trim();
                            exciseLine.TAXDTLID = CoreGeneral.Common.GeneralConfigurations.EXCISEDTLID;
                            exciseLine.SOPTYPE = SOPTYPE;

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
                                LineExcise = GetRoundedDecimal(dtDetails.Rows[i]["ExciseTax"], 2);
                            }
                            else
                            {
                                LineTax = 0;
                                LineExcise = 0;
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
                            TaxTotal += (LineTax + LineExcise);
                            TOTAL += XtndPrice;

                            salesLine.XTNDPRCE = XtndPrice;
                            salesLine.UNITPRCE = BaseUOMPrice;
                            salesLine.MRKDNAMTSpecified = true;

                            if (SendTax)
                            {
                                salesLine.TAXAMNT = LineTax + LineExcise;
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
                            exciseLine.SALESAMT = XtndPrice;
                            exciseLine.STAXAMNT = LineExcise;

                            if (SendTax && LineTax > 0)
                                TaxLines.Add(taxLine);

                            if (SendTax && LineExcise > 0)
                                TaxLines.Add(exciseLine);

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

                        //ExportSentTransactionDetails(dtTransDetails);

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
                            inCubeQuery = new InCubeQuery(db, string.Format("UPDATE SOP10200 SET SALSTERR = '{0}', SLPRSNID = '{1}' WHERE SOPNUMBE = '{2}' AND SOPTYPE = {3}", salesHdr.SALSTERR, salesHdr.SLPRSNID, TransactionID, SOPTYPE));
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

            object field = new object();
            DateTime date, ChDate;
            string PaymentID = "", PaymentComment = "", ChNumber = "", CustomerCode = "", SalesPersonCode = "", CHEKBKID = "";
            int DivisionID = 0, PaymentType = 0;
            decimal Amount = 0.0m;
            RMCashReceiptsType CashType = new RMCashReceiptsType();

            string SalespersonFilter = "";
            if (!AllSalespersons)
            {
                SalespersonFilter += "AND CP.EmployeeID = " + Salesperson;
            }

            string QueryString = string.Format(@"SELECT 
CP.CustomerPaymentID AS PaymentID, 
SUM(CP.AppliedAmount) AS Amount, 
CP.PaymentTypeID AS PaymentType, 
CP.VoucherNumber, 
CP.VoucherDate, 
CO.CustomerCode, 
CP.PaymentDate,
E.EmployeeCode,
CP.DivisionID,
DC.CHEKBKID

FROM CustomerPayment CP
INNER JOIN CustomerOutlet CO ON CO.CustomerID = CP.CustomerID AND CO.OutletID = CP.OutletID
INNER JOIN Employee E ON E.EmployeeID = CP.EmployeeID
INNER JOIN CustomerOutletLanguage COL ON COL.CustomerID = CO.CustomerID AND COL.OutletID = CO.OutletID AND COL.LanguageID = 1
LEFT JOIN DivisionCheckBook DC ON DC.DivisionID = CP.DivisionID AND DC.PaymentTypeID = CP.PaymentTypeID

WHERE CP.PaymentTypeID < 4 AND CP.PaymentStatusID <> 5 AND CP.Synchronized = 0 
AND CP.PaymentDate >= '{0}'
AND CP.PaymentDate <= DATEADD(DD,1,'{1}')
{2}

GROUP BY CP.DivisionID,CP.CustomerPaymentID,CP.PaymentTypeID,CP.VoucherNumber,CP.VoucherDate,CP.BankID
,CO.CustomerCode,CP.PaymentDate,E.EmployeeCode,COL.Description,CP.DivisionID,DC.CHEKBKID"
, FromDate.ToString("yyyy/MM/dd"), ToDate.ToString("yyyy/MM/dd"), SalespersonFilter);

            InCubeQuery incubeQuery = new InCubeQuery(db_vms, QueryString);
            DataTable dtPayments = new DataTable();
            if (incubeQuery.Execute() == InCubeErrors.Success)
            {
                dtPayments = incubeQuery.GetDataTable();
            }

            for (int iii = 0; iii < dtPayments.Rows.Count; iii++)
            {
                bool success = false;
                try
                {
                    
                    DivisionID = Convert.ToInt16(dtPayments.Rows[iii]["DivisionID"]);
                    if (DivisionID == 1)
                    {
                        sConnectionString = db_ERP.GetConnection().ConnectionString;
                    }
                    else
                    {
                        sConnectionString = db_ERP2.GetConnection().ConnectionString;
                    }
                    PaymentID = dtPayments.Rows[iii]["PaymentID"].ToString();
                    Amount = Convert.ToDecimal(dtPayments.Rows[iii]["Amount"]);
                    PaymentType = Convert.ToInt16(dtPayments.Rows[iii]["PaymentType"]);

                    ChNumber = dtPayments.Rows[iii]["VoucherNumber"].ToString();
                    if (ChNumber.Length > 12)
                        ChNumber = ChNumber.Substring(ChNumber.Length - 12, 12);
                    field = dtPayments.Rows[iii]["VoucherDate"];
                    if (field != null && !string.IsNullOrEmpty(field.ToString()) && DateTime.TryParse(field.ToString(), out ChDate))
                        ChNumber += "-" + ChDate.ToString("ddMMMyy");

                    CustomerCode = dtPayments.Rows[iii]["CustomerCode"].ToString();
                    if (CoreGeneral.Common.GeneralConfigurations.SiteSymbol.ToLower() == "esfnew")
                    {
                        string[] CustomerCodeParts = CustomerCode.Split(new char[] { '_' });
                        if (CustomerCodeParts.Length == 2)
                            CustomerCode = CustomerCodeParts[0];
                    }

                    date = Convert.ToDateTime(dtPayments.Rows[iii]["PaymentDate"]);
                    SalesPersonCode = dtPayments.Rows[iii]["EmployeeCode"].ToString();

                    PaymentComment = SalesPersonCode.ToString() + "-";
                    InCubeQuery qry = new InCubeQuery(db_vms, string.Format(@"IF ((SELECT COUNT(*) FROM CustomerPayment WHERE CustomerPaymentID = '{0}') = 1)
	SELECT TransactionID FROM CustomerPayment WHERE CustomerPaymentID = '{0}';
ELSE
	SELECT 'MULTIPLE';", PaymentID));
                    if (qry.ExecuteScalar(ref field) == InCubeErrors.Success)
                        PaymentComment += field.ToString();

                    if (PaymentComment.Length > 30)
                        PaymentComment = PaymentComment.Substring(PaymentComment.Length - 30, 30);

                    CHEKBKID = dtPayments.Rows[iii]["CHEKBKID"].ToString();

                    if (PaymentType == 1)// "Cash"
                    {
                        #region Cash

                        taRMCashReceiptInsert CustomerPaymentCash = new taRMCashReceiptInsert();
                        CustomerPaymentCash.CUSTNMBR = CustomerCode;
                        CustomerPaymentCash.DOCNUMBR = PaymentID;
                        CustomerPaymentCash.ORTRXAMT = Amount;
                        CustomerPaymentCash.GLPOSTDT = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.DOCDATE = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.CSHRCTYP = 1;//0=check , 1=cash , 2= credit card
                        CustomerPaymentCash.CHEKBKID = CHEKBKID; //"DX -CASH";
                        CustomerPaymentCash.CURNCYID = "DHS";
                        string Batch = "RV" + date.ToString("dd/MM/yy").Replace("/", "") + SalesPersonCode.ToString();
                        CustomerPaymentCash.BACHNUMB = Batch;
                        CustomerPaymentCash.CURNCYID = "";
                        CustomerPaymentCash.CREATEDIST = 1;

                        CustomerPaymentCash.TRXDSCRN = PaymentComment;
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
                        catch (eConnectException ex)
                        {
                            throw new Exception(ex.Message);
                        }
                        #endregion
                    }
                    else if (PaymentType == 2)// "Current Dated Cheque"
                    {
                        #region Current Dated Cheque

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
                        CustomerPaymentCash.CHEKBKID = CHEKBKID;
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
                        catch (eConnectException ex)
                        {
                            throw new Exception(ex.Message);
                        }
                        #endregion
                    }
                    else if (PaymentType == 3)// "Post Dated Cheque"
                    {
                        #region Current Dated Cheque

                        taRMCashReceiptInsert CustomerPaymentCash = new taRMCashReceiptInsert();
                        CustomerPaymentCash.CUSTNMBR = CustomerCode;
                        CustomerPaymentCash.DOCNUMBR = PaymentID;
                        CustomerPaymentCash.ORTRXAMT = Amount;
                        CustomerPaymentCash.GLPOSTDT = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.DOCDATE = date.ToString("yyyy-MM-dd");
                        CustomerPaymentCash.CSHRCTYP = 0;
                        CustomerPaymentCash.CURNCYID = "DHS";
                        CustomerPaymentCash.BACHNUMB = "PDC-" + DateTime.Now.ToString("MMM") + GetSalespersonCode(SalesPersonCode);
                        CustomerPaymentCash.CURNCYID = "";
                        CustomerPaymentCash.CREATEDIST = 1;
                        CustomerPaymentCash.TRXDSCRN = PaymentComment;
                        CustomerPaymentCash.CHEKNMBR = ChNumber;
                        CustomerPaymentCash.CHEKBKID = CHEKBKID;
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
                        catch (eConnectException ex)
                        {
                            throw new Exception(ex.Message);
                        }
                        #endregion
                    }
                    success = true;
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.errorPay);
                }
                finally
                {
                    if (success)
                    {
                        InCubeQuery UpdateQuery = new InCubeQuery(db_vms, "update  CustomerPayment set Synchronized=1 where CustomerPaymentID='" + PaymentID + "'");
                        err = UpdateQuery.Execute();
                        WriteMessage("\r\n" + PaymentID.ToString() + " - OK");
                    }
                    else
                    {
                        WriteMessage("\r\n" + PaymentID.ToString() + " - FAILED!");
                    }
                }
            }
            return ret;
        }
        public int SerializeSalesOrderObjectSendPayment_old(string filename, bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate)
        {
            int ret = 0;
            WriteMessage("\r\n" + "Send Payment");

            object field = new object();
            DateTime date;
            string PaymentID = "";
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
                                         CustomerOutletLanguage.Description,
                                         CustomerPayment.DivisionID

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


            QueryString += @"      GROUP BY   CustomerPayment.DivisionID,CustomerPayment.CustomerPaymentID, PaymentTypeLanguage.PaymentTypeID, PaymentTypeLanguage.LanguageID, 
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
                    err = CustomerPaymentQuery.GetField("DivisionID", ref field);
                    if (field.ToString() == "1")
                    {
                        sConnectionString = db_ERP.GetConnection().ConnectionString;
                    }
                    else
                    {
                        sConnectionString = db_ERP2.GetConnection().ConnectionString;
                    }
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
                                         CustomerOutletLanguage.Description,
                                         CustomerUnallocatedPayment.DevisionID DivisionID,
                                         DC.CHEKBKID

                                   FROM  CustomerOutlet RIGHT OUTER JOIN
                                         CustomerUnallocatedPayment ON CustomerOutlet.OutletID = CustomerUnallocatedPayment.OutletID AND 
                                         CustomerOutlet.CustomerID = CustomerUnallocatedPayment.CustomerID LEFT OUTER JOIN
                                         PaymentTypeLanguage ON CustomerUnallocatedPayment.PaymentTypeID = PaymentTypeLanguage.PaymentTypeID INNER JOIN
                                         Employee ON CustomerUnallocatedPayment.EmployeeID = Employee.EmployeeID INNER JOIN
                                         CustomerOutletLanguage ON CustomerOutlet.CustomerID = CustomerOutletLanguage.CustomerID AND 
                                         CustomerOutlet.OutletID = CustomerOutletLanguage.OutletID 
                                         LEFT JOIN DivisionCheckBook DC ON DC.DivisionID = CustomerUnallocatedPayment.DevisionID AND DC.PaymentTypeID = CustomerUnallocatedPayment.PaymentTypeID

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
                                              CustomerUnallocatedPayment.VoucherNumber, CustomerUnallocatedPayment.VoucherDate, CustomerUnallocatedPayment.BankID, CustomerOutlet.CustomerCode, CustomerUnallocatedPayment.DevisionID,
                                              CustomerUnallocatedPayment.Synchronised, CustomerUnallocatedPayment.PaymentDate,Employee.EmployeeCode, CustomerUnallocatedPayment.PaymentTypeID,CustomerOutletLanguage.Description,DC.CHEKBKID";



            InCubeQuery CustomerPaymentQuery = new InCubeQuery(db_vms, QueryString);

            err = CustomerPaymentQuery.Execute();
            err = CustomerPaymentQuery.FindFirst();
            while (err == InCubeErrors.Success)
            {
                //                                                                                                     0                                         1                                          2                                 3                               4                    5                           6                        7   
                try
                {
                    CustomerPaymentQuery.GetField("DivisionID", ref field);
                    if (field.ToString() == "1")
                    {
                        sConnectionString = db_ERP.GetConnection().ConnectionString;
                    }
                    else
                    {
                        sConnectionString = db_ERP2.GetConnection().ConnectionString;
                    }
                    Check = false;
                    int CO = CustomerPaymentQuery.GetDataTable().Rows.Count;
                    CustomerPaymentQuery.GetField("CHEKBKID", ref field);
                    string CHEKBKID = field.ToString();
                    CustomerPaymentQuery.GetField(0, ref field);
                    PaymentID = field.ToString();
                    CustomerPaymentQuery.GetField(1, ref field);
                    Amount = decimal.Parse(field.ToString());
                    CustomerPaymentQuery.GetField(2, ref field);
                    PaymentType = field.ToString();
                    CustomerPaymentQuery.GetField(3, ref field);
                    ChNumber = field.ToString();
                    CustomerPaymentQuery.GetField(4, ref field);
                    ChDate = field;
                    CustomerPaymentQuery.GetField(5, ref field);
                    if (field.ToString() == "")
                    {
                        BankID = "";
                    }
                    else
                    {
                        BankID = field.ToString();
                    }
                    CustomerPaymentQuery.GetField(6, ref field);
                    CustomerCode = field.ToString();
                    string[] CustomerCodeParts = CustomerCode.Split(new char[] { '_' });
                    if (CustomerCodeParts.Length == 2)
                        CustomerCode = CustomerCodeParts[0];

                    CustomerPaymentQuery.GetField(7, ref field);
                    date = DateTime.Parse(field.ToString());
                    CustomerPaymentQuery.GetField(8, ref SalesPersonCode);

                    CustomerPaymentQuery.GetField(9, ref CustomerName);

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
                        CustomerPaymentCash.CHEKBKID = CHEKBKID; //"DX -CASH";
                        CustomerPaymentCash.CURNCYID = "DHS";
                        string Batch = "RV" + date.ToString("dd/MM/yy").Replace("/", "") + SalesPersonCode.ToString();
                        CustomerPaymentCash.BACHNUMB = Batch;
                        CustomerPaymentCash.CURNCYID = "";
                        CustomerPaymentCash.CREATEDIST = 1;

                        CustomerPaymentCash.TRXDSCRN = SalesPersonCode + "-DownPayment";
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
                        CustomerPaymentCash.CHEKBKID = CHEKBKID;
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
                        CustomerPaymentCash.CHEKBKID = CHEKBKID;
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
                        UpdateQuery.Execute();
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

            return ret;
        }


    }
}