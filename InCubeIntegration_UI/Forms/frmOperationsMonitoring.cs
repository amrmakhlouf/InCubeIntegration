using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using InCubeLibrary;
using InCubeIntegration_BL;

namespace InCubeIntegration_UI
{
    public partial class frmOperationsMonitoring : Form
    {
        ExecutionManager execManager;
        DataTable dtSummary, dtDetails; //dtCurrentTable;
        private class ResultsSummary
        {
            public long FieldID;
            public long MinTriggerID;
            public long MaxTriggerID;
            public long Count;
            public string Duration;
            public string FieldName;
        }
        public frmOperationsMonitoring()
        {
            InitializeComponent();
            execManager = new ExecutionManager();
        }

        private void frmOperationsMonitoring_Load(object sender, EventArgs e)
        {
            try
            {
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                dtpFromDate.Value = DateTime.Today;
                dtpToDate.Value = DateTime.Today.AddDays(1).AddMinutes(-1);
                DataTable dtFields = new DataTable();
                dtFields.Columns.Add("FieldID", typeof(int));
                dtFields.Columns.Add("FieldName", typeof(string));
                foreach (FieldItem f in CoreGeneral.Common.userPrivileges.UpdateFieldsAccess.Values)
                {
                    dtFields.Rows.Add(new string[] { f.FieldID.ToString(), "Update " + f.Description });
                }
                foreach (FieldItem f in CoreGeneral.Common.userPrivileges.SendFieldsAccess.Values)
                {
                    dtFields.Rows.Add(new string[] { f.FieldID.ToString(), "Send " + f.Description });
                }
                foreach (FieldItem f in CoreGeneral.Common.userPrivileges.SpecialFunctionsAccess.Values)
                {
                    dtFields.Rows.Add(new string[] { f.FieldID.ToString(), "Special function [" + f.Description + "]" });
                }
                cmbField.DataSource = dtFields;
                cmbField.DisplayMember = "FieldName";
                cmbField.ValueMember = "FieldID";
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            try
            {
                FillSummaryTable();
                btnBack.Visible = false;
                btnFind.Enabled = true;
                dtpFromDate.Enabled = true;
                dtpToDate.Enabled = true;
                cmbField.Enabled = !cbAllFields.Checked;
                cbAllFields.Enabled = true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void lsvResults_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                ResultsSummary S = (ResultsSummary)lsvResults.SelectedItems[0].Tag;
                dtDetails = execManager.GetFieldTriggersList(S.FieldID, S.MinTriggerID, S.MaxTriggerID);
                FillDetailsTable();
                lblLines.Text = "Lines: " + S.Count.ToString();
                lblDuration.Text = "Total Duration: " + S.Duration;
                lblView.Text = "View: Details of (" + S.FieldName + ")";
                btnBack.Visible = true;
                btnFind.Enabled = false;
                dtpFromDate.Enabled = false;
                dtpToDate.Enabled = false;
                cmbField.Enabled = false;
                cbAllFields.Enabled = false;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void FillSummaryTable()
        {
            try
            {
                lsvResults.Clear();
                lsvResults.Columns.Add("Field", 150);
                lsvResults.Columns.Add("Count");
                lsvResults.Columns.Add("Min", 80);
                lsvResults.Columns.Add("Max", 80);
                lsvResults.Columns.Add("Average", 80);
                lsvResults.Columns.Add("Total", 80);
                ListViewItem lsvItem;
                TimeSpan totalDuration = new TimeSpan(0, 0, 0);
                for (int i = 0; i < dtSummary.Rows.Count; i++)
                {
                    totalDuration = totalDuration.Add(new TimeSpan(0, 0, Convert.ToInt32(dtSummary.Rows[i]["TotalSeconds"])));
                    ResultsSummary S = new ResultsSummary();
                    S.FieldID = Convert.ToInt64(dtSummary.Rows[i]["FieldID"]);
                    S.MaxTriggerID = Convert.ToInt64(dtSummary.Rows[i]["MaxTriggerID"]);
                    S.MinTriggerID = Convert.ToInt64(dtSummary.Rows[i]["MinTriggerID"]);
                    S.Count = Convert.ToInt64(dtSummary.Rows[i]["Count"]);
                    S.Duration = dtSummary.Rows[i]["Total"].ToString();
                    S.FieldName = dtSummary.Rows[i]["Field"].ToString();
                    lsvItem = new ListViewItem(dtSummary.Rows[i]["Field"].ToString());
                    lsvItem.SubItems.Add(dtSummary.Rows[i]["Count"].ToString());
                    lsvItem.SubItems.Add(dtSummary.Rows[i]["Min"].ToString());
                    lsvItem.SubItems.Add(dtSummary.Rows[i]["Max"].ToString());
                    lsvItem.SubItems.Add(dtSummary.Rows[i]["Avg"].ToString());
                    lsvItem.SubItems.Add(dtSummary.Rows[i]["Total"].ToString());
                    lsvItem.Tag = S;
                    lsvResults.Items.Add(lsvItem);
                }
                lblLines.Text = "Lines: " + lsvResults.Items.Count.ToString();
                lblDuration.Text = "Total Duration: " + ((int)totalDuration.TotalHours).ToString() + ":" + totalDuration.Minutes.ToString().PadLeft(2, '0') + ":" + totalDuration.Seconds.ToString().PadLeft(2, '0');
                lblView.Text = "View: Summary";
                btnExport.Visible = lsvResults.Items.Count > 0;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void FillDetailsTable()
        {
            try
            {
                lsvResults.Clear();
                lsvResults.Columns.Add("TriggerID", 60);
                lsvResults.Columns.Add("Mode", 70);
                lsvResults.Columns.Add("User", 120);
                lsvResults.Columns.Add("Start", 120);
                lsvResults.Columns.Add("End", 120);
                lsvResults.Columns.Add("Duration", 80);
                lsvResults.Columns.Add("TotalRows", 80);
                ListViewItem lsvItem;
                for (int i = 0; i < dtDetails.Rows.Count; i++)
                {
                    lsvItem = new ListViewItem(dtDetails.Rows[i]["TriggerID"].ToString());
                    lsvItem.SubItems.Add(dtDetails.Rows[i]["Mode"].ToString());
                    lsvItem.SubItems.Add(dtDetails.Rows[i]["User"].ToString());
                    lsvItem.SubItems.Add(dtDetails.Rows[i]["Start"].ToString());
                    lsvItem.SubItems.Add(dtDetails.Rows[i]["End"].ToString());
                    lsvItem.SubItems.Add(dtDetails.Rows[i]["Duration"].ToString());
                    lsvItem.SubItems.Add(dtDetails.Rows[i]["TotalRows"].ToString());
                    lsvItem.Tag = dtDetails.Rows[i]["TriggerID"];
                    lsvResults.Items.Add(lsvItem);
                }
                btnExport.Visible = lsvResults.Items.Count > 0;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void btnFind_Click(object sender, EventArgs e)
        {
            try
            {
                dtSummary = execManager.GetAllFieldExecutionSummary(dtpFromDate.Value, dtpToDate.Value, cbAllFields.Checked ? -1 : Convert.ToInt32(cmbField.SelectedValue));
                FillSummaryTable();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void cbAllFields_CheckedChanged(object sender, EventArgs e)
        {
            cmbField.Enabled = !cbAllFields.Checked;
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Excel Files (*.xlsx)|*.xlsx";
                sfd.DefaultExt = "xlsx";
                sfd.AddExtension = true;
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    DataSet ds = new DataSet("ds");
                    DataTable dtData = new DataTable();
                    foreach (ColumnHeader col in lsvResults.Columns)
                    {
                        dtData.Columns.Add(col.Text);
                    }
                    foreach (ListViewItem lsvitem in lsvResults.Items)
                    {
                        DataRow dr = dtData.NewRow();
                        for (int i = 0; i < lsvitem.SubItems.Count; i++)
                        {
                            dr[i] = lsvitem.SubItems[i].Text;
                        }
                        dtData.Rows.Add(dr);
                    }
                    ds.Tables.Add(dtData);
                    ExcelManager.CreateExcelDocument(ds, sfd.FileName);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}
