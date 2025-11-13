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
    public partial class frmPRNConfig : Form
    {
        ExecutionManager execManager;
        IntegrationBase integrationObj;
        RoadNetManager RNManager;
        DataTable dtIncludedColumns;
        DataTable dtOrders;
        private List<Color> colors = new List<Color>();
        public frmPRNConfig()
        {
            InitializeComponent();
            this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
        }

        private void frmPRNConfig_Load(object sender, EventArgs e)
        {
            try
            {
                execManager = new ExecutionManager();
                integrationObj = new IntegrationBase(execManager);
                RNManager = new RoadNetManager(true, integrationObj);
                colors.Add(Color.LightBlue);
                colors.Add(Color.LightCoral);
                colors.Add(Color.LightGreen);
                colors.Add(Color.LightSalmon);
                colors.Add(Color.Yellow);
                colors.Add(Color.Aqua);
                colors.Add(Color.Coral);
                colors.Add(Color.DimGray);
                colors.Add(Color.Cyan);
                colors.Add(Color.Gold);
                colors.Add(Color.GreenYellow);
                colors.Add(Color.Gainsboro);
                colors.Add(Color.LightSteelBlue);
                colors.Add(Color.Khaki);
                colors.Add(Color.LemonChiffon);
                colors.Add(Color.LightCyan);
                colors.Add(Color.LimeGreen);
                colors.Add(Color.Maroon);
                colors.Add(Color.MediumSpringGreen);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            try
            {
                dtOrders = new DataTable();
                if (RNManager.GetRoadNetOrdersTable(new DateTime(2100, 1, 1), new DateTime(2100, 1, 1), new DateTime(2100, 1, 1), new DateTime(2100, 1, 1), "", txtOrderID.Text.Trim(), 0, true, ref dtOrders) != Result.Success)
                {
                    MessageBox.Show("Failure in loading RoadNet orders structure!!");
                    this.Close();
                    return;
                }
                if (dtOrders.Rows.Count == 0)
                {
                    MessageBox.Show("Order number search didn't retrieve any results, try with other order number ..");
                    return;
                }
                dtIncludedColumns = new DataTable();
                if (RNManager.GetPRNConfig(ref dtIncludedColumns) !=  Result.Success)
                {
                    MessageBox.Show("Failure in loading PRN config!!");
                    this.Close();
                }

                for (int i = dtIncludedColumns.Rows.Count - 1; i >= 0; i--)
                {
                    if (!dtOrders.Columns.Contains(dtIncludedColumns.Rows[i]["ColumnName"].ToString()))
                    {
                        dtIncludedColumns.Rows.RemoveAt(i);
                    }
                }
                for (int i = 0; i < dtIncludedColumns.Rows.Count; i++)
                {
                    dtIncludedColumns.Rows[i]["Position"] = (i + 1).ToString();
                }

                foreach (DataColumn col in dtOrders.Columns)
                {
                    if (dtIncludedColumns.Select("ColumnName = '" + col.ColumnName + "'").Length == 0)
                        lbExcludedColumns.Items.Add(col.ColumnName);
                }

                grdIncludedColumns.DataSource = dtIncludedColumns;
                FillSample();

                pnl1.Visible = false;
                pnl2.Visible = true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void picUp_Click(object sender, EventArgs e)
        {
            try
            {
                if (grdIncludedColumns.SelectedRows.Count == 1)
                {
                    int selectedIndex = grdIncludedColumns.SelectedRows[0].Index;
                    if (selectedIndex > 0)
                    {
                        int currentSeq = selectedIndex + 1;
                        dtIncludedColumns.Rows[selectedIndex - 1]["Position"] = currentSeq.ToString();
                        DataRow dr = dtIncludedColumns.NewRow();
                        dr["columnName"] = dtIncludedColumns.Rows[selectedIndex]["columnName"];
                        dr["Position"] = (currentSeq - 1).ToString();
                        dr["Width"] = dtIncludedColumns.Rows[selectedIndex]["Width"];
                        dtIncludedColumns.Rows.RemoveAt(selectedIndex);
                        dtIncludedColumns.Rows.InsertAt(dr, selectedIndex - 1);
                        grdIncludedColumns.ClearSelection();
                        grdIncludedColumns.Rows[selectedIndex - 1].Selected = true;
                        FillSample();
                    }
                }
                else
                {
                    if (grdIncludedColumns.SelectedRows.Count == 0)
                        MessageBox.Show("Select an item to move up");
                    if (grdIncludedColumns.SelectedRows.Count > 1)
                        MessageBox.Show("Select one item only to move up");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void PicDown_Click(object sender, EventArgs e)
        {
            try
            {
                if (grdIncludedColumns.SelectedRows.Count == 1)
                {
                    int selectedIndex = grdIncludedColumns.SelectedRows[0].Index;
                    if (selectedIndex < grdIncludedColumns.Rows.Count - 1)
                    {
                        int currentSeq = selectedIndex + 1;

                        dtIncludedColumns.Rows[selectedIndex + 1]["Position"] = (currentSeq - 1).ToString();
                        DataRow dr = dtIncludedColumns.NewRow();
                        dr["columnName"] = dtIncludedColumns.Rows[selectedIndex]["columnName"];
                        dr["Position"] = (currentSeq + 1).ToString();
                        dr["Width"] = dtIncludedColumns.Rows[selectedIndex]["Width"];
                        dtIncludedColumns.Rows.RemoveAt(selectedIndex);
                        dtIncludedColumns.Rows.InsertAt(dr, selectedIndex + 1);
                        grdIncludedColumns.ClearSelection();
                        grdIncludedColumns.Rows[selectedIndex + 1].Selected = true;
                        FillSample();
                    }
                }
                else
                {
                    if (grdIncludedColumns.SelectedRows.Count == 0)
                        MessageBox.Show("Select an item to move up");
                    if (grdIncludedColumns.SelectedRows.Count > 1)
                        MessageBox.Show("Select one item only to move up");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void btnExclude_Click(object sender, EventArgs e)
        {
            try
            {
                string ColumnName = grdIncludedColumns.SelectedRows[0].Cells[0].Value.ToString();
                dtIncludedColumns.Rows.RemoveAt(grdIncludedColumns.SelectedRows[0].Index);

                for (int i = 0; i < dtIncludedColumns.Rows.Count; i++)
                {
                    dtIncludedColumns.Rows[i]["Position"] = (i + 1).ToString();
                }

                lbExcludedColumns.Items.Add(ColumnName);
                FillSample();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void btnInclude_Click(object sender, EventArgs e)
        {
            try
            {
                DataRow dr = dtIncludedColumns.NewRow();
                dr["ColumnName"] = lbExcludedColumns.SelectedItem.ToString();
                dr["Position"] = dtIncludedColumns.Rows.Count + 1;
                dr["Width"] = 25;
                dtIncludedColumns.Rows.Add(dr);
                lbExcludedColumns.Items.Remove(lbExcludedColumns.SelectedItem);
                grdIncludedColumns.ClearSelection();
                if (lbExcludedColumns.Items.Count > 0)
                    lbExcludedColumns.SelectedIndex = 0;
                FillSample();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (dtIncludedColumns.Rows.Count == 0)
                {
                    MessageBox.Show("No columns are added!!");
                    return;
                }
                for (int i = 0; i < dtIncludedColumns.Rows.Count; i++)
                {
                    int Width = 0;

                    if (!int.TryParse(dtIncludedColumns.Rows[0]["Width"].ToString(), out Width) || Width < 1)
                    {
                        MessageBox.Show("Invalid width");
                        grdIncludedColumns.ClearSelection();
                        grdIncludedColumns.Rows[i].Selected = true;
                    }
                }
                if (RNManager.SavePRNConfig(dtIncludedColumns) == Result.Success)
                {
                    MessageBox.Show("Saved successfully..");
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Saving failed!!");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void grdIncludedColumns_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (grdIncludedColumns.CurrentCell.ColumnIndex == 2)
            {
                grdIncludedColumns.CommitEdit(DataGridViewDataErrorContexts.Commit);
                FillSample();
            }
        }
        private void FillSample()
        {
            try
            {
                txtSample.Clear();
                for (int i = 0; i < dtIncludedColumns.Rows.Count; i++)
                {
                    string colName = dtIncludedColumns.Rows[i]["ColumnName"].ToString();
                    int Width = Convert.ToInt16(dtIncludedColumns.Rows[i]["Width"]);
                    string value = dtOrders.Rows[0][colName].ToString();
                    value = value.Substring(0, Math.Min(Width, value.Length)).PadRight(Width);
                    AppendText(value, colors[i % colors.Count]);
                    grdIncludedColumns.Rows[i].DefaultCellStyle.BackColor = colors[i % colors.Count];
                }
                AppendText("*",txtSample.BackColor);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        
        private void AppendText(string text, Color color)
        {
            try
            {
                txtSample.SelectionStart = txtSample.TextLength;
                txtSample.SelectionLength = 0;
                txtSample.SelectionBackColor = color;
                txtSample.AppendText(text);
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void txtOrderID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                btnOk_Click(sender, e);
        }

        private void grdIncludedColumns_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            try
            {
                e.Control.KeyPress -= new KeyPressEventHandler(Column1_KeyPress);
                TextBox tb = e.Control as TextBox;
                if (tb != null)
                {
                    tb.KeyPress += new KeyPressEventHandler(Column1_KeyPress);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void Column1_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (!char.IsDigit(e.KeyChar) && (Keys)e.KeyChar != Keys.Back)
                {
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void cbWrapText_CheckedChanged(object sender, EventArgs e)
        {
            txtSample.WordWrap = cbWrapText.Checked;
        }
    }
}
