using InCubeIntegration_DAL;
using InCubeLibrary;
using SAP.Middleware.Connector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Xml;

namespace InCubeIntegration_BL
{
    public class IntegrationQNIEPresales : IntegrationBase // Live branch
    {
        BackgroundWorker bgwCheckProgress;
        InCubeDatabase db_res;
        int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
        InCubeQuery incubeQuery = null;
        string IntegrationOrg = "1100";
        string SendServerName = "";
        int HistoryDays = 0;
        string StagingTable = "";

        enum requiredColumns
        {
            ID, Customer, Saturday, Sunday, Monday, Tuesday, Wednesday, Thursday
        }

        public IntegrationQNIEPresales(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            if (CoreGeneral.Common.CurrentSession.loginType != LoginType.WindowsService)
            {
                db_res = new InCubeDatabase();
                db_res.Open("InCube", "IntegrationQNIE");
            }

            bgwCheckProgress = new BackgroundWorker();
            bgwCheckProgress.DoWork += new DoWorkEventHandler(bgw_CheckProgress);
            bgwCheckProgress.WorkerSupportsCancellation = true;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(CoreGeneral.Common.StartupPath + "\\DataSources.xml");
            SendServerName = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'InCubeSQL']/DataSend").InnerText;
            try
            {
                HistoryDays = int.Parse(xmlDoc.SelectSingleNode("Connections/DaysHistory[Name = 'Days']/Data").InnerText);
            }
            catch (Exception)
            {
                HistoryDays = 30;
            }
        }

