using System;
using System.Collections.Generic;
using System.Text;
using InCubeLibrary;

namespace InCubeIntegration_DAL
{
    public class QueryBuilder
    {
        public string LastExecutedStatement;

        private Exception _currentException;

        public Exception CurrentException
        {
            get { return _currentException; }
        }

        List<string> _fieldNames = new List<string>();
        List<string> _fieldValues = new List<string>();

        public InCubeErrors SetField(string fieldName, string fieldValue)
        {
            try
            {
                _fieldNames.Add(fieldName);

                fieldValue = fieldValue.Replace("'", "''");
                if (fieldValue.Length > 2 && fieldValue.StartsWith("''"))
                {
                    fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);
                }
                else if (fieldValue.Length > 2 && fieldValue.StartsWith("N''") && fieldValue.EndsWith("''"))
                {
                    fieldValue = fieldValue.Remove(1, 1);
                    fieldValue = fieldValue.Remove(fieldValue.Length - 2, 1);
                }
                
                _fieldValues.Add(fieldValue);
                return InCubeErrors.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                _currentException = ex;
                return InCubeErrors.Error;
            }
        }

        public InCubeErrors SetStringField(string fieldName, string fieldValue)
        {
            try
            {
                _fieldNames.Add(fieldName);

                fieldValue = fieldValue.Replace("'", "''");
                if (fieldValue.Length > 2 && fieldValue.StartsWith("''"))
                {
                    fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);
                }
                else if (fieldValue.Length > 2 && fieldValue.StartsWith("N''") && fieldValue.EndsWith("''"))
                {
                    fieldValue = fieldValue.Remove(1, 1);
                    fieldValue = fieldValue.Remove(fieldValue.Length - 2, 1);
                }

                _fieldValues.Add("'" + fieldValue + "'");
                return InCubeErrors.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                _currentException = ex;
                return InCubeErrors.Error;
            }
        }

        public InCubeErrors SetDateField(string fieldName, DateTime fieldValue)
        {
            try
            {
                string dateValue;
                _fieldNames.Add(fieldName);
                dateValue = DatabaseDateTimeManager.ParseDateAndTimeToSQL(fieldValue);
                _fieldValues.Add(dateValue);
                return InCubeErrors.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                _currentException = ex;
                return InCubeErrors.Error;
            }
        }

        #region Non transactional
        public InCubeErrors UpdateQueryString(string tableName, string where, InCubeDatabase database)
        {
            InCubeErrors result;
            try
            {
                string Query = " Update " + tableName + " SET ";

                for (int i = 0; i < _fieldNames.Count; i++)
                {
                    Query += _fieldNames[i] + " = " + _fieldValues[i];

                    if (i != _fieldNames.Count - 1)
                    {
                        Query += ",";
                    }
                }

                Query += " WHERE " + where;

                result = RunQuery(Query, database);
                _fieldNames.Clear();
                _fieldValues.Clear(); 
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                _currentException = ex;
                result = InCubeErrors.Error;
            }
            return result;
        }

        public InCubeErrors UpdateQueryString(string tableName, InCubeDatabase database)
        {
            InCubeErrors result;
            try
            {
                string Query = " Update " + tableName + " SET ";

                for (int i = 0; i < _fieldNames.Count; i++)
                {
                    Query += _fieldNames[i] + " = " + _fieldValues[i];

                    if (i != _fieldNames.Count - 1)
                    {
                        Query += ",";
                    }
                }

                result = RunQuery(Query, database);
                _fieldNames.Clear();
                _fieldValues.Clear();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                _currentException = ex;
                result = InCubeErrors.Error;
            }
            return result;
        }

        public InCubeErrors InsertQueryString(string tableName, InCubeDatabase database)
        {
            InCubeErrors result;
            try
            {

                string Query = " Insert Into " + tableName + " (";

                for (int i = 0; i < _fieldNames.Count; i++)
                {
                    Query += _fieldNames[i];

                    if (i != _fieldNames.Count - 1)
                    {
                        Query += ",";
                    }
                }

                Query += ") Values (";

                for (int i = 0; i < _fieldValues.Count; i++)
                {
                    Query += _fieldValues[i];

                    if (i != _fieldValues.Count - 1)
                    {
                        Query += ",";
                    }
                }

                Query += ")";

                result = RunQuery(Query, database);
                _fieldNames.Clear();
                _fieldValues.Clear();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                _currentException = ex;
                result = InCubeErrors.Error;
            }
            return result;
        }

        public InCubeErrors RunQuery(string sqlQuery, InCubeDatabase database)
        {
            InCubeErrors result;
            try
            {
                LastExecutedStatement = sqlQuery;
                InCubeQuery Query = new InCubeQuery(database, sqlQuery);
                int affectedRowCount = -1;
                result = Query.ExecuteNonQuery(ref affectedRowCount);
                Query.Close();
                if (result != InCubeErrors.Success)
                {
                    Exception ex = Query.GetCurrentException();
                    _currentException = ex;
                }
                if (affectedRowCount <= 0)
                {
                    result = InCubeErrors.SuccessWithZeroRowAffected;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                _currentException = ex;
                result = InCubeErrors.Error;
            }
            return result;
        }
        #endregion

        #region Transactional
        public InCubeErrors UpdateQueryString(string tableName, string where, InCubeDatabase database, InCubeTransaction _trans)
        {
            InCubeErrors result;
            try
            {
                string Query = " Update " + tableName + " SET ";

                for (int i = 0; i < _fieldNames.Count; i++)
                {
                    Query += _fieldNames[i] + " = " + _fieldValues[i];

                    if (i != _fieldNames.Count - 1)
                    {
                        Query += ",";
                    }
                }

                Query += " WHERE " + where;

                result = RunQuery(Query, database, _trans);
                _fieldNames.Clear();
                _fieldValues.Clear();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                _currentException = ex;
                result = InCubeErrors.Error;
            }
            return result;
        }

        public InCubeErrors UpdateQueryString(string tableName, InCubeDatabase database, InCubeTransaction _trans)
        {
            InCubeErrors result;
            try
            {
                string Query = " Update " + tableName + " SET ";

                for (int i = 0; i < _fieldNames.Count; i++)
                {
                    Query += _fieldNames[i] + " = " + _fieldValues[i];

                    if (i != _fieldNames.Count - 1)
                    {
                        Query += ",";
                    }
                }

                result = RunQuery(Query, database, _trans);
                _fieldNames.Clear();
                _fieldValues.Clear();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                _currentException = ex;
                result = InCubeErrors.Error;
            }
            return result;
        }

        public InCubeErrors InsertQueryString(string tableName, InCubeDatabase database, InCubeTransaction _trans)
        {
            InCubeErrors result;
            try
            {

                string Query = " Insert Into " + tableName + " (";

                for (int i = 0; i < _fieldNames.Count; i++)
                {
                    Query += _fieldNames[i];

                    if (i != _fieldNames.Count - 1)
                    {
                        Query += ",";
                    }
                }

                Query += ") Values (";

                for (int i = 0; i < _fieldValues.Count; i++)
                {
                    Query += _fieldValues[i];

                    if (i != _fieldValues.Count - 1)
                    {
                        Query += ",";
                    }
                }

                Query += ")";

                result = RunQuery(Query, database, _trans);
                _fieldNames.Clear();
                _fieldValues.Clear();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                _currentException = ex;
                result = InCubeErrors.Error;
            }
            return result;
        }

        public InCubeErrors RunQuery(string sqlQuery, InCubeDatabase database, InCubeTransaction _trans)
        {
            InCubeErrors result;
            try
            {
                LastExecutedStatement = sqlQuery;
                InCubeQuery Query = new InCubeQuery(database, sqlQuery);
                int affectedRowCount = -1;
                result = Query.ExecuteNoneQuery(_trans, ref affectedRowCount);
                Query.Close();
                if (result != InCubeErrors.Success)
                {
                    Exception ex = Query.GetCurrentException();
                    _currentException = ex;
                }
                if (affectedRowCount <= 0)
                {
                    result = InCubeErrors.Error;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                _currentException = ex;
                result = InCubeErrors.Error;
            }
            return result;
        }
        #endregion
    }
}
