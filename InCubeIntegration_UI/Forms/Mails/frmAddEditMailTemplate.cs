using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmAddEditMailTemplate : Form
    {
        int _id = -1;
        MailManager MailManager;
        public frmAddEditMailTemplate(int ID, MailManager _mailManager)
        {
            InitializeComponent();
            _id = ID;
            MailManager = _mailManager;
        }

        private void frmAddEditMailTemplate_Load(object sender, EventArgs e)
        {
            try
            {
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                FillSenderProfiles();
                if (_id == -1)
                {
                    _id = MailManager.GetMaxMailTemplateID();
                    txtID.Text = _id.ToString();
                }
                else
                {
                    string Name = "", Subject = "", Header = "", Footer = "";
                    int SenderProfileID = 0;
                    List<string> To = new List<string>(), CC = new List<string>();
                    Result res = MailManager.GetMailTemplateDetails(_id, ref Name, ref SenderProfileID, ref Subject, ref Header, ref Footer, ref To, ref CC);
                    if (res == Result.Success)
                    {
                        txtID.Text = _id.ToString();
                        txtName.Text = Name;
                        cmbSenderProfile.SelectedValue = SenderProfileID;
                        txtSubject.Text = Subject;
                        txtHeader.Text = Header;
                        txtFooter.Text = Footer;
                        foreach (string toMail in To)
                            txtTo.AppendText(toMail + "\r\n");
                        foreach (string ccMail in CC)
                            txtCC.AppendText(ccMail + "\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void FillSenderProfiles()
        {
            try
            {
                DataTable dtSenderProfiles = new DataTable();
                MailManager.GetActiveSenderProfiles(ref dtSenderProfiles);
                cmbSenderProfile.DataSource = dtSenderProfiles;
                cmbSenderProfile.DisplayMember = "ProfileName";
                cmbSenderProfile.ValueMember = "ID";
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
                string Name = "", Subject = "", Header = "", Footer = "";
                int SenderProfileID = 0;
                List<string> To = new List<string>(), CC = new List<string>();

                if (txtName.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Fill name!!");
                    txtName.Focus();
                    return;
                }
                Name = txtName.Text.Trim();

                if (cmbSenderProfile.SelectedValue == null)
                {
                    MessageBox.Show("Select sender profile!!");
                    cmbSenderProfile.Focus();
                    return;
                }
                if (!int.TryParse(cmbSenderProfile.SelectedValue.ToString(), out SenderProfileID))
                {
                    MessageBox.Show("Invalid sender profile!!");
                    cmbSenderProfile.Focus();
                    return;
                }

                if (txtSubject.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Fill subject!!");
                    txtSubject.Focus();
                    return;
                }
                Subject = txtSubject.Text.Trim();

                if (txtHeader.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Fill header!!");
                    txtHeader.Focus();
                    return;
                }
                Header = txtHeader.Text.Trim();

                if (txtFooter.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Fill footer!!");
                    txtFooter.Focus();
                    return;
                }
                Footer = txtFooter.Text.Trim();

                string[] ToArr = txtTo.Text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string toMail in ToArr)
                {
                    if (MailManager.IsValidEmail(toMail))
                    {
                        To.Add(toMail);
                    }
                    else
                    {
                        MessageBox.Show("Invalid mail address: " + toMail);
                        return;
                    }
                }
                if (To.Count == 0)
                {
                    MessageBox.Show("Fill To Recipient(s)!!");
                    txtTo.Focus();
                    return;
                }

                string[] CCArr = txtCC.Text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string ccMail in CCArr)
                {
                    if (MailManager.IsValidEmail(ccMail))
                    {
                        CC.Add(ccMail);
                    }
                    else
                    {
                        MessageBox.Show("Invalid mail address: " + ccMail);
                        return;
                    }
                }

                Result res = MailManager.SaveMailTemplate(_id, Name, SenderProfileID, Subject, Header, Footer, To, CC);
                if (res == Result.Success)
                {
                    MessageBox.Show("Mail template saved successfully ..");
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Failed in saving mail template!!");
                }

                FillSenderProfiles();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}