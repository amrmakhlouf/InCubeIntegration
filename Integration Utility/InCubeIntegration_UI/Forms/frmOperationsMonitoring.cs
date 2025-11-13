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
        DataTable dtSummary, dtDetails, dtCurrentTable;
        private class ResultsSummary
        {
            public long FieldID;
            public long MinTriggerID;
            public long MaxTriggerID;
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
                dtSummary = execManager.GetAllFieldExecutionSummary(dtpFromDate.Value, dtpToDate.Value);
                dtCurrentTable = dtSummary;
                lsvResults.Clear();
                lsvResults.Columns.Add("Field",150);
                lsvResults.Columns.Add("Count");
                lsvResults.Columns.Add("Min", 80);
                lsvResults.Columns.Add("Max", 80);
                lsvResults.Columns.Add("Average", 80);
                lsvResults.Columns.Add("Total", 80);
                ListViewItem lsvItem;
                for (int i = 0; i < dtSummary.Rows.Count; i++)
                {
                    ResultsSummary S = new ResultsSummary();
                    S.FieldID = Convert.ToInt64(dtSummary.Rows[i]["FieldID"]);
                    S.MaxTriggerID = Convert.ToInt64(dtSummary.Rows[i]["MaxTriggerID"]);
                    S.MinTriggerID = Convert.ToInt64(dtSummary.Rows[i]["MinTriggerID"]);
                    lsvItem = new ListViewItem(dtSummary.Rows[i]["Field"].ToString());
                    lsvItem.SubItems.Add(dtSummary.Rows[i]["Count"].ToString());
                    lsvItem.SubItems.Add(dtSummary.Rows[i]["Min"].ToString());
                    lsvItem.SubItems.Add(dtSummary.Rows[i]["Max"].ToString());
                    lsvItem.SubItems.Add(dtSummary.Rows[i]["Avg"].ToString());
                    lsvItem.SubItems.Add(dtSummary.Rows[i]["Total"].ToString());
                    lsvItem.Tag = S;
                    lsvResults.Items.Add(lsvItem);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}
