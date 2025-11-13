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
    public class IntegrationDelmon : IntegrationBase
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
        public IntegrationDelmon(long CurrentUserID, ExecutionManager ExecManager)
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
            Initialized = false;

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

                foreach (DataRow dr in dtStaging.Rows)
                {
                    string OracleTable = dr["OracleTable"].ToString();
                    string OracleQuery = dr["OracleQuery"].ToString().Trim();
                    if (OracleQuery == string.Empty)
                    {
                        OracleQuery = "SELECT * FROM " + OracleTable;
                    }
                    string StagingTable = dr["StagingTable"].ToString();

                    string result = "";
                    res = SaveTable(OracleQuery, StagingTable, ref rowsCount, ref result);
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
                if (res == Result.Success)
                {
                    Initialized = true;
                    WriteMessage("\r\nProcessing with SQL procedures ..");
                }
                else
                {
                    WriteMessage("\r\nProcess terminated!!");
                }
                execManager.LogIntegrationEnding(ProcessID, res, "", res == Result.Success ? "Rows retrieved: " + rowsCount : "Incomplete data reading, rows: " + rowsCount);
                WriteMessage("\r\n");
            }
        }
        public override void UpdateItem()
        {
            try
            {
                GetMasterData();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private Result SaveTable(string OracleQuery, string TableName, ref int RowsCount, ref string Message)
        {
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
                    Message = "Error in opening Oracle data reader";
                    return Result.Failure;
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
                                Message = "Error writing to staging table";
                                return Result.Failure;
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
                            Message = "Error writing to staging table";
                            return Result.Failure;
                        }
                        dtData.Rows.Clear();
                        count = 0;
                    }
                    Message = "Success";
                    return Result.Success;
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                    Message = "Error writing to staging table";
                    return Result.Failure;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                Message = ex.Message;
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