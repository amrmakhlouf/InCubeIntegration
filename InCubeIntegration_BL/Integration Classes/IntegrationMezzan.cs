using System.Net.Http;
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
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Reflection;

namespace InCubeIntegration_BL
{
    public class IntegrationMezzan : IntegrationBase, IDisposable// Live branch
    {
        #region MainRegion
        BackgroundWorker bgwCheckProgress;
        InCubeDatabase db_res;
        int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
        SqlCommand cmd;
        InCubeQuery incubeQuery = null;
        string StagingTable = "";
        string _WarehouseID = "-1";
        string _Session = "";
        WebHeaderCollection webHeader = null;
        WebHeaderCollection cookies = null;

        ~IntegrationMezzan()
        {

            CloseSession();

        }
        protected virtual void Dispose(bool disposing)
        {
            CloseSession();
        }
        public IntegrationMezzan(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            if (CoreGeneral.Common.CurrentSession.loginType != LoginType.WindowsService)
            {
                db_res = new InCubeDatabase();
                db_res.Open("InCube", "IntegrationMezzan");
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

        private void GetSession()
        {
            try
            {
                string username = CoreGeneral.Common.GeneralConfigurations.WS_UserName;
                string password = CoreGeneral.Common.GeneralConfigurations.WS_Password;
                string authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));

                webHeader = new WebHeaderCollection
                {
                    { "Authorization", $"Basic {authValue}" }
                };

                cookies = new WebHeaderCollection();

                var result = Tools.GetTokenAndCookies(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_DELIVERY_SRV/HeaderSet", webHeader);

                webHeader.Add("x-csrf-token", result["token"].ToString());
                var cookiesResponse = result["cookies"] as Dictionary<string, string>;
                if (cookiesResponse != null)
                {
                    foreach (var cookie in cookiesResponse)
                    {
                        cookies.Add(cookie.Key, cookie.Value);
                    }
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
                _Session = "";
                //webHeader = null;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        #endregion

        #region UpdateFunctionOverrides
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
        public override void OutStanding()
        {
            GetMasterData(IntegrationField.Outstanding_U);
        }
        public override void UpdateRoutes()
        {
            GetMasterData(IntegrationField.Route_U);
        }
        public override void UpdatePrice()
        {
            GetMasterData(IntegrationField.Price_U);
        }
        public override void UpdateMainWarehouse()
        {
            GetMasterData(IntegrationField.Warehouse_U);
        }
        public override void UpdateDiscount()
        {
            GetMasterData(IntegrationField.Discount_U);
        }

        public override void UpdateSTA()
        {
            GetMasterData(IntegrationField.STA_U);
        }
        public override void UpdateKPI()
        {
            GetMasterData(IntegrationField.KPI_U);
        }

        public override void UpdateEDI()
        {
            GetMasterData(IntegrationField.EDI_U);
        }

        public override void UpdateSTP()
        {
            GetMasterData(IntegrationField.STP_U);
        }

        public override void UpdateOrders()
        {
            GetMasterData(IntegrationField.Orders_U);
        }

        public override void StoreInvoices()
        {
            GetMasterData(IntegrationField.CNT_U);
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
                        ProcName = "dbo.MySimpleProcedure";
                        break;
                    case IntegrationField.Customer_U:
                        res = GetCustomerTable(ref dtMasterData);
                        StagingTable = "Stg_Customers";
                        ProcName = "sp_UpdateCustomers";
                        break;
                    case IntegrationField.Outstanding_U:
                        res = GetUnpaid(ref dtMasterData);
                        StagingTable = "Stg_Unpaid";
                        ProcName = "sp_UpdateUnpaid";
                        break;
                    case IntegrationField.Invoice_U:
                        res = GetInvoicesTable(ref dtMasterData);
                        StagingTable = "Stg_Invoices";
                        ProcName = "sp_UpdateInvoices";
                        break;
                    case IntegrationField.Price_U:
                        res = GetPricesTable_A(ref dtMasterData);
                        StagingTable = "Stg_Prices_A";
                        ProcName = "sp_UpdatePrices_A";
                        break;
                    case IntegrationField.KPI_U:
                        res = GetPricesTable_B(ref dtMasterData);
                        StagingTable = "Stg_Prices_B";
                        ProcName = "sp_UpdatePrices_B";
                        break;
                    case IntegrationField.EDI_U:
                        res = GetPricesTable_C(ref dtMasterData);
                        StagingTable = "Stg_Prices_C";
                        ProcName = "sp_UpdatePrices_C";
                        break;
                    case IntegrationField.Stock_U:
                        res = GetStockTable(ref dtMasterData);
                        StagingTable = "Stg_Stock";
                        ProcName = "sp_UpdateStock";
                        break;
                    case IntegrationField.Discount_U:
                        res = GetDiscountTable(ref dtMasterData);
                        StagingTable = "Stg_Discounts";
                        ProcName = "sp_UpdateDiscounts";
                        break;
                    case IntegrationField.Route_U:
                        res = GetLoadTransferTable(ref dtMasterData);
                        StagingTable = "Stg_LoadTransfer";
                        ProcName = "sp_UpdateLoadTransfer";
                        break;
                    case IntegrationField.STA_U:
                        res = GetTaxRateTable(ref dtMasterData); 
                        StagingTable = "Stg_TaxRate";
                        ProcName = "sp_UpdateTaxRate";
                        break;
                    case IntegrationField.STP_U:
                        res = GetCollectionReversalTable(ref dtMasterData);
                        StagingTable = "Stg_CollectionReversal";
                        ProcName = "sp_UpdateCollectionReversal";
                        break;
                    case IntegrationField.Orders_U:
                        res = GetCollectionReversalTablePOS(ref dtMasterData);
                        StagingTable = "Stg_CollectionReversal";
                        ProcName = "sp_UpdateCollectionReversal";
                        break;
                    case IntegrationField.CNT_U:
                        res = GetAllocationTable(ref dtMasterData);
                        StagingTable = "Stg_Allocation";
                        ProcName = "sp_UpdateAllocation";
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
//                if (TableName == "Stg_Customers") 
//{
//    string Query = "ALTER TABLE stg_customers ADD guid UNIQUEIDENTIFIER";
//    incubeQuery = new InCubeQuery(db_vms, Query);
//}


                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    SqlBulkCopy bulk = new SqlBulkCopy(db_vms.GetConnection());
                    bulk.DestinationTableName = TableName;
                    bulk.WriteToServer(dtData);
                    if (TableName == "Stg_Customers")
                    {
                        string Query = "ALTER TABLE stg_customers ADD guid UNIQUEIDENTIFIER";
                        incubeQuery = new InCubeQuery(db_vms, Query);
                        if (incubeQuery.Execute() == InCubeErrors.Success) { }

                    }

                    res = Result.Success;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        #endregion

        #region GetTables

        private Result GetAllocationTable(ref DataTable DT)
        {
            if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
            Result res = Result.Failure;
            try
            {
                DT = Tools.GetRequestTable<MezzanAllocation>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_CLEARING_CC_SRV/CLEARING_DOCSSet?$filter=CompanyCode eq '1930'&$format=json",
                    "", "results", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);

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

        private Result GetTaxRateTable(ref DataTable DT)
        {
            if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
            Result res = Result.Failure;
            try
            {
                DT = Tools.GetRequestTable<MezzanTaxRate>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_TAX_RATE_API_SRV/TaxAmountSet?$filter=IpCondType eq 'OVAT' and IpCountry eq 'SA'&$format=json",
                    "", "results", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);

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

        private Result GetCollectionReversalTable(ref DataTable DT)
        {
            if (webHeader == null)
                throw (new Exception("Can't open session in API , please check the service!!"));

            Result res = Result.Failure;
            try
            {
                string currentDate = DateTime.Now.ToString("yyyyMMdd");
                DataTable dt1 = Tools.GetRequestTable<MezzanCollectionReversal>(
                    CoreGeneral.Common.GeneralConfigurations.WS_URL +
                    $"ZHH_INC_REVERSAL_CASH_SRV/ReversalCashSet?$filter=ICsCpCode eq '1330' and ICsClrEntryDate eq '{currentDate}'&$format=json",
                    "", "results",
                    CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                    CoreGeneral.Common.GeneralConfigurations.WS_Password,
                    "GET", webHeader);

                // Call 2: CpCode 1350
                DataTable dt2 = Tools.GetRequestTable<MezzanCollectionReversal>(
                    CoreGeneral.Common.GeneralConfigurations.WS_URL +
                    $"ZHH_INC_REVERSAL_CASH_SRV/ReversalCashSet?$filter=ICsCpCode eq '1350' and ICsClrEntryDate eq '{currentDate}'&$format=json",
                    "", "results",
                    CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                    CoreGeneral.Common.GeneralConfigurations.WS_Password,
                    "GET", webHeader);

                DataTable dt3 = Tools.GetRequestTable<MezzanCollectionReversal>(
                    CoreGeneral.Common.GeneralConfigurations.WS_URL +
                    $"ZHH_INC_REVERSAL_CASH_SRV/ReversalCashSet?$filter=ICsCpCode eq '1110' and ICsClrEntryDate eq '{currentDate}'&$format=json",
                    "", "results",
                    CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                    CoreGeneral.Common.GeneralConfigurations.WS_Password,
                    "GET", webHeader);

                DataTable dt4 = Tools.GetRequestTable<MezzanCollectionReversal>(
                    CoreGeneral.Common.GeneralConfigurations.WS_URL +
                    $"ZHH_INC_REVERSAL_CASH_SRV/ReversalCashSet?$filter=ICsCpCode eq '1090' and ICsClrEntryDate eq '{currentDate}'&$format=json",
                    "", "results",
                    CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                    CoreGeneral.Common.GeneralConfigurations.WS_Password,
                    "GET", webHeader);

                DT = new DataTable();
                MergeTables(ref DT, dt1);
                MergeTables(ref DT, dt2);
                MergeTables(ref DT, dt3);
                MergeTables(ref DT, dt4);

                res = (DT != null && DT.Rows.Count > 0) ? Result.Success : Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name,
                    System.Reflection.MethodBase.GetCurrentMethod().Name,
                    ex.Message + "\r\n\r\n" + ex.StackTrace,
                    LoggingType.Error,
                    LoggingFiles.InCubeLog);
            }
            finally
            {
                CloseSession();
            }
            return res;
        }


        private Result GetCollectionReversalTablePOS(ref DataTable DT)
        {
            //if (webHeader == null)
            //    throw (new Exception("Can't open session in API , please check the service!!"));

            Result res = Result.Failure;
            //try
            //{
            //    string currentDate = DateTime.Now.ToString("yyyyMMdd");

            //    DataTable dt1 = Tools.GetRequestTable<MezzanCollectionReversal>(
            //        CoreGeneral.Common.GeneralConfigurations.WS_URL +
            //        $"ZHH_INC_CLEARING_CC_SRV/CLEARING_DOCSSet?$filter=CompanyCode eq '1330' &$format=json",
            //        "", "results",
            //        CoreGeneral.Common.GeneralConfigurations.WS_UserName,
            //        CoreGeneral.Common.GeneralConfigurations.WS_Password,
            //        "GET", webHeader);

            //    DataTable dt2 = Tools.GetRequestTable<MezzanCollectionReversal>(
            //        CoreGeneral.Common.GeneralConfigurations.WS_URL +
            //        $"ZHH_INC_CLEARING_CC_SRV/CLEARING_DOCSSet?$filter=CompanyCode eq '1350' &$format=json",
            //        "", "results",
            //        CoreGeneral.Common.GeneralConfigurations.WS_UserName,
            //        CoreGeneral.Common.GeneralConfigurations.WS_Password,
            //        "GET", webHeader);

            //    DataTable dt3 = Tools.GetRequestTable<MezzanCollectionReversal>(
            //        CoreGeneral.Common.GeneralConfigurations.WS_URL +
            //        $"ZHH_INC_CLEARING_CC_SRV/CLEARING_DOCSSet?$filter=CompanyCode eq '1110' &$format=json",
            //        "", "results",
            //        CoreGeneral.Common.GeneralConfigurations.WS_UserName,
            //        CoreGeneral.Common.GeneralConfigurations.WS_Password,
            //        "GET", webHeader);

            //    DataTable dt4 = Tools.GetRequestTable<MezzanCollectionReversal>(
            //        CoreGeneral.Common.GeneralConfigurations.WS_URL +
            //        $"ZHH_INC_CLEARING_CC_SRV/CLEARING_DOCSSet?$filter=CompanyCode eq '1090' &$format=json",
            //        "", "results",
            //        CoreGeneral.Common.GeneralConfigurations.WS_UserName,
            //        CoreGeneral.Common.GeneralConfigurations.WS_Password,
            //        "GET", webHeader);

            //    // Merge all results into DT
            //    DT = new DataTable();
            //    MergeTables(ref DT, dt1);
            //    MergeTables(ref DT, dt2);
            //    MergeTables(ref DT, dt3);
            //    MergeTables(ref DT, dt4);

            //    res = (DT != null && DT.Rows.Count > 0) ? Result.Success : Result.NoRowsFound;
           //     res =  Result.NoRowsFound;
            //}
            //catch (Exception ex)
            //{
            //    Logger.WriteLog(
            //        System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name,
            //        System.Reflection.MethodBase.GetCurrentMethod().Name,
            //        ex.Message + "\r\n\r\n" + ex.StackTrace,
            //        LoggingType.Error,
            //        LoggingFiles.InCubeLog);
            //}
            //finally
            //{
            //    CloseSession();
            //}
            return res;
        }



        //private Result GetItemTable(ref DataTable DT)
        //{
        //    if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
        //    Result res = Result.Failure;
        //    try
        //    {
        //        DT = Tools.GetRequestTable<MezzanItems>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_MATERIAL_SRV/ProductSet?$filter=(IPrCpCode eq '1110' or IPrCpCode eq '1350')  and IPrDstrbChnl eq '03'&$format=json",
        //            "", "results", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);

        //        if (DT != null && DT.Rows.Count > 0)
        //            res = Result.Success;
        //        else
        //            res = Result.NoRowsFound;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
        //    }
        //    finally
        //    {
        //        CloseSession();
        //    }
        //    return res;
        //}
        private Result GetItemTable(ref DataTable DT)
        {
            if (webHeader == null)
                throw (new Exception("Can't open session in API , please check the service!!"));

            Result res = Result.Failure;
            try
            {
               DataTable dt1 = null;
               DataTable dt2 = null;
                DataTable dt3 = null;  // NEW third request
                DataTable dt4 = null;
                //Call for IPrCpCode 1110

                dt1 = Tools.GetRequestTable<MezzanItems>(
                    CoreGeneral.Common.GeneralConfigurations.WS_URL +
                    "ZHH_INC_MATERIAL_SRV/ProductSet?$filter=IPrCpCode eq '1110' and IPrDstrbChnl eq '03'&$format=json",
                    "", "results",
                    CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                    CoreGeneral.Common.GeneralConfigurations.WS_Password,
                    "GET", webHeader);

                //// Call for IPrCpCode 1350
                dt2 = Tools.GetRequestTable<MezzanItems>(
                    CoreGeneral.Common.GeneralConfigurations.WS_URL +
                    "ZHH_INC_MATERIAL_SRV/ProductSet?$filter=IPrCpCode eq '1350' and IPrDstrbChnl eq '03'&$format=json",
                    "", "results",
                    CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                    CoreGeneral.Common.GeneralConfigurations.WS_Password,
                    "GET", webHeader);

                //   Call for IPrCpCode 1400(example third API)

                dt3 = Tools.GetRequestTable<MezzanItems>(
                   CoreGeneral.Common.GeneralConfigurations.WS_URL +
                   "ZHH_INC_MATERIAL_SRV/ProductSet?$filter=IPrCpCode eq '1330' and IPrDstrbChnl eq '03'&$format=json",
                   "", "results",
                   CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                   CoreGeneral.Common.GeneralConfigurations.WS_Password,
                   "GET", webHeader);
               dt4 = Tools.GetRequestTable<MezzanItems>(
                 CoreGeneral.Common.GeneralConfigurations.WS_URL +
                 "ZHH_INC_MATERIAL_SRV/ProductSet?$filter=IPrCpCode eq '1090' and IPrDstrbChnl eq '03'&$format=json",
                 "", "results",
                 CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                 CoreGeneral.Common.GeneralConfigurations.WS_Password,
                 "GET", webHeader);


               // Initialize DT
               DT = new DataTable();

                // Merge dt1
                if (dt1 != null && dt1.Rows.Count > 0)
                    DT = dt1.Copy();

                //// Merge dt2
                if (dt2 != null && dt2.Rows.Count > 0)
                {
                    if (DT.Rows.Count == 0)
                        DT = dt2.Copy();
                    else
                    {
                        foreach (DataRow row in dt2.Rows)
                            DT.ImportRow(row);
                    }
                }

                // Merge dt3
                if (dt3 != null && dt3.Rows.Count > 0)
                {
                    if (DT.Rows.Count == 0)
                        DT = dt3.Copy();
                    else
                    {
                        foreach (DataRow row in dt3.Rows)
                            DT.ImportRow(row);
                    }
                }
                if (dt4 != null && dt4.Rows.Count > 0)
                {
                    if (DT.Rows.Count == 0)
                        DT = dt4.Copy();
                    else
                    {
                        foreach (DataRow row in dt4.Rows)
                            DT.ImportRow(row);
                    }
                }

                // Final result
                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name,
                    System.Reflection.MethodBase.GetCurrentMethod().Name,
                    ex.Message + "\r\n\r\n" + ex.StackTrace,
                    LoggingType.Error,
                    LoggingFiles.InCubeLog);
            }
            finally
            {
                CloseSession();
            }
            return res;
        }
        //private Result GetCustomerTable(ref DataTable DT)
        //{
        //    if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
        //    Result res = Result.Failure;
        //    try
        //    {
        //        DT = Tools.GetRequestTable<MezzanCustomers>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_CUSTOMER_MASTER_SRV/CustomerMasterSet?$filter=( ICuType eq 'CREDIT' or ICuType eq 'CASH' or ICuType eq 'B2B' ) and ICuDiv eq '07' and ICuDstrbChnl eq '03' and ICuCpCodeSorg eq '1930'&$format=json",
        //            "", "results", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);

        //        if (DT != null && DT.Rows.Count > 0)
        //            res = Result.Success;
        //        else
        //            res = Result.NoRowsFound;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
        //    }
        //    finally
        //    {
        //        CloseSession();
        //    }
        //    return res;
        //}
        private void MergeTables(ref DataTable mainTable, DataTable newTable)
        {
            if (newTable != null && newTable.Rows.Count > 0)
            {
                if (mainTable == null || mainTable.Rows.Count == 0)
                    mainTable = newTable.Copy(); // copy structure + rows
                else
                {
                    foreach (DataRow row in newTable.Rows)
                        mainTable.ImportRow(row);
                }
            }
        }
        private Result GetCustomerTable(ref DataTable DT)
        {
            if (webHeader == null)
                throw (new Exception("Can't open session in API , please check the service!!"));

            Result res = Result.Failure;
            try
            {
             DataTable dt1 = null;
              DataTable dt2 = null;
                DataTable dt3 = null; // NEW third request
                DataTable dt4 = null;
                //  Call for ICuCpCodeSorg 1110

                dt1 = Tools.GetRequestTable<MezzanCustomers>(
                    CoreGeneral.Common.GeneralConfigurations.WS_URL +
                    "ZHH_INC_CUSTOMER_MASTER_SRV/CustomerMasterSet?$filter=( ICuType eq 'CREDIT' or ICuType eq 'CASH' or ICuType eq 'B2B' ) and ICuDiv eq '07' and ICuDstrbChnl eq '03' and ICuCpCodeSorg eq '1110'&$format=json",
                    "", "results",
                    CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                    CoreGeneral.Common.GeneralConfigurations.WS_Password,
                    "GET", webHeader);

                //// Call for ICuCpCodeSorg 1350
                dt2 = Tools.GetRequestTable<MezzanCustomers>(
                    CoreGeneral.Common.GeneralConfigurations.WS_URL +
                    "ZHH_INC_CUSTOMER_MASTER_SRV/CustomerMasterSet?$filter=( ICuType eq 'CREDIT' or ICuType eq 'CASH' or ICuType eq 'B2B' ) and ICuDiv eq '07' and ICuDstrbChnl eq '03' and ICuCpCodeSorg eq '1350'&$format=json",
                    "", "results",
                    CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                    CoreGeneral.Common.GeneralConfigurations.WS_Password,
                    "GET", webHeader);

                // Call for ICuCpCodeSorg 1400 (example third API)
                dt3 = Tools.GetRequestTable<MezzanCustomers>(
                    CoreGeneral.Common.GeneralConfigurations.WS_URL +
                    "ZHH_INC_CUSTOMER_MASTER_SRV/CustomerMasterSet?$filter=( ICuType eq 'CREDIT' or ICuType eq 'CASH' or ICuType eq 'B2B' ) and ICuDiv eq '07' and ICuDstrbChnl eq '03' and ICuCpCodeSorg eq '1330'&$format=json",
                    "", "results",
                    CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                    CoreGeneral.Common.GeneralConfigurations.WS_Password,
                    "GET", webHeader);
                dt4 = Tools.GetRequestTable<MezzanCustomers>(
                 CoreGeneral.Common.GeneralConfigurations.WS_URL +
                 "ZHH_INC_CUSTOMER_MASTER_SRV/CustomerMasterSet?$filter=( ICuType eq 'CREDIT' or ICuType eq 'CASH' or ICuType eq 'B2B' ) and ICuDiv eq '02' and ICuDstrbChnl eq '03' and ICuCpCodeSorg eq '1090'&$format=json",
                 "", "results",
                 CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                 CoreGeneral.Common.GeneralConfigurations.WS_Password,
                 "GET", webHeader);


                // Merge into DT
                DT = new DataTable();

                // Merge dt1
                if (dt1 != null && dt1.Rows.Count > 0)
                    DT = dt1.Copy();

                //// Merge dt2
                if (dt2 != null && dt2.Rows.Count > 0)
                {
                    if (DT.Rows.Count == 0)
                        DT = dt2.Copy();
                    else
                    {
                        foreach (DataRow row in dt2.Rows)
                            DT.ImportRow(row);
                    }
                }

                // Merge dt3
                if (dt3 != null && dt3.Rows.Count > 0)
                {
                    if (DT.Rows.Count == 0)
                        DT = dt3.Copy();
                    else
                    {
                        foreach (DataRow row in dt3.Rows)
                            DT.ImportRow(row);
                    }
                }
                if (dt4 != null && dt4.Rows.Count > 0)
                {
                    if (DT.Rows.Count == 0)
                        DT = dt4.Copy();
                    else
                    {
                        foreach (DataRow row in dt4.Rows)
                            DT.ImportRow(row);
                    }
                }

                // Final result
                if (DT != null && DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name,
                    System.Reflection.MethodBase.GetCurrentMethod().Name,
                    ex.Message + "\r\n\r\n" + ex.StackTrace,
                    LoggingType.Error,
                    LoggingFiles.InCubeLog);
            }
            finally
            {
                CloseSession();
            }
            return res;
        }


        private Result GetUnpaid(ref DataTable DT)
        {
            if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
            Result res = Result.Failure;
            try
            {
                DT = Tools.GetRequestTable<MezzanUnpaid>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_PENDING_CASH_SRV/PendingCashSet?$filter=ICsCpCode eq '1930' &$format=json",
                    "", "results", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);

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


        private Result GetDiscountTable(ref DataTable DT)
        {



            if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
            Result res = Result.Failure;
            try
            {
                string currentDate = DateTime.Now.ToString("yyyyMMdd");
                DT = Tools.GetRequestTable<MezzanDiscount>(CoreGeneral.Common.GeneralConfigurations.WS_URL + $"ZHH_INC_PRODUCT_PRICE_MASTER_SRV/ProductDiscPriceSet?$filter=IPlCpCode eq '1930' and ICplDistrChl eq '03' and IPlPar eq 'A' and Idate eq '{"20250223"}'&$format=json",
                    "", "results", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);

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

        private Result GetLoadTransferTable(ref DataTable DT)
        {
            if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));
            Result res = Result.Failure;
            try
            {
                string currentDate = DateTime.Now.AddDays(-10).ToString("yyyyMMdd");
                DT = Tools.GetRequestTable<MezzanLoadTransfer>(CoreGeneral.Common.GeneralConfigurations.WS_URL + $"ZHH_INC_CHECKLOAD_MASTER_SRV/CheckLoadSet?$filter=Vkorg eq '1930' and Vtweg eq '03' and Audat eq '{currentDate}'&$format=json",
                    "", "results", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);

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
                DT = Tools.GetRequestTable<MezzanDueInvoice>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_DUE_INVOICE_SRV/DueInvoiceSet?$filter=Vkorg eq '1930' and Zcustype eq 'B2B' &$format=json",
                    "", "results", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);

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
                DT = Tools.GetRequestTable<MezzanSalesmanStock>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_MATERIAL_SRV/Product_WarehouseSet?$filter=IWpCpCode eq '1930' &$format=json",
                    "", "results", CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password, "GET", webHeader);

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

        private Result GetPricesTable_A(ref DataTable DT)
        {
            if (webHeader == null)
                throw (new Exception("Can't open session in API , please check the service!!"));

            Result res = Result.Failure;
            try
            {
                DataTable dt1 = Tools.GetRequestTable<MezzanPriceList>(
                    CoreGeneral.Common.GeneralConfigurations.WS_URL +
                    "ZHH_INC_PRODUCT_PRICE_MASTER_SRV/ProductPriceSet?$filter=IPlCpCode eq '1110' and ICplDistrChl eq '03' and IPlPar eq 'A' &$format=json",
                    "", "results",
                    CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                    CoreGeneral.Common.GeneralConfigurations.WS_Password,
                    "GET", webHeader);

                DataTable dt2 = Tools.GetRequestTable<MezzanPriceList>(
                    CoreGeneral.Common.GeneralConfigurations.WS_URL +
                    "ZHH_INC_PRODUCT_PRICE_MASTER_SRV/ProductPriceSet?$filter=IPlCpCode eq '1350' and ICplDistrChl eq '03' and IPlPar eq 'A' &$format=json",
                    "", "results",
                    CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                    CoreGeneral.Common.GeneralConfigurations.WS_Password,
                    "GET", webHeader);

                DataTable dt3 = Tools.GetRequestTable<MezzanPriceList>(
                    CoreGeneral.Common.GeneralConfigurations.WS_URL +
                    "ZHH_INC_PRODUCT_PRICE_MASTER_SRV/ProductPriceSet?$filter=IPlCpCode eq '1330' and ICplDistrChl eq '03' and IPlPar eq 'A' &$format=json",
                    "", "results",
                    CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                    CoreGeneral.Common.GeneralConfigurations.WS_Password,
                    "GET", webHeader);
                DataTable dt4 = Tools.GetRequestTable<MezzanPriceList>(
                  CoreGeneral.Common.GeneralConfigurations.WS_URL +
                  "ZHH_INC_PRODUCT_PRICE_MASTER_SRV/ProductPriceSet?$filter=IPlCpCode eq '1090' and ICplDistrChl eq '03' and IPlPar eq 'A' &$format=json",
                  "", "results",
                  CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                  CoreGeneral.Common.GeneralConfigurations.WS_Password,
                  "GET", webHeader);
                DT = new DataTable();
                MergeTables(ref DT, dt1);
                MergeTables(ref DT, dt2);
                MergeTables(ref DT, dt3);
                MergeTables(ref DT, dt4);

                res = (DT != null && DT.Rows.Count > 0) ? Result.Success : Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(GetType().Name,
                    System.Reflection.MethodBase.GetCurrentMethod().Name,
                    ex.Message + "\r\n\r\n" + ex.StackTrace,
                    LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                CloseSession();
            }
            return res;
        }


        private Result GetPricesTable_B(ref DataTable DT)
        {
            if (webHeader == null)
                throw (new Exception("Can't open session in API , please check the service!!"));

            Result res = Result.Failure;
            try
            {
                DataTable dt1 = Tools.GetRequestTable<MezzanPriceList>(
                    CoreGeneral.Common.GeneralConfigurations.WS_URL +
                    "ZHH_INC_PRODUCT_PRICE_MASTER_SRV/ProductPriceSet?$filter=IPlCpCode eq '1930' and ICplDistrChl eq '03' and IPlPar eq 'B' &$format=json",
                    "", "results",
                    CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                    CoreGeneral.Common.GeneralConfigurations.WS_Password,
                    "GET", webHeader);

                DataTable dt2 = Tools.GetRequestTable<MezzanPriceList>(
                    CoreGeneral.Common.GeneralConfigurations.WS_URL +
                    "ZHH_INC_PRODUCT_PRICE_MASTER_SRV/ProductPriceSet?$filter=IPlCpCode eq '1350' and ICplDistrChl eq '03' and IPlPar eq 'B' &$format=json",
                    "", "results",
                    CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                    CoreGeneral.Common.GeneralConfigurations.WS_Password,
                    "GET", webHeader);

                DataTable dt3 = Tools.GetRequestTable<MezzanPriceList>(
                    CoreGeneral.Common.GeneralConfigurations.WS_URL +
                    "ZHH_INC_PRODUCT_PRICE_MASTER_SRV/ProductPriceSet?$filter=IPlCpCode eq '1330' and ICplDistrChl eq '03' and IPlPar eq 'B' &$format=json",
                    "", "results",
                    CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                    CoreGeneral.Common.GeneralConfigurations.WS_Password,
                    "GET", webHeader);
                DataTable dt4 = Tools.GetRequestTable<MezzanPriceList>(
                CoreGeneral.Common.GeneralConfigurations.WS_URL +
                "ZHH_INC_PRODUCT_PRICE_MASTER_SRV/ProductPriceSet?$filter=IPlCpCode eq '1330' and ICplDistrChl eq '03' and IPlPar eq 'B' &$format=json",
                "", "results",
                CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                CoreGeneral.Common.GeneralConfigurations.WS_Password,
                "GET", webHeader);


                DT = new DataTable();
                MergeTables(ref DT, dt1);
                MergeTables(ref DT, dt2);
                MergeTables(ref DT, dt3);
                MergeTables(ref DT, dt4);

                res = (DT != null && DT.Rows.Count > 0) ? Result.Success : Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(GetType().Name,
                    System.Reflection.MethodBase.GetCurrentMethod().Name,
                    ex.Message + "\r\n\r\n" + ex.StackTrace,
                    LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                CloseSession();
            }
            return res;
        }

        private Result GetPricesTable_C(ref DataTable DT)
        {
            if (webHeader == null)
                throw (new Exception("Can't open session in API , please check the service!!"));

            Result res = Result.Failure;
            try
            {
                DataTable dt1 = Tools.GetRequestTable<MezzanPriceList>(
                    CoreGeneral.Common.GeneralConfigurations.WS_URL +
                    "ZHH_INC_PRODUCT_PRICE_MASTER_SRV/ProductPriceSet?$filter=IPlCpCode eq '1110' and ICplDistrChl eq '03' and IPlPar eq 'C' &$format=json",
                    "", "results",
                    CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                    CoreGeneral.Common.GeneralConfigurations.WS_Password,
                    "GET", webHeader);

                DataTable dt2 = Tools.GetRequestTable<MezzanPriceList>(
                    CoreGeneral.Common.GeneralConfigurations.WS_URL +
                    "ZHH_INC_PRODUCT_PRICE_MASTER_SRV/ProductPriceSet?$filter=IPlCpCode eq '1350' and ICplDistrChl eq '03' and IPlPar eq 'C' &$format=json",
                    "", "results",
                    CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                    CoreGeneral.Common.GeneralConfigurations.WS_Password,
                    "GET", webHeader);

                DataTable dt3 = Tools.GetRequestTable<MezzanPriceList>(
                    CoreGeneral.Common.GeneralConfigurations.WS_URL +
                    "ZHH_INC_PRODUCT_PRICE_MASTER_SRV/ProductPriceSet?$filter=IPlCpCode eq '1330' and ICplDistrChl eq '03' and IPlPar eq 'C' &$format=json",
                    "", "results",
                    CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                    CoreGeneral.Common.GeneralConfigurations.WS_Password,
                    "GET", webHeader);
                DataTable dt4 = Tools.GetRequestTable<MezzanPriceList>(
                  CoreGeneral.Common.GeneralConfigurations.WS_URL +
                  "ZHH_INC_PRODUCT_PRICE_MASTER_SRV/ProductPriceSet?$filter=IPlCpCode eq '1090' and ICplDistrChl eq '03' and IPlPar eq 'C' &$format=json",
                  "", "results",
                  CoreGeneral.Common.GeneralConfigurations.WS_UserName,
                  CoreGeneral.Common.GeneralConfigurations.WS_Password,
                  "GET", webHeader);

                DT = new DataTable();
              MergeTables(ref DT, dt1);
               MergeTables(ref DT, dt2);
                MergeTables(ref DT, dt3);
                MergeTables(ref DT, dt4);
                res = (DT != null && DT.Rows.Count > 0) ? Result.Success : Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(GetType().Name,
                    System.Reflection.MethodBase.GetCurrentMethod().Name,
                    ex.Message + "\r\n\r\n" + ex.StackTrace,
                    LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                CloseSession();
            }
            return res;
        }

        #endregion
        public async Task SendETransactionAsync(string headerData,string url)
        {
            
            var token = "D802B9A6-C4B6-496A-A87F-DDDF6C4DBD10";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

             

                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var content = new StringContent(headerData, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(url, content);

                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                   
                    Console.WriteLine("Posted successfully. Response: " + responseString);
                }
                else
                {
                    
                    Console.WriteLine($"Error {response.StatusCode}: {responseString}");
                }
            }
        }
        public override void SendInvoices()
        {


            //SendDelivery("", "SRE", false);
            //ReSendInvoice();
            ///////////////////////////
           SendInvoice();
            SendDelivery("", "SRE", false);
          SendPGI("", "SRE", false);
            SendBilling("");
            SendExchange();

         // AboodAPI("INV-11604-000021");

        }

        public override void SendATMCollections() //Offload
        {
           SendOffload();
           SendDelivery("", "WH",false);
            SendPGI("", "WH",false);
        }

        private void SendOffload()
        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));

                string salespersonFilter = "", TransactionID = "", VanCode = "", WarehouseCode = "", OrderType = "", StorageLoc="", DivisionCode="";
                int processID = 0;
                string responseBody = "";
                string headerData = "";
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND RequestedBy = " + Filters.EmployeeID;
                }
                string invoicesHeader = string.Format(@"
                select DivisionCode,V_POST_OffLoadRequest.VanCode,V_POST_OffLoadRequest.StorageLoc,V_POST_OffLoadRequest.TransactionID,V_POST_OffLoadRequest.RequestedBy,V_POST_OffLoadRequest.TransactionDate,V_POST_OffLoadRequest.OrderType,V_POST_OffLoadRequest.WarehouseCode   from
V_POST_OffLoadRequest
inner join Warehouse on V_POST_OffLoadRequest.VanCode = Warehouse.WarehouseCode
inner join  EmployeeVehicle on EmployeeVehicle.VehicleID = Warehouse.WarehouseID
where convert(date, TransactionDate) >= '{0}' AND convert(date, TransactionDate) <= '{1}' {2}
", Filters.FromDate.ToString("yyyy - MM - dd"), Filters.ToDate.AddDays(1).ToString("yyyy - MM - dd"), salespersonFilter);                //$" WHERE AND TransactionDate >= '{Filters.FromDate.ToString("yyyy-MM-dd")}' AND TransactionDate < '{Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd")} {salespersonFilter}";
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
                        DivisionCode = dtInvoices.Rows[i]["DivisionCode"].ToString();
                        ReportProgress("Sending Transaction: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(11, TransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        VanCode = dtInvoices.Rows[i]["VanCode"].ToString();
                        WarehouseCode = dtInvoices.Rows[i]["WarehouseCode"].ToString();
                        OrderType = dtInvoices.Rows[i]["OrderType"].ToString();
                        StorageLoc = dtInvoices.Rows[i]["StorageLoc"].ToString();

                        string invoiceDetails = $@"SELECT I.ItemCode, SUM(WTD.Quantity) Quantity, PTL.Description, WTD.PackID FROM WhTransDetail WTD
                                                INNER JOIN Pack P ON P.PackID = WTD.PackID
                                                INNER JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1
                                                INNER JOIN Item I ON I.ItemID = P.ItemID
                                                WHERE WTD.TransactionID = '{TransactionID}'
                                                GROUP BY I.ItemCode, PTL.Description, WTD.PackID
                                                ORDER BY I.ItemCode, WTD.PackID";

                        incubeQuery = new InCubeQuery(db_vms, invoiceDetails);
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            throw (new Exception("order details query failed !!"));
                        }

                        DataTable dtDetails = incubeQuery.GetDataTable();
                        List<object> allDetailsList = new List<object>();
                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        {
                            string ItemCode = "", UOM = "", Quantity = "";
                            string ItemCategory = OrderType == "YKA" ? "YKAN" : "ZKAN";

                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            UOM = dtDetails.Rows[j]["Description"].ToString();
                            Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString("#0.000");
                            string Plantt;
                            if (DivisionCode == "1350")
                            {
                                Plantt = "1351";
                            }
                            else if (DivisionCode == "1110")
                            {
                                Plantt = "1111";
                            }
                            else if (DivisionCode == "1330")
                            {
                                Plantt = "1331";
                            }
                            else if (int.TryParse(DivisionCode, out int divisionInt))
                            {
                                Plantt = (divisionInt + 1).ToString();
                            }
                            else
                            {
                                Plantt = DivisionCode; // fallback if parsing fails
                            }
                            var details = new
                            {
                                Plant = Plantt,
                                SalesDocumentno = "",
                                Material = ItemCode,
                                DiscPercent = "0.000",
                                OrderQty = Quantity,
                                DiscValue = "0.000",
                                Price = "0.000",
                                ItemCategory = ItemCategory,
                                SalesUnit = UOM,
                                FocPercent = "0.000",
                                FocValue = "0.000",
                                StoreLoc = StorageLoc
                            };

                            allDetailsList.Add(details);
                        }

                        var headerDataObject = new
                        {
                            LoadProcess = "",
                            SalesDocumentno = "",
                            SoldToParty = VanCode,
                            StockPartner = VanCode,
                            DiscPercent = "0.000",
                            OrderType = OrderType,
                            Status = "",
                            CreditExceed = "",
                            DiscValue = "0.000",
                            SalesOrg = DivisionCode,
                            DistributionChannel = "03",
                            Division = DivisionCode == "1090" ? "02" : "07",
                            SalesmanId = VanCode,
                            InvoiceNo = TransactionID,
                            Reference = "",
                            OrderReason = "",
                            FocPercent = "0.000",
                            FocValue = "0.000",
                            HdrItem = allDetailsList
                        };

                        headerData = JsonConvert.SerializeObject(headerDataObject);
                        incubeQuery = new InCubeQuery(db_vms, "INSERT INTO PostingTransaction (TransactionID) VALUES ('" + TransactionID + "')");
                        incubeQuery.ExecuteNonQuery();

                        MezzanSimpleResult[] result1 = Tools.GetRequest<MezzanSimpleResult>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_SALESORDER_SRV/HeaderSet", headerData, "", out responseBody, webHeader, cookies);
                        if (result1[0].d.SalesDocumentno == null || (result1[0].d.SalesDocumentno != null && result1[0].d.SalesDocumentno.Trim() == ""))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (result1[0].d.SalesDocumentno != null ? result1[0].d.SalesDocumentno : "") + "\r\n json:" + headerData);
                            WriteMessage("Error .. \r\n" + TransactionID + " \r\n" + result1[0].d.SalesDocumentno);

                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendOffload()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + result1[0].d.SalesDocumentno + "\r\n Message:" + (result1[0].d.SalesDocumentno != null ? result1[0].d.SalesDocumentno : "") + "\r\n json:" + headerData);
                            WriteMessage("Success, ERP No: " + result1[0].d.SalesDocumentno);
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [WarehouseTransaction] SET Synchronized = 1,LPONumber='1',TruckNumber='" + result1[0].d.SalesDocumentno + "' WHERE TransactionID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                            incubeQuery = new InCubeQuery(db_vms, "INSERT INTO SAP_Reference(TransactionID, SalesOrderRef) VALUES ('" + TransactionID + "', '" + result1[0].d.SalesDocumentno + "')");
                            incubeQuery.ExecuteNonQuery();
                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertSAPPostingData '{TransactionID}', '{result1[0].d.SalesDocumentno}', 'SendOffload()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                            SendDelivery(TransactionID, "WH", false);
                        }
                        incubeQuery = new InCubeQuery(db_vms, "DELETE FROM PostingTransaction WHERE TransactionID='" + TransactionID + "'");
                        incubeQuery.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {

                        incubeQuery = new InCubeQuery(db_vms, "DELETE FROM PostingTransaction WHERE TransactionID= '" + TransactionID + "'");
                        incubeQuery.ExecuteNonQuery();
                        incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendOffload()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                        incubeQuery.ExecuteNonQuery();
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

        public override void SendOrders() //LoadRequest
        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));

                string salespersonFilter = "", TransactionID = "", DivisionCode="", VanCode = "", WarehouseCode = "", OrderType = "", StorageLoc = "";
                int processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string responseBody = "";
                string headerData = "";

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND RequestedBy = " + Filters.EmployeeID;
                }
                string invoicesHeader = string.Format(@"

select DivisionCode, V_POST_LoadRequest.VanCode,V_POST_LoadRequest.StorageLoc,V_POST_LoadRequest.TransactionID,V_POST_LoadRequest.RequestedBy,V_POST_LoadRequest.TransactionDate,V_POST_LoadRequest.OrderType,V_POST_LoadRequest.WarehouseCode from 
V_POST_LoadRequest 
inner join Warehouse on V_POST_LoadRequest.VanCode = Warehouse.WarehouseCode
inner join  EmployeeVehicle on EmployeeVehicle.VehicleID = Warehouse.WarehouseID
where  convert(date,TransactionDate) >= '{0}' AND convert(date,TransactionDate) <= '{1}' {2}
", Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
                //$" WHERE AND TransactionDate >= '{Filters.FromDate.ToString("yyyy-MM-dd")}' AND TransactionDate < '{Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd")} {salespersonFilter}";
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
                        DivisionCode = dtInvoices.Rows[i]["DivisionCode"].ToString();

                        ReportProgress("Sending Transaction: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(11, TransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        VanCode = dtInvoices.Rows[i]["VanCode"].ToString();
                        WarehouseCode = dtInvoices.Rows[i]["WarehouseCode"].ToString();
                        OrderType = dtInvoices.Rows[i]["OrderType"].ToString();
                        StorageLoc = dtInvoices.Rows[i]["StorageLoc"].ToString();

                        string invoiceDetails = $@"SELECT I.ItemCode, WTD.Quantity, PTL.Description FROM WhTransDetail WTD
                                                INNER JOIN Pack P ON P.PackID = WTD.PackID
                                                INNER JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1
                                                INNER JOIN Item I ON I.ItemID = P.ItemID
                                                INNER JOIN WarehouseTransaction WT ON WT.TransactionID = WTD.TransactionID
      INNER JOIN ConfigurationDivision CO ON CO.DivisionID = WT.DivisionID AND CO.Keyname = 'DefaultPriceListID'
INNER JOIN PriceDefinition PD ON PD.PacKID = P.PackID AND PD.PriceListID = CO.KeyValue AND PD.Price > 0
                                                WHERE WTD.TransactionID = '{TransactionID}'";

                        incubeQuery = new InCubeQuery(db_vms, invoiceDetails);
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            throw (new Exception("order details query failed !!"));
                        }

                        DataTable dtDetails = incubeQuery.GetDataTable();
                        List<object> allDetailsList = new List<object>();
                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        {
                            string ItemCode = "", UOM = "", Quantity = "";
                            string ItemCategory = OrderType == "YKBW" ? "YKBW" : "YKBN";

                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            UOM = dtDetails.Rows[j]["Description"].ToString();
                            Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString("#0.000");


                            string Plantt;

                            if (DivisionCode == "1350")
                            {
                                Plantt = "1351";
                            }
                            else if (DivisionCode == "1110")
                            {
                                Plantt = "1111";
                            }
                            else if (DivisionCode == "1330")
                            {
                                Plantt = "1331";
                            }
                            else if (DivisionCode == "1090")
                            {
                                Plantt = "1091";
                            }
                            else if (int.TryParse(DivisionCode, out int divisionInt))
                            {
                                Plantt = (divisionInt + 1).ToString();
                            }
                            else
                            {
                                Plantt = DivisionCode; // fallback if parsing fails
                            }
                            var details = new
                            {
                                //Plant = WarehouseCode,
                        
                                
                                Plant = Plantt,


                                SalesDocumentno = "",
                                Material = ItemCode,
                                DiscPercent = "0.000",
                                OrderQty = Quantity,
                                DiscValue = "0.000",
                                Price = "0.000",
                                ItemCategory = ItemCategory,
                                SalesUnit = UOM,
                                FocPercent = "0.000",
                                FocValue = "0.000",
                                StoreLoc = StorageLoc
                            };

                            allDetailsList.Add(details);
                        }

                        if (dtDetails.Rows.Count > 0)
                        {
                            var headerDataObject = new
                            {
                                LoadProcess = "",
                                SalesDocumentno = "",
                                SoldToParty = VanCode,
                                StockPartner = VanCode,
                                DiscPercent = "0.000",
                                OrderType = OrderType,
                                Status = "",
                                CreditExceed = "",
                                DiscValue = "0.000",
                                SalesOrg = DivisionCode, //KSA
                                DistributionChannel = "03",
                                Division = DivisionCode == "1090" ? "02" : "07",
                                SalesmanId = VanCode,
                                InvoiceNo = TransactionID,
                                Reference = "",
                                OrderReason = "",
                                FocPercent = "0.000",
                                FocValue = "0.000",
                                HdrItem = allDetailsList
                            };

                            headerData = JsonConvert.SerializeObject(headerDataObject);
                           
                            incubeQuery = new InCubeQuery(db_vms, "INSERT INTO PostingTransaction (TransactionID) VALUES ('" + TransactionID + "')");
                           var x = incubeQuery.ExecuteNonQuery();

                            MezzanSimpleResult[] result1 = Tools.GetRequest<MezzanSimpleResult>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_SALESORDER_SRV/HeaderSet", headerData, "", out responseBody, webHeader, cookies);
                            if (result1[0].d.SalesDocumentno == null || (result1[0].d.SalesDocumentno != null && result1[0].d.SalesDocumentno.Trim() == ""))
                            {
                                res = Result.NoFileRetreived;
                                result.Append("ERP ERROR Message:" + (result1[0].d.SalesDocumentno != null ? result1[0].d.SalesDocumentno : "") + "\r\n json:" + headerData);
                                WriteMessage("Error .. \r\n" + TransactionID + " \r\n" + result1[0].d.SalesDocumentno);

                                incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendOrders()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                                incubeQuery.ExecuteNonQuery();
                            }
                            else
                            {
                                res = Result.Success;
                                result.Append("ERP No: " + result1[0].d.SalesDocumentno + "\r\n Message:" + (result1[0].d.SalesDocumentno != null ? result1[0].d.SalesDocumentno : "") + "\r\n json:" + headerData);
                                WriteMessage("Success, ERP No: " + result1[0].d.SalesDocumentno);
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE [WarehouseTransaction] SET Synchronized = 1,LPONumber='1',TruckNumber='" + result1[0].d.SalesDocumentno + "' WHERE TransactionID = '" + TransactionID + "'");
                                incubeQuery.ExecuteNonQuery();
                                incubeQuery = new InCubeQuery(db_vms, "INSERT INTO SAP_Reference(TransactionID, SalesOrderRef) VALUES ('" + TransactionID + "', '" + result1[0].d.SalesDocumentno + "')");
                                incubeQuery.ExecuteNonQuery();
                                incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertSAPPostingData '{TransactionID}', '{result1[0].d.SalesDocumentno}', 'SendOrders()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                                incubeQuery.ExecuteNonQuery();
                            }

                            incubeQuery = new InCubeQuery(db_vms, "DELETE FROM PostingTransaction WHERE TransactionID ='" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                        }
                        else
                        {
                            res = Result.NoFileRetreived;
                            result.Append("No details found for this order ..");
                            WriteMessage("No details found for this order ..");
                        }
                    }
                    catch (Exception ex)
                    {
                        incubeQuery = new InCubeQuery(db_vms, "DELETE FROM PostingTransaction WHERE TransactionID= '" + TransactionID + "'");
                        incubeQuery.ExecuteNonQuery();
                        incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendOrders()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                        incubeQuery.ExecuteNonQuery();
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

        public override void SendTransfers() // Load PGI
        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));

                string salespersonFilter = "", TransactionID = "", DeliveryRef = "";
                int processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string responseBody = "";
                string headerData = "";

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND EmployeeID = " + Filters.EmployeeID;
                }
                string headerQuery = string.Format(@"SELECT V_POST_PGI.TransactionID,V_POST_PGI.DeliveryRef FROM V_POST_PGI 
inner join WarehouseTransaction on V_POST_PGI.TransactionID =WarehouseTransaction.TransactionID 
inner join EmployeeVehicle on EmployeeVehicle.VehicleID = WarehouseTransaction.WarehouseID
where convert(date, TransactionDate) >= '{0}' AND convert(date, TransactionDate) <= '{1}' {2} 
", Filters.FromDate.ToString("yyyy - MM - dd"), Filters.ToDate.AddDays(1).ToString("yyyy - MM - dd"), salespersonFilter);
                incubeQuery = new InCubeQuery(db_vms, headerQuery);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("PGI header query failed !!"));
                }

