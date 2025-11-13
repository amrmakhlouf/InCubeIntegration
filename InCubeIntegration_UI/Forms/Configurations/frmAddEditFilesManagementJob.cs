using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using InCubeIntegration_BL;
using InCubeLibrary;

namespace InCubeIntegration_UI
{
    public partial class frmAddEditFilesManagementJob : Form
    {
        int _id = -1;
        FilesManager filesManager;
        string lastFilledDestination = "";
        public frmAddEditFilesManagementJob(int ID, FilesManager _filesManager)
        {
            InitializeComponent();
            _id = ID;
            filesManager = _filesManager;
        }

        private void frmAddEditFilesManagementJob_Load(object sender, EventArgs e)
        {
            try
            {
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;

                ContextMenu cm = new ContextMenu();
                cm.MenuItems.Add("Integration Directory", AddIntegrationDirectory);
                txtSourceFolder.ContextMenu = cm;

                Dictionary<int, string> JobTypes = new Dictionary<int, string>();
                JobTypes.Add(FileJobType.Delete.GetHashCode(), "Delete");
                JobTypes.Add(FileJobType.Move.GetHashCode(), "Move");
                JobTypes.Add(FileJobType.Copy.GetHashCode(), "Copy");
                cmbJobType.DataSource = new BindingSource(JobTypes, null);
                cmbJobType.ValueMember = "Key";
                cmbJobType.DisplayMember = "Value";

                Dictionary<int, string> AgeUnits = new Dictionary<int, string>();
                AgeUnits.Add(AgeTimeUnit.Second.GetHashCode(), "Second");
                AgeUnits.Add(AgeTimeUnit.Minute.GetHashCode(), "Minute");
                AgeUnits.Add(AgeTimeUnit.Hour.GetHashCode(), "Hour");
                AgeUnits.Add(AgeTimeUnit.Day.GetHashCode(), "Day");
                AgeUnits.Add(AgeTimeUnit.Month.GetHashCode(), "Month");
                AgeUnits.Add(AgeTimeUnit.Year.GetHashCode(), "Year");
                cmbAgeUnit.DataSource = new BindingSource(AgeUnits, null);
                cmbAgeUnit.ValueMember = "Key";
                cmbAgeUnit.DisplayMember = "Value";

                if (_id == -1)
                {
                    _id = filesManager.GetMaxFilesJobID();
                    txtID.Text = _id.ToString();
                }
                else
                {
                    string Name = "", SourceFolder = "", FileExtension = "", DestFolder = "";
                    int JobTypeID = 0, AgeUnitID = 0;
                    long Age = 0;
                    bool KeepDirectoryStructure = false;
                    ComparisonOperator compare = ComparisonOperator.GreaterThan;
                    Result res = filesManager.GetFilesJobDetails(_id, ref Name, ref SourceFolder, ref FileExtension, ref DestFolder, ref JobTypeID, ref Age, ref AgeUnitID, ref KeepDirectoryStructure, ref compare);
                    if (res == Result.Success)
                    {
                        txtID.Text = _id.ToString();
                        txtName.Text = Name;
                        cmbJobType.SelectedValue = JobTypeID;
                        txtSourceFolder.Text = SourceFolder;
                        txtFileExtension.Text = FileExtension;
                        numAge.Value = Age;
                        cmbAgeUnit.SelectedValue = AgeUnitID;
                        txtDestFolder.Text = DestFolder;
                        cbKeepDirectoryStructure.Checked = KeepDirectoryStructure;
                        if (compare == ComparisonOperator.GreaterThan)
                            btnComparisonOperator.Text = ">";
                        else
                            btnComparisonOperator.Text = "<";
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
                string Name = "", SourceFolder = "", FileExtension = "", DestFolder = "";
                int JobTypeID = 0, AgeUnitID = 0;
                long Age = 0;
                bool KeepDirectoryStructure = false;
                ComparisonOperator compare;

                if (txtName.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Fill name!!");
                    txtName.Focus();
                    return;
                }
                Name = txtName.Text.Trim();

                if (cmbJobType.SelectedValue == null)
                {
                    MessageBox.Show("Select job type!!");
                    cmbJobType.Focus();
                    return;
                }
                if (!int.TryParse(cmbJobType.SelectedValue.ToString(), out JobTypeID))
                {
                    MessageBox.Show("Invalid job type!!");
                    cmbJobType.Focus();
                    return;
                }

                if (txtSourceFolder.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Fill source folder!!");
                    txtSourceFolder.Focus();
                    return;
                }
                SourceFolder = txtSourceFolder.Text.Trim();
                switch (filesManager.ValidateDirectory(SourceFolder))
                {
                    case Result.NoRowsFound:
                        MessageBox.Show("Source folder doesn't exist!!");
                        return;
                    case Result.Failure:
                        MessageBox.Show("Invalid Source folder!!");
                        return;
                }

                if (txtFileExtension.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Fill file extension!!");
                    txtFileExtension.Focus();
                    return;
                }
                FileExtension = txtFileExtension.Text.Trim();

                if (numAge.Value < 1)
                {
                    MessageBox.Show("Select file age!!");
                    numAge.Focus();
                    return;
                }
                Age = (long)numAge.Value;

                if (cmbAgeUnit.SelectedValue == null)
                {
                    MessageBox.Show("Select file age unit!!");
                    cmbAgeUnit.Focus();
                    return;
                }
                if (!int.TryParse(cmbAgeUnit.SelectedValue.ToString(), out AgeUnitID))
                {
                    MessageBox.Show("Invalid age unit!!");
                    cmbAgeUnit.Focus();
                    return;
                }
                KeepDirectoryStructure = cbKeepDirectoryStructure.Checked;

                if (btnComparisonOperator.Text == ">")
                    compare = ComparisonOperator.GreaterThan;
                else
                    compare = ComparisonOperator.LessThan;

                if ((FileJobType)JobTypeID != FileJobType.Delete)
                {
                    if (txtDestFolder.Text.Trim() == string.Empty)
                    {
                        MessageBox.Show("Fill destination folder!!");
                        txtDestFolder.Focus();
                        return;
                    }
                    DestFolder = txtDestFolder.Text.Trim();
                    switch (filesManager.ValidateDirectory(DestFolder))
                    {
                        case Result.NoRowsFound:
                            MessageBox.Show("Destination folder doesn't exist!!");
                            return;
                        case Result.Failure:
                            MessageBox.Show("Invalid Destination folder!!");
                            return;
                    }
                }

                Result res = filesManager.SaveFilesJob(_id, Name, SourceFolder, FileExtension, DestFolder, JobTypeID, AgeUnitID, Age, KeepDirectoryStructure, compare);
                if (res == Result.Success)
                {
                    MessageBox.Show("Files management job saved successfully ..");
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Failed in saving files management job!!");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void cmbJobType_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cmbJobType.ValueMember != "")
                {
                    if ((FileJobType)cmbJobType.SelectedValue == FileJobType.Delete)
                    {
                        if (txtDestFolder.Text.Trim() != string.Empty)
                            lastFilledDestination = txtDestFolder.Text;
                        txtDestFolder.Clear();
                        txtDestFolder.Enabled = false;
                        cbKeepDirectoryStructure.Enabled = false;
                        cbKeepDirectoryStructure.Checked = false;
                    }
                    else
                    {
                        txtDestFolder.Enabled = true;
                        txtDestFolder.Text = lastFilledDestination;
                        cbKeepDirectoryStructure.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void AddIntegrationDirectory(object sender, EventArgs e)
        {
            txtSourceFolder.Text = "[Integration Directory]\\";
            txtSourceFolder.Select(txtSourceFolder.Text.Length, 0);
        }

        private void btnCompareFactor_Click(object sender, EventArgs e)
        {
            if (btnComparisonOperator.Text == ">")
                btnComparisonOperator.Text = "<";
            else
                btnComparisonOperator.Text = ">";
        }
    }
}
