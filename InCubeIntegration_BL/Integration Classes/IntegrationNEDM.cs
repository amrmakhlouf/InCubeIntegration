using System;
using System.Collections.Generic;
using System.Data; 
using InCubeIntegration_DAL;
using InCubeLibrary;
using System.Data.SqlClient;
using System.ComponentModel;
using System.Text;
using Newtonsoft.Json;

namespace InCubeIntegration_BL
{
    public class IntegrationNEDM : IntegrationBase // Live branch
    {
        BackgroundWorker bgwCheckProgress;
        InCubeDatabase db_res,db_erp;
        int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
        SqlCommand cmd;
        InCubeQuery incubeQuery = null;
        QueryBuilder QueryBuilderObject =null;
        string StagingTable = "";
        string _WarehouseID = "-1";
        public IntegrationNEDM(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            if (CoreGeneral.Common.CurrentSession.loginType != LoginType.WindowsService)
            {
                db_res = new InCubeDatabase();
                db_res.Open("InCube", "IntegrationNEDM");
           

            }

            db_erp = new InCubeDatabase();
            db_erp.Open("ERP", "IntegrationERP");

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

            if (db_erp != null && db_erp.GetConnection().State == ConnectionState.Open)
                db_erp.Close();
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
                string body = @"select  *  from InVanItems";
                
                incubeQuery = new InCubeQuery(body, db_erp);
                incubeQuery.Execute();
                DT = incubeQuery.GetDataTable();

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
                string body = @"select c.*,p.PriceListCode from InVanCustomers c left join InVanPriceList p
on c.CustomerCode=p.CustomerCode";
              
                incubeQuery = new InCubeQuery(body, db_erp);
                incubeQuery.Execute();
                DT = incubeQuery.GetDataTable();

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
                string body = "select * from InVanEmployees ";

                incubeQuery = new InCubeQuery(body, db_erp);
                incubeQuery.Execute();
                DT = incubeQuery.GetDataTable();

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

                incubeQuery = new InCubeQuery(body, db_erp);
                incubeQuery.Execute();
                DT = incubeQuery.GetDataTable();

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
                string body = @"select  *  from InVanWarehouses";

                incubeQuery = new InCubeQuery(body, db_erp);
                incubeQuery.Execute();
                DT = incubeQuery.GetDataTable();

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
                string body = @"select   [ItemCode]
      ,[Quantity]
      ,[WarehouseCode]
      ,[Batch]
      ,convert(nvarchar,[ExpiryDate],112) ExpiryDate
      ,[UOM],null WarehouseID,null Packid  from InVanWarehouseStock where [Quantity]<>0";

                incubeQuery = new InCubeQuery(body, db_erp);
                incubeQuery.Execute();
                DT = incubeQuery.GetDataTable();

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
                string body = @"select [CustAccount]
      , convert(varchar,[TRANSDATE],112)[TRANSDATE]
      ,[Amount]
      ,[RemainingAmount]
      ,[VOUCHER]
      ,[transactiontxt]
      ,[INVOICE]
      ,[Tax]
      ,[InVanOrderNumber]
      ,[Salesman]
      ,[CURRENCYCODE]
      ,[Reference]
      , convert(varchar,[DUEDATE],112)[DUEDATE] from InVanCustOpenTrans  ";

                incubeQuery = new InCubeQuery(body, db_erp);
                incubeQuery.Execute();
                DT = incubeQuery.GetDataTable();

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
                string body = @"select  *  from InVanItemPriceList";

                incubeQuery = new InCubeQuery(body, db_erp);
                incubeQuery.Execute();
                DT = incubeQuery.GetDataTable();

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
                string CustomerCode = "", cheeqDate="", note = "", BankBranch = "", Amount = "",
                    Bank ="" ,   PayID = "", Curnacy = "", IsDownPayment="",TransID="";
                DateTime TransactionDate;
                 ;
                InCubeTransaction trn = null;
              int processID = 0;
                StringBuilder result = new StringBuilder();
                InCubeErrors err = InCubeErrors.NotInitialized;
                 Result res = Result.UnKnown;
                if (Filters.EmployeeID!=-1)
                {
                    salespersonFilter = " AND CP.EmployeeID = " + Filters.EmployeeID;
                }

                string invoicesHeader = string.Format(@"SELECT 
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

,substring(BB.Description,0,CHARINDEX('-',BB.Description)) Branch
,0 IsDownPayment
                                                 FROM CustomerPayment CP
                                                 INNER JOIN Customer C ON CP.CustomerID = C.CustomerID
                                                 INNER JOIN CustomerOutlet CO ON CP.CustomerID = CO.CustomerID AND CP.OutletID = CO.OutletID
                                                 INNER JOIN employee e  ON CP.EmployeeID=e.EmployeeID
                                                 LEFT OUTER JOIN Bank B ON CP.BankID = B.BankID
                                                LEFT JOIN BankBranchLanguage BB on CP.BankID=BB.BankID and CP.BranchID=BB.BranchID and BB.LanguageID=1
                                               INNER JOIN Currency Cr on CP.CurrencyID=cr.CurrencyID
                                               inner join RouteHistory H on cp.RouteHistoryID=H.RouteHistoryID and h.Uploaded=0
                                                 WHERE CP.Synchronized = 0 AND CP.PaymentStatusID <> 5 AND CP.PaymentTypeID IN (1,2,3)
                                                 AND CP.PaymentDate >= CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102) 
                                                 AND CP.PaymentDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102)  
 {6}
group by CP.CustomerPaymentID,CO.CustomerCode,CP.PaymentDate, CP.PaymentTypeID,  isnull(CP.VoucherNumber,''), CP.VoucherDate, isnull(B.Code,''),
cr.Code,isnull(cp.notes,''),e.employeecode,BB.Description 

