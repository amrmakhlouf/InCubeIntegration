using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace InCubeLibrary
{
    public enum LoggingFiles
    {
        InCubeLog,
        errorInv,
        errorRet,
        errorPay,
        errorOrd,
        WindowsService,
        WindowsServiceErrors,
        ATM,
        UIErrors,
        errorTSFR
    }
    public enum LoggingType
    {
        Warning,
        Error,
        Information
    }

    public class Logger
    {
        public static void WriteLog(string ClassName, string MethodName, string Message, LoggingType type, LoggingFiles file)
        {
            WriteLog(ClassName, MethodName, Message, string.Empty, type, file);
        }
        public static void WriteLog(string ClassName, string MethodName, string Message, string StackTrace, LoggingType type, LoggingFiles file)
        {
            try
            {
                if (file == LoggingFiles.WindowsService && type == LoggingType.Error)
                    file = LoggingFiles.WindowsServiceErrors;

                string classLine = string.Empty;
                if (ClassName != string.Empty)
                {
                    classLine = "\r\nClass: " + ClassName;
                }

                string methodLine = string.Empty;
                if (MethodName != string.Empty)
                {
                    methodLine = "\r\nMethod: " + MethodName;
                }

                string stackTraceLine = string.Empty;
                if (StackTrace != string.Empty)
                {
                    stackTraceLine = "\r\nStack Trace: " + StackTrace;
                }

                string header = string.Empty;
                switch (type)
                {
                    case LoggingType.Error:
                        header = @"*****************************************************
*** ERROR *** ERROR *** ERROR *** ERROR *** ERROR *** 
*****************************************************";
                        break;

                    case LoggingType.Warning:
                        header = @"###################################
### WARNING ########### WARNING ###
###################################";
                        break;

                    case LoggingType.Information:
                        header = @"-----------------------------------
--- INFORMATION --- INFORMATION ---
-----------------------------------";
                        break;
                }

                string ErrorFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Substring(6) + "\\";
                switch (file)
                {
                    case LoggingFiles.InCubeLog:
                        ErrorFilePath += file.ToString() + ".txt";
                        break;
                    default:
                        ErrorFilePath += file.ToString() + ".log";
                        break;
                }

                FileInfo info = new FileInfo(ErrorFilePath);
                if (info.Exists && info.Length > 5242880)
                {
                    File.Move(ErrorFilePath, ErrorFilePath.Substring(0, ErrorFilePath.Length - 4) + " " + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".arc");
                }

                string timeLine = "Date and Time: " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");

                string Log = header + "\r\n" + timeLine + classLine + methodLine + stackTraceLine + "\r\n\r\n" + Message + "\r\n" + string.Empty.PadRight(100, '=') + "\r\n";

                File.AppendAllText(ErrorFilePath, Log);
            }
            catch
            {
                
            }
        }
    }
}
