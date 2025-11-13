using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.Data;
using System.Windows.Forms;
using System.Drawing;

namespace InCubeIntegration_UI
{
    public partial class frmUserAccess : Form
    {
        ExecutionManager execManager;
        bool _isChanging = false;
        int SelectedUserID = 0;
        FormMode _frmMode = FormMode.User;
        Font Regular, Bold;
        DateTime LastTabPressed = DateTime.Now;
        private class Privilege
        {
            public int PrivilegeID = 0;
            public int PrivilegeType = 0;
            public int ParentID = 0;
            public int Sequence = 0;
            public bool DefaultCheck = false;
            public bool HasAccess = false;
            public string Name = "";
            public Privilege(DataRow dr)
            {
                Name = dr["Name"].ToString();
                PrivilegeID = int.Parse(dr["PrivilegeID"].ToString());
                PrivilegeType = int.Parse(dr["PrivilegeType"].ToString());
                ParentID = int.Parse(dr["ParentID"].ToString());
                Sequence = int.Parse(dr["Sequence"].ToString());
                HasAccess = int.Parse(dr["HasAccess"].ToString()) == 1;
                DefaultCheck = bool.Parse(dr["DefaultCheck"].ToString());
            }
        }
        public enum FormMode
        {
            InCubeAdmin,
            User
        }

