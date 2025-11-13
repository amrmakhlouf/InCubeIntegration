using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.Data;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmLocationsExport : Form
    {
        ExecutionManager execManager;
        IntegrationBase integrationObj;
        RoadNetManager RNManager;
        bool _isFillig = false;
        DataTable dtLocations = new DataTable();

        private enum GridColumns
        {
            Check = 0,
            CustomerCode,
            CustomerName,
            RegionID,
            AccountType,
            Address1,
            Address2,
            PhoneNo,
            Latitude,
            Longitude,
            VisitPatternSet,
            FixedServiceTime,
            VariableServiceTime,
            DropSize,
            OpenTime,
            CloseTime,
            TW1Start,
            TW1Stop,
            TW2Start,
            TW2Stop
        }
        public frmLocationsExport()
        {
            try
            {
                InitializeComponent();
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                execManager = new ExecutionManager();
                integrationObj = new IntegrationBase(execManager);
                RNManager = new RoadNetManager(true, integrationObj);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void frmLocationsExport_Load(object sender, EventArgs e)
        {
            try
            {
                FillFilters();
                grdLocations.AutoGenerateColumns = false;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void FillFilters()
        {
            try
            {
                DataTable dtGroups = new DataTable();
                Result Res = RNManager.GetCustomerGroups(ref dtGroups);
                cmbCustomerGroups.DataSource = dtGroups;
                cmbCustomerGroups.DisplayMember = "Description";
                cmbCustomerGroups.ValueMember = "GroupID";
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void cbAllGroups_CheckedChanged(object sender, EventArgs e)
        {
            cmbCustomerGroups.Enabled = !cbAllGroups.Checked;
        }

        private void btnGetLocations_Click(object sender, EventArgs e)
        {
            try
            {
                int GroupID = -1;
                if (!cbAllGroups.Checked)
                    GroupID = Convert.ToInt32(cmbCustomerGroups.SelectedValue);
                DataTable dtLocations = new DataTable();
                Result Res = RNManager.GetLocations(GroupID, ref dtLocations);
                if (Res == Result.Success)
                {
                    cbAllLocations.Enabled = dtLocations.Rows.Count > 0;
                    _isFillig = true;
                    grdLocations.DataSource = null;
                    grdLocations.DataSource = dtLocations;
                    for (int i = 0; i < grdLocations.Rows.Count; i++)
                    {
                        grdLocations.Rows[i].Cells[0].Value = 1;
                    }
                    cbAllLocations.Checked = dtLocations.Rows.Count > 0;
                    btnSendLocations.Enabled = dtLocations.Rows.Count > 0;
                    lblSelectedLocations.Text = string.Format("Selected: {0}/{1}", grdLocations.Rows.Count, grdLocations.Rows.Count);
                    _isFillig = false;
                }
                else
                {
                    cbAllLocations.Enabled = false;
                    cbAllLocations.Checked = false;
                    lblSelectedLocations.Text = string.Format("Selected: 0/0");
                    btnSendLocations.Enabled = false;
                    grdLocations.DataSource = null;
                    MessageBox.Show("Error retrieving locations!!");
                }

            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void cbAllLocations_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_isFillig)
                {
                    _isFillig = true;
                    for (int i = 0; i < grdLocations.Rows.Count; i++)
                    {
                        grdLocations.Rows[i].Cells[0].Value = cbAllLocations.Checked;
                    }
                    lblSelectedLocations.Text = string.Format("Selected: {0}/{1}", cbAllLocations.Checked ? grdLocations.Rows.Count : 0, grdLocations.Rows.Count);
                    btnSendLocations.Enabled = cbAllLocations.Checked;
                    _isFillig = false;
                }
            }
            catch (Exception ex)
            {
                _isFillig = false;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void grdLocations_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (grdLocations.CurrentCell.ColumnIndex == 0)
            {
                grdLocations.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void grdLocations_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isFillig)
                return;
            try
            {
                if (e.ColumnIndex == 0)
                {
                    int checkedCount = 0;
                    _isFillig = true;
                    for (int i = 0; i < grdLocations.Rows.Count; i++)
                    {
                        if (Convert.ToInt16(grdLocations.Rows[i].Cells[0].Value) == 1)
                        {
                            checkedCount++;
                        }
                    }
                    lblSelectedLocations.Text = string.Format("Selected: {0}/{1}", checkedCount, grdLocations.Rows.Count);
                    cbAllLocations.Checked = checkedCount == grdLocations.Rows.Count;
                    btnSendLocations.Enabled = checkedCount > 0;
                    _isFillig = false;
                }
            }
            catch (Exception ex)
            {
                _isFillig = false;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void btnSendLocations_Click(object sender, EventArgs e)
        {
            try
            {
                frmRoadNetIntegrationExecution frm = new frmRoadNetIntegrationExecution(frmRoadNetIntegrationExecution.Mode.ExportLocations);
                frm.PrepareTablesForLocationsExportHandler += new frmRoadNetIntegrationExecution.PrepareTablesForLocationsExportDel(PrepareTables);
                frm.SendLocationsHandler += new frmRoadNetIntegrationExecution.SendLocationsDel(SendLocations);
                frm.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private bool PrepareTables(ref int locations)
        {
            try
            {
                dtLocations = new DataTable();
                dtLocations.Columns.Add("CustomerCode");
                dtLocations.Columns.Add("CustomerName");
                dtLocations.Columns.Add("RegionID");
                dtLocations.Columns.Add("AccountType");
                dtLocations.Columns.Add("Address1");
                dtLocations.Columns.Add("Address2");
                dtLocations.Columns.Add("PhoneNo");
                dtLocations.Columns.Add("Latitude");
                dtLocations.Columns.Add("Longitude");
                dtLocations.Columns.Add("VisitPatternSet");
                dtLocations.Columns.Add("FixedServiceTime");
                dtLocations.Columns.Add("VariableServiceTime");
                dtLocations.Columns.Add("DropSize");
                dtLocations.Columns.Add("TW1Start");
                dtLocations.Columns.Add("TW1Stop");
                dtLocations.Columns.Add("TW2Start");
                dtLocations.Columns.Add("TW2Stop");
                dtLocations.Columns.Add("OpenTime");
                dtLocations.Columns.Add("CloseTime");

                locations = 0;
                for (int i = 0; i < grdLocations.Rows.Count; i++)
                {
                    int isChecked = Convert.ToInt16(grdLocations.Rows[i].Cells[0].Value);
                    if (isChecked == 1)
                    {
                        locations++;

                        DataRow dr = dtLocations.NewRow();

                        dr["CustomerCode"] = GetTruncatedString(grdLocations.Rows[i].Cells[GridColumns.CustomerCode.GetHashCode()].Value.ToString(), 15, true);
                        dr["CustomerName"] = GetTruncatedString(grdLocations.Rows[i].Cells[GridColumns.CustomerName.GetHashCode()].Value.ToString(), 60, false);
                        dr["RegionID"] = GetTruncatedString(grdLocations.Rows[i].Cells[GridColumns.RegionID.GetHashCode()].Value.ToString(), 19, false);
                        dr["AccountType"] = GetTruncatedString(grdLocations.Rows[i].Cells[GridColumns.AccountType.GetHashCode()].Value.ToString(), 5, false);
                        dr["Address1"] = GetTruncatedString(grdLocations.Rows[i].Cells[GridColumns.Address1.GetHashCode()].Value.ToString(), 60, false);
                        dr["Address2"] = GetTruncatedString(grdLocations.Rows[i].Cells[GridColumns.Address2.GetHashCode()].Value.ToString(), 30, false);
                        dr["PhoneNo"] = GetTruncatedString(grdLocations.Rows[i].Cells[GridColumns.PhoneNo.GetHashCode()].Value.ToString(), 20, true);
                        dr["Latitude"] = ValidateLatitude(grdLocations.Rows[i].Cells[GridColumns.Latitude.GetHashCode()].Value);
                        dr["Longitude"] = ValidateLongitude(grdLocations.Rows[i].Cells[GridColumns.Longitude.GetHashCode()].Value);
                        dr["VisitPatternSet"] = GetTruncatedString(grdLocations.Rows[i].Cells[GridColumns.VisitPatternSet.GetHashCode()].Value.ToString(), 3, false);
                        dr["FixedServiceTime"] = ValidateIntegerValue(grdLocations.Rows[i].Cells[GridColumns.FixedServiceTime.GetHashCode()].Value);
                        dr["VariableServiceTime"] = ValidateDecimalValue(grdLocations.Rows[i].Cells[GridColumns.VariableServiceTime.GetHashCode()].Value, true);
                        dr["DropSize"] = ValidateDecimalValue(grdLocations.Rows[i].Cells[GridColumns.DropSize.GetHashCode()].Value, true);
                        dr["OpenTime"] = ValidateTimeValue(grdLocations.Rows[i].Cells[GridColumns.OpenTime.GetHashCode()].Value, "00:00");
                        dr["TW1Start"] = ValidateTimeValue(grdLocations.Rows[i].Cells[GridColumns.TW1Start.GetHashCode()].Value, "00:00");
                        dr["TW2Start"] = ValidateTimeValue(grdLocations.Rows[i].Cells[GridColumns.TW2Start.GetHashCode()].Value, "00:00");
                        dr["CloseTime"] = ValidateTimeValue(grdLocations.Rows[i].Cells[GridColumns.CloseTime.GetHashCode()].Value, "23:59");
                        dr["TW1Stop"] = ValidateTimeValue(grdLocations.Rows[i].Cells[GridColumns.TW1Stop.GetHashCode()].Value, "23:59");
                        dr["TW2Stop"] = ValidateTimeValue(grdLocations.Rows[i].Cells[GridColumns.TW2Stop.GetHashCode()].Value, "23:59");
                        dtLocations.Rows.Add(dr);
                    }
                }
                locations = dtLocations.Rows.Count;
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return false;
            }
        }
        private bool SendLocations()
        {
            return RNManager.SendLocations(dtLocations);
        }
        private bool SendLocationExtensions()
        {
            return RNManager.SendLocationExtensions(dtLocations);
        }
        private decimal ValidateLatitude (object GeoCode)
        {
            decimal validValue = 0;
            try
            {
                if (decimal.TryParse(GeoCode.ToString(), out validValue))
                {
                    if (validValue > 90 || validValue < -90)
                        validValue = 0;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return validValue;
        }
        private decimal ValidateLongitude(object GeoCode)
        {
            decimal validValue = 0;
            try
            {
                if (decimal.TryParse(GeoCode.ToString(), out validValue))
                {
                    if (validValue > 80 || validValue < -180)
                        validValue = 0;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return validValue;
        }
        private string ValidateTimeValue(object TimeObj, string DefValue)
        {
            string validValue = DefValue;
            try
            {
                if (TimeObj.ToString().Length >= 5 && TimeObj.ToString().Contains(":"))
                {
                    string[] parts = TimeObj.ToString().Split(new char[] { ':' });
                    if (parts.Length >= 2)
                    {
                        int hour = 0;
                        int minute = 0;
                        if (int.TryParse(parts[0], out hour) && int.TryParse(parts[1], out minute))
                        {
                            if (hour >= 0 && hour < 24 && minute >= 0 && minute < 60)
                                validValue = hour.ToString().PadLeft(2, '0') + ':' + minute.ToString().PadLeft(2, '0');
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return validValue;
        }
        private decimal ValidateDecimalValue(object DecObj, bool Unsigned)
        {
            decimal validValue = 0;
            try
            {
                decimal.TryParse(DecObj.ToString(), out validValue);
                if (Unsigned)
                    validValue = Math.Abs(validValue);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return validValue;
        }
        private int ValidateIntegerValue(object IntObj)
        {
            int validValue = 0;
            try
            {
                int.TryParse(IntObj.ToString(), out validValue);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return validValue;
        }        
        private string GetTruncatedString(string input, int length, bool truncateBegining)
        {
            string output = "";
            try
            {
                output = input;
                if (input.Length > length)
                {
                    if (truncateBegining)
                        output = input.Substring(input.Length - length, length);
                    else
                        output = input.Substring(0, length);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return output;
        }
    }
}
