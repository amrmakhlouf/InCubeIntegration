using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmImportTargets : Form
    {
        BackgroundWorker bgw = new BackgroundWorker();
        string SelectedSheet = "";
        int page = 1;
        int maxPage = 1;
        DataTable dtAllResults = new DataTable();
        DataTable dtErrors = new DataTable();
        DataTable dtResults = new DataTable();
        int year, month;
        TargetsManager targetsManager;
        ExcelManager excelManager;

        public frmImportTargets(int organizationID)
        {
            try
            {
                InitializeComponent();

                targetsManager = new TargetsManager(organizationID);
                targetsManager.WriteMessageHandler += new TargetsManager.WriteMessageDel(WriteMessage);
                if (CoreGeneral.Common.GeneralConfigurations.OrganizationOriented && CoreGeneral.Common.OrganizationConfigurations.ContainsKey(organizationID))
                {
                    this.BackColor = CoreGeneral.Common.OrganizationConfigurations[organizationID].IntegrationFormBackColor;
                }
                else
                {
                    this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                }
                lblStatus.BackColor = this.BackColor;
                bgw.DoWork += bgw_DoWork;
                bgw.RunWorkerCompleted += bgw_RunWorkerCompleted;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void frmImportTarget_Load(object sender, EventArgs e)
        {
            try
            {
                int currentmonth = DateTime.Now.Month;
                int currentyear = DateTime.Now.Year;
                int nextmonth = (currentmonth % 12) + 1;
                int nextyear = currentyear;
                if (nextmonth == 1)
                    nextyear++;

                cmbMonth.Items.Add(currentmonth);
                cmbMonth.Items.Add(nextmonth);
                cmbYear.Items.Add(currentyear);
                if (nextyear != currentyear)
                    cmbYear.Items.Add(nextyear);

                cmbMonth.SelectedIndex = 0;
                cmbYear.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void WriteMessage(string Message)
        {
            lblStatus.Text = Message;
        }
        void bgw_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                if (targetsManager.ImportTargets(SelectedSheet, year, month, excelManager.ExcelConnection) == Result.Success)
                {
                    dtAllResults = targetsManager.RetrieveResults();
                    if (dtAllResults.Rows.Count > 0)
                    {
                        dtAllResults.DefaultView.RowFilter = "Result <> 'Success'";
                        dtErrors = dtAllResults.DefaultView.ToTable();

                        DataColumn colID = new DataColumn("ID", typeof(int));
                        dtAllResults.Columns.Add(colID);
                        for (int i = 0; i < dtAllResults.Rows.Count; i++)
                        {
                            dtAllResults.Rows[i]["ID"] = i + 1;
                        }
                        colID.SetOrdinal(0);

                        DataColumn colID2 = new DataColumn("ID", typeof(int));
                        dtErrors.Columns.Add(colID2);
                        for (int i = 0; i < dtErrors.Rows.Count; i++)
                        {
                            dtErrors.Rows[i]["ID"] = i + 1;
                        }
                        colID2.SetOrdinal(0);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        void bgw_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            try
            {
                page = 1;
                maxPage = (int)Math.Ceiling((decimal)dtAllResults.Rows.Count / 1000);
                dtResults = dtAllResults;
                dtResults.DefaultView.RowFilter = "ID >= 1 AND ID <= 1000";
                dtResults.DefaultView.Sort = "ID";
                dgvResults.DataSource = dtResults.DefaultView;
                txtPageNo.Text = "1-1000 / " + dtResults.Rows.Count;
                txtPageNo.Enabled = true;
                btnNext.Enabled = true;
                btnPrevious.Enabled = true;
                cbShowErrorsOnly.Enabled = true;
                cmbYear.Enabled = true;
                cmbMonth.Enabled = true;
                btnLoadTargets.Enabled = true;
                lblStatus.Text = "";
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            try
            {
                if (page < maxPage)
                {
                    page++;
                    dtResults.DefaultView.RowFilter = string.Format("ID >= {0} AND ID <= {1}", (page - 1) * 1000 + 1, Math.Min(page * 1000, dtResults.Rows.Count));
                    txtPageNo.Text = string.Format("{0}-{1} / {2}", (page - 1) * 1000 + 1, Math.Min(page * 1000, dtResults.Rows.Count), dtResults.Rows.Count);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void cbShowErrorsOnly_Click(object sender, EventArgs e)
        {
            try
            {
                if (cbShowErrorsOnly.Checked)
                {
                    dtResults = dtErrors;
                }
                else
                {
                    dtResults = dtAllResults;
                }
                dtResults.DefaultView.RowFilter = "ID >= 1 AND ID <= 1000";
                dtResults.DefaultView.Sort = "ID";
                page = 1;
                maxPage = (int)Math.Ceiling((decimal)dtResults.Rows.Count / 1000);
                txtPageNo.Text = "1-1000 / " + dtResults.Rows.Count;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void btnPrevious_Click(object sender, EventArgs e)
        {
            try
            {
                if (page > 1)
                {
                    page--;
                    dtResults.DefaultView.RowFilter = string.Format("ID >= {0} AND ID <= {1}", (page - 1) * 1000 + 1, Math.Min(page * 1000, dtResults.Rows.Count));
                    txtPageNo.Text = string.Format("{0}-{1} / {2}", (page - 1) * 1000 + 1, Math.Min(page * 1000, dtResults.Rows.Count), dtResults.Rows.Count);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void btnLoadTargets_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Select targets excel sheet";
                ofd.FileName = "";
                ofd.Filter = "Excel files (*.xls,*.xlsx)|*.xls;*.xlsx";
                if (ofd.ShowDialog() == DialogResult.OK && !ofd.FileName.Equals(string.Empty))
                {
                    txtExcelPath.Text = ofd.FileName;
                    Result res;
                    DataTable AllSheets = new DataTable();
                    excelManager = new ExcelManager();
                    excelManager.ExcelPath = ofd.FileName;
                    res = excelManager.GetSheets(ref AllSheets);
                    switch (res)
                    {
                        case Result.Failure:
                            MessageBox.Show("Error in opening excel file !!");
                            break;
                        case Result.NoRowsFound:
                            MessageBox.Show("Selected file has no sheets !!");
                            break;
                        case Result.Success:
                            if (AllSheets.Rows.Count == 1)
                            {
                                SelectedSheet = AllSheets.Rows[0]["TABLE_NAME"].ToString();
                            }
                            else
                            {
                                frmSelectSheet frm = new frmSelectSheet(AllSheets);
                                frm.ShowDialog();
                                SelectedSheet = frm.SelectedSheet;
                            }

                            btnLoadTargets.Enabled = false;
                            txtPageNo.Clear();
                            dgvResults.DataSource = null;
                            txtPageNo.Enabled = false;
                            btnNext.Enabled = false;
                            btnPrevious.Enabled = false;
                            cbShowErrorsOnly.Enabled = false;
                            cmbYear.Enabled = false;
                            cmbMonth.Enabled = false;
                            year = int.Parse(cmbYear.Text);
                            month = int.Parse(cmbMonth.Text);
                            btnImport.Enabled = false;
                            lblStatus.Visible = true;
                            bgw.RunWorkerAsync();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}