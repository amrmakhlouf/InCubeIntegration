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
    public class IntegrationPepsiPal : IntegrationBase // Live branch
    {
        BackgroundWorker bgwCheckProgress;
        InCubeDatabase db_res;
        int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
        SqlCommand cmd;
        InCubeQuery incubeQuery = null;
        string StagingTable = "";
         string _WarehouseID = "-1";
        public IntegrationPepsiPal(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            if (CoreGeneral.Common.CurrentSession.loginType != LoginType.WindowsService)
            {
                db_res = new InCubeDatabase();
                db_res.Open("InCube", "IntegrationPepsiPal");
            }



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
        }

        public override void UpdateItem()
        {
            GetMasterData(IntegrationField.Item_U);
        }

        public override void UpdateStock()
        {
             _WarehouseID = Filters.WarehouseID.ToString();
           // GetMasterData(IntegrationField.Stock_U);
        }
        public override void UpdateCustomer()
        {
            GetMasterData(IntegrationField.Customer_U);
        }

        public override void UpdateSalesPerson()
        {
            GetMasterData(IntegrationField.Salesperson_U);
        }

        public override void UpdatePrice()
        {
            GetMasterData(IntegrationField.Price_U);
        }
        public override void UpdateMainWarehouse()
        {
            GetMasterData(IntegrationField.Warehouse_U);
        }
        public override void OutStanding()
        {
            GetMasterData(IntegrationField.Outstanding_U);
        }
        public override void UpdateInvoice()
        {
            GetMasterData(IntegrationField.Invoice_U);
        }
        private void GetMasterData(IntegrationField field)
        {
            Result res = Result.UnKnown;
            Dictionary<int, string> Filters;
            int ProcessID = 0;
            string Messg = "";
            try
            {
                string MasterName = field.ToString().Substring(0, field.ToString().Length - 2);
                string ProcName = "";
                WriteMessage("\r\nRetrieving " + MasterName + " from ERP ... ");
                
                //Log begining of read from SAP
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
                    case IntegrationField.Invoice_U:
                        res = GetInvoicesTable(ref dtMasterData);
                        StagingTable = "Stg_UnsettledInvoices";
                        ProcName = "sp_UpdateInvoices";
                        break;
                    case IntegrationField.Salesperson_U:
                        res = GetSalespersonTable(ref dtMasterData);
                        StagingTable = "Stg_Salespersons";
                        ProcName = "sp_UpdateSalespersons";
                        break;
                    case IntegrationField.Outstanding_U:
                        res = GetCustomerBalanceTable(ref dtMasterData);
                        StagingTable = "Stg_CustomerBalances";
                        ProcName = "sp_UpdateCustomerBalances";
                        break;
                    case IntegrationField.Stock_U:
                        res = GetStockTable(ref dtMasterData);
                        StagingTable = "Stg_Stock";
                        ProcName = "sp_UpdateStock";
                        break;

                    case IntegrationField.Warehouse_U:
                        res = GetWarehousTable(ref dtMasterData);
                        StagingTable = "Stg_Warehouse";
                        ProcName = "sp_UpdateWarehouse";
                        break;
                }

                if (res != Result.Success)
                {
                    if (res == Result.Failure)
                        WriteMessage(" Error in reading from SAP !!");
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
                Messg = ex.Message;
            }
            finally
            {
                execManager.LogIntegrationEnding(ProcessID, res, "", Messg);
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
            {// { \"field\" : \"ISNULL(isAssetItem)\", \"operation\" : \"=\",\"value\" : 0 }
                DT = new DataTable();
                string body = "{\"user\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_UserName + "\",\"password\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_Password + "\",\"command\":\"table\",\"table\":\"Item\",\"fields\":[\"code\",\"name\",\"nameAR\",\"used\",\"itemFamily.code\",\"unitList.unit\",\"unitList.partNumber\" ,\"unitList.packVolume\",\"itemFamily.nameAR\",\"itemCategory.code\",\"itemCategory.name\",\"brand.name\",\"enabled\",\"group\",\"group.name\",\"classification.name\",\"isAssetItem\"] " +
                        ",\"filters\" : [  { \"field\" : \"type\", \"operation\" : \"!=\",\"value\" : \"Service\" }] " +
                    "}";

                DT = Tools.GetRequestTable<PalItem>(CoreGeneral.Common.GeneralConfigurations.WS_URL, body, "rows", CoreGeneral.Common.GeneralConfigurations.SSL_File, CoreGeneral.Common.GeneralConfigurations.SSL_Key);


                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                throw new Exception(ex.Message);
            }
            return res;
        }
        private Result GetInvoicesTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            try

            {
                string body = "{\"user\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_UserName + "\",\"password\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_Password + "\",\"command\":\"report\",\"report\":\"unsettledReceivablesRpt\" " +
                       ",\"filters\" : [ ]}";

                DT = Tools.GetRequestTable<UnsettledInvoices>(CoreGeneral.Common.GeneralConfigurations.WS_URL, body, "rows", CoreGeneral.Common.GeneralConfigurations.SSL_File, CoreGeneral.Common.GeneralConfigurations.SSL_Key);

                if (DT != null && DT.Rows.Count > 0)
                {
                    res = Result.Success;
                }
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
                string body = "{\"user\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_UserName + "\",\"password\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_Password + "\",\"command\":\"table\",\"table\":\"contact\",\"fields\":[\"code\",\"name\",\"phone\",\"creditDays\",\"cusCurrency.symbol\",\"maxCredit\",\"area.nameAR\",\"area\",\"treeParent\",\"chkMaxCredit\",\"alert\",\"area\",\"treeParent\",\"chkMaxCredit\",\"children\",\"code\",\"type\",\"creditDays\",\"creditDaysFromEndOfMonth\",\"cusCurrency\",\"currentBalance\",\"isCustomer\",\"cusAccount\",\"cusContact\",\"customerExpiry\",\"cusPriceList\",\"country.name\",\"dependants\",\"discountPercent\",\"education\",\"email\",\"isEmployee\",\"empAccount\",\"empCurrency\",\"enabled\",\"fax\",\"gender\",\"headerFld\",\"homePhone\",\"idCard\",\"jobTitle\",\"lastChanged\",\"treeLevel\",\"maritalStatus\",\"maxCredit\",\"memberCard\",\"mobile\",\"name\",\"notes\",\"payToDriver\",\"personalCheck\",\"phone\",\"printContactBalance\",\"printPrices\",\"salesman\",\"sector\",\"students\",\"isSupplier\",\"supAccount\",\"supContact\",\"supCurrency\",\"supPriceList\",\"supTaxFormat\",\"taxId\",\"cusTaxType\",\"used\",\"sector\",\"memberCard.code\",\"memberCard.name\",\"type.code\",\"type.name\"] ," +
                                "\"filters\" : [ { \"field\" : \"isCustomer\"," +
                    " \"operation\" : \"=\"," +
                    "\"value\" : \"TRUE\" } ]}";
                DT = Tools.GetRequestTable<PalCustomers>(CoreGeneral.Common.GeneralConfigurations.WS_URL, body, "rows", CoreGeneral.Common.GeneralConfigurations.SSL_File, CoreGeneral.Common.GeneralConfigurations.SSL_Key);
                //@"https://gw.bisan.com/api/odemo_2"
                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                throw new Exception(ex.Message);
            }
            return res;
        }

        private Result GetSalespersonTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            try
            {
                DT = new DataTable();
                string body = "{\"user\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_UserName + "\",\"password\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_Password + "\",\"command\":\"table\",\"table\":\"Salesman\",\"fields\":[\"code\",\"emp.name\"] " +
                    //    ",\"filters\" : [ { \"field\" : \"isCustomer\"," +
                    //  " \"operation\" : \"=\"," +
                    //   "\"value\" : \"Yes\" }] " +
                    "}";

                DT = Tools.GetRequestTable<Salesman>(CoreGeneral.Common.GeneralConfigurations.WS_URL, body, "rows", CoreGeneral.Common.GeneralConfigurations.SSL_File, CoreGeneral.Common.GeneralConfigurations.SSL_Key);


                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                throw new Exception(ex.Message);
            }
            return res;
        }

        private Result GetWarehousTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            try
            {
                DT = new DataTable();
                string body = "{\"user\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_UserName + "\",\"password\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_Password + "\",\"command\":\"table\",\"table\":\"Warehouse\",\"fields\":[\"code\",\"name\",\"truck\" ] " +
                     ",\"filters\" : [ { \"field\" : \"enabled\"," +
                   " \"operation\" : \"=\"," +
                    "\"value\" : \"Yes\" }] " +
                 "}";

                DT = Tools.GetRequestTable<Warehouse>(CoreGeneral.Common.GeneralConfigurations.WS_URL, body, "rows", CoreGeneral.Common.GeneralConfigurations.SSL_File, CoreGeneral.Common.GeneralConfigurations.SSL_Key);


                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                throw new Exception(ex.Message);
            }
            return res;
        }


        private Result GetStockTable(ref DataTable DT)
        {
         
                //DT = new DataTable();
                //DT.Columns.Add("WarehouseCode", System.Type.GetType("System.String"));
                //DT.Columns.Add("ItemCode", System.Type.GetType("System.String"));
                //DT.Columns.Add("UOM", System.Type.GetType("System.String"));
                //DT.Columns.Add("Lot", System.Type.GetType("System.String"));
                //DT.Columns.Add("Qty", System.Type.GetType("System.String"));
                //DT.Columns.Add("ExpiryDate", System.Type.GetType("System.String"));
                //DT.Columns.Add("PackID", System.Type.GetType("System.Int16"));
                //DT.Columns.Add("WarehouseID", System.Type.GetType("System.Int16"));
            Result res = Result.Failure;
            try
            {
                DT = new DataTable();
                string body = "{\"user\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_UserName + "\",\"password\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_Password + "\",\"command\":\"report\",\"report\":\"stockBalance\" " +
                        ",\"filters\" : [ { \"field\" : \"byWarehouse\",\"value\" : \"true\" }" +
                        " ,{ \"field\" : \"includeZeroBalances\",\"value\" : \"false\" }" +
                        "  ] " +
                    "}";

                DT = Tools.GetRequestTable<ItemStock>(CoreGeneral.Common.GeneralConfigurations.WS_URL, body, "rows", CoreGeneral.Common.GeneralConfigurations.SSL_File, CoreGeneral.Common.GeneralConfigurations.SSL_Key);


                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                throw new Exception(ex.Message);
            }
            return res;
        }




        private Result GetCustomerBalanceTable(ref DataTable DT)
        {

          
            Result res = Result.Failure;
            try
            {
                DT = new DataTable();
                string body = "{\"user\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_UserName + "\",\"password\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_Password + "\",\"command\":\"report\",\"report\":\"customerBalance\" " +
                        ",\"filters\" : [ { \"field\" : \"fromDate\",\"value\" : \""+DateTime.Now.ToString("dd/MM/yyyy")+"\" }" +
                        " ,{ \"field\" : \"toDate\",\"value\" : \"" + DateTime.Now.ToString("dd/MM/yyyy") + "\" }" +
                        " ,{ \"field\" : \"currency\",\"value\" : \"02\" }" +
                        " ,{ \"field\" : \"currency_By\",\"value\" : \"true\" }" +
                        "  ] " +
                    "}";
                
                DT = Tools.GetRequestTable<CustBalance>(CoreGeneral.Common.GeneralConfigurations.WS_URL, body, "rows", CoreGeneral.Common.GeneralConfigurations.SSL_File, CoreGeneral.Common.GeneralConfigurations.SSL_Key);


                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                throw new Exception(ex.Message);
            }
            return res;
        }

        private Result GetPricesTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            try
            {
                DT = new DataTable();
                string body = "{\"user\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_UserName + "\",\"password\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_Password + "\",\"command\":\"table\",\"table\":\"ItemPrice\",\"fields\":[\"priceList\",\"item\",\"unit\",\"currency.symbol\",\"rawPrice\",\"taxedPrice\" ] " +
                    //    ",\"filters\" : [ { \"field\" : \"isCustomer\"," +
                    //  " \"operation\" : \"=\"," +
                    //   "\"value\" : \"Yes\" }] " +
                    "}";

                DT = Tools.GetRequestTable<PalPriceList>(CoreGeneral.Common.GeneralConfigurations.WS_URL, body, "rows", CoreGeneral.Common.GeneralConfigurations.SSL_File, CoreGeneral.Common.GeneralConfigurations.SSL_Key);


                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                throw new Exception(ex.Message);
            }
            return res;
        } 
        public override void SendReciepts( )
        {
            try
            {
                string salespersonFilter = "",fieldName="", cheeqNo = "", amount = "", amount2 = "", Curnacy2 = "",  cash,  cheeq;
                string CustomerCode = "", BlockNumber = "", PType="", Bank ="" , Branch="", CusAccount ="", SupAccount="", TransactionID = "", WarehouseCode = "", Curnacy1 = "",collectDiscount="";
                DateTime TransactionDate;
                DateTime cheeqDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                 Result res = Result.UnKnown;

                string headerTemp = "\"table\":\"mreceiptvoucher\",\"__TRANSACTION_ID\":\"{0}\",\"__DEVICE_ID\":\"{1}\",\"contact\":\"{2}\",\"salesman\":\"{3}\"," +
"\"docDate\":\"{4}\"{5}," +
"\"receipt\":{6} ," +
"\"comment\":\"InCube {7}\",\"manualNum\":\"{8}\"";


                string receiptTemp = "\"account\":\"{0}\",\"currency\":\"{1}\",\"{4}\":\"{2}\"{3}";
                string cheeqTemp     = ",\"dueDate\":\"{0}\",\"checkNumber\":\"{1}\",\"bank\":\"{2}\",\"thirdParty\":\"FALSE\"";//{11} =FALSE or TRUE
                string CNTemp = "\"table\":\"dCNote\",\"__TRANSACTION_ID\":\"{0}\",\"__DEVICE_ID\":\"{1}\",\"contact\":\"{2}\",\"salesman\":\"{3}\"," +
"\"account\":\"{4}\" ,\"currency\":{5} ," +
"\"noteType\":\"Credit Note\",\"docFormat\":\"Include Tax\",\"docDate\":\"{6}\",\"details\":{7},\"comment\":\"{8}\""; ;//,\"totalNet\":\"{9}\",\"accountTotal\":\"{9}\"  ";
                string CNTempDetail = "\"account\":\"{0}\",\"currency\":\"{1}\",\"value\":\"{2}\"";


                if (Filters.EmployeeID!=-1)
                {
                    salespersonFilter = "AND CP.EmployeeID = " + Filters.EmployeeID;
                }
                string invoicesHeader = string.Format(@"SELECT  1 PType,   CP.CustomerPaymentID,e.EmployeeCode, CP.PaymentDate, isnull( nullif(sum(SecondCurrencyAmount),0), sum(AppliedAmount)) AppliedAmount,sum(SecondCurrencyAmount)SecondCurrencyAmount,cp.ExchangeRate,isnull(c2.Description,c.Description  ) Currency , c2.Description Currency2,o.CustomerCode,cp.VoucherDate,cp.VoucherNumber,b.Code bank, Bl.description branch 
                                      ,cp.notes   ,O.RoadNumber CusAccount, O.ShopNumber SupAccount ,O.BlockNumber, sum(ISNULL(CP.CollectionDiscount,0)  ) CollectionDiscount  , e.HourlyRegularRate cash,e.HourlyOvertimeRate cheeq
									  FROM CustomerPayment CP left join [transaction] t on cp.TransactionID=t.TransactionID
											left join CustomerOutlet  o on o.CustomerID=cp.CustomerID and  o.OutletID=cp.OutletID  
											  INNER JOIN Customer  Cc  ON Cc .CustomerID = cp.CustomerID 
                  	left join CurrencyLanguage c on cp.CurrencyID=c.CurrencyID and c.LanguageID=1
												left join CurrencyLanguage c2 on cp.SecondCurrencyID=c2.CurrencyID and c2.LanguageID=1
                                                 left join Employee e on cp.EmployeeID=e.EmployeeID
                                              Left Join Bank B on CP.BankID=B.BankID  
                                                Left Join BankBranchLanguage Bl on CP.BankID=Bl.BankID and CP.BranchID=Bl.BranchID and Bl.LanguageID=1
                                                WHERE       CP.PaymentStatusID <> 5 AND CP.PaymentTypeID IN (1,2,3) AND cc.new<>1 
 AND CP.Synchronized=0
 {6}
                       AND CP.PaymentDate >= CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102) 
                                                 AND CP.PaymentDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102)

group by   CP.CustomerPaymentID,e.EmployeeCode,CP.PaymentDate, cp.ExchangeRate,c.Description ,O.BlockNumber, c2.Description ,o.CustomerCode,cp.VoucherDate,cp.VoucherNumber,b.Code, Bl.description,cp.notes,o.RoadNumber , O.ShopNumber   
 , e.HourlyRegularRate ,e.HourlyOvertimeRate 
union all

SELECT 2 PType,  CP.CustomerPaymentID,e.EmployeeCode, CP.PaymentDate,  PaidAmount AppliedAmount,0 SecondCurrencyAmount,1 ExchangeRate,c.Description Currency, '' Currency2,o.CustomerCode,cp.VoucherDate,cp.VoucherNumber,b.Code bank, Bl.description branch
                                      ,cp.notes   ,O.RoadNumber CusAccount, O.ShopNumber SupAccount ,O.BlockNumber, 0 CollectionDiscount   , e.HourlyRegularRate cash,e.HourlyOvertimeRate cheeq
									  FROM CustomerUnallocatedPayment CP  
											left join CustomerOutlet  o on o.CustomerID=cp.CustomerID and  o.OutletID=cp.OutletID  
												left join CurrencyLanguage c on cp.CurrencyID=c.CurrencyID and c.LanguageID=1
												 left join Employee e on cp.EmployeeID=e.EmployeeID
                                              Left Join Bank B on CP.BankID=B.BankID  
                                                Left Join BankBranchLanguage Bl on CP.BankID=Bl.BankID and CP.BranchID=Bl.BranchID and Bl.LanguageID=1
                                                WHERE       CP.voided <>1 AND CP.PaymentTypeID IN (1,2,3)
 AND CP.Synchronised=0
                        {6}
                       AND CP.PaymentDate >= CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102) 
                                                 AND CP.PaymentDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102)
 
