using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmExecutionDetailsQuery : Form
    {
        string ExecQuery = "";
        public frmExecutionDetailsQuery(string execQuery)
        {
            InitializeComponent();
            ExecQuery = execQuery;
        }

        private void frmExecutionDetailsQuery_Load(object sender, EventArgs e)
        {
            if (ExecQuery == "")
            {
                txtExecQuery.Text = @"SELECT ID, Message
FROM @ExecutionTable
WHERE TriggerID = @TriggerID AND ID > @ProcessID
AND ResultID IS NOT NULL";
            }
            else
            {
            }
        }
    }
}