  UNION ALL 
  SELECT 
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

,substring(BB.Description,0,CHARINDEX('-',BB.Description)) Branch
,1 IsDownPayment

                                                 FROM CustomerUnallocatedPayment CP
                                                 INNER JOIN Customer C ON CP.CustomerID = C.CustomerID
                                                 INNER JOIN CustomerOutlet CO ON CP.CustomerID = CO.CustomerID AND CP.OutletID = CO.OutletID
                                                 INNER JOIN employee e  ON CP.EmployeeID=e.EmployeeID
                                                 LEFT OUTER JOIN Bank B ON CP.BankID = B.BankID
                                                LEFT JOIN BankBranchLanguage BB on CP.BankID=BB.BankID and CP.BranchID=BB.BranchID and BB.LanguageID=1
                                               LEFT JOIN Currency Cr on CP.CurrencyID=cr.CurrencyID
                                                 WHERE CP.Synchronised = 0 AND isnull(CP.voided,0)<>1 AND CP.PaymentTypeID IN (1,2,3)
                                                 AND CP.PaymentDate >= CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102) 
                                                 AND CP.PaymentDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102) 
 {6}
group by CP.CustomerPaymentID,CO.CustomerCode,CP.PaymentDate, CP.PaymentTypeID, isnull(CP.VoucherNumber,'') , CP.VoucherDate,isnull(B.Code,''), 
cr.Code,isnull(cp.notes,''),e.employeecode,BB.Description", Filters.FromDate.Year, Filters.FromDate.Month, Filters.FromDate.Day, Filters.ToDate.Year, Filters.ToDate.Month, Filters.ToDate.Day, salespersonFilter);

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
                            if(IsDownPayment=="0")
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
                        note        = dtInvoices.Rows[i]["notes"].ToString();
                        BankBranch = dtInvoices.Rows[i]["Branch"].ToString();
                        Amount      = dtInvoices.Rows[i]["AppliedAmount"].ToString();

                             
                        trn = new InCubeTransaction();
                        trn.BeginTransaction(db_erp);

                        DateTime VoucherDate = TransactionDate;
                        if (PaymentType.ToString().Trim() == "2") VoucherDate = DateTime.Parse(cheeqDate.ToString());
                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetStringField("CustomerPaymentID", PayID.ToString());
                        QueryBuilderObject.SetStringField("PaymentTypeID", PaymentType.ToString());
                        QueryBuilderObject.SetStringField("CustomerCode", CustomerCode.ToString());
                        QueryBuilderObject.SetStringField("AppliedAmount", Amount.ToString());
                        if (PaymentType.ToString().Trim() == "2")
                        {
                            QueryBuilderObject.SetStringField("VoucherNumber", cheeqNo.ToString());
                            QueryBuilderObject.SetDateField("VoucherDate", VoucherDate);
                            QueryBuilderObject.SetStringField("BankCode", Bank.ToString());
                        }
                        QueryBuilderObject.SetDateField("PaymentDate", TransactionDate);
                        QueryBuilderObject.SetField("ExportDate", "GetDate()");
                        QueryBuilderObject.SetField("Result", "0");
                        QueryBuilderObject.SetStringField("Currency", Curnacy.ToString());
                        QueryBuilderObject.SetField("Notes", "N'" + note.ToString() + "'");
                        QueryBuilderObject.SetStringField("employeecode", Salesperson.ToString());
                        QueryBuilderObject.SetStringField("BankBranch", BankBranch.ToString());
                        QueryBuilderObject.SetStringField("IsDownPayment", "0");


                        err = QueryBuilderObject.InsertQueryString("Stg_CollectionHeader", db_erp, trn);
 
                        if (err != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            result.Append("Payment Header saving failed !!");
                        }

                        if (res != Result.Failure && IsDownPayment.ToString() == "0")
                        {

                            string invoiceDetails = string.Format(@"SELECT 
                                                CP.TransactionID,
                                                AppliedAmount
                                                FROM CustomerPayment CP
                                                WHERE  CP.CustomerPaymentID='{0}'    
 AND CP.PaymentStatusID <> 5 AND CP.PaymentTypeID IN (1,2,3) ", PayID);
                            incubeQuery = new InCubeQuery(db_vms, invoiceDetails);
                            if (incubeQuery.Execute() != InCubeErrors.Success)
                            {
                                res = Result.Failure;
                                result.Append("Payment details query failed !!");
                            }

                            DataTable dtDetails = incubeQuery.GetDataTable();
                            string allDetails = "";
                            for (int j = 0; j < dtDetails.Rows.Count; j++)
                            {


                                TransID = dtDetails.Rows[j]["TransactionID"].ToString();
                                Amount = dtDetails.Rows[j]["AppliedAmount"].ToString();


                                QueryBuilderObject = new QueryBuilder();
                                QueryBuilderObject.SetStringField("CustomerPaymentID", PayID.ToString());
                                QueryBuilderObject.SetStringField("TransactionID", TransID.ToString());
                                QueryBuilderObject.SetStringField("AppliedAmount", Amount.ToString());
                                err = QueryBuilderObject.InsertQueryString("Stg_CollectionDetail", db_erp, trn);


                                if (err != InCubeErrors.Success)
                                {
                                    res = Result.Failure;
                                    result.Append("Payment details saving failed !!");
                                    break;
                                }
                                res = Result.Success;
                            }

                        }
                        else if (res != Result.Failure && IsDownPayment.ToString() == "1")
                        {
                            res = Result.Success;
                        }

                         if (res != null && res != Result.Success)
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
                string OrderTypeID = "";
                string ReturnReason = "";
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
										 TR.DesiredDeliveryDate,
TR.OrderTypeID
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
                        OrderTypeID = dtInvoices.Rows[i]["OrderTypeID"].ToString();
                        DesiredDeliveryDate = Convert.ToDateTime(dtInvoices.Rows[i]["DesiredDeliveryDate"]);
                        trn = new InCubeTransaction();
                        trn.BeginTransaction(db_erp);
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
                        QueryBuilderObject.SetStringField("TransTypeID", OrderTypeID);
                        
                        err = QueryBuilderObject.InsertQueryString("Stg_TrnsHeader", db_erp, trn);
                        #endregion
                        if (err != InCubeErrors.Success) {
                            res = Result.Failure;
                            result.Append("Order Header saving failed !!");
                        }
                        if (res != Result.Failure) {

                            string invoiceDetails = string.Format(@"SELECT     
                                                  I.ItemCode,
                                                  TD.Quantity,
                                                  PTL.Description UOM,
                                                  TD.Price*((100+ TD.Tax)/100) Price,
                                             TD.Discount,--   ( ((TD.Price * TD.Discount/100)+((TD.Price-(TD.Price* TD.Discount/100)) * TD.PromotedDiscount/100))/isnull(nullif(TD.Price,0),1))*100  Discount,
--((TD.Price * TD.Discount/100)+((TD.Price-(TD.Price* TD.Discount/100)) * TD.PromotedDiscount/100))  Discount,
                                                  TD.Tax ,
                                                 case TD.SalesTransactionTypeID when 2 then 4 else TD.SalesTransactionTypeID end SalesTransactionTypeID
  ,nullif(TD.ReturnReason,-1) ReturnReason
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
                                ReturnReason = dtDetails.Rows[j]["ReturnReason"].ToString();

                                QueryBuilderObject = new QueryBuilder();
                                QueryBuilderObject.SetStringField("TransID", TransactionID.ToString());
                                QueryBuilderObject.SetStringField("ItemCode", ItemCode.ToString());
                                QueryBuilderObject.SetStringField("Discount", Discount.ToString());
                                QueryBuilderObject.SetStringField("Price", Price.ToString());
                                QueryBuilderObject.SetStringField("Tax", Tax.ToString());
                                QueryBuilderObject.SetStringField("UOM", UOM.ToString());
                                QueryBuilderObject.SetStringField("Quantity", Quantity.ToString());
                                QueryBuilderObject.SetStringField("TransTypeID", salesTransTypeID.ToString());
                                QueryBuilderObject.SetField("ReturnReason","N'"+ ReturnReason.ToString()+"'");
                                err = QueryBuilderObject.InsertQueryString("Stg_TrnsDetail", db_erp, trn);

                                if (err != InCubeErrors.Success)
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
                string Discount = "";
                string UOM = "";
                string ItemCode = "";
                string salesTransTypeID = "";
                string Quantity = "";
                string Price = "";
                string NetTotal = "";
                string Tax = "";
                string note = "", TransactionTypeID="";
                DateTime TransactionDate, DesiredDeliveryDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                string salespersonFilter = "";
                string CustomerCode = "", TransactionID = "", ReturnReason="";
                InCubeErrors err;
                InCubeTransaction trn = null;
                Result res = Result.UnKnown;
                if (Filters.EmployeeID!=-1)
                {
                    salespersonFilter = "AND TR.EmployeeID = " + Filters.EmployeeID;
                }

                string invoicesHeader = string.Format(@"SELECT     
                                         TR.TransactionID, 
                                         E.EmployeeCode ,
                                         CO.CustomerCode ,
                                         TR.TransactionDate, 
                                         TR.Discount, 
										 TR.NetTotal,
										 TR.Tax 
										,tr.Notes note
										,case when tr.TransactionTypeID in(1,3) then 3 else 4 end TransactionTypeID
                                                  FROM [Transaction] TR
                                         INNER JOIN Employee E ON TR.EmployeeID = E.EmployeeID 
                                         INNER JOIN Warehouse W ON TR.WarehouseID = W.WarehouseID
                                         INNER JOIN CustomerOutlet CO ON TR.CustomerID = CO.CustomerID AND TR.OutletID = CO.OutletID
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

                        TransactionID = dtInvoices.Rows[i]["TransactionID"].ToString();
                        ReportProgress("Sending Invoice: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(IntegrationField.Sales_S.GetHashCode(), TransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

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
                        CustomerCode = dtInvoices.Rows[i]["CustomerCode"].ToString();
                        NetTotal = decimal.Parse(dtInvoices.Rows[i]["NetTotal"].ToString()).ToString("F4"); 
                        Discount = dtInvoices.Rows[i]["Discount"].ToString();
                        Tax = dtInvoices.Rows[i]["Tax"].ToString();
                        note = dtInvoices.Rows[i]["note"].ToString();
                        TransactionTypeID = dtInvoices.Rows[i]["TransactionTypeID"].ToString();
                        DesiredDeliveryDate = Convert.ToDateTime(dtInvoices.Rows[i]["TransactionDate"]);
                        trn = new InCubeTransaction();
                        trn.BeginTransaction(db_erp);
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
                        QueryBuilderObject.SetField("Notes", "N'" + note.ToString() + "'");
                        QueryBuilderObject.SetStringField("TransTypeID", TransactionTypeID);

                        err = QueryBuilderObject.InsertQueryString("Stg_TrnsHeader", db_erp, trn);
 
                        if (err != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            result.Append("Invoice Header saving failed !!");
                        }
                        if (res != Result.Failure)
                        {

                            string invoiceDetails = string.Format(@"SELECT     
                                                  I.ItemCode,
                                                  TD.Quantity,
                                                  PTL.Description UOM,
                                                  TD.Price+ (TD.Tax/TD.Quantity) Price,
                                              --  ( ((TD.Price * TD.Discount/100)+((TD.Price-(TD.Price* TD.Discount/100)) * TD.PromotedDiscount/100))/isnull(nullif(TD.Price,0),1))*100  Discount,
  (  TD.Discount/ TD.Quantity)/isnull( nullif(TD.Price,0),1) *100.0    Discount,
                                                (TD.Tax/TD.Quantity)/ isnull( nullif(TD.Price,0),1) Tax,
                                                 case TD.SalesTransactionTypeID when 2 then 4 else TD.SalesTransactionTypeID end SalesTransactionTypeID
                                                    ,nullif(TD.ReturnReason,-1) ReturnReason
                                                  FROM TransactionDetail TD
                                                  INNER JOIN Pack P ON TD.PackID = P.PackID
                                                  INNER JOIN Item I ON P.ItemID = I.ItemID
                                                  INNER JOIN PackTypeLanguage PTL ON P.PackTypeID = PTL.PackTypeID
												    
                                                  WHERE PTL.LanguageID = 1 
												  AND TD.TransactionID    = '{0}'", TransactionID);
                            incubeQuery = new InCubeQuery(db_vms, invoiceDetails);
                            if (incubeQuery.Execute() != InCubeErrors.Success)
                            {
                                res = Result.Failure;
                                result.Append("Invoice details query failed !!");
                            }

                            DataTable dtDetails = incubeQuery.GetDataTable();
                            if(dtDetails== null || dtDetails.Rows.Count==0)
                                result.Append("Invoice details not found !!");
                            string allDetails = "";
                            for (int j = 0; j < dtDetails.Rows.Count; j++)
                            {


                                ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                                UOM = dtDetails.Rows[j]["UOM"].ToString();
                                Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString("F4");//.ToString("#0.000");
                                Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString("F2");//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                                Tax = decimal.Parse(dtDetails.Rows[j]["Tax"].ToString()).ToString("F4");
                                Discount = decimal.Parse(dtDetails.Rows[j]["Discount"].ToString()).ToString("F4");
                                salesTransTypeID = dtDetails.Rows[j]["SalesTransactionTypeID"].ToString();
                               // Price = (decimal.Parse(Price.ToString()) + decimal.Parse(Tax.ToString())).ToString("F4");
                                ReturnReason= dtDetails.Rows[j]["ReturnReason"].ToString();

                                QueryBuilderObject = new QueryBuilder();
                                QueryBuilderObject.SetStringField("TransID", TransactionID.ToString());
                                QueryBuilderObject.SetStringField("ItemCode", ItemCode.ToString());
                                QueryBuilderObject.SetStringField("Discount", Discount.ToString());
                                QueryBuilderObject.SetStringField("Price", Price.ToString());
                                QueryBuilderObject.SetStringField("Tax", Tax.ToString());
                                QueryBuilderObject.SetStringField("UOM", UOM.ToString());
                                QueryBuilderObject.SetStringField("Quantity", Quantity.ToString());
                                QueryBuilderObject.SetStringField("TransTypeID", salesTransTypeID.ToString());
                                QueryBuilderObject.SetStringField("returnReason", ReturnReason.ToString());
                                
                                err = QueryBuilderObject.InsertQueryString("Stg_TrnsDetail", db_erp, trn);

                                if (err != InCubeErrors.Success)
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
                            trn.Rollback();
                            res = Result.NoFileRetreived;
                            WriteMessage("Error .. \r\n" + result.ToString());
                        }
                        else
                        {
                            trn.Commit();
                            res = Result.Success;
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [Transaction] SET Synchronized = 1 WHERE TransactionID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                            result.Append("Success");
                            WriteMessage("Success");

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
                WriteMessage("Fetching Invoices failed !!");
            }

        }
    }
}