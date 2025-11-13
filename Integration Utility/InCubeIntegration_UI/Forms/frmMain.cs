using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmMain : Form
    {
        bool logOutButtonClicked = false;
        int selectedOrgID = 1;
        string selectedOrgCode = "";

        public frmMain()
        {
            InitializeComponent();
        }

        private void RunIntegrationConfigurations(object sender, EventArgs e)
        {
            try
            {
                if (Application.OpenForms["frmConfigurations"] == null)
                {
                    frmConfigurations frm = new frmConfigurations();
                    frm.Show();
                }
                else
                {
                    Application.OpenForms["frmConfigurations"].Focus();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void RunTargetsImporting(object sender, EventArgs e)
        {
            try
            {
                if (Application.OpenForms["frmImportTargets"] == null)
                {
                    if (SelectUserOrg() == Result.Success)
                    {
                        frmImportTargets frm = new frmImportTargets(selectedOrgID);
                        frm.Show();
                    }
                }
                else
                {
                    Application.OpenForms["frmImportTargets"].Focus();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void RunEmployeesImporting(object sender, EventArgs e)
        {
            try
            {
                if (Application.OpenForms["frmEmployeesImportingMain"] == null)
                {
                    frmEmployeesImportingMain frm = new frmEmployeesImportingMain();
                    frm.Show();
                }
                else
                {
                    Application.OpenForms["frmEmployeesImportingMain"].Focus();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void RunOperationsMonitoring(object sender, EventArgs e)
        {
            try
            {
                if (Application.OpenForms["frmOperationsMonitoring"] == null)
                {
                    frmOperationsMonitoring frm = new frmOperationsMonitoring();
                    frm.Show();
                }
                else
                {
                    Application.OpenForms["frmOperationsMonitoring"].Focus();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void RunUsersAccess(object sender, EventArgs e)
        {
            try
            {
                if (Application.OpenForms["frmUserAccess"] == null)
                {
                    frmUserAccess frm = new frmUserAccess(frmUserAccess.FormMode.User);
                    frm.Show();
                }
                else
                {
                    Application.OpenForms["frmUserAccess"].Focus();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void RunSchedulesManagement(object sender, EventArgs e)
        {
            try
            {
                if (Application.OpenForms["frmSchedulesManagement"] == null)
                {
                    frmSchedulesManagement frm = new frmSchedulesManagement();
                    frm.Show();
                }
                else
                {
                    Application.OpenForms["frmSchedulesManagement"].Focus();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void RunLoadRequestImport(object sender, EventArgs e)
        {
            try
            {
                if (Application.OpenForms["frmSchedulesManagement"] == null)
                {
                    frmSchedulesManagement frm = new frmSchedulesManagement();
                    frm.Show();
                }
                else
                {
                    Application.OpenForms["frmSchedulesManagement"].Focus();
                }

                if (CoreGeneral.Common.GeneralConfigurations.SiteSymbol.ToLower() == "delmontejo")
                {
                    if (Application.OpenForms["frmLoadRequestImportDelmonteJO"] == null)
                    {
                        frmLoadRequestImportDelmonteJO frm = new frmLoadRequestImportDelmonteJO();
                        frm.Show();
                    }
                    else
                    {
                        Application.OpenForms["frmLoadRequestImportDelmonteJO"].Focus();
                    }
                }
                else
                {
                    if (Application.OpenForms["frmLoadRequestImport"] == null)
                    {
                        frmLoadRequestImport frm = new frmLoadRequestImport();
                        frm.Show();
                    }
                    else
                    {
                        Application.OpenForms["frmLoadRequestImport"].Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void RunExcelImport(object sender, EventArgs e)
        {
            try
            {
                if (Application.OpenForms["frmImportExcel"] == null)
                {
                    frmImportExcel frm = new frmImportExcel();
                    frm.Show();
                }
                else
                {
                    Application.OpenForms["frmImportExcel"].Focus();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void RunRoadNetIntegration(object sender, EventArgs e)
        {
            try
            {
                if (Application.OpenForms["frmRoadNetIntegration"] == null)
                {
                    frmRoadNetIntegration frm = new frmRoadNetIntegration();
                    frm.Show();
                }
                else
                {
                    Application.OpenForms["frmRoadNetIntegration"].Focus();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void RunProcessReturns(object sender, EventArgs e)
        {
            try
            {
                if (Application.OpenForms["frmProcessReturns"] == null)
                {
                    frmProcessReturns frm = new frmProcessReturns();
                    frm.Show();
                }
                else
                {
                    Application.OpenForms["frmProcessReturns"].Focus();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void RunMainConfiguration(object sender, EventArgs e)
        {
            try
            {
                if (Application.OpenForms["frmMailConfiguration"] == null)
                {
                    frmMailConfiguration frm = new frmMailConfiguration();
                    frm.Show();
                }
                else
                {
                    Application.OpenForms["frmMailConfiguration"].Focus();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void RunTransactionsManagement(object sender, EventArgs e)
        {
            try
            {
                if (Application.OpenForms["frmTransactionsManagement"] == null)
                {
                    frmTransactionsManagement frm = new frmTransactionsManagement();
                    frm.Show();
                }
                else
                {
                    Application.OpenForms["frmTransactionsManagement"].Focus();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private Result SelectUserOrg()
        {
            try
            {
                selectedOrgID = 1;
                selectedOrgCode = "";
                if (CoreGeneral.Common.GeneralConfigurations.OrganizationOriented && CoreGeneral.Common.userPrivileges.Organizations.Contains(","))
                {
                    frmSelectOrganization frmOrg = new frmSelectOrganization();
                    if (frmOrg.ShowDialog() != DialogResult.OK)
                        return Result.NoRowsFound;
                    selectedOrgID = frmOrg.OrganizationID;
                    selectedOrgCode = frmOrg.OrganizationCode;
                }
                else if (CoreGeneral.Common.userPrivileges.UserOrganizationID > 0)
                {
                    selectedOrgID = CoreGeneral.Common.userPrivileges.UserOrganizationID;
                    selectedOrgCode = CoreGeneral.Common.userPrivileges.UserOrgCode;
                }
                return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                return Result.Failure;
            }
        }
        private void RunManualIntegration(object sender, EventArgs e)
        {
            try
            {
                if (SelectUserOrg() == Result.Success)
                {
                    frmIntegration _frmIntegration = new frmIntegration();
                    _frmIntegration.OrganizationID = selectedOrgID;
                    _frmIntegration.Show();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void frmMain_Load(object sender, EventArgs e)
        {
            try
            {
                lblWelcome.Text += CoreGeneral.Common.CurrentSession.EmployeeName + ",";
                lblLoginTime.Text += CoreGeneral.Common.CurrentSession.LoginTime.ToString();
                int index = 1;
                foreach (KeyValuePair<Menus, string> MenuPair in CoreGeneral.Common.userPrivileges.MenusAccess)
                {
                    switch (MenuPair.Key)
                    {
                        case Menus.Manual_Integration:
                            AddButton(Properties.Resources.Manual_Integration, MenuPair.Value, index++).MouseClick += new MouseEventHandler(RunManualIntegration);
                            break;
                        case Menus.Schedules_Management:
                            AddButton(Properties.Resources.Schedule, MenuPair.Value, index++).MouseClick += new MouseEventHandler(RunSchedulesManagement);
                            break;
                        case Menus.Users_Access:
                            AddButton(Properties.Resources.user_access, MenuPair.Value, index++).MouseClick += new MouseEventHandler(RunUsersAccess);
                            break;
                        case Menus.Operations_Monitoring:
                            AddButton(Properties.Resources.Monitoring_Integration, MenuPair.Value, index++).MouseClick += new MouseEventHandler(RunOperationsMonitoring);
                            break;
                        case Menus.Employees_Importing:
                            AddButton(Properties.Resources.Salesmen, MenuPair.Value, index++).MouseClick += new MouseEventHandler(RunEmployeesImporting);
                            break;
                        case Menus.Targets_Importing:
                            AddButton(Properties.Resources.Customers_Targets, MenuPair.Value, index++).MouseClick += new MouseEventHandler(RunTargetsImporting);
                            break;
                        case Menus.Integration_Configurations:
                            AddButton(Properties.Resources.Config, MenuPair.Value, index++).MouseClick += new MouseEventHandler(RunIntegrationConfigurations);
                            break;
                        case Menus.Load_Request:
                            AddButton(Properties.Resources.LoadRequest, MenuPair.Value, index++).MouseClick += new MouseEventHandler(RunLoadRequestImport);
                            break;
                        case Menus.Excel_Import:
                            AddButton(Properties.Resources.ExcelImport, MenuPair.Value, index++).MouseClick += new MouseEventHandler(RunExcelImport);
                            break;
                        case Menus.Transactions_Management:
                            AddButton(Properties.Resources.Manual_Integration, MenuPair.Value, index++).MouseClick += new MouseEventHandler(RunTransactionsManagement);
                            break;
                        case Menus.Mail_Configuration:
                            AddButton(Properties.Resources.mail, MenuPair.Value, index++).MouseClick += new MouseEventHandler(RunMainConfiguration);
                            break;
                        case Menus.Process_Returns:
                            AddButton(Properties.Resources.Process_Returns, MenuPair.Value, index++).MouseClick += new MouseEventHandler(RunProcessReturns);
                            break;
                        case Menus.RoadNet_Integration:
                            AddButton(Properties.Resources.RoadNet, MenuPair.Value, index++).MouseClick += new MouseEventHandler(RunRoadNetIntegration);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private PictureBox AddButton(Bitmap image, string Description, int Index)
        {
            PictureBox pic = new PictureBox();
            try
            {
                pic.Image = image;
                pic.SizeMode = PictureBoxSizeMode.StretchImage;
                pic.Dock = DockStyle.Fill;
                pic.Cursor = Cursors.Hand;
                toolTipInfo.SetToolTip(pic, Description);
                tableLayoutPanel1.Controls.Add(pic, 2 * ((Index - 1) % 4), 2 * ((Index - 1) / 4));
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
            return pic;
        }

        private void picLogOut_Click(object sender, EventArgs e)
        {
            if (LogOut() == Result.Success)
            {
                logOutButtonClicked = true;
                this.Close();
                Application.Exit();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (e.CloseReason == CloseReason.WindowsShutDown) return;

            // Confirm user wants to close
            if (!logOutButtonClicked && LogOut() != Result.Success)
            {
                e.Cancel = true;
            }
        }

        private Result LogOut()
        {
            if (MessageBox.Show("Are you sure you want to exit the Integration Utility?", "Confirm", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                return Result.Success;
            }
            else
            {
                return Result.Failure;
            }
        }
        int shiftPressCount = 0;
        DateTime lastPress = DateTime.Now;
        private void frmMain_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.ShiftKey)
                {
                    if (DateTime.Now - lastPress < new TimeSpan(0, 0, 1) && DateTime.Now - lastPress > new TimeSpan(0, 0, 0, 0, 200))
                    {
                        shiftPressCount++;
                    }
                    else
                    {
                        shiftPressCount = 1;
                    }
                    lastPress = DateTime.Now;
                    if (shiftPressCount == 7 || (CoreGeneral.Common.IsTesting && shiftPressCount == 3))
                    {
                        shiftPressCount = 0;
                        MessageBox.Show("Hey there InCuber!!");
                        frmAdminOptions frm = new frmAdminOptions();
                        frm.ShowDialog();
                    }
                }
                else
                {
                    shiftPressCount = 0;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}