using System;
using System.Data;
using System.Data.SqlClient;
using InCubeLibrary;

namespace InCubeIntegration_DAL
{
    public class InCubeTransaction
    {

        private Exception _currentException;

        public Exception CurrentException
        {
            get { return _currentException; }
        }

//#if SQL_SERVER
        public SqlTransaction Transaction;
//#else
//#if ORACLE
//        public OracleTransaction Transaction;
//#endif
//#endif

        public InCubeErrors BeginTransaction(InCubeDatabase DB)
        {
            InCubeErrors result= InCubeErrors.NotInitialized;
            try
            {
                if ((DB != null ) && DB.IsOpened() && (DB.GetConnection() != null) )
                {   
//#if SQL_SERVER
                    Transaction = DB.GetConnection().BeginTransaction(IsolationLevel.ReadUncommitted);    
//#else
//#if ORACLE
//                    Transaction = DB.GetConnection().BeginTransaction();
//#endif
//#endif
                    result =  InCubeErrors.Success;
                }
                else
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
        public InCubeErrors Commit()
        {
            InCubeErrors result= InCubeErrors.NotInitialized;
            try
            {
                if (Transaction != null)
                {
                    Transaction.Commit();
                    result = InCubeErrors.Success;
                }
                else
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
        public InCubeErrors Rollback()
        {
            InCubeErrors result = InCubeErrors.NotInitialized;
            try
            {
                if (Transaction != null)
                {
                    Transaction.Rollback();
                    result= InCubeErrors.Success;
                }
                else
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
    }
}