using InCubeIntegration_DAL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;


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
        public bool SendOrders(DataTable dtOrders, DateTime SessionDate)
        {
            try
            {
                DataAccess.MainFunctions.ErrorHandler("Orders");
                Business.Orders O = new Business.Orders();
                bool result = O.ImportOrders(dtOrders, false, SessionDate, false);
                //if (!result)
                //    result = O.ImportOrders(dtOrders, false, SessionDate, false);
                return result;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return false;
            }
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
                for (int i = 0; i < dtData.Rows.Count; i++)
                {
                    dtData.Rows[i]["ID"] = (i + 1);
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

                InCubeQry = new InCubeQuery(db_Sonic, dataTableCreationQuery);
                if (InCubeQry.Execute() == InCubeErrors.Success)
                {
                    SqlBulkCopy bulk = new SqlBulkCopy(db_Sonic.GetConnection());
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
        public Result GetSpecialInstructions(ref DataTable dtSpecialInstructions)
        {
            Result res = Result.UnKnown;
            try
            {
                InCubeQry = new InCubeQuery(db_Sonic, @"SELECT CO.CustomerID,CO.OutletID,C.CustomerCode,CO.CustomerCode OutletCode,SI.SpecialInstructions,SI.SpecialInstructions PreSP
FROM CustomerOutlet CO
INNER JOIN Customer C ON C.CustomerID = CO.CustomerID
LEFT JOIN RoadNetSpecialInstructions SI ON SI.CustomerID = CO.CustomerID AND SI.OutletID = CO.OutletID");
                InCubeQry.Execute();
                dtSpecialInstructions = InCubeQry.GetDataTable();
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
        public Result UpdateSpecialInstructions(Dictionary<string, string> SpecialInstructions)
        {
            Result res = Result.UnKnown;
            try
            {
                foreach (KeyValuePair<string, string> pair in SpecialInstructions)
                {
                    string[] CustOutID = pair.Key.Split(new char[] { ':' });
                    InCubeQry = new InCubeQuery(db_Sonic, string.Format(@"
IF EXISTS (SELECT * FROM RoadNetSpecialInstructions WHERE CustomerID = {0} AND OutletID = {1})
	UPDATE RoadNetSpecialInstructions SET SpecialInstructions = '{2}' WHERE CustomerID = {0} AND OutletID = {1}
ELSE
	INSERT INTO RoadNetSpecialInstructions (CustomerID, OutletID, SpecialInstructions) VALUES ({0},{1},'{2}')", CustOutID[0], CustOutID[1], pair.Value));
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
        public Result GetSpecialInstructionsExcelImportType(ref int ExcelImportTypeID)
        {
            Result res = Result.Failure;
            try
            {
                InCubeQry = new InCubeQuery(db_Sonic, "SELECT ImportTypeID FROM Int_ExcelImportTypes WHERE Name = 'RoadNet Special Instructions'");

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
    }
}
