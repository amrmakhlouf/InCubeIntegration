using InCubeIntegration_BL.BRF_SAP_WS;
using InCubeIntegration_DAL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Xml;


namespace InCubeIntegration_BL
{
    public class IntegrationBRF : IntegrationBase
    {
        private enum WS_Event
        {
            IN,
            OUT
        }
        private enum Configuration
        {
            NumberOfDigits,
            ImportVNI,
            SendBagNumberAndDeviceLocation,
            AllowPayingExtraAtCollection
        }
        string LastSavedFile = "";
        InCubeQuery inCubeQuery = null;
        QueryBuilder QueryBuilderObject = new QueryBuilder();
        InCubeErrors err;
        private long UserID;
        Dictionary<Configuration, Dictionary<int, int>> CachedConfigurations = new Dictionary<Configuration, Dictionary<int, int>>();
        ITF_O_S_EnvRecArqPalmService SAP_WS;
        DTP_EnvRecArqPalm_Req req;
        DTP_EnvRecArqPalm_Resp resp;
        System.Diagnostics.Stopwatch CallStopWatch = new System.Diagnostics.Stopwatch();

        private void CallWS()
        {
            //Connection initialization
            ITF_O_S_EnvRecArqPalmService SAP_WS;
            SAP_WS = new ITF_O_S_EnvRecArqPalmService();
            SAP_WS.Url = "URL";
            SAP_WS.UseDefaultCredentials = false;
            SAP_WS.Credentials = new NetworkCredential("UserName", "Password");

            //Fill Requset
            DTP_EnvRecArqPalm_Req req;
            req = new DTP_EnvRecArqPalm_Req();
            req.CaixaPostal = "E331";
            req.Evento = "IN";
            req.NumLinhas = "";
            req.TipoArquivo = "CLI";
            req.Serial = "";
            req.Versao = "";
            req.IMEI = "";
            req.T_Dados_IN = null;

            //Get Response
            DTP_EnvRecArqPalm_Resp resp;
            resp = SAP_WS.ITF_O_S_EnvRecArqPalm(req);
        }
    
