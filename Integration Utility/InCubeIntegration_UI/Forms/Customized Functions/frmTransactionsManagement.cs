using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmTransactionsManagement : Form
    {
        ExecutionManager execManager;
        IntegrationBase integrationObj;
        bool _isLoading = false;
        DataTable dtAllCustomers;
        Procedure Proc;
        Result Res;
        string CustomerID, OutletID, TransactionTypeID = "";
        string action = "", ToCustomerID = "0", ToOutletID = "0";
        int Success = 0, Failure = 0;
        BackgroundWorker bgwApply = new BackgroundWorker();
        DataTable dtSelecedTransactions;
        
        public frmTransactionsManagement()
        {
            InitializeComponent();
            this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
            execManager = new ExecutionManager();
            integrationObj = new IntegrationBase(execManager);
            bgwApply.DoWork += new DoWorkEventHandler(bgwApply_DoWork);
            bgwApply.WorkerReportsProgress = true;
            bgwApply.ProgressChanged += new ProgressChangedEventHandler(bgwApply_ProgressChanged);
            bgwApply.WorkerSupportsCancellation = true;
            bgwApply.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgwApply_RunWorkerCompleted);
        }

        private void rbTransfer_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                cmbCustToCode.Enabled = rbTransfer.Checked;
                cmbCustToName.Enabled = rbTransfer.Checked;
            }
            catch (Exception ex)
            {
                _isLoading = false;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void lsvTransactions_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            try
            {
                if (!_isLoading)
                {
                    _isLoading = true;
                    cbAllTransactions.Checked = lsvTransactions.CheckedItems.Count == lsvTransactions.Items.Count;
                    lblSelected.Text = "Selected " + lsvTransactions.CheckedItems.Count + "/" + lsvTransactions.Items.Count;
                    gbActions.Enabled = lsvTransactions.CheckedItems.Count > 0;
                    _isLoading = false;
                }
            }
            catch (Exception ex)
            {
                _isLoading = false;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void cbAllTransactions_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_isLoading)
                {
                    _isLoading = true;
                    foreach (ListViewItem lsvItem in lsvTransactions.Items)
                        lsvItem.Checked = cbAllTransactions.Checked;
                    lblSelected.Text = "Selected " + lsvTransactions.CheckedItems.Count + "/" + lsvTransactions.Items.Count;
                    gbActions.Enabled = lsvTransactions.CheckedItems.Count > 0;
                    _isLoading = false;
                }
            }
            catch (Exception ex)
            {
                _isLoading = false;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void frmTransactionsManagement_Load(object sender, EventArgs e)
        {
            try
            {
                lsvTransactions.ContextMenu = new ContextMenu();
                lsvTransactions.ContextMenu.MenuItems.Add("Edit", EditTransaction);
                Procedure Proc = new Procedure("sp_GetListOfAllCustomers");
                Result Res = integrationObj.ExecuteStoredProcedureWithTableOutput(Proc, ref dtAllCustomers);
                if (Res == Result.Success)
                {
                    cmbCustCode.DataSource = dtAllCustomers;
                    cmbCustCode.DisplayMember = "CustomerCode";
                    cmbCustCode.ValueMember = "CustOutID";
                    cmbCustName.DataSource = dtAllCustomers;
                    cmbCustName.DisplayMember = "CustomerName";
                    cmbCustName.ValueMember = "CustOutID";
                    string CustOutID = cmbCustName.SelectedValue.ToString();
                    CustomerID = CustOutID.Split(new char[] { ':' })[0];
                    OutletID = CustOutID.Split(new char[] { ':' })[1];
                }

                DataTable dtTransTypes = new DataTable();
                Proc = new Procedure("sp_GetListOfTransactionTypes");
                Res = integrationObj.ExecuteStoredProcedureWithTableOutput(Proc, ref dtTransTypes);
                if (Res == Result.Success)
                {
                    DataRow drBlank = dtTransTypes.NewRow();
                    foreach (DataColumn col in dtTransTypes.Columns)
                        drBlank[col.ColumnName] = "";
                    dtTransTypes.Rows.InsertAt(drBlank, 0);
                    cmbTransType.DataSource = dtTransTypes;
                    cmbTransType.DisplayMember = "TypeDesc";
                    cmbTransType.ValueMember = "TypeID";
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void btnFind_Click(object sender, EventArgs e)
        {
            try
            {
                lsvTransactions.Items.Clear();
                gbTransactions.Enabled = false;

                Procedure Proc = new Procedure("sp_GetCustomerTransactions");
                Proc.AddParameter("@CustomerID", ParamType.Integer, CustomerID);
                Proc.AddParameter("@OutletID", ParamType.Integer, OutletID);
                Proc.AddParameter("@TransactionTypeID", ParamType.Nvarchar, TransactionTypeID);
                Proc.AddParameter("@FromDate", ParamType.DateTime, dtpFromDate.Value.Date);
                Proc.AddParameter("@ToDate", ParamType.DateTime, dtpToDate.Value.Date);

                DataTable dtTransactions = new DataTable();
                Result Res = integrationObj.ExecuteStoredProcedureWithTableOutput(Proc, ref dtTransactions);
                if (Res == Result.Success)
                {
                    _isLoading = true;
                    gbTransactions.Enabled = dtTransactions.Rows.Count > 0;
                    lblSelected.Text = "Selected 0/" + dtTransactions.Rows.Count.ToString();
                    for (int i = 0; i < dtTransactions.Rows.Count; i++)
                    {
                        ListViewItem lsvItem = new ListViewItem(new string[] { dtTransactions.Rows[i]["TransactionID"].ToString(), dtTransactions.Rows[i]["TransactionDate"].ToString(), dtTransactions.Rows[i]["TransactionType"].ToString(), dtTransactions.Rows[i]["Amount"].ToString() });
                        lsvItem.Tag = dtTransactions.Rows[i];
                        lsvTransactions.Items.Add(lsvItem);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                _isLoading = false;
            }
        }
        private void EditTransaction(object sender, EventArgs e)
        {
            try
            {
                if (lsvTransactions.SelectedItems.Count == 1)
                {
                    DataRow dr = (DataRow)lsvTransactions.SelectedItems[0].Tag;
                    using (frmEditTransaction frm = new frmEditTransaction(dr["TransactionID"].ToString(), dr["TransactionTypeID"].ToString(), CustomerID, OutletID))
                    {
                        if (frm.ShowDialog() == DialogResult.OK)
                            btnFind_Click(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void cmbCustToName_EnabledChanged(object sender, EventArgs e)
        {
            try
            {
                if (cmbCustToName.Enabled)
                {
                    if (rbTransfer.Checked)
                    {
                        DataTable dtCustomersTo = dtAllCustomers.Copy();
                        dtCustomersTo.DefaultView.RowFilter = "CustOutID <> '" + cmbCustCode.SelectedValue.ToString() + "'";
                        dtCustomersTo = dtCustomersTo.DefaultView.ToTable();
                        cmbCustToCode.DataSource = dtCustomersTo;
                        cmbCustToCode.DisplayMember = "CustomerCode";
                        cmbCustToCode.ValueMember = "CustOutID";
                        cmbCustToName.DataSource = dtCustomersTo;
                        cmbCustToName.DisplayMember = "CustomerName";
                        cmbCustToName.ValueMember = "CustOutID";
                    }
                    else
                    {
                        cmbCustToCode.DataSource = null;
                        cmbCustToName.DataSource = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            try
            {
                gbFilters.Enabled = false;
                gbTransactions.Enabled = false;
                gbActions.Enabled = false;
                customProgressBar1.Value = 0;
                customProgressBar1.Maximum = lsvTransactions.CheckedItems.Count;

                if (rbTransfer.Checked)
                {
                    action = "Transfer";
                    string CustOutID = cmbCustToCode.SelectedValue.ToString();
                    ToCustomerID = CustOutID.Split(new char[] { ':' })[0];
                    ToOutletID = CustOutID.Split(new char[] { ':' })[1];
                }
                else if (rbVoid.Checked)
                {
                    action = "Void";
                }

                dtSelecedTransactions = new DataTable();
                dtSelecedTransactions.Columns.Add("TransactionID", typeof(string));
                dtSelecedTransactions.Columns.Add("TransactionTypeID", typeof(string));

                foreach (ListViewItem lsvItem in lsvTransactions.CheckedItems)
                {
                    DataRow dr = (DataRow)lsvItem.Tag;
                    DataRow drTransaction = dtSelecedTransactions.NewRow();
                    drTransaction["TransactionID"] = dr["TransactionID"];
                    drTransaction["TransactionTypeID"] = dr["TransactionTypeID"];
                    dtSelecedTransactions.Rows.Add(drTransaction);
                }

                bgwApply.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void cmbCustName_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cmbCustName.ValueMember != null && cmbCustName.ValueMember != string.Empty && cmbCustName.SelectedValue != null)
                {
                    string CustOutID = cmbCustName.SelectedValue.ToString();
                    CustomerID = CustOutID.Split(new char[] { ':' })[0];
                    OutletID = CustOutID.Split(new char[] { ':' })[1];
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void cmbTransType_SelectedIndexChanged(object sender, EventArgs e)
        {
            TransactionTypeID = "";
            if (cmbTransType.ValueMember != null && cmbTransType.ValueMember != string.Empty && cmbTransType.SelectedValue != null)
            {
                TransactionTypeID = cmbTransType.SelectedValue.ToString();
            }
        }

        private void bgwApply_DoWork(object o, DoWorkEventArgs e)
        {
            try
            {
                Success = 0; Failure = 0;

                
                for (int i = 0; i < dtSelecedTransactions.Rows.Count; i++)
                {
                    if (bgwApply.CancellationPending)
                        break;
                    bgwApply.ReportProgress(i);
                    Proc = new Procedure("sp_ApplyActionOnTransaction");
                    Proc.AddParameter("@TransactionID", ParamType.Nvarchar, dtSelecedTransactions.Rows[i]["TransactionID"]);
                    Proc.AddParameter("@TransactionTypeID", ParamType.Nvarchar, dtSelecedTransactions.Rows[i]["TransactionTypeID"]);
                    Proc.AddParameter("@CustomerID", ParamType.Nvarchar, CustomerID);
                    Proc.AddParameter("@OutletID", ParamType.Nvarchar, OutletID);
                    Proc.AddParameter("@Action", ParamType.Nvarchar, action);
                    Proc.AddParameter("@ToCustomerID", ParamType.Nvarchar, ToCustomerID);
                    Proc.AddParameter("@ToOutletID", ParamType.Nvarchar, ToOutletID);

                    Res = integrationObj.ExecuteStoredProcedure(Proc);
                    if (Res != Result.Success)
                        Failure++;
                    else
                        Success++;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void bgwApply_ProgressChanged(object o, ProgressChangedEventArgs e)
        {
            try
            {
                customProgressBar1.Value = e.ProgressPercentage;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void bgwApply_RunWorkerCompleted(object o, RunWorkerCompletedEventArgs e)
        {
            try
            {
                gbFilters.Enabled = true;

                customProgressBar1.Value = 0;

                if (Failure > 0)
                {
                    MessageBox.Show("Error in applying action for " + Failure + " transactions !!");
                }

                if (Success > 0)
                {
                    btnFind_Click(null, null);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (e.CloseReason == CloseReason.WindowsShutDown) return;
            bgwApply.CancelAsync();
        }
    }
}