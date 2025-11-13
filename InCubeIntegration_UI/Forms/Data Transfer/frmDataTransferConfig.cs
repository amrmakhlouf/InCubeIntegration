using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.Data;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmDataTransferConfig : Form
    {
        DataTransferManager dataTransferMgr;
        int GroupID = -1;
        TransferTypes Mode;
        public frmDataTransferConfig(TransferTypes mode)
        {
            InitializeComponent();
            dataTransferMgr = new DataTransferManager();
            Mode = mode;
        }

        private void frmDataTransferConfig_Load(object sender, EventArgs e)
        {
            try
            {
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                if (Mode == TransferTypes.DataWarehouse)
                {
                    this.Text = "Data Warehouse Config";
                    lblGroup.Text = "Warehouse Group";
                }
                FillDataTransferTypes();
                FillDataTransferGroups();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void FillDataTransferGroups()
        {
            try
            {
                DataTable dtTransferGroups = new DataTable();
                Result res = dataTransferMgr.GetTransferGroups(ref dtTransferGroups, Mode.GetHashCode(), true);
                if (res == Result.Success)
                {
                    cmbTransferGroups.DataSource = dtTransferGroups;
                    cmbTransferGroups.DisplayMember = "GroupName";
                    cmbTransferGroups.ValueMember = "GroupID";
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void FillDataTransferTypes()
        {
            try
            {
                lsvTransferTypes.Items.Clear();
                DataTable dtTransferTypes = new DataTable();
                Result res = dataTransferMgr.GetActiveTransferTypes(Mode, ref dtTransferTypes);
                if (res == Result.Success)
                {
                    for (int i = 0; i < dtTransferTypes.Rows.Count; i++)
                    {
                        string[] values = new string[7];
                        values[0] = dtTransferTypes.Rows[i]["ID"].ToString();
                        values[1] = dtTransferTypes.Rows[i]["Name"].ToString();
                        values[2] = dtTransferTypes.Rows[i]["Source"].ToString();
                        values[3] = dtTransferTypes.Rows[i]["Destination"].ToString();
                        values[4] = dtTransferTypes.Rows[i]["DestinationTable"].ToString();
                        values[5] = dtTransferTypes.Rows[i]["TransferMethod"].ToString();
                        //values[6] = dtTransferTypes.Rows[i]["Sequence"].ToString();
                        ListViewItem lsvItem = new ListViewItem(values);
                        lsvTransferTypes.Items.Add(lsvItem);
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
                frmAddEditDataTransferType frm = new frmAddEditDataTransferType(-1, dataTransferMgr, Mode);
                frm.ShowDialog();
                FillDataTransferTypes();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            try
            {
                if (lsvTransferTypes.SelectedItems.Count == 1)
                {
                    int ID = Convert.ToInt16(lsvTransferTypes.SelectedItems[0].SubItems[0].Text);
                    frmAddEditDataTransferType frm = new frmAddEditDataTransferType(ID, dataTransferMgr, Mode);
                    frm.ShowDialog();
                    FillDataTransferTypes();
                }
                else
                {
                    MessageBox.Show(string.Format("Select data {0} type to edit!!", Mode == TransferTypes.DataTransfer ? "transfer" : "warehouse"));
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            try
            {
                if (lsvTransferTypes.SelectedItems.Count == 1)
                {
                    int ID = Convert.ToInt16(lsvTransferTypes.SelectedItems[0].SubItems[0].Text);
                    if (MessageBox.Show("Are you sure you want to delete selected type?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        if (dataTransferMgr.DeleteDataTransferType(ID) == Result.Success)
                        {
                            MessageBox.Show(string.Format("Data {0} type deleted ..", Mode == TransferTypes.DataTransfer ? "transfer" : "warehouse"));
                            FillDataTransferTypes();
                        }
                        else
                        {
                            MessageBox.Show("Deleting type failed!!");
                        }
                    }
                }
                else
                {
                    MessageBox.Show(string.Format("Select data {0} type to delete!!", Mode == TransferTypes.DataTransfer ? "transfer" : "warehouse"));
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void picUp_Click(object sender, EventArgs e)
        {
            try
            {
                if (lsvTransferTypes.SelectedIndices.Count == 1)
                {
                    int selectedIndex = lsvTransferTypes.SelectedIndices[0];
                    if (selectedIndex > 0)
                    {
                        int currentSeq = int.Parse(lsvTransferTypes.Items[selectedIndex].SubItems[6].Text);
                        int item1ID = int.Parse(lsvTransferTypes.Items[selectedIndex - 1].SubItems[0].Text);
                        lsvTransferTypes.Items[selectedIndex - 1].SubItems[6].Text = currentSeq.ToString();
                        ListViewItem item = lsvTransferTypes.Items[selectedIndex];
                        int item2D = int.Parse(item.SubItems[0].Text);
                        item.SubItems[6].Text = (currentSeq - 1).ToString();
                        lsvTransferTypes.Items.RemoveAt(selectedIndex);
                        lsvTransferTypes.Items.Insert(selectedIndex - 1, item);
                        //dataTransferMgr.UpdateTypeSequence(item1ID, currentSeq);
                        //dataTransferMgr.UpdateTypeSequence(item2D, currentSeq - 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void picDown_Click(object sender, EventArgs e)
        {
            try
            {
                if (lsvTransferTypes.SelectedIndices.Count == 1)
                {
                    int selectedIndex = lsvTransferTypes.SelectedIndices[0];
                    if (selectedIndex < lsvTransferTypes.Items.Count - 1)
                    {
                        int currentSeq = int.Parse(lsvTransferTypes.Items[selectedIndex].SubItems[6].Text);
                        int item1ID = int.Parse(lsvTransferTypes.Items[selectedIndex + 1].SubItems[0].Text);
                        lsvTransferTypes.Items[selectedIndex + 1].SubItems[6].Text = currentSeq.ToString();
                        ListViewItem item = lsvTransferTypes.Items[selectedIndex];
                        int item2D = int.Parse(item.SubItems[0].Text);
                        item.SubItems[6].Text = (currentSeq + 1).ToString();
                        lsvTransferTypes.Items.RemoveAt(selectedIndex);
                        lsvTransferTypes.Items.Insert(selectedIndex + 1, item);
                        //dataTransferMgr.UpdateTypeSequence(item1ID, currentSeq);
                        //dataTransferMgr.UpdateTypeSequence(item2D, currentSeq + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void LoadGroupTransfers()
        {
            try
            {
                lsvGroupIncluded.Items.Clear();
                lsvGroupExcluded.Items.Clear();
                DataTable dtGroupTransfers = new DataTable();
                Result res = dataTransferMgr.GetGroupTransfers(GroupID, ref dtGroupTransfers);
                if (res == Result.Success)
                {
                    for (int i = 0; i < dtGroupTransfers.Rows.Count; i++)
                    {
                        int Seq = Convert.ToInt16(dtGroupTransfers.Rows[i]["Sequence"]);
                        if (Seq == -1)
                        {
                            string[] values = new string[4];
                            values[0] = dtGroupTransfers.Rows[i]["ID"].ToString();
                            values[1] = dtGroupTransfers.Rows[i]["Name"].ToString();
                            values[2] = dtGroupTransfers.Rows[i]["Source"].ToString();
                            values[3] = dtGroupTransfers.Rows[i]["Destination"].ToString();
                            ListViewItem lsvItem = new ListViewItem(values);
                            lsvGroupExcluded.Items.Add(lsvItem);
                        }
                        else
                        {
                            string[] values = new string[4];
                            values[0] = dtGroupTransfers.Rows[i]["ID"].ToString();
                            values[1] = dtGroupTransfers.Rows[i]["Name"].ToString();
                            values[2] = dtGroupTransfers.Rows[i]["Destination"].ToString();
                            values[3] = Seq.ToString();
                            ListViewItem lsvItem = new ListViewItem(values);
                            lsvGroupIncluded.Items.Add(lsvItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void cmbTransferGroups_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cmbTransferGroups.SelectedIndex > 0)
                {
                    GroupID = Convert.ToInt16(cmbTransferGroups.SelectedValue);
                    LoadGroupTransfers();
                }
                else
                {
                    lsvGroupExcluded.Items.Clear();
                    lsvGroupIncluded.Items.Clear();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnExclude_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (ListViewItem lsvItem in lsvGroupIncluded.SelectedItems)
                {
                    int DataTransferID = int.Parse(lsvItem.SubItems[0].Text);
                    int Seq = int.Parse(lsvItem.SubItems[3].Text);
                    dataTransferMgr.RemoveTransferTypeFromGroup(DataTransferID, GroupID, Seq);
                }
                LoadGroupTransfers();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnInclude_Click(object sender, EventArgs e)
        {
            try
            {
                int LastSeq = lsvGroupIncluded.Items.Count;
                foreach (ListViewItem lsvItem in lsvGroupExcluded.SelectedItems)
                {
                    int DataTransferID = int.Parse(lsvItem.SubItems[0].Text);
                    dataTransferMgr.AddTransferTypeToGroup(DataTransferID, GroupID, ++LastSeq);
                }
                LoadGroupTransfers();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void picGroupItemUp_Click(object sender, EventArgs e)
        {
            try
            {
                if (lsvGroupIncluded.SelectedIndices.Count == 1)
                {
                    int selectedIndex = lsvGroupIncluded.SelectedIndices[0];
                    if (selectedIndex > 0)
                    {
                        int currentSeq = int.Parse(lsvGroupIncluded.Items[selectedIndex].SubItems[3].Text);
                        int transfer1ID = int.Parse(lsvGroupIncluded.Items[selectedIndex - 1].SubItems[0].Text);
                        lsvGroupIncluded.Items[selectedIndex - 1].SubItems[3].Text = currentSeq.ToString();
                        ListViewItem item = lsvGroupIncluded.Items[selectedIndex];
                        int transfer2ID = int.Parse(item.SubItems[0].Text);
                        item.SubItems[3].Text = (currentSeq - 1).ToString();
                        lsvGroupIncluded.Items.RemoveAt(selectedIndex);
                        lsvGroupIncluded.Items.Insert(selectedIndex - 1, item);
                        dataTransferMgr.UpdateTypeSequenceInGroup(GroupID, transfer1ID, currentSeq);
                        dataTransferMgr.UpdateTypeSequenceInGroup(GroupID, transfer2ID, currentSeq - 1);
                    }
                }
                else
                {
                    if (lsvGroupIncluded.SelectedIndices.Count == 0)
                        MessageBox.Show("Select an item to move up");
                    if (lsvGroupIncluded.SelectedIndices.Count > 1)
                        MessageBox.Show("Select one item only to move up");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void PicGroupItemDown_Click(object sender, EventArgs e)
        {
            try
            {
                if (lsvGroupIncluded.SelectedIndices.Count == 1)
                {
                    int selectedIndex = lsvGroupIncluded.SelectedIndices[0];
                    if (selectedIndex < lsvGroupIncluded.Items.Count - 1)
                    {
                        int currentSeq = int.Parse(lsvGroupIncluded.Items[selectedIndex].SubItems[3].Text);
                        int TransferType1ID = int.Parse(lsvGroupIncluded.Items[selectedIndex + 1].SubItems[0].Text);
                        lsvGroupIncluded.Items[selectedIndex + 1].SubItems[3].Text = currentSeq.ToString();
                        ListViewItem item = lsvGroupIncluded.Items[selectedIndex];
                        int TransferType2ID = int.Parse(item.SubItems[0].Text);
                        item.SubItems[3].Text = (currentSeq + 1).ToString();
                        lsvGroupIncluded.Items.RemoveAt(selectedIndex);
                        lsvGroupIncluded.Items.Insert(selectedIndex + 1, item);
                        dataTransferMgr.UpdateTypeSequenceInGroup(GroupID, TransferType1ID, currentSeq);
                        dataTransferMgr.UpdateTypeSequenceInGroup(GroupID, TransferType2ID, currentSeq + 1);
                    }
                }
                else
                {
                    if (lsvGroupIncluded.SelectedIndices.Count == 0)
                        MessageBox.Show("Select an item to move up");
                    if (lsvGroupIncluded.SelectedIndices.Count > 1)
                        MessageBox.Show("Select one item only to move up");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnManageGroups_Click(object sender, EventArgs e)
        {
            try
            {
                frmDataTransferGroup frm = new frmDataTransferGroup(Mode);
                frm.ShowDialog();
                FillDataTransferGroups();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}