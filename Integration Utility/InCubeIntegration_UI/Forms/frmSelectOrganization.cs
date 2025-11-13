using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.Data;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmSelectOrganization : Form
    {
        public int OrganizationID;
        public string OrganizationCode;
        DataTable dtOrg;

        public frmSelectOrganization()
        {
            InitializeComponent();
        }

        private void frmSelectOrganization_Load(object sender, EventArgs e)
        {
            try
            {
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;

                using (OrganizationManager orgManager = new OrganizationManager())
                {
                    dtOrg = orgManager.GetOrganizations();
                    cmbOrganization.DataSource = dtOrg;
                    cmbOrganization.DisplayMember = "Description";
                    cmbOrganization.ValueMember = "OrganizationID";
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            try
            {
                OrganizationID = Convert.ToInt16(cmbOrganization.SelectedValue);
                OrganizationCode = dtOrg.Select("OrganizationID = " + OrganizationID)[0]["OrganizationCode"].ToString();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
