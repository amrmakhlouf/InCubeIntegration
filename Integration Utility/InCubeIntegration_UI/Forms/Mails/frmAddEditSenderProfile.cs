using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using InCubeLibrary;
using InCubeIntegration_BL;

namespace InCubeIntegration_UI
{
    public partial class frmAddEditSenderProfile : Form
    {
        int _id = -1;
        MailManager MailManager;
        public frmAddEditSenderProfile(int ID, MailManager _mailManager)
        {
            InitializeComponent();
            _id = ID;
            MailManager = _mailManager;
        }

        private void frmAddEditSenderProfile_Load(object sender, EventArgs e)
        {
            try
            {
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                
                if (_id == -1)
                {
                    _id = MailManager.GetMaxSenderProfileID();
                    txtID.Text = _id.ToString();
                }
                else
                {
                    string Name = "", Host = "", MailAddress = "", DisplayName = "", Password = "";
                    int Port = 0;
                    bool EnableSSL = false;
                    Result res = MailManager.GetSenderProfileDetails(_id, ref Name, ref Host, ref MailAddress, ref DisplayName, ref Password, ref Port, ref EnableSSL);
                    if (res == Result.Success)
                    {
                        txtID.Text = _id.ToString();
                        txtName.Text = Name;
                        txtHost.Text = Host;
                        txtMailAddress.Text = MailAddress;
                        txtDisplayName.Text = DisplayName;
                        txtPassword.Text = Password;
                        txtPort.Text = Port.ToString();
                        cbEnableSSL.Checked = EnableSSL;
                    }
                }
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
                string Name = "", Host = "", MailAddress = "", DisplayName = "", Password = "";
                int Port = 0;
                bool EnableSSL = false;

                if (txtName.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Fill name!!");
                    txtName.Focus();
                    return;
                }
                Name = txtName.Text.Trim();

                if (txtHost.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Fill host!!");
                    txtHost.Focus();
                    return;
                }
                Host = txtHost.Text.Trim();

                if (txtPort.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Fill port!!");
                    txtPort.Focus();
                    return;
                }
                if (!int.TryParse(txtPort.Text.Trim(), out Port))
                {
                    MessageBox.Show("Invalid port number!!");
                    txtPort.Focus();
                    return;
                }

                if (txtMailAddress.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Fill mail address!!");
                    txtMailAddress.Focus();
                    return;
                }
                MailAddress = txtMailAddress.Text.Trim();

                if (txtDisplayName.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Fill display name!!");
                    txtDisplayName.Focus();
                    return;
                }
                DisplayName = txtDisplayName.Text.Trim();

                if (txtPassword.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Fill password!!");
                    txtPassword.Focus();
                    return;
                }
                Password = txtPassword.Text.Trim();

                EnableSSL = cbEnableSSL.Checked;

                string ErrorMessage = "";
                Result res = MailManager.SendTestMail(Name, Host, Port, MailAddress, DisplayName, Password, EnableSSL, ref ErrorMessage);
                if (res != Result.Success)
                {
                    MessageBox.Show("Wrong mail defintion, sending test mail failed with error:\r\n" + ErrorMessage);
                    return;
                }
                res = MailManager.SaveSenderProfile(_id, Name, Host, Port, MailAddress, DisplayName, Password, EnableSSL);
                if (res == Result.Success)
                {
                    MessageBox.Show("Sender profile saved successfully ..");
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Failed in saving sender profile!!");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}
