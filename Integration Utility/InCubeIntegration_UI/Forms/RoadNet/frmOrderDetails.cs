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

namespace InCubeIntegration_UI
{
    public partial class frmOrderDetails : Form
    {
        string OrderID;
        string CustomerCode;
        DateTime OrderDate;
        DataTable dtItems;
        public frmOrderDetails(string _orderID, string _customerCode, DateTime _orderDate, DataTable _dtItems)
        {
            InitializeComponent();
            this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
            OrderID = _orderID;
            CustomerCode = _customerCode;
            OrderDate = _orderDate;
            dtItems = _dtItems;
        }

        private void frmOrderDetails_Load(object sender, EventArgs e)
        {
            try
            {
                txtOrderID.Text = OrderID;
                txtCustomerCode.Text = CustomerCode;
                dtpOrderDate.Value = OrderDate;
                grdItems.DataSource = dtItems;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
    }
}
