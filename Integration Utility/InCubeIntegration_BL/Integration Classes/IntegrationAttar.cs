 
using System;
using System.Collections.Generic;
using System.Data; 
using InCubeIntegration_DAL;
using InCubeLibrary;
using System.Data.SqlClient;
using System.ComponentModel;
using System.Text; 
using System.Net;
using System.Linq;
using Newtonsoft.Json;

namespace InCubeIntegration_BL
{

    public class IntegrationAttar : IntegrationBase ,IDisposable// Live branch
    {
        BackgroundWorker bgwCheckProgress;
        InCubeDatabase db_res;
        int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
        SqlCommand cmd;
        InCubeQuery incubeQuery = null;
        string StagingTable = "";
         string _WarehouseID = "-1";
        string _Session = "";
        WebHeaderCollection webHeader = null;
          ~IntegrationAttar()
        {
            
                CloseSession();
          
        }
        protected virtual void Dispose(bool disposing)
        {
            CloseSession();
        }
        public IntegrationAttar(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            if (CoreGeneral.Common.CurrentSession.loginType != LoginType.WindowsService)
            {
                db_res = new InCubeDatabase();
                db_res.Open("InCube", "IntegrationAttar");
            }

            GetSession();

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
            CloseSession();
            if (db_res != null && db_res.GetConnection().State == ConnectionState.Open)
                db_res.Close();
        }


        public override void Closeing()
        {
            CloseSession();
        }

