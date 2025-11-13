
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
using System.Windows.Forms;

namespace InCubeIntegration_BL
{

    public class IntegrationViewWater : IntegrationBase, IDisposable// Live branch
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
        ~IntegrationViewWater()
        {

            CloseSession();

        }
        protected virtual void Dispose(bool disposing)
        {
            CloseSession();
        }
        public IntegrationViewWater(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            if (CoreGeneral.Common.CurrentSession.loginType != LoginType.WindowsService)
            {
                db_res = new InCubeDatabase();
                db_res.Open("InCube", "IntegrationViewWater");
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
        public override void UpdateCustomer()
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

        public override void UpdatePrice()
        {
            GetMasterData(IntegrationField.Price_U);
        }
        public override void UpdateMainWarehouse()
        {
            GetMasterData(IntegrationField.Warehouse_U);
        }
        /* public override void ImportNewCustomer()
         {
             GetMasterData(IntegrationField.ImportNewCustomer_U);
         }*/

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
                        ProcName = "SP_UpdateItems";
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
                        /*case IntegrationField.ImportNewCustomer_U:
                            res = GetNewCustomerTable(ref dtMasterData);
                            StagingTable = "Stg_Customers";
                            ProcName = "sp_UpdateCustomers";
                            break;*/
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
                CloseSession();
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
                            columns += "NVARCHAR(2000)";
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

                /* DT = Tools.GetRequestTable<Categories>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/ClassGetalllist", "", "classes", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);

                 if (DT != null && DT.Rows.Count > 0) res = SaveTable(DT, "stg_Categories");*/

                // DT = Tools.GetRequestTable<Items>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/ItemsGetAllList/0", "", "items", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);
                DT = Tools.GetRequestTable<ViewWaterItems>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "Items?$select=ItemCode,ItemName,ForeignName,SalesUnit,DefaultWarehouse ,ItemsGroupCode&$filter=ItemsGroupCode eq 103",
                    "", "value", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);

                // DT = Tools.GetRequestTable<ViewWaterSalesperson>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "SalesPersons?$select=SalesEmployeeCode,SalesEmployeeName", "", "value", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);
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


                /*string sessionID = Tools.TestMouath2("VIEW_TEST", "0000", "Sonic");
                if (sessionID != null && sessionID.Length > 0)
                {
                    //_Session = o[0].SessionId;
                    webHeader = new WebHeaderCollection();
                    webHeader.Add("Cookie", "B1SESSION="+sessionID+"; ROUTEID=.node1");
                }*/
                view_result[] o = Tools.GetRequestNew<view_result>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "Login",
                     " {\"CompanyDB\": \"VIEW_TEST\",\"Password\": \"0000\",\"UserName\": \"Sonic\"}", "", "", "", "", "", "POST", webHeader);
                //Tools2.CallPostFunction("https://b1dev.wisys.com.sa:50000/b1s/v1/Logout", " {\"CompanyDB\": \"VIEW_TEST\",\"Password\": \"0000\",\"UserName\": \"Sonic\"}", "", "", ref x);
                if (o != null && o.Length > 0)
                {
                    _Session = o[0].SessionId;
                    webHeader = new WebHeaderCollection();
                    Cookie x = new Cookie("B1SESSION", _Session);
                    //webHeader.Add(Cookie)
                    webHeader.Add("B1SESSION", _Session);// ("Cookie", "B1SESSION=" + _Session + "; ROUTEID=.node10");
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
                object[] o = Tools.GetRequest<object>(/*CoreGeneral.Common.GeneralConfigurations.WS_URL + */"https://b1dev.wisys.com.sa:50000/b1s/v1/Logout",
                   "", "", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);
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
                // List<CustomerGroup[]> x = (Tools.GetRequest<CustomerGroup[]>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/find/33/", "", "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader)).OfType<CustomerGroup[]>().ToList();
                /* List<ViewWaterCustomerGroup[]> x = (Tools.GetRequest<ViewWaterCustomerGroup[]>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "BusinessPartnerGroups?$select=Code,Name,Type", "", "value", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader)).OfType<ViewWaterCustomerGroup[]>().ToList();

                 DT = Tools.ToDataTable<ViewWaterCustomerGroup>(x[0]);*/

                //  DT = Tools.GetRequestTable<AttCustomers>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/CustomerGetAllList/0/1", "", "clients", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);
                DT = Tools.GetRequestTable<ViewWaterCustomerGroup>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "BusinessPartnerGroups?$select=Code,Name,Type", "", "value", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);
                if (DT != null && DT.Rows.Count > 0) res = SaveTable(DT, "stg_CustomerGroup");
                //#region [Test API]
                //string testString = System.IO.File.ReadAllText(@"D:\Text.txt");
                //AttCustomers[] test = Tools.GetJsonData<AttCustomers[]>("", testString);

