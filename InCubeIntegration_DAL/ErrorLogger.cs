using System;
using System.Diagnostics;
using System.IO;
namespace InCubeIntegration_DAL
{
    public class ErrorLogger
    {
        public const Int64 ErrorLogFileSize = 1048576;//.. 1 MB
        public string ErrorLogFileName = "InCubelog.txt";
        public string ErrorLogFilePath;
        string _source = "InCube";
        string _log = "InCube";
        public static bool LogToEventLog = false;


        public string Source
        {
            get { return _source; }
            set { _source = value; }
        }

        public void LogError(string className, string functionName, string errMsg, string stackTrace)
        {
            if (LogToEventLog)
            {
                LogError(className, functionName, errMsg, stackTrace, true);
                return;
            }
            string str;
            string errorLogFilePath;
            StreamWriter sw = null;
            try
            {
                if (!string.IsNullOrEmpty(ErrorLogFilePath) && Directory.Exists(ErrorLogFilePath))
                {
                    errorLogFilePath = Path.Combine(ErrorLogFilePath, ErrorLogFileName);
                }
                else
                {
                    errorLogFilePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase), ErrorLogFileName);
                    errorLogFilePath = new Uri(errorLogFilePath).LocalPath;
                }
                if (File.Exists(errorLogFilePath) == false)
                {
                    File.Create(errorLogFilePath).Close();
                }
                FileInfo info = new FileInfo(errorLogFilePath);
                if (info.Exists && info.Length > 5242880)
                {
                    File.Move(errorLogFilePath, errorLogFilePath.Substring(0, errorLogFilePath.Length - 4) + " " + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".arc");
                }


                File.Open(errorLogFilePath, FileMode.OpenOrCreate).Close();
                sw = new StreamWriter(errorLogFilePath, true);
                str = DateTime.Now.ToString("dd/MMM/yyyy HH:mm:ss");
                str += string.Format("[Class: {0}] [Function: {1}] [Error: {2}] [Stack: {3}]", className, functionName, errMsg, stackTrace);
                sw.WriteLine(str);
                sw.WriteLine("------------------------------------------------");

            }
            catch
            {

            }

            finally
            {
                if (sw != null)
                {
                    sw.Flush();
                    sw.Close();
                    sw = null;
                }
            }
        }

        public void LogError(string className, string functionName, string errMsg, string stackTrace, bool logToEventLog)
        {
            string str;
            try
            {
                if (logToEventLog)
                {
                    str = DateTime.Now.ToString("dd/MMM/yyyy HH:mm:ss");
                    str += string.Format("[Class: {0}] [Function: {1}] [Error: {2}] [Stack: {3}]", className, functionName, errMsg, stackTrace);

                    if (!EventLog.SourceExists(_source))
                        EventLog.CreateEventSource(_source, _log);

                    EventLog.WriteEntry(_source, str);

                }
                else
                {
                    LogError(className, functionName, errMsg, stackTrace);
                }

            }
            catch (Exception)
            {

            }

        }


    }
}