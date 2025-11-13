using SAP.Middleware.Connector;
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
    public class IntegrationAnabtawi : IntegrationBase // Live branch
    {
        BackgroundWorker bgwCheckProgress;
        InCubeDatabase db_res;
        int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
        SqlCommand cmd;
        InCubeQuery incubeQuery = null;
        string StagingTable = "";
        Dictionary<int, RfcConfigParameters> RfcParams = new Dictionary<int, RfcConfigParameters>();
        string _WarehouseID = "-1";
        public IntegrationAnabtawi(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            if (CoreGeneral.Common.CurrentSession.loginType != LoginType.WindowsService)
            {
                db_res = new InCubeDatabase();
                db_res.Open("InCube", "IntegrationAnabtawi");
            }



            bgwCheckProgress = new BackgroundWorker();
            bgwCheckProgress.DoWork += new DoWorkEventHandler(bgw_CheckProgress);
            bgwCheckProgress.WorkerSupportsCancellation = true;

            RfcConfigParameters RfcPar = new RfcConfigParameters();
            foreach (int org in CoreGeneral.Common.OrganizationConfigurations.Keys)
            {
                RfcParams.Add(org, new RfcConfigParameters());
                RfcParams[org][RfcConfigParameters.Name] = CoreGeneral.Common.OrganizationConfigurations[org].Name;
                RfcParams[org][RfcConfigParameters.User] = CoreGeneral.Common.OrganizationConfigurations[org].User;
                RfcParams[org][RfcConfigParameters.Password] = CoreGeneral.Common.OrganizationConfigurations[org].Password;
                RfcParams[org][RfcConfigParameters.Client] = CoreGeneral.Common.OrganizationConfigurations[org].Client;
                RfcParams[org][RfcConfigParameters.Language] = CoreGeneral.Common.OrganizationConfigurations[org].Language;
                RfcParams[org][RfcConfigParameters.AppServerHost] = CoreGeneral.Common.OrganizationConfigurations[org].AppServerHost;
                RfcParams[org][RfcConfigParameters.SystemNumber] = CoreGeneral.Common.OrganizationConfigurations[org].SystemNumber;
                RfcParams[org][RfcConfigParameters.SystemID] = CoreGeneral.Common.OrganizationConfigurations[org].SystemID;
            }
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
            // if (UpdateAll) _WarehouseID = "-1"; else _WarehouseID = WarehouseID;
            GetMasterData(IntegrationField.Stock_U);
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
            {// { \"field\" : \"ISNULL(isAssetItem)\", \"operation\" : \"=\",\"value\" : 0 }
                DT = new DataTable();
                string body = "{\"user\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_UserName + "\",\"password\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_Password + "\",\"command\":\"table\",\"table\":\"Item\",\"fields\":[\"code\",\"name\",\"used\",\"itemFamily.code\",\"unitList.unit\",\"unitList.partNumber\" ,\"unitList.packVolume\" ,\"itemFamily.nameAR\",\"itemCategory.code\",\"itemCategory.name\",\"brand.name\",\"enabled\",\"isAssetItem\"] " +
                        ",\"filters\" : [  { \"field\" : \"type\", \"operation\" : \"!=\",\"value\" : \"Service\" }] " +
                    "}";

                DT = Tools.GetRequestTable<Item>(CoreGeneral.Common.GeneralConfigurations.WS_URL, body, "rows", CoreGeneral.Common.GeneralConfigurations.SSL_File, CoreGeneral.Common.GeneralConfigurations.SSL_Key);


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
                string body = "{\"user\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_UserName + "\",\"password\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_Password + "\",\"command\":\"table\",\"table\":\"contact\",\"fields\":[\"code\",\"name\",\"phone\",\"creditDays\",\"cusCurrency.nameAR\",\"maxCredit\",\"area.nameAR\",\"area\",\"treeParent\",\"branch\",\"chkMaxCredit\",\"alert\",\"area\",\"treeParent\",\"branch\",\"chkMaxCredit\",\"children\",\"city\",\"code\",\"type\",\"creditDays\",\"creditDaysFromEndOfMonth\",\"cusCurrency\",\"currentBalance\",\"isCustomer\",\"cusAccount\",\"cusContact\",\"customerExpiry\",\"cusPriceList\",\"country.name\",\"city\",\"dealerService\",\"dealerType\",\"dependants\",\"discountPercent\",\"education\",\"email\",\"isEmployee\",\"empAccount\",\"empCurrency\",\"enabled\",\"fax\",\"gender\",\"headerFld\",\"homePhone\",\"idCard\",\"jobTitle\",\"lastChanged\",\"treeLevel\",\"maritalStatus\",\"maxCredit\",\"memberCard\",\"mobile\",\"name\",\"notes\",\"payToDriver\",\"personalCheck\",\"phone\",\"printContactBalance\",\"printPrices\",\"salesman\",\"sector\",\"street\",\"students\",\"isSupplier\",\"supAccount\",\"supContact\",\"supCurrency\",\"supPriceList\",\"supTaxFormat\",\"taxId\",\"cusTaxType\",\"used\",\"sector\"] ," +
                                "\"filters\" : [ { \"field\" : \"isCustomer\"," +
                    " \"operation\" : \"=\"," +
                    "\"value\" : \"Yes\" } ]}";
                DT = Tools.GetRequestTable<Customers>(CoreGeneral.Common.GeneralConfigurations.WS_URL, body, "rows", CoreGeneral.Common.GeneralConfigurations.SSL_File, CoreGeneral.Common.GeneralConfigurations.SSL_Key);
                //@"https://gw.bisan.com/api/odemo_2"
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
            }
            return res;
        }


        private Result GetStockTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            try
            {
                DT = new DataTable();
                DT.Columns.Add("WarehouseCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemCode", System.Type.GetType("System.String"));
                DT.Columns.Add("UOM", System.Type.GetType("System.String"));
                DT.Columns.Add("Lot", System.Type.GetType("System.String"));
                DT.Columns.Add("Qty", System.Type.GetType("System.String"));
                DT.Columns.Add("ExpiryDate", System.Type.GetType("System.String"));
                DT.Columns.Add("PackID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("WarehouseID", System.Type.GetType("System.Int16"));


                //    DataRow _row = DT.NewRow();
                //    _row["WarehouseCode"] = "JO10";
                //    _row["ItemCode"] = "000000000003100614";//000000000003100616
                //_row["UOM"] = "KG";
                //_row["Qty"] = "10";
                //_row["Lot"] = "180320";
                //    _row["ExpiryDate"] = "2018-03-20";
                //    DT.Rows.Add(_row);
                //    DT.AcceptChanges();
                //DataRow _row2 = DT.NewRow();
                //_row2["WarehouseCode"] = "JO10";
                //_row2["ItemCode"] = "000000000003100616";//
                //_row["Qty"] = "20";
                //_row2["UOM"] = "KG";
                //_row2["Lot"] = "180320";
                //_row2["ExpiryDate"] = "2018-03-20";
                //DT.Rows.Add(_row2);
                //DT.AcceptChanges();

                foreach (KeyValuePair<int, RfcConfigParameters> pair in RfcParams)
                    try
                    {
                        if (pair.Key != -1)
                        {
                            RfcConfigParameters RfcParam = pair.Value;
                            RfcDestination prd = RfcDestinationManager.GetDestination(RfcParam);
                            RfcRepository repo = prd.Repository;
                            IRfcFunction companyBapi = repo.CreateFunction("BAPI_VS_STOCK");

                            IRfcTable tblImport = companyBapi.GetTable("PE_STOCK");
                            companyBapi.SetValue("PE_STOCK", tblImport);
                            companyBapi.Invoke(prd);
                            IRfcTable detail = companyBapi.GetTable("PE_STOCK");
                            foreach (IRfcStructure row in detail)
                            {
                                DataRow _row = DT.NewRow();
                                _row["WarehouseCode"] = row.GetValue("WERKS").ToString() + row.GetValue("LGORT").ToString();
                                _row["ItemCode"] = row.GetValue("MATNR").ToString();
                                _row["UOM"] = row.GetValue("MEINS").ToString();
                                _row["Lot"] = row.GetValue("CHARG").ToString();
                                _row["Qty"] = row.GetValue("LABST").ToString();
                                _row["ExpiryDate"] = row.GetValue("VFDAT").ToString();
                                DT.Rows.Add(_row);
                                DT.AcceptChanges();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                    }

                if (DT.Rows.Count > 0)
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
                string body = "{\"user\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_UserName + "\",\"password\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_Password + "\",\"command\":\"table\",\"table\":\"ItemPrice\",\"fields\":[\"priceList\",\"item\",\"unit\",\"currency.nameAR\",\"rawPrice\",\"taxedPrice\" ] " +
                    //    ",\"filters\" : [ { \"field\" : \"isCustomer\"," +
                    //  " \"operation\" : \"=\"," +
                    //   "\"value\" : \"Yes\" }] " +
                    "}";

                DT = Tools.GetRequestTable<PriceList>(CoreGeneral.Common.GeneralConfigurations.WS_URL, body, "rows", CoreGeneral.Common.GeneralConfigurations.SSL_File, CoreGeneral.Common.GeneralConfigurations.SSL_Key);


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
        public override void SendReciepts()
        {
            try
            {
                string salespersonFilter = "", cheeqNo = "", amount = "", amount2 = "", Curnacy2 = "";
                string CustomerCode = "", BlockNumber = "", Bank = "", Branch = "", CusAccount = "", SupAccount = "", TransactionID = "", WarehouseCode = "", Curnacy1 = "";
                DateTime TransactionDate;
                DateTime cheeqDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                Dictionary<int, RfcDestination> destinations = new Dictionary<int, RfcDestination>();
                Dictionary<int, RfcRepository> repositories = new Dictionary<int, RfcRepository>();
                Result res = Result.UnKnown;

                string headerTemp = "\"table\":\"mreceiptvoucher\",\"__TRANSACTION_ID\":\"{0}\",\"__DEVICE_ID\":\"{1}\",\"contact\":\"{2}\",\"salesman\":\"{3}\"," +
"\"docDate\":\"{4}\",\"branch\":\"{5}\"," +
"\"receipt\":{6} ," +
"\"comment\":\"{7}\",\"manualNum\":\"{8}\"";


                string receiptTemp = "\"account\":\"{0}\",\"currency\":\"{1}\",\"dbAmount\":\"{2}\"{3}";
                string cheeqTemp = ",\"dueDate\":\"{0}\",\"checkNumber\":\"{1}\",\"bank\":\"{2}\",\"thirdParty\":\"TRUE\"";//{11} =FALSE or TRUE

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND CP.EmployeeID = " + Filters.EmployeeID;
                }
                string invoicesHeader = string.Format(@"SELECT   CP.CustomerPaymentID,e.EmployeeCode, CP.PaymentDate,  sum(AppliedAmount)AppliedAmount,sum(SecondCurrencyAmount)SecondCurrencyAmount,cp.ExchangeRate,c.Description Currency, c2.Description Currency2,o.CustomerCode,cp.VoucherDate,cp.VoucherNumber,b.Code bank, Bl.description branch
                                      ,t.notes   ,O.RoadNumber CusAccount, O.ShopNumber SupAccount ,O.BlockNumber     FROM CustomerPayment CP left join [transaction] t on cp.TransactionID=t.TransactionID
											left join CustomerOutlet  o on o.CustomerID=cp.CustomerID and  o.OutletID=cp.OutletID  
												left join CurrencyLanguage c on cp.CurrencyID=c.CurrencyID and c.LanguageID=1
												left join CurrencyLanguage c2 on cp.SecondCurrencyID=c2.CurrencyID and c2.LanguageID=1
                                                 left join Employee e on cp.EmployeeID=e.EmployeeID
                                              Left Join Bank B on CP.BankID=B.BankID  
                                                Left Join BankBranchLanguage Bl on CP.BankID=Bl.BankID and CP.BranchID=Bl.BranchID and Bl.LanguageID=1
                                                WHERE       CP.PaymentStatusID <> 5 AND CP.PaymentTypeID IN (1,2,3) 
                        {6}
                       AND CP.PaymentDate >= CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102) 
                                                 AND CP.PaymentDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102)
group by   CP.CustomerPaymentID,e.EmployeeCode,CP.PaymentDate, cp.ExchangeRate,c.Description ,O.BlockNumber, c2.Description ,o.CustomerCode,cp.VoucherDate,cp.VoucherNumber,b.Code, Bl.description,t.notes,o.RoadNumber , O.ShopNumber   
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
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerPayment] SET Synchronized = 1 WHERE CustomerPaymentID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                            throw (new Exception("CustomerPayment already sent  check table  Int_ExecutionDetails!!"));
                        }



                        TransactionDate = Convert.ToDateTime(dtInvoices.Rows[i]["PaymentDate"]);
                        string Salesperson = dtInvoices.Rows[i]["Employeecode"].ToString();
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
                        CusAccount = "1101";
                        // string receiptTemp = "{\"account\":\"{0}\",\"currency\":\"{1}\",\"subAct\":\"{3}\",\"dbAmount\":\"{4}\" {7}}";
                        string cheeqInfo = "";
                        if (cheeqNo.Trim() != "")
                        {
                            CusAccount = "1200";

                            cheeqDate = Convert.ToDateTime(dtInvoices.Rows[i]["VoucherDate"].ToString());
                            cheeqInfo = string.Format(cheeqTemp, cheeqDate.ToString(), cheeqNo, Bank + ":" + Branch);
                        }
                        string allDetails = "";
                        if (Curnacy2.Trim() == "")
                            allDetails = "[{" + string.Format(receiptTemp, CusAccount, Curnacy1, amount, cheeqInfo) + "}]";
                        else
                            allDetails = "[{" + string.Format(receiptTemp, CusAccount, Curnacy2, amount2, cheeqInfo) + "}]";

                        //      allDetails = "[" + allDetails.Substring(0, allDetails.Length - 1) + "]";
                        string headerData = string.Format(headerTemp, TransactionID.Replace("PAY-", "2").Replace("-", ""), "", CustomerCode, Salesperson, DateTime.Parse(TransactionDate.ToString()).ToString("dd/MM/yyyy"), BlockNumber,
                           allDetails, "", TransactionID.Replace("PAY-", "2").Replace("-", ""));//, "\"\"");

                        string body = "{\"user\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_UserName + "\",\"password\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_Password + "\",\"command\":\"save\",\"record\":{" + headerData + "}}";
                        result[] result1 = Tools.GetRequest<result>(CoreGeneral.Common.GeneralConfigurations.WS_URL, body, "", CoreGeneral.Common.GeneralConfigurations.SSL_File, CoreGeneral.Common.GeneralConfigurations.SSL_Key);
                        if (result1 == null || (result1[0].error != null && result1[0].error.Trim() != ""))
                        {
                            res = Result.NoFileRetreived;
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
        public override void SendInvoices()
        {
            try
            {
                string salespersonFilter = "", SalesDocType = "", netTotal = "", VehicleCode = "", SalesOffice = "", SalesGroup = "";
                string CustomerCode = "", BlockNumber = "", TransactionID = "", WarehouseCode = "", SAP_SO_NUM = "";
                DateTime TransactionDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                Dictionary<int, RfcDestination> destinations = new Dictionary<int, RfcDestination>();
                Dictionary<int, RfcRepository> repositories = new Dictionary<int, RfcRepository>();
                Result res = Result.UnKnown;

                string headerTemp = "\"table\":\"invoice\",\"__TRANSACTION_ID\":\"{0}\",\"__DEVICE_ID\":\"{1}\",\"contact\":\"{2}\",\"salesman\":\"{3}\"," +
"\"docDate\":\"{4}\",\"branch\":\"{5}\",\"currency\":\"01\",\"docFormat\":\"Include Tax\",\"warehouse\":\"{6}\"," +
"\"orderDetail\":{7},\"discountPercent\":\"{8}\",\"discountTotal\":\"{9}\",\"totalNet\":\"{10}\"," +
"\"comment\":\"{11}\"";//,\"receipt\":{12}";


                string detailsTemp = "\"item\":\"{0}\",\"unit\":\"{1}\",\"price\":\"{2}\",\"quantity\":\"{3}\",\"bonus\":\"{4}\",\"discountPercent\":\"{5}\" ";
                string receiptTemp = "\"account\":\"{0}\",\"currency\":\"{1}\",\"subAct\":\"{3}\",\"reference\":\"{4}\",\"dbValue\":\"{5}\",\"dbAmount\":\"{6}\",\"dueDate\":\"{7}\",\"checkNumber\":\"{8}\",\"bank\":\"{9}\",\"accountNumber\":\"{10}\",\"thirdParty\":\"{11}\"";//{11} =FALSE or TRUE

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND T.EmployeeID = " + Filters.EmployeeID;
                }
                string invoicesHeader = string.Format(@"SELECT   T.TransactionID,   T.TransactionDate ,   T.TransactionTypeID
                    ,  V.WarehouseCode,e.Employeecode , CO.CustomerCode , t.Discount,t.NetTotal,CO.BlockNumber
                    FROM [Transaction] T 
                    INNER JOIN CustomerOutlet CO ON CO.CustomerID = T.CustomerID AND CO.OutletID = T.OutletID
                    INNER JOIN Customer  C  ON C .CustomerID = T.CustomerID --AND CO.OutletID = T.OutletID
                    INNER JOIN Organization O ON O.OrganizationID = T.OrganizationID
                    INNER JOIN Employee E ON E.EmployeeID = T.EmployeeID
                    LEFT JOIN Warehouse V ON V.WarehouseID = T.WarehouseID 
                    WHERE T.Synchronized = 0 AND dbo.IsRouteHistoryUploaded(T.RouteHistoryID) = 0 AND T.Voided = 0 AND T.TransactionDate >= '{0}' AND T.TransactionDate < '{1}' 
                    {2}
                    AND T.TransactionTypeID < 5 /*and T.TransactionID = 'INV-VC11-0071-000040'*/"
                    , Filters.FromDate.ToString("yyyy-MM-dd"), Filters.FromDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
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
                        SAP_SO_NUM = "";
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
                        string Salesperson = dtInvoices.Rows[i]["Employeecode"].ToString();
                        //SalesGroup = dtInvoices.Rows[i]["SalesGroup"].ToString();
                        CustomerCode = dtInvoices.Rows[i]["CustomerCode"].ToString();
                        netTotal = dtInvoices.Rows[i]["NetTotal"].ToString();
                        BlockNumber = dtInvoices.Rows[i]["BlockNumber"].ToString();




                        string invoiceDetails = string.Format(@"SELECT     
TransactionDetail.TransactionID,
sum(TransactionDetail.Quantity) Quantity,
TransactionDetail.Price, 
sum(TransactionDetail.Discount)/sum(TransactionDetail.Quantity) Discount, 
sum(TransactionDetail.Tax)/sum(TransactionDetail.Quantity) Tax, 
Item.ItemCode,  
PackTypeLanguage.Description AS PackName,    
((sum(TransactionDetail.Discount)/sum(TransactionDetail.Quantity))/CASE TransactionDetail.Price when 0 then 1 else TransactionDetail.Price end)*100 DiscPer, 
((sum(TransactionDetail.Tax)/sum(TransactionDetail.Quantity))/CASE TransactionDetail.Price when 0 then 1 else TransactionDetail.Price-(sum(TransactionDetail.Discount)/sum(TransactionDetail.Quantity)) end)*100 TaxPer
,TransactionDetail.salestransactiontypeid ItemType
FROM TransactionDetail INNER JOIN
Pack ON TransactionDetail.PackID = Pack.PackID INNER JOIN
Item ON Pack.ItemID = Item.ItemID INNER JOIN 
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID
WHERE (PackTypeLanguage.LanguageID = 1)and TransactionDetail.SalesTransactionTypeID in(1,4,5)
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
,TransactionDetail.CustomerID,TransactionDetail.OutletID
ORDER BY Item.ItemCode
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
                            string ItemCode = "", UOM = "", Quantity = "", Price = "", Tax = "", discount = "", Type = "", bonus = "0";

                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            UOM = dtDetails.Rows[j]["PackName"].ToString();
                            Quantity = dtDetails.Rows[j]["Quantity"].ToString();//.ToString("#0.000");
                            Price = dtDetails.Rows[j]["Price"].ToString();//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                            Tax = dtDetails.Rows[j]["Tax"].ToString();
                            discount = dtDetails.Rows[j]["discount"].ToString();
                            Type = dtDetails.Rows[j]["ItemType"].ToString();
                            Price = (decimal.Parse(Price.ToString()) + decimal.Parse(Tax.ToString())).ToString();
                            if (Type == "4")
                            {
                                bonus = Quantity;
                                Quantity = "0";
                            }
                            if (decimal.Parse(discount) > 0)
                            {
                                discount = (decimal.Parse(discount.ToString()) / decimal.Parse(Price.ToString())).ToString();
                            }

                            allDetails += "{" + string.Format(detailsTemp, ItemCode, UOM, Price, Quantity, bonus, discount) + "},";


                        }
                        allDetails = "[" + allDetails.Substring(0, allDetails.Length - 1) + "]";
                        string headerData = string.Format(headerTemp, TransactionID.Replace("INV-", "1").Replace("-", ""), "", CustomerCode, Salesperson, DateTime.Parse(TransactionDate.ToString()).ToString("dd/MM/yyyy"), BlockNumber, WarehouseCode,
                           allDetails, "0", "0", netTotal, "");//, "\"\"");
                        string body = "{\"user\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_UserName + "\",\"password\":\"" + CoreGeneral.Common.GeneralConfigurations.WS_Password + "\",\"command\":\"save\",\"record\":{" + headerData + "}}";
                        result[] result1 = Tools.GetRequest<result>(CoreGeneral.Common.GeneralConfigurations.WS_URL, body, "", CoreGeneral.Common.GeneralConfigurations.SSL_File, CoreGeneral.Common.GeneralConfigurations.SSL_Key);
                        if (result1 == null || (result1[0].error != null && result1[0].error.Trim() != ""))
                        {
                            res = Result.NoFileRetreived;
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
    abstract class Customers
    {
        public string code { get; set; }
        public string cusContact { get; set; }
        public string cusCurrency { get; set; }
        public string enabled { get; set; }
        public string name { get; set; }
        public string cusPriceList { get; set; }
        public string Sector { get; set; }
        public string taxId { get; set; }
        public string currentBalance { get; set; }
        public string cusAccount { get; set; }
        public string supAccount { get; set; }
        public string treeParent { get; set; }
        public string cusTaxType { get; set; }
        public string area { get; set; }

        [JsonProperty(PropertyName = "area.nameAR")]
        public string areanameAR { get; set; }
        public string empAccount { get; set; }

        [JsonProperty(PropertyName = "cusCurrency.nameAR")]
        abstract public string cusCurrencyName { get; set; }
        public string phone { get; set; }
        public string mobile { get; set; }
        public string fax { get; set; }
        public string email { get; set; }

        [JsonProperty(PropertyName = "address.value")]
        public string address { get; set; }
        public string creditDays { get; set; }
        public string maxCredit { get; set; }

        [JsonProperty(PropertyName = "country.name")]
        public string country { get; set; }
        //  [JsonProperty(PropertyName = "country.name")]
        //    public string city { get; set; } 
        public string salesman { get; set; }
        public string branch { get; set; }

    }

    class Salesman
    {
        public string code { get; set; }
        [JsonProperty(PropertyName = "emp.name")]
        public string empname { get; set; }

    }
    abstract class PriceList
    {
        public string priceList { get; set; }
        public string item { get; set; }
        public string unit { get; set; }
        public string rawPrice { get; set; }
        public string taxedPrice { get; set; }
        [JsonProperty(PropertyName = "currency.nameAR")]
        abstract public string currency { get; set; }

    }
    class Warehouse
    {
        public string code { get; set; }
        public string name { get; set; }
        public string truck { get; set; }
        public string group{ get; set; }
        

    }
    class Item
    {
        public string code { get; set; }
        public string name { get; set; }
        public string nameAR { get; set; }

        public string serial { get; set; }



        string _used = "";
        public string used
        {
            get { return (_used.ToLower() == "yes" || _used == "1") ? "1" : "0"; }
            set { _used = value == null ? "0" : value; }
        }


        [JsonProperty(PropertyName = "itemFamily.code")]
        public string Division { get; set; }

        [JsonProperty(PropertyName = "itemFamily.nameAR")]
        public string DivisionName { get; set; }

        [JsonProperty(PropertyName = "itemCategory.code")]
        public string itemCategoryCode { get; set; }

        [JsonProperty(PropertyName = "itemCategory.name")]
        public string itemCategoryName { get; set; }
        [JsonProperty(PropertyName = "brand.name")]
        public string brand { get; set; }


        [JsonProperty(PropertyName = "classification.name")]

        public string classification { get; set; }
        public string group { get; set; }
        [JsonProperty(PropertyName = "group.name")]
        public string groupName { get; set; }

        [JsonProperty(PropertyName = "group.code")]
        public string groupCode { get; set; }
        public string enabled { get; set; }
        public string isAssetItem { get; set; }
        public unitList[] unitList { get; set; }

    }
    class unitList
    {
        [JsonProperty(PropertyName = "partNumber")]
        public string barcode { get; set; }
        [JsonProperty(PropertyName = "unit.factor")]
        public string Qty { get; set; }
        [JsonProperty(PropertyName = "unit")]
        public string UOM { get; set; }
    }
    class ItemStock
    {

        public string item { get; set; }
        public string warehouse { get; set; }
        public string reportUnit { get; set; }
        public string endBalance { get; set; }
        public string WarehouseID { get; set; }
        public string PackID { get; set; }


    }
    class ItemSerials
    {

        public string item { get; set; }
        public string warehouse { get; set; }
        public string serial    { get; set; }

        public string WarehouseID { get; set; }
        public string PackID { get; set; }


    }

    class result
    {
        public string command { get; set; }
        public string error { get; set; }
        public rows rows { get; set; }
    }
    class rows
    {
        public string __TRANSACTION_ID { get; set; }
        public string table { get; set; }
        public string code { get; set; }
    }
    //public class InvoiceDetails
    //{
    //    public string parent { get; set; }
    //    public string approval { get; set; }
    //    public string bonusPercent { get; set; }
    //    public string contact { get; set; }
    //    DateTime TransDate;
    //    public string docDate
    //    {
    //        get { return TransDate.ToString("yyyyMMdd"); }
    //        set
    //        {
    //            if (value != null)
    //            {
    //                TransDate = new DateTime(int.Parse(value.Split('/')[2]), int.Parse(value.Split('/')[1]), int.Parse(value.Split('/')[0]));
    //            }
    //            else
    //                TransDate = new DateTime(1990, 1, 1);
    //        }
    //    }
    //    public string item { get; set; }
    //    public string name { get; set; }
    //    public string netBonus { get; set; }
    //    public string netQuantity { get; set; }
    //    public string price { get; set; }
    //    public string reportUnit { get; set; }
    //    public string total { get; set; }


    //}



    public class InvoiceDetails
    {
        public string code { get; set; }
        public string currency { get; set; }
        public string discountAmount { get; set; }
        public string contact { get; set; }
        DateTime TransDate, _deliveryDate;
        public string docDate
        {
            get { return TransDate.ToString("yyyyMMdd"); }
            set
            {
                if (value != null)
                {
                    TransDate = new DateTime(int.Parse(value.Split('/')[2]), int.Parse(value.Split('/')[1]), int.Parse(value.Split('/')[0]));
                }
                else
                    TransDate = new DateTime(1990, 1, 1);
            }
        }
        public string deliveryDate
        {
            get { return _deliveryDate.ToString("yyyyMMdd"); }
            set
            {
                if (value != null)
                {
                    _deliveryDate = new DateTime(int.Parse(value.Split('/')[2]), int.Parse(value.Split('/')[1]), int.Parse(value.Split('/')[0]));
                }
                else
                    _deliveryDate = new DateTime(1990, 1, 1);
            }
        }
        public string discountTotal { get; set; }
        public string salesman { get; set; }
        public string tax10 { get; set; }
        public string totalNet { get; set; }
        public string warehouse { get; set; }
        public string paymentTotal { get; set; }

        public InvDetails[] orderDetail { get; set; }
    }

    public class InvDetails
    {
        public string item { get; set; }
        public string unit { get; set; }
        public string rawPrice { get; set; }
        public string quantity { get; set; }
        public string taxPrice { get; set; }
        public string bonus { get; set; }
        public string sourceDoc { get; set; }

    }
    class Areas
    {
      //  public Areas()
      //  { treeParent = ""; }
        public string code { get; set; }
        public string name { get; set; }
        public string treeParent { get; set; }
        public string treeLevel { get; set; }
    }
}