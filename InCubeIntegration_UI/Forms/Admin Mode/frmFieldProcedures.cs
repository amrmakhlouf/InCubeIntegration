using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using InCubeIntegration_BL;
using InCubeLibrary;

namespace InCubeIntegration_UI
{
    public partial class frmFieldProcedures : Form
    {
        ExecutionManager execManager;
        MailManager mailManager;
        public frmFieldProcedures()
        {
            InitializeComponent();
            execManager = new ExecutionManager();
            mailManager = new MailManager();
        }

        private void frmFieldProcedures_Load(object sender, EventArgs e)
        {
            try
            {
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                DataTable dtFields = new DataTable();
                if (execManager.GetAllFields(ref dtFields) == Result.Success)
                {
                    Dictionary<string, ListViewGroup> groups = new Dictionary<string, ListViewGroup>();
                    foreach (DataRow dr in dtFields.Rows)
                    {
                        string Group = dr["Type"].ToString();
                        ListViewGroup grp;
                        if (groups.ContainsKey(Group))
                        {
                            grp = groups[Group];
                        }

                        else
                        {
                            grp = new ListViewGroup(Group);
                            lsvFields.Groups.Add(grp);
                            groups.Add(Group, grp);
                        }
                        string FieldID = dr["FieldID"].ToString();
                        string FieldName = dr["FieldName"].ToString();
                        bool ProcDefined = Convert.ToBoolean(dr["ProcDefined"]);
                        ListViewItem lsvItem = new ListViewItem(new string[] { FieldID, FieldName }, grp);
                        if (ProcDefined)
                            lsvItem.BackColor = Color.LightGreen;
                        lsvFields.Items.Add(lsvItem);
                    }
                }

                Dictionary<int, string> procTypes = new Dictionary<int, string>();
                foreach (ProcType type in Enum.GetValues(typeof(ProcType)))
                {
                    procTypes.Add(type.GetHashCode(), type.ToString());
                }
                DataGridViewComboBoxColumn col = (DataGridViewComboBoxColumn)grdProcedures.Columns["clmProcType"];
                col.DataSource = new BindingSource(procTypes, null);
                col.DisplayMember = "value";
                col.ValueMember = "key";

                DataTable dtMailTemplates = new DataTable();
                if (mailManager.GetDistinctActiveMailTemplates(ref dtMailTemplates) == Result.Success)
                {
                    col = (DataGridViewComboBoxColumn)grdProcedures.Columns["clmMailTemplateID"];
                    col.DataSource = dtMailTemplates;
                    col.DisplayMember = "TemplateName";
                    col.ValueMember = "MailTemplateID";
                }

                grdProcedures.CellValueChanged += new DataGridViewCellEventHandler(grdProcedures_CellValueChanged);
                grdProcedures.CurrentCellDirtyStateChanged += new EventHandler(grdProcedures_CurrentCellDirtyStateChanged);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void grdProcedures_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (grdProcedures.IsCurrentCellDirty)
            {
                // This fires the cell value changed handler below
                grdProcedures.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }
        private void grdProcedures_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == grdProcedures.Columns["clmProcType"].Index)
                {
                    int ProcTypeID = 0;
                    if (int.TryParse(grdProcedures.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString(), out ProcTypeID))
                    {
                        if (ProcTypeID == ProcType.Mail.GetHashCode())
                        {
                            grdProcedures.Rows[e.RowIndex].Cells["clmMailTemplateID"].ReadOnly = false;
                        }
                        else
                        {
                            grdProcedures.Rows[e.RowIndex].Cells["clmMailTemplateID"].ReadOnly = true;
                            grdProcedures.Rows[e.RowIndex].Cells["clmMailTemplateID"].Value = null;
                        }
                    }
                }
                else if (e.ColumnIndex == grdProcedures.Columns["clmReadExecDetails"].Index)
                {
                    bool ReadExecDetails = Convert.ToBoolean(grdProcedures.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                    grdProcedures.Rows[e.RowIndex].Cells["clmExecDetailsQry"].ReadOnly = !ReadExecDetails;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void grdProcedures_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                MessageBox.Show("Exec query");
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void grdProcedures_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            try
            {
                for (int i = 0; i < grdProcedures.Rows.Count; i++)
                {
                    grdProcedures.Rows[i].Cells["clmSeq"].Value = i + 1;
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
                if (grdProcedures.SelectedRows.Count == 1)
                {
                    int selectedIndex = grdProcedures.SelectedRows[0].Index;
                    if (selectedIndex < grdProcedures.Rows.Count - 1)
                    {
                        int currentSeq = int.Parse(grdProcedures.Rows[selectedIndex].Cells["clmSeq"].Value.ToString());
                        int item1ID = int.Parse(grdProcedures.Rows[selectedIndex + 1].Cells["clmSeq"].Value.ToString());
                        
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}
