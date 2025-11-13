using SAP.Middleware.Connector;
using System;
using System.Collections.Generic;
using System.Data;
using InCubeIntegration_DAL;
using InCubeLibrary;
using System.Data.SqlClient;
using System.ComponentModel;
using System.Text;

namespace InCubeIntegration_BL
{
    public class IntegrationDelMonteJordan : IntegrationBase // Live branch
    {
        BackgroundWorker bgwCheckProgress;
        InCubeDatabase db_res;
        int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
        SqlCommand cmd;
        InCubeQuery incubeQuery = null;
        string StagingTable = "";
        Dictionary<int, RfcConfigParameters> RfcParams = new Dictionary<int, RfcConfigParameters>();
        string _WarehouseID = "-1";
        public IntegrationDelMonteJordan(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            if (CoreGeneral.Common.CurrentSession.loginType != LoginType.WindowsService)
            {
                db_res = new InCubeDatabase();
                db_res.Open("InCube", "IntegrationDelMonteJordan");
            }

      

            bgwCheckProgress = new BackgroundWorker();
            bgwCheckProgress.DoWork += new DoWorkEventHandler(bgw_CheckProgress);
            bgwCheckProgress.WorkerSupportsCancellation = true;

            RfcConfigParameters RfcPar = new RfcConfigParameters();
            foreach (int org in CoreGeneral.Common.OrganizationConfigurations.Keys)
            {
                RfcParams.Add(org, new RfcConfigParameters());
                RfcParams[org][RfcConfigParameters.Name] = CoreGeneral.Common.OrganizationConfigurations[org].Name;
                RfcParams[org][RfcConfigParameters.User] = CoreGeneral.Common.OrganizationConfigurations[org].User;
                RfcParams[org][RfcConfigParameters.Password] = CoreGeneral.Common.OrganizationConfigurations[org].Password;
                RfcParams[org][RfcConfigParameters.Client] = CoreGeneral.Common.OrganizationConfigurations[org].Client;
                RfcParams[org][RfcConfigParameters.Language] = CoreGeneral.Common.OrganizationConfigurations[org].Language;
                RfcParams[org][RfcConfigParameters.AppServerHost] = CoreGeneral.Common.OrganizationConfigurations[org].AppServerHost;
                RfcParams[org][RfcConfigParameters.SystemNumber] = CoreGeneral.Common.OrganizationConfigurations[org].SystemNumber;
                RfcParams[org][RfcConfigParameters.SystemID] = CoreGeneral.Common.OrganizationConfigurations[org].SystemID;
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

        public override void UpdateStock()
        {
            _WarehouseID = Filters.WarehouseID.ToString();
            GetMasterData(IntegrationField.Stock_U);
        }
        public override void UpdateCustomer()
        {
            GetMasterData(IntegrationField.Customer_U);
        }

        public override void UpdateSalesPerson()
        {
            GetMasterData(IntegrationField.Salesperson_U);
        }

        public override void UpdatePrice()
        {
            GetMasterData(IntegrationField.Price_U);
        }

        private void GetMasterData(IntegrationField field)
        {
            Result res = Result.UnKnown;
            try
            {
                string MasterName = field.ToString().Substring(0, field.ToString().Length - 2);
                string ProcName = "";
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
                    case IntegrationField.Salesperson_U:
                        res = GetSalespersonTable(ref dtMasterData);
                        StagingTable = "Stg_Salespersons";
                        ProcName = "sp_UpdateSalespersons";
                        break;
                    case IntegrationField.Stock_U:
                        res = GetStockTable(ref dtMasterData);
                        StagingTable = "Stg_Stock";
                        ProcName = "sp_UpdateStock";
                        break;
                }

                if (res != Result.Success)
                {
                    if (res == Result.Failure)
                        WriteMessage(" Error in reading from SAP !!");
                    else
                        WriteMessage(" No data found !!");

                    return;
                }
                WriteMessage(" Rows retrieved: " + dtMasterData.Rows.Count);
                execManager.UpdateActionTotalRows(TriggerID, dtMasterData.Rows.Count);

                WriteMessage("\r\nSaving data to staging table ... ");

                res = SaveTable(dtMasterData, StagingTable);
                if (res != Result.Success)
                {
                    WriteMessage(" Error in saving to staging table !!");
                    return;
                }
                WriteMessage(" Success ..");

                WriteMessage("\r\nLooping through " + MasterName + " ...");

                if (CoreGeneral.Common.CurrentSession.loginType != LoginType.WindowsService)
                    bgwCheckProgress.RunWorkerAsync();

                cmd = new SqlCommand(ProcName, db_vms.GetConnection());
                //if(field==Fields.Stock_U)
                //{
                //    cmd.Parameters.Add(new SqlParameter("@WarehouseID", _WarehouseID));
                //}
                cmd.CommandTimeout = 3600000;
                cmd.ExecuteNonQuery();

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
            try
            {
                DT = new DataTable();
                DT.Columns.Add("OrgCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemName", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemName_A", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemCategoryCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemCategoryDescription", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemCategoryDescription_A", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemGroupCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemGroupName", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemGroupName_A", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemUOM", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionCode", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionName", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionName_A", System.Type.GetType("System.String"));
                DT.Columns.Add("InActive", System.Type.GetType("System.String"));
                DT.Columns.Add("Quantity", System.Type.GetType("System.String"));
                DT.Columns.Add("Numerator", System.Type.GetType("System.String"));
                DT.Columns.Add("Denominator", System.Type.GetType("System.String"));
                DT.Columns.Add("Taxable", System.Type.GetType("System.String"));
                DT.Columns.Add("TotalShelfLife", System.Type.GetType("System.String"));

                foreach (KeyValuePair<int, RfcConfigParameters> pair in RfcParams)
                    try
                    {
                        if (pair.Key != -1)
                        {
                            RfcConfigParameters RfcParam = pair.Value;
                            RfcDestination prd = RfcDestinationManager.GetDestination(RfcParam);
                            RfcRepository repo = prd.Repository;
                            IRfcFunction companyBapi = repo.CreateFunction("BAPI_VS_CUS_MASTER");

                            IRfcTable tblImport = companyBapi.GetTable("PE_MAT_MASTER");
                            companyBapi.SetValue("PE_MAT_MASTER", tblImport);
                            companyBapi.Invoke(prd);
                            IRfcTable detail = companyBapi.GetTable("PE_MAT_MASTER");

                            foreach (IRfcStructure row in detail)
                            {
                                DataRow _row = DT.NewRow();

                                _row["OrgCode"] = row.GetValue("VKORG").ToString();
                                _row["ItemCode"] = row.GetValue("MATNR").ToString();
                                _row["ItemName"] = row.GetValue("MAKTX").ToString();
                                _row["ItemName_A"] = row.GetValue("MAKTX_AR").ToString();
                                _row["ItemCategoryCode"] = row.GetValue("MATKL").ToString();
                                _row["ItemCategoryDescription"] = row.GetValue("WGBEZ").ToString();
                                _row["ItemCategoryDescription_A"] = row.GetValue("WGBEZ").ToString();
                                _row["ItemGroupCode"] = row.GetValue("MATKL").ToString();
                                _row["ItemGroupName"] = row.GetValue("WGBEZ").ToString();
                                _row["ItemGroupName_A"] = row.GetValue("WGBEZ").ToString();
                                _row["Quantity"] = row.GetValue("CONV_FACTOR").ToString();
                                _row["Numerator"] = row.GetValue("CONV_NUMER").ToString();
                                _row["Denominator"] = row.GetValue("CONV_DENOM").ToString();
                                _row["ItemUOM"] = row.GetValue("MEINS").ToString();
                                _row["DivisionCode"] = row.GetValue("SPART").ToString();
                                _row["DivisionName"] = row.GetValue("SPART").ToString();
                                _row["DivisionName_A"] = row.GetValue("SPART").ToString();
                                _row["Taxable"] = row.GetValue("TAXM1").ToString();
                                _row["InActive"] = row.GetValue("LVORM").ToString();
                                if (_row["InActive"].ToString().Trim().ToLower() == "x")
                                    _row["InActive"] = "1";
                                else
                                    _row["InActive"] = "0";
                                _row["TotalShelfLife"] = row.GetValue("MHDHB").ToString();
                               DT.Rows.Add(_row);
                                DT.AcceptChanges();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                    }

                if (DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }

        private Result GetCustomerTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            try
            {
                DT = new DataTable();
                DT.Columns.Add("OrgCode", System.Type.GetType("System.String"));
                DT.Columns.Add("DivisionCode", System.Type.GetType("System.String"));
                DT.Columns.Add("BillToCode", System.Type.GetType("System.String"));
                DT.Columns.Add("BillToName", System.Type.GetType("System.String"));
                DT.Columns.Add("BillToName_A", System.Type.GetType("System.String"));
                DT.Columns.Add("ShipToCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ShipToName", System.Type.GetType("System.String"));
                DT.Columns.Add("ShipToName_A", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerType", System.Type.GetType("System.String"));
                DT.Columns.Add("NoOfBills", System.Type.GetType("System.String"));
                DT.Columns.Add("PaymentTermDays", System.Type.GetType("System.String"));
                DT.Columns.Add("CreditLimit", System.Type.GetType("System.String"));
                DT.Columns.Add("CurrencyKey", System.Type.GetType("System.String"));
                DT.Columns.Add("BlockStatus", System.Type.GetType("System.String"));
                DT.Columns.Add("Taxable", System.Type.GetType("System.String"));
                DT.Columns.Add("TRNO", System.Type.GetType("System.String"));
                DT.Columns.Add("Address", System.Type.GetType("System.String"));
                DT.Columns.Add("Address_A", System.Type.GetType("System.String"));
                DT.Columns.Add("Telephone", System.Type.GetType("System.String"));
                DT.Columns.Add("GPS_Long", System.Type.GetType("System.String"));
                DT.Columns.Add("GPS_Lat", System.Type.GetType("System.String"));
                DT.Columns.Add("PriceGroup", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerGroup", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerGroupName", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerGroupName_A", System.Type.GetType("System.String"));
                DT.Columns.Add("ChannelCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ChannelName", System.Type.GetType("System.String"));
                DT.Columns.Add("ChannelName_A", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesmanCode", System.Type.GetType("System.String"));

                foreach (KeyValuePair<int, RfcConfigParameters> pair in RfcParams)
                    try
                    {
                        if (pair.Key != -1)
                        {
                            RfcConfigParameters RfcParam = pair.Value;
                            RfcDestination prd = RfcDestinationManager.GetDestination(RfcParam);
                            RfcRepository repo = prd.Repository;
                            IRfcFunction companyBapi = repo.CreateFunction("BAPI_VS_CUS_MASTER");

                            IRfcTable tblImport = companyBapi.GetTable("PE_CUS_MASTER");
                            companyBapi.SetValue("PE_CUS_MASTER", tblImport);
                            companyBapi.Invoke(prd);
                            IRfcTable detail = companyBapi.GetTable("PE_CUS_MASTER");

                            foreach (IRfcStructure row in detail)
                            {
                                DataRow _row = DT.NewRow();
                                _row["OrgCode"] = row.GetValue("VKORG").ToString().Trim();
                                _row["DivisionCode"] = row.GetValue("SPART").ToString();
                                _row["BillToCode"] = row.GetValue("KUNAG").ToString().Trim();
                                _row["BillToName"] = row.GetValue("KUNAG_NAME1").ToString();
                                _row["BillToName_A"] = row.GetValue("KUNAG_NAME1_AR").ToString();
                                _row["ShipToCode"] = row.GetValue("KUNWE").ToString().Trim();
                                _row["ShipToName"] = row.GetValue("KUNWE_NAME1").ToString();
                                _row["ShipToName_A"] = row.GetValue("KUNWE_NAME1_AR").ToString();
                                _row["CustomerType"] = row.GetValue("CUS_TYPE").ToString();
                                _row["NoOfBills"] = row.GetValue("NO_BILLS").ToString();
                                _row["PaymentTermDays"] = row.GetValue("PMT_TERM_DAYS").ToString();
                                _row["CreditLimit"] = decimal.Parse(row.GetValue("CREDIT_LIMIT").ToString()) / 10; 
                                _row["CurrencyKey"] = row.GetValue("WAERS").ToString();
                                _row["BlockStatus"] = row.GetValue("LOEVM").ToString();
                                if ( _row["BlockStatus"].ToString().Trim().ToLower() == "x")
                                    _row["BlockStatus"] = "1";
                                else
                                    _row["BlockStatus"] = "0";
                                _row["Taxable"] = row.GetValue("TAXKD").ToString();
                                _row["TRNO"] = row.GetValue("STCEG").ToString();
                                _row["Address"] = row.GetValue("STREET").ToString();
                                _row["Address_A"] = row.GetValue("STREET_AR").ToString();
                                _row["Telephone"] = row.GetValue("TEL_NUMBER").ToString();
                                _row["GPS_Long"] = row.GetValue("GPS_LONG").ToString();
                                _row["GPS_Lat"] = row.GetValue("GPS_LAT").ToString();
                                _row["PriceGroup"] = row.GetValue("PLTYP").ToString();
                                _row["CustomerGroup"] = row.GetValue("KDGRP").ToString();
                                _row["CustomerGroupName"] = row.GetValue("KTEXT").ToString();
                                _row["CustomerGroupName_A"] = row.GetValue("KTEXT").ToString();
                                _row["ChannelCode"] = row.GetValue("VTWEG").ToString();
                                _row["ChannelName"] = row.GetValue("VTEXT").ToString();
                                _row["ChannelName_A"] = row.GetValue("VTEXT").ToString();
                                _row["SalesmanCode"] = row.GetValue("PERNR").ToString();
                                DT.Rows.Add(_row);
                                DT.AcceptChanges();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                    }

                if (DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }

        private Result GetSalespersonTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            try
            {
                DT = new DataTable();
                DT.Columns.Add("SalesmanCode", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesmanName", System.Type.GetType("System.String"));
                DT.Columns.Add("SalesmanName_A", System.Type.GetType("System.String"));
                DT.Columns.Add("OrgCode", System.Type.GetType("System.String"));
                DT.Columns.Add("Division", System.Type.GetType("System.String"));

                foreach (KeyValuePair<int, RfcConfigParameters> pair in RfcParams)
                    try
                    {
                        if (pair.Key != -1)
                        {
                            RfcConfigParameters RfcParam = pair.Value;
                            RfcDestination prd = RfcDestinationManager.GetDestination(RfcParam);
                            RfcRepository repo = prd.Repository;
                            IRfcFunction companyBapi = repo.CreateFunction("BAPI_VS_CUS_MASTER");

                            IRfcTable tblImport = companyBapi.GetTable("PE_EMP_MASTER");
                            companyBapi.SetValue("PE_EMP_MASTER", tblImport);
                            companyBapi.Invoke(prd);
                            IRfcTable detail = companyBapi.GetTable("PE_EMP_MASTER");

                            foreach (IRfcStructure row in detail)
                            {
                                DataRow _row = DT.NewRow();
                                _row["SalesmanCode"] = row.GetValue("PERNR").ToString();
                                _row["SalesmanName"] = row.GetValue("CNAME").ToString();
                                _row["SalesmanName_A"] = row.GetValue("CNAME").ToString();
                                _row["OrgCode"] = row.GetValue("VKORG").ToString();
                                _row["Division"] = row.GetValue("SPART").ToString();
                                DT.Rows.Add(_row);
                                DT.AcceptChanges();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                    }

                if (DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }


        private Result GetStockTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            try
            {
                DT = new DataTable();
                DT.Columns.Add("WarehouseCode", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemCode", System.Type.GetType("System.String"));
                DT.Columns.Add("UOM", System.Type.GetType("System.String"));
                DT.Columns.Add("Lot", System.Type.GetType("System.String"));
                DT.Columns.Add("Qty", System.Type.GetType("System.String"));
                DT.Columns.Add("ExpiryDate", System.Type.GetType("System.String"));
                DT.Columns.Add("PackID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("WarehouseID", System.Type.GetType("System.Int16"));

               
                //    DataRow _row = DT.NewRow();
                //    _row["WarehouseCode"] = "JO10";
                //    _row["ItemCode"] = "000000000003100614";//000000000003100616
                //_row["UOM"] = "KG";
                //_row["Qty"] = "10";
                //_row["Lot"] = "180320";
                //    _row["ExpiryDate"] = "2018-03-20";
                //    DT.Rows.Add(_row);
                //    DT.AcceptChanges();
                //DataRow _row2 = DT.NewRow();
                //_row2["WarehouseCode"] = "JO10";
                //_row2["ItemCode"] = "000000000003100616";//
                //_row["Qty"] = "20";
                //_row2["UOM"] = "KG";
                //_row2["Lot"] = "180320";
                //_row2["ExpiryDate"] = "2018-03-20";
                //DT.Rows.Add(_row2);
                //DT.AcceptChanges();

                foreach (KeyValuePair<int, RfcConfigParameters> pair in RfcParams)
                    try
                    {
                        if (pair.Key != -1)
                        {
                            RfcConfigParameters RfcParam = pair.Value;
                            RfcDestination prd = RfcDestinationManager.GetDestination(RfcParam);
                            RfcRepository repo = prd.Repository;
                            IRfcFunction companyBapi = repo.CreateFunction("BAPI_VS_STOCK");

                            IRfcTable tblImport = companyBapi.GetTable("PE_STOCK");
                            companyBapi.SetValue("PE_STOCK", tblImport);
                            companyBapi.Invoke(prd);
                            IRfcTable detail = companyBapi.GetTable("PE_STOCK");
                          foreach (IRfcStructure row in detail)
                            {
                                DataRow _row = DT.NewRow();
                                _row["WarehouseCode"] = row.GetValue("WERKS").ToString()+ row.GetValue("LGORT").ToString();
                                _row["ItemCode"] = row.GetValue("MATNR").ToString();
                                _row["UOM"] = row.GetValue("MEINS").ToString();
                                _row["Lot"] = row.GetValue("CHARG").ToString();
                                _row["Qty"] = row.GetValue("LABST").ToString();
                                _row["ExpiryDate"] = row.GetValue("VFDAT").ToString();
                                DT.Rows.Add(_row);
                                DT.AcceptChanges();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                    }

                if (DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }

        private Result GetPricesTable(ref DataTable DT)
        {
            Result res = Result.Failure;
            try
            {
                DT = new DataTable();
                DT.Columns.Add("OrgCode", System.Type.GetType("System.String"));
                DT.Columns.Add("PriceListCode", System.Type.GetType("System.String"));
                DT.Columns.Add("PriceListName", System.Type.GetType("System.String"));
                DT.Columns.Add("IsDefault", System.Type.GetType("System.String"));
                DT.Columns.Add("ValidFrom", System.Type.GetType("System.String"));
                DT.Columns.Add("ValidTo", System.Type.GetType("System.String"));
                DT.Columns.Add("ItemCode", System.Type.GetType("System.String"));
                DT.Columns.Add("UOM", System.Type.GetType("System.String"));
                DT.Columns.Add("Price", System.Type.GetType("System.String"));
                DT.Columns.Add("Currency", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerCode", System.Type.GetType("System.String"));
                DT.Columns.Add("OutletCode", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerGroupCode", System.Type.GetType("System.String"));
                DT.Columns.Add("PriceGroupCode", System.Type.GetType("System.String"));
                DT.Columns.Add("TypeCode", System.Type.GetType("System.String"));
                DT.Columns.Add("CustomerID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("OutletID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("GroupID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("PackID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("ItemID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("CurrencyID", System.Type.GetType("System.Int16"));
                DT.Columns.Add("OrganizationID", System.Type.GetType("System.Int16"));

                foreach (KeyValuePair<int, RfcConfigParameters> pair in RfcParams)
                    try
                    {
                        if (pair.Key != -1)
                        {
                            RfcConfigParameters RfcParam = pair.Value;
                            RfcDestination prd = RfcDestinationManager.GetDestination(RfcParam);
                            RfcRepository repo = prd.Repository;
                            IRfcFunction companyBapi = repo.CreateFunction("BAPI_VS_PRC_MASTER");

                            IRfcTable tblImport = companyBapi.GetTable("PE_PRC_MASTER");
                            companyBapi.SetValue("PE_PRC_MASTER", tblImport);
                            companyBapi.Invoke(prd);
                            IRfcTable detail = companyBapi.GetTable("PE_PRC_MASTER");

                            foreach (IRfcStructure row in detail)
                            {
                                DataRow _row = DT.NewRow();
                                _row["OrgCode"] = row.GetValue("VKORG").ToString();
                                _row["PriceListCode"] = row.GetValue("PRICE_LIST_CODE").ToString();
                                _row["IsDefault"] = row.GetValue("IS_DEFAULT").ToString();
                                _row["ValidFrom"] = row.GetValue("DATAB").ToString();
                                _row["ValidTo"] = row.GetValue("DATBI").ToString();
                                _row["ItemCode"] = row.GetValue("MATNR").ToString().Trim();
                                _row["UOM"] = row.GetValue("MEINS").ToString();
                                _row["TypeCode"] = row.GetValue("KSCHL").ToString().Trim();
                                _row["Price"] = decimal.Parse( row.GetValue("KBETR").ToString())/10;
                                _row["Currency"] = row.GetValue("KONWA").ToString();
                                if (!row.GetValue("KUNAG").ToString().Trim().Equals(string.Empty))
                                {
                                    _row["CustomerCode"] = row.GetValue("KUNAG").ToString().Trim();
                                }

                                if (!row.GetValue("KUNWE").ToString().Trim().Equals(string.Empty))
                                {
                                    _row["OutletCode"] = row.GetValue("KUNWE").ToString().Trim();
                                }
                                if (!row.GetValue("KDGRP").ToString().Trim().Equals(string.Empty))
                                {
                                    _row["CustomerGroupCode"] = row.GetValue("KDGRP").ToString();
                                }
                                if (!row.GetValue("PLTYP").ToString().Trim().Equals(string.Empty))
                                {
                                    _row["PriceGroupCode"] = row.GetValue("PLTYP").ToString();
                                }
                                if (_row["PriceGroupCode"].ToString() == "" && _row["CustomerGroupCode"].ToString() == "" && _row["PriceGroupCode"].ToString().Trim().ToLower() == "10")
                                    _row["IsDefault"] = "1";

                                if (_row["IsDefault"].ToString() == "1")
                                    _row["PriceListName"] = "Default Price";
                                else if (_row["CustomerGroupCode"].ToString() != "")
                                {
                                    _row["PriceListName"] = "C-" + _row["PriceListCode"] + "\\" + _row["CustomerGroupCode"].ToString();
                                    _row["PriceListCode"] = "C-" + _row["PriceListCode"] + "\\" + _row["CustomerGroupCode"].ToString();
                                    _row["CustomerGroupCode"] = "C-" + _row["CustomerGroupCode"];
                                }

                                else if (_row["PriceGroupCode"].ToString() != "")
                                {
                                    _row["PriceListName"] = "P-" + _row["PriceListCode"] + "\\" + _row["PriceGroupCode"].ToString();
                                    _row["PriceListCode"] = "P-" + _row["PriceListCode"] + "\\" + _row["PriceGroupCode"].ToString();
                                    _row["PriceGroupCode"] = "P-" + _row["PriceGroupCode"];
                                }
                                DT.Rows.Add(_row);
                                DT.AcceptChanges();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                    }

                if (DT.Rows.Count > 0)
                    res = Result.Success;
                else
                    res = Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public override void SendInvoices()
        {
            try
            {
                string salespersonFilter = "", SalesDocType = "", SalesOrg = "", VehicleCode = "", SalesOffice = "", SalesGroup = "";
                string ShipTo = "", TransactionID = "", WarehouseCode = "", SAP_SO_NUM = "";
                DateTime TransactionDate;
                int OrgID = 0, processID = 0;
                StringBuilder result = new StringBuilder();
                Dictionary<int, RfcDestination> destinations = new Dictionary<int, RfcDestination>();
                Dictionary<int, RfcRepository> repositories = new Dictionary<int, RfcRepository>();
                Result res = Result.UnKnown;

                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter = "AND T.EmployeeID = " + Filters.EmployeeID;
                }
                string invoicesHeader = string.Format(@"SELECT t.DivisionID, T.TransactionID, SP.CredentialsOrgID OrgID, T.TransactionDate
, CASE T.TransactionTypeID WHEN 1 THEN 'Z' WHEN 2 THEN 'Y' WHEN 3 THEN 'Z' WHEN 4 THEN 'Y' END + SUBSTRING(E.NationalIdNumber,2,3) SalesDocType
, O.OrganizationCode SalesOrg
--,  CASE WHEN  T.TransactionTypeID=1 THEN V.WarehouseCode WHEN ( T.TransactionTypeID=2 or T.TransactionTypeID=4) and t.DivisionID=3 then 'RT01'  WHEN ( T.TransactionTypeID=2 or T.TransactionTypeID=4) and t.DivisionID=2 then 'RT02' when T.TransactionTypeID=3 THEN V.WarehouseCode END VehicleCode
,  CASE WHEN  T.TransactionTypeID=1 or T.TransactionTypeID=3 THEN 'FG99' WHEN ( T.TransactionTypeID=2 or T.TransactionTypeID=4) then 'RT99'  END VehicleCode
, W.WarehouseCode,w.Barcode SalesOffice, SP.SalesGroup, CO.CustomerCode ShipTo
FROM [Transaction] T
INNER JOIN SalesSendParams SP ON SP.OrganizationID = T.OrganizationID
INNER JOIN CustomerOutlet CO ON CO.CustomerID = T.CustomerID AND CO.OutletID = T.OutletID
INNER JOIN Customer  C  ON C .CustomerID = T.CustomerID --AND CO.OutletID = T.OutletID
INNER JOIN Organization O ON O.OrganizationID = T.OrganizationID
INNER JOIN Employee E ON E.EmployeeID = T.EmployeeID
LEFT JOIN EmployeeVehicle EV ON EV.EmployeeID = T.EmployeeID
LEFT JOIN Warehouse V ON V.WarehouseID = EV.VehicleID and t.RouteHistoryID<>-1
LEFT JOIN VehicleLoadingWH VW ON VW.VehicleID = EV.VehicleID and IsDefault=1
INNER JOIN Warehouse W ON W.WarehouseID = VW.WarehouseID or (T.RouteHistoryID=-1 and W.WarehouseID = t.WarehouseID)
WHERE T.Synchronized = 0 AND dbo.IsRouteHistoryUploaded(T.RouteHistoryID) = 0 AND T.Voided = 0 AND T.TransactionDate >= '{0}' AND T.TransactionDate < '{1}' 
{2}
AND T.TransactionTypeID < 5 /*and T.TransactionID = 'INV-VC11-0071-000040'*/
ORDER BY SP.CredentialsOrgID", Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
                incubeQuery = new InCubeQuery(db_vms, invoicesHeader);

                //Logger.WriteLog(FromDate.ToString(), ToDate.ToString(), invoicesHeader, LoggingType.Information, LoggingFiles.errorInv);

                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    throw (new Exception("Invoices header query failed !!"));
                }

                DataTable dtInvoices = incubeQuery.GetDataTable();
                if (dtInvoices.Rows.Count == 0)
                    WriteMessage("There is no invoices to send ..");
                else
                    SetProgressMax(dtInvoices.Rows.Count);

                for (int i = 0; i < dtInvoices.Rows.Count; i++)
                {
                    try
                    {
                        res = Result.UnKnown;
                        processID = 0;
                        result = new StringBuilder();
                        SAP_SO_NUM = "";
                        TransactionID = dtInvoices.Rows[i]["TransactionID"].ToString();
                        ReportProgress("Sending invoice: " + TransactionID);
                        WriteMessage("\r\n" + TransactionID + ": ");
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, TransactionID);
                       
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Sales_S, new List<string>(filters.Values), processID,60);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            res = Result.Duplicate;
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [Transaction] SET Synchronized = 1 WHERE TransactionID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                            throw (new Exception("Transaction already sent  check table  Int_ExecutionDetails!!"));
 
                        }
                        else if (lastRes == Result.Started)
                        {
                            res = Result.Blocked;
                            throw (new Exception("Sending is in progress with another process !!"));
                        }
                        OrgID = Convert.ToInt16(dtInvoices.Rows[i]["OrgID"]);

                        if (!destinations.ContainsKey(OrgID))
                        {
                            RfcConfigParameters RfcParam;
                            RfcDestination dest;
                            RfcRepository repo;
                            RfcParam = RfcParams[OrgID];
                            dest = RfcDestinationManager.GetDestination(RfcParam);
                            repo = dest.Repository;
                            destinations.Add(OrgID, dest);
                            repositories.Add(OrgID, repo);
                        }

                        TransactionDate = Convert.ToDateTime(dtInvoices.Rows[i]["TransactionDate"]);
                        SalesDocType = dtInvoices.Rows[i]["SalesDocType"].ToString();
                        SalesOrg = dtInvoices.Rows[i]["SalesOrg"].ToString();
                        VehicleCode = dtInvoices.Rows[i]["VehicleCode"].ToString();
                        WarehouseCode = dtInvoices.Rows[i]["WarehouseCode"].ToString();
                        SalesOffice = dtInvoices.Rows[i]["SalesOffice"].ToString();
                        SalesGroup = dtInvoices.Rows[i]["SalesGroup"].ToString();
                        ShipTo = dtInvoices.Rows[i]["ShipTo"].ToString();
                        if (ShipTo.Length > 0 && ShipTo.Substring(0, 1).ToUpper() != "E")
                            ShipTo = ShipTo.PadLeft(10, '0');
                        IRfcFunction companyBapi = repositories[OrgID].CreateFunction("BAPI_VS_CREATE_SO");

                        companyBapi.SetValue("PI_AUART", SalesDocType.ToString()); //Sales Document Type
                        companyBapi.SetValue("PI_VKORG", SalesOrg.ToString()); //Sales Organization
                        companyBapi.SetValue("PI_VTWEG", "10"); //Distribution Channel (Fixed to value '10')
                        companyBapi.SetValue("PI_VKBUR", SalesOffice); //Sales Office
                        companyBapi.SetValue("PI_VKGRP", SalesGroup); //Sales Group
                        companyBapi.SetValue("PI_KUNAG", ShipTo); //Sold-to party (same value as ship-to should be passed here)
                        companyBapi.SetValue("PI_KUNWE", ShipTo); //Ship-to party
                        companyBapi.SetValue("PI_AUDAT", TransactionDate.ToString("yyyyMMdd")); //Document Date (Date Received/Sent) YYYYMMDD
                        companyBapi.SetValue("PI_PO_NO", TransactionID); //Van Sales Invoice Reference Number
                        companyBapi.SetValue("PI_FLAG", "C"); //Fixed value 'C'

                        string invoiceDetails = string.Format(@"SELECT I.ItemCode, PTL.Description UOM, sum(TD.Quantity)Quantity, D.DivisionCode
,CASE WHEN T.TransactionTypeID IN (2,4) AND TD.PackStatusID = 1 THEN '103' 
WHEN T.TransactionTypeID IN (2,4) AND TD.PackStatusID = 2 THEN '102' ELSE '104' END ReturnReason
,TD.Price,'JOD' Currency
,case when ItemCategoryCode  LIKE 'FG-S-F%' then '' else TD.BatchNo end BatchNo
, 'X' UpdatePrice
FROM TransactionDetail TD
INNER JOIN [Transaction] T  on TD.TransactionID=T.TransactionID
and TD.CustomerID=T.CustomerID and TD.OutletID=T.OutletID
INNER JOIN Pack P ON P.PackID = TD.PackID
INNER JOIN Item I ON I.ItemID = P.ItemID and ItemType=1
INNER JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1
INNER JOIN ItemCategory IC ON IC.ItemCategoryID = I.ItemCategoryID
INNER JOIN Division D ON D.DivisionID = IC.DivisionID
WHERE TD.TransactionID = '{0}'
group by I.ItemCode, PTL.Description , D.DivisionCode
,CASE WHEN T.TransactionTypeID IN (2,4) AND TD.PackStatusID = 1 THEN '103' 
WHEN T.TransactionTypeID IN (2,4) AND TD.PackStatusID = 2 THEN '102' ELSE '104' END 
,TD.Price
,case when ItemCategoryCode  LIKE 'FG-S-F%' then '' else TD.BatchNo end", TransactionID);
                        incubeQuery = new InCubeQuery(db_vms, invoiceDetails);
                        if (incubeQuery.Execute() != InCubeErrors.Success)
                        {
                            res = Result.Failure;
                            throw (new Exception("Invoices details query failed !!"));
                        }

                        DataTable dtDetails = incubeQuery.GetDataTable();

                        IRfcTable linesTable = companyBapi.GetTable("PI_SO_ITEMS");
                        for (int j = 0; j < dtDetails.Rows.Count; j++)
                        {
                            string ItemCode = "", UOM = "", Quantity = "", Price="", Currency="", UpdatePrice="";

                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
                            UOM = dtDetails.Rows[j]["UOM"].ToString();
                            Quantity = Convert.ToDecimal(dtDetails.Rows[j]["Quantity"]).ToString("#0.000");
                            Price = Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
                            string batchno = dtDetails.Rows[j]["BatchNo"].ToString();
                            Currency = dtDetails.Rows[j]["Currency"].ToString();
                            UpdatePrice = dtDetails.Rows[j]["UpdatePrice"].ToString();
                            if (j == 0)
                            {
                                companyBapi.SetValue("PI_SPART", dtDetails.Rows[j]["DivisionCode"].ToString()); //Division
                                companyBapi.SetValue("PI_AUGRU", dtDetails.Rows[j]["ReturnReason"].ToString()); //Order reason (reason for the business transaction)
                            }

                            linesTable.Append();
                            linesTable.SetValue("MATNR", ItemCode.PadLeft(18, '0'));
                            linesTable.SetValue("PO_NO", TransactionID);
                            linesTable.SetValue("QUANTITY", Quantity);
                            linesTable.SetValue("UOM", UOM);
                            linesTable.SetValue("LGORT", VehicleCode);
                            linesTable.SetValue("CHARG", batchno);
                            linesTable.SetValue("WERKS", WarehouseCode);
                            linesTable.SetValue("COND_VALUE", Price);
                            linesTable.SetValue("CURRENCY", Currency);
                            linesTable.SetValue("COND_UPDAT", UpdatePrice);
                        }

                        companyBapi.Invoke(destinations[OrgID]);

                        IRfcTable results = companyBapi.GetTable("RETURN");
                        int index = 0;
                        SAP_SO_NUM = companyBapi.GetString("PE_VBELN");
                        if (SAP_SO_NUM.Trim() == "")
                        {
                            res = Result.NoFileRetreived;
                            WriteMessage("Error .. \r\n" + result);
                        }
                        else
                        {
                            res = Result.Success;
                            result.Append("SAP No: " + SAP_SO_NUM);
                            WriteMessage("Success, SAP No: " + SAP_SO_NUM);
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [Transaction] SET Synchronized = 1 WHERE TransactionID = '" + TransactionID + "'");
                            incubeQuery.ExecuteNonQuery();
                        }

                        foreach (IRfcStructure row in results)
                        {
                            result.AppendLine("index: " + index);
                            result.AppendLine("Type: " + row.GetValue("TYPE").ToString());
                            result.AppendLine("ID: " + row.GetValue("ID").ToString());
                            result.AppendLine("Number: " + row.GetValue("NUMBER").ToString());
                            result.AppendLine("Message: " + row.GetValue("MESSAGE").ToString());
                            result.AppendLine("Log_No: " + row.GetValue("LOG_NO").ToString());
                            result.AppendLine("Log_Msg_No: " + row.GetValue("LOG_MSG_NO").ToString());
                            result.AppendLine("Message_V1: " + row.GetValue("MESSAGE_V1").ToString());
                            result.AppendLine("Message_V2: " + row.GetValue("MESSAGE_V2").ToString());
                            result.AppendLine("Message_V3: " + row.GetValue("MESSAGE_V3").ToString());
                            result.AppendLine("Message_V4: " + row.GetValue("MESSAGE_V4").ToString());
                            index++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        result.Append(ex.Message);
                        if (res == Result.UnKnown)
                        {
                            res = Result.Failure;
                            WriteMessage("Unhandled exception !!");
                        }
                    }
                    finally
                    {
                        execManager.LogIntegrationEnding(processID, res, "", result.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("Fetching invoices failed !!");
            }
        }
    }
}