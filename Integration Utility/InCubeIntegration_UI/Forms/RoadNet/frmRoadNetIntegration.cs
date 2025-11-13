using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmRoadNetIntegration : Form
    {
        public frmRoadNetIntegration()
        {
            InitializeComponent();
        }

        private void frmRoadNetIntegration_Load(object sender, EventArgs e)
        {
            try
            {
                int index = 1;
                foreach (KeyValuePair<Menus, string> MenuPair in CoreGeneral.Common.userPrivileges.MenusAccess)
                {
                    switch (MenuPair.Key)
                    {
                        case Menus.RoadNet_Export:
                            AddButton(Properties.Resources.Export, MenuPair.Value, index++).MouseClick += new MouseEventHandler(ExportToRadNet);
                            break;
                        case Menus.RoadNet_Import:
                            AddButton(Properties.Resources.Import, MenuPair.Value, index++).MouseClick += new MouseEventHandler(ImportFromRoadNet);
                            break;
                        case Menus.Route_Region:
                            AddButton(Properties.Resources.Routes, MenuPair.Value, index++).MouseClick += new MouseEventHandler(ConfigureRouteRegion);
                            break;
                        case Menus.SpecialInstructions:
                            AddButton(Properties.Resources.instructions, MenuPair.Value, index++).MouseClick += new MouseEventHandler(ModifySpecialInstructions);
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
        private void ExportToRadNet(object sender, EventArgs e)
        {
            try
            {
                frmRoadNetExport frm = new frmRoadNetExport(-1);
                if (!frm.RN_Conn_Opened)
                {
                    MessageBox.Show("Failure in establishing connection to RoadNet DB");
                }
                else
                {
                    frm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void ImportFromRoadNet(object sender, EventArgs e)
        {
            try
            {
                frmRoadNetImport frm = new frmRoadNetImport();
                if (!frm.RN_Conn_Opened)
                {
                    MessageBox.Show("Failure in establishing connection to RoadNet DB");
                }
                else
                {
                    frm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void ConfigureRouteRegion(object sender, EventArgs e)
        {
            try
            {
                frmRouteRegion frm = new frmRouteRegion();
                frm.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void ModifySpecialInstructions(object sender, EventArgs e)
        {
            try
            {
                frmRoadNetSpecialInstructions frm = new frmRoadNetSpecialInstructions();
                frm.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}
