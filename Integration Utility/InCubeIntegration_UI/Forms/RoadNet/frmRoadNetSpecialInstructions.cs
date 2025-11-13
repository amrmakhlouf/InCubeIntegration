using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmRoadNetSpecialInstructions : Form
    {
        RoadNetManager RNManager = new RoadNetManager(false, null);
        DataTable dtSpecialInstructions = new DataTable();
        private enum GridColumns
        {
            CustomerID = 0,
            OutletID = 1,
            CustomerCode = 2,
            OutletCode = 3,
            SpecialInstructions = 4,
            PreSP = 5
        }
        public frmRoadNetSpecialInstructions()
        {
            InitializeComponent();
            this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
        }

        private void frmRoadNetSpecialInstructions_Load(object sender, EventArgs e)
        {
            LoadSpecialInstructions();
        }
        private void LoadSpecialInstructions()
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                RNManager.GetSpecialInstructions(ref dtSpecialInstructions);
                grdRoutes.DataSource = dtSpecialInstructions.DefaultView;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtSearch.Text.Trim() != "")
                    dtSpecialInstructions.DefaultView.RowFilter = string.Format("CustomerCode LIKE '%{0}%' OR OutletCode LIKE '%{0}%' OR SpecialInstructions LIKE '%{0}%'", txtSearch.Text.Trim());
                else
                    dtSpecialInstructions.DefaultView.RowFilter = "";
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
                Dictionary<string, string> SpecialInstructions = new Dictionary<string, string>();
                for (int i = 0; i < dtSpecialInstructions.Rows.Count; i++)
                {
                    if (dtSpecialInstructions.Rows[i]["SpecialInstructions"].ToString() != dtSpecialInstructions.Rows[i]["PreSP"].ToString())
                    {
                        SpecialInstructions.Add(dtSpecialInstructions.Rows[i]["CustomerID"].ToString() + ":" + dtSpecialInstructions.Rows[i]["OutletID"].ToString(), dtSpecialInstructions.Rows[i]["SpecialInstructions"].ToString());
                    }
                }
                if (SpecialInstructions.Count > 0)
                {
                    if (RNManager.UpdateSpecialInstructions(SpecialInstructions) == Result.Success)
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

        private void btnLoadFromExcel_Click(object sender, EventArgs e)
        {
            try
            {
                int ExcelImportTypeID = -1;
                if (RNManager.GetSpecialInstructionsExcelImportType(ref ExcelImportTypeID) == Result.Success)
                {
                    frmImportExcel frm = new frmImportExcel(ExcelImportTypeID);
                    frm.ShowDialog();
                    if (frm.ImportResult == Result.Success)
                        LoadSpecialInstructions();
                }
                else
                    MessageBox.Show("Excel import not defined!!");
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}
