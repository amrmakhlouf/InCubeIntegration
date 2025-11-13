using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.Data;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmMailConfiguration : Form
    {
        MailManager mailManager;
        public frmMailConfiguration()
        {
            InitializeComponent();
            mailManager = new MailManager();
        }

        private void frmMailConfiguration_Load(object sender, EventArgs e)
        {
            this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
            FillSenderProfiles();
            FillMailTemplates();
        }

        private void FillSenderProfiles()
        {
            try
            {
                lsvSenderProfile.Items.Clear();
                DataTable dtSenderProfiles = new DataTable();
                Result res = mailManager.GetActiveSenderProfiles(ref dtSenderProfiles);
                if (res == Result.Success)
                {
                    for (int i = 0; i < dtSenderProfiles.Rows.Count; i++)
                    {
                        string[] values = new string[6];
                        values[0] = dtSenderProfiles.Rows[i]["ProfileName"].ToString();
                        values[1] = dtSenderProfiles.Rows[i]["Host"].ToString();
                        values[2] = dtSenderProfiles.Rows[i]["Port"].ToString();
                        values[3] = dtSenderProfiles.Rows[i]["MailAddress"].ToString();
                        values[4] = dtSenderProfiles.Rows[i]["DisplayName"].ToString();
                        values[5] = dtSenderProfiles.Rows[i]["EnableSSL"].ToString();
                        ListViewItem lsvItem = new ListViewItem(values);
                        lsvItem.Tag = dtSenderProfiles.Rows[i]["ID"].ToString();
                        lsvSenderProfile.Items.Add(lsvItem);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void FillMailTemplates()
        {
            try
            {
                lsvTemplates.Items.Clear();
                DataTable dtMailTemplatesDetails = new DataTable();
                Result res = mailManager.GetActiveMailTemplates(ref dtMailTemplatesDetails);
                if (res == Result.Success)
                {
                    DataTable dtMailTemplates = dtMailTemplatesDetails.DefaultView.ToTable(true, new string[] { "TemplateName", "ProfileName", "MailTemplateID", "Subject" });
                    for (int i = 0; i < dtMailTemplates.Rows.Count; i++)
                    {
                        string[] values = new string[4];
                        values[0] = dtMailTemplates.Rows[i]["TemplateName"].ToString();
                        values[1] = dtMailTemplates.Rows[i]["ProfileName"].ToString();
                        values[2] = dtMailTemplates.Rows[i]["Subject"].ToString();
                        string Recipients = "";
                        foreach (DataRow dr in dtMailTemplatesDetails.Select("MailTemplateID = " + dtMailTemplates.Rows[i]["MailTemplateID"].ToString()))
                            Recipients += dr["RecipientAddress"] + "; ";
                        values[3] = Recipients.Substring(0, Recipients.Length - 2);
                        ListViewItem lsvItem = new ListViewItem(values);
                        lsvItem.Tag = dtMailTemplates.Rows[i]["MailTemplateID"].ToString();
                        lsvTemplates.Items.Add(lsvItem);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnAddProfile_Click(object sender, EventArgs e)
        {
            try
            {
                frmAddEditSenderProfile frm = new frmAddEditSenderProfile(-1, mailManager);
                frm.ShowDialog();
                FillSenderProfiles();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnEditProfile_Click(object sender, EventArgs e)
        {
            try
            {
                if (lsvSenderProfile.SelectedItems.Count == 1)
                {
                    int ID = Convert.ToInt16(lsvSenderProfile.SelectedItems[0].Tag);
                    frmAddEditSenderProfile frm = new frmAddEditSenderProfile(ID, mailManager);
                    frm.ShowDialog();
                    FillSenderProfiles();
                }
                else
                {
                    MessageBox.Show("Select sender profile to edit!!");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnDeleteProfile_Click(object sender, EventArgs e)
        {
            try
            {
                if (lsvSenderProfile.SelectedItems.Count == 1)
                {
                    int ID = Convert.ToInt16(lsvSenderProfile.SelectedItems[0].Tag);
                    if (MessageBox.Show("Are you sure you want to delete selected sender profile?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        if (mailManager.DeleteSenderProfile(ID) == Result.Success)
                        {
                            MessageBox.Show("Sender profile deleted ..");
                            FillSenderProfiles();
                        }
                        else
                        {
                            MessageBox.Show("Deleting profile failed!!");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Select sender profile to delete!!");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnAddTemplate_Click(object sender, EventArgs e)
        {
            try
            {
                frmAddEditMailTemplate frm = new frmAddEditMailTemplate(-1, mailManager);
                frm.ShowDialog();
                FillMailTemplates();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnEditTemplate_Click(object sender, EventArgs e)
        {
            try
            {
                if (lsvTemplates.SelectedItems.Count == 1)
                {
                    int ID = Convert.ToInt16(lsvTemplates.SelectedItems[0].Tag);
                    frmAddEditMailTemplate frm = new frmAddEditMailTemplate(ID, mailManager);
                    frm.ShowDialog();
                    FillMailTemplates();
                }
                else
                {
                    MessageBox.Show("Select mail template to edit!!");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnDeleteTemplate_Click(object sender, EventArgs e)
        {
            try
            {
                if (lsvTemplates.SelectedItems.Count == 1)
                {
                    int ID = Convert.ToInt16(lsvTemplates.SelectedItems[0].Tag);
                    if (MessageBox.Show("Are you sure you want to delete selected mail template?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        if (mailManager.DeleteMailTemplate(ID) == Result.Success)
                        {
                            MessageBox.Show("Mail template deleted ..");
                            FillMailTemplates();
                        }
                        else
                        {
                            MessageBox.Show("Deleting template failed!!");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Select mail template to delete!!");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}