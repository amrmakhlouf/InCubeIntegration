using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmSetConstantValues : Form
    {
        Dictionary<string, ColumnType> Columns;
        public string ConstantValuesStr = "";
        Dictionary<string, DBColumn> ConstantValuesDic;
        Dictionary<string, string> colsOriginalNames = new Dictionary<string, string>();
        ColumnType SelectedColumnType;
        string SelectedColumnName;
        DataTransferManager TRSFR_Manager;
        public frmSetConstantValues(Dictionary<string, ColumnType> _columns, DataTransferManager trfrManager)
        {
            InitializeComponent();
            Columns = _columns;
            TRSFR_Manager = trfrManager;
        }

        private void frmSetConstantValues_Load(object sender, EventArgs e)
        {
            try
            {
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                cmbBooleanValue.Location = dtpDateValue.Location;
                txtTextValue.Location = dtpDateValue.Location;
                TRSFR_Manager.FillConstantsDictionary(ConstantValuesStr, ref ConstantValuesDic);
                
                foreach (string colName in Columns.Keys)
                    colsOriginalNames.Add(colName.ToLower(), colName);
                FillConstantValues();
                Dictionary<int, string> booleanOptions = new Dictionary<int, string>();
                booleanOptions.Add(0, "False (0)");
                booleanOptions.Add(1, "True (1)");
                cmbBooleanValue.DataSource = new BindingSource(booleanOptions, null);
                cmbBooleanValue.ValueMember = "Key";
                cmbBooleanValue.DisplayMember = "Value";
                cmbColumns.DataSource = new BindingSource(Columns, null);
                cmbColumns.ValueMember = "Value";
                cmbColumns.DisplayMember = "Key";

            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void ConstructConstantValuesString()
        {
            try
            {
                ConstantValuesStr = "";
                foreach (DBColumn col in ConstantValuesDic.Values)
                {
                    ConstantValuesStr += col.Name + "&" + col.Type.GetHashCode() + "&" + col.Value.ToString() + "|";
                }
                ConstantValuesStr = ConstantValuesStr.TrimEnd(new char[] { '|' });
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        
        private void FillConstantValues()
        {
            try
            {
                lsvConstantValues.Items.Clear();
                foreach (KeyValuePair<string, DBColumn> pair in ConstantValuesDic)
                {
                    ListViewItem lsvItem = new ListViewItem(new string[] { colsOriginalNames[pair.Key.ToString()], pair.Value.Type.ToString(), pair.Value.Value.ToString() });
                    lsvConstantValues.Items.Add(lsvItem);
                }
                cmbColumns_SelectedIndexChanged(null, null);
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
                ConstantValuesDic.Remove(SelectedColumnName);
                FillConstantValues();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnAddEdit_Click(object sender, EventArgs e)
        {
            try
            {
                object value = null;
                switch(SelectedColumnType)
                {
                    case ColumnType.Bool:
                        value = cmbBooleanValue.SelectedValue;
                        break;
                    case ColumnType.Datetime:
                        value = dtpDateValue.Value.ToString("yyyy-MM-dd");
                        break;
                    case ColumnType.Decimal:
                        value = txtTextValue.Text;
                        decimal d;
                        if (!decimal.TryParse(value.ToString(), out d))
                        {
                            MessageBox.Show("Invalid decimal value!!");
                            return;
                        }
                        break;
                    case ColumnType.Int:
                        value = txtTextValue.Text;
                        int i;
                        if (!int.TryParse(value.ToString(), out i))
                        {
                            MessageBox.Show("Invalid integer value!!");
                            return;
                        }
                        break;
                    default:
                        value = txtTextValue.Text;
                        break;
                }
                if (ConstantValuesDic.Keys.Contains(SelectedColumnName.ToLower()))
                {
                    ConstantValuesDic[SelectedColumnName.ToLower()].Value = value;
                }
                else
                {
                    DBColumn col = new DBColumn();
                    col.Name = SelectedColumnName;
                    col.Type = SelectedColumnType;
                    col.Value = value;
                    ConstantValuesDic.Add(SelectedColumnName.ToLower(), col);
                }
                FillConstantValues();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void cmbColumns_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cmbColumns.DisplayMember != "" && cmbColumns.ValueMember != "" && cmbColumns.SelectedValue.ToString() != "")
                {
                    SelectedColumnName = cmbColumns.Text;
                    SelectedColumnType = (ColumnType)cmbColumns.SelectedValue;
                    lblType.Text = SelectedColumnType.ToString();
                    object value = null;
                    if (ConstantValuesDic.Keys.Contains(SelectedColumnName.ToLower()))
                    {
                        btnAddEdit.Text = "Edit";
                        btnDelete.Visible = true;
                        value = ConstantValuesDic[SelectedColumnName.ToLower()].Value;
                    }
                    else
                    {
                        btnAddEdit.Text = "Add";
                        btnDelete.Visible = false;
                    }

                    switch (SelectedColumnType)
                    {
                        case ColumnType.Bool:
                            cmbBooleanValue.Visible = true;
                            txtTextValue.Visible = false;
                            dtpDateValue.Visible = false;
                            if (value != null)
                                cmbBooleanValue.SelectedIndex = Convert.ToInt16(value);
                            else
                                cmbBooleanValue.SelectedIndex = 0;
                            break;
                        case ColumnType.Datetime:
                            dtpDateValue.Visible = true;
                            cmbBooleanValue.Visible = false;
                            txtTextValue.Visible = false;
                            if (value != null)
                                dtpDateValue.Value = Convert.ToDateTime(value);
                            else
                                dtpDateValue.Value = DateTime.Today;
                            break;
                        default:
                            txtTextValue.Visible = true;
                            cmbBooleanValue.Visible = false;
                            dtpDateValue.Visible = false;
                            if (value != null)
                                txtTextValue.Text = value.ToString();
                            else
                                txtTextValue.Text = "";
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            ConstructConstantValuesString();
            this.DialogResult = DialogResult.OK;
        }

        private void lsvConstantValues_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (lsvConstantValues.SelectedItems.Count == 1)
                {
                    cmbColumns.Text = lsvConstantValues.SelectedItems[0].SubItems[0].Text;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}