        public override void UpdateItem()
        {
            GetMasterData(IntegrationField.Item_U);
        }
        public override void UpdateInvoice()
        {
            GetMasterData(IntegrationField.Invoice_U);
        }
        public override void UpdateStock()
        {
             _WarehouseID = Filters.WarehouseID.ToString();
            GetMasterData(IntegrationField.Stock_U);
        }
        public override void UpdateCustomer( )
        {
             
            GetMasterData(IntegrationField.Customer_U);
        }
        public override void UpdateAreas()
        {

            GetMasterData(IntegrationField.Areas_U);
        }
        public override void OutStanding()
        { GetMasterData(IntegrationField.Outstanding_U); }
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
                    case IntegrationField.Outstanding_U:
                        res = GetCustomerBalanceTable(ref dtMasterData);
                        StagingTable = "Stg_CustomerBalances";
                        ProcName = "sp_UpdateCustomerBalances";
                        break;
                    case IntegrationField.Invoice_U:
                        res = GetInvoicesTable(ref dtMasterData);
                        StagingTable = "Stg_Invoices";
                        ProcName = "sp_UpdateInvoices";
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
            {   CloseSession();  
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
            if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
            Result res = Result.Failure;
            try
            {
                
                DT = Tools.GetRequestTable<Categories>(CoreGeneral.Common.GeneralConfigurations.WS_URL+"/TPhenixApi/ClassGetalllist", "", "classes", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET",webHeader);

              if(DT!=null &&DT.Rows.Count>0)  res = SaveTable(DT, "stg_Categories");

                DT = Tools.GetRequestTable<Items>(CoreGeneral.Common.GeneralConfigurations.WS_URL+"/TPhenixApi/ItemsGetAllList/0", "", "items", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET",webHeader);
                
                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                CloseSession();
            }
            return res;
        }
        private void GetSession()
        {
            try
            {
                //Test --
                CoreGeneral.Common.GeneralConfigurations.WS_URL = "HTTP://127.0.0.1:8282/api/rest";
                CoreGeneral.Common.GeneralConfigurations.WS_UserName = "api";
                CoreGeneral.Common.GeneralConfigurations.WS_Password = "incube";
                ph_result[] o = Tools.GetRequest<ph_result>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/initialize/3/0/123/3715042", "", "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET");

                //Test Site
                // ph_result[] o = Tools.GetRequest<ph_result>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/initialize/12/0/123/3715042", "", "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET");
                //Live
                //ph_result[] o = Tools.GetRequest<ph_result>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/initialize/1/0/123/3715042", "", "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET");
                //ph_result[] o = Tools.GetRequest<ph_result>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/initialize/16/0/123/3715042", "", "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET");

                if (o != null && o.Length > 0)
                {
                    _Session = o[0].session;
                    webHeader = new WebHeaderCollection();
                    webHeader.Add("Pragma", "dssession=" + _Session);
                }
            }
            catch (Exception ex)
            {
                CloseSession();
                WriteMessage("Cannot Connect to ERP server,Please check the netowrk and reset the ERP service !!");
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void CloseSession()
        {
            if (webHeader == null || webHeader.Count == 0) return;
            try
            {
                //string body = "{\"_parameters\":[{\"controls\":[{\"value\": \"21048\",\"type\": 4,\"name\": \"EDT_INNO\"},{\"value\": \"8\",\"type\": 2,\"name\": \"FRM_PERIOD\"},{\"value\": \"8\",\"type\": 2,\"name\": \"FRM_PERIOD2\"},{\"Vyear\": 0,\"type\": 9,\"name\": \"DT_FM\",\"Vday\": 0,\"Vmonth\": 0},{\"Vyear\": 0,\"type\": 9,\"name\": \"DT_TM\",\"Vday\": 0,\"Vmonth\": 0},{\"Vyear\": 0,\"type\": 9,\"name\": \"DT_FP\",\"Vday\": 0,\"Vmonth\": 0},{\"Vyear\": 0,\"type\": 9,\"name\": \"DT_TP\",\"Vday\": 0,\"Vmonth\": 0}]}]}";
                object[] o = Tools.GetRequest<object>(CoreGeneral.Common.GeneralConfigurations.WS_URL+"/CloseSession/", "", "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET",webHeader);
            _Session = "";
                webHeader = null;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private Result GetCustomerTable(ref DataTable DT)
        {
            if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
            Result res = Result.Failure;
            try
            {

                DT = new DataTable();
                List<CustomerGroup[]> x = (Tools.GetRequest<CustomerGroup[]>(CoreGeneral.Common.GeneralConfigurations.WS_URL+"/TPhenixApi/find/33/", "", "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader)).OfType<CustomerGroup[]>().ToList();

                DT = Tools.ToDataTable<CustomerGroup>(x[0]);
                if (DT != null && DT.Rows.Count > 0) res = SaveTable(DT, "stg_CustomerGroup");

                DT = Tools.GetRequestTable<AttCustomers>(CoreGeneral.Common.GeneralConfigurations.WS_URL+"/TPhenixApi/CustomerGetAllList/0/1", "", "clients", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);

                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;


            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                CloseSession();
            }
            return res;
        }
        private Result GetCustomerBalanceTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            try
            {

                DT = new DataTable();
                DT = Tools.GetRequestTable<CustomerBalance>(CoreGeneral.Common.GeneralConfigurations.WS_URL+"/TPhenixApi/\"Report\"/187", CustomerBalanceBody, "DATA", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);

                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;


            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                CloseSession();
            }
            return res;
        }


        private Result GetSalespersonTable(ref DataTable DT)
        {
            if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
            Result res = Result.Failure;
            try
            {
                DT = new DataTable();
                List<Salesperson[]> x=( Tools.GetRequest<Salesperson[]>(CoreGeneral.Common.GeneralConfigurations.WS_URL+"/TPhenixApi/find/37/", "", "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader)).OfType<Salesperson[]>().ToList();

                DT = Tools.ToDataTable<Salesperson>(x[0]);
                
                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally { CloseSession(); }
            return res;
        }

        private Result GetWarehousTable(ref DataTable DT)
        {
            if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
            Result res = Result.Failure;
            try
            {
                List<AttWarehouse[]> x = (Tools.GetRequest<AttWarehouse[]>(CoreGeneral.Common.GeneralConfigurations.WS_URL+"/TPhenixApi/find/8/", "", "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader)).OfType<AttWarehouse[]>().ToList();

                DT = Tools.ToDataTable<AttWarehouse>(x[0]);

                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                CloseSession();
            }
            return res;
        }
        private Result GetInvoicesTable(ref DataTable DT)
        {
            if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
            Result res = Result.Failure;
            try
            {
                DT = Tools.GetRequestTable<Invoices>(CoreGeneral.Common.GeneralConfigurations.WS_URL+"/TPhenixApi/\"Report\"/859", GetInvoicesBody, "DATA", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);

                 
                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                CloseSession();
            }
            return res;
        }


        private Result GetStockTable(ref DataTable DT)
        {
            if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));

            Result res = Result.Failure;
            try
            {
                DT = Tools.GetRequestTable<WarehouseStock>(CoreGeneral.Common.GeneralConfigurations.WS_URL+"/TPhenixApi/\"Report\"/13", stockBody, "DATA", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);

                
                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                CloseSession();
            }
            return res;
        }
        // string stockBody = "{\"_parameters\":[{\"controls\": [{\"Vyear\": 0,\"type\": 9,\"name\": \"DTP_FROM\",\"Vday\": 0,\"Vmonth\": 0},{\"Vyear\": 0,\"type\": 9,\"name\": \"DTP_TO\",\"Vday\": 0,\"Vmonth\": 0},{\"items\": [{\"value\": 0,\"name\": \"اليوم\"},{\"value\": 1,\"name\": \"اليوم السابق\"},{\"value\": 2,\"name\": \"هذا الشهر\"},{\"value\": 3,\"name\": \"هذا الشهر حتى اليوم\"},{\"value\": 4,\"name\": \"هذا الربع من السنة\"},{\"value\": 5,\"name\": \"هذا الربع من السنة حتى اليوم\"},{\"value\": 6,\"name\": \"هذه السنة\"},{\"value\": 7,\"name\": \"هذه السنة حتى اليوم\"},{\"value\": 8,\"name\": \"بداية السنة المحاسبية\"},{\"value\": 9,\"name\": \"بدون\"}],\"value\": \"9\",\"type\": 2,\"name\": \"FRM_PERIOD\"},{\"limit\": 0,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRAM_MAT\"},{\"limit\": 0,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"F_CLASS\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRAM_STORE\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"F_CLIENT\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRM_BRANSH\"},{\"items\": [{\"value\": 1,\"name\": \"الواحدة الأساسية\"},{\"value\": 50000,\"name\": \"واحدة المبيع\"},{\"value\": 55000,\"name\": \"واحدة الشراء\"},{\"value\": 2,\"name\": \"الواحدة 2\"}],\"value\": \"50000\",\"type\": 2,\"name\": \"CB_UT\"}]}]}";
        string stockBody = "{\"_parameters\":[{\"controls\":[{\"value\":\"50000\",\"type\":2,\"name\":\"CB_UT\"},{\"value\":\"0\",\"type\":2,\"name\":\"CB_SHOW\"},{\"Vyear\":2019,\"type\":9,\"name\":\"DTP_FROM\",\"Vday\":1,\"Vmonth\":1},{\"Vyear\":2019,\"type\":9,\"name\":\"DTP_TO\",\"Vday\":23,\"Vmonth\":5},{\"value\":\"8\",\"type\":2,\"name\":\"FRM_PERIOD\"},{\"value\":\"0,2\",\"type\":3,\"name\":\"CH_GROUPBY\"}]}]}";
            //"{\"_parameters\":[{\"controls\": [{\"limit\": 0,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"F_MATERIAL\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"F_CLASS\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"F_STORAGE\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"F_CLIENT\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRM_BRANSH\"},{\"items\": [{\"value\": 1,\"name\": \"fdghj\"},{\"value\": 50000,\"name\": \"jklhgf\"},{\"value\": 55000,\"name\": \"sdfghjh\"},{\"value\": 2,\"name\": \"fggf2\"}],\"value\": \"50000\",\"type\": 2,\"name\": \"CB_UT\"},{\"items\": [{\"value\": 0,\"name\": \"uio\"},{\"value\": 1,\"name\": \"ee\"},{\"value\": 2,\"name\": \"rr\"}],\"value\": \"0\",\"type\": 2,\"name\": \"CB_SHOW\"},{\"Vyear\": 2019,\"type\": 9,\"name\": \"DTP_FROM\",\"Vday\": 1,\"Vmonth\": 1},{\"Vyear\": 2019,\"type\": 9,\"name\": \"DTP_TO\",\"Vday\": 23,\"Vmonth\": 5},{\"items\": [{\"value\": 0,\"name\": \"uuu\"},{\"value\": 1,\"name\": \"i\"},{\"value\": 2,\"name\": \"p\"},{\"value\": 3,\"name\": \"u\"},{\"value\": 4,\"name\": \"o\"},{\"value\": 5,\"name\": \"t\"},{\"value\": 6,\"name\": \"aaw\"},{\"value\": 7,\"name\": \"df\"},{\"value\": 8,\"name\": \"hjk\"},{\"value\": 9,\"name\": \"asd\"}],\"value\": \"8\",\"type\": 2,\"name\": \"FRM_PERIOD\"},{\"items\": [{\"selected\": 1,\"value\": 0,\"name\": \"as\"},{\"selected\": 0,\"value\": 3,\"name\": \"asd\"},{\"selected\": 0,\"value\": 1,\"name\": \"a\"},{\"selected\": 1,\"value\": 2,\"name\": \"f\"},{\"selected\": 0,\"value\": 4,\"name\": \"d\"},{\"selected\": 0,\"value\": 5,\"name\": \"s\"},{\"selected\": 0,\"value\": 12,\"name\": \"a\"}],\"value\": \"0,2,3,5\",\"type\": 3,\"name\": \"CH_GROUPBY\"},{\"value\": 0,\"type\": 8,\"name\": \"CH_WITHOUTPREV\"},{\"value\": 0,\"type\": 8,\"name\": \"CH_ZEROMAT\"},{\"value\": 1,\"type\": 8,\"name\": \"CHK_IGNOREMPTYITEMS\"},{\"value\": 0,\"type\": 8,\"name\": \"CH_STATISTICSUT\"},{\"value\": 1,\"type\": 8,\"name\": \"CH_GETBEGINAMOUNT\"}]}]}";
        string PriceBody = "{\"_parameters\":[{\"controls\": [{\"Vyear\": 0,\"type\": 9,\"name\": \"DTP_FROM\",\"Vday\": 0,\"Vmonth\": 0},{\"Vyear\": 0,\"type\": 9,\"name\": \"DTP_TO\",\"Vday\": 0,\"Vmonth\": 0},{\"items\": [{\"value\": 0,\"name\": \"a\"},{\"value\": 1,\"name\": \"b\"},{\"value\": 2,\"name\": \"c\"},{\"value\": 3,\"name\": \"d\"},{\"value\": 4,\"name\": \"e\"},{\"value\": 5,\"name\": \"f\"},{\"value\": 6,\"name\": \"j\"},{\"value\": 7,\"name\": \"h\"},{\"value\": 8,\"name\": \"i\"},{\"value\": 9,\"name\": \"s\"}],\"value\": \"9\",\"type\": 2,\"name\": \"FRM_PERIOD\"},{\"limit\": 0,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRAM_MAT\"},{\"limit\": 0,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"F_CLASS\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRAM_STORE\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"F_CLIENT\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRM_BRANSH\"},{\"items\": [{\"value\": 1,\"name\": \"dd\"},{\"value\": 50000,\"name\": \"ff\"},{\"value\": 55000,\"name\": \"ss\"},{\"value\": 2,\"name\": \"rr\"}],\"value\": \"50000\",\"type\": 2,\"name\": \"CB_UT\"}]}]}";
        string CustomerBalanceBody = "{\"_parameters\":[ {    \"controls\": [     {      \"Vyear\": 2019,      \"type\": 9,      \"name\": \"DFROM\",      \"Vday\": 1,      \"Vmonth\": 1     },     {      \"Vyear\": 2019,      \"type\": 9,      \"name\": \"DTO\",      \"Vday\": 22,      \"Vmonth\": 5     },     {      \"items\": [       {        \"value\": 0,        \"name\": \"اليوم\"       },       {        \"value\": 1,        \"name\": \"اليوم السابق\"       },       {        \"value\": 2,        \"name\": \"هذا الشهر\"       },       {        \"value\": 3,        \"name\": \"هذا الشهر حتى اليوم\"       },       {        \"value\": 4,        \"name\": \"هذا الربع من السنة\"       },       {        \"value\": 5,        \"name\": \"هذا الربع من السنة حتى اليوم\"       },       {        \"value\": 6,        \"name\": \"هذه السنة\"       },       {        \"value\": 7,        \"name\": \"هذه السنة حتى اليوم\"       },       {        \"value\": 8,        \"name\": \"بداية السنة المحاسبية\"       },       {        \"value\": 9,        \"name\": \"بدون\"       }      ],      \"value\": \"8\",      \"type\": 2,      \"name\": \"FRM_PERIOD\"     },     {      \"limit\": 1,      \"items\": [],      \"value\": \"\",      \"type\": 1,      \"name\": \"FRM_ACC\"     },     {      \"limit\": 1,      \"items\": [],      \"value\": \"\",      \"type\": 1,      \"name\": \"FRM_CLIENT\"     },     {      \"limit\": 1,      \"items\": [],      \"value\": \"\",      \"type\": 1,      \"name\": \"FRM_CUSTGRP\"     },     {      \"limit\": 1,      \"items\": [       {        \"value\": \"ل.س\",        \"name\": \"1\"       }      ],      \"value\": \"\",      \"type\": 1,      \"name\": \"FRM_CURRENCY\"     },     {      \"limit\": 1,      \"items\": [],      \"value\": \"\",      \"type\": 1,      \"name\": \"FRM_BRANSH\"     }    ]   } ]}";
        string GetInvoicesBody = "{\"_parameters\":[{\"controls\": [{\"Vyear\": 2019,\"type\": 9,\"name\": \"DTP_FROM\",\"Vday\": 1,\"Vmonth\": 1},{\"Vyear\": 2019,\"type\": 9,\"name\": \"DTP_TO\",\"Vday\": 10,\"Vmonth\": 7},{\"items\": [{\"value\": 0,\"name\": \"اليوم\"},{\"value\": 1,\"name\": \"اليوم السابق\"},{\"value\": 2,\"name\": \"هذا الشهر\"},{\"value\": 3,\"name\": \"هذا الشهر حتى اليوم\"},{\"value\": 4,\"name\": \"هذا الربع من السنة\"},{\"value\": 5,\"name\": \"هذا الربع من السنة حتى اليوم\"},{\"value\": 6,\"name\": \"هذه السنة\"},{\"value\": 7,\"name\": \"هذه السنة حتى اليوم\"},{\"value\": 8,\"name\": \"بداية السنة المحاسبية\"},{\"value\": 9,\"name\": \"بدون\"}],\"value\": \"8\",\"type\": 2,\"name\": \"FRM_PERIOD\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"F_CLIENT\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRAM_CLIENTGROUP\"},{\"items\": [{\"value\": 0,\"name\": \"بدون تحديد\"},{\"value\": 1,\"name\": \"قبض\"},{\"value\": 2,\"name\": \"دفع\"}],\"value\": \"1\",\"type\": 2,\"name\": \"CB_PAYTYPE\"},{\"items\": [{\"value\": 0,\"name\": \"الكل\"},{\"value\": 1,\"name\": \"غير محصل\"},{\"value\": 2,\"name\": \"محصل\"}],\"value\": \"1\",\"type\": 2,\"name\": \"CB_PAIDTYPE\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRM_BRANSH\"},{\"value\": \"0\",\"type\": 6,\"name\": \"SUINUMBEREDIT2\"},{\"items\": [{\"selected\": 1,\"value\": 1,\"name\": \"الفاتورة\"},{\"selected\": 0,\"value\": 2,\"name\": \"نمط الفاتورة\"},{\"selected\": 0,\"value\": 3,\"name\": \"العميل\"},{\"selected\": 0,\"value\": 4,\"name\": \"التاريخ\"},{\"selected\": 0,\"value\": 6,\"name\": \"المندوب\"},{\"selected\": 0,\"value\": 7,\"name\": \"مركز الكلفة\"}],\"value\": \"1\",\"type\": 3,\"name\": \"CH_GROUPBY\"}]}]}";
        private Result GetPricesTable(ref DataTable DT)
        {
            if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
            Result res = Result.Failure;
            try
            { 
                List<CustomerPrices[]> x = (Tools.GetRequest<CustomerPrices[]>(CoreGeneral.Common.GeneralConfigurations.WS_URL+"/TPhenixApi/CustomerGetCustomisedPrices/0/0", "", "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader)).OfType<CustomerPrices[]>().ToList();
 
                DT = Tools.ToDataTable<CustomerPrices>(x[0]);
                SaveTable(DT, "Stg_CustomerPrices");

                DT = Tools.GetRequestTable<Prices>(CoreGeneral.Common.GeneralConfigurations.WS_URL+"/TPhenixApi/\"Report\"/710", PriceBody, "DATA", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);



                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                CloseSession();
            }
            return res;
        }
        private Result IsSentTransaction(string TransactionId)
        {
            if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));

            Result res = Result.Failure;
            try
            {
                string body = "{\"_parameters\":[{\"controls\":[ { \"value\": \"8\", \"type\": 2, \"name\": \"FRM_PERIOD\" }, { \"value\": \"" + TransactionId + "\", \"type\": 4, \"name\": \"SUINUMBEREDIT1\" }, { \"Vyear\": 0, \"type\": 9, \"name\": \"DRFROM\", \"Vday\": 0, \"Vmonth\": 0 }, { \"Vyear\": 0, \"type\": 9, \"name\": \"TO_RDATE\", \"Vday\": 0, \"Vmonth\": 0 } ]}]}";

                CheckTrans[] DT = Tools.GetRequest<CheckTrans>(CoreGeneral.Common.GeneralConfigurations.WS_URL+"/TPhenixApi/\"Report\"/42", body, "DATA", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);

                if (DT != null && DT.Count() > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                res = Result.NoRowsFound;
            }
            finally
            {
                //CloseSession();
            }
            return res;
        }


        private Result IsSentCash(string TransactionId)
        {
            if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));

            Result res = Result.Failure;
            try
            {
                string body = "{\"_parameters\":[{\"controls\":[ { \"value\": \"8\", \"type\": 2, \"name\": \"FRM_PERIOD\" }, { \"value\": \""+TransactionId+"\", \"type\": 4, \"name\": \"EDT_BNRID\" } ]}]} ";

                CheckTrans[] DT = Tools.GetRequest<CheckTrans>(CoreGeneral.Common.GeneralConfigurations.WS_URL+"/TPhenixApi/\"Report\"/302", body, "DATA", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);

                if (DT != null && DT.Count() > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                res = Result.NoRowsFound;
            }
            finally
            {
                //CloseSession();
            }
            return res;
        }


        private Result IsSentCheeq(string TransactionId)
        {
            if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));

            Result res = Result.Failure;
            try
            {
                //string body = "{\"_parameters\":[{\"controls\":[ { \"value\": \""+TransactionId+"\", \"type\": 4, \"name\": \"EDT_INNO\" }, { \"value\": \"8\", \"type\": 2, \"name\": \"FRM_PERIOD\" }, { \"value\": \"8\", \"type\": 2, \"name\": \"FRM_PERIOD2\" }, { \"Vyear\": 0, \"type\": 9, \"name\": \"DT_FM\", \"Vday\": 0, \"Vmonth\": 0 }, { \"Vyear\": 0, \"type\": 9, \"name\": \"DT_TM\", \"Vday\": 0, \"Vmonth\": 0 }, { \"Vyear\": 0, \"type\": 9, \"name\": \"DT_FP\", \"Vday\": 0, \"Vmonth\": 0 }, { \"Vyear\": 0, \"type\": 9, \"name\": \"DT_TP\", \"Vday\": 0, \"Vmonth\": 0 } ]}]}";
                string body = "{\"_parameters\":[{\"controls\":[{\"value\":\"" + TransactionId + "\",\"type\":4,\"name\":\"EDT_INNO\"},{\"value\":\"9\",\"type\":2,\"name\":\"FRM_PERIOD\"},{\"value\":\"9\",\"type\":2,\"name\":\"FRM_PERIOD2\"},{\"Vyear\":0,\"type\":9,\"name\":\"DT_FM\",\"Vday\":0,\"Vmonth\":0},{\"Vyear\":0,\"type\":9,\"name\":\"DT_TM\",\"Vday\":0,\"Vmonth\":0},{\"Vyear\":0,\"type\":9,\"name\":\"DT_FP\",\"Vday\":0,\"Vmonth\":0},{\"Vyear\":0,\"type\":9,\"name\":\"DT_TP\",\"Vday\":0,\"Vmonth\":0}]}]}";
                CheckTrans[] DT = Tools.GetRequest<CheckTrans>(CoreGeneral.Common.GeneralConfigurations.WS_URL+"/TPhenixApi/\"Report\"/488", body, "DATA", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);

                if (DT != null && DT.Count() > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                res = Result.NoRowsFound;
            }
            finally
            {
                //CloseSession();
            }
            return res;
        }
        public override void SendReciepts()
        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
                string Type = "", Notes = "", CustomerPaymentID = "", EmployeeCode = "", ERPInvoiceNo = "", AccNo = "", AppliedAmount = "", CustomerCode = "", OutletCode = "", VoucherDate = "", VoucherNumber = "", branch = "";

                DateTime PaymentDate;
                DateTime cheeqDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                DataTable invoices = null;

                string CashTemp = "{11}\"_parameters\": [{11}\"billid\":{0},\"bondtypeid\":{1}, \"dateMonth\":{2} ,\"dateYear\":{3},\"dateDay\":{4} ,\"billpayment\":{5},\"dateHour\":{6},\"dateMinute\":{7} ,\"delid\":{8},\"note\":\"{9}\" ,\"pn_receiptnumber\":\"{10}\" {12}]{12}";
                //string CheeqTemp = "{13}\"_parameters\": [{13}\"pn_ClientId\":{0},\"pn_customer_manipulateid\":{1},\"pn_typeid\":{2},\"pn_fto_accid\":0,\"dateDay\":{3},\"dateYear\":{4},\"dateMonth\":{5},\"pn_value\":{6},\"pn_curid\":1,\"pn_curequal\":1,\"pn_number\":\"{7}\" ,\"matdateDay\":{8},\"matdateYear\":{9},\"matdateMonth\":{10},\"pn_note\":\"{11}\",\"pn_delid\":\"{12}\",\"pn_receiptnumber\":\"{15}\" ,\"billspayments\":{16} {14}]{14}";
                string CheeqTemp = "{13}\"_parameters\": [{13} \"paymentdata\":{13}\"customerid\":{0},\"customerManipulateid\":{1},\"bondtypeid\":1,\"paymentaccount\":{2} ,\"dateDay\":{3},\"dateYear\":{4},\"dateMonth\":{5},\"value\":{6},\"currency\":1,\"currencyequal\":1,\"note\":\"{11}\",\"delid\":\"{12}\",\"pn_receiptnumber\":\"{15}\"{14},\"papernotes\":[{13} \"ccid\":0,\"pn_receiptnumber\":\"{15}\",\"pnnumber\":\"{7}\"  ,\"dateDay\":{3},\"dateYear\":{4},\"dateMonth\":{5},\"value\":{6},\"matdateDay\":{8},\"matdateYear\":{9},\"matdateMonth\":{10},\"note\":\"{11}\"{14}] , \"billspayments\":{16} {14}]{14}";
                string DownpaymentTemp = "{10}\"_parameters\": [{10}\"customerid\":{0},\"customerManipulateid\":{1},\"bondtypeid\":{2},\"paymentaccount\":0,\"dateDay\":{3},\"dateYear\":{4},\"dateMonth\":{5},\"value\":{6},\"currency\":1,\"currencyequal\":1,\"delid\":{7},\"note\":\"{8}\" ,\"pn_receiptnumber\":\"{9}\"{11}]{11}";
                string salespersonFilter = "";

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND CP.EmployeeID = " + Filters.EmployeeID;
                }
                string invoicesHeader = string.Format(@"SELECT 1 Type,    cp.AppliedPaymentID CustomerPaymentID,e.EmployeeCode, CP.PaymentDate,t.Description ERPInvoiceNo,e.MinHours CashAccNo ,  sum(AppliedAmount)AppliedAmount  ,c.CustomerCode,o.CustomerCode OutletCode,cp.VoucherDate,cp.VoucherNumber,b.Code bank, Bl.description branch
                                      ,cp.Notes 
									  FROM CustomerPayment CP INNER join [transaction] t on cp.TransactionID=t.TransactionID and (t.salesmode=2/* or t.transactiontypeid =3*/)
											INNER join Customer c on c.CustomerID=cp.CustomerID  
											INNER join CustomerOutlet  o on o.CustomerID=cp.CustomerID and  o.OutletID=cp.OutletID  
											INNER join Employee e on cp.EmployeeID=e.EmployeeID
                                              Left Join Bank B on CP.BankID=B.BankID  
                                                Left Join BankBranchLanguage Bl on CP.BankID=Bl.BankID and CP.BranchID=Bl.BranchID and Bl.LanguageID=1
                                                WHERE       CP.PaymentStatusID <> 5 AND CP.PaymentTypeID IN (1) and cp.Synchronized=0 and isnull(t.description,'')<>''
                         {6}
                        AND					    CP.PaymentDate >= CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102) 
                                                  AND CP.PaymentDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102)
group by    cp.AppliedPaymentID,e.EmployeeCode,e.MinHours,c.CustomerCode,t.Description,CP.PaymentDate,bl.Description, cp.ExchangeRate  ,O.BlockNumber ,o.CustomerCode   ,cp.VoucherDate,cp.VoucherNumber,b.Code  ,cp.Notes 
Union All

SELECT 2 Type,  CP.CustomerPaymentID,e.EmployeeCode, CP.PaymentDate,''   ERPInvoiceNo,e.MaxOTHours CashAccNo ,  sum(AppliedAmount)AppliedAmount  ,c.CustomerCode,o.CustomerCode  OutletCode,cp.VoucherDate,cp.VoucherNumber,b.Code bank, Bl.description branch
                                      ,cp.Notes 
									  FROM CustomerPayment CP  
											INNER join Customer c on c.CustomerID=cp.CustomerID  
											INNER join CustomerOutlet  o on o.CustomerID=cp.CustomerID and  o.OutletID=cp.OutletID  
											INNER join Employee e on cp.EmployeeID=e.EmployeeID
                                              Left Join Bank B on CP.BankID=B.BankID  
                                                Left Join BankBranchLanguage Bl on CP.BankID=Bl.BankID and CP.BranchID=Bl.BranchID and Bl.LanguageID=1
                                                WHERE       CP.PaymentStatusID <> 5 AND CP.PaymentTypeID IN (2,3) and cp.Synchronized=0  
                         {6}
                        AND					    CP.PaymentDate >= CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102) 
                                                  AND CP.PaymentDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102)
group by   CP.CustomerPaymentID,e.EmployeeCode,e.MaxOTHours,c.CustomerCode ,CP.PaymentDate,bl.Description, cp.ExchangeRate  ,O.BlockNumber ,o.CustomerCode   ,cp.VoucherDate,cp.VoucherNumber,b.Code  ,cp.Notes 

Union All

SELECT 4 Type,  CP.CustomerPaymentID,e.EmployeeCode, CP.PaymentDate,''   ERPInvoiceNo,e.MaxOTHours CashAccNo ,  sum(PaidAmount)AppliedAmount  ,c.CustomerCode,o.CustomerCode  OutletCode,cp.VoucherDate,cp.VoucherNumber,b.Code bank, Bl.description branch
                                      ,cp.Notes 
									  FROM CustomerUnallocatedPayment CP  
											INNER join Customer c on c.CustomerID=cp.CustomerID  
											INNER join CustomerOutlet  o on o.CustomerID=cp.CustomerID and  o.OutletID=cp.OutletID  
											INNER join Employee e on cp.EmployeeID=e.EmployeeID
                                              Left Join Bank B on CP.BankID=B.BankID  
                                                Left Join BankBranchLanguage Bl on CP.BankID=Bl.BankID and CP.BranchID=Bl.BranchID and Bl.LanguageID=1
                                                WHERE       CP.voided <> 1 AND CP.PaymentTypeID IN (2,3) and cp.Synchronised=0  
                         {6}
                        AND					    CP.PaymentDate >= CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102) 
                                                  AND CP.PaymentDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102)
group by   CP.CustomerPaymentID,e.EmployeeCode,e.MaxOTHours,c.CustomerCode ,CP.PaymentDate,bl.Description   ,O.BlockNumber ,o.CustomerCode   ,cp.VoucherDate,cp.VoucherNumber,b.Code  ,cp.Notes 

Union All

SELECT 3 Type,  CP.CustomerPaymentID,e.EmployeeCode, CP.PaymentDate,''   ERPInvoiceNo,e.MinHours CashAccNo ,  sum(PaidAmount)AppliedAmount  ,c.CustomerCode,o.CustomerCode OutletCode,cp.VoucherDate,cp.VoucherNumber,b.Code bank, Bl.description branch
                                      ,cp.Notes 
									  FROM CustomerUnallocatedPayment CP  
											INNER join Customer c on c.CustomerID=cp.CustomerID  
											INNER join CustomerOutlet  o on o.CustomerID=cp.CustomerID and  o.OutletID=cp.OutletID  
											INNER join Employee e on cp.EmployeeID=e.EmployeeID
                                              Left Join Bank B on CP.BankID=B.BankID  
                                                Left Join BankBranchLanguage Bl on CP.BankID=Bl.BankID and CP.BranchID=Bl.BranchID and Bl.LanguageID=1
                                                WHERE       CP.voided <> 1 AND CP.PaymentTypeID IN (1) and cp.Synchronised=0  
                         {6}
                        AND					    CP.PaymentDate >= CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102) 
                                                  AND CP.PaymentDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102)
group by   CP.CustomerPaymentID,e.EmployeeCode,e.MinHours,c.CustomerCode ,CP.PaymentDate,bl.Description   ,O.BlockNumber ,o.CustomerCode   ,cp.VoucherDate,cp.VoucherNumber,b.Code  ,cp.Notes 


"
                    , Filters.FromDate.Year, Filters.FromDate.Month, Filters.FromDate.Day, Filters.ToDate.Year, Filters.ToDate.Month, Filters.ToDate.Day, salespersonFilter);
                incubeQuery = new InCubeQuery(db_vms, invoicesHeader);

