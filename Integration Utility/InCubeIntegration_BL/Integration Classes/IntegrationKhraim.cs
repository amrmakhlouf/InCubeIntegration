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
    public class IntegrationKhraim : IntegrationBase // Live branch
    {
        BackgroundWorker bgwCheckProgress;
        InCubeDatabase db_res,db_erp;
        int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
        SqlCommand cmd;
        InCubeQuery incubeQuery = null;
        QueryBuilder QueryBuilderObject =null;
        string StagingTable = "";
        string _WarehouseID = "-1";
        public IntegrationKhraim(long CurrentUserID, ExecutionManager ExecManager)
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
                string body = @"  SELECT   *   FROM  [S2B_vw_Incube_Items]";
                
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
                string body = @"select  *  from [S2B_vw_Incube_Customers]  ";
              
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
                string body = "select * from   [S2B_vw_Incube_Salesperson] ";

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
                string body = @"select  *  from [S2B_vw_Incube_Site]";

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
                string body = @"select  *,'' WarehouseID,'' PackID from [S2B_vw_Incube_Quantities] where [qtyavailable]<>0";

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
                string body = @"SELECT H.TransactionID, H.CustomerCode, H.Division, H.MasterCustomerCode, H.TransactionType,
convert(varchar,H.TransactionDate,112) TransactionDate , H.NetAmount, H.RemainingAmount, H.TaxAmount, H.SalespersonID,
H.EmployeeCode, H.OrganizationCode,  H.VanCode, H.Currency, H.OrderID, D.ItemNumber AS ItemNumber, D.UoM AS UOM, 
 D.Barcode, D.ItemType, D.QUANTITY, D.UnitPrice, D.TaxValue, D.DiscountValue,''CustomerID,''OutletID,''AccountId,''PackID,''divisionid
FROM           S2B_vw_InCubeSalesInvoiceHeaderView H INNER JOIN
               S2B_vw_InCubeSalesInvoiceDetailsView D ON H.TransactionID = D.TransactionID";

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
                string body = @"select  *  from [S2B_vw_Incube_PriceList]";

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
                    Bank ="" ,   PayID = "", routehistoryid = "", IsDownPayment="",TransID="", Type="";
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

                string invoicesHeader = string.Format(@"SELECT o.Organizationcode company,CP.CustomerPaymentID, C.CustomerCode, CO.CustomerCode OutletCode--,CP.transactionid
, CP.PaymentDate, CP.PaymentTypeID,  CP.PaidAmount AppliedAmount, E.EmployeeCode EmployeeCode,CP.VoucherNumber,cp.VoucherDate,BankLanguage.Description BankName,cp.routehistoryid,cp.Notes
                                        FROM CustomerUnallocatedPayment CP inner join Organization o on CP.Organizationid=o.Organizationid
                                        INNER JOIN CustomerOutlet CO ON CP.CustomerID = CO.CustomerID AND CP.OutletID = CO.OutletID
                                        INNER JOIN Customer C ON CP.CustomerID = C.CustomerID
                                        INNER JOIN Employee E ON CP.EmployeeID = E.EmployeeID
                                        LEFT JOIN BankLanguage on cp.BankID=BankLanguage.BankID and BankLanguage.LanguageID=1
                                        WHERE CP.PaymentTypeID in( 1,2,3) AND ISNULL(CP.voided,0) <> 1 AND CP.Synchronised = 0 
  AND CP.PaymentDate >='{0}'AND CP.PaymentDate <'{1}  {2} ' 
                                     ", Filters.FromDate.ToString("yyyy/MM/dd"), Filters.ToDate.AddDays(1).ToString("yyyy/MM/dd"), salespersonFilter);
              
               
                incubeQuery = new InCubeQuery(db_vms, invoicesHeader);



                //Logger.WriteLog(FromDate.ToString(), ToDate.ToString(), invoicesHeader, LoggingType.Information, LoggingFiles.errorInv);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Reciepts query failed !!"));
                }



                DataTable dtInvoices = incubeQuery.GetDataTable();
                if (dtInvoices.Rows.Count == 0)
                    WriteMessage("There is no Reciepts to send ..");
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
                        //IsDownPayment = dtInvoices.Rows[i]["IsDownPayment"].ToString();
                        ReportProgress("Sending Reciepts: " + PayID);
                        WriteMessage("\r\n" + PayID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(IntegrationField.Reciept_S.GetHashCode(), PayID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Reciept_S, new List<string>(filters.Values), processID, 60);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            res = Result.Duplicate;
                            
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerUnallocatedPayment] SET Synchronised = 1 WHERE CustomerPaymentID = '" + PayID + "'");
                            incubeQuery.ExecuteNonQuery();
                            throw (new Exception("Reciepts already sent  check table  Int_ExecutionDetails!!"));
                        }
                       string Salesperson = dtInvoices.Rows[i]["EmployeeCode"].ToString();
                        TransactionDate = Convert.ToDateTime(dtInvoices.Rows[i]["PaymentDate"]);
                        CustomerCode = dtInvoices.Rows[i]["CustomerCode"].ToString();
                        PaymentType = dtInvoices.Rows[i]["PaymentTypeID"].ToString();
                        cheeqNo = dtInvoices.Rows[i]["VoucherNumber"].ToString();
                        cheeqDate = dtInvoices.Rows[i]["VoucherDate"].ToString();
                        Bank = dtInvoices.Rows[i]["BankName"].ToString();
                        routehistoryid = dtInvoices.Rows[i]["routehistoryid"].ToString();
                        note        = dtInvoices.Rows[i]["Notes"].ToString();
                       // BankBranch = dtInvoices.Rows[i]["Branch"].ToString();
                        Amount      = dtInvoices.Rows[i]["AppliedAmount"].ToString();
                          Type = "Cash";
                        if (PaymentType.Trim() == "2" || PaymentType.Trim() == "3")
                            Type = "Cheque";

                        //trn = new InCubeTransaction();
                        //trn.BeginTransaction(db_erp);

                        
                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetStringField("ReceiptNumber", PayID.ToString());
                        QueryBuilderObject.SetDateField("ReceiptDate", TransactionDate.Date);
                        QueryBuilderObject.SetStringField("PaymentType", Type);
                        QueryBuilderObject.SetStringField("CustomerNumber", CustomerCode.ToString());
                        if (Type != "Cash")
                        {
                            DateTime VoucherDate = TransactionDate;
                            if (PaymentType.Trim() == "2" || PaymentType.Trim() == "3") VoucherDate = DateTime.Parse(cheeqDate.ToString());

                            QueryBuilderObject.SetStringField("ChequeNumber", cheeqNo.ToString());
                            QueryBuilderObject.SetDateField("ChequeDueDate", VoucherDate.Date);
                            QueryBuilderObject.SetStringField("BankName", Bank.ToString());
                        }
                        QueryBuilderObject.SetStringField("BatchNumber", routehistoryid.ToString());
                        QueryBuilderObject.SetStringField("EmployeeID", Salesperson.ToString());
                        QueryBuilderObject.SetStringField("Comment", note.ToString());
                        QueryBuilderObject.SetStringField("Amount", Amount.ToString()); 


                        err = QueryBuilderObject.InsertQueryString("S2B_Incube_ReceiptHeader", db_erp);

                        if (err != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            result.Append(string.Format("Reciepts saving failed {0}!!\r\n"+ QueryBuilderObject.CurrentException.ToString(), PayID));
                        }
                        else
                        {
                           // trn.Commit();
                            res = Result.Success;
                            result.Append("Success");
                            WriteMessage("Success "+ PayID.ToString());
                            QueryBuilderObject = new QueryBuilder();
                            
                                QueryBuilderObject.SetField("Synchronised", "1");
                                err = QueryBuilderObject.UpdateQueryString("CustomerUnallocatedPayment", " CustomerPaymentID = '" + PayID.ToString() + "'", db_vms);
 
                            incubeQuery.ExecuteNonQuery();
                        }


                    }
                    catch (Exception ex)
                    {

                      //  if (trn != null && trn.Transaction != null) trn.Rollback();
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
                       // if (trn != null && trn.Transaction != null) trn.Transaction.Dispose();
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
                string Division = "";
                string Tax = "";
                string note = "";
                DateTime TransactionDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                string salespersonFilter = "",   BasePrice = "", SalesTransactionTypeID = "";
                string CustomerCode = "", CustomerName="", TransactionID = "";
                InCubeErrors err;
                InCubeTransaction trn = null;
                Result res = Result.UnKnown;
        if (Filters.EmployeeID!=-1)
                {
                    salespersonFilter = "AND t.EmployeeID = " + Filters.EmployeeID;
                }

                string invoicesHeader = string.Format(@"select t.OrderID ,t.OrderDate  ,o.Organizationcode company,
                                        c.customercode,co.customercode outletcode,e.employeecode  ,t.CreatedBy EmpID
                                        ,pt.SimplePeriodWidth PaymentTerm,dl.Description divisioncode
										--,W.WarehouseCode
                                        ,col.description customerName,col.address,t.routehistoryid ,
											--ISNULL((SELECT sum(((td.Price-(td.Price* isnull(td.Discount,0)/100))* isnull(td.PromotedDiscount,0)/100 )*td.quantity) Discount
                                           -- FROM SalesOrderDetail TD  WHERE TD.OrderID =T.OrderID and TD.SalesTransactionTypeID=1),0) Discount,
											isnull((select top(1) note from SalesOrderNote where customerid=T.customerid and OutletID=T.OutletID and OrderID=T.OrderID and note<>''),'') note
from salesorder t inner join Organization o on t.Organizationid=o.Organizationid
                                        INNER JOIN Customer c on t.customerid=c.customerid
                                        INNER JOIN Customeroutlet co on t.customerid=co.customerid and t.outletid=co.outletid
                                        INNER JOIN Customeroutletlanguage col on t.customerid=col.customerid and t.outletid=col.outletid and col.languageid=1
                                        INNER JOIN employee e on t.employeeid=e.employeeid
                                        inner join Division d on t.DivisionID=d.DivisionID
                                        LEFT JOIN PaymentTerm pt on co.PaymentTermid=pt.PaymentTermid
                                        inner JOIN Divisionlanguage dl on t.divisionid=dl.divisionid and dl.LanguageID=1  
                                         WHERE t.Synchronized = 0 AND(t.OrderStatusID  =2) AND C.New = 0
                                         {6}
                                            AND t.OrderDate >= CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102) AND t.OrderDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102) "
                                                 , Filters.FromDate.Year, Filters.FromDate.Month, Filters.FromDate.Day, Filters.ToDate.Year, Filters.ToDate.Month, Filters.ToDate.Day, salespersonFilter);
            
                incubeQuery = new InCubeQuery(db_vms, invoicesHeader);



                //Logger.WriteLog(FromDate.ToString(), ToDate.ToString(), invoicesHeader, LoggingType.Information, LoggingFiles.errorInv);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Orders header query failed !!"));
                }



                DataTable dtInvoices = incubeQuery.GetDataTable();
                if (dtInvoices.Rows.Count == 0)
                    WriteMessage("There is no order to send ..");
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
                                result.Append(string.Format("Order []{0} already sent  check table  Int_ExecutionDetails!!", TransactionID));
                            throw (new Exception(string.Format("Order []{0} already sent  check table  Int_ExecutionDetails!!", TransactionID)));
                         }

                        TransactionDate = Convert.ToDateTime(dtInvoices.Rows[i]["orderdate"]);
                        Employee = dtInvoices.Rows[i]["EmployeeCode"].ToString();
                        CustomerCode = dtInvoices.Rows[i]["CustomerCode"].ToString();
                        CustomerName= dtInvoices.Rows[i]["CustomerName"].ToString(); 
                        //Discount = dtInvoices.Rows[i]["Discount"].ToString(); 
                        note = dtInvoices.Rows[i]["note"].ToString();
                      Division =  dtInvoices.Rows[i]["divisioncode"].ToString();
                        trn = new InCubeTransaction();
                        trn.BeginTransaction(db_erp);
                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetStringField("[DocumentNumber]", TransactionID.ToString());
                        QueryBuilderObject.SetStringField("EmployeeID", Employee.ToString());
                        QueryBuilderObject.SetStringField("CustomerNumber", CustomerCode.ToString());
                        //QueryBuilderObject.SetStringField("Discount", Discount.ToString());
                        QueryBuilderObject.SetStringField("[Division]", Division);
                        QueryBuilderObject.SetStringField("[DocumentType]", "1");
                        QueryBuilderObject.SetDateField("[DocumentDate]", TransactionDate);
                        QueryBuilderObject.SetStringField("[CustomerName]", CustomerName);
                        QueryBuilderObject.SetField("ExportDate", "GetDate()");
                        QueryBuilderObject.SetField("Note","N'"+ note.ToString()+"'");
                         
                        err = QueryBuilderObject.InsertQueryString("[S2B_Incube_SalesHeader]", db_erp, trn);
                        #endregion
                        if (err != InCubeErrors.Success) {
                            res = Result.Failure;
                            result.Append("Order Header saving failed !!");
                        }
                        if (res != Result.Failure) {

                            string invoiceDetails = string.Format(@"SELECT I.ItemCode, pt.description UOM, sum(td.Quantity)Quantity, 
/*td.Price,td.BasePrice,rr.description returnreason,I.PackDefinition ,(td.Price* isnull(td.Discount,0)/100)+((td.Price-(td.Price* isnull(td.Discount,0)/100) )*isnull(td.PromotedDiscount,0)/100) Discount*/
td.Price-(td.Price* isnull(td.Discount,0)/100)  as Price,td.Price BasePrice,rr.description returnreason,I.PackDefinition ,((td.Price-(td.Price* isnull(td.Discount,0)/100) )*isnull(td.PromotedDiscount,0)/100) Discount
,TD.SalesTransactionTypeID
FROM SalesOrderDetail TD 
INNER JOIN Pack P ON P.PackID = TD.PackID
inner join Item i on p.itemid=i.itemid
LEFT JOIN ReturnReasonLanguage rr on  td.ReturnReason=rr.ReturnReasonID and rr.languageid=1
LEFT JOIN PackTypeLanguage pt on p.Packtypeid=pt.Packtypeid and Pt.languageid=1
WHERE OrderID = '{0}'
GROUP BY P.ItemID,I.ItemCode ,td.Price,pt.description,rr.description,I.PackDefinition,td.BasePrice,td.SalesTransactionTypeID,td.Discount,td.PromotedDiscount
order by I.ItemCode,TD.SalesTransactionTypeID", TransactionID);
                            incubeQuery = new InCubeQuery(db_vms, invoiceDetails);
                            if (incubeQuery.Execute() != InCubeErrors.Success)
                            {
                                res = Result.Failure;
                                result.Append("Order details query failed !!");
                            }

                            DataTable dtDetails = incubeQuery.GetDataTable();
                             for (int j = 0; j < dtDetails.Rows.Count; j++)
                            {


                                ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                                UOM = dtDetails.Rows[j]["UOM"].ToString();
                                Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString("F4");//.ToString("#0.000");
                                Price = dtDetails.Rows[j]["Price"].ToString();
                                //Tax = decimal.Parse(dtDetails.Rows[j]["Tax"].ToString()).ToString("F4");
                                Discount = decimal.Parse(dtDetails.Rows[j]["Discount"].ToString()).ToString("F4");
                                salesTransTypeID = dtDetails.Rows[j]["SalesTransactionTypeID"].ToString();
                                  BasePrice = dtDetails.Rows[j]["BasePrice"].ToString();
                                 Discount = dtDetails.Rows[j]["Discount"].ToString();
                                SalesTransactionTypeID = dtDetails.Rows[j]["SalesTransactionTypeID"].ToString();
                                QueryBuilderObject = new QueryBuilder();
                                QueryBuilderObject.SetStringField("[DocumentNumber]", TransactionID.ToString());
                                QueryBuilderObject.SetStringField("[ItemNumber]", ItemCode.ToString());
                                QueryBuilderObject.SetStringField("[Unit]", UOM.ToString());
                                QueryBuilderObject.SetStringField("[LineDiscount]", Discount.ToString());
                                QueryBuilderObject.SetStringField("[Quantity]", Quantity);

                                if (SalesTransactionTypeID.Trim() == "4"|| SalesTransactionTypeID.Trim() == "2")
                                {
                                    QueryBuilderObject.SetStringField("[UnitPrice]", "0");// (decimal.Parse(price.ToString()) * decimal.Parse(quantity) * ((100 - decimal.Parse(ItemDiscount.ToString())) / 100)).ToString());
                                    QueryBuilderObject.SetStringField("[ExtendedPrice]", "0");// BasePrice.ToString());
                                }
                                else
                                {
                                    QueryBuilderObject.SetStringField("UnitPrice", Price.ToString());
                                    QueryBuilderObject.SetStringField("ExtendedPrice", (decimal.Parse(Price.ToString()) * decimal.Parse(Quantity) * ((100 - decimal.Parse(Discount.ToString())) / 100)).ToString());
                                }
                                QueryBuilderObject.SetStringField("CompanyID", "Khraim");

                                err = QueryBuilderObject.InsertQueryString("S2B_Incube_SalesDetails", db_erp, trn);


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
        public override void SendNewCustomers( )
        {
            try
            {
                string Phone= "";
                string Fax = "";
                string Description = "";
                string Address = "";
                string State = "";
                string City = "";
                string Area = "";
                string Street = "";
               
                int   processID = 0;
                StringBuilder result = new StringBuilder();
                 string CustomerCode = "" ;
                InCubeErrors err;
                 Result res = Result.UnKnown;
             

                string invoicesHeader = string.Format(@"select o.CustomerCode,O.Phone,o.Fax,ol.Description,ol.Address,stl.Description State,Cl.Description City,
AL.Description Area,SL.Description Street from CustomerOutlet o
inner join customer c on o.CustomerID=c.CustomerID
left join CustomerOutletLanguage OL on o.CustomerID=Ol.CustomerID and o.OutletID=ol.OutletID and ol.LanguageID=1
left join Street S on O.StreetID=s.StreetID
LEFT JOIN StreetLanguage SL on s.StreetID=Sl.StreetID and SL.LanguageID=1
LEFT JOIN AreaLanguage AL on s.AreaID=AL.AreaID and AL.LanguageID=1
LEFT JOIN CityLanguage CL on s.CityID=CL.CityID and CL.LanguageID=1
LEFT JOIN StateLanguage StL on s.StateID=StL.StateID and StL.LanguageID=1
where o.UpdatedBy<>0  and c.New=0");

                incubeQuery = new InCubeQuery(db_vms, invoicesHeader);



                //Logger.WriteLog(FromDate.ToString(), ToDate.ToString(), invoicesHeader, LoggingType.Information, LoggingFiles.errorInv);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Customer Updates query failed !!"));
                }



                DataTable dtInvoices = incubeQuery.GetDataTable();
                if (dtInvoices.Rows.Count == 0)
                    WriteMessage("There is no Customer Updates to send ..");
                else
                    SetProgressMax(dtInvoices.Rows.Count);

                for (int i = 0; i < dtInvoices.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();

                        CustomerCode = dtInvoices.Rows[i]["CustomerCode"].ToString();
                        ReportProgress("Sending Customer Updates: " + CustomerCode);
                        WriteMessage("\r\n" + CustomerCode + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(IntegrationField.NewCustomer_S.GetHashCode(), CustomerCode);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        Phone = dtInvoices.Rows[i]["Phone"].ToString();
                         Fax = dtInvoices.Rows[i]["Fax"].ToString();
                        Description = dtInvoices.Rows[i]["Description"].ToString();
                        Address = dtInvoices.Rows[i]["Address"].ToString();
                        State = dtInvoices.Rows[i]["State"].ToString();
                        City = dtInvoices.Rows[i]["City"].ToString();
                        Area = dtInvoices.Rows[i]["Area"].ToString();
                        Street = dtInvoices.Rows[i]["Street"].ToString();
                        
                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetStringField("[CustomerNumber]", CustomerCode.ToString());
                        QueryBuilderObject.SetStringField("[CustomerPhone]", Phone.ToString());
                        //QueryBuilderObject.SetStringField("CustomerCode", Fax.ToString());
                        QueryBuilderObject.SetStringField("[CustomerName]", Description.ToString());
                        QueryBuilderObject.SetStringField("[CustomerAddress]", Address.ToString());
                        QueryBuilderObject.SetStringField("[Address2]", Address.ToString());
                        QueryBuilderObject.SetStringField("[State]", State.ToString());
                        QueryBuilderObject.SetStringField("[CustomerCity]", City);
                        QueryBuilderObject.SetStringField("[Address1]", City);
                        QueryBuilderObject.SetStringField("[Address3]", Area);
                        //QueryBuilderObject.SetStringField("Address2", Street);

                        err = QueryBuilderObject.InsertQueryString("[S2B_Incube_Customers]", db_erp);
 
                        if (err != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            result.Append("Customer Updates saving failed !!");
                             
                            WriteMessage("Error .. \r\n" + result.ToString());
                        }
                     
                        else
                        { 
                            res = Result.Success;
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [Customeroutlet] SET UpdatedBy =0 WHERE customercode = '" + CustomerCode + "'");
                            incubeQuery.ExecuteNonQuery();
                            result.Append("Success");
                            WriteMessage("Success");

                        }


                    }
                    catch (Exception ex)
                    {

                      
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
                        execManager.LogIntegrationEnding(processID, res, "", result.ToString());
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("Fetching Customer Updates failed !!");
            }

        }
    }
}