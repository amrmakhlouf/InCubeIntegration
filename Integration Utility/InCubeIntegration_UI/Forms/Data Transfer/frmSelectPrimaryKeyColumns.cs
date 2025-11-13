using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using InCubeLibrary;

namespace InCubeIntegration_UI
{
    public partial class frmSelectPrimaryKeyColumns : Form
    {
        List<string> Columns;
        public string PrimaryKeyColumns = "";
        public frmSelectPrimaryKeyColumns(List<string> columns)
        {
            Columns = columns;
            InitializeComponent();
        }

        private void frmSelectPrimaryKeyColumns_Load(object sender, EventArgs e)
        {
            try
            {
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                string[] CheckedColumnsArr = PrimaryKeyColumns.Split(new char[] { ',' });
                List<string> CheckedColumns = new List<string>(CheckedColumnsArr);
                foreach (string colName in Columns)
                    clbColumns.Items.Add(colName, CheckedColumns.Contains(colName));
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
                if (clbColumns.CheckedItems.Count == 0)
                {
                    MessageBox.Show("Select at least one column for primary key at destination!!");
                    return;
                }
                PrimaryKeyColumns = "";
                for (int i = 0; i < clbColumns.CheckedItems.Count; i++)
                    PrimaryKeyColumns = PrimaryKeyColumns + clbColumns.CheckedItems[i].ToString() + ",";
                PrimaryKeyColumns = PrimaryKeyColumns.Substring(0, PrimaryKeyColumns.Length - 1);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
}
    }
}
