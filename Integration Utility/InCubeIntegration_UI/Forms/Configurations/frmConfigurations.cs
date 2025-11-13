using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmConfigurations : Form
    {
        ConfigurationsManger configManager;
        FilesManager filesManager;
        DataTable dtConfigs = new DataTable();
        ConfigurationType configType;
        int ConfigurationID = 0;
        public frmConfigurations()
        {
            InitializeComponent();
            this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
            configManager = new ConfigurationsManger();
            filesManager = new FilesManager(true);
        }

        private void frmConfigurations_Load(object sender, EventArgs e)
        {
            try
            {
                FillConfigurations();
                FillFilesManagementJobs();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
        }
        private void FillConfigurations()
        {
            try
            {
                dtConfigs = new DataTable();
                bool ConfigOnOrgLevel = false;
                if (configManager.GetListOfEditableConfiguraitons(ref dtConfigs, ref ConfigOnOrgLevel) == Result.Success)
                {
                    int rowHeight = tblConfig.Height / 2 + 1;
                    if (CoreGeneral.Common.GeneralConfigurations.OrganizationOriented || ConfigOnOrgLevel)
                    {
                        string[] orgs = CoreGeneral.Common.userPrivileges.Organizations.Split(new char[] { ',' });
                        tblConfig.Height = rowHeight * (2 + orgs.Length);
                        for (int i = 0; i < orgs.Length; i++)
                        {
                            tblConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
                        }
                    }

                    cmbConfiguration.DataSource = dtConfigs;
                    cmbConfiguration.DisplayMember = "KeyName";
                    cmbConfiguration.ValueMember = "ConfigurationID";
                    cmbConfiguration_SelectedIndexChanged(null, null);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
        }
        private void FillFilesManagementJobs()
        {
            try
            {
                lsvFilesJobs.Items.Clear();
                DataTable dtFilesJobs = new DataTable();
                Result res = filesManager.GetActiveFilesJobs(ref dtFilesJobs);
                if (res == Result.Success)
                {
                    for (int i = 0; i < dtFilesJobs.Rows.Count; i++)
                    {
                        string[] values = new string[6];
                        values[0] = dtFilesJobs.Rows[i]["JobID"].ToString();
                        values[1] = dtFilesJobs.Rows[i]["JobName"].ToString();
                        values[2] = dtFilesJobs.Rows[i]["JobType"].ToString();
                        values[3] = dtFilesJobs.Rows[i]["SourceFolder"].ToString();
                        values[4] = dtFilesJobs.Rows[i]["FileExtension"].ToString();
                        ListViewItem lsvItem = new ListViewItem(values);
                        lsvItem.Tag = dtFilesJobs.Rows[i]["JobID"].ToString();
                        lsvFilesJobs.Items.Add(lsvItem);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void cmbConfiguration_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                tblConfig.Controls.Clear();
                if (cmbConfiguration.ValueMember != null && cmbConfiguration.ValueMember != "")
                {
                    ConfigurationID = int.Parse(cmbConfiguration.SelectedValue.ToString());
                    configType = (ConfigurationType)Convert.ToInt16(dtConfigs.Select("ConfigurationID = " + ConfigurationID)[0]["DataType"]);
                    DataTable dtConfiguValues = new DataTable();
                    Label lblOrg = new Label();
                    lblOrg.Text = "Organization";
                    tblConfig.Controls.Add(lblOrg, 0, 0);
                    Label lblValue = new Label();
                    lblValue.Text = "Value";
                    tblConfig.Controls.Add(lblValue, 1, 0);
                    
                    if (configManager.GetConfigurationValues(ConfigurationID, ref dtConfiguValues) == Result.Success)
                    {
                        for (int i = 0; i < dtConfiguValues.Rows.Count; i++)
                        {
                            Label lblOrgCode = new Label();
                            lblOrgCode.Text = dtConfiguValues.Rows[i]["OrganizationCode"].ToString();
                            lblOrgCode.Tag = dtConfiguValues.Rows[i]["OrganizationID"];
                            tblConfig.Controls.Add(lblOrgCode, 0, i + 1);

                            switch (configType)
                            {
                                case ConfigurationType.Boolean:
                                    ComboBox cmb = new ComboBox();
                                    cmb.DropDownStyle = ComboBoxStyle.DropDownList;
                                    cmb.Items.Add("true");
                                    cmb.Items.Add("false");
                                    cmb.Tag = dtConfiguValues.Rows[i]["KeyValue"];
                                    cmb.SelectedItem = dtConfiguValues.Rows[i]["KeyValue"].ToString().ToLower();
                                    tblConfig.Controls.Add(cmb, 1, i + 1);
                                    break;
                                case ConfigurationType.Color:
                                    Button btn = new Button();
                                    btn.Dock = DockStyle.Fill;
                                    btn.Tag = dtConfiguValues.Rows[i]["KeyValue"];
                                    btn.BackColor = Color.FromArgb(Convert.ToInt32(dtConfiguValues.Rows[i]["KeyValue"]));
                                    btn.Click += Btn_Click;
                                    tblConfig.Controls.Add(btn, 1, i + 1);
                                    break;
                                case ConfigurationType.Long:
                                case ConfigurationType.String:
                                    TextBox txt = new TextBox();
                                    if (configType == ConfigurationType.Long)
                                    {
                                        txt.KeyPress += Txt_KeyPress;
                                    }
                                    txt.Dock = DockStyle.Fill;
                                    txt.Tag = dtConfiguValues.Rows[i]["KeyValue"];
                                    txt.Text = dtConfiguValues.Rows[i]["KeyValue"].ToString();
                                    tblConfig.Controls.Add(txt, 1, i + 1);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
        }

        private void Txt_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                char c = e.KeyChar;
                Keys key = (Keys)c;
                string preStrValue = ((TextBox)sender).Text;
                if (char.IsDigit(c) || key == Keys.Back)
                {
                    e.Handled = false;
                }
                else
                {
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void Btn_Click(object sender, EventArgs e)
        {
            try
            {
                Button btn = (Button)sender;
                ColorDialog cd = new ColorDialog();
                cd.Color = Color.FromArgb(int.Parse(btn.Tag.ToString()));
                if (cd.ShowDialog() == DialogResult.OK)
                    btn.BackColor = cd.Color;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
        }

        private void btnSaveConfigChanges_Click(object sender, EventArgs e)
        {
            try
            {
                string OriginalValue = "", NewValue = "";
                int OrgID = 0;
                for (int i = 1; i < tblConfig.RowStyles.Count; i++)
                {
                    Label lbl = (Label)tblConfig.GetControlFromPosition(0, i);
                    OrgID = int.Parse(lbl.Tag.ToString());

                    switch (configType)
                    {
                        case ConfigurationType.Boolean:
                            ComboBox cmb = (ComboBox)tblConfig.GetControlFromPosition(1, i);
                            OriginalValue = cmb.Tag.ToString();
                            NewValue = cmb.SelectedItem.ToString();
                            break;
                        case ConfigurationType.Color:
                            Button btn = (Button)tblConfig.GetControlFromPosition(1, i);
                            OriginalValue = btn.Tag.ToString();
                            NewValue = btn.BackColor.ToArgb().ToString();
                            break;
                        case ConfigurationType.Long:
                        case ConfigurationType.String:
                            TextBox txt = (TextBox)tblConfig.GetControlFromPosition(1, i);
                            OriginalValue = txt.Tag.ToString();
                            NewValue = txt.Text;
                            break;
                    }

                    if (OriginalValue != NewValue)
                    {
                        if (configManager.AddEditConfigurationValue(ConfigurationID, OrgID, NewValue) != Result.Success)
                        {
                            MessageBox.Show("Failure!!");
                            return;
                        }
                    }
                }
                MessageBox.Show("Saved Successfully ..");
                configManager.LoadConfigurations();
                cmbConfiguration_SelectedIndexChanged(null, null);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
                MessageBox.Show("Failure!!");
            }
        }

        private void btnAddFilesJob_Click(object sender, EventArgs e)
        {
            try
            {
                frmAddEditFilesManagementJob frm = new frmAddEditFilesManagementJob(-1, filesManager);
                frm.ShowDialog();
                FillFilesManagementJobs();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnEditFilesJob_Click(object sender, EventArgs e)
        {
            try
            {
                if (lsvFilesJobs.SelectedItems.Count == 1)
                {
                    int ID = Convert.ToInt16(lsvFilesJobs.SelectedItems[0].Tag);
                    frmAddEditFilesManagementJob frm = new frmAddEditFilesManagementJob(ID, filesManager);
                    frm.ShowDialog();
                    FillFilesManagementJobs();
                }
                else
                {
                    MessageBox.Show("Select files job to edit!!");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnDeleteFilesJob_Click(object sender, EventArgs e)
        {
            try
            {
                if (lsvFilesJobs.SelectedItems.Count == 1)
                {
                    int ID = Convert.ToInt16(lsvFilesJobs.SelectedItems[0].Tag);
                    if (MessageBox.Show("Are you sure you want to delete selected files job?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        if (filesManager.DeleteFilesJob(ID) == Result.Success)
                        {
                            MessageBox.Show("Files job deleted ..");
                            FillFilesManagementJobs();
                        }
                        else
                        {
                            MessageBox.Show("Deleting job failed!!");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Select files job to delete!!");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}
