using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Xml;

namespace InCubeIntegration_UI
{
    public partial class frmImportTargets : Form
    {
        int page = 1;
        int maxPage = 1;
        int counter = 1;
        string month;
        string year;

        DataTable dtAllResults = new DataTable();
        DataTable dtErrors = new DataTable();

        enum requiredColumns
        {
            ID = 0,
            CustomerCode = 1,
            ItemCode = 2,
            UOM = 3,
            Quantity = 4,
            Value = 5
        }

        SqlConnection sqlConn = null;
        SqlCommand sqlCmd = null;
        SqlDataAdapter sqlAdp = null;
        OleDbConnection excelConn = null;
        OleDbCommand excelCmd = null;
        OleDbDataAdapter excelAdp = null;
        DataTable dtData = null;
        object field = null;
        string queryString = null;
        string warehouseID = "-1";
        string excelPath = "";
        string sheet1;
        DataRow[] dr;
        private string _encryptionKey = "!@#$%^&**&^%$#@!";
        BackgroundWorker bgw = new BackgroundWorker();

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            try
            {
                bgw.DoWork += bgw_DoWork;
                bgw.ProgressChanged += bgw_ProgressChanged;
                bgw.RunWorkerCompleted += bgw_RunWorkerCompleted;


                int currentmonth = DateTime.Now.Month;
                int currentyear = DateTime.Now.Year;
                int nextmonth = (currentmonth % 12) + 1;
                int nextyear = currentyear;
                if (nextmonth == 1)
                    nextyear++;

                cmbMonth.Items.Add(currentmonth);
                cmbMonth.Items.Add(nextmonth);
                cmbYear.Items.Add(currentyear);
                if (nextyear != currentyear)
                    cmbYear.Items.Add(nextyear);

                cmbMonth.SelectedIndex = 0;
                cmbYear.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private bool ImportToSQL()
        {
            try
            {
                bgw.WorkerReportsProgress = true;
                bgw.ReportProgress(2);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(Application.StartupPath + "\\Datasources.xml");
                XmlNode node = xmlDoc.SelectSingleNode("Connections/Connection[Name='InCube']/Data");

                excelCmd = new OleDbCommand(queryString, excelConn);
                excelCmd.CommandTimeout = 3600000;
                OleDbDataReader dr = excelCmd.ExecuteReader();

                sqlConn = new SqlConnection(node.InnerText);
                sqlConn.Open();
                sqlCmd = new SqlCommand("DELETE FROM VolumeTargets", sqlConn);
                sqlCmd.CommandTimeout = 3600000;
                sqlCmd.ExecuteNonQuery();

                SqlBulkCopy bulk = new SqlBulkCopy(node.InnerText);
                bulk.DestinationTableName = "VolumeTargets";
                bulk.BulkCopyTimeout = 3600000;
                bulk.WriteToServer(dr);

                bgw.WorkerReportsProgress = true;
                bgw.ReportProgress(3);
                sqlCmd = new SqlCommand("sp_PrepareTargetsData", sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.CommandTimeout = 3600000;
                sqlCmd.ExecuteNonQuery();

                bgw.WorkerReportsProgress = true;
                bgw.ReportProgress(4);
                sqlCmd = new SqlCommand("sp_DeleteVolumeTargets", sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.Parameters.Add(new SqlParameter("@Year", year));
                sqlCmd.Parameters.Add(new SqlParameter("@Month", month));
                sqlCmd.CommandTimeout = 3600000;
                sqlCmd.ExecuteNonQuery();

                bgw.WorkerReportsProgress = true;
                bgw.ReportProgress(5);
                sqlCmd = new SqlCommand("sp_InsertVolumeTargets", sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.Parameters.Add(new SqlParameter("@Year", year));
                sqlCmd.Parameters.Add(new SqlParameter("@Month", month));
                sqlCmd.CommandTimeout = 3600000;
                sqlCmd.ExecuteNonQuery();

                bgw.WorkerReportsProgress = true;
                bgw.ReportProgress(6);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                MessageBox.Show("Error: " + ex.Message);
                return false;
            }
            return true;
        }

        [STAThread]
        void bgw_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                bgw.WorkerReportsProgress = true;
                bgw.ReportProgress(1);
                if (LoadExcelFile() == 1)
                {
                    if (ImportToSQL())
                    {
                        sqlAdp = new SqlDataAdapter(@"SELECT DISTINCT TBL.ID XlsRowID,TBL.CustomerCode,TBL.ItemCode,TBL.GivenUOM,TBL.GivenQty,TBL.GivenValue
,TBL.TargetUOM, CASE WHEN TBL.TargetUOM IS NULL THEN NULL ELSE TBL.EquivalencyFactor END EquivalencyFactor,
CASE WHEN TBL.TargetUOM IS NULL THEN NULL ELSE TBL.GivenQty/TBL.EquivalencyFactor END TargetQty,
CASE WHEN TBL.TargetUOM IS NULL THEN NULL ELSE TBL.GivenValue/TBL.EquivalencyFactor END TargetValue,TBL.Result FROM (
SELECT VT.ID,VT.CustomerCode,VT.ItemCode,VT.UOM GivenUOM,VT.Quantity GivenQty,VT.Value GivenValue,PTL.Description TargetUOM,
CASE PTL.Description WHEN 'CX' THEN (CASE VT.UOM WHEN 'Kg' THEN (CASE P.Weight WHEN 0 THEN 1 ELSE P.Weight END) ELSE 1 END) ELSE 1 END EquivalencyFactor,
(CASE WHEN I.ItemID IS NULL THEN 'Item code not defined' ELSE
(CASE WHEN CO.CustomerID IS NULL THEN 'Customer code not defined' ELSE
(CASE WHEN P.PackID IS NULL THEN 'Item has no pack' ELSE 'Success' END) END) END) Result
FROM VolumeTargets VT
LEFT JOIN CustomerOutlet CO ON CO.CustomerCode = VT.CustomerCode
LEFT JOIN Item I ON I.ItemCode = VT.ItemCode
LEFT JOIN Pack P ON P.ItemID = I.ItemID
LEFT JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1) TBL", sqlConn);

                        dtAllResults = new DataTable();
                        sqlAdp.Fill(dtAllResults);

                        dtAllResults.DefaultView.RowFilter = "Result <> 'Success'";
                        dtErrors = dtAllResults.DefaultView.ToTable();
                        dtAllResults.DefaultView.RowFilter = "";

                        DataColumn colID = new DataColumn("ID", typeof(int));
                        dtAllResults.Columns.Add(colID);
                        for (int i = 0; i < dtAllResults.Rows.Count; i++)
                        {
                            dtAllResults.Rows[i]["ID"] = i + 1;
                        }
                        colID.SetOrdinal(0);

                        DataColumn colID2 = new DataColumn("ID", typeof(int));
                        dtErrors.Columns.Add(colID2);
                        for (int i = 0; i < dtErrors.Rows.Count; i++)
                        {
                            dtErrors.Rows[i]["ID"] = i + 1;
                        }
                        colID2.SetOrdinal(0);

                        dtData = dtAllResults.Copy();
                        dtData.DefaultView.RowFilter = "ID >= 1 AND ID <= 1000";
                        dtData.DefaultView.Sort = "ID";
                        page = 1;
                        maxPage = (int)Math.Ceiling((decimal)dtData.Rows.Count / 1000);
                        bgw.WorkerReportsProgress = true;
                        bgw.ReportProgress(7);
                    }
                    else
                    {
                        bgw.ReportProgress(8);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        void bgw_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            try
            {
                switch (e.ProgressPercentage)
                {
                    case 1:
                        lblStatus.Text = "Opening excel file ...";
                        break;
                    case 2:
                        lblStatus.Text = "Reading targets ...";
                        break;
                    case 3:
                        lblStatus.Text = "Processing targets ...";
                        break;
                    case 4:
                        lblStatus.Text = "Deleting old defined targets ...";
                        break;
                    case 5:
                        lblStatus.Text = "Defining new targets ...";
                        break;
                    case 6:
                        lblStatus.Text = "Retrieving Results ...";
                        break;
                    case 7:
                        lblStatus.Text = "Completed ...";
                        dgvResults.DataSource = dtData.DefaultView;
                        textBox1.Text = "1-1000 / " + dtData.Rows.Count;
                        textBox1.Enabled = true;
                        button1.Enabled = true;
                        button2.Enabled = true;
                        checkBox1.Enabled = true;
                        cmbYear.Enabled = true;
                        cmbMonth.Enabled = true;
                        break;
                    case 8:
                        lblStatus.Text = "Importing failed ...";
                        break;
                }
                Application.DoEvents();
                bgw.WorkerReportsProgress = false;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        void bgw_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            //lblStatus.Text = "Completed ...";
            btnLoadExcel.Enabled = true;
            Application.DoEvents();
        }


        private int LoadExcelFile()
        {
            try
            {
                //OpenFileDialog ofd = new OpenFileDialog();
                //ofd.Title = "Select targets excel sheet";
                //ofd.FileName = "";
                //ofd.Filter = "Excel files (*.xls,*.xlsx)|*.xls;*.xlsx";
                //if (ofd.ShowDialog() == DialogResult.OK && !ofd.FileName.Equals(string.Empty))
                //{
                //    excelPath = ofd.FileName;
                //    txtExcelPath.Text = excelPath;
                //}
                //else
                //    return 0;

                excelConn = new OleDbConnection();
                try
                {
                    excelConn.ConnectionString = @"Provider=Microsoft.JET.OLEDB.4.0;Data Source=" + excelPath + ";Extended Properties='Excel 8.0; HDR=Yes;IMEX=1;'";
                    excelConn.Open();
                }
                catch (Exception ex)
                {
                    //Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                    try
                    {
                        excelConn.ConnectionString = string.Format(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=""Excel 12.0 Xml;HDR=YES,IMEX = 1"";", excelPath);
                        excelConn.Open();
                    }
                    catch (Exception ex2)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex2.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        throw new Exception("Failed in openning excel file");
                    }
                }

                sheet1 = excelConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null).Rows[0]["TABLE_NAME"].ToString();
                dtData = excelConn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, null);
                sheet1 = "Sheet1$";
                string where = " WHERE ";
                queryString = "SELECT ";
                foreach (string colName in Enum.GetNames(typeof(requiredColumns)))
                {
                    queryString += "[" + colName + "],";
                    where += "[" + colName + "] IS NOT NULL AND ";
                    dr = dtData.Select("TABLE_NAME = '" + sheet1 + "' AND COLUMN_NAME = '" + colName + "'");
                    if (dr.Length == 0)
                    {
                        throw new Exception("Column [" + colName + "] doesn't exist in sheet " + sheet1);
                    }
                }

                queryString = queryString.Substring(0, queryString.Length - 1) + " FROM [" + sheet1 + "] " + where.Substring(0, where.Length - 5);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                MessageBox.Show("Error: " + ex.Message);
                return 0;
            }
            return 1;
        }

        private void btnLoadExcel_Click(object sender, EventArgs e)
        {
            try
            {
                year = cmbYear.Text;
                month = cmbMonth.Text;

                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Select targets excel sheet";
                ofd.FileName = "";
                ofd.Filter = "Excel files (*.xls,*.xlsx)|*.xls;*.xlsx";
                if (ofd.ShowDialog() == DialogResult.OK && !ofd.FileName.Equals(string.Empty))
                {
                    excelPath = ofd.FileName;
                    txtExcelPath.Text = excelPath;
                    btnLoadExcel.Enabled = false;
                    textBox1.Clear();
                    dgvResults.DataSource = null;
                    textBox1.Enabled = false;
                    button1.Enabled = false;
                    button2.Enabled = false;
                    checkBox1.Enabled = false;
                    cmbYear.Enabled = false;
                    cmbMonth.Enabled = false;

                    excelConn = new OleDbConnection();
                    try
                    {
                        excelConn.ConnectionString = @"Provider=Microsoft.JET.OLEDB.4.0;Data Source=" + excelPath + ";Extended Properties='Excel 8.0; HDR=Yes;IMEX=1;'";
                        excelConn.Open();
                    }
                    catch (Exception ex)
                    {
                        //Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        try
                        {
                            excelConn.ConnectionString = string.Format(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=""Excel 12.0 Xml;HDR=YES,IMEX = 1"";", excelPath);
                            excelConn.Open();
                        }
                        catch (Exception ex2)
                        {
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex2.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                            throw new Exception("Failed in openning excel file");
                        }
                    }

                    string queryString = "SELECT * FROM [SHEET1$]";
                    excelCmd = new OleDbCommand(queryString, excelConn);
                    excelCmd.CommandTimeout = 3600000;
                    OleDbDataReader dr = excelCmd.ExecuteReader();

                    SqlBulkCopy bulk = new SqlBulkCopy(@"Password=abc@123;Persist Security Info=True;User ID=sa;Initial Catalog=EmptyData;Data Source=AHMADQADOMI-PC\AQADOMI");
                    bulk.DestinationTableName = textBox2.Text;
                    bulk.BulkCopyTimeout = 3600000;
                    bulk.WriteToServer(dr);

                    //bgw.RunWorkerAsync();
                }
                

                //LoadExcelFile();       


                //btnImport.Enabled = false;
                //lblStatus.Visible = true;
                //timer1.Start();







            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                btnLoadExcel.Enabled = true;
                if (excelConn != null && excelConn.State == ConnectionState.Open)
                {
                    excelConn.Close();
                }
                if (sqlConn != null && sqlConn.State == ConnectionState.Open)
                {
                    sqlConn.Close();
                }
                //timer1.Stop();
                //lblStatus.Visible = false;
                //MessageBox.Show("Targets Imported ..");

            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                lblStatus.Text = "Importing ";
                for (int i = 1; i <= counter; i++)
                {
                    lblStatus.Text += ". ";
                }
                counter = ((counter + 1) % 5) + 1;
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (page < maxPage)
                {
                    page++;
                    dtData.DefaultView.RowFilter = string.Format("ID >= {0} AND ID <= {1}", (page - 1) * 1000 + 1, Math.Min(page * 1000, dtData.Rows.Count));
                    textBox1.Text = string.Format("{0}-{1} / {2}", (page - 1) * 1000 + 1, Math.Min(page * 1000, dtData.Rows.Count), dtData.Rows.Count);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (checkBox1.Checked)
                {
                    dtData = dtErrors.Copy();
                }
                else
                {
                    dtData = dtAllResults.Copy();
                }

                dtData.DefaultView.RowFilter = "ID >= 1 AND ID <= 1000";
                dtData.DefaultView.Sort = "ID";
                page = 1;
                maxPage = (int)Math.Ceiling((decimal)dtData.Rows.Count / 1000);
                dgvResults.DataSource = dtData.DefaultView;
                textBox1.Text = "1-1000 / " + dtData.Rows.Count;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (page > 1)
                {
                    page--;
                    dtData.DefaultView.RowFilter = string.Format("ID >= {0} AND ID <= {1}", (page - 1) * 1000 + 1, Math.Min(page * 1000, dtData.Rows.Count));
                    textBox1.Text = string.Format("{0}-{1} / {2}", (page - 1) * 1000 + 1, Math.Min(page * 1000, dtData.Rows.Count), dtData.Rows.Count);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
    }
}
