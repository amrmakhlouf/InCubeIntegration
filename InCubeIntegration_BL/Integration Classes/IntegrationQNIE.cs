using InCubeIntegration_DAL;
using InCubeLibrary;
using SAP.Middleware.Connector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Xml;
using System.Diagnostics;
using System.IO;

namespace InCubeIntegration_BL
{
    public class IntegrationQNIE : IntegrationBase
    {
        InCubeQuery incubeQuery = null;
        string SendServerName = "";
        int rowsCount = 0;
        List<Interface> Interfaces = null;
        Stopwatch sw = new Stopwatch();
        private class Interface
        {
            public string SAP_Table;
            public string StagingTable;
            public IRfcTable detail;
            public Dictionary<string, string> columns;
            public Interface(string stg_table)
            {
                StagingTable = stg_table;
                SAP_Table = "ITEMTAB";
            }
            public Interface(string stg_table, string sap_table)
            {
                StagingTable = stg_table;
                SAP_Table = sap_table;
            }
        }
        public IntegrationQNIE(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(CoreGeneral.Common.StartupPath + "\\DataSources.xml");
                SendServerName = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'SAP']/Data").InnerText;
            }
            catch (Exception ex)
            {
                Initialized = false;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        public override void Close()
        {
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
            GetMasterData(IntegrationField.Vehicles_U);
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

        public override void OutStanding()
        {
            GetMasterData(IntegrationField.Outstanding_U);
        }

        public override void UpdateStock()
        {
            GetMasterData(IntegrationField.Stock_U);
        }
        public override void UpdateMainWHStock()
        {
            GetMasterData(IntegrationField.MainWarehouseStock_U);
        }
        public override void UpdateOrders()
        {
            GetMasterData(IntegrationField.Orders_U);
        }
        private Result SaveToStagingTable(string MasterName)
        {
            Result res = Result.UnKnown;
            foreach (Interface i in Interfaces)
            {
                int ProcessID = 0;
                DataTable dtMasterData = new DataTable();
                try
                {
                    //Log begining of reading from RFC table
                    Dictionary<int, string> filters = new Dictionary<int, string>();
                    filters.Add(1, MasterName);
                    filters.Add(2, "Reading RFC Table");
                    filters.Add(3, i.SAP_Table);
                    ProcessID = execManager.LogIntegrationBegining(TriggerID, OrganizationID, filters);
                    WriteMessage("\r\nReading from RFC table ... ");
                    sw.Reset();
                    sw.Start();
                    res = FillDataTableFromRFCTable(i.columns, i.detail, ref dtMasterData);
                    sw.Stop();
                    WriteMessage("completed in " + sw.ElapsedMilliseconds / 1000m + " seconds");
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
                    break;

                ProcessID = 0;
                try
                {
                    //Log begining of saving to staging table
                    Dictionary<int, string> filters = new Dictionary<int, string>();
                    filters.Add(1, MasterName);
                    filters.Add(2, "Saving to staging");
                    filters.Add(3, i.StagingTable);
                    ProcessID = execManager.LogIntegrationBegining(TriggerID, OrganizationID, filters);
                    WriteMessage("\r\nSaving data to staging table ... ");
                    sw.Reset();
                    sw.Start();
                    res = SaveTable(dtMasterData, i.StagingTable);
                    sw.Stop();
                    WriteMessage("completed in " + sw.ElapsedMilliseconds / 1000m + " seconds");
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
                    break;
            }

            if (res != Result.Success)
            {
                WriteMessage(" Error in saving to staging table !!");
            }
            else
            {
                WriteMessage(" Success ..");
            }
            return res;
        }
        private void GetMasterData(IntegrationField field)
        {
            Result res = Result.UnKnown;
            Interfaces = new List<Interface>();
            string MasterName = field.ToString().Remove(field.ToString().Length - 2, 2);
            if (CoreGeneral.Common.userPrivileges.UpdateFieldsAccess.ContainsKey(field))
                MasterName = CoreGeneral.Common.userPrivileges.UpdateFieldsAccess[field].Description;
            Dictionary<int, string> filters;
            int ProcessID = 0;
            bool ClearAll = true;
            Initialized = false;
            rowsCount = 0;

            try
            {
                //Log begining of read from SAP
                filters = new Dictionary<int, string>();
                filters.Add(1, MasterName);
                filters.Add(2, "Reading from SAP");
                ProcessID = execManager.LogIntegrationBegining(TriggerID, OrganizationID, filters);
                WriteMessage("\r\nRetrieving " + MasterName + " from SAP ... ");
                sw.Start();

                switch (field)
                {
                    case IntegrationField.Item_U:
                        Interfaces.Add(new Interface("Stg_Items"));
                        res = GetItemsTable();
                        break;
                    case IntegrationField.Customer_U:
                        Interfaces.Add(new Interface("Stg_Customers"));
                        res = GetCustomerTable();
                        break;
                    case IntegrationField.Price_U:
                        Interfaces.Add(new Interface("Stg_Prices"));
                        res = GetPricesTable();
                        break;
                    case IntegrationField.Promotion_U:
                        Interfaces.Add(new Interface("Stg_Promotions"));
                        res = GetPromotionsTable();
                        Interfaces.Add(new Interface("Stg_GroupPromotions"));
                        Interfaces.Add(new Interface("Stg_PackGroups", "GRUPTAB"));
                        res = GetGroupPromotionsTable();
                        break;
                    case IntegrationField.Salesperson_U:
                        Interfaces.Add(new Interface("Stg_Salespersons"));
                        res = GetSalespersonTable();
                        break;
                    case IntegrationField.Vehicles_U:
                        Interfaces.Add(new Interface("Stg_Locations"));
                        res = GetLocationsTable();
                        break;
                    case IntegrationField.Outstanding_U:
                        Interfaces.Add(new Interface("Stg_Invoices"));
                        res = GetOutstandingTable(ref ClearAll);
                        break;
                    case IntegrationField.Stock_U:
                        Interfaces.Add(new Interface("Stg_WarehouseTransactions"));
                        res = GetStockTransactions();
                        break;
                    case IntegrationField.MainWarehouseStock_U:
                        Interfaces.Add(new Interface("Stg_MainWarehouseStock"));
                        res = GetMainWHStockTable();
                        break;
                    case IntegrationField.STA_U:
                        Interfaces.Add(new Interface("Stg_OrderStatus"));
                        res = GetOrderStatusTable();
                        break;
                    case IntegrationField.Orders_U:
                        Interfaces.Add(new Interface("Stg_LoadOrders"));
                        res = GetLoadOrdersTable();
                        break;
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
                execManager.LogIntegrationEnding(ProcessID, res, "", "Rows retrieved: " + rowsCount);
                sw.Stop();
            }

            try
            {
                if (res == Result.Success)
                {
                    WriteMessage(" Rows retrieved: " + rowsCount + " in " + sw.ElapsedMilliseconds / 1000m  + " seconds");
                    execManager.UpdateActionTotalRows(TriggerID, rowsCount);
                    res = SaveToStagingTable(MasterName);
                    if (res == Result.Success)
                        Initialized = true;
                }
                else
                {
                    switch (res)
                    {
                        case Result.Invalid:
                            WriteMessage(" Error in reading interface definition !!");
                            break;
                        case Result.Failure:
                            WriteMessage(" Error in reading from SAP !!");
                            break;
                        case Result.Blocked:
                            WriteMessage(" Invalid filters for calling SAP service !!");
                            break;
                        default:
                            execManager.UpdateActionTotalRows(TriggerID, 0);
                            WriteMessage(" No data found !!");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }

            if (res == Result.Success && field == IntegrationField.Outstanding_U)
            {
                Procedure Proc = new Procedure();
                Proc.ProcedureName = "sp_UpdateOutstanding";
                Proc.AddParameter("@OrganizationID", ParamType.Integer, OrganizationID);
                Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID);
                Proc.AddParameter("@ClearAll", ParamType.BIT, ClearAll);
                res = ExecuteStoredProcedure(Proc);

                if (res == Result.Success)
                {
                    int TotalRows = 0, Inserted = 0, Updated = 0, Skipped = 0;
                    GetExecutionResults(TriggerID, ref TotalRows, ref Inserted, ref Updated, ref Skipped, db_vms);
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
                SqlBulkCopy bulk = new SqlBulkCopy(db_vms.GetConnection());
                bulk.DestinationTableName = TableName;
                foreach (DataColumn col in dtData.Columns)
                    bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                bulk.BulkCopyTimeout = 120;
                decimal loopSize = 10000;
                int loops = (int)Math.Ceiling((decimal)dtData.Rows.Count / loopSize);
                SetProgressMax(loops);
                for (int i = 0; i < loops; i++)
                {
                    dtData.DefaultView.RowFilter = string.Format("ID >= {0} AND ID <= {1}", i * loopSize + 1, (i + 1) * loopSize);
                    bulk.WriteToServer(dtData.DefaultView.ToTable());
                    ReportProgress();
                }
                res = Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result SaveTable_old(DataTable dtData, string TableName)
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

        private class RFC_Filter
        {
            public string Name = "";
            public bool IsTableFilter = false;
            public List<RFC_Filter_Value> Values = new List<RFC_Filter_Value>();
        }
        private class RFC_Filter_Value
        {
            public string Option = "";
            public string Low = "";
            public string High = "";
         
            public Result SetValue(string ValueName, string Value, int ValueType, string ValueFormat)
            {
                Result res = Result.UnKnown;
                try
                {
                    string formattedValue = "";
                    if (ValueType == 1)
                    {
                        formattedValue = Value;
                    }
                    else if (ValueType == 2)
                    {
                        DateTime dateValue;
                        if (!DateTime.TryParse(Value, out dateValue))
                        {
                            int indexOfSign = Value.IndexOfAny(new char[] { '-', '+' });
                            string baseDate = Value;
                            if (indexOfSign > 0)
                                baseDate = Value.Substring(0, indexOfSign);
                            if (baseDate.ToLower() == "@today")
                                dateValue = DateTime.Today;

                            if (indexOfSign > 0)
                            {
                                int period = int.Parse(Value.Substring(indexOfSign, Value.Length - indexOfSign));
                                dateValue = dateValue.AddDays(period);
                            }
                        }
                        formattedValue = dateValue.ToString(ValueFormat);
                    }
                    if (ValueName == "HIGH")
                        High = formattedValue;
                    else
                        Low = formattedValue;

                    res = Result.Success;
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                    res = Result.Failure;
                }
                return res;
            }
        }
        private Result GetInterfaceParams(IntegrationField field, ref string FunctionName, ref Dictionary<string, RFC_Filter> filters)
        {
            Result res = Result.UnKnown;
            try
            {
                FunctionName = "";
                filters = new Dictionary<string, RFC_Filter>();
                incubeQuery = new InCubeQuery(db_vms, string.Format(@"SELECT *
FROM SAP_Functions
WHERE FieldID = {0} AND OrganizationID = {1}", field.GetHashCode(), OrganizationID));
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    DataTable dtFunctionDetails = new DataTable();
                    dtFunctionDetails = incubeQuery.GetDataTable();
                    if (dtFunctionDetails.Rows.Count > 0)
                    {
                        FunctionName = dtFunctionDetails.Rows[0]["FunctionName"].ToString();
                        for (int i = 0; i< dtFunctionDetails.Rows.Count;i++)
                        {
                            string FilterName = dtFunctionDetails.Rows[i]["FilterName"].ToString();
                            if (!filters.ContainsKey(FilterName))
                            {
                                RFC_Filter filter = new RFC_Filter();
                                filter.Name = FilterName;
                                filter.IsTableFilter = bool.Parse(dtFunctionDetails.Rows[i]["IsTableFilter"].ToString());
                                filters.Add(FilterName, filter);
                            }
                             
                            RFC_Filter_Value filterValue = new RFC_Filter_Value();
                            filterValue.Option = dtFunctionDetails.Rows[i]["FilterOption"].ToString();
                            int ValueType = int.Parse(dtFunctionDetails.Rows[i]["ValueType"].ToString());
                            string ValueFormat = dtFunctionDetails.Rows[i]["ValueFormat"].ToString();
                            string LowValue = dtFunctionDetails.Rows[i]["LowValue"].ToString();
                            string HighValue = dtFunctionDetails.Rows[i]["HighValue"].ToString();

                            res = filterValue.SetValue("LOW", LowValue, ValueType, ValueFormat);
                            if (res != Result.Success)
                                return res;
                            if (HighValue != "")
                            {
                                res = filterValue.SetValue("HIGH", HighValue, ValueType, ValueFormat);
                                if (res != Result.Success)
                                    return res;
                            }

                            filters[FilterName].Values.Add(filterValue);
                        }
                    }
                    res = Result.Success;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                res = Result.Failure;
            }
            return res;
        }
        private Result GetSAPTable(IntegrationField field,ref DataTable DT)
        {
            Result res = Result.Failure;
            DT = new DataTable();
            try
            {
                string functionName = "";
                Dictionary<string, RFC_Filter> filters = new Dictionary<string, RFC_Filter>();
                res = GetInterfaceParams(field, ref functionName, ref filters);
                if (res != Result.Success)
                    return Result.Invalid;

                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction(functionName);

                foreach (RFC_Filter filter in filters.Values)
                {
                    if (filter.IsTableFilter)
                    { 
                        IRfcTable rfc_Filter = companyBapi.GetTable(filter.Name);
                        foreach (RFC_Filter_Value filterVal in filter.Values)
                        {
                            rfc_Filter.Append();
                            rfc_Filter.SetValue("SIGN", "I");
                            rfc_Filter.SetValue("OPTION", filterVal.Option);
                            rfc_Filter.SetValue("LOW", filterVal.Low);
                            if (filterVal.High != "")
                                rfc_Filter.SetValue("HIGH", filterVal.High);
                            incubeQuery = new InCubeQuery(db_vms, string.Format("INSERT INTO Trigger_SAP_Filters VALUES ({0},1,'{1}','{2}','{3}','{4}')", TriggerID, filter.Name, filterVal.Option, filterVal.Low, filterVal.High));
                            incubeQuery.ExecuteNonQuery();
                        }
                        companyBapi.SetValue(filter.Name, rfc_Filter);
                    }
                    else
                    {
                        RFC_Filter_Value filterVal = filter.Values[0];
                        companyBapi.SetValue(filter.Name, filterVal.Low);
                        incubeQuery = new InCubeQuery(db_vms, string.Format("INSERT INTO Trigger_SAP_Filters VALUES ({0},0,'{1}','{2}','{3}','{4}')", TriggerID, filter.Name, filterVal.Option, filterVal.Low, filterVal.High));
                        incubeQuery.ExecuteNonQuery();
                    }
                }

                companyBapi.Invoke(prd);
                IRfcTable detail = companyBapi.GetTable(0);

                Dictionary<string, string> columns = new Dictionary<string, string>();
                columns.Add("CompanyCode", "BUKRS");
                columns.Add("SalesOrg", "VKORG");
                columns.Add("ItemBrandCode", "MATKL");
                columns.Add("ItemBrandName", "WGBEZ");
                columns.Add("ItemCode", "MATNR");
                columns.Add("ItemName", "GROES");
                columns.Add("BaseUOM", "MEINS");
                columns.Add("ItemUOM", "MEINH");
                columns.Add("Numerator", "UMREZ");
                columns.Add("Denominator", "UMREN");
                columns.Add("InActive", "LVORM");
                columns.Add("CWM", "CWMAT");
                columns.Add("ItemGroupCode", "PRDHA");
                columns.Add("ItemGroupName", "BEZEI");
                columns.Add("ItemCategoryCode", "EXTWG");
                columns.Add("ItemCategoryDescription", "EWBEZ");
                columns.Add("Barcode", "EAN11");
                columns.Add("DivisionCode", "SPART");
                columns.Add("DivisionName", "VTEXT");
                columns.Add("FLAG", "ZFLAG");

                foreach (string ColumnName in columns.Keys)
                {
                    DT.Columns.Add(ColumnName);
                }

                foreach (IRfcStructure row in detail)
                {
                    DataRow _row = DT.NewRow();
                    foreach (KeyValuePair<string, string> colData in columns)
                    {
                        _row[colData.Key] = row.GetValue(colData.Value).ToString();
                    }
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
        private Result FillDataTableFromRFCTable(Dictionary<string, string> columns, IRfcTable detail, ref DataTable DT)
        {
            DT = new DataTable();
            Result res = Result.UnKnown;
            try
            {
                DT.Columns.Add("ID", typeof(int));
                DT.Columns.Add("TriggerID", typeof(int));
                DT.Columns.Add("OrganizationID", typeof(int));
                foreach (string ColumnName in columns.Keys)
                {
                    DT.Columns.Add(ColumnName);
                }

                int ID = 0;
                SetProgressMax(detail.RowCount);
                foreach (IRfcStructure row in detail)
                {
                    DataRow _row = DT.NewRow();
                    _row["ID"] = ++ID;
                    _row["TriggerID"] = TriggerID;
                    _row["OrganizationID"] = OrganizationID;
                    foreach (KeyValuePair<string, string> colData in columns)
                    {
                        _row[colData.Key] = row.GetValue(colData.Value).ToString();
                    }
                    DT.Rows.Add(_row);
                    ReportProgress();    
                }
                if (DT.Rows.Count == detail.RowCount)
                    res = Result.Success;
                else
                    res = Result.Invalid;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result GetItemsTable()
        {
            Result res = Result.Failure;
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_MATERIAL_MASTER");

                IRfcTable OrgFilter = companyBapi.GetTable("IR_VKORG");
                OrgFilter.Append();
                OrgFilter.SetValue("SIGN", "I");
                OrgFilter.SetValue("OPTION", "EQ");
                OrgFilter.SetValue("LOW", OrganizationCode);
                companyBapi.SetValue("IR_VKORG", OrgFilter);

                if (Filters.FromDate != DateTime.MinValue && Filters.ToDate != DateTime.MaxValue)
                {
                    IRfcTable DateFilter = companyBapi.GetTable("IR_DATUM");
                    DateFilter.Append();
                    DateFilter.SetValue("SIGN", "I");
                    if (Filters.FromDate != Filters.ToDate)
                    {
                        DateFilter.SetValue("OPTION", "BT");
                        DateFilter.SetValue("LOW", Filters.FromDate.Date.ToString("yyyyMMdd"));
                        DateFilter.SetValue("HIGH", Filters.ToDate.Date.ToString("yyyyMMdd"));
                    }
                    else
                    {
                        DateFilter.SetValue("OPTION", "EQ");
                        DateFilter.SetValue("LOW", Filters.FromDate.Date.ToString("yyyyMMdd"));
                    }
                    companyBapi.SetValue("IR_DATUM", DateFilter);
                }

                companyBapi.Invoke(prd);

                Interfaces[0].detail = companyBapi.GetTable(Interfaces[0].SAP_Table);

                Dictionary<string, string> columns = new Dictionary<string, string>();
                columns.Add("CompanyCode", "BUKRS");
                columns.Add("SalesOrg", "VKORG");
                columns.Add("ItemBrandCode", "MATKL");
                columns.Add("ItemBrandName", "WGBEZ");
                columns.Add("ItemCode", "MATNR");
                columns.Add("ItemName", "GROES");
                columns.Add("BaseUOM", "MEINS");
                columns.Add("ItemUOM", "MEINH");
                columns.Add("Numerator", "UMREZ");
                columns.Add("Denominator", "UMREN");
                columns.Add("InActive", "LVORM");
                columns.Add("CWM", "CWMAT");
                columns.Add("ItemGroupCode", "PRDHA");
                columns.Add("ItemGroupName", "BEZEI");
                columns.Add("ItemCategoryCode", "EXTWG");
                columns.Add("ItemCategoryDescription", "EWBEZ");
                columns.Add("Barcode", "EAN11");
                columns.Add("DivisionCode", "SPART");
                columns.Add("DivisionName", "VTEXT");
                columns.Add("FLAG", "ZFLAG");
                columns.Add("CreationDate", "ERSDA");
                columns.Add("UpdateDate", "LAEDA");
                columns.Add("DeliveryPlant", "DWERK");
                Interfaces[0].columns = columns;

                if (Interfaces[0].detail.Count > 0)
                {
                    res = Result.Success;
                    rowsCount = Interfaces[0].detail.Count;
                }
                else
                {
                    res = Result.NoRowsFound;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        
        private Result GetCustomerTable()
        {
            Result res = Result.Failure;
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_CUSTOMER_MASTER");
                
                IRfcTable OrgFilter = companyBapi.GetTable("IR_VKORG");
                OrgFilter.Append();
                OrgFilter.SetValue("SIGN", "I");
                OrgFilter.SetValue("OPTION", "EQ");
                OrgFilter.SetValue("LOW", OrganizationCode);
                companyBapi.SetValue("IR_VKORG", OrgFilter);

                IRfcTable ChannelFilter = companyBapi.GetTable("IR_VTWEG");
                ChannelFilter.Append();
                ChannelFilter.SetValue("SIGN", "I");
                ChannelFilter.SetValue("OPTION", "BT");
                ChannelFilter.SetValue("LOW", "A1");
                ChannelFilter.SetValue("HIGH", "A6");
                companyBapi.SetValue("IR_VTWEG", ChannelFilter);

                if (Filters.FromDate != DateTime.MinValue && Filters.ToDate != DateTime.MaxValue)
                {
                    IRfcTable DateFilter = companyBapi.GetTable("IR_DATUM");
                    DateFilter.Append();
                    DateFilter.SetValue("SIGN", "I");
                    if (Filters.FromDate != Filters.ToDate)
                    {
                        DateFilter.SetValue("OPTION", "BT");
                        DateFilter.SetValue("LOW", Filters.FromDate.Date.ToString("yyyyMMdd"));
                        DateFilter.SetValue("HIGH", Filters.ToDate.Date.ToString("yyyyMMdd"));
                    }
                    else
                    {
                        DateFilter.SetValue("OPTION", "EQ");
                        DateFilter.SetValue("LOW", Filters.FromDate.Date.ToString("yyyyMMdd"));
                    }
                    companyBapi.SetValue("IR_DATUM", DateFilter);
                }

                incubeQuery = new InCubeQuery(db_vms, "SELECT DivisionCode FROM Division WHERE OrganizationID = " + OrganizationID);
                incubeQuery.Execute();
                DataTable dtDivisions = incubeQuery.GetDataTable();
                if (dtDivisions.Rows.Count > 0)
                {
                    IRfcTable DivisionFilter = companyBapi.GetTable("IR_SPART");
                    foreach (DataRow dr in dtDivisions.Rows)
                    {
                        DivisionFilter.Append();
                        DivisionFilter.SetValue("SIGN", "I");
                        DivisionFilter.SetValue("OPTION", "EQ");
                        DivisionFilter.SetValue("LOW", dr["DivisionCode"].ToString());
                    }
                    companyBapi.SetValue("IR_SPART", DivisionFilter);
                }

                companyBapi.Invoke(prd);
                Interfaces[0].detail = companyBapi.GetTable(Interfaces[0].SAP_Table);

                Dictionary<string,string> columns = new Dictionary<string, string>();
                columns.Add("CompanyCode", "BUKRS");
                columns.Add("SalesOrg", "VKORG");
                columns.Add("ChannelCode", "VTWEG");
                columns.Add("ChannelName", "DTEXT");
                columns.Add("DivisionCode", "SPART");
                columns.Add("DivisionName", "VTEXT");
                columns.Add("BillToCode", "KUNAG");
                columns.Add("BillToName", "NAMAG");
                columns.Add("PayerCode", "KUNRG");
                columns.Add("PayerName", "NAMRG");
                columns.Add("ShipToCode", "KUNWE");
                columns.Add("ShipToName", "NAMWE");
                columns.Add("BlockStatus", "AUFSD");
                columns.Add("DeleteStatus", "LOEVM");
                columns.Add("CustomerType", "CTYPE");
                columns.Add("PaymentBlock", "PTTYP");
                columns.Add("PaymentTermDays", "PTDAY");
                columns.Add("CreditLimit", "KLIMK");
                columns.Add("CustomerGroupCode", "KDGRP");
                columns.Add("CustomerGroupName", "KTEXT");
                columns.Add("PriceGroup", "KONDA");
                columns.Add("PriceGroupName", "PGTXT");
                columns.Add("RouteCode", "RCODE");
                columns.Add("RouteName", "RNAME");
                columns.Add("Address", "ADDRS");
                columns.Add("AddressName", "CCNAM");
                columns.Add("Telephone", "TELF1");
                columns.Add("Mobile", "TELF2");
                columns.Add("Region", "BLAND");
                columns.Add("Region2", "REGIO");
                columns.Add("Zone", "LZONE");
                columns.Add("Class", "KLABC");
                columns.Add("SalesmanCode", "PERNR");
                columns.Add("Flag", "ZFLAG");
                columns.Add("Longitude", "BAHNS");
                columns.Add("Latitude", "BAHNE");
                columns.Add("RiskCategory", "CTLPC");
                Interfaces[0].columns = columns;

                if (Interfaces[0].detail.Count > 0)
                {
                    res = Result.Success;
                    rowsCount = Interfaces[0].detail.Count;
                }
                else
                {
                    res = Result.NoRowsFound;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result GetSalespersonTable()
        {
            Result res = Result.Failure;
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_SALESMAN_MASTER");

                IRfcTable OrgFilter = companyBapi.GetTable("IR_BUKRS");
                OrgFilter.Append();
                OrgFilter.SetValue("SIGN", "I");
                OrgFilter.SetValue("OPTION", "EQ");
                OrgFilter.SetValue("LOW", OrganizationCode);
                companyBapi.SetValue("IR_BUKRS", OrgFilter);

                companyBapi.Invoke(prd);
                Interfaces[0].detail = companyBapi.GetTable(Interfaces[0].SAP_Table);

                Dictionary<string,string> columns = new Dictionary<string, string>();
                columns.Add("SalesmanCode", "PERNR");
                columns.Add("SalesmanName", "ENAME");
                columns.Add("SalesmanPhone", "TELF1");
                columns.Add("SupervisorCode", "ENSPV");
                columns.Add("SuperVisorName", "NMSPV");
                columns.Add("SalesManagerCode", "ENMGR");
                columns.Add("SalesManagerName", "NMNGR");
                columns.Add("OrganizationCode", "BUKRS");
                columns.Add("Status", "ZFLAG");
                Interfaces[0].columns = columns;

                if (Interfaces[0].detail.Count > 0)
                {
                    res = Result.Success;
                    rowsCount = Interfaces[0].detail.Count;
                }
                else
                {
                    res = Result.NoRowsFound;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result GetPricesTable()
        {
            Result res = Result.Failure;
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_PRICE_MASTER");

                IRfcTable OrgFilter = companyBapi.GetTable("IR_VKORG");
                OrgFilter.Append();
                OrgFilter.SetValue("SIGN", "I");
                OrgFilter.SetValue("OPTION", "EQ");
                OrgFilter.SetValue("LOW", OrganizationCode);
                companyBapi.SetValue("IR_VKORG", OrgFilter);

                incubeQuery = new InCubeQuery(db_vms, "SELECT DivisionCode FROM Division WHERE OrganizationID = " + OrganizationID);
                incubeQuery.Execute();
                DataTable dtDivisions = incubeQuery.GetDataTable();
                if (dtDivisions.Rows.Count > 0)
                {
                    IRfcTable DivisionFilter = companyBapi.GetTable("IR_SPART");
                    foreach (DataRow dr in dtDivisions.Rows)
                    {
                        DivisionFilter.Append();
                        DivisionFilter.SetValue("SIGN", "I");
                        DivisionFilter.SetValue("OPTION", "EQ");
                        DivisionFilter.SetValue("LOW", dr["DivisionCode"].ToString());
                    }
                    companyBapi.SetValue("IR_SPART", DivisionFilter);
                }

                companyBapi.Invoke(prd);
                Interfaces[0].detail = companyBapi.GetTable(Interfaces[0].SAP_Table);

                Dictionary<string,string> columns = new Dictionary<string, string>();
                columns.Add("CompanyCode", "BUKRS");
                columns.Add("SalesOrg", "VKORG");
                columns.Add("DivisionCode", "SPART");
                columns.Add("GroupCode", "KONDA");
                columns.Add("CustomerCode", "KUNWE");
                columns.Add("ItemCode", "MATNR");
                columns.Add("ValidFrom", "DATAB");
                columns.Add("ValidTo", "DATBI");
                columns.Add("UOM", "KMEIN");
                columns.Add("Price", "KBETR");
                columns.Add("IsDeleted", "LOEVM");
                columns.Add("ObjectKey", "OBJKY");
                columns.Add("ConversionFactor", "CONVF");
                columns.Add("DistributionChannel", "VTWEG");
                columns.Add("FLAG", "ZFLAG");
                Interfaces[0].columns = columns;

                if (Interfaces[0].detail.Count > 0)
                {
                    res = Result.Success;
                    rowsCount = Interfaces[0].detail.Count;
                }
                else
                {
                    res = Result.NoRowsFound;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result GetLocationsTable()
        {
            Result res = Result.Failure;
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_LOCATION_MASTER");

                IRfcTable OrgFilter = companyBapi.GetTable("IR_BUKRS");
                OrgFilter.Append();
                OrgFilter.SetValue("SIGN", "I");
                OrgFilter.SetValue("OPTION", "EQ");
                OrgFilter.SetValue("LOW", OrganizationCode);
                companyBapi.SetValue("IR_BUKRS", OrgFilter);
                
                companyBapi.Invoke(prd);
                Interfaces[0].detail = companyBapi.GetTable(Interfaces[0].SAP_Table);

                Dictionary<string,string> columns = new Dictionary<string, string>();
                columns.Add("CompanyCode", "BUKRS");
                columns.Add("Plant", "RESWK");
                columns.Add("RouteCode", "RCODE");
                columns.Add("SalesmanCode", "PERNR");
                columns.Add("DivisionCode", "SPART");
                columns.Add("Flag", "ZFLAG");
                Interfaces[0].columns = columns;

                if (Interfaces[0].detail.Count > 0)
                {
                    res = Result.Success;
                    rowsCount = Interfaces[0].detail.Count;
                }
                else
                {
                    res = Result.NoRowsFound;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result GetLoadOrdersTable()
        {
            Result res = Result.Failure;
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_PURCHASE_ORDER");

                IRfcTable OrgFilter = companyBapi.GetTable("IR_BUKRS");
                OrgFilter.Append();
                OrgFilter.SetValue("SIGN", "I");
                OrgFilter.SetValue("OPTION", "EQ");
                OrgFilter.SetValue("LOW", OrganizationCode);
                companyBapi.SetValue("IR_BUKRS", OrgFilter);

                IRfcTable DateFilter = companyBapi.GetTable("IR_BEDAT");
                DateFilter.Append();
                DateFilter.SetValue("SIGN", "I");
                DateFilter.SetValue("OPTION", "EQ");
                DateFilter.SetValue("LOW", Filters.StockDate.ToString("yyyyMMdd"));
                companyBapi.SetValue("IR_BEDAT", DateFilter);
                
                companyBapi.Invoke(prd);
                Interfaces[0].detail = companyBapi.GetTable(Interfaces[0].SAP_Table);

                Dictionary<string, string> columns = new Dictionary<string, string>();
                columns.Add("CompanyCode", "BUKRS");
                columns.Add("PONumber", "EBELN");
                columns.Add("Vendor", "LIFNR");
                columns.Add("VehicleCode", "VWERK");
                columns.Add("TransactionDate", "BEDAT");
                columns.Add("LoadDate", "LFDAT");
                columns.Add("ItemNumber", "EBELP");
                columns.Add("ItemCode", "MATNR");
                columns.Add("UOM", "MEINS");
                columns.Add("Quantity", "MENGE");

                Interfaces[0].columns = columns;

                if (Interfaces[0].detail.Count > 0)
                {
                    res = Result.Success;
                    rowsCount = Interfaces[0].detail.Count;
                }
                else
                {
                    res = Result.NoRowsFound;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result GetStockTransactions()
        {
            Result res = Result.Failure;
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_LOAD_OFFLOAD_STATUS");

                IRfcTable OrgFilter = companyBapi.GetTable("IR_BUKRS");
                OrgFilter.Append();
                OrgFilter.SetValue("SIGN", "I");
                OrgFilter.SetValue("OPTION", "EQ");
                OrgFilter.SetValue("LOW", OrganizationCode);
                companyBapi.SetValue("IR_BUKRS", OrgFilter);

                IRfcTable DateFilter = companyBapi.GetTable("IR_GRDAT");
                DateFilter.Append();
                DateFilter.SetValue("SIGN", "I");
                DateFilter.SetValue("OPTION", "EQ");
                DateFilter.SetValue("LOW", Filters.StockDate.ToString("yyyyMMdd"));
                companyBapi.SetValue("IR_GRDAT", DateFilter);
                
                IRfcTable YearFilter = companyBapi.GetTable("IR_GJAHR");
                YearFilter.Append();
                YearFilter.SetValue("SIGN", "I");
                YearFilter.SetValue("OPTION", "EQ");
                YearFilter.SetValue("LOW", Filters.StockDate.Year);
                companyBapi.SetValue("IR_GJAHR", YearFilter);

                companyBapi.SetValue("IV_VGART", "0");

                companyBapi.Invoke(prd);
                Interfaces[0].detail = companyBapi.GetTable(Interfaces[0].SAP_Table);

                Dictionary<string,string> columns = new Dictionary<string, string>();
                columns.Add("CompanyCode", "BUKRS");
                columns.Add("TransactionType", "VGART");
                columns.Add("RefNumber", "XBLNR");
                columns.Add("TransactionNumber", "VBELN");
                columns.Add("Plant", "WERKS");
                columns.Add("VehicleCode", "VWERK");
                columns.Add("Status", "ZSTAT");
                columns.Add("PostingDate", "BUDAT");
                columns.Add("ItemCode", "MATNR");
                columns.Add("Quantity", "MENGE");
                columns.Add("BaseUOM", "MEINS");
                columns.Add("CatchWeightQty", "CWQTY");
                columns.Add("ConversionFactor", "CONVF");
                columns.Add("DivisionCode", "SPART");
                columns.Add("BatchNo", "CHARG");
                columns.Add("ExpiryDate", "VFDAT");
                Interfaces[0].columns = columns;

                if (Interfaces[0].detail.Count > 0)
                {
                    res = Result.Success;
                    rowsCount = Interfaces[0].detail.Count;
                }
                else
                {
                    res = Result.NoRowsFound;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result GetPromotionsTable()
        {
            Result res = Result.Failure;
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_FOC_MASTER");

                IRfcTable tblImport = companyBapi.GetTable("IR_VKORG");
                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", OrganizationCode);
                companyBapi.SetValue("IR_VKORG", tblImport);

                incubeQuery = new InCubeQuery(db_vms, "SELECT DivisionCode FROM Division WHERE OrganizationID = " + OrganizationID);
                incubeQuery.Execute();
                DataTable dtDivisions = incubeQuery.GetDataTable();
                if (dtDivisions.Rows.Count > 0)
                {
                    IRfcTable DivisionFilter = companyBapi.GetTable("IR_SPART");
                    foreach (DataRow dr in dtDivisions.Rows)
                    {
                        DivisionFilter.Append();
                        DivisionFilter.SetValue("SIGN", "I");
                        DivisionFilter.SetValue("OPTION", "EQ");
                        DivisionFilter.SetValue("LOW", dr["DivisionCode"].ToString());
                    }
                    companyBapi.SetValue("IR_SPART", DivisionFilter);
                }

                if (Filters.FromDate != DateTime.MinValue && Filters.ToDate != DateTime.MaxValue)
                {
                    IRfcTable DateFilter = companyBapi.GetTable("IR_DATUM");
                    DateFilter.Append();
                    DateFilter.SetValue("SIGN", "I");
                    if (Filters.FromDate != Filters.ToDate)
                    {
                        DateFilter.SetValue("OPTION", "BT");
                        DateFilter.SetValue("LOW", Filters.FromDate.Date.ToString("yyyyMMdd"));
                        DateFilter.SetValue("HIGH", Filters.ToDate.Date.ToString("yyyyMMdd"));
                    }
                    else
                    {
                        DateFilter.SetValue("OPTION", "EQ");
                        DateFilter.SetValue("LOW", Filters.FromDate.Date.ToString("yyyyMMdd"));
                    }
                    companyBapi.SetValue("IR_DATUM", DateFilter);
                }

                companyBapi.Invoke(prd);
                Interfaces[0].detail = companyBapi.GetTable(Interfaces[0].SAP_Table);

                Dictionary<string,string> columns = new Dictionary<string, string>();
                columns.Add("CompanyCode", "BUKRS");
                columns.Add("ValidFrom", "DATAB");
                columns.Add("ValidTo", "DATBI");
                columns.Add("BuyItemCode", "MAT01");
                columns.Add("BuyUOM", "UOM01");
                columns.Add("BuyConversionFactor", "CNV01");
                columns.Add("BuyQuantity", "QTY01");
                columns.Add("GetItemCode", "MAT02");
                columns.Add("GetUOM", "UOM02");
                columns.Add("GetConversionFactor", "CNV02");
                columns.Add("GetQuantity", "QTY02");
                columns.Add("DivisionCode", "SPART");
                columns.Add("SalesGroupCode", "KONDA");
                columns.Add("CustomerShipTo", "KUNWE");
                columns.Add("IsDeleted", "LOEVM");
                Interfaces[0].columns = columns;

                if (Interfaces[0].detail.Count > 0)
                {
                    res = Result.Success;
                    rowsCount = Interfaces[0].detail.Count;
                }
                else
                {
                    res = Result.NoRowsFound;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        
        private Result GetGroupPromotionsTable()
        {
            Result res = Result.Failure;
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_GROUP_PROMOTION");

                IRfcTable tblImport = companyBapi.GetTable("IR_VKORG");
                tblImport.Append();
                tblImport.SetValue("SIGN", "I");
                tblImport.SetValue("OPTION", "EQ");
                tblImport.SetValue("LOW", OrganizationCode);
                companyBapi.SetValue("IR_VKORG", tblImport);

                incubeQuery = new InCubeQuery(db_vms, "SELECT DivisionCode FROM Division WHERE OrganizationID = " + OrganizationID);
                incubeQuery.Execute();
                DataTable dtDivisions = incubeQuery.GetDataTable();
                if (dtDivisions.Rows.Count > 0)
                {
                    IRfcTable DivisionFilter = companyBapi.GetTable("IR_SPART");
                    foreach (DataRow dr in dtDivisions.Rows)
                    {
                        DivisionFilter.Append();
                        DivisionFilter.SetValue("SIGN", "I");
                        DivisionFilter.SetValue("OPTION", "EQ");
                        DivisionFilter.SetValue("LOW", dr["DivisionCode"].ToString());
                    }
                    companyBapi.SetValue("IR_SPART", DivisionFilter);
                }

                if (Filters.FromDate != DateTime.MinValue && Filters.ToDate != DateTime.MaxValue)
                {
                    IRfcTable DateFilter = companyBapi.GetTable("IR_DATUM");
                    DateFilter.Append();
                    DateFilter.SetValue("SIGN", "I");
                    if (Filters.FromDate != Filters.ToDate)
                    {
                        DateFilter.SetValue("OPTION", "BT");
                        DateFilter.SetValue("LOW", Filters.FromDate.Date.ToString("yyyyMMdd"));
                        DateFilter.SetValue("HIGH", Filters.ToDate.Date.ToString("yyyyMMdd"));
                    }
                    else
                    {
                        DateFilter.SetValue("OPTION", "EQ");
                        DateFilter.SetValue("LOW", Filters.FromDate.Date.ToString("yyyyMMdd"));
                    }
                    companyBapi.SetValue("IR_DATUM", DateFilter);
                }

                companyBapi.Invoke(prd);

                for (int j = 1; j < Interfaces.Count; j++)
                {
                    Interface i = Interfaces[j];
                    i.detail = companyBapi.GetTable(i.SAP_Table);

                    Dictionary<string, string> columns = new Dictionary<string, string>();
                    if (i.SAP_Table == "GRUPTAB")
                    {
                        columns.Add("CompanyCode", "BUKRS");
                        columns.Add("GroupCode", "GRPNR");
                        columns.Add("ItemCode", "MATNR");
                        columns.Add("UOM", "MEINS");
                    }
                    else
                    {
                        columns = new Dictionary<string, string>();
                        columns.Add("CompanyCode", "BUKRS");
                        columns.Add("DivisionCode", "SPART");
                        columns.Add("SalesGroupCode", "KONDA");
                        columns.Add("CustomerShipTo", "KUNWE");
                        columns.Add("PromotionCode", "BBYNR");
                        columns.Add("ValidFrom", "DATAB");
                        columns.Add("ValidTo", "DATBI");
                        columns.Add("LineNumber", "POSNR");
                        columns.Add("BuyGroup", "BUYGP");
                        columns.Add("BuyQty", "QTY01");
                        columns.Add("GetGroup", "GETGP");
                        columns.Add("GetQty", "QTY02");
                        columns.Add("IsDeleted", "LOEVM");
                    }
                    i.columns = columns;

                    if (i.detail.Count > 0)
                    {
                        res = Result.Success;
                        rowsCount += i.detail.Count;
                    }
                    else
                    {
                        res = Result.NoRowsFound;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        string LastSavedFile = "";
        private void WriteFile(IntegrationField field, string MailBox, string FileName, string FileText)
        {
            try
            {
                if (!string.IsNullOrEmpty(MailBox))
                {
                    string path = CoreGeneral.Common.StartupPath + "\\FileBackup\\" + field.ToString() + "\\" + MailBox + "\\";

                    if (FileName.Contains(".txt"))
                        FileName = FileName.Substring(0, FileName.Length - 4);

                    string filePath = path + FileName + "_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".txt";
                    LastSavedFile = filePath;
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    File.AppendAllText(filePath, FileText);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private Result GetOutstandingTable(ref bool ClearAll)
        {
            Result res = Result.Failure;
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_OPEN_INVOICES");

                IRfcTable OrgFilter = companyBapi.GetTable("IR_BUKRS");
                OrgFilter.Append();
                OrgFilter.SetValue("SIGN", "I");
                OrgFilter.SetValue("OPTION", "EQ");
                OrgFilter.SetValue("LOW", OrganizationCode);
                companyBapi.SetValue("IR_BUKRS", OrgFilter);

                if (Filters.CustomerCode != string.Empty)
                {
                    ClearAll = false;
                    IRfcTable tblImport2 = companyBapi.GetTable("IR_KUNRG");
                    tblImport2.Append();
                    tblImport2.SetValue("SIGN", "I");
                    tblImport2.SetValue("OPTION", "EQ");
                    tblImport2.SetValue("LOW", new string('0', Math.Max(0, 10 - Filters.CustomerCode.ToString().Length)) + Filters.CustomerCode);
                    companyBapi.SetValue("IR_KUNRG", tblImport2);

                    String CustomerCheck = new string('0', Math.Max(0, 10 - Filters.CustomerCode.ToString().Length)) + Filters.CustomerCode;
                    WriteFile(IntegrationField.Customer_U,"Customer","Customer", CustomerCheck );
                }
                else
                {
                    incubeQuery = new InCubeQuery(db_vms, @"SELECT DISTINCT P.PayerCode FROM Payer P
INNER JOIN PayerAssignment PA ON PA.PayerID = P.PayerID
INNER JOIN Division D ON D.DivisionID = PA.DivisionID
WHERE D.OrganizationID = " + OrganizationID);
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
                            tblImport2.SetValue("LOW", new string('0', Math.Max(0, 10 - dtPayers.Rows[i]["PayerCode"].ToString().Length)) + dtPayers.Rows[i]["PayerCode"].ToString());
                            companyBapi.SetValue("IR_KUNRG", tblImport2);
                            string PayerCheck = new string('0', Math.Max(0, 10 - dtPayers.Rows[i]["PayerCode"].ToString().Length)) + dtPayers.Rows[i]["PayerCode"].ToString() + "--";
                            WriteFile(IntegrationField.Customer_U, "Payer", "Payer", PayerCheck);
                        }
                    }
                }
                
                if (Filters.OpenInvoicesOnly)
                {
                    companyBapi.SetValue("IV_INVTYP", "O");
                }
                else
                {
                    ClearAll = false;
                    companyBapi.SetValue("IV_INVTYP", "B");
                }

                companyBapi.SetValue("IV_PSIZE", "10000");

                if (Filters.FromDate != DateTime.MinValue && Filters.ToDate != DateTime.MaxValue)
                {
                    ClearAll = false;
                    IRfcTable DateFilter = companyBapi.GetTable("IR_DATE");
                    DateFilter.Append();
                    DateFilter.SetValue("SIGN", "I");
                    if (Filters.FromDate != Filters.ToDate)
                    {
                        DateFilter.SetValue("OPTION", "BT");
                        DateFilter.SetValue("LOW", Filters.FromDate.Date.ToString("yyyyMMdd"));
                        DateFilter.SetValue("HIGH", Filters.ToDate.Date.ToString("yyyyMMdd"));
                    }
                    else
                    {
                        DateFilter.SetValue("OPTION", "EQ");
                        DateFilter.SetValue("LOW", Filters.FromDate.Date.ToString("yyyyMMdd"));
                    }
                    companyBapi.SetValue("IR_DATE", DateFilter);
                }

                companyBapi.Invoke(prd);

                Interfaces[0].detail = companyBapi.GetTable(Interfaces[0].SAP_Table);

                Dictionary<string,string> columns = new Dictionary<string, string>();
                columns.Add("COMPANYCODE", "BUKRS");
                columns.Add("SoldTo", "KUNAG");
                columns.Add("ShipTo", "KUNWE");
                columns.Add("PayerCode", "KUNRG");
                columns.Add("TransactionNumber", "XBLNR");
                columns.Add("SAP_REF_NO", "VBELN");
                columns.Add("TotalAmount", "NETWR");
                columns.Add("RemainingAmount", "BLAMT");
                columns.Add("SalesmanCode", "PERNR");
                columns.Add("TransactionType", "VGART");
                columns.Add("DivisionCode", "SPART");
                columns.Add("InvoiceDate", "FKDAT");
                Interfaces[0].columns = columns;

                if (Interfaces[0].detail.Count > 0)
                {
                    res = Result.Success;
                    rowsCount = Interfaces[0].detail.Count;
                }
                else
                {
                    res = Result.NoRowsFound;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result GetMainWHStockTable()
        {
            Result res = Result.Failure;
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_WAREHOUSE_STOCK");

                IRfcTable OrgFilter = companyBapi.GetTable("IR_BUKRS");
                OrgFilter.Append();
                OrgFilter.SetValue("SIGN", "I");
                OrgFilter.SetValue("OPTION", "EQ");
                OrgFilter.SetValue("LOW", OrganizationCode);
                if (OrganizationID == 2)
                {
                    OrgFilter.Append();
                    OrgFilter.SetValue("SIGN", "I");
                    OrgFilter.SetValue("OPTION", "EQ");
                    OrgFilter.SetValue("LOW", "1600");

                }
                companyBapi.SetValue("IR_BUKRS", OrgFilter);

                incubeQuery = new InCubeQuery(db_vms, "SELECT WarehouseCode FROM Warehouse WHERE WarehouseTypeID = 1 AND OrganizationID IN (" + (OrganizationID == 2 ? "2,4" : OrganizationID.ToString()) + ")");
                incubeQuery.Execute();
                DataTable dtWarehouses = incubeQuery.GetDataTable();
                if (dtWarehouses.Rows.Count > 0)
                {
                    IRfcTable WarehouseFilter = companyBapi.GetTable("IR_WERKS");
                    foreach (DataRow dr in dtWarehouses.Rows)
                    {
                        WarehouseFilter.Append();
                        WarehouseFilter.SetValue("SIGN", "I");
                        WarehouseFilter.SetValue("OPTION", "EQ");
                        WarehouseFilter.SetValue("LOW", dr["WarehouseCode"].ToString());
                    }
                    companyBapi.SetValue("IR_WERKS", WarehouseFilter);
                }

                IRfcTable LocationFilter = companyBapi.GetTable("IR_LGORT");
                LocationFilter.Append();
                LocationFilter.SetValue("SIGN", "I");
                LocationFilter.SetValue("OPTION", "EQ");
                LocationFilter.SetValue("LOW", "0001");
                companyBapi.SetValue("IR_LGORT", LocationFilter);

                companyBapi.Invoke(prd);
                Interfaces[0].detail = companyBapi.GetTable(Interfaces[0].SAP_Table);

                Dictionary<string,string> columns = new Dictionary<string, string>();
                columns.Add("COMPANYCODE", "BUKRS");
                columns.Add("DivisionCode", "SPART");
                columns.Add("Plant", "WERKS");
                columns.Add("StorageLocation", "LGORT");
                columns.Add("ItemCode", "MATNR");
                columns.Add("ItemUOM", "MEINS");
                columns.Add("Denominator", "UMREN");
                columns.Add("Enumertaor", "UMREZ");
                columns.Add("Quantity", "LABST");
                columns.Add("CWQuantity", "/CWM/LABST");
                Interfaces[0].columns = columns;

                if (Interfaces[0].detail.Count > 0)
                {
                    res = Result.Success;
                    rowsCount = Interfaces[0].detail.Count;
                }
                else
                {
                    res = Result.NoRowsFound;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result GetOrderStatusTable()
        {
            Result res = Result.Failure;
            try
            {
                RfcDestination prd = RfcDestinationManager.GetDestination(SendServerName);
                RfcRepository repo = prd.Repository;
                IRfcFunction companyBapi = repo.CreateFunction("ZHOB_GET_PRESALES_ORDER_STATUS");

                IRfcTable OrgFilter = companyBapi.GetTable("IR_VKORG");
                OrgFilter.Append();
                OrgFilter.SetValue("SIGN", "I");
                OrgFilter.SetValue("OPTION", "EQ");
                OrgFilter.SetValue("LOW", OrganizationCode);
                companyBapi.SetValue("IR_VKORG", OrgFilter);

                incubeQuery = new InCubeQuery(db_vms, "SELECT DivisionCode FROM Division WHERE OrganizationID = " + OrganizationID);
                incubeQuery.Execute();
                DataTable dtDivisions = incubeQuery.GetDataTable();
                if (dtDivisions.Rows.Count > 0)
                {
                    IRfcTable DivisionFilter = companyBapi.GetTable("IR_SPART");
                    foreach (DataRow dr in dtDivisions.Rows)
                    {
                        DivisionFilter.Append();
                        DivisionFilter.SetValue("SIGN", "I");
                        DivisionFilter.SetValue("OPTION", "EQ");
                        DivisionFilter.SetValue("LOW", dr["DivisionCode"].ToString());
                    }
                    companyBapi.SetValue("IR_SPART", DivisionFilter);
                }

                if (Filters.FromDate != DateTime.MinValue && Filters.ToDate != DateTime.MaxValue)
                {
                    IRfcTable DateFilter = companyBapi.GetTable("IR_DATUM");
                    DateFilter.Append();
                    DateFilter.SetValue("SIGN", "I");
                    if (Filters.FromDate != Filters.ToDate)
                    {
                        DateFilter.SetValue("OPTION", "BT");
                        DateFilter.SetValue("LOW", Filters.FromDate.Date.ToString("yyyyMMdd"));
                        DateFilter.SetValue("HIGH", Filters.ToDate.Date.ToString("yyyyMMdd"));
                    }
                    else
                    {
                        DateFilter.SetValue("OPTION", "EQ");
                        DateFilter.SetValue("LOW", Filters.FromDate.Date.ToString("yyyyMMdd"));
                    }
                    companyBapi.SetValue("IR_DATUM", DateFilter);
                }
                
                IRfcTable OrderTypeFilter = companyBapi.GetTable("IR_AUART");
                OrderTypeFilter.Append();
                OrderTypeFilter.SetValue("SIGN", "I");
                OrderTypeFilter.SetValue("OPTION", "EQ");
                OrderTypeFilter.SetValue("LOW", "ZPR");
                companyBapi.SetValue("IR_AUART", OrderTypeFilter);
                
                companyBapi.SetValue("IV_PSIZE", "5000");
                
                companyBapi.Invoke(prd);

                Interfaces[0].detail = companyBapi.GetTable(Interfaces[0].SAP_Table);

                Dictionary<string, string> columns = new Dictionary<string, string>();
                columns.Add("CompanyCode", "BUKRS");
                columns.Add("SalesOrganization", "VKORG");
                columns.Add("DocumentNumber", "XBLNR");
                columns.Add("DivisionCode", "SPART");
                columns.Add("DistributionChannel", "VTWEG");
                columns.Add("DocumentType", "AUART");
                columns.Add("ShipToCode", "KUNWE");
                columns.Add("Plant", "WERKS");
                columns.Add("ItemCode", "MATNR");
                columns.Add("SalesUnit", "VRKME");
                columns.Add("OrderQuantity", "WMENG");
                columns.Add("ConfirmedQuantity", "BMENG");
                columns.Add("BilledQuantity", "FKIMG");
                columns.Add("BilledPrice", "NETPR");
                Interfaces[0].columns = columns;

                if (Interfaces[0].detail.Count > 0)
                {
                    res = Result.Success;
                    rowsCount = Interfaces[0].detail.Count;
                }
                else
                {
                    res = Result.NoRowsFound;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        
        public override void SendOrders()
        {
            try
            {
                Result result = Result.UnKnown;
                int processID = 0;
                string OrderID = "", OrderNo = "", CustomerID = "", OutletID = "", UpdateSynchronizedQuery = "";
                DataRow[] drDetails = null;
                string message = "";

                //Call procedure to prepare transaction details to send
                Procedure Proc = new Procedure("sp_PrepareOrdersToSend");
                Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID);
                Proc.AddParameter("@OrganizationID", ParamType.Integer, OrganizationID);
                Proc.AddParameter("@EmployeeID", ParamType.Integer, Filters.EmployeeID);
                Proc.AddParameter("@FromDate", ParamType.DateTime, Filters.FromDate);
                Proc.AddParameter("@ToDate", ParamType.DateTime, Filters.ToDate);
                Proc.AddParameter("@ResultID", ParamType.Integer, "0", ParamDirection.Output);
                result = ExecuteStoredProcedure(Proc);
                int ResultID = 0;
                int.TryParse(Proc.Parameters["@ResultID"].ParameterValue.ToString(), out ResultID);
                if (result != Result.Success || ResultID != 1)
                {
                    WriteMessage("Transactions preperation failed !!");
                    return;
                }

                //Read prepared details
                string DetailsQuery = string.Format(@"SELECT * FROM Stg_Orders WHERE TriggerID = {0}", TriggerID);
                incubeQuery = new InCubeQuery(db_vms, DetailsQuery);

                DataTable dtOrdersDetails = new DataTable();
                DataTable dtOrdersHeader = new DataTable();
                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    WriteMessage("Ordes query failed !!");
                    return;
                }
                else
                {
                    dtOrdersDetails = incubeQuery.GetDataTable();
                    if (dtOrdersDetails.Rows.Count == 0)
                    {
                        execManager.UpdateActionTotalRows(TriggerID, 0);
                        WriteMessage("There are no orders to send ..");
                        return;
                    }
                    else
                    {
                        //Extract distinct values table for header
                        dtOrdersHeader = dtOrdersDetails.DefaultView.ToTable(true, new string[] { "OrderID", "XBLNR", "CustomerID", "OutletID", "SPART" });
                        WriteMessage(dtOrdersHeader.Rows.Count + " order(s) found");
                        execManager.UpdateActionTotalRows(TriggerID, dtOrdersHeader.Rows.Count);
                        SetProgressMax(dtOrdersHeader.Rows.Count);
                    }
                }

                ClearProgress();
                SetProgressMax(dtOrdersHeader.Rows.Count);

                RfcDestination dest = RfcDestinationManager.GetDestination(SendServerName);
                IRfcFunction sendFunc;
                IRfcTable sendStruct;
                sendFunc = dest.Repository.CreateFunction("ZHIB_PROCESS_PRE_SALES");
                sendStruct = sendFunc.GetTable("ITEMTAB");

                for (int i = 0; i < dtOrdersHeader.Rows.Count; i++)
                {
                    try
                    {
                        //Read transaction primary key and log to execution details as well as to staging table
                        result = Result.UnKnown;
                        processID = 0;
                        message = "";
                        OrderID = dtOrdersHeader.Rows[i]["OrderID"].ToString();
                        OrderNo = dtOrdersHeader.Rows[i]["XBLNR"].ToString();
                        CustomerID = dtOrdersHeader.Rows[i]["CustomerID"].ToString();
                        OutletID = dtOrdersHeader.Rows[i]["OutletID"].ToString();
                        WriteMessage("\r\nSending order " + OrderNo + ": ");
                        ReportProgress();
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, OrderNo);
                        filters.Add(9, CustomerID + ":" + OutletID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        incubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE Stg_Orders SET ProcessID = {0} WHERE TriggerID = {1} AND XBLNR = '{2}' AND CustomerID = {3} AND OutletID = {4}", processID, TriggerID, OrderNo, CustomerID, OutletID));
                        incubeQuery.ExecuteNonQuery();
                        UpdateSynchronizedQuery = string.Format("UPDATE SalesOrderDetail SET AllItemDiscount = 1 WHERE OrderID = '{0}' AND CustomerID = {1} AND OutletID = {2} AND PackID = {3}", OrderID, CustomerID, OutletID, "{0}");

                        //Check last run for same transaction to avoid duplications
                        Result lastRes = Result.Blocked;
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                            incubeQuery.ExecuteNonQuery();
                            result = Result.Duplicate;
                            message = "Order already sent !!";
                            WriteMessage(message);
                            continue;
                        }

                        //Select details for current trasnaction
                        drDetails = dtOrdersDetails.Select(string.Format("XBLNR = '{0}' AND CustomerID = {1} AND OutletID = {2}", OrderNo, CustomerID, OutletID));
                        if (drDetails.Length == 0)
                        {
                            result = Result.NoRowsFound;
                            message = "No details returned !!";
                            WriteMessage(message);
                            continue;
                        }

                        sendStruct.Clear();
                        //Loop through details and fill objects
                        for (int j = 0; j < drDetails.Length; j++)
                        {
                            try
                            {
                                sendStruct.Append();
                                string[] columns = new string[] { "BUKRS", "VKORG", "SPART", "VTWEG", "PERNR", "AUART", "TRDAT", "LFDAT", "XBLNR", "HDRDC", "BSTNK", "KUNWE", "KUNAG", "KUNRG", "MATNR", "VRKME", "FKIMG", "WERKS", "POSNR", "ISFOC" };
                                foreach (string col in columns)
                                {
                                    sendStruct.SetValue(col, drDetails[j][col].ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                                message = "Failed in filling web service objects";
                                result = Result.Invalid;
                                break;
                            }
                        }

                        //Send only if no errors among collections for same ID
                        if (result == Result.UnKnown)
                        {
                            try
                            {
                                sendFunc.Invoke(dest);
                                result = Result.Success;
                                WriteMessage("Success");
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                                message = "Failed in filling web service objects";
                                result = Result.Failure;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        message = "Failure in preparing web service object!!";
                        result = Result.Failure;
                    }
                    finally
                    {
                        WriteMessage(message);
                        if (result == Result.Success)
                        {
                            foreach (DataRow dr in drDetails)
                            {
                                incubeQuery = new InCubeQuery(db_vms, string.Format(UpdateSynchronizedQuery, dr["PackID"]));
                                incubeQuery.ExecuteNonQuery();
                            }
                        }
                        execManager.LogIntegrationEnding(processID, result, "", message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public override void SendInvoices()
        {
            SendTransaction(Filters.EmployeeID == -1, Filters.EmployeeID.ToString(), Filters.FromDate, Filters.ToDate, IntegrationField.Sales_S);
        }
        public override void SendReturn()
        {
            SendTransaction(Filters.EmployeeID == -1, Filters.EmployeeID.ToString(), Filters.FromDate, Filters.ToDate, IntegrationField.Returns_S);
        }
        public override void SendOrderInvoices()
        {
            try
            {
                WriteMessage("\r\nSending processed PO ... ");

                Result result = Result.UnKnown;
                int processID = 0;
                string TransactionID = "", WarehouseID = "", UpdateSynchronizedQuery = "";
                string message = "";

                //Call procedure to prepare transaction details to send
                Procedure Proc = new Procedure("sp_PrepareExecutedLoadOrdersToSend");
                Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID);
                Proc.AddParameter("@OrganizationID", ParamType.Integer, OrganizationID);
                Proc.AddParameter("@FromDate", ParamType.DateTime, Filters.FromDate);
                Proc.AddParameter("@ToDate", ParamType.DateTime, Filters.ToDate);
                Proc.AddParameter("@ResultID", ParamType.Integer, "0", ParamDirection.Output);
                result = ExecuteStoredProcedure(Proc);
                int ResultID = 0;
                int.TryParse(Proc.Parameters["@ResultID"].ParameterValue.ToString(), out ResultID);
                if (result != Result.Success || ResultID != 1)
                {
                    WriteMessage("Transactions preperation failed !!");
                    return;
                }

                //Read prepared details
                string DetailsQuery = string.Format(@"SELECT * FROM Stg_ExecutedLoadOrders WHERE TriggerID = {0}", TriggerID);
                incubeQuery = new InCubeQuery(db_vms, DetailsQuery);

                DataTable dtTransactionsDetails = new DataTable();
                DataTable dtTransHeader = new DataTable();
                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    WriteMessage("Transactions query failed !!");
                    return;
                }
                else
                {
                    dtTransactionsDetails = incubeQuery.GetDataTable();
                    if (dtTransactionsDetails.Rows.Count == 0)
                    {
                        execManager.UpdateActionTotalRows(TriggerID, 0);
                        WriteMessage("There is no transactions to send ..");
                        return;
                    }
                    else
                    {
                        //Extract distinct values table for header
                        dtTransHeader = dtTransactionsDetails.DefaultView.ToTable(true, new string[] { "IHREZ", "WarehouseID" });
                        WriteMessage(dtTransHeader.Rows.Count + " transaction(s) found");
                        execManager.UpdateActionTotalRows(TriggerID, dtTransHeader.Rows.Count);
                        SetProgressMax(dtTransHeader.Rows.Count);
                    }
                }

                ClearProgress();
                SetProgressMax(dtTransHeader.Rows.Count);

                RfcDestination dest = RfcDestinationManager.GetDestination(SendServerName);
                IRfcFunction sendFunc;
                IRfcTable sendStruct;
                sendFunc = dest.Repository.CreateFunction("ZHIB_GOODS_RECEIVING");
                sendStruct = sendFunc.GetTable("ITEMTAB");

                for (int i = 0; i < dtTransHeader.Rows.Count; i++)
                {
                    try
                    {
                        //Read transaction primary key and log to execution details as well as to staging table
                        result = Result.UnKnown;
                        processID = 0;
                        message = "";
                        TransactionID = dtTransHeader.Rows[i]["IHREZ"].ToString();
                        WarehouseID = dtTransHeader.Rows[i]["WarehouseID"].ToString();
                        WriteMessage("\r\nSending transaction " + TransactionID + ": ");
                        ReportProgress();
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, TransactionID);
                        filters.Add(9, WarehouseID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        incubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE Stg_ExecutedLoadOrders SET ProcessID = {0} WHERE TriggerID = {1} AND IHREZ = '{2}' AND WarehouseID = {3}", processID, TriggerID, TransactionID, WarehouseID));
                        incubeQuery.ExecuteNonQuery();
                        UpdateSynchronizedQuery = string.Format("UPDATE WarehouseTransaction SET Synchronized = 1 WHERE TransactionID = '{0}' AND WarehouseID = {1}", TransactionID, WarehouseID);

                        //Check last run for same transaction to avoid duplications
                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.OrderInvoice_S, new List<string>(filters.Values), processID, 600);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                            incubeQuery.ExecuteNonQuery();
                            result = Result.Duplicate;
                            message = "Transaction already sent !!";
                            WriteMessage(message);
                            continue;
                        }

                        //Select details for current trasnaction
                        DataRow[] drDetails = dtTransactionsDetails.Select(string.Format("IHREZ = '{0}' AND WarehouseID = {1}", TransactionID, WarehouseID));
                        if (drDetails.Length == 0)
                        {
                            result = Result.NoRowsFound;
                            message = "No details returned !!";
                            WriteMessage(message);
                            continue;
                        }

                        sendStruct.Clear();
                        //Loop through details and fill objects
                        for (int j = 0; j < drDetails.Length; j++)
                        {
                            try
                            {
                                sendStruct.Append();
                                string[] columns = new string[] { "IHREZ", "BUKRS", "EBELN", "LIFNR", "RCODE", "TRDAT", "LFDAT", "EBELP", "MATNR", "MEINS", "MENGE", "CWQTY", "ZSTAT" };
                                foreach (string col in columns)
                                {
                                    sendStruct.SetValue(col, drDetails[j][col].ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                                message = "Failed in filling web service objects";
                                result = Result.Invalid;
                                break;
                            }
                        }

                        //Send only if no errors among collections for same ID
                        if (result == Result.UnKnown)
                        {
                            try
                            {
                                sendFunc.Invoke(dest);
                                result = Result.Success;
                                WriteMessage("Success");
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                                message = "Failed in filling web service objects";
                                result = Result.Failure;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        message = "Failure in preparing web service object!!";
                        result = Result.Failure;
                    }
                    finally
                    {
                        WriteMessage(message);
                        if (result == Result.Success)
                        {
                            incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                            incubeQuery.ExecuteNonQuery();
                        }
                        execManager.LogIntegrationEnding(processID, result, "", message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public override void SendTransfers()
        {
            try
            {
                WriteMessage("\r\nSending warehouse transactions ... ");
            
                Result result = Result.UnKnown;
                int processID = 0;
                string TransactionID = "", WarehouseID = "", UpdateSynchronizedQuery = "";
                string message = "";

                //Call procedure to prepare transaction details to send
                Procedure Proc = new Procedure("sp_PrepareWarehouseTransactionsToSend");
                Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID);
                Proc.AddParameter("@OrganizationID", ParamType.Integer, OrganizationID);
                Proc.AddParameter("@FromDate", ParamType.DateTime, Filters.FromDate);
                Proc.AddParameter("@ToDate", ParamType.DateTime, Filters.ToDate);
                Proc.AddParameter("@ResultID", ParamType.Integer, "0", ParamDirection.Output);
                result = ExecuteStoredProcedure(Proc);
                int ResultID = 0;
                int.TryParse(Proc.Parameters["@ResultID"].ParameterValue.ToString(), out ResultID);
                if (result != Result.Success || ResultID != 1)
                {
                    WriteMessage("Transactions preperation failed !!");
                    return;
                }

                //Read prepared details
                string DetailsQuery = string.Format(@"SELECT * FROM Stg_WHTransactions WHERE TriggerID = {0}", TriggerID);
                incubeQuery = new InCubeQuery(db_vms, DetailsQuery);

                DataTable dtTransactionsDetails = new DataTable();
                DataTable dtTransHeader = new DataTable();
                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    WriteMessage("Transactions query failed !!");
                    return;
                }
                else
                {
                    dtTransactionsDetails = incubeQuery.GetDataTable();
                    if (dtTransactionsDetails.Rows.Count == 0)
                    {
                        execManager.UpdateActionTotalRows(TriggerID, 0);
                        WriteMessage("There is no transactions to send ..");
                        return;
                    }
                    else
                    {
                        //Extract distinct values table for header
                        dtTransHeader = dtTransactionsDetails.DefaultView.ToTable(true, new string[] { "IHREZ", "WarehouseID" });
                        WriteMessage(dtTransHeader.Rows.Count + " transaction(s) found");
                        execManager.UpdateActionTotalRows(TriggerID, dtTransHeader.Rows.Count);
                        SetProgressMax(dtTransHeader.Rows.Count);
                    }
                }

                ClearProgress();
                SetProgressMax(dtTransHeader.Rows.Count);

                RfcDestination dest = RfcDestinationManager.GetDestination(SendServerName);
                IRfcFunction sendFunc;
                IRfcTable sendStruct;
                sendFunc = dest.Repository.CreateFunction("ZHIB_PROCESS_LOAD_REQUEST");
                sendStruct = sendFunc.GetTable("ITEMTAB");

                for (int i = 0; i < dtTransHeader.Rows.Count; i++)
                {
                    try
                    {
                        //Read transaction primary key and log to execution details as well as to staging table
                        result = Result.UnKnown;
                        processID = 0;
                        message = "";
                        TransactionID = dtTransHeader.Rows[i]["IHREZ"].ToString();
                        WarehouseID = dtTransHeader.Rows[i]["WarehouseID"].ToString();
                        WriteMessage("\r\nSending transaction " + TransactionID + ": ");
                        ReportProgress();
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, TransactionID);
                        filters.Add(9, WarehouseID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        incubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE Stg_WHTransactions SET ProcessID = {0} WHERE TriggerID = {1} AND IHREZ = '{2}' AND WarehouseID = {3}", processID, TriggerID, TransactionID, WarehouseID));
                        incubeQuery.ExecuteNonQuery();
                        UpdateSynchronizedQuery = string.Format("UPDATE WarehouseTransaction SET Synchronized = 1 WHERE TransactionID = '{0}' AND WarehouseID = {1}", TransactionID, WarehouseID);

                        //Check last run for same transaction to avoid duplications
                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Transfers_S, new List<string>(filters.Values), processID, 600);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                            incubeQuery.ExecuteNonQuery();
                            result = Result.Duplicate;
                            message = "Transaction already sent !!";
                            WriteMessage(message);
                            continue;
                        }

                        //Select details for current trasnaction
                        DataRow[] drDetails = dtTransactionsDetails.Select(string.Format("IHREZ = '{0}' AND WarehouseID = {1}", TransactionID, WarehouseID));
                        if (drDetails.Length == 0)
                        {
                            result = Result.NoRowsFound;
                            message = "No details returned !!";
                            WriteMessage(message);
                            continue;
                        }

                        sendStruct.Clear();
                        //Loop through details and fill objects
                        for (int j = 0; j < drDetails.Length; j++)
                        {
                            try
                            {
                                sendStruct.Append();
                                string[] columns = new string[] { "IHREZ", "BUKRS", "VKORG", "RESWK", "RCODE", "TRDAT", "VGART", "LFDAT", "EBELP", "MATNR", "MENGE", "MEINS", "SPART", "ISLIQ", "ZSTAT", "SGTXT" };
                                foreach (string col in columns)
                                {
                                    sendStruct.SetValue(col, drDetails[j][col].ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                                message = "Failed in filling web service objects";
                                result = Result.Invalid;
                                break;
                            }
                        }

                        //Send only if no errors among collections for same ID
                        if (result == Result.UnKnown)
                        {
                            try
                            {
                                sendFunc.Invoke(dest);
                                result = Result.Success;
                                WriteMessage("Success");
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                                message = "Failed in filling web service objects";
                                result = Result.Failure;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        message = "Failure in preparing web service object!!";
                        result = Result.Failure;
                    }
                    finally
                    {
                        WriteMessage(message);
                        if (result == Result.Success)
                        {
                            incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                            incubeQuery.ExecuteNonQuery();
                        }
                        execManager.LogIntegrationEnding(processID, result, "", message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void SendTransaction(bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate, IntegrationField field)
        {
            try
            {
                string TransactionType = "";
                if (field == IntegrationField.Sales_S)
                {
                    WriteMessage("\r\nSending invocies ... ");
                    TransactionType = "1,3";
                }
                else if (field == IntegrationField.Returns_S)
                {
                    WriteMessage("\r\nSending returns ... ");
                    TransactionType = "2,4";
                }
                else if (field == IntegrationField.CreditNoteRequest_S)
                {
                    WriteMessage("\r\nSending credit notes ... ");
                    TransactionType = "5";
                }

                    Result result = Result.UnKnown;
                int processID = 0, ACTYP = 0;
                string TransactionID = "", CustomerID = "", OutletID = "", DivisionID = "", UpdateSynchronizedQuery = "", InsertVoidedSyncQuery = "";
                string message = "";

                //Call procedure to prepare transaction details to send
                Procedure Proc = new Procedure("sp_PrepareTransactionsToSend");
                Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID);
                Proc.AddParameter("@OrganizationID", ParamType.Integer, OrganizationID);
                Proc.AddParameter("@EmployeeID", ParamType.Integer, AllSalespersons ? "-1" : Salesperson);
                Proc.AddParameter("@FromDate", ParamType.DateTime, FromDate);
                Proc.AddParameter("@ToDate", ParamType.DateTime, ToDate);
                Proc.AddParameter("@TransactionType", ParamType.Nvarchar, TransactionType);
                Proc.AddParameter("@ResultID", ParamType.Integer, "0", ParamDirection.Output);
                result = ExecuteStoredProcedure(Proc);
                int ResultID = 0;
                int.TryParse(Proc.Parameters["@ResultID"].ParameterValue.ToString(), out ResultID);
                if (result != Result.Success || ResultID != 1)
                {
                    WriteMessage("Transactions preperation failed !!");
                    return;
                }

                //Read prepared details
                string DetailsQuery = string.Format(@"SELECT * FROM Stg_Transactions WHERE TriggerID = {0}", TriggerID);
                incubeQuery = new InCubeQuery(db_vms, DetailsQuery);

                DataTable dtTransactionsDetails = new DataTable();
                DataTable dtTransHeader = new DataTable();
                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    WriteMessage("Transactions query failed !!");
                    return;
                }
                else
                {
                    dtTransactionsDetails = incubeQuery.GetDataTable();
                    if (dtTransactionsDetails.Rows.Count == 0)
                    {
                        WriteMessage("There is no transactions to send ..");
                        execManager.UpdateActionTotalRows(TriggerID, 0);
                        return;
                    }
                    else
                    {
                        //Extract distinct values table for header
                        dtTransHeader = dtTransactionsDetails.DefaultView.ToTable(true, new string[] { "XBLNR", "CustomerID", "OutletID", "DivisionID", "ACTYP" });
                        WriteMessage(dtTransHeader.Rows.Count + " transaction(s) found");
                        execManager.UpdateActionTotalRows(TriggerID, dtTransHeader.Rows.Count);
                        SetProgressMax(dtTransHeader.Rows.Count);
                    }
                }
                                
                ClearProgress();
                SetProgressMax(dtTransHeader.Rows.Count);

                RfcDestination dest = RfcDestinationManager.GetDestination(SendServerName);
                IRfcFunction sendFunc;
                IRfcTable sendStruct;
                sendFunc = dest.Repository.CreateFunction("ZHIB_PROCESS_SALES_INVOICE");
                sendStruct = sendFunc.GetTable("ITEMTAB");

                for (int i = 0; i < dtTransHeader.Rows.Count; i++)
                {
                    try
                    {
                        //Read transaction primary key and log to execution details as well as to staging table
                        result = Result.UnKnown;
                        processID = 0;
                        message = "";
                        TransactionID = dtTransHeader.Rows[i]["XBLNR"].ToString();
                        CustomerID = dtTransHeader.Rows[i]["CustomerID"].ToString();
                        OutletID = dtTransHeader.Rows[i]["OutletID"].ToString();
                        DivisionID = dtTransHeader.Rows[i]["DivisionID"].ToString();
                        ACTYP = Convert.ToInt16(dtTransHeader.Rows[i]["ACTYP"]);
                        WriteMessage("\r\nSending transaction " + TransactionID + ": ");
                        ReportProgress();
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, TransactionID);
                        filters.Add(9, CustomerID + ":" + OutletID + ":" + DivisionID);
                        filters.Add(10, ACTYP.ToString());
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        incubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE Stg_Transactions SET ProcessID = {0} WHERE TriggerID = {1} AND XBLNR = '{2}' AND CustomerID = {3} AND OutletID = {4} AND DivisionID = {5} AND ACTYP = {6}", processID, TriggerID, TransactionID, CustomerID, OutletID, DivisionID, ACTYP));
                        incubeQuery.ExecuteNonQuery();
                        UpdateSynchronizedQuery = string.Format("UPDATE [Transaction] SET Synchronized = 1 WHERE TransactionID = '{0}' AND CustomerID = {1} AND OutletID = {2} AND DivisionID = {3}", TransactionID, CustomerID, OutletID, DivisionID);
                        InsertVoidedSyncQuery = string.Format("INSERT INTO VoidedTransactionsSync (TransactionID,CustomerID,OutletID,DivisionID,SyncDate,ProcessID) VALUES('{0}',{1},{2},{3},GETDATE(),{4})", TransactionID, CustomerID, OutletID, DivisionID, processID);
                        //Check last run for same transaction to avoid duplications
                        Result lastRes = GetLastExecutionResultForEntry(field, new List<string>(filters.Values), processID, 600);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                            incubeQuery.ExecuteNonQuery();
                            result = Result.Duplicate;
                            message = "Transaction already sent !!";
                            WriteMessage(message);
                            continue;
                        }

                        //Select details for current trasnaction
                        DataRow[] drDetails = dtTransactionsDetails.Select(string.Format("XBLNR = '{0}' AND CustomerID = {1} AND OutletID = {2} AND DivisionID = {3}", TransactionID, CustomerID, OutletID, DivisionID));
                        if (drDetails.Length == 0)
                        {
                            result = Result.NoRowsFound;
                            message = "No details returned !!";
                            WriteMessage(message);
                            continue;
                        }

                        sendStruct.Clear();
                        //Loop through details and fill objects
                        for (int j = 0; j < drDetails.Length; j++)
                        {
                            try
                            {
                                sendStruct.Append();
                                string[] columns = new string[] { "XBLNR", "BUKRS", "VKORG", "SPART", "VTWEG", "PERNR", "AUART", "HDRDC", "BSTNK", "AUGRU", "TRDAT", "KUNAG", "KUNWE", "KUNRG", "RCODE", "POSNR", "MATNR", "VRKME", "FKIMG", "CWQTY", "NETPR", "NETWR", "ISFOC", "ITMDC", "ACTYP", "WAERS" };
                                foreach (string col in columns)
                                {
                                    sendStruct.SetValue(col, drDetails[j][col].ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                                message = "Failed in filling web service objects";
                                result = Result.Invalid;
                                break;
                            }
                        }

                        //Send only if no errors among collections for same ID
                        if (result == Result.UnKnown)
                        {
                            try
                            {
                                sendFunc.Invoke(dest);
                                result = Result.Success;
                                WriteMessage("Success");
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                                message = "Failed in filling web service objects";
                                result = Result.Failure;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        message = "Failure in preparing web service object!!";
                        result = Result.Failure;
                    }
                    finally
                    {
                        WriteMessage(message);
                        if (result == Result.Success)
                        {
                            incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                            incubeQuery.ExecuteNonQuery();
                            if (ACTYP == 2)
                            {
                                incubeQuery = new InCubeQuery(db_vms, InsertVoidedSyncQuery);
                                incubeQuery.ExecuteNonQuery();
                            }
                        }
                        execManager.LogIntegrationEnding(processID, result, "", message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public override void SendReciepts()
        {
            try
            {
                WriteMessage("\r\nSending collections ... ");

                Result result = Result.UnKnown;
                int processID = 0;
                string AppliedPaymentID = "", CustomerID = "", OutletID = "", DivisionID = "", UpdateSynchronizedQuery = "";
                string message = "";

                //Call procedure to prepare transaction details to send
                Procedure Proc = new Procedure("sp_PreparePaymentsToSend");
                Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID);
                Proc.AddParameter("@OrganizationID", ParamType.Integer, OrganizationID);
                Proc.AddParameter("@EmployeeID", ParamType.Integer, Filters.EmployeeID);
                Proc.AddParameter("@FromDate", ParamType.DateTime, Filters.FromDate);
                Proc.AddParameter("@ToDate", ParamType.DateTime, Filters.ToDate);
                Proc.AddParameter("@ResultID", ParamType.Integer, "0", ParamDirection.Output);
                result = ExecuteStoredProcedure(Proc);
                int ResultID = 0;
                int.TryParse(Proc.Parameters["@ResultID"].ParameterValue.ToString(), out ResultID);
                if (result != Result.Success || ResultID != 1)
                {
                    WriteMessage("Collections preperation failed !!");
                    return;
                }

                //Read prepared details
                string DetailsQuery = string.Format(@"SELECT * FROM Stg_Payments WHERE TriggerID = {0}", TriggerID);
                incubeQuery = new InCubeQuery(db_vms, DetailsQuery);

                DataTable dtCollections = new DataTable();
                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    WriteMessage("Collections query failed !!");
                    return;
                }
                else
                {
                    dtCollections = incubeQuery.GetDataTable();
                    if (dtCollections.Rows.Count == 0)
                    {
                        execManager.UpdateActionTotalRows(TriggerID, 0);
                        WriteMessage("There is no collections to send ..");
                        return;
                    }
                }

                ClearProgress();
                SetProgressMax(dtCollections.Rows.Count);

                RfcDestination dest = RfcDestinationManager.GetDestination(SendServerName);
                IRfcFunction sendFunc;
                IRfcTable sendStruct;
                sendFunc = dest.Repository.CreateFunction("ZHIB_PAYMENT_COLLECTION");
                sendStruct = sendFunc.GetTable("ITEMTAB");

                for (int i = 0; i < dtCollections.Rows.Count; i++)
                {
                    try
                    {
                        //Read transaction primary key and log to execution details as well as to staging table
                        result = Result.UnKnown;
                        processID = 0;
                        message = "";
                        AppliedPaymentID = dtCollections.Rows[i]["AppliedPaymentID"].ToString();
                        CustomerID = dtCollections.Rows[i]["CustomerID"].ToString();
                        OutletID = dtCollections.Rows[i]["OutletID"].ToString();
                        DivisionID = dtCollections.Rows[i]["DivisionID"].ToString();
                        WriteMessage("\r\nSending transaction " + AppliedPaymentID + ": ");
                        ReportProgress();
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, AppliedPaymentID);
                        filters.Add(9, CustomerID + ":" + OutletID + ":" + DivisionID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        incubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE Stg_Payments SET ProcessID = {0} WHERE TriggerID = {1} AND AppliedPaymentID = '{2}' AND CustomerID = {3} AND OutletID = {4} AND DivisionID = {5}", processID, TriggerID, AppliedPaymentID, CustomerID, OutletID, DivisionID));
                        incubeQuery.ExecuteNonQuery();
                        UpdateSynchronizedQuery = string.Format("UPDATE CustomerPayment SET Synchronized = 1 WHERE AppliedPaymentID = '{0}' AND CustomerID = {1} AND OutletID = {2} AND DivisionID = {3}", AppliedPaymentID, CustomerID, OutletID, DivisionID);

                        //Check last run for same transaction to avoid duplications
                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Reciept_S, new List<string>(filters.Values), processID, 600);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                            incubeQuery.ExecuteNonQuery();
                            result = Result.Duplicate;
                            message = "Payment already sent !!";
                            continue;
                        }

                        sendStruct.Clear();
                        sendStruct.Append();
                        //Loop through details and fill objects
                        string[] columns = new string[] { "BUKRS", "SPART", "KUNAG", "KUNWE", "KUNRG", "KIDNO", "HHINV", "PAYDT", "NEBTR", "NETWR", "XANET", "PMTYP", "CSTYP", "CHKNM", "BANKA", "CHKDT", "SGTXT", "RCODE", "PERNR", "HHRET", "POSNR" };
                        foreach (string col in columns)
                        {
                            sendStruct.SetValue(col, dtCollections.Rows[i][col].ToString());
                        }
                        sendFunc.Invoke(dest);
                        message = "success";
                        result = Result.Success;
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        message = ex.Message;
                        result = Result.Failure;
                    }
                    finally
                    {
                        WriteMessage(message);
                        if (result == Result.Success)
                        {
                            incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                            incubeQuery.ExecuteNonQuery();
                        }
                        execManager.LogIntegrationEnding(processID, result, "", message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
    }
}