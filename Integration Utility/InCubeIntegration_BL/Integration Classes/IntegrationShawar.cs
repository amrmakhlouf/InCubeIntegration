using System;
using System.Collections.Generic;
using System.Data; 
using InCubeIntegration_DAL;
using InCubeLibrary;
using System.Data.SqlClient;
using System.ComponentModel;
using System.Text;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Xml;

namespace InCubeIntegration_BL
{
    public class IntegrationShawar : IntegrationBase // Live branch
    {
        BackgroundWorker bgwCheckProgress;
        InCubeDatabase db_res;
        OdbcConnection con , conSend;
        int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
        OdbcCommand cmd;
        OdbcDataAdapter ad;
        InCubeQuery incubeQuery = null;
        QueryBuilder QueryBuilderObject =null;
        string StagingTable = "";
        string _WarehouseID = "-1";
        public IntegrationShawar(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            if (CoreGeneral.Common.CurrentSession.loginType != LoginType.WindowsService)
            {
                db_res = new InCubeDatabase();
                db_res.Open("InCube", "IntegrationNEDM");
            }
            string _dataSourceFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase) + "\\DataSources.xml";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_dataSourceFilePath);
            string strConnectionString = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'ERP']/Data").InnerText;
            con = new OdbcConnection(strConnectionString);
            con.Open();
            strConnectionString = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'ERPSend']/Data").InnerText;
            conSend = new OdbcConnection(strConnectionString);
            conSend.Open();
            bgwCheckProgress = new BackgroundWorker();
            bgwCheckProgress.DoWork += new DoWorkEventHandler(bgw_CheckProgress);
            bgwCheckProgress.WorkerSupportsCancellation = true;

            
        }

        private void bgw_CheckProgress(object sender, DoWorkEventArgs e)
        {
            try
            {
                while (TriggerID != -1)
                {
                    int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
                    GetExecutionResults(StagingTable, ref TotalRows, ref Inserted, ref Updated, ref Skipped, db_res);
                    SetProgressMax(TotalRows);
                    ReportProgress(Inserted + Updated + Skipped);
                    System.Threading.Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        public override void Close()
        {
            if (db_res != null && db_res.GetConnection().State == ConnectionState.Open)
                db_res.Close();

            if (con != null &&con.State != ConnectionState.Closed)
                con.Close();
        }

        public override void UpdateItem( )
        {
            GetMasterData(IntegrationField.Item_U);
        }
        public override void UpdateInvoice()
        {
            GetMasterData(IntegrationField.Invoice_U);
        }
        public override void UpdateBank( )
        {
            GetMasterData(IntegrationField.Bank_U);
        }
        public override void UpdateStock( )
        {
              _WarehouseID = Filters.WarehouseID.ToString();
            GetMasterData(IntegrationField.Stock_U);
        }
        public override void UpdateCustomer( )
        {
            GetMasterData(IntegrationField.Customer_U);
        }

        public override void UpdateSalesPerson()
        {
            GetMasterData(IntegrationField.Salesperson_U);
        }

        public override void UpdatePrice( )
        {
            GetMasterData(IntegrationField.Price_U);
        }
        public override void UpdateMainWarehouse() {
            GetMasterData(IntegrationField.Warehouse_U);
        }
        private void GetMasterData(IntegrationField field)
        {
            Result res = Result.UnKnown;
            Dictionary<int, string> Filters;
            int ProcessID = 0;
            try
            {
                string MasterName = field.ToString().Substring(0, field.ToString().Length - 2);
                string ProcName = "";
                WriteMessage("\r\nRetrieving " + MasterName + " from ERP ... ");

                //Log begining of read from ERP
                Filters = new Dictionary<int, string>();
                Filters.Add(1, "Reading from ERP");
                ProcessID = execManager.LogIntegrationBegining(TriggerID, 1, Filters);

                DataTable dtMasterData = new DataTable();
                switch (field)
                {
                    case IntegrationField.Item_U:
                        res = GetItemTable(ref dtMasterData);
                        StagingTable = "Stg_Items";
                        ProcName = "sp_UpdateItems";
                        break;
                    case IntegrationField.Customer_U:
                        res = GetCustomerTable(ref dtMasterData);
                        StagingTable = "Stg_Customers";
                        ProcName = "sp_UpdateCustomers";
                        break;
                    case IntegrationField.Price_U:
                        res = GetPricesTable(ref dtMasterData);
                        StagingTable = "Stg_Prices";
                        ProcName = "sp_UpdatePrices";
                        break;
                    case IntegrationField.Salesperson_U:
                        res = GetSalespersonTable(ref dtMasterData);
                        StagingTable = "Stg_Salespersons";
                        ProcName = "sp_UpdateSalespersons";
                        break;
                    case IntegrationField.Stock_U:
                        res = GetStockTable(ref dtMasterData);
                        StagingTable = "Stg_Stock";
                        ProcName = "sp_UpdateStock";
                        break;
                    case IntegrationField.Invoice_U:
                        res = GetInvoicesTable(ref dtMasterData);
                        StagingTable = "Stg_Invoices";
                        ProcName = "sp_UpdateInvoices";
                        break;

                    case IntegrationField.Warehouse_U:
                        res = GetWarehousTable(ref dtMasterData);
                        StagingTable = "Stg_Warehouse";
                        ProcName = "sp_UpdateWarehouse";
                        break;


                    case IntegrationField.Bank_U:
                        res = GetBankTable(ref dtMasterData);
                        StagingTable = "Stg_Bank";
                        ProcName = "sp_UpdateBank";
                        break;
                }

                if (res != Result.Success)
                {
                    if (res == Result.Failure)
                        WriteMessage(" Error in reading from ERP !!");
                    else
                        WriteMessage(" No data found !!");

                    return;
                }
                WriteMessage(" Rows retrieved: " + dtMasterData.Rows.Count);
                execManager.UpdateActionTotalRows(TriggerID, dtMasterData.Rows.Count);

                WriteMessage("\r\nSaving data to staging table ... ");

                res = SaveTable(dtMasterData, StagingTable);
                if (res != Result.Success)
                {
                    WriteMessage(" Error in saving to staging table !!");
                    return;
                }
                WriteMessage(" Success ..");

                //WriteMessage("\r\nLooping through " + MasterName + " ...");

                //if (CoreGeneral.Common.CurrentSession.loginType != LoginType.WindowsService)
                //    bgwCheckProgress.RunWorkerAsync();

                //cmd = new SqlCommand(ProcName, db_vms.GetConnection());
                ////if(field==Fields.Stock_U)
                ////{
                ////    cmd.Parameters.Add(new SqlParameter("@WarehouseID", _WarehouseID));
                ////}
                //cmd.CommandTimeout = 3600000;
                //cmd.ExecuteNonQuery();

                //WriteMessage("\r\n" + MasterName + " updated ...");
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("\r\nError !!!");
            }
            finally
            {
                execManager.LogIntegrationEnding(ProcessID, res, "", "");
            }
        }
        private Result SaveTable(DataTable dtData, string TableName)
        {
            Result res = Result.Failure;
            try
            {
                dtData.Columns.Add("ID", typeof(int));
                dtData.Columns.Add("ResultID", typeof(int));
                dtData.Columns.Add("Message", typeof(string));
                dtData.Columns.Add("Inserted", typeof(bool));
                dtData.Columns.Add("Updated", typeof(bool));
                dtData.Columns.Add("Skipped", typeof(bool));
                dtData.Columns.Add("TriggerID", typeof(int));
                for (int i = 0; i < dtData.Rows.Count; i++)
                {
                    dtData.Rows[i]["ID"] = (i + 1);
                    dtData.Rows[i]["TriggerID"] = TriggerID;
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

        private Result GetItemTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            try
            {

                DT = new DataTable();
                DataTable DT2 = new DataTable();
                string body = @"Select
  I.CATID ItemCode,
  I.CATNAME ItemName,
  CACTIVE IsActive,
  I.SETNO DivCode,
  I.SETNAME DivName,
  I.MANUFACTURER Brand,
  P.UNIT,
  P.BARCODE,
I.CATID ,
1 qty
From
Tablet_ItemsV I
 INNER JOIN    CATEGORYT P on I.CATID=P.CATID
where P.CClass=1 
 
";
                string units= @"Select
  u.EQUCATID ItemCode,
  Ca.CATNAME ItemName,
  P.CACTIVE IsActive,
  Ca.SETNO DivCode,
  Ca.SETNAME DivName,
  Ca.MANUFACTURER Brand,
  P.UNIT,
  P.BARCODE,
 I.CATID,
u.EquCatQty qty

From
Tablet_ItemsV I
 INNER JOIN CATEGORYT P on I.CATID = P.CATID
 INNER JOIN CatEquationT u on I.CATID = U.CatID
 inner join Tablet_ItemsV Ca on Ca.catid = u.EquCatID
 where P.CClass = 3";

                ad = new OdbcDataAdapter(body, con);
                ad.Fill(DT);


                ad = new OdbcDataAdapter(units, con);
                ad.Fill(DT2);
                  
                foreach (DataRow dr in DT2.Rows)
                {
                    DT.Rows.Add(dr.ItemArray);
                } 

                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }

        private Result GetCustomerTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            try
            {
                DT = new DataTable();
                string body = @"Select
  V.ACCOUNTID CustomerCode,
  V.NAME,
  V.STATUS IsActive,
  V.OFFICEPHONE1 Phone,
  V.ADDRESS,
  V.EMAIL,
  V.ResellerNo Salesman,
  V.PriceID,
   A.MaxCR, A.MaxDB,A.MBalance
From Tablet_CustomersV V inner join
  AccountT A on V.AccountID=a.AccountID;";

                ad = new OdbcDataAdapter( body,con);
                ad.Fill(DT);

                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }

        private Result GetSalespersonTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            try
            {
                DT = new DataTable();
                string body = @"Select AccountID, Name From AccountT 
        Where Class = 9 And Father = 0 And Status = 1";

                ad = new OdbcDataAdapter(body, con);
                ad.Fill(DT);

                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        
 private Result GetBankTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            try
            {
                DT = new DataTable();
                string body = @"SELECT BB.BankCode ,BB.DescriptionEnglish,b.BankBranchCode+'-'+b.BankBranchDescription  Branch
  FROM  [InVanBankBranch] B inner join [InVanBanks] BB on b.BankCode=bb.BankCode
";

                ad = new OdbcDataAdapter(body, con);
                ad.Fill(DT);

                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }

        private Result GetWarehousTable(ref DataTable DT)
        {
           
            Result res = Result.Failure;
            try
            {
                DT = new DataTable();
                string body = @" Select * From StoreT Where SActive = 1";

                ad = new OdbcDataAdapter(body, con);
                ad.Fill(DT);

                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }


        private Result GetStockTable(ref DataTable DT)
        {
         
               
            Result res = Result.Failure;
            try
            {
                DT = new DataTable();
                string body = @"Select StoreID, CatID, sum(Quantity) Quantity,''WarehouseID,''Packid From CatStoreT group by StoreID, CatID";

                ad = new OdbcDataAdapter(body, con);
                ad.Fill(DT);

                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
             
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result GetInvoicesTable(ref DataTable DT)
        {


            Result res = Result.Failure;
            try
            {
                DT = new DataTable();
                string body = @"SELECT  LedgerT.EntryID,LedgerT.ManualNo, LedgerT.EntryAccount, AccountT.Name, sum(LedgerT.AEntryCredit)AEntryCredit,sum( LedgerT.AEntryDebit)AEntryDebit, AccountT.CurID, LedgerT.EntryTransDate, LedgerT.DocID,   LedgerT.DocCLass, LedgerT.ResalerID
FROM LedgerT INNER JOIN AccountT ON LedgerT.EntryAccount = AccountT.AccountID
WHERE AccountT.Class = 2
AND DelFlag IS NULL
And LedgerT.EntryTransDate Between '{0}' and '{1}'
and LedgerT.DocCLass in(100,200,300,600)
AND ( LedgerT.ManualNo IS NULL OR
        (
          LedgerT.ManualNo NOT STARTING WITH 'INV' and
          LedgerT.ManualNo NOT STARTING WITH 'PAY' and
          LedgerT.ManualNo NOT STARTING WITH 'UFOC' and
          LedgerT.ManualNo NOT STARTING WITH 'RTN' and
          LedgerT.ManualNo NOT STARTING WITH 'CN' and
          LedgerT.ManualNo NOT STARTING WITH 'DN' and
          LedgerT.ManualNo NOT STARTING WITH 'APP'
        )
    )group by LedgerT.EntryID,LedgerT.ManualNo, LedgerT.EntryAccount, AccountT.Name, AccountT.CurID, LedgerT.EntryTransDate, LedgerT.DocID,   LedgerT.DocCLass, LedgerT.ResalerID
;
";
      //              @"select [CustAccount]
      //, convert(varchar,[TRANSDATE],112)[TRANSDATE]
      //,[Amount]
      //,[RemainingAmount]
      //,[VOUCHER]
      //,[transactiontxt]
      //,[INVOICE]
      //,[Tax]
      //,[InVanOrderNumber]
      //,[Salesman]
      //,[CURRENCYCODE]
      //,[Reference]
      //, convert(varchar,[DUEDATE],112)[DUEDATE] from InVanCustOpenTrans  ";

                ad = new OdbcDataAdapter(string.Format( body,"01/01/2020" /*DateTime.Today.AddDays(-5).ToString("MM/dd/yyyy")*/, DateTime.Today.AddDays(1).ToString("MM/dd/yyyy")), con);
                ad.Fill(DT);

                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }

        private Result GetPricesTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            try
            {
                DT = new DataTable();
                string body = @" Select H.PriceID code,H.PriceName , D.CatID itemcode, D.SalePrice,ConstFV vat From CatPriceT D INNER JOIN PRICETYPET H
on D.PriceId=H.priceID INNER JOIN  ConstT on ConstID = 1001 ";

                ad = new OdbcDataAdapter(body, con);
                ad.Fill(DT);

                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        } 
        public override void SendReciepts( )
        {
            try
            {
                string salespersonFilter = "", cheeqNo = "", PaymentType="";
                string CustomerCode = "", cheeqDate = "", note = "", BankBranch = "", Amount = "", employeeid = "",
                    Bank ="" ,   PayID = "", Curnacy = "", IsDownPayment="",TransID="", DOCID = ""; 
                 DateTime TransactionDate;
                
                OdbcTransaction odbcTransaction = null;
                InCubeTransaction trn = null;
              int processID = 0;
                StringBuilder result = new StringBuilder();
                InCubeErrors err = InCubeErrors.NotInitialized;
                 Result res = Result.UnKnown;
                if (Filters.EmployeeID!=-1)
                {
                    salespersonFilter = " AND CP.EmployeeID = " + Filters.EmployeeID;
                }

                string invoicesHeader = string.Format(@"SELECT cp.employeeid,
                                                 CP.CustomerPaymentID, 
                                                 CO.CustomerCode,
                                                 CP.PaymentDate, 
                                                 CASE CP.PaymentTypeID WHEN 1 THEN 1 ELSE 2 END PaymentTypeID, 
                                                 sum(CP.AppliedAmount)AppliedAmount,
                                           isnull(CP.VoucherNumber,'') VoucherNumber, 
                                                 CP.VoucherDate, 
                                                 isnull(B.Code,'')  BankCode,
												 cr.Code Currency
                                                    , isnull(cp.notes,'')notes,
e.employeecode

,BB.Description Branch
,0 IsDownPayment
                                                 FROM CustomerPayment CP
                                                 INNER JOIN Customer C ON CP.CustomerID = C.CustomerID
                                                 INNER JOIN CustomerOutlet CO ON CP.CustomerID = CO.CustomerID AND CP.OutletID = CO.OutletID
                                                 INNER JOIN employee e  ON CP.EmployeeID=e.EmployeeID
                                                 LEFT OUTER JOIN Bank B ON CP.BankID = B.BankID
                                                LEFT JOIN BankBranchLanguage BB on CP.BankID=BB.BankID and CP.BranchID=BB.BranchID and BB.LanguageID=1
                                               INNER JOIN Currency Cr on CP.CurrencyID=cr.CurrencyID
                                               left join RouteHistory H on cp.RouteHistoryID=H.RouteHistoryID 
                                                 WHERE CP.Synchronized = 0 and (h.Uploaded=0 or isnull(cp.routehistoryid,-1)=-1) AND CP.PaymentStatusID <> 5 AND CP.PaymentTypeID IN (1,2,3)
                                                 AND CP.PaymentDate >= CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102) 
                                                 AND CP.PaymentDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102)  
 {6}
group by cp.employeeid,CP.CustomerPaymentID,CO.CustomerCode,CP.PaymentDate, CP.PaymentTypeID,  isnull(CP.VoucherNumber,''), CP.VoucherDate, isnull(B.Code,''),
cr.Code,isnull(cp.notes,''),e.employeecode,BB.Description 

  UNION ALL 
  SELECT cp.employeeid,
                                                 CP.CustomerPaymentID, 
                                                 CO.CustomerCode,
                                                 CP.PaymentDate, 
                                                 CASE CP.PaymentTypeID WHEN 1 THEN 1 ELSE 2 END PaymentTypeID, 
                                                 sum(CP.PaidAmount)AppliedAmount,
                                                isnull(CP.VoucherNumber,'') VoucherNumber, 
                                                   CP.VoucherDate, 
                                                 isnull(B.Code,'')  BankCode,
												 /*cr.Code*/'ILS' Currency
                                                    , isnull(cp.notes,'')notes,
e.employeecode

,BB.Description Branch
,1 IsDownPayment

                                                 FROM CustomerUnallocatedPayment CP
                                                 INNER JOIN Customer C ON CP.CustomerID = C.CustomerID
                                                 INNER JOIN CustomerOutlet CO ON CP.CustomerID = CO.CustomerID AND CP.OutletID = CO.OutletID
                                                 INNER JOIN employee e  ON CP.EmployeeID=e.EmployeeID
                                                 LEFT OUTER JOIN Bank B ON CP.BankID = B.BankID
                                                LEFT JOIN BankBranchLanguage BB on CP.BankID=BB.BankID and CP.BranchID=BB.BranchID and BB.LanguageID=1
                                               LEFT JOIN Currency Cr on CP.CurrencyID=cr.CurrencyID
                                                left join RouteHistory H on cp.RouteHistoryID=H.RouteHistoryID 
                                               WHERE CP.Synchronised = 0 and (h.Uploaded=0 or isnull(cp.routehistoryid,-1)=-1) AND isnull(CP.voided,0)<>1 AND CP.PaymentTypeID IN (1,2,3)
                                                 AND CP.PaymentDate >= CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102) 
                                                 AND CP.PaymentDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102) 
 {6}
group by cp.employeeid,CP.CustomerPaymentID,CO.CustomerCode,CP.PaymentDate, CP.PaymentTypeID, isnull(CP.VoucherNumber,'') , CP.VoucherDate,isnull(B.Code,''), 
cr.Code,isnull(cp.notes,''),e.employeecode,BB.Description", Filters.FromDate.Year, Filters.FromDate.Month, Filters.FromDate.Day, Filters.ToDate.Year, Filters.ToDate.Month, Filters.ToDate.Day, salespersonFilter);

                incubeQuery = new InCubeQuery(db_vms, invoicesHeader);



                //Logger.WriteLog(FromDate.ToString(), ToDate.ToString(), invoicesHeader, LoggingType.Information, LoggingFiles.errorInv);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Payment header query failed !!"));
                }



                DataTable dtInvoices = incubeQuery.GetDataTable();
                if (dtInvoices.Rows.Count == 0)
                    WriteMessage("There is no Payment to send ..");
                else
                    SetProgressMax(dtInvoices.Rows.Count);

                for (int i = 0; i < dtInvoices.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();

                        PayID = dtInvoices.Rows[i]["CustomerPaymentID"].ToString();
                        IsDownPayment = dtInvoices.Rows[i]["IsDownPayment"].ToString();
                        ReportProgress("Sending Payment: " + PayID);
                        WriteMessage("\r\n" + PayID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(IntegrationField.Reciept_S.GetHashCode(), PayID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Reciept_S, new List<string>(filters.Values), processID, 60);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            res = Result.Duplicate;
                            if (IsDownPayment == "0")
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerPayment] SET Synchronized = 1 WHERE CustomerPaymentID = '" + PayID + "'");
                            else
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerUnallocatedPayment] SET Synchronised = 1 WHERE CustomerPaymentID = '" + PayID + "'");
                            incubeQuery.ExecuteNonQuery();
                            throw (new Exception("Payment already sent  check table  Int_ExecutionDetails!!"));
                        }
                        string Salesperson = dtInvoices.Rows[i]["employeecode"].ToString();
                        TransactionDate = Convert.ToDateTime(dtInvoices.Rows[i]["PaymentDate"]);
                        CustomerCode = dtInvoices.Rows[i]["CustomerCode"].ToString();
                        PaymentType = dtInvoices.Rows[i]["PaymentTypeID"].ToString();
                        cheeqNo = dtInvoices.Rows[i]["VoucherNumber"].ToString();
                        cheeqDate = dtInvoices.Rows[i]["VoucherDate"].ToString();
                        Bank = dtInvoices.Rows[i]["BankCode"].ToString();
                        Curnacy = dtInvoices.Rows[i]["Currency"].ToString();
                        note = dtInvoices.Rows[i]["notes"].ToString();
                        BankBranch = dtInvoices.Rows[i]["Branch"].ToString();
                        Amount = dtInvoices.Rows[i]["AppliedAmount"].ToString();
                        employeeid = dtInvoices.Rows[i]["employeeid"].ToString();


                        trn = new InCubeTransaction();
                        // trn.BeginTransaction(db_erp);

                        DateTime VoucherDate = TransactionDate;
                        if (PaymentType.ToString().Trim() == "2" || PaymentType.ToString().Trim() == "3")
                            VoucherDate = DateTime.Parse(cheeqDate.ToString());

                        odbcTransaction = conSend.BeginTransaction();

                        string getId = ("select max(DOCNO)+1 from RECDOCT");
                        cmd = new OdbcCommand(getId, conSend, odbcTransaction);
                        DOCID = cmd.ExecuteScalar().ToString();

                        if (DOCID == null || DOCID.ToString() == "" || DOCID == string.Empty)
                        {
                            DOCID = "1";
                        }
                        string InsertedSql;

                        if (PaymentType.ToString().Trim() == "2" || PaymentType.ToString().Trim() == "3")
                            //     trn.BeginTransaction(db_erp);
                            InsertedSql = string.Format(" Insert into RECDOCT (DEVICEID,DOCNO,ACCOUNTID,MANUALNO," +
                                " DOCDATE,DOCTIME,CURID,DOCVALUE,RECPERNAME,FORWHAT	,NOTES	,RESALERID,CUSTID)" +
                                "Values({0},{1},{2},'{3}','{4}','{5}',{6},'{7}',{8},{9},'{10}',{11},'{12}'  )",
                               employeeid, DOCID, CustomerCode, PayID, TransactionDate.ToString("yyyy-MM-dd"),
                               TransactionDate.ToString("HH:mm"), 1, 0, "NULL", "NULL", note, Salesperson, "NULL");
                        else
                            InsertedSql = string.Format(" Insert into RECDOCT (DEVICEID,DOCNO,ACCOUNTID,MANUALNO," +
                                " DOCDATE,DOCTIME,CURID,DOCVALUE,RECPERNAME,FORWHAT	,NOTES	,RESALERID,CUSTID)" +
                                "Values({0},{1},{2},'{3}','{4}','{5}',{6},'{7}',{8},{9},'{10}',{11},{12}  )",
                             employeeid, DOCID, CustomerCode, PayID, TransactionDate.ToString("yyyy-MM-dd"),
                               TransactionDate.ToString("HH:mm"), 1, Amount, "NULL", "NULL", note, Salesperson, "NULL");
                        try
                        {
                            cmd = new OdbcCommand(InsertedSql, conSend, odbcTransaction);
                            cmd.ExecuteNonQuery();
                            res = Result.Success;
                        }
                        catch (Exception ex)
                        {
                            res = Result.Failure;
                            result.Append("Payment Header saving failed !!");
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace + "\r\n\r\n\r\n" + InsertedSql, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);

                        }

                        if (res != Result.Failure  )
                        {

 //                          
                                string InsertedDetSql = "";
                                if (PaymentType.ToString().Trim() == "2" || PaymentType.ToString().Trim() == "3")
                                {
                      
                                try
                                {
                                    InsertedDetSql = string.Format(" Insert into RECDOCDETT  (DEVICEID,DOCNO,SERIAL,DEBIT," +
                      " NOTES	,CHEQUE_CHEQUENO,CHEQUE_CDATE,CHEQUE_CBANK,CHEQUE_CBANKBRANCH,CHEQUE_BANKACC)" +
                      "Values({0},{1},{2},'{3}','{4}',{5},'{6}',{7},{8},'{9}')",
                     employeeid, DOCID, i, Amount, Bank + ':' + BankBranch, cheeqNo, VoucherDate.ToString("yyyy-MM-dd"), Bank, BankBranch, "");


                                    cmd = new OdbcCommand(InsertedDetSql, conSend, odbcTransaction);
                                    cmd.ExecuteNonQuery();
                                    res = Result.Success;
                                }
                                catch (Exception ex)
                                {
                                    res = Result.Failure;
                                    result.Append("Payment details saving failed !!");
                                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace + "\r\n\r\n\r\n" + InsertedDetSql, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);

                                    break;
                                }
 
                                res = Result.Success;
                            }

                        }
                       
                         if (res != null && res != Result.Success)
                        {
                            odbcTransaction.Rollback();
                            res = Result.NoFileRetreived;
                            WriteMessage("Error .. \r\n" + result.ToString());
                        }
                        else
                        {
                            odbcTransaction.Commit();
                            res = Result.Success;
                            result.Append("Success");
                            WriteMessage("Success");
                            QueryBuilderObject = new QueryBuilder();
                            if (IsDownPayment.ToString() == "0")
                            {
                                QueryBuilderObject.SetField("Synchronized", "1");
                                err = QueryBuilderObject.UpdateQueryString("CustomerPayment", " CustomerPaymentID = '" + PayID.ToString() + "'", db_vms);

                            }
                            else
                            {
                                QueryBuilderObject.SetField("Synchronised", "1");
                                err = QueryBuilderObject.UpdateQueryString("CustomerUnallocatedPayment", " CustomerPaymentID = '" + PayID.ToString() + "'", db_vms);

                            }
                            incubeQuery.ExecuteNonQuery();
                        }


                    }
                    catch (Exception ex)
                    {

                        if (odbcTransaction != null  ) odbcTransaction.Rollback();
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        result.Append(ex.Message);
                        if (res == Result.UnKnown)
                        {
                            res = Result.Failure;
                            WriteMessage("Unhandled exception !!");
                        }
                    }
                    finally
                    {
                        if (odbcTransaction != null ) odbcTransaction.Dispose();
                        execManager.LogIntegrationEnding(processID, res, "", result.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("Fetching Payment failed !!");
            }

        }
        #region SendOrders
        public override void SendOrders ()
        {
            try
            {
                string Employee = "";
                string Discount = "";  
                string UOM = "";
                string ItemCode = "";
                string salesTransTypeID = ""; 
                string Quantity = "";
                string Price = "";
                string NetTotal = "";
                string Tax = "";
                string note = "";
                DateTime TransactionDate, DesiredDeliveryDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                string salespersonFilter = "", netTotal = "", SalesGroup = "";
                string CustomerCode = "", TransactionID = "";
                InCubeErrors err;
                InCubeTransaction trn = null;
                Result res = Result.UnKnown;
        if (Filters.EmployeeID!=-1)
                {
                    salespersonFilter = "AND TR.EmployeeID = " + Filters.EmployeeID;
                }

                string invoicesHeader = string.Format(@"SELECT     
                                         TR.OrderID, 
                                         E.EmployeeCode ,
                                         CO.CustomerCode ,
                                         TR.orderdate, 
                                         TR.Discount, 
										 TR.NetTotal,
										 TR.Tax,
										 TR.DesiredDeliveryDate
                                         --INTO Stg_OrderHeader
 , isnull((select top(1) note from SalesOrderNote where customerid = TR.customerid and OutletID = TR.OutletID and OrderID = TR.OrderID and note <> ''),'') note

                                                  FROM SalesOrder TR
                                         INNER JOIN Employee E ON TR.EmployeeID = E.EmployeeID
                                        -- INNER JOIN EmployeeVehicle EV ON TR.EmployeeID = EV.EmployeeID
                                        -- INNER JOIN VehicleLoadingWh VL on EV.VehicleID = VL.VehicleID
                                         -- INNER JOIN Warehouse W ON VL.WarehouseID = W.WarehouseID
                                         INNER JOIN CustomerOutlet CO ON TR.CustomerID = CO.CustomerID AND TR.OutletID = CO.OutletID
                                         INNER JOIN Customer C ON CO.CustomerID = C.CustomerID


                                         WHERE TR.Synchronized = 0 AND(TR.OrderStatusID not in(8, 10, 12, 13)) AND C.New = 0
                                         {6}
                                            AND TR.OrderDate >= CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102) AND TR.OrderDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102) "
                                                 , Filters.FromDate.Year, Filters.FromDate.Month, Filters.FromDate.Day, Filters.ToDate.Year, Filters.ToDate.Month, Filters.ToDate.Day, salespersonFilter);
            
                incubeQuery = new InCubeQuery(db_vms, invoicesHeader);



                //Logger.WriteLog(FromDate.ToString(), ToDate.ToString(), invoicesHeader, LoggingType.Information, LoggingFiles.errorInv);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Invoices header query failed !!"));
                }



                DataTable dtInvoices = incubeQuery.GetDataTable();
                if (dtInvoices.Rows.Count == 0)
                    WriteMessage("There is no invoices to send ..");
                else
                    SetProgressMax(dtInvoices.Rows.Count);

                for (int i = 0; i < dtInvoices.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();
                    
                        TransactionID = dtInvoices.Rows[i]["OrderID"].ToString();
                        ReportProgress("Sending ORDERS: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(11, TransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Orders_S, new List<string>(filters.Values), processID, 60);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            res = Result.Duplicate;
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [SalesOrder] SET Synchronized = 1 WHERE OrderID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                                result.Append("Order already sent  check table  Int_ExecutionDetails!!");
                            throw (new Exception("Order already sent  check table  Int_ExecutionDetails!!"));
                         }

                        TransactionDate = Convert.ToDateTime(dtInvoices.Rows[i]["orderdate"]);
                        Employee = dtInvoices.Rows[i]["EmployeeCode"].ToString();
                        CustomerCode = dtInvoices.Rows[i]["CustomerCode"].ToString();
                        NetTotal = decimal.Parse(dtInvoices.Rows[i]["NetTotal"].ToString()).ToString("F4");
                        //SalesGroup = dtInvoices.Rows[i]["SalesGroup"].ToString();
                        Discount = dtInvoices.Rows[i]["Discount"].ToString();
                        Tax = dtInvoices.Rows[i]["Tax"].ToString();
                        note = dtInvoices.Rows[i]["note"].ToString();
                        DesiredDeliveryDate = Convert.ToDateTime(dtInvoices.Rows[i]["DesiredDeliveryDate"]);
                        trn = new InCubeTransaction();
                     //   trn.BeginTransaction(db_erp);
                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetStringField("TransID", TransactionID.ToString());
                        QueryBuilderObject.SetStringField("EmployeeCode", Employee.ToString());
                        QueryBuilderObject.SetStringField("CustomerCode", CustomerCode.ToString());
                        QueryBuilderObject.SetStringField("Discount", Discount.ToString());
                        QueryBuilderObject.SetStringField("NetTotal", NetTotal.ToString());
                        QueryBuilderObject.SetStringField("Tax", Tax.ToString());
                        QueryBuilderObject.SetDateField("orderdate", TransactionDate);
                        QueryBuilderObject.SetDateField("DesiredDeliveryDate", DesiredDeliveryDate);
                        QueryBuilderObject.SetField("ExportDate", "GetDate()");
                        QueryBuilderObject.SetField("Result", "0");
                        QueryBuilderObject.SetField("Notes","N'"+ note.ToString()+"'");
                        QueryBuilderObject.SetStringField("TransTypeID", "1");
                        
                     //   err = QueryBuilderObject.InsertQueryString("Stg_TrnsHeader", db_erp, trn);
                        #endregion
                       // if (err != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            result.Append("Order Header saving failed !!");
                        }
                        if (res != Result.Failure) {

                            string invoiceDetails = string.Format(@"SELECT     
                                                    p.SerialSeparator ItemCode,
                                                  TD.Quantity,
                                                  PTL.Description UOM,
                                                  TD.Price*((100+ TD.Tax)/100) Price,
                                             TD.Discount,--   ( ((TD.Price * TD.Discount/100)+((TD.Price-(TD.Price* TD.Discount/100)) * TD.PromotedDiscount/100))/isnull(nullif(TD.Price,0),1))*100  Discount,
--((TD.Price * TD.Discount/100)+((TD.Price-(TD.Price* TD.Discount/100)) * TD.PromotedDiscount/100))  Discount,
                                                  TD.Tax ,
                                                 case TD.SalesTransactionTypeID when 2 then 4 else TD.SalesTransactionTypeID end SalesTransactionTypeID

                                                  FROM SalesOrderDetail TD
                                                  INNER JOIN Pack P ON TD.PackID = P.PackID
                                                  INNER JOIN Item I ON P.ItemID = I.ItemID
                                                  INNER JOIN PackTypeLanguage PTL ON P.PackTypeID = PTL.PackTypeID 
                                                  
                                                  WHERE PTL.LanguageID = 1 AND TD.OrderID  = '{0}'", TransactionID);
                            incubeQuery = new InCubeQuery(db_vms, invoiceDetails);
                            if (incubeQuery.Execute() != InCubeErrors.Success)
                            {
                                res = Result.Failure;
                                result.Append("Order details query failed !!");
                            }

                            DataTable dtDetails = incubeQuery.GetDataTable();
                            string allDetails = "";
                            for (int j = 0; j < dtDetails.Rows.Count; j++)
                            {


                                ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                                UOM = dtDetails.Rows[j]["UOM"].ToString();
                                Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString("F4");//.ToString("#0.000");
                                Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString("F4");//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                                Tax = decimal.Parse(dtDetails.Rows[j]["Tax"].ToString()).ToString("F4");
                                Discount = decimal.Parse(dtDetails.Rows[j]["Discount"].ToString()).ToString("F4");
                                salesTransTypeID = dtDetails.Rows[j]["SalesTransactionTypeID"].ToString();
                              //  Price = (decimal.Parse(Price.ToString()) + decimal.Parse(Tax.ToString())).ToString("F4");

                                QueryBuilderObject = new QueryBuilder();
                                QueryBuilderObject.SetStringField("TransID", TransactionID.ToString());
                                QueryBuilderObject.SetStringField("ItemCode", ItemCode.ToString());
                                QueryBuilderObject.SetStringField("Discount", Discount.ToString());
                                QueryBuilderObject.SetStringField("Price", Price.ToString());
                                QueryBuilderObject.SetStringField("Tax", Tax.ToString());
                                QueryBuilderObject.SetStringField("UOM", UOM.ToString());
                                QueryBuilderObject.SetStringField("Quantity", Quantity.ToString());
                                QueryBuilderObject.SetStringField("TransTypeID", salesTransTypeID.ToString());
                            //    err = QueryBuilderObject.InsertQueryString("Stg_TrnsDetail", db_erp, trn);

                            //    if (err != InCubeErrors.Success)
                                {
                                    res = Result.Failure;
                                    result.Append("Order details saving failed !!");
                                    break;
                                }
                                res = Result.Success;
                            }

                        }

                        if (res != null && res !=Result.Success)
                        {
                            trn.Rollback();
                            res = Result.NoFileRetreived;
                            WriteMessage("Error .. \r\n" + result.ToString());
                        }
                        else
                        {
                            trn.Commit();
                            res = Result.Success;
                            result.Append("Success");
                            WriteMessage("Success");
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [SalesOrder] SET Synchronized = 1 WHERE OrderID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                        }


                    }
                    catch (Exception ex)
                    {

                        if (trn != null && trn.Transaction != null) trn.Rollback();
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        result.Append(ex.Message);
                        if (res == Result.UnKnown)
                        {
                            res = Result.Failure;
                            WriteMessage("Unhandled exception !!");
                        }
                    }
                    finally
                    {
                        if (trn != null && trn.Transaction != null) trn.Transaction.Dispose();
                        execManager.LogIntegrationEnding(processID, res, "", result.ToString());
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("Fetching orders failed !!");
            }

        }
        public override void SendInvoices( )
        {
            try
            {
                string Employee = "";
                string EmployeeID = "";
                string Discount = "";
                string UOM = "";
                string ItemCode = "";
                string salesTransTypeID = "";
                string Quantity = "";
                string Price = "";
                string NetTotal = "";
                string Tax = "";
                string CustomerName = "", FOC="";
                string note = "", TransactionTypeID="";
                string DOCID = "";
                string RETID = "";
                DateTime TransactionDate, DesiredDeliveryDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                string salespersonFilter = "";
                string CustomerCode = "", TransactionID = "", ReturnReason="";
                InCubeErrors err;
                string invoiceDetails = "";
                InCubeTransaction trn = null;
                OdbcTransaction odbcTransaction = null;
                Result res = Result.UnKnown;
                if (Filters.EmployeeID!=-1)
                {
                    salespersonFilter = "AND TR.EmployeeID = " + Filters.EmployeeID;
                }
                string Sequence = "";
                                string InsertedSql = "";
                string invoicesHeader = string.Format(@"SELECT     
                                         TR.TransactionID, 
                                         E.EmployeeCode ,
                                         E.EmployeeID ,
                                         CO.CustomerCode ,
                                         TR.TransactionDate, 
                                         TR.Discount, 
										 TR.NetTotal,
										 TR.Tax 
										,tr.Notes note
                                        ,COL.Description CustomerName
										,case when tr.TransactionTypeID in(1,3) then 3 else 4 end TransactionTypeID
                                                  FROM [Transaction] TR
                                         INNER JOIN Employee E ON TR.EmployeeID = E.EmployeeID 
                                         INNER JOIN Warehouse W ON TR.WarehouseID = W.WarehouseID
                                         INNER JOIN CustomerOutlet CO ON TR.CustomerID = CO.CustomerID AND TR.OutletID = CO.OutletID 
                                         INNER JOIN CustomerOutletLanguage COL ON TR.CustomerID = COl.CustomerID AND TR.OutletID = COl.OutletID and COL.languageid=1
                                         INNER JOIN Customer C ON CO.CustomerID = C.CustomerID
                                         WHERE TR.Synchronized = 0 AND 
                                        TR.TransactionTypeID in(1,2,3,4) AND TR.Voided <> 1   AND (c.New = 0)
                                        AND TR.TransactionDate >=  CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102)  AND TR.TransactionDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102) 
                                         {6} "
                                                 , Filters.FromDate.Year, Filters.FromDate.Month, Filters.FromDate.Day, Filters.ToDate.Year, Filters.ToDate.Month, Filters.ToDate.Day, salespersonFilter);

                incubeQuery = new InCubeQuery(db_vms, invoicesHeader);



                //Logger.WriteLog(FromDate.ToString(), ToDate.ToString(), invoicesHeader, LoggingType.Information, LoggingFiles.errorInv);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Invoices header query failed !!"));
                }



                DataTable dtInvoices = incubeQuery.GetDataTable();
                if (dtInvoices.Rows.Count == 0)
                    WriteMessage("There is no invoices to send ..");
                else
                    SetProgressMax(dtInvoices.Rows.Count);

                for (int i = 0; i < dtInvoices.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();
                        InsertedSql = "";
                        TransactionID = dtInvoices.Rows[i]["TransactionID"].ToString();
                        ReportProgress("Sending Invoice: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(IntegrationField.Sales_S.GetHashCode(), TransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        odbcTransaction = null;
                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Sales_S, new List<string>(filters.Values), processID, 60);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            res = Result.Duplicate;
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [Transaction] SET Synchronized = 1 WHERE TransactionID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                            result.Append("Invoice already sent");
                            throw (new Exception("Invoice already sent  check table  Int_ExecutionDetails!!"));
                        }      
                        TransactionDate = Convert.ToDateTime(dtInvoices.Rows[i]["TransactionDate"]);
                        Employee = dtInvoices.Rows[i]["EmployeeCode"].ToString();
                        EmployeeID = dtInvoices.Rows[i]["EmployeeID"].ToString();
                        CustomerCode = dtInvoices.Rows[i]["CustomerCode"].ToString();
                        CustomerName = dtInvoices.Rows[i]["CustomerName"].ToString();
                        NetTotal = decimal.Parse(dtInvoices.Rows[i]["NetTotal"].ToString()).ToString("F4"); 
                        Discount = dtInvoices.Rows[i]["Discount"].ToString();
                        Tax = dtInvoices.Rows[i]["Tax"].ToString();
                        note = dtInvoices.Rows[i]["note"].ToString();
                        TransactionTypeID = dtInvoices.Rows[i]["TransactionTypeID"].ToString();
                        DesiredDeliveryDate = Convert.ToDateTime(dtInvoices.Rows[i]["TransactionDate"]);


                        odbcTransaction=conSend.BeginTransaction();
                        string getId = ("select max(DOCNO)+1 from INV");
                        cmd = new OdbcCommand(getId, conSend, odbcTransaction);
                        DOCID=cmd.ExecuteScalar().ToString();
                        if (DOCID == null || DOCID.ToString()=="" || DOCID==string.Empty)
                        {
                            DOCID = "1";
                        }

                        //odbcTransaction = conSend.BeginTransaction();
                        string getRetId = ("select max(DOCNO)+1 from RETINV");
                        cmd = new OdbcCommand(getRetId, conSend, odbcTransaction);
                        RETID = cmd.ExecuteScalar().ToString();
                        if (RETID == null || RETID.ToString() == "" || RETID == string.Empty)
                        {
                            RETID = "1";
                        }



                        //     trn.BeginTransaction(db_erp);
                        if (   TransactionTypeID=="1"|| TransactionTypeID=="3")
                        InsertedSql = string.Format(" Insert into INV(DEVICEID,DOCNO,ACCOUNTID,MANUALNO," +
                            "VATTYPE,DOCDATE,DOCTIME,DISCOUNTV,DISCOUNTP,DOCVALUE,NOTES,PRINTED,NAME,RESALERID,CUSTID)" +
                            "Values({0},{1},{2},'{3}','{4}','{5}','{6}','{7}',{8},{9},'{10}',{11},'{12}',{13},{14})",
                           EmployeeID, DOCID, CustomerCode, TransactionID,1, TransactionDate.ToString("yyyy-MM-dd"), 
                           TransactionDate.ToString("HH:mm"),0,0, NetTotal, Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(note)), 1, Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(CustomerName)), Employee,"NULL");
                        else
                            InsertedSql = string.Format(" Insert into RETINV (DEVICEID,DOCNO,ACCOUNTID,MANUALNO," +
                                "VATTYPE,DOCDATE,DOCTIME,DISCOUNTV,DISCOUNTP,DOCVALUE,NOTES,PRINTED,NAME,RESALERID,RET_EXP_QTY,RET_ERR_QTY,CUSTID)" +
                                "Values({0},{1},{2},'{3}','{4}','{5}','{6}','{7}',{8},{9},'{10}',{11},'{12}',{13},{14},{15},{16})",
                               EmployeeID, RETID, CustomerCode, TransactionID, 1, TransactionDate.ToString("yyyy-MM-dd"),
                               TransactionDate.ToString("HH:mm"), 0, 0, NetTotal, Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(note)), 1, Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(CustomerName)), Employee,0,0,"NULL" );
                        try
                        {
                            cmd = new OdbcCommand(InsertedSql, conSend, odbcTransaction);
                            cmd.ExecuteNonQuery();
                            res = Result.Success;
                        }
                        catch (Exception ex)
                        {
                            res = Result.Failure;
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name,ex.Message , ex.StackTrace+"\r\n\r\n"+ InsertedSql, LoggingType.Error, LoggingFiles.InCubeLog);
                           // odbcTransaction.Rollback();

                        }
                        if (res == Result.Failure )
                        {
                           result.Append("Invoice Header saving failed !!");
                        }
                        if (res != Result.Failure)
                        {

                              invoiceDetails = string.Format(@"SELECT     
                                                  p.SerialSeparator ItemCode,
                                                  sum(TD.Quantity) Quantity,
                                                  PTL.Description UOM,
                                                  TD.Price+ (sum(TD.Tax)/sum(TD.Quantity)) Price,
                                              --  ( ((TD.Price * TD.Discount/100)+((TD.Price-(TD.Price* TD.Discount/100)) * TD.PromotedDiscount/100))/isnull(nullif(TD.Price,0),1))*100  Discount,
  (  sum(TD.Discount)/ sum(TD.Quantity))/(isnull( nullif(TD.Price,0),1) )*100.0    Discount,
Sequence,
   (sum(TD.Tax)/sum(TD.Quantity))/ isnull( nullif(TD.Price,0),1) Tax,
                                                 case TD.SalesTransactionTypeID when 2 then 4 else TD.SalesTransactionTypeID end SalesTransactionTypeID
                                                    ,rr.Description ReturnReason
                                                  FROM TransactionDetail TD
                                                  INNER JOIN Pack P ON TD.PackID = P.PackID
                                                  INNER JOIN Item I ON P.ItemID = I.ItemID
                                                  INNER JOIN PackTypeLanguage PTL ON P.PackTypeID = PTL.PackTypeID
												  left join ReturnReasonLanguage rr on TD.ReturnReason= rr.ReturnReasonID and rr.LanguageID=1
                                                  
                                                  WHERE PTL.LanguageID = 1 
												  AND TD.TransactionID    = '{0}'
Group by p.SerialSeparator ,  PTL.Description , TD.Price,Sequence,TD.SalesTransactionTypeID    ,rr.Description  ", TransactionID);
                            incubeQuery = new InCubeQuery(db_vms, invoiceDetails);
                            if (incubeQuery.Execute() != InCubeErrors.Success)
                            {
                                res = Result.Failure;
                                result.Append("Invoice details query failed !!");

                            }

                            DataTable dtDetails = incubeQuery.GetDataTable();
                            if(dtDetails== null || dtDetails.Rows.Count==0)
                                result.Append("Invoice details not found !!");
                           
                            for (int j = 0; j < dtDetails.Rows.Count; j++)
                            {


                                ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                                UOM = dtDetails.Rows[j]["UOM"].ToString();
                                Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString("F4");//.ToString("#0.000");
                                Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString("F4");//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                                Tax = decimal.Parse(dtDetails.Rows[j]["Tax"].ToString()).ToString("F4");
                                Discount = decimal.Parse(dtDetails.Rows[j]["Discount"].ToString()).ToString("F4");
                                salesTransTypeID = dtDetails.Rows[j]["SalesTransactionTypeID"].ToString();
                               // Price = (decimal.Parse(Price.ToString()) + decimal.Parse(Tax.ToString())).ToString("F4");
                                ReturnReason= dtDetails.Rows[j]["ReturnReason"].ToString();
                                Sequence = dtDetails.Rows[j]["Sequence"].ToString();
                                FOC = "0";
                                if (salesTransTypeID=="2" || salesTransTypeID=="4")
                                {
                                    FOC = Quantity;
                                    Quantity = "0";
                                }
                             
                                if (TransactionTypeID == "1" || TransactionTypeID == "3")
                                    InsertedSql = string.Format(" Insert into CATESINVDOCDETT (DEVICEID,DOCNO,ID,SERIAL," +
                                        "CATID,CATUNIT,CATQTY,PACKQTY,CATPRICE,CATDISCOUNT,CATBONUS,CATCOUNT,NOTES,CATL,CATW,CATH)" +
                                        "Values({0},{1},{2},{3},'{4}','{5}','{6}','{7}',{8},{9},'{10}',{11},'{12}',{13},{14},{15})",
                                       EmployeeID, DOCID, j+1, Sequence, ItemCode, "" ,
                                       Quantity, 1,Price, Discount, FOC, Quantity, "NULL", 0, 0,0);
                                else
                                    InsertedSql = string.Format(" Insert into RETINVDET (DEVICEID,DOCNO,ID,SERIAL," +
                                         "CATID,CATUNIT,CATQTY,PACKQTY,CATPRICE,CATDISCOUNT,CATBONUS,CATCOUNT,NOTES,RET_EXP_QTY	,RET_ERR_QTY	)" +
                                         "Values({0},{1},{2},{3},'{4}','{5}','{6}','{7}',{8},{9},'{10}',{11},'{12}',{13},{14})",
                                        EmployeeID, RETID, j + 1, Sequence, ItemCode, "",
                                        Quantity, 1, Price, Discount, FOC, Quantity, "NULL", 0, 0);
                                try
                                {
                                    cmd = new OdbcCommand(InsertedSql, conSend, odbcTransaction);
                                    cmd.ExecuteNonQuery();
                                    res = Result.Success;
                                }
                                catch (Exception ex)
                                {
                                    res = Result.Failure;
                                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name,ex.Message, ex.StackTrace+"\r\n\r\n\r\n"+ InsertedSql, LoggingType.Error, LoggingFiles.InCubeLog);
                                     
                                }

                                if ( res== Result.Failure)
                                {
                                    res = Result.Failure;
                                    result.Append("Invoice details saving failed !!");
                                    break;
                                }
                                res = Result.Success;
                            }

                        }

                        if (res != null && res != Result.Success)
                        {
                            odbcTransaction.Rollback();
                            res = Result.NoFileRetreived;
                            WriteMessage("Error .. \r\n" + result.ToString());
                        }
                        else
                        {
                            odbcTransaction.Commit();
                            res = Result.Success;
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [Transaction] SET Synchronized = 1 WHERE TransactionID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                            result.Append("Success");
                            WriteMessage("Success");

                        }


                    }
                    catch (Exception ex)
                    {

                        if (odbcTransaction != null) odbcTransaction.Rollback();
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace + "\r\n\r\n\r\n" + InsertedSql, LoggingType.Error, LoggingFiles.InCubeLog);
                        result.Append(ex.Message);
                        if (res == Result.UnKnown)
                        {
                            res = Result.Failure;
                            WriteMessage("Unhandled exception !!");
                        }
                    }
                    finally
                    {
                        if (odbcTransaction != null ) odbcTransaction.Dispose();
                        execManager.LogIntegrationEnding(processID, res, "", result.ToString());
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("Fetching Invoices failed !!");
            }

        }
    }
}