using InCubeLibrary;
using System;

namespace InCubeIntegration_DAL
{
    public class DatabaseDateTimeManager
    {
        public static string ParseDateToSQLString(DateTime value)
        {
            try
            {
                string result = string.Empty;
                result = "  CONVERT(datetime, '" + value.Year + "/" + value.Month + "/" + value.Day + " 00:00:00', 102) ";
                return result;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                return string.Empty;
            }
        }
        public static string ParseDateAndTimeToSQL(DateTime value)
        {
            try
            {
                string result = string.Empty;
                result = "  CONVERT(datetime, '" + value.Year + "/" + value.Month + "/" + value.Day + " 00:00:00', 102) ";
                return result;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                return string.Empty;
            }
        }
    }
}
