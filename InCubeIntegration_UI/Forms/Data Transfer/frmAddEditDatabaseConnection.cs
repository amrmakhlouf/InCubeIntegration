using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmAddEditDatabaseConnection : Form
    {
        int _id = -1;
        DataTransferManager dataTransferMgr;
        int _dbTypeFilter = -1;
        public frmAddEditDatabaseConnection(int ID, int DbTypeFilter, DataTransferManager _dataTransferMgr)
        {
            InitializeComponent();
            dataTransferMgr = _dataTransferMgr;
            _id = ID;
            _dbTypeFilter = DbTypeFilter;
        }

        private void frmAddEditDatabaseConnection_Load(object sender, EventArgs e)
        {
            try
            {
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;

                Dictionary<int, string> _destDBType = new Dictionary<int, string>();
                if (_dbTypeFilter == -1)
                {
                    foreach (DataBaseType dbType in Enum.GetValues(typeof(DataBaseType)))
                        _destDBType.Add(dbType.GetHashCode(), dbType.ToString());
                }
                else
                {
                    _destDBType.Add(_dbTypeFilter, ((DataBaseType)_dbTypeFilter).ToString());
                }
                cmbDBType.DataSource = new BindingSource(_destDBType, null);
                cmbDBType.ValueMember = "Key";
                cmbDBType.DisplayMember = "Value";

                if (_id == -1)
                {
                    _id = dataTransferMgr.GetMaxDestinationDatabaseID();
                    txtID.Text = _id.ToString();
                }
                else
                {
                    string Name = "";
                    int DatabaseTypeID = 0;
                    string ConnectionString = "";
                    Result res = dataTransferMgr.GetDatabaseConnectionDetails(_id, ref Name, ref DatabaseTypeID, ref ConnectionString);
                    if (res == Result.Success)
                    {
                        txtID.Text = _id.ToString();
                        txtName.Text = Name;
                        cmbDBType.SelectedValue = DatabaseTypeID;
                        txtConnectionString.Text = ConnectionString;
                    }
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
                string Name = "";
                int DatabaseTypeID = 0;
                string ConnectionString = "";

                if (txtName.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Fill connection name!!");
                    txtName.Focus();
                    return;
                }
                Name = txtName.Text.Trim();

                if (cmbDBType.SelectedValue == null)
                {
                    MessageBox.Show("Select database type!!");
                    cmbDBType.Focus();
                    return;
                }
                DatabaseTypeID = Convert.ToInt16(cmbDBType.SelectedValue);

                if (txtConnectionString.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Fill connection string!!");
                    txtConnectionString.Focus();
                    return;
                }
                ConnectionString = txtConnectionString.Text.Trim();

                Result res = dataTransferMgr.TestDatabaseConnection((DataBaseType)DatabaseTypeID, ConnectionString);
                DialogResult dr = DialogResult.None;
                if (res != Result.Success)
                {
                    dr = MessageBox.Show("Connecting to database failed, do you want to proceed with saving destination anyway?", "Confirm", MessageBoxButtons.YesNo);
                }
                if (res == Result.Success || dr == DialogResult.Yes)
                {
                    res = dataTransferMgr.SaveDatabaseConnection(_id, Name, (DataBaseType)DatabaseTypeID, ConnectionString);
                    if (res == Result.Success)
                    {
                        MessageBox.Show("Database connection saved successfully ..");
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Failed in saving database connection!!");
                    }
                }
                else
                    return;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}