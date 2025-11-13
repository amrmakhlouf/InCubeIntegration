using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using InCubeLibrary;

namespace InCubeIntegration_UI
{
    public partial class frmRoadNetIntegrationExecution : Form
    {
        public enum Mode
        {
            ExportOrders,
            ExportLocations,
            ImportOrders,
            ImportJourneyPlan
        }
        Mode _frmMode;
        public delegate bool PrepareTablesForOrdersExportDel(ref int locations, ref int SKUs, ref int PackageTypes, ref int Orders, ref int OrderLines);
        public PrepareTablesForOrdersExportDel PrepareTablesForOrdersExportHandler;
        public delegate bool PrepareTablesForLocationsExportDel(ref int locations);
        public PrepareTablesForLocationsExportDel PrepareTablesForLocationsExportHandler;
        public delegate bool SendLocationsDel();
        public SendLocationsDel SendLocationsHandler;
        public delegate bool SendLocationExtensionsDel();
        public SendLocationExtensionsDel SendLocationExtensionsHandler;
        public delegate bool SendSKUsDel();
        public SendSKUsDel SendSKUsHandler;
        public delegate bool SendPackageTypesDel();
        public SendPackageTypesDel SendPackageTypesHandler;
        public delegate bool SendOrdersDel(ref string PRN_Path);
        public SendOrdersDel SendOrdersHandler;
        public delegate Result RetrieveRoadNetSessionDetailsDel();
        public RetrieveRoadNetSessionDetailsDel RetrieveRoadNetSessionDetailsHandler;
        public delegate Result ProcessSessionDetailsDel();
        public ProcessSessionDetailsDel ProcessSessionDetailsHandler;
        public delegate Result FetchImportResultsDel(ref DataTable dtResults);
        public FetchImportResultsDel FetchImportResultsHandler;
        public frmRoadNetIntegrationExecution(Mode frmMode)
        {
            InitializeComponent();
            this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
            _frmMode = frmMode;
            if (_frmMode == Mode.ImportOrders || _frmMode == Mode.ImportJourneyPlan)
            {
                this.Text = "Import status";
            }
            else if (_frmMode == Mode.ExportOrders || _frmMode == Mode.ExportLocations)
            {
                this.Text = "Export status";
                dgvImportResults.Visible = false;
                txtResults.Dock = DockStyle.Fill;
            }
        }

        private void frmRoadNetIntegrationExecution_Load(object sender, EventArgs e)
        {
            
        }

