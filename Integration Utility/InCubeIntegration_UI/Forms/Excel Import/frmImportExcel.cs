using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using System.Drawing;

namespace InCubeIntegration_UI
{
    public partial class frmImportExcel : Form
    {
        ExcelManager em;
        DataTable dtTypes = new DataTable();
        DataTable dtTypeSheets = new DataTable();
        DataTable dtSheetsColumns = new DataTable();
        DataTable dtProcedures = new DataTable();
        int selectedImportTypeID = 0;
        DataRow[] selectedProcedures;
        DataRow[] selectedSheets;
        BackgroundWorker bgwImport;
        Dictionary<int, DataGridView> Grids = new Dictionary<int, DataGridView>();
        int _startImportTypeID = -1;
        public Result ImportResult = Result.UnKnown;
        DataTable dtResults;
        public frmImportExcel(int ExcelImportTypeID) : this()
        {
            _startImportTypeID = ExcelImportTypeID;
        }
        public frmImportExcel()
        {
            InitializeComponent();
        }
        private void LoadImportTypes()
        {
            try
            {
                em.GetDataTypes(false, _startImportTypeID, ref dtTypes, ref dtTypeSheets, ref dtSheetsColumns, ref dtProcedures);
                dtSheetsColumns.DefaultView.Sort = "ImportTypeID,SheetNo,Sequence";
                dtProcedures.DefaultView.Sort = "ImportTypeID,Sequence";
                dtTypeSheets.Columns.Add("ExcelSheet", typeof(string));
                if (dtTypes != null && dtTypes.Rows.Count > 0)
                {
                    cmbDataType.DataSource = dtTypes;
                    cmbDataType.DisplayMember = "Name";
                    cmbDataType.ValueMember = "ImportTypeID";
                    cmbDataType.Enabled = true;
                    cmbDataType.SelectedIndex = -1;
                    cmbDataType.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void frmImportExcel_Load(object sender, EventArgs e)
        {
            this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
            em = new ExcelManager();
            LoadImportTypes();
            if (_startImportTypeID != -1)
                cmbDataType.Enabled = false;
        }
        private void btnBrowseFile_Click(object sender, EventArgs e)
        {
            try
            {
                OpenExcelConnection();
                customProgressBar1.Value = 0;
                customProgressBar1.Maximum = 0;
                txtInserted.Clear();
                txtSkipped.Clear();
                txtUpdated.Clear();
                txtTotalRows.Clear();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void cmbDataType_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cmbDataType.SelectedIndex >= 0 && cmbDataType.ValueMember != "")
                {
                    selectedImportTypeID = Convert.ToInt16(cmbDataType.SelectedValue);
                    btnBrowseFile.Enabled = true;
                    txtDescription.Clear();
                    tabControl1.TabPages.Clear();
                    Grids = new Dictionary<int, DataGridView>();
                    AppendText(dtTypes.Select("ImportTypeID = " + selectedImportTypeID)[0]["Description"].ToString(), Color.Black);
                    selectedProcedures = dtProcedures.Select("ImportTypeID = " + selectedImportTypeID);
                    selectedSheets = dtTypeSheets.Select("ImportTypeID = " + selectedImportTypeID);
                    AppendText("\r\n\r\nRequired Columns:", Color.Navy);

                    foreach (DataRow dr in selectedSheets)
                    {
                        int SheetNo = int.Parse(dr["SheetNo"].ToString());
                        string SheetDescription = dr["SheetDescription"].ToString();
                        AppendText("\r\n[" + SheetDescription + "]:\r\n", Color.DarkGreen);
                        DataRow[] selectedColumns = dtSheetsColumns.Select(string.Format("ImportTypeID = {0} AND SheetNo = {1}", selectedImportTypeID, SheetNo));
                        foreach (DataRow col in selectedColumns)
                        {
                            AppendText(col["FieldName"].ToString() + "\r\n", Color.DarkRed);
                        }
                        tabControl1.TabPages.Add(SheetDescription);

                        DataGridView grdPreview = new DataGridView();
                        grdPreview.BackgroundColor = System.Drawing.Color.White;
                        grdPreview.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
                        grdPreview.Dock = DockStyle.Fill;
                        tabControl1.TabPages[SheetNo - 1].Controls.Add(grdPreview);
                        Grids.Add(SheetNo, grdPreview);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void AppendText(string text, Color color)
        {
            try
            {
                txtDescription.SelectionStart = txtDescription.TextLength;
                txtDescription.SelectionLength = 0;
                txtDescription.SelectionColor = color;
                txtDescription.AppendText(text);
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void OpenExcelConnection()
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Select excel file";
                ofd.FileName = "";
                ofd.Filter = "Excel files (*.xls,*.xlsx,*.csv)|*.xls;*.xlsx;*.csv";
                if (ofd.ShowDialog() == DialogResult.OK && !ofd.FileName.Equals(string.Empty))
                {
                    txtExcelPath.Text = ofd.FileName;
                    em.ExcelPath = ofd.FileName;
                    Result res = em.TestConnection();
                    if (res == Result.Failure)
                    {
                        MessageBox.Show("Failed in opening connection to excel!!");
                        return;
                    }
                    else if (res == Result.Invalid)
                    {
                        MessageBox.Show("Unsupported file type!!");
                        return;
                    }
                }
                else
                    return;

                DataTable dtSheets = new DataTable();
                if (em.FileExt != ExcelManager.FileExtension.csv)
                    em.GetSheets(ref dtSheets);
                int SheetsCount = em.FileExt == ExcelManager.FileExtension.csv ? 1 : dtSheets.Rows.Count;
                string sheet1 = "";
                if (SheetsCount == 0)
                {
                    MessageBox.Show("File has no sheets!!");
                    return;
                }
                else if (SheetsCount < selectedSheets.Length)
                {
                    MessageBox.Show("Excel sheets are less than required for this type [" + selectedSheets.Length + " Sheets]");
                    return;
                }
                else if (em.FileExt != ExcelManager.FileExtension.csv && SheetsCount == 1 && selectedSheets.Length == 1)
                {
                    sheet1 = dtSheets.Rows[0]["TABLE_NAME"].ToString();
                    selectedSheets[0]["ExcelSheet"] = sheet1;
                }
                else if (em.FileExt != ExcelManager.FileExtension.csv)
                {
                    for (int i = 0; i < selectedSheets.Length; i++)
                    {
                        frmSelectSheet frm = new frmSelectSheet(dtSheets);
                        frm.Text = "Select Sheet for (" + selectedSheets[i]["SheetDescription"].ToString() + ")";
                        frm.ShowDialog();
                        sheet1 = frm.SelectedSheet;
                        selectedSheets[i]["ExcelSheet"] = sheet1;
                        dtSheets.DefaultView.RowFilter = "TABLE_NAME <> '" + sheet1 + "'";
                        dtSheets = dtSheets.DefaultView.ToTable();
                    }
                }
                
                foreach (DataRow dr in selectedSheets)
                {
                    DataTable dtTop100Rows = new DataTable();
                    string Exception = string.Empty;
                    int SheetNo = int.Parse(dr["SheetNo"].ToString());
                    DataRow[] selectedColumns = dtSheetsColumns.Select(string.Format("ImportTypeID = {0} AND SheetNo = {1}", selectedImportTypeID, SheetNo));
                    string ExcelSheet = dr["ExcelSheet"].ToString();
                    if (em.GetExcelTopNRows(ExcelSheet, selectedColumns, 1000, ref dtTop100Rows, ref Exception) == Result.Success)
                    {
                        Grids[SheetNo].DataSource = dtTop100Rows;
                        lbl.Text = "Top 1000 rows in the file";
                        btnImport.Enabled = true;
                        btnExportResults.Enabled = false;
                    }
                    else
                    {
                        MessageBox.Show(Exception);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void ReportUpdateProgress(int TotalRows ,int Inserted, int Updated, int Skipped, string labelText)
        {
            try
            {
                txtTotalRows.Text = TotalRows.ToString();
                txtInserted.Text = Inserted.ToString();
                txtUpdated.Text = Updated.ToString();
                txtSkipped.Text = Skipped.ToString();
                customProgressBar1.Maximum = TotalRows;
                if (Inserted + Updated + Skipped <= TotalRows)
                    customProgressBar1.Value = Inserted + Updated + Skipped;
                customProgressBar1.CustomText = labelText + " " + customProgressBar1.Value + "/" + customProgressBar1.Maximum;
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void bgwImport_DoWork(object o, DoWorkEventArgs e)
        {
            try
            {
                em.ReportProgressHandler += new ExcelManager.ReportProgressDel(ReportUpdateProgress);
                Result res = em.ExecuteProcedures(selectedProcedures);
                if (res == Result.Success)
                {
                    MessageBox.Show("Data processed successfully ..", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ImportResult = Result.Success;
                    btnExportResults.Enabled = true;
                }
                else
                    MessageBox.Show("Data processing failed!!", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lbl.Text = "Results: ";
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void bgwImport_RunWorkerCompleted(object o, RunWorkerCompletedEventArgs e)
        {
            try
            {
                foreach (int SheetNo in Grids.Keys)
                {
                    dtResults = new DataTable();
                    if (em.GetResultsTable(SheetNo, ref dtResults) == Result.Success)
                    {
                        Grids[SheetNo].DataSource = dtResults;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void btnImport_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> Tables = new List<string>();
                foreach (DataRow dr in selectedSheets)
                {
                    int SheetNo = int.Parse(dr["SheetNo"].ToString());
                    string ExcelSheet = dr["ExcelSheet"].ToString();
                    string StagingTable = dr["StagingTable"].ToString();
                    Tables.Add(StagingTable);
                    DataRow[] selectedColumns = dtSheetsColumns.Select(string.Format("ImportTypeID = {0} AND SheetNo = {1}", selectedImportTypeID, SheetNo));
                    em.DumpDataToStagingTable(SheetNo, ExcelSheet, StagingTable, selectedColumns);
                }
                em.ReportingTables = Tables;
                btnImport.Enabled = false;
                bgwImport = new BackgroundWorker();
                bgwImport.DoWork += new DoWorkEventHandler(bgwImport_DoWork);
                bgwImport.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgwImport_RunWorkerCompleted);
                bgwImport.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnExportResults_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Excel Files (*.xlsx)|*.xlsx";
                sfd.DefaultExt = "xlsx";
                sfd.AddExtension = true;
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    DataSet ds = new DataSet("ds");
                    ds.Tables.Add(dtResults);
                    ExcelManager.CreateExcelDocument(ds, sfd.FileName);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}
