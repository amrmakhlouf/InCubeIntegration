using InCubeIntegration_DAL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Xml;
using Oracle.ManagedDataAccess.Client;
using System.Diagnostics;
namespace InCubeIntegration_BL
{
    public class IntegrationBase : IDisposable
    {
        #region Member Data
        public int OrgTriggerID = -1;
        public int TriggerID = -1;
        public int LastProcessID = -1;
        public int TaskID = -1;
        public int ActionID = -1;
        public int FieldID = -1;
        private int _organizationID = 0;
        public bool CommonMastersUpdate = false;
        public int OrganizationID
        {
            get
            {
                return _organizationID;
            }
            set
            {
                _organizationID = value;
                OrganizationCode = GetFieldValue("Organization", "OrganizationCode", "OrganizationID = " + value, db_vms);
            }
        }
        public string OrganizationCode = "";
        public ExecutionManager execManager;
        public delegate void WriteMessageDel(string Message);
        public WriteMessageDel WriteMessageHandler;
        public delegate void ClearProgressDel();
        public ClearProgressDel ClearProgressHandler;
        public delegate void SetProgressMaxDel(int Value);
        public SetProgressMaxDel SetProgressMaxHandler;
        public delegate void ReportProgressDel(int currentValue, string labelText);
        public ReportProgressDel ReportProgressHandler;
        protected InCubeDatabase db_vms;
        protected InCubeDatabase db_ERP;
        protected InCubeDatabase db_ERP2;
        protected InCubeDatabase db_ERP_GET;
        private static InCubeQuery incubeQuery;
        private InCubeQuery resQuery;
        public IntegrationFilters Filters;
        public bool Initialized = true;

        //Standard ntegration
        BackgroundWorker bgwCheckProgress;
        InCubeDatabase db_res;
        string ExecutionTableName = "Int_ExecutionDetails";
        bool ReadExecutionDetails = false;
        string ExecDetailsReadQry = "";

        #endregion

        #region Update Methods
        public virtual void RunPostActionFunction() { }
        public virtual void GetMasterData() { }
        //Item
        public virtual void UpdateItem() { }
        //Customer
        public virtual void UpdateCustomer() { }
        //Price
        public virtual void UpdatePrice() { }
        //Discount
        public virtual void UpdateDiscount() { }
        //Route
        public virtual void UpdateRoutes() { }
        //Invoice
        public virtual void UpdateInvoice() { }
        public virtual void StoreInvoices() { }
        //History
        public virtual void UpdateKPI() { }
        //STA
        public virtual void UpdateSTA() { }
        //STP
        public virtual void UpdateSTP() { }
        //EDI
        public virtual void UpdateEDI() { }
        //MainWarehouseStock
        public virtual void UpdateMainWHStock() { }
        //Outstanding
        public virtual void OutStanding() { }
        //Stock
        public virtual void UpdateStock() { }
        //Serial Stock
        public virtual void UpdateSerialStock() { }
        //Warehouse
        public virtual void UpdateWarehouse() { }
        public virtual void UpdateMainWarehouse() { }
        //SalesPerson
        public virtual void UpdateSalesPerson() { }
        //InvoiceLimitAndBalance
        public virtual void UpdateBalanceCreditLimit() { }
        public virtual void UpdateCreditInvoiceAmount() { }
        //Organization
        public virtual void UpdateOrganization() { }
        //Geographical Locations
        public virtual void UpdateGeographicalLocation() { }
        //Promotion
        public virtual void UpdatePromotion() { }
        //Orders
        public virtual void UpdateOrders() { }
        //ContractedFOC
        public virtual void UpdateContractedFOC() { }
        //NewCustomer
        public virtual void UpdateNewCustomer() { }
        //Target
        public virtual void UpdateTarget() { }
        //POSM
        public virtual void UpdatePOSM() { }
        //Bank
        public virtual void UpdateBank() { }
        //PackGroup
        public virtual void UpdatePackGroup() { }
        //Areas
        public virtual void UpdateAreas() { }
        //Database Actions
        public virtual void RunDatabaseActions() { }
        //Close
        public virtual void Close() { }

        #endregion

        #region Send Methods

        //Sales
        public virtual void SendInvoices() { }
        //Reciepts
        public virtual void SendReciepts() { }
        //Down payments
        public virtual void SendDownPayments() { }
        //Transfers
        public virtual void SendTransfers() { }
        //Orders
        public virtual void SendOrders() { }
        //SendCreditNoteRequest
        public virtual void SendCreditNoteRequest() { }
        //ATM Deposits
        public virtual void SendATMCollections() { }
        //Returns
        public virtual void SendReturn() { }
        //Order Invoices
        public virtual void SendOrderInvoices() { }
        //NewCustomer
        public virtual void SendNewCustomers() { }
        //StockInterface
        public virtual void StockInterface() { }
        //InvoiceInterface
        public virtual void InvoiceInterface() { }
        //Price
        public virtual void SendPrice() { }
        //Promotion
        public virtual void SendPromotion() { }
        //POSM
        public virtual void SendPOSM() { }

        #endregion
        public virtual void Closeing() { }