                DataTable dtHeader = incubeQuery.GetDataTable();
                if (dtHeader.Rows.Count == 0)
                    WriteMessage("There is no PGI to send ..");
                else
                    SetProgressMax(dtHeader.Rows.Count);

                for (int i = 0; i < dtHeader.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();
                        TransactionID = dtHeader.Rows[i]["Transactionid"].ToString();
                        ReportProgress("Sending Transaction: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(11, TransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        DeliveryRef = dtHeader.Rows[i]["DeliveryRef"].ToString();

                        string currentDate = DateTime.Now.ToString("yyyyMMdd");
                        headerData = $"'{DeliveryRef}''{currentDate}'";
                        MezzanComplexResult[] PGIResult = Tools.GetRequest<MezzanComplexResult>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_DELIVERY_SRV/PostGoodsIssue?DeliveryNumber=" + headerData, "", "", out responseBody, webHeader, cookies);
                        if (PGIResult[0].d.results[0].DeliveryNumber == null || (PGIResult[0].d.results[0].Message != null && PGIResult[0].d.results[0].Message.Trim() != "PGI has been Created" && PGIResult[0].d.results[0].Message.Trim() != "PGI already created for this Delivery No."))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (PGIResult[0].d.results[0].DeliveryNumber != null ? PGIResult[0].d.results[0].DeliveryNumber : "") + "\r\n json:" + DeliveryRef);
                            WriteMessage("Error .. \r\n" + TransactionID + " \r\n" + PGIResult[0].d.results[0].Message);

                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendTransfers()', '{DeliveryRef}_{currentDate}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + PGIResult[0].d.results[0].DeliveryNumber + "\r\n Message:" + (PGIResult[0].d.results[0].DeliveryNumber != null ? PGIResult[0].d.results[0].DeliveryNumber : "") + "\r\n json:" + DeliveryRef);
                            WriteMessage("Success, ERP No: " + PGIResult[0].d.results[0].DeliveryNumber);
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [WarehouseTransaction] SET Synchronized = 1,LPONumber='3' WHERE TransactionID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE WhDelivery SET PGIRef ='" + PGIResult[0].d.results[0].DeliveryNumber + "' WHERE TransactionID='" + TransactionID + "' AND DeliveryRef ='" + DeliveryRef + "'");
                            incubeQuery.ExecuteNonQuery();
                            incubeQuery = new InCubeQuery(db_vms, $"DELETE FROM SAP_Error WHERE TransactionID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertSAPPostingData '{TransactionID}', '{PGIResult[0].d.results[0].DeliveryNumber}', 'SendTransfers()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendTransfers()', '{headerData.Replace("'", "_")}', '{responseBody.Replace("'", "*")}'");
                        incubeQuery.ExecuteNonQuery();
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

