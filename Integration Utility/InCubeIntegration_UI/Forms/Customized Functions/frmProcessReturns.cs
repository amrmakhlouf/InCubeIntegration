using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmProcessReturns : Form
    {
        ExecutionManager execManager;
        IntegrationBase integrationObj;
        Procedure Proc;
        Result Res;
        bool _isFillig = false;
        
        private enum GridColumns
        {
            ID = 0,
            ProcessStatus,
            RowChanged,
            TransactionID,
            Type,
            Warehouse,
            EmployeeName,
            Date,
            CustomerCode,
            CustomerName,
            ItemCode,
            ItemName,
            TotalRtn,
            Unprocessed,
            Freeze,
            Kill,
            Refresh,
            Reverse,
            DocNo,
            Notes
        }
        public frmProcessReturns()
        {
            _isFillig = true;
            InitializeComponent();
            this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
            execManager = new ExecutionManager();
            integrationObj = new IntegrationBase(execManager);
            _isFillig = false;
        }

        private void frmManageReturns_Load(object sender, EventArgs e)
        {
            try
            {
                _isFillig = true;

                Proc = new Procedure("sp_GetListOfAllCustomers");
                DataTable dtAllCustomers = new DataTable();
                Res = integrationObj.ExecuteStoredProcedureWithTableOutput(Proc, ref dtAllCustomers);
                if (Res == Result.Success)
                {
                    cmbCustCode.DataSource = dtAllCustomers;
                    cmbCustCode.DisplayMember = "CustomerCode";
                    cmbCustCode.ValueMember = "CustOutID";
                    cmbCustName.DataSource = dtAllCustomers;
                    cmbCustName.DisplayMember = "CustomerName";
                    cmbCustName.ValueMember = "CustOutID";
                }

                Proc = new Procedure("sp_GetListOfAllSalesmen");
                DataTable dtAllSalesmen = new DataTable();
                Res = integrationObj.ExecuteStoredProcedureWithTableOutput(Proc, ref dtAllSalesmen);
                if (Res == Result.Success)
                {
                    cmbSalesman.DataSource = dtAllSalesmen;
                    cmbSalesman.DisplayMember = "EmployeeName";
                    cmbSalesman.ValueMember = "EmployeeID";
                }

                Proc = new Procedure("sp_GetListOfAllMainWarehouses");
                DataTable dtAllWarehoues = new DataTable();
                Res = integrationObj.ExecuteStoredProcedureWithTableOutput(Proc, ref dtAllWarehoues);
                if (Res == Result.Success)
                {
                    cmbWarehouse.DataSource = dtAllWarehoues;
                    cmbWarehouse.DisplayMember = "WarehouseName";
                    cmbWarehouse.ValueMember = "WarehouseID";
                }

                Dictionary<int, string> statuses = new Dictionary<int, string>();
                statuses.Add(-1, "All");
                statuses.Add(1, "Fully Processed");
                statuses.Add(2, "Partially Processed");
                statuses.Add(3, "Unprocessed");
                cmbProcessStatus.DataSource = new BindingSource(statuses, null);
                cmbProcessStatus.ValueMember = "Key";
                cmbProcessStatus.DisplayMember = "Value";

                Dictionary<int, string> types = new Dictionary<int, string>();
                types.Add(-1, "All");
                types.Add(1, "Customer Returns");
                types.Add(2, "Shop Returns");
                types.Add(3, "Unsold");
                cmbReturnType.DataSource = new BindingSource(types, null);
                cmbReturnType.ValueMember = "Key";
                cmbReturnType.DisplayMember = "Value";

                _isFillig = false;
            }
            catch (Exception ex)
            {
                _isFillig = false;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void btnFind_Click(object sender, EventArgs e)
        {
            try
            {
                Proc = new Procedure("sp_FillNewReturnsProcessingDetails");
                integrationObj.ExecuteStoredProcedure(Proc);

                DataTable dtReturnProcess = new DataTable();
                string CustOutID = "";
                int CustomerID = -1, OutletID = -1, EmployeeID = -1, WarehouseID = -1;
                if (!cbAllCustomers.Checked)
                {
                    CustOutID = cmbCustCode.SelectedValue.ToString();
                    CustomerID = Convert.ToInt32(CustOutID.Split(new char[] { ':' })[0]);
                    OutletID = Convert.ToInt32(CustOutID.Split(new char[] { ':' })[1]);
                }
                if (!cbAllSalesmen.Checked)
                {
                    EmployeeID = Convert.ToInt32(cmbSalesman.SelectedValue);
                }
                if (!cbAllWarehouses.Checked)
                {
                    WarehouseID = Convert.ToInt32(cmbWarehouse.SelectedValue);
                }
                Proc = new Procedure("sp_FindReturnProcessDetails");
                Proc.AddParameter("@CustomerID", ParamType.Integer, CustomerID);
                Proc.AddParameter("@OutletID", ParamType.Integer, OutletID);
                Proc.AddParameter("@EmployeeID", ParamType.Integer, EmployeeID);
                Proc.AddParameter("@ProcessStatusID", ParamType.Integer, cmbProcessStatus.SelectedValue);
                Proc.AddParameter("@FromDate", ParamType.DateTime, dtpFromDate.Value.Date);
                Proc.AddParameter("@ToDate", ParamType.DateTime, dtpToDate.Value.Date);
                Proc.AddParameter("@WarehouseID", ParamType.Integer, WarehouseID);
                Proc.AddParameter("@TypeID", ParamType.Integer, cmbReturnType.SelectedValue);
                Res = integrationObj.ExecuteStoredProcedureWithTableOutput(Proc, ref dtReturnProcess);
                if (Res == Result.Success)
                {
                    _isFillig = true;
                    dgvReturns.Rows.Clear();
                    DataGridViewRow row;
                    for (int i = 0; i < dtReturnProcess.Rows.Count; i++)
                    {
                        dgvReturns.Rows.Add();
                        row = dgvReturns.Rows[i];
                        row.Cells[GridColumns.ID.GetHashCode()].Value = dtReturnProcess.Rows[i]["ID"];
                        row.Cells[GridColumns.ProcessStatus.GetHashCode()].Value = dtReturnProcess.Rows[i]["ProcessStatus"];
                        row.Cells[GridColumns.RowChanged.GetHashCode()].Value = false;
                        row.Cells[GridColumns.TransactionID.GetHashCode()].Value = dtReturnProcess.Rows[i]["TransactionID"];
                        row.Cells[GridColumns.EmployeeName.GetHashCode()].Value = dtReturnProcess.Rows[i]["EmployeeName"];
                        row.Cells[GridColumns.Date.GetHashCode()].Value = dtReturnProcess.Rows[i]["TransactionDate"];
                        row.Cells[GridColumns.CustomerCode.GetHashCode()].Value = dtReturnProcess.Rows[i]["CustomerCode"];
                        row.Cells[GridColumns.CustomerName.GetHashCode()].Value = dtReturnProcess.Rows[i]["CustomerName"];
                        row.Cells[GridColumns.ItemCode.GetHashCode()].Value = dtReturnProcess.Rows[i]["ItemCode"];
                        row.Cells[GridColumns.ItemName.GetHashCode()].Value = dtReturnProcess.Rows[i]["ItemName"];
                        row.Cells[GridColumns.TotalRtn.GetHashCode()].Value = dtReturnProcess.Rows[i]["TotalQuantity"];
                        row.Cells[GridColumns.Unprocessed.GetHashCode()].Value = dtReturnProcess.Rows[i]["Unprocessed"];
                        row.Cells[GridColumns.Reverse.GetHashCode()].Value = dtReturnProcess.Rows[i]["Reverse"];
                        row.Cells[GridColumns.Refresh.GetHashCode()].Value = dtReturnProcess.Rows[i]["Refresh"];
                        row.Cells[GridColumns.Freeze.GetHashCode()].Value = dtReturnProcess.Rows[i]["Freeze"];
                        row.Cells[GridColumns.Kill.GetHashCode()].Value = dtReturnProcess.Rows[i]["Kill"];
                        row.Cells[GridColumns.Notes.GetHashCode()].Value = dtReturnProcess.Rows[i]["Notes"];
                        row.Cells[GridColumns.DocNo.GetHashCode()].Value = dtReturnProcess.Rows[i]["DocNo"];
                        row.Cells[GridColumns.Warehouse.GetHashCode()].Value = dtReturnProcess.Rows[i]["WarehouseName"];
                        row.Cells[GridColumns.Type.GetHashCode()].Value = dtReturnProcess.Rows[i]["Type"];
                        switch (dtReturnProcess.Rows[i]["ProcessStatus"].ToString())
                        {
                            case "1":
                                row.DefaultCellStyle.BackColor = Color.LightGreen;
                                break;
                            case "2":
                                row.DefaultCellStyle.BackColor = Color.Yellow;
                                break;
                            case "3":
                                row.DefaultCellStyle.BackColor = Color.LightSalmon;
                                break;
                        }
                    }
                    _isFillig = false;
                }
            }
            catch (Exception ex)
            {
                _isFillig = false;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void dgvReturns_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            try
            {
                e.Control.KeyPress -= new KeyPressEventHandler(Column1_KeyPress);
                if (dgvReturns.CurrentCell.ColumnIndex != GridColumns.Notes.GetHashCode() && dgvReturns.CurrentCell.ColumnIndex != GridColumns.DocNo.GetHashCode()) //Desired Column
                {
                    TextBox tb = e.Control as TextBox;
                    if (tb != null)
                    {
                        tb.KeyPress += new KeyPressEventHandler(Column1_KeyPress);
                    }
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
                if (!char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void dgvReturns_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isFillig)
                return;
            try
            {
                if (e.ColumnIndex == GridColumns.Kill.GetHashCode() || e.ColumnIndex == GridColumns.Freeze.GetHashCode() || e.ColumnIndex == GridColumns.Reverse.GetHashCode() || e.ColumnIndex == GridColumns.Refresh.GetHashCode())
                {
                    _isFillig = true;

                    decimal total = 0, freeze = 0, kill = 0, refresh = 0, reverse = 0, unprocessed = 0;
                    int ProcessStatusID = -1;
                    total = Convert.ToDecimal(dgvReturns.Rows[e.RowIndex].Cells[GridColumns.TotalRtn.GetHashCode()].Value);
                    freeze = Convert.ToDecimal(dgvReturns.Rows[e.RowIndex].Cells[GridColumns.Freeze.GetHashCode()].Value);
                    kill = Convert.ToDecimal(dgvReturns.Rows[e.RowIndex].Cells[GridColumns.Kill.GetHashCode()].Value);
                    refresh = Convert.ToDecimal(dgvReturns.Rows[e.RowIndex].Cells[GridColumns.Refresh.GetHashCode()].Value);
                    reverse = Convert.ToDecimal(dgvReturns.Rows[e.RowIndex].Cells[GridColumns.Reverse.GetHashCode()].Value);
                    if (total < freeze + kill + reverse + refresh)
                    {
                        if (e.ColumnIndex == GridColumns.Kill.GetHashCode())
                            kill = total - freeze - reverse - refresh;
                        else if (e.ColumnIndex == GridColumns.Freeze.GetHashCode())
                            freeze = total - kill - reverse - refresh;
                        else if (e.ColumnIndex == GridColumns.Refresh.GetHashCode())
                            refresh = total - kill - freeze - reverse;
                        else if (e.ColumnIndex == GridColumns.Reverse.GetHashCode())
                            reverse = total - kill - freeze - refresh;
                    }

                    if (e.ColumnIndex == GridColumns.Kill.GetHashCode())
                        dgvReturns.Rows[e.RowIndex].Cells[GridColumns.Kill.GetHashCode()].Value = kill.ToString("#0.000");
                    else if (e.ColumnIndex == GridColumns.Freeze.GetHashCode())
                        dgvReturns.Rows[e.RowIndex].Cells[GridColumns.Freeze.GetHashCode()].Value = freeze.ToString("#0.000");
                    else if (e.ColumnIndex == GridColumns.Reverse.GetHashCode())
                        dgvReturns.Rows[e.RowIndex].Cells[GridColumns.Reverse.GetHashCode()].Value = reverse.ToString("#0.000");
                    else if (e.ColumnIndex == GridColumns.Refresh.GetHashCode())
                        dgvReturns.Rows[e.RowIndex].Cells[GridColumns.Refresh.GetHashCode()].Value = refresh.ToString("#0.000");

                    unprocessed = total - kill - reverse - freeze - refresh;
                    dgvReturns.Rows[e.RowIndex].Cells[GridColumns.Unprocessed.GetHashCode()].Value = unprocessed;
                    dgvReturns.Rows[e.RowIndex].Cells[GridColumns.RowChanged.GetHashCode()].Value = true;
                    if (unprocessed == 0)
                        ProcessStatusID = 1;
                    else if (unprocessed > 0 && unprocessed < total)
                        ProcessStatusID = 2;
                    else
                        ProcessStatusID = 3;
                    dgvReturns.Rows[e.RowIndex].Cells[GridColumns.ProcessStatus.GetHashCode()].Value = ProcessStatusID;
                    switch (ProcessStatusID)
                    {
                        case 1:
                            dgvReturns.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
                            break;
                        case 2:
                            dgvReturns.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.Yellow;
                            break;
                        case 3:
                            dgvReturns.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightSalmon;
                            break;
                    }
                    _isFillig = false;
                }
                else if (e.ColumnIndex == GridColumns.Notes.GetHashCode() || e.ColumnIndex == GridColumns.DocNo.GetHashCode())
                {
                    dgvReturns.Rows[e.RowIndex].Cells[GridColumns.RowChanged.GetHashCode()].Value = true;
                }
            }
            catch (Exception ex)
            {
                _isFillig = false;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                int success = 0, failure = 0;
                for (int i = 0; i < dgvReturns.Rows.Count; i++)
                {
                    bool rowChanged = Convert.ToBoolean(dgvReturns.Rows[i].Cells[GridColumns.RowChanged.GetHashCode()].Value);
                    if (rowChanged)
                    {
                        int ID = Convert.ToInt32(dgvReturns.Rows[i].Cells[GridColumns.ID.GetHashCode()].Value);
                        decimal unprocessed = Convert.ToDecimal(dgvReturns.Rows[i].Cells[GridColumns.Unprocessed.GetHashCode()].Value);
                        decimal freeze = Convert.ToDecimal(dgvReturns.Rows[i].Cells[GridColumns.Freeze.GetHashCode()].Value);
                        decimal kill = Convert.ToDecimal(dgvReturns.Rows[i].Cells[GridColumns.Kill.GetHashCode()].Value);
                        decimal reverse = Convert.ToDecimal(dgvReturns.Rows[i].Cells[GridColumns.Reverse.GetHashCode()].Value);
                        decimal refresh = Convert.ToDecimal(dgvReturns.Rows[i].Cells[GridColumns.Refresh.GetHashCode()].Value);
                        string Notes = "";
                        if (dgvReturns.Rows[i].Cells[GridColumns.Notes.GetHashCode()].Value != null)
                            Notes = dgvReturns.Rows[i].Cells[GridColumns.Notes.GetHashCode()].Value.ToString().Replace("'", "''");
                        string DocNo = "";
                        if (dgvReturns.Rows[i].Cells[GridColumns.DocNo.GetHashCode()].Value != null)
                            DocNo = dgvReturns.Rows[i].Cells[GridColumns.DocNo.GetHashCode()].Value.ToString().Replace("'", "''");
                        
                        Proc = new Procedure("sp_UpdateReturnProcessDetails");
                        Proc.AddParameter("@ID", ParamType.Integer, ID);
                        Proc.AddParameter("@UnProcessed", ParamType.Decimal, unprocessed);
                        Proc.AddParameter("@Reverse", ParamType.Decimal, reverse);
                        Proc.AddParameter("@Refresh", ParamType.Decimal, refresh);
                        Proc.AddParameter("@Freeze", ParamType.Decimal, freeze);
                        Proc.AddParameter("@Kill", ParamType.Decimal, kill);
                        Proc.AddParameter("@DocNo", ParamType.Nvarchar, DocNo);
                        Proc.AddParameter("@Notes", ParamType.Nvarchar, Notes);
                        Res = integrationObj.ExecuteStoredProcedure(Proc);
                        if (Res == Result.Success)
                        {
                            dgvReturns.Rows[i].Cells[GridColumns.RowChanged.GetHashCode()].Value = false;
                            success++;
                        }
                        else
                            failure++;
                    }
                }
                if (success + failure == 0)
                    MessageBox.Show("No changes !!");
                else
                {
                    MessageBox.Show(string.Format("Saving completed: {0} success, {1} failed ..", success, failure));
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void cbAllCustomers_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                cmbCustCode.Enabled = !cbAllCustomers.Checked;
                cmbCustName.Enabled = !cbAllCustomers.Checked;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void cbAllSalesmen_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                cmbSalesman.Enabled = !cbAllSalesmen.Checked;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void cbAllWarehouses_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                cmbWarehouse.Enabled = !cbAllWarehouses.Checked;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
    }
}
