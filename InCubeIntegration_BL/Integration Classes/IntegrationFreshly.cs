using InCubeIntegration_DAL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace InCubeIntegration_BL
{
    public class IntegrationFreshly : IntegrationBase
    {
        InCubeQuery incubeQuery = null;
        Dictionary<string, string> columns = new Dictionary<string, string>();
        int sourceRowsCount = 0;
        SqlBulkCopy bulk = null;
        private enum APIInterface
        {
            Item,
            Customer,
            PriceDefiniton,
            PriceAssignment,
            Outstanding
        }
        public IntegrationFreshly(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            try
            {
                if (ExecManager.Action_Type == ActionType.Update)
                {
                    CommonMastersUpdate = true;
                }
            }
            catch (Exception ex)
            {
                Initialized = false;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        class UOM_F
        {
            public string ItemUOM { get; set; }
            public string ConversionFactor { get; set; }
        }
        class Item_F
        {
            public string OrgCode { get; set; }
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public string ItemDivisionCode { get; set; }
            public string ItemDivisionName { get; set; }
            public string ItemCategoryCode { get; set; }
            public string ItemCategoryName { get; set; }
            public string ItemGroupCode { get; set; }
            public string ItemGroupName { get; set; }
            public string ItemBrandCode { get; set; }
            public string ItemBrandName { get; set; }
            public string PackBarcode { get; set; }
            public string TaxPercentage { get; set; }
            public string InActive { get; set; }
            public UOM_F[] UOM { get; set; }
        }
        class Customer_F
        {
            public string OrgCode { get; set; }
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public string BillToCode { get; set; }
            public string BillToName { get; set; }
            public string ShipToCode { get; set; }
            public string ShipToName { get; set; }
            public string CustomerType { get; set; }
            public string PaymentTermDays { get; set; }
            public string CreditLimit { get; set; }
            public string InActive { get; set; }
            public string Address { get; set; }
            public string Phone { get; set; }
            public string Mobile { get; set; }
            public string Fax { get; set; }
            public string GroupCode { get; set; }
            public string GroupName { get; set; }
            public string Taxable { get; set; }
            public string Currency { get; set; }
            public string TRN_No { get; set; }
        }
        class PriceDef_F
        {
            public string OrgCode { get; set; }
            public string PriceListCode { get; set; }
            public string PriceListName { get; set; }
            public string ValidFrom { get; set; }
            public string ValidTo { get; set; }
            public string ItemCode { get; set; }
            public string ItemUOM { get; set; }
            public string Price { get; set; }
            public string Currency { get; set; }
        }
        class PriceAssign_F
        {
            public string OrgCode { get; set; }
            public string PriceListCode { get; set; }
            public string AllCustomers { get; set; }
            public string CustomerCode { get; set; }
            public string GroupCode { get; set; }
        }
        class Outstanding_FF
        {
            public string OrgCode { get; set; }
            public string BillToCode { get; set; }
            public string ShipToCode { get; set; }
            public string TransactionNumber { get; set; }
            public string REF_NO { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal RemainingAmount { get; set; }
            public string SalesmanCode { get; set; }
            public DateTime InvoiceDate { get; set; }
        }

        public override void GetMasterData()
        {
            IntegrationField field = (IntegrationField)FieldID;
            Result res = Result.UnKnown;
            Initialized = false;

            Dictionary<string, string> Staging = new Dictionary<string, string>();
            string MasterName = field.ToString().Remove(field.ToString().Length - 2, 2);
            if (CoreGeneral.Common.userPrivileges.UpdateFieldsAccess.ContainsKey(field))
                MasterName = CoreGeneral.Common.userPrivileges.UpdateFieldsAccess[field].Description;
            Dictionary<int, string> filters;
            int ProcessID = 0;
            string APICallResult = "";
            int totalRows = 0;
            WriteMessage("\r\nRetrieving " + MasterName + " from API ...");
            try
            {
                //Log begining of read from Oracle
                filters = new Dictionary<int, string>();
                filters.Add(1, MasterName);
                filters.Add(2, "Reading from API");
                ProcessID = execManager.LogIntegrationBegining(TriggerID, OrganizationID, filters);
                Dictionary<string, DataTable> StagingTables = new Dictionary<string, DataTable>();

                switch (field)
                {
                    case IntegrationField.Item_U:
                        DataTable dtItems = new DataTable();
                        res = GetTableFromAPI(APIInterface.Item, ref dtItems);
                        StagingTables.Add("Stg_Items", dtItems);
                        totalRows = dtItems.Rows.Count;
                        break;
                    case IntegrationField.Customer_U:
                        DataTable dtCustomers = new DataTable();
                        res = GetTableFromAPI(APIInterface.Customer, ref dtCustomers);
                        StagingTables.Add("Stg_Customers", dtCustomers);
                        totalRows = dtCustomers.Rows.Count;
                        break;
                    case IntegrationField.Price_U:
                        DataTable dtPriceDef = new DataTable();
                        res = GetTableFromAPI(APIInterface.PriceDefiniton, ref dtPriceDef);
                        StagingTables.Add("Stg_PriceDefinition", dtPriceDef);
                        totalRows = dtPriceDef.Rows.Count;
                        if (res == Result.Success || res == Result.NoRowsFound)
                        {
                            DataTable dtPriceAssign = new DataTable();
                            res = GetTableFromAPI(APIInterface.PriceAssignment, ref dtPriceAssign);
                            StagingTables.Add("Stg_PriceAssignment", dtPriceAssign);
                            totalRows += dtPriceAssign.Rows.Count;
                        }
                        break;
                    case IntegrationField.Outstanding_U:
                        DataTable dtOutstanding = new DataTable();
                        res = GetTableFromAPI(APIInterface.Outstanding, ref dtOutstanding);
                        StagingTables.Add("Stg_Outstanding", dtOutstanding);
                        totalRows = dtOutstanding.Rows.Count;
                        break;
                }

                if (res != Result.Success)
                {
                    if (res == Result.NoRowsFound)
                    {
                        WriteMessage(" No data found !!");
                        APICallResult = "No data retreived";
                    }
                    else
                    {
                        WriteMessage(" Error in reading from API !!");
                        APICallResult = "Error reading from API";
                    }
                    return;
                }

                WriteMessage(" Rows retrieved: " + totalRows);
                APICallResult = "Rows retrieved: " + totalRows;

                execManager.UpdateActionTotalRows(TriggerID, totalRows);

                WriteMessage("\r\nSaving data to staging table ... ");

                foreach (KeyValuePair<string, DataTable> pair in StagingTables)
                {
                    string Message = "";
                    res = SaveTable(pair.Value, pair.Key, ref Message);
                    if (res != Result.Success)
                    {
                        WriteMessage(Message);
                        APICallResult += " " + Message;
                        return;
                    }
                }

                WriteMessage(" Success ..");                
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("Error !!!");
                APICallResult = "Unhandled exception";
            }
            finally
            {
                if (res == Result.Success)
                {
                    Initialized = true;
                    WriteMessage("\r\nProcessing with SQL procedures ..");   
                }
                else
                {
                    WriteMessage("\r\nProcess terminated!!");
                }
                execManager.LogIntegrationEnding(ProcessID, res, "", APICallResult);
                WriteMessage("\r\n");
            }
        }
        public override void RunPostActionFunction()
        {
            IntegrationField field = (IntegrationField)FieldID;
            Result res = Result.UnKnown;

            if (field != IntegrationField.Item_U && field != IntegrationField.Customer_U && field != IntegrationField.Price_U)
                return;

            Dictionary<string, string> Staging = new Dictionary<string, string>();
            string MasterName = field.ToString().Remove(field.ToString().Length - 2, 2);
            if (CoreGeneral.Common.userPrivileges.UpdateFieldsAccess.ContainsKey(field))
                MasterName = CoreGeneral.Common.userPrivileges.UpdateFieldsAccess[field].Description;
            Dictionary<int, string> filters;
            int ProcessID = 0;
            string message = "";

            WriteMessage("\r\nPosting processing results to API ...");
            try
            {
                //Log begining of read from Oracle
                filters = new Dictionary<int, string>();
                filters.Add(1, MasterName);
                filters.Add(2, "Posting processing results .. ");
                ProcessID = execManager.LogIntegrationBegining(OrgTriggerID, OrganizationID, filters);

                switch (field)
                {
                    case IntegrationField.Item_U:
                        incubeQuery = new InCubeQuery(db_vms, string.Format("SELECT DISTINCT ItemCode, Message AS Erorr_Message FROM Stg_Items WHERE TriggerID = {0} AND Message IS NOT NULL", OrgTriggerID));
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            message = "Failed in reading status table";
                            WriteMessage(" Failed !!");
                        }
                        else
                        {
                            DataTable dtStatus = incubeQuery.GetDataTable();
                            if (dtStatus.Rows.Count > 0)
                            {
                                using (APIManager am = new APIManager())
                                {
                                    string json = am.GetJsonFromDataTable("ItemStatus", dtStatus, -1);
                                    if (json != string.Empty)
                                    {
                                        string resp = "";
                                        if (am.CallPostFunction(CoreGeneral.Common.GeneralConfigurations.WS_URL + @"/Master/api/Item", json, ref resp) == Result.Success)
                                        {
                                            WriteMessage(" Status updated for " + dtStatus.Rows.Count);
                                            message = "Status updated for " + dtStatus.Rows.Count + ", Response = " + resp;
                                            res = Result.Success;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                WriteMessage(" No status records in DB");
                                message = "No status records in DB";
                                res = Result.NoRowsFound;
                            }
                        }
                        break;
                    case IntegrationField.Customer_U:
                        incubeQuery = new InCubeQuery(db_vms, string.Format("SELECT DISTINCT CardCode,CardName,BillToCode,BillToName,ShipToCode,ShipToName,Message AS Erorr_Message FROM Stg_Customers WHERE TriggerID = {0} AND Message IS NOT NULL", OrgTriggerID));
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            message = "Failed in reading status table";
                            WriteMessage(" Failed !!");
                        }
                        else
                        {
                            DataTable dtStatus = incubeQuery.GetDataTable();
                            if (dtStatus.Rows.Count > 0)
                            {
                                using (APIManager am = new APIManager())
                                {
                                    string json = am.GetJsonFromDataTable("BPStatus", dtStatus, -1);
                                    if (json != string.Empty)
                                    {
                                        string resp = "";
                                        if (am.CallPostFunction(CoreGeneral.Common.GeneralConfigurations.WS_URL + @"/Master/api/BP", json, ref resp) == Result.Success)
                                        {
                                            WriteMessage(" Status updated for " + dtStatus.Rows.Count);
                                            message = "Status updated for " + dtStatus.Rows.Count + ", Response = " + resp;
                                            res = Result.Success;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                WriteMessage(" No status records in DB");
                                message = "No status records in DB";
                                res = Result.NoRowsFound;
                            }
                        }
                        break;
                    case IntegrationField.Price_U:
                        WriteMessage("\r\nPrice definition: ");
                        message = "Definition: ";
                        incubeQuery = new InCubeQuery(db_vms, string.Format("SELECT DISTINCT PriceListCode,ItemCode,ItemUOM,Message AS Erorr_Message FROM Stg_PriceDefinition WHERE TriggerID = {0} AND Message IS NOT NULL", OrgTriggerID));
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            message += "Failed in reading definition status table";
                            WriteMessage(" Failed !!");
                        }
                        else
                        {
                            DataTable dtStatus = incubeQuery.GetDataTable();
                            if (dtStatus.Rows.Count > 0)
                            {
                                using (APIManager am = new APIManager())
                                {
                                    string json = am.GetJsonFromDataTable("PriceDefStatus", dtStatus, -1);
                                    if (json != string.Empty)
                                    {
                                        string resp = "";
                                        if (am.CallPostFunction(CoreGeneral.Common.GeneralConfigurations.WS_URL + @"/Master/api/PriceDef", json, ref resp) == Result.Success)
                                        {
                                            WriteMessage(" Status updated for " + dtStatus.Rows.Count);
                                            message += "Status updated for " + dtStatus.Rows.Count + ", Response = " + resp;
                                            res = Result.Success;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                WriteMessage(" No status records in DB");
                                message += "No status records in DB";
                                res = Result.NoRowsFound;
                            }
                        }
                        WriteMessage("\r\nPrice Assignment: ");
                        message += ", Assignment: ";
                        incubeQuery = new InCubeQuery(db_vms, string.Format("SELECT DISTINCT PriceListCode,Message AS Erorr_Message FROM Stg_PriceAssignment WHERE TriggerID = {0} AND Message IS NOT NULL", OrgTriggerID));
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            message += "Failed in reading assignment status table";
                            WriteMessage(" Failed !!");
                        }
                        else
                        {
                            DataTable dtStatus = incubeQuery.GetDataTable();
                            if (dtStatus.Rows.Count > 0)
                            {
                                using (APIManager am = new APIManager())
                                {
                                    string json = am.GetJsonFromDataTable("PriceAssignStatus", dtStatus, -1);
                                    if (json != string.Empty)
                                    {
                                        string resp = "";
                                        if (am.CallPostFunction(CoreGeneral.Common.GeneralConfigurations.WS_URL + @"/Master/api/PriceAssignment", json, ref resp) == Result.Success)
                                        {
                                            WriteMessage(" Status updated for " + dtStatus.Rows.Count);
                                            message += "Status updated for " + dtStatus.Rows.Count + ", Response = " + resp;
                                            res = Result.Success;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                WriteMessage(" No status records in DB");
                                message += "No status records in DB";
                                res = Result.NoRowsFound;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage(" Error !!!");
                message = "Unhandled exception";
            }
            finally
            {
                execManager.LogIntegrationEnding(ProcessID, res, "", message);
                WriteMessage("\r\n");
            }
        }
        private Result SaveTable(DataTable dtData, string TableName, ref string Message)
        {
            Result res = Result.Failure;
            try
            {
                //Get first row in staging table
                incubeQuery = new InCubeQuery(db_vms, "SELECT TOP 1 * FROM [" + TableName + "]");
                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    Message = "Error reading from staging table";
                    return Result.Failure;
                }
                DataTable dtStaging = incubeQuery.GetDataTable();
                if (dtStaging == null)
                {
                    Message = "Error reading from staging table";
                    return Result.Failure;
                }

                dtData.Columns.Add("ID", typeof(int));
                dtData.Columns.Add("TriggerID", typeof(int));

                int ID = 0;
                foreach (DataRow _row in dtData.Rows)
                {
                    _row["ID"] = ++ID;
                    _row["TriggerID"] = TriggerID;
                }

                //Bulk copy
                Dictionary<string, string> ColumnsMapping = new Dictionary<string, string>();

                for (int i = 0; i < dtStaging.Columns.Count; i++)
                {
                    string StagingColumn = dtStaging.Columns[i].ColumnName;
                    if (dtData.Columns.Contains(StagingColumn))
                    {
                        ColumnsMapping.Add(StagingColumn, StagingColumn);
                    }
                }

                SqlBulkCopy bulk = new SqlBulkCopy(db_vms.GetConnection());
                bulk.DestinationTableName = TableName;
                foreach (KeyValuePair<string, string> pair in ColumnsMapping)
                    bulk.ColumnMappings.Add(pair.Key, pair.Value);

                bulk.BulkCopyTimeout = 120;
                decimal loopSize = 10000;
                int loops = (int)Math.Ceiling((decimal)dtData.Rows.Count / loopSize);
                SetProgressMax(loops);
                for (int i = 0; i < loops; i++)
                {
                    dtData.DefaultView.RowFilter = string.Format("ID >= {0} AND ID <= {1}", i * loopSize + 1, (i + 1) * loopSize);
                    bulk.WriteToServer(dtData.DefaultView.ToTable());
                    ReportProgress();
                }
                res = Result.Success;
                Message = "Success";
            }
            catch (Exception ex)
            {
                Message = "Error saving to staging table !!";
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result GetTableFromAPI(APIInterface intrfc, ref DataTable dtData)
        {
            Result res = Result.UnKnown;
            dtData = null;
            try
            {
                switch (intrfc)
                {
                    case APIInterface.Item:
                        dtData = Tools.GetRequestTable<Item_F>(CoreGeneral.Common.GeneralConfigurations.WS_URL + @"/Master/api/Item", "", "noroot", "", "", "GET", null);
                        break;
                    case APIInterface.Customer:
                        dtData = Tools.GetRequestTable<Customer_F>(CoreGeneral.Common.GeneralConfigurations.WS_URL + @"/Master/api/BP", "", "noroot", "", "", "GET", null);
                        break;
                    case APIInterface.PriceDefiniton:
                        dtData = Tools.GetRequestTable<PriceDef_F>(CoreGeneral.Common.GeneralConfigurations.WS_URL + @"/Master/api/PriceDef", "", "noroot", "", "", "GET", null);
                        break;
                    case APIInterface.PriceAssignment:
                        dtData = Tools.GetRequestTable<PriceAssign_F>(CoreGeneral.Common.GeneralConfigurations.WS_URL + @"/Master/api/PriceAssignment", "", "noroot", "", "", "GET", null);
                        break;
                    case APIInterface.Outstanding:
                        dtData = Tools.GetRequestTable<Outstanding_FF>(CoreGeneral.Common.GeneralConfigurations.WS_URL + @"/Master/api/Outstanding", "", "noroot", "", "", "GET", null);
                        break;
                }
                
                if (dtData == null)
                {
                    res = Result.Invalid;
                }
                else if (dtData.Rows.Count == 0)
                {
                    res = Result.NoRowsFound;
                }
                else
                {
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
        public override void SendInvoices()
        {
            SendTransaction(IntegrationField.Sales_S);
        }
        public override void SendReturn()
        {
            SendTransaction(IntegrationField.Returns_S);
        }
        private void SendTransaction(IntegrationField field)
        {
            try
            {
                if (field == IntegrationField.Sales_S)
                {
                    WriteMessage("\r\nSending invoices ... ");
                }
                else if (field == IntegrationField.Returns_S)
                {
                    WriteMessage("\r\nSending returns ... ");
                }

                Result result = Result.UnKnown;
                Result MainResult = Result.UnKnown;
                int processID = 0;
                string TransactionID = "", CustomerID = "", OutletID = "", ID = "", UpdateSynchronizedQuery = "";
                string message = "";

                //Call procedure to prepare transaction details to send
                Procedure Proc = new Procedure("sp_PrepareTransactionsToSend");
                Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID);
                Proc.AddParameter("@EmployeeID", ParamType.Integer, Filters.EmployeeID);
                Proc.AddParameter("@FromDate", ParamType.DateTime, Filters.FromDate);
                Proc.AddParameter("@ToDate", ParamType.DateTime, Filters.ToDate);
                Proc.AddParameter("@TransactionType", ParamType.Integer, field == IntegrationField.Sales_S ? 1 : 2);
                MainResult = ExecuteStoredProcedure(Proc);
                if (MainResult != Result.Success)
                {
                    WriteMessage("Transactions preperation failed !!");
                    return;
                }

                //Read prepared transactions
                string HeadersQuery = string.Format(@"SELECT * FROM Stg_TransactionHeader WHERE I_TriggerID = {0} AND I_ResultID = 1", TriggerID);
                incubeQuery = new InCubeQuery(db_vms, HeadersQuery);
                DataTable dtTransHeader = new DataTable();
                DataTable dtHeaderClone = new DataTable();
                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    WriteMessage("Transactions query failed !!");
                    return;
                }
                else
                {
                    dtTransHeader = incubeQuery.GetDataTable();
                    execManager.UpdateActionTotalRows(TriggerID, dtTransHeader.Rows.Count);
                    if (dtTransHeader.Rows.Count == 0)
                    {
                        WriteMessage("There is no transactions to send ..");
                        return;
                    }
                    //Clone header table, remove all I_ columns
                    dtHeaderClone = dtTransHeader.Copy();
                    int columnsCount = dtHeaderClone.Columns.Count;
                    for (int index = columnsCount - 1; index >= 0; index--)
                    {
                        if (dtHeaderClone.Columns[index].ColumnName.StartsWith("I_"))
                        {
                            dtHeaderClone.Columns.RemoveAt(index);
                        }
                    }
                }

                ClearProgress();
                SetProgressMax(dtTransHeader.Rows.Count);

                for (int i = 0; i < dtTransHeader.Rows.Count; i++)
                {
                    try
                    {
                        //Read transaction primary key and log to execution details as well as to staging table
                        result = Result.UnKnown;
                        processID = 0;
                        message = "";
                        TransactionID = dtTransHeader.Rows[i]["TransactionNumber"].ToString();
                        CustomerID = dtTransHeader.Rows[i]["I_CustomerID"].ToString();
                        OutletID = dtTransHeader.Rows[i]["I_OutletID"].ToString();
                        ID = dtTransHeader.Rows[i]["I_ID"].ToString();
                        WriteMessage("\r\nSending transaction " + TransactionID + ": ");
                        ReportProgress();
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, TransactionID);
                        filters.Add(9, CustomerID);
                        filters.Add(10, OutletID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        if (processID == 0)
                        {
                            result = Result.Failure;
                            message = "Error logging execution start";
                            continue;
                        }
                        incubeQuery = new InCubeQuery(db_vms, string.Format(@"UPDATE Stg_TransactionHeader SET I_ProcessID = {0} WHERE I_TriggerID = {1} AND I_ID = {2}", processID, TriggerID, ID));
                        if (incubeQuery.ExecuteNonQuery() != InCubeErrors.Success)
                        {
                            result = Result.Failure;
                            message = "Error updating process ID";
                            continue;
                        }
                        UpdateSynchronizedQuery = string.Format("UPDATE [Transaction] SET Synchronized = 1 WHERE TransactionID = '{0}' AND CustomerID = {1} AND OutletID = {2}", TransactionID, CustomerID, OutletID);

                        //Check last run for same transaction to avoid duplications
                        Result lastRes = GetLastExecutionResultForEntry(field, new List<string>(filters.Values), processID, 600);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                            incubeQuery.ExecuteNonQuery();
                            result = Result.Duplicate;
                            message = "Transaction already sent !!";
                            continue;
                        }

                        //Read details for current trasnaction
                        string DetailsQuery = string.Format(@"SELECT * FROM Stg_TransactionDetails WHERE I_TriggerID = {0} AND I_ID = {1}", TriggerID, ID);
                        incubeQuery = new InCubeQuery(db_vms, DetailsQuery);
                        DataTable dtTransactionsDetails = new DataTable();
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            result = Result.Failure;
                            message = "Failure reading details !!";
                            continue;
                        }
                        else
                        {
                            dtTransactionsDetails = incubeQuery.GetDataTable();
                            if (dtTransactionsDetails.Rows.Count == 0)
                            {
                                result = Result.NoRowsFound;
                                message = "No details returned !!";
                                continue;
                            }
                        }

                        //send
                        //Get header row
                        DataRow drHeader = dtHeaderClone.Rows[i];
                        //Remove all I_ columns form details table
                        int columnsCount = dtTransactionsDetails.Columns.Count;
                        for (int index = columnsCount - 1; index >= 0; index--)
                        {
                            if (dtTransactionsDetails.Columns[index].ColumnName.StartsWith("I_"))
                            {
                                dtTransactionsDetails.Columns.RemoveAt(index);
                            }
                        }
                        //Get JSON
                        using (APIManager am = new APIManager())
                        {
                            string json = am.GetJsonFromDataTable(drHeader, "items", dtTransactionsDetails);
                            if (json != string.Empty)
                            {
                                string resp = "";
                                Result apiRes = am.CallPostFunction(CoreGeneral.Common.GeneralConfigurations.WS_URL + @"/Sales/api/Sales", json, ref resp);
                                if (apiRes == Result.Success)
                                {
                                    if (resp.ToLower().Contains("success"))
                                    {
                                        result = Result.Success;
                                    }
                                    else
                                    {
                                        result = Result.Invalid;
                                    }
                                    message = resp;
                                }
                                else
                                {
                                    result = Result.Failure;
                                    message = "Failed in posting to API";
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        message = "Unhandled exception in processing transaction";
                        result = Result.Failure;
                    }
                    finally
                    {
                        if (result == Result.Success)
                        {
                            incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                            incubeQuery.ExecuteNonQuery();
                        }
                        WriteMessage(message);
                        execManager.LogIntegrationEnding(processID, result, "", message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public override void SendReciepts()
        {
            try
            {
                WriteMessage("\r\nSending collections ... ");

                Result result = Result.UnKnown;
                Result MainResult = Result.UnKnown;
                int processID = 0;
                string PaymentNumber = "", InvoiceNumber = "", UpdateSynchronizedQuery = "";
                int CustomerID = 0, OutletID = 0, MainType = 0;
                string message = "";

                //Call procedure to prepare transaction details to send
                Procedure Proc = new Procedure("sp_PrepareCollectionsToSend");
                Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID);
                Proc.AddParameter("@EmployeeID", ParamType.Integer, Filters.EmployeeID);
                Proc.AddParameter("@FromDate", ParamType.DateTime, Filters.FromDate);
                Proc.AddParameter("@ToDate", ParamType.DateTime, Filters.ToDate);
                MainResult = ExecuteStoredProcedure(Proc);
                if (MainResult != Result.Success)
                {
                    WriteMessage("Payments preperation failed !!");
                    return;
                }

                //Read prepared payments
                string PaymnetsQuery = string.Format(@"SELECT * FROM Stg_Collections WHERE I_TriggerID = {0} ORDER BY I_MainType", TriggerID);
                incubeQuery = new InCubeQuery(db_vms, PaymnetsQuery);
                DataTable dtPayments = new DataTable();
                DataTable dtClonedPayments = new DataTable();
                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    WriteMessage("Payments query failed !!");
                    return;
                }
                else
                {
                    dtPayments = incubeQuery.GetDataTable();
                    execManager.UpdateActionTotalRows(TriggerID, dtPayments.Rows.Count);
                    if (dtPayments.Rows.Count == 0)
                    {
                        WriteMessage("There is no payments to send ..");
                        return;
                    }

                    //Clone table, remove all I_ columns
                    dtClonedPayments = dtPayments.Copy();
                    int columnsCount = dtClonedPayments.Columns.Count;
                    for (int index = columnsCount - 1; index >= 0; index--)
                    {
                        if (dtClonedPayments.Columns[index].ColumnName.StartsWith("I_"))
                        {
                            dtClonedPayments.Columns.RemoveAt(index);
                        }
                    }
                }

                ClearProgress();
                SetProgressMax(dtPayments.Rows.Count);

                for (int i = 0; i < dtPayments.Rows.Count; i++)
                {
                    try
                    {
                        result = Result.UnKnown;
                        processID = 0;
                        message = "";
                        PaymentNumber = dtPayments.Rows[i]["PaymentNumber"].ToString();
                        InvoiceNumber = dtPayments.Rows[i]["InvoiceNumber"].ToString();
                        CustomerID = Convert.ToInt32(dtPayments.Rows[i]["I_CustomerID"]);
                        OutletID = Convert.ToInt32(dtPayments.Rows[i]["I_OutletID"]);
                        MainType = Convert.ToInt32(dtPayments.Rows[i]["I_MainType"]);
                        if (MainType == 1)
                            WriteMessage("\r\nSending payment: " + PaymentNumber + " For " + InvoiceNumber + ": ");
                        else
                            WriteMessage("\r\nSending down payment: " + PaymentNumber + ": ");

                        ReportProgress();
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, PaymentNumber);
                        filters.Add(9, InvoiceNumber);
                        filters.Add(10, CustomerID.ToString() + ":" + OutletID.ToString());
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        if (processID == 0)
                        {
                            result = Result.Failure;
                            message = "Error logging execution start";
                            continue;
                        }

                        incubeQuery = new InCubeQuery(db_vms, string.Format(@"UPDATE Stg_Collections SET I_ProcessID = {0} WHERE I_TriggerID = {1} AND PaymentNumber = '{2}' AND InvoiceNumber = '{3}' AND I_CustomerID = {4} AND I_OutletID = {5}"
, processID, TriggerID, PaymentNumber, InvoiceNumber, CustomerID, OutletID));
                        if (incubeQuery.ExecuteNonQuery() != InCubeErrors.Success)
                        {
                            result = Result.Failure;
                            message = "Error updating process ID";
                            continue;
                        }
                        if (MainType == 1)
                            UpdateSynchronizedQuery = string.Format("UPDATE CustomerPayment SET Synchronized = 1 WHERE CustomerPaymentID = '{0}' AND TransactionID = '{1}' AND CustomerID = {2} AND OutletID = {3}", PaymentNumber, InvoiceNumber, CustomerID, OutletID);
                        else
                            UpdateSynchronizedQuery = string.Format("UPDATE CustomerUnallocatedPayment SET Synchronised = 1 WHERE CustomerPaymentID = '{0}' AND CustomerID = {1} AND OutletID = {2}", PaymentNumber, CustomerID, OutletID);

                        //Check last run for same transaction to avoid duplications
                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Reciept_S, new List<string>(filters.Values), processID, 600);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                            incubeQuery.ExecuteNonQuery();
                            result = Result.Duplicate;
                            message = "Payment already sent !!";
                            continue;
                        }

                        //send
                        //Get JSON
                        using (APIManager am = new APIManager())
                        {
                            string json = am.GetJsonFromDataRow(dtClonedPayments.Rows[i]);
                            if (json != string.Empty)
                            {
                                string resp = "";
                                Result apiRes = am.CallPostFunction(CoreGeneral.Common.GeneralConfigurations.WS_URL + @"/Sales/api/Collection", json, ref resp);
                                if (apiRes == Result.Success)
                                {
                                    if (resp.ToLower().Contains("success"))
                                    {
                                        result = Result.Success;
                                    }
                                    else
                                    {
                                        result = Result.Invalid;
                                    }
                                    message = resp;
                                }
                                else
                                {
                                    result = Result.Failure;
                                    message = "Failed in posting to API";
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        message = "Unhandled exception in processing payment";
                        result = Result.Failure;
                    }
                    finally
                    {
                        if (result == Result.Success)
                        {
                            incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                            incubeQuery.ExecuteNonQuery();
                        }
                        WriteMessage(message);
                        execManager.LogIntegrationEnding(processID, result, "", message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public override void SendTransfers()
        {
            try
            {
                Result result = Result.UnKnown;
                Result MainResult = Result.UnKnown;
                int processID = 0;
                string TransactionID = "", WarehouseID = "", ID = "", UpdateSynchronizedQuery = "";
                string message = "";

                //Call procedure to prepare transaction details to send
                Procedure Proc = new Procedure("sp_PrepareWarehouseTransToSend");
                Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID);
                Proc.AddParameter("@EmployeeID", ParamType.Integer, Filters.EmployeeID);
                Proc.AddParameter("@FromDate", ParamType.DateTime, Filters.FromDate);
                Proc.AddParameter("@ToDate", ParamType.DateTime, Filters.ToDate);
                MainResult = ExecuteStoredProcedure(Proc);
                if (MainResult != Result.Success)
                {
                    WriteMessage("Transactions preperation failed !!");
                    return;
                }

                //Read prepared transactions
                string HeadersQuery = string.Format(@"SELECT * FROM Stg_WHTransHeader WHERE I_TriggerID = {0} AND I_ResultID = 1", TriggerID);
                incubeQuery = new InCubeQuery(db_vms, HeadersQuery);
                DataTable dtTransHeader = new DataTable();
                DataTable dtHeaderClone = new DataTable();
                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    WriteMessage("Transactions query failed !!");
                    return;
                }
                else
                {
                    dtTransHeader = incubeQuery.GetDataTable();
                    execManager.UpdateActionTotalRows(TriggerID, dtTransHeader.Rows.Count);
                    if (dtTransHeader.Rows.Count == 0)
                    {
                        WriteMessage("There is no transactions to send ..");
                        return;
                    }
                    //Clone header table, remove all I_ columns
                    dtHeaderClone = dtTransHeader.Copy();
                    int columnsCount = dtHeaderClone.Columns.Count;
                    for (int index = columnsCount - 1; index >= 0; index--)
                    {
                        if (dtHeaderClone.Columns[index].ColumnName.StartsWith("I_"))
                        {
                            dtHeaderClone.Columns.RemoveAt(index);
                        }
                    }
                }

                ClearProgress();
                SetProgressMax(dtTransHeader.Rows.Count);

                for (int i = 0; i < dtTransHeader.Rows.Count; i++)
                {
                    try
                    {
                        //Read transaction primary key and log to execution details as well as to staging table
                        result = Result.UnKnown;
                        processID = 0;
                        message = "";
                        TransactionID = dtTransHeader.Rows[i]["TransactionNumber"].ToString();
                        WarehouseID = dtTransHeader.Rows[i]["I_WarehouseID"].ToString();
                        ID = dtTransHeader.Rows[i]["I_ID"].ToString();
                        WriteMessage("\r\nSending transaction " + TransactionID + ": ");
                        ReportProgress();
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, TransactionID);
                        filters.Add(9, WarehouseID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        if (processID == 0)
                        {
                            result = Result.Failure;
                            message = "Error logging execution start";
                            continue;
                        }
                        incubeQuery = new InCubeQuery(db_vms, string.Format(@"UPDATE Stg_WHTransHeader SET I_ProcessID = {0} WHERE I_TriggerID = {1} AND I_ID = {2}", processID, TriggerID, ID));
                        if (incubeQuery.ExecuteNonQuery() != InCubeErrors.Success)
                        {
                            result = Result.Failure;
                            message = "Error updating process ID";
                            continue;
                        }
                        UpdateSynchronizedQuery = string.Format("UPDATE WarehouseTransaction SET Synchronized = 1 WHERE TransactionID = '{0}' AND WarehouseID = {1}", TransactionID, WarehouseID);

                        //Check last run for same transaction to avoid duplications
                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Transfers_S, new List<string>(filters.Values), processID, 600);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                            incubeQuery.ExecuteNonQuery();
                            result = Result.Duplicate;
                            message = "Transaction already sent !!";
                            continue;
                        }

                        //Read details for current trasnaction
                        string DetailsQuery = string.Format(@"SELECT * FROM Stg_WHTransDetails WHERE I_TriggerID = {0} AND I_ID = {1}", TriggerID, ID);
                        incubeQuery = new InCubeQuery(db_vms, DetailsQuery);
                        DataTable dtTransactionsDetails = new DataTable();
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            result = Result.Failure;
                            message = "Failure reading details !!";
                            continue;
                        }
                        else
                        {
                            dtTransactionsDetails = incubeQuery.GetDataTable();
                            if (dtTransactionsDetails.Rows.Count == 0)
                            {
                                result = Result.NoRowsFound;
                                message = "No details returned !!";
                                continue;
                            }
                        }

                        //send
                        //Get header row
                        DataRow drHeader = dtHeaderClone.Rows[i];
                        //Remove all I_ columns form details table
                        int columnsCount = dtTransactionsDetails.Columns.Count;
                        for (int index = columnsCount - 1; index >= 0; index--)
                        {
                            if (dtTransactionsDetails.Columns[index].ColumnName.StartsWith("I_"))
                            {
                                dtTransactionsDetails.Columns.RemoveAt(index);
                            }
                        }
                        //Get JSON
                        using (APIManager am = new APIManager())
                        {
                            string json = am.GetJsonFromDataTable(drHeader, "Details", dtTransactionsDetails);
                            if (json != string.Empty)
                            {
                                string resp = "";
                                Result apiRes = am.CallPostFunction(CoreGeneral.Common.GeneralConfigurations.WS_URL + @"/Sales/api/StockTransfers", json, ref resp);
                                if (apiRes == Result.Success)
                                {
                                    if (resp.ToLower().Contains("success"))
                                    {
                                        result = Result.Success;
                                    }
                                    else
                                    {
                                        result = Result.Invalid;
                                    }
                                    message = resp;
                                }
                                else
                                {
                                    result = Result.Failure;
                                    message = "Failed in posting to API";
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        message = "Unhandled exception in processing transaction";
                        result = Result.Failure;
                    }
                    finally
                    {
                        if (result == Result.Success)
                        {
                            incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                            incubeQuery.ExecuteNonQuery();
                        }
                        WriteMessage(message);
                        execManager.LogIntegrationEnding(processID, result, "", message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public override void Close()
        {

        }

    }
}