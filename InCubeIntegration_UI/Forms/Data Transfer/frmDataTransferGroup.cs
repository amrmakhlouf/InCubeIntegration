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
using InCubeIntegration_BL;

namespace InCubeIntegration_UI
{
    public partial class frmDataTransferGroup : Form
    {
        DataTransferManager dataTransferMgr;
        TransferTypes Mode;
        DataTable dtTransferGroups = null;
        public frmDataTransferGroup(TransferTypes transType)
        {
            InitializeComponent();
            Mode = transType;
            dataTransferMgr = new DataTransferManager();
        }

        private void frmDataTransferGroup_Load(object sender, EventArgs e)
        {
            this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
            if (Mode == TransferTypes.DataWarehouse)
            {
                this.Text = "Data Warehouse Groups";
            }
            FillDataTransferGroups();
        }
        private void FillDataTransferGroups()
        {
            try
            {
                lsvGroups.Items.Clear();
                dtTransferGroups = new DataTable();
                Result res = dataTransferMgr.GetTransferGroups(ref dtTransferGroups, Mode.GetHashCode(), false);
                if (res == Result.Success && dtTransferGroups.Rows.Count > 0)
                {
                    foreach (DataRow dr in dtTransferGroups.Rows)
                    {
                        ListViewItem lsvItem = new ListViewItem(dr["GroupID"].ToString());
                        lsvItem.SubItems.Add(dr["GroupName"].ToString());
                        lsvItem.SubItems.Add(dr["TransferType"].ToString());
                        lsvGroups.Items.Add(lsvItem);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                if (txtName.Text.Trim().Equals(string.Empty))
                {
                    MessageBox.Show("Fill name");
                    return;
                }
                if (dtTransferGroups.Select("GroupName = '" + txtName.Text.Trim() + "'").Length > 0)
                {
                    MessageBox.Show("Name is already used");
                    return;
                }
                if (dataTransferMgr.AddTransferGroup(txtName.Text, Mode.GetHashCode()) == Result.Success)
                {
                    FillDataTransferGroups();
                }
                else
                {
                    MessageBox.Show("Adding group failed!!");
                }
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
                if (lsvGroups.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Select a group to delete");
                    return;
                }
                int groupID = int.Parse(lsvGroups.SelectedItems[0].SubItems[0].Text);
                if (dataTransferMgr.DeleteTransferGroup(groupID) == Result.Success)
                {
                    FillDataTransferGroups();
                }
                else
                {
                    MessageBox.Show("Deleting group failed!!");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}
