using InCubeIntegration_DAL;
using InCubeLibrary;
using System;
using System.Data;
using System.Drawing;

namespace InCubeIntegration_BL
{
    public class ConfigurationsManger : System.IDisposable
    {
        InCubeDatabase db_Config;
        InCubeQuery incubeQuery;
        public bool ConnectionOpened
        {
            get { return db_Config.Opened; }
        }
        public ConfigurationsManger()
        {
            db_Config = new InCubeDatabase();
            db_Config.Open("InCube", "ConfigurationsManger");
        }
        public Result LoadConfigurations()
        {
            Result res = Result.Failure;
            DataTable dtConfigurations = null;
            try
            {
                CoreGeneral.Common.OrganizationConfigurations.Clear();

                incubeQuery = new InCubeQuery(string.Format(@"IF ((SELECT KeyValue FROM Int_Configuration WHERE KeyName = 'OrganizationOriented') = 'TRUE')
SELECT KeyName, KeyValue, OrganizationID FROM Int_Configuration WHERE OrganizationID = -1
UNION
SELECT C.KeyName, ISNULL(O.KeyValue,C.KeyValue) KeyValue, C.OrganizationID FROM (
SELECT C.ConfigurationID, C.KeyName, C.KeyValue, O.OrganizationID FROM Int_Configuration C
CROSS JOIN Organization O
WHERE C.OrganizationID = -1) C
LEFT JOIN Int_Configuration O ON O.ConfigurationID = C.ConfigurationID AND O.OrganizationID = C.OrganizationID
ELSE
SELECT KeyName, KeyValue, OrganizationID FROM Int_Configuration WHERE OrganizationID = -1"), db_Config);

                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtConfigurations = incubeQuery.GetDataTable();
                    if (dtConfigurations.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dtConfigurations.Rows)
                        {
                            string keyName = dr["KeyName"].ToString();
                            string keyValue = dr["KeyValue"].ToString();
                            int OrganizationID = int.Parse(dr["OrganizationID"].ToString());
                            if (OrganizationID != -1 && !CoreGeneral.Common.OrganizationConfigurations.ContainsKey(OrganizationID))
                            {
                                CoreGeneral.Common.OrganizationConfigurations.Add(OrganizationID, new Configurations());
                            }
                            switch (keyName)
                            {
                                case "WS_URL":
                                    CoreGeneral.Common.GeneralConfigurations.WS_URL = keyValue;
                                    break;
                                case "WS_UserName":
                                    CoreGeneral.Common.GeneralConfigurations.WS_UserName = keyValue;
                                    break;
                                case "WS_Password":
                                    CoreGeneral.Common.GeneralConfigurations.WS_Password = keyValue;
                                    break;
                                case "ConditionalSymbol":
                                    CoreGeneral.Common.GeneralConfigurations.ConditionalSymbol = keyValue;
                                    break;
                                case "SiteSymbol":
                                    CoreGeneral.Common.GeneralConfigurations.SiteSymbol = keyValue;
                                    break;
                                case "LoginRequired":
                                    CoreGeneral.Common.GeneralConfigurations.LoginRequired = bool.Parse(keyValue);
                                    break;
                                case "OrganizationOriented":
                                    CoreGeneral.Common.GeneralConfigurations.OrganizationOriented = bool.Parse(keyValue);
                                    break;
                                case "WindowsServiceEnabled":
                                    CoreGeneral.Common.GeneralConfigurations.WindowsServiceEnabled = bool.Parse(keyValue);
                                    break;
                                case "IntegrationFormTitle":
                                    if (OrganizationID == -1)
                                        CoreGeneral.Common.GeneralConfigurations.IntegrationFormTitle = keyValue;
                                    else
                                        CoreGeneral.Common.OrganizationConfigurations[OrganizationID].IntegrationFormTitle = keyValue;
                                    break;
                                case "IntegrationFormBackColor":
                                    if (OrganizationID == -1)
                                        CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor = Color.FromArgb(int.Parse(keyValue));
                                    else
                                        CoreGeneral.Common.OrganizationConfigurations[OrganizationID].IntegrationFormBackColor = Color.FromArgb(int.Parse(keyValue));
                                    break;
                                case "AppServerHost":
                                    if (OrganizationID == -1)
                                        CoreGeneral.Common.GeneralConfigurations.AppServerHost = keyValue;
                                    else
                                        CoreGeneral.Common.OrganizationConfigurations[OrganizationID].AppServerHost = keyValue;
                                    break;
                                case "Name":
                                    if (OrganizationID == -1)
                                        CoreGeneral.Common.GeneralConfigurations.Name = keyValue;
                                    else
                                        CoreGeneral.Common.OrganizationConfigurations[OrganizationID].Name = keyValue;
                                    break;
                                case "User":
                                    if (OrganizationID == -1)
                                        CoreGeneral.Common.GeneralConfigurations.User = keyValue;
                                    else
                                        CoreGeneral.Common.OrganizationConfigurations[OrganizationID].User = keyValue;
                                    break;
                                case "Password":
                                    if (OrganizationID == -1)
                                        CoreGeneral.Common.GeneralConfigurations.Password = keyValue;
                                    else
                                        CoreGeneral.Common.OrganizationConfigurations[OrganizationID].Password = keyValue;
                                    break;
                                case "Client":
                                    if (OrganizationID == -1)
                                        CoreGeneral.Common.GeneralConfigurations.Client = keyValue;
                                    else
                                        CoreGeneral.Common.OrganizationConfigurations[OrganizationID].Client = keyValue;
                                    break;
                                case "Language":
                                    if (OrganizationID == -1)
                                        CoreGeneral.Common.GeneralConfigurations.Language = keyValue;
                                    else
                                        CoreGeneral.Common.OrganizationConfigurations[OrganizationID].Language = keyValue;
                                    break;
                                case "SystemNumber":
                                    if (OrganizationID == -1)
                                        CoreGeneral.Common.GeneralConfigurations.SystemNumber = keyValue;
                                    else
                                        CoreGeneral.Common.OrganizationConfigurations[OrganizationID].SystemNumber = keyValue;
                                    break;
                                case "SystemID":
                                    if (OrganizationID == -1)
                                        CoreGeneral.Common.GeneralConfigurations.SystemID = keyValue;
                                    else
                                        CoreGeneral.Common.OrganizationConfigurations[OrganizationID].SystemID = keyValue;
                                    break;
                                case "DefaultMenu":
                                    CoreGeneral.Common.GeneralConfigurations.DefaultMenu = (Menus)int.Parse(keyValue);
                                    break;
                                case "InboundStagingDB":
                                    CoreGeneral.Common.GeneralConfigurations.InboundStagingDB = keyValue;
                                    break;
                                case "OutboundStagingDB":
                                    CoreGeneral.Common.GeneralConfigurations.OutboundStagingDB = keyValue;
                                    break;
                                case "TAXDTLID":
                                    CoreGeneral.Common.GeneralConfigurations.TAXDTLID = keyValue;
                                    break;
                                case "EXCISEDTLID":
                                    CoreGeneral.Common.GeneralConfigurations.EXCISEDTLID = keyValue;
                                    break;
                                case "SSL_File":
                                    CoreGeneral.Common.GeneralConfigurations.SSL_File = keyValue;
                                    break;
                                case "SSL_Key":
                                    CoreGeneral.Common.GeneralConfigurations.SSL_Key = keyValue;
                                    break;
                                case "WS_Queues_Mode":
                                    CoreGeneral.Common.GeneralConfigurations.WS_Queues_Mode = (Queues_Mode)(int.Parse(keyValue));
                                    break;
                                case "DefaultPaymentTermDays":
                                    CoreGeneral.Common.GeneralConfigurations.DefaultPaymentTermDays = int.Parse(keyValue);
                                    break;
                                case "CurrencyCode":
                                    CoreGeneral.Common.GeneralConfigurations.CurrencyCode = keyValue;
                                    break;
                                case "WS_Machine_Name":
                                    CoreGeneral.Common.GeneralConfigurations.WS_Machine_Name = keyValue;
                                    break;
                            }
                        }
                        res = Result.Success;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
            return res;
        }
        public Result GetListOfEditableConfiguraitons(ref DataTable dtEditableConfigurations, ref bool ConfigOnOrgLevel)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery("SELECT DISTINCT ConfigurationID, KeyName, DataType FROM Int_Configuration WHERE ReadOnly = 0", db_Config);
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtEditableConfigurations = incubeQuery.GetDataTable();
                    if (dtEditableConfigurations.Rows.Count > 0)
                        res = Result.Success;
                    else
                        res = Result.NoRowsFound;
                }

                incubeQuery = new InCubeQuery("SELECT COUNT(*) FROM Int_Configuration WHERE OrganizationID <> -1", db_Config);
                object field = null;
                if (incubeQuery.ExecuteScalar(ref field) == InCubeErrors.Success)
                {
                    ConfigOnOrgLevel = Convert.ToInt16(field) > 0;
                }
                else
                    res = Result.Failure;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
                res = Result.Failure;
            }
            return res;
        }
        public Result GetConfigurationValues(int ConfigurationID, ref DataTable dtConfigValues)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db_Config, string.Format(@"IF ((SELECT KeyValue FROM Int_Configuration WHERE KeyName = 'OrganizationOriented') = 'TRUE')

SELECT O.OrganizationID,O.OrganizationCode
,ISNULL(C.KeyValue,(SELECT KeyValue FROM Int_Configuration WHERE ConfigurationID = {0} AND OrganizationID = -1)) KeyValue FROM (
SELECT -1 OrganizationID,'Default' OrganizationCode UNION
SELECT OrganizationID,OrganizationCode FROM Organization WHERE OrganizationID IN ({1})) O
LEFT JOIN (SELECT OrganizationID,KeyValue FROM Int_Configuration WHERE ConfigurationID = {0}) C
ON C.OrganizationID = O.OrganizationID
ELSE
SELECT -1 OrganizationID, 'Default' OrganizationCode, KeyValue 
FROM Int_Configuration WHERE ConfigurationID = {0} AND OrganizationID = -1", ConfigurationID, CoreGeneral.Common.userPrivileges.Organizations));
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtConfigValues = incubeQuery.GetDataTable();
                    if (dtConfigValues.Rows.Count > 0)
                        res = Result.Success;
                    else
                        res = Result.NoRowsFound;
                }
                else
                {
                    res = Result.Failure;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
                res = Result.Failure;
            }
            return res;
        }
        public Result AddEditConfigurationValue(int ConfigurationID, int OrganizationID, string Value)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(string.Format(@"IF EXISTS (SELECT * FROM Int_Configuration WHERE ConfigurationID = {0} AND OrganizationID = {1})
	UPDATE Int_Configuration SET KeyValue = '{2}' WHERE ConfigurationID = {0} AND OrganizationID = {1};
ELSE
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType,ReadOnly)
	SELECT ConfigurationID,KeyName,'{2}',{1},DataType,ReadOnly FROM Int_Configuration
	WHERE ConfigurationID = {0} AND OrganizationID = -1",ConfigurationID, OrganizationID, Value), db_Config);
                if (incubeQuery.ExecuteNonQuery() == InCubeErrors.Success)
                    res = Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
                res = Result.Failure;
            }
            return res;
        }
        public void Dispose()
        {
            try
            {
                if (incubeQuery != null)
                    incubeQuery.Close();

                if (db_Config != null)
                    db_Config.Dispose();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
    }
}
