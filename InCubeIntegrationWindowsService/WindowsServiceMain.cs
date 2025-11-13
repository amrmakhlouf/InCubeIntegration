using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;

namespace InCubeIntegrationWindowsService
{
    class WindowsServiceMain  : System.ServiceProcess.ServiceBase
    {
        ApplicationManager AppManager = null;
        Dictionary<string, BackgroundWorker> WorkersList = new Dictionary<string, BackgroundWorker>();
        private void TriggerAction(DataRow drAction, string key)
        {
            lock (key)
            {
                try
                {
                    int TaskID = Convert.ToInt16(drAction["TaskID"]);
                    int OrganizationID = Convert.ToInt16(drAction["OrganizationID"]);
                    int ActionID = Convert.ToInt16(drAction["ActionID"]);
                    int FieldID = Convert.ToInt16(drAction["FieldID"]);
                    ActionType _actionType = (ActionType)Convert.ToInt16(drAction["ActionType"]);

                    //Fill filters dictionary
                    IntegrationFilters Filters = new IntegrationFilters(_actionType);

                    DataRow[] drActionFilters = CoreGeneral.Common.Queues[key].Filters.Select(string.Format("TaskID = {0} AND ActionID = {1}", TaskID, ActionID));

                    foreach (DataRow dr in drActionFilters)
                    {
                        Filters.SetValue((BuiltInFilters)int.Parse(dr["FilterID"].ToString()), dr["Value"].ToString());
                    }
                    Filters.SetValue(BuiltInFilters.Organization, OrganizationID);
                    
                    //Trigger the action
                    ExecutionManager execManager = null;
                    IntegrationBase IntegrationObj = null;
                    try
                    {
                        execManager = new ExecutionManager();
                        execManager.Action_Type = _actionType;
                        IntegrationObj = execManager.InitializeIntegrationObject();
                        IntegrationObj.OrganizationID = Filters.OrganizationID;
                        int TriggerID = execManager.LogActionTriggerBegining(TaskID, ActionID, FieldID);
                        if (TriggerID != -1)
                        {
                            Result res = execManager.TriggerAction(_actionType, FieldID, Filters, TaskID, ActionID, TriggerID, IntegrationObj);
                            execManager.LogActionTriggerEnding(TriggerID);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.WindowsService);
                    }
                    finally
                    {
                        if (execManager != null)
                            execManager.Dispose();
                        if (IntegrationObj != null)
                            IntegrationObj.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
                }
            }
        }
        private void RunQueue(object o, DoWorkEventArgs e)
        {
            try
            {
                string key = e.Argument.ToString();
                lock (key)
                {
                    DataTable dtQueue = CoreGeneral.Common.Queues[key].Schedules;

                    while (dtQueue.Rows.Count > 0 && !CoreGeneral.Common.CurrentSession.LoggedOut)
                    {
                        DateTime nextSchedule = Convert.ToDateTime(dtQueue.Rows[0]["ScheduledTime"]);
                        if (nextSchedule <= DateTime.Now && !CoreGeneral.Common.Queues[key].IsRunning)
                        {
                            if (!CoreGeneral.Common.CurrentSession.LoggedOut)
                            {
                                CoreGeneral.Common.Queues[key].IsRunning = true;
                                TriggerAction(dtQueue.Rows[0], key);
                                CoreGeneral.Common.Queues[key].IsRunning = false;
                                dtQueue.Rows.RemoveAt(0);
                            }
                        }
                        else
                        {
                            Thread.Sleep(60000 - int.Parse(DateTime.Now.Second.ToString() + DateTime.Now.Millisecond.ToString()));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsServiceErrors);
            }
        }
        protected override void OnStart(string[] args)
        {
            try
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "Service Started ..", LoggingType.Information, LoggingFiles.WindowsService);
                Initialize();
                Thread thdCheckForNewDay = new Thread(new ThreadStart(CheckForNewDay));
                thdCheckForNewDay.Start();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
        }
        private void CheckForNewDay()
        {
            try
            {
                DateTime today = DateTime.Today;
                while (today == DateTime.Today)
                {
                    Thread.Sleep(60000 - int.Parse(DateTime.Now.Second.ToString() + DateTime.Now.Millisecond.ToString()));
                }
                StopWorkers();
                PrepareThreads();
                CheckForNewDay();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
        }
        private void Initialize()
        {
            ConfigurationsManger configManager = null;
            try
            {
                Result res = Result.UnKnown;

                //Load Application Version
                string AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                long appVersion = CoreGeneral.Common.GetLongVersionNumber(AssemblyVersion);
                CoreGeneral.Common.GeneralConfigurations.AppVersion = CoreGeneral.Common.FormatVersionNumber(AssemblyVersion);

                //Initialize app manager
                AppManager = new ApplicationManager();
                if (!AppManager.ConnectionOpened)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "Error in initializing AppManager ..", LoggingType.Error, LoggingFiles.WindowsService);
                    this.Stop();
                    return;
                }

                //Get App,DB and Client versions
                long currentAppVersion = 0;
                int dbVersion = 0, clientVersion = 0;
                if (AppManager.GetIntegrationVersions(out currentAppVersion, out dbVersion, out clientVersion) != Result.Success)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "Error reading application version ..", LoggingType.Error, LoggingFiles.WindowsService);
                    this.Stop();
                    return;
                }

                //Check Version
                if (currentAppVersion > appVersion)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "DB version number is higher than WS version ..", LoggingType.Error, LoggingFiles.WindowsService);
                    this.Stop();
                    return;
                }

