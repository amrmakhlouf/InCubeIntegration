using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmAddEditDataTransferType : Form
    {
        int _id = -1;
        DataTransferManager dataTransferMgr;
        string PrimaryKeyColumns = "";
        string ConstantValues = "";
        bool CloneName = false;
        TransferTypes Mode;
        public frmAddEditDataTransferType(int ID, DataTransferManager _dataTransferMgr, TransferTypes TransType)
        {
            InitializeComponent();
            _id = ID;
            dataTransferMgr = _dataTransferMgr;
            Mode = TransType;
        }

        private void btnAddDestDB_Click(object sender, EventArgs e)
        {
            try
            {
                frmAddEditDatabaseConnection frm = new frmAddEditDatabaseConnection(-1, Mode == TransferTypes.DataWarehouse ? 1 : -1, dataTransferMgr);
                frm.ShowDialog();
                FillConnectionsLists();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnEditConnection_Click(object sender, EventArgs e)
        {
            try
            {
                int ID = 0;
                if (sender == btnEditSrcConn)
                {
                    if (cmbSrcDB.SelectedValue == null)
                    {
                        MessageBox.Show("Select source connection to edit!!");
                    }
                    else
                    {
                        ID = Convert.ToInt16(cmbSrcDB.SelectedValue);
                    }
                }
                else if (sender == btnEditDestConn)
                {
                    if (cmbDestDB.SelectedValue == null)
                    {
                        MessageBox.Show("Select destination connection to edit!!");
                    }
                    else
                    {
                        ID = Convert.ToInt16(cmbDestDB.SelectedValue);
                    }
                }

                if (ID == 0)
                {
                    MessageBox.Show("Utility connection is the same database integration tool is running on :)");
                }
                else if (ID > 0)
                {
                    frmAddEditDatabaseConnection frm = new frmAddEditDatabaseConnection(ID, -1, dataTransferMgr);
                    frm.ShowDialog();
                    FillConnectionsLists();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void FillTransferMethods()
        {
            try
            {
                DataTable dtTransferMethods = new DataTable();
                if (dataTransferMgr.GetTransferMethodsList(ref dtTransferMethods) == Result.Success)
                {
                    cmbTransferMethod.DataSource = dtTransferMethods;
                    cmbTransferMethod.DisplayMember = "Description";
                    cmbTransferMethod.ValueMember = "ID";
                    cmbTransferMethod.SelectedValue = TransferMethod.InsertAndUpdate.GetHashCode();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void FillConnectionsLists()
        {
            try
            {
                DataTable dtConnections = new DataTable();
                if (dataTransferMgr.GetConnectionsList(Mode == TransferTypes.DataWarehouse ? DataBaseType.SQLServer.GetHashCode() : -1, ref dtConnections) == Result.Success)
                {
                    object selectedSrcValue = null;
                    if (cmbSrcDB.ValueMember != null && cmbSrcDB.SelectedValue != null)
                        selectedSrcValue = cmbSrcDB.SelectedValue;

                    object selectedDestValue = null;
                    if (cmbDestDB.ValueMember != null && cmbDestDB.SelectedValue != null)
                        selectedDestValue = cmbDestDB.SelectedValue;

                    DataTable dtSrcConn = dtConnections.Copy();
                    cmbSrcDB.DataSource = dtSrcConn;
                    cmbSrcDB.DisplayMember = "Name";
                    cmbSrcDB.ValueMember = "ID";
                    if (selectedSrcValue != null)
                    {
                        try
                        {
                            cmbSrcDB.SelectedValue = selectedSrcValue;
                        }
                        catch
                        {

                        }
                    }

                    DataTable dtDestConn = dtConnections.Copy();
                    cmbDestDB.DataSource = dtDestConn;
                    cmbDestDB.DisplayMember = "Name";
                    cmbDestDB.ValueMember = "ID";
                    if (selectedDestValue != null)
                    {
                        try
                        {
                            cmbDestDB.SelectedValue = selectedDestValue;
                        }
                        catch
                        {

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void frmAddEditDataTransferType_Load(object sender, EventArgs e)
        {
            try
            {
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                if (Mode == TransferTypes.DataWarehouse)
                {
                    this.Text = "Add/Edit Data Warehouse Type";
                }
                FillConnectionsLists();
                FillTransferMethods();
                if (Mode == TransferTypes.DataWarehouse)
                {
                    cbIdentity.Visible = false;
                    cbSetConstantValues.Visible = false;
                }
                if (_id == -1)
                {
                    _id = dataTransferMgr.GetMaxTransferTypeID();
                    txtID.Text = _id.ToString();
                }
                else
                {
                    string Name = "", SelectQuery = "", DestinationTable = "";
                    int DestinationDatabaseID = 0, TransferMethodID = 0, SourceDatabaseID = 0;
                    bool HasIdentityColumn = false;
                    Result res = dataTransferMgr.GetTransferTypeDetails(_id, ref Name, ref SelectQuery, ref SourceDatabaseID, ref DestinationDatabaseID, ref DestinationTable, ref TransferMethodID, ref HasIdentityColumn, ref PrimaryKeyColumns, ref ConstantValues);
                    if (res == Result.Success)
                    {
                        txtID.Text = _id.ToString();
                        txtName.Text = Name;
                        txtSelectQuery.Text = SelectQuery;
                        cmbSrcDB.SelectedValue = SourceDatabaseID;
                        cmbDestDB.SelectedValue = DestinationDatabaseID;
                        txtDestTable.Text = DestinationTable;
                        cmbTransferMethod.SelectedValue = TransferMethodID;
                        cbIdentity.Checked = HasIdentityColumn;
                        cbSetConstantValues.Checked = ConstantValues != string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private Result CheckQuery(TransferMethod transferMethod, int SourceDatabaseID, string SelectQuery)
        {
            Result result = Result.UnKnown;
            try
            {
                using (DataTransferManager transferManager = new DataTransferManager())
                {
                    result = transferManager.InitializeSourceConnection(SourceDatabaseID);
                    if (result != Result.Success)
                    {
                        MessageBox.Show("Error opening source connection!!");
                        return Result.Invalid;
                    }

                    Dictionary<string, ColumnType> columns = new Dictionary<string, ColumnType>();
                    result = transferManager.GetQueryExecutionColumns(SelectQuery, ref columns);
                    if (result == Result.Success)
                    {
                        List<string> cols = new List<string>(columns.Keys);
                        for (int i = 0; i < cols.Count - 1; i++)
                        {
                            for (int j = i + 1; j < cols.Count; j++)
                            {
                                if (cols[i] == cols[j])
                                {
                                    MessageBox.Show("Column [" + cols[i] + "] is repeated, make sure query returns unique columns!!");
                                    return Result.Duplicate;
                                }
                            }
                        }
                        if (transferMethod != TransferMethod.DeleteAndInsert && Mode == TransferTypes.DataTransfer)
                        {
                            frmSelectPrimaryKeyColumns frm = new frmSelectPrimaryKeyColumns(cols);
                            frm.PrimaryKeyColumns = PrimaryKeyColumns;
                            if (frm.ShowDialog() != DialogResult.OK)
                                return Result.Invalid;
                            else
                                PrimaryKeyColumns = frm.PrimaryKeyColumns;
                        }
                        if (cbSetConstantValues.Checked && Mode == TransferTypes.DataTransfer)
                        {
                            if (PrimaryKeyColumns != string.Empty)
                            {
                                string[] PK_Cols = PrimaryKeyColumns.Split(new char[] { ',' });
                                foreach (string col in PK_Cols)
                                    columns.Remove(col);
                            }
                            frmSetConstantValues frm = new frmSetConstantValues(columns, transferManager);
                            frm.ConstantValuesStr = ConstantValues;
                            frm.ShowDialog();
                            if (frm.DialogResult != DialogResult.OK)
                            {
                                return Result.Invalid;
                            }
                            else
                            {
                                ConstantValues = frm.ConstantValuesStr;
                            }
                        }
                        else
                            ConstantValues = "";
                    }
                    else
                        MessageBox.Show("Query execution failed, please modify the query!!");
                }
            }
            catch (Exception ex)
            {
                result = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
            return result;
        }
        
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                string Name = "", SelectQuery = "", DestinationTable = "";
                int DestinationDatabaseID, TransferMethodID, SourceDatabaseID;

                if (txtName.Text.Trim() == string.Empty)
                {
                    MessageBox.Show(string.Format("Fill data {0} type name!!", Mode == TransferTypes.DataTransfer ? "transfer" : "warehouse"));
                    txtName.Focus();
                    return;
                }
                Name = txtName.Text.Trim();

                if (txtSelectQuery.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Fill select query!!");
                    txtSelectQuery.Focus();
                    return;
                }
                SelectQuery = txtSelectQuery.Text.Trim();

                if (cmbDestDB.SelectedValue == null)
                {
                    MessageBox.Show("Select destination connection!!");
                    cmbDestDB.Focus();
                    return;
                }
                DestinationDatabaseID = Convert.ToInt16(cmbDestDB.SelectedValue);

                if (cmbSrcDB.SelectedValue == null)
                {
                    MessageBox.Show("Select source connection!!");
                    cmbSrcDB.Focus();
                    return;
                }
                SourceDatabaseID = Convert.ToInt16(cmbSrcDB.SelectedValue);

                if (txtDestTable.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Fill destination table!!");
                    txtDestTable.Focus();
                    return;
                }
                DestinationTable = txtDestTable.Text.Trim();

                if (cmbTransferMethod.SelectedValue == null)
                {
                    MessageBox.Show("Select transfer method!!");
                    cmbTransferMethod.Focus();
                    return;
                }
                TransferMethodID = Convert.ToInt16(cmbTransferMethod.SelectedValue);

                if (CheckQuery((TransferMethod)TransferMethodID, SourceDatabaseID, SelectQuery) != Result.Success)
                    return;

                Result res = dataTransferMgr.SaveTransferType(_id, Name, SelectQuery, SourceDatabaseID, DestinationDatabaseID, DestinationTable, Mode == TransferTypes.DataWarehouse ? false : cbIdentity.Checked, (TransferMethod)TransferMethodID, PrimaryKeyColumns, ConstantValues, Mode);
                if (res == Result.Success)
                {
                    MessageBox.Show(string.Format("Data {0} type saved successfully ..", Mode == TransferTypes.DataTransfer ? "transfer" : "warehouse"));
                    this.Close();
                }
                else
                {
                    MessageBox.Show(string.Format("Failed in saving Data {0} type!!", Mode == TransferTypes.DataTransfer ? "transfer" : "warehouse"));
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            if (CloneName)
            {
                txtDestTable.Text = txtName.Text;
                txtSelectQuery.Text = "SELECT * FROM " + txtName.Text;
                CloneName = false;
            }
        }

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if ((txtDestTable.Text.Trim() == "" || txtDestTable.Text == txtName.Text)
                && (txtSelectQuery.Text.Trim() == "" || txtSelectQuery.Text == "SELECT * FROM " + txtName.Text))
            {
                CloneName = true;
            }
        }
    }
}