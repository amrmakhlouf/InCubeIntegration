using InCubeLibrary;
using System;
using System.Data;
using System.Windows.Forms;
using System.Collections.Generic;
using InCubeIntegration_BL;

namespace InCubeIntegration_UI
{
    public partial class frmFieldFilters : Form
    {
        Dictionary<int, string> FilterValues;
        IntegrationField field;
        List<BuiltInFilters> FieldFilters = new List<BuiltInFilters>();
        DataTable dtTransferGroups = new DataTable();
        ExecutionManager execManager;
        FormMode frmMode;
        bool checkChanged = false;
        public frmFieldFilters(FormMode FrmMode, IntegrationField _field, ref Dictionary<int, string> _filterValues)
        {
            FilterValues = _filterValues;
            field = _field;
            frmMode = FrmMode;
            execManager = new ExecutionManager();
            InitializeComponent();
        }

        private void btnPlusMinusFrom_Click(object sender, EventArgs e)
        {
            if (btnPlusMinusFrom.Text == "-")
                btnPlusMinusFrom.Text = "+";
            else if (btnPlusMinusFrom.Text == "+")
                btnPlusMinusFrom.Text = "-";
        }

        private void btnPlusMinusTo_Click(object sender, EventArgs e)
        {
            if (btnPlusMinusTo.Text == "-")
                btnPlusMinusTo.Text = "+";
            else if (btnPlusMinusTo.Text == "+")
                btnPlusMinusTo.Text = "-";
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (FieldFilters.Contains(BuiltInFilters.FromDate) && FieldFilters.Contains(BuiltInFilters.ToDate))
                {
                    int from = 0;
                    int to = 0;
                    if (nudFrom.Value != 0)
                    {
                        if (btnPlusMinusFrom.Text == "-")
                            from = -1 * (int)nudFrom.Value;
                        if (btnPlusMinusFrom.Text == "+")
                            from = (int)nudFrom.Value;
                    }
                    if (nudTo.Value != 0)
                    {
                        if (btnPlusMinusTo.Text == "-")
                            to = -1 * (int)nudTo.Value;
                        if (btnPlusMinusTo.Text == "+")
                            to = (int)nudTo.Value;
                    }
                    if (to < from)
                    {
                        MessageBox.Show("To date value must be equal or larger than from date value");
                        return;
                    }
                    else
                    {
                        if (FilterValues.ContainsKey(BuiltInFilters.FromDate.GetHashCode()))
                        {
                            FilterValues[BuiltInFilters.FromDate.GetHashCode()] = "@Today" + (from != 0 ? (from > 0 ? "+" + from.ToString() : from.ToString()) : "");
                        }
                        else
                        {
                            FilterValues.Add(BuiltInFilters.FromDate.GetHashCode(), "@Today" + (from != 0 ? (from > 0 ? "+" + from.ToString() : from.ToString()) : ""));
                        }

                        if (FilterValues.ContainsKey(BuiltInFilters.ToDate.GetHashCode()))
                        {
                            FilterValues[BuiltInFilters.ToDate.GetHashCode()] = "@Today" + (to != 0 ? (to > 0 ? "+" + to.ToString() : to.ToString()) : "");
                        }
                        else
                        {
                            FilterValues.Add(BuiltInFilters.ToDate.GetHashCode(), "@Today" + (to != 0 ? (to > 0 ? "+" + to.ToString() : to.ToString()) : ""));
                        }
                    }
                }
                if (FieldFilters.Contains(BuiltInFilters.StockDate))
                {
                    int on = 0;
                    if (nudOn.Value != 0)
                    {
                        if (btnPlusMinusOn.Text == "-")
                            on = -1 * (int)nudOn.Value;
                        if (btnPlusMinusOn.Text == "+")
                            on = (int)nudOn.Value;
                    }
                    if (FilterValues.ContainsKey(BuiltInFilters.StockDate.GetHashCode()))
                    {
                        FilterValues[BuiltInFilters.StockDate.GetHashCode()] = "@Today" + (on != 0 ? (on > 0 ? "+" + on.ToString() : on.ToString()) : "");
                    }
                    else
                    {
                        FilterValues.Add(BuiltInFilters.StockDate.GetHashCode(), "@Today" + (on != 0 ? (on > 0 ? "+" + on.ToString() : on.ToString()) : ""));
                    }
                }
                if (FieldFilters.Contains(BuiltInFilters.DatabaseBackupJob) || FieldFilters.Contains(BuiltInFilters.FilesManagementJobs) || FieldFilters.Contains(BuiltInFilters.DataTransferCheckList) || FieldFilters.Contains(BuiltInFilters.DataWarehouseCheckList))
                {
                    if (lsvOptions.Items.Count > 0 && lsvOptions.CheckedItems.Count == 0)
                    {
                        MessageBox.Show("Check at least one option");
                        return;
                    }
                    string value = "";
                    foreach (ListViewItem lsvItem in lsvOptions.CheckedItems)
                    {
                        value += lsvItem.SubItems[1].Text + ",";
                    }
                    value = value.Substring(0, value.Length - 1);

                    if (FieldFilters.Contains(BuiltInFilters.DatabaseBackupJob))
                    {
                        if (FilterValues.ContainsKey(BuiltInFilters.DatabaseBackupJob.GetHashCode()))
                            FilterValues[BuiltInFilters.DatabaseBackupJob.GetHashCode()] = value;
                        else
                            FilterValues.Add(BuiltInFilters.DatabaseBackupJob.GetHashCode(), value);
                    }

                    if (FieldFilters.Contains(BuiltInFilters.FilesManagementJobs))
                    {
                        if (FilterValues.ContainsKey(BuiltInFilters.FilesManagementJobs.GetHashCode()))
                            FilterValues[BuiltInFilters.FilesManagementJobs.GetHashCode()] = value;
                        else
                            FilterValues.Add(BuiltInFilters.FilesManagementJobs.GetHashCode(), value);
                    }

                    if (FieldFilters.Contains(BuiltInFilters.DataTransferCheckList))
                    {
                        if (FilterValues.ContainsKey(BuiltInFilters.DataTransferCheckList.GetHashCode()))
                            FilterValues[BuiltInFilters.DataTransferCheckList.GetHashCode()] = value;
                        else
                            FilterValues.Add(BuiltInFilters.DataTransferCheckList.GetHashCode(), value);
                    }

                    if (FieldFilters.Contains(BuiltInFilters.DataWarehouseCheckList))
                    {
                        if (FilterValues.ContainsKey(BuiltInFilters.DataWarehouseCheckList.GetHashCode()))
                            FilterValues[BuiltInFilters.DataWarehouseCheckList.GetHashCode()] = value;
                        else
                            FilterValues.Add(BuiltInFilters.DataWarehouseCheckList.GetHashCode(), value);
                    }
                }
                this.Close();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void frmFieldFilters_Load(object sender, EventArgs e)
        {
            try
            {
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                tcFilters.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                if (frmMode == FormMode.View)
                    btnSave.Text = "Ok";

                execManager.GetFieldFilters(field, ref FieldFilters);

                if (FieldFilters.Contains(BuiltInFilters.FromDate) && FieldFilters.Contains(BuiltInFilters.ToDate))
                {
                    TabPage tp = new TabPage("Date Filters");
                    pnlFromToDateFilters.Visible = true;
                    pnlFromToDateFilters.Location = new System.Drawing.Point(0, 0);
                    pnlFromToDateFilters.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                    pnlFromToDateFilters.Dock = DockStyle.Fill;
                    tp.Controls.Add(pnlFromToDateFilters);
                    tcFilters.TabPages.Add(tp);
                }
                if (FieldFilters.Contains(BuiltInFilters.StockDate))
                {
                    TabPage tp = new TabPage("Trans Date");
                    pnlDate.Visible = true;
                    pnlDate.Location = new System.Drawing.Point(0, 0);
                    pnlDate.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                    pnlDate.Dock = DockStyle.Fill;
                    tp.Controls.Add(pnlDate);
                    tcFilters.TabPages.Add(tp);
                }
                if (FieldFilters.Contains(BuiltInFilters.DataTransferCheckList) || FieldFilters.Contains(BuiltInFilters.DataWarehouseCheckList))
                {
                    List<string> IDs = new List<string>();
                    if (FilterValues.ContainsKey(BuiltInFilters.DataTransferCheckList.GetHashCode()))
                    {
                        string[] checkedTypes = FilterValues[(BuiltInFilters.DataTransferCheckList.GetHashCode())].Split(new char[] { ',' });
                        IDs = new List<string>(checkedTypes);
                    }
                    if (FilterValues.ContainsKey(BuiltInFilters.DataWarehouseCheckList.GetHashCode()))
                    {
                        string[] checkedTypes = FilterValues[(BuiltInFilters.DataWarehouseCheckList.GetHashCode())].Split(new char[] { ',' });
                        IDs = new List<string>(checkedTypes);
                    }
                    using (DataTransferManager transferManager = new DataTransferManager())
                    {
                        transferManager.GetTransferGroups(ref dtTransferGroups, FieldFilters.Contains(BuiltInFilters.DataTransferCheckList) ? 1 : 2, false);
                    }

                    Dictionary<string, ListViewGroup> groups = new Dictionary<string, ListViewGroup>();
                    foreach (DataRow dr in dtTransferGroups.Rows)
                    {
                        string ID = dr["GroupID"].ToString();
                        string Name = dr["GroupName"].ToString();
                        
                        ListViewItem lsvItem = new ListViewItem();
                        lsvItem.SubItems.Add(ID);
                        lsvItem.SubItems.Add(Name);
                        if (IDs.Contains(ID))
                            lsvItem.Checked = true;
                        lsvOptions.Items.Add(lsvItem);
                    }

                    TabPage tp = new TabPage(FieldFilters.Contains(BuiltInFilters.DataTransferCheckList) ? "Data Transfer Filter" : "Data Warehouse Filter");
                    pnlCheckListFilter.Visible = true;
                    pnlCheckListFilter.Location = new System.Drawing.Point(0, 0);
                    pnlCheckListFilter.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                    pnlCheckListFilter.Dock = DockStyle.Fill;
                    tp.Controls.Add(pnlCheckListFilter);
                    tcFilters.TabPages.Add(tp);
                }

                if (FilterValues.ContainsKey(BuiltInFilters.FromDate.GetHashCode()))
                {
                    string value = FilterValues[BuiltInFilters.FromDate.GetHashCode()];
                    string sign = "-";
                    int days = 0;
                    if (value.Length > 6)
                    {
                        sign = value.Substring(6, 1);
                        days = int.Parse(value.Substring(7, value.Length - 7));
                    }
                    btnPlusMinusFrom.Text = sign;
                    nudFrom.Value = days;
                }
                if (FilterValues.ContainsKey(BuiltInFilters.ToDate.GetHashCode()))
                {
                    string value = FilterValues[BuiltInFilters.ToDate.GetHashCode()];
                    string sign = "-";
                    int days = 0;
                    if (value.Length > 6)
                    {
                        sign = value.Substring(6, 1);
                        days = int.Parse(value.Substring(7, value.Length - 7));
                    }
                    btnPlusMinusTo.Text = sign;
                    nudTo.Value = days;
                }
                if (FilterValues.ContainsKey(BuiltInFilters.StockDate.GetHashCode()))
                {
                    string value = FilterValues[BuiltInFilters.StockDate.GetHashCode()];
                    string sign = "-";
                    int days = 0;
                    if (value.Length > 6)
                    {
                        sign = value.Substring(6, 1);
                        days = int.Parse(value.Substring(7, value.Length - 7));
                    }
                    btnPlusMinusOn.Text = sign;
                    nudOn.Value = days;
                }

                if (FieldFilters.Contains(BuiltInFilters.FilesManagementJobs) || FieldFilters.Contains(BuiltInFilters.DatabaseBackupJob))
                {
                    List<string> IDs = new List<string>();
                    if (FilterValues.ContainsKey(BuiltInFilters.FilesManagementJobs.GetHashCode()))
                    {
                        string[] checkedTypes = FilterValues[(BuiltInFilters.FilesManagementJobs.GetHashCode())].Split(new char[] { ',' });
                        IDs = new List<string>(checkedTypes);
                    }
                    if (FilterValues.ContainsKey(BuiltInFilters.DatabaseBackupJob.GetHashCode()))
                    {
                        string[] checkedTypes = FilterValues[(BuiltInFilters.DatabaseBackupJob.GetHashCode())].Split(new char[] { ',' });
                        IDs = new List<string>(checkedTypes);
                    }

                    DataTable dtJobs = new DataTable();
                    if (FieldFilters.Contains(BuiltInFilters.FilesManagementJobs))
                        dtJobs = execManager.GetActiveFilesManagementJobs();
                    else if (FieldFilters.Contains(BuiltInFilters.DatabaseBackupJob))
                        dtJobs = execManager.GetActiveDatabaseBackupJob();

                    foreach (DataRow dr in dtJobs.Rows)
                    {
                        string ID = dr["JobID"].ToString();
                        string Name = dr["JobName"].ToString();
                        
                        ListViewItem lsvItem = new ListViewItem();
                        lsvItem.SubItems.Add(ID);
                        lsvItem.SubItems.Add(Name);
                        if (IDs.Contains(ID))
                            lsvItem.Checked = true;
                        lsvOptions.Items.Add(lsvItem);
                    }

                    TabPage tp = new TabPage();
                    if (FieldFilters.Contains(BuiltInFilters.FilesManagementJobs))
                        tp.Text = "Files Management Jobs";
                    else if (FieldFilters.Contains(BuiltInFilters.DatabaseBackupJob))
                        tp.Text = "Database Backup Jobs";

                    pnlCheckListFilter.Visible = true;
                    pnlCheckListFilter.Location = new System.Drawing.Point(0, 0);
                    pnlCheckListFilter.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                    pnlCheckListFilter.Dock = DockStyle.Fill;
                    tp.Controls.Add(pnlCheckListFilter);
                    tcFilters.TabPages.Add(tp);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void cbAllOptions_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (!checkChanged)
                {
                    checkChanged = true;
                    foreach (ListViewItem lsvItem in lsvOptions.Items)
                        lsvItem.Checked = cbAllOptions.Checked;
                    checkChanged = false;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void lsvOptions_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            try
            {
                if (!checkChanged)
                {
                    checkChanged = true;
                    cbAllOptions.Checked = lsvOptions.Items.Count == lsvOptions.CheckedItems.Count;
                    checkChanged = false;
                }
            }
            catch (Exception ex)
            {
                checkChanged = false;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnPlusMinusOn_Click(object sender, EventArgs e)
        {
            if (btnPlusMinusOn.Text == "-")
                btnPlusMinusOn.Text = "+";
            else if (btnPlusMinusOn.Text == "+")
                btnPlusMinusOn.Text = "-";
        }
    }
}
