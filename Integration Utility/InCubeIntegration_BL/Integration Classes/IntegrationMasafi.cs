using InCubeIntegration_DAL;
using InCubeLibrary;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Xml;

namespace InCubeIntegration_BL
{
    public class IntegrationMasafi : IntegrationBase
    {
        InCubeQuery incubeQuery = null;
        Dictionary<string, string> columns = new Dictionary<string, string>();
        int sourceRowsCount = 0;
        OracleConnection OracleConn = null;
        OracleCommand OracleCMD = null;
        OracleTransaction OracleTrans = null;
        OracleDataAdapter OracleADP = null;
        OracleDataReader OracleReader = null;
        SqlBulkCopy bulk = null;
        string LastQueryError = "";
        public IntegrationMasafi(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            try
            {
                if (ExecManager.Action_Type != ActionType.SpecialFunctions)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(CoreGeneral.Common.StartupPath + "\\DataSources.xml");
                    string OracleConnectionString = "";
                    if (ExecManager.Action_Type == ActionType.Send)
                        OracleConnectionString = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'OracleSend']/Data").InnerText;
                    else
                        OracleConnectionString = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'OracleGet']/Data").InnerText;
                    OracleConn = new OracleConnection(OracleConnectionString);
                    OracleConn.Open();
                    CommonMastersUpdate = true;
                }
            }
            catch (Exception ex)
            {
                Initialized = false;
                WriteMessage("Error in initializing Oracle connection ..");
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                if (OracleConn != null && OracleConn.State == ConnectionState.Open)
                {
                    OracleConn.Close();
                }
            }
        }

        public override void GetMasterData()
        {
            IntegrationField field = (IntegrationField)FieldID;
            Result res = Result.UnKnown;
            string result = "";
            Dictionary<string, string> Staging = new Dictionary<string, string>();
            string MasterName = field.ToString().Remove(field.ToString().Length - 2, 2);
            if (CoreGeneral.Common.userPrivileges.UpdateFieldsAccess.ContainsKey(field))
                MasterName = CoreGeneral.Common.userPrivileges.UpdateFieldsAccess[field].Description;
            Dictionary<int, string> filters;
            int ProcessID = 0;
            int rowsCount = 0;

            WriteMessage("\r\nRetrieving " + MasterName + " from Oracle ...");
            try
            {
                //Log begining of read from Oracle
                filters = new Dictionary<int, string>();
                filters.Add(1, MasterName);
                filters.Add(2, "Reading from Oracle");
                ProcessID = execManager.LogIntegrationBegining(TriggerID, OrganizationID, filters);
                incubeQuery = new InCubeQuery(db_vms, "SELECT OracleTable,StagingTable,OracleQuery FROM Int_StagingTables WHERE FieldID = " + FieldID);
                incubeQuery.Execute();
                DataTable dtStaging = incubeQuery.GetDataTable(); 
               
                foreach(DataRow dr in dtStaging.Rows)
                {
                    string OracleTable = dr["OracleTable"].ToString();
                    string OracleQuery = dr["OracleQuery"].ToString().Trim();
                    if (OracleQuery == string.Empty)
                    {
                        OracleQuery = "SELECT * FROM " + OracleTable;
                    }
                    string StagingTable = dr["StagingTable"].ToString();

                    result = SaveTable(OracleQuery, StagingTable, ref rowsCount);
                    if (result == "Success")
                        res = Result.Success;

                    WriteMessage(result);
                    if (res != Result.Success)
                        break;
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("Error !!!");
            }
            finally
            {
                if (res != Result.Success)
                {
                    Initialized = false;
                    WriteMessage("\r\nProcess terminated!!");
                }
                else
                {
                    WriteMessage("\r\nProcessing with SQL procedures ..");
                }
                execManager.LogIntegrationEnding(ProcessID, res, "", res == Result.Success ? "Rows retrieved: " + rowsCount : "");
                WriteMessage("\r\n");
            }
        }
        private string SaveTable(string OracleQuery, string TableName, ref int RowsCount)
        {
            try
            {
                //Get first row in staging table
                incubeQuery = new InCubeQuery(db_vms, "SELECT TOP 1 * FROM [" + TableName + "]");
                if (incubeQuery.Execute() != InCubeErrors.Success)
                    return "Error reading from staging table";
                DataTable dtStaging = incubeQuery.GetDataTable();
                if (dtStaging == null)
                    return "Error reading from staging table";

                //Open Oracle reader
                OracleReader = null;
                try
                {
                    if (OracleConn.State != ConnectionState.Open)
                        OracleConn.Open();

                    OracleCMD = new OracleCommand(OracleQuery, OracleConn);
                    OracleCMD.CommandType = CommandType.Text;
                    OracleCMD.CommandTimeout = 1800;
                    OracleReader = OracleCMD.ExecuteReader();
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\nQuery: " + OracleQuery, LoggingType.Error, LoggingFiles.InCubeLog);
                    return "Error in opening Oracle data reader";
                }

                //Bulk copy
                try
                {
                    Dictionary<string, string> ColumnsMapping = new Dictionary<string, string>();
                    DataTable dtData = new DataTable();
                    dtData.Columns.Add("TriggerID");
                    dtData.Columns.Add("ID");
                    for (int i = 0; i < OracleReader.FieldCount; i++)
                    {
                        string OracleColumn = OracleReader.GetName(i);
                        if (dtStaging.Columns.Contains(OracleColumn))
                        {
                            dtData.Columns.Add(dtStaging.Columns[OracleColumn].ColumnName);
                            ColumnsMapping.Add(OracleColumn, dtStaging.Columns[OracleColumn].ColumnName);
                        }
                    }

                    int count = 0;
                    int ID = 0;
                    DataRow dRow = null;
                    bulk = new SqlBulkCopy(db_vms.GetConnection());
                    bulk.DestinationTableName = TableName;
                    foreach (DataColumn col in dtData.Columns)
                        bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    bulk.BulkCopyTimeout = 300;

                    while (OracleReader.Read())
                    {
                        dRow = dtData.NewRow();
                        dRow["ID"] = ++ID;
                        dRow["TriggerID"] = TriggerID;
                        foreach (KeyValuePair<string, string> pair in ColumnsMapping)
                        {
                            dRow[pair.Value] = OracleReader[pair.Key];
                        }
                        dtData.Rows.Add(dRow);
                        count++;
                        if (count == 1000)
                        {
                            try
                            {
                                bulk.WriteToServer(dtData);
                                RowsCount += count;
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                            }
                            dtData.Rows.Clear();
                            count = 0;
                        }
                    }

                    if (count > 0)
                    {
                        try
                        {
                            bulk.WriteToServer(dtData);
                            RowsCount += count;
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        }
                        dtData.Rows.Clear();
                        count = 0;
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                    return "Error writing to staging table";
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return ex.Message;
            }
            finally
            {
                if (OracleConn != null && OracleConn.State == ConnectionState.Open)
                {
                    OracleConn.Close();
                }
            }
            return "Success";
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

                DataRow dr;
                Result result = Result.UnKnown;
                Result MainResult = Result.UnKnown;
                DataTransferManager dataTRSFR = new DataTransferManager(false);
                Dictionary<string, ColumnType> HeaderDestColumns = null;
                Dictionary<string, ColumnType> DetailsDestColumns = null;
                int processID = 0;
                string TransactionID = "", CustomerID = "", OutletID = "", UpdateSynchronizedQuery = "";
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
                string HeadersQuery = string.Format(@"SELECT * FROM XX_SFA_SALESINVOICEHEADER WHERE TriggerID = {0} AND ResultID = 1", TriggerID);
                incubeQuery = new InCubeQuery(db_vms, HeadersQuery);
                DataTable dtTransHeader = new DataTable();
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
                    else
                    {
                        //Extract distinct values table for header
                        WriteMessage(dtTransHeader.Rows.Count + " transaction(s) found");
                        if (!dtTransHeader.Columns.Contains("ID"))
                            dtTransHeader.Columns.Add("ID", typeof(int));
                        if (!dtTransHeader.Columns.Contains("PUSHED_DATE"))
                            dtTransHeader.Columns.Add("PUSHED_DATE", typeof(DateTime));
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
                        CustomerID = dtTransHeader.Rows[i]["CustomerID"].ToString();
                        OutletID = dtTransHeader.Rows[i]["OutletID"].ToString();
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
                        incubeQuery = new InCubeQuery(db_vms, string.Format(@"
UPDATE XX_Sfa_SALESINVOICEHEADER SET ProcessID = {0} WHERE TriggerID = {1} AND TransactionNumber = '{2}' AND CustomerID = {3} AND OutletID = {4};
UPDATE XX_SFA_SALESINVOICEDETAIL SET ProcessID = {0} WHERE TriggerID = {1} AND TransactionNumber = '{2}' AND CustomerID = {3} AND OutletID = {4};", processID, TriggerID, TransactionID, CustomerID, OutletID));
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
                        string DetailsQuery = string.Format(@"SELECT * FROM XX_SFA_SALESINVOICEDETAIL WHERE TriggerID = {0} AND TransactionNumber = '{1}' AND CustomerID = {2} AND OutletID = {3}", TriggerID, TransactionID, CustomerID, OutletID);
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
                        if (!dtTransactionsDetails.Columns.Contains("ID"))
                            dtTransactionsDetails.Columns.Add("ID", typeof(int));
                        if (!dtTransactionsDetails.Columns.Contains("PUSHED_DATE"))
                            dtTransactionsDetails.Columns.Add("PUSHED_DATE", typeof(DateTime));
                        if (!dtTransactionsDetails.Columns.Contains("DETAILID"))
                            dtTransactionsDetails.Columns.Add("DETAILID", typeof(int));
                        
                        if (HeaderDestColumns == null)
                        {
                            DataTable dtOraHeader = new DataTable();
                            result = GetOracleDataTable("SELECT * FROM XX_SFA_SALESINVOICEHEADER WHERE ROWNUM = 1", ref dtOraHeader);
                            if (result != Result.Success)
                            {
                                message = "Error reading Oracle header table schema";
                                continue;
                            }
                            dataTRSFR.PrepareDestinationColumns(dtTransHeader.Columns, dtOraHeader.Columns, ref HeaderDestColumns);
                        }
                        if (DetailsDestColumns == null)
                        {
                            DataTable dtOraDetails = new DataTable();
                            result = GetOracleDataTable("SELECT * FROM XX_SFA_SALESINVOICEDETAIL WHERE ROWNUM = 1", ref dtOraDetails);
                            if (result != Result.Success)
                            {
                                message = "Error reading Oracle details table schema";
                                continue;
                            }
                            dataTRSFR.PrepareDestinationColumns(dtTransactionsDetails.Columns, dtOraDetails.Columns, ref DetailsDestColumns);
                        }
                        
                        //Insert header value
                        //Get Max ID
                        string MaxIDQry = "SELECT MAX(ID)+1 AS ID FROM XX_SFA_SALESINVOICEHEADER";
                        object MaxHeaderID = null;
                        result = GetOracleScalar(MaxIDQry, false, ref MaxHeaderID);
                        if (result != Result.Success)
                        {
                            message = "Error getting Max header ID";
                            continue;
                        }
                        incubeQuery = new InCubeQuery(db_vms, string.Format(@"UPDATE XX_Sfa_SALESINVOICEHEADER SET OraID = {0} WHERE ProcessID = {1}", MaxHeaderID, processID));
                        incubeQuery.ExecuteNonQuery();
                        dr = dtTransHeader.Rows[i];
                        dr["ID"] = MaxHeaderID;
                        dr["PUSHED_DATE"] = DateTime.Now;
                        
                        string InsertQuery = "";
                        dataTRSFR.GenerateInsertStatement(DataBaseType.Oracle, dr, "XX_SFA_SALESINVOICEHEADER", false, HeaderDestColumns, null, ref InsertQuery);
                        
                        OracleTrans = null;
                        if (OracleConn.State != ConnectionState.Open)
                            OracleConn.Open();
                        OracleTrans = OracleConn.BeginTransaction();
                        
                        result = ExecuteOracleCommand(InsertQuery, true);
                        if (result != Result.Success)
                        {
                            message = "Error inserting header line in Oracle!!";
                            continue;
                        }

                        //Insert details
                        int LastLineNo = 0;
                        object MaxDetailID = null;
                        for (int j = 0; j < dtTransactionsDetails.Rows.Count; j++)
                        {
                            try
                            {
                                int LineNo = Convert.ToInt16(dtTransactionsDetails.Rows[j]["LineNumber"]);
                                if (LastLineNo != LineNo)
                                {
                                    MaxIDQry = "SELECT MAX(DetailID)+1 AS DetailID FROM XX_SFA_SALESINVOICEDETAIL";
                                    MaxDetailID = null;
                                    result = GetOracleScalar(MaxIDQry, true, ref MaxDetailID);
                                    if (result != Result.Success)
                                    {
                                        message = "Error getting Max detail ID";
                                        break;
                                    }
                                    incubeQuery = new InCubeQuery(db_vms, string.Format(@"UPDATE XX_SFA_SALESINVOICEDETAIL SET OraID = {0} WHERE TriggerID = {1} AND TransactionNumber = '{2}' AND CustomerID = {3} AND OutletID = {4} AND LineNumber = {5}", MaxDetailID, TriggerID, TransactionID, CustomerID, OutletID, LineNo));
                                    incubeQuery.ExecuteNonQuery();
                                }
                                LastLineNo = LineNo;

                                dr = dtTransactionsDetails.Rows[j];
                                dr["ID"] = MaxHeaderID;
                                dr["PUSHED_DATE"] = DateTime.Now;
                                dr["DETAILID"] = MaxDetailID;

                                InsertQuery = "";
                                dataTRSFR.GenerateInsertStatement(DataBaseType.Oracle, dr, "XX_SFA_SALESINVOICEDETAIL", false, DetailsDestColumns, null, ref InsertQuery);
                                result = ExecuteOracleCommand(InsertQuery, true);
                                if (result != Result.Success)
                                {
                                    message = "Error inserting detail line in Oracle!!";
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                                result = Result.Failure;
                                message = "Unhandled exception in inserting detail";
                                break;
                            }
                        }
                        if (result == Result.Success)
                        {
                            message = "Success";
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        message = "Unhandled exception in processing header";
                        result = Result.Failure;
                    }
                    finally
                    {
                        try
                        {
                            if (result == Result.Success)
                            {
                                //commit
                                OracleTrans.Commit();
                                OracleTrans = null;
                                incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                                incubeQuery.ExecuteNonQuery();
                            }
                            else
                            {
                                //rollback
                                if (OracleTrans != null)
                                {
                                    OracleTrans.Rollback();
                                    OracleTrans = null;
                                }
                            }
                            if (OracleConn != null && OracleConn.State == ConnectionState.Open)
                            {
                                OracleConn.Close();
                            }
                        }
                        catch
                        {
                            if (result == Result.Success)
                            {
                                message = "Error committing transaction";
                            }
                            else
                            {
                                message = "Error rolling transaction back";
                            }
                            result = Result.Failure;
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
                DataRow dr;
                Result result = Result.UnKnown;
                Result MainResult = Result.UnKnown;
                DataTransferManager dataTRSFR = new DataTransferManager(false);
                Dictionary<string, ColumnType> DestColumns = null;
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
                string PaymnetsQuery = string.Format(@"SELECT * FROM XX_Sfa_Collections WHERE TriggerID = {0}", TriggerID);
                incubeQuery = new InCubeQuery(db_vms, PaymnetsQuery);
                DataTable dtPayments = new DataTable();
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
                    else
                    {
                        WriteMessage(dtPayments.Rows.Count + " payments(s) found");
                        if (!dtPayments.Columns.Contains("IsProcessed"))
                            dtPayments.Columns.Add("IsProcessed", typeof(int));
                        if (!dtPayments.Columns.Contains("Bonus"))
                            dtPayments.Columns.Add("Bonus", typeof(int));
                        if (!dtPayments.Columns.Contains("ERPMESSAGE"))
                            dtPayments.Columns.Add("ERPMESSAGE", typeof(string));
                        if (!dtPayments.Columns.Contains("MIS_REC_STATUS"))
                            dtPayments.Columns.Add("MIS_REC_STATUS", typeof(string));
                        if (!dtPayments.Columns.Contains("CC_EXPIRY"))
                            dtPayments.Columns.Add("CC_EXPIRY", typeof(string));
                        if (!dtPayments.Columns.Contains("PUSHED_DATE"))
                            dtPayments.Columns.Add("PUSHED_DATE", typeof(DateTime));
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
                        CustomerID = Convert.ToInt32(dtPayments.Rows[i]["CustomerID"]);
                        OutletID = Convert.ToInt32(dtPayments.Rows[i]["OutletID"]);
                        MainType = Convert.ToInt32(dtPayments.Rows[i]["MainType"]);
                        WriteMessage("\r\nSending payment " + PaymentNumber + " For " + InvoiceNumber + ": ");
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
                        incubeQuery = new InCubeQuery(db_vms, string.Format(@"UPDATE XX_Sfa_Collections SET ProcessID = {0} WHERE TriggerID = {1} AND PaymentNumber = '{2}' AND InvoiceNumber = '{3}' AND CustomerID = {4} AND OutletID = {5}"
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

                        if (DestColumns == null)
                        {
                            DataTable dtOraHeader = new DataTable();
                            result = GetOracleDataTable("SELECT * FROM XX_Sfa_Collections WHERE ROWNUM = 1", ref dtOraHeader);
                            if (result != Result.Success)
                            {
                                message = "Error reading Oracle table schema";
                                continue;
                            }
                            dataTRSFR.PrepareDestinationColumns(dtPayments.Columns, dtOraHeader.Columns, ref DestColumns);
                        }

                        dr = dtPayments.Rows[i];
                        dr["IsProcessed"] = 0;
                        dr["Bonus"] = 1;
                        dr["ERPMESSAGE"] = "No Message";
                        dr["PUSHED_DATE"] = DateTime.Now;
                        dr["MIS_REC_STATUS"] = "-";
                        dr["CC_EXPIRY"] = "-";

                        string InsertQuery = "";
                        dataTRSFR.GenerateInsertStatement(DataBaseType.Oracle, dr, "XX_SFA_COLLECTIONS", false, DestColumns, null, ref InsertQuery);

                        if (OracleConn.State != ConnectionState.Open)
                            OracleConn.Open();

                        result = ExecuteOracleCommand(InsertQuery, false);
                        if (result != Result.Success)
                        {
                            result = Result.Invalid;
                            message = "Error inserting line in Oracle!!";
                            continue;
                        }

                        if (result == Result.Success)
                        {
                            message = "Success";
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
                        if (processID > 0)
                            execManager.LogIntegrationEnding(processID, result, "", result == Result.Invalid ? LastQueryError : message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private Result GetOracleDataTable(string SelectQuery, ref DataTable dtData)
        {
            try
            {
                OracleADP = new OracleDataAdapter(SelectQuery, OracleConn);
                OracleADP.SelectCommand.CommandTimeout = 1800;
                dtData = new DataTable();
                OracleADP.Fill(dtData);
                return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\nQuery:\r\n" + SelectQuery, LoggingType.Error, LoggingFiles.InCubeLog);
                LastQueryError = ex.Message;
                return Result.Failure;
            }
            finally
            {
                if (OracleConn != null && OracleConn.State == ConnectionState.Open)
                {
                    OracleConn.Close();
                }
            }
        }
        private Result GetOracleScalar(string Query, bool WithinTrans, ref object Value)
        {
            try
            {
                if (OracleConn.State != ConnectionState.Open)
                    OracleConn.Open();

                OracleCMD = new OracleCommand(Query, OracleConn);
                OracleCMD.CommandType = CommandType.Text;
                OracleCMD.CommandTimeout = 1800;
                if (WithinTrans)
                    OracleCMD.Transaction = OracleTrans;
                Value = OracleCMD.ExecuteScalar();

                return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\nQuery:\r\n" + Query, LoggingType.Error, LoggingFiles.InCubeLog);
                LastQueryError = ex.Message;
                return Result.Failure;
            }
            finally
            {
                if (!WithinTrans && OracleConn != null && OracleConn.State == ConnectionState.Open)
                {
                    OracleConn.Close();
                }
            }
        }
        private Result ExecuteOracleCommand(string Query, bool WithinTrans)
        {
            try
            {
                if (OracleConn.State != ConnectionState.Open)
                    OracleConn.Open();

                OracleCMD = new OracleCommand(Query, OracleConn);
                OracleCMD.CommandType = CommandType.Text;
                OracleCMD.CommandTimeout = 1800;
                if (WithinTrans)
                   OracleCMD.Transaction = OracleTrans;

                OracleCMD.ExecuteNonQuery();

                return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\nQuery:\r\n" + Query, LoggingType.Error, LoggingFiles.InCubeLog);
                LastQueryError = ex.Message;
                return Result.Failure;
            }
            finally
            {
                if (!WithinTrans && OracleConn != null && OracleConn.State == ConnectionState.Open)
                {
                    OracleConn.Close();
                }
            }
        }
        public override void Close()
        {
            if (OracleConn != null && OracleConn.State == ConnectionState.Open)
            {
                OracleConn.Close();
            }
            if (OracleConn != null)
                OracleConn.Dispose();
            if (OracleCMD != null)
                OracleCMD.Dispose();
            if (OracleADP != null)
                OracleADP.Dispose();
            if (OracleTrans != null)
                OracleTrans.Dispose();
            if (OracleReader != null)
                OracleReader.Dispose();
        }

    }
}