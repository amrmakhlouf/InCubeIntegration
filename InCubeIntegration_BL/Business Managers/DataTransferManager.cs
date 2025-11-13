using InCubeIntegration_DAL;
using InCubeLibrary;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace InCubeIntegration_BL
{
    public class DataTransferManager : System.IDisposable
    {
        InCubeDatabase db;
        InCubeQuery incubeQuery;

        DataBaseType SrcDbType;
        OracleConnection oracleSrcConn = null;
        OracleCommand oracleSrcCMD = null;
        SqlConnection sqlSrcConn = null;
        SqlCommand sqlSrcCMD = null;

        DataBaseType DestDbType;
        OracleConnection oracleDestConn = null;
        OracleCommand oracleDestCMD = null;
        SqlConnection sqlDestConn = null;
        SqlCommand sqlDestCMD = null;
        public DataTransferManager()
        {
            db = new InCubeDatabase();
            db.Open("InCube", "DataTransferManager");
        }
        public DataTransferManager(bool OpenDB)
        {
            if (OpenDB)
            {
                db = new InCubeDatabase();
                db.Open("InCube", "DataTransferManager");
            }
        }
        public int GetMaxTransferTypeID()
        {
            int ID = 0;
            try
            {
                incubeQuery = new InCubeQuery(db, "SELECT ISNULL(MAX(ID),0)+1 FROM Int_DataTransferType");
                object field = null;
                incubeQuery.ExecuteScalar(ref field);
                ID = Convert.ToInt16(field);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return ID;
        }
        public int GetMaxDestinationDatabaseID()
        {
            int ID = 0;
            try
            {
                incubeQuery = new InCubeQuery(db, "SELECT ISNULL(MAX(ID),0)+1 FROM Int_DatabaseConnection");
                object field = null;
                incubeQuery.ExecuteScalar(ref field);
                ID = Convert.ToInt16(field);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return ID;
        }
        public Result GetTransferTypeDetails(int ID, ref string Name, ref string SelectQuery, ref int SourceDatabaseID, ref int DestinationDatabaseID, ref string DestinationTable, ref int TransferMethodID, ref bool HasIdentityColumn, ref string PrimaryKeyColumns, ref string ConstantValues)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, "SELECT * FROM Int_DataTransferType WHERE ID = " + ID);
                InCubeErrors err = incubeQuery.Execute();
                if (err == InCubeErrors.Success)
                {
                    DataTable dtDetails = incubeQuery.GetDataTable();
                    if (dtDetails != null && dtDetails.Rows.Count == 1)
                    {
                        Name = dtDetails.Rows[0]["Name"].ToString();
                        SelectQuery = dtDetails.Rows[0]["SelectQuery"].ToString();
                        SourceDatabaseID = Convert.ToInt16(dtDetails.Rows[0]["SourceDatabaseID"]);
                        DestinationDatabaseID = Convert.ToInt16(dtDetails.Rows[0]["DestinationDatabaseID"]);
                        DestinationTable = dtDetails.Rows[0]["DestinationTable"].ToString();
                        TransferMethodID = Convert.ToInt16(dtDetails.Rows[0]["TransferMethodID"]);
                        PrimaryKeyColumns = dtDetails.Rows[0]["PrimaryKeyColumns"].ToString();
                        HasIdentityColumn = Convert.ToBoolean(dtDetails.Rows[0]["HasIdentityColumn"]);
                        ConstantValues = dtDetails.Rows[0]["ConstantValues"].ToString();
                        res = Result.Success;
                    }
                    else
                    {
                        res = Result.NoRowsFound;
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
        public Result GetDatabaseConnectionDetails(int ID, ref string Name, ref int DatabaseTypeID, ref string ConnectionString)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, "SELECT * FROM Int_DatabaseConnection WHERE ID = " + ID);
                InCubeErrors err = incubeQuery.Execute();
                if (err == InCubeErrors.Success)
                {
                    DataTable dtDetails = incubeQuery.GetDataTable();
                    if (dtDetails != null && dtDetails.Rows.Count == 1)
                    {
                        Name = dtDetails.Rows[0]["Name"].ToString();
                        DatabaseTypeID = Convert.ToInt16(dtDetails.Rows[0]["DatabaseTypeID"]);
                        ConnectionString = dtDetails.Rows[0]["ConnectionString"].ToString();
                        res = Result.Success;
                    }
                    else
                    {
                        res = Result.NoRowsFound;
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
        public Result TestDatabaseConnection(DataBaseType dbType, string ConnectionString)
        {
            Result res = Result.UnKnown;
            try
            {
                if (dbType == DataBaseType.Oracle)
                {
                    using (OracleConnection conn = new OracleConnection(ConnectionString))
                    {
                        conn.Open();
                        res = Result.Success;
                    }
                }
                else if (dbType == DataBaseType.SQLServer)
                {
                    using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(ConnectionString))
                    {
                        conn.Open();
                        res = Result.Success;
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
        public Result SaveTransferType(int ID, string Name, string SelectQuery, int SourceDatabaseID, int DestinationDatabaseID, string DestinationTable, bool HasIdentityColumn, TransferMethod transferMethod, string PrimaryKeyColumns, string ConstantValues, TransferTypes TransType)
        {
            Result res = Result.UnKnown;
            try
            {
                if (transferMethod == TransferMethod.DeleteAndInsert)
                    PrimaryKeyColumns = "";
                incubeQuery = new InCubeQuery(db, "SELECT COUNT(*) FROM Int_DataTransferType WHERE ID = " + ID);
                object fieldID = null;
                incubeQuery.ExecuteScalar(ref fieldID);
                if (fieldID.ToString() == "1")
                {
                    incubeQuery = new InCubeQuery(db, string.Format("UPDATE Int_DataTransferType SET Name = '{1}', SelectQuery = '{2}', SourceDatabaseID = {3}, DestinationDatabaseID = {4}, DestinationTable = '{5}', TransferMethodID = {6}, PrimaryKeyColumns = '{7}', HasIdentityColumn = {8}, ConstantValues = '{9}' WHERE ID = {0}"
                        , ID, Name.Replace("'", "''"), SelectQuery.Replace("'","''"), SourceDatabaseID, DestinationDatabaseID, DestinationTable.Replace("'", "''"), transferMethod.GetHashCode(), PrimaryKeyColumns, HasIdentityColumn ? 1 : 0, ConstantValues.Replace("'","''")));
                }
                else
                {
                    //incubeQuery = new InCubeQuery(db, "SELECT ISNULL(MAX(Sequence),0)+1 FROM Int_DataTransferType");
                    //object Sequence = null;
                    //incubeQuery.ExecuteScalar(ref Sequence);
                    incubeQuery = new InCubeQuery(db, string.Format("INSERT INTO Int_DataTransferType (ID,Name,SelectQuery,SourceDatabaseID,DestinationDatabaseID,DestinationTable,TransferMethodID,PrimaryKeyColumns,IsDeleted,HasIdentityColumn,ConstantValues,TransferTypeID) VALUES ({0},'{1}','{2}',{3},{4},'{5}',{6},'{7}',0,{8},'{9}',{10})"
                        , ID, Name.Replace("'", "''"), SelectQuery.Replace("'", "''"), SourceDatabaseID, DestinationDatabaseID, DestinationTable.Replace("'", "''"), transferMethod.GetHashCode(), PrimaryKeyColumns.Replace("'", "''"), HasIdentityColumn ? 1 : 0, ConstantValues.Replace("'", "''"), TransType.GetHashCode()));
                }
                if (incubeQuery.ExecuteNonQuery() == InCubeErrors.Success)
                    res = Result.Success;
                else
                    res = Result.Failure;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public Result DeleteDataTransferType(int ID)
        {
            Result res = Result.UnKnown;
            try
            {
                string deleteQuery = string.Format(@"UPDATE Int_DataTransferType SET IsDeleted = 1 WHERE ID = {0}
DELETE FROM Int_DataTrasferGroupDetails WHERE TransferTypeID = {0}", ID);
                incubeQuery = new InCubeQuery(db, deleteQuery);
                if (incubeQuery.ExecuteNonQuery() == InCubeErrors.Success)
                    res = Result.Success;
                else
                    res = Result.Failure;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public Result SaveDatabaseConnection(int ID, string Name, DataBaseType DBType, string ConnectionString)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, "SELECT COUNT(*) FROM Int_DatabaseConnection WHERE ID = " + ID);
                object fieldID = null;
                incubeQuery.ExecuteScalar(ref fieldID);
                if (fieldID.ToString() == "1")
                {
                    incubeQuery = new InCubeQuery(db, string.Format("UPDATE Int_DatabaseConnection SET Name = '{0}', DatabaseTypeID = {1}, ConnectionString = '{2}' WHERE ID = {3}"
                        , Name.Replace("'", "''"), DBType.GetHashCode(), ConnectionString.Replace("'", "''"), ID));
                }
                else
                {
                    incubeQuery = new InCubeQuery(db, string.Format("INSERT INTO Int_DatabaseConnection (ID,Name,DatabaseTypeID,ConnectionString) VALUES ({0},'{1}',{2},'{3}')"
                        , ID, Name.Replace("'", "''"), DBType.GetHashCode(), ConnectionString.Replace("'", "''")));
                }
                if (incubeQuery.ExecuteNonQuery() == InCubeErrors.Success)
                    res = Result.Success;
                else
                    res = Result.Failure;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public Result GetTransferMethodsList(ref DataTable dtDestinations)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, @"SELECT ID,Value Description FROM Int_Lookups WHERE LookupName = 'TransferMethod'");
                InCubeErrors err = incubeQuery.Execute();
                if (err == InCubeErrors.Success)
                {
                    dtDestinations = incubeQuery.GetDataTable();
                    if (dtDestinations != null && dtDestinations.Rows.Count > 0)
                    {
                        res = Result.Success;
                    }
                    else
                    {
                        res = Result.NoRowsFound;
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
        public Result GetConnectionsList(int DB_Type_Filter, ref DataTable dtDestinations)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, string.Format(@"SELECT * FROM (
SELECT 0 ID,'Utility Connection' Name
UNION
SELECT ID,Name FROM Int_DatabaseConnection
{0}
) T
ORDER BY ID", DB_Type_Filter != -1 ? "WHERE DataBaseTypeID = " + DB_Type_Filter : ""));
                InCubeErrors err = incubeQuery.Execute();
                if (err == InCubeErrors.Success)
                {
                    dtDestinations = incubeQuery.GetDataTable();
                    if (dtDestinations != null && dtDestinations.Rows.Count > 0)
                    {
                        res = Result.Success;
                    }
                    else
                    {
                        res = Result.NoRowsFound;
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
        public Result GetTransferGroups(ref DataTable dtTransferGroups, int TransferType, bool AddEmptyRows)
        {
            Result res = Result.UnKnown;
            try
            {
                string EmptyRows = @"UNION
SELECT -1, '',-1";
                incubeQuery = new InCubeQuery(db, string.Format(@"SELECT T.GroupID,T.GroupName,L.Value TransferType FROM (
SELECT * FROM Int_DataTrasferGroups
{0}
{1}
) T 
LEFT JOIN Int_Lookups L ON L.LookupName = 'TransferTypes' AND L.ID = T.TransferTypeID
ORDER BY GroupID", TransferType > -1 ? "WHERE TransferTypeID = " + TransferType.ToString() : "", AddEmptyRows ? EmptyRows : ""));
                InCubeErrors err = incubeQuery.Execute();
                if (err == InCubeErrors.Success)
                {
                    dtTransferGroups = incubeQuery.GetDataTable();
                    if (dtTransferGroups != null && dtTransferGroups.Rows.Count > 0)
                    {
                        res = Result.Success;
                    }
                    else
                    {
                        res = Result.NoRowsFound;
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
        //public Result UpdateTypeSequence(int ID, int Sequence)
        //{
        //    Result res = Result.UnKnown;
        //    try
        //    {
        //        incubeQuery = new InCubeQuery(db, "UPDATE Int_DataTransferType SET Sequence = " + Sequence + " WHERE ID = " + ID);
        //        if (incubeQuery.ExecuteNonQuery() == InCubeErrors.Success)
        //            res = Result.Success;
        //        else
        //            res = Result.Failure;
        //    }
        //    catch (Exception ex)
        //    {
        //        res = Result.Failure;
        //        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
        //    }
        //    return res;
        //}
        public Result UpdateTypeSequenceInGroup(int GroupID, int TransferTypeID, int Sequence)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, string.Format("UPDATE Int_DataTrasferGroupDetails SET Sequence = {0} WHERE GroupID = {1} AND TransferTypeID = {2}", Sequence, GroupID, TransferTypeID));
                if (incubeQuery.ExecuteNonQuery() == InCubeErrors.Success)
                    res = Result.Success;
                else
                    res = Result.Failure;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public Result GetActiveTransferTypes(TransferTypes transType, ref DataTable dtTransferTypes)
        {
            Result res = Result.UnKnown;
            try
            {
                //Resequence
                //                incubeQuery = new InCubeQuery(db, @"UPDATE Int_DataTransferType SET Sequence = -1 WHERE IsDeleted = 1

                //UPDATE T SET Sequence = T2.RNK FROM Int_DataTransferType T INNER JOIN (
                //SELECT RANK() OVER(ORDER BY Sequence, ID) RNK, ID FROM Int_DataTransferType WHERE IsDeleted = 0) T2
                //ON T2.ID = T.ID
                incubeQuery = new InCubeQuery(db, @"
UPDATE T SET Sequence = T2.RNK FROM Int_DataTrasferGroupDetails T INNER JOIN (
SELECT RANK() OVER(PARTITION BY GroupID ORDER BY Sequence, TransferTypeID) RNK, GroupID, TransferTypeID FROM Int_DataTrasferGroupDetails) T2
ON T2.GroupID = T.GroupID AND T2.TransferTypeID = T.TransferTypeID
");
                incubeQuery.Execute();

                //Get Types
                incubeQuery = new InCubeQuery(db, string.Format(@"SELECT T.ID,T.Name,ISNULL(S.Name,'Utility Connection') Source, ISNULL(D.Name,'Utility Connection') Destination
, T.DestinationTable, M.Value TransferMethod
FROM Int_DataTransferType T
LEFT JOIN Int_DatabaseConnection S ON S.ID = T.SourceDatabaseID
LEFT JOIN Int_DatabaseConnection D ON D.ID = T.DestinationDatabaseID
INNER JOIN Int_Lookups M ON M.ID = T.TransferMethodID AND M.LookupName = 'TransferMethod'
WHERE T.IsDeleted = 0 AND T.TransferTypeID = {0}
ORDER BY T.ID", transType.GetHashCode()));
                InCubeErrors err = incubeQuery.Execute();
                if (err == InCubeErrors.Success)
                {
                    dtTransferTypes = incubeQuery.GetDataTable();
                    if (dtTransferTypes != null && dtTransferTypes.Rows.Count > 0)
                    {
                        res = Result.Success;
                    }
                    else
                    {
                        res = Result.NoRowsFound;
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
        public Result GetGroupTransfers(int GroupID,ref DataTable dtGroupTransfers)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, string.Format(@"SELECT T.ID,T.Name,ISNULL(S.Name,'Utility Connection') Source, ISNULL(D.Name,'Utility Connection') Destination, ISNULL(GD.Sequence,-1) Sequence
FROM Int_DataTransferType T
LEFT JOIN Int_DatabaseConnection S ON S.ID = T.SourceDatabaseID
LEFT JOIN Int_DatabaseConnection D ON D.ID = T.DestinationDatabaseID
INNER JOIN Int_Lookups M ON M.ID = T.TransferMethodID AND M.LookupName = 'TransferMethod'
LEFT JOIN Int_DataTrasferGroupDetails GD ON T.ID = GD.TransferTypeID AND GD.GroupID = {0}
WHERE T.IsDeleted = 0 AND T.TransferTypeID = (SELECT TransferTypeID FROM Int_DataTrasferGroups WHERE GroupID = {0})
ORDER BY GD.Sequence, T.ID", GroupID));
                InCubeErrors err = incubeQuery.Execute();
                if (err == InCubeErrors.Success)
                {
                    dtGroupTransfers = incubeQuery.GetDataTable();
                    if (dtGroupTransfers != null && dtGroupTransfers.Rows.Count > 0)
                    {
                        res = Result.Success;
                    }
                    else
                    {
                        res = Result.NoRowsFound;
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
        public Result GetQueryExecutionColumns(string SelectQuery, ref Dictionary<string, ColumnType> ExecutionColumns)
        {
            Result res = Result.UnKnown;
            try
            {
                DataTable dtFirstRow = new DataTable();
                res = GetSourceDataTable(SelectQuery, ref dtFirstRow);
                if (res == Result.Success || res == Result.NoRowsFound)
                {
                    ExecutionColumns = new Dictionary<string, ColumnType>();
                    foreach (DataColumn col in dtFirstRow.Columns)
                    {
                        ExecutionColumns.Add(col.ColumnName, GetColumnType(col.DataType.ToString()));
                    }
                    res = Result.Success;
                }
                else
                    return Result.Failure;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public Result GetDataTransferTypesDetails(string DataTransferGroups, ref DataTable dtTransferTypesDetails)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, string.Format(@"SELECT G.GroupID,T.ID,T.Name,T.SelectQuery,T.DestinationTable,T.HasIdentityColumn,T.TransferMethodID,T.PrimaryKeyColumns
,ISNULL(C.DatabaseTypeID,0) SourceDatabaseTypeID, ISNULL(C.ConnectionString,'') SourceConnectionString
,ISNULL(D.DatabaseTypeID,0) DestinationDatabaseTypeID, ISNULL(D.ConnectionString,'') DestinationConnectionString
,ISNULL(T.ConstantValues,'') ConstantValues
FROM Int_DataTransferType T
INNER JOIN Int_DataTrasferGroupDetails G ON G.TransferTypeID = T.ID
LEFT JOIN Int_DatabaseConnection C ON C.ID = T.SourceDatabaseID
LEFT JOIN Int_DatabaseConnection D ON D.ID = T.DestinationDatabaseID
WHERE G.GroupID IN ({0}) AND T.IsDeleted = 0 ORDER BY G.GroupID,G.Sequence", DataTransferGroups));
                dtTransferTypesDetails = new DataTable();
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtTransferTypesDetails = incubeQuery.GetDataTable();
                    res = Result.Success;
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public Result InitializeSourceConnection(DataBaseType dbType, string ConnectionString)
        {
            SrcDbType = dbType;
            if (dbType == DataBaseType.Oracle)
                return InitializaOracleConnection(ConnectionString, ref oracleSrcConn);
            else if (dbType == DataBaseType.SQLServer)
                return InitializaSQLConnection(ConnectionString, ref sqlSrcConn);
            else
                return Result.Invalid;
        }
        public Result InitializeDestinationConnection(DataBaseType dbType, string ConnectionString)
        {
            DestDbType = dbType;
            if (dbType == DataBaseType.Oracle)
                return InitializaOracleConnection(ConnectionString, ref oracleDestConn);
            else if (dbType == DataBaseType.SQLServer)
                return InitializaSQLConnection(ConnectionString, ref sqlDestConn);
            else
                return Result.Invalid;
        }
        public Result InitializeDestinationConnection(int ConnectionID)
        {
            Result res = Result.UnKnown;
            try
            {
                DataBaseType DestDbType = DataBaseType.SQLServer;
                string ConnectionString = "";
                res = GetStoredConnectionDetails(ConnectionID, ref DestDbType, ref ConnectionString);
                if (res == Result.Success)
                {
                    res = InitializeDestinationConnection(DestDbType, ConnectionString);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
                res = Result.Failure;
            }
            return res;
        }
        public Result InitializeSourceConnection(int ConnectionID)
        {
            Result res = Result.UnKnown;
            try
            {
                DataBaseType SrcDbType = DataBaseType.SQLServer;
                string ConnectionString = "";
                res = GetStoredConnectionDetails(ConnectionID, ref SrcDbType, ref ConnectionString);
                if (res == Result.Success)
                {
                    res = InitializeSourceConnection(SrcDbType, ConnectionString);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
                res = Result.Failure;
            }
            return res;
        }
        public Result GetStoredConnectionDetails(int ConnID, ref DataBaseType DbType, ref string ConnStr)
        {
            Result res = Result.UnKnown;
            try
            {
                DbType = DataBaseType.SQLServer;
                ConnStr = "";
                if (ConnID != 0)
                {
                    int DatabaseTypeID = 0;
                    string Name = "";
                    res = GetDatabaseConnectionDetails(ConnID, ref Name, ref DatabaseTypeID, ref ConnStr);
                    DbType = (DataBaseType)DatabaseTypeID;
                }
                else
                {
                    ConnStr = db.GetConnection().ConnectionString;
                    res = Result.Success;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
                res = Result.Failure;
            }
            return res;
        }
        private Result InitializaOracleConnection(string ConnectionString, ref OracleConnection conn)
        {
            Result res = Result.UnKnown;
            try
            {
                conn = new OracleConnection(ConnectionString);
                conn.Open();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\nConnection String:\r\n" + ConnectionString, LoggingType.Error, LoggingFiles.errorTSFR);
                res = Result.Failure;
            }
            return res;
        }
        public Result GetSourceDataTable(string SelectQuery, ref DataTable dtData)
        {
            if (SrcDbType == DataBaseType.Oracle)
                return ExecuteOracleDataTable(SelectQuery, oracleSrcConn, ref dtData);
            else if (SrcDbType == DataBaseType.SQLServer)
                return ExecuteSQLDataTable(SelectQuery, sqlSrcConn, ref dtData);
            else
                return Result.Invalid;
        }
        public Result GetDestinationDataTable(string SelectQuery, ref DataTable dtData)
        {
            if (DestDbType == DataBaseType.Oracle)
                return ExecuteOracleDataTable(SelectQuery, oracleDestConn, ref dtData);
            else if (DestDbType == DataBaseType.SQLServer)
                return ExecuteSQLDataTable(SelectQuery, sqlDestConn, ref dtData);
            else
                return Result.Invalid;
        }
        public Result ExecuteSourceCommand(string CommandText)
        {
            if (SrcDbType == DataBaseType.Oracle)
                return ExecuteOracleCommand(CommandText, oracleSrcCMD, oracleSrcConn);
            else if (SrcDbType == DataBaseType.SQLServer)
                return ExecuteSQLCommand(CommandText, sqlSrcCMD, sqlSrcConn);
            else
                return Result.Invalid;
        }
        public Result ExecuteDestinationCommand(string CommandText)
        {
            if (DestDbType == DataBaseType.Oracle)
                return ExecuteOracleCommand(CommandText, oracleDestCMD, oracleDestConn);
            else if (DestDbType == DataBaseType.SQLServer)
                return ExecuteSQLCommand(CommandText, sqlDestCMD, sqlDestConn);
            else
                return Result.Invalid;
        }
        public Result ExecuteSourceScalar(string CommandText, ref object field)
        {
            if (SrcDbType == DataBaseType.Oracle)
                return ExecuteOracleScalar(CommandText, oracleSrcCMD, oracleSrcConn, ref field);
            else if (SrcDbType == DataBaseType.SQLServer)
                return ExecuteSQLScalar(CommandText, sqlSrcCMD, sqlSrcConn, ref field);
            else
                return Result.Invalid;
        }
        public Result ExecuteDestinationScalar(string CommandText, ref object field)
        {
            if (DestDbType == DataBaseType.Oracle)
                return ExecuteOracleScalar(CommandText, oracleDestCMD, oracleDestConn, ref field);
            else if (DestDbType == DataBaseType.SQLServer)
                return ExecuteSQLScalar(CommandText, sqlDestCMD, sqlDestConn, ref field);
            else
                return Result.Invalid;
        }
        private Result ExecuteOracleDataTable(string SelectQuery, OracleConnection conn, ref DataTable dtData)
        {
            Result res = Result.UnKnown;
            try
            {
                OracleDataAdapter adp = new OracleDataAdapter(SelectQuery, conn);
                dtData = new DataTable();
                adp.Fill(dtData);
                if (dtData.Rows.Count == 0)
                    res = Result.NoRowsFound;
                else
                    res = Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\nQuery:\r\n" + SelectQuery, LoggingType.Error, LoggingFiles.errorTSFR);
                res = Result.Failure;
            }
            return res;
        }
        private Result ExecuteOracleCommand(string QueryStr, OracleCommand cmd, OracleConnection conn)
        {
            Result res = Result.UnKnown;
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                cmd = new OracleCommand(QueryStr, conn);
                cmd.ExecuteNonQuery();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\nQuery:\r\n" + QueryStr, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        private Result ExecuteOracleScalar(string QueryStr, OracleCommand cmd, OracleConnection conn, ref object field)
        {
            Result res = Result.UnKnown;
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                cmd = new OracleCommand(QueryStr, conn);
                field = cmd.ExecuteScalar();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\nQuery:\r\n" + QueryStr, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }

        private Result InitializaSQLConnection(string ConnectionString, ref SqlConnection conn)
        {
            Result res = Result.UnKnown;
            try
            {
                conn = new SqlConnection(ConnectionString);
                conn.Open();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\nConnection String:\r\n" + ConnectionString, LoggingType.Error, LoggingFiles.errorTSFR);
                res = Result.Failure;
            }
            return res;
        }
        private Result ExecuteSQLDataTable(string SelectQuery, SqlConnection conn, ref DataTable dtData)
        {
            Result res = Result.UnKnown;
            try
            {
                SqlDataAdapter adp = new SqlDataAdapter(SelectQuery, conn);
                dtData = new DataTable();
                adp.Fill(dtData);
                if (dtData.Rows.Count == 0)
                    res = Result.NoRowsFound;
                else
                    res = Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\nQuery:\r\n" + SelectQuery, LoggingType.Error, LoggingFiles.errorTSFR);
                res = Result.Failure;
            }
            return res;
        }
        private Result ExecuteSQLCommand(string QueryStr, SqlCommand cmd, SqlConnection conn)
        {
            Result res = Result.UnKnown;
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                sqlDestCMD = new SqlCommand(QueryStr, sqlDestConn);
                sqlDestCMD.ExecuteNonQuery();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\nQuery:\r\n" + QueryStr, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        private Result ExecuteSQLScalar(string QueryStr, SqlCommand cmd, SqlConnection conn, ref object field)
        {
            Result res = Result.UnKnown;
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Close();
                cmd = new SqlCommand(QueryStr, conn);
                field = cmd.ExecuteScalar();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\nQuery:\r\n" + QueryStr, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }

        public ColumnType GetColumnType(string colTypeStr)
        {
            ColumnType colType = ColumnType.String;
            try
            {
                switch (colTypeStr)
                {
                    case "System.DateTime":
                        colType = ColumnType.Datetime;
                        break;
                    case "System.Int16":
                    case "System.Int32":
                    case "System.Int64":
                        colType = ColumnType.Int;
                        break;
                    case "System.Boolean":
                        colType = ColumnType.Bool;
                        break;
                    case "System.Decimal":
                        colType = ColumnType.Decimal;
                        break;
                    default:
                        colType = ColumnType.String;
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return colType;
        }
        public string GetValue(DataBaseType dbType, ColumnType columnType, object Value)
        {
            string ReturnedValue = "";
            try
            {
                if (Value == DBNull.Value)
                {
                    ReturnedValue += "NULL";
                }
                else
                {
                    if (columnType == ColumnType.Datetime)
                    {
                        if (dbType == DataBaseType.Oracle)
                            ReturnedValue = "TO_DATE('" + DateTime.Parse(Value.ToString()).ToString("yyyy/MM/dd HH:mm:ss") + "','YYYY/MM/DD HH24:MI:SS')";
                        else
                            ReturnedValue = "'" + DateTime.Parse(Value.ToString()).ToString("yyyy/MM/dd HH:mm:ss") + "'";
                    }
                    else if (columnType == ColumnType.Int || columnType == ColumnType.Decimal)
                    {
                        if (dbType == DataBaseType.Oracle)
                        {
                            if (Value.ToString().ToLower() == "true")
                                ReturnedValue = "1";
                            else if (Value.ToString().ToLower() == "false")
                                ReturnedValue = "0";
                            else
                                ReturnedValue = Value.ToString();
                        }
                        else
                        {
                            ReturnedValue = Value.ToString();
                        }
                    }
                    else if (columnType == ColumnType.Bool)
                    {
                        if (Value.ToString().ToLower() == "true" || Value.ToString().ToLower() == "1")
                            ReturnedValue = "1";
                        else
                            ReturnedValue = "0";
                    }
                    else
                    {
                        ReturnedValue = "'" + Value.ToString().Replace("'", "''") + "'";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return ReturnedValue;
        }
        public Result GenerateCheckExistanceStatement(DataBaseType destinationDB, DataRow drSourceData, string DestinationTable, Dictionary<string, ColumnType> PrimaryKeyColumns, ref string CheckExistanceStatement)
        {
            Result res = Result.UnKnown;
            try
            {
                char startChar = '[';
                char endChar = ']';
                if (destinationDB == DataBaseType.Oracle)
                {
                    startChar = '"';
                    endChar = '"';
                    DestinationTable = DestinationTable.ToUpper();
                }

                CheckExistanceStatement = string.Format("SELECT COUNT(*) FROM {0}{1}{2} WHERE ", startChar, DestinationTable, endChar);
                foreach (KeyValuePair<string, ColumnType> column in PrimaryKeyColumns)
                {
                    string ColumnName = "";
                    if (destinationDB == DataBaseType.Oracle)
                        ColumnName = column.Key.ToUpper();
                    else
                        ColumnName = column.Key;

                    CheckExistanceStatement += string.Format("{0}{1}{2} = {3} AND ", startChar, ColumnName, endChar, GetValue(destinationDB, column.Value, drSourceData[ColumnName]));
                }
                CheckExistanceStatement = CheckExistanceStatement.Substring(0, CheckExistanceStatement.Length - 5);
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public Result GenerateUpdateStatement(DataBaseType destinationDB, DataRow drSourceData, string DestinationTable, Dictionary<string, ColumnType> DestinationColumns, Dictionary<string, ColumnType> PrimaryKeyColumns, Dictionary<string, DBColumn> ConstantValues, ref string UpdateStatement)
        {
            Result res = Result.UnKnown;
            try
            {
                char startChar = '[';
                char endChar = ']';
                if (destinationDB == DataBaseType.Oracle)
                {
                    startChar = '"';
                    endChar = '"';
                    DestinationTable = DestinationTable.ToUpper();
                }

                UpdateStatement = string.Format("UPDATE {0}{1}{2} SET ", startChar, DestinationTable, endChar);
                foreach (KeyValuePair<string, ColumnType> column in DestinationColumns)
                {
                    string ColumnName = "";
                    if (destinationDB == DataBaseType.Oracle)
                        ColumnName = column.Key.ToUpper();
                    else
                        ColumnName = column.Key;

                    if (!PrimaryKeyColumns.ContainsKey(ColumnName.ToLower()))
                    {
                        object value = drSourceData[ColumnName];
                        if (ConstantValues != null && ConstantValues.ContainsKey(ColumnName.ToLower()))
                            value = ConstantValues[ColumnName.ToLower()].Value;
                        UpdateStatement += string.Format("{0}{1}{2} = {3}, ", startChar, ColumnName, endChar, GetValue(destinationDB, column.Value, value));
                    }
                }
                UpdateStatement = UpdateStatement.Substring(0, UpdateStatement.Length - 2);
                UpdateStatement += " WHERE ";
                foreach (KeyValuePair<string, ColumnType> column in PrimaryKeyColumns)
                {
                    string ColumnName = "";
                    if (destinationDB == DataBaseType.Oracle)
                        ColumnName = column.Key.ToUpper();
                    else
                        ColumnName = column.Key;

                    UpdateStatement += string.Format("{0}{1}{2} = {3} AND ", startChar, ColumnName, endChar, GetValue(destinationDB, column.Value, drSourceData[ColumnName]));
                }
                UpdateStatement = UpdateStatement.Substring(0, UpdateStatement.Length - 5);
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public Result GenerateInsertStatement(DataBaseType destinationDB, DataRow drSourceData, string DestinationTable, bool hasIdentityColumn, Dictionary<string, ColumnType> DestinationColumns, Dictionary<string, DBColumn> ConstantValues, ref string InsertStatement)
        {
            Result res = Result.UnKnown;
            try
            {
                string IdentityOn = "";
                string IdentityOff = "";
                char startChar = '[';
                char endChar = ']';
                if (destinationDB == DataBaseType.Oracle)
                {
                    startChar = '"';
                    endChar = '"';
                    DestinationTable = DestinationTable.ToUpper();
                }
                else if (hasIdentityColumn)
                {
                    IdentityOn = string.Format("SET IDENTITY_INSERT {0}{1}{2} ON", startChar, DestinationTable, endChar);
                    IdentityOff = string.Format("SET IDENTITY_INSERT {0}{1}{2} OFF", startChar, DestinationTable, endChar);
                }

                InsertStatement = string.Format(@"{3}
                    INSERT INTO {0}{1}{2} (", startChar, DestinationTable, endChar, IdentityOn);
                string valuesStatement = " VALUES (";
                foreach (KeyValuePair<string, ColumnType> column in DestinationColumns)
                {
                    string ColumnName = "";
                    if (destinationDB == DataBaseType.Oracle)
                        ColumnName = column.Key.ToUpper();
                    else
                        ColumnName = column.Key;

                    InsertStatement += string.Format("{0}{1}{2},", startChar, ColumnName, endChar);
                    object value = drSourceData[ColumnName];
                    if (ConstantValues != null && ConstantValues.ContainsKey(ColumnName.ToLower()))
                        value = ConstantValues[ColumnName.ToLower()].Value;
                    valuesStatement += GetValue(destinationDB, column.Value, value) + ",";
                }
                InsertStatement = InsertStatement.Substring(0, InsertStatement.Length - 1) + ")";
                InsertStatement += valuesStatement.Substring(0, valuesStatement.Length - 1) + ") " + IdentityOff;
                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public void CloseConnections()
        {
            try
            {
                if (oracleDestCMD != null)
                    oracleDestCMD.Dispose();
                if (oracleDestConn != null && oracleDestConn.State == ConnectionState.Open)
                {
                    oracleDestConn.Close();
                    oracleDestConn.Dispose();
                }

                if (sqlDestCMD != null)
                    sqlDestCMD.Dispose();
                if (sqlDestConn != null && sqlDestConn.State == ConnectionState.Open)
                {
                    sqlDestConn.Close();
                    sqlDestConn.Dispose();
                }

                if (oracleSrcCMD != null)
                    oracleSrcCMD.Dispose();
                if (oracleSrcConn != null && oracleSrcConn.State == ConnectionState.Open)
                {
                    oracleSrcConn.Close();
                    oracleSrcConn.Dispose();
                }

                if (sqlSrcCMD != null)
                    sqlSrcCMD.Dispose();
                if (sqlSrcConn != null && sqlSrcConn.State == ConnectionState.Open)
                {
                    sqlSrcConn.Close();
                    sqlSrcConn.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.errorTSFR);
            }
        }
        public void PrepareDestinationColumns(DataColumnCollection SourceColumns, DataColumnCollection DestColumns, ref Dictionary<string, ColumnType> DestinationColumns)
        {
            try
            {
                DestinationColumns = new Dictionary<string, ColumnType>();
                foreach (DataColumn col in DestColumns)
                {
                    if (SourceColumns.Contains(col.ColumnName))
                    {
                        string dbColType = col.DataType.ToString();
                        ColumnType colType = GetColumnType(dbColType);
                        DestinationColumns.Add(col.ColumnName, colType);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
        }
        public Result AddTransferGroup(string GroupName, int TransferType)
        {
            Result res = Result.UnKnown;
            try
            {
                int GroupID = -1;
                incubeQuery = new InCubeQuery(db, "SELECT ISNULL(MAX(GroupID),0)+1 FROM Int_DataTrasferGroups");
                object field = null;
                incubeQuery.ExecuteScalar(ref field);
                GroupID = Convert.ToInt32(field);
                string insertQuery = string.Format(@"INSERT INTO Int_DataTrasferGroups (GroupID,GroupName,TransferTypeID)
VALUES ({0},'{1}',{2})", GroupID, GroupName.Replace("'", "''"), TransferType);
                incubeQuery = new InCubeQuery(db, insertQuery);
                res = incubeQuery.ExecuteNonQuery() == InCubeErrors.Success ? Result.Success : Result.Failure;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public Result DeleteTransferGroup(int GroupID)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, string.Format(@"DELETE FROM Int_DataTrasferGroupDetails WHERE GroupID = {0}
DELETE FROM Int_DataTrasferGroups WHERE GroupID = {0}", GroupID));
                res = incubeQuery.ExecuteNonQuery() == InCubeErrors.Success ? Result.Success : Result.Failure;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public Result AddTransferTypeToGroup(int DataTransferID, int GroupID, int Seq)
        {
            Result res = Result.UnKnown;
            try
            {
                string insertQuery = string.Format(@"INSERT INTO Int_DataTrasferGroupDetails (GroupID,TransferTypeID,Sequence)
VALUES ({0},{1},{2})", GroupID, DataTransferID, Seq);
                incubeQuery = new InCubeQuery(db, insertQuery);
                res = incubeQuery.ExecuteNonQuery() == InCubeErrors.Success ? Result.Success : Result.Failure;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public Result RemoveTransferTypeFromGroup(int DataTransferID, int GroupID, int Seq)
        {
            Result res = Result.UnKnown;
            try
            {
                string insertQuery = string.Format(@"DELETE FROM Int_DataTrasferGroupDetails WHERE GroupID = {0} AND TransferTypeID = {1};
UPDATE Int_DataTrasferGroupDetails SET Sequence = Sequence - 1 WHERE Sequence > {2}", GroupID, DataTransferID, Seq);
                incubeQuery = new InCubeQuery(db, insertQuery);
                res = incubeQuery.ExecuteNonQuery() == InCubeErrors.Success ? Result.Success : Result.Failure;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public void FillConstantsDictionary(string ConstantValuesStr, ref Dictionary<string, DBColumn> ConstantValuesDic)
        {
            try
            {
                ConstantValuesDic = new Dictionary<string, DBColumn>();
                if (ConstantValuesStr.Trim() != string.Empty)
                {
                    string[] consCols = ConstantValuesStr.Split(new char[] { '|' });
                    foreach (string col in consCols)
                    {
                        string[] details = col.Split(new char[] { '&' });
                        string colName = details[0];
                        DBColumn dbCol = new DBColumn();
                        dbCol.Name = colName;
                        dbCol.Type = (ColumnType)Convert.ToInt16(details[1]);
                        dbCol.Value = details[2];
                        ConstantValuesDic.Add(colName.ToLower(), dbCol);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
        }
        public Result ReadTableSchema(string TableName, ref DataTable dtSchema)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, string.Format(@"SELECT C.name ColumnName,C.is_nullable AllowNull,Y.name ColumnType,C.max_length/2 NvarcharLength,C.precision NumericPrecision
,C.max_length NumericDigits,C.scale DecimalDigits, CASE WHEN P.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END IsPrimaryKey
FROM sys.columns C
INNER JOIN sys.tables T ON T.object_id = C.object_id
INNER JOIN sys.types Y ON Y.user_type_id = C.user_type_id
LEFT JOIN (SELECT KU.COLUMN_NAME
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS TC
INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KU ON TC.CONSTRAINT_TYPE = 'PRIMARY KEY' AND TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME
AND KU.TABLE_NAME = '{0}') P ON P.COLUMN_NAME = C.name
WHERE T.name = '{0}'", TableName));
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtSchema = incubeQuery.GetDataTable();
                    res = Result.Success;
                }
                else
                    res = Result.Failure;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public Result ReadTableSchemaAtDestDB(string TableName, ref DataTable dtSchema, ref Dictionary<string, string> TableColumns, ref List<string> PrimaryKeyColumns)
        {
            Result res = Result.UnKnown;
            try
            {
                dtSchema = new DataTable();
                string query = string.Format(@"SELECT C.name ColumnName,C.is_nullable AllowNull,Y.name ColumnType,C.max_length/2 NvarcharLength,C.precision NumericPrecision
,C.max_length NumericDigits,C.scale DecimalDigits, CASE WHEN P.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END IsPrimaryKey
FROM sys.columns C
INNER JOIN sys.tables T ON T.object_id = C.object_id
INNER JOIN sys.types Y ON Y.user_type_id = C.user_type_id
LEFT JOIN (SELECT KU.COLUMN_NAME
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS TC
INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KU ON TC.CONSTRAINT_TYPE = 'PRIMARY KEY' AND TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME
AND KU.TABLE_NAME = '{0}') P ON P.COLUMN_NAME = C.name
WHERE T.name = '{0}'", TableName);
                SqlDataAdapter adp = new SqlDataAdapter(query, sqlDestConn);
                adp.Fill(dtSchema);
                if (dtSchema == null || dtSchema.Rows.Count == 0)
                {
                    res = Result.NoRowsFound;
                }
                else
                {
                    TableColumns = new Dictionary<string, string>();
                    for (int i = 0; i < dtSchema.Rows.Count; i++)
                    {
                        TableColumns.Add(dtSchema.Rows[i]["ColumnName"].ToString().ToLower(), dtSchema.Rows[i]["ColumnName"].ToString());
                        if (Convert.ToInt16(dtSchema.Rows[i]["IsPrimaryKey"]) == 1)
                            PrimaryKeyColumns.Add(dtSchema.Rows[i]["ColumnName"].ToString());
                    }
                    res = Result.Success;
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public Result GenerateTableScriptFromSchema(DataTable dtSchema, string TableName, bool Drop, ref string TableQuery)
        {
            Result res = Result.UnKnown;
            try
            {
                StringBuilder sb = new StringBuilder();
                if (Drop)
                {
                    sb.AppendLine(string.Format(@"IF EXISTS (SELECT * FROM sys.tables WHERE name = '{0}')
BEGIN
	EXEC('DROP TABLE [{0}]')
END", TableName));
                }
                sb.AppendLine(string.Format(@"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{0}')
BEGIN
	EXEC('CREATE TABLE [dbo].[{0}](", TableName));
                foreach (DataRow dr in dtSchema.Rows)
                {
                    string Name = dr["ColumnName"].ToString();
                    string Type = dr["ColumnType"].ToString();
                    int NvarcharLength = Convert.ToInt16(dr["NvarcharLength"]);
                    int NumericPrecision = Convert.ToInt16(dr["NumericPrecision"]);
                    int NumericDigits = Convert.ToInt16(dr["NumericDigits"]);
                    int DecimalDigits = Convert.ToInt16(dr["DecimalDigits"]);
                    int AllowNull = Convert.ToInt16(dr["AllowNull"]);
                    string colType = "";
                    switch (Type.ToLower())
                    {
                        case "nvarchar":
                        case "nchar":
                            colType = "[nvarchar](" + NvarcharLength.ToString() + ")";
                            break;
                        case "varchar":
                            colType = "[nvarchar](" + (NvarcharLength * 2).ToString() + ")";
                            break;
                        case "char":
                            colType = "[nvarchar](1)";
                            break;
                        case "numeric":
                            colType = "[numeric](" + NumericPrecision.ToString() + ", " + NumericDigits.ToString() + ")";
                            break;
                        case "decimal":
                            colType = "[decimal](" + NumericPrecision.ToString() + ", " + DecimalDigits.ToString() + ")";
                            break;
                        default:
                            //image
                            //int
                            //datetime
                            //bit
                            //float
                            //bigint
                            //date
                            //money
                            //ntext
                            //smallint
                            //text
                            //uniqueidentifier
                            colType = "[" + Type.ToLower() + "]";
                            break;
                    }
                    sb.AppendLine(string.Format("			[{0}] {1} {2} NULL,", Name, colType, AllowNull == 0 ? "NOT" : ""));
                }
                TableQuery = sb.ToString();
                TableQuery = TableQuery.Substring(0, TableQuery.Length - 1);
                TableQuery += @")')
END";
                res = Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
                res = Result.Failure;
            }
            return res;
        }
        public Result CreateTableAtDestination(string TableName, DataTable dtSchema)
        {
            Result res = Result.UnKnown;
            try
            {
                string TableQuery = "";
                res = GenerateTableScriptFromSchema(dtSchema, TableName, true, ref TableQuery);
                if (res == Result.Success)
                {
                    sqlDestCMD = new SqlCommand(TableQuery, sqlDestConn);
                    sqlDestCMD.ExecuteNonQuery();
                    res = Result.Success;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
                res = Result.Failure;
            }
            return res;
        }
        public Result CopyDataToTempTable(string SelectQuery, string TempTableName, Dictionary<string, string> TableColumns, ref int InsertedRows)
        {
            Result res = Result.UnKnown;
            try
            {
                sqlSrcCMD = new SqlCommand(SelectQuery, sqlSrcConn);
                SqlDataReader reader = sqlSrcCMD.ExecuteReader();
                SqlBulkCopy bulk = new SqlBulkCopy(sqlDestConn);
                bulk.DestinationTableName = TempTableName;
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string SourceColumn = reader.GetName(i);
                    if (TableColumns.ContainsKey(SourceColumn.ToLower()))
                        bulk.ColumnMappings.Add(SourceColumn, TableColumns[SourceColumn.ToLower()]);
                }
                bulk.WriteToServer(reader);
                sqlDestCMD = new SqlCommand("SELECT COUNT(*) FROM [" + TempTableName + "]", sqlDestConn);
                object count = sqlDestCMD.ExecuteScalar();
                InsertedRows = Convert.ToInt32(count);
                res = Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }

        public Result RefillDataAtDestinationTable(string TableName)
        {
            Result res = Result.UnKnown;
            try
            {
                sqlDestCMD = new SqlCommand("TRUNCATE TABLE [" + TableName + "]", sqlDestConn);
                sqlDestCMD.ExecuteNonQuery();
                sqlDestCMD = new SqlCommand(string.Format(@"INSERT INTO [{0}] SELECT * FROM [Temp_{0}]", TableName), sqlDestConn);
                sqlDestCMD.ExecuteNonQuery();

                res = Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public Result UpdateDataAtDestinationTable(string TableName, List<string> TableColumns, List<string> PrimaryKeyColumns, ref int RowsAffected)
        {
            Result res = Result.UnKnown;
            try
            {
                RowsAffected = 0;
                string JoinColumns = "";
                for (int i = 0; i < PrimaryKeyColumns.Count; i++)
                {
                    JoinColumns += string.Format("T.[{0}] = O.[{0}]", PrimaryKeyColumns[i]);
                    if (i != PrimaryKeyColumns.Count - 1)
                    {
                        JoinColumns += " AND ";
                    }
                }

                string updateColumns = "";
                foreach (string col in TableColumns)
                {
                    if (!PrimaryKeyColumns.Contains(col))
                    {
                        updateColumns += string.Format("{0} = T.{0}, ", col);
                    }
                }
                updateColumns = updateColumns.Substring(0, updateColumns.Length - 2);

                string query = string.Format(@"UPDATE O SET {2}
FROM [{0}] O
INNER JOIN [Temp_{0}] T ON {1}", TableName, JoinColumns, updateColumns);
                sqlDestCMD = new SqlCommand(query, sqlDestConn);
                RowsAffected = sqlDestCMD.ExecuteNonQuery();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public Result InaertDataToDestinationTable(string TableName, List<string> PrimaryKeyColumns, ref int RowsAffected)
        {
            Result res = Result.UnKnown;
            try
            {
                RowsAffected = 0;
                string JoinColumns = "";
                string firstColumn = "";
                for (int i = 0; i < PrimaryKeyColumns.Count; i++)
                {
                    if (i == 0)
                        firstColumn = PrimaryKeyColumns[i];
                    JoinColumns += string.Format("O.[{0}] = T.[{0}]", PrimaryKeyColumns[i]);
                    if (i != PrimaryKeyColumns.Count - 1)
                    {
                        JoinColumns += " AND ";
                    }
                }

                string query = string.Format(@"INSERT INTO [{0}]
SELECT T.*
FROM [Temp_{0}] T
LEFT JOIN [{0}] O ON {1}
WHERE O.{2} IS NULL
", TableName, JoinColumns, firstColumn);
                sqlDestCMD = new SqlCommand(query, sqlDestConn);
                RowsAffected = sqlDestCMD.ExecuteNonQuery();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.errorTSFR);
            }
            return res;
        }
        public void Dispose()
        {
            try
            {
                if (incubeQuery != null)
                    incubeQuery.Close();

                if (db != null)
                    db.Dispose();

                CloseConnections();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.errorTSFR);
            }
        }
    }
}