        private void bgw_CheckProgress(object sender, DoWorkEventArgs e)
        {
            try
            {
                while (TriggerID != -1)
                {
                    int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
                    GetExecutionResults(StagingTable, ref TotalRows, ref Inserted, ref Updated, ref Skipped, db_res);
                    SetProgressMax(TotalRows);
                    ReportProgress(Inserted + Updated + Skipped);
                    System.Threading.Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        public override void Close()
        {
            if (db_res != null && db_res.GetConnection().State == ConnectionState.Open)
                db_res.Close();
        }

        public override void UpdateItem()
        {
            GetMasterData(IntegrationField.Item_U);
        }

        public override void UpdateCustomer()
        {
            GetMasterData(IntegrationField.Customer_U);
        }

        public override void UpdateSalesPerson()
        {
            GetMasterData(IntegrationField.Salesperson_U);
        }

        public override void UpdateSTA()
        {
            GetMasterData(IntegrationField.STA_U);
        }

        public override void UpdatePromotion()
        {
            GetMasterData(IntegrationField.Promotion_U);
        }

        public override void UpdatePrice()
        {
            GetMasterData(IntegrationField.Price_U);
        }

        public override void UpdateInvoice()
        {
            GetMasterData(IntegrationField.Invoice_U, Filters.FromDate, Filters.ToDate, Filters.OpenInvoicesOnly, Filters.CustomerCode);
        }

        public override void UpdateStock()
        {
            GetMasterData(IntegrationField.Stock_U);
        }

        private void GetMasterData(IntegrationField field)
        {
            GetMasterData(field, DateTime.MinValue, DateTime.MaxValue, false, "");
        }
        private void GetMasterData(IntegrationField field, DateTime FromDate, DateTime ToDate, bool GetOpenInvoicesOnly, string CustomerCode)
        {
            Result res = Result.UnKnown;
            string MasterName = "";
            string ProcName = "";
            Dictionary<int, string> Filters;
            int ProcessID = 0;
            bool ClearAll = true;

            try
            {
                //Log begining of read from SAP
                Filters = new Dictionary<int, string>();
                Filters.Add(1, "Reading from SAP");
                ProcessID = execManager.LogIntegrationBegining(TriggerID, 1, Filters);

                MasterName = field.ToString().Substring(0, field.ToString().Length - 2);
                WriteMessage("\r\nRetrieving " + MasterName + " from SAP ... ");
                DataTable dtMasterData = new DataTable();
                switch (field)
                {
                    case IntegrationField.Item_U:
                        res = GetItemTable(ref dtMasterData);
                        StagingTable = "Stg_Items";
                        ProcName = "sp_UpdateItems";
                        break;
                    case IntegrationField.Customer_U:
                        res = GetCustomerTable(ref dtMasterData);
                        StagingTable = "Stg_Customers";
                        ProcName = "sp_UpdateCustomers";
                        break;
                    case IntegrationField.Price_U:
                        res = GetPricesTable(ref dtMasterData);
                        StagingTable = "Stg_Prices";
                        ProcName = "sp_UpdatePrices";
                        break;
                    case IntegrationField.Promotion_U:
                        res = GetPromotionsTable(ref dtMasterData);
                        StagingTable = "Stg_Promotions";
                        ProcName = "sp_UpdatePromotions";
                        break;
                    case IntegrationField.Salesperson_U:
                        res = GetSalespersonTable(ref dtMasterData);
                        StagingTable = "Stg_Salespersons";
                        ProcName = "sp_UpdateSalespersons";
                        break;
                    case IntegrationField.Invoice_U:
                        res = GetOutstandingTable(FromDate, ToDate, GetOpenInvoicesOnly, CustomerCode, ref dtMasterData, ref ClearAll);
                        StagingTable = "Stg_Invoices";
                        ProcName = "sp_UpdateInvoices";
                        break;
                    case IntegrationField.Stock_U:
                        res = GetStockTable(ref dtMasterData);
                        StagingTable = "Stg_Stock";
                        ProcName = "sp_UpdateStock";
                        break;
                    case IntegrationField.STA_U:
                        res = GetOrderStatusTable(ref dtMasterData);
                        StagingTable = "Stg_OrderStatus";
                        ProcName = "sp_UpdateOrderStatus";
                        break;
                }

                if (res == Result.Success)
                {
                    WriteMessage(" Rows retrieved: " + dtMasterData.Rows.Count);
                    execManager.UpdateActionTotalRows(TriggerID, dtMasterData.Rows.Count);

                    WriteMessage("\r\nSaving data to staging table ... ");
                    res = SaveTable(dtMasterData, StagingTable);
                    if (res != Result.Success)
                    {
                        WriteMessage(" Error in saving to staging table !!");
                    }
                    else
                    {
                        WriteMessage(" Success ..");
                    }
                }
                else
                {
                    if (res == Result.Failure)
                        WriteMessage(" Error in reading from SAP !!");
                    else
                        WriteMessage(" No data found !!");
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("\r\nError !!!");
            }
            finally
            {
                execManager.LogIntegrationEnding(ProcessID, res, "", "");
            }
            if (res != Result.Success)
                return;


            try
            {
                //Log begining of procedure execution
                Filters = new Dictionary<int, string>();
                Filters.Add(2, "Procedure execution");
                ProcessID = execManager.LogIntegrationBegining(TriggerID, 1, Filters);

                WriteMessage("\r\nLooping through " + MasterName + " ...");

                if (CoreGeneral.Common.CurrentSession.loginType != LoginType.WindowsService)
                    bgwCheckProgress.RunWorkerAsync();

                Procedure proc = new Procedure(ProcName);
                if (field == IntegrationField.Invoice_U)
                {
                    proc.AddParameter("@ClearAll", ParamType.BIT, ClearAll ? 1 : 0);
                }
                res = ExecuteStoredProcedure(proc);
                if (res == Result.Success)
                    WriteMessage("\r\n" + MasterName + " updated ...");
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("\r\nError !!!");
            }
            finally
            {
                execManager.LogIntegrationEnding(ProcessID, res, "", "");
                TriggerID = -1;
                System.Threading.Thread.Sleep(550);
                if (res == Result.Success)
                {
                    GetExecutionResults(StagingTable, ref TotalRows, ref Inserted, ref Updated, ref Skipped, db_res);
                    WriteMessage("\r\nTotal rows found: " + TotalRows + ", Inserted: " + Inserted + ", Updated: " + Updated + ", Skipped: " + Skipped);
                    WriteMessage("\r\n=========================================================\r\n=========================================================\r\n");
                }
            }
        }
        private Result SaveTable(DataTable dtData, string TableName)
        {
            Result res = Result.Failure;
            try
            {
                dtData.Columns.Add("ID", typeof(int));
                dtData.Columns.Add("ResultID", typeof(int));
                dtData.Columns.Add("Message", typeof(string));
                dtData.Columns.Add("Inserted", typeof(bool));
                dtData.Columns.Add("Updated", typeof(bool));
                dtData.Columns.Add("Skipped", typeof(bool));
                for (int i = 0; i < dtData.Rows.Count; i++)
                {
                    dtData.Rows[i]["ID"] = (i + 1);
                }
                string columns = "";
                foreach (DataColumn col in dtData.Columns)
                {
                    columns += "\r\n        [" + col.Caption + "] ";
                    switch (col.DataType.Name)
                    {
                        case "Int32":
                            columns += "INT";
                            break;
                        case "Boolean":
                            columns += "BIT";
                            break;
                        default:
                            columns += "NVARCHAR(200)";
                            break;
                    }
                    columns += " NULL,";
                }

                string dataTableCreationQuery = string.Format(@"IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'{0}') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
	EXEC('DROP TABLE {0}');
END

EXEC('
CREATE TABLE {0}({1}
 );

')", TableName, columns.Substring(0, columns.Length - 1));

                incubeQuery = new InCubeQuery(db_vms, dataTableCreationQuery);
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    SqlBulkCopy bulk = new SqlBulkCopy(db_vms.GetConnection());
                    bulk.DestinationTableName = TableName;
                    bulk.WriteToServer(dtData);
                    res = Result.Success;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }


        private Result GetItemTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            DT = new DataTable();
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_MATERIAL_MASTER");

                IRfcTable tblImport = companyBapi.GetTable("IR_VKORG");
                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", IntegrationOrg);
                companyBapi.SetValue("IR_VKORG", tblImport);

                IRfcTable tblImport2 = companyBapi.GetTable("IR_WERKS");
                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", "1102");
                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", "1107");
                companyBapi.SetValue("IR_WERKS", tblImport2);

                IRfcTable tblImport3 = companyBapi.GetTable("IR_DATUM");
                tblImport2.Append();
                tblImport2.SetValue("SIGN", "I");
                tblImport2.SetValue("OPTION", "BT");
                tblImport2.SetValue("LOW", DateTime.Today.AddDays(-3).ToString("yyyyMMdd"));
                tblImport2.SetValue("HIGH", DateTime.Today.AddDays(1).ToString("yyyyMMdd"));
                companyBapi.SetValue("IR_DATUM", tblImport3);

                companyBapi.Invoke(prd);
                IRfcTable detail = companyBapi.GetTable("ITEMTAB");

                DT.Columns.Add("ItemCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemName", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemCategoryCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemCategoryDescription", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemGroupCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemGroupName", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemBrandCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemBrandName", System.Type.GetType("System.String"));
                DT.Columns.Add("BaseUOM", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemUOM", System.Type.GetType("System.String"));
                DT.Columns.Add("Numerator", System.Type.GetType("System.String"));
                DT.Columns.Add("Denominator", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionCode", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionName", System.Type.GetType("System.String"));
                DT.Columns.Add("CompanyCodeLevel2", System.Type.GetType("System.String"));
                DT.Columns.Add("CompanyCodeLevel3", System.Type.GetType("System.String"));
                DT.Columns.Add("FLAG", System.Type.GetType("System.String"));
                DT.Columns.Add("Barcode", System.Type.GetType("System.String"));
                DT.Columns.Add("InActive", System.Type.GetType("System.String"));
                DT.Columns.Add("CWM", System.Type.GetType("System.String"));

                foreach (IRfcStructure row in detail)
                {
                    DataRow _row = DT.NewRow();
                    _row["ItemCode"] = row.GetValue("MATNR").ToString();
                    _row["ItemName"] = row.GetValue("GROES").ToString();
                    _row["ItemCategoryCode"] = row.GetValue("EXTWG").ToString();
                    _row["ItemCategoryDescription"] = row.GetValue("EWBEZ").ToString();
                    _row["ItemGroupCode"] = row.GetValue("PRDHA").ToString();
                    _row["ItemGroupName"] = row.GetValue("BEZEI").ToString();
                    _row["ItemBrandCode"] = row.GetValue("MATKL").ToString();
                    _row["ItemBrandName"] = row.GetValue("WGBEZ").ToString();
                    _row["BaseUOM"] = row.GetValue("MEINS").ToString();
                    _row["ItemUOM"] = row.GetValue("MEINH").ToString();
                    if (_row["ItemUOM"].ToString().Trim().ToLower().Equals("l") || _row["ItemUOM"].ToString().Trim().ToLower().Equals("ml")) continue;
                    _row["Numerator"] = row.GetValue("UMREZ").ToString();
                    _row["Denominator"] = row.GetValue("UMREN").ToString();
                    _row["DivisionCode"] = row.GetValue("SPART").ToString();
                    _row["DivisionName"] = row.GetValue("VTEXT").ToString();
                    _row["CompanyCodeLevel2"] = row.GetValue("BUKRS").ToString();
                    _row["CompanyCodeLevel3"] = row.GetValue("VKORG").ToString();
                    _row["FLAG"] = row.GetValue("ZFLAG").ToString();
                    _row["Barcode"] = row.GetValue("EAN11").ToString();
                    _row["InActive"] = row.GetValue("LVORM").ToString();
                    _row["CWM"] = row.GetValue("CWMAT").ToString();
                    DT.Rows.Add(_row);
                    DT.AcceptChanges();
                }

                if (DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result GetCustomerTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            DT = new DataTable();
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                //Function
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_CUSTOMER_MASTER");
                //Table name (Array name)
                IRfcTable tblImport = companyBapi.GetTable("IR_VKORG");
                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", IntegrationOrg);
                companyBapi.SetValue("IR_VKORG", tblImport);

                IRfcTable tblImport2 = companyBapi.GetTable("IR_SPART");
                //tblImport2.Append();
                //tblImport2.SetValue("SIGN", "I");
                //tblImport2.SetValue("OPTION", "EQ");
                //tblImport2.SetValue("LOW", "15");
                tblImport2.Append();
                tblImport2.SetValue("SIGN", "I");
                tblImport2.SetValue("OPTION", "EQ");
                tblImport2.SetValue("LOW", "20");
                companyBapi.SetValue("IR_SPART", tblImport2);

                IRfcTable tblImport3 = companyBapi.GetTable("IR_VTWEG");
                tblImport3.Append();
                tblImport3.SetValue("SIGN", "I");
                tblImport3.SetValue("OPTION", "BT");
                tblImport3.SetValue("LOW", "A1");
                tblImport3.SetValue("HIGH", "A6");
                companyBapi.SetValue("IR_VTWEG", tblImport3);

                //IRfcTable tblImport5 = companyBapi.GetTable("IR_DATUM");
                //tblImport5.Append();
                //tblImport5.SetValue("SIGN", "I");
                //tblImport5.SetValue("OPTION", "BT");
                //tblImport5.SetValue("LOW", DateTime.Today.AddDays(-3).ToString("yyyyMMdd"));
                //tblImport5.SetValue("HIGH", DateTime.Today.ToString("yyyyMMdd"));
                //companyBapi.SetValue("IR_DATUM", tblImport5);

                companyBapi.Invoke(prd);
                IRfcTable detail = companyBapi.GetTable("ITEMTAB");

                DT.Columns.Add("BillToCode", System.Type.GetType("System.String"));
                DT.Columns.Add("BillToName", System.Type.GetType("System.String"));
                DT.Columns.Add("ShipToCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ShipToName", System.Type.GetType("System.String"));
                DT.Columns.Add("PayerCode", System.Type.GetType("System.String"));
                DT.Columns.Add("PayerName", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerType", System.Type.GetType("System.String"));
                DT.Columns.Add("RiskCategory", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerPriceGroup", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerPriceGroupName", System.Type.GetType("System.String"));
                DT.Columns.Add("DeleteStatus", System.Type.GetType("System.String"));
                DT.Columns.Add("BlockStatus", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionCode", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionName", System.Type.GetType("System.String"));
                DT.Columns.Add("Address", System.Type.GetType("System.String"));
                DT.Columns.Add("ContactName", System.Type.GetType("System.String"));
                DT.Columns.Add("Telephone", System.Type.GetType("System.String"));
                DT.Columns.Add("Mobile", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerCategoryCode", System.Type.GetType("System.String"));
                DT.Columns.Add("CategoryName", System.Type.GetType("System.String"));
                DT.Columns.Add("PaymentTermDays", System.Type.GetType("System.String"));
                DT.Columns.Add("PaymentTermType", System.Type.GetType("System.String"));
                DT.Columns.Add("CreditLimit", System.Type.GetType("System.String"));
                DT.Columns.Add("Zone", System.Type.GetType("System.String"));
                DT.Columns.Add("Region", System.Type.GetType("System.String"));
                DT.Columns.Add("ZoneCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ChannelCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ChannelName", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerClass", System.Type.GetType("System.String"));
                DT.Columns.Add("RouteCode", System.Type.GetType("System.String"));
                DT.Columns.Add("RouteName", System.Type.GetType("System.String"));
                DT.Columns.Add("FLAG", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesmanCode", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesOrganizationCode", System.Type.GetType("System.String"));
                DT.Columns.Add("CompanyCode", System.Type.GetType("System.String"));
                DT.Columns.Add("GPS_Long", System.Type.GetType("System.String"));
                DT.Columns.Add("GPS_Lat", System.Type.GetType("System.String"));

                foreach (IRfcStructure row in detail)
                {
                    if (row.GetValue("KUNAG").ToString().ToString().Trim().Equals(string.Empty)) continue;
                    DataRow _row = DT.NewRow();
                    _row["BillToCode"] = row.GetValue("KUNAG").ToString().Trim();
                    string typeC = row.GetValue("SPART").ToString();
                    _row["BillToName"] = row.GetValue("NAMAG").ToString();
                    _row["ShipToCode"] = row.GetValue("KUNWE").ToString().Trim();
                    _row["ShipToName"] = row.GetValue("NAMWE").ToString();
                    _row["PayerCode"] = row.GetValue("KUNRG").ToString().Trim();
                    _row["PayerName"] = row.GetValue("NAMRG").ToString();
                    _row["CustomerType"] = row.GetValue("CTYPE").ToString();
                    try
                    {
                        _row["RiskCategory"] = row.GetValue("CTLPC").ToString();
                    }
                    catch
                    {
                        _row["RiskCategory"] = "";
                    }
                    _row["CustomerPriceGroup"] = row.GetValue("KONDA").ToString();
                    _row["CustomerPriceGroupName"] = row.GetValue("PGTXT").ToString();
                    _row["DeleteStatus"] = row.GetValue("LOEVM").ToString();
                    _row["BlockStatus"] = row.GetValue("AUFSD").ToString();
                    _row["DivisionCode"] = row.GetValue("SPART").ToString();
                    _row["DivisionName"] = row.GetValue("VTEXT").ToString();
                    _row["Address"] = row.GetValue("ADDRS").ToString();
                    _row["ContactName"] = row.GetValue("CCNAM").ToString();
                    _row["Telephone"] = row.GetValue("TELF1").ToString();
                    _row["Mobile"] = row.GetValue("TELF2").ToString();
                    _row["CustomerCategoryCode"] = row.GetValue("KDGRP").ToString();
                    _row["CategoryName"] = row.GetValue("KTEXT").ToString();
                    _row["PaymentTermDays"] = row.GetValue("PTDAY").ToString();
                    if (_row["PaymentTermDays"].ToString().Trim().Equals(string.Empty)) continue;
                    int termdays = 0;
                    if (int.TryParse(_row["PaymentTermDays"].ToString().Trim(), out termdays))
                    {
                        if (termdays > 0)
                        {
                            _row["CustomerType"] = "1";
                        }
                        else
                        {
                            _row["CustomerType"] = "0";
                        }
                    }
                    else
                    {
                        continue;
                    }
                    _row["PaymentTermType"] = row.GetValue("PTTYP").ToString();
                    _row["CreditLimit"] = row.GetValue("KLIMK").ToString();
                    _row["Zone"] = row.GetValue("LZONE").ToString();
                    _row["Region"] = row.GetValue("REGIO").ToString();
                    _row["ZoneCode"] = row.GetValue("BLAND").ToString();
                    _row["ChannelCode"] = row.GetValue("VTWEG").ToString();
                    _row["ChannelName"] = row.GetValue("DTEXT").ToString();
                    _row["CustomerClass"] = row.GetValue("KLABC").ToString();
                    _row["RouteCode"] = row.GetValue("RCODE").ToString();
                    _row["RouteName"] = row.GetValue("RNAME").ToString();
                    _row["FLAG"] = row.GetValue("ZFLAG").ToString();
                    _row["SalesmanCode"] = row.GetValue("PERNR").ToString();
                    _row["SalesOrganizationCode"] = row.GetValue("VKORG").ToString();
                    _row["CompanyCode"] = row.GetValue("BUKRS").ToString();
                    _row["GPS_Long"] = row.GetValue("BAHNS").ToString();
                    _row["GPS_Lat"] = row.GetValue("BAHNE").ToString();

                    DT.Rows.Add(_row);
                    DT.AcceptChanges();
                }

                if (DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result GetSalespersonTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            DT = new DataTable();
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_SALESMAN_MASTER");
                IRfcTable tblImport = companyBapi.GetTable("IR_BUKRS");

                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", IntegrationOrg);
                companyBapi.SetValue("IR_BUKRS", tblImport);
                companyBapi.Invoke(prd);

                IRfcTable detail = companyBapi.GetTable("ITEMTAB");

                DT.Columns.Add("SalesmanCode", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesmanName", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesmanPhone", System.Type.GetType("System.String"));
                DT.Columns.Add("SupervisorCode", System.Type.GetType("System.String"));
                DT.Columns.Add("SuperVisorName", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesManagerCode", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesManagerName", System.Type.GetType("System.String"));
                DT.Columns.Add("OrganizationCode", System.Type.GetType("System.String"));
                DT.Columns.Add("Status", System.Type.GetType("System.String"));

                foreach (IRfcStructure row in detail)
                {
                    DataRow _row = DT.NewRow();

                    _row["SalesmanCode"] = row.GetValue("PERNR").ToString();
                    _row["SalesmanName"] = row.GetValue("ENAME").ToString();
                    _row["SalesmanPhone"] = row.GetValue("TELF1").ToString();
                    _row["SupervisorCode"] = row.GetValue("ENSPV").ToString();
                    _row["SuperVisorName"] = row.GetValue("NMSPV").ToString();
                    _row["SalesManagerCode"] = row.GetValue("ENMGR").ToString();
                    _row["SalesManagerName"] = row.GetValue("NMNGR").ToString();
                    _row["OrganizationCode"] = row.GetValue("BUKRS").ToString();
                    _row["Status"] = row.GetValue("ZFLAG").ToString();

                    DT.Rows.Add(_row);
                    DT.AcceptChanges();
                }

                if (DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result GetPricesTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            DT = new DataTable();
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_PRICE_MASTER");

                IRfcTable tblImport = companyBapi.GetTable("IR_VKORG");
                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", IntegrationOrg);
                companyBapi.SetValue("IR_VKORG", tblImport);

                IRfcTable tblImport2 = companyBapi.GetTable("IR_SPART");
                tblImport2.Append();
                tblImport2.SetValue("SIGN", "I");
                tblImport2.SetValue("OPTION", "EQ");
                tblImport2.SetValue("LOW", "15");
                tblImport2.Append();
                tblImport2.SetValue("SIGN", "I");
                tblImport2.SetValue("OPTION", "EQ");
                tblImport2.SetValue("LOW", "20");
                companyBapi.SetValue("IR_SPART", tblImport2);

                //IRfcTable tblImport5 = companyBapi.GetTable("IR_DATUM");
                //tblImport5.Append();
                //tblImport5.SetValue("SIGN", "I");
                //tblImport5.SetValue("OPTION", "BT");
                //tblImport5.SetValue("LOW", DateTime.Today.AddDays(-1).ToString("yyyyMMdd"));
                //tblImport5.SetValue("HIGH", DateTime.Today.AddDays(1).ToString("yyyyMMdd"));
                //companyBapi.SetValue("IR_DATUM", tblImport5);

                companyBapi.Invoke(prd);
                IRfcTable detail = companyBapi.GetTable("ITEMTAB");

                DT.Columns.Add("CompanyCode", System.Type.GetType("System.String"));
                DT.Columns.Add("CompanyCodeLevel3", System.Type.GetType("System.String"));
                DT.Columns.Add("PriceListName", System.Type.GetType("System.String"));
                DT.Columns.Add("ValidFrom", System.Type.GetType("System.String"));
                DT.Columns.Add("ValidTo", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemCode", System.Type.GetType("System.String"));
                DT.Columns.Add("UOM", System.Type.GetType("System.String"));
                DT.Columns.Add("ConversionFactor", System.Type.GetType("System.String"));
                DT.Columns.Add("Price", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerCode", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesGroupCode", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ChannelCode", System.Type.GetType("System.String"));
                DT.Columns.Add("StockStatus", System.Type.GetType("System.String"));
                DT.Columns.Add("FLAG", System.Type.GetType("System.String"));
                DT.Columns.Add("IsDeleted", System.Type.GetType("System.String"));
                DT.Columns.Add("PriceListCode", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("OutletID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("GroupID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("DivisionID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("PackID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("ItemID", System.Type.GetType("System.Int16"));

                foreach (IRfcStructure row in detail)
                {
                    DataRow _row = DT.NewRow();

                    _row["CompanyCode"] = row.GetValue("BUKRS").ToString();
                    _row["CompanyCodeLevel3"] = row.GetValue("VKORG").ToString();
                    _row["PriceListName"] = row.GetValue("OBJKY").ToString();
                    _row["ValidFrom"] = row.GetValue("DATAB").ToString();
                    _row["ValidTo"] = row.GetValue("DATBI").ToString();
                    _row["ItemCode"] = row.GetValue("MATNR").ToString().Trim();
                    _row["UOM"] = row.GetValue("KMEIN").ToString();
                    _row["ConversionFactor"] = row.GetValue("CONVF").ToString();
                    _row["Price"] = row.GetValue("KBETR").ToString();
                    if (!row.GetValue("KUNWE").ToString().Trim().Equals(string.Empty))
                    {
                        _row["CustomerCode"] = row.GetValue("KUNWE").ToString().Trim();
                    }
                    if (!row.GetValue("KONDA").ToString().Trim().Equals(string.Empty))
                    {
                        _row["SalesGroupCode"] = row.GetValue("KONDA").ToString();
                    }
                    _row["DivisionCode"] = row.GetValue("SPART").ToString();
                    _row["ChannelCode"] = row.GetValue("VTWEG").ToString();
                    _row["StockStatus"] = "";
                    _row["FLAG"] = row.GetValue("ZFLAG").ToString();
                    _row["IsDeleted"] = row.GetValue("LOEVM").ToString();

                    DT.Rows.Add(_row);
                    DT.AcceptChanges();
                }

                if (DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result GetPromotionsTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            DT = new DataTable();
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_FOC_MASTER");

                IRfcTable tblImport = companyBapi.GetTable("IR_VKORG");
                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", IntegrationOrg);
                companyBapi.SetValue("IR_VKORG", tblImport);

                IRfcTable tblImport2 = companyBapi.GetTable("IR_SPART");
                //tblImport2.Append();
                //tblImport2.SetValue("SIGN", "I");
                //tblImport2.SetValue("OPTION", "EQ");
                //tblImport2.SetValue("LOW", "15");
                tblImport2.Append();
                tblImport2.SetValue("SIGN", "I");
                tblImport2.SetValue("OPTION", "EQ");
                tblImport2.SetValue("LOW", "20");
                companyBapi.SetValue("IR_SPART", tblImport2);

                //IRfcTable tblImport5 = companyBapi.GetTable("IR_DATUM");      
                //tblImport5.Append();
                //tblImport5.SetValue("SIGN", "I");
                //tblImport5.SetValue("OPTION", "BT");
                //tblImport5.SetValue("LOW", DateTime.Today.AddDays(-1).ToString("yyyyMMdd"));
                //tblImport5.SetValue("HIGH", DateTime.Today.ToString("yyyyMMdd"));
                //companyBapi.SetValue("IR_DATUM", tblImport5);

                companyBapi.Invoke(prd);
                IRfcTable detail = companyBapi.GetTable("ITEMTAB");

                DT.Columns.Add("Companycode ", System.Type.GetType("System.String"));
                DT.Columns.Add("CompanyCodeLevel3", System.Type.GetType("System.String"));
                DT.Columns.Add("ValidFrom", System.Type.GetType("System.String"));
                DT.Columns.Add("ValidTo", System.Type.GetType("System.String"));
                DT.Columns.Add("BuyItemCode", System.Type.GetType("System.String"));
                DT.Columns.Add("BuyUOM", System.Type.GetType("System.String"));
                DT.Columns.Add("BuyConversionFactor", System.Type.GetType("System.String"));
                DT.Columns.Add("BuyQuantity", System.Type.GetType("System.String"));
                DT.Columns.Add("GetItemCode", System.Type.GetType("System.String"));
                DT.Columns.Add("GetUOM", System.Type.GetType("System.String"));
                DT.Columns.Add("GetConversionFactor", System.Type.GetType("System.String"));
                DT.Columns.Add("GetQuantity", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionCode", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesGroupCode", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerShipTo", System.Type.GetType("System.String"));
                DT.Columns.Add("InclusiveOrExclusive", System.Type.GetType("System.String"));
                DT.Columns.Add("FLAG", System.Type.GetType("System.String"));
                DT.Columns.Add("IsDeleted", System.Type.GetType("System.String"));

                foreach (IRfcStructure row in detail)
                {
                    DataRow _row = DT.NewRow();
                    _row["Companycode "] = row.GetValue("BUKRS").ToString();
                    _row["CompanyCodeLevel3"] = row.GetValue("VKORG").ToString();
                    _row["ValidFrom"] = row.GetValue("DATAB").ToString();
                    _row["ValidTo"] = row.GetValue("DATBI").ToString();
                    _row["BuyItemCode"] = row.GetValue("MAT01").ToString().Trim();
                    _row["BuyUOM"] = row.GetValue("UOM01").ToString();
                    _row["BuyConversionFactor"] = row.GetValue("CNV01").ToString();
                    _row["BuyQuantity"] = row.GetValue("QTY01").ToString();
                    _row["GetItemCode"] = row.GetValue("MAT02").ToString().Trim();
                    _row["GetUOM"] = row.GetValue("UOM02").ToString();
                    _row["GetConversionFactor"] = row.GetValue("CNV02").ToString();
                    _row["GetQuantity"] = row.GetValue("QTY02").ToString();
                    _row["DivisionCode"] = row.GetValue("SPART").ToString();
                    _row["SalesGroupCode"] = row.GetValue("KONDA").ToString();
                    _row["CustomerShipTo"] = row.GetValue("KUNWE").ToString().Trim();
                    _row["InclusiveOrExclusive"] = row.GetValue("KNRDD").ToString();
                    _row["FLAG"] = row.GetValue("ZFLAG").ToString();
                    _row["IsDeleted"] = row.GetValue("LOEVM").ToString();

                    DT.Rows.Add(_row);
                    DT.AcceptChanges();
                }

                if (DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result GetOutstandingTable(DateTime FromDate, DateTime ToDate, bool GetOpenInvoicesOnly, string CustomerCode, ref DataTable DT, ref bool ClearAll)
        {
            Result res = Result.Failure;
            DT = new DataTable();
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_OPEN_INVOICES");

                IRfcTable tblImport = companyBapi.GetTable("IR_BUKRS");
                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", IntegrationOrg);
                companyBapi.SetValue("IR_BUKRS", tblImport);
                System.Text.StringBuilder parameters = new System.Text.StringBuilder();
                parameters.AppendLine("IR_BUKRS = " + IntegrationOrg);
                parameters.AppendLine("IR_BUKRS: SIGN = I, OPTION = EQ, LOW = " + IntegrationOrg);

                parameters.AppendLine("CustomerCode = " + CustomerCode);
                if (CustomerCode != string.Empty)
                {
                    ClearAll = false;
                    IRfcTable tblImport2 = companyBapi.GetTable("IR_KUNRG");
                    tblImport2.Append();
                    tblImport2.SetValue("SIGN", "I");
                    tblImport2.SetValue("OPTION", "EQ");
                    tblImport2.SetValue("LOW", "0000" + CustomerCode);
                    companyBapi.SetValue("IR_KUNRG", tblImport2);
                }
                else
                {
                    incubeQuery = new InCubeQuery(db_vms, "SELECT PayerCode FROM Payer");
                    incubeQuery.Execute();
                    DataTable dtPayers = incubeQuery.GetDataTable();
                    if (dtPayers.Rows.Count > 0)
                    {
                        IRfcTable tblImport2 = companyBapi.GetTable("IR_KUNRG");
                        for (int i = 0; i < dtPayers.Rows.Count; i++)
                        {
                            tblImport2.Append();
                            tblImport2.SetValue("SIGN", "I");
                            tblImport2.SetValue("OPTION", "EQ");
                            tblImport2.SetValue("LOW", "0000" + dtPayers.Rows[i]["PayerCode"].ToString());
                            companyBapi.SetValue("IR_KUNRG", tblImport2);
                        }
                    }
                }

                parameters.AppendLine("GetOpenInvoicesOnly = " + GetOpenInvoicesOnly);
                if (GetOpenInvoicesOnly)
                {
                    companyBapi.SetValue("IV_INVTYP", "O");
                }
                else
                {
                    ClearAll = false;
                    companyBapi.SetValue("IV_INVTYP", "B");
                }

                companyBapi.SetValue("IV_PSIZE", "10000");

                parameters.AppendLine("FromDate = " + FromDate);
                parameters.AppendLine("ToDate = " + ToDate);
                if (FromDate != DateTime.MinValue && ToDate != DateTime.MaxValue)
                {
                    ClearAll = false;
                    IRfcTable tblImport5 = companyBapi.GetTable("IR_DATE");
                    tblImport5.Append();
                    tblImport5.SetValue("SIGN", "I");
                    if (FromDate != ToDate)
                    {
                        tblImport5.SetValue("OPTION", "BT");
                        tblImport5.SetValue("LOW", FromDate.Date.ToString("yyyyMMdd"));
                        tblImport5.SetValue("HIGH", ToDate.Date.ToString("yyyyMMdd"));
                    }
                    else
                    {
                        tblImport5.SetValue("OPTION", "EQ");
                        tblImport5.SetValue("LOW", FromDate.Date.ToString("yyyyMMdd"));
                    }
                    companyBapi.SetValue("IR_DATE", tblImport5);
                }
                parameters.AppendLine("ClearAll = " + ClearAll);
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, parameters.ToString(), LoggingType.Information, LoggingFiles.errorInv);
                companyBapi.Invoke(prd);

                IRfcTable detail = companyBapi.GetTable("ITEMTAB");

                DT.Columns.Add("COMPANYCODE", System.Type.GetType("System.String"));
                DT.Columns.Add("SoldTo", System.Type.GetType("System.String"));
                DT.Columns.Add("ShipTo", System.Type.GetType("System.String"));
                DT.Columns.Add("PayerCode", System.Type.GetType("System.String"));
                DT.Columns.Add("TransactionNumber", System.Type.GetType("System.String"));
                DT.Columns.Add("SAP_REF_NO", System.Type.GetType("System.String"));
                DT.Columns.Add("TotalAmount", System.Type.GetType("System.String"));
                DT.Columns.Add("RemainingAmount", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesmanCode", System.Type.GetType("System.String"));
                DT.Columns.Add("TransactionType", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionCode", System.Type.GetType("System.String"));
                DT.Columns.Add("InvoiceDate", System.Type.GetType("System.String"));
                DT.Columns.Add("IsExist", System.Type.GetType("System.Boolean"));
                DT.Columns.Add("CustomerID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("OutletID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("AccountID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("EmployeeID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("DivisionID", System.Type.GetType("System.Int16"));

                foreach (IRfcStructure row in detail)
                {
                    DataRow _row = DT.NewRow();
                    _row["COMPANYCODE"] = row.GetValue("BUKRS").ToString();
                    _row["SoldTo"] = row.GetValue("KUNAG").ToString().Trim();
                    _row["ShipTo"] = row.GetValue("KUNWE").ToString().Trim();
                    _row["PayerCode"] = row.GetValue("KUNRG").ToString().Trim();
                    _row["TransactionNumber"] = row.GetValue("XBLNR").ToString();
                    _row["SAP_REF_NO"] = row.GetValue("VBELN").ToString();
                    _row["TotalAmount"] = row.GetValue("NETWR").ToString();
                    _row["RemainingAmount"] = row.GetValue("BLAMT").ToString();
                    _row["SalesmanCode"] = row.GetValue("PERNR").ToString();
                    _row["TransactionType"] = row.GetValue("VGART").ToString();
                    _row["DivisionCode"] = row.GetValue("SPART").ToString();
                    _row["InvoiceDate"] = row.GetValue("FKDAT").ToString();

                    DT.Rows.Add(_row);
                    DT.AcceptChanges();
                }

                if (DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result GetStockTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            DT = new DataTable();
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_WAREHOUSE_STOCK");

                IRfcTable tblImport = companyBapi.GetTable("IR_BUKRS");
                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", IntegrationOrg);
                companyBapi.SetValue("IR_BUKRS", tblImport);

                IRfcTable tblImport2 = companyBapi.GetTable("IR_WERKS");
                tblImport2.Append();
                tblImport2.SetValue("SIGN", "I");
                tblImport2.SetValue("OPTION", "EQ");
                tblImport2.SetValue("LOW", "1102");
                companyBapi.SetValue("IR_WERKS", tblImport2);

                IRfcTable tblImport3 = companyBapi.GetTable("IR_WERKS");
                tblImport3.Append();
                tblImport3.SetValue("SIGN", "I");
                tblImport3.SetValue("OPTION", "EQ");
                tblImport3.SetValue("LOW", "1107");
                companyBapi.SetValue("IR_WERKS", tblImport3);

                IRfcTable tblImport4 = companyBapi.GetTable("IR_LGORT");
                tblImport4.Append();
                tblImport4.SetValue("SIGN", "I");
                tblImport4.SetValue("OPTION", "EQ");
                tblImport4.SetValue("LOW", "0001");
                companyBapi.SetValue("IR_LGORT", tblImport4);

                companyBapi.Invoke(prd);
                IRfcTable detail = companyBapi.GetTable("ITEMTAB");

                DT.Columns.Add("COMPANYCODE", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionCode", System.Type.GetType("System.String"));
                DT.Columns.Add("Plant", System.Type.GetType("System.String"));
                DT.Columns.Add("StorageLocation", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemUOM", System.Type.GetType("System.String"));
                DT.Columns.Add("Denominator", System.Type.GetType("System.String"));
                DT.Columns.Add("Enumertaor", System.Type.GetType("System.String"));
                DT.Columns.Add("Quantity", System.Type.GetType("System.String"));
                DT.Columns.Add("CWQuantity", System.Type.GetType("System.String"));
                DT.Columns.Add("WarehouseID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("PackID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("Qty", System.Type.GetType("System.Decimal"));

                foreach (IRfcStructure row in detail)
                {
                    DataRow _row = DT.NewRow();
                    _row["COMPANYCODE"] = row.GetValue("BUKRS").ToString();
                    _row["DivisionCode"] = row.GetValue("SPART").ToString();
                    _row["Plant"] = row.GetValue("WERKS").ToString();
                    _row["StorageLocation"] = row.GetValue("LGORT").ToString();
                    _row["ItemCode"] = row.GetValue("MATNR").ToString();
                    _row["ItemUOM"] = row.GetValue("MEINS").ToString();
                    _row["Denominator"] = row.GetValue("UMREN").ToString();
                    _row["Enumertaor"] = row.GetValue("UMREZ").ToString();
                    _row["Quantity"] = row.GetValue("LABST").ToString();
                    _row["CWQuantity"] = row.GetValue("/CWM/LABST").ToString();

                    DT.Rows.Add(_row);
                    DT.AcceptChanges();
                }

                if (DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }

        private Result GetOrderStatusTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            DT = new DataTable();
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_PRESALES_ORDER_STATUS");

                IRfcTable tblImport = companyBapi.GetTable("IR_VKORG");
                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", IntegrationOrg);
                companyBapi.SetValue("IR_VKORG", tblImport);
                string parameters = "IR_VKORG: SIGN = I, OPTION = EQ, LOW = " + IntegrationOrg + "\r\n";

                IRfcTable tblImport2 = companyBapi.GetTable("IR_SPART");
                tblImport2.Append();
                tblImport2.SetValue("SIGN", "I");
                tblImport2.SetValue("OPTION", "EQ");
                tblImport2.SetValue("LOW", "15");
                tblImport2.Append();
                tblImport2.SetValue("SIGN", "I");
                tblImport2.SetValue("OPTION", "EQ");
                tblImport2.SetValue("LOW", "20");
                companyBapi.SetValue("IR_SPART", tblImport2);
                parameters += "IR_SPART: SIGN = I, OPTION = EQ, LOW = " + "15" + "\r\n";
                parameters += "IR_SPART: SIGN = I, OPTION = EQ, LOW = " + "20" + "\r\n";

                IRfcTable tblImport3 = companyBapi.GetTable("IR_DATUM");
                tblImport3.Append();
                tblImport3.SetValue("SIGN", "I");
                tblImport3.SetValue("OPTION", "BT");
                tblImport3.SetValue("LOW", DateTime.Today.AddDays(-3).ToString("yyyyMMdd"));
                tblImport3.SetValue("HIGH", DateTime.Today.AddDays(1).ToString("yyyyMMdd"));
                companyBapi.SetValue("IR_DATUM", tblImport3);
                parameters += "IR_DATUM: SIGN = I, OPTION = BT, LOW = " + DateTime.Today.AddDays(-3).ToString("yyyyMMdd") + ", HIGH = " + DateTime.Today.AddDays(1).ToString("yyyyMMdd") + "\r\n";

                IRfcTable tblImport4 = companyBapi.GetTable("IR_AUART");
                tblImport4.Append();
                tblImport4.SetValue("SIGN", "I");
                tblImport4.SetValue("OPTION", "EQ");
                tblImport4.SetValue("LOW", "ZPR");
                companyBapi.SetValue("IR_AUART", tblImport4);
                parameters += "IR_AUART: SIGN = I, OPTION = EQ, LOW = " + "ZPR" + "\r\n";

                companyBapi.SetValue("IV_PSIZE", "5000");
                parameters += "IV_PSIZE: 5000";

                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, parameters, LoggingType.Information, LoggingFiles.errorInv);
                companyBapi.Invoke(prd);
                IRfcTable detail = companyBapi.GetTable("ITEMTAB");

                DT.Columns.Add("CompanyCode", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesOrganization", System.Type.GetType("System.String"));
                DT.Columns.Add("DocumentNumber", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionCode", System.Type.GetType("System.String"));
                DT.Columns.Add("DistributionChannel", System.Type.GetType("System.String"));
                DT.Columns.Add("DocumentType", System.Type.GetType("System.String"));
                DT.Columns.Add("ShipToCode", System.Type.GetType("System.String"));
                DT.Columns.Add("Plant", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemCode", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesUnit", System.Type.GetType("System.String"));
                DT.Columns.Add("OrderQuantity", System.Type.GetType("System.String"));
                DT.Columns.Add("ConfirmedQuantity", System.Type.GetType("System.String"));
                DT.Columns.Add("BilledQuantity", System.Type.GetType("System.String"));
                DT.Columns.Add("BilledPrice", System.Type.GetType("System.String"));

                foreach (IRfcStructure row in detail)
                {
                    DataRow _row = DT.NewRow();
                    _row["CompanyCode"] = row.GetValue("BUKRS").ToString();
                    _row["SalesOrganization"] = row.GetValue("VKORG").ToString();
                    _row["DocumentNumber"] = row.GetValue("XBLNR").ToString();
                    _row["DivisionCode"] = row.GetValue("SPART").ToString();
                    _row["DistributionChannel"] = row.GetValue("VTWEG").ToString();
                    _row["DocumentType"] = row.GetValue("AUART").ToString();
                    _row["ShipToCode"] = row.GetValue("KUNWE").ToString();
                    _row["Plant"] = row.GetValue("WERKS").ToString();
                    _row["ItemCode"] = row.GetValue("MATNR").ToString();
                    _row["SalesUnit"] = row.GetValue("VRKME").ToString();
                    _row["OrderQuantity"] = row.GetValue("WMENG").ToString();
                    _row["ConfirmedQuantity"] = row.GetValue("BMENG").ToString();
                    _row["BilledQuantity"] = row.GetValue("FKIMG").ToString();
                    _row["BilledPrice"] = row.GetValue("NETPR").ToString();

                    DT.Rows.Add(_row);
                    DT.AcceptChanges();
                }

                if (DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }

        public override void UpdateRoutes()
        {
            try
            {
                WriteMessage("\r\nUpdating Routes ..");
                OleDbConnection excelConn = new OleDbConnection();
                List<string> connectionStrings = new List<string>();
                connectionStrings.Add(string.Format(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=""Excel 12.0 Xml;HDR=YES"";", CoreGeneral.Common.StartupPath + "\\Routes.xlsx"));
                connectionStrings.Add(string.Format(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=""Excel 8.0;HDR=YES"";", CoreGeneral.Common.StartupPath + "\\Routes.xlsx"));
                connectionStrings.Add(string.Format(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=""Excel 8.0;HDR=Yes;IMEX=1"";", CoreGeneral.Common.StartupPath + "\\Routes.xlsx"));

                for (int i = 0; i < connectionStrings.Count; i++)
                {
                    try
                    {
                        excelConn.ConnectionString = connectionStrings[i];
                        excelConn.Open();
                        break;
                    }
                    catch (Exception ex2)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex2.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        if (i == connectionStrings.Count - 1)
                            throw new Exception("Failed in openning excel file");
                    }
                }

                DataTable dtData = excelConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                string sheet1 = "Sheet1$";// dtData.Rows[0]["TABLE_NAME"].ToString();
                dtData = excelConn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, null);
                string queryString = "SELECT ";
                foreach (string colName in Enum.GetNames(typeof(requiredColumns)))
                {
                    queryString += "[" + colName + "],";
                    DataRow[] dr = dtData.Select("TABLE_NAME = '" + sheet1 + "' AND COLUMN_NAME = '" + colName + "'");
                    if (dr.Length == 0)
                    {
                        throw new Exception("Column [" + colName + "] doesn't exist in sheet " + sheet1);
                    }
                }

                queryString = queryString.Substring(0, queryString.Length - 1) + " FROM [" + sheet1 + "] ";

                OleDbCommand excelCmd = new OleDbCommand(queryString, excelConn);
                excelCmd.CommandTimeout = 3600000;
                OleDbDataReader odr = excelCmd.ExecuteReader();

                InCubeQuery qry = new InCubeQuery(db_vms, "DELETE FROM Stg_Routes");
                if (qry.ExecuteNonQuery() != InCubeErrors.Success)
                    throw new Exception("Error in erasing old routes data");

                SqlBulkCopy bulk = new SqlBulkCopy(db_vms.GetConnection());
                bulk.DestinationTableName = "Stg_Routes";
                bulk.BulkCopyTimeout = 3600000;
                bulk.WriteToServer(odr);

                string countAll = GetFieldValue("Stg_Routes", "COUNT(*)", db_vms);
                execManager.UpdateActionTotalRows(TriggerID, int.Parse(countAll));
                WriteMessage("\r\nAll rows found are " + countAll);
                WriteMessage("\r\nProcessing ... This may take a few minutes ..");

                qry = new InCubeQuery(db_vms, "sp_UpdateRoutes");
                if (qry.ExecuteStoredProcedure() != InCubeErrors.Success)
                {
                    //WriteMessage(qry.GetCurrentException().Message);
                }
                string countSuccess = GetFieldValue("Stg_Routes", "COUNT(*)", "Result = 'Success'", db_vms);
                WriteMessage("\r\nAdded successfully rows: " + countSuccess);
                WriteMessage("\r\nYou can see more details by checking table Stg_Routes\r\n");
            }
            catch (Exception ex)
            {
                WriteMessage("\r\n" + ex.Message + "\r\n");
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        public override void SendReciepts()
        {
            try
            {
                WriteMessage("\r\n" + "Sending Receipts .. ");

                object CompanyCode = "";
                object DivisionCode = "";
                object SoldToCode = "";
                object ShipToCode = "";
                object PayerCode = "";
                object PaymentNumber = "";
                object InvoiceNumber = "";
                object InvoiceRefNumber = "";
                object PaymentDate = "";
                object PaidAmount = "";
                object InvoiceTotalAmt = "";
                object IsDownPayment = "";
                object PaymentType = "";
                object CustomerType = "";
                object CheqNumber = "";
                object BankCode = "";
                object CheqDate = "";
                object Notes = "";
                object RouteCode = "";
                object SalesmanCode = "";
                object RetRef = "";
                object sequnace = "";
                object AppliedPaymentID = "";
                RfcDestination dest = RfcDestinationManager.GetDestination(SendServerName);
                IRfcFunction func;
                IRfcTable impStruct2;

                func = dest.Repository.CreateFunction("ZHIB_PAYMENT_COLLECTION");

                string empFilter = "";
                if (Filters.EmployeeID != -1)
                    empFilter = "AND CP.EmployeeID = " + Filters.EmployeeID;

                string PaymentsQuery = string.Format(@"select *,RANK() OVER( order by   AppliedPaymentID ) sequnace from 
(SELECT CP.AppliedPaymentID, CP.CustomerPaymentID, O.OrganizationCode,D.DivisionCode, C.CustomerCode BillToCode, CO.CustomerCode ShipToCode, P.PayerCode,
CASE CP.PaymentTypeID WHEN 4 THEN ISNULL((SELECT MIN(CustomerPaymentID) FROM CustomerPayment WHERE RouteHistoryID = CP.RouteHistoryID AND VisitNo = CP.VisitNo
AND CustomerID = CP.CustomerID AND OutletID = CP.OutletID AND PaymentTypeID <> 4 ),CP.CustomerPaymentID)  ELSE CP.CustomerPaymentID END PaymentNumber
,CASE CP.PaymentTypeID WHEN 4 then T2.SourceTransactionID else  T.SourceTransactionID end InvoiceRefNo, CP.PaymentDate, CP.AppliedAmount PaidAmount,
T.NetTotal InvoiceAmount, CO.CustomerTypeID
, CASE CP.PaymentTypeID WHEN 4 THEN CP.SourceTransactionID ELSE T.TransactionID END InvoiceNo,
CASE(CP.PaymentTypeID) WHEN 1 THEN 0   
 WHEN 2 THEN 1   
 WHEN 3 THEN 2   
 WHEN 4 THEN 4   
 WHEN 5 THEN 3 ELSE 0 END 
PaymentType,
CP.VoucherNumber, CP.VoucherDate, B.Code BankCode, CP.Notes, E.EmployeeCode, TERR.TerritoryCode
, CASE CP.PaymentTypeID WHEN 4 THEN   T.SourceTransactionID ELSE '' END RetRef
FROM CustomerPayment CP
INNER JOIN [Transaction] T ON T.TransactionID = CP.TransactionID AND T.CustomerID = CP.CustomerID
AND T.OutletID = CP.OutletID
LEFT JOIN [Transaction] T2 ON T2.TransactionID = CP.SourceTransactionID AND T2.CustomerID = CP.CustomerID
AND T.OutletID = CP.OutletID
INNER JOIN Organization O ON O.OrganizationID = CP.OrganizationID
INNER JOIN Division D ON D.DivisionID = T.DivisionID
INNER JOIN Customer C ON C.CustomerID = CP.CustomerID
INNER JOIN CustomerOutlet CO ON CO.CustomerID = CP.CustomerID
INNER JOIN Payer P ON P.CustomerID = CP.CustomerID
LEFT JOIN Bank B ON B.BankID = CP.BankID
INNER JOIN Employee E ON E.EmployeeID = CP.EmployeeID
INNER JOIN EmployeeTerritory ET ON ET.EmployeeID = E.EmployeeID
INNER JOIN Territory TERR ON TERR.TerritoryID = ET.TerritoryID
WHERE CP.Synchronized = 0
{0}
AND CP.PaymentDate >= '{1}'
AND CP.PaymentDate < DATEADD(DD,1,'{2}'))T
", empFilter, Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.ToString("yyyy-MM-dd"));

                InCubeQuery dtlQry = new InCubeQuery(db_vms, PaymentsQuery);
                InCubeErrors err = dtlQry.Execute();

                DataTable DetailTable = dtlQry.GetDataTable();
                ClearProgress();
                SetProgressMax(DetailTable.Rows.Count);

                impStruct2 = func.GetTable("ITEMTAB");
                impStruct2.Insert();

                foreach (DataRow PaymentRow in DetailTable.Rows)
                {
                    try
                    {

                        AppliedPaymentID = PaymentRow["AppliedPaymentID"].ToString().Trim();
                        PaymentNumber = PaymentRow["PaymentNumber"].ToString().Trim();
                        if (PaymentNumber.ToString() == "") continue;
                        InvoiceNumber = PaymentRow["InvoiceNo"].ToString().Trim();
                        InvoiceRefNumber = PaymentRow["InvoiceRefNo"].ToString().Trim();
                        WriteMessage(string.Format("\r\nPayment [{0}] for invoice [{1}] .. ", PaymentNumber, InvoiceRefNumber));

                        CompanyCode = PaymentRow["OrganizationCode"].ToString().Trim();
                        DivisionCode = PaymentRow["DivisionCode"].ToString().Trim();
                        SoldToCode = PaymentRow["BillToCode"].ToString().Trim();
                        ShipToCode = PaymentRow["ShipToCode"].ToString().Trim();
                        PayerCode = PaymentRow["PayerCode"].ToString().Trim();
                        PaymentDate = PaymentRow["PaymentDate"].ToString().Trim();
                        PaidAmount = PaymentRow["PaidAmount"].ToString().Trim();
                        InvoiceTotalAmt = PaymentRow["InvoiceAmount"].ToString().Trim();
                        IsDownPayment = "0";
                        PaymentType = PaymentRow["PaymentType"].ToString().Trim();
                        CustomerType = "2";// PaymentRow["CustomerTypeID"].ToString().Trim();
                        CheqNumber = PaymentRow["VoucherNumber"].ToString().Trim();
                        BankCode = PaymentRow["BankCode"].ToString().Trim();
                        CheqDate = PaymentRow["VoucherDate"].ToString().Trim();
                        Notes = PaymentRow["Notes"].ToString().Trim();
                        RouteCode = PaymentRow["TerritoryCode"].ToString().Trim();
                        SalesmanCode = PaymentRow["EmployeeCode"].ToString().Trim();
                        RetRef = PaymentRow["RetRef"].ToString().Trim();
                        sequnace = PaymentRow["sequnace"].ToString().Trim();

                        impStruct2.SetValue("BUKRS", CompanyCode.ToString());
                        impStruct2.SetValue("SPART", DivisionCode.ToString());
                        impStruct2.SetValue("KUNAG", SoldToCode.ToString());
                        impStruct2.SetValue("KUNWE", ShipToCode.ToString());
                        impStruct2.SetValue("KUNRG", PayerCode.ToString());
                        impStruct2.SetValue("KIDNO", PaymentNumber.ToString());
                        impStruct2.SetValue("HHINV", InvoiceRefNumber.ToString());
                        impStruct2.SetValue("PAYDT", DateTime.Parse(PaymentDate.ToString()).ToString("yyyyMMddHHmmss"));
                        impStruct2.SetValue("NEBTR", decimal.Round(decimal.Parse(PaidAmount.ToString()), 2));
                        impStruct2.SetValue("NETWR", decimal.Round(decimal.Parse(InvoiceTotalAmt.ToString()), 2));
                        impStruct2.SetValue("XANET", IsDownPayment.ToString());
                        impStruct2.SetValue("PMTYP", PaymentType.ToString());
                        impStruct2.SetValue("CSTYP", CustomerType.ToString());
                        impStruct2.SetValue("CHKNM", CheqNumber.ToString());
                        impStruct2.SetValue("BANKA", BankCode.ToString());
                        if (CheqDate.ToString().Equals(string.Empty)) CheqDate = DateTime.Now.ToString();
                        impStruct2.SetValue("CHKDT", DateTime.Parse(CheqDate.ToString()).ToString("yyyyMMdd"));
                        impStruct2.SetValue("SGTXT", Notes.ToString());
                        impStruct2.SetValue("RCODE", RouteCode.ToString());
                        impStruct2.SetValue("PERNR", SalesmanCode.ToString());
                        impStruct2.SetValue("HHRET", RetRef);
                        impStruct2.SetValue("posnr", sequnace.ToString());

                        func.Invoke(dest);
                        InCubeQuery UpdateQuery = new InCubeQuery(db_vms, string.Format("UPDATE CustomerPayment SET Synchronized = 1 WHERE   AppliedPaymentID = '{0}'  ", AppliedPaymentID));
                        err = UpdateQuery.Execute();
                        WriteMessage("Success");
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        WriteMessage("Failed");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("Failed");
            }
        }
        public override void SendOrders()
        {
            try
            {
                WriteMessage("\r\n" + "Sending Orders .. ");

                object CompanyCode = "";
                object DivisionCode = "";
                object distCh = "";
                object EmployeeCode = "";
                object DocumentType = "ZPR";
                object OrderDate = "";
                object DeliveryDate = "";
                object OrderID = "";
                object OrgOrderID = "";
                object HeaderDiscount = "";
                object LPO_Number = "";
                object ShipToCode = "";
                object SoldToCode = "";
                object PayerCode = "";
                object ItemCode = "";
                object UOM = "";
                object Quantity = "";
                object Plant = "";
                object LineNumber = "";
                object CustomerID = "";
                object OutletID = "";
                object PackID = "";

                RfcDestination dest = RfcDestinationManager.GetDestination(SendServerName);
                IRfcFunction func;
                IRfcTable impStruct2;

                func = dest.Repository.CreateFunction("ZHIB_PROCESS_PRE_SALES");

                string empFilter = "";
                if (Filters.EmployeeID != -1)
                    empFilter = "AND SO.EmployeeID = " + Filters.EmployeeID;

                string OrdersQuery = string.Format(@"SELECT O.OrganizationCode, D.DivisionCode, DC.ChannelCode DistributionChannel, E.EmployeeCode,SO.OrderID,
SO.OrderDate, SO.DesiredDeliveryDate DeliveryDate, SO.Discount HeaderDiscount, SO.LPO LPO_Number,
C.CustomerCode SoldToCode, CO.CustomerCode ShipToCode, Payer.PayerCode, I.ItemCode, PTL.Description UOM,
SOD.Quantity, 10 * RANK() OVER(PARTITION BY SO.OrderID, D.DivisionCode ORDER BY I.ItemCode, PTL.Description) LineNumber,
SO.CustomerID, SO.OutletID, SOD.PackID
FROM SalesOrderdetail SOD
INNER JOIN SalesOrder SO ON SO.OrderID = SOD.OrderID AND SO.CustomerID = SOD.CustomerID AND SO.OutletID = SOD.OutletID
INNER JOIN Organization O On O.OrganizationID = SO.OrganizationID
INNER JOIN Pack P ON P.PackID = SOD.PackID
INNER JOIN Item I ON I.ItemID = P.ItemID
INNER JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1
INNER JOIN ItemCategory IC ON IC.ItemCategoryID = I.ItemCategoryID
INNER JOIN Division D ON D.DivisionID = IC.DivisionID
INNER JOIN DistributionChannel DC ON DC.CustomerID = SO.CustomerID AND DC.OutletID = SO.OutletID
INNER JOIN Employee E ON E.EmployeeID = SO.EmployeeID
INNER JOIN Customer C ON C.CustomerID = SO.CustomerID
INNER JOIN CustomerOutlet CO ON CO.CustomerID = SO.CustomerID AND CO.OutletID = SO.OutletID
INNER JOIN Payer ON Payer.CustomerID = SO.CustomerID
WHERE ISNULL(AllItemDiscount,0) = 0 AND SOD.SalesTransactionTypeID = 1
{0}
AND SO.OrderDate > '{1}'
AND SO.OrderDate <= DATEADD(DD,1,'{2}')
ORDER BY SO.OrderID, I.ItemCode, PTL.Description", empFilter, Filters.FromDate.ToString("yyyy/MM/dd"), Filters.ToDate.ToString("yyyy/MM/dd"));
                InCubeQuery dtlQry = new InCubeQuery(db_vms, OrdersQuery);
                InCubeErrors err = dtlQry.Execute();

                DataTable DetailTable = dtlQry.GetDataTable();
                ClearProgress();
                SetProgressMax(DetailTable.Rows.Count);

                impStruct2 = func.GetTable("ITEMTAB");
                impStruct2.Insert();

                foreach (DataRow salesTxRow in DetailTable.Rows)
                {
                    try
                    {
                        OrderID = salesTxRow["OrderID"].ToString().Trim();
                        OrgOrderID = OrderID;
                        ItemCode = salesTxRow["ItemCode"].ToString().Trim();
                        WriteMessage(string.Format("\r\nOrder [{0}], Item [{1}] .. ", OrderID, ItemCode));

                        CompanyCode = salesTxRow["OrganizationCode"].ToString().Trim();
                        DivisionCode = salesTxRow["DivisionCode"].ToString().Trim();
                        distCh = salesTxRow["DistributionChannel"].ToString().Trim();
                        EmployeeCode = salesTxRow["EmployeeCode"].ToString().Trim();
                        OrderDate = salesTxRow["OrderDate"].ToString().Trim();
                        DeliveryDate = salesTxRow["DeliveryDate"].ToString().Trim();
                        HeaderDiscount = salesTxRow["HeaderDiscount"].ToString().Trim();
                        LPO_Number = salesTxRow["LPO_Number"].ToString().Trim();
                        ShipToCode = salesTxRow["ShipToCode"].ToString().Trim();
                        SoldToCode = salesTxRow["SoldToCode"].ToString().Trim();
                        PayerCode = salesTxRow["PayerCode"].ToString().Trim();
                        UOM = salesTxRow["UOM"].ToString().Trim();
                        Quantity = salesTxRow["Quantity"].ToString().Trim();
                        LineNumber = salesTxRow["LineNumber"].ToString().Trim();
                        CustomerID = salesTxRow["CustomerID"].ToString().Trim();
                        OutletID = salesTxRow["OutletID"].ToString().Trim();
                        PackID = salesTxRow["PackID"].ToString().Trim();
                        switch (DivisionCode.ToString())
                        {
                            case "15":
                                OrderID = "F" + OrderID;
                                Plant = "1102";
                                break;
                            case "20":
                                OrderID = "D" + OrderID;
                                Plant = "1107";
                                break;
                        }

                        impStruct2.SetValue("BUKRS", CompanyCode.ToString());
                        impStruct2.SetValue("VKORG", CompanyCode.ToString());
                        impStruct2.SetValue("SPART", DivisionCode.ToString());
                        impStruct2.SetValue("VTWEG", distCh.ToString());
                        impStruct2.SetValue("PERNR", EmployeeCode.ToString());
                        impStruct2.SetValue("AUART", DocumentType.ToString());
                        impStruct2.SetValue("TRDAT", DateTime.Parse(OrderDate.ToString()).ToString("yyyyMMddHHmmss"));
                        impStruct2.SetValue("LFDAT", DateTime.Parse(DeliveryDate.ToString()).ToString("yyyyMMdd"));
                        impStruct2.SetValue("XBLNR", OrderID.ToString());
                        impStruct2.SetValue("HDRDC", HeaderDiscount.ToString());
                        impStruct2.SetValue("BSTNK", LPO_Number.ToString());
                        impStruct2.SetValue("KUNWE", ShipToCode.ToString());
                        impStruct2.SetValue("KUNAG", SoldToCode.ToString());
                        impStruct2.SetValue("KUNRG", PayerCode.ToString());
                        impStruct2.SetValue("MATNR", ItemCode.ToString().PadLeft(18, '0'));
                        impStruct2.SetValue("VRKME", UOM.ToString());
                        impStruct2.SetValue("FKIMG", Quantity.ToString());
                        impStruct2.SetValue("WERKS", Plant);
                        impStruct2.SetValue("POSNR", LineNumber.ToString());

                        func.Invoke(dest);
                        InCubeQuery UpdateQuery = new InCubeQuery(db_vms, string.Format("UPDATE SalesOrderDetail SET AllItemDiscount = 1 WHERE OrderID = '{0}' AND CustomerID = {1} AND OutletID = {2} AND PackID = {3}", OrgOrderID, CustomerID, OutletID, PackID));
                        err = UpdateQuery.Execute();
                        WriteMessage("Success");
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        WriteMessage("Failed");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("Failed");
            }
        }
    }
}