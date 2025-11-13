using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using InCubeIntegration_BL;
using InCubeLibrary;

namespace InCubeIntegration_UI
{
    public partial class frmRoadNetRegions : Form
    {
        RoadNetManager RNManager = new RoadNetManager(false, null);
        public frmRoadNetRegions()
        {
            InitializeComponent();
            this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
        }

        private void frmRoadNetRegions_Load(object sender, EventArgs e)
        {
            try
            {
                DataTable dtRegions = new DataTable();
                RNManager.GetDefinedRegionsInSonic(ref dtRegions);
                grdRegions.DataSource = dtRegions;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void grdRegions_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void grdRegions_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (grdRegions.SelectedRows.Count > 0)
                {
                    txtRegionName.Text = grdRegions.SelectedRows[0].Cells[0].Value.ToString();
                    btnAdd.Enabled = false;
                    btnEdit.Enabled = true;
                    btnDelete.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {

        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            try
            {
                txtRegionName.ReadOnly = false;
                btnEdit.Text = "Apply";
                btnAdd.Enabled = false;
                btnDelete.Enabled = false;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
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
