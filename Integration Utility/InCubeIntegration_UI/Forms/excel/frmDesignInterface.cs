using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using InVanImportingUtility_BL;
using InVanImportingUtility_Common;

namespace InCubeIntegration_UI
{
    public partial class frmDesignInterface : Form
    {
        DataTable dtTypes = new DataTable();
        DataTable dtDetails = new DataTable();
        DataTable dtProcs = new DataTable();
        BusinessManager bm = null;
        int selectedTypeID = 0;

        public frmDesignInterface(DataTable _dtTypes, DataTable _dtDetails, DataTable _dtProcs, BusinessManager BM)
        {
            dtTypes = _dtTypes;
            dtDetails = _dtDetails;
            dtProcs = _dtProcs;
            bm = BM;
            InitializeComponent();
        }

        private void frmDesignInterface_Load(object sender, EventArgs e)
        {
            try
            {
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

                Dictionary<int, string> _columnsTypes = new Dictionary<int, string>();
                foreach (ColumnType type in Enum.GetValues(typeof(ColumnType)))
                {
                    _columnsTypes.Add(type.GetHashCode(), type.ToString());
                }
                DataGridViewComboBoxColumn col = (DataGridViewComboBoxColumn)grdTableColumns.Columns[1];
                col.DataSource = new BindingSource(_columnsTypes, null);
                col.DisplayMember = "value";
                col.ValueMember = "key";
                col.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
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

                selectedTypeID = Convert.ToInt16(cmbImportType.SelectedValue.ToString());
                if (selectedTypeID == 0)
                {
                    txtDescription.Clear();
                    txtImportType.Clear();
                    txtProc1.Clear();
                    txtProc2.Clear();
                    txtProc3.Clear();
                    txtStagingTable.Clear();
                    grdTableColumns.Rows.Clear();
                }
                else
                {
                    txtProc2.Clear();
                    txtProc3.Clear();
                    grdTableColumns.Rows.Clear();

                    DataRow[] dr = dtTypes.Select("ImportTypeID = " + selectedTypeID);
                    txtImportType.Text = dr[0]["Name"].ToString();
                    txtDescription.Text = dr[0]["Description"].ToString();
                    txtStagingTable.Text = dr[0]["TableName"].ToString();

                    dr = dtDetails.Select("ImportTypeID = " + selectedTypeID);
                    foreach(DataRow row in dr)
                        grdTableColumns.Rows.Add(new object[] { row["FieldName"], row["FieldType"] });

                    dr = dtProcs.Select("ImportTypeID = " + selectedTypeID);
                    txtProc1.Text = dr[0]["ProcedureName"].ToString();
                    if (dr.Length >= 2)
                        txtProc2.Text = dr[1]["ProcedureName"].ToString();
                    if (dr.Length >= 3)
                        txtProc3.Text = dr[2]["ProcedureName"].ToString();
                }
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
                string ImportTypeName = txtImportType.Text.Trim();
                string Description = txtDescription.Text.Trim();
                string StagingTable = txtStagingTable.Text.Trim();
                List<string> Procs = new List<string>();
                DataTable dtColumns = new DataTable();
                dtColumns.Columns.Add("FieldName", typeof(string));
                dtColumns.Columns.Add("FieldType", typeof(int));

                if (ImportTypeName == string.Empty)
                {
                    MessageBox.Show("Enter an import name");
                    txtImportType.Select();
                    txtImportType.Focus();
                    tabControl1.SelectedIndex = 0;
                    return;
                }
                if (dtTypes.Select("Name = '" + ImportTypeName + "' AND ImportTypeID <> " + selectedTypeID).Length > 0)
                {
                    MessageBox.Show("An import type with same name is already defined");
                    txtImportType.Select();
                    txtImportType.Focus();
                    tabControl1.SelectedIndex = 0;
                    return;
                }
                if (Description == string.Empty)
                {
                    MessageBox.Show(@"Enter description about the import type, ex. ""This type imports employees and requires following columns: EmployeeCode, EmplyeeName and Country""");
                    txtDescription.Select();
                    txtDescription.Focus();
                    tabControl1.SelectedIndex = 0;
                    return;
                }
                if (StagingTable == string.Empty)
                {
                    MessageBox.Show("Enter staging table for excel data to be stored");
                    txtStagingTable.Select();
                    txtStagingTable.Focus();
                    tabControl1.SelectedIndex = 0;
                    return;
                }
                if (txtProc1.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Enter at least one procedure name to be called after storing data to staging table");
                    txtProc1.Select();
                    txtProc1.Focus();
                    tabControl1.SelectedIndex = 0;
                    return;
                }
                else
                {
                    Procs.Add(txtProc1.Text.Trim());
                    if (txtProc2.Text.Trim() != string.Empty)
                        Procs.Add(txtProc2.Text.Trim());
                    if (txtProc3.Text.Trim() != string.Empty)
                        Procs.Add(txtProc3.Text.Trim());
                }
                if (grdTableColumns.Rows.Count == 1)
                {
                    MessageBox.Show("Enter at least one column required in the excel file");
                    tabControl1.SelectedIndex = 1;
                    return;
                }
                for (int i = 0; i < grdTableColumns.Rows.Count - 1; i++)
                {
                    DataRow dr = dtColumns.NewRow();
                    if (grdTableColumns.Rows[i].Cells[0].Value == null || grdTableColumns.Rows[i].Cells[0].Value.ToString().Trim() == "")
                    {
                        MessageBox.Show("Fill columns names for all entries");
                        tabControl1.SelectedIndex = 1;
                        return;
                    }
                    dr["FieldName"] = grdTableColumns.Rows[i].Cells[0].Value.ToString().Trim();
                    if (dtColumns.Select("FieldName = '" + dr["FieldName"] + "'").Length > 0)
                    {
                        MessageBox.Show("Columns names must be unique");
                        tabControl1.SelectedIndex = 1;
                        return;
                    }
                    if (grdTableColumns.Rows[i].Cells[1].Value == null)
                    {
                        MessageBox.Show("Fill columns types for all entries");
                        tabControl1.SelectedIndex = 1;
                        return;
                    }
                    dr["FieldType"] = grdTableColumns.Rows[i].Cells[1].Value.ToString().Trim();
                    dtColumns.Rows.Add(dr);
                }

                if (bm.SaveImportType(selectedTypeID, ImportTypeName, Description, StagingTable, Procs, dtColumns) == Result.Success)
                {
                    MessageBox.Show("Saved successfully ..");
                    bm.GetDataTypes(ref dtTypes, ref dtDetails, ref dtProcs);
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
                else
                {
                    MessageBox.Show("Saving failed!!");
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
    }
}
