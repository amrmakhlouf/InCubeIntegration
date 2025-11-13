using InCubeIntegration_DAL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.IO;
using System.Xml;

namespace InCubeIntegration_BL
{
    public class RoadNetManager
    {
        public bool RN_Conn_Opened = false;
        InCubeDatabase db_RoadNet;
        InCubeDatabase db_Sonic;
        InCubeQuery InCubeQry;
        IntegrationBase integrationObj;
        public RoadNetManager(bool OpenRoadNetConnection, IntegrationBase _integrationObj)
        {
            try
            {
                db_RoadNet = new InCubeDatabase();
                if (OpenRoadNetConnection)
                {
                    db_RoadNet.Open("RoadNet", "RoadNetManager");
                    RN_Conn_Opened = db_RoadNet.Opened;
                }
                db_Sonic = new InCubeDatabase();
                db_Sonic.Open("InCube", "RoadNetManager");
                integrationObj = _integrationObj;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        public bool SendLocations(DataTable dtLocations)
        {
            try
            {
                DataAccess.MainFunctions.ErrorHandler("locations");
                Business.Locations L = new Business.Locations();
                return L.ImportLocations(dtLocations);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return false;
            }
        }
        public bool SendLocationExtensions(DataTable dtLocationExtensions)
        {
            try
            {
                DataAccess.MainFunctions.ErrorHandler("location extensions");
                Business.LocationExtensions L = new Business.LocationExtensions();
                return L.ImportLocations(dtLocationExtensions);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return false;
            }
        }
        
        public bool SendSKUs(DataTable dtSKUs)
        {
            try
            {
                DataAccess.MainFunctions.ErrorHandler("SKUs");
                Business.SKUs S = new Business.SKUs();
                return S.ImportSKUs(dtSKUs);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return false;
            }
        }
        public bool SendPackageTypes(DataTable dtPackageTypes)
        {
            try
            {
                DataAccess.MainFunctions.ErrorHandler("PackageTypes");
                Business.PackageTypes P = new Business.PackageTypes();
                return P.ImportPackageTypes(dtPackageTypes);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return false;
            }
        }
        public bool SendOrders(DataTable dtOrders, DateTime SessionDate, bool DirectSend, ref string PRN_Path)
        {
            bool result = false;
            try
            {
                if (DirectSend)
                {
                    DataAccess.MainFunctions.ErrorHandler("Orders");
                    Business.Orders O = new Business.Orders();
                    result = O.ImportOrders(dtOrders, false, SessionDate, false);
                }
                else
                {
                    result = SaveTableToPRN(dtOrders, ref PRN_Path) == Result.Success;
                }
                return result;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return false;
            }
        }
        private Result SaveTableToPRN(DataTable dtTable, ref string FileName)
        {
            Result res = Result.UnKnown;
            try
            {
                DataTable dtPRNConfig = new DataTable();
                res = GetPRNConfig(ref dtPRNConfig);
                if (res == Result.Success)
                {
                    StringBuilder prn = new StringBuilder();
                    foreach (DataRow dr in dtTable.Rows)
                    {
                        for (int i = 0; i < dtPRNConfig.Rows.Count; i++)
                        {
                            string colName = dtPRNConfig.Rows[i]["ColumnName"].ToString();
                            int Width = Convert.ToInt16(dtPRNConfig.Rows[i]["Width"]);
                            string value = "";
                            if (dtTable.Columns.Contains(colName))
                            {
                                value = dr[colName].ToString();
                            }
                            value = value.Substring(0, Math.Min(Width, value.Length)).PadRight(Width);
                            prn.Append(value);
                        }
                        prn.AppendLine();
                    }
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(CoreGeneral.Common.StartupPath + "\\DataSources.xml");
                    string PRN_Path = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'PRNExportPath']/Data").InnerText;
                    FileName = PRN_Path.TrimEnd(new char[] { '\\' }) + "\\" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".prn";
                    File.AppendAllText(FileName, prn.ToString());
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                res = Result.Failure;
            }
            return res;
        }

        //public Result GetLastNSonicSessions(int N, ref DataTable dtSessions)
        //{
        //    Result res = Result.UnKnown;
        //    try
        //    {
        //        InCubeQry = new InCubeQuery(db_Sonic, string.Format("SELECT {0} SessionID,SessionName FROM Int_SonicOrderSessions ORDER BY SessionID DESC", N == -1 ? "" : "TOP " + N));
        //        InCubeQry.Execute();
        //        dtSessions = InCubeQry.GetDataTable();
        //        res = Result.Success;
        //    }
        //    catch (Exception ex)
        //    {
        //        res = Result.Failure;
        //        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
        //    }
        //    return res;
        //}
        //        public Result AddNewSonicSession(string SessionName)
        //        {
        //            Result res = Result.UnKnown;
        //            try
        //            {
        //                InCubeQry = new InCubeQuery(db_Sonic, string.Format(@"DECLARE @SessionID INT = -1
        //SELECT @SessionID = ISNULL(MAX(SessionID),0)+1 FROM Int_SonicOrderSessions
        //SELECT @SessionID
        //INSERT INTO Int_SonicOrderSessions (SessionID, SessionName, CreatedBy, CreationDate)
        //VALUES (@SessionID,'{0}',{1},GETDATE())", SessionName, CoreGeneral.Common.CurrentSession.UserID));
        //                InCubeQry.ExecuteNonQuery();
        //                res = Result.Success;
        //            }
        //            catch (Exception ex)
        //            {
        //                res = Result.Failure;
        //                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
        //            }
        //            return res;
        //        }

        public Result GetRoadNetSessions(DateTime SessionDate, ref DataTable dtSessions)
        {
            Result res = Result.UnKnown;
            try
            {
                InCubeQry = new InCubeQuery(db_RoadNet, string.Format("SELECT DISTINCT REGION_ID, DESCRIPTION FROM TSDBA.Sonic_Integration_RNJP WHERE SESSION_DATE = '{0}'", SessionDate.ToString("yyyy-MM-dd")));
                InCubeQry.Execute();
                dtSessions = InCubeQry.GetDataTable();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result GetTPSessionsForRegion(string RegionID, ref DataTable dtSessions)
        {
            Result res = Result.UnKnown;
            try
            {
                InCubeQry = new InCubeQuery(db_RoadNet, string.Format("SELECT DISTINCT SESSION_DESCRIPTION SessionName FROM Sonic_Integration_TPJP WHERE REGION_ID = '{0}'", RegionID));
                InCubeQry.Execute();
                dtSessions = InCubeQry.GetDataTable();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result GetTPSessionDetails(string SessionName, ref DataTable dtSessionDetails)
        {
            Result res = Result.UnKnown;
            try
            {
                dtSessionDetails = new DataTable();
                InCubeQry = new InCubeQuery(db_RoadNet, string.Format("SELECT * FROM Sonic_Integration_TPJP WHERE SESSION_DESCRIPTION = '{0}'", SessionName));
                InCubeQry.Execute();
                dtSessionDetails = InCubeQry.GetDataTable();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        
        public Result ExportSessionDetailsToSonic(DateTime SessionDate, string SessionDescription, string RegionID)
        {
            Result res = Result.UnKnown;
            DataTable dtSessionDetails = new DataTable();
            try
            {
                string SessionFilter = "";
                if (SessionDescription != "-1")
                {
                    SessionFilter = string.Format(" AND DESCRIPTION = '{0}' AND REGION_ID = '{1}'", SessionDescription.Replace("'", "''"), RegionID);
                }
                InCubeQry = new InCubeQuery(db_RoadNet, string.Format("SELECT * FROM TSDBA.Sonic_Integration_RNJP WHERE SESSION_DATE = '{0}' {1}", SessionDate.ToString("yyyy-MM-dd"), SessionFilter));
                InCubeQry.Execute();
                dtSessionDetails = InCubeQry.GetDataTable();
                SaveTable(dtSessionDetails, "Stg_RoadNetSession");
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result ExportTPSessionDetailsToSonic(DataTable dtSessionDetails)
        {
            Result res = Result.UnKnown;
            try
            {
                SaveTable(dtSessionDetails, "Stg_JourneyPlan");
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result SaveTable(DataTable dtData1, string TableName)
        {
            Result res = Result.Failure;
            try
            {
                DataTable dtData2 = dtData1.Copy();
                dtData2.Columns.Add("ID", typeof(int));
                dtData2.Columns.Add("ResultID", typeof(int));
                dtData2.Columns.Add("Message", typeof(string));
                dtData2.Columns.Add("Inserted", typeof(bool));
                dtData2.Columns.Add("Updated", typeof(bool));
                dtData2.Columns.Add("Skipped", typeof(bool));
                for (int i = 0; i < dtData2.Rows.Count; i++)
                {
                    dtData2.Rows[i]["ID"] = (i + 1);
                }
                string columns = "";
                foreach (DataColumn col in dtData2.Columns)
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

                InCubeQry = new InCubeQuery(db_Sonic, dataTableCreationQuery);
                if (InCubeQry.Execute() == InCubeErrors.Success)
                {
                    SqlBulkCopy bulk = new SqlBulkCopy(db_Sonic.GetConnection());
                    bulk.DestinationTableName = TableName;
                    bulk.WriteToServer(dtData2);
                    res = Result.Success;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result GetRoadNetRegions(ref DataTable dtRegions)
        {
            Result res = Result.UnKnown;
            try
            {
                InCubeQry = new InCubeQuery(db_RoadNet, "SELECT REGION_ID FROM TSDBA.TS_REGION");
                InCubeQry.Execute();
                dtRegions = InCubeQry.GetDataTable();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result GetSalespersonGroupsAssignedToOperator(ref DataTable dtSecurityGroups)
        {
            Result res = Result.UnKnown;
            try
            {
                InCubeQry = new InCubeQuery(db_Sonic, @"SELECT G.*
FROM OperatorSecurityGroup O
INNER JOIN (
SELECT DISTINCT SG.SecurityGroupID, SGL.Description
FROM SecurityGroup SG
INNER JOIN SecurityGroupLanguage SGL ON SGL.SecurityGroupID = SG.SecurityGroupID AND SGL.LanguageID = 1
INNER JOIN OperatorSecurityGroup OSG ON OSG.SecurityGroupID = SG.SecurityGroupID
INNER JOIN EmployeeOperator EO ON EO.OperatorID = OSG.OperatorID
INNER JOIN Employee E ON E.EmployeeID = EO.EmployeeID
WHERE E.EmployeeTypeID = 2) G ON G.SecurityGroupID = O.SecurityGroupID
WHERE O.OperatorID = " + CoreGeneral.Common.CurrentSession.UserID.ToString());
                InCubeQry.Execute();
                dtSecurityGroups = InCubeQry.GetDataTable();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        
        public Result GetDefinedRegionsInSonic(ref DataTable dtRegions)
        {
            Result res = Result.UnKnown;
            try
            {
                InCubeQry = new InCubeQuery(db_Sonic, "SELECT Region FROM RoadNet_Regions");
                InCubeQry.Execute();
                dtRegions = InCubeQry.GetDataTable();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result GetDefinedRegionsForEmployee(ref DataTable dtRegions)
        {
            Result res = Result.UnKnown;
            try
            {
                InCubeQry = new InCubeQuery(db_Sonic, string.Format(@"SELECT EO.OrganizationID RegionID, CAST(EO.OrganizationID AS NVARCHAR(10)) + ' - ' + O.Description RegionName
FROM EmployeeOrganization EO
INNER JOIN OrganizationLanguage O ON O.OrganizationID = EO.OrganizationID AND O.LanguageID = 1
WHERE EO.EmployeeID = {0}", CoreGeneral.Common.CurrentSession.EmployeeID));
                InCubeQry.Execute();
                dtRegions = InCubeQry.GetDataTable();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result GetRouteRegions(ref DataTable dtRouteRegion)
        {
            Result res = Result.UnKnown;
            try
            {
                InCubeQry = new InCubeQuery(db_Sonic, @"SELECT T.TerritoryID,T.TerritoryCode,R.Region, R.Region PreRegion
FROM Territory T
LEFT JOIN Route_Region R ON R.TerritoryID = T.TerritoryID");
                InCubeQry.Execute();
                dtRouteRegion = InCubeQry.GetDataTable();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result GetStandardInstructions(ref DataTable dtStandardInstructions)
        {
            Result res = Result.UnKnown;
            try
            {
                InCubeQry = new InCubeQuery(db_Sonic, @"SELECT CO.CustomerID,CO.OutletID,C.CustomerCode,CO.CustomerCode OutletCode,SI.StandardInstructions,SI.StandardInstructions PreSP
FROM CustomerOutlet CO
INNER JOIN Customer C ON C.CustomerID = CO.CustomerID
LEFT JOIN RoadNetStandardInstructions SI ON SI.CustomerID = CO.CustomerID AND SI.OutletID = CO.OutletID");
                InCubeQry.Execute();
                dtStandardInstructions = InCubeQry.GetDataTable();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result GetImportResults(DateTime SessionDate, ref DataTable dtResults)
        {
            Result res = Result.UnKnown;
            dtResults = new DataTable();
            try
            {
                Procedure Proc = new Procedure("sp_GetRoadNetImportResults");
                Proc.AddParameter("@SessionDate", ParamType.DateTime, SessionDate);
                res = integrationObj.ExecuteStoredProcedureWithTableOutput(Proc, ref dtResults);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                res = Result.Failure;
            }
            return res;
        }
        public Result GetJourneyPlanImportResults(int TriggerID, ref DataTable dtResults)
        {
            Result res = Result.UnKnown;
            dtResults = new DataTable();
            try
            {
                Procedure Proc = new Procedure("sp_GetJourneyPlanImportResults");
                Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID);
                res = integrationObj.ExecuteStoredProcedureWithTableOutput(Proc, ref dtResults);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                res = Result.Failure;
            }
            return res;
        }
        public Result UpdateStandardInstructions(Dictionary<string, string> StandardInstructions)
        {
            Result res = Result.UnKnown;
            try
            {
                foreach (KeyValuePair<string, string> pair in StandardInstructions)
                {
                    string[] CustOutID = pair.Key.Split(new char[] { ':' });
                    InCubeQry = new InCubeQuery(db_Sonic, string.Format(@"
IF EXISTS (SELECT * FROM RoadNetStandardInstructions WHERE CustomerID = {0} AND OutletID = {1})
	UPDATE RoadNetStandardInstructions SET StandardInstructions = '{2}' WHERE CustomerID = {0} AND OutletID = {1}
ELSE
	INSERT INTO RoadNetStandardInstructions (CustomerID, OutletID, StandardInstructions) VALUES ({0},{1},'{2}')", CustOutID[0], CustOutID[1], pair.Value));
                    if (InCubeQry.ExecuteNonQuery() != InCubeErrors.Success)
                    {
                        res = Result.Failure;
                        break;
                    }
                }
                res = Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                res = Result.Failure;
            }
            return res;
        }
        public Result GetStandardInstructionsExcelImportType(ref int ExcelImportTypeID)
        {
            Result res = Result.Failure;
            try
            {
                InCubeQry = new InCubeQuery(db_Sonic, "SELECT ImportTypeID FROM Int_ExcelImportTypes WHERE Name = 'RoadNet Standard Instructions'");

                object typeID = null;
                if (InCubeQry.ExecuteScalar(ref typeID) == InCubeErrors.Success)
                {
                    if (typeID != null && typeID != DBNull.Value)
                    {
                        ExcelImportTypeID = Convert.ToInt16(typeID);
                        res = Result.Success;
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
        public Result UpdateRouteRegion(Dictionary<int, string> RouteRegion)
        {
            Result res = Result.UnKnown;
            try
            {
                foreach (KeyValuePair<int, string> pair in RouteRegion)
                {
                    InCubeQry = new InCubeQuery(db_Sonic, string.Format(@"
IF EXISTS (SELECT * FROM Route_Region WHERE TerritoryID = {0})
	UPDATE Route_Region SET Region = '{1}' WHERE TerritoryID = {0}
ELSE
	INSERT INTO Route_Region (TerritoryID, Region) VALUES ({0},'{1}')", pair.Key, pair.Value));
                    if (InCubeQry.ExecuteNonQuery() != InCubeErrors.Success)
                    {
                        res = Result.Failure;
                        break;
                    }
                }
                res = Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                res = Result.Failure;
            }
            return res;
        }

        public Result GetCustomerGroups(ref DataTable dtGroups)
        {
            Result res = Result.UnKnown;
            try
            {
                Procedure Proc = new Procedure("sp_GetCustomerGroupsForUser");
                Proc.AddParameter("@EmployeeID", ParamType.Integer, CoreGeneral.Common.CurrentSession.EmployeeID);
                dtGroups = new DataTable();
                res = integrationObj.ExecuteStoredProcedureWithTableOutput(Proc, ref dtGroups);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                res = Result.Failure;
            }
            return res;
        }
        public Result GetLocations(int GroupID, ref DataTable dtLocations)
        {
            Result res = Result.UnKnown;
            try
            {
                Procedure Proc = new Procedure("sp_GetLocationsForUser");
                Proc.AddParameter("@EmployeeID", ParamType.Integer, CoreGeneral.Common.CurrentSession.EmployeeID);
                Proc.AddParameter("@GroupID", ParamType.Integer, GroupID);
                dtLocations = new DataTable();
                res = integrationObj.ExecuteStoredProcedureWithTableOutput(Proc, ref dtLocations);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                res = Result.Failure;
            }
            return res;
        }
        public Result GetRoadNetOrdersTable(DateTime OrderDateFrom, DateTime OrderDateTo, DateTime DeliveryDateFrom, DateTime DeliveryDateTo, string Region, string OrderID, int ChannelFilter, bool IsSample, ref DataTable dtOrderDetails)
        {
            Result res = Result.UnKnown;
            try
            {
                Procedure Proc = new Procedure("sp_GetRoadNetOrders");
                Proc.AddParameter("@OrderDateFrom", ParamType.DateTime, OrderDateFrom.Date);
                Proc.AddParameter("@OrderDateTo", ParamType.DateTime, OrderDateTo.Date);
                Proc.AddParameter("@DeliveryDateFrom", ParamType.DateTime, DeliveryDateFrom.Date);
                Proc.AddParameter("@DeliveryDateTo", ParamType.DateTime, DeliveryDateTo.Date);
                Proc.AddParameter("@Region", ParamType.Nvarchar, Region);
                Proc.AddParameter("@OrderID", ParamType.Nvarchar, OrderID);
                Proc.AddParameter("@ChannelFilter", ParamType.Integer, ChannelFilter);
                Proc.AddParameter("@GetSample", ParamType.BIT, IsSample ? 1 : 0);
                

                dtOrderDetails = new DataTable();
                res = integrationObj.ExecuteStoredProcedureWithTableOutput(Proc, ref dtOrderDetails);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                res = Result.Failure;
            }
            return res;
        }
        public Result GetPRNConfig(ref DataTable dtPRN)
        {
            Result res = Result.UnKnown;
            try
            {
                InCubeQry = new InCubeQuery(db_Sonic, "SELECT * FROM Int_PRNConfig ORDER BY Position");
                InCubeQry.Execute();
                dtPRN = InCubeQry.GetDataTable();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result SavePRNConfig(DataTable dtPRNConfig)
        {
            Result res = Result.UnKnown;
            InCubeTransaction Trans = new InCubeTransaction();
            try
            {
                Trans.BeginTransaction(db_Sonic);
                InCubeQry = new InCubeQuery(db_Sonic, "DELETE FROM Int_PRNConfig");
                if (InCubeQry.ExecuteNoneQuery(Trans) == InCubeErrors.Success)
                {
                    for (int i = 0; i < dtPRNConfig.Rows.Count; i++)
                    {
                        InCubeQry = new InCubeQuery(db_Sonic, 
                            string.Format("INSERT INTO Int_PRNConfig (ColumnName,Position,Width) VALUES ('{0}',{1},{2})"
                            , dtPRNConfig.Rows[i]["ColumnName"], dtPRNConfig.Rows[i]["Position"], dtPRNConfig.Rows[i]["Width"]));
                        if (InCubeQry.ExecuteNoneQuery(Trans) != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            break;
                        }
                    }
                    if (res != Result.Failure)
                        res = Result.Success;
                }
                else
                {
                    res = Result.Failure;
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                if (res == Result.Success)
                {
                    Trans.Commit();
                }
                else if (res == Result.Failure)
                {
                    Trans.Rollback();
                }
            }
            return res;
        }
    }
}
