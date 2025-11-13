using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmRouteRegion : Form
    {
        RoadNetManager RNManager = new RoadNetManager(false, null);
        DataTable dtRouteRegion = new DataTable();
        private enum GridColumns
        {
            TerritoryID = 0,
            TerritoryCode = 1,
            Region = 2,
            PreRegion = 3
        }
        public frmRouteRegion()
        {
            InitializeComponent();
            this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
        }
        private void GetRegions()
        {
            try
            {
                DataTable dtRegions = new DataTable();
                RNManager.GetDefinedRegionsInSonic(ref dtRegions);
                DataGridViewComboBoxColumn col = (DataGridViewComboBoxColumn)grdRoutes.Columns[GridColumns.Region.GetHashCode()];
                col.DataSource = dtRegions;
                col.DisplayMember = "Region";
                col.ValueMember = "Region";
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void frmRouteRegion_Load(object sender, EventArgs e)
        {
            try
            {
                GetRegions();
                RNManager.GetRouteRegions(ref dtRouteRegion);
                grdRoutes.DataSource = dtRouteRegion.DefaultView;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtSearch.Text.Trim() != "")
                    dtRouteRegion.DefaultView.RowFilter = string.Format("TerritoryCode LIKE '%{0}%' OR Region LIKE '%{0}%'", txtSearch.Text.Trim());
                else
                    dtRouteRegion.DefaultView.RowFilter = "";
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                Dictionary<int, string> RouteRegion = new Dictionary<int, string>();
                for (int i = 0; i < dtRouteRegion.Rows.Count; i++)
                {
                    if (dtRouteRegion.Rows[i]["Region"].ToString() != dtRouteRegion.Rows[i]["PreRegion"].ToString())
                    {
                        RouteRegion.Add(Convert.ToInt16(dtRouteRegion.Rows[i]["TerritoryID"]), dtRouteRegion.Rows[i]["Region"].ToString());
                    }
                }
                if (RouteRegion.Count > 0)
                {
                    if (RNManager.UpdateRouteRegion(RouteRegion) == Result.Success)
                    {
                        MessageBox.Show("Saved successfully ..");
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Saving failed!!");
                    }
                }
                else
                {
                    MessageBox.Show("No changes!!");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnManageRegions_Click(object sender, EventArgs e)
        {
            try
            {
                frmRoadNetRegions frm = new frmRoadNetRegions();
                frm.ShowDialog();
                GetRegions();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}
