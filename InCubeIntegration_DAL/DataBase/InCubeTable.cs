using InCubeLibrary;
using System;
using System.Data;
using System.Data.SqlClient;

namespace InCubeIntegration_DAL
{
    public class InCubeTable
    {
        internal bool Opened = false;
        private DataTable Data;
        private InCubeDatabase Database;
        private string Name = "";
        private int TotalRows = 0;
        private int PrimaryKeyColumnsCount = 0;
        private DataRow[] SchemaTableFields;
        private DataRow[] SchemaIndexFields;
        private Exception CurrentException;
        public SqlDataAdapter DBAdapter;

        public int GetFieldCount()
        {
            if (!Opened) return -1;
            return SchemaTableFields.Length;
        }
        public DataTable GetDataTable()
        {
            return Data;
        }
        public Exception GetCurrentException()
        {
            return CurrentException;
        }
        public InCubeErrors GetFieldIndex(string fieldName, ref int fieldIndex)
        {
            try
            {
                if (!Opened) return InCubeErrors.DBTableNotOpened;
                try { fieldIndex = Data.Columns[fieldName].Ordinal; }
                catch (Exception e)
                {
                    CurrentException = e;
                    return InCubeErrors.DBInvalidFieldName;
                }
                return InCubeErrors.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                CurrentException = ex;
                return InCubeErrors.Error;
            }

        }
        public InCubeFieldTypes GetFieldType(int fieldIndex)
        {
            if (!Opened) return InCubeFieldTypes.Unknown;
            switch (SchemaTableFields[fieldIndex]["data_type"].ToString())
            {
                //#if SQL_SERVER
                case "int": return InCubeFieldTypes.Long;
                case "smallint": return InCubeFieldTypes.Integer;
                case "char":
                case "nchar":
                case "nvarchar": return InCubeFieldTypes.Text;
                case "ntext": return InCubeFieldTypes.Memo;
                case "decimal":
                case "numeric": return InCubeFieldTypes.Numeric;
                case "money": return InCubeFieldTypes.Currency;
                case "datetime": return InCubeFieldTypes.DateTime;
                case "bit": return InCubeFieldTypes.Boolean;
                case "image": return InCubeFieldTypes.OLEObject;
                case "float": return InCubeFieldTypes.Float;
                case "real": return InCubeFieldTypes.Real;
                default: return InCubeFieldTypes.Unknown;
                    //#elif MSACCESS
                    //                case "3": return InCubeFieldTypes.Long;
                    //                case "2":
                    //                case "17": return InCubeFieldTypes.Integer;
                    //                case "130": return InCubeFieldTypes.Text;
                    //                case "6": return InCubeFieldTypes.Currency;
                    //                case "7": return InCubeFieldTypes.DateTime;
                    //                case "11": return InCubeFieldTypes.Boolean;
                    //                case "128": return InCubeFieldTypes.OLEObject;
                    //                default: return InCubeFieldTypes.Unknown;
                    //#elif ORACLE
                    //                case "NUMBER":
                    //                    switch (SchemaTableFields[fieldIndex]["data_precision"].ToString() + SchemaTableFields[fieldIndex]["data_scale"].ToString())
                    //                    {
                    //                        case "10": return InCubeFieldTypes.Boolean;
                    //                        case "50": return InCubeFieldTypes.Integer;
                    //                        case "150": return InCubeFieldTypes.Long;
                    //                        case "100": return InCubeFieldTypes.Integer;
                    //                        case "105": return InCubeFieldTypes.Currency;

                    //                        default: return InCubeFieldTypes.Float; //return InCubeFieldTypes.Unknown;
                    //                    }
                    //                case "NVARCHAR2":
                    //                case "VARCHAR2":
                    //                    return InCubeFieldTypes.Text;
                    //                case "FLOAT": return InCubeFieldTypes.Float;
                    //                case "BLOB": return InCubeFieldTypes.OLEObject;
                    //                case "DATE": 
                    //                case "TIMESTAMP(0)":
                    //                case "TIMESTAMP(1)":
                    //                case "TIMESTAMP(2)":
                    //                case "TIMESTAMP(3)":
                    //                case "TIMESTAMP(4)":
                    //                case "TIMESTAMP(5)":
                    //                case "TIMESTAMP(6)":
                    //                case "TIMESTAMP(7)":
                    //                case "TIMESTAMP(8)":
                    //                case "TIMESTAMP(9)": 
                    //                    return InCubeFieldTypes.DateTime;
                    //                case "CHAR": return InCubeFieldTypes.Text;
                    //                case "NCLOB": return InCubeFieldTypes.Memo;                
                    //                default: return InCubeFieldTypes.Unknown;
                    //#endif
            }
        }
        public InCubeFieldTypes GetFieldType(string fieldName)
        {
            return GetFieldType(Data.Columns[fieldName].Ordinal);
        }
        private SqlDbType GetDataType(string dataType)
        {
            switch (dataType)
            {
                case "int": return SqlDbType.Int;
                case "bit": return SqlDbType.Bit;
                case "datetime": return SqlDbType.DateTime;
                case "image": return SqlDbType.Image;
                case "decimal":
                case "numeric": return SqlDbType.Decimal;
                case "money": return SqlDbType.Money;
                case "ntext": return SqlDbType.NText;
                case "char":
                case "nchar":
                case "nvarchar": return SqlDbType.NVarChar;
                case "smallint": return SqlDbType.SmallInt;
                case "float": return SqlDbType.Float;
                case "real": return SqlDbType.Real;
                default: return SqlDbType.Variant;
            }
        }
        public InCubeErrors Open(InCubeDatabase db, string tableName)
        {
            int i;
            if (Opened) return InCubeErrors.DBTableAlreadyOpened;
            Database = db;
            Data = new DataTable();
            string valuesString;
            string commandString;
            //#if SQL_SERVER
            SqlCommand insertCommand = new SqlCommand();
            SqlCommand updateCommand = new SqlCommand();
            SqlCommand deleteCommand = new SqlCommand();
            SqlParameter parameter;
            SqlDbType type;

            SchemaTableFields = Database.ConfiguredConnection.GetSchema("Columns").Select("table_name='" + tableName + "'", "ordinal_position");
            SchemaIndexFields = Database.ConfiguredConnection.GetSchema("IndexColumns").Select("table_name='" + tableName + "' And index_name='aaaaa" + tableName + "_PK'", "ordinal_position");
            SqlCommand tableCommand = new SqlCommand("Select * From " + tableName, Database.ConfiguredConnection);
            DBAdapter = new SqlDataAdapter(tableCommand);
            //#elif MSACCESS
            //            OleDbCommand insertCommand = new OleDbCommand();
            //            OleDbCommand updateCommand = new OleDbCommand();
            //            OleDbCommand deleteCommand = new OleDbCommand();
            //            OleDbParameter parameter;

            //            SchemaTableFields = Database.ConfiguredConnection.GetSchema("Columns").Select("table_name='" + tableName + "'", "ordinal_position");
            //            SchemaIndexFields = Database.ConfiguredConnection.GetSchema("Indexes").Select("table_name='" + tableName + "' And index_name='PrimaryKey'", "ordinal_position");
            //            OleDbCommand tableCommand = new OleDbCommand("Select * From " + tableName, Database.ConfiguredConnection);
            //            DBAdapter = new OleDbDataAdapter(tableCommand);
            //#elif ORACLE
            //            OracleCommand insertCommand = new OracleCommand();
            //            OracleCommand updateCommand = new OracleCommand();
            //            OracleCommand deleteCommand = new OracleCommand();
            //            OracleParameter parameter;
            //            //<Omar>
            //            OracleType type;
            //            //<End>
            //            DBAdapter = new OracleDataAdapter("Select * From user_tab_columns Where upper(table_name)='" + tableName.ToUpper() + "' Order By column_id", Database.ConfiguredConnection);
            //            DataTable schemaTable = new DataTable();
            //            DBAdapter.Fill(schemaTable);
            //            SchemaTableFields = schemaTable.Select();
            //            DBAdapter = new OracleDataAdapter("Select column_name From user_ind_columns Where upper(table_name)='" + tableName.ToUpper() + "' And upper(index_name)='PK_" + tableName.ToUpper() + "' Order By column_position", Database.ConfiguredConnection);
            //            DataTable schemaIndex = new DataTable();
            //            DBAdapter.Fill(schemaIndex);
            //            SchemaIndexFields = schemaIndex.Select();
            //            OracleCommand tableCommand = new OracleCommand("Select * From " + tableName.ToUpper(), Database.ConfiguredConnection);
            //            DBAdapter = new OracleDataAdapter(tableCommand);
            //#endif
            #region Insert Command
            valuesString = "";
            //#if SQL_SERVER
            commandString = "Insert Into [" + tableName + "] (";
            //#elif ORACLE
            //            commandString = "Insert Into " + tableName + " (";
            //#endif
            insertCommand.Connection = Database.ConfiguredConnection;
            for (i = 0; i < SchemaTableFields.Length; i++)
            {
                //#if SQL_SERVER
                //if (!SchemaTableFields[i]["data_type"].ToString().Equals("image"))
                if (!SchemaTableFields[i]["data_type"].ToString().Equals("uniqueidentifier"))
                //#elif MSACCESS
                //                if (!SchemaTableFields[i]["data_type"].ToString().Equals("128"))
                //#elif ORACLE
                //                if (!SchemaTableFields[i]["data_type"].ToString().Equals("image"))
                //#endif
                {
                    if (i > 0)
                    {
                        valuesString += ",";
                        commandString += ",";
                    }
                    commandString += SchemaTableFields[i]["column_name"].ToString();
                    //#if SQL_SERVER
                    valuesString += "@" + SchemaTableFields[i]["column_name"].ToString();
                    type = GetDataType(SchemaTableFields[i]["data_type"].ToString());
                    parameter = new SqlParameter("@" + SchemaTableFields[i]["column_name"].ToString(), type, 0, SchemaTableFields[i]["column_name"].ToString());
                    //#elif MSACCESS
                    //                    valuesString += "?";
                    //                    parameter = new OleDbParameter(SchemaTableFields[i]["column_name"].ToString(), (OleDbType)SchemaTableFields[i]["data_type"], 0, SchemaTableFields[i]["column_name"].ToString());
                    //#elif ORACLE
                    //                    //<Omar>
                    //                    //valuesString += ":" + SchemaTableFields[i]["column_name"].ToString();
                    //                    type = GetOracleDataType(SchemaTableFields[i]["data_type"].ToString());
                    //                    if (type == OracleType.Timestamp || type == OracleType.DateTime)
                    //                    {

                    //                        valuesString += ":" + SchemaTableFields[i]["column_name"].ToString() + "";
                    //                    }// "to_date(:" + SchemaTableFields[i]["column_name"].ToString(),'DD/MM/YYYY HH24:MI:SS' + ")";
                    //                    else
                    //                        valuesString += ":" + SchemaTableFields[i]["column_name"].ToString();

                    //                    //<End>
                    //                    parameter = new OracleParameter(SchemaTableFields[i]["column_name"].ToString(), GetOracleDataType(SchemaTableFields[i]["data_type"].ToString()), 0, SchemaTableFields[i]["column_name"].ToString());
                    //#endif
                    parameter.SourceVersion = DataRowVersion.Current;
                    insertCommand.Parameters.Add(parameter);
                }
            }
            insertCommand.CommandText = commandString + ") Values (" + valuesString + ")";
            DBAdapter.InsertCommand = insertCommand;
            #endregion
            #region Update Command
            valuesString = "";
            //#if SQL_SERVER
            commandString = "update [" + tableName + "] set ";
            //#elif ORACLE
            //            commandString = "update " + tableName + " set ";
            //#endif
            updateCommand.Connection = Database.ConfiguredConnection;
            for (i = 0; i < SchemaTableFields.Length; i++)
            {
                //#if SQL_SERVER
                //if (!SchemaTableFields[i]["data_type"].ToString().Equals("image"))
                if (!SchemaTableFields[i]["data_type"].ToString().Equals("uniqueidentifier"))
                //#elif MSACCESS
                //                if (!SchemaTableFields[i]["data_type"].ToString().Equals("128"))
                //#elif ORACLE
                //                if (!SchemaTableFields[i]["data_type"].ToString().Equals("image"))
                //#endif
                {
                    if (i > 0) commandString += ",";
                    //#if SQL_SERVER
                    commandString += SchemaTableFields[i]["column_name"].ToString() + "=@" + SchemaTableFields[i]["column_name"].ToString();
                    type = GetDataType(SchemaTableFields[i]["data_type"].ToString());
                    parameter = new SqlParameter("@" + SchemaTableFields[i]["column_name"].ToString(), type, 0, SchemaTableFields[i]["column_name"].ToString());
                    //#elif MSACCESS
                    //                    commandString += SchemaTableFields[i]["column_name"].ToString() + "=?";
                    //                    parameter = new OleDbParameter(SchemaTableFields[i]["column_name"].ToString(), (OleDbType)SchemaTableFields[i]["data_type"], 0, SchemaTableFields[i]["column_name"].ToString());
                    //#elif ORACLE
                    //                    //<Omar>                    
                    //                    type = GetOracleDataType(SchemaTableFields[i]["data_type"].ToString());
                    //                    if (type == OracleType.Timestamp || type == OracleType.DateTime)
                    //                        commandString += SchemaTableFields[i]["column_name"].ToString() + "=:" + SchemaTableFields[i]["column_name"].ToString() + "";
                    //                    else
                    //                        commandString += SchemaTableFields[i]["column_name"].ToString() + "=:" + SchemaTableFields[i]["column_name"].ToString();
                    //                    //<End>
                    //                    parameter = new OracleParameter(SchemaTableFields[i]["column_name"].ToString(), GetOracleDataType(SchemaTableFields[i]["data_type"].ToString()), 0, SchemaTableFields[i]["column_name"].ToString());
                    //#endif
                    parameter.SourceVersion = DataRowVersion.Current;
                    updateCommand.Parameters.Add(parameter);
                }
            }
            for (i = 0; i < SchemaIndexFields.Length; i++)
            {
                if (i > 0) valuesString += " and ";
                //#if SQL_SERVER
                valuesString += SchemaIndexFields[i]["column_name"].ToString() + "=@Original_" + SchemaIndexFields[i]["column_name"].ToString();
                type = GetDataType(SchemaTableFields[i]["data_type"].ToString());
                parameter = new SqlParameter("@Original_" + SchemaIndexFields[i]["column_name"].ToString(), type, 0, SchemaIndexFields[i]["column_name"].ToString());
                //#elif MSACCESS
                //                valuesString += SchemaIndexFields[i]["column_name"].ToString() + "=?";
                //                parameter = new OleDbParameter("Original_" + SchemaIndexFields[i]["column_name"].ToString(), (OleDbType)SchemaTableFields[i]["data_type"], 0, SchemaIndexFields[i]["column_name"].ToString());
                //#elif ORACLE
                //                //<Omar>

                //                type = GetOracleDataType(SchemaTableFields[i]["data_type"].ToString());
                //                if (type == OracleType.Timestamp || type == OracleType.DateTime)
                //                    valuesString += SchemaIndexFields[i]["column_name"].ToString() + "=to_date(:Original_" + SchemaIndexFields[i]["column_name"].ToString() + ", 'DD/MM/YYYY HH:MI:SS')";                    
                //                else
                //                    valuesString += SchemaIndexFields[i]["column_name"].ToString() + "=:Original_" + SchemaIndexFields[i]["column_name"].ToString();


                //                //<End>
                //                parameter = new OracleParameter("Original_" + SchemaIndexFields[i]["column_name"].ToString(), GetOracleDataType(SchemaTableFields[i]["data_type"].ToString()), 0, SchemaIndexFields[i]["column_name"].ToString());
                //#endif
                parameter.SourceVersion = DataRowVersion.Original;
                updateCommand.Parameters.Add(parameter);
            }
            updateCommand.CommandText = commandString + " Where " + valuesString;
            DBAdapter.UpdateCommand = updateCommand;
            #endregion
            #region DeleteCommand
            //#if SQL_SERVER
            commandString = "Delete From [" + tableName + "] Where ";
            //#elif ORACLE
            //            commandString = "Delete From " + tableName + " Where ";
            //#endif
            deleteCommand.Connection = Database.ConfiguredConnection;
            for (i = 0; i < SchemaIndexFields.Length; i++)
            {
                if (i > 0) commandString += " and ";
                //#if SQL_SERVER
                commandString += SchemaIndexFields[i]["column_name"].ToString() + "=@Original_" + SchemaIndexFields[i]["column_name"].ToString();
                type = GetDataType(SchemaTableFields[i]["data_type"].ToString());
                parameter = new SqlParameter("@Original_" + SchemaIndexFields[i]["column_name"].ToString(), type, 0, SchemaIndexFields[i]["column_name"].ToString());
                //#elif MSACCESS
                //                commandString += SchemaIndexFields[i]["column_name"].ToString() + "=?";
                //                parameter = new OleDbParameter("Original_" + SchemaIndexFields[i]["column_name"].ToString(), (OleDbType)SchemaTableFields[i]["data_type"], 0, SchemaIndexFields[i]["column_name"].ToString());
                //#elif ORACLE
                //                type = GetOracleDataType(SchemaTableFields[i]["data_type"].ToString());                
                //                //<End>

                //                if (type == OracleType.Timestamp || type == OracleType.DateTime)
                //                    commandString += string.Format("{0}=to_date(:Original_{1},'{2}')", SchemaIndexFields[i]["column_name"].ToString(), SchemaIndexFields[i]["column_name"].ToString(),"DD/MM/YYYY HH:MI:SS");
                //                else
                //                    commandString += SchemaIndexFields[i]["column_name"].ToString() + "=:Original_" + SchemaIndexFields[i]["column_name"].ToString();


                //                parameter = new OracleParameter("Original_" + SchemaIndexFields[i]["column_name"].ToString(), GetOracleDataType(SchemaTableFields[i]["data_type"].ToString()), 0, SchemaIndexFields[i]["column_name"].ToString());
                //#endif
                parameter.SourceVersion = DataRowVersion.Original;
                deleteCommand.Parameters.Add(parameter);
            }
            deleteCommand.CommandText = commandString;
            DBAdapter.DeleteCommand = deleteCommand;
            #endregion
            try
            {
                DBAdapter.Fill(Data);
                TotalRows = Data.Rows.Count;
                Name = tableName;
                Opened = true;
                PrimaryKeyColumnsCount = SchemaIndexFields.Length;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                CurrentException = ex;
                return InCubeErrors.DBCannotOpenTable;
            }
            return InCubeErrors.Success;
        }
        public InCubeErrors Close()
        {
            if (!Opened) return InCubeErrors.DBTableNotOpened;
            Name = "";
            TotalRows = 0;
            Opened = false;
            PrimaryKeyColumnsCount = 0;
            Database = null;
            Data = null;
            return InCubeErrors.Success;
        }
    }
}