"
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
 
                        TransactionID = dtInvoices.Rows[i]["CustomerPaymentID"].ToString();
                        ReportProgress("Sending Payment: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(12, TransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Reciept_S, new List<string>(filters.Values), processID, 60);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            res = Result.Duplicate;
                            if (PType == "1")
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerPayment] SET Synchronized = 1 WHERE CustomerPaymentID = '" + TransactionID + "'");
                            else
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerUnallocatedPayment] SET Synchronised = 1 WHERE CustomerPaymentID = '" + TransactionID + "'"); 
                            incubeQuery.ExecuteNonQuery();
                            throw (new Exception("CustomerPayment already sent  check table  Int_ExecutionDetails!!"));
                        }



                        TransactionDate = Convert.ToDateTime(dtInvoices.Rows[i]["PaymentDate"]);
                     string   Salesperson = dtInvoices.Rows[i]["Employeecode"].ToString();
                        PType =dtInvoices.Rows[i]["PType"].ToString();
                        CustomerCode = dtInvoices.Rows[i]["CustomerCode"].ToString();
                        amount = dtInvoices.Rows[i]["AppliedAmount"].ToString();
                        amount = dtInvoices.Rows[i]["AppliedAmount"].ToString();
                        CusAccount = dtInvoices.Rows[i]["CusAccount"].ToString();
                        SupAccount = dtInvoices.Rows[i]["SupAccount"].ToString();
                        Curnacy1 = dtInvoices.Rows[i]["Currency"].ToString();
                        Curnacy2 = dtInvoices.Rows[i]["Currency2"].ToString();
                        cheeqNo = dtInvoices.Rows[i]["VoucherNumber"].ToString();
                        Bank = dtInvoices.Rows[i]["Bank"].ToString();
                        Branch = dtInvoices.Rows[i]["Branch"].ToString();
                        BlockNumber = dtInvoices.Rows[i]["BlockNumber"].ToString();
                        collectDiscount = dtInvoices.Rows[i]["CollectionDiscount"].ToString();
                        cash = dtInvoices.Rows[i]["cash"].ToString();
                        cheeq = dtInvoices.Rows[i]["cheeq"].ToString();

                        if(cheeq.Trim()=="" || cash.Trim()=="")
                        {
                            res = Result.Invalid;
                            WriteMessage("Cash or cheeq box not defined to emplyee "+ Salesperson+" .. \r\n" );
                            result.Append("Cash or cheeq box not defined to emplyee " + Salesperson + " .. \r\n");
                            throw new Exception("Cash or cheeq box not defined to emplyee " + Salesperson + " .. \r\n");
                        }

                        CusAccount = cash;// "1101";
                        if(Curnacy1=="01") 
                         fieldName = "dbAmount";
                        else
                        fieldName = "dbValue";
                        // string receiptTemp = "{\"account\":\"{0}\",\"currency\":\"{1}\",\"subAct\":\"{3}\",\"dbAmount\":\"{4}\" {7}}";
                        string cheeqInfo = ""; 
                        if (cheeqNo!=null&& cheeqNo.ToLower()!="null"&& cheeqNo.Trim()!="")
                        {
                            CusAccount = cheeq;// "1200";

                            cheeqDate = Convert.ToDateTime(dtInvoices.Rows[i]["VoucherDate"].ToString());
                             cheeqInfo = string.Format(cheeqTemp, cheeqDate.ToString("dd/MM/yyyy"), cheeqNo, Bank );
                        }
                        string allDetails = "";
                      //  if (Curnacy2.Trim()=="")
                            allDetails = "[{" + string.Format(receiptTemp, CusAccount, Curnacy1,  amount, cheeqInfo, fieldName) + "}]";
                      //  else
                     //       allDetails = "[{" + string.Format(receiptTemp, CusAccount, Curnacy2,   amount2, cheeqInfo, fieldName) + "}]";

                  //      allDetails = "[" + allDetails.Substring(0, allDetails.Length - 1) + "]";
                        string headerData = string.Format(headerTemp, TransactionID.Replace("PAY-", "3").Replace("-", ""), "", CustomerCode, Salesperson, DateTime.Parse(TransactionDate.ToString()).ToString("dd/MM/yyyy"), "", 
                           allDetails, "", TransactionID.Replace("PAY-", "3").Replace("-", ""));//, "\"\"");
                     
                        string body = "{\"user\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_UserName + "\",\"password\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_Password + "\",\"command\":\"save\",\"record\":{" + headerData + "}}";
                        result[] result1 = Tools.GetRequest<result>(CoreGeneral.Common.GeneralConfigurations.WS_URL, body, "", CoreGeneral.Common.GeneralConfigurations.SSL_File, CoreGeneral.Common.GeneralConfigurations.SSL_Key);
                        if (result1 == null || (result1[0].error != null && result1[0].error.Trim() != ""))
                        {
                            res = Result.NoFileRetreived;
                            WriteMessage("Error .. \r\n" + result1[0].error);
                            result.Append("Pay No: " + TransactionID + "\r\n Message:" + (result1[0].error != null ? result1[0].error : "") + "\r\n json:" + body);
                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + result1[0].rows.code + "\r\n Message:" + (result1[0].error != null ? result1[0].error : "") + "\r\n json:" + body);
                            WriteMessage("Success, ERP No: " + result1[0].rows.code);
                           if(PType=="1")
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE CustomerPayment SET Synchronized = 1,Notes=Notes+' ,ERP:"+ result1[0].rows.code+ "' WHERE CustomerPaymentID = '" + TransactionID + "'");
                            else
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE CustomerUnallocatedPayment SET Synchronised = 1,Notes=Notes+' ,ERP:" + result1[0].rows.code+ "' WHERE CustomerPaymentID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                        }
                        if(collectDiscount.Trim()!="" && decimal.Parse(collectDiscount)>0)
                        {
                            allDetails = "[{" + string.Format(CNTempDetail, "3200", Curnacy1,  collectDiscount) + "}]";

                            headerData = string.Format(CNTemp, TransactionID.Replace("PAY-", "3").Replace("-", "")+"1", "", CustomerCode, Salesperson, "1300",Curnacy1, DateTime.Parse(TransactionDate.ToString()).ToString("dd/MM/yyyy")
                             , allDetails, "Collection Dsiacuont on"+ TransactionID, collectDiscount);


          
                              body = "{\"user\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_UserName + "\",\"password\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_Password + "\",\"command\":\"save\",\"record\":{" + headerData + "}}";
                           result1 = Tools.GetRequest<result>(CoreGeneral.Common.GeneralConfigurations.WS_URL, body, "", CoreGeneral.Common.GeneralConfigurations.SSL_File, CoreGeneral.Common.GeneralConfigurations.SSL_Key);
                            if (result1 == null || (result1[0].error != null && result1[0].error.Trim() != ""))
                            {
                                res = Result.NoFileRetreived;
                                WriteMessage("Error in send collection discount for paymentId "+TransactionID+".. \r\n" + result1[0].error);
                            }
                            else
                            {
                                res = Result.Success;
                                result.Append("ERP No: " + result1[0].rows.code + "\r\n Message:" + (result1[0].error != null ? result1[0].error : "") + "\r\n json:" + body);
                                WriteMessage("Success, ERP CN No: " + result1[0].rows.code);
                              
                                 if (PType == "1")
                                    incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerPayment] SET Synchronized = 1 ,Notes=Notes+' ,CN:" + result1[0].rows.code + "'  WHERE CustomerPaymentID = '" + TransactionID + "'");
                                else
                                    incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerUnallocatedPayment] SET Synchronised = 1 ,Notes=Notes+' ,CN:" + result1[0].rows.code + "'  WHERE CustomerPaymentID = '" + TransactionID + "'");

                                incubeQuery.ExecuteNonQuery();
                            }
                            result.Append("Pay No CN " + TransactionID + "\r\n Message:" + (result1[0].error != null ? result1[0].error : "") + "\r\n json:" + body);

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
                WriteMessage("Fetching invoices failed !!");
            }

        }
        public override void SendOrders()
        {
            try
            {
                string salespersonFilter = "", SalesDocType = "", netTotal = "", VehicleCode = "", SalesOffice = "", SalesGroup = "";
                string CustomerCode = "", note="", BlockNumber = "", TransactionID = "", WarehouseCode = "", Currency = "";
                DateTime TransactionDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;

                string headerTemp = "\"table\":\"{5}\",\"__TRANSACTION_ID\":\"{0}\",\"__DEVICE_ID\":\"{1}\",\"contact\":\"{2}\",\"salesman\":\"{3}\"," +
"\"docDate\":\"{4}\",\"currency\":\"{12}\",\"docFormat\":\"Include Tax\",\"warehouse\":\"{6}\"," +
"\"orderDetail\":{7},\"discountPercent\":\"{8}\",\"discountTotal\":\"{9}\",\"totalNet\":\"{10}\"," +
"\"comment\":\"InCube {11}\"";//,\"receipt\":{12}";


                string detailsTemp = "\"item\":\"{0}\",\"unit\":\"{1}\",\"price\":\"{2}\",\"quantity\":\"{3}\",\"bonus\":\"{4}\",\"discountPercent\":\"{5}\" ";
                string receiptTemp = "\"account\":\"{0}\",\"currency\":\"{1}\",\"subAct\":\"{3}\",\"reference\":\"{4}\",\"dbValue\":\"{5}\",\"dbAmount\":\"{6}\",\"dueDate\":\"{7}\",\"checkNumber\":\"{8}\",\"bank\":\"{9}\",\"accountNumber\":\"{10}\",\"thirdParty\":\"{11}\"";//{11} =FALSE or TRUE

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND T.EmployeeID = " + Filters.EmployeeID;
                }
                string invoicesHeader = string.Format(@"SELECT   T.OrderID,   T.OrderDate ,  V.WarehouseCode,e.Employeecode , CO.CustomerCode , t.Discount,t.NetTotal,CO.BlockNumber,isnull(cl.description,'02') Currency
                    ,isnull((select top(1) note from SalesOrderNote where customerid=T.customerid and OutletID=T.OutletID and OrderID=T.OrderID and note<>''),'') note
                    FROM SalesOrder T 
                    INNER JOIN CustomerOutlet CO ON CO.CustomerID = T.CustomerID AND CO.OutletID = T.OutletID
                    INNER JOIN Customer  C  ON C .CustomerID = T.CustomerID --AND CO.OutletID = T.OutletID
                    INNER JOIN Organization O ON O.OrganizationID = T.OrganizationID
                    INNER JOIN Employee E ON E.EmployeeID = T.EmployeeID
					LEFT JOIN EmployeeVehicle ev on e.EmployeeID=ev.EmployeeID
					LEFT JOIN VehicleLoadingWh vw on ev.VehicleID=vw.VehicleID
                    LEFT JOIN Warehouse V ON V.WarehouseID = vw.WarehouseID 
                    left join CurrencyLanguage cl on t.SelectedCurrencyID=cl.Currencyid and cl.languageid=1
                    WHERE T.Synchronized = 0 --AND dbo.IsRouteHistoryUploaded(T.RouteHistoryID) = 0 
					  AND T.OrderDate >= '{0}' AND T.OrderDate < '{1}' 
                  {2}
                    AND T.OrderStatusID =2 /*and T.TransactionID = 'INV-VC11-0071-000040'*/"
                    , Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
                incubeQuery = new InCubeQuery(db_vms, invoicesHeader);

                //Logger.WriteLog(FromDate.ToString(), ToDate.ToString(), invoicesHeader, LoggingType.Information, LoggingFiles.errorInv);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Order header query failed !!"));
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
                        Currency = "";
                        TransactionID = dtInvoices.Rows[i]["OrderID"].ToString();
                        ReportProgress("Sending order: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(11, TransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Orders_S, new List<string>(filters.Values), processID, 60);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            res = Result.Duplicate;
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [Salesorder] SET Synchronized = 1 WHERE OrderID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                            throw (new Exception("Order already sent  check table  Int_ExecutionDetails!!"));
                        }



                        TransactionDate = Convert.ToDateTime(dtInvoices.Rows[i]["OrderDate"]);
                        //  SalesDocType = dtInvoices.Rows[i]["TransactionTypeID"].ToString();
                        WarehouseCode = dtInvoices.Rows[i]["WarehouseCode"].ToString();
                        string Salesperson = dtInvoices.Rows[i]["Employeecode"].ToString();
                        Currency = dtInvoices.Rows[i]["Currency"].ToString();
                        CustomerCode = dtInvoices.Rows[i]["CustomerCode"].ToString();
                        netTotal = decimal.Parse(dtInvoices.Rows[i]["NetTotal"].ToString()).ToString("F4");
                        BlockNumber = dtInvoices.Rows[i]["BlockNumber"].ToString();
                        note = dtInvoices.Rows[i]["note"].ToString();

                        //if (SalesDocType.ToString() == "1" || SalesDocType.ToString() == "3")
                        //{

                        SalesDocType = "salesorder";
                        //}
                        //else {
                        //    SalesDocType = "salesreturn";
                        //    WarehouseCode = "0002";
                        //}

                        string invoiceDetails = string.Format(@"SELECT     
SalesOrderDetail.OrderID,
sum(SalesOrderDetail.Quantity) Quantity,
SalesOrderDetail.Price, 
SalesOrderDetail.Price*sum(SalesOrderDetail.Discount)  Discount, 
SalesOrderDetail.Price* (SalesOrderDetail.Tax/100)  Tax, 
Item.ItemCode,  
PackTypeLanguage.Description AS PackName,    
SalesOrderDetail.Discount  DiscPer, 
SalesOrderDetail.Tax TaxPer
,SalesOrderDetail.salestransactiontypeid ItemType
FROM SalesOrderDetail  INNER JOIN
Pack ON SalesOrderDetail.PackID = Pack.PackID INNER JOIN
Item ON Pack.ItemID = Item.ItemID INNER JOIN 
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID
WHERE (PackTypeLanguage.LanguageID = 1) 
 AND (SalesOrderDetail.OrderID like '{0}')
group by 
SalesOrderDetail.OrderID,
SalesOrderDetail.Price, 
SalesOrderDetail.Discount , 
SalesOrderDetail.Tax , 
Item.ItemCode, 
PackTypeLanguage.Description, 
 SalesOrderDetail.salestransactiontypeid,
SalesOrderDetail.Price 
,SalesOrderDetail.CustomerID,SalesOrderDetail.OutletID
ORDER BY Item.ItemCode
", TransactionID);
                        incubeQuery = new InCubeQuery(db_vms, invoiceDetails);
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            throw (new Exception("Order details query failed !!"));
                        }

                        DataTable dtDetails = incubeQuery.GetDataTable();
                        string allDetails = "";
                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        {
                            string ItemCode = "", UOM = "", Quantity = "", Price = "", Tax = "", discount = "", Type = "", bonus = "0";

                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            UOM = dtDetails.Rows[j]["PackName"].ToString();
                            Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString("F4");//.ToString("#0.000");
                            Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString("F4");//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                            Tax = decimal.Parse(dtDetails.Rows[j]["Tax"].ToString()).ToString("F4");
                            discount = decimal.Parse(dtDetails.Rows[j]["discount"].ToString()).ToString("F4");
                            Type = dtDetails.Rows[j]["ItemType"].ToString();
                            Price = (decimal.Parse(Price.ToString()) + decimal.Parse(Tax.ToString())).ToString("F4");
                            if (Type == "4" || Type == "2")
                            {
                                bonus = Quantity;
                                Quantity = "0";
                            }
                            if (decimal.Parse(discount) > 0)
                            {
                                discount = (decimal.Parse(discount.ToString()) / decimal.Parse(Price.ToString())).ToString("F4");
                            }

                            allDetails += "{" + string.Format(detailsTemp, ItemCode, UOM, Price, Quantity, bonus, discount) + "},";


                        }
                        allDetails = "[" + allDetails.Substring(0, allDetails.Length - 1) + "]";
                        string headerData = string.Format(headerTemp, TransactionID.Replace("OINV-", "1").Replace("OID-", "1").Replace("INV-", "1").Replace("RTN-", "2").Replace("-", ""), "", CustomerCode, Salesperson, DateTime.Parse(TransactionDate.ToString()).ToString("dd/MM/yyyy"), SalesDocType, WarehouseCode,
                           allDetails, "0", "0", netTotal, TransactionID+" Note("+ note + ")", Currency);//, "\"\"");
                        string body = "{\"user\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_UserName + "\",\"password\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_Password + "\",\"command\":\"save\",\"record\":{" + headerData + "}}";
                        result[] result1 = Tools.GetRequest<result>(CoreGeneral.Common.GeneralConfigurations.WS_URL, body, "", CoreGeneral.Common.GeneralConfigurations.SSL_File, CoreGeneral.Common.GeneralConfigurations.SSL_Key);
                        if (result1 == null || (result1[0].error != null && result1[0].error.Trim() != ""))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (result1[0].error != null ? result1[0].error : "") + "\r\n json:" + body);
                            WriteMessage("Error .. \r\n" + result1[0].error);

                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + result1[0].rows.code + "\r\n Message:" + (result1[0].error != null ? result1[0].error : "") + "\r\n json:" + body);
                            WriteMessage("Success, ERP No: " + result1[0].rows.code);
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [SalesOrder] SET Synchronized = 1,OrderStatusID=6,Description='" + result1[0].rows.code + "' WHERE OrderID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
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
                WriteMessage("Fetching order failed !!");
            }

        }
        public override void SendInvoices( )
        {
            try
            {
                string salespersonFilter = "", SalesDocType = "", netTotal = "";
                string CustomerCode = "", BlockNumber = "", TransactionID = "", WarehouseCode = "", Currency="";
                DateTime TransactionDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                 Result res = Result.UnKnown;

                string headerTemp = "\"table\":\"{5}\",\"__TRANSACTION_ID\":\"{0}\",\"__DEVICE_ID\":\"{1}\",\"contact\":\"{2}\",\"salesman\":\"{3}\"," +
"\"docDate\":\"{4}\",\"currency\":\"{12}\",\"docFormat\":\"Include Tax\",\"warehouse\":\"{6}\"," +
"\"orderDetail\":{7},\"discountPercent\":\"{8}\",\"discountTotal\":\"{9}\",\"totalNet\":\"{10}\"," +
"\"comment\":\"InCube {11}\",\"manualNum\":\"{13}\"";//,\"receipt\":{12}";


                string detailsTemp = "\"item\":\"{0}\",\"unit\":\"{1}\",\"price\":\"{2}\",\"quantity\":\"{3}\",\"bonus\":\"{4}\",\"discountPercent\":\"{5}\" ";
              //  string receiptTemp = "\"account\":\"{0}\",\"currency\":\"{1}\",\"subAct\":\"{3}\",\"reference\":\"{4}\",\"dbValue\":\"{5}\",\"dbAmount\":\"{6}\",\"dueDate\":\"{7}\",\"checkNumber\":\"{8}\",\"bank\":\"{9}\",\"accountNumber\":\"{10}\",\"thirdParty\":\"{11}\"";//{11} =FALSE or TRUE

                if (Filters.EmployeeID!=-1)
                {
                    salespersonFilter = "AND T.EmployeeID = " + Filters.EmployeeID;
                }
                string invoicesHeader = string.Format(@"SELECT   T.TransactionID,   T.TransactionDate ,   T.TransactionTypeID
                    ,  V.WarehouseCode,e.Employeecode , CO.CustomerCode , t.Discount,t.NetTotal,CO.BlockNumber,cl.description Currency
                    FROM [Transaction] T 
                    INNER JOIN CustomerOutlet CO ON CO.CustomerID = T.CustomerID AND CO.OutletID = T.OutletID
                    INNER JOIN Customer  C  ON C .CustomerID = T.CustomerID --AND CO.OutletID = T.OutletID
                    INNER JOIN Organization O ON O.OrganizationID = T.OrganizationID
                    INNER JOIN Employee E ON E.EmployeeID = T.EmployeeID
                    inner join EmployeeVehicle ev on e.EmployeeID=ev.EmployeeID
					inner join VehicleLoadingWh vl on ev.VehicleID=vl.VehicleID
                    LEFT JOIN Warehouse V ON V.WarehouseID = vl.WarehouseID
inner join CurrencyLanguage cl on t.Currencyid=cl.Currencyid and cl.languageid=1
                    WHERE T.Synchronized = 0 AND dbo.[IsRouteHistoryUploaded](T.RouteHistoryID) = 0 AND c.new<>1 AND T.Voided = 0 AND T.TransactionDate >= '{0}' AND T.TransactionDate < '{1}' 
                    {2}
                    AND T.TransactionTypeID < 5 /*and T.TransactionID = 'INV-VC11-0071-000040'*/"
                    , Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
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
                      //  SAP_SO_NUM = "";
                        TransactionID = dtInvoices.Rows[i]["TransactionID"].ToString();
                        ReportProgress("Sending invoice: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, TransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Sales_S, new List<string>(filters.Values), processID, 60);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            res = Result.Duplicate;
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [Transaction] SET Synchronized = 1 WHERE TransactionID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                            throw (new Exception("Transaction already sent  check table  Int_ExecutionDetails!!"));
                        }



                        TransactionDate = Convert.ToDateTime(dtInvoices.Rows[i]["TransactionDate"]);
                        SalesDocType = dtInvoices.Rows[i]["TransactionTypeID"].ToString();
                        WarehouseCode = dtInvoices.Rows[i]["WarehouseCode"].ToString();
                     string   Salesperson = dtInvoices.Rows[i]["Employeecode"].ToString();
                        //SalesGroup = dtInvoices.Rows[i]["SalesGroup"].ToString();
                        CustomerCode = dtInvoices.Rows[i]["CustomerCode"].ToString();
                        netTotal = decimal.Parse( dtInvoices.Rows[i]["NetTotal"].ToString()).ToString("F4");
                        BlockNumber = dtInvoices.Rows[i]["BlockNumber"].ToString();
                        Currency= dtInvoices.Rows[i]["Currency"].ToString();

                        if (SalesDocType.ToString() == "1" || SalesDocType.ToString() == "3")
                        {
                            
            SalesDocType = "manualinvoice";
                        }
                        else {
                            SalesDocType = "salesreturn";
                            WarehouseCode = "0002";
                        }

                        string invoiceDetails = string.Format(@"select 
TransactionID,	Quantity,	round(Price+(Price*TaxPer/100),2) price,	round(BasePrice+(BasePrice*TaxPer/100),2) BasePrice	,Discount	,Tax	,ItemCode	,PackName	,DiscPer	,TaxPer,	ItemType  from (
SELECT     
TransactionDetail.TransactionID,
sum(TransactionDetail.Quantity) Quantity,
TransactionDetail.Price, TransactionDetail.BasePrice, 
sum(TransactionDetail.Discount)/sum(TransactionDetail.Quantity) Discount, 
sum(TransactionDetail.Tax)/sum(TransactionDetail.Quantity) Tax, 
Item.ItemCode,  
PackTypeLanguage.Description AS PackName,    
round(((sum(TransactionDetail.Discount)/sum(TransactionDetail.Quantity))/CASE TransactionDetail.Price when 0 then 1 else TransactionDetail.Price end)*100,2)DiscPer, 
round(((sum(TransactionDetail.Tax)/sum(TransactionDetail.Quantity))/CASE TransactionDetail.Price when 0 then 1 else TransactionDetail.Price-(sum(TransactionDetail.Discount)/sum(TransactionDetail.Quantity)) end)*100,1) TaxPer
,TransactionDetail.salestransactiontypeid ItemType
FROM TransactionDetail INNER JOIN
Pack ON TransactionDetail.PackID = Pack.PackID INNER JOIN
Item ON Pack.ItemID = Item.ItemID INNER JOIN 
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID
WHERE (PackTypeLanguage.LanguageID = 1)and TransactionDetail.SalesTransactionTypeID in(1,2,4,5)
 AND (TransactionDetail.TransactionID like '{0}')
group by 
TransactionDetail.TransactionID,
TransactionDetail.Price, 
TransactionDetail.Discount/TransactionDetail.Quantity, 
TransactionDetail.Tax/TransactionDetail.Quantity, 
Item.ItemCode, 
PackTypeLanguage.Description, 
 TransactionDetail.salestransactiontypeid,
TransactionDetail.Price 
,TransactionDetail.CustomerID,TransactionDetail.BasePrice, TransactionDetail.OutletID

)t
ORDER BY ItemCode 
", TransactionID);
                        incubeQuery = new InCubeQuery(db_vms, invoiceDetails);
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            throw (new Exception("Invoices details query failed !!"));
                        }

                        DataTable dtDetails = incubeQuery.GetDataTable();
                        string allDetails = "";
                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        {
                            string ItemCode = "", UOM = "", Quantity = "", BasePrice="",  Price = "", Tax = "", discountPer = "", Type = "", bonus = "0";

                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            UOM = dtDetails.Rows[j]["PackName"].ToString();
                            Quantity =decimal.Parse( dtDetails.Rows[j]["Quantity"].ToString()).ToString("F4");//.ToString("#0.000");
                            Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString("F4") ;//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                            Tax = decimal.Parse(dtDetails.Rows[j]["Tax"].ToString()).ToString("F4") ;
                            discountPer =  decimal.Parse(dtDetails.Rows[j]["DiscPer"].ToString()).ToString("F4");
                            Type = dtDetails.Rows[j]["ItemType"].ToString();
                            BasePrice= decimal.Parse(dtDetails.Rows[j]["BasePrice"].ToString()).ToString("F4");//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                            if (Type == "4"|| Type == "2")
                            {
                                bonus = Quantity;
                                Quantity = "0";
                                Price = BasePrice;
                             }
                            //if (decimal.Parse(discount) > 0)
                            //{
                            //    discount = (decimal.Parse(discount.ToString()) / decimal.Parse(Price.ToString())).ToString("F4");
                            //}
                          //  Price = (decimal.Parse(Price.ToString()) + decimal.Parse(Tax.ToString())).ToString("F4");

                            allDetails += "{" + string.Format(detailsTemp, ItemCode, UOM, Price, Quantity, bonus, discountPer) + "},";


                        }
                        allDetails = "[" + allDetails.Substring(0, allDetails.Length - 1) + "]";
                        string headerData = string.Format(headerTemp, TransactionID.Replace("OINV-", "1").Replace("INV-", "1").Replace("RTN-", "2").Replace("-", ""), "", CustomerCode, Salesperson, DateTime.Parse(TransactionDate.ToString()).ToString("dd/MM/yyyy"), SalesDocType, WarehouseCode,
                           allDetails, "0", "0", netTotal, TransactionID,Currency,TransactionID);//, "\"\"");
                        string body = "{\"user\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_UserName + "\",\"password\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_Password + "\",\"command\":\"save\",\"record\":{" + headerData + "}}";
                        result[] result1 = Tools.GetRequest<result>(CoreGeneral.Common.GeneralConfigurations.WS_URL, body, "", CoreGeneral.Common.GeneralConfigurations.SSL_File, CoreGeneral.Common.GeneralConfigurations.SSL_Key);
                        if (result1 == null || (result1[0].error != null && result1[0].error.Trim() != ""))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (result1[0].error != null ? result1[0].error : "") + "\r\n json:" + body);
                            WriteMessage("Error .. \r\n" + result1[0].error);
                  
                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + result1[0].rows.code + "\r\n Message:" + (result1[0].error != null ? result1[0].error : "") + "\r\n json:" + body);
                            WriteMessage("Success, ERP No: " + result1[0].rows.code);
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [Transaction] SET Synchronized = 1 WHERE TransactionID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
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
                WriteMessage("Fetching invoices failed !!");
            }

        }
    }
    class PalCustomers :Customers
    {

        [JsonProperty(PropertyName = "cusCurrency.symbol")]
        public override string cusCurrencyName { get; set; }



        [JsonProperty(PropertyName = "type.code")]
        public  string classCode { get; set; }


        [JsonProperty(PropertyName = "type.name")]
        public  string className { get; set; }


        [JsonProperty(PropertyName = "memberCard.code")]
        public  string groupCode { get; set; }


        [JsonProperty(PropertyName = "memberCard.name")]
        public  string groupName { get; set; }




       


    }
    class PalItem : Item
    {

        [JsonProperty(PropertyName = "defaultUnit.code")]
        public string DfltUOM { get; set; }
    }
   
    class PalPriceList : PriceList
    {

        
        [JsonProperty(PropertyName = "currency.Symbol")]
         public override string currency { get; set; }
    }
    class CustBalance
    {
        public string reference { get; set; }
        
        [JsonProperty(PropertyName = "endBalanceVal")]
        public decimal endBalance { get; set; }

    }
}