                //Loading configurations
                configManager = new ConfigurationsManger();
                res = configManager.LoadConfigurations();
                if (!configManager.ConnectionOpened || res != Result.Success)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "Error in loading configurations ..", LoggingType.Error, LoggingFiles.WindowsService);
                    this.Stop();
                    return;
                }
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "Configurations loaded ..", LoggingType.Information, LoggingFiles.WindowsService);

                //Login
                res = AppManager.Login("", "", LoginType.WindowsService);
                if (res != Result.Success)
                {
                    res = AppManager.Login("Service", "", LoginType.WindowsService);
                    if (res != Result.Success)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "Log in failed .. Result: " + res.ToString(), LoggingType.Error, LoggingFiles.WindowsService);
                        this.Stop();
                        return;
                    }

                }
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "WS user logged in ..", LoggingType.Information, LoggingFiles.WindowsService);
                
                PrepareThreads();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
            finally
            {
                if (configManager != null)
                    configManager.Dispose();
            }
        }
        private void PrepareThreads()
        {
            WindowsServiceManager _wsManager = new WindowsServiceManager(true);
            WorkersList = new Dictionary<string, BackgroundWorker>();
            try
            {
                if (_wsManager.ConnectionOpened)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "Database Opened", LoggingType.Information, LoggingFiles.WindowsService);

                    if (_wsManager.PrepareIntegrationThreads() == Result.Success)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "Threads keys imported ..", "Number of threads: " + CoreGeneral.Common.Queues.Count, LoggingType.Information, LoggingFiles.WindowsService);

                        foreach (string key in CoreGeneral.Common.Queues.Keys)
                        {
                            try
                            {
                                if (!WorkersList.ContainsKey(key))
                                {
                                    BackgroundWorker bgw = new BackgroundWorker();
                                    bgw.DoWork += new DoWorkEventHandler(RunQueue);
                                    bgw.WorkerSupportsCancellation = true;
                                    WorkersList.Add(key, bgw);
                                    bgw.RunWorkerAsync(key);
                                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "Threads with key " + key + " started", LoggingType.Information, LoggingFiles.WindowsService);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "Error in preparing thread for key: " + key, ex.Message, LoggingType.Error, LoggingFiles.WindowsService);
                            }
                        }
                    }
                    else
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "Error in preparing threads ..", LoggingType.Error, LoggingFiles.WindowsService);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
            finally
            {
                _wsManager.Dispose();
            }
        }
        private void StopWorkers()
        {
            try
            {
                foreach (BackgroundWorker worker in WorkersList.Values)
                {
                    worker.CancelAsync();
                    worker.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        protected override void OnStop()
        {
            try
            {
                StopWorkers();
                if (AppManager != null)
                    AppManager.Dispose();
                ApplicationManager.LogOut();
                this.Dispose();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
        }
    }
}
