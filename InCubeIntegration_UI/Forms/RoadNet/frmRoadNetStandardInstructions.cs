using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmRoadNetStandardInstructions : Form
    {
        RoadNetManager RNManager = new RoadNetManager(false, null);
        DataTable dtStandardInstructions = new DataTable();
        private enum GridColumns
        {
            CustomerID = 0,
            OutletID = 1,
            CustomerCode = 2,
            OutletCode = 3,
            StandardInstructions = 4,
            PreSP = 5
        }
        public frmRoadNetStandardInstructions()
        {
            InitializeComponent();
            this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
        }

        private void frmRoadNetStandardInstructions_Load(object sender, EventArgs e)
        {
            LoadStandardInstructions();
        }
        private void LoadStandardInstructions()
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                RNManager.GetStandardInstructions(ref dtStandardInstructions);
                grdRoutes.DataSource = dtStandardInstructions.DefaultView;
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
                    dtStandardInstructions.DefaultView.RowFilter = string.Format("CustomerCode LIKE '%{0}%' OR OutletCode LIKE '%{0}%' OR StandardInstructions LIKE '%{0}%'", txtSearch.Text.Trim());
                else
                    dtStandardInstructions.DefaultView.RowFilter = "";
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
                Dictionary<string, string> StandardInstructions = new Dictionary<string, string>();
                for (int i = 0; i < dtStandardInstructions.Rows.Count; i++)
                {
                    if (dtStandardInstructions.Rows[i]["StandardInstructions"].ToString() != dtStandardInstructions.Rows[i]["PreSP"].ToString())
                    {
                        StandardInstructions.Add(dtStandardInstructions.Rows[i]["CustomerID"].ToString() + ":" + dtStandardInstructions.Rows[i]["OutletID"].ToString(), dtStandardInstructions.Rows[i]["StandardInstructions"].ToString());
                    }
                }
                if (StandardInstructions.Count > 0)
                {
                    if (RNManager.UpdateStandardInstructions(StandardInstructions) == Result.Success)
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
                if (RNManager.GetStandardInstructionsExcelImportType(ref ExcelImportTypeID) == Result.Success)
                {
                    frmImportExcel frm = new frmImportExcel(ExcelImportTypeID);
                    frm.ShowDialog();
                    if (frm.ImportResult == Result.Success)
                        LoadStandardInstructions();
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