                //Logger.WriteLog(FromDate.ToString(), ToDate.ToString(), invoicesHeader, LoggingType.Information, LoggingFiles.errorInv);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Payment  query failed !!"));
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
                        CustomerPaymentID = dtInvoices.Rows[i]["CustomerPaymentID"].ToString();
                        Type = dtInvoices.Rows[i]["Type"].ToString();
                        ReportProgress("Sending Payment: " + CustomerPaymentID);
                        WriteMessage("\r\n" + CustomerPaymentID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(12, CustomerPaymentID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Reciept_S, new List<string>(filters.Values), processID, 60);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            res = Result.Duplicate;
                            if (Type == "1")
                            {
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerPayment] SET Synchronized = 1 WHERE AppliedPaymentID = '" + CustomerPaymentID + "'");
                            }
                            else if (Type == "2")
                            {
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerPayment] SET Synchronized = 1 WHERE CustomerPaymentID = '" + CustomerPaymentID + "'");
                            }
                            else
                            {
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerUnallocatedPayment] SET Synchronised = 1 WHERE CustomerPaymentID = '" + CustomerPaymentID + "'");
                            }
                            incubeQuery.ExecuteNonQuery();
                            throw (new Exception("CustomerPayment already sent  check table  Int_ExecutionDetails!!"));
                        }
                        else
                        {
                            switch (Type.Trim())
                            {
                                case "1":
                                    if (IsSentCash(CustomerPaymentID) == Result.Success)
                                    {
                                        res = Result.Duplicate;
                                        incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerPayment] SET Synchronized = 1 WHERE CustomerPaymentID = '" + CustomerPaymentID + "'");
                                        incubeQuery.ExecuteNonQuery();
                                        result.Append("Duplicate Payment [" + CustomerPaymentID + "]");
                                        throw (new Exception("Payment already sent and checked in API  check table  Int_ExecutionDetails!!"));
                                    }
                                    break;

                                case "2":
                                    if (IsSentCheeq(CustomerPaymentID) == Result.Success)
                                    {
                                        res = Result.Duplicate;
                                        incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerPayment] SET Synchronized = 1 WHERE CustomerPaymentID = '" + CustomerPaymentID + "'");
                                        incubeQuery.ExecuteNonQuery();
                                        result.Append("Duplicate Payment [" + CustomerPaymentID + "]");
                                        throw (new Exception("Payment already sent and checked in API  check table  Int_ExecutionDetails!!"));
                                    }
                                    break;
                                case "3":
                                    if (IsSentCash(CustomerPaymentID) == Result.Success)
                                    {
                                        res = Result.Duplicate;
                                        incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerUnallocatedPayment] SET Synchronised = 1 WHERE CustomerPaymentID = '" + CustomerPaymentID + "'");
                                        incubeQuery.ExecuteNonQuery();
                                        result.Append("Duplicate Payment [" + CustomerPaymentID + "]");
                                        throw (new Exception("Payment already sent and checked in API  check table  Int_ExecutionDetails!!"));
                                    }
                                    break;
                                case "4":
                                    if (IsSentCheeq(CustomerPaymentID) == Result.Success)
                                    {
                                        res = Result.Duplicate;
                                        incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerUnallocatedPayment] SET Synchronised = 1 WHERE CustomerPaymentID = '" + CustomerPaymentID + "'");
                                        incubeQuery.ExecuteNonQuery();
                                        result.Append("Duplicate Payment [" + CustomerPaymentID + "]");
                                        throw (new Exception("Payment already sent and checked in API  check table  Int_ExecutionDetails!!"));
                                    }
                                    break;
                            }

                        }



                        PaymentDate = Convert.ToDateTime(dtInvoices.Rows[i]["PaymentDate"]);
                        Notes = dtInvoices.Rows[i]["Notes"].ToString();
                        EmployeeCode = dtInvoices.Rows[i]["EmployeeCode"].ToString();
                        ERPInvoiceNo = dtInvoices.Rows[i]["ERPInvoiceNo"].ToString();
                        AccNo = dtInvoices.Rows[i]["CashAccNo"].ToString();
                        AppliedAmount = dtInvoices.Rows[i]["AppliedAmount"].ToString();
                        CustomerCode = dtInvoices.Rows[i]["CustomerCode"].ToString();
                        OutletCode = dtInvoices.Rows[i]["OutletCode"].ToString();
                        VoucherDate = dtInvoices.Rows[i]["VoucherDate"].ToString();
                        VoucherNumber = dtInvoices.Rows[i]["VoucherNumber"].ToString();
                        branch = dtInvoices.Rows[i]["branch"].ToString();

                        string cheeqInfo = "";
                        cheeqDate = new DateTime(1900, 1, 1);
                        if (Type == "3" || Type == "2")
                        {

                            cheeqDate = Convert.ToDateTime(dtInvoices.Rows[i]["VoucherDate"].ToString());
                            cheeqInfo = branch + " --- (" + VoucherNumber + ")-" + cheeqDate.ToString("yyyy-MM-dd") + "---";
                        }
                        Notes = cheeqInfo + Notes;
                        string body = "";
                        string Link = "";
                        string Method = "";
                        switch (Type)
                        {
                            case "1": //Cash on invoices
                                body = string.Format(CashTemp, ERPInvoiceNo, AccNo, PaymentDate.Month, PaymentDate.Year, PaymentDate.Day, AppliedAmount, PaymentDate.Hour, PaymentDate.Minute, EmployeeCode, Notes, CustomerPaymentID, "{", "}");
                                Link = CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/BillPayment";
                                Method = "PUT";
                                break;
                            case "2":// PDC & CDC
                            case "3": // PDC & CDC downpayment
                                Link = CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/PaymentVoucher";
                                string details = string.Format(@"SELECT  t.Description ERPInvoiceNo ,AppliedAmount  
FROM CustomerPayment CP INNER join [transaction] t on cp.TransactionID=t.TransactionID and (t.salesmode=2)
  WHERE       CP.CustomerPaymentid ='{0}' and cp.Synchronized=0 ", CustomerPaymentID);
                                string invDetail = "";
                                incubeQuery = new InCubeQuery(details, db_vms);
                                if (incubeQuery.Execute() != InCubeErrors.Success) continue;
                                invoices = incubeQuery.GetDataTable();
                                if (invoices != null)
                                    for (int j = 0; j < invoices.Rows.Count; j++)
                                    {
                                        invDetail += "{\"paymentval\":" + invoices.Rows[j]["AppliedAmount"].ToString() + ",\"billid\":" + invoices.Rows[j]["ERPInvoiceNo"].ToString() + "},";
                                    }
                                if (invDetail.Length > 0) invDetail = "[" + invDetail.Substring(0, invDetail.Length - 1) + "]";
                                body = string.Format(CheeqTemp, CustomerCode, OutletCode, AccNo, PaymentDate.Day, PaymentDate.Year, PaymentDate.Month, AppliedAmount, VoucherNumber, cheeqDate.Day, cheeqDate.Year, cheeqDate.Month, Notes, EmployeeCode, "{", "}", CustomerPaymentID, invDetail);
                                Method = "PUT";
                                break;
                            case "4":// Cash downpayment
                                Link = CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/QuickVoucher";
                                body = string.Format(DownpaymentTemp, CustomerCode, OutletCode, AccNo, PaymentDate.Day, PaymentDate.Year, PaymentDate.Month, AppliedAmount, EmployeeCode, Notes, CustomerPaymentID, "{", "}");
                                Method = "PUT";
                                break;
                        }

                        ph_result[] result1 = Tools.GetRequest<ph_result>(Link, body, "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, Method, webHeader);
                        if (result1 == null || (result1[0].result != null && result1[0].result.Trim() != "success"))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (result1[0].result != null ? result1[0].value : "") + "\r\n json:" + body);
                            WriteMessage("Error .. \r\n" + result1[0].result + " \r\n" + result1[0].value);

                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + result1[0].bill_tid + "\r\n Message:" + (result1[0].bill_tid != null ? result1[0].bill_tid : "") + "\r\n json:" + body);
                            WriteMessage("Success, ERP No: " + result1[0].bill_tid);
                            if (Type == "1")
                            {
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerPayment] SET Synchronized = 1 WHERE AppliedPaymentID = '" + CustomerPaymentID + "'");
                            }
                            else if (Type == "2")
                            {
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerPayment] SET Synchronized = 1 WHERE CustomerPaymentID = '" + CustomerPaymentID + "'");
                            }
                            else
                            {
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerUnallocatedPayment] SET Synchronised = 1 WHERE CustomerPaymentID = '" + CustomerPaymentID + "'");
                            }
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
                WriteMessage("Fetching Payments failed !!");
            }

        }
        public override void SendOrders()
        {
            try
            {

                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
                string salespersonFilter = "", SalesDocType = "", netTotal = "", SalesType = "", SalesMode = "";
                string CustomerCode = "", OutletCode="", Hdiscount = "", Notes = "", TransactionID = "", WarehouseCode = "";
                DateTime TransactionDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string QtyField = "";
                string headerTemp =
"{14}\"_parameters\":[{14}\"billdata\":{14}\"discountamount\":{0}, \"CheckDataValidation\": 0,\"salesmanid\":{1},\"receiptid\":\"{2}\",\"iscash\":{3}," +
"\"dateMonth\":{4},\"customerid\":{5},\"billtype\":{6},\"dateYear\":{7},\"dateDay\":{8},\"dateHour\":{9},\"dateMinute\":{10},\"currencyid\":1,\"note\":\"{11}\",\"customerManipulateid\":\"{16}\",\"warehouseid\":{12}{15},\"billdetaildata\":[{13}]{15}]{15}";


                string detailsTemp = "{5}\"unitid\":{0},\"itemprice\":{1},\"itemid\":{2},\"discountvalue\":{3},\"mat_vatper\":{8},\"{7}\":{4}{6}";

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND T.EmployeeID = " + Filters.EmployeeID;
                }
                string invoicesHeader = string.Format(@"SELECT   T.orderid transactionid,   T.orderdate transactiondate ,  V.Barcode,e.Employeecode , C.CustomerCode, CO.CustomerCode OutletCode , t.Discount,convert(varchar,t.DesiredDeliveryDate)+' - '+isnull((select top(1) Note from SalesOrderNote where OrderID=t.orderid),'') Notes ,2 SalesMode            FROM salesorder T 
                    INNER JOIN CustomerOutlet CO ON CO.CustomerID = T.CustomerID AND CO.OutletID = T.OutletID
                    INNER JOIN Customer  C  ON C .CustomerID = T.CustomerID 
                    INNER JOIN Organization O ON O.OrganizationID = T.OrganizationID
                    INNER JOIN Employee E ON E.EmployeeID = T.EmployeeID
					LEFT JOIN EmployeeVehicle ev on e.EmployeeID=ev.EmployeeID
				    LEFT JOIN Warehouse V ON V.WarehouseID = ev.VehicleID 
                    WHERE T.Synchronized = 0  and t.OrderTypeID=1 
					  AND T.orderdate >= '{0}' AND T.orderdate < '{1}' 
                  {2}
                   and orderstatusid in (1,2) /*and T.TransactionID = 'INV-VC11-0071-000040'*/"
                    , Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
                incubeQuery = new InCubeQuery(db_vms, invoicesHeader);


                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Order header query failed !!"));
                }



                DataTable dtInvoices = incubeQuery.GetDataTable();
                if (dtInvoices.Rows.Count == 0)
                    WriteMessage("There is no Order to send ..");
                else
                    SetProgressMax(dtInvoices.Rows.Count);

                for (int i = 0; i < dtInvoices.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();
                        TransactionID = dtInvoices.Rows[i]["Transactionid"].ToString();
                        ReportProgress("Sending Transaction: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(11, TransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Orders_S, new List<string>(filters.Values), processID,60);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            res = Result.Duplicate;
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [salesorder] SET Synchronized = 1 WHERE orderid = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                            result.Append("Duplicate order [" + TransactionID + "]");
                            throw (new Exception("order already sent  check table  Int_ExecutionDetails!!"));
                        }
                         else if (IsSentTransaction(TransactionID) == Result.Success)
                        {
                            res = Result.Duplicate;
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [salesorder] SET Synchronized = 1 WHERE orderid = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                            result.Append("Duplicate order [" + TransactionID + "]");
                            throw (new Exception("order already sent and checked in API  check table  Int_ExecutionDetails!!"));
                        }

                        TransactionDate = Convert.ToDateTime(dtInvoices.Rows[i]["transactiondate"]);
                        CustomerCode = dtInvoices.Rows[i]["CustomerCode"].ToString();
                        WarehouseCode = dtInvoices.Rows[i]["Barcode"].ToString();
                        string Salesperson = dtInvoices.Rows[i]["Employeecode"].ToString();
                        SalesMode = dtInvoices.Rows[i]["SalesMode"].ToString();
                        SalesType = "83";// dtInvoices.Rows[i]["SalesType"].ToString();
                        Notes = dtInvoices.Rows[i]["Notes"].ToString();
                        Hdiscount = "0";// decimal.Parse(dtInvoices.Rows[i]["discount"].ToString()).ToString("F4");

                        OutletCode = dtInvoices.Rows[i]["OutletCode"].ToString();

                        
                            if (SalesMode != "1")
                            SalesMode = "0";

                        if (WarehouseCode == "") WarehouseCode = "1";


                        string invoiceDetails = string.Format(@"SELECT      
sum(SalesOrderDetail.Quantity) Quantity,
SalesOrderDetail.Price, 
SalesOrderDetail.basePrice, 
sum(SalesOrderDetail.Price*SalesOrderDetail.Quantity* SalesOrderDetail.Discount/100)  Discount,
 convert(int,pack.Width) ItemCode,  
convert(int,pack.Height) UOMID,     
--sum((SalesOrderDetail.Price*SalesOrderDetail.Quantity)-(SalesOrderDetail.Price*SalesOrderDetail.Quantity* SalesOrderDetail.Discount/100))*  SalesOrderDetail.Tax /100 tax
SalesOrderDetail.Tax    
,SalesOrderDetail.salestransactiontypeid ItemType
FROM SalesOrderDetail  INNER JOIN
Pack ON SalesOrderDetail.PackID = Pack.PackID INNER JOIN
Item ON Pack.ItemID = Item.ItemID INNER JOIN 
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID
WHERE (PackTypeLanguage.LanguageID = 1) 
 AND (SalesOrderDetail.OrderID like '{0}')
group by 
SalesOrderDetail.orderid,
SalesOrderDetail.Price,  
SalesOrderDetail.Tax , 
pack.Width, 
pack.Height, 
SalesOrderDetail.basePrice, 
 SalesOrderDetail.salestransactiontypeid   
,SalesOrderDetail.CustomerID,SalesOrderDetail.OutletID
ORDER BY pack.Width
", TransactionID);
                        incubeQuery = new InCubeQuery(db_vms, invoiceDetails);
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            throw (new Exception("order details query failed !!"));
                        }

                        DataTable dtDetails = incubeQuery.GetDataTable();
                        string allDetails = "";
                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        {
                            string ItemCode = "", UOM = "", Quantity = "", Price = "", BasePrice="", Tax = "", discount = "", Type = "", bonus = "0";

                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            UOM = dtDetails.Rows[j]["UOMID"].ToString();
                            Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString( );//.ToString("#0.000");
                            Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString( );//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                            Tax = decimal.Parse(dtDetails.Rows[j]["Tax"].ToString()).ToString( );
                            discount = decimal.Parse(dtDetails.Rows[j]["discount"].ToString()).ToString( );
                            Type = dtDetails.Rows[j]["ItemType"].ToString();
                            Price = decimal.Parse(Price.ToString()).ToString( );
                            BasePrice = dtDetails.Rows[j]["basePrice"].ToString();
                            if (Type == "4")
                            {
                               // QtyField = "bonus";
                                QtyField = "quantity";
                                Price = BasePrice;
                                discount = (decimal.Parse(BasePrice) * decimal.Parse(Quantity)).ToString();
                            }
                            else
                                QtyField = "quantity";

                            allDetails += string.Format(detailsTemp, UOM, Price, ItemCode, discount, Quantity, "{", "}", QtyField,Tax) + ",";


                        }
                        allDetails = allDetails.Substring(0, allDetails.Length - 1);
                        string headerData = string.Format(headerTemp, Hdiscount, Salesperson, TransactionID, SalesMode, DateTime.Parse(TransactionDate.ToString()).Month, CustomerCode, SalesType, DateTime.Parse(TransactionDate.ToString()).Year, DateTime.Parse(TransactionDate.ToString()).Day
                          , DateTime.Parse(TransactionDate.ToString()).Hour, DateTime.Parse(TransactionDate.ToString()).Minute
                            , Notes, WarehouseCode, allDetails, "{", "}", OutletCode);
                        ph_result[] result1 = Tools.GetRequest<ph_result>(CoreGeneral.Common.GeneralConfigurations.WS_URL+"/TPhenixApi/Bill", headerData, "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "PUT", webHeader);
                        if (result1 == null || (result1[0].result != null && result1[0].result.Trim() != "success"))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (result1[0].result != null ? result1[0].value : "") + "\r\n json:" + headerData);
                            WriteMessage("Error .. \r\n" + result1[0].result + " \r\n" + result1[0].value);

                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + result1[0].bill_tid + "\r\n Message:" + (result1[0].bill_id != null ? result1[0].bill_id : "") + "\r\n json:" + headerData);
                            WriteMessage("Success, ERP No: " + result1[0].bill_tid);
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [salesorder] SET Synchronized = 1,Description='" + result1[0].bill_id + "' WHERE orderid = '" + TransactionID + "'");
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
        public override void SendInvoices()
        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
                string salespersonFilter = "", SalesDocType = "", netTotal = "", SalesType = "", SalesMode = "";
                string CustomerCode = "", IsCredit="", OutletCode = "", Hdiscount ="", Notes = "", TransactionID = "", WarehouseCode = "";
                DateTime TransactionDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string QtyField = "quantity";
                string headerTemp =
"{14}\"_parameters\":[{14}\"billdata\":{14}\"discountamount\":{0}, \"CheckDataValidation\": 0,\"salesmanid\":{1},\"receiptid\":\"{2}\",\"iscash\":{3}," +
"\"dateMonth\":{4},\"customerid\":{5},\"billtype\":{6},\"dateYear\":{7},\"dateDay\":{8},\"dateHour\":{9},\"dateMinute\":{10},\"currencyid\":1,\"note\":\"{11}\",\"customerManipulateid\":\"{16}\",\"warehouseid\":{12}{15},\"billdetaildata\":[{13}]{15}]{15}";


                string detailsTemp = "{5}\"unitid\":{0},\"itemprice\":{1},\"itemid\":{2},\"discountvalue\":{3},\"mat_vatper\":{8},\"{7}\":{4}{6}";
                
                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND T.EmployeeID = " + Filters.EmployeeID;
                }
                string invoicesHeader = string.Format(@"SELECT   T.transactionid,   T.transactiondate ,  V.Barcode,e.Employeecode , C.CustomerCode, CO.CustomerCode OutletCode , t.Discount, t.Notes,case when TransactionTypeID in(1,3) then  e.HourlyRegularRate else e.HourlyOvertimeRate end SalesType
      ,t.TransactionTypeID,iif(isnull( t.CreationReason ,0)=11,1,isnull(t.SalesMode,co.customertypeid) )  SalesMode,(select count(*) from CustomerPayment cp where cp.TransactionID=t.TransactionID and cp.PaymentStatusID<>5 and cp.PaymentTypeID in(2,3) and t.SalesMode=1)  IsCredit 
FROM [Transaction] T 
                    INNER JOIN CustomerOutlet CO ON CO.CustomerID = T.CustomerID AND CO.OutletID = T.OutletID
                    INNER JOIN Customer  C  ON C .CustomerID = T.CustomerID 
                    INNER JOIN Organization O ON O.OrganizationID = T.OrganizationID
                    INNER JOIN Employee E ON E.EmployeeID = T.EmployeeID
					LEFT JOIN EmployeeVehicle ev on e.EmployeeID=ev.EmployeeID
				    LEFT JOIN Warehouse V ON V.WarehouseID = ev.VehicleID 
                    WHERE T.Synchronized = 0  
					  AND T.transactiondate >= '{0}' AND T.transactiondate < '{1}' 
                  {2}
                    AND T.transactiontypeid in (1,2,3,4) and t.Voided<>1 and posted=1 
            Order by T.transactionid /*and T.TransactionID = 'INV-VC11-0071-000040'*/"
                    , Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
                incubeQuery = new InCubeQuery(db_vms, invoicesHeader);

               
                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Transaction header query failed !!"));
                }



                DataTable dtInvoices = incubeQuery.GetDataTable();
                if (dtInvoices.Rows.Count == 0)
                    WriteMessage("There is no Transaction to send ..");
                else
                    SetProgressMax(dtInvoices.Rows.Count);

                for (int i = 0; i < dtInvoices.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();
                         TransactionID = dtInvoices.Rows[i]["Transactionid"].ToString();
                        ReportProgress("Sending Transaction: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(23, TransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Sales_S, new List<string>(filters.Values), processID, 60);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            res = Result.Duplicate;
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1 WHERE transactionid = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                                result.Append("Duplicate transaction [" + TransactionID + "]");
                        throw (new Exception("Transaction already sent  check table  Int_ExecutionDetails!!"));
                        }
                        else if(IsSentTransaction(TransactionID)==Result.Success)
                        {
                            res = Result.Duplicate;
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1 WHERE transactionid = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                            result.Append("Duplicate transaction [" + TransactionID + "]");
                            throw (new Exception("Transaction already sent and checked in API  check table  Int_ExecutionDetails!!"));
                        }



                        TransactionDate = Convert.ToDateTime(dtInvoices.Rows[i]["transactiondate"]);
                          SalesDocType = dtInvoices.Rows[i]["TransactionTypeID"].ToString();
                        WarehouseCode = dtInvoices.Rows[i]["Barcode"].ToString();
                        string Salesperson = dtInvoices.Rows[i]["Employeecode"].ToString();
                        SalesMode = dtInvoices.Rows[i]["SalesMode"].ToString();
                        SalesType = dtInvoices.Rows[i]["SalesType"].ToString();
                        Notes = dtInvoices.Rows[i]["Notes"].ToString();
                        OutletCode = dtInvoices.Rows[i]["OutletCode"].ToString();
                        CustomerCode = dtInvoices.Rows[i]["CustomerCode"].ToString();
                        IsCredit = dtInvoices.Rows[i]["IsCredit"].ToString();
                        Hdiscount = "0";// decimal.Parse(dtInvoices.Rows[i]["discount"].ToString()).ToString();//.ToString("F4");
                        if (SalesMode != "1")
                            SalesMode = "0";
                        if(/*SalesDocType == "2" || SalesDocType == "4" ||SalesDocType == "3" ||*/  IsCredit!="0")
                            SalesMode = "0";//Credit
                        

                        string invoiceDetails = string.Format(@"SELECT      
sum(transactiondetail.Quantity) Quantity,
transactiondetail.Price, 
transactiondetail.basePrice, 
sum(transactiondetail.Discount)  Discount,
convert(int,pack.Width) ItemCode,  
convert(int,pack.Height) UOMID,    
(case when sum(transactiondetail.Tax) >0 then item.PackDefinition else 0 end) Tax
,transactiondetail.salestransactiontypeid ItemType
FROM transactiondetail  INNER JOIN
Pack ON transactiondetail.PackID = Pack.PackID INNER JOIN
Item ON Pack.ItemID = Item.ItemID INNER JOIN 
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID
WHERE (PackTypeLanguage.LanguageID = 1) 
 AND (transactiondetail.TransactionID like '{0}')
group by 
transactiondetail.TransactionID,
transactiondetail.Price,   transactiondetail.basePrice, 
pack.Width, 
pack.Height,  item.PackDefinition,
 transactiondetail.salestransactiontypeid   
,transactiondetail.CustomerID,transactiondetail.OutletID
ORDER BY pack.Width
", TransactionID);
                        incubeQuery = new InCubeQuery(db_vms, invoiceDetails);
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            throw (new Exception("Transaction details query failed !!"));
                        }

                        DataTable dtDetails = incubeQuery.GetDataTable();
                        string allDetails = "";
                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        {
                            string ItemCode = "", UOM = "", Quantity = "", Price = "", BasePrice = "", Tax = "", discount = "", Type = "", bonus = "0";

                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            UOM = dtDetails.Rows[j]["UOMID"].ToString();
                            Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString( );//.ToString("#0.000");
                            Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString( );//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                            Tax = decimal.Parse(dtDetails.Rows[j]["Tax"].ToString()).ToString( );
                            discount = decimal.Parse(dtDetails.Rows[j]["discount"].ToString()).ToString( );
                            Type = dtDetails.Rows[j]["ItemType"].ToString();
                            Price = decimal.Parse(Price.ToString()).ToString( );
                            BasePrice = dtDetails.Rows[j]["basePrice"].ToString() ;
                            if (Type == "4"|| Type == "2")
                            {
                               // QtyField = "bonus";
                                QtyField = "quantity";
                                Price = BasePrice;
                                discount = (decimal.Parse(BasePrice) * decimal.Parse(Quantity)).ToString();

                            }
                            else
                                QtyField = "quantity";
                            allDetails +=  string.Format(detailsTemp,  UOM, Price,ItemCode, discount, Quantity ,"{","}", QtyField,Tax) +",";


                        }
                        allDetails = allDetails.Substring(0, allDetails.Length - 1) ;
                        string headerData = string.Format(headerTemp, Hdiscount, Salesperson,TransactionID, SalesMode, DateTime.Parse(TransactionDate.ToString()).Month,CustomerCode,SalesType, DateTime.Parse(TransactionDate.ToString()).Year, DateTime.Parse(TransactionDate.ToString()).Day
                          ,  DateTime.Parse(TransactionDate.ToString()).Hour, DateTime.Parse(TransactionDate.ToString()).Minute
                            ,Notes, WarehouseCode,allDetails, "{", "}",OutletCode);
                        ph_result[] result1 = Tools.GetRequest<ph_result>(CoreGeneral.Common.GeneralConfigurations.WS_URL+"/TPhenixApi/Bill", headerData, "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName,CoreGeneral.Common.GeneralConfigurations.WS_Password,"PUT",webHeader);
                        if (result1 == null || (result1[0].result != null && result1[0].result.Trim() != "success"))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (result1[0].result != null ? result1[0].value : "") + "\r\n json:" + headerData);
                            WriteMessage("Error .. \r\n" + result1[0].result+ " \r\n" + result1[0].value);

                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + result1[0].bill_tid + "\r\n Message:" + (result1[0].bill_tid != null ? result1[0].bill_tid : "") + "\r\n json:" + headerData);
                            WriteMessage("Success, ERP No: " + result1[0].bill_tid);
                            if ( IsCredit != "0")
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1,Description='" + result1[0].bill_id + "',SalesMode=2 WHERE TransactionID = '" + TransactionID + "'");
                            else
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1,Description='" + result1[0].bill_id + "' WHERE TransactionID = '" + TransactionID + "'");

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
                WriteMessage("Fetching transaction failed !!");
            }

        }

        public override void SendTransfers()
        {
            try
            {
                if (webHeader == null)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "Can't open session in API , please check the service!!"
                        , LoggingType.Error, LoggingFiles.InCubeLog);
                    WriteMessage("Can't open session in API , please check the service!!");
                }
                string salespersonFilter = "",  netTotal = "", SalesType = "";
                string    Notes = "", TransactionID = "", WarehouseCode = "";
                DateTime TransactionDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string QtyField = "quantity";
                string headerTemp ="{12}\"_parameters\":[{12}\"billdata\":{12} \"CheckDataValidation\": 0,\"salesmanid\":{0},\"receiptid\":\"{1}\" ," +
