using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.Data;
using System.Windows.Forms;


namespace InCubeIntegration_UI
{
    public partial class frmRoadNetImport : Form
    {
        int TriggerID = -1;
        ExecutionManager execManager;
        RoadNetManager RNManager;
        IntegrationBase integrationObj;
        public bool RN_Conn_Opened = false;
        public frmRoadNetImport()
        {
            InitializeComponent();
            this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
            execManager = new ExecutionManager();
            integrationObj = new IntegrationBase(execManager);
            RNManager = new RoadNetManager(true, integrationObj);
            RN_Conn_Opened = RNManager.RN_Conn_Opened;
        }

        private void FillRoadNetSessions()
        {
            try
            {
                DataTable dtSession = new DataTable();
                dtSession.Columns.Add("REGION_ID");
                dtSession.Columns.Add("DESCRIPTION");
                RNManager.GetRoadNetSessions(dtpSessionDate.Value.Date, ref dtSession);
                cmbSessions.DataSource = dtSession;
                cmbSessions.DisplayMember = "DESCRIPTION";
                cmbSessions.ValueMember = "DESCRIPTION";
                cmbRegion.DataSource = dtSession;
                cmbRegion.DisplayMember = "REGION_ID";
                cmbRegion.ValueMember = "REGION_ID";
                //DataTable dtSonicSessions = new DataTable();
                //dtSonicSessions.Columns.Add("SessionID");
                //dtSonicSessions.Columns.Add("SessionName");
                //RNManager.GetLastNSonicSessions(100, ref dtSonicSessions);
                //cmbSonicSessions.DataSource = dtSession;
                //cmbSonicSessions.DisplayMember = "SessionName";
                //cmbSonicSessions.ValueMember = "SessionID";
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void FillSonicSessions()
        {
            try
            {
                //DataTable dtSonicSessions = new DataTable();
                //dtSonicSessions.Columns.Add("SessionID");
                //dtSonicSessions.Columns.Add("SessionName");
                //RNManager.GetLastNSonicSessions(100, ref dtSonicSessions);
                //cmbSonicSessions.DataSource = dtSonicSessions;
                //cmbSonicSessions.DisplayMember = "SessionName";
                //cmbSonicSessions.ValueMember = "SessionID";
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void frmRoadNetImport_Load(object sender, EventArgs e)
        {
            try
            {
                FillRoadNetSessions();
                //FillSonicSessions();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void dtpSessionDate_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                FillRoadNetSessions();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void cbAllSessions_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                cmbSessions.Enabled = !cbAllSessions.Checked;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            TriggerID = -1;
            try
            {
                TriggerID = execManager.LogActionTriggerBegining(-1, -1, IntegrationField.RoadNetImport_U.GetHashCode());
                if (TriggerID != -1)
                {
                    frmRoadNetIntegrationExecution frm = new frmRoadNetIntegrationExecution(frmRoadNetIntegrationExecution.Mode.ImportOrders);
                    frm.RetrieveRoadNetSessionDetailsHandler += new frmRoadNetIntegrationExecution.RetrieveRoadNetSessionDetailsDel(RetrieveRoadNetSessionDetails);
                    frm.ProcessSessionDetailsHandler += new frmRoadNetIntegrationExecution.ProcessSessionDetailsDel(ProcessSessionDetails);
                    frm.FetchImportResultsHandler += new frmRoadNetIntegrationExecution.FetchImportResultsDel(FetchImportResults);
                    frm.ShowDialog();
                }

            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                execManager.LogActionTriggerEnding(TriggerID);
            }
        }

        private Result RetrieveRoadNetSessionDetails()
        {
            try
            {
                return RNManager.ExportSessionDetailsToSonic(dtpSessionDate.Value.Date, cbAllSessions.Checked ? "-1" : cmbSessions.SelectedValue.ToString(), cbAllSessions.Checked ? "-1" : cmbRegion.SelectedValue.ToString());
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
        }
        private Result ProcessSessionDetails()
        {
            Result Res = Result.UnKnown;
            try
            {
                Procedure Proc = new Procedure("sp_AddExtraColumnsToRoadNetSessions");
                Res = integrationObj.ExecuteStoredProcedure(Proc);
                if (Res == Result.Success)
                {
                    Proc = new Procedure("sp_ProcessRoadNetSession");
                    Proc.AddParameter("@SessionDate", ParamType.DateTime, dtpSessionDate.Value);
                    Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID);
                    Res = integrationObj.ExecuteStoredProcedure(Proc);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                Res = Result.Failure;
            }
            return Res;
        }
        private Result FetchImportResults (ref DataTable dtResults)
        {
            return RNManager.GetImportResults(dtpSessionDate.Value.Date, ref dtResults);
        }
        private void cbNewSession_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                //if (cbNewSession.Checked)
                //{
                //    txtSessionName.Enabled = true;
                //    txtSessionName.Focus();
                //    txtSessionName.SelectAll();
                //    cmbSonicSessions.Enabled = false;
                //    btnAddSession.Enabled = true;
                //    btnContinue.Enabled = false;
                //}
                //else
                //{
                //    txtSessionName.Clear();
                //    FillSonicSessions();
                //    txtSessionName.Enabled = false;
                //    cmbSonicSessions.Enabled = true;
                //    btnAddSession.Enabled = false;
                //    btnContinue.Enabled = cmbSonicSessions.SelectedValue != null;
                //}
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void btnAddSession_Click(object sender, EventArgs e)
        {
            try
            {
                //if (txtSessionName.Text.Trim() == string.Empty)
                //{
                //    MessageBox.Show("Enter session name!!");
                //    return;
                //}
                //RNManager.AddNewSonicSession(txtSessionName.Text);
                //cbNewSession.Checked = false;
                //FillSonicSessions();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void btnContinue_Click(object sender, EventArgs e)
        {
            try
            {
                //frmRoadNetExport frm = new frmRoadNetExport(Convert.ToInt16(cmbSonicSessions.SelectedValue));
                //frm.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
    }
}
