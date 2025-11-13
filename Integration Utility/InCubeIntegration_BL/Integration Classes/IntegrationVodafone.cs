using InCubeIntegration_DAL;
using InCubeLibrary;
using SAP.Middleware.Connector;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml;


namespace InCubeIntegration_BL
{
    public class IntegrationVodafone : IntegrationBase // Live branch
    {
        private enum EmployeeType
        {
            Employee = 1,
            Salesman = 2,
            Supervisor = 4,
            Salesmanager = 9
        }
        string IntegrationOrg = "7710";
        private DateTime _modificationDate;
        private InCubeErrors configResult = InCubeErrors.NotInitialized;
        private string DateFormat = "dd/MMM/yyyy";
        private string DateFormtSAP = "dd/MM/yyyy";
        private int digitsCount = -1;
        private DataTable dtData;
        private string employeeCode = string.Empty;
        private InCubeErrors err;
        private InCubeQuery incubeQuery;
        private string param = "where CustomerID=" + 6849;
        private QueryBuilder QueryBuilderObject = new QueryBuilder();
        private string SendServerName;
        private string GetServerName;
        string StockDateFormat = "yyyy-MM-dd";
        private long UserID;
        List<string> DivisionDiscounts = new List<string>();
        List<string> GroupDiscounts = new List<string>();
        List<string> CustomerDiscounts = new List<string>();
        public IntegrationVodafone(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            UserID = CurrentUserID;
            _modificationDate = GetIntegrationModificationDateNew(db_vms);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(CoreGeneral.Common.StartupPath + "\\DataSources.xml");
            SendServerName = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'InCubeSQL']/DataSend").InnerText;
            GetServerName = SendServerName;// xmlDoc.SelectSingleNode("Connections/Connection[Name = 'InCubeSQL']/DataGet").InnerText;
        }

        public static void GetSequenceNumberFormat(string maxTransactionOrderID, ref string charactersPart, ref string numericPart)
        {
            try
            {
                char[] valueChars = new char[maxTransactionOrderID.Length];
                valueChars = maxTransactionOrderID.ToCharArray();
                for (int i = valueChars.Length - 1; i >= 0; i--)
                {
                    if (!char.IsNumber(valueChars[i]))
                    {
                        charactersPart = maxTransactionOrderID.Substring(0, i + 1);
                        numericPart = maxTransactionOrderID.Substring(i + 1);
                        return;
                    }
                }
                numericPart = maxTransactionOrderID;
            }
            catch (Exception ex)
            {
                charactersPart = string.Empty;
                numericPart = string.Empty;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        public string GetMaxCreditNoteID(string EmployeeID)
        {
            try
            {
                string MaxCreditNoteID = string.Empty;
                string numericPart = string.Empty;
                string charactersPart = string.Empty;
                long newSequence;
                int numLength;

                string MaxCreditNote = GetFieldValue("DocumentSequence", "MaxTransactionCreditNote", " EmployeeID = " + EmployeeID, db_vms);

                if (MaxCreditNote != string.Empty)
                {
                    GetSequenceNumberFormat(MaxCreditNote, ref charactersPart, ref numericPart);
                    if (numericPart.Equals(string.Empty))
                    {
                        return string.Empty;
                    }
                    numLength = numericPart.Length;
                    newSequence = (Convert.ToInt64(numericPart) + 1) % 2 == 0 ? (Convert.ToInt64(numericPart) + 1) : (Convert.ToInt64(numericPart) + 2);
                    numericPart = newSequence.ToString().PadLeft(numLength, '0');
                    MaxCreditNoteID = charactersPart + numericPart;
                }
                return MaxCreditNoteID;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                return string.Empty;
            }
        }

        //INSTEAD OF HAVING CARREFOUR ITEMS, WE NEED TO USE FF ITEM CODES, SINCE CARREFOUR ARE USING SAME ITEM CODES AS FF
        //WE ARE DELETING ALL CUSTOMERS WHEN IMPORTING CARRFOUR CUSTOMERS, WE SHOULD NOT DELETE ALL CUSTOMERS.
        //ALWAYS SEND THE LARGEST UOM IN THE ORDER DETAILS.
        //FF CODE IN THE EDI FILE IS THE REFERENCE CODE, SO WE SHOULD TAKE IT INSTEAD OF THE BARCODE.
        //DELIVERY DATE SHOULD BE TAKEN FROM THE "DEAD LINE" IN THE EDI FILE.
        // WE SHOULD TAKE THE CARREFOUR OUTLET CODE AND CONCATINATE IT WITH THE DEPARTMENT CODE IN THE EDI FILE, AND THE RESULT SHOULD BE MATCHED WITH FF CUSTOMER CODE.

        public override void SendInvoices()
        {
            try
            {
                WriteMessage("\r\n" + "Sending Invoices & Returns");
                object COMPANYCODE = "";
                object SalesOrganization = "";
                object DivisionCode = "";
                object DistributionChannel = "";
                object EmployeeCode = "";
                object DocumentType = "";
                object TransactionDate = "";
                object TransactionNumber = "";
                object HeaderDiscount = "";
                object LPO_Number = "";
                object ShipToCode = "";
                object PayerCode = "";
                object VehicleCode = "";
                object LineNumber = "";
                object ItemReasonCode = "";
                object ItemCode = "";
                object UOM = "";
                object Quantity = "";
                object Quantity2 = "";
                object Price = "";
                object TotalLineAmount = "";
                object FOC_Indicator = "";
                object ItemDiscount = "";
                object ActionType = "";
                object SoldToCode = "";
                string OriginalTransactionNumber = string.Empty;
                //THIS IS THE STREAM WRITER FOR THE INVOICES LOG

                string invoiceLog = string.Empty;

                RfcDestination dest = RfcDestinationManager.GetDestination(SendServerName);
                IRfcFunction func;
                IRfcTable impStruct2;

                func = dest.Repository.CreateFunction("ZHIB_PROCESS_SALES_INVOICE");
                //func.GetTable("ITEMTAB");
                string QueryString = @"
           select 

TR.TransactionID
FROM 
[TRANSACTION] TR 
where Synchronized=0   and 
                (isnull(TR.Notes,'0')<>'ERP') AND (TR.TransactionTypeID IN(1,2,3,4))";
                if (Filters.EmployeeID != -1)
                {
                    QueryString += " AND TR.EmployeeID = " + Filters.EmployeeID;
                }

                InCubeQuery GetSalesTransactionInformation = new InCubeQuery(db_vms, QueryString);

                err = GetSalesTransactionInformation.Execute();
                err = GetSalesTransactionInformation.FindFirst();
                while (err == InCubeErrors.Success)
                {
                    //string filename = "INV-Log" + DateTime.Today.Year.ToString() + DateTime.Today.Month.ToString() + DateTime.Today.Day.ToString() + ".log";
                    //string filePath = Application.StartupPath + "\\" + filename;
                    //if (!File.Exists(filePath))
                    //{
                    //    FileStream fs = File.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite);
                    //    fs.Close();
                    //    fs.Dispose();
                    //}
                    //StreamWriter lgRwiter = new StreamWriter(filename, true);
                    try
                    {

                        #region Get SalesTransaction Information
                        {
                            err = GetSalesTransactionInformation.GetField("TransactionID", ref TransactionNumber);

                        }
                        #endregion

                        //date = DateTime.Parse(TransactionDate.ToString());
                        //THE FOLLOWING WILL NOT INSERT THE HEADER UNLESS THE GROSS TOTAL FOR THE HEADER IS EQUAL TO THE sum(quantity*price) IN THE TRANSACTION DETAIL .
                        //string totalDetails = GetFieldValue("TransactionDetail", "isnull(sum(quantity*price),0)", "TransactionID='" + TransactionID + "'", db_vms).Trim();
                        //if (decimal.Parse(GrossTotal.ToString().Trim()) != decimal.Parse(totalDetails))
                        //{
                        //    lgRwiter.WriteLine("INVOICE HEADER , THE GROSS TOTAL IS NOT EQUAL TO THE SUM OF DETAILS AMOUNT FOR TRANSACTION  " + TransactionID + " SENDING DATE IS " + DateTime.Now.ToString());
                        //    lgRwiter.Close();
                        //    lgRwiter.Dispose();
                        //    WriteMessage("\r\n" + "THE GROSS TOTAL IS NOT EQUAL TO THE SUM OF DETAILS AMOUNT FOR TRANSACTION  " + TransactionID + " SENDING ABORTED ! .");
                        //    err = GetSalesTransactionInformation.FindNext();
                        //    continue;
                        //}
                        #region invoice header

                        #endregion

                        string dtlQryStr = @"
SELECT 
CO.customerid,co.outletid,d.divisionid,
O.ORGANIZATIONCODE COMPANYCODE,
O.ORGANIZATIONCODE SalesOrganization,
isnull(D.DivisionCode,'70') DivisionCode,
'' DistributionChannel,
E.EmployeeCode,
CASE(TR.NetTotal) WHEN 0 THEN 'ZHF' ELSE 
case WHEN TR.TRANSACTIONTYPEID IN (1,3) THEN 
CASE(CO.CustomerTypeID) WHEN 1 THEN 'ZHS' ELSE 
CASE(CO.CustomerTypeID) WHEN 2 THEN 'ZHE' ELSE  'XX' 
END END 
ELSE case WHEN TR.TRANSACTIONTYPEID IN (2,4) THEN CASE(TD.PackStatusID) WHEN 0 THEN 'ZHR' ELSE 
CASE(TD.PackStatusID) WHEN 1 THEN 'ZVR' ELSE 
CASE(TD.PackStatusID) WHEN 2 THEN 'ZVR' ELSE 
CASE(TD.PackStatusID) WHEN 3 THEN 'ZHR' ELSE  'XX' END END END END END END end
DocumentType,
TR.TransactionDate,
TR.TRANSACTIONID TransactionNumber,
TR.PromotedDiscount+TR.Discount  HeaderDiscount,
TR.LPONumber LPO_Number,
CO.CustomerCode ShipToCode,
PR.PayerCode,
t.TerritoryCode VehicleCode,
ROW_NUMBER()OVER(ORDER BY I.ITEMCODE,PTL.DESCRIPTION)*10 LineNumber,
CASE(TR.NetTotal) WHEN 0 THEN '136' ELSE 
case WHEN TR.TRANSACTIONTYPEID IN (1,3) THEN 
CASE(CO.CustomerTypeID) WHEN 1 THEN '' ELSE 
CASE(CO.CustomerTypeID) WHEN 2 THEN '' ELSE  'XX' 
END END 
ELSE case WHEN TR.TRANSACTIONTYPEID IN (2,4) THEN CASE(TD.PackStatusID) WHEN 0 THEN '104' ELSE 
CASE(TD.PackStatusID) WHEN 1 THEN '102' ELSE 
CASE(TD.PackStatusID) WHEN 2 THEN '102' ELSE 
CASE(TD.PackStatusID) WHEN 3 THEN '104' ELSE  'XX' END END END END END END end ItemReasonCode,
RRL.DESCRIPTION ItemReasonCode,
I.ItemCode,
PTL.Description UOM,
 sum(TD.Quantity) Quantity,
0 Quantity2,
TD.Price,
(TD.Price* sum(TD.Quantity)-sum(TD.Discount)) TotalLineAmount,
CASE(TD.SalesTransactionTypeID) WHEN 2 THEN 1 ELSE  
CASE(TD.SalesTransactionTypeID) WHEN 3 THEN 1 ELSE 
CASE(TD.SalesTransactionTypeID) WHEN 4 THEN 1 ELSE 0 END END END
FOC_Indicator,
TD.Discount ItemDiscount,
CASE(TR.VOIDED) WHEN 1 THEN 2 ELSE 1 END ActionType,
CU.CUSTOMERCODE SoldToCode,
E.EmployeeID,TD.SalesTransactionTypeID,
CASE W.WarehouseTypeID WHEN 1 THEN '77110001' ELSE W.warehouseCode END warehouseCode
FROM 
[TRANSACTION] TR INNER JOIN TRANSACTIONDETAIL TD ON TR.TRANSACTIONID=TD.TRANSACTIONID AND TR.CUSTOMERID=TD.CUSTOMERID AND TR.OUTLETID=TD.OUTLETID

INNER JOIN EMPLOYEE E ON E.EMPLOYEEID=(CASE TR.CreationReason WHEN 1 THEN TR.EmployeeID ELSE (SELECT EmployeeID FROM CustomerLocation WHERE CustomerID = TR.CustomerID AND OutletID = TR.OutletID) END)
INNER JOIN Territory T ON T.TerritoryID = (CASE TR.CreationReason WHEN 1 THEN (SELECT TerritoryID FROM Route WHERE RouteID = TR.RouteID) ELSE (SELECT TerritoryID FROM EmployeeTerritory WHERE EmployeeID = E.EmployeeID) END)
INNER JOIN Warehouse W ON W.WarehouseID = TR.WarehouseID
INNER JOIN CUSTOMEROUTLET CO ON CO.CUSTOMERID=TR.CUSTOMERID AND CO.OUTLETID=TR.OUTLETID
INNER JOIN CUSTOMER CU ON CO.CUSTOMERID=CU.CUSTOMERID
INNER JOIN PAYER PR ON CO.CustomerID=PR.CustomerID AND PR.OUTLETID=CO.OUTLETID
inner join payerdivision pd on pr.PayerID=pd.PayerID and pd.divisionid=1
INNER JOIN ORGANIZATION O ON TR.OrganizationID=O.OrganizationID
left outer JOIN DIVISION D ON isnull(TR.DivisionID,1)=D.DivisionID
INNER JOIN PACK P ON TD.PackID=P.PACKID
INNER JOIN ITEM I ON P.ITEMID=I.ITEMID
INNER JOIN PACKTYPELANGUAGE PTL ON P.PackTypeID=PTL.PackTypeID AND PTL.LanguageID=1
LEFT OUTER JOIN ReturnReasonLanguage RRL ON TD.ReturnReason=RRL.ReturnReasonID AND RRL.LANGUAGEID=1
where Synchronized=0   and 
                (isnull(TR.Notes,'0')<>'ERP') AND (TR.TransactionTypeID in (1,2,3,4)) 
AND TR.TRANSACTIONID='" + TransactionNumber + @"'
GROUP BY
O.ORGANIZATIONCODE ,
O.ORGANIZATIONCODE ,
D.DivisionCode,
E.EmployeeCode,
CO.CustomerTypeID,TD.PackStatusID,
TR.TransactionDate,
TR.TRANSACTIONID ,
TR.PromotedDiscount+TR.Discount  ,
TR.LPONumber ,
CO.CustomerCode ,
PR.PayerCode,
t.TerritoryCode,
RRL.DESCRIPTION ,
I.ItemCode,
PTL.Description ,
TD.Price,
CASE(TD.SalesTransactionTypeID) WHEN 2 THEN 1 ELSE  
CASE(TD.SalesTransactionTypeID) WHEN 3 THEN 1 ELSE 
CASE(TD.SalesTransactionTypeID) WHEN 4 THEN 1 ELSE 0 END END END,
TD.Discount,CU.CUSTOMERCODE
,pr.PayerID,TR.TRANSACTIONTYPEID,NetTotal,TR.VOIDED,E.EmployeeID,TD.SalesTransactionTypeID
,CO.customerid,co.outletid,d.divisionid,W.warehouseCode,W.WarehouseTypeID
";
                        InCubeQuery dtlQry = new InCubeQuery(db_vms, dtlQryStr);
                        err = dtlQry.Execute();
                        //DataRow[] detailsList = dtlQry.GetDataTable().Select();
                        DataTable DetailTable = dtlQry.GetDataTable();
                        int count = DetailTable.Rows.Count;
                        int RowConter = 0;
                        ClearProgress();
                        SetProgressMax(count);
                        if (count == 0)
                        {
                            throw new Exception("No details found");
                        }
                        impStruct2 = func.GetTable("ITEMTAB");
                        impStruct2.Clear();
                        //impStruct2.Insert(); 
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, " TRANSACTION DETAILS, NUMBER OF DETAILS : " + count.ToString(), "", LoggingType.Error, LoggingFiles.errorInv);

                        //lgRwiter.WriteLine(" ***************************************************************** ");
                        //lgRwiter.WriteLine(" TRANSACTION DETAILS, NUMBER OF DETAILS : " + count.ToString());
                        //lgRwiter.WriteLine(" ***************************************************************** ");
                        string EmployeeID = string.Empty;

                        foreach (DataRow salesTxRow in DetailTable.Rows)
                        {
                            RowConter++;
                            ReportProgress("Sending Invoices");
                            impStruct2.Append();
                            EmployeeID = salesTxRow["EmployeeID"].ToString().Trim();
                            COMPANYCODE = salesTxRow["COMPANYCODE"].ToString().Trim();
                            SalesOrganization = salesTxRow["SalesOrganization"].ToString().Trim();
                            DivisionCode = salesTxRow["DivisionCode"].ToString().Trim();
                            DistributionChannel = salesTxRow["DistributionChannel"].ToString().Trim();
                            EmployeeCode = salesTxRow["EmployeeCode"].ToString().Trim();
                            DocumentType = salesTxRow["DocumentType"].ToString().Trim();
                            TransactionDate = salesTxRow["TransactionDate"].ToString().Trim();
                            TransactionNumber = salesTxRow["TransactionNumber"].ToString().Trim();
                            OriginalTransactionNumber = TransactionNumber.ToString();
                            HeaderDiscount = salesTxRow["HeaderDiscount"].ToString().Trim();
                            LPO_Number = salesTxRow["LPO_Number"].ToString().Trim();
                            ShipToCode = salesTxRow["ShipToCode"].ToString().Trim();
                            PayerCode = salesTxRow["PayerCode"].ToString().Trim();
                            VehicleCode = salesTxRow["VehicleCode"].ToString().Trim();
                            LineNumber = salesTxRow["LineNumber"].ToString().Trim();
                            ItemReasonCode = salesTxRow["ItemReasonCode"].ToString().Trim();
                            ItemCode = salesTxRow["ItemCode"].ToString().Trim();
                            UOM = salesTxRow["UOM"].ToString().Trim();
                            Quantity = salesTxRow["Quantity"].ToString().Trim();
                            Quantity2 = salesTxRow["Quantity2"].ToString().Trim();
                            Price = salesTxRow["Price"].ToString().Trim();
                            TotalLineAmount = salesTxRow["TotalLineAmount"].ToString().Trim();
                            FOC_Indicator = salesTxRow["FOC_Indicator"].ToString().Trim();
                            ItemDiscount = salesTxRow["ItemDiscount"].ToString().Trim();
                            ActionType = salesTxRow["ActionType"].ToString().Trim();
                            SoldToCode = salesTxRow["SoldToCode"].ToString().Trim();
                            string SalesTransactionTypeID = salesTxRow["SalesTransactionTypeID"].ToString().Trim();
                            string loc = salesTxRow["WarehouseCode"].ToString().Trim();
                            //
                            //if (SalesTransactionTypeID.Equals("2")) {TransactionNumber = "F" + FOC_Count.ToString() + TransactionNumber; FOC_Count++;  DocumentType = "ZHF"; }
                            string divID = GetFieldValue("Division", "DivisionID", " DivisionCode='" + salesTxRow["DivisionCode"].ToString().Trim() + "'", db_vms).Trim();
                            impStruct2.SetValue("BUKRS", COMPANYCODE.ToString());
                            string distCh = GetFieldValue("Channel", "ChannelCode", "ChannelID in (select channelid from qnie_custdistdivdelete where customerid=" + salesTxRow["Customerid"].ToString().Trim() + " and OutletID=" + salesTxRow["OutletID"].ToString().Trim() + " and DivisionID=" + divID + " AND ISNULL(Deleted,0) = 0)", db_vms).Trim();
                            if (Quantity2.ToString().Trim().Equals(string.Empty)) Quantity2 = "0";
                            impStruct2.SetValue("VKORG", SalesOrganization.ToString());
                            impStruct2.SetValue("SPART", DivisionCode.ToString());
                            impStruct2.SetValue("VTWEG", distCh.ToString());
                            impStruct2.SetValue("PERNR", EmployeeCode.ToString());
                            impStruct2.SetValue("AUART", DocumentType.ToString());
                            impStruct2.SetValue("TRDAT", DateTime.Parse(TransactionDate.ToString()).ToString("yyyyMMddHHmmss"));
                            impStruct2.SetValue("XBLNR", TransactionNumber.ToString());
                            impStruct2.SetValue("HDRDC", decimal.Round(decimal.Parse(HeaderDiscount.ToString()), 2));
                            impStruct2.SetValue("BSTNK", LPO_Number.ToString());
                            impStruct2.SetValue("KUNWE", ShipToCode.ToString());
                            impStruct2.SetValue("KUNAG", SoldToCode.ToString());
                            impStruct2.SetValue("KUNRG", PayerCode.ToString());
                            impStruct2.SetValue("RCODE", loc.ToString());
                            impStruct2.SetValue("POSNR", LineNumber.ToString());
                            impStruct2.SetValue("AUGRU", ItemReasonCode.ToString());
                            impStruct2.SetValue("MATNR", "0000000000" + ItemCode.ToString());
                            impStruct2.SetValue("VRKME", UOM.ToString());
                            impStruct2.SetValue("FKIMG", Quantity.ToString());
                            impStruct2.SetValue("CWQTY", Quantity2.ToString());
                            impStruct2.SetValue("NETPR", decimal.Round(decimal.Parse(Price.ToString()), 2));
                            impStruct2.SetValue("NETWR", decimal.Round(decimal.Parse(TotalLineAmount.ToString()), 2));
                            impStruct2.SetValue("ISFOC", FOC_Indicator.ToString());
                            impStruct2.SetValue("ITMDC", decimal.Round(decimal.Parse(ItemDiscount.ToString()), 2));
                            impStruct2.SetValue("ACTYP", ActionType.ToString());

                            //lgRwiter.WriteLine("DETAIL NUMBER " + RowConter.ToString());
                            //lgRwiter.WriteLine(" ITEM_CODE : " + salesTxRow["ItemCode"].ToString().Trim());
                            //lgRwiter.WriteLine(" UOM : " + salesTxRow["PackName"].ToString().Trim());
                            //lgRwiter.WriteLine(" QUANTITY : " + salesTxRow["Quantity"].ToString().Trim());
                            //lgRwiter.WriteLine(" EXPIRY_DATE : " + salesTxRow["ExpiryDate"].ToString().Trim());
                            //lgRwiter.WriteLine(" BATCH_NUMBER : " + salesTxRow["BatchNo"].ToString().Trim());

                        }
                        if (ActionType.Equals("2"))
                        {
                            string existInPool = GetFieldValue("[QNIE_TransactionSendingPool]", "EmployeeID", "TransactionID='" + OriginalTransactionNumber + "'", db_vms).Trim();
                            if (!existInPool.Equals(string.Empty))
                            {
                                func.Invoke(dest);
                            }
                            else
                            {
                                //func.Invoke(dest);
                                impStruct2.Clear();
                            }
                        }
                        else
                        {
                            func.Invoke(dest);
                            InCubeQuery insert = new InCubeQuery(db_vms, "INSERT INTO QNIE_TransactionSendingPool VALUES(" + EmployeeID + ",'" + OriginalTransactionNumber + "')");
                            err = insert.Execute();
                        }
                        InCubeQuery UpdateQuery = new InCubeQuery(db_vms, "Update [Transaction] SET Synchronized = 1 where TransactionID = '" + OriginalTransactionNumber.ToString() + "'");
                        err = UpdateQuery.Execute();
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, " TRANSACTION SENT ", "", LoggingType.Error, LoggingFiles.errorInv);

                        //lgRwiter.WriteLine(" TRANSACTION SENT ");
                        //lgRwiter.WriteLine(" *******************************************************");
                        //lgRwiter.WriteLine(" *******************************************************");
                        //lgRwiter.WriteLine(" *******************************************************");
                        //lgRwiter.WriteLine("");
                        //lgRwiter.Close();
                        //lgRwiter.Dispose();
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, TransactionNumber.ToString() + " <" + DateTime.Now.ToString() + "> OK\r\n", "", LoggingType.Information, LoggingFiles.errorInv);
                        WriteMessage("\r\n" + TransactionNumber.ToString() + " - OK");
                        //StreamWriter wrt = new StreamWriter("errorInv.log", true);
                        //wrt.Write("\n" + TransactionNumber.ToString() + " <"+DateTime.Now.ToString()+"> OK\r\n");
                        //wrt.Close();
                        if (err == InCubeErrors.Success)
                        {

                        }
                    }
                    catch (Exception ex)
                    {
                        //StreamWriter wrt = new StreamWriter("errorInv.log", true);
                        //wrt.Write(ex.ToString());
                        //wrt.Close();
                        WriteMessage("\r\n" + OriginalTransactionNumber.ToString() + "");
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, " <" + DateTime.Now.ToString() + "> SALES TRANSACTION FAILED ", TransactionNumber.ToString(), LoggingType.Error, LoggingFiles.errorInv);
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.errorInv);
                        //lgRwiter.WriteLine(" <" + DateTime.Now.ToString() + "> SALES TRANSACTION FAILED ");
                        //lgRwiter.WriteLine(" *******************************************************");
                        //lgRwiter.WriteLine(TransactionNumber.ToString());
                        //lgRwiter.WriteLine(" *******************************************************");
                        //lgRwiter.WriteLine(" *******************************************************");
                        //lgRwiter.WriteLine("");
                        //lgRwiter.Close();
                        //lgRwiter.Dispose();
                    }
                    err = GetSalesTransactionInformation.FindNext();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        public override void SendOrders()
        {
            WriteMessage("\r\n" + "Sending Presales Orders");
            try
            {
                object COMPANYCODE = "";
                object SalesOrganization = "";
                object DivisionCode = "";
                object DistributionChannel = "";
                object EmployeeCode = "";
                object DocumentType = "";
                object TransactionDate = "";
                object DeliveryDate = "";
                object TransactionNumber = "";
                object HeaderDiscount = "";
                object LPO_Number = "";
                object ShipToCode = "";
                object PayerCode = "";
                object VehicleCode = "";
                object LineNumber = "";
                object ItemReasonCode = "";
                object ItemCode = "";
                object UOM = "";
                object Quantity = "";
                object Quantity2 = "";
                object Price = "";
                object TotalLineAmount = "";
                object FOC_Indicator = "";
                object ItemDiscount = "";
                object ActionType = "";
                object SoldToCode = "";


                string invoiceLog = string.Empty;

                RfcDestination dest = RfcDestinationManager.GetDestination(SendServerName);
                IRfcFunction func;
                IRfcTable impStruct2;

                func = dest.Repository.CreateFunction("ZHIB_PROCESS_PRE_SALES");
                //func.GetTable("ITEMTAB");

                string QueryString = @"
           select 
TR.OrderID
FROM 
[SalesOrder] TR 
where Synchronized=0   and 
                 (TR.OrderTypeID IN(1,2)) and TR.OrderStatusID in (1,2)
           ";
                if (Filters.EmployeeID != -1)
                {
                    QueryString += " AND TR.EmployeeID = " + Filters.EmployeeID;
                }

                InCubeQuery GetSalesTransactionInformation = new InCubeQuery(db_vms, QueryString);
                err = GetSalesTransactionInformation.Execute();
                err = GetSalesTransactionInformation.FindFirst();
                while (err == InCubeErrors.Success)
                {
                    string filename = "INV-Log" + DateTime.Today.Year.ToString() + DateTime.Today.Month.ToString() + DateTime.Today.Day.ToString() + ".log";
                    string filePath = CoreGeneral.Common.StartupPath + "\\" + filename;
                    if (!File.Exists(filePath))
                    {
                        FileStream fs = File.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite);
                        fs.Close();
                        fs.Dispose();
                    }
                    StreamWriter lgRwiter = new StreamWriter(filename, true);
                    try
                    {
                        #region Get SalesTransaction Information
                        {
                            err = GetSalesTransactionInformation.GetField("OrderID", ref TransactionNumber);

                        }
                        #endregion

                        //date = DateTime.Parse(TransactionDate.ToString());
                        //THE FOLLOWING WILL NOT INSERT THE HEADER UNLESS THE GROSS TOTAL FOR THE HEADER IS EQUAL TO THE sum(quantity*price) IN THE TRANSACTION DETAIL .
                        //string totalDetails = GetFieldValue("TransactionDetail", "isnull(sum(quantity*price),0)", "TransactionID='" + TransactionID + "'", db_vms).Trim();
                        //if (decimal.Parse(GrossTotal.ToString().Trim()) != decimal.Parse(totalDetails))
                        //{
                        //    lgRwiter.WriteLine("INVOICE HEADER , THE GROSS TOTAL IS NOT EQUAL TO THE SUM OF DETAILS AMOUNT FOR TRANSACTION  " + TransactionID + " SENDING DATE IS " + DateTime.Now.ToString());
                        //    lgRwiter.Close();
                        //    lgRwiter.Dispose();
                        //    WriteMessage("\r\n" + "THE GROSS TOTAL IS NOT EQUAL TO THE SUM OF DETAILS AMOUNT FOR TRANSACTION  " + TransactionID + " SENDING ABORTED ! .");
                        //    err = GetSalesTransactionInformation.FindNext();
                        //    continue;
                        //}
                        #region invoice header

                        #endregion

                        string dtlQryStr = @"




SELECT 
O.ORGANIZATIONCODE COMPANYCODE,
O.ORGANIZATIONCODE SalesOrganization,
D.DivisionCode,
'' DistributionChannel,
E.EmployeeCode,
case (ordertypeid) when 1 then 'ZCE' else case (ordertypeid) when 2 then 'ZRQ' else 'ZCE' end end DocumentType,
TR.OrderDate TransactionDate,
TR.DesiredDeliveryDate DeliveryDate,
TR.OrderID TransactionNumber,
TR.PromotedDiscount+TR.Discount  HeaderDiscount,
TR.LPO LPO_Number,
CO.CustomerCode ShipToCode,
PR.PayerCode,
t.TerritoryCode VehicleCode,
ROW_NUMBER()OVER(ORDER BY I.ITEMCODE,PTL.DESCRIPTION)*10 LineNumber,
RRL.DESCRIPTION ItemReasonCode,
I.ItemCode,
PTL.Description UOM,
 TD.Quantity,
TD.SecondaryQuantity Quantity2,
TD.Price,
(TD.Price*TD.Quantity-AllItemDiscount) TotalLineAmount,
CASE(TD.SalesTransactionTypeID) WHEN 2 THEN 1 ELSE  
CASE(TD.SalesTransactionTypeID) WHEN 3 THEN 1 ELSE 
CASE(TD.SalesTransactionTypeID) WHEN 4 THEN 1 ELSE 0 END END END
FOC_Indicator,
AllItemDiscount ItemDiscount,
1 ActionType,
CU.CUSTOMERCODE SoldToCode,
W.WAREHOUSECODE PLANT
FROM 
[SalesORder] TR INNER JOIN SalesORderDetail TD ON TR.OrderID=TD.OrderID AND TR.CUSTOMERID=TD.CUSTOMERID AND TR.OUTLETID=TD.OUTLETID
INNER JOIN EMPLOYEE E ON TR.EMPLOYEEID=E.EmployeeID
INNER JOIN EMPLOYEEVEHICLE EV ON E.EMPLOYEEID=EV.EMPLOYEEID
INNER JOIN WAREHOUSE W2 ON EV.VEHICLEID=W2.WAREHOUSEID
INNER JOIN VEHICLELOADINGWH VW ON EV.VEHICLEID=VW.VEHICLEID
INNER JOIN WAREHOUSE W ON VW.WAREHOUSEID=W.WAREHOUSEID
inner join employeeterritory et on e.EmployeeID=et.EmployeeID
inner join territory t on t.TerritoryID=et.TerritoryID
INNER JOIN CUSTOMEROUTLET CO ON CO.CUSTOMERID=TR.CUSTOMERID AND CO.OUTLETID=TR.OUTLETID
INNER JOIN CUSTOMER CU ON CO.CUSTOMERID=CU.CUSTOMERID
INNER JOIN PAYER PR ON CO.CustomerID=PR.CustomerID
inner join payerdivision pd on pr.PayerID=pd.PayerID and pd.divisionid=tr.DivisionID
INNER JOIN ORGANIZATION O ON TR.OrganizationID=O.OrganizationID
INNER JOIN DIVISION D ON TR.DivisionID=D.DivisionID
INNER JOIN PACK P ON TD.PackID=P.PACKID
INNER JOIN ITEM I ON P.ITEMID=I.ITEMID
INNER JOIN PACKTYPELANGUAGE PTL ON P.PackTypeID=PTL.PackTypeID AND PTL.LanguageID=1
LEFT OUTER JOIN ReturnReasonLanguage RRL ON TD.ReturnReason=RRL.ReturnReasonID AND RRL.LANGUAGEID=1
where Synchronized=0   and 
              (TR.OrderTypeID IN(1,2)) and TR.OrderStatusID in (1,2)
AND TR.OrderID='" + TransactionNumber + @"'
GROUP BY
O.ORGANIZATIONCODE ,
O.ORGANIZATIONCODE ,
D.DivisionCode,
E.EmployeeCode,
TR.OrderDate ,
TR.DesiredDeliveryDate ,
TR.OrderID ,
TR.PromotedDiscount+TR.Discount  ,
TR.LPO ,
CO.CustomerCode ,
PR.PayerCode,
t.TerritoryCode,
RRL.DESCRIPTION ,
I.ItemCode,
PTL.Description ,
 TD.Quantity,
TD.SecondaryQuantity ,
TD.Price,
(TD.Price*TD.Quantity-AllItemDiscount) ,
CASE(TD.SalesTransactionTypeID) WHEN 2 THEN 1 ELSE  
CASE(TD.SalesTransactionTypeID) WHEN 3 THEN 1 ELSE 
CASE(TD.SalesTransactionTypeID) WHEN 4 THEN 1 ELSE 0 END END END,
AllItemDiscount,CU.CUSTOMERCODE
,pr.PayerID,ordertypeid,W.WAREHOUSECODE
order by TR.orderID

";

                        InCubeQuery dtlQry = new InCubeQuery(db_vms, dtlQryStr);
                        err = dtlQry.Execute();
                        //DataRow[] detailsList = dtlQry.GetDataTable().Select();
                        DataTable DetailTable = dtlQry.GetDataTable();
                        int count = DetailTable.Rows.Count;
                        int RowConter = 0;
                        ClearProgress();
                        SetProgressMax(count);

                        if (count == 0)
                        {
                            throw new Exception("No details found");
                        }

                        impStruct2 = func.GetTable("ITEMTAB");
                        impStruct2.Insert();
                        lgRwiter.WriteLine(" ***************************************************************** ");
                        lgRwiter.WriteLine(" TRANSACTION DETAILS, NUMBER OF DETAILS : " + count.ToString());
                        lgRwiter.WriteLine(" ***************************************************************** ");
                        foreach (DataRow salesTxRow in DetailTable.Rows)
                        {
                            RowConter++;
                            ReportProgress("Sending Invoices");

                            COMPANYCODE = salesTxRow["COMPANYCODE"].ToString().Trim();
                            SalesOrganization = salesTxRow["SalesOrganization"].ToString().Trim();
                            DivisionCode = salesTxRow["DivisionCode"].ToString().Trim();
                            DistributionChannel = salesTxRow["DistributionChannel"].ToString().Trim();
                            EmployeeCode = salesTxRow["EmployeeCode"].ToString().Trim();
                            DocumentType = salesTxRow["DocumentType"].ToString().Trim();
                            TransactionDate = salesTxRow["TransactionDate"].ToString().Trim();
                            TransactionNumber = salesTxRow["TransactionNumber"].ToString().Trim();
                            HeaderDiscount = salesTxRow["HeaderDiscount"].ToString().Trim();
                            LPO_Number = salesTxRow["LPO_Number"].ToString().Trim();
                            ShipToCode = salesTxRow["ShipToCode"].ToString().Trim();
                            PayerCode = salesTxRow["PayerCode"].ToString().Trim();
                            VehicleCode = salesTxRow["VehicleCode"].ToString().Trim();
                            LineNumber = salesTxRow["LineNumber"].ToString().Trim();
                            ItemReasonCode = salesTxRow["ItemReasonCode"].ToString().Trim();
                            ItemCode = salesTxRow["ItemCode"].ToString().Trim();
                            UOM = salesTxRow["UOM"].ToString().Trim();
                            Quantity = salesTxRow["Quantity"].ToString().Trim();
                            Quantity2 = salesTxRow["Quantity2"].ToString().Trim();
                            Price = salesTxRow["Price"].ToString().Trim();
                            TotalLineAmount = salesTxRow["TotalLineAmount"].ToString().Trim();
                            FOC_Indicator = salesTxRow["FOC_Indicator"].ToString().Trim();
                            ItemDiscount = salesTxRow["ItemDiscount"].ToString().Trim();
                            //ActionType = salesTxRow["ActionType"].ToString().Trim();
                            SoldToCode = salesTxRow["SoldToCode"].ToString().Trim();
                            DeliveryDate = salesTxRow["DeliveryDate"].ToString().Trim();
                            string Plant = salesTxRow["plant"].ToString().Trim();

                            impStruct2.SetValue("BUKRS", COMPANYCODE.ToString());
                            string distCh = GetFieldValue("Channel", "ChannelCode", "ChannelID in (select channelid from customergroup where groupid in (select groupid from customeroutletgroup where customerid in (select customerid from customeroutlet where customercode='" + ShipToCode + "')))", db_vms).Trim();
                            if (Quantity2.ToString().Trim().Equals(string.Empty)) Quantity2 = "0";
                            impStruct2.SetValue("VKORG", SalesOrganization.ToString());
                            impStruct2.SetValue("SPART", DivisionCode.ToString());
                            impStruct2.SetValue("VTWEG", distCh.ToString());
                            impStruct2.SetValue("PERNR", EmployeeCode.ToString());
                            impStruct2.SetValue("AUART", DocumentType.ToString());
                            impStruct2.SetValue("TRDAT", DateTime.Parse(TransactionDate.ToString()).ToString("yyyyMMddHHmmss"));
                            impStruct2.SetValue("LFDAT", DateTime.Parse(DeliveryDate.ToString()).ToString("yyyyMMdd"));
                            impStruct2.SetValue("XBLNR", TransactionNumber.ToString());
                            impStruct2.SetValue("HDRDC", HeaderDiscount.ToString());
                            impStruct2.SetValue("BSTNK", LPO_Number.ToString());
                            impStruct2.SetValue("KUNWE", ShipToCode.ToString());
                            impStruct2.SetValue("KUNAG", SoldToCode.ToString());
                            impStruct2.SetValue("KUNRG", PayerCode.ToString());
                            impStruct2.SetValue("WERKS", Plant);
                            impStruct2.SetValue("POSNR", LineNumber.ToString());
                            impStruct2.SetValue("AUGRU", ItemReasonCode.ToString());
                            impStruct2.SetValue("MATNR", "0000000000" + ItemCode.ToString());
                            impStruct2.SetValue("VRKME", UOM.ToString());
                            impStruct2.SetValue("FKIMG", Quantity.ToString());
                            //impStruct2.SetValue("CWQTY", Quantity2.ToString());
                            //impStruct2.SetValue("NETPR", Price.ToString());
                            //impStruct2.SetValue("NETWR", TotalLineAmount.ToString());
                            //impStruct2.SetValue("ISFOC", FOC_Indicator.ToString());
                            //impStruct2.SetValue("ITMDC", ItemDiscount.ToString());
                            //impStruct2.SetValue("ACTYP", ActionType.ToString());



                            //lgRwiter.WriteLine("DETAIL NUMBER " + RowConter.ToString());
                            //lgRwiter.WriteLine(" ITEM_CODE : " + salesTxRow["ItemCode"].ToString().Trim());
                            //lgRwiter.WriteLine(" UOM : " + salesTxRow["PackName"].ToString().Trim());
                            //lgRwiter.WriteLine(" QUANTITY : " + salesTxRow["Quantity"].ToString().Trim());
                            //lgRwiter.WriteLine(" EXPIRY_DATE : " + salesTxRow["ExpiryDate"].ToString().Trim());
                            //lgRwiter.WriteLine(" BATCH_NUMBER : " + salesTxRow["BatchNo"].ToString().Trim());

                            func.Invoke(dest);

                        }

                        InCubeQuery UpdateQuery = new InCubeQuery(db_vms, "Update [SALESORDER] SET Synchronized = 1 where ORDERID = '" + TransactionNumber.ToString() + "'");
                        err = UpdateQuery.Execute();
                        lgRwiter.WriteLine(" ORDER SENT ");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine("");
                        lgRwiter.Close();
                        lgRwiter.Dispose();
                        WriteMessage("\r\n" + TransactionNumber.ToString() + " - OK");
                        StreamWriter wrt = new StreamWriter("errorInv.log", true);
                        wrt.Write("\n" + TransactionNumber.ToString() + " OK\r\n");
                        wrt.Close();
                        if (err == InCubeErrors.Success)
                        {

                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                        StreamWriter wrt = new StreamWriter("errorInv.log", true);
                        wrt.Write(ex.ToString());
                        wrt.Close();
                        WriteMessage("\r\n" + TransactionNumber.ToString() + " - FAILED!");
                        lgRwiter.WriteLine(" ORDER FAILED ");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(TransactionNumber.ToString());
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine("");
                        lgRwiter.Close();
                        lgRwiter.Dispose();
                    }
                    err = GetSalesTransactionInformation.FindNext();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        public override void SendReciepts()
        {
            try
            {
                //SendDownPaymentReciepts(true, "", DateTime.Now, DateTime.Now);
                //return;
                WriteMessage("\r\n" + "Sending Receipts");
                object CompanyCode = "";
                object DivisionCode = "";
                object SoldToCode = "";
                object ShipToCode = "";
                object PayerCode = "";
                object PaymentNumber = "";
                object InvoiceNumber = "";
                object PaymentDate = "";
                object PaidAmount = "";
                object InvoiceTotalAmt = "";
                object IsDownPayment = "";
                object PaymentType = "";
                object CustomerType = "";
                object CheqNumber = "";
                object BankCode = "";
                object CheqDate = "";
                object Notes = "";
                object RouteCode = "";
                object SalesmanCode = "";


                string invoiceLog = string.Empty;

                RfcDestination dest = RfcDestinationManager.GetDestination(SendServerName);
                IRfcFunction func;
                IRfcTable impStruct2;

                func = dest.Repository.CreateFunction("ZHIB_PAYMENT_COLLECTION");
                //func.GetTable("ITEMTAB");


                string filename = "INV-Log" + DateTime.Today.Year.ToString() + DateTime.Today.Month.ToString() + DateTime.Today.Day.ToString() + ".log";
                string filePath = CoreGeneral.Common.StartupPath + "\\" + filename;
                if (!File.Exists(filePath))
                {
                    FileStream fs = File.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite);
                    fs.Close();
                    fs.Dispose();
                }
                StreamWriter lgRwiter = new StreamWriter(filename, true);

                try
                {

                    string dtlQryStr = @"
SELECT 
O.ORGANIZATIONCODE CompanyCode
,isnull(D.DivisionCode,'70') DivisionCode
,CU.CUSTOMERCODE SoldToCode
,CO.CUSTOMERCODE ShipToCode
,PR.PayerCode
,CP.CustomerPaymentID PaymentNumber
,TR.TRANSACTIONID InvoiceNumber
,CP.PaymentDate
,CP.AppliedAmount PaidAmount
,TR.NetTotal InvoiceTotalAmt
,0 IsDownPayment
,CASE(CP.PaymentTypeID) WHEN 1 THEN 0 ELSE  
CASE(CP.PaymentTypeID) WHEN 2 THEN 1 ELSE  
CASE(CP.PaymentTypeID) WHEN 3 THEN 2 ELSE  
CASE(CP.PaymentTypeID) WHEN 4 THEN 4 ELSE  
CASE(CP.PaymentTypeID) WHEN 5 THEN 3 ELSE 0 END END END END END 
PaymentType
,CO.CustomerTypeID CustomerType
,CP.VoucherNumber CheqNumber
,B.Code BankCode
,CP.VoucherDate CheqDate
,CP.Notes Notes
,T.TerritoryCode RouteCode
,E.EmployeeCode SalesmanCode
,case when t2.SourceTransactionID is null then CP.SourceTransactionID else t2.SourceTransactionID end as CN_RTN,
E.EmployeeID
FROM 
CUSTOMERPAYMENT CP INNER JOIN [TRANSACTION] TR ON CP.TRANSACTIONID=TR.TRANSACTIONID AND CP.CUSTOMERID=TR.CUSTOMERID AND CP.OUTLETID=TR.OUTLETID
INNER JOIN WAREHOUSE W2 ON TR.WAREHOUSEID=W2.WAREHOUSEID
INNER JOIN EMPLOYEE E ON CP.EMPLOYEEID=E.EmployeeID
inner join employeeterritory et on e.EmployeeID=et.EmployeeID
inner join territory t on t.TerritoryID=et.TerritoryID
INNER JOIN CUSTOMEROUTLET CO ON CO.CUSTOMERID=TR.CUSTOMERID AND CO.OUTLETID=TR.OUTLETID
INNER JOIN CUSTOMER CU ON CO.CUSTOMERID=CU.CUSTOMERID
INNER JOIN PAYER PR ON CO.CustomerID=PR.CustomerID AND PR.OUTLETID=CO.OUTLETID
inner join payerdivision pd on pr.PayerID=pd.PayerID and pd.divisionid=1
INNER JOIN ORGANIZATION O ON TR.OrganizationID=O.OrganizationID
left outer JOIN DIVISION D ON TR.DivisionID=D.DivisionID
LEFT OUTER JOIN BANK B ON CP.BankID=B.BankID
left outer join [transaction] T2 on cp.SourceTransactionID=T2.transactionid
WHERE (CP.Synchronized = 0) and CP.transactionid not in ('I1144000051','I1144000052','I1144000033')
                    and CP.PaymentStatusID <>5 and CP.EmployeeID<>0
";

                    InCubeQuery dtlQry = new InCubeQuery(db_vms, dtlQryStr);
                    err = dtlQry.Execute();

                    DataTable DetailTable = dtlQry.GetDataTable();
                    int count = DetailTable.Rows.Count;
                    int RowConter = 0;
                    ClearProgress();
                    SetProgressMax(count);

                    if (count == 0)
                    {
                        throw new Exception("No payments found");
                    }

                    impStruct2 = func.GetTable("ITEMTAB");
                    impStruct2.Insert();
                    lgRwiter.WriteLine(" ***************************************************************** ");
                    lgRwiter.WriteLine(" COLLECTION DETAILS, NUMBER OF DETAILS : " + count.ToString());
                    lgRwiter.WriteLine(" ***************************************************************** ");
                    foreach (DataRow salesTxRow in DetailTable.Rows)
                    {
                        RowConter++;
                        ReportProgress("Sending Invoices");
                        string UploadStatus = GetFieldValue("RouteHistory", "Top(1) Uploaded", " EmployeeID=" + salesTxRow["EmployeeID"].ToString().Trim() + " order by RouteHistoryID Desc", db_vms).Trim();
                        if (UploadStatus.Equals(string.Empty)) UploadStatus = "false";
                        if (UploadStatus.ToLower().Equals("true")) { continue; }

                        CompanyCode = salesTxRow["CompanyCode"].ToString().Trim();
                        DivisionCode = salesTxRow["DivisionCode"].ToString().Trim();
                        SoldToCode = salesTxRow["SoldToCode"].ToString().Trim();
                        ShipToCode = salesTxRow["ShipToCode"].ToString().Trim();
                        PayerCode = salesTxRow["PayerCode"].ToString().Trim();
                        PaymentNumber = salesTxRow["PaymentNumber"].ToString().Trim();
                        InvoiceNumber = salesTxRow["InvoiceNumber"].ToString().Trim();
                        PaymentDate = salesTxRow["PaymentDate"].ToString().Trim();
                        PaidAmount = salesTxRow["PaidAmount"].ToString().Trim();
                        InvoiceTotalAmt = salesTxRow["InvoiceTotalAmt"].ToString().Trim();
                        IsDownPayment = salesTxRow["IsDownPayment"].ToString().Trim();
                        PaymentType = salesTxRow["PaymentType"].ToString().Trim();
                        CustomerType = salesTxRow["CustomerType"].ToString().Trim();
                        CheqNumber = salesTxRow["CheqNumber"].ToString().Trim();
                        BankCode = salesTxRow["BankCode"].ToString().Trim();
                        CheqDate = salesTxRow["CheqDate"].ToString().Trim();
                        Notes = salesTxRow["Notes"].ToString().Trim();
                        RouteCode = salesTxRow["RouteCode"].ToString().Trim();
                        SalesmanCode = salesTxRow["SalesmanCode"].ToString().Trim();
                        string RetRef = salesTxRow["CN_RTN"].ToString().Trim();
                        //string xx = "1144000001";

                        impStruct2.SetValue("BUKRS", CompanyCode.ToString());
                        impStruct2.SetValue("SPART", DivisionCode.ToString());
                        impStruct2.SetValue("KUNAG", SoldToCode.ToString());
                        impStruct2.SetValue("KUNWE", ShipToCode.ToString());
                        impStruct2.SetValue("KUNRG", PayerCode.ToString());
                        impStruct2.SetValue("KIDNO", PaymentNumber.ToString());
                        impStruct2.SetValue("HHINV", InvoiceNumber.ToString());
                        impStruct2.SetValue("PAYDT", DateTime.Parse(PaymentDate.ToString()).ToString("yyyyMMddHHmmss"));
                        impStruct2.SetValue("NEBTR", decimal.Round(decimal.Round(decimal.Parse(PaidAmount.ToString()))));
                        impStruct2.SetValue("NETWR", decimal.Round(decimal.Round(decimal.Parse(InvoiceTotalAmt.ToString()))));
                        impStruct2.SetValue("XANET", IsDownPayment.ToString());
                        impStruct2.SetValue("PMTYP", PaymentType.ToString());
                        impStruct2.SetValue("CSTYP", CustomerType.ToString());
                        impStruct2.SetValue("CHKNM", CheqNumber.ToString());
                        impStruct2.SetValue("BANKA", BankCode.ToString());
                        if (CheqDate.ToString().Equals(string.Empty)) CheqDate = DateTime.Now.ToString();
                        impStruct2.SetValue("CHKDT", DateTime.Parse(CheqDate.ToString()).ToString("yyyyMMdd"));
                        impStruct2.SetValue("SGTXT", Notes.ToString());
                        impStruct2.SetValue("RCODE", RouteCode.ToString());
                        impStruct2.SetValue("PERNR", SalesmanCode.ToString());
                        impStruct2.SetValue("HHRET", RetRef);

                        //lgRwiter.WriteLine("DETAIL NUMBER " + RowConter.ToString());
                        //lgRwiter.WriteLine(" ITEM_CODE : " + salesTxRow["ItemCode"].ToString().Trim());
                        //lgRwiter.WriteLine(" UOM : " + salesTxRow["PackName"].ToString().Trim());
                        //lgRwiter.WriteLine(" QUANTITY : " + salesTxRow["Quantity"].ToString().Trim());
                        //lgRwiter.WriteLine(" EXPIRY_DATE : " + salesTxRow["ExpiryDate"].ToString().Trim());
                        //lgRwiter.WriteLine(" BATCH_NUMBER : " + salesTxRow["BatchNo"].ToString().Trim());

                        func.Invoke(dest);
                        InCubeQuery UpdateQuery = new InCubeQuery(db_vms, "Update [CUSTOMERPAYMENT] SET Synchronized = 1 where CUSTOMERPAYMENTID = '" + PaymentNumber.ToString() + "'");
                        err = UpdateQuery.Execute();
                        lgRwiter.WriteLine(" COLLECTION SENT ");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine("");
                        WriteMessage("\r\n" + PaymentNumber.ToString() + "  <" + DateTime.Now.ToString() + "> OK");
                    }
                    lgRwiter.Close();
                    lgRwiter.Dispose();

                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                    StreamWriter wrt = new StreamWriter("errorInv.log", true);
                    wrt.Write(ex.Message.ToString());
                    wrt.Close();
                    WriteMessage("\r\n" + PaymentNumber.ToString() + "!");
                    lgRwiter.WriteLine(" <" + DateTime.Now.ToString() + "> PAYMENT FAILED ");
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine(PaymentNumber.ToString());
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine("");
                    lgRwiter.Close();
                    lgRwiter.Dispose();
                }


            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void SendDownPaymentReciepts(bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate)
        {
            try
            {
                WriteMessage("\r\n" + "Sending Receipts");
                object CompanyCode = "";
                object DivisionCode = "";
                object SoldToCode = "";
                object ShipToCode = "";
                object PayerCode = "";
                object PaymentNumber = "";
                object InvoiceNumber = "";
                object PaymentDate = "";
                object PaidAmount = "";
                object InvoiceTotalAmt = "";
                object IsDownPayment = "";
                object PaymentType = "";
                object CustomerType = "";
                object CheqNumber = "";
                object BankCode = "";
                object CheqDate = "";
                object Notes = "";
                object RouteCode = "";
                object SalesmanCode = "";


                string invoiceLog = string.Empty;

                RfcDestination dest = RfcDestinationManager.GetDestination(SendServerName);
                IRfcFunction func;
                IRfcTable impStruct2;

                func = dest.Repository.CreateFunction("ZHIB_PAYMENT_COLLECTION");
                //func.GetTable("ITEMTAB");


                string filename = "INV-Log" + DateTime.Today.Year.ToString() + DateTime.Today.Month.ToString() + DateTime.Today.Day.ToString() + ".log";
                string filePath = CoreGeneral.Common.StartupPath + "\\" + filename;
                if (!File.Exists(filePath))
                {
                    FileStream fs = File.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite);
                    fs.Close();
                    fs.Dispose();
                }
                StreamWriter lgRwiter = new StreamWriter(filename, true);

                try
                {

                    string dtlQryStr = @"
SELECT 
O.ORGANIZATIONCODE CompanyCode
,D.DivisionCode
,CU.CUSTOMERCODE SoldToCode
,CO.CUSTOMERCODE ShipToCode
,PR.PayerCode
,CP.CustomerPaymentID PaymentNumber
,'' InvoiceNumber
,CP.PaymentDate
,CP.PaidAmount PaidAmount
,'0' InvoiceTotalAmt
,1 IsDownPayment
,CASE(CP.PaymentTypeID) WHEN 1 THEN 0 ELSE  
CASE(CP.PaymentTypeID) WHEN 2 THEN 1 ELSE  
CASE(CP.PaymentTypeID) WHEN 3 THEN 2 ELSE  
CASE(CP.PaymentTypeID) WHEN 4 THEN 4 ELSE  
CASE(CP.PaymentTypeID) WHEN 5 THEN 3 ELSE 0 END END END END END 
PaymentType
,CODT.CustomerTypeID CustomerType
,CP.VoucherNumber CheqNumber
,B.Code BankCode
,CP.VoucherDate CheqDate
,CP.Notes Notes
,T.TerritoryCode RouteCode
,E.EmployeeCode SalesmanCode

FROM 
CustomerUnallocatedPayment CP 
INNER JOIN EMPLOYEE E ON CP.EMPLOYEEID=E.EmployeeID
inner join employeeterritory et on e.EmployeeID=et.EmployeeID
inner join territory t on t.TerritoryID=et.TerritoryID
INNER JOIN CUSTOMEROUTLET CO ON CO.CUSTOMERID=CP.CUSTOMERID AND CO.OUTLETID=CP.OUTLETID
INNER JOIN CUSTOMER CU ON CO.CUSTOMERID=CU.CUSTOMERID
INNER JOIN PAYER PR ON CO.CustomerID=PR.CustomerID
inner join payerdivision pd on pr.PayerID=pd.PayerID and pd.divisionid=CP.DevisionID
INNER JOIN ORGANIZATION O ON CP.OrganizationID=O.OrganizationID
INNER JOIN DIVISION D ON CP.DevisionID=D.DivisionID
INNER JOIN CustOutDivCustomerType CODT ON CODT.CUSTOMERID=CO.CustomerID AND CODT.OUTLETID=CO.OUTLETID AND CODT.DivisionID=D.DivisionID
LEFT OUTER JOIN BANK B ON CP.BankID=B.BankID
WHERE (CP.Synchronised = 0) and CP.EmployeeID<>0
";

                    InCubeQuery dtlQry = new InCubeQuery(db_vms, dtlQryStr);
                    err = dtlQry.Execute();

                    DataTable DetailTable = dtlQry.GetDataTable();
                    int count = DetailTable.Rows.Count;
                    int RowConter = 0;
                    ClearProgress();
                    SetProgressMax(count);

                    if (count == 0)
                    {
                        throw new Exception("No payments found");
                    }

                    impStruct2 = func.GetTable("ITEMTAB");
                    impStruct2.Insert();
                    lgRwiter.WriteLine(" ***************************************************************** ");
                    lgRwiter.WriteLine(" COLLECTION DETAILS, NUMBER OF DETAILS : " + count.ToString());
                    lgRwiter.WriteLine(" ***************************************************************** ");
                    foreach (DataRow salesTxRow in DetailTable.Rows)
                    {
                        RowConter++;
                        ReportProgress("Sending Invoices");

                        CompanyCode = salesTxRow["CompanyCode"].ToString().Trim();
                        DivisionCode = salesTxRow["DivisionCode"].ToString().Trim();
                        SoldToCode = salesTxRow["SoldToCode"].ToString().Trim();
                        ShipToCode = salesTxRow["ShipToCode"].ToString().Trim();
                        PayerCode = salesTxRow["PayerCode"].ToString().Trim();
                        PaymentNumber = salesTxRow["PaymentNumber"].ToString().Trim();
                        InvoiceNumber = salesTxRow["InvoiceNumber"].ToString().Trim();
                        PaymentDate = salesTxRow["PaymentDate"].ToString().Trim();
                        PaidAmount = salesTxRow["PaidAmount"].ToString().Trim();
                        InvoiceTotalAmt = salesTxRow["InvoiceTotalAmt"].ToString().Trim();
                        IsDownPayment = salesTxRow["IsDownPayment"].ToString().Trim();
                        PaymentType = salesTxRow["PaymentType"].ToString().Trim();
                        CustomerType = salesTxRow["CustomerType"].ToString().Trim();
                        CheqNumber = salesTxRow["CheqNumber"].ToString().Trim();
                        BankCode = salesTxRow["BankCode"].ToString().Trim();
                        CheqDate = salesTxRow["CheqDate"].ToString().Trim();
                        Notes = salesTxRow["Notes"].ToString().Trim();
                        RouteCode = salesTxRow["RouteCode"].ToString().Trim();
                        SalesmanCode = salesTxRow["SalesmanCode"].ToString().Trim();
                        //string RetRef = salesTxRow["CN_RTN"].ToString().Trim();
                        //string xx = "1144000001";

                        impStruct2.SetValue("BUKRS", CompanyCode.ToString());
                        impStruct2.SetValue("SPART", DivisionCode.ToString());
                        impStruct2.SetValue("KUNAG", SoldToCode.ToString());
                        impStruct2.SetValue("KUNWE", ShipToCode.ToString());
                        impStruct2.SetValue("KUNRG", PayerCode.ToString());
                        impStruct2.SetValue("KIDNO", PaymentNumber.ToString());
                        impStruct2.SetValue("HHINV", InvoiceNumber.ToString());
                        impStruct2.SetValue("PAYDT", DateTime.Parse(PaymentDate.ToString()).ToString("yyyyMMddHHmmss"));
                        impStruct2.SetValue("NEBTR", decimal.Round(decimal.Round(decimal.Parse(PaidAmount.ToString()))));
                        impStruct2.SetValue("NETWR", 0);
                        impStruct2.SetValue("XANET", IsDownPayment.ToString());
                        impStruct2.SetValue("PMTYP", PaymentType.ToString());
                        impStruct2.SetValue("CSTYP", CustomerType.ToString());
                        impStruct2.SetValue("CHKNM", CheqNumber.ToString());
                        impStruct2.SetValue("BANKA", BankCode.ToString());
                        if (CheqDate.ToString().Equals(string.Empty)) CheqDate = DateTime.Now.ToString();
                        impStruct2.SetValue("CHKDT", DateTime.Parse(CheqDate.ToString()).ToString("yyyyMMdd"));
                        impStruct2.SetValue("SGTXT", Notes.ToString());
                        impStruct2.SetValue("RCODE", RouteCode.ToString());
                        impStruct2.SetValue("PERNR", SalesmanCode.ToString());
                        impStruct2.SetValue("HHRET", "");

                        //lgRwiter.WriteLine("DETAIL NUMBER " + RowConter.ToString());
                        //lgRwiter.WriteLine(" ITEM_CODE : " + salesTxRow["ItemCode"].ToString().Trim());
                        //lgRwiter.WriteLine(" UOM : " + salesTxRow["PackName"].ToString().Trim());
                        //lgRwiter.WriteLine(" QUANTITY : " + salesTxRow["Quantity"].ToString().Trim());
                        //lgRwiter.WriteLine(" EXPIRY_DATE : " + salesTxRow["ExpiryDate"].ToString().Trim());
                        //lgRwiter.WriteLine(" BATCH_NUMBER : " + salesTxRow["BatchNo"].ToString().Trim());

                        func.Invoke(dest);
                        InCubeQuery UpdateQuery = new InCubeQuery(db_vms, "Update [CustomerUnallocatedPayment] SET Synchronised = 1 where CUSTOMERPAYMENTID = '" + PaymentNumber.ToString() + "'");
                        err = UpdateQuery.Execute();
                        lgRwiter.WriteLine(" COLLECTION SENT ");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine("");
                        WriteMessage("\r\n" + PaymentNumber.ToString() + "  <" + DateTime.Now.ToString() + "> OK");
                    }
                    lgRwiter.Close();
                    lgRwiter.Dispose();

                }
                catch (Exception ex)
                {
                    StreamWriter wrt = new StreamWriter("errorInv.log", true);
                    wrt.Write(ex.Message.ToString());
                    wrt.Close();
                    WriteMessage("\r\n" + PaymentNumber.ToString() + "!");
                    lgRwiter.WriteLine(" <" + DateTime.Now.ToString() + "> PAYMENT FAILED ");
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine(PaymentNumber.ToString());
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine("");
                    lgRwiter.Close();
                    lgRwiter.Dispose();
                }


            }
            catch
            {
            }
        }

        public override void SendReturn()
        {
            WriteMessage("\r\n" + "Send Return");
            InCubeRow SalesTransactionRow1 = new InCubeRow();
            InCubeRow PackRow = new InCubeRow();
            InCubeTable SalesTransactionDetail = new InCubeTable();
            InCubeTable PackTable = new InCubeTable();
            object TransactionID = "";
            object TransactionDate = "";
            object CustomerName = "";
            object CustomerCode = "";
            object WarehouseCode = "";
            object EmployeeCode = "";
            object CustomerID = "";
            object OutletID = "";
            object CustomerAddress = "";
            object CustomerAddress1 = "";
            object CustomerAddress2 = "";
            object OutletCode = "";
            DateTime date;
            object GrossTotal = "";
            object Discount = "";
            object NetTotal = "";
            object RemainingAmount = "";
            object DivisionID = "";
            object Transtype = "";
            object SourceTranID = " ";
            object invoice = "";
            string ReturnLog = string.Empty;
            string wherestring = string.Empty;
            RfcDestination dest = RfcDestinationManager.GetDestination(SendServerName);
            IRfcFunction func;
            IRfcStructure impStruct;
            IRfcTable impStruct2;

            func = dest.Repository.CreateFunction("ZSD_FM_PDT_SALES");
            impStruct = func.GetStructure("EX_HEADER");

            if (Filters.EmployeeID != -1)
            {
                wherestring = " AND [Transaction].EmployeeID = " + Filters.EmployeeID;
            }
            string QueryString = string.Format(@"SELECT
            [Transaction].TransactionID,
            [Transaction].TransactionDate,
            CustomerOutlet.CustomerCode,
            Warehouse.WarehouseCode,
            Employee.EmployeeCode,
            CustomerOutletLanguage.Description,
            CustomerOutlet.CustomerID,
            CustomerOutlet.OutletID,
            CustomerOutletLanguage.Address,
            CustomerOutlet.CustomerCode as OutletCode,
            [Transaction].GrossTotal,
            [Transaction].Discount,
            [Transaction].NetTotal,
            [Transaction].RemainingAmount,
            [Transaction].DivisionID,
            [Transaction].TransactionTypeID,
            [Transaction].SourceTransactionID,
CP.TransactionID as INVOICE
             FROM         [Transaction] INNER JOIN
CustomerOutletLanguage ON [Transaction].CustomerID = CustomerOutletLanguage.CustomerID INNER JOIN
CustomerOutlet ON [Transaction].CustomerID = CustomerOutlet.CustomerID AND [Transaction].OutletID = CustomerOutlet.OutletID AND
CustomerOutletLanguage.CustomerID = CustomerOutlet.CustomerID AND CustomerOutletLanguage.OutletID = CustomerOutlet.OutletID INNER JOIN
warehouse on [Transaction].WarehouseID=Warehouse.WarehouseID inner join
Employee ON [Transaction].EmployeeID = Employee.EmployeeID
            left outer join CustomerPayment CP on  CP.SourceTransactionID=[Transaction].TransactionID
            WHERE ([Transaction].Synchronized = 0) AND ([Transaction].TransactionTypeID = 2 or [Transaction].TransactionTypeID = 5) AND
            ([Transaction].TransactionDate >= '{0}'
            AND [Transaction].TransactionDate < '{1}')  {2}
            group by [Transaction].TransactionID,
            [Transaction].TransactionDate,
            CustomerOutlet.CustomerCode,
            Warehouse.WarehouseCode,
            Employee.EmployeeCode,
            CustomerOutletLanguage.Description,
            CustomerOutlet.CustomerID,
            CustomerOutlet.OutletID,
            CustomerOutletLanguage.Address,
            CustomerOutlet.CustomerCode,
            [Transaction].GrossTotal,
            [Transaction].Discount,
            [Transaction].NetTotal,
            [Transaction].RemainingAmount,
            [Transaction].DivisionID,
            [Transaction].TransactionTypeID,
            [Transaction].SourceTransactionID,
CP.TransactionID
             order by CP.TransactionID desc", Filters.FromDate.Date.ToString("yyyy/MM/dd"), Filters.ToDate.Date.AddDays(1).ToString("yyyy/MM/dd"), wherestring);
            /*SELECT
        [Transaction].TransactionID,
        [Transaction].TransactionDate,
        CustomerOutlet.CustomerCode,
        Warehouse.WarehouseCode,
        Employee.EmployeeCode,
        CustomerOutletLanguage.Description,
        CustomerOutlet.CustomerID,
        CustomerOutlet.OutletID,
        CustomerOutletLanguage.Address,
        CustomerOutlet.CustomerCode as OutletCode,
        [Transaction].GrossTotal,
        [Transaction].Discount,
        [Transaction].NetTotal,
        [Transaction].RemainingAmount,
        [Transaction].DivisionID,
        [Transaction].TransactionTypeID,
        [Transaction].SourceTransactionID

        FROM [Transaction] INNER JOIN
        CustomerOutletLanguage ON [Transaction].CustomerID = CustomerOutletLanguage.CustomerID INNER JOIN
        CustomerOutlet ON [Transaction].CustomerID = CustomerOutlet.CustomerID AND [Transaction].OutletID = CustomerOutlet.OutletID AND
        CustomerOutletLanguage.CustomerID = CustomerOutlet.CustomerID AND CustomerOutletLanguage.OutletID = CustomerOutlet.OutletID INNER JOIN
        EmployeeVehicle ON [Transaction].EmployeeID = EmployeeVehicle.EmployeeID INNER JOIN
        Warehouse ON EmployeeVehicle.VehicleID = Warehouse.WarehouseID INNER JOIN
        Employee ON [Transaction].EmployeeID = Employee.EmployeeID
        WHERE ([Transaction].Synchronized = 0) AND ([Transaction].TransactionTypeID = 2 or [Transaction].TransactionTypeID = 5) AND
        ([Transaction].TransactionDate >= '" + FromDate.Date.ToString("yyyy/MM/dd") + @"'
        AND [Transaction].TransactionDate < '" + ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + "');*/

            InCubeQuery GetSalesTransactionInformation = new InCubeQuery(db_vms, QueryString);

            err = GetSalesTransactionInformation.Execute();
            err = GetSalesTransactionInformation.FindFirst();
            while (err == InCubeErrors.Success)
            {
                string filename = "RET-Log" + DateTime.Today.Year.ToString() + DateTime.Today.Month.ToString() + DateTime.Today.Day.ToString() + ".log";
                string filePath = CoreGeneral.Common.StartupPath + "\\" + filename;
                if (!File.Exists(filePath))
                {
                    FileStream fs = File.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite);
                    fs.Close();
                    fs.Dispose();
                }
                StreamWriter lgRwiter = new StreamWriter(filename, true);
                object field = new object();

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
                    err = GetSalesTransactionInformation.GetField(10, ref GrossTotal);
                    err = GetSalesTransactionInformation.GetField(11, ref Discount);
                    err = GetSalesTransactionInformation.GetField(12, ref NetTotal);
                    err = GetSalesTransactionInformation.GetField(13, ref RemainingAmount);
                    err = GetSalesTransactionInformation.GetField(14, ref DivisionID);
                    err = GetSalesTransactionInformation.GetField(15, ref Transtype);
                    err = GetSalesTransactionInformation.GetField(16, ref SourceTranID);
                    if (SourceTranID.ToString().Trim().Equals(string.Empty)) SourceTranID = " ";
                    err = GetSalesTransactionInformation.GetField(17, ref invoice);
                    if (invoice.ToString().Trim().Equals(string.Empty)) invoice = "";
                }
                if (!Transtype.ToString().Trim().Equals("5"))
                {
                    string totalDetails = GetFieldValue("TransactionDetail", "isnull(sum(quantity*price),0)", "TransactionID='" + TransactionID + "'", db_vms).Trim();
                    if (decimal.Parse(GrossTotal.ToString().Trim()) != decimal.Parse(totalDetails))
                    {
                        lgRwiter.WriteLine("INVOICE HEADER , THE GROSS TOTAL IS NOT EQUAL TO THE SUM OF DETAILS AMOUNT FOR TRANSACTION  " + TransactionID + " SENDING DATE IS " + DateTime.Now.ToString());
                        lgRwiter.Close();
                        lgRwiter.Dispose();
                        WriteMessage("\r\n" + "THE GROSS TOTAL IS NOT EQUAL TO THE SUM OF DETAILS AMOUNT FOR TRANSACTION  " + TransactionID + " SENDING ABORTED ! .");
                        err = GetSalesTransactionInformation.FindNext();
                        continue;
                    }
                }
                #endregion

                try
                {
                    #region invoice header

                    string DivisionCode = GetFieldValue("Division", "DivisionCode", " DivisionID = " + DivisionID, db_vms);
                    lgRwiter.WriteLine("RETURN HEADER , SENDING DATE IS " + DateTime.Now.ToString());
                    lgRwiter.WriteLine(" ***************************************************************** ");
                    //impStruct.SetValue("MANDT", "320");
                    impStruct.SetValue("CUST_CODE", CustomerCode.ToString());
                    impStruct.SetValue("OUTLET_CODE", OutletCode.ToString());
                    impStruct.SetValue("TRAN_ID", TransactionID.ToString());
                    impStruct.SetValue("TRAN_DATE", TransactionDate.ToString());
                    impStruct.SetValue("SALES_CODE", EmployeeCode.ToString());
                    impStruct.SetValue("TRAN_TYPE", Transtype.ToString());
                    impStruct.SetValue("GROSS_AMT", GrossTotal.ToString());
                    impStruct.SetValue("DISCOUNT", Discount.ToString());
                    impStruct.SetValue("NET_AMT", NetTotal.ToString());
                    impStruct.SetValue("BALANCE", RemainingAmount.ToString());
                    impStruct.SetValue("VEHICLE_CODE", WarehouseCode.ToString());
                    impStruct.SetValue("DIVISION", DivisionCode);
                    impStruct.SetValue("NOTES", "");
                    impStruct.SetValue("RETURN_ID", SourceTranID);
                    impStruct.SetValue("INVOICE_ID", invoice);

                    if (Transtype.ToString() == "5")
                    {
                        func.Invoke(dest);

                        InCubeQuery UpdateQuery1 = new InCubeQuery(db_vms, "Update [Transaction] SET Synchronized = 1 where TransactionID = '" + TransactionID.ToString() + "'");
                        err = UpdateQuery1.Execute();
                        WriteMessage("\r\n" + TransactionID.ToString() + "-Done");
                        StreamWriter wrt11 = new StreamWriter("errorret.log", true);
                        wrt11.Write(TransactionID.ToString() + " - OK\r\n");
                        wrt11.Close();

                        lgRwiter.WriteLine(" CREDIT NOTE SENT ");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine("");
                        lgRwiter.Close();

                        err = GetSalesTransactionInformation.FindNext();

                        continue;
                    }

                    #endregion

                    date = DateTime.Parse(TransactionDate.ToString());

                    string dtlQryStr = @"SELECT
TransactionDetail.TransactionID,
TransactionDetail.BatchNo,
TransactionDetail.Quantity,
TransactionDetail.Price,
TransactionDetail.ExpiryDate,
TransactionDetail.Discount,
ItemLanguage.Description AS ItemName,
Pack.Barcode,
PackTypeLanguage.Description AS PackName,
Pack.Quantity AS PcsInCse,
TransactionDetail.PackID,
Item.ItemCode,
TransactionDetail.PackStatusID

FROM TransactionDetail INNER JOIN
Pack ON TransactionDetail.PackID = Pack.PackID INNER JOIN
ItemLanguage ON Pack.ItemID = ItemLanguage.ItemID INNER JOIN
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID INNER JOIN
Item ON Pack.ItemID = Item.ItemID

WHERE (PackTypeLanguage.LanguageID = 1) AND (ItemLanguage.LanguageID = 1) AND (TransactionDetail.TransactionID = '" + TransactionID.ToString() + "')";

                    InCubeQuery dtlQry = new InCubeQuery(db_vms, dtlQryStr);
                    err = dtlQry.Execute();
                    DataRow[] detailsList = dtlQry.GetDataTable().Select();

                    int count = detailsList.Length;
                    ClearProgress();
                    SetProgressMax(count);

                    if (count == 0)
                    {
                        throw new Exception("No details found");
                    }

                    impStruct2 = func.GetTable("EX_ITEMS");
                    impStruct2.Insert();

                    lgRwiter.WriteLine(" ***************************************************************** ");
                    lgRwiter.WriteLine(" RETURN DETAILS, NUMBER OF DETAILS : " + count.ToString());
                    lgRwiter.WriteLine(" ***************************************************************** ");

                    for (int i = 0; i < count; i++)
                    {
                        ReportProgress("Sending Returns");

                        DataRow salesTxRow = detailsList[i];
                        lgRwiter.WriteLine("DETAIL NUMBER " + i.ToString());
                        //impStruct2.SetValue("MANDT", "320");
                        impStruct2.SetValue("TRAN_ID", TransactionID.ToString());
                        impStruct2.SetValue("ITEM_CODE", salesTxRow["ItemCode"].ToString().Trim());
                        lgRwiter.WriteLine(" ITEM_CODE : " + salesTxRow["ItemCode"].ToString().Trim());

                        impStruct2.SetValue("UOM", salesTxRow["PackName"].ToString().Trim());
                        lgRwiter.WriteLine(" UOM : " + salesTxRow["PackName"].ToString().Trim());

                        impStruct2.SetValue("QTY", salesTxRow["Quantity"].ToString().Trim());
                        lgRwiter.WriteLine(" QUANTITY : " + salesTxRow["Quantity"].ToString().Trim());

                        impStruct2.SetValue("PRICE", salesTxRow["Price"].ToString());
                        impStruct2.SetValue("DISCOUNT", salesTxRow["Discount"].ToString());
                        impStruct2.SetValue("EXP_DATE", salesTxRow["ExpiryDate"].ToString());
                        lgRwiter.WriteLine(" EXPIRY_DATE : " + salesTxRow["ExpiryDate"].ToString().Trim());

                        impStruct2.SetValue("BATCH", salesTxRow["BatchNo"].ToString());
                        lgRwiter.WriteLine(" BATCH_NUMBER : " + salesTxRow["BatchNo"].ToString().Trim());

                        func.Invoke(dest);
                    }

                    InCubeQuery UpdateQuery = new InCubeQuery(db_vms, "Update [Transaction] SET Synchronized = 1 where TransactionID = '" + TransactionID.ToString() + "'");
                    err = UpdateQuery.Execute();
                    lgRwiter.WriteLine(" RETURN TRANSACTION SENT ");
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine("");
                    lgRwiter.Close();
                    lgRwiter.Dispose();
                    WriteMessage("\r\n" + TransactionID.ToString() + "-Done");
                    StreamWriter wrt1 = new StreamWriter("errorret.log", true);
                    wrt1.Write(TransactionID.ToString() + " - OK\r\n");
                    wrt1.Close();
                }
                catch (Exception ex)
                {
                    Console.Write(ex.ToString());
                    StreamWriter wrt = new StreamWriter("errorret.log", true);
                    wrt.Write(ex.ToString());
                    wrt.Close();
                    WriteMessage("\r\n" + TransactionID.ToString() + " - FAILED!");
                    lgRwiter.WriteLine(" RETURN TRANSACTION FAILED ");
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine(TransactionID.ToString());
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine("");
                    lgRwiter.Close();
                    lgRwiter.Dispose();
                }
                err = GetSalesTransactionInformation.FindNext();
            }
        }

        public void SendReturnOrders(bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate)
        {
            WriteMessage("\r\n" + "Sending Return Orders");
            object TransactionID = "";
            object TransactionDate = "";
            object CustomerName = "";
            object CustomerCode = "";
            object WarehouseCode = "";
            object EmployeeCode = "";
            object CustomerID = "";
            object OutletID = "";
            object Customeraddress = "";
            object Customeraddress1 = "";
            object Customeraddress2 = "";
            object OutletCode = "";
            object LPONumber = "";
            object GrossTotal = "";
            object Discount = "";
            object NetTotal = "";
            object RemainingAmount = "";
            object DivisionID = "";
            object CHANNEL_CODE = "";
            object LPO = "";
            object SALES_GRP = "";
            DateTime date;
            //THIS IS THE STREAM WRITER FOR THE INVOICES LOG

            string invoiceLog = string.Empty;

            RfcDestination dest = RfcDestinationManager.GetDestination(SendServerName);
            IRfcFunction func;
            IRfcStructure impStruct;
            IRfcTable impStruct2;

            func = dest.Repository.CreateFunction("ZSD_FM_PDT_SALES_N");
            impStruct = func.GetStructure("EX_HEADER");

            string QueryString = @"SELECT [Transaction].TransactionID,
            [Transaction].TransactionDate,
            CustomerOutlet.CustomerCode,
            Warehouse.WarehouseCode,
            Employee.EmployeeCode,
            CustomerOutletLanguage.Description,
            CustomerOutlet.CustomerID,
            CustomerOutlet.OutletID,
            CustomerOutletLanguage.Address,
            CustomerOutlet.CustomerCode as OutletCode,
            [Transaction].GrossTotal,
            [Transaction].Discount,
            [Transaction].NetTotal,
            [Transaction].NetTotal RemainingAmount,
            [Transaction].DivisionID,
			CHANNEL.CHANNELCODE,
[Transaction].Notes LPO,
CG.GROUPCODE

            FROM         [Transaction] INNER JOIN
CustomerOutletLanguage ON [Transaction].CustomerID = CustomerOutletLanguage.CustomerID INNER JOIN
CustomerOutlet ON [Transaction].CustomerID = CustomerOutlet.CustomerID AND [Transaction].OutletID = CustomerOutlet.OutletID AND
CustomerOutletLanguage.CustomerID = CustomerOutlet.CustomerID AND CustomerOutletLanguage.OutletID = CustomerOutlet.OutletID
INNER JOIN CUSTOMEROUTLETGROUP COG ON CustomerOutlet.CUSTOMERID=COG.CUSTOMERID AND CustomerOutlet.OUTLETID=COG.OUTLETID
INNER JOIN CUSTOMERGROUP CG ON COG.GROUPID=CG.GROUPID
INNER JOIN CHANNEL ON CG.CHANNELID=CHANNEL.CHANNELID
INNER JOIN Employee ON [Transaction].EmployeeID = Employee.EmployeeID
INNER JOIN WAREHOUSE ON [Transaction].warehouseID=WAREHOUSE.WAREHOUSEID
WHERE ([Transaction].Synchronized = 0) and [Transaction].transactionTypeID=2
           ";
            if (!AllSalespersons)
            {
                QueryString += " AND [Transaction].EmployeeID = " + Salesperson;
            }

            InCubeQuery GetSalesTransactionInformation = new InCubeQuery(db_vms, QueryString);

            err = GetSalesTransactionInformation.Execute();
            err = GetSalesTransactionInformation.FindFirst();
            while (err == InCubeErrors.Success)
            {
                string filename = "INV-Log" + DateTime.Today.Year.ToString() + DateTime.Today.Month.ToString() + DateTime.Today.Day.ToString() + ".log";
                string filePath = CoreGeneral.Common.StartupPath + "\\" + filename;
                if (!File.Exists(filePath))
                {
                    FileStream fs = File.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite);
                    fs.Close();
                    fs.Dispose();
                }
                StreamWriter lgRwiter = new StreamWriter(filename, true);

                try
                {
                    Customeraddress = "";
                    Customeraddress1 = "";
                    Customeraddress2 = "";

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
                        err = GetSalesTransactionInformation.GetField(10, ref GrossTotal);
                        err = GetSalesTransactionInformation.GetField(11, ref Discount);
                        err = GetSalesTransactionInformation.GetField(12, ref NetTotal);
                        err = GetSalesTransactionInformation.GetField(13, ref RemainingAmount);
                        err = GetSalesTransactionInformation.GetField(14, ref DivisionID);
                        err = GetSalesTransactionInformation.GetField(15, ref CHANNEL_CODE);
                        err = GetSalesTransactionInformation.GetField(16, ref LPO);
                        err = GetSalesTransactionInformation.GetField(17, ref SALES_GRP);
                    }
                    #endregion

                    date = DateTime.Parse(TransactionDate.ToString());
                    if (LPO.ToString().Equals(string.Empty)) LPO = TransactionID;
                    //THE FOLLOWING WILL NOT INSERT THE HEADER UNLESS THE GROSS TOTAL FOR THE HEADER IS EQUAL TO THE sum(quantity*price) IN THE TRANSACTION DETAIL .
                    string totalDetails = GetFieldValue("TransactionDetail", "isnull(sum(quantity*price),0)", "TransactionID='" + TransactionID + "'", db_vms).Trim();
                    if (decimal.Parse(GrossTotal.ToString().Trim()) != decimal.Parse(totalDetails))
                    {
                        lgRwiter.WriteLine("INVOICE HEADER , THE GROSS TOTAL IS NOT EQUAL TO THE SUM OF DETAILS AMOUNT FOR TRANSACTION  " + TransactionID + " SENDING DATE IS " + DateTime.Now.ToString());
                        lgRwiter.Close();
                        lgRwiter.Dispose();
                        WriteMessage("\r\n" + "THE GROSS TOTAL IS NOT EQUAL TO THE SUM OF DETAILS AMOUNT FOR TRANSACTION  " + TransactionID + " SENDING ABORTED ! .");
                        err = GetSalesTransactionInformation.FindNext();
                        continue;
                    }
                    #region invoice header

                    string DivisionCode = GetFieldValue("Division", "DivisionCode", " DivisionID = " + DivisionID, db_vms);
                    lgRwiter.WriteLine("INVOICE HEADER , SENDING DATE IS " + DateTime.Now.ToString());
                    lgRwiter.WriteLine(" ***************************************************************** ");
                    impStruct.SetValue("CUST_CODE", CustomerCode.ToString());
                    lgRwiter.WriteLine(" CUSTOMER_CODE : " + CustomerCode.ToString().Trim());
                    impStruct.SetValue("OUTLET_CODE", OutletCode.ToString());
                    lgRwiter.WriteLine(" OUTLET_CODE : " + OutletCode.ToString().Trim());
                    impStruct.SetValue("TRAN_ID", TransactionID.ToString());
                    lgRwiter.WriteLine(" TRANSACTION_ID : " + TransactionID.ToString().Trim());
                    impStruct.SetValue("TRAN_DATE", TransactionDate.ToString());
                    impStruct.SetValue("SALES_GRP", SALES_GRP);//NEW
                    impStruct.SetValue("AUART", "ZRE");//NEW
                    impStruct.SetValue("DIS_CHL", CHANNEL_CODE);//NEW
                    impStruct.SetValue("CUS_PO", LPO);//NEW
                    lgRwiter.WriteLine(" TRANSACTION_DATE : " + TransactionDate.ToString().Trim());
                    impStruct.SetValue("SALES_CODE", EmployeeCode.ToString());
                    lgRwiter.WriteLine(" EMPLOYEE_CODE : " + EmployeeCode.ToString().Trim());
                    impStruct.SetValue("TRAN_TYPE", "2");
                    impStruct.SetValue("GROSS_AMT", GrossTotal.ToString());
                    impStruct.SetValue("DISCOUNT", Discount.ToString());
                    impStruct.SetValue("NET_AMT", NetTotal.ToString());
                    impStruct.SetValue("BALANCE", RemainingAmount.ToString());
                    impStruct.SetValue("VEHICLE_CODE", WarehouseCode.ToString());
                    impStruct.SetValue("DIVISION", DivisionCode);
                    impStruct.SetValue("NOTES", "xx");

                    #endregion

                    string dtlQryStr = @"
SELECT
TransactionDetail.TransactionID,
Item.ItemCode,
PackTypeLanguage.Description as PackName,
TransactionDetail.Quantity,
TransactionDetail.Price,
TransactionDetail.Discount,
TransactionDetail.ExpiryDate,
TransactionDetail.BatchNo,
TransactionDetail.PackStatusID ReturnReason
FROM TransactionDetail INNER JOIN
Pack ON TransactionDetail.PackID = Pack.PackID INNER JOIN
Item ON Pack.ItemID = Item.ItemID INNER JOIN
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID
WHERE
(PackTypeLanguage.LanguageID = 1)

AND (TransactionDetail.TransactionID = '" + TransactionID.ToString() + @"')";

                    InCubeQuery dtlQry = new InCubeQuery(db_vms, dtlQryStr);
                    err = dtlQry.Execute();
                    DataRow[] detailsList = dtlQry.GetDataTable().Select();

                    int count = detailsList.Length;

                    ClearProgress();
                    SetProgressMax(count);

                    if (count == 0)
                    {
                        throw new Exception("No details found");
                    }

                    impStruct2 = func.GetTable("EX_ITEMS");
                    impStruct2.Insert();
                    lgRwiter.WriteLine(" ***************************************************************** ");
                    lgRwiter.WriteLine(" TRANSACTION DETAILS, NUMBER OF DETAILS : " + count.ToString());
                    lgRwiter.WriteLine(" ***************************************************************** ");
                    for (int i = 0; i < count; i++)
                    {
                        DataRow salesTxRow = detailsList[i];
                        if (decimal.Parse(salesTxRow["Price"].ToString().Trim()) == 0)//FF DONT WANT ANY LINE ITEM WITH ZERO PRICE TO GO TO SAP
                        {
                            continue;
                        }
                        ReportProgress("Sending Invoices");

                        string PackStatus = salesTxRow["ReturnReason"].ToString().Trim();

                        lgRwiter.WriteLine("DETAIL NUMBER " + i.ToString());
                        impStruct2.SetValue("TRAN_ID", TransactionID.ToString());
                        impStruct2.SetValue("ITEM_CODE", salesTxRow["ItemCode"].ToString().Trim());
                        lgRwiter.WriteLine(" ITEM_CODE : " + salesTxRow["ItemCode"].ToString().Trim());
                        impStruct2.SetValue("UOM", salesTxRow["PackName"].ToString().Trim());
                        lgRwiter.WriteLine(" UOM : " + salesTxRow["PackName"].ToString().Trim());
                        impStruct2.SetValue("QTY", salesTxRow["Quantity"].ToString().Trim());
                        lgRwiter.WriteLine(" QUANTITY : " + salesTxRow["Quantity"].ToString().Trim());
                        impStruct2.SetValue("PRICE", salesTxRow["Price"].ToString());
                        impStruct2.SetValue("DISCOUNT", salesTxRow["Discount"].ToString());
                        impStruct2.SetValue("EXP_DATE", "");//USED TO BE salesTxRow["ExpiryDate"].ToString()
                        lgRwiter.WriteLine(" EXPIRY_DATE : " + salesTxRow["ExpiryDate"].ToString().Trim());
                        impStruct2.SetValue("BATCH", "");//USED TO BE salesTxRow["BatchNo"].ToString()
                        lgRwiter.WriteLine(" BATCH_NUMBER : " + salesTxRow["BatchNo"].ToString().Trim());
                        switch (DivisionCode + PackStatus)
                        {
                            case "011":
                                impStruct2.SetValue("ST_LOC", "1903");
                                lgRwiter.WriteLine(" ST_LOC : 1903");
                                impStruct2.SetValue("MFRGR", "001");
                                lgRwiter.WriteLine(" MFRGR : 001");
                                break;

                            case "012":
                                impStruct2.SetValue("ST_LOC", "1904");
                                lgRwiter.WriteLine(" ST_LOC : 1904");
                                impStruct2.SetValue("MFRGR", "002");
                                lgRwiter.WriteLine(" MFRGR : 002");
                                break;

                            case "013":
                                impStruct2.SetValue("ST_LOC", "1901");
                                lgRwiter.WriteLine(" ST_LOC : 1901");
                                impStruct2.SetValue("MFRGR", "003");
                                lgRwiter.WriteLine(" MFRGR : 003");
                                break;

                            case "014":
                                impStruct2.SetValue("ST_LOC", "1902");
                                lgRwiter.WriteLine(" ST_LOC : 1902");
                                impStruct2.SetValue("MFRGR", "013");
                                lgRwiter.WriteLine(" MFRGR : 013");
                                break;

                            case "021":
                                impStruct2.SetValue("ST_LOC", "2153");
                                lgRwiter.WriteLine(" ST_LOC : 2153");
                                impStruct2.SetValue("MFRGR", "004");
                                lgRwiter.WriteLine(" MFRGR : 004");
                                break;

                            case "022":
                                impStruct2.SetValue("ST_LOC", "2154");
                                lgRwiter.WriteLine(" ST_LOC : 2154");
                                impStruct2.SetValue("MFRGR", "005");
                                lgRwiter.WriteLine(" MFRGR : 005");
                                break;

                            case "023":
                                impStruct2.SetValue("ST_LOC", "2151");
                                lgRwiter.WriteLine(" ST_LOC : 2151");
                                impStruct2.SetValue("MFRGR", "006");
                                lgRwiter.WriteLine(" MFRGR : 006");
                                break;

                            case "024":
                                impStruct2.SetValue("ST_LOC", "2152");
                                lgRwiter.WriteLine(" ST_LOC : 2152");
                                impStruct2.SetValue("MFRGR", "015");
                                lgRwiter.WriteLine(" MFRGR : 015");
                                break;

                            case "031":
                                impStruct2.SetValue("ST_LOC", "3903");
                                lgRwiter.WriteLine(" ST_LOC : 3903");
                                impStruct2.SetValue("MFRGR", "007");
                                lgRwiter.WriteLine(" MFRGR : 007");
                                break;

                            case "032":
                                impStruct2.SetValue("ST_LOC", "3904");
                                lgRwiter.WriteLine(" ST_LOC : 3904");
                                impStruct2.SetValue("MFRGR", "008");
                                lgRwiter.WriteLine(" MFRGR : 008");
                                break;

                            case "033":
                                impStruct2.SetValue("ST_LOC", "3901");
                                lgRwiter.WriteLine(" ST_LOC : 3901");
                                impStruct2.SetValue("MFRGR", "009");
                                lgRwiter.WriteLine(" MFRGR : 009");
                                break;

                            case "034":
                                impStruct2.SetValue("ST_LOC", "3902");
                                lgRwiter.WriteLine(" ST_LOC : 3902");
                                impStruct2.SetValue("MFRGR", "014");
                                lgRwiter.WriteLine(" MFRGR : 014");
                                break;

                            case "041":
                                impStruct2.SetValue("ST_LOC", "3903");
                                lgRwiter.WriteLine(" ST_LOC : 3903");
                                impStruct2.SetValue("MFRGR", "010");
                                lgRwiter.WriteLine(" MFRGR : 010");
                                break;

                            case "042":
                                impStruct2.SetValue("ST_LOC", "3904");
                                lgRwiter.WriteLine(" ST_LOC : 3904");
                                impStruct2.SetValue("MFRGR", "011");
                                lgRwiter.WriteLine(" MFRGR : 011");
                                break;

                            case "043":
                                impStruct2.SetValue("ST_LOC", "3901");
                                lgRwiter.WriteLine(" ST_LOC : 3901");
                                impStruct2.SetValue("MFRGR", "012");
                                lgRwiter.WriteLine(" MFRGR : 012");
                                break;
                        }

                        func.Invoke(dest);
                    }

                    InCubeQuery UpdateQuery = new InCubeQuery(db_vms, "Update [Transaction] SET Synchronized = 1 where TransactionID = '" + TransactionID.ToString() + "'");
                    err = UpdateQuery.Execute();
                    UpdateQuery = new InCubeQuery(db_vms, "Update [Transaction] SET RemainingAmount = 0 where SourceTransactionID = '" + TransactionID.ToString() + "'");
                    err = UpdateQuery.Execute();
                    lgRwiter.WriteLine(" TRANSACTION SENT ");
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine("");
                    lgRwiter.Close();
                    lgRwiter.Dispose();
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
                    lgRwiter.WriteLine(" SALES TRANSACTION FAILED ");
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine(TransactionID.ToString());
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine("");
                    lgRwiter.Close();
                    lgRwiter.Dispose();
                }
                err = GetSalesTransactionInformation.FindNext();
            }
        }

        public override void SendTransfers()
        {
            try
            {
                SendIVTransfers(Filters.EmployeeID == -1, Filters.EmployeeID.ToString(), Filters.FromDate, Filters.ToDate);
                //TO DO VERY IMPORTANT, ANY WH TRANSACTION COMES FROM SAP, WE NEED TO DIFFERENTIATE IT SO THAT WHEN WE SEND TRANSACTIONS TO SAP, WE DONT SEND THEM. 
                WriteMessage("\r\n" + "Sending LoadRequests");
                object ComapnyCode = "";
                object TransactionID = "";
                object WHFrom = "";
                object LocationTo = "";
                object TransactionDate = "";
                object Status = "";
                object Notes = "";
                object TransactionType = "";
                object IsLiquidation = "";
                object DeliveryDate = "";
                object ItemCode = "";
                object UOM = "";
                object Qty = "";
                object SalesOrganizationCode = "";
                object LineNumber = "";
                object DivisionCode = "";

                string invoiceLog = string.Empty;

                RfcDestination dest = RfcDestinationManager.GetDestination(SendServerName);
                IRfcFunction func;
                IRfcTable impStruct2;

                func = dest.Repository.CreateFunction("ZHIB_PROCESS_TRANSFER_REQUEST");
                //func.GetTable("ITEMTAB");
                //or WHT.TransactionTypeID=2
                string QueryString = @"
                     select 

WHT.TransactionID
FROM 
WAREHOUSETRANSACTION WHT
WHERE 
(WHT.TransactionTypeID in (1,2) and WarehouseTransactionStatusID IN (4,5)) and WHT.TransactionID not in (select TransactionID from WH_Sync)   order by WHT.transactiondate
 ";
                if (Filters.EmployeeID != -1)
                {
                    QueryString += " AND WHT.RequestedBy = " + Filters.EmployeeID;
                }

                InCubeQuery GetSalesTransactionInformation = new InCubeQuery(db_vms, QueryString);

                err = GetSalesTransactionInformation.Execute();
                err = GetSalesTransactionInformation.FindFirst();
                while (err == InCubeErrors.Success)
                {
                    string filename = "INV-Log" + DateTime.Today.Year.ToString() + DateTime.Today.Month.ToString() + DateTime.Today.Day.ToString() + ".log";
                    string filePath = CoreGeneral.Common.StartupPath + "\\" + filename;
                    if (!File.Exists(filePath))
                    {
                        FileStream fs = File.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite);
                        fs.Close();
                        fs.Dispose();
                    }
                    StreamWriter lgRwiter = new StreamWriter(filename, true);

                    try
                    {

                        #region Get SalesTransaction Information
                        {
                            err = GetSalesTransactionInformation.GetField("TransactionID", ref TransactionID);

                        }

                        #endregion

                        //date = DateTime.Parse(TransactionDate.ToString());
                        //THE FOLLOWING WILL NOT INSERT THE HEADER UNLESS THE GROSS TOTAL FOR THE HEADER IS EQUAL TO THE sum(quantity*price) IN THE TRANSACTION DETAIL .
                        //string totalDetails = GetFieldValue("TransactionDetail", "isnull(sum(quantity*price),0)", "TransactionID='" + TransactionID + "'", db_vms).Trim();
                        //if (decimal.Parse(GrossTotal.ToString().Trim()) != decimal.Parse(totalDetails))
                        //{
                        //    lgRwiter.WriteLine("INVOICE HEADER , THE GROSS TOTAL IS NOT EQUAL TO THE SUM OF DETAILS AMOUNT FOR TRANSACTION  " + TransactionID + " SENDING DATE IS " + DateTime.Now.ToString());
                        //    lgRwiter.Close();
                        //    lgRwiter.Dispose();
                        //    WriteMessage("\r\n" + "THE GROSS TOTAL IS NOT EQUAL TO THE SUM OF DETAILS AMOUNT FOR TRANSACTION  " + TransactionID + " SENDING ABORTED ! .");
                        //    err = GetSalesTransactionInformation.FindNext();
                        //    continue;
                        //}
                        #region invoice header

                        #endregion

                        string dtlQryStr = @"
select 
ISNULL(O.OrganizationCode,'7710') ComapnyCode,
WHT.TransactionID,
W1.WAREHOUSECODE WHFrom,
WHT.TransactionDate,
case(WHT.WarehouseTransactionStatusID) when 1 then 1 else case when WHT.WarehouseTransactionStatusID>=4 then 5 else 0 end end Status,
''Notes,
WHT.TRANSACTIONTYPEID TransactionType,
0 IsLiquidation,
WHT.TransactionDate DeliveryDate,
I.ItemCode,
PTL.DESCRIPTION UOM,
SUM(WHD.QUANTITY) Qty,
O.OrganizationCode SalesOrganizationCode,
ROW_NUMBER()OVER(ORDER BY WHD.packid)LineNumber,
ISNULL(D.DivisionCode,'70') DivisionCode,
T.TERRITORYCODE,
W2.WarehouseCode LocationTo 

FROM 
WAREHOUSETRANSACTION WHT INNER JOIN WHTRANSDETAIL WHD ON WHT.TRANSACTIONID=WHD.TRANSACTIONID AND WHT.WAREHOUSEID=WHD.WAREHOUSEID
INNER JOIN WAREHOUSE W1 ON WHT.RefWarehouseID=W1.WAREHOUSEID
INNER JOIN WAREHOUSE W2 ON WHT.WAREHOUSEID=W2.WAREHOUSEID
left outer JOIN ORGANIZATION O ON WHT.OrganizationID=O.OrganizationID
INNER JOIN PACK P ON WHD.PackID=P.PACKID
INNER JOIN ITEM I ON P.ITEMID=I.ITEMID
INNER JOIN PACKTYPELANGUAGE PTL ON P.PackTypeID=PTL.PackTypeID AND PTL.LanguageID=1
left outer join employeevehicle ev on w2.warehouseid=ev.vehicleid
left outer JOIN EMPLOYEETERRITORY ET ON ev.EmployeeID=ET.EMPLOYEEID
left outer JOIN TERRITORY T ON ET.TERRITORYID=T.TERRITORYID
inner join itemcategory ic on i.itemcategoryid=ic.itemcategoryid
LEFT OUTER join division d on ic.divisionid=d.divisionid
WHERE 
WHT.TRANSACTIONID='" + TransactionID + @"'
GROUP BY 
O.OrganizationCode ,
WHT.TransactionID,
W1.WAREHOUSECODE ,
W2.WAREHOUSECODE ,
WHT.TransactionDate,
WHT.TRANSACTIONTYPEID ,
WHT.TransactionDate ,
I.ItemCode,
PTL.DESCRIPTION ,
D.DivisionCode,T.TERRITORYCODE,WHT.WarehouseTransactionStatusID,WHD.packid,W2.WarehouseCode
";
                        InCubeQuery dtlQry = new InCubeQuery(db_vms, dtlQryStr);
                        err = dtlQry.Execute();
                        //DataRow[] detailsList = dtlQry.GetDataTable().Select();
                        DataTable DetailTable = dtlQry.GetDataTable();
                        int count = DetailTable.Rows.Count;
                        int RowConter = 0;
                        ClearProgress();
                        SetProgressMax(count);
                        if (count == 0)
                        {
                            throw new Exception("No details found");
                        }

                        impStruct2 = func.GetTable("ITEMTAB");
                        impStruct2.Clear();
                        lgRwiter.WriteLine(" ***************************************************************** ");
                        lgRwiter.WriteLine(" TRANSACTION DETAILS, NUMBER OF DETAILS : " + count.ToString());
                        lgRwiter.WriteLine(" ***************************************************************** ");
                        foreach (DataRow salesTxRow in DetailTable.Rows)
                        {

                            RowConter++;
                            ReportProgress("Sending Invoices");
                            impStruct2.Append();
                            ComapnyCode = salesTxRow["ComapnyCode"].ToString().Trim();
                            TransactionID = salesTxRow["TransactionID"].ToString().Trim();
                            WHFrom = salesTxRow["WHFrom"].ToString().Trim();
                            LocationTo = salesTxRow["LocationTo"].ToString().Trim();
                            TransactionDate = salesTxRow["TransactionDate"].ToString().Trim();
                            Status = salesTxRow["Status"].ToString().Trim();
                            Notes = salesTxRow["Notes"].ToString().Trim();
                            TransactionType = salesTxRow["TransactionType"].ToString().Trim();
                            //if(TransactionType.Equals("2"))
                            //{
                            //string tempLoc=string.Empty;
                            //tempLoc = WHFrom.ToString();
                            //WHFrom = LocationTo;
                            //LocationTo = tempLoc;
                            //}
                            //IsLiquidation = salesTxRow["IsLiquidation"].ToString().Trim();
                            //DeliveryDate = salesTxRow["DeliveryDate"].ToString().Trim();
                            ItemCode = salesTxRow["ItemCode"].ToString().Trim();
                            UOM = salesTxRow["UOM"].ToString().Trim();
                            Qty = salesTxRow["Qty"].ToString().Trim();
                            SalesOrganizationCode = salesTxRow["SalesOrganizationCode"].ToString().Trim();
                            LineNumber = salesTxRow["LineNumber"].ToString().Trim();
                            DivisionCode = salesTxRow["DivisionCode"].ToString().Trim();
                            string loc = LocationTo.ToString().Substring(4);
                            ComapnyCode = "7710";
                            impStruct2.SetValue("BUKRS", ComapnyCode.ToString());
                            impStruct2.SetValue("TRDAT", DateTime.Parse(TransactionDate.ToString()).ToString("yyyyMMddHHmmss"));
                            impStruct2.SetValue("IHREZ", TransactionID.ToString());
                            impStruct2.SetValue("WERKS", WHFrom.ToString());
                            impStruct2.SetValue("EBELP", (RowConter * 10).ToString());
                            impStruct2.SetValue("MATNR", "0000000000" + ItemCode.ToString());
                            impStruct2.SetValue("SLOC1", "0001");
                            impStruct2.SetValue("SLOC2", loc);
                            impStruct2.SetValue("MENGE", Qty.ToString());
                            impStruct2.SetValue("MEINS", UOM.ToString());
                            impStruct2.SetValue("ERFMG", "0");
                            impStruct2.SetValue("VGART", TransactionType);


                            //string temp = DateTime.Parse(TransactionDate.ToString()).ToString("yyyyMMddHHmmss");

                            //lgRwiter.WriteLine("DETAIL NUMBER " + RowConter.ToString());
                            //lgRwiter.WriteLine(" ITEM_CODE : " + salesTxRow["ItemCode"].ToString().Trim());
                            //lgRwiter.WriteLine(" UOM : " + salesTxRow["PackName"].ToString().Trim());
                            //lgRwiter.WriteLine(" QUANTITY : " + salesTxRow["Quantity"].ToString().Trim());
                            //lgRwiter.WriteLine(" EXPIRY_DATE : " + salesTxRow["ExpiryDate"].ToString().Trim());
                            //lgRwiter.WriteLine(" BATCH_NUMBER : " + salesTxRow["BatchNo"].ToString().Trim());



                        }
                        func.Invoke(dest);
                        InCubeQuery UpdateQuery = new InCubeQuery(db_vms, "Update [WarehouseTransaction] SET Synchronized = 1 where TransactionID = '" + TransactionID.ToString() + "'");
                        err = UpdateQuery.Execute();
                        lgRwiter.WriteLine(" TRANSACTION SENT ");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine("");
                        lgRwiter.Close();
                        lgRwiter.Dispose();
                        WriteMessage("\r\n" + TransactionID.ToString() + "  <" + DateTime.Now.ToString() + "> OK");
                        StreamWriter wrt = new StreamWriter("errorInv.log", true);
                        wrt.Write("\n" + TransactionID.ToString() + " OK\r\n");
                        wrt.Close();
                        if (err == InCubeErrors.Success)
                        {
                            string update = string.Format("INSERT INTO WH_Sync (TRANSACTIONID,StatusID,StatusDate) VALUES('{0}',1,GetDate())", TransactionID); //string.Format("update [WarehouseTransaction] set Synchronized=1 where TransactionID='{0}'", InvoiceNumberComplete);
                            UpdateQuery = new InCubeQuery(update, db_vms);
                            err = UpdateQuery.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                        StreamWriter wrt = new StreamWriter("errorInv.log", true);
                        wrt.Write(ex.ToString());
                        wrt.Close();
                        WriteMessage("\r\n" + TransactionID.ToString() + " - Not Sent!");
                        lgRwiter.WriteLine(" <" + DateTime.Now.ToString() + "> SALES TRANSACTION FAILED ");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(TransactionID.ToString());
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine("");
                        lgRwiter.Close();
                        lgRwiter.Dispose();
                    }
                    err = GetSalesTransactionInformation.FindNext();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void SendIVTransfers(bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate)
        {
            try
            {
                //TO DO VERY IMPORTANT, ANY WH TRANSACTION COMES FROM SAP, WE NEED TO DIFFERENTIATE IT SO THAT WHEN WE SEND TRANSACTIONS TO SAP, WE DONT SEND THEM. 
                WriteMessage("\r\n" + "Sending LoadRequests");
                object ComapnyCode = "";
                object TransactionID = "";
                object WHFrom = "";
                object LocationTo = "";
                object TransactionDate = "";
                object Status = "";
                object Notes = "";
                object TransactionType = "";
                object IsLiquidation = "";
                object DeliveryDate = "";
                object ItemCode = "";
                object UOM = "";
                object Qty = "";
                object SalesOrganizationCode = "";
                object LineNumber = "";
                object DivisionCode = "";
                object LPONumber = "";
                object VendorCode = "";

                string invoiceLog = string.Empty;

                RfcDestination dest = RfcDestinationManager.GetDestination(SendServerName);
                IRfcFunction func;
                IRfcTable impStruct2;

                func = dest.Repository.CreateFunction("ZHIB_PROCESS_GOODS_RECEIVING");
                //func.GetTable("ITEMTAB");
                //or WHT.TransactionTypeID=2
                string QueryString = @"
                     select 

WHT.TransactionID
FROM 
WAREHOUSETRANSACTION WHT
WHERE 
(WHT.TransactionTypeID in (3) and WarehouseTransactionStatusID IN (4)) and WHT.TransactionID not in (select TransactionID from WH_Sync)  order by WHT.transactiondate
 ";
                if (!AllSalespersons)
                {
                    QueryString += " AND WHT.RequestedBy = " + Salesperson;
                }

                InCubeQuery GetSalesTransactionInformation = new InCubeQuery(db_vms, QueryString);

                err = GetSalesTransactionInformation.Execute();
                err = GetSalesTransactionInformation.FindFirst();
                while (err == InCubeErrors.Success)
                {
                    string filename = "INV-Log" + DateTime.Today.Year.ToString() + DateTime.Today.Month.ToString() + DateTime.Today.Day.ToString() + ".log";
                    string filePath = CoreGeneral.Common.StartupPath + "\\" + filename;
                    if (!File.Exists(filePath))
                    {
                        FileStream fs = File.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite);
                        fs.Close();
                        fs.Dispose();
                    }
                    StreamWriter lgRwiter = new StreamWriter(filename, true);

                    try
                    {

                        #region Get SalesTransaction Information
                        {
                            err = GetSalesTransactionInformation.GetField("TransactionID", ref TransactionID);

                        }

                        #endregion

                        //date = DateTime.Parse(TransactionDate.ToString());
                        //THE FOLLOWING WILL NOT INSERT THE HEADER UNLESS THE GROSS TOTAL FOR THE HEADER IS EQUAL TO THE sum(quantity*price) IN THE TRANSACTION DETAIL .
                        //string totalDetails = GetFieldValue("TransactionDetail", "isnull(sum(quantity*price),0)", "TransactionID='" + TransactionID + "'", db_vms).Trim();
                        //if (decimal.Parse(GrossTotal.ToString().Trim()) != decimal.Parse(totalDetails))
                        //{
                        //    lgRwiter.WriteLine("INVOICE HEADER , THE GROSS TOTAL IS NOT EQUAL TO THE SUM OF DETAILS AMOUNT FOR TRANSACTION  " + TransactionID + " SENDING DATE IS " + DateTime.Now.ToString());
                        //    lgRwiter.Close();
                        //    lgRwiter.Dispose();
                        //    WriteMessage("\r\n" + "THE GROSS TOTAL IS NOT EQUAL TO THE SUM OF DETAILS AMOUNT FOR TRANSACTION  " + TransactionID + " SENDING ABORTED ! .");
                        //    err = GetSalesTransactionInformation.FindNext();
                        //    continue;
                        //}
                        #region invoice header

                        #endregion

                        string dtlQryStr = @"
select 
ISNULL(O.OrganizationCode,'7710') ComapnyCode,
WHT.TransactionID,
W1.WAREHOUSECODE WHFrom,
WHT.TransactionDate,
case(WHT.WarehouseTransactionStatusID) when 1 then 1 else case when WHT.WarehouseTransactionStatusID>=4 then 5 else 0 end end Status,
''Notes,
WHT.TRANSACTIONTYPEID TransactionType,
0 IsLiquidation,
WHT.TransactionDate DeliveryDate,
I.ItemCode,
PTL.DESCRIPTION UOM,
SUM(WHD.QUANTITY) Qty,
O.OrganizationCode SalesOrganizationCode,
ROW_NUMBER()OVER(ORDER BY WHD.packid)LineNumber,
ISNULL(D.DivisionCode,'70') DivisionCode,
WHT.LPONumber,
V.VendorCode

FROM 
WAREHOUSETRANSACTION WHT INNER JOIN WHTRANSDETAIL WHD ON WHT.TRANSACTIONID=WHD.TRANSACTIONID AND WHT.WAREHOUSEID=WHD.WAREHOUSEID
INNER JOIN WAREHOUSE W1 ON WHT.WarehouseID=W1.WAREHOUSEID
LEFT OUTER JOIN ORGANIZATION O ON WHT.OrganizationID=O.OrganizationID
INNER JOIN PACK P ON WHD.PackID=P.PACKID
INNER JOIN ITEM I ON P.ITEMID=I.ITEMID
INNER JOIN PACKTYPELANGUAGE PTL ON P.PackTypeID=PTL.PackTypeID AND PTL.LanguageID=1
inner join itemcategory ic on i.itemcategoryid=ic.itemcategoryid
LEFT OUTER join division d on ic.divisionid=d.divisionid
inner join Vendor V on WHT.vendorID=V.VendorID
WHERE 
WHT.TRANSACTIONID='" + TransactionID + @"'
GROUP BY 
O.OrganizationCode ,
WHT.TransactionID,
W1.WAREHOUSECODE ,
WHT.TransactionDate,
WHT.TRANSACTIONTYPEID ,
WHT.TransactionDate ,
I.ItemCode,
PTL.DESCRIPTION ,
D.DivisionCode,WHT.WarehouseTransactionStatusID,WHD.packid,WHT.LPONumber,
V.VendorCode
";
                        InCubeQuery dtlQry = new InCubeQuery(db_vms, dtlQryStr);
                        err = dtlQry.Execute();
                        //DataRow[] detailsList = dtlQry.GetDataTable().Select();
                        DataTable DetailTable = dtlQry.GetDataTable();
                        int count = DetailTable.Rows.Count;
                        int RowConter = 0;
                        ClearProgress();
                        SetProgressMax(count);
                        if (count == 0)
                        {
                            throw new Exception("No details found");
                        }

                        impStruct2 = func.GetTable("ITEMTAB");
                        impStruct2.Clear();
                        lgRwiter.WriteLine(" ***************************************************************** ");
                        lgRwiter.WriteLine(" TRANSACTION DETAILS, NUMBER OF DETAILS : " + count.ToString());
                        lgRwiter.WriteLine(" ***************************************************************** ");
                        foreach (DataRow salesTxRow in DetailTable.Rows)
                        {
                            RowConter++;
                            ReportProgress("Sending Invoices");
                            impStruct2.Append();
                            ComapnyCode = salesTxRow["ComapnyCode"].ToString().Trim();
                            TransactionID = salesTxRow["TransactionID"].ToString().Trim();
                            WHFrom = salesTxRow["WHFrom"].ToString().Trim();
                            //LocationTo = salesTxRow["LocationTo"].ToString().Trim();
                            TransactionDate = salesTxRow["TransactionDate"].ToString().Trim();
                            Status = salesTxRow["Status"].ToString().Trim();
                            Notes = salesTxRow["Notes"].ToString().Trim();
                            TransactionType = salesTxRow["TransactionType"].ToString().Trim();
                            LPONumber = salesTxRow["LPONumber"].ToString().Trim();
                            VendorCode = salesTxRow["VendorCode"].ToString().Trim();
                            //if(TransactionType.Equals("2"))
                            //{
                            //string tempLoc=string.Empty;
                            //tempLoc = WHFrom.ToString();
                            //WHFrom = LocationTo;
                            //LocationTo = tempLoc;
                            //}
                            //IsLiquidation = salesTxRow["IsLiquidation"].ToString().Trim();
                            //DeliveryDate = salesTxRow["DeliveryDate"].ToString().Trim();
                            ItemCode = salesTxRow["ItemCode"].ToString().Trim();
                            UOM = salesTxRow["UOM"].ToString().Trim();
                            Qty = salesTxRow["Qty"].ToString().Trim();
                            SalesOrganizationCode = salesTxRow["SalesOrganizationCode"].ToString().Trim();
                            LineNumber = salesTxRow["LineNumber"].ToString().Trim();
                            DivisionCode = salesTxRow["DivisionCode"].ToString().Trim();

                            ComapnyCode = "7710";
                            impStruct2.SetValue("BUKRS", ComapnyCode.ToString());
                            impStruct2.SetValue("TRDAT", DateTime.Parse(TransactionDate.ToString()).ToString("yyyyMMddHHmmss"));
                            impStruct2.SetValue("IHREZ", TransactionID.ToString());
                            impStruct2.SetValue("WERKS", WHFrom.ToString());
                            impStruct2.SetValue("LIFNR", VendorCode.ToString());
                            impStruct2.SetValue("EBELN", LPONumber.ToString());
                            impStruct2.SetValue("EBELP", (RowConter * 10).ToString());
                            impStruct2.SetValue("MATNR", "0000000000" + ItemCode.ToString());
                            impStruct2.SetValue("LGORT", "0001");
                            impStruct2.SetValue("MENGE", Qty.ToString());
                            impStruct2.SetValue("MEINS", UOM.ToString());
                            //impStruct2.SetValue("ERFMG", "");
                            //impStruct2.SetValue("VGART", TransactionType);


                            //string temp = DateTime.Parse(TransactionDate.ToString()).ToString("yyyyMMddHHmmss");

                            //lgRwiter.WriteLine("DETAIL NUMBER " + RowConter.ToString());
                            //lgRwiter.WriteLine(" ITEM_CODE : " + salesTxRow["ItemCode"].ToString().Trim());
                            //lgRwiter.WriteLine(" UOM : " + salesTxRow["PackName"].ToString().Trim());
                            //lgRwiter.WriteLine(" QUANTITY : " + salesTxRow["Quantity"].ToString().Trim());
                            //lgRwiter.WriteLine(" EXPIRY_DATE : " + salesTxRow["ExpiryDate"].ToString().Trim());
                            //lgRwiter.WriteLine(" BATCH_NUMBER : " + salesTxRow["BatchNo"].ToString().Trim());



                        }
                        func.Invoke(dest);
                        InCubeQuery UpdateQuery = new InCubeQuery(db_vms, "Update [WarehouseTransaction] SET Synchronized = 1 where TransactionID = '" + TransactionID.ToString() + "'");
                        err = UpdateQuery.Execute();
                        lgRwiter.WriteLine(" TRANSACTION SENT ");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine("");
                        lgRwiter.Close();
                        lgRwiter.Dispose();
                        WriteMessage("\r\n" + TransactionID.ToString() + "  <" + DateTime.Now.ToString() + "> OK");
                        StreamWriter wrt = new StreamWriter("errorInv.log", true);
                        wrt.Write("\n" + TransactionID.ToString() + " OK\r\n");
                        wrt.Close();
                        if (err == InCubeErrors.Success)
                        {
                            string update = string.Format("INSERT INTO WH_Sync (TRANSACTIONID,StatusID,StatusDate) VALUES('{0}',1,GetDate())", TransactionID); //string.Format("update [WarehouseTransaction] set Synchronized=1 where TransactionID='{0}'", InvoiceNumberComplete);
                            UpdateQuery = new InCubeQuery(update, db_vms);
                            err = UpdateQuery.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        StreamWriter wrt = new StreamWriter("errorInv.log", true);
                        wrt.Write(ex.ToString());
                        wrt.Close();
                        WriteMessage("\r\n" + TransactionID.ToString() + " - Not Sent!");
                        lgRwiter.WriteLine(" <" + DateTime.Now.ToString() + "> SALES TRANSACTION FAILED ");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(TransactionID.ToString());
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine("");
                        lgRwiter.Close();
                        lgRwiter.Dispose();
                    }
                    err = GetSalesTransactionInformation.FindNext();
                }
            }
            catch
            {
            }
        }

        public void SendRoadNetLocations()
        {
            #region Temporarly Commented
            //            InCubeQuery qry ;
            //            string getRegions = string.Format("Select distinct RegionID from RNLocationRegion");
            //            DataTable LoopTbl = new DataTable();
            //            qry = new InCubeQuery(getRegions, db_vms);
            //            qry.Execute();
            //            LoopTbl = qry.GetDataTable();
            //            foreach (DataRow lr in LoopTbl.Rows)
            //            {

            //                string regionID = lr["RegionID"].ToString().Trim();
            //                string GetCustomerInfo = string.Format(@"select CO.CustomerCode LocationID,COL.Description LocationName,CO.GPSLongitude Longitude,co.GPSLatitude Latitude,COL.Address,co.Phone,RN.RegionID RegionID
            //,c.AccountTypeCode AccountType,a.AreaCode ZoneID, e.EmployeeCode SalesmanID,1 AvgDropSize,t.TerritoryCode RouteID,'' SrvcPatternSet,D.DIVISIONCODE
            //from CustomerOutlet CO inner join CustomerOutletLanguage COL on co.customerid=col.customerid and co.outletid=col.outletid and col.languageid=1
            //inner join Customer cust on cust.customerid=co.customerid
            //left outer join street s on co.StreetID=s.StreetID
            //left outer join Area a on s.AreaID=a.AreaID
            //left outer join AreaLanguage al on a.AreaID=al.AreaID and al.languageid=1
            //left outer join CustOutTerritory ct on co.customerid=ct.customerid and co.outletid=ct.outletid
            //left outer join EmployeeTerritory et on et.TerritoryID=ct.TerritoryID
            //left outer join Territory t on et.TerritoryID=t.TerritoryID
            //inner join RNLocationRegion RN on t.territorycode=rn.territorycode
            //inner join RNRegionDivision RD on RD.RegionID=RN.RegionID
            //inner join Division D on RD.DivisionCode=D.DivisionCode
            //left outer join RNCustomerAccountType c on co.customerid=c.customerid and co.outletid=c.outletid and D.DivisionID=c.DivisionID
            //--inner join CustOutDivOnHoldStatus COH on co.customerid=coh.customerid and co.outletid=coh.outletid and coh.divisionid=d.divisionid
            //left outer join employee e on e.employeeid=et.employeeid
            //where RN.RegionID='" + regionID + @"' 
            //and not exists(select coh.customerid,coh.outletid,coh.divisionid from CustOutDivOnHoldStatus coh
            // where coh.customerid=co.customerid and coh.outletid=co.outletid and coh.divisionid=d.divisionid)
            //group by 
            //CO.CustomerCode ,COL.Description ,CO.GPSLongitude ,co.GPSLatitude ,COL.Address,co.Phone,RN.RegionID 
            //,c.AccountTypeCode   ,a.AreaCode , e.EmployeeCode,t.TerritoryCode,D.DIVISIONCODE");
            //                qry = new InCubeQuery(GetCustomerInfo, db_vms);
            //                DataTable tbl = new DataTable();
            //                err = qry.Execute();
            //                tbl = qry.GetDataTable();
            //                if (tbl.Rows.Count > 0)
            //                {
            //                    //foreach (DataRow dr in tbl.Rows)
            //                    //{
            //                    //   // RoadNetLocationsIntergration.RemoveLocation(dr["LocationID"].ToString().Trim(), regionID);
            //                    //}
            //                    RoadNetLocationsIntergration.SaveLocations(tbl);
            //                    RoadNetLocationsIntergration.SaveLocationExtention(tbl);
            //                }
            //            }
            #endregion
        }

        #region COMPARISON INTERFACES

        public override void StockInterface()
        {
            try
            {
                WriteMessage("\r\n" + "Sending Stock");
                string invoiceLog = string.Empty;
                RfcDestination dest = RfcDestinationManager.GetDestination(SendServerName);
                IRfcFunction func;
                IRfcTable impStruct2;
                func = dest.Repository.CreateFunction("ZHIB_COMPARE_STOCK");
                string filename = "INV-Log" + DateTime.Today.Year.ToString() + DateTime.Today.Month.ToString() + DateTime.Today.Day.ToString() + ".log";
                string filePath = CoreGeneral.Common.StartupPath + "\\" + filename;
                if (!File.Exists(filePath))
                {
                    FileStream fs = File.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite);
                    fs.Close();
                    fs.Dispose();
                }
                StreamWriter lgRwiter = new StreamWriter(filename, true);
                try
                {

                    string dtlQryStr = @"select O.OrganizationCode BUKRS,O.OrganizationCode VKORG,W.WarehouseCode RCODE,I.ItemCode MATNR,BPTL.Description MEINS,IBU.PackID BasePackID,P.PackID StockPackID,
BP.Quantity BasePackQty,P.Quantity StockPackQuantity,
sum(case when P.PackID=IBU.PackID then ws.Quantity else case when P.Quantity>BP.Quantity then ws.Quantity*P.Quantity else case when P.Quantity<BP.Quantity then ws.Quantity/BP.Quantity
else case when P.Quantity=BP.Quantity then ws.Quantity else 0 end end end end )LABST,'0' CWM,W.WAREHOUSEID
,D.DivisionCode SPART,GetDate() TRDAT
from warehousestock ws inner join pack P on P.packid=ws.packid
inner join item i on i.itemid=p.itemid
inner join warehouse w on ws.warehouseid=w.warehouseid 
inner join organization o on w.organizationid=o.OrganizationID
inner join itemcategory ic on i.ItemCategoryID=ic.ItemCategoryID
inner join division d on ic.divisionid=d.divisionid
inner join ItemBaseUOM IBU on i.ItemCode=ibu.ItemCode
inner join pack BP on bp.PackID=ibu.PackID
inner join packtypelanguage BPTL on BPTL.PackTypeID=bp.PackTypeID and BPTL.LanguageID=1
where ws.quantity>0 and w.WarehouseTypeID=2 and (select top 1 uploaded from routehistory where vehicleid=w.warehouseid order by routehistoryid desc)=0
group by 
 O.OrganizationCode ,O.OrganizationCode ,W.WarehouseCode ,I.ItemCode ,BPTL.Description ,IBU.PackID ,P.PackID ,
BP.Quantity ,P.Quantity ,
W.WAREHOUSEID
,D.DivisionCode
";

                    InCubeQuery dtlQry = new InCubeQuery(db_vms, dtlQryStr);
                    err = dtlQry.Execute();

                    DataTable DetailTable = dtlQry.GetDataTable();
                    int count = DetailTable.Rows.Count;
                    int RowConter = 0;
                    ClearProgress();
                    SetProgressMax(count);

                    if (count == 0)
                    {
                        throw new Exception("No STOCK found");
                    }

                    impStruct2 = func.GetTable("ITEMTAB");
                    impStruct2.Clear();
                    lgRwiter.WriteLine(" ***************************************************************** ");
                    lgRwiter.WriteLine(" TRANSACTION DETAILS, NUMBER OF DETAILS : " + count.ToString());
                    lgRwiter.WriteLine(" ***************************************************************** ");
                    string EmployeeID = string.Empty;

                    foreach (DataRow salesTxRow in DetailTable.Rows)
                    {

                        ReportProgress("Sending Stock Interface");
                        //string UploadStatus = GetFieldValue("RouteHistory", "Top(1) Uploaded", " VehicleID=" + salesTxRow["WAREHOUSEID"].ToString().Trim() + " order by RouteHistoryID Desc", db_vms).Trim();
                        //if (UploadStatus.Equals(string.Empty)) UploadStatus = "false";
                        //if (UploadStatus.ToLower().Equals("true")) { continue; }
                        RowConter++;
                        impStruct2.Append();
                        string BUKRS = string.Empty;
                        string VKORG = string.Empty;
                        string RCODE = string.Empty;
                        string MATNR = string.Empty;
                        string MEINS = string.Empty;
                        string LABST = string.Empty;
                        string CWM = string.Empty;
                        string SPART = string.Empty;
                        string TRDAT = string.Empty;

                        BUKRS = salesTxRow["BUKRS"].ToString().Trim();
                        VKORG = salesTxRow["VKORG"].ToString().Trim();
                        RCODE = salesTxRow["RCODE"].ToString().Trim();
                        MATNR = "0000000000" + salesTxRow["MATNR"].ToString().Trim();
                        MEINS = salesTxRow["MEINS"].ToString().Trim();
                        LABST = salesTxRow["LABST"].ToString().Trim();
                        CWM = salesTxRow["CWM"].ToString().Trim();
                        SPART = salesTxRow["SPART"].ToString().Trim();
                        TRDAT = salesTxRow["TRDAT"].ToString().Trim();

                        impStruct2.SetValue("BUKRS", BUKRS.ToString());
                        impStruct2.SetValue("VKORG", VKORG.ToString());
                        impStruct2.SetValue("RCODE", RCODE.ToString());
                        impStruct2.SetValue("MATNR", MATNR.ToString());
                        impStruct2.SetValue("MEINS", MEINS.ToString());
                        impStruct2.SetValue("LABST", decimal.Round(decimal.Parse(LABST.ToString())));
                        impStruct2.SetValue("/CWM/LABST", CWM.ToString());
                        impStruct2.SetValue("SPART", SPART.ToString());
                        impStruct2.SetValue("TRDAT", DateTime.Parse(TRDAT.ToString()).ToString("yyyyMMdd"));


                        lgRwiter.WriteLine(" STOCK INTERFACE SENT ");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine("");
                    }
                    if (RowConter > 0)
                        func.Invoke(dest);
                    lgRwiter.Close();
                    lgRwiter.Dispose();

                }
                catch (Exception ex)
                {
                    StreamWriter wrt = new StreamWriter("errorInv.log", true);
                    wrt.Write(ex.Message.ToString());
                    wrt.Close();
                    WriteMessage("\r\n" + "Error Interfacing Stock " + "!");
                    lgRwiter.WriteLine(" <" + DateTime.Now.ToString() + "> STOCK INTERFACE FAILED ");
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine("");
                    lgRwiter.Close();
                    lgRwiter.Dispose();
                }


            }
            catch
            {
            }
        }

        public override void InvoiceInterface()
        {
            try
            {
                WriteMessage("\r\n" + "Sending Invoice Comparison");
                string invoiceLog = string.Empty;
                RfcDestination dest = RfcDestinationManager.GetDestination(SendServerName);
                IRfcFunction func;
                IRfcTable impStruct2;
                func = dest.Repository.CreateFunction("ZHIB_COMPARE_SALES_INVOICE");
                string filename = "INV-Log" + DateTime.Today.Year.ToString() + DateTime.Today.Month.ToString() + DateTime.Today.Day.ToString() + ".log";
                string filePath = CoreGeneral.Common.StartupPath + "\\" + filename;
                if (!File.Exists(filePath))
                {
                    FileStream fs = File.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite);
                    fs.Close();
                    fs.Dispose();
                }
                StreamWriter lgRwiter = new StreamWriter(filename, true);
                try
                {
                    string DateConfig = GetFieldValue("Configuration", "KeyValue", "KeyName='LastInvInterFaceDate'", db_vms).Trim();
                    string dtlQryStr = @"
select O.OrganizationCode BUKRS,O.OrganizationCode VKORG,'' VTWEG,D.DivisionCode SPART,C.CustomerCode KUNAG,CO.CustomerCode KUNWE,P.PayerCode KUNRG,CTL.Description CSTYP,
CASE(T.NetTotal) WHEN 0 THEN 'ZHF' ELSE 
case WHEN T.TRANSACTIONTYPEID IN (1,3) THEN 
CASE(T.SalesMode) WHEN 1 THEN 'ZHS' ELSE 
CASE(T.SalesMode) WHEN 2 THEN 'ZHE' ELSE  'XX' 
END END 
ELSE case WHEN T.TRANSACTIONTYPEID IN (2,4) THEN CASE(TD.PackStatusID) WHEN 0 THEN 'ZHR' ELSE 
CASE(TD.PackStatusID) WHEN 1 THEN 'ZVR' ELSE 
CASE(TD.PackStatusID) WHEN 2 THEN 'ZVR' ELSE 
CASE(TD.PackStatusID) WHEN 3 THEN 'ZHR' ELSE  'XX' END END END END END END end AUART
,E.EmployeeCode PERNR,Ter.TerritoryCode RCODE,T.TransactionDate TRDAT,T.TransactionID XBLNR,T.Nettotal NETWR,'QR' WAERS
,T.CustomerID,T.OutletID,T.DivisionID
from [Transaction] T inner join TransactionDetail TD on T.TransactionID=TD.TransactionID and T.CustomerID=TD.CustomerID and T.OutletID=TD.OutletID
Inner join Customer C on T.CustomerID=C.CustomerID 
inner join CustomerOutlet CO on T.CustomerID=CO.CustomerID and T.OutletID=CO.OutletID
inner join Organization O on T.OrganizationID=O.OrganizationID
inner join Division D on T.DivisionID=D.DivisionID
inner join AccountPayer AP on T.AccountID=AP.AccountID
inner join Payer P on AP.PayerID=P.PayerID
inner join Route R on T.RouteID=R.RouteID
inner join Territory Ter on R.TerritoryID=Ter.TerritoryID
inner join Employee E on T.EmployeeID=E.EmployeeID
inner join CustOutDivCustomerType CODT on CODT.CustomerID=T.CustomerID and CODT.OutletID=T.OutletID and CODT.divisionID=T.DivisionID
inner join CustomerTypeLanguage CTL on CODT.CustomerTypeID=CTL.CustomerTypeID and CTL.LanguageID=1
where T.TransactionTypeID in (1,3) and T.TransactionDate>'" + DateConfig + @"' and E.EmployeeTypeID=2
group by 
 O.OrganizationCode ,O.OrganizationCode  ,D.DivisionCode ,C.CustomerCode ,CO.CustomerCode ,P.PayerCode ,CTL.Description ,
CASE(T.NetTotal) WHEN 0 THEN 'ZHF' ELSE 
case WHEN T.TRANSACTIONTYPEID IN (1,3) THEN 
CASE(T.SalesMode) WHEN 1 THEN 'ZHS' ELSE 
CASE(T.SalesMode) WHEN 2 THEN 'ZHE' ELSE  'XX' 
END END 
ELSE case WHEN T.TRANSACTIONTYPEID IN (2,4) THEN CASE(TD.PackStatusID) WHEN 0 THEN 'ZHR' ELSE 
CASE(TD.PackStatusID) WHEN 1 THEN 'ZVR' ELSE 
CASE(TD.PackStatusID) WHEN 2 THEN 'ZVR' ELSE 
CASE(TD.PackStatusID) WHEN 3 THEN 'ZHR' ELSE  'XX' END END END END END END end 
,E.EmployeeCode ,Ter.TerritoryCode ,T.TransactionDate ,T.TransactionID ,T.Nettotal ,T.CustomerID,T.OutletID,T.DivisionID
";

                    InCubeQuery dtlQry = new InCubeQuery(db_vms, dtlQryStr);
                    err = dtlQry.Execute();

                    DataTable DetailTable = dtlQry.GetDataTable();
                    int count = DetailTable.Rows.Count;
                    int RowConter = 0;
                    ClearProgress();
                    SetProgressMax(count);

                    if (count == 0)
                    {
                        throw new Exception("No Invoice found");
                    }

                    impStruct2 = func.GetTable("ITEMTAB");
                    impStruct2.Clear();
                    lgRwiter.WriteLine(" ***************************************************************** ");
                    lgRwiter.WriteLine(" Invoice DETAILS, NUMBER OF DETAILS : " + count.ToString());
                    lgRwiter.WriteLine(" ***************************************************************** ");
                    string EmployeeID = string.Empty;

                    foreach (DataRow salesTxRow in DetailTable.Rows)
                    {

                        ReportProgress("Sending Invoice Comparison Interface");
                        //string UploadStatus = GetFieldValue("RouteHistory", "Top(1) Uploaded", " VehicleID=" + salesTxRow["WAREHOUSEID"].ToString().Trim() + " order by RouteHistoryID Desc", db_vms).Trim();
                        //if (UploadStatus.Equals(string.Empty)) UploadStatus = "false";
                        //if (UploadStatus.ToLower().Equals("true")) { continue; }
                        RowConter++;
                        impStruct2.Append();
                        string BUKRS = string.Empty;
                        string VKORG = string.Empty;
                        string VTWEG = string.Empty;
                        string SPART = string.Empty;
                        string KUNAG = string.Empty;
                        string KUNWE = string.Empty;
                        string KUNRG = string.Empty;
                        string CSTYP = string.Empty;
                        string AUART = string.Empty;
                        string PERNR = string.Empty;
                        string RCODE = string.Empty;
                        string TRDAT = string.Empty;
                        string XBLNR = string.Empty;
                        string NETWR = string.Empty;
                        string WAERS = string.Empty;

                        BUKRS = salesTxRow["BUKRS"].ToString().Trim();
                        VKORG = salesTxRow["VKORG"].ToString().Trim();
                        //VTWEG = salesTxRow["VTWEG"].ToString().Trim();
                        SPART = salesTxRow["SPART"].ToString().Trim();
                        KUNAG = salesTxRow["KUNAG"].ToString().Trim();
                        KUNWE = salesTxRow["KUNWE"].ToString().Trim();
                        KUNRG = salesTxRow["KUNRG"].ToString().Trim();
                        CSTYP = salesTxRow["CSTYP"].ToString().Trim();
                        AUART = salesTxRow["AUART"].ToString().Trim();
                        PERNR = salesTxRow["PERNR"].ToString().Trim();
                        RCODE = salesTxRow["RCODE"].ToString().Trim();
                        TRDAT = salesTxRow["TRDAT"].ToString().Trim();
                        XBLNR = salesTxRow["XBLNR"].ToString().Trim();
                        NETWR = salesTxRow["NETWR"].ToString().Trim();
                        WAERS = salesTxRow["WAERS"].ToString().Trim();
                        VTWEG = GetFieldValue("Channel", "ChannelCode", "ChannelID in (select channelid from qnie_custdistdivdelete where customerid=" + salesTxRow["Customerid"].ToString().Trim() + " and OutletID=" + salesTxRow["OutletID"].ToString().Trim() + " and DivisionID=" + salesTxRow["DivisionID"].ToString().Trim() + ")", db_vms).Trim();

                        impStruct2.SetValue("BUKRS", BUKRS.ToString());
                        impStruct2.SetValue("VKORG", VKORG.ToString());
                        impStruct2.SetValue("VTWEG", VTWEG.ToString());
                        impStruct2.SetValue("SPART", SPART.ToString());
                        impStruct2.SetValue("KUNAG", KUNAG.ToString());
                        impStruct2.SetValue("KUNWE", KUNWE.ToString());
                        impStruct2.SetValue("KUNRG", KUNRG.ToString());
                        impStruct2.SetValue("CSTYP", CSTYP.ToString());
                        impStruct2.SetValue("AUART", AUART.ToString());
                        impStruct2.SetValue("PERNR", PERNR.ToString());
                        impStruct2.SetValue("RCODE", RCODE.ToString());
                        impStruct2.SetValue("TRDAT", DateTime.Parse(TRDAT.ToString()).ToString("yyyyMMdd"));
                        impStruct2.SetValue("XBLNR", XBLNR.ToString());
                        impStruct2.SetValue("NETWR", decimal.Round(decimal.Parse(NETWR.ToString())));
                        impStruct2.SetValue("WAERS", WAERS.ToString());

                        lgRwiter.WriteLine(" STOCK INTERFACE SENT ");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine(" *******************************************************");
                        lgRwiter.WriteLine("");
                    }
                    if (RowConter > 0)
                    {
                        string NowDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        func.Invoke(dest);
                        InCubeQuery UpdateQuery = new InCubeQuery(db_vms, "Update [Configuration] SET KeyValue = '" + NowDate + "' where Keyname = 'LastInvInterFaceDate'");
                        err = UpdateQuery.Execute();
                    }
                    lgRwiter.Close();
                    lgRwiter.Dispose();

                }
                catch (Exception ex)
                {
                    StreamWriter wrt = new StreamWriter("errorInv.log", true);
                    wrt.Write(ex.Message.ToString());
                    wrt.Close();
                    WriteMessage("\r\n" + "Error Interfacing Invoices " + "!");
                    lgRwiter.WriteLine(" <" + DateTime.Now.ToString() + "> INVOICE INTERFACE FAILED ");
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine(" *******************************************************");
                    lgRwiter.WriteLine("");
                    lgRwiter.Close();
                    lgRwiter.Dispose();
                }


            }
            catch
            {
            }
        }

        private decimal BaseUOMQuantity(string ItemID, string PackID, decimal Quantity, decimal PackQty)
        {
            decimal returnQuantity = 0;
            try
            {
                string getBasePackID = GetFieldValue("ItemBaseUOM", "PackID", "ItemID=" + ItemID + "", db_vms).Trim();
                if (getBasePackID.Equals(PackID))
                {
                    return Quantity;
                }
                else
                {
                    decimal BaseUOMQty = decimal.Parse(GetFieldValue("Pack", "Quantity", "PackID=" + getBasePackID + "", db_vms).Trim());
                    if (BaseUOMQty > PackQty)
                    {
                        return Quantity / BaseUOMQty;
                    }
                    else if (BaseUOMQty < PackQty)
                    {
                        return Quantity * PackQty;
                    }
                    else if (BaseUOMQty == PackQty)
                    {
                        return Quantity;
                    }
                }
            }
            catch
            {

            }
            return returnQuantity;
        }
        #endregion

        public override void UpdateCustomer()
        {
            try
            {
                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;
                DataTable DT = new DataTable();
                DT = GetCustomerTable();
                DT.DefaultView.Sort = "DeleteStatus DESC";
                DT = DT.DefaultView.ToTable();
                //HERE YOU WILL WRITE THE INVALID CUSTOMERS ON AN EXTERNAL TEXT FILE "InvalidEntry.txt"
                //WriteExceptions(Exceptions("CUSTOMERS"), "Customer Exceptions");
                ClearProgress();
                SetProgressMax(DT.Rows.Count);
                //DataTable test = new DataTable();
                //test.Rows.Add(DT.Select("", "CreationDate Asc"));
                //string ResetAccount = "Update Account set Creditlimit = 0 Where AccountTypeID = 1";
                //QueryBuilderObject.RunQuery(ResetAccount, db_vms);
                //for (int i = 0; i < DT.Rows.Count; i++)            Select("", "CreationDate DESC")
                //{
                foreach (DataRow dr in DT.Rows)
                {
                    ReportProgress("Updating Customers");
                    string BillToCode = dr["BillToCode"].ToString();
                    string BillToName = dr["BillToName"].ToString();
                    string ShipToCode = dr["ShipToCode"].ToString();
                    string ShipToName = dr["ShipToName"].ToString();
                    string PayerCode = dr["PayerCode"].ToString();
                    string PayerName = dr["PayerName"].ToString();
                    if (PayerCode.Trim().Equals(string.Empty))
                        PayerCode = BillToCode;
                    if (PayerName.Trim().Equals(string.Empty))
                        PayerName = BillToName;
                    string CustomerType = dr["CustomerType"].ToString();
                    string CustomerPriceGroup = dr["CustomerPriceGroup"].ToString();
                    string CustomerPriceGroupName = dr["CustomerPriceGroupName"].ToString();
                    string DeleteStatus = dr["DeleteStatus"].ToString();
                    string BlockStatus = dr["BlockStatus"].ToString();
                    string DivisionCode = dr["DivisionCode"].ToString();
                    string DivisionName = dr["DivisionName"].ToString();
                    string Address = dr["Address"].ToString();
                    string ContactName = dr["ContactName"].ToString();
                    string Telephone = dr["Telephone"].ToString();
                    string Mobile = dr["Mobile"].ToString();
                    string CustomerCategoryCode = dr["CustomerCategoryCode"].ToString();
                    string CategoryName = dr["CategoryName"].ToString();
                    string PaymentTermDays = dr["PaymentTermDays"].ToString();
                    string PaymentTermType = dr["PaymentTermType"].ToString();
                    string CreditLimit = dr["CreditLimit"].ToString();
                    string Zone = dr["Zone"].ToString();
                    string Region = dr["Region"].ToString();
                    string ZoneCode = dr["ZoneCode"].ToString();
                    string ChannelCode = dr["ChannelCode"].ToString();
                    string ChannelName = dr["ChannelName"].ToString();
                    string CustomerClass = dr["CustomerClass"].ToString();
                    string RouteCode = dr["RouteCode"].ToString();
                    string RouteName = dr["RouteName"].ToString();
                    string FLAG = dr["FLAG"].ToString();
                    string SalesmanCode = dr["SalesmanCode"].ToString();
                    string SalesOrganizationCode = dr["SalesOrganizationCode"].ToString();
                    string CompanyCode = dr["CompanyCode"].ToString();
                    string GPS_Long = dr["GPS_Long"].ToString();
                    string GPS_Lat = dr["GPS_Lat"].ToString();
                    string CHANNEL_ID = "";
                    string InactiveOnChannel = DeleteStatus;

                    string Inactive = "1";
                    DataRow[] drCustRows = DT.Select("ShipToCode = '" + ShipToCode + "'");
                    foreach (DataRow drCustRow in drCustRows)
                    {
                        if (drCustRow["DeleteStatus"].ToString().Trim().Equals("0"))
                        {
                            Inactive = "0";
                            break;
                        }
                    }

                    if (DeleteStatus.Equals("1"))
                    {
                        //string RNRegion = GetFieldValue("RNLocationRegion", "RegionID", "TerritoryCode='" + RouteCode + "'", db_vms).Trim();
                        //if (!RNRegion.Equals(string.Empty))
                        //    RoadNetLocationsIntergration.RemoveLocation(ShipToCode, RNRegion);//REGION SHOULD BE CHANGED TO REGIONCODE
                    }

                    WriteExceptions("CUSTOMER CODE=" + ShipToCode + "  CREDIT LIMIT IS " + CreditLimit + "", "SAP Customer", true);
                    string OrganizationCode = SalesOrganizationCode;
                    string OrganizationID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + OrganizationCode + "'", db_vms).Trim();
                    if (OrganizationID.Equals(string.Empty)) continue;
                    string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode = '" + DivisionCode + "' AND ORGANIZATIONID=" + OrganizationID + "", db_vms);
                    if (DivisionID.Equals(string.Empty))
                    {
                        DivisionID = GetFieldValue("Division", "isnull(MAX(DivisionID),0) + 1", db_vms);
                        QueryBuilderObject.SetField("DivisionID", DivisionID);
                        QueryBuilderObject.SetField("DivisionCode", "'" + DivisionCode + "'");
                        QueryBuilderObject.SetField("OrganizationID", OrganizationID);// THIS USED TO BE 1 THEN I ADDED THE MiscOrgID FOR THE NEW RELEASE INTEGRATION.
                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        QueryBuilderObject.InsertQueryString("Division", db_vms);
                        string DivisionString = DivisionName + "/" + OrganizationCode;
                        QueryBuilderObject.SetField("DivisionID", DivisionID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + DivisionString + "'");
                        err = QueryBuilderObject.InsertQueryString("DivisionLanguage", db_vms);
                    }

                    #region Get Customer Group

                    //INSERTING CHANNEL
                    CHANNEL_ID = GetFieldValue("Channel", "ChannelID", "ChannelCode='" + ChannelCode + "'", db_vms).Trim();
                    if (CHANNEL_ID.Equals(string.Empty))
                    {
                        CHANNEL_ID = GetFieldValue("Channel", "isnull(MAX(ChannelID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("ChannelID", CHANNEL_ID);
                        QueryBuilderObject.SetField("ChannelCode", "'" + ChannelCode + "'");
                        err = QueryBuilderObject.InsertQueryString("Channel", db_vms);

                        QueryBuilderObject.SetField("ChannelID", CHANNEL_ID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + ChannelName + "'");
                        err = QueryBuilderObject.InsertQueryString("ChannelLanguage", db_vms);

                    }

                    string SUBCHANNEL_ID = GetFieldValue("SubChannel", "SubChannelID", "SubChannelCode='" + CustomerCategoryCode + "'", db_vms).Trim();
                    if (SUBCHANNEL_ID.Equals(string.Empty))
                    {
                        SUBCHANNEL_ID = GetFieldValue("SubChannel", "isnull(MAX(SubChannelID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("SubChannelID", SUBCHANNEL_ID);
                        QueryBuilderObject.SetField("SubChannelCode", "'" + CustomerCategoryCode + "'");
                        QueryBuilderObject.SetField("ChannelID", CHANNEL_ID);
                        err = QueryBuilderObject.InsertQueryString("SubChannel", db_vms);

                        QueryBuilderObject.SetField("SubChannelID", SUBCHANNEL_ID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + CategoryName + "'");
                        err = QueryBuilderObject.InsertQueryString("SubChannelLanguage", db_vms);

                    }

                    string GroupID = GetFieldValue("CustomerGroup", "GroupID", "GroupCode='" + CustomerPriceGroup + "'", db_vms);
                    if (GroupID.Equals(string.Empty))
                    {
                        GroupID = GetFieldValue("CustomerGroup", "isnull(MAX(GroupID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                        QueryBuilderObject.SetField("GroupCode", "'" + CustomerPriceGroup + "'");
                        QueryBuilderObject.SetField("ChannelID", CHANNEL_ID);
                        QueryBuilderObject.SetField("SubChannelID", SUBCHANNEL_ID);
                        err = QueryBuilderObject.InsertQueryString("CustomerGroup", db_vms);

                        QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + CustomerPriceGroupName + "'");
                        err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);
                    }
                    else
                    {
                        QueryBuilderObject.SetField("SubChannelID", SUBCHANNEL_ID);
                        QueryBuilderObject.SetField("ChannelID", CHANNEL_ID);
                        err = QueryBuilderObject.UpdateQueryString("CustomerGroup", "GROUPID=" + GroupID + "", db_vms);
                    }
                    //continue;
                    #endregion Get Customer Group

                    #region City & Channel

                    #region City
                    string CityID = "";

                    err = ExistObject("CityLanguage", "CityID", "LanguageID = 1 AND Description = '" + Region + "'", db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        CityID = GetFieldValue("City", "isnull(MAX(CityID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("CountryID", "1");
                        QueryBuilderObject.SetField("StateID", "1");
                        QueryBuilderObject.SetField("CityID", CityID);
                        err = QueryBuilderObject.InsertQueryString("City", db_vms);

                        QueryBuilderObject.SetField("CountryID", "1");
                        QueryBuilderObject.SetField("StateID", "1");
                        QueryBuilderObject.SetField("CityID", CityID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + Region + "'");
                        err = QueryBuilderObject.InsertQueryString("CityLanguage", db_vms);

                        QueryBuilderObject.SetField("CountryID", "1");
                        QueryBuilderObject.SetField("StateID", "1");
                        QueryBuilderObject.SetField("CityID", CityID);
                        QueryBuilderObject.SetField("AreaID", CityID);
                        QueryBuilderObject.SetField("AreaCode", "'" + ZoneCode + "'");
                        err = QueryBuilderObject.InsertQueryString("Area", db_vms);

                        QueryBuilderObject.SetField("CountryID", "1");
                        QueryBuilderObject.SetField("StateID", "1");
                        QueryBuilderObject.SetField("CityID", CityID);
                        QueryBuilderObject.SetField("AreaID", CityID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + Region + "'");
                        err = QueryBuilderObject.InsertQueryString("AreaLanguage", db_vms);

                        QueryBuilderObject.SetField("CountryID", "1");
                        QueryBuilderObject.SetField("StateID", "1");
                        QueryBuilderObject.SetField("CityID", CityID);
                        QueryBuilderObject.SetField("AreaID", CityID);
                        QueryBuilderObject.SetField("StreetID", CityID);
                        err = QueryBuilderObject.InsertQueryString("Street", db_vms);

                        QueryBuilderObject.SetField("CountryID", "1");
                        QueryBuilderObject.SetField("StateID", "1");
                        QueryBuilderObject.SetField("CityID", CityID);
                        QueryBuilderObject.SetField("AreaID", CityID);
                        QueryBuilderObject.SetField("StreetID", CityID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + Region + "'");
                        err = QueryBuilderObject.InsertQueryString("StreetLanguage", db_vms);
                    }
                    else
                    {
                        CityID = GetFieldValue("CityLanguage", "CityID", "LanguageID = 1 AND Description = '" + Region + "'", db_vms);
                        QueryBuilderObject.SetField("AreaCode", "'" + ZoneCode + "'");
                        err = QueryBuilderObject.UpdateQueryString("Area", "AreaID=" + CityID + " and CityID=" + CityID + "", db_vms);
                    }
                    #endregion City

                    #region Channel

                    string ClassificationID = "";
                    err = ExistObject("CustomerClassLanguage", "CustomerClassID", "LanguageID = 1 AND Description = '" + CustomerClass + "'", db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        ClassificationID = GetFieldValue("CustomerClass", "isnull(MAX(CustomerClassID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("CustomerClassID", ClassificationID);
                        QueryBuilderObject.SetField("Code", "'" + ClassificationID + "'");
                        QueryBuilderObject.InsertQueryString("CustomerClass", db_vms);

                        QueryBuilderObject.SetField("CustomerClassID", ClassificationID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + CustomerClass + "'");
                        err = QueryBuilderObject.InsertQueryString("CustomerClassLanguage", db_vms);
                    }
                    else
                    {
                        ClassificationID = GetFieldValue("CustomerClassLanguage", "CustomerClassID", "LanguageID = 1 AND Description = '" + CustomerClass + "'", db_vms);
                    }

                    #endregion Channel

                    #endregion City & Channel

                    #region PaymentTerm

                    #region TEMP

                    int Temp;
                    if (!int.TryParse(PaymentTermDays, out Temp))
                    {
                        PaymentTermDays = "0";
                    }

                    if (CreditLimit.Equals(string.Empty))
                    {
                        CreditLimit = "0";
                    }

                    #endregion TEMP

                    //err = ExistObject("PaymentTerm", "PaymentTermID", "SimplePeriodWidth = " + PaymentTermDays, db_vms);
                    //if (err != InCubeErrors.Success)
                    //{
                    //    PaymentTermID = GetFieldValue("PaymentTerm", "isnull(MAX(PaymentTermID),0) + 1", db_vms);

                    //    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                    //    QueryBuilderObject.SetField("PaymentTermTypeID", "1");
                    //    QueryBuilderObject.SetField("SimplePeriodWidth", PaymentTermDays);
                    //    QueryBuilderObject.SetField("SimplePeriodID", "1"); //Days
                    //    err = QueryBuilderObject.InsertQueryString("PaymentTerm", db_vms);

                    //    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                    //    QueryBuilderObject.SetField("LanguageID", "1");
                    //    QueryBuilderObject.SetField("Description", "'Every " + PaymentTermDays + " Days'");
                    //    QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms);
                    //}
                    //else if (err == InCubeErrors.Success)
                    //{
                    //    PaymentTermID = GetFieldValue("PaymentTerm", "PaymentTermID", "SimplePeriodWidth = " + PaymentTermDays, db_vms);
                    //}

                    if (CustomerType.Equals(string.Empty))
                    {
                        continue;
                    }
                    else if (CustomerType.Equals("0"))
                    {
                        CustomerType = "1";
                    }
                    else if (CustomerType.Equals("1"))
                    {
                        CustomerType = "2";
                    }

                    #endregion PaymentTerm

                    string CustomerID = "";

                    #region Customer
                    CustomerID = GetFieldValue("Customer", "CustomerID", "CustomerCode = '" + BillToCode + "' and customerid in (Select customerid from CustomerOrganization where Customercode='" + BillToCode + "' and OrganizationID=" + OrganizationID + ")", db_vms);
                    if (CustomerID == string.Empty)
                    {
                        CustomerID = GetFieldValue("Customer", "isnull(MAX(CustomerID),0) + 1", db_vms);
                    }
                    string ExistCustomer = GetFieldValue("Customer", "CustomerID", "CustomerID = " + CustomerID, db_vms);
                    if (ExistCustomer != string.Empty) // Exist Customer --- Update Query
                    {
                        TOTALUPDATED++;

                        QueryBuilderObject.SetField("CustomerCode", "'" + BillToCode + "'");
                        QueryBuilderObject.SetField("Phone", "'" + Telephone + "'");
                        QueryBuilderObject.SetField("Fax", "'" + Mobile + "'");
                        QueryBuilderObject.SetField("OnHold", "0"); //QueryBuilderObject.SetField("OnHold", BlockStatus);
                        QueryBuilderObject.SetField("InActive", "0");
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        err = QueryBuilderObject.UpdateQueryString("Customer", " CustomerID = " + CustomerID, db_vms);

                    }
                    else // New Customer --- Insert Query
                    {
                        TOTALINSERTED++;

                        QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                        QueryBuilderObject.SetField("Phone", "'" + Telephone + "'");
                        QueryBuilderObject.SetField("Fax", "'" + Mobile + "'");
                        QueryBuilderObject.SetField("Email", "' '");
                        QueryBuilderObject.SetField("CustomerCode", "'" + BillToCode + "'");
                        QueryBuilderObject.SetField("OnHold", "0"); //QueryBuilderObject.SetField("OnHold", BlockStatus);
                        QueryBuilderObject.SetField("StreetID", "0");
                        QueryBuilderObject.SetField("StreetAddress", "0");
                        QueryBuilderObject.SetField("InActive", "0");
                        QueryBuilderObject.SetField("New", "0");

                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        err = QueryBuilderObject.InsertQueryString("Customer", db_vms);

                        QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                        QueryBuilderObject.SetField("OrganizationID", OrganizationID.ToString());
                        QueryBuilderObject.SetField("CustomerCode", "'" + BillToCode + "'");
                        err = QueryBuilderObject.InsertQueryString("CustomerOrganization", db_vms);

                    }

                    #endregion

                    #region CustomerLanguage
                    ExistCustomer = GetFieldValue("CustomerLanguage", "CustomerID", "CustomerID = " + CustomerID + " AND LanguageID = 1", db_vms);
                    if (ExistCustomer != string.Empty) // Exist CustomerLanguage --- Update Query
                    {
                        QueryBuilderObject.SetField("Description", "'" + BillToName + "'");
                        QueryBuilderObject.SetField("Address", "'" + Address + "'");
                        err = QueryBuilderObject.UpdateQueryString("CustomerLanguage", "  CustomerID = " + CustomerID + " AND LanguageID = 1", db_vms);
                    }
                    else  // New CustomerLanguage --- Insert Query
                    {
                        QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + BillToName + "'");
                        QueryBuilderObject.SetField("Address", "'" + Address + "'");
                        err = QueryBuilderObject.InsertQueryString("CustomerLanguage", db_vms);
                    }

                    ExistCustomer = GetFieldValue("CustomerLanguage", "CustomerID", "CustomerID = " + CustomerID + " AND LanguageID = 2", db_vms); // ARABIC
                    if (ExistCustomer != string.Empty) // Exist CustomerLanguage --- Update Query
                    {
                        QueryBuilderObject.SetField("Description", "N'" + BillToName + "'");
                        QueryBuilderObject.SetField("Address", "N'" + Address + "'");
                        QueryBuilderObject.UpdateQueryString("CustomerLanguage", "  CustomerID = " + CustomerID + " AND LanguageID = 2", db_vms);
                    }
                    else  // New CustomerLanguage --- Insert Query
                    {
                        QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "N'" + BillToName + "'");
                        QueryBuilderObject.SetField("Address", "N'" + Address + "'");
                        err = QueryBuilderObject.InsertQueryString("CustomerLanguage", db_vms);
                    }

                    #endregion

                    #region Customer Account

                    int AccountID = 1;
                    string Balance = "0";
                    ExistCustomer = GetFieldValue("AccountCust", "AccountID", "CustomerID = " + CustomerID + " and AccountID in (select AccountID from Account where OrganizationID=" + OrganizationID + ") ", db_vms);
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
                        QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                        QueryBuilderObject.SetField("CurrencyID", "1");
                        err = QueryBuilderObject.InsertQueryString("Account", db_vms);

                        QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                        QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                        err = QueryBuilderObject.InsertQueryString("AccountCust", db_vms);

                        QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + BillToName.Trim() + " Account'");
                        err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);

                        QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "N'" + BillToName.Trim() + " Account'");
                        err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
                    }
                    #endregion

                    //string PayerID = "";
                    //string isPayerExistForThisCustomerOnThisDivision = GetFieldValue("Payer", "PayerID", "CustomerID=" + CustomerID + " and PayerCode='"+PayerCode+"'", db_vms).Trim();
                    //if (isPayerExistForThisCustomerOnThisDivision.Equals(string.Empty))
                    //{
                    //        PayerID = GetFieldValue("Payer", "isnull(MAX(PayerID),0) + 1", db_vms);
                    //        QueryBuilderObject.SetField("PayerID", PayerID);
                    //        QueryBuilderObject.SetField("PayerCode", "'" + PayerCode + "'");
                    //        QueryBuilderObject.SetField("CustomerID", CustomerID);
                    //        err = QueryBuilderObject.InsertQueryString("Payer", db_vms);
                    //}
                    //else
                    //{
                    //    PayerID = isPayerExistForThisCustomerOnThisDivision;
                    //}
                    //string isThereAnotherPayerForThisCustomerWithSameDivision = GetFieldValue("Payer P inner join PayerDivision PD on P.PayerID=PD.PayerID", "P.PayerID", "P.PayerCode<>'"+PayerCode+"' and P.CustomerID="+CustomerID+" and PD.DivisionID=" + DivisionID + "", db_vms).Trim();
                    //if (!isThereAnotherPayerForThisCustomerWithSameDivision.Equals(string.Empty))
                    //{
                    //    string DeleteCustomerPayer = string.Format("delete from payerDivision where PayerID=" + isThereAnotherPayerForThisCustomerWithSameDivision + " and DivisionID=" + DivisionID + "");
                    //    InCubeQuery qry = new InCubeQuery(DeleteCustomerPayer, db_vms);
                    //    err = qry.ExecuteNonQuery();
                    //    string InsertCustPayerHist = string.Format("insert into QNIE_CustomerPayerHistory Values({0},{1},{2},{3},GetDate())", CustomerID, DivisionID, isThereAnotherPayerForThisCustomerWithSameDivision, PayerID);
                    //    qry = new InCubeQuery(InsertCustPayerHist, db_vms);
                    //    err = qry.ExecuteNonQuery();
                    //}
                    //string payerDivision = GetFieldValue("PayerDivision", "PayerID", "PayerID = " + PayerID + " and DivisionID=" + DivisionID + "", db_vms).Trim();
                    //if (payerDivision.Equals(string.Empty))
                    //{
                    //    QueryBuilderObject.SetField("PayerID", PayerID);
                    //    QueryBuilderObject.SetField("DivisionID", DivisionID);
                    //    err = QueryBuilderObject.InsertQueryString("PayerDivision", db_vms);
                    //}
                    //int PayerAccountID = 1;
                    //string PayerBalance = "0";
                    //ExistCustomer = GetFieldValue("AccountPayer", "AccountID", "PayerID = " + PayerID, db_vms);
                    //if (ExistCustomer != string.Empty)
                    //{
                    //    PayerAccountID = int.Parse(GetFieldValue("AccountPayer", "AccountID", "PayerID = " + PayerID, db_vms));
                    //    QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                    //    //QueryBuilderObject.SetField("Balance", Balance);
                    //    QueryBuilderObject.SetField("ParentAccountID", AccountID.ToString());
                    //    err = QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + PayerAccountID.ToString(), db_vms);
                    //}
                    //else
                    //{
                    //    PayerAccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));
                    //    QueryBuilderObject.SetField("AccountID", PayerAccountID.ToString());
                    //    QueryBuilderObject.SetField("AccountTypeID", "1");
                    //    QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                    //    QueryBuilderObject.SetField("Balance", Balance);
                    //    QueryBuilderObject.SetField("GL", "0");
                    //    QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                    //    QueryBuilderObject.SetField("CurrencyID", "1");
                    //    QueryBuilderObject.SetField("ParentAccountID",AccountID.ToString());
                    //    err = QueryBuilderObject.InsertQueryString("Account", db_vms);

                    //    QueryBuilderObject.SetField("PayerID", PayerID);
                    //    QueryBuilderObject.SetField("AccountID", PayerAccountID.ToString());
                    //    err = QueryBuilderObject.InsertQueryString("AccountPayer", db_vms);

                    //    QueryBuilderObject.SetField("AccountID", PayerAccountID.ToString());
                    //    QueryBuilderObject.SetField("LanguageID", "1");
                    //    QueryBuilderObject.SetField("Description", "'" + BillToName.Trim() + " PayerAccount'");
                    //    err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);

                    //    QueryBuilderObject.SetField("AccountID", PayerAccountID.ToString());
                    //    QueryBuilderObject.SetField("LanguageID", "2");
                    //    QueryBuilderObject.SetField("Description", "N'" + BillToName.Trim() + " PayerAccount'");
                    //    err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
                    //}

                    CreateCustomerOutlet(CustomerCategoryCode, CategoryName, CustomerID, AccountID.ToString(), "", GroupID, "", "0", CustomerType, ShipToCode, ChannelName, PaymentTermDays, ShipToName, Address, ShipToName, Address, Telephone, Mobile, BlockStatus, "0", BillToCode, CreditLimit, Balance, GPS_Long, GPS_Lat, ShipToCode, "", DivisionID, Inactive, OrganizationID, CityID, RouteCode, RouteName, SalesmanCode, ClassificationID, PayerCode, DivisionCode, CHANNEL_ID, InactiveOnChannel);
                }

                //THE FOLLOWING UPDATE IS TO DISTRIBUTE THE CREDIT LIMIT OVER ACCOUNTCUSTOUTDIVEMP
                string ResetAccount = @"
update a set a.creditlimit= (ac.creditlimit*(dcl.creditlimitpercentage/100))
from AccountCustOutDivEmp acod
inner join account a on a.accountid=acod.accountid
inner join account ac on a.ParentAccountID=ac.AccountID
inner join [DivisionsCreditLimit] dcl on acod.CustomerID=dcl.CustomerID and acod.OutletID=dcl.OutletID
and acod.DivisionID=dcl.DivisionID and acod.EmployeeID=dcl.EmployeeID and dcl.employeeid<>-1

";
                InCubeQuery CL_Query = new InCubeQuery(ResetAccount, db_vms);
                err = CL_Query.ExecuteNonQuery();
                DT.Dispose();
                WriteMessage("\r\n");
                WriteMessage("<<< CUSTOMERS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);

                string UpdateCustomerTypes = string.Format(@"update co set co.customertypeID=2 
from customeroutlet co inner join CustOutDivCustomerType ct on co.CustomerID=ct.customerid and co.outletid=ct.outletid
where ct.CustomerTypeID=2 and co.CustomerTypeID=1
");
                CL_Query = new InCubeQuery(UpdateCustomerTypes, db_vms);
                err = CL_Query.ExecuteNonQuery();

                string DeleteFromRoutes = string.Format(@"DELETE RC FROM RouteCustomer RC INNER JOIN Route R ON R.RouteID = RC.RouteID LEFT JOIN CustOutTerritory COT ON COT.CustomerID = RC.CustomerID AND COT.OutletID = RC.OutletID 
AND COT.TerritoryID = R.TerritoryID WHERE COT.TerritoryID IS NULL");
                CL_Query = new InCubeQuery(DeleteFromRoutes, db_vms);
                err = CL_Query.ExecuteNonQuery();
                //                string EmpTerr = string.Format(@"delete from employeeterritory where territoryid in (select territoryid from territory where territorycode in 
                //(select employeecode from employee))
                //");
                //                CL_Query = new InCubeQuery(EmpTerr, db_vms);
                //                err = CL_Query.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                WriteExceptions("EXCEPTION HAPPENED IN THE UpdateCustomer() FUNCTION, THE MESSAGE IS <<" + ex.Message + ">>", "UPDATE CUSTOMERS", false);
            }
        }
        private void CreateCustomerOutlet(string CategoryCode, string CategoryName, string customerID, string parentAccount, string supervisorID, string GroupID, string GroupID2, string B2B_Inv, string CustType, string CustomerCode, string channelDescription, string Paymentterms, string CustomerDescriptionEnglish, string CustomerAddressEnglish, string CustomerDescriptionArabic, string CustomerAddressArabic, string Phonenumber, string Faxnumber, string OnHold, string Taxable, string HeadOfficeCode, string CreditLimit, string Balance, string Longitude, string latitude, string CustomerBarCode, string email, string divisionID, string inactive, string organizationID, string streetID, string RouteCode, string RouteName, string SalesmanCode, string ClassificationID, string PayerCode, string DivisionCode, string ChannelID, string inactiveOnChannel)
        {
            int CustomerID;
            InCubeErrors err;

            decimal outcome = 0;
            if (Longitude.Trim() == string.Empty || !decimal.TryParse(Longitude.Trim(), out outcome) || outcome == 0)
                Longitude = "0";

            outcome = 0;
            if (latitude.Trim() == string.Empty || !decimal.TryParse(latitude.Trim(), out outcome) || outcome == 0)
                latitude = "0";

            if (latitude == string.Empty)
                latitude = "0";

            string ExistCustomer = "";

            CustomerID = int.Parse(customerID);// int.Parse(GetFieldValue("Customer", "CustomerID", "CustomerCode='" + HeadOfficeCode + "'", db_vms));

            string SalesmanDivision = GetFieldValue("EmployeeDivision", "DivisionID", " DivisionID=" + divisionID + " and EmployeeID in (select employeeid from employee where employeecode='" + SalesmanCode + "')", db_vms).Trim();
            if (SalesmanDivision.Equals(string.Empty))
            {
                string EmpID = GetFieldValue("Employee", "EmployeeID", "employeecode='" + SalesmanCode + "'", db_vms).Trim();
                QueryBuilderObject.SetField("EmployeeID", EmpID);
                QueryBuilderObject.SetField("DivisionID", divisionID); //Days
                err = QueryBuilderObject.InsertQueryString("EmployeeDivision", db_vms);
            }

            string PaymentTermID = "1";

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

            #region Customer Outlet and language

            string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerID = " + CustomerID + " AND CustomerCode = '" + CustomerCode + "' and OrganizationID=" + organizationID + "", db_vms);
            if (!OutletID.Trim().Equals(string.Empty))
            {
                if (latitude != "0")
                    QueryBuilderObject.SetField("GPSLatitude", latitude);
                if (Longitude != "0")
                    QueryBuilderObject.SetField("GPSLongitude", Longitude);
                QueryBuilderObject.SetField("Phone", "'" + Phonenumber + "'");
                QueryBuilderObject.SetField("Fax", "'" + Faxnumber + "'");
                QueryBuilderObject.SetField("Email", "'" + email + "'");
                QueryBuilderObject.SetField("CustomerTypeID", CustType); //HardCoded -1- Cash -2- Credit
                QueryBuilderObject.SetField("OnHold", OnHold);
                QueryBuilderObject.SetField("Inactive", inactive);
                QueryBuilderObject.SetField("BillsOpenNumber", B2B_Inv);
                QueryBuilderObject.SetField("Barcode", "'" + CustomerBarCode + "'");
                QueryBuilderObject.SetField("PaymentTermID", "0");
                QueryBuilderObject.SetField("CustomerClassID", ClassificationID);
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
                QueryBuilderObject.SetField("GPSLatitude", latitude);
                QueryBuilderObject.SetField("GPSLongitude", Longitude);
                QueryBuilderObject.SetField("StreetAddress", "0");
                QueryBuilderObject.SetField("Inactive", inactive);
                QueryBuilderObject.SetField("Notes", "0");
                QueryBuilderObject.SetField("SkipCreditCheck", "0");
                QueryBuilderObject.SetField("PaymentTermID", "0");
                QueryBuilderObject.SetField("CustomerClassID", ClassificationID);
                QueryBuilderObject.SetField("StreetID", streetID);
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("OrganizationID", organizationID);
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                err = QueryBuilderObject.InsertQueryString("CustomerOutlet", db_vms);
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

            string CustPaymentTermID = GetFieldValue("CustomerOutletPaymentTerm", "PaymentTermID", "PaymentTermID = " + PaymentTermID + " and CustomerID=" + CustomerID + " and Outletid=" + OutletID, db_vms);
            if (CustPaymentTermID == string.Empty && PaymentTermID != "")
            {

                QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                err = QueryBuilderObject.InsertQueryString("CustomerOutletPaymentTerm", db_vms);
            }

            //InCubeQuery DeleteCustDivTerm = new InCubeQuery(db_vms, "Delete From CustOutDivPaymentTerm Where CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and DivisionID=" + divisionID + "");
            //err = DeleteCustDivTerm.ExecuteNonQuery();
            //QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
            //QueryBuilderObject.SetField("OutletID", OutletID);
            //QueryBuilderObject.SetField("DivisionID", divisionID);
            //QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
            //err = QueryBuilderObject.InsertQueryString("CustOutDivPaymentTerm", db_vms);

            //InCubeQuery DeleteCustDivType = new InCubeQuery(db_vms, "Delete From CustOutDivCustomerType Where CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and DivisionID=" + divisionID + "");
            //err = DeleteCustDivType.ExecuteNonQuery();
            //QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
            //QueryBuilderObject.SetField("OutletID", OutletID);
            //QueryBuilderObject.SetField("DivisionID", divisionID);
            //QueryBuilderObject.SetField("CustomerTypeID", CustType);
            //err = QueryBuilderObject.InsertQueryString("CustOutDivCustomerType", db_vms);


            ExistCustomer = GetFieldValue("RNCustomerAccountType", "OutletID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " AND AccountTypeCode='" + CategoryCode + "' and DivisionID=" + divisionID + "", db_vms).Trim();
            if (ExistCustomer.Equals(string.Empty))
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("DivisionID", divisionID);
                QueryBuilderObject.SetField("AccountTypeCode", "'" + CategoryCode + "'");
                QueryBuilderObject.SetField("AccountTypeName", "'" + CategoryName + "'");
                err = QueryBuilderObject.InsertQueryString("RNCustomerAccountType", db_vms);
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

            ExistCustomer = GetFieldValue("AccountCustOut", "AccountID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " and AccountID in (select AccountID from Account where OrganizationID=" + organizationID + ") ", db_vms);
            if (ExistCustomer == string.Empty)
            {
                AccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("AccountTypeID", "1");
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.SetField("Balance", Balance);
                QueryBuilderObject.SetField("GL", "0");
                QueryBuilderObject.SetField("OrganizationID", organizationID);
                QueryBuilderObject.SetField("CurrencyID", "1");
                QueryBuilderObject.SetField("ParentAccountID", parentAccount);
                err = QueryBuilderObject.InsertQueryString("Account", db_vms);

                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                err = QueryBuilderObject.InsertQueryString("AccountCustOut", db_vms);

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + CustomerCode + ":" + CustomerDescriptionEnglish.Trim() + " Account'");
                err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + CustomerCode + ":" + CustomerDescriptionArabic.Trim() + " Account'");
                err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
            }
            else
            {
                AccountID = int.Parse(ExistCustomer);
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                //QueryBuilderObject.SetField("Balance", Balance);
                err = QueryBuilderObject.UpdateQueryString("Account", "AccountID=" + ExistCustomer + "", db_vms);

                QueryBuilderObject.SetField("Description", "'" + CustomerCode + ":" + CustomerDescriptionArabic.Trim() + " Account'");
                err = QueryBuilderObject.UpdateQueryString("AccountLanguage", "AccountID=" + ExistCustomer + " and LanguageID=1", db_vms);
            }
            string Parent2 = AccountID.ToString();
            ExistCustomer = GetFieldValue("AccountCustOutDiv", "AccountID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " and DivisionID=" + divisionID + " and AccountID in (select AccountID from Account where OrganizationID=" + organizationID + ") ", db_vms);
            if (ExistCustomer == string.Empty)
            {
                AccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("AccountTypeID", "1");
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.SetField("Balance", Balance);
                QueryBuilderObject.SetField("GL", "0");
                QueryBuilderObject.SetField("OrganizationID", organizationID);
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
                QueryBuilderObject.SetField("Description", "'" + CustomerCode + ":" + CustomerDescriptionEnglish.Trim() + " Account'");
                err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "'" + CustomerCode + ":" + CustomerDescriptionArabic.Trim() + " Account'");
                err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
            }
            else
            {
                AccountID = int.Parse(ExistCustomer);
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                //QueryBuilderObject.SetField("Balance", Balance);
                err = QueryBuilderObject.UpdateQueryString("Account", "AccountID=" + ExistCustomer + "", db_vms);

                QueryBuilderObject.SetField("Description", "'" + CustomerCode + ":" + CustomerDescriptionArabic.Trim() + " Account'");
                err = QueryBuilderObject.UpdateQueryString("AccountLanguage", "AccountID=" + ExistCustomer + " and LanguageID=1", db_vms);
            }
            //if (err == InCubeErrors.Success)
            //{
            //    err = UpdateFlag("accmst", "ConvCode='" + CustomerCode + "'");
            //}

            #region Territory


            //string deleteFromRoutecustomer = string.Format("delete from RouteCustomer where customerID={0} and OutletID={1}", CustomerID, OutletID);
            //qry = new InCubeQuery(deleteFromRoutecustomer, db_vms);
            //err = qry.ExecuteNonQuery();

            //string deleteFromRoutecustomer = string.Format("delete from CustOutTerritory where customerID={0} and OutletID={1}", CustomerID, OutletID);
            //qry = new InCubeQuery(deleteFromRoutecustomer, db_vms);
            //err = qry.ExecuteNonQuery();


            string TerritoryID = GetFieldValue("Territory", "TerritoryID", "TerritoryCode='" + RouteCode + "'", db_vms);
            if (TerritoryID == string.Empty)
            {
                TerritoryID = GetFieldValue("[Territory]", "isnull(max(TerritoryID),0)+1", db_vms);
                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                QueryBuilderObject.SetField("OrganizationID", organizationID);
                QueryBuilderObject.SetField("TerritoryCode", "'" + RouteCode + "'");
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                err = QueryBuilderObject.InsertQueryString("Territory", db_vms);

                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + RouteName + "'");
                QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);

                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "'" + RouteName + "'");
                err = QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);

            }
            else
            {
                QueryBuilderObject.SetField("Description", "'" + RouteName + "'");
                err = QueryBuilderObject.UpdateQueryString("TerritoryLanguage", "TerritoryID=" + TerritoryID + "", db_vms);
            }
            //string deleteCustTerr = "delete from EmployeeTerritory where TerritoryID=" + TerritoryID + "";
            //InCubeQuery qry = new InCubeQuery(deleteCustTerr, db_vms);
            //err = qry.ExecuteNonQuery();

            //string EmployeeID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode='" + SalesmanCode + "'", db_vms).Trim();
            //if (!EmployeeID.Equals(string.Empty))
            //{
            //    QueryBuilderObject.SetField("EmployeeID", EmployeeID);
            //    QueryBuilderObject.SetField("TerritoryID", TerritoryID);
            //    err = QueryBuilderObject.InsertQueryString("EmployeeTerritory", db_vms);
            //}
            if (SalesmanDivision.Equals(divisionID))
            {
                string isManual = GetFieldValue("ManualRoutes", "RouteCode", "RouteCode LIKE '%" + RouteCode + "%'", db_vms);
                if (isManual == string.Empty)
                {
                    err = ExistObject("CustOutTerritory", "TerritoryID", "TerritoryID = " + TerritoryID + " AND CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        incubeQuery = new InCubeQuery(db_vms, "DELETE FROM CustOutTerritory WHERE CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " AND TerritoryID NOT IN (SELECT TerritoryID FROM Territory WHERE TerritoryCode IN (SELECT RouteCode FROM ManualRoutes))");
                        incubeQuery.ExecuteNonQuery();
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
                }
            }

            string RouteID = GetFieldValue("[Route]", "RouteID", "RouteCode='" + RouteCode + "'", db_vms);
            if (RouteID == string.Empty)
            {
                RouteID = GetFieldValue("[Route]", "isnull(max(RouteID),0)+1", db_vms);
                QueryBuilderObject.SetField("RouteID", RouteID);
                QueryBuilderObject.SetField("Inactive", "0");
                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                QueryBuilderObject.SetField("RouteCode", "'" + RouteCode + "'");
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                err = QueryBuilderObject.InsertQueryString("[Route]", db_vms);

                QueryBuilderObject.SetField("RouteID", RouteID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + RouteName + "'");
                QueryBuilderObject.InsertQueryString("RouteLanguage", db_vms);

                QueryBuilderObject.SetField("RouteID", RouteID);
                QueryBuilderObject.SetField("Week", "0");
                QueryBuilderObject.SetField("Sunday", "0");
                QueryBuilderObject.SetField("Monday", "0");
                QueryBuilderObject.SetField("Tuesday", "0");
                QueryBuilderObject.SetField("Wednesday", "0");
                QueryBuilderObject.SetField("Thursday", "0");
                QueryBuilderObject.SetField("Friday", "0");
                QueryBuilderObject.SetField("Saturday", "0");
                QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("Description", "'" + RouteName + "'");
                err = QueryBuilderObject.UpdateQueryString("RouteLanguage", "RouteID=" + RouteID + "", db_vms);

            }
            //deleteCustTerr = "delete from RouteCustomer where customerid=" + CustomerID + " AND OutletID = " + OutletID + "";
            //qry = new InCubeQuery(deleteCustTerr, db_vms);
            //err = qry.ExecuteNonQuery();
            if (SalesmanDivision.Equals(divisionID))
            {
                string isManual = GetFieldValue("ManualRoutes", "RouteCode", "RouteCode LIKE '%" + RouteCode + "%'", db_vms);
                if (isManual == string.Empty)
                {
                    err = ExistObject("RouteCustomer", "RouteID", "RouteID = " + RouteID + " AND CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                    if (err != InCubeErrors.Success)
                    {

                        QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                        QueryBuilderObject.SetField("OutletID", OutletID);
                        QueryBuilderObject.SetField("RouteID", RouteID);
                        err = QueryBuilderObject.InsertQueryString("RouteCustomer", db_vms);
                    }
                }
            }

            InCubeQuery DeleteCustDivHold;
            string DeleteQNIEGrp = string.Empty;
            string DeleteQNIEDist = string.Empty;
            string DeleteQNIERoute = string.Empty;
            string CheckQNIETbl = GetFieldValue("QNIE_CustDistDivDelete", "RouteID", "CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and ChannelID=" + ChannelID + " and DivisionID=" + divisionID + "  and RouteID=" + RouteID + "", db_vms).Trim();
            if (CheckQNIETbl.Equals(string.Empty))
            {
                QueryBuilderObject.SetField("RouteID", RouteID);
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("ChannelID", ChannelID);
                QueryBuilderObject.SetField("DivisionID", divisionID);
                QueryBuilderObject.SetField("GroupID", GroupID);
                QueryBuilderObject.SetField("Deleted", "0");
                err = QueryBuilderObject.InsertQueryString("QNIE_CustDistDivDelete", db_vms);
            }
            if (inactiveOnChannel.Trim().Equals("1"))
            {
                incubeQuery = new InCubeQuery("UPDATE QNIE_CustDistDivDelete SET Deleted = 1 WHERE CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and ChannelID=" + ChannelID + " and DivisionID=" + divisionID, db_vms);
                incubeQuery.ExecuteNonQuery();
                string existWithOtherChannel = GetFieldValue("QNIE_CustDistDivDelete", "RouteID", "CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and ChannelID<>" + ChannelID + " and DivisionID=" + divisionID + "", db_vms).Trim();
                if (!existWithOtherChannel.Equals(string.Empty))
                {
                    if (existWithOtherChannel.Equals(RouteID))
                    {
                        //Remove Channel Assignment in QNIE table, and change the customer group. then insert the new assignment in the QNIE table and customer group.
                        DeleteQNIEGrp = string.Format("Delete from CustomerOutletGroup where CustomerID=" + customerID + " and outletID=" + OutletID + " and GroupID=" + GroupID + "");
                        DeleteQNIEDist = string.Format("Delete from QNIE_CustDistDivDelete where CustomerID=" + customerID + " and outletID=" + OutletID + " and ChannelID=" + ChannelID + " and DivisionID=" + divisionID + " and RouteID=" + RouteID + "");
                        DeleteCustDivHold = new InCubeQuery(DeleteQNIEGrp, db_vms);
                        err = DeleteCustDivHold.ExecuteNonQuery();
                        DeleteCustDivHold = new InCubeQuery(DeleteQNIEDist, db_vms);
                        err = DeleteCustDivHold.ExecuteNonQuery();
                    }
                    else
                    {
                        //remove channel assignemnt, group assignment, route assignment
                        DeleteQNIEGrp = string.Format("Delete from CustomerOutletGroup where CustomerID=" + customerID + " and outletID=" + OutletID + " and GroupID=" + GroupID + "");
                        DeleteQNIEDist = string.Format("Delete from QNIE_CustDistDivDelete where CustomerID=" + customerID + " and outletID=" + OutletID + " and ChannelID=" + ChannelID + " and DivisionID=" + divisionID + "  and RouteID=" + RouteID + "");
                        DeleteQNIERoute = string.Format("Delete from RouteCustomer where CustomerID=" + customerID + " and outletID=" + OutletID + " and RouteID=" + RouteID + "");
                        DeleteCustDivHold = new InCubeQuery(DeleteQNIEGrp, db_vms);
                        err = DeleteCustDivHold.ExecuteNonQuery();
                        DeleteCustDivHold = new InCubeQuery(DeleteQNIEDist, db_vms);
                        err = DeleteCustDivHold.ExecuteNonQuery();
                        DeleteCustDivHold = new InCubeQuery(DeleteQNIERoute, db_vms);
                        err = DeleteCustDivHold.ExecuteNonQuery();
                    }
                }
                else
                {
                    //remove channel assignemnt, group assignment, route assignment
                    DeleteQNIEGrp = string.Format("Delete from CustomerOutletGroup where CustomerID=" + customerID + " and outletID=" + OutletID + " and GroupID=" + GroupID + "");
                    DeleteQNIEDist = string.Format("Delete from QNIE_CustDistDivDelete where CustomerID=" + customerID + " and outletID=" + OutletID + " and ChannelID=" + ChannelID + " and DivisionID=" + divisionID + "");
                    DeleteQNIERoute = string.Format("Delete from RouteCustomer where CustomerID=" + customerID + " and outletID=" + OutletID + " and RouteID=" + RouteID + "");
                    DeleteCustDivHold = new InCubeQuery(DeleteQNIEGrp, db_vms);
                    err = DeleteCustDivHold.ExecuteNonQuery();
                    DeleteCustDivHold = new InCubeQuery(DeleteQNIEDist, db_vms);
                    err = DeleteCustDivHold.ExecuteNonQuery();
                    DeleteCustDivHold = new InCubeQuery(DeleteQNIERoute, db_vms);
                    err = DeleteCustDivHold.ExecuteNonQuery();
                }

            }
            else
            {
                incubeQuery = new InCubeQuery("UPDATE QNIE_CustDistDivDelete SET Deleted = 0 WHERE CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and ChannelID=" + ChannelID + " and DivisionID=" + divisionID, db_vms);
                err = incubeQuery.ExecuteNonQuery();
                CustomerOutletGoupCheck(customerID, OutletID, GroupID, "", DivisionCode);
                string existWithOtherRoute = GetFieldValue("QNIE_CustDistDivDelete", "RouteID", "CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and ChannelID=" + ChannelID + " and DivisionID=" + divisionID + " and RouteID<>" + RouteID + "", db_vms).Trim();
                if (!existWithOtherRoute.Equals(string.Empty))
                {
                    //NOTE: THE BELOW ROWS WHERE COMMENTED BECAUSE VODAFONE ARE PROVIDING THE JURNEY PLAN ON EXCEL FILE, AND WE DIDN'T WANT IT TO BE OVERWRITTEN BY INTEGRATION
                    //DeleteQNIERoute = string.Format("Delete from RouteCustomer where CustomerID=" + customerID + " and outletID=" + OutletID + " and RouteID<>" + RouteID + "");
                    //string DeleteCustOutTerrDiv = string.Format("Delete from CustOutTerritory where CustomerID=" + customerID + " and outletID=" + OutletID + " and TerritoryID<>" + TerritoryID + "");
                    DeleteQNIEDist = string.Format("Delete from QNIE_CustDistDivDelete where CustomerID=" + customerID + " and outletID=" + OutletID + " and ChannelID=" + ChannelID + " and DivisionID=" + divisionID + " and RouteID<>" + RouteID + "");
                    DeleteCustDivHold = new InCubeQuery(DeleteQNIERoute, db_vms);
                    err = DeleteCustDivHold.ExecuteNonQuery();
                    //DeleteCustDivHold = new InCubeQuery(DeleteCustOutTerrDiv, db_vms);
                    //err = DeleteCustDivHold.ExecuteNonQuery();
                    DeleteCustDivHold = new InCubeQuery(DeleteQNIEDist, db_vms);
                    err = DeleteCustDivHold.ExecuteNonQuery();
                }
            }



            //DeleteCustDivHold = new InCubeQuery(db_vms, "Delete From CustOutDivOnHoldStatus Where CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and DivisionID=" + divisionID + "");
            //err = DeleteCustDivHold.ExecuteNonQuery();
            ////CustomerOutletGoupCheck(CustomerID.ToString(), OutletID, GroupID, GroupID2,DivisionCode);
            //if (OnHold.Equals("1"))
            //{
            //    // DeleteCustDivHold = new InCubeQuery(db_vms, "Delete From CustOutDivOnHoldStatus Where CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and DivisionID=" + divisionID + "");
            //    //err = DeleteCustDivTerm.ExecuteNonQuery();
            //    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
            //    QueryBuilderObject.SetField("OutletID", OutletID);
            //    QueryBuilderObject.SetField("DivisionID", divisionID);
            //    err = QueryBuilderObject.InsertQueryString("CustOutDivOnHoldStatus", db_vms);
            //}
            //if (removeFromRoute)
            //{
            //    string deleteCustRoute = string.Format("delete from RouteCustomer where RouteID=" + RouteID + " and CustomerID=" + customerID + " and OutletID=" + OutletID + "");
            //    string deleteCustTerritory = string.Format("delete from CustOutTerritory where TerritoryID=" + TerritoryID + " and CustomerID=" + customerID + " and OutletID=" + OutletID + "");
            //    DeleteCustDivHold = new InCubeQuery(deleteCustRoute, db_vms);
            //    err = DeleteCustDivHold.ExecuteNonQuery();
            //    DeleteCustDivHold = new InCubeQuery(deleteCustTerritory, db_vms);
            //    err = DeleteCustDivHold.ExecuteNonQuery();

            //}

            string PayerID = "";
            string isPayerExistForThisCustomerOnThisDivision = GetFieldValue("Payer", "PayerID", "CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and PayerID in (select PayerID from PayerDivision where DivisionID=" + divisionID + ")", db_vms).Trim();
            if (isPayerExistForThisCustomerOnThisDivision.Equals(string.Empty))
            {
                PayerID = GetFieldValue("Payer", "isnull(MAX(PayerID),0) + 1", db_vms);
                QueryBuilderObject.SetField("PayerID", PayerID);
                QueryBuilderObject.SetField("PayerCode", "'" + PayerCode + "'");
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID.ToString());
                err = QueryBuilderObject.InsertQueryString("Payer", db_vms);
            }
            else
            {
                PayerID = isPayerExistForThisCustomerOnThisDivision;
                QueryBuilderObject.SetField("PayerCode", "'" + PayerCode + "'");
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID.ToString());
                err = QueryBuilderObject.UpdateQueryString("Payer", "PayerID = " + PayerID.ToString(), db_vms);
            }
            string payerDivision = GetFieldValue("PayerDivision", "PayerID", "PayerID = " + PayerID + " and DivisionID=" + divisionID + "", db_vms).Trim();
            if (payerDivision.Equals(string.Empty))
            {
                QueryBuilderObject.SetField("PayerID", PayerID);
                QueryBuilderObject.SetField("DivisionID", divisionID);
                err = QueryBuilderObject.InsertQueryString("PayerDivision", db_vms);
            }
            int PayerAccountID = 1;
            ExistCustomer = GetFieldValue("AccountPayer", "AccountID", "PayerID = " + PayerID, db_vms);
            if (ExistCustomer != string.Empty)
            {
                PayerAccountID = int.Parse(GetFieldValue("AccountPayer", "AccountID", "PayerID = " + PayerID, db_vms));
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.SetField("Balance", Balance);
                err = QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + PayerAccountID.ToString(), db_vms);
            }
            else
            {
                PayerAccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));
                QueryBuilderObject.SetField("AccountID", PayerAccountID.ToString());
                QueryBuilderObject.SetField("AccountTypeID", "1");
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.SetField("Balance", Balance);
                QueryBuilderObject.SetField("GL", "0");
                QueryBuilderObject.SetField("OrganizationID", organizationID);
                QueryBuilderObject.SetField("CurrencyID", "1");
                err = QueryBuilderObject.InsertQueryString("Account", db_vms);

                QueryBuilderObject.SetField("PayerID", PayerID);
                QueryBuilderObject.SetField("AccountID", PayerAccountID.ToString());
                err = QueryBuilderObject.InsertQueryString("AccountPayer", db_vms);

                QueryBuilderObject.SetField("AccountID", PayerAccountID.ToString());
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish.Trim() + " PayerAccount'");
                err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);

                QueryBuilderObject.SetField("AccountID", PayerAccountID.ToString());
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionEnglish.Trim() + " PayerAccount'");
                err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
            }
            #endregion

            #endregion


            #endregion
        }

        private void CustomerOutletGoupCheck(string CustomerID, string OutletID, string GroupID, string GroupID2, string DivisionCode)
        {
            try
            {
                InCubeQuery DeleteCustomerGroup = new InCubeQuery(db_vms, "Delete From CustomerOutletGroup Where CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " and GroupID<>" + GroupID + " and GroupID in (select GroupID from CustomerGroup where GroupCode like '%/" + DivisionCode + "')");
                err = DeleteCustomerGroup.ExecuteNonQuery();

                err = ExistObject("CustomerOutletGroup", "GroupID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " and GroupID=" + GroupID.Trim() + " and GroupID in (select GroupID from CustomerGroup where GroupCode like '%/" + DivisionCode + "')", db_vms);
                if (err != InCubeErrors.Success)
                {
                    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                    QueryBuilderObject.SetField("OutletID", OutletID);
                    QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                    err = QueryBuilderObject.InsertQueryString("CustomerOutletGroup", db_vms);
                }
                if (!GroupID2.Trim().Equals(string.Empty))
                {
                    //err = ExistObject("CustomerOutletGroup", "GroupID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " and GroupID=" + GroupID2.Trim() + "", db_vms);
                    //if (err != InCubeErrors.Success)
                    //{
                    //    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                    //    QueryBuilderObject.SetField("OutletID", OutletID);
                    //    QueryBuilderObject.SetField("GroupID", GroupID2.ToString());
                    //    err = QueryBuilderObject.InsertQueryString("CustomerOutletGroup", db_vms);
                    //}
                }
            }
            catch
            {

            }
        }

        public override void UpdateDiscount()
        {
            try
            {
                UpdateDiscountFromSAP();
            }
            catch
            {
            }
        }

        public override void UpdateInvoice()
        {

            string exceptionTransaction = string.Empty;
            string exceptionDate = string.Empty;
            InCubeErrors err;
            int Tran_Counter = 0;
            try
            {

                DataTable DT = GetInvoiceTable(ref Tran_Counter);
                InCubeQuery qryOut = new InCubeQuery(db_vms, "UpdateOutstanding", 1000000000);
                qryOut.AddParameter("@OutstandingTbl", DT);
                err = qryOut.ExecuteStoredProcedure();
                if (err == InCubeErrors.Success)
                {
                    WriteMessage("\r\n");
                    WriteMessage("<<< OUTSTANDING UPDATED SUCCESSFULLY >>>" + DateTime.Now.ToString());
                }
                else
                {
                    WriteMessage("\r\n");
                    WriteMessage("<<< OUTSTANDING UPDATE FAILED! >>>" + DateTime.Now.ToString());
                }
                return;

            }
            catch
            {

            }
        }

        public override void UpdateItem()
        {
            //NOTE:
            //I STILL NEED TO HANDLE THE BASE UOM, PLEASE CHECK THE INTEGRATION DOCUMENT TO SEE WHEN THE UOM IS CONSIDERED A BASE UOM.
            //ALSO, I NEED TO ADD THE FUNCTIONALITIES FOR THE FIELDS THAT EXIST IN THE DOCUMENT BUT DON'T EXIST IN THE INTEGRATION HERE.
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            InCubeQuery qry;
            DataTable DT = GetItemTable();



            //HERE YOU WILL WRITE THE INVALID ITEMS ON AN EXTERNAL TEXT FILE "InvalidEntry.txt"
            //WriteExceptions(Exceptions("ITEMS_N"), "Item Exceptions");
            ClearProgress();
            SetProgressMax(DT.Rows.Count);
            //(int i = 0; i < DT.Rows.Count; i++)
            foreach (DataRow dr in DT.Rows)
            {
                ReportProgress("Updating Items");

                string ItemCode = dr["ItemCode"].ToString().Trim();
                string ItemName = dr["ItemName"].ToString().Trim();
                string ItemCategoryCode = dr["ItemCategoryCode"].ToString().Trim();
                if (ItemCategoryCode.Equals(string.Empty)) ItemCategoryCode = "Default";
                string ItemCategoryDescription = dr["ItemCategoryDescription"].ToString().Trim();
                if (ItemCategoryDescription.Equals(string.Empty)) ItemCategoryDescription = "Default";
                string ItemGroupCode = dr["ItemGroupCode"].ToString().Trim();
                if (ItemGroupCode.Equals(string.Empty)) ItemGroupCode = "Default";
                string ItemGroupName = dr["ItemGroupName"].ToString().Trim();
                if (ItemGroupName.Equals(string.Empty)) ItemGroupName = "Default";
                string ItemBrandCode = dr["ItemBrandCode"].ToString().Trim();
                if (ItemBrandCode.Equals(string.Empty)) ItemBrandCode = "Default";
                string ItemBrandName = dr["ItemBrandName"].ToString().Trim();
                if (ItemBrandName.Equals(string.Empty)) ItemBrandName = "Default";
                string ItemUOM = dr["ItemUOM"].ToString().Trim();
                string Numerator = dr["Numerator"].ToString().Trim();
                string Denominator = dr["Denominator"].ToString().Trim();
                string DivisionCode = dr["DivisionCode"].ToString().Trim();
                string DivisionName = dr["DivisionName"].ToString().Trim();
                string CompanyCodeLevel2 = dr["CompanyCodeLevel2"].ToString().Trim();
                string CompanyCodeLevel3 = dr["CompanyCodeLevel3"].ToString().Trim();
                string FLAG = dr["FLAG"].ToString().Trim();
                string Barcode = dr["Barcode"].ToString().Trim();
                string InActive = dr["InActive"].ToString().Trim();
                string CWM = dr["CWM"].ToString().Trim();
                string Origin = CompanyCodeLevel3;
                string PackDef = ItemBrandName;
                string BaseUOM = dr["BaseUOM"].ToString().Trim();
                string isBaseUOM = "0";
                string OrganizationCode = CompanyCodeLevel3;
                DivisionCode = OrganizationCode;
                ItemCode = ItemCode.Substring(10, 8);
                //CHECK IF THIS ITEM IS AN ACTIVE ITEM IN (QNIE_ActiveItems)
                //string existActiveItem = GetFieldValue("QNIE_ActiveItems", "ItemCode", "ItemCode='" + ItemCode + "'", db_vms).Trim();
                //if (existActiveItem.Equals(string.Empty)) continue;

                string OrganizationID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + OrganizationCode + "'", db_vms).Trim();
                if (OrganizationID.Equals(string.Empty)) continue;
                if (Numerator.Equals(string.Empty) || Denominator.Equals(string.Empty)) continue;

                if (ItemUOM.Equals(BaseUOM))//this is a base UOM
                {
                    isBaseUOM = "1";
                }
                ItemCategoryCode = ItemCategoryCode;


                #region ItemDivision
                DivisionCode = "70";
                DivisionName = "01 TELECOM";
                string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode = '" + DivisionCode + "'", db_vms);
                if (DivisionID.Equals(string.Empty))
                {
                    DivisionID = GetFieldValue("Division", "isnull(MAX(DivisionID),0) + 1", db_vms);
                    QueryBuilderObject.SetField("DivisionID", DivisionID);
                    QueryBuilderObject.SetField("DivisionCode", "'" + DivisionCode + "'");
                    QueryBuilderObject.SetField("OrganizationID", OrganizationID);// THIS USED TO BE 1 THEN I ADDED THE MiscOrgID FOR THE NEW RELEASE INTEGRATION.
                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    QueryBuilderObject.InsertQueryString("Division", db_vms);
                    string DivisionString = OrganizationCode;
                    QueryBuilderObject.SetField("DivisionID", DivisionID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + DivisionString + "'");
                    err = QueryBuilderObject.InsertQueryString("DivisionLanguage", db_vms);
                }

                #endregion ItemDivision

                #region ItemCategory

                string ItemCategoryID = GetFieldValue("ItemCategoryLanguage", "ItemCategoryID", "Description= '" + ItemCategoryDescription + "' and ItemCategoryID in (select ItemCategoryID from ItemCategory where ItemCategoryCode='" + ItemCategoryCode + "')", db_vms);

                if (ItemCategoryID.Equals(string.Empty))
                {
                    ItemCategoryID = GetFieldValue("ItemCategory", "isnull(MAX(ItemCategoryID),0) + 1", db_vms);

                    QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());
                    QueryBuilderObject.SetField("ItemCategoryCode", "'" + ItemCategoryCode + "'");
                    QueryBuilderObject.SetField("DivisionID", DivisionID);

                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    err = QueryBuilderObject.InsertQueryString("ItemCategory", db_vms);

                    QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + ItemCategoryDescription + "'");
                    err = QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", db_vms);
                }
                else
                {
                    //QueryBuilderObject.SetField("DivisionID", DivisionID);
                    //err = QueryBuilderObject.UpdateQueryString("ItemCategory", "ItemCategoryID = " + ItemCategoryID, db_vms);
                }

                #endregion ItemCategory

                #region Brand New
                string BrandNew = GetFieldValue("BrandLanguage", "BrandID", "Description= '" + ItemBrandName + "'", db_vms);

                if (BrandNew.Equals(string.Empty))
                {
                    BrandNew = GetFieldValue("Brand", "isnull(MAX(BrandID),0) + 1", db_vms);

                    QueryBuilderObject.SetField("BrandID", BrandNew.ToString());
                    err = QueryBuilderObject.InsertQueryString("Brand", db_vms);

                    QueryBuilderObject.SetField("BrandID", BrandNew.ToString());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + ItemBrandName + "'");
                    err = QueryBuilderObject.InsertQueryString("BrandLanguage", db_vms);
                }
                else
                {
                    QueryBuilderObject.SetField("Description", "'" + ItemBrandName + "'");
                    err = QueryBuilderObject.UpdateQueryString("BrandLanguage", "BrandID = " + BrandNew, db_vms);
                }
                #endregion Brand New

                #region Get Barcode

                string realBarcode = ItemCode;

                #endregion Get Barcode

                #region Item
                string ItemID = GetFieldValue("Item", "ItemID", "ItemCode = '" + ItemCode + "' order by ItemID asc", db_vms);
                if (!ItemID.Equals(string.Empty))
                {
                    TOTALUPDATED++;
                    //QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());
                    //QueryBuilderObject.SetField("ItemCode", "'" + ItemCode + "'");
                    //QueryBuilderObject.SetField("PackDefinition", "'" + PackDef + "'");
                    QueryBuilderObject.SetField("InActive", "0");
                    QueryBuilderObject.SetField("BrandID", BrandNew);
                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    err = QueryBuilderObject.UpdateQueryString("Item", "ItemCode = '" + ItemCode + "'", db_vms);

                    QueryBuilderObject.SetField("Description", "'" + ItemName + "'");
                    err = QueryBuilderObject.UpdateQueryString("ItemLanguage", " ItemID IN ( SELECT ITEMID FROM ITEM WHERE ITEMCODE='" + ItemCode + "' ) AND LanguageID = 1", db_vms);
                }
                else
                {
                    TOTALINSERTED++;

                    ItemID = GetFieldValue("Item", "isnull(MAX(ItemID),0) + 1", db_vms);

                    QueryBuilderObject.SetField("ItemID", ItemID);
                    QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());
                    QueryBuilderObject.SetField("ItemCode", "'" + ItemCode + "'");
                    QueryBuilderObject.SetField("PackDefinition", "'" + PackDef + "'");
                    QueryBuilderObject.SetField("InActive", "0");
                    QueryBuilderObject.SetField("ItemType", "1");
                    QueryBuilderObject.SetField("BrandID", BrandNew);
                    QueryBuilderObject.SetField("Origin", "'" + Origin + "'");
                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    err = QueryBuilderObject.InsertQueryString("Item", db_vms);

                    QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + ItemName + "'");
                    err = QueryBuilderObject.InsertQueryString("ItemLanguage", db_vms);
                }
                #endregion Item

                #region Pack

                #region PackType

                int PacktypeID = 1;
                err = ExistObject("PackTypeLanguage", "PackTypeID", " Description = '" + ItemUOM + "'", db_vms);
                if (err == InCubeErrors.Success)
                {
                    PacktypeID = int.Parse(GetFieldValue("PackTypeLanguage", "PackTypeID", " Description = '" + ItemUOM + "'", db_vms));
                }
                else
                {
                    PacktypeID = int.Parse(GetFieldValue("PackType", "ISNULL(MAX(PackTypeID),0) + 1", db_vms));

                    QueryBuilderObject.SetField("PackTypeID", PacktypeID.ToString());
                    QueryBuilderObject.InsertQueryString("PackType", db_vms);

                    QueryBuilderObject.SetField("PackTypeID", PacktypeID.ToString());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + ItemUOM + "'");
                    err = QueryBuilderObject.InsertQueryString("PackTypeLanguage", db_vms);
                }

                int PackID = 1;

                #region Get Pack Quantity
                string PackQuantity = "1";
                bool Multiply = false;
                foreach (DataRow row in DT.Select("ItemCode='0000000000" + ItemCode + "'"))
                {
                    if (int.Parse(row["Denominator"].ToString().Trim()) > int.Parse(row["Numerator"].ToString().Trim()))
                    {
                        Multiply = false;
                        PackQuantity = row["Denominator"].ToString().Trim();
                    }
                    else if (int.Parse(row["Denominator"].ToString().Trim()) < int.Parse(row["Numerator"].ToString().Trim()))
                    {
                        Multiply = true;
                        PackQuantity = row["Numerator"].ToString().Trim();
                    }
                    else if (int.Parse(row["Denominator"].ToString().Trim()) == int.Parse(row["Numerator"].ToString().Trim()))
                    {
                        Multiply = true;
                    }
                }
                if (isBaseUOM.Equals("1"))
                {
                    if (Multiply)
                    {
                        PackQuantity = "1";
                    }
                    else
                    {
                        // PackQuantity = Denominator;
                    }
                    if (ItemUOM.Trim().ToLower().Equals("kg"))
                    {
                        PackQuantity = "1000";
                    }
                }
                else
                {
                    if (Multiply)
                    {
                        //PackQuantity = Numerator;
                    }
                    else
                    {
                        PackQuantity = "1";//decimal.Round( (decimal.Parse("1") / decimal.Parse(PackQuantity)),4).ToString();
                    }
                    if (ItemUOM.Trim().ToLower().Equals("kg"))
                    {
                        PackQuantity = "1000";
                    }
                }

                #endregion

                if (CWM.Trim().Equals("1") && !ItemUOM.Trim().ToLower().Equals("kg"))//THIS IS THE CWM UOM. SO THERE IS NO NEED TO INSERT A PACK.
                {
                    string checkCWM = GetFieldValue("SecondaryPack", "Quantity", "ItemID=" + ItemID + " order by Quantity Desc", db_vms).Trim();
                    if (checkCWM.Equals(string.Empty))
                    {
                        QueryBuilderObject.SetField("ItemID", ItemID);
                        QueryBuilderObject.SetField("PackTypeID", PacktypeID.ToString());
                        QueryBuilderObject.SetField("Quantity", PackQuantity);
                        QueryBuilderObject.SetField("Barcode", "'" + Barcode + "'");
                        err = QueryBuilderObject.InsertQueryString("SecondaryPack", db_vms);
                    }
                    else if (decimal.Parse(checkCWM) < decimal.Parse(Denominator))
                    {
                        InCubeQuery DeleteCustDivTerm = new InCubeQuery(db_vms, "Delete From SecondaryPack Where ItemID=" + ItemID + " ");
                        err = DeleteCustDivTerm.ExecuteNonQuery();
                        QueryBuilderObject.SetField("ItemID", ItemID);
                        QueryBuilderObject.SetField("PackTypeID", PacktypeID.ToString());
                        QueryBuilderObject.SetField("Quantity", PackQuantity);
                        QueryBuilderObject.SetField("Barcode", "'" + Barcode + "'");
                        err = QueryBuilderObject.InsertQueryString("SecondaryPack", db_vms);
                    }
                }
                else
                {
                    err = ExistObject("Pack", "PackID", "ItemID = " + ItemID + " and PackTypeID = " + PacktypeID, db_vms);

                    if (err == InCubeErrors.Success)
                    {
                        PackID = int.Parse(GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " and PackTypeID = " + PacktypeID, db_vms));

                        QueryBuilderObject.SetField("Quantity", PackQuantity);
                        QueryBuilderObject.SetField("EquivalencyFactor", "0");

                        err = QueryBuilderObject.UpdateQueryString("Pack", "PackID = " + PackID, db_vms);
                    }
                    else
                    {
                        PackID = int.Parse(GetFieldValue("Pack", "ISNULL(MAX(PackID),0) + 1", db_vms));

                        QueryBuilderObject.SetField("PackID", PackID.ToString());
                        QueryBuilderObject.SetField("Barcode", "'" + ItemCode + "'");
                        QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                        QueryBuilderObject.SetField("PackTypeID", PacktypeID.ToString());
                        QueryBuilderObject.SetField("Quantity", PackQuantity);
                        QueryBuilderObject.SetField("EquivalencyFactor", "0");
                        QueryBuilderObject.SetField("HasSerialNumber", "1");
                        err = QueryBuilderObject.InsertQueryString("Pack", db_vms);

                        if (ItemUOM.Trim().ToLower().Equals("kg"))
                        {
                            int CWPacktypeID = 0;
                            err = ExistObject("PackTypeLanguage", "PackTypeID", " Description = 'Gram'", db_vms);
                            if (err == InCubeErrors.Success)
                            {
                                CWPacktypeID = int.Parse(GetFieldValue("PackTypeLanguage", "PackTypeID", " Description = 'Gram'", db_vms));
                            }
                            else
                            {
                                CWPacktypeID = int.Parse(GetFieldValue("PackType", "ISNULL(MAX(PackTypeID),0) + 1", db_vms));

                                QueryBuilderObject.SetField("PackTypeID", CWPacktypeID.ToString());
                                err = QueryBuilderObject.InsertQueryString("PackType", db_vms);

                                QueryBuilderObject.SetField("PackTypeID", CWPacktypeID.ToString());
                                QueryBuilderObject.SetField("LanguageID", "1");
                                QueryBuilderObject.SetField("Description", "'Gram'");
                                err = QueryBuilderObject.InsertQueryString("PackTypeLanguage", db_vms);
                            }

                            int CWPackID = int.Parse(GetFieldValue("Pack", "ISNULL(MAX(PackID),0) + 1", db_vms));
                            QueryBuilderObject.SetField("PackID", CWPackID.ToString());
                            QueryBuilderObject.SetField("Barcode", "'" + ItemCode + "'");
                            QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                            QueryBuilderObject.SetField("PackTypeID", CWPacktypeID.ToString());
                            QueryBuilderObject.SetField("Quantity", "1");
                            QueryBuilderObject.SetField("EquivalencyFactor", "0");
                            err = QueryBuilderObject.InsertQueryString("Pack", db_vms);
                        }
                    }
                    if (Denominator.Trim().Equals("1"))
                    {
                        //SAP confirmed that a UOM can only exist in one group
                        qry = new InCubeQuery("delete from PackGroupDetail where PackID=" + PackID.ToString() + "", db_vms);
                        err = qry.ExecuteNonQuery();
                    }
                    if (ItemGroupName.Trim().Equals(string.Empty)) continue;
                    string InvanGroup = GetFieldValue("PackGroupLanguage", "PackGroupID", "Description='" + ItemGroupName + "' and languageID=1", db_vms).Trim();
                    if (InvanGroup.Equals(string.Empty))
                    {

                        InvanGroup = GetFieldValue("PackGroup", "ISNULL(MAX(PackGroupID),0) + 1", db_vms);
                        if (GetFieldValue("MasterPackGroup", "MasterPackGroupID", "MasterPackGroupCode='" + ItemGroupCode.ToString() + "' ", db_vms).Trim().Equals(string.Empty))
                        {
                            QueryBuilderObject.SetField("MasterPackGroupID", InvanGroup.ToString());
                            QueryBuilderObject.SetField("MasterPackGroupCode", "'" + ItemGroupCode.ToString() + "'");
                            err = QueryBuilderObject.InsertQueryString("MasterPackGroup", db_vms);
                        }
                        QueryBuilderObject.SetField("MasterPackGroupID", InvanGroup.ToString());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + ItemGroupName.ToString() + "'");
                        err = QueryBuilderObject.InsertQueryString("MasterPackGroupLanguage", db_vms);

                        QueryBuilderObject.SetField("PackGroupID", InvanGroup.ToString());
                        QueryBuilderObject.SetField("MasterPackGroupID", InvanGroup.ToString());
                        QueryBuilderObject.SetField("PackGroupCode", "'" + ItemGroupCode.ToString() + "'");
                        err = QueryBuilderObject.InsertQueryString("PackGroup", db_vms);

                        QueryBuilderObject.SetField("PackGroupID", InvanGroup.ToString());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + ItemGroupName.ToString() + "'");
                        err = QueryBuilderObject.InsertQueryString("PackGroupLanguage", db_vms);
                    }
                    else
                    {
                        QueryBuilderObject.SetField("Description", ItemGroupName.ToString());
                        err = QueryBuilderObject.UpdateQueryString("PackGroupLanguage", "PackGroupID=" + InvanGroup + "", db_vms);
                    }

                    QueryBuilderObject.SetField("PackGroupID", InvanGroup.ToString());
                    QueryBuilderObject.SetField("PackID", PackID.ToString().Trim());
                    err = QueryBuilderObject.InsertQueryString("PackGroupDetail", db_vms);

                    if (isBaseUOM.Equals("1"))
                    {
                        string deleteBaseUOM = string.Format("delete from ItemBaseUOM where ItemID={0}", ItemID);
                        qry = new InCubeQuery(deleteBaseUOM, db_vms);
                        err = qry.ExecuteNonQuery();
                        if (err == InCubeErrors.Success)
                        {
                            QueryBuilderObject.SetField("PackID", PackID.ToString());
                            QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                            QueryBuilderObject.SetField("PackTypeID", PacktypeID.ToString());
                            err = QueryBuilderObject.InsertQueryString("ItemBaseUOM", db_vms);

                            QueryBuilderObject.SetField("DefaultPackID", PackID.ToString());
                            err = QueryBuilderObject.UpdateQueryString("Item", "ItemID = " + ItemID + "", db_vms);
                        }
                    }
                    else if (ItemUOM.Trim().ToLower().Equals("kg"))
                    {
                        QueryBuilderObject.SetField("DefaultPackID", PackID.ToString());
                        err = QueryBuilderObject.UpdateQueryString("Item", "ItemID = " + ItemID + "", db_vms);
                    }

                    //int ItemCountInTBL = DT.Select("ItemCode='0000000000" + ItemCode + "'").Length;
                    //int ItemCountInDB = Convert.ToInt32(GetFieldValue("Pack", "Count(PackID)", "ItemID=" + ItemID + "", db_vms).Trim());
                    //if (ItemCountInDB >= ItemCountInTBL)
                    //{
                    //    string ReDistributeUOMConv = GetFieldValue("Pack", "Top(1) Quantity", "ItemID=" + ItemID + " order by Quantity Desc", db_vms).Trim();
                    //    if (!ReDistributeUOMConv.Equals(string.Empty))
                    //    {
                    //        string UpdatePackQuantity = string.Format("update Pack set quantity=" + ReDistributeUOMConv + "/quantity where ItemID=" + ItemID + "");
                    //        qry = new InCubeQuery(UpdatePackQuantity, db_vms);
                    //        err = qry.ExecuteNonQuery();
                    //    }
                    //}



                }
                #endregion PackType

                #endregion Pack
            }

            //UpdateUOM();
            DT.Dispose();
            WriteMessage("\r\n");
            WriteMessage("<<< ITEMS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        public override void UpdatePrice()
        {
            UpdatePriceFromSAP();
        }

        public override void UpdatePromotion()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;
            try
            {
                DataTable DT = GetPromotionTable();
                //HERE YOU WILL WRITE THE INVALID PROMOTIONS ON AN EXTERNAL TEXT FILE "InvalidEntry.txt"
                //WriteExceptions(Exceptions("FOC"), "PROMOTION Exceptions");
                string deleteCustomerAssignment = string.Format("Delete from CustomerPromotion");
                InCubeQuery qry = new InCubeQuery(deleteCustomerAssignment, db_vms);
                err = qry.ExecuteNonQuery();
                #region GroupPromotion
                ClearProgress();
                SetProgressMax(DT.Select("(SalesGroupCode='' or SalesGroupCode is null) and DivisionCode is not null and DivisionCode<>'' and (CustomerShipTo='' or CustomerShipTo is null) ").Length);
                foreach (DataRow Rows in DT.Select("(SalesGroupCode='' or SalesGroupCode is null) and DivisionCode is not null and DivisionCode<>'' and (CustomerShipTo='' or CustomerShipTo is null) "))
                {
                    ReportProgress("Updating Promotions ");
                    string Companycode = Rows["Companycode "].ToString().Trim();
                    string CompanyCodeLevel3 = Rows["CompanyCodeLevel3"].ToString().Trim();
                    string ValidFrom = Rows["ValidFrom"].ToString().Trim();
                    string ValidTo = Rows["ValidTo"].ToString().Trim().Replace("9999", "2020");
                    string BuyItemCode = Rows["BuyItemCode"].ToString().Trim();
                    string BuyUOM = Rows["BuyUOM"].ToString().Trim();
                    string BuyConversionFactor = Rows["BuyConversionFactor"].ToString().Trim();
                    string BuyQuantity = Rows["BuyQuantity"].ToString().Trim();
                    string GetItemCode = Rows["GetItemCode"].ToString().Trim();
                    string GetUOM = Rows["GetUOM"].ToString().Trim();
                    string GetConversionFactor = Rows["GetConversionFactor"].ToString().Trim();
                    string GetQuantity = Rows["GetQuantity"].ToString().Trim();
                    string DivisionCode = Rows["DivisionCode"].ToString().Trim();
                    string SalesGroupCode = Rows["SalesGroupCode"].ToString().Trim();
                    string CustomerShipTo = Rows["CustomerShipTo"].ToString().Trim();
                    string InclusiveOrExclusive = Rows["InclusiveOrExclusive"].ToString().Trim();
                    string FLAG = Rows["FLAG"].ToString().Trim();
                    string IsDeleted = Rows["IsDeleted"].ToString().Trim();
                    string ItemGroupBuy = string.Empty;
                    string PromotionID = "";// Rows[0].ToString();
                    string PromotionDesc = "";// Rows[1].ToString();
                    string IsRepeated = "1";//Rows[6].ToString();
                    string RepeatCount = "10000";//Rows[7].ToString();
                    //string ItemGroupGet = Rows[12].ToString().Trim();//By Anas adding new promotions schemas
                    string CreationDate = DateTime.Now.ToString().Trim();// Rows[14].ToString();
                    string ModDate = DateTime.Now.ToString().Trim(); //Rows[15].ToString();
                    //if (!SalesGroupCode.Equals("142")) continue;
                    string CustomerID;
                    string OutletID = "1";
                    if (ValidFrom.Trim().Equals(string.Empty) || ValidTo.Trim().Equals(string.Empty)) continue;
                    //getting groups
                    string CustomerGroupID = GetFieldValue("CustomerGroup", "GroupID", "GroupCode='" + SalesGroupCode + "'", db_vms).Trim();
                    string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode='" + DivisionCode + "'", db_vms).Trim();
                    string OrganizationID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + CompanyCodeLevel3 + "'", db_vms).Trim();
                    if ((!SalesGroupCode.Equals(string.Empty) && CustomerGroupID.Equals(string.Empty))) continue;
                    if ((BuyItemCode.Trim().Equals(string.Empty)) || (GetItemCode.Equals(string.Empty))) continue;

                    CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode = '" + CustomerShipTo + "' and customerid in (select customerid from CustomerOrganization where OrganizationID=" + OrganizationID + ")", db_vms);
                    OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerID = " + CustomerID + " AND CustomerCode = '" + CustomerShipTo + "'", db_vms);

                    string BuyPacktypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " Description = '" + BuyUOM + "'", db_vms);
                    string GETPacktypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " Description = '" + GetUOM + "'", db_vms);
                    //ExtendItem(BuyItemCode, DivisionCode, OrganizationID);
                    //ExtendItem(GetItemCode, DivisionCode, OrganizationID);
                    string BuyPackID = GetFieldValue("Pack INNER JOIN Item ON Pack.ItemID = Item.ItemID INNER JOIN ITEMCATEGORY IC ON ITEM.ITEMCATEGORYID=IC.ITEMCATEGORYID INNER JOIN DIVISION D ON D.DIVISIONID=IC.DIVISIONID INNER JOIN ORGANIZATION O ON D.ORGANIZATIONID=O.ORGANIZATIONID", "Pack.PackID", "Item.ItemCode ='" + BuyItemCode + "' AND Pack.PackTypeID = " + BuyPacktypeID + " AND D.DIVISIONID=" + DivisionID + " and O.OrganizationID=" + OrganizationID + "", db_vms);
                    string GetPackID = GetFieldValue("Pack INNER JOIN Item ON Pack.ItemID = Item.ItemID INNER JOIN ITEMCATEGORY IC ON ITEM.ITEMCATEGORYID=IC.ITEMCATEGORYID INNER JOIN DIVISION D ON D.DIVISIONID=IC.DIVISIONID INNER JOIN ORGANIZATION O ON D.ORGANIZATIONID=O.ORGANIZATIONID", "Pack.PackID", "Item.ItemCode ='" + GetItemCode + "' AND Pack.PackTypeID = " + GETPacktypeID + " AND D.DIVISIONID=" + DivisionID + " and O.OrganizationID=" + OrganizationID + "", db_vms);
                    string BuyItemCodeDesc = GetFieldValue(" Item INNER JOIN ItemLanguage ON Item.ItemID = ItemLanguage.ItemID INNER JOIN ITEMCATEGORY IC ON ITEM.ITEMCATEGORYID=IC.ITEMCATEGORYID INNER JOIN DIVISION D ON D.DIVISIONID=IC.DIVISIONID INNER JOIN ORGANIZATION O ON D.ORGANIZATIONID=O.ORGANIZATIONID", "ItemLanguage.Description", "Item.ItemCode ='" + BuyItemCode + "' And ItemLanguage.LanguageID = 1 AND D.DIVISIONID=" + DivisionID + " and O.OrganizationID=" + OrganizationID + "", db_vms);
                    string GetItemCodeDesc = GetFieldValue(" Item INNER JOIN ItemLanguage ON Item.ItemID = ItemLanguage.ItemID INNER JOIN ITEMCATEGORY IC ON ITEM.ITEMCATEGORYID=IC.ITEMCATEGORYID INNER JOIN DIVISION D ON D.DIVISIONID=IC.DIVISIONID INNER JOIN ORGANIZATION O ON D.ORGANIZATIONID=O.ORGANIZATIONID", "ItemLanguage.Description", "Item.ItemCode ='" + GetItemCode + "' And ItemLanguage.LanguageID = 1 AND D.DIVISIONID=" + DivisionID + " and O.OrganizationID=" + OrganizationID + "", db_vms);
                    string CHANNEL_ID = string.Empty;
                    string CHANNEL_CODE = string.Empty;
                    if (!CHANNEL_CODE.Equals(string.Empty)) CHANNEL_ID = GetFieldValue("Channel", "ChannelID", " ChannelCode = '" + CHANNEL_CODE + "'", db_vms);
                    if (BuyPackID.Equals(string.Empty) && GetPackID.Equals(string.Empty))
                    {
                        continue;
                    }
                    if (ItemGroupBuy.Equals(string.Empty))
                    {
                        PromotionDesc = "Buy " + BuyQuantity.Trim() + "(Q) from item : " + BuyItemCode.Trim() + " and Get " + GetQuantity.Trim() + "(Q) from item : " + GetItemCode.Trim() + " Division :" + DivisionCode.Trim();
                        PromotionID = GetFieldValue("PromotionLanguage", "PromotionID", "Description = '" + PromotionDesc + "' and LanguageID = 1 and promotionid in (select promotionid from promotion where isdeleted=0)", db_vms);
                    }

                    if (PromotionID.Equals(string.Empty))
                    {
                        PromotionID = GetFieldValue("Promotion", "isnull(MAX(PromotionID),0) + 1", db_vms);
                    }
                    err = ExistObject("Promotion", "PromotionID", "PromotionID = " + PromotionID, db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        QueryBuilderObject.SetField("PromotionCode", "'" + PromotionID + "'");
                        QueryBuilderObject.SetField("StartDate", "'" + DateTime.Parse(ValidFrom).ToString("yyyy-MM-dd") + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + DateTime.Parse(ValidTo).ToString("yyyy-MM-dd") + "'");
                        QueryBuilderObject.SetField("IsRepeated", IsRepeated);
                        QueryBuilderObject.SetField("RepeatCount", RepeatCount);
                        QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                        //QueryBuilderObject.SetField("IsLoyaltyPromotion", "0");
                        QueryBuilderObject.SetField("Inactive", "0");
                        QueryBuilderObject.SetField("PromotionType", "1");
                        err = QueryBuilderObject.InsertQueryString("Promotion", db_vms);

                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + PromotionDesc + "'");
                        err = QueryBuilderObject.InsertQueryString("PromotionLanguage", db_vms);
                    }
                    else
                    {
                        QueryBuilderObject.SetField("PromotionCode", "'" + PromotionID + "'");
                        QueryBuilderObject.SetField("IsDeleted", "0");
                        QueryBuilderObject.SetField("StartDate", "'" + DateTime.Parse(ValidFrom).ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + DateTime.Parse(ValidTo).ToString(DateFormat) + "'");
                        err = QueryBuilderObject.UpdateQueryString("Promotion", "PromotionID=" + PromotionID + "", db_vms);
                    }
                    string CustomerPromotionID = string.Empty;

                    #region Group Customers Assignment



                    string GetCustomers = string.Format(@"Select CO.CustomerID,CO.OutletID from CustomerOutletDivision CO
WHERE CO.DivisionID={0}
 ", DivisionID);
                    InCubeQuery QRY = new InCubeQuery(GetCustomers, db_vms);
                    err = QRY.Execute();
                    DataTable INNER = QRY.GetDataTable();
                    foreach (DataRow DR in INNER.Rows)
                    {
                        string CustID = DR["CustomerID"].ToString().Trim();
                        string OutID = DR["OutletID"].ToString().Trim();
                        string deleteCustomerDiscountAssignment = string.Format("Delete from CustomerPromotion where CustomerID={0} and OutletID={1}", CustID, OutID);
                        QRY = new InCubeQuery(deleteCustomerDiscountAssignment, db_vms);
                        err = QRY.ExecuteNonQuery();
                        CustomerPromotionID = GetFieldValue("CustomerPromotion", "isnull(MAX(CustomerPromotionID),0) + 1", db_vms);
                        QueryBuilderObject.SetField("CustomerPromotionID", CustomerPromotionID);
                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        if (!CustID.Equals(string.Empty) && !OutID.Equals(string.Empty))
                        {
                            QueryBuilderObject.SetField("CustomerID", CustID);
                            QueryBuilderObject.SetField("OutletID", OutID);
                        }
                        err = QueryBuilderObject.InsertQueryString("CustomerPromotion", db_vms);
                    }

                    #endregion


                    err = ExistObject("PromotionOptionDetail", "PromotionID", "PromotionID = " + PromotionID, db_vms);
                    if (err == InCubeErrors.Success)
                    {
                        TOTALUPDATED++;

                        QueryBuilderObject.SetField("PackID", BuyPackID);

                        QueryBuilderObject.SetField("Value", BuyQuantity);
                        QueryBuilderObject.SetField("BatchNo", "'1990/01/01'");
                        QueryBuilderObject.SetField("ExpiryDate", "'1990/01/01'");
                        if (ItemGroupBuy.Equals(string.Empty))
                        {
                            QueryBuilderObject.UpdateQueryString("PromotionOptionDetail", "PromotionID = " + PromotionID + " AND PromotionOptionID = 1 AND PromotionOptionTypeID = 1 AND PromotionOptionDetailID = 1 AND PromotionOptionDetailTypeID = 2", db_vms);
                        }
                        else
                        {
                            QueryBuilderObject.UpdateQueryString("PromotionOptionDetail", "PromotionID = " + PromotionID + " AND PromotionOptionID = 1 AND PromotionOptionTypeID = 1 AND PromotionOptionDetailID = 1 AND PromotionOptionDetailTypeID = 3", db_vms);
                        }

                        if (ItemGroupBuy.Equals(string.Empty))
                        {
                            QueryBuilderObject.SetField("Description", "' Buy " + BuyQuantity + " Quantity From Item " + BuyItemCodeDesc + "'");
                        }
                        else
                        {
                            QueryBuilderObject.SetField("Description", "' Buy " + BuyQuantity + " Quantity From Group " + ItemGroupBuy + "'");
                        }
                        err = QueryBuilderObject.UpdateQueryString("PromOptionDetailLanguage", "PromotionID = " + PromotionID + " AND PromotionOptionID = 1  AND PromotionOptionDetailID = 1 And LanguageID = 1", db_vms);
                        //if (ItemGroupBuy.Equals(string.Empty))
                        //{
                        QueryBuilderObject.SetField("PackID", GetPackID);
                        //}
                        //else
                        //{
                        //    QueryBuilderObject.SetField("PackGroupID", ItemGroupGetID);
                        //}
                        QueryBuilderObject.SetField("Value", GetQuantity);
                        QueryBuilderObject.SetField("BatchNo", "'1990/01/01'");
                        QueryBuilderObject.SetField("ExpiryDate", "'1990/01/01'");
                        err = QueryBuilderObject.UpdateQueryString("PromotionOptionDetail", "PromotionID = " + PromotionID + " AND PromotionOptionID = 1 AND PromotionOptionTypeID = 2 AND PromotionOptionDetailID = 2 AND PromotionOptionDetailTypeID = 3", db_vms);
                        QueryBuilderObject.SetField("Description", "' Get " + GetQuantity + " Quantity From Item " + GetItemCodeDesc + "'");
                        err = QueryBuilderObject.UpdateQueryString("PromOptionDetailLanguage", "PromotionID = " + PromotionID + " AND PromotionOptionID = 1 AND PromotionOptionDetailID = 2 And LanguageID = 1", db_vms);
                    }
                    else
                    {
                        TOTALINSERTED++;
                        string DetailID = GetFieldValue("PromotionOptionDetail", "isnull(MAX(DetailID),0) + 1", db_vms);
                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        QueryBuilderObject.SetField("PromotionOptionID", "1");
                        QueryBuilderObject.SetField("PromotionOptionTypeID", "1");
                        QueryBuilderObject.SetField("PromotionOptionDetailID", "1");

                        QueryBuilderObject.SetField("PromotionOptionDetailTypeID", "2");
                        QueryBuilderObject.SetField("PackID", BuyPackID);

                        QueryBuilderObject.SetField("Value", BuyQuantity);
                        QueryBuilderObject.SetField("Range", "0");
                        QueryBuilderObject.SetField("DetailID", DetailID);
                        QueryBuilderObject.SetField("BatchNo", "'1990/01/01'");
                        QueryBuilderObject.SetField("ExpiryDate", "'1990/01/01'");
                        err = QueryBuilderObject.InsertQueryString("PromotionOptionDetail", db_vms);

                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        QueryBuilderObject.SetField("PromotionOptionID", "1");
                        QueryBuilderObject.SetField("PromotionOptionDetailID", "1");
                        QueryBuilderObject.SetField("LanguageID", "1");

                        QueryBuilderObject.SetField("Description", "' Buy " + BuyQuantity + " Quantity From Item " + BuyItemCodeDesc + "'");

                        err = QueryBuilderObject.InsertQueryString("PromOptionDetailLanguage", db_vms);
                        DetailID = GetFieldValue("PromotionOptionDetail", "isnull(MAX(DetailID),0) + 1", db_vms);
                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        QueryBuilderObject.SetField("PromotionOptionID", "1");
                        QueryBuilderObject.SetField("PromotionOptionTypeID", "2");
                        QueryBuilderObject.SetField("PromotionOptionDetailID", "2");
                        QueryBuilderObject.SetField("PromotionOptionDetailTypeID", "3");
                        QueryBuilderObject.SetField("PackID", GetPackID);
                        QueryBuilderObject.SetField("Value", GetQuantity);
                        QueryBuilderObject.SetField("DetailID", DetailID);
                        QueryBuilderObject.SetField("BatchNo", "'1990/01/01'");
                        QueryBuilderObject.SetField("ExpiryDate", "'1990/01/01'");
                        err = QueryBuilderObject.InsertQueryString("PromotionOptionDetail", db_vms);
                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        QueryBuilderObject.SetField("PromotionOptionID", "1");
                        QueryBuilderObject.SetField("PromotionOptionDetailID", "2");
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "' Get " + GetQuantity + " Quantity From Item " + GetItemCodeDesc + "'");
                        err = QueryBuilderObject.InsertQueryString("PromOptionDetailLanguage", db_vms);
                    }
                }
                #endregion

                #region GroupPromotion
                ClearProgress();
                SetProgressMax(DT.Select("SalesGroupCode is not null and SalesGroupCode<>'' and (CustomerShipTo='' or CustomerShipTo is null)").Length);
                foreach (DataRow Rows in DT.Select("SalesGroupCode is not null and SalesGroupCode<>'' and (CustomerShipTo='' or CustomerShipTo is null) "))
                {
                    ReportProgress("Updating Promotions ");
                    string Companycode = Rows["Companycode "].ToString().Trim();
                    string CompanyCodeLevel3 = Rows["CompanyCodeLevel3"].ToString().Trim();
                    string ValidFrom = Rows["ValidFrom"].ToString().Trim();
                    string ValidTo = Rows["ValidTo"].ToString().Trim().Replace("9999", "2020");
                    string BuyItemCode = Rows["BuyItemCode"].ToString().Trim();
                    string BuyUOM = Rows["BuyUOM"].ToString().Trim();
                    string BuyConversionFactor = Rows["BuyConversionFactor"].ToString().Trim();
                    string BuyQuantity = Rows["BuyQuantity"].ToString().Trim();
                    string GetItemCode = Rows["GetItemCode"].ToString().Trim();
                    string GetUOM = Rows["GetUOM"].ToString().Trim();
                    string GetConversionFactor = Rows["GetConversionFactor"].ToString().Trim();
                    string GetQuantity = Rows["GetQuantity"].ToString().Trim();
                    string DivisionCode = Rows["DivisionCode"].ToString().Trim();
                    string SalesGroupCode = Rows["SalesGroupCode"].ToString().Trim();
                    string CustomerShipTo = Rows["CustomerShipTo"].ToString().Trim();
                    string InclusiveOrExclusive = Rows["InclusiveOrExclusive"].ToString().Trim();
                    string FLAG = Rows["FLAG"].ToString().Trim();
                    string IsDeleted = Rows["IsDeleted"].ToString().Trim();
                    string ItemGroupBuy = string.Empty;
                    string PromotionID = "";// Rows[0].ToString();
                    string PromotionDesc = "";// Rows[1].ToString();
                    string IsRepeated = "1";//Rows[6].ToString();
                    string RepeatCount = "10000";//Rows[7].ToString();
                    //string ItemGroupGet = Rows[12].ToString().Trim();//By Anas adding new promotions schemas
                    string CreationDate = DateTime.Now.ToString().Trim();// Rows[14].ToString();
                    string ModDate = DateTime.Now.ToString().Trim(); //Rows[15].ToString();
                    //if (!SalesGroupCode.Equals("142")) continue;
                    string CustomerID;
                    string OutletID = "1";
                    if (ValidFrom.Trim().Equals(string.Empty) || ValidTo.Trim().Equals(string.Empty)) continue;
                    //getting groups
                    string CustomerGroupID = GetFieldValue("CustomerGroup", "GroupID", "GroupCode='" + SalesGroupCode + "'", db_vms).Trim();
                    string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode='" + DivisionCode + "'", db_vms).Trim();
                    string OrganizationID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + CompanyCodeLevel3 + "'", db_vms).Trim();
                    if ((!SalesGroupCode.Equals(string.Empty) && CustomerGroupID.Equals(string.Empty))) continue;
                    if ((BuyItemCode.Trim().Equals(string.Empty)) || (GetItemCode.Equals(string.Empty))) continue;

                    CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode = '" + CustomerShipTo + "' and customerid in (select customerid from CustomerOrganization where OrganizationID=" + OrganizationID + ")", db_vms);
                    OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerID = " + CustomerID + " AND CustomerCode = '" + CustomerShipTo + "'", db_vms);

                    string BuyPacktypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " Description = '" + BuyUOM + "'", db_vms);
                    string GETPacktypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " Description = '" + GetUOM + "'", db_vms);
                    //ExtendItem(BuyItemCode, DivisionCode, OrganizationID);
                    ExtendItem(GetItemCode, DivisionCode, OrganizationID);
                    string BuyPackID = GetFieldValue("Pack INNER JOIN Item ON Pack.ItemID = Item.ItemID INNER JOIN ITEMCATEGORY IC ON ITEM.ITEMCATEGORYID=IC.ITEMCATEGORYID INNER JOIN DIVISION D ON D.DIVISIONID=IC.DIVISIONID INNER JOIN ORGANIZATION O ON D.ORGANIZATIONID=O.ORGANIZATIONID", "Pack.PackID", "Item.ItemCode ='" + BuyItemCode + "' AND Pack.PackTypeID = " + BuyPacktypeID + " AND D.DIVISIONID=" + DivisionID + " and O.OrganizationID=" + OrganizationID + "", db_vms);
                    string GetPackID = GetFieldValue("Pack INNER JOIN Item ON Pack.ItemID = Item.ItemID INNER JOIN ITEMCATEGORY IC ON ITEM.ITEMCATEGORYID=IC.ITEMCATEGORYID INNER JOIN DIVISION D ON D.DIVISIONID=IC.DIVISIONID INNER JOIN ORGANIZATION O ON D.ORGANIZATIONID=O.ORGANIZATIONID", "Pack.PackID", "Item.ItemCode ='" + GetItemCode + "' AND Pack.PackTypeID = " + GETPacktypeID + " AND D.DIVISIONID=" + DivisionID + " and O.OrganizationID=" + OrganizationID + "", db_vms);
                    string BuyItemCodeDesc = GetFieldValue(" Item INNER JOIN ItemLanguage ON Item.ItemID = ItemLanguage.ItemID INNER JOIN ITEMCATEGORY IC ON ITEM.ITEMCATEGORYID=IC.ITEMCATEGORYID INNER JOIN DIVISION D ON D.DIVISIONID=IC.DIVISIONID INNER JOIN ORGANIZATION O ON D.ORGANIZATIONID=O.ORGANIZATIONID", "ItemLanguage.Description", "Item.ItemCode ='" + BuyItemCode + "' And ItemLanguage.LanguageID = 1 AND D.DIVISIONID=" + DivisionID + " and O.OrganizationID=" + OrganizationID + "", db_vms);
                    string GetItemCodeDesc = GetFieldValue(" Item INNER JOIN ItemLanguage ON Item.ItemID = ItemLanguage.ItemID INNER JOIN ITEMCATEGORY IC ON ITEM.ITEMCATEGORYID=IC.ITEMCATEGORYID INNER JOIN DIVISION D ON D.DIVISIONID=IC.DIVISIONID INNER JOIN ORGANIZATION O ON D.ORGANIZATIONID=O.ORGANIZATIONID", "ItemLanguage.Description", "Item.ItemCode ='" + GetItemCode + "' And ItemLanguage.LanguageID = 1 AND D.DIVISIONID=" + DivisionID + " and O.OrganizationID=" + OrganizationID + "", db_vms);
                    string CHANNEL_ID = string.Empty;
                    string CHANNEL_CODE = string.Empty;
                    if (!CHANNEL_CODE.Equals(string.Empty)) CHANNEL_ID = GetFieldValue("Channel", "ChannelID", " ChannelCode = '" + CHANNEL_CODE + "'", db_vms);
                    if (BuyPackID.Equals(string.Empty) && GetPackID.Equals(string.Empty))
                    {
                        continue;
                    }
                    if (ItemGroupBuy.Equals(string.Empty))
                    {
                        if (!CustomerID.Trim().Equals(string.Empty))
                        {
                            PromotionDesc = "Buy " + BuyQuantity.Trim() + "(Q) from item : " + BuyItemCode.Trim() + " and Get " + GetQuantity.Trim() + "(Q) from item : " + GetItemCode.Trim() + " Customer :" + CustomerShipTo.Trim();
                        }
                        else if (!SalesGroupCode.Equals(string.Empty))
                        {
                            PromotionDesc = "Buy " + BuyQuantity.Trim() + "(Q) from item : " + BuyItemCode.Trim() + " and Get " + GetQuantity.Trim() + "(Q) from item : " + GetItemCode.Trim() + " Customer Group :" + SalesGroupCode.Trim();
                        }
                        PromotionID = GetFieldValue("PromotionLanguage", "PromotionID", "Description = '" + PromotionDesc + "' and LanguageID = 1 and promotionid in (select promotionid from promotion where isdeleted=0)", db_vms);
                    }
                    else
                    {
                        PromotionDesc = "Buy " + BuyQuantity.Trim() + "(Q) from Group : " + ItemGroupBuy + " and Get " + GetQuantity.Trim() + "(Q) from item : " + GetItemCode.Trim() + " Customer Group :" + SalesGroupCode;
                        PromotionID = GetFieldValue("PromotionLanguage", "PromotionID", "Description = '" + PromotionDesc + "' and LanguageID = 1", db_vms);
                    }
                    if (PromotionID.Equals(string.Empty))
                    {
                        PromotionID = GetFieldValue("Promotion", "isnull(MAX(PromotionID),0) + 1", db_vms);
                    }
                    err = ExistObject("Promotion", "PromotionID", "PromotionID = " + PromotionID, db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        QueryBuilderObject.SetField("PromotionCode", "'" + PromotionID + "'");
                        QueryBuilderObject.SetField("StartDate", "'" + DateTime.Parse(ValidFrom).ToString("yyyy-MM-dd") + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + DateTime.Parse(ValidTo).ToString("yyyy-MM-dd") + "'");
                        QueryBuilderObject.SetField("IsRepeated", IsRepeated);
                        QueryBuilderObject.SetField("RepeatCount", RepeatCount);
                        QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                        //QueryBuilderObject.SetField("IsLoyaltyPromotion", "0");
                        QueryBuilderObject.SetField("Inactive", "0");
                        QueryBuilderObject.SetField("PromotionType", "1");
                        err = QueryBuilderObject.InsertQueryString("Promotion", db_vms);

                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + PromotionDesc + "'");
                        err = QueryBuilderObject.InsertQueryString("PromotionLanguage", db_vms);
                    }
                    else
                    {
                        QueryBuilderObject.SetField("PromotionCode", "'" + PromotionID + "'");
                        QueryBuilderObject.SetField("IsDeleted", "0");
                        QueryBuilderObject.SetField("StartDate", "'" + DateTime.Parse(ValidFrom).ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + DateTime.Parse(ValidTo).ToString(DateFormat) + "'");
                        err = QueryBuilderObject.UpdateQueryString("Promotion", "PromotionID=" + PromotionID + "", db_vms);
                    }
                    string CustomerPromotionID = string.Empty;

                    #region Group Customers Assignment

                    string GroupIDExist = GetFieldValue("CustomerGroup", "GroupID", "GroupCode='" + SalesGroupCode + "'", db_vms).Trim();
                    if (GroupIDExist.Equals(string.Empty)) continue;
                    string GetCustomers = string.Format(@"Select CO.CustomerID,CO.OutletID from CustomerOutletGroup CO
WHERE CO.GroupID={0}
 ", GroupIDExist);
                    InCubeQuery QRY = new InCubeQuery(GetCustomers, db_vms);
                    err = QRY.Execute();
                    DataTable INNER = QRY.GetDataTable();
                    foreach (DataRow DR in INNER.Rows)
                    {
                        string CustID = DR["CustomerID"].ToString().Trim();
                        string OutID = DR["OutletID"].ToString().Trim();
                        string deleteCustomerDiscountAssignment = string.Format("Delete from CustomerPromotion where CustomerID={0} and OutletID={1}", CustID, OutID);
                        QRY = new InCubeQuery(deleteCustomerDiscountAssignment, db_vms);
                        err = QRY.ExecuteNonQuery();
                        CustomerPromotionID = GetFieldValue("CustomerPromotion", "isnull(MAX(CustomerPromotionID),0) + 1", db_vms);
                        QueryBuilderObject.SetField("CustomerPromotionID", CustomerPromotionID);
                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        if (!CustID.Equals(string.Empty) && !OutID.Equals(string.Empty))
                        {
                            QueryBuilderObject.SetField("CustomerID", CustID);
                            QueryBuilderObject.SetField("OutletID", OutID);
                        }
                        err = QueryBuilderObject.InsertQueryString("CustomerPromotion", db_vms);
                    }

                    #endregion


                    err = ExistObject("PromotionOptionDetail", "PromotionID", "PromotionID = " + PromotionID, db_vms);
                    if (err == InCubeErrors.Success)
                    {
                        TOTALUPDATED++;

                        QueryBuilderObject.SetField("PackID", BuyPackID);

                        QueryBuilderObject.SetField("Value", BuyQuantity);
                        QueryBuilderObject.SetField("BatchNo", "'1990/01/01'");
                        QueryBuilderObject.SetField("ExpiryDate", "'1990/01/01'");
                        if (ItemGroupBuy.Equals(string.Empty))
                        {
                            QueryBuilderObject.UpdateQueryString("PromotionOptionDetail", "PromotionID = " + PromotionID + " AND PromotionOptionID = 1 AND PromotionOptionTypeID = 1 AND PromotionOptionDetailID = 1 AND PromotionOptionDetailTypeID = 2", db_vms);
                        }
                        else
                        {
                            QueryBuilderObject.UpdateQueryString("PromotionOptionDetail", "PromotionID = " + PromotionID + " AND PromotionOptionID = 1 AND PromotionOptionTypeID = 1 AND PromotionOptionDetailID = 1 AND PromotionOptionDetailTypeID = 3", db_vms);
                        }

                        if (ItemGroupBuy.Equals(string.Empty))
                        {
                            QueryBuilderObject.SetField("Description", "' Buy " + BuyQuantity + " Quantity From Item " + BuyItemCodeDesc + "'");
                        }
                        else
                        {
                            QueryBuilderObject.SetField("Description", "' Buy " + BuyQuantity + " Quantity From Group " + ItemGroupBuy + "'");
                        }
                        err = QueryBuilderObject.UpdateQueryString("PromOptionDetailLanguage", "PromotionID = " + PromotionID + " AND PromotionOptionID = 1  AND PromotionOptionDetailID = 1 And LanguageID = 1", db_vms);
                        //if (ItemGroupBuy.Equals(string.Empty))
                        //{
                        QueryBuilderObject.SetField("PackID", GetPackID);
                        //}
                        //else
                        //{
                        //    QueryBuilderObject.SetField("PackGroupID", ItemGroupGetID);
                        //}
                        QueryBuilderObject.SetField("Value", GetQuantity);
                        QueryBuilderObject.SetField("BatchNo", "'1990/01/01'");
                        QueryBuilderObject.SetField("ExpiryDate", "'1990/01/01'");
                        err = QueryBuilderObject.UpdateQueryString("PromotionOptionDetail", "PromotionID = " + PromotionID + " AND PromotionOptionID = 1 AND PromotionOptionTypeID = 2 AND PromotionOptionDetailID = 2 AND PromotionOptionDetailTypeID = 3", db_vms);
                        QueryBuilderObject.SetField("Description", "' Get " + GetQuantity + " Quantity From Item " + GetItemCodeDesc + "'");
                        err = QueryBuilderObject.UpdateQueryString("PromOptionDetailLanguage", "PromotionID = " + PromotionID + " AND PromotionOptionID = 1 AND PromotionOptionDetailID = 2 And LanguageID = 1", db_vms);
                    }
                    else
                    {
                        TOTALINSERTED++;
                        string DetailID = GetFieldValue("PromotionOptionDetail", "isnull(MAX(DetailID),0) + 1", db_vms);
                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        QueryBuilderObject.SetField("PromotionOptionID", "1");
                        QueryBuilderObject.SetField("PromotionOptionTypeID", "1");
                        QueryBuilderObject.SetField("PromotionOptionDetailID", "1");

                        QueryBuilderObject.SetField("PromotionOptionDetailTypeID", "2");
                        QueryBuilderObject.SetField("PackID", BuyPackID);

                        QueryBuilderObject.SetField("Value", BuyQuantity);
                        QueryBuilderObject.SetField("Range", "0");
                        QueryBuilderObject.SetField("DetailID", DetailID);
                        QueryBuilderObject.SetField("BatchNo", "'1990/01/01'");
                        QueryBuilderObject.SetField("ExpiryDate", "'1990/01/01'");
                        err = QueryBuilderObject.InsertQueryString("PromotionOptionDetail", db_vms);

                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        QueryBuilderObject.SetField("PromotionOptionID", "1");
                        QueryBuilderObject.SetField("PromotionOptionDetailID", "1");
                        QueryBuilderObject.SetField("LanguageID", "1");

                        QueryBuilderObject.SetField("Description", "' Buy " + BuyQuantity + " Quantity From Item " + BuyItemCodeDesc + "'");

                        err = QueryBuilderObject.InsertQueryString("PromOptionDetailLanguage", db_vms);
                        DetailID = GetFieldValue("PromotionOptionDetail", "isnull(MAX(DetailID),0) + 1", db_vms);
                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        QueryBuilderObject.SetField("PromotionOptionID", "1");
                        QueryBuilderObject.SetField("PromotionOptionTypeID", "2");
                        QueryBuilderObject.SetField("PromotionOptionDetailID", "2");
                        QueryBuilderObject.SetField("PromotionOptionDetailTypeID", "3");
                        QueryBuilderObject.SetField("PackID", GetPackID);
                        QueryBuilderObject.SetField("Value", GetQuantity);
                        QueryBuilderObject.SetField("DetailID", DetailID);
                        QueryBuilderObject.SetField("BatchNo", "'1990/01/01'");
                        QueryBuilderObject.SetField("ExpiryDate", "'1990/01/01'");
                        err = QueryBuilderObject.InsertQueryString("PromotionOptionDetail", db_vms);
                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        QueryBuilderObject.SetField("PromotionOptionID", "1");
                        QueryBuilderObject.SetField("PromotionOptionDetailID", "2");
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "' Get " + GetQuantity + " Quantity From Item " + GetItemCodeDesc + "'");
                        err = QueryBuilderObject.InsertQueryString("PromOptionDetailLanguage", db_vms);
                    }
                }
                #endregion

                #region CustomerPromotion
                ClearProgress();
                SetProgressMax(DT.Select("CustomerShipTo is not null and CustomerShipTo<>''").Length);

                foreach (DataRow Rows in DT.Select("CustomerShipTo is not null and CustomerShipTo<>''"))
                {
                    ReportProgress("Updating Promotions ");
                    string Companycode = Rows["Companycode "].ToString().Trim();
                    string CompanyCodeLevel3 = Rows["CompanyCodeLevel3"].ToString().Trim();
                    string ValidFrom = Rows["ValidFrom"].ToString().Trim();
                    string ValidTo = Rows["ValidTo"].ToString().Trim().Replace("9999", "2020");
                    string BuyItemCode = Rows["BuyItemCode"].ToString().Trim();
                    string BuyUOM = Rows["BuyUOM"].ToString().Trim();
                    string BuyConversionFactor = Rows["BuyConversionFactor"].ToString().Trim();
                    string BuyQuantity = Rows["BuyQuantity"].ToString().Trim();
                    string GetItemCode = Rows["GetItemCode"].ToString().Trim();
                    string GetUOM = Rows["GetUOM"].ToString().Trim();
                    string GetConversionFactor = Rows["GetConversionFactor"].ToString().Trim();
                    string GetQuantity = Rows["GetQuantity"].ToString().Trim();
                    string DivisionCode = Rows["DivisionCode"].ToString().Trim();
                    string SalesGroupCode = Rows["SalesGroupCode"].ToString().Trim();
                    string CustomerShipTo = Rows["CustomerShipTo"].ToString().Trim();
                    string InclusiveOrExclusive = Rows["InclusiveOrExclusive"].ToString().Trim();
                    string FLAG = Rows["FLAG"].ToString().Trim();
                    string IsDeleted = Rows["IsDeleted"].ToString().Trim();
                    string ItemGroupBuy = string.Empty;
                    string PromotionID = "";// Rows[0].ToString();
                    string PromotionDesc = "";// Rows[1].ToString();
                    string IsRepeated = "1";//Rows[6].ToString();
                    string RepeatCount = "10000";//Rows[7].ToString();
                    //string ItemGroupGet = Rows[12].ToString().Trim();//By Anas adding new promotions schemas
                    string CreationDate = DateTime.Now.ToString().Trim();// Rows[14].ToString();
                    string ModDate = DateTime.Now.ToString().Trim(); //Rows[15].ToString();
                    //if (!SalesGroupCode.Equals("142")) continue;
                    string CustomerID;
                    string OutletID = "1";
                    if (ValidFrom.Trim().Equals(string.Empty) || ValidTo.Trim().Equals(string.Empty)) continue;
                    //getting groups
                    string CustomerGroupID = GetFieldValue("CustomerGroup", "GroupID", "GroupCode='" + SalesGroupCode + "'", db_vms).Trim();
                    string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode='" + DivisionCode + "'", db_vms).Trim();
                    string OrganizationID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + CompanyCodeLevel3 + "'", db_vms).Trim();
                    if ((!SalesGroupCode.Equals(string.Empty) && CustomerGroupID.Equals(string.Empty))) continue;
                    if ((BuyItemCode.Trim().Equals(string.Empty)) || (GetItemCode.Equals(string.Empty))) continue;

                    CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode = '" + CustomerShipTo + "' and customerid in (select customerid from CustomerOrganization where OrganizationID=" + OrganizationID + ")", db_vms);
                    OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerID = " + CustomerID + " AND CustomerCode = '" + CustomerShipTo + "'", db_vms);

                    string BuyPacktypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " Description = '" + BuyUOM + "'", db_vms);
                    string GETPacktypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " Description = '" + GetUOM + "'", db_vms);
                    //ExtendItem(BuyItemCode, DivisionCode, OrganizationID);
                    ExtendItem(GetItemCode, DivisionCode, OrganizationID);
                    string BuyPackID = GetFieldValue("Pack INNER JOIN Item ON Pack.ItemID = Item.ItemID INNER JOIN ITEMCATEGORY IC ON ITEM.ITEMCATEGORYID=IC.ITEMCATEGORYID INNER JOIN DIVISION D ON D.DIVISIONID=IC.DIVISIONID INNER JOIN ORGANIZATION O ON D.ORGANIZATIONID=O.ORGANIZATIONID", "Pack.PackID", "Item.ItemCode ='" + BuyItemCode + "' AND Pack.PackTypeID = " + BuyPacktypeID + " AND D.DIVISIONID=" + DivisionID + " and O.OrganizationID=" + OrganizationID + "", db_vms);
                    string GetPackID = GetFieldValue("Pack INNER JOIN Item ON Pack.ItemID = Item.ItemID INNER JOIN ITEMCATEGORY IC ON ITEM.ITEMCATEGORYID=IC.ITEMCATEGORYID INNER JOIN DIVISION D ON D.DIVISIONID=IC.DIVISIONID INNER JOIN ORGANIZATION O ON D.ORGANIZATIONID=O.ORGANIZATIONID", "Pack.PackID", "Item.ItemCode ='" + GetItemCode + "' AND Pack.PackTypeID = " + GETPacktypeID + " AND D.DIVISIONID=" + DivisionID + " and O.OrganizationID=" + OrganizationID + "", db_vms);
                    string BuyItemCodeDesc = GetFieldValue(" Item INNER JOIN ItemLanguage ON Item.ItemID = ItemLanguage.ItemID INNER JOIN ITEMCATEGORY IC ON ITEM.ITEMCATEGORYID=IC.ITEMCATEGORYID INNER JOIN DIVISION D ON D.DIVISIONID=IC.DIVISIONID INNER JOIN ORGANIZATION O ON D.ORGANIZATIONID=O.ORGANIZATIONID", "ItemLanguage.Description", "Item.ItemCode ='" + BuyItemCode + "' And ItemLanguage.LanguageID = 1 AND D.DIVISIONID=" + DivisionID + " and O.OrganizationID=" + OrganizationID + "", db_vms);
                    string GetItemCodeDesc = GetFieldValue(" Item INNER JOIN ItemLanguage ON Item.ItemID = ItemLanguage.ItemID INNER JOIN ITEMCATEGORY IC ON ITEM.ITEMCATEGORYID=IC.ITEMCATEGORYID INNER JOIN DIVISION D ON D.DIVISIONID=IC.DIVISIONID INNER JOIN ORGANIZATION O ON D.ORGANIZATIONID=O.ORGANIZATIONID", "ItemLanguage.Description", "Item.ItemCode ='" + GetItemCode + "' And ItemLanguage.LanguageID = 1 AND D.DIVISIONID=" + DivisionID + " and O.OrganizationID=" + OrganizationID + "", db_vms);
                    string CHANNEL_ID = string.Empty;
                    string CHANNEL_CODE = string.Empty;
                    if (!CHANNEL_CODE.Equals(string.Empty)) CHANNEL_ID = GetFieldValue("Channel", "ChannelID", " ChannelCode = '" + CHANNEL_CODE + "'", db_vms);
                    if (BuyPackID.Equals(string.Empty) && GetPackID.Equals(string.Empty))
                    {
                        continue;
                    }
                    if (ItemGroupBuy.Equals(string.Empty))
                    {
                        if (!CustomerID.Trim().Equals(string.Empty))
                        {
                            PromotionDesc = "Buy " + BuyQuantity.Trim() + "(Q) from item : " + BuyItemCode.Trim() + " and Get " + GetQuantity.Trim() + "(Q) from item : " + GetItemCode.Trim() + " Customer :" + CustomerShipTo.Trim();
                        }
                        else if (!SalesGroupCode.Equals(string.Empty))
                        {
                            PromotionDesc = "Buy " + BuyQuantity.Trim() + "(Q) from item : " + BuyItemCode.Trim() + " and Get " + GetQuantity.Trim() + "(Q) from item : " + GetItemCode.Trim() + " Customer Group :" + SalesGroupCode.Trim();
                        }
                        PromotionID = GetFieldValue("PromotionLanguage", "PromotionID", "Description = '" + PromotionDesc + "' and LanguageID = 1 and promotionid in (select promotionid from promotion where isdeleted=0)", db_vms);
                    }
                    else
                    {
                        PromotionDesc = "Buy " + BuyQuantity.Trim() + "(Q) from Group : " + ItemGroupBuy + " and Get " + GetQuantity.Trim() + "(Q) from item : " + GetItemCode.Trim() + " Customer Group :" + SalesGroupCode;
                        PromotionID = GetFieldValue("PromotionLanguage", "PromotionID", "Description = '" + PromotionDesc + "' and LanguageID = 1", db_vms);
                    }
                    if (PromotionID.Equals(string.Empty))
                    {
                        PromotionID = GetFieldValue("Promotion", "isnull(MAX(PromotionID),0) + 1", db_vms);
                    }
                    err = ExistObject("Promotion", "PromotionID", "PromotionID = " + PromotionID, db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        QueryBuilderObject.SetField("PromotionCode", "'" + PromotionID + "'");
                        QueryBuilderObject.SetField("StartDate", "'" + DateTime.Parse(ValidFrom).ToString("yyyy-MM-dd") + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + DateTime.Parse(ValidTo).ToString("yyyy-MM-dd") + "'");
                        QueryBuilderObject.SetField("IsRepeated", IsRepeated);
                        QueryBuilderObject.SetField("RepeatCount", RepeatCount);
                        QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                        //QueryBuilderObject.SetField("IsLoyaltyPromotion", "0");
                        QueryBuilderObject.SetField("Inactive", "0");
                        QueryBuilderObject.SetField("PromotionType", "1");
                        err = QueryBuilderObject.InsertQueryString("Promotion", db_vms);

                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + PromotionDesc + "'");
                        err = QueryBuilderObject.InsertQueryString("PromotionLanguage", db_vms);
                    }
                    else
                    {
                        QueryBuilderObject.SetField("PromotionCode", "'" + PromotionID + "'");
                        QueryBuilderObject.SetField("IsDeleted", "0");
                        QueryBuilderObject.SetField("StartDate", "'" + DateTime.Parse(ValidFrom).ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + DateTime.Parse(ValidTo).ToString(DateFormat) + "'");
                        err = QueryBuilderObject.UpdateQueryString("Promotion", "PromotionID=" + PromotionID + "", db_vms);
                    }
                    string CustomerPromotionID = string.Empty;
                    string deleteCustomerDiscountAssignment = string.Format("Delete from CustomerPromotion where CustomerID={0} and OutletID={1}", CustomerID, OutletID);
                    InCubeQuery QRY = new InCubeQuery(deleteCustomerDiscountAssignment, db_vms);
                    err = QRY.ExecuteNonQuery();

                    if (CustomerPromotionID.Equals(string.Empty))
                    {
                        CustomerPromotionID = GetFieldValue("CustomerPromotion", "isnull(MAX(CustomerPromotionID),0) + 1", db_vms);
                        QueryBuilderObject.SetField("CustomerPromotionID", CustomerPromotionID);
                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        if (!CustomerID.Equals(string.Empty) && !OutletID.Equals(string.Empty))
                        {
                            QueryBuilderObject.SetField("CustomerID", CustomerID);
                            QueryBuilderObject.SetField("OutletID", OutletID);
                        }

                        else if (!SalesGroupCode.Equals(string.Empty) && CHANNEL_ID.Equals(string.Empty))
                        {
                            err = QueryBuilderObject.SetField("CustomerGroupID", CustomerGroupID);
                        }
                        else if (CHANNEL_ID.Equals(string.Empty))
                        {
                            err = QueryBuilderObject.SetField("ChannelID", CHANNEL_ID);
                        }
                        err = QueryBuilderObject.InsertQueryString("CustomerPromotion", db_vms);
                        //if (MasterCustomerCode.Equals(string.Empty))
                        //{
                        //    QueryBuilderObject.SetField("AllOutlets", "1");
                        //}
                        //else
                        //{
                        //QueryBuilderObject.SetField("AllOutlets", "0");
                        //}
                    }
                    err = ExistObject("PromotionOptionDetail", "PromotionID", "PromotionID = " + PromotionID, db_vms);
                    if (err == InCubeErrors.Success)
                    {
                        TOTALUPDATED++;

                        QueryBuilderObject.SetField("PackID", BuyPackID);

                        QueryBuilderObject.SetField("Value", BuyQuantity);
                        QueryBuilderObject.SetField("BatchNo", "'1990/01/01'");
                        QueryBuilderObject.SetField("ExpiryDate", "'1990/01/01'");
                        if (ItemGroupBuy.Equals(string.Empty))
                        {
                            QueryBuilderObject.UpdateQueryString("PromotionOptionDetail", "PromotionID = " + PromotionID + " AND PromotionOptionID = 1 AND PromotionOptionTypeID = 1 AND PromotionOptionDetailID = 1 AND PromotionOptionDetailTypeID = 2", db_vms);
                        }
                        else
                        {
                            QueryBuilderObject.UpdateQueryString("PromotionOptionDetail", "PromotionID = " + PromotionID + " AND PromotionOptionID = 1 AND PromotionOptionTypeID = 1 AND PromotionOptionDetailID = 1 AND PromotionOptionDetailTypeID = 3", db_vms);
                        }

                        if (ItemGroupBuy.Equals(string.Empty))
                        {
                            QueryBuilderObject.SetField("Description", "' Buy " + BuyQuantity + " Quantity From Item " + BuyItemCodeDesc + "'");
                        }
                        else
                        {
                            QueryBuilderObject.SetField("Description", "' Buy " + BuyQuantity + " Quantity From Group " + ItemGroupBuy + "'");
                        }
                        err = QueryBuilderObject.UpdateQueryString("PromOptionDetailLanguage", "PromotionID = " + PromotionID + " AND PromotionOptionID = 1  AND PromotionOptionDetailID = 1 And LanguageID = 1", db_vms);
                        //if (ItemGroupBuy.Equals(string.Empty))
                        //{
                        QueryBuilderObject.SetField("PackID", GetPackID);
                        //}
                        //else
                        //{
                        //    QueryBuilderObject.SetField("PackGroupID", ItemGroupGetID);
                        //}
                        QueryBuilderObject.SetField("Value", GetQuantity);
                        QueryBuilderObject.SetField("BatchNo", "'1990/01/01'");
                        QueryBuilderObject.SetField("ExpiryDate", "'1990/01/01'");
                        err = QueryBuilderObject.UpdateQueryString("PromotionOptionDetail", "PromotionID = " + PromotionID + " AND PromotionOptionID = 1 AND PromotionOptionTypeID = 2 AND PromotionOptionDetailID = 2 AND PromotionOptionDetailTypeID = 3", db_vms);
                        QueryBuilderObject.SetField("Description", "' Get " + GetQuantity + " Quantity From Item " + GetItemCodeDesc + "'");
                        err = QueryBuilderObject.UpdateQueryString("PromOptionDetailLanguage", "PromotionID = " + PromotionID + " AND PromotionOptionID = 1 AND PromotionOptionDetailID = 2 And LanguageID = 1", db_vms);
                    }
                    else
                    {
                        TOTALINSERTED++;
                        string DetailID = GetFieldValue("PromotionOptionDetail", "isnull(MAX(DetailID),0) + 1", db_vms);
                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        QueryBuilderObject.SetField("PromotionOptionID", "1");
                        QueryBuilderObject.SetField("PromotionOptionTypeID", "1");
                        QueryBuilderObject.SetField("PromotionOptionDetailID", "1");

                        QueryBuilderObject.SetField("PromotionOptionDetailTypeID", "2");
                        QueryBuilderObject.SetField("PackID", BuyPackID);

                        QueryBuilderObject.SetField("Value", BuyQuantity);
                        QueryBuilderObject.SetField("Range", "0");
                        QueryBuilderObject.SetField("DetailID", DetailID);
                        QueryBuilderObject.SetField("BatchNo", "'1990/01/01'");
                        QueryBuilderObject.SetField("ExpiryDate", "'1990/01/01'");
                        err = QueryBuilderObject.InsertQueryString("PromotionOptionDetail", db_vms);

                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        QueryBuilderObject.SetField("PromotionOptionID", "1");
                        QueryBuilderObject.SetField("PromotionOptionDetailID", "1");
                        QueryBuilderObject.SetField("LanguageID", "1");

                        QueryBuilderObject.SetField("Description", "' Buy " + BuyQuantity + " Quantity From Item " + BuyItemCodeDesc + "'");

                        err = QueryBuilderObject.InsertQueryString("PromOptionDetailLanguage", db_vms);
                        DetailID = GetFieldValue("PromotionOptionDetail", "isnull(MAX(DetailID),0) + 1", db_vms);
                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        QueryBuilderObject.SetField("PromotionOptionID", "1");
                        QueryBuilderObject.SetField("PromotionOptionTypeID", "2");
                        QueryBuilderObject.SetField("PromotionOptionDetailID", "2");
                        QueryBuilderObject.SetField("PromotionOptionDetailTypeID", "3");
                        QueryBuilderObject.SetField("PackID", GetPackID);
                        QueryBuilderObject.SetField("Value", GetQuantity);
                        QueryBuilderObject.SetField("DetailID", DetailID);
                        QueryBuilderObject.SetField("BatchNo", "'1990/01/01'");
                        QueryBuilderObject.SetField("ExpiryDate", "'1990/01/01'");
                        err = QueryBuilderObject.InsertQueryString("PromotionOptionDetail", db_vms);
                        QueryBuilderObject.SetField("PromotionID", PromotionID);
                        QueryBuilderObject.SetField("PromotionOptionID", "1");
                        QueryBuilderObject.SetField("PromotionOptionDetailID", "2");
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "' Get " + GetQuantity + " Quantity From Item " + GetItemCodeDesc + "'");
                        err = QueryBuilderObject.InsertQueryString("PromOptionDetailLanguage", db_vms);
                    }
                }
                #endregion
                DT.Dispose();
            }
            catch
            {
            }
            WriteMessage("\r\n");
            WriteMessage("<<< PROMOTIONS >>> Total Inserted = " + TOTALINSERTED + " Total Updated = " + TOTALUPDATED);
        }

        public override void UpdateSalesPerson()
        {
            try
            {
                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;

                DataTable DT = GetSalespersonTable();

                ClearProgress();
                SetProgressMax(DT.Rows.Count);

                foreach (DataRow dr in DT.Rows)
                {
                    ReportProgress("Updating Salesperon ");

                    string SalesmanCode = dr["SalesmanCode"].ToString().Trim();
                    string SalesmanName = dr["SalesmanName"].ToString().Trim();
                    string SalesmanPhone = dr["SalesmanPhone"].ToString().Trim();
                    string SupervisorCode = dr["SupervisorCode"].ToString().Trim();
                    string SuperVisorName = dr["SuperVisorName"].ToString().Trim();
                    string SalesManagerCode = dr["SalesManagerCode"].ToString().Trim();
                    string SalesManagerName = dr["SalesManagerName"].ToString().Trim();
                    string OrganizationCode = dr["OrganizationCode"].ToString().Trim();
                    string Status = dr["Status"].ToString().Trim();
                    if (Status.Equals(string.Empty)) Status = "0";
                    //WE SHOULD HAVE ORGANIZATIONCODE COMING WITH THE SALESMAN
                    //if (!SalesmanCode.Equals("00002208")) continue;
                    string OrganizationID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + OrganizationCode + "'", db_vms).Trim();
                    AddUpdateSalesperson(EmployeeType.Salesmanager, SalesManagerCode, SalesManagerName, "", OrganizationID, "", Status, SalesmanPhone, ref TOTALUPDATED, ref TOTALINSERTED);
                    AddUpdateSalesperson(EmployeeType.Supervisor, SupervisorCode, SuperVisorName, "", OrganizationID, SalesManagerCode, Status, SalesmanPhone, ref TOTALUPDATED, ref TOTALINSERTED);
                    AddUpdateSalesperson(EmployeeType.Salesman, SalesmanCode, SalesmanName, "", OrganizationID, SupervisorCode, Status, SalesmanPhone, ref TOTALUPDATED, ref TOTALINSERTED);

                }
                DT.Dispose();
                WriteMessage("\r\n");
                WriteMessage("<<< SALESPERSON >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
            }
            catch (Exception ex)
            {
                WriteExceptions("EXCEPTION HAPPENED IN THE UpdateSalesPerson() FUNCTION, THE MESSAGE IS <<" + ex.Message + ">>", "UPDATE REMAINING AMOUNT", false);
                //MessageBox.Show("Exception : " + ex.Message + "    Happened in Transaction :" + exceptionTransaction + "   Date: " + exceptionDate);
            }
        }
        private void AddUpdateSalesperson(EmployeeType employeeType, string EmployeeCode, string EmployeeName, string DivisionID, string OrganizationID, string ParentCode, string Status, string Phone, ref int TOTALUPDATED, ref int TOTALINSERTED)
        {
            InCubeErrors err;
            string EmployeeID = string.Empty;
            EmployeeID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode = '" + EmployeeCode + "'", db_vms).Trim();
            if (EmployeeID.Equals(string.Empty))// New Salesperon --- Insert Query
            {
                TOTALINSERTED++;
                string EmployeeTypeID = ((int)employeeType).ToString();
                EmployeeID = GetFieldValue("Employee", "isnull(MAX(EmployeeID),0) + 1", db_vms);

                QueryBuilderObject.SetField("EmployeeID", EmployeeID);

                QueryBuilderObject.SetField("EmployeeCode", "'" + EmployeeCode + "'");
                QueryBuilderObject.SetField("NationalIDNumber", "'0'");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.SetField("InActive", "0");
                QueryBuilderObject.SetField("OnHold", "0");
                QueryBuilderObject.SetField("EmployeeTypeID", EmployeeTypeID);
                QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                err = QueryBuilderObject.InsertQueryString("Employee", db_vms);

                QueryBuilderObject.SetField("EmployeeID", EmployeeID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + EmployeeName + "'");

                QueryBuilderObject.InsertQueryString("EmployeeLanguage", db_vms);

                err = ExistObject("Operator", "OperatorID", "OperatorID = " + EmployeeID, db_vms);
                if (err == InCubeErrors.DBNoMoreRows)
                {
                    QueryBuilderObject.SetField("OperatorID", EmployeeID);
                    QueryBuilderObject.SetField("OperatorName", "'" + EmployeeCode + "'");
                    QueryBuilderObject.SetField("FrontOffice", "1");
                    QueryBuilderObject.SetField("LoginTypeID", "1");
                    err = QueryBuilderObject.InsertQueryString("Operator", db_vms);
                }

                err = ExistObject("EmployeeOperator", "EmployeeID", "EmployeeID = " + EmployeeID, db_vms);
                if (err == InCubeErrors.DBNoMoreRows)
                {
                    QueryBuilderObject.SetField("EmployeeID", EmployeeID);
                    QueryBuilderObject.SetField("OperatorID", EmployeeID);
                    QueryBuilderObject.InsertQueryString("EmployeeOperator", db_vms);
                }
            }
            else
            {
                TOTALUPDATED++;
                QueryBuilderObject.SetField("Description", "'" + EmployeeName + "'");

                QueryBuilderObject.UpdateQueryString("EmployeeLanguage", "EmployeeID = " + EmployeeID, db_vms);
            }

            err = ExistObject("EmployeeDivision", "EmployeeID", "EmployeeID = " + EmployeeID + " AND DivisionID = " + DivisionID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("EmployeeID", EmployeeID);
                QueryBuilderObject.SetField("DivisionID", DivisionID);
                err = QueryBuilderObject.InsertQueryString("EmployeeDivision", db_vms);
            }

            err = ExistObject("EmployeeOrganization", "EmployeeID", "EmployeeID = " + EmployeeID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("EmployeeID", EmployeeID);
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                err = QueryBuilderObject.InsertQueryString("EmployeeOrganization", db_vms);
            }

            int AccountID = 1;

            err = ExistObject("AccountEmp", "AccountID", "EmployeeID = " + EmployeeID + " and AccountID in (select AccountID from Account where OrganizationID=" + OrganizationID + ")", db_vms);
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

                QueryBuilderObject.SetField("EmployeeID", EmployeeID);
                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                err = QueryBuilderObject.InsertQueryString("AccountEmp", db_vms);

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + EmployeeName.Trim() + " Account'");
                QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
            }
            if (ParentCode.Equals(string.Empty)) return;
            string ParentID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode='" + ParentCode + "'", db_vms).Trim();
            if (!ParentID.Equals(string.Empty))
            {
                if (employeeType == EmployeeType.Salesman)
                {
                    string existSMSUP = GetFieldValue("EmployeeSupervisor", "EmployeeID", "EmployeeID=" + EmployeeID + " and SupervisorID=" + ParentID + "", db_vms).Trim();
                    if (existSMSUP.Equals(string.Empty))
                    {
                        QueryBuilderObject.SetField("EmployeeID", EmployeeID);
                        QueryBuilderObject.SetField("SupervisorID", ParentID);
                        err = QueryBuilderObject.InsertQueryString("EmployeeSupervisor", db_vms);
                    }
                }
                else if (employeeType == EmployeeType.Supervisor)
                {
                    string existSMSUP = GetFieldValue("SupervisorSalesMngr", "SupervisorID", "SalesManagerID=" + ParentID + " and SupervisorID=" + EmployeeID + "", db_vms).Trim();
                    if (existSMSUP.Equals(string.Empty))
                    {
                        QueryBuilderObject.SetField("SalesManagerID", ParentID);
                        QueryBuilderObject.SetField("SupervisorID", EmployeeID);
                        err = QueryBuilderObject.InsertQueryString("SupervisorSalesMngr", db_vms);
                    }
                }
            }
        }

        public override void UpdateStock()
        {
            try
            {
                UpdateStockForWarehouse(Filters.StockDate);
                UpdateStock2();
            }
            catch
            {

            }
        }

        public void UpdateStock2()
        {
            //*** IMPORTANT IMPORTANT ***
            //NOTE SERIAS REQUIREMENT: WE NEED THE DIVISION CODE WITH THE MAIN WAREHOUSE STOCK

            int TOTALUPDATED = 0;
            object field = null;

            DataTable DT = GetMainWHStockTable();

            ClearProgress();
            SetProgressMax(DT.Rows.Count);
            //string MAIN_WH = GetFieldValue("WAREHOUSE", "WAREHOUSEID", "WAREHOUSECODE='1011'", db_vms).Trim();
            string DeleteStock = "";
            DeleteStock = "delete from WarehouseStock where warehouseid in (select WarehouseID from Warehouse where WarehouseTypeID=1)";
            err = QueryBuilderObject.RunQuery(DeleteStock, db_vms);

            foreach (DataRow dr in DT.Rows)
            {
                ReportProgress();
                string COMPANYCODE = dr["COMPANYCODE"].ToString().Trim();
                string DivisionCode = dr["DivisionCode"].ToString().Trim();
                string Plant = dr["Plant"].ToString().Trim();
                string StorageLocation = dr["StorageLocation"].ToString().Trim();
                string ItemCode = dr["ItemCode"].ToString().Trim();
                string ItemUOM = dr["ItemUOM"].ToString().Trim();
                string Denominator = dr["Denominator"].ToString().Trim();
                string Enumertaor = dr["Enumertaor"].ToString().Trim();
                string Batch = "1990/01/01";
                string Quantity = dr["Quantity"].ToString().Trim();
                string CWQuantity = dr["CWQuantity"].ToString().Trim();
                string OrganizationID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + COMPANYCODE + "'", db_vms).Trim();
                if (CWQuantity.Equals(string.Empty))
                    CWQuantity = "0";

                string CWMUOM = "";
                string CWMQty = "";
                if (decimal.Parse(CWQuantity) > 0)
                {
                    CWMUOM = ItemUOM;
                    ItemUOM = "KG";
                    CWMQty = Quantity;
                    Quantity = CWQuantity;
                }
                // string packQty = GetFieldValue("Pack", "Quantity", "ItemID in (select itemid from item where itemcode='" + ItemCode + "' and itemcategoryID in (select itemCategoryID from ItemCategory where divisionID in (select DivisionID from Division where DivisionCode='" + DivisionCode + "' and OrganizationID in (select OrganizationID from Organization where OrganizationCode='" + COMPANYCODE + "')))) and PackTypeID in ( select PackTypeID from PackTypeLanguage where Description ='" + ItemUOM + "')", db_vms).Trim();
                string ItemID = GetFieldValue("Item", "ItemID", "itemcode='" + ItemCode + "' and itemcategoryID in (select itemCategoryID from ItemCategory where divisionID in (select DivisionID from Division where OrganizationID in (select OrganizationID from Organization where OrganizationCode='" + COMPANYCODE + "')))", db_vms).Trim();
                string packQty = "";
                if (ItemID.Equals(string.Empty)) continue;
                string ExpiryDate = "1990/01/01";

                string _warehouseID = GetFieldValue("Warehouse", "WarehouseID", "WarehouseCode='" + Plant + "'", db_vms).Trim();

                if (_warehouseID.Equals(string.Empty))
                {
                    continue;
                }

                InCubeQuery PackQuery = new InCubeQuery(db_vms, string.Format(@"SELECT Pack.PackID, Pack.Quantity, Pack.PackTypeID,ptl.description FROM Pack  INNER JOIN
Item ON Pack.ItemID = Item.ItemID inner join packtype pt on Pack.packtypeid=pt.packtypeid
inner join packtypelanguage ptl on pt.packtypeid=ptl.packtypeid and ptl.languageid=1
INNER JOIN ITEMCATEGORY IC ON IC.ITEMCATEGORYID=ITEM.ITEMCATEGORYID
INNER JOIN DIVISION DIV ON DIV.DIVISIONID=IC.DIVISIONID
INNER JOIN ORGANIZATION O ON O.ORGANIZATIONID=DIV.ORGANIZATIONID
WHERE  O.ORGANIZATIONID=" + OrganizationID + @" AND ITEM.INACTIVE=0 AND
Item.ItemCode ='{0}' order by Pack.Quantity desc", ItemCode.Trim()));
                PackQuery.Execute();
                err = PackQuery.FindFirst();

                while (err == InCubeErrors.Success)
                {
                    TOTALUPDATED++;
                    PackQuery.GetField(0, ref field);
                    string PackID = field.ToString();

                    PackQuery.GetField(1, ref field);

                    decimal ConversionFactor = decimal.Parse(field.ToString());

                    PackQuery.GetField(3, ref field);
                    string tempUOM = field.ToString().Trim();
                    if (!tempUOM.Equals(ItemUOM.Trim()))
                    {
                        packQty = "0";
                    }
                    else
                    {
                        packQty = Quantity;
                    }
                    err = ExistObject("WarehouseStock", "PackID", "WarehouseID = " + _warehouseID + " AND ZoneID = 1 AND PackID = " + PackID + " AND ExpiryDate = '" + DateTime.Parse(ExpiryDate).ToString(DateFormat) + "' AND BatchNo = '" + Batch + "'", db_vms);
                    if (err != InCubeErrors.Success)
                    {
                        QueryBuilderObject.SetField("WarehouseID", _warehouseID);
                        QueryBuilderObject.SetField("ZoneID", "1");

                        QueryBuilderObject.SetField("PackID", PackID);

                        QueryBuilderObject.SetField("ExpiryDate", "'" + DateTime.Parse(ExpiryDate).ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("BatchNo", "'" + Batch + "'");
                        QueryBuilderObject.SetField("SampleQuantity", "0");
                        QueryBuilderObject.SetField("Quantity", packQty.ToString());
                        QueryBuilderObject.SetField("BaseQuantity", packQty.ToString());
                        err = QueryBuilderObject.InsertQueryString("WarehouseStock", db_vms);

                        QueryBuilderObject.SetField("WarehouseID", _warehouseID);
                        QueryBuilderObject.SetField("ZoneID", "1");
                        QueryBuilderObject.SetField("PackID", PackID);
                        QueryBuilderObject.SetField("ExpiryDate", "'" + DateTime.Parse(ExpiryDate).ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("StockDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("BatchNo", "'" + Batch + "'");
                        QueryBuilderObject.SetField("Quantity", packQty);
                        QueryBuilderObject.SetField("ProductionDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("SampleQuantity", "0");
                        err = QueryBuilderObject.InsertQueryString("DailyWarehouseStock", db_vms);
                    }
                    else
                    {
                        QueryBuilderObject.SetField("Quantity", Quantity);
                        QueryBuilderObject.UpdateQueryString("DailyWarehouseStock", "WarehouseID = " + _warehouseID + " AND ZoneID = 1 AND PackID = " + PackID + " AND ExpiryDate = '" + DateTime.Parse(ExpiryDate).ToString(DateFormat) + "' AND BatchNo = '" + Batch + "'", db_vms);
                    }

                    err = PackQuery.FindNext();
                    if (err == InCubeErrors.Success)
                    {
                    }
                }
            }

            DT.Dispose();
            WriteMessage("\r\n");
            WriteMessage("<<< STOCK Updated >>> Total Updated = " + TOTALUPDATED);
        }

        #region UpdateStock

        public void UpdateVehicleStock(bool UpdateAll, string WarehouseID, bool CaseChecked, DateTime StockDate)
        {
            UpdateStockForWarehouse(StockDate);
        }

        private void UpdateStockForWarehouse(DateTime stockDate)
        {
            try
            {
                int TOTALUPDATED = 0;
                List<string> DownloadedVehicles = new List<string>();
                object field = new object();
                #region Update Stock
                List<string> cancelledTrx = new List<string>();
                DataTable DTBL = new DataTable();
                InCubeQuery qry;
                List<string> transactionsList = new List<string>();
                DTBL = GetApprovedLoad(ref transactionsList);
                WriteExceptions("the number of items are " + DTBL.Rows.Count.ToString() + "", "Number of Items", false);

                #region Stock Update
                //foreach (DataRow row in DTBL.Rows)
                //{
                //    string COMPANYCODE = row["COMPANYCODE"].ToString().Trim();
                //    string TransactionID = row["TransactionID"].ToString().Trim();
                //    string SAP_REF_NUM = row["SAP_REF_NUM"].ToString().Trim();
                //    string LocationFrom = row["LocationFrom"].ToString().Trim();
                //    string LocationTo = row["LocationTo"].ToString().Trim();
                //    string TransactionType = row["TransactionType"].ToString().Trim();
                //    string DivisionCode = row["DivisionCode"].ToString().Trim();
                //    string Date = row["Date"].ToString().Trim();
                //    string Status = row["Status"].ToString().Trim();
                //    string ItemCode = row["ItemCode"].ToString().Trim();
                //    string UOM = row["UOM"].ToString().Trim();
                //    string ConversionFactor = row["ConversionFactor"].ToString().Trim();
                //    string Qty1 = row["Qty1"].ToString().Trim();
                //    string Qty2 = row["Qty2"].ToString().Trim();
                //   if(Qty2.Equals(string.Empty))
                //    Qty2 = "0";
                //   if (TransactionID.Equals(string.Empty)&&SAP_REF_NUM.Equals(string.Empty)) continue;
                //   string CWMUOM = "";
                //    string CWMQty="";
                //   if (decimal.Parse(Qty2) > 0)
                //   {
                //       CWMUOM = UOM;
                //       UOM = "KG";
                //       CWMQty=Qty1;
                //       Qty1 = Qty2;
                //   }
                //    string Batch = row["Batch"].ToString().Trim();
                //    string Expiry = row["Expiry"].ToString().Trim();
                //    //if (DivisionCode.Trim().ToLower().Equals("40")) continue;
                //    WriteExceptions("ENTERING STOCK LOOP", "STOCK", false);
                //    ReportProgress()++;
                //    IntegrationForm.lblProgress.Text = "Updating Stock" + " " + ReportProgress() + " / " + IntegrationForm.progressBar1.Maximum;
                //    ();
                //    string OrganizationID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + COMPANYCODE + "'", db_vms).Trim();
                //    if (cancelledTrx.Contains(TransactionID))
                //    {
                //        WriteExceptions("TRANSACTION CANCELLED, CONTINUE", "STOCK", false);
                //        continue;
                //    }
                //    string TransactionDate = Date;
                //    WriteExceptions("TRANSACTIONID IS " + TransactionID + "", "STOCK", false);
                //    //string TransactionType = row["TransactionType"].ToString().Trim();
                //    string vehicleCode = row["LocationTo"].ToString().Trim();
                //    string WarehouseCode = row["LocationFrom"].ToString().Trim();
                //    string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionID in (select DivisionID from EmployeeDivision where EmployeeID in (select employeeid from employeevehicle where vehicleid in (select warehouseid from warehouse where WarehouseCode='" + LocationTo + "')))", db_vms).Trim();
                //    if (DivisionID.Equals(string.Empty)) continue;
                //    DivisionCode = GetFieldValue("Division", "DivisionCode", "DivisionID=" + DivisionID + "", db_vms).Trim();
                //    if (ItemCode.Equals("4647.0002") && vehicleCode.Equals("22252"))
                //    {

                //    }
                //    string PackType = UOM;// row["UOM"].ToString().Trim();
                //    string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + PackType + "'", db_vms).Trim();
                //    //string conversionFactor = row["ConversionFactor"].ToString().Trim();
                //    string Quantity = Qty1;// row["Qty1"].ToString().Trim();

                //    //string DivisionCode = row["DivisionCode"].ToString().Trim();
                //    //string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode='" + DivisionCode + "'", db_vms).Trim();
                //    if (DivisionID.Equals(string.Empty)) continue;
                //    if (Batch.Equals(string.Empty)) Batch = "1990/01/01";
                //    //string TransactionStatus = row["Status"].ToString().Trim();
                //    string expiry = row["Expiry"].ToString().Trim();// DateTime.Parse("01/01/1990 00:00:00").ToString(StockDateFormat);
                //    WriteExceptions("GETTING THE IDS", "STOCK", false);

                //    string vehicleID = GetFieldValue("warehouse", "WarehouseID", "WarehouseCode='" + vehicleCode + "'", db_vms).Trim();
                //    if (vehicleID.Equals(string.Empty)) continue;
                //    string WarehouseID = GetFieldValue("warehouse", "WarehouseID", "Barcode='" + WarehouseCode + "'", db_vms).Trim();
                //    if (WarehouseID.Equals(string.Empty)) continue;
                //    string EmployeeID = GetFieldValue("EmployeeVehicle", "EmployeeID", "VehicleID=" + vehicleID + "", db_vms).Trim();
                //    if (EmployeeID.Equals(string.Empty)) continue;
                //    string ref_no = SAP_REF_NUM;
                //    string ItemID = GetFieldValue("Item", "ItemID", "ItemCode='" + ItemCode.Trim() + "' and ItemCategoryID in (select ItemCategoryID from ItemCategory where DivisionID in (select DivisionID from Division where DivisionID=" + DivisionID + " and OrganizationID=" + OrganizationID + ")) ", db_vms).Trim();
                //    if (ItemID.Equals(string.Empty))
                //    {
                //        ExtendItem(ItemCode, DivisionCode, OrganizationID);
                //        ItemID = GetFieldValue("Item", "ItemID", "ItemCode='" + ItemCode.Trim() + "' and ItemCategoryID in (select ItemCategoryID from ItemCategory where DivisionID in (select DivisionID from Division where DivisionID=" + DivisionID + " and OrganizationID=" + OrganizationID + ")) ", db_vms).Trim();
                //        if (ItemID.Equals(string.Empty)) continue;
                //    }
                //    string PackID = GetFieldValue("Pack", "PackID", "ItemID=" + ItemID + " and packTypeID=" + PackTypeID + "", db_vms).Trim();
                //    if (PackID.Equals(string.Empty)) continue;
                //    if (TransactionType.Equals("2"))
                //    {
                //        //THIS IS AN OFFLOAD TRANSACTION.
                //        Quantity = "-" + Quantity;
                //    }


                //    string UOMdesc = string.Empty;// row[2].ToString().Trim();
                //    string Expirydate = row["Expiry"].ToString().Trim();// DateTime.Parse("01/01/1990 00:00:00").ToString(StockDateFormat);
                //    if (Expirydate.Equals(string.Empty) || Expirydate.Contains("0000"))
                //        Expirydate = "1990/01/01";

                //    Expirydate = DateTime.Parse(Expirydate).ToString(StockDateFormat);
                //    if (WarehouseCode == string.Empty)
                //        continue;
                //    WriteExceptions("ATTEMPTING TO CHECK ROUTE HISTORY STATUS", "STOCK", false);
                //    //field = new object();
                //    //string CheckUploaded = string.Format("select top(1)uploaded,deviceserial from RouteHistory where vehicleid=" + vehicleID + " ORDER BY RouteHistoryID Desc ");
                //    //qry = new InCubeQuery(CheckUploaded, db_vms);
                //    //err = qry.Execute();
                //    //err = qry.FindFirst();
                //    //err = qry.GetField("uploaded", ref field);
                //    //string uploaded = field.ToString().Trim();
                //    //err = qry.GetField("deviceserial", ref field);
                //    //string deviceserial = field.ToString().Trim();
                //    ////if (uploaded.ToString().Trim().Equals(string.Empty) || uploaded.ToString().Trim().Equals("System.Object")) continue;
                //    //if (!uploaded.ToString().Trim().Equals(string.Empty) && !uploaded.ToString().Trim().Equals("System.Object"))
                //    //{
                //    //    if (Convert.ToBoolean(uploaded.ToString().Trim()))
                //    //    {
                //    //        WriteMessage("\r\n");
                //    //        WriteMessage("<<< The Route " + vehicleCode + " is not downloaded . No stock will be added .>>> Total Updated = " + TOTALUPDATED);
                //    //        WriteExceptions("Route " + vehicleCode + " was not downloaded ---- transaction = " + TransactionID + "", "Device not downloaded", true);
                //    //        continue;
                //    //    }

                //    //}
                //    WriteExceptions("Route " + vehicleCode + " is downloaded ...", "Device is downloaded", false);

                //    //string updateReady = string.Format("update readyDevice set Ready=0,ReadyDate='{1}' where Routecode='{0}'", vehicleCode, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                //    //qry = new InCubeQuery(updateReady, db_vms);
                //    //err = qry.ExecuteNonQuery();

                //    GPerror = InCubeErrors.Error;
                //    TOTALUPDATED++;
                //    //DownloadedVehicles.Add(vehicleCode);
                //}
                #endregion

                bool TransactionHeaderUpdated = false;
                ClearProgress();
                SetProgressMax(transactionsList.Count);
                foreach (string tranStr in transactionsList)
                {
                    if (tranStr.Equals("W25-10470172"))
                    {

                    }
                    TransactionHeaderUpdated = false;
                    #region Combine Procedure
                    /*
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
      ,'1990/01/01'
      ,Sum(isnull(Quantity,0))
      ,[Balanced]
      ,'1990/01/01'
      ,[ProductionDate]
      ,[PackStatusID]
      ,[DivisionID]
      ,0
      ,Sum(isnull([ApprovedQuantity],0))
      ,Sum(isnull([RequestedQuantity],0))
      ,[ExistingQty]
      ,[SecondaryPackTypeID]
      ,[SecondaryQuantity]
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
      ,[ExistingQty]
      ,[SecondaryPackTypeID]
      ,[SecondaryQuantity]
delete from Whtransdetail where transactionid=@TransactionID
insert into WhTransDetail select * from @Details
END
GO
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
                        ReportProgress();
                        WriteMessage("\r\n");
                        WriteMessage("Transaction <<" + tranStr + ">> Update Started..");

                        #region Warehouse Transaction
                        Dictionary<string, string> transactionList = new Dictionary<string, string>();
                        string previousTransaction = string.Empty;
                        foreach (DataRow row in DTBL.Select("TransactionID='" + tranStr + "' or SAP_REF_NUM='" + tranStr + "'"))
                        {
                            #region Trigger
                            /* THE FOLLOWING TRIGGER IS TO HABDLE THE CASE WHERE A LOAD WITH STATUS 8 IS INSERTED OR UPDATED, BUT THEN THE SALESMAN DOWNLOADED THE DEVICE WITHOUT GETTING THE SECOND LOAD.
                             * ALTER TRIGGER [dbo].[TRG_WH_TRANS_STATUS]  
           ON [dbo].[RouteHistory]  
           AFTER update  
        AS  

        DECLARE @IS_NEW int ,@ROUTE_HISTORY_STATUS_ID int ,@VEHICLE_ID INT,@EmployeeID int ;
        SELECT @ROUTE_HISTORY_STATUS_ID=ROUTEHISTORYSTATUSID FROM INSERTED ;
        SELECT @VEHICLE_ID=VEHICLEID FROM INSERTED 
        SELECT @EmployeeID=EmployeeID FROM INSERTED 
        IF UPDATE(ROUTEHISTORYSTATUSID)
        BEGIN
        if @ROUTE_HISTORY_STATUS_ID=6  
        begin  
        BEGIN TRANSACTION T1  
        UPDATE WAREHOUSETRANSACTION SET WarehouseTransactionStatusID=4 WHERE warehouseid=@VEHICLE_ID and WarehouseTransactionStatusID=8 and LoadDate is null
        DELETE FROM QNIE_TransactionSendingPool WHERE EMPLOYEEID=@EmployeeID
        --TRANSACTIONID IN 
        --(SELECT DISTINCT INVANORDERNUMBER FROM ZAPPROVEDLOAD WHERE FLAG='Y') AND WarehouseTransactionStatusID=2 AND WarehouseID=@VEHICLE_ID
        COMMIT TRANSACTION T1  
        end
                            */
                            #endregion
                            string COMPANYCODE = row["COMPANYCODE"].ToString().Trim();
                            string TransactionID = row["TransactionID"].ToString().Trim();
                            string SAP_REF_NUM = row["SAP_REF_NUM"].ToString().Trim();
                            string LocationFrom = row["LocationFrom"].ToString().Trim();
                            string LocationTo = row["LocationTo"].ToString().Trim();
                            //if (LocationTo.Equals("1131V253")) continue;
                            string TransactionType = row["TransactionType"].ToString().Trim();
                            string DivisionCode = row["DivisionCode"].ToString().Trim();
                            string Date = row["Date"].ToString().Trim();
                            string Status = row["Status"].ToString().Trim();
                            string ItemCode = row["ItemCode"].ToString().Trim();
                            string UOM = row["UOM"].ToString().Trim();
                            string ConversionFactor = row["ConversionFactor"].ToString().Trim();
                            string Qty1 = row["Qty1"].ToString().Trim();
                            string Qty2 = row["Qty2"].ToString().Trim();
                            string Batch = row["Batch"].ToString().Trim();
                            string Expiry = row["Expiry"].ToString().Trim();
                            if (TransactionID.Equals(string.Empty) && SAP_REF_NUM.Equals(string.Empty)) continue;
                            string OrganizationID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + COMPANYCODE + "'", db_vms, tran).Trim();
                            if (TransactionID.Trim().Equals(string.Empty))
                                TransactionID = SAP_REF_NUM;
                            WriteExceptions("ENTERING STOCK LOOP", "STOCK", false);
                            OrganizationID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + COMPANYCODE + "'", db_vms, tran).Trim();
                            if (cancelledTrx.Contains(TransactionID))
                            {
                                WriteExceptions("TRANSACTION CANCELLED, CONTINUE", "STOCK", false);
                                continue;
                            }
                            if (Qty2.Equals(string.Empty))
                                Qty2 = "0";
                            string CWMUOM = "";
                            string CWMQty = "";
                            bool isCWM = false;
                            string CWMPackTypeID = string.Empty;
                            if (decimal.Parse(Qty2) > 0)
                            {
                                isCWM = true;
                                CWMUOM = UOM;
                                UOM = "KG";
                                CWMQty = Qty1;
                                Qty1 = Qty2;
                                CWMPackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + CWMUOM + "'", db_vms, tran).Trim();
                            }
                            string TransactionDate = Date;
                            WriteExceptions("TRANSACTIONID IS " + TransactionID + "", "STOCK", false);
                            string vehicleCode = row["LocationTo"].ToString().Trim();
                            string WarehouseCode = row["LocationFrom"].ToString().Trim();
                            //vehicleCode = "1131V152"; WarehouseCode = "1102"; LocationTo = "1131V152"; LocationFrom = "1102";

                            string PackType = UOM;// row["UOM"].ToString().Trim();
                            string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + PackType + "'", db_vms, tran).Trim();
                            string Quantity = Qty1;// row["Qty1"].ToString().Trim();
                            string StkQty = Quantity;
                            if (Batch.Equals(string.Empty)) Batch = "1990/01/01";
                            string originalbatch = string.Empty;
                            string ERP_TRAN_TO_UPDATE = string.Empty;
                            WriteExceptions("FILLING PARAMETERS", "Inserting Stock ", true);
                            if (cancelledTrx.Contains(TransactionID)) { continue; }
                            if (TransactionType.Equals("0")) { TransactionType = "1"; } else if (TransactionType.Equals("1")) { TransactionType = "2"; StkQty = "-" + StkQty; }// row["TransactionType"].ToString().Trim();
                            string Tran_To_Update = TransactionID;
                            originalbatch = Batch;
                            if (!originalbatch.Equals(string.Empty)) originalbatch = "and LotNumber ='" + Batch + "'";
                            if (Batch.Equals(string.Empty)) Batch = "1990/01/01";
                            string expiry = row["Expiry"].ToString().Trim();
                            if (expiry.Equals(string.Empty) || expiry.Contains("0000"))
                                expiry = "1990/01/01";
                            expiry = DateTime.Parse(expiry).ToString(StockDateFormat);

                            string vehicleID = string.Empty;
                            string WarehouseID = string.Empty;
                            string DivisionID = string.Empty;
                            if (TransactionType.Equals("2"))
                            {
                                vehicleID = GetFieldValue("WarehouseTransaction", "WarehouseID", "TransactionID='" + TransactionID + "'", db_vms, tran).Trim();
                                WarehouseID = GetFieldValue("WarehouseTransaction", "RefWarehouseID", "TransactionID='" + TransactionID + "'", db_vms, tran).Trim();
                                DivisionID = GetFieldValue("WarehouseTransaction", "DivisionID", "TransactionID='" + TransactionID + "'", db_vms, tran).Trim();
                            }
                            else
                            {
                                vehicleID = GetFieldValue("warehouse", "WarehouseID", "WarehouseCode='" + vehicleCode + "'", db_vms, tran).Trim();
                                WarehouseID = GetFieldValue("warehouse", "WarehouseID", "Barcode='" + WarehouseCode + "'", db_vms, tran).Trim();
                                if (GetFieldValue("WarehouseTransaction", "TransactionID", "TransactionID='" + TransactionID + "'", db_vms, tran).Trim().Equals(string.Empty))
                                {
                                    DivisionID = GetFieldValue("Division", "DivisionID", "DivisionID in (select DivisionID from EmployeeDivision where EmployeeID in (select employeeid from employeevehicle where vehicleid =" + vehicleID + "))", db_vms, tran).Trim();
                                }
                                else
                                {
                                    DivisionID = GetFieldValue("WarehouseTransaction", "DivisionID", "TransactionID='" + TransactionID + "'", db_vms, tran).Trim();
                                }
                            }
                            if (vehicleID.Equals(string.Empty)) continue;
                            if (WarehouseID.Equals(string.Empty)) continue;
                            if (DivisionID.Equals(string.Empty)) continue;
                            DivisionCode = GetFieldValue("Division", "DivisionCode", "DivisionID=" + DivisionID + "", db_vms, tran).Trim();
                            string ItemID = GetFieldValue("Item", "ItemID", "ItemCode='" + ItemCode.Trim() + "' and ItemCategoryID in (select ItemCategoryID from ItemCategory where DivisionID in (select DivisionID from Division where DivisionID=" + DivisionID + " and OrganizationID=" + OrganizationID + ")) ", db_vms, tran).Trim();
                            if (ItemID.Equals(string.Empty))
                            {
                                ExtendItem(ItemCode, DivisionCode, OrganizationID, ref tran);
                                ItemID = GetFieldValue("Item", "ItemID", "ItemCode='" + ItemCode.Trim() + "' and ItemCategoryID in (select ItemCategoryID from ItemCategory where DivisionID in (select DivisionID from Division where DivisionID=" + DivisionID + " and OrganizationID=" + OrganizationID + ")) ", db_vms, tran).Trim();
                                if (ItemID.Equals(string.Empty)) continue;
                            }
                            string PackID = GetFieldValue("Pack", "PackID", "ItemID=" + ItemID + " and packTypeID=" + PackTypeID + "", db_vms, tran).Trim();
                            if (PackID.Equals(string.Empty)) continue;
                            string EmployeeID = GetFieldValue("EmployeeVehicle", "EmployeeID", "VehicleID=" + vehicleID + "", db_vms, tran).Trim();
                            if (EmployeeID.Equals(string.Empty)) continue;
                            string ref_no = SAP_REF_NUM;
                            WriteExceptions("PARAMETERS ARE FILLED, REF NO IS " + ref_no + "", "Inserting Stock ", true);
                            WriteExceptions("CHECKING IF VEHICLE ID " + vehicleID + "  IS DOWNLOADED", "Inserting Stock ", true);
                            #region Check Route Downloaded
                            //string CheckUploaded = string.Format("select top(1)uploaded,deviceserial from RouteHistory where vehicleid=" + vehicleID + " ORDER BY RouteHistoryID Desc");
                            //qry = new InCubeQuery(CheckUploaded, db_vms);
                            //err = qry.Execute();
                            //err = qry.FindFirst();
                            //err = qry.GetField("uploaded", ref field);
                            //WriteExceptions("THE UPLOADED FLAG=" + field.ToString() + "", "Inserting STOCK ", false);
                            //string uploaded = field.ToString().Trim();
                            //err = qry.GetField("deviceserial", ref field);
                            //string deviceserial = field.ToString().Trim();
                            //if (!uploaded.ToString().Trim().Equals(string.Empty) && !uploaded.ToString().Trim().Equals("System.Object"))
                            //{
                            //    //if (Convert.ToBoolean(uploaded.ToString().Trim()))
                            //    //{
                            //    //    WriteMessage("\r\n");
                            //    //    WriteMessage("<<< The Route " + vehicleCode + "  is not downloaded . No stock will be added .>>> Total Updated = " + TOTALUPDATED);
                            //    //    //continue;
                            //    //}

                            //}
                            #endregion
                            WriteExceptions("CHECKING THE VEHICLES LIST <<LIST COUNT=" + DownloadedVehicles.Count + ">>", "Inserting STOCK ", false);
                            if (PackID.Equals("14100"))
                            {

                            }
                            if (!TransactionHeaderUpdated)
                            {
                                QueryBuilderObject.SetField("Balanced", "1");
                                err = QueryBuilderObject.UpdateQueryString("WhTransDetail", "TransactionID='" + TransactionID + "'", db_vms, tran);
                                //if (err != InCubeErrors.Success) throw new Exception("Error");

                                string UploadStatus = GetFieldValue("RouteHistory", "Top(1) Uploaded", " VehicleID=" + vehicleID + " order by RouteHistoryID Desc", db_vms, tran).Trim();
                                if (UploadStatus.Equals(string.Empty)) UploadStatus = "0";
                                if (UploadStatus.ToLower().Equals("false")) { UploadStatus = "0"; } else if (UploadStatus.ToLower().Equals("true")) { UploadStatus = "1"; }
                                //FOLLOWING LINE IS WHEN IT IS AN OFFLOAD AND THE DEVICE IS UPLOADED.
                                // if (UploadStatus.Equals("1") && TransactionType.Equals("2")) { tran.Rollback(); break; }
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
                                    QueryBuilderObject.SetField("ProductionDate", "'" + DateTime.Today.ToString(StockDateFormat) + "'");
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
                                        WriteMessage("\r\n");
                                        WriteMessage("Transaction <<" + TransactionID + ">> Failed.");
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
                                    }
                                    else
                                    {
                                        if (TransactionType.Equals("2")) { QueryBuilderObject.SetField("WarehouseTransactionStatusID", "4"); }
                                        else { QueryBuilderObject.SetField("WarehouseTransactionStatusID", "8"); }
                                    }
                                    QueryBuilderObject.SetField("Posted", "1");
                                    QueryBuilderObject.SetField("Synchronized", "0");
                                    err = QueryBuilderObject.UpdateQueryString("warehouseTransaction", "TransactionID='" + TransactionID + "' and WarehouseTransactionStatusID<>5", db_vms, tran);
                                    if (err == InCubeErrors.Success)
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
                            string existDetail = GetFieldValue("WhTransDetail", "TransactionID", "TransactionID='" + TransactionID + "' and WarehouseID=" + vehicleID + " and PackID=" + PackID + " and BatchNo='" + Batch + "' ", db_vms, tran).Trim();
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
                                if (isCWM)
                                {
                                    QueryBuilderObject.SetField("SecondaryPackTypeID", CWMPackTypeID);
                                    QueryBuilderObject.SetField("SecondaryQuantity", CWMQty);
                                }
                                err = QueryBuilderObject.InsertQueryString("WhTransDetail", db_vms, tran);
                                if (err != InCubeErrors.Success) { throw new Exception("Error"); }
                                err = UpdateQNIEVehicleStock(ref tran, ItemID, vehicleID, expiry, Batch, PackID, StkQty);
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
                                err = QueryBuilderObject.UpdateQueryString("WhTransDetail", "TransactionID='" + TransactionID + "' and WarehouseID=" + vehicleID + " and PackID=" + PackID + " and BatchNo='" + Batch + "'", db_vms, tran);
                                if (err != InCubeErrors.Success) { throw new Exception("Error"); }
                                err = UpdateQNIEVehicleStock(ref tran, ItemID, vehicleID, expiry, Batch, PackID, StkQty);
                                if (err != InCubeErrors.Success) { throw new Exception("Error"); }
                            }
                            TOTALUPDATED++;
                            WriteExceptions("END OF  transaction = " + TransactionID + "", "Inserting WH Transaction ", true);
                        }

                        #endregion
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
                            WriteMessage("\r\n");
                            WriteMessage("Transaction <<" + tranStr + ">> Successful.");
                        }
                    }
                }

                DTBL.Dispose();
                WriteMessage("\r\n");
                WriteMessage("<<< GATE PASS Updated >>> Total Updated = " + TOTALUPDATED);
                #endregion
            }
            catch (Exception ex)
            {
                WriteExceptions("HANDLED EXCEPTION <<" + ex.Message + ">>", "Inserting WH Transaction ", true);
            }
        }

        private InCubeErrors UpdateQNIEVehicleStock(ref InCubeTransaction tran, string ItemID, string vehicleID, string Expirydate, string Batch, string PackID, string Quantity)
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
                        err = QueryBuilderObject.UpdateQueryString("WarehouseStock", "WarehouseID = " + vehicleID + " AND ZoneID = 1 AND PackID = " + PackID + " AND BatchNo = '" + Batch + "'", db_vms, tran);
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

        public override void UpdateWarehouse()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;


            DataTable DT = GetWarehouseTable();

            ClearProgress();
            SetProgressMax(DT.Rows.Count);

            foreach (DataRow dr in DT.Rows)
            {
                ReportProgress("Updating Warehouses ");

                string COMPANYCODE = dr["COMPANYCODE"].ToString().Trim();
                string VehicleLocationCode = dr["VehicleLocationCode"].ToString().Trim();
                string LoadingWarehouse = dr["LoadingWarehouse"].ToString().Trim();
                string Flag = dr["Flag"].ToString().Trim();

                string OrganizationID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + COMPANYCODE + "'", db_vms).Trim();
                string CreationDate = DateTime.Now.ToString();// dr[2].ToString();
                string ModDate = DateTime.Now.ToString();//dr[3].ToString();
                AddUpdateWarehouse("1", LoadingWarehouse, LoadingWarehouse, "", OrganizationID, ref TOTALUPDATED, ref TOTALINSERTED);
                AddUpdateWarehouse("2", VehicleLocationCode, VehicleLocationCode, LoadingWarehouse, OrganizationID, ref TOTALUPDATED, ref TOTALINSERTED);
            }

            DT.Dispose();
            WriteMessage("\r\n");
            WriteMessage("<<< WAREHOUSE >>> Total Inserted = " + TOTALINSERTED);
        }
        private void AddUpdateWarehouse(string warehouseType, string WarehouseCode, string WarehouseName, string ParentCode, string OrganizationID, ref int TOTALUPDATED, ref int TOTALINSERTED)
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
                QueryBuilderObject.SetField("Description", "'" + WarehouseName + "'");
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
                QueryBuilderObject.SetField("Description", "'" + WarehouseName + "'");
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
                QueryBuilderObject.SetField("Description", "'" + WarehouseName + " Zone'");
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
                string MainWH = GetFieldValue("Warehouse", "WarehouseID", "WarehouseCode =" + ParentCode + " ", db_vms).Trim();
                if (!MainWH.Equals(string.Empty)) // Exist Warehouse --- Update Query
                {
                    string LoadingWH = GetFieldValue("VehicleLoadingWh", "VehicleID", "vehicleID =" + vehID + " and WarehouseID=" + MainWH + "", db_vms).Trim();
                    if (LoadingWH.Equals(string.Empty)) // Exist Warehouse --- Update Query
                    {
                        QueryBuilderObject.SetField("VehicleID", WarehouseID);
                        QueryBuilderObject.SetField("WarehouseID", MainWH);
                        err = QueryBuilderObject.InsertQueryString("VehicleLoadingWh", db_vms);
                    }
                }
            }



            #endregion
        }

        private void AccounteRecursiveFunction(int accountID, decimal balance, bool IsIncrease, string PayerCode)
        {
            try
            {
                string update = string.Empty;
                //if (accountID == 0) return;
                //if (IsIncrease)
                //{
                //    update = string.Format("update Account set Balance =Balance+" + balance.ToString() + " where AccountID=" + accountID + "");
                //}
                //else
                //{
                //    update = string.Format("update Account set Balance =Balance-" + balance.ToString() + " where AccountID=" + accountID + "");
                //}
                //InCubeQuery qry = new InCubeQuery(update, db_vms);
                //err = qry.ExecuteNonQuery();
                //int parentAccount = 0;
                //parentAccount = int.Parse(GetFieldValue("Account", "isnull(ParentAccountID,0)", "AccountID=" + accountID + "", db_vms).Trim());
                //if (!parentAccount.Equals(0))
                //{
                //    AccounteRecursiveFunction(parentAccount, balance, IsIncrease, PayerCode);
                //}
                //else
                //{ 
                //HERE WE NEED TO UPDATE THE PAYER ACCOUNT BALANCE.
                if (IsIncrease)
                {
                    update = string.Format("update Account set Balance =Balance+" + balance.ToString() + " where AccountID in (select accountid from AccountPayer where PayerID in (select PayerID from Payer where PayerCode='" + PayerCode + "'))");
                }
                else
                {
                    update = string.Format("update Account set Balance =Balance-" + balance.ToString() + " where AccountID in (select accountid from AccountPayer where PayerID in (select PayerID from Payer where PayerCode='" + PayerCode + "'))");
                }
                InCubeQuery qry = new InCubeQuery(update, db_vms);
                err = qry.ExecuteNonQuery();
                //}

            }
            catch
            {
            }
        }

        private DataTable BalanceBase()
        {
            DataTable DT = new DataTable();
            try
            {
                InCubeQuery tempQry = new InCubeQuery("update account set balance=0", db_vms);
                err = tempQry.ExecuteNonQuery();

                string tempquery = @"select t.transactiontypeid,t.transactionID,t.transactiondate,t.EmployeeID,t.CustomerID,t.outletID,t.nettotal,t.remainingamount,
t.divisionID,ad.accountid
from [transaction] t inner join customeroutlet co on t.customerid=co.customerid and t.outletid=co.outletid
inner join customer c on t.customerid=c.customerid
inner join employee e on t.employeeid=e.employeeid
inner join division d on t.divisionid=d.divisionid
inner join AccountCustOut ad on t.CustomerID=ad.customerid and t.outletid=ad.outletid
where t. transactiontypeid in (1,3,5,6) and t.voided<>1 and t.remainingamount>0 ";//and t.customerid=8855
                tempQry = new InCubeQuery(tempquery, db_vms);
                err = tempQry.Execute();
                DT = tempQry.GetDataTable();
                string exceptionTransaction = string.Empty;
                string exceptionDate = string.Empty;
                ClearProgress();
                SetProgressMax(DT.Rows.Count);
                foreach (DataRow dr in DT.Rows)
                {
                    ReportProgress("BUILDING BALANCE");
                    string TranType = dr[0].ToString().Trim();
                    string TranID = dr[1].ToString().Trim();
                    string TranDate = dr[2].ToString().Trim();
                    string SalespersonID = dr[3].ToString().Trim();
                    //WriteExceptions(TranID, "INVOICE OUTSTANDING", false);
                    string CustomerID = dr[4].ToString().Trim();
                    string OutletID = dr[5].ToString().Trim();
                    //continue;
                    string TranAmount = dr[6].ToString().Trim();
                    string Balance = decimal.Parse(dr[7].ToString().Trim()).ToString();
                    if (decimal.Parse(Balance) < 0) continue;
                    string DivisionID = dr[8].ToString().Trim();
                    string AccountID = dr[9].ToString().Trim();

                    if (CustomerID.Equals(string.Empty))
                    {
                        WriteExceptions("TransactionID:" + TranID + " has no Customer", "TransactionID:" + TranID + "", false);
                        continue;
                    }

                    if (OutletID.Equals(string.Empty))
                    {
                        continue;
                    }

                    if (SalespersonID.Equals(string.Empty))
                    {
                        WriteExceptions("TransactionID:" + TranID + " has no salesman", "TransactionID:" + TranID + "", false);
                        continue;
                    }

                    if (DivisionID.Equals(string.Empty))
                    {
                        WriteExceptions("TransactionID:" + TranID + " has no division", "TransactionID:" + TranID + "", false);
                        continue;
                    }

                    //string AccountID = GetFieldValue("AccountCustOutDivEmp", "AccountID", "CustomerID=" + CustomerID + " and OutletID=" + OutletID + " and DivisionID=" + DivisionID + " and EmployeeID=" + SalespersonID + "", db_vms).Trim();
                    if (AccountID.Equals(string.Empty))
                    {
                        WriteExceptions("TransactionID:" + TranID + " has no Account", "TransactionID:" + TranID + "", false);
                        continue;
                    }
                    decimal newBal = decimal.Parse(Balance);
                    if (TranType.Equals("5")) newBal = newBal * -1;

                    if (err == InCubeErrors.Success)
                    {
                        //if (newBal >= oldTranBal)
                        //{
                        //    AccounteRecursiveFunction(int.Parse(AccountID), newBal - oldTranBal, true);
                        //}
                        //else
                        //{
                        //    AccounteRecursiveFunction(int.Parse(AccountID), oldTranBal - newBal, false);
                        //}
                    }
                }
                DT.Dispose();
                WriteMessage("\r\n");
                WriteMessage("<<< BALANCE BUILT SUCCESSFULLY >>>");
            }
            catch
            {
            }
            return DT;
        }

        private void CreateCreditNote(string GRV, string EmployeeID)
        {
            string CreditNoteID = GetMaxCreditNoteID(EmployeeID);
            int _rowaffected = 0;

            if (CreditNoteID != string.Empty)
            {
                string Query = @"Insert into [Transaction]
SELECT
CustomerID, OutletID, '" + CreditNoteID + @"', EmployeeID, TransactionDate, 5, DiscountAuthorization, Discount, Signature, Synchronized, RemainingAmount,
'" + GRV + @"', GrossTotal, GPSLatitude, GPSLongitude, Voided, Notes, TransactionStatusID, RouteID, NetTotal, Tax, Posted, CurrencyID, WarehouseID,
CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, Downloaded,VisitNo, RouteHistoryID, AccountID, PromotedDiscount, DivisionID, LPONumber
FROM [Transaction] Where TransactionID = '" + GRV + "' AND TransactionTypeID = 2";

                InCubeQuery CMD = new InCubeQuery(Query, db_vms);
                CMD.ExecuteNonQuery(ref _rowaffected);

                if (_rowaffected > 0)
                {
                    Query = @"UPDATE DocumentSequence SET MaxTransactionCreditNote = '" + CreditNoteID + "' WHERE EmployeeID = " + EmployeeID;
                    CMD = new InCubeQuery(Query, db_vms);
                    CMD.ExecuteNonQuery();
                }
            }
        }

        private DataTable Exceptions(string field)
        {
            ///// Passing a table from SAP to .Net //////
            RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
            RfcRepository repo = prd.Repository;
            IRfcFunction companyBapi = repo.CreateFunction("ZSD_FM_PDT_" + field);
            // Passing the Parameter value
            companyBapi.SetValue("CR_DATE", _modificationDate.ToString(DateFormtSAP));
            //companyBapi.SetValue("MOD_DATE", _modificationDate.ToString(DateFormtSAP));
            companyBapi.Invoke(prd);

            IRfcTable detail = companyBapi.GetTable("EX_RETURNS"); // returning table

            DataTable DT = new DataTable();
            DT.Columns.Add("ITEMS", System.Type.GetType("System.String"));
            DT.Columns.Add("STATUS", System.Type.GetType("System.String"));

            foreach (IRfcStructure row in detail)
            {
                DataRow _row = DT.NewRow();

                _row[0] = row.GetValue("ITEMS").ToString();
                _row[1] = row.GetValue("STATUS").ToString();

                DT.Rows.Add(_row);
                DT.AcceptChanges();
            }

            return DT;
        }

        private InCubeErrors GetCarrefourCustomerInfo(string CarrefourCustomerCode, ref string CustomerID, ref string OutletID)
        {
            InCubeErrors result = InCubeErrors.Error;
            try
            {
                string query = string.Format(@"SELECT CO.CustomerID, CO.OutletID
                                               FROM CarrefourOutlets CARO
                                               INNER JOIN CustomerOutlet CO ON CARO.InVanCode = CO.CustomerCode
                                               WHERE CARO.CarrefourCode = '{0}'", CarrefourCustomerCode);
                incubeQuery = new InCubeQuery(db_vms, query);
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtData = incubeQuery.GetDataTable();
                    if (dtData != null && dtData.Rows.Count == 1)
                    {
                        CustomerID = dtData.Rows[0]["CustomerID"].ToString();
                        OutletID = dtData.Rows[0]["OutletID"].ToString();
                        result = InCubeErrors.Success;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return result;
        }

        private InCubeErrors GetCarrefourEmpoyeeInfo(ref string EmployeeID, ref string OrderID)
        {
            InCubeErrors result = InCubeErrors.Error;
            string maxOrderID, charPart, numericPart;
            try
            {
                string query = string.Format(@"SELECT E.EmployeeID, DS.MaxTransactionOrderID
                                               FROM Employee E
                                               INNER JOIN DocumentSequence DS ON E.EmployeeID = DS.EmployeeID
                                               WHERE E.EmployeeCode = '{0}'", employeeCode);
                incubeQuery = new InCubeQuery(db_vms, query);
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtData = incubeQuery.GetDataTable();
                    if (dtData != null && dtData.Rows.Count == 1)
                    {
                        EmployeeID = dtData.Rows[0]["EmployeeID"].ToString();
                        maxOrderID = dtData.Rows[0]["MaxTransactionOrderID"].ToString();
                        charPart = ""; numericPart = "";
                        GetSequenceNumberFormat(maxOrderID, ref charPart, ref numericPart);
                        OrderID = charPart + (int.Parse(numericPart) + 1).ToString().PadLeft(maxOrderID.Length - charPart.Length, '0');
                        result = InCubeErrors.Success;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return result;
        }

        private DataTable GetCustomerTable()
        {
            DataTable DT = new DataTable();
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(GetServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_CUSTOMER_MASTER");
                IRfcTable tblImport = companyBapi.GetTable("IR_VKORG");
                //HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", IntegrationOrg);
                tblImport.SetValue("HIGH", IntegrationOrg);
                companyBapi.SetValue("IR_VKORG", tblImport);

                //IRfcTable tblImport6 = companyBapi.GetTable("IR_SPART");
                ////HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "D4");
                //tblImport6.SetValue("HIGH", "D4");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "D3");
                //tblImport6.SetValue("HIGH", "D3");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "15");
                //tblImport6.SetValue("HIGH", "15");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "20");
                //tblImport6.SetValue("HIGH", "20");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "25");
                //tblImport6.SetValue("HIGH", "25");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "35");
                //tblImport6.SetValue("HIGH", "35");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "40");
                //tblImport6.SetValue("HIGH", "40");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "45");
                //tblImport6.SetValue("HIGH", "45");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "99");
                //tblImport6.SetValue("HIGH", "99");
                //companyBapi.SetValue("IR_SPART", tblImport6);


                //IRfcTable tblImport3 = companyBapi.GetTable("IR_PERNR");
                //tblImport3.Append();
                //tblImport3.SetValue("SIGN", "I");
                //tblImport3.SetValue("OPTION", "EQ");
                //tblImport3.SetValue("LOW", "00002216");
                ////tblImport3.Append();
                ////tblImport3.SetValue("SIGN", "I");
                ////tblImport3.SetValue("OPTION", "EQ");
                ////tblImport3.SetValue("LOW", "00000270");
                ////tblImport3.Append();
                ////tblImport3.SetValue("SIGN", "I");
                ////tblImport3.SetValue("OPTION", "EQ");
                ////tblImport3.SetValue("LOW", "00001289");
                ////tblImport3.Append();
                ////tblImport3.SetValue("SIGN", "I");
                ////tblImport3.SetValue("OPTION", "EQ");
                ////tblImport3.SetValue("LOW", "00001350");
                ////tblImport3.Append();
                ////tblImport3.SetValue("SIGN", "I");
                ////tblImport3.SetValue("OPTION", "EQ");
                ////tblImport3.SetValue("LOW", "00001391");
                ////tblImport3.Append();
                ////tblImport3.SetValue("SIGN", "I");
                ////tblImport3.SetValue("OPTION", "EQ");
                ////tblImport3.SetValue("LOW", "00001872");
                //companyBapi.SetValue("IR_PERNR", tblImport3);

                //IRfcTable tblImport2 = companyBapi.GetTable("IR_VTWEG");
                //tblImport2.Append();
                //tblImport2.SetValue("SIGN", "I");
                //tblImport2.SetValue("OPTION", "BT");
                //tblImport2.SetValue("LOW", "A1");
                //tblImport2.SetValue("HIGH", "A5");
                //companyBapi.SetValue("IR_VTWEG", tblImport2);

                //IRfcTable tblImport5 = companyBapi.GetTable("IR_DATUM");
                ////HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
                //tblImport5.Append();
                //tblImport5.SetValue("SIGN", "I");
                //tblImport5.SetValue("OPTION", "BT");
                //tblImport5.SetValue("LOW", DateTime.Today.AddDays(-333).ToString("yyyyMMdd"));
                //tblImport5.SetValue("HIGH", DateTime.Today.ToString("yyyyMMdd"));
                //companyBapi.SetValue("IR_DATUM", tblImport5);


                //IRfcTable tblImport4 = companyBapi.GetTable("IR_KUNNR");
                //tblImport4.Append();
                //tblImport4.SetValue("SIGN", "I");
                //tblImport4.SetValue("OPTION", "EQ");
                //tblImport4.SetValue("LOW", "0000701847");
                ////tblImport4.Append();
                ////tblImport4.SetValue("SIGN", "I");
                ////tblImport4.SetValue("OPTION", "EQ");
                ////tblImport4.SetValue("LOW", "00001047");
                //companyBapi.SetValue("IR_KUNNR", tblImport4);


                //IRfcTable tblImport3 = companyBapi.GetTable("IR_SPART");
                //tblImport3.Append();
                //tblImport3.SetValue("SIGN", "I");
                //tblImport3.SetValue("OPTION", "EQ");
                //tblImport3.SetValue("LOW", "20");
                //companyBapi.SetValue("IR_SPART", tblImport3);
                companyBapi.Invoke(prd);
                IRfcTable detail = companyBapi.GetTable("ITEMTAB");

                DT.Columns.Add("BillToCode", System.Type.GetType("System.String"));
                DT.Columns.Add("BillToName", System.Type.GetType("System.String"));
                DT.Columns.Add("ShipToCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ShipToName", System.Type.GetType("System.String"));
                DT.Columns.Add("PayerCode", System.Type.GetType("System.String"));
                DT.Columns.Add("PayerName", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerType", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerPriceGroup", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerPriceGroupName", System.Type.GetType("System.String"));
                DT.Columns.Add("DeleteStatus", System.Type.GetType("System.String"));
                DT.Columns.Add("BlockStatus", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionCode", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionName", System.Type.GetType("System.String"));
                DT.Columns.Add("Address", System.Type.GetType("System.String"));
                DT.Columns.Add("ContactName", System.Type.GetType("System.String"));
                DT.Columns.Add("Telephone", System.Type.GetType("System.String"));
                DT.Columns.Add("Mobile", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerCategoryCode", System.Type.GetType("System.String"));
                DT.Columns.Add("CategoryName", System.Type.GetType("System.String"));
                DT.Columns.Add("PaymentTermDays", System.Type.GetType("System.String"));
                DT.Columns.Add("PaymentTermType", System.Type.GetType("System.String"));
                DT.Columns.Add("CreditLimit", System.Type.GetType("System.String"));
                DT.Columns.Add("Zone", System.Type.GetType("System.String"));
                DT.Columns.Add("Region", System.Type.GetType("System.String"));
                DT.Columns.Add("ZoneCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ChannelCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ChannelName", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerClass", System.Type.GetType("System.String"));
                DT.Columns.Add("RouteCode", System.Type.GetType("System.String"));
                DT.Columns.Add("RouteName", System.Type.GetType("System.String"));
                DT.Columns.Add("FLAG", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesmanCode", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesOrganizationCode", System.Type.GetType("System.String"));
                DT.Columns.Add("CompanyCode", System.Type.GetType("System.String"));
                DT.Columns.Add("GPS_Long", System.Type.GetType("System.String"));
                DT.Columns.Add("GPS_Lat", System.Type.GetType("System.String"));
                //                DataTable Employees = new DataTable();
                //                string getEmps = string.Format(@"select employeecode from employee");
                //                InCubeQuery qry = new InCubeQuery(getEmps, db_vms);
                //                qry.Execute();
                //                Employees = qry.GetDataTable();
                //                DataTable CustTbl = new DataTable();
                //                string FillTemp = string.Format(@"select CustomerCode from Customer where customerid in (select customerid from custoutterritory where customerid in (select customerid from payer group by customerid having count(payerid)>1) and territoryid in (select distinct territoryid from routehistory)
                //group by customerid
                //)");
                //                InCubeQuery tempQry = new InCubeQuery(FillTemp, db_vms);
                //                tempQry.Execute();
                //                CustTbl = tempQry.GetDataTable();
                foreach (IRfcStructure row in detail)
                {
                    if (row.GetValue("KUNAG").ToString().ToString().Trim().Equals(string.Empty)) continue;
                    DataRow _row = DT.NewRow();
                    _row["BillToCode"] = row.GetValue("KUNAG").ToString().ToString().Trim().Substring(4, 6);
                    //if (!_row["BillToCode"].ToString().Trim().Equals("705728")) continue;
                    //if (CustTbl.Select("CustomerCode='" + _row["BillToCode"].ToString().Trim() + "'").Length == 0) continue;
                    string typeC = row.GetValue("SPART").ToString();
                    _row["BillToName"] = row.GetValue("NAMAG").ToString();
                    _row["ShipToCode"] = row.GetValue("KUNWE").ToString().ToString().Trim().Substring(4, 6);
                    _row["ShipToName"] = row.GetValue("NAMWE").ToString();
                    _row["PayerCode"] = row.GetValue("KUNRG").ToString().ToString().Trim().Substring(4, 6);
                    _row["PayerName"] = row.GetValue("NAMRG").ToString();
                    _row["CustomerType"] = row.GetValue("CTYPE").ToString();
                    _row["CustomerPriceGroup"] = row.GetValue("KONDA").ToString() + "/" + row.GetValue("SPART").ToString();// +"/" + row.GetValue("VTWEG").ToString();
                    _row["CustomerPriceGroupName"] = row.GetValue("PGTXT").ToString() + "/" + row.GetValue("SPART").ToString();// +"/" + row.GetValue("VTWEG").ToString();
                    _row["DeleteStatus"] = row.GetValue("LOEVM").ToString();
                    _row["BlockStatus"] = row.GetValue("AUFSD").ToString();
                    _row["DivisionCode"] = row.GetValue("SPART").ToString();
                    _row["DivisionName"] = row.GetValue("VTEXT").ToString();
                    _row["Address"] = row.GetValue("ADDRS").ToString();
                    _row["ContactName"] = row.GetValue("CCNAM").ToString();
                    _row["Telephone"] = row.GetValue("TELF1").ToString();
                    _row["Mobile"] = row.GetValue("TELF2").ToString();
                    _row["CustomerCategoryCode"] = row.GetValue("KDGRP").ToString();
                    _row["CategoryName"] = row.GetValue("KTEXT").ToString();
                    _row["PaymentTermDays"] = row.GetValue("PTDAY").ToString();
                    if (_row["PaymentTermDays"].ToString().Trim().Equals(string.Empty)) continue;
                    int termdays = 0;
                    if (int.TryParse(_row["PaymentTermDays"].ToString().Trim(), out termdays))
                    {
                        if (termdays > 0)
                        {
                            _row["CustomerType"] = "1";
                        }
                        else
                        {
                            _row["CustomerType"] = "0";
                        }
                    }
                    else
                    {
                        continue;
                    }
                    _row["PaymentTermType"] = row.GetValue("PTTYP").ToString();
                    _row["CreditLimit"] = row.GetValue("KLIMK").ToString();
                    _row["Zone"] = row.GetValue("LZONE").ToString();
                    _row["Region"] = row.GetValue("REGIO").ToString();
                    _row["ZoneCode"] = row.GetValue("BLAND").ToString();
                    _row["ChannelCode"] = row.GetValue("VTWEG").ToString();
                    _row["ChannelName"] = row.GetValue("DTEXT").ToString();
                    _row["CustomerClass"] = row.GetValue("KLABC").ToString();
                    _row["RouteCode"] = row.GetValue("RCODE").ToString();
                    //if (!_row["RouteCode"].ToString().Trim().Equals("1131V354"))
                    //    continue;
                    _row["RouteName"] = row.GetValue("RNAME").ToString();
                    _row["FLAG"] = row.GetValue("ZFLAG").ToString();
                    _row["SalesmanCode"] = row.GetValue("PERNR").ToString();
                    _row["SalesOrganizationCode"] = row.GetValue("VKORG").ToString();
                    _row["CompanyCode"] = row.GetValue("BUKRS").ToString();
                    _row["GPS_Long"] = row.GetValue("BAHNS").ToString();
                    _row["GPS_Lat"] = row.GetValue("BAHNE").ToString();

                    DT.Rows.Add(_row);
                    DT.AcceptChanges();
                }
            }
            catch (Exception ex)
            {
                WriteExceptions("EXCEPTION HAPPENED IN THE GetCustomerTable() FUNCTION, THE MESSAGE IS <<" + ex.Message + ">>", "UPDATE CUSTOMER", false);
            }
            //DataRow[] tempt = new DataRow[50];
            //tempt = DT.Select("ShipToCode=0000704878");
            InCubeQuery qryOut = new InCubeQuery(db_vms, "QNIE_INSERTCUSTOMERS", 1000000000);
            qryOut.AddParameter("@Customers", DT);
            err = qryOut.ExecuteStoredProcedure();
            return DT;

        }

        private DataTable GetDiscountTable()
        {
            DataTable DT = new DataTable();
            try
            {
                ///// Passing a table from SAP to .Net //////
                RfcDestination prd = RfcDestinationManager.GetDestination(GetServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_DISCOUNT_MASTER");
                IRfcTable tblImport = companyBapi.GetTable("IR_VKORG");
                //HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              

                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", IntegrationOrg);
                //tblImport.SetValue("HIGH", "1200");
                //tblImport.Append();
                //tblImport.SetValue("SIGN", "I");
                //tblImport.SetValue("OPTION", "EQ");
                //tblImport.SetValue("LOW", "1200");
                companyBapi.SetValue("IR_VKORG", tblImport);

                //IRfcTable tblImport5 = companyBapi.GetTable("IR_DATUM");
                ////HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
                //tblImport5.Append();
                //tblImport5.SetValue("SIGN", "I");
                //tblImport5.SetValue("OPTION", "BT");
                //tblImport5.SetValue("LOW", DateTime.Today.AddDays(-10).ToString("yyyyMMdd"));
                //tblImport5.SetValue("HIGH", DateTime.Today.ToString("yyyyMMdd"));
                //companyBapi.SetValue("IR_DATUM", tblImport5);

                //IRfcTable tblImport2 = companyBapi.GetTable("IR_MATNR");
                //tblImport2.Append();
                //tblImport2.SetValue("SIGN", "I");
                //tblImport2.SetValue("OPTION", "EQ");
                //tblImport2.SetValue("LOW", "000000000011040004");
                //companyBapi.SetValue("IR_MATNR", tblImport2);
                //IRfcTable tblImport2 = companyBapi.GetTable("IR_KONDA");
                ////HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
                //tblImport2.Append();
                //tblImport2.SetValue("SIGN", "I");
                //tblImport2.SetValue("OPTION", "EQ");
                //tblImport2.SetValue("LOW", "10");
                ////tblImport.SetValue("HIGH", "1200");
                //companyBapi.SetValue("IR_KONDA", tblImport2);
                //IRfcTable tblImport6 = companyBapi.GetTable("IR_SPART");
                ////HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "D4");
                //tblImport6.SetValue("HIGH", "D4");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "D3");
                //tblImport6.SetValue("HIGH", "D3");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "15");
                //tblImport6.SetValue("HIGH", "15");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "20");
                //tblImport6.SetValue("HIGH", "20");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "25");
                //tblImport6.SetValue("HIGH", "25");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "35");
                //tblImport6.SetValue("HIGH", "35");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "40");
                //tblImport6.SetValue("HIGH", "40");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "45");
                //tblImport6.SetValue("HIGH", "45");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "99");
                //tblImport6.SetValue("HIGH", "99");
                //companyBapi.SetValue("IR_SPART", tblImport6);
                //DivisionDiscounts = new List<string>();
                //GroupDiscounts = new List<string>();
                //CustomerDiscounts = new List<string>();

                companyBapi.Invoke(prd);
                IRfcTable detail = companyBapi.GetTable("ITEMTAB");

                DT.Columns.Add("CompanyCode", System.Type.GetType("System.String"));
                DT.Columns.Add("CompanyCodeLevel3", System.Type.GetType("System.String"));
                DT.Columns.Add("PriceListName", System.Type.GetType("System.String"));
                DT.Columns.Add("ValidFrom", System.Type.GetType("System.String"));
                DT.Columns.Add("ValidTo", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemCode", System.Type.GetType("System.String"));
                DT.Columns.Add("UOM", System.Type.GetType("System.String"));
                DT.Columns.Add("ConversionFactor", System.Type.GetType("System.String"));
                DT.Columns.Add("Price", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerCode", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesGroupCode", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ChannelCode", System.Type.GetType("System.String"));
                DT.Columns.Add("StockStatus", System.Type.GetType("System.String"));
                DT.Columns.Add("FLAG", System.Type.GetType("System.String"));
                DT.Columns.Add("IsDeleted", System.Type.GetType("System.String"));

                ClearProgress();
                SetProgressMax(detail.Count);


                foreach (IRfcStructure row in detail)
                {
                    ReportProgress("Filling SAP Results");
                    if (row.GetValue("KONDA").ToString().Trim().Equals(string.Empty) && row.GetValue("KUNWE").ToString().Trim().Equals(string.Empty) && !row.GetValue("SPART").ToString().Trim().Equals(string.Empty))
                    {

                    }
                    else
                    {
                        //continue;
                    }

                    DataRow _row = DT.NewRow();

                    _row["CompanyCode"] = row.GetValue("BUKRS").ToString();
                    _row["CompanyCodeLevel3"] = row.GetValue("VKORG").ToString();
                    _row["PriceListName"] = row.GetValue("OBJKY").ToString();
                    _row["ValidFrom"] = row.GetValue("DATAB").ToString();
                    _row["ValidTo"] = row.GetValue("DATBI").ToString();
                    _row["ItemCode"] = row.GetValue("MATNR").ToString().Trim().Substring(10, 8);
                    //if (!row.GetValue("MATNR").ToString().Trim().Substring(10, 8).Equals("10160007")) continue;
                    _row["UOM"] = row.GetValue("KMEIN").ToString();
                    _row["ConversionFactor"] = row.GetValue("CONVF").ToString();
                    _row["Price"] = Math.Abs(decimal.Parse(row.GetValue("KBETR").ToString()));
                    if (!row.GetValue("KUNWE").ToString().Trim().Equals(string.Empty))
                    {
                        _row["CustomerCode"] = row.GetValue("KUNWE").ToString().Trim().Substring(4, 6);
                    }
                    else
                    {
                        _row["CustomerCode"] = row.GetValue("KUNWE").ToString().Trim();
                    }//.ToString().Trim().Substring(4, 6); //10/40/1100
                    if (!row.GetValue("KONDA").ToString().Trim().Equals(string.Empty))
                    {
                        _row["SalesGroupCode"] = row.GetValue("KONDA").ToString() + "/" + row.GetValue("SPART").ToString();
                    }
                    else
                    {
                        _row["SalesGroupCode"] = row.GetValue("KONDA").ToString();
                    }
                    _row["DivisionCode"] = row.GetValue("SPART").ToString();
                    _row["ChannelCode"] = row.GetValue("VTWEG").ToString();
                    _row["StockStatus"] = "";
                    _row["FLAG"] = row.GetValue("ZFLAG").ToString();
                    _row["IsDeleted"] = row.GetValue("LOEVM").ToString();
                    //if (_row["DivisionCode"].ToString().Trim().Equals("40") && _row["SalesGroupCode"].ToString().Trim().Equals("10") && _row["CompanyCodeLevel3"].ToString().Trim().Equals("1100"))
                    //{ ; }
                    //else { continue; }//
                    //if (!_row["DivisionCode"].ToString().Trim().Equals(string.Empty) && _row["ChannelCode"].ToString().Trim().Equals(string.Empty) && _row["SalesGroupCode"].ToString().Trim().Equals(string.Empty) && _row["CustomerCode"].ToString().Trim().Equals(string.Empty))
                    //{
                    //    if (!DivisionDiscounts.Contains(_row["DivisionCode"].ToString().Trim() + "-" + _row["ItemCode"].ToString().Trim()))
                    //    {
                    //        DivisionDiscounts.Add(_row["DivisionCode"].ToString().Trim() + "-" + _row["ItemCode"].ToString().Trim());
                    //    }
                    //}
                    //if ( !_row["SalesGroupCode"].ToString().Trim().Equals(string.Empty) && _row["CustomerCode"].ToString().Trim().Equals(string.Empty))
                    //{
                    //    if (!GroupDiscounts.Contains(_row["SalesGroupCode"].ToString().Trim() + "-" + _row["ItemCode"].ToString().Trim()))
                    //    {
                    //        GroupDiscounts.Add(_row["SalesGroupCode"].ToString().Trim() + "-" + _row["ItemCode"].ToString().Trim());
                    //    }
                    //}
                    //if (!_row["CustomerCode"].ToString().Trim().Equals(string.Empty))
                    //{
                    //    if (!CustomerDiscounts.Contains(_row["CustomerCode"].ToString().Trim() + "-" + _row["ItemCode"].ToString().Trim()))
                    //    {
                    //        CustomerDiscounts.Add(_row["CustomerCode"].ToString().Trim() + "-" + _row["ItemCode"].ToString().Trim());
                    //    }
                    //}
                    DT.Rows.Add(_row);
                    DT.AcceptChanges();
                }
            }
            catch (Exception ex)
            {
                WriteExceptions("EXCEPTION HAPPENED IN THE GetPriceTable() FUNCTION, THE MESSAGE IS <<" + ex.Message + ">>", "UPDATE REMAINING AMOUNT", false);
            }
            return DT;
        }

        private DataTable GetInvoiceTable(ref int counter)
        {
            DataTable DT = new DataTable();
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(GetServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_OPEN_INVOICES");
                IRfcTable tblImport = companyBapi.GetTable("IR_BUKRS");
                //HERE WE CREATE A TABLE PARAMETER TO PASS TO SAP FUNCTION.              
                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", IntegrationOrg);
                tblImport.SetValue("HIGH", IntegrationOrg);
                companyBapi.SetValue("IR_BUKRS", tblImport);

                //Dist_Channel = "702399";

                //IRfcTable tblImport2 = companyBapi.GetTable("IR_KUNAG");
                ////HERE WE CREATE A TABLE PARAMETER TO PASS CUSTOMER CODE TO SAP FUNCTION.  
                //Dist_Channel = "0000" + Dist_Channel;
                //tblImport2.Append();
                //tblImport2.SetValue("SIGN", "I");
                //tblImport2.SetValue("OPTION", "EQ");
                //tblImport2.SetValue("LOW", Dist_Channel);
                //companyBapi.SetValue("IR_KUNAG", tblImport2);

                companyBapi.Invoke(prd);

                IRfcTable detail = companyBapi.GetTable("ITEMTAB");
                WriteMessage("\r\n");
                WriteMessage("<<< Outstanding >>> start update... " + ">>" + DateTime.Now.ToString());

                DT.Columns.Add("COMPANYCODE", System.Type.GetType("System.String"));
                DT.Columns.Add("SoldTo", System.Type.GetType("System.String"));
                DT.Columns.Add("ShipTo", System.Type.GetType("System.String"));
                DT.Columns.Add("PayerCode", System.Type.GetType("System.String"));
                DT.Columns.Add("TransactionNumber", System.Type.GetType("System.String"));
                // DT.Columns.Add("SAP_REF_NO", System.Type.GetType("System.String"));
                DT.Columns.Add("TotalAmount", System.Type.GetType("System.String"));
                DT.Columns.Add("RemainingAmount", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesmanCode", System.Type.GetType("System.String"));
                DT.Columns.Add("TransactionType", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionCode", System.Type.GetType("System.String"));
                DT.Columns.Add("InvoiceDate", System.Type.GetType("System.String"));

                ClearProgress();
                SetProgressMax(detail.Count);
                int co = 0;
                foreach (IRfcStructure row in detail)
                {
                    ReportProgress("Updating Invoices " + ++co);
                    DataRow _row = DT.NewRow();
                    _row["COMPANYCODE"] = row.GetValue("BUKRS").ToString();
                    if (!row.GetValue("KUNAG").ToString().Trim().Equals(string.Empty))
                    {
                        string xxxx = row.GetValue("KUNAG").ToString().Trim();
                        if (row.GetValue("KUNAG").ToString().Trim().ToLower().StartsWith("st")) continue;
                        _row["SoldTo"] = row.GetValue("KUNAG").ToString().Trim().Substring(4, 6);
                    }
                    else
                    {
                        _row["SoldTo"] = string.Empty;
                    }
                    if (!row.GetValue("KUNWE").ToString().Trim().Equals(string.Empty))
                    {
                        _row["ShipTo"] = row.GetValue("KUNWE").ToString().Trim().Substring(4, 6);
                    }
                    else
                    {
                        _row["ShipTo"] = string.Empty;
                    }
                    if (!row.GetValue("KUNRG").ToString().Trim().Equals(string.Empty))
                    {
                        _row["PayerCode"] = row.GetValue("KUNRG").ToString().Trim().Substring(4, 6);
                    }
                    else
                    {
                        _row["PayerCode"] = string.Empty;
                    }


                    if (row.GetValue("XBLNR").ToString().Trim().Equals(string.Empty))
                    {
                        _row["TransactionNumber"] = row.GetValue("VBELN").ToString().Trim();
                    }
                    else
                    {
                        _row["TransactionNumber"] = row.GetValue("XBLNR").ToString().Trim();
                    }
                    _row["TransactionType"] = row.GetValue("VGART").ToString();
                    if (_row["TransactionType"].ToString().Trim().Equals("2"))
                        _row["TransactionNumber"] = "C" + _row["TransactionNumber"].ToString();
                    if (_row["TransactionType"].ToString().Trim().Equals("2"))
                        _row["TransactionType"] = "5";
                    _row["TotalAmount"] = row.GetValue("NETWR").ToString();
                    _row["RemainingAmount"] = row.GetValue("BLAMT").ToString();
                    if (decimal.Parse(_row["RemainingAmount"].ToString().Trim()) <= 0) continue;
                    _row["SalesmanCode"] = row.GetValue("PERNR").ToString();
                    _row["DivisionCode"] = row.GetValue("SPART").ToString();
                    _row["InvoiceDate"] = row.GetValue("FKDAT").ToString();


                    DT.Rows.Add(_row);
                    DT.AcceptChanges();
                }

                WriteMessage("\r\n");
                WriteMessage("<<< Outstanding>>>  finished update .... please wait while changes are being made." + ">>" + DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                WriteExceptions("EXCEPTION HAPPENED IN THE GetInvoiceTable(string Dist_Channel, ref int counter) FUNCTION, THE MESSAGE IS <<" + ex.Message + ">>", "UPDATE REMAINING AMOUNT", false);
            }
            return DT;
        }

        private DataTable OLDGetInvoiceTableOLD(string Dist_Channel, ref int counter)
        {
            DataTable DT = new DataTable();
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(GetServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_OPEN_INVOICES");
                IRfcTable tblImport = companyBapi.GetTable("IR_BUKRS");
                //HERE WE CREATE A TABLE PARAMETER TO PASS TO SAP FUNCTION.              
                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", IntegrationOrg);
                tblImport.SetValue("HIGH", IntegrationOrg);
                //tblImport.Append();
                //tblImport.SetValue("SIGN", "I");
                //tblImport.SetValue("OPTION", "EQ");
                //tblImport.SetValue("LOW", "1200");
                //tblImport.SetValue("HIGH", "1200");
                companyBapi.SetValue("IR_BUKRS", tblImport);

                //IRfcTable tblImport2 = companyBapi.GetTable("IR_PERNR");
                ////HERE WE CREATE A TABLE PARAMETER TO PASS CUSTOMER CODE TO SAP FUNCTION.              
                //tblImport2.Append();
                //tblImport2.SetValue("SIGN", "I");
                //tblImport2.SetValue("OPTION", "EQ");
                //tblImport2.SetValue("LOW", Dist_Channel);
                //companyBapi.SetValue("IR_PERNR", tblImport2);

                IRfcTable tblImport3 = companyBapi.GetTable("IR_KUNRG");
                //HERE WE CREATE A TABLE PARAMETER TO PASS CUSTOMER CODE TO SAP FUNCTION.              
                tblImport3.Append();
                tblImport3.SetValue("SIGN", "I");
                tblImport3.SetValue("OPTION", "EQ");
                tblImport3.SetValue("LOW", "0000" + Dist_Channel);
                companyBapi.SetValue("IR_KUNRG", tblImport3);

                //IRfcTable tblImport4 = companyBapi.GetTable("IR_DATE");
                ////HERE WE CREATE A TABLE PARAMETER TO PASS CUSTOMER CODE TO SAP FUNCTION.              
                //tblImport4.Append();
                //tblImport4.SetValue("SIGN", "I");
                //tblImport4.SetValue("OPTION", "BT");
                //tblImport4.SetValue("LOW", DateTime.Today.AddDays(-120).ToString("yyyyMMdd"));
                //tblImport4.SetValue("HIGH", DateTime.Today.ToString("yyyyMMdd"));
                //companyBapi.SetValue("IR_DATE", tblImport4);

                companyBapi.SetValue("IV_INVTYP", "O");

                IRfcTable tblImport6 = companyBapi.GetTable("IR_SPART");
                //HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
                tblImport6.Append();
                tblImport6.SetValue("SIGN", "I");
                tblImport6.SetValue("OPTION", "EQ");
                tblImport6.SetValue("LOW", "D4");
                tblImport6.SetValue("HIGH", "D4");
                tblImport6.Append();
                tblImport6.SetValue("SIGN", "I");
                tblImport6.SetValue("OPTION", "EQ");
                tblImport6.SetValue("LOW", "D3");
                tblImport6.SetValue("HIGH", "D3");
                tblImport6.Append();
                tblImport6.SetValue("SIGN", "I");
                tblImport6.SetValue("OPTION", "EQ");
                tblImport6.SetValue("LOW", "15");
                tblImport6.SetValue("HIGH", "15");
                tblImport6.Append();
                tblImport6.SetValue("SIGN", "I");
                tblImport6.SetValue("OPTION", "EQ");
                tblImport6.SetValue("LOW", "20");
                tblImport6.SetValue("HIGH", "20");
                tblImport6.Append();
                tblImport6.SetValue("SIGN", "I");
                tblImport6.SetValue("OPTION", "EQ");
                tblImport6.SetValue("LOW", "25");
                tblImport6.SetValue("HIGH", "25");
                tblImport6.Append();
                tblImport6.SetValue("SIGN", "I");
                tblImport6.SetValue("OPTION", "EQ");
                tblImport6.SetValue("LOW", "35");
                tblImport6.SetValue("HIGH", "35");
                tblImport6.Append();
                tblImport6.SetValue("SIGN", "I");
                tblImport6.SetValue("OPTION", "EQ");
                tblImport6.SetValue("LOW", "40");
                tblImport6.SetValue("HIGH", "40");
                tblImport6.Append();
                tblImport6.SetValue("SIGN", "I");
                tblImport6.SetValue("OPTION", "EQ");
                tblImport6.SetValue("LOW", "45");
                tblImport6.SetValue("HIGH", "45");
                tblImport6.Append();
                tblImport6.SetValue("SIGN", "I");
                tblImport6.SetValue("OPTION", "EQ");
                tblImport6.SetValue("LOW", "99");
                tblImport6.SetValue("HIGH", "99");
                companyBapi.SetValue("IR_SPART", tblImport6);

                companyBapi.Invoke(prd);

                IRfcTable detail = companyBapi.GetTable("ITEMTAB");
                WriteMessage("\r\n");
                WriteMessage("<<< Customer Invoices >>>  " + Dist_Channel + " start update ");

                DT.Columns.Add("COMPANYCODE", System.Type.GetType("System.String"));
                DT.Columns.Add("SoldTo", System.Type.GetType("System.String"));
                DT.Columns.Add("ShipTo", System.Type.GetType("System.String"));
                DT.Columns.Add("PayerCode", System.Type.GetType("System.String"));
                DT.Columns.Add("TransactionNumber", System.Type.GetType("System.String"));
                DT.Columns.Add("SAP_REF_NO", System.Type.GetType("System.String"));
                DT.Columns.Add("TotalAmount", System.Type.GetType("System.String"));
                DT.Columns.Add("RemainingAmount", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesmanCode", System.Type.GetType("System.String"));
                DT.Columns.Add("TransactionType", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionCode", System.Type.GetType("System.String"));
                DT.Columns.Add("InvoiceDate", System.Type.GetType("System.String"));

                ClearProgress();
                SetProgressMax(detail.Count);
                int co = 0;
                foreach (IRfcStructure row in detail)
                {
                    ReportProgress("Updating Invoices " + ++co);

                    DataRow _row = DT.NewRow();
                    _row["COMPANYCODE"] = row.GetValue("BUKRS").ToString();
                    if (row.GetValue("KUNAG").ToString().Trim().Equals(string.Empty)) continue;
                    if (row.GetValue("KUNWE").ToString().Trim().Equals(string.Empty)) continue;
                    if (row.GetValue("KUNRG").ToString().Trim().Equals(string.Empty)) continue;
                    _row["SoldTo"] = row.GetValue("KUNAG").ToString().Trim().Substring(4, 6);
                    _row["ShipTo"] = row.GetValue("KUNWE").ToString().Trim().Substring(4, 6);
                    _row["PayerCode"] = row.GetValue("KUNRG").ToString().Trim().Substring(4, 6);
                    _row["TransactionNumber"] = row.GetValue("XBLNR").ToString();
                    _row["SAP_REF_NO"] = row.GetValue("VBELN").ToString();
                    _row["TotalAmount"] = row.GetValue("NETWR").ToString();
                    _row["RemainingAmount"] = row.GetValue("BLAMT").ToString();
                    //if (decimal.Parse(_row["RemainingAmount"].ToString().Trim()) <= 0) continue;
                    _row["SalesmanCode"] = row.GetValue("PERNR").ToString();
                    _row["TransactionType"] = row.GetValue("VGART").ToString();
                    _row["DivisionCode"] = row.GetValue("SPART").ToString();
                    //if (!_row["DivisionCode"].ToString().Trim().Equals("15")) continue;
                    _row["InvoiceDate"] = row.GetValue("FKDAT").ToString();

                    DT.Rows.Add(_row);
                    DT.AcceptChanges();
                }

                WriteMessage("\r\n");
                WriteMessage("<<< Customer Invoices >>>  " + Dist_Channel + " finished update (" + co + ")");
            }
            catch (Exception ex)
            {
                WriteExceptions("EXCEPTION HAPPENED IN THE GetInvoiceTable(string Dist_Channel, ref int counter) FUNCTION, THE MESSAGE IS <<" + ex.Message + ">>", "UPDATE REMAINING AMOUNT", false);
            }
            return DT;
        }

        private DataTable GetItemTable()
        {
            DataTable DT = new DataTable();
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(GetServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_MATERIAL_MASTER");

                IRfcTable tblImport = companyBapi.GetTable("IR_VKORG");
                //HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", IntegrationOrg);
                tblImport.SetValue("HIGH", IntegrationOrg);
                //tblImport.Append();
                //tblImport.SetValue("SIGN", "I");
                //tblImport.SetValue("OPTION", "EQ");
                //tblImport.SetValue("LOW", "1200");
                //tblImport.SetValue("HIGH", "1200");
                companyBapi.SetValue("IR_VKORG", tblImport);

                //IRfcTable tblImport2 = companyBapi.GetTable("IR_DATUM");
                ////HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
                //tblImport2.Append();
                //tblImport2.SetValue("SIGN", "I");
                //tblImport2.SetValue("OPTION", "BT");
                //tblImport2.SetValue("LOW", DateTime.Today.AddDays(-800).ToString("yyyyMMdd"));
                //tblImport2.SetValue("HIGH", DateTime.Today.AddDays(1).ToString("yyyyMMdd"));
                //companyBapi.SetValue("IR_DATUM", tblImport2);


                //FOLLOWING PARAMETER TO GET A SPECIFIC ITEM (REMOVE IT)
                //IRfcTable tblImport6 = companyBapi.GetTable("IR_MATNR");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "000000000011700017");
                //companyBapi.SetValue("IR_MATNR", tblImport6);

                companyBapi.Invoke(prd);
                IRfcTable detail = companyBapi.GetTable("ITEMTAB");

                DT.Columns.Add("ItemCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemName", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemCategoryCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemCategoryDescription", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemGroupCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemGroupName", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemBrandCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemBrandName", System.Type.GetType("System.String"));
                DT.Columns.Add("BaseUOM", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemUOM", System.Type.GetType("System.String"));
                DT.Columns.Add("Numerator", System.Type.GetType("System.String"));
                DT.Columns.Add("Denominator", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionCode", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionName", System.Type.GetType("System.String"));
                DT.Columns.Add("CompanyCodeLevel2", System.Type.GetType("System.String"));
                DT.Columns.Add("CompanyCodeLevel3", System.Type.GetType("System.String"));
                DT.Columns.Add("FLAG", System.Type.GetType("System.String"));
                DT.Columns.Add("Barcode", System.Type.GetType("System.String"));
                DT.Columns.Add("InActive", System.Type.GetType("System.String"));
                DT.Columns.Add("CWM", System.Type.GetType("System.String"));

                foreach (IRfcStructure row in detail)
                {
                    //if (!row.GetValue("MATNR").ToString().Trim().Substring(10, 8).Equals("77030040")) continue;
                    DataRow _row = DT.NewRow();
                    _row["ItemCode"] = row.GetValue("MATNR").ToString();
                    _row["ItemName"] = row.GetValue("GROES").ToString();
                    _row["ItemCategoryCode"] = row.GetValue("EXTWG").ToString();
                    _row["ItemCategoryDescription"] = row.GetValue("EWBEZ").ToString();
                    _row["ItemGroupCode"] = row.GetValue("PRDHA").ToString();
                    _row["ItemGroupName"] = row.GetValue("BEZEI").ToString();
                    _row["ItemBrandCode"] = row.GetValue("MATKL").ToString();
                    _row["ItemBrandName"] = row.GetValue("WGBEZ").ToString();
                    _row["BaseUOM"] = row.GetValue("MEINS").ToString();
                    if (_row["BaseUOM"].ToString().Trim().ToLower().Equals("l") || _row["BaseUOM"].ToString().Trim().ToLower().Equals("ml"))
                    {

                    }
                    _row["ItemUOM"] = row.GetValue("MEINH").ToString();
                    if (_row["ItemUOM"].ToString().Trim().ToLower().Equals("l") || _row["ItemUOM"].ToString().Trim().ToLower().Equals("ml")) continue;
                    _row["Numerator"] = row.GetValue("UMREZ").ToString();
                    _row["Denominator"] = row.GetValue("UMREN").ToString();
                    _row["DivisionCode"] = row.GetValue("SPART").ToString();
                    _row["DivisionName"] = row.GetValue("VTEXT").ToString();
                    _row["CompanyCodeLevel2"] = row.GetValue("BUKRS").ToString();
                    _row["CompanyCodeLevel3"] = row.GetValue("VKORG").ToString();
                    _row["FLAG"] = row.GetValue("ZFLAG").ToString();
                    _row["Barcode"] = row.GetValue("EAN11").ToString();
                    _row["InActive"] = row.GetValue("LVORM").ToString();
                    _row["CWM"] = row.GetValue("CWMAT").ToString();
                    //if (row.GetValue("CWMAT").ToString().Trim().Equals(string.Empty))
                    //    continue;
                    DT.Rows.Add(_row);
                    DT.AcceptChanges();
                }
            }
            catch
            {
            }
            return DT;
        }

        private DataTable GetApprovedLoad(ref List<string> TransactionsList)
        {
            DataTable DT = new DataTable();
            try
            {
                DateTime SW = DateTime.Now;
                RfcDestination prd = RfcDestinationManager.GetDestination(GetServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_LOAD_OFFLOAD_STATUS");
                IRfcTable tblImport = companyBapi.GetTable("IR_BUKRS");
                //HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", IntegrationOrg);
                tblImport.SetValue("HIGH", IntegrationOrg);
                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", "1200");
                tblImport.SetValue("HIGH", "1200");
                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", "9900");
                tblImport.SetValue("HIGH", "9900");
                companyBapi.SetValue("IR_BUKRS", tblImport);

                IRfcTable tblImport2 = companyBapi.GetTable("IR_GRDAT");
                //HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
                tblImport2.Append();
                tblImport2.SetValue("SIGN", "I");
                tblImport2.SetValue("OPTION", "BT");
                tblImport2.SetValue("LOW", DateTime.Today.AddDays(-2).ToString("yyyyMMdd"));
                tblImport2.SetValue("HIGH", DateTime.Today.ToString("yyyyMMdd"));
                companyBapi.SetValue("IR_GRDAT", tblImport2);

                //IRfcTable tblImport3 = companyBapi.GetTable("IT_LGORT");
                ////HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
                //tblImport3.Append();
                //tblImport3.SetValue("LOW", "1131V251");
                ////tblImport3.SetValue("HIGH", DateTime.Today.ToString("yyyyMMdd"));
                //companyBapi.SetValue("IT_LGORT", tblImport3);

                IRfcTable tblImport4 = companyBapi.GetTable("IR_GJAHR");
                //HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
                tblImport4.Append();
                tblImport4.SetValue("SIGN", "I");
                tblImport4.SetValue("OPTION", "EQ");
                tblImport4.SetValue("LOW", "2016");
                //tblImport3.SetValue("HIGH", DateTime.Today.ToString("yyyyMMdd"));
                companyBapi.SetValue("IR_GJAHR", tblImport4);


                companyBapi.Invoke(prd);
                IRfcTable detail = companyBapi.GetTable("ITEMTAB");

                DT.Columns.Add("COMPANYCODE", System.Type.GetType("System.String"));
                DT.Columns.Add("TransactionID", System.Type.GetType("System.String"));
                DT.Columns.Add("SAP_REF_NUM", System.Type.GetType("System.String"));
                DT.Columns.Add("LocationFrom", System.Type.GetType("System.String"));
                DT.Columns.Add("LocationTo", System.Type.GetType("System.String"));
                DT.Columns.Add("TransactionType", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionCode", System.Type.GetType("System.String"));
                DT.Columns.Add("Date", System.Type.GetType("System.String"));
                DT.Columns.Add("Status", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemCode", System.Type.GetType("System.String"));
                DT.Columns.Add("UOM", System.Type.GetType("System.String"));
                DT.Columns.Add("ConversionFactor", System.Type.GetType("System.String"));
                DT.Columns.Add("Qty1", System.Type.GetType("System.String"));
                DT.Columns.Add("Qty2", System.Type.GetType("System.String"));
                DT.Columns.Add("Batch", System.Type.GetType("System.String"));
                DT.Columns.Add("Expiry", System.Type.GetType("System.String"));
                //DT.Columns.Add("TransactionID", System.Type.GetType("System.String"));

                foreach (IRfcStructure row in detail)
                {
                    DataRow _row = DT.NewRow();
                    _row["COMPANYCODE"] = row.GetValue("BUKRS").ToString();
                    _row["TransactionID"] = row.GetValue("XBLNR").ToString();
                    _row["SAP_REF_NUM"] = row.GetValue("VBELN").ToString();

                    //if (!_row["SAP_REF_NUM"].ToString().Trim().Equals("5000402338") ) { continue; }
                    //if (!_row["TransactionID"].ToString().Trim().Equals("W35-12890278")) { continue; }
                    //if (!row.GetValue("MATNR").ToString().Trim().Substring(10, 8).Equals("10520151")) { continue; }
                    if (!GetFieldValue("WH_Sync", "TransactionID", "TransactionID in ('" + row.GetValue("VBELN").ToString().Trim() + "','" + _row["TransactionID"].ToString().Trim() + "') and StatusID=2", db_vms).Trim().Equals(string.Empty)) continue;
                    _row["TransactionType"] = row.GetValue("VGART").ToString();
                    //if (!_row["TransactionType"].ToString().Trim().Equals("0")) { continue; }
                    _row["LocationFrom"] = row.GetValue("WERKS").ToString();
                    _row["LocationTo"] = row.GetValue("VWERK").ToString();
                    //_row["LocationTo"] = "1121"; _row["LocationFrom"] = "1131V152";
                    //if (!_row["LocationTo"].ToString().Equals("1131V152") && !_row["LocationFrom"].ToString().Equals("1131V152")) continue;

                    _row["DivisionCode"] = row.GetValue("SPART").ToString();
                    _row["Date"] = row.GetValue("BUDAT").ToString();
                    _row["Status"] = row.GetValue("ZSTAT").ToString();
                    _row["ItemCode"] = row.GetValue("MATNR").ToString().Trim().Substring(10, 8);
                    _row["UOM"] = row.GetValue("MEINS").ToString();
                    _row["ConversionFactor"] = row.GetValue("CONVF").ToString();
                    _row["Qty1"] = row.GetValue("MENGE").ToString();
                    _row["Qty2"] = row.GetValue("CWQTY").ToString();
                    _row["Batch"] = row.GetValue("CHARG").ToString();
                    _row["Expiry"] = row.GetValue("VFDAT").ToString();
                    //_row["TransactionID"] = row.GetValue("XBLNR").ToString();
                    if (_row["TransactionID"].ToString().Trim().Equals(string.Empty))
                    {
                        if (!TransactionsList.Contains(_row["SAP_REF_NUM"].ToString().Trim()))
                        {
                            TransactionsList.Add(_row["SAP_REF_NUM"].ToString().Trim());
                        }
                    }
                    else
                    {
                        if (!TransactionsList.Contains(_row["TransactionID"].ToString().Trim()))
                        {
                            TransactionsList.Add(_row["TransactionID"].ToString().Trim());
                        }
                    }
                    DT.Rows.Add(_row);
                    DT.AcceptChanges();
                }
            }
            catch
            {
            }
            DateTime SW2 = DateTime.Now;
            return DT;
        }

        private DataTable GetMainWHStockTable()
        {
            RfcDestination prd = RfcDestinationManager.GetDestination(GetServerName);
            RfcRepository repo = prd.Repository;
            IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_WAREHOUSE_STOCK");
            IRfcTable tblImport = companyBapi.GetTable("IR_BUKRS");
            //HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
            tblImport.Append();
            tblImport.SetValue("SIGN", "I");
            tblImport.SetValue("OPTION", "EQ");
            tblImport.SetValue("LOW", IntegrationOrg);
            tblImport.SetValue("HIGH", IntegrationOrg);
            //tblImport.Append();
            //tblImport.SetValue("SIGN", "I");
            //tblImport.SetValue("OPTION", "EQ");
            //tblImport.SetValue("LOW", "1200");
            //tblImport.SetValue("HIGH", "1200");
            companyBapi.SetValue("IR_BUKRS", tblImport);

            IRfcTable tblImport2 = companyBapi.GetTable("IR_WERKS");
            //HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
            tblImport2.Append();
            tblImport2.SetValue("SIGN", "I");
            tblImport2.SetValue("OPTION", "EQ");
            tblImport2.SetValue("LOW", "1102");
            tblImport2.Append();
            tblImport2.SetValue("SIGN", "I");
            tblImport2.SetValue("OPTION", "EQ");
            tblImport2.SetValue("LOW", "1103");
            tblImport2.Append();
            tblImport2.SetValue("SIGN", "I");
            tblImport2.SetValue("OPTION", "EQ");
            tblImport2.SetValue("LOW", "1105");
            tblImport2.Append();
            tblImport2.SetValue("SIGN", "I");
            tblImport2.SetValue("OPTION", "EQ");
            tblImport2.SetValue("LOW", "1107");
            tblImport2.Append();
            tblImport2.SetValue("SIGN", "I");
            tblImport2.SetValue("OPTION", "EQ");
            tblImport2.SetValue("LOW", "1151");
            tblImport2.Append();
            tblImport2.SetValue("SIGN", "I");
            tblImport2.SetValue("OPTION", "EQ");
            tblImport2.SetValue("LOW", "1202");
            tblImport2.Append();
            tblImport2.SetValue("SIGN", "I");
            tblImport2.SetValue("OPTION", "EQ");
            tblImport2.SetValue("LOW", "1207");
            companyBapi.SetValue("IR_WERKS", tblImport2);

            //FOLLOWING PARAMETER TO GET A SPECIFIC ITEM (REMOVE IT)
            //IRfcTable tblImport6 = companyBapi.GetTable("IR_MATNR");
            //tblImport6.Append();
            //tblImport6.SetValue("SIGN", "I");
            //tblImport6.SetValue("OPTION", "EQ");
            //tblImport6.SetValue("LOW", "000000000011040004");
            //companyBapi.SetValue("IR_MATNR", tblImport6);

            IRfcTable tblImport3 = companyBapi.GetTable("IR_LGORT");
            //HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
            tblImport3.Append();
            tblImport3.SetValue("SIGN", "I");
            tblImport3.SetValue("OPTION", "EQ");
            tblImport3.SetValue("LOW", "0001");
            //tblImport.SetValue("HIGH", "1102");
            companyBapi.SetValue("IR_LGORT", tblImport3);

            companyBapi.Invoke(prd);
            IRfcTable detail = companyBapi.GetTable("ITEMTAB");
            DataTable DT = new DataTable();
            DT.Columns.Add("COMPANYCODE", System.Type.GetType("System.String"));
            DT.Columns.Add("DivisionCode", System.Type.GetType("System.String"));
            DT.Columns.Add("Plant", System.Type.GetType("System.String"));
            DT.Columns.Add("StorageLocation", System.Type.GetType("System.String"));
            DT.Columns.Add("ItemCode", System.Type.GetType("System.String"));
            DT.Columns.Add("ItemUOM", System.Type.GetType("System.String"));
            DT.Columns.Add("Denominator", System.Type.GetType("System.String"));
            DT.Columns.Add("Enumertaor", System.Type.GetType("System.String"));
            DT.Columns.Add("Quantity", System.Type.GetType("System.String"));
            DT.Columns.Add("CWQuantity", System.Type.GetType("System.String"));


            foreach (IRfcStructure row in detail)
            {
                DataRow _row = DT.NewRow();
                _row["COMPANYCODE"] = row.GetValue("BUKRS").ToString();
                _row["DivisionCode"] = row.GetValue("SPART").ToString();
                _row["Plant"] = row.GetValue("WERKS").ToString();
                _row["StorageLocation"] = row.GetValue("LGORT").ToString();
                _row["ItemCode"] = row.GetValue("MATNR").ToString().Trim().Substring(10, 8);
                //if (!_row["ItemCode"].ToString().Trim().Equals("11040004"))
                //{ continue; }
                _row["ItemUOM"] = row.GetValue("MEINS").ToString();
                _row["Denominator"] = row.GetValue("UMREN").ToString();
                _row["Enumertaor"] = row.GetValue("UMREZ").ToString();
                _row["Quantity"] = row.GetValue("LABST").ToString();
                _row["CWQuantity"] = row.GetValue("/CWM/LABST").ToString();

                DT.Rows.Add(_row);
                DT.AcceptChanges();
            }

            return DT;
        }

        private DataTable GetOrderStatusTable()
        {
            ///// Passing a table from SAP to .Net //////
            RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
            RfcRepository repo = prd.Repository;
            IRfcFunction companyBapi = repo.CreateFunction("ZSD_FM_PDT_SALES_STATUS");
            // Passing the Parameter value

            companyBapi.SetValue("CR_DATE", _modificationDate.ToString(DateFormtSAP));//
            // companyBapi.SetValue("MOD_DATE", _modificationDate.ToString(DateFormtSAP));
            companyBapi.Invoke(prd);

            IRfcTable detail = companyBapi.GetTable("EX_ITEMS"); // returning table

            //string SalepersonCode = detail.GetString("SALES_CODE");
            //string SalesPersonName = detail.GetString("SALES_NAME");
            //string Phone = detail.GetString("SALES_PHONE");
            //string Address = detail.GetString("SALES_ADDR");
            //string Active = detail.GetString("ACTIVE");
            //string CreationDate = detail.GetString("CR_DATE");
            //string ModDate = detail.GetString("MOD_DATE");

            DataTable DT = new DataTable();
            DT.Columns.Add("OrderID", System.Type.GetType("System.String"));
            DT.Columns.Add("MasterCust", System.Type.GetType("System.String"));
            DT.Columns.Add("CustomerCode", System.Type.GetType("System.String"));
            DT.Columns.Add("DeliveryDate", System.Type.GetType("System.String"));
            DT.Columns.Add("DeliveryStatus", System.Type.GetType("System.String"));
            DT.Columns.Add("OrderStatus", System.Type.GetType("System.String"));
            DT.Columns.Add("MovementStatus", System.Type.GetType("System.String"));
            DT.Columns.Add("BillingStatus", System.Type.GetType("System.String"));
            DT.Columns.Add("DocumentNumber", System.Type.GetType("System.String"));

            foreach (IRfcStructure row in detail)
            {
                DataRow _row = DT.NewRow();

                _row[0] = row.GetValue("TRAN_ID").ToString();
                //if (_row[0].ToString().Trim().Equals(string.Empty)) continue;
                _row[1] = row.GetValue("MASTER_CUST").ToString();
                _row[2] = row.GetValue("CUST_CODE").ToString();
                _row[3] = row.GetValue("DEL_DATE").ToString();
                _row[4] = row.GetValue("DEL_STATUS").ToString();
                _row[5] = row.GetValue("SO_STATUS").ToString();
                _row[6] = row.GetValue("PGI_STATUS").ToString();
                _row[7] = row.GetValue("INV_STATUS").ToString();
                _row[8] = row.GetValue("VBELN").ToString();
                DT.Rows.Add(_row);
                DT.AcceptChanges();
            }

            return DT;
        }

        private DataTable GetPriceTable()
        {
            DataTable DT = new DataTable();
            try
            {
                //IntegrationForm.lblProgress.Text = "Calling ZHOB_GET_PRICE_MASTER...";

                ///// Passing a table from SAP to .Net //////
                RfcDestination prd = RfcDestinationManager.GetDestination(GetServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_PRICE_MASTER");
                IRfcTable tblImport = companyBapi.GetTable("IR_VKORG");
                //HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              

                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", IntegrationOrg);
                //tblImport.SetValue("HIGH", "1200");
                //tblImport.Append();
                //tblImport.SetValue("SIGN", "I");
                //tblImport.SetValue("OPTION", "EQ");
                //tblImport.SetValue("LOW", "1200");
                companyBapi.SetValue("IR_VKORG", tblImport);

                //IRfcTable tblImport5 = companyBapi.GetTable("IR_DATUM");
                ////HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
                //tblImport5.Append();
                //tblImport5.SetValue("SIGN", "I");
                //tblImport5.SetValue("OPTION", "BT");
                //tblImport5.SetValue("LOW", DateTime.Today.AddDays(-10).ToString("yyyyMMdd"));
                //tblImport5.SetValue("HIGH", DateTime.Today.ToString("yyyyMMdd"));
                //companyBapi.SetValue("IR_DATUM", tblImport5);

                //IRfcTable tblImport2 = companyBapi.GetTable("IR_MATNR");
                //tblImport2.Append();
                //tblImport2.SetValue("SIGN", "I");
                //tblImport2.SetValue("OPTION", "EQ");
                //tblImport2.SetValue("LOW", "000000000011040004");
                //companyBapi.SetValue("IR_MATNR", tblImport2);
                //IRfcTable tblImport2 = companyBapi.GetTable("IR_KONDA");
                ////HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
                //tblImport2.Append();
                //tblImport2.SetValue("SIGN", "I");
                //tblImport2.SetValue("OPTION", "EQ");
                //tblImport2.SetValue("LOW", "10");
                ////tblImport.SetValue("HIGH", "1200");
                //companyBapi.SetValue("IR_KONDA", tblImport2);
                //IRfcTable tblImport6 = companyBapi.GetTable("IR_SPART");
                ////HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "D4");
                //tblImport6.SetValue("HIGH", "D4");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "D3");
                //tblImport6.SetValue("HIGH", "D3");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "15");
                //tblImport6.SetValue("HIGH", "15");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "20");
                //tblImport6.SetValue("HIGH", "20");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "25");
                //tblImport6.SetValue("HIGH", "25");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "35");
                //tblImport6.SetValue("HIGH", "35");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "40");
                //tblImport6.SetValue("HIGH", "40");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "45");
                //tblImport6.SetValue("HIGH", "45");
                //tblImport6.Append();
                //tblImport6.SetValue("SIGN", "I");
                //tblImport6.SetValue("OPTION", "EQ");
                //tblImport6.SetValue("LOW", "99");
                //tblImport6.SetValue("HIGH", "99");
                //companyBapi.SetValue("IR_SPART", tblImport6);

                companyBapi.Invoke(prd);
                IRfcTable detail = companyBapi.GetTable("ITEMTAB");

                DT.Columns.Add("CompanyCode", System.Type.GetType("System.String"));
                DT.Columns.Add("CompanyCodeLevel3", System.Type.GetType("System.String"));
                DT.Columns.Add("PriceListName", System.Type.GetType("System.String"));
                DT.Columns.Add("ValidFrom", System.Type.GetType("System.String"));
                DT.Columns.Add("ValidTo", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemCode", System.Type.GetType("System.String"));
                DT.Columns.Add("UOM", System.Type.GetType("System.String"));
                DT.Columns.Add("ConversionFactor", System.Type.GetType("System.String"));
                DT.Columns.Add("Price", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerCode", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesGroupCode", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ChannelCode", System.Type.GetType("System.String"));
                DT.Columns.Add("StockStatus", System.Type.GetType("System.String"));
                DT.Columns.Add("FLAG", System.Type.GetType("System.String"));
                DT.Columns.Add("IsDeleted", System.Type.GetType("System.String"));

                ClearProgress();
                SetProgressMax(detail.Count);


                foreach (IRfcStructure row in detail)
                {
                    ReportProgress("Filling SAP Results");
                    if (row.GetValue("KONDA").ToString().Trim().Equals(string.Empty) && row.GetValue("KUNWE").ToString().Trim().Equals(string.Empty) && !row.GetValue("SPART").ToString().Trim().Equals(string.Empty))
                    {

                    }
                    else
                    {
                        //continue;
                    }

                    DataRow _row = DT.NewRow();

                    _row["CompanyCode"] = row.GetValue("BUKRS").ToString();
                    _row["CompanyCodeLevel3"] = row.GetValue("VKORG").ToString();
                    _row["PriceListName"] = row.GetValue("OBJKY").ToString();
                    _row["ValidFrom"] = row.GetValue("DATAB").ToString();
                    _row["ValidTo"] = row.GetValue("DATBI").ToString();
                    _row["ItemCode"] = row.GetValue("MATNR").ToString().Trim().Substring(10, 8);
                    //if (!row.GetValue("MATNR").ToString().Trim().Substring(10, 8).Equals("77030040")) continue;
                    _row["UOM"] = row.GetValue("KMEIN").ToString();
                    _row["ConversionFactor"] = row.GetValue("CONVF").ToString();
                    _row["Price"] = row.GetValue("KBETR").ToString();
                    if (!row.GetValue("KUNWE").ToString().Trim().Equals(string.Empty))
                    {
                        _row["CustomerCode"] = row.GetValue("KUNWE").ToString().Trim().Substring(4, 6);
                    }
                    else
                    {
                        _row["CustomerCode"] = row.GetValue("KUNWE").ToString().Trim();
                    }//.ToString().Trim().Substring(4, 6); //10/40/1100
                    if (!row.GetValue("KONDA").ToString().Trim().Equals(string.Empty))
                    {
                        _row["SalesGroupCode"] = row.GetValue("KONDA").ToString() + "/" + row.GetValue("SPART").ToString();
                    }
                    else
                    {
                        _row["SalesGroupCode"] = row.GetValue("KONDA").ToString();
                    }
                    _row["DivisionCode"] = row.GetValue("SPART").ToString();
                    _row["ChannelCode"] = row.GetValue("VTWEG").ToString();
                    _row["StockStatus"] = "";
                    _row["FLAG"] = row.GetValue("ZFLAG").ToString();
                    _row["IsDeleted"] = row.GetValue("LOEVM").ToString();
                    //if (_row["DivisionCode"].ToString().Trim().Equals("40") && _row["SalesGroupCode"].ToString().Trim().Equals("10") && _row["CompanyCodeLevel3"].ToString().Trim().Equals("1100"))
                    //{ ; }
                    //else { continue; }//
                    DT.Rows.Add(_row);
                    DT.AcceptChanges();
                }
            }
            catch (Exception ex)
            {
                WriteExceptions("EXCEPTION HAPPENED IN THE GetPriceTable() FUNCTION, THE MESSAGE IS <<" + ex.Message + ">>", "UPDATE REMAINING AMOUNT", false);
            }
            return DT;
        }

        private DataTable GetPromotionTable()
        {
            RfcDestination prd = RfcDestinationManager.GetDestination(GetServerName);
            RfcRepository repo = prd.Repository;
            IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_FOC_MASTER");

            //IRfcTable tblImport2 = companyBapi.GetTable("IR_MATNR");
            //tblImport2.Append();
            //tblImport2.SetValue("SIGN", "I");
            //tblImport2.SetValue("OPTION", "EQ");
            //tblImport2.SetValue("LOW", "000000000010160015");
            //companyBapi.SetValue("IR_MATNR", tblImport2);

            IRfcTable tblImport = companyBapi.GetTable("IR_VKORG");
            //HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
            tblImport.Append();
            tblImport.SetValue("SIGN", "I");
            tblImport.SetValue("OPTION", "EQ");
            tblImport.SetValue("LOW", IntegrationOrg);
            //tblImport.SetValue("HIGH", "1200");
            companyBapi.SetValue("IR_VKORG", tblImport);

            //IRfcTable tblImport5 = companyBapi.GetTable("IR_DATUM");
            ////HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              
            //tblImport5.Append();
            //tblImport5.SetValue("SIGN", "I");
            //tblImport5.SetValue("OPTION", "BT");
            //tblImport5.SetValue("LOW", DateTime.Today.AddDays(-1).ToString("yyyyMMdd"));
            //tblImport5.SetValue("HIGH", DateTime.Today.ToString("yyyyMMdd"));
            //companyBapi.SetValue("IR_DATUM", tblImport5);

            companyBapi.Invoke(prd);
            IRfcTable detail = companyBapi.GetTable("ITEMTAB");

            DataTable DT = new DataTable();
            DT.Columns.Add("Companycode ", System.Type.GetType("System.String"));
            DT.Columns.Add("CompanyCodeLevel3", System.Type.GetType("System.String"));
            DT.Columns.Add("ValidFrom", System.Type.GetType("System.String"));
            DT.Columns.Add("ValidTo", System.Type.GetType("System.String"));
            DT.Columns.Add("BuyItemCode", System.Type.GetType("System.String"));
            DT.Columns.Add("BuyUOM", System.Type.GetType("System.String"));
            DT.Columns.Add("BuyConversionFactor", System.Type.GetType("System.String"));
            DT.Columns.Add("BuyQuantity", System.Type.GetType("System.String"));
            DT.Columns.Add("GetItemCode", System.Type.GetType("System.String"));
            DT.Columns.Add("GetUOM", System.Type.GetType("System.String"));
            DT.Columns.Add("GetConversionFactor", System.Type.GetType("System.String"));
            DT.Columns.Add("GetQuantity", System.Type.GetType("System.String"));
            DT.Columns.Add("DivisionCode", System.Type.GetType("System.String"));
            DT.Columns.Add("SalesGroupCode", System.Type.GetType("System.String"));
            DT.Columns.Add("CustomerShipTo", System.Type.GetType("System.String"));
            DT.Columns.Add("InclusiveOrExclusive", System.Type.GetType("System.String"));
            DT.Columns.Add("FLAG", System.Type.GetType("System.String"));
            DT.Columns.Add("IsDeleted", System.Type.GetType("System.String"));


            foreach (IRfcStructure row in detail)
            {
                DataRow _row = DT.NewRow();
                _row["Companycode "] = row.GetValue("BUKRS").ToString();
                _row["CompanyCodeLevel3"] = row.GetValue("VKORG").ToString();
                _row["ValidFrom"] = row.GetValue("DATAB").ToString();
                _row["ValidTo"] = row.GetValue("DATBI").ToString();
                _row["BuyItemCode"] = row.GetValue("MAT01").ToString().Trim().Substring(10, 8);
                _row["BuyUOM"] = row.GetValue("UOM01").ToString();
                _row["BuyConversionFactor"] = row.GetValue("CNV01").ToString();
                _row["BuyQuantity"] = row.GetValue("QTY01").ToString();
                _row["GetItemCode"] = row.GetValue("MAT02").ToString().Trim().Substring(10, 8);
                _row["GetUOM"] = row.GetValue("UOM02").ToString();
                _row["GetConversionFactor"] = row.GetValue("CNV02").ToString();
                _row["GetQuantity"] = row.GetValue("QTY02").ToString();
                _row["DivisionCode"] = row.GetValue("SPART").ToString();
                if (!row.GetValue("KONDA").ToString().Trim().Equals(string.Empty))
                {
                    _row["SalesGroupCode"] = row.GetValue("KONDA").ToString() + "/" + row.GetValue("SPART").ToString();
                }
                else
                {
                    _row["SalesGroupCode"] = row.GetValue("KONDA").ToString();
                }
                if (!row.GetValue("KUNWE").ToString().Trim().Equals(string.Empty))
                {
                    _row["CustomerShipTo"] = row.GetValue("KUNWE").ToString().Trim().Substring(4, 6);
                }

                _row["InclusiveOrExclusive"] = row.GetValue("KNRDD").ToString();
                _row["FLAG"] = row.GetValue("ZFLAG").ToString();
                _row["IsDeleted"] = row.GetValue("LOEVM").ToString();

                DT.Rows.Add(_row);
                DT.AcceptChanges();
            }

            return DT;
        }

        private DataTable GetSalespersonTable()
        {
            DataTable DT = new DataTable();
            try
            {

                RfcDestination prd = RfcDestinationManager.GetDestination(GetServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_SALESMAN_MASTER");
                IRfcTable tblImport = companyBapi.GetTable("IR_BUKRS");
                //HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              

                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", IntegrationOrg);
                //tblImport.Append();
                //tblImport.SetValue("SIGN", "I");
                //tblImport.SetValue("OPTION", "EQ");
                //tblImport.SetValue("LOW", "1200");
                //tblImport.Append();
                //tblImport.SetValue("SIGN", "I");
                //tblImport.SetValue("OPTION", "EQ");
                //tblImport.SetValue("LOW", "9900");
                //tblImport.SetValue("HIGH", "1200");
                companyBapi.SetValue("IR_BUKRS", tblImport);
                companyBapi.Invoke(prd);
                IRfcTable detail = companyBapi.GetTable("ITEMTAB");

                DT.Columns.Add("SalesmanCode", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesmanName", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesmanPhone", System.Type.GetType("System.String"));
                DT.Columns.Add("SupervisorCode", System.Type.GetType("System.String"));
                DT.Columns.Add("SuperVisorName", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesManagerCode", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesManagerName", System.Type.GetType("System.String"));
                DT.Columns.Add("OrganizationCode", System.Type.GetType("System.String"));
                DT.Columns.Add("Status", System.Type.GetType("System.String"));

                foreach (IRfcStructure row in detail)
                {
                    DataRow _row = DT.NewRow();

                    _row["SalesmanCode"] = row.GetValue("PERNR").ToString();
                    _row["SalesmanName"] = row.GetValue("ENAME").ToString();
                    _row["SalesmanPhone"] = row.GetValue("TELF1").ToString();
                    _row["SupervisorCode"] = row.GetValue("ENSPV").ToString();
                    _row["SuperVisorName"] = row.GetValue("NMSPV").ToString();
                    _row["SalesManagerCode"] = row.GetValue("ENMGR").ToString();
                    _row["SalesManagerName"] = row.GetValue("NMNGR").ToString();
                    _row["OrganizationCode"] = row.GetValue("BUKRS").ToString();
                    _row["Status"] = row.GetValue("ZFLAG").ToString();

                    DT.Rows.Add(_row);
                    DT.AcceptChanges();
                }
            }
            catch (Exception ex)
            {
                WriteExceptions("EXCEPTION HAPPENED IN THE GetSalespersonTable() FUNCTION, THE MESSAGE IS <<" + ex.Message + ">>", "UPDATE REMAINING AMOUNT", false);
            }

            return DT;
        }

        private DataTable GetStockTable()
        {
            ///// Passing a table from SAP to .Net //////
            RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
            RfcRepository repo = prd.Repository;
            IRfcFunction companyBapi = repo.CreateFunction("ZSD_FM_PDT_STOCK");
            // Passing the Parameter value

            //companyBapi.SetValue("CR_DATE", "09/10/2011");
            //companyBapi.SetValue("MOD_DATE", "09/10/2011");
            companyBapi.Invoke(prd);

            IRfcTable detail = companyBapi.GetTable("EX_ITEMS"); // returning table

            //string WarehouseCode = detail.GetString("WH_CODE");
            //string ItemCode = detail.GetString("ITEM_CODE");
            //string UOM = detail.GetString("UOM");
            //string ExpiryDate = detail.GetString("EXP_DATE");
            //string BatchNo = detail.GetString("BATCH");
            //string Qty = detail.GetString("QTY");

            DataTable DT = new DataTable();
            DT.Columns.Add("WarehouseCode", System.Type.GetType("System.String"));
            DT.Columns.Add("ItemCode", System.Type.GetType("System.String"));
            DT.Columns.Add("UOM", System.Type.GetType("System.String"));
            DT.Columns.Add("ExpiryDate", System.Type.GetType("System.String"));
            DT.Columns.Add("BatchNo", System.Type.GetType("System.String"));
            DT.Columns.Add("Qty", System.Type.GetType("System.String"));

            foreach (IRfcStructure row in detail)
            {
                DataRow _row = DT.NewRow();

                _row[0] = row.GetValue("WH_CODE").ToString();
                _row[1] = row.GetValue("ITEM_CODE").ToString();
                _row[2] = row.GetValue("UOM").ToString();
                _row[3] = row.GetValue("EXP_DATE").ToString();
                _row[4] = row.GetValue("BATCH").ToString();
                _row[5] = row.GetValue("QTY").ToString();

                DT.Rows.Add(_row);
                DT.AcceptChanges();
            }

            return DT;
        }

        private DataTable GetUOMTable()
        {
            DataTable DT = new DataTable();
            try
            {
                ///// Passing a table from SAP to .Net //////
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZSD_FM_PDT_UOM_N");
                // Passing the Parameter value

                companyBapi.SetValue("CR_DATE", _modificationDate.ToString(DateFormtSAP));
                //companyBapi.SetValue("MOD_DATE", _modificationDate.ToString(DateFormtSAP));
                companyBapi.Invoke(prd);

                IRfcTable detail = companyBapi.GetTable("EX_ITEMS"); // returning table

                //string itemCode = detail.GetString("ITEM_CODE");
                //string UOM = detail.GetString("UOM");
                //string creationDate = detail.GetString("CR_DATE");
                //string UOMDesc = detail.GetString("UOM_DESC");
                //string ConvFac = detail.GetString("CONV_RULE");

                DT.Columns.Add("ItemCode", System.Type.GetType("System.String"));
                DT.Columns.Add("UOM", System.Type.GetType("System.String"));
                DT.Columns.Add("CreationDate", System.Type.GetType("System.String"));
                DT.Columns.Add("UOMDesc", System.Type.GetType("System.String"));
                DT.Columns.Add("ConvFac", System.Type.GetType("System.String"));
                DT.Columns.Add("GroupName", System.Type.GetType("System.String"));
                foreach (IRfcStructure row in detail)
                {
                    DataRow _row = DT.NewRow();

                    _row[0] = row.GetValue("ITEM_CODE").ToString();
                    _row[1] = row.GetValue("UOM").ToString();
                    _row[2] = row.GetValue("CR_DATE").ToString();
                    _row[3] = row.GetValue("UOM_DESC").ToString();
                    _row[4] = row.GetValue("CONV_RULE").ToString();
                    _row[5] = row.GetValue("ITEM_GRP").ToString();
                    DT.Rows.Add(_row);
                    DT.AcceptChanges();
                }
            }
            catch
            {
            }
            return DT;
        }

        private DataTable GetWarehouseTable()
        {
            DataTable DT = new DataTable();
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(GetServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_LOCATION_MASTER");
                IRfcTable tblImport = companyBapi.GetTable("IR_BUKRS");
                //HERE WE CREATE A TABLE PARAMETER TO PASS TP SAP FUNCTION.              

                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", IntegrationOrg);
                //tblImport.Append();
                //tblImport.SetValue("SIGN", "I");
                //tblImport.SetValue("OPTION", "EQ");
                //tblImport.SetValue("LOW", "1200");
                //tblImport.Append();
                //tblImport.SetValue("SIGN", "I");
                //tblImport.SetValue("OPTION", "EQ");
                //tblImport.SetValue("LOW", "9900");
                //tblImport.SetValue("HIGH", "1200");
                companyBapi.SetValue("IR_BUKRS", tblImport);
                companyBapi.Invoke(prd);
                IRfcTable detail = companyBapi.GetTable("ITEMTAB");
                DT.Columns.Add("COMPANYCODE", System.Type.GetType("System.String"));
                DT.Columns.Add("VehicleLocationCode", System.Type.GetType("System.String"));
                DT.Columns.Add("LoadingWarehouse", System.Type.GetType("System.String"));
                DT.Columns.Add("Flag", System.Type.GetType("System.String"));

                //DT.Columns.Add("CreationDate", System.Type.GetType("System.String"));
                //DT.Columns.Add("ModDate", System.Type.GetType("System.String"));

                foreach (IRfcStructure row in detail)
                {
                    DataRow _row = DT.NewRow();
                    _row["COMPANYCODE"] = row.GetValue("BUKRS").ToString();
                    _row["VehicleLocationCode"] = row.GetValue("RCODE").ToString();
                    _row["LoadingWarehouse"] = row.GetValue("RESWK").ToString();
                    _row["Flag"] = row.GetValue("ZFLAG").ToString();

                    DT.Rows.Add(_row);
                    DT.AcceptChanges();
                }
            }
            catch
            {

            }
            return DT;
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

        private void ReadConfigurations()
        {
            try
            {
                if (configResult == InCubeErrors.NotInitialized)
                {
                    string CurrentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(CurrentDirectory + "\\DataSources.xml");
                    XmlNode node = xmlDoc.SelectSingleNode("Connections/EmployeeCode");
                    employeeCode = node.InnerText;
                    node = xmlDoc.SelectSingleNode("Connections/DigitsCount");
                    digitsCount = Convert.ToInt32(node.InnerText);
                    if (employeeCode != string.Empty && digitsCount != -1)
                    {
                        configResult = InCubeErrors.Success;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                configResult = InCubeErrors.Error;
            }
        }

        private void ResetAccountBalance(string query)
        {
            object field = null;
            InCubeQuery ResetBalance = new InCubeQuery(query, db_vms);
            ResetBalance.Execute();
            err = ResetBalance.FindFirst();

            while (err == InCubeErrors.Success)
            {
                ResetBalance.GetField(0, ref field);
                string _accid = field.ToString();

                QueryBuilderObject.SetField("Balance", "0");
                QueryBuilderObject.UpdateQueryString("account", " AccountID = " + _accid, db_vms);

                err = ResetBalance.FindNext();
            }

            ResetBalance.Close();
        }

        private void UpdatePriceFromSAP()
        {
            try
            {
                if (DateTime.Now.TimeOfDay.Hours >= 1 && DateTime.Now.TimeOfDay.Hours < 5)
                {
                    string Price_END_DATE = "SP_PriceENDDATE";
                    InCubeQuery TEMP_QRY = new InCubeQuery(Price_END_DATE, db_vms);
                    err = TEMP_QRY.ExecuteStoredProcedure();
                    if (err == InCubeErrors.Success)
                    {
                        WriteExceptions("SP_PriceENDDATE PROCEDURE WAS SUCCESSFULLY EXECUTED", "SP_PriceENDDATE", false);
                    }
                    else
                    {
                        WriteExceptions("SP_PriceENDDATE PROCEDURE WAS FAILED", "SP_PriceENDDATE", false);
                    }
                }

                object field = null;
                List<string> appliedList = new List<string>();
                DataTable TBL = GetPriceTable();
                InCubeQuery QRY;
                DataTable INNER = new DataTable();

                #region CHANNEL Price
                ClearProgress();
                SetProgressMax(TBL.Select("ChannelCode is not null and ChannelCode<>'' and CustomerCode='' and SalesGroupCode='' ").Length);

                #endregion

                #region GROUP Price
                ClearProgress();
                SetProgressMax(TBL.Select("SalesGroupCode is not null and SalesGroupCode<>'' and ChannelCode='' and CustomerCode='' and len(SalesGroupCode)>3 ").Length);
                foreach (DataRow dr in TBL.Select("SalesGroupCode is not null and SalesGroupCode<>'' and ChannelCode='' and CustomerCode=''  "))
                {
                    ReportProgress("Filling Group Prices");

                    string CompanyCode = dr["CompanyCode"].ToString().Trim();
                    string CompanyCodeLevel3 = dr["CompanyCodeLevel3"].ToString().Trim();
                    string PriceListName = dr["PriceListName"].ToString().Trim();
                    string ValidFrom = dr["ValidFrom"].ToString().Trim();
                    string ValidTo = dr["ValidTo"].ToString().Trim();
                    string ItemCode = dr["ItemCode"].ToString().Trim();
                    string UOM = dr["UOM"].ToString().Trim();
                    string ConversionFactor = dr["ConversionFactor"].ToString().Trim();
                    string Price = dr["Price"].ToString().Trim();
                    string CustomerCode = dr["CustomerCode"].ToString().Trim();
                    string SalesGroupCode = dr["SalesGroupCode"].ToString().Trim();
                    string DivisionCode = dr["DivisionCode"].ToString().Trim();
                    //if (!DivisionCode.Equals("15")) continue;
                    string ChannelCode = dr["ChannelCode"].ToString().Trim();
                    string StockStatus = dr["StockStatus"].ToString().Trim();
                    string FLAG = dr["FLAG"].ToString().Trim();
                    string IsDeleted = dr["IsDeleted"].ToString().Trim();
                    string BATCH = string.Empty;
                    string BEXP_DATE = string.Empty;
                    //if (GetFieldValue("QNIE_ActiveItems", "ItemCode", "ItemCode='" + ItemCode + "'", db_vms).Trim().Equals(string.Empty)) continue;
                    string MiscOrgID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + CompanyCodeLevel3 + "'", db_vms).Trim();
                    if (!ValidTo.Equals(string.Empty))
                        ValidTo = ValidTo.Replace("9999", DateTime.Today.Year.ToString());
                    DateTime PriceCreatDate = DateTime.Parse(ValidFrom);
                    DateTime PriceEndDate = DateTime.Parse(ValidTo);


                    string definitionBatch = string.Empty;
                    string PriceListTypeID = string.Empty;
                    PriceListTypeID = "1";

                    string checkPricelistExist = GetFieldValue("PriceList", "PriceListID", "PriceListCode='" + SalesGroupCode + "/" + DivisionCode + "/" + CompanyCodeLevel3 + "' and PriceListTypeID=" + PriceListTypeID + "", db_vms).Trim();
                    string PriceListID = string.Empty;
                    if (!checkPricelistExist.Equals(string.Empty))
                    {
                        PriceListID = checkPricelistExist;// GetFieldValue("PriceListLanguage", "PriceListID", " Description = '" + PriceListName + "'", db_vms);// #####
                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");//*********
                        QueryBuilderObject.SetField("Priority", "3");
                        err = QueryBuilderObject.UpdateQueryString("PriceList", "PriceListID=" + PriceListID + "", db_vms);

                        QueryBuilderObject.SetField("Description", "'" + SalesGroupCode + "/" + DivisionCode + "/" + CompanyCodeLevel3 + "'");//#####
                        err = QueryBuilderObject.UpdateQueryString("PriceListLanguage", "PriceListID=" + PriceListID + "", db_vms);//#####
                    }
                    else
                    {
                        PriceListID = GetFieldValue("PriceList", "ISNULL(MAX(PriceListID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("PriceListCode", "'" + SalesGroupCode + "/" + DivisionCode + "/" + CompanyCodeLevel3 + "'");
                        QueryBuilderObject.SetField("StartDate", "'" + DateTime.Now.Date.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("Priority", "3");
                        QueryBuilderObject.SetField("PriceListTypeID", PriceListTypeID);
                        QueryBuilderObject.SetField("OrganizationID", MiscOrgID);//TO BE ADDED TO THE NEW RELEASE INTEGRATION
                        err = QueryBuilderObject.InsertQueryString("PriceList", db_vms);

                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + SalesGroupCode + "/" + DivisionCode + "/" + CompanyCodeLevel3 + "'");
                        err = QueryBuilderObject.InsertQueryString("PriceListLanguage", db_vms);
                    }
                    string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + UOM + "' AND LanguageID = 1", db_vms);
                    if (PackTypeID.Equals(string.Empty))
                    {
                        continue;
                    }
                    ExtendItem(ItemCode, DivisionCode, MiscOrgID);//THIS FUNCTION WAS ADDED TO CREATE THE ITEM AGAIN UNDER DIFFERENT DIVISIONS
                    string PACKID = GetFieldValue("Pack", "PACKID", @"ItemID in (select ItemID from Item where ItemCode='" + ItemCode + "' and ItemCategoryID in (select ItemCategoryID from ItemCategory where DivisionID in (select DivisionID from Division where DivisionCode='" + DivisionCode + "' and OrganizationID=" + MiscOrgID + "))) and PackTypeID in (select PackTypeID from PackTypeLanguage where Description ='" + UOM + "') ", db_vms).Trim();
                    if (PACKID.Equals(string.Empty)) continue;
                    string PackQuantity = GetFieldValue("Pack", "Quantity", "ItemID in (select ItemID from Item where ItemCode='" + ItemCode + "')  AND PackTypeID = " + PackTypeID + "", db_vms);
                    InCubeQuery PackQuery = new InCubeQuery(db_vms, @"
SELECT
Pack.PACKID,
Pack.Quantity,
Pack.PackTypeID,
PackTypeLanguage.Description
FROM Pack INNER JOIN
Item ON Pack.ItemID = Item.ItemID INNER JOIN
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID
INNER JOIN ITEMCATEGORY IC ON IC.ITEMCATEGORYID=ITEM.ITEMCATEGORYID
INNER JOIN DIVISION DIV ON DIV.DIVISIONID=IC.DIVISIONID
INNER JOIN ORGANIZATION O ON O.ORGANIZATIONID=DIV.ORGANIZATIONID
WHERE DIV.DIVISIONCODE='" + DivisionCode + @"' AND O.ORGANIZATIONID=" + MiscOrgID + @" AND
Item.ItemCode = '" + ItemCode.Trim() + @"' AND
PackTypeLanguage.LanguageID = 1");

                    PackQuery.Execute();
                    err = PackQuery.FindFirst();

                    while (err == InCubeErrors.Success)
                    {
                        decimal loopPrice = decimal.Parse(Price);

                        PackQuery.GetField(0, ref field);
                        PACKID = field.ToString();

                        PackQuery.GetField(1, ref field);
                        decimal ConversionFactor2 = decimal.Parse(field.ToString());

                        PackQuery.GetField(2, ref field);
                        string LoopPackTypeID = field.ToString().Trim();

                        if (LoopPackTypeID != PackTypeID)
                        {
                            loopPrice = Math.Round(ConversionFactor2 * (decimal.Parse(Price) / decimal.Parse(PackQuantity)), 3);
                        }

                        int PriceDefinitionID = 1;
                        string currentPrice = GetFieldValue("PriceDefinition", "Price", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID + " " + definitionBatch + "", db_vms);
                        if (currentPrice.Equals(string.Empty))
                        {
                            PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms));

                            QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID.ToString());
                            QueryBuilderObject.SetField("QuantityRangeID", "1");
                            QueryBuilderObject.SetField("PACKID", PACKID);
                            QueryBuilderObject.SetField("CurrencyID", "1");
                            QueryBuilderObject.SetField("Tax", "0");
                            QueryBuilderObject.SetField("Price", loopPrice.ToString());
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
                            if (!currentPrice.Equals(Price.ToString()))
                            {
                                QueryBuilderObject.SetField("Price", loopPrice.ToString());
                                QueryBuilderObject.UpdateQueryString("PriceDefinition", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID + " AND PriceDefinitionID = " + PriceDefinitionID + " " + definitionBatch + "", db_vms);
                            }
                        }
                        //THE FOLLOWING FUNCTION IS TO FILL THE PriceENDDATE TABLE WHICH WILL LATER DELETE THE EXPIRED PriceS
                        PricePeriodControl(PriceDefinitionID.ToString(), PriceCreatDate, PriceEndDate);
                        err = PackQuery.FindNext();
                    }
                    if (!appliedList.Contains(SalesGroupCode + DivisionCode + CompanyCodeLevel3)) { appliedList.Add(SalesGroupCode + DivisionCode + CompanyCodeLevel3); } else { continue; }
                    string GroupIDExist = GetFieldValue("CustomerGroup", "GroupID", "GroupCode='" + SalesGroupCode + "'", db_vms).Trim();
                    string checkCHLPrc = GetFieldValue("GroupPrice", "GroupID", "GroupID=" + GroupIDExist + " and PriceListID=" + PriceListID + "", db_vms).Trim();
                    if (checkCHLPrc.Equals(string.Empty))
                    {
                        QueryBuilderObject.SetField("GroupID", GroupIDExist);
                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        err = QueryBuilderObject.InsertQueryString("GroupPrice", db_vms);
                    }

                }
                #endregion

                #region CUSTOMER Price
                ClearProgress();
                SetProgressMax(TBL.Select("CustomerCode is not null and CustomerCode<>''").Length);

                foreach (DataRow dr in TBL.Select("CustomerCode is not null and CustomerCode<>''"))
                {
                    ReportProgress("Filling Customer Prices");
                    string CompanyCode = dr["CompanyCode"].ToString().Trim();
                    string CompanyCodeLevel3 = dr["CompanyCodeLevel3"].ToString().Trim();
                    string PriceListName = dr["PriceListName"].ToString().Trim();
                    string ValidFrom = dr["ValidFrom"].ToString().Trim();
                    string ValidTo = dr["ValidTo"].ToString().Trim();
                    string ItemCode = dr["ItemCode"].ToString().Trim();
                    string UOM = dr["UOM"].ToString().Trim();
                    string ConversionFactor = dr["ConversionFactor"].ToString().Trim();
                    string Price = dr["Price"].ToString().Trim();
                    string CustomerCode = dr["CustomerCode"].ToString().Trim();
                    //if (!CustomerCode.Equals("700071")) continue;
                    string SalesGroupCode = dr["SalesGroupCode"].ToString().Trim();
                    string DivisionCode = dr["DivisionCode"].ToString().Trim();
                    string ChannelCode = dr["ChannelCode"].ToString().Trim();
                    string StockStatus = dr["StockStatus"].ToString().Trim();
                    string FLAG = dr["FLAG"].ToString().Trim();
                    string IsDeleted = dr["IsDeleted"].ToString().Trim();
                    string BATCH = string.Empty;
                    string BEXP_DATE = string.Empty;
                    //if (GetFieldValue("QNIE_ActiveItems", "ItemCode", "ItemCode='" + ItemCode + "'", db_vms).Trim().Equals(string.Empty)) continue;
                    string MiscOrgID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + CompanyCodeLevel3 + "'", db_vms).Trim();
                    if (!ValidTo.Equals(string.Empty))
                        ValidTo = ValidTo.Replace("9999", DateTime.Today.Year.ToString());
                    DateTime PriceCreatDate = DateTime.Parse(ValidFrom);
                    DateTime PriceEndDate = DateTime.Parse(ValidTo);

                    string definitionBatch = string.Empty;
                    string PriceListTypeID = string.Empty;
                    PriceListTypeID = "1";
                    if (!BATCH.Equals(string.Empty)) { PriceListTypeID = "4"; definitionBatch = "and BatchNo='" + BATCH + "'"; }
                    if (BEXP_DATE.Equals(string.Empty)) { BEXP_DATE = "1990/01/01"; }

                    string checkPricelistExist = GetFieldValue("PriceList", "PriceListID", "PriceListCode='" + CustomerCode + "/" + DivisionCode + "/" + CompanyCodeLevel3 + "' and PriceListTypeID=" + PriceListTypeID + "", db_vms).Trim();
                    string PriceListID = string.Empty;
                    if (!checkPricelistExist.Equals(string.Empty))
                    {
                        PriceListID = checkPricelistExist;// GetFieldValue("PriceListLanguage", "PriceListID", " Description = '" + PriceListName + "'", db_vms);// #####
                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");//*********
                        QueryBuilderObject.SetField("Priority", "1");
                        err = QueryBuilderObject.UpdateQueryString("PriceList", "PriceListID=" + PriceListID + "", db_vms);

                        QueryBuilderObject.SetField("Description", "'" + CustomerCode + "/" + DivisionCode + "/" + CompanyCodeLevel3 + "'");//#####
                        err = QueryBuilderObject.UpdateQueryString("PriceListLanguage", "PriceListID=" + PriceListID + "", db_vms);//#####
                    }
                    else
                    {
                        PriceListID = GetFieldValue("PriceList", "ISNULL(MAX(PriceListID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("PriceListCode", "'" + CustomerCode + "/" + DivisionCode + "/" + CompanyCodeLevel3 + "'");
                        QueryBuilderObject.SetField("StartDate", "'" + DateTime.Now.Date.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("Priority", "1");
                        QueryBuilderObject.SetField("PriceListTypeID", PriceListTypeID);
                        QueryBuilderObject.SetField("OrganizationID", MiscOrgID);//TO BE ADDED TO THE NEW RELEASE INTEGRATION
                        err = QueryBuilderObject.InsertQueryString("PriceList", db_vms);

                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + CustomerCode + "'");
                        err = QueryBuilderObject.InsertQueryString("PriceListLanguage", db_vms);
                    }

                    string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + UOM + "' AND LanguageID = 1", db_vms);
                    if (PackTypeID.Equals(string.Empty))
                    {
                        continue;
                    }
                    ExtendItem(ItemCode, DivisionCode, MiscOrgID);//THIS FUNCTION WAS ADDED TO CREATE THE ITEM AGAIN UNDER DIFFERENT DIVISIONS
                    string PACKID = GetFieldValue("Pack", "PACKID", @"ItemID in (select ItemID from Item where ItemCode='" + ItemCode + "' and ItemCategoryID in (select ItemCategoryID from ItemCategory where DivisionID in (select DivisionID from Division where DivisionCode='" + DivisionCode + "' and OrganizationID=" + MiscOrgID + "))) and PackTypeID in (select PackTypeID from PackTypeLanguage where Description ='" + UOM + "') ", db_vms).Trim();
                    if (PACKID.Equals(string.Empty)) continue;
                    string PackQuantity = GetFieldValue("Pack", "Quantity", "ItemID in (select ItemID from Item where ItemCode='" + ItemCode + "')  AND PackTypeID = " + PackTypeID + "", db_vms);
                    InCubeQuery PackQuery = new InCubeQuery(db_vms, @"
SELECT
Pack.PACKID,
Pack.Quantity,
Pack.PackTypeID,
PackTypeLanguage.Description
FROM Pack INNER JOIN
Item ON Pack.ItemID = Item.ItemID INNER JOIN
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID
INNER JOIN ITEMCATEGORY IC ON IC.ITEMCATEGORYID=ITEM.ITEMCATEGORYID
INNER JOIN DIVISION DIV ON DIV.DIVISIONID=IC.DIVISIONID
INNER JOIN ORGANIZATION O ON O.ORGANIZATIONID=DIV.ORGANIZATIONID
WHERE DIV.DIVISIONCODE='" + DivisionCode + @"' AND O.ORGANIZATIONID=" + MiscOrgID + @" AND
Item.ItemCode = '" + ItemCode.Trim() + @"' AND
PackTypeLanguage.LanguageID = 1");

                    PackQuery.Execute();
                    err = PackQuery.FindFirst();

                    while (err == InCubeErrors.Success)
                    {
                        decimal loopPrice = decimal.Parse(Price);

                        PackQuery.GetField(0, ref field);
                        PACKID = field.ToString();

                        PackQuery.GetField(1, ref field);
                        decimal ConversionFactor2 = decimal.Parse(field.ToString());

                        PackQuery.GetField(2, ref field);
                        string LoopPackTypeID = field.ToString().Trim();

                        if (LoopPackTypeID != PackTypeID)
                        {
                            loopPrice = Math.Round(ConversionFactor2 * (decimal.Parse(Price) / decimal.Parse(PackQuantity)), 3);
                        }
                        int PriceDefinitionID = 1;
                        string currentPrice = GetFieldValue("PriceDefinition", "Price", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID + " " + definitionBatch + "", db_vms);
                        if (currentPrice.Equals(string.Empty))
                        {
                            PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms));

                            QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID.ToString());
                            QueryBuilderObject.SetField("QuantityRangeID", "1");
                            QueryBuilderObject.SetField("PACKID", PACKID);
                            QueryBuilderObject.SetField("CurrencyID", "1");
                            QueryBuilderObject.SetField("Tax", "0");
                            QueryBuilderObject.SetField("Price", loopPrice.ToString());
                            QueryBuilderObject.SetField("PriceListID", PriceListID);
                            if (!BATCH.Equals(string.Empty))
                            {
                                QueryBuilderObject.SetField("BatchNo", "'" + BATCH + "'");
                                QueryBuilderObject.SetField("ExpiryDate", "'" + DateTime.Parse(BEXP_DATE).ToString(DateFormat) + "'");
                            }
                            err = QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);
                        }
                        else
                        {
                            PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "PriceDefinitionID", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID + " " + definitionBatch + "", db_vms));
                            if (!currentPrice.Equals(Price.ToString()))
                            {
                                QueryBuilderObject.SetField("Price", loopPrice.ToString());
                                QueryBuilderObject.UpdateQueryString("PriceDefinition", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID + " AND PriceDefinitionID = " + PriceDefinitionID + " " + definitionBatch + "", db_vms);
                            }
                        }
                        //THE FOLLOWING FUNCTION IS TO FILL THE PriceENDDATE TABLE WHICH WILL LATER DELETE THE EXPIRED PriceS
                        PricePeriodControl(PriceDefinitionID.ToString(), PriceCreatDate, PriceEndDate);
                        err = PackQuery.FindNext();
                    }
                    string GetCustomers = string.Format(@"Select CO.CustomerID,CO.OutletID from CustomerOutlet CO
WHERE CO.CUSTOMERCODE='{0}'
 ", CustomerCode);
                    QRY = new InCubeQuery(GetCustomers, db_vms);
                    err = QRY.Execute();
                    INNER = QRY.GetDataTable();
                    if (CustomerCode.ToLower().Equals("0000401441"))
                    {
                    }
                    foreach (DataRow DR in INNER.Rows)
                    {
                        string CustomerID = DR["CustomerID"].ToString().Trim();
                        string OutletID = DR["OutletID"].ToString().Trim();
                        //if (CustomerID.Equals("4528")) { }
                        //QRY = new InCubeQuery("DELETE FROM CUSTOMERPrice WHERE CUSTOMERID=" + CustomerID + " AND OUTLETID=" + OutletID + " and PriceListID in (select PriceListID from Pricedefinition where packid=" + PACKID + ")", db_vms);
                        //err = QRY.ExecuteNonQuery();
                        QueryBuilderObject.SetField("CustomerID", CustomerID);
                        QueryBuilderObject.SetField("OutletID", OutletID);
                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        err = QueryBuilderObject.InsertQueryString("CustomerPrice", db_vms);
                    }
                }
                #endregion

                #region DEFAULT Price
                ClearProgress();
                SetProgressMax(TBL.Select("ChannelCode='' and CustomerCode='' and SalesGroupCode='' ").Length);
                foreach (DataRow dr in TBL.Select("ChannelCode='' and CustomerCode='' and SalesGroupCode='' "))
                {
                    ReportProgress("Filling Default Prices");
                    string CompanyCode = dr["CompanyCode"].ToString().Trim();
                    string CompanyCodeLevel3 = dr["CompanyCodeLevel3"].ToString().Trim();
                    string PriceListName = dr["PriceListName"].ToString().Trim();
                    string ValidFrom = dr["ValidFrom"].ToString().Trim();
                    string ValidTo = dr["ValidTo"].ToString().Trim();
                    string ItemCode = dr["ItemCode"].ToString().Trim();
                    string UOM = dr["UOM"].ToString().Trim();
                    string ConversionFactor = dr["ConversionFactor"].ToString().Trim();
                    string Price = dr["Price"].ToString().Trim();
                    string CustomerCode = dr["CustomerCode"].ToString().Trim();
                    string SalesGroupCode = dr["SalesGroupCode"].ToString().Trim();
                    string DivisionCode = dr["DivisionCode"].ToString().Trim();
                    string ChannelCode = dr["ChannelCode"].ToString().Trim();
                    string StockStatus = dr["StockStatus"].ToString().Trim();
                    string FLAG = dr["FLAG"].ToString().Trim();
                    string IsDeleted = dr["IsDeleted"].ToString().Trim();
                    //if (GetFieldValue("QNIE_ActiveItems", "ItemCode", "ItemCode='" + ItemCode + "'", db_vms).Trim().Equals(string.Empty)) continue;
                    string MiscOrgID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + CompanyCodeLevel3 + "'", db_vms).Trim();
                    string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode='" + DivisionCode + "' and OrganizationID=" + MiscOrgID + "", db_vms).Trim();
                    if (!ValidTo.Equals(string.Empty))
                        ValidTo = ValidTo.Replace("9999", DateTime.Today.Year.ToString());
                    DateTime PriceCreatDate = DateTime.Parse(ValidFrom);
                    DateTime PriceEndDate = DateTime.Parse(ValidTo).AddYears(5);


                    string checkPricelistExist = GetFieldValue("PriceListLanguage", "PriceListID", "Description='" + DivisionCode + "/" + CompanyCodeLevel3 + "'", db_vms).Trim();
                    string PriceListID = string.Empty;
                    if (!checkPricelistExist.Equals(string.Empty))
                    {
                        PriceListID = checkPricelistExist;// GetFieldValue("PriceListLanguage", "PriceListID", " Description = '" + PriceListName + "'", db_vms);// #####
                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");//*********
                        err = QueryBuilderObject.UpdateQueryString("PriceList", "PriceListID=" + PriceListID + "", db_vms);

                        QueryBuilderObject.SetField("Description", "'" + DivisionCode + "/" + CompanyCodeLevel3 + "'");//#####
                        err = QueryBuilderObject.UpdateQueryString("PriceListLanguage", "PriceListID=" + PriceListID + "", db_vms);//#####
                    }
                    else
                    {
                        PriceListID = GetFieldValue("PriceList", "ISNULL(MAX(PriceListID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("PriceListCode", "'" + DivisionCode + "/" + CompanyCodeLevel3 + "'");
                        QueryBuilderObject.SetField("StartDate", "'" + DateTime.Now.Date.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("Priority", "5");
                        QueryBuilderObject.SetField("OrganizationID", MiscOrgID);//TO BE ADDED TO THE NEW RELEASE INTEGRATION
                        err = QueryBuilderObject.InsertQueryString("PriceList", db_vms);

                        QueryBuilderObject.SetField("PriceListID", PriceListID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + DivisionCode + "/" + CompanyCodeLevel3 + "'");
                        err = QueryBuilderObject.InsertQueryString("PriceListLanguage", db_vms);

                        QueryBuilderObject.SetField("KeyValue", PriceListID);
                        err = QueryBuilderObject.UpdateQueryString("Configuration", "KeyName = 'DefaultPriceListID' AND EmployeeID = -1", db_vms);
                    }

                    string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + UOM + "' AND LanguageID = 1", db_vms);
                    if (PackTypeID.Equals(string.Empty))
                    {
                        continue;
                    }
                    ExtendItem(ItemCode, DivisionCode, MiscOrgID);//THIS FUNCTION WAS ADDED TO CREATE THE ITEM AGAIN UNDER DIFFERENT DIVISIONS
                    string PACKID = GetFieldValue("Pack", "PACKID", @"ItemID in (select ItemID from Item where ItemCode='" + ItemCode + "' and ItemCategoryID in (select ItemCategoryID from ItemCategory where DivisionID in (select DivisionID from Division where DivisionCode='" + DivisionCode + "' and OrganizationID=" + MiscOrgID + "))) and PackTypeID in (select PackTypeID from PackTypeLanguage where Description ='" + UOM + "') ", db_vms).Trim();
                    if (PACKID.Equals(string.Empty)) continue;
                    AssignDivisionDefaultPriceList(PriceListID, DivisionID);
                    string PackQuantity = GetFieldValue("Pack", "Quantity", "ItemID in (select ItemID from Item where ItemCode='" + ItemCode + "')  AND PackTypeID = " + PackTypeID + "", db_vms);
                    InCubeQuery PackQuery = new InCubeQuery(db_vms, @"
SELECT
Pack.PACKID,
Pack.Quantity,
Pack.PackTypeID,
PackTypeLanguage.Description
FROM Pack INNER JOIN
Item ON Pack.ItemID = Item.ItemID INNER JOIN
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID
INNER JOIN ITEMCATEGORY IC ON IC.ITEMCATEGORYID=ITEM.ITEMCATEGORYID
INNER JOIN DIVISION DIV ON DIV.DIVISIONID=IC.DIVISIONID
INNER JOIN ORGANIZATION O ON O.ORGANIZATIONID=DIV.ORGANIZATIONID
WHERE DIV.DIVISIONCODE='" + DivisionCode + @"' AND O.ORGANIZATIONID=" + MiscOrgID + @" AND
Item.ItemCode = '" + ItemCode.Trim() + @"' AND
PackTypeLanguage.LanguageID = 1");

                    PackQuery.Execute();
                    err = PackQuery.FindFirst();

                    while (err == InCubeErrors.Success)
                    {
                        decimal loopPrice = decimal.Parse(Price);

                        PackQuery.GetField(0, ref field);
                        PACKID = field.ToString();

                        PackQuery.GetField(1, ref field);
                        decimal ConversionFactor2 = decimal.Parse(field.ToString());

                        PackQuery.GetField(2, ref field);
                        string LoopPackTypeID = field.ToString().Trim();

                        if (LoopPackTypeID != PackTypeID)
                        {
                            loopPrice = Math.Round(ConversionFactor2 * (decimal.Parse(Price) / decimal.Parse(PackQuantity)), 3);
                        }
                        int PriceDefinitionID = 1;
                        string currentPrice = GetFieldValue("PriceDefinition", "Price", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID, db_vms);
                        if (currentPrice.Equals(string.Empty))
                        {
                            PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms));

                            QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID.ToString());
                            QueryBuilderObject.SetField("QuantityRangeID", "1");
                            QueryBuilderObject.SetField("PACKID", PACKID);
                            QueryBuilderObject.SetField("CurrencyID", "1");
                            QueryBuilderObject.SetField("Tax", "0");
                            QueryBuilderObject.SetField("Price", loopPrice.ToString());
                            QueryBuilderObject.SetField("PriceListID", PriceListID);
                            err = QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);
                        }
                        else
                        {
                            PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "PriceDefinitionID", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID, db_vms));
                            if (!currentPrice.Equals(Price.ToString()))
                            {
                                QueryBuilderObject.SetField("Price", loopPrice.ToString());
                                QueryBuilderObject.UpdateQueryString("PriceDefinition", "PACKID = " + PACKID + " AND PriceListID = " + PriceListID + " AND PriceDefinitionID = " + PriceDefinitionID, db_vms);
                            }
                        }
                        //THE FOLLOWING FUNCTION IS TO FILL THE PriceENDDATE TABLE WHICH WILL LATER DELETE THE EXPIRED PriceS
                        PricePeriodControl(PriceDefinitionID.ToString(), PriceCreatDate, PriceEndDate);
                        err = PackQuery.FindNext();
                    }
                }
                #endregion
            }
            catch
            {
            }
        }

        private void UpdateDiscountFromSAP()
        {
            try
            {
                //if (DateTime.Now.TimeOfDay.Hours >= 1 && DateTime.Now.TimeOfDay.Hours < 5)
                //{
                string DeleteDiscount = "SP_DeleteDiscount";
                InCubeQuery TEMP_QRY = new InCubeQuery(DeleteDiscount, db_vms);
                err = TEMP_QRY.ExecuteStoredProcedure();
                if (err == InCubeErrors.Success)
                {
                    WriteExceptions("SP_DeleteDiscount PROCEDURE WAS SUCCESSFULLY EXECUTED", "SP_PriceENDDATE", false);
                }
                else
                {
                    WriteExceptions("SP_DeleteDiscount PROCEDURE WAS FAILED", "SP_PriceENDDATE", false);
                }
                //}

                List<string> appliedList = new List<string>();
                DataTable TBL = GetDiscountTable();
                InCubeQuery QRY;
                DataTable INNER = new DataTable();

                #region DEFAULT Discount
                ClearProgress();
                SetProgressMax(TBL.Select("ChannelCode='' and CustomerCode='' and SalesGroupCode='' ").Length);
                foreach (DataRow dr in TBL.Select("ChannelCode='' and CustomerCode='' and SalesGroupCode='' "))
                {
                    ReportProgress("Filling Default Prices");
                    string CompanyCode = dr["CompanyCode"].ToString().Trim();
                    string CompanyCodeLevel3 = dr["CompanyCodeLevel3"].ToString().Trim();
                    string PriceListName = dr["PriceListName"].ToString().Trim();
                    string ValidFrom = dr["ValidFrom"].ToString().Trim();
                    string ValidTo = dr["ValidTo"].ToString().Trim();
                    string ItemCode = dr["ItemCode"].ToString().Trim();
                    string UOM = "EA";// dr["UOM"].ToString().Trim();
                    string ConversionFactor = dr["ConversionFactor"].ToString().Trim();
                    string Price = dr["Price"].ToString().Trim();
                    string CustomerCode = dr["CustomerCode"].ToString().Trim();
                    string SalesGroupCode = dr["SalesGroupCode"].ToString().Trim();
                    string DivisionCode = dr["DivisionCode"].ToString().Trim();
                    string ChannelCode = dr["ChannelCode"].ToString().Trim();
                    string StockStatus = dr["StockStatus"].ToString().Trim();
                    string FLAG = dr["FLAG"].ToString().Trim();
                    string IsDeleted = dr["IsDeleted"].ToString().Trim();
                    //if (GetFieldValue("QNIE_ActiveItems", "ItemCode", "ItemCode='" + ItemCode + "'", db_vms).Trim().Equals(string.Empty)) continue;
                    string MiscOrgID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + CompanyCodeLevel3 + "'", db_vms).Trim();
                    string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode='" + DivisionCode + "' and OrganizationID=" + MiscOrgID + "", db_vms).Trim();
                    if (!ValidTo.Equals(string.Empty))
                        ValidTo = ValidTo.Replace("9999", DateTime.Today.Year.ToString());
                    DateTime PriceCreatDate = DateTime.Parse(ValidFrom);
                    DateTime PriceEndDate = DateTime.Parse(ValidTo).AddYears(5);



                    string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + UOM + "' AND LanguageID = 1", db_vms);
                    if (PackTypeID.Equals(string.Empty))
                    {
                        continue;
                    }
                    // ExtendItem(ItemCode, DivisionCode, MiscOrgID);//THIS FUNCTION WAS ADDED TO CREATE THE ITEM AGAIN UNDER DIFFERENT DIVISIONS
                    string PACKID = GetFieldValue("Pack", "PACKID", @"ItemID in (select ItemID from Item where ItemCode='" + ItemCode + "' and ItemCategoryID in (select ItemCategoryID from ItemCategory where DivisionID in (select DivisionID from Division where DivisionCode='" + DivisionCode + "' and OrganizationID=" + MiscOrgID + "))) and PackTypeID in (select PackTypeID from PackTypeLanguage where Description ='" + UOM + "') ", db_vms).Trim();
                    if (PACKID.Equals(string.Empty)) continue;
                    string PackQuantity = GetFieldValue("Pack", "Quantity", "ItemID in (select ItemID from Item where ItemCode='" + ItemCode + "')  AND PackTypeID = " + PackTypeID + "", db_vms);
                    string definitionBatch = string.Empty;
                    string PriceListTypeID = string.Empty;
                    PriceListTypeID = "1";
                    string DiscountName = DivisionCode + "/" + CompanyCodeLevel3 + "/" + ItemCode;
                    //string checkPricelistExist = GetFieldValue("PriceList", "PriceListID", "PriceListCode='" + SalesGroupCode + "/" + DivisionCode + "/" + CompanyCodeLevel3 + "' and PriceListTypeID=" + PriceListTypeID + "", db_vms).Trim();
                    string DISCOUN_ID = GetFieldValue("DiscountLanguage", "DiscountID", "Description='" + DiscountName + "'", db_vms).Trim();
                    if (DISCOUN_ID.Equals(string.Empty))
                    {
                        DISCOUN_ID = GetFieldValue("Discount", "ISNULL(MAX(DiscountID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("DiscountID", DISCOUN_ID);
                        QueryBuilderObject.SetField("PackID", PACKID);
                        QueryBuilderObject.SetField("Discount", Price.ToString());
                        QueryBuilderObject.SetField("FOC", "0");
                        QueryBuilderObject.SetField("StartDate", "'" + PriceCreatDate.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("DiscountTypeID", "1");
                        QueryBuilderObject.SetField("OrganizationID", MiscOrgID);
                        QueryBuilderObject.SetField("DiscountCode", "'" + DiscountName + "'");
                        QueryBuilderObject.SetField("TypeID", "1");
                        err = QueryBuilderObject.InsertQueryString("Discount", db_vms);

                        QueryBuilderObject.SetField("DiscountID", DISCOUN_ID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + DiscountName + "'");
                        err = QueryBuilderObject.InsertQueryString("DiscountLanguage", db_vms);
                    }

                    #region DISCOUNT ASSIGNMENT
                    string DiscountAssignmentID = string.Empty;

                    DiscountAssignmentID = GetFieldValue("DiscountAssignment", "DiscountAssignmentID", "DiscountID=" + DISCOUN_ID + " and AllCustomers=1", db_vms).Trim();

                    if (DiscountAssignmentID.Equals(string.Empty))
                    {
                        DiscountAssignmentID = GetFieldValue("DiscountAssignment", "ISNULL(MAX(DiscountAssignmentID),0) + 1", db_vms);
                        QueryBuilderObject.SetField("DiscountAssignmentID", DiscountAssignmentID);
                        QueryBuilderObject.SetField("DiscountID", DISCOUN_ID);

                        QueryBuilderObject.SetField("AllCustomers", "1");

                        err = QueryBuilderObject.InsertQueryString("DiscountAssignment", db_vms);
                    }
                }
                #endregion
                #endregion

                #region GROUP Discont
                List<string> groupDiscountList = new List<string>();
                ClearProgress();
                SetProgressMax(TBL.Select("SalesGroupCode is not null and SalesGroupCode<>'' and ChannelCode='' and CustomerCode='' and len(SalesGroupCode)>3 ").Length);
                foreach (DataRow dr in TBL.Select("SalesGroupCode is not null and SalesGroupCode<>'' and ChannelCode='' and CustomerCode=''  "))
                {
                    ReportProgress("Filling Group Prices");

                    string CompanyCode = dr["CompanyCode"].ToString().Trim();
                    string CompanyCodeLevel3 = dr["CompanyCodeLevel3"].ToString().Trim();
                    string PriceListName = dr["PriceListName"].ToString().Trim();
                    string ValidFrom = dr["ValidFrom"].ToString().Trim();
                    string ValidTo = dr["ValidTo"].ToString().Trim();
                    string ItemCode = dr["ItemCode"].ToString().Trim();
                    string UOM = "EA";// dr["UOM"].ToString().Trim();
                    string ConversionFactor = dr["ConversionFactor"].ToString().Trim();
                    string Price = dr["Price"].ToString().Trim();
                    string CustomerCode = dr["CustomerCode"].ToString().Trim();
                    string SalesGroupCode = dr["SalesGroupCode"].ToString().Trim();
                    string DivisionCode = dr["DivisionCode"].ToString().Trim();
                    if (groupDiscountList.Contains(SalesGroupCode + ItemCode + UOM))
                        continue;
                    groupDiscountList.Add(SalesGroupCode + ItemCode + UOM);
                    //if (!DivisionCode.Equals("15")) continue;
                    string ChannelCode = dr["ChannelCode"].ToString().Trim();
                    string StockStatus = dr["StockStatus"].ToString().Trim();
                    string FLAG = dr["FLAG"].ToString().Trim();
                    string IsDeleted = dr["IsDeleted"].ToString().Trim();
                    string BATCH = string.Empty;
                    string BEXP_DATE = string.Empty;
                    //if (GetFieldValue("QNIE_ActiveItems", "ItemCode", "ItemCode='" + ItemCode + "'", db_vms).Trim().Equals(string.Empty)) continue;
                    string MiscOrgID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + CompanyCodeLevel3 + "'", db_vms).Trim();
                    if (!ValidTo.Equals(string.Empty))
                        ValidTo = ValidTo.Replace("9999", DateTime.Today.Year.ToString());
                    DateTime PriceCreatDate = DateTime.Parse(ValidFrom);
                    DateTime PriceEndDate = DateTime.Parse(ValidTo);
                    string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + UOM + "' AND LanguageID = 1", db_vms);
                    if (PackTypeID.Equals(string.Empty))
                    {
                        continue;
                    }
                    //ExtendItem(ItemCode, DivisionCode, MiscOrgID);//THIS FUNCTION WAS ADDED TO CREATE THE ITEM AGAIN UNDER DIFFERENT DIVISIONS
                    string PACKID = GetFieldValue("Pack", "PACKID", @"ItemID in (select ItemID from Item where ItemCode='" + ItemCode + "' and ItemCategoryID in (select ItemCategoryID from ItemCategory where DivisionID in (select DivisionID from Division where DivisionCode='" + DivisionCode + "' and OrganizationID=" + MiscOrgID + "))) and PackTypeID in (select PackTypeID from PackTypeLanguage where Description ='" + UOM + "') ", db_vms).Trim();
                    if (PACKID.Equals(string.Empty)) continue;
                    string PackQuantity = GetFieldValue("Pack", "Quantity", "ItemID in (select ItemID from Item where ItemCode='" + ItemCode + "')  AND PackTypeID = " + PackTypeID + "", db_vms);
                    string definitionBatch = string.Empty;
                    string PriceListTypeID = string.Empty;
                    PriceListTypeID = "1";
                    string DiscountName = SalesGroupCode + "/" + DivisionCode + "/" + CompanyCodeLevel3 + "/" + ItemCode;
                    //string checkPricelistExist = GetFieldValue("PriceList", "PriceListID", "PriceListCode='" + SalesGroupCode + "/" + DivisionCode + "/" + CompanyCodeLevel3 + "' and PriceListTypeID=" + PriceListTypeID + "", db_vms).Trim();
                    string DISCOUN_ID = GetFieldValue("DiscountLanguage", "DiscountID", "Description='" + SalesGroupCode + "/" + DivisionCode + "/" + CompanyCodeLevel3 + "'", db_vms).Trim();
                    if (DISCOUN_ID.Equals(string.Empty))
                    {
                        DISCOUN_ID = GetFieldValue("Discount", "ISNULL(MAX(DiscountID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("DiscountID", DISCOUN_ID);
                        QueryBuilderObject.SetField("PackID", PACKID);
                        QueryBuilderObject.SetField("Discount", Price.ToString());
                        QueryBuilderObject.SetField("FOC", "0");
                        QueryBuilderObject.SetField("StartDate", "'" + PriceCreatDate.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("DiscountTypeID", "1");
                        QueryBuilderObject.SetField("OrganizationID", MiscOrgID);
                        QueryBuilderObject.SetField("DiscountCode", "'" + DiscountName + "'");
                        QueryBuilderObject.SetField("TypeID", "1");
                        err = QueryBuilderObject.InsertQueryString("Discount", db_vms);

                        QueryBuilderObject.SetField("DiscountID", DISCOUN_ID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + DiscountName + "'");
                        err = QueryBuilderObject.InsertQueryString("DiscountLanguage", db_vms);
                    }

                    #region DISCOUNT ASSIGNMENT
                    string DiscountAssignmentID = string.Empty;

                    #region Group Customers Assignment
                    string GroupIDExist = GetFieldValue("CustomerGroup", "GroupID", "GroupCode='" + SalesGroupCode + "'", db_vms).Trim();
                    if (GroupIDExist.Equals(string.Empty)) continue;
                    string GetCustomers = string.Format(@"Select CO.CustomerID,CO.OutletID from CustomerOutletGroup CO
WHERE CO.GroupID={0}
 ", GroupIDExist);
                    QRY = new InCubeQuery(GetCustomers, db_vms);
                    err = QRY.Execute();
                    INNER = QRY.GetDataTable();

                    foreach (DataRow DR in INNER.Rows)
                    {
                        string CustomerID = DR["CustomerID"].ToString().Trim();
                        string OutletID = DR["OutletID"].ToString().Trim();
                        #region DISCOUNT ASSIGNMENT

                        string deleteCustomerDiscountAssignment = string.Format("Delete from DiscountAssignment where CustomerID={0} and OutletID={1}", CustomerID, OutletID);
                        QRY = new InCubeQuery(deleteCustomerDiscountAssignment, db_vms);
                        err = QRY.ExecuteNonQuery();
                        DiscountAssignmentID = GetFieldValue("DiscountAssignment", "ISNULL(MAX(DiscountAssignmentID),0) + 1", db_vms);
                        QueryBuilderObject.SetField("DiscountAssignmentID", DiscountAssignmentID);
                        QueryBuilderObject.SetField("DiscountID", DISCOUN_ID);
                        if (!CustomerID.Equals(string.Empty))
                            QueryBuilderObject.SetField("CustomerID", CustomerID);

                        if (!OutletID.Equals(string.Empty))
                            QueryBuilderObject.SetField("OutletID", OutletID);
                        QueryBuilderObject.SetField("AllCustomers", "0");

                        err = QueryBuilderObject.InsertQueryString("DiscountAssignment", db_vms);

                        #endregion
                    }


                    #endregion







                    //// string checkCHLPrc = GetFieldValue("GroupPrice", "GroupID", "GroupID=" + GroupIDExist + " and PriceListID=" + PriceListID + "", db_vms).Trim();
                    // if (!GroupIDExist.Equals(string.Empty))
                    // {
                    //     DiscountAssignmentID = GetFieldValue("DiscountAssignment", "DiscountAssignmentID", "DiscountID=" + DISCOUN_ID + " and CustomerGroupID=" + GroupIDExist + "", db_vms).Trim();
                    // }


                    // if (DiscountAssignmentID.Equals(string.Empty))
                    // {
                    //     DiscountAssignmentID = GetFieldValue("DiscountAssignment", "ISNULL(MAX(DiscountAssignmentID),0) + 1", db_vms);
                    //     QueryBuilderObject.SetField("DiscountAssignmentID", DiscountAssignmentID);
                    //     QueryBuilderObject.SetField("DiscountID", DISCOUN_ID);

                    //     if (!GroupIDExist.Equals(string.Empty))
                    //         QueryBuilderObject.SetField("CustomerGroupID", GroupIDExist);

                    //     QueryBuilderObject.SetField("AllCustomers", "0");

                    //     err = QueryBuilderObject.InsertQueryString("DiscountAssignment", db_vms);
                    // }
                    #endregion

                }
                #endregion

                #region CUSTOMER Discount
                ClearProgress();
                SetProgressMax(TBL.Select("CustomerCode is not null and CustomerCode<>''").Length);

                foreach (DataRow dr in TBL.Select("CustomerCode is not null and CustomerCode<>''"))
                {
                    ReportProgress("Filling Customer Prices");
                    string CompanyCode = dr["CompanyCode"].ToString().Trim();
                    string CompanyCodeLevel3 = dr["CompanyCodeLevel3"].ToString().Trim();
                    string PriceListName = dr["PriceListName"].ToString().Trim();
                    string ValidFrom = dr["ValidFrom"].ToString().Trim();
                    string ValidTo = dr["ValidTo"].ToString().Trim();
                    string ItemCode = dr["ItemCode"].ToString().Trim();

                    string UOM = "EA";// dr["UOM"].ToString().Trim();
                    string ConversionFactor = dr["ConversionFactor"].ToString().Trim();
                    string Price = dr["Price"].ToString().Trim();
                    string CustomerCode = dr["CustomerCode"].ToString().Trim();
                    //if (!CustomerCode.Equals("700071")) continue;
                    string SalesGroupCode = dr["SalesGroupCode"].ToString().Trim();
                    string DivisionCode = dr["DivisionCode"].ToString().Trim();
                    string ChannelCode = dr["ChannelCode"].ToString().Trim();
                    string StockStatus = dr["StockStatus"].ToString().Trim();
                    string FLAG = dr["FLAG"].ToString().Trim();
                    string IsDeleted = dr["IsDeleted"].ToString().Trim();
                    if (CustomerCode.Equals("708377") && ItemCode.Equals("77030041"))
                    {

                    }
                    string BATCH = string.Empty;
                    string BEXP_DATE = string.Empty;
                    //if (GetFieldValue("QNIE_ActiveItems", "ItemCode", "ItemCode='" + ItemCode + "'", db_vms).Trim().Equals(string.Empty)) continue;
                    string MiscOrgID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode='" + CompanyCodeLevel3 + "'", db_vms).Trim();
                    if (!ValidTo.Equals(string.Empty))
                        ValidTo = ValidTo.Replace("9999", DateTime.Today.Year.ToString());
                    DateTime PriceCreatDate = DateTime.Parse(ValidFrom);
                    DateTime PriceEndDate = DateTime.Parse(ValidTo);

                    string definitionBatch = string.Empty;
                    string PriceListTypeID = string.Empty;
                    PriceListTypeID = "1";
                    if (!BATCH.Equals(string.Empty)) { PriceListTypeID = "4"; definitionBatch = "and BatchNo='" + BATCH + "'"; }
                    if (BEXP_DATE.Equals(string.Empty)) { BEXP_DATE = "1990/01/01"; }
                    string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", "Description='" + UOM + "' AND LanguageID = 1", db_vms);
                    if (PackTypeID.Equals(string.Empty))
                    {
                        continue;
                    }
                    //ExtendItem(ItemCode, DivisionCode, MiscOrgID);//THIS FUNCTION WAS ADDED TO CREATE THE ITEM AGAIN UNDER DIFFERENT DIVISIONS
                    string PACKID = GetFieldValue("Pack", "PACKID", @"ItemID in (select ItemID from Item where ItemCode='" + ItemCode + "' and ItemCategoryID in (select ItemCategoryID from ItemCategory where DivisionID in (select DivisionID from Division where DivisionCode='" + DivisionCode + "' and OrganizationID=" + MiscOrgID + "))) and PackTypeID in (select PackTypeID from PackTypeLanguage where Description ='" + UOM + "') ", db_vms).Trim();
                    if (PACKID.Equals(string.Empty)) continue;
                    string PackQuantity = GetFieldValue("Pack", "Quantity", "ItemID in (select ItemID from Item where ItemCode='" + ItemCode + "')  AND PackTypeID = " + PackTypeID + "", db_vms);

                    PriceListTypeID = "1";
                    string DiscountName = CustomerCode + "/" + DivisionCode + "/" + CompanyCodeLevel3 + "/" + ItemCode;
                    //string checkPricelistExist = GetFieldValue("PriceList", "PriceListID", "PriceListCode='" + SalesGroupCode + "/" + DivisionCode + "/" + CompanyCodeLevel3 + "' and PriceListTypeID=" + PriceListTypeID + "", db_vms).Trim();
                    string DISCOUN_ID = GetFieldValue("DiscountLanguage", "DiscountID", "Description='" + DiscountName + "'", db_vms).Trim();
                    if (DISCOUN_ID.Equals(string.Empty))
                    {
                        DISCOUN_ID = GetFieldValue("Discount", "ISNULL(MAX(DiscountID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("DiscountID", DISCOUN_ID);
                        QueryBuilderObject.SetField("PackID", PACKID);
                        QueryBuilderObject.SetField("Discount", Price.ToString());
                        QueryBuilderObject.SetField("FOC", "0");
                        QueryBuilderObject.SetField("StartDate", "'" + PriceCreatDate.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("EndDate", "'" + PriceEndDate.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("DiscountTypeID", "1");
                        QueryBuilderObject.SetField("OrganizationID", MiscOrgID);
                        QueryBuilderObject.SetField("DiscountCode", "'" + DiscountName + "'");
                        QueryBuilderObject.SetField("TypeID", "1");
                        err = QueryBuilderObject.InsertQueryString("Discount", db_vms);

                        QueryBuilderObject.SetField("DiscountID", DISCOUN_ID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + DiscountName + "'");
                        err = QueryBuilderObject.InsertQueryString("DiscountLanguage", db_vms);
                    }

                    string GetCustomers = string.Format(@"Select CO.CustomerID,CO.OutletID from CustomerOutlet CO
WHERE CO.CUSTOMERCODE='{0}'
 ", CustomerCode);
                    QRY = new InCubeQuery(GetCustomers, db_vms);
                    err = QRY.Execute();
                    INNER = QRY.GetDataTable();

                    foreach (DataRow DR in INNER.Rows)
                    {
                        string CustomerID = DR["CustomerID"].ToString().Trim();
                        string OutletID = DR["OutletID"].ToString().Trim();
                        #region DISCOUNT ASSIGNMENT
                        string DiscountAssignmentID = string.Empty;
                        string deleteCustomerDiscountAssignment = string.Format("Delete from DiscountAssignment where CustomerID={0} and OutletID={1} and discountid in (select discountid from discount where PackID={2})", CustomerID, OutletID, PACKID);
                        QRY = new InCubeQuery(deleteCustomerDiscountAssignment, db_vms);
                        err = QRY.ExecuteNonQuery();
                        DiscountAssignmentID = GetFieldValue("DiscountAssignment", "ISNULL(MAX(DiscountAssignmentID),0) + 1", db_vms);
                        QueryBuilderObject.SetField("DiscountAssignmentID", DiscountAssignmentID);
                        QueryBuilderObject.SetField("DiscountID", DISCOUN_ID);
                        if (!CustomerID.Equals(string.Empty))
                            QueryBuilderObject.SetField("CustomerID", CustomerID);

                        if (!OutletID.Equals(string.Empty))
                            QueryBuilderObject.SetField("OutletID", OutletID);
                        QueryBuilderObject.SetField("AllCustomers", "0");

                        err = QueryBuilderObject.InsertQueryString("DiscountAssignment", db_vms);
                        #endregion
                    }
                }
                #endregion


            }
            catch
            {
            }
        }

        private void ExtendItem(string ItemCode, string DivisionCode, string OrganizationID)
        {
            try
            {
                #region INSERT/UPDATE DIVISION
                string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode = '" + DivisionCode + "' AND ORGANIZATIONID=" + OrganizationID + "", db_vms);
                if (DivisionID.Equals(string.Empty))
                {
                    DivisionID = GetFieldValue("Division", "isnull(MAX(DivisionID),0) + 1", db_vms);
                    QueryBuilderObject.SetField("DivisionID", DivisionID);
                    QueryBuilderObject.SetField("DivisionCode", "'" + DivisionCode + "'");
                    QueryBuilderObject.SetField("OrganizationID", OrganizationID);// THIS USED TO BE 1 THEN I ADDED THE MiscOrgID FOR THE NEW RELEASE INTEGRATION.
                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    QueryBuilderObject.InsertQueryString("Division", db_vms);
                    string DivisionString = "Division " + DivisionCode;
                    QueryBuilderObject.SetField("DivisionID", DivisionID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + DivisionString + "'");
                    err = QueryBuilderObject.InsertQueryString("DivisionLanguage", db_vms);
                }
                #endregion

                #region UPDATE CATEGORY
                string ItemCategoryID;
                string MainItemCategory = GetFieldValue("ItemCategory", "ItemCategoryCode", " ItemCategoryID in (select ItemCategoryID from item where ItemCode='" + ItemCode + "')", db_vms).Trim();// and ItemCategoryID in (Select ItemCategoryID from ItemCategory where DivisionID="+DivisionID+")
                string CatDiv = GetFieldValue("ItemCategory", "ItemCategoryID", "ItemCategoryCode='" + MainItemCategory + "' and DivisionID=" + DivisionID + "", db_vms).Trim();
                if (CatDiv.Equals(string.Empty))//DOES THIS CATEGORY EXIST WITH THIS DIVISION?
                {
                    string ITEMID = GetFieldValue("Item", "ITEMID", "ItemCode='" + ItemCode + "'", db_vms).Trim();
                    string ItemCategoryCode = MainItemCategory;// GetFieldValue("ItemCategory", "ItemCategoryCode", "ItemCategoryID=" + ItemCategoryID + "", db_vms).Trim();
                    string ItemCategoryDescription = GetFieldValue("ItemCategoryLanguage", "Description", "ItemCategoryID in (Select ItemCategoryID from ItemCategory where ItemCategoryCode='" + MainItemCategory + "')", db_vms).Trim();
                    if (!ITEMID.Equals(string.Empty) && !ItemCategoryCode.Equals(string.Empty))
                    {
                        ItemCategoryID = GetFieldValue("ItemCategory", "isnull(MAX(ItemCategoryID),0) + 1", db_vms);
                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());
                        QueryBuilderObject.SetField("ItemCategoryCode", "'" + ItemCategoryCode + "'");
                        QueryBuilderObject.SetField("DivisionID", DivisionID);
                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        err = QueryBuilderObject.InsertQueryString("ItemCategory", db_vms);

                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + ItemCategoryDescription + "'");
                        err = QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", db_vms);

                        InCubeQuery qry = new InCubeQuery("SP_EXTEND_ITEM", db_vms);
                        qry.AddParameter("@ITEMID", ITEMID);
                        qry.AddParameter("@DIVISIONCODE", DivisionCode);
                        qry.AddParameter("@CATEGORYID", ItemCategoryID);
                        qry.AddParameter("@ITEMCODE", ItemCode);
                        err = qry.ExecuteStoredProcedure();
                    }
                }
                else
                {
                    string CatDivItem = GetFieldValue("ItemCategory", "ItemCategoryID", "ItemCategoryCode='" + MainItemCategory + "' and DivisionID=" + DivisionID + " and ItemCategoryID in (select ItemCategoryID from item where ItemCode='" + ItemCode + "')", db_vms).Trim();
                    if (CatDivItem.Equals(string.Empty))
                    {//HERE, THE CATEGORY EXIST WITH THAT DIVISION BUT NOT WITH THIS ITEM
                        string ITEMID = GetFieldValue("Item", "ITEMID", "ItemCode='" + ItemCode + "'", db_vms).Trim();
                        string ItemCategoryCode = MainItemCategory;// GetFieldValue("ItemCategory", "ItemCategoryCode", "ItemCategoryID=" + ItemCategoryID + "", db_vms).Trim();
                        string ItemCategoryDescription = GetFieldValue("ItemCategoryLanguage", "Description", "ItemCategoryID in (Select ItemCategoryID from ItemCategory where ItemCategoryCode='" + MainItemCategory + "')", db_vms).Trim();
                        InCubeQuery qry = new InCubeQuery("SP_EXTEND_ITEM", db_vms);
                        qry.AddParameter("@ITEMID", ITEMID);
                        qry.AddParameter("@DIVISIONCODE", DivisionCode);
                        qry.AddParameter("@CATEGORYID", CatDiv);
                        qry.AddParameter("@ITEMCODE", ItemCode);
                        err = qry.ExecuteStoredProcedure();
                    }
                    //string ITEMID = GetFieldValue("Item", "ITEMID", "ItemCode='" + ItemCode + "' and ItemCategoryID=" + CatDiv + "", db_vms).Trim();
                    //InCubeQuery qry = new InCubeQuery("SP_EXTEND_ITEM", db_vms);
                    //qry.AddParameter("@ITEMID", ITEMID);
                    //qry.AddParameter("@DIVISIONCODE", DivisionCode);
                    //qry.AddParameter("@CATEGORYID", ItemCategoryID);
                    //qry.AddParameter("@ITEMCODE", ItemCode);
                    //err = qry.ExecuteStoredProcedure();
                }

                #endregion
            }
            catch
            {

            }
        }

        private void ExtendItem(string ItemCode, string DivisionCode, string OrganizationID, ref InCubeTransaction tran)
        {
            try
            {
                #region INSERT/UPDATE DIVISION
                string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode = '" + DivisionCode + "' AND ORGANIZATIONID=" + OrganizationID + "", db_vms, tran);
                if (DivisionID.Equals(string.Empty))
                {
                    DivisionID = GetFieldValue("Division", "isnull(MAX(DivisionID),0) + 1", db_vms);
                    QueryBuilderObject.SetField("DivisionID", DivisionID);
                    QueryBuilderObject.SetField("DivisionCode", "'" + DivisionCode + "'");
                    QueryBuilderObject.SetField("OrganizationID", OrganizationID);// THIS USED TO BE 1 THEN I ADDED THE MiscOrgID FOR THE NEW RELEASE INTEGRATION.
                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    QueryBuilderObject.InsertQueryString("Division", db_vms, tran);
                    string DivisionString = "Division " + DivisionCode;
                    QueryBuilderObject.SetField("DivisionID", DivisionID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + DivisionString + "'");
                    err = QueryBuilderObject.InsertQueryString("DivisionLanguage", db_vms, tran);
                }
                #endregion

                #region UPDATE CATEGORY
                string ItemCategoryID;
                string MainItemCategory = GetFieldValue("ItemCategory", "ItemCategoryCode", " ItemCategoryID in (select ItemCategoryID from item where ItemCode='" + ItemCode + "')", db_vms, tran).Trim();// and ItemCategoryID in (Select ItemCategoryID from ItemCategory where DivisionID="+DivisionID+")
                string CatDiv = GetFieldValue("ItemCategory", "ItemCategoryID", "ItemCategoryCode='" + MainItemCategory + "' and DivisionID=" + DivisionID + "", db_vms, tran).Trim();
                if (CatDiv.Equals(string.Empty))//DOES THIS CATEGORY EXIST WITH THIS DIVISION?
                {
                    string ITEMID = GetFieldValue("Item", "ITEMID", "ItemCode='" + ItemCode + "'", db_vms, tran).Trim();
                    string ItemCategoryCode = MainItemCategory;// GetFieldValue("ItemCategory", "ItemCategoryCode", "ItemCategoryID=" + ItemCategoryID + "", db_vms).Trim();
                    string ItemCategoryDescription = GetFieldValue("ItemCategoryLanguage", "Description", "ItemCategoryID in (Select ItemCategoryID from ItemCategory where ItemCategoryCode='" + MainItemCategory + "')", db_vms, tran).Trim();
                    if (!ITEMID.Equals(string.Empty) && !ItemCategoryCode.Equals(string.Empty))
                    {
                        ItemCategoryID = GetFieldValue("ItemCategory", "isnull(MAX(ItemCategoryID),0) + 1", db_vms, tran);
                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());
                        QueryBuilderObject.SetField("ItemCategoryCode", "'" + ItemCategoryCode + "'");
                        QueryBuilderObject.SetField("DivisionID", DivisionID);
                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        err = QueryBuilderObject.InsertQueryString("ItemCategory", db_vms, tran);

                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + ItemCategoryDescription + "'");
                        err = QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", db_vms, tran);

                        //InCubeQuery qry = new InCubeQuery("SP_EXTEND_ITEM", db_vms);
                        //qry.AddParameter("@ITEMID", ITEMID);
                        //qry.AddParameter("@DIVISIONCODE", DivisionCode);
                        //qry.AddParameter("@CATEGORYID", ItemCategoryID);
                        //qry.AddParameter("@ITEMCODE", ItemCode);
                        //err = qry.ExecuteStoredProcedure();
                        InCubeQuery qry = new InCubeQuery("exec SP_EXTEND_ITEM " + ITEMID + ",'" + DivisionCode + "'," + ItemCategoryID + ",'" + ItemCode + "'", db_vms);
                        err = qry.ExecuteNoneQuery(tran);
                    }
                }
                else
                {
                    string CatDivItem = GetFieldValue("ItemCategory", "ItemCategoryID", "ItemCategoryCode='" + MainItemCategory + "' and DivisionID=" + DivisionID + " and ItemCategoryID in (select ItemCategoryID from item where ItemCode='" + ItemCode + "')", db_vms, tran).Trim();
                    if (CatDivItem.Equals(string.Empty))
                    {//HERE, THE CATEGORY EXIST WITH THAT DIVISION BUT NOT WITH THIS ITEM
                        string ITEMID = GetFieldValue("Item", "ITEMID", "ItemCode='" + ItemCode + "'", db_vms, tran).Trim();
                        string ItemCategoryCode = MainItemCategory;// GetFieldValue("ItemCategory", "ItemCategoryCode", "ItemCategoryID=" + ItemCategoryID + "", db_vms).Trim();
                        string ItemCategoryDescription = GetFieldValue("ItemCategoryLanguage", "Description", "ItemCategoryID in (Select ItemCategoryID from ItemCategory where ItemCategoryCode='" + MainItemCategory + "')", db_vms, tran).Trim();
                        InCubeQuery qry = new InCubeQuery("exec SP_EXTEND_ITEM " + ITEMID + ",'" + DivisionCode + "'," + CatDiv + ",'" + ItemCode + "'", db_vms);
                        err = qry.ExecuteNoneQuery(tran);
                        //qry.AddParameter("@ITEMID", ITEMID);
                        //qry.AddParameter("@DIVISIONCODE", DivisionCode);
                        //qry.AddParameter("@CATEGORYID", CatDiv);
                        //qry.AddParameter("@ITEMCODE", ItemCode);
                        //err = qry.ExecuteStoredProcedure();

                    }
                    //string ITEMID = GetFieldValue("Item", "ITEMID", "ItemCode='" + ItemCode + "' and ItemCategoryID=" + CatDiv + "", db_vms).Trim();
                    //InCubeQuery qry = new InCubeQuery("SP_EXTEND_ITEM", db_vms);
                    //qry.AddParameter("@ITEMID", ITEMID);
                    //qry.AddParameter("@DIVISIONCODE", DivisionCode);
                    //qry.AddParameter("@CATEGORYID", ItemCategoryID);
                    //qry.AddParameter("@ITEMCODE", ItemCode);
                    //err = qry.ExecuteStoredProcedure();
                }

                #endregion
            }
            catch
            {

            }
        }

        private void AssignDivisionDefaultPriceList(string PriceListID, string DivisionID)
        {
            try
            {
                InCubeQuery qry = new InCubeQuery("SP_APPLY_DEFAULT_PRICE_LIST", db_vms);
                qry.AddParameter("@DIVISIONID", DivisionID);
                qry.AddParameter("@PRICELISTID", PriceListID);
                err = qry.ExecuteStoredProcedure();
            }
            catch
            {

            }
        }

        private void WriteExceptions(DataTable dt, string header)
        {
            try
            {
                StreamWriter wrt = new StreamWriter("InvalidEntry.txt", true);
                wrt.Write("\n" + "===========================================================================" + "\r\n");
                wrt.Write("\n" + header + "----" + DateTime.Now.ToString() + "\r\n");
                foreach (DataRow dr in dt.Rows)
                {
                    wrt.Write("\n" + dr[0].ToString() + "------->" + dr[1].ToString() + "\r\n");
                }
                wrt.Close();
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

        #region (Import Carrefour Orders)
        #endregion

    }
}