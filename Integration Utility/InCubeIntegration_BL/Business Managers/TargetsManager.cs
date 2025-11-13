using InCubeLibrary;
using System;
using System.Data;
using System.Data.SqlClient;
using InCubeIntegration_DAL;
using System.Data.OleDb;


namespace InCubeIntegration_BL
{
    public class TargetsManager
    {
        InCubeDatabase db_Targets;
        int OrganizationID;
        public delegate void WriteMessageDel(string Message);
        public WriteMessageDel WriteMessageHandler;
        InCubeQuery incubeQry;
       
        enum requiredColumns
        {
            ID = 0,
            CustomerCode = 1,
            ItemCode = 2,
            UOM = 3,
            Quantity = 4,
            Value = 5
        }

        public TargetsManager(int organizationID)
        {
            db_Targets = new InCubeDatabase();
            db_Targets.Open("InCube", "TargetsManager");
            OrganizationID = organizationID;
        }

        public void WriteMessage(string Message)
        {
            if (WriteMessageHandler != null && !CoreGeneral.Common.IsTesting)
                WriteMessageHandler(Message);
        }

        
        public Result ImportTargets(string SheetName, int Year, int Month, OleDbConnection excelConn)
        {
            try
            {
                if (excelConn.State == ConnectionState.Closed)
                    excelConn.Open();

                WriteMessage("Validating file format ..");
                string where = " WHERE ";
                string queryString = "SELECT ";
                DataTable dtData = excelConn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, null);
                foreach (string colName in Enum.GetNames(typeof(requiredColumns)))
                {
                    queryString += "[" + colName + "],";
                    where += "[" + colName + "] IS NOT NULL AND ";
                    
                    DataRow[] drow = dtData.Select("TABLE_NAME = '" + SheetName + "' AND COLUMN_NAME = '" + colName + "'");
                    if (drow.Length == 0)
                    {
                        WriteMessage("Column [" + colName + "] doesn't exist in sheet " + SheetName);
                        return Result.Failure;
                    }
                }

                queryString = queryString.Substring(0, queryString.Length - 1) + " FROM [" + SheetName + "] " + where.Substring(0, where.Length - 5);

                WriteMessage("Reading file contents ..");
                OleDbCommand excelCmd = new OleDbCommand(queryString, excelConn);
                excelCmd.CommandTimeout = 3600000;
                OleDbDataReader dr = excelCmd.ExecuteReader();
                incubeQry = new InCubeQuery(db_Targets, "TRUNCATE TABLE VolumeTargets");
                if (incubeQry.ExecuteNonQuery() != InCubeErrors.Success)
                {
                    WriteMessage("Error in reading targets data !!");
                    return Result.Failure;
                }

                try
                {
                    SqlBulkCopy bulk = new SqlBulkCopy(db_Targets.GetConnection());
                    bulk.DestinationTableName = "VolumeTargets";
                    bulk.BulkCopyTimeout = 3600000;
                    bulk.WriteToServer(dr);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                    WriteMessage("Error in reading targets data !!");
                    return Result.Failure;
                }

                WriteMessage("Processing provided data ..");
                incubeQry = new InCubeQuery(db_Targets, "sp_PrepareTargetsData");
                incubeQry.AddParameter("@OrganizationID", OrganizationID);
                if (incubeQry.ExecuteStoredProcedure() != InCubeErrors.Success)
                {
                    WriteMessage("Error in processing targets data");
                    return Result.Failure;
                }

                WriteMessage("Deleting old defined targets ..");
                incubeQry = new InCubeQuery(db_Targets, "sp_DeleteVolumeTargets");
                incubeQry.AddParameter("@Year", Year);
                incubeQry.AddParameter("@Month", Month);
                incubeQry.AddParameter("@OrganizationID", OrganizationID);
                if (incubeQry.ExecuteStoredProcedure() != InCubeErrors.Success)
                {
                    WriteMessage("Error in deleting old defined targets");
                    return Result.Failure;
                }

                WriteMessage("Defining new targets ..");
                incubeQry = new InCubeQuery(db_Targets, "sp_InsertVolumeTargets");
                incubeQry.AddParameter("@Year", Year);
                incubeQry.AddParameter("@Month", Month);
                if (incubeQry.ExecuteStoredProcedure() != InCubeErrors.Success)
                {
                    WriteMessage("Error in adding new targets");
                    return Result.Failure;
                }

                WriteMessage("Targets added successfully .. Retrieving results ..");
                return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
            finally
            {
                if (excelConn != null && excelConn.State == ConnectionState.Open)
                {
                    excelConn.Close();
                }
            }
        }

        public DataTable RetrieveResults()
        {
            DataTable dtResults = new DataTable();
            try
            {
                incubeQry = new InCubeQuery(db_Targets, @"SELECT DISTINCT TBL.ID XlsRowID,TBL.CustomerCode,TBL.ItemCode,TBL.GivenUOM,TBL.GivenQty,TBL.GivenValue
,TBL.TargetUOM, CASE WHEN TBL.TargetUOM IS NULL THEN NULL ELSE TBL.EquivalencyFactor END EquivalencyFactor,
CASE WHEN TBL.TargetUOM IS NULL THEN NULL ELSE TBL.GivenQty/TBL.EquivalencyFactor END TargetQty,
CASE WHEN TBL.TargetUOM IS NULL THEN NULL ELSE TBL.GivenValue/TBL.EquivalencyFactor END TargetValue,TBL.Result FROM (
SELECT VT.ID,VT.CustomerCode,VT.ItemCode,VT.UOM GivenUOM,VT.Quantity GivenQty,VT.Value GivenValue,PTL.Description TargetUOM,
CASE PTL.Description WHEN 'CX' THEN (CASE VT.UOM WHEN 'Kg' THEN (CASE P.Weight WHEN 0 THEN 1 ELSE P.Weight END) ELSE 1 END) ELSE 1 END EquivalencyFactor,
(CASE WHEN I.ItemID IS NULL THEN 'Item code not defined' ELSE
(CASE WHEN CO.CustomerID IS NULL THEN 'Customer code not defined' ELSE
(CASE WHEN P.PackID IS NULL THEN 'Item has no pack' ELSE 'Success' END) END) END) Result
FROM VolumeTargets VT
LEFT JOIN CustomerOutlet CO ON CO.CustomerCode = VT.CustomerCode
LEFT JOIN Item I ON I.ItemCode = VT.ItemCode
LEFT JOIN Pack P ON P.ItemID = I.ItemID
LEFT JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1) TBL");

                dtResults = new DataTable();
                if (incubeQry.Execute() == InCubeErrors.Success)
                    dtResults = incubeQry.GetDataTable();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return dtResults;
        }
    }
}