        private void frmRoadNetIntegrationExecution_Shown(object sender, EventArgs e)
        {
            try
            {
                if (_frmMode == Mode.ImportOrders)
                {
                    Result res = Result.UnKnown;
                    AppendText("Retrieving RoadNet Session Details .. ", Color.Black);
                    res = RetrieveRoadNetSessionDetailsHandler();
                    if (res == Result.Success)
                    {
                        AppendText("Success\r\n\r\n", Color.DarkGreen);
                        AppendText("Processing Session Details .. ", Color.Black);
                        res = ProcessSessionDetailsHandler();
                        if (res == Result.Success)
                        {
                            AppendText("Success\r\n\r\n", Color.DarkGreen);
                            AppendText("Fetching results .. ", Color.Black);
                            DataTable dtResults = new DataTable();
                            res = FetchImportResultsHandler(ref dtResults);
                            if (res == Result.Success)
                            {
                                if (dtResults.Rows.Count == 0)
                                {
                                    AppendText("No Trips found ..", Color.Red);
                                }
                                else
                                {
                                    AppendText(dtResults.Rows.Count.ToString() + " Trips found ..", Color.DarkGreen);
                                    dgvImportResults.DataSource = dtResults;
                                }
                            }
                            else
                            {
                                AppendText("Failure !!", Color.Red);
                            }
                        }
                        else
                        {
                            AppendText("Failure!!", Color.Red);
                        }
                    }
                    else
                    {
                        AppendText("Failure!!", Color.Red);
                    }
                }
                else if (_frmMode == Mode.ExportOrders)
                {
                    AppendText("Preparing RoadNet tables ..\r\n", Color.Black);
                    int Locations = 0, SKUs = 0, PackageTypes = 0, Orders = 0, OrderLines = 0;
                    bool Result = PrepareTablesForOrdersExportHandler(ref Locations, ref SKUs, ref PackageTypes, ref Orders, ref OrderLines);
                    if (Result)
                    {
                        AppendText(string.Format("Tables preperation successful ..\r\nLocations: {0}\r\nSKUs: {1}\r\nPackage Types: {2}\r\nOrder: {3}\r\nOrder Lines: {4}\r\n", Locations, SKUs, PackageTypes, Orders, OrderLines), Color.DarkGreen);
                        AppendText("\r\nSending locations: ", Color.Black);
                        Result = SendLocationsHandler();
                        if (Result)
                        {
                            AppendText("Success ..\r\n", Color.DarkGreen);
                        }
                        else
                        {
                            AppendText("Failed !!", Color.Red);
                            return;
                        }
                        AppendText("\r\nSending PackageTypes: ", Color.Black);
                        Result = SendPackageTypesHandler();
                        if (Result)
                        {
                            AppendText("Success ..\r\n", Color.DarkGreen);
                        }
                        else
                        {
                            AppendText("Failed !!", Color.Red);
                            return;
                        }

                        AppendText("\r\nSending SKUs: ", Color.Black);
                        Result = SendSKUsHandler();
                        if (Result)
                        {
                            AppendText("Success ..\r\n", Color.DarkGreen);
                        }
                        else
                        {
                            AppendText("Failed !!", Color.Red);
                            return;
                        }

                        AppendText("\r\nSending Orders: ", Color.Black);
                        string PRN_Path = "";
                        Result = SendOrdersHandler(ref PRN_Path);
                        if (Result)
                        {
                            AppendText("Success ..\r\n", Color.DarkGreen);
                            if (PRN_Path != "")
                            {
                                AppendText("PRN exported to:", Color.Black);
                                AppendText(PRN_Path, Color.Blue);
                            }
                        }
                        else
                        {
                            AppendText("Failed !!", Color.Red);
                            return;
                        }
                    }
                    else
                    {
                        AppendText("Tables preperation failed !!\r\n", Color.Red);
                    }
                }
                else if (_frmMode == Mode.ExportLocations)
                {
                    AppendText("Preparing Location tables ..\r\n", Color.Black);
                    int Locations = 0;
                    bool Result = PrepareTablesForLocationsExportHandler(ref Locations);
                    if (Result)
                    {
                        AppendText(string.Format("Tables preperation successful ..\r\nLocations: {0}\r\n", Locations), Color.DarkGreen);
                        AppendText("\r\nSending locations: ", Color.Black);
                        Result = SendLocationsHandler();
                        if (Result)
                        {
                            AppendText("Success ..\r\n", Color.DarkGreen);
                        }
                        else
                        {
                            AppendText("Failed !!", Color.Red);
                            return;
                        }
                        AppendText("\r\nSending location extensions: ", Color.Black);
                        Result = SendLocationExtensionsHandler();
                        if (Result)
                        {
                            AppendText("Success ..\r\n", Color.DarkGreen);
                        }
                        else
                        {
                            AppendText("Failed !!", Color.Red);
                            return;
                        }
                    }
                    else
                    {
                        AppendText("Tables preperation failed !!\r\n", Color.Red);
                    }
                }
                else if (_frmMode == Mode.ImportJourneyPlan)
                {
                    Result res = Result.UnKnown;
                    AppendText("Retrieving Journey Plan Details .. ", Color.Black);
                    res = RetrieveRoadNetSessionDetailsHandler();
                    if (res == Result.Success)
                    {
                        AppendText("Success\r\n\r\n", Color.DarkGreen);
                        AppendText("Processing Journey Plan Details .. ", Color.Black);
                        res = ProcessSessionDetailsHandler();
                        if (res == Result.Success)
                        {
                            AppendText("Success\r\n\r\n", Color.DarkGreen);
                            AppendText("Fetching results .. ", Color.Black);
                            DataTable dtResults = new DataTable();
                            res = FetchImportResultsHandler(ref dtResults);
                            if (res == Result.Success)
                            {
                                if (dtResults.Rows.Count == 0)
                                {
                                    AppendText("No result found ..", Color.Red);
                                }
                                else
                                {
                                    dgvImportResults.DataSource = dtResults;
                                }
                            }
                            else
                            {
                                AppendText("Failure !!", Color.Red);
                            }
                        }
                        else
                        {
                            AppendText("Failure!!", Color.Red);
                        }
                    }
                    else
                    {
                        AppendText("Failure!!", Color.Red);
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
                txtResults.SelectionStart = txtResults.TextLength;
                txtResults.SelectionLength = 0;

                txtResults.SelectionColor = color;
                txtResults.AppendText(text);
                //txtResults.SelectionColor = txtResults.ForeColor;
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}
