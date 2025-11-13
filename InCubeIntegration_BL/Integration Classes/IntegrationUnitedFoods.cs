using InCubeIntegration_DAL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Xml;

namespace InCubeIntegration_BL
{
    public class IntegrationUnitedFoods : IntegrationBase // Live branch
    {
        BackgroundWorker bgwCheckProgress;
        InCubeDatabase db_res;
        SqlCommand cmd;
        InCubeQuery incubeQuery = null;
        Dictionary<string, string> Tables = new Dictionary<string, string>();
        string MasterDirectory = "";
        string MasterName = "";
        IntegrationField currentField = IntegrationField.ATM_S;

        public IntegrationUnitedFoods(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            try
            {
                if (CoreGeneral.Common.CurrentSession.loginType != LoginType.WindowsService)
                {
                    db_res = new InCubeDatabase();
                    db_res.Open("InCube", "IntegrationUnitedFoods");
                }

                bgwCheckProgress = new BackgroundWorker();
                bgwCheckProgress.DoWork += new DoWorkEventHandler(bgw_CheckProgress);
                bgwCheckProgress.WorkerSupportsCancellation = true;

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(CoreGeneral.Common.StartupPath + "\\DataSources.xml");
                MasterDirectory = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'MasterDirectory']/Data").InnerText;
            }
            catch (Exception ex)
            {
                Initialized = false;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void bgw_CheckProgress(object sender, DoWorkEventArgs e)
        {
            try
            {
                while (TriggerID != -1)
                {
                    int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
                    if (currentField == IntegrationField.Stock_U)
                    {
                        GetExecutionResults(TriggerID, ref TotalRows, ref Inserted, ref Updated, ref Skipped, db_res);
                    }
                    else
                    {
                        GetExecutionResults(new List<string>(Tables.Values), ref TotalRows, ref Inserted, ref Updated, ref Skipped, db_res);
                    }
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
            Result res = Result.UnKnown;
            currentField = IntegrationField.Stock_U;
            try
            {
                DataTable dtData = new DataTable();
                string FileName = MasterDirectory + "\\" + "STOCK_MOVEMENT.csv";
                WriteMessage("\r\nRetrieving data from flat file [" + FileName + "] .. ");
                if (File.Exists(FileName))
                {
                    string[] FileContents = File.ReadAllText(FileName).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    int ColumnsCount = 0;
                    if (FileContents.Length > 0)
                    {
                        for (int i = 0; i < FileContents.Length; i++)
                        {
                            string[] lineContents = FileContents[i].Split(new char[] { '^' });
                            if (i == 0)
                            {
                                ColumnsCount = lineContents.Length;
                                for (int j = 0; j < lineContents.Length; j++)
                                    dtData.Columns.Add(lineContents[j]);
                            }
                            else
                            {
                                DataRow dr = dtData.NewRow();
                                for (int j = 0; j < Math.Min(lineContents.Length, ColumnsCount); j++)
                                {
                                    dr[j] = lineContents[j];
                                }
                                dtData.Rows.Add(dr);
                            }
                        }

                        if (dtData.Rows.Count == 0)
                        {
                            WriteMessage("No data found !!");
                        }
                        else
                        {
                            WriteMessage("Rows retrieved: " + dtData.Rows.Count);
                        }

                        WriteMessage("\r\nSaving data to staging table ... ");
                        res = SaveToStockTables(dtData);
                        if (res == Result.Success)
                        {
                            WriteMessage("\r\nLooping through pending Stock Movements transactions ...");

                            if (CoreGeneral.Common.CurrentSession.loginType != LoginType.WindowsService)
                                bgwCheckProgress.RunWorkerAsync();

                            cmd = new SqlCommand("sp_UpdateStock", db_vms.GetConnection());
                            SqlParameter SQLparam = new SqlParameter("@TriggerID", SqlDbType.Int);
                            SQLparam.Value = TriggerID;
                            cmd.Parameters.Add(SQLparam);
                            cmd.CommandTimeout = 3600000;
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.ExecuteNonQuery();

                            res = Result.Success;
                            WriteMessage("\r\nStock Movements updated ...");
                        }
                    }
                    else
                    {
                        WriteMessage("File is blank !!");
                        res = Result.Failure;
                    }
                }
                else
                {
                    WriteMessage("File doesn't exist !!");
                    res = Result.Failure;
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
                System.Threading.Thread.Sleep(550);
                if (res == Result.Success)
                {
                    int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
                    GetExecutionResults(TriggerID, ref TotalRows, ref Inserted, ref Updated, ref Skipped, db_res);
                    WriteMessage("\r\nTotal rows found: " + TotalRows + ", Inserted: " + Inserted + ", Updated: " + Updated + ", Skipped: " + Skipped);
                    WriteMessage("\r\n=========================================================\r\n=========================================================\r\n\r\n");
                }
                TriggerID = -1;
            }
        }
        private Result SaveToStockTables(DataTable dtStock)
        {
            Result res = Result.Success;
            int NewTrans = 0;

            try
            {
                string TransactionID = string.Empty;
                dtStock.DefaultView.Sort = "TransactionNumber ASC";
                dtStock = dtStock.DefaultView.ToTable();

                string deleteUnprocessed = string.Format(@"Delete From Stg_StockMovementsHeader Where IsProcessed = 0 Delete From Stg_StockMovementsDetails Where TransactionNumber Not In (Select TransactionNumber From Stg_StockMovementsHeader)");
                incubeQuery = new InCubeQuery(deleteUnprocessed, db_vms);
                if (incubeQuery.ExecuteNonQuery() != InCubeErrors.Success)
                {
                    res = Result.Failure;
                    return res;
                }

                for (int i = 0; i < dtStock.Rows.Count; i++)
                {
                    if (TransactionID != dtStock.Rows[i]["TransactionNumber"].ToString())
                    {
                        //insert header if not exists
                        TransactionID = dtStock.Rows[i]["TransactionNumber"].ToString();
                        if (GetFieldValue("Stg_StockMovementsHeader", "TransactionNumber", string.Format("TransactionNumber = '{0}'", TransactionID), db_vms) == string.Empty)
                        {
                            NewTrans++;
                            string transDateStr = dtStock.Rows[i]["TransactionDate"].ToString();
                            DateTime transactionDate = new DateTime(int.Parse(transDateStr.Substring(6, 4)), int.Parse(transDateStr.Substring(3, 2)), int.Parse(transDateStr.Substring(0, 2)));
                            string insertHeader = string.Format(@"INSERT INTO Stg_StockMovementsHeader 
(TransactionNumber,REF_NO,TransactionType,WarehouseCode,VehicleCode,TransactionDate,IsProcessed) 
VALUES ('{0}','{1}',{2},'{3}','{4}','{5}',0)", TransactionID, dtStock.Rows[i]["REF_NO"].ToString(), int.Parse(dtStock.Rows[i]["TransactionType"].ToString())
                                                 , dtStock.Rows[i]["WarehouseCode"].ToString(), dtStock.Rows[i]["VehicleCode"].ToString()
                                                 , transactionDate.ToString("yyyy-MM-dd"));
                            incubeQuery = new InCubeQuery(insertHeader, db_vms);
                            if (incubeQuery.ExecuteNonQuery() != InCubeErrors.Success)
                            {
                                res = Result.Failure;
                                break;
                            }
                        }
                    }

                    int ItemCode = int.Parse(dtStock.Rows[i]["ItemCode"].ToString());
                    string UOM = dtStock.Rows[i]["UOM"].ToString();

                    if (GetFieldValue("Stg_StockMovementsHeader", "TransactionNumber", string.Format("TransactionNumber = '{0}' AND IsProcessed = 1", TransactionID), db_vms) == string.Empty)
                    {
                        if (GetFieldValue("Stg_StockMovementsDetails", "TransactionNumber", string.Format("TransactionNumber = '{0}' AND ItemCode = '{1}' AND UOM = '{2}'", TransactionID, ItemCode, UOM), db_vms) == string.Empty)
                        {
                            string insertDetails = string.Format(@"INSERT INTO Stg_StockMovementsDetails 
(TransactionNumber,ItemCode,UOM,Quantity) 
VALUES ('{0}','{1}','{2}',{3})", TransactionID, ItemCode, UOM, decimal.Parse(dtStock.Rows[i]["Quantity"].ToString().TrimStart().TrimEnd()));
                            incubeQuery = new InCubeQuery(insertDetails, db_vms);
                            if (incubeQuery.ExecuteNonQuery() != InCubeErrors.Success)
                            {
                                res = Result.Failure;
                                break;
                            }
                        }
                        else
                        {
                            string updateDetails = string.Format(@"UPDATE Stg_StockMovementsDetails Set Quantity = Quantity + {3}
                         Where TransactionNumber = '{0}' And ItemCode = '{1}' And UOM ='{2}' 
                        ", TransactionID, ItemCode, UOM, decimal.Parse(dtStock.Rows[i]["Quantity"].ToString().TrimStart().TrimEnd()));
                            incubeQuery = new InCubeQuery(updateDetails, db_vms);
                            if (incubeQuery.ExecuteNonQuery() != InCubeErrors.Success)
                            {
                                res = Result.Failure;
                                break;

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            finally
            {
                if (res == Result.Success && NewTrans > 0)
                {
                    WriteMessage("Success .. " + NewTrans + " new transaction(s) found..");
                }
                else if (res == Result.Success && NewTrans == 0)
                {
                    WriteMessage("Success .. No new transactions provided ..");
                }
                else
                {
                    WriteMessage("Error in saving to staging tables !!");
                }
            }
            return res;
        }
        private void GetMasterData(IntegrationField field)
        {
            Result res = Result.UnKnown;
            try
            {
                currentField = field;
                MasterName = field.ToString().Substring(0, field.ToString().Length - 2);
                List<string> Procs = new List<string>();
                Tables = new Dictionary<string, string>();

                switch (field)
                {
                    case IntegrationField.Item_U:
                        Tables.Add("INVAN_ITEMS", "Stg_Items");
                        Procs.Add("sp_UpdateItems");
                        break;
                    case IntegrationField.Price_U:
                        Tables.Add("INVAN_Customer_Prices", "Stg_CustomerPrice");
                        //Tables.Add("INVAN_Customer_Prices1", "Stg_CustomerPrice");
                        //Tables.Add("INVAN_Customer_Prices2", "Stg_CustomerPrice");
                        Procs.Add("sp_UpdatePrices");
                        break;
                    case IntegrationField.Customer_U:
                        Tables.Add("INVAN_CUSTOMERS", "Stg_Customers");
                        Procs.Add("sp_UpdateCustomers");
                        break;
                    case IntegrationField.Outstanding_U:
                        Tables.Add("INVAN_CUSTOMER OUTSTANDING", "Stg_Outstanding");
                        Procs.Add("sp_AddColumnsToOutstanding");
                        Procs.Add("sp_UpdateOutstanding");
                        break;

                }

                res = GetMasterTablesFromCSV(Tables);

                if (res == Result.Success)
                {
                    WriteMessage("\r\nLooping through " + MasterName + " ...");

                    if (CoreGeneral.Common.CurrentSession.loginType != LoginType.WindowsService)
                        bgwCheckProgress.RunWorkerAsync();

                    for (int k = 0; k < Procs.Count; k++)
                    {
                        cmd = new SqlCommand(Procs[k], db_vms.GetConnection());
                        cmd.CommandTimeout = 3600000;
                        cmd.ExecuteNonQuery();
                    }

                    res = Result.Success;
                    WriteMessage("\r\n" + MasterName + " updated ...");
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
                TriggerID = -1;
                System.Threading.Thread.Sleep(550);
                if (res == Result.Success)
                {
                    int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
                    GetExecutionResults(new List<string>(Tables.Values), ref TotalRows, ref Inserted, ref Updated, ref Skipped, db_res);
                    WriteMessage("\r\nTotal rows found: " + TotalRows + ", Inserted: " + Inserted + ", Updated: " + Updated + ", Skipped: " + Skipped);
                    WriteMessage("\r\n=========================================================\r\n=========================================================\r\n\r\n");
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
        private Result GetMasterTablesFromCSV(Dictionary<string, string> Tables)
        {
            Result res = Result.Success;
            foreach (KeyValuePair<string, string> pair in Tables)
            {
                try
                {
                    if (res == Result.Failure)
                        return res;

                    DataTable dtData = new DataTable();
                    string FileName = MasterDirectory + "\\" + pair.Key + ".csv";
                    string TableName = pair.Value;
                    WriteMessage("\r\nRetrieving data from flat file [" + FileName + "] .. ");
                    if (File.Exists(FileName))
                    {
                        BackupMasterFile(FileName);
                        string[] FileContents = File.ReadAllText(FileName).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        int ColumnsCount = 0;
                        if (FileContents.Length > 0)
                        {
                            for (int i = 0; i < FileContents.Length; i++)
                            {
                                string[] lineContents = FileContents[i].Split(new char[] { '^' });
                                if (i == 0)
                                {
                                    ColumnsCount = lineContents.Length;
                                    for (int j = 0; j < lineContents.Length; j++)
                                        dtData.Columns.Add(lineContents[j]);
                                }
                                else
                                {
                                    DataRow dr = dtData.NewRow();
                                    for (int j = 0; j < Math.Min(lineContents.Length, ColumnsCount); j++)
                                    {
                                        dr[j] = lineContents[j];
                                    }
                                    dtData.Rows.Add(dr);
                                }
                            }

                            if (dtData.Rows.Count == 0)
                            {
                                WriteMessage("No data found !!");
                                res = Result.NoRowsFound;
                            }
                            else
                            {
                                WriteMessage("Rows retrieved: " + dtData.Rows.Count);
                            }

                            execManager.UpdateActionTotalRows(TriggerID, dtData.Rows.Count);
                            WriteMessage("\r\nSaving data to staging table ... ");
                            res = SaveTable(dtData, TableName);
                            if (res != Result.Success)
                            {
                                WriteMessage("Error in saving to staging table !!");
                            }
                            else
                            {
                                WriteMessage("Success ..");
                            }
                        }
                        else
                        {
                            WriteMessage("File is blank !!");
                            res = Result.Failure;
                        }
                    }
                    else
                    {
                        WriteMessage("File doesn't exist !!");
                        res = Result.Failure;
                    }
                }
                catch (Exception ex)
                {
                    WriteMessage("Unhandeled exception !!");
                    res = Result.Failure;
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                }
            }
            return res;
        }
        private string BackupMasterFile(string FilePath)
        {
            string SavingPath = "";
            try
            {
                SavingPath = CoreGeneral.Common.StartupPath + "\\FileBackup\\" + MasterName;
                if (!Directory.Exists(SavingPath))
                {
                    Directory.CreateDirectory(SavingPath);
                }
                SavingPath = SavingPath + "\\" + TriggerID + "-" + Path.GetFileName(FilePath);
                File.Copy(FilePath, SavingPath);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return SavingPath;
        }
        //        public override void SendOrders(bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate)
        //        {
        //            try
        //            {
        //                Result result = Result.UnKnown;
        //                string salespersonFilter = "";
        //                int processID = 0;
        //                string OrderID = "";
        //                string message = "";

        //                if (!AllSalespersons)
        //                {
        //                    salespersonFilter = "AND SO.EmployeeID = " + Salesperson;
        //                }
        //                string OrderHeaderQuery = string.Format(@"SELECT SO.OrderID, C.CustomerCode, CO.CustomerCode [OutletCode], SO.OrderDate, SO.DesiredDeliveryDate, E.EmployeeCode,
        //W.WarehouseCode, SO.LPO, SO.NetTotal, SO.Discount
        //FROM SalesOrder SO
        //INNER JOIN Customer C ON C.CustomerID = SO.CustomerID
        //INNER JOIN CustomerOutlet CO ON CO.CustomerID = SO.CustomerID AND CO.OutletID = SO.OutletID
        //INNER JOIN Employee E ON E.EmployeeID = SO.EmployeeID
        //INNER JOIN EmployeeVehicle EV ON EV.EmployeeID = SO.EmployeeID
        //INNER JOIN VehicleLoadingWh VLW ON VLW.VehicleID = EV.VehicleID
        //INNER JOIN Warehouse W ON W.WarehouseID = VLW.WarehouseID
        //WHERE SO.Synchronized = 0 AND SO.OrderStatusID <> 9 AND SO.OrderDate > '{0}' AND SO.OrderDate < '{1}' {2}"
        //                    , FromDate.ToString("yyyy-MM-dd"), ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
        //                incubeQuery = new InCubeQuery(db_vms, OrderHeaderQuery);

        //                if (incubeQuery.Execute() != InCubeErrors.Success)
        //                    throw (new Exception("Orders header query failed !!"));

        //                DataTable dtOrders = incubeQuery.GetDataTable();
        //                if (dtOrders.Rows.Count == 0)
        //                    WriteMessage("There is no orders to send ..");
        //                else
        //                    SetProgressMax(dtOrders.Rows.Count);

        //                UFC_SalesOrder_WS.ZSD_INVAN_SO_TO_INVOICE_NClient cli = new UFC_SalesOrder_WS.ZSD_INVAN_SO_TO_INVOICE_NClient();
        //                UFC_SalesOrder_WS.ZSD_INVAN_SO_TO_INVOICE_POST so = new UFC_SalesOrder_WS.ZSD_INVAN_SO_TO_INVOICE_POST();
        //                UFC_SalesOrder_WS.ZINVAN_INVOICE_HEADER header = new UFC_SalesOrder_WS.ZINVAN_INVOICE_HEADER();
        //                UFC_SalesOrder_WS.ZINVAN_INVOICE_ITEMS item = new UFC_SalesOrder_WS.ZINVAN_INVOICE_ITEMS();

        //                for (int i = 0; i < dtOrders.Rows.Count; i++)
        //                {
        //                    try
        //                    {
        //                        processID = 0;
        //                        message = "";

        //                        OrderID = dtOrders.Rows[i]["OrderID"].ToString();
        //                        ReportProgress("Sending order: " + OrderID);
        //                        WriteMessage("\r\n" + OrderID + ": ");
        //                        Dictionary<int, string> filters = new Dictionary<int, string>();
        //                        filters.Add(8, OrderID);
        //                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

        //                        cli = new UFC_SalesOrder_WS.ZSD_INVAN_SO_TO_INVOICE_NClient();
        //                        so = new UFC_SalesOrder_WS.ZSD_INVAN_SO_TO_INVOICE_POST();
        //                        header = new UFC_SalesOrder_WS.ZINVAN_INVOICE_HEADER();
        //                        so.ET_HEAD = new UFC_SalesOrder_WS.ZINVAN_INVOICE_HEADER[1];

        //                        header.Ordernumber = dtOrders.Rows[i]["OrderID"].ToString();
        //                        header.Billtocode = dtOrders.Rows[i]["CustomerCode"].ToString();
        //                        header.Shiptocode = dtOrders.Rows[i]["OutletCode"].ToString();
        //                        header.Orderdate = DateTime.Parse(dtOrders.Rows[i]["OrderDate"].ToString()).ToString("dd.MM.yyyy");
        //                        header.Deliverydate = DateTime.Parse(dtOrders.Rows[i]["DesiredDeliveryDate"].ToString()).ToString("dd.MM.yyyy");
        //                        header.Salesmancode = dtOrders.Rows[i]["EmployeeCode"].ToString();
        //                        header.Locationcode = dtOrders.Rows[i]["WarehouseCode"].ToString();
        //                        header.Lponumber = dtOrders.Rows[i]["LPO"].ToString();
        //                        header.Orderamount = Decimal.Parse(dtOrders.Rows[i]["NetTotal"].ToString()).ToString("#0.00");
        //                        header.Headerdiscount = Decimal.Parse(dtOrders.Rows[i]["Discount"].ToString()).ToString("#0.00");

        //                        so.EtSoHeader[0] = header;

        //                        string OrderDetails = string.Format(@"SELECT I.ItemCode, PTL.Description UOM, SOD.Quantity, SOD.Price, SOD.Discount,
        //CASE SOD.SalesTransactionTypeID WHEN 1 THEN 0 ELSE 1 END FOCIndicator,
        //(SOD.Quantity * SOD.Price) - SOD.Discount TotalLineAmount
        //FROM SalesOrderDetail SOD
        //INNER JOIN Pack P ON P.PackID = SOD.PackID
        //INNER JOIN Item I on I.ItemID = P.ItemID
        //INNER JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID
        //WHERE SOD.OrderID = '{0}'", OrderID);

        //                        incubeQuery = new InCubeQuery(db_vms, OrderDetails);
        //                        if (incubeQuery.Execute() != InCubeErrors.Success)
        //                            throw (new Exception("Order details query failed !!"));

        //                        DataTable dtDetails = incubeQuery.GetDataTable();

        //                        so.EtSoItem = new UFC_SalesOrder_WS.ZinvanSoItem[dtDetails.Rows.Count];

        //                        for (int j = 0; j < dtDetails.Rows.Count; j++)
        //                        {
        //                            item = new UFC_SalesOrder_WS.ZinvanSoItem();
        //                            item.Ordernumber = header.Ordernumber;
        //                            item.Billtocode = header.Billtocode;
        //                            item.Shiptocode = header.Shiptocode;
        //                            item.Linenumber = (j + 1).ToString();
        //                            item.Itemcode = dtDetails.Rows[j]["ItemCode"].ToString();
        //                            item.Uom = dtDetails.Rows[j]["UOM"].ToString();
        //                            item.Quantity = Decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString("#0.00");
        //                            item.Price = Decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString("#0.00");
        //                            item.Itemdiscount = Decimal.Parse(dtDetails.Rows[j]["Discount"].ToString()).ToString("#0.00");
        //                            item.Focindicator = dtDetails.Rows[j]["FOCIndicator"].ToString();
        //                            item.Totallineamount = Decimal.Parse(dtDetails.Rows[j]["TotalLineAmount"].ToString()).ToString("#0.00");
        //                            so.EtSoItem[j] = item;
        //                        }

        //                        UFC_SalesOrder_WS.ZsdInvanSalesOrderCrWservResponse res = cli.ZsdInvanSalesOrderCrWserv(so);
        //                        if (res.Return != null && res.Return.Length > 0)
        //                        {
        //                            message = res.Return[0].Message;
        //                            WriteMessage(message);
        //                            if (res.Return[0].Ordernumber == "")
        //                            {
        //                                result = Result.Failure;
        //                                WriteMessage(", No Order Number Returned ..");
        //                            }
        //                            else 
        //                            {
        //                                result = Result.Success;
        //                                WriteMessage(", Order Created .." + res.Return[0].Ordernumber);
        //                                message = res.Return[0].Ordernumber + " " + message;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            WriteMessage("Error ..");
        //                            result = Result.Failure;
        //                        }

        //                        if (result == Result.Success)
        //                        {
        //                            incubeQuery = new InCubeQuery(db_vms, "UPDATE SalesOrder SET Synchronized = 1 WHERE OrderID = '" + OrderID + "'");
        //                            incubeQuery.ExecuteNonQuery();
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
        //                        message = ex.Message;
        //                        result = Result.Failure;
        //                        WriteMessage("Failure !!");
        //                    }
        //                    finally
        //                    {
        //                        execManager.LogIntegrationEnding(processID, result, "", message);
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
        //            }
        //        }

        public override void SendInvoices()
        {
            SendTransaction(Filters.EmployeeID == -1, Filters.EmployeeID.ToString(), Filters.FromDate, Filters.ToDate, IntegrationField.Sales_S);
        }
        public override void SendReturn()
        {
            SendTransaction(Filters.EmployeeID == -1, Filters.EmployeeID.ToString(), Filters.FromDate, Filters.ToDate, IntegrationField.Returns_S);
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
                else
                {
                    WriteMessage("\r\nSending returns ... ");
                    TransactionType = "2,4";
                }

                Result result = Result.UnKnown;
                int processID = 0;
                string TransactionID = "", CustomerID = "", OutletID = "", UpdateSynchronizedQuery = "";
                string message = "";

                //Call procedure to prepare transaction details to send
                Procedure Proc = new Procedure("sp_PrepareTransactionsToSend");
                Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID);
                Proc.AddParameter("@EmployeeID", ParamType.Integer, AllSalespersons ? "-1" : Salesperson);
                Proc.AddParameter("@FromDate", ParamType.DateTime, FromDate);
                Proc.AddParameter("@ToDate", ParamType.DateTime, ToDate);
                Proc.AddParameter("@TransactionType", ParamType.Nvarchar, TransactionType);
                result = ExecuteStoredProcedure(Proc);

                if (result != Result.Success)
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
                        return;
                    }
                    else
                    {
                        //Extract distinct values table for header
                        dtTransHeader = dtTransactionsDetails.DefaultView.ToTable(true, new string[] { "Trnum", "CustomerID", "OutletID", "Bcode", "Scode", "Blart", "Tdate", "Smcode", "Lcode", "Lpono", "Inamt", "Hdisc", "Reamt", "Rtrnu" });
                        WriteMessage(dtTransHeader.Rows.Count + " transaction(s) found");
                        execManager.UpdateActionTotalRows(TriggerID, dtTransHeader.Rows.Count);
                        SetProgressMax(dtTransHeader.Rows.Count);
                    }
                }

                UFC_SalesReturns_WS.ZSD_Invan_so_to_invoice_FGClient cli;
                cli = new UFC_SalesReturns_WS.ZSD_Invan_so_to_invoice_FGClient();
                cli.ClientCredentials.UserName.UserName = CoreGeneral.Common.GeneralConfigurations.WS_UserName;
                cli.ClientCredentials.UserName.Password = CoreGeneral.Common.GeneralConfigurations.WS_Password;
                UFC_SalesReturns_WS.ZsdInvanSoToInvoicePost trans;
                UFC_SalesReturns_WS.ZinvanInvoiceHeader header;
                UFC_SalesReturns_WS.ZinvanInvoiceItems item;
                UFC_SalesReturns_WS.ZsdInvanSoToInvoicePostResponse res;


                for (int i = 0; i < dtTransHeader.Rows.Count; i++)
                {
                    try
                    {
                        //Read transaction primary key and log to execution details as well as to staging table
                        result = Result.UnKnown;
                        processID = 0;
                        message = "";
                        TransactionID = dtTransHeader.Rows[i]["Trnum"].ToString();
                        CustomerID = dtTransHeader.Rows[i]["CustomerID"].ToString();
                        OutletID = dtTransHeader.Rows[i]["OutletID"].ToString();
                        WriteMessage("\r\nSending transaction " + TransactionID + ": ");
                        ReportProgress();
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, TransactionID);
                        filters.Add(9, CustomerID);
                        filters.Add(10, OutletID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        incubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE Stg_Transactions SET ProcessID = {0} WHERE TriggerID = {1} AND Trnum = '{2}' AND CustomerID = {3} AND OutletID = {4}", processID, TriggerID, TransactionID, CustomerID, OutletID));
                        incubeQuery.ExecuteNonQuery();
                        UpdateSynchronizedQuery = string.Format("UPDATE [Transaction] SET Synchronized = 1 WHERE TransactionID = '{0}' AND CustomerID = {1} AND OutletID = {2}", TransactionID, CustomerID, OutletID);

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

                        trans = new UFC_SalesReturns_WS.ZsdInvanSoToInvoicePost();
                        header = new UFC_SalesReturns_WS.ZinvanInvoiceHeader();
                        trans.EtHead = new UFC_SalesReturns_WS.ZinvanInvoiceHeader[1];
                        res = new UFC_SalesReturns_WS.ZsdInvanSoToInvoicePostResponse();

                        //Fill header object
                        header.Trnum = dtTransHeader.Rows[i]["Trnum"].ToString();
                        header.Bcode = dtTransHeader.Rows[i]["Bcode"].ToString();
                        header.Scode = dtTransHeader.Rows[i]["Scode"].ToString();
                        header.Blart = Convert.ToByte(dtTransHeader.Rows[i]["Blart"]);
                        header.Tdate = dtTransHeader.Rows[i]["Tdate"].ToString();
                        header.Smcode = dtTransHeader.Rows[i]["Smcode"].ToString();
                        header.Lcode = dtTransHeader.Rows[i]["Lcode"].ToString();
                        header.Lpono = dtTransHeader.Rows[i]["Lpono"].ToString();
                        header.Inamt = Convert.ToDecimal(dtTransHeader.Rows[i]["Inamt"]);
                        header.Hdisc = Convert.ToDecimal(dtTransHeader.Rows[i]["Hdisc"]);
                        header.Reamt = Convert.ToDecimal(dtTransHeader.Rows[i]["Reamt"]);
                        header.Rtrnu = dtTransHeader.Rows[i]["Rtrnu"].ToString();

                        trans.EtHead[0] = header;

                        //Select details for current trasnaction
                        DataRow[] drDetails = dtTransactionsDetails.Select(string.Format("Trnum = '{0}' AND CustomerID = {1} AND OutletID = {2}", TransactionID, CustomerID, OutletID));
                        if (drDetails.Length == 0)
                        {
                            result = Result.NoRowsFound;
                            message = "No details returned !!";
                            WriteMessage(message);
                            continue;
                        }

                        trans.EtItem = new UFC_SalesReturns_WS.ZinvanInvoiceItems[drDetails.Length];
                        //Loop through details and fill objects
                        for (int j = 0; j < drDetails.Length; j++)
                        {
                            try
                            {
                                item = new UFC_SalesReturns_WS.ZinvanInvoiceItems();
                                item.Trnum = drDetails[j]["Trnum"].ToString();
                                item.Bcode = drDetails[j]["Bcode"].ToString();
                                item.Scode = drDetails[j]["Scode"].ToString();
                                item.Lnumb = Convert.ToByte(drDetails[j]["Lnumb"]);
                                item.Icode = drDetails[j]["Icode"].ToString();
                                item.Uom = drDetails[j]["Uom"].ToString();
                                item.Istatus = Convert.ToByte(drDetails[j]["Istatus"]);
                                item.Sqty = Convert.ToDecimal(drDetails[j]["Sqty"]);
                                item.Rqty = Convert.ToDecimal(drDetails[j]["Rqty"]);
                                item.Focqty = Convert.ToDecimal(drDetails[j]["Focqty"]);
                                item.Freeqty = Convert.ToDecimal(drDetails[j]["Freeqty"]);
                                item.Price = Convert.ToDecimal(drDetails[j]["Price"]);
                                item.Idisc = Convert.ToDecimal(drDetails[j]["Idisc"]);
                                item.Ttlamt = Convert.ToDecimal(drDetails[j]["Ttlamt"]);
                                trans.EtItem[j] = item;
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
                                res = cli.ZsdInvanSoToInvoicePost(trans);
                                if (res != null && res.EtError != null && res.EtError.Length > 0)
                                {
                                    foreach (UFC_SalesReturns_WS.ZsdInvoiceErr status in res.EtError)
                                    {
                                        QueryBuilder qry = new QueryBuilder();
                                        qry.SetField("TriggerID", TriggerID.ToString());
                                        qry.SetField("ProcessID", processID.ToString());
                                        qry.SetStringField("VBELN", status.Vbeln);
                                        qry.SetStringField("MSGTY", status.Msgty);
                                        qry.SetStringField("MESSAGE", status.Message);
                                        qry.InsertQueryString("Stg_TransactionResponse", db_vms);
                                    }
                                    message = res.EtError[0].Message + " (" + res.EtError[0].Vbeln + ")";
                                    if (res.EtError[0].Msgty.ToLower() == "s")
                                        result = Result.Success;
                                    else
                                        result = Result.Invalid;
                                }
                                else
                                {
                                    message = "No result from service";
                                    result = Result.NoFileRetreived;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                                message = "Error in calling service (" + ex.Message + ")";
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
        public override void SendOrders()
        {
            try
            {
                Result result = Result.UnKnown;
                int processID = 0;
                string OrderID = "", CustomerID = "", OutletID = "", UpdateSynchronizedQuery = "";
                string message = "";

                //Call procedure to prepare transaction details to send
                Procedure Proc = new Procedure("sp_PrepareOrdersToSend");
                Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID);
                Proc.AddParameter("@EmployeeID", ParamType.Integer, Filters.EmployeeID);
                Proc.AddParameter("@FromDate", ParamType.DateTime, Filters.FromDate);
                Proc.AddParameter("@ToDate", ParamType.DateTime, Filters.ToDate);
                result = ExecuteStoredProcedure(Proc);

                if (result != Result.Success)
                {
                    WriteMessage("Orders preperation failed !!");
                    return;
                }

                //Read prepared details
                string DetailsQuery = string.Format(@"SELECT * FROM Stg_Orders WHERE TriggerID = {0}", TriggerID);
                incubeQuery = new InCubeQuery(db_vms, DetailsQuery);

                DataTable dtOrdersDetails = new DataTable();
                DataTable dtOrderHeader = new DataTable();
                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    WriteMessage("Orders query failed !!");
                    return;
                }
                else
                {
                    dtOrdersDetails = incubeQuery.GetDataTable();
                    if (dtOrdersDetails.Rows.Count == 0)
                    {
                        WriteMessage("There is no orders to send ..");
                        return;
                    }
                    else
                    {
                        //Extract distinct values table for header
                        dtOrderHeader = dtOrdersDetails.DefaultView.ToTable(true, new string[] { "Ordernumber", "CustomerID", "OutletID", "Billtocode", "Shiptocode", "Orderdate", "Deliverydate", "Headerdiscount", "Isprocessed", "Locationcode", "Lponumber", "Orderamount", "Routecode", "Routekey", "Salesmancode" });
                        WriteMessage(dtOrderHeader.Rows.Count + " order(s) found");
                        execManager.UpdateActionTotalRows(TriggerID, dtOrderHeader.Rows.Count);
                        SetProgressMax(dtOrderHeader.Rows.Count);
                    }
                }

                UFC_SalesOrder_WS.ZUFC_INVAN_SALE_ORD_POSTClient cli;
                cli = new UFC_SalesOrder_WS.ZUFC_INVAN_SALE_ORD_POSTClient();
                cli.ClientCredentials.UserName.UserName = CoreGeneral.Common.GeneralConfigurations.WS_UserName;
                cli.ClientCredentials.UserName.Password = CoreGeneral.Common.GeneralConfigurations.WS_Password;
                UFC_SalesOrder_WS.ZsdInvanSalesOrderCrWserv trans;
                UFC_SalesOrder_WS.ZinvanSoHeader header;
                UFC_SalesOrder_WS.ZinvanSoItem item;
                UFC_SalesOrder_WS.ZsdInvanSalesOrderCrWservResponse res;

                for (int i = 0; i < dtOrderHeader.Rows.Count; i++)
                {
                    try
                    {
                        result = Result.UnKnown;
                        processID = 0;
                        message = "";
                        OrderID = dtOrderHeader.Rows[i]["Ordernumber"].ToString();
                        CustomerID = dtOrderHeader.Rows[i]["CustomerID"].ToString();
                        OutletID = dtOrderHeader.Rows[i]["OutletID"].ToString();
                        WriteMessage("\r\nSending order " + OrderID + ": ");
                        ReportProgress();
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, OrderID);
                        filters.Add(9, CustomerID);
                        filters.Add(10, OutletID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        incubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE Stg_Orders SET ProcessID = {0} WHERE TriggerID = {1} AND Ordernumber = '{2}' AND CustomerID = {3} AND OutletID = {4}", processID, TriggerID, OrderID, CustomerID, OutletID));
                        incubeQuery.ExecuteNonQuery();
                        UpdateSynchronizedQuery = string.Format("UPDATE SalesOrder SET Synchronized = 1 WHERE OrderID = '{0}' AND CustomerID = {1} AND OutletID = {2}", OrderID, CustomerID, OutletID);

                        //Check last run for same transaction to avoid duplications
                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Orders_S, new List<string>(filters.Values), processID, 600);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                            incubeQuery.ExecuteNonQuery();
                            result = Result.Duplicate;
                            message = "Order already sent !!";
                            WriteMessage(message);
                            continue;
                        }

                        trans = new UFC_SalesOrder_WS.ZsdInvanSalesOrderCrWserv();
                        header = new UFC_SalesOrder_WS.ZinvanSoHeader();
                        trans.EtSoHeader = new UFC_SalesOrder_WS.ZinvanSoHeader[1];
                        res = new UFC_SalesOrder_WS.ZsdInvanSalesOrderCrWservResponse();

                        //Fill header object
                        header.Ordernumber = dtOrderHeader.Rows[i]["Ordernumber"].ToString();
                        header.Billtocode = dtOrderHeader.Rows[i]["Billtocode"].ToString();
                        header.Shiptocode = dtOrderHeader.Rows[i]["Shiptocode"].ToString();
                        header.Orderdate = dtOrderHeader.Rows[i]["Orderdate"].ToString();
                        header.Deliverydate = dtOrderHeader.Rows[i]["Deliverydate"].ToString();
                        header.Headerdiscount = dtOrderHeader.Rows[i]["Headerdiscount"].ToString();
                        header.Isprocessed = dtOrderHeader.Rows[i]["Isprocessed"].ToString();
                        header.Locationcode = dtOrderHeader.Rows[i]["Locationcode"].ToString();
                        header.Lponumber = dtOrderHeader.Rows[i]["Lponumber"].ToString();
                        header.Orderamount = dtOrderHeader.Rows[i]["Orderamount"].ToString();
                        header.Routecode = dtOrderHeader.Rows[i]["Routecode"].ToString();
                        header.Routekey = dtOrderHeader.Rows[i]["Routekey"].ToString();
                        header.Salesmancode = dtOrderHeader.Rows[i]["Salesmancode"].ToString();

                        trans.EtSoHeader[0] = header;

                        //Select details for current trasnaction
                        DataRow[] drDetails = dtOrdersDetails.Select(string.Format("Ordernumber = '{0}' AND CustomerID = {1} AND OutletID = {2}", OrderID, CustomerID, OutletID));
                        if (drDetails.Length == 0)
                        {
                            result = Result.NoRowsFound;
                            message = "No details returned !!";
                            WriteMessage(message);
                            continue;
                        }

                        trans.EtSoItem = new UFC_SalesOrder_WS.ZinvanSoItem[drDetails.Length];
                        //Loop through details and fill objects
                        for (int j = 0; j < drDetails.Length; j++)
                        {
                            try
                            {
                                item = new UFC_SalesOrder_WS.ZinvanSoItem();
                                item.Ordernumber = drDetails[j]["Ordernumber"].ToString();
                                item.Billtocode = drDetails[j]["Billtocode"].ToString();
                                item.Shiptocode = drDetails[j]["Shiptocode"].ToString();
                                item.Linenumber = drDetails[j]["Linenumber"].ToString();
                                item.Itemcode = drDetails[j]["Itemcode"].ToString();
                                item.Uom = drDetails[j]["Uom"].ToString();
                                item.Price = drDetails[j]["Price"].ToString();
                                item.Quantity = drDetails[j]["Quantity"].ToString();
                                item.Focindicator = drDetails[j]["Focindicator"].ToString();
                                item.Itemdiscount = drDetails[j]["Itemdiscount"].ToString();
                                item.Totallineamount = drDetails[j]["Totallineamount"].ToString();

                                trans.EtSoItem[j] = item;
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
                                res = cli.ZsdInvanSalesOrderCrWserv(trans);
                                if (res != null && res.Return != null && res.Return.Length > 0)
                                {
                                    foreach (UFC_SalesOrder_WS.Zreturn status in res.Return)
                                    {
                                        QueryBuilder qry = new QueryBuilder();
                                        qry.SetField("TriggerID", TriggerID.ToString());
                                        qry.SetField("ProcessID", processID.ToString());
                                        qry.SetField("CustomerID", CustomerID.ToString());
                                        qry.SetField("OutletID", OutletID.ToString());
                                        qry.SetStringField("Ordernumber", status.Ordernumber);
                                        qry.SetStringField("Type", status.Type);
                                        qry.SetStringField("Message", status.Message);
                                        qry.InsertQueryString("Stg_OrderResponse", db_vms);
                                    }
                                    message = res.Return[0].Message + " (" + res.Return[0].Ordernumber + ")";
                                    if (res.Return[0].Type.ToLower() == "s")
                                        result = Result.Success;
                                    else
                                        result = Result.Invalid;
                                }
                                else
                                {
                                    message = "No result from service";
                                    result = Result.NoFileRetreived;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                                message = "Error in calling service (" + ex.Message + ")";
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
        private void SendPendingLoadRequest()
        {
            try
            {
                WriteMessage("\r\nSending load requests:\r\n");

                Result result = Result.UnKnown;
                int processID = 0;
                string TransactionID = "", warehouseID = "";
                string message = "";

                //Call procedure to prepare transaction details to send
                Procedure Proc = new Procedure("sp_PrepareWarehouseTransactionsToSend");
                Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID);
                Proc.AddParameter("@EmployeeID", ParamType.Integer, Filters.EmployeeID);
                Proc.AddParameter("@FromDate", ParamType.DateTime, Filters.FromDate);
                Proc.AddParameter("@ToDate", ParamType.DateTime, Filters.ToDate);
                result = ExecuteStoredProcedure(Proc);

                if (result != Result.Success)
                {
                    WriteMessage("Requests preperation failed !!");
                    return;
                }

                //Read prepared details
                string DetailsQuery = string.Format(@"SELECT * FROM Stg_WarehouseTransactions WHERE TriggerID = {0}", TriggerID);
                incubeQuery = new InCubeQuery(db_vms, DetailsQuery);

                DataTable dtTransactionsDetails = new DataTable();
                DataTable dtTransHeader = new DataTable();
                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    WriteMessage("Requests query failed !!");
                    return;
                }
                else
                {
                    dtTransactionsDetails = incubeQuery.GetDataTable();
                    if (dtTransactionsDetails.Rows.Count == 0)
                    {
                        WriteMessage("There is no requests to send ..");
                        return;
                    }
                    else
                    {
                        //Extract distinct values table for header
                        dtTransHeader = dtTransactionsDetails.DefaultView.ToTable(true, new string[] { "Trnum", "WarehouseID", "LGORT", "TR_DATE", "TR_STAT", "TR_TYPE", "VEH_NO" });
                        WriteMessage(dtTransHeader.Rows.Count + " transaction(s) found");
                        execManager.UpdateActionTotalRows(TriggerID, dtTransHeader.Rows.Count);
                        SetProgressMax(dtTransHeader.Rows.Count);
                    }
                }

                UFC_LoadRequest_WS.ZMM_LOAD_REQ_POST_RET_WSClient cli;
                cli = new UFC_LoadRequest_WS.ZMM_LOAD_REQ_POST_RET_WSClient();
                cli.ClientCredentials.UserName.UserName = CoreGeneral.Common.GeneralConfigurations.WS_UserName;
                cli.ClientCredentials.UserName.Password = CoreGeneral.Common.GeneralConfigurations.WS_Password;
                UFC_LoadRequest_WS.ZMM_INV_LOAD_REQ_WS trans;
                UFC_LoadRequest_WS.ZMM_INV_LOAD header;
                UFC_LoadRequest_WS.ZMM_INV_LOAD_ITEM item;

                for (int i = 0; i < dtTransHeader.Rows.Count; i++)
                {
                    try
                    {
                        //Read transaction primary key and log to execution details as well as to staging table
                        result = Result.UnKnown;
                        processID = 0;
                        message = "";
                        TransactionID = dtTransHeader.Rows[i]["TRNUM"].ToString();
                        warehouseID = dtTransHeader.Rows[i]["WarehouseID"].ToString();
                        WriteMessage("\r\nSending WH trans " + TransactionID + ": ");
                        ReportProgress();
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(1, "Load Request");
                        filters.Add(8, TransactionID);
                        filters.Add(9, warehouseID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        incubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE Stg_WarehouseTransactions SET ProcessID = {0} WHERE TriggerID = {1} AND Trnum = '{2}' AND WarehouseID = {3}", processID, TriggerID, TransactionID, warehouseID));
                        incubeQuery.ExecuteNonQuery();

                        //Check last run for same transaction to avoid duplications
                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Transfers_S, new List<string>(filters.Values), processID, 600);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            result = Result.Duplicate;
                            message = "Transaction already sent !!";
                            WriteMessage(": Transaction already sent !!");
                            continue;
                        }

                        trans = new UFC_LoadRequest_WS.ZMM_INV_LOAD_REQ_WS();
                        header = new UFC_LoadRequest_WS.ZMM_INV_LOAD();

                        //Fill header object
                        header.TRNUM = dtTransHeader.Rows[i]["TRNUM"].ToString();
                        header.LGORT = dtTransHeader.Rows[i]["LGORT"].ToString();
                        header.TR_DATE = dtTransHeader.Rows[i]["TR_DATE"].ToString();
                        header.TR_STAT = dtTransHeader.Rows[i]["TR_STAT"].ToString();
                        header.TR_TYPE = dtTransHeader.Rows[i]["TR_TYPE"].ToString();
                        header.VEH_NO = dtTransHeader.Rows[i]["VEH_NO"].ToString();
                        trans.ET_HEAD = header;

                        //Select details for current trasnaction
                        DataRow[] drDetails = dtTransactionsDetails.Select(string.Format("TRNUM = '{0}' AND WarehouseID = {1}", TransactionID, warehouseID));
                        if (drDetails.Length == 0)
                        {
                            result = Result.NoRowsFound;
                            message = "No details returned !!";
                            WriteMessage(": No details returned !!");
                            continue;
                        }

                        trans.ET_ITEM = new UFC_LoadRequest_WS.ZMM_INV_LOAD_ITEM[drDetails.Length];
                        //Loop through details and fill objects
                        for (int j = 0; j < drDetails.Length; j++)
                        {
                            item = new UFC_LoadRequest_WS.ZMM_INV_LOAD_ITEM();
                            item.ICODE = drDetails[j]["ICODE"].ToString();
                            item.LNUMB = Convert.ToByte(drDetails[j]["LNUMB"]);
                            item.QTY = Convert.ToDecimal(drDetails[j]["QTY"]);
                            item.TRNUM = drDetails[j]["TRNUM"].ToString();
                            item.UOM = drDetails[j]["UOM"].ToString();

                            trans.ET_ITEM[j] = item;
                        }

                        UFC_LoadRequest_WS.ZMM_INV_LOAD_REQ_WSResponse res = cli.ZMM_INV_LOAD_REQ_WS(trans);
                        UFC_LoadRequest_WS.ZMM_INV_LOAD_RETURN[] results = res.ET_ERROR;

                        if (results != null && results.Length > 0)
                        {
                            result = Result.Invalid;
                            foreach (UFC_LoadRequest_WS.ZMM_INV_LOAD_RETURN rtn in results)
                            {
                                QueryBuilder qry = new QueryBuilder();
                                qry.SetField("TriggerID", TriggerID.ToString());
                                qry.SetField("ProcessID", processID.ToString());
                                qry.SetStringField("TransactionID", TransactionID);
                                qry.SetField("WarehouseID", warehouseID);
                                qry.SetStringField("ICODE", rtn.ICODE);
                                qry.SetStringField("UOM", rtn.UOM);
                                qry.SetStringField("MAT_DOC", rtn.MAT_DOC);
                                qry.SetStringField("MESSAGE", rtn.MESSAGE);
                                qry.SetStringField("MSGTY", rtn.MSGTY);
                                qry.SetField("QTY", rtn.QTY.ToString());
                                qry.InsertQueryString("Stg_LoadRequestResponse", db_vms);
                                message = rtn.MESSAGE;

                                if (rtn.MSGTY.ToLower() == "s")
                                    result = Result.Success;
                            }
                        }
                        else
                        {
                            WriteMessage(": No result from service");
                            message = "No result from service";
                            result = Result.NoFileRetreived;
                        }

                        if (result == Result.Success)
                        {
                            Proc = new Procedure("sp_ApproveLoadRequest");
                            Proc.AddParameter("@ProcessID", ParamType.Integer, processID);
                            Proc.AddParameter("@TransactionID", ParamType.Nvarchar, TransactionID);
                            Proc.AddParameter("@WarehouseID", ParamType.Integer, warehouseID);
                            result = ExecuteStoredProcedure(Proc);
                        }
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
                        execManager.LogIntegrationEnding(processID, result, "", message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private void SendPostingLoadRequest()
        {
            try
            {
                WriteMessage("\r\nSending load posts:\r\n");
                Result result = Result.UnKnown;
                int processID = 0;
                string TransactionID = "", warehouseID = "", status = "";
                string message = "", updateSyncFlag = "";

                //Call procedure to prepare transaction details to send
                Procedure Proc = new Procedure("sp_PrepareLoadPostToSend");
                Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID);
                Proc.AddParameter("@EmployeeID", ParamType.Integer, Filters.EmployeeID);
                Proc.AddParameter("@FromDate", ParamType.DateTime, Filters.FromDate);
                Proc.AddParameter("@ToDate", ParamType.DateTime, Filters.ToDate);
                result = ExecuteStoredProcedure(Proc);

                if (result != Result.Success)
                {
                    WriteMessage("Data preperation failed !!");
                    return;
                }

                //Read prepared details
                string DetailsQuery = string.Format(@"SELECT * FROM Stg_LoadPost WHERE TriggerID = {0}", TriggerID);
                incubeQuery = new InCubeQuery(db_vms, DetailsQuery);

                DataTable dtLoadPost = new DataTable();
                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    WriteMessage("Posts query failed !!");
                    return;
                }
                else
                {
                    dtLoadPost = incubeQuery.GetDataTable();
                    if (dtLoadPost.Rows.Count == 0)
                    {
                        WriteMessage("There is no posts to send ..");
                        return;
                    }
                }

                UFC_LoadRequest_WS.ZMM_LOAD_REQ_POST_RET_WSClient cli;
                cli = new UFC_LoadRequest_WS.ZMM_LOAD_REQ_POST_RET_WSClient();
                cli.ClientCredentials.UserName.UserName = CoreGeneral.Common.GeneralConfigurations.WS_UserName;
                cli.ClientCredentials.UserName.Password = CoreGeneral.Common.GeneralConfigurations.WS_Password;
                UFC_LoadRequest_WS.ZMM_INV_LOAD_POST_WS trans;

                for (int i = 0; i < dtLoadPost.Rows.Count; i++)
                {
                    try
                    {
                        result = Result.UnKnown;
                        processID = 0;
                        message = "";
                        TransactionID = dtLoadPost.Rows[i]["TRNUM"].ToString();
                        warehouseID = dtLoadPost.Rows[i]["WarehouseID"].ToString();
                        status = dtLoadPost.Rows[i]["STATUS"].ToString();
                        WriteMessage("\r\nSending Post of WH trans " + TransactionID + ": ");
                        ReportProgress();

                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(1, "Load Post");
                        filters.Add(8, TransactionID);
                        filters.Add(9, warehouseID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        incubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE Stg_LoadPost SET ProcessID = {0} WHERE TriggerID = {1} AND Trnum = '{2}' AND WarehouseID = {3}", processID, TriggerID, TransactionID, warehouseID));
                        incubeQuery.ExecuteNonQuery();

                        updateSyncFlag = string.Format("UPDATE WarehouseTransaction SET Synchronized = 1 WHERE TransactionID = '{0}' AND WarehouseID = {1}", TransactionID, warehouseID);

                        //Check last run for same transaction to avoid duplications
                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Transfers_S, new List<string>(filters.Values), processID, 600);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            incubeQuery = new InCubeQuery(db_vms, updateSyncFlag);
                            incubeQuery.ExecuteNonQuery();
                            result = Result.Duplicate;
                            message = "Load post already sent !!";
                            continue;
                        }

                        trans = new UFC_LoadRequest_WS.ZMM_INV_LOAD_POST_WS();
                        trans.TRNUM = TransactionID;
                        trans.STATUS = status;

                        UFC_LoadRequest_WS.ZMM_INV_LOAD_POST_WSResponse res = cli.ZMM_INV_LOAD_POST_WS(trans);
                        UFC_LoadRequest_WS.ZMM_INV_LOAD_RETURN[] results = res.ET_LOG;

                        if (results != null && results.Length > 0)
                        {
                            foreach (UFC_LoadRequest_WS.ZMM_INV_LOAD_RETURN rtn in results)
                            {
                                QueryBuilder qry = new QueryBuilder();
                                qry.SetField("TriggerID", TriggerID.ToString());
                                qry.SetField("ProcessID", processID.ToString());
                                qry.SetStringField("TransactionID", TransactionID);
                                qry.SetField("WarehouseID", warehouseID);
                                qry.SetStringField("ICODE", rtn.ICODE);
                                qry.SetStringField("UOM", rtn.UOM);
                                qry.SetStringField("MAT_DOC", rtn.MAT_DOC);
                                qry.SetStringField("MESSAGE", rtn.MESSAGE);
                                qry.SetStringField("MSGTY", rtn.MSGTY);
                                qry.SetField("QTY", rtn.QTY.ToString());
                                qry.InsertQueryString("Stg_LoadPostResponse", db_vms);
                                message = rtn.MESSAGE;

                                if (rtn.MSGTY.ToLower() == "s")
                                    result = Result.Success;
                                else if (rtn.MESSAGE.Contains("has already been posted"))
                                    result = Result.Duplicate;
                                else
                                    result = Result.Invalid;
                            }
                        }
                        else
                        {
                            WriteMessage(": No result from service");
                            message = "No result from service";
                            result = Result.NoFileRetreived;
                        }

                        if (result == Result.Success || result == Result.Duplicate)
                        {
                            incubeQuery = new InCubeQuery(db_vms, updateSyncFlag);
                            incubeQuery.ExecuteNonQuery();
                        }
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
            SendPendingLoadRequest();
            SendPostingLoadRequest();
        }
        //        public override void SendReciepts_bak(bool AllSalespersons, string Salesperson, DateTime FromDate, DateTime ToDate)
        //        {
        //            try
        //            {
        //                WriteMessage("\r\nSending Collections ... ");
        //                Result result = Result.UnKnown;
        //                string salespersonFilter = "";
        //                int processID = 0;
        //                string PaymentID = "";
        //                string message = "";

        //                //Get main payments
        //                if (!AllSalespersons)
        //                {
        //                    salespersonFilter = "AND CP.EmployeeID = " + Salesperson;
        //                }
        //                string PaymentsQuery = string.Format(@"SELECT DISTINCT CustomerPaymentID FROM CustomerPayment CP WHERE Synchronized = 0 AND PaymentTypeID < 4
        //AND CP.PaymentDate > '{0}' AND CP.PaymentDate < '{1}' AND CP.PaymentStatusID <> 5 {2}",
        //                     FromDate.ToString("yyyy-MM-dd"), ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
        //                incubeQuery = new InCubeQuery(db_vms, PaymentsQuery);

        //                DataTable dtPayments = new DataTable();
        //                if (incubeQuery.Execute() != InCubeErrors.Success)
        //                {
        //                    WriteMessage("Payments query failed !!");
        //                    return;
        //                }
        //                else
        //                {
        //                    dtPayments = incubeQuery.GetDataTable();
        //                    if (dtPayments.Rows.Count == 0)
        //                    {
        //                        WriteMessage("There is no payments to send ..");
        //                        return;
        //                    }
        //                    else
        //                    {
        //                        WriteMessage(dtPayments.Rows.Count + " collection(s) found");
        //                        SetProgressMax(dtPayments.Rows.Count);
        //                    }
        //                }

        //                UFC_Collection_WS.ZUFC_INVAN_COLLECTIONPOSTClient cli = new UFC_Collection_WS.ZUFC_INVAN_COLLECTIONPOSTClient();
        //                UFC_Collection_WS.ZinvanCollectionPost pay = new UFC_Collection_WS.ZinvanCollectionPost();
        //                UFC_Collection_WS.ZinVanCollection col = new UFC_Collection_WS.ZinVanCollection();

        //                for (int j = 0; j < dtPayments.Rows.Count; j++)
        //                {
        //                    try
        //                    {
        //                        result = Result.UnKnown;
        //                        processID = 0;
        //                        message = "";
        //                        PaymentID = dtPayments.Rows[j]["CustomerPaymentID"].ToString();
        //                        WriteMessage("\r\nSending payment " + PaymentID);
        //                        ReportProgress();
        //                        Dictionary<int, string> filters = new Dictionary<int, string>();
        //                        filters.Add(8, PaymentID);
        //                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

        //                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Reciept_S, new List<string>(filters.Values), processID, 600);
        //                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
        //                        {
        //                            incubeQuery = new InCubeQuery(db_vms, "UPDATE CustomerPayment SET Synchronized = 1 WHERE CustomerPaymentID = '" + PaymentID + "'");
        //                            incubeQuery.ExecuteNonQuery();
        //                            result = Result.Duplicate;
        //                            message = "Payments already sent !!";
        //                            WriteMessage(": Payments already sent !!");
        //                            continue;
        //                        }

        //                        //Get payment details
        //                        string PaymentsDetails = string.Format(@"SELECT CP.CustomerPaymentID, CP.TransactionID, C.CustomerCode, CO.CustomerCode OutletCode, CP.PaymentDate, E.EmployeeCode
        //, CP.AppliedAmount, CP.PaymentTypeID, CP.VoucherNumber, CP.VoucherDate, B.Code Bank,T.TransactionDate,Case When (T.Notes = 'SAP') Then 1 Else 2 End TransactionSource
        //FROM CustomerPayment CP
        //INNER JOIN Customer C ON C.CustomerID = CP.CustomerID
        //INNER JOIN CustomerOutlet CO ON CO.CustomerID = CP.CustomerID AND CO.OutletID = CP.OutletID
        //INNER JOIN Employee E ON E.EmployeeID = CP.EmployeeID
        //LEFT JOIN Bank B ON B.BankID = CP.BankID
        //INNER JOIN [Transaction] T on T.TransactionID = CP.TransactionID And  T.CustomerID = CP.CustomerID And  T.OutletID = CP.OutletID
        //WHERE CP.CustomerPaymentID = '{0}'",
        //                         PaymentID);

        //                        incubeQuery = new InCubeQuery(db_vms, PaymentsDetails);
        //                        if (incubeQuery.Execute() != InCubeErrors.Success)
        //                        {
        //                            result = Result.ErrorExecutingQuery;
        //                            message = "Payments details query failed !!";
        //                            WriteMessage(": Payments details query failed !!");
        //                            continue;
        //                        }

        //                        DataTable dtPaymentDetails = incubeQuery.GetDataTable();
        //                        if (dtPaymentDetails.Rows.Count == 0)
        //                        {
        //                            result = Result.NoRowsFound;
        //                            message = "No details returned !!";
        //                            WriteMessage(": No details returned !!");
        //                            continue;
        //                        }

        //                        cli = new UFC_Collection_WS.ZUFC_INVAN_COLLECTIONPOSTClient();
        //                        pay = new UFC_Collection_WS.ZinvanCollectionPost();
        //                        pay.GtCollection = new UFC_Collection_WS.ZinVanCollection[dtPaymentDetails.Rows.Count];
        //                        string invoices = "";
        //                        foreach (DataRow row in dtPaymentDetails.Rows)
        //                        {
        //                            invoices += row["TransactionID"].ToString() + ",";
        //                        }
        //                        invoices = invoices.Substring(0, invoices.Length - 1);
        //                        WriteMessage(" applied for " + invoices + ": ");
        //                        incubeQuery = new InCubeQuery(db_vms, "UPDATE Int_ExecutionDetails SET Filter2ID = 9, Filter2Value = '" + invoices + "' WHERE ID = " + processID);
        //                        incubeQuery.ExecuteNonQuery();

        //                        for (int i = 0; i < dtPaymentDetails.Rows.Count; i++)
        //                        {
        //                            try
        //                            {
        //                                col = new UFC_Collection_WS.ZinVanCollection();
        //                                col.ZpayNum = dtPaymentDetails.Rows[i]["CustomerPaymentID"].ToString();
        //                                col.ZvanInvoice = dtPaymentDetails.Rows[i]["TransactionID"].ToString();
        //                                col.ZbillTo = dtPaymentDetails.Rows[i]["CustomerCode"].ToString();
        //                                col.ZshipTo = dtPaymentDetails.Rows[i]["OutletCode"].ToString();
        //                                col.ZpayDate = DateTime.Parse(dtPaymentDetails.Rows[i]["PaymentDate"].ToString()).ToString("dd.MM.yyyy");
        //                                col.ZsalemanCode = dtPaymentDetails.Rows[i]["EmployeeCode"].ToString();
        //                                col.ZpaidAmount = Decimal.Parse(dtPaymentDetails.Rows[i]["AppliedAmount"].ToString()).ToString("#0.00");
        //                                col.ZpayType = dtPaymentDetails.Rows[i]["PaymentTypeID"].ToString();
        //                                col.ZinvDate = DateTime.Parse(dtPaymentDetails.Rows[i]["TransactionDate"].ToString()).ToString("dd.MM.yyyy");
        //                                col.ZinvType = dtPaymentDetails.Rows[i]["TransactionSource"].ToString();
        //                                if (dtPaymentDetails.Rows[i]["VoucherNumber"] == DBNull.Value)
        //                                    col.ZchequeNo = "";
        //                                else
        //                                    col.ZchequeNo = dtPaymentDetails.Rows[i]["VoucherNumber"].ToString();

        //                                if (dtPaymentDetails.Rows[i]["VoucherDate"] == DBNull.Value)
        //                                    col.ZchequeDate = "";
        //                                else
        //                                    col.ZchequeDate = DateTime.Parse(dtPaymentDetails.Rows[i]["VoucherDate"].ToString()).ToString("dd.MM.yyyy");

        //                                if (dtPaymentDetails.Rows[i]["Bank"] == DBNull.Value)
        //                                    col.ZbankCode = "";
        //                                else
        //                                    col.ZbankCode = dtPaymentDetails.Rows[i]["Bank"].ToString();

        //                                pay.GtCollection[i] = col;
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
        //                                message = "Failed in filling web service objects";
        //                                WriteMessage("Failed in filling web service objects !!");
        //                                result = Result.Invalid;
        //                                break;
        //                            }
        //                        }

        //                        //Send only if no errors among collections for same ID
        //                        if (result == Result.UnKnown)
        //                        {
        //                            try
        //                            {
        //                                UFC_Collection_WS.ZinvanCollectionPostResponse res = cli.ZinvanCollectionPost(pay);

        //                                if (res.GtStatus != null && res.GtStatus.Length > 0)
        //                                {
        //                                    message = res.GtStatus[0].StatusMess;
        //                                    WriteMessage(message);
        //                                    if (message.ToLower().Contains("success") || message.ToLower().Contains("sucess"))
        //                                    {
        //                                        result = Result.Success;
        //                                        incubeQuery = new InCubeQuery(db_vms, "UPDATE CustomerPayment SET Synchronized = 1 WHERE CustomerPaymentID = '" + PaymentID + "'");
        //                                        incubeQuery.ExecuteNonQuery();
        //                                    }
        //                                    else
        //                                    {
        //                                        result = Result.Failure;
        //                                    }
        //                                }
        //                                else
        //                                {
        //                                    WriteMessage("Error ..");
        //                                    message = "No result from service";
        //                                    result = Result.NoFileRetreived;
        //                                }
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
        //                                message = ex.Message;
        //                                WriteMessage("Failure !!");
        //                                result = Result.Failure;
        //                            }
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
        //                        message = ex.Message;
        //                        result = Result.Failure;
        //                        WriteMessage("Failure !!");
        //                    }
        //                    finally
        //                    {
        //                        execManager.LogIntegrationEnding(processID, result, "", message);
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
        //            }
        //        }
        public override void SendReciepts()
        {
            try
            {
                WriteMessage("\r\nSending Collections ... ");

                Result result = Result.UnKnown;
                int processID = 0;
                string PaymentID = "", CustomerID = "", OutletID = "", PaymentType = "", UpdateSynchronizedQuery = "";
                string message = "", invoices = "";

                //Call procedure to prepare transaction details to send
                Procedure Proc = new Procedure("sp_PreparePaymentsToSend");
                Proc.AddParameter("@TriggerID", ParamType.Integer, TriggerID);
                Proc.AddParameter("@EmployeeID", ParamType.Integer, Filters.EmployeeID);
                Proc.AddParameter("@FromDate", ParamType.DateTime, Filters.FromDate);
                Proc.AddParameter("@ToDate", ParamType.DateTime, Filters.ToDate);
                result = ExecuteStoredProcedure(Proc);

                if (result != Result.Success)
                {
                    WriteMessage("Payments preperation failed !!");
                    return;
                }

                //Read prepared details
                string DetailsQuery = string.Format(@"SELECT * FROM Stg_Payments WHERE TriggerID = {0}", TriggerID);
                incubeQuery = new InCubeQuery(db_vms, DetailsQuery);

                DataTable dtPaymentHeader = new DataTable();
                DataTable dtPaymentDetails = new DataTable();
                if (incubeQuery.Execute() != InCubeErrors.Success)
                {
                    WriteMessage("Payments query failed !!");
                    return;
                }
                else
                {
                    dtPaymentDetails = incubeQuery.GetDataTable();
                    if (dtPaymentDetails.Rows.Count == 0)
                    {
                        WriteMessage("There is no payments to send ..");
                        return;
                    }
                    else
                    {
                        //Extract distinct values table for header
                        dtPaymentHeader = dtPaymentDetails.DefaultView.ToTable(true, new string[] { "ZPAY_NUM", "CustomerID", "OutletID", "ZINV_TYPE" });
                        WriteMessage(dtPaymentHeader.Rows.Count + " collection(s) found");
                        execManager.UpdateActionTotalRows(TriggerID, dtPaymentHeader.Rows.Count);
                        SetProgressMax(dtPaymentHeader.Rows.Count);
                    }
                }

                UFC_Collections_WS.zinvan_integration1Client cli;
                cli = new UFC_Collections_WS.zinvan_integration1Client();
                cli.ClientCredentials.UserName.UserName = CoreGeneral.Common.GeneralConfigurations.WS_UserName;
                cli.ClientCredentials.UserName.Password = CoreGeneral.Common.GeneralConfigurations.WS_Password;
                UFC_Collections_WS.ZINVAN_COLLECTION_POST_C1 pay;
                UFC_Collections_WS.ZIN_VAN_COLLECTION col;

                for (int j = 0; j < dtPaymentHeader.Rows.Count; j++)
                {
                    try
                    {
                        //Read transaction primary key and log to execution details as well as to staging table
                        result = Result.UnKnown;
                        processID = 0;
                        message = "";
                        PaymentID = dtPaymentHeader.Rows[j]["ZPAY_NUM"].ToString();
                        CustomerID = dtPaymentHeader.Rows[j]["CustomerID"].ToString();
                        OutletID = dtPaymentHeader.Rows[j]["OutletID"].ToString();
                        PaymentType = dtPaymentHeader.Rows[j]["ZINV_TYPE"].ToString();
                        WriteMessage("\r\nSending payment " + PaymentID + ": ");
                        ReportProgress();
                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(8, PaymentID);
                        filters.Add(9, CustomerID + ":" + OutletID);
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);
                        incubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE Stg_Payments SET ProcessID = {0} WHERE TriggerID = {1} AND ZPAY_NUM = '{2}' AND CustomerID = {3} AND OutletID = {4}", processID, TriggerID, PaymentID, CustomerID, OutletID));
                        incubeQuery.ExecuteNonQuery();

                        if (PaymentType == "")
                            UpdateSynchronizedQuery = string.Format("UPDATE CustomerUnallocatedPayment SET Synchronised = 1 WHERE CustomerPaymentID = '{0}' AND CustomerID = {1} AND OutletID = {2}", PaymentID, CustomerID, OutletID);
                        else
                            UpdateSynchronizedQuery = string.Format("UPDATE CustomerPayment SET Synchronized = 1 WHERE CustomerPaymentID = '{0}' AND CustomerID = {1} AND OutletID = {2}", PaymentID, CustomerID, OutletID);

                        //Check last run for same transaction to avoid duplications
                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Reciept_S, new List<string>(filters.Values), processID, 600);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                            incubeQuery.ExecuteNonQuery();
                            result = Result.Duplicate;
                            message = "Transaction already sent !!";
                            continue;
                        }

                        //Select details for current transaction
                        DataRow[] drDetails = dtPaymentDetails.Select(string.Format("ZPAY_NUM = '{0}' AND CustomerID = {1} AND OutletID = {2} AND ZINV_TYPE = '{3}'", PaymentID, CustomerID, OutletID, PaymentType));
                        if (drDetails.Length == 0)
                        {
                            result = Result.NoRowsFound;
                            message = "No details returned !!";
                            continue;
                        }

                        //Loop through details and fill objects
                        pay = new UFC_Collections_WS.ZINVAN_COLLECTION_POST_C1();
                        pay.GT_COLLECTION = new UFC_Collections_WS.ZIN_VAN_COLLECTION[drDetails.Length];
                        invoices = "";
                        for (int i = 0; i < drDetails.Length; i++)
                        {
                            try
                            {
                                col = new UFC_Collections_WS.ZIN_VAN_COLLECTION();
                                col.ZPAY_NUM = drDetails[i]["ZPAY_NUM"].ToString();
                                col.ZVAN_INVOICE = drDetails[i]["ZVAN_INVOICE"].ToString();
                                invoices = invoices + col.ZVAN_INVOICE + ",";
                                col.ZBILL_TO = drDetails[i]["ZBILL_TO"].ToString();
                                col.ZSHIP_TO = drDetails[i]["ZSHIP_TO"].ToString();
                                col.ZPAY_DATE = drDetails[i]["ZPAY_DATE"].ToString();
                                col.ZSALEMAN_CODE = drDetails[i]["ZSALEMAN_CODE"].ToString();
                                col.ZPAID_AMOUNT = drDetails[i]["ZPAID_AMOUNT"].ToString();
                                col.ZPAY_TYPE = drDetails[i]["ZPAY_TYPE"].ToString();
                                col.ZINV_DATE = drDetails[i]["ZINV_DATE"].ToString();
                                col.ZINV_TYPE = drDetails[i]["ZINV_TYPE"].ToString();
                                col.ZCHEQUE_NO = drDetails[i]["ZCHEQUE_NO"].ToString();
                                col.ZCHEQUE_DATE = drDetails[i]["ZCHEQUE_DATE"].ToString();
                                col.ZBANK_CODE = drDetails[i]["ZBANK_CODE"].ToString();

                                pay.GT_COLLECTION[i] = col;
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                                message = "Failed in filling web service objects";
                                result = Result.Invalid;
                                break;
                            }
                        }
                        invoices = invoices.TrimEnd(new char[] { ',' });

                        //Send only if no errors among collections for same ID
                        if (result == Result.UnKnown)
                        {
                            try
                            {
                                UFC_Collections_WS.ZINVAN_COLLECTION_POST_C1Response res = cli.ZINVAN_COLLECTION_POST_C1(pay);

                                if (res != null && res.GT_STATUS != null && res.GT_STATUS.Length > 0)
                                {
                                    foreach (UFC_Collections_WS.ZFI_COLL_STATUS status in res.GT_STATUS)
                                    {
                                        QueryBuilder qry = new QueryBuilder();
                                        qry.SetField("TriggerID", TriggerID.ToString());
                                        qry.SetField("ProcessID", processID.ToString());
                                        qry.SetStringField("ZPAY_NUM", status.ZPAY_NUM);
                                        qry.SetStringField("ZVAN_INVOICE", status.ZVAN_INVOICE);
                                        qry.SetStringField("ZBILL_TO", status.ZBILL_TO);
                                        qry.SetStringField("ZSHIP_TO", status.ZSHIP_TO);
                                        qry.SetStringField("[TYPE]", status.TYPE);
                                        qry.SetStringField("STATUS_MESS", status.STATUS_MESS);
                                        qry.InsertQueryString("Stg_PaymentResponse", db_vms);
                                        message = status.STATUS_MESS;
                                        if (status.TYPE.ToLower() == "s")
                                            result = Result.Success;
                                        else
                                            result = Result.Invalid;
                                    }
                                }
                                else
                                {
                                    WriteMessage("Error ..");
                                    message = "No result from service";
                                    result = Result.NoFileRetreived;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                                message = ex.Message;
                                WriteMessage("Failure !!");
                                result = Result.Failure;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        message = ex.Message;
                        result = Result.Failure;
                        WriteMessage("Failure !!");
                    }
                    finally
                    {
                        incubeQuery = new InCubeQuery(db_vms, string.Format("UPDATE Int_ExecutionDetails SET Filter3ID = 10, Filter3Value = '{0}' WHERE ID = {1}", invoices, processID));
                        incubeQuery.ExecuteNonQuery();
                        if (result == Result.Success)
                        {
                            incubeQuery = new InCubeQuery(db_vms, UpdateSynchronizedQuery);
                            incubeQuery.ExecuteNonQuery();
                        }
                        WriteMessage(message);
                        execManager.LogIntegrationEnding(processID, result, "", message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }


        //Update VehicleStock
        public override void UpdateWarehouse()
        {
            try
            {
                WriteMessage("\r\nGet Vehicle Stock:\r\n");

                Result result = Result.UnKnown;
                int processID = 0;
                string message = "";

              
                UFC_VehicleStock_WS.ZVECHSTOCKClient cli;
                cli = new UFC_VehicleStock_WS.ZVECHSTOCKClient();
                cli.ClientCredentials.UserName.UserName = "LORHAN";
                cli.ClientCredentials.UserName.Password = "LOG@UFC230";
                UFC_VehicleStock_WS.ZFM_VECH_STOCK vhc;
                UFC_VehicleStock_WS.ZFM_VECH_STOCKRequest req;

                try
                {
                    //Read transaction primary key and log to execution details as well as to staging table
                    result = Result.UnKnown;
                    processID = 0;
                    message = "";
                    vhc = new UFC_VehicleStock_WS.ZFM_VECH_STOCK();
                    req = new UFC_VehicleStock_WS.ZFM_VECH_STOCKRequest();

                    //Fill header object
                    vhc.VEHCODE = "AFG2";
                    req.ZFM_VECH_STOCK = vhc;


                    //Select details for current trasnaction
                    UFC_VehicleStock_WS.ZFM_VECH_STOCKResponse res = cli.ZFM_VECH_STOCK(req.ZFM_VECH_STOCK);
                    
                    //UFC_VehicleStock_WS.ZVECSTOCK[] results =

                    if (res.VECHSTOCK != null && res.VECHSTOCK.Length > 0)
                    {
                        result = Result.Invalid;
                        foreach (UFC_VehicleStock_WS.ZVECSTOCK rtn in res.VECHSTOCK)
                        {
                            QueryBuilder qry = new QueryBuilder();
                            qry.SetField("VEHCODE", rtn.VEHCODE.ToString());
                            qry.SetField("ICODE", rtn.ICODE.ToString());
                            qry.SetField("UOM", rtn.UOM.ToString());
                            qry.SetField("EXPIRY_DATE", rtn.EXPIRY_DATE.ToString());
                            qry.SetField("BATCH_NO", rtn.BATCH_NO.ToString());
                            qry.SetField("QTY", rtn.QTY.ToString());
                            qry.InsertQueryString("Stg_VehicleStock", db_vms);
                            message = "Success";

                            result = Result.Success;
                            
                        }
                    }
                    else
                    {
                        WriteMessage(": No result from service");
                        message = "No result from service";
                        result = Result.NoFileRetreived;
                    }

                    if (result == Result.Success)
                    {
                        //Proc = new Procedure("sp_ApproveLoadRequest");
                        //Proc.AddParameter("@ProcessID", ParamType.Integer, processID);
                        //Proc.AddParameter("@TransactionID", ParamType.Nvarchar, TransactionID);
                        //Proc.AddParameter("@WarehouseID", ParamType.Integer, warehouseID);
                        //result = ExecuteStoredProcedure(Proc);
                    }
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
                    execManager.LogIntegrationEnding(processID, result, "", message);
                }
                }
            
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
    }
}