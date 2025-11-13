using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using InCubeLibrary;
using System.ComponentModel;
using InCubeIntegration_BL;
using System.Collections.Generic;

namespace InCubeIntegration_UI
{
    public partial class frmIntegration : Form
    {
        #region Member Data

        BackgroundWorker bgwUpdate;
        IntegrationFilters UpdateFilters;
        List<IntegrationField> UpdateFields;
        BackgroundWorker bgwSend;
        IntegrationFilters SendFilters;
        List<IntegrationField> SendFields;
        BackgroundWorker bgwSpecialActions;
        IntegrationFilters SpecialActionFilters;
        List<IntegrationField> SpecialActionFields;

        public int OrganizationID = -1;
        bool _isLoading = false;
        #endregion

        #region Constructor
        public frmIntegration()
        {
            InitializeComponent();
            this.Width = lblFormWidth.Width;
        }
        #endregion

        #region Event Handlers

        private void frmIntegration_Load(object sender, EventArgs e)
        {
            try
            {
                _isLoading = true;

                lblVersion.Text = "Ver. " + CoreGeneral.Common.GeneralConfigurations.AppVersion;
                AddItemsTolist();
                ApplyConditionalFormatting();
                FillEmployeesCombos();
                FillWarehousesCombo();
                _isLoading = false;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                _isLoading = false;
            }
        }
        private void btnSendToERP_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnSendToERP.Text == "Start Send")
                {
                    btnSendToERP.Text = "Send in progress";
                    btnSendToERP.ForeColor = Color.Red;
                    pnlSendEmployeeFilter.Enabled = false;
                    gbSendItems.Enabled = false;
                    gbFilterSend.Enabled = false;
                    Application.DoEvents();

                    SendFilters = new IntegrationFilters(ActionType.Send);
                    int EmployeeID = -1;
                    if (!cbSendAllEmployees.Checked)
                        EmployeeID = int.Parse(cmbSendEmployee.SelectedValue.ToString());

                    SendFilters.SetValue(BuiltInFilters.Employee, EmployeeID);
                    SendFilters.SetValue(BuiltInFilters.Organization, OrganizationID);
                    SendFilters.SetValue(BuiltInFilters.FromDate, dtpSendFromDate.Value.Date);
                    SendFilters.SetValue(BuiltInFilters.ToDate, dtpSendToDate.Value.Date);
                    
                    SendFields = new List<IntegrationField>();
                    for (int i = 0; i < lsvSendItems.CheckedItems.Count; i++)
                    {
                        IntegrationField field = (IntegrationField)(int.Parse(lsvSendItems.CheckedItems[i].Tag.ToString()));
                        SendFields.Add(field);
                    }

                    bgwSend = new BackgroundWorker();
                    bgwSend.DoWork += new DoWorkEventHandler(bgwSend_DoWork);
                    bgwSend.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgwSend_RunWorkerCompleted);
                    bgwSend.RunWorkerAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void bgwSend_DoWork(object o, DoWorkEventArgs e)
        {
            ExecutionManager SendExecManager = null;
            IntegrationBase IntegrationObj = null;
            try
            {
                SendExecManager = new ExecutionManager();
                SendExecManager.Action_Type = ActionType.Send;
                IntegrationObj = SendExecManager.InitializeIntegrationObject();
                IntegrationObj.ClearProgressHandler += new IntegrationBase.ClearProgressDel(ClearSendProgress);
                IntegrationObj.SetProgressMaxHandler += new IntegrationBase.SetProgressMaxDel(SetSendProgressMax);
                IntegrationObj.ReportProgressHandler += new IntegrationBase.ReportProgressDel(ReportSendProgress);
                IntegrationObj.WriteMessageHandler += new IntegrationBase.WriteMessageDel(WriteMessage);
                IntegrationObj.OrganizationID = OrganizationID;
                SendFilters.SetValue(BuiltInFilters.ExtraSendFilter, txtInv.Text);

                foreach (IntegrationField field in SendFields)
                {
                    int TriggerID = SendExecManager.LogActionTriggerBegining(-1, -1, field.GetHashCode());
                    if (TriggerID != -1)
                    {
                        SendExecManager.TriggerAction(ActionType.Send, field.GetHashCode(), SendFilters, -1, -1, TriggerID, IntegrationObj);
                        SendExecManager.LogActionTriggerEnding(TriggerID);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
            finally
            {
                if (SendExecManager != null)
                    SendExecManager.Dispose();
                if (IntegrationObj != null)
                    IntegrationObj.Dispose();
            }
        }
        private void bgwSend_RunWorkerCompleted(object o, RunWorkerCompletedEventArgs e)
        {
            try
            {
                txtMessages.AppendText("\r\n .............................................................................. \r\n ............................ Sending Completed ........................... \r\n .............................................................................. \r\n");
                ClearSendProgress();
                btnSendToERP.Text = "Start Send";
                btnSendToERP.ForeColor = Color.Black;
                pnlSendEmployeeFilter.Enabled = true;
                gbSendItems.Enabled = true;
                gbFilterSend.Enabled = true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void bgwSpecialActions_DoWork(object o, DoWorkEventArgs e)
        {
            ExecutionManager SpecialActionsExecManager = null;
            IntegrationBase IntegrationObj = null;
            try
            {
                SpecialActionsExecManager = new ExecutionManager();
                SpecialActionsExecManager.Action_Type = ActionType.SpecialFunctions;
                IntegrationObj = SpecialActionsExecManager.InitializeIntegrationObject();
                IntegrationObj.ClearProgressHandler += new IntegrationBase.ClearProgressDel(ClearSpecialActionsProgress);
                IntegrationObj.SetProgressMaxHandler += new IntegrationBase.SetProgressMaxDel(SetSpecialActionsProgressMax);
                IntegrationObj.ReportProgressHandler += new IntegrationBase.ReportProgressDel(ReportSpecialActionsProgress);
                IntegrationObj.WriteMessageHandler += new IntegrationBase.WriteMessageDel(WriteMessage);
                IntegrationObj.OrganizationID = OrganizationID;
                
                foreach (IntegrationField field in SpecialActionFields)
                {
                    int TriggerID = SpecialActionsExecManager.LogActionTriggerBegining(-1, -1, field.GetHashCode());
                    if (TriggerID != -1)
                    {
                        SpecialActionsExecManager.TriggerAction(ActionType.SpecialFunctions, field.GetHashCode(), SpecialActionFilters, -1, -1, TriggerID, IntegrationObj);
                        SpecialActionsExecManager.LogActionTriggerEnding(TriggerID);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
            finally
            {
                if (SpecialActionsExecManager != null)
                    SpecialActionsExecManager.Dispose();
                if (IntegrationObj != null)
                {
                    IntegrationObj.Closeing();
                    IntegrationObj.Dispose();
                }
            }
        }
        private void bgwSpecialActions_RunWorkerCompleted(object o, RunWorkerCompletedEventArgs e)
        {
            try
            {
                txtMessages.AppendText("\r\n .............................................................................. \r\n ............................ Running Completed ........................... \r\n .............................................................................. \r\n");
                ClearSpecialActionsProgress();
                btnRunSpecialActions.Text = "Run";
                btnRunSpecialActions.ForeColor = Color.Black;
                gpSpecialActions.Enabled = true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void btnUpdateFromERP_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnUpdateFromERP.Text == "Start Update")
                {
                    btnUpdateFromERP.Text = "Update in progress";
                    btnUpdateFromERP.ForeColor = Color.Red;
                    pnlImportEmployeeFilter.Enabled = false;
                    pnlMasterDateFilter.Enabled = false;
                    gbImportItems.Enabled = false;
                    gbUpdateStock.Enabled = false;
                    ChkUpdateStock.Enabled = false;
                    Application.DoEvents();

                    UpdateFilters = new IntegrationFilters(ActionType.Update);
                    int EmployeeID = -1;
                    if (!cbImportAllEmployees.Checked)
                        EmployeeID = int.Parse(cmbImportEmployee.SelectedValue.ToString());

                    string warehouseID = "-1";
                    if (!cbxUpdateStockAllVans.Checked && cmbWarehouse.SelectedValue != null)
                        warehouseID = cmbWarehouse.SelectedValue.ToString();

                    UpdateFilters.SetValue(BuiltInFilters.Employee, EmployeeID);
                    UpdateFilters.SetValue(BuiltInFilters.Warehouse, warehouseID);
                    UpdateFilters.SetValue(BuiltInFilters.Organization, OrganizationID);
                    UpdateFilters.SetValue(BuiltInFilters.StockDate, dtpStockDate.Value.Date);
                    UpdateFilters.SetValue(BuiltInFilters.OpenInvoicesOnly, cbOpenInvoicesOnly.Checked);
                    
                    if (txtCustCode.Text.Trim() != "" && txtCustCode.Text.ToLower() != "payer")
                        UpdateFilters.SetValue(BuiltInFilters.CustomerCode, txtCustCode.Text);

                    if (txtTextSearch.Text.Trim() != "" && txtTextSearch.Text != "Enter any text to minimize integrated data")
                        UpdateFilters.SetValue(BuiltInFilters.TextSearch, txtTextSearch.Text);

                    if (dtpImportFromDate.Checked)
                        UpdateFilters.SetValue(BuiltInFilters.FromDate, dtpImportFromDate.Value.Date);
                    
                    if (dtpImportToDate.Checked)
                        UpdateFilters.SetValue(BuiltInFilters.ToDate, dtpImportToDate.Value.Date);
                    
                    UpdateFields = new List<IntegrationField>();
                    for (int i = 0; i < lsvUpdateItems.CheckedItems.Count; i++)
                    {
                        UpdateFields.Add((IntegrationField)(int.Parse(lsvUpdateItems.CheckedItems[i].Tag.ToString())));
                    }
                    if (ChkUpdateStock.Checked)
                    {
                        UpdateFields.Add(IntegrationField.Stock_U);
                    }
                    bgwUpdate = new BackgroundWorker();
                    bgwUpdate.DoWork += new DoWorkEventHandler(bgwUpdate_DoWork);
                    bgwUpdate.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgwUpdate_RunWorkerCompleted);
                    bgwUpdate.RunWorkerAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void bgwUpdate_DoWork(object o, DoWorkEventArgs e)
        {
            ExecutionManager UpdateExecManager = null;
            IntegrationBase IntegrationObj = null;
            try
            {
                UpdateExecManager = new ExecutionManager();
                UpdateExecManager.Action_Type = ActionType.Update;
                IntegrationObj = UpdateExecManager.InitializeIntegrationObject();
                IntegrationObj.ClearProgressHandler += new IntegrationBase.ClearProgressDel(ClearUpdateProgress);
                IntegrationObj.SetProgressMaxHandler += new IntegrationBase.SetProgressMaxDel(SetUpdateProgressMax);
                IntegrationObj.ReportProgressHandler += new IntegrationBase.ReportProgressDel(ReportUpdateProgress);
                IntegrationObj.WriteMessageHandler += new IntegrationBase.WriteMessageDel(WriteMessage);
                IntegrationObj.OrganizationID = OrganizationID;
                
                foreach (IntegrationField field in UpdateFields)
                {
                    int TriggerID = UpdateExecManager.LogActionTriggerBegining(-1, -1, field.GetHashCode());
                    if (TriggerID != -1)
                    {
                        UpdateExecManager.TriggerAction(ActionType.Update, field.GetHashCode(), UpdateFilters, -1, -1, TriggerID, IntegrationObj);
                        UpdateExecManager.LogActionTriggerEnding(TriggerID);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
            finally
            {
                if (UpdateExecManager != null)
                    UpdateExecManager.Dispose();
                if (IntegrationObj != null)
                {
                    IntegrationObj.Closeing();
                    IntegrationObj.Dispose();
                }
                }
            }
        private void bgwUpdate_RunWorkerCompleted(object o, RunWorkerCompletedEventArgs e)
        {
            try
            {
                txtMessages.AppendText("\r\n .............................................................................. \r\n ............................ Updating Completed ........................... \r\n .............................................................................. \r\n");
                ClearUpdateProgress();
                btnUpdateFromERP.Text = "Start Update";
                btnUpdateFromERP.ForeColor = Color.Black;
                pnlImportEmployeeFilter.Enabled = true;
                pnlMasterDateFilter.Enabled = true;
                gbImportItems.Enabled = true;
                gbUpdateStock.Enabled = true;
                ChkUpdateStock.Enabled = true;
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void cbAllEmployees_CheckedChanged(object sender, EventArgs e)
        {
            cmbImportEmployee.Enabled = !cbImportAllEmployees.Checked;
        }
        private void cbSendAllEmployees_CheckedChanged(object sender, EventArgs e)
        {
            cmbSendEmployee.Enabled = !cbSendAllEmployees.Checked;
        }
        private void cbxUpdateAllItems_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (_isLoading)
                    return;

                _isLoading = true;

                for (int i = 0; i < lsvUpdateItems.Items.Count; i++)
                    lsvUpdateItems.Items[i].Checked = cbUpdateAllItems.Checked;

                _isLoading = false;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                _isLoading = false;
            }
        }
        private void lvUpdateItems_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            try
            {
                if (_isLoading)
                    return;

                _isLoading = true;

                cbUpdateAllItems.Checked = lsvUpdateItems.CheckedItems.Count == lsvUpdateItems.Items.Count;

                _isLoading = false;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                _isLoading = false;
            }
        }
        private void cbSendAllItems_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (_isLoading)
                    return;

                _isLoading = true;

                for (int i = 0; i < lsvSendItems.Items.Count; i++)
                    lsvSendItems.Items[i].Checked = cbSendAllItems.Checked;

                _isLoading = false;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                _isLoading = false;
            }
        }
        private void lsvSendItems_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            try
            {
                if (_isLoading)
                    return;

                _isLoading = true;

                cbSendAllItems.Checked = lsvSendItems.CheckedItems.Count == lsvSendItems.Items.Count;

                _isLoading = false;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                _isLoading = false;
            }
        }
        private void cbxUpdateStockAllVans_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoading)
                return;

            cmbWarehouse.Enabled = !cbxUpdateStockAllVans.Checked;
        }
        private void ChkUpdateStock_CheckedChanged(object sender, EventArgs e)
        {
            gbUpdateStock.Enabled = ChkUpdateStock.Checked;
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
        }
        private void txtCustCode_MouseDown(object sender, MouseEventArgs e)
        {
            txtCustCode.Clear();
            txtCustCode.ForeColor = Color.Black;
            
        }
        private void txtTextSearch_MouseDown(object sender, MouseEventArgs e)
        {
            txtTextSearch.Clear();
            txtTextSearch.ForeColor = Color.Black;
        }
        #endregion

        #region Methods

        private void ApplyConditionalFormatting()
        {
            try
            {
                tabControl1.TabPages.RemoveAt(2);
                tabControl1.TabPages.RemoveAt(1);
                tabControl1.TabPages.RemoveAt(0);
                foreach (KeyValuePair<Menus, string> menu in CoreGeneral.Common.userPrivileges.MenusAccess)
                {
                    if (menu.Key == Menus.Integration_Send)
                    {
                        tpSend.Text = menu.Value;
                        tabControl1.TabPages.Add(tpSend);
                    }
                    if (menu.Key == Menus.Integration_SpecialActions)
                    {
                        tpSpecial.Text = menu.Value;
                        tabControl1.TabPages.Add(tpSpecial);
                    }
                    if (menu.Key == Menus.Integration_Update)
                    {
                        tpImport.Text = menu.Value;
                        tabControl1.TabPages.Add(tpImport);
                    }
                }

                switch (CoreGeneral.Common.GeneralConfigurations.SiteSymbol.ToLower())
                {
                    case "maidubai":
                        dtpStockDate.MinDate = new DateTime(2017, 3, 25);
                        break;
                    case "brf":
                        pnlImportEmployeeFilter.Visible = true;
                        break;
                    case "qnie":
                        pnlMasterDateFilter.Visible = true;
                        dtpImportFromDate.Value = DateTime.Today;
                        dtpImportToDate.Value = DateTime.Today;
                        lblStockDate.Text = "Trans Date";
                        lblWarehouse.Text = "Vehicle";
                        break;
                    case "qnie_presales":
                        pnlMasterDateFilter.Visible = true;
                        dtpImportFromDate.Value = DateTime.Today;
                        dtpImportToDate.Value = DateTime.Today;
                        break;
                    case "esf":
                    case "esfnew":
                        lblDocNo.Visible = true;
                        txtInv.Visible = true;
                        lblOR.Visible = true;
                        pnlTextSearchFilter.Visible = true;
                        break;
                }
                if (pnlMasterDateFilter.Visible)
                    pnlMasterDateFilter.Location = pnlImportEmployeeFilter.Location;
                if (pnlTextSearchFilter.Visible)
                    pnlTextSearchFilter.Location = pnlImportEmployeeFilter.Location;

                Color backColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                if (CoreGeneral.Common.GeneralConfigurations.OrganizationOriented && CoreGeneral.Common.OrganizationConfigurations.ContainsKey(OrganizationID))
                {
                    backColor = CoreGeneral.Common.OrganizationConfigurations[OrganizationID].IntegrationFormBackColor;
                }

                string Title = "";
                if (CoreGeneral.Common.GeneralConfigurations.OrganizationOriented && CoreGeneral.Common.OrganizationConfigurations.ContainsKey(OrganizationID))
                {
                    Title = CoreGeneral.Common.OrganizationConfigurations[OrganizationID].IntegrationFormTitle;
                }
                if (Title == "")
                {
                    Title = CoreGeneral.Common.GeneralConfigurations.IntegrationFormTitle;
                }
                this.Text = Title;
                this.BackColor = backColor;
                tpImport.BackColor = backColor;
                tpSend.BackColor = backColor;
                tpSpecial.BackColor = backColor;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void AddItemsTolist()
        {
            try
            {
                if (!CoreGeneral.Common.userPrivileges.UpdateFieldsAccess.ContainsKey(IntegrationField.Stock_U))
                {
                    ChkUpdateStock.Visible = false;
                    gbUpdateStock.Visible = false;
                }
                else
                {
                    ChkUpdateStock.Checked = CoreGeneral.Common.userPrivileges.UpdateFieldsAccess[IntegrationField.Stock_U].DefaultCheck;
                    ChkUpdateStock.Text = CoreGeneral.Common.userPrivileges.UpdateFieldsAccess[IntegrationField.Stock_U].Description;
                }

                foreach (FieldItem field in CoreGeneral.Common.userPrivileges.UpdateFieldsAccess.Values)
                {
                    if (field.Field != IntegrationField.Stock_U)
                    {
                        ListViewItem _item = new ListViewItem();
                        _item.Text = field.Description;
                        _item.Tag = field.FieldID;
                        _item.Name = field.Description;
                        _item.Checked = field.DefaultCheck;
                        lsvUpdateItems.Items.Add(_item);
                    }
                }

                foreach (FieldItem field in CoreGeneral.Common.userPrivileges.SendFieldsAccess.Values)
                {
                    ListViewItem _item = new ListViewItem();
                    _item.Text = field.Description;
                    _item.Tag = field.FieldID;
                    _item.Name = field.Description;
                    _item.Checked = field.DefaultCheck;
                    lsvSendItems.Items.Add(_item);
                }

                foreach (FieldItem field in CoreGeneral.Common.userPrivileges.SpecialFunctionsAccess.Values)
                {
                    ListViewItem _item = new ListViewItem();
                    _item.Text = field.Description;
                    _item.Tag = field.FieldID;
                    _item.Name = field.Description;
                    _item.Checked = field.DefaultCheck;
                    lsvSpecialActions.Items.Add(_item);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void FillEmployeesCombos()
        {
            try
            {
                using (EmployeeManager empManager = new EmployeeManager())
                {
                    DataTable dtEmployees = empManager.GetEmployees(OrganizationID);

                    cmbImportEmployee.DataSource = dtEmployees;
                    cmbImportEmployee.DisplayMember = "Employee";
                    cmbImportEmployee.ValueMember = "EmployeeID";

                    DataTable dtSendEmployees = dtEmployees.Copy();
                    cmbSendEmployee.DataSource = dtSendEmployees;
                    cmbSendEmployee.DisplayMember = "Employee";
                    cmbSendEmployee.ValueMember = "EmployeeID";
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void FillWarehousesCombo()
        {
            try
            {
                using (WarehouseManager whManager = new WarehouseManager())
                {
                    DataTable dtWarehouses = whManager.GetWarehouses(OrganizationID);
                    cmbWarehouse.DataSource = dtWarehouses;
                    cmbWarehouse.DisplayMember = "Warehouse";
                    cmbWarehouse.ValueMember = "WarehouseID";
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void SetUpdateProgressMax(int maxValue)
        {
            try
            {
                if (maxValue >= 0)
                    UpdateProgressBar.Maximum = maxValue;
                else
                    UpdateProgressBar.Maximum = 0;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void ClearUpdateProgress()
        {
            try
            {
#if LegacyUI

#else
                UpdateProgressBar.CustomText = "";
#endif

                UpdateProgressBar.Maximum = 0;
                UpdateProgressBar.Value = 0;
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void ReportUpdateProgress(int currentValue, string labelText)
        {
            try
            {
                if (currentValue == -1)
                    currentValue = UpdateProgressBar.Value + 1;

                if (currentValue <= UpdateProgressBar.Maximum)
                {
                    UpdateProgressBar.Value = currentValue;
#if LegacyUI

#else
                    UpdateProgressBar.CustomText = labelText + " " + currentValue + "/" + UpdateProgressBar.Maximum;
#endif
                    Application.DoEvents();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void WriteMessage(string Message)
        {
            txtMessages.AppendText(Message);
        }
        private void SetSendProgressMax(int maxValue)
        {
            try
            {
                if (maxValue >= 0)
                    SendProgressBar.Maximum = maxValue;
                else
                    SendProgressBar.Maximum = 0;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void SetSpecialActionsProgressMax(int maxValue)
        {
            try
            {
                if (maxValue >= 0)
                    SpecialAccessProgressBar.Maximum = maxValue;
                else
                    SpecialAccessProgressBar.Maximum = 0;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void ClearSendProgress()
        {
            try
            {
#if LegacyUI

#else
                SendProgressBar.CustomText = "";
#endif

                SendProgressBar.Maximum = 0;
                SendProgressBar.Value = 0;
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void ClearSpecialActionsProgress()
        {
            try
            {
#if LegacyUI

#else
                SpecialAccessProgressBar.CustomText = "";
#endif

                SpecialAccessProgressBar.Maximum = 0;
                SpecialAccessProgressBar.Value = 0;
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void ReportSendProgress(int currentValue, string labelText)
        {
            try
            {
                if (currentValue == -1)
                    currentValue = SendProgressBar.Value + 1;

                if (currentValue <= SendProgressBar.Maximum)
                {
                    SendProgressBar.Value = currentValue;
#if LegacyUI

#else
                    SendProgressBar.CustomText = labelText + " " + currentValue + "/" + SendProgressBar.Maximum;
#endif

                    Application.DoEvents();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void ReportSpecialActionsProgress(int currentValue, string labelText)
        {
            try
            {
                if (currentValue == -1)
                    currentValue = SpecialAccessProgressBar.Value + 1;

                if (currentValue <= SpecialAccessProgressBar.Maximum)
                {
                    SpecialAccessProgressBar.Value = currentValue;
#if LegacyUI

#else
                    SpecialAccessProgressBar.CustomText = labelText + " " + currentValue + "/" + SpecialAccessProgressBar.Maximum;
#endif

                    Application.DoEvents();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        #endregion

        private void btnRunSpecialActions_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnRunSpecialActions.Text == "Run")
                {
                    btnRunSpecialActions.Text = "Running in progress";
                    btnRunSpecialActions.ForeColor = Color.Red;
                    gpSpecialActions.Enabled = false;
                    Application.DoEvents();

                    SpecialActionFilters = new IntegrationFilters(ActionType.SpecialFunctions);
                    SpecialActionFilters.SetValue(BuiltInFilters.Organization, OrganizationID);
                    SpecialActionFilters.SetValue(BuiltInFilters.FromDate, dtpSF_FromDate.Value.Date);
                    SpecialActionFilters.SetValue(BuiltInFilters.ToDate, dtpSF_ToDate.Value.Date);

                    SpecialActionFields = new List<IntegrationField>();
                    for (int i = 0; i < lsvSpecialActions.CheckedItems.Count; i++)
                    {
                        IntegrationField field = (IntegrationField)(int.Parse(lsvSpecialActions.CheckedItems[i].Tag.ToString()));
                        if (field == IntegrationField.DataTransfer_SP || field == IntegrationField.DatabaseBackup_SP || field == IntegrationField.FilesJobs_SP)
                        {
                            Dictionary<int, string> filterValues = new Dictionary<int, string>();
                            frmFieldFilters frm = new frmFieldFilters(FormMode.View, field, ref filterValues);
                            frm.ShowDialog();
                            if (filterValues.ContainsKey(BuiltInFilters.DataTransferCheckList.GetHashCode()))
                                SpecialActionFilters.SetValue(BuiltInFilters.DataTransferCheckList, filterValues[BuiltInFilters.DataTransferCheckList.GetHashCode()]);
                            else if (filterValues.ContainsKey(BuiltInFilters.FilesManagementJobs.GetHashCode()))
                                SpecialActionFilters.SetValue(BuiltInFilters.FilesManagementJobs, filterValues[BuiltInFilters.FilesManagementJobs.GetHashCode()]);
                            else if(filterValues.ContainsKey(BuiltInFilters.DatabaseBackupJob.GetHashCode()))
                                SpecialActionFilters.SetValue(BuiltInFilters.DatabaseBackupJob, filterValues[BuiltInFilters.DatabaseBackupJob.GetHashCode()]);
                        }
                        SpecialActionFields.Add(field);
                    }

                    bgwSpecialActions = new BackgroundWorker();
                    bgwSpecialActions.DoWork += new DoWorkEventHandler(bgwSpecialActions_DoWork);
                    bgwSpecialActions.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgwSpecialActions_RunWorkerCompleted);
                    bgwSpecialActions.RunWorkerAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}
