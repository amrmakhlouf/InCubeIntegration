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
    public class IntegrationAINFoodTruck : IntegrationBase
    {
        QueryBuilder QueryBuilderObject = new QueryBuilder();
        InCubeErrors err;
        string dairyConnectionString = "";
        string poultryConnectionString = "";
        private long UserID;

        public IntegrationAINFoodTruck(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            UserID = CurrentUserID;
        }

        public override void SendInvoices()
        {
            SendTransactions(CoreGeneral.Common.StartupPath + "\\E-Connect\\", Filters.EmployeeID == -1, Filters.EmployeeID.ToString(), Filters.FromDate, Filters.ToDate, IntegrationField.Sales_S);
        }
        public override void SendReturn()
        {
            SendTransactions(CoreGeneral.Common.StartupPath + "\\E-Connect\\", Filters.EmployeeID == -1, Filters.EmployeeID.ToString(), Filters.FromDate, Filters.ToDate, IntegrationField.Returns_S);
        }
        private void SetSynchronizedFlag(string TransactionID, int DivisionID, int TransactionTypeID)
        {
            try
            {
                InCubeQuery insertQuery = new InCubeQuery(db_vms, string.Format("INSERT INTO SentTransactions (TransactionID,DivisionID,TransactionTypeID,SendingDate) VALUES ('{0}',{1},{2},GETDATE())", TransactionID, DivisionID, TransactionTypeID));
                err = insertQuery.Execute();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void FillCustomerInfoFromGP(string CustomerCode, taSopHdrIvcInsert salesHdr, InCubeDatabase db_GP)
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

                InCubeQuery inCubeQuery = new InCubeQuery(db_GP, QueryString);
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
        public Result SendTransactions(string filename, bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate, IntegrationField Field)
        {
            try
            {
                if (db_ERP == null)
                {
                    db_ERP = new InCubeDatabase();
                    InCubeErrors err = db_ERP.Open("DairyGP", "InVan");
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("Unable to connect to Dairy GP database");
                        return Result.Failure;
                    }
                    dairyConnectionString = db_ERP.GetConnection().ConnectionString;
                }
                if (db_ERP2 == null)
                {
                    db_ERP2 = new InCubeDatabase();
                    InCubeErrors err = db_ERP2.Open("PoultryGP", "InVan");
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("Unable to connect to poultry GP database");
                        return Result.Failure;
                    }
                    poultryConnectionString = db_ERP2.GetConnection().ConnectionString;
                }
            }
            catch
            {

            }

            Result res = Result.UnKnown;
            try
            {
                //Declarations
                InCubeQuery inCubeQuery;
                InCubeDatabase db_GP;
                string GP_ConnectionString = "";
                int processID = 0, totalSuccess = 0, totalFailure = 0, DivisionID = 0, CustomerID = 0, OutletID = 0, CustomerTypeID = 0, FOCTypeID = 0, PackStatusID = 0, CreationReason = 0;
                short SOPTYPE = 0;
                string TransactionID = "", CustomerName = "", CustomerCode = "", WarehouseCode = "", EmployeeCode = "", QryStr = "", result = "";
                string SLPRSNID = "", _DOCID = "";
                DateTime TransactionDate;
                SOPTransactionType salesOrder = new SOPTransactionType();
                taSopHdrIvcInsert salesHdr = new taSopHdrIvcInsert();
                DataTable dtDetails = new DataTable();
                List<taSopLineIvcInsert_ItemsTaSopLineIvcInsert> LineItems = new List<taSopLineIvcInsert_ItemsTaSopLineIvcInsert>();
                List<taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert> TaxLines = new List<taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert>();

                decimal Quantity = 0, LineDiscount = 0, unitPrice = 0, exclusivePrice = 0, BasePrice = 0, LineTax = 0;
                string ItemCode = "", packCode = "", STRPack = "";
                taSopLineIvcInsert_ItemsTaSopLineIvcInsert salesLine = new taSopLineIvcInsert_ItemsTaSopLineIvcInsert();
                taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert taxLine = new taSopLineIvcTaxInsert_ItemsTaSopLineIvcTaxInsert();
                eConnectType eConnect = new eConnectType();
                string strDOC = "";

                switch (Field)
                {
                    case IntegrationField.Sales_S:
                        WriteMessage("\r\n" + "Sending Invoices");
                        SOPTYPE = 3;
                        break;

                    case IntegrationField.Returns_S:
                        WriteMessage("\r\n" + "Sending Returns");
                        SOPTYPE = 4;
                        break;
                }

                string EmpFilter = "";
                if (!AllSalespersons)
                {
                    EmpFilter = "AND T.EmployeeID = " + Salesperson;
                }

                string QueryString = string.Format(@"SELECT T.TransactionID, T.DivisionID, T.CreationReason, T.TransactionDate,T.CustomerID,T.OutletID,CO.CustomerTypeID,COL.Description
,CASE T.DivisionID WHEN 1 THEN CO.CustomerCode ELSE CO.Barcode END CustomerCode
,CASE T.DivisionID WHEN 1 THEN E.EmployeeCode ELSE E.NationalIDNumber END EmployeeCode
,CASE T.DivisionID WHEN 1 THEN W.WarehouseCode ELSE W.Barcode END WarehouseCode
FROM        
(SELECT DISTINCT T.TransactionID,T.CreationReason,T.TransactionDate,T.CustomerID,T.OutletID,IC.DivisionID,T.WarehouseID,T.EmployeeID
FROM [Transaction]  T
INNER JOIN TransactionDetail TD ON TD.TransactionID = T.TransactionID AND TD.CustomerID = T.CustomerID AND TD.OutletID = T.OutletID
INNER JOIN Pack P ON P.PackID = TD.PackID
INNER JOIN Item I ON I.ItemID = P.ItemID
INNER JOIN ItemCategory IC ON IC.ItemCategoryID = I.ItemCategoryID
LEFT JOIN SentTransactions ST ON ST.TransactionID = T.TransactionID AND ST.DivisionID = IC.DivisionID
AND ST.TransactionTypeID = {4}
WHERE (T.TransactionTypeID IN ({0}) OR T.CreationReason = 2) AND T.Voided = 0 {3}
AND T.TransactionDate >= '{1}' AND T.TransactionDate < DATEADD(DD,1,'{2}')
AND ST.TransactionID IS NULL) T
INNER JOIN CustomerOutletLanguage COL ON COL.CustomerID = T.CustomerID AND COL.OutletID = T.OutletID AND COL.LanguageID = 1
INNER JOIN CustomerOutlet CO ON CO.CustomerID = T.CustomerID AND CO.OutletID = T.OutletID
INNER JOIN Warehouse W ON W.WarehouseID = T.WarehouseID 
INNER JOIN Employee E ON E.EmployeeID = T.EmployeeID"
, Field == IntegrationField.Sales_S ? "1,3" : "2,4", FromDate.ToString("yyyy/MM/dd")
, ToDate.ToString("yyyy/MM/dd"), EmpFilter, Field == IntegrationField.Sales_S ? "1" : "2");

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
                        DivisionID = Convert.ToInt32(dtHeader.Rows[m]["DivisionID"]);
                        CustomerID = Convert.ToInt32(dtHeader.Rows[m]["CustomerID"]);
                        OutletID = Convert.ToInt32(dtHeader.Rows[m]["OutletID"]);

                        ReportProgress("Sending invoice: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + " (" + (DivisionID == 1 ? "Dairy" : "Poultry") + "): ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, TransactionID);
                        filters.Add(9, DivisionID.ToString());
                        filters.Add(10, CustomerID.ToString());
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        decimal TOTAL = 0;
                        int LINSEQ = 0;

                        TransactionDate = Convert.ToDateTime(dtHeader.Rows[m]["TransactionDate"]);
                        CustomerCode = dtHeader.Rows[m]["CustomerCode"].ToString();
                        EmployeeCode = dtHeader.Rows[m]["EmployeeCode"].ToString();
                        CustomerName = dtHeader.Rows[m]["Description"].ToString();
                        WarehouseCode = dtHeader.Rows[m]["WarehouseCode"].ToString();
                        CustomerTypeID = Convert.ToInt16(dtHeader.Rows[m]["CustomerTypeID"]);
                        CreationReason = Convert.ToInt16(dtHeader.Rows[m]["CreationReason"]);

                        if (DivisionID == 1)
                        {
                            db_GP = db_ERP;
                            GP_ConnectionString = dairyConnectionString;
                        }
                        else
                        {
                            db_GP = db_ERP2;
                            GP_ConnectionString = poultryConnectionString;
                        }

                        string COUNTRY = GetFieldValue("IV40700", "STATE", " rtrim(ltrim(LOCNCODE)) = '" + EmployeeCode + "'", db_GP);
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
                        FillCustomerInfoFromGP(CustomerCode, salesHdr, db_GP);

                        QryStr = string.Format(@"SELECT TD.Quantity,0 FOCTypeID,ISNULL(TD.PackStatusID,0) PackStatusID,I.ItemCode,PTL.Description UOM,IL.Description ItemName,TD.Discount
,TD.Price,TD.BasePrice
FROM TransactionDetail TD 
INNER JOIN Pack P ON P.PackID = TD.PackID
INNER JOIN Item I ON I.ItemID = P.ItemID
INNER JOIN ItemCategory IC ON IC.ItemCategoryID = I.ItemCategoryID
INNER JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1
INNER JOIN ItemLanguage IL ON IL.ItemID = I.ItemID AND IL.LanguageID = 1
WHERE TD.TransactionID = '{0}' AND TD.CustomerID = {1} AND TD.OutletID = {2} AND IC.DivisionID = {3}"
, TransactionID, CustomerID, OutletID, DivisionID);

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
                            ItemCode = dtDetails.Rows[i]["ItemCode"].ToString().Trim();
                            packCode = dtDetails.Rows[i]["UOM"].ToString().Trim();
                            STRPack = dtDetails.Rows[i]["ItemName"].ToString().Trim().Split('_')[0];
                            LineDiscount = 0;
                            unitPrice = Convert.ToDecimal(dtDetails.Rows[i]["Price"]);
                            LineTax = 0;
                            BasePrice = Convert.ToDecimal(dtDetails.Rows[i]["BasePrice"]);
                            LINSEQ = LINSEQ + 16384;

                            if (Field == IntegrationField.Returns_S && CreationReason == 2)
                            {
                                unitPrice = 0;
                                PackStatusID = 3;
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
                                //salesLine.COMMNTID = SALESMODE.ToString();
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

                            exclusivePrice = decimal.Round(unitPrice * 100 / 105, 2);
                            decimal XTNDPRCE = Math.Round(Quantity * exclusivePrice, 2, MidpointRounding.AwayFromZero);
                            LineTax = (unitPrice * Quantity - XTNDPRCE);

                            TOTAL += XTNDPRCE;
                            TaxTotal += LineTax;

                            salesLine.UNITPRCE = exclusivePrice;
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
                        string fname = filename + (DivisionID == 1 ? "Dairy" : "Poultry") + "\\" + TransactionID.ToString().Trim() + ".xml";

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
                        eConCall.eConnect_EntryPoint(GP_ConnectionString, EnumTypes.ConnectionStringType.SqlClient, salesOrderDocument, EnumTypes.SchemaValidationType.None, "");


                        SetSynchronizedFlag(TransactionID, DivisionID, Field == IntegrationField.Sales_S ? 1 : 2);
                        //This query to set SALSTERR and SLPRSNID since sending to GP doesnt affect those fields
                        InCubeQuery UpdateQuery = new InCubeQuery(db_GP, "Update SOP10200 SET SALSTERR = '" + EmployeeCode.ToString() + "', SLPRSNID = '" + SLPRSNID + "' where  SOPNUMBE = '" + TransactionID.ToString() + "' AND SOPTYPE = 3");
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
                            SetSynchronizedFlag(TransactionID, DivisionID, Field == IntegrationField.Sales_S ? 1 : 2);
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
    }
}