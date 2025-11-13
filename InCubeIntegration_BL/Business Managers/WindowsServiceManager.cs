using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InCubeIntegration_DAL;
using InCubeLibrary;
using System.Data;
using System.ServiceProcess;

namespace InCubeIntegration_BL
{
    public class WindowsServiceManager : System.IDisposable
    {
        InCubeQuery incubeQuery;
        private InCubeDatabase db_WS;
        public bool ConnectionOpened
        {
            get { return db_WS.Opened; }
        }
        public WindowsServiceManager(bool OpenConnection)
        {
            if (OpenConnection)
            {
                db_WS = new InCubeDatabase();
                db_WS.Open("InCube", "WindowsServiceManager");
            }
        }
        public Result ReadFilters(ref DataTable dtFilters, ActionType _actionType, int OrganizationID, int TaskID)
        {
            Result res = Result.Failure;
            dtFilters = new DataTable();
            try
            {
                string keyFilter = "";
                if (CoreGeneral.Common.GeneralConfigurations.WS_Queues_Mode == Queues_Mode.TaskID)
                {
                    keyFilter = "AND T.TaskID = " + TaskID;
                }
                else
                {
                    keyFilter = string.Format("AND A.ActionType = {0} AND T.OrganizationID = {1}", _actionType.GetHashCode(), OrganizationID);
                }
                incubeQuery = new InCubeQuery(string.Format(@"SELECT AF.*
FROM Int_ActionFilter AF
INNER JOIN Int_Tasks T ON T.TaskID = AF.TaskID
INNER JOIN Int_TaskAction A ON A.TaskID = AF.TaskID AND A.ActionID = AF.ActionID
WHERE T.Status = {0} AND T.StartDate <= {1} AND T.EndDate >= {1} {2}", TaskStatus.Active.GetHashCode(), DatabaseDateTimeManager.ParseDateToSQLString(DateTime.Today), keyFilter), db_WS);

                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtFilters = incubeQuery.GetDataTable();
                    if (dtFilters.Rows.Count > 0)
                        res = Result.Success;
                    else
                        res = Result.NoRowsFound;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
            return res;
        }
        public Result ReadSchedules(ref DataTable dtSchedules, ActionType _actionType, int OrganizationID, int TaskID)
        {
            Result res = Result.Failure;
            dtSchedules = null;
            try
            {
                string keyFilter = "";
                if (CoreGeneral.Common.GeneralConfigurations.WS_Queues_Mode == Queues_Mode.TaskID)
                {
                    keyFilter = "AND T.TaskID = " + TaskID;
                }
                else
                {
                    keyFilter = string.Format("AND A.ActionType = {0} AND T.OrganizationID = {1}", _actionType.GetHashCode(), OrganizationID);
                }

                string scheduleQuery = string.Format(@"SELECT T.OrganizationID,A.ActionType,T.TaskID,T.Priority,S.ScheduleType,ISNULL(S.Time,0) Time,ISNULL(S.EndTime,0) EndTime,ISNULL(S.Period,0) Period,ISNULL(S.Day,0) Day,A.ActionID,A.FieldID,A.Sequence
FROM Int_Tasks T 
INNER JOIN Int_TaskSchedule S ON S.TaskID = T.TaskID
INNER JOIN Int_TaskAction A ON A.TaskID = T.TaskID
WHERE T.Status = {0} AND T.StartDate <= {1} AND T.EndDate >= {1} {2}", TaskStatus.Active.GetHashCode(), DatabaseDateTimeManager.ParseDateToSQLString(DateTime.Today), keyFilter);

                incubeQuery = new InCubeQuery(scheduleQuery, db_WS);
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtSchedules = incubeQuery.GetDataTable();
                    if (dtSchedules.Rows.Count > 0)
                        res = Result.Success;
                    else
                        res = Result.NoRowsFound;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
            return res;
        }
        public Result PrepareIntegrationThreads()
        {
            Result res = Result.Failure;
            DataTable dtThreads = null;
            try
            {
                CoreGeneral.Common.Queues = new Dictionary<string, Queue>();
                string select = "";
                if (CoreGeneral.Common.GeneralConfigurations.WS_Queues_Mode == Queues_Mode.TaskID)
                {
                    select = string.Format(@"SELECT T.TaskID
FROM Int_Tasks T 
WHERE T.Status = {0} AND T.StartDate <= {1} AND T.EndDate >= {1}"
                    , TaskStatus.Active.GetHashCode(), DatabaseDateTimeManager.ParseDateToSQLString(DateTime.Today));
                }
                else
                {
                    select = string.Format(@"SELECT DISTINCT {2} ActionType,T.OrganizationID
FROM Int_Tasks T 
INNER JOIN Int_TaskAction A ON A.TaskID = T.TaskID
WHERE T.Status = {0} AND T.StartDate <= {1} AND T.EndDate >= {1} AND A.ActionType = {2}
UNION ALL
SELECT DISTINCT {3},T.OrganizationID
FROM Int_Tasks T 
INNER JOIN Int_TaskAction A ON A.TaskID = T.TaskID
WHERE T.Status = {0} AND T.StartDate <= {1} AND T.EndDate >= {1} AND A.ActionType = {3}
UNION ALL
SELECT DISTINCT {4},T.OrganizationID
FROM Int_Tasks T 
INNER JOIN Int_TaskAction A ON A.TaskID = T.TaskID
WHERE T.Status = {0} AND T.StartDate <= {1} AND T.EndDate >= {1} AND A.ActionType = {4}"
                    , TaskStatus.Active.GetHashCode(), DatabaseDateTimeManager.ParseDateToSQLString(DateTime.Today), ActionType.Update.GetHashCode(), ActionType.Send.GetHashCode(), ActionType.SpecialFunctions.GetHashCode());
                }
                incubeQuery = new InCubeQuery(select, db_WS);

                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtThreads = incubeQuery.GetDataTable();
                    if (dtThreads.Rows.Count > 0)
                    {
                        DataTable dtSchedules;
                        DataTable dtFilters;
                        for (int i = 0; i < dtThreads.Rows.Count; i++)
                        {
                            dtSchedules = new DataTable();
                            dtFilters = new DataTable();
                            ActionType type = ActionType.Send;
                            int OrganizationID = 0;
                            int TaskID = 0;
                            if (CoreGeneral.Common.GeneralConfigurations.WS_Queues_Mode == Queues_Mode.TaskID)
                            {
                                TaskID = Convert.ToInt16(dtThreads.Rows[i]["TaskID"]);
                            }
                            else
                            {
                                type = (ActionType)dtThreads.Rows[i]["ActionType"];
                                OrganizationID = Convert.ToInt16(dtThreads.Rows[i]["OrganizationID"]);
                            }

                            if (ReadSchedules(ref dtSchedules, type, OrganizationID, TaskID) == Result.Success)
                            {
                                res = ReadFilters(ref dtFilters, type, OrganizationID, TaskID);
                                if (res == Result.Success || res == Result.NoRowsFound)
                                {
                                    string key = "";
                                    if (CoreGeneral.Common.GeneralConfigurations.WS_Queues_Mode == Queues_Mode.TaskID)
                                        key = TaskID.ToString();
                                    else
                                        key = type.GetHashCode().ToString() + ":" + OrganizationID.ToString();

                                    Queue Q = new Queue(key);
                                    if (FillQueue(dtSchedules, dtFilters, ref Q) == Result.Success)
                                    {
                                        CoreGeneral.Common.Queues.Add(key, Q);
                                    }
                                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, "Queue for key (" + key + ") count :" + Q.Schedules.Rows.Count, LoggingType.Information, LoggingFiles.WindowsService);
                                }
                            }
                        }
                        res = Result.Success;
                    }
                    else
                    {
                        res = Result.NoRowsFound;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
            return res;
        }
        private Result FillQueue(DataTable dtSchedules, DataTable dtFilters, ref Queue queue)
        {
            Result res = Result.Failure;
            try
            {
                int TaskID;
                int OrganizationID;
                int Priority;
                int ActionID;
                int actionType;
                int FieldID;
                int Sequence;
                DateTime _scheduledTime;
                int period;
                int _scheduleType;
                ScheduleType type;
                string Time;
                string EndTime;
                int day;

                DataTable dtQueue = new DataTable();
                dtQueue.Columns.Add("TaskID", typeof(int));
                dtQueue.Columns.Add("OrganizationID", typeof(int));
                dtQueue.Columns.Add("Priority", typeof(int));
                dtQueue.Columns.Add("ScheduledTime", typeof(DateTime));
                dtQueue.Columns.Add("ActionID", typeof(int));
                dtQueue.Columns.Add("ActionType", typeof(int));
                dtQueue.Columns.Add("FieldID", typeof(int));
                dtQueue.Columns.Add("Sequence", typeof(int));

                for (int i = 0; i < dtSchedules.Rows.Count; i++)
                {
                    TaskID = Convert.ToInt32(dtSchedules.Rows[i]["TaskID"]);
                    OrganizationID = Convert.ToInt32(dtSchedules.Rows[i]["OrganizationID"]);
                    Priority = Convert.ToInt32(dtSchedules.Rows[i]["Priority"]);
                    ActionID = Convert.ToInt32(dtSchedules.Rows[i]["ActionID"]);
                    actionType = Convert.ToInt32(dtSchedules.Rows[i]["ActionType"]);
                    FieldID = Convert.ToInt32(dtSchedules.Rows[i]["FieldID"]);
                    Sequence = Convert.ToInt32(dtSchedules.Rows[i]["Sequence"]);
                    period = Convert.ToInt32(dtSchedules.Rows[i]["Period"]);
                    day = Convert.ToInt32(dtSchedules.Rows[i]["Day"]);
                    _scheduleType = Convert.ToInt32(dtSchedules.Rows[i]["ScheduleType"]);
                    type = ((ScheduleType)_scheduleType);
                    Time = dtSchedules.Rows[i]["Time"].ToString();
                    EndTime = dtSchedules.Rows[i]["EndTime"].ToString();

                    switch (type)
                    {
                        case ScheduleType.DailyEvery:
                            DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, int.Parse(Time.Substring(0, 2)), int.Parse(Time.Substring(2, 2)), 0);
                            DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, int.Parse(EndTime.Substring(0, 2)), int.Parse(EndTime.Substring(2, 2)), 0);
                            endTime = endTime == DateTime.Today ? DateTime.Today.AddDays(1) : endTime;
                            _scheduledTime = startTime;// > DateTime.Now ? startTime : DateTime.Now.AddMinutes(1).AddSeconds(-DateTime.Now.Second);

                            while (_scheduledTime <= endTime)
                            {
                                if (_scheduledTime > DateTime.Now && _scheduledTime < DateTime.Today.AddDays(1))
                                {
                                    dtQueue.Rows.Add(dtQueue.NewRow());
                                    dtQueue.Rows[dtQueue.Rows.Count - 1]["TaskID"] = TaskID;
                                    dtQueue.Rows[dtQueue.Rows.Count - 1]["OrganizationID"] = OrganizationID;
                                    dtQueue.Rows[dtQueue.Rows.Count - 1]["Priority"] = Priority;
                                    dtQueue.Rows[dtQueue.Rows.Count - 1]["ActionID"] = ActionID;
                                    dtQueue.Rows[dtQueue.Rows.Count - 1]["ActionType"] = actionType;
                                    dtQueue.Rows[dtQueue.Rows.Count - 1]["FieldID"] = FieldID;
                                    dtQueue.Rows[dtQueue.Rows.Count - 1]["Sequence"] = Sequence;
                                    dtQueue.Rows[dtQueue.Rows.Count - 1]["ScheduledTime"] = _scheduledTime;
                                }
                                _scheduledTime = _scheduledTime.AddSeconds(period);
                            }
                            break;
                        case ScheduleType.DailyAt:
                        case ScheduleType.Weekly:
                        case ScheduleType.Monthly:
                            _scheduledTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, int.Parse(Time.Substring(0, 2)), int.Parse(Time.Substring(2, 2)), 0);
                            if ((_scheduledTime > DateTime.Now) && ((type == ScheduleType.DailyAt) || (type == ScheduleType.Weekly && day == DateTime.Now.DayOfWeek.GetHashCode()) || (type == ScheduleType.Monthly && day <= 28 && day == DateTime.Now.Day) || (type == ScheduleType.Monthly && day == 31 && DateTime.Now.Day == DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month))))
                            {
                                dtQueue.Rows.Add(dtQueue.NewRow());
                                dtQueue.Rows[dtQueue.Rows.Count - 1]["TaskID"] = TaskID;
                                dtQueue.Rows[dtQueue.Rows.Count - 1]["OrganizationID"] = OrganizationID;
                                dtQueue.Rows[dtQueue.Rows.Count - 1]["Priority"] = Priority;
                                dtQueue.Rows[dtQueue.Rows.Count - 1]["ActionID"] = ActionID;
                                dtQueue.Rows[dtQueue.Rows.Count - 1]["ActionType"] = actionType;
                                dtQueue.Rows[dtQueue.Rows.Count - 1]["FieldID"] = FieldID;
                                dtQueue.Rows[dtQueue.Rows.Count - 1]["Sequence"] = Sequence;
                                dtQueue.Rows[dtQueue.Rows.Count - 1]["ScheduledTime"] = _scheduledTime;
                            }
                            break;
                    }
                }
                dtQueue.DefaultView.Sort = "ScheduledTime ASC, Priority ASC, TaskID ASC, Sequence ASC";
                dtQueue = dtQueue.DefaultView.ToTable();
                if (dtQueue.Rows.Count > 0)
                {
                    queue.Schedules = dtQueue;
                    queue.Filters = dtFilters;
                    res = Result.Success;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
            return res;
        }
        public Result AddEditScheduledTask(bool EditMode, int RefTaskID, string Name, DateTime StartDate, DateTime EndDate, Priority priority, int OrganizationID, DataTable dtSchedules, DataTable dtActions, DataTable dtActionFilters)
        {
            Result res = Result.Failure;
            InCubeErrors err = InCubeErrors.Error;
            InCubeTransaction dbTrans = new InCubeTransaction();
            try
            {
                err = dbTrans.BeginTransaction(db_WS);
                if (err == InCubeErrors.Success)
                {
                    string insertTask = string.Format(@"INSERT INTO Int_Tasks (Name,StartDate,EndDate,Status,Priority,CreatedBy,CreationDate,OrganizationID,RefTaskID)
VALUES ('{0}','{1}','{2}',{3},{4},{5},GETDATE(),{6},{7});
SELECT SCOPE_IDENTITY();"
                    , Name, StartDate.ToString("yyyy-MM-dd"), EndDate.ToString("yyyy-MM-dd"), TaskStatus.Active.GetHashCode(), priority.GetHashCode()
                    , CoreGeneral.Common.CurrentSession.EmployeeID, OrganizationID, EditMode ? RefTaskID.ToString() : "null");
                    incubeQuery = new InCubeQuery(db_WS, insertTask);

                    object field = null;
                    err = incubeQuery.ExcuteScaler(dbTrans, ref field);
                    if (err == InCubeErrors.Success && field != null && field != DBNull.Value)
                    {
                        err = InCubeErrors.Error;
                        int TaskID = int.Parse(field.ToString());

                        for (int i = 0; i < dtSchedules.Rows.Count; i++)
                        {
                            ScheduleType scheduleType = (ScheduleType)Convert.ToInt16(dtSchedules.Rows[i]["ScheduleType"]);
                            string time = dtSchedules.Rows[i]["StartTime"].ToString();
                            string endtime = "";
                            int period = 0;
                            int day = -1;

                            if (scheduleType == ScheduleType.DailyEvery)
                            {
                                endtime = dtSchedules.Rows[i]["EndTime"].ToString();
                                period = int.Parse(dtSchedules.Rows[i]["Period"].ToString());
                            }
                            if (scheduleType == ScheduleType.Monthly || scheduleType == ScheduleType.Weekly)
                                day = int.Parse(dtSchedules.Rows[i]["Day"].ToString());

                            string insertSchedule = string.Format(@"INSERT INTO Int_TaskSchedule (TaskID,ScheduleType,Time,EndTime,Period,Day) 
VALUES ({0},{1},'{2}',{3},{4},{5})"
                                , TaskID, scheduleType.GetHashCode(), time, endtime == "" ? "NULL" : "'" + endtime + "'", period == 0 ? "NULL" : period.ToString(), day);
                            incubeQuery = new InCubeQuery(db_WS, insertSchedule);
                            err = incubeQuery.ExecuteNoneQuery(dbTrans);
                            if (err != InCubeErrors.Success)
                                break;
                        }

                        if (err == InCubeErrors.Success)
                        {
                            err = InCubeErrors.Error;
                            for (int i = 0; i < dtActions.Rows.Count; i++)
                            {
                                ActionType actionType = (ActionType)Convert.ToInt16(dtActions.Rows[i]["ActionType"]);
                                string fieldID = dtActions.Rows[i]["FieldID"].ToString();
                                string seq = dtActions.Rows[i]["Sequence"].ToString();

                                string insertAction = string.Format(@"INSERT INTO Int_TaskAction (TaskID,ActionType,FieldID,Sequence) 
VALUES ({0},{1},{2},{3});
SELECT SCOPE_IDENTITY();"
                                    , TaskID, actionType.GetHashCode(), fieldID, seq);
                                incubeQuery = new InCubeQuery(db_WS, insertAction);
                                err = incubeQuery.ExcuteScaler(dbTrans, ref field);
                                if (err != InCubeErrors.Success)
                                    break;
                                int ActionID = int.Parse(field.ToString());
                                dtActions.Rows[i]["ActionID"] = ActionID;
                            }
                        }

                        if (dtActionFilters.Rows.Count > 0 && err == InCubeErrors.Success)
                        {
                            err = InCubeErrors.Error;
                            for (int i = 0; i < dtActionFilters.Rows.Count; i++)
                            {
                                string ActionID = dtActions.Select("FieldID = " + dtActionFilters.Rows[i]["FieldID"].ToString())[0]["ActionID"].ToString();
                                string FilterID = dtActionFilters.Rows[i]["FilterID"].ToString();
                                string Value = dtActionFilters.Rows[i]["Value"].ToString();

                                string insertActionFilter = string.Format(@"INSERT INTO Int_ActionFilter (TaskID,ActionID,FilterID,Value)
VALUES ({0},{1},{2},'{3}')"
                                    , TaskID, ActionID, FilterID, Value);
                                incubeQuery = new InCubeQuery(db_WS, insertActionFilter);
                                err = incubeQuery.ExecuteNoneQuery(dbTrans);
                                if (err != InCubeErrors.Success)
                                    break;
                            }
                        }

                        if (EditMode && err == InCubeErrors.Success)
                        {
                            string DeactivateOldTask = string.Format(@"UPDATE Int_Tasks SET Status = {0} WHERE TaskID = {1}",
                                                                        TaskStatus.Changed.GetHashCode(), RefTaskID);
                            incubeQuery = new InCubeQuery(db_WS, DeactivateOldTask);
                            err = incubeQuery.ExecuteNoneQuery(dbTrans);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                err = InCubeErrors.Error;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
            finally
            {
                if (err == InCubeErrors.Success)
                {
                    dbTrans.Commit();
                    res = Result.Success;
                }
                else
                    dbTrans.Rollback();
            }
            return res;
        }
        public Result UpdateTaskStatus(int TaskID, TaskStatus status)
        {
            InCubeErrors err = InCubeErrors.NotInitialized;
            try
            {
                string ChangeTaskStatus = string.Format(@"UPDATE Int_Tasks SET Status = {0} WHERE TaskID = {1}", status.GetHashCode(), TaskID);
                incubeQuery = new InCubeQuery(db_WS, ChangeTaskStatus);
                err = incubeQuery.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                err = InCubeErrors.Error;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
            return (err == InCubeErrors.Success ? Result.Success : Result.Failure);
        }
        public Result GetAllTasks(ref DataTable dtTasks, ref DataTable dtActions, ref DataTable dtSchedules, ref DataTable dtActionsFilters)
        {
            Result res = Result.Failure;
            try
            {
                string qry = string.Format(@"SELECT TaskID,Name,StartDate [From], EndDate [To],CONVERT(nvarchar(20),StartDate,103) StartDate,CONVERT(nvarchar(20),EndDate,103) EndDate
,CASE WHEN EndDate < GETDATE() THEN 'Expired' ELSE (CASE Status WHEN 1 THEN 'Active' ELSE 'Stopped' END) END Status
,CASE Priority WHEN {0} THEN 'High' WHEN {1} THEN 'Medium' ELSE 'Low' END Priority, OL.Description Organization
,Status StatusID, Priority PriorityID, T.OrganizationID
FROM Int_Tasks T
INNER JOIN OrganizationLanguage OL ON OL.OrganizationID = T.OrganizationID AND OL.LanguageID = 1
WHERE Status NOT IN ({2},{4}) AND T.OrganizationID IN ({3})", Priority.High.GetHashCode(), Priority.Medium.GetHashCode(), TaskStatus.Deleted.GetHashCode(), CoreGeneral.Common.userPrivileges.Organizations, TaskStatus.Changed.GetHashCode());

                incubeQuery = new InCubeQuery(db_WS, qry);
                dtTasks = new DataTable();
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtTasks = incubeQuery.GetDataTable();

                    qry = string.Format(@"SELECT TA.TaskID,TA.ActionID,TA.ActionType,TA.FieldID,TA.Sequence, CASE TA.ActionType WHEN 1 THEN 'Update ' WHEN 2 THEN 'Send ' ELSE '' END + F.FieldName [Action]
FROM Int_TaskAction TA
INNER JOIN Int_Tasks T ON T.TaskID = TA.TaskID
INNER JOIN Int_Field F ON F.FieldID = TA.FieldID AND F.ActionType = TA.ActionType
WHERE T.Status NOT IN ({0},{2}) AND T.OrganizationID IN ({1})
ORDER BY TA.TaskID,TA.Sequence", TaskStatus.Deleted.GetHashCode(), CoreGeneral.Common.userPrivileges.Organizations, TaskStatus.Changed.GetHashCode());
                    incubeQuery = new InCubeQuery(db_WS, qry);
                    dtActions = new DataTable();
                    if (incubeQuery.Execute() == InCubeErrors.Success)
                    {
                        dtActions = incubeQuery.GetDataTable();

                        qry = string.Format(@"SELECT TS.TaskID,TS.ScheduleType,ISNULL(TS.Time,'0000') StartTime,ISNULL(TS.EndTime,'0000') EndTime,ISNULL(TS.Period,0) Period,ISNULL(TS.Day,0) [Day],'' Schedule
FROM Int_TaskSchedule TS
INNER JOIN Int_Tasks T ON T.TaskID = TS.TaskID
WHERE T.Status NOT IN ({0},{2}) AND T.OrganizationID IN ({1})
ORDER BY TS.TaskID,TS.ScheduleType", TaskStatus.Deleted.GetHashCode(), CoreGeneral.Common.userPrivileges.Organizations, TaskStatus.Changed.GetHashCode());
                        incubeQuery = new InCubeQuery(db_WS, qry);
                        dtSchedules = new DataTable();
                        if (incubeQuery.Execute() == InCubeErrors.Success)
                        {
                            dtSchedules = incubeQuery.GetDataTable();

                            qry = string.Format(@"SELECT AF.TaskID, TA.FieldID, AF.FilterID, AF.Value
FROM Int_ActionFilter AF
INNER JOIN Int_Tasks T ON T.TaskID = AF.TaskID
INNER JOIN Int_TaskAction TA ON TA.ActionID = AF.ActionID
WHERE T.Status NOT IN ({0},{2}) AND T.OrganizationID IN ({1})
ORDER BY AF.TaskID,TA.FieldID,AF.FilterID", TaskStatus.Deleted.GetHashCode(), CoreGeneral.Common.userPrivileges.Organizations, TaskStatus.Changed.GetHashCode());
                            incubeQuery = new InCubeQuery(db_WS, qry);
                            dtActionsFilters = new DataTable();
                            if (incubeQuery.Execute() == InCubeErrors.Success)
                            {
                                dtActionsFilters = incubeQuery.GetDataTable();
                                res = Result.Success;
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
        public Result InstallService(bool Install)
        {
            Result res = Result.UnKnown;
            try
            {
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = string.Format(@"WindowsService{0}Installer.bat", Install ? "" : "Un");
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.Verb = "runas";
                proc.Start();
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result GetServiceStatus(ref string Name, ref ServiceStatus Status)
        {
            Result res = Result.UnKnown;
            Status = ServiceStatus.UnKnown;
            Name = "";
            try
            {
                ServiceController sc = new ServiceController();
                sc.ServiceName = "InCube Integration Service " + CoreGeneral.Common.GeneralConfigurations.AppVersion;
                try
                {
                    switch (sc.Status)
                    {
                        case ServiceControllerStatus.Running:
                            Status = ServiceStatus.Running;
                            break;
                        case ServiceControllerStatus.Stopped:
                            Status = ServiceStatus.Stopped;
                            break;
                    }
                    Name = sc.ServiceName;
                    res = Result.Success;
                }
                catch
                {
                    Status = ServiceStatus.NotInstalled;
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public void Dispose()
        {
            try
            {
                if (incubeQuery != null)
                    incubeQuery.Close();

                if (db_WS != null)
                    db_WS.Dispose();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
    }
}