        public void SendExchangeOld()
        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));

                string salespersonFilter = "", TransactionID = "", DivisionCode="", RetTransactionID = "", TransactionType="", CustomerCode = "", EmployeeCode = "", OrderType = "", MainItem = "", OrganizationCode = "", HDiscount = "", OrderReason = "", Reference = "", CreditExceed = "";
                int processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string responseBody = "";
                string headerData = "";
                int isFOC = -1;

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND EmployeeID = " + Filters.EmployeeID;
                }
                string headerQuery = string.Format(@"SELECT CustomerCode
      ,EmployeeCode
      ,V_POST_Exchange.Discount
      ,V_POST_Exchange.SalesTransactionID
	  ,V_POST_Exchange.ReturnTransactionID
	  ,V_POST_Exchange.TransactionTypeID
      ,OrderType
      ,MainItem
      ,OrganizationCode
      ,OrderReason
      ,Reference
      ,isFOC
      ,CreditExceed
,DivisionCode
  FROM V_POST_Exchange
    inner join [Transaction] t on t.TransactionID= V_POST_Exchange.SalesTransactionID where 1=1  AND t.TransactionDate >= '{0}' AND t.TransactionDate < '{1}' {2}", Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
                //  string headerQuery = string.Format($"select [V_POST_SalesDelivery].SalesOrderRef,EmployeeCode,OrganizationCode,[V_POST_SalesDelivery].Transactionid,[V_POST_SalesDelivery].TransactionTypeID from  [V_POST_SalesDelivery]  inner join [Transaction] t on t.TransactionID= [V_POST_SalesDelivery].TransactionID where 1=1", salespersonFilter);

                incubeQuery = new InCubeQuery(db_vms, headerQuery);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Order header query failed !!"));
                }

                DataTable dtHeader = incubeQuery.GetDataTable();
                if (dtHeader.Rows.Count == 0)
                    WriteMessage("There is no Order to send ..");
                else
                    SetProgressMax(dtHeader.Rows.Count);

                for (int i = 0; i < dtHeader.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();
                        TransactionID = dtHeader.Rows[i]["SalesTransactionID"].ToString();
                        RetTransactionID = dtHeader.Rows[i]["ReturnTransactionID"].ToString();
                        TransactionType = dtHeader.Rows[i]["TransactionTypeID"].ToString();
                        DivisionCode = dtHeader.Rows[i]["DivisionCode"].ToString();

                        ReportProgress("Sending Transaction: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(11, TransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        CustomerCode = dtHeader.Rows[i]["CustomerCode"].ToString();
                        EmployeeCode = dtHeader.Rows[i]["EmployeeCode"].ToString();
                        OrderType = dtHeader.Rows[i]["OrderType"].ToString();
                        MainItem = dtHeader.Rows[i]["MainItem"].ToString();
                        OrganizationCode = dtHeader.Rows[i]["OrganizationCode"].ToString();
                        HDiscount = decimal.Parse(dtHeader.Rows[i]["Discount"].ToString()).ToString("#0.000");
                        OrderReason = dtHeader.Rows[i]["OrderReason"].ToString();
                        Reference = dtHeader.Rows[i]["Reference"].ToString();
                        isFOC = int.Parse(dtHeader.Rows[i]["IsFOC"].ToString());
                        CreditExceed = dtHeader.Rows[i]["CreditExceed"].ToString();
                        //IIF(TD.Price<>PD.Price, TD.Price, 0) 

                        string detailsQuery= $@"
                                      

SELECT I.ItemCode, SUM(TD.Quantity) Quantity, PTL.Description, TD.Price*10 Price, IIF(TD.Price<>PD.Price,SUM(TD.Discount)*-10,SUM(TD.Discount)*-10) Discount, 0 DiscountPercentage,
												FOC.BillCancelType as MainItem
												, P.PackID
                                                FROM TransactionDetail TD
                                                INNER JOIN Pack P ON P.PackID = TD.PackID
                                                INNER JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1
                                                INNER JOIN Item I ON I.ItemID = P.ItemID
                                                INNER JOIN [transaction] T ON T.TransactionID = TD.TransactionID
                                                LEFT JOIN ConfigurationOrganization CO ON CO.OrganizationID = T.OrganizationID AND CO.KeyName = 'DefaultPriceListID'
                                                LEFT JOIN PriceDefinition PD ON PD.PackID = TD.PackID AND PD.PriceListID = CO.KeyValue
                                                INNER JOIN CustomerOutlet C ON C.CustomerID = T.CustomerID AND C.OutletID = T.OutletID
                                                LEFT JOIN V_SAP_FOCWithExchange FOC ON FOC.CustomerTypeID = IIF(C.CustomerTypeID=3 AND C.BillsOpenNumber =1,1,T.SalesMode) AND  FOC.TransactionTypeID ='{TransactionType}'
                                                WHERE TD.TransactionID = '{RetTransactionID}'
                                                GROUP BY I.ItemCode,PTL.Description,TD.Price,DiscountPercentage, PD.Price, FOC.MainItem, P.PackID,BillCancelType
Union all
SELECT I.ItemCode, SUM(TD.Quantity) Quantity, PTL.Description, TD.Price*10 Price, IIF(TD.Price<>PD.Price,SUM(TD.Discount)*-10,SUM(TD.Discount)*-10) Discount, 0 DiscountPercentage,
												FOC.MainItem as MainItem
										
												, P.PackID
                                                FROM TransactionDetail TD
                                                INNER JOIN Pack P ON P.PackID = TD.PackID
                                                INNER JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1
                                                INNER JOIN Item I ON I.ItemID = P.ItemID
                                                INNER JOIN [transaction] T ON T.TransactionID = TD.TransactionID
                                                LEFT JOIN ConfigurationOrganization CO ON CO.OrganizationID = T.OrganizationID AND CO.KeyName = 'DefaultPriceListID'
                                                LEFT JOIN PriceDefinition PD ON PD.PackID = TD.PackID AND PD.PriceListID = CO.KeyValue
                                                INNER JOIN CustomerOutlet C ON C.CustomerID = T.CustomerID AND C.OutletID = T.OutletID
                                                LEFT JOIN V_SAP_FOCWithExchange FOC ON FOC.CustomerTypeID = IIF(C.CustomerTypeID=3 AND C.BillsOpenNumber =1,1,T.SalesMode) AND FOC.TransactionTypeID ='{TransactionType}'
                                                WHERE TD.TransactionID = '{TransactionID}'
                                                GROUP BY I.ItemCode,PTL.Description,TD.Price,DiscountPercentage, PD.Price, FOC.MainItem, P.PackID
                                                ";




                      

                        incubeQuery = new InCubeQuery(db_vms, detailsQuery);
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            throw (new Exception("order details query failed !!"));
                        }

                        DataTable dtDetails = incubeQuery.GetDataTable();
                        List<object> allDetailsList = new List<object>();
                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        {
                            string ItemCode = "", UOM = "", Quantity = "", Price = "", DDiscount = "", DDiscountPercentage = "";

                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            UOM = dtDetails.Rows[j]["Description"].ToString();
                            Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString("#0.000");
                            Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString("#0.000000000");
                            DDiscount = decimal.Parse(dtDetails.Rows[j]["Discount"].ToString()).ToString("#0.000");
                            DDiscountPercentage = decimal.Parse(dtDetails.Rows[j]["DiscountPercentage"].ToString()).ToString("#0.000");
                            MainItem = dtDetails.Rows[j]["MainItem"].ToString();

                            string Plantt = "";

                            if (DivisionCode == "1350")
                            {
                                Plantt = "1351";
                            }
                            else if (DivisionCode == "1110")
                            {
                                Plantt = "1111";
                            }
                            else if (DivisionCode == "1330")
                            {
                                Plantt = "1331";
                            }
                            else if (int.TryParse(DivisionCode, out int divisionInt))
                            {
                                Plantt = (divisionInt + 1).ToString();
                            }
                            var details = new
                            {
                                Plant = Plantt,
                                SalesDocumentno = "",
                                Material = ItemCode,
                                DiscPercent = DDiscountPercentage,
                                OrderQty = Quantity,
                                DiscValue = DDiscount,
                                Price = Price, //Price,
                                ItemCategory = MainItem, //for Category all tranasaction for return and sales togther but the item category to be taken from excel provided by diab 
                                SalesUnit = UOM,
                                FocPercent = "0.000",
                                FocValue = "0.000",
                                StoreLoc = ""
                            };

                            allDetailsList.Add(details);
                        }

                        var headerDataObject = new
                        {
                            LoadProcess = "",
                            SalesDocumentno = "",
                            SoldToParty = CustomerCode,
                            StockPartner = EmployeeCode,
                            DiscPercent = "0.000",
                            OrderType = OrderType, // Depend on CustomerType and on the amount if its equals or not (abslute Value to be within the tolerance ) from configuration exchange tolerance 
                            Status = "",
                            CreditExceed = CreditExceed,
                            DiscValue = "0.000",
                            //    SalesOrg = "1930",
                            SalesOrg = DivisionCode,
                            DistributionChannel = "03",
                            Division = DivisionCode == "1090" ? "02" : "07",
                            SalesmanId = EmployeeCode,
                            //    InvoiceNo = TransactionID,
                            InvoiceNo = TransactionID, //exchangeSales 
                            Reference = Reference,
                            OrderReason = OrderReason,
                            FocPercent = "0.000",
                            FocValue = "0.000",
                            HdrItem = allDetailsList
                        };

                        headerData = JsonConvert.SerializeObject(headerDataObject);

                        incubeQuery = new InCubeQuery(db_vms, "INSERT INTO PostingTransaction (TransactionID) VALUES ('" + TransactionID + "')");
                        incubeQuery.ExecuteNonQuery();

                        MezzanSimpleResult[] salesResult = Tools.GetRequest<MezzanSimpleResult>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_SALESORDER_SRV/HeaderSet", headerData, "", out responseBody, webHeader, cookies);
                        if (salesResult == null || (salesResult[0].d.SalesDocumentno != null && salesResult[0].d.SalesDocumentno.Trim() == ""))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (salesResult[0].d.SalesDocumentno != null ? salesResult[0].d.SalesDocumentno : "") + "\r\n json:" + headerData);
                            WriteMessage("Error .. \r\n" + TransactionID + " \r\n" + salesResult[0].d.SalesDocumentno);

                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendInvoice()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + salesResult[0].d.SalesDocumentno + "\r\n Message:" + (salesResult[0].d.SalesDocumentno != null ? salesResult[0].d.SalesDocumentno : "") + "\r\n json:" + headerData);
                            WriteMessage("Success, ERP No: " + salesResult[0].d.SalesDocumentno);
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [Transaction] SET Synchronized = 1,LPONumber='1',Description='" + salesResult[0].d.SalesDocumentno + "' WHERE TransactionID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();

                            incubeQuery = new InCubeQuery(db_vms, "INSERT INTO SAP_Reference (TransactionID, SalesOrderRef) VALUES ('" + TransactionID + "','" + salesResult[0].d.SalesDocumentno + "')");
                            incubeQuery.ExecuteNonQuery();
                            incubeQuery = new InCubeQuery(db_vms, "INSERT INTO SAP_Reference (TransactionID, SalesOrderRef) VALUES ('" + RetTransactionID + "','" + salesResult[0].d.SalesDocumentno + "')");
                            incubeQuery.ExecuteNonQuery();

                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertSAPPostingData '{TransactionID}', '{salesResult[0].d.SalesDocumentno}', 'SendInvoice()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();

                            SendDelivery(RetTransactionID, "SRE", true);
                            SendPGI(RetTransactionID, "SRE", true);

                            SendDelivery(TransactionID, "SRE", true);
                            SendPGI(TransactionID, "SRE",true);

                            SendBilling(TransactionID);
                            AboodAPI(TransactionID);
                        }

                        incubeQuery = new InCubeQuery(db_vms, "DELETE FROM PostingTransaction WHERE TransactionID= '" + TransactionID + "'");
                        incubeQuery.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {

                        incubeQuery = new InCubeQuery(db_vms, "DELETE FROM PostingTransaction WHERE TransactionID= '" + TransactionID + "'");
                        incubeQuery.ExecuteNonQuery();
                        incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendInvoice()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                        incubeQuery.ExecuteNonQuery();
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
        public void SendExchange()
        {
            try
            {
                if (this.webHeader == null)
                    throw new Exception("Can't open session in API , please check the service!!");
                string str1 = "";
                string TransactionID1 = "";
                string str2 = "";
                string str3 = "";
                int ID = 0;
                StringBuilder stringBuilder = new StringBuilder();
                InCubeLibrary.Result result = InCubeLibrary.Result.UnKnown;
                string responseBody = "";
                string body = "";
                int num1 = -1;
                if (this.Filters.EmployeeID != -1)
                    str1 = "AND EmployeeID = " + this.Filters.EmployeeID.ToString();
                this.incubeQuery = new InCubeQuery(this.db_vms, $"SELECT CustomerCode\r\n      ,EmployeeCode\r\n      ,V_POST_Exchange.Discount\r\n      ,V_POST_Exchange.SalesTransactionID\r\n\t  ,V_POST_Exchange.ReturnTransactionID\r\n\t  ,V_POST_Exchange.TransactionTypeID\r\n      ,OrderType\r\n      ,MainItem\r\n      ,OrganizationCode\r\n      ,OrderReason\r\n      ,Reference\r\n      ,isFOC\r\n      ,CreditExceed\r\n\r\n  FROM V_POST_Exchange\r\n    inner join [Transaction] t on t.TransactionID= V_POST_Exchange.SalesTransactionID where 1=1  AND t.TransactionDate >= '{this.Filters.FromDate.ToString("yyyy-MM-dd")}' AND t.TransactionDate < '{this.Filters.ToDate.AddDays(1.0).ToString("yyyy-MM-dd")}' {str1}");
                DataTable dataTable1 = this.incubeQuery.Execute() == 0 ? this.incubeQuery.GetDataTable() : throw new Exception("Order header query failed !!");
                if (dataTable1.Rows.Count == 0)
                    this.WriteMessage("There is no Order to send ..");
                else
                    this.SetProgressMax(dataTable1.Rows.Count);
                for (int index1 = 0; index1 < dataTable1.Rows.Count; ++index1)
                {
                    try
                    {
                        result = InCubeLibrary.Result.UnKnown;
                        ID = 0;
                        stringBuilder = new StringBuilder();
                        TransactionID1 = dataTable1.Rows[index1]["SalesTransactionID"].ToString();
                        string TransactionID2 = dataTable1.Rows[index1]["ReturnTransactionID"].ToString();
                        string str4 = dataTable1.Rows[index1]["TransactionTypeID"].ToString();
                        this.ReportProgress("Sending Transaction: " + TransactionID1);
                        this.WriteMessage($"\r\n{TransactionID1}: ");
                        ID = this.execManager.LogIntegrationBegining(this.TriggerID, -1, new Dictionary<int, string>()
                {
                    { 11, TransactionID1 }
                });
                        string str5 = dataTable1.Rows[index1]["CustomerCode"].ToString();
                        string str6 = dataTable1.Rows[index1]["EmployeeCode"].ToString();
                        string str7 = dataTable1.Rows[index1]["OrderType"].ToString();
                        str2 = dataTable1.Rows[index1]["MainItem"].ToString();
                        string str8 = dataTable1.Rows[index1]["OrganizationCode"].ToString();
                        Decimal num2 = Decimal.Parse(dataTable1.Rows[index1]["Discount"].ToString());
                        str3 = num2.ToString("#0.000");
                        string str9 = dataTable1.Rows[index1]["OrderReason"].ToString();
                        string str10 = dataTable1.Rows[index1]["Reference"].ToString();
                        num1 = int.Parse(dataTable1.Rows[index1]["IsFOC"].ToString());
                        string str11 = dataTable1.Rows[index1]["CreditExceed"].ToString();
                        this.incubeQuery = new InCubeQuery(this.db_vms, $"\r\nSELECT I.ItemCode,Division.DivisionCode, SUM(TD.Quantity) Quantity, PTL.Description, TD.Price*10 Price, IIF(TD.Price<>PD.Price,SUM(TD.Discount)*-10,SUM(TD.Discount)*-10) Discount, 0 DiscountPercentage,\r\n\t\t\t\t\t\t\t\t\t\t\t\tFOC.BillCancelType as MainItem\r\n\t\t\t\t\t\t\t\t\t\t\t\t, P.PackID,td.SalesTransactionTypeID\r\n                                                FROM TransactionDetail TD\r\n                                                INNER JOIN Pack P ON P.PackID = TD.PackID\r\n                                                INNER JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1\r\n                                                INNER JOIN Item I ON I.ItemID = P.ItemID\r\n\t\t\t\t\t\t\t\t\t\t\t\t                                                INNER JOIN ItemCategory Ic ON Ic.ItemCategoryid = i.ItemCategoryID\r\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\tinner join Division on Division.DivisionID =Ic.DivisionID\r\n                                                INNER JOIN [transaction] T ON T.TransactionID = TD.TransactionID\r\n                                                LEFT JOIN ConfigurationOrganization CO ON CO.OrganizationID = T.OrganizationID AND CO.KeyName = 'DefaultPriceListID'\r\n                                                LEFT JOIN PriceDefinition PD ON PD.PackID = TD.PackID AND PD.PriceListID = CO.KeyValue\r\n                                                INNER JOIN CustomerOutlet C ON C.CustomerID = T.CustomerID AND C.OutletID = T.OutletID\r\n                                                LEFT JOIN V_SAP_FOCWithExchange FOC ON FOC.CustomerTypeID = IIF(C.CustomerTypeID=3 AND C.BillsOpenNumber =1,1,T.SalesMode) AND  FOC.TransactionTypeID ='{str4}'\r\n                                                WHERE TD.TransactionID = '{TransactionID2}'\r\n                                                GROUP BY I.ItemCode,PTL.Description,TD.Price,DiscountPercentage, PD.Price, FOC.MainItem, P.PackID,BillCancelType,DivisionCode,td.SalesTransactionTypeID\r\nUnion all\r\nSELECT I.ItemCode, Division.DivisionCode, SUM(TD.Quantity) Quantity, PTL.Description, TD.Price*10 Price, IIF(TD.Price<>PD.Price,SUM(TD.Discount)*-10,SUM(TD.Discount)*-10) Discount, 0 DiscountPercentage,\r\n\t\t\t\t\t\t\t\t\t\t\t\tFOC.MainItem as MainItem\r\n\t\t\t\t\t\t\t\t\t\t\t\t, P.PackID,td.SalesTransactionTypeID\r\n                                                FROM TransactionDetail TD\r\n                                                INNER JOIN Pack P ON P.PackID = TD.PackID\r\n                                                INNER JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1\r\n                                                INNER JOIN Item I ON I.ItemID = P.ItemID\r\n\t\t\t\t\t\t\t\t\t\t\t\t INNER JOIN ItemCategory Ic ON Ic.ItemCategoryid = i.ItemCategoryID\r\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\tinner join Division on Division.DivisionID =Ic.DivisionID\r\n                                                INNER JOIN [transaction] T ON T.TransactionID = TD.TransactionID\r\n                                                LEFT JOIN ConfigurationOrganization CO ON CO.OrganizationID = T.OrganizationID AND CO.KeyName = 'DefaultPriceListID'\r\n                                                LEFT JOIN PriceDefinition PD ON PD.PackID = TD.PackID AND PD.PriceListID = CO.KeyValue\r\n                                                INNER JOIN CustomerOutlet C ON C.CustomerID = T.CustomerID AND C.OutletID = T.OutletID\r\n                                                LEFT JOIN V_SAP_FOCWithExchange FOC ON FOC.CustomerTypeID = IIF(C.CustomerTypeID=3 AND C.BillsOpenNumber =1,1,T.SalesMode) AND FOC.TransactionTypeID ='{str4}'\r\n                                                WHERE TD.TransactionID = '{TransactionID1}'\r\n                                                GROUP BY I.ItemCode,PTL.Description,TD.Price,DiscountPercentage, PD.Price, FOC.MainItem, P.PackID,Division.DivisionCode,td.SalesTransactionTypeID\r\n\t\t\t\t\t\t\t\t\t\t\t\torder by td.SalesTransactionTypeID desc ,DivisionCode, i.ItemCode\r\n                                                ");
                        if (this.incubeQuery.Execute() != 0)
                        {
                            result = InCubeLibrary.Result.Failure;
                            throw new Exception("order details query failed !!");
                        }
                        DataTable dataTable2 = this.incubeQuery.GetDataTable();
                        List<object> objectList = new List<object>();
                        for (int index2 = 0; index2 < dataTable2.Rows.Count; ++index2)
                        {
                            string str12 = dataTable2.Rows[index2]["ItemCode"].ToString();
                            string str13 = dataTable2.Rows[index2]["DivisionCode"].ToString();
                            string str14 = dataTable2.Rows[index2]["Description"].ToString();
                            num2 = Decimal.Parse(dataTable2.Rows[index2]["Quantity"].ToString());
                            string str15 = num2.ToString("#0.000");
                            num2 = Decimal.Parse(dataTable2.Rows[index2]["Price"].ToString());
                            string str16 = num2.ToString("#0.000000000");
                            num2 = Decimal.Parse(dataTable2.Rows[index2]["Discount"].ToString());
                            string str17 = num2.ToString("#0.000");
                            num2 = Decimal.Parse(dataTable2.Rows[index2]["DiscountPercentage"].ToString());
                            string str18 = num2.ToString("#0.000");
                            string str19 = dataTable2.Rows[index2]["MainItem"].ToString();
                            var data = new
                            {
                                Plant = str13,
                                SalesDocumentno = "",
                                Material = str12,
                                DiscPercent = str18,
                                OrderQty = str15,
                                DiscValue = str17,
                                Price = str16,
                                ItemCategory = str19,
                                SalesUnit = str14,
                                FocPercent = "0.000",
                                FocValue = "0.000",
                                StoreLoc = ""
                            };
                            objectList.Add((object)data);
                        }
                        var data1 = new
                        {
                            LoadProcess = "",
                            SalesDocumentno = "",
                            SoldToParty = str5,
                            StockPartner = str6,
                            DiscPercent = "0.000",
                            OrderType = str7,
                            Status = "",
                            CreditExceed = str11,
                            DiscValue = "0.000",
                            SalesOrg = str8,
                            DistributionChannel = "03",
                            Division = str8 == "1030" ? "02" : "07",
                            SalesmanId = str6,
                            InvoiceNo = TransactionID1,
                            Reference = str10,
                            OrderReason = str9,
                            FocPercent = "0.000",
                            FocValue = "0.000",
                            HdrItem = objectList
                        };
                        body = JsonConvert.SerializeObject((object)data1);
                        this.incubeQuery = new InCubeQuery(this.db_vms, $"INSERT INTO PostingTransaction (TransactionID) VALUES ('{TransactionID1}')");
                        int num3 = (int)this.incubeQuery.ExecuteNonQuery();
                        MezzanSimpleResult[] request = Tools.GetRequest<MezzanSimpleResult>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_SALESORDER_SRV/HeaderSet", body, "", out responseBody, this.webHeader, this.cookies);
                        if (request == null || request[0].d.SalesDocumentno != null && request[0].d.SalesDocumentno.Trim() == "")
                        {
                            result = InCubeLibrary.Result.NoFileRetreived;
                            stringBuilder.Append($"ERP ERROR Message:{(request[0].d.SalesDocumentno != null ? request[0].d.SalesDocumentno : "")}\r\n json:{body}");
                            this.WriteMessage($"Error .. \r\n{TransactionID1} \r\n{request[0].d.SalesDocumentno}");
                            this.incubeQuery = new InCubeQuery(this.db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID1}', 'SendInvoice()', '{body}', '{responseBody.Replace("'", "*")}'");
                            int num4 = (int)this.incubeQuery.ExecuteNonQuery();
                        }
                        else
                        {
                            result = InCubeLibrary.Result.Success;
                            stringBuilder.Append($"ERP No: {request[0].d.SalesDocumentno}\r\n Message:{(request[0].d.SalesDocumentno != null ? request[0].d.SalesDocumentno : "")}\r\n json:{body}");
                            this.WriteMessage("Success, ERP No: " + request[0].d.SalesDocumentno);
                            this.incubeQuery = new InCubeQuery(this.db_vms, $"UPDATE [Transaction] SET Synchronized = 1,LPONumber='1',Description='{request[0].d.SalesDocumentno}' WHERE TransactionID = '{TransactionID1}'");
                            int num5 = (int)this.incubeQuery.ExecuteNonQuery();
                            this.incubeQuery = new InCubeQuery(this.db_vms, $"INSERT INTO SAP_Reference SELECT t.TransactionID, '{request[0].d.SalesDocumentno}', null, null, null, null, DivisionCode FROM [Transaction] t INNER JOIN ( SELECT DISTINCT DivisionCode, TransactionID FROM TransactionDetail INNER JOIN pack ON pack.PackID = TransactionDetail.PackID INNER JOIN item ON item.ItemID = pack.ItemID INNER JOIN ItemCategory ON ItemCategory.ItemCategoryID = item.ItemCategoryID INNER JOIN Division ON Division.DivisionID = ItemCategory.DivisionID ) div ON div.TransactionID = t.TransactionID WHERE t.TransactionID = '{TransactionID1}'");
                            int num6 = (int)this.incubeQuery.ExecuteNonQuery();
                            this.incubeQuery = new InCubeQuery(this.db_vms, $"INSERT INTO SAP_Reference SELECT t.TransactionID, '{request[0].d.SalesDocumentno}', null, null, null, null, DivisionCode FROM [Transaction] t INNER JOIN ( SELECT DISTINCT DivisionCode, TransactionID FROM TransactionDetail INNER JOIN pack ON pack.PackID = TransactionDetail.PackID INNER JOIN item ON item.ItemID = pack.ItemID INNER JOIN ItemCategory ON ItemCategory.ItemCategoryID = item.ItemCategoryID INNER JOIN Division ON Division.DivisionID = ItemCategory.DivisionID ) div ON div.TransactionID = t.TransactionID WHERE t.TransactionID = '{TransactionID2}'");
                            int num7 = (int)this.incubeQuery.ExecuteNonQuery();
                            this.incubeQuery = new InCubeQuery(this.db_vms, $"EXEC SP_InsertSAPPostingData '{TransactionID1}', '{request[0].d.SalesDocumentno}', 'SendInvoice()', '{body}', '{responseBody.Replace("'", "*")}'");
                            int num8 = (int)this.incubeQuery.ExecuteNonQuery();
                            this.SendDelivery(TransactionID2, "SRE", true);
                            this.SendPGI(TransactionID2, "SRE", true);
                            this.SendDelivery(TransactionID1, "SRE", true);
                            this.SendPGI(TransactionID1, "SRE", true);
                            this.SendBilling(TransactionID1);
                            this.AboodAPI(TransactionID1);
                        }
                        this.incubeQuery = new InCubeQuery(this.db_vms, $"DELETE FROM PostingTransaction WHERE TransactionID= '{TransactionID1}'");
                        int num9 = (int)this.incubeQuery.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        this.incubeQuery = new InCubeQuery(this.db_vms, $"DELETE FROM PostingTransaction WHERE TransactionID= '{TransactionID1}'");
                        int num10 = (int)this.incubeQuery.ExecuteNonQuery();
                        this.incubeQuery = new InCubeQuery(this.db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID1}', 'SendInvoice()', '{body}', '{responseBody.Replace("'", "*")}'");
                        int num11 = (int)this.incubeQuery.ExecuteNonQuery();
                        Logger.WriteLog(MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        stringBuilder.Append(ex.Message);
                        if (result == InCubeLibrary.Result.UnKnown)
                        {
                            result = InCubeLibrary.Result.Failure;
                            this.WriteMessage("Unhandled exception !!");
                        }
                    }
                    finally
                    {
                        this.execManager.LogIntegrationEnding(ID, result, "", stringBuilder.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                this.WriteMessage("Fetching order failed !!");
            }
            finally
            {
                this.CloseSession();
            }
        }


        public void SendInvoiceold()
        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));

                string salespersonFilter = "", TransactionID = "", CustomerRefOrTransactionID = "", DivisionCode = "", CustomerCode = "", EmployeeCode = "", OrderType = "", MainItem = "", OrganizationCode = "", HDiscount = "", OrderReason = "", Reference = "", CreditExceed = "";
                int processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string responseBody = "";
                string headerData = "";
                int isFOC = -1;

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND EmployeeID = " + Filters.EmployeeID;
                }
                string headerQuery = string.Format(@"SELECT DivisionCode, CustomerCode,CustomerRefOrTransactionID
      ,EmployeeCode
      ,V_POST_Sales.Discount
      ,V_POST_Sales.TransactionID
      ,OrderType
      ,MainItem
      ,OrganizationCode
      ,OrderReason
      ,Reference
      ,isFOC
      ,CreditExceed
  FROM V_POST_Sales
    inner join [Transaction] t on t.TransactionID= V_POST_Sales.TransactionID   inner join Division d on d.DivisionID = t.DivisionID
 where 1=1  AND t.TransactionDate >= '{0}' AND t.TransactionDate < '{1}' {2}", Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
                //  string headerQuery = string.Format($"select [V_POST_SalesDelivery].SalesOrderRef,EmployeeCode,OrganizationCode,[V_POST_SalesDelivery].Transactionid,[V_POST_SalesDelivery].TransactionTypeID from  [V_POST_SalesDelivery]  inner join [Transaction] t on t.TransactionID= [V_POST_SalesDelivery].TransactionID where 1=1", salespersonFilter);

                incubeQuery = new InCubeQuery(db_vms, headerQuery);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Order header query failed !!"));
                }

                DataTable dtHeader = incubeQuery.GetDataTable();
                if (dtHeader.Rows.Count == 0)
                    WriteMessage("There is no Order to send ..");
                else
                    SetProgressMax(dtHeader.Rows.Count);

                for (int i = 0; i < dtHeader.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();
                        TransactionID = dtHeader.Rows[i]["Transactionid"].ToString();
                        DivisionCode = dtHeader.Rows[i]["DivisionCode"].ToString();

                        CustomerRefOrTransactionID = dtHeader.Rows[i]["CustomerRefOrTransactionID"].ToString();

                        ReportProgress("Sending Transaction: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(11, TransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        CustomerCode = dtHeader.Rows[i]["CustomerCode"].ToString();
                        EmployeeCode = dtHeader.Rows[i]["EmployeeCode"].ToString();
                        OrderType = dtHeader.Rows[i]["OrderType"].ToString();
                        MainItem = dtHeader.Rows[i]["MainItem"].ToString();
                        OrganizationCode = dtHeader.Rows[i]["OrganizationCode"].ToString();
                        HDiscount = decimal.Parse(dtHeader.Rows[i]["Discount"].ToString()).ToString("#0.000");
                        OrderReason = dtHeader.Rows[i]["OrderReason"].ToString();
                        Reference = dtHeader.Rows[i]["Reference"].ToString();
                        isFOC = int.Parse(dtHeader.Rows[i]["IsFOC"].ToString());
                        CreditExceed = dtHeader.Rows[i]["CreditExceed"].ToString();
                        //IIF(TD.Price<>PD.Price, TD.Price, 0) 
                        string detailsQuery = $@"SELECT I.ItemCode, SUM(TD.Quantity) Quantity, PTL.Description, TD.Price * 10 Price  , IIF(TD.Price<>PD.Price,(SUM(TD.Discount)*-1)*10,SUM(TD.Discount)*-10) Discount, 0 DiscountPercentage
                                                ,CASE WHEN {isFOC} = 2 AND TD.Price = 0 THEN FOC.MainItem ELSE '{MainItem}' END MainItem, P.PackID
                                                FROM TransactionDetail TD
                                                INNER JOIN Pack P ON P.PackID = TD.PackID
                                                INNER JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1
                                                INNER JOIN Item I ON I.ItemID = P.ItemID
                                                INNER JOIN [transaction] T ON T.TransactionID = TD.TransactionID
                                                LEFT JOIN ConfigurationOrganization CO ON CO.OrganizationID = T.OrganizationID AND CO.KeyName = 'DefaultPriceListID'
                                                LEFT JOIN PriceDefinition PD ON PD.PackID = TD.PackID AND PD.PriceListID = CO.KeyValue
                                                INNER JOIN CustomerOutlet C ON C.CustomerID = T.CustomerID AND C.OutletID = T.OutletID
                                                LEFT JOIN V_SAP_FOCWithSales FOC ON FOC.CustomerTypeID = IIF(C.CustomerTypeID=3 AND C.BillsOpenNumber =1,1,T.SalesMode) AND FOC.TransactionTypeID = T.TransactionTypeID
                                                WHERE TD.TransactionID = '{TransactionID}'
                                                GROUP BY I.ItemCode,PTL.Description,TD.Price,DiscountPercentage, PD.Price, FOC.MainItem, P.PackID,td.SalesTransactionTypeID
                                                Order by td.SalesTransactionTypeID ,I.ItemCode, P.PackID";

                        incubeQuery = new InCubeQuery(db_vms, detailsQuery);
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            throw (new Exception("order details query failed !!"));
                        }

                        DataTable dtDetails = incubeQuery.GetDataTable();
                        List<object> allDetailsList = new List<object>();
                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        {
                            string ItemCode = "", UOM = "", Quantity = "", Price = "", DDiscount = "", DDiscountPercentage = "";

                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            UOM = dtDetails.Rows[j]["Description"].ToString();
                            Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString("#0.000");
                            Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString("#0.000000000");
                            DDiscount = decimal.Parse(dtDetails.Rows[j]["Discount"].ToString()).ToString("#0.000");
                            DDiscountPercentage = decimal.Parse(dtDetails.Rows[j]["DiscountPercentage"].ToString()).ToString("#0.000");
                            MainItem = dtDetails.Rows[j]["MainItem"].ToString();

                            string Plantt = "";

                            if (DivisionCode == "1350")
                            {
                                Plantt = "1351";
                            }
                            else if (DivisionCode == "1110")
                            {
                                Plantt = "1111";
                            }
                            else if (DivisionCode == "1330")
                            {
                                Plantt = "1331";
                            }
                            else if (DivisionCode == "1090")
                            {
                                Plantt = "1091";
                            }
                            else if (int.TryParse(DivisionCode, out int divisionInt))
                            {
                                Plantt = (divisionInt + 1).ToString();
                            }

                            var details = new
                            {
                                Plant = Plantt,
                                SalesDocumentno = "",
                                Material = ItemCode,
                                DiscPercent = DDiscountPercentage,
                                OrderQty = Quantity,
                                DiscValue = DDiscount,
                                Price = Price, //Price,
                                ItemCategory = MainItem, //for Category all tranasaction for return and sales togther but the item category to be taken from excel provided by diab 
                                SalesUnit = UOM,
                                FocPercent = "0.000",
                                FocValue = "0.000",
                                StoreLoc = ""
                            };

                            allDetailsList.Add(details);
                        }

                        var headerDataObject = new
                        {
                            LoadProcess = "",
                            SalesDocumentno = "",
                            SoldToParty = CustomerCode,
                            StockPartner = EmployeeCode,
                            DiscPercent = "0.000",
                            OrderType = OrderType, // Depend on CustomerType and on the amount if its equals or not (abslute Value to be within the tolerance ) from configuration exchange tolerance 
                            Status = "",
                            CreditExceed = CreditExceed,
                            DiscValue = "0.000",
                            SalesOrg = DivisionCode,

                            DistributionChannel = "03",

                            Division = DivisionCode == "1090" ? "02" : "07",
                            SalesmanId = EmployeeCode,
                            //    InvoiceNo = TransactionID,
                            InvoiceNo = CustomerRefOrTransactionID, //exchangeSales 
                            Reference = Reference,
                            OrderReason = OrderReason,
                            FocPercent = "0.000",
                            FocValue = "0.000",
                            HdrItem = allDetailsList
                        };

                        headerData = JsonConvert.SerializeObject(headerDataObject);

                        incubeQuery = new InCubeQuery(db_vms, "INSERT INTO PostingTransaction (TransactionID) VALUES ('" + TransactionID + "')");
                        incubeQuery.ExecuteNonQuery();

                        MezzanSimpleResult[] salesResult = Tools.GetRequest<MezzanSimpleResult>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_SALESORDER_SRV/HeaderSet", headerData, "", out responseBody, webHeader, cookies);
                        if (salesResult == null || (salesResult[0].d.SalesDocumentno != null && salesResult[0].d.SalesDocumentno.Trim() == ""))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (salesResult[0].d.SalesDocumentno != null ? salesResult[0].d.SalesDocumentno : "") + "\r\n json:" + headerData);
                            WriteMessage("Error .. \r\n" + TransactionID + " \r\n" + salesResult[0].d.SalesDocumentno);

                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendInvoice()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + salesResult[0].d.SalesDocumentno + "\r\n Message:" + (salesResult[0].d.SalesDocumentno != null ? salesResult[0].d.SalesDocumentno : "") + "\r\n json:" + headerData);
                            WriteMessage("Success, ERP No: " + salesResult[0].d.SalesDocumentno);
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [Transaction] SET Synchronized = 1,LPONumber='1',Description='" + salesResult[0].d.SalesDocumentno + "' WHERE TransactionID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();

                            incubeQuery = new InCubeQuery(db_vms, "INSERT INTO SAP_Reference (TransactionID, SalesOrderRef) VALUES ('" + TransactionID + "','" + salesResult[0].d.SalesDocumentno + "')");
                            incubeQuery.ExecuteNonQuery();

                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertSAPPostingData '{TransactionID}', '{salesResult[0].d.SalesDocumentno}', 'SendInvoice()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                            SendDelivery(TransactionID, "SRE", false);
                        }

                        incubeQuery = new InCubeQuery(db_vms, "DELETE FROM PostingTransaction WHERE TransactionID= '" + TransactionID + "'");
                        incubeQuery.ExecuteNonQuery();
                    }

                    catch (Exception ex)
                    {

                        incubeQuery = new InCubeQuery(db_vms, "DELETE FROM PostingTransaction WHERE TransactionID= '" + TransactionID + "'");
                        incubeQuery.ExecuteNonQuery();
                        incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendInvoice()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                        incubeQuery.ExecuteNonQuery();
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

        public void SendInvoice()
        {
            try
            {
                if (webHeader == null)
                    throw new Exception("Can't open session in API, please check the service!!");

                string salespersonFilter = "";
                string transactionID = "";
                string discount = "";
                int processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string responseBody = "";
                string headerData = "";

                if (Filters.EmployeeID != -1)
                    salespersonFilter = "AND EmployeeID = " + Filters.EmployeeID;

                // --- Fetch header (invoice) records ---
                string headerQuery = $@"
SELECT CustomerCode, CustomerRefOrTransactionID, EmployeeCode, V_POST_Sales.Discount,
       V_POST_Sales.TransactionID, OrderType, MainItem, OrganizationCode, OrderReason,
       Reference, IsFOC, CreditExceed
FROM V_POST_Sales
INNER JOIN [Transaction] t ON t.TransactionID = V_POST_Sales.TransactionID
WHERE 1=1  
  AND t.TransactionDate >= '{Filters.FromDate:yyyy-MM-dd}'
  AND t.TransactionDate < '{Filters.ToDate.AddDays(1):yyyy-MM-dd}'
  {salespersonFilter}";

                incubeQuery = new InCubeQuery(db_vms, headerQuery);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                    throw new Exception("Order header query failed !!");

                DataTable dtHeader = incubeQuery.GetDataTable();

                if (dtHeader.Rows.Count == 0)
                {
                    WriteMessage("There is no Order to send ..");
                }
                else
                {
                    SetProgressMax(dtHeader.Rows.Count);
                }

                // --- Loop through each transaction ---
                for (int i = 0; i < dtHeader.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();

                        transactionID = dtHeader.Rows[i]["TransactionID"].ToString();
                        string customerRef = dtHeader.Rows[i]["CustomerRefOrTransactionID"].ToString();

                        ReportProgress("Sending Transaction: " + transactionID);
                        WriteMessage($"\r\n{transactionID}: ");

                        processID = execManager.LogIntegrationBegining(TriggerID, -1,
                            new Dictionary<int, string> { { 11, transactionID } });

                        string customerCode = dtHeader.Rows[i]["CustomerCode"].ToString();
                        string employeeCode = dtHeader.Rows[i]["EmployeeCode"].ToString();
                        string orderType = dtHeader.Rows[i]["OrderType"].ToString();
                        string mainItem = dtHeader.Rows[i]["MainItem"].ToString();
                        string organizationCode = dtHeader.Rows[i]["OrganizationCode"].ToString();
                        discount = decimal.Parse(dtHeader.Rows[i]["Discount"].ToString()).ToString("#0.000");
                        string orderReason = dtHeader.Rows[i]["OrderReason"].ToString();
                        string reference = dtHeader.Rows[i]["Reference"].ToString();
                        int isFOC = int.Parse(dtHeader.Rows[i]["IsFOC"].ToString());
                        string creditExceed = dtHeader.Rows[i]["CreditExceed"].ToString();

                        // --- Fetch transaction details ---
                        string detailsQuery = $@"
SELECT I.ItemCode, SUM(TD.Quantity) AS Quantity, Division.DivisionCode, PTL.Description,
       IIF( T.OrganizationID=9, ROUND(TD.Price, 2)   ,TD.Price * 10 )AS Price,
       IIF(T.OrganizationID=9, (SUM(TD.Discount) * -1) , SUM(TD.Discount) * -10)   AS Discount,
       0 AS DiscountPercentage,
       CASE WHEN {isFOC} = 2 AND TD.Price = 0 THEN FOC.MainItem ELSE '{mainItem}' END AS MainItem,
       P.PackID
FROM TransactionDetail TD
INNER JOIN Pack P ON P.PackID = TD.PackID
INNER JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1
INNER JOIN Item I ON I.ItemID = P.ItemID
INNER JOIN ItemCategory Ic ON Ic.ItemCategoryID = I.ItemCategoryID
INNER JOIN Division ON Division.DivisionID = Ic.DivisionID
INNER JOIN [Transaction] T ON T.TransactionID = TD.TransactionID
LEFT JOIN ConfigurationOrganization CO ON CO.OrganizationID = T.OrganizationID AND CO.KeyName = 'DefaultPriceListID'
LEFT JOIN PriceDefinition PD ON PD.PackID = TD.PackID AND PD.PriceListID = CO.KeyValue
INNER JOIN CustomerOutlet C ON C.CustomerID = T.CustomerID AND C.OutletID = T.OutletID
LEFT JOIN V_SAP_FOCWithSales FOC ON FOC.CustomerTypeID = IIF(C.CustomerTypeID=3 AND C.BillsOpenNumber=1,1,T.SalesMode)
  AND FOC.TransactionTypeID = T.TransactionTypeID
WHERE TD.TransactionID = '{transactionID}'
GROUP BY I.ItemCode, PTL.Description, TD.Price, DiscountPercentage, PD.Price, 
         FOC.MainItem, P.PackID, TD.SalesTransactionTypeID, Division.DivisionCode,T.OrganizationID
ORDER BY TD.SalesTransactionTypeID, Division.DivisionCode, I.ItemCode, P.PackID
";

                        incubeQuery = new InCubeQuery(db_vms, detailsQuery);

                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            throw new Exception("Order details query failed !!");
                        }

                        DataTable dtDetails = incubeQuery.GetDataTable();

                        // --- Build invoice item list ---
                        var invoiceItems = new List<object>();

                        foreach (DataRow row in dtDetails.Rows)
                        {
                            string itemCode = row["ItemCode"].ToString();
                            string divisionCode = row["DivisionCode"].ToString();
                            string description = row["Description"].ToString();
                            string qty = decimal.Parse(row["Quantity"].ToString()).ToString("#0.000");
                            string price = decimal.Parse(row["Price"].ToString()).ToString("#0.000000000");
                            string discValue = decimal.Parse(row["Discount"].ToString()).ToString("#0.000");
                            string discPercent = decimal.Parse(row["DiscountPercentage"].ToString()).ToString("#0.000");
                            string itemMain = row["MainItem"].ToString();

                            var itemObj = new
                            {
                                Plant = divisionCode,
                                SalesDocumentno = "",
                                Material = itemCode,
                                DiscPercent = discPercent,
                                OrderQty = qty,
                                DiscValue = discValue,
                                Price = price,
                                ItemCategory = itemMain,
                                SalesUnit = description,
                                FocPercent = "0.000",
                                FocValue = "0.000",
                                StoreLoc = ""
                            };

                            invoiceItems.Add(itemObj);
                        }

                        // --- Build header data JSON ---
                        var headerObject = new
                        {
                            LoadProcess = "",
                            SalesDocumentno = "",
                            SoldToParty = customerCode,
                            StockPartner = employeeCode,
                            DiscPercent = "0.000",
                            OrderType = orderType,
                            Status = "",
                            CreditExceed = creditExceed,
                            DiscValue = "0.000",
                            SalesOrg = organizationCode,
                            DistributionChannel = "03",
                            Division = organizationCode == "1030" ? "02" : "07",
                            SalesmanId = employeeCode,
                            InvoiceNo = customerRef,
                            Reference = reference,
                            OrderReason = orderReason,
                            FocPercent = "0.000",
                            FocValue = "0.000",
                            HdrItem = invoiceItems
                        };

                        headerData = JsonConvert.SerializeObject(headerObject);

                        // --- Insert temporary posting record ---
                        incubeQuery = new InCubeQuery(db_vms, $"INSERT INTO PostingTransaction (TransactionID) VALUES ('{transactionID}')");
                        incubeQuery.ExecuteNonQuery();

                        // --- Send to SAP / API ---
                        var apiResult = Tools.GetRequest<MezzanSimpleResult>(
                            CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_SALESORDER_SRV/HeaderSet",
                            headerData,
                            "",
                            out responseBody,
                            webHeader,
                            cookies);

                        bool invalidResponse = apiResult == null ||
                                               apiResult[0]?.d?.SalesDocumentno == null ||
                                               apiResult[0].d.SalesDocumentno.Trim() == "";

                        if (invalidResponse)
                        {
                            res = Result.NoFileRetreived;
                            result.Append($"ERP ERROR Message:{(apiResult?[0]?.d?.SalesDocumentno ?? "")}\r\n json:{headerData}");
                            WriteMessage($"Error .. \r\n{transactionID} \r\n{apiResult?[0]?.d?.SalesDocumentno}");

                            incubeQuery = new InCubeQuery(db_vms,
                                $"EXEC SP_InsertUpdateErrors '{transactionID}', 'SendInvoice()', '{EscapeForSql(headerData)}', '{EscapeForSql(responseBody)}'");
                            incubeQuery.ExecuteNonQuery();
                        }
                        else
                        {
                            // --- Success path ---
                            res = Result.Success;
                            string sapDoc = apiResult[0].d.SalesDocumentno;

                            result.Append($"ERP No: {sapDoc}\r\n Message:{sapDoc}\r\n json:{headerData}");
                            WriteMessage("Success, ERP No: " + sapDoc);

                            // Update and log
                            incubeQuery = new InCubeQuery(db_vms,
                                $"UPDATE [Transaction] SET Synchronized = 1, LPONumber='1', Description='{sapDoc}' WHERE TransactionID = '{transactionID}'");
                            incubeQuery.ExecuteNonQuery();

                            incubeQuery = new InCubeQuery(db_vms, $@"
INSERT INTO SAP_Reference
SELECT t.TransactionID, '{sapDoc}', null, null, null, null, DivisionCode
FROM [Transaction] t
INNER JOIN (
    SELECT DISTINCT DivisionCode, TransactionID
    FROM TransactionDetail
    INNER JOIN Pack ON Pack.PackID = TransactionDetail.PackID
    INNER JOIN Item ON Item.ItemID = Pack.ItemID
    INNER JOIN ItemCategory ON ItemCategory.ItemCategoryID = Item.ItemCategoryID
    INNER JOIN Division ON Division.DivisionID = ItemCategory.DivisionID
) div ON div.TransactionID = t.TransactionID
WHERE t.TransactionID = '{transactionID}'");
                            incubeQuery.ExecuteNonQuery();

                            incubeQuery = new InCubeQuery(db_vms,
                                $"EXEC SP_InsertSAPPostingData '{transactionID}', '{sapDoc}', 'SendInvoice()', '{EscapeForSql(headerData)}', '{EscapeForSql(responseBody)}'");
                            incubeQuery.ExecuteNonQuery();

                            // Trigger delivery
                            SendDelivery(transactionID, "SRE", false);
                        }

                        // Clean up
                        incubeQuery = new InCubeQuery(db_vms, $"DELETE FROM PostingTransaction WHERE TransactionID = '{transactionID}'");
                        incubeQuery.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        // rollback posting transaction
                        incubeQuery = new InCubeQuery(db_vms, $"DELETE FROM PostingTransaction WHERE TransactionID = '{transactionID}'");
                        incubeQuery.ExecuteNonQuery();

                        incubeQuery = new InCubeQuery(db_vms,
                            $"EXEC SP_InsertUpdateErrors '{transactionID}', 'SendInvoice()', '{EscapeForSql(headerData)}', '{EscapeForSql(responseBody)}'");
                        incubeQuery.ExecuteNonQuery();

                        Logger.WriteLog(MethodBase.GetCurrentMethod().DeclaringType.Name,
                                        MethodBase.GetCurrentMethod().Name,
                                        ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);

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
                Logger.WriteLog(MethodBase.GetCurrentMethod().DeclaringType.Name,
                                MethodBase.GetCurrentMethod().Name,
                                ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("Fetching order failed !!");
            }
            finally
            {
                CloseSession();
            }
        }


public void SendDelivery(string TransactionID, string TransactionType, bool IsExch)
    {
        try
        {
            if (this.webHeader == null)
                throw new Exception("Can't open session in API, please check the service!!");

            string str1 = "";
            string str2 = "";
            int ID = 0;
            StringBuilder stringBuilder = new StringBuilder();
            InCubeLibrary.Result result = InCubeLibrary.Result.UnKnown;
            string responseBody = "";
            string body = "";

            if (this.Filters.EmployeeID != -1)
                str1 = "AND EmployeeID = " + this.Filters.EmployeeID.ToString();

            string queryString1;
            if (TransactionType == "WH")
            {
                DateTime dateTime = this.Filters.FromDate;
                string str3 = dateTime.ToString("yyyy-MM-dd");
                dateTime = this.Filters.ToDate;
                dateTime = dateTime.AddDays(1.0);
                string str4 = dateTime.ToString("yyyy-MM-dd");
                string str5 = str1;
                queryString1 = $"SELECT Divisioncode, SalesOrderRef, V_POST_OffloadDelivery.Employeecode,OrganizationCode,V_POST_OffloadDelivery.TransactionID,V_POST_OffloadDelivery.TransactionTypeID FROM V_POST_OffloadDelivery \r\n\t\r\n\twhere  convert(date,TransactionDate) >= '{str3}' AND convert(date,TransactionDate) <= '{str4}' {str5}\r\n\torder by V_POST_OffloadDelivery.TransactionID, V_POST_OffloadDelivery.Divisioncode";
            }
            else if (!IsExch)
            {
                DateTime dateTime = this.Filters.FromDate;
                string str6 = dateTime.ToString("yyyy-MM-dd");
                dateTime = this.Filters.ToDate;
                dateTime = dateTime.AddDays(1.0);
                string str7 = dateTime.ToString("yyyy-MM-dd");
                string str8 = str1;
                queryString1 = $"SELECT Divisioncode, SalesOrderRef,V_POST_SalesDelivery.EmployeeCode,OrganizationCode,V_POST_SalesDelivery.TransactionID,V_POST_SalesDelivery.TransactionTypeID  FROM\tV_POST_SalesDelivery\r\ninner join [Transaction] on V_POST_SalesDelivery.TransactionID= [Transaction].TransactionID   where   convert(date,TransactionDate) >= '{str6}' AND convert(date,TransactionDate) <= '{str7}' {str8} order by V_POST_SalesDelivery.TransactionID, Divisioncode ";
            }
            else
            {
                queryString1 = $"SELECT Divisioncode,  SalesOrderRef,V_POST_SalesDelivery.EmployeeCode,OrganizationCode,V_POST_SalesDelivery.TransactionID,V_POST_SalesDelivery.TransactionTypeID  FROM\tV_POST_SalesDelivery\r\ninner join [Transaction] on V_POST_SalesDelivery.TransactionID= [Transaction].TransactionID  where [Transaction].Transactionid='{TransactionID}' order by V_POST_SalesDelivery.TransactionID, Divisioncode  ";
            }

            this.incubeQuery = new InCubeQuery(this.db_vms, queryString1);
            DataTable dataTable1;
            if (this.incubeQuery.Execute() == 0)
            {
                dataTable1 = this.incubeQuery.GetDataTable();
            }
            else
            {
                throw new Exception("delivery header query failed !!");
            }

            if (dataTable1.Rows.Count == 0)
                this.WriteMessage("There is no Order to send ..");
            else
                this.SetProgressMax(dataTable1.Rows.Count);

            for (int index1 = 0; index1 < dataTable1.Rows.Count; ++index1)
            {
                try
                {
                    result = InCubeLibrary.Result.UnKnown;
                    ID = 0;
                    stringBuilder = new StringBuilder();
                    string str9 = dataTable1.Rows[index1]["SalesOrderRef"].ToString();
                    string str10 = dataTable1.Rows[index1]["Divisioncode"].ToString();
                    this.ReportProgress("Sending Delivery: " + str9);
                    this.WriteMessage($"\r\n{str9}: ");
                    ID = this.execManager.LogIntegrationBegining(this.TriggerID, -1, new Dictionary<int, string>()
                {
                    { 11, str9 }
                });
                    string str11 = dataTable1.Rows[index1]["EmployeeCode"].ToString();
                    str2 = dataTable1.Rows[index1]["OrganizationCode"].ToString();
                    TransactionID = dataTable1.Rows[index1]["TransactionID"].ToString();
                    int int32_1 = Convert.ToInt32(dataTable1.Rows[index1]["TransactionTypeID"]);

                    string queryString2;
                    if (TransactionType == "WH")
                    {
                        queryString2 = $"\t\tselect * from( SELECT FORMAT(ROW_NUMBER() OVER (ORDER BY d.DivisionCode, I.ItemCode), '000000') LineNum,d.DivisionCode ,I.ItemCode, SUM(TD.Quantity) Quantity, TD.PackID, 0 Price, count(1) cnt, P.Barcode\r\n                                                FROM WhTransDetail TD\r\n                                                INNER JOIN Pack P ON P.PackID = TD.PackID\r\n                                                INNER JOIN Item I ON I.ItemID = P.ItemID\r\n\t\t\t\t\t\t\t\t\t\t\t\tINNER JOIN ItemCategory Ic ON Ic.ItemCategoryID = i.ItemCategoryID\r\n\t\t\t\t\t\t\t\t\t\t\t\tINNER JOIN Division d ON d.DivisionID = ic.DivisionID\r\n                                                WHERE TD.TransactionID = '{TransactionID}' \r\n                                                GROUP BY I.ItemCode, TD.PackID, P.Barcode,d.DivisionCode)u\r\n                                                where u.DivisionCode='{str10}'";
                    }
                    else
                    {
                        queryString2 = $"select * from\r\n(SELECT FORMAT(ROW_NUMBER() OVER (ORDER BY  td.SalesTransactionTypeID,d.DivisionCode,I.ItemCode) + case when (select TransactionTypeID from [Transaction] where TransactionID = '{TransactionID}') = 3 then (select count(distinct PackID) ReturnItemsCount from [TransactionDetail] where Transactionid in (select Transactionid from   [Transaction] where SourceTransactionID ='{TransactionID}')) else 0 end, '000000') LineNum,\r\nI.ItemCode, SUM(TD.Quantity) Quantity, TD.PackID, TD.Price, count(1) cnt, P.Barcode,d.DivisionCode\r\nFROM TransactionDetail TD\r\nINNER JOIN Pack P ON P.PackID = TD.PackID\r\nINNER JOIN Item I ON I.ItemID = P.ItemID\r\nINNER JOIN ItemCategory Ic ON Ic.ItemCategoryID = i.ItemCategoryID\r\nINNER JOIN Division d ON d.DivisionID = ic.DivisionID\r\nWHERE TD.TransactionID = '{TransactionID}'\r\nGROUP BY I.ItemCode, TD.PackID, TD.Price, P.Barcode ,td.SalesTransactionTypeID,d.DivisionCode\r\n) D where d.DivisionCode= '{str10}'\r\n\r\n";
                    }

                    this.incubeQuery = new InCubeQuery(this.db_vms, queryString2);
                    if (this.incubeQuery.Execute() != 0)
                    {
                        result = InCubeLibrary.Result.Failure;
                        throw new Exception("delivery details query failed !!");
                    }
                    DataTable dataTable2 = this.incubeQuery.GetDataTable();
                    List<object> objectList1 = new List<object>();

                    for (int index2 = 0; index2 < dataTable2.Rows.Count; ++index2)
                    {
                        string str12 = dataTable2.Rows[index2]["ItemCode"].ToString();
                        string str13 = dataTable2.Rows[index2]["DivisionCode"].ToString();
                        string str14 = dataTable2.Rows[index2]["LineNum"].ToString();
                        string str15 = Decimal.Parse(dataTable2.Rows[index2]["Quantity"].ToString()).ToString("#0.000");
                        string str16 = dataTable2.Rows[index2]["PackID"].ToString();
                        string str17 = Decimal.Parse(dataTable2.Rows[index2]["Price"].ToString()).ToString("#0.000000000");
                        int int32_2 = Convert.ToInt32(dataTable2.Rows[index2]["cnt"]);
                        string str18 = dataTable2.Rows[index2]["Barcode"].ToString();

                        string queryString3;
                        if (int32_2 > 1)
                        {
                            if (int32_1 == 0)
                            {
                                queryString3 = $"select distinct WTD.Quantity * p.Quantity Quantity, case when WT.TransactionTypeID= 2 and WTD.BatchNo= '1990/01/01' and    wtd.WarehouseID<> -1 then '8888888888' when  (WT.TransactionTypeID= 2 and wtd.WarehouseID=-1 and WTD.BatchNo= '1990/01/01')  then '9999999999' when   WT.TransactionTypeID= 6 then '9999999999' else ISNULL(STD.BatchNo,WTD.BatchNo)  end as BatchNo , uom.Barcode from warehouseTransaction WT\r\n                                                    INNER JOIN WHTransDetail WTD ON WTD.TransactionID = WT.TransactionID\r\n\r\n                                                    inner join pack p on p.packid = wtd.packid\r\n                                                    inner join pack uom on uom.itemid = p.itemid and uom.Quantity = 1 and uom.barcode = p.GTIN\r\n                                                    LEFT JOIN [Transaction] T ON T.RouteHistoryID = WT.RouteHistoryID and T.TransactionTypeID = 4\r\n                                                    LEFT JOIN TransactionDetail TD ON TD.TransactionID = T.TransactionID AND TD.PackID = WTD.PackID AND TD.BatchNo = WTD.BatchNo AND TD.Quantity = WTD.Quantity AND TD.ExpiryDate = WTD.ExpiryDate\r\n                                                    LEFT JOIN [Transaction] ST ON ST.TransactionID = T.SourceTransactionID\r\n                                                    LEFT JOIN TransactionDetail STD ON STD.TransactionID = ST.TransactionID AND STD.PackID = TD.PackID AND STD.Quantity = TD.Quantity\r\n                                                    where wt.transactionid ='{TransactionID}' AND WTD.PackID = {str16}";
                            }
                            else
                            {
                                queryString3 = $"SELECT td.Quantity * p.Quantity Quantity, CASE\r\n        WHEN SalesTransactionTypeID in (5,6,8,9,10) THEN\r\n            CASE\r\n                WHEN PackStatusID in (select StatusID from PackStatus where ReSellable=1) THEN '8888888888'\r\n                ELSE '9999999999'\r\n            END\r\n        when BatchNo ='1990/01/01' and SalesTransactionTypeID in (1,2,3,4,5,6,7,8)  then '8888888888'\r\n        ELSE BatchNo\r\n\r\n    END AS BatchNo, uom.Barcode \r\n                                                        FROM {(TransactionType == "WH" ? "WhTransDetail" : "TransactionDetail")} td \r\n                                                        inner join pack p on p.packid = td.packid\r\n                                                        inner join pack uom on uom.itemid = p.itemid and uom.Quantity = 1 and uom.barcode = p.GTIN\r\n                                                        WHERE TransactionID = '{TransactionID}' AND td.PackID = {str16} and Price = {str17}";
                            }
                        }
                        else if (int32_1 == 0)
                        {
                            queryString3 = $"select distinct WTD.Quantity, case when WT.TransactionTypeID= 2 and WTD.BatchNo= '1990/01/01' and    wtd.WarehouseID<> -1 then '8888888888' when  (WT.TransactionTypeID= 2 and wtd.WarehouseID=-1 and WTD.BatchNo= '1990/01/01')  then '9999999999' when   WT.TransactionTypeID= 6 then '9999999999' else ISNULL(STD.BatchNo,WTD.BatchNo)  end as BatchNo , '{str18}' barcode from warehouseTransaction WT\r\n                                                    INNER JOIN WHTransDetail WTD ON WTD.TransactionID = WT.TransactionID\r\n                                                    LEFT JOIN [Transaction] T ON T.RouteHistoryID = WT.RouteHistoryID and T.TransactionTypeID = 4\r\n                                                    LEFT JOIN TransactionDetail TD ON TD.TransactionID = T.TransactionID AND TD.PackID = WTD.PackID AND TD.BatchNo = WTD.BatchNo AND TD.Quantity = WTD.Quantity AND TD.ExpiryDate = WTD.ExpiryDate\r\n                                                    LEFT JOIN [Transaction] ST ON ST.TransactionID = T.SourceTransactionID\r\n                                                    LEFT JOIN TransactionDetail STD ON STD.TransactionID = ST.TransactionID AND STD.PackID = TD.PackID AND STD.Quantity = TD.Quantity\r\n                                                    where wt.transactionid ='{TransactionID}' AND WTD.PackID = {str16}";
                        }
                        else
                        {
                            queryString3 = $"SELECT Quantity, CASE\r\n        WHEN SalesTransactionTypeID in (5,6,8,9,10) THEN\r\n            CASE\r\n                WHEN PackStatusID in (select StatusID from PackStatus where ReSellable=1) THEN '8888888888'\r\n                ELSE '9999999999'\r\n            END\r\n       when BatchNo ='1990/01/01' and SalesTransactionTypeID in  (1,2,3,4,5,6,7,8)    then '8888888888'\r\n        ELSE BatchNo\r\n    END AS BatchNo, '{str18}' barcode FROM {(TransactionType == "WH" ? "WhTransDetail" : "TransactionDetail")} WHERE TransactionID = '{TransactionID}' AND PackID = {str16} and Price = {str17}";
                        }

                        this.incubeQuery = new InCubeQuery(this.db_vms, queryString3);
                        if (this.incubeQuery.Execute() != 0)
                        {
                            result = InCubeLibrary.Result.Failure;
                            throw new Exception("delivery batch query failed !!");
                        }
                        DataTable dataTable3 = this.incubeQuery.GetDataTable();
                        List<object> objectList2 = new List<object>();

                        for (int index3 = 0; index3 < dataTable3.Rows.Count; ++index3)
                        {
                            string str19 = dataTable3.Rows[index3]["BatchNo"].ToString();
                            string str20 = Decimal.Parse(dataTable3.Rows[index3]["Quantity"].ToString()).ToString("#0.000");
                            string str21 = dataTable3.Rows[index3]["barcode"].ToString();
                            var batchData = new
                            {
                                ItemNumber = str14,
                                BatchNumber = str19,
                                Quantity = str20,
                                SalesUOM = str21
                            };
                            objectList2.Add(batchData);
                        }

                        var itemData = new
                        {
                            ItemNumber = str14,
                            MaterialNumber = str12,
                            Plant = str13,
                            Quantity = str15,
                            DeliveryNumber = "",
                            SalesUOM = str18,
                            ItemToBatch = objectList2
                        };
                        objectList1.Add(itemData);
                    }

                    var headerData = new
                    {
                        SalesOrderNumber = str9,
                        StockPartner = str11,
                        ShippingPoint = "",
                        HeaderToItem = objectList1
                    };

                    body = JsonConvert.SerializeObject(headerData);
                    MezzanComplexResult[] request = Tools.GetRequest<MezzanComplexResult>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_DELIVERY_SRV/HeaderSet", body, "", out responseBody, this.webHeader, this.cookies);

                    if (request[0].d.HeaderToItem.results[0].DeliveryNumber == null || request[0].d.HeaderToItem.results[0].Message != null && request[0].d.HeaderToItem.results[0].Message.Trim().ToLower() != "delivery and batch created")
                    {
                        result = InCubeLibrary.Result.NoFileRetreived;
                        stringBuilder.Append($"ERP ERROR Message:{(request[0].d.HeaderToItem.results[0].DeliveryNumber != null ? request[0].d.HeaderToItem.results[0].DeliveryNumber : "")}\r\n json:{body}");
                        this.WriteMessage($"Error .. \r\n{TransactionID} \r\n{request[0].d.HeaderToItem.results[0].DeliveryNumber}");
                        this.incubeQuery = new InCubeQuery(this.db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendDeliver()', '{body}', '{responseBody.Replace("'", "*")}'");
                        int num = (int)this.incubeQuery.ExecuteNonQuery();
                    }
                    else
                    {
                        result = InCubeLibrary.Result.Success;
                        stringBuilder.Append($"ERP No: {request[0].d.HeaderToItem.results[0].DeliveryNumber}\r\n Message:{(request[0].d.HeaderToItem.results[0].Message != null ? request[0].d.HeaderToItem.results[0].Message : "")}\r\n json:{body}");
                        this.WriteMessage("Success, ERP No: " + request[0].d.HeaderToItem.results[0].DeliveryNumber);
                        this.incubeQuery = new InCubeQuery(this.db_vms, $"UPDATE SAP_Reference SET DeliveryRef = '{request[0].d.HeaderToItem.results[0].DeliveryNumber}' WHERE TransactionID = '{TransactionID}' and Divisioncode ='{str10}'  ");
                        int num1 = (int)this.incubeQuery.ExecuteNonQuery();
                        this.incubeQuery = new InCubeQuery(this.db_vms, $"EXEC SP_InsertSAPPostingData '{TransactionID}', '{request[0].d.HeaderToItem.results[0].DeliveryNumber}', 'SendDelivery()', '{body}', '{responseBody.Replace("'", "*")}'");
                        int num2 = (int)this.incubeQuery.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    this.incubeQuery = new InCubeQuery(this.db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendDeliver()', '{body}', '{responseBody.Replace("'", "*")}'");
                    int num = (int)this.incubeQuery.ExecuteNonQuery();
                    Logger.WriteLog(MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                    stringBuilder.Append(ex.Message);
                    if (result == InCubeLibrary.Result.UnKnown)
                    {
                        result = InCubeLibrary.Result.Failure;
                        this.WriteMessage("Unhandled exception !!");
                    }
                }
                finally
                {
                    this.execManager.LogIntegrationEnding(ID, result, "", stringBuilder.ToString());
                }
            }

            if (IsExch)
                return;

            this.SendPGI(TransactionID, TransactionType, false);
        }
        catch (Exception ex)
        {
            Logger.WriteLog(MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            this.WriteMessage("Fetching order failed !!");
        }
        finally
        {
            this.CloseSession();
        }
    }

    // Helper to sanitize SQL strings
    private static string EscapeForSql(string input)
        {
            return (input ?? "").Replace("'", "*");
        }


        public void ReSendInvoice()
        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));

                string salespersonFilter = "", TransactionID = "", CustomerCode = "", EmployeeCode = "", OrderType = "", MainItem = "", OrganizationCode = "", HDiscount = "", OrderReason = "", Reference = "";
                int processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string responseBody = "";
                string headerData = "";
                int isFOC = -1;

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND EmployeeID = " + Filters.EmployeeID;
                }

                string headerQuery = $"SELECT * FROM [V_POST_Sales_Specific]";
                incubeQuery = new InCubeQuery(db_vms, headerQuery);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Order header query failed !!"));
                }

                DataTable dtHeader = incubeQuery.GetDataTable();
                if (dtHeader.Rows.Count == 0)
                    WriteMessage("There is no Order to send ..");
                else
                    SetProgressMax(dtHeader.Rows.Count);

                for (int i = 0; i < dtHeader.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();
                        TransactionID = dtHeader.Rows[i]["Transactionid"].ToString();
                        ReportProgress("Sending Transaction: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(11, TransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        CustomerCode = dtHeader.Rows[i]["CustomerCode"].ToString();
                        EmployeeCode = dtHeader.Rows[i]["EmployeeCode"].ToString();
                        OrderType = dtHeader.Rows[i]["OrderType"].ToString();
                        MainItem = dtHeader.Rows[i]["MainItem"].ToString();
                        OrganizationCode = dtHeader.Rows[i]["OrganizationCode"].ToString();
                        HDiscount = decimal.Parse(dtHeader.Rows[i]["Discount"].ToString()).ToString("#0.000");
                        OrderReason = dtHeader.Rows[i]["OrderReason"].ToString();
                        Reference = dtHeader.Rows[i]["Reference"].ToString();
                        isFOC = int.Parse(dtHeader.Rows[i]["IsFOC"].ToString());

                        string detailsQuery = $@"SELECT I.ItemCode, SUM(TD.Quantity) Quantity, PTL.Description, IIF(TD.Price<>PD.Price, TD.Price, 0) Price, IIF(TD.Price<>PD.Price,SUM(TD.Discount)*-1,SUM(TD.Discount)) Discount, 0 DiscountPercentage
                                                ,CASE WHEN {isFOC} = 2 AND TD.Price = 0 THEN FOC.MainItem ELSE '{MainItem}' END MainItem, P.PackID
                                                FROM TransactionDetail TD
                                                INNER JOIN Pack P ON P.PackID = TD.PackID
                                                INNER JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1
                                                INNER JOIN Item I ON I.ItemID = P.ItemID
                                                INNER JOIN [transaction] T ON T.TransactionID = TD.TransactionID
                                                LEFT JOIN ConfigurationOrganization CO ON CO.OrganizationID = T.OrganizationID AND CO.KeyName = 'DefaultPriceListID'
                                                LEFT JOIN PriceDefinition PD ON PD.PackID = TD.PackID AND PD.PriceListID = CO.KeyValue
                                                INNER JOIN CustomerOutlet C ON C.CustomerID = T.CustomerID AND C.OutletID = T.OutletID
                                                LEFT JOIN V_SAP_FOCWithSales FOC ON FOC.CustomerTypeID = C.CustomerTypeID
                                                WHERE TD.TransactionID = '{TransactionID}'
                                                GROUP BY I.ItemCode,PTL.Description,TD.Price,DiscountPercentage, PD.Price, FOC.MainItem, P.PackID
                                                Order by I.ItemCode, P.PackID, TD.Price";

                        incubeQuery = new InCubeQuery(db_vms, detailsQuery);
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            throw (new Exception("order details query failed !!"));
                        }

                        DataTable dtDetails = incubeQuery.GetDataTable();
                        List<object> allDetailsList = new List<object>();
                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        {
                            string ItemCode = "", UOM = "", Quantity = "", Price = "", DDiscount = "", DDiscountPercentage = "";

                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            UOM = dtDetails.Rows[j]["Description"].ToString();
                            Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString("#0.000");
                            Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString("#0.000");
                            DDiscount = decimal.Parse(dtDetails.Rows[j]["Discount"].ToString()).ToString("#0.000");
                            DDiscountPercentage = decimal.Parse(dtDetails.Rows[j]["DiscountPercentage"].ToString()).ToString("#0.000");
                            MainItem = dtDetails.Rows[j]["MainItem"].ToString();

                            var details = new
                            {
                                Plant = OrganizationCode,
                                SalesDocumentno = "",
                                Material = ItemCode,
                                DiscPercent = DDiscountPercentage,
                                OrderQty = Quantity,
                                DiscValue = DDiscount,
                                Price = "0.000", //Price,
                                ItemCategory = MainItem,
                                SalesUnit = UOM,
                                FocPercent = "0.000",
                                FocValue = "0.000",
                                StoreLoc = ""
                            };

                            allDetailsList.Add(details);
                        }

                        var headerDataObject = new
                        {
                            LoadProcess = "",
                            SalesDocumentno = "",
                            SoldToParty = CustomerCode,
                            StockPartner = EmployeeCode,
                            DiscPercent = "0.000",
                            OrderType = OrderType,
                            Status = "",
                            CreditExceed = "X",
                            DiscValue = "0.000",
                            SalesOrg = "1930",
                            DistributionChannel = "03",
                            Division = "07",
                            SalesmanId = EmployeeCode,
                            InvoiceNo = TransactionID,
                            Reference = Reference,
                            OrderReason = OrderReason,
                            FocPercent = "0.000",
                            FocValue = "0.000",
                            HdrItem = allDetailsList
                        };

                        headerData = JsonConvert.SerializeObject(headerDataObject);

                        incubeQuery = new InCubeQuery(db_vms, "INSERT INTO PostingTransaction (TransactionID) VALUES ('" + TransactionID + "')");
                        incubeQuery.ExecuteNonQuery();

                        MezzanSimpleResult[] salesResult = Tools.GetRequest<MezzanSimpleResult>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_SALESORDER_SRV/HeaderSet", headerData, "", out responseBody, webHeader, cookies);
                        if (salesResult == null || (salesResult[0].d.SalesDocumentno != null && salesResult[0].d.SalesDocumentno.Trim() == ""))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (salesResult[0].d.SalesDocumentno != null ? salesResult[0].d.SalesDocumentno : "") + "\r\n json:" + headerData);
                            WriteMessage("Error .. \r\n" + TransactionID + " \r\n" + salesResult[0].d.SalesDocumentno);

                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendInvoice()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + salesResult[0].d.SalesDocumentno + "\r\n Message:" + (salesResult[0].d.SalesDocumentno != null ? salesResult[0].d.SalesDocumentno : "") + "\r\n json:" + headerData);
                            WriteMessage("Success, ERP No: " + salesResult[0].d.SalesDocumentno);
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [Transaction] SET Synchronized = 1,LPONumber='1',Description='" + salesResult[0].d.SalesDocumentno + "' WHERE TransactionID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();

                            incubeQuery = new InCubeQuery(db_vms, "UPDATE SAP_Reference SET SalesOrderRef='" + salesResult[0].d.SalesDocumentno + "' WHERE TransactionID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();

                            incubeQuery = new InCubeQuery(db_vms, "DELETE FROM SAP_ResendTransaction WHERE TransactionID= '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();

                            SendDelivery(TransactionID, "SRE",false);
                        }

                        incubeQuery = new InCubeQuery(db_vms, "DELETE FROM PostingTransaction WHERE TransactionID= '" + TransactionID + "'");
                        incubeQuery.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {

                        incubeQuery = new InCubeQuery(db_vms, "DELETE FROM PostingTransaction WHERE TransactionID= '" + TransactionID + "'");
                        incubeQuery.ExecuteNonQuery();
                        incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendInvoice()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                        incubeQuery.ExecuteNonQuery();
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

        public void SendDeliveryOLD(string TransactionID, string TransactionType, bool IsExch)
        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));

                string salespersonFilter = "", SalesOrderNumber = "", EmployeeCode = "", OrganizationCode = "", DivisionCode = "";
                int processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string responseBody = "";
                string headerData = "";
                int TransactionTypeID = -1;
                string headerQuery = "";
                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND EmployeeID = " + Filters.EmployeeID;
                }
                // string headerQuery = $@"SELECT * FROM V_POST_{(TransactionType == "WH" ? "OffloadDelivery" : "SalesDelivery")} ";

                if (TransactionType == "WH")
                {
                    headerQuery = string.Format(@"SELECT DivisionCode,SalesOrderRef, V_POST_OffloadDelivery.Employeecode,OrganizationCode,V_POST_OffloadDelivery.TransactionID,V_POST_OffloadDelivery.TransactionTypeID FROM V_POST_OffloadDelivery 
	
	where  convert(date,TransactionDate) >= '{0}' AND convert(date,TransactionDate) <= '{1}' {2}
	", Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);

                }

                else
                {
                    if (IsExch == false)
                    {
                        headerQuery = string.Format(@"SELECT Division.DivisionCode, SalesOrderRef,V_POST_SalesDelivery.EmployeeCode,OrganizationCode,V_POST_SalesDelivery.TransactionID,V_POST_SalesDelivery.TransactionTypeID  FROM	V_POST_SalesDelivery
inner join [Transaction] on V_POST_SalesDelivery.TransactionID= [Transaction].TransactionID inner join Division on Division.DivisionID = [Transaction].DivisionID  where   convert(date,TransactionDate) >= '{0}' AND convert(date,TransactionDate) <= '{1}' {2}", Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
                    }
                    else
                    {
                        headerQuery = string.Format(@"SELECT Division.DivisionCode, SalesOrderRef,V_POST_SalesDelivery.EmployeeCode,OrganizationCode,V_POST_SalesDelivery.TransactionID,V_POST_SalesDelivery.TransactionTypeID  FROM	V_POST_SalesDelivery
inner join [Transaction] on V_POST_SalesDelivery.TransactionID= [Transaction].TransactionID inner join Division on Division.DivisionID = [Transaction].DivisionID  where [Transaction].Transactionid='{0}'   ", TransactionID);
                    }
                }

                incubeQuery = new InCubeQuery(db_vms, headerQuery);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("delivery header query failed !!"));
                }

                DataTable dtHeader = incubeQuery.GetDataTable();
                if (dtHeader.Rows.Count == 0)
                    WriteMessage("There is no Order to send ..");
                else
                    SetProgressMax(dtHeader.Rows.Count);

                for (int i = 0; i < dtHeader.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();
                        SalesOrderNumber = dtHeader.Rows[i]["SalesOrderRef"].ToString();
                        ReportProgress("Sending Delivery: " + SalesOrderNumber);
                        WriteMessage("\r\n" + SalesOrderNumber + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(11, SalesOrderNumber);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        EmployeeCode = dtHeader.Rows[i]["EmployeeCode"].ToString();
                        OrganizationCode = dtHeader.Rows[i]["OrganizationCode"].ToString();
                        TransactionID = dtHeader.Rows[i]["TransactionID"].ToString();
                        DivisionCode = dtHeader.Rows[i]["DivisionCode"].ToString();
                        TransactionTypeID = Convert.ToInt32(dtHeader.Rows[i]["TransactionTypeID"]);

                        string detailsQuery = "";


                        if (TransactionType == "WH")
                        {
                            detailsQuery = $@"SELECT FORMAT(ROW_NUMBER() OVER (ORDER BY I.ItemCode), '000000') LineNum, I.ItemCode, SUM(TD.Quantity) Quantity, TD.PackID, 0 Price, count(1) cnt, P.Barcode
                                                FROM WhTransDetail TD
                                                INNER JOIN Pack P ON P.PackID = TD.PackID
                                                INNER JOIN Item I ON I.ItemID = P.ItemID
                                                WHERE TD.TransactionID = '{TransactionID}'
                                                GROUP BY I.ItemCode, TD.PackID, P.Barcode
                                                ORDER BY I.ItemCode, TD.PackID";
                        }
                        else
                        {
                            detailsQuery = $@"SELECT FORMAT(ROW_NUMBER() OVER (ORDER BY  td.SalesTransactionTypeID,I.ItemCode) + case when (select TransactionTypeID from [Transaction] where TransactionID = '{TransactionID}') = 3 then (select count(distinct PackID) ReturnItemsCount from [TransactionDetail] where Transactionid in (select Transactionid from   [Transaction] where SourceTransactionID ='{TransactionID}')) else 0 end, '000000') LineNum,
I.ItemCode, SUM(TD.Quantity) Quantity, TD.PackID, TD.Price, count(1) cnt, P.Barcode
FROM TransactionDetail TD
INNER JOIN Pack P ON P.PackID = TD.PackID
INNER JOIN Item I ON I.ItemID = P.ItemID
WHERE TD.TransactionID = '{TransactionID}'
GROUP BY I.ItemCode, TD.PackID, TD.Price, P.Barcode ,td.SalesTransactionTypeID
ORDER BY td.SalesTransactionTypeID, I.ItemCode, TD.PackID, TD.Price
";
                        }

                        incubeQuery = new InCubeQuery(db_vms, detailsQuery);
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            throw (new Exception("delivery details query failed !!"));
                        }

                        DataTable dtDetails = incubeQuery.GetDataTable();
                        List<object> allDetailsList = new List<object>();
                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        {
                            string ItemCode = "", Quantity = "", PackID = "", LineNum = "", Price = "", dUOM = "";
                            int count;

                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            LineNum = dtDetails.Rows[j]["LineNum"].ToString();
                            Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString("#0.000");
                            PackID = dtDetails.Rows[j]["PackID"].ToString();
                            Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString("#0.000000000");
                            count = Convert.ToInt32(dtDetails.Rows[j]["cnt"]);
                            dUOM = dtDetails.Rows[j]["Barcode"].ToString();

                            string batchQuery = "";

                            if (count > 1)
                            {

                                switch (TransactionTypeID)
                                {
                                    case 0:
                                        batchQuery = $@"select distinct WTD.Quantity * p.Quantity Quantity, case when WT.TransactionTypeID= 2 and WTD.BatchNo= '1990/01/01' and    wtd.WarehouseID<> -1 then '8888888888' when  (WT.TransactionTypeID= 2 and wtd.WarehouseID=-1 and WTD.BatchNo= '1990/01/01')  then '9999999999' when   WT.TransactionTypeID= 6 then '9999999999' else ISNULL(STD.BatchNo,WTD.BatchNo)  end as BatchNo , uom.Barcode from warehouseTransaction WT
                                                    INNER JOIN WHTransDetail WTD ON WTD.TransactionID = WT.TransactionID
                                                    inner join pack p on p.packid = wtd.packid
                                                    inner join pack uom on uom.itemid = p.itemid and uom.Quantity = 1 and uom.barcode = p.GTIN
                                                    LEFT JOIN [Transaction] T ON T.RouteHistoryID = WT.RouteHistoryID and T.TransactionTypeID = 4
                                                    LEFT JOIN TransactionDetail TD ON TD.TransactionID = T.TransactionID AND TD.PackID = WTD.PackID AND TD.BatchNo = WTD.BatchNo AND TD.Quantity = WTD.Quantity AND TD.ExpiryDate = WTD.ExpiryDate
                                                    LEFT JOIN [Transaction] ST ON ST.TransactionID = T.SourceTransactionID
                                                    LEFT JOIN TransactionDetail STD ON STD.TransactionID = ST.TransactionID AND STD.PackID = TD.PackID AND STD.Quantity = TD.Quantity
                                                    where wt.transactionid ='{TransactionID}' AND WTD.PackID = {PackID}";
                                        break;
                                    //case 4:
                                    //    batchQuery = $@"select td.Quantity * p.Quantity Quantity, BatchNo, uom.Barcode from [Transaction] T 
                                    //            INNER JOIN TransactionDetail TD ON TD.TransactionID = T.TransactionID
                                    //            inner join pack p on p.packid = td.packid
                                    //            inner join pack uom on uom.itemid = p.itemid and uom.Quantity = 1 and uom.barcode = p.GTIN
                                    //            WHERE T.SourceTransactionID = '{TransactionID}' AND T.TransactionTypeID=3 AND TD.PackID = {PackID}";
                                    //    break;
                                    default:
                                        batchQuery = $@"SELECT td.Quantity * p.Quantity Quantity, CASE
        WHEN SalesTransactionTypeID in (5,6,8,9,10) THEN
            CASE
                WHEN PackStatusID in (select StatusID from PackStatus where ReSellable=1) THEN '8888888888'
                ELSE '9999999999'
            END
        when BatchNo ='1990/01/01' and SalesTransactionTypeID in (1,2,3,4,5,6,7,8)  then '8888888888'
        ELSE BatchNo

    END AS BatchNo, uom.Barcode 
                                                        FROM {(TransactionType == "WH" ? "WhTransDetail" : "TransactionDetail")} td 
                                                        inner join pack p on p.packid = td.packid
                                                        inner join pack uom on uom.itemid = p.itemid and uom.Quantity = 1 and uom.barcode = p.GTIN
                                                        WHERE TransactionID = '{TransactionID}' AND td.PackID = {PackID} and Price = {Price}";
                                        break;
                                }
                            }
                            else
                            {
                                switch (TransactionTypeID)
                                {
                                    case 0:
                                        batchQuery = $@"select distinct WTD.Quantity, case when WT.TransactionTypeID= 2 and WTD.BatchNo= '1990/01/01' and    wtd.WarehouseID<> -1 then '8888888888' when  (WT.TransactionTypeID= 2 and wtd.WarehouseID=-1 and WTD.BatchNo= '1990/01/01')  then '9999999999' when   WT.TransactionTypeID= 6 then '9999999999' else ISNULL(STD.BatchNo,WTD.BatchNo)  end as BatchNo , '{dUOM}' barcode from warehouseTransaction WT
                                                    INNER JOIN WHTransDetail WTD ON WTD.TransactionID = WT.TransactionID
                                                    LEFT JOIN [Transaction] T ON T.RouteHistoryID = WT.RouteHistoryID and T.TransactionTypeID = 4
                                                    LEFT JOIN TransactionDetail TD ON TD.TransactionID = T.TransactionID AND TD.PackID = WTD.PackID AND TD.BatchNo = WTD.BatchNo AND TD.Quantity = WTD.Quantity AND TD.ExpiryDate = WTD.ExpiryDate
                                                    LEFT JOIN [Transaction] ST ON ST.TransactionID = T.SourceTransactionID
                                                    LEFT JOIN TransactionDetail STD ON STD.TransactionID = ST.TransactionID AND STD.PackID = TD.PackID AND STD.Quantity = TD.Quantity
                                                    where wt.transactionid ='{TransactionID}' AND WTD.PackID = {PackID}";
                                        break;
                                    //case 4:
                                    //    batchQuery = $@"select TD.Quantity, TD.BatchNo, '{dUOM}' barcode from [Transaction] T 
                                    //            INNER JOIN TransactionDetail TD ON TD.TransactionID = T.TransactionID
                                    //            WHERE T.SourceTransactionID = '{TransactionID}' AND T.TransactionTypeID=3 AND TD.PackID = {PackID}";
                                    //    break;
                                    default:
                                        batchQuery = $@"SELECT Quantity, CASE
        WHEN SalesTransactionTypeID in (5,6,8,9,10) THEN
            CASE
                WHEN PackStatusID in (select StatusID from PackStatus where ReSellable=1) THEN '8888888888'
                ELSE '9999999999'
            END
       when BatchNo ='1990/01/01' and SalesTransactionTypeID in  (1,2,3,4,5,6,7,8)    then '8888888888'
        ELSE BatchNo
    END AS BatchNo, '{dUOM}' barcode FROM {(TransactionType == "WH" ? "WhTransDetail" : "TransactionDetail")} WHERE TransactionID = '{TransactionID}' AND PackID = {PackID} and Price = {Price}";
                                        break;
                                }
                            }

                            incubeQuery = new InCubeQuery(db_vms, batchQuery);
                            if (incubeQuery.Execute() != InCubeErrors.Success)
                            {
                                res = Result.Failure;
                                throw (new Exception("delivery batch query failed !!"));
                            }

                            DataTable dtBatch = incubeQuery.GetDataTable();
                            List<Object> allBatchList = new List<Object>();

                            for (int k = 0; k < dtBatch.Rows.Count; k++)
                            {
                                string BatchNo = "", batchQuantity = "", bUOM = "";
                                BatchNo = dtBatch.Rows[k]["BatchNo"].ToString();
                                batchQuantity = decimal.Parse(dtBatch.Rows[k]["Quantity"].ToString()).ToString("#0.000");
                                bUOM = dtBatch.Rows[k]["barcode"].ToString();

                                var batch = new
                                {
                                    ItemNumber = LineNum,
                                    BatchNumber = BatchNo,
                                    Quantity = batchQuantity,
                                    SalesUOM = bUOM          //  To ask Diab about the ItemCategory 
                                };

                                allBatchList.Add(batch);
                            }


                            string Plantt = "";

                            if (DivisionCode == "1350")
                            {
                                Plantt = "1351";
                            }
                            else if (DivisionCode == "1110")
                            {
                                Plantt = "1111";
                            }
                            else if (int.TryParse(DivisionCode, out int divisionInt))
                            {
                                Plantt = (divisionInt + 1).ToString();
                            }
                            var details = new
                            {
                                ItemNumber = LineNum,
                                MaterialNumber = ItemCode,
                                Plant = Plantt,
                                Quantity = Quantity,
                                DeliveryNumber = "",
                                SalesUOM = dUOM,
                                ItemToBatch = allBatchList
                            };

                            allDetailsList.Add(details);
                        }

                        var headerDataObject = new
                        {
                            SalesOrderNumber = SalesOrderNumber,
                            StockPartner = EmployeeCode,
                            ShippingPoint = "",
                            HeaderToItem = allDetailsList
                        };

                        headerData = JsonConvert.SerializeObject(headerDataObject);

                        MezzanComplexResult[] deliveryResult = Tools.GetRequest<MezzanComplexResult>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_DELIVERY_SRV/HeaderSet", headerData, "", out responseBody, webHeader, cookies);

                        if (deliveryResult[0].d.HeaderToItem.results[0].DeliveryNumber == null || (deliveryResult[0].d.HeaderToItem.results[0].Message != null && deliveryResult[0].d.HeaderToItem.results[0].Message.Trim().ToLower() != "delivery and batch created"))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (deliveryResult[0].d.HeaderToItem.results[0].DeliveryNumber != null ? deliveryResult[0].d.HeaderToItem.results[0].DeliveryNumber : "") + "\r\n json:" + headerData);
                            WriteMessage("Error .. \r\n" + TransactionID + " \r\n" + deliveryResult[0].d.HeaderToItem.results[0].DeliveryNumber);

                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendDeliver()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + deliveryResult[0].d.HeaderToItem.results[0].DeliveryNumber + "\r\n Message:" + (deliveryResult[0].d.HeaderToItem.results[0].Message != null ? deliveryResult[0].d.HeaderToItem.results[0].Message : "") + "\r\n json:" + headerData);
                            WriteMessage("Success, ERP No: " + deliveryResult[0].d.HeaderToItem.results[0].DeliveryNumber);
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE SAP_Reference SET DeliveryRef = '" + deliveryResult[0].d.HeaderToItem.results[0].DeliveryNumber + "' WHERE TransactionID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();

                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertSAPPostingData '{TransactionID}', '{deliveryResult[0].d.HeaderToItem.results[0].DeliveryNumber}', 'SendDelivery()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();

                            if (IsExch == false)
                                SendPGI(TransactionID, TransactionType, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendDeliver()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                        incubeQuery.ExecuteNonQuery();
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


        public void SendDeliveryForExchange(string TransactionID, string TransactionType)
        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));

                string salespersonFilter = "", SalesOrderNumber = "", EmployeeCode = "", OrganizationCode = "";
                int processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string responseBody = "";
                string headerData = "";
                int TransactionTypeID = -1;
                string headerQuery = "";
                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND EmployeeID = " + Filters.EmployeeID;
                }
                // string headerQuery = $@"SELECT * FROM V_POST_{(TransactionType == "WH" ? "OffloadDelivery" : "SalesDelivery")} ";

                if (TransactionType == "WH")
                {
                    headerQuery = string.Format(@"SELECT SalesOrderRef, V_POST_OffloadDelivery.Employeecode,OrganizationCode,V_POST_OffloadDelivery.TransactionID,V_POST_OffloadDelivery.TransactionTypeID FROM V_POST_OffloadDelivery 
	
	where  convert(date,TransactionDate) >= '{0}' AND convert(date,TransactionDate) <= '{1}' {2}
	", Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);

                }

                else
                {
                    headerQuery = string.Format(@"SELECT SalesOrderRef,V_POST_SalesDelivery.EmployeeCode,OrganizationCode,V_POST_SalesDelivery.TransactionID,V_POST_SalesDelivery.TransactionTypeID  FROM	V_POST_SalesDelivery
inner join [Transaction] on V_POST_SalesDelivery.TransactionID= [Transaction].TransactionID where  convert(date,TransactionDate) >= '{0}' AND convert(date,TransactionDate) <= '{1}' {2}", Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
                }

                incubeQuery = new InCubeQuery(db_vms, headerQuery);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("delivery header query failed !!"));
                }

                DataTable dtHeader = incubeQuery.GetDataTable();
                if (dtHeader.Rows.Count == 0)
                    WriteMessage("There is no Order to send ..");
                else
                    SetProgressMax(dtHeader.Rows.Count);

                for (int i = 0; i < dtHeader.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();
                        SalesOrderNumber = dtHeader.Rows[i]["SalesOrderRef"].ToString();
                        ReportProgress("Sending Delivery: " + SalesOrderNumber);
                        WriteMessage("\r\n" + SalesOrderNumber + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(11, SalesOrderNumber);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        EmployeeCode = dtHeader.Rows[i]["EmployeeCode"].ToString();
                        OrganizationCode = dtHeader.Rows[i]["OrganizationCode"].ToString();
                        TransactionID = dtHeader.Rows[i]["TransactionID"].ToString();
                        TransactionTypeID = Convert.ToInt32(dtHeader.Rows[i]["TransactionTypeID"]);

                        string detailsQuery = "";


                        if (TransactionType == "WH")
                        {
                            detailsQuery = $@"SELECT FORMAT(ROW_NUMBER() OVER (ORDER BY I.ItemCode), '000000') LineNum, I.ItemCode, SUM(TD.Quantity) Quantity, TD.PackID, 0 Price, count(1) cnt, P.Barcode
                                                FROM WhTransDetail TD
                                                INNER JOIN Pack P ON P.PackID = TD.PackID
                                                INNER JOIN Item I ON I.ItemID = P.ItemID
                                                WHERE TD.TransactionID = '{TransactionID}'
                                                GROUP BY I.ItemCode, TD.PackID, P.Barcode
                                                ORDER BY I.ItemCode, TD.PackID";
                        }
                        else
                        {
                            detailsQuery = $@"SELECT FORMAT(ROW_NUMBER() OVER (ORDER BY I.ItemCode), '000000') LineNum, I.ItemCode, SUM(TD.Quantity) Quantity, TD.PackID, TD.Price, count(1) cnt, P.Barcode
                                                FROM TransactionDetail TD
                                                INNER JOIN Pack P ON P.PackID = TD.PackID
                                                INNER JOIN Item I ON I.ItemID = P.ItemID
                                                WHERE TD.TransactionID = '{TransactionID}'
                                                GROUP BY I.ItemCode, TD.PackID, TD.Price, P.Barcode
                                                ORDER BY I.ItemCode, TD.PackID, TD.Price";
                        }

                        incubeQuery = new InCubeQuery(db_vms, detailsQuery);
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            throw (new Exception("delivery details query failed !!"));
                        }

                        DataTable dtDetails = incubeQuery.GetDataTable();
                        List<object> allDetailsList = new List<object>();
                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        {
                            string ItemCode = "", Quantity = "", PackID = "", LineNum = "", Price = "", dUOM = "";
                            int count;

                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            LineNum = dtDetails.Rows[j]["LineNum"].ToString();
                            Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString("#0.000");
                            PackID = dtDetails.Rows[j]["PackID"].ToString();
                            Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString("#0.000000000");
                            count = Convert.ToInt32(dtDetails.Rows[j]["cnt"]);
                            dUOM = dtDetails.Rows[j]["Barcode"].ToString();

                            string batchQuery = "";

                            if (count > 1)
                            {
                                switch (TransactionTypeID)
                                {
                                    case 0:
                                        batchQuery = $@"select WTD.Quantity * p.Quantity Quantity, ISNULL(STD.BatchNo,WTD.BatchNo) BatchNo, uom.Barcode from warehouseTransaction WT
                                                    INNER JOIN WHTransDetail WTD ON WTD.TransactionID = WT.TransactionID
                                                    inner join pack p on p.packid = wtd.packid
                                                    inner join pack uom on uom.itemid = p.itemid and uom.Quantity = 1 and uom.barcode = p.GTIN
                                                    LEFT JOIN [Transaction] T ON T.RouteHistoryID = WT.RouteHistoryID and T.TransactionTypeID = 4
                                                    LEFT JOIN TransactionDetail TD ON TD.TransactionID = T.TransactionID AND TD.PackID = WTD.PackID AND TD.BatchNo = WTD.BatchNo AND TD.Quantity = WTD.Quantity AND TD.ExpiryDate = WTD.ExpiryDate
                                                    LEFT JOIN [Transaction] ST ON ST.TransactionID = T.SourceTransactionID
                                                    LEFT JOIN TransactionDetail STD ON STD.TransactionID = ST.TransactionID AND STD.PackID = TD.PackID AND STD.Quantity = TD.Quantity
                                                    where wt.transactionid ='{TransactionID}' AND WTD.PackID = {PackID}";
                                        break;
                                    case 4:
                                        batchQuery = $@"select td.Quantity * p.Quantity Quantity, BatchNo, uom.Barcode from [Transaction] T 
                                                INNER JOIN TransactionDetail TD ON TD.TransactionID = T.TransactionID
                                                inner join pack p on p.packid = td.packid
                                                inner join pack uom on uom.itemid = p.itemid and uom.Quantity = 1 and uom.barcode = p.GTIN
                                                WHERE T.SourceTransactionID = '{TransactionID}' AND T.TransactionTypeID=3 AND TD.PackID = {PackID}";
                                        break;
                                    default:
                                        batchQuery = $@"SELECT td.Quantity * p.Quantity Quantity, CASE
        WHEN SalesTransactionTypeID in (5,6,8,9,10) THEN
            CASE
                WHEN PackStatusID in (select StatusID from PackStatus where ReSellable=1) THEN '8888888888'
                ELSE '9999999999'
            END
        --WHEN condition2 THEN result2
        ELSE BatchNo
    END AS BatchNo, uom.Barcode 
                                                        FROM {(TransactionType == "WH" ? "WhTransDetail" : "TransactionDetail")} td 
                                                        inner join pack p on p.packid = td.packid
                                                        inner join pack uom on uom.itemid = p.itemid and uom.Quantity = 1 and uom.barcode = p.GTIN
                                                        WHERE TransactionID = '{TransactionID}' AND td.PackID = {PackID} and Price = {Price}";
                                        break;
                                }
                            }
                            else
                            {
                                switch (TransactionTypeID)
                                {
                                    case 0:
                                        batchQuery = $@"select WTD.Quantity, ISNULL(STD.BatchNo,WTD.BatchNo) BatchNo, '{dUOM}' barcode from warehouseTransaction WT
                                                    INNER JOIN WHTransDetail WTD ON WTD.TransactionID = WT.TransactionID
                                                    LEFT JOIN [Transaction] T ON T.RouteHistoryID = WT.RouteHistoryID and T.TransactionTypeID = 4
                                                    LEFT JOIN TransactionDetail TD ON TD.TransactionID = T.TransactionID AND TD.PackID = WTD.PackID AND TD.BatchNo = WTD.BatchNo AND TD.Quantity = WTD.Quantity AND TD.ExpiryDate = WTD.ExpiryDate
                                                    LEFT JOIN [Transaction] ST ON ST.TransactionID = T.SourceTransactionID
                                                    LEFT JOIN TransactionDetail STD ON STD.TransactionID = ST.TransactionID AND STD.PackID = TD.PackID AND STD.Quantity = TD.Quantity
                                                    where wt.transactionid ='{TransactionID}' AND WTD.PackID = {PackID}";
                                        break;
                                    case 4:
                                        batchQuery = $@"select TD.Quantity, TD.BatchNo, '{dUOM}' barcode from [Transaction] T 
                                                INNER JOIN TransactionDetail TD ON TD.TransactionID = T.TransactionID
                                                WHERE T.SourceTransactionID = '{TransactionID}' AND T.TransactionTypeID=3 AND TD.PackID = {PackID}";
                                        break;
                                    default:
                                        batchQuery = $@"SELECT Quantity, CASE
        WHEN SalesTransactionTypeID in (5,6,8,9,10) THEN
            CASE
                WHEN PackStatusID in (select StatusID from PackStatus where ReSellable=1) THEN '8888888888'
                ELSE '9999999999'
            END
        --WHEN condition2 THEN result2
        ELSE BatchNo
    END AS BatchNo, '{dUOM}' barcode FROM {(TransactionType == "WH" ? "WhTransDetail" : "TransactionDetail")} WHERE TransactionID = '{TransactionID}' AND PackID = {PackID} and Price = {Price}";
                                        break;
                                }
                            }

                            incubeQuery = new InCubeQuery(db_vms, batchQuery);
                            if (incubeQuery.Execute() != InCubeErrors.Success)
                            {
                                res = Result.Failure;
                                throw (new Exception("delivery batch query failed !!"));
                            }

                            DataTable dtBatch = incubeQuery.GetDataTable();
                            List<Object> allBatchList = new List<Object>();

                            for (int k = 0; k < dtBatch.Rows.Count; k++)
                            {
                                string BatchNo = "", batchQuantity = "", bUOM = "";
                                BatchNo = dtBatch.Rows[k]["BatchNo"].ToString();
                                batchQuantity = decimal.Parse(dtBatch.Rows[k]["Quantity"].ToString()).ToString("#0.000");
                                bUOM = dtBatch.Rows[k]["barcode"].ToString();

                                var batch = new
                                {
                                    ItemNumber = LineNum,
                                    BatchNumber = BatchNo,
                                    Quantity = batchQuantity,
                                    SalesUOM = bUOM
                                };

                                allBatchList.Add(batch);
                            }

                            var details = new
                            {
                                ItemNumber = LineNum,
                                MaterialNumber = ItemCode,
                                Plant = OrganizationCode,
                                Quantity = Quantity,
                                DeliveryNumber = "",
                                SalesUOM = dUOM,
                                ItemToBatch = allBatchList
                            };

                            allDetailsList.Add(details);
                        }

                        var headerDataObject = new
                        {
                            SalesOrderNumber = SalesOrderNumber,
                            StockPartner = EmployeeCode,
                            ShippingPoint = "",
                            HeaderToItem = allDetailsList
                        };

                        headerData = JsonConvert.SerializeObject(headerDataObject);

                        MezzanComplexResult[] deliveryResult = Tools.GetRequest<MezzanComplexResult>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_DELIVERY_SRV/HeaderSet", headerData, "", out responseBody, webHeader, cookies);
                        if (deliveryResult[0].d.HeaderToItem.results[0].DeliveryNumber == null || (deliveryResult[0].d.HeaderToItem.results[0].Message != null && deliveryResult[0].d.HeaderToItem.results[0].Message.Trim() != "Delivery and Batch created"))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (deliveryResult[0].d.HeaderToItem.results[0].DeliveryNumber != null ? deliveryResult[0].d.HeaderToItem.results[0].DeliveryNumber : "") + "\r\n json:" + headerData);
                            WriteMessage("Error .. \r\n" + TransactionID + " \r\n" + deliveryResult[0].d.HeaderToItem.results[0].DeliveryNumber);
                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendDeliver()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + deliveryResult[0].d.HeaderToItem.results[0].DeliveryNumber + "\r\n Message:" + (deliveryResult[0].d.HeaderToItem.results[0].Message != null ? deliveryResult[0].d.HeaderToItem.results[0].Message : "") + "\r\n json:" + headerData);
                            WriteMessage("Success, ERP No: " + deliveryResult[0].d.HeaderToItem.results[0].DeliveryNumber);
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE SAP_Reference SET DeliveryRef = '" + deliveryResult[0].d.HeaderToItem.results[0].DeliveryNumber + "' WHERE TransactionID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();

                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertSAPPostingData '{TransactionID}', '{deliveryResult[0].d.HeaderToItem.results[0].DeliveryNumber}', 'SendDelivery()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                            SendPGI(TransactionID, TransactionType,false);
                        }
                    }
                    catch (Exception ex)
                    {
                        incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendDeliver()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                        incubeQuery.ExecuteNonQuery();
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


        public void AboodAPI(string TransactionID)
        {
            string Transactionid = "", billtocode = "", shiptocode = "", TransactionTypeID = "", ArchiveDate = "", QRCode = "", PIH = "", GeneratedID = "", ETransactionStatus = "", UUID="";
            Result res = Result.UnKnown;
            int processID = 0;
            StringBuilder result = new StringBuilder();
            string headerData = "";

            string headerquery = $@"
select 
  SAP_Reference.Transactionid,  customer.CustomerCode billtocode,
    CustomerOutlet.CustomerCode shiptocode,-1 DivisionID,
    TransactionTypeID,
    GETDATE() ArchiveDate,
    'AWlNZXp6YW4gRm9vZHMgQ28uINi02LHZg9ipINmF2YrYstin2YYg2YTZhNij2LrYsNmK2KkgfCBNZXp6YW4gRm9vZHMgQ28uINi02LHZg9ipINmF2YrYstin2YYg2YTZhNij2LrYsNmK2KkCDzMwMDkzOTA5NDQwMDAwMwMTMjAyNS0wNi0yOVQxMjowMDoxOQQFMTMuMjgFBDEuNzMGLExGL0FUZGZxRGIxZmh5bk1TK2ZaM2U2NW05VTFaUjRTVnNLbUY0NVdqek09B2BNRVVDSVFESll6VksvbFdCdnhGQkxoY08xVTZEK2J0NFJmdGtyelpQcjZlMG1oRHVRQUlnUUhQSXhpcVA1dlpZTjZ1Q2VWdGpwdmpaRWtQb205N2pDa0VaRHdBaDY1UT0IWzBZMBMGByqGSM49AgEGCCqGSM49AwEHA0IABHJ7W+qe1j3RJem8fmGkn5BVxucFjcNyiAMmpH+Jh+dtlFxMuBkU3rioicn3QrvC62b/DJplM4ZdT8NIsQmeQKM=' QRCode,
    isnull(SAP_Reference.DeliveryRef, 0000000000) + '_' + SAP_Reference.BillingRef PIH,
    SAP_Reference.BillingRef 'Billing Code',
    -1 GeneratedID,
    1 ETransactionStatus
from SAP_Reference
inner join [Transaction] on SAP_Reference.TransactionID = [Transaction].TransactionID
inner join customer on [Transaction].CustomerID = Customer.CustomerID
inner join CustomerOutlet on [Transaction].CustomerID = CustomerOutlet.CustomerID 
    and CustomerOutlet.outletid = [Transaction].outletid
where SAP_Reference.Transactionid = '{TransactionID}'";

            incubeQuery = new InCubeQuery(db_vms, headerquery);

            if (incubeQuery.Execute() != InCubeErrors.Success)
            {
                res = Result.Failure;
                throw new Exception("Details query failed !!");
            }

            DataTable dtHeader = incubeQuery.GetDataTable();
            if (dtHeader.Rows.Count == 0)
                WriteMessage("There is no Order to send ..");
            else
                SetProgressMax(dtHeader.Rows.Count);

            for (int i = 0; i < dtHeader.Rows.Count; i++)
            {
                try
                {
                    res = Result.UnKnown;
                    processID = 0;
                    result = new StringBuilder();
                    Transactionid = dtHeader.Rows[i]["Transactionid"].ToString();

                    billtocode = dtHeader.Rows[i]["billtocode"].ToString();
                    ReportProgress("Sending ETransaction: " + "E" + TransactionID);
                    WriteMessage("\r\n" + "E" + TransactionID + ": ");
                    Dictionary<int, string> filters = new Dictionary<int, string>();
                    filters.Add(11, TransactionID);
                    processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                    ArchiveDate = DateTime
                        .Parse(dtHeader.Rows[i]["ArchiveDate"].ToString())     
                        .ToUniversalTime()                                     
                        .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");

                    shiptocode = dtHeader.Rows[i]["shiptocode"].ToString();
                    TransactionTypeID = dtHeader.Rows[i]["TransactionTypeID"].ToString();
                    QRCode = dtHeader.Rows[i]["QRCode"].ToString();
                    PIH = dtHeader.Rows[i]["PIH"].ToString();
                    GeneratedID = dtHeader.Rows[i]["GeneratedID"].ToString();
                    ETransactionStatus = dtHeader.Rows[i]["ETransactionStatus"].ToString();
                    UUID = "1001";

                    var headerDataObject = new
                    {
                        Transactionid = Transactionid,
                        billtocode = billtocode,
                        shiptocode = shiptocode,
                        PIH = PIH,
                        TransactionTypeID = TransactionTypeID,
                        ArchiveDate = ArchiveDate,
                        QRCode = QRCode,
                        UUID = UUID
                    };

                    headerData = JsonConvert.SerializeObject(headerDataObject);
                    string apiurl = CoreGeneral.Common.GeneralConfigurations.AboodsAPI;

                    SendETransactionAsync(headerData, apiurl).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    WriteMessage($"Error sending ETransaction: {ex.Message}");
                }
            }
        }

        private void SendBillingOLD(string TransactionID)





        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));

                string salespersonFilter = "", DeliveryNumber = "", BillingType = "", BillingDate = "";
                int processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string responseBody = "";
                string headerData = "";

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND EmployeeID = " + Filters.EmployeeID;
                }
                //      string headerQuery = $@"SELECT * FROM V_POST_Billing {(TransactionID == "" ? "--" : "")} WHERE TransactionID = '{TransactionID}'";
                string headerQuery = string.Format(@"select V_POST_Billing.DeliveryRef,V_POST_Billing.BillingType,V_POST_Billing.BillingDate,V_POST_Billing.TransactionID from V_POST_Billing
	inner join [Transaction] on V_POST_Billing.TransactionID=[Transaction].TransactionID
	where  convert(date,TransactionDate) >= '{0}' AND convert(date,TransactionDate) <= '{1}' {2}", Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
                incubeQuery = new InCubeQuery(db_vms, headerQuery);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("delivery header query failed !!"));
                }

                DataTable dtHeader = incubeQuery.GetDataTable();
                if (dtHeader.Rows.Count == 0)
                    WriteMessage("There is no Order to send ..");
                else
                    SetProgressMax(dtHeader.Rows.Count);

                for (int i = 0; i < dtHeader.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();
                        DeliveryNumber = dtHeader.Rows[i]["DeliveryRef"].ToString();
                        ReportProgress("Sending Delivery: " + DeliveryNumber);
                        WriteMessage("\r\n" + DeliveryNumber + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(11, DeliveryNumber);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        BillingType = dtHeader.Rows[i]["BillingType"].ToString();
                        BillingDate = dtHeader.Rows[i]["BillingDate"].ToString();
                        TransactionID = dtHeader.Rows[i]["TransactionID"].ToString();

                        var headerDataObject = new
                        {
                            BillingType = BillingType,
                            BillingDate = BillingDate,
                            ToBillingItems = new[] { new { Delivery = DeliveryNumber } }
                        };

                        headerData = JsonConvert.SerializeObject(headerDataObject);

                        MezzanSimpleResult[] deliveryResult = Tools.GetRequest<MezzanSimpleResult>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_BILLING_UTILITY_SRV/BillingHeaderSet", headerData, "", out responseBody, webHeader, cookies);
                        if (deliveryResult[0].d.BillingDocument == null || (deliveryResult[0].d.BillingDocument != null && deliveryResult[0].d.BillingDocument.Trim() == ""))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (deliveryResult[0].d.BillingDocument != null ? deliveryResult[0].d.BillingDocument : "") + "\r\n json:" + headerData);
                            WriteMessage("Error .. \r\n" + TransactionID + " \r\n" + deliveryResult[0].d.BillingDocument);

                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendBilling()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + deliveryResult[0].d.BillingDocument + "\r\n Message:" + (deliveryResult[0].d.BillingDocument != null ? deliveryResult[0].d.BillingDocument : "") + "\r\n json:" + headerData);
                            WriteMessage("Success, ERP No: " + deliveryResult[0].d.BillingDocument);
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE SAP_Reference SET BillingRef = '" + deliveryResult[0].d.BillingDocument + "' WHERE TransactionID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                            incubeQuery = new InCubeQuery(db_vms, $"delete from SAP_Error where transactionid = '{TransactionID}'");
                            incubeQuery.ExecuteNonQuery();
                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertSAPPostingData '{TransactionID}', '{deliveryResult[0].d.BillingDocument}', 'SendBilling()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendBilling()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                        incubeQuery.ExecuteNonQuery();
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
        private void SendBilling(string TransactionID)
        {
            try
            {
                if (this.webHeader == null)
                    throw new Exception("Can't open session in API, please check the service!!");

                string str1 = "";
                int ID = 0;
                StringBuilder stringBuilder = new StringBuilder();
                InCubeLibrary.Result result = InCubeLibrary.Result.UnKnown;
                string responseBody = "";
                string body = "";

                if (this.Filters.EmployeeID != -1)
                    str1 = "AND EmployeeID = " + this.Filters.EmployeeID.ToString();

                this.incubeQuery = new InCubeQuery(
                    this.db_vms,
                    $@"SELECT V_POST_Billing.DeliveryRef, V_POST_Billing.BillingType, 
                      V_POST_Billing.BillingDate, V_POST_Billing.TransactionID 
               FROM V_POST_Billing
               INNER JOIN [Transaction] ON V_POST_Billing.TransactionID = [Transaction].TransactionID
               WHERE CONVERT(date, TransactionDate) >= '{this.Filters.FromDate:yyyy-MM-dd}' 
               AND CONVERT(date, TransactionDate) <= '{this.Filters.ToDate.AddDays(1):yyyy-MM-dd}' 
               {str1}"
                );

                DataTable dataTable1 = this.incubeQuery.Execute() == 0
                    ? this.incubeQuery.GetDataTable()
                    : throw new Exception("Delivery header query failed !!");

                if (dataTable1.Rows.Count == 0)
                {
                    this.WriteMessage("There is no Order to send ..");
                }
                else
                {
                    this.SetProgressMax(dataTable1.Rows.Count);
                }

                for (int index = 0; index < dataTable1.Rows.Count; index++)
                {
                    try
                    {
                        result = InCubeLibrary.Result.UnKnown;
                        ID = 0;
                        stringBuilder = new StringBuilder();

                        string deliveryRef = dataTable1.Rows[index]["DeliveryRef"].ToString();
                        this.ReportProgress("Sending Delivery: " + deliveryRef);
                        this.WriteMessage($"\r\n{deliveryRef}: ");

                        ID = this.execManager.LogIntegrationBegining(this.TriggerID, -1, new Dictionary<int, string>
                {
                    { 11, deliveryRef }
                });

                        string billingType = dataTable1.Rows[index]["BillingType"].ToString();
                        string billingDate = dataTable1.Rows[index]["BillingDate"].ToString();
                        TransactionID = dataTable1.Rows[index]["TransactionID"].ToString();

                        InCubeQuery inCubeQuery = new InCubeQuery(this.db_vms,
                            $"SELECT DeliveryRef FROM SAP_Reference WHERE TransactionID = '{TransactionID}'");

                        DataTable dataTable2 = inCubeQuery.Execute() == 0
                            ? inCubeQuery.GetDataTable()
                            : throw new Exception("Delivery query failed for TransactionID " + TransactionID);

                        List<object> deliveryList = new List<object>();
                        foreach (DataRow row in dataTable2.Rows)
                        {
                            string delRef = row["DeliveryRef"].ToString();
                            deliveryList.Add(new { Delivery = delRef });
                        }

                        var data = new
                        {
                            BillingType = billingType,
                            BillingDate = billingDate,
                            ToBillingItems = deliveryList
                        };

                        body = JsonConvert.SerializeObject(data);

                        MezzanSimpleResult[] request = Tools.GetRequest<MezzanSimpleResult>(
                            CoreGeneral.Common.GeneralConfigurations.WS_URL +
                            "ZHH_INC_BILLING_UTILITY_SRV/BillingHeaderSet",
                            body, "", out responseBody, this.webHeader, this.cookies);

                        if (request[0].d.BillingDocument == null ||
                            (request[0].d.BillingDocument != null && request[0].d.BillingDocument.Trim() == ""))
                        {
                            result = InCubeLibrary.Result.NoFileRetreived;
                            stringBuilder.Append($"ERP ERROR Message: {(request[0].d.BillingDocument ?? "")}\r\n json: {body}");
                            this.WriteMessage($"Error .. \r\n{TransactionID} \r\n{request[0].d.BillingDocument}");

                            this.incubeQuery = new InCubeQuery(
                                this.db_vms,
                                $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendBilling()', '{body}', '{responseBody.Replace("'", "*")}'");
                            _ = this.incubeQuery.ExecuteNonQuery();
                        }
                        else
                        {
                            result = InCubeLibrary.Result.Success;
                            stringBuilder.Append($"ERP No: {request[0].d.BillingDocument}\r\n Message: {request[0].d.BillingDocument}\r\n json: {body}");
                            this.WriteMessage("Success, ERP No: " + request[0].d.BillingDocument);

                            this.incubeQuery = new InCubeQuery(
                                this.db_vms,
                                $"UPDATE SAP_Reference SET BillingRef = '{request[0].d.BillingDocument}' WHERE TransactionID = '{TransactionID}'");
                            _ = this.incubeQuery.ExecuteNonQuery();

                            this.incubeQuery = new InCubeQuery(this.db_vms, $"DELETE FROM SAP_Error WHERE TransactionID = '{TransactionID}'");
                            _ = this.incubeQuery.ExecuteNonQuery();

                            this.incubeQuery = new InCubeQuery(
                                this.db_vms,
                                $"EXEC SP_InsertSAPPostingData '{TransactionID}', '{request[0].d.BillingDocument}', 'SendBilling()', '{body}', '{responseBody.Replace("'", "*")}'");
                            _ = this.incubeQuery.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        this.incubeQuery = new InCubeQuery(
                            this.db_vms,
                            $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendBilling()', '{body}', '{responseBody.Replace("'", "*")}'");
                        _ = this.incubeQuery.ExecuteNonQuery();

                        Logger.WriteLog(MethodBase.GetCurrentMethod().DeclaringType.Name,
                                        MethodBase.GetCurrentMethod().Name,
                                        ex.Message,
                                        LoggingType.Error,
                                        LoggingFiles.InCubeLog);

                        stringBuilder.Append(ex.Message);

                        if (result == InCubeLibrary.Result.UnKnown)
                        {
                            result = InCubeLibrary.Result.Failure;
                            this.WriteMessage("Unhandled exception !!");
                        }
                    }
                    finally
                    {
                        this.execManager.LogIntegrationEnding(ID, result, "", stringBuilder.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().DeclaringType.Name,
                                MethodBase.GetCurrentMethod().Name,
                                ex.Message,
                                LoggingType.Error,
                                LoggingFiles.InCubeLog);
                this.WriteMessage("Fetching order failed !!");
            }
            finally
            {
                this.CloseSession();
            }
        }

        private void SendPGIOld(string TransactionID, string TransactionType,bool isExch)
        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));

                string salespersonFilter = "", DeliveryNumber = "";
                int processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string responseBody = "";
                string headerData = "";
                string headerQuery = "";

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND EmployeeID = " + Filters.EmployeeID;
                }
                //      string headerQuery = $@"SELECT * FROM V_POST_{(TransactionType == "WH" ? "OffloadPGI" : "SalesPGI")} {(TransactionID == "" ? "--" : "")} WHERE TransactionID = '{TransactionID}'";
                if (TransactionType == "WH")
                {
                    headerQuery = @"SELECT V_POST_OffloadPGI.DeliveryRef, V_POST_OffloadPGI.TransactionID
FROM V_POST_OffloadPGI
INNER JOIN WarehouseTransaction 
    ON V_POST_OffloadPGI.TransactionID = WarehouseTransaction.TransactionID";
                }
                else
                {
                    if (isExch == false)
                    {
                        headerQuery = string.Format(@"SELECT V_POST_SalesPGI.DeliveryRef,V_POST_SalesPGI.TransactionID FROM V_POST_SalesPGI
 inner join [Transaction] on V_POST_SalesPGI.TransactionID= [Transaction].TransactionID 
 	where  convert(date,TransactionDate) >= '{0}' AND convert(date,TransactionDate) <= '{1}' {2}", Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
                    }
                    else
                    {
                        headerQuery = string.Format(@"SELECT V_POST_SalesPGI.DeliveryRef,V_POST_SalesPGI.TransactionID FROM V_POST_SalesPGI
 inner join [Transaction] on V_POST_SalesPGI.TransactionID= [Transaction].TransactionID 
 	where  [Transaction].transactionid = '{0}' ", TransactionID);
                    }
                }

                incubeQuery = new InCubeQuery(db_vms, headerQuery);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("cash collection header query failed !!"));
                }

                DataTable dtHeader = incubeQuery.GetDataTable();
                if (dtHeader.Rows.Count == 0)
                    WriteMessage("There is no Order to send ..");
                else
                    SetProgressMax(dtHeader.Rows.Count);

                for (int i = 0; i < dtHeader.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();
                        ReportProgress("Sending Transaction: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(11, TransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        DeliveryNumber = dtHeader.Rows[i]["DeliveryRef"].ToString();      
                        TransactionID = dtHeader.Rows[i]["TransactionID"].ToString();

                        string currentDate = DateTime.Now.ToString("yyyyMMdd");
                        headerData = $"'{DeliveryNumber}''{currentDate}'";
                        MezzanComplexResult[] PGIResult = Tools.GetRequest<MezzanComplexResult>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_DELIVERY_SRV/PostGoodsIssue?DeliveryNumber=" + headerData, "", "", out responseBody, webHeader, cookies);
                        if (PGIResult[0].d.results[0].DeliveryNumber == null || (PGIResult[0].d.results[0].Message != null && PGIResult[0].d.results[0].Message.Trim() != "PGI has been Created"))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (PGIResult[0].d.results[0].DeliveryNumber != null ? PGIResult[0].d.results[0].DeliveryNumber : "") + "\r\n json:" + DeliveryNumber);
                            WriteMessage("Error .. \r\n" + TransactionID + " \r\n" + PGIResult[0].d.results[0].DeliveryNumber);

                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendPGI()', '{DeliveryNumber}_{currentDate}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + PGIResult[0].d.results[0].DeliveryNumber + "\r\n Message:" + (PGIResult[0].d.results[0].DeliveryNumber != null ? PGIResult[0].d.results[0].DeliveryNumber : "") + "\r\n json:" + DeliveryNumber);
                            WriteMessage("Success, ERP No: " + PGIResult[0].d.results[0].DeliveryNumber);
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE SAP_Reference SET PGIRef ='" + PGIResult[0].d.results[0].DeliveryNumber + "' WHERE TransactionID='" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();

                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertSAPPostingData '{TransactionID}', '{PGIResult[0].d.results[0].DeliveryNumber}', 'SendPGI()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                            if (TransactionType != "WH")
                            {
                                if (isExch == false)
                                {
                                    SendBilling(TransactionID);
                                    AboodAPI( TransactionID);
                                }
                            }
                            else
                            {
                                incubeQuery = new InCubeQuery(db_vms, $"delete from SAP_Error where transactionid = '{TransactionID}'");
                                incubeQuery.ExecuteNonQuery();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendPGI()', '{headerData.Replace("'", "_")}', '{responseBody.Replace("'", "*")}'");
                        incubeQuery.ExecuteNonQuery();
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

        private void SendPGI(string TransactionID, string TransactionType, bool isExch)
        {
            try
            {
                if (this.webHeader == null)
                    throw new Exception("Can't open session in API, please check the service!!");

                string str1 = "";
                int ID = 0;
                StringBuilder stringBuilder = new StringBuilder();
                InCubeLibrary.Result result = InCubeLibrary.Result.UnKnown;
                string responseBody = "";
                string str2 = "";

                if (this.Filters.EmployeeID != -1)
                    str1 = "AND EmployeeID = " + this.Filters.EmployeeID.ToString();

                string queryString;
                DateTime dateTime;

                if (TransactionType == "WH")
                {
                    queryString = @"SELECT DivisionCode, V_POST_OffloadPGI.DeliveryRef, V_POST_OffloadPGI.TransactionID
                            FROM V_POST_OffloadPGI
                            INNER JOIN WarehouseTransaction 
                            ON V_POST_OffloadPGI.TransactionID = WarehouseTransaction.TransactionID 
                            ORDER BY V_POST_OffloadPGI.TransactionID, DivisionCode";
                }
                else if (!isExch)
                {
                    dateTime = this.Filters.FromDate;
                    string fromDate = dateTime.ToString("yyyy-MM-dd");

                    dateTime = this.Filters.ToDate.AddDays(1);
                    string toDate = dateTime.ToString("yyyy-MM-dd");

                    queryString = $@"SELECT DivisionCode, V_POST_SalesPGI.DeliveryRef, V_POST_SalesPGI.TransactionID 
                             FROM V_POST_SalesPGI
                             INNER JOIN [Transaction] ON V_POST_SalesPGI.TransactionID = [Transaction].TransactionID
                             WHERE CONVERT(date, TransactionDate) >= '{fromDate}' 
                             AND CONVERT(date, TransactionDate) <= '{toDate}' {str1}
                             ORDER BY V_POST_SalesPGI.TransactionID, DivisionCode";
                }
                else
                {
                    queryString = $@"SELECT DivisionCode, V_POST_SalesPGI.DeliveryRef, V_POST_SalesPGI.TransactionID 
                             FROM V_POST_SalesPGI
                             INNER JOIN [Transaction] ON V_POST_SalesPGI.TransactionID = [Transaction].TransactionID
                             WHERE [Transaction].TransactionID = '{TransactionID}' 
                             ORDER BY V_POST_SalesPGI.TransactionID, DivisionCode";
                }

                this.incubeQuery = new InCubeQuery(this.db_vms, queryString);
                DataTable dataTable = this.incubeQuery.Execute() == 0
                    ? this.incubeQuery.GetDataTable()
                    : throw new Exception("Cash collection header query failed !!");

                if (dataTable.Rows.Count == 0)
                {
                    this.WriteMessage("There is no Order to send ..");
                }
                else
                {
                    this.SetProgressMax(dataTable.Rows.Count);
                }

                for (int index = 0; index < dataTable.Rows.Count; index++)
                {
                    try
                    {
                        result = InCubeLibrary.Result.UnKnown;
                        ID = 0;
                        stringBuilder = new StringBuilder();

                        this.ReportProgress("Sending Transaction: " + TransactionID);
                        this.WriteMessage($"\r\n{TransactionID}: ");

                        ID = this.execManager.LogIntegrationBegining(this.TriggerID, -1, new Dictionary<int, string>
                {
                    { 11, TransactionID }
                });

                        string deliveryRef = dataTable.Rows[index]["DeliveryRef"].ToString();
                        TransactionID = dataTable.Rows[index]["TransactionID"].ToString();
                        string divisionCode = dataTable.Rows[index]["DivisionCode"].ToString();

                        dateTime = DateTime.Now;
                        string today = dateTime.ToString("yyyyMMdd");

                        str2 = $"'{deliveryRef}''{today}'";

                        MezzanComplexResult[] request = Tools.GetRequest<MezzanComplexResult>(
                            $"{CoreGeneral.Common.GeneralConfigurations.WS_URL}ZHH_INC_DELIVERY_SRV/PostGoodsIssue?DeliveryNumber={str2}",
                            "", "", out responseBody, this.webHeader, this.cookies);

                        if (request[0].d.results[0].DeliveryNumber == null ||
                            (request[0].d.results[0].Message != null &&
                             request[0].d.results[0].Message.Trim() != "PGI has been Created"))
                        {
                            result = InCubeLibrary.Result.NoFileRetreived;
                            stringBuilder.Append($"ERP ERROR Message: {(request[0].d.results[0].DeliveryNumber ?? "")}\r\n json: {deliveryRef}");
                            this.WriteMessage($"Error .. \r\n{TransactionID} \r\n{request[0].d.results[0].DeliveryNumber}");

                            this.incubeQuery = new InCubeQuery(this.db_vms,
                                $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendPGI()', '{deliveryRef}_{today}', '{responseBody.Replace("'", "*")}'");
                            _ = this.incubeQuery.ExecuteNonQuery();
                        }
                        else
                        {
                            result = InCubeLibrary.Result.Success;
                            stringBuilder.Append($"ERP No: {request[0].d.results[0].DeliveryNumber}\r\nMessage: {request[0].d.results[0].DeliveryNumber}\r\njson: {deliveryRef}");
                            this.WriteMessage("Success, ERP No: " + request[0].d.results[0].DeliveryNumber);

                            this.incubeQuery = new InCubeQuery(this.db_vms,
                                $"UPDATE SAP_Reference SET PGIRef = '{request[0].d.results[0].DeliveryNumber}' WHERE TransactionID = '{TransactionID}' AND DivisionCode = '{divisionCode}'");
                            _ = this.incubeQuery.ExecuteNonQuery();

                            this.incubeQuery = new InCubeQuery(this.db_vms,
                                $"EXEC SP_InsertSAPPostingData '{TransactionID}', '{request[0].d.results[0].DeliveryNumber}', 'SendPGI()', '{str2}', '{responseBody.Replace("'", "*")}'");
                            _ = this.incubeQuery.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        this.incubeQuery = new InCubeQuery(this.db_vms,
                            $"EXEC SP_InsertUpdateErrors '{TransactionID}', 'SendPGI()', '{str2.Replace("'", "_")}', '{responseBody.Replace("'", "*")}'");
                        _ = this.incubeQuery.ExecuteNonQuery();

                        Logger.WriteLog(MethodBase.GetCurrentMethod().DeclaringType.Name,
                                        MethodBase.GetCurrentMethod().Name,
                                        ex.Message,
                                        LoggingType.Error,
                                        LoggingFiles.InCubeLog);

                        stringBuilder.Append(ex.Message);

                        if (result == InCubeLibrary.Result.UnKnown)
                        {
                            result = InCubeLibrary.Result.Failure;
                            this.WriteMessage("Unhandled exception !!");
                        }
                    }
                    finally
                    {
                        this.execManager.LogIntegrationEnding(ID, result, "", stringBuilder.ToString());
                    }
                }

                if (TransactionType != "WH")
                {
                    if (!isExch)
                    {
                        this.SendBilling(TransactionID);
                        this.AboodAPI(TransactionID);
                    }
                }
                else
                {
                    this.incubeQuery = new InCubeQuery(this.db_vms, $"DELETE FROM SAP_Error WHERE TransactionID = '{TransactionID}'");
                    _ = this.incubeQuery.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().DeclaringType.Name,
                                MethodBase.GetCurrentMethod().Name,
                                ex.Message,
                                LoggingType.Error,
                                LoggingFiles.InCubeLog);
                this.WriteMessage("Fetching order failed !!");
            }
            finally
            {
                this.CloseSession();
            }
        }

        public override void SendReciepts()
        {
            SendCashCollection();
            SendChequeCollection();
            SendBankCollection();
        }

        private void SendBankCollection()
        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));

                string salespersonFilter = "", VoucherNumber = "", CustomerCode = "", DivisionCode="", VoucherDate = "", CustomerPaymentID = "", PayType="", EmployeeCode = "";
                int processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string responseBody = "";
                string headerData = "";

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND EmployeeID = " + Filters.EmployeeID;
                }
                string headerQuery = string.Format(@"SELECT * FROM V_POST_BankCollection
where  convert(date,PaymentDate) >= '{0}' AND convert(date,PaymentDate) <= '{1}' {2}", Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter); incubeQuery = new InCubeQuery(db_vms, headerQuery);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Order header query failed !!"));
                }

                DataTable dtHeader = incubeQuery.GetDataTable();
                if (dtHeader.Rows.Count == 0)
                    WriteMessage("There is no Order to send ..");
                else
                    SetProgressMax(dtHeader.Rows.Count);

                for (int i = 0; i < dtHeader.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();
                        CustomerPaymentID = dtHeader.Rows[i]["CustomerPaymentID"].ToString();
                        DivisionCode = dtHeader.Rows[i]["DivisionCode"].ToString();

                        ReportProgress("Sending Transaction: " + CustomerPaymentID);
                        WriteMessage("\r\n" + CustomerPaymentID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(11, CustomerPaymentID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        CustomerCode = dtHeader.Rows[i]["CustomerCode"].ToString();
                        VoucherNumber = dtHeader.Rows[i]["VoucherNumber"].ToString();
                        VoucherDate = dtHeader.Rows[i]["VoucherDate"].ToString();
                        PayType = dtHeader.Rows[i]["PayType"].ToString();
                        EmployeeCode = dtHeader.Rows[i]["EmployeeCode"].ToString();

                        string detailsQuery = $@"select SR.BillingRef, CP.AppliedAmount, CP.AppliedPaymentID, FORMAT(ROW_NUMBER() OVER (ORDER BY CP.AppliedPaymentID), '000') LineNum from CustomerPayment CP
                                                INNER JOIN SAP_Reference SR ON SR.TransactionID = CP.TransactionID
                                                LEFT JOIN SAP_Reference S ON S.TransactionID = CP.AppliedPaymentID
                                                where CustomerPaymentID = '{CustomerPaymentID}' AND SR.BillingRef IS NOT NULL AND (S.TransactionID IS NULL OR S.CollectionRef IS NULL)";

                        incubeQuery = new InCubeQuery(db_vms, detailsQuery);
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            throw (new Exception("customer payment details query failed !!"));
                        }

                        DataTable dtDetails = incubeQuery.GetDataTable();
                        List<object> allDetailsList = new List<object>();
                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        {
                            string BillingRef = "", AppliedAmount = "", LineNo = "", AppliedPaymentID = "";

                            BillingRef = dtDetails.Rows[j]["BillingRef"].ToString();
                            AppliedAmount = decimal.Parse(dtDetails.Rows[j]["AppliedAmount"].ToString()).ToString("#0.000");
                            LineNo = dtDetails.Rows[j]["LineNum"].ToString();
                            AppliedPaymentID = dtDetails.Rows[j]["AppliedPaymentID"].ToString();

                            var details = new
                            {
                                Itemno = LineNo,
                                DocumentHdrTxt = PayType,
                                AmountLc = AppliedAmount,
                                Assignment = BillingRef
                            };

                            allDetailsList.Add(details);

                            incubeQuery = new InCubeQuery(db_vms, $"INSERT INTO [SAP_Reference] (TransactionID,BillingRef,PGIRef,DeliveryRef) VALUES ('{AppliedPaymentID}','{BillingRef}','Bank','{CustomerPaymentID}')");
                            incubeQuery.ExecuteNonQuery();
                        }

                        var headerDataObject = new
                        {
                            CompanyCode = DivisionCode,
                            DocumentDate = VoucherDate,
                            Reference = VoucherNumber,
                            DocumentHdrTxt = PayType,
                            Customer = CustomerCode,
                            SalesmanCode = EmployeeCode,
                            Message = "",
                            DocumentNumber = "",
                            PostToItem = allDetailsList
                        };

                        headerData = JsonConvert.SerializeObject(headerDataObject);

                        MezzanSimpleResult[] bankResult = Tools.GetRequest<MezzanSimpleResult>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_BANK_POSTING_SRV/PostingDataSet", headerData, "", out responseBody, webHeader, cookies);
                        if (bankResult[0].d.DocumentNumber == null || (bankResult[0].d.Message != null && bankResult[0].d.Message.Trim() != "Document posted successfully"))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (bankResult[0].d.Message != null ? bankResult[0].d.Message : "") + "\r\n json:" + headerData);
                            WriteMessage("Error .. \r\n" + CustomerPaymentID + " \r\n" + bankResult[0].d.DocumentNumber);

                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{CustomerPaymentID}','SendBankCollection()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + bankResult[0].d.DocumentNumber + "\r\n Message:" + (bankResult[0].d.Message != null ? bankResult[0].d.Message : "") + "\r\n json:" + headerData);
                            WriteMessage("Success, ERP No: " + bankResult[0].d.DocumentNumber);
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerPayment] SET Synchronized = 1  WHERE CustomerPaymentID = '" + CustomerPaymentID + "'");
                            incubeQuery.ExecuteNonQuery();

                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertSAPPostingData '{CustomerPaymentID}', '{bankResult[0].d.DocumentNumber}', 'SendBankCollection()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [SAP_Reference] SET CollectionRef = '" + bankResult[0].d.DocumentNumber + "' WHERE PGIRef='Bank' and DeliveryRef = '" + CustomerPaymentID + "'");
                            incubeQuery.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{CustomerPaymentID}','SendBankCollection()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                        incubeQuery.ExecuteNonQuery();
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
     
        private void SendChequeCollection()
        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));

                string salespersonFilter = "", BankCode = "", VoucherNumber = "", EmployeeCode = "", VoucherDate = "", CustomerPaymentID = "";
                int processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string responseBody = "";
                string headerData = "";

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND EmployeeID = " + Filters.EmployeeID;
                }
                string headerQuery = string.Format(@"SELECT * FROM V_POST_ChequeCollection
where  convert(date,PaymentDate) >= '{0}' AND convert(date,PaymentDate) <= '{1}' {2}", Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter); incubeQuery = new InCubeQuery(db_vms, headerQuery);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Order header query failed !!"));
                }

                DataTable dtHeader = incubeQuery.GetDataTable();
                if (dtHeader.Rows.Count == 0)
                    WriteMessage("There is no Order to send ..");
                else
                    SetProgressMax(dtHeader.Rows.Count);

                for (int i = 0; i < dtHeader.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();
                        CustomerPaymentID = dtHeader.Rows[i]["CustomerPaymentID"].ToString();
                        ReportProgress("Sending Transaction: " + CustomerPaymentID);
                        WriteMessage("\r\n" + CustomerPaymentID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(11, CustomerPaymentID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        BankCode = dtHeader.Rows[i]["Code"].ToString();
                        EmployeeCode = dtHeader.Rows[i]["EmployeeCode"].ToString();
                        VoucherNumber = dtHeader.Rows[i]["VoucherNumber"].ToString();
                        VoucherDate = dtHeader.Rows[i]["VoucherDate"].ToString();

                        string detailsQuery = $@"select SR.BillingRef, CP.AppliedAmount, CP.AppliedPaymentID from CustomerPayment CP
                                                INNER JOIN SAP_Reference SR ON SR.TransactionID = CP.TransactionID
                                                LEFT JOIN SAP_Reference S ON S.TransactionID = CP.AppliedPaymentID
                                                where CustomerPaymentID = '{CustomerPaymentID}' AND SR.BillingRef IS NOT NULL AND (S.TransactionID IS NULL OR S.CollectionRef IS NULL)";

                        incubeQuery = new InCubeQuery(db_vms, detailsQuery);
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            throw (new Exception("customer payment details query failed !!"));
                        }

                        DataTable dtDetails = incubeQuery.GetDataTable();
                        List<object> allDetailsList = new List<object>();
                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        {
                            string BillingRef = "", AppliedAmount="", AppliedPaymentID="";

                            BillingRef = dtDetails.Rows[j]["BillingRef"].ToString();
                            AppliedAmount = decimal.Parse(dtDetails.Rows[j]["AppliedAmount"].ToString()).ToString("#0.000");
                            AppliedPaymentID = dtDetails.Rows[j]["AppliedPaymentID"].ToString();

                            var details = new
                            {
                                CheckNo = "",
                                BillingNo = BillingRef,
                                Amount = AppliedAmount,
                                DocumentNumber = "",
                                Message = ""
                            };

                            allDetailsList.Add(details);


                            incubeQuery = new InCubeQuery(db_vms, $"INSERT INTO [SAP_Reference] (TransactionID,BillingRef, PGIRef, DeliveryRef) VALUES ('{AppliedPaymentID}','{BillingRef}', 'Cheque', '{CustomerPaymentID}')");
                            incubeQuery.ExecuteNonQuery();
                        }

                        var headerDataObject = new
                        {
                            BankKey = BankCode,
                            CheckNo = VoucherNumber,
                            Date = VoucherDate,
                            Customer = EmployeeCode,
                            CheckToBilling = allDetailsList
                        };

                        headerData = JsonConvert.SerializeObject(headerDataObject);

                        MezzanComplexResult[] chequeResult = Tools.GetRequest<MezzanComplexResult>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_CHECK_COLLECTION_SRV/CheckDataSet", headerData, "", out responseBody, webHeader, cookies);
                        if (chequeResult[0].d.CheckToBilling.results[0].BillingNo == null || (chequeResult[0].d.CheckToBilling.results[0].DocumentNumber != null && chequeResult[0].d.CheckToBilling.results[0].DocumentNumber.Trim() == ""))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (chequeResult[0].d.CheckToBilling.results[0].Message != null ? chequeResult[0].d.CheckToBilling.results[0].Message : "") + "\r\n json:" + headerData);
                            WriteMessage("Error .. \r\n" + CustomerPaymentID + " \r\n" + chequeResult[0].d.CheckToBilling.results[0].BillingNo);

                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{CustomerPaymentID}','SendChequeCollection()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + chequeResult[0].d.CheckToBilling.results[0].DocumentNumber + "\r\n Message:" + (chequeResult[0].d.CheckToBilling.results[0].Message != null ? chequeResult[0].d.CheckToBilling.results[0].Message : "") + "\r\n json:" + headerData);
                            WriteMessage("Success, ERP No: " + chequeResult[0].d.CheckToBilling.results[0].BillingNo);
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [CustomerPayment] SET Synchronized = 1  WHERE CustomerPaymentID = '" + CustomerPaymentID + "'");
                            incubeQuery.ExecuteNonQuery();

                            foreach(var item in chequeResult[0].d.CheckToBilling.results)
                            {
                                incubeQuery = new InCubeQuery(db_vms, "UPDATE [SAP_Reference] SET CollectionRef = '" + item.DocumentNumber + "' WHERE BillingRef = '" + item.BillingNo + "' and PGIRef='Cheque' and DeliveryRef = '" + CustomerPaymentID + "'");
                                incubeQuery.ExecuteNonQuery();
                                incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertSAPPostingData '{CustomerPaymentID}', '{item.DocumentNumber}', 'SendChequeCollection()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                                incubeQuery.ExecuteNonQuery();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{CustomerPaymentID}','SendChequeCollection()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                        incubeQuery.ExecuteNonQuery();
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

        private void SendCashCollection()
        {
            try
            {
                if (webHeader == null) throw (new Exception("Can't open session in API , please check the service!!"));

                string salespersonFilter = "", BillingRef = "", EmployeeCode = "", AppliedAmount = "", PayYear = "", TransactionID = "", AppliedPaymentID ="", CustomerPaymentID= "";
                int processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                string responseBody = "";
                string headerData = "";

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND EmployeeID = " + Filters.EmployeeID;
                }
                string headerQuery = string.Format(@"SELECT * FROM V_POST_CashCollection
where  convert(date,PaymentDate) >= '{0}' AND convert(date,PaymentDate) <= '{1}' {2}", Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
                incubeQuery = new InCubeQuery(db_vms, headerQuery);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("cash collection header query failed !!"));
                }

                DataTable dtHeader = incubeQuery.GetDataTable();
                if (dtHeader.Rows.Count == 0)
                    WriteMessage("There is no Order to send ..");
                else
                    SetProgressMax(dtHeader.Rows.Count);

                for (int i = 0; i < dtHeader.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();
                        BillingRef = dtHeader.Rows[i]["BillingRef"].ToString();
                        ReportProgress("Sending Delivery: " + BillingRef);
                        WriteMessage("\r\n" + BillingRef + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(11, BillingRef);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        EmployeeCode = dtHeader.Rows[i]["EmployeeCode"].ToString();
                        AppliedAmount = decimal.Parse(dtHeader.Rows[i]["AppliedAmount"].ToString()).ToString("#0.000");
                        PayYear = dtHeader.Rows[i]["PayYear"].ToString();
                        TransactionID = dtHeader.Rows[i]["TransactionID"].ToString();
                        AppliedPaymentID = dtHeader.Rows[i]["AppliedPaymentID"].ToString();
                        CustomerPaymentID = dtHeader.Rows[i]["CustomerPaymentID"].ToString();

                        var headerDataObject = new
                        {
                            BillingDoc = BillingRef,
                            Customer = EmployeeCode,
                            Amount = AppliedAmount,
                            CompanyCode = "1930",
                            Year = PayYear,
                            DocumentNumber = ""
                        };

                        headerData = JsonConvert.SerializeObject(headerDataObject);

                        MezzanSimpleResult[] cashResult = Tools.GetRequest<MezzanSimpleResult>(CoreGeneral.Common.GeneralConfigurations.WS_URL + "ZHH_INC_CREATE_PAYMENT_SRV/PaymentSet", headerData, "", out responseBody, webHeader, cookies);
                        if (cashResult[0].d.DocumentNumber == null || (cashResult[0].d.DocumentNumber != null && cashResult[0].d.DocumentNumber.Trim() == ""))
                        {
                            res = Result.NoFileRetreived;
                            result.Append("ERP ERROR Message:" + (cashResult[0].d.DocumentNumber != null ? cashResult[0].d.DocumentNumber : "") + "\r\n json:" + headerData);
                            WriteMessage("Error .. \r\n" + TransactionID + " \r\n" + cashResult[0].d.DocumentNumber);

                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{CustomerPaymentID}','SendCashCollection()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("ERP No: " + cashResult[0].d.DocumentNumber + "\r\n Message:" + (cashResult[0].d.DocumentNumber != null ? cashResult[0].d.DocumentNumber : "") + "\r\n json:" + headerData);
                            WriteMessage("Success, ERP No: " + cashResult[0].d.DocumentNumber);
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE CustomerPayment SET Synchronized = 1 WHERE TransactionID = '" + TransactionID + "' AND AppliedPaymentID = '" + AppliedPaymentID + "' AND CustomerPaymentID = '" + CustomerPaymentID + "'");
                            incubeQuery.ExecuteNonQuery();
                            incubeQuery = new InCubeQuery(db_vms, $"INSERT INTO [SAP_Reference] (TransactionID,BillingRef,CollectionRef,PGIref,DeliveryRef) VALUES ('{AppliedPaymentID}','{BillingRef}','{cashResult[0].d.DocumentNumber}', 'Cash', '{CustomerPaymentID}')");
                            incubeQuery.ExecuteNonQuery();
                            incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertSAPPostingData '{CustomerPaymentID}', '{cashResult[0].d.DocumentNumber}', 'SendCashCollection()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                            incubeQuery.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        incubeQuery = new InCubeQuery(db_vms, $"EXEC SP_InsertUpdateErrors '{CustomerPaymentID}','SendCashCollection()', '{headerData}', '{responseBody.Replace("'", "*")}'");
                        incubeQuery.ExecuteNonQuery();
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
    }

    class MezzanItems
    {
        public string IPrCode { get; set; }
        public string IPrBarcode { get; set; }
        public string IPrEanCgy { get; set; }
        public string IPrEnLngKey { get; set; }
        public string IPrDescription { get; set; }
        public string IPrArLngKey { get; set; }
        public string IPrDescriptionAr { get; set; }
        public string IPrPgCode { get; set; }
        public string IPrBrCode { get; set; }
        public string IPrCpCode { get; set; }
        public string IPrDstrbChnl { get; set; }
        public string Plant { get; set; }
        public string IPrVat { get; set; }
        public string IPrDeonmtr { get; set; }
        public string IPrUom { get; set; }
        public string IPrNumtr { get; set; }
        public string IPrFactor { get; set; }
        public string IPrType { get; set; }
        public string IPrTypeLngKey { get; set; }
        public string IPrTypeDescEn { get; set; }
        public string IPrTypeDescAr { get; set; }
        public string IPrSaleUnit { get; set; }
        public string IPrDelFlgDstrb { get; set; }
        public string IPrDstrbStatus { get; set; }
        public string IPrPrcGp { get; set; }
        public string IPrGp1 { get; set; }
        public string IPrGp2 { get; set; }
        public string IPrGp3 { get; set; }
        public string IPrGp4 { get; set; }
        public string IPrGp5 { get; set; }
        public string IPrAttr1 { get; set; }
        public string IPrAttr2 { get; set; }
        public string IPrAttr3 { get; set; }
        public string IPrAttr4 { get; set; }
        public string IPrAttr5 { get; set; }
        public string IPrAttr6 { get; set; }
        public string IPrAttr7 { get; set; }
        public string IPrAttr8 { get; set; }
        public string IPrAttr9 { get; set; }
        public string IPrAttr10 { get; set; }
        public string DeliveryPlant { get; set; }
        public string IPrDelFlgPlnt { get; set; }
        public string IPrBatchMgmtInd { get; set; }
        public string IPrPlntSpcStatus { get; set; }
        public string IPrStatusBlk { get; set; }
        public string IPrDstrbStatusBlk { get; set; }
        public string IPrTaxMaterial { get; set; }
        public string IPrTaxRate { get; set; }
        public string IPrIndicator { get; set; }
        public string IPrPgcodeDesc { get; set; }
        public string IPrPgcodeDescAr { get; set; }
    }

    class MezzanCustomers
    {
        public string ICuCode { get; set; }
        public string ICuCpCodeSorg { get; set; }
        public string ICuDstrbChnl { get; set; }
        public string ICuDiv { get; set; }
        public string ICuType { get; set; }
        public string ICuCgCode { get; set; }
        public string ICuSalGpKey { get; set; }
        public string ICuSalGpDesc { get; set; }
        public string ICuSalGpCode { get; set; }
        public string ICuBarcode { get; set; }
        public string ICuNameAr { get; set; }
        public string ICuName { get; set; }
        public string ICuAddressAr { get; set; }
        public string ICuAddress { get; set; }
        public string ICuPhone { get; set; }
        public string ICuMobile { get; set; }
        public string ICuContactname { get; set; }
        public string ICuRgCode { get; set; }
        public string ICuCrCode { get; set; }
        public string ICuRemark { get; set; }
        public string ICuTaxNo { get; set; }
        public string ICuCodeOrig { get; set; }
        public string ICuBalance { get; set; }
        public string ICuCreditLimit { get; set; }
        public string ICuPaymentTerms { get; set; }
        public string ICuPayTermsDesc { get; set; }
        public string ICuCrn { get; set; }
        public string ICuSmCode { get; set; }
        public string ICuCpCode1 { get; set; }
        public string ICuPstBlkCpCode { get; set; }
        public string ICuDelFlgMrCp { get; set; }
        public string ICuDelBlkMrCpCode { get; set; }
        public string ICuFax { get; set; }
        public string ICuCntPstBlk { get; set; }
        public string ICuCntDelFlgMr { get; set; }
        public string ICuCntDelBlkMr { get; set; }
        public string ICuCntOrdBlk { get; set; }
        public string ICuCntDlvBlk { get; set; }
        public string ICuCntBilBlk { get; set; }
        public string ICuCntSalBlk { get; set; }
        public string ICuAttr1 { get; set; }
        public string ICuAttr2 { get; set; }
        public string ICuAttr3 { get; set; }
        public string ICuAttr4 { get; set; }
        public string ICuAttr5 { get; set; }
        public string ICuAttr6 { get; set; }
        public string ICuAttr7 { get; set; }
        public string ICuAttr8 { get; set; }
        public string ICuAttr9 { get; set; }
        public string ICuAttr10 { get; set; }
        public string ICuAddress2 { get; set; }
        public string ICuVatRegNo { get; set; }
        public string ICuBldgNo { get; set; }
        public string ICuFlrNo { get; set; }
        public string ICuAptNo { get; set; }
        public string ICuAddress1 { get; set; }
        public string ICuStreet { get; set; }
        public string ICuDistrict { get; set; }
        public string ICuCity { get; set; }
        public string ICuPostalCode { get; set; }
        public string ICuCity1 { get; set; }
        public string ICuCntyKey { get; set; }
        public string ICuAddressNo { get; set; }
        public string ICuMail { get; set; }
        public string ICuDelFlgSal { get; set; }
        public string ICuSalBlkArea { get; set; }
        public string ICuOrdBlkArea { get; set; }
        public string ICuDlvBlkArea { get; set; }
        public string ICuBilBlkSalDstrb { get; set; }
        public string ICuSalDistrict { get; set; }
        public string ICuDlvPlnt { get; set; }
        public string ICuSalOffice { get; set; }
        public string ICuGp1 { get; set; }
        public string ICuGp2 { get; set; }
        public string ICuGp3 { get; set; }
        public string ICuGp4 { get; set; }
        public string ICuGp5 { get; set; }
        public string ICuCrdCntlArea { get; set; }
        public string ICuLongitude { get; set; }
        public string ICuLatitude { get; set; }
        public string ICuPayrPatnrFnc { get; set; }
        public string ICuCrdBlk { get; set; }
        public string ICuCndtGp1 { get; set; }
        public string ICuCndtGp2 { get; set; }
        public string ICuCndtGp3 { get; set; }
        public string ICuCndtGp4 { get; set; }
        public string ICuCndtGp5 { get; set; }
        public string ICuPlCode { get; set; }
        public string ICuGp1Desc { get; set; }
        public string ICuGp2Desc { get; set; }
        public string ICuGp3Desc { get; set; }
        public string ICuGp4Desc { get; set; }
        public string ICuGp5Desc { get; set; }
        public string ICuCndtGp1Desc { get; set; }
        public string ICuCndtGp2Desc { get; set; }
        public string ICuCndtGp3Desc { get; set; }
        public string ICuCndtGp4Desc { get; set; }
        public string ICuCndtGp5Desc { get; set; }
        public string ICuAttr1Desc { get; set; }
        public string ICuAttr2Desc { get; set; }
        public string ICuAttr3Desc { get; set; }
        public string ICuAttr4Desc { get; set; }
        public string ICuAttr5Desc { get; set; }
        public string ICuAttr6Desc { get; set; }
        public string ICuAttr7Desc { get; set; }
        public string ICuAttr8Desc { get; set; }
        public string ICuAttr9Desc { get; set; }
        public string ICuAttr10Desc { get; set; }
        public string ICucomreg1 { get; set; }
        public string ICucomreg2 { get; set; }
        public string ICuaccgrp { get; set; }
        public string ICukfh { get; set; }
        public string ICuHouseNum { get; set; }
    }

    class MezzanSalesmanStock
    {
        public string IWpSmCode { get; set; }
        public string IWpPrCode { get; set; }
        public string IWpCpCode { get; set; }
        public string IWpSmPlnt { get; set; }
        public string IWpWarehouseCode { get; set; }
        public string IWpQuantity { get; set; }
        public string IWpLotnumber { get; set; }
        public string IWpExpiryDate { get; set; }
        public string IWpUom { get; set; }
    }
    class MezzanDueInvoice
    {
        public string IDiCuCode { get; set; }
        public string Vkorg { get; set; }
        public string Zcustype { get; set; }
        public string IDiSmCode { get; set; }
        public string IDiRgCode { get; set; }
        public string IDiCrCode { get; set; }
        public string IDiCpCode { get; set; }
        public string IDiInvoiceNb { get; set; }
        public string IDiBillNb { get; set; }
        public string IDiDate { get; set; }
        public string IDiDueDate { get; set; }
        public string IDiTotalAmount { get; set; }
        public string IDiRemainingAmount { get; set; }
    }

    class MezzanB2BInvoice
    {
        public string Zcustype { get; set; }
        public string IDiPayerNumber { get; set; }
        public string IDiCpCode { get; set; }
        public string IDiSmCode { get; set; }
        public string IDiCuCode { get; set; }
        public string IDiInvoiceNb { get; set; }
        public string IDiDate { get; set; }
        public string IDiTotalAmount { get; set; }
        public string IDiRetAmount { get; set; }
        public string IDiCashAmount { get; set; }
        public string IDiChequeAmount { get; set; }
        public string IDiCollectedAmount { get; set; }
        public string IDiRemainingAmount { get; set; }
        public string IDiCrCode { get; set; }
        public string IDiBillingNo { get; set; }
    }

    class MezzanPriceList
    {
        public string Idate { get; set; }
        public string ICplConditionType { get; set; }
        public string IPlCpCode { get; set; }
        public string ICplPlant { get; set; }
        public string ICplDistrChl { get; set; }
        public string IPlPrCode { get; set; }
        public string ICplFrom { get; set; }
        public string ICplTo { get; set; }
        public string ICplAConditionRecord { get; set; }
        public string ICplKConditionRecord { get; set; }
        public string ICplKConditionType { get; set; }
        public string IPlPrice { get; set; }
        public string IPlCrCode { get; set; }
        public string ICplConditionUnit { get; set; }
        public string IPlUom { get; set; }
        public string ICplDeletionIndicator { get; set; }
        public string IPlType { get; set; }
        public string IPlPriority { get; set; }
        public string IPlCommon { get; set; }
        public string IPlPar { get; set; }
    }
    class MezzanLoadTransfer
    {
        public string Vkorg { get; set; }
        public string Vtweg { get; set; }
        public string Kunnr { get; set; }
        public string Audat { get; set; }
        public string ICldReqQty { get; set; }
        public string ICldReqUom { get; set; }
        public string ICldClCode { get; set; }
        public string IClLrqNo { get; set; }
        public string ICldCode { get; set; }
        public string ICldPrCode { get; set; }
        public string ICldQty { get; set; }
        public string ICldLotno { get; set; }
        public string ICldPlant { get; set; }
        public string ICldExpiryDate { get; set; }
        public string ICldUom { get; set; }
        public string ICldQtyBase { get; set; }
        public string ICldBaseUnit { get; set; }
        public string ICldHHNumber { get; set; }
        public string ICldCreationDate { get; set; }
    }
    class MezzanDeliveryInvoice
    {
        public string IDlvReqQty { get; set; }
        public string IDlvReqUom { get; set; }
        public string Vkorg { get; set; }
        public string Vtweg { get; set; }
        public string Kunnr { get; set; }
        public string IDlvPrSeqMain { get; set; }
        public string IDlvPrSeq { get; set; }
        public string IDlvPrCode { get; set; }
        public string IDlvDeliveredQty { get; set; }
        public string IDlvLotno { get; set; }
        public string IDlvUom { get; set; }
        public string IDlvInvoiceno { get; set; }
        public string IDlvBillingno { get; set; }
        public string IItemCat { get; set; }
        public string IItemCatDesc { get; set; }
        public string IDlvBillingDate { get; set; }
        public string IDlvSalesOrderno { get; set; }
    }
    class MezzanUnpaid
    {
        public string ICsCpCode { get; set; }
        public string ICsGlAccount { get; set; }
        public string ICsDocNumber { get; set; }
        public string ICsTsNumber { get; set; }
        public string ICsReference { get; set; }
        public string ICsPayRef { get; set; }
        public string ICsPostDate { get; set; }
        public string ICsDocDate { get; set; }
        public string ICsMonth { get; set; }
        public string ICsYear { get; set; }
        public string ICsSmNumber { get; set; }
        public string ICsDocType { get; set; }
        public string ICsAmount { get; set; }
        public string ICsCurr { get; set; }
        public string ICsInvRef { get; set; }
        public string ICsRef1 { get; set; }
        public string ICsRef2 { get; set; }
    }

    class MezzanDiscount
    {
        public string Idate { get; set; }
        public string ICplConditionType { get; set; }
        public string IPlCpCode { get; set; }
        public string ICplPlant { get; set; }
        public string ICplDistrChl { get; set; }
        public string ICplDivision { get; set; }
        public string ICplSalesGrp { get; set; }
        public string ICplCuCode { get; set; }
        public string IPlPrCode { get; set; }
        public string ICplFrom { get; set; }
        public string ICplTo { get; set; }
        public string ICplAConditionRecord { get; set; }
        public string ICplKConditionRecord { get; set; }
        public string ICplKConditionType { get; set; }
        public string IPlPrice { get; set; }
        public string IPlCrCode { get; set; }
        public string ICplConditionUnit { get; set; }
        public string IPlUom { get; set; }
        public string ICplDeletionIndicator { get; set; }
        public string IPlType { get; set; }
        public string IPlPriority { get; set; }
        public string IPlCommon { get; set; }
        public string IPlPar { get; set; }
    }

    class MezzanTaxRate
    {
        public string IpCondType { get; set; }
        public string IpCountry { get; set; }
        public string IpCustomer { get; set; }
        public string IpMaterial { get; set; }
        public string IpValidfrom { get; set; }
        public string IpValidto { get; set; }
        public string IpCondRecord { get; set; }
        public string IpAmount { get; set; }
        public string IpTaxCode { get; set; }
        public string IpDeletionInd { get; set; }
    }
    class MezzanCollectionReversal
    {
        public string CompanyCode { get; set; }
        public string PostingDate { get; set; }
        public string DocumentDate { get; set; }
        public string PostingYear { get; set; }
        public string PostingPeriod { get; set; }
        public string LineItem { get; set; }
        public string DocumentNum { get; set; }
        public string BillingNum { get; set; }
        public string DocumentType { get; set; }
        public string Reference { get; set; }
        public string DocumentHdrTxt { get; set; }
        public string ReverseDocNum { get; set; }
        public string ReverseFiscalYear { get; set; }
        public string ReasonReversal { get; set; }
        public string Customer { get; set; }
        public string AccountType { get; set; }
        public string ClearingDoc { get; set; }
        public string ClearingDate { get; set; }
        public string ClearingFisyear { get; set; }
        public string PostingKey { get; set; }
        public decimal LocalCurrency { get; set; }
        public string DrCrIndicator { get; set; }
        public string AmountCurrency { get; set; }
        public string Text { get; set; }
        public string BaselinePaymentDate { get; set; }
        public string PaymentTerms { get; set; }
        public string PaymentReference { get; set; }
        public string Referencekey1 { get; set; }
        public string Referencekey2 { get; set; }
        public string Referencekey3 { get; set; }
    }

    class MezzanAllocation
    {
        public string CompanyCode { get; set; }
        public string PostingDate { get; set; }
        public string DocumentDate { get; set; }
        public string PostingYear { get; set; }
        public string PostingPeriod { get; set; }
        public string LineItem { get; set; }
        public string DocumentNum { get; set; }
        public string DocumentType { get; set; }
        public string Reference { get; set; }
        public string DocumentHdrTxt { get; set; }
        public string ReverseDocNum { get; set; }
        public string ReverseFiscalYear { get; set; }
        public string ReasonReversal { get; set; }
        public string Customer { get; set; }
        public string AccountType { get; set; }
        public string ClearingDoc { get; set; }
        public string ClearingDate { get; set; }
        public string ClearingFisyear { get; set; }
        public string PostingKey { get; set; }
        public string LocalCurrency { get; set; }
        public string DrCrIndicator { get; set; }
        public string AmountCurrency { get; set; }
        public string Text { get; set; }
        public string BaselinePaymentDate { get; set; }
        public string PaymentTerms { get; set; }
        public string PaymentReference { get; set; }
        public string ReferenceKey1 { get; set; }
        public string ReferenceKey2 { get; set; }
        public string ReferenceKey3 { get; set; }
    }

    class MezzanSimpleResult
    {
        public ResultDocument d { get; set; }
    }

    class MezzanComplexResult
    {
        public MezzanResultData d { get; set; }
    }

    class MezzanResultData
    {
        public HeaderResults HeaderToItem { get; set; }
        public HeaderResults CheckToBilling { get; set; }
        public ResultDocument[] results { get; set; }
    }
    class HeaderResults
    {
        public ResultDocument[] results { get; set; }
    }
    class ResultDocument
    {
        public string SalesDocumentno { get; set; }
        public string DeliveryNumber { get; set; }
        public string BillingDocument { get; set; }
        public string Message { get; set; }
        public string DocumentNumber { get; set; }
        public string BillingNo { get; set; }
    }
}