"\"dateMonth\":{2} {3},\"billtype\":{4},\"dateYear\":{5},\"dateDay\":{6},\"dateHour\":{7},\"dateMinute\":{8},\"currencyid\":1,\"note\":\"{9}\" ,\"warehouseid\":{10}{13},\"billdetaildata\":[{11}]{13}]{13}";


                string detailsTemp = "{5}\"unitid\":{0},\"itemprice\":{1},\"itemid\":{2},\"discountvalue\":{3},\"mat_vatper\":{8},\"{7}\":{4}{6}";

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND wt.RequestedBy = " + Filters.EmployeeID;
                }
                string invoicesHeader = string.Format(@"

					select wt.transactionid,wt.TransactionDate,e.EmployeeCode,w.Barcode ,wt.Notes,e.Phone TempletID
 from WarehouseTransaction wt inner join Organization o on wt.Organizationid=o.Organizationid
LEFT JOIN Warehouse w on wt.WarehouseID=W.WarehouseID 
LEFT JOIN Warehouse rw on wt.RefWarehouseID=RW.WarehouseID 
INNER JOIN Employee e on wt.RequestedBy=e.employeeid 
where wt.Synchronized<>1 and (
 (TransactionOperationID=1 and WarehouseTransactionStatusID in(1,2))) 
					  AND wt.transactiondate >= '{0}' AND wt.transactiondate < '{1}' 
                  {2}"
                    , Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
                incubeQuery = new InCubeQuery(db_vms, invoicesHeader);


                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Transaction header query failed !!"));
                }



                DataTable dtInvoices = incubeQuery.GetDataTable();
                if (dtInvoices.Rows.Count == 0)
                    WriteMessage("There is no Transaction to send ..");
                else
                    SetProgressMax(dtInvoices.Rows.Count);

                for (int i = 0; i < dtInvoices.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();
                        TransactionID = dtInvoices.Rows[i]["Transactionid"].ToString();
                        ReportProgress("Sending Transaction: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(IntegrationField.Transfers_S.GetHashCode(), TransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Transfers_S, new List<string>(filters.Values), processID, 60);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            res = Result.Duplicate;
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [WarehouseTransaction] SET Synchronized = 1 WHERE transactionid = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                            result.Append("Duplicate transaction [" + TransactionID + "]");
                            throw (new Exception("Transaction already sent  check table  Int_ExecutionDetails!!"));
                        }
                        else if (IsSentTransaction(TransactionID) == Result.Success)
                        {
                            res = Result.Duplicate;
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [WarehouseTransaction] SET Synchronized = 1 WHERE transactionid = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                            result.Append("Duplicate transaction [" + TransactionID + "]");
                            throw (new Exception("Transaction already sent and checked in API  check table  Int_ExecutionDetails!!"));
                        }

 
                        TransactionDate = Convert.ToDateTime(dtInvoices.Rows[i]["TransactionDate"]);
                 
                        WarehouseCode = dtInvoices.Rows[i]["Barcode"].ToString();
                        string Salesperson = dtInvoices.Rows[i]["Employeecode"].ToString(); 
                        SalesType = dtInvoices.Rows[i]["TempletID"].ToString();
                        Notes = dtInvoices.Rows[i]["Notes"].ToString(); 

                   

                        string invoiceDetails = string.Format(@"SELECT      
sum(WhTransDetail.Quantity) Quantity,
isnull(PriceDefinition.Price,0)Price,   
convert(int,pack.Width) ItemCode,  
convert(int,pack.Height) UOMID    
FROM WhTransDetail  INNER JOIN
Pack ON WhTransDetail.PackID = Pack.PackID INNER JOIN
Item ON Pack.ItemID = Item.ItemID INNER JOIN 
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID
left join PriceDefinition on WhTransDetail.PackID = PriceDefinition.PackID
and PriceDefinition.PriceListID in (select keyvalue from Configuration where KeyName='DefaultPriceListID')
WHERE (PackTypeLanguage.LanguageID = 1) 
 AND (WhTransDetail.TransactionID like '{0}')
group by 
WhTransDetail.TransactionID,
PriceDefinition.Price ,
pack.Width, 
pack.Height,  item.PackDefinition   
ORDER BY pack.Width
", TransactionID);
                        incubeQuery = new InCubeQuery(db_vms, invoiceDetails);
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            throw (new Exception("Transaction details query failed !!"));
                        }

                        DataTable dtDetails = incubeQuery.GetDataTable();
                        string allDetails = "";
                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        {
                            string ItemCode = "", UOM = "", Quantity = "", Price = "";

                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            UOM = dtDetails.Rows[j]["UOMID"].ToString();
                            Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString();//.ToString("#0.000");
                            Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString();//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                         
                            
                                QtyField = "quantity";
                            allDetails += string.Format(detailsTemp, UOM, Price, ItemCode, "0", Quantity, "{", "}", QtyField, "0") + ",";


                        }
                        allDetails = allDetails.Substring(0, allDetails.Length - 1);

                        string headerData = string.Format(headerTemp, Salesperson, TransactionID, DateTime.Parse(TransactionDate.ToString()).Month ,"", SalesType, DateTime.Parse(TransactionDate.ToString()).Year, DateTime.Parse(TransactionDate.ToString()).Day
                          , DateTime.Parse(TransactionDate.ToString()).Hour, DateTime.Parse(TransactionDate.ToString()).Minute
                            , Notes, WarehouseCode, allDetails, "{", "}");
                        ph_result[] result1 = Tools.GetRequest<ph_result>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/Bill", headerData, "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "PUT", webHeader);
                        if (result1 == null || (result1[0].result != null && result1[0].result.Trim() != "success"))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (result1[0].result != null ? result1[0].value : "") + "\r\n json:" + headerData);
                            WriteMessage("Error .. \r\n" + result1[0].result + " \r\n" + result1[0].value);

                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + result1[0].bill_tid + "\r\n Message:" + (result1[0].bill_tid != null ? result1[0].bill_tid : "") + "\r\n json:" + headerData);
                            WriteMessage("Success, ERP No: " + result1[0].bill_tid);
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [WarehouseTransaction] SET Synchronized = 1  WHERE TransactionID = '" + TransactionID + "'");
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
                WriteMessage("Fetching transaction failed !!");
            }

        }
    }
  
    class Categories
    {
        public string Class_ID { get; set; }
        public string Class_Father { get; set; }
        public string Display_Name { get; set; }
        public string Class_Name { get; set; }
        public string Class_Code { get; set; }
    }
    class Items
    {
        
        public string Material_id { get; set; }
        public string Material_code { get; set; }
        public string Material_BarCode { get; set; }
        public string Display_Name { get; set; }
        public string Material_Wieght { get; set; }
        public string Manufactured_BY { get; set; }
        public string Material_Class_ID { get; set; }
        public string Material_aname { get; set; }
        public string mat_vatper { get; set; }
        public Funits[] Funits { get; set; }
    }
    class Funits { 
        public string Ut_Name { get; set; }
        public string Ut_Equal { get; set; }

        public string Ut_Id { get; set; }

    }
    class AttCustomers
    {
        public string ClientBarcode { get; set; }
        public string Client_Address { get; set; }
        public string Client_city { get; set; }
        public string Client_country { get; set; }
        public string Client_fax { get; set; }
        public string Client_F_Name { get; set; }
        public string Client_Name { get; set; }
        public string Client_TEL { get; set; }
        public string Client_Latitude { get; set; }
        public string Client_Longitude { get; set; }
        public string client_CreditLimit { get; set; }
        public string Client_email { get; set; }
        public string Client_ID { get; set; }
        public string Client_Acc_ID { get; set; }
        public string Client_tel2 { get; set; }
        public string ID_GROUP { get; set; }
        public string Client_street { get; set; }
        public string Clientfollowup { get; set; }
        public string salesman_id  { get; set; }
        
        public Manipulate_List[] Manipulate_List { get; set; }



    }

    class CheckTrans
    {
        public string Receipt_no { get; set; }
        public string Interior_No { get; set; }
        
    }
    class Manipulate_List
    {
        [JsonProperty(PropertyName = "Manipulate_Name")]
        public string OutletName { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string OutletCode { get; set; }
    }
    class Salesperson
    {
        public string del_Acc_ID { get; set; }
        public string Del_ID { get; set; }
        public string del_Name { get; set; }
    }
    class CustomerGroup
    {
        [JsonProperty(PropertyName = "ID")]
        public string groupID { get; set; }
        public string grpname  { get; set; }
    }
    class AttWarehouse
    {
        public string Storage_ID { get; set; }
        public string storage_code { get; set; }
        public string Storage_Keeper { get; set; }
        public string Storage_Name { get; set; }

    }
    class WarehouseStock
    {
        public string Code { get; set; }
        public string Current_balance { get; set; }
        public string Storage_ID { get; set; }
        public string Unit { get; set; }
        public string WarehouseID { get; set; }
        public string PackId { get; set; }



    }
    class Prices
    {
        public string Code { get; set; }
        public decimal Distribution { get; set; }
        public decimal Export { get; set; }
        public decimal Purchase { get; set; }
        public decimal Retail { get; set; }
        public decimal Sale { get; set; }
        public string Unit { get; set; }
        public decimal whole { get; set; }



    }
    class CustomerPrices
    {
        public string client_id { get; set; }
        public decimal mat_id { get; set; }
        public decimal price { get; set; }
        public decimal unit_id { get; set; }  
    }
    class Invoices
    {
        public string bill_id { get; set; }
        public string Bill_no { get; set; }
        public string client_id { get; set; }
        public string ClientManipulateId { get; set; }
        string _Date = "";
        public string Date {
            get { return _Date; }
            set
            {
                if (value.Split('/').Count() == 3)
                    _Date = value.Split('/')[2] + value.Split('/')[1].PadLeft(2, '0') + value.Split('/')[0].PadLeft(2, '0');
                else
                    _Date = value;
            }
        }
        public string del_id { get; set; }
        public string Receipt_no { get; set; }
        public decimal Rest { get; set; }
       

    }
    class CustomerBalance
    {
        public string client_id { get; set; }
        public decimal Debit_balance { get; set; }
        public decimal Credit_balance { get; set; }
        public decimal Max_limit { get; set; }
 
    }

    class ph_result
    {
        public string result { get; set; }
        public string session { get; set; }
        public string value { get; set; }
        public string bill_id { get; set; }
        public string bill_tid { get; set; }
    }
   
}