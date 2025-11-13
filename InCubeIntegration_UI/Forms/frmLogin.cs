using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using InCubeLibrary;
using InCubeIntegration_BL;

namespace InCubeIntegration_UI
{
    public partial class frmLogin : Form
    {
        ApplicationManager AppManager;
        string userName = "", password = "";
        public frmLogin(string UserName, string Password, ApplicationManager _appManager)
        {
            InitializeComponent();
            userName = UserName;
            password = Password;
            AppManager = _appManager;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }

        private void frmLogin_Load(object sender, EventArgs e)
        {
            try
            {
                //Tools.TestMouath2("VIEW_TEST", "0000", "Sonic");
               // string sessionID = Tools.TestMouath2("VIEW_TEST", "0000", "Sonic");
                //this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
                lblVersion.Text = "Ver. " + CoreGeneral.Common.GeneralConfigurations.AppVersion;
                string[] images = Directory.GetFiles(Application.StartupPath, "*.png");
                if (images != null && images.Length > 0)
                {
                    picLogo.Image = Image.FromFile(images[0]);
                }
                txtUserName.Text = userName;
                txtUserName.SelectAll();
                if (CoreGeneral.Common.IsTesting)
                    txtPassword.Text = password;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
                Login();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            Login();
        }

        private void txtUserName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
                Login();
        }

        private void Login()
        {
            try
            {
                if (txtUserName.Text.Trim().Equals(string.Empty))
                {
                    MessageBox.Show("Enter User Name ..");
                    txtUserName.Focus();
                    return;
                }
                if (txtPassword.Text.Trim().Equals(string.Empty))
                {
                    MessageBox.Show("Enter Password ..");
                    txtPassword.Focus();
                    return;
                }
                
                switch (AppManager.Login(txtUserName.Text.Trim(), txtPassword.Text.Trim(), LoginType.User))
                {
                    case Result.Success:
                        this.DialogResult = System.Windows.Forms.DialogResult.OK;
                        this.Close();
                        break;
                    case Result.Invalid:
                        MessageBox.Show("Invalid username / Password !!");
                        break;
                    case Result.InActive:
                        MessageBox.Show("User is inactive !!");
                        break;
                    case Result.Failure:
                        MessageBox.Show("Login failed !!");
                        break;
                    case Result.LoggedIn:
                        MessageBox.Show("User is logged in !!");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}
