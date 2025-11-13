using InCubeLibrary;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Xml;

namespace InCubeIntegration_DAL
{
    public enum InCubeFieldTypes
    {
        Unknown,
        Integer,
        Long,
        Text,
        Memo,
        Currency,
        DateTime,
        Boolean,
        OLEObject,
        Float,
        Real,
        Numeric
    }
    public class InCubeDatabase : IDisposable
    { 
        public bool Opened
        {
            get { return ConfiguredConnection.State == ConnectionState.Open; }
        }
        private string Name = "";
        public string Alias = "";
        private Exception CurrentException;
        private string _dataSourceFilePath;

        public InCubeDatabase()
        {
            _dataSourceFilePath = string.Empty;
        }
        internal SqlConnection ConfiguredConnection;
        public bool IsOpened()
        {
            return Opened;
        }
        public InCubeErrors Open(string databaseName, string alias)
        {
            Alias = alias;
            return Open(databaseName);
        }
        private InCubeErrors Open(string databaseName)
        {
            if (ConfiguredConnection != null && ConfiguredConnection.State == ConnectionState.Open)
                return InCubeErrors.DBDatabaseAlreadyOpened;

            ConfiguredConnection = new SqlConnection();
            try
            {
                if (string.IsNullOrEmpty(_dataSourceFilePath))
                {
                    _dataSourceFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase) + "\\DataSources.xml";
                }

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(_dataSourceFilePath);

                string ConnStr = (!Alias.Trim().Equals(string.Empty) ? "App=" + Alias + ";" : "");
                string conn = xmlDoc.SelectSingleNode("Connections/Connection[Name = '" + databaseName + "']/Data").InnerText;
                InCubeSecurityClass cls = new InCubeSecurityClass();
                conn = cls.DecryptData(conn);
                ConnStr += conn;
                ConfiguredConnection.ConnectionString = ConnStr;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                CurrentException = ex;
                return InCubeErrors.DBCannotReadConfigurationFile;
            }

            try
            {
                ConfiguredConnection.Open();
                Name = databaseName;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                CurrentException = ex;
                return InCubeErrors.DBCannotOpenDatabase;
            }

            return InCubeErrors.Success;

        }
        public SqlConnection GetConnection()
        {
            return ConfiguredConnection;
        }
        public InCubeErrors Close()
        {
            if (!Opened) return InCubeErrors.DBDatabaseNotOpened;
            try { ConfiguredConnection.Close(); }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                CurrentException = ex;
                return InCubeErrors.DBCannotCloseDatabase;
            }
            Name = "";
            return InCubeErrors.Success;
        }
        public void Dispose()
        {
            try
            {
                if (ConfiguredConnection != null)
                {
                    if (ConfiguredConnection.State == ConnectionState.Open)
                    {
                        ConfiguredConnection.Close();
                    }
                    SqlConnection.ClearPool(ConfiguredConnection);
                    ConfiguredConnection.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
    }
}