        public IntegrationBRF(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            if (UserID != 999)
            {
                UserID = CurrentUserID;

                if (!CoreGeneral.Common.IsTesting)
                {
                    SAP_WS = new ITF_O_S_EnvRecArqPalmService();
                    SAP_WS.Url = CoreGeneral.Common.GeneralConfigurations.WS_URL;
                    SAP_WS.UseDefaultCredentials = false;
                    SAP_WS.Credentials = new NetworkCredential(CoreGeneral.Common.GeneralConfigurations.WS_UserName, CoreGeneral.Common.GeneralConfigurations.WS_Password);
                }
            }

            db_ERP = new InCubeDatabase();
            InCubeErrors err = db_ERP.Open("StagingDB", "InVan");
            if (err != InCubeErrors.Success)
            {
                WriteMessage("Unable to connect to staging database");
                Initialized = false;
                return;
            }
        }
        DTP_EnvRecArqPalm_Resp CallWS(string Mailbox, string FileName, WS_Event Event, DTP_EnvRecArqPalm_ReqT_Dados_IN[] Dados, ref Result res)
        {
            resp = null;
            res = Result.Success;
            try
            {
                CallStopWatch.Restart();

                req = new DTP_EnvRecArqPalm_Req();
                req.CaixaPostal = Mailbox;
                req.Evento = Event.ToString();
                req.NumLinhas = Dados == null ? "" : Dados.Length.ToString();
                req.TipoArquivo = FileName;
                req.Serial = "";
                req.Versao = "";
                req.IMEI = "";
                req.T_Dados_IN = Dados;
                resp = SAP_WS.ITF_O_S_EnvRecArqPalm(req);
                if (Event == WS_Event.IN)
                {
                    if (resp == null || resp.T_Dados_OUT == null)
                        res = Result.NoFileRetreived;
                    else if (resp.T_Dados_OUT.Length < 3)
                        res = Result.NoRowsFound;
                }

                string temp = "";

                if (resp != null && resp.T_Dados_OUT != null)
                {
                    for (int i = 0; i < resp.T_Dados_OUT.Length; i++)
                    {
                        temp += resp.T_Dados_OUT[i].Dados + "\r\n";
                    }
                }

                if (Dados != null)
                {
                    for (int i = 0; i < Dados.Length; i++)
                    {
                        temp += Dados[i].Dados + "\r\n";
                    }
                }

                WriteFile(Mailbox, Event, resp.NomeArq, temp);
            }
            catch (Exception ex)
            {
                res = Result.WebServiceConnectionError;
                WriteMessage("\r\n---------------------------------------------\r\n");
                WriteMessage("- There is an error in connect to web service -\r\n");
                WriteMessage("- Email : " + Mailbox + " - File : " + FileName + " -\r\n");
                WriteMessage("\r\n---------------------------------------------\r\n");

                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                CallStopWatch.Stop();
            }
            return resp;
        }
        public override void UpdateKPI()
        {
            try
            {
                #region(Member Data)
                string record, Salesperson, Email, EmployeeCode;
                #endregion

                DataTable DT_Emp = GetEmployees(Filters.EmployeeID);

                SetProgressMax(DT_Emp.Rows.Count);
                for (int e = 0; e < DT_Emp.Rows.Count; e++)
                {
                    int processID = 0;
                    int TOTALUPDATED = 0;
                    int TOTALINSERTED = 0;
                    Result res = Result.Success;

                    try
                    {
                        Salesperson = DT_Emp.Rows[e][0].ToString();
                        Email = DT_Emp.Rows[e][1].ToString();
                        EmployeeCode = DT_Emp.Rows[e][2].ToString();

                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(2, Salesperson);
                        processID = execManager.LogIntegrationBegining(TriggerID, OrganizationID, filters);

                        ReportProgress(Email);
                        WriteMessage("<< Update History [" + Email + "]>>\r\n");

                        resp = CallWS(Email, "HIST", WS_Event.IN, null, ref res);
                        if (res == Result.NoFileRetreived)
                        {
                            WriteMessage("<<No changes on [" + Email + "] history>>\r\n");
                            continue;
                        }
                        else if (res == Result.Success || res == Result.NoRowsFound)
                        {
                            TOTALUPDATED = resp.T_Dados_OUT.Length - 2;
                            WriteMessage(string.Format("<<{0} Record(s) Found>>\r\n", resp.T_Dados_OUT.Length - 2));
                        }
                        else
                        {
                            continue;
                        }

                        inCubeQuery = new InCubeQuery(db_vms, "sp_DeleteHistoricalData");
                        inCubeQuery.AddParameter("@EmployeeID", Salesperson);
                        err = inCubeQuery.ExecuteStoredProcedure();
                        if (err != InCubeErrors.Success) continue;

                        for (int i = 1; i < resp.T_Dados_OUT.Length - 1; i++)
                        {
                            record = resp.T_Dados_OUT[i].Dados;
                            inCubeQuery = new InCubeQuery(db_vms, "sp_InsertHistoricalData");
                            inCubeQuery.AddParameter("@HIST_Info", record);
                            inCubeQuery.AddParameter("@EmployeeID", Salesperson);
                            err = inCubeQuery.ExecuteStoredProcedure();
                        }

                        inCubeQuery = new InCubeQuery(db_vms, "sp_CreateDummyTransactions");
                        inCubeQuery.AddParameter("@EmployeeID", Salesperson);
                        err = inCubeQuery.ExecuteStoredProcedure();

                        inCubeQuery = new InCubeQuery(db_vms, "sp_CalculateVolumeAchievements");
                        inCubeQuery.AddParameter("@Month", DateTime.Now.Month);
                        inCubeQuery.AddParameter("@Year", DateTime.Now.Year);
                        inCubeQuery.AddParameter("@EmployeeID", Salesperson);
                        err = inCubeQuery.ExecuteStoredProcedure();

                        inCubeQuery = new InCubeQuery(db_vms, "sp_CalculateActiveCustomersAchievements");
                        inCubeQuery.AddParameter("@Month", DateTime.Now.Month);
                        inCubeQuery.AddParameter("@Year", DateTime.Now.Year);
                        inCubeQuery.AddParameter("@EmployeeID", Salesperson);
                        err = inCubeQuery.ExecuteStoredProcedure();

                        inCubeQuery = new InCubeQuery(db_vms, "sp_CalculateRoutePlanAchievements");
                        inCubeQuery.AddParameter("@Day", DateTime.Now.Day);
                        inCubeQuery.AddParameter("@Month", DateTime.Now.Month);
                        inCubeQuery.AddParameter("@Year", DateTime.Now.Year);
                        inCubeQuery.AddParameter("@EmployeeID", Salesperson);
                        err = inCubeQuery.ExecuteStoredProcedure();

                        if (DateTime.Now.Day <= 5)
                        {
                            inCubeQuery = new InCubeQuery(db_vms, "sp_CalculateVolumeAchievements");
                            inCubeQuery.AddParameter("@Month", DateTime.Now.AddMonths(-1).Month);
                            inCubeQuery.AddParameter("@Year", DateTime.Now.AddMonths(-1).Year);
                            inCubeQuery.AddParameter("@EmployeeID", Salesperson);
                            err = inCubeQuery.ExecuteStoredProcedure();

                            inCubeQuery = new InCubeQuery(db_vms, "sp_CalculateActiveCustomersAchievements");
                            inCubeQuery.AddParameter("@Month", DateTime.Now.AddMonths(-1).Month);
                            inCubeQuery.AddParameter("@Year", DateTime.Now.AddMonths(-1).Year);
                            inCubeQuery.AddParameter("@EmployeeID", Salesperson);
                            err = inCubeQuery.ExecuteStoredProcedure();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                    }
                    finally
                    {
                        execManager.LogIntegrationEnding(processID, res, TOTALINSERTED, TOTALUPDATED, LastSavedFile);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public override void UpdateSTP()
        {
            try
            {
                int id = execManager.LogIntegrationBegining(-1, -1, null);

                #region(Member Data)
                DataTable dtMailBoxes, dtOrders, ordersIDs;
                DataRow[] dr;
                string email, OrderNo, OrdersQuery, deleteQuery, dados, EmailsQuery;
                DateTime SendingDate;
                string orderSubStatusCode;
                string orderStatusID = string.Empty;
                string orderSubStatusID = string.Empty;
                #endregion

                EmailsQuery = "SELECT DISTINCT MailBox FROM PendingSTP WHERE SendingDate >= CONVERT(datetime, '" + DateTime.Today.AddDays(-2).ToString("dd/MM/yyyy") + "', 103)";
                inCubeQuery = new InCubeQuery(db_vms, EmailsQuery);
                err = inCubeQuery.Execute();
                if (err == InCubeErrors.Success)
                {
                    dtMailBoxes = inCubeQuery.GetDataTable();
                    if (dtMailBoxes != null && dtMailBoxes.Rows.Count > 0)
                    {
                        WriteMessage("\r\n" + "Updating STP files");
                        for (int e = 0; e < dtMailBoxes.Rows.Count; e++)
                        {
                            email = dtMailBoxes.Rows[e][0].ToString();
                            OrdersQuery = "SELECT OrderNo, SendingDate FROM PendingSTP WHERE MailBox = '" + email + "' AND SendingDate >= CONVERT(dateTime, '" + DateTime.Today.AddDays(-2).ToString("dd/MM/yyyy") + "', 103)";
                            inCubeQuery = new InCubeQuery(db_vms, OrdersQuery);
                            err = inCubeQuery.Execute();
                            if (err == InCubeErrors.Success)
                            {
                                dtOrders = inCubeQuery.GetDataTable();
                                if (dtOrders != null && dtOrders.Rows.Count > 0)
                                {
                                    Result res = Result.Success;
                                    resp = CallWS(email, "STP", WS_Event.IN, null, ref res);
                                    if (resp != null && resp.T_Dados_OUT != null)
                                    {
                                        WriteMessage("<< Update STP [" + email + "]>>\r\n");
                                        dados = string.Empty;
                                        for (int i = resp.T_Dados_OUT.Length - 1; i >= 0; i--)
                                        {
                                            err = InCubeErrors.Success;
                                            dados = resp.T_Dados_OUT[i].Dados;
                                            if (dados.Contains("STP>")) continue;
                                            OrderNo = dados.Substring(0, 5).Trim();
                                            if (OrderNo == string.Empty) continue;

                                            dr = dtOrders.Select(string.Format("OrderNo LIKE '%{0}'", OrderNo));
                                            if (dr.Length >= 1)
                                            {
                                                DateTime.TryParse(dr[0]["SendingDate"].ToString(), out SendingDate);
                                                OrderNo = dr[0]["OrderNo"].ToString();
                                                QueryBuilderObject = new QueryBuilder();
                                                QueryBuilderObject.SetField("MailBox", "'" + email + "'");
                                                QueryBuilderObject.SetField("OrderNo", "'" + OrderNo + "'");
                                                QueryBuilderObject.SetField("Description", "'" + (dados.Length > 200 ? dados.Substring(0, 200).Replace("'", "''") : dados.Replace("'", "''")) + "'");
                                                QueryBuilderObject.SetField("ReadingDate", "getdate()");
                                                QueryBuilderObject.SetDateField("SendingDate", SendingDate);
                                                if (ExistObject("STPDetails", "MailBox", string.Format("MailBox = '{0}' AND OrderNo = '{1}'", email, OrderNo), db_vms) != InCubeErrors.Success)
                                                {
                                                    err = QueryBuilderObject.InsertQueryString("STPDetails", db_vms);
                                                }
                                                else
                                                {
                                                    err = QueryBuilderObject.UpdateQueryString("STPDetails", string.Format("MailBox = '{0}' AND OrderNo = '{1}'", email, OrderNo), db_vms);
                                                }
                                                if (err != InCubeErrors.Success) break;
                                                deleteQuery = string.Format("DELETE FROM PendingSTP WHERE MailBox = '{0}' AND OrderNo = '{1}'", email, OrderNo);
                                                inCubeQuery = new InCubeQuery(db_vms, deleteQuery);
                                                inCubeQuery.ExecuteNonQuery();

                                                dtOrders.Rows.RemoveAt(dtOrders.Rows.IndexOf(dr[0]));

                                                if (dados.Substring(5, 1) == "I")
                                                {
                                                    orderSubStatusCode = dados.Substring(6, 6).TrimEnd(new char[] { ' ', '-' });
                                                    if (orderSubStatusCode == string.Empty)
                                                    {
                                                        orderStatusID = "8";
                                                        orderSubStatusID = "1000";
                                                    }
                                                    else
                                                    {
                                                        EnsureExistanceOfSubStatus(orderSubStatusCode, "8", ref orderStatusID, ref orderSubStatusID);
                                                    }

                                                    inCubeQuery = new InCubeQuery(db_vms, string.Format("SELECT OrderID FROM SalesOrder WHERE EmployeeID = (SELECT EmployeeID FROM Employee WHERE Email = '{0}') AND OrderID LIKE '%{1}'", email, OrderNo));
                                                    if (inCubeQuery.Execute() == InCubeErrors.Success)
                                                    {
                                                        ordersIDs = new DataTable();
                                                        ordersIDs = inCubeQuery.GetDataTable();
                                                        if (ordersIDs.Rows.Count == 1)
                                                        {
                                                            OrderNo = ordersIDs.Rows[0][0].ToString();
                                                            inCubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE SalesOrder SET OrderStatusID = {0}, OrderSubStatusID = {1} WHERE OrderID = '{2}' AND EmployeeID = (SELECT EmployeeID FROM Employee WHERE Email = '{3}')", orderStatusID, orderSubStatusID, OrderNo, email));
                                                            inCubeQuery.ExecuteNonQuery();
                                                        }
                                                    }
                                                }
                                                if (dtOrders.Rows.Count == 0) break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                execManager.LogIntegrationEnding(id);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private DataTable GetEmployees(int EmployeeID)
        {
            DataTable dtEmployees = new DataTable();
            try
            {
                //EmployeeID   0
                //Email        1
                //EmployeeCode 2
                string employeeFilter = "WHERE InActive = 0 AND OrganizationID = " + OrganizationID + " AND (ISNULL(Email,'') <> '')";
                if (EmployeeID != -1)
                {
                    employeeFilter = "WHERE InActive = 0 AND OrganizationID = " + OrganizationID + " AND EmployeeID = " + EmployeeID;
                }

                string QueryStringEmp = @"SELECT     
		            EmployeeID, 
	                Email,
                    EmployeeCode 
            		
	            FROM   Employee " + employeeFilter;

                inCubeQuery = new InCubeQuery(db_vms, QueryStringEmp);
                inCubeQuery.Execute();
                dtEmployees = inCubeQuery.GetDataTable();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return dtEmployees;
        }
        private int ReadConfiguration(Configuration Config, int EmployeeID)
        {
            int ConfigValue = 0;
            try
            {
                string queryString = string.Format(@"SELECT SUM(CAST(ISNULL(CE.KeyValue,ISNULL(CS.KeyValue,ISNULL(CO.KeyValue,C.KeyValue))) AS INT)) KeyValue
FROM Configuration C
LEFT JOIN Configuration CE ON CE.KeyName = C.KeyName AND CE.EmployeeID = {0}
LEFT JOIN ConfigurationOrganization CO ON CO.KeyName = C.KeyName AND CO.OrganizationID = 
(SELECT OrganizationID FROM Employee WHERE EmployeeID = {0})
LEFT JOIN ConfigurationSecurityGroup CS ON CS.KeyName = C.KeyName AND CS.SecurityGroupID IN
(SELECT SecurityGroupID FROM OperatorSecurityGroup WHERE OperatorID = (SELECT OperatorID FROM EmployeeOperator WHERE EmployeeID = {0}))
WHERE C.KeyName = '{1}' AND C.EmployeeID = -1", EmployeeID, Config.ToString());

                inCubeQuery = new InCubeQuery(db_vms, queryString);

                if (inCubeQuery.Execute() == InCubeErrors.Success)
                {
                    DataTable dtConfig = new DataTable();
                    dtConfig = inCubeQuery.GetDataTable();
                    if (dtConfig.Rows.Count > 0)
                    {
                        ConfigValue = Convert.ToInt16(dtConfig.Rows[0][0]) == 0 ? 0 : 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return ConfigValue;
        }
        private int ReadConfiguration(Configuration Config)
        {
            int ConfigValue = 0;
            try
            {
                if (!CachedConfigurations.ContainsKey(Config))
                {
                    CachedConfigurations.Add(Config, new Dictionary<int, int>());
                    inCubeQuery = new InCubeQuery(db_vms, string.Format(@"SELECT O.OrganizationID, ISNULL(CO.KeyValue,(SELECT KeyValue FROM Configuration WHERE KeyName = '{0}' AND EmployeeID = -1)) KeyValue
FROM Organization O
LEFT JOIN ConfigurationOrganization CO ON CO.OrganizationID = O.OrganizationID AND CO.KeyName = '{0}'", Config.ToString()));
                    if (inCubeQuery.Execute() == InCubeErrors.Success)
                    {
                        DataTable dtConfiguration = inCubeQuery.GetDataTable();
                        if (dtConfiguration != null && dtConfiguration.Rows.Count > 0)
                        {
                            foreach (DataRow dr in dtConfiguration.Rows)
                            {
                                if (!CachedConfigurations[Config].ContainsKey(Convert.ToInt16(dr["OrganizationID"])))
                                {
                                    int value = 0;
                                    switch (dr["KeyValue"].ToString().ToLower())
                                    {
                                        case "true":
                                            value = 1;
                                            break;
                                        case "false":
                                            value = 0;
                                            break;
                                        default:
                                            value = int.Parse(dr["KeyValue"].ToString());
                                            break;
                                    }
                                    CachedConfigurations[Config].Add(Convert.ToInt16(dr["OrganizationID"]), value);
                                }
                            }
                        }
                    }
                }

                ConfigValue = CachedConfigurations[Config][OrganizationID];
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return ConfigValue;
        }
        private DateTime GetDateTime(string Data, int StartIndex, int TotalLength, string dateFormat)
        {
            DateTime dateTimeValue = new DateTime(2000, 1, 1);
            try
            {
                string Value_A = Data.Substring(StartIndex, TotalLength);
                int startIndex = -1;
                int length = -1;
                dateFormat = dateFormat.ToLower();

                //Year
                int year = DateTime.Now.Year;
                startIndex = dateFormat.IndexOf('y');
                length = dateFormat.LastIndexOf('y') - startIndex + 1;

                if (startIndex != -1 && length > 0)
                {
                    if (length == 2)
                    {
                        year = int.Parse(Value_A.Substring(startIndex, length)) + 2000;
                    }
                    else if (length == 4)
                    {
                        year = int.Parse(Value_A.Substring(startIndex, length));
                    }
                }

                //Month
                int month = DateTime.Now.Month;
                startIndex = dateFormat.IndexOf('m');
                length = dateFormat.LastIndexOf('m') - startIndex + 1;
                if (startIndex != -1 && length > 0)
                {
                    month = int.Parse(Value_A.Substring(startIndex, length));
                }

                //Day
                int day = DateTime.Now.Day;
                startIndex = dateFormat.IndexOf('d');
                length = dateFormat.LastIndexOf('d') - startIndex + 1;
                if (startIndex != -1 && length > 0)
                {
                    day = int.Parse(Value_A.Substring(startIndex, length));
                }

                dateTimeValue = new DateTime(year, month, day);
            }
            catch
            {
                //Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return dateTimeValue;
        }
        private string GetDecimal(string Data, int startIndex, int TotalLength, int decimals)
        {
            string decimalValue = "".PadLeft(TotalLength, '0');
            try
            {
                if (Data.Length >= startIndex + TotalLength)
                    decimalValue = Data.Substring(startIndex, TotalLength - decimals).Trim() + "." + Data.Substring(startIndex + TotalLength - decimals, decimals).Trim();
            }
            catch
            {
                //Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return decimalValue;
        }
        private string GetStringField(string Data, int startIndex, int Length)
        {
            string stringValue = "";
            try
            {
                if (Data.Length >= startIndex)
                    stringValue = Data.Substring(startIndex, Math.Min(Length, Data.Length - startIndex)).Trim();
            }
            catch
            {
                //Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return stringValue;
        }
        private decimal GetDecimalValue(string Data, int startIndex, int TotalLength, int decimals)
        {
            decimal decimalValue = 0;
            try
            {
                decimalValue = decimal.Parse(Data.Substring(startIndex, TotalLength - decimals).Trim() + "." + Data.Substring(startIndex + TotalLength - decimals, decimals).Trim());
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return decimalValue;
        }
        private string SetDecimal(string UnformattedValue, int TotalLength, int decimals, bool addDecimalPoint)
        {
            string decimalValue = "".PadLeft(TotalLength, '0');
            try
            {
                string integerPart = "".PadLeft(TotalLength - decimals, '0');
                string decimalPart = "".PadLeft(decimals, '0');

                string[] decimalParts = UnformattedValue.Split('.');
                if (decimalParts.Length > 0)
                {
                    if (decimalParts[0].Length > TotalLength - decimals)
                        integerPart = decimalParts[0].Substring(0, TotalLength - decimals);
                    else
                        integerPart = decimalParts[0].PadLeft(TotalLength - decimals, '0');
                }
                if (decimalParts.Length > 1)
                {
                    if (decimalParts[1].Length > decimals)
                        decimalPart = decimalParts[1].Substring(0, decimals);
                    else
                        decimalPart = decimalParts[1].PadRight(decimals, '0');
                }

                decimalValue = integerPart + (addDecimalPoint ? "." : "") + decimalPart;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return decimalValue;
        }
        private void ReadFromExternalFile(string filename, int filterStartIndex, int filterLength, string filterValue)
        {
            try
            {
                string[] contents = File.ReadAllLines(filename);
                resp = new DTP_EnvRecArqPalm_Resp();

                DTP_EnvRecArqPalm_RespT_Dados_OUT fff;
                List<string> filtered = new List<string>();
                for (int i = 0; i < contents.Length; i++)
                {
                    if (filterStartIndex == -1 || i == 0 || i == contents.Length - 1 || (filterStartIndex + filterLength <= contents[i].Length && contents[i].Substring(filterStartIndex, filterLength).Contains(filterValue)))
                    {
                        filtered.Add(contents[i]);
                    }
                }
                resp.T_Dados_OUT = new DTP_EnvRecArqPalm_RespT_Dados_OUT[filtered.Count];
                for (int j = 0; j < filtered.Count; j++)
                {
                    fff = new DTP_EnvRecArqPalm_RespT_Dados_OUT();
                    fff.Dados = filtered[j];
                    resp.T_Dados_OUT[j] = fff;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private Result UpdateMasterDataForMailBox(IntegrationField Field, string Salesperson, string Email, string FileName, string FieldName)
        {
            int processID = 0;
            Result res = Result.Success;

            try
            {
                Dictionary<int, string> filters;

                //Log Get begining
                filters = new Dictionary<int, string>();
                filters.Add(2, Salesperson);
                filters.Add(3, FieldName);
                filters.Add(4, "Get " + FileName);
                processID = execManager.LogIntegrationBegining(TriggerID, OrganizationID, filters);
                //Read file
                WriteMessage(FieldName + "\r\n" + FileName + " file: Reading .. ");
                if (CoreGeneral.Common.IsTesting)
                    ReadFromExternalFile(@"C:\Users\Ahmad Qadomi\Desktop\BRF\vndE274.txt", -1, 0, "");
                else
                    resp = CallWS(Email, FileName, WS_Event.IN, null, ref res);
                //Log Get ending
                execManager.LogIntegrationEnding(processID, res, LastSavedFile, res == Result.Success ? (resp.T_Dados_OUT.Length - 2).ToString() + " rows found .." : res.ToString());

                if (res == Result.Success)
                {
                    WriteMessage((resp.T_Dados_OUT.Length - 2).ToString() + " rows found .. ");
                    //Log Process begining
                    filters = new Dictionary<int, string>();
                    filters.Add(2, Salesperson);
                    filters.Add(3, FieldName);
                    filters.Add(4, "Process " + FileName);
                    processID = execManager.LogIntegrationBegining(TriggerID, OrganizationID, filters);
                    //Start processing
                    Procedure Proc = new Procedure();

                    switch (Field)
                    {
                        case IntegrationField.Item_U:
                            FillItems(Email, int.Parse(Salesperson));
                            Proc.ProcedureName = "sp_UpdateItems";
                            Proc.AddParameter("@EmployeeID", ParamType.Integer, Salesperson);
                            Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID.ToString());
                            Proc.AddParameter("@OrganizationID", ParamType.Integer, OrganizationID);
                            Proc.AddParameter("@ProcessID", ParamType.Integer, processID.ToString());
                            break;
                        case IntegrationField.Price_U:
                            FillPrices(Email, int.Parse(Salesperson));
                            Proc.ProcedureName = "sp_UpdatePrice";
                            Proc.AddParameter("@EmployeeID", ParamType.Integer, Salesperson);
                            Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID.ToString());
                            Proc.AddParameter("@OrganizationID", ParamType.Integer, OrganizationID);
                            Proc.AddParameter("@DigitsCount", ParamType.Integer, ReadConfiguration(Configuration.NumberOfDigits).ToString());
                            Proc.AddParameter("@ProcessID", ParamType.Integer, processID.ToString());
                            break;
                        case IntegrationField.Discount_U:
                            FillDiscounts(Email, int.Parse(Salesperson));
                            Proc.ProcedureName = "sp_UpdateDiscount";
                            Proc.AddParameter("@EmployeeID", ParamType.Integer, Salesperson);
                            Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID.ToString());
                            Proc.AddParameter("@OrganizationID", ParamType.Integer, OrganizationID);
                            Proc.AddParameter("@DigitsCount", ParamType.Integer, ReadConfiguration(Configuration.NumberOfDigits).ToString());
                            Proc.AddParameter("@ProcessID", ParamType.Integer, processID.ToString());
                            break;
                        case IntegrationField.Invoice_U:
                            FillInvoices(Email, int.Parse(Salesperson));
                            Proc.ProcedureName = "sp_UpdateInvoices";
                            Proc.AddParameter("@EmployeeID", ParamType.Integer, Salesperson);
                            Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID.ToString());
                            Proc.AddParameter("@OrganizationID", ParamType.Integer, OrganizationID);
                            Proc.AddParameter("@ProcessID", ParamType.Integer, processID.ToString());
                            break;
                        case IntegrationField.STA_U:
                            FillOrderStaus(Email, int.Parse(Salesperson));
                            Proc.ProcedureName = "sp_UpdateOrderStatus";
                            Proc.AddParameter("@EmployeeID", ParamType.Integer, Salesperson);
                            Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID.ToString());
                            Proc.AddParameter("@OrganizationID", ParamType.Integer, OrganizationID);
                            Proc.AddParameter("@ProcessID", ParamType.Integer, processID.ToString());
                            break;
                        case IntegrationField.Stock_U:
                            FillStock(Email, int.Parse(Salesperson));
                            Proc.ProcedureName = "sp_UpdateStock";
                            Proc.AddParameter("@WarehouseID", ParamType.Integer, Salesperson);
                            Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID.ToString());
                            Proc.AddParameter("@OrganizationID", ParamType.Integer, OrganizationID);
                            Proc.AddParameter("@ProcessID", ParamType.Integer, processID.ToString());
                            break;
                    }

                    WriteMessage("Processing .. ");
                    res = ExecuteStoredProcedure(Proc);
                    WriteMessage(res.ToString());
                    //Log process end is done in the procedure
                }
                else
                {
                    if (res != Result.NoRowsFound)
                        WriteMessage(res.ToString());
                    else
                        WriteMessage("No rows found !!");
                }
                WriteMessage("\r\n");
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                res = Result.Failure;
            }
            return res;
        }
        public override void UpdateRoutes()
        {
            try
            {
                InCubeErrors err;
                object field = new object();

                DataTable DT_Emp = GetEmployees(Filters.EmployeeID);

                for (int e = 0; e < DT_Emp.Rows.Count; e++)
                {
                    int processID = 0;
                    int TOTALUPDATED = 0;
                    int TOTALINSERTED = 0;
                    Result res = Result.Success;

                    try
                    {
                        string Salesperson = DT_Emp.Rows[e][0].ToString();
                        string Code = DT_Emp.Rows[e][2].ToString();
                        string Email = DT_Emp.Rows[e][1].ToString();

                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(2, Salesperson);
                        processID = execManager.LogIntegrationBegining(TriggerID, OrganizationID, filters);

                        resp = CallWS(Email, "ROT", WS_Event.IN, null, ref res);
                        if (res != Result.Success && res != Result.NoRowsFound)
                        {
                            if (res == Result.NoFileRetreived)
                            {
                                WriteMessage("\r\n");
                                WriteMessage("<<<No changes on [" + Email + "] routes>>> ");
                            }
                            continue;
                        }
                        //HERE YOU WILL WRITE THE INVALID ITEMS ON AN EXTERNAL TEXT FILE "InvalidEntry.txt"
                        ClearProgress();
                        SetProgressMax(resp.T_Dados_OUT.Length - 2);

                        inCubeQuery = new InCubeQuery(db_vms, "DELETE FROM RouteCustomer WHERE RouteID IN (SELECT RouteID FROM Route WHERE TerritoryID IN (SELECT TerritoryID FROM EmployeeTerritory WHERE EmployeeID = " + Salesperson + "))");
                        err = inCubeQuery.ExecuteNonQuery();

                        for (int i = 1; i < resp.T_Dados_OUT.Length - 1; i++)
                        {
                            try
                            {

                                ReportProgress("Updating Routes [" + Email + "]");

                                string record = resp.T_Dados_OUT[i].Dados;

                                TOTALUPDATED++;

                                string TerCode = Code;
                                string TerName = Code;
                                string EmpCode = Code;
                                string RouteNumber = Code;
                                string SaleManCode = EmpCode;
                                string CustomerCode = record.Substring(8, 12).Trim();
                                CustomerCode = (int.Parse(CustomerCode)).ToString();

                                if (RouteNumber == string.Empty)
                                    continue;
                                DateTime dt = new DateTime(int.Parse(record.Substring(24, 4).Trim()), int.Parse(record.Substring(22, 2).Trim()), int.Parse(record.Substring(20, 2).Trim()));
                                bool Sat = false, Sun = false, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false;
                                switch (dt.DayOfWeek)
                                {
                                    case DayOfWeek.Friday:
                                        Fri = true;
                                        RouteNumber += "-Fri";
                                        break;
                                    case DayOfWeek.Monday:
                                        Mon = true;
                                        RouteNumber += "-Mon";
                                        break;
                                    case DayOfWeek.Saturday:
                                        Sat = true;
                                        RouteNumber += "-Sat";
                                        break;
                                    case DayOfWeek.Sunday:
                                        Sun = true;
                                        RouteNumber += "-Sun";
                                        break;
                                    case DayOfWeek.Thursday:
                                        Thu = true;
                                        RouteNumber += "-Thu";
                                        break;
                                    case DayOfWeek.Tuesday:
                                        Tue = true;
                                        RouteNumber += "-Tue";
                                        break;
                                    case DayOfWeek.Wednesday:
                                        Wed = true;
                                        RouteNumber += "-Wed";
                                        break;
                                    default:
                                        break;
                                }



                                string RouteID = GetFieldValue("Route", "RouteID", " RouteCode = '" + RouteNumber + "'", db_vms);
                                if (RouteID == string.Empty)
                                {
                                    RouteID = GetFieldValue("Route", "ISNULL(MAX(RouteID),0) + 1", db_vms);
                                }

                                string CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", " CustomerCode = '" + CustomerCode + "'", db_vms);
                                string OutletID = GetFieldValue("CustomerOutlet", "OutletID", " CustomerID = " + CustomerID + "", db_vms);

                                if (OutletID == string.Empty)
                                {
                                    continue;
                                }

                                string EmployeeID = Salesperson;
                                string TerritoryID = "";

                                TerritoryID = GetFieldValue("Territory", "TerritoryID", "TerritoryCode = '" + TerCode + "'", db_vms);
                                if (TerritoryID.Trim() == string.Empty)
                                {
                                    TOTALINSERTED++;

                                    TerritoryID = GetFieldValue("Territory", "ISNULL(MAX(TerritoryID),0) + 1", db_vms);

                                    QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                                    QueryBuilderObject.SetField("OrganizationID", OrganizationID.ToString());
                                    QueryBuilderObject.SetField("TerritoryCode", "'" + TerCode + "'");

                                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                                    QueryBuilderObject.SetField("CreatedDate", "getdate()");

                                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                                    QueryBuilderObject.SetField("UpdatedDate", "getdate()");

                                    QueryBuilderObject.InsertQueryString("Territory", db_vms);
                                }

                                err = ExistObject("Route", "RouteID", "RouteCode = '" + RouteNumber + "'", db_vms);
                                if (err != InCubeErrors.Success)
                                {
                                    DateTime EstimatedStart = DateTime.Parse(DateTime.Now.Date.AddHours(7).ToString());
                                    DateTime EstimatedEnd = DateTime.Parse(DateTime.Now.Date.AddHours(23).ToString());

                                    QueryBuilderObject.SetField("RouteID", RouteID);
                                    QueryBuilderObject.SetField("RouteCode", "'" + RouteNumber + "'");
                                    QueryBuilderObject.SetField("Inactive", "0");
                                    QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                                    QueryBuilderObject.SetField("EstimatedStart", "getdate()");
                                    QueryBuilderObject.SetField("EstimatedEnd", "getdate()");

                                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                                    QueryBuilderObject.SetField("CreatedDate", "getdate()");

                                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                                    QueryBuilderObject.SetField("UpdatedDate", "getdate()");

                                    err = QueryBuilderObject.InsertQueryString("Route", db_vms);
                                }

                                err = ExistObject("RouteLanguage", "RouteID", "RouteID = " + RouteID + "", db_vms);
                                if (err != InCubeErrors.Success)
                                {
                                    QueryBuilderObject.SetField("RouteID", RouteID);
                                    QueryBuilderObject.SetField("LanguageID", "1");
                                    QueryBuilderObject.SetField("Description", "'" + RouteNumber + "'");
                                    err = QueryBuilderObject.InsertQueryString("RouteLanguage", db_vms);


                                }

                                err = ExistObject("TerritoryLanguage", "TerritoryID", "TerritoryID = " + TerritoryID + " AND LanguageID = 1", db_vms);
                                if (err != InCubeErrors.Success)
                                {
                                    QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                                    QueryBuilderObject.SetField("LanguageID", "1");
                                    QueryBuilderObject.SetField("Description", "'" + TerName + "'");
                                    QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);

                                }

                                err = ExistObject("RouteLanguage", "RouteID", "RouteID = " + RouteID + " AND LanguageID = 2", db_vms);
                                if (err != InCubeErrors.Success)
                                {
                                    QueryBuilderObject.SetField("RouteID", RouteID);
                                    QueryBuilderObject.SetField("LanguageID", "2");
                                    QueryBuilderObject.SetField("Description", "'" + RouteNumber + "'");
                                    err = QueryBuilderObject.InsertQueryString("RouteLanguage", db_vms);
                                }

                                err = ExistObject("TerritoryLanguage", "TerritoryID", "TerritoryID = " + TerritoryID + " AND LanguageID = 2", db_vms);
                                if (err != InCubeErrors.Success)
                                {
                                    QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                                    QueryBuilderObject.SetField("LanguageID", "2");
                                    QueryBuilderObject.SetField("Description", "'" + TerName + "'");
                                    QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);
                                }

                                err = ExistObject("RouteCustomer", "RouteID", "RouteID = " + RouteID + " AND CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                                if (err != InCubeErrors.Success)
                                {
                                    TOTALINSERTED++;
                                    QueryBuilderObject.SetField("RouteID", RouteID);
                                    QueryBuilderObject.SetField("CustomerID", CustomerID);
                                    QueryBuilderObject.SetField("OutletID", OutletID);
                                    err = QueryBuilderObject.InsertQueryString("RouteCustomer", db_vms);


                                }

                                err = ExistObject("Employee", "EmployeeID", "EmployeeID = " + EmployeeID, db_vms);
                                if (err == InCubeErrors.Success)
                                {
                                    err = ExistObject("EmployeeTerritory", "EmployeeID", "EmployeeID = " + EmployeeID + " AND TerritoryID = " + TerritoryID, db_vms);
                                    if (err != InCubeErrors.Success)
                                    {
                                        TOTALINSERTED++;
                                        InCubeQuery DeleteRouteCustomerQuery = new InCubeQuery("delete  from EmployeeTerritory where EmployeeID=" + EmployeeID, db_vms);
                                        DeleteRouteCustomerQuery.ExecuteNonQuery(); //Delete old territory
                                        QueryBuilderObject.SetField("EmployeeID", EmployeeID);
                                        QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                                        QueryBuilderObject.InsertQueryString("EmployeeTerritory", db_vms);
                                    }
                                }

                                err = ExistObject("CustOutTerritory", "TerritoryID", "TerritoryID = " + TerritoryID + " AND CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
                                if (err != InCubeErrors.Success)
                                {
                                    TOTALINSERTED++;
                                    QueryBuilderObject.SetField("CustomerID", CustomerID);
                                    QueryBuilderObject.SetField("OutletID", OutletID);
                                    QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                                    QueryBuilderObject.InsertQueryString("CustOutTerritory", db_vms);
                                }

                                err = ExistObject("RouteVisitPattern", "RouteID", "RouteID = " + RouteID, db_vms);
                                if (err != InCubeErrors.Success)
                                {
                                    TOTALINSERTED++;
                                    QueryBuilderObject.SetField("RouteID", RouteID);
                                    QueryBuilderObject.SetField("Week", "1");
                                    if (Sun)
                                        QueryBuilderObject.SetField("Sunday", "1");
                                    else
                                        QueryBuilderObject.SetField("Sunday", "0");
                                    if (Mon)
                                        QueryBuilderObject.SetField("Monday", "1");
                                    else
                                        QueryBuilderObject.SetField("Monday", "0");
                                    if (Tue)
                                        QueryBuilderObject.SetField("Tuesday", "1");
                                    else
                                        QueryBuilderObject.SetField("Tuesday", "0");
                                    if (Wed)
                                        QueryBuilderObject.SetField("Wednesday", "1");
                                    else
                                        QueryBuilderObject.SetField("Wednesday", "0");
                                    if (Thu)
                                        QueryBuilderObject.SetField("Thursday", "1");
                                    else
                                        QueryBuilderObject.SetField("Thursday", "0");
                                    if (Fri)
                                        QueryBuilderObject.SetField("Friday", "1");
                                    else
                                        QueryBuilderObject.SetField("Friday", "0");
                                    if (Sat)
                                        QueryBuilderObject.SetField("Saturday", "1");
                                    else
                                        QueryBuilderObject.SetField("Saturday", "0");

                                    err = QueryBuilderObject.InsertQueryString("RouteVisitPattern", db_vms);
                                }
                            }
                            catch
                            {

                            }
                        }
                        WriteMessage("\r\n");
                        WriteMessage("<<< ROUTE [" + Email + "] >>> Total Inserted = " + TOTALINSERTED);

                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                    }
                    finally
                    {
                        execManager.LogIntegrationEnding(processID, res, TOTALINSERTED, TOTALUPDATED, LastSavedFile);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public override void SendNewCustomers()
        {
            try
            {
                WriteMessage("\r\n" + "Sending New Customer Requests " + DateTime.Now.ToString("HH:mm:ss") + "\r\n");

                DTP_EnvRecArqPalm_ReqT_Dados_IN record;
                List<DTP_EnvRecArqPalm_ReqT_Dados_IN> records;
                string Email = "", RegistrationNo = "", UpdateFlagQuery = "", result = "";
                int SurveyID = 0, RouteHistoryID = 0, ReadingID = 0, EmployeeID = 0;
                string EmpFilter = "";
                int processID = 0;
                Result res;

                //Get Sending Structure
                string SendNewCustomerStrucQry = "SELECT * FROM TBL_NewCustIntStruct ORDER BY Position";
                inCubeQuery = new InCubeQuery(db_vms, SendNewCustomerStrucQry);
                inCubeQuery.Execute();
                DataTable dtSendNewCustomerStruc = inCubeQuery.GetDataTable();

                //Get Survey Readings
                if (Filters.EmployeeID != -1)
                {
                    EmpFilter = "AND H.EmployeeID = " + Filters.EmployeeID;
                }

                string SurveysQuery = string.Format(@"SELECT H.SurveyID,H.EmployeeID,E.Email,H.RouteHistoryID,H.ReadingID,S.Question
, CASE S.FieldTypeID 
WHEN 1 THEN (CASE WHEN I.IntValue IS NULL THEN '' ELSE CAST(I.IntValue AS NVARCHAR(50)) END)
WHEN 2 THEN (CASE WHEN T.StringValue IS NULL THEN '' ELSE T.StringValue END)
WHEN 3 THEN (CASE WHEN D.DateValue IS NULL THEN '' ELSE FORMAT(D.DateValue,'dd-MM-yyyy') END)
WHEN 9 THEN (CASE WHEN G.GPSLatitude IS NULL THEN '' ELSE CAST(G.GPSLatitude AS NVARCHAR(50)) + ',' + CAST(G.GPSLongitude AS NVARCHAR(50)) END)
WHEN 4 THEN (CASE ISNULL(B.BitValue,-1) WHEN -1 THEN '' WHEN 0 THEN 'No' WHEN 1 THEN 'Yes' END)
WHEN 8 THEN (CASE WHEN M.ImagesCount IS NULL THEN '' ELSE CAST(M.ImagesCount AS NVARCHAR(10)) END)
WHEN 5 THEN (CASE WHEN LV.OptionName IS NULL THEN '' ELSE LV.OptionName END) END Answer
FROM SurveyEmployeeHistory H
INNER JOIN Employee E ON E.EmployeeID = H.EmployeeID
INNER JOIN Surveys_V S ON S.SurveyID = H.SurveyID 
LEFT JOIN FieldValueInt I ON I.FieldID = S.FieldID AND I.ReadingID = H.ReadingID 
AND I.RouteHistoryID = H.RouteHistoryID AND S.FieldTypeID = 1 AND S.FieldSubTypeID = 1
LEFT JOIN FieldValueBit B ON B.FieldID = S.FieldID AND B.ReadingID = H.ReadingID 
AND B.RouteHistoryID = H.RouteHistoryID AND S.FieldTypeID = 4
LEFT JOIN FieldValueString T ON T.FieldID = S.FieldID AND T.ReadingID = H.ReadingID 
AND T.RouteHistoryID = H.RouteHistoryID AND S.FieldTypeID = 2
LEFT JOIN FieldValueLOV L ON L.FieldID = S.FieldID AND L.ReadingID = H.ReadingID 
AND L.RouteHistoryID = H.RouteHistoryID AND S.FieldTypeID = 5
LEFT JOIN FieldValueDate D ON D.FieldID = S.FieldID AND D.ReadingID = H.ReadingID 
AND D.RouteHistoryID = H.RouteHistoryID AND S.FieldTypeID = 3
LEFT JOIN FieldValueGPS G ON G.FieldID = S.FieldID AND G.ReadingID = H.ReadingID 
AND G.RouteHistoryID = H.RouteHistoryID AND S.FieldTypeID = 9
LEFT JOIN LOV_V LV ON LV.LOVID = L.LOVID AND LV.LOVOptionID = L.LOVOptionID
LEFT JOIN (SELECT RouteHistoryID, ReadingID, FieldID,COUNT(*) ImagesCount
FROM FieldValueImages
GROUP BY RouteHistoryID, ReadingID, FieldID) M ON M.FieldID = S.FieldID AND M.ReadingID = H.ReadingID
AND M.RouteHistoryID = H.RouteHistoryID AND S.FieldTypeID = 8
WHERE S.SurveyTypeID = 4 AND ISNULL(H.Synchronized,0) = 0 AND E.OrganizationID = {3}
AND H.StartFillingTime >= '{0}' AND H.StartFillingTime < '{1}'
{2}"
                   , Filters.FromDate.Date.ToString("yyyy/MM/dd"), Filters.ToDate.Date.AddDays(1).ToString("yyyy/MM/dd"), EmpFilter, OrganizationID);

                inCubeQuery = new InCubeQuery(db_vms, SurveysQuery);
                inCubeQuery.Execute();
                DataTable dtSurveyReadings = inCubeQuery.GetDataTable();

                //Get Header Readings
                DataTable dtDistinctReadings = new DataTable();
                dtSurveyReadings.DefaultView.RowFilter = "Question = 'Registration No'";
                dtDistinctReadings = dtSurveyReadings.DefaultView.ToTable(true, new string[] { "Answer", "SurveyID", "RouteHistoryID", "ReadingID", "EmployeeID", "Email" });

                //Set progress max
                execManager.UpdateActionInputOutput(TriggerID, SurveysQuery, dtDistinctReadings.Rows.Count);
                ClearProgress();
                SetProgressMax(dtDistinctReadings.Rows.Count);

                //Loop through distinct readings
                for (int k = 0; k < dtDistinctReadings.Rows.Count; k++)
                {
                    LastSavedFile = "";
                    result = "";
                    res = Result.UnKnown;
                    try
                    {
                        ///Get header information
                        RegistrationNo = dtDistinctReadings.Rows[k]["Answer"].ToString().Trim();
                        Email = dtDistinctReadings.Rows[k]["Email"].ToString().Trim();
                        WriteMessage(Email + "[" + RegistrationNo + "]: ");
                        ReportProgress();

                        SurveyID = Convert.ToInt32(dtDistinctReadings.Rows[k]["SurveyID"]);
                        RouteHistoryID = Convert.ToInt32(dtDistinctReadings.Rows[k]["RouteHistoryID"]);
                        ReadingID = Convert.ToInt32(dtDistinctReadings.Rows[k]["ReadingID"]);
                        EmployeeID = Convert.ToInt32(dtDistinctReadings.Rows[k]["EmployeeID"]);

                        //Log sending details
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, EmployeeID.ToString());
                        filters.Add(9, RegistrationNo);
                        processID = execManager.LogIntegrationBegining(TriggerID, OrganizationID, filters);

                        UpdateFlagQuery = string.Format("UPDATE SurveyEmployeeHistory SET Synchronized = 1 WHERE SurveyID = {0} AND RouteHistoryID = {1} AND EmployeeID = {2} AND ReadingID = {3}", SurveyID, RouteHistoryID, EmployeeID, ReadingID);

                        //Check actual sending status to avoid duplications
                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.NewCustomer_S, new List<string>(filters.Values), processID, 300);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            res = Result.Duplicate;
                            inCubeQuery = new InCubeQuery(db_vms, UpdateFlagQuery);
                            inCubeQuery.ExecuteNonQuery();
                            throw (new Exception("New customer request already sent !!"));
                        }
                        else if (lastRes == Result.Started)
                        {
                            res = Result.Blocked;
                            throw (new Exception("Sending is in progress with another process !!"));
                        }

                        //Prepare for sending
                        records = new List<DTP_EnvRecArqPalm_ReqT_Dados_IN>();
                        record = new DTP_EnvRecArqPalm_ReqT_Dados_IN();
                        record.Dados = "";

                        //Loop through structure and fill Dados
                        for (int i = 0; i < dtSendNewCustomerStruc.Rows.Count; i++)
                        {
                            string value = "";
                            string answer = "";
                            string fieldName = dtSendNewCustomerStruc.Rows[i]["FieldName"].ToString();
                            int Length = Convert.ToInt16(dtSendNewCustomerStruc.Rows[i]["Length"]);
                            int Type = Convert.ToInt16(dtSendNewCustomerStruc.Rows[i]["Type"]);
                            int Precision = Convert.ToInt16(dtSendNewCustomerStruc.Rows[i]["Precision"]);
                            string DateFormat = dtSendNewCustomerStruc.Rows[i]["DateFormat"].ToString();
                            string ConstantValue = dtSendNewCustomerStruc.Rows[i]["ConstantValue"].ToString();

                            //Format data
                            if (Type == 4)
                            {
                                value = ConstantValue;
                            }
                            else
                            {
                                DataRow[] dr = dtSurveyReadings.Select(string.Format("EmployeeID = {0} AND RouteHistoryID = {1} AND SurveyID = {2} AND ReadingID = {3} AND Question = '{4}'"
                                    , EmployeeID, RouteHistoryID, SurveyID, ReadingID, fieldName));
                                if (dr.Length > 0)
                                {
                                    answer = dr[0]["Answer"].ToString().Replace("\n"," ");
                                    switch (Type)
                                    {
                                        case 1:
                                            //String
                                            value = answer;
                                            break;
                                        case 2:
                                            //Date
                                            DateTime dateValue = new DateTime(2000, 1, 1);
                                            DateTime.TryParse(answer, out dateValue);
                                            value = dateValue.ToString(DateFormat);
                                            break;
                                        case 3:
                                            //Decimal
                                            value = SetDecimal(answer, Length, Precision, false);
                                            break;
                                    }
                                }
                            }
                            record.Dados += value.PadRight(Length).Substring(0, Length);
                        }
                        record.Dados += "*";
                        records.Add(record);

                        //Send to SAP
                        if (records.Count > 0)
                        {
                            resp = CallWS(Email.ToString(), "CUS", WS_Event.OUT, records.ToArray(), ref res);
                            if (resp != null && resp.Erro.Trim() == "")
                            {
                                res = Result.Success;
                                result = "Success";
                            }
                            else
                            {
                                res = Result.WebServiceConnectionError;
                                result = "Web Service Connection Error";
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "Failed in sending order, SAP error code is: " + ((resp != null && resp.Erro != null) ? resp.Erro.Trim() : "null"), LoggingType.Error, LoggingFiles.InCubeLog);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (res == Result.UnKnown)
                            res = Result.Failure;
                        else if (res != Result.Duplicate && res != Result.Blocked)
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        result = ex.Message;
                    }
                    finally
                    {
                        if (res == Result.Success || res == Result.Duplicate)
                        {
                            inCubeQuery = new InCubeQuery(db_vms, UpdateFlagQuery);
                            err = inCubeQuery.ExecuteNonQuery();
                        }
                        execManager.LogIntegrationEnding(processID, res, CallStopWatch.ElapsedMilliseconds, LastSavedFile, result);
                        WriteMessage(result + "\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public override void SendOrders()
        {
            try
            {
                WriteMessage("\r\n" + "Sending Orders " + DateTime.Now.ToString("HH:mm:ss") + "\r\n");

                DTP_EnvRecArqPalm_ReqT_Dados_IN record;
                List<DTP_EnvRecArqPalm_ReqT_Dados_IN> records;
                DateTime DeliveryDate, OrderDate;
                string OrderID = "", Email = "", CustomerCode = "", UpdateFlagQuery = "", LPO = "", Note = "", result = "";
                int CustomerID = 0, OutletID = 0;
                string EmpFilter = "";
                int processID = 0;
                Result res;

                if (Filters.EmployeeID != -1)
                {
                    EmpFilter = "AND SO.EmployeeID = " + Filters.EmployeeID;
                }

                string AllOrdersQuery = string.Format(@"SELECT E.Email, SO.OrderID, CO.CustomerID, CO.OutletID, SO.DesiredDeliveryDate, CO.CustomerCode
, SO.OrderDate, SO.LPO, SON.Note
FROM SalesOrder SO
INNER JOIN CustomerOutlet CO ON CO.CustomerID = SO.CustomerID AND CO.OutletID = SO.OutletID
INNER JOIN Employee E ON E.EmployeeID = SO.EmployeeID
INNER JOIN SalesOrderNote SON ON SON.OrderID = SO.OrderID AND SON.CustomerID = SO.CustomerID AND SON.OutletID = SO.OutletID
WHERE SO.Synchronized = 0 AND OrderTypeID = 1
AND SO.OrderDate >= '{0}' AND SO.OrderDate < '{1}'
AND SO.OrganizationID = {3}
{2}
ORDER BY E.Email, SO.OrderID"
                   , Filters.FromDate.Date.ToString("yyyy/MM/dd"), Filters.ToDate.Date.AddDays(1).ToString("yyyy/MM/dd"), EmpFilter, OrganizationID);

                inCubeQuery = new InCubeQuery(db_vms, AllOrdersQuery);
                inCubeQuery.Execute();
                DataTable dtOrders = inCubeQuery.GetDataTable();
                execManager.UpdateActionInputOutput(TriggerID, AllOrdersQuery, dtOrders.Rows.Count);
                ClearProgress();
                SetProgressMax(dtOrders.Rows.Count);

                for (int k = 0; k < dtOrders.Rows.Count; k++)
                {
                    LastSavedFile = "";
                    result = "";
                    res = Result.UnKnown;
                    try
                    {
                        OrderID = dtOrders.Rows[k]["OrderID"].ToString().Trim();
                        //Logger.WriteLog(OrderID, "", "Loop entered", LoggingType.Information, LoggingFiles.errorOrd);
                        CustomerID = Convert.ToInt32(dtOrders.Rows[k]["CustomerID"]);
                        OutletID = Convert.ToInt16(dtOrders.Rows[k]["OutletID"]);
                        Email = dtOrders.Rows[k]["Email"].ToString().Trim();

                        ReportProgress();
                        WriteMessage(Email + " [" + OrderID + "]: ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, OrderID);
                        filters.Add(9, CustomerID.ToString());
                        filters.Add(10, OutletID.ToString());
                        //Logger.WriteLog(OrderID, "", "Log integration begining", LoggingType.Information, LoggingFiles.errorOrd);
                        processID = execManager.LogIntegrationBegining(TriggerID, OrganizationID, filters);

                        UpdateFlagQuery = string.Format("UPDATE SalesOrder SET Synchronized = 1 WHERE OrderID = '{0}' AND CustomerID = {1} AND OutletID = {2} AND OrderTypeID = 1", OrderID, CustomerID, OutletID);

                        //Logger.WriteLog(OrderID, "", "Get last result", LoggingType.Information, LoggingFiles.errorOrd);
                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Orders_S, new List<string>(filters.Values), processID, 300);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            res = Result.Duplicate;
                            inCubeQuery = new InCubeQuery(db_vms, UpdateFlagQuery);
                            inCubeQuery.ExecuteNonQuery();
                            throw (new Exception("Order already sent !!"));
                        }
                        else if (lastRes == Result.Started)
                        {
                            res = Result.Blocked;
                            throw (new Exception("Sending is in progress with another process !!"));
                        }

                        DeliveryDate = DateTime.Parse(dtOrders.Rows[k]["DesiredDeliveryDate"].ToString());
                        CustomerCode = dtOrders.Rows[k]["CustomerCode"].ToString();
                        OrderDate = DateTime.Parse(dtOrders.Rows[k]["OrderDate"].ToString());
                        LPO = dtOrders.Rows[k]["LPO"].ToString();
                        if (string.IsNullOrEmpty(LPO.Trim()))
                            LPO = OrderID.ToString();
                        Note = dtOrders.Rows[k]["Note"].ToString();
                        decimal cda = 0;

                        records = new List<DTP_EnvRecArqPalm_ReqT_Dados_IN>();

                        record = new DTP_EnvRecArqPalm_ReqT_Dados_IN();
                        record.Dados = "N0";
                        record.Dados += Email.ToString().PadRight(4);
                        record.Dados += OrderID.ToString().Substring(OrderID.ToString().Length - 5);//Code
                        record.Dados += CustomerCode.ToString().PadLeft(12, '0');//customer code
                        record.Dados += DeliveryDate.Day.ToString().PadLeft(2, '0') + DeliveryDate.Month.ToString().PadLeft(2, '0') + DeliveryDate.Year.ToString().Substring(2, 2);//deliveary order date
                        record.Dados += OrderDate.Day.ToString().PadLeft(2, '0') + OrderDate.Month.ToString().PadLeft(2, '0');//month and day
                        record.Dados += cda.ToString().Split('.')[0].PadLeft(4, '0') + (cda.ToString().Split('.').Length > 1 ? cda.ToString().Split('.')[1].Substring(0, 2).PadRight(2, '0') : "00");//CDA
                        record.Dados += "000000";//Extra
                        record.Dados += "*";
                        records.Add(record);

                        //Logger.WriteLog(OrderID, "", "Get promo details", LoggingType.Information, LoggingFiles.errorOrd);
                        DataTable dtPromoDetails = new DataTable();
                        bool OrderHasPromo = false;
                        string PromoCheckQry = string.Format("SELECT COUNT(*) FROM SalesOrderDetail (nolock) WHERE OrderID = '{0}' AND CustomerID = {1} AND OutletID = {2} AND SalesTransactionTypeID = 4", OrderID, CustomerID, OutletID);
                        inCubeQuery = new InCubeQuery(db_vms, PromoCheckQry);
                        object field = null;
                        inCubeQuery.ExecuteScalar(ref field);
                        if (field != null && field != DBNull.Value)
                        {
                            if (int.Parse(field.ToString()) > 0)
                            {
                                OrderHasPromo = true;
                                inCubeQuery = new InCubeQuery(db_vms, string.Format(@"SELECT DISTINCT PBH.PackID FreePack, PGD.PackID ReqPack, PBH.PromotionID
FROM PromotionBenefitHistory (nolock) PBH
INNER JOIN PromotionOptionDetail (nolock) POD ON PBH.PromotionID = POD.PromotionID
INNER JOIN PackGroupDetail (nolock) PGD ON PGD.PackGroupID = POD.PackGroupID
WHERE POD.PromotionOptionTypeID = 1 AND PBH.TransactionID = '{0}'
AND PBH.CustomerID = {1} AND PBH.OutletID = {2}", OrderID, CustomerID, OutletID));
                                inCubeQuery.Execute();
                                dtPromoDetails = inCubeQuery.GetDataTable();
                                if (dtPromoDetails == null || dtPromoDetails.Rows.Count == 0)
                                {
                                    throw new Exception("Promotion details not available");
                                }
                            }
                        }

                        //Logger.WriteLog(OrderID, "", "Get order details", LoggingType.Information, LoggingFiles.errorOrd);
                        string dtlQryStr = @"SELECT   
                                                    Item.ItemCode, 
                                                    PackTypeLanguage.Description as PackName, 
                                                    SalesOrderDetail.Quantity, 
                                                    SalesOrderDetail.Price,
                                                    SalesOrderDetail.Discount, 
                                                    SalesOrderDetail.ExpiryDate, 
                                                    SalesOrderDetail.ReturnReason,
                                                    SalesOrderDetail.CDADiscount,
                                                    SalesOrderDetail.Sequence,
                                                    StockStatus.StockStatusCode,
                                                    CASE SOT.IsDefault WHEN 1 THEN '' ELSE ISNULL(SOT.TypeCode,'') END SalesOrderTypeCode,
                                                    SalesOrderDetail.SalesTransactionTypeID,SalesOrderDetail.PackID
                                                    FROM SalesOrderDetail (nolock) INNER JOIN
                            		                SalesOrder (nolock) ON SalesOrder.OrderID = SalesOrderDetail.OrderID
                        		                    AND SalesOrder.CustomerID = SalesOrderDetail.CustomerID 
                        		                    AND SalesOrder.OutletID = SalesOrderDetail.OutletID 
                                                    inner join Pack (nolock) ON SalesOrderDetail.PackID = Pack.PackID INNER JOIN
                                                    Item (nolock) ON Pack.ItemID = Item.ItemID Left JOIN
                                                    PackTypeLanguage (nolock) ON Pack.PackTypeID = PackTypeLanguage.PackTypeID
                                                    LEFT JOIN StockStatus (nolock) on SalesOrderDetail.StockStatusID= StockStatus.StockStatusID
                                                    LEFT OUTER JOIN SalesOrderType (nolock) SOT ON SalesOrderDetail.SalesOrderTypeID = SOT.SalesOrderTypeID
                                                    LEFT JOIN Salesorderdetail (nolock) SOD on Salesorderdetail.orderid=sod.orderid and Salesorderdetail.customerid=sod.customerid
                                                    AND Salesorderdetail.outletid=sod.outletid and Salesorderdetail.packid=sod.packid and Salesorderdetail.batchno=sod.batchno
                                                    AND Salesorderdetail.expirydate=sod.expirydate and Salesorderdetail.quantity=sod.quantity
                                                    and sod.salesordertypeid<>-1 and  Salesorderdetail.salesordertypeid=-1
                                                    WHERE 
                                                    (PackTypeLanguage.LanguageID = 1) 
                                                    AND ((sod.packid is null)OR (SalesOrderDetail.SalesTransactionTypeID <> 1) OR (SalesOrder.CreationSource = 1))
                                                    AND SalesOrderDetail.OrderID = '" + OrderID + "' and SalesOrderDetail.CustomerID=" + CustomerID + " and SalesOrderDetail.OutletID=" + OutletID + " order by SalesOrderDetail.SalesTransactionTypeID,SalesOrderDetail.Sequence";

                        InCubeQuery dtlQry = new InCubeQuery(db_vms, dtlQryStr);
                        err = dtlQry.Execute();
                        DataTable detailsList = dtlQry.GetDataTable();

                        int count = detailsList.Rows.Count;
                        if (count == 0)
                        {
                            throw new Exception("No details found");
                        }

                        //Logger.WriteLog(OrderID, "", "Fill details", LoggingType.Information, LoggingFiles.errorOrd);
                        for (int j = 0; j < count; j++)
                        {
                            DataRow salesTxRow = detailsList.Rows[j];
                            string PackID = salesTxRow["PackID"].ToString();
                            decimal price = Math.Round(decimal.Parse(salesTxRow[3].ToString()), 3);
                            decimal totalCDA = Math.Round(decimal.Parse(salesTxRow[7].ToString().Trim().Equals(string.Empty) ? "0" : salesTxRow[7].ToString()), 6);
                            decimal qty = Math.Round(decimal.Parse(salesTxRow[2].ToString()), 3);
                            decimal dis = Math.Round(decimal.Parse(salesTxRow[4].ToString()), 3);
                            decimal itemCDA = 0;
                            if (qty > 0)
                                itemCDA = totalCDA / qty;
                            string salesOrderType = salesTxRow[10].ToString();
                            int salesTransTypeID = int.Parse(salesTxRow["SalesTransactionTypeID"].ToString());
                            if (salesTransTypeID == 4 || salesTransTypeID == 2)
                                salesOrderType = "YFOM";
                            price = price - (price * dis / 100);

                            record = new DTP_EnvRecArqPalm_ReqT_Dados_IN();
                            record.Dados = "N1"; //0 2
                            record.Dados += OrderID.ToString().Substring(OrderID.ToString().Length - 5);//Code 2 5
                            record.Dados += CustomerCode.ToString().PadLeft(12, '0');//customer code 7 12
                            record.Dados += (j + 1).ToString().PadLeft(4, '0');//salesTxRow[8].ToString().PadLeft(4, '0');//sequance 19 4
                            record.Dados += salesTxRow[0].ToString().PadLeft(18, '0');//sequance 23 18
                            record.Dados += salesOrderType.Length > 4 ? salesOrderType.Substring(0, 4) : salesOrderType.PadLeft(4, ' ');//empty 41 4
                            record.Dados += SetDecimal(price.ToString(), 5, ReadConfiguration(Configuration.NumberOfDigits), false);
                            record.Dados += qty.ToString().Split('.')[0].PadLeft(5, '0') + (qty.ToString().Split('.').Length > 1 ? qty.ToString().Split('.')[1].Substring(0, 3).PadRight(3, '0') : "000");//quantity
                            record.Dados += string.Empty.PadLeft(3);// PaymentTerm.ToString().PadLeft(3, '0');//PaymentTerm
                            record.Dados += salesTxRow[1].ToString().PadRight(3);//UOM
                            record.Dados += itemCDA.ToString().Split('.')[0].PadLeft(4, '0') + (itemCDA.ToString().Split('.').Length > 1 ? itemCDA.ToString().Split('.')[1].Substring(0, 2).PadRight(2, '0') : "00");//itemCDA
                            record.Dados += totalCDA.ToString().Split('.')[0].PadLeft(4, '0') + (totalCDA.ToString().Split('.').Length > 1 ? totalCDA.ToString().Split('.')[1].Substring(0, 2).PadRight(2, '0') : "00");//totalCDA
                            record.Dados += "                000000";//Extra

                            string PromoID = "";
                            if (OrderHasPromo)
                            {
                                DataRow[] dr = dtPromoDetails.Select(string.Format("FreePack = {0} OR ReqPack = {0}", PackID));
                                if (dr.Length > 0)
                                {
                                    PromoID = dr[0]["PromotionID"].ToString();
                                }
                            }
                            record.Dados += PromoID.PadLeft(10);//Extra
                            
                            record.Dados += "0000000000000 ";//Extra
                            record.Dados += "*";
                            records.Add(record);
                        }

                        record = new DTP_EnvRecArqPalm_ReqT_Dados_IN();
                        record.Dados = "N2";
                        record.Dados += Email.ToString().PadRight(4);
                        record.Dados += OrderID.ToString().Substring(OrderID.ToString().Length - 5);//Code
                        record.Dados += CustomerCode.ToString().PadLeft(12, '0');//customer code
                        record.Dados += LPO.ToString().Substring(0, Math.Min(15, LPO.ToString().Length)).PadRight(15);//orderID
                        record.Dados += CustomerCode.ToString().PadLeft(12, '0');//customer code
                        record.Dados += "N";
                        record.Dados += "N";
                        record.Dados += string.Empty.PadLeft(4);//PaymentTerm
                        record.Dados += "YA10";
                        record.Dados += "00";
                        record.Dados += "".PadRight(93);
                        record.Dados += "*";
                        records.Add(record);

                        record = new DTP_EnvRecArqPalm_ReqT_Dados_IN();
                        record.Dados = "N3";
                        record.Dados += Email.ToString().PadRight(4);
                        record.Dados += OrderID.ToString().Substring(OrderID.ToString().Length - 5);//Code
                        record.Dados += CustomerCode.ToString().PadLeft(12, '0');//customer code
                        record.Dados += (LPO + "-" + Note).Substring(0, Math.Min((LPO + "-" + Note).Length, 40)).PadRight(40);
                        record.Dados += "".PadRight(92);
                        record.Dados += "*";
                        records.Add(record);

                        //Logger.WriteLog(OrderID, "", "Call service", LoggingType.Information, LoggingFiles.errorOrd);
                        if (records.Count > 0)
                        {
                            resp = CallWS(Email.ToString(), "ORD", WS_Event.OUT, records.ToArray(), ref res);
                            if (resp != null && resp.Erro.Trim() == "")
                            {
                                res = Result.Success;
                                result = "Success";
                            }
                            else
                            {
                                res = Result.WebServiceConnectionError;
                                result = "Web Service Connection Error";
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "Failed in sending order, SAP error code is: " + ((resp != null && resp.Erro != null) ? resp.Erro.Trim() : "null"), LoggingType.Error, LoggingFiles.InCubeLog);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (res == Result.UnKnown)
                            res = Result.Failure;
                        else if (res != Result.Duplicate && res != Result.Blocked)
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        result = ex.Message;
                    }
                    finally
                    {
                        if (res == Result.Success || res == Result.Duplicate)
                        {
                            inCubeQuery = new InCubeQuery(db_vms, UpdateFlagQuery);
                            err = inCubeQuery.ExecuteNonQuery();
                        }
                        //Logger.WriteLog(OrderID, "", "Log integration ending", LoggingType.Information, LoggingFiles.errorOrd);
                        execManager.LogIntegrationEnding(processID, res, CallStopWatch.ElapsedMilliseconds, LastSavedFile, result);
                        //Logger.WriteLog(OrderID, "", "End", LoggingType.Information, LoggingFiles.errorOrd);
                        WriteMessage(result + "\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public override void SendCreditNoteRequest()
        {
            try
            {
                WriteMessage("\r\n" + "Sending Credit Notes " + DateTime.Now.ToString("HH:mm:ss") + "\r\n");

                DTP_EnvRecArqPalm_ReqT_Dados_IN record;
                List<DTP_EnvRecArqPalm_ReqT_Dados_IN> records;

                string EmpFilter = "", Email = "", OrderID = "", CustomerCode = "", CustRefNo = "", InvoiceRefNo = "", ReturnReasonCode = "";
                string ItemCode = "", UOM = "", UpdateFlagQuery = "", result = "";
                int processID = 0, EmployeeID, CustomerID, OutletID;
                decimal Quantity, Value;
                DateTime OrderDate;
                Result res;

                if (Filters.EmployeeID != -1)
                {
                    EmpFilter = "AND SO.EmployeeID = " + Filters.EmployeeID;
                }

                string HeaderQuery = string.Format(@"
SELECT SO.OrderID, SO.CustomerID, SO.OutletID, SO.CustomerRef CustRefNo, CO.CustomerCode, SO.OrderDate, PSL.Description ReturnReasonCode, SO.LPO InvoiceRefNo
, SO.EmployeeID, E.Email              		
FROM SalesOrder SO
INNER JOIN PackStatusLanguage PSL ON PSL.StatusID = (SELECT TOP 1 PackStatusID FROM SalesOrderDetail SOD
WHERE SOD.OrderID = SO.OrderID AND SOD.CustomerID = SO.CustomerID AND SOD.OutletID = SO.OutletID) AND PSL.LanguageID = 1
INNER JOIN CustomerOutlet CO ON CO.CustomerID = SO.CustomerID AND CO.OutletID = SO.OutletID
INNER JOIN Employee E ON E.EmployeeID = SO.EmployeeID
WHERE SO.Synchronized = 0 AND OrderTypeID = 5
AND SO.OrderDate >= '{0}' AND SO.OrderDate < '{1}'
AND SO.OrganizationID = {2} 
{3}
ORDER BY E.Email, SO.OrderID", Filters.FromDate.Date.ToString("yyyy/MM/dd"), Filters.ToDate.Date.AddDays(1).ToString("yyyy/MM/dd"), OrganizationID, EmpFilter);

                inCubeQuery = new InCubeQuery(db_vms, HeaderQuery);
                inCubeQuery.Execute();
                DataTable dtOrders = inCubeQuery.GetDataTable();
                execManager.UpdateActionInputOutput(TriggerID, HeaderQuery, dtOrders.Rows.Count);
                ClearProgress();
                SetProgressMax(dtOrders.Rows.Count);

                for (int i = 0; i < dtOrders.Rows.Count; i++)
                {
                    LastSavedFile = "";
                    result = "";
                    res = Result.UnKnown;
                    try
                    {
                        OrderID = dtOrders.Rows[i]["OrderID"].ToString().Trim();
                        CustomerID = Convert.ToInt32(dtOrders.Rows[i]["CustomerID"]);
                        OutletID = Convert.ToInt16(dtOrders.Rows[i]["OutletID"]);
                        Email = dtOrders.Rows[i]["Email"].ToString().Trim();

                        ReportProgress();
                        WriteMessage(Email + " [" + OrderID + "]: ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, OrderID);
                        filters.Add(9, CustomerID.ToString());
                        filters.Add(10, OutletID.ToString());
                        processID = execManager.LogIntegrationBegining(TriggerID, OrganizationID, filters);

                        UpdateFlagQuery = string.Format("UPDATE SalesOrder SET Synchronized = 1 WHERE OrderID = '{0}' AND CustomerID = {1} AND OutletID = {2} AND OrderTypeID = 5", OrderID, CustomerID, OutletID);

                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.CreditNoteRequest_S, new List<string>(filters.Values), processID, 300);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            res = Result.Duplicate;
                            inCubeQuery = new InCubeQuery(db_vms, UpdateFlagQuery);
                            inCubeQuery.ExecuteNonQuery();
                            throw (new Exception("Transaction already sent !!"));
                        }
                        else if (lastRes == Result.Started)
                        {
                            res = Result.Blocked;
                            throw (new Exception("Sending is in progress with another process !!"));
                        }

                        CustRefNo = dtOrders.Rows[i]["CustRefNo"].ToString().Trim();
                        CustomerCode = dtOrders.Rows[i]["CustomerCode"].ToString().Trim();
                        ReturnReasonCode = dtOrders.Rows[i]["ReturnReasonCode"].ToString().Trim();
                        InvoiceRefNo = dtOrders.Rows[i]["InvoiceRefNo"].ToString().Trim();
                        Email = dtOrders.Rows[i]["Email"].ToString().Trim();
                        EmployeeID = Convert.ToInt16(dtOrders.Rows[i]["EmployeeID"]);
                        OrderDate = Convert.ToDateTime(dtOrders.Rows[i]["OrderDate"]);

                        records = new List<DTP_EnvRecArqPalm_ReqT_Dados_IN>();
                        //Header
                        record = new DTP_EnvRecArqPalm_ReqT_Dados_IN();
                        record.Dados = "F3";
                        record.Dados += Email.PadRight(4).Substring(0, 4);
                        record.Dados += CustRefNo.PadRight(12).Substring(0, 12);
                        record.Dados += "ZCRF";
                        record.Dados += CustomerCode.PadLeft(10, '0').Substring(0, 10);
                        record.Dados += OrderDate.Day.ToString().PadLeft(2, '0') + OrderDate.Month.ToString().PadLeft(2, '0') + OrderDate.Year.ToString().Substring(2, 2);
                        record.Dados += ReturnReasonCode.PadRight(3).Substring(0, 3);
                        record.Dados += OrderID.PadRight(35).Substring(0, 35);
                        record.Dados += InvoiceRefNo.PadRight(16).Substring(0, 16);
                        record.Dados += "*";
                        records.Add(record);

                        string dtlQryStr = string.Format(@"
SELECT I.ItemCode, PTL.Description UOM, ROUND(SOD.Quantity,3) Quantity, ROUND(SOD.Quantity*SOD.Price,{3}) Value
FROM SalesOrderDetail SOD
INNER JOIN Items I ON I.PackID = SOD.PackID
INNER JOIN PackTypeLanguage PTL ON PTL.PackTypeID = SOD.SelectedUOMID AND PTL.LanguageID = 1
WHERE SOD.OrderID = '{0}' AND SOD.CustomerID = {1} AND SOD.OutletID = {2}"
, OrderID, CustomerID, OutletID, ReadConfiguration(Configuration.NumberOfDigits));

                        inCubeQuery = new InCubeQuery(db_vms, dtlQryStr);
                        inCubeQuery.Execute();
                        DataTable dtDetails = inCubeQuery.GetDataTable();

                        if (dtDetails.Rows.Count == 0)
                        {
                            throw new Exception("No details found");
                        }

                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        {
                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            Quantity = Convert.ToDecimal(dtDetails.Rows[j]["Quantity"]);
                            Value = Convert.ToDecimal(dtDetails.Rows[j]["Value"]);
                            UOM = dtDetails.Rows[j]["UOM"].ToString();

                            record = new DTP_EnvRecArqPalm_ReqT_Dados_IN();
                            record.Dados = "F4";
                            record.Dados += Email.PadRight(4).Substring(0, 4);
                            record.Dados += CustRefNo.PadRight(35).Substring(0, 35);
                            record.Dados += ((j + 1) * 100).ToString().PadLeft(6, '0');
                            record.Dados += ItemCode.PadLeft(18, '0').Substring(0, 18);
                            record.Dados += SetDecimal(Value.ToString(), 15, ReadConfiguration(Configuration.NumberOfDigits), false);
                            record.Dados += SetDecimal(Quantity.ToString(), 8, 3, false);
                            record.Dados += UOM.PadRight(3);
                            record.Dados += "*";
                            records.Add(record);
                        }

                        if (records.Count > 0)
                        {
                            resp = CallWS(Email.ToString(), "ORD", WS_Event.OUT, records.ToArray(), ref res);
                            if (resp != null && resp.Erro.Trim() == "")
                            {
                                res = Result.Success;
                                result = "Success";
                            }
                            else
                            {
                                res = Result.WebServiceConnectionError;
                                result = "Web Service Connection Error";
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "Failed in sending order, SAP error code is: " + ((resp != null && resp.Erro != null) ? resp.Erro.Trim() : "null"), LoggingType.Error, LoggingFiles.InCubeLog);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (res == Result.UnKnown)
                            res = Result.Failure;
                        else
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        result = ex.Message;
                    }
                    finally
                    {
                        if (res == Result.Success || res == Result.Duplicate)
                        {
                            inCubeQuery = new InCubeQuery(db_vms, UpdateFlagQuery);
                            err = inCubeQuery.ExecuteNonQuery();
                        }
                        execManager.LogIntegrationEnding(processID, res, CallStopWatch.ElapsedMilliseconds, LastSavedFile, result);
                        WriteMessage(result + "\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        public override void SendATMCollections()
        {
            try
            {
                #region PREREQUISITES
                /* THE FOLLOWING TABLE SHOULD EXIST IN THE DATABASE:
                 * 
                 * 
                 CREATE TABLE [dbo].[ATM_Deposite](
    [DepositID] [Bigint] not nul,
	[SalesmanID] [nvarchar](50) NULL,
	[Currency] [nvarchar](50) NULL,
	[Value] [decimal](18, 2) NULL,
	[DepositeDate] [datetime] NULL,
	[Synchronized] [bit] NULL
) ON [PRIMARY]
                 * *
                 * 
                 * 
                 *THEN CREATE AN ODBC CONNECTION AND NAME IT ATM
                 *FINALLY CREATE A LINKED SERVER AND CALL IT ATM, FOLLOWING IS THE SCRIPT TO CREATE A LINKED SERVER:
                 
                 USE [master]
GO


EXEC master.dbo.sp_addlinkedserver @server = N'ATM', @srvproduct=N'ATM', @provider=N'MSDASQL', @datasrc=N'ATM'

EXEC master.dbo.sp_addlinkedsrvlogin @rmtsrvname=N'ATM',@useself=N'False',@locallogin=NULL,@rmtuser=N'federalfoods',@rmtpassword='########'

GO

EXEC master.dbo.sp_serveroption @server=N'ATM', @optname=N'collation compatible', @optvalue=N'false'
GO

EXEC master.dbo.sp_serveroption @server=N'ATM', @optname=N'data access', @optvalue=N'true'
GO

EXEC master.dbo.sp_serveroption @server=N'ATM', @optname=N'dist', @optvalue=N'false'
GO

EXEC master.dbo.sp_serveroption @server=N'ATM', @optname=N'pub', @optvalue=N'false'
GO

EXEC master.dbo.sp_serveroption @server=N'ATM', @optname=N'rpc', @optvalue=N'false'
GO

EXEC master.dbo.sp_serveroption @server=N'ATM', @optname=N'rpc out', @optvalue=N'false'
GO

EXEC master.dbo.sp_serveroption @server=N'ATM', @optname=N'sub', @optvalue=N'false'
GO

EXEC master.dbo.sp_serveroption @server=N'ATM', @optname=N'connect timeout', @optvalue=N'0'
GO

EXEC master.dbo.sp_serveroption @server=N'ATM', @optname=N'collation name', @optvalue=null
GO

EXEC master.dbo.sp_serveroption @server=N'ATM', @optname=N'lazy schema validation', @optvalue=N'false'
GO

EXEC master.dbo.sp_serveroption @server=N'ATM', @optname=N'query timeout', @optvalue=N'0'
GO

EXEC master.dbo.sp_serveroption @server=N'ATM', @optname=N'use remote collation', @optvalue=N'true'
GO

EXEC master.dbo.sp_serveroption @server=N'ATM', @optname=N'remote proc transaction promotion', @optvalue=N'true'
GO



                 */
                #endregion
                WriteMessage("\r\n" + "Sending ATM deposits .. " + DateTime.Now.ToString("HH:mm:ss"));

                //FillAllATMOnce(FromDate);
                InCubeQuery GetDepositesFromATMQuery;
                DTP_EnvRecArqPalm_ReqT_Dados_IN record;
                List<DTP_EnvRecArqPalm_ReqT_Dados_IN> records;
                object field = new object();

                DateTime _DepositeDate = DateTime.Now;
                string _SalesmanID = "";

                string _Currency = "";
                Int64 _DepositID = 0;
                string bagNumber = "";
                string deviceLocation = "";

                decimal _Value = 0.00m;

                object ChDate = "";

                string DIST_CHANNEL = string.Empty;
                string CN_TRAN = string.Empty;
                string note2 = string.Empty;

                string QueryStringEmp = @"SELECT     
		            EmployeeID,
                    EmployeeCode, 
	                Email 
            		
	            FROM   Employee
            		
	            WHERE (isnull(Email,'')<> '') AND OrganizationID = " + OrganizationID;

                if (Filters.EmployeeID != -1)
                {
                    QueryStringEmp += " AND EmployeeID = " + Filters.EmployeeID;
                }
                inCubeQuery = new InCubeQuery(db_vms, QueryStringEmp);

                inCubeQuery.Execute();
                DataTable DT_Emp = inCubeQuery.GetDataTable();

                string datasourcePath = CoreGeneral.Common.StartupPath + "\\DataSources.xml";
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(datasourcePath);

                XmlNode node = xmlDoc.SelectSingleNode("Connections/Connection[Name='InCube']/Data");
                string SQL_CON_STR = node.InnerText;

                node = xmlDoc.SelectSingleNode("Connections/Connection[Name='ATM']/Data");
                string ODBC_CON_STR = node.InnerText;

                for (int e = 0; e < DT_Emp.Rows.Count; e++)
                {
                    int processID = 0;
                    int TOTALUPDATED = 0;
                    int TOTALINSERTED = 0;
                    Result res = Result.Success;

                    try
                    {
                        string Salesperson = DT_Emp.Rows[e][0].ToString();
                        string Code = DT_Emp.Rows[e][1].ToString();
                        string Email = DT_Emp.Rows[e][2].ToString();

                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(2, Salesperson);
                        processID = execManager.LogIntegrationBegining(TriggerID, OrganizationID, filters);

                        DateTime LastDepositDate = DateTime.Now;
                        string DepositCount = GetFieldValue("ATM_Deposite", "Count(DepositID)", "SalesmanID='" + Code + "'", db_vms).Trim();
                        if (!DepositCount.Equals("0"))
                        {
                            if (!DateTime.TryParse(GetFieldValue("ATM_Deposite", "Top(1) DepositeDate", "SalesmanID='" + Code + "' order by DepositeDate Desc", db_vms).Trim(), out LastDepositDate))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            LastDepositDate = DateTime.Now.AddYears(-2);
                        }
                        //                    string GetDepositesFromATM = string.Format(@"insert into ATM_Deposite 
                        //select * from Openquery([ATM],'SELECT isn_2 DepositID, serial_number as SalesmanID, ''AED'' as Currency, 
                        //amount as Value, trx_date DepositeDate,0 as Synchronized from trx_ref_deposits where trx_date>''{0}'' and serial_number=''{1}''')
                        //					", LastDepositDate.ToString("yyyy-MM-dd HH:mm:ss"),Code);
                        // HERE INSTEAD OF THE LINKED SERVER ABOVE, GET THE DATA DIRECTLY FROM ODBC AND INSERT IT INTO A TEMPORARY TABLE, THEN INSERT INTO ATM_DEPOSIT.
                        #region FILL ATM TABLE FROM ODBC
                        try
                        {
                            int RowsAffected = 0;

                            DataTable ATM_Data_Table = new DataTable();
                            using (var connection =
                       new OdbcConnection(ODBC_CON_STR))
                            {
                                var adapter =
                                    new OdbcDataAdapter(string.Format(@"SELECT isn_2 DepositID, serial_number as SalesmanID, 'AED' as Currency, 
amount as Value, trx_date DepositeDate,0 as Synchronized,bag_number BagNumber,device_location DeviceLocation from trx_ref_deposits where trx_date>'{0}' and serial_number='{1}'
					", LastDepositDate.ToString("yyyy-MM-dd HH:mm:ss"), Code), connection);

                                // Open the connection and fill the DataSet.
                                try
                                {
                                    if (connection.State == ConnectionState.Closed)
                                        connection.Open();
                                    adapter.Fill(ATM_Data_Table);
                                    if (connection.State == ConnectionState.Open) connection.Close();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);

                                }

                            }

                            using (var sqlConn = new SqlConnection(SQL_CON_STR))
                            {
                                using (var command = new SqlCommand("InsertATMTable") { CommandType = CommandType.StoredProcedure })
                                {
                                    command.Parameters.Add(new SqlParameter("@myTableType", ATM_Data_Table));
                                    command.Connection = sqlConn;
                                    if (sqlConn.State == ConnectionState.Closed)
                                        sqlConn.Open();
                                    RowsAffected = command.ExecuteNonQuery();
                                    if (sqlConn.State == ConnectionState.Open) sqlConn.Close();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                        }
                        #endregion

                        //GetDepositesFromATMQuery = new InCubeQuery(db_vms, GetDepositesFromATM);
                        //err = GetDepositesFromATMQuery.ExecuteNonQuery();
                        //if (err == InCubeErrors.Success)
                        //{
                        //    WriteOrderExceptions("Inserting ATM Deposits for salesman(" + Code + ") into table ATM_Deposite has succseeded", "ATM SENDING", "ATM");
                        //}
                        //else
                        //{
                        //    WriteOrderExceptions("Inserting ATM Deposits for salesman(" + Code + ") into table ATM_Deposite has Failed !", "ATM SENDING", "ATM");
                        //}
                        string GetDepositesFromATM = string.Format(@"select * from ATM_Deposite where Synchronized=0 and SalesmanID='{0}'
					", Code);
                        GetDepositesFromATMQuery = new InCubeQuery(db_vms, GetDepositesFromATM);
                        records = new List<DTP_EnvRecArqPalm_ReqT_Dados_IN>();
                        err = GetDepositesFromATMQuery.Execute();
                        DataTable DT = new DataTable();
                        DT = GetDepositesFromATMQuery.GetDataTable();
                        err = GetDepositesFromATMQuery.FindFirst();
                        List<string> payments = new List<string>();

                        ClearProgress();
                        SetProgressMax(DT.Rows.Count);
                        while (err == InCubeErrors.Success)
                        {
                            try
                            {
                                ReportProgress("Sending Deposits for employee" + " " + Email);

                                err = GetDepositesFromATMQuery.GetField("DepositID", ref field);
                                _DepositID = Convert.ToInt64(field.ToString());
                                err = GetDepositesFromATMQuery.GetField("SalesmanID", ref field);
                                _SalesmanID = field.ToString();
                                err = GetDepositesFromATMQuery.GetField("Value", ref field);
                                _Value = decimal.Parse(field.ToString());
                                err = GetDepositesFromATMQuery.GetField("DepositeDate", ref field);
                                ChDate = field;
                                if (!ChDate.ToString().Trim().Equals(string.Empty))
                                {
                                    _DepositeDate = DateTime.Parse(field.ToString());
                                }
                                err = GetDepositesFromATMQuery.GetField("Currency", ref field);
                                _Currency = field.ToString();
                                err = GetDepositesFromATMQuery.GetField("BagNumber", ref field);
                                bagNumber = field.ToString();
                                err = GetDepositesFromATMQuery.GetField("DeviceLocation", ref field);
                                deviceLocation = field.ToString();
                                record = new DTP_EnvRecArqPalm_ReqT_Dados_IN();
                                record.Dados = "";
                                record.Dados += _SalesmanID.Trim().Substring(0, Math.Min(10, _SalesmanID.Length)).PadLeft(10);//SP_GL_IND 13,1
                                record.Dados += _Currency.Trim().Substring(0, Math.Min(3, _Currency.Length)).PadLeft(3);
                                string Value = ((int)_Value).ToString();
                                string val8 = Value;
                                string val2 = "00";
                                if (Value.Contains("."))
                                {
                                    val8 = Value.Split(new char[] { '.' })[0];
                                    val2 = Value.Split(new char[] { '.' })[1];
                                }
                                val8 = val8.Substring(0, Math.Min(8, val8.Length)).PadLeft(8);
                                val2 = val2.Substring(0, Math.Min(2, val2.Length)).PadRight(2, '0');
                                record.Dados += val8 + val2;
                                record.Dados += _DepositeDate.ToString("yyyyMMdd");//PAY_TYPE 32,1
                                if (ReadConfiguration(Configuration.SendBagNumberAndDeviceLocation) == 1)
                                {
                                    record.Dados += bagNumber.Substring(0, Math.Min(bagNumber.Length, 20)).PadLeft(20);
                                    record.Dados += deviceLocation.Substring(0, Math.Min(deviceLocation.Length, 80)).PadLeft(80);
                                }
                                //record.Dados += _DepositID.ToString().Substring(0, Math.Min(10, _DepositID.ToString().Length)).PadLeft(10);
                                record.Dados += "*";
                                records.Add(record);
                                payments.Add(_DepositID.ToString());
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                            }
                            err = GetDepositesFromATMQuery.FindNext();
                        }
                        if (records.Count > 0)
                        {
                            DTP_EnvRecArqPalm_Resp req = CallWS(Email, "DEP", WS_Event.OUT, records.ToArray(), ref res);
                            if (resp != null && payments.Count > 0)
                            {
                                string DepositStr = "";
                                foreach (string DepositPair in payments)
                                    DepositStr += DepositPair + ",";
                                if (req.Erro.Trim() == "")
                                {
                                    InCubeQuery UpdateQuery = new InCubeQuery(db_vms, "update  ATM_Deposite set Synchronized=1 where DepositID in ( " + DepositStr.Substring(0, DepositStr.Length - 1) + ")");
                                    err = UpdateQuery.Execute();
                                    if (err == InCubeErrors.Success)
                                    {
                                    }
                                    else
                                    {
                                    }
                                    WriteMessage("\r\n" + DT.Rows.Count + " deposits were sent for [" + Email + "]\r\n");
                                }

                                else
                                {
                                    WriteMessage("\r\n" + "failed to send Deposits for [" + Email + "]\r\n");
                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                    }
                    finally
                    {
                        execManager.LogIntegrationEnding(processID, res, TOTALINSERTED, TOTALUPDATED, LastSavedFile);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void EnsureExistanceOfSubStatus(string orderSubStatusCode, string MainStatusIDToBeAddedTo, ref string orderStatusID, ref string orderSubStatusID)
        {
            try
            {
                string statusColor = "";
                orderStatusID = "";
                orderSubStatusID = "";
                string statusIDs = GetFieldValue("OrderSubStatus", "CAST(OrderStatusID AS nvarchar(10)) + ':' + CAST(OrderSubStatusID AS nvarchar(10))", "OrderSubStatusCode = '" + orderSubStatusCode + "'", db_vms);
                if (statusIDs != string.Empty)
                {
                    string[] statusIDsArr = statusIDs.Split(new char[] { ':' });
                    if (statusIDsArr.Length == 2)
                    {
                        orderStatusID = statusIDsArr[0];
                        orderSubStatusID = statusIDsArr[1];
                    }
                }
                if (MainStatusIDToBeAddedTo == "8" && orderStatusID != "8")
                {
                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetField("OrderStatusID", "8");
                    QueryBuilderObject.UpdateQueryString("OrderSubStatus", "OrderSubStatusCode = '" + orderSubStatusCode + "'", db_vms);
                    orderStatusID = "8";
                }

                if (orderStatusID == "" && orderSubStatusID == "")
                {
                    orderStatusID = MainStatusIDToBeAddedTo;
                    switch (orderStatusID)
                    {
                        case "1":
                            orderSubStatusID = GetFieldValue("OrderSubStatus", "ISNULL(MIN(OrderSubStatusID),-10) - 1", "OrderStatusID = " + orderStatusID, db_vms);
                            statusColor = "3";
                            break;
                        case "8":
                            orderSubStatusID = GetFieldValue("OrderSubStatus", "ISNULL(MAX(OrderSubStatusID),1000) + 1", "OrderStatusID = " + orderStatusID, db_vms);
                            statusColor = "1";
                            break;
                    }
                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetField("OrderSubStatusID", orderSubStatusID);
                    QueryBuilderObject.SetField("OrderStatusID", orderStatusID);
                    QueryBuilderObject.SetField("OrderSubStatusCode", "'" + orderSubStatusCode + "'");
                    QueryBuilderObject.SetField("OrderStatusColorID", statusColor);
                    err = QueryBuilderObject.InsertQueryString("OrderSubStatus", db_vms);

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetField("OrderSubStatusID", orderSubStatusID);
                    QueryBuilderObject.SetField("OrderStatusID", orderStatusID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", orderSubStatusCode);
                    err = QueryBuilderObject.InsertQueryString("OrderSubStatusLanguage", db_vms);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private bool isATMEmployee(int EmployeeID)
        {
            try
            {
                string securityGrpID = GetFieldValue("OperatorSecurityGroup", "SecurityGroupID", string.Format("SecurityGroupID = {1} AND OperatorID IN (SELECT OperatorID FROM EmployeeOperator WHERE EmployeeID = {0})", EmployeeID, 12), db_vms);
                return securityGrpID != string.Empty;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return false;
            }
        }
        public override void SendReciepts()
        {
            try
            {
                WriteMessage("\r\n" + "Sending Payments ..");

                DTP_EnvRecArqPalm_ReqT_Dados_IN record;
                List<DTP_EnvRecArqPalm_ReqT_Dados_IN> records;
                DateTime date;
                DateTime vouchDate;
                string Type = "";
                string PaymentID = "";
                string TransactionID = "";
                int PaymentType = 0;
                string BankName = "";
                decimal Amount = 0.0m;
                string ChNumber = "";
                object ChDate = "";
                string OutletCode = "";
                string SalesPersonCode = "";
                object CustomerName = "";
                object SalesPersonID = "";
                string BranchName = "";
                string DIST_CHANNEL = string.Empty;
                string CN_TRAN = string.Empty;
                string note2 = string.Empty;
                string OriginalTransactionID = "";
                string CustomerID = "";
                string updateFlag = "";

                bool ExtraPaymentAllowed = ReadConfiguration(Configuration.NumberOfDigits) == 1;

                string extraPaymentClause = "";
                if (ExtraPaymentAllowed)
                {
                    extraPaymentClause = "+ CP.RemainingAmount";
                }

                string cpFilter = "", cupFilter = "";
                if (Filters.EmployeeID != -1)
                {
                    cpFilter = "AND CP.EmployeeID = " + Filters.EmployeeID;
                    cupFilter = "AND CUP.EmployeeID = " + Filters.EmployeeID;
                }

                string resetSyncFlagForNonSentPayments = string.Format(@"UPDATE CP SET Synchronized = 0
FROM CustomerPayment CP
LEFT JOIN (SELECT DISTINCT CustomerPaymentID,TransactionID,CustomerID FROM PaymentsSyncHistory WHERE Result = 'SUCCESS') PSS 
ON PSS.CustomerPaymentID = CP.CustomerPaymentID 
AND PSS.TransactionID = CP.TransactionID 
AND PSS.CustomerID = CP.CustomerID
WHERE CP.PaymentDate >= '{0}' AND CP.PaymentDate < '{1}' {2}
AND CP.Synchronized = 1 AND PSS.CustomerID IS NULL AND CP.OrganizationID = {3};

UPDATE CP SET Synchronised = 0
FROM CustomerUnallocatedPayment CP
LEFT JOIN (SELECT DISTINCT CustomerPaymentID,TransactionID,CustomerID FROM PaymentsSyncHistory WHERE Result = 'SUCCESS') PSS 
ON PSS.CustomerPaymentID = CP.CustomerPaymentID 
AND PSS.CustomerID = CP.CustomerID
WHERE CP.PaymentDate >= '{0}' AND CP.PaymentDate < '{1}' {2}
AND CP.Synchronised = 1 AND PSS.CustomerID IS NULL AND CP.OrganizationID = {3};",
                    Filters.FromDate.Date.ToString("yyyy/MM/dd"), Filters.ToDate.Date.AddDays(1).ToString("yyyy/MM/dd"), cpFilter, OrganizationID);

                inCubeQuery = new InCubeQuery(db_vms, resetSyncFlagForNonSentPayments);
                err = inCubeQuery.ExecuteNonQuery();

                string AllPaymentsQuery = string.Format(@"SELECT
'CP' Type,
CP.EmployeeID,
E.Email,
CP.CustomerPaymentID,
CP.CustomerID,
CO.CustomerCode,
E.EmployeeCode,
CP.PaymentDate,
CP.AppliedAmount {5} PaidAmount,
CP.VoucherNumber,
CP.VoucherDate,
CP.VoucherOwner,
CP.BankID,
CP.BranchID,
CP.TransactionID,
CP.PaymentTypeID,
T.Notes,
T.SalesMode,
BL.Description Bank,
BBL.Description Branch
,CP.Notes note2
,T.SourceTransactionID

FROM CustomerPayment CP 
INNER JOIN CustomerOutlet CO ON CO.CustomerID = CP.CustomerID AND CO.OutletID = CP.OutletID 
INNER JOIN Employee E ON E.EmployeeID = CP.EmployeeID
INNER JOIN Customer C ON C.CustomerID = CP.CustomerID
INNER JOIN [Transaction] T ON T.TransactionID = CP.TransactionID AND T.CustomerID = CP.CustomerID
AND T.OutletID = CP.OutletID AND T.TransactionTypeID = 1
LEFT JOIN BankLanguage BL ON BL.BankID = CP.BankID AND BL.LanguageID = 1
LEFT JOIN BankBranchLanguage BBL ON BBL.BankID = CP.BankID AND BBL.BranchID = CP.BranchID AND BBL.LanguageID = 1
WHERE CP.PaymentStatusID <> 5 AND C.New = 0 AND CP.PaymentDate >= '{0}' AND CP.PaymentDate < '{1}' 
{2}
AND CP.OrganizationID = {3}
AND CP.Synchronized = 0

UNION

SELECT
'DP' Type,
CUP.EmployeeID,
E.Email,
CUP.CustomerPaymentID,
CUP.CustomerID,
CO.CustomerCode,
E.EmployeeCode,
CUP.PaymentDate,
CUP.PaidAmount,
CUP.VoucherNumber,
CUP.VoucherDate,
CUP.VoucherOwner,
CUP.BankID,
CUP.BranchID,
'' TransactionID,
CUP.PaymentTypeID,
'' Notes,
0 SalesMode,
BL.Description Bank,
BBL.Description Branch
,CUP.Notes note2
,'' SourceTransactionID
					
FROM CustomerUnallocatedPayment CUP
INNER JOIN CustomerOutlet CO ON CO.CustomerID = CUP.CustomerID AND CO.OutletID = CUP.OutletID
INNER JOIN Employee E ON E.EmployeeID = CUP.EmployeeID
INNER JOIN Customer C ON C.CustomerID = CUP.CustomerID
LEFT JOIN BankLanguage BL ON BL.BankID = CUP.BankID AND BL.LanguageID = 1
LEFT JOIN BankBranchLanguage BBL ON BBL.BankID = CUP.BankID AND BBL.BranchID = CUP.BranchID AND BBL.languageID = 1
WHERE C.New = 0 AND CUP.PaymentDate >= '{0}' AND CUP.PaymentDate < '{1}' 
{4}
AND CUP.OrganizationID = {3}
AND CUP.Synchronised = 0

ORDER BY EmployeeID"
                   , Filters.FromDate.Date.ToString("yyyy/MM/dd"), Filters.ToDate.Date.AddDays(1).ToString("yyyy/MM/dd"), cpFilter, OrganizationID, cupFilter, extraPaymentClause);

                inCubeQuery = new InCubeQuery(db_vms, AllPaymentsQuery);
                inCubeQuery.Execute();
                DataTable dtPayments = inCubeQuery.GetDataTable();
                execManager.UpdateActionInputOutput(TriggerID, AllPaymentsQuery, dtPayments.Rows.Count);

                for (int j = 0; j < dtPayments.Rows.Count; j++)
                {
                    int EmployeeID = -1;
                    string Email = "";
                    int processID = 0;
                    LastSavedFile = "";
                    Result res = Result.UnKnown;
                    try
                    {
                        EmployeeID = Convert.ToInt16(dtPayments.Rows[j]["EmployeeID"]);
                        Email = dtPayments.Rows[j]["Email"].ToString();
                        PaymentID = dtPayments.Rows[j]["CustomerPaymentID"].ToString();
                        OriginalTransactionID = dtPayments.Rows[j]["TransactionID"].ToString();
                        CustomerID = dtPayments.Rows[j]["CustomerID"].ToString();
                        Type = dtPayments.Rows[j]["Type"].ToString();

                        switch (Type)
                        {
                            case "CP":
                                updateFlag = string.Format(@"UPDATE CustomerPayment SET Synchronized = 1 WHERE CustomerPaymentID = '{0}' AND TransactionID = '{1}' AND CustomerID = {2};"
                                    , PaymentID, OriginalTransactionID, CustomerID);
                                break;
                            case "DP":
                                updateFlag = string.Format(@"UPDATE CustomerUnallocatedPayment SET Synchronised = 1 WHERE CustomerPaymentID = '{0}' AND CustomerID = {1};"
                                    , PaymentID, CustomerID);
                                break;
                        }

                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(2, EmployeeID.ToString());
                        filters.Add(10, PaymentID);
                        filters.Add(11, OriginalTransactionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, OrganizationID, filters);

                        object id = null;
                        inCubeQuery = new InCubeQuery(db_vms, string.Format("INSERT INTO PaymentsSyncHistory (SendingProcessID,CustomerPaymentID,TransactionID,CustomerID,AttemptDate,Result) VALUES ({0},'{1}','{2}',{3},GETDATE(),'UnKnown'); SELECT SCOPE_IDENTITY();", processID, PaymentID, OriginalTransactionID, CustomerID));
                        inCubeQuery.ExecuteScalar(ref id);
                        if (err != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            continue;
                        }

                        inCubeQuery = new InCubeQuery(db_vms, string.Format("SELECT Result FROM PaymentSendingStatus WHERE CustomerPaymentID = '{0}' AND TransactionID = '{1}' AND CustomerID = {2}", PaymentID, OriginalTransactionID, CustomerID));
                        object sendingResult = null;
                        err = inCubeQuery.ExecuteScalar(ref sendingResult);

                        bool resend = false;
                        if (err != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            continue;
                        }
                        else if (sendingResult != DBNull.Value && sendingResult != null && !string.IsNullOrEmpty(sendingResult.ToString()))
                        {
                            if (sendingResult.ToString().Trim() == "Started")
                            {
                                inCubeQuery = new InCubeQuery(db_vms, string.Format("SELECT AttemptDate FROM PaymentSendingStatus WHERE CustomerPaymentID = '{0}' AND TransactionID = '{1}' AND CustomerID = {2}", PaymentID, OriginalTransactionID, CustomerID));
                                object attemptDate = null;
                                inCubeQuery.ExecuteScalar(ref attemptDate);
                                if (attemptDate != DBNull.Value && attemptDate != null && !string.IsNullOrEmpty(attemptDate.ToString()))
                                {
                                    DateTime AttemptDate = DateTime.Now;
                                    if (DateTime.TryParse(attemptDate.ToString(), out AttemptDate))
                                    {
                                        if (DateTime.Now - AttemptDate >= new TimeSpan(0, 10, 0))
                                            resend = true;
                                    }
                                }
                            }
                            else if (sendingResult.ToString().Trim() == "ReSend")
                            {
                                resend = true;
                            }
                            else
                            {
                                inCubeQuery = new InCubeQuery(db_vms, string.Format("SELECT COUNT(*) FROM PaymentsSyncHistory WHERE CustomerPaymentID = '{0}' AND TransactionID = '{1}' AND CustomerID = {2} AND Result = 'Success'", PaymentID, OriginalTransactionID, CustomerID));
                                object isSent = null;
                                err = inCubeQuery.ExecuteScalar(ref isSent);
                                if (err != InCubeErrors.Success)
                                {
                                    res = Result.Failure;
                                    continue;
                                }
                                else if (Convert.ToInt16(isSent) > 0)
                                {
                                    res = Result.Duplicate;
                                    continue;
                                }
                                else
                                {
                                    resend = true;
                                }
                            }

                            if (!resend)
                            {
                                res = Result.Invalid;
                                continue;
                            }
                        }

                        inCubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE PaymentsSyncHistory SET Result = 'Started' WHERE ID = {0}", id));
                        err = inCubeQuery.ExecuteNonQuery();
                        if (err != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            continue;
                        }

                        Amount = decimal.Parse(dtPayments.Rows[j]["PaidAmount"].ToString());
                        PaymentType = int.Parse(dtPayments.Rows[j]["PaymentTypeID"].ToString());
                        ChNumber = dtPayments.Rows[j]["VoucherNumber"].ToString();
                        OutletCode = dtPayments.Rows[j]["CustomerCode"].ToString();
                        date = DateTime.Parse(dtPayments.Rows[j]["PaymentDate"].ToString());
                        ChDate = dtPayments.Rows[j]["VoucherDate"].ToString();
                        if (PaymentType != 1 && !ChDate.ToString().Trim().Equals(string.Empty))
                        {
                            vouchDate = DateTime.Parse(ChDate.ToString());
                        }
                        else
                        {
                            vouchDate = date;
                        }
                        SalesPersonCode = dtPayments.Rows[j]["EmployeeCode"].ToString();
                        TransactionID = OriginalTransactionID;
                        string InvItemNumber = "";
                        if (TransactionID.Contains(":"))
                        {
                            TransactionID = OriginalTransactionID.Split(new char[] { ':' })[0];
                            InvItemNumber = OriginalTransactionID.Split(new char[] { ':' })[1];
                        }
                        BankName = dtPayments.Rows[j]["Bank"].ToString();
                        BranchName = dtPayments.Rows[j]["Branch"].ToString();
                        string VOC_NO = "RV - " + dtPayments.Rows[j]["VoucherOwner"].ToString();
                        string note = dtPayments.Rows[j]["Notes"].ToString().Substring(0, Math.Min(dtPayments.Rows[j]["Notes"].ToString().Length, 10)).PadRight(10);
                        note2 = dtPayments.Rows[j]["note2"].ToString().PadRight(16);
                        string RV = dtPayments.Rows[j]["SourceTransactionID"].ToString().PadRight(13);

                        records = new List<DTP_EnvRecArqPalm_ReqT_Dados_IN>();
                        record = new DTP_EnvRecArqPalm_ReqT_Dados_IN();

                        switch (PaymentType)
                        {
                            case 1:
                                BankName = "Cash";
                                break;
                            case 2:
                                BankName = "CK-" + ChNumber + "," + BankName;
                                break;
                            case 3:
                                BankName = "CK-" + ChNumber + "," + BankName;
                                break;
                            case 5:
                                BankName = "TRF-" + DateTime.Parse(date.ToString()).ToString("dd/MM/yyyy") + "," + BankName;
                                break;

                            default:
                                continue;
                        }

                        int len = BankName.Length;
                        BankName = BankName.Substring(0, Math.Min(BankName.Length, 25)).PadRight(60);
                        if (BankName != "Cash" && len < 25)
                        {
                            BranchName = BranchName.Trim() != "" ? "-" : "" + BranchName;
                            BranchName = BranchName.Substring(0, Math.Min(BranchName.Length, 25 - len)).PadRight(40);
                        }
                        else
                        {
                            BranchName = "".PadRight(40);
                        }
                        if (RV.Length > 13) RV = RV.Substring(0, 13);

                        string Pay_Type = string.Empty;
                        string Down_Type = string.Empty;
                        bool _isATMEmployee = isATMEmployee(EmployeeID);
                        //GetPaymentType(Type, PaymentType, _isATMEmployee, ref Pay_Type, ref Down_Type);
                        GetPaymentTypeFromDB(Type, PaymentType, _isATMEmployee, ref Pay_Type, ref Down_Type);

                        record.Dados = "";
                        record.Dados += PaymentID.Substring(PaymentID.Length - 10, 10).PadLeft(10);//Pay_ID 0,10
                        record.Dados += "000";//ITEM_NUM 10,3
                        record.Dados += " ";//SP_GL_IND 13,1
                        string temp = date.Day.ToString().PadLeft(2, '0') + date.Month.ToString().PadLeft(2, '0') + date.Year.ToString().PadLeft(4, '0');//TRAN_DATE 14,8
                        record.Dados += date.Day.ToString().PadLeft(2, '0') + date.Month.ToString().PadLeft(2, '0') + date.Year.ToString().PadLeft(4, '0');//TRAN_DATE 14,8
                        record.Dados += TransactionID.Substring(0, Math.Min(TransactionID.Length, 10)).PadRight(10);//TRAN_ID 22,10
                        record.Dados += Pay_Type;//PAY_TYPE 32,1
                        record.Dados += SalesPersonCode.ToString().PadLeft(10);//EMP_CODE 33,10
                        record.Dados += OutletCode.PadLeft(10, '0');//CUST_CODE 43,10
                        record.Dados += OutletCode.PadLeft(10, '0');//OUTLET_CODE 53,10
                        record.Dados += "0";//DB_CR_IND 63,1
                        record.Dados += SetDecimal(Amount.ToString(), 12, ReadConfiguration(Configuration.NumberOfDigits), true);
                        record.Dados += string.Empty.PadLeft(3);//SALES_GRP 77,3
                        record.Dados += "01";//DIVISION 80,2
                        record.Dados += "10";//DIS_CHL 82,2
                        record.Dados += note.PadLeft(10);//CR_NO 84,10
                        record.Dados += string.Empty.PadLeft(18);//ALLOC_NMBR 94,18
                        record.Dados += note2.Length > 16 ? note2.Substring(0, 16) : note2.PadRight(16);//REF_DOC_NO 112,16
                        record.Dados += TransactionID.PadLeft(10);//INV_REF 128,10
                        record.Dados += string.Empty.PadLeft(4);//SALES_OFFICE 138,4
                        record.Dados += Down_Type;//DOWN_TYPE 142,2
                        record.Dados += Email.ToString().PadLeft(4);//MAIL_BOX 144,4
                        record.Dados += VOC_NO.Length > 30 ? VOC_NO.Substring(0, 30) : VOC_NO.PadRight(30);//no desc 148,30
                        record.Dados += BankName.ToString();//BANK 178,60
                        record.Dados += BranchName.ToString();//BRANCH 238,40
                        record.Dados += note2.Length > 50 ? note2.Substring(0, 50) : note2.PadRight(50);//REC_DATE 278,50
                        record.Dados += RV;//CR_VAL 328,13
                        temp = vouchDate.Year.ToString().PadLeft(4, '0') + vouchDate.Month.ToString().PadLeft(2, '0') + vouchDate.Day.ToString().PadLeft(2, '0');
                        record.Dados += vouchDate.Year.ToString().PadLeft(4, '0') + vouchDate.Month.ToString().PadLeft(2, '0') + vouchDate.Day.ToString().PadLeft(2, '0');
                        record.Dados += InvItemNumber.PadLeft(3, '0');
                        record.Dados += "*";
                        records.Add(record);

                        if (records.Count > 0)
                        {
                            DTP_EnvRecArqPalm_Resp req = CallWS(Email, "COL", WS_Event.OUT, records.ToArray(), ref res);
                            if (resp != null && req.Erro.Trim() == "")
                            {
                                res = Result.Success;
                                WriteMessage("\r\n" + "Sent Payment for [" + Email + "]\r\n" + PaymentID + " Applied for " + OriginalTransactionID);
                            }
                            else
                            {
                                res = Result.WebServiceConnectionError;
                                WriteMessage("\r\n" + "failed to send Payment for [" + Email + "]\r\n" + PaymentID + " Applied for " + OriginalTransactionID);
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "Failed in sending payment, SAP error code is: " + req.Erro.Trim(), LoggingType.Error, LoggingFiles.InCubeLog);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        res = Result.Failure;
                        WriteMessage("\r\n" + "failed to send Payment for [" + Email + "]\r\n" + PaymentID + " Applied for " + OriginalTransactionID);
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                    }
                    finally
                    {
                        string result = "";
                        switch (res)
                        {
                            case Result.Success:
                                result = "Success";
                                inCubeQuery = new InCubeQuery(db_vms, updateFlag);
                                err = inCubeQuery.ExecuteNonQuery();
                                break;
                            case Result.Invalid:
                                result = "Blocked";
                                break;
                            case Result.Failure:
                            case Result.WebServiceConnectionError:
                                result = "Failed";
                                break;
                            case Result.Duplicate:
                                result = "Duplicate";
                                inCubeQuery = new InCubeQuery(db_vms, updateFlag);
                                err = inCubeQuery.ExecuteNonQuery();
                                break;
                        }
                        if (result != "")
                        {
                            inCubeQuery = new InCubeQuery(db_vms, "UPDATE PaymentsSyncHistory SET Result = '" + result + "' WHERE SendingProcessID = " + processID);
                            inCubeQuery.ExecuteNonQuery();
                        }
                        execManager.LogIntegrationEnding(processID, res, CallStopWatch.ElapsedMilliseconds, LastSavedFile, result);
                    }
                }
                inCubeQuery = new InCubeQuery(db_vms, @"UPDATE PSH SET Result = (CASE EH.ResultID 
WHEN 4 THEN 'Blocked' 
WHEN 2 THEN 'Failed' 
WHEN 7 THEN 'Duplicate' 
WHEN 9 THEN 'Duplicate' 
WHEN 1 THEN 'Success' END)
FROM PaymentsSyncHistory PSH
INNER JOIN Int_ExecutionDetails EH ON EH.ID = PSH.SendingProcessID
WHERE EH.ResultID IN (4,2,7,9,1) AND EH.TriggerID > " + (TriggerID - 2000));
                inCubeQuery.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void GetPaymentTypeFromDB(string PT, int PaymentType, bool _isATMEmployee, ref string Pay_Type, ref string Down_Type)
        {
            try
            {
                inCubeQuery = new InCubeQuery(db_vms, string.Format(@"SELECT TOP 1 Pay_Type, Down_Type
FROM IntegrationPaymentType WHERE OrganizationID IN (-1,{0}) AND MainType = '{1}' AND isATM = {2} AND PaymentTypeID = {3}
ORDER BY OrganizationID DESC", OrganizationID, PT, _isATMEmployee ? 1 : 0, PaymentType));
                inCubeQuery.Execute();
                DataTable dt = new DataTable();
                dt = inCubeQuery.GetDataTable();
                Pay_Type = dt.Rows[0]["Pay_Type"].ToString();
                Down_Type = dt.Rows[0]["Down_Type"].ToString();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void GetPaymentType(string PT, int PaymentType, bool _isATMEmployee, ref string Pay_Type, ref string Down_Type)
        {
            try
            {
                Pay_Type = string.Empty;
                Down_Type = string.Empty;
                switch (PT)
                {
                    case "CP":
                        switch (PaymentType)
                        {
                            case 1:
                                Pay_Type = _isATMEmployee ? "6" : "1";
                                Down_Type = "  ";
                                break;
                            case 2:
                                Pay_Type = "2";
                                Down_Type = "  ";
                                break;
                            case 3:
                                Pay_Type = "5";
                                Down_Type = "  ";
                                break;
                            case 5:
                                Pay_Type = "3";
                                Down_Type = "  ";
                                break;
                        }
                        break;

                    case "DP":
                        switch (PaymentType)
                        {
                            case 1:
                                Down_Type = _isATMEmployee ? "6" : "1";
                                Pay_Type = "4";
                                break;
                            case 2:
                                Pay_Type = "4";
                                Down_Type = "2 ";
                                break;
                            case 5:
                                Pay_Type = "4";
                                Down_Type = "2 ";
                                break;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void WriteFile(string MailBox, WS_Event Event, string FileName, string FileText)
        {
            try
            {
                if (!string.IsNullOrEmpty(MailBox))
                {
                    string path = CoreGeneral.Common.StartupPath + "\\FileBackup\\" + MailBox + "\\" + Event.ToString() + "\\";

                    if (FileName.Contains(".txt"))
                        FileName = FileName.Substring(0, FileName.Length - 4);

                    string filePath = path + FileName + "_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".txt";
                    LastSavedFile = filePath;
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    File.AppendAllText(filePath, FileText);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        #region Prices
        public override void UpdatePrice()
        {
            try
            {
                if (TriggerID < 1)
                    return;

                DataTable DT_Emp = GetEmployees(Filters.EmployeeID);
                ClearProgress();
                SetProgressMax(DT_Emp.Rows.Count);

                for (int e = 0; e < DT_Emp.Rows.Count; e++)
                {
                    string Salesperson = DT_Emp.Rows[e][0].ToString();
                    string Email = DT_Emp.Rows[e][1].ToString();

                    ReportProgress(Email);
                    WriteMessage("\r\n\r\nMailbox (" + Email + "):");

                    if (ReadConfiguration(Configuration.ImportVNI, int.Parse(Salesperson)) == 1)
                        UpdateMasterDataForMailBox(IntegrationField.Discount_U, Salesperson, Email, "VNI", "Discounts");

                    UpdateMasterDataForMailBox(IntegrationField.Price_U, Salesperson, Email, "PRE", "Prices");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void FillDiscounts(string Email, int EmployeeID)
        {
            try
            {
                for (int i = 1; i < resp.T_Dados_OUT.Length - 1; i++)
                {
                    try
                    {
                        string record = resp.T_Dados_OUT[i].Dados;
                        string type = record.Substring(4, 2).Trim();
                        if (type != "01" && type != "02" && type != "03" && type != "04" && type != "05" && type != "06") continue;

                        string salesOrderTypeCode = "";
                        string CustomerGroup = "";
                        long CustomerCode = 0;
                        long ItemCode = 0;
                        decimal Discount = 0;

                        switch (type)
                        {
                            case "01":
                                CustomerGroup = Email + "-Standard-" + record.Substring(6, 2).Trim();
                                ItemCode = long.Parse(record.Substring(28, 18).Trim());
                                Discount = GetDecimalValue(record, 46, 6, 2);
                                break;

                            case "02":
                                CustomerCode = long.Parse(record.Substring(6, 12).Trim());
                                CustomerGroup = Email + CustomerCode;
                                ItemCode = long.Parse(record.Substring(18, 18).Trim());
                                Discount = GetDecimalValue(record, 36, 6, 2);
                                break;

                            case "03":
                                CustomerGroup = Email + "-PriceGroup-" + record.Substring(6, 2).Trim();
                                ItemCode = long.Parse(record.Substring(28, 18).Trim());
                                Discount = GetDecimalValue(record, 46, 6, 2);
                                break;

                            case "04":
                                CustomerGroup = Email + "-CustomerGroup-" + record.Substring(6, 2).Trim();
                                ItemCode = long.Parse(record.Substring(28, 18).Trim());
                                Discount = GetDecimalValue(record, 46, 6, 2);
                                break;

                            case "05":
                                salesOrderTypeCode = record.Substring(6, 4).Trim();
                                CustomerGroup = Email;
                                ItemCode = long.Parse(record.Substring(30, 18).Trim());
                                Discount = GetDecimalValue(record, 48, 6, 2);
                                break;

                            case "06":
                                salesOrderTypeCode = record.Substring(6, 4).Trim();
                                CustomerGroup = Email + "-Standard-" + record.Substring(30, 2).Trim();
                                ItemCode = long.Parse(record.Substring(52, 18).Trim());
                                Discount = GetDecimalValue(record, 70, 6, 2);
                                break;
                        }

                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("TriggerID", TriggerID.ToString());
                        QueryBuilderObject.SetField("EmployeeID", EmployeeID.ToString());
                        QueryBuilderObject.SetStringField("Email", Email);
                        QueryBuilderObject.SetField("[LineNo]", (i + 1).ToString());
                        QueryBuilderObject.SetStringField("TableType", type);
                        QueryBuilderObject.SetStringField("GroupCode", CustomerGroup);
                        QueryBuilderObject.SetStringField("CustomerCode", CustomerCode.ToString());
                        QueryBuilderObject.SetStringField("ItemCode", ItemCode.ToString());
                        QueryBuilderObject.SetStringField("SalesOrderType", salesOrderTypeCode);
                        QueryBuilderObject.SetField("Discount", Discount.ToString());
                        err = QueryBuilderObject.InsertQueryString("DiscountDetails", db_ERP);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void FillPrices(string Email, int EmployeeID)
        {
            try
            {
                string taxable = "";
                Dictionary<string, string> taxItems = new Dictionary<string, string>();
                for (int i = 1; i < resp.T_Dados_OUT.Length - 1; i++)
                {
                    try
                    {
                        string record = resp.T_Dados_OUT[i].Dados;
                        string type = record.Substring(4, 2).Trim();
                        if (type == "00")
                        {
                            if (record.Length >= 240)
                                taxItems.Add(long.Parse(record.Substring(6, 18).Trim()).ToString(), record.Substring(239, 1).Trim());
                            continue;
                        }
                        if (type != "01" && type != "02" && type != "03" && type != "04" && type != "05" && type != "08" && type != "P8") continue;

                        string salesOrderTypeCode = "";
                        string PriceListCode = "";
                        string CustomerGroup = "";
                        long CustomerCode = 0;
                        long ItemCode = 0;
                        decimal Price = 0;
                        decimal MaxPrice = 0;
                        decimal MinPrice = 0;

                        switch (type)
                        {
                            case "01"://Defaulte pricelist
                                PriceListCode = Email + "-Standard-" + record.Substring(6, 2).Trim();
                                CustomerGroup = PriceListCode;
                                ItemCode = long.Parse(record.Substring(28, 18).Trim());
                                Price = GetDecimalValue(record, 46, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                MaxPrice = GetDecimalValue(record, 52, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                MinPrice = GetDecimalValue(record, 58, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                break;
                            case "02":// Customer Price List
                                CustomerCode = long.Parse(record.Substring(6, 12).Trim());
                                PriceListCode = Email + "-Customer-" + CustomerCode.ToString();
                                ItemCode = long.Parse(record.Substring(18, 18).Trim());
                                Price = GetDecimalValue(record, 36, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                MaxPrice = GetDecimalValue(record, 42, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                MinPrice = GetDecimalValue(record, 48, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                break;
                            case "03"://Price group
                                PriceListCode = Email + "-PriceGroup-" + record.Substring(6, 2).Trim();
                                CustomerGroup = PriceListCode;
                                ItemCode = long.Parse(record.Substring(28, 18).Trim());
                                Price = GetDecimalValue(record, 46, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                MaxPrice = GetDecimalValue(record, 52, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                MinPrice = GetDecimalValue(record, 58, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                break;
                            case "04"://Customer group
                                PriceListCode = Email + "-CustomerGroup-" + record.Substring(6, 2).Trim();
                                CustomerGroup = PriceListCode;
                                ItemCode = long.Parse(record.Substring(28, 18).Trim());
                                Price = GetDecimalValue(record, 46, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                MaxPrice = GetDecimalValue(record, 52, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                MinPrice = GetDecimalValue(record, 58, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                break;
                            case "05":// FIFO
                                salesOrderTypeCode = record.Substring(6, 4).Trim();
                                PriceListCode = Email + "-FEFO-" + salesOrderTypeCode;
                                CustomerGroup = Email;
                                ItemCode = long.Parse(record.Substring(30, 18).Trim());
                                Price = GetDecimalValue(record, 48, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                MaxPrice = GetDecimalValue(record, 54, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                MinPrice = GetDecimalValue(record, 60, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                break;
                            case "08":// Customer FIFO
                                salesOrderTypeCode = record.Substring(18, 4).Trim();
                                CustomerCode = long.Parse(record.Substring(6, 12).Trim());
                                PriceListCode = Email + "-CustomerFEFO-" + salesOrderTypeCode + "-" + CustomerCode.ToString();
                                ItemCode = long.Parse(record.Substring(22, 18).Trim());
                                Price = GetDecimalValue(record, 40, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                MaxPrice = GetDecimalValue(record, 46, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                MinPrice = GetDecimalValue(record, 52, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                break;
                            case "P8":// Group FIFO
                                salesOrderTypeCode = record.Substring(6, 4).Trim();
                                CustomerGroup = Email + "-CustomerGroup-" + record.Substring(10, 2).Trim();
                                PriceListCode = Email + "-GroupFIFO-" + salesOrderTypeCode + "-" + record.Substring(10, 2).Trim();
                                ItemCode = long.Parse(record.Substring(12, 18).Trim());
                                Price = GetDecimalValue(record, 30, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                MaxPrice = GetDecimalValue(record, 36, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                MinPrice = GetDecimalValue(record, 42, 6, ReadConfiguration(Configuration.NumberOfDigits));
                                break;
                        }
                        taxable = "0";
                        if (taxItems.ContainsKey(ItemCode.ToString())) taxable = taxItems[ItemCode.ToString()];
                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("TriggerID", TriggerID.ToString());
                        QueryBuilderObject.SetField("EmployeeID", EmployeeID.ToString());
                        QueryBuilderObject.SetStringField("Email", Email);
                        QueryBuilderObject.SetField("[LineNo]", (i + 1).ToString());
                        QueryBuilderObject.SetStringField("TableType", type);
                        QueryBuilderObject.SetStringField("PriceListCode", PriceListCode);
                        QueryBuilderObject.SetStringField("GroupCode", CustomerGroup);
                        QueryBuilderObject.SetStringField("CustomerCode", CustomerCode.ToString());
                        QueryBuilderObject.SetStringField("ItemCode", ItemCode.ToString());
                        QueryBuilderObject.SetStringField("SalesOrderType", salesOrderTypeCode);
                        QueryBuilderObject.SetField("Price", Price.ToString());
                        QueryBuilderObject.SetField("MaxPrice", MaxPrice.ToString());
                        QueryBuilderObject.SetField("MinPrice", MinPrice.ToString());
                        QueryBuilderObject.SetField("taxable", taxable.ToString());
                        err = QueryBuilderObject.InsertQueryString("PriceDetails", db_ERP);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }

        }
        #endregion

        #region Items
        public override void UpdateItem()
        {
            if (TriggerID < 1)
                return;

            DataTable DT_Emp = GetEmployees(Filters.EmployeeID);
            ClearProgress();
            SetProgressMax(DT_Emp.Rows.Count);

            for (int e = 0; e < DT_Emp.Rows.Count; e++)
            {
                string Salesperson = DT_Emp.Rows[e][0].ToString();
                string Email = DT_Emp.Rows[e][1].ToString();

                ReportProgress(Email);
                WriteMessage("\r\n\r\nMailbox (" + Email + "):");

                if (UpdateMasterDataForMailBox(IntegrationField.Item_U, Salesperson, Email, "PRE", "Items") == Result.Success)
                {
                    
                }
            }
        }
        private void FillItems(string Email, int EmployeeID)
        {
            try
            {
                for (int i = 1; i < resp.T_Dados_OUT.Length - 1; i++)
                {
                    try
                    {
                        string record = resp.T_Dados_OUT[i].Dados;
                        string type = record.Substring(4, 2).Trim();
                        if (type != "00")
                        {
                            continue;
                        }

                        string ItemCode = GetStringField(record, 6, 18);
                        string OldItemCode = GetStringField(record, 24, 18);
                        string itemDescriptionEnglish = GetStringField(record, 42, 40);
                        string CategoryCode = GetStringField(record, 148, 2);
                        string packQty = GetDecimal(record, 103, 8, 3);
                        string weight = GetDecimal(record, 91, 6, 3);
                        string PackDescriptionEnglish = GetStringField(record, 97, 3);
                        string priceType = GetStringField(record, 100, 3);
                        string brand = GetStringField(record, 240, 30);
                        string family1 = GetStringField(record, 270, 60);
                        string family2 = GetStringField(record, 330, 60);
                        string family3 = GetStringField(record, 390, 60);
                        string family4 = GetStringField(record, 450, 60);
                        string Barcode = GetStringField(record, 510, 14);

                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("TriggerID", TriggerID.ToString());
                        QueryBuilderObject.SetField("EmployeeID", EmployeeID.ToString());
                        QueryBuilderObject.SetStringField("Email", Email);
                        QueryBuilderObject.SetField("[LineNo]", (i + 1).ToString());
                        QueryBuilderObject.SetStringField("ItemCode", ItemCode);
                        QueryBuilderObject.SetStringField("ItemUOM", PackDescriptionEnglish);
                        QueryBuilderObject.SetStringField("ItemName", itemDescriptionEnglish);
                        QueryBuilderObject.SetStringField("OldItemCode", OldItemCode);
                        QueryBuilderObject.SetStringField("CategoryCode", CategoryCode);
                        QueryBuilderObject.SetField("PackQuantity", packQty.ToString());
                        QueryBuilderObject.SetField("Weight", weight.ToString());
                        QueryBuilderObject.SetStringField("PriceType", priceType);
                        QueryBuilderObject.SetStringField("Brand", brand);
                        QueryBuilderObject.SetStringField("Family1", family1);
                        QueryBuilderObject.SetStringField("Family2", family2);
                        QueryBuilderObject.SetStringField("Family3", family3);
                        QueryBuilderObject.SetStringField("Family4", family4);
                        QueryBuilderObject.SetStringField("Barcode", Barcode);
                        err = QueryBuilderObject.InsertQueryString("ItemDetails", db_ERP);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        #endregion

        #region STA
        public override void UpdateSTA()
        {
            if (TriggerID < 1)
                return;

            DataTable DT_Emp = GetEmployees(Filters.EmployeeID);
            ClearProgress();
            SetProgressMax(DT_Emp.Rows.Count);

            for (int e = 0; e < DT_Emp.Rows.Count; e++)
            {
                string Salesperson = DT_Emp.Rows[e][0].ToString();
                string Email = DT_Emp.Rows[e][1].ToString();

                ReportProgress(Email);
                WriteMessage("\r\n\r\nMailbox (" + Email + "):");

                UpdateMasterDataForMailBox(IntegrationField.STA_U, Salesperson, Email, "STA", "Order Status");
            }
        }
        public void FillOrderStaus(string Email, int EmployeeID)
        {
            #region(Declarations)
            string orderSubStatusCode = string.Empty;
            string email = string.Empty;
            string employeeID = string.Empty;
            string record = string.Empty;
            string docType = string.Empty;
            string customerOrderNo = string.Empty;
            DateTime deliveryDate = new DateTime();
            DateTime orderDate = new DateTime();
            string itemCode;
            string OrderID = string.Empty;
            string price;
            string orderedQty;
            string actualQty;
            string customerCode = "";
            #endregion

            for (int i = 1; i < resp.T_Dados_OUT.Length - 1; i++)
            {
                try
                {
                    record = resp.T_Dados_OUT[i].Dados;
                    docType = record.Substring(0, 3);

                    switch (docType)
                    {
                        case "001": //Order Header (Table = Header Status)                                       
                            customerOrderNo = record.Substring(18, 15).Trim();
                            if (customerOrderNo == string.Empty) continue;

                            orderDate = GetDateTime(record, 57, 8, "ddmmyyyy");
                            deliveryDate = GetDateTime(record, 73, 8, "ddmmyyyy");
                            orderSubStatusCode = record.Substring(82, 6).TrimEnd(new char[] { ' ', '-' });
                            customerCode = record.Substring(45, 12).TrimStart(new char[] { '0' });

                            QueryBuilderObject = new QueryBuilder();
                            QueryBuilderObject.SetField("TriggerID", TriggerID.ToString());
                            QueryBuilderObject.SetField("EmployeeID", EmployeeID.ToString());
                            QueryBuilderObject.SetStringField("Email", Email);
                            QueryBuilderObject.SetField("[LineNo]", (i + 1).ToString());
                            QueryBuilderObject.SetStringField("DocType", docType);
                            QueryBuilderObject.SetStringField("CustomerOrderNo", customerOrderNo);
                            QueryBuilderObject.SetDateField("OrderDate", orderDate);
                            QueryBuilderObject.SetDateField("DeliveryDate", deliveryDate);
                            QueryBuilderObject.SetStringField("OrderSubStatusCode", orderSubStatusCode);
                            QueryBuilderObject.SetStringField("CustomerCode", customerCode);
                            err = QueryBuilderObject.InsertQueryString("OrderStatusDetails", db_ERP);
                            break;

                        case "002": //Order Details (Table = Item Status)
                            if (customerOrderNo == string.Empty) continue;
                            itemCode = record.Substring(13, 18).TrimStart(new char[] { '0' });
                            price = GetDecimal(record, 49, 5, ReadConfiguration(Configuration.NumberOfDigits));
                            orderedQty = GetDecimal(record, 54, 8, 3);
                            actualQty = GetDecimal(record, 62, 8, 3);
                            orderSubStatusCode = record.Substring(74, 6).TrimEnd(new char[] { ' ', '-' });

                            QueryBuilderObject = new QueryBuilder();
                            QueryBuilderObject.SetField("TriggerID", TriggerID.ToString());
                            QueryBuilderObject.SetField("EmployeeID", EmployeeID.ToString());
                            QueryBuilderObject.SetStringField("Email", Email);
                            QueryBuilderObject.SetField("[LineNo]", (i + 1).ToString());
                            QueryBuilderObject.SetStringField("DocType", docType);
                            QueryBuilderObject.SetStringField("CustomerOrderNo", customerOrderNo);
                            QueryBuilderObject.SetStringField("OrderSubStatusCode", orderSubStatusCode);
                            QueryBuilderObject.SetStringField("CustomerCode", customerCode);
                            QueryBuilderObject.SetStringField("ItemCode", itemCode);
                            QueryBuilderObject.SetField("Price", price);
                            QueryBuilderObject.SetField("OrderedQty", orderedQty);
                            QueryBuilderObject.SetField("ActualQty", actualQty);
                            err = QueryBuilderObject.InsertQueryString("OrderStatusDetails", db_ERP);
                            break;
                    }


                    //record = resp.T_Dados_OUT[i].Dados;
                    //docType = record.Substring(0, 3);

                    //switch (docType)
                    //{
                    //    case "001": //Order Header (Table = Header Status)                                       
                    //        OrderID = string.Empty; customerID = string.Empty; outletID = string.Empty;

                    //        customerOrderNo = record.Substring(18, 15).TrimEnd(new char[] { ' ' }); //Customer Order number
                    //        if (customerOrderNo == string.Empty) continue;
                    //        deliveryDate = GetDateTime(record, 73, 8, "ddmmyyyy");
                    //        orderDate = GetDateTime(record, 57, 8, "ddmmyyyy");
                    //        orderSubStatusCode = record.Substring(82, 6).TrimEnd(new char[] { ' ', '-' }); //Order Status
                    //        customerCode = record.Substring(45, 12).TrimStart(new char[] { '0' });
                    //        custOutID = GetFieldValue("CustomerOutlet", "CAST(CustomerID AS nvarchar) + ':' + CAST(OutletID AS nvarchar)", "CustomerCode = '" + customerCode + "'", db_vms);

                    //        EnsureExistanceOfSubStatus(orderSubStatusCode, "1", ref orderStatusID, ref orderSubStatusID);

                    //        OrderID = GetFieldValue("SalesOrder", "OrderID", string.Format("(OrderID = '{0}' OR LPO LIKE '%{1}%') AND CustomerID = {2} AND OutletID = {3} AND DATEADD(dd, 0, DATEDIFF(dd, 0, orderdate)) = CONVERT(DateTime,'{4}',103)", customerOrderNo.Substring(0, Math.Min(13, customerOrderNo.Length)), customerOrderNo, customerID, outletID, orderDate.ToString("dd/MM/yyyy")), db_vms);
                    //        if (OrderID != string.Empty)
                    //        {
                    //            //update existing order
                    //            updateQuery = string.Format("UPDATE SalesOrder SET OrderStatusID = {0}, OrderSubStatusID = {1}, ActualDeliveryDate = CONVERT(datetime, '{2}', 103) WHERE OrderID = '{3}' AND CustomerID = {4} AND OutletID = {5}"
                    //            , orderStatusID, orderSubStatusID, deliveryDate.ToString("dd/MM/yyyy"), OrderID, customerID, outletID);
                    //            inCubeQuery = new InCubeQuery(db_vms, updateQuery);
                    //            err = inCubeQuery.ExecuteNonQuery(ref affectedRows);
                    //        }
                    //        break;

                    //    case "002": //Order Details (Table = Item Status)
                    //        if (OrderID == string.Empty || customerID == string.Empty || outletID == string.Empty) continue;

                    //        itemCode = record.Substring(13, 18).TrimStart(new char[] { '0' }); //Order Code product
                    //        PackID = GetFieldValue("ItemDefaultPack", "PackID", "ItemCode = '" + itemCode + "'", db_vms);

                    //        if (PackID != "")
                    //        {
                    //            price = GetDecimal(record, 49, 5, ReadConfiguration(Configuration.NumberOfDigits));
                    //            salesTransTypeID = decimal.Parse(price) == 0 ? "4" : "1";
                    //            orderedQty = GetDecimal(record, 54, 8, 3);
                    //            actualQty = GetDecimal(record, 62, 8, 3);
                    //            cdaDiscount = GetDecimal(record, 151, 13, 2);
                    //            orderSubStatusCode = record.Substring(74, 6).TrimEnd(new char[] { ' ', '-' }); //Order Status
                    //            EnsureExistanceOfSubStatus(orderSubStatusCode, "1", ref orderStatusID, ref orderSubStatusID);

                    //            if (GetFieldValue("SalesOrderDetail", "PackID", string.Format("OrderID = '{0}' AND PackID = {1} AND Quantity = {2} AND CustomerID = {3} AND OutletID = {4}", customerOrderNo, PackID, orderedQty, customerID, outletID), db_vms) != string.Empty)
                    //            {
                    //                updateQuery = string.Format("UPDATE SalesOrderDetail SET OrderStatusID = {0}, OrderSubStatusID = {1}, ActualDeliveredQuantity = {2}, ActualPrice = {3}, CDADiscount = {9} WHERE OrderID = '{4}' AND PackID = {5} AND Quantity = {6} AND CustomerID = {7} AND OutletID = {8}"
                    //                    , orderStatusID, orderSubStatusID, actualQty, price, customerOrderNo, PackID, orderedQty, customerID, outletID, cdaDiscount);
                    //                inCubeQuery = new InCubeQuery(db_vms, updateQuery);
                    //                err = inCubeQuery.ExecuteNonQuery(ref affectedRows);
                    //            }
                    //        }
                    //        break;
                    //}
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                }
            }
        }
        #endregion

        #region Stock
        public override void UpdateStock()
        {
            if (TriggerID < 1)
                return;

            string whereStr = string.Empty;
            if (Filters.WarehouseID == -1)
            {
                whereStr = "WHERE (ISNULL(WarehouseCode,'') <> '') AND WarehouseTypeID = 1 AND WarehouseID IN (SELECT WarehouseID FROM VehicleLoadingWh) AND OrganizationID = " + OrganizationID;
            }
            else
            {
                whereStr = "WHERE WarehouseID = " + Filters.WarehouseID;
            }

            string QueryStringEmp = string.Format(@"SELECT     
		            WarehouseID,
                    WarehouseCode           		
	            FROM   Warehouse
            		
	            {0}", whereStr);
            inCubeQuery = new InCubeQuery(db_vms, QueryStringEmp);

            inCubeQuery.Execute();
            DataTable DT_Emp = inCubeQuery.GetDataTable();
            ClearProgress();
            SetProgressMax(DT_Emp.Rows.Count);

            for (int e = 0; e < DT_Emp.Rows.Count; e++)
            {
                string Warehouse = DT_Emp.Rows[e][0].ToString();
                string Email = DT_Emp.Rows[e][1].ToString();

                ReportProgress(Email);
                WriteMessage("\r\n\r\nMailbox (" + Email + "):");

                if (UpdateMasterDataForMailBox(IntegrationField.Stock_U, Warehouse, Email, "ETQ", "Stock") == Result.Success)
                {
                    //ReportProgress(DT_Emp.Rows.Count);
                }
            }
        }
        public void FillStock(string Email, int WarehouseID)
        {
            for (int i = 1; i < resp.T_Dados_OUT.Length - 1; i++)
            {
                try
                {
                    string record = resp.T_Dados_OUT[i].Dados;
                    if (record.Substring(0, 2).Trim() != "E3") continue;

                    string ItemCode = GetStringField(record, 6, 18);
                    string ItemStatus = GetStringField(record, 29, 2);
                    string Quantity = GetDecimal(record, 31, 7, 0);

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetField("TriggerID", TriggerID.ToString());
                    QueryBuilderObject.SetField("WarehouseID", WarehouseID.ToString());
                    QueryBuilderObject.SetStringField("Email", Email);
                    QueryBuilderObject.SetField("[LineNo]", (i + 1).ToString());
                    QueryBuilderObject.SetStringField("ItemCode", ItemCode);
                    QueryBuilderObject.SetStringField("ItemStatus", ItemStatus);
                    QueryBuilderObject.SetField("Quantity", Quantity);
                    err = QueryBuilderObject.InsertQueryString("StockDetails", db_ERP);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                }
            }
        }
        #endregion

        #region Invoice
        public override void UpdateInvoice()
        {
            try
            {
                if (TriggerID < 1)
                    return;

                DataTable DT_Emp = GetEmployees(Filters.EmployeeID);
                ClearProgress();
                SetProgressMax(DT_Emp.Rows.Count);

                for (int e = 0; e < DT_Emp.Rows.Count; e++)
                {
                    string Salesperson = DT_Emp.Rows[e][0].ToString();
                    string Email = DT_Emp.Rows[e][1].ToString();

                    ReportProgress(Email);
                    WriteMessage("\r\n\r\nMailbox (" + Email + "):");

                    UpdateMasterDataForMailBox(IntegrationField.Invoice_U, Salesperson, Email, "CNT", "Invoices");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public void FillInvoices(string email, int EmployeeID)
        {
            try
            {
                for (int i = 1; i < resp.T_Dados_OUT.Length - 1; i++)
                {
                    string record = resp.T_Dados_OUT[i].Dados;

                    string CustomerCode = GetStringField(record, 0, 12);
                    string TotalValueAvailable = GetDecimal(record, 12, 10, ReadConfiguration(Configuration.NumberOfDigits));
                    string SAPBillingNumber = GetStringField(record, 22, 10);
                    string AppliedAmount = GetDecimal(record, 32, 10, ReadConfiguration(Configuration.NumberOfDigits));
                    string PaymentID = GetStringField(record, 48, 5);
                    DateTime InvoiceDate = GetDateTime(record, 53, 6, "ddMMyy");
                    string DocumentStatus = GetStringField(record, 59, 1);
                    string CreditStatus = GetStringField(record, 60, 1);
                    string SAPAccountingDocument = GetStringField(record, 61, 10);
                    string ItemNumber = GetStringField(record, 71, 2);
                    string RVNumber = GetStringField(record, 73, 13);
                    DateTime DueDate = GetDateTime(record, 42, 6, "ddmmyy");

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetField("TriggerID", TriggerID.ToString());
                    QueryBuilderObject.SetField("EmployeeID", EmployeeID.ToString());
                    QueryBuilderObject.SetStringField("Email", email);
                    QueryBuilderObject.SetField("[LineNo]", (i + 1).ToString());
                    QueryBuilderObject.SetStringField("CustomerCode", CustomerCode);
                    QueryBuilderObject.SetField("TotalValueAvailable", TotalValueAvailable);
                    QueryBuilderObject.SetStringField("SAPBillingNumber", SAPBillingNumber);
                    QueryBuilderObject.SetField("AppliedAmount", AppliedAmount);
                    QueryBuilderObject.SetDateField("DueDate", DueDate);
                    QueryBuilderObject.SetStringField("PaymentID", PaymentID);
                    QueryBuilderObject.SetDateField("InvoiceDate", InvoiceDate);
                    QueryBuilderObject.SetStringField("DocumentStatus", DocumentStatus);
                    QueryBuilderObject.SetStringField("CreditStatus", CreditStatus);
                    QueryBuilderObject.SetStringField("SAPAccountingDocument", SAPAccountingDocument);
                    QueryBuilderObject.SetStringField("ItemNumber", ItemNumber);
                    QueryBuilderObject.SetStringField("RVNumber", RVNumber);
                    err = QueryBuilderObject.InsertQueryString("InvoiceDetails", db_ERP);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        #endregion

        #region Customer
        public override void UpdateCustomer()
        {
            WriteMessage("\r\n" + "Updating Customers ... " + DateTime.Now.ToString("HH:mm:ss"));
            DataTable DT_Emp = GetEmployees(Filters.EmployeeID);

            for (int e = 0; e < DT_Emp.Rows.Count; e++)
            {
                int processID = 0;
                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;
                Result res = Result.Success;

                try
                {
                    string Salesperson = DT_Emp.Rows[e][0].ToString();
                    string Email = DT_Emp.Rows[e][1].ToString();
                    string EmployeeCode = DT_Emp.Rows[e][2].ToString();

                    WriteMessage("\r\n");
                    WriteMessage("<<<Start Import CUSTOMERS [" + Email + "] >>> ");

                    Dictionary<int, string> filters = new Dictionary<int, string>();
                    filters.Add(2, Salesperson);
                    processID = execManager.LogIntegrationBegining(TriggerID, OrganizationID, filters);


                    if (e == 123456)
                    {
                        //ReadFromExternalFile("cliD100_05042016_133310.txt", -1, 0, "");
                    }
                    else
                    {
                        resp = CallWS(Email, "CLI", WS_Event.IN, null, ref res);
                    }

                    if (res != Result.Success && res != Result.NoRowsFound)
                    {
                        if (res == Result.NoFileRetreived)
                        {
                            WriteMessage("\r\n");
                            WriteMessage("<<<No changes on [" + Email + "] customers>>> ");
                        }
                        continue;
                    }
                    ClearProgress();
                    SetProgressMax(resp.T_Dados_OUT.Length - 2);
                    #region Territory
                    string TerritoryID = "";

                    TerritoryID = GetFieldValue("Territory", "TerritoryID", "TerritoryCode = '" + EmployeeCode + "' and OrganizationID=" + OrganizationID + "", db_vms);
                    if (TerritoryID.Trim() == string.Empty)
                    {
                        // TOTALINSERTED++;

                        TerritoryID = GetFieldValue("Territory", "ISNULL(MAX(TerritoryID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                        QueryBuilderObject.SetField("OrganizationID", OrganizationID.ToString());
                        QueryBuilderObject.SetField("TerritoryCode", "'" + EmployeeCode + "'");

                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("CreatedDate", "getdate()");

                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "getdate()");

                        QueryBuilderObject.InsertQueryString("Territory", db_vms);
                        err = ExistObject("TerritoryLanguage", "TerritoryID", "TerritoryID = " + TerritoryID + " AND LanguageID = 1", db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + EmployeeCode + "'");
                            QueryBuilderObject.InsertQueryString("TerritoryLanguage", db_vms);

                        }

                    }
                    //if (TerritoryID != "")
                    //{
                    //    string QueryDeleteCustTerr = @"Delete from CustOutTerritory where TerritoryID= " + TerritoryID;
                    //    inCubeQuery = new InCubeQuery(db_vms, QueryDeleteCustTerr);
                    //   err= inCubeQuery.ExecuteNonQuery();
                    //}
                    #endregion

                    #region(Clear employee territory customers)
                    //03122015 I COMMENTED THE FOLLOWING DELETE STATEMENT BECAUSE IN THE ROUTE FUNCTION THE WS DOES NOT RETURN ANY RECORDS WHICH CAUSED THE CUSTOUTTERRITORY TO BE DELETED BUT NOT INSERTED AGAIN IN THE ROUTE INTEGRATION.
                    //05122015 EDIT: I KEPT THE BELOW DELETE STATEMENT, AND ADDED AN INSERT STATEMENT AT THE END OF THIS FUNCTION TO ADD ALL ROUTE CUSTOMERS TO THE TERRITORY CUSTOMERS.
                    inCubeQuery = new InCubeQuery(db_vms, "DELETE FROM CustOutTerritory WHERE TerritoryID = " + TerritoryID);
                    inCubeQuery.ExecuteNonQuery();

                    #endregion

                    #region Get Employee Group

                    string EGroupID = AddCustomerGroup(Email, Email, "", "", false);

                    #endregion

                    for (int i = 1; i < resp.T_Dados_OUT.Length - 1; i++)
                    {

                        string record = resp.T_Dados_OUT[i].Dados;

                        ReportProgress("Updating Customers [" + Email + "] ");

                        string CustomerCode = record.Substring(4, 12).Trim();

                        CustomerCode = (int.Parse(CustomerCode)).ToString();
                        string CustomerDescriptionEnglish = record.Substring(18, 40).Trim();
                        string CustomerAddressEnglish = record.Substring(58, 40).Trim();
                        string Phonenumber = record.Substring(307, 15).Trim();
                        string Faxnumber = "";
                        string Paymentterms = record.Substring(182, 4).Trim().ToLower();
                        string OnHold = "0";
                        string City = record.Substring(118, 40).Trim();
                        string State = record.Substring(158, 2).Trim();
                        string Area = record.Substring(98, 20).Trim();
                        string Country = "UAE";
                        string Street = Area;
                        string HeadOfficeCode = "";// record.Substring(, 12).Trim();
                        string Taxable = GetStringField(record, 473, 1).Trim();
                        if (Taxable != "0" && Taxable != "1")
                            Taxable = "0";
                        string CreditLimit = GetDecimal(record, 188, 15, ReadConfiguration(Configuration.NumberOfDigits));
                        string CustomerGroup = "";
                        string PriceGroup = "";
                        string Segment = "";
                        string ChannelCode = "";
                        string SubChannelCode = "";

                        if (record.Substring(214, 2).Trim() != "")
                        {
                            CustomerGroup = Email + "-CustomerGroup-" + record.Substring(214, 2).Trim();
                            SubChannelCode = record.Substring(214, 2).Trim();
                        }
                        if (record.Substring(212, 2).Trim() != "")
                        {
                            PriceGroup = Email + "-PriceGroup-" + record.Substring(212, 2).Trim();
                        }
                        if (record.Substring(186, 2).Trim() != "")
                        {
                            Segment = Email + "-Standard-" + record.Substring(186, 2).Trim();
                        }
                        if (record.Substring(454, 4).Trim() != "")
                        {
                            ChannelCode = GetStringField(record, 454, 4).Trim();
                        }

                        string Balance = "0";

                        decimal latDec = 0;
                        decimal.TryParse(record.Substring(416, 15).Trim(), out latDec);
                        string GPSlatitude = latDec.ToString();

                        decimal longDec = 0;
                        decimal.TryParse(record.Substring(431, 15).Trim(), out longDec);
                        string GPSlongitude = longDec.ToString();

                        string CustomerBarCode = "";
                        string _custID = "";
                        string _outletID = "";

                        if (CustomerCode == string.Empty)

                            continue;

                        string CustomerGroupID = AddCustomerGroup(CustomerGroup, CustomerGroup, "", "", false);
                        string PriceGroupID = AddCustomerGroup(PriceGroup, PriceGroup, "", "", false);
                        string SegmentID = AddCustomerGroup(Segment, Segment, "", "", false);

                        string SubChannelID = "";
                        string HirGroupID = "";
                        string ChannelID = GetFieldValue("Channel", "ChannelID", "ChannelCode = '" + ChannelCode + "'", db_vms);
                        if (ChannelCode != "" && ChannelID == string.Empty)
                        {
                            ChannelID = GetFieldValue("Channel", "isnull(MAX(ChannelID),0) + 1", db_vms);

                            QueryBuilderObject.SetField("ChannelID", ChannelID.ToString());
                            QueryBuilderObject.SetField("ChannelCode", "'" + ChannelCode + "'");
                            QueryBuilderObject.InsertQueryString("Channel", db_vms);

                            QueryBuilderObject.SetField("ChannelID", ChannelID.ToString());
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + ChannelCode + "'");
                            QueryBuilderObject.InsertQueryString("ChannelLanguage", db_vms);
                        }
                        if (ChannelID != string.Empty)
                        {
                            SubChannelID = GetFieldValue("SubChannel", "SubChannelID", "SubChannelCode = '" + SubChannelCode + "' AND ChannelID = " + ChannelID, db_vms);
                            if (SubChannelCode != "" && SubChannelID == string.Empty)
                            {
                                SubChannelID = GetFieldValue("SubChannel", "isnull(MAX(SubChannelID),0) + 1", db_vms);

                                QueryBuilderObject.SetField("ChannelID", ChannelID);
                                QueryBuilderObject.SetField("SubChannelID", SubChannelID);
                                QueryBuilderObject.SetStringField("SubChannelCode", SubChannelCode);
                                QueryBuilderObject.InsertQueryString("SubChannel", db_vms);

                                QueryBuilderObject.SetField("SubChannelID", SubChannelID);
                                QueryBuilderObject.SetField("LanguageID", "1");
                                QueryBuilderObject.SetStringField("Description", SubChannelCode);
                                QueryBuilderObject.InsertQueryString("SubChannelLanguage", db_vms);
                            }
                        }
                        if (SubChannelID != string.Empty)
                        {
                            HirGroupID = AddCustomerGroup(SubChannelCode, SubChannelCode, SubChannelID, ChannelID, true);
                        }
                        string CustomerID = "0";

                        string ExistCustomer = "";

                        if (HeadOfficeCode == string.Empty)
                        {

                            CustomerID = GetFieldValue("Customer", "CustomerID", "CustomerCode = '" + CustomerCode + "'", db_vms);
                            if (CustomerID == string.Empty)
                            {
                                CustomerID = GetFieldValue("Customer", "isnull(MAX(CustomerID),0) + 1", db_vms);
                            }

                            #region Customer
                            ExistCustomer = GetFieldValue("Customer", "CustomerID", "CustomerID = " + CustomerID, db_vms);
                            if (ExistCustomer != string.Empty)
                            {
                                TOTALUPDATED++;

                                QueryBuilderObject.SetField("CustomerCode", "'" + CustomerCode + "'");
                                QueryBuilderObject.SetField("Phone", "'" + Phonenumber + "'");
                                QueryBuilderObject.SetField("Fax", "'" + Faxnumber + "'");
                                QueryBuilderObject.SetField("OnHold", OnHold);
                                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                                QueryBuilderObject.SetField("UpdatedDate", "GETDATE()");
                                QueryBuilderObject.UpdateQueryString("Customer", " CustomerID = " + CustomerID, db_vms);

                            }
                            else // New Customer --- Insert Query
                            {
                                TOTALINSERTED++;

                                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                                QueryBuilderObject.SetField("Phone", "'" + Phonenumber + "'");
                                QueryBuilderObject.SetField("Fax", "'" + Faxnumber + "'");
                                QueryBuilderObject.SetField("Email", "' '");
                                QueryBuilderObject.SetField("CustomerCode", "'" + CustomerCode + "'");
                                QueryBuilderObject.SetField("OnHold", OnHold);
                                QueryBuilderObject.SetField("StreetID", "0");
                                QueryBuilderObject.SetField("StreetAddress", "0");
                                QueryBuilderObject.SetField("InActive", "0");
                                QueryBuilderObject.SetField("New", "0");

                                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                                QueryBuilderObject.SetField("CreatedDate", "GETDATE()");

                                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                                QueryBuilderObject.SetField("UpdatedDate", "GETDATE()");
                                QueryBuilderObject.InsertQueryString("Customer", db_vms);
                            }

                            #endregion

                            #region CustomerLanguage
                            ExistCustomer = GetFieldValue("CustomerLanguage", "CustomerID", "CustomerID = " + CustomerID + " AND LanguageID = 1", db_vms);
                            if (ExistCustomer != string.Empty) // Exist CustomerLanguage --- Update Query
                            {
                                QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish + "'");
                                QueryBuilderObject.SetField("Address", "'" + CustomerAddressEnglish + "'");
                                QueryBuilderObject.UpdateQueryString("CustomerLanguage", "  CustomerID = " + CustomerID + " AND LanguageID = 1", db_vms);
                            }
                            else  // New CustomerLanguage --- Insert Query
                            {
                                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                                QueryBuilderObject.SetField("LanguageID", "1");
                                QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish + "'");
                                QueryBuilderObject.SetField("Address", "'" + CustomerAddressEnglish + "'");
                                QueryBuilderObject.InsertQueryString("CustomerLanguage", db_vms);
                            }

                            ExistCustomer = GetFieldValue("CustomerLanguage", "CustomerID", "CustomerID = " + CustomerID + " AND LanguageID = 2", db_vms); // ARABIC
                            if (ExistCustomer != string.Empty) // Exist CustomerLanguage --- Update Query
                            {
                                QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionEnglish + "'");
                                QueryBuilderObject.SetField("Address", "N'" + CustomerAddressEnglish + "'");
                                QueryBuilderObject.UpdateQueryString("CustomerLanguage", "  CustomerID = " + CustomerID + " AND LanguageID = 2", db_vms);
                            }
                            else  // New CustomerLanguage --- Insert Query
                            {
                                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                                QueryBuilderObject.SetField("LanguageID", "2");
                                QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionEnglish + "'");
                                QueryBuilderObject.SetField("Address", "N'" + CustomerAddressEnglish + "'");
                                QueryBuilderObject.InsertQueryString("CustomerLanguage", db_vms);
                            }

                            #endregion

                            #region GeoLocations

                            string CountryID = "";
                            string StateID = "";
                            string CityID = "";
                            string AreaID = "";
                            string StreetID = "";

                            if (!Country.Equals(string.Empty))
                            {
                                CountryID = GetFieldValue("CountryLanguage", "CountryID", "Description = '" + Country + "'", db_vms);

                                if (CountryID.Equals(string.Empty))
                                {
                                    CountryID = GetFieldValue("Country", "isnull(MAX(CountryID),0) + 1", db_vms);

                                    QueryBuilderObject.SetField("CountryID", CountryID);
                                    QueryBuilderObject.InsertQueryString("Country", db_vms);

                                    QueryBuilderObject.SetField("CountryID", CountryID);
                                    QueryBuilderObject.SetField("LanguageID", "1");
                                    QueryBuilderObject.SetField("Description", "N'" + Country + "'");
                                    QueryBuilderObject.InsertQueryString("CountryLanguage", db_vms);

                                    QueryBuilderObject.SetField("CountryID", CountryID);
                                    QueryBuilderObject.SetField("LanguageID", "2");
                                    QueryBuilderObject.SetField("Description", "N'" + Country + "'");
                                    QueryBuilderObject.InsertQueryString("CountryLanguage", db_vms);
                                }

                                if (!State.Equals(string.Empty))
                                {
                                    StateID = GetFieldValue("StateLanguage", "StateID", "Description = '" + State + "' AND CountryID = " + CountryID, db_vms);

                                    if (StateID.Equals(string.Empty))
                                    {
                                        StateID = GetFieldValue("State", "isnull(MAX(StateID),0) + 1", db_vms);

                                        QueryBuilderObject.SetField("CountryID", CountryID);
                                        QueryBuilderObject.SetField("StateID", StateID);
                                        QueryBuilderObject.InsertQueryString("State", db_vms);

                                        QueryBuilderObject.SetField("CountryID", CountryID);
                                        QueryBuilderObject.SetField("StateID", StateID);
                                        QueryBuilderObject.SetField("LanguageID", "1");
                                        QueryBuilderObject.SetField("Description", "N'" + State + "'");
                                        QueryBuilderObject.InsertQueryString("StateLanguage", db_vms);

                                        QueryBuilderObject.SetField("CountryID", CountryID);
                                        QueryBuilderObject.SetField("StateID", StateID);
                                        QueryBuilderObject.SetField("LanguageID", "2");
                                        QueryBuilderObject.SetField("Description", "N'" + State + "'");
                                        QueryBuilderObject.InsertQueryString("StateLanguage", db_vms);
                                    }

                                    if (!City.Equals(string.Empty))
                                    {
                                        CityID = GetFieldValue("CityLanguage", "CityID", "Description = '" + City + "' AND CountryID = " + CountryID + " AND StateID = " + StateID, db_vms);

                                        if (CityID.Equals(string.Empty))
                                        {
                                            CityID = GetFieldValue("City", "isnull(MAX(CityID),0) + 1", db_vms);

                                            QueryBuilderObject.SetField("CountryID", CountryID);
                                            QueryBuilderObject.SetField("StateID", StateID);
                                            QueryBuilderObject.SetField("CityID", CityID);
                                            QueryBuilderObject.InsertQueryString("City", db_vms);

                                            QueryBuilderObject.SetField("CountryID", CountryID);
                                            QueryBuilderObject.SetField("StateID", StateID);
                                            QueryBuilderObject.SetField("CityID", CityID);
                                            QueryBuilderObject.SetField("LanguageID", "1");
                                            QueryBuilderObject.SetField("Description", "N'" + City + "'");
                                            QueryBuilderObject.InsertQueryString("CityLanguage", db_vms);

                                            QueryBuilderObject.SetField("CountryID", CountryID);
                                            QueryBuilderObject.SetField("StateID", StateID);
                                            QueryBuilderObject.SetField("CityID", CityID);
                                            QueryBuilderObject.SetField("LanguageID", "2");
                                            QueryBuilderObject.SetField("Description", "N'" + City + "'");
                                            QueryBuilderObject.InsertQueryString("CityLanguage", db_vms);
                                        }

                                        if (!Area.Equals(string.Empty))
                                        {
                                            AreaID = GetFieldValue("AreaLanguage", "AreaID", "Description = '" + Area + "' AND CountryID = " + CountryID + " AND StateID = " + StateID + " AND CityID = " + CityID, db_vms);

                                            if (AreaID.Equals(string.Empty))
                                            {
                                                AreaID = GetFieldValue("Area", "isnull(MAX(AreaID),0) + 1", db_vms);

                                                QueryBuilderObject.SetField("CountryID", CountryID);
                                                QueryBuilderObject.SetField("StateID", StateID);
                                                QueryBuilderObject.SetField("CityID", CityID);
                                                QueryBuilderObject.SetField("AreaID", AreaID);
                                                QueryBuilderObject.InsertQueryString("Area", db_vms);

                                                QueryBuilderObject.SetField("CountryID", CountryID);
                                                QueryBuilderObject.SetField("StateID", StateID);
                                                QueryBuilderObject.SetField("CityID", CityID);
                                                QueryBuilderObject.SetField("AreaID", AreaID);
                                                QueryBuilderObject.SetField("LanguageID", "1");
                                                QueryBuilderObject.SetField("Description", "N'" + Area + "'");
                                                QueryBuilderObject.InsertQueryString("AreaLanguage", db_vms);

                                                QueryBuilderObject.SetField("CountryID", CountryID);
                                                QueryBuilderObject.SetField("StateID", StateID);
                                                QueryBuilderObject.SetField("CityID", CityID);
                                                QueryBuilderObject.SetField("AreaID", AreaID);
                                                QueryBuilderObject.SetField("LanguageID", "2");
                                                QueryBuilderObject.SetField("Description", "N'" + Area + "'");
                                                QueryBuilderObject.InsertQueryString("AreaLanguage", db_vms);
                                            }

                                            if (!Street.Equals(string.Empty))
                                            {
                                                StreetID = GetFieldValue("StreetLanguage", "StreetID", "Description = '" + Street + "' AND CountryID = " + CountryID + " AND StateID = " + StateID + " AND CityID = " + CityID + " AND AreaID = " + AreaID, db_vms);

                                                if (StreetID.Equals(string.Empty))
                                                {
                                                    StreetID = GetFieldValue("Street", "isnull(MAX(StreetID),0) + 1", db_vms);

                                                    QueryBuilderObject.SetField("CountryID", CountryID);
                                                    QueryBuilderObject.SetField("StateID", StateID);
                                                    QueryBuilderObject.SetField("CityID", CityID);
                                                    QueryBuilderObject.SetField("AreaID", AreaID);
                                                    QueryBuilderObject.SetField("StreetID", StreetID);
                                                    QueryBuilderObject.InsertQueryString("Street", db_vms);

                                                    QueryBuilderObject.SetField("CountryID", CountryID);
                                                    QueryBuilderObject.SetField("StateID", StateID);
                                                    QueryBuilderObject.SetField("CityID", CityID);
                                                    QueryBuilderObject.SetField("AreaID", AreaID);
                                                    QueryBuilderObject.SetField("StreetID", StreetID);
                                                    QueryBuilderObject.SetField("LanguageID", "1");
                                                    QueryBuilderObject.SetField("Description", "N'" + Street + "'");
                                                    QueryBuilderObject.InsertQueryString("StreetLanguage", db_vms);

                                                    QueryBuilderObject.SetField("CountryID", CountryID);
                                                    QueryBuilderObject.SetField("StateID", StateID);
                                                    QueryBuilderObject.SetField("CityID", CityID);
                                                    QueryBuilderObject.SetField("AreaID", AreaID);
                                                    QueryBuilderObject.SetField("StreetID", StreetID);
                                                    QueryBuilderObject.SetField("LanguageID", "2");
                                                    QueryBuilderObject.SetField("Description", "N'" + Street + "'");
                                                    QueryBuilderObject.InsertQueryString("StreetLanguage", db_vms);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            #endregion

                            string AccountID = "1";

                            AccountID = GetFieldValue("AccountCust", "AccountID", "CustomerID = " + CustomerID, db_vms);
                            if (AccountID != string.Empty)
                            {
                                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                                QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + AccountID, db_vms);
                            }
                            else
                            {
                                AccountID = GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms);

                                QueryBuilderObject.SetField("AccountID", AccountID);
                                QueryBuilderObject.SetField("AccountTypeID", "1");
                                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                                QueryBuilderObject.SetField("Balance", Balance);
                                QueryBuilderObject.SetField("GL", "0");
                                QueryBuilderObject.SetField("OrganizationID", OrganizationID.ToString());
                                QueryBuilderObject.SetField("CurrencyID", "1");
                                QueryBuilderObject.InsertQueryString("Account", db_vms);

                                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                                QueryBuilderObject.SetField("AccountID", AccountID);
                                QueryBuilderObject.InsertQueryString("AccountCust", db_vms);

                                QueryBuilderObject.SetField("AccountID", AccountID);
                                QueryBuilderObject.SetField("LanguageID", "1");
                                QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish.Trim() + " Account'");
                                QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);

                                QueryBuilderObject.SetField("AccountID", AccountID);
                                QueryBuilderObject.SetField("LanguageID", "2");
                                QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionEnglish.Trim() + " Account'");
                                QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
                            }

                            CreateCustomerOutlet(CustomerCode, CustomerGroupID, Paymentterms, CustomerDescriptionEnglish, CustomerAddressEnglish, CustomerDescriptionEnglish, CustomerAddressEnglish, Phonenumber, Faxnumber, OnHold, Taxable, CustomerCode, CreditLimit, Balance, GPSlongitude, GPSlatitude, CustomerBarCode, "", StreetID, "", "", ref _custID, ref _outletID, EGroupID, PriceGroupID, SegmentID, HirGroupID, TerritoryID);
                        }
                        else // Outlet
                        {
                            CreateCustomerOutlet(CustomerCode, CustomerGroupID, Paymentterms, CustomerDescriptionEnglish, CustomerAddressEnglish, CustomerDescriptionEnglish, CustomerAddressEnglish, Phonenumber, Faxnumber, OnHold, Taxable, HeadOfficeCode, CreditLimit, Balance, GPSlongitude, GPSlatitude, CustomerBarCode, "", "", "", "", ref _custID, ref _outletID, EGroupID, PriceGroupID, SegmentID, HirGroupID, TerritoryID);
                        }
                    }
                    WriteMessage("\r\n");
                    WriteMessage("<<< CUSTOMERS [" + Email + "] >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                }
                finally
                {
                    execManager.LogIntegrationEnding(processID, res, TOTALINSERTED, TOTALUPDATED, LastSavedFile);
                }
                inCubeQuery = new InCubeQuery(db_vms, @"update CustomerOutlet set gpslongitude = '0' where gpslongitude is null
update CustomerOutlet set gpslatitude = '0' where gpslatitude is null");
                err = inCubeQuery.ExecuteNonQuery();
            }
        }
        private void CreateCustomerOutlet(string CustomerCode, string CustomerGroupID, string PaymentTermDesc, string CustomerDescriptionEnglish, string CustomerAddressEnglish, string CustomerDescriptionArabic, string CustomerAddressArabic, string Phonenumber, string Faxnumber, string OnHold, string Taxable, string HeadOfficeCode, string CreditLimit, string Balance, string Longitude, string latitude, string CustomerBarCode, string email, string StreetID, string Contact, string ContactMobile, ref string _custID, ref string _outtletID, string EGroupID, string PriceGroupID, string SegmentID, string HirGroupID, string TerritoryID)
        {
            int CustomerID;
            InCubeErrors err;

            if (Longitude == string.Empty)
                Longitude = "null";

            if (latitude == string.Empty)
                latitude = "null";

            string ExistCustomer = "";

            int CustomerTypeID = 2;
            bool IsComplexPaymentTerm = false;
            int PaymentTermDays = 0;
            ClassifyPaymentTerm(PaymentTermDesc, ref IsComplexPaymentTerm, ref PaymentTermDays);
            if (PaymentTermDays == 0) CustomerTypeID = 1;


            CustomerID = int.Parse(GetFieldValue("Customer", "CustomerID", "CustomerCode='" + HeadOfficeCode + "'", db_vms));

            string PaymentTermID = "0";

            if (PaymentTermDesc.Trim() != "" && PaymentTermDesc.Trim() != "0000")
            {
                PaymentTermID = GetFieldValue("PaymentTermLanguage", "PaymentTermID", "Description = '" + PaymentTermDesc + "'", db_vms);
                if (PaymentTermID == string.Empty)
                {
                    PaymentTermID = GetFieldValue("PaymentTerm", "isnull(MAX(PaymentTermID),0) + 1", db_vms);

                    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                    if (IsComplexPaymentTerm)
                    {
                        QueryBuilderObject.SetField("PaymentTermTypeID", "2");
                        QueryBuilderObject.SetField("SimplePeriodWidth", "0");
                        QueryBuilderObject.SetField("SimplePeriodID", "0");
                        QueryBuilderObject.SetField("ComplexPeriodWidth", "31");
                        QueryBuilderObject.SetField("ComplexPeriodID", "3");
                        QueryBuilderObject.SetField("GracePeriod", (PaymentTermDays - 1).ToString());
                        QueryBuilderObject.SetField("GracePeriodTypeID", "1");
                        err = QueryBuilderObject.InsertQueryString("PaymentTerm", db_vms);
                    }
                    else
                    {
                        QueryBuilderObject.SetField("PaymentTermTypeID", "1");
                        QueryBuilderObject.SetField("SimplePeriodWidth", PaymentTermDays.ToString());
                        QueryBuilderObject.SetField("SimplePeriodID", "1");
                        err = QueryBuilderObject.InsertQueryString("PaymentTerm", db_vms);
                    }

                    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + PaymentTermDesc + "'");
                    QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms);
                }
            }

            #region Customer Outlet and language

            string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerID = " + CustomerID + " AND CustomerCode = '" + CustomerCode + "'", db_vms);
            if (!OutletID.Trim().Equals(string.Empty))
            {
                QueryBuilderObject.SetField("Phone", "'" + Phonenumber + "'");
                QueryBuilderObject.SetField("Fax", "'" + Faxnumber + "'");
                QueryBuilderObject.SetField("Email", "'" + email + "'");
                QueryBuilderObject.SetField("CustomerTypeID", CustomerTypeID.ToString()); //HardCoded -1- Cash -2- Credit
                QueryBuilderObject.SetField("OnHold", OnHold);
                if (latitude != "0")
                    QueryBuilderObject.SetField("GPSLatitude", latitude);
                if (Longitude != "0")
                    QueryBuilderObject.SetField("GPSLongitude", Longitude);
                QueryBuilderObject.SetField("Taxeable", Taxable);
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "GETDATE()");
                QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                QueryBuilderObject.SetField("OrganizationID", OrganizationID.ToString());
                QueryBuilderObject.UpdateQueryString("CustomerOutlet", "  CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);

            }
            else
            {
                OutletID = GetFieldValue("CustomerOutlet", "isnull(MAX(OutletID),0) + 1", "CustomerID = " + CustomerID, db_vms);

                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("CustomerCode", "'" + CustomerCode + "'");
                QueryBuilderObject.SetField("Barcode", "'" + CustomerBarCode + "'");
                QueryBuilderObject.SetField("Phone", "'" + Phonenumber + "'");
                QueryBuilderObject.SetField("Fax", "'" + Faxnumber + "'");
                QueryBuilderObject.SetField("Email", "'" + email + "'");
                QueryBuilderObject.SetField("Taxeable", Taxable);
                QueryBuilderObject.SetField("OrganizationID", OrganizationID.ToString());

                if (!StreetID.Equals(string.Empty))
                {
                    QueryBuilderObject.SetField("StreetID", StreetID);
                }

                QueryBuilderObject.SetField("CustomerTypeID", CustomerTypeID.ToString()); //HardCoded -1- Cash -2- Credit
                QueryBuilderObject.SetField("CurrencyID", "1");
                QueryBuilderObject.SetField("OnHold", OnHold);
                QueryBuilderObject.SetField("GPSLatitude", latitude);
                QueryBuilderObject.SetField("GPSLongitude", Longitude);
                QueryBuilderObject.SetField("StreetAddress", "0");
                QueryBuilderObject.SetField("InActive", "0");
                QueryBuilderObject.SetField("Notes", "0");
                QueryBuilderObject.SetField("SkipCreditCheck", "0");
                QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);

                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "GETDATE()");

                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "GETDATE()");

                err = QueryBuilderObject.InsertQueryString("CustomerOutlet", db_vms);
            }

            DeleteCustomerGroups(CustomerID.ToString(), OutletID.ToString());

            #region Customer payment
            string CustPaymentTermID = GetFieldValue("CustomerOutletPaymentTerm", "PaymentTermID", "PaymentTermID = " + PaymentTermID + " and CustomerID=" + CustomerID + " and Outletid=" + OutletID, db_vms);
            if (CustPaymentTermID == string.Empty && PaymentTermID != "")
            {

                QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                err = QueryBuilderObject.InsertQueryString("CustomerOutletPaymentTerm", db_vms);
            }

            #endregion


            #region Territory Customer
            err = ExistObject("CustOutTerritory", "TerritoryID", "TerritoryID = " + TerritoryID + " AND CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
            if (err != InCubeErrors.Success)
            {

                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("TerritoryID", TerritoryID);
                QueryBuilderObject.InsertQueryString("CustOutTerritory", db_vms);
            }
            #endregion

            if (CustomerGroupID.Trim() != "")
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("GroupID", CustomerGroupID.ToString());
                QueryBuilderObject.InsertQueryString("CustomerOutletGroup", db_vms);
            }
            if (EGroupID.ToString() != "")
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("GroupID", EGroupID.ToString());
                QueryBuilderObject.InsertQueryString("CustomerOutletGroup", db_vms);
            }
            if (PriceGroupID != "")
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("GroupID", PriceGroupID.ToString());
                QueryBuilderObject.InsertQueryString("CustomerOutletGroup", db_vms);
            }
            if (SegmentID != "")
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("GroupID", SegmentID.ToString());
                QueryBuilderObject.InsertQueryString("CustomerOutletGroup", db_vms);
            }
            if (HirGroupID != "")
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("GroupID", HirGroupID);
                QueryBuilderObject.InsertQueryString("CustomerOutletGroup", db_vms);
            }
            ExistCustomer = GetFieldValue("CustomerOutletLanguage", "OutletID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " AND LanguageID = 1", db_vms);
            if (ExistCustomer != string.Empty)
            {
                QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish + "'");
                QueryBuilderObject.SetField("Address", "'" + CustomerAddressEnglish + "'");
                QueryBuilderObject.UpdateQueryString("CustomerOutletLanguage", "  CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " AND LanguageID = 1", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish + "'");
                QueryBuilderObject.SetField("Address", "'" + CustomerAddressEnglish + "'");
                QueryBuilderObject.InsertQueryString("CustomerOutletLanguage", db_vms);
            }

            ExistCustomer = GetFieldValue("CustomerOutletLanguage", "OutletID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " AND LanguageID = 2", db_vms); //Arabic
            if (ExistCustomer != string.Empty)
            {
                QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionArabic + "'");
                QueryBuilderObject.SetField("Address", "N'" + CustomerAddressArabic + "'");
                QueryBuilderObject.UpdateQueryString("CustomerOutletLanguage", "  CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " AND LanguageID = 2", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionArabic + "'");
                QueryBuilderObject.SetField("Address", "N'" + CustomerAddressArabic + "'");
                QueryBuilderObject.InsertQueryString("CustomerOutletLanguage", db_vms);
            }

            #region Customer Outlet Account
            int AccountID = 1;

            string MainCustomerAccount = GetFieldValue("AccountCust", "AccountID", "CustomerID = " + CustomerID, db_vms);

            ExistCustomer = GetFieldValue("AccountCustOut", "AccountID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
            if (ExistCustomer == string.Empty)
            {
                AccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("AccountTypeID", "1");//customer account
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.SetField("Balance", Balance); // Balance =  Balance + ChqNotCollected
                QueryBuilderObject.SetField("GL", "0");
                QueryBuilderObject.SetField("OrganizationID", OrganizationID.ToString());
                QueryBuilderObject.SetField("CurrencyID", "1");
                QueryBuilderObject.SetField("ParentAccountID", MainCustomerAccount);

                QueryBuilderObject.InsertQueryString("Account", db_vms);

                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.InsertQueryString("AccountCustOut", db_vms);

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish.Trim() + " Account'");
                QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionArabic.Trim() + " Account'");
                QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("ParentAccountID", MainCustomerAccount);
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + ExistCustomer, db_vms);
            }
            #endregion

            #region Contact

            if (!Contact.Equals(string.Empty))
            {
                string ContactID = GetFieldValue("ContactLanguage", "ContactID", "Description = '" + Contact + "'", db_vms);

                if (ContactID.Equals(string.Empty))
                {
                    ContactID = GetFieldValue("Contact", "isnull(MAX(ContactID),0) + 1", db_vms);

                    QueryBuilderObject.SetField("ContactID", ContactID);
                    QueryBuilderObject.SetField("Mobile", "'" + ContactMobile + "'");
                    QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                    QueryBuilderObject.SetField("OutletID", OutletID);
                    QueryBuilderObject.InsertQueryString("Contact", db_vms);

                    QueryBuilderObject.SetField("ContactID", ContactID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "N'" + Contact + "'");
                    QueryBuilderObject.InsertQueryString("ContactLanguage", db_vms);

                    QueryBuilderObject.SetField("ContactID", ContactID);
                    QueryBuilderObject.SetField("LanguageID", "2");
                    QueryBuilderObject.SetField("Description", "N'" + Contact + "'");
                    QueryBuilderObject.InsertQueryString("ContactLanguage", db_vms);
                }
                else
                {
                    QueryBuilderObject.SetField("Mobile", "'" + ContactMobile + "'");
                    QueryBuilderObject.UpdateQueryString("Contact", "ContactID = " + ContactID, db_vms);
                }
            }

            #endregion

            #endregion

            _custID = CustomerID.ToString();
            _outtletID = OutletID;
        }
        private void ClassifyPaymentTerm(string PaymentTermString, ref bool IsComplexTerm, ref int Period)
        {
            IsComplexTerm = false;
            string periodString = "0";
            try
            {
                for (int i = 0; i < PaymentTermString.Length; i++)
                {
                    if (!Char.IsDigit(PaymentTermString[i]))
                    {
                        IsComplexTerm = true;
                    }
                    else
                    {
                        periodString += PaymentTermString[i];
                    }
                }
                Period = int.Parse(periodString);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public string AddCustomerGroup(string GroupCode, string GroupName, string SubChannelID, string ChannelID, bool IsHirGroup)
        {
            string GroupID = "";
            try
            {
                GroupCode = GroupCode.Trim();
                GroupName = GroupName.Trim();

                if (GroupCode.Trim() == string.Empty)
                    return string.Empty;

                if (IsHirGroup)
                    GroupID = GetFieldValue("CustomerGroup", "GroupID", string.Format("GroupCode = '{0}' AND SubChannelID = {1} AND ChannelID = {2}", GroupCode, SubChannelID, ChannelID), db_vms);
                else
                    GroupID = GetFieldValue("CustomerGroup", "GroupID", " GroupCode = '" + GroupCode + "'", db_vms);

                if (GroupID == string.Empty)
                {
                    GroupID = GetFieldValue("CustomerGroup", "ISNULL(MAX(GroupID),0) + 1", "GroupID < 1000000", db_vms);

                    QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                    QueryBuilderObject.SetStringField("GroupCode", GroupCode);
                    if (SubChannelID != string.Empty)
                        QueryBuilderObject.SetField("SubChannelID", SubChannelID);
                    if (ChannelID != string.Empty)
                        QueryBuilderObject.SetField("ChannelID", ChannelID);
                    QueryBuilderObject.SetField("OrganizationID", OrganizationID.ToString());
                    QueryBuilderObject.InsertQueryString("CustomerGroup", db_vms);

                    QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", GroupName);
                    QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return GroupID;
        }
        private void DeleteCustomerGroups(string Customerid, string Outletid)
        {
            string deleteQ = "Delete from CustomerOutletGroup where Groupid<1000000 and CustomerID=" + Customerid + " And outletid=" + Outletid;
            InCubeQuery quere = new InCubeQuery(db_vms, deleteQ);
            quere.ExecuteNonQuery();
        }

        #endregion
    }
}