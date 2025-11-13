using InCubeIntegration_BL;
using InCubeLibrary;
using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using System.Collections.Generic;
namespace InCubeIntegration_UI
{
    public partial class frmRoadNetExport : Form
    {
        DataTable dtLocations = new DataTable();
        DataTable dtPackageTypes = new DataTable();
        DataTable dtItems = new DataTable();
        DataTable dtOrders = new DataTable();
        private enum GridColumns
        {
            Check = 0,
            OrderID = 1,
            OrderType = 2,
            CustomerCode = 3,
            CustomerName = 4,
            OrderDate = 5,
            DeliveryDate = 6,
            Route = 7,
            Region = 8,
            OrderTypeID = 9,
            StandardInstructions = 10,
            OrderNotes = 11
        }
        ExecutionManager execManager;
        IntegrationBase integrationObj;
        RoadNetManager RNManager;
        bool _isFillig = false;
        BackgroundWorker bgwApply = new BackgroundWorker();
        DataTable dtOrderHeaders;
        DataTable dtOrderDetails;
        int _sonicSessionID = 0;
        public bool RN_Conn_Opened = false;
        public frmRoadNetExport(int SonicSessionID)
        {
            _isFillig = true;
            InitializeComponent();
            this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
            execManager = new ExecutionManager();
            integrationObj = new IntegrationBase(execManager);
            RNManager = new RoadNetManager(true, integrationObj);
            RN_Conn_Opened = RNManager.RN_Conn_Opened;
            _sonicSessionID = SonicSessionID;
            _isFillig = false;
        }
        private void dgvOrders_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentCell.ColumnIndex == GridColumns.Check.GetHashCode() || dgvOrders.CurrentCell.ColumnIndex == GridColumns.Region.GetHashCode() || dgvOrders.CurrentCell.ColumnIndex == GridColumns.StandardInstructions.GetHashCode() || dgvOrders.CurrentCell.ColumnIndex == GridColumns.OrderNotes.GetHashCode())
            {
                dgvOrders.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }
        private void dgvOrders_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isFillig)
                return;
            try
            {
                if (e.ColumnIndex == GridColumns.Check.GetHashCode())
                {
                    _isFillig = true;
                    bool ischecked = true;
                    for (int i = 0; i < dgvOrders.Rows.Count; i++)
                    {
                        if (!Convert.ToBoolean(dgvOrders.Rows[i].Cells[GridColumns.Check.GetHashCode()].Value))
                        {
                            ischecked = false;
                            break;
                        }
                    }
                    cbAll.Checked = ischecked;
                    _isFillig = false;
                }
                FillSummaries();
            }
            catch (Exception ex)
            {
                _isFillig = false;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void cbAll_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_isFillig)
                {
                    _isFillig = true;
                    for (int i = 0; i < dgvOrders.Rows.Count; i++)
                    {
                        dgvOrders.Rows[i].Cells[GridColumns.Check.GetHashCode()].Value = cbAll.Checked;
                    }
                    _isFillig = false;
                }
                FillSummaries();
            }
            catch (Exception ex)
            {
                _isFillig = false;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        
        private bool PrepareTables(ref int locations, ref int SKUs, ref int PackageTypes, ref int Orders, ref int OrderLines)
        {
            try
            {
                dtLocations = new DataTable();
                dtLocations.Columns.Add("CustomerCode");
                dtLocations.Columns.Add("CustomerName");
                dtLocations.Columns.Add("Latitude");
                dtLocations.Columns.Add("Longitude");
                dtLocations.Columns.Add("Region");
                dtLocations.Columns.Add("TW1Start");
                dtLocations.Columns.Add("TW1Stop");
                dtLocations.Columns.Add("TW2Start");
                dtLocations.Columns.Add("TW2Stop");
                dtLocations.Columns.Add("OpenTime");
                dtLocations.Columns.Add("CloseTime");
                dtLocations.Columns.Add("FixedServiceTime");
                dtLocations.Columns.Add("StandardInstructions");
                dtLocations.Columns.Add("Address");
                dtLocations.Columns.Add("Landmark");

                dtPackageTypes = new DataTable();
                dtPackageTypes.Columns.Add("PackageTypeID");
                dtPackageTypes.Columns.Add("PackageTypeName");
                dtPackageTypes.Columns.Add("EqivalencyFactor");
                dtPackageTypes.Columns.Add("Region");
                dtPackageTypes.Columns.Add("Weight");

                dtItems = new DataTable();
                dtItems.Columns.Add("ItemCode");
                dtItems.Columns.Add("ItemName");
                dtItems.Columns.Add("PackageTypeID");
                dtItems.Columns.Add("BrandID");
                dtItems.Columns.Add("Region");

                dtOrders = new DataTable();
                dtOrders.Columns.Add("OrderID");
                dtOrders.Columns.Add("CustomerCode");
                dtOrders.Columns.Add("ItemCode");
                dtOrders.Columns.Add("Size1");
                dtOrders.Columns.Add("Size2");
                dtOrders.Columns.Add("Size3");
                dtOrders.Columns.Add("Region");
                dtOrders.Columns.Add("Notes");

                Orders = 0;
                for (int i = 0; i < dgvOrders.Rows.Count; i++)
                {
                    int isChecked = Convert.ToInt16(dgvOrders.Rows[i].Cells[GridColumns.Check.GetHashCode()].Value);
                    if (isChecked == 1)
                    {
                        Orders++;
                        string CustomerCode = dgvOrders.Rows[i].Cells[GridColumns.CustomerCode.GetHashCode()].Value.ToString();
                        string OrderID = dgvOrders.Rows[i].Cells[GridColumns.OrderID.GetHashCode()].Value.ToString();
                        string Region = dgvOrders.Rows[i].Cells[GridColumns.Region.GetHashCode()].Value.ToString();
                        if (dtLocations.Select(string.Format(@"CustomerCode = '{0}'", CustomerCode)).Length == 0)
                        {
                            DataRow dr = dtLocations.NewRow();
                            DataRow drLocationDetails = dtOrderDetails.Select(string.Format(@"CustomerCode = '{0}'", CustomerCode))[0];
                            dr["CustomerCode"] = GetTruncatedString(CustomerCode, 15, true);
                            dr["CustomerName"] = GetTruncatedString(drLocationDetails["CustomerName"].ToString(), 60, false);
                            dr["Latitude"] = drLocationDetails["Latitude"];
                            dr["Longitude"] = drLocationDetails["Longitude"];
                            dr["Region"] = Region;
                            dr["TW1Start"] = drLocationDetails["TW1Start"];
                            dr["TW1Stop"] = drLocationDetails["TW1Stop"];
                            dr["TW2Start"] = drLocationDetails["TW2Start"];
                            dr["TW2Stop"] = drLocationDetails["TW2Stop"];
                            dr["OpenTime"] = drLocationDetails["OpenTime"];
                            dr["CloseTime"] = drLocationDetails["CloseTime"];
                            dr["FixedServiceTime"] = drLocationDetails["FixedServiceTime"];
                            dr["StandardInstructions"] = GetTruncatedString(dgvOrders.Rows[i].Cells[GridColumns.StandardInstructions.GetHashCode()].Value.ToString(), 60, false);
                            dr["Address"] = GetTruncatedString(drLocationDetails["Address"].ToString(), 50, false);
                            dr["Landmark"] = GetTruncatedString(drLocationDetails["Landmark"].ToString(), 20, false);
                            dtLocations.Rows.Add(dr);
                        }

                        DataRow[] drOrderDetails = dtOrderDetails.Select(string.Format(@"OrderID = '{0}' AND CustomerCode = '{1}'", OrderID, CustomerCode));
                        foreach (DataRow drDetails in drOrderDetails)
                        {
                            drDetails["Checked"] = 1;
                            
                            string UOM = drDetails["UOM"].ToString();
                            string ItemCode = drDetails["ItemCode"].ToString();
                            if (dtPackageTypes.Select(string.Format(@"PackageTypeName = '{0}'", UOM)).Length == 0)
                            {
                                DataRow dr = dtPackageTypes.NewRow();
                                DataRow drPackageTypeDetails = dtOrderDetails.Select(string.Format(@"UOM = '{0}'", UOM))[0];
                                dr["PackageTypeID"] = UOM;
                                dr["PackageTypeName"] = UOM;
                                dr["EqivalencyFactor"] = drPackageTypeDetails["EquivalencyFactor"].ToString();
                                dr["Weight"] = drPackageTypeDetails["Weight"].ToString();
                                dr["Region"] = Region;
                                dtPackageTypes.Rows.Add(dr);
                            }
                            if (dtItems.Select(string.Format(@"ItemCode = '{0}'", ItemCode)).Length == 0)
                            {
                                DataRow dr = dtItems.NewRow();
                                DataRow drItemDetails = dtOrderDetails.Select(string.Format(@"ItemCode = '{0}'", ItemCode))[0];
                                dr["ItemCode"] = ItemCode;
                                dr["ItemName"] = drItemDetails["ItemName"].ToString();
                                dr["PackageTypeID"] = UOM;
                                dr["BrandID"] = GetTruncatedString(drItemDetails["Brand"].ToString(), 3, false);
                                dr["Region"] = Region;
                                dtItems.Rows.Add(dr);
                            }
                            DataRow drOrderLine = dtOrders.NewRow();
                            drOrderLine["OrderID"] = GetTruncatedString(OrderID, 15, true);
                            drOrderLine["CustomerCode"] = CustomerCode;
                            drOrderLine["ItemCode"] = ItemCode;
                            drOrderLine["Size1"] = drDetails["Size1"];
                            drOrderLine["Size2"] = drDetails["Size2"];
                            drOrderLine["Size3"] = drDetails["Size3"];
                            drOrderLine["Region"] = Region;
                            drOrderLine["Notes"] = dgvOrders.Rows[i].Cells[GridColumns.OrderNotes.GetHashCode()].Value.ToString();
                            dtOrders.Rows.Add(drOrderLine);
                        }
                    }
                }
                locations = dtLocations.Rows.Count;
                PackageTypes = dtPackageTypes.Rows.Count;
                SKUs = dtItems.Rows.Count;
                OrderLines = dtOrders.Rows.Count;
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return false;
            }
        }
        private void FillSummaries()
        {
            try
            {
                if (_isFillig)
                    return;

                int All_Rtn_Ord = 0, Sel_Rtn_Ord = 0, All_Rtn_Qty = 0, Sel_Rtn_Qty = 0;
                int All_Sal_Ord = 0, Sel_Sal_Ord = 0, All_Sal_Qty = 0, Sel_Sal_Qty = 0;

                for (int i = 0; i < dgvOrders.Rows.Count; i++)
                {
                    bool isChecked = Convert.ToBoolean(dgvOrders.Rows[i].Cells[GridColumns.Check.GetHashCode()].Value);
                    string CustomerCode = dgvOrders.Rows[i].Cells[GridColumns.CustomerCode.GetHashCode()].Value.ToString();
                    string OrderID = dgvOrders.Rows[i].Cells[GridColumns.OrderID.GetHashCode()].Value.ToString();
                    int OrderTypeID = Convert.ToInt16(dgvOrders.Rows[i].Cells[GridColumns.OrderTypeID.GetHashCode()].Value);
                    DataRow[] drOrderDetails = dtOrderDetails.Select(string.Format(@"OrderID = '{0}' AND CustomerCode = '{1}'", OrderID, CustomerCode));
                    decimal qty = 0;
                    foreach (DataRow drDetails in drOrderDetails)
                    {
                        qty += decimal.Parse(drDetails["Size1"].ToString());
                    }
                    if (OrderTypeID == 1)
                    {
                        All_Sal_Ord++;
                        All_Sal_Qty += (int)qty;
                        if (isChecked)
                        {
                            Sel_Sal_Ord++;
                            Sel_Sal_Qty += (int)qty;
                        }
                    }
                    else
                    {
                        All_Rtn_Ord++;
                        All_Rtn_Qty += (int)qty;
                        if (isChecked)
                        {
                            Sel_Rtn_Ord++;
                            Sel_Rtn_Qty += (int)qty;
                        }
                    }
                }

                txtSalesOrders.Text = Sel_Sal_Ord.ToString() + "/" + All_Sal_Ord.ToString();
                txtSalesQty.Text = Sel_Sal_Qty.ToString() + "/" + All_Sal_Qty.ToString();
                txtRtnOrders.Text = Sel_Rtn_Ord.ToString() + "/" + All_Rtn_Ord.ToString();
                txtRtnQty.Text = Sel_Rtn_Qty.ToString() + "/" + All_Rtn_Qty.ToString();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void btnSendOrders_Click(object sender, EventArgs e)
        {
            try
            {
                if (!dtpSessionDate.Checked && rbDelivery.Checked)
                {
                    MessageBox.Show("Select Session Date !!");
                    return;
                }
                bool isChecked = false;
                for (int i = 0; i < dgvOrders.Rows.Count; i++)
                {
                    isChecked = Convert.ToBoolean(dgvOrders.Rows[i].Cells[GridColumns.Check.GetHashCode()].Value);
                    if (isChecked)
                        break;
                }
                if (!isChecked)
                {
                    MessageBox.Show("No orders checked to send !!");
                    return;
                }

                frmRoadNetIntegrationExecution frm = new frmRoadNetIntegrationExecution(frmRoadNetIntegrationExecution.Mode.ExportOrders);
                frm.PrepareTablesForOrdersExportHandler += new frmRoadNetIntegrationExecution.PrepareTablesForOrdersExportDel(PrepareTables);
                frm.SendLocationsHandler += new frmRoadNetIntegrationExecution.SendLocationsDel(SendLocations);
                frm.SendPackageTypesHandler += new frmRoadNetIntegrationExecution.SendPackageTypesDel(SendPackageTypes);
                frm.SendSKUsHandler += new frmRoadNetIntegrationExecution.SendSKUsDel(SendSKUs);
                frm.SendOrdersHandler += new frmRoadNetIntegrationExecution.SendOrdersDel(SendOrders);

                frm.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private  bool SendLocations()
        {
            return RNManager.SendLocations(dtLocations);
        }
        private bool SendPackageTypes()
        {
            return RNManager.SendPackageTypes(dtPackageTypes);
        }
        private bool SendSKUs()
        {
            return RNManager.SendSKUs(dtItems);
        }
        private bool SendOrders(ref string PRN_Path)
        {
            try
            {
                DataTable dtHOOrders = new DataTable();
                if (rbHomeAndOffice.Checked)
                {
                    dtOrderDetails.DefaultView.RowFilter = "Checked = 1";
                    dtHOOrders = dtOrderDetails.DefaultView.ToTable();
                }
                return RNManager.SendOrders(rbHomeAndOffice.Checked ? dtHOOrders : dtOrders, dtpSessionDate.Value.Date, !rbHomeAndOffice.Checked, ref PRN_Path);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return false;
            }
        }
        private void frmRoadNetIntegration_Load(object sender, EventArgs e)
        {
            try
            {
                btnSendOrders.Enabled = false;
                if (CoreGeneral.Common.userPrivileges.MenuActionAccess.ContainsKey(MenuActions.RoadNet_Export_Delivery))
                {
                    rbDelivery.Enabled = true;
                }
                if (CoreGeneral.Common.userPrivileges.MenuActionAccess.ContainsKey(MenuActions.RoadNet_Export_HomeAndOffice))
                {
                    rbHomeAndOffice.Enabled = true;
                }
                if (rbHomeAndOffice.Enabled)
                    rbHomeAndOffice.Checked = true;
                else if (rbDelivery.Enabled)
                    rbDelivery.Checked = true;
                    
                tabControl1.TabPages.Remove(tpSessionDetails);
                dtpOrderDateFrom.Value = DateTime.Today;
                dtpOrderDateTo.Value = DateTime.Today;
                if (DateTime.Today.DayOfWeek == DayOfWeek.Thursday)
                {
                    dtpDeliveryDateFrom.Value = DateTime.Today.AddDays(2);
                    dtpDeliveryDateTo.Value = DateTime.Today.AddDays(2);
                }
                else
                {
                    dtpDeliveryDateFrom.Value = DateTime.Today.AddDays(1);
                    dtpDeliveryDateTo.Value = DateTime.Today.AddDays(1);
                }

                DataTable dtRegions = new DataTable();
                RNManager.GetRoadNetRegions(ref dtRegions);
                cmbRegion.DataSource = dtRegions;
                cmbRegion.DisplayMember = "REGION_ID";
                cmbRegion.ValueMember = "REGION_ID";
                
                dtRegions = new DataTable();
                RNManager.GetDefinedRegionsInSonic(ref dtRegions);
                DataGridViewComboBoxColumn col = (DataGridViewComboBoxColumn)dgvOrders.Columns[GridColumns.Region.GetHashCode()];
                col.DataSource = dtRegions;
                col.DisplayMember = "Region";
                col.ValueMember = "Region";

                //DataTable dtSecurityGroups = new DataTable();
                //RNManager.GetSalespersonGroupsAssignedToOperator(ref dtSecurityGroups);
                //cmbSecurityGroup.DataSource = dtSecurityGroups;
                //cmbSecurityGroup.ValueMember = "SecurityGroupID";
                //cmbSecurityGroup.DisplayMember = "Description";

                DateTime SessionDate = DateTime.Now.AddDays(1).Date;
                if (DateTime.Now.Hour < 6)
                {
                    SessionDate = DateTime.Now.Date;
                }
                else if (SessionDate.DayOfWeek == DayOfWeek.Friday)
                {
                    SessionDate = SessionDate.AddDays(1);
                }
                dtpSessionDate.Value = SessionDate;
                dtpSessionDate.Checked = false;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void btnGetOrders_Click(object sender, EventArgs e)
        {
            try
            {
                btnSendOrders.Enabled = false;

                dgvOrders.DataSource = null;
                int ChannelFilter = -1;
                if (rbHomeAndOffice.Checked)
                    ChannelFilter = 1;
                else if (rbDelivery.Checked)
                    ChannelFilter = 2;

                Result Res = RNManager.GetRoadNetOrdersTable(dtpOrderDateFrom.Value, dtpOrderDateTo.Value, dtpDeliveryDateFrom.Value, dtpDeliveryDateTo.Value, cmbRegion.SelectedValue == null ? "" : cmbRegion.SelectedValue.ToString(), txtOrderID.Text.Trim(), ChannelFilter, false, ref dtOrderDetails);
                
                if (Res == Result.Success)
                {
                    btnSendOrders.Enabled = dtOrderDetails.Rows.Count > 0;
                    dtOrderHeaders = dtOrderDetails.DefaultView.ToTable(true, new string[] { "OrderID", "OrderDate", "DeliveryDate", "CustomerCode", "CustomerName", "Region", "RouteCode", "OrderTypeID", "OrderType", "StandardInstructions", "OrderNotes" });
                    DataColumn colChecked = new DataColumn("Checked", typeof(int));
                    colChecked.DefaultValue = 0;
                    dtOrderDetails.Columns.Add(colChecked);

                    _isFillig = true;
                    dgvOrders.Rows.Clear();
                    DataGridViewRow row;
                    for (int i = 0; i < dtOrderHeaders.Rows.Count; i++)
                    {
                        dgvOrders.Rows.Add();
                        row = dgvOrders.Rows[i];
                        row.Cells[GridColumns.Check.GetHashCode()].Value = true;
                        row.Cells[GridColumns.OrderID.GetHashCode()].Value = dtOrderHeaders.Rows[i]["OrderID"].ToString();
                        row.Cells[GridColumns.CustomerCode.GetHashCode()].Value = dtOrderHeaders.Rows[i]["CustomerCode"].ToString();
                        row.Cells[GridColumns.CustomerName.GetHashCode()].Value = dtOrderHeaders.Rows[i]["CustomerName"].ToString();
                        row.Cells[GridColumns.OrderDate.GetHashCode()].Value = dtOrderHeaders.Rows[i]["OrderDate"];
                        row.Cells[GridColumns.DeliveryDate.GetHashCode()].Value = dtOrderHeaders.Rows[i]["DeliveryDate"];
                        row.Cells[GridColumns.Region.GetHashCode()].Value = dtOrderHeaders.Rows[i]["Region"];
                        row.Cells[GridColumns.StandardInstructions.GetHashCode()].Value = dtOrderHeaders.Rows[i]["StandardInstructions"];
                        row.Cells[GridColumns.Route.GetHashCode()].Value = dtOrderHeaders.Rows[i]["RouteCode"];
                        row.Cells[GridColumns.OrderTypeID.GetHashCode()].Value = dtOrderHeaders.Rows[i]["OrderTypeID"];
                        row.Cells[GridColumns.OrderType.GetHashCode()].Value = dtOrderHeaders.Rows[i]["OrderType"];
                        row.Cells[GridColumns.OrderNotes.GetHashCode()].Value = dtOrderHeaders.Rows[i]["OrderNotes"];
                    }
                    cbAll.Checked = dtOrderHeaders.Rows.Count > 0;
                    _isFillig = false;
                }
                FillSummaries();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                _isFillig = false;
            }
        }

        private void dgvOrders_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0)
                {
                    string CustomerCode = dgvOrders.Rows[e.RowIndex].Cells[GridColumns.CustomerCode.GetHashCode()].Value.ToString();
                    string OrderID = dgvOrders.Rows[e.RowIndex].Cells[GridColumns.OrderID.GetHashCode()].Value.ToString();
                    DateTime OrderDate = Convert.ToDateTime(dgvOrders.Rows[e.RowIndex].Cells[GridColumns.OrderDate.GetHashCode()].Value);
                    DataTable dtDetails = dtOrderDetails.Copy();
                    dtDetails.DefaultView.RowFilter = string.Format(@"OrderID = '{0}' AND CustomerCode = '{1}'", OrderID, CustomerCode);
                    frmOrderDetails frm = new frmOrderDetails(OrderID, CustomerCode, OrderDate, dtDetails.DefaultView.ToTable());
                    frm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void rbHomeAndOffice_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                dtpSessionDate.Enabled = rbDelivery.Checked;
                if (!dtpSessionDate.Enabled)
                    dtpSessionDate.Checked = false;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void rbDelivery_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                dtpSessionDate.Enabled = rbDelivery.Checked;
                if (!dtpSessionDate.Enabled)
                    dtpSessionDate.Checked = false;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private string GetTruncatedString(string input, int length, bool truncateBegining)
        {
            string output = "";
            try
            {
                output = input;
                if (input.Length > length)
                {
                    if (truncateBegining)
                        output = input.Substring(input.Length - length, length);
                    else
                        output = input.Substring(0, length);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return output;
        }
    }
}
