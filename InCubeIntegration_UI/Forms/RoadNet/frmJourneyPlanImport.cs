using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.Data;
using System.Windows.Forms;


namespace InCubeIntegration_UI
{
    public partial class frmJourneyPlanImport : Form
    {
        int TriggerID = -1;
        ExecutionManager execManager;
        RoadNetManager RNManager;
        IntegrationBase integrationObj;
        public bool RN_Conn_Opened = false;
        DataTable dtJourneyPlan;
        public frmJourneyPlanImport()
        {
            InitializeComponent();
            this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
            execManager = new ExecutionManager();
            integrationObj = new IntegrationBase(execManager);
            RNManager = new RoadNetManager(true, integrationObj);
            RN_Conn_Opened = RNManager.RN_Conn_Opened;
        }

        private void frmJourneyPlanImport_Load(object sender, EventArgs e)
        {
            try
            {
                DataTable dtRegions = new DataTable();
                RNManager.GetDefinedRegionsForEmployee(ref dtRegions);
                cmbRegion.DataSource = dtRegions;
                cmbRegion.DisplayMember = "RegionName";
                cmbRegion.ValueMember = "RegionID";
                if (dtRegions.Rows.Count > 0)
                    cmbRegion_SelectedIndexChanged(this, null);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);

            }
        }

        private void cmbRegion_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cmbRegion.ValueMember != "" && cmbRegion.SelectedValue != null)
                {
                    DataTable dtSessions = new DataTable();
                    RNManager.GetTPSessionsForRegion(cmbRegion.SelectedValue.ToString(), ref dtSessions);
                    cmbSessions.DataSource = dtSessions;
                    cmbSessions.DisplayMember = "SessionName";
                    cmbSessions.ValueMember = "SessionName";
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);

            }
        }

        private void btnRetrieve_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbSessions.SelectedValue == null)
                {
                    MessageBox.Show("Select session!!");
                    return;
                }
                dtJourneyPlan = new DataTable();
                if (RNManager.GetTPSessionDetails(cmbSessions.SelectedValue.ToString(), ref dtJourneyPlan) != Result.Success)
                {
                    MessageBox.Show("Error retrieving session details..");
                }
                else
                {
                    grdJourneyPlan.DataSource = dtJourneyPlan;
                    btnImport.Enabled = dtJourneyPlan.Rows.Count > 0;
                }
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
                    frmRoadNetIntegrationExecution frm = new frmRoadNetIntegrationExecution(frmRoadNetIntegrationExecution.Mode.ImportJourneyPlan);
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
                return RNManager.ExportTPSessionDetailsToSonic(dtJourneyPlan);
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
                Procedure Proc = new Procedure("sp_AddExtraColumnsToJourneyPlan");
                Res = integrationObj.ExecuteStoredProcedure(Proc);
                if (Res == Result.Success)
                {
                    Proc = new Procedure("sp_ProcessJourneyPlan");
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
        private Result FetchImportResults(ref DataTable dtResults)
        {
            return RNManager.GetJourneyPlanImportResults(TriggerID, ref dtResults);
        }
    }
}
