using System;
using System.Data;
using InCubeLibrary;

namespace InCubeIntegration_DAL
{
    public class InCubeRow
    {
        internal DataRow CurrentRow;
        internal InCubeTable CurrentTable;
        private Exception CurrentException;

        public Exception GetCurrentException()
        {
            return CurrentException;
        }
        internal InCubeErrors FormatFieldData(string fieldName, ref string formattedData)
        {
            DateTime d;

            if (!CurrentTable.Opened) return InCubeErrors.DBTableNotOpened;
            formattedData = "";
            if (CurrentRow == null) return InCubeErrors.DBNoCurrentRow;
            try
            {
                if (CurrentRow[fieldName].ToString().Trim() == "") formattedData = "NULL";
                else
                {
                    switch (CurrentTable.GetFieldType(fieldName))
                    {
                        case InCubeFieldTypes.Text:
                        case InCubeFieldTypes.Memo: formattedData = "'" + CurrentRow[fieldName].ToString() + "'"; return InCubeErrors.Success;
                        case InCubeFieldTypes.Integer:
                        case InCubeFieldTypes.Real:
                        case InCubeFieldTypes.Long:
                        case InCubeFieldTypes.Float:
                        case InCubeFieldTypes.Numeric:
                        case InCubeFieldTypes.Currency: formattedData = CurrentRow[fieldName].ToString(); return InCubeErrors.Success;
                        case InCubeFieldTypes.DateTime:

                            d = DateTime.Parse(CurrentRow[fieldName].ToString());
                            formattedData = "'" + d.ToString("MM/dd/yyyy") + "'";
                            return InCubeErrors.Success;

                        case InCubeFieldTypes.Boolean:
                            if (bool.Parse(CurrentRow[fieldName].ToString()))
                                formattedData = "-1";
                            else
                                formattedData = "0";
                            return InCubeErrors.Success;

                        case InCubeFieldTypes.OLEObject: return InCubeErrors.Success;
                        default: return InCubeErrors.DBInvalidFieldType;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                CurrentException = ex;
                return InCubeErrors.DBInvalidFieldName;
            }
            return InCubeErrors.Success;
        }
        internal InCubeErrors FormatFieldData(int fieldIndex, ref string formattedData)
        {
            return FormatFieldData(CurrentRow.Table.Columns[fieldIndex].ColumnName, ref formattedData);
        }
        public InCubeErrors GetField(string fieldName, ref object field)
        {

            try
            {
                if (!CurrentTable.Opened) return InCubeErrors.DBTableNotOpened;
                field = CurrentRow[fieldName];
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                CurrentException = ex;
                return InCubeErrors.DBInvalidFieldName;
            }
            return InCubeErrors.Success;
        }
        public InCubeErrors SetField(int fieldIndex, object field)
        {
            if (!CurrentTable.Opened) return InCubeErrors.DBTableNotOpened;
            if (CurrentRow == null) return InCubeErrors.DBNoCurrentRow;
            if (fieldIndex > CurrentTable.GetFieldCount() - 1) return InCubeErrors.DBIndexOutOfRange;
            try
            {
                switch (CurrentTable.GetFieldType(fieldIndex))
                {
                    case InCubeFieldTypes.Integer:
                    case InCubeFieldTypes.Real:
                    case InCubeFieldTypes.Long:
                    case InCubeFieldTypes.Currency:
                    case InCubeFieldTypes.DateTime:
                    case InCubeFieldTypes.Text:
                    case InCubeFieldTypes.Memo:
                    case InCubeFieldTypes.Float:
                    case InCubeFieldTypes.Numeric:
                    case InCubeFieldTypes.OLEObject:
                        if (field == null)
                            CurrentRow[fieldIndex] = DBNull.Value;
                        else
                            if (field.ToString().Equals(""))
                            CurrentRow[fieldIndex] = DBNull.Value;
                        else
                            CurrentRow[fieldIndex] = field;
                        return InCubeErrors.Success;
                    case InCubeFieldTypes.Boolean:
                        if (field == null)
                            CurrentRow[fieldIndex] = false;
                        else
                            CurrentRow[fieldIndex] = field;
                        return InCubeErrors.Success;
                    default:
                        return InCubeErrors.DBInvalidFieldType;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                CurrentException = ex;
                return InCubeErrors.DBDatabaseError;
            }
        }
        public InCubeErrors SetField(string fieldName, object field)
        {
            try
            {
                InCubeErrors err;
                int fieldIndex = -1;

                if (!CurrentTable.Opened) return InCubeErrors.DBTableNotOpened;
                err = CurrentTable.GetFieldIndex(fieldName, ref fieldIndex);
                if (err != InCubeErrors.Success) { CurrentException = CurrentTable.GetCurrentException(); return err; }
                return SetField(fieldIndex, field);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                this.CurrentException = ex;
                return InCubeErrors.Error;
            }
        }
    }
}
