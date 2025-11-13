using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using InCubeIntegration_DAL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace InCubeIntegration_BL
{
    public class ExcelManager : System.IDisposable
    {
        OleDbConnection excelConn;
        OleDbDataAdapter adp;
        InCubeDatabase db_Sonic;
        InCubeDatabase db_Result;
        InCubeQuery incubeQry;
        InCubeTransaction dbTrans;
        public string ExcelPath;
        public int TriggerID = -1;

        public delegate void ReportProgressDel(int TotalRows, int Inserted, int Updated, int Skipped, string labelText);
        public ReportProgressDel ReportProgressHandler;
        BackgroundWorker bgwCheckProgress;
        bool RunInProgress = false;
        public List<string> ReportingTables = new List<string>();
        Dictionary<int, string> ResultsQueries = new Dictionary<int, string>();
        ExecutionManager execManager;
        IntegrationBase integrationObj;
        public FileExtension FileExt;
        DataTable dtCSVContetns = new DataTable();
        public enum FileExtension
        {
            xls,
            xlsx,
            csv
        }

        public OleDbConnection ExcelConnection
        {
            get { return excelConn; }
        }
        public ExcelManager()
        {
            db_Sonic = new InCubeDatabase();
            db_Sonic.Open("InCube", "ExcelManager");
            db_Result = new InCubeDatabase();
            db_Result.Open("InCube", "ExcelManager");
            ExcelPath = "";
            execManager = new ExecutionManager();
            integrationObj = new IntegrationBase(execManager);
        }
        public Result GetSheets(ref DataTable dtSheets)
        {
            Result res = Result.UnKnown;
            try
            {
                if (excelConn != null && excelConn.State == ConnectionState.Open)
                    res = Result.Success;
                else
                    res = TestConnection();

                if (res != Result.Failure)
                {
                    dtSheets = excelConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                    if (dtSheets == null)
                    {
                        res = Result.Failure;
                    }
                    else if (dtSheets.Rows.Count == 0)
                    {
                        res = Result.NoRowsFound;
                    }
                    else
                    {
                        res = Result.Success;
                    }
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                if (excelConn != null && excelConn.State == ConnectionState.Open)
                {
                    excelConn.Close();
                }
            }
            return res;
        }
        public Result GetResultsTable (int SheetNo, ref DataTable dtResults)
        {
            return GetSonicDataTable(ResultsQueries[SheetNo] + " WHERE TriggerID = " + TriggerID, ref dtResults);
        }
        public Result GetDataTable(string Query, ref DataTable dtData)
        {
            Result res = Result.UnKnown;
            try
            {
                adp = new OleDbDataAdapter(Query, excelConn);
                adp.Fill(dtData);
                if (dtData != null & dtData.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result SaveImportType(string ImportTypeName, string Description, int DropCreate, DataTable dtSheets, List<string> Procs, DataTable dtColumns, SaveMode saveMode, ref string QueryString)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(string.Format(@"DECLARE @ImportTypeID INT = -1;
SELECT @ImportTypeID = ImportTypeID FROM Int_ExcelImportTypes WHERE Name = '{0}';

IF (@ImportTypeID <> -1)
BEGIN
	DELETE FROM Int_ExcelImportTypes WHERE ImportTypeID = @ImportTypeID;
	DELETE FROM Int_ExcelImportSheets WHERE ImportTypeID = @ImportTypeID;
	DELETE FROM Int_ExcelImportColumns WHERE ImportTypeID = @ImportTypeID;
	DELETE FROM Int_ExcelImportProcedures WHERE ImportTypeID = @ImportTypeID;
END
ELSE BEGIN
	SELECT @ImportTypeID = ISNULL(MAX(ImportTypeID),0)+1 FROM Int_ExcelImportTypes;
END

--Int_ExcelImportTypes
INSERT INTO Int_ExcelImportTypes (ImportTypeID,Name,Description,DropCreateStagingTables) VALUES (@ImportTypeID,'{0}','{1}',{2});

--Int_ExcelImportSheets", ImportTypeName.Replace("'", "''"), Description.Replace("'", "''"), DropCreate));
                for (int i = 0; i < dtSheets.Rows.Count; i++)
                {
                    sb.AppendLine(string.Format("INSERT INTO Int_ExcelImportSheets (ImportTypeID,SheetNo,SheetDescription,StagingTable) VALUES (@ImportTypeID,{0},'{1}','{2}');", dtSheets.Rows[i]["SheetNo"], dtSheets.Rows[i]["SheetDescription"], dtSheets.Rows[i]["StagingTable"]));
                }
                sb.AppendLine("\r\n--Int_ExcelImportColumns");
                for (int i = 0; i < dtColumns.Rows.Count; i++)
                {
                    sb.AppendLine(string.Format("INSERT INTO Int_ExcelImportColumns (ImportTypeID,SheetNo,Sequence,FieldName,FieldType) VALUES (@ImportTypeID,{0},{1},'{2}',{3});", dtColumns.Rows[i]["SheetNo"], dtColumns.Rows[i]["Sequence"], dtColumns.Rows[i]["FieldName"], dtColumns.Rows[i]["FieldType"]));
                }
                sb.AppendLine("\r\n--Int_ExcelImportProcedures");
                for (int i = 0; i < Procs.Count; i++)
                {
                    sb.AppendLine(string.Format("INSERT INTO Int_ExcelImportProcedures (ImportTypeID,Sequence,ProcedureName) VALUES (@ImportTypeID,{0},'{1}');", i + 1, Procs[i]));
                }
                sb.AppendLine(@"
--Int_Privileges
IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = @ImportTypeID AND PrivilegeType = 3)
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (@ImportTypeID,3,11);");

                for (int i = 0; i < dtSheets.Rows.Count; i++)
                {
                    string DropCreateStr = "";
                    string SheetNo = dtSheets.Rows[i]["SheetNo"].ToString();
                    string TableName = dtSheets.Rows[i]["StagingTable"].ToString();
                    DataRow[] drColumns = dtColumns.Select("SheetNo = " + SheetNo);
                    CreateTable(TableName, drColumns, DropCreate, false, ref DropCreateStr);
                    sb.AppendLine("\r\n--" + TableName);
                    sb.AppendLine(DropCreateStr);
                }

                QueryString = sb.ToString();
                if (saveMode == SaveMode.ToDatabase)
                {
                    incubeQry = new InCubeQuery(db_Sonic, QueryString);
                    dbTrans = new InCubeTransaction();
                    if (dbTrans.BeginTransaction(db_Sonic) == InCubeErrors.Success)
                    {
                        if (incubeQry.ExecuteNoneQuery(dbTrans) == InCubeErrors.Success)
                        {
                            dbTrans.Commit();
                            return Result.Success;
                        }
                        else
                        {
                            dbTrans.Rollback();
                            return Result.Failure;
                        }
                    }
                    else
                    {
                        return Result.Failure;
                    }
                }
                else
                {
                    return Result.Success;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
        }
        public Result GetDataTypes(bool GetAll, int FilteredTypeID, ref DataTable dtTypes, ref DataTable dtSheets, ref DataTable dtColumns, ref DataTable dtProcedures)
        {
            try
            {
                string qry = "SELECT T.* FROM {0} T";
                if (FilteredTypeID != -1)
                    qry += " WHERE ImportTypeID = " + FilteredTypeID;
                else if (!GetAll)
                    qry += string.Format(" INNER JOIN Int_UserPrivileges UP ON UP.PrivilegeID = T.ImportTypeID AND UP.PrivilegeType = {1} AND UserID = {0} ORDER BY UP.Sequence", CoreGeneral.Common.CurrentSession.UserID, PrivilegeType.ExcelImport.GetHashCode());

                if (GetSonicDataTable(string.Format(qry, "Int_ExcelImportTypes"), ref dtTypes) != Result.Success)
                    return Result.Failure;
                if (GetSonicDataTable(string.Format(qry, "Int_ExcelImportSheets"), ref dtSheets) != Result.Success)
                    return Result.Failure;
                if (GetSonicDataTable(string.Format(qry, "Int_ExcelImportColumns") + (!GetAll && FilteredTypeID == -1 ? ", T.Sequence" : " ORDER BY T.SheetNo, T.Sequence"), ref dtColumns) != Result.Success)
                    return Result.Failure;
                if (GetSonicDataTable(string.Format(qry, "Int_ExcelImportProcedures"), ref dtProcedures) != Result.Success)
                    return Result.Failure;
                return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
        }
        public Result ExecuteQuery(string QueryString)
        {
            try
            {
                incubeQry = new InCubeQuery(db_Sonic, QueryString);
                if (incubeQry.ExecuteNonQuery() == InCubeErrors.Success)
                    return Result.Success;
                else
                    return Result.ErrorExecutingQuery;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
        }
        public Result ExecuteStoredProcedure(string ProcedureName)
        {
            Result res = Result.UnKnown;
            try
            {
                Procedure Proc = new Procedure();
                Proc.ProcedureName = ProcedureName;
                InCubeQuery Qry = new InCubeQuery(db_Sonic, string.Format("SELECT Name,type_name(user_type_id) Type FROM sys.parameters WHERE object_id = object_id('{0}') AND is_output = 0", ProcedureName));
                res = (Qry.Execute() == InCubeErrors.Success ? Result.Success : Result.Failure);
                if (res == Result.Success)
                {
                    DataTable dtParams = new DataTable();
                    dtParams = Qry.GetDataTable();
                    if (dtParams.Rows.Count > 0)
                    {
                        for (int i = 0; i < dtParams.Rows.Count; i++)
                        {
                            string name = dtParams.Rows[i]["Name"].ToString();
                            string type = dtParams.Rows[i]["Type"].ToString();
                            object value = null;
                            ParamType parmType;
                            switch (type)
                            {
                                case "int":
                                    parmType = ParamType.Integer;
                                    value = -1;
                                    break;
                                case "decimal":
                                    parmType = ParamType.Decimal;
                                    value = 0;
                                    break;
                                case "bit":
                                    parmType = ParamType.BIT;
                                    value = 0;
                                    break;
                                default:
                                    parmType = ParamType.Nvarchar;
                                    value = "";
                                    break;
                            }
                            if (name.ToLower() == "@triggerid")
                                value = TriggerID;
                            if (name.ToLower() == "@userid")
                                value = CoreGeneral.Common.CurrentSession.EmployeeID;

                            Proc.AddParameter(name, parmType, value);
                        }
                    }
                    res = integrationObj.ExecuteStoredProcedure(Proc);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                res = Result.Failure;
            }
            return res;
        }
        private Result ExecuteScalar(string QueryString, ref object field)
        {
            field = null;
            try
            {
                incubeQry = new InCubeQuery(db_Sonic, QueryString);
                if (incubeQry.ExecuteScalar(ref field) == InCubeErrors.Success)
                    return Result.Success;
                else
                    return Result.Failure;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
        }
        private Result GetSonicDataTable(string QueryString, ref DataTable dtData)
        {
            dtData = new DataTable();
            try
            {
                incubeQry = new InCubeQuery(db_Sonic, QueryString);
                if (incubeQry.Execute() == InCubeErrors.Success)
                {
                    dtData = incubeQry.GetDataTable();
                    return Result.Success;
                }
                else
                    return Result.Failure;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
        }

        public Result TestConnection()
        {
            try
            {
                ResultsQueries = new Dictionary<int, string>();
                string Ext = Path.GetExtension(ExcelPath);
                string ConnectionString = "";
                switch (Ext)
                {
                    case ".xlsx":
                        FileExt = FileExtension.xlsx;
                        ConnectionString = string.Format(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=Excel 12.0;", ExcelPath);
                        break;
                    case ".xls":
                        FileExt = FileExtension.xls;
                        ConnectionString = string.Format(@"Provider=Microsoft.JET.OLEDB.4.0;Data Source={0};Extended Properties='Excel 8.0; HDR=Yes;IMEX=1;'", ExcelPath);
                        break;
                    case ".csv":
                        FileExt = FileExtension.csv;
                        break;
                    default:
                        return Result.Invalid;
                }
                if (FileExt != FileExtension.csv)
                {
                    excelConn = new OleDbConnection();
                    excelConn.ConnectionString = ConnectionString;
                    excelConn.Open();
                }
                return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
            finally
            {
            }
        }
        private List<string> GetCSVLineValues(string CSVLine, bool LowerCase)
        {
            List<string> values = new List<string>();
            try
            {
                CSVLine = CSVLine.Replace("\"\"", "\"");
                int firstQuoteIndex = -1;
                firstQuoteIndex = CSVLine.IndexOf('"');
                int endofvalueIndex = -1;
                while (firstQuoteIndex > -1)
                {
                    for (int i = firstQuoteIndex + 1; i < CSVLine.Length; i++)
                    {
                        if (CSVLine[i] == '"' && (i == CSVLine.Length - 1 || CSVLine[i + 1] == ','))
                        {
                            endofvalueIndex = i;
                            break;
                        }
                    }
                    string part = CSVLine.Substring(firstQuoteIndex + 1, endofvalueIndex - firstQuoteIndex - 1);
                    part = part.Replace(",", "|");
                    CSVLine = CSVLine.Substring(0, firstQuoteIndex + 1) + part + CSVLine.Substring(endofvalueIndex, CSVLine.Length - endofvalueIndex);
                    firstQuoteIndex = CSVLine.IndexOf('"', endofvalueIndex + 1);
                }
                if (!CSVLine.Contains(","))
                {
                    values.Add(LowerCase ? CSVLine.ToLower() : CSVLine);
                }
                else
                {
                    int indexOfComma = CSVLine.IndexOf(',', 0);
                    string value = "";
                    while (indexOfComma != -1)
                    {
                        if (indexOfComma > 0)
                        {
                            value = CSVLine.Substring(0, indexOfComma);
                            CSVLine = CSVLine.Remove(0, indexOfComma + 1);
                        }
                        else
                        {
                            value = "";
                            CSVLine = CSVLine.Remove(0, 1);
                        }
                        value = value.Replace('|', ',');
                        if (value.StartsWith("\"") && value.EndsWith("\""))
                            value = value.Substring(1, value.Length - 2);
                        values.Add(LowerCase ? value.ToLower() : value);
                        indexOfComma = CSVLine.IndexOf(',', 0);
                    }
                    CSVLine = CSVLine.Replace('|', ',');
                    if (CSVLine.StartsWith("\"") && CSVLine.EndsWith("\""))
                        CSVLine = CSVLine.Substring(1, CSVLine.Length - 2);
                    values.Add(LowerCase ? CSVLine.ToLower() : CSVLine);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return values;
        }
        public Result GetExcelSheetsAndColumns(ref DataTable dtSheets, ref DataTable dtColumns)
        {
            try
            {
                if (excelConn.State == ConnectionState.Closed)
                    excelConn.Open();

                dtSheets = excelConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                dtColumns = excelConn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, null);

                return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
            finally
            {
                if (excelConn.State == ConnectionState.Open)
                    excelConn.Close();
            }
        }
        public Result GetExcelTopNRows(string ExcelSheet, DataRow[] ColumnsList, int NoOfRows, ref DataTable dtTopNRows, ref string Exception)
        {
            int lastI = 0;
            try
            {
                if (FileExt != FileExtension.csv)
                {
                    string QueryString = "";
                    if (FormSelectQueryInExcel(ExcelSheet, ColumnsList, NoOfRows, ref QueryString, ref Exception) == Result.Success)
                    {
                        return GetDataTable(QueryString, ref dtTopNRows);
                    }
                    else
                        return Result.Failure;
                }
                else
                {
                    dtCSVContetns = new DataTable();
                    dtCSVContetns.Columns.Add("TriggerID", typeof(int));
                    dtCSVContetns.Columns.Add("ID", typeof(int));
                    string[] CSV_Contents = File.ReadAllLines(ExcelPath);
                    string Line = CSV_Contents[0];
                    List<string> CSV_Headers = GetCSVLineValues(Line, true);
                    Dictionary<string, int> CSVMatchingColumns = new Dictionary<string, int>();
                    foreach (DataRow dr in ColumnsList)
                    {
                        string ColumnName = dr["FieldName"].ToString();
                        ColumnType FieldType = (ColumnType)(int.Parse(dr["FieldType"].ToString()));
                        if (!CSV_Headers.Contains(ColumnName.ToLower()))
                        {
                            Exception = "Column [" + ColumnName + "] doesn't exist in sheet " + ExcelSheet;
                            return Result.Failure;
                        }
                        else
                        {
                            switch (FieldType)
                            {
                                case ColumnType.Bool:
                                    dtCSVContetns.Columns.Add(ColumnName, typeof(int));
                                    break;
                                case ColumnType.Datetime:
                                    dtCSVContetns.Columns.Add(ColumnName, typeof(DateTime));
                                    break;
                                case ColumnType.Decimal:
                                    dtCSVContetns.Columns.Add(ColumnName, typeof(decimal));
                                    break;
                                case ColumnType.Int:
                                    dtCSVContetns.Columns.Add(ColumnName, typeof(int));
                                    break;
                                default:
                                    dtCSVContetns.Columns.Add(ColumnName, typeof(string));
                                    break;
                            }
                            
                            for (int i = 0; i < CSV_Headers.Count; i++)
                            {
                                if (CSV_Headers[i] == ColumnName.ToLower())
                                {
                                    CSVMatchingColumns.Add(ColumnName, i);
                                    break;
                                }
                            }
                        }
                    }
                    dtTopNRows = new DataTable();
                    for (int i = 1; i < CSV_Contents.Length; i++)
                    {
                        lastI = i;
                        List<string> values = GetCSVLineValues(CSV_Contents[i], false);
                        DataRow dr = dtCSVContetns.NewRow();
                        foreach (KeyValuePair<string, int> pair in CSVMatchingColumns)
                        {
                            dr[pair.Key] = values[pair.Value];
                        }
                        dtCSVContetns.Rows.Add(dr);
                        if (i == NoOfRows)
                            dtTopNRows = dtCSVContetns.Copy();
                    }
                    if (dtTopNRows.Rows.Count == 0)
                        dtTopNRows = dtCSVContetns.Copy();
                    dtTopNRows.Columns.Remove("TriggerID");
                    dtTopNRows.Columns.Remove("ID");
                    return Result.Success;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                if (Exception == string.Empty)
                    Exception = ex.Message;
                return Result.Failure;
            }
        }
        public Result FormSelectQueryInExcel(string ExcelSheet, DataRow[] ColumnsList, int NoOfRows, ref string queryString, ref string Exception)
        {
            try
            {
                if (excelConn.State == ConnectionState.Closed)
                    excelConn.Open();

                DataTable dtColumns = excelConn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, null);

                queryString = "SELECT ";
                if (NoOfRows != -1)
                    queryString += "TOP " + NoOfRows.ToString() + " ";
                foreach (DataRow dr in ColumnsList)
                {
                    string ColumnName = dr["FieldName"].ToString();
                    queryString += "[" + ColumnName + "],";
                    DataRow[] drs = dtColumns.Select("TABLE_NAME = '" + ExcelSheet.Replace("'","''") + "' AND COLUMN_NAME = '" + ColumnName.Replace("'","''") + "'");
                    if (drs.Length == 0)
                    {
                        Exception = "Column [" + ColumnName + "] doesn't exist in sheet " + ExcelSheet;
                        return Result.Failure;
                    }
                }
                queryString = queryString.Substring(0, queryString.Length - 1) + " FROM [" + ExcelSheet + "] ";
                return Result.Success;
            }
            catch (Exception ex)
            {
                Exception = ex.Message;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
            finally
            {
                if (excelConn.State == ConnectionState.Open)
                    excelConn.Close();
            }
        }
        public Result DumpDataToStagingTable(int SheetNo, string ExcelSheet, string StagingTable, int DropCreate, DataRow[] Columns)
        {
            string script = "";
            if (CreateTable(StagingTable, Columns, DropCreate, true, ref script) == Result.Success)
            {
                if (FileExt != FileExtension.csv)
                    return StoreExcelContentsToTable(SheetNo, ExcelSheet, StagingTable, Columns);
                else
                    return StoreCSVContentsToTable(StagingTable);
            }
            else
                return Result.Failure;
        }
        public Result CreateTable(string TableName, DataRow[] Columns, int DropCreate, bool Execute, ref string dataTableCreationQuery)
        {
            try
            {
                string columns = "\r\n        [TriggerID] INT,\r\n        [ID] INT,";
                foreach (DataRow drDetail in Columns)
                {
                    string colName = drDetail["FieldName"].ToString();
                    ColumnType colType = (ColumnType)drDetail["FieldType"];
                    columns += "\r\n        [" + colName + "] ";
                    switch (colType)
                    {
                        case ColumnType.Int:
                            columns += "INT";
                            break;
                        case ColumnType.Datetime:
                            columns += "DATETIME";
                            break;
                        case ColumnType.Decimal:
                            columns += "NUMERIC(19,9)";
                            break;
                        case ColumnType.Bool:
                            columns += "BIT";
                            break;
                        default:
                            columns += "NVARCHAR(200)";
                            break;
                    }
                    columns += " NULL,";
                }
                columns += "\r\n        [ResultID] INT NULL,";
                columns += "\r\n        [Inserted] BIT NULL,";
                columns += "\r\n        [Updated] BIT NULL,";
                columns += "\r\n        [Skipped] BIT NULL,";
                columns += "\r\n        [Message] NVARCHAR(MAX) NULL,";

                if (DropCreate == 1)
                {
                    dataTableCreationQuery = string.Format(@"IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'{0}') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
	EXEC('DROP TABLE {0}');
END

EXEC('
CREATE TABLE {0}({1});')", TableName, columns.Substring(0, columns.Length - 1));
                }
                else
                {
                    dataTableCreationQuery = string.Format(@"IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'{0}') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE {0}({1})
END", TableName, columns.Substring(0, columns.Length - 1));
                }

                if (Execute)
                    return ExecuteQuery(dataTableCreationQuery);
                else
                    return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
        }
        public Result ReadAllExcelRows(string QueryString, ref DataTable dtData)
        {
            try
            {
                if (excelConn.State != ConnectionState.Open)
                    excelConn.Open();

                OleDbCommand cmd = new OleDbCommand(QueryString, excelConn);
                cmd.CommandTimeout = 3600000;
                OleDbDataReader XlsDataReader = cmd.ExecuteReader();
                dtData = new DataTable();
                dtData.Load(XlsDataReader);
                dtData.Columns.Add("TriggerID");
                dtData.Columns.Add("ID");
                for (int i = 0; i < dtData.Rows.Count; i++)
                {
                    dtData.Rows[i]["TriggerID"] = TriggerID;
                    dtData.Rows[i]["ID"] = (i + 1);
                }
                return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
        }
        
        public Result StoreCSVContentsToTable(string TableName)
        {
            string ResultsQry = "SELECT ";
            try
            {
                for (int i = 0; i < dtCSVContetns.Rows.Count; i++)
                {
                    dtCSVContetns.Rows[i]["TriggerID"] = TriggerID;
                    dtCSVContetns.Rows[i]["ID"] = (i + 1);
                }
                SqlBulkCopy bulk = new SqlBulkCopy(db_Sonic.GetConnection());
                foreach (DataColumn Column in dtCSVContetns.Columns)
                {
                    bulk.ColumnMappings.Add(Column.ColumnName, Column.ColumnName);
                    if (Column.ColumnName != "TriggerID" && Column.ColumnName != "ID")
                        ResultsQry += "[" + Column.ColumnName + "],";
                }
                bulk.DestinationTableName = TableName;
                bulk.BulkCopyTimeout = 3600000;
                bulk.WriteToServer(dtCSVContetns);
                ResultsQry += "Message [Result] FROM [" + TableName + "]";
                ResultsQueries.Add(1, ResultsQry);
                return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
            finally
            {
                if (db_Sonic.GetConnection().State == ConnectionState.Open)
                    db_Sonic.GetConnection().Close();
            }
        }
        public Result StoreExcelContentsToTable(int SheetNo, string ExcelSheet, string TableName, DataRow[] ColumnsList)
        {
            try
            {
                string ResultsQry = "SELECT ";
                string QueryString = "";
                string Exception = "";
                if (FormSelectQueryInExcel(ExcelSheet, ColumnsList, -1, ref QueryString, ref Exception) == Result.Success)
                {
                    DataTable dtData = null;
                    if (ReadAllExcelRows(QueryString, ref dtData) == Result.Success)
                    {
                        if (db_Sonic.GetConnection().State == ConnectionState.Closed)
                            db_Sonic.GetConnection().Open();
                        SqlBulkCopy bulk = new SqlBulkCopy(db_Sonic.GetConnection());
                        foreach (DataColumn col in dtData.Columns)
                        {
                            string colName = col.ColumnName;
                            bulk.ColumnMappings.Add(colName, colName);
                            if (colName != "TriggerID" && colName != "ID")
                                ResultsQry += "[" + colName + "],";
                        }
                        bulk.DestinationTableName = TableName;
                        bulk.BulkCopyTimeout = 3600000;
                        bulk.WriteToServer(dtData);
                        ResultsQry += "Message [Result] FROM [" + TableName + "]";
                        ResultsQueries.Add(SheetNo, ResultsQry);
                        return Result.Success;
                    }
                    else
                        return Result.Failure;
                }
                else
                    return Result.Failure;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
            finally
            {
                if (db_Sonic.GetConnection().State == ConnectionState.Open)
                    db_Sonic.GetConnection().Close();
                if (excelConn.State == ConnectionState.Open)
                    excelConn.Close();
            }
        }
        private void bgw_CheckProgress(object sender, DoWorkEventArgs e)
        {
            try
            {
                while (RunInProgress)
                {
                    UpdateProgress();
                    System.Threading.Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void UpdateProgress()
        {
            try
            {
                int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
                GetExecutionResults(ReportingTables, ref TotalRows, ref Inserted, ref Updated, ref Skipped);
                ReportProgressHandler(TotalRows, Inserted, Updated, Skipped, "");
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public Result ExecuteProcedures(DataRow[] Procedures)
        {
            try
            {
                bgwCheckProgress = new BackgroundWorker();
                bgwCheckProgress.DoWork += new DoWorkEventHandler(bgw_CheckProgress);
                bgwCheckProgress.WorkerSupportsCancellation = true;
                bgwCheckProgress.RunWorkerAsync();

                RunInProgress = true;
                foreach (DataRow dr in Procedures)
                {
                    if (ExecuteStoredProcedure(dr["ProcedureName"].ToString()) != Result.Success)
                    {
                        return Result.Failure;
                    }
                }
                return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
            finally
            {
                RunInProgress = false;
                UpdateProgress();
            }
        }
        public void GetExecutionResults(List<string> StagingTables, ref int TotalRows, ref int Inserted, ref int Updated, ref int Skipped)
        {
            try
            {
                if (CoreGeneral.Common.CurrentSession.loginType == LoginType.WindowsService)
                    return;

                foreach (string StagingTable in StagingTables)
                {
                    string CheckQry = string.Format(@"SELECT COUNT(*) TotalRows, ISNULL(SUM(CAST(ISNULL(Inserted,0) AS INT)),0) Inserted, ISNULL(SUM(CAST(ISNULL(Updated,0) AS INT)),0) Updated, ISNULL(SUM(CAST(ISNULL(Skipped,0) AS INT)),0) Skipped 
FROM 
{0}
WHERE TriggerID = {1}", StagingTable, TriggerID);
                    incubeQry = new InCubeQuery(db_Result, CheckQry);
                    DataTable dtResults = new DataTable();
                    if (incubeQry.Execute() == InCubeErrors.Success)
                    {
                        dtResults = incubeQry.GetDataTable();
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
        public static bool CreateExcelDocument(DataSet ds, string excelFilename)
        {
            try
            {
                using (SpreadsheetDocument spreadsheet =  SpreadsheetDocument.Create(excelFilename, SpreadsheetDocumentType.Workbook))
                {
                    WriteExcelFile(ds, spreadsheet);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public static void WriteExcelFile(DataSet ds, SpreadsheetDocument spreadsheet)
        {
            //  Create the Excel file contents.  This function is used when creating an Excel file either writing 
            //  to a file, or writing to a MemoryStream.
            spreadsheet.AddWorkbookPart();
            spreadsheet.WorkbookPart.Workbook = new DocumentFormat.OpenXml.Spreadsheet.Workbook();

            //  My thanks to James Miera for the following line of code (which prevents crashes in Excel 2010)
            spreadsheet.WorkbookPart.Workbook.Append(new BookViews(new WorkbookView()));

            //  If we don't add a "WorkbookStylesPart", OLEDB will refuse to connect to this .xlsx file !
            WorkbookStylesPart workbookStylesPart = spreadsheet.WorkbookPart.AddNewPart<WorkbookStylesPart>("rIdStyles");
            Stylesheet stylesheet = new Stylesheet();
            workbookStylesPart.Stylesheet = stylesheet;


            //  Loop through each of the DataTables in our DataSet, and create a new Excel Worksheet for each.
            uint worksheetNumber = 1;
            Sheets sheets = spreadsheet.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());
            foreach (DataTable dt in ds.Tables)
            {
                //  For each worksheet you want to create
                string worksheetName = dt.TableName;

                //  Create worksheet part, and add it to the sheets collection in workbook
                WorksheetPart newWorksheetPart = spreadsheet.WorkbookPart.AddNewPart<WorksheetPart>();
                Sheet sheet = new Sheet() { Id = spreadsheet.WorkbookPart.GetIdOfPart(newWorksheetPart), SheetId = worksheetNumber, Name = worksheetName };

                // If you want to define the Column Widths for a Worksheet, you need to do this *before* appending the SheetData
                // http://social.msdn.microsoft.com/Forums/en-US/oxmlsdk/thread/1d93eca8-2949-4d12-8dd9-15cc24128b10/

                sheets.Append(sheet);

                //  Append this worksheet's data to our Workbook, using OpenXmlWriter, to prevent memory problems
                WriteDataTableToExcelWorksheet(dt, newWorksheetPart);

                worksheetNumber++;
            }

            spreadsheet.WorkbookPart.Workbook.Save();
        }
        private static void WriteDataTableToExcelWorksheet(DataTable dt, WorksheetPart worksheetPart)
        {
            OpenXmlWriter writer = OpenXmlWriter.Create(worksheetPart, Encoding.ASCII);
            writer.WriteStartElement(new Worksheet());
            writer.WriteStartElement(new SheetData());

            string cellValue = "";
            string cellReference = "";

            //  Create a Header Row in our Excel file, containing one header for each Column of data in our DataTable.
            //
            //  We'll also create an array, showing which type each column of data is (Text or Numeric), so when we come to write the actual
            //  cells of data, we'll know if to write Text values or Numeric cell values.
            int numberOfColumns = dt.Columns.Count;
            bool[] IsIntegerColumn = new bool[numberOfColumns];
            bool[] IsFloatColumn = new bool[numberOfColumns];
            bool[] IsDateColumn = new bool[numberOfColumns];

            string[] excelColumnNames = new string[numberOfColumns];
            for (int n = 0; n < numberOfColumns; n++)
                excelColumnNames[n] = GetExcelColumnName(n);

            //
            //  Create the Header row in our Excel Worksheet
            //
            uint rowIndex = 1;

            writer.WriteStartElement(new Row { RowIndex = rowIndex });
            for (int colInx = 0; colInx < numberOfColumns; colInx++)
            {
                DataColumn col = dt.Columns[colInx];
                AppendHeaderTextCell(excelColumnNames[colInx] + "1", col.ColumnName, writer);
                IsIntegerColumn[colInx] = (col.DataType.FullName.StartsWith("System.Int"));
                IsFloatColumn[colInx] = (col.DataType.FullName == "System.Decimal") || (col.DataType.FullName == "System.Double") || (col.DataType.FullName == "System.Single");
                IsDateColumn[colInx] = (col.DataType.FullName == "System.DateTime");

            }
            writer.WriteEndElement();   //  End of header "Row"

            //
            //  Now, step through each row of data in our DataTable...
            //
            double cellFloatValue = 0;
            CultureInfo ci = new CultureInfo("en-US");
            foreach (DataRow dr in dt.Rows)
            {
                // ...create a new row, and append a set of this row's data to it.
                ++rowIndex;

                writer.WriteStartElement(new Row { RowIndex = rowIndex });

                for (int colInx = 0; colInx < numberOfColumns; colInx++)
                {
                    cellValue = dr.ItemArray[colInx].ToString();
                    cellValue = ReplaceHexadecimalSymbols(cellValue);
                    cellReference = excelColumnNames[colInx] + rowIndex.ToString();

                    // Create cell with data
                    if (IsIntegerColumn[colInx] || IsFloatColumn[colInx])
                    {
                        //  For numeric cells without any decimal places.
                        //  If this numeric value is NULL, then don't write anything to the Excel file.
                        cellFloatValue = 0;
                        if (double.TryParse(cellValue, out cellFloatValue))
                        {
                            cellValue = cellFloatValue.ToString(ci);
                            AppendNumericCell(cellReference, cellValue, writer);
                        }
                    }
                    else if (IsDateColumn[colInx])
                    {
                        //  This is a date value.
                        DateTime dateValue;
                        if (DateTime.TryParse(cellValue, out dateValue))
                        {
                            AppendDateCell(cellReference, dateValue, writer);
                        }
                        else
                        {
                            //  This should only happen if we have a DataColumn of type "DateTime", but this particular value is null/blank.
                            AppendTextCell(cellReference, cellValue, writer);
                        }
                    }
                    else
                    {
                        //  For text cells, just write the input data straight out to the Excel file.
                        AppendTextCell(cellReference, cellValue, writer);
                    }
                }
                writer.WriteEndElement(); //  End of Row
            }
            writer.WriteEndElement(); //  End of SheetData
            writer.WriteEndElement(); //  End of worksheet

            writer.Close();
        }
        private static string ReplaceHexadecimalSymbols(string txt)
        {
            string r = "[\x00-\x08\x0B\x0C\x0E-\x1F\x26]";
            return Regex.Replace(txt, r, "", RegexOptions.Compiled);
        }
        public static string GetExcelColumnName(int columnIndex)
        {
            //  eg  (0) should return "A"
            //      (1) should return "B"
            //      (25) should return "Z"
            //      (26) should return "AA"
            //      (27) should return "AB"
            //      ..etc..
            char firstChar;
            char secondChar;
            char thirdChar;

            if (columnIndex < 26)
            {
                return ((char)('A' + columnIndex)).ToString();
            }

            if (columnIndex < 702)
            {
                firstChar = (char)('A' + (columnIndex / 26) - 1);
                secondChar = (char)('A' + (columnIndex % 26));

                return string.Format("{0}{1}", firstChar, secondChar);
            }

            int firstInt = columnIndex / 676;
            int secondInt = (columnIndex % 676) / 26;
            if (secondInt == 0)
            {
                secondInt = 26;
                firstInt = firstInt - 1;
            }
            int thirdInt = (columnIndex % 26);

            firstChar = (char)('A' + firstInt - 1);
            secondChar = (char)('A' + secondInt - 1);
            thirdChar = (char)('A' + thirdInt);

            return string.Format("{0}{1}{2}", firstChar, secondChar, thirdChar);
        }
        private static void AppendDateCell(string cellReference, DateTime dateTimeValue, OpenXmlWriter writer)
        {
            //  Add a new "datetime" Excel Cell to our Row.
            //
            string cellStringValue = dateTimeValue.ToShortDateString();

            writer.WriteElement(new Cell
            {
                CellValue = new CellValue(cellStringValue),
                CellReference = cellReference,
                DataType = CellValues.String
            });
        }

        private static void AppendFormulaCell(string cellReference, string cellStringValue, OpenXmlWriter writer)
        {
            //  Add a new "formula" Excel Cell to our Row 
            writer.WriteElement(new Cell
            {
                CellFormula = new CellFormula(cellStringValue),
                CellReference = cellReference,
                DataType = CellValues.Number
            });
        }

        private static void AppendNumericCell(string cellReference, string cellStringValue, OpenXmlWriter writer)
        {
            //  Add a new numeric Excel Cell to our Row.
            writer.WriteElement(new Cell
            {
                CellValue = new CellValue(cellStringValue),
                CellReference = cellReference,
                DataType = CellValues.Number
            });
        }
        private static void AppendTextCell(string cellReference, string cellStringValue, OpenXmlWriter writer)
        {
            //  Add a new "text" Cell to our Row 

#if DATA_CONTAINS_FORMULAE
            //  If this item of data looks like a formula, let's store it in the Excel file as a formula rather than a string.
            if (cellStringValue.StartsWith("="))
            {
                AppendFormulaCell(cellReference, cellStringValue, writer);
                return;
            }
#endif

            //  Add a new Excel Cell to our Row 
            writer.WriteElement(new Cell
            {
                CellValue = new CellValue(cellStringValue),
                CellReference = cellReference,
                DataType = CellValues.String
            });
        }
        private static void AppendHeaderTextCell(string cellReference, string cellStringValue, OpenXmlWriter writer)
        {
            //  Add a new "text" Cell to the first row in our Excel worksheet
            //  We set these cells to use "Style # 3", so they have a gray background color & white text.
            writer.WriteElement(new Cell
            {
                CellValue = new CellValue(cellStringValue),
                CellReference = cellReference,
                DataType = CellValues.String
            });
        }
        public void LogActionTrigger(int ImportTypeID)
        {
            TriggerID = execManager.LogActionTriggerBegining(-1, -1, -1, ImportTypeID);
        }

        public void Dispose()
        {
            if (adp != null)
                adp.Dispose();

            if (excelConn != null)
            {
                if (excelConn.State == ConnectionState.Open)
                    excelConn.Close();

                excelConn.Dispose();
            }

            if (incubeQry != null)
                incubeQry.Close();

            if (db_Sonic != null && db_Sonic.GetConnection() != null)
            {
                if (db_Sonic.GetConnection().State == ConnectionState.Open)
                    db_Sonic.GetConnection().Close();

                db_Sonic.GetConnection().Dispose();
            }

            if (db_Result != null && db_Result.GetConnection() != null)
            {
                if (db_Result.GetConnection().State == ConnectionState.Open)
                    db_Result.GetConnection().Close();

                db_Result.GetConnection().Dispose();
            }
        }
    }
}
