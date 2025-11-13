using InCubeLibrary;
using System;
using System.Windows.Forms;
using System.Drawing;

namespace InCubeIntegration_UI
{
    public partial class frmAdminOptions : Form
    {
        public frmAdminOptions()
        {
            InitializeComponent();
        }
        private void frmAdminOptions_Load(object sender, EventArgs e)
        {
            int index = 1;
            AddButton(Properties.Resources.Admin, "Configure Admin Menu Access", index++).MouseClick += new MouseEventHandler(ConfigureAdminMenusAccess);
            AddButton(Properties.Resources.Procedures, "Configure Field Procedures", index++).MouseClick += new MouseEventHandler(ConfigureFieldProcedures);
            AddButton(Properties.Resources.DataSheet, "Configure Import Excel Types", index++).MouseClick += new MouseEventHandler(ConfigureImportExcelTypes);
            AddButton(Properties.Resources.Transfer, "Configure Data Transfer Types", index++).MouseClick += new MouseEventHandler(ConfigureDataTransferTypes);
        }
        private void ConfigureFieldProcedures(object sender, EventArgs e)
        {
            try
            {
                frmFieldProcedures frm = new frmFieldProcedures();
                frm.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void ConfigureDataTransferTypes(object sender, EventArgs e)
        {
            try
            {
                frmDataTransferConfig frm = new frmDataTransferConfig();
                frm.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void ConfigureAdminMenusAccess(object sender, EventArgs e)
        {
            try
            {
                frmUserAccess frm = new frmUserAccess(frmUserAccess.FormMode.InCubeAdmin);
                frm.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void ConfigureImportExcelTypes(object sender, EventArgs e)
        {
            try
            {
                frmDesignInterface frm = new frmDesignInterface();
                frm.ShowDialog();
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
    }
}