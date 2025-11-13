using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using System.IO;

namespace InCubeIntegration_UI
{
    public partial class frmDesignInterface : Form
    {
        DataTable dtTypes = new DataTable();
        DataTable dtSheetsColumns = new DataTable();
        DataTable dtProcs = new DataTable();
        DataTable dtSheets = new DataTable();
        ExcelManager em = null;
        int selectedTypeID = 0;
        Dictionary<int, string> _columnsTypes = new Dictionary<int, string>();
        Dictionary<int, DataGridView> Grids = new Dictionary<int, DataGridView>();
        bool IgnoreCheckChange = false;
        public frmDesignInterface()
        {
            em = new ExcelManager();
            InitializeComponent();
        }

        private void frmDesignInterface_Load(object sender, EventArgs e)
        {
            try
            {
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                foreach (ColumnType type in Enum.GetValues(typeof(ColumnType)))
                {
                    _columnsTypes.Add(type.GetHashCode(), type.ToString());
                }

                em.GetDataTypes(true, -1, ref dtTypes, ref dtSheets, ref dtSheetsColumns, ref dtProcs);
                Dictionary<int, string> Types = new Dictionary<int, string>();
                foreach (DataRow dr in dtTypes.Rows)
                {
                    Types.Add(Convert.ToInt16(dr["ImportTypeID"]), dr["Name"].ToString());
                }
                Types.Add(0, "New ..");
                cmbImportType.DataSource = new BindingSource(Types, null);
                cmbImportType.DisplayMember = "value";
                cmbImportType.ValueMember = "key";
                cmbImportType.SelectedValue = 0;

                AddColumnsTab(1);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void ClearSheetsTabs()
        {
            try
            {
                for (int i = tabControl1.TabCount - 1; i > 2; i--)
                {
                    tabControl1.TabPages.RemoveAt(i);
                }
                foreach (int Key in new List<int>(Grids.Keys))
                {
                    if (Key > 1)
                        Grids.Remove(Key);
                    else
                        Grids[Key].Rows.Clear();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void cmbImportType_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cmbImportType.ValueMember == "")
                    return;

                txtDescription.Clear();
                txtImportType.Clear();
                txtProc1.Clear();
                txtProc2.Clear();
                txtProc3.Clear();
                if (cbSheet2.Checked)
                {
                    IgnoreCheckChange = true;
                    cbSheet2.Checked = false;
                }
                if (cbSheet3.Checked)
                {
                    IgnoreCheckChange = true;
                    cbSheet3.Checked = false;
                }
                txtSheet1Desc.Clear();
                txtSheet1Staging.Clear();
                txtSheet2Desc.Clear();
                txtSheet2Staging.Clear();
                txtSheet2Desc.ReadOnly = true;
                txtSheet2Staging.ReadOnly = true;
                txtSheet3Desc.Clear();
                txtSheet3Staging.Clear();
                txtSheet3Desc.ReadOnly = true;
                txtSheet3Staging.ReadOnly = true;
                cbSheet3.Enabled = false;
                ClearSheetsTabs();

                selectedTypeID = Convert.ToInt16(cmbImportType.SelectedValue.ToString());
                if (selectedTypeID == 0)
                {
                    btnExport.Visible = false;
                }
                else
                {
                    DataRow[] dr = dtTypes.Select("ImportTypeID = " + selectedTypeID);
                    txtImportType.Text = dr[0]["Name"].ToString();
                    txtDescription.Text = dr[0]["Description"].ToString();

                    dr = dtSheets.Select("ImportTypeID = " + selectedTypeID);
                    foreach (DataRow row in dr)
                    {
                        int SheetNo = Convert.ToInt16(row["SheetNo"]);
                        switch (SheetNo)
                        {
                            case 1:
                                txtSheet1Desc.Text = row["SheetDescription"].ToString();
                                txtSheet1Staging.Text = row["StagingTable"].ToString();
                                break;
                            case 2:
                                cbSheet2.Checked = true;
                                txtSheet2Desc.Text = row["SheetDescription"].ToString();
                                txtSheet2Staging.Text = row["StagingTable"].ToString();
                                AddColumnsTab(2);
                                break;
                            case 3:
                                cbSheet3.Checked = true;
                                txtSheet3Desc.Text = row["SheetDescription"].ToString();
                                txtSheet3Staging.Text = row["StagingTable"].ToString();
                                AddColumnsTab(3);
                                break;
                        }
                    }

                    dr = dtSheetsColumns.Select("ImportTypeID = " + selectedTypeID);
                    foreach (DataRow row in dr)
                    {
                        int SheetNo = Convert.ToInt16(row["SheetNo"]);
                        DataGridView grd = Grids[SheetNo];
                        grd.Rows.Add(new object[] { row["FieldName"], row["FieldType"] });
                    }

                    dr = dtProcs.Select("ImportTypeID = " + selectedTypeID);
                    txtProc1.Text = dr[0]["ProcedureName"].ToString();
                    if (dr.Length >= 2)
                        txtProc2.Text = dr[1]["ProcedureName"].ToString();
                    if (dr.Length >= 3)
                        txtProc3.Text = dr[2]["ProcedureName"].ToString();
                    btnExport.Visible = true;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private Result Save(SaveMode saveMode, ref string QueryString)
        {
            try
            {
                string ImportTypeName = txtImportType.Text.Trim();
                string Description = txtDescription.Text.Trim();
                DataTable dtSheets = new DataTable();
                dtSheets.Columns.Add("SheetNo", typeof(int));
                dtSheets.Columns.Add("SheetDescription", typeof(string));
                dtSheets.Columns.Add("StagingTable", typeof(string));

                List<string> Procs = new List<string>();
                DataTable dtColumns = new DataTable();
                dtColumns.Columns.Add("SheetNo", typeof(int));
                dtColumns.Columns.Add("FieldName", typeof(string));
                dtColumns.Columns.Add("FieldType", typeof(int));
                dtColumns.Columns.Add("Sequence", typeof(int));

                if (ImportTypeName == string.Empty)
                {
                    MessageBox.Show("Enter an import name");
                    txtImportType.Select();
                    txtImportType.Focus();
                    tabControl1.SelectedIndex = 0;
                    return Result.Invalid;
                }
                if (dtTypes.Select("Name = '" + ImportTypeName + "' AND ImportTypeID <> " + selectedTypeID).Length > 0)
                {
                    MessageBox.Show("An import type with same name is already defined");
                    txtImportType.Select();
                    txtImportType.Focus();
                    tabControl1.SelectedIndex = 0;
                    return Result.Invalid;
                }
                if (Description == string.Empty)
                {
                    MessageBox.Show(@"Enter description about the import type, ex. ""This type imports employees by adding new entries and update existing""");
                    txtDescription.Select();
                    txtDescription.Focus();
                    tabControl1.SelectedIndex = 0;
                    return Result.Invalid;
                }
                if (!ValidateSheetDescAndStaging(1, txtSheet1Desc, txtSheet1Staging, ref dtSheets))
                    return Result.Invalid;

                if (cbSheet2.Checked)
                {
                    if (!ValidateSheetDescAndStaging(2, txtSheet2Desc, txtSheet2Staging, ref dtSheets))
                        return Result.Invalid;
                }

                if (cbSheet3.Checked)
                {
                    if (!ValidateSheetDescAndStaging(3, txtSheet3Desc, txtSheet3Staging, ref dtSheets))
                        return Result.Invalid;
                }
                
                if (txtProc1.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Enter at least one procedure name to be called after storing data to staging table");
                    txtProc1.Select();
                    txtProc1.Focus();
                    tabControl1.SelectedIndex = 1;
                    return Result.Invalid;
                }
                else
                {
                    Procs.Add(txtProc1.Text.Trim());
                    if (txtProc2.Text.Trim() != string.Empty)
                        Procs.Add(txtProc2.Text.Trim());
                    if (txtProc3.Text.Trim() != string.Empty)
                        Procs.Add(txtProc3.Text.Trim());
                }

                if (!ValidateSheetsColumns(ref dtColumns))
                    return Result.Invalid;

                return em.SaveImportType(ImportTypeName, Description, dtSheets, Procs, dtColumns, saveMode, ref QueryString);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                return Result.Failure;
            }
        }
        private bool ValidateSheetDescAndStaging(int SheetNo, TextBox txtSheetDesc, TextBox txtSheetStaging, ref DataTable dtSheets)
        {
            try
            {
                if (txtSheetDesc.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Enter sheet description for sheet " + SheetNo + " to be visible to user on selection");
                    txtSheetDesc.Select();
                    txtSheetDesc.Focus();
                    tabControl1.SelectedIndex = 0;
                    return false;
                }
                if (txtSheetStaging.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Enter staging table for excel sheet " + SheetNo + " data to be stored");
                    txtSheetStaging.Select();
                    txtSheetStaging.Focus();
                    tabControl1.SelectedIndex = 0;
                    return false;
                }
                DataRow dr = dtSheets.NewRow();
                dr["SheetNo"] = SheetNo;
                dr["SheetDescription"] = txtSheetDesc.Text;
                dr["StagingTable"] = txtSheetStaging.Text;
                dtSheets.Rows.Add(dr);
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                return false;
            }
        }
        private bool ValidateSheetsColumns(ref DataTable dtColumns)
        {
            try
            {
                foreach (KeyValuePair<int, DataGridView> pair in Grids)
                {
                    if (pair.Value.Rows.Count == 1)
                    {
                        MessageBox.Show("Enter at least one column required in the excel file");
                        tabControl1.SelectedIndex = pair.Key + 1;
                        return false;
                    }
                    for (int i = 0; i < pair.Value.Rows.Count - 1; i++)
                    {
                        DataRow dr = dtColumns.NewRow();
                        dr["SheetNo"] = pair.Key;
                        if (pair.Value.Rows[i].Cells[0].Value == null || pair.Value.Rows[i].Cells[0].Value.ToString().Trim() == "")
                        {
                            MessageBox.Show("Fill columns names for all entries");
                            tabControl1.SelectedIndex = pair.Key + 1;
                            return false;
                        }
                        dr["FieldName"] = pair.Value.Rows[i].Cells[0].Value.ToString().Trim();
                        if (dtColumns.Select(string.Format("SheetNo = {0} AND FieldName = '{1}'", dr["SheetNo"], dr["FieldName"])).Length > 0)
                        {
                            MessageBox.Show("Columns names must be unique");
                            tabControl1.SelectedIndex = pair.Key + 1;
                            return false;
                        }
                        if (pair.Value.Rows[i].Cells[1].Value == null)
                        {
                            MessageBox.Show("Fill columns types for all entries");
                            tabControl1.SelectedIndex = pair.Key + 1;
                            return false;
                        }
                        dr["FieldType"] = pair.Value.Rows[i].Cells[1].Value.ToString().Trim();
                        dr["Sequence"] = i + 1;
                        dtColumns.Rows.Add(dr);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                return false;
            }
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                string QryStr = "";
                Result res = Save(SaveMode.ToDatabase, ref QryStr);
                if (res == Result.Success)
                {
                    MessageBox.Show("Saved successfully ..");
                    em.GetDataTypes(true, -1, ref dtTypes, ref dtSheets, ref dtSheetsColumns, ref dtProcs);
                    Dictionary<int, string> Types = new Dictionary<int, string>();
                    foreach (DataRow dr in dtTypes.Rows)
                    {
                        Types.Add(Convert.ToInt16(dr["ImportTypeID"]), dr["Name"].ToString());
                    }
                    Types.Add(0, "New ..");
                    cmbImportType.DataSource = new BindingSource(Types, null);
                    cmbImportType.DisplayMember = "value";
                    cmbImportType.ValueMember = "key";
                    cmbImportType.SelectedValue = 0;
                    tabControl1.SelectedIndex = 0;
                }
                else if (res != Result.Invalid)
                {
                    MessageBox.Show("Saving failed ..");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void txtProc1_TextChanged(object sender, EventArgs e)
        {
            if (txtProc1.Text.Trim() != string.Empty)
            {
                txtProc2.ReadOnly = false;
            }
            else
            {
                txtProc2.Clear();
                txtProc2.ReadOnly = true;
                txtProc3.Clear();
                txtProc3.ReadOnly = true;
            }
        }

        private void txtProc2_TextChanged(object sender, EventArgs e)
        {
            if (txtProc2.Text.Trim() != string.Empty)
            {
                txtProc3.ReadOnly = false;
            }
            else
            {
                txtProc3.Clear();
                txtProc3.ReadOnly = true;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                string QryStr = "";
                if (Save(SaveMode.ToScript, ref QryStr) == Result.Success)
                {
                    SaveFileDialog svd = new SaveFileDialog();
                    svd.FileName = "Import Type_" + txtImportType.Text;
                    svd.Filter = "SQL (*.sql)|*.sql";
                    if (svd.ShowDialog() == DialogResult.OK)
                    {
                        if (File.Exists(svd.FileName))
                            File.Delete(svd.FileName);
                        File.AppendAllText(svd.FileName, QryStr);
                    }
                }
                else
                {
                    MessageBox.Show("Preparing script failed!!");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                MessageBox.Show("Saving script failed!!");
            }
        }
        private void GrdColumns_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 1)
                SendKeys.Send("{F4}");
            else
                SendKeys.Send("{F2}");
        }
        private void GrdColumns_CellLeave(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0)
                SendKeys.Send("{ESC}");
        }
        private void AddColumnsTab(int No)
        {
            try
            {
                if (Grids.ContainsKey(No))
                    return;

                TabPage tbSheetX = new TabPage("Sheet" + No.ToString());
                tbSheetX.Name = "Sheet" + No.ToString(); ;
                tabControl1.TabPages.Add(tbSheetX);
                DataGridView grdColumns = new DataGridView();
                grdColumns.CellEnter += GrdColumns_CellEnter;
                grdColumns.CellLeave += GrdColumns_CellLeave;
                grdColumns.Name = "Columns" + No.ToString();
                grdColumns.BackgroundColor = System.Drawing.Color.White;
                grdColumns.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
                grdColumns.Dock = DockStyle.Fill;
                grdColumns.Location = new System.Drawing.Point(3, 3);
//                grdColumns.Size = new System.Drawing.Size(348, 258);
                DataGridViewTextBoxColumn col1 = new DataGridViewTextBoxColumn();
                col1.HeaderText = "Column Title";
                col1.Width = 150;
                grdColumns.Columns.Add(col1);
                DataGridViewComboBoxColumn col2 = new DataGridViewComboBoxColumn();
                col2.HeaderText = "Column Type";
                col2.DataSource = new BindingSource(_columnsTypes, null);
                col2.DisplayMember = "value";
                col2.ValueMember = "key";
                col2.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                grdColumns.Columns.Add(col2);
                tbSheetX.Controls.Add(grdColumns);
                Grids.Add(No, grdColumns);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }


        //private void GrdColumns_CellEnter(object sender, DataGridViewCellEventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

        private void cbSheet2_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (IgnoreCheckChange)
                {
                    IgnoreCheckChange = false;
                    return;
                }
                if (cbSheet2.Checked)
                {
                    AddColumnsTab(2);
                    txtSheet2Desc.ReadOnly = false;
                    txtSheet2Staging.ReadOnly = false;
                    cbSheet3.Enabled = true;
                }
                else
                {
                    if (cbSheet3.Checked)
                    {
                        MessageBox.Show("Can't uncheck sheet 2 as long as sheet 3 is checked");
                        IgnoreCheckChange = true;
                        cbSheet2.Checked = true;
                    }
                    else
                    {
                        DialogResult dr = MessageBox.Show("Uncheck sheet will remove the defined details and columns, continue?", "Confirm", MessageBoxButtons.YesNo);
                        if (dr == DialogResult.Yes)
                        {
                            tabControl1.TabPages.RemoveAt(tabControl1.TabPages.Count - 1);
                            Grids.Remove(2);
                            txtSheet2Desc.Clear();
                            txtSheet2Staging.Clear();
                            txtSheet2Desc.ReadOnly = true;
                            txtSheet2Staging.ReadOnly = true;
                            cbSheet3.Enabled = false;
                        }
                        else
                        {
                            IgnoreCheckChange = true;
                            cbSheet2.Checked = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void cbSheet3_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (IgnoreCheckChange)
                {
                    IgnoreCheckChange = false;
                    return;
                }
                if (cbSheet3.Checked)
                {
                    AddColumnsTab(3);
                    txtSheet3Desc.ReadOnly = false;
                    txtSheet3Staging.ReadOnly = false;
                }
                else
                {
                    DialogResult dr = MessageBox.Show("Uncheck sheet will remove the defined details and columns, continue?", "Confirm", MessageBoxButtons.YesNo);
                    if (dr == DialogResult.Yes)
                    {
                        tabControl1.TabPages.RemoveAt(tabControl1.TabPages.Count - 1);
                        Grids.Remove(3);
                        txtSheet3Desc.Clear();
                        txtSheet3Staging.Clear();
                        txtSheet3Desc.ReadOnly = true;
                        txtSheet3Staging.ReadOnly = true;
                    }
                    else
                    {
                        IgnoreCheckChange = true;
                        cbSheet3.Checked = true;
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