                //#endregion
                //DT = Tools.ToDataTable<AttCustomers>(test);
                if (DT != null && DT.Rows.Count > 0)
                {
                    DT.Columns.Remove("PaymentCondition");
                    res = Result.Success;
                }
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
        private Result GetNewCustomerTable(ref DataTable DT)
        {
            if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
            Result res = Result.Failure;
            try
            {

                DT = new DataTable();
                List<CustomerGroup[]> x = (Tools.GetRequest<CustomerGroup[]>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/find/33/", "", "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader)).OfType<CustomerGroup[]>().ToList();

                DT = Tools.ToDataTable<CustomerGroup>(x[0]);
                if (DT != null && DT.Rows.Count > 0) res = SaveTable(DT, "stg_CustomerGroup");


                /*DataTable dt1 = new DataTable();
                dt1 = Tools.GetRequestTable<Max_triangle>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/CustomerGetnewlist/0/0", "", "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);
                string Max_triangle1 = dt1.Rows[0][0].ToString();

                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/CustomerGetnewlist/" + Max_triangle1.ToString() + "/0" + "\r\n\r\n" + dt1.Rows[0][0].ToString(), LoggingType.Error, LoggingFiles.InCubeLog);
                Max_triangle1 = "67379";
                DT = Tools.GetRequestTable<AttCustomers>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/CustomerGetnewlist/"+Max_triangle1.ToString()+"/0/1", "", "clients", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);
                */
                //#region [Test API]
                //string testString = System.IO.File.ReadAllText(@"D:\Text.txt");
                //AttCustomers[] test = Tools.GetJsonData<AttCustomers[]>("", testString);

                //#endregion
                //DT = Tools.ToDataTable<AttCustomers>(test);
                if (DT != null && DT.Rows.Count > 0)
                {
                    DT.Columns.Remove("PaymentCondition");
                    res = Result.Success;
                }
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
                DT = Tools.GetRequestTable<CustomerBalance>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/\"Report\"/187", CustomerBalanceBody, "DATA", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);

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
                DT = Tools.GetRequestTable<ViewWaterSalesperson>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "SalesPersons?$select=SalesEmployeeCode,SalesEmployeeName", "", "value", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);
                //DT = Tools.GetRequestTable<ViewWaterSalesperson>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/b1s/v1/SalesPersons?$select=SalesEmployeeCode,SalesEmployeeName","", "value","test.crt" ,"","GET", webHeader);

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
                List<AttWarehouse[]> x = (Tools.GetRequest<AttWarehouse[]>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/find/8/", "", "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader)).OfType<AttWarehouse[]>().ToList();

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
                DT = Tools.GetRequestTable<Invoices>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/\"Report\"/859", GetInvoicesBody, "DATA", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);


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
               // DT = Tools.GetRequestTable<WarehouseStock>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/\"Report\"/13", stockBody, "DATA", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);
               // DT = Tools.GetRequestTable<WarehouseStock>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/\"Report\"/13", stockBody, "DATA", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);
                DT = Tools.GetRequestTable<Stock>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "$crossjoin(Items,Items/ItemWarehouseInfoCollection,BatchNumberDetails)?$expand=Items($select=ItemCode,ItemName,ForeignName,SalesUnit),Items/ItemWarehouseInfoCollection($select=WarehouseCode,InStock),BatchNumberDetails($select=Batch)&$filter=Items/ItemCode eq Items/ItemWarehouseInfoCollection/ItemCode and Items/ItemCode eq BatchNumberDetails/ItemCode and Items/ItemsGroupCode eq 103", "", "value", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);


                if (DT != null && DT.Rows.Count > 0)
                {
                    DT.Columns.RemoveAt(0);
                    DT.Columns.RemoveAt(0);
                    DT.Columns.RemoveAt(0);
                    res = Result.Success;
                }
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
        string PriceBody1 = "{\"_parameters\":[{\"controls\": [{\"Vyear\": 0,\"type\": 9,\"name\": \"DTP_FROM\",\"Vday\": 0,\"Vmonth\": 0},{\"Vyear\": 0,\"type\": 9,\"name\": \"DTP_TO\",\"Vday\": 0,\"Vmonth\": 0},{\"items\": [{\"value\": 0,\"name\": \"a\"},{\"value\": 1,\"name\": \"b\"},{\"value\": 2,\"name\": \"c\"},{\"value\": 3,\"name\": \"d\"},{\"value\": 4,\"name\": \"e\"},{\"value\": 5,\"name\": \"f\"},{\"value\": 6,\"name\": \"j\"},{\"value\": 7,\"name\": \"h\"},{\"value\": 8,\"name\": \"i\"},{\"value\": 9,\"name\": \"s\"}],\"value\": \"9\",\"type\": 2,\"name\": \"FRM_PERIOD\"},{\"limit\": 0,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRAM_MAT\"},{\"limit\": 0,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"F_CLASS\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRAM_STORE\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"F_CLIENT\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRM_BRANSH\"},{\"items\": [{\"value\": 1,\"name\": \"dd\"},{\"value\": 50000,\"name\": \"ff\"},{\"value\": 55000,\"name\": \"ss\"},{\"value\": 2,\"name\": \"الواحدة 2\"},{\"value\": 3,\"name\": \"الواحدة 3\"}],\"value\": \"3\",\"type\": 2,\"name\": \"CB_UT\"}]}]}";
        string PriceBody2 = "{\"_parameters\":[{\"controls\": [{\"Vyear\": 0,\"type\": 9,\"name\": \"DTP_FROM\",\"Vday\": 0,\"Vmonth\": 0},{\"Vyear\": 0,\"type\": 9,\"name\": \"DTP_TO\",\"Vday\": 0,\"Vmonth\": 0},{\"items\": [{\"value\": 0,\"name\": \"a\"},{\"value\": 1,\"name\": \"b\"},{\"value\": 2,\"name\": \"c\"},{\"value\": 3,\"name\": \"d\"},{\"value\": 4,\"name\": \"e\"},{\"value\": 5,\"name\": \"f\"},{\"value\": 6,\"name\": \"j\"},{\"value\": 7,\"name\": \"h\"},{\"value\": 8,\"name\": \"i\"},{\"value\": 9,\"name\": \"s\"}],\"value\": \"9\",\"type\": 2,\"name\": \"FRM_PERIOD\"},{\"limit\": 0,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRAM_MAT\"},{\"limit\": 0,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"F_CLASS\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRAM_STORE\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"F_CLIENT\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRM_BRANSH\"},{\"items\": [{\"value\": 1,\"name\": \"dd\"},{\"value\": 50000,\"name\": \"ff\"},{\"value\": 55000,\"name\": \"ss\"},{\"value\": 2,\"name\": \"الواحدة 2\"},{\"value\": 3,\"name\": \"الواحدة 3\"}],\"value\": \"3\",\"type\": 3,\"name\": \"CB_UT\"}]}]}";
        string CustomerBalanceBody = "{\"_parameters\":[ {    \"controls\": [     {      \"Vyear\": 2019,      \"type\": 9,      \"name\": \"DFROM\",      \"Vday\": 1,      \"Vmonth\": 1     },     {      \"Vyear\": 2019,      \"type\": 9,      \"name\": \"DTO\",      \"Vday\": 22,      \"Vmonth\": 5     },     {      \"items\": [       {        \"value\": 0,        \"name\": \"اليوم\"       },       {        \"value\": 1,        \"name\": \"اليوم السابق\"       },       {        \"value\": 2,        \"name\": \"هذا الشهر\"       },       {        \"value\": 3,        \"name\": \"هذا الشهر حتى اليوم\"       },       {        \"value\": 4,        \"name\": \"هذا الربع من السنة\"       },       {        \"value\": 5,        \"name\": \"هذا الربع من السنة حتى اليوم\"       },       {        \"value\": 6,        \"name\": \"هذه السنة\"       },       {        \"value\": 7,        \"name\": \"هذه السنة حتى اليوم\"       },       {        \"value\": 8,        \"name\": \"بداية السنة المحاسبية\"       },       {        \"value\": 9,        \"name\": \"بدون\"       }      ],      \"value\": \"8\",      \"type\": 2,      \"name\": \"FRM_PERIOD\"     },     {      \"limit\": 1,      \"items\": [],      \"value\": \"\",      \"type\": 1,      \"name\": \"FRM_ACC\"     },     {      \"limit\": 1,      \"items\": [],      \"value\": \"\",      \"type\": 1,      \"name\": \"FRM_CLIENT\"     },     {      \"limit\": 1,      \"items\": [],      \"value\": \"\",      \"type\": 1,      \"name\": \"FRM_CUSTGRP\"     },     {      \"limit\": 1,      \"items\": [       {        \"value\": \"ل.س\",        \"name\": \"1\"       }      ],      \"value\": \"\",      \"type\": 1,      \"name\": \"FRM_CURRENCY\"     },     {      \"limit\": 1,      \"items\": [],      \"value\": \"\",      \"type\": 1,      \"name\": \"FRM_BRANSH\"     }    ]   } ]}";
        // string GetInvoicesBody = "{\"_parameters\":[{\"controls\": [{\"Vyear\": 2019,\"type\": 9,\"name\": \"DTP_FROM\",\"Vday\": 1,\"Vmonth\": 1},{\"Vyear\": 2019,\"type\": 9,\"name\": \"DTP_TO\",\"Vday\": 10,\"Vmonth\": 7},{\"items\": [{\"value\": 0,\"name\": \"اليوم\"},{\"value\": 1,\"name\": \"اليوم السابق\"},{\"value\": 2,\"name\": \"هذا الشهر\"},{\"value\": 3,\"name\": \"هذا الشهر حتى اليوم\"},{\"value\": 4,\"name\": \"هذا الربع من السنة\"},{\"value\": 5,\"name\": \"هذا الربع من السنة حتى اليوم\"},{\"value\": 6,\"name\": \"هذه السنة\"},{\"value\": 7,\"name\": \"هذه السنة حتى اليوم\"},{\"value\": 8,\"name\": \"بداية السنة المحاسبية\"},{\"value\": 9,\"name\": \"بدون\"}],\"value\": \"8\",\"type\": 2,\"name\": \"FRM_PERIOD\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"F_CLIENT\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRAM_CLIENTGROUP\"},{\"items\": [{\"value\": 0,\"name\": \"بدون تحديد\"},{\"value\": 1,\"name\": \"قبض\"},{\"value\": 2,\"name\": \"دفع\"}],\"value\": \"1\",\"type\": 2,\"name\": \"CB_PAYTYPE\"},{\"items\": [{\"value\": 0,\"name\": \"الكل\"},{\"value\": 1,\"name\": \"غير محصل\"},{\"value\": 2,\"name\": \"محصل\"}],\"value\": \"1\",\"type\": 2,\"name\": \"CB_PAIDTYPE\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRM_BRANSH\"},{\"value\": \"0\",\"type\": 6,\"name\": \"SUINUMBEREDIT2\"},{\"items\": [{\"selected\": 1,\"value\": 1,\"name\": \"الفاتورة\"},{\"selected\": 0,\"value\": 2,\"name\": \"نمط الفاتورة\"},{\"selected\": 0,\"value\": 3,\"name\": \"العميل\"},{\"selected\": 0,\"value\": 4,\"name\": \"التاريخ\"},{\"selected\": 0,\"value\": 6,\"name\": \"المندوب\"},{\"selected\": 0,\"value\": 7,\"name\": \"مركز الكلفة\"}],\"value\": \"1\",\"type\": 3,\"name\": \"CH_GROUPBY\"}]}]}";
        string GetInvoicesBody = "{\"_parameters\":[{\"controls\": [{\"Vyear\": 2019,\"type\": 9,\"name\": \"DTP_FROM\",\"Vday\": 1,\"Vmonth\": 1},{\"Vyear\": 2022,\"type\": 9,\"name\": \"DTP_TO\",\"Vday\": 31,\"Vmonth\": 12},{\"items\": [{\"value\": 0,\"name\": \"اليوم\"},{\"value\": 1,\"name\": \"اليوم السابق\"},{\"value\": 2,\"name\": \"هذا الشهر\"},{\"value\": 3,\"name\": \"هذا الشهر حتى اليوم\"},{\"value\": 4,\"name\": \"هذا الربع من السنة\"},{\"value\": 5,\"name\": \"هذا الربع من السنة حتى اليوم\"},{\"value\": 6,\"name\": \"هذه السنة\"},{\"value\": 7,\"name\": \"هذه السنة حتى اليوم\"},{\"value\": 8,\"name\": \"بداية السنة المحاسبية\"},{\"value\": 9,\"name\": \"بدون\"}],\"value\": \"9\",\"type\": 2,\"name\": \"FRM_PERIOD\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"F_CLIENT\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRAM_CLIENTGROUP\"},{\"items\": [{\"value\": 0,\"name\": \"بدون تحديد\"},{\"value\": 1,\"name\": \"قبض\"},{\"value\": 2,\"name\": \"دفع\"}],\"value\": \"1\",\"type\": 2,\"name\": \"CB_PAYTYPE\"},{\"items\": [{\"value\": 0,\"name\": \"الكل\"},{\"value\": 1,\"name\": \"غير محصل\"},{\"value\": 2,\"name\": \"محصل\"}],\"value\": \"1\",\"type\": 2,\"name\": \"CB_PAIDTYPE\"},{\"limit\": 1,\"items\": [],\"value\": \"\",\"type\": 1,\"name\": \"FRM_BRANSH\"},{\"value\": \"0\",\"type\": 6,\"name\": \"SUINUMBEREDIT2\"},{\"items\": [{\"selected\": 1,\"value\": 1,\"name\": \"الفاتورة\"},{\"selected\": 0,\"value\": 2,\"name\": \"نمط الفاتورة\"},{\"selected\": 0,\"value\": 3,\"name\": \"العميل\"},{\"selected\": 0,\"value\": 4,\"name\": \"التاريخ\"},{\"selected\": 0,\"value\": 6,\"name\": \"المندوب\"},{\"selected\": 0,\"value\": 7,\"name\": \"مركز الكلفة\"}],\"value\": \"1\",\"type\": 3,\"name\": \"CH_GROUPBY\"}]}]}";

        private Result GetPricesTable(ref DataTable DT)
        {
            if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
            Result res = Result.Failure;
            try
            {
                /*List<CustomerPrices[]> x = (Tools.GetRequest<CustomerPrices[]>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/CustomerGetCustomisedPrices/0/0", "", "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader)).OfType<CustomerPrices[]>().ToList();

                DT = Tools.ToDataTable<CustomerPrices>(x[0]);
                SaveTable(DT, "Stg_CustomerPrices");*/



                /*DataTable DT1 = new DataTable();
                DT1 = Tools.GetRequestTable<Prices>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/\"Report\"/710", PriceBody1, "DATA", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);
                SaveTable(DT1, "Stg_Prices1");

                DataTable DT2 = new DataTable();
                DT1 = Tools.GetRequestTable<Prices>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/\"Report\"/710", PriceBody2, "DATA", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);
                SaveTable(DT2, "Stg_Prices2");*/

                //  DT = Tools.GetRequestTable<Prices>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/\"Report\"/710", PriceBody, "DATA", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);

                DT = Tools.GetRequestTable<ViewWaterPrices>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "sml.svc/UnitPrice", "", "value", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);

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

                CheckTrans[] DT = Tools.GetRequest<CheckTrans>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/\"Report\"/42", body, "DATA", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);

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
                string body = "{\"_parameters\":[{\"controls\":[ { \"value\": \"8\", \"type\": 2, \"name\": \"FRM_PERIOD\" }, { \"value\": \"" + TransactionId + "\", \"type\": 4, \"name\": \"EDT_BNRID\" } ]}]} ";

                CheckTrans[] DT = Tools.GetRequest<CheckTrans>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/\"Report\"/302", body, "DATA", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);

                if (DT != null && DT.Count() > 0)
                    if (DT.Length > 0)
                    {
                        if (DT[0].Receipt_no.ToString() != "" && DT[0].Receipt_no.ToString() != null)
                        {
                            res = Result.Success;
                        }

                        else
                            res = Result.NoRowsFound;

                    } else
                        res = Result.NoRowsFound;
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


                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name,
    System.Reflection.MethodBase.GetCurrentMethod().Name, CoreGeneral.Common.GeneralConfigurations.WS_URL.ToString() + "/TPhenixApi/\"Report\"/488 " + body.ToString(), LoggingType.Error, LoggingFiles.InCubeLog);


                CheckTrans[] DT = Tools.GetRequest<CheckTrans>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/\"Report\"/488", body, "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);
                string reciptNo = "";

                if (DT != null && DT.Count() > 0)
                    if (DT.Length > 0)
                    {
                        reciptNo = DT[0].ToString();
                        if (DT[0].Receipt_no.ToString() != "" && DT[0].Receipt_no.ToString() != null)
                        {
                            res = Result.Success;
                        }

                        else
                            res = Result.NoRowsFound;

                    }
                    else
                        res = Result.NoRowsFound;
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
        /* public override void SendReciepts()
         {
             try
             {
                 if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
                 string Type = "", Notes = "", CustomerPaymentID = "", EmployeeCode = "", ERPInvoiceNo = "", AccNo = "", AppliedAmount = "", CustomerCode = "", OutletCode = "", VoucherDate = "", VoucherNumber = "", branch = "";

                 DateTime PaymentDate, VoucherDateTime;
                 DateTime cheeqDate;
                 int OrgID = 0, processID = 0;
                 StringBuilder result = new StringBuilder();
                 Result res = Result.UnKnown;
                 DataTable invoices = null;

                 string CashTemp = "{11}\"_parameters\": [{11}\"billid\":{0},\"bondtypeid\":{1}, \"dateMonth\":{2} ,\"dateYear\":{3},\"dateDay\":{4} ,\"billpayment\":{5},\"dateHour\":{6},\"dateMinute\":{7} ,\"delid\":{8},\"note\":\"{9}\" ,\"pn_receiptnumber\":\"{10}\" {12}]{12}";
                 //string CheeqTemp = "{13}\"_parameters\": [{13}\"pn_ClientId\":{0},\"pn_customer_manipulateid\":{1},\"pn_typeid\":{2},\"pn_fto_accid\":0,\"dateDay\":{3},\"dateYear\":{4},\"dateMonth\":{5},\"pn_value\":{6},\"pn_curid\":1,\"pn_curequal\":1,\"pn_number\":\"{7}\" ,\"matdateDay\":{8},\"matdateYear\":{9},\"matdateMonth\":{10},\"pn_note\":\"{11}\",\"pn_delid\":\"{12}\",\"pn_receiptnumber\":\"{15}\" {14}]{14}";
                 //string CheeqTemp = "{13}\"_parameters\": [{13} \"paymentdata\":{13}\"customerid\":{0},\"customerManipulateid\":{1},\"bondtypeid\":1,\"paymentaccount\":{2} ,\"dateDay\":{3},\"dateYear\":{4},\"dateMonth\":{5},\"value\":{6},\"currency\":1,\"currencyequal\":1,\"note\":\"{11}\",\"delid\":\"{12}\",\"pn_receiptnumber\":\"{15}\"{14},\"papernotes\":[{13} \"ccid\":0,\"pn_receiptnumber\":\"{15}\",\"pnnumber\":\"{7}\"  ,\"dateDay\":{3},\"dateYear\":{4},\"dateMonth\":{5},\"value\":{6},\"matdateDay\":{8},\"matdateYear\":{9},\"matdateMonth\":{10},\"note\":\"{11}\"{14}] , \"billspayments\":{16} {14}]{14}";
                 string CheeqTemp = "{10}\"_parameters\": [{10} \"billid\":{0},\"matdateMonth\":{1},\"pntypeid\":{2} ,\"matdateYear\":{3} ,\"matdateDay\":{4},\"billpayment\":{5},\"pnnumber\":{6},\"pn_receiptnumber\":\"{7}\",\"note\":\"{8}\",\"delid\":\"{9}\"{11}]{11}";
                 string DownpaymentTemp = "{10}\"_parameters\": [{10}\"customerid\":{0},\"customerManipulateid\":{1},\"bondtypeid\":{2},\"paymentaccount\":0,\"dateDay\":{3},\"dateYear\":{4},\"dateMonth\":{5},\"value\":{6},\"currency\":1,\"currencyequal\":1,\"delid\":{7},\"note\":\"{8}\" ,\"pn_receiptnumber\":\"{9}\"{11}]{11}";
                 string salespersonFilter = "";


                 // body = string.Format(CashTemp, ERPInvoiceNo, AccNo, PaymentDate.Month, PaymentDate.Year, PaymentDate.Day, AppliedAmount, PaymentDate.Hour, PaymentDate.Minute, EmployeeCode, Notes, CustomerPaymentID, "{", "}");
                 // body = string.Format(CheeqTemp, CustomerCode, OutletCode, AccNo, PaymentDate.Day, PaymentDate.Year, PaymentDate.Month, AppliedAmount, VoucherNumber, cheeqDate.Day, cheeqDate.Year, cheeqDate.Month, Notes, EmployeeCode, "{", "}", CustomerPaymentID);

                 if (Filters.EmployeeID != -1)
                 {
                     salespersonFilter = "AND CP.EmployeeID = " + Filters.EmployeeID;
                 }
                 string invoicesHeader = string.Format(@"SELECT 1 Type,    cp.AppliedPaymentID CustomerPaymentID,e.EmployeeCode, CP.PaymentDate,t.Description ERPInvoiceNo,e.MinHours CashAccNo ,  sum(AppliedAmount)AppliedAmount  ,c.CustomerCode,o.CustomerCode OutletCode,cp.VoucherDate,cp.VoucherNumber,b.Code bank, Bl.description branch
                                       ,cp.Notes 
                                       FROM CustomerPayment CP INNER join [transaction] t on cp.TransactionID=t.TransactionID and (t.salesmode=2/* or t.transactiontypeid =3)
 and cp.CustomerID = t.customerid and cp.OutletID = t.OutletID
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

 SELECT 2 Type,  CP.CustomerPaymentID,e.EmployeeCode, CP.PaymentDate,t.description  ERPInvoiceNo,e.MaxOTHours CashAccNo ,  sum(AppliedAmount)AppliedAmount  ,c.CustomerCode,o.CustomerCode  OutletCode,cp.VoucherDate,cp.VoucherNumber,b.Code bank, Bl.description branch
                                       ,cp.Notes 
                                       FROM CustomerPayment CP  
                                       inner join [transaction] t on cp.transactionid = t.transactionid
                                             INNER join Customer c on c.CustomerID=cp.CustomerID  
                                             INNER join CustomerOutlet  o on o.CustomerID=cp.CustomerID and  o.OutletID=cp.OutletID  
                                             INNER join Employee e on cp.EmployeeID=e.EmployeeID
                                               Left Join Bank B on CP.BankID=B.BankID  
                                                 Left Join BankBranchLanguage Bl on CP.BankID=Bl.BankID and CP.BranchID=Bl.BranchID and Bl.LanguageID=1
                                                 WHERE       CP.PaymentStatusID <> 5 AND CP.PaymentTypeID IN (2,3) and cp.Synchronized=0  
                          {6}
                         AND					    CP.PaymentDate >= CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102) 
                                                   AND CP.PaymentDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102)
 group by   CP.CustomerPaymentID,e.EmployeeCode,e.MaxOTHours,c.CustomerCode ,CP.PaymentDate,bl.Description, cp.ExchangeRate  ,O.BlockNumber ,o.CustomerCode   ,cp.VoucherDate,cp.VoucherNumber,b.Code  ,cp.Notes ,t.Description

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
                                                 WHERE       isnull(CP.voided,0) <> 1 AND CP.PaymentTypeID IN (1) and cp.Synchronised=0  
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

                 string VarBody="";
                 string Varlink="";
                 for (int i = 0; i < dtInvoices.Rows.Count; i++)
                 {
                     try
                     {
                         res = Result.UnKnown;
                         processID = 0;
                         result = new StringBuilder();
                         CustomerPaymentID = dtInvoices.Rows[i]["CustomerPaymentID"].ToString();
                         string InvoiceNo = dtInvoices.Rows[i]["ERPInvoiceNo"].ToString();
                         Type = dtInvoices.Rows[i]["Type"].ToString();
                         ReportProgress("Sending Payment: " + CustomerPaymentID);
                         WriteMessage("\r\n" + CustomerPaymentID + ": ");
                         Dictionary<int, string> filters = new Dictionary<int, string>();
                         if (Type == "1" || Type == "2")
                         {
                             filters.Add(12, CustomerPaymentID + '-' + InvoiceNo);
                         }
                         else
                         {
                             filters.Add(12, CustomerPaymentID);
                         }
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
                         //else
                         //{
                         //    switch (Type.Trim())
                         //    {
                         //        case "1":
                         //            if (IsSentCash(CustomerPaymentID) == Result.Success)
                         //            {
                         //                res = Result.Duplicate;
                         //                incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerPayment] SET Synchronized = 1 WHERE CustomerPaymentID = '" + CustomerPaymentID + "'");
                         //                incubeQuery.ExecuteNonQuery();
                         //                result.Append("Duplicate Payment [" + CustomerPaymentID + "]");
                         //                throw (new Exception("Payment already sent and checked in API  check table  Int_ExecutionDetails!!"));
                         //            }
                         //            break;

                         //        case "2":
                         //            if (IsSentCheeq(CustomerPaymentID) == Result.Success)
                         //            {
                         //                res = Result.Duplicate;
                         //                incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerPayment] SET Synchronized = 1 WHERE CustomerPaymentID = '" + CustomerPaymentID + "'");
                         //                incubeQuery.ExecuteNonQuery();
                         //                result.Append("Duplicate Payment [" + CustomerPaymentID + "]");
                         //                throw (new Exception("Payment already sent and checked in API  check table  Int_ExecutionDetails!!"));
                         //            }
                         //            break;
                         //        case "3":
                         //            if (IsSentCash(CustomerPaymentID) == Result.Success)
                         //            {
                         //                res = Result.Duplicate;
                         //                incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerUnallocatedPayment] SET Synchronised = 1 WHERE CustomerPaymentID = '" + CustomerPaymentID + "'");
                         //                incubeQuery.ExecuteNonQuery();
                         //                result.Append("Duplicate Payment [" + CustomerPaymentID + "]");
                         //                throw (new Exception("Payment already sent and checked in API  check table  Int_ExecutionDetails!!"));
                         //            }
                         //            break;
                         //        case "4":
                         //            if (IsSentCheeq(CustomerPaymentID) == Result.Success)
                         //            {
                         //                res = Result.Duplicate;
                         //                incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerUnallocatedPayment] SET Synchronised = 1 WHERE CustomerPaymentID = '" + CustomerPaymentID + "'");
                         //                incubeQuery.ExecuteNonQuery();
                         //                result.Append("Duplicate Payment [" + CustomerPaymentID + "]");
                         //                throw (new Exception("Payment already sent and checked in API  check table  Int_ExecutionDetails!!"));
                         //            }
                         //            break;
                         //    }

                         //}



                         PaymentDate = Convert.ToDateTime(dtInvoices.Rows[i]["PaymentDate"]);
                         Notes = dtInvoices.Rows[i]["Notes"].ToString();
                         EmployeeCode = dtInvoices.Rows[i]["EmployeeCode"].ToString();
                         ERPInvoiceNo = dtInvoices.Rows[i]["ERPInvoiceNo"].ToString();
                         AccNo = dtInvoices.Rows[i]["CashAccNo"].ToString();
                         AppliedAmount = dtInvoices.Rows[i]["AppliedAmount"].ToString();
                         CustomerCode = dtInvoices.Rows[i]["CustomerCode"].ToString();
                         OutletCode = dtInvoices.Rows[i]["OutletCode"].ToString();
                         VoucherDate = dtInvoices.Rows[i]["VoucherDate"].ToString();
                         if(VoucherDate.ToString() != "" && VoucherDate.ToString() != string.Empty)
                         {
                              VoucherDateTime = Convert.ToDateTime(dtInvoices.Rows[i]["VoucherDate"]);
                         }else
                         {
                             VoucherDateTime = Convert.ToDateTime(dtInvoices.Rows[i]["PaymentDate"]);
                         }

                         VoucherNumber = dtInvoices.Rows[i]["VoucherNumber"].ToString();
                         branch = dtInvoices.Rows[i]["branch"].ToString();

                         string cheeqInfo = "";
                         cheeqDate = new DateTime(1900, 1, 1);
                         if (Type == "4" || Type == "2")
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
                             case "4": // PDC & CDC downpayment
                                       //                                Link = CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/PaymentVoucher";
                                       //                                string details = string.Format(@"SELECT  t.Description ERPInvoiceNo ,AppliedAmount  
                                       //FROM CustomerPayment CP INNER join [transaction] t on cp.TransactionID=t.TransactionID and (t.salesmode=2)
                                       //  WHERE       CP.CustomerPaymentid ='{0}' and cp.Synchronized=0 ", CustomerPaymentID);
                                       //                                string invDetail = "";
                                       //                                incubeQuery = new InCubeQuery(details, db_vms);
                                       //                                if (incubeQuery.Execute() != InCubeErrors.Success) continue;
                                       //                                invoices = incubeQuery.GetDataTable();
                                       //                                if (invoices != null)
                                       //                                    for (int j = 0; j < invoices.Rows.Count; j++)
                                       //                                    {
                                       //                                        invDetail += "{\"paymentval\":" + invoices.Rows[j]["AppliedAmount"].ToString() + ",\"billid\":" + invoices.Rows[j]["ERPInvoiceNo"].ToString() + "},";
                                       //                                    }
                                       //                                if (invDetail.Length > 0) invDetail = "[" + invDetail.Substring(0, invDetail.Length - 1) + "]";
                                       //                                body = string.Format(CheeqTemp, CustomerCode, OutletCode, AccNo, PaymentDate.Day, PaymentDate.Year, PaymentDate.Month, AppliedAmount, VoucherNumber, cheeqDate.Day, cheeqDate.Year, cheeqDate.Month, Notes, EmployeeCode, "{", "}", CustomerPaymentID, invDetail);
                                       //                                Method = "PUT";
                                 Link = CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/BillPNPayment";
                                 //body = string.Format(CheeqTemp, CustomerCode, OutletCode, AccNo, PaymentDate.Day, PaymentDate.Year, PaymentDate.Month, AppliedAmount, VoucherNumber, cheeqDate.Day, cheeqDate.Year, cheeqDate.Month, Notes, EmployeeCode, "{", "}", CustomerPaymentID);
                                 //string CheeqTemp "{10}\"_parameters\": [{10} \"billid\":{0},\"matdateMonth\":{1},\"pntypeid\":{2} ,\"matdateYear\":{3} ,\"matdateDay\":{4},\"billpayment\":{5},\"pnnumber\":{6},\"pn_receiptnumber\":\"{7}\",\"note\":\"{8}\",\"delid\":{9}\"{11}]{11}";
                                 body = string.Format(CheeqTemp,ERPInvoiceNo, VoucherDateTime.Month ,AccNo, VoucherDateTime.Year, VoucherDateTime.Day,AppliedAmount,VoucherNumber,CustomerPaymentID,Notes,EmployeeCode,"{", "}");
                                 Method = "PUT";

                                 break;
                             case "3":// Cash downpayment
                                 Link = CoreGeneral.Common.GeneralConfigurations.WS_URL + "/TPhenixApi/QuickVoucher";
                                 body = string.Format(DownpaymentTemp, CustomerCode, OutletCode, AccNo, PaymentDate.Day, PaymentDate.Year, PaymentDate.Month, AppliedAmount, EmployeeCode, Notes, CustomerPaymentID, "{", "}");
                                 Method = "PUT";
                                 break;
                         }


                         VarBody = body;
                         Varlink = Link;
                         ph_result[] result1 = Tools.GetRequest<ph_result>(Link, body, "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, Method, webHeader);
                         Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, body.ToString()+'-'+result1[0].result.ToString(), LoggingType.Error, LoggingFiles.InCubeLog);
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
             finally
             {

                 CloseSession();
             }

         }*/
        public override void SendOrders()
        {
            try
            {

                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
                string salespersonFilter = "", SalesDocType = "", netTotal = "", SalesType = "", SalesMode = "";
                string CustomerCode = "", OutletCode = "", Hdiscount = "", Notes = "", TransactionID = "", WarehouseCode = "";
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
                   and orderstatusid =2 /*and T.TransactionID = 'INV-VC11-0071-000040'*/"
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

                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Orders_S, new List<string>(filters.Values), processID, 60);
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
sum(SalesOrderDetail.Price*SalesOrderDetail.Quantity* (SalesOrderDetail.Discount+salesorderdetail.promoteddiscount) /100)  Discount,
 convert(int,pack.Width) ItemCode,  
convert(int,pack.Height) UOMID,     
--sum((SalesOrderDetail.Price*SalesOrderDetail.Quantity)-(SalesOrderDetail.Price*SalesOrderDetail.Quantity* SalesOrderDetail.Discount/100))*  SalesOrderDetail.Tax /100 tax
SalesOrderDetail.Tax    
,SalesOrderDetail.salestransactiontypeid ItemType , SalesOrderDetail.Sequence
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
,SalesOrderDetail.CustomerID,SalesOrderDetail.OutletID,SalesOrderDetail.Sequence
ORDER BY SalesOrderDetail.Sequence
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
                            string ItemCode = "", UOM = "", Quantity = "", Price = "", BasePrice = "", Tax = "", discount = "", Type = "", bonus = "0";

                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            UOM = dtDetails.Rows[j]["UOMID"].ToString();
                            Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString();//.ToString("#0.000");
                            Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString();//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                            Tax = decimal.Parse(dtDetails.Rows[j]["Tax"].ToString()).ToString();
                            discount = decimal.Parse(dtDetails.Rows[j]["discount"].ToString()).ToString();
                            Type = dtDetails.Rows[j]["ItemType"].ToString();
                            Price = decimal.Parse(Price.ToString()).ToString();
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

                            allDetails += string.Format(detailsTemp, UOM, Price, ItemCode, discount, Quantity, "{", "}", QtyField, Tax) + ",";


                        }
                        allDetails = allDetails.Substring(0, allDetails.Length - 1);
                        string headerData = string.Format(headerTemp, Hdiscount, Salesperson, TransactionID, SalesMode, DateTime.Parse(TransactionDate.ToString()).Month, CustomerCode, SalesType, DateTime.Parse(TransactionDate.ToString()).Year, DateTime.Parse(TransactionDate.ToString()).Day
                          , DateTime.Parse(TransactionDate.ToString()).Hour, DateTime.Parse(TransactionDate.ToString()).Minute
                            , Notes, WarehouseCode, allDetails, "{", "}", OutletCode);
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
            finally
            {
                CloseSession();
            }
        }
        public override void SendInvoices()
        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
                string salespersonFilter = "", SalesDocType = "", netTotal = "", SalesType = "", SalesMode = "";
                string CustomerCode = "", IsCredit = "", OutletCode = "", Hdiscount = "", Notes = "", TransactionID = "", WarehouseCode = "", TaxCode = "", VatGroup = "";
                DateTime TransactionDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string QtyField = "quantity";
                /* string headerTemp =
 "{14}\"_parameters\":[{14}\"billdata\":{14}\"discountamount\":{0}, \"CheckDataValidation\": 0,\"salesmanid\":{1},\"receiptid\":\"{2}\",\"iscash\":{3}," +
 "\"dateMonth\":{4},\"customerid\":{5},\"billtype\":{6},\"dateYear\":{7},\"dateDay\":{8},\"dateHour\":{9},\"dateMinute\":{10},\"currencyid\":1,\"note\":\"{11}\",\"customerManipulateid\":\"{16}\",\"warehouseid\":{12}{15},\"billdetaildata\":[{13}]{15}]{15}";*/
                string headerTemp = "{3} \"CardCode\":\"{0}\",\"DocDueDate\": \"{1}\",\"DocumentLines\" :[{2}]{4}";

                //  string detailsTemp = "{5}\"unitid\":{0},\"itemprice\":{1},\"itemid\":{2},\"discountvalue\":{3},\"mat_vatper\":{8},\"{7}\":{4}{6}";
                string detailsTemp = "{5}\"ItemCode\":\"{1}\",\"Quantity\":\"{2}\",\"TaxCode\":\"{3}\",\"UnitPrice\":\"{0}\",\"BatchNumbers\":[{4}] {6}";
                //string.Format(detailsTemp, Price, ItemCode, Quantity, TaxCode, "{", "}") + ",";
                string BatchDetailsTemp = "{2} \"BatchNumber\": \"{0}\",\"Quantity\": {1}{3}";

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
                     AND T.transactiontypeid in (1) and t.Voided<>1 and posted=1 
             Order by T.transactionid "
                     , Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
                /* string invoicesHeader = string.Format(@"select t.TransactionID,i.ItemCode,td.Quantity,'T1' T1,td.Price


 from[Transaction] T
 INNER JOIN[TransactionDetail] td on t.TransactionID = td.TransactionID

 inner join Pack p on p.PackID = td.PackID
 inner join Item i on i.ItemID = p.ItemID

 where t.Voided = 0 and t.Posted = 1 and t.TransactionTypeID = 1 and t.Synchronized = 0 AND T.transactiondate >= '{0}' AND T.transactiondate < '{1}'",
 Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);*/
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

                        /* Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Sales_S, new List<string>(filters.Values), processID, 60);
                         if (lastRes == Result.Success || lastRes == Result.Duplicate)
                         {
                             res = Result.Duplicate;
                             incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1 WHERE transactionid = '" + TransactionID + "'");
                             incubeQuery.ExecuteNonQuery();
                             result.Append("Duplicate transaction [" + TransactionID + "]");
                             throw (new Exception("Transaction already sent  check table  Int_ExecutionDetails!!"));
                         }
                         else if (IsSentTransaction(TransactionID) == Result.Success)
                         {
                             res = Result.Duplicate;
                             incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1 WHERE transactionid = '" + TransactionID + "'");
                             incubeQuery.ExecuteNonQuery();
                             result.Append("Duplicate transaction [" + TransactionID + "]");
                             throw (new Exception("Transaction already sent and checked in API  check table  Int_ExecutionDetails!!"));
                         }*/



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
                                        // if (SalesMode != "1")
                                        //   SalesMode = "0";
                                        //if (/*SalesDocType == "2" || SalesDocType == "4" ||SalesDocType == "3" ||*/  IsCredit != "0")
                                        //  SalesMode = "0";//Credit


                        string invoiceDetails = string.Format(@"SELECT  pack.PackID,    
sum(transactiondetail.Quantity) Quantity,
transactiondetail.Price, 
transactiondetail.basePrice, 
sum(transactiondetail.Discount)  Discount,
convert(int,pack.Width) ItemCode,  
convert(int,pack.Height) UOMID,    
(case when sum(transactiondetail.Tax) >0 then item.PackDefinition else 0 end) Tax
,transactiondetail.salestransactiontypeid ItemType ,'O1' AS 'TaxCode','01' AS 'VatGroup'
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
,transactiondetail.CustomerID,transactiondetail.OutletID,pack.PackID
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
                            string PackID = "", ItemCode = "", UOM = "", Quantity = "", Price = "", BasePrice = "", Tax = "", discount = "", Type = "", bonus = "0";
                            PackID = dtDetails.Rows[j]["PackID"].ToString();
                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            UOM = dtDetails.Rows[j]["UOMID"].ToString();
                            Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString();//.ToString("#0.000");
                            Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString();//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                            Tax = decimal.Parse(dtDetails.Rows[j]["Tax"].ToString()).ToString();
                            discount = decimal.Parse(dtDetails.Rows[j]["discount"].ToString()).ToString();
                            Type = dtDetails.Rows[j]["ItemType"].ToString();
                            Price = decimal.Parse(Price.ToString()).ToString();
                            BasePrice = dtDetails.Rows[j]["basePrice"].ToString();
                            TaxCode = dtDetails.Rows[j]["TaxCode"].ToString();
                            VatGroup = dtDetails.Rows[j]["VatGroup"].ToString();



                            string BatchDetails = string.Format(@"select BatchNo,Quantity from TransactionDetail

                               where TransactionID like '{0}' and TransactionDetail.PackID='{1}' ", TransactionID, PackID);
                            incubeQuery = new InCubeQuery(db_vms, BatchDetails);
                            if (incubeQuery.Execute() != InCubeErrors.Success)
                            {
                                res = Result.Failure;
                                throw (new Exception("Batch details query failed !!"));
                            }

                            DataTable BtDetails = incubeQuery.GetDataTable();
                            string allBatchDetails = "";
                            for (int k = 0; k < BtDetails.Rows.Count; k++)
                            {
                                string BatchNo = "", BatchQuantity = "";

                                BatchNo = BtDetails.Rows[k]["BatchNo"].ToString();
                                // UOM = dtDetails.Rows[j]["UOMID"].ToString();
                                BatchQuantity = decimal.Parse(BtDetails.Rows[k]["Quantity"].ToString()).ToString();//.ToString("#0.000");
                                /* Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString();//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                                 Tax = decimal.Parse(dtDetails.Rows[j]["Tax"].ToString()).ToString();
                                 discount = decimal.Parse(dtDetails.Rows[j]["discount"].ToString()).ToString();
                                 Type = dtDetails.Rows[j]["ItemType"].ToString();
                                 Price = decimal.Parse(Price.ToString()).ToString();
                                 BasePrice = dtDetails.Rows[j]["basePrice"].ToString();
                                 TaxCode = dtDetails.Rows[j]["TaxCode"].ToString();
                                 VatGroup = dtDetails.Rows[j]["VatGroup"].ToString();*/
                                /*  if (Type == "4" || Type == "2")
                                  {
                                      // QtyField = "bonus";
                                      QtyField = "quantity";
                                      Price = BasePrice;
                                      discount = (decimal.Parse(BasePrice) * decimal.Parse(Quantity)).ToString();

                                  }
                                  else
                                      QtyField = "quantity";*/
                                allBatchDetails += string.Format(BatchDetailsTemp, BatchNo, BatchQuantity, "{", "}") + ",";


                            }
                            allBatchDetails = allBatchDetails.Substring(0, allBatchDetails.Length - 1);


                            allDetails += string.Format(detailsTemp, Price, ItemCode, Quantity, TaxCode, allBatchDetails, "{", "}") + ",";


                        }
                        allDetails = allDetails.Substring(0, allDetails.Length - 1);


                        /* string headerData = string.Format(headerTemp, Hdiscount, Salesperson, TransactionID, SalesMode, DateTime.Parse(TransactionDate.ToString()).Month, CustomerCode, SalesType, DateTime.Parse(TransactionDate.ToString()).Year, DateTime.Parse(TransactionDate.ToString()).Day
                           , DateTime.Parse(TransactionDate.ToString()).Hour, DateTime.Parse(TransactionDate.ToString()).Minute
                             , Notes, WarehouseCode, allDetails, "{", "}", OutletCode);*/
                        string headerData = string.Format(headerTemp, CustomerCode, TransactionDate, allDetails, "{", "}");
                        view_water_result[] result1 = Tools.GetRequest<view_water_result>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "Invoices", headerData, "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);
                        if (result1 == null || (result1[0].result != null && result1[0].result.Trim() != "success"))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (result1[0].result != null ? result1[0].value : "") + "\r\n json:" + headerData);
                            WriteMessage("Error .. \r\n" + result1[0].result + " \r\n" + result1[0].value);

                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + result1[0].DocNum + "\r\n Message:" + (result1[0].DocNum != null ? result1[0].DocNum : "") + "\r\n json:" + headerData);
                            WriteMessage("Success, ERP No: " + result1[0].DocNum);
                            //if (IsCredit != "0")
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1,Description='" + result1[0].DocNum + "' WHERE TransactionID = '" + TransactionID + "'");
                            /*else
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1,Description='" + result1[0].bill_id + "' WHERE TransactionID = '" + TransactionID + "'");*/

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
            finally
            {
                CloseSession();
            }

        }

        public override void SendNewCustomers()
        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
                string salespersonFilter = "", Phone = "", CreditLimit = "", TaxNumber = "", CustomerType = "";
                string CustomerCode = "", CardType = "", OutletCode = "", Hdiscount = "", Notes = "", TransactionID = "", CustomerName = "", TaxCode = "", VatGroup = "", Frozen = "";
                DateTime TransactionDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string QtyField = "quantity";
                
                /* string headerTemp =
 "{14}\"_parameters\":[{14}\"billdata\":{14}\"discountamount\":{0}, \"CheckDataValidation\": 0,\"salesmanid\":{1},\"receiptid\":\"{2}\",\"iscash\":{3}," +
 "\"dateMonth\":{4},\"customerid\":{5},\"billtype\":{6},\"dateYear\":{7},\"dateDay\":{8},\"dateHour\":{9},\"dateMinute\":{10},\"currencyid\":1,\"note\":\"{11}\",\"customerManipulateid\":\"{16}\",\"warehouseid\":{12}{15},\"billdetaildata\":[{13}]{15}]{15}";*/
                string headerTemp = "{8}  \"CardName\":\"{1}\",\"CardType\": \"{2}\",\"GroupCode\" :\"{3}\",\"PayTermsGrpCode\":\"{4}\",\"FederalTaxID\": \"{5}\",\"CreditLimit\" :\"{6}\",\"Phone1\":\"{7}\",\"Frozen\":\"{10}\",\"U_SonicID\":\"{0}\"{9}";
                //      string headerData = string.Format(headerTemp, OutletCode, CustomerName, CardType, GroupCode, CustomerType,TaxNumber,CreditLimit,Phone, "{", "}");
                //string headerData = string.Format(headerTemp, OutletCode, CardType, GroupCode, CustomerType, TaxNumber,CreditLimit, CustomerCode, Phone, "{", "}");
                //  string detailsTemp = "{5}\"unitid\":{0},\"itemprice\":{1},\"itemid\":{2},\"discountvalue\":{3},\"mat_vatper\":{8},\"{7}\":{4}{6}";
                //  string detailsTemp = "{5}\"ItemCode\":\"{1}\",\"Quantity\":\"{2}\",\"TaxCode\":\"{3}\",\"UnitPrice\":\"{0}\",\"BatchNumbers\":[{4}] {6}";
                //string.Format(detailsTemp, Price, ItemCode, Quantity, TaxCode, "{", "}") + ",";
                // string BatchDetailsTemp = "{2} \"BatchNumber\": \"{0}\",\"Quantity\": {1}{3}";

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND T.EmployeeID = " + Filters.EmployeeID;
                }
                string invoicesHeader = string.Format(@"select c.CustomerCode,'C' CardType,cl.Description CustomeName ,co.CustomerCode OutletCode,col.Description OutletName,co.CustomerTypeID 'CustomerType',co.TaxNumber,cg.GroupCode,a.CreditLimit,co.Phone,'Y' Frozen from 

customer c

inner join CustomerOutlet co on co.CustomerID=c.CustomerID
left join CustomerLanguage cl on cl.CustomerID=c.CustomerID and cl.LanguageID=1
left join CustomerOutletLanguage col on col.CustomerID=co.CustomerID and col.OutletID=co.OutletID and col.LanguageID=1
inner join CustomerOutletGroup cog on cog.CustomerID=co.CustomerID and co.OutletID=cog.OutletID
inner join CustomerGroup cg on cg.GroupID=cog.GroupID
inner join AccountCustOut aco on aco.CustomerID=co.CustomerID and aco.OutletID=co.OutletID
inner join Account a on a.AccountID=aco.AccountID
left join CustomerTypeLanguage ctp on ctp.CustomerTypeID=co.CustomerTypeID and ctp.LanguageID=1

where c.New=1");


                /* string invoicesHeader = string.Format(@"select t.TransactionID,i.ItemCode,td.Quantity,'T1' T1,td.Price


 from[Transaction] T
 INNER JOIN[TransactionDetail] td on t.TransactionID = td.TransactionID

 inner join Pack p on p.PackID = td.PackID
 inner join Item i on i.ItemID = p.ItemID

 where t.Voided = 0 and t.Posted = 1 and t.TransactionTypeID = 1 and t.Synchronized = 0 AND T.transactiondate >= '{0}' AND T.transactiondate < '{1}'",
 Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);*/
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
                        /*res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();
                        TransactionID = dtInvoices.Rows[i]["Transactionid"].ToString();
                        ReportProgress("Sending Transaction: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(23, TransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);*/

                        /* Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Sales_S, new List<string>(filters.Values), processID, 60);
                         if (lastRes == Result.Success || lastRes == Result.Duplicate)
                         {
                             res = Result.Duplicate;
                             incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1 WHERE transactionid = '" + TransactionID + "'");
                             incubeQuery.ExecuteNonQuery();
                             result.Append("Duplicate transaction [" + TransactionID + "]");
                             throw (new Exception("Transaction already sent  check table  Int_ExecutionDetails!!"));
                         }
                         else if (IsSentTransaction(TransactionID) == Result.Success)
                         {
                             res = Result.Duplicate;
                             incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1 WHERE transactionid = '" + TransactionID + "'");
                             incubeQuery.ExecuteNonQuery();
                             result.Append("Duplicate transaction [" + TransactionID + "]");
                             throw (new Exception("Transaction already sent and checked in API  check table  Int_ExecutionDetails!!"));
                         }*/



                        //TransactionDate = Convert.ToDateTime(dtInvoices.Rows[i]["transactiondate"]);
                        OutletCode = dtInvoices.Rows[i]["OutletCode"].ToString();
                        CardType = dtInvoices.Rows[i]["CardType"].ToString();
                        string GroupCode = dtInvoices.Rows[i]["GroupCode"].ToString();
                        CustomerType = dtInvoices.Rows[i]["CustomerType"].ToString();
                        TaxNumber = dtInvoices.Rows[i]["TaxNumber"].ToString();
                        CreditLimit = dtInvoices.Rows[i]["CreditLimit"].ToString();
                        CustomerName = dtInvoices.Rows[i]["OutletName"].ToString();

                        CustomerCode = dtInvoices.Rows[i]["CustomerCode"].ToString();
                        Phone = dtInvoices.Rows[i]["Phone"].ToString();
                        Frozen= dtInvoices.Rows[i]["Frozen"].ToString();
                        // Hdiscount = "0";// decimal.Parse(dtInvoices.Rows[i]["discount"].ToString()).ToString();//.ToString("F4");
                        // if (SalesMode != "1")
                        //   SalesMode = "0";
                        //if (/*SalesDocType == "2" || SalesDocType == "4" ||SalesDocType == "3" ||*/  IsCredit != "0")
                        //  SalesMode = "0";//Credit


                        //                        string invoiceDetails = string.Format(@"SELECT  pack.PackID,    
                        //sum(transactiondetail.Quantity) Quantity,
                        //transactiondetail.Price, 
                        //transactiondetail.basePrice, 
                        //sum(transactiondetail.Discount)  Discount,
                        //convert(int,pack.Width) ItemCode,  
                        //convert(int,pack.Height) UOMID,    
                        //(case when sum(transactiondetail.Tax) >0 then item.PackDefinition else 0 end) Tax
                        //,transactiondetail.salestransactiontypeid ItemType ,'O1' AS 'TaxCode','01' AS 'VatGroup'
                        //FROM transactiondetail  INNER JOIN
                        //Pack ON transactiondetail.PackID = Pack.PackID INNER JOIN
                        //Item ON Pack.ItemID = Item.ItemID INNER JOIN 
                        //PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID
                        //WHERE (PackTypeLanguage.LanguageID = 1) 
                        // AND (transactiondetail.TransactionID like '{0}')
                        //group by 
                        //transactiondetail.TransactionID,
                        //transactiondetail.Price,   transactiondetail.basePrice, 
                        //pack.Width, 
                        //pack.Height,  item.PackDefinition,
                        // transactiondetail.salestransactiontypeid   
                        //,transactiondetail.CustomerID,transactiondetail.OutletID,pack.PackID
                        //ORDER BY pack.Width
                        //", TransactionID);
                        //                        incubeQuery = new InCubeQuery(db_vms, invoiceDetails);
                        //                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        //                        {
                        //                            res = Result.Failure;
                        //                            throw (new Exception("Transaction details query failed !!"));
                        //                        }

                        //                        DataTable dtDetails = incubeQuery.GetDataTable();
                        //                        string allDetails = "";
                        //                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        //                        {
                        //                            string PackID = "", ItemCode = "", UOM = "", Quantity = "", Price = "", BasePrice = "", Tax = "", discount = "", Type = "", bonus = "0";
                        //                            PackID = dtDetails.Rows[j]["PackID"].ToString();
                        //                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                        //                            UOM = dtDetails.Rows[j]["UOMID"].ToString();
                        //                            Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString();//.ToString("#0.000");
                        //                            Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString();//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                        //                            Tax = decimal.Parse(dtDetails.Rows[j]["Tax"].ToString()).ToString();
                        //                            discount = decimal.Parse(dtDetails.Rows[j]["discount"].ToString()).ToString();
                        //                            Type = dtDetails.Rows[j]["ItemType"].ToString();
                        //                            Price = decimal.Parse(Price.ToString()).ToString();
                        //                            BasePrice = dtDetails.Rows[j]["basePrice"].ToString();
                        //                            TaxCode = dtDetails.Rows[j]["TaxCode"].ToString();
                        //                            VatGroup = dtDetails.Rows[j]["VatGroup"].ToString();



                        //                            string BatchDetails = string.Format(@"select BatchNo,Quantity from TransactionDetail

                        //                               where TransactionID like '{0}' and TransactionDetail.PackID='{1}' ", TransactionID, PackID);
                        //                            incubeQuery = new InCubeQuery(db_vms, BatchDetails);
                        //                            if (incubeQuery.Execute() != InCubeErrors.Success)
                        //                            {
                        //                                res = Result.Failure;
                        //                                throw (new Exception("Batch details query failed !!"));
                        //                            }

                        //                            DataTable BtDetails = incubeQuery.GetDataTable();
                        //                            string allBatchDetails = "";
                        //                            for (int k = 0; k < BtDetails.Rows.Count; k++)
                        //                            {
                        //                                string BatchNo = "", BatchQuantity = "";

                        //                                BatchNo = BtDetails.Rows[k]["BatchNo"].ToString();
                        //                                // UOM = dtDetails.Rows[j]["UOMID"].ToString();
                        //                                BatchQuantity = decimal.Parse(BtDetails.Rows[k]["Quantity"].ToString()).ToString();//.ToString("#0.000");
                        //                                /* Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString();//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                        //                                 Tax = decimal.Parse(dtDetails.Rows[j]["Tax"].ToString()).ToString();
                        //                                 discount = decimal.Parse(dtDetails.Rows[j]["discount"].ToString()).ToString();
                        //                                 Type = dtDetails.Rows[j]["ItemType"].ToString();
                        //                                 Price = decimal.Parse(Price.ToString()).ToString();
                        //                                 BasePrice = dtDetails.Rows[j]["basePrice"].ToString();
                        //                                 TaxCode = dtDetails.Rows[j]["TaxCode"].ToString();
                        //                                 VatGroup = dtDetails.Rows[j]["VatGroup"].ToString();*/
                        //                                /*  if (Type == "4" || Type == "2")
                        //                                  {
                        //                                      // QtyField = "bonus";
                        //                                      QtyField = "quantity";
                        //                                      Price = BasePrice;
                        //                                      discount = (decimal.Parse(BasePrice) * decimal.Parse(Quantity)).ToString();

                        //                                  }
                        //                                  else
                        //                                      QtyField = "quantity";*/
                        //                                allBatchDetails += string.Format(BatchDetailsTemp, BatchNo, Quantity, "{", "}") + ",";


                        //                            }
                        //                            allBatchDetails = allBatchDetails.Substring(0, allBatchDetails.Length - 1);


                        //                            allDetails += string.Format(detailsTemp, Price, ItemCode, Quantity, TaxCode, allBatchDetails, "{", "}") + ",";


                        //                        }
                        //                        allDetails = allDetails.Substring(0, allDetails.Length - 1);


                        /* string headerData = string.Format(headerTemp, Hdiscount, Salesperson, TransactionID, SalesMode, DateTime.Parse(TransactionDate.ToString()).Month, CustomerCode, SalesType, DateTime.Parse(TransactionDate.ToString()).Year, DateTime.Parse(TransactionDate.ToString()).Day
                           , DateTime.Parse(TransactionDate.ToString()).Hour, DateTime.Parse(TransactionDate.ToString()).Minute
                             , Notes, WarehouseCode, allDetails, "{", "}", OutletCode);*/
                        string headerData = string.Format(headerTemp, OutletCode, CustomerName, CardType, GroupCode, CustomerType, TaxNumber, CreditLimit, Phone, "{", "}",Frozen);
                        view_water_result[] result1 = Tools.GetRequest<view_water_result>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "BusinessPartners", headerData, "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);
                        if (result1 == null || (result1[0].result != null && result1[0].result.Trim() != "success"))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (result1[0].result != null ? result1[0].value : "") + "\r\n json:" + headerData);
                            WriteMessage("Error .. \r\n" + result1[0].result + " \r\n" + result1[0].value);

                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + result1[0].DocNum + "\r\n Message:" + (result1[0].bill_tid != null ? result1[0].DocNum : "") + "\r\n json:" + headerData);
                            WriteMessage("Success, ERP No: " + result1[0].bill_tid);
                            //if (IsCredit != "0")
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1,Description='" + result1[0].DocNum + "' WHERE TransactionID = '" + TransactionID + "'");
                            /*else
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1,Description='" + result1[0].bill_id + "' WHERE TransactionID = '" + TransactionID + "'");*/

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
            finally
            {
                CloseSession();
            }

        }

        public override void SendReturn()
        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
                string salespersonFilter = "", SalesDocType = "", netTotal = "", SalesType = "", SalesMode = "";
                string CustomerCode = "", IsCredit = "", OutletCode = "", Hdiscount = "", ReturnReason = "", Notes = "", TransactionID = "", WarehouseCode = "", TaxCode = "", VatGroup = "";
                DateTime TransactionDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string QtyField = "quantity";
                /* string headerTemp =
 "{14}\"_parameters\":[{14}\"billdata\":{14}\"discountamount\":{0}, \"CheckDataValidation\": 0,\"salesmanid\":{1},\"receiptid\":\"{2}\",\"iscash\":{3}," +
 "\"dateMonth\":{4},\"customerid\":{5},\"billtype\":{6},\"dateYear\":{7},\"dateDay\":{8},\"dateHour\":{9},\"dateMinute\":{10},\"currencyid\":1,\"note\":\"{11}\",\"customerManipulateid\":\"{16}\",\"warehouseid\":{12}{15},\"billdetaildata\":[{13}]{15}]{15}";*/
                string headerTemp = "{3} \"CardCode\":\"{0}\",\"U_COR\":\"{1}\",\"DocumentLines\" :[{2}]{4}";

                //  string detailsTemp = "{5}\"unitid\":{0},\"itemprice\":{1},\"itemid\":{2},\"discountvalue\":{3},\"mat_vatper\":{8},\"{7}\":{4}{6}";
                string detailsTemp = "{5}\"ItemCode\":\"{1}\",\"Quantity\":\"{2}\",\"TaxCode\":\"{3}\",\"UnitPrice\":\"{0}\",\"BatchNumbers\":[{4}] {6}";
                //string.Format(detailsTemp, Price, ItemCode, Quantity, TaxCode, "{", "}") + ",";
                string BatchDetailsTemp = "{2} \"BatchNumber\": \"{0}\",\"Quantity\": {1}{3}";

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND T.EmployeeID = " + Filters.EmployeeID;
                }
                string invoicesHeader = string.Format(@"SELECT   T.transactionid,   T.transactiondate ,  V.Barcode,e.Employeecode , C.CustomerCode, CO.CustomerCode OutletCode , t.Discount, t.Notes,case when TransactionTypeID in(1,3) then  e.HourlyRegularRate else e.HourlyOvertimeRate end SalesType
       ,t.TransactionTypeID,iif(isnull( t.CreationReason ,0)=11,1,isnull(t.SalesMode,co.customertypeid) )  SalesMode,(select count(*) from CustomerPayment cp where cp.TransactionID=t.TransactionID and cp.PaymentStatusID<>5 and cp.PaymentTypeID in(2,3) and t.SalesMode=1)  IsCredit, rrl.Description ReturnReason 
 FROM [Transaction] T 
 inner join TransactionDetail td on td.TransactionID=t.TransactionID
                     INNER JOIN CustomerOutlet CO ON CO.CustomerID = T.CustomerID AND CO.OutletID = T.OutletID
                     INNER JOIN Customer  C  ON C .CustomerID = T.CustomerID 
                     INNER JOIN Organization O ON O.OrganizationID = T.OrganizationID
                     INNER JOIN Employee E ON E.EmployeeID = T.EmployeeID
                     LEFT JOIN EmployeeVehicle ev on e.EmployeeID=ev.EmployeeID
                     LEFT JOIN Warehouse V ON V.WarehouseID = ev.VehicleID 
					 LEFT JOIN ReturnReasonLanguage rrl on rrl.ReturnReasonID=td.ReturnReason and rrl.LanguageID=1
                     WHERE T.Synchronized = 0  
                       AND T.transactiondate >= '{0}' AND T.transactiondate < '{1}' 
                   {2}
                     AND T.transactiontypeid in (2) and t.Voided<>1 and posted=1 
             Order by T.transactionid"
                     , Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
                /* string invoicesHeader = string.Format(@"select t.TransactionID,i.ItemCode,td.Quantity,'T1' T1,td.Price


 from[Transaction] T
 INNER JOIN[TransactionDetail] td on t.TransactionID = td.TransactionID

 inner join Pack p on p.PackID = td.PackID
 inner join Item i on i.ItemID = p.ItemID

 where t.Voided = 0 and t.Posted = 1 and t.TransactionTypeID = 1 and t.Synchronized = 0 AND T.transactiondate >= '{0}' AND T.transactiondate < '{1}'",
 Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);*/
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

                        /* Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Sales_S, new List<string>(filters.Values), processID, 60);
                         if (lastRes == Result.Success || lastRes == Result.Duplicate)
                         {
                             res = Result.Duplicate;
                             incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1 WHERE transactionid = '" + TransactionID + "'");
                             incubeQuery.ExecuteNonQuery();
                             result.Append("Duplicate transaction [" + TransactionID + "]");
                             throw (new Exception("Transaction already sent  check table  Int_ExecutionDetails!!"));
                         }
                         else if (IsSentTransaction(TransactionID) == Result.Success)
                         {
                             res = Result.Duplicate;
                             incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1 WHERE transactionid = '" + TransactionID + "'");
                             incubeQuery.ExecuteNonQuery();
                             result.Append("Duplicate transaction [" + TransactionID + "]");
                             throw (new Exception("Transaction already sent and checked in API  check table  Int_ExecutionDetails!!"));
                         }*/



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
                        ReturnReason = dtInvoices.Rows[i]["ReturnReason"].ToString();
                        Hdiscount = "0";// decimal.Parse(dtInvoices.Rows[i]["discount"].ToString()).ToString();//.ToString("F4");
                                        // if (SalesMode != "1")
                                        // SalesMode = "0";
                                        //if (/*SalesDocType == "2" || SalesDocType == "4" ||SalesDocType == "3" ||*/  IsCredit != "0")
                                        //SalesMode = "0";//Credit


                        string invoiceDetails = string.Format(@"SELECT  pack.PackID,    
sum(transactiondetail.Quantity) Quantity,
transactiondetail.Price, 
transactiondetail.basePrice, 
sum(transactiondetail.Discount)  Discount,
convert(int,pack.Width) ItemCode,  
convert(int,pack.Height) UOMID,    
(case when sum(transactiondetail.Tax) >0 then item.PackDefinition else 0 end) Tax
,transactiondetail.salestransactiontypeid ItemType ,'O1' AS 'TaxCode',psl.Description as 'PackStatus'
FROM transactiondetail  INNER JOIN
Pack ON transactiondetail.PackID = Pack.PackID 
inner join PackStatusLanguage psl on psl.StatusID=TransactionDetail.PackStatusID and psl.LanguageID=1
 INNER JOIN Item ON Pack.ItemID = Item.ItemID INNER JOIN 
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID
WHERE (PackTypeLanguage.LanguageID = 1) 
 AND (transactiondetail.TransactionID like '{0}')
group by 
transactiondetail.TransactionID,
transactiondetail.Price,   transactiondetail.basePrice, 
pack.Width, 
pack.Height,  item.PackDefinition,
 transactiondetail.salestransactiontypeid   
,transactiondetail.CustomerID,transactiondetail.OutletID,psl.Description,pack.PackID
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
                            string ItemCode = "", PackID = "", UOM = "", Quantity = "", Price = "", BasePrice = "", Tax = "", discount = "", Type = "", bonus = "0", PackStatus = "";
                            PackID = dtDetails.Rows[j]["PackID"].ToString();
                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            UOM = dtDetails.Rows[j]["UOMID"].ToString();
                            Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString();//.ToString("#0.000");
                            Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString();//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                            Tax = decimal.Parse(dtDetails.Rows[j]["Tax"].ToString()).ToString();
                            discount = decimal.Parse(dtDetails.Rows[j]["discount"].ToString()).ToString();
                            Type = dtDetails.Rows[j]["ItemType"].ToString();
                            Price = decimal.Parse(Price.ToString()).ToString();
                            BasePrice = dtDetails.Rows[j]["basePrice"].ToString();
                            TaxCode = dtDetails.Rows[j]["TaxCode"].ToString();
                            PackStatus = dtDetails.Rows[j]["PackStatus"].ToString();

                            string BatchDetails = string.Format(@"select BatchNo,Quantity from TransactionDetail

                               where TransactionID like '{0}' and TransactionDetail.PackID='{1}' ", TransactionID, PackID);
                            incubeQuery = new InCubeQuery(db_vms, BatchDetails);
                            if (incubeQuery.Execute() != InCubeErrors.Success)
                            {
                                res = Result.Failure;
                                throw (new Exception("Batch details query failed !!"));
                            }

                            DataTable BtDetails = incubeQuery.GetDataTable();
                            string allBatchDetails = "";
                            for (int k = 0; k < BtDetails.Rows.Count; k++)
                            {
                                string BatchNo = "", BatchQuantity = "";

                                BatchNo = BtDetails.Rows[k]["BatchNo"].ToString();
                                // UOM = dtDetails.Rows[j]["UOMID"].ToString();
                                BatchQuantity = decimal.Parse(BtDetails.Rows[k]["Quantity"].ToString()).ToString();//.ToString("#0.000");
                                /* Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString();//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                                 Tax = decimal.Parse(dtDetails.Rows[j]["Tax"].ToString()).ToString();
                                 discount = decimal.Parse(dtDetails.Rows[j]["discount"].ToString()).ToString();
                                 Type = dtDetails.Rows[j]["ItemType"].ToString();
                                 Price = decimal.Parse(Price.ToString()).ToString();
                                 BasePrice = dtDetails.Rows[j]["basePrice"].ToString();
                                 TaxCode = dtDetails.Rows[j]["TaxCode"].ToString();
                                 VatGroup = dtDetails.Rows[j]["VatGroup"].ToString();*/
                                /*  if (Type == "4" || Type == "2")
                                  {
                                      // QtyField = "bonus";
                                      QtyField = "quantity";
                                      Price = BasePrice;
                                      discount = (decimal.Parse(BasePrice) * decimal.Parse(Quantity)).ToString();

                                  }
                                  else
                                      QtyField = "quantity";*/
                                allBatchDetails += string.Format(BatchDetailsTemp, BatchNo, Quantity, "{", "}") + ",";


                            }
                            allBatchDetails = allBatchDetails.Substring(0, allBatchDetails.Length - 1);

                            allDetails += string.Format(detailsTemp, Price, ItemCode, Quantity, TaxCode, allBatchDetails, "{", "}") + ",";


                        }
                        allDetails = allDetails.Substring(0, allDetails.Length - 1);
                        /* string headerData = string.Format(headerTemp, Hdiscount, Salesperson, TransactionID, SalesMode, DateTime.Parse(TransactionDate.ToString()).Month, CustomerCode, SalesType, DateTime.Parse(TransactionDate.ToString()).Year, DateTime.Parse(TransactionDate.ToString()).Day
                           , DateTime.Parse(TransactionDate.ToString()).Hour, DateTime.Parse(TransactionDate.ToString()).Minute
                             , Notes, WarehouseCode, allDetails, "{", "}", OutletCode);*/
                        string headerData = string.Format(headerTemp, CustomerCode, ReturnReason, allDetails, "{", "}");
                        view_water_result[] result1 = Tools.GetRequest<view_water_result>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "Returns", headerData, "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);
                        if (result1 == null || (result1[0].result != null && result1[0].result.Trim() != "success"))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (result1[0].result != null ? result1[0].value : "") + "\r\n json:" + headerData);
                            WriteMessage("Error .. \r\n" + result1[0].result + " \r\n" + result1[0].value);

                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + result1[0].DocNum + "\r\n Message:" + (result1[0].bill_tid != null ? result1[0].DocNum : "") + "\r\n json:" + headerData);
                            WriteMessage("Success, ERP No: " + result1[0].bill_tid);
                            //if (IsCredit != "0")
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1,Description='" + result1[0].DocNum + "' WHERE TransactionID = '" + TransactionID + "'");
                            /*else
                                 incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1,Description='" + result1[0].bill_id + "' WHERE TransactionID = '" + TransactionID + "'");*/

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
            finally
            {
                CloseSession();
            }

        }

        public override void SendReciepts()
        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
                string salespersonFilter = "", SalesDocType = "", netTotal = "", SalesType = "", SalesMode = "";
                string CustomerCode = "", IsCredit = "", OutletCode = "", Hdiscount = "", Notes = "", CustomerPaymentID = "", WarehouseCode = "", TaxCode = "", VatGroup = "";
                DateTime TransactionDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string QtyField = "quantity";
                /* string headerTemp =
 "{14}\"_parameters\":[{14}\"billdata\":{14}\"discountamount\":{0}, \"CheckDataValidation\": 0,\"salesmanid\":{1},\"receiptid\":\"{2}\",\"iscash\":{3}," +
 "\"dateMonth\":{4},\"customerid\":{5},\"billtype\":{6},\"dateYear\":{7},\"dateDay\":{8},\"dateHour\":{9},\"dateMinute\":{10},\"currencyid\":1,\"note\":\"{11}\",\"customerManipulateid\":\"{16}\",\"warehouseid\":{12}{15},\"billdetaildata\":[{13}]{15}]{15}";*/
                string headerTemp = "{5} \"CardCode\":\"{0}\",\"CashAccount\" :\"{1}\",\"U_SalesEmp\" :\"{2}\",\"CashFlowAssignments\" :[{3}],\"CashSum\":\"{4}\"{6}";

                //  string detailsTemp = "{5}\"unitid\":{0},\"itemprice\":{1},\"itemid\":{2},\"discountvalue\":{3},\"mat_vatper\":{8},\"{7}\":{4}{6}";
                string detailsTemp = "{2}\"AmountLC\":\"{1}\",\"PaymentMeans\":\"{0}\"{3}";

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND T.EmployeeID = " + Filters.EmployeeID;
                }
                string invoicesHeader = string.Format(@"select cp.CustomerPaymentID,co.CustomerCode,e.EmployeeCode,el.Description EmpName,e.NationalIDNumber as 'Account',sum(cp.AppliedAmount) as 'CashSum'
from CustomerPayment cp

inner join CustomerOutlet co on co.CustomerID=cp.CustomerID and co.OutletID=cp.OutletID
inner join Employee e on e.EmployeeID=cp.EmployeeID
left join EmployeeLanguage el on el.EmployeeID=e.EmployeeID and LanguageID=1

where cp.CustomerPaymentID='PAY-102019-000147' and cp.PaymentStatusID <> 5 and cp.Synchronized=0

and cp.Posted=1      AND cp.PaymentDate >= '{0}' AND cp.PaymentDate < '{1}' 
                   {2}
group by cp.CustomerPaymentID,co.CustomerCode,e.EmployeeCode,el.Description ,e.NationalIDNumber 
                  
                     
             "
                     , Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
                /* string invoicesHeader = string.Format(@"select t.TransactionID,i.ItemCode,td.Quantity,'T1' T1,td.Price


 from[Transaction] T
 INNER JOIN[TransactionDetail] td on t.TransactionID = td.TransactionID

 inner join Pack p on p.PackID = td.PackID
 inner join Item i on i.ItemID = p.ItemID

 where t.Voided = 0 and t.Posted = 1 and t.TransactionTypeID = 1 and t.Synchronized = 0 AND T.transactiondate >= '{0}' AND T.transactiondate < '{1}'",
 Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);*/
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
                        CustomerPaymentID = dtInvoices.Rows[i]["CustomerPaymentID"].ToString();
                        ReportProgress("Sending Transaction: " + CustomerPaymentID);
                        WriteMessage("\r\n" + CustomerPaymentID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(23, CustomerPaymentID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        /* Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Sales_S, new List<string>(filters.Values), processID, 60);
                         if (lastRes == Result.Success || lastRes == Result.Duplicate)
                         {
                             res = Result.Duplicate;
                             incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1 WHERE transactionid = '" + TransactionID + "'");
                             incubeQuery.ExecuteNonQuery();
                             result.Append("Duplicate transaction [" + TransactionID + "]");
                             throw (new Exception("Transaction already sent  check table  Int_ExecutionDetails!!"));
                         }
                         else if (IsSentTransaction(TransactionID) == Result.Success)
                         {
                             res = Result.Duplicate;
                             incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1 WHERE transactionid = '" + TransactionID + "'");
                             incubeQuery.ExecuteNonQuery();
                             result.Append("Duplicate transaction [" + TransactionID + "]");
                             throw (new Exception("Transaction already sent and checked in API  check table  Int_ExecutionDetails!!"));
                         }*/



                        /* TransactionDate = Convert.ToDateTime(dtInvoices.Rows[i]["transactiondate"]);
                         SalesDocType = dtInvoices.Rows[i]["TransactionTypeID"].ToString();
                         WarehouseCode = dtInvoices.Rows[i]["Barcode"].ToString();*/
                        CustomerCode = dtInvoices.Rows[i]["CustomerCode"].ToString();
                        string Salesperson = dtInvoices.Rows[i]["Employeecode"].ToString();
                        string EmpName = dtInvoices.Rows[i]["EmpName"].ToString();
                        string Account = dtInvoices.Rows[i]["Account"].ToString();
                        string CashSum = dtInvoices.Rows[i]["CashSum"].ToString();

                        /* SalesMode = dtInvoices.Rows[i]["SalesMode"].ToString();
                         SalesType = dtInvoices.Rows[i]["SalesType"].ToString();
                         Notes = dtInvoices.Rows[i]["Notes"].ToString();
                         OutletCode = dtInvoices.Rows[i]["OutletCode"].ToString();*/

                        // IsCredit = dtInvoices.Rows[i]["IsCredit"].ToString();
                        // Hdiscount = "0";// decimal.Parse(dtInvoices.Rows[i]["discount"].ToString()).ToString();//.ToString("F4");
                        // if (SalesMode != "1")
                        //    SalesMode = "0";
                        //if (/*SalesDocType == "2" || SalesDocType == "4" ||SalesDocType == "3" ||*/  IsCredit != "0")
                        // SalesMode = "0";//Credit


                        string invoiceDetails = string.Format(@"select ptl.Description PaymentType,cp.AppliedAmount

from CustomerPayment cp


left join PaymentTypeLanguage ptl on ptl.PaymentTypeID=cp.PaymentTypeID and ptl.LanguageID=1
where cp.CustomerPaymentID like '{0}' and cp.PaymentStatusID <> 5 and cp.Synchronized=0

and cp.Posted=1
", CustomerPaymentID);
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
                            string PaymentType = "", AppliedAmount = "", Quantity = "", Price = "", BasePrice = "", Tax = "", discount = "", Type = "", bonus = "0", PackStatus = "";

                            PaymentType = dtDetails.Rows[j]["PaymentType"].ToString();
                            AppliedAmount = dtDetails.Rows[j]["AppliedAmount"].ToString();
                            // Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString();//.ToString("#0.000");
                            // Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString();//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                            /* Tax = decimal.Parse(dtDetails.Rows[j]["Tax"].ToString()).ToString();
                             discount = decimal.Parse(dtDetails.Rows[j]["discount"].ToString()).ToString();
                             Type = dtDetails.Rows[j]["ItemType"].ToString();
                             Price = decimal.Parse(Price.ToString()).ToString();
                             BasePrice = dtDetails.Rows[j]["basePrice"].ToString();
                             TaxCode = dtDetails.Rows[j]["TaxCode"].ToString();
                             PackStatus = dtDetails.Rows[j]["PackStatus"].ToString();*/
                            /*  if (Type == "4" || Type == "2")
                              {
                                  // QtyField = "bonus";
                                  QtyField = "quantity";
                                  Price = BasePrice;
                                  discount = (decimal.Parse(BasePrice) * decimal.Parse(Quantity)).ToString();

                              }
                              else
                                  QtyField = "quantity";*/
                            allDetails += string.Format(detailsTemp, PaymentType, AppliedAmount, "{", "}") + ",";


                        }
                        allDetails = allDetails.Substring(0, allDetails.Length - 1);
                        /* string headerData = string.Format(headerTemp, Hdiscount, Salesperson, TransactionID, SalesMode, DateTime.Parse(TransactionDate.ToString()).Month, CustomerCode, SalesType, DateTime.Parse(TransactionDate.ToString()).Year, DateTime.Parse(TransactionDate.ToString()).Day
                           , DateTime.Parse(TransactionDate.ToString()).Hour, DateTime.Parse(TransactionDate.ToString()).Minute
                             , Notes, WarehouseCode, allDetails, "{", "}", OutletCode);*/
                        string headerData = string.Format(headerTemp, CustomerCode, Account, Salesperson, allDetails, CashSum, "{", "}");
                        view_water_result[] result1 = Tools.GetRequest<view_water_result>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "IncomingPayments", headerData, "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);
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
                            WriteMessage("Success, ERP No: " + result1[0].DocNum);
                            // if (IsCredit != "0")
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE CustomerPayment SET Synchronized = 1,Description='" + result1[0].DocNum + "',SalesMode=2 WHERE CustomerPaymentID= '" + CustomerPaymentID + "'");
                            /*else
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE CustomerPayment SET Synchronized = 1,Description='" + result1[0].DocNum + "' WHERE CustomerPaymentID = '" + CustomerPaymentID + "'");*/

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
            finally
            {
                CloseSession();
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
                string salespersonFilter = "", netTotal = "", SalesType = "", TransactionTypeID="";
                string Notes = "", TransactionID = "", WarehouseCode = "";
                DateTime TransactionDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string QtyField = "quantity";
                string headerTemp = " {1}\"StockTransferLines\": [{0}]{2}";


                string detailsTemp = "{5}\"ItemCode\":\"{0}\",\"Quantity\":\"{1}\",\"UnitPrice\":\"{2}\",\"WarehouseCode\":\"{3}\",\"FromWarehouseCode\":\"{4}\",{6}";
                //       allDetails += string.Format(detailsTemp, ItemCode, Quantity, Price, WHCode, RefWHCode, allBatchDetails, "{", "}") + ",";
             //   string BatchDetailsTemp = "{2} \"BatchNumber\": \"{0}\",\"Quantity\": {1}{3}";

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND wt.RequestedBy = " + Filters.EmployeeID;
                }
                string invoicesHeader = string.Format(@"

					select wt.transactionid,wt.TransactionDate,wt.TransactionTypeID
from WarehouseTransaction wt
where wt.Synchronized<>1 and (TransactionTypeID in(1,2) and
 (TransactionOperationID in(1,5) and WarehouseTransactionStatusID in(3,4,5,6))) 
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
                        TransactionTypeID= dtInvoices.Rows[i]["TransactionTypeID"].ToString();
                        TransactionID = dtInvoices.Rows[i]["Transactionid"].ToString();
                        ReportProgress("Sending Transaction: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(IntegrationField.Transfers_S.GetHashCode(), TransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                       /* Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Transfers_S, new List<string>(filters.Values), processID, 60);
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
                        }*/


                        TransactionDate = Convert.ToDateTime(dtInvoices.Rows[i]["TransactionDate"]);

                       /* WarehouseCode = dtInvoices.Rows[i]["Barcode"].ToString();
                        string Salesperson = dtInvoices.Rows[i]["Employeecode"].ToString();
                        SalesType = dtInvoices.Rows[i]["TempletID"].ToString();
                        Notes = dtInvoices.Rows[i]["Notes"].ToString();*/



                        string invoiceDetails = string.Format(@"select   p.packid,i.itemcode,sum(wtd.quantity) ItemQuantity,pd.price, w.warehousecode WHCode,refw.warehousecode RefWHCode
from WarehouseTransaction wt
inner join WhTransDetail wtd on wtd.TransactionID=wt.TransactionID
inner join Pack p on p.PackID=wtd.PackID
inner join Item i on i.ItemID=p.ItemID
inner join Warehouse w on w.WarehouseID=wt.WarehouseID
inner join Warehouse refw on refw.WarehouseID=wt.RefWarehouseID
left join PriceDefinition pd on wtd.PackID = pd.PackID
and pd.PriceListID in (select keyvalue from Configuration where KeyName='DefaultPriceListID')

where wt.TransactionID like  '{0}'

group by wt.transactionid,p.packid,i.itemcode,pd.price, w.warehousecode ,refw.warehousecode 
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
                            string ItemCode = "", WHCode = "", Quantity = "", Price = "" , RefWHCode="", PackID="";

                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            WHCode = dtDetails.Rows[j]["WHCode"].ToString();
                            RefWHCode = dtDetails.Rows[j]["RefWHCode"].ToString();
                            if (TransactionTypeID=="2") {
                                WHCode = dtDetails.Rows[j]["RefWHCode"].ToString();
                                RefWHCode = dtDetails.Rows[j]["WHCode"].ToString();
                            }
                           
                            Quantity = decimal.Parse(dtDetails.Rows[j]["ItemQuantity"].ToString()).ToString();//.ToString("#0.000");
                            Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString();//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                            PackID = dtDetails.Rows[j]["PackID"].ToString();


                           /* string BatchDetails = string.Format(@"select BatchNo,Quantity from WhTransDetail

                               where TransactionID like '{0}' and WhTransDetail.PackID='{1}' ", TransactionID, PackID);
                            incubeQuery = new InCubeQuery(db_vms, BatchDetails);
                            if (incubeQuery.Execute() != InCubeErrors.Success)
                            {
                                res = Result.Failure;
                                throw (new Exception("Batch details query failed !!"));
                            }

                            DataTable BtDetails = incubeQuery.GetDataTable();
                            string allBatchDetails = "";
                            for (int k = 0; k < BtDetails.Rows.Count; k++)
                            {
                                string BatchNo = "", BatchQuantity = "";

                                BatchNo = BtDetails.Rows[k]["BatchNo"].ToString();
                                // UOM = dtDetails.Rows[j]["UOMID"].ToString();
                                BatchQuantity = decimal.Parse(BtDetails.Rows[k]["Quantity"].ToString()).ToString();//.ToString("#0.000");
                                /* Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString();//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                                 Tax = decimal.Parse(dtDetails.Rows[j]["Tax"].ToString()).ToString();
                                 discount = decimal.Parse(dtDetails.Rows[j]["discount"].ToString()).ToString();
                                 Type = dtDetails.Rows[j]["ItemType"].ToString();
                                 Price = decimal.Parse(Price.ToString()).ToString();
                                 BasePrice = dtDetails.Rows[j]["basePrice"].ToString();
                                 TaxCode = dtDetails.Rows[j]["TaxCode"].ToString();
                                 VatGroup = dtDetails.Rows[j]["VatGroup"].ToString();*/
                                /*  if (Type == "4" || Type == "2")
                                  {
                                      // QtyField = "bonus";
                                      QtyField = "quantity";
                                      Price = BasePrice;
                                      discount = (decimal.Parse(BasePrice) * decimal.Parse(Quantity)).ToString();

                                  }
                                  else
                                      QtyField = "quantity";
                                allBatchDetails += string.Format(BatchDetailsTemp, BatchNo, BatchQuantity, "{", "}") + ",";


                            }
                            allBatchDetails = allBatchDetails.Substring(0, allBatchDetails.Length - 1);*/



                            allDetails += string.Format(detailsTemp, ItemCode, Quantity, Price, WHCode, RefWHCode, "{", "}") + ",";




                        }
                        allDetails = allDetails.Substring(0, allDetails.Length - 1);

                        string headerData = string.Format(headerTemp, allDetails,  "{", "}");
                        view_water_result[] result1 = Tools.GetRequest<view_water_result>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "/InventoryTransferRequests", headerData, "result", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "POST", webHeader);
                        if (result1 == null || (result1[0].result != null && result1[0].result.Trim() != "success"))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (result1[0].result != null ? result1[0].value : "") + "\r\n json:" + headerData);
                            WriteMessage("Error .. \r\n" + result1[0].result + " \r\n" + result1[0].value);

                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + result1[0].DocNum + "\r\n Message:" + (result1[0].bill_tid != null ? result1[0].DocNum : "") + "\r\n json:" + headerData);
                            WriteMessage("Success, ERP No: " + result1[0].DocNum);
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [WarehouseTransaction] SET Synchronized = 1,LPONumber='" + result1[0].DocNum + "'  WHERE TransactionID = '" + TransactionID + "'");
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
            finally
            {
                CloseSession();
            }

        }
    }

    class ViewWaterSalesperson
    {
        public string SalesEmployeeCode { get; set; }
        public string SalesEmployeeName { get; set; }
    }

    class ViewWaterItems
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ForeignName { get; set; }
        public string ItemsGroupCode { get; set; }
        public string SalesUnit { get; set; }
        public string DefaultWarehouse { get; set; }
    }

    class ViewWaterPrices
    {
        public string CUSTOMERCODE { get; set; }
        public string ITEMCODE { get; set; }
        public decimal PRICE { get; set; }
        public string UOM { get; set; }
        public string id__ { get; set; }
       
    }

    class ViewWaterCustomerGroup
    {
        //[JsonProperty(PropertyName = "value")]
        public int Code { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }

    }
    class view_water_result
    {
        public string result { get; set; }
        public string session { get; set; }
        public string value { get; set; }
        public string DocNum { get; set; }
        public string bill_tid { get; set; }
    }
    class View_WarehouseStock
    {
        public string Code { get; set; }
        public string Current_balance { get; set; }
        public string Storage_ID { get; set; }
        public string Unit { get; set; }
        public string WarehouseID { get; set; }
        public string PackId { get; set; }



    }

    class view_result
    {
        public string SessionId { get; set; }
       
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class BatchNumberDetails
    {
        public string Batch { get; set; }
    }

    public class VanItems
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ForeignName { get; set; }
        public string SalesUnit { get; set; }
    }

    public class ItemsItemWarehouseInfoCollection
    {
        public string WarehouseCode { get; set; }
        public double InStock { get; set; }
    }


    public class Stock
    {
        public VanItems Items { get; set; }

        [JsonProperty("Items/ItemWarehouseInfoCollection")]
        public ItemsItemWarehouseInfoCollection ItemsItemWarehouseInfoCollection { get; set; }
        public BatchNumberDetails BatchNumberDetails { get; set; }

        public string ItemCode { get { return (Items.ItemCode); } }
        public string ItemName { get { return (Items.ItemName); } }
        public string ForeignName { get { return (Items.ForeignName); } }
        public string SalesUnit { get {return (Items.SalesUnit); } }
        public string WarehouseCode { get { return (ItemsItemWarehouseInfoCollection.WarehouseCode); } }
        public double InStock { get { return (ItemsItemWarehouseInfoCollection.InStock); } }
        public string Batch { get { return (BatchNumberDetails.Batch); } }

    }


}