        public frmUserAccess(FormMode frmMode)
        {
            try
            {
                InitializeComponent();
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                Regular = new Font(trvPrivileges.Font, FontStyle.Regular);
                Bold = new Font(trvPrivileges.Font, FontStyle.Bold);
                execManager = new ExecutionManager();
                _frmMode = frmMode;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void frmUserAccess_Load(object sender, EventArgs e)
        {
            try
            {
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;

                DataTable dtUsers = new DataTable();
                Result res = Result.UnKnown;
                if (_frmMode == FormMode.User)
                {
                    res = execManager.GetIntegrationUsersList(ref dtUsers);
                }
                else
                {
                    dtUsers.Columns.Add("UserID", typeof(int));
                    dtUsers.Columns.Add("UserCode", typeof(string));
                    dtUsers.Columns.Add("UserName", typeof(string));
                    dtUsers.Rows.Add(dtUsers.NewRow());
                    dtUsers.Rows[0]["UserID"] = 0;
                    dtUsers.Rows[0]["UserCode"] = "Sys_Admin";
                    dtUsers.Rows[0]["UserName"] = "Sys_Admin";
                    cmbUserCode.Enabled = false;
                    cmbUserName.Enabled = false;
                    res = Result.Success;
                }

                if (res == Result.Success)
                {
                    cmbUserCode.DataSource = dtUsers;
                    cmbUserCode.ValueMember = "UserID";
                    cmbUserCode.DisplayMember = "UserCode";

                    cmbUserName.DataSource = dtUsers;
                    cmbUserName.ValueMember = "UserID";
                    cmbUserName.DisplayMember = "UserName";
                }

                cmbUserCode.SelectedIndex = 0;

            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void cmbUserCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cmbUserCode.ValueMember != "" && cmbUserCode.SelectedValue != null)
                {
                    trvPrivileges.Nodes.Clear();
                    SelectedUserID = int.Parse(cmbUserCode.SelectedValue.ToString());
                    DataTable dtPrivileges = new DataTable();

                    if (execManager.GetUserPrivileges(SelectedUserID, _frmMode == FormMode.InCubeAdmin, ref dtPrivileges) == Result.Success)
                    {
                        FillTreeView(dtPrivileges);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void FillTreeView(DataTable dtPrivileges)
        {
            try
            {
                _isChanging = true;
                DataRow[] dr = dtPrivileges.Select("ParentID = 0");
                for (int i = 0; i < dr.Length; i++)
                {
                    Privilege p = new Privilege(dr[i]);
                    TreeNode td = new TreeNode(p.Name);
                    td.Checked = p.HasAccess;
                    td.NodeFont = Regular;
                    td.Tag = p;
                    
                    DataRow[] drChilds = dtPrivileges.Select("ParentID = " + p.PrivilegeID);
                    if (drChilds.Length > 0)
                    {
                        for (int j = 0; j < drChilds.Length; j++)
                        {
                            Privilege c1 = new Privilege(drChilds[j]);
                            TreeNode tdChild1 = new TreeNode(c1.Name);
                            tdChild1.Checked = c1.HasAccess;
                            tdChild1.NodeFont = Regular;
                            tdChild1.Tag = c1;

                            if ((Menus)p.PrivilegeID != Menus.Excel_Import)
                            {
                                DataRow[] drChilds2 = dtPrivileges.Select("ParentID = " + c1.PrivilegeID);
                                if (drChilds2.Length > 0)
                                {
                                    for (int k = 0; k < drChilds2.Length; k++)
                                    {
                                        Privilege c2 = new Privilege(drChilds2[k]);
                                        TreeNode tdChild2 = new TreeNode(c2.Name);
                                        tdChild2.Checked = c2.HasAccess;
                                        tdChild2.NodeFont = c2.DefaultCheck ? Bold : Regular;
                                        tdChild2.Tag = c2;
                                        tdChild1.Nodes.Add(tdChild2);
                                    }
                                }
                            }
                            td.Nodes.Add(tdChild1);
                        }
                    }
                    trvPrivileges.Nodes.Add(td);
                }
                trvPrivileges.ExpandAll();
                trvPrivileges.SelectedNode = trvPrivileges.Nodes[0];
                _isChanging = false;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void trvPrivileges_AfterCheck(object sender, TreeViewEventArgs e)
        {
            try
            {
                if (!_isChanging)
                {
                    _isChanging = true;
                    if (e.Node.Nodes.Count > 0)
                    {
                        foreach (TreeNode node in e.Node.Nodes)
                        {
                            if (node.Nodes.Count > 0)
                            {
                                foreach (TreeNode node2 in node.Nodes)
                                {
                                    if (node2.Nodes.Count > 0)
                                    {
                                        foreach (TreeNode node3 in node2.Nodes)
                                        {
                                            node3.Checked = e.Node.Checked;
                                        }
                                    }
                                    node2.Checked = e.Node.Checked;
                                }
                            }
                            node.Checked = e.Node.Checked;
                        }
                    }
                    if (e.Node.Parent != null)
                    {
                        if (e.Node.Checked && !e.Node.Parent.Checked)
                        {
                            e.Node.Parent.Checked = true;
                        }
                        if (e.Node.Parent.Parent != null && !e.Node.Parent.Parent.Checked)
                        {
                            e.Node.Parent.Parent.Checked = true;
                        }
                        else
                        {
                            int count = 0;
                            foreach (TreeNode node in e.Node.Parent.Nodes)
                            {
                                if (!node.Checked)
                                    count++;
                            }
                            e.Node.Parent.Checked = !(count == e.Node.Parent.Nodes.Count);

                            if (e.Node.Parent.Parent != null)
                            {
                                count = 0;
                                foreach (TreeNode node in e.Node.Parent.Parent.Nodes)
                                {
                                    if (!node.Checked)
                                        count++;
                                }
                                e.Node.Parent.Parent.Checked = !(count == e.Node.Parent.Parent.Nodes.Count);
                            }
                        }
                    }
                    _isChanging = false;
                }
                Privilege p = (Privilege)e.Node.Tag;
                if (e.Node.Checked != p.HasAccess)
                {
                    p.HasAccess = e.Node.Checked;
                    int preSequence = p.Sequence;
                    UpdateSequence(e.Node, p);
                    p.DefaultCheck = e.Node.NodeFont.Style == FontStyle.Bold;

                    execManager.AddRemoveUserPrivilges(SelectedUserID, p.PrivilegeID, p.PrivilegeType, p.HasAccess, p.ParentID, p.HasAccess ? p.Sequence : preSequence, p.DefaultCheck);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void trvPrivileges_KeyPress(object sender, KeyPressEventArgs e)
        {
        }

        private void trvPrivileges_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            bool NewSeq = false;
            int change = -1;
            try
            {
                
                if (e.KeyCode == Keys.Tab || e.KeyCode == Keys.Back || e.KeyCode == Keys.CapsLock)
                    LastTabPressed = DateTime.Now;
                TreeNode tn = trvPrivileges.SelectedNode;
                
                if (e.KeyCode == Keys.Tab)
                    change = 1;
                else if (e.KeyCode == Keys.Back)
                    change = -1;
                else if (e.KeyCode == Keys.CapsLock)
                    change = 0;
                else
                    return;

                int grandparentindex = -1;
                int parentindex = -1;
                int index = tn.Index;
                if (index == 0 && change == -1)
                    return;

                int newindex = index + change;

                if (tn.Parent != null)
                {
                    parentindex = tn.Parent.Index;
                    if (tn.Parent.Parent != null)
                    {
                        grandparentindex = tn.Parent.Parent.Index;
                    }
                }
                if (grandparentindex != -1)
                {
                    if (change == 1 && index == trvPrivileges.Nodes[grandparentindex].Nodes[parentindex].Nodes.Count - 1)
                        return;
                    if (change == 0)
                        tn.NodeFont = tn.NodeFont == Bold ? Regular : Bold;
                    if (change != 0 && trvPrivileges.SelectedNode.Checked && trvPrivileges.Nodes[grandparentindex].Nodes[parentindex].Nodes[newindex].Checked)
                    {
                        NewSeq = true;
                    }
                    trvPrivileges.SelectedNode.Remove();
                    trvPrivileges.Nodes[grandparentindex].Nodes[parentindex].Nodes.Insert(newindex, tn);
                    trvPrivileges.SelectedNode = trvPrivileges.Nodes[grandparentindex].Nodes[parentindex].Nodes[newindex];
                }
                else if (parentindex != -1 && change != 0)
                {
                    if (change == 1 && index == trvPrivileges.Nodes[parentindex].Nodes.Count - 1)
                        return;
                    if (change != 0 && trvPrivileges.SelectedNode.Checked && trvPrivileges.Nodes[parentindex].Nodes[newindex].Checked)
                    {
                        NewSeq = true;
                    }
                    trvPrivileges.SelectedNode.Remove();
                    trvPrivileges.Nodes[parentindex].Nodes.Insert(newindex, tn);
                    trvPrivileges.SelectedNode = trvPrivileges.Nodes[parentindex].Nodes[newindex];
                }
                else if (change != 0)
                {
                    if (change == 1 && index == trvPrivileges.Nodes.Count - 1)
                        return;
                    if (change != 0 && trvPrivileges.SelectedNode.Checked && trvPrivileges.Nodes[newindex].Checked)
                    {
                        NewSeq = true;
                    }
                    trvPrivileges.SelectedNode.Remove();
                    trvPrivileges.Nodes.Insert(newindex, tn);
                    trvPrivileges.SelectedNode = trvPrivileges.Nodes[newindex];
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
            finally
            {
                if (NewSeq)
                {
                    Privilege p = (Privilege)trvPrivileges.SelectedNode.Tag;
                    UpdateSequence(trvPrivileges.SelectedNode, p);
                    execManager.UpdateSequence(p.PrivilegeID, SelectedUserID, p.ParentID, p.PrivilegeType, p.Sequence, change);
                }
                if (trvPrivileges.SelectedNode.Checked && change == 0)
                {
                    Privilege p = (Privilege)trvPrivileges.SelectedNode.Tag;
                    p.DefaultCheck = trvPrivileges.SelectedNode.NodeFont.Style == FontStyle.Bold;
                    execManager.UpdateDefaultCheck(p.PrivilegeID, SelectedUserID, p.PrivilegeType, p.DefaultCheck);
                }
            }
        }
        private void UpdateSequence (TreeNode MovedNode, Privilege p)
        {
            try
            {
                TreeNodeCollection col = null;
                if (MovedNode.Parent != null)
                    col = MovedNode.Parent.Nodes;
                else
                    col = trvPrivileges.Nodes;

                p.Sequence = p.HasAccess ? 1 : 0;
                bool itemFound = false;
                foreach (TreeNode node in col)
                {
                    if (node == MovedNode)
                    {
                        itemFound = true;
                        continue;
                    }
                    if (node.Checked)
                    {
                        if (!itemFound && p.HasAccess)
                            p.Sequence++;
                        else
                        {
                            Privilege currPriv = (Privilege)node.Tag;
                            if (currPriv.HasAccess)
                                currPriv.Sequence += (p.HasAccess ? 1 : -1);
                        }
                    }
                }
            }
            catch
            {

            }
        }
        private void trvPrivileges_KeyDown(object sender, KeyEventArgs e)
        {
            if (DateTime.Now - LastTabPressed <= new TimeSpan(0, 0, 0, 0, 300))
                e.Handled = true;
        }
    }
}