using InCubeIntegration_BL;
using InCubeIntegration_UI;
using InCubeLibrary;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace InCubeIntegration_App
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.ApplicationExit += new EventHandler(Application_ApplicationExit);

            //Load Application Version
            string AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            long appVersion = CoreGeneral.Common.GetLongVersionNumber(AssemblyVersion);
            CoreGeneral.Common.GeneralConfigurations.AppVersion = CoreGeneral.Common.FormatVersionNumber(AssemblyVersion);

            //For making a build, force the code to enter the if body below
            if (AssemblyVersion == "12345")
            {
                AppBase.CopySiteFiles("Hassani", BuildMode.Live);
                return;
            }

            if (AssemblyVersion == "12345")
            {
                CoreGeneral.Common.IsTesting = true;
            }

            //Open DB Connection
            ApplicationManager AppManager = new ApplicationManager();
            //string text = "Password=sonicbo@123;Persist Security Info=True;User ID=sonicbo;Initial Catalog=SonicDataLive;Data Source=172.16.44.11";
            //AppManager.EncryptText(ref text);
            if (!AppManager.ConnectionOpened)
            {
                MessageBox.Show("Error opening database connection");
                return;
            }

            //Run Scripts
            if (!CoreGeneral.Common.IsTesting)
            {
                //Get App,DB and Client versions
                long currentAppVersion = 0;
                int dbVersion = 0, clientVersion = 0;
                if (AppManager.GetIntegrationVersions(out currentAppVersion, out dbVersion, out clientVersion) != Result.Success)
                {
                    MessageBox.Show("Error reading application version");
                    return;
                }
                //Check Version
                if (currentAppVersion > appVersion)
                {
                    MessageBox.Show("This integration build version is older than database version, contact your IT administrator or InCube to get the latest one ..");
                    //using (WebClient client = new WebClient())
                    //{
                    //    client.DownloadFile(@"http://invan.onefoods.com/incubeservice3.1.99/incubelog.txt", CoreGeneral.Common.StartupPath + "\\ff.txt");
                    //}
                    return;
                }
                AppManager.UpdateAppVersion(appVersion);

                //Run Customer Scripts
                AppManager.RunCustomerScripts(Application.StartupPath, dbVersion, clientVersion);
                if (currentAppVersion < appVersion)
                {
                    AppManager.UpdateAppVersion(appVersion);
                }
            }

            //Load Configurations
            using (ConfigurationsManger configManager = new ConfigurationsManger())
            {
                configManager.LoadConfigurations();
            }
            //Create Windows Service Installation Files
            if (CoreGeneral.Common.GeneralConfigurations.WindowsServiceEnabled)
            {
                AppBase.CreateInstalltionFile(true);
                AppBase.CreateInstalltionFile(false);
            }
            
            Application.EnableVisualStyles();
            //Ask for login
            if (CoreGeneral.Common.GeneralConfigurations.LoginRequired)
            {
                frmLogin _frmLogin = new frmLogin(Properties.Settings.Default.UserName, CoreGeneral.Common.IsTesting ? Properties.Settings.Default.Password : "", AppManager);
                if (_frmLogin.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                Properties.Settings.Default.UserName = CoreGeneral.Common.CurrentSession.UserName;
                if (CoreGeneral.Common.IsTesting)
                    Properties.Settings.Default.Password = CoreGeneral.Common.CurrentSession.Password;
                Properties.Settings.Default.Save();
                //frmLogin _frmLogin = new frmLogin("admin", "123", AppManager);
                //if (_frmLogin.ShowDialog() != DialogResult.OK)
                //{
                //    return;
                //}
                
                frmMain _frmMain = new frmMain();
                Application.Run(_frmMain);
            }
            else
            {
                AppManager.Login("admin", "", LoginType.NoLoginForm);

                if (CoreGeneral.Common.GeneralConfigurations.OrganizationOriented && Properties.Settings.Default.OrganizationID == -1)
                {
                    frmSelectOrganization frmOrgSel = new frmSelectOrganization();
                    if (frmOrgSel.ShowDialog() != DialogResult.OK)
                        return;
                    Properties.Settings.Default.OrganizationID = frmOrgSel.OrganizationID;
                    Properties.Settings.Default.Save();
                    Properties.Settings.Default.Reload();
                }

                switch (CoreGeneral.Common.GeneralConfigurations.DefaultMenu)
                {
                    case Menus.Manual_Integration:
                        frmIntegration frmInt = new frmIntegration();
                        frmInt.OrganizationID = Properties.Settings.Default.OrganizationID;
                        Application.Run(frmInt);
                        break;
                    case Menus.Excel_Import:
                        frmImportExcel frmExcel = new frmImportExcel();
                        Application.Run(frmExcel);
                        break;
                    case Menus.Employees_Importing:
                        frmEmployeesImportingMain frmEmployee = new frmEmployeesImportingMain();
                        Application.Run(frmEmployee);
                        break;
                    case Menus.Load_Request:
                        frmLoadRequestImport _frmLoadRequest = new frmLoadRequestImport();
                        Application.Run(_frmLoadRequest);
                        break;
                    case Menus.Targets_Importing:
                        frmImportTargets _frmTargets = new frmImportTargets(Properties.Settings.Default.OrganizationID);
                        Application.Run(_frmTargets);
                        break;
                }
            }
        }
        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            ApplicationManager.LogOut();
        }
    }
}