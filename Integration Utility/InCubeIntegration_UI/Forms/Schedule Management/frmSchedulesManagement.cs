using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using InCubeIntegration_BL;
using InCubeLibrary;

namespace InCubeIntegration_UI
{
    public partial class frmSchedulesManagement : Form
    {
        DataTable dtTasks;
        DataTable dtSchedules;
        DataTable dtActions;
        DataTable dtActionsFilters;
        DataTable dtFieldFilters = new DataTable();

        DataTable dtViewedActions;
        DataTable dtViewedActionsFilters;
        DataTable dtViewedSchedules;
        bool _isEditing = false;
        bool _isAdding = false;
        int selectedTaskID;

        public frmSchedulesManagement()
        {
            try
            {
                InitializeComponent();
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                //wsManager = new WindowsServiceManager();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void frmSchedules_Load(object sender, EventArgs e)
        {
            try
            {
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;

                MessageBox.Show("Any changes applied in schedules management screen will be effictive after restarting integration windows service or automatically after the begining of a new day.", "Note");
                DataTable dtOrganizations = new DataTable();
                if (CoreGeneral.Common.GeneralConfigurations.OrganizationOriented)
                {
                    using (OrganizationManager OrgManager = new OrganizationManager())
                    {
                        if (OrgManager.GetAllOrganizations(ref dtOrganizations) == Result.Success)
                        {
                            cmbOrganization.DataSource = dtOrganizations;
                            cmbOrganization.DisplayMember = "Description";
                            cmbOrganization.ValueMember = "OrganizationID";
                        }
                    }
                }
                else
                {
                    Organization.Visible = false;
                    cmbOrganization.Visible = false;
                    lblOrganization.Visible = false;
                }

                Dictionary<int, string> priorities = new Dictionary<int, string>();
                priorities.Add(Priority.Low.GetHashCode(), Priority.Low.ToString());
                priorities.Add(Priority.Medium.GetHashCode(), Priority.Medium.ToString());
                priorities.Add(Priority.High.GetHashCode(), Priority.High.ToString());
                cmbPriority.DataSource = new BindingSource(priorities, null);
                cmbPriority.DisplayMember = "Value";
                cmbPriority.ValueMember = "Key";

                using (ExecutionManager execManager = new ExecutionManager())
                {
                    execManager.GetAllFieldsFilters(ref dtFieldFilters);
                }

                string ServiceName = "";
                ServiceStatus Status = ServiceStatus.UnKnown;
                using (WindowsServiceManager wsm = new WindowsServiceManager(false))
                {
                    if (wsm.GetServiceStatus(ref ServiceName, ref Status) == Result.Success)
                    {
                        if (Status == ServiceStatus.NotInstalled)
                        {
                            lblServiceStatus.Text = "  : Not installed";
                            lblServiceName.Text = "  : Not installed";
                            lblServiceMachine.Text = "  :";
                            //btnInstall.Enabled = true;
                        }
                        else
                        {
                            lblServiceStatus.Text = "  : " + Status.ToString();
                            lblServiceName.Text = "  : " + ServiceName;
                            lblServiceMachine.Text = "  : " + CoreGeneral.Common.GeneralConfigurations.WS_Machine_Name;
                            //btnInstall.Text = "Uninstall";
                            //if (CoreGeneral.Common.GeneralConfigurations.WS_Machine_Name == Environment.MachineName)
                            //    btnInstall.Enabled = true;
                        }
                    }
                }
                FillTasks();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void FillTasks()
        {
            WindowsServiceManager wsManager = new WindowsServiceManager(true);
            try
            {
                dtTasks = new DataTable();
                dtSchedules = new DataTable();
                dtActions = new DataTable();
                dtActionsFilters = new DataTable();

                if (wsManager.GetAllTasks(ref dtTasks, ref dtActions, ref dtSchedules, ref dtActionsFilters) == Result.Success)
                {
                    dtViewedActions = dtActions.Clone();
                    dtViewedActions.Rows.Clear();
                    dtViewedActions.DefaultView.Sort = "Sequence ASC";
                    dgvActions.DataSource = dtViewedActions.DefaultView;

                    dtViewedActionsFilters = dtActionsFilters.Clone();
                    dtViewedActionsFilters.Rows.Clear();

                    dtViewedSchedules = dtSchedules.Clone();
                    dtViewedSchedules.Rows.Clear();
                    dgvSchedules.DataSource = dtViewedSchedules.DefaultView;

                    dgvTasks.DataSource = dtTasks;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
            finally
            {
                wsManager.Dispose();
            }
        }

        private void dgvTasks_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (dgvTasks.SelectedCells.Count > 0)
                {
                    dgvTasks.Rows[dgvTasks.SelectedCells[0].RowIndex].Selected = true;
                    FillSelectedTaskDetails(int.Parse(dgvTasks.SelectedRows[0].Cells["TaskID"].Value.ToString()));
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void FillSelectedTaskDetails(int TaskID)
        {
            try
            {
                selectedTaskID = TaskID;
                DataRow[] drTask = dtTasks.Select("TaskID = " + TaskID);
                txtName.Text = drTask[0]["Name"].ToString();
                dtpFromDate.Value = Convert.ToDateTime(drTask[0]["From"]);
                dtpToDate.Value = Convert.ToDateTime(drTask[0]["To"]);
                if (CoreGeneral.Common.GeneralConfigurations.OrganizationOriented)
                {
                    cmbOrganization.SelectedValue = drTask[0]["OrganizationID"];
                }
                cmbPriority.SelectedValue = drTask[0]["PriorityID"];
                dtActions.DefaultView.RowFilter = "TaskID = " + TaskID;
                dtViewedActions = dtActions.DefaultView.ToTable();
                dtViewedActions.DefaultView.Sort = "Sequence ASC";
                dgvActions.DataSource = dtViewedActions.DefaultView;

                dtActionsFilters.DefaultView.RowFilter = "TaskID = " + TaskID;
                dtViewedActionsFilters = dtActionsFilters.DefaultView.ToTable();

                dtViewedSchedules.Rows.Clear();
                DataRow[] drSchedules = dtSchedules.Select("TaskID = " + TaskID);
                foreach (DataRow dr in drSchedules)
                {
                    AddScheduleToList(int.Parse(dr["ScheduleType"].ToString()), dr["StartTime"].ToString(), dr["EndTime"].ToString(), int.Parse(dr["Period"].ToString()), int.Parse(dr["Day"].ToString()));
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private string AddScheduleToList(int ScheduleTypeID, string StartTime, string EndTime, int Period, int Day)
        {
            string res = "";
            try
            {
                string day = "", start = "", end = "";
                start = StartTime.Substring(0, 2) + ":" + StartTime.Substring(2, 2);
                end = EndTime.Substring(0, 2) + ":" + EndTime.Substring(2, 2);
                int periodMin = Period / 60;
                if ((ScheduleType)ScheduleTypeID == ScheduleType.Weekly)
                {
                    day = ((DayOfWeek)Day).ToString();
                }
                else if ((ScheduleType)ScheduleTypeID == ScheduleType.Monthly)
                {
                    if (Day == 31)
                        day = "last day in the month";
                    else
                        day = "day " + Day.ToString();
                }
                string desc = "";
                switch ((ScheduleType)ScheduleTypeID)
                {
                    case ScheduleType.DailyAt:
                        desc = "Daily at " + start;
                        break;
                    case ScheduleType.Monthly:
                        desc = "Monthly on " + day + " at " + start;
                        break;
                    case ScheduleType.DailyEvery:
                        desc = "Daily every " + periodMin + " minute(s) from " + start + " to " + end;
                        break;
                    case ScheduleType.Weekly:
                        desc = "Weekly on " + day + " at " + start;
                        break;
                }

                DataRow[] drSchedules = dtViewedSchedules.Select("Schedule = '" + desc + "'");
                if (drSchedules.Length > 0)
                {
                    res = "Already added!!";
                }
                else
                {
                    if ((ScheduleType)ScheduleTypeID == ScheduleType.DailyEvery)
                    {
                        TimeSpan ts1 = new TimeSpan(int.Parse(StartTime.Substring(0, 2)), int.Parse(StartTime.Substring(2, 2)), 0);
                        TimeSpan ts2 = new TimeSpan(int.Parse(EndTime.Substring(0, 2)), int.Parse(EndTime.Substring(2, 2)), 0);
                        if (ts2 <= ts1)
                            return "End time should be greater than start time!!";
                        if (ts2.Subtract(ts1).TotalSeconds < Period)
                            return "Entered period less than selected interval!!";

                        DataRow[] drDaily = dtViewedSchedules.Select("ScheduleType = " + ScheduleTypeID);
                        foreach (DataRow dr in drDaily)
                        {
                            TimeSpan ts3 = new TimeSpan(int.Parse(dr["StartTime"].ToString().Substring(0, 2)), int.Parse(dr["StartTime"].ToString().Substring(2, 2)), 0);
                            TimeSpan ts4 = new TimeSpan(int.Parse(dr["EndTime"].ToString().Substring(0, 2)), int.Parse(dr["EndTime"].ToString().Substring(2, 2)), 0);
                            if ((ts1 >= ts3 && ts1 < ts4) || (ts2 > ts3 && ts2 <= ts4))
                                return "Schedule overlaps with existing:\r\n" + dr["Schedule"].ToString();
                        }
                    }
                    dtViewedSchedules.Rows.Add(new object[] { -1, ScheduleTypeID, StartTime, EndTime, Period, Day, desc });
                    res = "Added ..";
                }
                //dgvSchedules.Rows.Insert(0, new object[] { desc, ScheduleTypeID, StartTime, EndTime, Period, Day });
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
                res = "Failure !!";
            }
            return res;
        }
        private void EditTask(object sender, EventArgs e)
        {
            try
            {
                _isEditing = true;
                dgvTasks.ReadOnly = true;
                dgvTasks.Enabled = false;
                txtName.Focus();
                txtName.ReadOnly = false;
                dtpFromDate.Enabled = true;
                dtpToDate.Enabled = true;
                dtpToDate.MinDate = dtpFromDate.Value;
                cmbPriority.Enabled = true;
                if (CoreGeneral.Common.GeneralConfigurations.OrganizationOriented)
                {
                    cmbOrganization.Enabled = true;
                }
                btnAddAction.Enabled = true;
                btnAddSchedule.Enabled = true;
                btnCancel.Enabled = true;
                btnAddTask.Enabled = false;
                btnSaveChanges.Enabled = true;
                picUp.Enabled = true;
                picDown.Enabled = true;
                btnSaveChanges.BackColor = Color.LightGreen;
                btnCancel.BackColor = Color.LightCoral;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void StopTask(object sender, EventArgs e)
        {
            WindowsServiceManager wsManager = new WindowsServiceManager(true);
            try
            {
                switch (wsManager.UpdateTaskStatus(selectedTaskID, TaskStatus.Stopped))
                {
                    case Result.Success:
                        MessageBox.Show("Task stopped ..");
                        FillTasks();
                        break;
                    default:
                        MessageBox.Show("Failed to stop task !!");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
            finally
            {
                wsManager.Dispose();
            }
        }
        private void DeleteTask(object sender, EventArgs e)
        {
            WindowsServiceManager wsManager = new WindowsServiceManager(true);
            try
            {
                switch (wsManager.UpdateTaskStatus(selectedTaskID,TaskStatus.Deleted))
                {
                    case Result.Success:
                        MessageBox.Show("Task deleted ..");
                        FillTasks();
                        break;
                    default:
                        MessageBox.Show("Failed to delete task !!");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
            finally
            {
                wsManager.Dispose();
            }
        }
        private void StartTask(object sender, EventArgs e)
        {
            WindowsServiceManager wsManager = new WindowsServiceManager(true);
            try
            {
                switch (wsManager.UpdateTaskStatus(selectedTaskID, TaskStatus.Active))
                {
                    case Result.Success:
                        MessageBox.Show("Task activated ..");
                        FillTasks();
                        break;
                    default:
                        MessageBox.Show("Failed to start task !!");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
            finally
            {
                wsManager.Dispose();
            }
        }
        private void RemoveAction(object sender, EventArgs e)
        {
            try
            {
                int selectedFieldID = int.Parse(dgvActions.SelectedRows[0].Cells["FieldID"].Value.ToString());
                int selectedSeq = int.Parse(dgvActions.SelectedRows[0].Cells["Sequence"].Value.ToString());
                dtViewedActions.Rows.Remove(dtViewedActions.Select("FieldID = " + selectedFieldID)[0]);
                foreach(DataRow drActionFilter in dtViewedActionsFilters.Select("FieldID = " + selectedFieldID))
                    dtViewedActionsFilters.Rows.Remove(drActionFilter);
                foreach (DataRow dr in dtViewedActions.Select("Sequence > " + selectedSeq))
                {
                    dr["Sequence"] = int.Parse(dr["Sequence"].ToString()) - 1;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void CollectActionFilters(int selectedFieldID)
        {
            try
            {
                Dictionary<int, string> filterValues = new Dictionary<int, string>();
                foreach (DataRow dr in dtViewedActionsFilters.Select("FieldID = " + selectedFieldID))
                    filterValues.Add(int.Parse(dr["FilterID"].ToString()), dr["Value"].ToString());

                frmFieldFilters frm = new frmFieldFilters(FormMode.Add, (IntegrationField)selectedFieldID, ref filterValues);
                frm.ShowDialog();

                foreach (KeyValuePair<int, string> filter in filterValues)
                {
                    DataRow dr;
                    if (dtViewedActionsFilters.Select(string.Format("FieldID = {0} AND FilterID = {1}", selectedFieldID, filter.Key)).Length > 0)
                    {
                        dr = dtViewedActionsFilters.Select(string.Format("FieldID = {0} AND FilterID = {1}", selectedFieldID, filter.Key))[0];
                        dr["Value"] = filter.Value;
                    }
                    else
                    {
                        dr = dtViewedActionsFilters.NewRow();
                        dr["TaskID"] = "-1";
                        dr["FieldID"] = selectedFieldID;
                        dr["FilterID"] = filter.Key;
                        dr["Value"] = filter.Value;
                        dtViewedActionsFilters.Rows.Add(dr);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void ActionFilters(object sender, EventArgs e)
        {
            try
            {
                int selectedFieldID = int.Parse(dgvActions.SelectedRows[0].Cells["FieldID"].Value.ToString());
                CollectActionFilters(selectedFieldID);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void RemoveSchedule(object sender, EventArgs e)
        {
            try
            {
                string selectedSchedule = dgvSchedules.SelectedRows[0].Cells["Schedule"].Value.ToString();
                dtViewedSchedules.Rows.Remove(dtViewedSchedules.Select("Schedule = '" + selectedSchedule + "'")[0]);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void dgvTasks_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                int selectedRow = dgvTasks.HitTest(e.X, e.Y).RowIndex;
                if (e.Button == System.Windows.Forms.MouseButtons.Right && selectedRow >= 0)
                {
                    dgvTasks.Rows[selectedRow].Selected = true;
                    ContextMenu m = new System.Windows.Forms.ContextMenu();
                    m.MenuItems.Add("Edit", EditTask);
                    if (dgvTasks.Rows[selectedRow].Cells["StatusID"].Value.ToString() == TaskStatus.Active.GetHashCode().ToString())
                        m.MenuItems.Add("Stop", StopTask);
                    else
                        m.MenuItems.Add("Start", StartTask);
                    m.MenuItems.Add("Delete", DeleteTask);
                    m.Show(dgvTasks, new Point(e.X, e.Y));
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void dgvActions_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (!_isAdding && !_isEditing)
                    return;
                int selectedRow = dgvActions.HitTest(e.X, e.Y).RowIndex;
                if (e.Button == System.Windows.Forms.MouseButtons.Right && selectedRow >= 0)
                {
                    dgvActions.Rows[selectedRow].Selected = true;
                    ContextMenu m = new System.Windows.Forms.ContextMenu();
                    m.MenuItems.Add("Remove", RemoveAction);
                    int selectedFieldID = int.Parse(dgvActions.SelectedRows[0].Cells["FieldID"].Value.ToString());
                    if (dtFieldFilters.Select("FieldID = " + selectedFieldID).Length > 0)
                        m.MenuItems.Add("Filters", ActionFilters);
                    m.Show(dgvActions, new Point(e.X, e.Y));
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void dgvSchedules_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (!_isAdding && !_isEditing)
                    return;
                int selectedRow = dgvSchedules.HitTest(e.X, e.Y).RowIndex;
                if (e.Button == System.Windows.Forms.MouseButtons.Right && selectedRow >= 0)
                {
                    foreach (DataGridViewRow r in dgvSchedules.Rows)
                        r.Selected = false;
                    dgvSchedules.Rows[selectedRow].Selected = true;
                    ContextMenu m = new System.Windows.Forms.ContextMenu();
                    m.MenuItems.Add("Remove", RemoveSchedule);
                    m.Show(dgvSchedules, new Point(e.X, e.Y));
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnAddSchedule_Click(object sender, EventArgs e)
        {
            try
            {
                frmAddSchedule frm = new frmAddSchedule();
                frm.AddScheduleHandler += new frmAddSchedule.AddScheduleDel(AddScheduleToList);
                frm.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnAddAction_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    ContextMenu m = new System.Windows.Forms.ContextMenu();
                    if (CoreGeneral.Common.userPrivileges.UpdateFieldsAccess.Count > 0)
                    {
                        MenuItem mu = new MenuItem("Update");
                        mu.Tag = ActionType.Update.GetHashCode();
                        foreach (FieldItem field in CoreGeneral.Common.userPrivileges.UpdateFieldsAccess.Values)
                        {
                            if (dtViewedActions.Rows.Count == 0 || dtViewedActions.Select("FieldID = " + field.FieldID).Length == 0)
                            {
                                MenuItem i = new MenuItem(field.Description, AddAction);
                                i.Tag = field.FieldID;
                                mu.MenuItems.Add(i);
                            }
                        }
                        if (mu.MenuItems.Count > 0)
                            m.MenuItems.Add(mu);
                    }

                    if (CoreGeneral.Common.userPrivileges.SendFieldsAccess.Count > 0)
                    {
                        MenuItem ms = new MenuItem("Send");
                        ms.Tag = ActionType.Send.GetHashCode();
                        foreach (FieldItem field in CoreGeneral.Common.userPrivileges.SendFieldsAccess.Values)
                        {
                            if (dtViewedActions.Rows.Count == 0 || dtViewedActions.Select("FieldID = " + field.FieldID).Length == 0)
                            {
                                MenuItem i = new MenuItem(field.Description, AddAction);
                                i.Tag = field.FieldID;
                                ms.MenuItems.Add(i);
                            }
                        }
                        if (ms.MenuItems.Count > 0)
                            m.MenuItems.Add(ms);
                    }

                    if (CoreGeneral.Common.userPrivileges.SpecialFunctionsAccess.Count > 0)
                    {
                        MenuItem ms = new MenuItem("Special Functions");
                        ms.Tag = ActionType.SpecialFunctions.GetHashCode();
                        foreach (FieldItem field in CoreGeneral.Common.userPrivileges.SpecialFunctionsAccess.Values)
                        {
                            if (dtViewedActions.Rows.Count == 0 || dtViewedActions.Select("FieldID = " + field.FieldID).Length == 0)
                            {
                                MenuItem i = new MenuItem(field.Description, AddAction);
                                i.Tag = field.FieldID;
                                ms.MenuItems.Add(i);
                            }
                        }
                        if (ms.MenuItems.Count > 0)
                            m.MenuItems.Add(ms);
                    }

                    m.Show(btnAddAction, new Point(e.X, e.Y));
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void AddAction(object sender, EventArgs e)
        {
            try
            {
                MenuItem mi = (MenuItem)sender;
                ActionType type = (ActionType)int.Parse(mi.Parent.Tag.ToString());
                string Action = "";
                if (type != ActionType.SpecialFunctions)
                    Action = type.ToString() + " ";
                Action += mi.Text;
                int Sequence = dtViewedActions.Rows.Count + 1;
                dtViewedActions.Rows.Add(new object[] { "-1", "-1", mi.Parent.Tag, mi.Tag, Sequence, Action });
                if (dtFieldFilters.Select("FieldID = " + mi.Tag).Length > 0)
                    CollectActionFilters(Convert.ToInt16(mi.Tag));
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnAddTask_Click(object sender, EventArgs e)
        {
            try
            {
                _isAdding = true;
                dgvTasks.ReadOnly = true;
                dgvTasks.Enabled = false;

                txtName.Clear();
                txtName.Focus();
                txtName.ReadOnly = false;
                dtpFromDate.Enabled = true;
                dtpFromDate.MinDate = DateTime.Today;
                dtpFromDate.Value = DateTime.Today;
                dtpToDate.Enabled = true;
                dtpToDate.MinDate = DateTime.Today;
                dtpToDate.Value = new DateTime(DateTime.Now.Year, 12, 31);
                cmbPriority.Enabled = true;
                cmbPriority.SelectedValue = Priority.Medium.GetHashCode();
                if (CoreGeneral.Common.GeneralConfigurations.OrganizationOriented)
                {
                    cmbOrganization.Enabled = true;
                    cmbOrganization.SelectedIndex = 0;
                }
                btnAddAction.Enabled = true;
                dtViewedActions.Rows.Clear();
                dtViewedActionsFilters.Rows.Clear();
                btnAddSchedule.Enabled = true;
                dtViewedSchedules.Rows.Clear();
                btnCancel.Enabled = true;
                btnAddTask.Enabled = false;
                btnSaveChanges.Enabled = true;
                picUp.Enabled = true;
                picDown.Enabled = true;
                btnSaveChanges.BackColor = Color.LightGreen;
                btnCancel.BackColor = Color.LightCoral;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void picUp_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvActions.SelectedRows.Count == 1)
                {
                    int selectedIndex = dgvActions.SelectedRows[0].Index;
                    int currentSeq = int.Parse(dgvActions.SelectedRows[0].Cells["Sequence"].Value.ToString());
                    if (currentSeq > 1)
                    {
                        int CurrtblRowIndx = dtViewedActions.Rows.IndexOf(dtViewedActions.Select("Sequence = " + currentSeq)[0]);
                        int PreTblRowIndx = dtViewedActions.Rows.IndexOf(dtViewedActions.Select("Sequence = " + (currentSeq - 1))[0]);
                        dtViewedActions.Rows[CurrtblRowIndx]["Sequence"] = currentSeq - 1;
                        dtViewedActions.Rows[PreTblRowIndx]["Sequence"] = currentSeq;
                        dgvActions.SelectedRows[0].Selected = false;
                        dgvActions.Rows[selectedIndex - 1].Selected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void picDown_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvActions.SelectedRows.Count == 1)
                {
                    int selectedIndex = dgvActions.SelectedRows[0].Index;
                    int currentSeq = int.Parse(dgvActions.SelectedRows[0].Cells["Sequence"].Value.ToString());
                    if (currentSeq < dgvActions.Rows.Count)
                    {
                        int CurrtblRowIndx = dtViewedActions.Rows.IndexOf(dtViewedActions.Select("Sequence = " + currentSeq)[0]);
                        int NxtTblRowIndx = dtViewedActions.Rows.IndexOf(dtViewedActions.Select("Sequence = " + (currentSeq + 1))[0]);
                        dtViewedActions.Rows[CurrtblRowIndx]["Sequence"] = currentSeq + 1;
                        dtViewedActions.Rows[NxtTblRowIndx]["Sequence"] = currentSeq;
                        dgvActions.SelectedRows[0].Selected = false;
                        dgvActions.Rows[selectedIndex + 1].Selected = true;
                        dgvActions.FirstDisplayedScrollingRowIndex = selectedIndex + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void dgvActions_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (dgvActions.SelectedCells.Count > 0)
                    dgvActions.Rows[dgvActions.SelectedCells[0].RowIndex].Selected = true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            WindowsServiceManager wsManager = null;
            try
            {
                if (txtName.Text.Trim().Equals(string.Empty))
                {
                    MessageBox.Show("Please fill task name");
                    return;
                }
                if (dtViewedActions.Rows.Count == 0)
                {
                    MessageBox.Show("Please fill task actions");
                    return;
                }
                if (dtViewedSchedules.Rows.Count == 0)
                {
                    MessageBox.Show("Please fill task schedules");
                    return;
                }
                wsManager = new WindowsServiceManager(true);
                switch (wsManager.AddEditScheduledTask(_isEditing, selectedTaskID, txtName.Text, dtpFromDate.Value, dtpToDate.Value, (Priority)Convert.ToInt16(cmbPriority.SelectedValue), CoreGeneral.Common.GeneralConfigurations.OrganizationOriented ? Convert.ToInt16(cmbOrganization.SelectedValue) : 1, dtViewedSchedules, dtViewedActions.DefaultView.ToTable(), dtViewedActionsFilters))
                {
                    case Result.Success:
                        MessageBox.Show("Saved Successfully ..");
                        EndChanges();
                        FillTasks();
                        break;
                    default:
                        MessageBox.Show("Failed to save task !!");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
            finally
            {
                _isEditing = false;
                _isAdding = false;
                if (wsManager != null)
                    wsManager.Dispose();
            }
        }

        private void EndChanges()
        {
            try
            {
                dgvTasks.Enabled = true;
                dgvTasks.Focus();
                txtName.ReadOnly = true;
                txtName.Clear();
                dtpFromDate.Enabled = false;
                dtpToDate.Enabled = false;
                cmbPriority.Enabled = false;
                if (CoreGeneral.Common.GeneralConfigurations.OrganizationOriented)
                {
                    cmbOrganization.Enabled = false;
                }
                btnAddAction.Enabled = false;
                btnAddSchedule.Enabled = false;
                btnCancel.Enabled = false;
                btnAddTask.Enabled = true;
                btnSaveChanges.Enabled = false;
                dtpFromDate.MinDate = new DateTime(1990, 1, 1);
                dtpToDate.MinDate = new DateTime(1990, 1, 1);
                dgvActions.ReadOnly = true;
                dgvSchedules.ReadOnly = true;
                btnSaveChanges.BackColor = Color.Gainsboro;
                btnCancel.BackColor = Color.Gainsboro;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            try
            {
                EndChanges();
                if (dgvTasks.SelectedCells.Count > 0)
                {
                    dgvTasks.Rows[dgvTasks.SelectedCells[0].RowIndex].Selected = true;
                    FillSelectedTaskDetails(int.Parse(dgvTasks.SelectedRows[0].Cells["TaskID"].Value.ToString()));
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
            finally
            {
                _isEditing = false;
                _isAdding = false;
            }
        }

        private void dtpFromDate_ValueChanged(object sender, EventArgs e)
        {
            dtpToDate.MinDate = dtpFromDate.Value.Date;
        }
    }
}
