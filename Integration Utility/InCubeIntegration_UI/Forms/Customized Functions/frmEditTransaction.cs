using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmEditTransaction : Form
    {
        IntegrationBase integrationObj;
        ExecutionManager execManager;
        int TransactionTypeID = 0;
        string MainType = "";
        string TransactionID = "";
        int CustomerID = 0;
        int OutletID = 0;

        public frmEditTransaction(string transactionID, string Type, string customerID, string outletID)
        {
            try
            {
                InitializeComponent();

                TransactionID = transactionID;
                CustomerID = int.Parse(customerID);
                OutletID = int.Parse(outletID);
                
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                Procedure Proc = new Procedure("sp_GetTransactionDetails");
                Proc.AddParameter("@TransactionID", ParamType.Nvarchar, TransactionID);
                Proc.AddParameter("@TypeID", ParamType.Nvarchar, Type);
                Proc.AddParameter("@CustomerID", ParamType.Integer, CustomerID);
                Proc.AddParameter("@OutletID", ParamType.Integer, OutletID);
                DataTable dtTransactionDetails = new DataTable();
                execManager = new ExecutionManager();
                integrationObj = new IntegrationBase(execManager);
                Result Res = integrationObj.ExecuteStoredProcedureWithTableOutput(Proc, ref dtTransactionDetails);
                if (Res == Result.Success)
                {
                    if (dtTransactionDetails.Rows.Count > 0)
                    {
                        DateTime TransactionDate = Convert.ToDateTime(dtTransactionDetails.Rows[0]["TransactionDate"]);
                        string Notes = dtTransactionDetails.Rows[0]["Notes"].ToString();
                        string TransactionType = dtTransactionDetails.Rows[0]["TransactionType"].ToString();
                        TransactionTypeID = Convert.ToInt16(dtTransactionDetails.Rows[0]["TransactionTypeID"]);
                        MainType = dtTransactionDetails.Rows[0]["MainType"].ToString();

                        txtTransactionID.Text = TransactionID;
                        txtTransactionType.Text = TransactionType;
                        txtNotes.Text = Notes;
                        dtpTransactionDate.Value = TransactionDate;
                    }
                }
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
                Procedure proc = new Procedure("sp_UpdateTransactionDetails");
                proc.AddParameter("@MainType", ParamType.Nvarchar, MainType);
                proc.AddParameter("@TransactionID", ParamType.Nvarchar, TransactionID);
                proc.AddParameter("@TransactionTypeID", ParamType.Integer, TransactionTypeID);
                proc.AddParameter("@CustomerID", ParamType.Integer, CustomerID);
                proc.AddParameter("@OutletID", ParamType.Integer, OutletID);
                proc.AddParameter("@TransactionDate", ParamType.DateTime, dtpTransactionDate.Value);
                proc.AddParameter("@Notes", ParamType.Nvarchar, txtNotes.Text.Replace("'", "''"));
                Result res = integrationObj.ExecuteStoredProcedure(proc);
                MessageBox.Show(res.ToString());
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
    }
}