        public void WriteMessage(string Message)
        {
            try
            {
                if (WriteMessageHandler != null && !CoreGeneral.Common.IsTesting)
                    WriteMessageHandler(Message);
            }
            catch (Exception)
            {
            }

        }
        public void ClearProgress()
        {
            if (ClearProgressHandler != null)
                ClearProgressHandler();
        }
        public void SetProgressMax(int value)
        {
            if (SetProgressMaxHandler != null)
                SetProgressMaxHandler(value);
        }
        public void ReportProgress(int currentValue, string labelText)
        {
            if (ReportProgressHandler != null)
                ReportProgressHandler(currentValue, labelText);
        }
        public void ReportProgress(int currentValue)
        {
            try
            {
                ReportProgress(currentValue, "");
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public void ReportProgress()
        {
            try
            {
                ReportProgress(-1, "");
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public void ReportProgress(string labelText)
        {
            try
            {
                ReportProgress(-1, labelText);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public IntegrationBase(ExecutionManager ExecMngr)
        {
            if (db_vms == null || !db_vms.Opened)
            {
                db_vms = new InCubeDatabase();
                InCubeErrors err = db_vms.Open("InCube", "IntegrationBase");
            }
            execManager = ExecMngr;
        }
        protected InCubeErrors ExistObject(string TableName, string Field, string WhereCondition, InCubeDatabase _db)
        {
            if (!_db.IsOpened())
            {
                throw new Exception("The Connection State is Closed");
            }

            InCubeErrors err;
            string QueryStr = "Select " + Field + " From " + TableName + " Where " + WhereCondition;
            InCubeQuery Query = new InCubeQuery(_db, QueryStr);
            Query.Execute();
            err = Query.FindFirst();
            Query.Close();
            return err;
        }
        protected InCubeErrors ExistObject(string TableName, string Field, string WhereCondition, InCubeDatabase _db,InCubeTransaction transaction)
        {
            if (!_db.IsOpened())
            {
                throw new Exception("The Connection State is Closed");
            }

            InCubeErrors err;
            string QueryStr = "Select " + Field + " From " + TableName + " Where " + WhereCondition;
            InCubeQuery Query = new InCubeQuery(_db, QueryStr);
            Query.Execute(transaction);
            err = Query.FindFirst();
            Query.Close();
            return err;
        }
        protected string GetFieldValue(string TableName, string Field, InCubeDatabase _db)
        {
            if (!_db.IsOpened())
            {
                throw new Exception("The Connection State is Closed");
            }

            object field = new object();
            string QueryStr = "Select " + Field + " FROM " + TableName;
            InCubeQuery Query = new InCubeQuery(_db, QueryStr);
            Query.ExecuteScalar(ref field);
            Query.Close();
            if (field == null)
            {
                return "";
            }
            return field.ToString().Trim();
        }
        protected string GetFieldValue(string TableName, string Field, string WhereCondition, InCubeDatabase _db, InCubeTransaction Tran)
        {
            if (!_db.IsOpened())
            {
                throw new Exception("The Connection State is Closed");
            }

            object field = "";
            string QueryStr = "Select " + Field + " FROM " + TableName + " Where " + WhereCondition;
            InCubeQuery Query = new InCubeQuery(_db, QueryStr);
            Query.ExcuteScaler(Tran, ref field);
            Query.Close();

            if (field != null)
            {
                return field.ToString().Trim();
            }
            return string.Empty;
        }
        protected string GetFieldValue(string TableName, string Field, string WhereCondition, InCubeDatabase _db )
        {
            if (!_db.IsOpened())
            {
                throw new Exception("The Connection State is Closed");
            }

            object field = "";
            string QueryStr = "Select " + Field + " FROM " + TableName + " Where " + WhereCondition;
            InCubeQuery Query = new InCubeQuery(_db, QueryStr);
            Query.ExecuteScalar( ref field);
            Query.Close();

            if (field != null)
            {
                return field.ToString().Trim();
            }
            return string.Empty;
        }
        protected string GetFieldValue(string TableName, string Field, InCubeDatabase _db, InCubeTransaction Tran)
        {
            if (!_db.IsOpened())
            {
                throw new Exception("The Connection State is Closed");
            }

            object field = new object();
            string QueryStr = "Select " + Field + " FROM " + TableName;
            InCubeQuery Query = new InCubeQuery(_db, QueryStr);
            Query.ExcuteScaler(Tran, ref field);
            Query.Close();

            if (field == null)
            {
                return "";
            }
            return field.ToString().Trim();
        }
        protected bool IsVehicleUploaded(string vehicleID, InCubeDatabase database)
        {
            bool isUploaded = false;

            string Uploaded = GetFieldValue("RouteHistory", "ActualStart", " RouteHistoryID = (SELECT MAX(RouteHistoryID) FROM RouteHistory WHERE VehicleID = " + vehicleID + ") AND Uploaded = 1 ", database);

            if (Uploaded == null)
            {
                isUploaded = true;
            }

            return isUploaded;
        }
        public void GetExecutionResults(List<string> StagingTables, ref int TotalRows, ref int Inserted, ref int Updated, ref int Skipped, InCubeDatabase db_res)
        {
            try
            {
                if (CoreGeneral.Common.CurrentSession.loginType == LoginType.WindowsService)
                    return;

                foreach (string StagingTable in StagingTables)
                {
                    resQuery = new InCubeQuery(db_res, string.Format(@"SELECT COUNT(*) TotalRows, ISNULL(SUM(CAST(ISNULL(Inserted,0) AS INT)),0) Inserted, ISNULL(SUM(CAST(ISNULL(Updated,0) AS INT)),0) Updated, ISNULL(SUM(CAST(ISNULL(Skipped,0) AS INT)),0) Skipped 
FROM 
{0}", StagingTable));
                    DataTable dtResults = new DataTable();
                    if (resQuery.Execute() == InCubeErrors.Success)
                    {
                        dtResults = resQuery.GetDataTable();
                        if (dtResults.Rows.Count > 0)
                        {
                            TotalRows += int.Parse(dtResults.Rows[0]["TotalRows"].ToString());
                            Inserted += int.Parse(dtResults.Rows[0]["Inserted"].ToString());
                            Updated += int.Parse(dtResults.Rows[0]["Updated"].ToString());
                            Skipped += int.Parse(dtResults.Rows[0]["Skipped"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public void GetExecutionResults(string StagingTable, ref int TotalRows, ref int Inserted, ref int Updated, ref int Skipped, InCubeDatabase db_res)
        {
            List<string> StagingTables = new List<string>();
            StagingTables.Add(StagingTable);
            GetExecutionResults(StagingTables, ref TotalRows, ref Inserted, ref Updated, ref Skipped, db_res);
        }
        public void GetExecutionResults(int triggerID, ref int TotalRows, ref int Inserted, ref int Updated, ref int Skipped, InCubeDatabase db_res)
        {
            GetExecutionResults("Int_ExecutionDetails", triggerID, ref TotalRows, ref Inserted, ref Updated, ref Skipped, db_res);
        }
        public void GetExecutionResults(string ExecutionTable, int triggerID, ref int TotalRows, ref int Inserted, ref int Updated, ref int Skipped, string ExecDetailsReadQry, int LastProcessID, ref DataTable dtExecutionDetails, InCubeDatabase db_res)
        {
            try
            {
                if (CoreGeneral.Common.CurrentSession.loginType == LoginType.WindowsService)
                    return;

                GetExecutionResults(ExecutionTable, triggerID, ref TotalRows, ref Inserted, ref Updated, ref Skipped, db_res);
                ExecDetailsReadQry = ExecDetailsReadQry.Replace("@ExecutionTable", ExecutionTable);
                ExecDetailsReadQry = ExecDetailsReadQry.Replace("@TriggerID", triggerID.ToString());
                ExecDetailsReadQry = ExecDetailsReadQry.Replace("@ProcessID", LastProcessID.ToString());

                resQuery = new InCubeQuery(db_res, ExecDetailsReadQry);
                InCubeErrors err = resQuery.Execute();
                if (err == InCubeErrors.Success)
                    dtExecutionDetails = resQuery.GetDataTable();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public void GetExecutionResults(string ExecutionTable, int triggerID, ref int TotalRows, ref int Inserted, ref int Updated, ref int Skipped, InCubeDatabase db_res)
        {
            try
            {
                if (CoreGeneral.Common.CurrentSession.loginType == LoginType.WindowsService)
                    return;

                resQuery = new InCubeQuery(db_res, string.Format(@"SELECT ISNULL(TotalRows,0) TotalRows, ISNULL(SUM(cast(ISNULL(Inserted,0) as int)),0) Inserted, ISNULL(SUM(cast(ISNULL(Updated,0) as int)),0) Updated, ISNULL(SUM(cast(ISNULL(Skipped,0) as int)),0) Skipped 
FROM 
Int_ActionTrigger AT 
LEFT JOIN {0} ED ON ED.TriggerID = AT.ID AND ED.TriggerID = {1}
WHERE AT.ID = {1}
GROUP BY AT.TotalRows", ExecutionTable, triggerID));
                DataTable dtResults = new DataTable();
                resQuery.Execute();
                dtResults = resQuery.GetDataTable();
                if (dtResults.Rows.Count > 0)
                {
                    TotalRows = int.Parse(dtResults.Rows[0]["TotalRows"].ToString());
                    Inserted = int.Parse(dtResults.Rows[0]["Inserted"].ToString());
                    Updated = int.Parse(dtResults.Rows[0]["Updated"].ToString());
                    Skipped = int.Parse(dtResults.Rows[0]["Skipped"].ToString());
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public Result GetLastExecutionResultForEntry(IntegrationField field, List<string> FiltersValues, int CurrentProcessID, long Timeout)
        {
            Result res = Result.NoRowsFound;
            try
            {
                string filters = string.Empty;
                for (int i = 1; i <= FiltersValues.Count; i++)
                {
                    filters += "AND ED.Filter" + i.ToString() + "Value = '" + FiltersValues[i - 1] + "' ";
                }

                string getQuery = string.Format(@"
SELECT TOP 1 ISNULL(ResultID,0) ResultID,DATEDIFF(SS,ED.RunTimeStart,GETDATE()) Elapsed
FROM Int_ExecutionDetails ED
INNER JOIN Int_ActionTrigger AT ON AT.ID = ED.TriggerID
WHERE ED.ID <> {2} AND ED.ResultID <> {3} AND AT.FieldID = {0}
{1}
ORDER BY ED.ID DESC
", field.GetHashCode(), filters, CurrentProcessID, Result.Blocked.GetHashCode());

                resQuery = new InCubeQuery(db_vms, getQuery);
                DataTable dtResult = new DataTable();
                if (resQuery.Execute() == InCubeErrors.Success)
                {
                    dtResult = resQuery.GetDataTable();
                    if (dtResult != null && dtResult.Rows.Count == 1)
                    {
                        int ResultID = int.Parse(dtResult.Rows[0]["ResultID"].ToString());
                        if (ResultID == 0 || ResultID == Result.Blocked.GetHashCode())
                        {
                            long Elapsed = long.Parse(dtResult.Rows[0]["Elapsed"].ToString());
                            if (Elapsed < Timeout)
                                res = Result.Started;
                            else
                                res = (Result)ResultID;
                        }
                        else
                            res = (Result)ResultID;
                    }
                }
            }
            catch (Exception ex)
            {
                res = Result.Invalid;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public virtual Result RunDataWarehouse()
        {
            Result res = Result.UnKnown;
            try
            {
                if (Filters.SpecialFunctionFilter == "")
                    return Result.NoFileRetreived;
                using (DataTransferManager transferManager = new DataTransferManager())
                {
                    DataTable dtTransferTypesDetails = new DataTable();
                    res = transferManager.GetDataTransferTypesDetails(Filters.SpecialFunctionFilter, ref dtTransferTypesDetails);
                    if (res != Result.Success)
                        return res;

                    SetProgressMax(dtTransferTypesDetails.Rows.Count);
                    foreach (DataRow dr in dtTransferTypesDetails.Rows)
                    {
                        Result result = Result.UnKnown;
                        int processID = 0, inserted = 0, updated = 0, total = 0;
                        string message = "";
                        string messageTotals = "";
                        long t_CopyDataToTemp = 0, t_DeleteAndInsert = 0, t_insert = 0, t_update = 0;
                        try
                        {
                            int GroupID = int.Parse(dr["GroupID"].ToString());
                            int ID = int.Parse(dr["ID"].ToString());
                            Dictionary<int, string> filters = new Dictionary<int, string>();
                            filters.Add(1, GroupID.ToString());
                            filters.Add(2, ID.ToString());
                            processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                            string Name = dr["Name"].ToString();
                            ReportProgress(Name);
                            string SelectQuery = dr["SelectQuery"].ToString();
                            TransferMethod Method = (TransferMethod)int.Parse(dr["TransferMethodID"].ToString());

                            int srcDBTypeID = int.Parse(dr["SourceDatabaseTypeID"].ToString());
                            string SrcConnectionString = dr["SourceConnectionString"].ToString();
                            DataBaseType SrcDbType = DataBaseType.SQLServer;
                            if (srcDBTypeID != 0)
                                SrcDbType = (DataBaseType)srcDBTypeID;
                            else
                                SrcConnectionString = db_vms.GetConnection().ConnectionString;

                            int destDBTypeID = int.Parse(dr["DestinationDatabaseTypeID"].ToString());
                            string DestConnectionString = dr["DestinationConnectionString"].ToString();
                            DataBaseType DestDbType = DataBaseType.SQLServer;
                            if (destDBTypeID != 0)
                                DestDbType = (DataBaseType)destDBTypeID;
                            else
                                DestConnectionString = db_vms.GetConnection().ConnectionString;

                            string DestinationTable = dr["DestinationTable"].ToString();
                            string TempTable = "Temp_" + DestinationTable;

                            WriteMessage("\r\nTransferring (" + Name + ") ..");

                            //Check source connection
                            result = transferManager.InitializeSourceConnection(SrcDbType, SrcConnectionString);
                            if (result != Result.Success)
                            {
                                message = "Error opening source connection!!";
                                throw (new Exception());
                            }

                            //Check destination connection
                            result = transferManager.InitializeDestinationConnection(DestDbType, DestConnectionString);
                            if (result != Result.Success)
                            {
                                message = "Error opening destination connection!!";
                                throw (new Exception());
                            }

                            Dictionary<string, string> TableColumns = new Dictionary<string, string>();
                            List<string> PrimaryKeyColumns = new List<string>();
                            DataTable dtSchema = new DataTable();
                            result = transferManager.ReadTableSchemaAtDestDB(DestinationTable, ref dtSchema, ref TableColumns, ref PrimaryKeyColumns);
                            if (result != Result.Success)
                            {
                                message = "Error reading destination table schema!";
                                result = Result.Invalid;
                                throw (new Exception());
                            }

                            if (Method != TransferMethod.DeleteAndInsert && PrimaryKeyColumns.Count == 0)
                            {
                                message = "Destination table doesn't have primary key!";
                                result = Result.Invalid;
                                throw (new Exception());
                            }

                            result = transferManager.CreateTableAtDestination(TempTable, dtSchema);
                            if (result != Result.Success)
                            {
                                message = "Error creating temp table at destination!!";
                                throw (new Exception());
                            }

                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            result = transferManager.CopyDataToTempTable(SelectQuery, TempTable, TableColumns, ref total);
                            sw.Stop();
                            t_CopyDataToTemp = sw.ElapsedMilliseconds;
                            if (result != Result.Success)
                            {
                                message = "Error copying data to temp table at destination!!";
                                throw (new Exception());
                            }

                            if (Method == TransferMethod.DeleteAndInsert)
                            {
                                //truncate table and insert without join
                                sw.Restart();
                                result = transferManager.RefillDataAtDestinationTable(DestinationTable);
                                sw.Stop();
                                t_DeleteAndInsert = sw.ElapsedMilliseconds;
                                if (result != Result.Success)
                                    message = "Error in delete and insert at destination!!";
                                else
                                    inserted = total;
                            }
                            else
                            {
                                if (Method == TransferMethod.InsertAndUpdate || Method == TransferMethod.UpdateOnly)
                                {
                                    //update: inner join and update
                                    sw.Restart();
                                    result = transferManager.UpdateDataAtDestinationTable(DestinationTable, new List<string>(TableColumns.Values), PrimaryKeyColumns, ref updated);
                                    sw.Stop();
                                    t_update = sw.ElapsedMilliseconds;
                                    if (result != Result.Success)
                                        message = "Error in update data at destination!!";
                                }
                                if (result == Result.Success && (Method == TransferMethod.InsertAndUpdate || Method == TransferMethod.InsertOnly))
                                {
                                    //insert: left join and insert where null
                                    sw.Restart();
                                    result = transferManager.InaertDataToDestinationTable(DestinationTable, PrimaryKeyColumns, ref inserted);
                                    sw.Stop();
                                    t_insert = sw.ElapsedMilliseconds;
                                    if (result != Result.Success)
                                        message = "Error in insert data at destination!!";
                                }
                            }
                            if (result == Result.Success)
                            {
                                messageTotals = "|| Total rows: " + total.ToString();
                                message = "Elapsed time (ms): Copy Data To Temp Table: " + t_CopyDataToTemp.ToString();
                                if (Method == TransferMethod.DeleteAndInsert)
                                {
                                    message += ", Delete and Insert: " + t_DeleteAndInsert.ToString();
                                }
                                if (Method == TransferMethod.InsertAndUpdate || Method == TransferMethod.UpdateOnly)
                                {
                                    messageTotals += ", Updated: " + updated.ToString();
                                    message += ", Update: " + t_update.ToString();
                                }
                                if (Method == TransferMethod.InsertAndUpdate || Method == TransferMethod.InsertOnly)
                                {
                                    messageTotals += ", Inserted: " + inserted.ToString();
                                    message += ", Insert: " + t_insert.ToString();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            result = Result.Failure;
                            if (message == string.Empty)
                            {
                                message = "Unhandled exception!!";
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
                            }
                        }
                        finally
                        {
                            WriteMessage(message + messageTotals);
                            if (processID != 0)
                                execManager.LogIntegrationEnding(processID, DateTime.Now, result, inserted, updated, 0, total.ToString(), message);
                            transferManager.CloseConnections();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public virtual Result RunDataTransfer()
        {
            Result res = Result.UnKnown;
            try
            {
                if (Filters.SpecialFunctionFilter == "")
                    return Result.NoFileRetreived;

                using (DataTransferManager transferManager = new DataTransferManager())
                {
                    DataTable dtTransferTypesDetails = new DataTable();
                    res = transferManager.GetDataTransferTypesDetails(Filters.SpecialFunctionFilter, ref dtTransferTypesDetails);
                    if (res != Result.Success)
                        return res;

                    foreach (DataRow dr in dtTransferTypesDetails.Rows)
                    {
                        DataTable dtDestTable = null, dtSourceData = null;
                        Result result = Result.UnKnown;
                        int processID = 0, inserted = 0, updated = 0, skipped = 0, total = 0;
                        string message = "";

                        try
                        {
                            int ID = int.Parse(dr["ID"].ToString());

                            Dictionary<int, string> filters = new Dictionary<int, string>();
                            filters.Add(BuiltInFilters.DataTransferCheckList.GetHashCode(), ID.ToString());
                            processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                            string Name = dr["Name"].ToString();
                            string SelectQuery = dr["SelectQuery"].ToString();
                            TransferMethod Method = (TransferMethod)int.Parse(dr["TransferMethodID"].ToString());

                            int srcDBTypeID = int.Parse(dr["SourceDatabaseTypeID"].ToString());
                            string SrcConnectionString = dr["SourceConnectionString"].ToString();
                            DataBaseType SrcDbType = DataBaseType.SQLServer;
                            if (srcDBTypeID != 0)
                                SrcDbType = (DataBaseType)srcDBTypeID;
                            else
                                SrcConnectionString = db_vms.GetConnection().ConnectionString;

                            int destDBTypeID = int.Parse(dr["DestinationDatabaseTypeID"].ToString());
                            string DestConnectionString = dr["DestinationConnectionString"].ToString();
                            DataBaseType DestDbType = DataBaseType.SQLServer;
                            if (destDBTypeID != 0)
                                DestDbType = (DataBaseType)destDBTypeID;
                            else
                                DestConnectionString = db_vms.GetConnection().ConnectionString;

                            string DestinationTable = dr["DestinationTable"].ToString();
                            bool HasIdentityColumn = bool.Parse(dr["HasIdentityColumn"].ToString());
                            string PrimaryKeyColumnsStr = dr["PrimaryKeyColumns"].ToString();
                            string ConstantValuesStr = dr["ConstantValues"].ToString();

                            WriteMessage("\r\nTransferring (" + Name + ") ..");

                            //Check source connection
                            Result resConn = Result.UnKnown;
                            resConn = transferManager.InitializeSourceConnection(SrcDbType, SrcConnectionString);
                            if (resConn != Result.Success)
                            {
                                message = "Error opening source connection!!";
                                result = Result.Invalid;
                                throw (new Exception());
                            }

                            //Check destination connection
                            resConn = Result.UnKnown;
                            resConn = transferManager.InitializeDestinationConnection(DestDbType, DestConnectionString);
                            if (resConn != Result.Success)
                            {
                                message = "Error opening destination connection!!";
                                result = Result.Invalid;
                                throw (new Exception());
                            }

                            //Check destination table schema
                            string selectTopRowQry = "";
                            Result resSchema = Result.UnKnown;
                            if (DestDbType == DataBaseType.Oracle)
                                selectTopRowQry = "SELECT * FROM " + '"' + DestinationTable.ToUpper() + '"' + " WHERE ROWNUM = 1";
                            else
                                selectTopRowQry = "SELECT TOP 1 * FROM [" + DestinationTable + "]";
                            resSchema = transferManager.GetDestinationDataTable(selectTopRowQry, ref dtDestTable);

                            if (resSchema != Result.Success && resSchema != Result.NoRowsFound)
                            {
                                message = "Error reading destination table schema!!";
                                result = Result.Invalid;
                                throw (new Exception());
                            }

                            //Run select query
                            Result resData = transferManager.GetSourceDataTable(SelectQuery, ref dtSourceData);
                            if (resData == Result.NoRowsFound)
                            {
                                message = "No data found!!";
                                result = Result.NoRowsFound;
                                throw (new Exception());
                            }
                            else if (resData != Result.Success)
                            {
                                message = "Error executing select query!!";
                                result = Result.ErrorExecutingQuery;
                            }

                            //Generate primary key columns Dictionary
                            Dictionary<string, ColumnType> PrimaryKeyColumns = new Dictionary<string, ColumnType>();
                            if (PrimaryKeyColumnsStr.Trim() != string.Empty)
                            {
                                string[] primaryKeys = PrimaryKeyColumnsStr.Split(new char[] { ',' });
                                foreach (string colName in primaryKeys)
                                {
                                    if (!dtDestTable.Columns.Contains(colName))
                                    {
                                        message = "Primary column (" + colName + ") is not avaialble in destination table!!";
                                        result = Result.Invalid;
                                        throw (new Exception());
                                    }
                                    string dbColType = dtDestTable.Columns[colName].DataType.ToString();
                                    ColumnType colType = transferManager.GetColumnType(dbColType);
                                    PrimaryKeyColumns.Add(colName.ToLower(), colType);
                                }
                            }
                            
                            //Generate constant values Dictionary
                            Dictionary<string, DBColumn> ConstantValues = null;
                            transferManager.FillConstantsDictionary(ConstantValuesStr, ref ConstantValues);
                            
                            //Generate destination columns Dictionary
                            Dictionary<string, ColumnType> DestinationColumns = new Dictionary<string, ColumnType>();
                            transferManager.PrepareDestinationColumns(dtSourceData.Columns, dtDestTable.Columns, ref DestinationColumns);
                            
                            //Trasfer
                            ClearProgress();
                            SetProgressMax(dtSourceData.Rows.Count);

                            //Delete from destination
                            if (Method == TransferMethod.DeleteAndInsert)
                            {
                                Result resDelete = Result.UnKnown;
                                string deleteStatement = "";
                                if (DestDbType == DataBaseType.Oracle)
                                    deleteStatement = string.Format("DELETE FROM {0}{1}{0}", '"', DestinationTable.ToUpper());
                                else
                                    deleteStatement = "DELETE FROM [" + DestinationTable + "]";

                                resDelete = transferManager.ExecuteDestinationCommand(deleteStatement);
                                if (resDelete != Result.Success)
                                {
                                    message = "Error deleting from destination!!";
                                    result = Result.ErrorExecutingQuery;
                                    throw (new Exception());
                                }
                            }

                            //Insert and update 
                            for (int i = 0; i < dtSourceData.Rows.Count; i++)
                            {
                                ReportProgress();
                                string QueryStr = "";

                                try
                                {
                                    //Check existance
                                    int exist = 0;
                                    Result resExec = Result.UnKnown;
                                    if (Method != TransferMethod.DeleteAndInsert)
                                    {
                                        resExec = transferManager.GenerateCheckExistanceStatement(DestDbType, dtSourceData.Rows[i], DestinationTable, PrimaryKeyColumns, ref QueryStr);
                                        if (resExec != Result.Success)
                                            throw new Exception();

                                        object Exist = 0;
                                        resExec = transferManager.ExecuteDestinationScalar(QueryStr, ref Exist);
                                        if (resExec != Result.Success)
                                            throw new Exception();

                                        exist = Convert.ToInt16(Exist);

                                        if (exist > 1)
                                        {
                                            throw new Exception("Record exists in destination " + exist + " times, modify primary key\r\nCheck existance query:\r\n" + QueryStr);
                                        }
                                    }

                                    QueryStr = "";
                                    if (exist == 0 && (Method == TransferMethod.InsertAndUpdate || Method == TransferMethod.InsertOnly || Method == TransferMethod.DeleteAndInsert))
                                    {
                                        resExec = transferManager.GenerateInsertStatement(DestDbType, dtSourceData.Rows[i], DestinationTable, HasIdentityColumn, DestinationColumns, ConstantValues, ref QueryStr);
                                    }
                                    if (exist == 1 && (Method == TransferMethod.InsertAndUpdate || Method == TransferMethod.UpdateOnly))
                                    {
                                        resExec = transferManager.GenerateUpdateStatement(DestDbType, dtSourceData.Rows[i], DestinationTable, DestinationColumns, PrimaryKeyColumns, ConstantValues, ref QueryStr);
                                    }
                                    if (resExec != Result.Success)
                                        throw new Exception();

                                    if (QueryStr.Equals(string.Empty))
                                    {
                                        skipped++;
                                    }
                                    else
                                    {
                                        resExec = transferManager.ExecuteDestinationCommand(QueryStr);
                                        if (resExec == Result.Success)
                                        {
                                            if (exist == 0)
                                                inserted++;
                                            else
                                                updated++;
                                        }
                                        else
                                        {
                                            skipped++;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    skipped++;
                                    if (ex.Message != "")
                                    {
                                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
                                    }
                                }
                            }

                            message = "Inserted = " + inserted + ", Updated = " + updated + ", Skipped = " + skipped;
                            result = Result.Success;
                        }
                        catch (Exception ex)
                        {
                            if (message == string.Empty)
                            {
                                message = "Unhandled exception!!";
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
                            }
                        }
                        finally
                        {
                            WriteMessage(message);
                            if (processID != 0)
                                execManager.LogIntegrationEnding(processID, DateTime.Now, result, inserted, updated, skipped, total.ToString(), message);
                            transferManager.CloseConnections();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        private bool IsFileInUse(string FilePath)
        {
            bool InUse = true;
            FileInfo fi = new FileInfo(FilePath);
            FileStream stream = null;

            try
            {
                stream = fi.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                stream.Close();
                InUse = false;
            }
            catch (IOException)
            {
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            return InUse;
        }
        public virtual Result RunDatabaseBackup()
        {
            try
            {
                WriteMessage("\r\nRunning database backup jobs .. ");
                if (Filters.SpecialFunctionFilter == "")
                {
                    WriteMessage("Zero jobs selected !!");
                    return Result.NoFileRetreived;
                }

                incubeQuery = new InCubeQuery(db_vms, string.Format(@"
SELECT JobID, JobName, DatabaseName, BackupPath
FROM Int_DatabaseBackupJobs
WHERE IsDeleted = 0 AND JobID IN ({0})
", Filters.SpecialFunctionFilter));
                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    WriteMessage("Reading jobs details failed !!");
                    return Result.Invalid;
                }

                DataTable dtJobs = new DataTable();
                dtJobs = incubeQuery.GetDataTable();
                if (dtJobs.Rows.Count == 0)
                {
                    WriteMessage("No details found for selected jobs !!");
                    return Result.NoRowsFound;
                }
                WriteMessage(dtJobs.Rows.Count + " jobs found ..");

                foreach (DataRow dr in dtJobs.Rows)
                {
                    int processID = 0;
                    long BackupSize = 0, ZipSize = 0;
                    Result res = Result.UnKnown;
                    string message = "";
                    try
                    {
                        int JobID = Convert.ToInt16(dr["JobID"]);
                        string JobName = dr["JobName"].ToString();
                        string DatabaseName = dr["DatabaseName"].ToString();
                        string BackupPath = dr["BackupPath"].ToString();
                        if (!BackupPath.EndsWith("\\"))
                            BackupPath += "\\";

                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, JobID.ToString());
                        processID = execManager.LogIntegrationBegining(TriggerID, OrganizationID, filters);

                        WriteMessage(string.Format("\r\n\r\nRunning job [{0}]: {1} .. ", JobID, JobName));

                        string fileName = DatabaseName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        string tempPath = CoreGeneral.Common.StartupPath + "\\TempBackup\\";
                        DirectoryInfo dir = new DirectoryInfo(tempPath);
                        if (!dir.Exists)
                            dir.Create();
                        foreach (FileInfo file in dir.GetFiles())
                        {
                            file.Delete();
                        }
                        if (!Directory.Exists(BackupPath))
                            Directory.CreateDirectory(BackupPath);
                        string bakFilePath = tempPath + fileName + ".bak";
                        string ZipDestination = BackupPath + fileName + ".zip";
                        incubeQuery = new InCubeQuery(string.Format("BACKUP DATABASE [{0}] TO DISK = '{1}'", DatabaseName, bakFilePath), db_vms);
                        if (incubeQuery.ExecuteNonQuery() == InCubeErrors.Success)
                        {
                            while (!File.Exists(bakFilePath))
                            {
                                System.Threading.Thread.Sleep(1000);
                            }
                            while (IsFileInUse(bakFilePath))
                            {
                                System.Threading.Thread.Sleep(1000);
                            }
                            FileInfo bakInfo = new FileInfo(bakFilePath);
                            BackupSize = bakInfo.Length;
                            execManager.Compress(tempPath, ZipDestination, 5, false, true);
                            FileInfo zipInfo = new FileInfo(ZipDestination);
                            ZipSize = zipInfo.Length;
                        }
                        else
                        {
                            message = "Backup script failed";
                            res = Result.Failure;
                            continue;
                        }

                        message = "Database backup size: " + Math.Round((decimal)BackupSize / (1024*1024), 2).ToString() + "MB\r\nCompressed file saved to: " + ZipDestination + "\r\nZip file size: " + Math.Round((decimal)ZipSize / (1024 * 1024), 2).ToString() + "MB";
                        res = Result.Success;
                    }
                    catch (Exception ex)
                    {
                        res = Result.Failure;
                        message = ex.Message;
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                    }
                    finally
                    {
                        WriteMessage(message);
                        execManager.LogIntegrationEnding(processID, res, (int)BackupSize, (int)ZipSize, 0, message);
                    }
                }
                return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
        }
        public virtual Result RunFilesManagementJobs()
        {
            try
            {
                WriteMessage("\r\nRunning files management jobs .. ");
                if (Filters.SpecialFunctionFilter == "")
                {
                    WriteMessage("Zero jobs selected !!");
                    return Result.NoFileRetreived;
                }

                incubeQuery = new InCubeQuery(db_vms, string.Format(@"
SELECT JobID,JobName,JobType,SourceFolder,ISNULL(FileExtension,'') FileExtension, ISNULL(ModifyAge, 0) ModifyAge
, ISNULL(AgeTimeUnit, 1) AgeTimeUnit, ISNULL(DestinationFolder, '') DestinationFolder, KeepDirectoryStructure, ComparisonOperator
FROM Int_FilesManagementJobs WHERE IsDeleted = 0 AND JobID IN ({0})
", Filters.SpecialFunctionFilter));
                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    WriteMessage("Reading jobs details failed !!");
                    return Result.Invalid;
                }

                DataTable dtJobs = new DataTable();
                dtJobs = incubeQuery.GetDataTable();
                if (dtJobs.Rows.Count == 0)
                {
                    WriteMessage("No details found for selected jobs !!");
                    return Result.NoRowsFound;
                }
                WriteMessage(dtJobs.Rows.Count + " jobs found ..");

                foreach (DataRow dr in dtJobs.Rows)
                {
                    int processID = 0, Found = 0, Matching = 0, JobPerformed = 0;
                    Result res = Result.UnKnown;
                    string message = "";
                    try
                    {
                        int JobID = Convert.ToInt16(dr["JobID"]);
                        string JobName = dr["JobName"].ToString();
                        FileJobType JobType = (FileJobType)Convert.ToInt16(dr["JobType"]);
                        ComparisonOperator compOp = (ComparisonOperator)Convert.ToInt16(dr["ComparisonOperator"]);
                        string SourceFolder = dr["SourceFolder"].ToString();
                        if (SourceFolder.StartsWith("[Integration Directory]"))
                            SourceFolder = SourceFolder.Replace("[Integration Directory]", CoreGeneral.Common.StartupPath);
                        if (!SourceFolder.EndsWith("\\"))
                            SourceFolder += "\\";

                        string FileExtension = dr["FileExtension"].ToString();
                        int ModifyAge = Convert.ToInt16(dr["ModifyAge"]);
                        AgeTimeUnit TimeUnit = (AgeTimeUnit)Convert.ToInt16(dr["AgeTimeUnit"]);
                        string DestinationFolder = "";
                        if (JobType != FileJobType.Delete)
                        {
                            DestinationFolder = dr["DestinationFolder"].ToString();
                            if (!DestinationFolder.EndsWith("\\"))
                                DestinationFolder += "\\";
                        }
                        bool KeepDirectoryStructure = Convert.ToBoolean(dr["KeepDirectoryStructure"]); ;

                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, JobID.ToString());
                        processID = execManager.LogIntegrationBegining(TriggerID, OrganizationID, filters);

                        WriteMessage(string.Format("\r\n\r\nRunning job [{0}]: {1} .. ", JobID, JobName));
                        if (!Directory.Exists(SourceFolder))
                        {
                            message = "Invalid source folder";
                            res = Result.Invalid;
                            continue;
                        }

                        string filesFilter = "*";
                        if (FileExtension != "")
                            filesFilter = "*." + FileExtension;
                        string[] files = Directory.GetFiles(SourceFolder, filesFilter, SearchOption.AllDirectories);
                        Found = files.Length;
                        WriteMessage(Found + " files found in specified folder and extension .. ");
                        ClearProgress();
                        SetProgressMax(Found);

                        DateTime refDate = DateTime.MaxValue;
                        if (ModifyAge != 0)
                        {
                            refDate = DateTime.Now;
                            switch (TimeUnit)
                            {
                                case AgeTimeUnit.Second:
                                    refDate = refDate.AddSeconds(-1 * ModifyAge);
                                    break;
                                case AgeTimeUnit.Minute:
                                    refDate = refDate.AddMinutes(-1 * ModifyAge);
                                    break;
                                case AgeTimeUnit.Hour:
                                    refDate = refDate.AddHours(-1 * ModifyAge);
                                    break;
                                case AgeTimeUnit.Day:
                                    refDate = refDate.AddDays(-1 * ModifyAge);
                                    break;
                                case AgeTimeUnit.Month:
                                    refDate = refDate.AddMonths(-1 * ModifyAge);
                                    break;
                                case AgeTimeUnit.Year:
                                    refDate = refDate.AddYears(-1 * ModifyAge);
                                    break;
                            }
                        }
                        
                        foreach (string filename in files)
                        {
                            try
                            {
                                ReportProgress(Path.GetFileName(filename));
                                FileInfo fi = new FileInfo(filename);
                                string Destination = "";
                                if (KeepDirectoryStructure)
                                {
                                    Destination = DestinationFolder + filename.Remove(0, SourceFolder.Length);
                                }
                                else
                                {
                                    Destination = DestinationFolder + fi.Name;
                                }

                                bool performJob = true;
                                if (refDate != DateTime.MaxValue)
                                {
                                    if (compOp == ComparisonOperator.GreaterThan && fi.LastWriteTime > refDate)
                                    {
                                        performJob = false;
                                    }
                                    else if (compOp == ComparisonOperator.LessThan && fi.LastWriteTime < refDate)
                                    {
                                        performJob = false;
                                    }
                                }
                                if (performJob)
                                {
                                    Matching++;
                                    switch (JobType)
                                    {
                                        case FileJobType.Delete:
                                            File.Delete(filename);
                                            JobPerformed++;
                                            break;
                                        case FileJobType.Copy:
                                        case FileJobType.Move:
                                            string path = Path.GetDirectoryName(Destination);
                                            if (!Directory.Exists(path))
                                                Directory.CreateDirectory(path);
                                            File.Copy(filename, Destination, true);
                                            if (JobType == FileJobType.Move)
                                                File.Delete(filename);
                                            JobPerformed++;
                                            break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                WriteMessage(string.Format("\r\nFailure in performing job on [{0}]: {1}", filename, ex.Message));
                            }
                        }
                        res = Result.Success;
                    }
                    catch (Exception ex)
                    {
                        res = Result.Failure;
                        message = ex.Message;
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                    }
                    finally
                    {
                        if (res == Result.Success)
                        {
                            message = "\r\nJob Completed:\r\nFiles Found: " + Found + "\r\nMatching Search Critira: " + Matching + "\r\nJob Performed: " + JobPerformed + "\r\nFailed: " + (Matching - JobPerformed);
                        }
                        WriteMessage(message);
                        execManager.LogIntegrationEnding(processID, res, Found, Matching, JobPerformed, message);
                    }
                }
                return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
        }
        public Result SendMail(Procedure proc)
        {
            Result res = Result.UnKnown;
            try
            {
                if (ExecuteStoredProcedure(proc) == Result.Success)
                {
                    using (MailManager mailManager = new MailManager())
                    {
                        res = mailManager.SendMails(proc.MailTemplateID, TriggerID);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                res = Result.Failure;
            }
            return res;
        }
        public Result SendSMS(Procedure proc)
        {
            try
            {
                DataTable dtContents = new DataTable();
                incubeQuery = new InCubeQuery(db_vms, string.Format("SELECT * FROM dbo.GetSMSContents({0})", TriggerID));
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtContents = incubeQuery.GetDataTable();
                    if (dtContents != null && dtContents.Rows.Count > 0)
                    {
                        string smsURL = "", userName = "", password = "", sender = "";
                        foreach (Parameter param in proc.Parameters.Values)
                        {
                            if (param.ParameterName == "URL")
                                smsURL = param.ParameterValue.ToString();
                            else if (param.ParameterName == "UserName")
                                userName = param.ParameterValue.ToString();
                            else if (param.ParameterName == "Password")
                                password = param.ParameterValue.ToString();
                            else if (param.ParameterName == "Sender")
                                sender = param.ParameterValue.ToString();
                        }
                        for (int i = 0; i < dtContents.Rows.Count; i++)
                        {
                            if (CoreGeneral.Common.GeneralConfigurations.SiteSymbol.ToLower() == "delmon")
                            {
                                string url = smsURL + "encoding=url" + "&username=" + userName + "&password=" + password
                                    + "&SenderID=" + sender + "&messagedata=" + dtContents.Rows[i]["Contents"].ToString()
                                    + "&receiver=" + dtContents.Rows[i]["Mobile"].ToString();
                                int LogID = execManager.LogSMSRequestStart(TriggerID, proc.ProcedureName, smsURL, userName, password, sender
                                    , dtContents.Rows[i]["Mobile"].ToString(), dtContents.Rows[i]["Contents"].ToString(), url);
                                string response = CallURL(url);
                                execManager.LogSMSRequestEnd(LogID, response);
                            }
                        }
                    }
                }
                return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
        }
        private string CallURL(string URL)
        {
            string response = "";
            try
            {
                using (WebClient client = new WebClient())
                {
                    response = client.DownloadString(URL);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return response;
        }
        internal static DateTime GetIntegrationModificationDateNew(InCubeDatabase db)
        {
            DateTime ret = new DateTime(1900, 1, 1);
            object d = null;
            InCubeQuery MaxModificationDate = new InCubeQuery(db, "Select KeyValue FROM Configuration Where KeyName = 'LastIntegrationUpdate' And EmployeeID = -1");
            InCubeErrors err = MaxModificationDate.Execute();
            err = MaxModificationDate.FindFirst();
            if (err == InCubeErrors.Success)
                err = MaxModificationDate.GetField(0, ref d);
            if (d != null) ret = DateTime.Parse(d.ToString());
            return ret;
        }

        //Standard Integraton
        public void ExecuteFunction(ActionType actionType, IntegrationField field, List<Procedure> Procs)
        {
            try
            {
                if (CoreGeneral.Common.CurrentSession.loginType != LoginType.WindowsService)
                {
                    if (db_res == null || !db_res.Opened)
                    {
                        db_res = new InCubeDatabase();
                        db_res.Open("InCube", "IntegrationResult");
                    }
                    bgwCheckProgress = new BackgroundWorker();
                    bgwCheckProgress.DoWork += new DoWorkEventHandler(bgw_CheckProgress);
                    bgwCheckProgress.WorkerSupportsCancellation = true;
                    bgwCheckProgress.RunWorkerAsync();
                }

                int counter = 0;
                foreach (Procedure proc in Procs)
                {
                    try
                    {
                        if (CoreGeneral.Common.CurrentSession.loginType != LoginType.WindowsService)
                        {
                            ExecutionTableName = proc.ExecutionTableName;
                            ReadExecutionDetails = proc.ReadExecutionDetails;
                            ExecDetailsReadQry = proc.ExecDetailsReadQry;
                            string message = "";
                            if (counter++ == 0)
                            {
                                if (proc.ProcedureType == ProcType.SMS)
                                    message += " Sending SMS ..";
                                else if (proc.ProcedureType == ProcType.Mail)
                                    message += " Sending Mail ..";
                                else if (actionType == ActionType.Update)
                                    message = "\r\nUpdating " + CoreGeneral.Common.userPrivileges.UpdateFieldsAccess[field].Description + " ..";
                                else if (actionType == ActionType.Send)
                                    message = "\r\nSending " + CoreGeneral.Common.userPrivileges.SendFieldsAccess[field].Description + " ..";
                            }
                            if (proc.ProcedureType == ProcType.SQLProcedure || proc.ProcedureType == ProcType.OracleProcedure)
                                message += "\r\nExecuting procedure (" + proc.ProcedureName + ")";
                            WriteMessage(message);
                        }
                        if (proc.ProcedureType == ProcType.SQLProcedure || proc.ProcedureType == ProcType.OracleProcedure)
                        {
                            if (ExecuteStoredProcedure(proc) != Result.Success)
                                break;
                        }
                        else if (proc.ProcedureType == ProcType.SMS)
                        {
                            SendSMS(proc);
                        }
                        else if (proc.ProcedureType == ProcType.Mail)
                        {
                            SendMail(proc);
                        }
                        else if (proc.ProcedureType == ProcType.ExcelExport)
                        {
                            ExportToExcel(proc);
                        }
                        else if (proc.ProcedureType == ProcType.DataTransfer)
                        {
                            Filters.SetValue(BuiltInFilters.DataTransferCheckList, proc.DataTransferGroupID);
                            RunDataTransfer();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                TriggerID = -1;
                System.Threading.Thread.Sleep(550);

                int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
                DataTable dtExecutionDetails = new DataTable();
                if (ReadExecutionDetails)
                {
                    GetExecutionResults(ExecutionTableName, OrgTriggerID, ref TotalRows, ref Inserted, ref Updated, ref Skipped, ExecDetailsReadQry, LastProcessID, ref dtExecutionDetails, db_res);
                }
                else
                {
                    GetExecutionResults(ExecutionTableName, OrgTriggerID, ref TotalRows, ref Inserted, ref Updated, ref Skipped, db_res);
                }

                if (dtExecutionDetails != null && dtExecutionDetails.Rows.Count > 0)
                {
                    foreach (DataRow dr in dtExecutionDetails.Rows)
                    {
                        if (dtExecutionDetails.Columns.Contains("ID"))
                            LastProcessID = Convert.ToInt32(dr["ID"]);
                        if (dtExecutionDetails.Columns.Contains("Message"))
                            WriteMessage("\r\n" + dr["Message"].ToString());
                    }
                }
                WriteMessage("\r\nTotal rows found: " + TotalRows + ", Inserted: " + Inserted + ", Updated: " + Updated + ", Skipped: " + Skipped);

                ExecutionTableName = "Int_ExecutionDetails";
            }
        }
        public Result ExportToExcel(Procedure Proc)
        {
            Result res = Result.UnKnown;
            try
            {
                //FieldID
                DataTable dtExcelContents = new DataTable();
                res = ExecuteStoredProcedureWithTableOutput(Proc, ref dtExcelContents);
                if (res == Result.Success && dtExcelContents != null && dtExcelContents.Rows.Count > 0)
                {
                    DataSet ds = new DataSet("ds");
                    ds.Tables.Add(dtExcelContents);
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(CoreGeneral.Common.StartupPath + "\\DataSources.xml");
                    string excelFileName = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'ExcelExportPath']/Data").InnerText;
                    excelFileName = excelFileName.TrimEnd(new char[] { (char)92 }) + "\\";
                    string field = ((IntegrationField)FieldID).ToString();
                    string MasterName = field.Remove(field.Length - 2, 2);
                    excelFileName += MasterName + "_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".xlsx";
                    ExcelManager.CreateExcelDocument(ds, excelFileName);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                res = Result.Failure;
            }
            return res;
        }
        public Result ExecuteStoredProcedure(Procedure proc)
        {
            DataTable dt = null;
            if (proc.ProcedureType == ProcType.OracleProcedure)
                return ExecuteOracleProcedure(proc, false, ref dt);
            else
                return ExecuteSQLProcedure(proc, false, ref dt);
        }
        public Result ExecuteStoredProcedureWithTableOutput(Procedure proc, ref DataTable dtOutput)
        {
            if (proc.ProcedureType == ProcType.OracleProcedure)
                return ExecuteOracleProcedure(proc, true, ref dtOutput);
            else
                return ExecuteSQLProcedure(proc, true, ref dtOutput);
        }
        
        private Result ExecuteOracleProcedure(Procedure proc, bool GetOutputTable, ref DataTable dtOutput)
        {
            Result res = Result.UnKnown;
            OracleConnection OraConn = null;
            OracleCommand OraCMD = null;
            OracleDataReader OraReader = null;
            try
            {
                DataTransferManager TRFR_MNGR = new DataTransferManager(true);
                DataBaseType dbType = DataBaseType.Oracle;
                string connStr = "";
                res = TRFR_MNGR.GetStoredConnectionDetails(proc.ConnectionID, ref dbType, ref connStr);
                if (dbType != DataBaseType.Oracle)
                    return Result.Invalid;
                OraConn = new OracleConnection(connStr);
                OraConn.Open();
                OraCMD = new OracleCommand(proc.ProcedureName, OraConn);
                OracleParameter OraParam;
                foreach (Parameter Param in proc.Parameters.Values)
                {
                    OraParam = new OracleParameter();
                    switch (Param.ParameterType)
                    {
                        case ParamType.Integer:
                            OraParam = new OracleParameter(Param.ParameterName, SqlDbType.Int);
                            break;
                        case ParamType.Nvarchar:
                            OraParam = new OracleParameter(Param.ParameterName, SqlDbType.NVarChar);
                            break;
                        case ParamType.DateTime:
                            OraParam = new OracleParameter(Param.ParameterName, SqlDbType.DateTime);
                            break;
                        case ParamType.BIT:
                            OraParam = new OracleParameter(Param.ParameterName, SqlDbType.Bit);
                            break;
                        case ParamType.Decimal:
                            OraParam = new OracleParameter(Param.ParameterName, SqlDbType.Decimal);
                            break;
                    }
                    if (Param.Direction == ParamDirection.Input)
                    {
                        switch (Param.ParameterValue.ToString())
                        {
                            case "@TriggerID":
                                OraParam.Value = TriggerID;
                                break;
                            case "@UserID":
                                OraParam.Value = CoreGeneral.Common.CurrentSession.EmployeeID;
                                break;
                            case "@FromDate":
                                OraParam.Value = Filters.FromDate;
                                break;
                            case "@ToDate":
                                OraParam.Value = Filters.ToDate;
                                break;
                            case "@StockDate":
                                OraParam.Value = Filters.StockDate;
                                break;
                            case "@EmployeeID":
                                OraParam.Value = Filters.EmployeeID;
                                break;
                            case "@WarehouseID":
                                OraParam.Value = Filters.WarehouseID;
                                break;
                            case "@InboundStagingDB":
                                OraParam.Value = CoreGeneral.Common.GeneralConfigurations.InboundStagingDB;
                                break;
                            case "@OutboundStagingDB":
                                OraParam.Value = CoreGeneral.Common.GeneralConfigurations.OutboundStagingDB;
                                break;
                            case "@OrganizationID":
                                OraParam.Value = Filters.OrganizationID;
                                break;
                            case "@TextSearch":
                                OraParam.Value = Filters.TextSearch;
                                break;
                            default:
                                OraParam.Value = Param.ParameterValue;
                                break;
                        }
                    }
                    OraParam.Direction = (ParameterDirection)Param.Direction.GetHashCode();
                    OraCMD.Parameters.Add(OraParam);
                }
                OraCMD.CommandTimeout = 3600000;
                OraCMD.CommandType = CommandType.StoredProcedure;
                OraCMD.InitialLONGFetchSize = 10000;

                if (!GetOutputTable)
                {
                    OraCMD.ExecuteNonQuery();
                }
                else
                {
                    OraReader = OraCMD.ExecuteReader();
                    if (OraReader != null && !OraReader.IsClosed)
                    {
                        dtOutput = new DataTable();
                        dtOutput.Load(OraReader);
                    }
                }

                foreach (Parameter p in proc.Parameters.Values)
                {
                    if (p.Direction == ParamDirection.Output)
                    {
                        p.ParameterValue = OraCMD.Parameters[p.ParameterName].Value.ToString();
                    }
                }

                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                if (OraConn != null && OraConn.State == ConnectionState.Open)
                {
                    OraConn.Close();
                }
                if (OraConn != null)
                    OraConn.Dispose();
                if (OraCMD != null)
                    OraCMD.Dispose();
                if (OraReader != null)
                    OraReader.Dispose();
            }
            return res;
        }
        public Result ExecuteSQLProcedure(Procedure proc, bool GetOutputTable, ref DataTable dtOutput)
        {
            Result res = Result.UnKnown;
            SqlCommand cmd;
            SqlDataReader sqlReader;
            try
            {
                cmd = new SqlCommand(proc.ProcedureName, db_vms.GetConnection());
                SqlParameter SQLparam;
                foreach (Parameter Param in proc.Parameters.Values)
                {
                    SQLparam = new SqlParameter();
                    switch (Param.ParameterType)
                    {
                        case ParamType.Integer:
                            SQLparam = new SqlParameter(Param.ParameterName, SqlDbType.Int);
                            break;
                        case ParamType.Nvarchar:
                            SQLparam = new SqlParameter(Param.ParameterName, SqlDbType.NVarChar);
                            break;
                        case ParamType.DateTime:
                            SQLparam = new SqlParameter(Param.ParameterName, SqlDbType.DateTime);
                            break;
                        case ParamType.BIT:
                            SQLparam = new SqlParameter(Param.ParameterName, SqlDbType.Bit);
                            break;
                        case ParamType.Decimal:
                            SQLparam = new SqlParameter(Param.ParameterName, SqlDbType.Decimal);
                            break;
                    }
                    if (Param.Direction == ParamDirection.Input)
                    {
                        switch (Param.ParameterValue.ToString())
                        {
                            case "@TriggerID":
                                SQLparam.Value = TriggerID;
                                break;
                            case "@UserID":
                                SQLparam.Value = CoreGeneral.Common.CurrentSession.EmployeeID;
                                break;
                            case "@FromDate":
                                SQLparam.Value = Filters.FromDate;
                                break;
                            case "@ToDate":
                                SQLparam.Value = Filters.ToDate;
                                break;
                            case "@StockDate":
                                SQLparam.Value = Filters.StockDate;
                                break;
                            case "@EmployeeID":
                                SQLparam.Value = Filters.EmployeeID;
                                break;
                            case "@WarehouseID":
                                SQLparam.Value = Filters.WarehouseID;
                                break;
                            case "@InboundStagingDB":
                                SQLparam.Value = CoreGeneral.Common.GeneralConfigurations.InboundStagingDB;
                                break;
                            case "@OutboundStagingDB":
                                SQLparam.Value = CoreGeneral.Common.GeneralConfigurations.OutboundStagingDB;
                                break;
                            case "@OrganizationID":
                                SQLparam.Value = Filters.OrganizationID;
                                break;
                            case "@TextSearch":
                                SQLparam.Value = Filters.TextSearch;
                                break;
                            default:
                                SQLparam.Value = Param.ParameterValue;
                                break;
                        }
                    }
                    SQLparam.Direction = (ParameterDirection)Param.Direction.GetHashCode();
                    cmd.Parameters.Add(SQLparam);
                }
                cmd.CommandTimeout = 3600000;
                cmd.CommandType = CommandType.StoredProcedure;

                if (!GetOutputTable)
                {
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    sqlReader = cmd.ExecuteReader();
                    if (sqlReader != null && !sqlReader.IsClosed)
                    {
                        dtOutput = new DataTable();
                        dtOutput.Load(sqlReader);
                    }
                }

                foreach (Parameter p in proc.Parameters.Values)
                {
                    if (p.Direction == ParamDirection.Output)
                    {
                        p.ParameterValue = cmd.Parameters[p.ParameterName].Value.ToString();
                    }
                }

                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private void bgw_CheckProgress(object sender, DoWorkEventArgs e)
        {
            try
            {
                DataTable dtExecutionDetails = new DataTable();
                while (TriggerID != -1)
                {
                    int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
                    dtExecutionDetails = new DataTable();
                    if (ReadExecutionDetails)
                    {
                        GetExecutionResults(ExecutionTableName, OrgTriggerID, ref TotalRows, ref Inserted, ref Updated, ref Skipped, ExecDetailsReadQry, LastProcessID, ref dtExecutionDetails, db_res);
                    }
                    else
                    {
                        GetExecutionResults(ExecutionTableName, OrgTriggerID, ref TotalRows, ref Inserted, ref Updated, ref Skipped, db_res);
                    }
                    SetProgressMax(TotalRows);
                    ReportProgress(Inserted + Updated + Skipped);
                    if (dtExecutionDetails != null && dtExecutionDetails.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dtExecutionDetails.Rows)
                        {
                            if (dtExecutionDetails.Columns.Contains("ID"))
                                LastProcessID = Convert.ToInt32(dr["ID"]);
                            if (dtExecutionDetails.Columns.Contains("Message"))
                                WriteMessage("\r\n" + dr["Message"].ToString());
                        }
                    }
                    System.Threading.Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public Result ExportTransImages()
        {
            Result res = Result.UnKnown;
            try
            {
//                SELECT* FROM[Transaction] WHERE TransactionDate >= '2020-07-01' AND DeliveryChargesID IS NULL
//SELECT* FROM TransSignature WHERE TransactionID = 'INV-OV3-000043'
//SELECT* FROM TransactionImage WHERE TransactionID = 'INV-OV3-000043'

            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result RunExtractTransactionsMapImages()
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db_vms, "SELECT TransactionID,CustomerID,OutletID,GPSLatitude,GPSLongitude FROM [Transaction] WHERE IsPrinted = 0");
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    FilesManager fileMngr = new FilesManager(true);
                    DataTable dtTrans = incubeQuery.GetDataTable();
                    for (int i = 0; i < dtTrans.Rows.Count;i++)
                    {
                        string TransactionID = dtTrans.Rows[i]["TransactionID"].ToString();
                        int CustomerID = Convert.ToInt32(dtTrans.Rows[i]["CustomerID"]);
                        int OutletID = Convert.ToInt32(dtTrans.Rows[i]["OutletID"]);
                        decimal GPSLatitude = Convert.ToDecimal(dtTrans.Rows[i]["GPSLatitude"]);
                        decimal GPSLongitude = Convert.ToDecimal(dtTrans.Rows[i]["GPSLongitude"]);
                        if (fileMngr.SaveTransactionMapImage(TransactionID, CustomerID, OutletID, GPSLatitude, GPSLongitude) == Result.Success)
                        {
                            string qry = string.Format("UPDATE [Transaction] SET IsPrinted = 1 WHERE TransactionID = '{0}' AND CustomerID = {1} AND OutletID = {2}", TransactionID, CustomerID, OutletID);
                            incubeQuery = new InCubeQuery(db_vms, qry);
                            incubeQuery.ExecuteNonQuery();
                        }
                    }
                    res = Result.Success;
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public virtual Result ExportImages()
        {
            try
            {
                WriteMessage("\r\nExporting images ..");
                incubeQuery = new InCubeQuery(string.Format(@"SELECT I.RouteHistoryID,I.ReadingID,I.FieldID,I.ImageID,FORMAT(R.ReadingDate,'yyyy-MM-dd') ReadingDate,EL.Description EmployeeName, COL.Description CustomerName
, SCH.CustomerID, SCH.OutletID, SCH.SurveyID, RH.DeviceSerial
FROM FieldValueImages I
INNER JOIN Reading R ON R.RouteHistoryID = I.RouteHistoryID AND R.ReadingID = I.ReadingID
INNER JOIN EmployeeLanguage EL ON EL.EmployeeID = R.EmployeeID AND EL.LanguageID = 1
INNER JOIN SurveyCustomerHistory SCH ON SCH.RouteHistoryID = R.RouteHistoryID AND SCH.ReadingID = R.ReadingID
INNER JOIN CustomerOutletLanguage COL ON COL.CustomerID = SCH.CustomerID AND COL.OutletID = SCH.OutletID AND COL.LanguageID = 1
INNER JOIN RouteHistory RH ON RH.RouteHistoryID = I.RouteHistoryID
WHERE R.ReadingDate > '{0}' AND R.ReadingDate < DATEADD(DD,1,'{1}')"
, Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.ToString("yyyy-MM-dd")), db_vms);
                
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    DataTable dtImages = new DataTable();
                    dtImages = incubeQuery.GetDataTable();
                    if (dtImages.Rows.Count > 0)
                    {
                        WriteMessage(dtImages.Rows.Count + " images(s) found ..\r\n");
                        ClearProgress();
                        SetProgressMax(dtImages.Rows.Count);
                        string DestPath = "", SrcPath = "";
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(CoreGeneral.Common.StartupPath + "\\DataSources.xml");
                        DestPath = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'ImagesDestination']/Data").InnerText;
                        SrcPath = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'ImagesSource']/Data").InnerText;

                        int success = 0;
                        int failure = 0;
                        int exportedAlready = 0;
                        for (int i = 0; i < dtImages.Rows.Count; i++)
                        {
                            Result res = Result.UnKnown;
                            ReportProgress();
                            string message = "";
                            int RouteHistoryID = Convert.ToInt32(dtImages.Rows[i]["RouteHistoryID"]);
                            int ReadingID = Convert.ToInt32(dtImages.Rows[i]["ReadingID"]);
                            int FieldID = Convert.ToInt32(dtImages.Rows[i]["FieldID"]);
                            int ImageID = Convert.ToInt32(dtImages.Rows[i]["ImageID"]);
                            int CustomerID = Convert.ToInt32(dtImages.Rows[i]["CustomerID"]);
                            int OutletID = Convert.ToInt32(dtImages.Rows[i]["OutletID"]);
                            int SurveyID = Convert.ToInt32(dtImages.Rows[i]["SurveyID"]);
                            string DeviceSerial = dtImages.Rows[i]["DeviceSerial"].ToString();

                            object LastRes = "";
                            incubeQuery = new InCubeQuery(db_vms, string.Format(@"DECLARE @ResultID INT = -1
SELECT TOP 1 @ResultID = ResultID FROM ImagesExportPath WHERE RouteHistoryID = {0} AND ReadingID = {1} AND FieldID = {2} AND ImageID = {3} ORDER BY ID DESC
SELECT @ResultID", RouteHistoryID, ReadingID, FieldID, ImageID));
                            incubeQuery.ExecuteScalar(ref LastRes);
                            if (LastRes.ToString() == "1")
                            {
                                exportedAlready++;
                                continue;
                            }

                            string EmployeeName = dtImages.Rows[i]["EmployeeName"].ToString();
                            string Date = dtImages.Rows[i]["ReadingDate"].ToString();
                            string CustomerName = dtImages.Rows[i]["CustomerName"].ToString();

                            string ImagePath = DestPath + "\\" + EmployeeName + "\\" + Date + "\\";
                            string ImageName = ImagePath + 
                                CustomerName.Replace("\\","").Replace("/", "") + "_" + 
                                ImageID + ".jpg";
                            WriteMessage("Exporting image: " + ImageName + " .. ");
                            object ID = 0;
                            int id = 0;

                            string srcImage = string.Format(SrcPath + @"\{0}_{1}\SurveyImages\Cust_{2}_{3}_{4}_{5}_{6}_{7}_{1}_SurveyImage.jpg", DeviceSerial, RouteHistoryID, CustomerID, OutletID, SurveyID, FieldID, ReadingID, ImageID);
                            incubeQuery = new InCubeQuery(db_vms, string.Format(@"INSERT INTO ImagesExportPath (TriggerID,RouteHistoryID,ReadingID,FieldID,ImageID,ImagePath,ExportDate,ImageSourcePath) 
                                    VALUES ({0},{1},{2},{3},{4},'{5}',GETDATE(),'{6}'); SELECT SCOPE_IDENTITY(); ", TriggerID, RouteHistoryID, ReadingID, FieldID, ImageID, ImagePath,srcImage));
                            if (incubeQuery.ExecuteScalar(ref ID) == InCubeErrors.Success)
                            {
                                id = int.Parse(ID.ToString());

                                try
                                {
                                    DirectoryInfo d = new DirectoryInfo(ImagePath);
                                    if (!d.Exists)
                                        d.Create();

                                    //                                    object imgValue = "";
                                    //                                    incubeQuery = new InCubeQuery(db_vms, string.Format(@"
                                    //SELECT ImageValue FROM FieldValueImages WHERE RouteHistoryID = {0} AND ReadingID = {1} AND FieldID = {2} AND ImageID = {3}"
                                    //, RouteHistoryID, ReadingID, FieldID, ImageID));
                                    //                                    incubeQuery.ExecuteScalar(ref imgValue);

                                    //                                    byte[] ImageValue = (byte[])(imgValue);
                                    //                                    MemoryStream ms = new MemoryStream(ImageValue);
                                    //                                    Image img = Image.FromStream(ms);

                                    //                                    img.Save(ImageName, ImageFormat.Jpeg);
                                    
                                    File.Copy(srcImage, ImageName, true);
                                    res = Result.Success;
                                    message = "Success";
                                    success++;
                                }
                                catch (Exception ex)
                                {
                                    res = Result.Failure;
                                    failure++;
                                    message = ex.Message;
                                }
                                finally
                                {
                                    WriteMessage(message + "\r\n");
                                    incubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE ImagesExportPath SET ResultID = {0}, Message = '{1}' WHERE ID = {2}", res.GetHashCode(), message, id));
                                    incubeQuery.ExecuteNonQuery();
                                }
                            }
                        }
                        WriteMessage(string.Format("\r\nImages export finished, total found : {0}, Exported before : {1}, Success : {2}, Failure : {3}\r\n", dtImages.Rows.Count, exportedAlready, success, failure));
                    }
                    else
                    {
                        WriteMessage("No images to export ..\r\n");
                    }
                }
                else
                {
                    WriteMessage("Query images failed!!\r\n");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return Result.Success;
        }
        public void Dispose()
        {
            try
            {
                if (incubeQuery != null)
                    incubeQuery.Close();

                if (db_vms != null)
                    db_vms.Dispose();

                if (db_res != null)
                    db_res.Dispose();

                if (db_ERP != null)
                    db_ERP.Dispose();

                if (db_ERP2 != null)
                    db_ERP2.Dispose();

                if (db_ERP_GET != null)
                    db_ERP_GET.Dispose();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
    }
}