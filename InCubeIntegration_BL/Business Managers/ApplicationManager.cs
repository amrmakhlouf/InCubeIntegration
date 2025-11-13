using InCubeIntegration_DAL;
using InCubeLibrary;
using System;
using System.ComponentModel;
using System.Data;
using System.IO;

namespace InCubeIntegration_BL
{
    public class ApplicationManager : System.IDisposable
    {
        InCubeQuery incubeQuery;
        private InCubeDatabase db_App;
        BackgroundWorker bgwKeepAlive = null;

        public bool ConnectionOpened
        {
            get { return db_App.Opened; }
        }
        public ApplicationManager()
        {
            db_App = new InCubeDatabase();
            db_App.Open("InCube", "ApplicationManager");
        }
        public void EncryptText(ref string text)
        {
            InCubeSecurityClass cls = new InCubeSecurityClass();
            text = cls.EncryptData(text);
        }
        public Result GetIntegrationVersions(out long AppVersion, out int dbVersion, out int ClientVersion)
        {
            AppVersion = 0;
            dbVersion = 0;
            ClientVersion = 999;
            Result res = Result.UnKnown;
            try
            {
                object obj = null;
                incubeQuery = new InCubeQuery(db_App, "SELECT KeyValue FROM Int_Configuration WHERE KeyName = 'AppVersion'");
                if (incubeQuery.ExecuteScalar(ref obj) == InCubeErrors.Success && obj != null && !string.IsNullOrEmpty(obj.ToString()))
                {
                    AppVersion = long.Parse(obj.ToString());
                }
                //else
                //{
                //    return Result.Failure;
                //}

                incubeQuery = new InCubeQuery(db_App, "SELECT KeyValue FROM Int_Configuration WHERE KeyName = 'DBVersion'");
                if (incubeQuery.ExecuteScalar(ref obj) == InCubeErrors.Success && obj != null && !string.IsNullOrEmpty(obj.ToString()))
                {
                    dbVersion = int.Parse(obj.ToString());
                }
                //else
                //{
                //    return Result.Failure;
                //}
                return Result.Success;
                //incubeQuery = new InCubeQuery(db_App, "SELECT KeyValue FROM Int_Configuration WHERE KeyName = 'ClientVersion'");
                //if (incubeQuery.ExecuteScalar(ref obj) == InCubeErrors.Success && obj != null && !string.IsNullOrEmpty(obj.ToString()))
                //{
                //    ClientVersion = int.Parse(obj.ToString());
                //}
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
        }
        public void UpdateAppVersion(long AppVersion)
        {
            try
            {
                incubeQuery = new InCubeQuery(db_App, "UPDATE Int_Configuration SET KeyValue = '" + AppVersion + "' WHERE KeyName = 'AppVersion'");
                incubeQuery.ExecuteNonQuery();
            }
            catch
            {

            }
        }
        public void RunCustomerScripts(string ApplicationPath, int dbVersion, int clientVersion)
        {
            try
            {
                DirectoryInfo scriptsDir = new DirectoryInfo(ApplicationPath + "\\Scripts");
                string[] scripts = Directory.GetFiles(ApplicationPath + "\\Scripts", "*.sql");
                Array.Sort(scripts);

                if (scriptsDir.Exists)
                {
                    for (int i = 0; i < scripts.Length; i++)
                    {
                        string scriptName = Path.GetFileNameWithoutExtension(scripts[i]);
                        if (scriptName.StartsWith("S") && int.Parse(scriptName.Substring(1, scriptName.IndexOf(' ') - 1)) > dbVersion)
                        {
                            incubeQuery = new InCubeQuery(db_App, File.ReadAllText(scripts[i]));
                            if (incubeQuery.ExecuteNonQuery() == InCubeErrors.Success)
                            {
                                incubeQuery = new InCubeQuery(db_App, "UPDATE Int_Configuration SET KeyValue = '" + int.Parse(scriptName.Substring(1, scriptName.IndexOf(' ') - 1)) + "' WHERE KeyName = 'DBVersion'");
                                incubeQuery.ExecuteNonQuery();
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    //for (int i = 0; i < scripts.Length; i++)
                    //{
                    //    string scriptName = Path.GetFileNameWithoutExtension(scripts[i]);
                    //    if (scriptName.StartsWith("C") && int.Parse(scriptName.Substring(1, scriptName.IndexOf(' ') - 1)) > clientVersion)
                    //    {
                    //        incubeQuery = new InCubeQuery(db_App, File.ReadAllText(scripts[i]));
                    //        if (incubeQuery.ExecuteNonQuery() == InCubeErrors.Success)
                    //        {
                    //            incubeQuery = new InCubeQuery(db_App, "UPDATE Int_Configuration SET KeyValue = '" + int.Parse(scriptName.Substring(1, scriptName.IndexOf(' ') - 1)) + "' WHERE KeyName = 'ClientVersion'");
                    //            incubeQuery.ExecuteNonQuery();
                    //        }
                    //        else
                    //        {
                    //            break;
                    //        }
                    //    }
                    //}
                }
            }
            catch
            {

            }
        }
        public Result Login(string UserName, string Password, LoginType loginType)
        {
            Result res = Result.Failure;
            try
            {
                InCubeSecurityClass cls = new InCubeSecurityClass();
                string qry = "";
                if (loginType == LoginType.NoLoginForm)
                    qry = string.Format(@"SELECT OperatorID FROM Operator WHERE OperatorName = '{0}'", UserName);
                else if (loginType == LoginType.User)
                    qry = string.Format(@"SELECT OperatorID FROM Operator WHERE OperatorName = '{0}' AND OperatorPassword = '{1}'", UserName, cls.EncryptData(Password));
                if (loginType == LoginType.WindowsService)
                    qry = string.Format(@"SELECT OperatorID FROM Operator WHERE OperatorName = '{0}'", UserName);

                InCubeErrors err = InCubeErrors.Success;
                object field = null;

                if (loginType != LoginType.WindowsService)
                {
                    incubeQuery = new InCubeQuery(db_App, qry);
                    err = incubeQuery.ExecuteScalar(ref field);
                    res = Result.Success;
                }
                else
                {
                    if (loginType == LoginType.WindowsService)
                    {
                        incubeQuery = new InCubeQuery(db_App, qry);
                        err = incubeQuery.ExecuteScalar(ref field);
                        
                        if (field == null)
                        {

                            field = -1;
                        }
                        else
                        {
                            res = Result.Success;
                        }
                    }

                }

                if (err == InCubeErrors.Success)
                {
                    if (field == null || field == DBNull.Value || string.IsNullOrEmpty(field.ToString()))
                    {
                        res = Result.Invalid;
                    }
                    else
                    {
                        int userID = Convert.ToInt16(field);
                        if (CoreGeneral.Common.IsTesting)
                        {
                            LoadUserPrivileges(userID);
                            CreateSession(userID, UserName, Password, loginType);
                        }
                        else
                        {
                            incubeQuery = new InCubeQuery(db_App, string.Format(@"SELECT SessionID,EndTime,GETDATE() CurrentTime,LastActive FROM Int_Session WHERE SessionID IN (SELECT MAX(SessionID) FROM Int_Session WHERE UserID = {0})", userID));
                            if (incubeQuery.Execute() == InCubeErrors.Success)
                            {
                                DataTable dtSession = incubeQuery.GetDataTable();
                                if (dtSession != null && dtSession.Rows.Count == 1)
                                {
                                    if (dtSession.Rows[0]["EndTime"] == DBNull.Value)
                                    {
                                        if (Convert.ToDateTime(dtSession.Rows[0]["LastActive"]) < Convert.ToDateTime(dtSession.Rows[0]["CurrentTime"]).AddSeconds(-12) || loginType == LoginType.NoLoginForm)
                                        {
                                            if (loginType != LoginType.NoLoginForm)
                                            {
                                                incubeQuery = new InCubeQuery(db_App, string.Format("UPDATE Int_Session SET EndTime = LastActive WHERE SessionID = {0}", dtSession.Rows[0]["SessionID"].ToString()));
                                                res = incubeQuery.ExecuteNonQuery() == InCubeErrors.Success ? Result.Success : Result.Failure;
                                            }
                                            if (loginType == LoginType.WindowsService || LoadUserPrivileges(userID) == Result.Success)
                                            {
                                                res = Result.Success;
                                                CreateSession(userID, UserName, Password, loginType);
                                            }

                                        }
                                        else
                                        {
                                            res = Result.LoggedIn;
                                        }
                                    }
                                    else
                                    {
                                        if (loginType == LoginType.WindowsService || LoadUserPrivileges(userID) == Result.Success)
                                        {
                                            res = Result.Success;
                                            CreateSession(userID, UserName, Password, loginType);
                                        }
                                    }
                                }
                                else
                                {
                                    if (loginType == LoginType.WindowsService || LoadUserPrivileges(userID) == Result.Success)
                                    {
                                        res = Result.Success;
                                        CreateSession(userID, UserName, Password, loginType);
                                    }
                                }
                            }
                            else
                            {
                                res = Result.Failure;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public static Result LogOut()
        {
            Result res = Result.Failure;
            try
            {
                CoreGeneral.Common.CurrentSession.LoggedOut = true;

                InCubeDatabase db = new InCubeDatabase();
                db.Open("InCube", "ApplicationManager");
                InCubeQuery _incubeQuery = new InCubeQuery(db, string.Format("UPDATE Int_Session SET EndTime = GETDATE() WHERE SessionID = {0}", CoreGeneral.Common.CurrentSession.SessionID));
                _incubeQuery.ExecuteNonQuery();

                if (_incubeQuery != null)
                    _incubeQuery.Close();

                if (db != null)
                    db.Dispose();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public void KeepSessionAlive()
        {
            incubeQuery = new InCubeQuery(db_App, string.Format("UPDATE Int_Session SET LastActive = GETDATE() WHERE SessionID = {0}", CoreGeneral.Common.CurrentSession.SessionID));
            incubeQuery.ExecuteNonQuery();
        }
        private Result LoadUserOrganizationAccess(int UserID)
        {
            Result res = Result.Failure;
            try
            {
                string qry = string.Format(@"SELECT OrganizationID FROM EmployeeOrganization WHERE EmployeeID IN (SELECT EmployeeID FROM EmployeeOperator WHERE OperatorID = {0})", UserID);
                if (CoreGeneral.Common.GeneralConfigurations.SiteSymbol.ToLower() == "brf")
                    qry += " AND OrganizationID IN (SELECT OrganizationID FROM Organization WHERE ParentOrganizationID IS NOT NULL)";
                incubeQuery = new InCubeQuery(db_App, qry);

                DataTable dtOrgsAccess = new DataTable();
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtOrgsAccess = incubeQuery.GetDataTable();
                    if (dtOrgsAccess.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dtOrgsAccess.Rows)
                        {
                            string orgID = dr["OrganizationID"].ToString();

                            if (CoreGeneral.Common.userPrivileges.Organizations != "")
                                CoreGeneral.Common.userPrivileges.Organizations += ",";

                            CoreGeneral.Common.userPrivileges.Organizations += orgID;
                        }
                        if (CoreGeneral.Common.userPrivileges.Organizations != "")
                        {
                            res = Result.Success;
                        }
                    }
                    else
                    {
                        res = Result.NoRowsFound;
                    }
                }
                else
                {
                    res = Result.Failure;
                }

                if (res == Result.Success)
                {
                    qry = string.Format(@"SELECT OrganizationID FROM Employee WHERE EmployeeID IN (SELECT EmployeeID FROM EmployeeOperator WHERE OperatorID = {0})", UserID);
                    incubeQuery = new InCubeQuery(db_App, qry);
                    object Field = null;
                    if (incubeQuery.ExecuteScalar(ref Field) == InCubeErrors.Success)
                    {
                        if (Field != DBNull.Value || string.IsNullOrEmpty(Field.ToString()))
                        {
                            CoreGeneral.Common.userPrivileges.UserOrganizationID = int.Parse(Field.ToString());
                            qry = string.Format(@"SELECT OrganizationCode FROM Organization WHERE OrganizationID = {0}", Field);
                            incubeQuery = new InCubeQuery(db_App, qry);
                            Field = null;
                            if (incubeQuery.ExecuteScalar(ref Field) == InCubeErrors.Success)
                            {
                                if (Field != DBNull.Value || string.IsNullOrEmpty(Field.ToString()))
                                {
                                    CoreGeneral.Common.userPrivileges.UserOrgCode = Field.ToString();
                                }
                            }
                            res = Result.Success;
                        }
                        else
                        {
                            res = Result.NoRowsFound;
                        }
                    }
                    else
                    {
                        res = Result.Failure;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result LoadUserPrivileges(int UserID)
        {
            Result res = Result.Failure;
            try
            {
                LoadUserOrganizationAccess(UserID);

                incubeQuery = new InCubeQuery(db_App, string.Format(@"SELECT UP.PrivilegeID, UP.PrivilegeType, ISNULL(P.Description,F.FieldName) Description, F.ActionType, ISNULL(UP.DefaultCheck,0) DefaultCheck
FROM Int_UserPrivileges UP
LEFT JOIN Int_Privileges P ON P.PrivilegeID = UP.PrivilegeID AND P.PrivilegeType = UP.PrivilegeType AND UP.PrivilegeType IN ({1},{3})
LEFT JOIN Int_Field F ON F.FieldID = UP.PrivilegeID AND UP.PrivilegeType = {2}
WHERE UserID = {0} AND UP.PrivilegeType <> 3 ORDER BY UP.Sequence", UserID, PrivilegeType.MenuAccess.GetHashCode(), PrivilegeType.FieldAccess.GetHashCode(), PrivilegeType.MenuAction.GetHashCode()));
                DataTable dtUserPrivileges = new DataTable();

                res = incubeQuery.Execute() == InCubeErrors.Success ? Result.Success : Result.Failure;
                if (res == Result.Success)
                {
                    dtUserPrivileges = incubeQuery.GetDataTable();
                    if (dtUserPrivileges.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dtUserPrivileges.Rows)
                        {
                            int PrivilegeID = int.Parse(dr["PrivilegeID"].ToString());
                            string Description = dr["Description"].ToString();
                            bool DefaultCheck = Convert.ToBoolean(dr["DefaultCheck"].ToString());
                            PrivilegeType type = (PrivilegeType)(int.Parse(dr["PrivilegeType"].ToString()));

                            if (type == PrivilegeType.MenuAccess)
                            {
                                if (!CoreGeneral.Common.userPrivileges.MenusAccess.ContainsKey((Menus)PrivilegeID))
                                    CoreGeneral.Common.userPrivileges.MenusAccess.Add((Menus)PrivilegeID, Description);
                            }
                            else if (type == PrivilegeType.MenuAction)
                            {
                                if (!CoreGeneral.Common.userPrivileges.MenuActionAccess.ContainsKey((MenuActions)PrivilegeID))
                                    CoreGeneral.Common.userPrivileges.MenuActionAccess.Add((MenuActions)PrivilegeID, Description);
                            }
                            else if (type == PrivilegeType.FieldAccess)
                            {
                                FieldItem fieldItem = new FieldItem();
                                fieldItem.Type = (ActionType)int.Parse(dr["ActionType"].ToString());
                                fieldItem.Field = (IntegrationField)PrivilegeID;
                                fieldItem.Description = Description;
                                fieldItem.DefaultCheck = DefaultCheck;
                                switch (fieldItem.Type)
                                {
                                    case ActionType.Send:
                                        if (!CoreGeneral.Common.userPrivileges.SendFieldsAccess.ContainsKey(fieldItem.Field))
                                            CoreGeneral.Common.userPrivileges.SendFieldsAccess.Add(fieldItem.Field, fieldItem);
                                        break;
                                    case ActionType.Update:
                                        if (!CoreGeneral.Common.userPrivileges.UpdateFieldsAccess.ContainsKey(fieldItem.Field))
                                            CoreGeneral.Common.userPrivileges.UpdateFieldsAccess.Add(fieldItem.Field, fieldItem);
                                        break;
                                    case ActionType.SpecialFunctions:
                                        if (!CoreGeneral.Common.userPrivileges.SpecialFunctionsAccess.ContainsKey(fieldItem.Field))
                                            CoreGeneral.Common.userPrivileges.SpecialFunctionsAccess.Add(fieldItem.Field, fieldItem);
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private void CreateSession(int UserID, string UserName, string Password, LoginType loginType)
        {
            try
            {
                int sessionID = 0;
                int employeeID = 0;
                string employeeName = "";
                object field = null;
                if (!CoreGeneral.Common.IsTesting)
                {
                    string insertQuery = string.Format(@"INSERT INTO Int_Session (UserID,StartTime,LastActive,MachineName,VersionNo) VALUES ({0},GETDATE(),GETDATE(),'{1}','{2}');
                SELECT SCOPE_IDENTITY();", UserID, Environment.MachineName, CoreGeneral.Common.GeneralConfigurations.AppVersion);
                    incubeQuery = new InCubeQuery(db_App, insertQuery);
                    incubeQuery.ExecuteScalar(ref field);
                    sessionID = int.Parse(field.ToString());
                }
                else
                {
                    sessionID = -1;
                }
                if (loginType != LoginType.WindowsService)
                {
                    incubeQuery = new InCubeQuery(db_App, "SELECT EmployeeID FROM EmployeeOperator WHERE OperatorID = " + UserID);
                    incubeQuery.ExecuteScalar(ref field);
                    employeeID = int.Parse(field.ToString());

                    incubeQuery = new InCubeQuery(db_App, "SELECT Description FROM EmployeeLanguage WHERE LanguageID = 1 AND EmployeeID = " + employeeID);
                    incubeQuery.ExecuteScalar(ref field);
                    employeeName = field.ToString();
                }

                CoreGeneral.Common.CurrentSession.UserID = UserID;
                CoreGeneral.Common.CurrentSession.EmployeeName = employeeName;
                CoreGeneral.Common.CurrentSession.EmployeeID = employeeID;
                CoreGeneral.Common.CurrentSession.SessionID = sessionID;
                CoreGeneral.Common.CurrentSession.UserName = UserName;
                CoreGeneral.Common.CurrentSession.Password = Password;
                CoreGeneral.Common.CurrentSession.LoginTime = DateTime.Now;
                CoreGeneral.Common.CurrentSession.loginType = loginType;

                bgwKeepAlive = new BackgroundWorker();
                bgwKeepAlive.WorkerSupportsCancellation = true;
                bgwKeepAlive.DoWork += new DoWorkEventHandler(KeepSessionAliveWorker);
                bgwKeepAlive.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void KeepSessionAliveWorker(object o, DoWorkEventArgs e)
        {
            try
            {
                while (!CoreGeneral.Common.CurrentSession.LoggedOut)
                {
                    System.Threading.Thread.Sleep(10000);
                    if (!CoreGeneral.Common.CurrentSession.LoggedOut)
                        KeepSessionAlive();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsServiceErrors);
            }
        }
        public void Dispose()
        {
            try
            {
                if (bgwKeepAlive != null)
                    bgwKeepAlive.CancelAsync();

                if (incubeQuery != null)
                    incubeQuery.Close();

                if (db_App != null)
                    db_App.Dispose();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
    }
}
